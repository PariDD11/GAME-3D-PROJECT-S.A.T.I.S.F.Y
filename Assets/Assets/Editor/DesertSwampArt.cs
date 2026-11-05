using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lapisan detail untuk scene gurun &amp; rawa (n42): pohon berdaun kartu, kolom berukir, jendela berbingkai dengan daun jendela,
/// lentera, jemuran, hiasan relief. Dipanggil di akhir fungsi Palm/Cactus/Agave/DHouse/Temple/StiltHouse/... agar semua pemanggil ikut meningkat.
/// </summary>
public static partial class ForestKit
{
    static Vector3 Q(float x, float y, float z) { return new Vector3(x, y, z); }

    // ================================================================== pembantu bangunan
    /// <summary>Bingkai jendela: ambang, lintel, daun jendela, kotak bunga. c = tengah bawah pada muka dinding (z = muka).</summary>
    static void WinTrim(GeoBag b, Vector3 c, float w, float h, int trim, int shutter, bool flowers, bool glow)
    {
        b.Box(trim, c + Q(0, -0.07f, 0.12f), Q(w + 0.45f, 0.14f, 0.3f));                  // ambang
        b.Box(trim, c + Q(0, h + 0.1f, 0.1f), Q(w + 0.5f, 0.2f, 0.26f));                  // lintel
        b.Box(trim, c + Q(-w / 2f - 0.08f, h / 2f, 0.07f), Q(0.16f, h + 0.1f, 0.2f));
        b.Box(trim, c + Q(w / 2f + 0.08f, h / 2f, 0.07f), Q(0.16f, h + 0.1f, 0.2f));
        if (glow) b.Box(Sl.Glow, c + Q(0, h / 2f, 0.02f), Q(w, h, 0.04f));
        b.Box(Sl.Wood, c + Q(0, h / 2f, 0.06f), Q(0.06f, h, 0.05f));
        b.Box(Sl.Wood, c + Q(0, h * 0.55f, 0.06f), Q(w, 0.06f, 0.05f));
        if (shutter >= 0)
        {
            b.Box(shutter, c + Q(-w / 2f - 0.34f, h / 2f, 0.1f), Q(0.44f, h, 0.06f), 14f);
            b.Box(shutter, c + Q(w / 2f + 0.34f, h / 2f, 0.1f), Q(0.44f, h, 0.06f), -14f);
            for (int i = 1; i < 4; i++)
            {
                float yy = h * i / 4f;
                b.Box(Sl.Wood, c + Q(-w / 2f - 0.34f, yy, 0.14f), Q(0.38f, 0.03f, 0.03f), 14f);
                b.Box(Sl.Wood, c + Q(w / 2f + 0.34f, yy, 0.14f), Q(0.38f, 0.03f, 0.03f), -14f);
            }
        }
        if (flowers) FantasyKit.FlowerBox(b, c + Q(0, -0.12f, 0.1f), w + 0.4f);
    }

    /// <summary>Pilaster/sudut berundak (quoin) bergantian di sudut bangunan.</summary>
    static void Quoins(GeoBag b, int slot, float w, float d, float y0, float y1, float size)
    {
        int n = Mathf.Max(2, Mathf.FloorToInt((y1 - y0) / (size * 1.8f)));
        for (int i = 0; i < n; i++)
        {
            float y = y0 + size * 0.9f + i * size * 1.8f;
            float ext = (i % 2 == 0) ? 1.25f : 0.95f;
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                    b.Box(slot, Q(sx * (w / 2f), y, sz * (d / 2f)), Q(size * ext, size * 1.2f, size * ext));
        }
    }

    /// <summary>Gelang pita relief (cornice/plinth) mengelilingi tapak persegi panjang.</summary>
    static void BandRing(GeoBag b, int slot, float w, float d, float y, float h, float proud)
    {
        b.Box(slot, Q(0, y, d / 2f), Q(w + proud * 2f, h, proud * 2f));
        b.Box(slot, Q(0, y, -d / 2f), Q(w + proud * 2f, h, proud * 2f));
        b.Box(slot, Q(w / 2f, y, 0), Q(proud * 2f, h, d));
        b.Box(slot, Q(-w / 2f, y, 0), Q(proud * 2f, h, d));
    }

    /// <summary>Gerigi batu di sepanjang tepi atap (gaya benteng gurun).</summary>
    static void Merlons(GeoBag b, int slot, float w, float d, float y, float s)
    {
        int nx = Mathf.Max(3, Mathf.RoundToInt(w / (s * 2.2f))), nz = Mathf.Max(3, Mathf.RoundToInt(d / (s * 2.2f)));
        for (int i = 0; i < nx; i++)
        {
            float x = -w / 2f + s + i * (w - 2f * s) / (nx - 1);
            b.Box(slot, Q(x, y, d / 2f), Q(s, s * 1.1f, s * 0.5f));
            b.Box(slot, Q(x, y, -d / 2f), Q(s, s * 1.1f, s * 0.5f));
        }
        for (int i = 1; i < nz - 1; i++)
        {
            float z = -d / 2f + s + i * (d - 2f * s) / (nz - 1);
            b.Box(slot, Q(w / 2f, y, z), Q(s * 0.5f, s * 1.1f, s));
            b.Box(slot, Q(-w / 2f, y, z), Q(s * 0.5f, s * 1.1f, s));
        }
    }

    /// <summary>Untaian daun menjuntai (willow, lumut Spanyol) berupa pita segitiga bergoyang.</summary>
    static void LeafStrand(GeoBag b, int slot, Vector3 from, float len, int seed, float w)
    {
        int segs = 5;
        Vector3 prev = from;
        float sway = (R01(seed, 3, 951) - 0.5f) * 0.5f;
        for (int i = 1; i <= segs; i++)
        {
            float t = i / (float)segs;
            Vector3 cur = from + Q(sway * Mathf.Sin(t * 2.4f), -len * t, sway * 0.6f * Mathf.Cos(t * 2f));
            float ww = w * (1f - t * 0.75f);
            Vector3 sd = Q(ww, 0, 0), sd2 = Q(0, 0, ww);
            b.QuadBoth(slot, prev - sd, prev + sd, cur + sd * 0.8f, cur - sd * 0.8f);
            b.QuadBoth(slot, prev - sd2, prev + sd2, cur + sd2 * 0.8f, cur - sd2 * 0.8f);
            prev = cur;
        }
    }

    // ================================================================== pohon gurun
    static void PalmArt(GeoBag b, int seed, float sc, List<Vector3> pts, List<float> rad, Vector3 top)
    {
        // cincin bekas pelepah pada batang
        for (int i = 1; i < pts.Count - 1; i++)
            for (int k = 0; k < 2; k++)
                b.Cyl(Bark, pts[i] + Q(0, k * 0.4f * sc, 0), rad[i] * 1.16f, 0.1f * sc, 8, rad[i] * 1.12f, true);
        // pangkal batang membesar + akar
        for (int k = 0; k < 5; k++)
        {
            float a = (k * 72f + R01(seed, k, 960) * 30f) * Deg;
            b.Cone(Bark, pts[0] + Q(Mathf.Sin(a) * 0.38f * sc, -0.05f, Mathf.Cos(a) * 0.38f * sc), 0.14f * sc, 0.6f * sc, 5);
        }
        // anak daun menjuntai di setiap pelepah
        int nf = 12;
        for (int k = 0; k < nf; k++)
        {
            float yaw = k * 360f / nf + (R01(seed, k, 502) - 0.5f) * 24f;
            float L = (3.4f + R01(seed, k, 503) * 1.1f) * sc;
            float rise = L * (k % 3 == 0 ? 0.55f : 0.32f);
            float droop = L * (0.75f + R01(seed, k, 504) * 0.3f);
            Vector3 dirH = Q(Mathf.Sin(yaw * Deg), 0f, Mathf.Cos(yaw * Deg));
            Vector3 sideV = Vector3.Cross(dirH, Vector3.up).normalized;
            for (int s = 2; s <= 9; s++)
            {
                float t = s / 10f;
                Vector3 p = top + dirH * (L * t) + Vector3.up * (0.1f + Mathf.Sin(t * Mathf.PI * 0.75f) * rise - droop * t * t * t);
                float ll = 0.55f * sc * (1f - t * 0.55f);
                Vector3 tipN = p + dirH * ll * 0.35f + sideV * ll + Vector3.down * ll * 0.8f;
                Vector3 tipS = p + dirH * ll * 0.35f - sideV * ll + Vector3.down * ll * 0.8f;
                b.Tri(LeafGreen, p, tipN, p + dirH * 0.12f); b.Tri(LeafGreen, p, p + dirH * 0.12f, tipN);
                b.Tri(LeafGreen, p, tipS, p + dirH * 0.12f); b.Tri(LeafGreen, p, p + dirH * 0.12f, tipS);
            }
            // pelepah tua kering di bawah mahkota
            if (k % 3 == 1)
            {
                Vector3 e = top + dirH * (L * 0.5f) + Vector3.down * (L * 0.55f);
                Vector3 s2 = sideV * 0.2f * sc;
                b.QuadBoth(Weathered, top + Q(0, -0.1f, 0) - s2, top + Q(0, -0.1f, 0) + s2, e + s2 * 0.4f, e - s2 * 0.4f);
            }
        }
        // tandan kurma
        for (int k = 0; k < 6; k++)
        {
            float a = (k * 60f + 15f) * Deg;
            Vector3 p = top + Q(Mathf.Sin(a) * 0.4f * sc, -0.55f * sc, Mathf.Cos(a) * 0.4f * sc);
            b.Beam(Weathered, p + Q(0, 0.3f, 0), p, 0.03f, 0.03f);
            Blob(b, LeafOrange, p + Q(0, -0.12f, 0), Q(0.12f, 0.18f, 0.12f) * sc, seed + k * 3, 0.15f, 3, 6, 0f);
        }
        // rumput kering di pangkal
        for (int k = 0; k < 4; k++) TreeKit.GrassTuft(b, LeafYellow, Q(Mathf.Sin(k * 1.6f) * 0.9f * sc, 0f, Mathf.Cos(k * 1.6f) * 0.9f * sc), 0.7f * sc, seed + k);
    }

    static void CactusArt(GeoBag b, int seed, float sc, float h, float wob)
    {
        float r = 0.34f * sc;
        // duri halus + rusuk
        for (int k = 0; k < 44; k++)
        {
            float t = 0.08f + 0.88f * R01(seed, k, 970);
            float a = R01(seed, k, 971) * 360f * Deg;
            Vector3 c = Q(wob * Mathf.Sin(t * 3f) + Mathf.Sin(a) * r * 0.98f, t * h, Mathf.Cos(a) * r * 0.98f);
            Vector3 o = Q(Mathf.Sin(a), 0.25f, Mathf.Cos(a)).normalized;
            b.Beam(Sl.Marble, c, c + o * 0.16f * sc, 0.016f * sc, 0.016f * sc, Vector3.up);
        }
        for (int k = 0; k < 8; k++)
        {
            float a = k * 45f * Deg;
            b.Beam(LeafPine, Q(wob * 0.02f + Mathf.Sin(a) * r * 1.0f, 0.1f, Mathf.Cos(a) * r * 1.0f), Q(wob * Mathf.Sin(h / h * 3f) + Mathf.Sin(a) * r * 0.45f, h * 0.97f, Mathf.Cos(a) * r * 0.45f), 0.045f * sc, 0.03f * sc, Vector3.up);
        }
        // bunga di puncak dan sisi
        for (int k = 0; k < 4; k++)
        {
            float a = (k * 90f + 20f) * Deg;
            Vector3 p = Q(wob * 0.9f + Mathf.Sin(a) * r * 0.5f, h - 0.04f, Mathf.Cos(a) * r * 0.5f);
            b.Ball(k % 2 == 0 ? Sl.Blossom : LeafRed, p, 0.1f * sc, 3, 6, 0.8f);
            b.Ball(Sl.Gold, p + Q(0, 0.05f, 0), 0.04f * sc, 3, 5);
        }
        // kerikil dan rumput di pangkal
        for (int k = 0; k < 3; k++) Blob(b, Rock, Q(Mathf.Sin(k * 2.2f) * 0.7f, 0.05f, Mathf.Cos(k * 2.2f) * 0.7f) * sc, Q(0.22f, 0.12f, 0.2f) * sc, seed + k, 0.25f, 3, 6, 0.4f);
        TreeKit.GrassTuft(b, LeafYellow, Q(0.55f * sc, 0f, -0.3f * sc), 0.5f * sc, seed);
    }

    static void AgaveArt(GeoBag b, int seed, float sc)
    {
        for (int k = 0; k < 10; k++)
        {
            float yaw = k * 36f + R01(seed, k, 981) * 14f;
            Vector3 d = Dir(yaw, 0.1f);
            Vector3 p = d * (0.55f + (k % 3) * 0.12f) * sc + Q(0, 0.35f * sc, 0);
            b.Cone(Weathered, p + d * 0.28f * sc, 0.025f * sc, 0.12f * sc, 4);
        }
        if (seed % 3 == 0)
        {
            float h = (2.4f + R01(seed, 1, 982) * 1.0f) * sc;
            b.Beam(LeafYellow, Q(0, 0.2f, 0), Q(0.1f * sc, h, 0), 0.07f * sc, 0.035f * sc, Vector3.right);
            for (int k = 0; k < 6; k++)
            {
                float t = 0.55f + k * 0.07f;
                float a = k * 100f * Deg;
                Vector3 s = Q(0.1f * sc * t, h * t, 0);
                Vector3 e = s + Q(Mathf.Sin(a) * 0.4f * sc, 0.25f * sc, Mathf.Cos(a) * 0.4f * sc);
                b.Beam(LeafYellow, s, e, 0.03f * sc, 0.02f * sc, Vector3.up);
                b.Ball(Sl.Gold, e, 0.07f * sc, 3, 5);
            }
        }
    }

    static void DryShrubArt(GeoBag b, int seed, float sc)
    {
        TreeKit.LeafCards(b, LeafYellow, Q(0, 0.4f * sc, 0), Q(0.55f, 0.35f, 0.55f) * sc, 12, seed, 0.28f * sc, -0.2f);
        for (int k = 0; k < 3; k++) TreeKit.GrassTuft(b, LeafYellow, Q(Mathf.Sin(k * 2.2f) * 0.5f * sc, 0, Mathf.Cos(k * 2.2f) * 0.5f * sc), 0.5f * sc, seed + k);
    }

    // ================================================================== bangunan gurun
    static void DHouseArt(GeoBag b, int seed, float w, float d, float ruin, int floors, bool awning, float y0, float y1, float dx, List<float> winX, int wall)
    {
        if (ruin > 0.7f) return;
        int trim = Sl.PlasterB;
        float th = 0.42f;
        // plinth + cornice + pita warna
        BandRing(b, Sl.Stone, w + th * 0.5f, d + th * 0.5f, y0 + 0.28f, 0.34f, 0.12f);
        if (ruin < 0.3f)
        {
            BandRing(b, trim, w + th * 0.9f, d + th * 0.9f, y1 - 0.08f, 0.22f, 0.1f);
            b.Box(Sl.ClothB, Q(0, y1 - 0.58f, d / 2f + 0.23f), Q(w, 0.2f, 0.05f));
            b.Box(Sl.Cloth, Q(0, y1 - 0.58f, -d / 2f - 0.23f), Q(w, 0.2f, 0.05f));
            for (int i = 0; i < Mathf.RoundToInt(w / 0.7f); i++)
                b.Box(Sl.Gold, Q(-w / 2f + 0.45f + i * 0.7f, y1 - 0.58f, d / 2f + 0.26f), Q(0.14f, 0.1f, 0.03f), 45f);
            Merlons(b, wall, w + 0.2f, d + 0.2f, y1 + 0.95f, 0.5f);
        }
        Quoins(b, trim, w + th * 0.9f, d + th * 0.9f, y0, y1, 0.38f);
        // jendela
        foreach (float sx in winX)
            WinTrim(b, Q(sx, y0 + 1.5f, d / 2f + th / 2f), 0.92f, 0.78f, trim, ruin < 0.3f ? Sl.Cloth : Weathered, ruin < 0.2f, ruin < 0.15f);
        // pintu: pilaster + lintel berukir + lentera + tanaman
        float jx = 1.1f + 0.25f;
        foreach (float s in new[] { -1f, 1f })
        {
            b.Box(Sl.Stone, Q(dx + s * jx, y0 + 1.15f, d / 2f + th / 2f + 0.05f), Q(0.3f, 2.3f, 0.3f));
            b.Cyl(Sl.Stone, Q(dx + s * jx, y0 + 2.28f, d / 2f + th / 2f + 0.05f), 0.24f, 0.12f, 8, 0.2f, true);
        }
        b.Box(Sl.Stone, Q(dx, y0 + 2.5f, d / 2f + th / 2f + 0.05f), Q(jx * 2f + 0.5f, 0.2f, 0.34f));
        b.Box(Sl.Gold, Q(dx, y0 + 2.66f, d / 2f + th / 2f + 0.06f), Q(1.0f, 0.08f, 0.04f));
        b.Ball(Sl.Gold, Q(dx, y0 + 2.8f, d / 2f + th / 2f + 0.06f), 0.1f, 3, 6);
        if (ruin < 0.3f)
        {
            AnimeKit.HangLantern(b, Q(dx + jx + 0.9f, y0 + 2.7f, d / 2f + th / 2f + 0.35f), 0.5f);
            FantasyKit.HerbBundle(b, Q(dx - jx - 0.8f, y0 + 2.45f, d / 2f + th / 2f + 0.2f), seed);
            FantasyKit.HerbBundle(b, Q(dx - jx - 1.2f, y0 + 2.45f, d / 2f + th / 2f + 0.2f), seed + 1);
            // pot besar dengan tanaman
            Jar(b, Q(dx + jx + 0.7f, y0, d / 2f + th / 2f + 0.6f), 1.5f, Sl.Roof);
            Blob(b, LeafGreen, Q(dx + jx + 0.7f, y0 + 0.85f, d / 2f + th / 2f + 0.6f), Q(0.3f, 0.26f, 0.3f), seed, 0.3f, 3, 7, 0f);
            TreeKit.LeafCards(b, LeafGreen, Q(dx + jx + 0.7f, y0 + 0.95f, d / 2f + th / 2f + 0.6f), Q(0.34f, 0.3f, 0.34f), 8, seed, 0.3f, -0.2f);
            // tali jemuran pakaian di sisi
            FantasyKit.Laundry(b, Q(-w / 2f + 0.5f, y0 + 2.7f, d / 2f + th / 2f + 0.15f), Q(-w / 2f + 2.2f, y0 + 2.5f, d / 2f + th / 2f + 0.15f), seed);
            // tangga bambu ke atap + antena kain
            FantasyKit.Ladder(b, Q(-w / 2f + 0.3f, 0f, -d / 2f - 0.3f), y1 + 0.6f, 0.08f);
            // tangki air di atap
            b.Cyl(Sl.Wood, Q(w * 0.28f, y1 + 0.3f, d * 0.2f), 0.55f, 0.8f, 10, 0.5f, true);
            b.Cyl(Sl.Metal, Q(w * 0.28f, y1 + 0.5f, d * 0.2f), 0.57f, 0.05f, 10, 0.57f, true);
            b.Cyl(Sl.Metal, Q(w * 0.28f, y1 + 0.95f, d * 0.2f), 0.52f, 0.05f, 10, 0.52f, true);
            // cabai/jagung kering tergantung
            for (int i = 0; i < 4; i++) FantasyKit.HerbBundle(b, Q(w / 2f - 0.9f - i * 0.28f, y1 - 0.75f, -d / 2f - 0.5f), seed + i);
        }
        else
        {
            FantasyKit.BrickPatch(b, Q(-w * 0.2f, y0 + 1.2f, d / 2f + th / 2f), 1.2f, 1.0f, seed);
            FantasyKit.BrickPatch(b, Q(w * 0.28f, y0 + 2.0f, d / 2f + th / 2f), 0.9f, 0.7f, seed + 1);
        }
        TreeKit.GrassTuft(b, LeafYellow, Q(-w / 2f - 0.25f, y0 - 0.2f, d * 0.3f), 0.6f, seed);
        TreeKit.GrassTuft(b, LeafYellow, Q(w / 2f + 0.25f, y0 - 0.2f, -d * 0.2f), 0.6f, seed + 2);
    }

    static void DInnArt(GeoBag b, int seed, float w, float d, float dx)
    {
        float y1 = 3.35f;
        // pelana batu & kubah lampu atap
        b.Cyl(Sl.Gold, Q(-w * 0.2f, y1 + 3.28f + 3.4f + 1.2f, -d * 0.18f), 0.06f, 1.3f, 5, 0.01f, true);
        b.Ball(Sl.Gold, Q(-w * 0.2f, y1 + 3.28f + 3.4f + 1.25f, -d * 0.18f), 0.14f, 3, 6);
        // tong kayu & meja di teras
        for (int k = 0; k < 3; k++)
        {
            float x = dx - 2.9f + k * 1.4f;
            b.Cyl(Sl.Wood, Q(x, 0f, d / 2f + 2.5f), 0.3f, 0.72f, 10, 0.3f, true);
            b.Cyl(Sl.Metal, Q(x, 0.15f, d / 2f + 2.5f), 0.315f, 0.05f, 10, 0.315f, true);
            b.Cyl(Sl.Metal, Q(x, 0.55f, d / 2f + 2.5f), 0.315f, 0.05f, 10, 0.315f, true);
        }
        b.Cyl(Sl.Wood, Q(dx + 3.4f, 0.6f, d / 2f + 2.2f), 0.7f, 0.08f, 10, 0.7f, true);
        b.Cyl(Sl.Wood, Q(dx + 3.4f, 0f, d / 2f + 2.2f), 0.08f, 0.62f, 6, 0.08f, true);
        // tanduk unta di plakat
        b.Beam(Sl.Wood, Q(dx + 2.5f, 3.2f, d / 2f + 0.3f), Q(dx + 2.5f, 3.6f, d / 2f + 0.3f), 0.05f, 0.05f);
        // bendera segitiga
        for (int i = 0; i < 7; i++)
        {
            float t0 = i / 7f;
            float x0 = -w / 2f + t0 * w;
            float yb = y1 + 0.3f - Mathf.Sin(t0 * Mathf.PI) * 0.4f;
            b.QuadBoth(i % 2 == 0 ? Sl.Cloth : Sl.ClothB, Q(x0, yb, d / 2f + 1.2f), Q(x0 + w / 7f * 0.8f, yb - 0.02f, d / 2f + 1.2f), Q(x0 + w / 14f, yb - 0.55f, d / 2f + 1.2f), Q(x0 + w / 14f, yb - 0.55f, d / 2f + 1.2f));
        }
    }

    static void DStallArt(GeoBag b, int seed)
    {
        // rumbai kain, keranjang, buah bertumpuk, timbangan
        for (int i = 0; i < 12; i++)
        {
            float x = -1.85f + i * 0.336f;
            b.Tri(i % 2 == 0 ? Sl.ClothB : Sl.Cloth, Q(x, 2.52f, 1.46f), Q(x + 0.3f, 2.52f, 1.46f), Q(x + 0.15f, 2.2f, 1.5f));
            b.Tri(i % 2 == 0 ? Sl.ClothB : Sl.Cloth, Q(x + 0.3f, 2.52f, 1.46f), Q(x, 2.52f, 1.46f), Q(x + 0.15f, 2.2f, 1.5f));
        }
        for (int k = 0; k < 3; k++)
        {
            Vector3 p = Q(-1.0f + k * 1.0f, 1.38f, 0.9f);
            b.Cyl(Sl.WoodLight, p, 0.3f, 0.2f, 10, 0.34f, false);
            for (int j = 0; j < 6; j++)
                b.Ball(k == 0 ? LeafRed : (k == 1 ? LeafOrange : Sl.Gold), p + Q(Mathf.Sin(j * 1.05f) * 0.12f, 0.2f + (j / 4) * 0.1f, Mathf.Cos(j * 1.05f) * 0.12f), 0.09f, 3, 6);
        }
        b.Beam(Sl.Metal, Q(0, 2.4f, 0.6f), Q(0, 1.9f, 0.6f), 0.02f, 0.02f);
        b.Beam(Sl.Metal, Q(-0.3f, 1.9f, 0.6f), Q(0.3f, 1.9f, 0.6f), 0.02f, 0.02f);
        b.Cyl(Sl.Gold, Q(-0.3f, 1.6f, 0.6f), 0.14f, 0.05f, 8, 0.1f, true);
        b.Cyl(Sl.Gold, Q(0.3f, 1.6f, 0.6f), 0.14f, 0.05f, 8, 0.1f, true);
        AnimeKit.HangLantern(b, Q(1.5f, 2.4f, 1.2f), 0.45f);
    }

    static void DomeShrineArt(GeoBag b, int seed, float s, float ruin, float y0, float y1)
    {
        if (ruin > 0.6f) return;
        BandRing(b, Sl.Stone, s + 0.5f, s + 0.5f, y0 + 0.2f, 0.4f, 0.1f);
        BandRing(b, Sl.PlasterB, s + 0.7f, s + 0.7f, y1 - 0.1f, 0.3f, 0.1f);
        Quoins(b, Sl.PlasterB, s + 0.5f, s + 0.5f, y0, y1, 0.42f);
        foreach (float sdx in new[] { -1f, 1f })
        {
            b.Push(Q(sdx * (s / 2f + 0.25f), 0, 0), sdx * 90f);
            WinTrim(b, Q(0, y0 + 1.8f, 0), 0.9f, 1.2f, Sl.PlasterB, Sl.ClothB, false, ruin < 0.3f);
            b.Pop();
        }
        // pilaster pintu & lentera
        foreach (float sx in new[] { -1f, 1f })
        {
            b.Cyl(Sl.Stone, Q(sx * 1.9f, y0, s / 2f + 0.5f), 0.26f, 3.4f, 10, 0.22f, true);
            b.Ball(Sl.Stone, Q(sx * 1.9f, y0 + 3.5f, s / 2f + 0.5f), 0.3f, 3, 8, 0.7f);
            AnimeKit.HangLantern(b, Q(sx * 1.2f, y0 + 3.1f, s / 2f + 0.8f), 0.5f);
        }
        b.Box(Sl.Stone, Q(0, y0 + 3.55f, s / 2f + 0.5f), Q(4.4f, 0.28f, 0.5f));
        if (ruin < 0.4f)
        {
            // pita kubah + bulan sabit emas
            for (int k = 0; k < 8; k++)
            {
                float a = k * 45f * Deg;
                b.Beam(Sl.Gold, Q(Mathf.Sin(a) * s * 0.5f, y1 + 0.4f, Mathf.Cos(a) * s * 0.5f), Q(Mathf.Sin(a) * 0.1f, y1 + 0.3f + s * 0.5f * 0.85f, Mathf.Cos(a) * 0.1f), 0.06f, 0.06f, Vector3.up);
            }
            float ty = y1 + 0.3f + s * 0.5f * 0.85f + 1.3f;
            b.Ball(Sl.Gold, Q(0, ty, 0), 0.22f, 4, 8);
            b.Cone(Sl.Gold, Q(0, ty + 0.2f, 0), 0.16f, 0.6f, 6);
        }
        // kolam kecil & bunga di depan
        b.Cyl(Sl.Stone, Q(0, 0f, s / 2f + 2.8f), 0.9f, 0.4f, 12, 0.9f, false);
        b.Disc(Sl.Water, Q(0, 0.36f, s / 2f + 2.8f), 0.8f, 12, 0f);
        for (int k = 0; k < 3; k++) Blob(b, LeafGreen, Q(-2.4f + k * 2.4f, 0.3f, s / 2f + 3.2f), Q(0.5f, 0.35f, 0.5f), seed + k, 0.3f, 3, 7, 0.1f);
    }

    static void PylonArt(GeoBag b, int seed, float cx, float zf, float y0, float hh, float ruin)
    {
        // relief lajur emas/kristal, mahkota cornice, bendera tiang
        for (int row = 0; row < 5; row++)
        {
            float y = y0 + 1.6f + row * (hh - 3f) / 4f;
            b.Box(Sl.Gold, Q(cx, y, zf + 2.28f), Q(2.4f, 0.14f, 0.05f));
            for (int k = 0; k < 4; k++) b.Box(Sl.Gold, Q(cx - 0.9f + k * 0.6f, y + 0.35f, zf + 2.3f), Q(0.12f, 0.36f, 0.04f), k * 15f);
        }
        if (ruin < 0.6f)
        {
            b.Box(Sl.Stone, Q(cx, y0 + hh + 0.15f, zf), Q(7.9f, 0.6f, 4.6f));
            b.Box(Sl.Gold, Q(cx, y0 + hh + 0.5f, zf + 2.2f), Q(7.9f, 0.14f, 0.1f));
            b.Cyl(Sl.Wood, Q(cx + 2.5f, y0 + 5f, zf + 2.55f), 0.12f, hh - 4f, 6, 0.1f, true);
            b.QuadBoth(Sl.Cloth, Q(cx + 2.5f, y0 + hh + 0.8f, zf + 2.55f), Q(cx + 4.5f, y0 + hh + 0.5f, zf + 2.55f), Q(cx + 4.5f, y0 + hh - 1.9f, zf + 2.55f), Q(cx + 2.5f, y0 + hh - 1.6f, zf + 2.55f));
        }
    }

    static void ColumnArt(GeoBag b, Vector3 at, float h, float r, int seed, float brk)
    {
        // relief cincin warna & alur
        if (brk > 0.3f) return;
        for (int k = 0; k < 10; k++)
        {
            float a = k * 36f * Deg;
            b.Beam(Sl.Stone, at + Q(Mathf.Sin(a) * r * 0.96f, 0.9f, Mathf.Cos(a) * r * 0.96f), at + Q(Mathf.Sin(a) * r * 0.9f, h - 1.1f, Mathf.Cos(a) * r * 0.9f), 0.07f, 0.06f, Vector3.up);
        }
        b.Cyl(Sl.ClothB, at + Q(0, h * 0.62f, 0), r * 1.03f, 0.22f, 12, r * 1.03f, false);
        b.Cyl(Sl.Gold, at + Q(0, h * 0.62f + 0.22f, 0), r * 1.04f, 0.05f, 12, r * 1.04f, false);
        b.Cyl(Sl.Gold, at + Q(0, h * 0.62f - 0.05f, 0), r * 1.04f, 0.05f, 12, r * 1.04f, false);
    }

    static void TempleArt(GeoBag b, int seed, float ruin, float W, float D, float y0, float zf)
    {
        // sphinx penjaga & kolam di tangga depan, obor
        foreach (float sx in new[] { -1f, 1f })
        {
            Vector3 p = Q(sx * 5.4f, 0.5f, D / 2f + 9f);
            b.BoxB(Sl.Stone, p, Q(1.7f, 0.5f, 3.6f));
            b.BoxB(Sl.Stone, p + Q(0, 0.5f, -0.3f), Q(1.3f, 0.9f, 2.6f));
            Blob(b, Sl.Stone, p + Q(0, 1.65f, 1.0f), Q(0.55f, 0.6f, 0.6f), seed + (int)sx, 0.08f, 4, 8, 0f);
            b.Box(Sl.ClothB, p + Q(0, 1.7f, 0.7f), Q(1.5f, 0.55f, 0.18f));
            b.Box(Sl.Gold, p + Q(0, 2.25f, 1.0f), Q(0.3f, 0.12f, 0.3f));
            b.Box(Crystal, p + Q(sx * 0.2f, 1.75f, 1.58f), Q(0.14f, 0.07f, 0.04f));
            b.Box(Crystal, p + Q(-sx * 0.2f, 1.75f, 1.58f), Q(0.14f, 0.07f, 0.04f));
        }
        // dua obor pilar di tangga
        foreach (float sx in new[] { -1f, 1f })
        {
            Vector3 p = Q(sx * 2.9f, 0.0f, D / 2f + 6.2f);
            b.Cyl(Sl.Stone, p, 0.34f, 1.7f, 8, 0.26f, true);
            b.Cyl(Sl.Gold, p + Q(0, 1.7f, 0), 0.4f, 0.14f, 8, 0.4f, true);
            b.Disc(Sl.Glow, p + Q(0, 1.92f, 0), 0.34f, 8, 0f);
            b.Cone(Fire, p + Q(0, 1.9f, 0), 0.3f, 0.7f, 6);
        }
        // pita hieroglif pada dinding samping
        foreach (float sx in new[] { -1f, 1f })
            for (int k = 0; k < 18; k++)
            {
                float z = D / 2f - 12f - k * 1.5f;
                if (R01(seed, k, 990 + (int)sx) < ruin * 0.5f) continue;
                b.Box(Crystal, Q(sx * (W / 2f - 0.5f + 0.64f * sx * 0f) + sx * 0.05f, y0 + 6.6f, z), Q(0.04f, 0.2f + R01(seed, k, 991) * 0.45f, 0.3f));
            }
        // jalan prosesi
        b.Box(Sl.PlasterB, Q(0, y0 - 0.4f, D / 2f + 14f), Q(6.5f, 0.12f, 12f));
        for (int k = 0; k < 6; k++) b.Box(Sl.Stone, Q(((k % 2) - 0.5f) * 1.5f, y0 - 0.32f, D / 2f + 9f + k * 1.8f), Q(1.2f, 0.06f, 1.2f), k * 13f);
        for (int k = 0; k < 8; k++) TreeKit.GrassTuft(b, LeafYellow, Q(((k % 2) * 2 - 1) * (4.5f + (k / 2) * 0.4f), 0f, D / 2f + 5f + k * 1.4f), 0.7f, seed + k);
    }

    static void ZigguratArt(GeoBag b, int seed, float ruin, float s0, float step, float th, int tiers)
    {
        // lajur relief emas/warna di tiap tingkat + obor sudut + jalur tangga berpagar
        for (int i = 0; i < tiers; i++)
        {
            float s = s0 - i * step, y = i * th;
            if (ruin > 0.4f && i >= 3) continue;
            b.Box(Sl.ClothB, Q(0, y + th * 0.62f, s / 2f + 0.03f), Q(s * 0.9f, 0.22f, 0.06f));
            b.Box(Sl.Gold, Q(0, y + th * 0.62f + 0.16f, s / 2f + 0.05f), Q(s * 0.9f, 0.05f, 0.05f));
            for (int k = 0; k < Mathf.RoundToInt(s / 1.6f); k++)
                b.Box(Sl.PlasterB, Q(-s / 2f + 0.8f + k * 1.6f, y + th - 0.2f, s / 2f + 0.05f), Q(0.5f, 0.4f, 0.1f));
            foreach (float cx in new[] { -1f, 1f })
            {
                b.Cyl(Sl.Stone, Q(cx * (s / 2f - 0.2f), y + th, s / 2f - 0.2f), 0.3f, 1.0f, 8, 0.22f, true);
                b.Disc(Sl.Glow, Q(cx * (s / 2f - 0.2f), y + th + 1.02f, s / 2f - 0.2f), 0.26f, 8, 0f);
                b.Cone(Fire, Q(cx * (s / 2f - 0.2f), y + th + 1.0f, s / 2f - 0.2f), 0.24f, 0.5f, 6);
            }
        }
        // pagar tangga
        float h = tiers * th;
        foreach (float sx in new[] { -1f, 1f })
            b.Beam(Sl.Stone, Q(sx * 3.7f, 0.2f, s0 / 2f + step), Q(sx * 3.7f, h + 0.5f, s0 / 2f - (tiers - 1) * step + step * 0.5f), 0.5f, 0.5f, Vector3.right);
    }

    static void ColossusArt(GeoBag b, int seed, float ruin)
    {
        // nemes (penutup kepala) bergaris, kalung emas, pelita kaki, hiasan dasar
        if (ruin < 0.5f)
        {
            float hy = 1.6f + 9.2f;
            for (int k = 0; k < 6; k++)
                b.Box(Sl.ClothB, Q(0, hy + 1.2f - k * 0.0f, 1.1f - k * 0.0f) + Q(0, 0.1f * k - 0.3f, 0), Q(2.45f, 0.12f, 2.6f));
            b.Box(Sl.Gold, Q(0, hy + 2.2f, 0.1f), Q(2.5f, 0.2f, 2.6f));
            b.Cone(Sl.Gold, Q(0, hy + 2.35f, 1.15f), 0.18f, 0.4f, 4);
        }
        foreach (float sx in new[] { -1f, 1f })
        {
            b.Cyl(Sl.Stone, Q(sx * 5.3f, 0f, 6.9f), 0.45f, 1.9f, 8, 0.34f, true);
            b.Disc(Sl.Glow, Q(sx * 5.3f, 1.92f, 6.9f), 0.4f, 8, 0f);
            b.Cone(Fire, Q(sx * 5.3f, 1.9f, 6.9f), 0.34f, 0.8f, 6);
        }
        for (int k = 0; k < 9; k++) b.Box(Crystal, Q(-4f + k * 1.0f, 1.0f, 7.62f), Q(0.14f, 0.7f, 0.04f));
    }

    // ================================================================== pohon & bangunan rawa
    static void MangroveArt(GeoBag b, int seed, float s, Vector3 top)
    {
        for (int k = 0; k < 4; k++)
        {
            Vector3 c = top + Q((R01(seed, k, 805) - 0.5f) * 2.4f * s, -0.4f * s + (k % 2) * 0.5f * s, (R01(seed, k, 806) - 0.5f) * 2.4f * s);
            TreeKit.LeafCards(b, LeafGreen, c, Q(1.9f, 0.95f, 1.9f) * s, 18, seed + k, 0.62f * s, -0.4f);
            TreeKit.LeafCards(b, LeafYellow, c + Q(0, 0.3f * s, 0), Q(1.2f, 0.7f, 1.2f) * s, 6, seed + k + 9, 0.5f * s, 0f);
        }
        for (int k = 0; k < 10; k++)
        {
            float a = (k * 36f + R01(seed, k, 951) * 20f) * Deg;
            float rr = (0.6f + R01(seed, k, 952) * 1.6f) * s;
            b.Cone(Bark, Q(Mathf.Sin(a) * rr, -0.4f, Mathf.Cos(a) * rr), 0.07f * s, (0.3f + R01(seed, k, 953) * 0.4f) * s, 4);
        }
        for (int k = 0; k < 5; k++) TreeKit.GrassTuft(b, LeafPine, Q(Mathf.Sin(k * 1.3f) * 1.6f * s, -0.2f, Mathf.Cos(k * 1.3f) * 1.6f * s), 0.6f * s, seed + k);
    }

    static void WillowArt(GeoBag b, int seed, float s, Vector3 top)
    {
        for (int k = 0; k < 5; k++)
        {
            float a = (k * 72f + R01(seed, k, 824) * 40f) * Deg;
            Vector3 c = top + Q(Mathf.Sin(a) * 2.0f * s, 0.2f * s + R01(seed, k, 825) * 0.6f * s, Mathf.Cos(a) * 2.0f * s);
            TreeKit.LeafCards(b, LeafGreen, c, Q(1.3f, 0.7f, 1.3f) * s, 12, seed + k, 0.5f * s, -0.4f);
            for (int j = 0; j < 7; j++)
            {
                float aa = a + (j - 3) * 0.3f;
                Vector3 from = c + Q(Mathf.Sin(aa) * 1.0f * s, -0.15f * s, Mathf.Cos(aa) * 1.0f * s);
                LeafStrand(b, j % 3 == 0 ? LeafYellow : LeafGreen, from, (2.5f + R01(seed, k * 9 + j, 954) * 1.9f) * s, seed + k * 9 + j, 0.14f * s);
            }
        }
        TreeKit.LeafCards(b, LeafGreen, top + Q(0, 0.3f * s, 0), Q(1.6f, 0.8f, 1.6f) * s, 14, seed + 40, 0.6f * s, -0.3f);
        for (int k = 0; k < 4; k++) TreeKit.GrassTuft(b, LeafPine, Q(Mathf.Sin(k * 1.7f) * 1.2f * s, -0.2f, Mathf.Cos(k * 1.7f) * 1.2f * s), 0.7f * s, seed + k);
    }

    static void CypressArt(GeoBag b, int seed, float s, float h)
    {
        for (int k = 0; k < 4; k++)
        {
            Vector3 c = Q((R01(seed, k, 836) - 0.5f) * 0.8f * s, h * (0.6f + 0.13f * k), (R01(seed, k, 837) - 0.5f) * 0.8f * s);
            TreeKit.LeafCards(b, LeafPine, c, Q(1.6f - k * 0.25f, 0.9f, 1.6f - k * 0.25f) * s, 16, seed + k, 0.5f * s, -0.4f);
        }
        for (int k = 0; k < 7; k++)
            LeafStrand(b, LeafYellow, Q((R01(seed, k, 955) - 0.5f) * 2f * s, h * (0.55f + 0.07f * (k % 4)), (R01(seed, k, 956) - 0.5f) * 2f * s), (1.2f + R01(seed, k, 957) * 1.6f) * s, seed + k, 0.1f * s);
        // akar berlutut ekstra + pelebaran pangkal
        for (int k = 0; k < 6; k++)
        {
            float a = (k * 60f + 20f) * Deg;
            b.Cone(Bark, Q(Mathf.Sin(a) * 0.62f * s, -0.4f, Mathf.Cos(a) * 0.62f * s), 0.26f * s, 1.1f * s, 5);
        }
    }

    static void StiltHouseArt(GeoBag b, int seed, float w, float d, float ruin, float pf, float y0, float y1)
    {
        if (ruin > 0.7f) return;
        int trim = ruin > 0.3f ? Weathered : Sl.WoodLight;
        // papan vertikal (board-and-batten) di muka depan
        if (ruin < 0.5f)
            for (int i = 0; i < Mathf.RoundToInt(w / 0.55f); i++)
            {
                float x = -w / 2f + 0.3f + i * (w - 0.6f) / Mathf.Max(1, Mathf.RoundToInt(w / 0.55f) - 1);
                b.Box(Sl.Wood, Q(x, y0 + 1.3f, d / 2f + 0.12f), Q(0.07f, 2.5f, 0.04f));
                b.Box(Sl.Wood, Q(x, y0 + 1.3f, -d / 2f - 0.12f), Q(0.07f, 2.5f, 0.04f));
            }
        // balok pita atas/bawah + sudut
        BandRing(b, trim, w + 0.2f, d + 0.2f, y0 + 0.06f, 0.16f, 0.06f);
        BandRing(b, trim, w + 0.2f, d + 0.2f, y1 - 0.05f, 0.18f, 0.07f);
        foreach (float sx in new[] { -1f, 1f }) foreach (float sz in new[] { -1f, 1f })
            b.Box(trim, Q(sx * (w / 2f + 0.05f), (y0 + y1) / 2f, sz * (d / 2f + 0.05f)), Q(0.2f, y1 - y0, 0.2f));
        // jendela berdaun & kotak tanaman
        foreach (float sx in new[] { -w * 0.32f, w * 0.32f })
            if (Mathf.Abs(sx) > 1.3f && Mathf.Abs(sx) < w / 2f - 0.6f)
                WinTrim(b, Q(sx, y0 + 0.85f, d / 2f + 0.1f), 0.62f, 0.9f, trim, ruin < 0.3f ? Sl.Cloth : Weathered, ruin < 0.2f, false);
        // pagar beranda
        float rz = d / 2f + 1.65f;
        for (int i = 0; i <= 6; i++)
        {
            float x = -w * 0.3f + i * w * 0.6f / 6f;
            b.Box(Sl.Wood, Q(x, pf + 0.6f, rz + 0.62f), Q(0.07f, 0.9f, 0.07f));
        }
        b.Box(Sl.Wood, Q(0, pf + 1.05f, rz + 0.62f), Q(w * 0.6f, 0.07f, 0.1f));
        foreach (float sx in new[] { -1f, 1f })
        {
            b.Box(Sl.Wood, Q(sx * w * 0.3f, pf + 0.6f, rz), Q(0.07f, 0.9f, 1.25f));
            b.Box(Sl.Wood, Q(sx * w * 0.3f, pf + 1.05f, rz), Q(0.1f, 0.07f, 1.3f));
        }
        if (ruin < 0.3f)
        {
            // jaring, keranjang ikan, tali tambat
            b.QuadBoth(Sl.Cloth, Q(-w / 2f - 0.05f, y1 - 0.2f, d * 0.2f), Q(-w / 2f - 0.05f, y1 - 0.2f, -d * 0.2f), Q(-w / 2f - 0.9f, y0 + 0.4f, -d * 0.2f), Q(-w / 2f - 0.9f, y0 + 0.4f, d * 0.2f));
            for (int k = 0; k < 3; k++)
            {
                b.Cyl(Sl.WoodLight, Q(w * 0.1f + k * 0.55f, pf + 0.1f, rz + 0.3f), 0.2f, 0.34f, 8, 0.16f, false);
                b.Ball(Sl.Metal, Q(w * 0.1f + k * 0.55f, pf + 0.44f, rz + 0.3f), 0.11f, 3, 6, 0.6f);
            }
            AnimeKit.HangLantern(b, Q(-0.9f, y1 - 0.3f, d / 2f + 0.9f), 0.55f);
            AnimeKit.HangLantern(b, Q(0.9f, y1 - 0.3f, d / 2f + 0.9f), 0.55f);
            // cerobong asap & akar tanaman rambat
            for (int k = 0; k < 4; k++) Hang(b, Q(-w / 2f + 0.2f + k * 0.3f, y1 + 0.2f, d / 2f + 0.5f), 0.9f + k * 0.2f, seed + k, Ivy, 0.03f);
            Blob(b, Ivy, Q(w / 2f, pf + 0.3f, d / 2f * 0.3f), Q(0.3f, 0.6f, 0.4f), seed + 3, 0.3f, 3, 7, 0f);
            b.Cyl(Sl.WoodLight, Q(w * 0.38f, pf + 0.1f, -d * 0.3f), 0.38f, 0.6f, 10, 0.34f, true);                                            // tong air hujan
            TreeKit.LeafCards(b, LeafGreen, Q(-w / 2f - 0.2f, pf - 0.7f, d * 0.2f), Q(0.5f, 0.5f, 0.5f), 8, seed, 0.3f, -0.3f);
        }
        // tiang lumut + lampu tiang kapur
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                Blob(b, Moss, Q(sx * (w / 2f + 0.3f), pf - 0.5f, sz * (d / 2f + 0.3f)), Q(0.28f, 0.55f, 0.28f), seed + sx * 3 + sz, 0.3f, 3, 7, 0f);
        // pelampung pada tiang
        b.Ball(Sl.Cloth, Q(w / 2f + 0.5f, pf + 0.1f, d / 2f * 0.0f), 0.18f, 3, 6);
    }

    static void WitchHutArt(GeoBag b, int seed)
    {
        float pf = 1.2f;
        // jamur besar di tiang + botol bercahaya + tali pengikat
        for (int k = 0; k < 6; k++)
        {
            float a = (k * 60f + 10f) * Deg;
            Vector3 p = Q(Mathf.Sin(a) * 3.05f, -0.7f + (k % 3) * 0.5f, Mathf.Cos(a) * 3.05f);
            b.Cyl(Sl.PlasterB, p, 0.05f, 0.26f, 5, 0.05f, true);
            b.Dome(k % 2 == 0 ? LeafRed : Sl.Blossom, p + Q(0, 0.24f, 0), 0.22f, 3, 8, 0.6f);
            b.Ball(Sl.Marble, p + Q(0.07f, 0.34f, 0.07f), 0.03f, 3, 5);
        }
        // jerami berlapis atap (cincin pita)
        for (int j = 0; j < 5; j++)
        {
            float y = pf + 2.9f + j * 0.34f;
            float r = 2.7f - j * 0.4f;
            b.Cyl(OldThatch, Q(0, y, 0), r, 0.1f, 10, r * 0.99f, false);
        }
        // jendela bulat menyala + pintu melengkung dan kenop
        b.Disc(Sl.Glow, Q(-1.2f, pf + 1.6f, 1.76f), 0.28f, 10, 0f);
        b.Ball(Sl.Glow, Q(1.4f, pf + 1.6f, 1.45f), 0.26f, 3, 8, 0.6f);
        b.Ball(Sl.Gold, Q(0.3f, pf + 1.1f, 2.12f), 0.07f, 3, 6);
        for (int k = 0; k < 5; k++) b.Box(Weathered, Q(-0.5f + k * 0.25f, pf + 1.15f, 2.12f), Q(0.03f, 1.8f, 0.02f));
        // lonceng angin dari tulang
        for (int k = 0; k < 5; k++)
        {
            Vector3 p = Q(2.2f + k * 0.12f, pf + 2.5f, 2.2f + (k % 2) * 0.15f);
            b.Beam(Sl.Marble, p, p + Q(0, -0.55f - (k % 3) * 0.15f, 0), 0.05f, 0.04f);
        }
        // kuali kecil pada tripod & tumpukan kayu bakar
        for (int k = 0; k < 6; k++) b.Cyl(Weathered, Q(-2.3f + (k % 3) * 0.28f, pf, -1.6f - (k / 3) * 0.28f), 0.11f, 0.7f, 6, 0.1f, true);
        // lampu gantung hijau
        AnimeKit.HangLantern(b, Q(-0.9f, pf + 2.4f, 2.3f), 0.5f);
        b.Ball(Crystal, Q(-0.9f, pf + 2.2f, 2.3f), 0.1f, 3, 6);
    }

    static void ChapelArt(GeoBag b, int seed, float ruin, float w, float d, float y0, float y1)
    {
        if (ruin > 0.7f) return;
        // jendela lancip + tracery pada dinding samping
        foreach (float sx in new[] { -1f, 1f })
            foreach (float oz in new[] { -3.6f, 0f, 3.6f })
            {
                b.Push(Q(sx * (w / 2f + 0.3f), 0, oz), sx * 90f);
                b.Box(Sl.Stone, Q(0, y0 + 4.05f, 0), Q(1.55f, 0.24f, 0.4f));
                b.Box(Sl.Stone, Q(0, y0 + 1.5f, 0), Q(1.6f, 0.2f, 0.45f));
                b.Cone(Sl.Stone, Q(0, y0 + 4.1f, 0), 0.22f, 0.7f, 4);
                b.Box(Crystal, Q(0, y0 + 2.9f, 0.06f), Q(0.06f, 2.6f, 0.04f));
                b.Pop();
            }
        // penopang (buttress) dengan pinnacle
        foreach (float sx in new[] { -1f, 1f })
            for (int k = 0; k < 4; k++)
            {
                float z = -d / 2f + 1.5f + k * (d - 3f) / 3f;
                float oz = (k == 1 || k == 2) ? 1.8f : 0f;
                b.BoxB(Grime, Q(sx * (w / 2f + 0.5f), y0, z + oz * 0f), Q(0.9f, y1 - y0 - 1.3f, 0.8f));
                b.BoxB(Sl.Stone, Q(sx * (w / 2f + 0.5f), y1 - 1.3f, z), Q(1.0f, 0.4f, 0.9f));
                b.Cone(Sl.Stone, Q(sx * (w / 2f + 0.5f), y1 - 0.9f, z), 0.4f, 1.1f, 4);
            }
        BandRing(b, Sl.Stone, w + 0.3f, d + 0.3f, y1 - 0.2f, 0.3f, 0.12f);
        // mawar kaca bulat di muka depan + lampu pintu
        b.Ball(Crystal, Q(0, y0 + 4.3f, d / 2f + 0.28f), 0.7f, 3, 10, 0.18f);
        for (int k = 0; k < 8; k++)
        {
            float a = k * 45f * Deg;
            b.Beam(Sl.Stone, Q(0, y0 + 4.3f, d / 2f + 0.3f), Q(Mathf.Sin(a) * 0.72f, y0 + 4.3f + Mathf.Cos(a) * 0.72f, d / 2f + 0.3f), 0.05f, 0.04f, Vector3.forward);
        }
        AnimeKit.HangLantern(b, Q(-1.6f, y0 + 3.0f, d / 2f + 0.6f), 0.55f);
        AnimeKit.HangLantern(b, Q(1.6f, y0 + 3.0f, d / 2f + 0.6f), 0.55f);
        // batu nisan mengitari
        for (int k = 0; k < 6; k++)
        {
            float x = (k % 2 == 0 ? -1f : 1f) * (w / 2f + 2.2f + (k / 2) * 0.6f);
            float z = -d / 2f + 1.5f + k * 1.7f;
            b.Box(Sl.Stone, Q(x, 0.5f, z), Q(0.6f, 1.0f, 0.18f), (R01(seed, k, 958) - 0.5f) * 20f);
            b.Ball(Sl.Stone, Q(x, 1.02f, z), 0.3f, 3, 6, 0.4f);
            Blob(b, Moss, Q(x, 0.1f, z), Q(0.45f, 0.18f, 0.35f), seed + k, 0.3f, 3, 6, 0.3f);
        }
    }

    static void CryptArt(GeoBag b, int seed, float ruin, float scale)
    {
        float w = 4.6f * scale, d = 5.6f * scale, h = 3.4f * scale;
        BandRing(b, Sl.Stone, w, d, 0.5f + h - 0.1f, 0.3f, 0.1f);
        BandRing(b, Sl.Stone, w, d, 0.9f, 0.22f, 0.08f);
        // obor biru dan relief tengkorak di fasad, pagar besi
        foreach (float sx in new[] { -1f, 1f })
        {
            b.Beam(Sl.Metal, Q(sx * (w / 2f + 0.5f), 0.3f, d / 2f + 1.2f), Q(sx * (w / 2f + 0.5f), 2.0f, d / 2f + 1.2f), 0.08f, 0.08f);
            b.Cyl(Sl.Metal, Q(sx * (w / 2f + 0.5f), 2.0f, d / 2f + 1.2f), 0.18f, 0.18f, 8, 0.25f, true);
            b.Cone(Crystal, Q(sx * (w / 2f + 0.5f), 2.15f, d / 2f + 1.2f), 0.2f, 0.5f, 5);
        }
        for (int i = 0; i < 8; i++)
        {
            float x = -w / 2f - 0.2f + i * (w + 0.4f) / 7f;
            b.Beam(Sl.Metal, Q(x, 0.2f, d / 2f + 3.1f), Q(x, 1.5f, d / 2f + 3.1f), 0.04f, 0.04f);
            b.Cone(Sl.Metal, Q(x, 1.5f, d / 2f + 3.1f), 0.05f, 0.18f, 4);
        }
        b.Beam(Sl.Metal, Q(-w / 2f - 0.2f, 1.2f, d / 2f + 3.1f), Q(w / 2f + 0.2f, 1.2f, d / 2f + 3.1f), 0.04f, 0.04f);
        // peti batu terbuka, lilin, tanaman rambat
        b.Box(Sl.Stone, Q(w / 2f + 1.4f, 0.35f, 0.4f), Q(0.9f, 0.5f, 2.2f), 10f);
        b.Box(Sl.DarkStone, Q(w / 2f + 1.4f, 0.65f, 0.4f), Q(1.0f, 0.12f, 2.3f), 10f);
        for (int k = 0; k < 4; k++) b.Cyl(Sl.Marble, Q(-w / 2f - 0.6f + k * 0.18f, 0.0f, d / 2f + 0.9f), 0.05f, 0.18f + k * 0.04f, 6, 0.04f, true);
        for (int k = 0; k < 5; k++) Hang(b, Q(-w / 2f + 0.4f + k * 0.55f * scale, 0.5f + h - 0.1f, d / 2f + 0.15f), 0.9f + k * 0.15f, seed + k, Ivy, 0.05f);
        Blob(b, Moss, Q(0, 0.5f + h + 0.3f, -d * 0.2f), Q(1.8f, 0.2f, 1.2f) * scale, seed, 0.3f, 3, 8, 0.3f);
    }

    static void DockArt(GeoBag b, int seed, float len, float ruin)
    {
        // tali tambat, bolder, lampu tiang tambahan, jaring, kotak
        int n = Mathf.Max(2, Mathf.RoundToInt(len / 1.1f));
        for (int i = 0; i <= (int)(len / 3f); i++)
            foreach (float sx in new[] { -1.05f, 1.05f })
            {
                Vector3 p = Q(sx, 0.6f, 0.3f + i * 3f);
                b.Cyl(Sl.WoodLight, p, 0.16f, 0.5f, 7, 0.14f, true);
                if (ruin < 0.3f && i < (int)(len / 3f))
                {
                    Vector3 q = Q(sx, 0.6f, 0.3f + (i + 1) * 3f);
                    b.Beam(Sl.Cloth, p + Q(0, 0.45f, 0), (p + q) / 2f + Q(0, 0.3f, 0), 0.04f, 0.04f);
                    b.Beam(Sl.Cloth, (p + q) / 2f + Q(0, 0.3f, 0), q + Q(0, 0.45f, 0), 0.04f, 0.04f);
                }
                if (ruin > 0.2f) Blob(b, Moss, Q(sx, -0.1f, 0.3f + i * 3f), Q(0.3f, 0.5f, 0.3f), seed + i, 0.3f, 3, 6, 0f);
            }
        if (ruin < 0.3f)
        {
            for (int k = 0; k < 3; k++) b.Box(Sl.WoodLight, Q(-0.6f + k * 0.55f, 0.85f, len * 0.4f), Q(0.5f, 0.42f, 0.5f), k * 20f);
            b.Cyl(Sl.WoodLight, Q(0.7f, 0.6f, len * 0.7f), 0.3f, 0.5f, 8, 0.28f, true);
            AnimeKit.HangLantern(b, Q(-1.05f, 2.7f, len * 0.5f), 0.5f);
            b.Beam(Sl.Wood, Q(-1.05f, 0.6f, len * 0.5f), Q(-1.05f, 2.9f, len * 0.5f), 0.1f, 0.1f);
        }
        for (int k = 0; k < 4; k++) TreeKit.GrassTuft(b, LeafPine, Q((k % 2 == 0 ? -1.3f : 1.3f), -0.1f, 0.5f + k * 1.4f), 0.8f, seed + k);
    }
}
