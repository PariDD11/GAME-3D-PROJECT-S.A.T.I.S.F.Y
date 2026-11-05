using System.Collections.Generic;
using UnityEngine;

public static partial class NpcKit
{
    // titik di permukaan kepala: phi = lintang (-90 dagu .. +90 ubun), theta = bujur (0 = depan)
    const float SQ = 2.45f;                      // eksponen superelips: 2 = bola, lebih besar = lebih kotak
    static float SPow(float v, float e) { return Mathf.Sign(v) * Mathf.Pow(Mathf.Abs(v), e); }

    static Vector3 HP(Ctx c, float phiDeg, float thetaDeg, float grow = 0f)
    {
        float ph = phiDeg * Mathf.Deg2Rad, th = thetaDeg * Mathf.Deg2Rad;
        float ee = 2f / SQ;
        float f = SPow(Mathf.Cos(ph), ee), sn = Mathf.Sin(ph);
        float chinK = ChinK(c);
        float chin = sn < 0f ? 1f - chinK * Mathf.Pow(-sn, 1.3f) : 1f;
        float rx = c.hr * 0.97f + grow, ry = c.hr * 1.0f + grow, rz = c.hr * 0.97f + grow;
        float z = SPow(Mathf.Cos(th), ee) * f * rz;
        if (sn < 0f && Mathf.Cos(th) > 0f) z += 0.012f * Mathf.Pow(-sn, 1.5f) * Mathf.Cos(th);
        float hcy = c.Y(1.46f) + c.hr * 0.92f;
        return new Vector3(SPow(Mathf.Sin(th), ee) * f * rx * chin, hcy + SPow(sn, ee) * ry, z);
    }

    static float HeadCY(Ctx c) { return c.Y(1.46f) + c.hr * 0.92f; }

    /// <summary>Seberapa runcing dagu (anime: dagu lancip, terutama wanita).</summary>
    static float ChinK(Ctx c) { return ClassicHead ? (c.fem ? 0.16f : 0.07f) : (c.fem ? 0.21f : 0.11f); }

    static Vector3 SurfaceAt(Ctx c, float x, float yRel, float grow, out float yawDeg)
    {
        float rx = c.hr * 0.97f + grow, ry = c.hr * 1.0f + grow, rz = c.hr * 0.97f + grow;
        float vv = Mathf.Min(1f, Mathf.Abs(yRel / ry));
        float chinF = yRel < 0f ? 1f - ChinK(c) * Mathf.Pow(Mathf.Pow(vv, 1.225f), 1.3f) : 1f;       // sama dengan penyempitan dagu pada HP()
        rx *= chinF;
        float u = Mathf.Min(1f, Mathf.Abs(x / rx)), v = Mathf.Min(1f, Mathf.Abs(yRel / ry));
        float k = Mathf.Max(0.02f, 1f - Mathf.Pow(u, SQ) - Mathf.Pow(v, SQ));
        float z = rz * Mathf.Pow(k, 1f / SQ);
        float slope = rz * Mathf.Pow(k, 1f / SQ - 1f) * Mathf.Pow(u, SQ - 1f) * Mathf.Sign(x) / rx;      // -dz/dx
        yawDeg = Mathf.Atan(slope) * Mathf.Rad2Deg;
        return new Vector3(x, HeadCY(c) + yRel, z);
    }

    static void Ellipse(GeoBag b, int slot, Vector3 center, float rw, float rh, float rot, float yaw, int n = 10)
    {
        b.Push(center, yaw);
        float cr = Mathf.Cos(rot * Mathf.Deg2Rad), sr = Mathf.Sin(rot * Mathf.Deg2Rad);
        for (int i = 0; i < n; i++)
        {
            float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
            Vector2 p0 = new Vector2(Mathf.Cos(a0) * rw, Mathf.Sin(a0) * rh), p1 = new Vector2(Mathf.Cos(a1) * rw, Mathf.Sin(a1) * rh);
            Vector3 q0 = new Vector3(p0.x * cr - p0.y * sr, p0.x * sr + p0.y * cr, 0f), q1 = new Vector3(p1.x * cr - p1.y * sr, p1.x * sr + p1.y * cr, 0f);
            b.Tri(slot, Vector3.zero, q0, q1);
        }
        b.Pop();
    }

    // =====================================================================
    // Kepala
    // =====================================================================
    static void BuildHead(GeoBag b, Ctx c)
    {
        Spec s = c.s; float hr = c.hr; float hcy = HeadCY(c);
        bool fem = c.fem;
        // leher (dibangun dalam ruang kepala; hasil akhir ~ radius 0.05 m, pria sedikit lebih tebal)
        float nk = 1f / Mathf.Max(0.2f, c.hk), nw = Mathf.Lerp(1f, c.W, 0.8f) * (fem ? 0.88f : 1.08f);
        float ny0 = c.Y(1.46f) - 0.075f * nk;
        Tube(b, Sl.SkinS, new[] { new Vector3(0f, ny0, 0f), new Vector3(0f, c.Y(1.46f) + hr * 0.2f, 0.004f), new Vector3(0f, c.Y(1.46f) + hr * 0.55f, 0.008f) },
            new[] { 0.052f * nk * nw, 0.047f * nk * nw, 0.046f * nk * nw }, new[] { 0.050f * nk * nw, 0.045f * nk * nw, 0.044f * nk * nw }, 14, false, false);
        // tengkorak (superelips, resolusi tinggi)
        var rings = new List<Vector3[]>();
        float[] phis = { -90f, -78f, -62f, -46f, -30f, -14f, 0f, 14f, 30f, 46f, 62f, 78f, 90f };
        foreach (float ph in phis)
        {
            var r = new Vector3[28];
            for (int j = 0; j < 28; j++) r[j] = HP(c, ph, j * (360f / 28f));
            rings.Add(r);
        }
        NLoft(b, Sl.SkinS, rings, false, false);

        if (ClassicHead) Face(b, c, fem); else AnimeFace(b, c);
        Ears(b, c);
        if (s.beard > 0) { if (ClassicHead) Beard(b, c); else AnimeBeard(b, c); }
        if (ClassicHead) Hair(b, c); else AnimeHair(b, c);
        Headgear(b, c);
    }

    // elips yang menempel di permukaan kepala (setiap titik diproyeksikan ke bola kepala)
    static void SurfEllipse(GeoBag b, Ctx c, int slot, float cx, float cy, float rw, float rh, float rot, float off, int n = 14, float skewTop = 0f)
    {
        float cr = Mathf.Cos(rot * Mathf.Deg2Rad), sr = Mathf.Sin(rot * Mathf.Deg2Rad);
        float yw;
        Vector3 ctr = SurfaceAt(c, cx, cy, off, out yw);
        Vector3 prev = Vector3.zero;
        for (int i = 0; i <= n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            float px = Mathf.Cos(a) * rw, py = Mathf.Sin(a) * rh;
            if (py > 0f) px += skewTop * py;
            float qx = px * cr - py * sr, qy = px * sr + py * cr;
            Vector3 q = SurfaceAt(c, cx + qx, cy + qy, off, out yw);
            if (i > 0) b.Tri(slot, ctr, prev, q);
            prev = q;
        }
    }

    // Wajah bersih ala mannequin ramah: mata oval gelap berkilau, senyum, alis; wanita: bulu mata + pipi merona.
    static void Face(GeoBag b, Ctx c, bool fem)
    {
        Spec s = c.s; float hr = c.hr;
        float ew = hr * (fem ? 0.100f : 0.090f), eh = hr * (fem ? 0.165f : 0.150f);
        float ey = hr * 0.02f, ex = hr * 0.36f;
        for (int sg = -1; sg <= 1; sg += 2)
        {
            SurfEllipse(b, c, Sl.Dark, sg * ex, ey, ew, eh, sg * (fem ? -4f : 0f), 0.0040f, 16);
            SurfEllipse(b, c, Sl.EyeW, sg * ex - sg * ew * 0.18f, ey + eh * 0.38f, ew * 0.30f, eh * 0.22f, 0f, 0.0062f, 8);       // kilau
            if (fem)
            {
                SurfEllipse(b, c, Sl.Dark, sg * (ex + ew * 1.15f), ey + eh * 0.72f, ew * 0.62f, eh * 0.09f, sg * -28f, 0.0050f, 8);     // bulu mata
                SurfEllipse(b, c, Sl.Dark, sg * (ex + ew * 0.95f), ey + eh * 1.12f, ew * 0.55f, eh * 0.075f, sg * -48f, 0.0050f, 8);
            }
            // alis
            float byw; Vector3 br = SurfaceAt(c, sg * ex * 1.02f, ey + eh * (fem ? 2.0f : 1.75f), 0.0045f, out byw);
            b.Push(br, byw);
            b.Box(Sl.Hair, Vector3.zero, new Vector3(ew * 2.4f, hr * (fem ? 0.030f : 0.052f), 0.006f), sg * (fem ? 9f : 4f) * (s.race == Elf ? 1.6f : 1f));
            b.Pop();
        }
        // hidung kecil
        float nyaw; Vector3 np = SurfaceAt(c, 0f, -hr * 0.20f, 0f, out nyaw);
        if (s.race == Beast && s.beast != 3) b.Ball(Sl.Dark, np + new Vector3(0f, -hr * 0.10f, 0.012f), hr * 0.07f, 4, 8);
        else
        {
            float nr = hr * (s.race == Dwarf ? 0.12f : 0.062f);
            Sphere(b, Sl.SkinS, np + new Vector3(0f, -hr * 0.08f, nr * 0.6f), nr * 0.8f, nr * 1.1f, nr * 1.0f, 4, 10);
        }
        // senyum: busur tebal
        for (int i = -3; i <= 3; i++)
        {
            float t = i / 3f; float yw;
            Vector3 mp = SurfaceAt(c, t * hr * 0.22f, -hr * (0.50f + 0.075f * (1f - t * t)), 0.0035f, out yw);
            b.Push(mp, yw); b.Box(Sl.Dark, Vector3.zero, new Vector3(hr * 0.075f, hr * (fem ? 0.034f : 0.040f), 0.004f), t * 28f); b.Pop();
        }
        if (fem) SurfEllipse(b, c, Sl.Lip, 0f, -hr * 0.575f, hr * 0.15f, hr * 0.032f, 0f, 0.0030f, 10);      // sedikit warna bibir
        // kumis tipis untuk pria berjenggot
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
        // pipi merona
        for (int sg = -1; sg <= 1; sg += 2)
            SurfEllipse(b, c, Sl.Lip, sg * hr * 0.58f, -hr * 0.30f, hr * (fem ? 0.14f : 0.10f), hr * (fem ? 0.075f : 0.05f), 0f, 0.0028f, 8);
    }

    static Vector3 Dir(float yawDeg) { return new Vector3(Mathf.Sin(yawDeg * Mathf.Deg2Rad), 0f, Mathf.Cos(yawDeg * Mathf.Deg2Rad)); }

    static void Ears(GeoBag b, Ctx c)
    {
        Spec s = c.s; float hr = c.hr; float hcy = HeadCY(c);
        float rxs = hr * 0.97f;
        for (int sg = -1; sg <= 1; sg += 2)
        {
            Vector3 root = new Vector3(sg * rxs * 0.97f, hcy - hr * 0.05f, -hr * 0.05f);
            switch (s.race)
            {
                case Elf:
                    Pyramid(b, Sl.SkinS, root + new Vector3(0f, hr * 0.22f, 0.035f), root + new Vector3(0f, -hr * 0.18f, 0.02f), root + new Vector3(0f, hr * 0.06f, -0.05f),
                        root + new Vector3(sg * hr * 0.55f, hr * 0.30f, -0.05f));
                    break;
                case Dwarf:
                    Sphere(b, Sl.SkinS, root + new Vector3(sg * 0.012f, 0f, 0f), 0.014f, 0.036f, 0.026f, 4, 8);
                    break;
                case Beast:
                    BeastEars(b, c, sg);
                    break;
                default:
                    Sphere(b, Sl.SkinS, root + new Vector3(sg * 0.008f, 0f, 0f), 0.010f, 0.030f, 0.020f, 4, 8);
                    break;
            }
        }
    }

    static void BeastEars(GeoBag b, Ctx c, int sg)
    {
        float hr = c.hr; float hcy = HeadCY(c);
        int k = c.s.beast;
        if (k == 3)
        {
            float bx = sg * hr * 0.30f, by = hcy + hr * 0.80f;
            var path = new[] { new Vector3(bx, by, -0.01f), new Vector3(bx + sg * hr * 0.10f, by + hr * 1.1f, -0.02f), new Vector3(bx + sg * hr * 0.22f, by + hr * 2.1f, -0.03f), new Vector3(bx + sg * hr * 0.26f, by + hr * 2.7f, -0.03f) };
            var rxx = new[] { hr * 0.15f, hr * 0.16f, hr * 0.14f, hr * 0.07f };
            var rzz = new[] { hr * 0.08f, hr * 0.085f, hr * 0.075f, hr * 0.04f };
            Tube(b, Sl.Fur, path, rxx, rzz, 14, false, true);
            var ip = new[] { path[0] + new Vector3(0f, hr * 0.15f, 0.022f), path[1] + new Vector3(0f, 0f, 0.026f), path[2] + new Vector3(0f, 0f, 0.022f), path[3] + new Vector3(0f, -hr * 0.1f, 0.012f) };
            var irx = new[] { hr * 0.08f, hr * 0.09f, hr * 0.08f, hr * 0.03f };
            var irz = new[] { hr * 0.04f, hr * 0.045f, hr * 0.04f, hr * 0.02f };
            Tube(b, Sl.Lip, ip, irx, irz, 12, false, true);
            return;
        }
        float x = sg * hr * (k == 3 ? 0.30f : 0.58f);
        float baseY = hcy + hr * (k == 3 ? 0.88f : 0.78f);
        float zc = k == 3 ? -0.01f : -hr * 0.05f;
        float h = k == 0 ? hr * 1.1f : (k == 1 ? hr * 1.25f : (k == 2 ? hr * 1.45f : hr * 3.0f));
        float w = k == 3 ? hr * 0.28f : hr * 0.42f;
        float lean = k == 3 ? sg * hr * 0.18f : sg * hr * (k == 1 ? 0.14f : 0.2f);
        Vector3 p0 = new Vector3(x - w * 0.5f * sg, baseY - 0.02f, zc + 0.03f), p1 = new Vector3(x + w * 0.5f * sg, baseY - 0.02f, zc + 0.03f), p2 = new Vector3(x, baseY - 0.02f, zc - 0.045f);
        Vector3 apex = new Vector3(x + lean, baseY + h, zc - (k == 3 ? 0.05f : 0.0f));
        Pyramid(b, Sl.Fur, p0, p1, p2, apex);
        // bagian dalam telinga (pink) di sisi depan
        Vector3 q0 = p0 * 0.8f + p1 * 0.2f + new Vector3(0f, 0.008f, 0.004f), q1 = p0 * 0.2f + p1 * 0.8f + new Vector3(0f, 0.008f, 0.004f);
        Vector3 qa = Vector3.Lerp(new Vector3(x, baseY, zc + 0.03f), apex, 0.72f) + new Vector3(0f, 0f, 0.006f);
        b.Tri(Sl.Lip, q0, qa, q1);
        b.Tri(Sl.Lip, q0, q1, qa);
        if (k == 1 || k == 2) b.Ball(Sl.Fur, new Vector3(sg * hr * 0.88f, hcy - hr * 0.35f, 0.0f), hr * 0.12f, 3, 6);        // jumbai pipi
    }

    // =====================================================================
    // Rambut
    // =====================================================================
    static void HairShell(GeoBag b, Ctx c, int slot, float grow, float botFront, float botBack, float botSide, int K = 10, float sweep = 0f)
    {
        var rings = new List<Vector3[]>();
        const int N = 28;
        for (int k = 0; k <= K; k++)
        {
            float t = k / (float)K;
            var r = new Vector3[N];
            for (int j = 0; j < N; j++)
            {
                float th = j * (360f / N);
                float cth = Mathf.Cos(th * Mathf.Deg2Rad), sth = Mathf.Sin(th * Mathf.Deg2Rad);
                float front = botFront + sweep * sth;
                float bot = cth > 0f ? Mathf.Lerp(botSide, front, Mathf.Pow(cth, 0.8f)) : Mathf.Lerp(botSide, botBack, Mathf.Pow(-cth, 0.8f));
                float ph = Mathf.Lerp(90f, bot, Mathf.Pow(t, 0.9f));
                r[j] = HP(c, ph, th, grow * (0.85f + 0.15f * Mathf.Sin(t * Mathf.PI * 0.5f)));
            }
            rings.Add(r);
        }
        NLoft(b, slot, rings, false, false);
    }

    static void Bangs(GeoBag b, Ctx c, int slot, int count, float tipPhi, float grow)
    {
        Vector3 center = new Vector3(0f, HeadCY(c), 0f);
        for (int i = 0; i < count; i++)
        {
            float t0 = -55f + i * (110f / count), t1 = t0 + 110f / count;
            Vector3 a = HP(c, 34f, t0, grow), bb = HP(c, 34f, t1, grow);
            float tipTh = (t0 + t1) / 2f + ((i % 2 == 0) ? 4f : -4f);
            Vector3 tip = HP(c, tipPhi + (i % 3) * 7f, tipTh, grow + 0.008f);
            TriOut(b, slot, a, bb, tip, center);
        }
    }

    static void SideLocks(GeoBag b, Ctx c, int style)
    {
        float hr = c.hr; float hcy = HeadCY(c);
        if (style == 5 || style == 8) return;
        Vector3 cen = new Vector3(0f, hcy, 0f);
        for (int sg = -1; sg <= 1; sg += 2)
        {
            float len = (style == 1 || style == 4) ? 0.9f : 0.55f;
            for (int k = 0; k < 2; k++)
            {
                float th = sg * (66f + k * 14f);
                Vector3 a = HP(c, 30f, th - 7f, 0.028f), bb = HP(c, 30f, th + 7f, 0.028f);
                Vector3 tip = HP(c, -10f - k * 12f, th + sg * 4f, 0.036f) - new Vector3(0f, hr * len * 0.3f, 0f);
                TriOut(b, Sl.Hair, a, bb, tip, cen);
            }
        }
    }

    static void Hair(GeoBag b, Ctx c)
    {
        Spec s = c.s; float hr = c.hr; float hcy = HeadCY(c);
        int style = s.hair;
        bool hooded = s.hat == 5, helmet = s.hat == 4 || s.hat == 16;
        if (s.race == Beast && s.beast == 3 && style == 5) style = 0;
        if (hooded) { HairShell(b, c, Sl.Hair, 0.018f, 26f, -10f, 0f); return; }
        if (style == 8 && s.race != Dwarf) return;                       // botak
        if (s.race == Dwarf && !c.fem && style > 4) style = style % 5;
        float g = 0.030f;
        float front = c.fem ? 20f : 28f;
        float sweep = c.fem ? 12f : 7f;
        float side = helmet ? 8f : (style == 4 ? -14f : (style == 1 || style == 7 ? -6f : 4f));
        float backLow = helmet ? 5f : (style == 4 || style == 1 || style == 7 ? -48f : -34f);
        HairShell(b, c, Sl.Hair, g, helmet ? 30f : front, backLow, side, 10, helmet ? 0f : sweep);
        if (!helmet && style != 5)
        {
            // volume lembut di atas dahi agar siluet membulat
            b.Ball(Sl.Hair, new Vector3(sg0(c) * hr * 0.12f, hcy + hr * 0.86f, hr * 0.22f), hr * 0.62f, 6, 14, 0.45f);
        }
        switch (style)
        {
            case 1:      // panjang lurus
            case 4:      // bob
                {
                    bool bob = style == 4;
                    float yEnd = bob ? hcy - hr * 1.15f : c.Y(1.02f);
                    var rings = new List<Vector3[]>();
                    int rn = bob ? 6 : 12;
                    const int NR = 24;
                    for (int k = 0; k <= rn; k++)
                    {
                        float t = k / (float)rn;
                        float y = Mathf.Lerp(hcy + hr * 0.15f, yEnd, t);
                        float w = hr * (1.10f + (bob ? 0.10f * Mathf.Sin(t * 2.2f) : 0.12f * Mathf.Sin(t * 2.6f) - 0.16f * t * t));
                        float d = hr * (1.04f - 0.22f * t) + 0.012f;
                        var r = new Vector3[NR];
                        for (int j = 0; j < NR; j++)
                        {
                            float a = j * Mathf.PI * 2f / NR;
                            float cs = Mathf.Cos(a);
                            float z = cs < 0f ? cs * d : Mathf.Lerp(-0.01f, 0.18f * hr, Mathf.Pow(cs, 3f) * (1f - t));
                            float x = Mathf.Sin(a) * w * (cs < 0f ? 1f : Mathf.Lerp(0.86f, 0.98f, t));
                            r[j] = new Vector3(x, y, z - 0.005f);
                        }
                        rings.Add(r);
                    }
                    NLoft(b, Sl.Hair, rings, false, true);
                }
                break;
            case 2:      // kuncir kuda
                {
                    var rings = new List<Vector3[]>();
                    Vector3[] path = { new Vector3(0f, hcy + hr * 0.55f, -hr * 0.85f), new Vector3(0f, hcy + hr * 0.45f, -hr * 1.35f), new Vector3(0f, hcy - hr * 0.1f, -hr * 1.6f), new Vector3(0f, hcy - hr * 0.9f, -hr * 1.55f), new Vector3(0f, hcy - hr * 1.7f, -hr * 1.35f) };
                    float[] rad = { 0.032f, 0.052f, 0.064f, 0.055f, 0.030f };
                    Tube(b, Sl.Hair, path, rad, rad, 12, true, true);
                    b.Ball(Sl.CoatC, path[0] + new Vector3(0f, 0f, -0.03f), 0.034f, 4, 8);
                }
                break;
            case 3:      // dua kuncir
                for (int sg = -1; sg <= 1; sg += 2)
                {
                    Vector3[] path = { new Vector3(sg * hr * 0.95f, hcy + hr * 0.1f, -hr * 0.2f), new Vector3(sg * hr * 1.25f, hcy - hr * 0.5f, -hr * 0.25f), new Vector3(sg * hr * 1.3f, hcy - hr * 1.4f, -hr * 0.15f), new Vector3(sg * hr * 1.2f, hcy - hr * 2.4f, -hr * 0.05f), new Vector3(sg * hr * 1.1f, hcy - hr * 3.0f, 0.0f) };
                    float[] rad = { 0.036f, 0.058f, 0.054f, 0.042f, 0.028f };
                    Tube(b, Sl.Hair, path, rad, rad, 12, true, true);
                    b.Ball(Sl.CoatC, path[0] + new Vector3(sg * 0.012f, 0.02f, 0f), 0.032f, 4, 8);
                }
                break;
            case 5:      // afro bulat halus (tanpa duri)
                b.Ball(Sl.Hair, new Vector3(0f, hcy + hr * 0.30f, -hr * 0.05f), hr * 1.30f, 9, 20, 0.92f);
                break;
            case 6:      // sanggul
                b.Ball(Sl.Hair, new Vector3(0f, hcy + hr * 1.12f, -hr * 0.12f), hr * 0.42f, 7, 14);
                b.Beam(Sl.CoatC, new Vector3(-hr * 0.38f, hcy + hr * 0.86f, -hr * 0.12f), new Vector3(hr * 0.38f, hcy + hr * 0.86f, -hr * 0.12f), 0.02f, 0.02f);
                break;
            case 7:      // kepang panjang
                {
                    for (int i = 0; i < 12; i++)
                    {
                        float t = i / 11f;
                        b.Ball(Sl.Hair, new Vector3(0f, hcy - hr * 0.25f - t * hr * 3.6f, -hr * 1.02f - 0.02f * Mathf.Sin(t * 3f)), 0.046f * (1f - t * 0.45f), 4, 8, 0.85f);
                    }
                    b.Ball(Sl.CoatC, new Vector3(0f, hcy - hr * 3.95f, -hr * 1.05f), 0.032f, 4, 8);
                }
                break;
        }
    }

    static float sg0(Ctx c) { return c.fem ? -0.6f : 0.4f; }

    // =====================================================================
    // Janggut
    // =====================================================================
    static void Beard(GeoBag b, Ctx c)
    {
        Spec s = c.s; float hr = c.hr;
        float hcy = HeadCY(c);
        int type = s.beard;      // 1 jenggot tipis, 2 penuh pendek, 3 panjang berkepang (dwarf)
        var rings = new List<Vector3[]>();
        int K = 5;
        float top = type == 1 ? -42f : -18f;
        for (int k = 0; k <= K; k++)
        {
            float ph = Mathf.Lerp(-88f, top, k / (float)K);
            var r = new Vector3[16];
            for (int j = 0; j < 16; j++)
            {
                float th = j * 22.5f; float cth = Mathf.Cos(th * Mathf.Deg2Rad);
                float g = cth > -0.25f ? (type == 1 ? 0.007f : 0.016f) : -0.03f;
                r[j] = HP(c, ph, th, g);
            }
            rings.Add(r);
        }
        NLoft(b, Sl.Hair, rings, false, false);
        // kumis
        for (int sg = -1; sg <= 1; sg += 2)
        {
            float yw;
            Vector3 mp = SurfaceAt(c, sg * hr * 0.16f, -hr * 0.46f, 0.014f, out yw);
            b.Push(mp, yw); b.Box(Sl.Hair, new Vector3(sg * 0.012f, 0f, 0f), new Vector3(hr * 0.34f, hr * 0.07f, 0.02f), sg * -18f); b.Pop();
        }
        if (type >= 3 || (type == 2 && s.race == Dwarf))
        {
            Vector3 chin = HP(c, -86f, 0f, 0.016f);
            float len = type == 3 ? hr * 3.0f : hr * 1.5f;
            var cen = new List<Vector3>(); var rx = new List<float>(); var rz = new List<float>();
            int n = 6;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                cen.Add(new Vector3(0f, chin.y + hr * 0.2f - t * len, chin.z - 0.01f + 0.01f * Mathf.Sin(t * 3f)));
                rx.Add(hr * Mathf.Lerp(0.66f, 0.1f, Mathf.Pow(t, 0.8f)));
                rz.Add(hr * Mathf.Lerp(0.34f, 0.1f, t));
            }
            Tube(b, Sl.Hair, cen.ToArray(), rx.ToArray(), rz.ToArray(), 10, false, true);
            if (type == 3)
            {
                for (int sg = -1; sg <= 1; sg += 2)
                {
                    b.Ball(Sl.Gold, new Vector3(sg * hr * 0.28f, chin.y - len * 0.55f, chin.z + 0.04f), 0.022f, 3, 6);
                    b.Ball(Sl.Gold, new Vector3(sg * hr * 0.28f, chin.y - len * 0.2f, chin.z + 0.07f), 0.018f, 3, 6);
                }
            }
        }
    }

    // =====================================================================
    // Penutup kepala
    // =====================================================================
    static void Headgear(GeoBag b, Ctx c)
    {
        Spec s = c.s; float hr = c.hr; float hcy = HeadCY(c);
        Vector3 top = new Vector3(0f, hcy + hr * 0.62f, 0f);
        switch (s.hat)
        {
            case 1:      // topi jerami
                b.Cyl(Sl.Thatch, top + new Vector3(0f, 0.01f, 0f), hr * 2.2f, 0.016f, 18, hr * 2.2f, true, true);
                b.Cyl(Sl.Thatch, top + new Vector3(0f, 0.02f, 0f), hr * 1.1f, hr * 0.78f, 14, hr * 0.92f, true, false);
                b.Cyl(Sl.CoatB, top + new Vector3(0f, 0.03f, 0f), hr * 1.12f, 0.03f, 14, hr * 1.12f, false, false);
                break;
            case 2:      // baret pedagang
                b.Ball(Sl.CoatA, top + new Vector3(hr * 0.15f, hr * 0.12f, 0f), hr * 1.2f, 5, 10, 0.5f);
                b.Cyl(Sl.CoatB, top + new Vector3(0f, -0.01f, 0f), hr * 1.02f, 0.03f, 14, hr * 1.02f, false, false);
                b.Box(Sl.CoatC, top + new Vector3(hr * 0.6f, hr * 0.35f, hr * 0.1f), new Vector3(0.012f, 0.14f, 0.03f), 0f);
                break;
            case 3:      // topi penyihir
                b.Cyl(Sl.CoatA, top + new Vector3(0f, -0.02f, 0f), hr * 1.9f, 0.018f, 18, hr * 1.9f, true, true);
                b.Cone(Sl.CoatA, top + new Vector3(0f, -0.005f, 0f), hr * 1.12f, hr * 3.0f, 14);
                b.Cyl(Sl.CoatC, top + new Vector3(0f, 0.0f, 0f), hr * 1.12f, 0.05f, 14, hr * 1.1f, false, false);
                b.Box(Sl.Gold, top + new Vector3(0f, 0.025f, hr * 1.12f), new Vector3(0.05f, 0.05f, 0.01f));
                b.Ball(Sl.Gold, top + new Vector3(hr * 0.1f, hr * 2.95f, 0f), 0.02f, 3, 6);
                break;
            case 4:      // helm
                {
                    bool knight = s.outfit == OKsatria || s.armHead == 3;
                    b.Dome(Sl.Metal, new Vector3(0f, hcy + hr * 0.05f, 0f), hr * 1.14f, 5, 14, 0.95f);
                    b.Cyl(Sl.Metal, new Vector3(0f, hcy + hr * 0.02f, 0f), hr * 1.16f, hr * 0.38f, 14, hr * 1.16f, false, false);
                    b.Cyl(Sl.Gold, new Vector3(0f, hcy + hr * 0.30f, 0f), hr * 1.17f, 0.016f, 14, hr * 1.17f, true, true);
                    b.Box(Sl.Metal, new Vector3(0f, hcy - hr * 0.1f, hr * 1.0f), new Vector3(0.026f, hr * 0.9f, 0.016f));        // pelindung hidung
                    b.Box(Sl.Metal, new Vector3(hr * 1.0f, hcy - hr * 0.25f, hr * 0.15f), new Vector3(0.016f, hr * 0.7f, hr * 0.9f));
                    b.Box(Sl.Metal, new Vector3(-hr * 1.0f, hcy - hr * 0.25f, hr * 0.15f), new Vector3(0.016f, hr * 0.7f, hr * 0.9f));
                    if (knight)
                    {
                        b.Box(Sl.CoatC, new Vector3(0f, hcy + hr * 1.35f, -hr * 0.1f), new Vector3(0.03f, hr * 0.7f, hr * 1.6f));
                        b.Ball(Sl.CoatC, new Vector3(0f, hcy + hr * 1.2f, -hr * 0.9f), hr * 0.5f, 4, 6, 1.4f);
                    }
                    else if (s.armHead < 4) b.Cone(Sl.CoatA, new Vector3(0f, hcy + hr * 1.0f, 0f), 0.014f, hr * 0.55f, 4);
                    if (s.armHead >= 4)      // set area Eldrath: 4 Perbatasan (tanduk banteng), 5 Bara (jambul api), 6 Tulang (duri tulang), 7 Iblis (tanduk iblis)
                    {
                        foreach (float sx in new[] { -1f, 1f })
                        {
                            if (s.armHead == 4) { b.Beam(Sl.EyeW, new Vector3(sx * hr * 1.05f, hcy + hr * 0.5f, 0f), new Vector3(sx * hr * 1.9f, hcy + hr * 0.9f, 0.02f), 0.03f, 0.012f, Vector3.forward); b.Cone(Sl.EyeW, new Vector3(sx * hr * 1.9f, hcy + hr * 0.9f, 0.02f), 0.012f, hr * 0.8f, 5); }
                            else if (s.armHead == 5) { b.Cone(Sl.Glow, new Vector3(sx * hr * 0.35f, hcy + hr * 1.0f, -hr * 0.1f), 0.02f, hr * 0.95f, 5); }
                            else if (s.armHead == 6) { for (int k = 0; k < 3; k++) b.Cone(Sl.EyeW, new Vector3(sx * hr * (0.5f + k * 0.3f), hcy + hr * (0.95f - k * 0.15f), -hr * 0.1f), 0.018f, hr * 0.55f, 4); }
                            else { b.Beam(Sl.DarkStone, new Vector3(sx * hr * 0.9f, hcy + hr * 0.8f, 0f), new Vector3(sx * hr * 1.3f, hcy + hr * 1.9f, -hr * 0.3f), 0.035f, 0.01f, Vector3.forward); b.Ball(Sl.Glow, new Vector3(0f, hcy + hr * 0.45f, hr * 1.05f), 0.012f, 3, 5); }
                        }
                        if (s.armHead == 5) b.Cone(Sl.Glow, new Vector3(0f, hcy + hr * 1.0f, -hr * 0.1f), 0.024f, hr * 1.3f, 5);
                    }
                }
                break;
            case 5:      // tudung
                HairShell(b, c, Sl.CoatC, 0.032f, 12f, -78f, -6f, 7);
                {
                    var rg = new List<Vector3[]>();
                    for (int k = 0; k <= 4; k++)
                    {
                        float y = Mathf.Lerp(hcy - hr * 0.3f, c.Y(1.32f), k / 4f);
                        var r = new Vector3[14];
                        for (int j = 0; j < 14; j++)
                        {
                            float a = j * Mathf.PI * 2f / 14f; float cs = Mathf.Cos(a);
                            r[j] = new Vector3(Mathf.Sin(a) * (hr * 1.2f + k * 0.03f), y, cs < 0f ? cs * (hr * 1.1f + k * 0.012f) - 0.01f : -0.03f);
                        }
                        rg.Add(r);
                    }
                    NLoft(b, Sl.CoatC, rg, false, false);
                }
                break;
            case 6:      // hiasan kepala pelayan / ikat kepala
                {
                    var rr = new List<Vector3[]>();
                    rr.Add(new Vector3[16]); rr.Add(new Vector3[16]);
                    for (int j = 0; j < 16; j++) { rr[0][j] = HP(c, 40f, j * 22.5f, 0.012f); rr[1][j] = HP(c, 56f, j * 22.5f, 0.014f); }
                    NLoft(b, Sl.EyeW, rr, false, false);
                    for (int i = -3; i <= 3; i++) b.Ball(Sl.EyeW, HP(c, 62f, i * 14f, 0.016f), 0.014f, 3, 5);
                    b.Beam(Sl.CoatA, HP(c, 50f, 90f, 0.016f), HP(c, 50f, 90f, 0.016f) + new Vector3(0.05f, -0.02f, -0.02f), 0.03f, 0.012f);
                }
                break;
            case 7:      // lingkar mahkota
                {
                    var rr = new List<Vector3[]>();
                    rr.Add(new Vector3[16]); rr.Add(new Vector3[16]);
                    for (int j = 0; j < 16; j++) { rr[0][j] = HP(c, 30f, j * 22.5f, 0.012f); rr[1][j] = HP(c, 40f, j * 22.5f, 0.012f); }
                    NLoft(b, Sl.Gold, rr, false, false);
                    b.Ball(Sl.Eye, HP(c, 36f, 0f, 0.018f), 0.018f, 3, 6);
                    for (int sg = -1; sg <= 1; sg += 2)
                        b.Beam(Sl.Gold, HP(c, 35f, sg * 40f, 0.012f), HP(c, 35f, sg * 40f, 0.012f) + new Vector3(sg * 0.01f, 0.05f, 0f), 0.01f, 0.01f);
                }
                break;
            case 9:      // topi berbulu bangsawan
                b.Cyl(Sl.CoatC, top + new Vector3(0f, 0f, 0f), hr * 1.55f, 0.016f, 16, hr * 1.55f, true, true);
                b.Cyl(Sl.CoatC, top + new Vector3(0f, 0.01f, 0f), hr * 1.05f, hr * 0.62f, 14, hr * 0.9f, true, false);
                b.Beam(Sl.EyeW, top + new Vector3(hr * 0.5f, hr * 0.5f, 0f), top + new Vector3(hr * 1.4f, hr * 1.3f, -hr * 0.5f), 0.025f, 0.01f);
                break;
            case 10:     // mitra pendeta
                b.Cone(Sl.CoatA, top + new Vector3(0f, -0.02f, 0f), hr * 0.95f, hr * 1.6f, 4);
                b.Cyl(Sl.Gold, top + new Vector3(0f, -0.02f, 0f), hr * 1.0f, 0.04f, 12, hr * 1.0f, false, false);
                b.Box(Sl.Gold, top + new Vector3(0f, hr * 0.5f, hr * 0.55f), new Vector3(0.012f, hr * 0.55f, 0.008f));
                b.Box(Sl.Gold, top + new Vector3(0f, hr * 0.6f, hr * 0.55f), new Vector3(hr * 0.36f, 0.012f, 0.008f));
                break;
            case 11:     // topi bard berbulu panjang
                b.Cyl(Sl.CoatA, top, hr * 1.35f, 0.016f, 16, hr * 1.35f, true, true);
                b.Dome(Sl.CoatA, top + new Vector3(0f, 0.01f, 0f), hr * 0.98f, 4, 12, 0.75f);
                b.Beam(Sl.CoatB, top + new Vector3(-hr * 0.8f, hr * 0.2f, 0f), top + new Vector3(-hr * 1.9f, hr * 1.0f, -hr * 0.4f), 0.04f, 0.012f);
                b.Beam(Sl.EyeW, top + new Vector3(-hr * 1.0f, hr * 0.35f, 0f), top + new Vector3(-hr * 1.9f, hr * 1.0f, -hr * 0.4f), 0.01f, 0.01f);
                break;
            case 12:     // topi koki
                b.Cyl(Sl.CoatA, top + new Vector3(0f, -0.04f, 0f), hr * 1.0f, hr * 0.5f, 14, hr * 1.1f, false, false);
                b.Ball(Sl.CoatA, top + new Vector3(0f, hr * 0.75f, 0f), hr * 1.22f, 5, 12, 0.7f);
                break;
            case 16:     // topi kulit berlapis (armor kepala tingkat 1)
                b.Dome(Sl.Leather, new Vector3(0f, hcy + hr * 0.05f, 0f), hr * 1.1f, 5, 14, 0.95f);
                b.Cyl(Sl.Leather, new Vector3(0f, hcy - hr * 0.05f, 0f), hr * 1.12f, hr * 0.34f, 14, hr * 1.12f, false, false);
                for (int sg = -1; sg <= 1; sg += 2) b.Box(Sl.Leather, new Vector3(sg * hr * 1.0f, hcy - hr * 0.35f, hr * 0.1f), new Vector3(0.02f, hr * 0.7f, hr * 0.55f));
                b.Box(Sl.Gold, new Vector3(0f, hcy + hr * 0.18f, hr * 1.1f), new Vector3(0.03f, 0.03f, 0.01f));
                b.Box(Sl.Leather, new Vector3(0f, hcy + hr * 0.62f, -hr * 0.2f), new Vector3(0.012f, 0.02f, hr * 1.3f));
                break;
            case 14:     // turban
                b.Ball(Sl.EyeW, new Vector3(0f, hcy + hr * 0.22f, 0f), hr * 1.18f, 6, 14, 0.78f);
                for (int i = 0; i < 3; i++) b.Cyl(Sl.CoatC, new Vector3(0f, hcy + hr * (0.1f + i * 0.27f), 0f), hr * (1.17f - i * 0.1f), hr * 0.07f, 14, hr * (1.17f - i * 0.1f), false, false);
                b.Ball(Sl.EyeW, new Vector3(hr * 0.5f, hcy + hr * 0.95f, hr * 0.6f), hr * 0.3f, 4, 8);
                break;
            case 15:     // kerudung kepala + ikat melingkar
                b.Dome(Sl.EyeW, new Vector3(0f, hcy + hr * 0.02f, 0f), hr * 1.08f, 6, 14, 0.98f);
                b.Cyl(Sl.Dark, new Vector3(0f, hcy + hr * 0.52f, 0f), hr * 1.1f, hr * 0.14f, 14, hr * 1.1f, false, false);
                for (int sg = -1; sg <= 1; sg += 2) b.Beam(Sl.EyeW, new Vector3(sg * hr * 0.95f, hcy + hr * 0.2f, -hr * 0.1f), new Vector3(sg * hr * 1.25f, hcy - hr * 1.4f, -hr * 0.2f), hr * 0.3f, hr * 0.12f);
                break;
            case 13:     // bandana / ikat kepala
                {
                    var rr = new List<Vector3[]>();
                    rr.Add(new Vector3[16]); rr.Add(new Vector3[16]);
                    for (int j = 0; j < 16; j++) { rr[0][j] = HP(c, 28f, j * 22.5f, 0.014f); rr[1][j] = HP(c, 42f, j * 22.5f, 0.016f); }
                    NLoft(b, s.outfit == OPandai ? Sl.Leather : Sl.CoatC, rr, false, false);
                    b.Box(Sl.CoatC, new Vector3(hr * 0.6f, hcy + hr * 0.4f, -hr * 1.0f), new Vector3(0.05f, 0.12f, 0.01f), 20f);
                    if (s.outfit == OPandai)    // kacamata las di dahi
                    {
                        b.Cyl(Sl.Metal, new Vector3(-hr * 0.32f, hcy + hr * 0.9f, hr * 0.7f), 0.032f, 0.02f, 8, 0.032f, true, true);
                        b.Cyl(Sl.Metal, new Vector3(hr * 0.32f, hcy + hr * 0.9f, hr * 0.7f), 0.032f, 0.02f, 8, 0.032f, true, true);
                    }
                }
                break;
        }
    }
}
