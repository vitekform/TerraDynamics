using UnityEngine;

/// <summary>
/// Scatters ore veins into existing rock blocks based on depth range and host-rock rules.
/// Each ore type uses an independently seeded 3-D noise field so their distributions
/// don't correlate.
/// </summary>
public static class OreGenerator
{
    // ── Vein descriptor ──────────────────────────────────────────────────────

    private struct VeinProfile
    {
        public string   oreName;          // key for WorldBlockPalette.GetOreByName()
        public int      minY, maxY;       // world-Y depth range
        public float    noiseThreshold;   // noise ≥ this → place ore (higher = rarer)
        public float    noiseScale;       // 3-D noise frequency (smaller = larger veins)
        public string[] validHostRocks;   // BlockMaterials.materialName values allowed
    }

    // ── Ore table ────────────────────────────────────────────────────────────
    // Depths and host rocks based on real-world geology (simplified).

    private static readonly VeinProfile[] Profiles =
    {
        new VeinProfile
        {
            oreName = "Coal",    minY = -200, maxY =  50,
            noiseThreshold = 0.72f, noiseScale = 0.08f,
            validHostRocks = new[] { "Limestone", "Shale", "Stone" }
        },
        new VeinProfile
        {
            oreName = "Iron",    minY = -500, maxY =   0,
            noiseThreshold = 0.74f, noiseScale = 0.07f,
            validHostRocks = new[] { "Basalt", "Granite", "Stone" }
        },
        new VeinProfile
        {
            oreName = "Copper",  minY = -400, maxY = 100,
            noiseThreshold = 0.76f, noiseScale = 0.07f,
            validHostRocks = new[] { "Granite", "Stone" }
        },
        new VeinProfile
        {
            oreName = "Gold",    minY = -800, maxY = -200,
            noiseThreshold = 0.85f, noiseScale = 0.05f,
            validHostRocks = new[] { "Granite", "Stone" }
        },
        new VeinProfile
        {
            oreName = "Silver",  minY = -700, maxY = -100,
            noiseThreshold = 0.84f, noiseScale = 0.05f,
            validHostRocks = new[] { "Granite", "Stone" }
        },
        new VeinProfile
        {
            oreName = "Diamond", minY = -1000, maxY = -500,
            noiseThreshold = 0.92f, noiseScale = 0.04f,
            validHostRocks = new[] { "Basalt", "Stone" }
        },
        new VeinProfile
        {
            oreName = "Sulfur",  minY = -300, maxY = 100,
            noiseThreshold = 0.80f, noiseScale = 0.06f,
            validHostRocks = new[] { "Basalt", "Granite" }
        },
    };

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Iterates every block in <paramref name="chunk"/> and replaces eligible
    /// rock blocks with ore where the noise threshold is met.
    /// </summary>
    public static void PlaceOres(Chunk chunk, WorldBlockPalette palette, int seed)
    {
        int cs = WorldSettings.ChunkSize;

        for (int oi = 0; oi < Profiles.Length; oi++)
        {
            VeinProfile p = Profiles[oi];

            // Quickly skip chunks entirely outside this ore's Y range
            int chunkWorldYMin = WorldSettings.WorldYFromChunk(chunk.position.y, 0);
            int chunkWorldYMax = WorldSettings.WorldYFromChunk(chunk.position.y, cs - 1);
            if (chunkWorldYMax < p.minY || chunkWorldYMin > p.maxY) continue;

            BlockMaterials oreMat = palette.GetOreByName(p.oreName);
            if (oreMat == null) continue;

            // Each ore type gets a unique seed offset so their maps don't overlap
            float seedOff = seed * 0.01f + oi * 17.3f;

            for (int lx = 0; lx < cs; lx++)
            for (int ly = 0; ly < cs; ly++)
            for (int lz = 0; lz < cs; lz++)
            {
                int wy = WorldSettings.WorldYFromChunk(chunk.position.y, ly);
                if (wy < p.minY || wy > p.maxY) continue;

                Block b = chunk.blocks[lx, ly, lz];
                if (b.materials == null) continue;                     // air/water – skip
                if (!IsValidHost(b.materials.materialName, p.validHostRocks)) continue;

                float wx = chunk.position.x * cs + lx;
                float wz = chunk.position.z * cs + lz;

                float n = (Noise3D.Sample(
                    wx * p.noiseScale + seedOff,
                    wy * p.noiseScale + seedOff,
                    wz * p.noiseScale + seedOff) + 1f) * 0.5f;       // remap to [0,1]

                if (n >= p.noiseThreshold)
                    chunk.blocks[lx, ly, lz].materials = oreMat;
            }
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static bool IsValidHost(string matName, string[] valid)
    {
        if (string.IsNullOrEmpty(matName)) return false;
        for (int i = 0; i < valid.Length; i++)
            if (matName == valid[i]) return true;
        return false;
    }
}
