using UnityEngine;

public struct Block
{
    public BlockMaterials materials;

    //ONLY HERE VALUES
    public float temperature; //this is in Kelvin
    public MatterState state;
    public float burnProgress;

    public byte fluidLevel;
    
    //Structural
    public float stress;
    public float damage;
    public float pressure;

    //Contamination
    public float dirtContamination;
    public float chemicalContamination;
    public float bacteriaContamination;
    public float saltContent;

    //DUPLICATED VALUES
    public float frictionCoef;
    public float shearStrength;
    public float thermalDiffusivity;

    public float viscosity;
    public float surfaceTension;
    public float fluidDensity;

    public float reactivity;
    public float corrosionRate;
    public float combustibility;
    public float magneticPermeability;

    public float conductivity;
    public float resistivity;
    public float magneticSusceptibility;
    public float piezoelectricCoefficient;
}
