using UnityEngine;

namespace Resources.Scripts
{
    public class GameInitializer : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("Preparing MaterialRegistry..."); 
            PrepareMaterials();
            Debug.Log("Materials registered!");
            Debug.Log("Running worldgen...");
            WorldGenerator.Instance.GenerateChunksAround(WorldGenerator.Instance.transform.position, 2);
            Debug.Log("Worldgen finished.");
        }

        private void PrepareMaterials()
        {
            MaterialRegistry registry = WorldGenerator.Instance.MaterialRegistry;
            registry.Register("water", new Material(1000f, "Materials/Fluids/water"));

            // Rocks
            //   IGNEOUS
            //     INTRUSIVE
            //       GRANITE
            const double graniteDensity = 2750.0;
            const string graniteBase    = "Materials/Rocks/Igneous/Intrusive/Granite/";
            registry.Register("granite_black", new Material(graniteDensity, graniteBase + "granite_black"));
            registry.Register("granite_blue",  new Material(graniteDensity, graniteBase + "granite_blue"));
            registry.Register("granite_gray",  new Material(graniteDensity, graniteBase + "granite_gray"));
            registry.Register("granite_green", new Material(graniteDensity, graniteBase + "granite_green"));
            registry.Register("granite_pink",  new Material(graniteDensity, graniteBase + "granite_pink"));
            registry.Register("granite_red",   new Material(graniteDensity, graniteBase + "granite_red"));
            registry.Register("granite_white", new Material(graniteDensity, graniteBase + "granite_white"));
            registry.Register("breccia", new Material(0f, "Materials/Rocks/Igneous/Intrusive/breccia"));
            registry.Register("conglomerate", new Material(0f, "Materials/Rocks/Igneous/Intrusive/conglomerate"));
            
            
            // Soils have been here.
            // Jokes on you. Žádné soily tady nebudou protože žádné soily tady nikdy nebudou :D - Siryakari
        }
    }
}