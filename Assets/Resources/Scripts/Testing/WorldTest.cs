using UnityEngine;

public class WorldTest : MonoBehaviour
{
    public BlockMaterials graniteMaterial;
    public int chunkRadius = 2; // number of chunks to generate in each direction

    private void Start()
    {
        for (int x = -chunkRadius; x <= chunkRadius; x++)
        {
            for (int z = -chunkRadius; z <= chunkRadius; z++)
            {
                Chunk chunk = new Chunk(new Vector3Int(x, 0, z));
                WorldGeneration.GenerateChunk(chunk, graniteMaterial);
                //GenerateVisuals(chunk);
            }
        }
    }

    /*void GenerateVisuals(Chunk chunk)
    {
        int chunkSize = Chunk.chunkSize;
        int chunkHeight = Chunk.chunkHeight;

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    if (chunk.blocks[x, y, z].materials != null)
                    {
                        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        Vector3 worldPos = new Vector3(
                            chunk.position.x * chunkSize + x,
                            chunk.position.y * chunkHeight + y,
                            chunk.position.z * chunkSize + z
                        );
                        cube.transform.position = worldPos;
                    }
                }
            }
        }
    }*/
}