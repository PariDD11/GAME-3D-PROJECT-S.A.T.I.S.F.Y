using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// v35z102n74: bagian bangunan desa — jendela berbingkai (persegi atau lengkung, daun jendela setengah terbuka, kotak bunga), pintu papan (lengkung, tudung),
/// cerobong bertingkat berpot, fondasi batu bulat tak rata, sulur ivy, serambi. Bidang jendela/pintu: z = 0, menghadap +Z, dipasang menempel di depan dinding.
/// </summary>
public static partial class V2Kit
{
    // ------------------------------------------------------------------ jendela
    public static void Window(GeoBag b, float x, float y, float w, float h, bool shutters, bool flowers, int seed, bool arch = false)
    {
        float r = w * 0.5f, yb = y - h * 0.5f, yt = y + h * 0.5f;
        float fw = 0.1f, fd = 0.12f;
        float yS = arch ? yt - r : yt;                                    // garis awal lengkung
        float zg = 0.035f;
        // kaca (agak masuk dari bingkai)
        b.Quad(Sl.Glass, new Vector3(x - r, yb, zg), new Vector3(x + r, yb, zg), new Vector3(x + r, yS, zg), new Vector3(x - r, yS, zg));
        // bingkai
        b.Box(Sl.Wood, new Vector3(x - r - fw * 0.5f, (yb + yS) * 0.5f, fd * 0.5f), new Vector3(fw, yS - yb + fw, fd));
        b.Box(Sl.Wood, new Vector3(x + r + fw * 0.5f, (yb + yS) * 0.5f, fd * 0.5f), new Vector3(fw, yS - yb + fw, fd));
        b.Box(Sl.Wood, new Vector3(x, yb - fw * 0.5f, fd * 0.5f), new Vector3(w + 2f * fw, fw, fd));
        float yBar = arch ? yS + r * 0.96f : yt;
        if (arch)
        {
            const int n = 8;
            Vector3 cen = new Vector3(x, yS, zg);
            for (int k = 0; k < n; k++)
            {
                float a0 = Mathf.PI * (1f - k / (float)n), a1 = Mathf.PI * (1f - (k + 1) / (float)n);
                Vector3 d0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f), d1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f);
                b.Tri(Sl.Glass, cen, cen + d1 * r, cen + d0 * r);
                b.Beam(Sl.Wood, cen + d0 * (r + fw * 0.5f) + new Vector3(0f, 0f, fd * 0.5f - zg), cen + d1 * (r + fw * 0.5f) + new Vector3(0f, 0f, fd * 0.5f - zg), fw * 1.05f, fd, Vector3.forward);
            }
            b.Box(Sl.Stone, new Vector3(x, yS + r + fw + 0.07f, 0.08f), new Vector3(0.24f, 0.22f, 0.16f));       // batu kunci
        }
        else
        {
            b.Box(Sl.Wood, new Vector3(x, yt + fw * 0.5f, fd * 0.5f), new Vector3(w + 2f * fw, fw, fd));
            b.Box(Sl.Wood, new Vector3(x, yt + fw + 0.08f, 0.1f), new Vector3(w + 0.55f, 0.15f, 0.18f));          // ambang atas berat
        }
        // kisi
        b.Quad(Sl.Wood, new Vector3(x - 0.02f, yb, 0.075f), new Vector3(x + 0.02f, yb, 0.075f), new Vector3(x + 0.02f, yBar, 0.075f), new Vector3(x - 0.02f, yBar, 0.075f));
        float ym = yb + (yS - yb) * 0.56f;
        b.Quad(Sl.Wood, new Vector3(x - r, ym - 0.02f, 0.075f), new Vector3(x + r, ym - 0.02f, 0.075f), new Vector3(x + r, ym + 0.02f, 0.075f), new Vector3(x - r, ym + 0.02f, 0.075f));
        // ambang batu
        b.Box(Sl.Stone, new Vector3(x, yb - fw - 0.04f, 0.13f), new Vector3(w + 0.46f, 0.09f, 0.32f));
        if (shutters)
        {
            for (int sd = -1; sd <= 1; sd += 2)
            {
                float open = (seed + sd) % 3 == 0 ? 0f : 18f + 40f * Rnd(seed, sd + 5);
                float lw = r * 0.96f, lh = h + (arch ? 0.05f : 0.12f);
                b.Push(new Vector3(x + sd * (r + fw + 0.02f), y, 0.09f), -sd * open);
                b.Box(Sl.WoodLight, new Vector3(sd * lw * 0.5f, 0f, 0.025f), new Vector3(lw, lh, 0.05f));
                b.Beam(Sl.Wood, new Vector3(sd * 0.05f, -lh * 0.4f, 0.06f), new Vector3(sd * (lw - 0.05f), lh * 0.4f, 0.06f), 0.07f, 0.03f, Vector3.forward);
                b.Quad(Sl.Dark, new Vector3(sd * lw * 0.5f - 0.05f, lh * 0.2f, 0.052f), new Vector3(sd * lw * 0.5f + 0.05f, lh * 0.2f, 0.052f), new Vector3(sd * lw * 0.5f + 0.05f, lh * 0.2f + 0.16f, 0.052f), new Vector3(sd * lw * 0.5f - 0.05f, lh * 0.2f + 0.16f, 0.052f));
                b.Pop();
            }
        }
        if (flowers)
        {
            float fy = yb - fw - 0.24f;
            b.Box(Sl.Wood, new Vector3(x, fy, 0.22f), new Vector3(w + 0.22f, 0.17f, 0.26f));
            int[] bl = { Sl.Blossom, Sl.Lip, Sl.EyeW, Sl.Blossom, Sl.Hair };
            int nb = Mathf.Max(4, Mathf.RoundToInt(w / 0.17f));
            for (int k = 0; k < nb; k++)
            {
                float px = x - w * 0.5f + (k + 0.5f) * w / nb;
                b.Ball(Sl.Fur, new Vector3(px, fy + 0.13f, 0.22f), 0.12f + 0.03f * Rnd(seed, k + 20), 2, 5, 0.9f);
                if (k % 2 == 0) b.Ball(bl[(k / 2 + seed) % 5], new Vector3(px + 0.04f, fy + 0.26f + 0.03f * Rnd(seed, k), 0.24f), 0.065f, 2, 5);
            }
        }
    }

    // ------------------------------------------------------------------ pintu papan
    public static void Door(GeoBag b, float x, float w, float h, bool arch, bool lantern, int seed, bool stoneFrame = false)
    {
        float r = w * 0.5f;
        float yS = arch ? h - r : h;
        int fr = stoneFrame ? Sl.Stone : Sl.Wood;
        b.Box(fr, new Vector3(x - r - 0.12f, yS * 0.5f, 0.09f), new Vector3(0.24f, yS, 0.18f));
        b.Box(fr, new Vector3(x + r + 0.12f, yS * 0.5f, 0.09f), new Vector3(0.24f, yS, 0.18f));
        if (!arch) b.Box(fr, new Vector3(x, h + 0.12f, 0.09f), new Vector3(w + 0.6f, 0.24f, 0.18f));
        else
        {
            Vector3 cen = new Vector3(x, yS, 0.09f);
            for (int k = 0; k < 6; k++)
            {
                float a0 = Mathf.PI * (1f - k / 6f), a1 = Mathf.PI * (1f - (k + 1) / 6f);
                Vector3 d0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f), d1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f);
                b.Beam(fr, cen + d0 * (r + 0.12f), cen + d1 * (r + 0.12f), 0.25f, 0.18f, Vector3.forward);
            }
        }
        int np = Mathf.Max(3, Mathf.RoundToInt(w / 0.2f));
        float pw = w / np;
        for (int k = 0; k < np; k++)
        {
            float px = x - r + (k + 0.5f) * pw;
            float dxn = (px - x) / r;
            float top = arch ? yS + Mathf.Sqrt(Mathf.Max(0f, 1f - dxn * dxn)) * r - 0.02f : h;
            b.Box(((k + seed) % 3 == 0) ? Sl.Wood : Sl.WoodLight, new Vector3(px, top * 0.5f, 0.05f), new Vector3(pw - 0.012f, top, 0.07f));
        }
        b.Beam(Sl.Metal, new Vector3(x - r + 0.02f, h * 0.76f, 0.1f), new Vector3(x + r * 0.35f, h * 0.76f, 0.1f), 0.1f, 0.03f);
        b.Beam(Sl.Metal, new Vector3(x - r + 0.02f, h * 0.22f, 0.1f), new Vector3(x + r * 0.35f, h * 0.22f, 0.1f), 0.1f, 0.03f);
        b.Ball(Sl.Metal, new Vector3(x + r * 0.6f, h * 0.46f, 0.11f), 0.05f, 3, 5);
        b.Box(Sl.Stone, new Vector3(x, 0.09f, 0.55f), new Vector3(w + 0.9f, 0.18f, 0.9f));
        b.Box(Sl.Stone, new Vector3(x, 0.03f, 1.1f), new Vector3(w + 0.5f, 0.06f, 0.3f));
        if (lantern)
        {
            float lx = x + r + 0.8f, ly = h * 0.92f;
            b.Beam(Sl.Metal, new Vector3(lx - 0.3f, ly + 0.05f, 0.05f), new Vector3(lx, ly + 0.05f, 0.36f), 0.04f, 0.04f);
            b.Box(Sl.Metal, new Vector3(lx, ly - 0.16f, 0.36f), new Vector3(0.2f, 0.04f, 0.2f));
            b.Box(Sl.Glow, new Vector3(lx, ly - 0.04f, 0.36f), new Vector3(0.14f, 0.2f, 0.14f));
            b.Cyl(Sl.Metal, new Vector3(lx, ly + 0.06f, 0.36f), 0.14f, 0.12f, 4, 0.02f, false);
        }
    }

    /// <summary>Tudung kecil di atas pintu: atap miring berundak di dua konsol kayu. maxBack = tinggi tertinggi sisi belakang (di bawah tritisan).</summary>
    public static void DoorHood(GeoBag b, float x, float w, float h, int sRoof, int sWood, int seed, float maxBack = 99f)
    {
        float yBack = Mathf.Min(h + 1.15f, maxBack), yb = Mathf.Max(h + 0.3f, yBack - 0.55f);
        float depth = 1.15f, hw2 = w * 0.5f + 0.55f;
        Pent(b, sRoof, sWood, x - hw2, x + hw2, 0f, depth, yBack, yb, seed);
        for (int sd = -1; sd <= 1; sd += 2)
        {
            float bx = x + sd * (hw2 - 0.13f);
            b.Beam(sWood, new Vector3(bx, yb - 0.7f, 0.05f), new Vector3(bx, yb - 0.08f, depth * 0.84f), 0.1f, 0.1f, new Vector3(sd, 0f, 0f));
            b.Beam(sWood, new Vector3(bx, yb - 0.06f, 0.05f), new Vector3(bx, yb - 0.06f, depth * 0.88f), 0.1f, 0.1f, new Vector3(sd, 0f, 0f));
        }
    }

    // ------------------------------------------------------------------ cerobong batu bertingkat
    public static void Chimney(GeoBag b, Vector3 baseP, float w, float h, float yaw = 0f)
    {
        b.Push(baseP, yaw);
        float h1 = h * 0.6f, w2 = w * 0.8f, h2 = h - h1 - 0.06f;
        b.Box(Sl.Stone, new Vector3(0f, h1 * 0.5f, 0f), new Vector3(w, h1, w));
        b.Box(Sl.DarkStone, new Vector3(0f, h1 + 0.03f, 0f), new Vector3(w + 0.14f, 0.08f, w + 0.14f));
        b.Box(Sl.Stone, new Vector3(0f, h1 + 0.06f + h2 * 0.5f, 0f), new Vector3(w2, h2, w2));
        b.Box(Sl.DarkStone, new Vector3(0f, h + 0.02f, 0f), new Vector3(w2 + 0.22f, 0.1f, w2 + 0.22f));
        b.Box(Sl.DarkStone, new Vector3(0f, h + 0.11f, 0f), new Vector3(w2 + 0.08f, 0.08f, w2 + 0.08f));
        b.Cyl(Sl.Roof, new Vector3(-w2 * 0.2f, h + 0.15f, 0f), 0.11f, 0.34f, 8, 0.09f, true, false, Sl.Dark);
        b.Cyl(Sl.Roof, new Vector3(w2 * 0.22f, h + 0.15f, w2 * 0.05f), 0.09f, 0.26f, 8, 0.075f, true, false, Sl.Dark);
        // batu menonjol di badan cerobong
        for (int k = 0; k < 3; k++)
            b.Box(Sl.DarkStone, new Vector3((k - 1) * w * 0.3f, h1 * (0.25f + 0.23f * k), w * 0.5f + 0.01f), new Vector3(0.22f, 0.14f, 0.05f));
        b.Pop();
    }

    // ------------------------------------------------------------------ fondasi batu bulat tak rata
    public static void Foundation(GeoBag b, float w, float d, float baseY, int seed, float doorX)
    {
        b.Box(Sl.Stone, new Vector3(0f, baseY * 0.5f, 0f), new Vector3(w + 0.22f, baseY, d + 0.22f));
        for (int row = 0; row < 2; row++)
            for (int side = 0; side < 4; side++)
            {
                bool alongX = side < 2;
                float Ls = alongX ? w : d;
                float step = row == 0 ? (side == 1 ? 1.05f : 0.72f) : 0.95f;
                if (row == 1 && side == 1) continue;
                int n = Mathf.Max(3, Mathf.RoundToInt(Ls / step));
                float sg = (side % 2 == 0) ? 1f : -1f;
                for (int k = 0; k < n; k++)
                {
                    float t = (k + 0.5f + row * 0.5f) / (n + (row == 1 ? 0.0f : 0f)) - 0.5f;
                    if (row == 1 && k == n - 1) continue;
                    float along = t * (Ls + 0.3f) + (Rnd(seed + side + row * 5, k) - 0.5f) * 0.12f;
                    if (side == 0 && Mathf.Abs(along - doorX) < 1.0f) continue;
                    float rad = (row == 0 ? 0.27f : 0.22f) + 0.12f * Rnd(seed + side * 3 + row, k + 9);
                    float off = (alongX ? d : w) * 0.5f + 0.1f + (Rnd(seed + side + row, k + 31) - 0.2f) * 0.1f;
                    float yy = row == 0 ? baseY * (0.28f + 0.2f * Rnd(seed + side, k + 41)) : baseY * (0.78f + 0.12f * Rnd(seed + side, k + 43));
                    int sl = Rnd(seed + side + row, k + 57) > 0.8f ? Sl.DarkStone : Sl.Stone;
                    Vector3 p = alongX ? new Vector3(along, yy, sg * off) : new Vector3(sg * off, yy, along);
                    b.Ball(sl, p, rad, 3, 5, 0.7f);
                }
            }
        b.Box(Sl.DarkStone, new Vector3(0f, baseY + 0.03f, 0f), new Vector3(w + 0.3f, 0.07f, d + 0.3f));
    }

    // ------------------------------------------------------------------ sulur ivy menempel di dinding (bidang z = 0 menghadap +Z)
    public static void Ivy(GeoBag b, float x0, float y0, float width, float height, int seed)
    {
        int n = Mathf.Clamp(Mathf.RoundToInt(width * height * 7f), 10, 46);
        const int m = 7;
        for (int k = 0; k < n; k++)
        {
            float u = Rnd(seed, k * 3 + 1), v = Rnd(seed, k * 3 + 2);
            float taper = 1f - 0.6f * v;                                   // melebar di bawah, menyempit ke atas
            float px = x0 + (u - 0.5f) * width * taper + Mathf.Sin(v * 5f + seed) * 0.1f;
            float py = y0 + v * height;
            float rad = 0.13f + 0.08f * Rnd(seed, k + 90);
            int sl = Rnd(seed, k * 3 + 3) > 0.84f ? Sl.Hair : Sl.Fur;
            float z = 0.05f + 0.06f * Rnd(seed, k);
            float rot = Rnd(seed, k + 200) * 6.2832f;
            Vector3 c = new Vector3(px, py, z + 0.03f);
            for (int q = 0; q < m; q++)
            {
                float a0 = rot + q * 2f * Mathf.PI / m, a1 = rot + (q + 1) * 2f * Mathf.PI / m;
                float r0 = rad * (0.78f + 0.44f * Rnd(seed * 3 + q, k)), r1 = rad * (0.78f + 0.44f * Rnd(seed * 3 + (q + 1) % m, k));
                b.Tri(sl, c, new Vector3(px + Mathf.Cos(a0) * r0, py + Mathf.Sin(a0) * r0 * 0.9f, z), new Vector3(px + Mathf.Cos(a1) * r1, py + Mathf.Sin(a1) * r1 * 0.9f, z));
            }
            if (Rnd(seed, k + 140) > 0.94f) b.Ball(Sl.EyeW, new Vector3(px, py + 0.05f, z + 0.1f), 0.05f, 2, 4);
        }
    }

    // ------------------------------------------------------------------ serambi kecil
    public static void Porch(GeoBag b, float x, float zFront, float w, float depth, float h)
    {
        for (int sd = -1; sd <= 1; sd += 2)
        {
            float px = x + sd * w * 0.5f;
            b.Box(Sl.Stone, new Vector3(px, 0.12f, zFront + depth), new Vector3(0.34f, 0.24f, 0.34f));
            Prism(b, Sl.Wood, new Vector3(px, 0.24f, zFront + depth), new Vector3(px, h - 0.1f, zFront + depth), 0.1f, 0.085f, 8, false, false);
            b.Box(Sl.Wood, new Vector3(px, h - 0.08f, zFront + depth), new Vector3(0.26f, 0.12f, 0.26f));
            b.Beam(Sl.Wood, new Vector3(px - sd * 0.06f, h - 0.55f, zFront + depth), new Vector3(px - sd * 0.62f, h - 0.04f, zFront + depth), 0.09f, 0.08f, Vector3.forward);
        }
        b.Box(Sl.Wood, new Vector3(x, h + 0.02f, zFront + depth), new Vector3(w + 0.36f, 0.2f, 0.2f));
        b.Box(Sl.WoodLight, new Vector3(x, 0.16f, zFront + depth * 0.5f + 0.08f), new Vector3(w + 0.5f, 0.12f, depth + 0.4f));
        Pent(b, Sl.RoofB, Sl.Wood, x - w * 0.5f - 0.4f, x + w * 0.5f + 0.4f, zFront - 0.05f, zFront + depth + 0.4f, h + 1.0f, h + 0.12f, 5);
    }

    public static void WoodPile(GeoBag b, Vector3 p, float len, float hgt, float yaw, int seed)
    {
        b.Push(p, yaw);
        int rows = Mathf.Max(2, (int)(hgt / 0.26f));
        for (int r = 0; r < rows; r++)
        {
            int n = Mathf.Max(2, (int)(len / 0.3f) - r);
            for (int k = 0; k < n; k++)
            {
                float x = -len * 0.5f + (k + 0.5f + r * 0.5f) * (len / (n + rows * 0.5f - 0.5f)) * 1.0f;
                Prism(b, ((r + k) % 3 == 0) ? Sl.WoodLight : Sl.Wood, new Vector3(x, 0.13f + r * 0.25f, -0.35f), new Vector3(x, 0.13f + r * 0.25f, 0.35f), 0.125f, 0.125f, 5, true, true);
            }
        }
        b.Box(Sl.Wood, new Vector3(-len * 0.5f - 0.05f, hgt * 0.5f, 0f), new Vector3(0.08f, hgt, 0.8f));
        b.Box(Sl.Wood, new Vector3(len * 0.5f + 0.05f, hgt * 0.5f, 0f), new Vector3(0.08f, hgt, 0.8f));
        b.Pop();
    }
}
