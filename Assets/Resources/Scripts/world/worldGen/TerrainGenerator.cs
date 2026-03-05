using UnityEngine;

namespace Resources.Scripts
{
    /// <summary>
    /// Generates terrain height and biome data using layered Perlin noise.
    ///
    /// Three independent noise maps drive world generation:
    ///   - Height map   : very low frequency → extremely smooth rolling terrain
    ///   - Temperature  : medium frequency
    ///   - Humidity     : medium frequency
    ///
    /// Bedrock sits at Y = <see cref="BedrockY"/> (-1000).
    /// The actual surface Y is determined per-biome from the height-noise value.
    /// </summary>
    public static class TerrainGenerator
    {
        public const int BedrockY = -1000;

        // ── Noise scales ────────────────────────────────────────────────
        // Very small values → very large features → very smooth terrain.
        // Height uses a single dominant low-frequency octave + one faint detail
        // octave so the surface never gets sharp peaks.

        private const float HeightScaleBase   = 0.0015f; // primary smooth wave
        private const float HeightScaleDetail = 0.006f;  // faint secondary detail
        private const float HeightDetailBlend = 0.08f;   // weight of detail octave

        private const float TemperatureScale  = 0.004f;
        private const float HumidityScale     = 0.004f;

        // ── Offsets applied per seed to decorrelate each noise map ──────
        private const float TempOffset     = 10000f;
        private const float HumidityOffset = 20000f;

        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Samples a very smooth terrain surface Y at (worldX, worldZ).
        /// The biome's MinSurfaceY/MaxSurfaceY range is used to map the 0–1
        /// noise value into world space.
        /// </summary>
        public static int SampleTerrainHeight(float worldX, float worldZ, int seed, Biome biome)
        {
            float seedOffset = seed * 0.1f;

            float nx = worldX + seedOffset;
            float nz = worldZ + seedOffset;

            // Two-octave smooth height: base dominates, detail barely visible.
            float baseH   = Mathf.PerlinNoise(nx * HeightScaleBase,   nz * HeightScaleBase);
            float detailH = Mathf.PerlinNoise(nx * HeightScaleDetail, nz * HeightScaleDetail);
            float smoothH = Mathf.Lerp(baseH, detailH, HeightDetailBlend);

            // Map to the biome's Y surface range.
            return Mathf.RoundToInt(Mathf.Lerp(biome.MinSurfaceY, biome.MaxSurfaceY, smoothH));
        }

        /// <summary>
        /// Samples the height-noise value (0–1) at the given world position.
        /// Used for biome selection, not the actual terrain Y.
        /// </summary>
        public static float SampleHeightNoise(float worldX, float worldZ, int seed)
        {
            float o = seed * 0.1f;
            return Mathf.PerlinNoise((worldX + o) * HeightScaleBase, (worldZ + o) * HeightScaleBase);
        }

        /// <summary>Samples temperature noise (0–1) at the given world position.</summary>
        public static float SampleTemperature(float worldX, float worldZ, int seed)
        {
            float o = seed * 0.1f + TempOffset;
            return Mathf.PerlinNoise((worldX + o) * TemperatureScale, (worldZ + o) * TemperatureScale);
        }

        /// <summary>Samples humidity noise (0–1) at the given world position.</summary>
        public static float SampleHumidity(float worldX, float worldZ, int seed)
        {
            float o = seed * 0.1f + HumidityOffset;
            return Mathf.PerlinNoise((worldX + o) * HumidityScale, (worldZ + o) * HumidityScale);
        }

        /// <summary>
        /// Returns the biome at (worldX, worldZ) by sampling all three noise maps
        /// and looking up the best match in the registry.
        /// Falls back to <see cref="BiomeRegistry.Plains"/> when no biome matches.
        /// </summary>
        public static Biome SampleBiome(float worldX, float worldZ, int seed, BiomeRegistry registry)
        {
            float h = SampleHeightNoise(worldX, worldZ, seed);
            float t = SampleTemperature(worldX, worldZ, seed);
            float u = SampleHumidity(worldX, worldZ, seed);

            return registry.Get(h, t, u) ?? BiomeRegistry.Plains;
        }

        /// <summary>
        /// Convenience: returns both the biome and the terrain surface Y in one call.
        /// </summary>
        public static (Biome biome, int surfaceY) Sample(
            float worldX, float worldZ, int seed, BiomeRegistry registry)
        {
            Biome biome = SampleBiome(worldX, worldZ, seed, registry);
            int   y     = SampleTerrainHeight(worldX, worldZ, seed, biome);
            return (biome, y);
        }
    }
}
