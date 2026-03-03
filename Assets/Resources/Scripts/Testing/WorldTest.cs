using UnityEngine;

public class WorldTest : MonoBehaviour
{
    public BlockMaterials graniteMaterial;
    public BlockMaterials waterMaterial;    // Assign a water BlockMaterials asset in the Inspector
    public int chunkRadius = 2;             // chunks generated in each direction

    [Tooltip("World-space Y level at or below which fluid is seeded into air gaps.")]
    public int seaLevel = 20;

    // FluidSimulator lives as a sibling or child component; assign in Inspector.
    public FluidSimulator fluidSimulator;

    private void Start()
    {
        for (int x = -chunkRadius; x <= chunkRadius; x++)
        {
            for (int z = -chunkRadius; z <= chunkRadius; z++)
            {
                Vector3Int vec = new Vector3Int(x, 0, z);
                WorldGeneration worldGen = FindObjectOfType<WorldGeneration>();
                Chunk c = worldGen.GenerateChunk(vec);

                // Register the chunk with the fluid simulator so it can read/write
                // fluid data and manage the transparent fluid mesh for this chunk.
                if (fluidSimulator != null)
                    fluidSimulator.RegisterChunk(c);
            }
        }

        // Seed an initial body of water: fill all air blocks at or below sea level
        // with water so the world starts with a lake/ocean.
        if (fluidSimulator != null && waterMaterial != null)
            SeedSeaLevel();
    }

    // Fill every air voxel at y <= seaLevel with one full fluid unit.
    private void SeedSeaLevel()
    {
        int cs = Chunk.chunkSize;
        for (int cx = -chunkRadius; cx <= chunkRadius; cx++)
        for (int cz = -chunkRadius; cz <= chunkRadius; cz++)
        for (int lx = 0; lx < cs; lx++)
        for (int lz = 0; lz < cs; lz++)
        for (int y = seaLevel; y >= 0; y--)
        {
            int wx = cx * cs + lx;
            int wz = cz * cs + lz;
            fluidSimulator.AddFluid(new Vector3Int(wx, y, wz), waterMaterial, FluidSimulator.MaxLevel);
        }
    }
}