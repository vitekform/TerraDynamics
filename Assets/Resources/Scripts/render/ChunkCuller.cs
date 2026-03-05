using System.Collections.Generic;
using UnityEngine;

namespace Resources.Scripts
{
    /// <summary>
    /// Distance-based chunk streaming and culling.
    ///
    /// Attach alongside (or as a child of) <see cref="WorldGenerator"/>.
    /// Point <see cref="Observer"/> at your player/camera transform.
    ///
    /// Three distance tiers (all in chunk units):
    ///   ≤ HorizontalViewDistance  → chunk active and visible
    ///   ≤ EvictDistance           → chunk disabled (invisible, mesh kept in memory)
    ///   >  EvictDistance          → chunk destroyed and removed; regenerated on re-entry
    /// </summary>
    public class ChunkCuller : MonoBehaviour
    {
        [Header("References")]
        public WorldGenerator WorldGenerator;

        [Tooltip("The transform to track (player, main camera, etc.).")]
        public Transform Observer;

        [Header("View Distance (chunks)")]
        [Min(1)] public int HorizontalViewDistance = 6;
        [Min(1)] public int VerticalViewDistance   = 3;

        [Tooltip("Chunks beyond this XZ radius are fully destroyed to free RAM. " +
                 "Must be >= HorizontalViewDistance.")]
        [Min(1)] public int EvictDistance = 10;

        // Coord → root GameObject of that chunk (one merged mesh per chunk).
        private readonly Dictionary<Vector3Int, GameObject> _chunks =
            new Dictionary<Vector3Int, GameObject>();

        private Vector3Int _lastObserverChunk = new Vector3Int(int.MinValue, 0, 0);

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Update()
        {
            if (Observer == null || WorldGenerator == null) return;

            Vector3Int current = WorldToChunkCoord(Observer.position);
            if (current == _lastObserverChunk) return;

            _lastObserverChunk = current;
            RefreshChunks(current);
            EvictDistantChunks();
        }

        // ── Core logic ────────────────────────────────────────────────────────

        private void RefreshChunks(Vector3Int center)
        {
            int rh  = HorizontalViewDistance;
            int rv  = VerticalViewDistance;
            int rh2 = rh * rh;

            var shouldBeActive = new HashSet<Vector3Int>();

            for (int dx = -rh; dx <= rh; dx++)
            for (int dz = -rh; dz <= rh; dz++)
            {
                if (dx * dx + dz * dz > rh2) continue;

                for (int dy = -rv; dy <= rv; dy++)
                    shouldBeActive.Add(new Vector3Int(center.x + dx, center.y + dy, center.z + dz));
            }

            foreach (Vector3Int coord in shouldBeActive)
            {
                if (_chunks.TryGetValue(coord, out GameObject existing))
                {
                    existing.SetActive(true);
                }
                else
                {
                    Chunk chunk = WorldGenerator.GenerateChunk(coord.x, coord.y, coord.z);
                    GameObject go = chunk.Construct(WorldGenerator.MaterialRegistry);
                    // go is null when the chunk has no visible faces (e.g. all-air chunk).
                    if (go != null) _chunks[coord] = go;
                }
            }

            foreach (KeyValuePair<Vector3Int, GameObject> kvp in _chunks)
            {
                if (!shouldBeActive.Contains(kvp.Key))
                    kvp.Value.SetActive(false);
            }
        }

        /// <summary>
        /// Destroys and removes all chunk GameObjects (+ their meshes) beyond
        /// <see cref="EvictDistance"/> from the current observer position.
        /// Called automatically every time the observer moves to a new chunk.
        /// </summary>
        private void EvictDistantChunks()
        {
            int ed2 = EvictDistance * EvictDistance;
            var toEvict = new List<Vector3Int>();

            foreach (Vector3Int coord in _chunks.Keys)
            {
                Vector3Int d = coord - _lastObserverChunk;
                if (d.x * d.x + d.z * d.z > ed2)
                    toEvict.Add(coord);
            }

            foreach (Vector3Int coord in toEvict)
            {
                Destroy(_chunks[coord]);
                _chunks.Remove(coord);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Vector3Int WorldToChunkCoord(Vector3 world)
        {
            return new Vector3Int(
                Mathf.FloorToInt(world.x / Chunk.Size),
                Mathf.FloorToInt(world.y / Chunk.Size),
                Mathf.FloorToInt(world.z / Chunk.Size));
        }

        /// <summary>Manually evicts a specific chunk.</summary>
        public void EvictChunk(Vector3Int chunkCoord)
        {
            if (!_chunks.TryGetValue(chunkCoord, out GameObject go)) return;
            Destroy(go);
            _chunks.Remove(chunkCoord);
        }
    }
}
