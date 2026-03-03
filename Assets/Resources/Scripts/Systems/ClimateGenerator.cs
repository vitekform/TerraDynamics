using UnityEngine;

/// <summary>
/// Generates base annual temperature (°C) and moisture (0–1) for any world column.
/// These are static map values; SeasonSystem provides a runtime delta on top.
/// </summary>
public static class ClimateGenerator
{
    // ── Temperature constants ────────────────────────────────────────────────
    private const float EquatorTemp    = 32f;     // °C at sea level at the equator
    private const float PoleTemp       = -40f;    // °C at the poles
    private const float LapseRate      = 0.0065f; // °C per metre elevation above sea level
    private const float TempNoiseScale = 0.0015f;
    private const float TempNoiseAmp   = 8f;      // ±°C local noise variation

    // ── Moisture constants ───────────────────────────────────────────────────
    private const float MoistureScale      = 0.0018f;
    private const float CoastalMoistBoost  = 0.20f;
    private const float CoastalBoostRadius = 200f;  // blocks from sea level surface
    private const float RainShadowStart    = 800f;  // above this Y, reduce moisture
    private const float RainShadowStrength = 0.6f;

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the base annual temperature in °C for a column.
    /// Accounts for latitude (equator→pole), elevation lapse rate, and noise variation.
    /// </summary>
    public static float GetTemperature(int worldX, int worldZ, int surfaceY, int seed)
    {
        // Latitude: quadratic falloff so mid-latitudes aren't too cold
        float lat = WorldSettings.LatitudeFactor(worldZ);
        float baseTemp = Mathf.Lerp(EquatorTemp, PoleTemp, lat * lat);

        // Elevation penalty above sea level (real lapse rate)
        float elevation = Mathf.Max(0f, surfaceY - WorldSettings.SeaLevel);
        baseTemp -= elevation * LapseRate;

        // Noise perturbation – simulates ocean currents and microclimates
        float s = seed * 0.007f;
        float noise = Mathf.PerlinNoise(worldX * TempNoiseScale + s,
                                        worldZ * TempNoiseScale + s + 3.1f) * 2f - 1f;
        baseTemp += noise * TempNoiseAmp;

        return baseTemp;
    }

    /// <summary>
    /// Returns moisture as a 0–1 value for a column.
    /// 0 = completely arid, 1 = very wet (tropical rainforest / coast).
    /// </summary>
    public static float GetMoisture(int worldX, int worldZ, int surfaceY, int seed)
    {
        float s = seed * 0.009f + 100f;
        float moisture = Mathf.PerlinNoise(worldX * MoistureScale + s,
                                           worldZ * MoistureScale + s);

        // Coastal boost: columns near sea level get extra moisture
        float seaProximity = 1f - Mathf.Clamp01(Mathf.Abs(surfaceY - WorldSettings.SeaLevel) / CoastalBoostRadius);
        moisture = Mathf.Clamp01(moisture + seaProximity * CoastalMoistBoost);

        // Rain shadow: high terrain blocks moisture on the leeward side
        if (surfaceY > RainShadowStart)
        {
            float shadowFactor = Mathf.Clamp01((surfaceY - RainShadowStart) / 600f);
            moisture *= 1f - shadowFactor * RainShadowStrength;
        }

        return Mathf.Clamp01(moisture);
    }
}
