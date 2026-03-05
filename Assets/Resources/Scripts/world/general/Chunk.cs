using UnityEngine;

namespace Resources.Scripts
{
    /// <summary>
    /// A 32×32×32 block container. ChunkCoord is in chunk-grid space;
    /// multiply by <see cref="Size"/> to get world-space origin.
    /// </summary>
    public class Chunk
    {
        public const int Size = 32;

        public Vector3Int ChunkCoord { get; }
        public Vector3    WorldOrigin => new Vector3(
            ChunkCoord.x * Size,
            ChunkCoord.y * Size,
            ChunkCoord.z * Size);

        private readonly Block[,,] _blocks = new Block[Size, Size, Size];

        public Chunk(Vector3Int chunkCoord)
        {
            ChunkCoord = chunkCoord;
        }

        /// <summary>Returns the block at chunk-local (x, y, z), or null if empty.</summary>
        public Block GetBlock(int x, int y, int z) => _blocks[x, y, z];

        /// <summary>Stores a block at chunk-local (x, y, z).</summary>
        public void SetBlock(int x, int y, int z, Block block) => _blocks[x, y, z] = block;

        /// <summary>
        /// Builds a single merged-mesh GameObject for this chunk via <see cref="ChunkMeshBuilder"/>.
        /// One draw call per material — vastly cheaper than one GameObject per block.
        /// Returns null if the chunk contains no visible geometry.
        /// </summary>
        public GameObject Construct(MaterialRegistry materialRegistry)
            => ChunkMeshBuilder.Build(this, materialRegistry);

        // ── Transparency query (used by ChunkMeshBuilder for face culling) ────

        /// <summary>
        /// Returns true when the given chunk-local position should not occlude a neighbour:
        /// out-of-bounds (chunk edge), air (null), or a non-occluding block such as water.
        /// </summary>
        public bool IsTransparent(int x, int y, int z)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size || z < 0 || z >= Size)
                return true;

            Block b = _blocks[x, y, z];
            return b == null || b.MaterialKey == "water";
        }
    }
}
