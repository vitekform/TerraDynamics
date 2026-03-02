using UnityEngine;

public class Chunk
{
    public Vector3Int position;             // chunk coordinates
    public Block[,,] blocks;                // all blocks in this chunk
    public bool isGenerated = false;        // terrain generation flag

    public const int chunkSize = 32;
    public const int chunkHeight = 128;

    // Constructor
    public Chunk(Vector3Int pos)
    {
        position = pos;
        blocks = new Block[chunkSize, chunkHeight, chunkSize];
    }

    // Example: initialize all blocks to granite
    public void InitializeBlocks(BlockMaterials graniteMaterial)
    {
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    blocks[x, y, z] = new Block();
                    blocks[x, y, z].materials = graniteMaterial;
                    blocks[x, y, z].temperature = 293f;      // default temp
                    blocks[x, y, z].stress = 0f;             // initial stress
                    blocks[x, y, z].damage = 0f;             // no damage
                    blocks[x, y, z].fluidLevel = 16;         // full block by default
                    // ... initialize other dynamic values as needed
                }
            }
        }
        isGenerated = true;
    }
}
