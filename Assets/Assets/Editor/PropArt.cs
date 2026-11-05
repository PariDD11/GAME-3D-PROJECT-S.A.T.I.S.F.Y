using System.Collections.Generic;
using UnityEngine;

/// <summary>Properti kota bergaya anime-fantasi: air mancur dewi bersayap, sumur beratap, lapak pasar, patung ksatria, pagar.</summary>
public static class PropArt
{
    static Vector3 P(float x, float y, float z) { return new Vector3(x, y, z); }

    static void Arc(GeoBag b, int slot, Vector3 a, Vector3 c, float peak, float w)
    {
        Vector3 prev = a;
        for (int i = 1; i <= 5; i++)
        {
            float t = i / 5f;
            Vector3 p = Vector3.Lerp(a, c, t);
            p.y += peak * 4f * t * (1f - t);
            b.Beam(slot, prev, p, w, w);
            prev = p;
        }
    }

    /// <summary>Mangkuk air berongga: dinding miring tanpa tutup, cincin bibir, air, dan bola-bola di bibir.</summary>
    static void Bowl(GeoBag b, Vector3 baseP, float r0, float r1, float h, float lipR, int n, int scallops)
    {
        b.Cyl(Sl.Marble, baseP, r0, h, n, r1, false, false);
        b.Annulus(Sl.Marble, baseP + P(0f, 0f, 0f), r1 - lipR, r1 + 0.12f, 0f, 360f, n, baseP.y + h);
        b.Disc(Sl.Water, baseP + P(0f, 0f, 0f), r1 - lipR - 0.02f, n, baseP.y + h - 0.12f);
        b.Disc(Sl.Marble, baseP, r1 - lipR, n, baseP.y + h - 0.35f);
        for (int i = 0; i < scallops; i++)
        {
            float a = i * Mathf.PI * 2f / scallops;
            b.Ball(Sl.Marble, baseP + P(Mathf.Sin(a) * (r1 + 0.05f), h + 0.02f, Mathf.Cos(a) * (r1 + 0.05f)), r1 * 0.09f, 3, 6, 0.9f);
        }
    }

    // ------------------------------------------------------------------ air mancur
    public static void Fountain(GeoBag b, Vector3 c, float s)
    {
        b.Push(c, 0f);
        // lantai batu bulat di sekeliling
        b.Disc(Sl.Cobble, Vector3.zero, 8.6f * s, 28, 0.025f);
        // bak utama
        b.Cyl(Sl.DarkStone, Vector3.zero, 8.1f * s, 0.4f * s, 24, 7.9f * s, true);
        b.Cyl(Sl.Marble, P(0f, 0.4f * s, 0f), 7.6f * s, 0.55f * s, 24, 7.55f * s, false);
        b.Annulus(Sl.Marble, Vector3.zero, 6.85f * s, 7.8f * s, 0f, 360f, 24, 0.97f * s);
        b.Disc(Sl.Marble, Vector3.zero, 6.9f * s, 24, 0.4f * s);
        b.Disc(Sl.Water, Vector3.zero, 6.85f * s, 24, 0.8f * s);
        for (int i = 0; i < 24; i++)
        {
            float a = i * Mathf.PI * 2f / 24f;
            b.Ball(Sl.Marble, P(Mathf.Sin(a) * 7.35f * s, 1.0f * s, Mathf.Cos(a) * 7.35f * s), 0.26f * s, 3, 6, 0.9f);
            // panel ukir di dinding luar
            b.Box(Sl.PlasterB, P(Mathf.Sin(a) * 7.65f * s, 0.62f * s, Mathf.Cos(a) * 7.65f * s), P(1.05f * s, 0.34f * s, 0.07f), a * Mathf.Rad2Deg);
            if (i % 2 == 0) b.Ball(Sl.Gold, P(Mathf.Sin(a) * 7.72f * s, 0.62f * s, Mathf.Cos(a) * 7.72f * s), 0.08f * s, 3, 5);
        }
        // batu pijakan dalam air
        for (int i = 0; i < 6; i++)
        {
            float a = i * 1.047f + 0.3f;
            b.Cyl(Sl.Stone, P(Mathf.Sin(a) * 5.6f * s, 0.4f * s, Mathf.Cos(a) * 5.6f * s), 0.55f * s, 0.55f * s, 7, 0.5f * s, true);
        }
        // tiang tingkat dua
        b.Cyl(Sl.Marble, P(0f, 0.8f * s, 0f), 1.5f * s, 0.35f * s, 14, 1.3f * s, true);
        b.Cyl(Sl.Marble, P(0f, 1.15f * s, 0f), 1.0f * s, 1.5f * s, 12, 0.75f * s, true);
        b.Cyl(Sl.Gold, P(0f, 1.9f * s, 0f), 0.88f * s, 0.12f * s, 12, 0.88f * s, true);
        Bowl(b, P(0f, 2.65f * s, 0f), 1.2f * s, 3.4f * s, 0.7f * s, 0.55f * s, 20, 16);
        // tiang tingkat tiga
        b.Cyl(Sl.Marble, P(0f, 3.35f * s, 0f), 0.62f * s, 1.9f * s, 10, 0.42f * s, true);
        b.Cyl(Sl.Gold, P(0f, 4.2f * s, 0f), 0.56f * s, 0.1f * s, 10, 0.56f * s, true);
        Bowl(b, P(0f, 5.25f * s, 0f), 0.5f * s, 1.55f * s, 0.4f * s, 0.35f * s, 14, 10);
        // dewi
        float gy = 5.65f * s;
        b.Cyl(Sl.Marble, P(0f, gy, 0f), 0.82f * s, 1.15f * s, 14, 0.27f * s, true);               // gaun mengembang
        for (int i = 0; i < 8; i++)                                                                 // lipatan gaun
        {
            float a = i * Mathf.PI * 2f / 8f;
            b.Beam(Sl.Marble, P(Mathf.Sin(a) * 0.3f * s, gy + 1.0f * s, Mathf.Cos(a) * 0.3f * s), P(Mathf.Sin(a) * 0.88f * s, gy + 0.02f * s, Mathf.Cos(a) * 0.88f * s), 0.09f * s, 0.07f * s);
        }
        b.Cyl(Sl.Marble, P(0f, gy + 1.1f * s, 0f), 0.27f * s, 0.62f * s, 10, 0.24f * s, true);   // torso
        b.Ball(Sl.Marble, P(0f, gy + 1.78f * s, 0f), 0.27f * s, 5, 8);                              // kepala
        b.Ball(Sl.Gold, P(0f, gy + 1.88f * s, -0.05f * s), 0.28f * s, 4, 8, 0.9f);                 // rambut
        for (int i = -1; i <= 1; i++)
            b.Beam(Sl.Gold, P(i * 0.12f * s, gy + 1.75f * s, -0.15f * s), P(i * 0.2f * s, gy + 0.65f * s, -0.32f * s), 0.12f * s, 0.07f * s);
        b.Beam(Sl.Marble, P(0.26f * s, gy + 1.5f * s, 0f), P(0.52f * s, gy + 2.2f * s, 0.12f * s), 0.12f * s, 0.12f * s);   // lengan terangkat
        b.Beam(Sl.Marble, P(-0.26f * s, gy + 1.5f * s, 0f), P(-0.52f * s, gy + 2.2f * s, 0.12f * s), 0.12f * s, 0.12f * s);
        b.Ball(Sl.Glow, P(0f, gy + 2.45f * s, 0.12f * s), 0.4f * s, 5, 8);                          // orb sihir
        for (int i = 0; i < 18; i++)                                                                // cincin cahaya
        {
            float a = i * Mathf.PI * 2f / 18f;
            b.Ball(Sl.Glow, P(Mathf.Sin(a) * 0.85f * s, gy + 2.45f * s + Mathf.Cos(a) * 0.85f * s, 0.12f * s), 0.06f * s, 3, 5);
        }
        foreach (float sx in new[] { -1f, 1f })                                                     // sayap
            for (int i = 0; i < 6; i++)
            {
                float t = i / 5f;
                Vector3 root = P(sx * 0.14f * s, gy + 1.45f * s - t * 0.2f * s, -0.18f * s);
                Vector3 tip = P(sx * (0.55f + 0.95f * (1f - t * 0.55f)) * s, gy + 2.45f * s - t * 1.45f * s, -0.62f * s - t * 0.2f * s);
                Vector3 sd = P(0f, 0.1f * s, 0f);
                b.QuadBoth(Sl.PlasterB, root - sd, root + sd, tip + sd * 0.6f, tip - sd * 0.6f);
            }
        // pancuran: dari mangkuk tiga ke dua, dari dua ke bak
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI * 2f / 8f;
            Arc(b, Sl.Water, P(Mathf.Sin(a) * 1.5f * s, 5.6f * s, Mathf.Cos(a) * 1.5f * s), P(Mathf.Sin(a) * 2.7f * s, 3.4f * s, Mathf.Cos(a) * 2.7f * s), 0.35f * s, 0.07f * s);
        }
        for (int i = 0; i < 14; i++)
        {
            float a = i * Mathf.PI * 2f / 14f + 0.15f;
            Arc(b, Sl.Water, P(Mathf.Sin(a) * 3.4f * s, 3.35f * s, Mathf.Cos(a) * 3.4f * s), P(Mathf.Sin(a) * 5.4f * s, 0.85f * s, Mathf.Cos(a) * 5.4f * s), 0.7f * s, 0.07f * s);
            b.Ball(Sl.Water, P(Mathf.Sin(a) * 5.45f * s, 0.84f * s, Mathf.Cos(a) * 5.45f * s), 0.2f * s, 3, 5, 0.35f);
        }
        // tiang lentera & semak bunga di luar bak
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            Vector3 lp = P(Mathf.Sin(a) * 8.7f * s, 0f, Mathf.Cos(a) * 8.7f * s);
            b.Cyl(Sl.Metal, lp, 0.3f, 0.5f, 8, 0.2f, true);
            b.Cyl(Sl.Metal, lp + P(0f, 0.5f, 0f), 0.1f, 3.0f, 8, 0.08f, true);
            BuildKit.Lantern(b, lp + P(0f, 3.7f, 0f));
            b.Ball(Sl.Gold, lp + P(0f, 4.1f, 0f), 0.12f, 3, 6);
        }
        for (int i = 0; i < 8; i++)
        {
            float a = (i * 45f) * Mathf.Deg2Rad;
            AnimeKit.FlowerBush(b, P(Mathf.Sin(a) * 9.0f * s, 0f, Mathf.Cos(a) * 9.0f * s), 0.65f, i % 2 == 0 ? Sl.Blossom : Sl.Gold);
        }
        b.Pop();
    }

    // ------------------------------------------------------------------ sumur
    public static void Well(GeoBag b, Vector3 c)
    {
        b.Push(c, 0f);
        // pijakan batu
        for (int i = 0; i < 12; i++)
        {
            float a = i * Mathf.PI * 2f / 12f + 0.2f;
            float r = 2.45f + 0.18f * Mathf.Sin(i * 2.7f);
            b.Cyl(Sl.Stone, P(Mathf.Sin(a) * r, 0f, Mathf.Cos(a) * r), 0.45f, 0.14f, 7, 0.4f, true);
        }
        // dinding sumur
        b.Cyl(Sl.DarkStone, Vector3.zero, 1.85f, 0.3f, 16, 1.8f, true);
        b.Cyl(Sl.Stone, P(0f, 0.3f, 0f), 1.7f, 0.95f, 16, 1.65f, false);
        for (int k = 0; k < 3; k++)
            b.Cyl(Sl.DarkStone, P(0f, 0.55f + k * 0.28f, 0f), 1.7f - k * 0.025f + 0.04f, 0.05f, 16, 1.7f - k * 0.025f + 0.04f, false);
        b.Annulus(Sl.Stone, Vector3.zero, 1.1f, 1.85f, 0f, 360f, 16, 1.25f);
        b.Disc(Sl.Water, Vector3.zero, 1.12f, 16, 0.75f);
        for (int i = 0; i < 16; i++)
        {
            float a = i * Mathf.PI * 2f / 16f;
            b.Ball(Sl.Stone, P(Mathf.Sin(a) * 1.55f, 1.3f, Mathf.Cos(a) * 1.55f), 0.2f, 3, 6, 0.8f);
        }
        // tiang dan atap
        foreach (float sx in new[] { -1f, 1f })
        {
            b.Cyl(Sl.Wood, P(sx * 1.55f, 1.2f, 0f), 0.15f, 2.55f, 7, 0.13f, true);
            b.Ball(Sl.Wood, P(sx * 1.55f, 3.75f, 0f), 0.18f, 3, 6, 0.8f);
            b.Beam(Sl.WoodLight, P(sx * 1.55f, 1.9f, 0f), P(sx * 0.9f, 3.5f, 0f), 0.14f, 0.14f);
        }
        b.Beam(Sl.Wood, P(-1.65f, 3.7f, 0f), P(1.65f, 3.7f, 0f), 0.22f, 0.22f);
        b.Gable(Sl.Roof, Sl.Plaster, Sl.Wood, P(0f, 3.8f, 0f), 4.0f, 3.4f, 1.55f, 0.55f, true);
        b.Ball(Sl.Gold, P(-2.15f, 4.15f, 0f), 0.15f, 3, 6);
        b.Ball(Sl.Gold, P(2.15f, 4.15f, 0f), 0.15f, 3, 6);
        // kerekan: poros, engkol, tali, ember
        b.Beam(Sl.Wood, P(-1.55f, 2.7f, 0f), P(1.55f, 2.7f, 0f), 0.3f, 0.3f);
        b.Beam(Sl.WoodLight, P(1.55f, 2.7f, 0f), P(2.1f, 2.7f, 0.35f), 0.07f, 0.07f);
        b.Beam(Sl.Wood, P(2.1f, 2.7f, 0.35f), P(2.1f, 2.7f, 0.82f), 0.09f, 0.09f);
        b.Ball(Sl.Wood, P(2.1f, 2.7f, 0.85f), 0.1f, 3, 6);
        b.Beam(Sl.WoodLight, P(0f, 2.55f, 0.05f), P(0f, 1.55f, 0.05f), 0.025f, 0.025f);
        b.Cyl(Sl.Wood, P(0f, 1.0f, 0.05f), 0.24f, 0.42f, 9, 0.3f, true);
        b.Cyl(Sl.Metal, P(0f, 1.06f, 0.05f), 0.265f, 0.04f, 9, 0.265f, true);
        b.Cyl(Sl.Metal, P(0f, 1.34f, 0.05f), 0.305f, 0.04f, 9, 0.305f, true);
        b.Beam(Sl.Metal, P(-0.3f, 1.42f, 0.05f), P(0.3f, 1.42f, 0.05f), 0.04f, 0.04f);
        // lentera gantung, pot bunga, tanaman rambat
        AnimeKit.HangLantern(b, P(0f, 3.55f, 1.0f), 0.0f + 0.3f);
        b.Cyl(Sl.WoodLight, P(-1.0f, 1.25f, 1.4f), 0.2f, 0.3f, 8, 0.16f, true);
        b.Ball(Sl.Hedge, P(-1.0f, 1.58f, 1.4f), 0.24f, 3, 6, 0.7f);
        b.Ball(Sl.Blossom, P(-0.95f, 1.7f, 1.45f), 0.09f, 3, 5);
        b.Push(P(1.55f, 0f, 0.1f), 0f);
        AnimeKit.Ivy(b, P(0f, 0.6f, 0f), 2.6f, 0.4f, 4);
        b.Pop();
        AnimeKit.FlowerBush(b, P(-2.5f, 0f, -1.5f), 0.6f, Sl.Blossom);
        b.Pop();
    }

    // ------------------------------------------------------------------ lapak pasar
    public static void Stall(GeoBag b, Vector3 c, float yaw, int clothA, int clothB)
    {
        b.Push(c, yaw);
        // meja dengan panel papan
        b.BoxB(Sl.Wood, P(0f, 0.1f, 0f), P(3.4f, 0.95f, 1.5f));
        for (int i = 0; i < 8; i++)
            b.Box(Sl.WoodLight, P(-1.5f + i * 0.43f, 0.58f, 0.76f), P(0.38f, 0.8f, 0.04f));
        b.Box(Sl.WoodLight, P(0f, 1.1f, 0f), P(3.7f, 0.1f, 1.8f));
        b.Box(Sl.Wood, P(0f, 1.0f, 0.92f), P(3.7f, 0.12f, 0.1f));
        // tiang bulat dengan bola penutup
        foreach (float x in new[] { -1.75f, 1.75f })
            foreach (float z in new[] { -0.8f, 0.9f })
            {
                float top = z < 0f ? 3.3f : 2.85f;
                b.Cyl(Sl.Wood, P(x, 0f, z), 0.08f, top, 7, 0.065f, true);
                b.Ball(Sl.Gold, P(x, top + 0.05f, z), 0.1f, 3, 6);
            }
        // kanopi bergaris, melengkung turun di tengah, dengan jumbai bergerigi di depan
        int ns = 8;
        for (int i = 0; i < ns; i++)
        {
            float x0 = -2.0f + i * 4.0f / ns, x1 = x0 + 4.0f / ns;
            int sl = i % 2 == 0 ? clothA : clothB;
            float zb = -0.9f, zf = 1.15f;
            Vector3 b0 = P(x0, 3.3f, zb), b1 = P(x1, 3.3f, zb);
            Vector3 m0 = P(x0, 3.12f, 0.1f), m1 = P(x1, 3.12f, 0.1f);
            Vector3 f0 = P(x0, 2.78f, zf), f1 = P(x1, 2.78f, zf);
            b.QuadBoth(sl, b0, b1, m1, m0);
            b.QuadBoth(sl, m0, m1, f1, f0);
            Vector3 mid = P((x0 + x1) / 2f, 2.42f, zf + 0.08f);
            b.Tri(sl, f0, f1, mid); b.Tri(sl, f1, f0, mid);
        }
        b.Beam(Sl.Wood, P(-1.8f, 2.8f, 1.0f), P(1.8f, 2.8f, 1.0f), 0.07f, 0.07f);
        // dagangan: peti buah, keranjang, karung
        for (int i = 0; i < 3; i++)
        {
            float x = -1.3f + i * 0.9f;
            b.Box(Sl.Wood, P(x, 1.3f, 0.35f), P(0.7f, 0.25f, 0.55f));
            for (int k = 0; k < 6; k++)
                b.Ball((i + k) % 3 == 0 ? Sl.Cloth : ((i + k) % 3 == 1 ? Sl.Gold : Sl.Leaf), P(x - 0.22f + (k % 3) * 0.22f, 1.5f + (k / 3) * 0.14f, 0.25f + (k % 2) * 0.2f), 0.14f, 3, 6);
        }
        b.Cyl(Sl.WoodLight, P(1.35f, 1.15f, -0.2f), 0.4f, 0.4f, 10, 0.33f, false);
        b.Beam(Sl.WoodLight, P(1.0f, 1.55f, -0.2f), P(1.7f, 1.55f, -0.2f), 0.04f, 0.04f);
        for (int k = 0; k < 4; k++) b.Ball(Sl.Cloth, P(1.25f + (k % 2) * 0.2f, 1.6f, -0.3f + (k / 2) * 0.2f), 0.13f, 3, 6);
        b.Ball(Sl.PlasterB, P(-2.35f, 0.35f, 0.4f), 0.4f, 4, 7, 0.9f);
        b.Ball(Sl.Cloth, P(-2.35f, 0.7f, 0.4f), 0.07f, 3, 5);
        b.Ball(Sl.PlasterB, P(-2.2f, 0.3f, -0.15f), 0.34f, 4, 7, 0.9f);
        // bawang/rempah menggantung & lentera
        for (int i = 0; i < 5; i++)
            FantasyKit.HerbBundle(b, P(-1.3f + i * 0.65f, 2.8f, 0.9f), i);
        AnimeKit.HangLantern(b, P(0f, 3.0f, -0.1f), 0.0f + 0.3f);
        // papan nama kecil
        b.Box(Sl.WoodLight, P(0f, 1.9f, -0.85f), P(1.4f, 0.5f, 0.06f));
        b.Box(Sl.Gold, P(0f, 2.18f, -0.85f), P(1.5f, 0.05f, 0.08f));
        BuildKit.Crate(b, P(2.3f, 0f, 0.3f), 0.8f, 20f);
        BuildKit.Crate(b, P(2.3f, 0.8f, 0.3f), 0.55f, -10f);
        BuildKit.Barrel(b, P(-2.35f, 0f, -0.55f));
        b.Pop();
    }

    // ------------------------------------------------------------------ patung ksatria
    public static void KnightStatue(GeoBag b, Vector3 baseC, float yaw)
    {
        b.Push(baseC, yaw);
        b.BoxB(Sl.DarkStone, Vector3.zero, P(3.0f, 0.3f, 3.0f));
        b.BoxB(Sl.Marble, P(0f, 0.3f, 0f), P(2.6f, 0.35f, 2.6f));
        b.BoxB(Sl.Marble, P(0f, 0.65f, 0f), P(1.9f, 1.5f, 1.9f));
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                b.Cyl(Sl.Marble, P(sx * 0.95f, 0.65f, sz * 0.95f), 0.17f, 1.5f, 8, 0.15f, true);
        b.Box(Sl.DarkStone, P(0f, 1.4f, 0.97f), P(1.1f, 0.75f, 0.06f));
        b.Box(Sl.Gold, P(0f, 1.4f, 1.0f), P(0.85f, 0.5f, 0.04f));
        b.BoxB(Sl.DarkStone, P(0f, 2.15f, 0f), P(2.3f, 0.2f, 2.3f));
        b.BoxB(Sl.Marble, P(0f, 2.35f, 0f), P(2.0f, 0.18f, 2.0f));
        float y = 2.53f;
        // sepatu & kaki
        foreach (float sx in new[] { -1f, 1f })
        {
            b.Box(Sl.Marble, P(sx * 0.27f, y + 0.12f, 0.12f), P(0.26f, 0.24f, 0.6f));
            b.Cyl(Sl.Marble, P(sx * 0.27f, y + 0.2f, 0f), 0.15f, 0.85f, 8, 0.12f, true);
            b.Ball(Sl.Marble, P(sx * 0.27f, y + 1.05f, 0.05f), 0.14f, 3, 6);
            b.Cyl(Sl.Marble, P(sx * 0.27f, y + 1.05f, 0f), 0.17f, 0.55f, 8, 0.19f, true);
        }
        // rok baju zirah, pinggang, dada
        b.Cyl(Sl.Marble, P(0f, y + 1.35f, 0f), 0.62f, 0.48f, 14, 0.43f, true);
        b.Cyl(Sl.Gold, P(0f, y + 1.8f, 0f), 0.44f, 0.1f, 14, 0.44f, true);                         // sabuk
        b.Cyl(Sl.Marble, P(0f, y + 1.88f, 0f), 0.42f, 0.78f, 14, 0.5f, true);                      // dada melebar
        b.Box(Sl.Marble, P(0f, y + 2.35f, 0.4f), P(0.14f, 0.52f, 0.1f));
        b.Ball(Sl.Gold, P(0f, y + 2.5f, 0.47f), 0.1f, 3, 6);
        // bahu dan lengan memegang pedang
        foreach (float sx in new[] { -1f, 1f })
        {
            b.Ball(Sl.Marble, P(sx * 0.58f, y + 2.5f, 0f), 0.26f, 4, 7, 0.8f);
            b.Ball(Sl.Gold, P(sx * 0.6f, y + 2.58f, 0f), 0.09f, 3, 5);
            b.Beam(Sl.Marble, P(sx * 0.6f, y + 2.4f, 0.0f), P(sx * 0.38f, y + 1.95f, 0.38f), 0.17f, 0.17f);
            b.Beam(Sl.Marble, P(sx * 0.38f, y + 1.95f, 0.38f), P(sx * 0.08f, y + 2.0f, 0.62f), 0.14f, 0.14f);
        }
        // kepala berhelm + jambul
        b.Cyl(Sl.Marble, P(0f, y + 2.62f, 0f), 0.18f, 0.2f, 8, 0.15f, true);
        b.Ball(Sl.Marble, P(0f, y + 2.98f, 0f), 0.3f, 5, 9, 1.05f);
        b.Box(Sl.DarkStone, P(0f, y + 2.95f, 0.27f), P(0.34f, 0.07f, 0.08f));
        b.Box(Sl.DarkStone, P(0f, y + 2.85f, 0.27f), P(0.07f, 0.25f, 0.08f));
        b.Beam(Sl.Gold, P(0f, y + 3.24f, 0.15f), P(0f, y + 3.28f, -0.25f), 0.1f, 0.2f);
        for (int i = 0; i < 4; i++)
            b.Ball(Sl.Gold, P(0f, y + 3.2f - i * 0.12f, -0.3f - i * 0.12f), 0.12f - i * 0.015f, 3, 5);
        // pedang bertumpu
        b.Beam(Sl.Marble, P(0f, y, 0.62f), P(0f, y + 2.1f, 0.62f), 0.15f, 0.05f);
        b.Box(Sl.Gold, P(0f, y + 2.05f, 0.62f), P(0.72f, 0.1f, 0.12f));
        b.Beam(Sl.Gold, P(0f, y + 2.1f, 0.62f), P(0f, y + 2.45f, 0.62f), 0.07f, 0.07f);
        b.Ball(Sl.Gold, P(0f, y + 2.5f, 0.62f), 0.1f, 3, 6);
        // jubah
        float cz = -0.35f;
        for (int i = 0; i < 4; i++)
        {
            float x0 = -0.55f + i * 0.275f, x1 = x0 + 0.275f;
            float fold = (i % 2 == 0 ? 0f : -0.1f);
            b.QuadBoth(Sl.Cloth, P(x0, y + 0.35f, cz - 0.3f + fold), P(x1, y + 0.35f, cz - 0.3f - fold), P(x1 * 0.7f, y + 2.5f, cz + 0.15f), P(x0 * 0.7f, y + 2.5f, cz + 0.15f));
        }
        // perisai layang bersandar
        b.Push(P(-0.72f, y, 0.35f), 20f);
        Vector3 sTop = P(0f, 1.55f, 0f);
        b.QuadBoth(Sl.Gold, P(-0.34f, 1.55f, 0f), P(0.34f, 1.55f, 0f), P(0.34f, 0.85f, 0.1f), P(-0.34f, 0.85f, 0.1f));
        b.Tri(Sl.Gold, P(-0.34f, 0.85f, 0.1f), P(0.34f, 0.85f, 0.1f), P(0f, 0.15f, 0.2f));
        b.Tri(Sl.Gold, P(0.34f, 0.85f, 0.1f), P(-0.34f, 0.85f, 0.1f), P(0f, 0.15f, 0.2f));
        b.Box(Sl.Cloth, P(0f, 1.1f, 0.04f), P(0.34f, 0.8f, 0.04f));
        b.Box(Sl.Gold, P(0f, 1.15f, 0.07f), P(0.1f, 0.6f, 0.03f));
        b.Box(Sl.Gold, P(0f, 1.25f, 0.07f), P(0.4f, 0.1f, 0.03f));
        b.Pop();
        b.Pop();
    }

    // ------------------------------------------------------------------ pagar kayu
    public static void Fence(GeoBag b, IList<Vector3> pts, float h)
    {
        for (int i = 0; i + 1 < pts.Count; i++)
        {
            Vector3 a = pts[i], c = pts[i + 1];
            float len = Vector3.Distance(a, c);
            int n = Mathf.Max(1, Mathf.RoundToInt(len / 2.2f));
            for (int k = 0; k <= n; k++)
            {
                Vector3 p = Vector3.Lerp(a, c, k / (float)n) + P(0f, 0f, 0f);
                b.Cyl(Sl.Wood, p, 0.1f, h, 6, 0.085f, true);
                b.Ball(Sl.WoodLight, p + P(0f, h + 0.04f, 0f), 0.12f, 3, 6, 0.9f);
                if (k < n)
                {
                    Vector3 q = Vector3.Lerp(a, c, (k + 0.5f) / n);
                    b.Beam(Sl.WoodLight, p + P(0f, h * 0.84f, 0f), Vector3.Lerp(a, c, (k + 1f) / n) + P(0f, h * 0.84f + 0.02f, 0f), 0.1f, 0.08f);
                    b.Beam(Sl.WoodLight, p + P(0f, h * 0.46f, 0f), Vector3.Lerp(a, c, (k + 1f) / n) + P(0f, h * 0.46f - 0.02f, 0f), 0.1f, 0.08f);
                    b.Ball(Sl.Wood, q + P(0f, h * 0.84f + 0.02f, 0.05f), 0.055f, 3, 5);
                    if ((k + i) % 3 == 0) TreeKit.GrassTuft(b, Sl.Leaf, q + P(0.1f, 0f, 0.1f), 0.4f, k + i);
                }
            }
        }
    }
}
