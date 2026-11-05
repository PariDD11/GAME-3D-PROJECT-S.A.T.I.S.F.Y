using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Perabot jalan Kota Ashwatch (geometri murni, bisa dipratinjau): lampu besi, air mancur bundaran + air mancur alun-alun tengah, lapak pasar bergaris,
/// papan pengumuman, pot bunga batu, tiang spanduk, plinth patung, tumpukan barang, lumbung padi. Semua titik asal di tanah, muka depan = +Z.
/// </summary>
public static partial class T2Kit
{
    static Vector3 Pv(float x, float y, float z) { return new Vector3(x, y, z); }

    // ------------------------------------------------------------------ patung penjaga gerbang dalam (kanon: patung batu bermata kosong)
    /// <summary>Sosok berjubah dari batu poles pucat bertumpu pada pedang lebar; wajah polos dengan dua rongga mata kosong (hitam). Tinggi ~6,2 m. Slot: Marble, Stone, DarkStone, Dark (rongga).</summary>
    public static void HollowStatue(GeoBag b, Vector3 baseC, float yaw)
    {
        b.Push(baseC, yaw);
        // alas bertingkat dengan tiga baris sisik terukir di mukanya
        b.BoxB(Sl.DarkStone, Pv(0f, 0f, 0f), Pv(3.2f, 0.35f, 3.2f));
        b.BoxB(Sl.Stone, Pv(0f, 0.35f, 0f), Pv(2.7f, 0.3f, 2.7f));
        b.BoxB(Sl.Stone, Pv(0f, 0.65f, 0f), Pv(2.1f, 1.85f, 2.1f));
        b.BoxB(Sl.DarkStone, Pv(0f, 2.5f, 0f), Pv(2.5f, 0.22f, 2.5f));
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 4; c++)
            {
                float x = -0.75f + c * 0.5f + (r % 2) * 0.25f, y = 1.1f + r * 0.4f;
                if (x > 0.8f) continue;
                b.Quad(Sl.DarkStone, Pv(x - 0.2f, y, 1.06f), Pv(x, y - 0.24f, 1.06f), Pv(x + 0.2f, y, 1.06f), Pv(x, y + 0.24f, 1.06f));
            }
        float y0 = 2.72f;
        // jubah panjang melebar ke bawah, bahu bidang, tudung
        b.Cyl(Sl.Marble, Pv(0f, y0, 0f), 0.8f, 2.25f, 12, 0.44f, true);
        for (int i = 0; i < 5; i++)
        {
            float a = i * Mathf.PI * 2f / 5f + 0.3f;
            b.Beam(Sl.Stone, Pv(Mathf.Sin(a) * 0.42f, y0 + 1.9f, Mathf.Cos(a) * 0.42f), Pv(Mathf.Sin(a) * 0.78f, y0 + 0.05f, Mathf.Cos(a) * 0.78f), 0.1f, 0.06f);
        }
        b.Ball(Sl.Marble, Pv(0f, y0 + 2.25f, 0f), 0.56f, 4, 10, 0.52f);
        foreach (float sx in new[] { -1f, 1f })
        {
            b.Beam(Sl.Marble, Pv(sx * 0.5f, y0 + 2.18f, 0f), Pv(sx * 0.3f, y0 + 1.55f, 0.45f), 0.2f, 0.2f);
            b.Beam(Sl.Marble, Pv(sx * 0.3f, y0 + 1.55f, 0.45f), Pv(0.04f * sx, y0 + 1.6f, 0.66f), 0.16f, 0.16f);
        }
        b.Cyl(Sl.Marble, Pv(0f, y0 + 2.45f, 0f), 0.15f, 0.22f, 8, 0.13f, true);
        b.Ball(Sl.Marble, Pv(0f, y0 + 2.98f, 0f), 0.36f, 6, 10, 1.12f);
        b.Ball(Sl.Stone, Pv(0f, y0 + 3.02f, -0.1f), 0.44f, 5, 10, 1.0f);                         // tudung di belakang wajah
        b.Box(Sl.Marble, Pv(0f, y0 + 2.93f, 0.32f), Pv(0.1f, 0.2f, 0.1f));                        // hidung
        foreach (float sx in new[] { -1f, 1f })
            b.Ball(Sl.Dark, Pv(sx * 0.14f, y0 + 3.05f, 0.31f), 0.085f, 3, 6, 1.7f);              // rongga mata kosong
        // pedang lebar bertumpu di depan, tangan di gagang
        b.Beam(Sl.Marble, Pv(0f, y0 + 0.05f, 0.7f), Pv(0f, y0 + 2.05f, 0.7f), 0.2f, 0.07f);
        b.Box(Sl.Stone, Pv(0f, y0 + 2.0f, 0.7f), Pv(0.8f, 0.12f, 0.14f));
        b.Beam(Sl.Stone, Pv(0f, y0 + 2.05f, 0.7f), Pv(0f, y0 + 2.4f, 0.7f), 0.08f, 0.08f);
        b.Pop();
    }

    // ------------------------------------------------------------------ lampu jalan: tiang besi di alas batu, lengan melengkung ke +X, lentera segi empat berkaca
    public static void Lamp(GeoBag b)
    {
        b.Box(Sl.DarkStone, Pv(0f, 0.2f, 0f), Pv(0.66f, 0.4f, 0.66f));
        b.Box(Sl.Stone, Pv(0f, 0.5f, 0f), Pv(0.46f, 0.2f, 0.46f));
        b.Cyl(Sl.Metal, Pv(0f, 0.6f, 0f), 0.12f, 3.4f, 8, 0.075f, true);
        b.Cyl(Sl.Metal, Pv(0f, 1.1f, 0f), 0.17f, 0.08f, 8, 0.17f, true);
        b.Cyl(Sl.Metal, Pv(0f, 2.5f, 0f), 0.14f, 0.07f, 8, 0.14f, true);
        b.Ball(Sl.Metal, Pv(0f, 4.05f, 0f), 0.13f, 3, 6);
        // lengan melengkung (busur seperempat)
        int n = 5;
        for (int k = 0; k < n; k++)
        {
            float t0 = k / (float)n, t1 = (k + 1) / (float)n;
            Vector3 a = Pv(0.85f * Mathf.Sin(t0 * 1.35f), 3.35f + 0.55f * (1f - Mathf.Cos(t0 * 1.35f)), 0f);
            Vector3 c = Pv(0.85f * Mathf.Sin(t1 * 1.35f), 3.35f + 0.55f * (1f - Mathf.Cos(t1 * 1.35f)), 0f);
            b.Beam(Sl.Metal, a, c, 0.07f, 0.07f, Vector3.forward);
        }
        b.Beam(Sl.Metal, Pv(0.05f, 3.0f, 0f), Pv(0.6f, 3.55f, 0f), 0.05f, 0.05f, Vector3.forward);
        Vector3 lp = Pv(0.85f, 3.28f, 0f);
        b.Box(Sl.Metal, lp + Pv(0f, -0.38f, 0f), Pv(0.34f, 0.07f, 0.34f));
        b.Box(Sl.Glow, lp + Pv(0f, -0.14f, 0f), Pv(0.24f, 0.42f, 0.24f));
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                b.Beam(Sl.Metal, lp + Pv(sx * 0.15f, -0.36f, sz * 0.15f), lp + Pv(sx * 0.12f, 0.1f, sz * 0.12f), 0.03f, 0.03f);
        b.Cone(Sl.Metal, lp + Pv(0f, 0.09f, 0f), 0.26f, 0.3f, 4);
        b.Ball(Sl.Gold, lp + Pv(0f, 0.42f, 0f), 0.05f, 3, 5);
    }

    // ------------------------------------------------------------------ air mancur bundaran utama (alun-alun gerbang)
    public static void Fountain(GeoBag b)
    {
        const int N = 20;
        float R = 7.4f, Ri = 6.4f, H = 0.95f;
        b.Cyl(Sl.Stone, Vector3.zero, R + 0.25f, 0.22f, N, R + 0.15f, true);
        b.Cyl(Sl.Marble, Pv(0f, 0.2f, 0f), R, H - 0.2f, N, R - 0.1f, false);
        b.Annulus(Sl.Marble, Vector3.zero, Ri, R + 0.05f, 0f, 360f, N, H);
        for (int k = 0; k < N; k++)                       // dinding dalam bak (dua sisi)
        {
            float a0 = k * Mathf.PI * 2f / N, a1 = (k + 1) * Mathf.PI * 2f / N;
            Vector3 p0 = Pv(Mathf.Sin(a0) * Ri, 0.2f, Mathf.Cos(a0) * Ri), p1 = Pv(Mathf.Sin(a1) * Ri, 0.2f, Mathf.Cos(a1) * Ri);
            b.QuadBoth(Sl.Stone, p0, p1, p1 + Pv(0f, H - 0.2f, 0f), p0 + Pv(0f, H - 0.2f, 0f));
        }
        b.Disc(Sl.Water, Vector3.zero, Ri, N, 0.74f);
        // tonjolan hias di bibir bak: delapan pilar pendek berbola emas
        for (int k = 0; k < 8; k++)
        {
            float a = k * Mathf.PI * 2f / 8f + 0.39f;
            Vector3 p = Pv(Mathf.Sin(a) * (R + 0.05f), 0f, Mathf.Cos(a) * (R + 0.05f));
            b.Cyl(Sl.Marble, p + Pv(0f, 0.9f, 0f), 0.34f, 0.5f, 8, 0.28f, true);
            b.Ball(Sl.Gold, p + Pv(0f, 1.58f, 0f), 0.2f, 3, 6);
        }
        // tiang tengah bertingkat
        b.Cyl(Sl.Marble, Pv(0f, 0.2f, 0f), 2.3f, 0.9f, 12, 2.0f, true);
        b.Cyl(Sl.Marble, Pv(0f, 1.1f, 0f), 1.2f, 1.5f, 12, 0.85f, true);
        b.Cyl(Sl.Marble, Pv(0f, 2.55f, 0f), 0.9f, 0.5f, 14, 3.1f, false);                // mangkuk bawah
        b.Annulus(Sl.Marble, Pv(0f, 0f, 0f), 2.7f, 3.15f, 0f, 360f, 14, 3.05f);
        b.Disc(Sl.Water, Vector3.zero, 2.75f, 14, 2.9f);
        b.Cyl(Sl.Marble, Pv(0f, 2.9f, 0f), 0.55f, 1.9f, 10, 0.4f, true);
        b.Cyl(Sl.Marble, Pv(0f, 4.6f, 0f), 0.4f, 0.4f, 12, 1.6f, false);                 // mangkuk atas
        b.Annulus(Sl.Marble, Vector3.zero, 1.3f, 1.65f, 0f, 360f, 12, 5.0f);
        b.Disc(Sl.Water, Vector3.zero, 1.32f, 12, 4.88f);
        b.Cyl(Sl.Marble, Pv(0f, 4.9f, 0f), 0.3f, 0.9f, 8, 0.2f, true);
        PropKit.KnightStatue(b, Pv(0f, 5.65f, 0f), 0f);
        // pancuran air: busur tipis dari mangkuk atas ke mangkuk bawah dan dari mangkuk bawah ke bak
        for (int k = 0; k < 8; k++)
        {
            float a = k * Mathf.PI * 2f / 8f;
            float sx = Mathf.Sin(a), cz = Mathf.Cos(a);
            b.Beam(Sl.Water, Pv(sx * 1.55f, 4.95f, cz * 1.55f), Pv(sx * 1.95f, 3.5f, cz * 1.95f), 0.1f, 0.1f);
            b.Beam(Sl.Water, Pv(sx * 1.95f, 3.5f, cz * 1.95f), Pv(sx * 2.55f, 2.95f, cz * 2.55f), 0.1f, 0.1f);
        }
        for (int k = 0; k < 8; k++)
        {
            float a = k * Mathf.PI * 2f / 8f + 0.39f;
            float sx = Mathf.Sin(a), cz = Mathf.Cos(a);
            b.Beam(Sl.Water, Pv(sx * 3.1f, 2.85f, cz * 3.1f), Pv(sx * 3.5f, 1.9f, cz * 3.5f), 0.1f, 0.1f);
            b.Beam(Sl.Water, Pv(sx * 3.5f, 1.9f, cz * 3.5f), Pv(sx * 3.9f, 0.8f, cz * 3.9f), 0.1f, 0.1f);
        }
    }

    // ------------------------------------------------------------------ air mancur kecil alun-alun tengah (kota atas): kolam bulat + plinth patung
    public static void PlazaFountain(GeoBag b)
    {
        const int N = 16;
        float R = 4.6f, Ri = 3.8f;
        b.Cyl(Sl.Stone, Vector3.zero, R + 0.2f, 0.18f, N, R + 0.12f, true);
        b.Cyl(Sl.Marble, Pv(0f, 0.18f, 0f), R, 0.62f, N, R - 0.06f, false);
        b.Annulus(Sl.Marble, Vector3.zero, Ri, R + 0.04f, 0f, 360f, N, 0.8f);
        for (int k = 0; k < N; k++)
        {
            float a0 = k * Mathf.PI * 2f / N, a1 = (k + 1) * Mathf.PI * 2f / N;
            Vector3 p0 = Pv(Mathf.Sin(a0) * Ri, 0.18f, Mathf.Cos(a0) * Ri), p1 = Pv(Mathf.Sin(a1) * Ri, 0.18f, Mathf.Cos(a1) * Ri);
            b.QuadBoth(Sl.Stone, p0, p1, p1 + Pv(0f, 0.62f, 0f), p0 + Pv(0f, 0.62f, 0f));
        }
        b.Disc(Sl.Water, Vector3.zero, Ri, N, 0.6f);
        b.Cyl(Sl.Marble, Pv(0f, 0.18f, 0f), 1.3f, 0.7f, 10, 1.1f, true);
        b.Cyl(Sl.Stone, Pv(0f, 0.88f, 0f), 0.85f, 1.9f, 10, 0.7f, true);
        b.Cyl(Sl.Marble, Pv(0f, 2.78f, 0f), 0.78f, 0.18f, 10, 0.78f, true);
        PropKit.KnightStatue(b, Pv(0f, 2.96f, 0f), 0f);
        for (int k = 0; k < 4; k++)
        {
            float a = k * Mathf.PI * 0.5f + 0.785f;
            Vector3 p = Pv(Mathf.Sin(a) * 1.15f, 1.55f, Mathf.Cos(a) * 1.15f);
            b.Box(Sl.Gold, p, Pv(0.2f, 0.2f, 0.2f), a * Mathf.Rad2Deg);
            b.Beam(Sl.Water, p, Pv(Mathf.Sin(a) * 2.3f, 0.7f, Mathf.Cos(a) * 2.3f), 0.09f, 0.09f);
        }
    }

    // ------------------------------------------------------------------ lapak pasar bergaris (lebar 3.6, dalam 2.4); v = variasi barang
    public static void Stall(GeoBag b, int v)
    {
        int sA = (v % 2 == 0) ? Sl.Cloth : Sl.ClothB, sB = Sl.PlasterB;
        // tiang
        foreach (float sx in new[] { -1.75f, 1.75f })
        {
            b.Box(Sl.Wood, Pv(sx, 1.4f, -1.0f), Pv(0.14f, 2.8f, 0.14f));
            b.Box(Sl.Wood, Pv(sx, 1.1f, 1.05f), Pv(0.14f, 2.2f, 0.14f));
        }
        // meja
        b.Box(Sl.WoodLight, Pv(0f, 0.5f, 0.55f), Pv(3.3f, 1.0f, 1.0f));
        b.Box(Sl.Wood, Pv(0f, 1.04f, 0.55f), Pv(3.5f, 0.09f, 1.2f));
        b.Box(Sl.Wood, Pv(0f, 1.55f, -0.88f), Pv(3.2f, 0.07f, 0.42f));
        // kanopi bergaris miring ke depan
        int ns = 6;
        float cw = 3.9f / ns;
        for (int k = 0; k < ns; k++)
        {
            float x0 = -1.95f + k * cw, x1 = x0 + cw;
            Vector3 a = Pv(x0, 2.85f, -1.15f), c = Pv(x1, 2.85f, -1.15f), d = Pv(x1, 2.3f, 1.35f), e = Pv(x0, 2.3f, 1.35f);
            b.QuadBoth(k % 2 == 0 ? sA : sB, a, c, d, e);
            // rumbai bergigi di tepi depan
            b.Tri(k % 2 == 0 ? sA : sB, e, d, Pv((x0 + x1) * 0.5f, 1.95f, 1.4f));
        }
        b.Box(Sl.Wood, Pv(0f, 2.86f, -1.15f), Pv(3.9f, 0.1f, 0.1f));
        // barang
        int style = v % 4;
        for (int k = 0; k < 4; k++)
        {
            float x = -1.25f + k * 0.85f;
            b.Box(Sl.Wood, Pv(x, 1.2f, 0.5f), Pv(0.62f, 0.2f, 0.62f), k * 12f);
            if (style == 0) b.Ball(k % 2 == 0 ? Sl.Blossom : Sl.Leaf, Pv(x, 1.4f, 0.5f), 0.27f, 3, 7, 0.7f);                // buah/sayur
            else if (style == 1) b.Cyl(Sl.WoodLight, Pv(x, 1.3f, 0.5f), 0.2f, 0.34f, 8, 0.2f, true);                         // toples/roti
            else if (style == 2) b.Box(k % 2 == 0 ? Sl.Metal : Sl.Cloth, Pv(x, 1.38f, 0.5f), Pv(0.48f, 0.12f, 0.4f), k * 20f);   // perkakas / kain
            else b.Cone(Sl.Cloth, Pv(x, 1.3f, 0.5f), 0.26f, 0.5f, 6);
        }
        // rak belakang berisi toples
        for (int k = 0; k < 5; k++) b.Cyl(Sl.Wood, Pv(-1.3f + k * 0.65f, 1.59f, -0.88f), 0.13f, 0.32f, 6, 0.1f, true);
        b.Cyl(Sl.Wood, Pv(2.3f, 0f, -0.5f), 0.34f, 0.8f, 8, 0.3f, true);        // tong di samping
    }

    // ------------------------------------------------------------------ papan pengumuman beratap (lebar 4.4)
    public static void NoticeBoard(GeoBag b)
    {
        b.Box(Sl.Wood, Pv(-1.9f, 1.7f, 0f), Pv(0.3f, 3.4f, 0.3f));
        b.Box(Sl.Wood, Pv(1.9f, 1.7f, 0f), Pv(0.3f, 3.4f, 0.3f));
        b.Box(Sl.WoodLight, Pv(0f, 2.15f, 0f), Pv(4.0f, 2.1f, 0.22f));
        b.Box(Sl.Wood, Pv(0f, 3.25f, 0f), Pv(4.2f, 0.14f, 0.4f));
        b.Box(Sl.Wood, Pv(0f, 1.08f, 0f), Pv(4.0f, 0.12f, 0.38f));
        float[] px = { -1.4f, -0.45f, 0.5f, 1.4f };
        float[] py = { 2.5f, 2.0f, 2.45f, 1.85f };
        for (int k = 0; k < 4; k++)
        {
            b.Box(Sl.PlasterB, Pv(px[k], py[k], 0.13f), Pv(0.72f, 0.95f, 0.03f), (k - 1.5f) * 4f);
            b.Box(Sl.Metal, Pv(px[k], py[k] + 0.4f, 0.16f), Pv(0.07f, 0.07f, 0.03f));
        }
        TileRoof(b, Sl.Roof, Sl.DarkStone, Sl.Wood, Sl.Wood, Pv(0f, 3.32f, 0f), 4.6f, 1.7f, 0.75f, 0.2f, 0.2f, 3, 0.42f, true);
        b.Box(Sl.DarkStone, Pv(0f, 0.12f, 0f), Pv(4.6f, 0.24f, 0.9f));
    }

    // ------------------------------------------------------------------ pot bunga batu memanjang berisi semak berbunga
    public static void Planter(GeoBag b, int v)
    {
        b.Box(Sl.Stone, Pv(0f, 0.38f, 0f), Pv(2.4f, 0.76f, 0.95f));
        b.Box(Sl.DarkStone, Pv(0f, 0.8f, 0f), Pv(2.55f, 0.1f, 1.08f));
        b.Box(Sl.Dirt, Pv(0f, 0.78f, 0f), Pv(2.2f, 0.06f, 0.75f));
        for (int k = 0; k < 4; k++)
        {
            float x = -0.85f + k * 0.57f;
            float r = 0.34f + 0.06f * Rnd(v + 3, k);
            b.Dome(Sl.Hedge, Pv(x, 0.82f, 0f), r, 3, 8, 0.85f);
            for (int f = 0; f < 3; f++)
                b.Ball(f % 2 == 0 ? Sl.Blossom : Sl.Gold, Pv(x + (Rnd(v, k * 3 + f) - 0.5f) * 0.5f, 1.05f + Rnd(v + 1, k * 3 + f) * 0.2f, (Rnd(v + 2, k * 3 + f) - 0.5f) * 0.3f), 0.07f, 2, 5);
        }
    }

    // ------------------------------------------------------------------ tiang spanduk (tinggi 6.6): lengan silang + dua spanduk
    public static void BannerPole(GeoBag b, int slotCloth)
    {
        b.Box(Sl.DarkStone, Pv(0f, 0.22f, 0f), Pv(0.8f, 0.44f, 0.8f));
        b.Cyl(Sl.Wood, Pv(0f, 0.4f, 0f), 0.12f, 6.4f, 8, 0.08f, true);
        b.Beam(Sl.Wood, Pv(-1.1f, 6.1f, 0f), Pv(1.1f, 6.1f, 0f), 0.09f, 0.09f);
        b.Ball(Sl.Gold, Pv(0f, 6.9f, 0f), 0.16f, 3, 6);
        Banner(b, Pv(-0.62f, 6.05f, 0f), 1.0f, 3.2f, slotCloth, Sl.Gold);
        Banner(b, Pv(0.62f, 6.05f, 0f), 1.0f, 3.2f, slotCloth, Sl.Gold);
    }

    // ------------------------------------------------------------------ tumpukan barang pasar (peti, tong, karung)
    public static void Goods(GeoBag b, int v)
    {
        V2Kit.Crate(b, Pv(0f, 0f, 0f), 0.95f, 8f);
        V2Kit.Crate(b, Pv(1.0f, 0f, 0.1f), 0.85f, -10f);
        V2Kit.Crate(b, Pv(0.45f, 0.82f, 0.05f), 0.8f, 20f);
        V2Kit.Barrel(b, Pv(-1.0f, 0f, 0.2f));
        if (v % 2 == 0) V2Kit.Barrel(b, Pv(-1.65f, 0f, -0.3f));
        V2Kit.Sack(b, Pv(1.8f, 0f, -0.4f), 0.9f);
        V2Kit.Sack(b, Pv(1.55f, 0f, 0.7f), 0.8f);
    }

    // ------------------------------------------------------------------ plinth patung (patung ksatria di atasnya)
    public static void StatuePlinth(GeoBag b, float scaleUp)
    {
        b.Box(Sl.DarkStone, Pv(0f, 0.2f, 0f), Pv(2.2f, 0.4f, 2.2f));
        b.Box(Sl.Marble, Pv(0f, 0.8f, 0f), Pv(1.7f, 0.8f, 1.7f));
        b.Box(Sl.DarkStone, Pv(0f, 1.3f, 0f), Pv(1.95f, 0.2f, 1.95f));
        PropKit.KnightStatue(b, Pv(0f, 1.4f, 0f), 0f);
    }

    // ------------------------------------------------------------------ lumbung padi: bilik kayu di atas tiang batu + atap genteng (6 x 6)
    public static float Granary(GeoBag b)
    {
        foreach (float sx in new[] { -2.3f, 0f, 2.3f })
            foreach (float sz in new[] { -2.3f, 0f, 2.3f })
            {
                b.Cyl(Sl.Stone, Pv(sx, 0f, sz), 0.34f, 0.9f, 8, 0.24f, false);
                b.Cyl(Sl.DarkStone, Pv(sx, 0.9f, sz), 0.4f, 0.16f, 8, 0.4f, true);
            }
        b.Box(Sl.Wood, Pv(0f, 1.15f, 0f), Pv(5.6f, 0.2f, 5.6f));
        for (int k = 0; k < 8; k++)
        {
            float x = -2.45f + k * 0.7f;
            b.Box(k % 2 == 0 ? Sl.WoodLight : Sl.Wood, Pv(x, 2.5f, 2.7f), Pv(0.66f, 2.5f, 0.12f));
            b.Box(k % 2 == 0 ? Sl.WoodLight : Sl.Wood, Pv(x, 2.5f, -2.7f), Pv(0.66f, 2.5f, 0.12f));
            b.Box(k % 2 == 0 ? Sl.WoodLight : Sl.Wood, Pv(2.7f, 2.5f, x), Pv(0.12f, 2.5f, 0.66f));
            b.Box(k % 2 == 0 ? Sl.WoodLight : Sl.Wood, Pv(-2.7f, 2.5f, x), Pv(0.12f, 2.5f, 0.66f));
        }
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                b.Box(Sl.Wood, Pv(sx * 2.75f, 2.5f, sz * 2.75f), Pv(0.2f, 2.6f, 0.2f));
        b.Box(Sl.Wood, Pv(0f, 1.9f, 2.78f), Pv(1.1f, 1.4f, 0.1f));          // pintu
        b.Box(Sl.Metal, Pv(0.35f, 1.85f, 2.85f), Pv(0.1f, 0.1f, 0.05f));
        TileRoof(b, Sl.Roof, Sl.DarkStone, Sl.Wood, Sl.Plaster, Pv(0f, 3.8f, 0f), 6.2f, 6.6f, 1.9f, 0.5f, 0.4f, 7, 0.42f, true);
        // tangga kayu kecil
        b.Stairs(Sl.Wood, Pv(0f, 0f, 2.9f), 1.3f, 5, 0.23f, 0.3f, 0f);
        return 6.0f;
    }
    // ------------------------------------------------------------------ prop lore (kanon bagian 5: garam, tali merah, lonceng terikat, goresan 3+3, papan hitam)
    /// <summary>Sumur tua yang ditutup papan: lima papan melintang + dua papan pengunci + batu pemberat, tali merah melilit pinggang, garis garam di sekeliling. Slot: Stone, DarkStone, Wood, WoodLight, Cloth (merah), Marble (garam).</summary>
    public static void PlankWell(GeoBag b)
    {
        b.Cyl(Sl.Stone, Pv(0f, 0f, 0f), 1.25f, 1.0f, 12, 1.12f, true);
        b.Cyl(Sl.DarkStone, Pv(0f, 0.95f, 0f), 1.32f, 0.12f, 12, 1.32f, true);
        for (int i = 0; i < 5; i++)
            b.Box(Sl.Wood, Pv(-0.84f + i * 0.42f, 1.12f + (i % 2) * 0.015f, 0f), Pv(0.4f, 0.09f, 2.9f), i % 2 == 0 ? 1.8f : -2.4f);
        b.Box(Sl.WoodLight, Pv(0f, 1.23f, 0.62f), Pv(2.3f, 0.08f, 0.3f), 7f);
        b.Box(Sl.WoodLight, Pv(0f, 1.23f, -0.62f), Pv(2.3f, 0.08f, 0.3f), -5f);
        b.Ball(Sl.Stone, Pv(0.05f, 1.46f, 0.02f), 0.36f, 3, 6, 0.75f);
        const int N = 14;
        for (int k = 0; k < N; k++)
        {
            float a0 = k * Mathf.PI * 2f / N, a1 = (k + 1) * Mathf.PI * 2f / N;
            b.Beam(Sl.Cloth, Pv(Mathf.Sin(a0) * 1.2f, 0.62f, Mathf.Cos(a0) * 1.2f), Pv(Mathf.Sin(a1) * 1.2f, 0.62f, Mathf.Cos(a1) * 1.2f), 0.06f, 0.06f);
        }
        b.Ball(Sl.Cloth, Pv(0f, 0.62f, 1.22f), 0.09f, 3, 6);
        b.Beam(Sl.Cloth, Pv(0f, 0.62f, 1.24f), Pv(0.08f, 0.2f, 1.34f), 0.04f, 0.04f);
        b.Beam(Sl.Cloth, Pv(0f, 0.62f, 1.24f), Pv(-0.1f, 0.28f, 1.3f), 0.04f, 0.04f);
        b.Annulus(Sl.Marble, Pv(0f, 0f, 0f), 2.35f, 2.47f, 0f, 360f, 36, 0.035f);              // garam dua jari, tak putus
    }

    /// <summary>Papan hitam larangan (huruf putih ditambahkan sebagai teks oleh pemanggil, muka depan = +Z): dua tiang kayu di alas batu + papan arang bertepi besi.</summary>
    public static void WarnBoard(GeoBag b)
    {
        foreach (float sx in new[] { -1.2f, 1.2f })
        {
            b.Box(Sl.Stone, Pv(sx, 0.12f, 0f), Pv(0.5f, 0.24f, 0.5f));
            b.Cyl(Sl.Wood, Pv(sx, 0.24f, 0f), 0.1f, 2.3f, 6, 0.085f, true);
        }
        b.Box(Sl.Dark, Pv(0f, 1.62f, 0f), Pv(2.7f, 1.05f, 0.09f));
        b.Box(Sl.Metal, Pv(0f, 2.16f, 0f), Pv(2.8f, 0.05f, 0.13f));
        b.Box(Sl.Metal, Pv(0f, 1.08f, 0f), Pv(2.8f, 0.05f, 0.13f));
        foreach (float sx in new[] { -1.37f, 1.37f }) b.Box(Sl.Metal, Pv(sx, 1.62f, 0f), Pv(0.05f, 1.1f, 0.13f));
    }

    /// <summary>Lonceng kuningan di tiang pos: lidahnya diikat tali merah supaya tak pernah berbunyi (kanon: yang berbunyi akan dijawab).</summary>
    public static void TiedBell(GeoBag b)
    {
        b.Box(Sl.Stone, Pv(0f, 0.2f, 0f), Pv(0.7f, 0.4f, 0.7f));
        b.Cyl(Sl.Wood, Pv(0f, 0.4f, 0f), 0.13f, 2.9f, 6, 0.1f, true);
        b.Beam(Sl.Wood, Pv(0f, 3.25f, 0f), Pv(1.0f, 3.25f, 0f), 0.1f, 0.1f);
        b.Beam(Sl.Wood, Pv(0f, 2.8f, 0f), Pv(0.85f, 3.23f, 0f), 0.07f, 0.07f);
        b.Beam(Sl.Metal, Pv(0.95f, 3.23f, 0f), Pv(0.95f, 3.05f, 0f), 0.05f, 0.05f);
        b.Cyl(Sl.Gold, Pv(0.95f, 2.45f, 0f), 0.34f, 0.6f, 10, 0.15f, true);
        b.Cyl(Sl.Gold, Pv(0.95f, 2.4f, 0f), 0.37f, 0.07f, 10, 0.34f, true);
        b.Ball(Sl.Metal, Pv(0.95f, 2.33f, 0f), 0.07f, 3, 6);
        b.Beam(Sl.Cloth, Pv(0.95f, 2.5f, 0f), Pv(0.95f, 2.22f, 0f), 0.06f, 0.06f);               // ikatan di lidah
        b.Beam(Sl.Cloth, Pv(0.95f, 2.24f, 0f), Pv(1.04f, 1.95f, 0.05f), 0.04f, 0.04f);
        b.Beam(Sl.Cloth, Pv(0.95f, 2.24f, 0f), Pv(0.88f, 2.02f, -0.05f), 0.04f, 0.04f);
    }

    /// <summary>Tiang hitung: tiang batu rendah dengan tiga goresan, jarak, tiga goresan lagi (kanon: motif 3+3).</summary>
    public static void TallyPost(GeoBag b)
    {
        b.Box(Sl.DarkStone, Pv(0f, 0.15f, 0f), Pv(0.9f, 0.3f, 0.9f));
        b.Box(Sl.Stone, Pv(0f, 0.9f, 0f), Pv(0.62f, 1.2f, 0.62f));
        b.Box(Sl.DarkStone, Pv(0f, 1.55f, 0f), Pv(0.78f, 0.12f, 0.78f));
        for (int g = 0; g < 6; g++)
        {
            float x = g < 3 ? -0.2f + g * 0.07f : 0.06f + (g - 3) * 0.07f;
            b.Quad(Sl.Dark, Pv(x, 0.85f, 0.315f), Pv(x + 0.012f, 0.85f, 0.315f), Pv(x + 0.012f, 1.25f, 0.315f), Pv(x, 1.25f, 0.315f));
        }
    }
}
