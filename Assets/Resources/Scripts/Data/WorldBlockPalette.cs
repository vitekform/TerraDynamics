using UnityEngine;

/// <summary>
/// ScriptableObject palette that holds references to every BlockMaterials asset
/// used by world generation. Assign all fields in the Unity Inspector, then
/// pass this palette into WorldGeneration.GenerateChunk().
/// </summary>
[CreateAssetMenu(fileName = "WorldBlockPalette", menuName = "TerraDynamics/WorldBlockPalette")]
public class WorldBlockPalette : ScriptableObject
{
    [Header("Stone & Rock")]
    public BlockMaterials bedrock;
    public BlockMaterials stone;        // generic deep rock / fallback
    public BlockMaterials granite;
    public BlockMaterials basalt;
    public BlockMaterials limestone;    // sedimentary
    public BlockMaterials shale;        // sedimentary

    [Header("Surface")]
    public BlockMaterials dirt;
    public BlockMaterials grass;
    public BlockMaterials sand;
    public BlockMaterials gravel;
    public BlockMaterials clay;
    public BlockMaterials snow;
    public BlockMaterials ice;
    public BlockMaterials permafrost;   // frozen dirt / tundra subsurface

    [Header("Fluids")]
    public BlockMaterials water;
    public BlockMaterials lava;

    [Header("Ores")]
    public BlockMaterials oreCoal;
    public BlockMaterials oreIron;
    public BlockMaterials oreCopper;
    public BlockMaterials oreGold;
    public BlockMaterials oreSilver;
    public BlockMaterials oreDiamond;
    public BlockMaterials oreSulfur;

    // ── Lookup helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns the surface/terrain BlockMaterials matching the given name.
    /// Falls back to <see cref="stone"/> if the name is unknown or the field is unassigned.
    /// </summary>
    public BlockMaterials GetByName(string name)
    {
        switch (name)
        {
            case "Bedrock":    return bedrock    != null ? bedrock    : stone;
            case "Stone":      return stone;
            case "Granite":    return granite    != null ? granite    : stone;
            case "Basalt":     return basalt     != null ? basalt     : stone;
            case "Limestone":  return limestone  != null ? limestone  : stone;
            case "Shale":      return shale      != null ? shale      : stone;
            case "Dirt":       return dirt       != null ? dirt       : stone;
            case "Grass":      return grass      != null ? grass      : dirt;
            case "Sand":       return sand       != null ? sand       : stone;
            case "Gravel":     return gravel     != null ? gravel     : stone;
            case "Clay":       return clay       != null ? clay       : dirt;
            case "Snow":       return snow       != null ? snow       : stone;
            case "Ice":        return ice        != null ? ice        : stone;
            case "Permafrost": return permafrost != null ? permafrost : dirt;
            case "Water":      return water;
            case "Lava":       return lava;
            default:           return stone;
        }
    }

    /// <summary>Returns the ore BlockMaterials for the given ore name, or null if unassigned.</summary>
    public BlockMaterials GetOreByName(string name)
    {
        switch (name)
        {
            case "Coal":    return oreCoal;
            case "Iron":    return oreIron;
            case "Copper":  return oreCopper;
            case "Gold":    return oreGold;
            case "Silver":  return oreSilver;
            case "Diamond": return oreDiamond;
            case "Sulfur":  return oreSulfur;
            default:        return null;
        }
    }
}
