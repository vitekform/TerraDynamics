using System.Collections;
using Resources.Scripts.UI;
using UnityEngine;

namespace Resources.Scripts
{
    public class GameInitializer : MonoBehaviour
    {
        private IEnumerator Start()
        {
            // ── Show loading screen ──────────────────────────────────────────
            var loadingScreenGO = new GameObject("LoadingScreen");
            var loadingScreen   = loadingScreenGO.AddComponent<LoadingScreen>();

            // ── Stage 1: Register materials ──────────────────────────────────
            loadingScreen.SetStatus("Preparing materials...");
            loadingScreen.SetProgress(0f);
            yield return null;

            PrepareMaterials();
            yield return null;

            // ── Stage 2 & 3: Generate world with live progress ───────────────
            yield return WorldGenerator.Instance.GenerateChunksAroundCoroutine(
                worldCenter: WorldGenerator.Instance.transform.position,
                chunkRadius: 2,
                onProgress: (status, progress) =>
                {
                    loadingScreen.SetStatus(status);
                    loadingScreen.SetProgress(progress);
                });

            // ── Done ─────────────────────────────────────────────────────────
            loadingScreen.SetStatus("Done!");
            loadingScreen.SetProgress(1f);

            yield return new WaitForSeconds(0.5f);

            loadingScreen.Hide();
        }

        private void PrepareMaterials()
        {
            MaterialRegistry registry = WorldGenerator.Instance.MaterialRegistry;
            registry.Register("water", new Material(1000f, "Materials/Fluids/water"));

            // Rocks
            //   IGNEOUS
            const string igneousBase = "Materials/Rocks/Igneous/Intrusive/";
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