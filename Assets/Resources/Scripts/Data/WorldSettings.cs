using UnityEngine;

/// <summary>
/// Central world configuration. All generators read from here.
/// Edit these constants to adjust world bounds, climate scale, and wrap behaviour.
/// </summary>
public static class WorldSettings
{
    // ── Chunk ───────────────────────────────────────────────────────────────
    /// <summary>Side length of a chunk in blocks (all three axes).</summary>
    public const int ChunkSize = 32;

    // ── Vertical bounds ─────────────────────────────────────────────────────
    /// <summary>Minimum world Y (deepest point, below bedrock).</summary>
    public const int WorldMinY = -1000;

    /// <summary>Maximum world Y (highest possible mountain peak).</summary>
    public const int WorldMaxY = 2000;

    /// <summary>Ocean / sea level in world-space Y.</summary>
    public const int SeaLevel = 0;

    /// <summary>Total vertical block count (convenience).</summary>
    public const int WorldHeight = WorldMaxY - WorldMinY; // 3000

    // ── Horizontal bounds ───────────────────────────────────────────────────
    /// <summary>
    /// Total block width on the X axis. The world wraps east–west:
    /// players crossing ±WorldWidth/2 are teleported to the other side.
    /// </summary>
    public const int WorldWidth = 16384;

    /// <summary>
    /// Total block depth on the Z axis. Bounded north–south by impassable
    /// polar ice walls at Z = ±WorldDepth/2.
    /// </summary>
    public const int WorldDepth = 8192;

    // ── Chunk coordinate helpers ────────────────────────────────────────────

    /// <summary>Returns the chunk-Y index that contains the given world-space Y.</summary>
    public static int ChunkYFromWorldY(int worldY)
        => Mathf.FloorToInt((float)(worldY - WorldMinY) / ChunkSize);

    /// <summary>Returns the local Y (0 – ChunkSize-1) within its chunk for a world Y.</summary>
    public static int LocalYFromWorldY(int worldY)
    {
        int rel = worldY - WorldMinY;
        return ((rel % ChunkSize) + ChunkSize) % ChunkSize;
    }

    /// <summary>Reconstructs world-space Y from a chunk-Y index and a local Y.</summary>
    public static int WorldYFromChunk(int chunkY, int localY)
        => WorldMinY + chunkY * ChunkSize + localY;

    // ── East/West wrap helpers ───────────────────────────────────────────────

    /// <summary>Wraps an integer world-X into [−WorldWidth/2, WorldWidth/2).</summary>
    public static int WrapWorldX(int worldX)
    {
        int half = WorldWidth / 2;
        worldX %= WorldWidth;
        if (worldX >= half)  worldX -= WorldWidth;
        else if (worldX < -half) worldX += WorldWidth;
        return worldX;
    }

    /// <summary>Wraps a float world-X for noise sampling (seamless cylinder).</summary>
    public static float WrapWorldX(float worldX)
    {
        float half = WorldWidth * 0.5f;
        worldX %= WorldWidth;
        if (worldX >= half)  worldX -= WorldWidth;
        else if (worldX < -half) worldX += WorldWidth;
        return worldX;
    }

    // ── Climate helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns a 0–1 latitude factor: 0 = equator (Z = 0), 1 = pole (Z = ±WorldDepth/2).
    /// Used to drive temperature falloff from equator to poles.
    /// </summary>
    public static float LatitudeFactor(float worldZ)
        => Mathf.Clamp01(Mathf.Abs(worldZ) / (WorldDepth * 0.5f));
}
