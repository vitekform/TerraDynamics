using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Resources.Scripts
{
    /// <summary>
    /// MonoBehaviour entry point for world generation.
    ///
    /// Workflow:
    ///   1. Attach this component to a scene GameObject.
    ///   2. Populate <see cref="MaterialRegistry"/> with your materials (or leave defaults).
    ///   3. Call <see cref="GenerateChunk"/> or <see cref="GenerateChunksAround"/> at runtime.
    ///
    /// Internally uses <see cref="TerrainGenerator"/> for smooth Perlin-based terrain,
    /// <see cref="BiomeRegistry"/> for biome selection, and each biome's
    /// <see cref="IBiomeFeatureGenerator"/> for block column data.
    /// </summary>
    public class WorldGenerator : MonoBehaviour
    {
        [Header("World Settings")]
        [Tooltip("Seed that offsets all Perlin noise maps.")]
        public int seed = 0;

        // Registries are created in Awake so they can also be pre-populated
        // from other scripts in the same Awake/Start frame via lazy property access.
        public static WorldGenerator Instance { get; private set; }

        public MaterialRegistry MaterialRegistry { get; private set; }

        // ── Unity lifecycle ───────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null) Destroy(gameObject);
            else Instance = this;

            MaterialRegistry = new MaterialRegistry();
            BiomeRegistry    = new BiomeRegistry(registerDefaults: true);

            _worldRoot = new GameObject("World").transform;
            _worldRoot.SetParent(transform, worldPositionStays: false);
        }
        public BiomeRegistry    BiomeRegistry    { get; private set; }

        // Parent transform that holds every generated chunk root.
        private Transform _worldRoot;
        
        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Generates and returns one chunk at chunk-grid coordinate (chunkX, chunkY, chunkZ).
        /// Does NOT instantiate GameObjects; call <see cref="Chunk.Construct"/> on the result
        /// when you want visible geometry.
        /// </summary>
        public Chunk GenerateChunk(int chunkX, int chunkY, int chunkZ)
        {
            var chunk = new Chunk(new Vector3Int(chunkX, chunkY, chunkZ));

            int originX = chunkX * Chunk.Size;
            int originY = chunkY * Chunk.Size;
            int originZ = chunkZ * Chunk.Size;

            for (int lx = 0; lx < Chunk.Size; lx++)
            for (int lz = 0; lz < Chunk.Size; lz++)
            {
                int worldX = originX + lx;
                int worldZ = originZ + lz;

                // Sample biome + smooth terrain surface height for this column.
                var (biome, surfaceY) = TerrainGenerator.Sample(worldX, worldZ, seed, BiomeRegistry);

                if (biome?.FeatureGenerator == null) continue;

                // Ask the biome to fill only this chunk's Y slice.
                ColumnBlock[] column = biome.FeatureGenerator.GenerateColumn(
                    worldX, worldZ, surfaceY, seed,
                    minY: originY,
                    maxY: originY + Chunk.Size - 1);

                foreach (ColumnBlock cb in column)
                {
                    int ly = cb.WorldY - originY;
                    chunk.SetBlock(lx, ly, lz, new Block(
                        shape:       "cube",
                        materialKey: cb.MaterialKey,
                        x:           (byte)lx,
                        y:           (byte)ly,
                        z:           (byte)lz));
                }
            }

            return chunk;
        }

        /// <summary>
        /// Generates all chunks in a cubic radius around a world position,
        /// instantiates their geometry, and parents them to the world root.
        /// </summary>
        /// <param name="worldCenter">Centre position in world units.</param>
        /// <param name="chunkRadius">Half-extent in chunk units on each axis.</param>
        /// <returns>All generated chunk GameObjects.</returns>
        public List<GameObject> GenerateChunksAround(Vector3 worldCenter, int chunkRadius = 4)
        {
            int cx = Mathf.FloorToInt(worldCenter.x / Chunk.Size);
            int cy = Mathf.FloorToInt(worldCenter.y / Chunk.Size);
            int cz = Mathf.FloorToInt(worldCenter.z / Chunk.Size);

            var result = new List<GameObject>();

            for (int dx = -chunkRadius; dx <= chunkRadius; dx++)
            for (int dy = -chunkRadius; dy <= chunkRadius; dy++)
            for (int dz = -chunkRadius; dz <= chunkRadius; dz++)
            {
                Chunk chunk = GenerateChunk(cx + dx, cy + dy, cz + dz);
                GameObject go = chunk.Construct(MaterialRegistry);
                if (go == null) continue; // empty chunk — no visible faces
                go.transform.SetParent(_worldRoot, worldPositionStays: true);
                result.Add(go);
            }

            return result;
        }

        // World generation is intentionally NOT started here.
        // GameInitializer.Start() registers all materials first, then calls Generate().
        // DO NOT call GenerateChunksAround here — materials won't be registered yet.
        private void Start() { }

        // ── Async API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Coroutine version of <see cref="GenerateChunksAround"/> that reports
        /// progress via a callback and yields between each chunk so the UI can update.
        /// </summary>
        /// <param name="worldCenter">Centre position in world units.</param>
        /// <param name="chunkRadius">Half-extent in chunk units on each axis.</param>
        /// <param name="onProgress">
        ///   Invoked before each yield with a human-readable status string and a
        ///   normalised progress value in [0, 1].
        /// </param>
        /// <param name="onComplete">Invoked once all chunks are generated and built.</param>
        public IEnumerator GenerateChunksAroundCoroutine(
            Vector3 worldCenter,
            int chunkRadius,
            Action<string, float> onProgress,
            Action<List<GameObject>> onComplete = null)
        {
            int cx = Mathf.FloorToInt(worldCenter.x / Chunk.Size);
            int cy = Mathf.FloorToInt(worldCenter.y / Chunk.Size);
            int cz = Mathf.FloorToInt(worldCenter.z / Chunk.Size);

            // Collect all chunk coordinates up front.
            var coords = new List<Vector3Int>();
            for (int dx = -chunkRadius; dx <= chunkRadius; dx++)
            for (int dy = -chunkRadius; dy <= chunkRadius; dy++)
            for (int dz = -chunkRadius; dz <= chunkRadius; dz++)
                coords.Add(new Vector3Int(cx + dx, cy + dy, cz + dz));

            int total = coords.Count;

            // ── Phase 1: generate block data for every chunk ─────────────────
            var chunks = new List<Chunk>(total);
            for (int i = 0; i < total; i++)
            {
                onProgress?.Invoke($"Generating chunks {i + 1}/{total}", (float)i / total * 0.5f);
                yield return null;

                var coord = coords[i];
                chunks.Add(GenerateChunk(coord.x, coord.y, coord.z));
            }

            // ── Phase 2: build meshes for every chunk ────────────────────────
            var result = new List<GameObject>(total);
            for (int i = 0; i < chunks.Count; i++)
            {
                onProgress?.Invoke($"Preparing render {i + 1}/{total}", 0.5f + (float)i / total * 0.5f);
                yield return null;

                GameObject go = chunks[i].Construct(MaterialRegistry);
                if (go == null) continue;
                go.transform.SetParent(_worldRoot, worldPositionStays: true);
                result.Add(go);
            }

            onComplete?.Invoke(result);
        }
    }
}
