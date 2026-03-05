using UnityEngine;

namespace Resources.Scripts
{
    /// <summary>
    /// Represents a single block inside a chunk (32x32x32).
    /// Coordinates X, Y, Z are relative to the chunk origin (range 0–31).
    /// </summary>
    public class Block
    {
        /// <summary>Shape identifier (e.g. "cube", "slope", "stair").</summary>
        public string Shape { get; set; }

        /// <summary>Key used to look up this block's material in <see cref="MaterialRegistry"/>.</summary>
        public string MaterialKey { get; set; }

        /// <summary>Chunk-relative X coordinate (0–31).</summary>
        public byte X { get; set; }

        /// <summary>Chunk-relative Y coordinate (0–31).</summary>
        public byte Y { get; set; }

        /// <summary>Chunk-relative Z coordinate (0–31).</summary>
        public byte Z { get; set; }

        public Block(string shape, string materialKey, byte x, byte y, byte z)
        {
            Shape = shape;
            MaterialKey = materialKey;
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>Resolves the material from the provided registry.</summary>
        public Material GetMaterial(MaterialRegistry registry)
        {
            return registry.Get(MaterialKey);
        }

        /// <summary>
        /// Creates a GameObject for this block.
        /// The mesh is loaded from Resources using <see cref="Shape"/> as the path,
        /// and the Unity material is loaded from the path stored in the registry material's <c>matPath</c>.
        /// The object is positioned at (<see cref="X"/>, <see cref="Y"/>, <see cref="Z"/>)
        /// relative to <paramref name="chunkOrigin"/>.
        /// </summary>
        public GameObject Construct(MaterialRegistry registry, Vector3 chunkOrigin = default)
        {
            var go = new GameObject($"Block_{Shape}_{X}_{Y}_{Z}");
            go.transform.position = chunkOrigin + new Vector3(X, Y, Z);

            var meshFilter = go.AddComponent<MeshFilter>();
            var meshRenderer = go.AddComponent<MeshRenderer>();

            Mesh mesh = UnityEngine.Resources.Load<Mesh>(Shape);
            if (mesh != null)
                meshFilter.mesh = mesh;

            Material blockMaterial = registry.Get(MaterialKey);
            if (blockMaterial != null)
            {
                UnityEngine.Material unityMaterial =
                    UnityEngine.Resources.Load<UnityEngine.Material>(blockMaterial.matPath);
                if (unityMaterial != null)
                    meshRenderer.material = unityMaterial;
            }

            return go;
        }
    }
}
