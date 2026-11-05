using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// v35z102n74: atap desa baru. Sirap: lereng sedikit melengkung dengan ujung tepi terangkat, deretan sirap BERUNDAK (tiap deret menonjol di atas deret di bawahnya),
/// papan tepi mengikuti lereng, lis punggung, lukarne. Jerami: lapisan tebal bermuka halus, bibir tepi membulat, gulungan punggung dengan ikatan dan silang kayu.
/// Hanya memakai GeoBag/Sl/Vector/Mathf (bisa dipratinjau di luar Unity). Konvensi: punggung sepanjang X lokal, titik pusat = tengah di tinggi puncak dinding.
/// </summary>
public static partial class V2Kit
{
    // ------------------------------------------------------------------ permukaan grid halus
    /// <summary>
    /// Permukaan terbuka P[i,j] dengan normal halus (selisih tengah antar tetangga). Sisi depan = cross(dP/di, dP/dj); flip membalik.
    /// closedI: kolom i terakhir menduplikasi kolom pertama (cincin tertutup) sehingga normal tersambung mulus.
    /// UV dalam meter: u = panjang busur sepanjang i, v = panjang busur sepanjang j (atau vRow bila diberikan).
    /// </summary>
    public static void Grid(GeoBag b, int slot, Vector3[,] P, bool flip, bool closedI, float su = 1f, float sv = 1f, float[] vRow = null, float u0 = 0f, float v0 = 0f)
    {
        int ni = P.GetLength(0), nj = P.GetLength(1);
        if (ni < 2 || nj < 2) return;
        var vs = new List<Vector3>(ni * nj); var ns = new List<Vector3>(ni * nj); var us = new List<Vector2>(ni * nj);
        var tr = new List<int>((ni - 1) * (nj - 1) * 6);
        var cu = new float[ni, nj]; var cv = new float[ni, nj];
        for (int j = 0; j < nj; j++) for (int i = 1; i < ni; i++) cu[i, j] = cu[i - 1, j] + (P[i, j] - P[i - 1, j]).magnitude;
        for (int i = 0; i < ni; i++) for (int j = 1; j < nj; j++) cv[i, j] = cv[i, j - 1] + (P[i, j] - P[i, j - 1]).magnitude;
        for (int i = 0; i < ni; i++)
            for (int j = 0; j < nj; j++)
            {
                int ia, ic;
                if (closedI) { ia = i == 0 ? ni - 2 : i - 1; ic = i == ni - 1 ? 1 : i + 1; }
                else { ia = Mathf.Max(0, i - 1); ic = Mathf.Min(ni - 1, i + 1); }
                int ja = Mathf.Max(0, j - 1), jc = Mathf.Min(nj - 1, j + 1);
                Vector3 n = Vector3.Cross(P[ic, j] - P[ia, j], P[i, jc] - P[i, ja]);
                if (n.sqrMagnitude < 1e-10f)
                {
                    // simpul menyempit (mis. ujung kerucut): pakai selisih ke depan/belakang
                    int i2 = Mathf.Min(ni - 1, i + 1), i1 = Mathf.Max(0, i - 1), j2 = Mathf.Min(nj - 1, j + 1), j1 = Mathf.Max(0, j - 1);
                    n = Vector3.Cross(P[i2, j] - P[i1, j], P[i, j2] - P[i, j1]);
                    if (n.sqrMagnitude < 1e-12f) n = Vector3.up;
                }
                n = n.normalized;
                if (flip) n = -n;
                vs.Add(P[i, j]); ns.Add(n);
                us.Add(new Vector2(u0 + cu[i, j] * su, vRow != null ? vRow[j] : v0 + cv[i, j] * sv));
            }
        for (int i = 0; i + 1 < ni; i++)
            for (int j = 0; j + 1 < nj; j++)
            {
                int a = i * nj + j, bb = (i + 1) * nj + j, c = (i + 1) * nj + j + 1, d = i * nj + j + 1;
                if (!flip) { tr.Add(a); tr.Add(bb); tr.Add(c); tr.Add(a); tr.Add(c); tr.Add(d); }
                else { tr.Add(a); tr.Add(c); tr.Add(bb); tr.Add(a); tr.Add(d); tr.Add(c); }
            }
        b.Raw(slot, vs, ns, us, tr);
    }

    /// <summary>Pita dua baris sepanjang X (A -> B), mis. satu deret sirap atau permukaan sempit.</summary>
    static void Ribbon(GeoBag b, int slot, Vector3[] A, Vector3[] B, bool flip, float v0, float v1)
    {
        var P = new Vector3[A.Length, 2];
        for (int i = 0; i < A.Length; i++) { P[i, 0] = A[i]; P[i, 1] = B[i]; }
        Grid(b, slot, P, flip, false, 1f, 1f, new[] { v0, v1 });
    }

    static float Sm01(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

    // ------------------------------------------------------------------ profil atap sirap
    /// <summary>Tinggi permukaan pada jarak z dari punggung (0..hs). Garis lurus dari punggung ke puncak dinding, lalu tepi terangkat.</summary>
    public static float ShY(float z, float hw, float hs, float rise, float kick)
    {
        float lin = rise - rise / hw * z;
        float t = Mathf.Clamp01(z / hs);
        float k = Mathf.Clamp01((t - 0.52f) / 0.48f);
        return lin + kick * k * k * (3f - 2f * k);
    }

    static float ShDy(int seed, int sd, int i, int k, float x)
    {
        return (Rnd(seed * 5 + sd + 9, i * 37 + k * 11) - 0.5f) * 0.02f + 0.007f * Mathf.Sin(x * 1.9f + k * 1.7f + seed);
    }

    // ------------------------------------------------------------------ atap sirap (punggung sepanjang X lokal)
    /// <summary>c = titik tengah di tinggi tepi dinding. len = panjang dinding sepanjang X, span = lebar sepanjang Z.
    /// detail = rumah warga: gable berbingkai kayu, hiasan ujung punggung. dormers = posisi X lukarne di lereng depan (+Z).</summary>
    public static void ShingleRoof(GeoBag b, int sRoof, int sRidge, int sWood, int sWall, Vector3 c, float len, float span, float rise, float ov, float ovEnd, bool gableFill = true, int seed = 1, bool detail = false, float[] dormers = null)
    {
        b.Push(c, 0f);
        float hw = span * 0.5f, hs = hw + ov, L = len * 0.5f + ovEnd, xl = len * 0.5f;
        float kick = Mathf.Clamp(0.06f + 0.28f * ov, 0.05f, 0.4f);
        float lip = Mathf.Clamp(rise * 0.028f, 0.04f, 0.07f);
        float ang = Mathf.Atan2(rise, hw);

        // sampel profil tepi -> punggung, lalu bagi menjadi deret sama panjang (~0,5 m)
        const int S = 40;
        float[] zs = new float[S + 1], ys = new float[S + 1], arc = new float[S + 1];
        for (int s = 0; s <= S; s++)
        {
            zs[s] = hs * (1f - s / (float)S);
            ys[s] = ShY(zs[s], hw, hs, rise, kick);
            if (s > 0) { float dz = zs[s] - zs[s - 1], dy = ys[s] - ys[s - 1]; arc[s] = arc[s - 1] + Mathf.Sqrt(dz * dz + dy * dy); }
        }
        float total = arc[S];
        int K = Mathf.Max(3, Mathf.RoundToInt(total / 0.5f));
        float[] zk = new float[K + 1], yk = new float[K + 1];
        for (int k = 0; k <= K; k++)
        {
            float target = total * k / K; int s = 0;
            while (s < S - 1 && arc[s + 1] < target) s++;
            float f = arc[s + 1] > arc[s] ? Mathf.Clamp01((target - arc[s]) / (arc[s + 1] - arc[s])) : 0f;
            zk[k] = Mathf.Lerp(zs[s], zs[s + 1], f); yk[k] = Mathf.Lerp(ys[s], ys[s + 1], f);
        }
        zk[K] = 0f; yk[K] = rise;
        int nx = Mathf.Max(4, Mathf.CeilToInt(2f * L / 0.8f));

        for (int sd = -1; sd <= 1; sd += 2)
        {
            bool fl = sd < 0;
            for (int k = 0; k < K; k++)
            {
                var A = new Vector3[nx + 1]; var B = new Vector3[nx + 1]; var C = new Vector3[nx + 1];
                for (int i = 0; i <= nx; i++)
                {
                    float x = -L + 2f * L * i / nx;
                    float d0 = ShDy(seed, sd, i, k, x) * (k == 0 ? 1.8f : 1f);
                    float d1 = (k + 1 == K) ? 0f : ShDy(seed, sd, i, k + 1, x);
                    A[i] = new Vector3(x, yk[k] + d0 + lip * 0.5f, sd * zk[k]);
                    B[i] = new Vector3(x, yk[k + 1] + d1 - lip * 0.5f, sd * zk[k + 1]);
                    C[i] = new Vector3(x, yk[k + 1] + d1 + lip * 0.5f, sd * zk[k + 1]);
                }
                Ribbon(b, sRoof, A, B, fl, 0.5f * k, 0.5f * (k + 1));                                // permukaan deret
                if (k + 1 < K) Ribbon(b, sRoof, B, C, fl, 0.5f * (k + 1) - 0.035f, 0.5f * (k + 1) - 0.002f);    // tebal tepi deret berikutnya (garis bayangan)
            }
            // wajah tebal tepi bawah + papan fasia + langit-langit tritisan
            {
                var E0 = new Vector3[nx + 1]; var E1 = new Vector3[nx + 1]; var F0 = new Vector3[nx + 1]; var F1 = new Vector3[nx + 1]; var G0 = new Vector3[nx + 1]; var G1 = new Vector3[nx + 1];
                float zw = hw - 0.02f, yw = ShY(zw, hw, hs, rise, kick) - 0.2f;
                for (int i = 0; i <= nx; i++)
                {
                    float x = -L + 2f * L * i / nx;
                    float d0 = ShDy(seed, sd, i, 0, x) * 1.8f;
                    float y0 = yk[0] + d0;
                    E0[i] = new Vector3(x, y0 - 0.11f, sd * zk[0]); E1[i] = new Vector3(x, y0 + lip * 0.5f, sd * zk[0]);
                    F0[i] = new Vector3(x, y0 - 0.27f, sd * (zk[0] - 0.05f)); F1[i] = new Vector3(x, y0 - 0.11f, sd * (zk[0] - 0.05f));
                    G0[i] = F0[i]; G1[i] = new Vector3(x, yw, sd * zw);
                }
                Ribbon(b, sRoof, E0, E1, fl, 0.5f - 0.1f, 0.5f - 0.002f);
                Ribbon(b, sWood, F0, F1, fl, 0f, 0.16f);
                Ribbon(b, sWood, G0, G1, !fl, 0f, 0.7f);
            }
        }

        // sisi bawah tritisan ujung (atap menjorok melewati dinding gable) + papan tepi mengikuti lereng
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sd = -1; sd <= 1; sd += 2)
                for (int k = 0; k < K; k++)
                {
                    Vector3 a = new Vector3(sx * xl, yk[k] - 0.16f, sd * zk[k]), bq = new Vector3(sx * L, yk[k] - 0.16f, sd * zk[k]);
                    Vector3 cq = new Vector3(sx * L, yk[k + 1] - 0.16f, sd * zk[k + 1]), dq = new Vector3(sx * xl, yk[k + 1] - 0.16f, sd * zk[k + 1]);
                    QOut(b, sWood, a, bq, cq, dq, new Vector3(0f, -1f, 0f));
                    Vector3 p0 = new Vector3(sx * (L + 0.01f), yk[k] - 0.05f, sd * zk[k]), p1 = new Vector3(sx * (L + 0.01f), yk[k + 1] - 0.05f, sd * zk[k + 1]);
                    Vector3 dir = (p1 - p0).normalized;
                    b.Beam(sWood, p0 - dir * 0.04f, p1 + dir * 0.04f, 0.26f, 0.07f, new Vector3(sx, 0f, 0f));
                }

        // punggung: dua papan miring + batang bulat + hiasan ujung
        {
            float cw = 0.36f, cs = Mathf.Cos(ang), sn = Mathf.Sin(ang);
            for (int sd = -1; sd <= 1; sd += 2)
            {
                Vector3 hint = new Vector3(0f, cs, sd * sn);
                b.Beam(sRidge, new Vector3(-L - 0.05f, rise + 0.03f - cw * 0.5f * sn, sd * cw * 0.5f * cs), new Vector3(L + 0.05f, rise + 0.03f - cw * 0.5f * sn, sd * cw * 0.5f * cs), cw, 0.075f, hint);
            }
            Prism(b, sWood, new Vector3(-L - 0.1f, rise + 0.07f, 0f), new Vector3(L + 0.1f, rise + 0.07f, 0f), 0.075f, 0.075f, 6, true, true);
            if (ov >= 0.3f && rise > 1.2f)
                for (int sx = -1; sx <= 1; sx += 2)
                {
                    b.Cyl(sWood, new Vector3(sx * L, rise + 0.02f, 0f), 0.055f, 0.52f, 6, 0.022f, true);
                    b.Ball(sWood, new Vector3(sx * L, rise + 0.2f, 0f), 0.085f, 3, 6);
                }
        }

        // segitiga gable di dinding (sedikit di bawah profil) + bingkai kayu
        if (gableFill)
        {
            float yTop = rise - lip - 0.05f;
            b.Tri(sWall, new Vector3(xl, 0f, hw), new Vector3(xl, 0f, -hw), new Vector3(xl, yTop, 0f));
            b.Tri(sWall, new Vector3(-xl, 0f, -hw), new Vector3(-xl, 0f, hw), new Vector3(-xl, yTop, 0f));
            if (detail && span > 3.6f && rise > 1.3f)
            {
                float yb = rise * 0.34f;
                for (int sx = -1; sx <= 1; sx += 2)
                {
                    float xx = sx * (xl + 0.05f);
                    b.Box(sWood, new Vector3(xx, yb, 0f), new Vector3(0.1f, 0.15f, 2f * hw * (1f - yb / rise) + 0.1f));
                    b.Box(sWood, new Vector3(xx, (yTop - 0.05f) * 0.5f, 0f), new Vector3(0.1f, yTop - 0.05f, 0.15f));
                    for (int sd = -1; sd <= 1; sd += 2)
                    {
                        b.Beam(sWood, new Vector3(xx, yTop * 0.72f, 0f), new Vector3(xx, yb + 0.06f, sd * hw * (1f - yb / rise) * 0.92f), 0.13f, 0.1f, new Vector3(sx, 0f, 0f));
                        b.Beam(sWood, new Vector3(xx, 0.03f, sd * hw * 0.5f), new Vector3(xx, yb - 0.04f, sd * hw * 0.5f), 0.11f, 0.1f, new Vector3(sx, 0f, 0f));
                    }
                }
            }
        }
        if (dormers != null && rise > 2.6f)
            for (int q = 0; q < dormers.Length; q++)
                ShingleDormer(b, sRoof, sWood, Sl.Plaster, dormers[q], 0.7f * hw, hw, hs, rise, kick, 1.2f, seed + q * 7);
        b.Pop();
    }

    // ------------------------------------------------------------------ lukarne beratap pelana di lereng depan (+Z)
    static void ShingleDormer(GeoBag b, int sRoof, int sWood, int sWall, float x0, float zf, float hw, float hs, float rise, float kick, float w, int seed)
    {
        float rr = w * 0.5f;
        float yf = ShY(zf, hw, hs, rise, kick);
        float wallH = 0.95f, yTop = yf + wallH, ridgeY = yTop + rr * 0.95f;
        // z tempat atap utama mencapai puncak dinding & punggung lukarne
        float zc = zf, zm = zf;
        for (int it = 0; it < 80 && zc > 0.01f; it++) { zc -= 0.04f; if (ShY(zc, hw, hs, rise, kick) >= yTop - 0.02f) break; }
        for (int it = 0; it < 120 && zm > 0.01f; it++) { zm -= 0.04f; if (ShY(zm, hw, hs, rise, kick) >= ridgeY - 0.02f) break; }
        zc = Mathf.Max(0.05f, zc - 0.05f); zm = Mathf.Max(0.05f, zm - 0.12f);
        float ov = 0.22f;
        // dinding depan + segitiga + jendela
        QOut(b, sWall, new Vector3(x0 - rr, yf - 0.35f, zf), new Vector3(x0 + rr, yf - 0.35f, zf), new Vector3(x0 + rr, yTop, zf), new Vector3(x0 - rr, yTop, zf), new Vector3(0f, 0f, 1f));
        b.Tri(sWall, new Vector3(x0 - rr, yTop, zf), new Vector3(x0 + rr, yTop, zf), new Vector3(x0, ridgeY, zf));
        // pipi (dinding samping)
        for (int sx = -1; sx <= 1; sx += 2)
            QOut(b, sWall, new Vector3(x0 + sx * rr, yf - 0.35f, zf), new Vector3(x0 + sx * rr, yf - 0.35f, zc), new Vector3(x0 + sx * rr, yTop, zc), new Vector3(x0 + sx * rr, yTop, zf), new Vector3(sx, 0f, 0f));
        // dua bidang atap lukarne + papan tepi depan
        for (int sx = -1; sx <= 1; sx += 2)
        {
            Vector3 e0 = new Vector3(x0 + sx * (rr + ov), yTop - 0.1f, zf + ov), e1 = new Vector3(x0 + sx * (rr + ov), yTop - 0.1f, zm);
            Vector3 r1 = new Vector3(x0, ridgeY + 0.07f, zm), r0 = new Vector3(x0, ridgeY + 0.07f, zf + ov);
            QOut(b, sRoof, e0, e1, r1, r0, new Vector3(sx * 0.7f, 1f, 0f));
            QOut(b, sWood, e0 - new Vector3(0f, 0.08f, 0f), e1 - new Vector3(0f, 0.08f, 0f), r1 - new Vector3(0f, 0.08f, 0f), r0 - new Vector3(0f, 0.08f, 0f), new Vector3(-sx * 0.7f, -1f, 0f));
            b.Beam(sWood, e0, r0, 0.2f, 0.05f, new Vector3(0f, 0f, 1f));
        }
        b.Ball(sWood, new Vector3(x0, ridgeY + 0.12f, zf + ov), 0.07f, 3, 6);
        b.Push(new Vector3(x0, yf, zf), 0f);
        Window(b, 0f, 0.5f * wallH + 0.05f, w * 0.45f, wallH * 0.62f, false, false, seed, true);
        b.Pop();
    }

    // ------------------------------------------------------------------ atap miring satu sisi berundak (tudung pintu, serambi). Depan = +Z, belakang menempel dinding.
    public static void Pent(GeoBag b, int sRoof, int sWood, float x0, float x1, float zBack, float zFront, float yBack, float yFront, int seed)
    {
        float dz = zFront - zBack, dy = yBack - yFront;
        float slopeLen = Mathf.Sqrt(dz * dz + dy * dy);
        int K = Mathf.Max(2, Mathf.RoundToInt(slopeLen / 0.36f));
        int nx = Mathf.Max(2, Mathf.CeilToInt((x1 - x0) / 0.8f));
        float lip = 0.04f;
        for (int k = 0; k < K; k++)
        {
            float f0 = k / (float)K, f1 = (k + 1) / (float)K;
            var A = new Vector3[nx + 1]; var B = new Vector3[nx + 1]; var C = new Vector3[nx + 1];
            for (int i = 0; i <= nx; i++)
            {
                float x = Mathf.Lerp(x0, x1, i / (float)nx);
                float w0 = (Rnd(seed, i * 17 + k) - 0.5f) * 0.02f, w1 = (Rnd(seed, i * 17 + k + 1) - 0.5f) * 0.02f;
                A[i] = new Vector3(x, Mathf.Lerp(yFront, yBack, f0) + lip * 0.5f + w0, Mathf.Lerp(zFront, zBack, f0));
                B[i] = new Vector3(x, Mathf.Lerp(yFront, yBack, f1) - lip * 0.5f + (k + 1 == K ? 0f : w1), Mathf.Lerp(zFront, zBack, f1));
                C[i] = new Vector3(x, B[i].y + lip, B[i].z);
            }
            Ribbon(b, sRoof, A, B, false, 0.5f * k, 0.5f * (k + 1));
            if (k + 1 < K) Ribbon(b, sRoof, B, C, false, 0.5f * (k + 1) - 0.035f, 0.5f * (k + 1) - 0.002f);
        }
        // tepi depan tebal + sisi + bawah
        QOut(b, sRoof, new Vector3(x0, yFront - 0.1f, zFront), new Vector3(x1, yFront - 0.1f, zFront), new Vector3(x1, yFront + lip * 0.5f, zFront), new Vector3(x0, yFront + lip * 0.5f, zFront), new Vector3(0f, 0f, 1f));
        QOut(b, sWood, new Vector3(x0, yFront - 0.1f, zFront), new Vector3(x1, yFront - 0.1f, zFront), new Vector3(x1, yBack - 0.14f, zBack), new Vector3(x0, yBack - 0.14f, zBack), new Vector3(0f, -1f, 0f));
        for (int sx = -1; sx <= 1; sx += 2)
        {
            float xx = sx < 0 ? x0 : x1;
            b.Beam(sWood, new Vector3(xx, yFront - 0.03f, zFront + 0.02f), new Vector3(xx, yBack - 0.02f, zBack), 0.17f, 0.06f, new Vector3(sx, 0f, 0f));
        }
    }

    // ------------------------------------------------------------------ atap jerami tebal (punggung sepanjang X lokal)
    static float Wob(float x, float z, int seed)
    {
        return 0.045f * Mathf.Sin(1.31f * x + 0.7f * z + seed) + 0.03f * Mathf.Sin(2.3f * z - 1.1f * x + seed * 1.7f) + 0.02f * Mathf.Sin(3.7f * x + 1.9f * z);
    }

    /// <summary>Tinggi permukaan jerami (tanpa tepi bergerigi).</summary>
    static float ThatchH(float x, float z, float Sh, float Lh, float A, float yE, float hip, int seed)
    {
        float tz = Mathf.Abs(z) / Sh, tx = Mathf.Abs(x) / Lh;
        float endDrop = 1f - 0.84f * Sm01((tx - (1f - hip)) / hip);
        float prof = 1f - Mathf.Pow(tz, 1.45f);
        return yE + A * prof * endDrop + Wob(x, z, seed) * (1f - 0.5f * tz) * endDrop;
    }

    /// <summary>"Alis" jendela: tudung jerami melengkung di lereng depan (+Z) dengan bukaan berkaca di bawah bibirnya.</summary>
    static void ThatchEyebrow(GeoBag b, int sThatch, int sWood, float xe, float Sh, float Lh, float A, float yE, float hip, int seed)
    {
        float zl = Sh * 0.56f, hw = 0.62f;
        float[] zOff = { 0.04f, 0.07f, 0.04f, -0.07f, -0.25f, -0.5f, -0.82f, -1.15f, -1.5f };
        float[] lift = { 0.52f, 0.62f, 0.74f, 0.83f, 0.88f, 0.80f, 0.56f, 0.26f, 0.0f };
        const int NU = 13;
        int NV = zOff.Length;
        var P = new Vector3[NU, NV];
        for (int i = 0; i < NU; i++)
        {
            float u = -1f + 2f * i / (NU - 1);
            float x = xe + u * hw;
            float edge = Mathf.Pow(Mathf.Max(0f, 1f - u * u), 0.6f);
            for (int j = 0; j < NV; j++)
            {
                float z = zl + zOff[j];
                float y = ThatchH(x, z, Sh, Lh, A, yE, hip, seed) + Mathf.Max(0.012f, lift[j] * edge);
                P[i, j] = new Vector3(x, y, z);
            }
        }
        Grid(b, sThatch, P, false, false, 1f, 1f);
        // bukaan berkaca di bawah bibir (bidang tegak sedikit di belakang bibir) + bingkai
        float zg = zl + 0.02f;
        for (int i = 0; i + 1 < NU; i++)
        {
            float u0 = -1f + 2f * i / (NU - 1), u1 = -1f + 2f * (i + 1) / (NU - 1);
            float x0 = xe + u0 * hw, x1 = xe + u1 * hw;
            float e0 = Mathf.Pow(Mathf.Max(0f, 1f - u0 * u0), 0.6f), e1 = Mathf.Pow(Mathf.Max(0f, 1f - u1 * u1), 0.6f);
            float yb0 = ThatchH(x0, zg, Sh, Lh, A, yE, hip, seed) - 0.04f, yb1 = ThatchH(x1, zg, Sh, Lh, A, yE, hip, seed) - 0.04f;
            float yt0 = ThatchH(x0, zg, Sh, Lh, A, yE, hip, seed) + 0.52f * e0, yt1 = ThatchH(x1, zg, Sh, Lh, A, yE, hip, seed) + 0.52f * e1;
            b.Quad(Sl.Glass, new Vector3(x0, yb0, zg), new Vector3(x1, yb1, zg), new Vector3(x1, yt1, zg), new Vector3(x0, yt0, zg));
            if (i > 0 && i < NU - 2) b.Beam(sWood, new Vector3(x0, yt0, zg + 0.03f), new Vector3(x1, yt1, zg + 0.03f), 0.07f, 0.06f, Vector3.up);
        }
        float ys = ThatchH(xe, zg, Sh, Lh, A, yE, hip, seed);
        b.Box(sWood, new Vector3(xe, ys + 0.03f, zg + 0.07f), new Vector3(hw * 1.5f, 0.06f, 0.16f));
        b.Box(sWood, new Vector3(xe, ys + 0.27f, zg + 0.02f), new Vector3(0.04f, 0.5f, 0.04f));
    }

    public static void ThatchRoof(GeoBag b, int sThatch, int sRidge, int sWood, int sWall, Vector3 c, float len, float span, float rise, float ov, int seed, float hip = 0.28f, float[] eyes = null)
    {
        b.Push(c, 0f);
        float Sh = span * 0.5f + ov, Lh = len * 0.5f + ov * 0.8f;
        float yE = -0.06f, yB = -0.42f;
        float A = rise * 1.06f - yE;
        int nx = Mathf.Max(12, Mathf.CeilToInt(2f * Lh / 0.4f)), nz = 34;
        var T = new Vector3[nx + 1, nz + 1];
        for (int i = 0; i <= nx; i++)
            for (int j = 0; j <= nz; j++)
            {
                float x = (i / (float)nx - 0.5f) * 2f * Lh;
                float z = (j / (float)nz - 0.5f) * 2f * Sh;
                float y = ThatchH(x, z, Sh, Lh, A, yE, hip, seed);
                float zz = z;
                bool edge = j == 0 || j == nz, endE = i == 0 || i == nx;
                if (edge) { y -= 0.03f + 0.07f * Rnd(seed + 7, i); zz += (j == 0 ? -1f : 1f) * 0.05f * Rnd(seed + 3, i); }
                if (endE) { y -= 0.03f + 0.06f * Rnd(seed + 11, j); }
                T[i, j] = new Vector3(x, y, zz);
            }
        Grid(b, sThatch, T, true, false, 1f, 1f);

        // bibir tepi membulat: lingkaran sekeliling atap (cincin tertutup) turun ke langit-langit
        int nl = 2 * (nx + nz);
        var loop = new Vector3[nl + 1];
        int li = 0;
        for (int i = 0; i < nx; i++) loop[li++] = T[i, 0];
        for (int j = 0; j < nz; j++) loop[li++] = T[nx, j];
        for (int i = nx; i > 0; i--) loop[li++] = T[i, nz];
        for (int j = nz; j > 0; j--) loop[li++] = T[0, j];
        loop[nl] = loop[0];
        var R = new Vector3[nl + 1, 4];
        for (int q = 0; q <= nl; q++)
        {
            Vector3 p0 = loop[q];
            Vector3 tg = loop[q == nl ? 1 : q + 1] - loop[q == 0 ? nl - 1 : q - 1];
            Vector3 outv = new Vector3(tg.z, 0f, -tg.x);
            if (outv.sqrMagnitude < 1e-8f) outv = new Vector3(p0.x, 0f, p0.z);
            outv = outv.normalized;
            float h0 = Mathf.Max(0.2f, p0.y - yB);
            R[q, 0] = p0;
            R[q, 1] = new Vector3(p0.x + outv.x * 0.07f, p0.y - 0.14f * h0, p0.z + outv.z * 0.07f);
            R[q, 2] = new Vector3(p0.x + outv.x * 0.09f, p0.y - 0.62f * h0, p0.z + outv.z * 0.09f);
            R[q, 3] = new Vector3(p0.x + outv.x * 0.02f, yB, p0.z + outv.z * 0.02f);
        }
        Grid(b, sThatch, R, false, true, 1f, 1f);
        QOut(b, sWood, new Vector3(-Lh, yB, -Sh), new Vector3(Lh, yB, -Sh), new Vector3(Lh, yB, Sh), new Vector3(-Lh, yB, Sh), new Vector3(0f, -1f, 0f));

        // gulungan punggung (mengikuti tinggi punggung, mengecil di ujung limas) + ikatan kayu + silang kayu di ujung
        int nr = Mathf.Max(8, nx);
        var Rr = new Vector3[9, nr + 1];
        float rxx = Lh * 0.96f;
        for (int q = 0; q < 9; q++)
            for (int m = 0; m <= nr; m++)
            {
                float x = -rxx + 2f * rxx * m / nr;
                float tx = Mathf.Abs(x) / Lh;
                float endDrop = 1f - 0.84f * Sm01((tx - (1f - hip)) / hip);
                float yc = yE + A * endDrop + Wob(x, 0f, seed) * endDrop;
                float rad = (0.17f + 0.1f * endDrop) * (0.7f + 0.3f * Sm01(1f - tx * tx));
                float th = q / 8f * Mathf.PI * 2f;
                Rr[q, m] = new Vector3(x, yc + 0.03f + Mathf.Sin(th) * rad, Mathf.Cos(th) * rad * 1.15f);
            }
        Grid(b, sThatch, Rr, true, true, 1f, 1f);
        for (int k = -1; k <= 1; k++)
        {
            float x = k * Lh * 0.5f;
            float tx = Mathf.Abs(x) / Lh;
            float endDrop = 1f - 0.84f * Sm01((tx - (1f - hip)) / hip);
            float yc = yE + A * endDrop + Wob(x, 0f, seed) * endDrop + 0.03f;
            Prism(b, sWood, new Vector3(x - 0.035f, yc, 0f), new Vector3(x + 0.035f, yc, 0f), 0.31f * (0.7f + 0.3f * endDrop), 0.31f * (0.7f + 0.3f * endDrop), 8, false, false);
        }
        for (int sx = -1; sx <= 1; sx += 2)
        {
            float x = sx * Lh * 0.8f;
            float tx = Mathf.Abs(x) / Lh;
            float endDrop = 1f - 0.84f * Sm01((tx - (1f - hip)) / hip);
            float yc = yE + A * endDrop + 0.2f;
            b.Beam(sWood, new Vector3(x, yc, -0.34f), new Vector3(x + sx * 0.12f, yc + 0.72f, 0.34f), 0.075f, 0.065f, new Vector3(1f, 0f, 0f));
            b.Beam(sWood, new Vector3(x, yc, 0.34f), new Vector3(x + sx * 0.12f, yc + 0.72f, -0.34f), 0.075f, 0.065f, new Vector3(1f, 0f, 0f));
        }
        if (eyes != null)
            for (int e = 0; e < eyes.Length; e++) ThatchEyebrow(b, sThatch, sWood, eyes[e], Sh, Lh, A, yE, hip, seed + e * 5);
        b.Pop();
    }
}
