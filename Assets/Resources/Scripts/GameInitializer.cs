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
            const string igneousBase = "Materials/Rocks/Igneous/";
            //     INTRUSIVE
            //       GRANITE
            const double graniteDensity = 2750.0;
            const string graniteBase    = igneousBase + "Granite/";
            registry.Register("granite_black", new Material(graniteDensity, graniteBase + "granite_black"));
            registry.Register("granite_blue",  new Material(graniteDensity, graniteBase + "granite_blue"));
            registry.Register("granite_gray",  new Material(graniteDensity, graniteBase + "granite_gray"));
            registry.Register("granite_green", new Material(graniteDensity, graniteBase + "granite_green"));
            registry.Register("granite_pink",  new Material(graniteDensity, graniteBase + "granite_pink"));
            registry.Register("granite_red",   new Material(graniteDensity, graniteBase + "granite_red"));
            registry.Register("granite_white", new Material(graniteDensity, graniteBase + "granite_white"));
            
            //       GABBRO
            
            //       DIORITE
            
            //       PERIDOTITE
            
            //       PEGMATITE
            
            //       TONALITE
            
            //       GRANODIORITE
            
            //       ANORTHOSITE
            
            //       SYENITE
            
            //       NORITE
            
            
            //     EXTRUSIVE
            //       BASALT
            
            //       ANDESITE
            
            //       PHONOLITE
            
            //       MELAPHYRE
            
            //       RHYOLITE
            
            //       DACITE
            
            //       TRACHYTE
            
            //       LATITE
            
            //       KOMATIITE
            
            
            //     VOLCANIC GLASS
            //       OBSIDIAN
            
            //       PUMICE
            
            //       SCORIA
            
            //       TUFF
            
            
            //   SEDIMENTARY
            const string sedimentaryBase = "Materials/Rocks/Sedimentary/";
            //     CLASTIC
            //       CONGLOMERATE
            registry.Register("conglomerate", new Material(0f, sedimentaryBase + "Clastic/conglomerate"));
            //       BRECCIA
            registry.Register("breccia", new Material(0f, sedimentaryBase + "Clastic/breccia"));
            //       SAND
            registry.Register("sand", new Material(0f, sedimentaryBase + "Clastic/sand"));
            //       SANDSTONE
            registry.Register("sandstone", new Material(0f, sedimentaryBase + "Clastic/sandstone"));
            //       GRAVEL
            registry.Register("gravel", new Material(0f, sedimentaryBase + "Clastic/gravel"));
            //       DIRT
            registry.Register("dirt", new Material(0f, sedimentaryBase + "Clastic/dirt"));
            //       SLATE
            const string shaleBase =  sedimentaryBase + "Clastic/Shale/";
            //       CLAY
            const string clayBase =  sedimentaryBase + "Clastic/Clay/";
            //       SILTSTONE
            
            //       MUDSTONE
            
            //       GRAYWACKE
            
            
            //     BIOGENIC
            //       LIMESTONE
            registry.Register("limestone", new Material(0f, sedimentaryBase + "Biogenic/limestone"));
            //       COAL
            //         LIGNITE
            
            //         SUBBITUMINOUS COAL
            
            //         BITUMINOUS COAL
            
            //         ANTHRACITE
            
            
            //       COQUINA
            
            //       CHALK
            
            //       FOSSILIFEROUS LIMESTONE
            
            //     CHEMICAL
            //       TRAVERTINE
            registry.Register("travertine", new Material(0f, sedimentaryBase + "Chemical/travertine"));
            //       ROCK SALT
            
            //       GYPSUM
            
            //       CHERT
            
            //       DOLOSTONE
            
            
            //   METAMORPHIC
            const string metamorphicBase = "Materials/Rocks/Metamorphic/";
            //     FOLIATED
            //       SLATE
            
            //       PHYLLITE
            
            //       SCHIST
            //         MICA SCHIST
            
            //       GNEISS
            
            //       MIGMATITE
            
            
            //     NON FOLIATED
            //       MARBLE
            
            //       QUARTZITE
            
            //       HORNFELS
            
            //       SERPENTINITE
            
            //       AMPHIBOLITE
            
            
            
            
            //Holy hell the sheer amount of rocks is insane. Just wait for the minerals too, and it will be even worse
            // Soils have been here.
            // Jokes on you. Žádné soily tady nebudou protože žádné soily tady nikdy nebudou :D - Siryakari
            
            registry.Register("grass", new Material(0f, "Materials/Soil/grass"));
        }
    }
}