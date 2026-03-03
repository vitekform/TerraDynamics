using UnityEngine;

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

    // ── Cube building ─────────────────────────────────────────────────────────

    /// <summary>
    /// Spawns one unit-cube GameObject per visible block in the chunk.
    /// A block is "visible" when at least one of its six face-neighbours is air
    /// (or lies outside the chunk boundary). Fully-buried interior blocks are
    /// skipped entirely — this is the face-based occlusion culling step.
    ///
    /// All cubes share the same Mesh asset (GetCubeMesh) so GPU instancing can
    /// batch same-material cubes into a single draw call automatically.
    /// </summary>
    public static void BuildMesh(Chunk chunk)
    {
        int cs = WorldSettings.ChunkSize;

        // Create or reuse the chunk parent (empty transform, no renderer)
        if (chunk.chunkObject == null)
        {
            chunk.chunkObject = new GameObject(
                $"Chunk_{chunk.position.x}_{chunk.position.y}_{chunk.position.z}");
            chunk.chunkObject.transform.position = new Vector3(
                chunk.position.x * cs,
                WorldSettings.WorldMinY + chunk.position.y * cs,
                chunk.position.z * cs);
        }
        else
        {
            // Destroy old cube children before rebuilding
            for (int i = chunk.chunkObject.transform.childCount - 1; i >= 0; i--)
                Object.Destroy(chunk.chunkObject.transform.GetChild(i).gameObject);
        }

        Mesh cubeMesh = GetCubeMesh();

        for (int x = 0; x < cs; x++)
        for (int y = 0; y < cs; y++)
        for (int z = 0; z < cs; z++)
        {
            Block block = chunk.blocks[x, y, z];
            if (block.materials == null) continue;          // air — skip
            if (!HasExposedFace(chunk.blocks, x, y, z, cs)) continue; // buried — cull

            // Spawn a unit cube for this surface block
            var go = new GameObject();
            go.transform.SetParent(chunk.chunkObject.transform, false);
            // Unity cubes are centred at the origin, so offset by 0.5 on every axis
            go.transform.localPosition = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = cubeMesh;

            var mr = go.AddComponent<MeshRenderer>();
            if (block.materials?.material?.Length > 0)
                mr.sharedMaterial = block.materials.material[0];
        }
    }

    // ── Culling helper ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true when block (x,y,z) has at least one face neighbour that is
    /// air or lies outside the chunk bounds (and therefore might be visible).
    /// Returns false when all six neighbours are solid — the block is buried.
    /// </summary>
    private static bool HasExposedFace(Block[,,] blocks, int x, int y, int z, int cs)
    {
        if (x == 0      || blocks[x - 1, y, z].materials == null) return true;
        if (x == cs - 1 || blocks[x + 1, y, z].materials == null) return true;
        if (y == 0      || blocks[x, y - 1, z].materials == null) return true;
        if (y == cs - 1 || blocks[x, y + 1, z].materials == null) return true;
        if (z == 0      || blocks[x, y, z - 1].materials == null) return true;
        if (z == cs - 1 || blocks[x, y, z + 1].materials == null) return true;
        return false;
    }

    // ── Shared cube mesh ──────────────────────────────────────────────────────

    private static Mesh s_cubeMesh;

    /// <summary>
    /// Returns a cached reference to Unity's built-in unit cube mesh.
    /// All cube GameObjects share this single mesh asset; no per-block mesh is allocated.
    /// </summary>
    private static Mesh GetCubeMesh()
    {
        if (s_cubeMesh != null) return s_cubeMesh;
        // CreatePrimitive gives us the canonical Unity cube mesh; destroy the
        // temporary GameObject immediately — the mesh asset itself persists.
        var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        s_cubeMesh = temp.GetComponent<MeshFilter>().sharedMesh;
        Object.Destroy(temp);
        return s_cubeMesh;
    }

    // ── Utility ───────────────────────────────────────────────────────────────

    private static Block NewBlock(BlockMaterials mat)
        => new Block { materials = mat, temperature = 293f };
}