namespace Resources.Scripts
{
    /// <summary>
    /// Defines a biome by its name, normalized noise thresholds (0–1) for selection,
    /// and the world-space Y surface range used to map terrain height within the biome.
    /// </summary>
    public class Biome
    {
        public string Name { get; set; }

        // --- Biome selection thresholds (normalized Perlin noise values 0–1) ---

        /// <summary>Minimum height-noise value for this biome to be selected.</summary>
        public float MinHeight { get; set; }
        /// <summary>Maximum height-noise value for this biome to be selected.</summary>
        public float MaxHeight { get; set; }

        /// <summary>Minimum temperature-noise value for this biome to be selected.</summary>
        public float MinTemperature { get; set; }
        /// <summary>Maximum temperature-noise value for this biome to be selected.</summary>
        public float MaxTemperature { get; set; }

        /// <summary>Minimum humidity-noise value for this biome to be selected.</summary>
        public float MinHumidity { get; set; }
        /// <summary>Maximum humidity-noise value for this biome to be selected.</summary>
        public float MaxHumidity { get; set; }

        // --- Terrain surface range within this biome (world Y) ---

        /// <summary>Lowest possible surface Y in this biome. Bedrock floor is at -1000.</summary>
        public int MinSurfaceY { get; set; }
        /// <summary>Highest possible surface Y in this biome.</summary>
        public int MaxSurfaceY { get; set; }

        /// <summary>Generates the block column for any position in this biome.</summary>
        public IBiomeFeatureGenerator FeatureGenerator { get; set; }

        public Biome(
            string name,
            float minHeight, float maxHeight,
            float minTemperature, float maxTemperature,
            float minHumidity, float maxHumidity,
            int minSurfaceY, int maxSurfaceY,
            IBiomeFeatureGenerator featureGenerator = null)
        {
            Name = name;
            MinHeight = minHeight;   MaxHeight = maxHeight;
            MinTemperature = minTemperature; MaxTemperature = maxTemperature;
            MinHumidity = minHumidity;       MaxHumidity = maxHumidity;
            MinSurfaceY = minSurfaceY;       MaxSurfaceY = maxSurfaceY;
            FeatureGenerator = featureGenerator;
        }

        /// <summary>
        /// Returns true if the given noise samples fall within this biome's thresholds.
        /// </summary>
        public bool Matches(float height, float temperature, float humidity)
        {
            return height      >= MinHeight      && height      < MaxHeight      &&
                   temperature >= MinTemperature && temperature < MaxTemperature &&
                   humidity    >= MinHumidity    && humidity    < MaxHumidity;
        }
    }
}
