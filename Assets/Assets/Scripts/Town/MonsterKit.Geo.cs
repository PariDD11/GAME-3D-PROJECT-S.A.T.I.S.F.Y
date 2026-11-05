using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Alat geometri organik untuk model monster buatan kode: tabung berprofil (badan, leher, anggota gerak, ekor), elipsoid, tanduk/duri, batu.
    /// Semua titik ditulis dalam ruang bentuk (+Z depan, +Y atas, kaki di y = 0). Hasil berupa saluran GeoBag (slot MS.*) dengan normal halus,
    /// sehingga garis tepi toon (inverted hull) tidak bocor.
    /// </summary>
    public static partial class MonsterKit
    {
        public static Vector3 Mx(Vector3 p) { return new Vector3(-p.x, p.y, p.z); }
        public static Color Dk(Color c, float f) { return new Color(Mathf.Clamp01(c.r * f), Mathf.Clamp01(c.g * f), Mathf.Clamp01(c.b * f), 1f); }
        public static Color Mix(Color a, Color b, float t) { return new Color(Mathf.Lerp(a.r, b.r, t), Mathf.Lerp(a.g, b.g, t), Mathf.Lerp(a.b, b.b, t), 1f); }

        static Vector3 CRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        static float CRomF(float p0, float p1, float p2, float p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        /// <summary>Hash deterministik 0..1 (untuk variasi batu/duri; tidak memakai Random agar mesh bisa di-cache).</summary>
        static float Hash(int a, int b, int seed)
        {
            unchecked
            {
                int h = a * 374761393 + b * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177; h ^= h >> 16;
                return (h & 0xffffff) / (float)0x1000000;
            }
        }

        // ------------------------------------------------------------------ permukaan dari grid
        static void AddTri(List<Vector3> v, List<Vector3> n, List<int> tri, int a, int b, int c)
        {
            Vector3 g = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
            if (g.sqrMagnitude < 1e-10f) return;
            if (Vector3.Dot(g, n[a] + n[b] + n[c]) < 0f) { int t = b; b = c; c = t; }     // sisi depan = arah normal
            tri.Add(a); tri.Add(b); tri.Add(c);
        }

        static Vector3 GridNormal(Vector3[,] P, int i, int j, int R, int S, Vector3 ctr)
        {
            Vector3 du = P[i, (j + 1) % S] - P[i, (j + S - 1) % S];
            Vector3 dv = P[Mathf.Min(R - 1, i + 1), j] - P[Mathf.Max(0, i - 1), j];
            Vector3 nn = Vector3.Cross(du, dv);
            Vector3 rc = Vector3.zero;
            for (int k = 0; k < S; k++) rc += P[i, k];
            rc /= S;
            Vector3 rd = P[i, j] - rc;
            if (rd.sqrMagnitude < 1e-8f) rd = P[i, j] - ctr;
            if (nn.sqrMagnitude < 1e-12f) return rd.sqrMagnitude > 1e-10f ? rd.normalized : Vector3.up;
            if (Vector3.Dot(nn, rd) < 0f) nn = -nn;
            return nn.normalized;
        }

        /// <summary>Permukaan tertutup keliling dari grid titik P[i,j] (i sepanjang, j keliling melingkar). N = normal (null = dihitung).</summary>
        public static void Grid(GeoBag b, int slot, Vector3[,] P, Vector3[,] N) { Grid(b, slot, P, N, true); }

        /// <summary>wrap = false: kolom terakhir tidak disambung ke kolom pertama (potongan busur).</summary>
        public static void Grid(GeoBag b, int slot, Vector3[,] P, Vector3[,] N, bool wrap)
        {
            int R = P.GetLength(0), S = P.GetLength(1);
            if (R < 2 || S < (wrap ? 3 : 2)) return;
            var v = new List<Vector3>(R * S); var n = new List<Vector3>(R * S); var uv = new List<Vector2>(R * S); var tri = new List<int>(R * S * 6);
            Vector3 ctr = Vector3.zero;
            for (int i = 0; i < R; i++) for (int j = 0; j < S; j++) ctr += P[i, j];
            ctr /= (R * S);
            for (int i = 0; i < R; i++)
                for (int j = 0; j < S; j++)
                {
                    v.Add(P[i, j]);
                    n.Add(N != null ? N[i, j] : GridNormal(P, i, j, R, S, ctr));
                    uv.Add(new Vector2(j / (float)S, i / (float)(R - 1)));
                }
            for (int i = 0; i + 1 < R; i++)
                for (int j = 0; j < (wrap ? S : S - 1); j++)
                {
                    int j2 = (j + 1) % S;
                    int a = i * S + j, bb = i * S + j2, c = (i + 1) * S + j2, d = (i + 1) * S + j;
                    AddTri(v, n, tri, a, bb, c);
                    AddTri(v, n, tri, a, c, d);
                }
            b.Raw(slot, v, n, uv, tri);
        }

        // ------------------------------------------------------------------ elipsoid
        /// <summary>Elipsoid dengan radius r (x,y,z) dan orientasi rot. Kutub di sumbu Y lokal.</summary>
        public static void Ell(GeoBag b, int slot, Vector3 c, Vector3 r, Quaternion rot, int rings = 6, int segs = 10)
        {
            r = new Vector3(Mathf.Max(0.001f, r.x), Mathf.Max(0.001f, r.y), Mathf.Max(0.001f, r.z));
            var P = new Vector3[rings + 1, segs]; var N = new Vector3[rings + 1, segs];
            for (int i = 0; i <= rings; i++)
            {
                float lat = (i / (float)rings - 0.5f) * Mathf.PI;
                float y = Mathf.Sin(lat), rr = Mathf.Cos(lat);
                for (int j = 0; j < segs; j++)
                {
                    float a = j / (float)segs * Mathf.PI * 2f;
                    Vector3 u = new Vector3(Mathf.Sin(a) * rr, y, Mathf.Cos(a) * rr);
                    P[i, j] = c + rot * new Vector3(u.x * r.x, u.y * r.y, u.z * r.z);
                    N[i, j] = (rot * new Vector3(u.x / r.x, u.y / r.y, u.z / r.z)).normalized;
                }
            }
            Grid(b, slot, P, N);
        }

        public static void Ell(GeoBag b, int slot, Vector3 c, Vector3 r, int rings = 6, int segs = 10) { Ell(b, slot, c, r, Quaternion.identity, rings, segs); }

        /// <summary>Elipsoid dengan sumbu panjang (Z lokal) menunjuk dari a ke b; rx/ry = radius penampang, panjang = jarak a-b / 2 * stretch.</summary>
        public static void EllAB(GeoBag b, int slot, Vector3 a, Vector3 bp, float rx, float ry, float stretch = 1f, int rings = 6, int segs = 10)
        {
            Vector3 d = bp - a; float len = d.magnitude; if (len < 1e-4f) return;
            Quaternion q = Face(d, Vector3.up);
            Ell(b, slot, (a + bp) * 0.5f, new Vector3(rx, ry, len * 0.5f * stretch), q, rings, segs);
        }

        /// <summary>Rotasi yang membuat sumbu Z menunjuk ke dir dengan sumbu Y sedekat mungkin ke up (tanpa Quaternion.LookRotation agar bisa diuji di luar Unity).</summary>
        public static Quaternion Face(Vector3 dir, Vector3 up)
        {
            Vector3 f = dir.normalized;
            Vector3 s = Vector3.Cross(up, f);
            if (s.sqrMagnitude < 1e-6f) s = Vector3.Cross(Vector3.right, f);
            s = s.normalized;
            Vector3 u = Vector3.Cross(f, s);
            // matriks -> kuaternion (kolom: s, u, f)
            float m00 = s.x, m01 = u.x, m02 = f.x, m10 = s.y, m11 = u.y, m12 = f.y, m20 = s.z, m21 = u.z, m22 = f.z;
            float tr = m00 + m11 + m22; Quaternion q;
            if (tr > 0f) { float k = Mathf.Sqrt(tr + 1f) * 2f; q = new Quaternion((m21 - m12) / k, (m02 - m20) / k, (m10 - m01) / k, 0.25f * k); }
            else if (m00 > m11 && m00 > m22) { float k = Mathf.Sqrt(1f + m00 - m11 - m22) * 2f; q = new Quaternion(0.25f * k, (m01 + m10) / k, (m02 + m20) / k, (m21 - m12) / k); }
            else if (m11 > m22) { float k = Mathf.Sqrt(1f + m11 - m00 - m22) * 2f; q = new Quaternion((m01 + m10) / k, 0.25f * k, (m12 + m21) / k, (m02 - m20) / k); }
            else { float k = Mathf.Sqrt(1f + m22 - m00 - m11) * 2f; q = new Quaternion((m02 + m20) / k, (m12 + m21) / k, 0.25f * k, (m10 - m01) / k); }
            float mag = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            return new Quaternion(q.x / mag, q.y / mag, q.z / mag, q.w / mag);
        }

        // ------------------------------------------------------------------ tabung berprofil
        static readonly float[] CapAng4 = { 90f, 68f, 46f, 24f }, CapAng3 = { 90f, 58f, 26f }, CapAng2 = { 90f, 45f };
        // ujung membulat: makin kecil radius makin sedikit cincin (menghemat segitiga pada cakar/duri/jari)
        static float[] CapFor(float ra) { return ra >= 0.1f ? CapAng4 : (ra >= 0.04f ? CapAng3 : CapAng2); }

        /// <summary>
        /// Tabung mengikuti kurva pts (Catmull-Rom) dengan penampang elips (rx lebar, ry tinggi menurut sumbu atas up) per titik.
        /// capA/capB = ujung membulat seperti setengah bola (tertutup).
        /// </summary>
        sealed class Rings { public int R; public Vector3[] C, T, Sd, Up; public float[] RX, RY, slope; public bool[] tip; }

        static Rings MakeRings(Vector3[] pts, float[] rx, float[] ry, Vector3 up, bool capA, bool capB)
        {
            int m = pts.Length; if (m < 2) return null;
            var C = new List<Vector3>(); var RX = new List<float>(); var RY = new List<float>();
            float plen = 0f; for (int i = 0; i + 1 < m; i++) plen += (pts[i + 1] - pts[i]).magnitude;
            int sub = plen > 0.6f ? 3 : (plen > 0.25f ? 2 : 1);
            if (m == 2)
            {
                // lurus & meruncing linear: dua cincin sudah tepat (normal analitik memuat kemiringan)
                C.Add(pts[0]); RX.Add(Mathf.Max(0.001f, rx[0])); RY.Add(Mathf.Max(0.001f, ry[0]));
                C.Add(pts[1]); RX.Add(Mathf.Max(0.001f, rx[1])); RY.Add(Mathf.Max(0.001f, ry[1]));
            }
            else
            {
                for (int i = 0; i < m - 1; i++)
                    for (int k = 0; k < sub; k++)
                    {
                        float t = k / (float)sub; int i0 = Mathf.Max(0, i - 1), i3 = Mathf.Min(m - 1, i + 2);
                        C.Add(CRom(pts[i0], pts[i], pts[i + 1], pts[i3], t));
                        RX.Add(Mathf.Max(0.001f, CRomF(rx[i0], rx[i], rx[i + 1], rx[i3], t)));
                        RY.Add(Mathf.Max(0.001f, CRomF(ry[i0], ry[i], ry[i + 1], ry[i3], t)));
                    }
                C.Add(pts[m - 1]); RX.Add(Mathf.Max(0.001f, rx[m - 1])); RY.Add(Mathf.Max(0.001f, ry[m - 1]));
            }
            // ujung membulat: tambahkan cincin setengah bola
            if (capA)
            {
                Vector3 t0 = (C[1] - C[0]).normalized; float ra = (RX[0] + RY[0]) * 0.5f; float x0 = RX[0], y0 = RY[0];
                Vector3 c0 = C[0]; float[] cap = CapFor(ra);
                for (int q = cap.Length - 1; q >= 0; q--)          // dari yang terdekat ke badan; ujung (90 derajat) disisipkan terakhir di depan
                {
                    float ph = cap[q] * Mathf.Deg2Rad;
                    C.Insert(0, c0 - t0 * (ra * Mathf.Sin(ph))); RX.Insert(0, Mathf.Max(0.0005f, x0 * Mathf.Cos(ph))); RY.Insert(0, Mathf.Max(0.0005f, y0 * Mathf.Cos(ph)));
                }
            }
            if (capB)
            {
                int e = C.Count - 1; Vector3 t1 = (C[e] - C[e - 1]).normalized; float ra = (RX[e] + RY[e]) * 0.5f; float x1 = RX[e], y1 = RY[e]; Vector3 c1 = C[e];
                float[] cap = CapFor(ra);
                for (int q = cap.Length - 1; q >= 0; q--)
                {
                    float ph = cap[q] * Mathf.Deg2Rad;
                    C.Add(c1 + t1 * (ra * Mathf.Sin(ph))); RX.Add(Mathf.Max(0.0005f, x1 * Mathf.Cos(ph))); RY.Add(Mathf.Max(0.0005f, y1 * Mathf.Cos(ph)));
                }
            }
            int R = C.Count;
            var d = new Rings { R = R, C = C.ToArray(), RX = RX.ToArray(), RY = RY.ToArray(), T = new Vector3[R], Sd = new Vector3[R], Up = new Vector3[R], slope = new float[R], tip = new bool[R] };
            var rm = new float[R];
            for (int k = 0; k < R; k++)
            {
                Vector3 dd = d.C[Mathf.Min(R - 1, k + 1)] - d.C[Mathf.Max(0, k - 1)];
                d.T[k] = dd.sqrMagnitude > 1e-10f ? dd.normalized : (k > 0 ? d.T[k - 1] : Vector3.forward);
                rm[k] = (d.RX[k] + d.RY[k]) * 0.5f;
            }
            for (int k = 0; k < R; k++)
            {
                Vector3 s;
                if (k == 0)
                {
                    s = Vector3.Cross(up, d.T[0]);
                    if (s.sqrMagnitude < 1e-6f) s = Vector3.Cross(Vector3.right, d.T[0]);
                }
                else
                {
                    s = d.Sd[k - 1] - d.T[k] * Vector3.Dot(d.Sd[k - 1], d.T[k]);
                    if (s.sqrMagnitude < 1e-8f) s = Vector3.Cross(up, d.T[k]);
                    if (s.sqrMagnitude < 1e-8f) s = Vector3.Cross(Vector3.right, d.T[k]);
                }
                d.Sd[k] = s.normalized; d.Up[k] = Vector3.Cross(d.T[k], d.Sd[k]).normalized;
                int k0 = Mathf.Max(0, k - 1), k1 = Mathf.Min(R - 1, k + 1);
                float ds = (d.C[k1] - d.C[k0]).magnitude;
                d.slope[k] = ds > 1e-6f ? (rm[k1] - rm[k0]) / ds : 0f;
                d.tip[k] = d.RX[k] * d.RY[k] < 1e-6f;
            }
            return d;
        }

        // busur penampang a0..a1 (derajat; 0 = sumbu samping +, 90 = atas up); wrap bila penuh 360
        static void RingSurface(GeoBag b, int slot, Rings d, int segs, float a0, float a1, bool wrap)
        {
            int R = d.R; int cols = wrap ? segs : segs + 1;
            var P = new Vector3[R, cols]; var N = new Vector3[R, cols];
            for (int k = 0; k < R; k++)
            {
                Vector3 tipN = Vector3.zero;
                if (d.tip[k]) { tipN = k == 0 ? -d.T[k] : d.T[k]; if (k > 0 && k < R - 1) tipN = d.T[k]; }
                for (int j = 0; j < cols; j++)
                {
                    float a = (a0 + (a1 - a0) * (wrap ? j / (float)segs : j / (float)segs)) * Mathf.Deg2Rad;
                    float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                    P[k, j] = d.C[k] + d.Sd[k] * (ca * d.RX[k]) + d.Up[k] * (sa * d.RY[k]);
                    if (d.tip[k]) { N[k, j] = tipN; continue; }
                    float nx = ca / d.RX[k], ny = sa / d.RY[k]; float nl = Mathf.Sqrt(nx * nx + ny * ny);
                    nx /= nl; ny /= nl;
                    N[k, j] = (d.Sd[k] * nx + d.Up[k] * ny - d.T[k] * d.slope[k]).normalized;
                }
            }
            Grid(b, slot, P, N, wrap);
        }

        public static void Tube(GeoBag b, int slot, Vector3[] pts, float[] rx, float[] ry, int segs, Vector3 up, bool capA, bool capB)
        {
            var d = MakeRings(pts, rx, ry, up, capA, capB); if (d == null) return;
            RingSurface(b, slot, d, segs, 0f, 360f, true);
        }

        /// <summary>
        /// Tabung berpita warna: penampang dibagi menjadi pita menurut sudut (derajat, naik; 0 = samping +X, 90 = atas, 270 = bawah).
        /// ang punya (jumlah slot + 1) nilai; tiap pita memakai slot masing-masing. Sambungan antarpita memakai titik yang sama (tanpa celah).
        /// </summary>
        public static void TubeBands(GeoBag b, int[] slots, float[] ang, Vector3[] pts, float[] rx, float[] ry, int segsPerBand, Vector3 up, bool capA, bool capB)
        {
            var d = MakeRings(pts, rx, ry, up, capA, capB); if (d == null) return;
            for (int i = 0; i < slots.Length; i++)
            {
                float span = ang[i + 1] - ang[i];
                int sg = Mathf.Max(2, Mathf.RoundToInt(segsPerBand * span / 90f));
                RingSurface(b, slots[i], d, sg, ang[i], ang[i + 1], false);
            }
        }

        /// <summary>
        /// Potongan elipsoid terbuka (tudung, helm, bahu zirah, tameng, cangkang): azimut azA..azB (derajat dari +Z, berputar ke +X), lintang latA..latB (-90..90).
        /// innerSlot >= 0 menambah lapisan dalam (normal terbalik) agar sisi dalam terlihat dari depan yang terbuka.
        /// </summary>
        public static void EllArc(GeoBag b, int slot, Vector3 c, Vector3 r, Quaternion rot, int rings, int segs, float azA, float azB, float latA, float latB, int innerSlot = -1, float innerK = 0.93f)
        {
            r = new Vector3(Mathf.Max(0.001f, r.x), Mathf.Max(0.001f, r.y), Mathf.Max(0.001f, r.z));
            for (int pass = 0; pass < (innerSlot >= 0 ? 2 : 1); pass++)
            {
                float k = pass == 0 ? 1f : innerK; float sg = pass == 0 ? 1f : -1f;
                var P = new Vector3[rings + 1, segs + 1]; var N = new Vector3[rings + 1, segs + 1];
                for (int i = 0; i <= rings; i++)
                {
                    float lat = Mathf.Lerp(latA, latB, i / (float)rings) * Mathf.Deg2Rad;
                    float y = Mathf.Sin(lat), rr = Mathf.Cos(lat);
                    for (int j = 0; j <= segs; j++)
                    {
                        float a = Mathf.Lerp(azA, azB, j / (float)segs) * Mathf.Deg2Rad;
                        Vector3 u = new Vector3(Mathf.Sin(a) * rr, y, Mathf.Cos(a) * rr);
                        P[i, j] = c + rot * new Vector3(u.x * r.x * k, u.y * r.y * k, u.z * r.z * k);
                        N[i, j] = (rot * new Vector3(u.x / r.x, u.y / r.y, u.z / r.z)).normalized * sg;
                    }
                }
                Grid(b, pass == 0 ? slot : innerSlot, P, N, false);
            }
        }

        /// <summary>Tabung bulat (rx = ry).</summary>
        public static void Tube(GeoBag b, int slot, Vector3[] pts, float[] r, int segs, bool capA, bool capB)
        {
            Tube(b, slot, pts, r, r, segs, Vector3.up, capA, capB);
        }

        /// <summary>Tabung lurus a -> b dengan radius ra -> rb.</summary>
        public static void Limb(GeoBag b, int slot, Vector3 a, Vector3 c, float ra, float rb, int segs = 8, bool capA = true, bool capB = true)
        {
            Tube(b, slot, new[] { a, c }, new[] { ra, rb }, new[] { ra, rb }, segs, Vector3.up, capA, capB);
        }

        /// <summary>Tanduk/duri melengkung: bend = pergeseran titik tengah dari garis lurus.</summary>
        public static void Horn(GeoBag b, int slot, Vector3 baseP, Vector3 tip, Vector3 bend, float r0, int segs = 6)
        {
            Vector3 m1 = Vector3.Lerp(baseP, tip, 0.38f) + bend * 0.55f, m2 = Vector3.Lerp(baseP, tip, 0.72f) + bend;
            var r = new[] { r0, r0 * 0.84f, r0 * 0.52f, Mathf.Max(0.004f, r0 * 0.07f) };
            Tube(b, slot, new[] { baseP, m1, m2, tip }, r, r, segs, Vector3.up, true, true);
        }

        /// <summary>Kerucut lurus (taring, cakar, duri kecil).</summary>
        public static void Spike(GeoBag b, int slot, Vector3 baseP, Vector3 tip, float r, int segs = 5)
        {
            Tube(b, slot, new[] { baseP, tip }, new[] { r, Mathf.Max(0.003f, r * 0.05f) }, new[] { r, Mathf.Max(0.003f, r * 0.05f) }, segs, Vector3.up, true, false);
        }

        /// <summary>Batu/gumpalan kasar: elipsoid beresolusi rendah dengan jitter radius.</summary>
        public static void Rock(GeoBag b, int slot, Vector3 c, Vector3 r, Quaternion rot, int seed, float rough, int rings = 4, int segs = 7)
        {
            var P = new Vector3[rings + 1, segs];
            for (int i = 0; i <= rings; i++)
            {
                float lat = (i / (float)rings - 0.5f) * Mathf.PI;
                float y = Mathf.Sin(lat), rr = Mathf.Cos(lat);
                for (int j = 0; j < segs; j++)
                {
                    float a = j / (float)segs * Mathf.PI * 2f;
                    float jit = 1f + (Hash(i, j, seed) - 0.5f) * 2f * rough;
                    if (i == 0 || i == rings) jit = 1f;
                    Vector3 u = new Vector3(Mathf.Sin(a) * rr, y, Mathf.Cos(a) * rr) * jit;
                    P[i, j] = c + rot * new Vector3(u.x * r.x, u.y * r.y, u.z * r.z);
                }
            }
            Grid(b, slot, P, null);
        }

        /// <summary>Bidang datar dua sisi (selaput sayap, kain, sirip). Titik a-b-c-d berurutan.</summary>
        public static void Sheet(GeoBag b, int slot, Vector3 a, Vector3 bb, Vector3 c, Vector3 d)
        {
            b.QuadBoth(slot, a, bb, c, d);
        }
    }
}
