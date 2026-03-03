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
            return null;
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
                        newChunk.blocks[x, y, z].materials = waterBlock;
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

        for (int i = 0; i < octaves; i++)
        {
            float sampleX = (worldX + seed) / scale * frequency;
            float sampleZ = (worldZ + seed) / scale * frequency;

            float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ);
            noiseHeight += perlinValue * amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return noiseHeight;
    }

    public void BuildMesh(Chunk chunk)
    {
        int chunkSize = Chunk.chunkSize;
        int chunkHeight = Chunk.chunkHeight;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    Block block = chunk.blocks[x, y, z];
                    if (block.materials == null) continue;

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

        GameObject chunkObj = new GameObject($"Chunk_{chunk.position.x}_{chunk.position.y}");
        chunkObj.transform.parent = this.transform;
        chunkObj.transform.position = new Vector3(chunk.position.x * chunkSize, 0, chunk.position.y * chunkSize);
        chunk.chunkObject = chunkObj;

        MeshFilter mf = chunkObj.AddComponent<MeshFilter>();
        MeshRenderer mr = chunkObj.AddComponent<MeshRenderer>();

        if (vertices.Count > 0)
        {
            // This part is tricky without knowing BlockMaterials structure.
            // For now, I'll just assign the grass block material.
            // You might need to create a texture atlas for different block types.
            mr.material = grassBlock.material[0];
        }
        else
        {
            mr.material = new Material(Shader.Find("Standard"));
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
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
            if (chunk.blocks[nx, ny, nz].materials != null) renderFace = false;
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
        
        triangles.AddRange(new int[] { vertStart, vertStart + 2, vertStart + 1, vertStart, vertStart + 3, vertStart + 2 });
        uvs.AddRange(new Vector2[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
    }
}
