namespace Resources.Scripts
{
    /// <summary>
    /// Generates the block column for a single (worldX, worldZ) position within a biome.
    /// Only blocks whose worldY falls within [minY, maxY] are returned — callers pass the
    /// chunk's Y band so we never allocate blocks that will be thrown away immediately.
    /// </summary>
    public interface IBiomeFeatureGenerator
    {
        /// <summary>
        /// Produces the block column slice for the given world position and Y range.
        /// </summary>
        /// <param name="worldX">World X coordinate of the column.</param>
        /// <param name="worldZ">World Z coordinate of the column.</param>
        /// <param name="surfaceY">Terrain surface Y for this column (from TerrainGenerator).</param>
        /// <param name="seed">World seed for any per-column randomisation.</param>
        /// <param name="minY">Lowest world Y to generate (inclusive) — chunk origin Y.</param>
        /// <param name="maxY">Highest world Y to generate (inclusive) — chunk origin Y + 31.</param>
        ColumnBlock[] GenerateColumn(int worldX, int worldZ, int surfaceY, int seed, int minY, int maxY);
    }
}
