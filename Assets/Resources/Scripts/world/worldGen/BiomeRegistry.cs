using System.Collections.Generic;

namespace Resources.Scripts
{
    /// <summary>
    /// Registry of all known biomes. Ships with three defaults: ocean, river, plains.
    /// Biomes are evaluated in registration order; the first match wins.
    /// </summary>
    public class BiomeRegistry
    {
        private readonly List<Biome> _biomes = new List<Biome>();

        // ---------------------------------------------------------------
        // Default biome definitions
        //
        //  Height noise bands (normalized 0–1):
        //    < 0.38  → ocean territory
        //    0.35–0.42 (narrow, high humidity) → river
        //    ≥ 0.40  → land (plains, etc.)
        //
        //  Surface Y mapping is done by TerrainGenerator using MinSurfaceY/MaxSurfaceY.
        //  Bedrock sits at Y = -1000; ocean floor never needs to reach it.
        // ---------------------------------------------------------------

        public static readonly Biome Ocean = new Biome(
            name:           "ocean",
            minHeight:      0.00f, maxHeight:      0.38f,
            minTemperature: 0.00f, maxTemperature: 1.00f,
            minHumidity:    0.00f, maxHumidity:    1.00f,
            minSurfaceY:    -300,  maxSurfaceY:    -10,
            featureGenerator: new OceanFeatureGenerator()
        );

        /// <summary>
        /// Rivers occupy a narrow height band at the land/ocean transition
        /// with high humidity, carving shallow valleys across the surface.
        /// </summary>
        public static readonly Biome River = new Biome(
            name:           "river",
            minHeight:      0.35f, maxHeight:      0.42f,
            minTemperature: 0.00f, maxTemperature: 1.00f,
            minHumidity:    0.65f, maxHumidity:    1.00f,
            minSurfaceY:    -20,   maxSurfaceY:    5,
            featureGenerator: new RiverFeatureGenerator()
        );

        public static readonly Biome Plains = new Biome(
            name:           "plains",
            minHeight:      0.40f, maxHeight:      1.00f,
            minTemperature: 0.00f, maxTemperature: 1.00f,
            minHumidity:    0.00f, maxHumidity:    1.00f,
            minSurfaceY:    2,     maxSurfaceY:    80,
            featureGenerator: new PlainsFeatureGenerator()
        );

        public BiomeRegistry(bool registerDefaults = true)
        {
            if (registerDefaults)
            {
                // Order matters: more specific biomes (river) before broader ones (plains).
                Register(River);
                Register(Ocean);
                Register(Plains);
            }
        }

        /// <summary>Adds a biome. Evaluated before all previously registered biomes.</summary>
        public void Register(Biome biome)
        {
            _biomes.Insert(0, biome);
        }

        /// <summary>
        /// Returns the first biome whose thresholds match the supplied noise values,
        /// or <c>null</c> if none match.
        /// </summary>
        public Biome Get(float height, float temperature, float humidity)
        {
            foreach (var biome in _biomes)
                if (biome.Matches(height, temperature, humidity))
                    return biome;
            return null;
        }

        /// <summary>Returns a biome by name, or <c>null</c> if not found.</summary>
        public Biome GetByName(string name)
        {
            foreach (var biome in _biomes)
                if (biome.Name == name)
                    return biome;
            return null;
        }

        public bool Delete(string name)
        {
            return _biomes.RemoveAll(b => b.Name == name) > 0;
        }
    }
}
