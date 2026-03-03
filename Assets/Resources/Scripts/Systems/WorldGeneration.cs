using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Orchestrates all world-generation layers for a single chunk:
/// heightmap → climate → biome → terrain fill → cave carving → ore placement → mesh build.
/// </summary>
public static class WorldGeneration
{
    public static int seed = 23642;

    // ── Full generation (palette path) ───────────────────────────────────────

    /// <summary>
    /// Fully generates a chunk using the complete pipeline.
    /// All block materials are sourced from <paramref name="palette"/>.
    /// </summary>
    public static void GenerateChunk(Chunk chunk, WorldBlockPalette palette)
    {
        int cs = WorldSettings.ChunkSize;

        // 1 ── Heightmap: surface world-Y for every XZ column in this chunk ──
        int[,] surfaceHeights = new int[cs, cs];
        for (int lx = 0; lx < cs; lx++)
        for (int lz = 0; lz < cs; lz++)
        {
            int wx = chunk.position.x * cs + lx;
            int wz = chunk.position.z * cs + lz;
            surfaceHeights[lx, lz] = HeightmapGenerator.GetSurfaceY(wx, wz, seed);
        }

        // 2 ── Climate: sample at chunk centre for a single biome per chunk ───
        int   cx        = chunk.position.x * cs + cs / 2;
        int   cz        = chunk.position.z * cs + cs / 2;
        int   cSurfY    = surfaceHeights[cs / 2, cs / 2];
        float temp      = ClimateGenerator.GetTemperature(cx, cz, cSurfY, seed);
        float moisture  = ClimateGenerator.GetMoisture(cx, cz, cSurfY, seed);
        BiomeType biome = BiomeSystem.Classify(temp, moisture, cSurfY);

        // Resolve block materials from biome rules
        BlockMaterials surfaceMat    = palette.GetByName(BiomeSystem.SurfaceBlockName(biome));
        BlockMaterials subSurfaceMat = palette.GetByName(BiomeSystem.SubsurfaceBlockName(biome));
        int            subDepth      = BiomeSystem.SubsurfaceDepth(biome);
        BlockMaterials stoneMat      = palette.stone  != null ? palette.stone  : surfaceMat;
        BlockMaterials bedrockMat    = palette.bedrock != null ? palette.bedrock : stoneMat;

        // 3 ── Terrain fill ────────────────────────────────────────────────────
        for (int lx = 0; lx < cs; lx++)
        for (int lz = 0; lz < cs; lz++)
        {
            int surfY = surfaceHeights[lx, lz];

            for (int ly = 0; ly < cs; ly++)
            {
                int wy = WorldSettings.WorldYFromChunk(chunk.position.y, ly);

                if (wy > surfY)
                {
                    // Above surface: air, or standing water below sea level
                    if (wy <= WorldSettings.SeaLevel && palette.water != null)
                        chunk.blocks[lx, ly, lz] = new Block
                        {
                            materials  = palette.water,
                            fluidLevel = FluidSimulator.MaxLevel,
                            state      = MatterState.Liquid,
                        };
                    else
                        chunk.blocks[lx, ly, lz] = default;
                }
                else if (wy <= WorldSettings.WorldMinY)
                {
                    chunk.blocks[lx, ly, lz] = NewBlock(bedrockMat);
                }
                else
                {
                    int depth = surfY - wy;   // 0 = surface, increasing downward
                    if (depth == 0)
                        chunk.blocks[lx, ly, lz] = NewBlock(surfaceMat);
                    else if (depth <= subDepth)
                        chunk.blocks[lx, ly, lz] = NewBlock(subSurfaceMat);
                    else
                        chunk.blocks[lx, ly, lz] = NewBlock(stoneMat);
                }
            }
        }

        // 4 ── Cave carving ────────────────────────────────────────────────────
        CaveGenerator.CarveCaves(chunk, surfaceHeights, palette, seed, biome);

        // 5 ── Ore placement ───────────────────────────────────────────────────
        OreGenerator.PlaceOres(chunk, palette, seed);

        // 6 ── Mesh ────────────────────────────────────────────────────────────
        BuildMesh(chunk);
        chunk.isGenerated = true;
    }

    // ── Legacy single-material overload (backward compatibility) ─────────────

    /// <summary>
    /// Simple generation using a single material. Kept so existing code
    /// (e.g. <see cref="WorldTest"/>) continues to compile without changes.
    /// No biomes, caves, or ores are applied.
    /// </summary>
    public static void GenerateChunk(Chunk chunk, BlockMaterials material)
    {
        int cs = WorldSettings.ChunkSize;

        for (int lx = 0; lx < cs; lx++)
        for (int lz = 0; lz < cs; lz++)
        {
            float wx = chunk.position.x * cs + lx;
            float wz = chunk.position.z * cs + lz;

            float noise      = Mathf.PerlinNoise((wx + seed) / 40f, (wz + seed) / 40f);
            int   localSurfY = Mathf.Clamp(Mathf.FloorToInt(noise * (cs - 1)), 0, cs - 1);

            for (int ly = 0; ly <= localSurfY; ly++)
                chunk.blocks[lx, ly, lz] = new Block { materials = material };
        }

        BuildMesh(chunk);
        chunk.isGenerated = true;
    }

    // ── Mesh building ─────────────────────────────────────────────────────────

    public static void BuildMesh(Chunk chunk)
    {
        int cs = WorldSettings.ChunkSize;

        var vertices  = new List<Vector3>();
        var triangles = new List<int>();
        var uvs       = new List<Vector2>();

        for (int x = 0; x < cs; x++)
        for (int y = 0; y < cs; y++)
        for (int z = 0; z < cs; z++)
        {
            if (chunk.blocks[x, y, z].materials == null) continue;

            Vector3 pos = new Vector3(x, y, z);
            AddFace(vertices, triangles, uvs, pos, chunk.blocks, x, y + 1, z, Face.Top);
            AddFace(vertices, triangles, uvs, pos, chunk.blocks, x, y - 1, z, Face.Bottom);
            AddFace(vertices, triangles, uvs, pos, chunk.blocks, x + 1, y, z, Face.Right);
            AddFace(vertices, triangles, uvs, pos, chunk.blocks, x - 1, y, z, Face.Left);
            AddFace(vertices, triangles, uvs, pos, chunk.blocks, x, y, z + 1, Face.Front);
            AddFace(vertices, triangles, uvs, pos, chunk.blocks, x, y, z - 1, Face.Back);
        }

        // Create or reuse the chunk GameObject
        if (chunk.chunkObject == null)
        {
            chunk.chunkObject = new GameObject(
                $"Chunk_{chunk.position.x}_{chunk.position.y}_{chunk.position.z}");
            chunk.chunkObject.transform.position = new Vector3(
                chunk.position.x * cs,
                WorldSettings.WorldMinY + chunk.position.y * cs,
                chunk.position.z * cs);
        }

        MeshFilter mf = chunk.chunkObject.GetComponent<MeshFilter>();
        if (mf == null) mf = chunk.chunkObject.AddComponent<MeshFilter>();
        MeshRenderer mr = chunk.chunkObject.GetComponent<MeshRenderer>();
        if (mr == null) mr = chunk.chunkObject.AddComponent<MeshRenderer>();

        // Assign the first valid material found in the chunk
        BlockMaterials firstMat = null;
        for (int x = 0; x < cs && firstMat == null; x++)
        for (int y = 0; y < cs && firstMat == null; y++)
        for (int z = 0; z < cs && firstMat == null; z++)
            if (chunk.blocks[x, y, z].materials?.material?.Length > 0)
                firstMat = chunk.blocks[x, y, z].materials;
        if (firstMat != null) mr.material = firstMat.material[0];

        // UInt32 index format supports meshes with > 65535 vertices
        var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mf.mesh = mesh;
    }

    // ── Face helpers ──────────────────────────────────────────────────────────

    private enum Face { Top, Bottom, Left, Right, Front, Back }

    private static void AddFace(List<Vector3> verts, List<int> tris, List<Vector2> uvs,
                                 Vector3 pos, Block[,,] blocks,
                                 int nx, int ny, int nz, Face face)
    {
        int cs = WorldSettings.ChunkSize;

        // Only render face when neighbour is air or out of bounds
        if (nx >= 0 && nx < cs && ny >= 0 && ny < cs && nz >= 0 && nz < cs)
            if (blocks[nx, ny, nz].materials != null) return;

        int v = verts.Count;
        switch (face)
        {
            case Face.Top:
                verts.Add(pos + new Vector3(0,1,0)); verts.Add(pos + new Vector3(1,1,0));
                verts.Add(pos + new Vector3(1,1,1)); verts.Add(pos + new Vector3(0,1,1));
                tris.AddRange(new[]{ v,v+2,v+1, v,v+3,v+2 }); break;
            case Face.Bottom:
                verts.Add(pos + new Vector3(0,0,0)); verts.Add(pos + new Vector3(1,0,0));
                verts.Add(pos + new Vector3(1,0,1)); verts.Add(pos + new Vector3(0,0,1));
                tris.AddRange(new[]{ v,v+1,v+2, v,v+2,v+3 }); break;
            case Face.Left:
                verts.Add(pos + new Vector3(0,0,0)); verts.Add(pos + new Vector3(0,1,0));
                verts.Add(pos + new Vector3(0,1,1)); verts.Add(pos + new Vector3(0,0,1));
                tris.AddRange(new[]{ v,v+2,v+1, v,v+3,v+2 }); break;
            case Face.Right:
                verts.Add(pos + new Vector3(1,0,0)); verts.Add(pos + new Vector3(1,1,0));
                verts.Add(pos + new Vector3(1,1,1)); verts.Add(pos + new Vector3(1,0,1));
                tris.AddRange(new[]{ v,v+1,v+2, v,v+2,v+3 }); break;
            case Face.Front:
                verts.Add(pos + new Vector3(0,0,1)); verts.Add(pos + new Vector3(0,1,1));
                verts.Add(pos + new Vector3(1,1,1)); verts.Add(pos + new Vector3(1,0,1));
                tris.AddRange(new[]{ v,v+2,v+1, v,v+3,v+2 }); break;
            case Face.Back:
                verts.Add(pos + new Vector3(0,0,0)); verts.Add(pos + new Vector3(1,0,0));
                verts.Add(pos + new Vector3(1,1,0)); verts.Add(pos + new Vector3(0,1,0));
                tris.AddRange(new[]{ v,v+2,v+1, v,v+3,v+2 }); break;
        }
        uvs.AddRange(new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
    }

    // ── Utility ───────────────────────────────────────────────────────────────

    private static Block NewBlock(BlockMaterials mat)
        => new Block { materials = mat, temperature = 293f };
}