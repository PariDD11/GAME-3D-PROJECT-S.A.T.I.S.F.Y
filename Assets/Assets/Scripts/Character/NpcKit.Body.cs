using System.Collections.Generic;
using UnityEngine;

public static partial class NpcKit
{
    // =====================================================================
    // Konteks proporsi
    // =====================================================================
    class Ctx
    {
        public Spec s;
        public float H, W, hr, ax, hx, hk;
        public bool fem;
        public Def d;
        public Ctx(Spec sp)
        {
            s = sp; fem = sp.female;
            // Proporsi manusia nyata (kepala ~1/7.5 tinggi). Kepala dibangun pada ukuran dasar 0.24 lalu diskalakan (hk).
            float hrReal;
            switch (sp.race)
            {
                case Dwarf: H = fem ? 0.74f : 0.78f; W = fem ? 1.10f : 1.22f; hrReal = fem ? 0.150f : 0.152f; break;
                case Elf: H = fem ? 1.01f : 1.07f; W = fem ? 0.84f : 0.92f; hrReal = fem ? 0.139f : 0.141f; break;
                case Beast: H = fem ? 0.96f : 1.03f; W = fem ? 0.88f : 0.98f; hrReal = fem ? 0.143f : 0.146f; break;
                default: H = fem ? 0.95f : 1.02f; W = fem ? 0.88f : 0.98f; hrReal = fem ? 0.144f : 0.150f; break;
            }
            hr = 0.24f; hk = hrReal / hr;
            W *= 0.92f + 0.10f * sp.build;
            ax = ProfRx(1.38f) * W + 0.014f;
            hx = 0.085f * W;
            d = MakeDef(this);
        }
        public float Y(float v) { return v * H; }
        public float TotalHeight { get { return Y(1.46f) + hr * hk * 2.05f + (s.race == Beast && s.beast == 3 ? 0.18f : 0f); } }
        public Vector3 Pivot(int p)
        {
            switch (p)
            {
                case PTorso: return new Vector3(0f, Y(0.95f), 0f);
                case PHead: return new Vector3(0f, Y(1.46f), 0f);
                case PArmL: return new Vector3(-ax, Y(1.38f), 0f);
                case PArmR: return new Vector3(ax, Y(1.38f), 0f);
                case PLegL: return new Vector3(-hx, Y(0.90f), 0f);
                case PLegR: return new Vector3(hx, Y(0.90f), 0f);
                case PForeL: return ElbowPos(this, -1);
                case PForeR: return ElbowPos(this, 1);
                default: return new Vector3(0f, Y(0.93f), -0.09f * W);
            }
        }
        float ProfRx(float y) { return Prof(y)[0]; }
        public float[] Prof(float y)
        {
            float[][] P = fem ? ProfF : ProfM;
            if (y <= P[0][0]) return new[] { P[0][1], P[0][2], P[0][3] };
            for (int i = 0; i + 1 < P.Length; i++)
                if (y <= P[i + 1][0])
                {
                    float t = (y - P[i][0]) / (P[i + 1][0] - P[i][0]); t = t * t * (3f - 2f * t);
                    return new[] { Mathf.Lerp(P[i][1], P[i + 1][1], t), Mathf.Lerp(P[i][2], P[i + 1][2], t), Mathf.Lerp(P[i][3], P[i + 1][3], t) };
                }
            var l = P[P.Length - 1];
            return new[] { l[1], l[2], l[3] };
        }
        // satu titik pada permukaan badan: ketinggian y (satuan dasar), sudut a (rad; 0 = depan, + ke arah +X, pi = belakang)
        public Vector3 TPt(float y, float a, float grow, float flare)
        {
            float[] p = Prof(y);
            float extra = y < 0.82f ? flare * Mathf.Pow(0.82f - y, 1.35f) * 0.8f : 0f;
            float h01 = Mathf.Clamp01((0.82f - y) / 0.10f), hip = 0.034f * W * h01 * h01 * (3f - 2f * h01);        // pinggul melebar menutupi paha
            float rx = (p[0] + grow) * W + extra + hip, rz = (p[1] + grow) * Mathf.Lerp(1f, W, 0.6f) + extra * 0.9f + hip * 0.8f;
            float amp = Mathf.Min(0.075f, extra * 0.20f);
            float pl = extra > 0f ? 1f + amp * (Mathf.Cos(a * 8f) + 0.45f * Mathf.Cos(a * 11f + 1.3f)) : 1f;      // lipatan rok
            return new Vector3(Mathf.Sin(a) * rx * pl, Y(y), p[2] + Mathf.Cos(a) * rz * pl);
        }
        // satu cincin badan pada ketinggian dasar y (satuan manusia), dengan pembesaran 'grow' dan pelebaran rok 'flare'
        /// <summary>Cincin badan. Jumlah titik default bergantung pada flare (rok melebar 32, lainnya 28) dan SAMA untuk TorsoLoft maupun hiasan (Trim, sabuk, kerah)
        /// agar sampel lipatan rok sejajar; jumlah titik berbeda membuat pita hiasan terpotong lipatan.</summary>
        public Vector3[] TRing(float y, float grow, float flare, int n = 0)
        {
            if (n <= 0) n = flare > 0.2f ? 32 : 28;
            var r = new Vector3[n];
            for (int j = 0; j < n; j++) r[j] = TPt(y, j * Mathf.PI * 2f / n, grow, flare);
            return r;
        }
    }

    // y, rx, rz, zc  (satuan manusia dasar)
    static readonly float[][] ProfM =
    {
        new[] { 0.82f, 0.148f, 0.093f, 0f }, new[] { 0.90f, 0.154f, 0.096f, 0f }, new[] { 1.00f, 0.142f, 0.088f, 0f }, new[] { 1.10f, 0.140f, 0.086f, 0f },
        new[] { 1.22f, 0.168f, 0.100f, 0.004f }, new[] { 1.32f, 0.188f, 0.104f, 0.006f }, new[] { 1.38f, 0.180f, 0.090f, 0f },
        new[] { 1.43f, 0.100f, 0.068f, 0f }, new[] { 1.46f, 0.054f, 0.054f, 0f }
    };
    static readonly float[][] ProfF =
    {
        new[] { 0.82f, 0.150f, 0.096f, 0f }, new[] { 0.90f, 0.172f, 0.106f, -0.004f }, new[] { 1.00f, 0.132f, 0.090f, 0f }, new[] { 1.10f, 0.098f, 0.076f, 0f },
        new[] { 1.22f, 0.128f, 0.094f, 0.016f }, new[] { 1.31f, 0.142f, 0.100f, 0.026f }, new[] { 1.37f, 0.138f, 0.082f, 0.004f },
        new[] { 1.43f, 0.088f, 0.060f, 0f }, new[] { 1.46f, 0.046f, 0.046f, 0f }
    };

    // =====================================================================
    // Util geometri
    // =====================================================================
    static Vector3[] Circle(Vector3 c, float rx, float rz, int n)
    {
        var r = new Vector3[n];
        for (int j = 0; j < n; j++)
        {
            float a = j * Mathf.PI * 2f / n;
            r[j] = new Vector3(c.x + Mathf.Sin(a) * rx, c.y, c.z + Mathf.Cos(a) * rz);
        }
        return r;
    }

    /// <summary>Loft badan dari y0 ke y1 (satuan dasar). Tutup atas/bawah opsional.</summary>
    static float lastFlare;
    static void TorsoLoft(GeoBag b, Ctx c, int slot, float y0, float y1, float grow, float flare, bool capB = false, bool capT = false, float step = 0.05f, int n = 0)
    {
        if (flare > 0f && y0 < 0.82f) lastFlare = flare;
        if (n <= 0) n = flare > 0.2f ? 32 : 28;
        var rings = new List<Vector3[]>();
        for (float y = y0; y < y1 - 0.001f; y += step) rings.Add(c.TRing(y, grow, flare, n));
        rings.Add(c.TRing(y1, grow, flare, n));
        NLoft(b, slot, rings, capB, capT);
    }

    /// <summary>Tabung bertingkat antar titik tengah (a bawah -> b atas) dengan radius per titik, n sisi.</summary>
    static void Tube(GeoBag b, int slot, Vector3[] centers, float[] rx, float[] rz, int n, bool capB, bool capT)
    {
        int m = centers.Length;
        var rings = new List<Vector3[]>();
        if (m < 3)
        {
            for (int i = 0; i < m; i++) rings.Add(Circle(centers[i], rx[i], rz[i], n));
        }
        else
        {
            const int sub = 3;
            for (int i = 0; i < m - 1; i++)
                for (int k = 0; k < sub; k++)
                {
                    float t = k / (float)sub;
                    int i0 = Mathf.Max(0, i - 1), i3 = Mathf.Min(m - 1, i + 2);
                    Vector3 c = CR(centers[i0], centers[i], centers[i + 1], centers[i3], t);
                    float x = CRf(rx[i0], rx[i], rx[i + 1], rx[i3], t), z = CRf(rz[i0], rz[i], rz[i + 1], rz[i3], t);
                    rings.Add(Circle(c, Mathf.Max(0.002f, x), Mathf.Max(0.002f, z), n));
                }
            rings.Add(Circle(centers[m - 1], rx[m - 1], rz[m - 1], n));
        }
        NLoft(b, slot, rings, capB, capT);
    }

    static Vector3 CR(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t, t3 = t2 * t;
        return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }
    static float CRf(float p0, float p1, float p2, float p3, float t)
    {
        float t2 = t * t, t3 = t2 * t;
        return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    static void Sphere(GeoBag b, int slot, Vector3 c, float rx, float ry, float rz, int rings = 8, int n = 14)
    {
        var rs = new List<Vector3[]>();
        for (int i = 0; i <= rings; i++)
        {
            float phi = Mathf.Lerp(-90f, 90f, i / (float)rings) * Mathf.Deg2Rad;
            float f = Mathf.Cos(phi);
            rs.Add(Circle(new Vector3(c.x, c.y + Mathf.Sin(phi) * ry, c.z), rx * f, rz * f, n));
        }
        NLoft(b, slot, rs, false, false);
    }

    /// <summary>Segitiga yang otomatis menghadap keluar dari titik pusat benda.</summary>
    static void TriOut(GeoBag b, int slot, Vector3 a, Vector3 bb, Vector3 cc, Vector3 center)
    {
        Vector3 n = Vector3.Cross(bb - a, cc - a);
        Vector3 mid = (a + bb + cc) / 3f;
        if (Vector3.Dot(n, mid - center) < 0f) b.Tri(slot, a, cc, bb);
        else b.Tri(slot, a, bb, cc);
    }

    /// <summary>Limas segitiga (telinga, taring, duri) dari 3 titik alas dan satu puncak.</summary>
    static void Pyramid(GeoBag b, int slot, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 apex)
    {
        Vector3 ctr = (p0 + p1 + p2 + apex) / 4f;
        TriOut(b, slot, p0, p1, apex, ctr);
        TriOut(b, slot, p1, p2, apex, ctr);
        TriOut(b, slot, p2, p0, apex, ctr);
        TriOut(b, slot, p0, p1, p2, ctr);
    }

    static Vector3 ElbowPos(Ctx c, int sgn) { return new Vector3(ArmX(c, sgn, 1.20f) + sgn * 0.012f, c.Y(1.20f), 0.022f); }
    /// <summary>Properti bertiang (garpu, tombak, tongkat) dipegang tangan kanan: lengan diayun ke depan supaya tiang berdiri di depan badan, tidak menembus lengan.</summary>
    static bool PoleProp(Spec s) { return s != null && (s.prop == 2 || s.prop == 3 || s.prop == 5 || s.prop == 6); }
    static Vector3 HandPos(Ctx c, int sgn)
    {
        bool pole = sgn == 1 && PoleProp(c.s);
        return new Vector3(ArmX(c, sgn, 0.84f) - sgn * 0.004f + (pole ? 0.010f : 0f), c.Y(0.82f) - 0.01f, 0.080f + (pole ? 0.078f : 0f));
    }

    static float ArmX(Ctx c, int sgn, float y) { return sgn * (c.ax + 0.010f * (1.38f - y) / 0.5f * c.W); }

    // =====================================================================
    // Badan
    // =====================================================================
    static void BuildTorso(GeoBag b, Ctx c)
    {
        TorsoLoft(b, c, Sl.SkinS, 0.82f, 1.46f, 0f, 0f, true, true, 0.05f, 24);
        // pinggul kecil di bawah (celana dalam / dasar)
        var rg = new List<Vector3[]> { c.TRing(0.74f, 0f, 0f), c.TRing(0.82f, 0f, 0f) };
        NLoft(b, c.d.pantsSlot >= 0 ? c.d.pantsSlot : Sl.CoatB, rg, true, false);
        Garments(b, c);
        Props(b, c);
    }

    static void BuildLeg(GeoBag b, Ctx c, int sgn)
    {
        float x = sgn * c.hx, W = c.W;
        float[] ys = { 0.90f, 0.72f, 0.50f, 0.28f, 0.09f };
        float[] rr = { 0.088f, 0.078f, 0.054f, 0.050f, 0.036f };
        var cs = new Vector3[ys.Length]; var rx = new float[ys.Length];
        for (int i = 0; i < ys.Length; i++) { cs[i] = new Vector3(x, c.Y(ys[i]), 0f); rx[i] = rr[i] * (c.fem ? (i < 2 ? 1.04f : 0.92f) : 1.05f) * W * (c.s.race == Dwarf ? 1.05f : 1f); }
        Tube(b, Sl.SkinS, cs, rx, rx, (c.d.pantsSlot >= 0 && c.d.pantsEnd < 0.2f) ? 10 : 14, false, false);      // kulit tertutup celana: sisi lebih sedikit
        b.Ball(Sl.SkinS, cs[0] + new Vector3(0f, 0.01f, 0f), rx[0] * 1.02f, 5, 12, 0.9f);       // sendi pinggul
        // pakaian kaki
        Def d = c.d;
        if (d.pantsSlot >= 0 && d.pantsEnd < 0.89f)
        {
            var pc = new List<Vector3>(); var pr = new List<float>();
            foreach (float y in new[] { 0.90f, 0.72f, 0.50f, 0.28f, 0.10f })
            {
                if (y < d.pantsEnd - 0.001f) break;
                float t = (0.90f - y) / 0.80f;
                pc.Add(new Vector3(x, c.Y(y), 0f)); pr.Add((Mathf.Lerp(0.092f, 0.056f, t) + d.pantsLoose) * W);
            }
            if (pc.Count < 2) { pc.Add(new Vector3(x, c.Y(d.pantsEnd), 0f)); pr.Add(0.06f * W); }
            Tube(b, d.pantsSlot, pc.ToArray(), pr.ToArray(), pr.ToArray(), 12, false, false);
        }
        // kaus kaki / stoking bila rok pendek
        if (d.sockSlot >= 0)
        {
            var sc = new[] { new Vector3(x, c.Y(d.sockTop), 0f), new Vector3(x, c.Y(0.12f), 0f) };
            float r0 = 0.050f * W + 0.006f, r1 = 0.038f * W + 0.006f;
            Tube(b, d.sockSlot, new[] { sc[1], sc[0] }, new[] { r1, r0 }, new[] { r1, r0 }, 12, false, false);
        }
        // sepatu / bot
        float bt = d.bootTop;
        var bcn = new[] { new Vector3(x, c.Y(0.02f), 0f), new Vector3(x, c.Y(0.10f), 0f), new Vector3(x, c.Y(bt), 0f) };
        float br0 = 0.045f * W + 0.01f, br1 = 0.044f * W + 0.012f, br2 = Mathf.Lerp(0.046f, 0.056f, (bt - 0.1f) / 0.4f) * W + 0.012f;
        Tube(b, d.bootSlot, bcn, new[] { br0, br0, br2 }, new[] { br0, br0, br2 }, 12, true, true);
        Sphere(b, d.bootSlot, new Vector3(x, 0.040f, 0.060f), 0.052f * W + 0.008f, 0.042f, 0.125f, 6, 14);
        Sphere(b, Sl.Dark, new Vector3(x, 0.012f, 0.060f), 0.054f * W + 0.009f, 0.016f, 0.128f, 4, 14);     // sol
        if (d.greaves)
        {
            var gcn = new[] { new Vector3(x, c.Y(0.14f), 0.0f), new Vector3(x, c.Y(0.34f), 0.0f), new Vector3(x, c.Y(0.50f), 0.0f) };
            Tube(b, Sl.Metal, gcn, new[] { 0.058f * W, 0.062f * W, 0.066f * W }, new[] { 0.058f * W, 0.062f * W, 0.066f * W }, 12, false, false);
            b.Ball(Sl.Metal, new Vector3(x, c.Y(0.50f), 0.05f * W), 0.058f * W, 4, 6, 0.8f);       // lutut
            var tcn = new[] { new Vector3(x, c.Y(0.52f), 0f), new Vector3(x, c.Y(0.72f), 0f), new Vector3(x, c.Y(0.86f), 0f) };
            Tube(b, Sl.Metal, tcn, new[] { 0.062f * W, 0.084f * W, 0.094f * W }, new[] { 0.062f * W, 0.084f * W, 0.094f * W }, 12, false, false);
            b.Box(Sl.Gold, new Vector3(x, c.Y(0.50f), 0.085f * W), new Vector3(0.05f, 0.03f, 0.02f));
        }
    }

    /// <summary>seg: 0 = seluruh lengan (NPC), 1 = lengan atas (bahu..siku), 2 = lengan bawah + tangan (siku..tangan).</summary>
    static void BuildArm(GeoBag b, Ctx c, int sgn, int seg = 0)
    {
        float W = c.W;
        float[] ys = { 1.40f, 1.34f, 1.20f, 1.10f, 0.96f, 0.87f };
        float[] rr = c.fem ? new[] { 0.037f, 0.036f, 0.029f, 0.028f, 0.025f, 0.020f } : new[] { 0.054f, 0.051f, 0.041f, 0.039f, 0.034f, 0.027f };
        var cs = new Vector3[ys.Length]; var rx = new float[ys.Length];
        float[] az = { 0f, 0.004f, 0.022f, 0.040f, 0.062f, 0.075f }, ax2 = { 0f, 0.004f, 0.012f, 0.014f, 0.006f, -0.004f };
        if (sgn == 1 && PoleProp(c.s)) { az = new[] { 0f, 0.004f, 0.026f, 0.070f, 0.120f, 0.150f }; ax2 = new[] { 0f, 0.004f, 0.016f, 0.022f, 0.018f, 0.006f }; }     // lengan kanan memegang tiang: ayun ke depan
        for (int i = 0; i < ys.Length; i++) { cs[i] = new Vector3(ArmX(c, sgn, ys[i]) + sgn * ax2[i], c.Y(ys[i]), az[i]); rx[i] = rr[i] * 1.0f * Mathf.Lerp(1f, c.W, 0.7f); }
        int i0 = seg == 2 ? 2 : 0, i1 = seg == 1 ? 2 : ys.Length - 1;          // rentang ring yang dipakai segmen ini
        bool upper = seg != 2, lower = seg != 1;
        Tube(b, Sl.SkinS, Sub(cs, i0, i1), Sub(rx, i0, i1), Sub(rx, i0, i1), (c.d.sleeveEnd >= 0f && c.d.sleeveEnd <= 0.95f) ? 10 : 14, false, false);
        if (upper) b.Ball(Sl.SkinS, cs[0] + new Vector3(0f, -0.004f, 0f), rx[0] * 0.99f, 5, 12);                                  // bahu bulat
        if (seg == 1) b.Ball(Sl.SkinS, cs[2], rx[2] * 1.02f, 3, 8);                   // sendi siku
        Def d = c.d;
        if (c.s.race == Beast && lower)      // bulu di lengan bawah
        {
            float[] fr = { rx[3] + 0.006f, rx[4] + 0.007f, rx[5] + 0.008f };
            Tube(b, Sl.Fur, new[] { cs[3], cs[4], cs[5] }, fr, fr, 12, false, false);
        }
        if (d.sleeveEnd >= 0f)
        {
            if (upper) b.Ball(d.sleeveSlot, cs[0] + new Vector3(0f, -0.004f, 0f), rx[0] + 0.011f, 5, 12);
            var sc = new List<Vector3>(); var sr = new List<float>(); var si = new List<int>();
            for (int i = 0; i < ys.Length; i++)
            {
                if (ys[i] < d.sleeveEnd - 0.001f) { break; }
                sc.Add(cs[i]); si.Add(i); sr.Add(rx[i] + 0.010f + (d.sleeveBell && i >= 3 ? 0.02f * (i - 2) : 0f));
            }
            if (sc.Count == 1) { sc.Add(new Vector3(ArmX(c, sgn, d.sleeveEnd), c.Y(d.sleeveEnd), 0f)); sr.Add(sr[0] - 0.004f); si.Add(1); }
            var tc = new List<Vector3>(); var tr = new List<float>();
            for (int k = 0; k < sc.Count; k++) if (si[k] >= i0 && si[k] <= i1) { tc.Add(sc[k]); tr.Add(sr[k]); }
            if (tc.Count >= 2) Tube(b, d.sleeveSlot, tc.ToArray(), tr.ToArray(), tr.ToArray(), 12, false, true);
            if (seg == 1 && sc.Count > 3) b.Ball(d.sleeveSlot, cs[2], sr[2] * 1.02f, 3, 8);
            if (d.cuffSlot >= 0 && sc.Count >= 2 && si[si.Count - 1] <= i1 && si[si.Count - 1] >= i0)
            {
                Vector3 e = sc[sc.Count - 1];
                float cr = sr[sr.Count - 1] + 0.004f;
                Tube(b, d.cuffSlot, new[] { e + new Vector3(0f, 0.035f * c.H, 0f), e }, new[] { cr, cr }, new[] { cr, cr }, 12, false, true);
            }
        }
        if (upper && d.puffSleeve) b.Ball(d.sleeveSlot, new Vector3(ArmX(c, sgn, 1.35f), c.Y(1.36f), 0f), 0.052f * Mathf.Lerp(1f, W, 0.7f), 5, 10, 0.9f);
        // tangan
        Vector3 hp = HandPos(c, sgn);
        if (lower)
        {
            int handSlot = c.s.race == Beast ? Sl.Fur : Sl.SkinS;
            if (d.gloveSlot >= 0) handSlot = d.gloveSlot;
            float hs = c.fem ? 1.0f : 1.12f;
            Sphere(b, handSlot, hp + new Vector3(0f, 0.012f, 0f), 0.034f * hs, 0.040f * hs, 0.020f * hs, 6, 12);                       // telapak
            Tube(b, handSlot, new[] { hp + new Vector3(0f, -0.018f, 0.002f), hp + new Vector3(0f, -0.058f * hs, 0.010f), hp + new Vector3(0f, -0.082f * hs, 0.030f * hs) },
                new[] { 0.031f * hs, 0.029f * hs, 0.020f * hs }, new[] { 0.016f * hs, 0.015f * hs, 0.012f * hs }, 12, false, true);   // jari menekuk
            Tube(b, handSlot, new[] { hp + new Vector3(-sgn * 0.026f * hs, 0.02f, 0.012f), hp + new Vector3(-sgn * 0.030f * hs, -0.015f, 0.028f * hs), hp + new Vector3(-sgn * 0.022f * hs, -0.038f * hs, 0.036f * hs) },
                new[] { 0.011f * hs, 0.010f * hs, 0.008f * hs }, new[] { 0.011f * hs, 0.010f * hs, 0.008f * hs }, 8, false, true);        // jempol
            if (c.s.race == Beast)
                for (int i = 0; i < 3; i++) b.Box(Sl.EyeW, hp + new Vector3((i - 1) * 0.018f, c.Y(-0.035f), 0.02f), new Vector3(0.008f, 0.02f, 0.01f));    // cakar kecil
            if (d.vambrace)
            {
                float v0 = rx[4] + 0.014f, v1 = rx[5] + 0.014f;
                Tube(b, d.vambraceSlot, new[] { cs[4], cs[5] + new Vector3(0f, 0.01f, 0f) }, new[] { v0, v1 }, new[] { v0, v1 }, 12, false, true);
                if (d.vambraceSlot == Sl.Metal) Tube(b, Sl.Metal, new[] { cs[3], cs[4] }, new[] { rx[3] + 0.012f, v0 }, new[] { rx[3] + 0.012f, v0 }, 12, false, false);
            }
        }
        if (upper && d.pauldron > 0)
        {
            Vector3 sp = new Vector3(sgn * (c.ax + 0.01f), c.Y(1.40f), 0f);
            b.Ball(d.pauldron >= 2 ? Sl.Metal : Sl.Leather, sp, 0.074f * Mathf.Lerp(1f, W, 0.7f), 6, 10, 0.70f);
            if (d.pauldron >= 2)
            {
                b.Ball(Sl.Metal, sp + new Vector3(sgn * 0.016f, c.Y(-0.055f), 0f), 0.066f * Mathf.Lerp(1f, W, 0.7f), 6, 10, 0.46f);
                b.Ball(Sl.Metal, sp + new Vector3(sgn * 0.026f, c.Y(-0.105f), 0f), 0.056f * Mathf.Lerp(1f, W, 0.7f), 6, 10, 0.40f);
                b.Box(Sl.Gold, sp + new Vector3(sgn * 0.012f, c.Y(0.030f), 0f), new Vector3(0.002f, 0.002f, 0.002f));
            }
        }
        if (lower) ArmProps(b, c, sgn, hp);
    }

    static T[] Sub<T>(T[] a, int i0, int i1)
    {
        var r = new T[i1 - i0 + 1];
        for (int i = i0; i <= i1; i++) r[i - i0] = a[i];
        return r;
    }

    // =====================================================================
    // Ekor (beastfolk)
    // =====================================================================
    static void BuildTail(GeoBag b, Ctx c)
    {
        Vector3 p = c.Pivot(PTail);
        float W = c.W;
        int k = c.s.beast;
        float len = k == 1 ? 0.55f : (k == 2 ? 0.62f : (k == 0 ? 0.6f : 0f));
        if (k == 3)       // kelinci: ekor bulat
        {
            b.Ball(Sl.Fur, p + new Vector3(0f, 0.02f, -0.07f), 0.085f, 5, 8, 0.95f);
            return;
        }
        int n = 7;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)(n - 1);
            float thick = k == 2 ? Mathf.Lerp(0.045f, 0.085f, Mathf.Sin(t * 3.0f)) + 0.012f : (k == 0 ? 0.034f - t * 0.008f : 0.05f - t * 0.014f);
            if (k == 2 && t > 0.8f) thick = 0.06f - (t - 0.8f) * 0.2f;
            Vector3 c0 = p + new Vector3(0f, 0.04f + t * (k == 0 ? 0.26f : 0.12f) - (k == 1 ? t * t * 0.14f : 0f), -0.04f - t * len * 0.7f);
            int slot = (t > 0.82f && (k == 2 || k == 0) && c.s.skin % 2 == 0) ? Sl.EyeW : Sl.Fur;
            b.Ball(slot, c0, thick, 4, 7, 1.0f);
        }
    }
}
