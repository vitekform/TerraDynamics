using System.Collections.Generic;

namespace Resources.Scripts
{
    /// <summary>
    /// Shared logic for all biome feature generators.
    ///
    /// Column layout (top → bottom):
    ///   [sea level … surfaceY+1]  water fill  (only when surfaceY &lt; SeaLevel)
    ///   [surfaceY]                surface layer(s) defined by subclass
    ///   [surfaceY-1 … last layer] sub-surface layer(s) defined by subclass
    ///   [last layer-1 … bedrock+1] stone fill
    ///   [BedrockY]                bedrock
    /// </summary>
    public abstract class BiomeFeatureGenerator : IBiomeFeatureGenerator
    {
        /// <summary>Sea level world Y. Blocks below this are filled with water when submerged.</summary>
        public const int SeaLevel = 0;

        // ── Material keys ─────────────────────────────────────────────────────
        protected const string KeyBedrock = "bedrock";
        protected const string KeyStone   = "stone";
        protected const string KeyWater   = "water";

        // ── Surface layer descriptor ──────────────────────────────────────────

        /// <summary>One layer in the surface stack: material key + how many blocks thick.</summary>
        protected struct SurfaceLayer
        {
            public string MaterialKey;
            public int    Depth;
            public SurfaceLayer(string materialKey, int depth) { MaterialKey = materialKey; Depth = depth; }
        }

        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the ordered surface layers for this biome (top → down).
        /// Called once per column; may inspect surfaceY to vary layers (e.g. river banks vs bed).
        /// </summary>
        protected abstract SurfaceLayer[] GetSurfaceLayers(int worldX, int worldZ, int surfaceY, int seed);

        // ─────────────────────────────────────────────────────────────────────

        public ColumnBlock[] GenerateColumn(int worldX, int worldZ, int surfaceY, int seed, int minY, int maxY)
        {
            var blocks = new List<ColumnBlock>();

            SurfaceLayer[] layers = GetSurfaceLayers(worldX, worldZ, surfaceY, seed);
            int totalLayerDepth = 0;
            foreach (var l in layers) totalLayerDepth += l.Depth;

            int stoneTop = surfaceY - totalLayerDepth;

            // Iterate only the Y slice this chunk actually needs.
            for (int y = minY; y <= maxY; y++)
            {
                if (y == TerrainGenerator.BedrockY)
                {
                    blocks.Add(new ColumnBlock(y, KeyBedrock));
                }
                else if (y < stoneTop)
                {
                    blocks.Add(new ColumnBlock(y, KeyStone));
                }
                else if (y < surfaceY)
                {
                    // Determine which surface layer this Y falls into.
                    int depth = surfaceY - y; // 1 = just below surface, increases downward
                    int accumulated = 0;
                    string layerKey = KeyStone;
                    foreach (var layer in layers)
                    {
                        accumulated += layer.Depth;
                        if (depth <= accumulated) { layerKey = layer.MaterialKey; break; }
                    }
                    blocks.Add(new ColumnBlock(y, layerKey));
                }
                else if (y == surfaceY)
                {
                    // Top surface layer.
                    blocks.Add(new ColumnBlock(y, layers.Length > 0 ? layers[0].MaterialKey : KeyStone));
                }
                else if (y <= SeaLevel)
                {
                    // Above terrain surface but at or below sea level → water.
                    blocks.Add(new ColumnBlock(y, KeyWater));
                }
                // Above sea level and above surface → air, emit nothing.
            }

            return blocks.ToArray();
        }
    }
}
