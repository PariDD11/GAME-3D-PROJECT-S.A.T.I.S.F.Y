using System;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Seni makhluk mitologi (prosedural): kurva spline halus, pita (ribbon) meruncing untuk anggota badan / leher / ekor / tanduk,
    /// bulu sayap berlapis, tubuh dan kepala bersiluet organik yang diisi cahaya. Setiap makhluk memakai proporsi dan ciri khasnya sendiri.
    /// Koordinat lokal: x = arah hadap, y = atas, kaki di y = 0.
    /// </summary>
    public static partial class SkillSilhouette
    {
        internal sealed class MySpec
        {
            public string id, name, prof, bas, mot, atk, tone;
            public float scale = 1.5f;
            public string[] f = new string[0];
            public bool Has(string k) { return Array.IndexOf(f, k) >= 0; }
        }

        internal struct MyFill { public Vector2[] p; public bool strip; public byte kind; public bool ink; public float sh; }   // kind: 0 poligon, 1 pita, 2 bulu, 3 terang (mata)
        static float myShade = 1f;   // 1 = terang (sisi dekat), <1 = lebih gelap (anggota badan sisi jauh)

        static readonly List<MyFill> myFills = new List<MyFill>();   // isian bercahaya: poligon (kipas dari pusat) atau strip (pasangan kiri/kanan)
        static Vector2 myHead;      // posisi mulut (lokal)
        static Vector2 myTop;       // titik tertinggi

        // ------------------------------------------------------------------ gerak hidup: gelombang tulang punggung, napas, hentakan langkah, condong ke depan
        static Vector2 MyWarpPt(Vector2 p, float t, float ph, float lean, float amp, float bob, float breathe)
        {
            float w = Mathf.Clamp01(p.y / 3f) * Mathf.Clamp01(Mathf.Abs(p.x) / 4f);
            float y = (p.y + amp * Mathf.Sin(0.33f * p.x - t * 4.2f) * w + bob) * breathe;
            float a = -lean, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            return V(p.x * ca - y * sa, p.x * sa + y * ca);
        }

        static void MyWarp(List<Vector2[]> shape, float t, float ph, float lean, string bas)
        {
            bool ground = bas == "q" || bas == "h" || bas == "t";
            float amp = ground ? 0.11f : (bas == "b" ? 0.03f : 0.3f);
            float bob = ground ? 0.09f * Mathf.Abs(Mathf.Sin(ph)) : 0.16f * Mathf.Sin(t * 2.1f);
            float br = 1f + 0.018f * Mathf.Sin(t * 2.3f);
            if (bas == "o" || bas == "m" || bas == "r") { lean = 0f; amp = 0.08f; }
            for (int i = 0; i < shape.Count; i++) { var a = shape[i]; for (int j = 0; j < a.Length; j++) a[j] = MyWarpPt(a[j], t, ph, lean, amp, bob, br); }
            for (int i = 0; i < myFills.Count; i++) { var a = myFills[i].p; for (int j = 0; j < a.Length; j++) a[j] = MyWarpPt(a[j], t, ph, lean, amp, bob, br); }
            myHead = MyWarpPt(myHead, t, ph, lean, amp, bob, br); myTop = MyWarpPt(myTop, t, ph, lean, amp, bob, br);
        }

        // ------------------------------------------------------------------ spline & primitif
        static Vector2 MyCR(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return ((b * 2f) + (c - a) * t + (a * 2f - b * 5f + c * 4f - d) * t2 + (b * 3f - a - c * 3f + d) * t3) * 0.5f;
        }

        static Vector2[] MySpline(Vector2[] c, int sub, bool closed)
        {
            int n = c.Length;
            if (n < 3)
            {
                if (n < 2) return c;
                var r2 = new Vector2[sub * 2 + 1];
                for (int i = 0; i <= sub * 2; i++) r2[i] = c[0] + (c[1] - c[0]) * (i / (float)(sub * 2));
                return r2;
            }
            var r = new List<Vector2>();
            int segs = closed ? n : n - 1;
            for (int i = 0; i < segs; i++)
            {
                Vector2 p0 = c[closed ? (i + n - 1) % n : Mathf.Max(0, i - 1)], p1 = c[i], p2 = c[(i + 1) % n], p3 = c[closed ? (i + 2) % n : Mathf.Min(n - 1, i + 2)];
                for (int k = 0; k < sub; k++) r.Add(MyCR(p0, p1, p2, p3, k / (float)sub));
            }
            r.Add(closed ? c[0] : c[n - 1]);
            return r.ToArray();
        }

        static Vector2 MyRot(Vector2 p, Vector2 o, float a)
        {
            Vector2 d = p - o; float c = Mathf.Cos(a), s = Mathf.Sin(a);
            return o + V(d.x * c - d.y * s, d.x * s + d.y * c);
        }

        static Vector2[] MyXf(Vector2[] pts, Vector2 o, float tilt, float k)
        {
            var r = new Vector2[pts.Length];
            for (int i = 0; i < pts.Length; i++) r[i] = o + MyRot(pts[i] * k, Vector2.zero, tilt);
            return r;
        }

        static void MyAddPoly(Vector2[] pts, bool ink = true, byte kind = 0)
        {
            int n = pts.Length;
            if (n > 1 && (pts[0] - pts[n - 1]).sqrMagnitude < 1e-6f) n--;
            if (n < 3) return;
            var q = new Vector2[n]; Array.Copy(pts, q, n);
            myFills.Add(new MyFill { p = q, strip = false, kind = kind, ink = ink, sh = myShade });
        }

        /// <summary>Bentuk organik tertutup dari titik kendali (spline halus), terisi + garis tepi.</summary>
        static void MyBlob(List<Vector2[]> l, Vector2[] ctrl, bool outline = true, int sub = 4)
        {
            var c = MySpline(ctrl, sub, true);
            MyAddPoly(c, outline);
        }

        /// <summary>Pita meruncing sepanjang kurva (anggota badan, leher, ekor, tanduk, tentakel, ular).</summary>
        static void MyRibbon(List<Vector2[]> l, Vector2[] ctrl, float w0, float w1, float bulge = 0f, bool outline = true, int sub = 5)
        {
            var c = MySpline(ctrl, sub, false);
            int n = c.Length; if (n < 2) return;
            var L = new Vector2[n]; var R = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 t = (c[Mathf.Min(i + 1, n - 1)] - c[Mathf.Max(i - 1, 0)]).normalized; Vector2 nr = V(-t.y, t.x);
                float u = i / (float)(n - 1), w = Mathf.Lerp(w0, w1, u) * (1f + bulge * Mathf.Sin(u * Mathf.PI)) * 0.5f;
                L[i] = c[i] + nr * w; R[i] = c[i] - nr * w;
            }
            var st = new Vector2[n * 2];
            for (int i = 0; i < n; i++) { st[i] = L[i]; st[n + i] = R[i]; }
            myFills.Add(new MyFill { p = st, strip = true, kind = 1, ink = outline, sh = myShade });
        }

        /// <summary>Bulu / daun runcing dari a ke b dengan lebar w dan lengkung.</summary>
        static void MyLeaf(List<Vector2[]> l, Vector2 a, Vector2 b, float w, float curve, bool outline = false)
        {
            const int n = 7;
            Vector2 d = b - a, nr = V(-d.y, d.x).normalized; float len = d.magnitude;
            var L = new Vector2[n]; var R = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)(n - 1), wd = w * Mathf.Pow(Mathf.Sin(Mathf.Pow(u, 0.7f) * Mathf.PI), 0.85f) * 0.5f;
                Vector2 c = a + d * u + nr * (curve * len * Mathf.Sin(u * Mathf.PI));
                L[i] = c + nr * wd; R[i] = c - nr * wd;
            }
            var st = new Vector2[n * 2];
            for (int i = 0; i < n; i++) { st[i] = L[i]; st[n + i] = R[i]; }
            myFills.Add(new MyFill { p = st, strip = true, kind = 2, ink = outline, sh = myShade });
        }

        static Vector2[] MyCircle(Vector2 c, float rx, float ry, int n)
        {
            var r = new Vector2[n + 1];
            for (int i = 0; i <= n; i++) { float a = i * Mathf.PI * 2f / n; r[i] = V(c.x + Mathf.Cos(a) * rx, c.y + Mathf.Sin(a) * ry); }
            return r;
        }

        static void MyDisc(List<Vector2[]> l, Vector2 c, float r, bool outline = true)
        {
            var ci = MyCircle(c, r, r, 12); if (outline) MyAddPoly(ci, true, 0); else MyAddPoly(ci, false, 3);
        }

        static Vector2[] MyArc(Vector2 c, float r, float a0, float a1, int n)
        {
            var p = new Vector2[n + 1];
            for (int i = 0; i <= n; i++) { float a = Mathf.Lerp(a0, a1, i / (float)n) * Mathf.Deg2Rad; p[i] = V(c.x + Mathf.Cos(a) * r, c.y + Mathf.Sin(a) * r); }
            return p;
        }

        static Vector2[] MyZig(Vector2 a, Vector2 b, int n, float amp, float seed)
        {
            var p = new Vector2[n + 1];
            Vector2 d = b - a, nr = V(-d.y, d.x).normalized;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n, o = (i == 0 || i == n) ? 0f : (Mathf.PerlinNoise(seed + i * 1.7f, seed * 0.7f) - 0.5f) * 2f * amp;
                p[i] = a + d * t + nr * o;
            }
            return p;
        }

        static void MyFlame(List<Vector2[]> l, Vector2 root, float h, float ph, int seed)
        {
            float sw = Mathf.Sin(ph * 1.7f + seed) * h * 0.2f;
            MyLeaf(l, root, root + V(sw, h), h * 0.5f, sw / Mathf.Max(0.1f, h) * 0.6f, true);
        }

        static void MyBolt(List<Vector2[]> l, Vector2 a, Vector2 b, float amp, int seed) { l.Add(MyZig(a, b, 6, amp, seed * 3.3f + 1f)); }

        static Vector2 MyIK(Vector2 a, Vector2 t, float l1, float l2, float bend)
        {
            Vector2 d = t - a; float dist = Mathf.Clamp(d.magnitude, 0.01f, l1 + l2 - 0.02f); Vector2 dn = d.normalized;
            float x = (dist * dist + l1 * l1 - l2 * l2) / (2f * dist), h = Mathf.Sqrt(Mathf.Max(0f, l1 * l1 - x * x));
            return a + dn * x + V(-dn.y, dn.x) * (h * bend);
        }

        // ------------------------------------------------------------------ sayap berbulu berlapis (primer, sekunder, penutup)
        static Vector2 MyDir(float deg) { float r = deg * Mathf.Deg2Rad; return V(Mathf.Cos(r), Mathf.Sin(r)); }

        /// <summary>Sayap samping: lengan naik dari punggung, bulu primer mengipas dari pergelangan, bulu sekunder dan penutup menjuntai ke belakang. Hadap = +x.</summary>
        static void MyWingF(List<Vector2[]> l, Vector2 root, float span, float flapDeg, float lean, bool outline = true)
        {
            float upA = 80f + flapDeg;
            Vector2 el = root + MyDir(upA + 30f) * span * 0.2f + V(0f, span * 0.1f);
            Vector2 wr = root + MyDir(upA) * span * 0.52f + V(-lean, 0f);
            // bulu belakang (sekunder) di bawah lengan
            int ns = 8;
            for (int k = 0; k < ns; k++)
            {
                float u = k / (float)(ns - 1);
                Vector2 o = wr + (root - wr) * (0.08f + 0.86f * u);
                float ang = Mathf.Lerp(200f, 248f, u) + flapDeg * 0.3f;
                MyLeaf(l, o, o + MyDir(ang) * span * Mathf.Lerp(0.56f, 0.36f, u), span * 0.115f, -0.03f, outline && (k % 2 == 0));
            }
            // bulu primer: mengipas dari pergelangan
            int np = 9;
            for (int k = 0; k < np; k++)
            {
                float u = k / (float)(np - 1);
                Vector2 o = wr + (el - wr) * (0.12f * u);
                float ang = Mathf.Lerp(100f, 205f, u) + flapDeg * Mathf.Lerp(0.9f, 0.3f, u);
                float ln = span * Mathf.Lerp(0.62f, 0.92f, Mathf.Sin(Mathf.Clamp01(u * 1.25f) * Mathf.PI * 0.62f)) * (1f - 0.18f * u * u);
                MyLeaf(l, o, o + MyDir(ang) * ln, span * 0.12f, -0.035f, outline && (k % 2 == 0));
            }
            // bulu penutup kecil sepanjang lengan
            for (int k = 0; k < 7; k++)
            {
                float u = k / 6f; Vector2 o = root + (wr - root) * (0.1f + 0.85f * u);
                MyLeaf(l, o, o + MyDir(Mathf.Lerp(215f, 190f, u) + flapDeg * 0.3f) * span * 0.2f, span * 0.085f, 0f, false);
            }
            MyRibbon(l, new[] { root, el, wr }, span * 0.1f, span * 0.045f, 0.1f, true, 4);
        }

        /// <summary>Sayap kelelawar / naga: lengan, tulang jari mengipas ke belakang, selaput bergerigi.</summary>
        static void MyBatWing(List<Vector2[]> l, Vector2 root, float span, float height, float flap)
        {
            float f = flap * 14f;
            Vector2 el = root + V(-span * 0.08f, height * 0.85f + f * 0.05f);
            Vector2 wr = el + MyDir(125f + f) * span * 0.32f;
            float[] ang = { 150f + f, 178f + f * 0.7f, 205f + f * 0.4f, 232f + f * 0.2f };
            float[] ln = { 0.95f, 0.9f, 0.78f, 0.6f };
            var tips = new Vector2[4];
            var mem = new List<Vector2> { root, el, wr };
            for (int i = 0; i < 4; i++)
            {
                tips[i] = wr + MyDir(ang[i]) * span * ln[i];
                MyRibbon(l, new[] { wr, (wr + tips[i]) * 0.5f + V(0f, 0.15f), tips[i] }, span * 0.04f, span * 0.012f, 0f, false, 3);
                mem.Add(tips[i]);
                if (i < 3) { Vector2 m = (tips[i] + tips[i + 1]) * 0.5f; mem.Add(m + (wr - m) * 0.22f); }
            }
            mem.Add(root + V(-0.6f, -0.2f));
            var spl = MySpline(mem.ToArray(), 2, true); MyAddPoly(spl);
            MyRibbon(l, new[] { root, el, wr }, height * 0.11f, height * 0.045f, 0.1f, true, 4);
            MyLeaf(l, wr, wr + MyDir(100f) * height * 0.28f, height * 0.08f, 0f, true);
        }

        // ------------------------------------------------------------------ kepala hewan
        static readonly Vector2[] hdCanineU = { V(-0.12f, 0.48f), V(0.22f, 0.62f), V(0.48f, 0.46f), V(0.95f, 0.33f), V(1.12f, 0.26f), V(1.14f, 0.12f), V(1.02f, 0.04f), V(0.7f, 0.0f), V(0.28f, -0.02f), V(0.05f, -0.10f), V(-0.12f, 0.1f) };
        static readonly Vector2[] hdCanineJ = { V(0.22f, -0.02f), V(0.75f, -0.02f), V(1.02f, 0.02f), V(1.0f, -0.12f), V(0.72f, -0.2f), V(0.3f, -0.28f), V(0.05f, -0.22f), V(0.0f, -0.08f) };
        static readonly Vector2[] hdFelineU = { V(-0.1f, 0.5f), V(0.2f, 0.66f), V(0.5f, 0.52f), V(0.82f, 0.3f), V(0.95f, 0.18f), V(0.95f, 0.04f), V(0.8f, -0.02f), V(0.4f, -0.04f), V(0.05f, -0.1f), V(-0.12f, 0.1f) };
        static readonly Vector2[] hdFelineJ = { V(0.2f, -0.02f), V(0.65f, -0.04f), V(0.82f, -0.06f), V(0.78f, -0.2f), V(0.5f, -0.3f), V(0.1f, -0.3f), V(-0.02f, -0.15f) };
        static readonly Vector2[] hdEquineU = { V(-0.1f, 0.55f), V(0.25f, 0.6f), V(0.7f, 0.4f), V(1.15f, 0.22f), V(1.3f, 0.1f), V(1.28f, -0.1f), V(1.1f, -0.16f), V(0.5f, -0.12f), V(0.12f, -0.3f), V(-0.1f, -0.1f) };
        static readonly Vector2[] hdEquineJ = { V(0.35f, -0.12f), V(1.0f, -0.16f), V(1.2f, -0.2f), V(1.15f, -0.32f), V(0.8f, -0.36f), V(0.3f, -0.5f), V(0.1f, -0.4f) };
        static readonly Vector2[] hdBovineU = { V(-0.1f, 0.5f), V(0.2f, 0.6f), V(0.6f, 0.42f), V(1.0f, 0.2f), V(1.12f, 0.06f), V(1.1f, -0.14f), V(0.9f, -0.2f), V(0.4f, -0.18f), V(0.1f, -0.35f), V(-0.1f, -0.1f) };
        static readonly Vector2[] hdBovineJ = { V(0.3f, -0.18f), V(0.9f, -0.2f), V(1.02f, -0.3f), V(0.85f, -0.42f), V(0.4f, -0.5f), V(0.1f, -0.42f) };
        static readonly Vector2[] hdCervineU = { V(-0.1f, 0.45f), V(0.25f, 0.5f), V(0.75f, 0.35f), V(1.15f, 0.16f), V(1.25f, 0.05f), V(1.22f, -0.08f), V(1.0f, -0.12f), V(0.5f, -0.1f), V(0.1f, -0.25f), V(-0.1f, -0.08f) };
        static readonly Vector2[] hdCervineJ = { V(0.35f, -0.1f), V(1.0f, -0.12f), V(1.12f, -0.16f), V(1.05f, -0.26f), V(0.7f, -0.28f), V(0.3f, -0.38f), V(0.12f, -0.3f) };
        static readonly Vector2[] hdRamU = { V(-0.1f, 0.5f), V(0.2f, 0.68f), V(0.55f, 0.55f), V(0.95f, 0.2f), V(1.05f, 0.05f), V(1.0f, -0.14f), V(0.5f, -0.14f), V(0.1f, -0.35f), V(-0.1f, -0.1f) };
        static readonly Vector2[] hdEagleU = { V(-0.1f, 0.4f), V(0.15f, 0.55f), V(0.45f, 0.5f), V(0.65f, 0.38f), V(0.98f, 0.22f), V(1.12f, -0.02f), V(1.08f, -0.25f), V(0.9f, -0.1f), V(0.62f, -0.04f), V(0.3f, -0.06f), V(0.05f, -0.15f), V(-0.12f, 0.05f) };
        static readonly Vector2[] hdEagleJ = { V(0.45f, -0.05f), V(0.85f, -0.08f), V(0.95f, -0.2f), V(0.7f, -0.2f), V(0.4f, -0.22f) };

        /// <summary>Kepala dengan rahang atas dan rahang bawah yang membuka (roar 0..1). o = pangkal tengkorak, hs = ukuran.</summary>
        static void MyHead(List<Vector2[]> l, Vector2 o, string type, float hs, float roar, float tilt, bool teeth, float ear)
        {
            Vector2[] U = hdCanineU, J = hdCanineJ; Vector2 piv = V(0.22f, -0.02f);
            switch (type)
            {
                case "feline": U = hdFelineU; J = hdFelineJ; piv = V(0.2f, -0.02f); break;
                case "equine": U = hdEquineU; J = hdEquineJ; piv = V(0.35f, -0.12f); break;
                case "bovine": U = hdBovineU; J = hdBovineJ; piv = V(0.3f, -0.18f); break;
                case "cervine": U = hdCervineU; J = hdCervineJ; piv = V(0.35f, -0.1f); break;
                case "ram": U = hdRamU; J = hdBovineJ; piv = V(0.3f, -0.18f); break;
                case "eagle": U = hdEagleU; J = hdEagleJ; piv = V(0.45f, -0.05f); break;
            }
            var jaw = new Vector2[J.Length];
            for (int i = 0; i < J.Length; i++) jaw[i] = MyRot(J[i], piv, -roar * 0.6f);
            MyBlob(l, MyXf(U, o, tilt, hs), true, 4);
            MyBlob(l, MyXf(jaw, o, tilt, hs), true, 4);
            Vector2 eye = o + MyRot(V(0.42f, 0.3f) * hs, Vector2.zero, tilt);
            MyDisc(l, eye, 0.085f * hs, false);
            if (ear > 0f)
            {
                Vector2 a = o + MyRot(V(0.02f, 0.5f) * hs, Vector2.zero, tilt), b = a + MyRot(V(-0.28f, 1.0f) * ear, Vector2.zero, tilt);
                MyLeaf(l, a, b, 0.34f * hs * 0.45f + 0.1f, 0.12f, true);
            }
            if (teeth)
            {
                float jr = Mathf.Max(0.12f, roar);
                for (int i = 0; i < 3; i++)
                {
                    float x = 0.62f + i * 0.14f;
                    Vector2 a = o + MyRot(V(x, 0.02f) * hs, Vector2.zero, tilt), b = a + MyRot(V(0.03f, -0.17f - jr * 0.1f) * hs, Vector2.zero, tilt);
                    MyLeaf(l, a, b, 0.05f * hs + 0.05f, 0f, false);
                }
            }
            myHead = o + MyRot(V(1.05f, 0.0f) * hs, Vector2.zero, tilt);
        }

        static void MyLeg(List<Vector2[]> l, Vector2 hip, float len, float phase, float stride, float lift, float thick, float bend, bool hoof, float footLen)
        {
            float sw = Mathf.Sin(phase), lf = Mathf.Max(0f, Mathf.Cos(phase));
            Vector2 foot = V(hip.x + sw * stride, lf * lift + 0.05f);
            Vector2 d = foot - hip; float reach = Mathf.Min(d.magnitude, len * 1.02f);
            foot = hip + d.normalized * reach;
            Vector2 knee = MyIK(hip, foot, len * 0.56f, len * 0.56f, bend);
            MyRibbon(l, new[] { hip, knee, foot }, thick, thick * 0.4f, 0.18f, true, 4);
            if (hoof) MyBlob(l, new[] { foot + V(-thick * 0.25f, 0.4f), foot + V(thick * 0.4f, 0.35f), foot + V(thick * 0.55f, -0.02f), foot + V(-thick * 0.3f, -0.03f) }, true, 3);
            else MyBlob(l, new[] { foot + V(-thick * 0.3f, 0.3f), foot + V(footLen * 0.55f, 0.32f), foot + V(footLen, 0.08f), foot + V(footLen * 0.5f, -0.05f), foot + V(-thick * 0.3f, -0.03f) }, true, 3);
        }

        // ------------------------------------------------------------------ makhluk berkaki empat
        static List<Vector2[]> MyQuad(MySpec sp, float ph, float roar)
        {
            var l = new List<Vector2[]>();
            string p = sp.prof;
            if (p == "sphinx") return MySphinx(sp, ph, roar);
            float L = 3.5f, dep = 2f, H = 2.4f, hump = 0.1f, ndx = 1f, rise = 0.8f, hs = 2f, thick = 0.6f, ear = 0.8f, tilt = 0f, arch = 0f, tuck = 0.35f;
            string ht = "canine"; bool hoof = false, teeth = true;
            switch (p)
            {
                case "wolf": L = 3.4f; dep = 1.7f; H = 2.5f; hump = 0.2f; ndx = 0.9f; rise = 0.5f; hs = 2.4f; thick = 0.62f; ear = 1.0f; tilt = -0.05f; break;
                case "weasel": L = 3.6f; dep = 1.1f; H = 1.2f; hump = 0f; ndx = 1.0f; rise = 0.3f; hs = 1.7f; thick = 0.4f; ear = 0.5f; tilt = -0.1f; tuck = 0.15f; break;
                case "lion": ht = "feline"; L = 3.5f; dep = 2.2f; H = 2.2f; hump = 0.25f; ndx = 0.8f; rise = 0.65f; hs = 2.0f; thick = 0.85f; ear = 0.5f; tilt = -0.1f; break;
                case "chimera": ht = "feline"; L = 3.7f; dep = 2.3f; H = 2.3f; hump = 0.25f; ndx = 0.8f; rise = 0.7f; hs = 2.0f; thick = 0.85f; ear = 0.5f; tilt = -0.1f; break;
                case "horse": ht = "equine"; L = 3.7f; dep = 2.0f; H = 3.5f; hump = 0f; ndx = 1.5f; rise = 2.2f; hs = 2.9f; thick = 0.5f; ear = 0.8f; tilt = -0.7f; arch = 0.5f; hoof = true; teeth = false; break;
                case "hippogriff": ht = "eagle"; L = 3.7f; dep = 2.0f; H = 3.5f; hump = 0f; ndx = 1.5f; rise = 2.0f; hs = 2.4f; thick = 0.5f; ear = 0f; tilt = -0.5f; arch = 0.5f; hoof = true; teeth = false; break;
                case "bull": ht = "bovine"; L = 3.7f; dep = 2.8f; H = 2.2f; hump = 0.55f; ndx = 1.0f; rise = 0.25f; hs = 2.5f; thick = 0.95f; ear = 0.6f; tilt = -0.5f; hoof = true; teeth = false; break;
                case "lamassu": ht = "bovine"; L = 3.8f; dep = 2.7f; H = 2.3f; hump = 0.4f; ndx = 1.0f; rise = 1.4f; hs = 2.2f; thick = 0.95f; ear = 0f; tilt = -0.2f; hoof = true; teeth = false; break;
                case "stag": ht = "cervine"; L = 3.4f; dep = 1.8f; H = 3.5f; hump = 0f; ndx = 1.4f; rise = 2.0f; hs = 2.5f; thick = 0.45f; ear = 0.9f; tilt = -0.55f; arch = 0.4f; hoof = true; teeth = false; break;
                case "qilin": ht = "cervine"; L = 3.5f; dep = 1.9f; H = 3.3f; hump = 0.1f; ndx = 1.3f; rise = 1.6f; hs = 2.6f; thick = 0.5f; ear = 0.5f; tilt = -0.4f; arch = 0.3f; hoof = true; break;
                case "ram": ht = "ram"; L = 2.9f; dep = 2.1f; H = 2.2f; hump = 0f; ndx = 0.9f; rise = 0.8f; hs = 1.9f; thick = 0.6f; ear = 0.6f; tilt = -0.2f; hoof = true; teeth = false; break;
                case "elephant": ht = "equine"; L = 3.9f; dep = 3.7f; H = 3.2f; hump = 0.3f; ndx = 0.9f; rise = 0.2f; hs = 0f; thick = 1.5f; ear = 0f; tilt = -0.2f; hoof = true; teeth = false; break;
                case "baku": ht = "feline"; L = 3.6f; dep = 2.3f; H = 2.3f; hump = 0.35f; ndx = 0.9f; rise = 0.5f; hs = 2.1f; thick = 0.85f; ear = 0.5f; tilt = -0.25f; teeth = false; break;
                case "griffin": ht = "eagle"; L = 3.5f; dep = 2.2f; H = 2.2f; hump = 0.2f; ndx = 0.9f; rise = 1.3f; hs = 2.4f; thick = 0.85f; ear = 0f; tilt = -0.15f; teeth = false; break;
            }
            bool lowhead = sp.Has("lowhead"), human = sp.Has("human");
            float bot = H - dep * 0.3f, top = H + dep * 0.7f, br = Mathf.Sin(ph * 0.6f) * 0.05f;
            // ---- kaki
            int legs = sp.Has("legs8") ? 8 : sp.Has("legs6") ? 6 : sp.Has("legs5") ? 5 : 4;
            int pairs = Mathf.Max(2, legs / 2);
            float stride = H * 0.5f, lift = H * 0.3f, hipY = bot + dep * 0.4f;
            for (int i = 0; i < pairs; i++)
            {
                float x = Mathf.Lerp(L * 0.6f, -L * 0.62f, i / (float)(pairs - 1));
                float bend = (i < pairs / 2 || (pairs == 2 && i == 0)) ? 1f : -1f; if (pairs == 3 && i == 1) bend = 1f;
                float p0 = ph + i * 1.1f;
                myShade = 0.55f; MyLeg(l, V(x - 0.4f, hipY), hipY, p0 + Mathf.PI, stride, lift, thick * 0.9f, bend, hoof, thick * 1.5f); myShade = 1f;
                MyLeg(l, V(x, hipY), hipY, p0, stride, lift, thick, bend, hoof, thick * 1.5f);
            }
            if (legs == 5) MyLeg(l, V(L * 0.15f, hipY), hipY, ph + 0.8f, stride * 0.5f, lift * 0.5f, thick * 0.9f, 1f, hoof, thick * 1.5f);
            // ---- tubuh
            var body = new[] {
                V(-L, top - 0.45f * dep), V(-0.55f * L, top + 0.03f * dep), V(0f, top - 0.02f * dep + br), V(0.55f * L, top + hump * 0.5f * dep + br),
                V(0.95f * L, top - 0.15f * dep), V(1.03f * L, bot + 0.42f * dep), V(0.8f * L, bot - 0.12f * dep), V(0.2f * L, bot + tuck * dep * 0.5f),
                V(-0.4f * L, bot + tuck * dep * 0.55f), V(-0.82f * L, bot + 0.3f * dep), V(-1.06f * L, bot + 0.5f * dep) };
            MyBlob(l, body, true, 4);
            // ---- leher dan kepala
            float hx = L * 0.95f + ndx, hy = top + rise;
            if (lowhead) { hx = L + ndx + 0.5f; hy = 1.5f; tilt = -1.0f; }
            hy += Mathf.Sin(ph * 0.5f - 0.7f) * 0.22f; hx += Mathf.Sin(ph * 0.5f - 1.1f) * 0.12f; tilt += Mathf.Sin(ph * 0.5f - 0.5f) * 0.06f;
            Vector2 headO = V(hx, hy);
            Vector2 shoulder = V(0.6f * L, top - 0.25f * dep);
            Vector2 mid = V(L * 0.85f + ndx * 0.35f, top + rise * 0.5f + arch + Mathf.Sin(ph * 0.5f) * 0.25f);
            if (lowhead) mid = V(L + 0.5f, top - 0.2f);
            MyRibbon(l, new[] { shoulder, mid, headO + V(-0.15f, -0.1f) }, dep * 0.8f, Mathf.Max(0.7f, hs * 0.38f), 0.1f, true, 5);
            Vector2 mouth;
            if (sp.Has("heads3"))
            {
                MyHead(l, headO + V(0f, 0.7f), ht, hs * 0.92f, roar, tilt + 0.75f, true, ear);
                Vector2 m1 = myHead;
                MyHead(l, headO + V(0.1f, -0.1f), ht, hs, roar, tilt, true, ear);
                Vector2 m2 = myHead;
                MyHead(l, headO + V(-0.1f, -0.9f), ht, hs * 0.92f, roar, tilt - 0.75f, true, ear);
                myHead = V((m1.x + m2.x) * 0.5f + 0.3f, m2.y);
                MyRibbon(l, new[] { shoulder + V(0.2f, 0.2f), mid + V(0.2f, 0.7f), headO + V(-0.1f, 0.6f) }, 0.9f, 0.6f, 0.1f, true, 4);
                MyRibbon(l, new[] { shoulder + V(0.2f, -0.4f), mid + V(0.1f, -0.7f), headO + V(-0.2f, -0.9f) }, 0.9f, 0.6f, 0.1f, true, 4);
            }
            else if (human) MyHumanFace(l, headO + V(0.2f, 0.2f), 1.2f, roar, sp);
            else if (p == "elephant") MyElephantHead(l, headO + V(0.2f, 0.1f), roar, ph, sp);
            else if (p == "baku") { MyHead(l, headO, "feline", hs, roar * 0.4f, tilt, false, ear); MyRibbon(l, new[] { headO + V(hs * 0.95f, -0.1f), headO + V(hs * 1.3f, -0.7f), headO + V(hs * 1.4f + Mathf.Sin(ph) * 0.2f, -1.6f) }, 0.9f, 0.45f, 0.2f, true, 4); }
            else MyHead(l, headO, ht, hs, roar, tilt, teeth, ear);
            mouth = myHead;
            Vector2 hb = headO + MyRot(V(0.15f, 0.55f) * hs, Vector2.zero, tilt);
            // ---- tanduk
            if (sp.Has("bullhorn") || p == "bull")
                for (int s = 0; s < 2; s++) { Vector2 b0 = hb + V(-0.35f * s, 0f); MyRibbon(l, new[] { b0, b0 + V(0.1f, 0.8f), b0 + V(0.9f + s * 0.1f, 1.7f), b0 + V(1.6f, 2.0f) }, 0.55f, 0.04f, 0f, true, 4); }
            if (sp.Has("ramhorn"))
            {
                var hc = new Vector2[10];
                for (int i = 0; i < 10; i++) { float t = i / 9f, a = t * 4.8f + 0.4f, r = 1.9f - t * 1.25f; hc[i] = hb + V(-0.3f + Mathf.Cos(a + 1.5f) * r, 0.3f + Mathf.Sin(a + 1.5f) * r - 0.9f); }
                MyRibbon(l, hc, 0.75f, 0.08f, 0.1f, true, 3);
                if (sp.Has("golden")) MyDisc(l, hb + V(-0.3f, 0.3f), 0.35f);
            }
            if (sp.Has("spiral") || sp.Has("onehorn"))
            {
                float hl = sp.Has("onehorn") ? 3.0f : 4.2f;
                Vector2 b0 = hb + V(0.7f, 0.1f), tip = b0 + V(1.9f, hl * 0.78f);
                MyRibbon(l, new[] { b0, (b0 + tip) * 0.5f + V(-0.15f, 0f), tip }, 0.42f, 0.02f, 0f, true, 4);
                if (sp.Has("spiral")) for (int i = 1; i < 8; i++) { float t = i / 8f; Vector2 c = b0 + (tip - b0) * t; l.Add(new[] { c + V(-0.2f * (1f - t), -0.1f), c + V(0.2f * (1f - t), 0.12f) }); }
            }
            if (sp.Has("antler") || sp.Has("tree") || sp.Has("goldantler"))
            {
                float big = sp.Has("tree") ? 1.4f : 1f;
                for (int s = 0; s < 2; s++)
                {
                    Vector2 a0 = hb + V(-0.45f * s, 0.05f * s), m1 = a0 + V(-0.5f * big, 1.5f * big), m2 = a0 + V(-0.35f * big, 2.9f * big), m3 = a0 + V(0.15f * big, 3.9f * big);
                    MyRibbon(l, new[] { a0, m1, m2, m3 }, 0.34f, 0.05f, 0f, true, 4);
                    for (int k = 1; k <= 3; k++)
                    {
                        Vector2 c = k == 1 ? m1 : k == 2 ? m2 : (m2 + m3) * 0.5f;
                        MyRibbon(l, new[] { c, c + V(0.5f * big, 0.4f * big), c + V(0.95f * big, 0.9f * big + k * 0.15f) }, 0.2f, 0.03f, 0f, true, 3);
                    }
                    if (sp.Has("tree")) { MyDisc(l, m3 + V(0.1f, 0.5f), 0.5f); MyDisc(l, m1 + V(1.0f, 0.9f), 0.4f); MyDisc(l, m2 + V(0.9f, 0.9f), 0.4f); }
                }
            }
            if (sp.Has("tusk"))
                for (int s = 0; s < 2; s++) { Vector2 b0 = headO + MyRot(V(0.8f, -0.1f - s * 0.1f) * hs, Vector2.zero, tilt); MyRibbon(l, new[] { b0, b0 + V(0.1f, -1.0f), b0 + V(0.9f, -1.5f) }, 0.4f, 0.04f, 0f, true, 3); }
            // ---- surai
            if (sp.Has("mane"))
            {
                if (p == "lion" || p == "chimera" || p == "griffin")
                {
                    Vector2 c = headO + V(-0.2f, -0.1f);
                    for (int i = 0; i < 16; i++)
                    {
                        float an = Mathf.Lerp(60f, 300f, i / 15f) * Mathf.Deg2Rad, ln = 1.9f + (i % 2) * 0.55f + Mathf.Sin(ph + i) * 0.15f;
                        Vector2 d = V(Mathf.Cos(an), Mathf.Sin(an));
                        MyLeaf(l, c + d * 0.4f, c + d * (0.4f + ln), 0.75f, 0.12f, i % 3 == 0);
                    }
                }
                else
                    for (int i = 0; i < 9; i++)
                    {
                        float u = i / 8f; Vector2 o = mid * (1f - u) + (headO + V(-0.2f, 0.3f)) * u + V(0f, 0.35f);
                        float w = Mathf.Sin(ph * 1.2f + i * 0.7f) * 0.3f;
                        MyLeaf(l, o, o + V(-1.7f, -0.1f + w), 0.55f, 0.18f, i % 3 == 0);
                    }
            }
            // ---- sayap
            if (sp.Has("wing") || p == "griffin" || p == "hippogriff" || p == "lamassu")
            {
                float fl = Mathf.Sin(ph * 0.6f) * 20f;
                MyWingF(l, V(L * 0.22f, top + 0.1f), 7.0f + dep, fl, 0.2f);
                myShade = 0.6f; MyWingF(l, V(L * 0.15f, top - 0.05f), 6.0f + dep, fl * 0.8f - 6f, 0.4f, false); myShade = 1f;
            }
            if (sp.Has("batwing")) { MyBatWing(l, V(L * 0.2f, top + 0.1f), 8.0f, 6.0f, Mathf.Sin(ph * 0.6f)); myShade = 0.6f; MyBatWing(l, V(L * 0.1f, top - 0.1f), 6.5f, 5.0f, Mathf.Sin(ph * 0.6f + 0.7f)); myShade = 1f; }
            // ---- ekor
            Vector2 tr = V(-L * 1.02f, top - 0.45f * dep);
            float sw = Mathf.Sin(ph * 0.8f), sw2 = Mathf.Sin(ph * 0.8f - 1.0f), sw3 = Mathf.Sin(ph * 0.8f - 2.0f);
            if (sp.Has("scorp"))
            {
                var tail = new Vector2[8];
                for (int i = 0; i < 8; i++) { float t = i / 7f; tail[i] = tr + V(-1.2f * Mathf.Sin(t * 2.4f) - t * 0.6f, 0.3f + t * 6.0f + sw * 0.25f * t); }
                MyRibbon(l, tail, 0.9f, 0.4f, 0f, true, 3);
                for (int i = 1; i < 7; i++) l.Add(MyArc(tail[i], 0.45f, 200f, 340f, 4));
                Vector2 st = tail[7];
                MyBlob(l, new[] { st + V(0.1f, 0.2f), st + V(0.7f, 0.0f), st + V(0.55f, -0.5f), st + V(-0.2f, -0.4f) }, true, 3);
                MyLeaf(l, st + V(0.6f, -0.2f), st + V(1.7f, -1.0f), 0.3f, 0.1f, true);
                myTop = st;
            }
            else if (sp.Has("snaketail"))
            {
                var tail = new Vector2[10];
                for (int i = 0; i < 10; i++) { float t = i / 9f; tail[i] = tr + V(-t * 5.4f, 0.2f + Mathf.Sin(t * 7f + ph) * 0.8f * t + t * 1.2f); }
                MyRibbon(l, tail, 0.9f, 0.4f, 0f, true, 3);
                Vector2 h2 = tail[9]; MyBlob(l, new[] { h2 + V(0.2f, 0.4f), h2 + V(-0.5f, 0.5f), h2 + V(-1.4f, 0.2f), h2 + V(-0.5f, -0.3f) }, true, 3);
            }
            else if (p == "wolf" || sp.Has("bushy"))
                MyRibbon(l, new[] { tr, tr + V(-1.3f, -0.1f + sw * 0.4f), tr + V(-2.6f, -0.7f + sw2 * 0.8f), tr + V(-3.6f, -1.8f + sw3 * 1.1f) }, 1.0f, 0.15f, 0.7f, true, 4);
            else if (p == "horse" || p == "hippogriff" || p == "qilin" || p == "stag")
                MyRibbon(l, new[] { tr, tr + V(-1.1f, -0.5f), tr + V(-1.9f - sw2 * 0.4f, -1.7f + sw2 * 0.3f), tr + V(-2.3f - sw3 * 0.9f, -3.1f) }, 0.8f, 0.1f, 0.5f, true, 4);
            else
            {
                MyRibbon(l, new[] { tr, tr + V(-1.3f, -0.5f + sw * 0.4f), tr + V(-2.3f - sw2 * 0.3f, -1.6f + sw2 * 0.5f), tr + V(-2.5f - sw3 * 0.9f, -2.8f + sw3 * 0.3f) }, 0.4f, 0.2f, 0f, true, 4);
                if (p == "lion" || p == "chimera" || p == "baku") MyBlob(l, new[] { tr + V(-2.3f, -2.6f), tr + V(-2.0f, -3.4f), tr + V(-2.6f, -4.0f), tr + V(-3.0f, -3.2f) }, true, 3);
            }
            if (sp.Has("goathead"))
            {
                Vector2 gb = V(0.15f * L, top + 0.6f);
                MyRibbon(l, new[] { gb + V(0f, -0.3f), gb + V(0.2f, 0.8f), gb + V(0.3f, 1.6f) }, 0.9f, 0.6f, 0f, true, 3);
                MyHead(l, gb + V(0.3f, 1.6f), "ram", 1.5f, roar * 0.6f, 0.35f, false, 0.5f);
                MyRibbon(l, new[] { gb + V(0.2f, 2.3f), gb + V(-0.6f, 3.0f), gb + V(-1.3f, 2.6f), gb + V(-1.2f, 1.9f) }, 0.4f, 0.05f, 0f, true, 3);
            }
            if (sp.Has("shell"))
            {
                var sh = new List<Vector2> { V(-L * 0.9f, top - 0.2f) };
                for (int i = 0; i <= 8; i++) { float a = Mathf.Lerp(170f, 10f, i / 8f) * Mathf.Deg2Rad; sh.Add(V(Mathf.Cos(a) * L * 0.95f, top - 0.2f + Mathf.Sin(a) * dep * 1.1f)); }
                sh.Add(V(L * 0.9f, top - 0.2f));
                var shp = MySpline(sh.ToArray(), 3, true); MyAddPoly(shp);
                for (int i = -3; i <= 3; i++) l.Add(new[] { V(i * L * 0.3f, top - 0.2f), V(i * L * 0.24f, top - 0.2f + dep * 1.0f * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow(i / 3.4f, 2f)))) });
                for (int i = -2; i <= 2; i++) { Vector2 b0 = V(i * L * 0.36f, top - 0.2f + dep * 0.95f * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow(i / 2.8f, 2f)))); MyLeaf(l, b0, b0 + V(i * 0.1f, 1.6f), 0.7f, 0f, true); }
            }
            if (sp.Has("spikes"))
                for (int i = 0; i < 6; i++) { float x = Mathf.Lerp(L * 0.45f, -L * 0.95f, i / 5f); Vector2 b0 = V(x, top + 0.05f * dep); MyLeaf(l, b0, b0 + V(-0.35f, 1.5f + (i % 2) * 0.5f), 0.75f, 0.05f, true); }
            if (sp.Has("armor"))
                for (int i = 0; i < 6; i++) { float x = Mathf.Lerp(-L * 0.8f, L * 0.7f, i / 5f); l.Add(MyArc(V(x, top - 0.1f * dep), dep * 0.55f, 200f, 340f, 5)); }
            if (sp.Has("sickle"))
                for (int s = 0; s < 2; s++) { Vector2 w0 = V(L * 0.55f - s * 0.4f, 0.8f); MyRibbon(l, new[] { w0, w0 + V(1.2f, 1.0f), w0 + V(1.5f, 2.3f), w0 + V(0.5f, 3.0f) }, 0.55f, 0.04f, 0f, true, 3); }
            if (sp.Has("bolt"))
            {
                for (int i = 0; i < 4; i++) MyBolt(l, V(Mathf.Lerp(-L * 0.7f, L * 0.6f, i / 3f), top + 0.3f), V(Mathf.Lerp(-L * 0.7f, L * 0.6f, i / 3f) + 0.8f, bot - 0.2f), 0.5f, i + (int)(ph * 2f));
                MyBolt(l, V(-L, top), V(-L - 3.5f, top - 1.5f), 0.6f, (int)(ph * 3f)); MyBolt(l, headO + V(0f, 1f), headO + V(1.5f, 3.2f), 0.5f, (int)(ph * 3f) + 5);
            }
            if (sp.Has("flame"))
            {
                for (int i = 0; i < 7; i++) MyFlame(l, V(Mathf.Lerp(-L * 0.9f, L * 0.55f, i / 6f), top + 0.1f + (i == 3 ? hump : 0f)), 1.7f + (i % 2) * 0.6f, ph, i);
                MyFlame(l, headO + V(0f, 0.6f * hs), 1.8f, ph, 9); MyFlame(l, tr + V(-1f, -0.5f), 1.8f, ph, 11);
                if (hoof) for (int i = 0; i < 4; i++) MyFlame(l, V(i * 1.7f - 2.5f, 0.1f), 0.9f, ph, 20 + i);
            }
            if (sp.Has("chain"))
            {
                Vector2 c0 = headO + V(-0.2f, -hs * 0.3f);
                for (int i = 0; i < 5; i++) l.Add(MyCircle(c0 + V(-0.2f - i * 0.15f, -0.5f - i * 0.7f + Mathf.Sin(ph + i) * 0.1f), 0.3f, 0.42f, 7));
                for (int i = 0; i < 6; i++) l.Add(MyCircle(tr + V(-2.0f - i * 1.0f, -1.2f - (i % 2) * 0.5f + Mathf.Sin(ph + i * 0.7f) * 0.25f), 0.42f, 0.28f, 7));
            }
            if (sp.Has("sun"))
            {
                Vector2 c = mouth + V(2.7f, 1.2f);
                MyDisc(l, c, 1.5f);
                for (int i = 0; i < 12; i++) { float a = i * Mathf.PI * 2f / 12f + ph * 0.2f; Vector2 d = V(Mathf.Cos(a), Mathf.Sin(a)); MyLeaf(l, c + d * 1.7f, c + d * (2.5f + (i % 2) * 0.5f), 0.3f, 0f, false); }
            }
            if (sp.Has("blood")) for (int i = 0; i < 4; i++) MyLeaf(l, mouth + V(-0.4f - i * 0.45f, -0.2f), mouth + V(-0.4f - i * 0.45f, -1.3f - i * 0.25f - Mathf.Abs(Mathf.Sin(ph + i)) * 0.4f), 0.22f, 0f, false);
            if (sp.Has("halo"))
            {
                Vector2 c = headO + V(-0.3f, 2.8f);
                l.Add(MyCircle(c, 1.7f, 0.5f, 16)); l.Add(MyCircle(c, 1.3f, 0.38f, 16));
                for (int i = 0; i < 10; i++) { float a = i * Mathf.PI * 2f / 10f; Vector2 d = V(Mathf.Cos(a) * 1.9f, Mathf.Sin(a) * 0.6f); MyLeaf(l, c + d, c + d * 1.5f + V(0f, 0.3f), 0.2f, 0f, false); }
            }
            if (sp.Has("wool"))
                for (int i = 0; i < 10; i++) { float x = Mathf.Lerp(-L * 0.9f, L * 0.75f, i / 9f); MyDisc(l, V(x, top - 0.25f * dep), 0.7f, true); MyDisc(l, V(x * 0.92f + 0.2f, bot + dep * 0.45f), 0.65f, i % 2 == 0); }
            if (sp.Has("scales"))
                for (int i = 0; i < 8; i++) { float x = Mathf.Lerp(-L * 0.85f, L * 0.7f, i / 7f); l.Add(MyArc(V(x, top - 0.6f * dep), 0.6f, 190f, 350f, 5)); l.Add(MyArc(V(x + 0.4f, top - 0.95f * dep), 0.6f, 190f, 350f, 5)); }
            if (sp.Has("goldaura")) { for (int i = 0; i < 8; i++) { float a = i * Mathf.PI * 2f / 8f; Vector2 c0 = V(0f, H); MyLeaf(l, c0 + V(Mathf.Cos(a) * (L + 0.5f), Mathf.Sin(a) * (dep + 1.5f)), c0 + V(Mathf.Cos(a) * (L + 1.8f), Mathf.Sin(a) * (dep + 2.8f)), 0.4f, 0f, false); } }
            if (!sp.Has("scorp")) myTop = V(0f, top + 3f);
            myHead = mouth;
            return l;
        }

        // ------------------------------------------------------------------ wajah manusia dan kepala gajah
        static void MyHumanFace(List<Vector2[]> l, Vector2 c, float r, float roar, MySpec sp)
        {
            // hiasan kepala di belakang wajah
            if (sp.Has("nemes"))
            {
                MyBlob(l, new[] { c + V(0.55f, 0.85f) * r, c + V(-0.2f, 1.3f) * r, c + V(-1.05f, 0.8f) * r, c + V(-1.6f, -0.9f) * r, c + V(-1.3f, -1.9f) * r, c + V(-0.55f, -1.2f) * r, c + V(-0.4f, 0.0f) * r, c + V(0.2f, 0.65f) * r }, true, 4);
                for (int i = 0; i < 5; i++) { float u = i / 4f; l.Add(new[] { c + V(Mathf.Lerp(-0.1f, -1.0f, u), 1.2f - u * 0.4f) * r, c + V(Mathf.Lerp(-0.4f, -1.4f, u), -1.1f - u * 0.5f) * r }); }
            }
            Vector2[] prof = {
                c + V(-0.85f, 0.2f) * r, c + V(-0.6f, 0.85f) * r, c + V(-0.05f, 1.05f) * r, c + V(0.5f, 0.88f) * r, c + V(0.72f, 0.5f) * r, c + V(0.7f, 0.2f) * r,
                c + V(0.98f, -0.14f) * r, c + V(0.74f, -0.22f) * r, c + V(0.8f, -0.36f) * r, c + V(0.72f, -0.5f - roar * 0.12f) * r, c + V(0.74f, -0.85f - roar * 0.12f) * r,
                c + V(0.25f, -1.0f - roar * 0.1f) * r, c + V(-0.35f, -0.9f) * r, c + V(-0.8f, -0.45f) * r };
            MyBlob(l, prof, true, 4);
            // mata almond, alis, telinga, bibir
            MyLeaf(l, c + V(0.28f, 0.3f) * r, c + V(0.62f, 0.34f) * r, 0.2f * r, 0.15f, true);
            MyDisc(l, c + V(0.46f, 0.32f) * r, 0.05f * r, false);
            l.Add(new[] { c + V(0.2f, 0.52f) * r, c + V(0.45f, 0.58f) * r, c + V(0.68f, 0.5f) * r });
            l.Add(MyArc(c + V(-0.18f, 0.0f) * r, 0.22f * r, 60f, 300f, 6));
            l.Add(new[] { c + V(0.3f, -0.42f) * r, c + V(0.62f, -0.44f - roar * 0.1f) * r });
            if (sp.Has("beard"))
            {
                MyBlob(l, new[] { c + V(0.7f, -0.75f) * r, c + V(0.95f, -1.5f) * r, c + V(0.6f, -2.5f) * r, c + V(0.15f, -2.2f) * r, c + V(-0.2f, -1.0f) * r }, true, 3);
                for (int i = 0; i < 4; i++) l.Add(new[] { c + V(0.7f - i * 0.17f, -0.9f) * r, c + V(0.55f - i * 0.12f, -2.0f - (i % 2) * 0.2f) * r });
            }
            if (sp.Has("crown"))
            {
                MyBlob(l, new[] { c + V(-0.5f, 0.9f) * r, c + V(-0.6f, 2.0f) * r, c + V(0.0f, 2.2f) * r, c + V(0.6f, 2.0f) * r, c + V(0.6f, 0.9f) * r }, true, 3);
                for (int i = 0; i < 3; i++) l.Add(MyArc(c + V(0.0f, 1.2f + i * 0.35f) * r, 0.75f * r, 190f, 350f, 5));
                for (int s = -1; s <= 1; s += 2) MyRibbon(l, new[] { c + V(0.0f + s * 0.5f, 1.6f) * r, c + V(s * 1.0f, 1.9f) * r, c + V(s * 1.15f, 2.7f) * r }, 0.35f * r, 0.04f, 0f, true, 3);
            }
            myHead = c + V(0.85f, -0.45f) * r;
        }

        static void MyElephantHead(List<Vector2[]> l, Vector2 c, float roar, float ph, MySpec sp)
        {
            MyBlob(l, new[] { c + V(-1.3f, 1.0f), c + V(0.0f, 1.9f), c + V(1.3f, 1.2f), c + V(1.5f, 0.0f), c + V(1.1f, -1.1f), c + V(0.1f, -1.5f), c + V(-1.2f, -0.8f) }, true, 4);
            var trunk = new Vector2[7];
            for (int i = 0; i < 7; i++) { float t = i / 6f; trunk[i] = c + V(1.3f + Mathf.Sin(t * 2.4f + ph * 0.5f) * 0.5f + t * 0.9f, -0.2f - t * 3.7f + (t > 0.7f ? (t - 0.7f) * 4f * (0.5f + roar) : 0f)); }
            MyRibbon(l, trunk, 1.15f, 0.4f, 0f, true, 4);
            for (int i = 1; i < 6; i++) l.Add(MyArc(trunk[i], 0.55f, 200f, 340f, 4));
            MyBlob(l, new[] { c + V(-0.6f, 1.4f), c + V(-2.6f, 2.2f), c + V(-3.2f, -0.2f), c + V(-1.8f, -1.8f), c + V(-0.6f, -0.6f) }, true, 4);
            for (int s = -1; s <= 1; s += 2) MyRibbon(l, new[] { c + V(0.9f + s * 0.25f, -0.55f), c + V(1.8f + s * 0.3f, -1.4f), c + V(3.2f, -0.9f + s * 0.2f) }, 0.4f, 0.04f, 0f, true, 3);
            if (sp.Has("tusk4")) MyRibbon(l, new[] { c + V(0.6f, -0.9f), c + V(1.4f, -2.1f), c + V(2.9f, -2.4f) }, 0.4f, 0.04f, 0f, true, 3);
            MyDisc(l, c + V(0.9f, 0.5f), 0.12f);
            myHead = c + V(2.3f, -3.0f);
        }

        // ------------------------------------------------------------------ Sphinx berbaring
        static List<Vector2[]> MySphinx(MySpec sp, float ph, float roar)
        {
            var l = new List<Vector2[]>();
            float br = Mathf.Sin(ph * 0.6f) * 0.06f;
            MyRibbon(l, new[] { V(3.2f, 1.4f), V(5.0f, 0.5f), V(7.0f, 0.3f) }, 1.2f, 0.9f, 0.1f, true, 4);
            MyRibbon(l, new[] { V(2.6f, 1.2f), V(4.6f, 0.2f), V(6.6f, 0.1f) }, 1.0f, 0.8f, 0.1f, true, 4);
            for (int i = 0; i < 3; i++) l.Add(new[] { V(6.6f + i * 0.4f, 0.2f), V(7.3f + i * 0.4f, 0.0f) });
            MyBlob(l, new[] { V(-4.2f, 1.6f), V(-3.6f, 3.1f), V(-1.0f, 3.4f + br), V(1.8f, 3.5f), V(3.2f, 3.6f), V(3.6f, 2.2f), V(3.0f, 0.6f), V(0.5f, 0.2f), V(-2.8f, 0.1f), V(-4.2f, 0.6f) }, true, 4);
            MyBlob(l, new[] { V(-3.9f, 0.4f), V(-3.4f, 2.2f), V(-1.8f, 2.6f), V(-0.4f, 1.6f), V(-0.8f, 0.2f), V(-2.8f, 0.1f) }, true, 4);
            MyRibbon(l, new[] { V(-4.2f, 0.7f), V(-6.0f, 0.3f), V(-7.0f, 1.4f + Mathf.Sin(ph) * 0.3f), V(-6.4f, 2.5f) }, 0.5f, 0.15f, 0f, true, 4);
            MyRibbon(l, new[] { V(2.6f, 3.0f), V(3.3f, 4.4f), V(3.8f, 5.9f) }, 2.0f, 1.3f, 0.1f, true, 4);
            MyHumanFace(l, V(4.2f, 7.1f), 1.25f, roar, sp);
            MyWingF(l, V(0.8f, 3.5f), 8.5f, Mathf.Sin(ph * 0.6f) * 12f, 0.2f);
            myTop = V(2f, 12f);
            return l;
        }
    }
}
