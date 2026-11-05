using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kepala bergaya anime-isekai untuk NpcKit: mata besar berlapis (garis kelopak, sklera, iris bergradasi, pupil, kilau), alis, hidung kecil,
/// mulut, pipi merona, dan rambut berlapis (tudung tipis + helai runcing + massa belakang bergerigi) dengan 12 gaya.
/// Dipanggil dari BuildHead (NpcKit.Head.cs). Semua geometri memakai ruang kepala (hr = 0.24, kemudian diskalakan hk).
/// </summary>
public static partial class NpcKit
{
    /// <summary>true = kembali ke wajah/rambut lama (cadangan bila ada masalah).</summary>
    public static bool ClassicHead = false;

    // slot khusus karakter: memakai ulang indeks slot bangunan yang tidak dipakai NpcKit
    const int SHairHi = Sl.Plaster, SHairLo = Sl.Roof, SIrisHi = Sl.Marble, SIrisLo = Sl.Cobble, SBlush = Sl.Dirt, SSkinLo = Sl.Grass;

    static Color Mix(Color a, Color b, float t) { return new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, 1f); }
    static Color Shade(Color c, float k) { return new Color(c.r * k, c.g * k, c.b * k, 1f); }

    /// <summary>Hash stabil dari bagian-bagian Spec (bukan nama), supaya arketipe yang di-cache selalu sama.</summary>
    static int SpecHash(Spec s)
    {
        string k = s.Key(); int h = 17;
        for (int i = 0; i < k.Length; i++) h = h * 31 + k[i];
        return h & 0x7fffffff;
    }

    // ------------------------------------------------------------------ geometri bantu
    /// <summary>Debug (harness uji): dipanggil saat sebuah loft ternyata menghadap ke dalam dan dibalik.</summary>
    public static System.Action<string> LoftLog;

    /// <summary>
    /// Loft yang selalu menghadap keluar: GeoBag.Loft menghadap keluar hanya bila cincin berurutan bawah -> atas. Bila urutannya terbalik
    /// (mis. tudung rambut dari ubun-ubun ke bawah) permukaan menghadap ke dalam dan hilang dari sisi luar (backface culling) — di sini urutan dibalik otomatis.
    /// </summary>
    static void NLoft(GeoBag b, int slot, IList<Vector3[]> rings, bool capB, bool capT, bool smooth = true)
    {
        int R = rings.Count; if (R < 2) return;
        int n = rings[0].Length; float score = 0f;
        for (int i = 0; i < R; i++)
        {
            Vector3 cen = Vector3.zero;
            for (int j = 0; j < n; j++) cen += rings[i][j];
            cen = cen / n;
            for (int j = 0; j < n; j++)
            {
                Vector3 du = rings[i][(j + 1) % n] - rings[i][(j + n - 1) % n];
                Vector3 dv = rings[Mathf.Min(R - 1, i + 1)][j] - rings[Mathf.Max(0, i - 1)][j];
                Vector3 nm = Vector3.Cross(du, dv);
                if (nm.sqrMagnitude < 1e-14f) continue;
                score += Vector3.Dot(nm.normalized, rings[i][j] - cen);
            }
        }
        // Loft hanya benar bila ditumpuk ke atas dan dinding menghadap luar (tutup bawah = turun, tutup atas = naik). Perbaiki dua hal itu terpisah:
        Vector3 c0 = Vector3.zero, c1 = Vector3.zero;
        for (int j = 0; j < n; j++) { c0 += rings[0][j]; c1 += rings[R - 1][j]; }
        bool down = (c1.y - c0.y) < -1e-5f * n, inward = score < 0f;
        if (!down && !inward) { b.Loft(slot, rings, capB, capT, smooth); return; }
        bool flipVerts = inward != down;                                   // tumpukan turun: membaliknya sudah membalik dinding; ke atas tapi menghadap dalam: balik titik tiap cincin
        var rv = new List<Vector3[]>(R);
        for (int i = 0; i < R; i++)
        {
            var src = rings[down ? R - 1 - i : i];
            var q = new Vector3[n];
            for (int j = 0; j < n; j++) q[j] = flipVerts ? src[n - 1 - j] : src[j];
            rv.Add(q);
        }
        if (LoftLog != null) LoftLog("slot=" + slot + " rings=" + R + " y0=" + rings[0][0].y.ToString("0.000") + " y1=" + rings[R - 1][0].y.ToString("0.000"));
        if (down) b.Loft(slot, rv, capT, capB, smooth); else b.Loft(slot, rv, capB, capT, smooth);
    }

    /// <summary>Tabung pipih yang menyapu lintasan (titik pusat), lebar w dan tebal t per titik; ujung dibuat meruncing, tanpa tutup.</summary>
    static void Sweep(GeoBag b, int slot, Vector3[] pts, float[] w, float[] t, Vector3 hint, int n = 8, int sub = 2)
    {
        int m = pts.Length; if (m < 2) return;
        var cs = new List<Vector3>(); var ws = new List<float>(); var ts = new List<float>();
        for (int i = 0; i < m - 1; i++)
            for (int k = 0; k < sub; k++)
            {
                float u = k / (float)sub; int i0 = Mathf.Max(0, i - 1), i3 = Mathf.Min(m - 1, i + 2);
                cs.Add(CR(pts[i0], pts[i], pts[i + 1], pts[i3], u));
                ws.Add(Mathf.Max(0.0008f, CRf(w[i0], w[i], w[i + 1], w[i3], u)));
                ts.Add(Mathf.Max(0.0008f, CRf(t[i0], t[i], t[i + 1], t[i3], u)));
            }
        cs.Add(pts[m - 1]); ws.Add(Mathf.Max(0.0008f, w[m - 1])); ts.Add(Mathf.Max(0.0008f, t[m - 1]));
        var rings = new List<Vector3[]>(); Vector3 prevS = Vector3.right;
        for (int i = 0; i < cs.Count; i++)
        {
            Vector3 T = i == 0 ? cs[1] - cs[0] : (i == cs.Count - 1 ? cs[i] - cs[i - 1] : cs[i + 1] - cs[i - 1]);
            T = T.normalized; if (T.sqrMagnitude < 0.5f) T = Vector3.down;
            Vector3 S = hint - T * Vector3.Dot(hint, T);
            if (S.sqrMagnitude < 1e-5f) S = prevS - T * Vector3.Dot(prevS, T);
            if (S.sqrMagnitude < 1e-5f) S = Vector3.Cross(T, Vector3.up);
            S = S.normalized; prevS = S;
            Vector3 N = Vector3.Cross(S, T).normalized;
            var r = new Vector3[n];
            for (int j = 0; j < n; j++) { float a = j * Mathf.PI * 2f / n; r[j] = cs[i] + S * (Mathf.Sin(a) * ws[i]) + N * (Mathf.Cos(a) * ts[i]); }
            rings.Add(r);
        }
        NLoft(b, slot, rings, false, false);
    }

    /// <summary>Permukaan terbuka dari grid titik rows[baris][kolom]; baris bergerak ke bawah, kolom ke arah theta menurun (bagian belakang menghadap -Z).</summary>
    static void GridSurf(GeoBag b, int slot, List<Vector3[]> rows)
    {
        for (int r = 0; r + 1 < rows.Count; r++)
            for (int j = 0; j + 1 < rows[r].Length; j++)
                b.Quad(slot, rows[r][j + 1], rows[r][j], rows[r + 1][j], rows[r + 1][j + 1]);
    }

    /// <summary>Satu helai rambut meruncing di permukaan kepala: dari (phi0,th0) ke (phi1,th1) lewat lengkungan, lebar w0 -> ujung.</summary>
    static void HLock(GeoBag b, Ctx c, int slot, float phi0, float th0, float phi1, float th1, float grow0, float grow1, float w0, float t0, float flick = 0f)
    {
        var p = new Vector3[4]; var w = new float[4]; var t = new float[4];
        for (int i = 0; i < 4; i++)
        {
            float u = i / 3f;
            float ph = Mathf.Lerp(phi0, phi1, u), th = Mathf.Lerp(th0, th1, u) + flick * u * u;
            float gr = Mathf.Lerp(grow0, grow1, u);
            p[i] = HP(c, ph, th, gr);
            float tp = 1f - u; w[i] = w0 * (0.15f + 0.85f * Mathf.Pow(tp, 0.8f)); t[i] = t0 * (0.35f + 0.65f * tp);
        }
        w[3] = 0.0012f; t[3] = 0.0012f;
        Sweep(b, slot, p, w, t, Vector3.right, 6, 2);
    }

    // ------------------------------------------------------------------ wajah
    static void AnimeFace(GeoBag b, Ctx c)
    {
        Spec s = c.s; float hr = c.hr; bool fem = c.fem; int hsh = SpecHash(s);
        float ew = hr * (fem ? 0.135f : 0.122f), eh = hr * (fem ? 0.205f : 0.178f);
        float tilt = s.race == Elf ? 9f : (fem ? 3f : 0f);
        if (s.race == Dwarf) { ew *= 0.86f; eh *= 0.84f; }
        if (s.race == Beast) { ew *= 1.04f; eh *= 1.02f; }
        float ex = hr * (s.race == Dwarf ? 0.36f : 0.345f), ey = -hr * 0.04f;
        bool serious = s.outfit == OPenjaga || s.outfit == OKsatria || s.outfit == OPandai || (s.outfit == OHunter && (hsh & 1) == 0);
        for (int sg = -1; sg <= 1; sg += 2)
        {
            float tr = sg * tilt;
            SurfEllipse(b, c, SSkinLo, sg * ex, ey + eh * 0.34f, ew * 1.12f, eh * 0.92f, tr, 0.0030f, 16);                  // lipatan kelopak
            SurfEllipse(b, c, Sl.Dark, sg * ex, ey + eh * 0.06f, ew * 1.05f, eh * 1.05f, tr, 0.0036f, 18);                   // garis kelopak atas (tebal) + tepi
            SurfEllipse(b, c, Sl.EyeW, sg * ex, ey - eh * 0.05f, ew * 0.94f, eh * 0.89f, tr, 0.0041f, 16);                   // sklera
            SurfEllipse(b, c, Sl.Eye, sg * ex, ey - eh * 0.07f, ew * 0.80f, eh * 0.86f, 0f, 0.0047f, 18);                    // iris
            SurfEllipse(b, c, SIrisLo, sg * ex, ey + eh * 0.36f, ew * 0.68f, eh * 0.40f, 0f, 0.0052f, 14);                  // iris atas lebih gelap
            SurfEllipse(b, c, SIrisHi, sg * ex, ey - eh * 0.52f, ew * 0.56f, eh * 0.24f, 0f, 0.0052f, 12);                  // pantulan bawah
            SurfEllipse(b, c, Sl.Dark, sg * ex, ey - eh * 0.05f, ew * 0.30f, eh * 0.47f, 0f, 0.0058f, 12);                  // pupil
            SurfEllipse(b, c, Sl.EyeW, sg * ex + ew * 0.28f, ey + eh * 0.40f, ew * 0.27f, eh * 0.20f, 0f, 0.0064f, 10);      // kilau besar
            SurfEllipse(b, c, Sl.EyeW, sg * ex - ew * 0.30f, ey - eh * 0.36f, ew * 0.12f, eh * 0.09f, 0f, 0.0064f, 8);       // kilau kecil
            if (fem)
            {
                SurfEllipse(b, c, Sl.Dark, sg * (ex + ew * 1.08f), ey + eh * 0.46f, ew * 0.50f, eh * 0.075f, sg * 24f, 0.0050f, 8);      // bulu mata luar
                SurfEllipse(b, c, Sl.Dark, sg * (ex + ew * 0.95f), ey - eh * 0.46f, ew * 0.32f, eh * 0.05f, sg * -20f, 0.0050f, 8);
            }
            float browY = ey + eh * (fem ? 1.80f : 1.62f), browTilt = sg * (s.race == Elf ? 10f : (serious ? -8f : (fem ? 5f : 2f)));
            float byw; Vector3 br = SurfaceAt(c, sg * ex * 1.03f, browY, 0.0052f, out byw);
            b.Push(br, byw);
            b.Box(SHairLo, Vector3.zero, new Vector3(ew * (fem ? 1.9f : 2.25f), hr * (fem ? 0.030f : 0.046f), 0.006f), browTilt);
            b.Pop();
        }
        // hidung
        float nyaw; Vector3 np = SurfaceAt(c, 0f, -hr * 0.22f, 0f, out nyaw);
        if (s.race == Beast && s.beast != 3)
        {
            b.Push(np + new Vector3(0f, hr * 0.02f, 0.006f), 0f); b.Ball(Sl.Dark, new Vector3(0f, -hr * 0.04f, 0.004f), hr * 0.062f, 4, 8, 0.8f); b.Pop();
        }
        else
        {
            float nr = hr * 0.10f;
            if (s.race == Dwarf) Sphere(b, Sl.SkinS, np + new Vector3(0f, -hr * 0.05f, nr * 0.55f), nr * 0.8f, nr * 1.0f, nr * 1.0f, 4, 10);
            SurfEllipse(b, c, SSkinLo, 0f, -hr * 0.275f, hr * (s.race == Dwarf ? 0.075f : 0.042f), hr * 0.020f, 0f, 0.0040f, 8);
        }
        // mulut: senyum kecil (atau datar / terbuka)
        int expr = serious ? 1 : (hsh >> 3) % 4;                   // 0,2 senyum  1 datar  3 terbuka
        float mw = hr * 0.30f;
        if (expr == 3 && s.race != Dwarf)
        {
            SurfEllipse(b, c, Sl.Dark, 0f, -hr * 0.565f, hr * 0.095f, hr * 0.062f, 0f, 0.0040f, 12);
            SurfEllipse(b, c, Sl.Lip, 0f, -hr * 0.595f, hr * 0.055f, hr * 0.028f, 0f, 0.0046f, 8);
            SurfEllipse(b, c, Sl.EyeW, 0f, -hr * 0.525f, hr * 0.075f, hr * 0.016f, 0f, 0.0046f, 8);
        }
        else
            for (int i = -3; i <= 3; i++)
            {
                float t = i / 3f, yw;
                float curve = expr == 1 ? 0.012f : 0.055f;
                Vector3 mp = SurfaceAt(c, t * mw * 0.5f, -hr * (0.545f + curve * (1f - t * t)), 0.0036f, out yw);
                b.Push(mp, yw); b.Box(Sl.Dark, Vector3.zero, new Vector3(hr * 0.062f, hr * (fem ? 0.020f : 0.024f), 0.004f), t * (expr == 1 ? 6f : 26f)); b.Pop();
            }
        if (fem) SurfEllipse(b, c, Sl.Lip, 0f, -hr * 0.585f, hr * 0.10f, hr * 0.022f, 0f, 0.0034f, 10);
        // pipi merona
        for (int sg = -1; sg <= 1; sg += 2)
            SurfEllipse(b, c, SBlush, sg * hr * 0.56f, -hr * 0.27f, hr * (fem ? 0.15f : 0.10f), hr * (fem ? 0.072f : 0.048f), sg * 6f, 0.0032f, 10);
        if (!fem && s.beard > 0 && s.race != Beast)
            for (int sg = -1; sg <= 1; sg += 2)
            {
                float yw; Vector3 mm = SurfaceAt(c, sg * hr * 0.11f, -hr * 0.40f, 0.008f, out yw);
                b.Push(mm, yw); b.Ball(Sl.Hair, new Vector3(sg * hr * 0.07f, 0f, 0.006f), hr * 0.10f, 3, 8, 0.6f); b.Pop();
            }
        if (s.race == Beast && s.beast != 3)
            for (int sg = -1; sg <= 1; sg += 2)
                for (int i = 0; i < 3; i++)
                {
                    float yw; Vector3 wp = SurfaceAt(c, sg * hr * 0.32f, -hr * (0.34f + i * 0.05f), 0f, out yw);
                    b.Push(wp, yw); b.Box(Sl.EyeW, new Vector3(sg * hr * 0.2f, 0f, 0.02f), new Vector3(hr * 0.5f, 0.003f, 0.003f), sg * (i - 1) * -12f); b.Pop();
                }
    }

    // ------------------------------------------------------------------ rambut
    /// <summary>Tudung rambut dengan tepi bergerigi (zig = sudut gerigi dalam derajat).</summary>
    static void HairCap(GeoBag b, Ctx c, int slot, float grow, float botFront, float botBack, float botSide, float sweep, float zig, int K = 9)
    {
        var rings = new List<Vector3[]>(); const int N = 28;
        for (int k = 0; k <= K; k++)
        {
            float t = k / (float)K; var r = new Vector3[N];
            for (int j = 0; j < N; j++)
            {
                float th = j * (360f / N), cth = Mathf.Cos(th * Mathf.Deg2Rad), sth = Mathf.Sin(th * Mathf.Deg2Rad);
                float front = botFront + sweep * sth;
                float bot = cth > 0f ? Mathf.Lerp(botSide, front, Mathf.Pow(cth, 0.8f)) : Mathf.Lerp(botSide, botBack, Mathf.Pow(-cth, 0.8f));
                float zg = (j % 2 == 0 ? 0f : -zig) * Mathf.Clamp01((-cth + 0.35f) * 1.2f);      // gerigi hanya di sisi/belakang
                float ph = Mathf.Lerp(90f, bot + zg, Mathf.Pow(t, 0.9f));
                r[j] = HP(c, ph, th, grow * (0.85f + 0.15f * Mathf.Sin(t * Mathf.PI * 0.5f)));
            }
            rings.Add(r);
        }
        NLoft(b, slot, rings, false, false);
    }

    /// <summary>Tirai rambut belakang (busur terbuka dari sisi ke sisi lewat belakang) dengan ujung bergerigi.</summary>
    static void HairCurtain(GeoBag b, Ctx c, int slot, float yTop, float yBot, float zig, float width, float depth, float bulge, int cols = 17, int rows = 7, float th0 = 62f, float th1 = 298f, float tuck = 0.0f)
    {
        var rs = new List<Vector3[]>(); float hr = c.hr;
        for (int r = 0; r <= rows; r++)
        {
            float t = r / (float)rows; var row = new Vector3[cols + 1];
            for (int j = 0; j <= cols; j++)
            {
                float u = j / (float)cols; float th = Mathf.Lerp(th1, th0, u);
                float yb = yBot - ((j % 2 == 0) ? 0f : zig) * (0.4f + 0.6f * Mathf.Sin(u * Mathf.PI));
                float y = Mathf.Lerp(yTop, yb, t);
                float prof = 1f + bulge * Mathf.Sin(Mathf.Clamp01(t * 1.15f) * Mathf.PI * 0.9f) - tuck * t * t;
                float sth = Mathf.Sin(th * Mathf.Deg2Rad), cth = Mathf.Cos(th * Mathf.Deg2Rad);
                row[j] = new Vector3(sth * width * prof, y, cth * depth * prof - 0.006f);
            }
            rs.Add(row);
        }
        GridSurf(b, slot, rs);
    }


    /// <summary>Helai rambut yang mengikuti permukaan tirai belakang (supaya ujung rambut berlapis dan runcing).</summary>
    static void CurtainStrands(GeoBag b, Ctx c, int slot, int slotHi, float yTop, float yBot, float width, float depth, float bulge, float tuck, int count, float th0, float th1, float zig)
    {
        float arc = (th1 - th0) * Mathf.Deg2Rad * (width + depth) * 0.5f; float w0 = arc / count * 0.66f; float off = 0.012f;
        for (int i = 0; i < count; i++)
        {
            float u = (i + 0.5f) / count; float th = Mathf.Lerp(th1, th0, u);
            float yb = yBot - ((i % 3 == 0) ? 0f : (i % 3 == 1 ? zig : zig * 0.5f)) * (0.5f + 0.5f * Mathf.Sin(u * Mathf.PI));
            var pts = new Vector3[6]; var ws = new float[6]; var ts = new float[6];
            float sth = Mathf.Sin(th * Mathf.Deg2Rad), cth = Mathf.Cos(th * Mathf.Deg2Rad);
            for (int k = 0; k < 6; k++)
            {
                float t = k / 5f; float y = Mathf.Lerp(yTop, yb, t);
                float prof = 1f + bulge * Mathf.Sin(Mathf.Clamp01(t * 1.15f) * Mathf.PI * 0.9f) - tuck * t * t;
                pts[k] = new Vector3(sth * (width * prof + off), y, cth * (depth * prof + off) - 0.006f);
                float tp = 1f - t; ws[k] = k == 5 ? 0.0012f : w0 * (0.40f + 0.60f * Mathf.Pow(tp, 0.55f)); ts[k] = k == 5 ? 0.0012f : 0.012f * (0.5f + 0.5f * tp);
            }
            Sweep(b, (i % 4 == 1) ? slotHi : slot, pts, ws, ts, new Vector3(cth, 0f, -sth), 6, 2);
        }
    }

    static void CurtainSet(GeoBag b, Ctx c, float yTop, float yBot, float zig, float width, float depth, float bulge, float tuck, int count, float th0, float th1)
    {
        HairCurtain(b, c, SHairLo, yTop, yBot + 0.03f, zig * 0.5f, width * 0.985f, depth * 0.985f, bulge, 21, 7, th0, th1, tuck);
        CurtainStrands(b, c, Sl.Hair, SHairHi, yTop, yBot, width, depth, bulge, tuck, count, th0 + 7f, th1 - 7f, zig);
    }

    static void HairHalo(GeoBag b, Ctx c, float grow)
    {
        // pita kilau lembut melintang di atas kepala (efek rambut anime)
        var rg = new List<Vector3[]> { new Vector3[15], new Vector3[15] };
        for (int j = 0; j < 15; j++) { float th = -78f + j * 11.14f; rg[0][j] = HP(c, 53f + 3f * Mathf.Cos(th * Mathf.Deg2Rad), th, grow); rg[1][j] = HP(c, 61f + 3f * Mathf.Cos(th * Mathf.Deg2Rad), th, grow); }
        // loft menutup cincin; buat quad manual agar terbuka
        var rows = new List<Vector3[]> { rg[1], rg[0] };
        for (int j = 0; j + 1 < 15; j++) b.Quad(SHairHi, rows[0][j + 1], rows[0][j], rows[1][j], rows[1][j + 1]);
    }

    static void AnimeHair(GeoBag b, Ctx c)
    {
        Spec s = c.s; float hr = c.hr; float hcy = HeadCY(c); bool fem = c.fem; int hsh = SpecHash(s);
        int style = s.hair;
        bool hooded = s.hat == 5, helmet = s.hat == 4 || s.hat == 16;
        if (s.race == Beast && s.beast == 3 && style == 5) style = 0;
        if (style == 8 && s.race != Dwarf) return;
        if (s.race == Dwarf && !fem && style > 4) style = style % 5;
        if (hooded) { HairCap(b, c, Sl.Hair, 0.020f, 26f, -10f, 0f, 0f, 0f, 7); return; }
        float g = 0.034f;
        if (helmet)
        {
            HairCap(b, c, Sl.Hair, g, 34f, 4f, 10f, 0f, 0f, 6);
            if (style != 5) HairCurtain(b, c, SHairLo, hcy - hr * 0.1f, hcy - hr * (style == 1 || style == 7 ? 1.9f : 1.0f), 0.04f, hr * 0.94f, hr * 0.92f, 0.04f, 13, 4, 80f, 280f);
            return;
        }
        float front = fem ? 24f : 30f, sweep = fem ? 12f : 8f;
        float side = (style == 4 || style == 10) ? -14f : (style == 1 || style == 7 || style == 11 ? -8f : 4f);
        float back = (style == 4 || style == 1 || style == 7 || style == 10 || style == 11) ? -46f : -34f;
        bool spiky = style == 9 || style == 0;
        HairCap(b, c, SHairLo, g - 0.010f, front - 4f, back - 6f, side - 4f, 0f, 0f, 5);                         // lapisan dalam lebih gelap
        HairCap(b, c, Sl.Hair, g, front, back, side, sweep, spiky ? 11f : 6f, 10);                                // lapisan luar
        HairHalo(b, c, g + 0.003f);

        // ------- poni: helai runcing berlapis
        int nb = style == 5 ? 0 : (style == 4 ? 9 : 7);
        int part = (hsh >> 5) % 3;                       // 0 belah tengah, 1 miring kiri, 2 miring kanan
        for (int i = 0; i < nb; i++)
        {
            float u = nb == 1 ? 0f : (i / (float)(nb - 1)) * 2f - 1f;           // -1..1
            float th0 = u * 50f;
            float tipPhi = (style == 4 ? 22f : 26f) - Mathf.Abs(u) * 11f + ((i % 2 == 0) ? 0f : -6f);
            if (part != 0) tipPhi -= (part == 1 ? u : -u) * 8f;
            float flick = (part == 0 ? u * 16f : (part == 1 ? 24f : -24f)) * (style == 4 ? 0.25f : 1f);
            HLock(b, c, i % 3 == 1 ? SHairHi : Sl.Hair, 72f, th0 * 0.55f, tipPhi, th0 * 1.04f, g + 0.010f, g + 0.019f + 0.002f * (i % 3), hr * (style == 4 ? 0.24f : 0.20f), 0.016f, flick);
        }
        // ------- helai bingkai wajah
        float sideLen = (style == 0 || style == 9 || style == 5) ? -8f : (style == 4 ? -38f : (style == 1 || style == 7 || style == 11 ? -78f : -44f));
        for (int sg = -1; sg <= 1; sg += 2)
        {
            if (style == 5) break;
            for (int k = 0; k < 2; k++)
            {
                float th = sg * (70f + k * 12f);
                HLock(b, c, k == 0 ? Sl.Hair : SHairHi, 40f, th * 0.9f, sideLen - k * 8f, th + sg * (4f + k * 4f), g + 0.008f, g + 0.020f, hr * (k == 0 ? 0.20f : 0.12f), 0.018f, -sg * 8f);
            }
        }
        // ------- ahoge
        if (style != 5 && style != 9 && (hsh % 4) == 0)
        {
            Vector3 a0 = HP(c, 80f, 4f, g);
            Vector3[] ap = { a0, a0 + new Vector3(0.004f, hr * 0.28f, hr * 0.05f), a0 + new Vector3(0.030f, hr * 0.50f, hr * 0.22f), a0 + new Vector3(0.070f, hr * 0.48f, hr * 0.40f), a0 + new Vector3(0.090f, hr * 0.34f, hr * 0.44f) };
            Sweep(b, Sl.Hair, ap, new[] { 0.018f, 0.016f, 0.012f, 0.008f, 0.0012f }, new[] { 0.012f, 0.011f, 0.009f, 0.006f, 0.0012f }, Vector3.right, 6, 2);
        }

        switch (style)
        {
            case 0:      // pendek: jambul belakang runcing
                for (int i = 0; i < 7; i++)
                {
                    float th = 150f + i * 10f;
                    HLock(b, c, i % 2 == 0 ? Sl.Hair : SHairLo, 30f, th, -62f, th + (i - 3) * 2f, g + 0.006f, g + 0.016f, hr * 0.20f, 0.02f, 0f);
                }
                break;
            case 1:      // panjang lurus
                CurtainSet(b, c, hcy + hr * 0.30f, hcy - hr * 3.55f, 0.22f, hr * 1.08f, hr * 1.02f, 0.06f, 0.18f, 11, 56f, 304f);
                break;
            case 2:      // kuncir kuda
                {
                    Vector3 tie = new Vector3(0f, hcy + hr * 0.60f, -hr * 0.90f);
                    Vector3[] path = { tie, tie + new Vector3(0f, hr * 0.25f, -hr * 0.50f), tie + new Vector3(0f, -hr * 0.25f, -hr * 1.05f), tie + new Vector3(0f, -hr * 1.35f, -hr * 1.20f), tie + new Vector3(0f, -hr * 2.65f, -hr * 0.95f), tie + new Vector3(0f, -hr * 3.30f, -hr * 0.70f) };
                    float[] ww = { 0.030f, 0.070f, 0.092f, 0.088f, 0.050f, 0.0012f }, tt = { 0.030f, 0.055f, 0.070f, 0.066f, 0.040f, 0.0012f };
                    Sweep(b, Sl.Hair, path, ww, tt, Vector3.right, 8, 2);
                    for (int sg = -1; sg <= 1; sg += 2)
                    {
                        Vector3[] sp = { tie + new Vector3(sg * 0.02f, 0f, -0.01f), tie + new Vector3(sg * 0.05f, hr * 0.10f, -hr * 0.55f), tie + new Vector3(sg * 0.07f, -hr * 0.55f, -hr * 1.10f), tie + new Vector3(sg * 0.06f, -hr * 1.75f, -hr * 1.18f), tie + new Vector3(sg * 0.03f, -hr * 2.90f, -hr * 0.85f) };
                        Sweep(b, SHairHi, sp, new[] { 0.014f, 0.022f, 0.030f, 0.026f, 0.0012f }, new[] { 0.010f, 0.016f, 0.020f, 0.018f, 0.0012f }, Vector3.right, 6, 2);
                    }
                    b.Ball(Sl.CoatC, tie + new Vector3(0f, 0f, -0.015f), 0.040f, 4, 8, 0.8f);
                    HairCurtain(b, c, SHairLo, hcy + hr * 0.2f, hcy - hr * 0.95f, 0.05f, hr * 0.97f, hr * 0.94f, 0.03f, 13, 3, 78f, 282f);
                }
                break;
            case 3:      // dua kuncir
                for (int sg = -1; sg <= 1; sg += 2)
                {
                    Vector3 tie = new Vector3(sg * hr * 1.0f, hcy + hr * 0.40f, -hr * 0.45f);
                    Vector3[] path = { tie, tie + new Vector3(sg * hr * 0.30f, -hr * 0.30f, -hr * 0.30f), tie + new Vector3(sg * hr * 0.50f, -hr * 1.20f, -hr * 0.28f), tie + new Vector3(sg * hr * 0.45f, -hr * 2.50f, -hr * 0.10f), tie + new Vector3(sg * hr * 0.20f, -hr * 3.40f, 0.0f), tie + new Vector3(sg * hr * 0.10f, -hr * 3.90f, 0.02f) };
                    Sweep(b, Sl.Hair, path, new[] { 0.034f, 0.070f, 0.090f, 0.082f, 0.046f, 0.0012f }, new[] { 0.030f, 0.056f, 0.070f, 0.064f, 0.040f, 0.0012f }, Vector3.right, 8, 2);
                    Vector3[] p2 = { tie + new Vector3(sg * 0.01f, 0f, 0.012f), tie + new Vector3(sg * hr * 0.40f, -hr * 0.45f, -hr * 0.12f), tie + new Vector3(sg * hr * 0.62f, -hr * 1.40f, -hr * 0.12f), tie + new Vector3(sg * hr * 0.55f, -hr * 2.65f, hr * 0.0f), tie + new Vector3(sg * hr * 0.28f, -hr * 3.50f, 0.03f) };
                    Sweep(b, SHairHi, p2, new[] { 0.012f, 0.020f, 0.028f, 0.024f, 0.0012f }, new[] { 0.010f, 0.016f, 0.020f, 0.018f, 0.0012f }, Vector3.right, 6, 2);
                    b.Ball(Sl.CoatC, tie + new Vector3(sg * 0.012f, 0.012f, 0f), 0.034f, 4, 8);
                    b.Ball(Sl.CoatC, tie + new Vector3(sg * 0.012f, 0.052f, 0f), 0.022f, 3, 6);
                }
                HairCurtain(b, c, SHairLo, hcy + hr * 0.2f, hcy - hr * 0.80f, 0.04f, hr * 0.97f, hr * 0.94f, 0.03f, 13, 3, 78f, 282f);
                break;
            case 4:      // bob
                CurtainSet(b, c, hcy + hr * 0.30f, hcy - hr * 1.18f, 0.04f, hr * 1.10f, hr * 1.04f, 0.10f, 0f, 9, 52f, 308f);
                for (int sg = -1; sg <= 1; sg += 2)
                    Sweep(b, Sl.Hair, new[] { HP(c, 40f, sg * 80f, g + 0.01f), HP(c, 0f, sg * 94f, g + 0.032f), HP(c, -34f, sg * 90f, g + 0.036f), HP(c, -52f, sg * 76f, g + 0.020f) }, new[] { 0.052f, 0.060f, 0.056f, 0.020f }, new[] { 0.018f, 0.026f, 0.026f, 0.010f }, Vector3.forward, 8, 2);
                break;
            case 5:      // ikal bulat (berkelompok)
                for (int i = 0; i < 14; i++)
                {
                    float ph = 28f + (i % 4) * 16f, th = i * (360f / 14f) + (i % 2) * 11f;
                    if (Mathf.Abs(Mathf.DeltaAngle(th, 0f)) < 30f && ph < 55f) continue;      // jangan menutup muka
                    b.Ball(i % 3 == 0 ? SHairHi : Sl.Hair, HP(c, ph, th, g + 0.034f), hr * (0.30f + 0.04f * (i % 3)), 4, 8, 0.9f);
                }
                b.Ball(Sl.Hair, new Vector3(0f, hcy + hr * 0.50f, -hr * 0.1f), hr * 1.05f, 6, 12, 0.75f);
                for (int i = 0; i < 3; i++) b.Ball(Sl.Hair, HP(c, 52f, -22f + i * 22f, g + 0.032f), hr * 0.22f, 3, 7, 0.9f);
                break;
            case 6:      // sanggul
                {
                    Vector3 bun = new Vector3(0f, hcy + hr * 1.02f, -hr * 0.42f);
                    b.Ball(Sl.Hair, bun, hr * 0.44f, 7, 14);
                    b.Ball(SHairHi, bun + new Vector3(hr * 0.1f, hr * 0.15f, hr * 0.12f), hr * 0.17f, 4, 8);
                    b.Beam(Sl.CoatC, bun + new Vector3(-hr * 0.5f, -hr * 0.18f, 0f), bun + new Vector3(hr * 0.5f, -hr * 0.18f, 0f), 0.022f, 0.022f);
                    b.Beam(Sl.Gold, bun + new Vector3(-hr * 0.55f, hr * 0.05f, hr * 0.05f), bun + new Vector3(hr * 0.55f, hr * 0.28f, hr * 0.05f), 0.010f, 0.010f);
                    HairCurtain(b, c, SHairLo, hcy + hr * 0.1f, hcy - hr * 0.60f, 0.03f, hr * 0.98f, hr * 0.95f, 0.03f, 13, 3, 78f, 282f);
                    for (int sg = -1; sg <= 1; sg += 2)
                        Sweep(b, Sl.Hair, new[] { HP(c, 36f, sg * 78f, g + 0.01f), HP(c, -4f, sg * 92f, g + 0.034f), HP(c, -40f, sg * 88f, g + 0.040f), HP(c, -70f, sg * 76f, g + 0.034f) }, new[] { 0.034f, 0.030f, 0.026f, 0.0014f }, new[] { 0.014f, 0.016f, 0.016f, 0.0014f }, Vector3.forward, 6, 2);
                }
                break;
            case 7:      // kepang panjang
                {
                    HairCurtain(b, c, SHairLo, hcy + hr * 0.1f, hcy - hr * 0.75f, 0.04f, hr * 0.98f, hr * 0.95f, 0.03f, 13, 3, 78f, 282f);
                    int segN = 13;
                    for (int i = 0; i < segN; i++)
                    {
                        float t = i / (float)(segN - 1); float sd = (i % 2 == 0) ? 1f : -1f;
                        Vector3 p = new Vector3(sd * 0.020f * (1f - t * 0.5f), hcy - hr * 0.55f - t * hr * 3.5f, -hr * 1.02f - 0.016f * Mathf.Sin(t * 3f));
                        b.Ball(i % 2 == 0 ? Sl.Hair : SHairLo, p, 0.050f * (1f - t * 0.45f), 4, 8, 0.78f);
                    }
                    Vector3 tipP = new Vector3(0f, hcy - hr * 4.15f, -hr * 1.04f);
                    b.Ball(Sl.CoatC, tipP + new Vector3(0f, 0.045f, 0f), 0.030f, 4, 8);
                    Sweep(b, Sl.Hair, new[] { tipP, tipP + new Vector3(0.01f, -0.06f, -0.01f), tipP + new Vector3(0.0f, -0.12f, 0.0f) }, new[] { 0.036f, 0.026f, 0.0012f }, new[] { 0.030f, 0.020f, 0.0012f }, Vector3.right, 6, 2);
                    for (int sg = -1; sg <= 1; sg += 2)
                        Sweep(b, SHairHi, new[] { HP(c, 36f, sg * 84f, g + 0.01f), HP(c, -8f, sg * 94f, g + 0.034f), HP(c, -48f, sg * 86f, g + 0.040f), HP(c, -76f, sg * 74f, g + 0.034f) }, new[] { 0.026f, 0.024f, 0.020f, 0.0014f }, new[] { 0.012f, 0.014f, 0.014f, 0.0014f }, Vector3.forward, 6, 2);
                }
                break;
            case 9:      // jabrik: duri besar ala tokoh utama
                for (int i = 0; i < 9; i++)
                {
                    float th = 110f + i * 17f; float ph = 70f - (i % 3) * 14f;
                    Vector3 a0 = HP(c, ph, th, g), dir = (HP(c, ph, th, 0f) - new Vector3(0f, hcy, 0f)).normalized;
                    Vector3 tip = a0 + dir * hr * (0.95f + 0.25f * (i % 3)) + new Vector3(0f, hr * 0.15f, -hr * 0.18f);
                    Vector3 ba = HP(c, ph, th - 9f, g), bb = HP(c, ph, th + 9f, g);
                    Pyramid(b, i % 2 == 0 ? Sl.Hair : SHairHi, ba, bb, a0 + dir * hr * 0.25f + new Vector3(0f, hr * 0.35f, 0f), tip);
                }
                for (int i = 0; i < 5; i++) { float th = -44f + i * 22f; Vector3 a0 = HP(c, 70f, th, g), tip = HP(c, 40f, th * 1.1f, g + 0.075f) + new Vector3(0f, hr * 0.20f, 0f); Pyramid(b, Sl.Hair, HP(c, 72f, th - 10f, g), HP(c, 72f, th + 10f, g), a0 + new Vector3(0f, 0.05f, 0.05f), tip); }
                break;
            case 10:     // acak sedang
                CurtainSet(b, c, hcy + hr * 0.30f, hcy - hr * 1.75f, 0.20f, hr * 1.06f, hr * 1.00f, 0.08f, 0.10f, 9, 58f, 302f);
                break;
            case 11:     // poni miring + ekor samping
                {
                    CurtainSet(b, c, hcy + hr * 0.25f, hcy - hr * 1.40f, 0.08f, hr * 1.04f, hr * 1.0f, 0.06f, 0.05f, 8, 58f, 302f);
                    float sg = (hsh & 1) == 0 ? 1f : -1f;
                    Vector3[] path = { HP(c, 36f, sg * 84f, g + 0.012f), HP(c, -2f, sg * 98f, g + 0.040f), HP(c, -52f, sg * 94f, g + 0.050f) + new Vector3(sg * 0.02f, 0f, 0.02f), new Vector3(sg * hr * 1.0f, hcy - hr * 2.2f, hr * 0.15f), new Vector3(sg * hr * 0.85f, hcy - hr * 3.0f, hr * 0.28f) };
                    Sweep(b, Sl.Hair, path, new[] { 0.040f, 0.060f, 0.070f, 0.060f, 0.0012f }, new[] { 0.018f, 0.030f, 0.036f, 0.030f, 0.0012f }, Vector3.forward, 8, 2);
                    for (int i = 0; i < 4; i++) { float th = sg * (-20f + i * 15f); HLock(b, c, i % 2 == 0 ? Sl.Hair : SHairHi, 70f, th * 0.4f, 8f, th + sg * 30f, g + 0.016f, g + 0.036f, hr * 0.22f, 0.022f, sg * 20f); }
                }
                break;
        }
    }

    // ------------------------------------------------------------------ janggut
    /// <summary>
    /// Janggut anime: pita rahang + cambang yang menipis ke kulit di tepi atasnya (mulut tetap kelihatan), kumis menyapu,
    /// dan untuk dwarf / janggut panjang: baji janggut meruncing berhias cincin emas.
    /// </summary>
    static void AnimeBeard(GeoBag b, Ctx c)
    {
        Spec s = c.s; float hr = c.hr; int type = s.beard;
        float g0 = type == 1 ? 0.006f : 0.014f, lift = type == 1 ? -16f : 0f;
        const int K = 7, J = 23;
        SurfGrid(b, Sl.Hair, K, J - 1, (r, j) =>
        {
            float th = -102f + j * (204f / (J - 1)), u = Mathf.Abs(th);
            float pm = u <= 28f ? -50f : (u <= 60f ? Mathf.Lerp(-50f, -18f, Sm((u - 28f) / 32f)) : Mathf.Lerp(-18f, 6f, Sm((u - 60f) / 40f)));
            pm += lift;
            float fk = r / (float)K, ph = Mathf.Lerp(-86f, pm, fk);
            float g = g0 * (1f - Sm((fk - 0.55f) / 0.45f));
            return HP(c, ph, th, g);
        }, false, true, new Vector3(0f, HeadCY(c), 0f));
        if (type >= 2 || s.race == Dwarf)       // kumis menyapu
            for (int sg = -1; sg <= 1; sg += 2)
                Sweep(b, Sl.Hair, new[] { HP(c, -21f, sg * 3f, 0.016f), HP(c, -26f, sg * 13f, 0.022f), HP(c, -33f, sg * 25f, 0.019f), HP(c, -27f, sg * 34f, 0.011f) },
                    new[] { hr * 0.050f, hr * 0.052f, hr * 0.036f, hr * 0.006f }, new[] { hr * 0.050f, hr * 0.046f, hr * 0.030f, hr * 0.006f }, Vector3.up, 8, 2);
        if (type >= 3 || (type == 2 && s.race == Dwarf))
        {
            Vector3 chin = HP(c, -80f, 0f, 0.010f);
            float len = type == 3 ? hr * 2.5f : hr * 1.55f;
            float[] ts = { 0f, 0.3f, 0.6f, 0.85f, 1f };
            float[] wk = { 0.84f, 0.80f, 0.62f, 0.34f, 0.03f }, tk = { 0.36f, 0.36f, 0.31f, 0.21f, 0.03f };
            var pts = new Vector3[ts.Length]; var w = new float[ts.Length]; var th2 = new float[ts.Length];
            for (int i = 0; i < ts.Length; i++)
            {
                float t = ts[i];
                pts[i] = new Vector3(0f, chin.y + hr * 0.30f - t * len, chin.z - hr * 0.10f + hr * 0.10f * Mathf.Sin(t * 3.0f));
                w[i] = hr * wk[i]; th2[i] = hr * tk[i];
            }
            Sweep(b, Sl.Hair, pts, w, th2, Vector3.right, 10, 3);
            if (type == 3)      // cincin emas pengikat
                for (int i = 0; i < 2; i++)
                {
                    float t = 0.46f + i * 0.2f;
                    float ww = Mathf.Lerp(wk[2], wk[1], 1f - t) * hr, tt = Mathf.Lerp(tk[2], tk[1], 1f - t) * hr;
                    Vector3 cen = new Vector3(0f, chin.y + hr * 0.30f - t * len, chin.z - hr * 0.10f + hr * 0.10f * Mathf.Sin(t * 3.0f));
                    Tube(b, Sl.Gold, new[] { cen + new Vector3(0f, -0.012f, 0f), cen + new Vector3(0f, 0.012f, 0f) }, new[] { ww * 1.1f, ww * 1.1f }, new[] { tt * 1.12f, tt * 1.12f }, 12, false, false);
                }
        }
    }
}
