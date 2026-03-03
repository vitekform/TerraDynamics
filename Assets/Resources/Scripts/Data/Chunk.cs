using UnityEngine;

public class Chunk
{
    public Vector3Int position;             // chunk coordinates (x, y, z) in chunk-space
    public Block[,,] blocks;                // all blocks in this chunk
    public bool isGenerated = false;        // terrain generation flag

    /// <summary>The solid-mesh GameObject created by WorldGeneration.</summary>
    [System.NonSerialized] public GameObject chunkObject;
    /// <summary>Set to true by FluidSimulator when fluid has changed and the mesh needs rebuild.</summary>
    [System.NonSerialized] public bool isFluidDirty = false;

    /// <summary>
    /// Side length of a chunk in blocks on every axis (32 × 32 × 32).
    /// Use <see cref="WorldSettings.ChunkSize"/> where possible; this alias
    /// is kept for code that was written before WorldSettings existed.
    /// </summary>
    public const int chunkSize = WorldSettings.ChunkSize;

    // Constructor
    public Chunk(Vector3Int pos)
    {
        position = pos;
        blocks = new Block[chunkSize, chunkSize, chunkSize];
    }

    // Example: initialise all blocks to a single material (useful for testing)
    public void InitializeBlocks(BlockMaterials graniteMaterial)
    {
        for (int x = 0; x < chunkSize; x++)
        for (int y = 0; y < chunkSize; y++)
        for (int z = 0; z < chunkSize; z++)
        {
            blocks[x, y, z] = new Block
            {
                materials   = graniteMaterial,
                temperature = 293f,
                stress      = 0f,
                damage      = 0f,
                fluidLevel  = 0,
            };
        }
        isGenerated = true;
    }
}
