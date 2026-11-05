using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// v35z102n75 "Ashwatch Bukit Abu": kit aset Kota Ashwatch (DIBUAT BARU, tidak memakai FantasyKit / LandmarkKit / HouseKit lama).
/// Alat dasar di sini: jalur kayu tipis, jendela, pintu, toko + tenda, papan gantung, cerobong, fondasi batu, atap genteng BERUNDAK (pelana & limas).
/// Rumah lengkap: Town2Kit.House.cs. Bangunan penting: Town2Kit.Civic.cs / Town2Kit.Grand.cs. Tembok, gerbang, jembatan: Town2Kit.Wall.cs. Prop: Town2Kit.Props.cs.
/// Hanya memakai GeoBag/Sl/Vector/Mathf supaya bisa dipratinjau di luar Unity.
/// Konvensi: muka bangunan = +Z lokal, lebar = X lokal, alas lantai di y = 0.45 (permukaan tanah = y 0), pusat denah di titik asal.
/// </summary>
public static partial class T2Kit
{
    public const float BaseY = 0.45f;

    public static float Rnd(int seed, int k) { return V2Kit.Rnd(seed, k); }

    // ------------------------------------------------------------------ jalur & balok tipis di bidang dinding (ruang panel: x melintang, y atas, z keluar)
    /// <summary>Jalur kayu/batu menempel di dinding: muka depan + sisi atas (+ sisi kiri/kanan bila tegak ramping).</summary>
    public static void Strip(GeoBag b, int s, float x0, float x1, float y0, float y1, float z, float depth)
    {
        float zf = z + depth;
        b.Quad(s, new Vector3(x0, y0, zf), new Vector3(x1, y0, zf), new Vector3(x1, y1, zf), new Vector3(x0, y1, zf));
        b.Quad(s, new Vector3(x0, y1, zf), new Vector3(x1, y1, zf), new Vector3(x1, y1, z), new Vector3(x0, y1, z));
    }

    /// <summary>Balok miring (braket / sokong) pada bidang dinding.</summary>
    public static void Brace(GeoBag b, int s, float xa, float ya, float xb, float yb, float z, float w, float depth)
    {
        b.Beam(s, new Vector3(xa, ya, z + depth * 0.5f), new Vector3(xb, yb, z + depth * 0.5f), w, depth, new Vector3(0f, 0f, 1f));
    }

    /// <summary>Segi tiga gable di bidang z (alas y0, lebar 2*hw, puncak y0+rise), muka +Z; facing=-1 untuk muka -Z.</summary>
    public static void GableTri(GeoBag b, int s, float cx, float y0, float hw, float rise, float z, bool front)
    {
        if (front) b.Tri(s, new Vector3(cx - hw, y0, z), new Vector3(cx + hw, y0, z), new Vector3(cx, y0 + rise, z));
        else b.Tri(s, new Vector3(cx + hw, y0, z), new Vector3(cx - hw, y0, z), new Vector3(cx, y0 + rise, z));
    }

    // ------------------------------------------------------------------ jendela
    // style: 1 daun jendela (shutter), 2 ambang batu (bukan kayu), 4 kotak bunga, 8 kecil (loteng, tanpa palang), 16 kaca kecil berpetak (kisi timah)
    public static void Win(GeoBag b, float cx, float cy, float w, float h, int seed, int style)
    {
        float x0 = cx - w * 0.5f, x1 = cx + w * 0.5f, y0 = cy - h * 0.5f, y1 = cy + h * 0.5f;
        float f = 0.07f, zf = 0.06f;
        b.Quad(Sl.Glass, new Vector3(x0, y0, 0.02f), new Vector3(x1, y0, 0.02f), new Vector3(x1, y1, 0.02f), new Vector3(x0, y1, 0.02f));
        // kusen: empat bilah datar menempel di depan kaca
        b.Quad(Sl.Wood, new Vector3(x0 - f, y0 - f, zf), new Vector3(x0, y0 - f, zf), new Vector3(x0, y1 + f, zf), new Vector3(x0 - f, y1 + f, zf));
        b.Quad(Sl.Wood, new Vector3(x1, y0 - f, zf), new Vector3(x1 + f, y0 - f, zf), new Vector3(x1 + f, y1 + f, zf), new Vector3(x1, y1 + f, zf));
        b.Quad(Sl.Wood, new Vector3(x0, y1, zf), new Vector3(x1, y1, zf), new Vector3(x1, y1 + f, zf), new Vector3(x0, y1 + f, zf));
        b.Quad(Sl.Wood, new Vector3(x0, y0 - f, zf), new Vector3(x1, y0 - f, zf), new Vector3(x1, y0, zf), new Vector3(x0, y0, zf));
        if ((style & 8) == 0)
        {
            b.Quad(Sl.Wood, new Vector3(cx - 0.025f, y0, zf), new Vector3(cx + 0.025f, y0, zf), new Vector3(cx + 0.025f, y1, zf), new Vector3(cx - 0.025f, y1, zf));
            float ym = y0 + h * 0.6f;
            b.Quad(Sl.Wood, new Vector3(x0, ym - 0.025f, zf), new Vector3(x1, ym - 0.025f, zf), new Vector3(x1, ym + 0.025f, zf), new Vector3(x0, ym + 0.025f, zf));
        }
        // ambang bawah menjorok + kepala atas
        Strip(b, (style & 2) != 0 ? Sl.Stone : Sl.Wood, x0 - 0.13f, x1 + 0.13f, y0 - f - 0.08f, y0 - f, 0f, 0.18f);
        Strip(b, (style & 2) != 0 ? Sl.DarkStone : Sl.Wood, x0 - 0.15f, x1 + 0.15f, y1 + f, y1 + f + 0.12f, 0f, 0.12f);
        if ((style & 1) != 0)
        {
            float sw = w * 0.5f;
            for (int q = 0; q < 2; q++)
            {
                float xs0 = q == 0 ? x0 - f - sw : x1 + f, xs1 = q == 0 ? x0 - f : x1 + f + sw;
                b.Quad(Sl.WoodLight, new Vector3(xs0, y0, 0.04f), new Vector3(xs1, y0, 0.04f), new Vector3(xs1, y1, 0.04f), new Vector3(xs0, y1, 0.04f));
                float yl = y0 + h * 0.2f, yu = y1 - h * 0.24f;
                b.Quad(Sl.Wood, new Vector3(xs0, yl, 0.055f), new Vector3(xs1, yl, 0.055f), new Vector3(xs1, yl + 0.07f, 0.055f), new Vector3(xs0, yl + 0.07f, 0.055f));
                b.Quad(Sl.Wood, new Vector3(xs0, yu, 0.055f), new Vector3(xs1, yu, 0.055f), new Vector3(xs1, yu + 0.07f, 0.055f), new Vector3(xs0, yu + 0.07f, 0.055f));
            }
        }
        if ((style & 4) != 0)
        {
            b.Box(Sl.Wood, new Vector3(cx, y0 - f - 0.2f, 0.2f), new Vector3(w + 0.1f, 0.2f, 0.26f));
            b.Box(Sl.Hedge, new Vector3(cx, y0 - f - 0.04f, 0.2f), new Vector3(w, 0.1f, 0.2f));
            int nb = Mathf.Max(3, Mathf.RoundToInt(w / 0.24f));
            for (int k = 0; k < nb; k++)
                b.Ball(Sl.Blossom, new Vector3(x0 + (k + 0.5f) * w / nb, y0 - f + 0.02f + 0.04f * Rnd(seed, k), 0.2f + 0.04f * Rnd(seed, k + 9)), 0.065f, 3, 5);
        }
    }

    // ------------------------------------------------------------------ pintu: style 0 satu daun, 1 dua daun lebar (toko/gudang), 2 pintu besar lengkung (gedung)
    public static void DoorT(GeoBag b, float cx, float w, float h, int seed, int style, bool lantern)
    {
        float x0 = cx - w * 0.5f, x1 = cx + w * 0.5f;
        float j = 0.2f;
        b.Box(Sl.DarkStone, new Vector3(x0 - j * 0.5f, h * 0.5f, 0.08f), new Vector3(j, h, 0.2f));
        b.Box(Sl.DarkStone, new Vector3(x1 + j * 0.5f, h * 0.5f, 0.08f), new Vector3(j, h, 0.2f));
        b.Box(Sl.DarkStone, new Vector3(cx, h + 0.13f, 0.08f), new Vector3(w + 2f * j + 0.1f, 0.26f, 0.22f));
        b.Quad(Sl.Dark, new Vector3(x0, 0f, 0.015f), new Vector3(x1, 0f, 0.015f), new Vector3(x1, h, 0.015f), new Vector3(x0, h, 0.015f));
        int np = style == 1 ? 8 : 4;
        for (int k = 0; k < np; k++)
        {
            float a = x0 + k * w / np + 0.015f, c = x0 + (k + 1) * w / np - 0.015f;
            b.Quad(k % 2 == 0 ? Sl.Wood : Sl.WoodLight, new Vector3(a, 0f, 0.05f), new Vector3(c, 0f, 0.05f), new Vector3(c, h - 0.04f, 0.05f), new Vector3(a, h - 0.04f, 0.05f));
        }
        Strip(b, Sl.Metal, x0, x1, h * 0.22f, h * 0.22f + 0.07f, 0.05f, 0.025f);
        Strip(b, Sl.Metal, x0, x1, h * 0.72f, h * 0.72f + 0.07f, 0.05f, 0.025f);
        if (style == 1) Strip(b, Sl.Metal, cx - 0.025f, cx + 0.025f, 0f, h - 0.04f, 0.05f, 0.04f);
        else b.Ball(Sl.Metal, new Vector3(x1 - 0.2f, h * 0.48f, 0.1f), 0.045f, 3, 5);
        // ambang batu + undakan
        b.Box(Sl.Stone, new Vector3(cx, 0.03f, 0.3f), new Vector3(w + 0.7f, 0.09f, 0.62f));
        if (lantern)
        {
            float lx = x1 + j + 0.28f;
            b.Beam(Sl.Metal, new Vector3(lx - 0.14f, h * 0.9f, 0.1f), new Vector3(lx, h * 0.9f, 0.34f), 0.04f, 0.04f);
            b.Box(Sl.Metal, new Vector3(lx, h * 0.9f - 0.12f, 0.34f), new Vector3(0.16f, 0.22f, 0.16f));
            b.Box(Sl.Glow, new Vector3(lx, h * 0.9f - 0.12f, 0.34f), new Vector3(0.1f, 0.14f, 0.1f));
        }
    }

    // ------------------------------------------------------------------ tenda toko bergaris + kanopi tiang
    public static void Awning(GeoBag b, float cx, float w, float y, float drop, float reach, int sA, int sB)
    {
        int n = Mathf.Max(4, Mathf.RoundToInt(w / 0.55f));
        float x0 = cx - w * 0.5f, dw = w / n;
        for (int k = 0; k < n; k++)
        {
            int s = (k % 2 == 0) ? sA : sB;
            float a = x0 + k * dw, c = x0 + (k + 1) * dw;
            V2Kit.QUp(b, s, new Vector3(a, y, 0.04f), new Vector3(c, y, 0.04f), new Vector3(c, y - drop, reach), new Vector3(a, y - drop, reach));
            b.Quad(s, new Vector3(a, y - drop, reach), new Vector3(c, y - drop, reach), new Vector3(c, y - drop - 0.22f, reach + 0.03f), new Vector3(a, y - drop - 0.22f, reach + 0.03f));
            b.Quad(Sl.Dark, new Vector3(c, y - drop + 0.0f, reach - 0.02f), new Vector3(a, y - drop + 0.0f, reach - 0.02f), new Vector3(a, y - 0.0f, 0.06f), new Vector3(c, y - 0.0f, 0.06f));
        }
        foreach (float sg in new[] { -1f, 1f })
        {
            float ax = cx + sg * (w * 0.5f - 0.05f);
            b.Beam(Sl.Wood, new Vector3(ax, y - drop - 0.6f, 0.06f), new Vector3(ax, y - drop * 0.1f, reach * 0.95f), 0.08f, 0.08f);
        }
    }

    /// <summary>Papan gantung bertangkai besi di sisi dinding (di x = ax, tinggi y); dir = arah menonjol ke luar (z).</summary>
    public static void HangSign(GeoBag b, float ax, float y, float reach, int seed, int slotBoard)
    {
        b.Beam(Sl.Metal, new Vector3(ax, y, 0.04f), new Vector3(ax, y, reach), 0.05f, 0.05f);
        b.Beam(Sl.Metal, new Vector3(ax, y - 0.38f, 0.04f), new Vector3(ax, y - 0.02f, reach * 0.72f), 0.04f, 0.04f);
        float bx = ax;
        b.Box(Sl.Metal, new Vector3(bx - 0.19f, y - 0.14f, reach - 0.1f), new Vector3(0.02f, 0.28f, 0.02f));
        b.Box(Sl.Metal, new Vector3(bx + 0.19f, y - 0.14f, reach - 0.1f), new Vector3(0.02f, 0.28f, 0.02f));
        b.Box(Sl.Wood, new Vector3(bx, y - 0.54f, reach - 0.1f), new Vector3(0.6f, 0.42f, 0.06f));
        b.Box(slotBoard, new Vector3(bx, y - 0.54f, reach - 0.065f), new Vector3(0.46f, 0.3f, 0.02f));
        b.Ball(Sl.Gold, new Vector3(bx, y - 0.54f, reach - 0.04f), 0.07f, 3, 6);
    }

    // ------------------------------------------------------------------ cerobong batu dengan mahkota & dua periuk
    public static void Chimney(GeoBag b, Vector3 baseP, float w, float h, float yaw)
    {
        b.Push(baseP, yaw);
        b.Box(Sl.Stone, new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, w * 0.82f));
        b.Box(Sl.DarkStone, new Vector3(0f, h * 0.55f, 0f), new Vector3(w + 0.06f, 0.1f, w * 0.82f + 0.06f));
        b.Box(Sl.DarkStone, new Vector3(0f, h + 0.06f, 0f), new Vector3(w + 0.26f, 0.12f, w * 0.82f + 0.26f));
        b.Box(Sl.DarkStone, new Vector3(0f, h + 0.16f, 0f), new Vector3(w + 0.1f, 0.1f, w * 0.82f + 0.1f));
        b.Box(Sl.Dark, new Vector3(0f, h + 0.215f, 0f), new Vector3(w - 0.24f, 0.02f, w * 0.82f - 0.24f));
        b.Cyl(Sl.RoofB, new Vector3(-w * 0.24f, h + 0.21f, 0f), 0.11f, 0.38f, 6, 0.08f, true);
        b.Cyl(Sl.RoofB, new Vector3(w * 0.24f, h + 0.21f, 0f), 0.11f, 0.3f, 6, 0.08f, true);
        b.Pop();
    }

    // ------------------------------------------------------------------ fondasi batu (kaki bangunan), sedikit menjorok & berlapis
    public static void Plinth(GeoBag b, float w, float d, float zc, float top)
    {
        b.Box(Sl.Stone, new Vector3(0f, (top - 0.7f) * 0.5f, zc), new Vector3(w + 0.16f, top + 0.7f, d + 0.16f));
        b.Box(Sl.DarkStone, new Vector3(0f, top - 0.04f, zc), new Vector3(w + 0.26f, 0.1f, d + 0.26f));
    }

    // =====================================================================
    // ATAP GENTENG BERUNDAK
    // =====================================================================
    /// <summary>Tinggi profil atap pada jarak z dari punggung (0..hs): lereng lurus + tepi sedikit terangkat.</summary>
    static float Prof(float z, float hw, float hs, float rise, float kick)
    {
        float lin = rise - rise / hw * z;
        float t = Mathf.Clamp01(z / hs);
        float k = Mathf.Clamp01((t - 0.55f) / 0.45f);
        return lin + kick * k * k * (3f - 2f * k);
    }

    /// <summary>
    /// Atap pelana berundak. c = pusat di tinggi puncak dinding (tepi dinding), punggung sepanjang X lokal sepanjang len, melintang span.
    /// ov = tonjolan tepi (melintang), ovEnd = tonjolan ujung (sepanjang punggung). Tiap deret genteng menonjol di atas deret di bawahnya.
    /// gableFill = segitiga dinding gable di kedua ujung (x = ±len/2).
    /// </summary>
    public static void TileRoof(GeoBag b, int sRoof, int sRidge, int sWood, int sEnd, Vector3 c, float len, float span, float rise, float ov, float ovEnd, int seed, float rowH = 0.42f, bool gableFill = true)
    {
        b.Push(c, 0f);
        float hw = span * 0.5f, hs = hw + ov, L = len * 0.5f + ovEnd, xl = len * 0.5f;
        float kick = Mathf.Clamp(0.05f + 0.22f * ov, 0.05f, 0.3f);
        float slopeLen = Mathf.Sqrt(hs * hs + rise * rise);
        int R = Mathf.Max(4, Mathf.RoundToInt(slopeLen / rowH));
        var zs = new float[R + 1]; var ys = new float[R + 1];
        for (int i = 0; i <= R; i++) { zs[i] = hs * (1f - i / (float)R); ys[i] = Prof(zs[i], hw, hs, rise, kick) - 0.0f; }
        foreach (float sg in new[] { 1f, -1f })
        {
            for (int i = 0; i < R; i++)
            {
                Vector3 lo = new Vector3(0f, ys[i], sg * zs[i]), hi = new Vector3(0f, ys[i + 1], sg * zs[i + 1]);
                Vector3 tg = (hi - lo).normalized;
                Vector3 nr = new Vector3(0f, Mathf.Abs(tg.z), sg * Mathf.Abs(tg.y)).normalized;    // tegak lurus lereng, mengarah keluar-atas
                float th = 0.055f + 0.03f * Rnd(seed, i * 3 + (sg > 0 ? 1 : 0));
                Vector3 lo2 = lo + nr * th;
                // muka deret (tepi bawah terangkat, tepi atas masuk di bawah deret berikutnya)
                V2Kit.QUp(b, sRoof, new Vector3(-L, lo2.y, lo2.z), new Vector3(L, lo2.y, lo2.z), new Vector3(L, hi.y, hi.z), new Vector3(-L, hi.y, hi.z));
                // tepi tebal (riser) menghadap ke bawah-luar
                V2Kit.QOut(b, sRoof, new Vector3(-L, lo.y, lo.z), new Vector3(L, lo.y, lo.z), new Vector3(L, lo2.y, lo2.z), new Vector3(-L, lo2.y, lo2.z), new Vector3(0f, -tg.y * 0.3f, sg * 1f));
            }
            // papan tepi (verge) di kedua ujung mengikuti lereng (4 ruas cukup: lengkung hanya di tepi)
            {
                float[] tz = { 1f, 0.55f, 0.25f, 0f };
                for (int i = 0; i + 1 < tz.Length; i++)
                {
                    float za = hs * tz[i], zb = hs * tz[i + 1];
                    float ya = Prof(za, hw, hs, rise, kick), yb = Prof(zb, hw, hs, rise, kick);
                    foreach (float sx in new[] { -1f, 1f })
                    {
                        Vector3 a = new Vector3(sx * L, ya, sg * za), d = new Vector3(sx * L, yb, sg * zb);
                        V2Kit.QOut(b, sWood, a + new Vector3(0f, -0.2f, 0f), d + new Vector3(0f, -0.2f, 0f), d + new Vector3(0f, 0.04f, 0f), a + new Vector3(0f, 0.04f, 0f), new Vector3(sx, 0f, 0f));
                    }
                }
            }
            // fasia + soffit di tepi bawah
            float ye = ys[0], ze = sg * zs[0];
            V2Kit.QOut(b, sWood, new Vector3(-L, ye - 0.22f, ze), new Vector3(L, ye - 0.22f, ze), new Vector3(L, ye + 0.03f, ze), new Vector3(-L, ye + 0.03f, ze), new Vector3(0f, 0f, sg));
            V2Kit.QOut(b, sWood, new Vector3(-L, -0.03f, sg * hw), new Vector3(L, -0.03f, sg * hw), new Vector3(L, ye - 0.2f, ze), new Vector3(-L, ye - 0.2f, ze), new Vector3(0f, -1f, 0f));
        }
        // punggung: balok + tutup genteng
        b.Box(sRidge, new Vector3(0f, rise + 0.07f, 0f), new Vector3(L * 2f + 0.08f, 0.2f, 0.36f));
        b.Box(sWood, new Vector3(0f, rise + 0.2f, 0f), new Vector3(L * 2f + 0.2f, 0.08f, 0.16f));
        if (gableFill)
        {
            b.Tri(sEnd, new Vector3(xl, 0f, hw), new Vector3(xl, 0f, -hw), new Vector3(xl, rise, 0f));
            b.Tri(sEnd, new Vector3(-xl, 0f, -hw), new Vector3(-xl, 0f, hw), new Vector3(-xl, rise, 0f));
        }
        b.Pop();
    }

    /// <summary>Segitiga gable di kedua ujung punggung (x = ±len/2) untuk atap pelana yang sudah dibuat TileRoof(gableFill:false), sebelum dirotasi.</summary>
    public static void RoofEnds(GeoBag b, int sEnd, Vector3 c, float len, float span, float rise)
    {
        b.Push(c, 0f);
        float hw = span * 0.5f, xl = len * 0.5f;
        b.Tri(sEnd, new Vector3(xl, 0f, hw), new Vector3(xl, 0f, -hw), new Vector3(xl, rise, 0f));
        b.Tri(sEnd, new Vector3(-xl, 0f, -hw), new Vector3(-xl, 0f, hw), new Vector3(-xl, rise, 0f));
        b.Pop();
    }

    /// <summary>
    /// Atap limas berundak (empat lereng; punggung pendek bila len &gt; span). c = pusat di tinggi tepi dinding.
    /// </summary>
    public static void HipRoof(GeoBag b, int sRoof, int sRidge, int sWood, Vector3 c, float len, float span, float rise, float ov, int seed, float rowH = 0.45f)
    {
        b.Push(c, 0f);
        float hw = span * 0.5f, hl = len * 0.5f, hs = hw + ov, hL = hl + ov;
        float rl = Mathf.Max(0f, hl - hw);
        float kick = Mathf.Clamp(0.05f + 0.2f * ov, 0.05f, 0.28f);
        float slopeLen = Mathf.Sqrt(hs * hs + rise * rise);
        int R = Mathf.Max(4, Mathf.RoundToInt(slopeLen / rowH));
        var hx = new float[R + 1]; var hz = new float[R + 1]; var hy = new float[R + 1];
        for (int i = 0; i <= R; i++)
        {
            float u = i / (float)R;
            hz[i] = hs * (1f - u);
            hx[i] = Mathf.Lerp(hL, rl, u);
            hy[i] = Prof(hz[i], hw, hs, rise, kick);
        }
        for (int i = 0; i < R; i++)
        {
            float th = 0.055f + 0.03f * Rnd(seed, i);
            for (int side = 0; side < 4; side++)
            {
                // sisi 0: +Z, 1: -Z, 2: +X, 3: -X
                Vector3 a0, a1, b0, b1, outw;
                if (side < 2)
                {
                    float sg = side == 0 ? 1f : -1f;
                    a0 = new Vector3(-hx[i], hy[i], sg * hz[i]); a1 = new Vector3(hx[i], hy[i], sg * hz[i]);
                    b0 = new Vector3(-hx[i + 1], hy[i + 1], sg * hz[i + 1]); b1 = new Vector3(hx[i + 1], hy[i + 1], sg * hz[i + 1]);
                    outw = new Vector3(0f, 0.6f, sg);
                }
                else
                {
                    float sg = side == 2 ? 1f : -1f;
                    a0 = new Vector3(sg * hx[i], hy[i], -hz[i]); a1 = new Vector3(sg * hx[i], hy[i], hz[i]);
                    b0 = new Vector3(sg * hx[i + 1], hy[i + 1], -hz[i + 1]); b1 = new Vector3(sg * hx[i + 1], hy[i + 1], hz[i + 1]);
                    outw = new Vector3(sg, 0.6f, 0f);
                }
                Vector3 raise = new Vector3(0f, th, 0f);
                V2Kit.QOut(b, sRoof, a0 + raise, a1 + raise, b1, b0, outw);
                V2Kit.QOut(b, sRoof, a0, a1, a1 + raise, a0 + raise, new Vector3(outw.x, -0.4f, outw.z));
            }
        }
        // fasia & soffit keliling
        float ye = hy[0];
        Vector3[] ring = { new Vector3(-hL, ye, hs), new Vector3(hL, ye, hs), new Vector3(hL, ye, -hs), new Vector3(-hL, ye, -hs) };
        for (int k = 0; k < 4; k++)
        {
            Vector3 p = ring[k], q = ring[(k + 1) % 4];
            Vector3 mid = (p + q) * 0.5f; Vector3 ow = new Vector3(mid.x, 0f, mid.z).normalized;
            V2Kit.QOut(b, sWood, p + new Vector3(0f, -0.22f, 0f), q + new Vector3(0f, -0.22f, 0f), q + new Vector3(0f, 0.03f, 0f), p + new Vector3(0f, 0.03f, 0f), ow);
        }
        V2Kit.QOut(b, sWood, new Vector3(-hL, ye - 0.2f, -hs), new Vector3(hL, ye - 0.2f, -hs), new Vector3(hL, ye - 0.2f, hs), new Vector3(-hL, ye - 0.2f, hs), new Vector3(0f, -1f, 0f));
        if (rl > 0.1f)
        {
            b.Box(sRidge, new Vector3(0f, rise + 0.06f, 0f), new Vector3(rl * 2f + 0.5f, 0.2f, 0.34f));
        }
        else b.Cyl(sRidge, new Vector3(0f, rise - 0.02f, 0f), 0.22f, 0.3f, 6, 0.05f, true);
        b.Pop();
    }

    /// <summary>Lukarne pelana di lereng depan (+Z) atap pelana yang punggungnya sepanjang X. c = pusat atap (tepi dinding).</summary>
    public static void Dormer(GeoBag b, int sRoof, int sWood, int sWall, Vector3 roofC, float x, float zf, float hw, float rise, float w, float wallH, int seed)
    {
        // y permukaan atap di zf (lereng lurus)
        float yf = rise - rise / hw * zf;
        float yt = yf + wallH;
        float zb = (rise - yt - 0.15f) / (rise / hw);                      // z tempat lereng setinggi bidang atas lukarne
        zb = Mathf.Max(0.05f, zb);
        float dr = w * 0.55f;
        b.Push(roofC + new Vector3(x, 0f, 0f), 0f);
        float hx = w * 0.5f;
        // dinding depan + jendela
        b.Quad(sWall, new Vector3(-hx, yf - 0.35f, zf), new Vector3(hx, yf - 0.35f, zf), new Vector3(hx, yt, zf), new Vector3(-hx, yt, zf));
        b.Tri(sWall, new Vector3(-hx, yt, zf), new Vector3(hx, yt, zf), new Vector3(0f, yt + dr, zf));
        b.Push(new Vector3(0f, yf + 0.18f, zf), 0f);
        Win(b, 0f, wallH * 0.48f, w * 0.52f, wallH * 0.7f, seed, 8 | 16);
        b.Pop();
        // pipi
        foreach (float sg in new[] { -1f, 1f })
        {
            float xs = sg * hx;
            V2Kit.QOut(b, sWall, new Vector3(xs, yf - 0.35f, zf), new Vector3(xs, yt, zf), new Vector3(xs, yt, zb), new Vector3(xs, yf - 0.35f, zb + 0.05f), new Vector3(sg, 0f, 0f));
        }
        // atap pelana kecil (punggung sepanjang Z)
        float ov = 0.22f;
        foreach (float sg in new[] { -1f, 1f })
        {
            Vector3 e0 = new Vector3(sg * (hx + ov), yt - ov * dr / hx, zf + 0.28f), e1 = new Vector3(sg * (hx + ov), yt - ov * dr / hx, zb);
            Vector3 r0 = new Vector3(0f, yt + dr + 0.05f, zf + 0.28f), r1 = new Vector3(0f, yt + dr + 0.05f, zb);
            V2Kit.QUp(b, sRoof, e0, e1, r1, r0);
            V2Kit.QOut(b, sWood, e0 + new Vector3(0f, -0.14f, 0f), e0, r0, r0 + new Vector3(0f, -0.14f, 0f), new Vector3(0f, 0f, 1f));
        }
        b.Box(sWood, new Vector3(0f, yt + dr + 0.1f, (zf + 0.28f + zb) * 0.5f), new Vector3(0.14f, 0.1f, zf + 0.28f - zb));
        b.Pop();
    }
}
