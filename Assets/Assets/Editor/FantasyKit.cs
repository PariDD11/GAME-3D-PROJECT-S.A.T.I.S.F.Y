using UnityEngine;

/// <summary>
/// Kit bangunan isekai-anime / fantasi abad pertengahan: massa bertingkat (beberapa volume), pondasi batu bulat, rangka kayu tebal,
/// atap melengkung berujung terangkat, menara bulat beratap terompet, balkon, teras beratap, jendela lengkung menyala,
/// tanaman rambat, pot bunga, lentera. Semua bangunan menghadap +Z lokal; pusat di x=0,z=0.
/// </summary>
public static partial class FantasyKit
{
    static Vector3 P(float x, float y, float z) { return new Vector3(x, y, z); }
    static int Pick(int rnd, int salt, int n) { return n <= 0 ? 0 : ((rnd / (salt * 3 + 1)) + salt * 7) % n; }
    static float R01(int rnd, int salt) { return (((rnd >> (salt % 20)) ^ (salt * 2654435)) & 1023) / 1023f; }

    // ====================================================================== dasar
    /// <summary>Pondasi batu: alas gelap + blok batu + batu bulat menonjol di sudut dan sepanjang muka depan.</summary>
    public static void Foundation(GeoBag b, float cx, float cz, float w, float d, float h, int rnd)
    {
        b.BoxB(Sl.DarkStone, P(cx, -0.6f, cz), new Vector3(w + 0.6f, 0.9f, d + 0.6f));
        b.BoxB(Sl.Stone, P(cx, 0.3f, cz), new Vector3(w + 0.4f, h - 0.3f, d + 0.4f));
        b.BoxB(Sl.DarkStone, P(cx, h - 0.08f, cz), new Vector3(w + 0.52f, 0.12f, d + 0.52f));
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                b.Ball(Sl.Stone, P(cx + sx * (w / 2f + 0.2f), h * 0.42f, cz + sz * (d / 2f + 0.2f)), 0.46f, 4, 7, 0.85f);
        int n = Mathf.Max(3, Mathf.RoundToInt(w / 1.25f));
        for (int i = 0; i < n; i++)
        {
            float x = cx - w / 2f + (i + 0.5f) * w / n;
            int j = Mathf.Min(i, n - 1 - i);                                    // simetris kiri-kanan
            b.Ball(Sl.Stone, P(x, h * (0.28f + 0.2f * R01(rnd, j)), cz + d / 2f + 0.2f), 0.3f + 0.1f * R01(rnd, j + 9), 3, 6, 0.8f);
        }
    }

    /// <summary>Tiang sudut kayu bulat dengan kepala menggembung + balok pengikat + balok lantai.</summary>
    public static void CornerPosts(GeoBag b, float cx, float cz, float w, float d, float y0, float h, int rnd)
    {
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
            {
                Vector3 p = P(cx + sx * w / 2f, y0, cz + sz * d / 2f);
                b.Cyl(Sl.Wood, p, 0.17f, h, 6, 0.15f, true);
                b.Ball(Sl.Wood, p + P(0f, h, 0f), 0.2f, 3, 6, 0.8f);
            }
        // balok keliling atas dan bawah
        b.Box(Sl.Wood, P(cx, y0 + 0.12f, cz + d / 2f + 0.04f), new Vector3(w + 0.2f, 0.24f, 0.2f));
        b.Box(Sl.Wood, P(cx, y0 + 0.12f, cz - d / 2f - 0.04f), new Vector3(w + 0.2f, 0.24f, 0.2f));
        b.Box(Sl.Wood, P(cx + w / 2f + 0.04f, y0 + 0.12f, cz), new Vector3(0.2f, 0.24f, d + 0.2f));
        b.Box(Sl.Wood, P(cx - w / 2f - 0.04f, y0 + 0.12f, cz), new Vector3(0.2f, 0.24f, d + 0.2f));
        b.Box(Sl.Wood, P(cx, y0 + h - 0.14f, cz + d / 2f + 0.04f), new Vector3(w + 0.2f, 0.28f, 0.22f));
        b.Box(Sl.Wood, P(cx, y0 + h - 0.14f, cz - d / 2f - 0.04f), new Vector3(w + 0.2f, 0.28f, 0.22f));
        b.Box(Sl.Wood, P(cx + w / 2f + 0.04f, y0 + h - 0.14f, cz), new Vector3(0.22f, 0.28f, d + 0.2f));
        b.Box(Sl.Wood, P(cx - w / 2f - 0.04f, y0 + h - 0.14f, cz), new Vector3(0.22f, 0.28f, d + 0.2f));
    }

    /// <summary>Rangka kayu pada satu muka (w lebar, x dari -w/2..w/2, z = muka). Setelah b.Push ke muka; tiang tengah + braket diagonal melengkung.</summary>
    public static void Frame(GeoBag b, float w, float y0, float h, int rnd, int style)
    {
        float t = 0.17f;
        int posts = Mathf.Max(2, Mathf.RoundToInt(w / 2.4f) + 1);
        for (int i = 1; i < posts - 1; i++)
        {
            float x = -w / 2f + i * w / (posts - 1);
            b.Box(Sl.Wood, P(x, y0 + h / 2f, 0.05f), new Vector3(t, h - 0.3f, 0.16f));
        }
        b.Box(Sl.Wood, P(0f, y0 + h * 0.5f, 0.05f), new Vector3(w - 0.2f, 0.12f, 0.14f));
        if (style == 1)
        {
            float seg = w / (posts - 1);
            for (int i = 0; i < posts - 1; i++)
            {
                float x0 = -w / 2f + i * seg, x1 = x0 + seg;
                int jm = Mathf.Min(i, posts - 2 - i);
                if (2 * i == posts - 2) continue;                              // segmen tengah tanpa diagonal (menjaga simetri)
                bool flip = ((rnd >> jm) & 1) == 0;
                if (i > posts - 2 - i) flip = !flip;                           // sisi kanan = cermin sisi kiri
                if (flip) b.Beam(Sl.Wood, P(x0, y0 + 0.3f, 0.06f), P(x1, y0 + h * 0.5f, 0.06f), 0.13f, 0.14f, Vector3.forward);
                else b.Beam(Sl.Wood, P(x1, y0 + 0.3f, 0.06f), P(x0, y0 + h * 0.5f, 0.06f), 0.13f, 0.14f, Vector3.forward);
            }
        }
    }

    // ====================================================================== jendela & pintu
    /// <summary>Jendela lengkung menyala. c = tengah bawah. glow=true memakai bahan menyala.</summary>
    public static void ArchWin(GeoBag b, Vector3 c, float w, float h, bool shutters, bool flowers)
    {
        BuildKit.ArchWindow(b, c, w, h, Sl.WoodLight);
        if (shutters)
        {
            b.Box(Sl.ClothB, c + P(-w / 2f - 0.42f, h * 0.45f, 0.14f), new Vector3(0.5f, h * 0.9f, 0.08f));
            b.Box(Sl.ClothB, c + P(w / 2f + 0.42f, h * 0.45f, 0.14f), new Vector3(0.5f, h * 0.9f, 0.08f));
        }
        if (flowers) FlowerBox(b, c + P(0f, -0.18f, 0.1f), w + 0.5f);
    }

    public static void FlowerBox(GeoBag b, Vector3 c, float w)
    {
        b.Box(Sl.Wood, c + P(0f, -0.1f, 0.3f), new Vector3(w, 0.34f, 0.42f));
        int n = Mathf.Max(3, Mathf.RoundToInt(w / 0.4f));
        for (int i = 0; i < n; i++)
        {
            float x = -w / 2f + 0.15f + i * (w - 0.3f) / (n - 1);
            b.Ball(Sl.Hedge, c + P(x, 0.15f, 0.3f), 0.2f, 3, 6, 0.8f);
            b.Ball(i % 3 == 0 ? Sl.Blossom : (i % 3 == 1 ? Sl.Gold : Sl.Cloth), c + P(x + 0.04f, 0.33f, 0.36f), 0.1f, 3, 5);
        }
    }

    /// <summary>Jendela menjorok (oriel) tiga sisi dengan atap kecil. c = tengah bawah pada muka dinding.</summary>
    public static void Oriel(GeoBag b, Vector3 c, float w, float h, int roofSlot)
    {
        float dpt = 0.9f;
        b.Box(Sl.Wood, c + P(0f, -0.15f, dpt / 2f), new Vector3(w + 0.3f, 0.3f, dpt + 0.2f));
        b.Beam(Sl.Wood, c + P(-w / 2f, -0.3f, 0.05f), c + P(-w / 2f + 0.3f, -1.1f, 0.05f + dpt * 0.5f), 0.14f, 0.14f);
        b.Beam(Sl.Wood, c + P(w / 2f, -0.3f, 0.05f), c + P(w / 2f - 0.3f, -1.1f, 0.05f + dpt * 0.5f), 0.14f, 0.14f);
        b.Box(Sl.Plaster, c + P(0f, h / 2f, dpt), new Vector3(w, h, 0.12f));
        b.Quad(Sl.Glass, c + P(-w / 2f + 0.12f, 0.18f, dpt + 0.07f), c + P(w / 2f - 0.12f, 0.18f, dpt + 0.07f), c + P(w / 2f - 0.12f, h - 0.15f, dpt + 0.07f), c + P(-w / 2f + 0.12f, h - 0.15f, dpt + 0.07f));
        b.Box(Sl.Wood, c + P(0f, h / 2f, dpt + 0.09f), new Vector3(0.08f, h - 0.2f, 0.05f));
        b.Box(Sl.Wood, c + P(0f, h * 0.55f, dpt + 0.09f), new Vector3(w - 0.2f, 0.07f, 0.05f));
        // sisi samping miring
        b.Quad(Sl.Plaster, c + P(-w / 2f, 0f, 0.02f), c + P(-w / 2f, 0f, dpt), c + P(-w / 2f, h, dpt), c + P(-w / 2f, h, 0.02f));
        b.Quad(Sl.Plaster, c + P(w / 2f, 0f, dpt), c + P(w / 2f, 0f, 0.02f), c + P(w / 2f, h, 0.02f), c + P(w / 2f, h, dpt));
        // atap kecil
        b.Push(c + P(0f, h, dpt / 2f), 0f);
        b.Hip(roofSlot, Sl.Wood, P(0f, 0f, 0f), w + 0.2f, dpt + 0.4f, 0.75f, 0.28f);
        b.Pop();
    }

    /// <summary>Pintu lengkung kayu bergaris besi dengan bingkai batu, tangga, pot dan lentera. c = tengah bawah pada muka dinding.</summary>
    public static void ArchDoor(GeoBag b, Vector3 c, float w, float h, int rnd)
    {
        float r = w / 2f;
        float hs = h - r;
        b.Box(Sl.Stone, c + P(-r - 0.22f, hs / 2f + 0.1f, 0.06f), new Vector3(0.44f, hs + 0.2f, 0.24f));
        b.Box(Sl.Stone, c + P(r + 0.22f, hs / 2f + 0.1f, 0.06f), new Vector3(0.44f, hs + 0.2f, 0.24f));
        int n = 10;
        Vector3 cen = c + P(0f, hs, 0.12f);
        for (int k = 0; k < n; k++)
        {
            float a0 = Mathf.PI - Mathf.PI * k / n, a1 = Mathf.PI - Mathf.PI * (k + 1) / n;
            Vector3 p0 = cen + P(Mathf.Cos(a0) * r, Mathf.Sin(a0) * r, 0f), p1 = cen + P(Mathf.Cos(a1) * r, Mathf.Sin(a1) * r, 0f);
            b.Tri(Sl.Wood, cen, p1, p0);
            Vector3 q0 = cen + P(Mathf.Cos(a0) * (r + 0.44f), Mathf.Sin(a0) * (r + 0.44f), -0.04f), q1 = cen + P(Mathf.Cos(a1) * (r + 0.44f), Mathf.Sin(a1) * (r + 0.44f), -0.04f);
            b.Beam(Sl.Stone, (p0 + q0) / 2f, (p1 + q1) / 2f, 0.46f, 0.26f, Vector3.forward);
        }
        b.Box(Sl.Wood, c + P(0f, hs / 2f, 0.12f), new Vector3(w, hs, 0.12f));
        for (int i = 1; i < 5; i++) b.Box(Sl.Wood, c + P(-r + i * w / 5f, hs / 2f, 0.19f), new Vector3(0.05f, hs, 0.04f));
        b.Box(Sl.Metal, c + P(0f, hs * 0.25f, 0.22f), new Vector3(w, 0.1f, 0.05f));
        b.Box(Sl.Metal, c + P(0f, hs * 0.75f, 0.22f), new Vector3(w, 0.1f, 0.05f));
        b.Ball(Sl.Gold, c + P(r * 0.55f, hs * 0.5f, 0.24f), 0.09f, 3, 6);
        // tangga & lentera
        if (c.y > 0.12f)
        {
            // tangga batu menyambung dari tanah sampai ambang pintu: anak tangga ~0.17 m, terbenam 0.45 m ke tanah
            int ns = Mathf.Max(2, Mathf.CeilToInt(c.y / 0.17f));
            float rs = c.y / ns, run = 0.38f, sw = w + 1.0f;
            for (int i = 0; i < ns; i++)
            {
                float top = rs * (ns - i);                          // i = 0 paling dekat pintu
                float zc = c.z + 0.18f + run * (i + 0.5f);
                b.Box(i % 2 == 0 ? Sl.Stone : Sl.DarkStone, new Vector3(c.x, (top - 0.45f) / 2f, zc), new Vector3(sw - i * 0.06f, top + 0.45f, run));
                b.Box(Sl.Stone, new Vector3(c.x, top - 0.015f, zc + 0.02f), new Vector3(sw - i * 0.06f + 0.06f, 0.05f, run + 0.05f));
            }
            // ambang/teras datar di depan pintu + pipi tangga miring
            b.Box(Sl.DarkStone, new Vector3(c.x, c.y - 0.03f, c.z + 0.09f), new Vector3(sw, 0.08f, 0.3f));
            foreach (float sx in new[] { -1f, 1f })
            {
                Vector3 hi = new Vector3(c.x + sx * (sw / 2f + 0.07f), c.y + 0.12f, c.z + 0.2f);
                Vector3 lo = new Vector3(c.x + sx * (sw / 2f + 0.07f), 0.1f, c.z + 0.18f + run * ns);
                b.Beam(Sl.DarkStone, hi, lo, 0.16f, 0.2f, Vector3.up);
            }
        }
        else b.Stairs(Sl.Stone, c + P(0f, 0f, 0.9f), w + 0.9f, 2, 0.2f, 0.5f, 0f);
        foreach (float sg in new[] { -1f, 1f })
        {
            AnimeKit.HangLantern(b, c + P(sg * (r + 0.9f), h + 0.1f, 0.05f), 0.5f);
            b.Ball(Sl.Hedge, c + P(sg * (r + 0.85f), 0.35f, 0.75f), 0.4f, 3, 6, 0.9f);
            b.Ball((rnd & 1) == 0 ? Sl.Blossom : Sl.Gold, c + P(sg * (r + 0.85f), 0.7f, 0.78f), 0.15f, 3, 5);
        }
    }

    // ====================================================================== atap & menara
    /// <summary>Atap kerucut terompet (cekung, ujung bawah melebar) dengan cincin tepi, bola & pucuk. base = tepi bawah.</summary>
    public static void FlareRoof(GeoBag b, int slot, Vector3 baseP, float R, float H, int n, int trimSlot)
    {
        int M = 9;
        float yPrev = 0f, rPrev = R;
        for (int k = 1; k <= M; k++)
        {
            float t = k / (float)M;
            float y = H * t;
            float r = R * Mathf.Pow(1f - t, 1.85f);
            b.Cyl(slot, baseP + P(0f, yPrev, 0f), rPrev, y - yPrev, n, r, false);
            yPrev = y; rPrev = r;
        }
        b.Cyl(trimSlot, baseP + P(0f, -0.22f, 0f), R * 0.93f, 0.26f, n, R * 1.0f, true);
        // sisik tepi
        int m = n;
        for (int i = 0; i < m; i++)
        {
            float a = i / (float)m * Mathf.PI * 2f;
            b.Ball(slot, baseP + P(Mathf.Sin(a) * R * 0.98f, 0.02f, Mathf.Cos(a) * R * 0.98f), R * 0.12f, 3, 5, 0.5f);
        }
        b.Ball(Sl.Gold, baseP + P(0f, H + 0.05f, 0f), 0.24f, 4, 8);
        b.Cyl(Sl.Gold, baseP + P(0f, H + 0.2f, 0f), 0.08f, 1.5f, 6, 0.02f, false);
    }

    /// <summary>Menara bulat bertingkat. Mengembalikan y puncak atap.</summary>
    public static float Tower(GeoBag b, float cx, float cz, float r, int floors, float fh, int wallSlot, int roofSlot, float roofH, int rnd, bool shutters)
    {
        float y0 = 0.9f;
        int n = 14;
        b.Cyl(Sl.DarkStone, P(cx, 0f, cz), r + 0.5f, 0.3f, n, r + 0.45f, true);
        b.Cyl(Sl.Stone, P(cx, 0.3f, cz), r + 0.38f, y0 - 0.3f, n, r + 0.3f, true);
        b.Cyl(wallSlot, P(cx, y0, cz), r, floors * fh, n, r, false);
        for (int i = 0; i <= floors; i++)
            b.Cyl(Sl.Wood, P(cx, y0 + i * fh - 0.1f, cz), r + 0.12f, 0.24f, n, r + 0.12f, true);
        for (int i = 0; i < floors; i++)
        {
            int cnt = 3;
            for (int k = 0; k < cnt; k++)
            {
                float a = (k + 0.5f * (i % 2)) * Mathf.PI * 2f / cnt + 0.4f;
                b.Push(P(cx + Mathf.Sin(a) * r, y0 + i * fh + fh * 0.2f, cz + Mathf.Cos(a) * r), a * Mathf.Rad2Deg);
                if (i == 0 && k == 0) ArchWin(b, P(0f, 0.1f, 0f), 0.8f, 1.7f, shutters, false);
                else ArchWin(b, P(0f, 0.1f, 0f), 0.7f, 1.5f, shutters, k == 1);
                b.Pop();
            }
        }
        float yt = y0 + floors * fh;
        // lis atas menjorok + panggung kecil
        b.Cyl(Sl.WoodLight, P(cx, yt + 0.1f, cz), r + 0.55f, 0.5f, n, r + 0.85f, true);
        float rr = r + 0.85f;
        FlareRoof(b, roofSlot, P(cx, yt + 0.6f, cz), rr, roofH, n, Sl.Wood);
        return yt + 0.6f + roofH + 1.5f;
    }

    /// <summary>Cerobong bulat dari batu dengan topi dan pot asap.</summary>
    public static void RoundChimney(GeoBag b, Vector3 baseP, float r, float h)
    {
        b.Cyl(Sl.Stone, baseP, r + 0.12f, 0.5f, 8, r, true);
        b.Cyl(Sl.Stone, baseP + P(0f, 0.5f, 0f), r, h - 0.5f, 8, r * 0.92f, true);
        b.Cyl(Sl.DarkStone, baseP + P(0f, h, 0f), r * 1.25f, 0.3f, 8, r * 1.1f, true);
        b.Cyl(Sl.Metal, baseP + P(0f, h + 0.3f, 0f), r * 0.7f, 0.42f, 8, r * 0.6f, true);
        b.Cyl(Sl.RoofB, baseP + P(-r * 0.35f, h + 0.7f, 0f), r * 0.22f, 0.45f, 6, r * 0.17f, true);
        b.Cyl(Sl.RoofB, baseP + P(r * 0.35f, h + 0.7f, 0.05f), r * 0.2f, 0.38f, 6, r * 0.16f, true);
        for (int i = 0; i < 3; i++) b.Ball(Sl.Plaster, baseP + P(r * (0.2f + 0.35f * i), h + 1.35f + i * 0.5f, 0f), 0.2f + 0.09f * i, 3, 6, 0.8f);   // asap
        for (int i = 0; i < 5; i++) b.Box(Sl.DarkStone, baseP + P(Mathf.Sin(i * 1.26f) * (r + 0.09f), 0.55f + i * ((h - 0.9f) / 5f), Mathf.Cos(i * 1.26f) * (r + 0.09f)), P(0.3f, 0.16f, 0.12f), i * 72f);
    }

    /// <summary>Tonjolan lumut dan rumput di atap (gumpalan daun) di sepanjang titik-titik yang diberikan.</summary>
    public static void Moss(GeoBag b, Vector3 p, float r, int rnd)
    {
        b.Ball(Sl.Hedge, p, r, 3, 6, 0.55f);
        b.Ball(Sl.Leaf, p + P(r * 0.7f, 0.02f, r * 0.2f), r * 0.65f, 3, 6, 0.5f);
        if ((rnd & 3) == 0) b.Ball(Sl.Blossom, p + P(-r * 0.4f, r * 0.3f, 0.1f), r * 0.28f, 3, 5);
    }

    // ====================================================================== teras, balkon, halaman
    /// <summary>Teras beratap kecil di atas pintu: dua tiang kayu berbraket + atap miring melengkung.</summary>
    public static void Porch(GeoBag b, float x, float zWall, float w, float depth, float y, float roofY, int roofSlot, int rnd)
    {
        float zf = zWall + depth;
        foreach (float sx in new[] { -1f, 1f })
        {
            Vector3 p = P(x + sx * (w / 2f), y, zf);
            b.Cyl(Sl.Wood, p, 0.14f, roofY - y, 6, 0.12f, true);
            b.Beam(Sl.Wood, p + P(0f, roofY - y - 0.9f, 0f), p + P(-sx * 0.6f, roofY - y, 0f), 0.1f, 0.1f);
            b.Beam(Sl.Wood, p + P(0f, roofY - y - 0.9f, 0f), p + P(0f, roofY - y - 0.9f, -depth + 0.1f), 0.1f, 0.1f);
        }
        b.Box(Sl.Wood, P(x, roofY - 0.1f, zf), new Vector3(w + 0.3f, 0.2f, 0.22f));
        // atap pelana kecil menghadap depan
        b.Gable(roofSlot, Sl.Plaster, Sl.Wood, P(x, roofY, zWall + depth / 2f), w + 0.4f, depth + 0.4f, 0.95f, 0.45f, false);
        // lantai teras
        b.BoxB(Sl.Stone, P(x, 0f, zWall + depth / 2f), new Vector3(w + 0.6f, y, depth + 0.3f));
    }

    /// <summary>Balkon kayu berpagar. c = tengah sisi dinding pada lantai balkon; menjorok depth ke +Z.</summary>
    public static void Balcony(GeoBag b, Vector3 c, float w, float depth)
    {
        b.Box(Sl.WoodLight, c + P(0f, -0.1f, depth / 2f), new Vector3(w, 0.2f, depth));
        for (int i = 0; i < 4; i++)
        {
            float x = -w / 2f + 0.3f + i * (w - 0.6f) / 3f;
            b.Beam(Sl.Wood, c + P(x, -0.2f, 0.05f), c + P(x, -0.9f, depth - 0.15f), 0.12f, 0.12f);
        }
        b.Box(Sl.Wood, c + P(0f, 0.95f, depth - 0.08f), new Vector3(w, 0.12f, 0.14f));
        b.Box(Sl.Wood, c + P(-w / 2f + 0.07f, 0.95f, depth / 2f), new Vector3(0.14f, 0.12f, depth));
        b.Box(Sl.Wood, c + P(w / 2f - 0.07f, 0.95f, depth / 2f), new Vector3(0.14f, 0.12f, depth));
        int n = Mathf.Max(4, Mathf.RoundToInt(w / 0.4f));
        for (int i = 0; i <= n; i++)
            b.Cyl(Sl.Wood, c + P(-w / 2f + i * w / n, 0f, depth - 0.08f), 0.045f, 0.95f, 5, 0.04f, true);
        b.Ball(Sl.Hedge, c + P(-w / 2f + 0.5f, 0.28f, depth - 0.35f), 0.3f, 3, 6, 0.8f);
        b.Ball(Sl.Blossom, c + P(-w / 2f + 0.5f, 0.55f, depth - 0.35f), 0.14f, 3, 5);
    }

    /// <summary>Pagar kayu pendek (picket) sepanjang x dari a ke b pada z tetap.</summary>
    public static void Picket(GeoBag b, float x0, float x1, float z, float h)
    {
        float len = x1 - x0;
        int n = Mathf.Max(2, Mathf.RoundToInt(len / 0.32f));
        for (int i = 0; i <= n; i++)
        {
            float x = x0 + i * len / n;
            b.Box(Sl.WoodLight, P(x, h / 2f, z), new Vector3(0.14f, h, 0.06f));
            b.Cone(Sl.WoodLight, P(x, h, z), 0.08f, 0.1f, 4);
        }
        b.Box(Sl.Wood, P((x0 + x1) / 2f, h * 0.3f, z - 0.04f), new Vector3(len, 0.08f, 0.06f));
        b.Box(Sl.Wood, P((x0 + x1) / 2f, h * 0.75f, z - 0.04f), new Vector3(len, 0.08f, 0.06f));
    }

    public static void Woodpile(GeoBag b, Vector3 c, float yaw)
    {
        b.Push(c, yaw);
        for (int row = 0; row < 3; row++)
            for (int i = 0; i < 5 - row; i++)
                b.Beam(Sl.WoodLight, P(-0.9f + i * 0.4f + row * 0.2f, 0.17f + row * 0.32f, -0.45f), P(-0.9f + i * 0.4f + row * 0.2f, 0.17f + row * 0.32f, 0.45f), 0.32f, 0.32f);
        b.Pop();
    }
}
