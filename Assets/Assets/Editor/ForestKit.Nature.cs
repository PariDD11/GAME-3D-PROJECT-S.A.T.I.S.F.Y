using System;
using System.Collections.Generic;
using UnityEngine;

public static partial class ForestKit
{
    // ------------------------------------------------------------------ primitif organik
    static float R01(int a, int b, int seed) { return Hash(a, b, seed); }

    /// <summary>Tabung mengikuti lintasan titik, jari-jari berubah mulus. buttress &gt; 0 = akar papan di pangkal.</summary>
    public static void Tube(GeoBag b, int slot, IList<Vector3> pts, IList<float> rad, int segs, int seed, float buttress = 0f, bool capEnd = true)
    {
        var rings = new List<Vector3[]>();
        int n = pts.Count;
        for (int i = 0; i < n; i++)
        {
            Vector3 d = i == 0 ? pts[1] - pts[0] : (i == n - 1 ? pts[n - 1] - pts[n - 2] : pts[i + 1] - pts[i - 1]);
            d = d.normalized;
            Vector3 reff = Mathf.Abs(d.z) < 0.9f ? Vector3.forward : Vector3.right;
            Vector3 W = (reff - d * Vector3.Dot(reff, d)).normalized;
            Vector3 U = Vector3.Cross(d, W);
            float t = n > 1 ? i / (float)(n - 1) : 0f;
            var ring = new Vector3[segs];
            for (int j = 0; j < segs; j++)
            {
                float a = j * Mathf.PI * 2f / segs;
                float lobe = buttress * Mathf.Pow(1f - t, 4f) * (0.5f + 0.5f * Mathf.Sin(a * 3f + seed * 1.3f));
                float jit = (R01(j, i, seed) - 0.5f) * 0.10f;
                float r = rad[i] * (1f + lobe + jit);
                ring[j] = pts[i] + (U * Mathf.Sin(a) + W * Mathf.Cos(a)) * r;
            }
            rings.Add(ring);
        }
        b.Loft(slot, rings, false, capEnd, true);
    }

    public static void Limb(GeoBag b, int slot, Vector3 a, Vector3 c, float r0, float r1, int seed, int segs = 6, float bend = 0.12f)
    {
        Vector3 d = c - a;
        Vector3 side = Vector3.Cross(d, Vector3.up);
        if (side.sqrMagnitude < 1e-4f) side = Vector3.right;
        side = side.normalized * (d.magnitude * bend * (R01(seed, 1, 5) - 0.3f));
        var pts = new List<Vector3> { a, (a + c) * 0.5f + side + Vector3.up * d.magnitude * 0.05f, c };
        var rad = new List<float> { r0, (r0 + r1) * 0.5f, r1 };
        Tube(b, slot, pts, rad, segs, seed, 0f, true);
    }

    /// <summary>Gumpalan daun/batu tidak beraturan (bola dengan noise).</summary>
    public static void Blob(GeoBag b, int slot, Vector3 c, Vector3 r, int seed, float lump = 0.25f, int rings = 5, int segs = 9, float flatBottom = 0f)
    {
        var R = new List<Vector3[]>();
        for (int i = 0; i < rings; i++)
        {
            float phi = -Mathf.PI / 2f + (i + 0.5f) / rings * Mathf.PI;
            var ring = new Vector3[segs];
            for (int j = 0; j < segs; j++)
            {
                float a = j * Mathf.PI * 2f / segs;
                float cx = Mathf.Cos(phi) * Mathf.Sin(a), cy = Mathf.Sin(phi), cz = Mathf.Cos(phi) * Mathf.Cos(a);
                float nz = Mathf.PerlinNoise(seed * 1.7f + 50f + cx * 1.8f + cz * 1.1f, seed * 0.9f + 50f + cy * 1.9f + cx * 0.7f);
                float f = 1f + lump * (nz - 0.5f) * 2f;
                float y = cy * r.y * f;
                if (flatBottom > 0f) y = Mathf.Max(y, -r.y * flatBottom);
                ring[j] = c + new Vector3(cx * r.x * f, y, cz * r.z * f);
            }
            R.Add(ring);
        }
        b.Loft(slot, R, true, true, true);
    }

    static Vector3 Dir(float yawDeg, float up)
    {
        float a = yawDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(a), up, Mathf.Cos(a)).normalized;
    }

    // ------------------------------------------------------------------ pohon (bentuk dibuat TreeGen: batang berakar melebar, dahan terlihat, mahkota gumpalan halus)
    /// <summary>Ek / pohon berdaun lebar. leaf = LeafRed/LeafOrange -> maple musim gugur; selain itu daun hijau dengan nada gelap-tengah-terang.</summary>
    public static void Oak(GeoBag b, int seed, int leaf = LeafGreen, int leafAlt = LeafYellow, float scale = 1f)
    {
        bool autumn = leaf == LeafRed || leaf == LeafOrange;
        TreeGen.Pal pal = autumn ? TreeGen.Autumn : TreeGen.Forest;
        if (!autumn && leaf != LeafGreen) pal.mid = leaf;
        if (!autumn && leafAlt >= 0) pal.accent = leafAlt;
        if (autumn) TreeGen.Maple(b, seed, pal, scale * 1.2f, seed);
        else TreeGen.Oak(b, seed, pal, scale * 1.2f, seed);
    }

    public static void Spruce(GeoBag b, int seed, float scale = 1f)
    {
        float h = (9f + R01(seed, 1, 31) * 2.5f) * scale;
        TreeGen.Spruce(b, seed, TreeGen.ForestPine, h, 2.7f * scale, 9);
    }

    public static void Birch(GeoBag b, int seed, float scale = 1f)
    {
        TreeGen.Birch(b, seed, new TreeGen.Pal(BarkBirch, LeafGreen, LeafLight, LeafYellow, LeafYellow, -1), scale * 1.15f);
    }

    public static void DeadTree(GeoBag b, int seed, float scale = 1f)
    {
        TreeGen.Dead(b, seed, new TreeGen.Pal(BarkDead, -1, -1, -1, -1, -1), scale * 1.05f);
    }

    public static void Bush(GeoBag b, int seed, int leaf = LeafGreen, float scale = 1f)
    {
        TreeGen.Bush(b, seed, new TreeGen.Pal(Bark, Ivy, leaf, leaf, Sl.Blossom, -1), scale * 1.1f, 3);
    }

    static void UpQuad(GeoBag b, int slot, Vector3 a, Vector3 b2, Vector3 c, Vector3 d)
    {
        if (Vector3.Cross(b2 - a, c - a).y < 0f) b.Quad(slot, d, c, b2, a); else b.Quad(slot, a, b2, c, d);
    }

    /// <summary>Pakis: tujuh helai daun melengkung, tepi bergerigi, ujung menunduk.</summary>
    public static void Fern(GeoBag b, int seed)
    {
        int fronds = 7;
        for (int k = 0; k < fronds; k++)
        {
            float yaw = k * (360f / fronds) + R01(seed, k, 71) * 20f;
            float len = 1.05f + R01(seed, k, 72) * 0.5f;
            float rise = 0.6f + R01(seed, k, 73) * 0.4f;
            Vector3 dir = Dir(yaw, 0f);
            Vector3 side = Vector3.Cross(dir, Vector3.up).normalized;
            var P = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                float t = i / 3f;
                P[i] = new Vector3(0f, 0.04f, 0f) + dir * (len * t * (0.9f + 0.1f * t)) + Vector3.up * (rise * (Mathf.Sin(t * 2.3f) * 0.8f - 0.32f * t * t));
            }
            float[] w = { 0.05f, 0.2f, 0.15f, 0.04f };
            for (int sgn = -1; sgn <= 1; sgn += 2)
            {
                for (int i = 1; i < 4; i++)
                {
                    int sl = (k + i) % 3 == 0 ? Ivy : LeafGreen;
                    Vector3 o0 = P[i - 1] + side * (sgn * w[i - 1]) + Vector3.down * 0.03f, o1 = P[i] + side * (sgn * w[i]) + Vector3.down * 0.05f;
                    UpQuad(b, sl, P[i - 1], P[i], o1, o0);
                }
            }
        }
    }

    // ------------------------------------------------------------------ batu & detail alam
    public static void Boulder(GeoBag b, int seed, float s = 1f, bool mossy = true)
    {
        Blob(b, Rock, new Vector3(0, 0.55f * s, 0), new Vector3(1.4f * s, 0.95f * s, 1.15f * s), seed, 0.38f, 6, 10, 0.55f);
        if (R01(seed, 1, 81) > 0.45f)
            Blob(b, Rock, new Vector3(0.9f * s, 0.3f * s, 0.5f * s), new Vector3(0.6f * s, 0.45f * s, 0.55f * s), seed + 4, 0.35f, 4, 8, 0.5f);
        if (mossy)
            Blob(b, Moss, new Vector3(-0.1f * s, 1.0f * s, 0.05f * s), new Vector3(1.0f * s, 0.32f * s, 0.85f * s), seed + 9, 0.3f, 4, 9, 0.2f);
    }

    public static void Stump(GeoBag b, int seed)
    {
        var pts = new List<Vector3> { new Vector3(0, -0.2f, 0), new Vector3(0, 0.45f, 0.02f), new Vector3(0, 0.7f, 0) };
        var rad = new List<float> { 0.5f, 0.4f, 0.37f };
        Tube(b, Bark, pts, rad, 9, seed, 0.6f, true);
        Blob(b, Moss, new Vector3(0.1f, 0.68f, 0.05f), new Vector3(0.28f, 0.1f, 0.24f), seed, 0.3f, 3, 7, 0.3f);
    }

    public static void FallenLog(GeoBag b, int seed)
    {
        var pts = new List<Vector3>(); var rad = new List<float>();
        for (int i = 0; i <= 5; i++) { float t = i / 5f; pts.Add(new Vector3(0f, 0.38f + Mathf.Sin(t * 3f) * 0.04f, -2.2f + t * 4.4f)); rad.Add(Mathf.Lerp(0.38f, 0.28f, t)); }
        Tube(b, Bark, pts, rad, 8, seed, 0f, true);
        Blob(b, Moss, new Vector3(0.05f, 0.68f, -0.4f), new Vector3(0.3f, 0.12f, 1.2f), seed + 3, 0.25f, 3, 8, 0.3f);
        Limb(b, Bark, new Vector3(0, 0.5f, 0.8f), new Vector3(0.7f, 0.9f, 1.3f), 0.08f, 0.03f, seed, 4);
    }

    public static void CrystalCluster(GeoBag b, int seed)
    {
        for (int k = 0; k < 5; k++)
        {
            Vector3 a = new Vector3((R01(seed, k, 91) - 0.5f) * 0.6f, -0.05f, (R01(seed, k, 92) - 0.5f) * 0.6f);
            Vector3 c = a + Dir(k * 72f + 10f, 2.2f - R01(seed, k, 93) * 0.8f) * (0.9f + R01(seed, k, 94) * 1.3f);
            Limb(b, Crystal, a, c, 0.16f, 0.015f, seed + k, 5, 0.02f);
        }
        Blob(b, Rock, new Vector3(0, 0.1f, 0), new Vector3(0.6f, 0.28f, 0.55f), seed, 0.3f, 3, 8, 0.2f);
    }

    public static void GlowMushrooms(GeoBag b, int seed)
    {
        for (int k = 0; k < 4; k++)
        {
            float x = (R01(seed, k, 101) - 0.5f) * 0.9f, z = (R01(seed, k, 102) - 0.5f) * 0.9f, h = 0.25f + R01(seed, k, 103) * 0.35f;
            b.Cyl(Sl.Plaster, new Vector3(x, 0f, z), 0.035f, h, 6, 0.025f, true);
            Blob(b, Crystal, new Vector3(x, h, z), new Vector3(0.14f + 0.06f * k % 3 * 0.3f, 0.07f, 0.14f), seed + k, 0.1f, 3, 7, 0.1f);
        }
    }

    public static void Gravestone(GeoBag b, int seed, bool cross)
    {
        float tilt = (R01(seed, 1, 111) - 0.5f) * 0.5f;
        Vector3 a = new Vector3(0, -0.1f, 0), c = new Vector3(tilt, 0.95f, 0);
        b.Beam(Sl.Stone, a, c, 0.55f, 0.13f, Vector3.forward);
        if (cross) b.Beam(Sl.Stone, new Vector3(tilt * 0.8f - 0.28f, 0.72f, 0f), new Vector3(tilt * 0.8f + 0.28f, 0.72f, 0f), 0.14f, 0.13f, Vector3.forward);
        else Blob(b, Sl.Stone, new Vector3(tilt, 0.96f, 0), new Vector3(0.29f, 0.14f, 0.07f), seed, 0.05f, 3, 8, 0f);
        Blob(b, Moss, new Vector3(0, 0.05f, 0.05f), new Vector3(0.5f, 0.12f, 0.3f), seed, 0.3f, 3, 7, 0.3f);
    }

    public static void StandingStone(GeoBag b, int seed, float h = 3.2f)
    {
        float tilt = (R01(seed, 1, 121) - 0.5f) * 0.5f;
        b.Beam(Rock, new Vector3(0, -0.4f, 0), new Vector3(tilt, h, 0), 1.2f, 0.8f, Vector3.forward);
        Blob(b, Moss, new Vector3(tilt * 0.4f, 0.9f, 0.1f), new Vector3(0.7f, 0.5f, 0.5f), seed, 0.3f, 3, 8, 0.3f);
        b.Box(Crystal, new Vector3(tilt * 0.6f, h * 0.55f, 0.43f), new Vector3(0.14f, 0.7f, 0.04f));
        b.Box(Crystal, new Vector3(tilt * 0.6f, h * 0.62f, 0.43f), new Vector3(0.5f, 0.12f, 0.04f));
    }

    /// <summary>Pohon Kuno raksasa: batang berakar papan, dahan besar, mahkota bergumpal dan kelopak bercahaya menggantung.</summary>
    public static void AncientTree(GeoBag b, int seed)
    {
        TreeGen.Ancient(b, seed, TreeGen.Forest, 1.6f);
        Vector3 top = new Vector3(0.5f, 19f, -0.2f);
        for (int k = 0; k < 9; k++)           // kelopak bercahaya menggantung
        {
            Vector3 p = top + Dir(k * 40f, 0f) * (4f + R01(seed, k, 133) * 4f) + new Vector3(0, -2.5f - R01(seed, k, 134) * 5f, 0);
            Blob(b, Crystal, p, new Vector3(0.22f, 0.28f, 0.22f), seed + k, 0.15f, 3, 6, 0f);
        }
    }
}

public static partial class ForestKit
{
    /// <summary>Dipakai harness uji: bangun objek berdasarkan nama.</summary>
    public static void Test(GeoBag b, string name, int seed)
    {
        switch (name)
        {
            case "oak": Oak(b, seed); break;
            case "maple": Oak(b, seed, LeafOrange, LeafRed); break;
            case "spruce": Spruce(b, seed); break;
            case "birch": Birch(b, seed); break;
            case "dead": DeadTree(b, seed); break;
            case "bush": Bush(b, seed); break;
            case "fern": Fern(b, seed); break;
            case "boulder": Boulder(b, seed); break;
            case "stump": Stump(b, seed); break;
            case "log": FallenLog(b, seed); break;
            case "crystal": CrystalCluster(b, seed); break;
            case "mush": GlowMushrooms(b, seed); break;
            case "grave": Gravestone(b, seed, seed % 2 == 0); break;
            case "stone": StandingStone(b, seed); break;
            case "ancient": AncientTree(b, seed); break;
            default: TestBuild(b, name, seed); break;
        }
    }
    static void TestBuildImpl(GeoBag b, string name, int seed) { TestBuildImpl2(b, name, seed); }
    static void TestBuild(GeoBag b, string name, int seed) { TestBuildImpl(b, name, seed); }
}
