using UnityEngine;
using System.Collections.Generic;

public class WorldGeneration : MonoBehaviour
{
    [Header("World Generation Settings")]
    public int seed;
    public float scale = 40f;
    public int chunkRenderDistance = 5;

    [Header("Height Settings")]
    public int minHeight = 0;
    public int maxHeight = 60;
    public int oceanLevel = 20;

    [Header("Noise Settings")]
    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2.0f;

    [Header("Block Materials")]
    public BlockMaterials grassBlock;
    public BlockMaterials dirtBlock;
    public BlockMaterials stoneBlock;
    public BlockMaterials waterBlock;
    
    private Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

    void Start()
    {
        if (seed == 0)
        {
            seed = Random.Range(10000, 99999);
        }
        Random.InitState(seed);

        GenerateInitialChunks();
    }

    void GenerateInitialChunks()
    {
        for (int x = -chunkRenderDistance; x <= chunkRenderDistance; x++)
        {
            for (int z = -chunkRenderDistance; z <= chunkRenderDistance; z++)
            {
                Vector3Int chunkPos = new Vector3Int(x, 0, z);
                GenerateChunk(chunkPos);
            }
        }
    }

    public Chunk GenerateChunk(Vector3Int chunkPosition)
    {
        if (chunks.ContainsKey(chunkPosition))
        {
            return chunks[chunkPosition];
        }

        Chunk newChunk = new Chunk(chunkPosition);
        chunks.Add(chunkPosition, newChunk);

        int chunkSize = Chunk.chunkSize;
        int chunkHeight = Chunk.chunkHeight;

        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                int worldX = chunkPosition.x * chunkSize + x;
                int worldZ = chunkPosition.z * chunkSize + z;

                float noiseHeight = GetFractalPerlinNoise(worldX, worldZ);
                int height = Mathf.FloorToInt(Mathf.Lerp(minHeight, maxHeight, noiseHeight));
                height = Mathf.Clamp(height, minHeight, chunkHeight - 1);

                for (int y = 0; y < chunkHeight; y++)
                {
                    if (y < height)
                    {
                        if (y < oceanLevel - 1)
                        {
                            newChunk.blocks[x, y, z].materials = stoneBlock;
                        }
                        else if (y < height - 3)
                        {
                            newChunk.blocks[x, y, z].materials = dirtBlock;
                        }
                        else
                        {
                            newChunk.blocks[x, y, z].materials = grassBlock;
                        }
                    }
                    else if (y <= oceanLevel && y >= height)
                    {
                        newChunk.blocks[x, y, z].materials      = waterBlock;
                        newChunk.blocks[x, y, z].state          = MatterState.Liquid;
                        newChunk.blocks[x, y, z].fluidLevel     = FluidSimulator.MaxLevel;
                        newChunk.blocks[x, y, z].viscosity      = waterBlock.viscosity;
                        newChunk.blocks[x, y, z].fluidDensity   = waterBlock.fluidDensity;
                        newChunk.blocks[x, y, z].surfaceTension = waterBlock.surfaceTension;
                    }
                    else
                    {
                        newChunk.blocks[x, y, z].materials = null;
                    }
                }
            }
        }

        BuildMesh(newChunk);
        newChunk.isGenerated = true;
        return newChunk;
    }

    float GetFractalPerlinNoise(int worldX, int worldZ)
    {
        float amplitude = 1;
        float frequency = 1;
        float noiseHeight = 0;
        float maxValue = 0;

        for (int i = 0; i < octaves; i++)
        {
            float sampleX = (worldX + seed) / scale * frequency;
            float sampleZ = (worldZ + seed) / scale * frequency;

            float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ);
            noiseHeight += perlinValue * amplitude;
            maxValue    += amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return noiseHeight / maxValue;
    }

    public void BuildMesh(Chunk chunk)
    {
        int chunkSize = Chunk.chunkSize;
        int chunkHeight = Chunk.chunkHeight;

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        // Separate triangle list per unique material for multi-submesh rendering.
        Dictionary<BlockMaterials, List<int>> matTriangles = new Dictionary<BlockMaterials, List<int>>();

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    Block block = chunk.blocks[x, y, z];
                    if (block.materials == null) continue;
                    if (block.state == MatterState.Liquid) continue; // rendered by FluidMeshBuilder

                    if (!matTriangles.ContainsKey(block.materials))
                        matTriangles[block.materials] = new List<int>();

                    List<int> triangles = matTriangles[block.materials];
                    Vector3 blockPos = new Vector3(x, y, z);

                    AddFace(vertices, triangles, uvs, blockPos, chunk, x, y + 1, z, Face.Top);
                    AddFace(vertices, triangles, uvs, blockPos, chunk, x, y - 1, z, Face.Bottom);
                    AddFace(vertices, triangles, uvs, blockPos, chunk, x + 1, y, z, Face.Right);
                    AddFace(vertices, triangles, uvs, blockPos, chunk, x - 1, y, z, Face.Left);
                    AddFace(vertices, triangles, uvs, blockPos, chunk, x, y, z + 1, Face.Front);
                    AddFace(vertices, triangles, uvs, blockPos, chunk, x, y, z - 1, Face.Back);
                }
            }
        }

        GameObject chunkObj = new GameObject($"Chunk_{chunk.position.x}_{chunk.position.z}");
        chunkObj.transform.parent = this.transform;
        chunkObj.transform.position = new Vector3(chunk.position.x * chunkSize, 0, chunk.position.z * chunkSize);
        chunk.chunkObject = chunkObj;

        MeshFilter mf = chunkObj.AddComponent<MeshFilter>();
        MeshRenderer mr = chunkObj.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);

        if (matTriangles.Count > 0)
        {
            mesh.subMeshCount = matTriangles.Count;
            Material[] materials = new Material[matTriangles.Count];
            Material fallback = null;
            int idx = 0;
            foreach (var kvp in matTriangles)
            {
                mesh.SetTriangles(kvp.Value, idx);
                if (kvp.Key.material != null && kvp.Key.material.Length > 0)
                {
                    materials[idx] = kvp.Key.material[0];
                }
                else
                {
                    if (fallback == null) fallback = new Material(Shader.Find("Standard"));
                    materials[idx] = fallback;
                }
                idx++;
            }
            mr.materials = materials;
        }
        else
        {
            mr.material = new Material(Shader.Find("Standard"));
        }

        mesh.RecalculateNormals();
        mf.mesh = mesh;
    }

    enum Face { Top, Bottom, Left, Right, Front, Back }

    static void AddFace(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, Vector3 pos, Chunk chunk, int nx, int ny, int nz, Face face)
    {
        int chunkSizeX = Chunk.chunkSize;
        int chunkHeight = Chunk.chunkHeight;
        int chunkSizeZ = Chunk.chunkSize;

        bool renderFace = true;
        if (nx >= 0 && nx < chunkSizeX && ny >= 0 && ny < chunkHeight && nz >= 0 && nz < chunkSizeZ)
        {
            Block nb = chunk.blocks[nx, ny, nz];
            // Only cull against a solid neighbour; liquid neighbours are transparent.
            if (nb.materials != null && nb.state != MatterState.Liquid) renderFace = false;
        }

        if (!renderFace) return;

        int vertStart = vertices.Count;

        switch (face)
        {
            case Face.Top:
                vertices.Add(pos + new Vector3(0, 1, 0));
                vertices.Add(pos + new Vector3(1, 1, 0));
                vertices.Add(pos + new Vector3(1, 1, 1));
                vertices.Add(pos + new Vector3(0, 1, 1));
                break;
            case Face.Bottom:
                vertices.Add(pos + new Vector3(0, 0, 0));
                vertices.Add(pos + new Vector3(1, 0, 0));
                vertices.Add(pos + new Vector3(1, 0, 1));
                vertices.Add(pos + new Vector3(0, 0, 1));
                break;
            case Face.Left:
                vertices.Add(pos + new Vector3(0, 0, 0));
                vertices.Add(pos + new Vector3(0, 1, 0));
                vertices.Add(pos + new Vector3(0, 1, 1));
                vertices.Add(pos + new Vector3(0, 0, 1));
                break;
            case Face.Right:
                vertices.Add(pos + new Vector3(1, 0, 0));
                vertices.Add(pos + new Vector3(1, 1, 0));
                vertices.Add(pos + new Vector3(1, 1, 1));
                vertices.Add(pos + new Vector3(1, 0, 1));
                break;
            case Face.Front:
                vertices.Add(pos + new Vector3(0, 0, 1));
                vertices.Add(pos + new Vector3(0, 1, 1));
                vertices.Add(pos + new Vector3(1, 1, 1));
                vertices.Add(pos + new Vector3(1, 0, 1));
                break;
            case Face.Back:
                vertices.Add(pos + new Vector3(0, 0, 0));
                vertices.Add(pos + new Vector3(1, 0, 0));
                vertices.Add(pos + new Vector3(1, 1, 0));
                vertices.Add(pos + new Vector3(0, 1, 0));
                break;
        }

        // Bottom and Right faces need reversed winding so their normals point outward.
        if (face == Face.Bottom || face == Face.Right)
            triangles.AddRange(new int[] { vertStart, vertStart + 1, vertStart + 2, vertStart, vertStart + 2, vertStart + 3 });
        else
            triangles.AddRange(new int[] { vertStart, vertStart + 2, vertStart + 1, vertStart, vertStart + 3, vertStart + 2 });

        uvs.AddRange(new Vector2[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
    }
}
