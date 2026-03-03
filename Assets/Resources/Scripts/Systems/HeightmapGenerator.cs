using UnityEngine;

/// <summary>
/// Generates a 2D surface-height map using fractal Brownian motion,
/// domain warping, and ridge noise for mountain ranges.
/// X-axis is sampled on a cylinder to prevent seams at the east-west world wrap.
/// </summary>
public static class HeightmapGenerator
{
    // ── Noise tuning constants ───────────────────────────────────────────────
    private const float BaseScale    = 0.0008f;   // continental shape frequency
    private const float RidgeScale   = 0.002f;    // mountain ridge frequency
    private const float DetailScale  = 0.004f;    // local terrain detail
    private const float WarpStrength = 80f;       // domain-warp displacement (blocks)

    // ── Height mapping bounds ────────────────────────────────────────────────
    private const float OceanFloor  = -800f;      // deepest ocean trench
    private const float MountainTop = 1800f;      // tallest mountain peak

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the surface world-Y for a single (worldX, worldZ) column.
    /// </summary>
    public static int GetSurfaceY(int worldX, int worldZ, int seed)
    {
        // Project X onto a cylinder so east-west wrap has no seam
        float angle = (worldX / (float)WorldSettings.WorldWidth) * Mathf.PI * 2f;
        float radius = WorldSettings.WorldWidth / (Mathf.PI * 2f);
        float cx = Mathf.Cos(angle) * radius;
        float cz = Mathf.Sin(angle) * radius;   // second cylinder axis for X
        float wz = worldZ;

        float s = seed * 0.01f;

        // Domain warp: shift sample coordinates for organic, non-repeating shapes
        float dwX = Noise3D.FBm(cx * BaseScale + s,         wz * BaseScale + s,         1.7f, 4, 0.5f, 2.0f) * WarpStrength;
        float dwZ = Noise3D.FBm(cx * BaseScale + s + 5.2f,  wz * BaseScale + s + 1.3f,  1.7f, 4, 0.5f, 2.0f) * WarpStrength;

        float sampX = (cx + dwX) * BaseScale + s;
        float sampZ = (wz + dwZ) * BaseScale + s;

        // Base continent shape – 6-octave fBm
        float baseN = Noise3D.FBm(sampX, sampZ, 0.5f, 6, 0.5f, 2.0f);

        // Ridge noise adds sharp mountain ranges on top of elevated areas
        float ridge = RidgeNoise(cx * RidgeScale + s, wz * RidgeScale + s, seed);
        float ridgeBlend = Mathf.Clamp01((baseN + 0.2f) * 1.5f);  // only blend ridges onto land

        // Fine detail noise (small hills, cliffs)
        float detail = Noise3D.FBm(cx * DetailScale + s, wz * DetailScale + s, 2.1f, 4, 0.45f, 2.2f) * 0.15f;

        float combined = Mathf.Clamp(baseN + ridge * ridgeBlend * 0.5f + detail, -1f, 1f);

        // Remap [−1, 1] → [OceanFloor, MountainTop]
        float surfaceY = Mathf.Lerp(OceanFloor, MountainTop, (combined + 1f) * 0.5f);

        return Mathf.RoundToInt(Mathf.Clamp(surfaceY, WorldSettings.WorldMinY, WorldSettings.WorldMaxY));
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Ridge noise: folds abs(Perlin) to create sharp crests.
    /// Returns a value in [−1, 1].
    /// </summary>
    private static float RidgeNoise(float x, float z, int seed)
    {
        float s = seed * 0.013f;
        float n = 0f, amp = 1f, freq = 1f, maxAmp = 0f;
        for (int i = 0; i < 4; i++)
        {
            float raw = Noise3D.Sample(x * freq + s, z * freq + s, 0.3f);
            n      += (1f - Mathf.Abs(raw)) * amp;
            maxAmp += amp;
            amp    *= 0.5f;
            freq   *= 2.2f;
        }
        return (n / maxAmp) * 2f - 1f;
    }
}
