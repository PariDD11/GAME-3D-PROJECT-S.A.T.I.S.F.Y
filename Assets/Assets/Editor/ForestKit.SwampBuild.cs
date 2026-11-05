using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Geometri Rawa Terkutuk: pohon rawa, rumah panggung, dermaga, perahu, gubuk penyihir, lingkaran ritual, makam, kapel, menara nekromancer.</summary>
public static partial class ForestKit
{
    static void Hang(GeoBag b, Vector3 from, float len, int seed, int slot = Ivy, float w = 0.06f)
    {
        Vector3 to = from + new Vector3((R01(seed, 1, 811) - 0.5f) * 0.3f, -len, (R01(seed, 2, 812) - 0.5f) * 0.3f);
        b.Beam(slot, from, to, w * 2.2f, w * 0.6f, Vector3.right);
    }

    static void Skull(GeoBag b, Vector3 at, float s)
    {
        b.Ball(Sl.Marble, at, 0.15f * s, 4, 7, 0.95f);
        b.Box(Sl.Marble, at + new Vector3(0, -0.14f * s, 0.06f * s), new Vector3(0.14f * s, 0.07f * s, 0.12f * s));
        b.Box(Sl.Dark, at + new Vector3(-0.05f * s, 0.01f * s, 0.12f * s), new Vector3(0.05f * s, 0.05f * s, 0.03f * s));
        b.Box(Sl.Dark, at + new Vector3(0.05f * s, 0.01f * s, 0.12f * s), new Vector3(0.05f * s, 0.05f * s, 0.03f * s));
    }

    // ------------------------------------------------------------------ pohon
    public static void Mangrove(GeoBag b, int seed, float s = 1f)
    {
        float h = (5.2f + R01(seed, 1, 801) * 2.2f) * s;
        float lean = (R01(seed, 2, 802) - 0.5f) * 1.2f * s;
        var pts = new List<Vector3>(); var rad = new List<float>();
        for (int i = 0; i <= 5; i++)
        {
            float t = i / 5f;
            pts.Add(new Vector3(lean * t * t, 1.5f * s + (h - 1.5f * s) * t, 0f));
            rad.Add(Mathf.Lerp(0.34f, 0.16f, t) * s);
        }
        Tube(b, Bark, pts, rad, 7, seed, 0.1f, true);
        int roots = 6 + (seed % 3);
        for (int k = 0; k < roots; k++)
        {
            float a = (k * 360f / roots + R01(seed, k, 803) * 30f) * Deg;
            float rr = (1.7f + R01(seed, k, 804) * 1.1f) * s;
            Vector3 from = new Vector3(Mathf.Sin(a) * 0.18f, 1.9f * s, Mathf.Cos(a) * 0.18f);
            Vector3 to = new Vector3(Mathf.Sin(a) * rr, -0.5f, Mathf.Cos(a) * rr);
            Limb(b, Bark, from, to, 0.13f * s, 0.05f * s, seed + k, 5, 0.25f);
        }
        Vector3 top = pts[pts.Count - 1];
        for (int k = 0; k < 4; k++)
            Blob(b, LeafGreen, top + new Vector3((R01(seed, k, 805) - 0.5f) * 2.4f * s, -0.4f * s + (k % 2) * 0.5f * s, (R01(seed, k, 806) - 0.5f) * 2.4f * s),
                new Vector3(1.9f, 0.95f, 1.9f) * s, seed + k, 0.3f, 4, 8, 0f);
        for (int k = 0; k < 5; k++)
            Hang(b, top + new Vector3((R01(seed, k, 807) - 0.5f) * 2.6f * s, -0.7f * s, (R01(seed, k, 808) - 0.5f) * 2.6f * s), (1.2f + R01(seed, k, 809) * 1.4f) * s, seed + k);
        MangroveArt(b, seed, s, top);
    }

    public static void Willow(GeoBag b, int seed, float s = 1f)
    {
        float h = (4.4f + R01(seed, 1, 821) * 1.4f) * s;
        var pts = new List<Vector3>(); var rad = new List<float>();
        float lx = (R01(seed, 2, 822) - 0.5f) * 1.4f * s, lz = (R01(seed, 3, 823) - 0.5f) * 1.4f * s;
        for (int i = 0; i <= 5; i++)
        {
            float t = i / 5f;
            pts.Add(new Vector3(lx * t * t, -0.3f + (h + 0.3f) * t, lz * t * t));
            rad.Add(Mathf.Lerp(0.55f, 0.25f, t) * s);
        }
        Tube(b, Bark, pts, rad, 8, seed, 0.5f, true);
        Vector3 top = pts[pts.Count - 1];
        int n = 5;
        for (int k = 0; k < n; k++)
        {
            float a = (k * 360f / n + R01(seed, k, 824) * 40f) * Deg;
            Vector3 c = top + new Vector3(Mathf.Sin(a) * 2.0f * s, 0.2f * s + R01(seed, k, 825) * 0.6f * s, Mathf.Cos(a) * 2.0f * s);
            Limb(b, Bark, top - Vector3.up * 0.8f * s, c, 0.2f * s, 0.07f * s, seed + k, 5, 0.1f);
            Blob(b, LeafGreen, c, new Vector3(1.3f, 0.7f, 1.3f) * s, seed + k, 0.3f, 3, 7, 0f);
            for (int j = 0; j < 5; j++)
            {
                float aa = a + (j - 2) * 0.35f;
                Vector3 from = c + new Vector3(Mathf.Sin(aa) * 0.9f * s, -0.2f * s, Mathf.Cos(aa) * 0.9f * s);
                Hang(b, from, (2.6f + R01(seed, k * 7 + j, 826) * 1.8f) * s, seed + k * 7 + j, LeafGreen, 0.09f);
            }
        }
        Blob(b, LeafGreen, top + Vector3.up * 0.3f * s, new Vector3(1.6f, 0.8f, 1.6f) * s, seed + 9, 0.3f, 3, 8, 0f);
        WillowArt(b, seed, s, top);
    }

    public static void BaldCypress(GeoBag b, int seed, float s = 1f)
    {
        float h = (9.5f + R01(seed, 1, 831) * 3f) * s;
        var pts = new List<Vector3>(); var rad = new List<float>();
        for (int i = 0; i <= 7; i++)
        {
            float t = i / 7f;
            pts.Add(new Vector3((R01(seed, i, 832) - 0.5f) * 0.25f * t, -0.4f + (h + 0.4f) * t, 0f));
            rad.Add(Mathf.Lerp(0.55f, 0.1f, Mathf.Pow(t, 0.8f)) * s);
        }
        Tube(b, Bark, pts, rad, 8, seed, 0.9f, true);
        for (int k = 0; k < 7; k++)
        {
            float a = (k * 51f + R01(seed, k, 833) * 40f) * Deg;
            float rr = (1.0f + R01(seed, k, 834) * 1.8f) * s;
            b.Cone(Bark, new Vector3(Mathf.Sin(a) * rr, -0.3f, Mathf.Cos(a) * rr), 0.17f * s, (0.5f + R01(seed, k, 835) * 0.6f) * s, 5);
        }
        Vector3 top = pts[pts.Count - 1];
        for (int k = 0; k < 4; k++)
            Blob(b, LeafPine, new Vector3((R01(seed, k, 836) - 0.5f) * 0.8f * s, h * (0.6f + 0.13f * k), (R01(seed, k, 837) - 0.5f) * 0.8f * s),
                new Vector3(1.6f - k * 0.25f, 0.9f, 1.6f - k * 0.25f) * s, seed + k, 0.35f, 3, 7, 0f);
        for (int k = 0; k < 6; k++)
            Hang(b, new Vector3((R01(seed, k, 838) - 0.5f) * 2f * s, h * (0.55f + 0.08f * (k % 3)), (R01(seed, k, 839) - 0.5f) * 2f * s), (1.4f + R01(seed, k, 840) * 1.8f) * s, seed + k);
        CypressArt(b, seed, s, h);
    }

    public static void SwampDeadTree(GeoBag b, int seed, float s = 1f)
    {
        DeadTree(b, seed, s);
        for (int k = 0; k < 4; k++)
            Hang(b, new Vector3((R01(seed, k, 841) - 0.5f) * 1.6f * s, (2.6f + k * 0.5f) * s, (R01(seed, k, 842) - 0.5f) * 1.6f * s), (0.9f + R01(seed, k, 843) * 1.2f) * s, seed + k, Ivy, 0.05f);
    }

    public static void Cattail(GeoBag b, int seed)
    {
        for (int k = 0; k < 14; k++)
        {
            float x = (R01(seed, k, 851) - 0.5f) * 1.4f, z = (R01(seed, k, 852) - 0.5f) * 1.4f, h = 1.4f + R01(seed, k, 853) * 1.1f;
            Vector3 lean = new Vector3((R01(seed, k, 854) - 0.5f) * 0.35f, 0, (R01(seed, k, 855) - 0.5f) * 0.35f);
            b.Beam(LeafGreen, new Vector3(x, -0.1f, z), new Vector3(x, h, z) + lean, 0.05f, 0.015f, Vector3.right);
            if (k % 2 == 0) b.Cyl(Bark, new Vector3(x, h - 0.35f, z) + lean, 0.045f, 0.36f, 5, 0.035f, true);
        }
    }

    public static void LilyPads(GeoBag b, int seed)
    {
        for (int k = 0; k < 8; k++)
        {
            float x = (R01(seed, k, 861) - 0.5f) * 3.6f, z = (R01(seed, k, 862) - 0.5f) * 3.6f, r = 0.22f + R01(seed, k, 863) * 0.3f;
            b.Disc(LeafGreen, new Vector3(x, 0.07f, z), r, 9, 0f);
            if (k % 4 == 0) Blob(b, LeafOrange, new Vector3(x + 0.05f, 0.14f, z), new Vector3(0.1f, 0.08f, 0.1f), seed + k, 0.1f, 3, 6, 0f);
        }
    }

    public static void BogLotus(GeoBag b, int seed)
    {
        for (int k = 0; k < 5; k++)
        {
            float a = k * 72f * Deg;
            b.Beam(LeafGreen, new Vector3(Mathf.Sin(a) * 0.1f, 0f, Mathf.Cos(a) * 0.1f), new Vector3(Mathf.Sin(a) * 0.35f, 0.35f, Mathf.Cos(a) * 0.35f), 0.2f, 0.02f, Vector3.up);
        }
        Blob(b, Crystal, new Vector3(0, 0.38f, 0), new Vector3(0.14f, 0.12f, 0.14f), seed, 0.1f, 3, 7, 0f);
    }

    // ------------------------------------------------------------------ desa nelayan
    /// <summary>Rumah panggung. Depan = +Z. Lantai di y = 1.5. ruin 0 = dihuni (jendela menyala).</summary>
    public static void StiltHouse(GeoBag b, int seed, float w, float d, float ruin)
    {
        float pf = 1.5f, wh = 2.7f, th = 0.2f;
        int wood = ruin > 0.3f ? Weathered : Sl.Wood;
        float y0 = pf + 0.1f, y1 = pf + wh;
        for (int sx = -1; sx <= 1; sx++)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                Vector3 p = new Vector3(sx * (w / 2f + 0.3f), 0f, sz * (d / 2f + 0.3f));
                if (ruin > 0.5f && R01(seed, sx * 3 + sz, 871) > 0.6f) continue;
                b.Box(Sl.Wood, p + new Vector3(0, (pf - 3.2f) / 2f + 0.1f, 0), new Vector3(0.28f, pf + 3.2f, 0.28f));
            }
        b.Box(Sl.Wood, new Vector3(0, pf - 0.5f, 0), new Vector3(w + 0.8f, 0.14f, 0.16f));
        b.Box(wood, new Vector3(0, pf, 0), new Vector3(w + 0.9f, 0.2f, d + 0.9f));
        b.Box(wood, new Vector3(0, pf, d / 2f + 1.0f), new Vector3(w * 0.6f, 0.2f, 1.3f));
        // tangga turun dari beranda
        for (int i = 0; i < 4; i++)
            b.Box(wood, new Vector3(0.0f, pf - 0.3f * (i + 1) + 0.05f, d / 2f + 1.9f + 0.42f * i), new Vector3(1.1f, 0.1f, 0.4f));
        for (int sx = -1; sx <= 1; sx += 2)
            b.Beam(Sl.Wood, new Vector3(sx * 0.6f, pf + 0.1f, d / 2f + 1.7f), new Vector3(sx * 0.6f, pf - 1.25f, d / 2f + 3.4f), 0.08f, 0.08f);

        var front = new List<Op> { new Op(0f, 1.0f, y0, y0 + 2.0f) };
        var winX = new List<float>();
        foreach (float sx in new[] { -w * 0.32f, w * 0.32f })
            if (Mathf.Abs(sx) > 1.3f && Mathf.Abs(sx) < w / 2f - 0.6f) { front.Add(new Op(sx, 0.8f, y0 + 0.85f, y0 + 1.75f)); winX.Add(sx); }
        float xa = -w / 2f - th / 2f, xb = w / 2f + th / 2f;
        Wall(b, wood, xa, xb, d / 2f, y0, y1, th, front, ruin, seed + 1);
        Wall(b, wood, xa, xb, -d / 2f, y0, y1, th, new List<Op> { new Op(0f, 0.8f, y0 + 0.85f, y0 + 1.75f) }, ruin, seed + 2);
        for (int sdx = -1; sdx <= 1; sdx += 2)
        {
            b.Push(new Vector3(sdx * w / 2f, 0f, 0f), 90f);
            Wall(b, wood, -d / 2f + th / 2f, d / 2f - th / 2f, 0f, y0, y1, th, new List<Op> { new Op(0f, 0.7f, y0 + 0.85f, y0 + 1.75f) }, ruin, seed + 3 + sdx);
            b.Pop();
        }
        if (ruin < 0.6f)
        {
            b.Box(Sl.Wood, new Vector3(0, y0 + 1.0f, d / 2f - 0.06f), new Vector3(0.95f, 2.0f, 0.06f));
            if (ruin < 0.15f)
                foreach (float sx in winX) b.Box(Sl.Glow, new Vector3(sx, y0 + 1.3f, d / 2f - 0.08f), new Vector3(0.62f, 0.72f, 0.04f));
        }
        if (ruin < 0.7f)
        {
            b.Gable(ruin > 0.3f ? OldThatch : Sl.Thatch, wood, Sl.Wood, new Vector3(0, y1, 0), w, d, 1.5f, 0.6f, true);
            if (ruin < 0.15f) b.Box(Sl.Stone, new Vector3(w * 0.3f, y1 + 1.2f, -d * 0.2f), new Vector3(0.6f, 2.0f, 0.6f));
        }
        else
        {
            for (int k = 0; k < 4; k++)
                b.Beam(Weathered, new Vector3(-w / 2f + k * w / 3f, y1, -d / 2f), new Vector3(-w / 2f + k * w / 3f + 0.3f, y1 + 1.2f + R01(seed, k, 872), 0f), 0.14f, 0.14f);
        }
        // lentera gantung dan jemuran ikan
        if (ruin < 0.15f)
        {
            b.Box(Sl.Wood, new Vector3(w / 2f - 0.15f, pf + 1.6f, d / 2f + 1.55f), new Vector3(0.1f, 3.2f, 0.1f));
            b.Box(Sl.Glow, new Vector3(w / 2f - 0.15f, pf + 2.9f, d / 2f + 1.75f), new Vector3(0.22f, 0.3f, 0.22f));
            b.Beam(Sl.Wood, new Vector3(-w / 2f + 0.2f, pf + 1.9f, d / 2f + 1.5f), new Vector3(-w / 2f - 1.6f, pf + 1.9f, d / 2f + 1.5f), 0.08f, 0.08f);
            for (int k = 0; k < 4; k++)
                Blob(b, Sl.Metal, new Vector3(-w / 2f - 0.3f - k * 0.35f, pf + 1.55f, d / 2f + 1.5f), new Vector3(0.07f, 0.2f, 0.04f), seed + k, 0.1f, 3, 5, 0f);
        }
        else if (ruin > 0.4f)
            b.Box(wood, new Vector3(w * 0.2f, pf + 0.12f, d / 2f + 0.3f), new Vector3(1.3f, 0.07f, 0.4f), 20f);
        StiltHouseArt(b, seed, w, d, ruin, pf, y0, y1);
    }

    public static void StiltInn(GeoBag b, int seed)
    {
        StiltHouse(b, seed, 10f, 7f, 0f);
        float pf = 1.5f;
        b.Box(Sl.Wood, new Vector3(3.2f, pf + 2.6f, 5.2f), new Vector3(0.12f, 2.6f, 0.12f));
        b.Box(Sl.Wood, new Vector3(3.2f + 0.7f, pf + 3.3f, 5.2f), new Vector3(1.5f, 0.55f, 0.08f));
        b.Box(Sl.Gold, new Vector3(3.2f + 0.7f, pf + 3.3f, 5.26f), new Vector3(0.4f, 0.3f, 0.03f));
        b.Box(Sl.Wood, new Vector3(-3.4f, pf + 0.55f, 5.0f), new Vector3(0.9f, 0.7f, 0.9f), 15f);
        Jar(b, new Vector3(-3.2f, pf + 0.9f, 5.5f), 1.2f, Sl.Wood);
    }

    public static void NetRack(GeoBag b, int seed)
    {
        for (int sx = -1; sx <= 1; sx += 2) b.Beam(Sl.Wood, new Vector3(sx * 1.6f, -0.2f, 0), new Vector3(sx * 1.6f, 2.8f, 0), 0.12f, 0.12f);
        b.Beam(Sl.Wood, new Vector3(-1.7f, 2.7f, 0), new Vector3(1.7f, 2.7f, 0), 0.1f, 0.1f);
        b.QuadBoth(Sl.Cloth, new Vector3(-1.5f, 2.65f, 0f), new Vector3(1.5f, 2.65f, 0f), new Vector3(1.3f, 0.95f, 0.1f), new Vector3(-1.3f, 0.95f, 0.1f));
        for (int k = 0; k < 3; k++) Blob(b, Sl.Metal, new Vector3(-0.8f + k * 0.8f, 0.85f, 0.1f), new Vector3(0.07f, 0.2f, 0.04f), seed + k, 0.1f, 3, 5, 0f);
    }

    /// <summary>Dermaga: berjalan sepanjang +Z dari z = 0. Origin = permukaan air; geladak di y = 0.55.</summary>
    public static void Dock(GeoBag b, int seed, float len, float ruin)
    {
        int n = Mathf.Max(2, Mathf.RoundToInt(len / 1.1f));
        for (int i = 0; i < n; i++)
        {
            if (ruin > 0.2f && R01(seed, i, 881) < ruin * 0.5f) continue;
            b.Box(ruin > 0.2f ? Weathered : Sl.Wood, new Vector3(0, 0.55f, 0.55f + i * 1.1f), new Vector3(2.2f, 0.1f, 1.02f), (R01(seed, i, 882) - 0.5f) * 2f * ruin * 4f);
        }
        for (int i = 0; i <= (int)(len / 3f); i++)
            foreach (float sx in new[] { -1.05f, 1.05f })
            {
                if (ruin > 0.3f && R01(seed, i, 883 + (int)sx) < ruin * 0.4f) continue;
                b.Box(Sl.Wood, new Vector3(sx, -0.3f, 0.3f + i * 3f), new Vector3(0.22f, 2.4f, 0.22f));
            }
        b.Beam(Sl.Wood, new Vector3(-1.05f, 0.45f, 0), new Vector3(-1.05f, 0.45f, len), 0.14f, 0.14f);
        b.Beam(Sl.Wood, new Vector3(1.05f, 0.45f, 0), new Vector3(1.05f, 0.45f, len), 0.14f, 0.14f);
        if (ruin < 0.2f)
        {
            b.Box(Sl.Wood, new Vector3(1.05f, 1.6f, len - 0.2f), new Vector3(0.12f, 2.2f, 0.12f));
            b.Box(Sl.Glow, new Vector3(1.05f, 2.8f, len - 0.2f), new Vector3(0.22f, 0.3f, 0.22f));
        }
        else Skull(b, new Vector3(1.05f, 2.2f, len - 0.2f), 1.2f);
        DockArt(b, seed, len, ruin);
    }

    /// <summary>Perahu dayung. Panjang 3,8 m sepanjang Z. sink &gt; 0 = setengah tenggelam.</summary>
    public static void Boat(GeoBag b, int seed, float sink)
    {
        const int ns = 9, m = 8;
        float L = 1.9f;
        var rings = new List<Vector3[]>();
        for (int i = 0; i <= ns; i++)
        {
            float z = -L + 2f * L * i / ns, t = z / L;
            float wd = 0.7f * Mathf.Pow(Mathf.Max(0.02f, 1f - t * t), 0.55f);
            float rise = 0.35f * t * t;
            var ring = new Vector3[m + 1];
            for (int j = 0; j <= m; j++)
            {
                float a = Mathf.PI + j * Mathf.PI / m;
                ring[j] = new Vector3(Mathf.Cos(a) * wd, 0.45f + Mathf.Sin(a) * 0.42f + rise - sink, z);
            }
            rings.Add(ring);
        }
        for (int i = 0; i < ns; i++)
            for (int j = 0; j < m; j++)
                b.QuadBoth(Sl.Wood, rings[i][j], rings[i + 1][j], rings[i + 1][j + 1], rings[i][j + 1]);
        for (int i = 0; i < ns; i++)
        {
            b.Beam(Weathered, rings[i][0], rings[i + 1][0], 0.07f, 0.06f, Vector3.up);
            b.Beam(Weathered, rings[i][m], rings[i + 1][m], 0.07f, 0.06f, Vector3.up);
        }
        b.Box(Sl.Wood, new Vector3(0, 0.62f - sink, -0.5f), new Vector3(1.0f, 0.07f, 0.28f));
        b.Box(Sl.Wood, new Vector3(0, 0.62f - sink, 0.6f), new Vector3(0.9f, 0.07f, 0.28f));
        if (sink <= 0.01f)
        {
            b.Beam(Sl.Wood, new Vector3(-0.5f, 0.9f, 0.1f), new Vector3(-1.5f, 0.55f, 0.4f), 0.07f, 0.07f);
            b.Box(Sl.Glow, new Vector3(0, 1.0f, 1.55f), new Vector3(0.16f, 0.2f, 0.16f));
            b.Beam(Sl.Wood, new Vector3(0, 0.5f, 1.55f), new Vector3(0, 0.95f, 1.55f), 0.05f, 0.05f);
        }
    }

    public static void LanternPost(GeoBag b, bool ghost)
    {
        b.Box(Sl.Wood, new Vector3(0, 1.3f, 0), new Vector3(0.14f, 2.7f, 0.14f));
        b.Beam(Sl.Wood, new Vector3(0, 2.5f, 0), new Vector3(0.5f, 2.5f, 0), 0.08f, 0.08f);
        b.Box(ghost ? Crystal : Sl.Glow, new Vector3(0.5f, 2.3f, 0), new Vector3(0.22f, 0.3f, 0.22f));
        b.Box(Sl.Metal, new Vector3(0.5f, 2.5f, 0), new Vector3(0.28f, 0.05f, 0.28f));
    }

    public static void Gallows(GeoBag b, int seed)
    {
        b.Box(Weathered, new Vector3(0, 1.8f, 0), new Vector3(0.28f, 3.8f, 0.28f));
        b.Beam(Weathered, new Vector3(0, 3.6f, 0), new Vector3(2.4f, 3.6f, 0), 0.22f, 0.22f);
        b.Beam(Weathered, new Vector3(0.1f, 2.7f, 0), new Vector3(0.9f, 3.5f, 0), 0.12f, 0.12f);
        b.Beam(Sl.Wood, new Vector3(2.0f, 3.5f, 0), new Vector3(2.0f, 2.3f, 0), 0.04f, 0.04f);
        b.Cyl(Sl.Wood, new Vector3(2.0f, 2.1f, 0), 0.15f, 0.04f, 8, 0.15f, true);
        b.Box(Weathered, new Vector3(1.2f, 0.25f, 0), new Vector3(3.2f, 0.5f, 1.8f));
    }

    // ------------------------------------------------------------------ penyihir
    public static void WitchHut(GeoBag b, int seed)
    {
        float pf = 1.2f;
        for (int k = 0; k < 5; k++)
        {
            float a = k * 72f * Deg;
            Limb(b, Sl.Wood, new Vector3(Mathf.Sin(a) * 2.7f, -1.6f, Mathf.Cos(a) * 2.7f), new Vector3(Mathf.Sin(a) * 1.9f, pf, Mathf.Cos(a) * 1.9f), 0.2f, 0.14f, seed + k, 5, 0.1f);
        }
        b.Cyl(Weathered, new Vector3(0, pf - 0.1f, 0), 2.7f, 0.25f, 10, 2.7f, true);
        b.Cyl(Sl.Wood, new Vector3(0, pf + 0.1f, 0), 2.1f, 2.7f, 10, 1.9f, true);
        for (int k = 0; k < 5; k++)
        {
            float a = (k * 72f + 18f) * Deg;
            b.Beam(Sl.Wood, new Vector3(Mathf.Sin(a) * 2.1f, pf + 0.1f, Mathf.Cos(a) * 2.1f), new Vector3(Mathf.Sin(a) * 1.9f, pf + 2.8f, Mathf.Cos(a) * 1.9f), 0.2f, 0.12f);
        }
        b.Cone(OldThatch, new Vector3(0.0f, pf + 2.7f, 0f), 2.9f, 1.9f, 10);
        b.Cone(OldThatch, new Vector3(0.5f, pf + 4.3f, 0.2f), 1.5f, 2.3f, 9);
        b.Cone(Sl.ClothB, new Vector3(1.0f, pf + 6.2f, 0.5f), 0.4f, 1.0f, 7);
        b.Box(Sl.Wood, new Vector3(0, pf + 1.15f, 2.05f), new Vector3(0.95f, 1.9f, 0.1f));
        b.Box(Crystal, new Vector3(-1.2f, pf + 1.6f, 1.7f), new Vector3(0.5f, 0.6f, 0.05f), 35f);
        b.Box(Crystal, new Vector3(1.4f, pf + 1.6f, 1.5f), new Vector3(0.5f, 0.6f, 0.05f), -50f);
        for (int i = 0; i < 4; i++) b.Box(Weathered, new Vector3(0f, pf - 0.3f * (i + 1) + 0.05f, 2.9f + 0.4f * i), new Vector3(1.1f, 0.1f, 0.4f));
        // cerobong bengkok
        var pts = new List<Vector3> { new Vector3(-1.5f, pf + 2.2f, -0.6f), new Vector3(-1.6f, pf + 3.4f, -0.7f), new Vector3(-1.2f, pf + 4.6f, -0.9f), new Vector3(-1.4f, pf + 5.4f, -1.0f) };
        var rad = new List<float> { 0.3f, 0.26f, 0.24f, 0.28f };
        Tube(b, Sl.Stone, pts, rad, 6, seed, 0f, false);
        // tengkorak di tiang, herba gantung, botol
        b.Box(Sl.Wood, new Vector3(2.6f, pf + 1.2f, 1.6f), new Vector3(0.1f, 2.4f, 0.1f));
        Skull(b, new Vector3(2.6f, pf + 2.5f, 1.6f), 1.5f);
        for (int k = 0; k < 6; k++) Hang(b, new Vector3(-0.7f + k * 0.3f, pf + 2.0f, 2.2f), 0.5f + R01(seed, k, 891) * 0.4f, seed + k, LeafGreen, 0.05f);
        Jars(b, seed);
        b.Box(Sl.Wood, new Vector3(-2.3f, pf + 0.4f, 1.5f), new Vector3(0.8f, 0.6f, 0.8f), 20f);
        WitchHutArt(b, seed);
    }

    public static void Cauldron(GeoBag b, int seed)
    {
        for (int k = 0; k < 3; k++)
        {
            float a = k * 120f * Deg;
            b.Beam(Sl.Metal, new Vector3(Mathf.Sin(a) * 0.45f, 0.5f, Mathf.Cos(a) * 0.45f), new Vector3(Mathf.Sin(a) * 0.7f, 0f, Mathf.Cos(a) * 0.7f), 0.1f, 0.1f);
        }
        b.Ball(Sl.Metal, new Vector3(0, 0.95f, 0), 0.8f, 6, 10, 0.8f);
        b.Annulus(Sl.Metal, new Vector3(0, 1.42f, 0), 0.5f, 0.68f, 0f, 360f, 12, 0f);
        b.Disc(Crystal, new Vector3(0, 1.4f, 0), 0.52f, 10, 0f);
        for (int k = 0; k < 4; k++)
            b.Beam(Sl.Wood, new Vector3(Mathf.Sin(k * 1.6f) * 0.7f, 0.1f, Mathf.Cos(k * 1.6f) * 0.7f), new Vector3(-Mathf.Sin(k * 1.6f) * 0.1f, 0.3f, -Mathf.Cos(k * 1.6f) * 0.1f), 0.12f, 0.12f);
        b.Cone(Fire, new Vector3(0, 0.12f, 0), 0.3f, 0.7f, 6);
        b.Beam(Sl.Wood, new Vector3(0.1f, 1.45f, 0.1f), new Vector3(0.7f, 2.0f, 0.5f), 0.06f, 0.06f);
    }

    public static void RitualCircle(GeoBag b, int seed, float ruin)
    {
        int n = 9;
        for (int i = 0; i < n; i++)
        {
            if (ruin > 0.3f && R01(seed, i, 901) < ruin * 0.4f) continue;
            float a = i * 360f / n;
            b.Push(new Vector3(Mathf.Sin(a * Deg) * 7f, 0f, Mathf.Cos(a * Deg) * 7f), a);
            StandingStone(b, seed + i, 2.6f + R01(seed, i, 902) * 1.4f);
            b.Pop();
        }
        b.Disc(Crystal, new Vector3(0, 0.05f, 0), 4.6f, 20, 0f);
        b.Annulus(Sl.DarkStone, new Vector3(0, 0.07f, 0), 3.9f, 4.5f, 0f, 360f, 20, 0f);
        for (int i = 0; i < 5; i++)
        {
            float a0 = i * 144f * Deg, a1 = (i + 1) * 144f * Deg;
            b.Beam(Sl.DarkStone, new Vector3(Mathf.Sin(a0) * 3.9f, 0.1f, Mathf.Cos(a0) * 3.9f), new Vector3(Mathf.Sin(a1) * 3.9f, 0.1f, Mathf.Cos(a1) * 3.9f), 0.2f, 0.05f, Vector3.up);
        }
        b.Box(Sl.DarkStone, new Vector3(0, 0.45f, 0), new Vector3(1.6f, 0.9f, 0.9f));
        Skull(b, new Vector3(0.3f, 1.0f, 0f), 1.3f);
    }

    public static void BoneTotem(GeoBag b, int seed)
    {
        b.Box(Sl.Wood, new Vector3(0, 1.8f, 0), new Vector3(0.28f, 3.8f, 0.28f));
        for (int k = 0; k < 4; k++) Skull(b, new Vector3(0, 1.0f + k * 0.8f, 0.2f), 1.6f - k * 0.15f);
        b.Beam(Sl.Wood, new Vector3(-0.9f, 2.9f, 0), new Vector3(0.9f, 2.9f, 0), 0.1f, 0.1f);
        for (int k = 0; k < 3; k++) Hang(b, new Vector3(-0.8f + k * 0.8f, 2.85f, 0), 0.6f, seed + k, Sl.ClothB, 0.06f);
        b.Cone(Sl.ClothB, new Vector3(0, 3.7f, 0), 0.18f, 0.9f, 5);
    }

    // ------------------------------------------------------------------ desa hantu, makam, menara
    public static void Chapel(GeoBag b, int seed, float ruin)
    {
        float w = 7.5f, d = 12f, wh = 5.0f, th = 0.5f, y0 = 0.4f, y1 = y0 + wh;
        b.BoxB(Sl.Stone, new Vector3(0, -0.3f, 0), new Vector3(w + 0.6f, y0 + 0.3f, d + 0.6f));
        var front = new List<Op> { new Op(0f, 1.8f, y0, y0 + 3.2f) };
        float xa = -w / 2f - th / 2f, xb = w / 2f + th / 2f;
        Wall(b, Grime, xa, xb, d / 2f, y0, y1, th, front, ruin * 0.8f, seed + 1);
        Wall(b, Grime, xa, xb, -d / 2f, y0, y1, th, new List<Op> { new Op(0f, 1.1f, y0 + 1.8f, y0 + 4.2f) }, ruin, seed + 2);
        for (int sdx = -1; sdx <= 1; sdx += 2)
        {
            b.Push(new Vector3(sdx * w / 2f, 0f, 0f), 90f);
            var side = new List<Op>();
            foreach (float oz in new[] { -3.6f, 0f, 3.6f }) side.Add(new Op(oz, 1.0f, y0 + 1.5f, y0 + 4.0f));
            Wall(b, Grime, -d / 2f + th / 2f, d / 2f - th / 2f, 0f, y0, y1, th, side, ruin, seed + 3 + sdx);
            b.Pop();
        }
        b.Box(Sl.Cobble, new Vector3(0, y0 - 0.04f, 0), new Vector3(w - 0.2f, 0.1f, d - 0.2f));
        // jendela kaca hantu
        foreach (float sdx in new[] { -1f, 1f })
            foreach (float oz in new[] { -3.6f, 0f, 3.6f })
                if (R01(seed, (int)(oz * 3f + sdx * 11f), 911) > ruin * 0.5f)
                    b.Box(Crystal, new Vector3(sdx * (w / 2f - 0.05f), y0 + 2.75f, oz), new Vector3(0.05f, 2.2f, 1.2f));
        b.Box(Crystal, new Vector3(0, y0 + 3.0f, -d / 2f + 0.05f), new Vector3(1.5f, 2.2f, 0.05f));
        if (ruin < 0.8f) b.Gable(OldRoof, Grime, Sl.Wood, new Vector3(0, y1, 0), w, d, 3.0f, 0.6f, false);
        // menara lonceng di depan
        float tw = 3.4f, ty = y1 + 3.0f;
        b.BoxB(Grime, new Vector3(0, y0, d / 2f + tw / 2f - 0.4f), new Vector3(tw, ty - y0 + 1.0f, tw));
        b.Box(Sl.Stone, new Vector3(0, ty + 0.9f, d / 2f + tw / 2f - 0.4f), new Vector3(tw + 0.4f, 0.3f, tw + 0.4f));
        b.Box(Crystal, new Vector3(0, ty - 1.2f, d / 2f + tw - 0.38f), new Vector3(0.8f, 1.6f, 0.05f));
        if (ruin < 0.6f) b.Spire(OldRoof, new Vector3(0, ty + 1.05f, d / 2f + tw / 2f - 0.4f), tw * 0.62f, 4.2f, 4, 0f);
        else for (int k = 0; k < 4; k++) b.Beam(Weathered, new Vector3((k % 2 - 0.5f) * 2f, ty + 1.0f, d / 2f + tw / 2f - 0.4f + (k / 2 - 0.5f) * 2f), new Vector3((k % 2 - 0.5f) * 0.6f, ty + 3.2f, d / 2f + tw / 2f - 0.4f), 0.2f, 0.2f);
        // altar dalam
        b.Box(Sl.DarkStone, new Vector3(0, 0.9f, -d / 2f + 1.2f), new Vector3(2.4f, 1.0f, 1.0f));
        b.Box(Sl.Stone, new Vector3(0, y0 + 0.2f, d / 2f + 0.9f), new Vector3(3.4f, 0.4f, 1.4f));
        if (ruin > 0.2f) Rubble(b, new Vector3(2f, 0.4f, -1f), 2.4f, 6, seed);
        ChapelArt(b, seed, ruin, w, d, y0, y1);
    }

    public static void Crypt(GeoBag b, int seed, float ruin, float scale = 1f)
    {
        float w = 4.6f * scale, d = 5.6f * scale, h = 3.4f * scale;
        b.BoxB(Sl.Stone, new Vector3(0, -0.2f, 0), new Vector3(w + 0.8f, 0.7f, d + 0.8f));
        b.BoxB(Sl.DarkStone, new Vector3(0, 0.5f, 0), new Vector3(w, h, d));
        b.Box(Sl.Stone, new Vector3(0, 0.5f + h + 0.15f, 0), new Vector3(w + 0.5f, 0.3f, d + 0.5f));
        if (ruin < 0.6f)
        {
            b.Gable(Sl.Roof, Sl.DarkStone, Sl.Stone, new Vector3(0, 0.5f + h + 0.3f, 0), w, d, 1.6f * scale, 0.35f, false);
            b.Cone(Sl.DarkStone, new Vector3(0, 0.5f + h + 1.8f * scale, 0), 0.25f, 1.1f, 4);
        }
        for (int sx = -1; sx <= 1; sx += 2)
        {
            b.Cyl(Sl.Stone, new Vector3(sx * (w / 2f - 0.15f), 0.5f, d / 2f + 0.35f), 0.28f, h, 8, 0.24f, true);
        }
        b.Box(Sl.Dark, new Vector3(0, 0.5f + 1.1f * scale, d / 2f + 0.02f), new Vector3(1.5f * scale, 2.2f * scale, 0.1f));
        b.Box(Crystal, new Vector3(0, 0.5f + 2.6f * scale, d / 2f + 0.03f), new Vector3(0.9f * scale, 0.18f, 0.06f));
        Skull(b, new Vector3(0, 0.5f + h - 0.2f, d / 2f + 0.3f), 1.5f);
        for (int k = 0; k < 3; k++) b.Box(Sl.Stone, new Vector3(0, 0.1f + k * 0.18f, d / 2f + 0.9f + k * 0.4f), new Vector3(2.4f * scale, 0.2f, 0.5f));
        if (ruin > 0.2f) Rubble(b, new Vector3(w / 2f, 0.3f, d / 2f), 1.6f, 5, seed);
        CryptArt(b, seed, ruin, scale);
    }

    public static void NecroSpire(GeoBag b, int seed, float ruin)
    {
        float[] rs = { 7.5f, 6f, 4.8f, 3.5f };
        float[] hs = { 6f, 6f, 5.5f, 5f };
        float y = 0f;
        b.Cyl(Sl.DarkStone, new Vector3(0, -0.5f, 0), 9.2f, 1.6f, 10, 8.4f, true);
        y = 1.1f;
        for (int t = 0; t < rs.Length; t++)
        {
            float r1 = t + 1 < rs.Length ? rs[t + 1] + 0.4f : 2.8f;
            b.Cyl(Sl.DarkStone, new Vector3(0, y, 0), rs[t], hs[t], 8, r1, true);
            for (int k = 0; k < 8; k++)
            {
                float a = (k * 45f + t * 22f) * Deg;
                float rr = Mathf.Lerp(rs[t], r1, 0.5f) + 0.05f;
                b.Box(Crystal, new Vector3(Mathf.Sin(a) * rr, y + hs[t] * 0.5f, Mathf.Cos(a) * rr), new Vector3(0.5f, 1.8f, 0.5f), a / Deg);
            }
            b.Annulus(Sl.Stone, new Vector3(0, y + hs[t], 0), r1, rs[t] + 0.7f, 0f, 360f, 14, 0f);
            for (int k = 0; k < 6; k++)
            {
                float a = (k * 60f + t * 17f) * Deg;
                b.Cone(Sl.DarkStone, new Vector3(Mathf.Sin(a) * (rs[t] + 0.3f), y + hs[t], Mathf.Cos(a) * (rs[t] + 0.3f)), 0.5f, 1.8f + R01(seed, k + t * 7, 921) * 1.5f, 4);
            }
            y += hs[t];
        }
        // gerbang dan tangga
        b.Box(Sl.Dark, new Vector3(0, 2.5f, rs[0] - 0.5f), new Vector3(2.6f, 3.8f, 1.2f));
        b.Box(Crystal, new Vector3(0, 4.8f, rs[0] + 0.1f), new Vector3(1.6f, 0.2f, 0.1f));
        for (int k = 0; k < 5; k++) b.Box(Sl.Stone, new Vector3(0, 0.1f + k * 0.16f, rs[0] + 1.6f + k * 0.7f), new Vector3(3.8f, 0.22f, 0.7f));
        // kristal melayang di puncak
        b.Spire(Crystal, new Vector3(0, y + 1.2f, 0), 1.4f, 5.5f, 6, 0.3f);
        b.Ball(Crystal, new Vector3(0, y + 5.2f, 0), 0.8f, 4, 7, 1.2f);
        for (int k = 0; k < 6; k++)
        {
            float a = k * 60f * Deg;
            b.Box(Crystal, new Vector3(Mathf.Sin(a) * 3.2f, y + 3.4f + Mathf.Sin(k * 2f) * 0.8f, Mathf.Cos(a) * 3.2f), new Vector3(0.35f, 1.1f, 0.35f), k * 40f);
        }
        if (ruin > 0.1f) Rubble(b, new Vector3(5f, 0.2f, 6f), 3f, 6, seed);
    }

    public static void BogShack(GeoBag b, int seed)
    {
        StiltHouse(b, seed, 3.6f, 3.2f, 0.9f);
    }

    public static void HangingCage(GeoBag b, int seed)
    {
        b.Box(Weathered, new Vector3(0, 2.0f, 0), new Vector3(0.2f, 4.0f, 0.2f));
        b.Beam(Weathered, new Vector3(0, 3.8f, 0), new Vector3(1.6f, 3.8f, 0), 0.16f, 0.16f);
        b.Beam(Sl.Metal, new Vector3(1.4f, 3.8f, 0), new Vector3(1.4f, 3.1f, 0), 0.03f, 0.03f);
        b.Cyl(Sl.Metal, new Vector3(1.4f, 2.1f, 0), 0.45f, 1.0f, 8, 0.38f, true);
        for (int k = 0; k < 8; k++)
        {
            float a = k * 45f * Deg;
            b.Beam(Sl.Metal, new Vector3(1.4f + Mathf.Sin(a) * 0.46f, 2.1f, Mathf.Cos(a) * 0.46f), new Vector3(1.4f + Mathf.Sin(a) * 0.4f, 3.1f, Mathf.Cos(a) * 0.4f), 0.03f, 0.03f);
        }
        Skull(b, new Vector3(1.4f, 2.4f, 0), 1.3f);
    }
}
