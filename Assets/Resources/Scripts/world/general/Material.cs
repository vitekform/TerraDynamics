namespace Resources.Scripts
{
    public class Material
    {
        public double density; // in kg/m3
        public string matPath; // path to the material .Asset

        public Material(double density, string matPath)
        {
            this.density = density;
            this.matPath = matPath;
        }
    }
}