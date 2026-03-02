using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Cellular-automata fluid simulation.
///
/// Algorithm: each "active" cell is processed every tick.
///   1. Gravity  – flow as much fluid as possible straight down.
///   2. Horizontal equalisation – spread to the 4 lateral neighbours when the
///      level difference exceeds the viscosity threshold.
///
/// Performance budget: at most <updatesPerTick> cells are processed per tick,
/// and ticks fire at <tickInterval> seconds (default 20 Hz), so the simulation
/// is decoupled from frame rate and keeps the CPU budget stable at 60 FPS.
///
/// Fluid level encoding (fluidLevel field on Block):
///   0            = empty / air
///   1 … MaxLevel = partial fill  (MaxLevel = full voxel)
///
/// A block is treated as fluid when state == MatterState.Liquid AND fluidLevel > 0.
/// A block is treated as solid when state == MatterState.Solid AND materials != null.
/// A block is treated as air  when materials == null.
/// </summary>
public class FluidSimulator : MonoBehaviour
{
    // ── Constants ──────────────────────────────────────────────────────────────

    /// <summary>fluidLevel value that represents a completely full voxel.</summary>
    public const int MaxLevel = 8;

    // ── Inspector ──────────────────────────────────────────────────────────────

    [Header("Performance")]
    [Tooltip("Maximum active cells processed per simulation tick. " +
             "Raise for faster-spreading fluids; lower to save CPU.")]
    public int updatesPerTick = 3000;

    [Tooltip("Seconds between simulation ticks. 0.05 = 20 ticks/s.")]
    public float tickInterval = 0.05f;

    [Header("Rendering")]
    [Tooltip("Semi-transparent material used for all fluid surfaces. " +
             "Use a URP Lit material with Surface Type = Transparent.")]
    public Material fluidMaterial;

    // ── Runtime state ──────────────────────────────────────────────────────────

    // Registered chunks keyed by chunk-space coordinate (cx, 0, cz).
    private readonly Dictionary<Vector3Int, Chunk>      _chunks     = new Dictionary<Vector3Int, Chunk>();
    // MeshFilter of the per-chunk transparent fluid mesh GameObject.
    private readonly Dictionary<Vector3Int, MeshFilter> _fluidMesh  = new Dictionary<Vector3Int, MeshFilter>();

    // Cells with fluid that may still need to move.
    private readonly HashSet<Vector3Int> _active  = new HashSet<Vector3Int>();
    // Newly-activated cells (double-buffer: added during tick, merged after).
    private readonly HashSet<Vector3Int> _pending = new HashSet<Vector3Int>();
    // Chunks whose fluid mesh is stale.
    private readonly HashSet<Vector3Int> _dirty   = new HashSet<Vector3Int>();

    // Scratch list used inside Tick() to avoid modifying the set mid-iteration.
    private readonly List<Vector3Int> _scratch = new List<Vector3Int>();

    private float _timer;
    // Alternates ±1 each tick so horizontal spreading has no fixed directional bias.
    private int _sweep = 1;

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Register a chunk and create its fluid mesh child GameObject.
    /// Call this immediately after WorldGeneration.GenerateChunk().
    /// </summary>
    public void RegisterChunk(Chunk chunk)
    {
        Vector3Int key = chunk.position;
        _chunks[key] = chunk;

        GameObject go = new GameObject($"FluidMesh_{key.x}_{key.z}");
        go.transform.SetParent(transform, worldPositionStays: false);
        go.transform.position = new Vector3(
            key.x * Chunk.chunkSize,
            0f,
            key.z * Chunk.chunkSize);

        MeshFilter   mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.material = fluidMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        _fluidMesh[key] = mf;

        // Activate any fluid blocks that were baked into the chunk at generation time.
        ScanChunkForInitialFluid(chunk);
        MarkDirty(key);
    }

    /// <summary>
    /// Spawn fluid at the given world-space voxel position.
    /// Silently ignored when the position is outside registered chunks or inside solid rock.
    /// </summary>
    /// <param name="worldPos">Integer world-space voxel coordinate.</param>
    /// <param name="mat">BlockMaterials asset describing the fluid (water, lava, oil…).</param>
    /// <param name="amount">Fluid units to add (1–MaxLevel; MaxLevel = full voxel).</param>
    public void AddFluid(Vector3Int worldPos, BlockMaterials mat, int amount = MaxLevel)
    {
        if (!GetBlock(worldPos, out Chunk chunk, out int lx, out int ly, out int lz)) return;
        ref Block b = ref chunk.blocks[lx, ly, lz];

        // Refuse to fill a solid block.
        if (b.state == MatterState.Solid && b.materials != null) return;

        b.materials      = mat;
        b.state          = MatterState.Liquid;
        b.fluidLevel     = (byte)Mathf.Clamp(b.fluidLevel + amount, 0, MaxLevel);
        b.viscosity      = mat.viscosity;
        b.fluidDensity   = mat.fluidDensity;
        b.surfaceTension = mat.surfaceTension;

        Activate(worldPos);
        MarkDirty(chunk.position);
    }

    // ── Unity loop ─────────────────────────────────────────────────────────────

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < tickInterval) return;
        _timer -= tickInterval;

        Tick();
        RebuildDirtyMeshes();

        _sweep = -_sweep;
    }

    // ── Simulation ─────────────────────────────────────────────────────────────

    private void Tick()
    {
        // Merge pending activations.
        foreach (var p in _pending) _active.Add(p);
        _pending.Clear();

        // Snapshot active set into scratch list so we can modify _active freely.
        _scratch.Clear();
        _scratch.AddRange(_active);
        _active.Clear();

        // Cap and shuffle (partial Fisher-Yates) to eliminate sweep-order bias.
        int limit = Mathf.Min(_scratch.Count, updatesPerTick);
        for (int i = 0; i < limit; i++)
        {
            int j = Random.Range(i, _scratch.Count);
            (_scratch[i], _scratch[j]) = (_scratch[j], _scratch[i]);
        }

        for (int i = 0; i < limit; i++)
            ProcessCell(_scratch[i]);
    }

    private void ProcessCell(Vector3Int pos)
    {
        if (!GetBlock(pos, out Chunk chunk, out int lx, out int ly, out int lz)) return;
        ref Block src = ref chunk.blocks[lx, ly, lz];

        if (src.state != MatterState.Liquid || src.fluidLevel <= 0) return;

        bool moved = false;

        // ── 1. Gravity ─────────────────────────────────────────────────────────
        Vector3Int below = new Vector3Int(pos.x, pos.y - 1, pos.z);
        if (GetBlock(below, out Chunk bc, out int bx, out int by, out int bz))
        {
            ref Block bd = ref bc.blocks[bx, by, bz];
            if (CanReceive(ref bd))
            {
                int flow = Mathf.Min(src.fluidLevel, MaxLevel - bd.fluidLevel);
                if (flow > 0)
                {
                    Transfer(ref src, ref bd, flow, chunk.position, bc.position);
                    Activate(below);
                    moved = true;
                    if (src.fluidLevel == 0) return;   // all fluid fell; cell is now air
                }
            }
        }

        // ── 2. Horizontal equalisation ─────────────────────────────────────────
        // Viscosity maps to a minimum level-difference threshold before spreading:
        //   water  (~0.001 Pa·s) → threshold 1  (spreads freely)
        //   oil    (~0.1   Pa·s) → threshold 2
        //   honey  (~2     Pa·s) → threshold 3
        //   lava   (~100   Pa·s) → threshold 4
        float visc      = Mathf.Max(src.viscosity, 0.001f);
        int   threshold = Mathf.Clamp(Mathf.FloorToInt(Mathf.Log10(visc) + 3f), 1, MaxLevel);

        // Process directions in sweep-alternated order to prevent directional bias.
        for (int d = 0; d < 4 && src.fluidLevel > 0; d++)
        {
            Vector3Int npos = HorizDir(pos, d, _sweep);
            if (!GetBlock(npos, out Chunk nc, out int nx, out int ny, out int nz)) continue;

            ref Block nb = ref nc.blocks[nx, ny, nz];
            if (!CanReceive(ref nb)) continue;

            int diff = src.fluidLevel - nb.fluidLevel;
            if (diff < threshold) continue;

            int flow = diff / 2;
            if (flow <= 0) continue;

            Transfer(ref src, ref nb, flow, chunk.position, nc.position);
            Activate(npos);
            moved = true;
        }

        // Keep this cell active if it still has fluid and actually moved.
        if (moved && src.fluidLevel > 0)
            Activate(pos);
    }

    // ── Transfer & helpers ─────────────────────────────────────────────────────

    /// <summary>Move <paramref name="amount"/> units of fluid from src to dst.</summary>
    private void Transfer(ref Block src, ref Block dst, int amount,
                          Vector3Int srcChunk, Vector3Int dstChunk)
    {
        // Initialise an empty destination with the source fluid's properties.
        if (dst.state != MatterState.Liquid)
        {
            dst.materials      = src.materials;
            dst.state          = MatterState.Liquid;
            dst.viscosity      = src.viscosity;
            dst.fluidDensity   = src.fluidDensity;
            dst.surfaceTension = src.surfaceTension;
        }

        src.fluidLevel = (byte)Mathf.Max(0, src.fluidLevel - amount);
        dst.fluidLevel = (byte)Mathf.Min(MaxLevel, dst.fluidLevel + amount);

        // Clear source cell if it is now empty.
        if (src.fluidLevel == 0)
        {
            src.state     = default;   // MatterState.Solid(0) but materials==null → treated as air
            src.materials = null;
        }

        MarkDirty(srcChunk);
        MarkDirty(dstChunk);
    }

    /// <summary>Returns true if a fluid can flow into this block.</summary>
    private static bool CanReceive(ref Block b)
    {
        // Solid blocks (materials != null, state == Solid) block flow.
        if (b.state == MatterState.Solid && b.materials != null) return false;
        return b.fluidLevel < MaxLevel;
    }

    /// <summary>Returns the d-th horizontal neighbour of pos, biased by sweep direction.</summary>
    private static Vector3Int HorizDir(Vector3Int pos, int d, int sweep)
    {
        switch (d)
        {
            case 0: return new Vector3Int(pos.x + sweep, pos.y, pos.z);
            case 1: return new Vector3Int(pos.x - sweep, pos.y, pos.z);
            case 2: return new Vector3Int(pos.x, pos.y, pos.z + sweep);
            default: return new Vector3Int(pos.x, pos.y, pos.z - sweep);
        }
    }

    private void Activate(Vector3Int p)  => _pending.Add(p);
    private void MarkDirty(Vector3Int c) => _dirty.Add(c);

    // ── Chunk / block lookup ────────────────────────────────────────────────────

    /// <summary>
    /// Converts a world-space position to a registered Chunk and local indices.
    /// Returns false when the position is out of range or chunk is not registered.
    /// </summary>
    private bool GetBlock(Vector3Int world,
                          out Chunk chunk,
                          out int lx, out int ly, out int lz)
    {
        int cs = Chunk.chunkSize;
        int ch = Chunk.chunkHeight;

        int cx = Mathf.FloorToInt((float)world.x / cs);
        int cz = Mathf.FloorToInt((float)world.z / cs);

        lx = world.x - cx * cs;
        ly = world.y;
        lz = world.z - cz * cs;

        if (ly < 0 || ly >= ch) { chunk = null; return false; }

        return _chunks.TryGetValue(new Vector3Int(cx, 0, cz), out chunk)
               && lx >= 0 && lx < cs && lz >= 0 && lz < cs;
    }

    /// <summary>
    /// Walk a freshly-registered chunk and activate any pre-placed fluid blocks.
    /// </summary>
    private void ScanChunkForInitialFluid(Chunk chunk)
    {
        int cs = Chunk.chunkSize;
        int ch = Chunk.chunkHeight;
        for (int x = 0; x < cs; x++)
        for (int y = 0; y < ch; y++)
        for (int z = 0; z < cs; z++)
        {
            if (chunk.blocks[x, y, z].state == MatterState.Liquid &&
                chunk.blocks[x, y, z].fluidLevel > 0)
            {
                int wx = chunk.position.x * cs + x;
                int wz = chunk.position.z * cs + z;
                Activate(new Vector3Int(wx, y, wz));
            }
        }
    }

    // ── Fluid mesh rebuild ─────────────────────────────────────────────────────

    private void RebuildDirtyMeshes()
    {
        foreach (Vector3Int cpos in _dirty)
        {
            if (!_chunks.TryGetValue(cpos, out Chunk chunk)) continue;
            if (!_fluidMesh.TryGetValue(cpos, out MeshFilter mf)) continue;

            Mesh oldMesh = mf.sharedMesh;
            mf.sharedMesh = FluidMeshBuilder.Build(chunk);
            if (oldMesh != null) Destroy(oldMesh);
        }
        _dirty.Clear();
    }
}
