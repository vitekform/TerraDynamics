namespace Resources.Scripts
{
    /// <summary>
    /// River column generator.
    ///
    /// Channel (surfaceY &lt; sea level):
    ///   water fill up to sea level (handled by base)
    ///   2 × sand riverbed
    ///   3 × dirt
    ///   stone below
    ///
    /// Bank (surfaceY &gt;= sea level):
    ///   1 × grass
    ///   3 × dirt
    ///   stone below
    /// </summary>
    public class RiverFeatureGenerator : BiomeFeatureGenerator
    {
        protected override SurfaceLayer[] GetSurfaceLayers(int worldX, int worldZ, int surfaceY, int seed)
        {
            bool isChannel = surfaceY < SeaLevel;

            if (isChannel)
            {
                return new[]
                {
                    new SurfaceLayer("sand", 2),
                    new SurfaceLayer("dirt", 3),
                };
            }
            else
            {
                return new[]
                {
                    new SurfaceLayer("grass", 1),
                    new SurfaceLayer("dirt",  3),
                };
            }
        }
    }
}
