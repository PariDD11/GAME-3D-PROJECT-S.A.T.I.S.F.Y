using System;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Makhluk mitologi sebagai GAMBAR VEKTOR dalam kode: siluet + lapisan warna bergradasi hasil pelacakan ilustrasi referensi
    /// (data di SkillSilhouette.TraceData*.cs). Tidak ada file gambar: bentuk disusun dari poligon, dibangun jadi mesh, dan digerakkan
    /// per-frame (napas, langkah kaki, ekor, kepakan sayap, auman).
    /// </summary>
    public static partial class SkillSilhouette
    {
        internal sealed class TraceArt
        {
            public float[] nx, ny;          // posisi dasar ternormalisasi: nx -0.5..0.5 (kiri->kanan), ny 0..1 (bawah->atas)
            public Color[] col;
            public int[] tris;
            public float aspect;            // lebar / tinggi
            static Dictionary<string, string> data;
            static readonly Dictionary<string, TraceArt> cache = new Dictionary<string, TraceArt>();

            public static bool Has(string id)
            {
                Init(); return !string.IsNullOrEmpty(id) && data.ContainsKey(id);
            }

            static void Init()
            {
                if (data != null) return;
                data = new Dictionary<string, string>();
                TraceA(data); TraceB(data); TraceC(data); TraceD(data);
            }

            public static TraceArt Get(string id)
            {
                Init();
                if (string.IsNullOrEmpty(id)) return null;
                TraceArt t; if (cache.TryGetValue(id, out t)) return t;
                string s; if (!data.TryGetValue(id, out s)) { cache[id] = null; return null; }
                t = Build(s); cache[id] = t; return t;
            }

            static Vector2[] Ring(string s)
            {
                var p = s.Split(' ');
                int n = p.Length / 2; var r = new Vector2[n];
                int x = 0, y = 0;
                for (int i = 0; i < n; i++)
                {
                    int a = int.Parse(p[2 * i]), b = int.Parse(p[2 * i + 1]);
                    if (i == 0) { x = a; y = b; } else { x += a; y += b; }
                    r[i] = new Vector2(x, y);
                }
                return r;
            }

            static TraceArt Build(string s)
            {
                var parts = s.Split('~');
                var hd = parts[0].Split(',');
                float W = float.Parse(hd[0]), H = float.Parse(hd[1]);
                var pal = new Color[hd.Length - 2];
                for (int i = 0; i < pal.Length; i++)
                {
                    string h = hd[i + 2];
                    pal[i] = new Color(Convert.ToInt32(h.Substring(0, 2), 16) / 255f, Convert.ToInt32(h.Substring(2, 2), 16) / 255f, Convert.ToInt32(h.Substring(4, 2), 16) / 255f, 1f);
                }
                var X = new List<float>(); var Y = new List<float>(); var C = new List<Color>(); var T = new List<int>();
                for (int pi = 1; pi < parts.Length; pi++)
                {
                    string body = parts[pi]; int k = 0;
                    if (pi > 1) { int c = body.IndexOf(':'); k = int.Parse(body.Substring(0, c)); body = body.Substring(c + 1); }
                    if (body.Length == 0) continue;
                    foreach (var poly in body.Split('|'))
                    {
                        if (poly.Length == 0) continue;
                        var rs = poly.Split(';');
                        var outer = new List<Vector2>(Ring(rs[0]));
                        var holes = new List<List<Vector2>>();
                        for (int i = 1; i < rs.Length; i++) holes.Add(new List<Vector2>(Ring(rs[i])));
                        var merged = Merge(outer, holes);
                        int b0 = X.Count;
                        Color col = pal[Mathf.Min(k, pal.Length - 1)];
                        foreach (var v in merged) { X.Add(v.x / W - 0.5f); Y.Add(1f - v.y / H); C.Add(col); }
                        int t0 = T.Count;
                        EarClip(merged, T, b0);
                        // pengaman: poligon yang berpotongan sendiri bisa menghasilkan segitiga "nyasar" -> buang saja, warna dasar tetap tampil
                        float want = Mathf.Abs(Area2(merged)) * 0.5f, got = 0f;
                        for (int q = t0; q < T.Count; q += 3) got += Mathf.Abs(Cr(merged[T[q] - b0], merged[T[q + 1] - b0], merged[T[q + 2] - b0])) * 0.5f;
                        if (got > want * 1.12f + 6f) T.RemoveRange(t0, T.Count - t0);
                    }
                }
                var t = new TraceArt { nx = X.ToArray(), ny = Y.ToArray(), tris = T.ToArray(), aspect = W / H };
                t.col = new Color[X.Count];
                for (int i = 0; i < t.col.Length; i++)
                {
                    float m = Mathf.Lerp(0.9f, 1.16f, Mathf.Clamp01(t.ny[i]));      // gradasi: bawah lebih gelap, atas lebih terang
                    Color c = C[i]; t.col[i] = new Color(Mathf.Min(1f, c.r * m + 0.02f * t.ny[i]), Mathf.Min(1f, c.g * m + 0.02f * t.ny[i]), Mathf.Min(1f, c.b * m + 0.03f * t.ny[i]), 1f);
                }
                return t;
            }

            // ------------------------------------------------------------------ triangulasi (poligon + lubang)
            static float Area2(List<Vector2> p) { float a = 0f; for (int i = 0, j = p.Count - 1; i < p.Count; j = i++) a += p[j].x * p[i].y - p[i].x * p[j].y; return a; }

            static bool SegX(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
            {
                float d1 = Cr(c, d, a), d2 = Cr(c, d, b), d3 = Cr(a, b, c), d4 = Cr(a, b, d);
                return ((d1 > 1e-4f && d2 < -1e-4f) || (d1 < -1e-4f && d2 > 1e-4f)) && ((d3 > 1e-4f && d4 < -1e-4f) || (d3 < -1e-4f && d4 > 1e-4f));
            }
            static float Cr(Vector2 a, Vector2 b, Vector2 c) { return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x); }

            static List<Vector2> Merge(List<Vector2> outer, List<List<Vector2>> holes)
            {
                var o = outer; if (Area2(o) < 0f) o.Reverse();
                if (holes.Count == 0) return o;
                foreach (var h in holes) if (Area2(h) > 0f) h.Reverse();
                holes.Sort((a, b) => MaxX(b).CompareTo(MaxX(a)));
                for (int hn = 0; hn < holes.Count; hn++)
                {
                    var h = holes[hn];
                    int hi = 0; for (int i = 1; i < h.Count; i++) if (h[i].x > h[hi].x) hi = i;
                    Vector2 hp = h[hi];
                    var order = new List<int>(); for (int i = 0; i < o.Count; i++) order.Add(i);
                    order.Sort((a, b) => (o[a] - hp).sqrMagnitude.CompareTo((o[b] - hp).sqrMagnitude));
                    int best = -1;
                    for (int q = 0; q < order.Count; q++)
                    {
                        int bi = order[q]; Vector2 bp = o[bi]; bool ok = true;
                        for (int i = 0; i < o.Count && ok; i++) { int j = (i + 1) % o.Count; if (i == bi || j == bi) continue; if (SegX(hp, bp, o[i], o[j])) ok = false; }
                        for (int g = hn; g < holes.Count && ok; g++)
                        {
                            var hh = holes[g];
                            for (int i = 0; i < hh.Count && ok; i++) { int j = (i + 1) % hh.Count; if (SegX(hp, bp, hh[i], hh[j])) ok = false; }
                        }
                        if (ok) { best = bi; break; }
                    }
                    if (best < 0) continue;     // tidak ketemu jembatan: lubang diabaikan (lebih baik daripada gagal)
                    var res = new List<Vector2>(o.Count + h.Count + 2);
                    for (int i = 0; i <= best; i++) res.Add(o[i]);
                    for (int i = 0; i < h.Count; i++) res.Add(h[(hi + i) % h.Count]);
                    res.Add(h[hi]); res.Add(o[best]);
                    for (int i = best + 1; i < o.Count; i++) res.Add(o[i]);
                    o = res;
                }
                return o;
            }
            static float MaxX(List<Vector2> h) { float m = float.MinValue; foreach (var v in h) if (v.x > m) m = v.x; return m; }

            static bool InTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
            {
                return Cr(a, b, p) > 1e-4f && Cr(b, c, p) > 1e-4f && Cr(c, a, p) > 1e-4f;
            }

            static void EarClip(List<Vector2> p, List<int> tris, int b0)
            {
                var idx = new List<int>(p.Count); for (int i = 0; i < p.Count; i++) idx.Add(i);
                int guard = 0, start = 0;
                while (idx.Count > 3 && guard++ < 200000)
                {
                    int n = idx.Count; bool clipped = false;
                    for (int s = 0; s < n; s++)
                    {
                        int i = (start + s) % n;
                        int a = idx[(i + n - 1) % n], b = idx[i], c = idx[(i + 1) % n];
                        Vector2 A = p[a], B = p[b], C = p[c];
                        if (Cr(A, B, C) <= 1e-4f) continue;
                        bool ok = true;
                        for (int k = 0; k < n && ok; k++)
                        {
                            int v = idx[k]; if (v == a || v == b || v == c) continue;
                            Vector2 P = p[v];
                            if (P == A || P == B || P == C) continue;
                            if (InTri(P, A, B, C)) ok = false;
                        }
                        if (!ok) continue;
                        tris.Add(b0 + a); tris.Add(b0 + b); tris.Add(b0 + c);
                        idx.RemoveAt(i); start = i > 0 ? i - 1 : 0; clipped = true; break;
                    }
                    if (!clipped)
                    {
                        // sudut sejajar / berpotongan sendiri: buang titik yang paling "rata" supaya tidak macet
                        int bi = 0; float bv = float.MaxValue;
                        for (int i = 0; i < n; i++) { float v = Mathf.Abs(Cr(p[idx[(i + n - 1) % n]], p[idx[i]], p[idx[(i + 1) % n]])); if (v < bv) { bv = v; bi = i; } }
                        idx.RemoveAt(bi); start = 0;
                    }
                }
                if (idx.Count == 3 && Cr(p[idx[0]], p[idx[1]], p[idx[2]]) > 1e-4f) { tris.Add(b0 + idx[0]); tris.Add(b0 + idx[1]); tris.Add(b0 + idx[2]); }
            }

            // ------------------------------------------------------------------ gerak hidup (dihitung ulang tiap frame di CPU)
            /// <param name="mode">0 = hewan darat, 1 = bersayap/terbang, 2 = ular/gurita/kura-kura (meliuk)</param>
            public void Pose(Vector3[] v, float width, float height, bool center, bool left, int mode, float clock, float ph, float run, float roar)
            {
                float yo = center ? 0.5f : 0.04f;
                float walk = ph * 1.9f;
                for (int i = 0; i < v.Length; i++)
                {
                    float x = nx[i], y = ny[i];
                    float fx = left ? -x : x;                  // fx > 0 = arah depan makhluk
                    float dx = 0f, dy = 0f;
                    if (mode == 0)
                    {
                        if (y < 0.4f)
                        {
                            float w = (0.4f - y) / 0.4f; w *= w;
                            float sw = Mathf.Sin(walk + x * 9f);
                            dx += sw * 0.05f * run * w;
                            dy += Mathf.Max(0f, Mathf.Sin(walk + x * 9f + 1.5f)) * 0.03f * run * w;
                        }
                        if (fx < -0.28f && y > 0.12f) dy += Mathf.Sin(clock * 3.4f + fx * 8f) * 0.018f * (-0.28f - fx) * 4f;    // ekor
                        if (fx > 0.18f && y > 0.4f) { float t = (fx - 0.18f) * 2.2f; dy += -roar * 0.05f * t + Mathf.Sin(clock * 2.1f) * 0.006f * t; dx += roar * 0.01f * t; }   // kepala
                    }
                    else if (mode == 1)
                    {
                        float up = Mathf.Max(0f, y - 0.42f);
                        float fl = Mathf.Sin(clock * 7.5f);
                        dy += fl * 0.075f * up * (0.35f + Mathf.Abs(x) * 1.7f);
                        dx -= fl * 0.012f * up * (left ? -1f : 1f);
                        if (y < 0.4f) { float w = (0.4f - y) / 0.4f; dx += Mathf.Sin(walk + x * 9f) * 0.03f * run * w * w; }
                        if (fx < -0.3f) dy += Mathf.Sin(clock * 3f + fx * 6f) * 0.012f;
                        if (fx > 0.18f && y > 0.4f) dy += -roar * 0.04f * (fx - 0.18f) * 2.2f;
                    }
                    else
                    {
                        dy += Mathf.Sin(x * 9f - clock * 4.2f) * 0.018f * (0.4f + y);
                        dx += Mathf.Sin(y * 7f + clock * 3f) * 0.012f * (1f - y);
                        dy += roar * 0.03f * y;
                    }
                    v[i] = new Vector3((x + dx) * width, (y + dy - yo) * height, 0f);
                }
            }
        }
    }
}
