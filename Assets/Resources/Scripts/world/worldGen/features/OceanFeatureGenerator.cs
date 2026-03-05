namespace Resources.Scripts
{
    /// <summary>
    /// Ocean column: sand/gravel seafloor, with water filling up to sea level.
    ///
    /// Layer stack (top → bottom):
    ///   2 × sand   (ocean floor surface)
    ///   2 × gravel (transition)
    ///   stone below
    /// </summary>
    public class OceanFeatureGenerator : BiomeFeatureGenerator
    {
        protected override SurfaceLayer[] GetSurfaceLayers(int worldX, int worldZ, int surfaceY, int seed)
        {
            return new[]
            {
                new SurfaceLayer("sand",   2),
                new SurfaceLayer("gravel", 2),
            };
        }
    }
}
