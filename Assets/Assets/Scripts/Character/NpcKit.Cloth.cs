using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kain untuk NpcKit: celemek, tabard, jubah (cape), mantel bahu, dan panel hias yang MENEMPEL pada permukaan badan (bukan kotak datar).
/// Semua panel dibangun dari titik permukaan badan Ctx.TPt, jadi mengikuti lekuk dada/pinggang dan lipatan rok.
/// </summary>
public static partial class NpcKit
{
    static float Sm(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

    /// <summary>Segiempat yang otomatis menghadap keluar dari sumbu badan (urutan titik tidak perlu dipikirkan).</summary>
    static void QuadOut(GeoBag b, int slot, Vector3 a, Vector3 bb, Vector3 cc, Vector3 d, bool useCenter = false, Vector3 center = default(Vector3))
    {
        Vector3 n = Vector3.Cross(bb - a, cc - a);
        Vector3 mid = (a + bb + cc + d) * 0.25f;
        if (useCenter) mid -= center; else mid.y = 0f;
        if (Vector3.Dot(n, mid) < 0f) b.Quad(slot, d, cc, bb, a); else b.Quad(slot, a, bb, cc, d);
    }

    /// <summary>Permukaan terbuka dari grid titik pt(baris, kolom); both = tampak dari dua sisi (kain bebas), selain itu menghadap keluar.</summary>
    static void SurfGrid(GeoBag b, int slot, int rows, int cols, System.Func<int, int, Vector3> pt, bool both, bool useCenter = false, Vector3 center = default(Vector3))
    {
        var g = new Vector3[rows + 1][];
        for (int r = 0; r <= rows; r++)
        {
            g[r] = new Vector3[cols + 1];
            for (int j = 0; j <= cols; j++) g[r][j] = pt(r, j);
        }
        for (int r = 0; r < rows; r++)
            for (int j = 0; j < cols; j++)
            {
                if (both) b.QuadBoth(slot, g[r][j + 1], g[r][j], g[r + 1][j], g[r + 1][j + 1]);
                else QuadOut(b, slot, g[r][j + 1], g[r][j], g[r + 1][j], g[r + 1][j + 1], useCenter, center);
            }
    }

    /// <summary>Sudut cincin badan (rad, 0 = depan) untuk jarak mendatar x pada ketinggian y.</summary>
    static float AngleAtX(Ctx c, float y, float x, float grow, float flare, bool back)
    {
        float rx = Mathf.Max(0.05f, Mathf.Abs(c.TPt(y, Mathf.PI * 0.5f, grow, flare).x));
        float a = Mathf.Asin(Mathf.Clamp(x / rx, -0.985f, 0.985f));
        return back ? Mathf.PI - a : a;
    }

    /// <summary>
    /// Panel kain yang menempel di depan (back = false) atau punggung badan.
    /// topAt/hemAt: tinggi tepi atas/bawah menurut u (-1..1 melintang); xLo/xHi: batas kiri/kanan (meter) menurut tinggi y.
    /// </summary>
    static void Panel(GeoBag b, Ctx c, int slot, bool back, System.Func<float, float> topAt, System.Func<float, float> hemAt,
        System.Func<float, float> xLo, System.Func<float, float> xHi, float grow, float flare, int cols, int rows, float fold = 0f, bool both = false)
    {
        SurfGrid(b, slot, rows, cols, (r, j) =>
        {
            float t = r / (float)rows, u = j / (float)cols, uc = u * 2f - 1f;
            float y = Mathf.Lerp(topAt(uc), hemAt(uc), t);
            float x = Mathf.Lerp(xLo(y), xHi(y), u);
            float g = grow + fold * t * Mathf.Sin(u * 11f + 0.8f);
            return c.TPt(y, AngleAtX(c, y, x, g, flare, back), g, flare);
        }, both);
    }

    // =====================================================================
    // Celemek: dada menyempit ke atas, rok melebar ke bawah, pita pinggang, tali leher, kantong
    // =====================================================================
    static void Apron(GeoBag b, Ctx c, int slot, float top, float hem, float halfW, float grow = 0.016f)
    {
        float Wk = Mathf.Lerp(1f, c.W, 0.7f), fl = lastFlare, hw = halfW * Wk;
        float waist = Mathf.Clamp(1.0f, hem + 0.12f, top - 0.12f);
        int trim = slot == Sl.EyeW ? Sl.CoatC : Sl.Dark;
        System.Func<float, float> topAt = u => top;
        System.Func<float, float> hemAt = u => hem + 0.020f * u * u;
        System.Func<float, float> half = y =>
        {
            if (y > waist) return hw * Mathf.Lerp(0.84f, 0.56f, Sm((y - waist) / Mathf.Max(0.05f, top - waist)));
            float q = (waist - y) / Mathf.Max(0.05f, waist - hem);
            return hw * Mathf.Lerp(0.84f, 1.12f, q * q);
        };
        System.Func<float, float> nhalf = y => -half(y);
        Panel(b, c, slot, false, topAt, hemAt, nhalf, half, grow, fl, 12, 16, 0.0025f);
        // pinggiran: tepi kiri-kanan dan bawah
        Panel(b, c, trim, false, topAt, u => hem + 0.020f, y => -half(y), y => -half(y) + 0.012f, grow + 0.003f, fl, 1, 16);
        Panel(b, c, trim, false, topAt, u => hem + 0.020f, y => half(y) - 0.012f, y => half(y), grow + 0.003f, fl, 1, 16);
        Panel(b, c, trim, false, u => hemAt(u) + 0.026f, hemAt, nhalf, half, grow + 0.003f, fl, 12, 1);
        if (top - hem > 0.55f)         // kantong
        {
            float py = hem + 0.22f;
            Panel(b, c, trim, false, u => py, u => py - 0.085f, y => -0.050f * Wk, y => 0.050f * Wk, grow + 0.004f, fl, 4, 3);
            Panel(b, c, slot, false, u => py - 0.008f, u => py - 0.077f, y => -0.042f * Wk, y => 0.042f * Wk, grow + 0.0055f, fl, 4, 3);
        }
        // pita pinggang + simpul di punggung
        var rs = new List<Vector3[]> { c.TRing(waist - 0.022f, grow + 0.002f, fl), c.TRing(waist + 0.022f, grow + 0.002f, fl) };
        NLoft(b, trim, rs, false, false);
        Vector3 bk = c.TPt(waist, Mathf.PI, grow + 0.004f, fl);
        for (int sx = -1; sx <= 1; sx += 2)
        {
            b.Ball(trim, bk + new Vector3(sx * 0.030f, 0f, -0.012f), 0.024f, 4, 8, 0.7f);
            b.QuadBoth(trim, bk + new Vector3(sx * 0.006f, -0.01f, -0.020f), bk + new Vector3(sx * 0.024f, -0.01f, -0.020f),
                bk + new Vector3(sx * 0.050f, -0.17f, -0.026f), bk + new Vector3(sx * 0.028f, -0.17f, -0.026f));
        }
        b.Ball(trim, bk + new Vector3(0f, 0f, -0.016f), 0.014f, 4, 6);
        // tali leher: sudut dada -> bahu -> punggung
        for (int sx = -1; sx <= 1; sx += 2)
        {
            float xa = sx * half(top) * 0.8f;
            Vector3 p0 = c.TPt(top - 0.012f, AngleAtX(c, top - 0.012f, xa, grow + 0.003f, fl, false), grow + 0.003f, fl);
            Vector3 p1 = c.TPt(1.40f, sx * 1.02f, 0.014f, 0f);
            Vector3 p2 = c.TPt(1.33f, sx * (Mathf.PI - 0.95f), 0.012f, 0f);
            Vector3 p3 = c.TPt(1.10f, sx * (Mathf.PI - 0.45f), grow, fl);
            b.Beam(trim, p0, p1, 0.020f, 0.007f, Vector3.forward);
            b.Beam(trim, p1, p2, 0.020f, 0.007f, Vector3.up);
            b.Beam(trim, p2, p3, 0.020f, 0.007f, Vector3.back);
        }
    }

    // =====================================================================
    // Tabard: dua panel (dada + punggung) berujung runcing, bingkai emas, lambang di dada
    // =====================================================================
    static void Tabard(GeoBag b, Ctx c, int slot, float top, float hem, float growBase = 0.034f, float flareCap = 1f)
    {
        float Wk = Mathf.Lerp(1f, c.W, 0.7f), fl = Mathf.Min(lastFlare, flareCap), hw = 0.112f * Wk;
        int trim = Sl.Gold;
        for (int sd = 0; sd < 2; sd++)
        {
            bool back = sd == 1;
            float hb = back ? 0.035f : 0f, pt = back ? 0.05f : 0.08f, g = growBase - (back ? 0.002f : 0f);
            System.Func<float, float> topAt = u => top - 0.012f * u * u;
            System.Func<float, float> hemAt = u => hem + hb + pt * Mathf.Abs(u);
            System.Func<float, float> half = y => hw * (1f + 0.18f * Mathf.Clamp01((top - y) / Mathf.Max(0.1f, top - hem)));
            System.Func<float, float> nhalf = y => -half(y);
            Panel(b, c, slot, back, topAt, hemAt, nhalf, half, g, fl, 10, 14, 0.003f);
            Panel(b, c, trim, back, u => hemAt(u) + 0.030f, hemAt, nhalf, half, g + 0.003f, fl, 10, 1);                                    // bingkai bawah
            Panel(b, c, trim, back, u => top, u => hemAt(1f), y => -half(y), y => -half(y) + 0.014f, g + 0.003f, fl, 1, 14);              // bingkai kiri
            Panel(b, c, trim, back, u => top, u => hemAt(1f), y => half(y) - 0.014f, y => half(y), g + 0.003f, fl, 1, 14);                // bingkai kanan
            if (!back)
            {
                float ey = top - 0.15f, ez = FrontZ(c, ey, g + 0.004f) + 0.003f, s1 = 0.040f * Wk;
                b.Quad(trim, new Vector3(-s1, c.Y(ey), ez), new Vector3(0f, c.Y(ey) - s1 * 1.35f, ez), new Vector3(s1, c.Y(ey), ez), new Vector3(0f, c.Y(ey) + s1 * 1.35f, ez));      // wajik emas
                float s2 = s1 * 0.55f; ez += 0.0015f;
                b.Quad(Sl.Dark, new Vector3(-s2, c.Y(ey), ez), new Vector3(0f, c.Y(ey) - s2 * 1.35f, ez), new Vector3(s2, c.Y(ey), ez), new Vector3(0f, c.Y(ey) + s2 * 1.35f, ez));
            }
        }
    }

    // =====================================================================
    // Jubah (cape) dan mantel bahu
    // =====================================================================
    static readonly float[] YokeY = { 1.468f, 1.474f, 1.462f, 1.425f, 1.350f };
    static readonly float[] YokeR = { 0.112f, 0.160f, 0.215f, 0.268f, 0.290f };

    /// <summary>Kain menjuntai berpenampang elips: baris bahu (YokeY/YokeR) lalu menjuntai ke hemAt(u); radius menjuntai = hangR(y, sudut).</summary>
    static void Drape(GeoBag b, Ctx c, int slot, float a0, float a1, System.Func<float, float> hemAt, System.Func<float, float, float> hangR,
        float rzK, int cols, int nHang, float zc, bool both, float yokeK)
    {
        int ny = YokeY.Length;
        System.Func<int, int, Vector3> pf = (r, j) =>
        {
            float u = j / (float)cols, a = Mathf.Lerp(a0, a1, u), y, R;
            if (r < ny) { y = YokeY[r]; R = YokeR[r] * yokeK; }
            else { float k = (r - ny + 1) / (float)nHang; y = Mathf.Lerp(YokeY[ny - 1], hemAt(u), k); R = hangR(y, a); }
            return new Vector3(Mathf.Sin(a) * R, c.Y(y), zc + Mathf.Cos(a) * R * rzK);
        };
        if (both) ShellGrid(b, slot, ny - 1 + nHang, cols, pf, 0.006f);          // kain bebas bertebal tipis: tepi mendapat garis tepi toon
        else SurfGrid(b, slot, ny - 1 + nHang, cols, pf, false);
    }

    static void HemBand(GeoBag b, Ctx c, int slot, float a0, float a1, System.Func<float, float> hemAt, System.Func<float, float, float> hangR,
        float rzK, int cols, float zc, float h, float push)
    {
        SurfGrid(b, slot, 1, cols, (r, j) =>
        {
            float u = j / (float)cols, a = Mathf.Lerp(a0, a1, u), y = hemAt(u) + (r == 0 ? h : 0f);
            float R = hangR(y, a) + push;
            return new Vector3(Mathf.Sin(a) * R, c.Y(y), zc + Mathf.Cos(a) * R * rzK);
        }, true);                                                            // pita tipis dua sisi: tetap utuh dari sudut kamera rendah
    }

    static void Cape(GeoBag b, Ctx c, int slot, float bottomY, float spread, bool collar)
    {
        float Wk = Mathf.Lerp(1f, c.W, 0.8f), R0 = 0.290f * Wk, yB = YokeY[YokeY.Length - 1];
        int trim = bottomY < 0.5f ? Sl.Gold : Sl.Leather;
        float a0 = 96f * Mathf.Deg2Rad, a1 = 264f * Mathf.Deg2Rad;
        System.Func<float, float> hemAt = u => bottomY + 0.07f * Mathf.Pow(Mathf.Abs(u * 2f - 1f), 1.6f) + 0.012f * Mathf.Cos(u * Mathf.PI * 2f * 3f);
        float fl0 = lastFlare;
        System.Func<float, float, float> hangR = (y, a) =>
        {
            float q = Mathf.Clamp01((yB - y) / Mathf.Max(0.1f, yB - bottomY));
            float pl = 1f + 0.07f * Mathf.Pow(q, 0.7f) * (Mathf.Cos(a * 7.3f + 0.3f) + 0.5f * Mathf.Cos(a * 13f + 1.7f));
            float R = (R0 + spread * 1.25f * q * q) * pl;
            if (fl0 > 0f)                       // jubah selalu di luar rok/jubah panjang yang mengembang di bawahnya
            {
                Vector3 pb = c.TPt(y, Mathf.PI, 0.03f, fl0);
                R = Mathf.Max(R, (Mathf.Abs(pb.z) + 0.05f) / 0.5f);
            }
            return R;
        };
        Drape(b, c, slot, a0, a1, hemAt, hangR, 0.5f, 28, 9, -0.012f, true, Wk);
        HemBand(b, c, trim, a0, a1, hemAt, hangR, 0.5f, 28, -0.012f, 0.032f, 0.003f);
        if (collar)       // kerah tinggi di belakang leher
        {
            float cr0 = 0.104f * Wk, cr1 = 0.135f * Wk;
            SurfGrid(b, slot, 3, 12, (r, j) =>
            {
                float u = j / 12f, a = Mathf.Lerp(Mathf.PI - 1.15f, Mathf.PI + 1.15f, u), k = r / 3f;
                float R = Mathf.Lerp(cr0, cr1, k);
                return new Vector3(Mathf.Sin(a) * R, c.Y(Mathf.Lerp(1.40f, 1.545f, k)), -0.004f + Mathf.Cos(a) * R * 0.9f);
            }, true);
        }
    }

    /// <summary>Mantel bahu pendek (penyihir, pendeta): menutup bahu dan lengan atas, depan terbuka, tepi bergerigi lembut.</summary>
    static void Mantle(GeoBag b, Ctx c, int slot, float yHem, float rHem, int trim)
    {
        float Wk = Mathf.Lerp(1f, c.W, 0.8f), yB = YokeY[YokeY.Length - 1];
        float a0 = 30f * Mathf.Deg2Rad, a1 = 330f * Mathf.Deg2Rad;
        System.Func<float, float> hemAt = u => yHem + 0.025f * Mathf.Pow(Mathf.Abs(Mathf.Cos(u * Mathf.PI * 5f)), 0.8f) - 0.03f * Mathf.Abs(u * 2f - 1f);
        System.Func<float, float, float> hangR = (y, a) =>
        {
            float q = Mathf.Clamp01((yB - y) / Mathf.Max(0.05f, yB - yHem));
            return (0.290f + (rHem - 0.290f) * q) * Wk * (1f + 0.03f * q * Mathf.Cos(a * 9f));
        };
        Drape(b, c, slot, a0, a1, hemAt, hangR, 0.80f, 40, 4, 0.0f, true, Wk);
        HemBand(b, c, trim, a0, a1, hemAt, hangR, 0.80f, 40, 0.0f, 0.026f, 0.003f);
    }

    /// <summary>Rompi/jerkin: panel depan berleher V + panel punggung; sisi badan dibiarkan terbuka sehingga kain di bawahnya terlihat. Tepi bergaris gelap dan tali di leher V.</summary>
    static void Vest(GeoBag b, Ctx c, int slot, int edgeSlot, float top, float hem, float halfX, float grow, float vDepth = 0.12f)
    {
        float fl = lastFlare, hx = halfX * Mathf.Lerp(1f, c.W, 0.7f);
        for (int bk = 0; bk < 2; bk++)
        {
            bool back = bk == 1;
            System.Func<float, float> topAt = pu => back ? top : top - vDepth * Mathf.Pow(1f - Mathf.Abs(pu), 1.3f);
            System.Func<float, float> hemAt = pu => hem + 0.03f * pu * pu;
            Panel(b, c, slot, back, topAt, hemAt, py => -hx, py => hx, grow, fl, 14, 9);
            Panel(b, c, edgeSlot, back, pu => hemAt(pu) + 0.022f, hemAt, py => -hx, py => hx, grow + 0.003f, fl, 14, 1);
            Panel(b, c, edgeSlot, back, pu => topAt(1f), pu => hemAt(1f), py => -hx, py => -hx + 0.012f, grow + 0.003f, fl, 1, 9);
            Panel(b, c, edgeSlot, back, pu => topAt(1f), pu => hemAt(1f), py => hx - 0.012f, py => hx, grow + 0.003f, fl, 1, 9);
        }
        for (int i = 0; i < 3; i++)        // tali silang di leher V
        {
            float ly = top - vDepth + 0.015f + i * 0.0f - i * 0.0f;
            float yy = top - 0.045f - i * 0.032f, ww = (vDepth - (top - yy)) < 0f ? 0f : 0f;
            float half = 0.012f + (yy - (top - vDepth)) * 0.9f; if (half < 0.012f) continue;
            b.Box(edgeSlot, new Vector3(0f, c.Y(yy), FrontZ(c, yy, grow) + 0.003f), new Vector3(Mathf.Min(half * 2f, 0.12f), 0.008f, 0.006f));
        }
    }

    /// <summary>Garis sambungan pelat: balok tipis tegak mengelilingi badan di antara tinggi y0 dan y1.</summary>
    static void SeamBeams(GeoBag b, Ctx c, int slot, float y0, float y1, float grow, float flare, int count, float phase)
    {
        for (int i = 0; i < count; i++)
        {
            float a = phase + i * Mathf.PI * 2f / count;
            Vector3 p0 = c.TPt(y0, a, grow, flare), p1 = c.TPt(y1, a, grow, flare);
            b.Beam(slot, p0, p1, 0.007f, 0.005f, new Vector3(p0.x, 0f, p0.z));
        }
    }

    static void QuadIn(GeoBag b, int slot, Vector3 a, Vector3 bb, Vector3 cc, Vector3 d)
    {
        Vector3 n = Vector3.Cross(bb - a, cc - a);
        Vector3 mid = (a + bb + cc + d) * 0.25f; mid.y = 0f;
        if (Vector3.Dot(n, mid) > 0f) b.Quad(slot, d, cc, bb, a); else b.Quad(slot, a, bb, cc, d);
    }

    static void EdgeQ(GeoBag b, int slot, Vector3 a, Vector3 bb, Vector3 cc, Vector3 d, Vector3 outward)
    {
        Vector3 n = Vector3.Cross(bb - a, cc - a);
        if (Vector3.Dot(n, outward) < 0f) b.Quad(slot, d, cc, bb, a); else b.Quad(slot, a, bb, cc, d);
    }

    /// <summary>Kain bebas bertebal tipis: sisi luar + sisi dalam + empat tepi, supaya garis tepi toon (hull terbalik) muncul di pinggiran kain.
    /// Arah tepi diambil dari kisi itu sendiri (dari baris/kolom dalam ke baris/kolom tepi), bukan dari pusat kain: di lubang leher tepi justru menghadap sumbu badan.</summary>
    static void ShellGrid(GeoBag b, int slot, int rows, int cols, System.Func<int, int, Vector3> pt, float thick)
    {
        var g = new Vector3[rows + 1][]; var h = new Vector3[rows + 1][]; var nv = new Vector3[rows + 1][];
        for (int r = 0; r <= rows; r++)
        {
            g[r] = new Vector3[cols + 1]; h[r] = new Vector3[cols + 1]; nv[r] = new Vector3[cols + 1];
            for (int j = 0; j <= cols; j++) g[r][j] = pt(r, j);
        }
        float side = 0f;                                                   // satu tanda global: kisi (r,j) berorientasi konsisten, jadi cukup diputuskan sekali
        for (int r = 0; r <= rows; r++)
            for (int j = 0; j <= cols; j++)
            {
                Vector3 du = g[r][Mathf.Min(cols, j + 1)] - g[r][Mathf.Max(0, j - 1)], dv = g[Mathf.Min(rows, r + 1)][j] - g[Mathf.Max(0, r - 1)][j];
                Vector3 n = Vector3.Cross(du, dv);
                n = n.sqrMagnitude > 1e-12f ? n.normalized : Vector3.zero;
                nv[r][j] = n;
                side += Vector3.Dot(n, new Vector3(g[r][j].x, 0f, g[r][j].z));
            }
        float sg = side < 0f ? -1f : 1f;
        for (int r = 0; r <= rows; r++)
            for (int j = 0; j <= cols; j++) { nv[r][j] = nv[r][j] * sg; h[r][j] = g[r][j] - nv[r][j] * thick; }
        for (int r = 0; r < rows; r++)
            for (int j = 0; j < cols; j++)
            {
                QuadOut(b, slot, g[r][j + 1], g[r][j], g[r + 1][j], g[r + 1][j + 1]);
                QuadIn(b, slot, h[r][j + 1], h[r][j], h[r + 1][j], h[r + 1][j + 1]);
            }
        for (int j = 0; j < cols; j++)
        {
            EdgeQ(b, slot, g[0][j], g[0][j + 1], h[0][j + 1], h[0][j], g[0][j] + g[0][j + 1] - g[1][j] - g[1][j + 1]);
            EdgeQ(b, slot, g[rows][j], g[rows][j + 1], h[rows][j + 1], h[rows][j], g[rows][j] + g[rows][j + 1] - g[rows - 1][j] - g[rows - 1][j + 1]);
        }
        for (int r = 0; r < rows; r++)
        {
            EdgeQ(b, slot, g[r][0], g[r + 1][0], h[r + 1][0], h[r][0], g[r][0] + g[r + 1][0] - g[r][1] - g[r + 1][1]);
            EdgeQ(b, slot, g[r][cols], g[r + 1][cols], h[r + 1][cols], h[r][cols], g[r][cols] + g[r + 1][cols] - g[r][cols - 1] - g[r + 1][cols - 1]);
        }
    }
}
