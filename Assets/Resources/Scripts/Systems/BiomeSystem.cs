using UnityEngine;

/// <summary>
/// Classifies biomes from temperature and moisture and provides per-biome
/// block fill rules used during world generation.
/// Based on Whittaker biome classification.
/// </summary>
public static class BiomeSystem
{
    // ── Classification ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns the biome for a world column given its climate values and surface elevation.
    /// Columns below sea level are always Ocean or DeepOcean.
    /// </summary>
    /// <param name="temperature">Annual base temperature in °C.</param>
    /// <param name="moisture">Moisture 0–1 (0 = arid, 1 = very wet).</param>
    /// <param name="surfaceY">World-space Y of the surface block.</param>
    public static BiomeType Classify(float temperature, float moisture, int surfaceY)
    {
        if (surfaceY < WorldSettings.SeaLevel - 100) return BiomeType.DeepOcean;
        if (surfaceY < WorldSettings.SeaLevel)       return BiomeType.Ocean;

        if (temperature < -15f) return BiomeType.PolarIce;
        if (temperature <   0f) return BiomeType.Tundra;

        if (temperature < 8f)
            return moisture > 0.45f ? BiomeType.BorealForest : BiomeType.Tundra;

        if (temperature < 15f)
        {
            if (moisture > 0.65f) return BiomeType.TemperateRainforest;
            if (moisture > 0.35f) return BiomeType.TemperateForest;
            return BiomeType.Grassland;
        }

        if (temperature < 22f)
        {
            if (moisture > 0.55f) return BiomeType.TemperateForest;
            if (moisture > 0.25f) return BiomeType.Grassland;
            return BiomeType.Shrubland;
        }

        // Hot band
        if (moisture > 0.65f) return BiomeType.TropicalRainforest;
        if (moisture > 0.35f) return BiomeType.Savanna;
        return BiomeType.Desert;
    }

    // ── Block fill rules ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns the name of the topmost surface block material for this biome.
    /// Used as a key in <see cref="WorldBlockPalette.GetByName"/>.
    /// </summary>
    public static string SurfaceBlockName(BiomeType biome)
    {
        switch (biome)
        {
            case BiomeType.PolarIce:             return "Ice";
            case BiomeType.Tundra:               return "Permafrost";
            case BiomeType.BorealForest:         return "Grass";
            case BiomeType.TemperateRainforest:
            case BiomeType.TemperateForest:
            case BiomeType.Grassland:            return "Grass";
            case BiomeType.Shrubland:            return "Gravel";
            case BiomeType.Savanna:              return "Dirt";
            case BiomeType.Desert:               return "Sand";
            case BiomeType.TropicalRainforest:   return "Grass";
            case BiomeType.Ocean:
            case BiomeType.DeepOcean:            return "Sand";
            default:                             return "Stone";
        }
    }

    /// <summary>
    /// Returns the name of the sub-surface fill material (dirt / sand / ice layer)
    /// that sits between the surface block and the underlying stone.
    /// </summary>
    public static string SubsurfaceBlockName(BiomeType biome)
    {
        switch (biome)
        {
            case BiomeType.Desert:   return "Sand";
            case BiomeType.PolarIce: return "Ice";
            case BiomeType.Tundra:   return "Permafrost";
            default:                 return "Dirt";
        }
    }

    /// <summary>
    /// How many blocks thick the sub-surface layer is before stone begins.
    /// </summary>
    public static int SubsurfaceDepth(BiomeType biome)
    {
        switch (biome)
        {
            case BiomeType.PolarIce: return 0;
            case BiomeType.Tundra:   return 2;
            case BiomeType.Desert:   return 4;
            case BiomeType.Ocean:
            case BiomeType.DeepOcean: return 2;
            default:                  return 3;
        }
    }

    /// <summary>
    /// Extra moisture value (stored in <c>Block.chemicalContamination</c>) applied
    /// to air blocks carved inside caves. Higher = wetter caves.
    /// </summary>
    public static float CaveMoistureModifier(BiomeType biome)
    {
        switch (biome)
        {
            case BiomeType.Desert:               return 0.05f;
            case BiomeType.TropicalRainforest:
            case BiomeType.TemperateRainforest:  return 0.30f;
            default:                             return 0.15f;
        }
    }
}
