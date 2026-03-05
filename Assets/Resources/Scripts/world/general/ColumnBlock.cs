namespace Resources.Scripts
{
    /// <summary>
    /// Describes a single block produced by a feature generator: world Y + material key.
    /// The chunk builder is responsible for converting worldY to chunk-relative coordinates.
    /// </summary>
    public struct ColumnBlock
    {
        public int    WorldY;
        public string MaterialKey;

        public ColumnBlock(int worldY, string materialKey)
        {
            WorldY      = worldY;
            MaterialKey = materialKey;
        }
    }
}
