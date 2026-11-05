using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generator pohon prosedural gaya anime-fantasi (menggantikan kanopi "bola bersisik" lama):
///  - batang berakar melebar dengan dahan bercabang yang terlihat (bukan tabung lurus),
///  - mahkota = gumpalan halus yang saling tumpang tindih (siluet bergelombang seperti awan), tiap gumpalan di slot gelap/tengah/terang
///    dan normalnya dicampur ke arah luar mahkota supaya satu pohon terbaca sebagai satu volume yang diterangi lembut,
///  - cemara bertingkat dengan tepi bergerigi menjuntai, sipres beralur, birch ramping, sakura melengkung, apel, pohon mati, semak.
/// Semua permukaan dibuat lewat GeoBag.Raw (normal dan UV sendiri, UV tercermin sehingga tidak ada kelim tekstur).
/// Palet slot diberikan pemanggil, jadi kit kota (Sl.Hedge/Leaf/...) dan kit hutan (ForestKit.*) memakai bentuk yang sama.
/// </summary>
public static class TreeGen
{
    // ------------------------------------------------------------------ palet
    public struct Pal
    {
        public int bark, dark, mid, light, accent, fruit;
        public Pal(int bark, int dark, int mid, int light, int accent, int fruit)
        { this.bark = bark; this.dark = dark; this.mid = mid; this.light = light; this.accent = accent; this.fruit = fruit; }
    }

    /// <summary>Kit kota/desa: kulit kayu Sl.Leather, daun Hedge (gelap) - Fur (tengah) - Leaf (terang), Hair (hangat). Diisi TownMats.Slots().</summary>
    public static Pal Town { get { return new Pal(Sl.Leather, Sl.Hedge, Sl.Fur, Sl.Leaf, Sl.Hair, Sl.Cloth); } }
    /// <summary>Sakura kit kota: merah muda tua (Lip) - Blossom - pucat (EyeW).</summary>
    public static Pal TownBlossom { get { return new Pal(Sl.Leather, Sl.Lip, Sl.Blossom, Sl.EyeW, Sl.Blossom, Sl.Cloth); } }
    /// <summary>Kit hutan (ForestKit.Slots): Bark, LeafPine (gelap), Ivy (tengah), LeafGreen (terang), LeafYellow (hangat).</summary>
    public static Pal Forest { get { return new Pal(ForestKit.Bark, ForestKit.Ivy, ForestKit.LeafGreen, ForestKit.LeafLight, ForestKit.LeafYellow, ForestKit.LeafOrange); } }
    /// <summary>Cemara hutan: LeafPine (gelap) - Ivy - LeafGreen.</summary>
    public static Pal ForestPine { get { return new Pal(ForestKit.Bark, ForestKit.LeafPine, ForestKit.Ivy, ForestKit.LeafGreen, ForestKit.LeafYellow, -1); } }
    /// <summary>Maple musim gugur: merah (bawah) - oranye (tengah/atas), jumbai kecil kuning-hijau di tepi tajuk.</summary>
    public static Pal Autumn { get { return new Pal(ForestKit.Bark, ForestKit.LeafRed, ForestKit.LeafOrange, ForestKit.LeafOrange, ForestKit.LeafYellow, ForestKit.LeafRed); } }

    // ------------------------------------------------------------------ acak & noise
    sealed class Rng
    {
        uint s;
        public Rng(int seed) { unchecked { s = (uint)seed * 2654435761u + 0x9E3779B9u; } if (s == 0u) s = 1u; for (int i = 0; i < 4; i++) F(); }
        public float F() { s ^= s << 13; s ^= s >> 17; s ^= s << 5; return (s & 0xFFFFFFu) / 16777216f; }
        public float R(float a, float b) { return a + (b - a) * F(); }
        public int I(int n) { return Mathf.Min(n - 1, (int)(F() * n)); }
    }

    static float Hash3(int x, int y, int z, int seed)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + z * 1274126177 + seed * 1442695041;
            h = (h ^ (h >> 13)) * 1274126177; h ^= h >> 16;
            return (h & 0xffffff) / 16777216f;
        }
    }

    static float N3(float x, float y, float z, int seed)
    {
        int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y), zi = Mathf.FloorToInt(z);
        float fx = x - xi, fy = y - yi, fz = z - zi;
        fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy); fz = fz * fz * (3f - 2f * fz);
        float a = Mathf.Lerp(Mathf.Lerp(Hash3(xi, yi, zi, seed), Hash3(xi + 1, yi, zi, seed), fx), Mathf.Lerp(Hash3(xi, yi + 1, zi, seed), Hash3(xi + 1, yi + 1, zi, seed), fx), fy);
        float c = Mathf.Lerp(Mathf.Lerp(Hash3(xi, yi, zi + 1, seed), Hash3(xi + 1, yi, zi + 1, seed), fx), Mathf.Lerp(Hash3(xi, yi + 1, zi + 1, seed), Hash3(xi + 1, yi + 1, zi + 1, seed), fx), fy);
        return Mathf.Lerp(a, c, fz);
    }

    static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }
    static Vector3 RotY(Vector3 v, float deg) { float a = deg * Mathf.Deg2Rad, c = Mathf.Cos(a), sn = Mathf.Sin(a); return new Vector3(v.x * c + v.z * sn, v.y, -v.x * sn + v.z * c); }
    static Vector3 Flat(float azRad) { return new Vector3(Mathf.Sin(azRad), 0f, Mathf.Cos(azRad)); }

    // ------------------------------------------------------------------ permukaan grid -> mesh halus
    delegate Vector3 NFx(Vector3 p, Vector3 n);

    static void AddTri(List<int> t, int a, int b, int c, bool flip)
    {
        if (!flip) { t.Add(a); t.Add(b); t.Add(c); } else { t.Add(a); t.Add(c); t.Add(b); }
    }

    /// <summary>
    /// P[i,j]: baris i (pangkal -> ujung), kolom j mengelilingi (jumlah kolom genap). Baris yang semua titiknya sama = kutub/ujung.
    /// Normal dihitung dari selisih tengah (halus), arah luar dideteksi otomatis. UV: u tercermin sepanjang keliling (meter), v = vRow[i] (meter).
    /// </summary>
    static void Surf(GeoBag b, int slot, Vector3[,] P, float circ, float[] vRow, Vector3 firstN, Vector3 lastN, NFx fx)
    {
        int rows = P.GetLength(0), cols = P.GetLength(1);
        int half = cols / 2, W = cols + 2;
        var rc = new Vector3[rows];
        var apex = new bool[rows];
        for (int i = 0; i < rows; i++)
        {
            Vector3 sum = Vector3.zero;
            for (int j = 0; j < cols; j++) sum += P[i, j];
            rc[i] = sum / cols;
            float md = 0f;
            for (int j = 0; j < cols; j++) md = Mathf.Max(md, (P[i, j] - rc[i]).sqrMagnitude);
            apex[i] = md < 1e-7f;
        }
        var N = new Vector3[rows, cols];
        var deg = new bool[rows, cols];
        bool flip = false, flipSet = false;
        for (int i = 0; i < rows; i++)
        {
            if (apex[i]) continue;
            int ia = Mathf.Max(0, i - 1), ib = Mathf.Min(rows - 1, i + 1);
            for (int j = 0; j < cols; j++)
            {
                Vector3 tp = P[i, (j + 1) % cols] - P[i, (j + cols - 1) % cols];
                Vector3 tr = P[ib, j] - P[ia, j];
                Vector3 n = Vector3.Cross(tp, tr);
                Vector3 ov = P[i, j] - rc[i];
                if (n.sqrMagnitude < 1e-14f) { n = ov; deg[i, j] = true; }
                else if (!flipSet && ov.sqrMagnitude > 1e-8f)
                {
                    float d = Vector3.Dot(n, ov);
                    if (Mathf.Abs(d) > 1e-12f) { flip = d < 0f; flipSet = true; }
                }
                N[i, j] = n;
            }
        }
        for (int i = 0; i < rows; i++)
        {
            if (apex[i]) continue;
            for (int j = 0; j < cols; j++)
            {
                Vector3 n = N[i, j];
                if (!deg[i, j] && flip) n = -n;
                N[i, j] = n.sqrMagnitude > 1e-14f ? n.normalized : Vector3.up;
            }
        }
        for (int e = 0; e < 2; e++)
        {
            int i = e == 0 ? 0 : rows - 1, nb = e == 0 ? 1 : rows - 2;
            if (!apex[i]) continue;
            Vector3 g = e == 0 ? firstN : lastN;
            if (g.sqrMagnitude < 1e-8f)
            {
                g = Vector3.zero;
                if (nb >= 0 && nb < rows && !apex[nb]) for (int j = 0; j < cols; j++) g += N[nb, j];
                if (g.sqrMagnitude < 1e-8f) g = Vector3.up;
            }
            g.Normalize();
            for (int j = 0; j < cols; j++) N[i, j] = g;
        }
        if (fx != null)
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++) N[i, j] = fx(P[i, j], N[i, j]);

        var Vv = new List<Vector3>(rows * W); var Nn = new List<Vector3>(rows * W); var UV = new List<Vector2>(rows * W); var T = new List<int>(rows * cols * 6);
        float du = circ / cols;
        for (int i = 0; i < rows; i++)
            for (int c = 0; c < W; c++)
            {
                int j = c <= half ? c : c - 1;
                float u = c <= half ? j * du : (cols - j) * du;
                int jj = j % cols;
                Vv.Add(P[i, jj]); Nn.Add(N[i, jj]); UV.Add(new Vector2(u, vRow[i]));
            }
        for (int i = 0; i + 1 < rows; i++)
            for (int c = 0; c + 1 < W; c++)
            {
                if (c == half) continue;
                int a = i * W + c, bb = a + 1, cc = (i + 1) * W + c, d = cc + 1;
                if (apex[i] && apex[i + 1]) continue;
                if (apex[i]) AddTri(T, bb, d, cc, flip);
                else if (apex[i + 1]) AddTri(T, a, bb, cc, flip);
                else { AddTri(T, a, bb, cc, flip); AddTri(T, bb, d, cc, flip); }
            }
        b.Raw(slot, Vv, Nn, UV, T);
    }

    // ------------------------------------------------------------------ gumpalan (puff)
    /// <summary>Gumpalan daun halus (elipsoid + noise). rings = jumlah cincin antar kutub, cols = segmen keliling (genap).
    /// mix = campuran normal menuju arah luar mahkota (crownC, crownR), flat = pipihnya dasar (0 = bulat).</summary>
    static void Puff(GeoBag b, int slot, Vector3 c, Vector3 r, int seed, float lump, float flat, Vector3 crownC, Vector3 crownR, float mix, int rings, int cols)
    {
        var rg = new Rng(seed * 7 + 3);
        float yaw = rg.F() * 6.2831853f;
        Vector3 ax = Flat(yaw), b1 = Vector3.up, b2 = Vector3.Cross(ax, b1);
        int rows = rings + 2;
        var P = new Vector3[rows, cols];
        var vRow = new float[rows];
        float rm = (r.x + r.y + r.z) / 3f;
        float off = seed * 0.37f;
        for (int i = 0; i < rows; i++)
        {
            float th = Mathf.PI * i / (rows - 1);
            float st = Mathf.Sin(th), ct = Mathf.Cos(th);
            for (int j = 0; j < cols; j++)
            {
                float ph = 2f * Mathf.PI * j / cols;
                Vector3 d = ax * ct + (b1 * Mathf.Cos(ph) + b2 * Mathf.Sin(ph)) * st;
                float f = 1f + lump * (N3(d.x * 1.6f + off, d.y * 1.6f + 3.1f, d.z * 1.6f - off, seed) - 0.5f) * 2f
                             + lump * 0.5f * (N3(d.x * 3.4f - off, d.y * 3.4f, d.z * 3.4f + 7.7f, seed + 5) - 0.5f) * 2f;
                Vector3 p = V(d.x * r.x, d.y * r.y, d.z * r.z) * f;
                if (flat > 0f && p.y < -r.y * flat) p.y = -r.y * flat + (p.y + r.y * flat) * 0.3f;
                P[i, j] = c + p;
            }
            vRow[i] = th * rm;
        }
        NFx fx = null;
        if (mix > 0f)
        {
            Vector3 cc = crownC, cr = crownR;
            fx = (p, n) =>
            {
                Vector3 q = p - cc;
                Vector3 g = V(q.x / (cr.x * cr.x), q.y / (cr.y * cr.y), q.z / (cr.z * cr.z));
                if (g.sqrMagnitude < 1e-10f) return n;
                return Vector3.Lerp(n, g.normalized, mix).normalized;
            };
        }
        Surf(b, slot, P, 2f * Mathf.PI * rm, vRow, Vector3.zero, Vector3.zero, fx);
    }

    // ------------------------------------------------------------------ tabung (batang/dahan)
    /// <summary>Tabung mengikuti titik pts dengan jari-jari rad. flare[i] (opsional) = pelebaran akar di cincin i dengan 'lobes' tonjolan. tip = tutup ujung.</summary>
    static void Tube(GeoBag b, int slot, IList<Vector3> pts, IList<float> rad, int segs, int seed, float irreg, IList<float> flare, int lobes, float lobePh, bool tip, float tipLen = 1.4f)
    {
        int n = pts.Count;
        if (n < 2) return;
        int rows = n + (tip ? 1 : 0);
        var P = new Vector3[rows, segs];
        var vRow = new float[rows];
        float acc = 0f;
        Vector3 Wv = Vector3.zero;
        for (int i = 0; i < n; i++)
        {
            Vector3 d = i == 0 ? pts[1] - pts[0] : (i == n - 1 ? pts[n - 1] - pts[n - 2] : pts[i + 1] - pts[i - 1]);
            d = d.normalized;
            if (i == 0)
            {
                Vector3 refv = Mathf.Abs(d.z) < 0.9f ? Vector3.forward : Vector3.right;
                Wv = (refv - d * Vector3.Dot(refv, d)).normalized;
            }
            else
            {
                Wv = (Wv - d * Vector3.Dot(Wv, d));
                if (Wv.sqrMagnitude < 1e-8f) Wv = Vector3.Cross(d, Vector3.up);
                Wv.Normalize();
            }
            Vector3 Uv = Vector3.Cross(d, Wv);
            for (int j = 0; j < segs; j++)
            {
                float a = j * 2f * Mathf.PI / segs;
                float k = 1f + irreg * (N3(Mathf.Cos(a) * 1.3f + seed * 0.1f, i * 0.9f, Mathf.Sin(a) * 1.3f, seed) - 0.5f) * 2f;
                if (flare != null) { float lobe = Mathf.Pow(0.5f + 0.5f * Mathf.Cos(lobes * a + lobePh), 2f); k *= 1f + flare[i] * (0.15f + 0.85f * lobe); }
                P[i, j] = pts[i] + (Uv * Mathf.Sin(a) + Wv * Mathf.Cos(a)) * (rad[i] * k);
            }
            if (i > 0) acc += (pts[i] - pts[i - 1]).magnitude;
            vRow[i] = acc;
        }
        Vector3 lastN = Vector3.zero;
        if (tip)
        {
            Vector3 d = (pts[n - 1] - pts[n - 2]).normalized;
            Vector3 tp = pts[n - 1] + d * rad[n - 1] * tipLen;
            for (int j = 0; j < segs; j++) P[n, j] = tp;
            vRow[n] = acc + rad[n - 1] * tipLen;
            lastN = d;
        }
        float rAvg = (rad[0] + rad[n - 1]) * 0.5f;
        Surf(b, slot, P, 2f * Mathf.PI * Mathf.Max(0.02f, rAvg), vRow, Vector3.zero, lastN, null);
    }

    static List<Vector3> Path(Vector3 start, Vector3 dir, float len, int n, float up, float wob, Rng rg)
    {
        var pts = new List<Vector3> { start };
        Vector3 d = dir.normalized, p = start;
        float step = len / (n - 1);
        Vector3 side = Vector3.Cross(d, Vector3.up);
        if (side.sqrMagnitude < 1e-4f) side = Vector3.right;
        side.Normalize();
        for (int i = 1; i < n; i++)
        {
            d = (d + Vector3.up * (up / (n - 1)) + side * ((rg.F() - 0.5f) * wob)).normalized;
            p += d * step; pts.Add(p);
        }
        return pts;
    }

    static List<float> Taper(float r0, float r1, int n, float pw)
    {
        var l = new List<float>();
        for (int i = 0; i < n; i++) l.Add(Mathf.Lerp(r0, r1, Mathf.Pow(i / (float)(n - 1), pw)));
        return l;
    }

    static void Branch(GeoBag b, int slot, List<Vector3> pts, float r0, float r1, int segs, int seed, float pw = 0.8f)
    {
        Tube(b, slot, pts, Taper(r0, r1, pts.Count, pw), segs, seed, 0.05f, null, 0, 0f, true, 1.2f);
    }

    // ------------------------------------------------------------------ batang berakar melebar
    /// <summary>Batang dari tanah sampai titik cabang; mengembalikan titik puncak. flareAmt = kekuatan akar papan.</summary>
    static Vector3 Trunk(GeoBag b, int slot, Rng rg, int seed, float tH, float r0, float r1, Vector3 lean, float flareAmt, int roots, float s, int segs)
    {
        float[] fy = { -0.12f, 0f, 0.1f, 0.24f, 0.42f, 0.66f, 0.85f, 1f };
        var pts = new List<Vector3>(); var rad = new List<float>(); var fl = new List<float>();
        float wob = rg.R(0.04f, 0.12f) * s, wph = rg.F() * 6.28f;
        for (int i = 0; i < fy.Length; i++)
        {
            float t = Mathf.Max(0f, fy[i]);
            float y = fy[i] * tH;
            Vector3 p = V(0f, y, 0f) + lean * (t * t) + V(Mathf.Sin(t * 5.2f + wph) * wob * t, 0f, Mathf.Cos(t * 4.1f + wph) * wob * t);
            pts.Add(p);
            rad.Add(Mathf.Lerp(r0, r1, Mathf.Pow(t, 0.8f)));
            fl.Add(flareAmt * Mathf.Exp(-Mathf.Max(0f, y) / (0.5f * s)));
        }
        float ph = rg.F() * 6.28f;
        Tube(b, slot, pts, rad, segs, seed, 0.06f, fl, roots, ph, false);
        // akar menjalar di tanah searah tonjolan (tonjolan ring ada di sudut a = (2*pi*k - ph) / roots, ring memakai x = sin(a), z = cos(a))
        if (flareAmt > 0.05f)
            for (int k = 0; k < roots; k++)
            {
                float a = (2f * Mathf.PI * k - ph) / roots + rg.R(-0.12f, 0.12f);
                Vector3 dir = Flat(a);
                var rp = new List<Vector3> { dir * r0 * 0.7f + V(0f, 0.55f * s, 0f), dir * r0 * 1.45f + V(0f, 0.2f * s, 0f), dir * r0 * (2.0f + rg.R(0f, 0.6f)) + V(0f, 0.03f * s, 0f), dir * r0 * (2.6f + rg.R(0f, 0.8f)) + V(0f, -0.12f * s, 0f) };
                Tube(b, slot, rp, new List<float> { r0 * 0.5f, r0 * 0.36f, r0 * 0.22f, r0 * 0.12f }, 7, seed + k, 0.08f, null, 0, 0f, true, 0.5f);
            }
        return pts[pts.Count - 1];
    }

    // ------------------------------------------------------------------ pohon berdaun lebar
    sealed class Style
    {
        public float trunkH = 2.1f, trunkR = 0.56f, lean = 0.35f, flare = 0.7f; public int roots = 5;
        public int limbs = 4; public float limbLen = 3.0f, limbElev = 52f, limbUp = 0.9f, limbGrav = 0f;
        public int sec = 2; public float secLen = 0.6f;
        public float puffR = 1.3f, squash = 0.84f, lump = 0.16f, flat = 0.4f; public int upper = 5;
        public int sat = 6; public float satR = 0.45f;
        public float mix = 0.5f, accent = 0.34f;      // aksen hangat hanya untuk gumpalan SATELIT kecil di sisi terang (jumbai), bukan lempengan besar
        public int twigs = 3;
        public int hang = 0; public float hangDrop = 1.0f;
        public int fruit = 0; public float fruitR = 0.07f;
        public bool petals;
        public int rings = 6, cols = 12;
    }

    sealed class Site { public Vector3 p; public float r; public bool top; }

    static void Broadleaf(GeoBag b, int seed, Pal pal, Style st, float s)
    {
        var rg = new Rng(seed);
        float tH = st.trunkH * s * rg.R(0.92f, 1.1f);
        float r0 = st.trunkR * s * rg.R(0.92f, 1.08f), r1 = r0 * 0.58f;
        float la = rg.F() * 6.2831f;
        Vector3 lean = Flat(la) * st.lean * s * rg.R(0.3f, 1f);
        Vector3 F = Trunk(b, pal.bark, rg, seed, tH, r0, r1, lean, st.flare, st.roots, s, 12);

        var sites = new List<Site>();
        float a0 = rg.F() * 360f;
        for (int k = 0; k < st.limbs; k++)
        {
            float az = (a0 + k * 360f / st.limbs + rg.R(-20f, 20f)) * Mathf.Deg2Rad;
            float el = (st.limbElev + rg.R(-12f, 10f)) * Mathf.Deg2Rad;
            Vector3 dir = V(Mathf.Sin(az) * Mathf.Sin(el), Mathf.Cos(el), Mathf.Cos(az) * Mathf.Sin(el));
            float len = st.limbLen * s * rg.R(0.85f, 1.15f);
            Vector3 start = F - V(0f, 0.3f * s, 0f);
            var path = Path(start, dir, len, 5, st.limbUp - st.limbGrav, 0.35f, rg);
            float rl0 = r1 * rg.R(0.8f, 0.95f);
            Branch(b, pal.bark, path, rl0, rl0 * 0.26f, 7, seed + k * 13);
            sites.Add(new Site { p = path[path.Count - 1], r = st.puffR * s * rg.R(0.9f, 1.15f) });
            for (int m = 0; m < st.sec; m++)
            {
                int idx = 2 + m % 2;
                Vector3 bp = path[idx];
                Vector3 tg = (path[idx + 1 > path.Count - 1 ? path.Count - 1 : idx + 1] - path[idx - 1]).normalized;
                float rot = (m % 2 == 0 ? 1f : -1f) * rg.R(28f, 62f) * Mathf.Deg2Rad;
                Vector3 hd = V(tg.x, 0f, tg.z);
                if (hd.sqrMagnitude < 1e-4f) hd = Flat(az);
                hd.Normalize();
                Vector3 d2 = (RotY(hd, rot * Mathf.Rad2Deg) + V(0f, rg.R(0.45f, 0.9f), 0f)).normalized;
                float l2 = st.secLen * len * rg.R(0.7f, 1.05f) * (1f - idx * 0.12f);
                var p2 = Path(bp, d2, l2, 4, st.limbUp * 0.8f - st.limbGrav, 0.3f, rg);
                float rr = rl0 * Mathf.Lerp(1f, 0.26f, idx / 4f) * 0.7f;
                Branch(b, pal.bark, p2, rr, rr * 0.28f, 6, seed + k * 31 + m * 7);
                sites.Add(new Site { p = p2[p2.Count - 1], r = st.puffR * s * rg.R(0.8f, 1.1f) });
            }
        }
        // lapis atas (di sela dahan), puncak, dan inti mahkota (menutup lubang tengah) -> mahkota menumpuk tinggi seperti awan, bukan payung pipih
        float up = st.limbLen * s;
        float reach = up * Mathf.Sin(st.limbElev * Mathf.Deg2Rad) * 0.9f;
        for (int k = 0; k < st.upper; k++)
        {
            float az = (a0 + (k + 0.5f) * 360f / st.upper + rg.R(-14f, 14f)) * Mathf.Deg2Rad;
            sites.Add(new Site { p = F + Flat(az) * reach * rg.R(0.38f, 0.6f) + V(0f, up * rg.R(0.8f, 1.05f), 0f), r = st.puffR * s * rg.R(0.85f, 1.05f) });
        }
        sites.Add(new Site { p = F + V(lean.x * 0.2f, up * 1.3f, lean.z * 0.2f), r = st.puffR * s * 1.15f, top = true });
        sites.Add(new Site { p = F + V(0f, up * 0.5f, 0f), r = st.puffR * s * 1.05f });

        // daftar gumpalan
        var P = new List<Site>();
        foreach (var si in sites) P.Add(new Site { p = si.p + V(0f, si.r * 0.28f, 0f), r = si.r, top = si.top });
        int n0 = P.Count;
        for (int k = 0; k < st.sat; k++)
        {
            var host = P[rg.I(n0)];
            float az = rg.F() * 6.28f;
            Vector3 d = V(Mathf.Sin(az), rg.R(-0.2f, 0.7f), Mathf.Cos(az)).normalized;
            P.Add(new Site { p = host.p + d * host.r * 0.85f, r = host.r * st.satR * rg.R(0.8f, 1.2f) });
        }
        // pusat dan jari-jari mahkota (untuk campuran normal & pewarnaan)
        Vector3 C = Vector3.zero; float wsum = 0f;
        foreach (var q in P) { float w = q.r * q.r; C += q.p * w; wsum += w; }
        C /= wsum;
        Vector3 R = V(0.1f, 0.1f, 0.1f);
        foreach (var q in P)
        {
            R.x = Mathf.Max(R.x, Mathf.Abs(q.p.x - C.x) + q.r);
            R.y = Mathf.Max(R.y, Mathf.Abs(q.p.y - C.y) + q.r * st.squash);
            R.z = Mathf.Max(R.z, Mathf.Abs(q.p.z - C.z) + q.r);
        }
        for (int qi = 0; qi < P.Count; qi++)
        {
            var q = P[qi];
            Vector3 qn = V((q.p.x - C.x) / R.x, (q.p.y - C.y) / R.y, (q.p.z - C.z) / R.z);
            float L = qn.y * 0.85f + (rg.F() - 0.5f) * 0.55f - Mathf.Sqrt(qn.x * qn.x + qn.z * qn.z) * 0.1f;
            int slot = L > 0.24f ? pal.light : (L < -0.32f ? pal.dark : pal.mid);
            if (slot < 0) slot = rg.F() < 0.5f ? pal.light : pal.dark;
            // nada hangat (kuning-hijau / oranye) hanya pada gumpalan satelit di sisi terang -> jumbai kecil di tepi tajuk
            if (qi >= n0 && L > 0f && pal.accent >= 0 && rg.F() < st.accent) slot = pal.accent;
            Puff(b, slot, q.p, V(q.r, q.r * st.squash, q.r), seed * 5 + qi * 11, st.lump, st.flat, C, R, st.mix, st.rings, st.cols);
        }
        // gumpalan menggantung di tepi bawah (sakura / pohon terkulai)
        for (int k = 0; k < st.hang; k++)
        {
            var host = P[rg.I(n0)];
            float az = rg.F() * 6.28f;
            Vector3 d = Flat(az);
            Vector3 hp = host.p + d * host.r * 0.8f + V(0f, -host.r * st.hangDrop * rg.R(0.5f, 1f), 0f);
            float hr = host.r * rg.R(0.22f, 0.32f);
            Puff(b, pal.dark >= 0 ? pal.dark : pal.mid, hp, V(hr, hr * 1.15f, hr), seed * 3 + k * 17, 0.14f, 0f, C, R, st.mix, 4, 8);
        }
        // ranting kecil yang menyembul dari tepi mahkota
        for (int k = 0; k < st.twigs; k++)
        {
            var host = P[rg.I(n0)];
            float az = rg.F() * 6.28f;
            Vector3 d = V(Mathf.Sin(az), rg.R(0.1f, 0.8f), Mathf.Cos(az)).normalized;
            Vector3 a = host.p + d * host.r * 0.55f, c2 = host.p + d * host.r * 1.2f + V(0f, 0.2f * s, 0f);
            var tp = new List<Vector3> { a, (a + c2) * 0.5f + V(0f, 0.08f * s, 0f), c2 };
            Tube(b, pal.bark, tp, new List<float> { 0.05f * s, 0.032f * s, 0.012f * s }, 5, seed + k, 0.05f, null, 0, 0f, true, 1f);
        }
        // buah / bunga
        if (st.fruit > 0 && pal.fruit >= 0)
            for (int k = 0; k < st.fruit; k++)
            {
                var host = P[rg.I(n0)];
                float az = rg.F() * 6.28f;
                Vector3 d = V(Mathf.Sin(az), rg.R(-0.35f, 0.6f), Mathf.Cos(az)).normalized;
                b.Ball(pal.fruit, host.p + V(d.x * host.r, d.y * host.r * st.squash, d.z * host.r) * 0.98f, st.fruitR * s, 3, 6);
            }
        if (st.petals)
            for (int k = 0; k < 16; k++)
            {
                float a = rg.F() * 6.28f, rr = rg.R(0.9f, 3.6f) * s;
                Vector3 p = V(Mathf.Sin(a) * rr, 0.04f, Mathf.Cos(a) * rr);
                float sz = 0.16f;
                b.QuadBoth(pal.mid >= 0 ? pal.mid : pal.light, p, p + V(sz, 0f, 0f), p + V(sz * 0.9f, 0.02f, sz), p + V(0f, 0.02f, sz * 0.9f));
            }
    }

    // ------------------------------------------------------------------ varian berdaun lebar
    /// <summary>Ek / pohon lebar. v = 0..3 memberi bentuk berbeda (bulat, lebar-rendah, tinggi, miring).</summary>
    public static void Oak(GeoBag b, int seed, Pal pal, float s, int v = 0)
    {
        var st = new Style();
        switch (v & 3)
        {
            case 1: st.trunkH = 1.8f; st.limbs = 5; st.limbLen = 3.3f; st.limbElev = 62f; st.puffR = 1.35f; st.squash = 0.74f; st.lean = 0.25f; st.upper = 6; break;
            case 2: st.trunkH = 2.5f; st.limbs = 4; st.limbLen = 2.8f; st.limbElev = 38f; st.puffR = 1.25f; st.squash = 0.95f; st.limbUp = 1.2f; st.upper = 4; break;
            case 3: st.trunkH = 2.0f; st.limbs = 3; st.limbLen = 3.1f; st.limbElev = 58f; st.lean = 0.9f; st.puffR = 1.3f; st.sat = 8; st.upper = 4; break;
        }
        Broadleaf(b, seed, pal, st, s);
    }

    public static void Sakura(GeoBag b, int seed, Pal pal, float s, int v = 0)
    {
        var st = new Style { trunkH = 1.9f, trunkR = 0.4f, lean = 0.6f, flare = 0.5f, limbs = 4, limbLen = 3.2f, limbElev = 66f, limbUp = 0.45f, puffR = 1.4f, squash = 0.68f, lump = 0.17f, flat = 0.3f, mix = 0.45f, sat = 8, hang = 5, hangDrop = 0.7f, petals = true, accent = 0.0f, twigs = 4 };
        if ((v & 1) == 1) { st.limbs = 5; st.limbElev = 72f; st.trunkH = 1.6f; }
        Broadleaf(b, seed, pal, st, s);
    }

    public static void Apple(GeoBag b, int seed, Pal pal, float s, int v = 0)
    {
        var st = new Style { trunkH = 1.5f, trunkR = 0.32f, lean = 0.4f, flare = 0.5f, roots = 4, limbs = 4, limbLen = 2.1f, limbElev = 60f, limbUp = 0.7f, puffR = 1.05f, squash = 0.84f, sat = 5, fruit = 11, fruitR = 0.085f, twigs = 2, secLen = 0.5f };
        if ((v & 1) == 1) { st.limbs = 3; st.limbLen = 2.3f; st.trunkH = 1.3f; st.lean = 0.7f; }
        Broadleaf(b, seed, pal, st, s);
    }

    public static void Maple(GeoBag b, int seed, Pal pal, float s, int v = 0)
    {
        var st = new Style { trunkH = 2.9f, trunkR = 0.4f, lean = 0.3f, limbs = 4, limbLen = 2.9f, limbElev = 36f, limbUp = 1.1f, puffR = 1.4f, squash = 0.95f, sat = 7, twigs = 3 };
        if ((v & 1) == 1) { st.limbs = 5; st.limbElev = 44f; st.trunkH = 2.4f; }
        Broadleaf(b, seed, pal, st, s);
    }

    // ------------------------------------------------------------------ birch
    public static void Birch(GeoBag b, int seed, Pal pal, float s)
    {
        var rg = new Rng(seed);
        float H = rg.R(5.6f, 7.0f) * s;
        float lean = rg.R(0.1f, 0.6f) * s, la = rg.F() * 6.28f;
        Vector3 lv = Flat(la) * lean;
        var pts = new List<Vector3>(); var rad = new List<float>(); var fl = new List<float>();
        float[] fy = { -0.05f, 0f, 0.1f, 0.28f, 0.5f, 0.72f, 0.9f, 1f };
        float ph = rg.F() * 6.28f;
        for (int i = 0; i < fy.Length; i++)
        {
            float t = Mathf.Max(0f, fy[i]);
            pts.Add(V(0f, fy[i] * H, 0f) + lv * (t * t) + V(Mathf.Sin(t * 6f + ph) * 0.12f * s * t, 0f, Mathf.Cos(t * 5f + ph) * 0.1f * s * t));
            rad.Add(Mathf.Lerp(0.17f * s, 0.045f * s, Mathf.Pow(t, 0.9f)));
            fl.Add(0.5f * Mathf.Exp(-Mathf.Max(0f, fy[i] * H) / (0.35f * s)));
        }
        Tube(b, pal.bark, pts, rad, 8, seed, 0.05f, fl, 3, ph, true, 1.5f);
        var sites = new List<Site>();
        Vector3 top = pts[pts.Count - 1];
        float cy = H * 0.74f;                          // pusat mahkota
        Vector3 cc0 = V(top.x * 0.9f, cy, top.z * 0.9f);
        float rx = 1.55f * s, ry = 2.15f * s;
        int np = 9;
        for (int k = 0; k < np; k++)
        {
            float t = (k + 0.5f) / np;
            float yy = 1f - 2f * t;                    // -1 .. 1 (spiral Fibonacci di permukaan elipsoid)
            float ph2 = k * 2.39996f + rg.F();
            float rr = Mathf.Sqrt(Mathf.Max(0f, 1f - yy * yy));
            Vector3 q = V(Mathf.Cos(ph2) * rr * rx * 0.85f, yy * ry * 0.85f, Mathf.Sin(ph2) * rr * rx * 0.85f);
            if (k == np - 1) q = V(0f, ry * 0.95f, 0f);
            sites.Add(new Site { p = cc0 + q, r = rg.R(0.8f, 1.12f) * s });
        }
        for (int k = 0; k < sites.Count; k++)          // dahan tipis menuju tiap gumpalan
        {
            var q = sites[k];
            float by = Mathf.Clamp(q.p.y - 1.1f * s, H * 0.42f, H * 0.9f);
            int bi = 0; for (int i = 1; i < pts.Count; i++) if (pts[i].y <= by) bi = i;
            Vector3 bp = pts[bi];
            Vector3 dd = q.p - bp;
            if (dd.magnitude < 0.4f * s) continue;
            var pp = Path(bp, dd.normalized, dd.magnitude * 0.9f, 4, 0.2f, 0.25f, rg);
            Branch(b, pal.bark, pp, 0.05f * s, 0.018f * s, 5, seed + k);
        }
        Vector3 C = cc0, R = V(rx + 0.9f * s, ry + 0.9f * s, rx + 0.9f * s);
        int idxp = 0;
        foreach (var q in sites)
        {
            float L = (q.p.y - C.y) / R.y + (rg.F() - 0.5f) * 0.5f;
            int slot = L > 0.12f ? pal.light : (pal.mid >= 0 ? pal.mid : pal.dark);
            if (L < -0.3f) slot = pal.dark;
            // sebagian gumpalan terang diganti nada tengah supaya puncak tidak seluruhnya kuning
            else if (slot == pal.light && pal.mid >= 0 && rg.F() < 0.45f) slot = pal.mid;
            Puff(b, slot, q.p, V(q.r, q.r * 0.9f, q.r), seed * 3 + idxp++ * 5, 0.17f, 0.25f, C, R, 0.4f, 5, 10);
        }
    }

    // ------------------------------------------------------------------ cemara bertingkat
    /// <summary>Satu tingkat rok cemara: tepi bergerigi yang menjuntai, naik ke ujung. Mengembalikan y puncak.</summary>
    static void Skirt(GeoBag b, int slotLow, int slotHigh, Vector3 bas, float R, float hgt, int lobes, float drop, float rot, int seed, float topR)
    {
        int cols = lobes * 4;
        float[] fr = { 0f, 0.26f, 0.62f, 1f };
        float[] rr = { 1f, 0.8f, 0.46f, topR };
        int rows = 4;
        var P = new Vector3[rows, cols];
        var vRow = new float[rows];
        float slant = Mathf.Sqrt(R * R + hgt * hgt);
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                float a = 2f * Mathf.PI * j / cols;
                float lobe = Mathf.Pow(Mathf.Abs(Mathf.Cos(lobes * 0.5f * a + rot)), 1.4f);
                float nz = (N3(Mathf.Cos(a) * 1.5f + seed, i * 1.7f, Mathf.Sin(a) * 1.5f, seed) - 0.5f) * 0.16f;
                float rad = R * rr[i] * (i == 0 ? (0.74f + 0.26f * lobe) : (i == 1 ? (0.93f + 0.07f * lobe) : 1f)) * (1f + nz);
                float y = bas.y + hgt * fr[i] - (i == 0 ? drop * lobe : (i == 1 ? drop * 0.3f * lobe : 0f));
                P[i, j] = V(bas.x + Mathf.Sin(a) * rad, y, bas.z + Mathf.Cos(a) * rad);
            }
            vRow[i] = slant * fr[i];
        }
        if (slotHigh < 0 || slotHigh == slotLow) { Surf(b, slotLow, P, 2f * Mathf.PI * R * 0.7f, vRow, Vector3.zero, Vector3.up, null); return; }
        // dua nada: pinggir bawah gelap, bagian atas lebih terang
        var P1 = new Vector3[3, cols]; var P2 = new Vector3[3, cols];
        var v1 = new float[3]; var v2 = new float[3];
        for (int j = 0; j < cols; j++) { for (int i = 0; i < 3; i++) P1[i, j] = P[i, j]; for (int i = 0; i < 3; i++) P2[i, j] = P[i + 1, j]; }
        for (int i = 0; i < 3; i++) { v1[i] = vRow[i]; v2[i] = vRow[i + 1]; }
        Surf(b, slotLow, P1, 2f * Mathf.PI * R * 0.7f, v1, Vector3.zero, Vector3.zero, null);
        Surf(b, slotHigh, P2, 2f * Mathf.PI * R * 0.5f, v2, Vector3.zero, Vector3.up, null);
    }

    /// <summary>Cemara: H = tinggi total (m), Rb = jari-jari tingkat terbawah. Tingkat bergerigi, sedikit miring, ujung runcing.</summary>
    public static void Spruce(GeoBag b, int seed, Pal pal, float H, float Rb, int tiers)
    {
        var rg = new Rng(seed);
        float tr0 = 0.065f * H + 0.1f;
        var pts = new List<Vector3>(); var rad = new List<float>(); var fl = new List<float>();
        float[] fy = { -0.04f, 0f, 0.08f, 0.2f, 0.4f, 0.62f, 0.78f, 0.88f };
        float ph = rg.F() * 6.28f;
        for (int i = 0; i < fy.Length; i++)
        {
            pts.Add(V(Mathf.Sin(fy[i] * 4f + ph) * 0.04f * H * fy[i], fy[i] * H, 0f));
            rad.Add(Mathf.Lerp(tr0, 0.03f, Mathf.Pow(Mathf.Max(0f, fy[i]), 0.85f)));
            fl.Add(0.55f * Mathf.Exp(-Mathf.Max(0f, fy[i] * H) / (0.4f * H / 9f + 0.15f)));
        }
        Tube(b, pal.bark, pts, rad, 8, seed, 0.05f, fl, 4, ph, true, 1.2f);
        float y0 = H * 0.13f;
        float yTop = H * 0.97f;
        for (int i = 0; i < tiers; i++)
        {
            float f = i / (float)(tiers - 1);
            float y = Mathf.Lerp(y0, H * 0.74f, Mathf.Pow(f, 0.92f));
            float R = Rb * Mathf.Pow(1f - 0.9f * f, 0.95f) * rg.R(0.94f, 1.06f);
            float hg = Mathf.Min(yTop - y, H * (0.22f - 0.06f * f)) ;
            if (i == tiers - 1) hg = yTop - y;
            int lobes = 6 + (i % 2);
            int hi = pal.mid >= 0 ? pal.mid : pal.light;
            Skirt(b, pal.dark, hi, V(rg.R(-0.05f, 0.05f) * R, y, rg.R(-0.05f, 0.05f) * R), R, hg, lobes, hg * 0.34f, rg.F() * 6.28f, seed + i * 3, i == tiers - 1 ? 0f : 0.07f);
        }
    }

    // ------------------------------------------------------------------ sipres (kolom gumpalan bertumpuk)
    public static void Cypress(GeoBag b, int seed, Pal pal, float H, float R0)
    {
        var rg = new Rng(seed);
        var tp = new List<Vector3> { V(0f, -0.2f, 0f), V(0f, 0.35f, 0f), V(0f, 1.3f, 0f) };
        Tube(b, pal.bark, tp, new List<float> { 0.22f * R0 + 0.14f, 0.16f * R0 + 0.1f, 0.1f * R0 + 0.07f }, 7, seed, 0.05f, null, 0, 0f, true, 0.8f);
        int n = 8;
        Vector3 C = V(0f, H * 0.5f, 0f), R = V(R0, H * 0.5f, R0);
        float lean = rg.R(-0.04f, 0.04f) * H, la = rg.F() * 6.28f;
        for (int i = 0; i < n; i++)
        {
            float t = (i + 0.5f) / n;
            float prof = Mathf.Pow(Mathf.Sin(Mathf.PI * (0.08f + 0.84f * t)), 0.75f) * (1f - 0.3f * t * t);
            float r = Mathf.Max(0.22f * R0, R0 * prof) * rg.R(0.92f, 1.06f);
            float y = Mathf.Lerp(0.9f, H - 0.7f, t);
            float az = i * 2.4f + la;
            Vector3 c = V(Mathf.Sin(az) * 0.09f * R0 + Mathf.Sin(la) * lean * t * t, y, Mathf.Cos(az) * 0.09f * R0 + Mathf.Cos(la) * lean * t * t);
            int slot = i < 3 ? pal.dark : (i == n - 1 ? (pal.light >= 0 ? pal.light : pal.mid) : (pal.mid >= 0 ? pal.mid : pal.dark));
            if (i % 3 == 2 && i > 2 && i < n - 1) slot = pal.dark;
            float ry = H / n * (i == n - 1 ? 1.55f : 0.98f);
            Puff(b, slot, c, V(r, ry, r), seed * 5 + i * 3, 0.13f, 0f, C, R, 0.45f, 5, 10);
        }
    }

    // ------------------------------------------------------------------ semak
    public static void Bush(GeoBag b, int seed, Pal pal, float s, int flowers)
    {
        var rg = new Rng(seed);
        int n = 5;
        var P = new List<Site>();
        for (int k = 0; k < n; k++)
        {
            float az = (k * 72f + rg.R(-20f, 20f)) * Mathf.Deg2Rad;
            float rr = k == 0 ? 0f : rg.R(0.45f, 0.8f) * s;
            P.Add(new Site { p = V(Mathf.Sin(az) * rr, (k == 0 ? 0.62f : rg.R(0.42f, 0.55f)) * s, Mathf.Cos(az) * rr), r = (k == 0 ? 0.85f : rg.R(0.5f, 0.7f)) * s });
        }
        Vector3 C = V(0f, 0.55f * s, 0f), R = V(1.2f * s, 0.8f * s, 1.2f * s);
        for (int k = 0; k < n; k++)
        {
            float L = (P[k].p.y - C.y) / R.y + (rg.F() - 0.5f) * 0.5f;
            int slot = (k == 0 || L > 0.0f) ? pal.light : (pal.mid >= 0 ? pal.mid : pal.dark);
            if (k % 3 == 2) slot = pal.dark;
            Puff(b, slot, P[k].p, V(P[k].r, P[k].r * 0.78f, P[k].r), seed * 3 + k * 7, 0.2f, 0.45f, C, R, 0.4f, 4, 10);
        }
        for (int k = 0; k < flowers; k++)
        {
            float az = rg.F() * 6.28f;
            Vector3 d = V(Mathf.Sin(az), rg.R(0.1f, 0.9f), Mathf.Cos(az)).normalized;
            b.Ball(pal.accent >= 0 ? pal.accent : pal.light, V(d.x * 1.0f * s, (0.45f + d.y * 0.6f) * s, d.z * 1.0f * s), 0.075f * s, 3, 5);
        }
    }

    // ------------------------------------------------------------------ pohon mati
    public static void Dead(GeoBag b, int seed, Pal pal, float s)
    {
        var rg = new Rng(seed);
        float H = rg.R(4.2f, 5.6f) * s;
        float r0 = 0.42f * s;
        float ph = rg.F() * 6.28f;
        var pts = new List<Vector3>(); var rad = new List<float>(); var fl = new List<float>();
        float[] fy = { -0.06f, 0f, 0.1f, 0.25f, 0.45f, 0.65f, 0.82f, 1f };
        for (int i = 0; i < fy.Length; i++)
        {
            float t = Mathf.Max(0f, fy[i]);
            pts.Add(V(Mathf.Sin(t * 4.4f + ph) * 0.4f * s * t, fy[i] * H, Mathf.Cos(t * 3.3f + ph) * 0.34f * s * t));
            rad.Add(Mathf.Lerp(r0, r0 * 0.24f, Mathf.Pow(t, 0.75f)));
            fl.Add(0.9f * Mathf.Exp(-Mathf.Max(0f, fy[i] * H) / (0.5f * s)));
        }
        Tube(b, pal.bark, pts, rad, 8, seed, 0.1f, fl, 5, ph, true, 1.0f);
        int nb = 6;
        for (int k = 0; k < nb; k++)
        {
            int idx = 2 + (k % 4);
            Vector3 bp = pts[idx];
            float az = (k * 360f / nb + rg.R(-30f, 30f)) * Mathf.Deg2Rad;
            Vector3 d = V(Mathf.Sin(az) * 0.9f, rg.R(0.25f, 0.9f), Mathf.Cos(az) * 0.9f);
            float len = rg.R(1.4f, 2.6f) * s * (1f - idx * 0.07f);
            var pp = Path(bp, d, len, 5, rg.R(-0.3f, 0.5f), 0.7f, rg);
            float rb = rad[idx] * 0.55f;
            Branch(b, pal.bark, pp, rb, rb * 0.15f, 6, seed + k);
            for (int m = 0; m < 2; m++)
            {
                Vector3 sp = pp[2 + m];
                Vector3 d2 = (pp[pp.Count - 1] - pp[0]).normalized;
                d2 = (RotY(d2, (m == 0 ? 1f : -1f) * rg.R(30f, 60f)) + V(0f, rg.R(0.2f, 0.7f), 0f)).normalized;
                var p2 = Path(sp, d2, len * rg.R(0.3f, 0.55f), 4, rg.R(-0.2f, 0.4f), 0.8f, rg);
                Branch(b, pal.bark, p2, rb * 0.5f, rb * 0.08f, 5, seed + k * 9 + m);
            }
        }
    }

    // ------------------------------------------------------------------ pohon raksasa (kuno)
    public static void Ancient(GeoBag b, int seed, Pal pal, float s)
    {
        var st = new Style { trunkH = 3.4f, trunkR = 1.5f, lean = 0.5f, flare = 1.1f, roots = 6, limbs = 6, limbLen = 5.4f, limbElev = 50f, limbUp = 1.0f, sec = 3, secLen = 0.55f, puffR = 2.7f, squash = 0.8f, sat = 12, twigs = 6, rings = 5, cols = 12 };
        Broadleaf(b, seed, pal, st, s);
    }
}
