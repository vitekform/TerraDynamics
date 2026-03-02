using UnityEngine;
using System.Collections.Generic;

public static class WorldGeneration
{
    public static int seed = 12345;
    public static float scale = 40f;
    public static int maxHeight = 60;

    // Call this to generate a chunk: fills blocks and builds mesh
    public static void GenerateChunk(Chunk chunk, BlockMaterials material)
    {
        int chunkSize = Chunk.chunkSize;
        int chunkHeight = Chunk.chunkHeight;

        // Initialize blocks using Perlin noise
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                int worldX = chunk.position.x * chunkSize + x;
                int worldZ = chunk.position.z * chunkSize + z;

                float noise = Mathf.PerlinNoise((worldX + seed) / scale, (worldZ + seed) / scale);
                int height = Mathf.FloorToInt(noise * maxHeight);
                height = Mathf.Clamp(height, 0, chunkHeight - 1);

                for (int y = 0; y <= height; y++)
                {
                    chunk.blocks[x, y, z] = new Block();
                    chunk.blocks[x, y, z].materials = material;
                }
            }
        }

        // Build the mesh for this chunk
        BuildMesh(chunk);

        chunk.isGenerated = true;
    }

    // Build mesh from blocks
    public static void BuildMesh(Chunk chunk)
    {
        int chunkSize = Chunk.chunkSize;
        int chunkHeight = Chunk.chunkHeight;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // Loop through blocks
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    Block block = chunk.blocks[x, y, z];
                    if (block.materials == null) continue;

                    Vector3 blockPos = new Vector3(x, y, z);

                    // Add each face if neighbor is empty/out of bounds
                    AddFace(vertices, triangles, uvs, blockPos, chunk.blocks, x, y + 1, z, Face.Top);
                    AddFace(vertices, triangles, uvs, blockPos, chunk.blocks, x, y - 1, z, Face.Bottom);
                    AddFace(vertices, triangles, uvs, blockPos, chunk.blocks, x + 1, y, z, Face.Right);
                    AddFace(vertices, triangles, uvs, blockPos, chunk.blocks, x - 1, y, z, Face.Left);
                    AddFace(vertices, triangles, uvs, blockPos, chunk.blocks, x, y, z + 1, Face.Front);
                    AddFace(vertices, triangles, uvs, blockPos, chunk.blocks, x, y, z - 1, Face.Back);
                }
            }
        }

        // Create GameObject for chunk
        GameObject chunkObj = new GameObject($"Chunk_{chunk.position.x}_{chunk.position.z}");
        chunkObj.transform.position = new Vector3(
            chunk.position.x * chunkSize,
            chunk.position.y * chunkHeight,
            chunk.position.z * chunkSize
        );

        MeshFilter mf = chunkObj.AddComponent<MeshFilter>();
        MeshRenderer mr = chunkObj.AddComponent<MeshRenderer>();
        mr.material = chunk.blocks[0, 0, 0].materials.material[0]; // all faces same material

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mf.mesh = mesh;
    }

    enum Face { Top, Bottom, Left, Right, Front, Back }

    // Adds a single face if neighbor is empty/out of bounds
    static void AddFace(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, Vector3 pos, Block[,,] blocks, int nx, int ny, int nz, Face face)
    {
        int chunkSizeX = blocks.GetLength(0);
        int chunkHeight = blocks.GetLength(1);
        int chunkSizeZ = blocks.GetLength(2);

        // Neighbor out of bounds = render face
        bool renderFace = true;
        if (nx >= 0 && nx < chunkSizeX && ny >= 0 && ny < chunkHeight && nz >= 0 && nz < chunkSizeZ)
        {
            if (blocks[nx, ny, nz].materials != null) renderFace = false; // neighbor exists - skip
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

                triangles.AddRange(new int[] { vertStart, vertStart + 2, vertStart + 1, vertStart, vertStart + 3, vertStart + 2 });
                break;

            case Face.Bottom:
                vertices.Add(pos + new Vector3(0, 0, 0));
                vertices.Add(pos + new Vector3(1, 0, 0));
                vertices.Add(pos + new Vector3(1, 0, 1));
                vertices.Add(pos + new Vector3(0, 0, 1));

                triangles.AddRange(new int[] { vertStart, vertStart + 1, vertStart + 2, vertStart, vertStart + 2, vertStart + 3 });
                break;

            case Face.Left:
                vertices.Add(pos + new Vector3(0, 0, 0));
                vertices.Add(pos + new Vector3(0, 1, 0));
                vertices.Add(pos + new Vector3(0, 1, 1));
                vertices.Add(pos + new Vector3(0, 0, 1));

                triangles.AddRange(new int[] { vertStart, vertStart + 2, vertStart + 1, vertStart, vertStart + 3, vertStart + 2 });
                break;

            case Face.Right:
                vertices.Add(pos + new Vector3(1, 0, 0));
                vertices.Add(pos + new Vector3(1, 1, 0));
                vertices.Add(pos + new Vector3(1, 1, 1));
                vertices.Add(pos + new Vector3(1, 0, 1));

                triangles.AddRange(new int[] { vertStart, vertStart + 1, vertStart + 2, vertStart, vertStart + 2, vertStart + 3 });
                break;

            case Face.Front:
                vertices.Add(pos + new Vector3(0, 0, 1));
                vertices.Add(pos + new Vector3(0, 1, 1));
                vertices.Add(pos + new Vector3(1, 1, 1));
                vertices.Add(pos + new Vector3(1, 0, 1));

                triangles.AddRange(new int[] { vertStart, vertStart + 2, vertStart + 1, vertStart, vertStart + 3, vertStart + 2 });
                break;

            case Face.Back:
                vertices.Add(pos + new Vector3(0, 0, 0));
                vertices.Add(pos + new Vector3(1, 0, 0));
                vertices.Add(pos + new Vector3(1, 1, 0));
                vertices.Add(pos + new Vector3(0, 1, 0));

                triangles.AddRange(new int[] { vertStart, vertStart + 2, vertStart + 1, vertStart, vertStart + 3, vertStart + 2 });
                break;
        }

        // UVs: simple square for now
        uvs.AddRange(new Vector2[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
    }
}