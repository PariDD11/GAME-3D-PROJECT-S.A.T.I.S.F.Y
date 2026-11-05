using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tembok kota Ashwatch: jalur tembok mengikuti medan (dasar landai, korbel, parapet berbenteng, pilaster), menara tembok, lengkung sungai berterali.
/// Koordinat dunia langsung (tanpa Push); y titik jalur = tinggi tanah. Gerbang ada di Town2Gate.cs.
/// </summary>
public static partial class T2Kit
{
    public struct WPt { public float x, y, z; }

    static Vector3 O(Vector3 p, Vector3 n, float k, float y) { return p + n * k + Vector3.up * y; }

    /// <summary>Hasil bagi ulang jalur tembok: titik tiap ~step meter, titik awal tiap ruas asli tetap ada.</summary>
    public static List<WPt> Resample(IList<Vector2> pts, System.Func<float, float, float> ground, float step, bool closed)
    {
        var res = new List<WPt>();
        int n = pts.Count;
        int last = closed ? n : n - 1;
        for (int i = 0; i < last; i++)
        {
            Vector2 a = pts[i], c = pts[(i + 1) % n];
            float L = (c - a).magnitude;
            int m = Mathf.Max(1, Mathf.CeilToInt(L / step));
            for (int k = 0; k < m; k++)
            {
                Vector2 p = Vector2.Lerp(a, c, k / (float)m);
                res.Add(new WPt { x = p.x, y = ground(p.x, p.y), z = p.y });
            }
        }
        if (!closed) { Vector2 e = pts[n - 1]; res.Add(new WPt { x = e.x, y = ground(e.x, e.y), z = e.y }); }
        return res;
    }

    /// <summary>
    /// Satu jalur tembok (terbuka) dari titik s[0..n-1]. outerSign = +1 bila sisi luar (benteng) ada di kiri arah jalan (-tz, tx), -1 di kanan.
    /// thick = tebal, h = tinggi di atas tanah. Mengembalikan arclength total.
    /// </summary>
    public static float WallRun(GeoBag b, IList<WPt> s, float outerSign, float thick, float h, int seed, bool capStart, bool capEnd)
    {
        int n = s.Count;
        if (n < 2) return 0f;
        float ht = thick * 0.5f;
        var top = new float[n];
        for (int i = 0; i < n; i++)
        {
            float sum = 0f; int c = 0;
            for (int j = -3; j <= 3; j++) { int q = Mathf.Clamp(i + j, 0, n - 1); sum += s[q].y; c++; }
            top[i] = Mathf.Max(s[i].y + h * 0.82f, sum / c + h);
        }
        var cum = new float[n];
        for (int i = 1; i < n; i++) cum[i] = cum[i - 1] + new Vector2(s[i].x - s[i - 1].x, s[i].z - s[i - 1].z).magnitude;
        // normal luar per titik dengan sambungan miter (tidak ada celah di tikungan)
        var nrm = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            Vector3 d0 = i > 0 ? new Vector3(s[i].x - s[i - 1].x, 0f, s[i].z - s[i - 1].z) : Vector3.zero;
            Vector3 d1 = i + 1 < n ? new Vector3(s[i + 1].x - s[i].x, 0f, s[i + 1].z - s[i].z) : Vector3.zero;
            if (d0.sqrMagnitude < 1e-6f) d0 = d1; if (d1.sqrMagnitude < 1e-6f) d1 = d0;
            d0 = d0.normalized; d1 = d1.normalized;
            Vector3 n0 = new Vector3(-d0.z, 0f, d0.x) * outerSign, n1 = new Vector3(-d1.z, 0f, d1.x) * outerSign;
            Vector3 m = n0 + n1; float ml = m.magnitude;
            if (ml < 0.05f) { nrm[i] = n0; continue; }
            m /= ml;
            float cosH = Mathf.Max(0.45f, Vector3.Dot(m, n0));
            nrm[i] = m / cosH;
        }
        for (int i = 0; i + 1 < n; i++)
        {
            Vector3 p0 = new Vector3(s[i].x, 0f, s[i].z), p1 = new Vector3(s[i + 1].x, 0f, s[i + 1].z);
            Vector3 t = (p1 - p0); float L = t.magnitude; if (L < 0.01f) continue; t /= L;
            Vector3 no = new Vector3(-t.z, 0f, t.x) * outerSign;                    // arah luar (ruas)
            Vector3 n0 = nrm[i], n1 = nrm[i + 1];                                   // normal miter di kedua ujung
            float g0 = s[i].y, g1 = s[i + 1].y, t0 = top[i], t1 = top[i + 1];
            float yb0 = g0 - 1.3f, yb1 = g1 - 1.3f;
            float yp0 = Mathf.Min(g0 + 2.0f, t0 - 4f), yp1 = Mathf.Min(g1 + 2.0f, t1 - 4f);
            float yc0 = t0 - 1.15f, yc1 = t1 - 1.15f;
            // sisi luar: landai dasar -> dinding tegak -> korbel menonjol (n76: parapet menjorok pj m dengan penopang korbel = machicolation)
            const float pj = 0.85f;
            V2Kit.QOut(b, Sl.DarkStone, O(p0, n0, ht + 0.7f, yb0), O(p1, n1, ht + 0.7f, yb1), O(p1, n1, ht + 0.06f, yp1), O(p0, n0, ht + 0.06f, yp0), no);
            V2Kit.QOut(b, Sl.Stone, O(p0, n0, ht, yp0), O(p1, n1, ht, yp1), O(p1, n1, ht, yc1), O(p0, n0, ht, yc0), no);
            V2Kit.QOut(b, Sl.DarkStone, O(p0, n0, ht, yc0), O(p1, n1, ht, yc1), O(p1, n1, ht + pj, yc1 + 0.3f), O(p0, n0, ht + pj, yc0 + 0.3f), no);
            V2Kit.QOut(b, Sl.DarkStone, O(p0, n0, ht + pj, yc0 + 0.3f), O(p1, n1, ht + pj, yc1 + 0.3f), O(p1, n1, ht + pj, t1), O(p0, n0, ht + pj, t0), no);
            V2Kit.QOut(b, Sl.DarkStone, O(p0, n0, ht + pj, yc0), O(p1, n1, ht + pj, yc1), O(p1, n1, ht, yc1), O(p0, n0, ht, yc0), Vector3.down);
            // pita batu gelap setengah tinggi + celah panah (memecah dinding tinggi jadi bidang berskala manusia)
            float yb2a = Mathf.Max(g0 + 6.4f, yp0 + 1.0f), yb2b = Mathf.Max(g1 + 6.4f, yp1 + 1.0f);
            if (yb2a + 0.7f < yc0 - 2f && yb2b + 0.7f < yc1 - 2f)
            {
                V2Kit.QOut(b, Sl.DarkStone, O(p0, n0, ht + 0.22f, yb2a), O(p1, n1, ht + 0.22f, yb2b), O(p1, n1, ht + 0.22f, yb2b + 0.7f), O(p0, n0, ht + 0.22f, yb2a + 0.7f), no);
                V2Kit.QUp(b, Sl.DarkStone, O(p0, n0, ht, yb2a + 0.7f), O(p1, n1, ht, yb2b + 0.7f), O(p1, n1, ht + 0.22f, yb2b + 0.7f), O(p0, n0, ht + 0.22f, yb2a + 0.7f));
            }
            // sisi dalam
            V2Kit.QOut(b, Sl.Stone, O(p0, n0, -ht, yb0), O(p1, n1, -ht, yb1), O(p1, n1, -ht, t1), O(p0, n0, -ht, t0), -no);
            // lantai jalan benteng
            V2Kit.QUp(b, Sl.Pave, O(p0, n0, -(ht - 0.5f), t0), O(p1, n1, -(ht - 0.5f), t1), O(p1, n1, ht + pj, t1), O(p0, n0, ht + pj, t0));
            // parapet dalam rendah
            Vector3 pa = O(p0, n0, -ht, 0f), pb = O(p1, n1, -ht, 0f), pc = O(p0, n0, -(ht - 0.5f), 0f), pd = O(p1, n1, -(ht - 0.5f), 0f);
            V2Kit.QOut(b, Sl.Stone, pa + Vector3.up * t0, pb + Vector3.up * t1, pb + Vector3.up * (t1 + 0.95f), pa + Vector3.up * (t0 + 0.95f), -no);
            V2Kit.QOut(b, Sl.Stone, pc + Vector3.up * t0, pd + Vector3.up * t1, pd + Vector3.up * (t1 + 0.95f), pc + Vector3.up * (t0 + 0.95f), no);
            V2Kit.QUp(b, Sl.DarkStone, pa + Vector3.up * (t0 + 0.95f), pb + Vector3.up * (t1 + 0.95f), pd + Vector3.up * (t1 + 0.95f), pc + Vector3.up * (t0 + 0.95f));
            // korbel penopang parapet (machicolation): dua blok per langkah, bertingkat ke luar
            {
                Vector3 pm = (p0 + p1) * 0.5f; float tm = (t0 + t1) * 0.5f;
                float yaw = Mathf.Atan2(t.x, t.z) * Mathf.Rad2Deg;
                Vector3 pmn = (n0 + n1) * 0.5f;
                for (int q = 0; q < 2; q++)
                {
                    float f = (q + 0.5f) / 2f;
                    Vector3 pq = Vector3.Lerp(p0, p1, f); float tq = Mathf.Lerp(t0, t1, f);
                    Vector3 nq = Vector3.Lerp(n0, n1, f);
                    b.Box(Sl.DarkStone, pq + nq * (ht + 0.34f) + Vector3.up * (tq - 1.75f), new Vector3(0.5f, 1.0f, 0.7f), yaw);
                    b.Box(Sl.DarkStone, pq + nq * (ht + 0.62f) + Vector3.up * (tq - 1.38f), new Vector3(0.58f, 0.42f, 0.5f), yaw);
                }
                // celah panah tiap 2 langkah (~5 m), tinggi bergantian
                if ((i % 2) == 0)
                {
                    float gm = (g0 + g1) * 0.5f;
                    float yy = gm + (((i / 2) % 2) == 0 ? 9.6f : 11.6f);
                    if (yy + 1.0f < tm - 3.2f)
                    {
                        b.Box(Sl.Dark, pm + pmn * (ht + 0.03f) + Vector3.up * yy, new Vector3(0.26f, 1.5f, 0.1f), yaw);
                        b.Box(Sl.DarkStone, pm + pmn * (ht + 0.06f) + Vector3.up * (yy + 0.85f), new Vector3(0.56f, 0.16f, 0.16f), yaw);
                    }
                }
            }
            // pilaster (penopang) luar tiap ~10 m (tiap 4 langkah): lebih besar, berjenjang
            if ((i % 4) == 0)
            {
                Vector3 pm = (p0 + p1) * 0.5f; float gm = (g0 + g1) * 0.5f, tm = (t0 + t1) * 0.5f;
                float yaw = Mathf.Atan2(t.x, t.z) * Mathf.Rad2Deg;
                b.Box(Sl.Stone, pm + no * (ht + 0.5f) + Vector3.up * ((gm - 1.0f + tm - 1.3f) * 0.5f), new Vector3(2.0f, tm - 1.3f - gm + 1.0f, 1.2f), yaw);
                b.Box(Sl.DarkStone, pm + no * (ht + 0.6f) + Vector3.up * (gm + 2.4f), new Vector3(2.3f, 0.4f, 1.5f), yaw);
                b.Box(Sl.DarkStone, pm + no * (ht + 0.6f) + Vector3.up * (tm - 1.45f), new Vector3(2.3f, 0.32f, 1.5f), yaw);
            }
        }
        // benteng luar (merlon) sepanjang jalur
        float step = 2.5f, mlen = 1.5f;
        float sTot = cum[n - 1];
        int seg = 0;
        for (float d = 1.0f; d < sTot - 0.9f; d += step)
        {
            while (seg + 2 < n && cum[seg + 1] < d) seg++;
            float f = (d - cum[seg]) / Mathf.Max(0.01f, cum[seg + 1] - cum[seg]);
            Vector3 a = new Vector3(s[seg].x, 0f, s[seg].z), c = new Vector3(s[seg + 1].x, 0f, s[seg + 1].z);
            Vector3 tg = (c - a).normalized;
            Vector3 nm = Vector3.Lerp(nrm[seg], nrm[seg + 1], f);
            Vector3 pm = Vector3.Lerp(a, c, f);
            float ty = Mathf.Lerp(top[seg], top[seg + 1], f);
            float yaw = Mathf.Atan2(tg.x, tg.z) * Mathf.Rad2Deg;
            b.Box(Sl.Stone, pm + nm * (ht + 0.2f) + Vector3.up * (ty + 0.8f), new Vector3(0.85f, 1.6f, mlen), yaw);
            b.Box(Sl.DarkStone, pm + nm * (ht + 0.2f) + Vector3.up * (ty + 1.67f), new Vector3(0.95f, 0.16f, mlen + 0.1f), yaw);
        }
        return sTot;
    }

    /// <summary>Menara tembok bulat bergaya kota: dasar berkorbel, celah panah, puncak berbenteng terbuka (cap = false) atau kerucut biru (cap = true).</summary>
    public static void WallTower(GeoBag b, float x, float gy, float z, float r, float h, bool cone, int seed)
    {
        RoundTower(b, x, z, r, gy, gy + h, cone ? 5.5f + r * 1.0f : 0f, Sl.Stone, Sl.RoofB, seed, 7);
        // pita batu gelap setengah tinggi + dasar berundak (kesan benteng)
        b.Cyl(Sl.DarkStone, new Vector3(x, gy + h * 0.46f, z), r * 1.06f, 0.5f, 14, r * 1.06f, true);
        b.Cyl(Sl.DarkStone, new Vector3(x, gy + 4.6f, z), r * 1.07f, 0.4f, 14, r * 1.03f, true);
    }

    // =====================================================================
    // JEMBATAN PARIT (n76): jembatan batu berlengkung banyak di atas parit di depan gerbang
    // =====================================================================
    /// <summary>
    /// Jalur sepanjang +Z (ke luar gerbang), asal di tengah bentang; y = 0 = permukaan dek di ujung dekat gerbang (z = -L/2).
    /// W lebar luar, drop = beda tinggi dek di ujung luar (negatif = menurun, mengikuti tanah di luar parit), depth = kedalaman dasar parit di bawah dek.
    /// </summary>
    public static void MoatBridge(GeoBag b, float L, float W, float drop, float depth)
    {
        b.Push(Vector3.zero, 270f);                       // di dalam: jalur sepanjang X (-L/2 = ujung gerbang), lebar sepanjang Z
        float hw = W * 0.5f, inner = hw - 0.55f, para = 1.0f, yBot = -(depth + 1.8f);
        int n = Mathf.Max(2, Mathf.RoundToInt(L / 8.4f));
        float ph = 0.95f;                                 // setengah tebal pilar
        float springDrop = Mathf.Min(2.3f, depth * 0.72f), crownDrop = 1.05f;
        var uL = new float[n]; var uR = new float[n]; var cyA = new float[n]; var rA = new float[n]; var ysA = new float[n];
        for (int k = 0; k < n; k++)
        {
            uL[k] = -L * 0.5f + L * k / n + (k > 0 ? ph : 0f);
            uR[k] = -L * 0.5f + L * (k + 1) / n - (k < n - 1 ? ph : 0f);
            float uc = (uL[k] + uR[k]) * 0.5f, A = uR[k] - uL[k];
            float dc = drop * (uc + L * 0.5f) / L;
            float ra = springDrop - crownDrop;
            float r = (A * A * 0.25f + ra * ra) / (2f * ra);
            rA[k] = r; cyA[k] = dc - crownDrop - r; ysA[k] = dc - springDrop;
        }
        var us = new List<float>();
        int N = 26;
        for (int i = 0; i <= N; i++) us.Add(-L * 0.5f + L * i / N);
        for (int k = 0; k < n; k++) { us.Add(uL[k]); us.Add(uR[k]); }
        us.Sort();
        for (int i = 0; i + 1 < us.Count; i++)
        {
            float u0 = us[i], u1 = us[i + 1];
            if (u1 - u0 < 0.02f) continue;
            float d0 = drop * (u0 + L * 0.5f) / L, d1 = drop * (u1 + L * 0.5f) / L;
            float um = (u0 + u1) * 0.5f;
            int sp = -1;
            for (int k = 0; k < n; k++) if (um > uL[k] && um < uR[k]) { sp = k; break; }
            float a0 = yBot, a1 = yBot;
            if (sp >= 0)
            {
                float uc = (uL[sp] + uR[sp]) * 0.5f;
                a0 = cyA[sp] + Mathf.Sqrt(Mathf.Max(0.01f, rA[sp] * rA[sp] - (u0 - uc) * (u0 - uc)));
                a1 = cyA[sp] + Mathf.Sqrt(Mathf.Max(0.01f, rA[sp] * rA[sp] - (u1 - uc) * (u1 - uc)));
            }
            // permukaan jalan: paving batu
            V2Kit.QUp(b, Sl.Pave, new Vector3(u0, d0, -inner), new Vector3(u1, d1, -inner), new Vector3(u1, d1, inner), new Vector3(u0, d0, inner));
            for (int sg = -1; sg <= 1; sg += 2)
            {
                float vo = sg * hw, vi = sg * inner;
                V2Kit.QOut(b, Sl.Stone, new Vector3(u0, a0, vo), new Vector3(u1, a1, vo), new Vector3(u1, d1, vo), new Vector3(u0, d0, vo), new Vector3(0f, 0f, sg));
                // pita lis di tepi dek
                V2Kit.QOut(b, Sl.DarkStone, new Vector3(u0, d0 - 0.45f, vo + sg * 0.12f), new Vector3(u1, d1 - 0.45f, vo + sg * 0.12f), new Vector3(u1, d1, vo + sg * 0.12f), new Vector3(u0, d0, vo + sg * 0.12f), new Vector3(0f, 0f, sg));
                // pagar pembatas
                V2Kit.QOut(b, Sl.Stone, new Vector3(u0, d0, vo), new Vector3(u1, d1, vo), new Vector3(u1, d1 + para, vo), new Vector3(u0, d0 + para, vo), new Vector3(0f, 0f, sg));
                V2Kit.QOut(b, Sl.Stone, new Vector3(u0, d0, vi), new Vector3(u1, d1, vi), new Vector3(u1, d1 + para, vi), new Vector3(u0, d0 + para, vi), new Vector3(0f, 0f, -sg));
                b.Beam(Sl.DarkStone, new Vector3(u0, d0 + para + 0.07f, (vo + vi) * 0.5f), new Vector3(u1, d1 + para + 0.07f, (vo + vi) * 0.5f), (hw - inner) + 0.2f, 0.15f, new Vector3(0f, 1f, 0f));
            }
            if (sp >= 0) V2Kit.QOut(b, Sl.DarkStone, new Vector3(u0, a0, -hw), new Vector3(u1, a1, -hw), new Vector3(u1, a1, hw), new Vector3(u0, a0, hw), new Vector3(0f, -1f, 0f));
        }
        // sisi pilar menghadap bukaan, pilar tengah (alas lebar), tutup ujung + tiang pagar
        for (int k = 0; k < n; k++)
        {
            for (int e = 0; e < 2; e++)
            {
                float u = e == 0 ? uL[k] : uR[k], dir = e == 0 ? 1f : -1f;
                V2Kit.QOut(b, Sl.DarkStone, new Vector3(u, yBot, -hw), new Vector3(u, yBot, hw), new Vector3(u, ysA[k], hw), new Vector3(u, ysA[k], -hw), new Vector3(dir, 0f, 0f));
            }
        }
        for (int k = 1; k < n; k++)
        {
            float pc = -L * 0.5f + L * k / n;
            // pemecah arus berbentuk baji di kedua muka (di muka luar jembatan)
            for (int sg = -1; sg <= 1; sg += 2)
            {
                Vector3 pa = new Vector3(pc - ph, yBot, sg * hw), pb = new Vector3(pc + ph, yBot, sg * hw);
                Vector3 pt = new Vector3(pc, yBot, sg * (hw + 0.9f));
                float yTop = Mathf.Min(ysA[k - 1], ysA[k]) + 0.35f;
                b.Tri(Sl.DarkStone, new Vector3(pa.x, yTop, pa.z), new Vector3(pt.x, yTop, pt.z), new Vector3(pb.x, yTop, pb.z));
                V2Kit.QOut(b, Sl.DarkStone, pa, pt, new Vector3(pt.x, yTop, pt.z), new Vector3(pa.x, yTop, pa.z), new Vector3(-0.6f, 0f, sg));
                V2Kit.QOut(b, Sl.DarkStone, pt, pb, new Vector3(pb.x, yTop, pb.z), new Vector3(pt.x, yTop, pt.z), new Vector3(0.6f, 0f, sg));
            }
        }
        for (int sd = -1; sd <= 1; sd += 2)
        {
            float ue = sd * L * 0.5f;
            float de = sd < 0 ? 0f : drop;
            V2Kit.QOut(b, Sl.Stone, new Vector3(ue, yBot, -hw), new Vector3(ue, yBot, hw), new Vector3(ue, de, hw), new Vector3(ue, de, -hw), new Vector3(sd, 0f, 0f));
            for (int sg = -1; sg <= 1; sg += 2)
            {
                b.Box(Sl.DarkStone, new Vector3(ue - sd * 0.4f, de + para * 0.5f + 0.15f, sg * (hw - 0.3f)), new Vector3(0.8f, para + 0.3f, 0.85f));
                b.Box(Sl.Stone, new Vector3(ue - sd * 0.4f, de + para + 0.38f, sg * (hw - 0.3f)), new Vector3(0.96f, 0.2f, 1.0f));
                b.Ball(Sl.Stone, new Vector3(ue - sd * 0.4f, de + para + 0.66f, sg * (hw - 0.3f)), 0.26f, 3, 6);
            }
        }
        // cincin batu pasak + batu kunci pada kedua muka tiap lengkung
        for (int k = 0; k < n; k++)
        {
            float A = uR[k] - uL[k], uc = (uL[k] + uR[k]) * 0.5f, r = rA[k];
            float asn = Mathf.Asin(Mathf.Clamp01(A * 0.5f / r)) * Mathf.Rad2Deg;
            float a0d = 90f - asn, a1d = 90f + asn;
            for (int sg = -1; sg <= 1; sg += 2)
            {
                float zA = sg > 0 ? hw - 0.04f : -hw - 0.16f, zB = sg > 0 ? hw + 0.16f : -hw + 0.04f;
                V2Kit.ArchRing(b, Sl.DarkStone, uc, cyA[k], r, r + 0.55f, a0d, a1d, 12, zA, zB);
                b.Box(Sl.DarkStone, new Vector3(uc, cyA[k] + r + 0.3f, sg * (hw + 0.07f)), new Vector3(0.72f, 0.9f, 0.24f));
            }
        }
        b.Pop();
    }

    // =====================================================================
    // LENGKUNG AIR (sungai menembus tembok luar)
    // =====================================================================
    /// <summary>
    /// Lengkung air di tembok luar. Bingkai lokal: +Z = ke luar kota, sungai mengalir sepanjang Z melewati bukaan (lebar W).
    /// Tinggi tembok h di atas y = 0 (muka air kira-kira y = waterY). Terali besi dua lapis menahan lewat.
    /// </summary>
    public static void WaterArch(GeoBag b, float W, float depth, float wallH, float bedY, float springY, float crownY)
    {
        float hw = W * 0.5f, D = depth, hd = D * 0.5f;
        float rise = crownY - springY;
        float R = (hw * hw + rise * rise) / (2f * rise);
        float cy = crownY - R;
        float X1 = hw + 9.5f;
        int n = 14;
        // dua muka (depan +Z, belakang -Z): kiri-kanan penuh + irisan di atas lengkung
        for (int side = -1; side <= 1; side += 2)
        {
            float z = side * hd;
            Vector3 outw = new Vector3(0f, 0f, side);
            V2Kit.QOut(b, Sl.Stone, new Vector3(-X1, bedY, z), new Vector3(-hw, bedY, z), new Vector3(-hw, wallH, z), new Vector3(-X1, wallH, z), outw);
            V2Kit.QOut(b, Sl.Stone, new Vector3(hw, bedY, z), new Vector3(X1, bedY, z), new Vector3(X1, wallH, z), new Vector3(hw, wallH, z), outw);
            for (int i = 0; i < n; i++)
            {
                float x0 = -hw + 2f * hw * i / n, x1 = -hw + 2f * hw * (i + 1) / n;
                float y0 = cy + Mathf.Sqrt(Mathf.Max(0f, R * R - x0 * x0)), y1 = cy + Mathf.Sqrt(Mathf.Max(0f, R * R - x1 * x1));
                V2Kit.QOut(b, Sl.Stone, new Vector3(x0, y0, z), new Vector3(x1, y1, z), new Vector3(x1, wallH, z), new Vector3(x0, wallH, z), outw);
                // cincin batu kunci pada tepi lengkung
                V2Kit.QOut(b, Sl.DarkStone, new Vector3(x0, y0, z), new Vector3(x1, y1, z), new Vector3(x1, y1 + 0.0f, z + side * 0.3f), new Vector3(x0, y0, z + side * 0.3f), Vector3.up);
                V2Kit.QOut(b, Sl.DarkStone, new Vector3(x0, y0 + 0.9f, z + side * 0.3f), new Vector3(x1, y1 + 0.9f, z + side * 0.3f), new Vector3(x1, y1, z + side * 0.3f), new Vector3(x0, y0, z + side * 0.3f), outw);
            }
        }
        // langit-langit terowongan + dinding samping
        for (int i = 0; i < n; i++)
        {
            float x0 = -hw + 2f * hw * i / n, x1 = -hw + 2f * hw * (i + 1) / n;
            float y0 = cy + Mathf.Sqrt(Mathf.Max(0f, R * R - x0 * x0)), y1 = cy + Mathf.Sqrt(Mathf.Max(0f, R * R - x1 * x1));
            V2Kit.QOut(b, Sl.DarkStone, new Vector3(x0, y0, -hd), new Vector3(x1, y1, -hd), new Vector3(x1, y1, hd), new Vector3(x0, y0, hd), Vector3.down);
        }
        foreach (float sx in new[] { -1f, 1f })
            V2Kit.QOut(b, Sl.Stone, new Vector3(sx * hw, bedY, -hd), new Vector3(sx * hw, bedY, hd), new Vector3(sx * hw, springY, hd), new Vector3(sx * hw, springY, -hd), new Vector3(-sx, 0f, 0f));
        // atas: lantai jalan benteng + parapet berbenteng
        V2Kit.QUp(b, Sl.Pave, new Vector3(-X1, wallH, -hd), new Vector3(X1, wallH, -hd), new Vector3(X1, wallH, hd), new Vector3(-X1, wallH, hd));
        for (float x = -X1 + 1.0f; x < X1 - 0.8f; x += 2.5f)
        {
            b.Box(Sl.Stone, new Vector3(x, wallH + 0.7f, hd - 0.3f), new Vector3(1.5f, 1.4f, 0.8f));
            b.Box(Sl.Stone, new Vector3(x, wallH + 0.5f, -hd + 0.3f), new Vector3(1.5f, 1.0f, 0.5f));
        }
        b.Box(Sl.DarkStone, new Vector3(0f, wallH - 0.15f, 0f), new Vector3(W + 3f, 0.3f, D + 0.6f));
        // terali besi: dua lapis, batang tegak + palang
        foreach (float gz in new[] { hd - 1.3f, -hd + 1.3f })
        {
            int nb = Mathf.RoundToInt(W / 0.55f);
            for (int k = 0; k <= nb; k++)
            {
                float x = -hw + 0.3f + k * (W - 0.6f) / nb;
                float yTop = cy + Mathf.Sqrt(Mathf.Max(0f, R * R - x * x)) - 0.25f;
                b.Box(Sl.Metal, new Vector3(x, (bedY + yTop) * 0.5f, gz), new Vector3(0.12f, yTop - bedY, 0.12f));
            }
            for (float y = bedY + 1.5f; y < crownY - 0.4f; y += 2.4f)
            {
                float xe = Mathf.Min(hw - 0.2f, Mathf.Sqrt(Mathf.Max(0.01f, R * R - (y - cy) * (y - cy))));
                b.Box(Sl.Metal, new Vector3(0f, y, gz), new Vector3(xe * 2f, 0.14f, 0.16f));
            }
        }
        // menara kecil pengapit
        foreach (float sx in new[] { -1f, 1f })
            RoundTower(b, sx * (hw + 3.8f), 0f, 3.5f, bedY + 1.5f, wallH + 6.5f, 6.2f, Sl.Stone, Sl.RoofB, 31 + (int)sx, 3);
    }
}
