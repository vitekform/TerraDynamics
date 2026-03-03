using UnityEngine;

/// <summary>
/// Carves 3-D cave systems into already-filled terrain blocks.
/// Uses a combination of two independent fBm noise fields (large chambers +
/// narrow tunnels) for natural-looking results.
///
/// Rules:
///   • No carving at or above the surface heightmap (no floating ceilings).
///   • Lava fills carved air below <see cref="LavaFloorY"/>.
///   • Underground rivers fill carved blocks in specific depth bands with water.
///   • All other carved blocks become air, with a cave-moisture value stored
///     in <c>Block.chemicalContamination</c> for later simulation systems.
/// </summary>
public static class CaveGenerator
{
    // ── Noise thresholds ─────────────────────────────────────────────────────
    private const float CaveScale      = 0.04f;
    private const float CaveThreshold  = 0.55f;   // large chamber noise threshold

    private const float TunnelScale     = 0.06f;
    private const float TunnelThreshold = 0.65f;   // narrow tunnel noise threshold

    // ── Special fills ────────────────────────────────────────────────────────
    private const int LavaFloorY = -800;   // below this Y, carved air → lava

    // Underground river depth bands [minY, maxY] filled with water
    private static readonly (int min, int max)[] RiverBands =
    {
        (-300, -250),
        (-100,  -60),
        ( -20,   -5),
    };

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Carves caves into <paramref name="chunk"/>. Must be called after terrain fill.
    /// </summary>
    /// <param name="surfaceHeights">
    /// [localX, localZ] array of surface world-Y values for this chunk's columns.
    /// Blocks at or above their surface Y are never carved.
    /// </param>
    public static void CarveCaves(Chunk chunk, int[,] surfaceHeights,
                                   WorldBlockPalette palette, int seed, BiomeType biome)
    {
        int   cs           = WorldSettings.ChunkSize;
        float seedOff      = seed * 0.011f;
        float caveMoisture = BiomeSystem.CaveMoistureModifier(biome);

        for (int lx = 0; lx < cs; lx++)
        for (int ly = 0; ly < cs; ly++)
        for (int lz = 0; lz < cs; lz++)
        {
            int wy = WorldSettings.WorldYFromChunk(chunk.position.y, ly);

            // Never carve at or above surface
            if (wy >= surfaceHeights[lx, lz]) continue;

            Block b = chunk.blocks[lx, ly, lz];
            if (b.materials == null) continue;   // already air / fluid

            float wx = chunk.position.x * cs + lx;
            float wz = chunk.position.z * cs + lz;

            float nx = wx * CaveScale  + seedOff;
            float ny = wy * CaveScale  + seedOff;
            float nz = wz * CaveScale  + seedOff;

            // Large chamber noise
            float cave   = (Noise3D.FBm(nx, ny, nz, 4, 0.5f, 2.0f) + 1f) * 0.5f;

            // Narrow tunnel noise (offset to decorrelate from chamber)
            float tunnel = (Noise3D.FBm(nx * 1.5f + 3.7f, ny * 1.5f,
                                        nz * 1.5f,         3, 0.5f, 2.1f) + 1f) * 0.5f;

            if (cave < CaveThreshold && tunnel < TunnelThreshold) continue;

            // ── Lava fill at depth ────────────────────────────────────────
            if (wy < LavaFloorY)
            {
                if (palette.lava != null)
                    chunk.blocks[lx, ly, lz] = new Block { materials = palette.lava, temperature = 1473f };
                continue;
            }

            // ── Underground river fill ────────────────────────────────────
            bool isRiver = false;
            for (int r = 0; r < RiverBands.Length; r++)
            {
                if (wy >= RiverBands[r].min && wy <= RiverBands[r].max)
                { isRiver = true; break; }
            }

            if (isRiver && palette.water != null)
            {
                chunk.blocks[lx, ly, lz] = new Block
                {
                    materials  = palette.water,
                    fluidLevel = FluidSimulator.MaxLevel,
                    state      = MatterState.Liquid,
                };
                continue;
            }

            // ── Air block with cave-moisture marker ───────────────────────
            chunk.blocks[lx, ly, lz] = new Block
            {
                materials            = null,
                chemicalContamination = caveMoisture
            };
        }
    }
}
