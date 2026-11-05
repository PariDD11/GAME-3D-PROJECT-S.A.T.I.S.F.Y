using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tembok Kota Ashwatch (sisi data + geometri, tanpa API editor): memotong jalur tembok di gerbang & lengkung sungai, memecahnya jadi bentang,
/// menaruh menara sudut/antara, dan menghasilkan daftar collider. Dipakai builder Kota (lengkap) dan latar kota di scene hutan (sebagian).
/// Konvensi: titik tembok berurutan berlawanan arah jarum jam (sisi dalam di KIRI) -> sisi luar benteng di kanan (outerSign = -1).
/// </summary>
public static class T2Walls
{
    public const float ArchW = 13f, ArchDepth = 8f, ArchHalf = ArchW * 0.5f + 9.5f;

    public class Col { public bool capsule; public Vector3 c; public Vector3 size; public float yaw; public float r, h; }
    public class Tower { public float x, z, gy, r, top; public bool cone; public int seed; }
    public class Arch { public Vector2 p; public Vector2 outward; public float yaw, yA; }

    public class Result
    {
        public readonly List<Col> cols = new List<Col>();
        public readonly List<Tower> towers = new List<Tower>();
        public readonly List<Arch> arches = new List<Arch>();
        public int runs;
    }

    // ------------------------------------------------------------------ polyline berparameter jarak
    class Path
    {
        public IList<Vector2> p; public bool closed; public float[] cum; public float total; public int n;
        public Path(IList<Vector2> pts, bool closed)
        {
            p = pts; this.closed = closed; n = pts.Count;
            int segs = closed ? n : n - 1;
            cum = new float[segs + 1];
            for (int i = 0; i < segs; i++) cum[i + 1] = cum[i] + (pts[(i + 1) % n] - pts[i]).magnitude;
            total = cum[segs];
        }
        public Vector2 At(float d)
        {
            if (closed) { d = Mathf.Repeat(d, total); } else d = Mathf.Clamp(d, 0f, total);
            int segs = cum.Length - 1;
            int i = 0;
            while (i + 1 < segs && cum[i + 1] < d) i++;
            float L = Mathf.Max(0.0001f, cum[i + 1] - cum[i]);
            float t = Mathf.Clamp01((d - cum[i]) / L);
            return Vector2.Lerp(p[i], p[(i + 1) % n], t);
        }
        /// <summary>Jarak terdekat titik q ke garis tembok.</summary>
        public float Dist(Vector2 q)
        {
            float best = 1e9f;
            int segs = cum.Length - 1;
            for (int i = 0; i < segs; i++)
            {
                Vector2 a = p[i], b = p[(i + 1) % n];
                Vector2 v = b - a; float l2 = v.x * v.x + v.y * v.y;
                float t = l2 < 1e-6f ? 0f : Mathf.Clamp01(((q.x - a.x) * v.x + (q.y - a.y) * v.y) / l2);
                float dx = a.x + v.x * t - q.x, dz = a.y + v.y * t - q.y;
                best = Mathf.Min(best, dx * dx + dz * dz);
            }
            return Mathf.Sqrt(best);
        }
        public float Arc(Vector2 q)
        {
            float best = 1e9f, arc = 0f;
            int segs = cum.Length - 1;
            for (int i = 0; i < segs; i++)
            {
                Vector2 a = p[i], b = p[(i + 1) % n];
                Vector2 v = b - a; float l2 = v.x * v.x + v.y * v.y;
                float t = l2 < 1e-6f ? 0f : Mathf.Clamp01(((q.x - a.x) * v.x + (q.y - a.y) * v.y) / l2);
                Vector2 c = new Vector2(a.x + v.x * t, a.y + v.y * t);
                float d = (c.x - q.x) * (c.x - q.x) + (c.y - q.y) * (c.y - q.y);
                if (d < best) { best = d; arc = cum[i] + Mathf.Sqrt(l2) * t; }
            }
            return arc;
        }
        /// <summary>Titik-titik dari d0 sampai d1 (d1 &gt;= d0, boleh melewati batas total pada poligon tertutup): semua simpul asli di antaranya ikut, selang maksimum step.</summary>
        public List<Vector2> Slice(float d0, float d1, float step)
        {
            var br = new List<float>();
            br.Add(d0);
            int segs = cum.Length - 1;
            int laps = closed ? 2 : 1;
            for (int lap = 0; lap < laps; lap++)
                for (int i = 0; i <= segs; i++)
                {
                    float d = cum[i] + lap * total;
                    if (d > d0 + 0.05f && d < d1 - 0.05f) br.Add(d);
                }
            br.Add(d1);
            br.Sort();
            var res = new List<Vector2>();
            for (int k = 0; k + 1 < br.Count; k++)
            {
                float a = br[k], b = br[k + 1];
                if (b - a < 0.01f) continue;
                int m = Mathf.Max(1, Mathf.CeilToInt((b - a) / step));
                for (int j = 0; j < m; j++) res.Add(At(a + (b - a) * j / m));
            }
            res.Add(At(d1));
            return res;
        }
    }

    class Cut { public float c, h; public int kind; public Vector2 p; }       // kind 0 = gerbang, 1 = lengkung sungai

    // ------------------------------------------------------------------ sudut belok di simpul i (derajat)
    static float Turn(IList<Vector2> p, int i, bool closed)
    {
        int n = p.Count;
        if (!closed && (i == 0 || i == n - 1)) return 180f;
        Vector2 a = p[(i + n - 1) % n], b = p[i], c = p[(i + 1) % n];
        Vector2 u = (b - a).normalized, v = (c - b).normalized;
        return Mathf.Acos(Mathf.Clamp(u.x * v.x + u.y * v.y, -1f, 1f)) * Mathf.Rad2Deg;
    }

    /// <summary>Lengkung air untuk tiap titik silang sungai-tembok luar.</summary>
    public static List<Arch> ArchesOf(System.Func<float, float, float> ground)
    {
        var res = new List<Arch>();
        foreach (var c in T2.WaterArches)
        {
            Vector2 tg; Vector2 np = T2.NearestOnPoly(c.x, c.y, T2.OuterPoly, out tg);
            // titik tembok yang dilintasi: gunakan titik silang dari data (c); garis singgung dari poligon (berlawanan arah jarum jam -> luar di kanan)
            Vector2 t2; T2.NearestOnPoly(c.x, c.y, closedOuter(), out t2);
            Vector2 outv = new Vector2(t2.y, -t2.x).normalized;
            float yaw = Mathf.Atan2(outv.x, outv.y) * Mathf.Rad2Deg;
            float yA = 0.5f * (ground(c.x + t2.x * (ArchHalf + 4f), c.y + t2.y * (ArchHalf + 4f)) + ground(c.x - t2.x * (ArchHalf + 4f), c.y - t2.y * (ArchHalf + 4f)));
            res.Add(new Arch { p = c, outward = outv, yaw = yaw, yA = yA });
        }
        return res;
    }

    static List<Vector2> closedOuter()
    {
        var l = new List<Vector2>(T2.OuterPoly);
        l.Add(T2.OuterPoly[0]);
        return l;
    }

    static List<Vector2> PathPts(T2.Wall w)
    {
        return new List<Vector2>(w.pts);
    }

    // ------------------------------------------------------------------ bangun satu tembok
    /// <param name="include">filter titik tengah bentang/menara (null = semua); dipakai latar kota agar hanya sisi selatan yang dibangun</param>
    public static void BuildWall(T2.Wall w, GeoBag bag, Result res, System.Func<float, float, float> ground, System.Func<float, float, bool> include)
    {
        var pts = PathPts(w);
        var path = new Path(pts, w.closed);
        var sp = T2Gate.SpecFor(w.tier);
        float pierOut = sp.tX + sp.towerR * 0.55f;
        var cuts = new List<Cut>();
        foreach (var g in T2.Gates)       // n81: Tembok Dalam dan tembok pembatas kastil sama-sama tier 2, jadi hanya gerbang yang benar-benar berdiri di garis tembok ini yang memotongnya
            if (g.tier == w.tier && path.Dist(g.p) < 3f) cuts.Add(new Cut { c = path.Arc(g.p), h = pierOut - 0.5f, kind = 0, p = g.p });
        if (w.tier == 0)
            foreach (var a in res.arches) cuts.Add(new Cut { c = path.Arc(a.p), h = ArchHalf - 0.4f, kind = 1, p = a.p });
        cuts.Sort((x, y) => x.c.CompareTo(y.c));

        // bentang (run) di antara pemotongan
        var spans = new List<Vector2>();      // (d0, d1)
        if (cuts.Count == 0) spans.Add(new Vector2(0f, path.total));
        else if (w.closed)
        {
            for (int i = 0; i < cuts.Count; i++)
            {
                var a = cuts[i]; var b = cuts[(i + 1) % cuts.Count];
                float d0 = a.c + a.h, d1 = b.c - b.h + (i + 1 == cuts.Count ? path.total : 0f);
                if (d1 - d0 > 1.5f) spans.Add(new Vector2(d0, d1));
            }
        }
        else
        {
            float d0 = 0f;
            foreach (var c in cuts) { if (c.c - c.h - d0 > 1.5f) spans.Add(new Vector2(d0, c.c - c.h)); d0 = c.c + c.h; }
            if (path.total - d0 > 1.5f) spans.Add(new Vector2(d0, path.total));
        }

        int seed = 3 + w.tier * 11;
        float thick = w.thick, h = w.height;
        int spanIdx = 0;
        foreach (var span in spans)
        {
            spanIdx++;
            var pl = path.Slice(span.x, span.y, 2.5f);
            if (pl.Count < 2) continue;
            Vector2 mid = pl[pl.Count / 2];
            if (include != null && !include(mid.x, mid.y)) continue;
            var smp = T2Kit.Resample(pl, ground, 100f, false);          // step besar: titik sudah rapat, hanya menambah tinggi tanah
            T2Kit.WallRun(bag, smp, -1f, thick, h, seed + spanIdx, false, false);
            res.runs++;
            AddWallColliders(smp, thick, h, res.cols);
        }

        // menara: simpul bertikungan tajam (dan ujung bila terbuka), lalu menara antara (n76: rapat, tiap ~36 m di tembok luar)
        float spacing = w.tier == 0 ? 36f : (w.tier == 1 ? 44f : 32f);
        var tw = new List<Vector2>();
        var twCone = new List<bool>();
        for (int i = 0; i < pts.Count; i++)
        {
            if (!w.closed && (i == 0 || i == pts.Count - 1)) continue;          // ujung tembok terbuka menyatu ke tembok lain (menara ada di tembok induk)
            float tr = Turn(pts, i, w.closed);
            if (tr < 11f) continue;
            if (InCut(path, cuts, pts[i], 3.5f)) continue;
            tw.Add(pts[i]); twCone.Add(true);
        }
        foreach (var span in spans)
        {
            float len = span.y - span.x;
            int m = Mathf.Max(0, Mathf.RoundToInt(len / spacing) - 1);
            for (int k = 1; k <= m; k++)
            {
                Vector2 q = path.At(span.x + len * k / (m + 1));
                bool near = false;
                foreach (var t in tw) if ((t - q).magnitude < spacing * 0.45f) { near = true; break; }
                if (near || InCut(path, cuts, q, 3f)) continue;
                tw.Add(q); twCone.Add(false);
            }
        }
        for (int i = 0; i < tw.Count; i++)
        {
            if (include != null && !include(tw[i].x, tw[i].y)) continue;
            AddTower(res, ground, tw[i], w.tier, twCone[i], 100 + w.tier * 40 + i, h);
        }
    }

    static bool InCut(Path path, List<Cut> cuts, Vector2 q, float margin)
    {
        foreach (var c in cuts)
        {
            if ((q - c.p).magnitude < c.h + margin) return true;
        }
        return false;
    }

    public static void AddTower(Result res, System.Func<float, float, float> ground, Vector2 q, int tier, bool cone, int seed, float wallH)
    {
        float r = tier == 2 ? (cone ? 5.6f : 4.9f) : (tier == 1 ? (cone ? 5.2f : 4.5f) : (cone ? 5.6f : 4.8f));
        float gmin = 1e9f, gc = ground(q.x, q.y);
        for (int k = 0; k < 8; k++)
        {
            float a = k * Mathf.PI * 0.25f;
            gmin = Mathf.Min(gmin, ground(q.x + Mathf.Sin(a) * r, q.y + Mathf.Cos(a) * r));
        }
        float gy = Mathf.Min(gmin, gc) - 0.7f;
        float top = gc + wallH + (cone ? 6.5f : 5.0f);
        res.towers.Add(new Tower { x = q.x, z = q.y, gy = gy, r = r, top = top, cone = cone, seed = seed });
        res.cols.Add(new Col { capsule = true, c = new Vector3(q.x, (gy + top) * 0.5f, q.y), r = r + 0.35f, h = top - gy + (cone ? 6f : 0f) });
    }

    /// <summary>Collider kotak per bentang lurus (titik hampir segaris digabung, maks 14 m).</summary>
    static void AddWallColliders(IList<T2Kit.WPt> s, float thick, float h, List<Col> cols)
    {
        int n = s.Count, i = 0;
        while (i + 1 < n)
        {
            int j = i + 1;
            Vector2 a = new Vector2(s[i].x, s[i].z), b = new Vector2(s[j].x, s[j].z);
            Vector2 dir = (b - a).normalized;
            float gmin = Mathf.Min(s[i].y, s[j].y), gmax = Mathf.Max(s[i].y, s[j].y);
            while (j + 1 < n)
            {
                Vector2 c = new Vector2(s[j + 1].x, s[j + 1].z);
                Vector2 d2 = (c - new Vector2(s[j].x, s[j].z)).normalized;
                if (d2.x * dir.x + d2.y * dir.y < 0.9986f) break;               // belok > ~3 derajat
                if ((c - a).magnitude > 14f) break;
                j++;
                gmin = Mathf.Min(gmin, s[j].y); gmax = Mathf.Max(gmax, s[j].y);
            }
            Vector2 e = new Vector2(s[j].x, s[j].z);
            Vector2 m = (a + e) * 0.5f;
            float len = (e - a).magnitude + 0.4f;
            float y0 = gmin - 1.5f, y1 = gmax + h + 1.0f;
            cols.Add(new Col { c = new Vector3(m.x, (y0 + y1) * 0.5f, m.y), size = new Vector3(thick + 0.3f, y1 - y0, len), yaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg });
            i = j;
        }
    }

    // ------------------------------------------------------------------ lengkung air: geometri (dipasang di Push(pos, yaw) oleh pemanggil) + collider
    /// <summary>Isi 'b' dengan lengkung air pada bingkai lokal arch (y = 0 pada tanah bahu). Mengembalikan parameter yang dipakai.</summary>
    public static void ArchGeometry(GeoBag b, Arch a)
    {
        float bedY = T2.RiverBed - a.yA - 0.3f;
        float spring = 1.0f, crown = 6.0f;
        T2Kit.WaterArch(b, ArchW, ArchDepth, 13f, bedY, spring, crown);
    }

    public static void ArchColliders(Arch a, List<Col> cols)
    {
        float hw = ArchW * 0.5f, X1 = ArchHalf, D = ArchDepth, hd = D * 0.5f;
        float bedY = T2.RiverBed - a.yA - 0.3f, crown = 6.0f, wallH = 13f;
        float yw = a.yA;
        System.Action<float, float, float, float, float, float> box = (lx, ly0, ly1, lz, sx, sz) =>
        {
            float cs = Mathf.Cos(a.yaw * Mathf.Deg2Rad), sn = Mathf.Sin(a.yaw * Mathf.Deg2Rad);
            Vector3 c = new Vector3(a.p.x + lx * cs + lz * sn, yw + (ly0 + ly1) * 0.5f, a.p.y - lx * sn + lz * cs);
            cols.Add(new Col { c = c, size = new Vector3(sx, ly1 - ly0, sz), yaw = a.yaw });
        };
        foreach (float sg in new[] { -1f, 1f })
        {
            box(sg * (hw + X1) * 0.5f, bedY, wallH + 1.5f, 0f, X1 - hw, D);                    // pilar muka kiri / kanan
            box(0f, bedY, bedY + 0.1f, 0f, 1f, 1f);                                           // (penanda, tak berpengaruh)
        }
        box(0f, crown, wallH + 1.5f, 0f, ArchW, D);                                           // di atas lengkung
        foreach (float sz in new[] { -1f, 1f }) box(0f, bedY, crown, sz * (hd - 1.3f), ArchW, 0.5f);       // terali besi (menahan perenang)
        foreach (float sg in new[] { -1f, 1f })
        {
            float cs = Mathf.Cos(a.yaw * Mathf.Deg2Rad), sn = Mathf.Sin(a.yaw * Mathf.Deg2Rad);
            float lx = sg * (hw + 3.8f);
            Vector3 c = new Vector3(a.p.x + lx * cs, yw + (bedY + 1.5f + 13f + 6.5f) * 0.5f, a.p.y - lx * sn);
            cols.Add(new Col { capsule = true, c = c, r = 3.85f, h = 13f + 6.5f - bedY });
        }
    }
}
