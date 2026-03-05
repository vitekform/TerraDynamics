namespace Resources.Scripts
{
    /// <summary>
    /// Generates the block column for a single (worldX, worldZ) position within a biome.
    /// Returns every block from bedrock up to the surface (or water surface if submerged).
    /// </summary>
    public interface IBiomeFeatureGenerator
    {
        /// <summary>
        /// Produces the full block column for the given world position.
        /// </summary>
        /// <param name="worldX">World X coordinate of the column.</param>
        /// <param name="worldZ">World Z coordinate of the column.</param>
        /// <param name="surfaceY">Terrain surface Y for this column (from TerrainGenerator).</param>
        /// <param name="seed">World seed for any per-column randomisation.</param>
        ColumnBlock[] GenerateColumn(int worldX, int worldZ, int surfaceY, int seed);
    }
}
