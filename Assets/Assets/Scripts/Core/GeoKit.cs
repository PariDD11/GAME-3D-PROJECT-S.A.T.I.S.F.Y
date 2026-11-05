using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Alat geometri untuk Town Builder: semua bangunan dirakit sebagai SATU mesh bertekstur (UV dalam meter, jadi
/// tekstur batu/plester/genteng tidak melar di dinding besar). Slot (Sl.*) dipetakan ke material saat mesh dikeluarkan.
/// Dipakai oleh TownSceneBuilder dan HuntSceneBuilder (replika gerbang luar harus identik).
/// </summary>
public static class Sl
{
    public const int Plaster = 0, Stone = 1, Wood = 2, Roof = 3, Glass = 4, Metal = 5, Gold = 6, Cloth = 7, Thatch = 8, Marble = 9,
                     DarkStone = 10, Glow = 11, Cobble = 12, Dirt = 13, Grass = 14, Field = 15, Leaf = 16, RoofB = 17, PlasterB = 18,
                     ClothB = 19, WoodLight = 20, Water = 21, Pave = 22, FieldB = 23, Blossom = 24, Hedge = 25,
                     SkinS = 26, Hair = 27, Eye = 28, Fur = 29, CoatA = 30, CoatB = 31, CoatC = 32, Leather = 33, EyeW = 34, Dark = 35, Lip = 36, Count = 37;
}

public class GeoBag
{
    /// <summary>Diisi oleh kode Editor (GeoIO) supaya mesh bisa disimpan sebagai asset; null saat runtime/build.</summary>
    public static System.Func<Mesh, string, Mesh> MeshSaver;

    class Ch
    {
        public List<Vector3> v = new List<Vector3>();
        public List<Vector3> n = new List<Vector3>();
        public List<Vector2> uv = new List<Vector2>();
        public List<int> t = new List<int>();
    }

    readonly Ch[] ch = new Ch[Sl.Count];
    Matrix4x4 T = Matrix4x4.identity;
    readonly Stack<Matrix4x4> stack = new Stack<Matrix4x4>();

    public int VertexCount
    {
        get { int c = 0; foreach (var x in ch) if (x != null) c += x.v.Count; return c; }
    }

    public void Push(Vector3 pos, float yaw)
    {
        stack.Push(T);
        T = T * Matrix4x4.TRS(pos, Quaternion.Euler(0f, yaw, 0f), Vector3.one);
    }

    public void Pop() { T = stack.Pop(); }

    /// <summary>Push dengan rotasi penuh + skala seragam (mis. memasang model senjata terbalik di tangan NPC).</summary>
    public void PushTRS(Vector3 pos, Quaternion rot, float scale)
    {
        stack.Push(T);
        T = T * Matrix4x4.TRS(pos, rot, Vector3.one * scale);
    }

    Ch C(int s)
    {
        if (ch[s] == null) ch[s] = new Ch();
        return ch[s];
    }

    int AddV(int s, Vector3 p, Vector3 n, Vector2 uv)
    {
        Ch c = C(s);
        c.v.Add(T.MultiplyPoint3x4(p));
        c.n.Add(T.MultiplyVector(n).normalized);
        c.uv.Add(uv);
        return c.v.Count - 1;
    }

    void AddT(int s, int a, int b, int c) { var l = C(s).t; l.Add(a); l.Add(b); l.Add(c); }

    // ------------------------------------------------------------------ bidang dasar
    /// <summary>Segiempat planar. Urutan titik: a-b-c-d, dan cross(b-a, c-a) adalah arah permukaan depan. a->b = arah U (mendatar).</summary>
    public void Quad(int s, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        Vector3 n = Vector3.Cross(b - a, c - a);
        if (n.sqrMagnitude < 1e-12f) n = Vector3.Cross(c - a, d - a);
        n = n.normalized;
        Vector3 e1 = (b - a).normalized;
        Vector3 w = d - a;
        Vector3 e2 = (w - e1 * Vector3.Dot(w, e1)).normalized;
        int i0 = AddV(s, a, n, UVOf(a, a, e1, e2));
        int i1 = AddV(s, b, n, UVOf(b, a, e1, e2));
        int i2 = AddV(s, c, n, UVOf(c, a, e1, e2));
        int i3 = AddV(s, d, n, UVOf(d, a, e1, e2));
        AddT(s, i0, i1, i2);
        AddT(s, i0, i2, i3);
    }

    /// <summary>Segiempat tampak dari kedua sisi (kain, bendera, jeruji).</summary>
    public void QuadBoth(int s, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        Quad(s, a, b, c, d);
        Quad(s, d, c, b, a);
    }

    public void Tri(int s, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 n = Vector3.Cross(b - a, c - a).normalized;
        Vector3 e1 = (b - a).normalized;
        Vector3 w = c - a;
        Vector3 e2 = (w - e1 * Vector3.Dot(w, e1)).normalized;
        int i0 = AddV(s, a, n, UVOf(a, a, e1, e2));
        int i1 = AddV(s, b, n, UVOf(b, a, e1, e2));
        int i2 = AddV(s, c, n, UVOf(c, a, e1, e2));
        AddT(s, i0, i1, i2);
    }

    static Vector2 UVOf(Vector3 p, Vector3 o, Vector3 e1, Vector3 e2)
    {
        Vector3 q = p - o;
        return new Vector2(Vector3.Dot(q, e1), Vector3.Dot(q, e2));
    }

    // ------------------------------------------------------------------ kotak
    public void Box(int s, Vector3 center, Vector3 size, float yaw = 0f, bool top = true, bool bottom = false)
    {
        Push(center, yaw);
        float hx = size.x / 2f, hy = size.y / 2f, hz = size.z / 2f;
        Quad(s, new Vector3(-hx, -hy, hz), new Vector3(hx, -hy, hz), new Vector3(hx, hy, hz), new Vector3(-hx, hy, hz));        // +Z
        Quad(s, new Vector3(hx, -hy, -hz), new Vector3(-hx, -hy, -hz), new Vector3(-hx, hy, -hz), new Vector3(hx, hy, -hz));    // -Z
        Quad(s, new Vector3(hx, -hy, hz), new Vector3(hx, -hy, -hz), new Vector3(hx, hy, -hz), new Vector3(hx, hy, hz));        // +X
        Quad(s, new Vector3(-hx, -hy, -hz), new Vector3(-hx, -hy, hz), new Vector3(-hx, hy, hz), new Vector3(-hx, hy, -hz));    // -X
        if (top) Quad(s, new Vector3(-hx, hy, hz), new Vector3(hx, hy, hz), new Vector3(hx, hy, -hz), new Vector3(-hx, hy, -hz));
        if (bottom) Quad(s, new Vector3(-hx, -hy, -hz), new Vector3(hx, -hy, -hz), new Vector3(hx, -hy, hz), new Vector3(-hx, -hy, hz));
        Pop();
    }

    /// <summary>Kotak dengan alas di y = base (lebih praktis untuk dinding dan menara persegi).</summary>
    public void BoxB(int s, Vector3 baseCenter, Vector3 size, float yaw = 0f, bool top = true)
    {
        Box(s, baseCenter + new Vector3(0f, size.y / 2f, 0f), size, yaw, top, false);
    }

    /// <summary>Balok miring dari titik a ke b. tw = lebar (sepanjang "side"), th = tebal. hint = arah atas acuan (otomatis bila nol).</summary>
    public void Beam(int s, Vector3 a, Vector3 b, float tw, float th, Vector3 hint = default(Vector3))
    {
        Vector3 dir = b - a;
        float len = dir.magnitude;
        if (len < 1e-4f) return;
        dir /= len;
        if (hint.sqrMagnitude < 1e-6f) hint = Mathf.Abs(dir.y) > 0.95f ? Vector3.forward : Vector3.up;
        Vector3 side = Vector3.Cross(hint, dir).normalized;
        Vector3 up = Vector3.Cross(dir, side).normalized;
        Matrix4x4 m = Matrix4x4.identity;
        m.SetColumn(0, new Vector4(side.x, side.y, side.z, 0f));
        m.SetColumn(1, new Vector4(up.x, up.y, up.z, 0f));
        m.SetColumn(2, new Vector4(dir.x, dir.y, dir.z, 0f));
        m.SetColumn(3, new Vector4(a.x, a.y, a.z, 1f));
        stack.Push(T);
        T = T * m;
        Box(s, new Vector3(0f, 0f, len / 2f), new Vector3(tw, th, len), 0f, true, true);
        Pop();
    }

    // ------------------------------------------------------------------ silinder, kerucut, kubah
    public void Cyl(int s, Vector3 b, float r, float h, int n, float rTop = -1f, bool capTop = true, bool capBottom = false, int capSlot = -1)
    {
        if (rTop < 0f) rTop = r;
        if (capSlot < 0) capSlot = s;
        float slant = Mathf.Sqrt(h * h + (r - rTop) * (r - rTop));
        float ny = h > 0.0001f ? (r - rTop) / h : 0f;
        float rm = (r + rTop) / 2f;
        int prevB = -1, prevT = -1;
        float a0 = n == 4 ? Mathf.PI * 0.25f : 0f;           // limas 4 sisi: sudut sejajar sumbu agar pas di atas menara/dinding kotak
        for (int i = 0; i <= n; i++)
        {
            float a = (i % n) / (float)n * Mathf.PI * 2f + a0;
            float sn = Mathf.Sin(a), cs = Mathf.Cos(a);
            Vector3 nrm = new Vector3(sn, ny, cs);
            float u = i / (float)n * Mathf.PI * 2f * rm;
            int vb = AddV(s, b + new Vector3(sn * r, 0f, cs * r), nrm, new Vector2(u, 0f));
            int vt = AddV(s, b + new Vector3(sn * rTop, h, cs * rTop), nrm, new Vector2(u, slant));
            if (i > 0)
            {
                AddT(s, prevB, vb, vt);
                AddT(s, prevB, vt, prevT);
            }
            prevB = vb; prevT = vt;
        }
        if (capTop && rTop > 0.001f)
        {
            int c = AddV(capSlot, b + new Vector3(0f, h, 0f), Vector3.up, Vector2.zero);
            int first = -1, prev = -1;
            for (int i = 0; i <= n; i++)
            {
                float a = (i % n) / (float)n * Mathf.PI * 2f + a0;
                Vector2 q = new Vector2(Mathf.Sin(a) * rTop, Mathf.Cos(a) * rTop);
                int v = AddV(capSlot, b + new Vector3(q.x, h, q.y), Vector3.up, q);
                if (i == 0) first = v;
                if (i > 0) AddT(capSlot, c, prev, v);
                prev = v;
            }
            if (first < 0) first = 0;
        }
        if (capBottom)
        {
            int c = AddV(capSlot, b, Vector3.down, Vector2.zero);
            int prev = -1;
            for (int i = 0; i <= n; i++)
            {
                float a = (i % n) / (float)n * Mathf.PI * 2f + a0;
                Vector2 q = new Vector2(Mathf.Sin(a) * r, Mathf.Cos(a) * r);
                int v = AddV(capSlot, b + new Vector3(q.x, 0f, q.y), Vector3.down, q);
                if (i > 0) AddT(capSlot, c, v, prev);
                prev = v;
            }
        }
    }

    public void Cone(int s, Vector3 b, float r, float h, int n)
    {
        Cyl(s, b, r, h, n, 0f, false, false);
    }

    /// <summary>Kubah setengah bola (atau lebih rendah dengan squash &lt; 1).</summary>
    public void Dome(int s, Vector3 center, float r, int rings, int segs, float squash = 1f)
    {
        int[,] idx = new int[rings + 1, segs + 1];
        for (int i = 0; i <= rings; i++)
        {
            float lat = i / (float)rings * Mathf.PI * 0.5f;
            float y = Mathf.Sin(lat), rr = Mathf.Cos(lat);
            for (int j = 0; j <= segs; j++)
            {
                float a = (j % segs) / (float)segs * Mathf.PI * 2f;
                Vector3 nrm = new Vector3(Mathf.Sin(a) * rr, y, Mathf.Cos(a) * rr);
                Vector3 p = center + new Vector3(nrm.x * r, nrm.y * r * squash, nrm.z * r);
                idx[i, j] = AddV(s, p, nrm, new Vector2(j / (float)segs * Mathf.PI * 2f * r, lat * r));
            }
        }
        for (int i = 0; i < rings; i++)
            for (int j = 0; j < segs; j++)
            {
                AddT(s, idx[i, j], idx[i, j + 1], idx[i + 1, j + 1]);
                AddT(s, idx[i, j], idx[i + 1, j + 1], idx[i + 1, j]);
            }
    }

    /// <summary>Bola / elipsoid penuh (tajuk pohon, ornamen puncak).</summary>
    public void Ball(int s, Vector3 center, float r, int rings, int segs, float sy = 1f)
    {
        int[,] idx = new int[rings + 1, segs + 1];
        for (int i = 0; i <= rings; i++)
        {
            float lat = (i / (float)rings - 0.5f) * Mathf.PI;
            float y = Mathf.Sin(lat), rr = Mathf.Cos(lat);
            for (int j = 0; j <= segs; j++)
            {
                float a = (j % segs) / (float)segs * Mathf.PI * 2f;
                Vector3 nrm = new Vector3(Mathf.Sin(a) * rr, y, Mathf.Cos(a) * rr);
                Vector3 p = center + new Vector3(nrm.x * r, nrm.y * r * sy, nrm.z * r);
                idx[i, j] = AddV(s, p, nrm, new Vector2(j / (float)segs * Mathf.PI * 2f * r, (lat + Mathf.PI * 0.5f) * r));
            }
        }
        for (int i = 0; i < rings; i++)
            for (int j = 0; j < segs; j++)
            {
                AddT(s, idx[i, j], idx[i, j + 1], idx[i + 1, j + 1]);
                AddT(s, idx[i, j], idx[i + 1, j + 1], idx[i + 1, j]);
            }
    }

    // ------------------------------------------------------------------ atap
    /// <summary>
    /// Atap pelana tebal. (cx,by,cz) = titik tengah pada ketinggian tepi dinding; wx,wz = ukuran bangunan; ridgeAlongX = punggung atap sejajar sumbu X.
    /// Segitiga gable memakai endSlot, sisi bawah/ujung memakai woodSlot.
    /// </summary>
    public void Gable(int roofSlot, int endSlot, int woodSlot, Vector3 c, float wx, float wz, float rise, float ov, bool ridgeAlongX)
    {
        // Atap bergaya anime fantasi: lereng sedikit cekung dan ujung tepi (eave) terangkat, tepi bawah bergelombang bertumpuk.
        float span = ridgeAlongX ? wz : wx;
        float len = ridgeAlongX ? wx : wz;
        Push(c, ridgeAlongX ? 90f : 0f);
        float hw = span / 2f, X = hw + ov, Zh = len / 2f + ov;
        float slope = rise / hw;
        float yr = rise, t = 0.24f;
        float sag = Mathf.Min(0.5f, rise * 0.035f);
        float up = Mathf.Min(0.75f, 0.14f + ov * 0.32f);
        const int N = 9;
        // profil penampang (sisi +X): titik dari tepi (i=0) ke punggung (i=N)
        Vector3[] pr = new Vector3[N + 1];
        for (int i = 0; i <= N; i++)
        {
            float u = i / (float)N;                                   // 0 tepi .. 1 punggung
            float x = X * (1f - u);
            float yl = (x <= hw) ? (hw - x) * slope + 0f : -(x - hw) * slope;     // garis lurus ke bawah melewati dinding
            yl = yr - x * slope;
            float lift = up * Mathf.Pow(Mathf.Clamp01(1f - u * (X / (ov + 0.001f)) * 0.0f - u * 3.2f), 2.2f);   // terangkat hanya di ~30% dekat tepi
            float curve = -sag * Mathf.Sin(Mathf.Clamp01(x / hw) * Mathf.PI);
            pr[i] = new Vector3(x, yl + lift + curve, 0f);
        }
        float ye = pr[0].y;
        for (int i = 0; i < N; i++)
        {
            Vector3 a0 = pr[i], a1 = pr[i + 1];
            Vector3 L0 = new Vector3(-a0.x, a0.y, 0f), L1 = new Vector3(-a1.x, a1.y, 0f);
            // lereng atas
            Quad(roofSlot, new Vector3(-a0.x, a0.y, -Zh), new Vector3(-a0.x, a0.y, Zh), new Vector3(-a1.x, a1.y, Zh), new Vector3(-a1.x, a1.y, -Zh));
            Quad(roofSlot, new Vector3(a0.x, a0.y, Zh), new Vector3(a0.x, a0.y, -Zh), new Vector3(a1.x, a1.y, -Zh), new Vector3(a1.x, a1.y, Zh));
            // sisi bawah
            Quad(woodSlot, new Vector3(-a0.x, a0.y - t, Zh), new Vector3(-a0.x, a0.y - t, -Zh), new Vector3(-a1.x, a1.y - t, -Zh), new Vector3(-a1.x, a1.y - t, Zh));
            Quad(woodSlot, new Vector3(a0.x, a0.y - t, -Zh), new Vector3(a0.x, a0.y - t, Zh), new Vector3(a1.x, a1.y - t, Zh), new Vector3(a1.x, a1.y - t, -Zh));
            // papan tepi (bargeboard) ujung depan & belakang
            Quad(woodSlot, new Vector3(-a0.x, a0.y - t, Zh), new Vector3(-a1.x, a1.y - t, Zh), new Vector3(-a1.x, a1.y, Zh), new Vector3(-a0.x, a0.y, Zh));
            Quad(woodSlot, new Vector3(a1.x, a1.y - t, Zh), new Vector3(a0.x, a0.y - t, Zh), new Vector3(a0.x, a0.y, Zh), new Vector3(a1.x, a1.y, Zh));
            Quad(woodSlot, new Vector3(-a1.x, a1.y - t, -Zh), new Vector3(-a0.x, a0.y - t, -Zh), new Vector3(-a0.x, a0.y, -Zh), new Vector3(-a1.x, a1.y, -Zh));
            Quad(woodSlot, new Vector3(a0.x, a0.y - t, -Zh), new Vector3(a1.x, a1.y - t, -Zh), new Vector3(a1.x, a1.y, -Zh), new Vector3(a0.x, a0.y, -Zh));
        }
        // fascia tepi bawah (dua sisi)
        Quad(woodSlot, new Vector3(-X, ye - t, -Zh), new Vector3(-X, ye - t, Zh), new Vector3(-X, ye, Zh), new Vector3(-X, ye, -Zh));
        Quad(woodSlot, new Vector3(X, ye - t, Zh), new Vector3(X, ye - t, -Zh), new Vector3(X, ye, -Zh), new Vector3(X, ye, Zh));
        // deretan genteng bertumpuk di tepi (kesan sisik)
        int nk = Mathf.Max(3, Mathf.RoundToInt(len / 0.7f));
        for (int k = 0; k < nk; k++)
        {
            float z = -len / 2f + (k + 0.5f) * len / nk;
            Ball(roofSlot, new Vector3(X - 0.02f, ye + 0.02f, z), len / nk * 0.52f, 3, 6, 0.45f);
            Ball(roofSlot, new Vector3(-X + 0.02f, ye + 0.02f, z), len / nk * 0.52f, 3, 6, 0.45f);
        }
        // deretan genteng (course): balok tipis sepanjang punggung tiap ~0.55 m + sisik bergantian
        {
            float slopeLen = Mathf.Sqrt(X * X + yr * yr);
            int nc = Mathf.Clamp(Mathf.RoundToInt(slopeLen / 0.5f), 3, 26);
            for (int j = 1; j < nc; j++)
            {
                float u = Mathf.Lerp(0.04f, 0.96f, j / (float)nc);
                float fi = u * N; int i0 = Mathf.Min(N - 1, (int)fi); float ft = fi - i0;
                Vector3 pa = Vector3.Lerp(pr[i0], pr[i0 + 1], ft);
                Vector3 tg = (pr[i0 + 1] - pr[i0]); Vector2 nn = new Vector2(tg.y, -tg.x).normalized;
                float off = 0.035f;
                Vector3 pR = new Vector3(pa.x + nn.x * off, pa.y + nn.y * off, 0f);
                Vector3 pL = new Vector3(-pa.x - nn.x * off, pa.y + nn.y * off, 0f);
                float zz = Zh - 0.08f;
                Beam(roofSlot, new Vector3(pR.x, pR.y, -zz), new Vector3(pR.x, pR.y, zz), 0.3f, 0.08f, new Vector3(nn.x, nn.y, 0f));
                Beam(roofSlot, new Vector3(pL.x, pL.y, -zz), new Vector3(pL.x, pL.y, zz), 0.3f, 0.08f, new Vector3(-nn.x, nn.y, 0f));
                if (j <= 2)
                {
                    int ns = Mathf.Max(3, Mathf.RoundToInt(len / 0.7f));
                    for (int k = 0; k < ns; k++)
                    {
                        float z = -len / 2f + (k + 0.5f + (j % 2) * 0.5f) * len / ns;
                        if (Mathf.Abs(z) > Zh - 0.3f) continue;
                        Ball(roofSlot, new Vector3(pR.x, pR.y, z), len / ns * 0.5f, 3, 6, 0.4f);
                        Ball(roofSlot, new Vector3(pL.x, pL.y, z), len / ns * 0.5f, 3, 6, 0.4f);
                    }
                }
            }
            // genteng punggung (ridge) berjajar
            int nr = Mathf.Max(4, Mathf.RoundToInt(Zh * 2f / 0.55f));
            for (int k = 0; k < nr; k++)
                Ball(roofSlot, new Vector3(0f, yr + 0.22f, -Zh + (k + 0.5f) * 2f * Zh / nr), Zh / nr * 0.95f, 3, 6, 0.55f);
        }
        // segitiga gable di bidang dinding (mengikuti garis lurus pada hw)
        float zf = len / 2f;
        Tri(endSlot, new Vector3(-hw, 0f, zf), new Vector3(hw, 0f, zf), new Vector3(0f, rise, zf));
        Tri(endSlot, new Vector3(hw, 0f, -zf), new Vector3(-hw, 0f, -zf), new Vector3(0f, rise, -zf));
        // punggung atap: balok + hiasan ujung
        Box(roofSlot, new Vector3(0f, yr + 0.02f, 0f), new Vector3(0.46f, 0.32f, Zh * 2f + 0.1f));
        Box(woodSlot, new Vector3(0f, yr + 0.2f, 0f), new Vector3(0.2f, 0.12f, Zh * 2f + 0.3f));
        Pop();
    }

    /// <summary>Atap limas / perisai (ridge sepanjang sisi panjang). ov = tonjolan atap.</summary>
    public void Hip(int roofSlot, int woodSlot, Vector3 c, float wx, float wz, float rise, float ov)
    {
        // atap limas anime: lereng sedikit cekung, tepi terangkat, tepi bawah bersisik
        bool alongX = wx >= wz;
        float L = alongX ? wx : wz, S = alongX ? wz : wx;
        Push(c, alongX ? 0f : 90f);
        float W = L / 2f + ov, Z = S / 2f + ov;
        float rl = Mathf.Max(0f, (L - S) / 2f);
        float slope = rise / (S / 2f);
        float ye = -ov * slope, yr = rise;
        float up = Mathf.Min(0.7f, 0.12f + ov * 0.3f);
        float sag = Mathf.Min(0.45f, rise * 0.035f);
        const int N = 8;
        float t0 = 0.2f;
        Vector3[] ex = new Vector3[N + 1];   // x = half-X, y = height, z = half-Z
        for (int i = 0; i <= N; i++)
        {
            float u = i / (float)N;
            float hx = Mathf.Lerp(W, rl, u), hz = Mathf.Lerp(Z, 0f, u);
            float y = Mathf.Lerp(ye, yr, u) + up * Mathf.Pow(Mathf.Clamp01(1f - u * 3.4f), 2.2f) - sag * Mathf.Sin(u * Mathf.PI);
            ex[i] = new Vector3(hx, y, hz);
        }
        for (int i = 0; i < N; i++)
        {
            Vector3 a = ex[i], d = ex[i + 1];
            Quad(roofSlot, new Vector3(-a.x, a.y, a.z), new Vector3(a.x, a.y, a.z), new Vector3(d.x, d.y, d.z), new Vector3(-d.x, d.y, d.z));
            Quad(roofSlot, new Vector3(a.x, a.y, -a.z), new Vector3(-a.x, a.y, -a.z), new Vector3(-d.x, d.y, -d.z), new Vector3(d.x, d.y, -d.z));
            Quad(roofSlot, new Vector3(a.x, a.y, a.z), new Vector3(a.x, a.y, -a.z), new Vector3(d.x, d.y, -d.z), new Vector3(d.x, d.y, d.z));
            Quad(roofSlot, new Vector3(-a.x, a.y, -a.z), new Vector3(-a.x, a.y, a.z), new Vector3(-d.x, d.y, d.z), new Vector3(-d.x, d.y, -d.z));
        }
        float ee = ex[0].y;
        Quad(woodSlot, new Vector3(-W, ee - 0.05f, -Z), new Vector3(W, ee - 0.05f, -Z), new Vector3(W, ee - 0.05f, Z), new Vector3(-W, ee - 0.05f, Z));
        float t = 0.22f;
        Quad(woodSlot, new Vector3(-W, ee - t, Z), new Vector3(W, ee - t, Z), new Vector3(W, ee, Z), new Vector3(-W, ee, Z));
        Quad(woodSlot, new Vector3(W, ee - t, -Z), new Vector3(-W, ee - t, -Z), new Vector3(-W, ee, -Z), new Vector3(W, ee, -Z));
        Quad(woodSlot, new Vector3(W, ee - t, Z), new Vector3(W, ee - t, -Z), new Vector3(W, ee, -Z), new Vector3(W, ee, Z));
        Quad(woodSlot, new Vector3(-W, ee - t, -Z), new Vector3(-W, ee - t, Z), new Vector3(-W, ee, Z), new Vector3(-W, ee, -Z));
        int nk = Mathf.Max(3, Mathf.RoundToInt(W * 2f / 0.8f));
        for (int k = 0; k < nk; k++)
        {
            float x = -W + (k + 0.5f) * 2f * W / nk;
            Ball(roofSlot, new Vector3(x, ee + 0.02f, Z - 0.02f), W / nk * 0.9f, 3, 6, 0.45f);
            Ball(roofSlot, new Vector3(x, ee + 0.02f, -Z + 0.02f), W / nk * 0.9f, 3, 6, 0.45f);
        }
        int nz = Mathf.Max(3, Mathf.RoundToInt(Z * 2f / 0.8f));
        for (int k = 0; k < nz; k++)
        {
            float z = -Z + (k + 0.5f) * 2f * Z / nz;
            Ball(roofSlot, new Vector3(W - 0.02f, ee + 0.02f, z), Z / nz * 0.9f, 3, 6, 0.45f);
            Ball(roofSlot, new Vector3(-W + 0.02f, ee + 0.02f, z), Z / nz * 0.9f, 3, 6, 0.45f);
        }
        // jalur genteng: cincin tipis mengikuti lereng
        for (int j = 1; j < 6; j++)
        {
            float u = j / 6.2f; float fi = u * N; int i0 = Mathf.Min(N - 1, (int)fi); float ft = fi - i0;
            Vector3 q = Vector3.Lerp(ex[i0], ex[i0 + 1], ft);
            float o2 = 0.04f;
            Vector3 c0 = new Vector3(-q.x, q.y + o2, q.z), c1 = new Vector3(q.x, q.y + o2, q.z), c2 = new Vector3(q.x, q.y + o2, -q.z), c3 = new Vector3(-q.x, q.y + o2, -q.z);
            float ww = 0.3f, hh = 0.07f;
            Beam(roofSlot, c0, c1, ww, hh, Vector3.up); Beam(roofSlot, c1, c2, ww, hh, Vector3.up);
            Beam(roofSlot, c2, c3, ww, hh, Vector3.up); Beam(roofSlot, c3, c0, ww, hh, Vector3.up);
        }
        if (rl > 0.1f)
        {
            Box(roofSlot, new Vector3(0f, yr + 0.02f, 0f), new Vector3(rl * 2f + 0.4f, 0.3f, 0.42f));
            int nr2 = Mathf.Max(3, Mathf.RoundToInt(rl * 2f / 0.5f));
            for (int k = 0; k < nr2; k++) Ball(roofSlot, new Vector3(-rl + (k + 0.5f) * 2f * rl / nr2, yr + 0.2f, 0f), rl / nr2 * 0.95f, 3, 6, 0.55f);
        }
        Pop();
    }

    /// <summary>Atap menara kerucut cembung bergaya anime (sedikit melengkung) dengan lis di dasar dan pucuk.</summary>
    public void Spire(int s, Vector3 b, float r, float h, int n, float flare = 0.0f)
    {
        // profil terompet: cekung (curam di pucuk, melebar landai di tepi) seperti atap menara anime
        int M = 9;
        float exp = 1.75f - Mathf.Clamp(flare, 0f, 0.5f);
        float yPrev = 0f, rPrev = r;
        for (int k = 1; k <= M; k++)
        {
            float t = k / (float)M;
            float y = h * t;
            float rr = r * Mathf.Pow(1f - t, exp);
            Cyl(s, b + new Vector3(0f, yPrev, 0f), rPrev, y - yPrev, n, rr, false, false);
            if (k < M - 1 && r > 0.9f) Cyl(s, b + new Vector3(0f, y - 0.05f, 0f), rr * 1.045f + 0.03f, 0.1f, n, rr * 1.045f + 0.03f, true);   // cincin genteng
            yPrev = y; rPrev = rr;
        }
    }

    // ------------------------------------------------------------------ permukaan datar (jalan, lantai)
    /// <summary>Pita mengikuti garis tengah (jalan). Titik berupa x,z. UV: u = panjang (m), v = lebar (m).</summary>
    public void Ribbon(int s, IList<Vector2> pts, float width, float y)
    {
        if (pts.Count < 2) return;
        float acc = 0f;
        int prevL = -1, prevR = -1;
        for (int i = 0; i < pts.Count; i++)
        {
            Vector2 d;
            if (i == 0) d = pts[1] - pts[0];
            else if (i == pts.Count - 1) d = pts[i] - pts[i - 1];
            else d = pts[i + 1] - pts[i - 1];
            if (d.sqrMagnitude < 1e-6f) d = Vector2.up;
            d.Normalize();
            Vector2 nrm = new Vector2(-d.y, d.x) * (width / 2f);
            if (i > 0) acc += (pts[i] - pts[i - 1]).magnitude;
            int l = AddV(s, new Vector3(pts[i].x - nrm.x, y, pts[i].y - nrm.y), Vector3.up, new Vector2(acc, 0f));
            int r = AddV(s, new Vector3(pts[i].x + nrm.x, y, pts[i].y + nrm.y), Vector3.up, new Vector2(acc, width));
            if (i > 0)
            {
                AddT(s, prevL, r, l);
                AddT(s, prevL, prevR, r);
            }
            prevL = l; prevR = r;
        }
    }

    /// <summary>Cakram datar (alun-alun) dengan UV dunia.</summary>
    public void Disc(int s, Vector3 center, float r, int segs, float y)
    {
        int c = AddV(s, new Vector3(center.x, y, center.z), Vector3.up, new Vector2(center.x, center.z));
        int prev = -1;
        for (int i = 0; i <= segs; i++)
        {
            float a = (i % segs) / (float)segs * Mathf.PI * 2f;
            Vector3 p = new Vector3(center.x + Mathf.Sin(a) * r, y, center.z + Mathf.Cos(a) * r);
            int v = AddV(s, p, Vector3.up, new Vector2(p.x, p.z));
            if (i > 0) AddT(s, c, prev, v);
            prev = v;
        }
    }

    /// <summary>Cincin datar antara radius r0 dan r1 (sudut a0..a1 derajat, 0 = +Z, searah jarum jam dari atas).</summary>
    public void Annulus(int s, Vector3 center, float r0, float r1, float a0, float a1, int segs, float y)
    {
        int prevI = -1, prevO = -1;
        for (int i = 0; i <= segs; i++)
        {
            float a = Mathf.Lerp(a0, a1, i / (float)segs) * Mathf.Deg2Rad;
            float sn = Mathf.Sin(a), cs = Mathf.Cos(a);
            Vector3 pi = new Vector3(center.x + sn * r0, y, center.z + cs * r0);
            Vector3 po = new Vector3(center.x + sn * r1, y, center.z + cs * r1);
            int vi = AddV(s, pi, Vector3.up, new Vector2(pi.x, pi.z));
            int vo = AddV(s, po, Vector3.up, new Vector2(po.x, po.z));
            if (i > 0)
            {
                AddT(s, prevI, vo, vi);
                AddT(s, prevI, prevO, vo);
            }
            prevI = vi; prevO = vo;
        }
    }

    /// <summary>Segiempat datar di tanah (petak sawah). Titik searah jarum jam dilihat dari atas; UV dunia diputar ke arah baris tanaman.</summary>
    public void Patch(int s, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float y, Vector2 rowDir)
    {
        Vector2 ru = rowDir.normalized;
        Vector2 rv = new Vector2(-ru.y, ru.x);
        Vector2[] ps = { p0, p1, p2, p3 };
        int[] id = new int[4];
        for (int i = 0; i < 4; i++)
            id[i] = AddV(s, new Vector3(ps[i].x, y, ps[i].y), Vector3.up, new Vector2(Vector2.Dot(ps[i], ru), Vector2.Dot(ps[i], rv)));
        AddT(s, id[0], id[1], id[2]);
        AddT(s, id[0], id[2], id[3]);
    }


    /// <summary>
    /// Loft halus: tiap cincin = n titik (urutan searah jarum jam dilihat dari atas, mulai dari +Z), cincin dari bawah ke atas.
    /// Normal dihaluskan; tutup bawah/atas opsional (datar).
    /// </summary>
    public void Loft(int s, IList<Vector3[]> rings, bool capBottom, bool capTop, bool smooth = true)
    {
        int R = rings.Count, n = rings[0].Length;
        var ids = new int[R, n];
        for (int i = 0; i < R; i++)
            for (int j = 0; j < n; j++)
            {
                Vector3 p = rings[i][j];
                Vector3 du = rings[i][(j + 1) % n] - rings[i][(j + n - 1) % n];
                Vector3 dv = rings[Mathf.Min(R - 1, i + 1)][j] - rings[Mathf.Max(0, i - 1)][j];
                Vector3 nm = Vector3.Cross(du, dv);
                if (nm.sqrMagnitude < 1e-12f)
                {
                    Vector3 c = Vector3.zero;
                    foreach (var q in rings[i]) c += q;
                    c /= n;
                    nm = new Vector3(p.x - c.x, 0.0001f, p.z - c.z);
                }
                ids[i, j] = AddV(s, p, nm.normalized, new Vector2(j / (float)n * 2f, p.y));
            }
        for (int i = 0; i + 1 < R; i++)
            for (int j = 0; j < n; j++)
            {
                int j2 = (j + 1) % n;
                int a = ids[i, j], b = ids[i, j2], c = ids[i + 1, j2], d = ids[i + 1, j];
                AddT(s, a, b, c);
                AddT(s, a, c, d);
            }
        if (capBottom) LoftCap(s, rings[0], false);
        if (capTop) LoftCap(s, rings[R - 1], true);
    }

    void LoftCap(int s, Vector3[] ring, bool up)
    {
        int n = ring.Length;
        Vector3 c = Vector3.zero;
        foreach (var q in ring) c += q;
        c /= n;
        Vector3 nm = up ? Vector3.up : Vector3.down;
        int ci = AddV(s, c, nm, new Vector2(0f, 0f));
        var vi = new int[n];
        for (int j = 0; j < n; j++) vi[j] = AddV(s, ring[j], nm, new Vector2(ring[j].x, ring[j].z));
        for (int j = 0; j < n; j++)
        {
            int j2 = (j + 1) % n;
            if (up) AddT(s, ci, vi[j], vi[j2]);       // searah jarum jam dilihat dari atas -> menghadap atas
            else AddT(s, ci, vi[j2], vi[j]);
        }
    }

    /// <summary>
    /// Segitiga mentah dengan normal dan UV buatan sendiri (bentuk organik yang dihaluskan sendiri, mis. gumpalan daun, rok cemara).
    /// Titik dikenai transformasi Push/Pop yang sedang aktif. tri = indeks segitiga relatif terhadap daftar titik ini.
    /// </summary>
    public void Raw(int s, IList<Vector3> v, IList<Vector3> n, IList<Vector2> uv, IList<int> tri)
    {
        if (v == null || tri == null || v.Count == 0 || tri.Count < 3) return;
        int off = C(s).v.Count;
        for (int i = 0; i < v.Count; i++) AddV(s, v[i], n[i], uv[i]);
        var l = C(s).t;
        for (int i = 0; i + 2 < tri.Count; i += 3) { l.Add(tri[i] + off); l.Add(tri[i + 1] + off); l.Add(tri[i + 2] + off); }
    }

    // ------------------------------------------------------------------ komposit kecil
    public void Stairs(int s, Vector3 baseCenter, float width, int steps, float rise, float run, float yaw)
    {
        Push(baseCenter, yaw);
        for (int i = 0; i < steps; i++)      // anak tangga naik ke arah -Z lokal; i = 0 paling bawah (paling depan)
            BoxB(s, new Vector3(0f, 0f, -i * run), new Vector3(width, rise * (i + 1), run), 0f);
        Pop();
    }

    public void Crenellate(int s, Vector3 a, Vector3 b, float height, float thick, float merlonLen, float gap)
    {
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 0.1f) return;
        float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        float step = merlonLen + gap;
        int n = Mathf.Max(1, Mathf.FloorToInt(len / step));
        for (int i = 0; i < n; i++)
        {
            float t = (i + 0.5f) * step / len;
            Vector3 p = a + d * t;
            Box(s, p + new Vector3(0f, height / 2f, 0f), new Vector3(thick, height, merlonLen), yaw);
        }
    }

    // ------------------------------------------------------------------ keluaran
    public GameObject Emit(Transform parent, string name, Material[] slotMats, string assetPath = null)
    {
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var uvs = new List<Vector2>();
        var subs = new List<int[]>();
        var mats = new List<Material>();
        for (int s = 0; s < Sl.Count; s++)
        {
            if (ch[s] == null || ch[s].t.Count == 0) continue;
            int off = verts.Count;
            verts.AddRange(ch[s].v);
            norms.AddRange(ch[s].n);
            uvs.AddRange(ch[s].uv);
            var tr = new int[ch[s].t.Count];
            for (int i = 0; i < tr.Length; i++) tr[i] = ch[s].t[i] + off;
            subs.Add(tr);
            mats.Add(slotMats[s]);
        }
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        if (verts.Count == 0) return go;

        var mesh = new Mesh();
        mesh.name = name;
        mesh.indexFormat = verts.Count > 60000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        HunterGuild.MeshSafe.Clean(verts); mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = subs.Count;
        for (int i = 0; i < subs.Count; i++) mesh.SetTriangles(subs[i], i);
        mesh.RecalculateBounds();

        if (assetPath != null && MeshSaver != null && Application.isEditor && !Application.isPlaying) mesh = MeshSaver(mesh, assetPath);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterials = mats.ToArray();
        return go;
    }

    /// <summary>Hanya menghasilkan Mesh (untuk arketipe rumah yang dipakai ulang).</summary>
    public Mesh ToMesh(string name, string assetPath, out int[] usedSlots)
    {
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var uvs = new List<Vector2>();
        var subs = new List<int[]>();
        var used = new List<int>();
        for (int s = 0; s < Sl.Count; s++)
        {
            if (ch[s] == null || ch[s].t.Count == 0) continue;
            int off = verts.Count;
            verts.AddRange(ch[s].v);
            norms.AddRange(ch[s].n);
            uvs.AddRange(ch[s].uv);
            var tr = new int[ch[s].t.Count];
            for (int i = 0; i < tr.Length; i++) tr[i] = ch[s].t[i] + off;
            subs.Add(tr);
            used.Add(s);
        }
        var mesh = new Mesh();
        mesh.name = name;
        mesh.indexFormat = verts.Count > 60000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        HunterGuild.MeshSafe.Clean(verts); mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = subs.Count;
        for (int i = 0; i < subs.Count; i++) mesh.SetTriangles(subs[i], i);
        mesh.RecalculateBounds();
        usedSlots = used.ToArray();
        if (assetPath != null && MeshSaver != null && Application.isEditor && !Application.isPlaying) mesh = MeshSaver(mesh, assetPath);
        return mesh;
    }

    /// <summary>
    /// Satu submesh untuk semua saluran: UV tiap titik menunjuk satu texel palet warna (u = (slot + 0.5) / Sl.Count, v = 0.5).
    /// Hasilnya cukup SATU bahan (tekstur palet) per model — jauh lebih sedikit draw call daripada satu submesh per warna.
    /// </summary>
    public Mesh ToMeshPalette(string name, out int[] usedSlots)
    {
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        var used = new List<int>();
        for (int s = 0; s < Sl.Count; s++)
        {
            if (ch[s] == null || ch[s].t.Count == 0) continue;
            int off = verts.Count;
            verts.AddRange(ch[s].v);
            norms.AddRange(ch[s].n);
            var u = new Vector2((s + 0.5f) / Sl.Count, 0.5f);
            for (int i = 0; i < ch[s].v.Count; i++) uvs.Add(u);
            for (int i = 0; i < ch[s].t.Count; i++) tris.Add(ch[s].t[i] + off);
            used.Add(s);
        }
        var mesh = new Mesh();
        mesh.name = name;
        mesh.indexFormat = verts.Count > 60000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        HunterGuild.MeshSafe.Clean(verts); mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = 1;
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        usedSlots = used.ToArray();
        return mesh;
    }
}

namespace HunterGuild
{
    /// <summary>Pengaman mesh: titik NaN/Infinity (mis. hasil 0/0) diganti 0 agar Unity tidak melaporkan "Converting invalid MinMaxAABB".</summary>
    public static class MeshSafe
    {
        public static void Clean(UnityEngine.Vector3[] v)
        {
            if (v == null) return;
            for (int i = 0; i < v.Length; i++)
            {
                var p = v[i];
                if (float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z) || float.IsInfinity(p.x) || float.IsInfinity(p.y) || float.IsInfinity(p.z))
                    v[i] = UnityEngine.Vector3.zero;
            }
        }

        public static void Clean(System.Collections.Generic.List<UnityEngine.Vector3> v)
        {
            if (v == null) return;
            for (int i = 0; i < v.Count; i++)
            {
                var p = v[i];
                if (float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z) || float.IsInfinity(p.x) || float.IsInfinity(p.y) || float.IsInfinity(p.z))
                    v[i] = UnityEngine.Vector3.zero;
            }
        }
    }
}
