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

        public ColumnBlock[] GenerateColumn(int worldX, int worldZ, int surfaceY, int seed)
        {
            var blocks = new List<ColumnBlock>();

            // 1. Bedrock at the absolute floor.
            blocks.Add(new ColumnBlock(TerrainGenerator.BedrockY, KeyBedrock));

            // 2. Stone fill from just above bedrock up to where surface layers begin.
            SurfaceLayer[] layers = GetSurfaceLayers(worldX, worldZ, surfaceY, seed);
            int totalLayerDepth = 0;
            foreach (var l in layers) totalLayerDepth += l.Depth;

            int stoneTop = surfaceY - totalLayerDepth; // exclusive upper bound of stone
            for (int y = TerrainGenerator.BedrockY + 1; y < stoneTop; y++)
                blocks.Add(new ColumnBlock(y, KeyStone));

            // 3. Surface layers (top layer last in the stack = closest to surfaceY).
            int layerY = surfaceY - totalLayerDepth;
            foreach (var layer in layers)
            {
                for (int d = 0; d < layer.Depth; d++)
                    blocks.Add(new ColumnBlock(layerY++, layer.MaterialKey));
            }

            // 4. Water fill above surface up to sea level (when submerged).
            if (surfaceY < SeaLevel)
            {
                for (int y = surfaceY + 1; y <= SeaLevel; y++)
                    blocks.Add(new ColumnBlock(y, KeyWater));
            }

            return blocks.ToArray();
        }
    }
}
