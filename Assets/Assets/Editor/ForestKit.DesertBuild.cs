using System;
using System.Collections.Generic;
using UnityEngine;

public static partial class ForestKit
{
    const float Deg = Mathf.Deg2Rad;

    // ------------------------------------------------------------------ pembantu
    static void Lathe(GeoBag b, int slot, float[] rs, float[] ys, int segs, Vector3 at, float scale = 1f, bool capTop = true)
    {
        var rings = new List<Vector3[]>();
        for (int i = 0; i < rs.Length; i++)
        {
            var ring = new Vector3[segs];
            for (int j = 0; j < segs; j++)
            {
                float a = j * Mathf.PI * 2f / segs;
                ring[j] = at + new Vector3(Mathf.Sin(a) * rs[i] * scale, ys[i] * scale, Mathf.Cos(a) * rs[i] * scale);
            }
            rings.Add(ring);
        }
        b.Loft(slot, rings, true, capTop, true);
    }

    /// <summary>Balok meruncing persegi panjang (menara pylon, obelisk). hw0/hd0 = setengah lebar dasar, hw1/hd1 = puncak. jag &gt; 0 = puncak bergerigi.</summary>
    static void TaperBox(GeoBag b, int slot, Vector3 baseC, float hw0, float hd0, float hw1, float hd1, float h, int seed, float jag = 0f, int steps = 6)
    {
        var rings = new List<Vector3[]>();
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            float hw = Mathf.Lerp(hw0, hw1, t), hd = Mathf.Lerp(hd0, hd1, t);
            var ring = new Vector3[4];
            float[] sx = { -1, -1, 1, 1 }, sz = { 1, -1, -1, 1 };
            for (int j = 0; j < 4; j++)
            {
                float cut = (i == steps) ? jag * (0.2f + R01(j, 3, seed) * 1.4f) : 0f;
                ring[j] = baseC + new Vector3(sx[j] * hw, t * h - cut, sz[j] * hd);
            }
            rings.Add(ring);
        }
        b.Loft(slot, rings, false, jag <= 0.01f, false);
    }

    static void SandDrift(GeoBag b, Vector3 c, Vector3 r, int seed)
    {
        Blob(b, Moss, c, r, seed, 0.18f, 4, 10, 0.12f);
    }

    // ------------------------------------------------------------------ tumbuhan
    public static void Palm(GeoBag b, int seed, float scale = 1f, float lean = 1f)
    {
        float h = (6.8f + R01(seed, 1, 501) * 2.6f) * scale;
        float ang = R01(seed, 2, 501) * 360f * Deg;
        float amt = (0.7f + R01(seed, 3, 501) * 1.7f) * lean * scale;
        float lx = Mathf.Sin(ang) * amt, lz = Mathf.Cos(ang) * amt;
        var pts = new List<Vector3>(); var rad = new List<float>();
        for (int i = 0; i <= 8; i++)
        {
            float t = i / 8f;
            pts.Add(new Vector3(lx * t * t, -0.3f + (h + 0.3f) * t, lz * t * t));
            rad.Add((Mathf.Lerp(0.36f, 0.2f, t) * (i == 0 ? 1.35f : 1f)) * scale);
        }
        Tube(b, Bark, pts, rad, 8, seed, 0.2f, true);
        Vector3 top = pts[pts.Count - 1];
        int nf = 12;
        for (int k = 0; k < nf; k++)
        {
            float yaw = k * 360f / nf + (R01(seed, k, 502) - 0.5f) * 24f;
            float L = (3.4f + R01(seed, k, 503) * 1.1f) * scale;
            float rise = L * (k % 3 == 0 ? 0.55f : 0.32f);
            float droop = L * (0.75f + R01(seed, k, 504) * 0.3f);
            Vector3 dirH = new Vector3(Mathf.Sin(yaw * Deg), 0f, Mathf.Cos(yaw * Deg));
            Vector3 sideV = Vector3.Cross(dirH, Vector3.up).normalized;
            int segs = 6;
            Vector3 prev = top + Vector3.up * 0.1f;
            for (int s = 1; s <= segs; s++)
            {
                float t = s / (float)segs, tp = (s - 1) / (float)segs;
                Vector3 cur = top + dirH * (L * t) + Vector3.up * (0.1f + Mathf.Sin(t * Mathf.PI * 0.75f) * rise - droop * t * t * t);
                float w0 = 0.5f * scale * Mathf.Sin(Mathf.PI * Mathf.Min(1f, tp * 0.9f + 0.1f)) * (1f - 0.35f * tp);
                float w1 = 0.5f * scale * Mathf.Sin(Mathf.PI * Mathf.Min(1f, t * 0.9f + 0.1f)) * (1f - 0.35f * t);
                if (s == segs) w1 = 0.01f;
                Vector3 dn0 = Vector3.down * w0 * 0.3f, dn1 = Vector3.down * w1 * 0.3f;
                b.QuadBoth(LeafGreen, prev - sideV * w0 + dn0 * 0f, prev + dn0, cur + dn1, cur - sideV * w1);
                b.QuadBoth(LeafGreen, prev + dn0, prev + sideV * w0, cur + sideV * w1, cur + dn1);
                prev = cur;
            }
        }
        for (int k = 0; k < 3; k++)
            Blob(b, LeafOrange, top + new Vector3(Mathf.Sin(k * 2.1f) * 0.32f, -0.35f - R01(seed, k, 505) * 0.2f, Mathf.Cos(k * 2.1f) * 0.32f) * scale, new Vector3(0.2f, 0.26f, 0.2f) * scale, seed + k, 0.1f, 3, 6, 0f);
        PalmArt(b, seed, scale, pts, rad, top);
    }

    public static void Cactus(GeoBag b, int seed, float scale = 1f)
    {
        float h = (3.4f + R01(seed, 1, 511) * 2.6f) * scale, r = 0.34f * scale;
        float wob = (R01(seed, 2, 511) - 0.5f) * 0.3f * scale;
        var pts = new List<Vector3>(); var rad = new List<float>();
        for (int i = 0; i <= 5; i++)
        {
            float t = i / 5f;
            pts.Add(new Vector3(wob * Mathf.Sin(t * 3f), -0.2f + (h + 0.2f) * t, 0f));
            rad.Add(r * (i == 0 ? 1.15f : 1f) * (i == 5 ? 0.35f : (i == 4 ? 0.8f : 1f)));
        }
        Tube(b, LeafPine, pts, rad, 10, seed, 0f, true);
        int arms = 1 + seed % 3;
        for (int k = 0; k < arms; k++)
        {
            float yaw = R01(seed, k, 512) * 360f + k * 150f;
            float y0 = h * (0.32f + 0.26f * R01(seed, k + 4, 513));
            Vector3 p0 = new Vector3(wob * Mathf.Sin(y0 / h * 3f), y0, 0f);
            Vector3 dH = new Vector3(Mathf.Sin(yaw * Deg), 0, Mathf.Cos(yaw * Deg));
            float out1 = (0.55f + R01(seed, k, 514) * 0.35f) * scale, up1 = (1.0f + R01(seed, k + 2, 514) * 1.3f) * scale;
            var ap = new List<Vector3> { p0 - dH * 0.1f, p0 + dH * out1 * 0.6f + Vector3.up * 0.05f, p0 + dH * out1 + Vector3.up * 0.4f * scale, p0 + dH * out1 * 1.05f + Vector3.up * (0.4f * scale + up1) };
            var ar = new List<float> { r * 0.62f, r * 0.6f, r * 0.58f, r * 0.22f };
            Tube(b, LeafPine, ap, ar, 8, seed + k, 0f, true);
        }
        Blob(b, LeafRed, new Vector3(wob * 0.9f, h + 0.02f, 0f), new Vector3(0.14f, 0.1f, 0.14f) * scale, seed, 0.2f, 3, 6, 0f);
        CactusArt(b, seed, scale, h, wob);
    }

    public static void Agave(GeoBag b, int seed, float scale = 1f)
    {
        int n = 16;
        for (int k = 0; k < n; k++)
        {
            float yaw = k * 360f / n + R01(seed, k, 521) * 12f;
            float tilt = 0.35f + (k % 3) * 0.55f;
            float len = (0.9f + R01(seed, k, 522) * 0.7f) * scale * (1f - (k % 3) * 0.12f);
            Vector3 d0 = Dir(yaw, tilt), d1 = Dir(yaw, tilt * 0.35f - 0.1f);
            Vector3 p0 = new Vector3(0, 0.08f, 0), p1 = p0 + d0 * len * 0.55f, p2 = p1 + d1 * len * 0.5f;
            Vector3 side = Vector3.Cross(d0, Vector3.up).normalized;
            float w = 0.13f * scale;
            b.QuadBoth(LeafPine, p0 - side * w * 0.7f, p0 + side * w * 0.7f, p1 + side * w, p1 - side * w);
            b.QuadBoth(LeafPine, p1 - side * w, p1 + side * w, p2, p2);
        }
        AgaveArt(b, seed, scale);
    }

    public static void DryShrub(GeoBag b, int seed, float scale = 1f)
    {
        int n = 3 + seed % 3;
        for (int k = 0; k < n; k++)
        {
            float yaw = k * (360f / n) + R01(seed, k, 531) * 40f;
            Vector3 c = Dir(yaw, 0f) * (0.25f + R01(seed, k, 532) * 0.35f) * scale + new Vector3(0, 0.25f * scale, 0);
            Limb(b, Weathered, new Vector3(0, 0, 0), c + Vector3.up * 0.2f * scale, 0.035f * scale, 0.02f * scale, seed + k, 4);
            Blob(b, LeafYellow, c + Vector3.up * 0.12f * scale, new Vector3(0.5f, 0.32f, 0.5f) * scale, seed + k * 3, 0.35f, 3, 7, 0.1f);
        }
        DryShrubArt(b, seed, scale);
    }

    public static void Tumbleweed(GeoBag b, int seed)
    {
        Blob(b, LeafYellow, new Vector3(0, 0.45f, 0), new Vector3(0.5f, 0.45f, 0.5f), seed, 0.5f, 4, 8, 0f);
        for (int k = 0; k < 5; k++) Limb(b, Weathered, new Vector3(0, 0.45f, 0), new Vector3(0, 0.45f, 0) + Dir(k * 72f, (k - 2) * 0.35f) * 0.62f, 0.02f, 0.01f, seed + k, 3);
    }

    public static void Reeds(GeoBag b, int seed)
    {
        for (int k = 0; k < 16; k++)
        {
            float x = (R01(seed, k, 541) - 0.5f) * 1.4f, z = (R01(seed, k, 542) - 0.5f) * 1.4f, h = 1.2f + R01(seed, k, 543) * 1.1f;
            Vector3 lean = new Vector3((R01(seed, k, 544) - 0.5f) * 0.4f, 0, (R01(seed, k, 545) - 0.5f) * 0.4f);
            b.Beam(LeafGreen, new Vector3(x, -0.1f, z), new Vector3(x, h, z) + lean, 0.05f, 0.015f, Vector3.right);
            if (k % 4 == 0) b.Cyl(Weathered, new Vector3(x, h - 0.3f, z) + lean, 0.035f, 0.32f, 5, 0.03f, true);
        }
    }

    // ------------------------------------------------------------------ batuan & sisa
    public static void Hoodoo(GeoBag b, int seed, float h = 7f)
    {
        int layers = 5 + seed % 3;
        float y = -0.5f;
        float[] prof = { 2.3f, 1.7f, 1.2f, 1.0f, 1.3f, 1.0f, 1.6f };
        for (int i = 0; i < layers; i++)
        {
            float lh = (h / layers) * (0.8f + R01(seed, i, 551) * 0.5f);
            float r = prof[i % prof.Length] * (0.85f + R01(seed, i, 552) * 0.3f);
            if (i == layers - 1) r = 1.9f;
            Blob(b, i == layers - 1 ? Sl.Stone : Rock, new Vector3((R01(seed, i, 553) - 0.5f) * 0.3f, y + lh * 0.5f, (R01(seed, i, 554) - 0.5f) * 0.3f), new Vector3(r, lh * 0.62f, r * 0.9f), seed + i * 5, 0.2f, 4, 10, 0f);
            y += lh * 0.8f;
        }
        SandDrift(b, new Vector3(0, 0.1f, 0), new Vector3(3.2f, 0.7f, 3.0f), seed);
    }

    public static void BeastSkeleton(GeoBag b, int seed)
    {
        var sp = new List<Vector3>(); var sr = new List<float>();
        for (int i = 0; i <= 6; i++) { float t = i / 6f; sp.Add(new Vector3(0, 0.75f + Mathf.Sin(t * 3f) * 0.25f, -3.2f + t * 6.4f)); sr.Add(0.12f * (1f - 0.4f * t)); }
        Tube(b, Sl.Marble, sp, sr, 6, seed, 0f, true);
        for (int k = 1; k <= 5; k++)
        {
            Vector3 root = sp[k];
            foreach (float sx in new[] { -1f, 1f })
            {
                var rp = new List<Vector3>(); var rr = new List<float>();
                float rad = 1.2f - Mathf.Abs(k - 3) * 0.15f;
                for (int i = 0; i <= 5; i++)
                {
                    float a = i / 5f * Mathf.PI * 0.85f;
                    rp.Add(root + new Vector3(sx * Mathf.Sin(a) * rad, -Mathf.Cos(a) * rad * 0.8f + 0.3f, 0f));
                    rr.Add(0.06f * (1f - i / 8f));
                }
                if (R01(seed, k * 2 + (sx > 0 ? 1 : 0), 561) > 0.25f) Tube(b, Sl.Marble, rp, rr, 5, seed + k, 0f, true);
            }
        }
        Vector3 hc = sp[6] + new Vector3(0, -0.15f, 0.95f);
        Blob(b, Sl.Marble, hc, new Vector3(0.5f, 0.38f, 0.85f), seed, 0.1f, 4, 8, 0.2f);
        foreach (float sx in new[] { -1f, 1f })
        {
            b.Box(Sl.Dark, hc + new Vector3(sx * 0.2f, 0.12f, 0.5f), new Vector3(0.2f, 0.2f, 0.12f));
            Limb(b, Sl.Marble, hc + new Vector3(sx * 0.3f, 0.2f, 0.1f), hc + new Vector3(sx * 0.95f, 0.9f, 0.1f), 0.1f, 0.02f, seed + 7, 5);
        }
        SandDrift(b, new Vector3(0, 0f, 0), new Vector3(2.4f, 0.55f, 3.8f), seed + 2);
    }

    // ------------------------------------------------------------------ arsitektur batu pasir
    public static void Column(GeoBag b, Vector3 at, float h, float r, int seed, float brk = 0f)
    {
        float hh = brk > 0f ? h * Mathf.Lerp(1f, 0.18f + 0.5f * R01(seed, 1, 571), brk) : h;
        b.Cyl(Sl.Stone, at, r * 1.3f, 0.45f, 12, r * 1.25f, true);
        b.Cyl(Sl.Stone, at + new Vector3(0, 0.45f, 0), r, hh - 0.45f, 12, r * 0.88f, brk <= 0f);
        if (brk <= 0f)
        {
            float top = at.y + hh;
            Lathe(b, Sl.Stone, new[] { r * 0.88f, r * 1.0f, r * 1.35f, r * 1.55f, r * 1.4f }, new[] { 0f, 0.3f, 0.75f, 1.2f, 1.45f }, 12, new Vector3(at.x, top, at.z));
            b.Box(Sl.Stone, new Vector3(at.x, top + 1.6f, at.z), new Vector3(r * 3.3f, 0.35f, r * 3.3f));
            b.Cyl(Crystal, at + new Vector3(0, hh * 0.45f, 0), r * 1.02f, 0.06f, 12, r * 1.02f, false);       // cincin cahaya kristal
        }
        else
        {
            Rubble(b, at + new Vector3(r * 2.4f, 0f, r * 1.1f), 1.4f, 5, seed + 3);
            b.Box(Sl.Stone, at + new Vector3(r * 2.8f, 0.35f, -r), new Vector3(r * 2f, 0.7f, r * 2f), R01(seed, 4, 571) * 80f);
        }
        ColumnArt(b, at, hh, r, seed, brk);
    }

    public static void Brazier(GeoBag b, int seed)
    {
        b.Cyl(Sl.Stone, Vector3.zero, 0.38f, 1.0f, 8, 0.28f, true);
        Lathe(b, Sl.DarkStone, new[] { 0.2f, 0.5f, 0.6f, 0.55f }, new[] { 0f, 0.18f, 0.4f, 0.46f }, 10, new Vector3(0, 1.0f, 0), 1f, false);
        b.Disc(Sl.Glow, new Vector3(0, 1.4f, 0), 0.5f, 10, 0f);
        b.Cone(Fire, new Vector3(0, 1.42f, 0), 0.36f, 0.75f, 7);
    }

    public static void Obelisk(GeoBag b, int seed, float ruin = 0f, float h = 11f)
    {
        b.BoxB(Sl.Stone, new Vector3(0, -0.4f, 0), new Vector3(3.4f, 0.8f, 3.4f));
        b.BoxB(Sl.Stone, new Vector3(0, 0.4f, 0), new Vector3(2.7f, 0.6f, 2.7f));
        float hh = ruin > 0.5f ? h * (0.55f + 0.2f * R01(seed, 1, 581)) : h;
        TaperBox(b, Sl.Stone, new Vector3(0, 1.0f, 0), 1.05f, 1.05f, 0.62f * (hh / h) + 0.33f, 0.62f * (hh / h) + 0.33f, hh, seed, ruin > 0.5f ? 0.9f : 0f, 8);
        if (ruin <= 0.5f)
        {
            TaperBox(b, Sl.Gold, new Vector3(0, 1.0f + hh, 0), 0.62f * 0.33f + 0.33f - 0.02f, 0.62f * 0.33f + 0.33f - 0.02f, 0.02f, 0.02f, 1.3f, seed, 0f, 1);
            for (int f = 0; f < 4; f++)
            {
                b.Push(Vector3.zero, f * 90f);
                for (int g = 0; g < 3; g++)
                {
                    float y = 2.0f + g * hh * 0.28f;
                    float hw = Mathf.Lerp(1.05f, 0.95f, (y - 1f) / hh) * (1f - (y - 1f) / hh * 0.38f);
                    b.Box(Crystal, new Vector3(0, y, hw + 0.01f), new Vector3(0.16f, hh * 0.18f, 0.03f));
                    b.Box(Crystal, new Vector3(0.28f, y - 0.4f, hw + 0.01f), new Vector3(0.1f, 0.1f, 0.03f));
                    b.Box(Crystal, new Vector3(-0.28f, y + 0.2f, hw + 0.01f), new Vector3(0.1f, 0.1f, 0.03f));
                }
                b.Pop();
            }
        }
        else
        {
            b.Beam(Sl.Stone, new Vector3(3.6f, 0.75f, 1.5f), new Vector3(8.8f, 0.55f, 2.8f), 1.0f, 1.0f, Vector3.up);
            TaperBox(b, Sl.Stone, new Vector3(9.2f, 0.4f, 2.9f), 0.5f, 0.5f, 0.02f, 0.02f, 1.6f, seed, 0f, 1);
            Rubble(b, new Vector3(1.8f, 0f, 1.2f), 2.6f, 8, seed + 4);
        }
        SandDrift(b, new Vector3(0.6f, 0f, -0.4f), new Vector3(2.6f, 0.6f, 2.4f), seed);
    }

    public static void Archway(GeoBag b, int seed, float ruin = 0f)
    {
        b.BoxB(Sl.Stone, new Vector3(0, -0.4f, 0), new Vector3(10.5f, 0.6f, 3.2f));
        foreach (float sx in new[] { -1f, 1f })
        {
            float hh = (ruin > 0.5f && sx > 0) ? 4.2f : 7.4f;
            TaperBox(b, Sl.Stone, new Vector3(sx * 3.6f, 0.2f, 0), 0.95f, 0.95f, 0.8f, 0.8f, hh, seed + (int)sx, hh < 7f ? 0.7f : 0f, 5);
            if (hh > 7f) b.Box(Crystal, new Vector3(sx * 2.62f, 4.5f, 0), new Vector3(0.04f, 2.4f, 0.16f));
        }
        if (ruin < 0.5f)
        {
            b.Box(Sl.Stone, new Vector3(0, 7.9f, 0), new Vector3(9.4f, 1.3f, 1.9f));
            b.Box(Sl.Gold, new Vector3(0, 7.9f, 0.97f), new Vector3(2.6f, 0.4f, 0.04f));
            b.Box(Crystal, new Vector3(0, 7.9f, 1.0f), new Vector3(0.5f, 0.5f, 0.04f));
        }
        else
        {
            b.Box(Sl.Stone, new Vector3(-2.2f, 7.9f, 0), new Vector3(4.4f, 1.3f, 1.9f));
            b.Box(Sl.Stone, new Vector3(4.6f, 0.45f, 1.7f), new Vector3(3.4f, 1.0f, 1.6f), 24f);
            Rubble(b, new Vector3(3.2f, 0f, 0.8f), 2.8f, 8, seed + 5);
        }
        SandDrift(b, new Vector3(-1f, 0f, 0.5f), new Vector3(3.0f, 0.6f, 2.0f), seed);
    }

    /// <summary>Kuil batu pasir. Depan +Z. Pylon, pelataran, aula tiang, ruang suci. ruin 0..1.</summary>
    public static void Temple(GeoBag b, int seed, float ruin)
    {
        float W = 24f, D = 44f;
        // panggung bertingkat
        b.BoxB(Sl.Stone, new Vector3(0, -0.6f, 0), new Vector3(W + 6f, 1.1f, D + 4f));
        b.BoxB(Sl.Stone, new Vector3(0, 0.5f, 0), new Vector3(W + 2f, 0.7f, D));
        float y0 = 1.2f;
        b.Stairs(Sl.Stone, new Vector3(0, -0.6f, D / 2f + 5.2f), 9.5f, 6, 0.3f, 0.75f, 0f);
        // pylon (gerbang menara) di depan
        float zf = D / 2f - 3f;
        foreach (float sx in new[] { -1f, 1f })
        {
            float hh = 13f * (ruin > 0.4 ? 0.62f + 0.3f * R01(seed, (int)sx, 591) : 1f);
            TaperBox(b, Sl.Stone, new Vector3(sx * 8.3f, y0, zf), 3.7f, 2.2f, 3.1f, 1.8f, hh, seed + (int)sx * 7, ruin > 0.4f ? 1.2f : 0f, 7);
            PylonArt(b, seed, sx * 8.3f, zf, y0, hh, ruin);
            b.Box(Crystal, new Vector3(sx * 4.4f, y0 + 6f, zf + 0.0f), new Vector3(0.05f, 4.5f, 0.3f));
            for (int g = 0; g < 3; g++) b.Box(Crystal, new Vector3(sx * 8.3f, y0 + 3f + g * 2.8f, zf + 2.25f), new Vector3(0.18f, 1.6f, 0.04f));
        }
        if (ruin < 0.6f) b.Box(Sl.Stone, new Vector3(0, y0 + 11.2f, zf), new Vector3(9.6f, 1.8f, 3.4f));
        else b.Box(Sl.Stone, new Vector3(-3f, y0 + 9f, zf), new Vector3(3.6f, 1.6f, 3.0f), 8f);
        // pelataran depan: tiang & dinding rendah
        for (int i = 0; i < 3; i++)
            foreach (float sx in new[] { -1f, 1f })
                Column(b, new Vector3(sx * 7.5f, y0, zf - 6f - i * 4.6f), 5.6f, 0.62f, seed + i * 3 + (int)sx, R01(seed, i * 2 + (int)sx + 5, 592) < ruin * 0.8f ? ruin : 0f);
        float sideZ0 = zf - 3.5f, sideZ1 = -D / 2f + 1f;
        foreach (float sx in new[] { -1f, 1f })
        {
            b.Push(new Vector3(sx * (W / 2f - 0.5f), 0f, (sideZ0 + sideZ1) / 2f), 90f);
            var ops = new List<Op> { new Op(-5f, 1.1f, y0 + 4.2f, y0 + 6.2f), new Op(2f, 1.1f, y0 + 4.2f, y0 + 6.2f), new Op(9f, 1.1f, y0 + 4.2f, y0 + 6.2f) };
            Wall(b, Sl.Stone, -(sideZ0 - sideZ1) / 2f, (sideZ0 - sideZ1) / 2f, 0f, y0, y0 + 8.6f, 1.2f, ops, ruin, seed + 40 + (int)sx);
            b.Pop();
        }
        // aula tiang (hypostyle)
        float hallZ0 = -D / 2f + 14f;
        for (int row = 0; row < 3; row++)
            foreach (float cx in new[] { -7.5f, -3f, 3f, 7.5f })
            {
                float z = hallZ0 - 1f - row * 4.6f;
                float brk = R01(seed + row, (int)(cx * 2f), 593) < ruin * 0.85f ? Mathf.Clamp01(ruin + 0.1f) : 0f;
                Column(b, new Vector3(cx, y0, z), 8.2f, 0.74f, seed + row * 11 + (int)cx, brk);
                if (brk <= 0f && ruin < 0.8f) b.Box(Sl.Stone, new Vector3(cx, y0 + 8.2f + 1.6f + 0.5f, z), new Vector3(1.9f, 0.8f, 4.9f));       // arsitrave memanjang
            }
        for (int row = 0; row < 3; row++)
            for (int k = 0; k < 3; k++)
            {
                float z = hallZ0 - 1f - row * 4.6f;
                float x0s = (k - 1) * 5.0f;
                if (R01(seed + row, k, 594) < ruin * 0.8f) continue;
                b.Box(Sl.Stone, new Vector3(x0s * (k == 1 ? 0f : 1f) + (k == 0 ? -5.2f : (k == 2 ? 5.2f : 0f)), y0 + 10.7f, z - 0.0f), new Vector3(k == 1 ? 6.5f : 4.6f, 0.7f, 4.7f));
            }
        // dinding belakang & ruang suci
        float zb = -D / 2f + 2f;
        Wall(b, Sl.Stone, -W / 2f + 0.5f, W / 2f - 0.5f, zb, y0, y0 + 9.5f, 1.4f, new List<Op> { new Op(0f, 2.4f, y0, y0 + 4.6f) }, ruin, seed + 61);
        Wall(b, Sl.Stone, -W / 2f + 0.5f, W / 2f - 0.5f, hallZ0 - 15f, y0, y0 + 9.5f, 1.4f, new List<Op> { new Op(0f, 3.0f, y0, y0 + 5.2f) }, ruin, seed + 62);
        // altar & bola cahaya
        b.Box(Sl.Marble, new Vector3(0, y0 + 0.6f, zb + 3.2f), new Vector3(3.6f, 1.2f, 2.2f));
        b.Box(Sl.Marble, new Vector3(0, y0 + 1.5f, zb + 3.2f), new Vector3(2.4f, 0.6f, 1.4f));
        Blob(b, Crystal, new Vector3(0, y0 + 2.5f, zb + 3.2f), new Vector3(0.55f, 0.8f, 0.55f), seed, 0.2f, 4, 7, 0f);
        // sampul langit-langit ruang suci
        if (ruin < 0.5f) b.Box(Sl.Stone, new Vector3(0, y0 + 9.9f, zb + 3.5f), new Vector3(W - 3f, 0.8f, 8f));
        else Rubble(b, new Vector3(0, y0, zb + 4f), 5f, 12, seed + 66);
        // pasir dan puing
        int drifts = 3 + (int)(ruin * 5f);
        for (int i = 0; i < drifts; i++)
            SandDrift(b, new Vector3((R01(seed, i, 595) - 0.5f) * (W - 6f), y0 - 0.2f, D / 2f - 4f - R01(seed, i, 596) * (D - 8f)), new Vector3(2.2f + R01(seed, i, 597) * 2.4f, 0.55f + ruin * 0.3f, 2f + R01(seed, i, 598) * 2.4f), seed + i);
        SandDrift(b, new Vector3(0, -0.3f, D / 2f + 5f), new Vector3(6f, 0.9f, 3.2f), seed + 99);
        Rubble(b, new Vector3(-9f, y0, 8f), 5f, 6 + (int)(ruin * 10f), seed + 70);
        TempleArt(b, seed, ruin, W, D, y0, zf);
    }

    public static void Ziggurat(GeoBag b, int seed, float ruin)
    {
        float s0 = 36f, step = 5.6f, th = 3.4f;
        int tiers = 5;
        for (int i = 0; i < tiers; i++)
        {
            float s = s0 - i * step * 2f / 2f * 1f;
            s = s0 - i * step * 1.0f * 1.0f;
            float y = i * th;
            if (ruin > 0.4f && i >= 3)
            {
                int n = 3; float cell = s / n;
                for (int a = 0; a < n; a++)
                    for (int c = 0; c < n; c++)
                    {
                        float hh = th * (0.25f + 0.75f * R01(seed + i, a * 3 + c, 601));
                        if (R01(seed + i, a * 3 + c, 602) < ruin * 0.3f) continue;
                        b.BoxB(Sl.Stone, new Vector3(-s / 2f + cell * (a + 0.5f), y, -s / 2f + cell * (c + 0.5f)), new Vector3(cell, hh, cell));
                    }
            }
            else b.BoxB(Sl.Stone, new Vector3(0, y, 0), new Vector3(s, th, s));
            b.BoxB(Sl.DarkStone, new Vector3(0, y + th - 0.05f, 0), new Vector3(s + 0.4f, 0.14f, s + 0.4f), 0f, true);
            foreach (float sx in new[] { -1f, 1f })
                b.Box(Crystal, new Vector3(sx * (s / 2f + 0.02f), y + th * 0.5f, 0), new Vector3(0.05f, 0.35f, s * 0.72f));
        }
        // tangga lebar di depan (+Z)
        float zf = s0 / 2f;
        for (int i = 0; i < tiers; i++)
        {
            float s = s0 - i * step;
            b.Stairs(Sl.Stone, new Vector3(0, i * th, s / 2f + step * 0.5f), 7f, 7, th / 7f, step / 7f, 0f);
        }
        float topS = s0 - (tiers - 1) * step;
        if (ruin < 0.4f)
        {
            float yT = tiers * th;
            b.BoxB(Sl.Plaster, new Vector3(0, yT, 0), new Vector3(topS * 0.6f, 3.6f, topS * 0.6f));
            b.Box(Sl.Stone, new Vector3(0, yT + 3.7f, 0), new Vector3(topS * 0.6f + 0.8f, 0.5f, topS * 0.6f + 0.8f));
            b.Box(Crystal, new Vector3(0, yT + 1.6f, topS * 0.3f + 0.02f), new Vector3(1.4f, 2.4f, 0.05f));
            b.Cone(Fire, new Vector3(0, yT + 3.9f, 0), 0.8f, 1.7f, 8);
        }
        else Rubble(b, new Vector3(0, tiers * th * 0.9f, 0), topS * 0.3f, 14, seed + 5);
        SandDrift(b, new Vector3(-s0 * 0.3f, 0f, s0 * 0.55f), new Vector3(7f, 1.4f, 5f), seed);
        SandDrift(b, new Vector3(s0 * 0.45f, 0f, -s0 * 0.2f), new Vector3(5f, 1.2f, 7f), seed + 3);
        ZigguratArt(b, seed, ruin, s0, step, th, tiers);
    }

    /// <summary>Patung raja duduk raksasa (menghadap +Z). ruin &gt; 0.5 = kepala jatuh.</summary>
    public static void Colossus(GeoBag b, int seed, float ruin)
    {
        b.BoxB(Sl.Stone, new Vector3(0, -0.5f, 0), new Vector3(10f, 2.1f, 13f));
        float y = 1.6f;
        foreach (float sx in new[] { -1f, 1f })
        {
            b.BoxB(Sl.Stone, new Vector3(sx * 1.7f, y, 3.6f), new Vector3(2.6f, 2.4f, 6.8f));          // paha
            b.BoxB(Sl.Stone, new Vector3(sx * 1.7f, y - 0.1f, 6.6f), new Vector3(2.4f, 3.6f, 1.8f));   // betis depan
            b.BoxB(Sl.DarkStone, new Vector3(sx * 1.7f, y - 0.1f, 7.9f), new Vector3(2.5f, 0.5f, 2.0f));   // kaki
        }
        b.BoxB(Sl.Stone, new Vector3(0, y + 2.3f, 0.2f), new Vector3(5.8f, 3.8f, 3.4f));
        b.BoxB(Sl.Stone, new Vector3(0, y + 6.0f, -0.1f), new Vector3(5.2f, 3.2f, 2.9f));
        b.Box(Sl.Gold, new Vector3(0, y + 4.6f, 1.75f), new Vector3(2.8f, 0.4f, 0.1f));
        b.Box(Crystal, new Vector3(0, y + 5.4f, 1.78f), new Vector3(0.7f, 0.7f, 0.06f));
        foreach (float sx in new[] { -1f, 1f })
        {
            bool broken = ruin > 0.6f && sx > 0;
            b.BoxB(Sl.Stone, new Vector3(sx * 3.3f, y + 3.4f, 0.0f), new Vector3(1.3f, broken ? 2.0f : 3.8f, 1.5f));
            if (!broken) b.BoxB(Sl.Stone, new Vector3(sx * 3.1f, y + 2.45f, 1.6f), new Vector3(1.2f, 1.2f, 3.2f));
            else b.Box(Sl.Stone, new Vector3(5.8f, 0.7f, 3.8f), new Vector3(1.3f, 1.3f, 3.2f), 40f);
        }
        float hy = y + 9.2f;
        if (ruin < 0.5f)
        {
            b.BoxB(Sl.Stone, new Vector3(0, hy - 0.2f, 0), new Vector3(1.6f, 0.7f, 1.6f));
            b.BoxB(Sl.Stone, new Vector3(0, hy + 0.4f, 0.1f), new Vector3(2.4f, 2.6f, 2.5f));
            b.Box(Sl.Stone, new Vector3(0, hy + 1.1f, 1.3f), new Vector3(0.5f, 0.9f, 0.5f));
            b.Box(Crystal, new Vector3(-0.55f, hy + 1.45f, 1.29f), new Vector3(0.42f, 0.12f, 0.05f));
            b.Box(Crystal, new Vector3(0.55f, hy + 1.45f, 1.29f), new Vector3(0.42f, 0.12f, 0.05f));
            b.Box(Sl.Gold, new Vector3(0, hy + 0.2f, 1.0f), new Vector3(0.45f, 1.5f, 0.3f));
            foreach (float sx in new[] { -1f, 1f }) b.BoxB(Sl.ClothB, new Vector3(sx * 1.55f, hy - 0.4f, 0.3f), new Vector3(0.8f, 3.2f, 1.9f));
            b.BoxB(Sl.Gold, new Vector3(0, hy + 3.0f, 0.1f), new Vector3(2.0f, 0.7f, 2.2f));
            b.BoxB(Sl.Gold, new Vector3(0, hy + 3.7f, 0.1f), new Vector3(1.0f, 0.9f, 1.0f));
        }
        else
        {
            b.BoxB(Sl.Stone, new Vector3(0, hy - 0.2f, 0), new Vector3(1.6f, 0.4f, 1.6f), 12f);
            b.Box(Sl.Stone, new Vector3(-6.2f, 1.4f, 9.4f), new Vector3(2.4f, 2.6f, 2.5f), 62f);
            b.Box(Sl.Gold, new Vector3(-6.0f, 1.0f, 10.4f), new Vector3(0.5f, 1.4f, 0.3f), 62f);
            b.Box(Crystal, new Vector3(-5.2f, 1.9f, 8.4f), new Vector3(0.42f, 0.12f, 0.05f), 62f);
            Rubble(b, new Vector3(0, y + 9f, 0), 2f, 6, seed + 3);
        }
        Rubble(b, new Vector3(-5f, 0, 9f), 4f, 8, seed + 9);
        SandDrift(b, new Vector3(-4.8f, -0.2f, 1.0f), new Vector3(4f, 1.8f, 5f), seed);
        SandDrift(b, new Vector3(5.0f, -0.2f, 6.0f), new Vector3(3.6f, 1.2f, 3.4f), seed + 5);
        ColossusArt(b, seed, ruin);
    }

    // ------------------------------------------------------------------ desa gurun
    public static void Jar(GeoBag b, Vector3 at, float s = 1f, int slot = -1)
    {
        if (slot < 0) slot = Sl.Roof;
        Lathe(b, slot, new[] { 0.0f, 0.2f, 0.3f, 0.34f, 0.3f, 0.2f, 0.15f, 0.2f }, new[] { 0f, 0.02f, 0.18f, 0.38f, 0.58f, 0.74f, 0.84f, 0.9f }, 10, at, s, false);
    }

    public static void Jars(GeoBag b, int seed)
    {
        for (int k = 0; k < 4; k++)
            Jar(b, new Vector3((k % 2) * 0.62f - 0.3f + R01(seed, k, 611) * 0.15f, 0f, (k / 2) * 0.55f - 0.2f), 0.9f + R01(seed, k, 612) * 0.5f);
    }

    public static void Rug(GeoBag b, Vector3 c, float w, float d, float yaw, int slot = -1)
    {
        if (slot < 0) slot = Sl.Cloth;
        b.Box(slot, c, new Vector3(w, 0.04f, d), yaw);
    }

    /// <summary>Rumah bata lumpur beratap datar. Depan +Z. floors 2 = ada lantai atas. ruin 0..1.</summary>
    public static void DHouse(GeoBag b, int seed, float w, float d, float ruin, int floors, bool awning)
    {
        float wh = 3.0f, y0 = 0.35f, y1 = y0 + wh, th = 0.42f;
        int wall = ruin > 0.15f ? Grime : Sl.Plaster;
        b.BoxB(Sl.Stone, new Vector3(0, -0.3f, 0), new Vector3(w + 0.3f, y0 + 0.3f, d + 0.3f));
        float dx = DoorX(seed, w);
        var front = new List<Op> { new Op(dx, 1.1f, y0, y0 + 2.15f) };
        var winX = new List<float>();
        foreach (float sx in new[] { -w * 0.32f, w * 0.32f })
            if (Mathf.Abs(sx - dx) > 1.5f && Mathf.Abs(sx) < w / 2f - 0.7f) { front.Add(new Op(sx, 0.55f, y0 + 1.5f, y0 + 2.25f)); winX.Add(sx); }
        float xa = -w / 2f - th / 2f, xb = w / 2f + th / 2f;
        Wall(b, wall, xa, xb, d / 2f, y0, y1, th, front, ruin, seed + 1);
        var back = new List<Op>();
        if (w > 4.4f) back.Add(new Op((R01(seed, 2, 621) - 0.5f) * w * 0.4f, 0.55f, y0 + 1.5f, y0 + 2.25f));
        Wall(b, wall, xa, xb, -d / 2f, y0, y1, th, back, ruin, seed + 2);
        for (int sdx = -1; sdx <= 1; sdx += 2)
        {
            b.Push(new Vector3(sdx * w / 2f, 0f, 0f), 90f);
            var side = new List<Op>();
            if (d > 3.6f) side.Add(new Op(0f, 0.55f, y0 + 1.5f, y0 + 2.25f));
            Wall(b, wall, -d / 2f + th / 2f, d / 2f - th / 2f, 0f, y0, y1, th, side, ruin, seed + 3 + sdx);
            b.Pop();
        }
        // lantai dalam + pintu + jendela berkisi
        b.Box(Sl.Dirt, new Vector3(0, y0 - 0.04f, 0), new Vector3(w - 0.2f, 0.1f, d - 0.2f));
        int woodSlot = ruin > 0.15f ? Weathered : Sl.Wood;
        b.Box(woodSlot, new Vector3(dx, y0 + 2.28f, d / 2f), new Vector3(1.55f, 0.2f, 0.58f));         // lintel kayu
        if (ruin < 0.6f)
        {
            b.Box(woodSlot, new Vector3(dx, y0 + 1.05f, d / 2f - 0.05f), new Vector3(0.98f, 2.05f, 0.07f));
            b.Box(Sl.Metal, new Vector3(dx + 0.32f, y0 + 1.0f, d / 2f + 0.0f), new Vector3(0.07f, 0.07f, 0.04f));
            foreach (float sx in winX) b.Box(woodSlot, new Vector3(sx, y0 + 1.88f, d / 2f - 0.02f), new Vector3(0.62f, 0.8f, 0.05f));
        }
        b.Box(Sl.Stone, new Vector3(dx, y0 - 0.1f, d / 2f + 0.5f), new Vector3(1.7f, 0.22f, 0.85f));
        if (ruin < 0.3f)
        {
            // atap datar + parapet + vigas
            b.Box(wall, new Vector3(0, y1 + 0.12f, 0), new Vector3(w + 0.1f, 0.3f, d + 0.1f));
            b.Box(wall, new Vector3(0, y1 + 0.5f, d / 2f + 0.02f), new Vector3(w + 0.1f, 0.5f, 0.3f));
            b.Box(wall, new Vector3(0, y1 + 0.5f, -d / 2f - 0.02f), new Vector3(w + 0.1f, 0.5f, 0.3f));
            foreach (float sx in new[] { -1f, 1f }) b.Box(wall, new Vector3(sx * (w / 2f + 0.02f), y1 + 0.5f, 0), new Vector3(0.3f, 0.5f, d + 0.1f));
            int n = Mathf.Max(3, Mathf.RoundToInt(w / 0.95f));
            for (int i = 0; i < n; i++)
            {
                float x = -w / 2f + 0.5f + i * (w - 1f) / (n - 1);
                b.Beam(Sl.Wood, new Vector3(x, y1 - 0.22f, d / 2f - 0.3f), new Vector3(x, y1 - 0.2f, d / 2f + 0.55f), 0.14f, 0.14f, Vector3.up);
                b.Beam(Sl.Wood, new Vector3(x, y1 - 0.22f, -d / 2f + 0.3f), new Vector3(x, y1 - 0.2f, -d / 2f - 0.55f), 0.14f, 0.14f, Vector3.up);
            }
            // pancuran air hujan
            b.Box(Sl.Stone, new Vector3(w / 2f - 0.8f, y1 - 0.3f, d / 2f + 0.28f), new Vector3(0.22f, 0.14f, 0.7f));
            if (floors > 1)
            {
                float uw = w * 0.52f, ud = d * 0.56f, ux = -w * 0.2f, uz = -d * 0.18f;
                b.BoxB(wall, new Vector3(ux, y1 + 0.28f, uz), new Vector3(uw, 2.7f, ud));
                b.Box(wall, new Vector3(ux, y1 + 3.12f, uz), new Vector3(uw + 0.25f, 0.28f, ud + 0.25f));
                b.Box(woodSlot, new Vector3(ux + uw * 0.18f, y1 + 1.4f, uz + ud / 2f + 0.02f), new Vector3(0.9f, 1.7f, 0.07f));
                b.Box(Sl.Glow, new Vector3(ux - uw * 0.28f, y1 + 1.9f, uz + ud / 2f + 0.04f), new Vector3(0.3f, 0.4f, 0.05f));
                // tangga luar
                b.Stairs(wall, new Vector3(w / 2f + 1.0f, 0.2f, d * 0.1f), 0.95f, 8, (y1 + 0.4f) / 8f, 0.42f, 90f);
            }
            if (awning)
            {
                foreach (float sx in new[] { -1.6f, 1.6f }) b.Box(Sl.Wood, new Vector3(dx + sx, 1.3f, d / 2f + 1.9f), new Vector3(0.12f, 2.6f, 0.12f));
                b.QuadBoth(Sl.Cloth, new Vector3(dx - 1.8f, 2.65f, d / 2f + 0.25f), new Vector3(dx + 1.8f, 2.65f, d / 2f + 0.25f), new Vector3(dx + 1.8f, 2.2f, d / 2f + 2.05f), new Vector3(dx - 1.8f, 2.2f, d / 2f + 2.05f));
                Rug(b, new Vector3(dx + 0.2f, 0.06f, d / 2f + 1.3f), 1.7f, 1.0f, 8f, Sl.ClothB);
            }
            Jar(b, new Vector3(dx - 1.4f, 0f, d / 2f + 0.9f), 1.2f);
            Jar(b, new Vector3(dx + 1.6f, 0f, d / 2f + 0.7f), 1.0f);
            Jar(b, new Vector3(-w / 2f + 0.8f, y1 + 0.28f, -d / 2f + 0.9f), 1.1f);
            // tungku roti kubah di belakang
            b.Dome(Sl.Plaster, new Vector3(w * 0.25f, 0.2f, -d / 2f - 1.0f), 0.85f, 4, 9, 0.8f);
            b.Box(Sl.DarkStone, new Vector3(w * 0.25f, 0.32f, -d / 2f - 0.2f), new Vector3(0.5f, 0.4f, 0.1f));
            // kain jemuran
            b.QuadBoth(Sl.ClothB, new Vector3(-w * 0.15f, 2.7f, d / 2f + 0.06f), new Vector3(-w * 0.15f + 0.9f, 2.7f, d / 2f + 0.06f), new Vector3(-w * 0.15f + 0.9f, 1.7f, d / 2f + 0.06f), new Vector3(-w * 0.15f, 1.7f, d / 2f + 0.06f));
        }
        else
        {
            // atap runtuh sebagian: sisa balok + pelat miring
            int n = Mathf.Max(3, Mathf.RoundToInt(w / 0.95f));
            for (int i = 0; i < n; i++)
            {
                if (R01(seed, i, 631) < ruin * 0.7f) continue;
                float x = -w / 2f + 0.5f + i * (w - 1f) / (n - 1);
                b.Beam(Weathered, new Vector3(x, y1 - 0.2f, d / 2f - 0.3f), new Vector3(x, y1 - 0.2f - ruin * 0.5f, R01(seed, i, 632) * 0.4f), 0.14f, 0.14f, Vector3.up);
            }
            if (ruin < 0.85f) b.Box(wall, new Vector3(-w * 0.18f, y1 - 0.7f, -d * 0.12f), new Vector3(w * 0.4f, 0.28f, d * 0.55f), 12f);
            Rubble(b, new Vector3(0, y0, 0), Mathf.Min(w, d) * 0.4f, 6 + (int)(ruin * 8f), seed + 31);
            Rubble(b, new Vector3(dx, y0 - 0.2f, d / 2f + 1.3f), 1.8f, 4, seed + 32);
            Jar(b, new Vector3(-w * 0.2f, y0, -d * 0.2f), 1.1f, Sl.Roof);
            b.Beam(Weathered, new Vector3(w * 0.2f, y0 + 0.1f, -d * 0.1f), new Vector3(-w * 0.25f, y0 + 0.5f, d * 0.2f), 0.2f, 0.2f, Vector3.up);
        }
        // pasir yang menelan rumah
        float buried = ruin > 0.15f ? 0.4f + ruin * 1.0f : 0.15f;
        SandDrift(b, new Vector3(dx, y0 - 0.1f, d / 2f + 0.9f), new Vector3(1.7f + ruin * 1.4f, buried, 1.4f), seed + 40);
        SandDrift(b, new Vector3(-w / 2f - 0.3f, y0 - 0.1f, -d * 0.1f), new Vector3(1.3f, buried * 1.2f, d * 0.6f), seed + 41);
        if (ruin > 0.15f) SandDrift(b, new Vector3(w * 0.1f, y0, -d * 0.1f), new Vector3(w * 0.3f, 0.3f + ruin * 0.4f, d * 0.3f), seed + 42);
        DHouseArt(b, seed, w, d, ruin, floors, awning, y0, y1, dx, winX, wall);
    }

    public static void DInn(GeoBag b, int seed)
    {
        float w = 11f, d = 8f;
        DHouse(b, seed, w, d, 0f, 2, true);
        float dx = DoorX(seed, w);
        b.Box(Sl.Wood, new Vector3(dx + 2.5f, 3.0f, d / 2f + 0.35f), new Vector3(0.08f, 0.6f, 0.7f));
        b.Box(Sl.Gold, new Vector3(dx + 2.5f, 3.0f, d / 2f + 0.7f), new Vector3(0.05f, 0.4f, 0.4f));
        for (int k = 0; k < 2; k++) b.Box(Sl.Wood, new Vector3(dx - 2.6f + k * 1.5f, 0.28f, d / 2f + 1.9f), new Vector3(1.2f, 0.32f, 0.45f));
        b.Dome(Sl.Plaster, new Vector3(-w * 0.2f, 0.35f + 3f + 0.28f + 3.4f, -d * 0.18f), 1.3f, 4, 10, 0.9f);
        DInnArt(b, seed, w, d, dx);
    }

    public static void DStall(GeoBag b, int seed)
    {
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                b.Box(Sl.Wood, new Vector3(sx * 1.5f, 1.2f, sz * 1.0f), new Vector3(0.12f, 2.4f, 0.12f));
        b.Box(Sl.Wood, new Vector3(0, 0.85f, 0.85f), new Vector3(3.0f, 0.9f, 0.7f));
        b.Box(Sl.Wood, new Vector3(0, 1.33f, 0.85f), new Vector3(3.1f, 0.07f, 0.85f));
        for (int i = 0; i < 6; i++)
        {
            float x0 = -1.9f + i * 0.633f, x1 = x0 + 0.633f;
            b.QuadBoth(i % 2 == 0 ? Sl.Cloth : Sl.ClothB, new Vector3(x0, 2.55f, 1.45f), new Vector3(x1, 2.55f, 1.45f), new Vector3(x1, 2.95f, -1.15f), new Vector3(x0, 2.95f, -1.15f));
        }
        for (int k = 0; k < 5; k++) Blob(b, k % 2 == 0 ? LeafRed : LeafOrange, new Vector3(-1.1f + k * 0.5f, 1.45f, 0.85f), new Vector3(0.13f, 0.11f, 0.13f), seed + k, 0.1f, 3, 6, 0f);
        Jar(b, new Vector3(-1.2f, 0f, -0.2f), 1.5f);
        Jar(b, new Vector3(-0.5f, 0f, -0.4f), 1.1f);
        Jar(b, new Vector3(1.3f, 0f, -0.3f), 1.3f);
        Rug(b, new Vector3(0.3f, 0.05f, 2.2f), 2.0f, 1.2f, 12f);
        Blob(b, Sl.Thatch, new Vector3(1.0f, 0.3f, 1.8f), new Vector3(0.35f, 0.3f, 0.3f), seed, 0.15f, 3, 7, 0.2f);
        DStallArt(b, seed);
    }

    /// <summary>Kapel berkubah kecil. Depan +Z. ruin &gt; 0.4: kubah runtuh.</summary>
    public static void DomeShrine(GeoBag b, int seed, float ruin)
    {
        float s = 6.2f, wh = 3.8f, y0 = 0.4f, y1 = y0 + wh, th = 0.5f;
        int wall = ruin > 0.15f ? Grime : Sl.Plaster;
        b.BoxB(Sl.Stone, new Vector3(0, -0.3f, 0), new Vector3(s + 1.2f, y0 + 0.3f, s + 1.2f));
        var front = new List<Op> { new Op(0f, 1.5f, y0, y0 + 2.9f) };
        Wall(b, wall, -s / 2f - th / 2f, s / 2f + th / 2f, s / 2f, y0, y1, th, front, ruin, seed + 1);
        Wall(b, wall, -s / 2f - th / 2f, s / 2f + th / 2f, -s / 2f, y0, y1, th, new List<Op> { new Op(0f, 0.6f, y0 + 1.8f, y0 + 3.0f) }, ruin, seed + 2);
        for (int sdx = -1; sdx <= 1; sdx += 2)
        {
            b.Push(new Vector3(sdx * s / 2f, 0f, 0f), 90f);
            Wall(b, wall, -s / 2f + th / 2f, s / 2f - th / 2f, 0f, y0, y1, th, new List<Op> { new Op(0f, 0.6f, y0 + 1.8f, y0 + 3.0f) }, ruin, seed + 3 + sdx);
            b.Pop();
        }
        b.Box(Sl.Stone, new Vector3(0, y0 - 0.1f, s / 2f + 0.8f), new Vector3(2.4f, 0.22f, 1.3f));
        if (ruin < 0.4f)
        {
            b.Box(wall, new Vector3(0, y1 + 0.15f, 0), new Vector3(s + 0.5f, 0.3f, s + 0.5f));
            b.Dome(Sl.Plaster, new Vector3(0, y1 + 0.3f, 0), s * 0.5f, 6, 14, 0.85f);
            b.Spire(Sl.Gold, new Vector3(0, y1 + 0.3f + s * 0.5f * 0.85f - 0.05f, 0), 0.12f, 1.4f, 6);
            foreach (float sx in new[] { -1f, 1f }) foreach (float sz in new[] { -1f, 1f })
                b.Spire(Sl.Plaster, new Vector3(sx * (s / 2f - 0.1f), y1 + 0.3f, sz * (s / 2f - 0.1f)), 0.35f, 1.4f, 8);
        }
        else
        {
            int n = 5;
            for (int i = 0; i < n; i++)
            {
                if (R01(seed, i, 641) < ruin * 0.5f) continue;
                float a = i * 72f * Deg;
                b.Beam(Weathered, new Vector3(Mathf.Sin(a) * s * 0.45f, y1, Mathf.Cos(a) * s * 0.45f), new Vector3(0, y1 + 1.6f - ruin, 0), 0.2f, 0.2f, Vector3.up);
            }
            Rubble(b, new Vector3(0, y0, 0), s * 0.35f, 10, seed + 9);
            Blob(b, Sl.Plaster, new Vector3(3.8f, 0.5f, 2.6f), new Vector3(1.5f, 0.9f, 1.2f), seed, 0.3f, 3, 8, 0.3f);
        }
        b.Box(Sl.Marble, new Vector3(0, y0 + 0.5f, -s / 2f + 1.2f), new Vector3(1.8f, 1.0f, 0.9f));
        b.Box(Crystal, new Vector3(0, y0 + 1.35f, -s / 2f + 1.2f), new Vector3(0.28f, 0.6f, 0.28f));
        SandDrift(b, new Vector3(0, y0 - 0.1f, s / 2f + 1.2f), new Vector3(2.6f + ruin * 1.5f, 0.35f + ruin * 0.6f, 1.6f), seed + 3);
        DomeShrineArt(b, seed, s, ruin, y0, y1);
    }

    // ------------------------------------------------------------------ harness uji
    public static void TestBuildDesert(GeoBag b, string name, int seed)
    {
        switch (name)
        {
            case "palm": Palm(b, seed); break;
            case "cactus": Cactus(b, seed); break;
            case "agave": Agave(b, seed); break;
            case "shrub": DryShrub(b, seed); break;
            case "tumble": Tumbleweed(b, seed); break;
            case "reeds": Reeds(b, seed); break;
            case "hoodoo": Hoodoo(b, seed); break;
            case "bones": BeastSkeleton(b, seed); break;
            case "column": Column(b, Vector3.zero, 8f, 0.7f, seed, 0f); break;
            case "brazier": Brazier(b, seed); break;
            case "obelisk": Obelisk(b, seed, 0f); break;
            case "ruinobelisk": Obelisk(b, seed, 0.8f); break;
            case "archway": Archway(b, seed, 0f); break;
            case "ruinarch": Archway(b, seed, 0.8f); break;
            case "temple": Temple(b, seed, 0f); break;
            case "ruintemple": Temple(b, seed, 0.7f); break;
            case "zig": Ziggurat(b, seed, 0f); break;
            case "ruinzig": Ziggurat(b, seed, 0.8f); break;
            case "colossus": Colossus(b, seed, 0f); break;
            case "ruincolossus": Colossus(b, seed, 0.8f); break;
            case "jars": Jars(b, seed); break;
            case "dhouse": DHouse(b, seed, 6f, 5f, 0f, 1, true); break;
            case "dhouse2": DHouse(b, seed, 7.5f, 6f, 0f, 2, true); break;
            case "ruindhouse": DHouse(b, seed, 6f, 5f, 0.75f, 1, false); break;
            case "dinn": DInn(b, seed); break;
            case "dstall": DStall(b, seed); break;
            case "domeshrine": DomeShrine(b, seed, 0f); break;
            case "ruindome": DomeShrine(b, seed, 0.8f); break;
        }
    }
}
