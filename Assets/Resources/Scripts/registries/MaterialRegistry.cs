using System.Collections.Generic;

namespace Resources.Scripts
{
    public class MaterialRegistry
    {
        private readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();

        public void Register(string key, Material material)
        {
            _materials[key] = material;
        }

        public Material Get(string key)
        {
            if (key == "stone")
            {
                key = "granite_white";
            }

            if (key == "grass")
            {
                // temporary until we have proper grass
                key = "granite_red";
            }

            if (key == "dirt")
            {
                // temporary until we have proper dirt
                key = "granite_black";
            }

            _materials.TryGetValue(key, out Material material);
            if (material == null)
            {
                UnityEngine.Debug.LogWarning($"MaterialRegistry: Material with key '{key}' not found.");
            }

            return material;
        }

        public bool Delete(string key)
        {
            return _materials.Remove(key);
        }
    }
}
