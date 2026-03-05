using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Resources.Scripts
{
    /// <summary>
    /// Builds a single merged mesh for an entire chunk instead of one GameObject per block.
    ///
    /// For each exposed block face a quad is added to a per-material submesh.
    /// The result is ONE GameObject / ONE draw-call per material per chunk —
    /// orders of magnitude fewer objects and far less RAM than per-block rendering.
    /// </summary>
    public static class ChunkMeshBuilder
    {
        // ── Cube face definitions (local block space 0–1) ─────────────────────
        // Vertices listed counter-clockwise when viewed from the outside (Unity left-hand).

        private static readonly Vector3[][] FaceVertices =
        {
            // Top    (+Y)
            new[] { new Vector3(0,1,0), new Vector3(0,1,1), new Vector3(1,1,1), new Vector3(1,1,0) },
            // Bottom (-Y)
            new[] { new Vector3(0,0,1), new Vector3(0,0,0), new Vector3(1,0,0), new Vector3(1,0,1) },
            // North  (+Z)
            new[] { new Vector3(1,0,1), new Vector3(1,1,1), new Vector3(0,1,1), new Vector3(0,0,1) },
            // South  (-Z)
            new[] { new Vector3(0,0,0), new Vector3(0,1,0), new Vector3(1,1,0), new Vector3(1,0,0) },
            // East   (+X)
            new[] { new Vector3(1,0,0), new Vector3(1,1,0), new Vector3(1,1,1), new Vector3(1,0,1) },
            // West   (-X)
            new[] { new Vector3(0,0,1), new Vector3(0,1,1), new Vector3(0,1,0), new Vector3(0,0,0) },
        };

        private static readonly Vector3[] FaceNormals =
        {
            Vector3.up, Vector3.down,
            Vector3.forward, Vector3.back,
            Vector3.right, Vector3.left,
        };

        private static readonly Vector3Int[] FaceDirections =
        {
            new Vector3Int( 0,  1,  0),
            new Vector3Int( 0, -1,  0),
            new Vector3Int( 0,  0,  1),
            new Vector3Int( 0,  0, -1),
            new Vector3Int( 1,  0,  0),
            new Vector3Int(-1,  0,  0),
        };

        // Same UV layout for every face.
        private static readonly Vector2[] FaceUVs =
        {
            new Vector2(0, 0), new Vector2(0, 1),
            new Vector2(1, 1), new Vector2(1, 0),
        };

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Builds a chunk GameObject with a single merged mesh (one submesh per material).
        /// Fully occluded faces are never added. Returns null if the chunk is empty.
        /// </summary>
        public static GameObject Build(Chunk chunk, MaterialRegistry materialRegistry)
        {
            // Accumulate geometry per material key.
            var verts  = new Dictionary<string, List<Vector3>>();
            var norms  = new Dictionary<string, List<Vector3>>();
            var uvs    = new Dictionary<string, List<Vector2>>();
            var tris   = new Dictionary<string, List<int>>();

            for (int x = 0; x < Chunk.Size; x++)
            for (int y = 0; y < Chunk.Size; y++)
            for (int z = 0; z < Chunk.Size; z++)
            {
                Block block = chunk.GetBlock(x, y, z);
                if (block == null) continue;

                string key = block.MaterialKey;
                EnsureKey(verts, norms, uvs, tris, key);

                var blockOrigin = new Vector3(x, y, z);

                for (int f = 0; f < 6; f++)
                {
                    Vector3Int dir = FaceDirections[f];
                    if (!chunk.IsTransparent(x + dir.x, y + dir.y, z + dir.z)) continue;

                    int startIdx = verts[key].Count;

                    foreach (Vector3 v in FaceVertices[f])
                    {
                        verts[key].Add(blockOrigin + v);
                        norms[key].Add(FaceNormals[f]);
                    }
                    foreach (Vector2 uv in FaceUVs)
                        uvs[key].Add(uv);

                    // Two triangles per quad (0-1-2, 0-2-3).
                    tris[key].Add(startIdx);     tris[key].Add(startIdx + 1);
                    tris[key].Add(startIdx + 2); tris[key].Add(startIdx);
                    tris[key].Add(startIdx + 2); tris[key].Add(startIdx + 3);
                }
            }

            if (verts.Count == 0) return null; // fully empty chunk

            // ── Combine all submesh geometry into one Mesh ────────────────────

            var allVerts  = new List<Vector3>();
            var allNorms  = new List<Vector3>();
            var allUVs    = new List<Vector2>();
            var submeshes = new List<int[]>();
            var materials = new List<UnityEngine.Material>();

            foreach (string key in verts.Keys)
            {
                int offset = allVerts.Count;
                allVerts.AddRange(verts[key]);
                allNorms.AddRange(norms[key]);
                allUVs.AddRange(uvs[key]);

                var triList = tris[key];
                var offsetTris = new int[triList.Count];
                for (int i = 0; i < triList.Count; i++)
                    offsetTris[i] = triList[i] + offset;
                submeshes.Add(offsetTris);

                materials.Add(LoadUnityMaterial(key, materialRegistry));
            }

            var mesh = new Mesh
            {
                // UInt32 allows >65 535 vertices (needed for dense surface chunks).
                indexFormat  = IndexFormat.UInt32,
                vertices     = allVerts.ToArray(),
                normals      = allNorms.ToArray(),
                uv           = allUVs.ToArray(),
                subMeshCount = submeshes.Count,
            };
            for (int i = 0; i < submeshes.Count; i++)
                mesh.SetTriangles(submeshes[i], i);

            // ── Assemble the GameObject ───────────────────────────────────────

            var go = new GameObject(
                $"Chunk_{chunk.ChunkCoord.x}_{chunk.ChunkCoord.y}_{chunk.ChunkCoord.z}");
            go.transform.position = chunk.WorldOrigin;
            go.AddComponent<MeshFilter>().sharedMesh      = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = materials.ToArray();
            go.AddComponent<MeshCollider>().sharedMesh    = mesh;

            return go;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void EnsureKey(
            Dictionary<string, List<Vector3>> v,
            Dictionary<string, List<Vector3>> n,
            Dictionary<string, List<Vector2>> u,
            Dictionary<string, List<int>>     t,
            string key)
        {
            if (v.ContainsKey(key)) return;
            v[key] = new List<Vector3>();
            n[key] = new List<Vector3>();
            u[key] = new List<Vector2>();
            t[key] = new List<int>();
        }

        private static UnityEngine.Material LoadUnityMaterial(string key, MaterialRegistry registry)
        {
            Material blockMat = registry.Get(key);
            if (blockMat != null)
            {
                var loaded = UnityEngine.Resources.Load<UnityEngine.Material>(blockMat.matPath);
                if (loaded != null) return loaded;
            }
            // Fallback: magenta so missing materials are obvious.
            return new UnityEngine.Material(Shader.Find("Standard"));
        }
    }
}
