namespace Resources.Scripts
{
    /// <summary>
    /// Plains column: grass on top, dirt underneath, stone below.
    ///
    /// Layer stack (top → bottom):
    ///   1 × grass
    ///   3 × dirt
    ///   stone below
    /// </summary>
    public class PlainsFeatureGenerator : BiomeFeatureGenerator
    {
        protected override SurfaceLayer[] GetSurfaceLayers(int worldX, int worldZ, int surfaceY, int seed)
        {
            return new[]
            {
                new SurfaceLayer("grass", 1),
                new SurfaceLayer("dirt",  3),
            };
        }
    }
}
