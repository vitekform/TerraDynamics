using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Builds a transparent Mesh that visualises all fluid blocks in a Chunk.
///
/// Face-culling rules (similar to the solid mesh, adapted for fluids):
///   Top    – rendered when the block above is NOT liquid (water surface).
///   Bottom – rendered when the block below is NOT solid (floating fluid body).
///   Sides  – rendered when the lateral neighbour is NOT solid AND NOT liquid
///             (you can see the fluid wall from outside).
///
/// Fill height:
///   The top face and the upper edge of side faces sit at y + fill,
///   where fill = fluidLevel / MaxLevel.  When the block above is also
///   liquid the upper edge is forced to y + 1 so the column is seamless.
/// </summary>
public static class FluidMeshBuilder
{
    // Reusable lists; static to avoid per-call GC pressure.
    private static readonly List<Vector3> Verts = new List<Vector3>();
    private static readonly List<int>     Tris  = new List<int>();
    private static readonly List<Vector2> UVs   = new List<Vector2>();
    private static readonly List<Color>   Cols  = new List<Color>();

    /// <summary>
    /// Build and return a new Mesh representing all fluid in <paramref name="chunk"/>.
    /// Returns an empty Mesh when there is no fluid.
    /// </summary>
    public static Mesh Build(Chunk chunk)
    {
        Verts.Clear();
        Tris.Clear();
        UVs.Clear();
        Cols.Clear();

        int    cs = Chunk.chunkSize;
        Block[,,] b = chunk.blocks;

        for (int x = 0; x < cs; x++)
        for (int y = 0; y < cs; y++)
        for (int z = 0; z < cs; z++)
        {
            Block bl = b[x, y, z];
            if (bl.state != MatterState.Liquid || bl.fluidLevel <= 0) continue;

            float fill = bl.fluidLevel / (float)FluidSimulator.MaxLevel;
            Color tint = bl.materials != null ? bl.materials.color : Color.blue;
            tint.a = 0.78f;   // translucency

            // Is the block directly above also liquid?
            bool aboveLiquid = y + 1 < cs && b[x, y + 1, z].state == MatterState.Liquid;

            // Top edge for side faces: extend to y+1 when part of a submerged column.
            float sideTop = aboveLiquid ? y + 1f : y + fill;

            // ── Top face (water surface) ──────────────────────────────────────
            if (!aboveLiquid)
                AddTopFace(x, y + fill, z, tint);

            // ── Bottom face ───────────────────────────────────────────────────
            // Render when below is not solid (e.g. fluid hanging in air, or
            // the underside of a submerged block visible through a gap).
            if (NeedBottomFace(b, x, y - 1, z, cs))
                AddBottomFace(x, y, z, tint);

            // ── Side faces ────────────────────────────────────────────────────
            if (NeedSideFace(b, x - 1, y, z, cs))
                AddLeftFace(x,     y, sideTop, z, tint);   // -X
            if (NeedSideFace(b, x + 1, y, z, cs))
                AddRightFace(x + 1, y, sideTop, z, tint);  // +X
            if (NeedSideFace(b, x, y, z - 1, cs))
                AddBackFace(x, y, sideTop, z,     tint);   // -Z
            if (NeedSideFace(b, x, y, z + 1, cs))
                AddFrontFace(x, y, sideTop, z + 1, tint);  // +Z
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(Verts);
        mesh.SetTriangles(Tris, 0);
        mesh.SetUVs(0, UVs);
        mesh.SetColors(Cols);
        mesh.RecalculateNormals();
        return mesh;
    }

    // ── Face-culling helpers ───────────────────────────────────────────────────

    /// <summary>True when the block at (nx,ny,nz) is not solid → show top face.</summary>
    private static bool NeedBottomFace(Block[,,] b, int nx, int ny, int nz, int cs)
    {
        if (ny < 0) return true;                             // below world floor
        if (nx < 0 || nx >= cs || nz < 0 || nz >= cs) return true; // chunk edge
        if (ny >= cs) return false;
        Block nb = b[nx, ny, nz];
        return !(nb.state == MatterState.Solid && nb.materials != null);
    }

    /// <summary>True when the lateral neighbour is air → show the fluid wall.</summary>
    private static bool NeedSideFace(Block[,,] b, int nx, int ny, int nz, int cs)
    {
        // Out-of-bounds → render (handles chunk borders gracefully)
        if (nx < 0 || nx >= cs || ny < 0 || ny >= cs || nz < 0 || nz >= cs) return true;
        Block nb = b[nx, ny, nz];
        if (nb.state == MatterState.Solid && nb.materials != null) return false; // solid wall
        if (nb.state == MatterState.Liquid) return false;                        // another fluid
        return true;   // air
    }

    // ── Quad helpers ───────────────────────────────────────────────────────────
    // Each face = 4 verts (CCW from outside) + 2 triangles + 4 UVs + 4 colors.

    private static void AddTopFace(float x, float topY, float z, Color c)
    {
        int v = Verts.Count;
        Verts.Add(new Vector3(x,     topY, z));
        Verts.Add(new Vector3(x + 1, topY, z));
        Verts.Add(new Vector3(x + 1, topY, z + 1));
        Verts.Add(new Vector3(x,     topY, z + 1));
        Tris.AddRange(new[] { v, v + 2, v + 1, v, v + 3, v + 2 });
        AddUVsAndColors(c);
    }

    private static void AddBottomFace(float x, float y, float z, Color c)
    {
        int v = Verts.Count;
        Verts.Add(new Vector3(x,     y, z));
        Verts.Add(new Vector3(x + 1, y, z));
        Verts.Add(new Vector3(x + 1, y, z + 1));
        Verts.Add(new Vector3(x,     y, z + 1));
        Tris.AddRange(new[] { v, v + 1, v + 2, v, v + 2, v + 3 });
        AddUVsAndColors(c);
    }

    // -X face
    private static void AddLeftFace(float x, float bot, float top, float z, Color c)
    {
        int v = Verts.Count;
        Verts.Add(new Vector3(x, bot, z));
        Verts.Add(new Vector3(x, top, z));
        Verts.Add(new Vector3(x, top, z + 1));
        Verts.Add(new Vector3(x, bot, z + 1));
        Tris.AddRange(new[] { v, v + 2, v + 1, v, v + 3, v + 2 });
        AddUVsAndColors(c);
    }

    // +X face
    private static void AddRightFace(float x, float bot, float top, float z, Color c)
    {
        int v = Verts.Count;
        Verts.Add(new Vector3(x, bot, z));
        Verts.Add(new Vector3(x, top, z));
        Verts.Add(new Vector3(x, top, z + 1));
        Verts.Add(new Vector3(x, bot, z + 1));
        Tris.AddRange(new[] { v, v + 1, v + 2, v, v + 2, v + 3 });
        AddUVsAndColors(c);
    }

    // -Z face
    private static void AddBackFace(float x, float bot, float top, float z, Color c)
    {
        int v = Verts.Count;
        Verts.Add(new Vector3(x,     bot, z));
        Verts.Add(new Vector3(x,     top, z));
        Verts.Add(new Vector3(x + 1, top, z));
        Verts.Add(new Vector3(x + 1, bot, z));
        Tris.AddRange(new[] { v, v + 2, v + 1, v, v + 3, v + 2 });
        AddUVsAndColors(c);
    }

    // +Z face
    private static void AddFrontFace(float x, float bot, float top, float z, Color c)
    {
        int v = Verts.Count;
        Verts.Add(new Vector3(x,     bot, z));
        Verts.Add(new Vector3(x,     top, z));
        Verts.Add(new Vector3(x + 1, top, z));
        Verts.Add(new Vector3(x + 1, bot, z));
        Tris.AddRange(new[] { v, v + 1, v + 2, v, v + 2, v + 3 });
        AddUVsAndColors(c);
    }

    private static void AddUVsAndColors(Color c)
    {
        UVs.Add(Vector2.zero);
        UVs.Add(Vector2.up);
        UVs.Add(Vector2.one);
        UVs.Add(Vector2.right);
        Cols.Add(c); Cols.Add(c); Cols.Add(c); Cols.Add(c);
    }
}
