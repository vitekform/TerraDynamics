using UnityEngine;

[CreateAssetMenu(fileName = "BlockMaterials", menuName = "TerraDynamics/BlockMaterial")]
public class BlockMaterials : ScriptableObject
{
    public string materialName;
    public Material[] material;

    //Structural
    [Header("Structural")]
    public float density;
    public float hardness;
    public float elasticity;
    public float poissonRatio;
    public float fractureToughness;
    public float brittleness;
    public float frictionCoef; //Duplicate
    public float shearStrength; //Duplicate

    //Thermal
    [Header("Thermal")]
    public float boilingPoint;
    public float meltingPoint;
    public float thermalConductivity;
    public float specificHeat;
    public float thermalExpansion;
    public float thermalDiffusivity; //Duplicate

    //Fluid
    [Header("Fluid")]
    public float viscosity; //Duplicate
    public float surfaceTension; //Duplicate
    public float fluidDensity; //Duplicate

    //Optical
    [Header("Optical")]
    public Color color;
    public float albedo;
    public float transparency;
    public float refractiveIndex;

    //Chemical
    [Header("Chemical")]
    public float reactivity; //Duplicate
    public float corrosionRate; //Duplicate
    public float combustibility; //Duplicate
    public float ignitionTemperature;
    public float combustionEnergy;
    public float solubility;
    public float toxicity;
    public float magneticPermeability; //Duplicate

    //Electrical
    [Header("Electrical")]
    public float conductivity; //Duplicate
    public float resistivity; //Duplicate
    public float dielectricConstant;
    public float magneticSusceptibility; //Duplicate
    public float piezoelectricCoefficient; //Duplicate
}
