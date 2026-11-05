using System;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Sprite HUD gaya "bingkai logam ornamen + kristal" yang digambar prosedural (putih/abu metalik, beralpha) supaya
    /// bisa diwarnai ulang lewat Image.color mengikuti warna job. Tidak butuh aset gambar.
    /// </summary>
    public static class HudArt
    {
        public enum Glyph { Sword, Bow, Flame, Rock, Drop, Wind, Cross, Skull, Person, Star, Bag, Scroll, Book, Gear, Shield, Diamond, Sun, Moon, Cloud }

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static Sprite white;

        static Sprite Mk(string key, int w, int h, Func<int, int, Color32> f, Vector4 border, bool linear = false)
        {
            Sprite s;
            if (cache.TryGetValue(key, out s) && s != null) return s;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) px[y * w + x] = f(x, y);
            tex.SetPixels32(px);
            tex.wrapMode = TextureWrapMode.Clamp; tex.filterMode = FilterMode.Bilinear; tex.hideFlags = HideFlags.HideAndDontSave;
            tex.Apply(false, true);
            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            cache[key] = s;
            return s;
        }

        static Color32 G(float v, float a) { byte b = (byte)(Mathf.Clamp01(v) * 255f); return new Color32(b, b, b, (byte)(Mathf.Clamp01(a) * 255f)); }
        static float Soft(float d, float aa = 1.2f) { return Mathf.Clamp01(0.5f - d / aa); }   // tepi halus: d<0 di dalam

        /// <summary>Kotak persegi dengan sudut terpotong. Mengembalikan jarak masuk dari tepi (negatif di luar).</summary>
        static float BoxD(int x, int y, int w, int h, float cut)
        {
            float u = Mathf.Min(x, w - 1 - x), v = Mathf.Min(y, h - 1 - y);
            float d = Mathf.Min(u, v);
            if (u + v < cut * 2f) d = Mathf.Min(d, (u + v - cut) / 1.4142f);
            return d;
        }

        public static Sprite White { get { return white != null ? white : white = Mk("white", 4, 4, (x, y) => G(1f, 1f), Vector4.zero); } }

        // ------------------------------------------------------------------ panel & bar
        /// <summary>Isi panel (gelap, diwarnai). Sudut terpotong cut piksel.</summary>
        public static Sprite PanelFill(int n = 96, int cut = 12, int border = 32)
        {
            return Mk("pf" + n + "_" + cut, n, n, (x, y) =>
            {
                float d = BoxD(x, y, n, n, cut);
                float t = (float)y / n;
                float gloss = 0.55f + 0.45f * t * t;                                   // atas lebih terang (kaca)
                if (y > n * 0.86f && d > 3f) gloss += 0.25f;                           // pita kilau tipis di tepi atas
                return G(gloss, Soft(-(d - 2.4f)));
            }, new Vector4(border, border, border, border));
        }

        /// <summary>Bingkai logam: garis luar terang, garis dalam redup, berlian dan titik hias di sudut.</summary>
        public static Sprite PanelTrim(int n = 96, int cut = 12, int border = 32, bool ornate = true)
        {
            return Mk("pt" + n + "_" + cut + (ornate ? "o" : ""), n, n, (x, y) =>
            {
                float u = Mathf.Min(x, n - 1 - x), v = Mathf.Min(y, n - 1 - y);
                float d = BoxD(x, y, n, n, cut);
                float t = (float)y / n;
                float bright = 0.62f + 0.38f * t;
                float a = 0f, val = bright;
                if (d >= 0f && d < 2.4f) { a = 1f; val = bright; }
                else if (d >= 2.4f && d < 3.6f) { a = 0.75f; val = 0.28f; }
                if (ornate)
                {
                    if (d >= 6.5f && d < 7.5f) { a = Mathf.Max(a, 0.55f); val = 0.7f * bright; }
                    float cx = cut + 2f;
                    if (Mathf.Abs(u - cx) + Mathf.Abs(v - cx) < 4.8f) { a = 1f; val = 1f; }
                    if (Mathf.Abs(u - cx) + Mathf.Abs(v - cx) < 2.2f) { a = 1f; val = 0.35f; }
                    if ((Mathf.Abs(u - cx - 12f) < 1.6f && Mathf.Abs(v - 3.2f) < 1.4f) || (Mathf.Abs(v - cx - 12f) < 1.6f && Mathf.Abs(u - 3.2f) < 1.4f)) { a = 1f; val = 1f; }
                }
                return G(val, a);
            }, new Vector4(border, border, border, border));
        }

        /// <summary>Bar: bingkai tipis dengan ujung runcing (cut besar).</summary>
        public static Sprite BarTrim() { return PanelTrim(48, 10, 16, false); }
        public static Sprite BarFill() { return PanelFill(48, 10, 16); }

        // ------------------------------------------------------------------ heksagon (potret) & berlian (status)
        static float HexD(float x, float y, float r) { x = Mathf.Abs(x); y = Mathf.Abs(y); return Mathf.Max(x, x * 0.5f + y * 0.8660254f) - r; }

        public static Sprite HexRing(int n = 128)
        {
            return Mk("hxr" + n, n, n, (px, py) =>
            {
                float x = px - n * 0.5f + 0.5f, y = py - n * 0.5f + 0.5f, r = n * 0.5f - 3f;
                float d = HexD(x, y, r);
                float t = (float)py / n;
                float a = 0f, val = 0.6f + 0.4f * t;
                if (d > -6f && d <= 0f) a = Soft(-d - 0f) * 0.0f + (d > -5.2f ? 1f : Soft(d + 5.2f - 0.5f));
                if (d > -10.5f && d <= -9.2f) { a = Mathf.Max(a, 0.6f); val *= 0.6f; }
                // kristal di enam sudut
                for (int i = 0; i < 6; i++)
                {
                    float ang = (30f + i * 60f) * Mathf.Deg2Rad; float vx = Mathf.Cos(ang) * (r - 1f), vy = Mathf.Sin(ang) * (r - 1f);
                    if (Mathf.Abs(x - vx) + Mathf.Abs(y - vy) < 5.2f) { a = 1f; val = 1f; }
                }
                return G(val, a);
            }, Vector4.zero);
        }

        public static Sprite HexFill(int n = 128)
        {
            return Mk("hxf" + n, n, n, (px, py) =>
            {
                float x = px - n * 0.5f + 0.5f, y = py - n * 0.5f + 0.5f, r = n * 0.5f - 3f;
                float d = HexD(x, y, r);
                return G(0.75f + 0.25f * ((float)py / n), Soft(d + 5.5f));
            }, Vector4.zero);
        }

        public static Sprite DiamondRing(int n = 64)
        {
            return Mk("dmr" + n, n, n, (px, py) =>
            {
                float x = px - n * 0.5f + 0.5f, y = py - n * 0.5f + 0.5f, r = n * 0.5f - 2f;
                float d = Mathf.Abs(x) + Mathf.Abs(y) - r;
                float t = (float)py / n;
                float a = 0f, val = 0.65f + 0.35f * t;
                if (d > -4.4f && d <= 0f) a = 1f;
                if (d > -7.5f && d <= -6.4f) { a = 0.55f; val *= 0.55f; }
                if (Mathf.Abs(x) + Mathf.Abs(y - r) < 4f || Mathf.Abs(x) + Mathf.Abs(y + r) < 4f) { a = 1f; val = 1f; }
                return G(val, a);
            }, Vector4.zero);
        }

        public static Sprite DiamondFill(int n = 64)
        {
            return Mk("dmf" + n, n, n, (px, py) =>
            {
                float x = px - n * 0.5f + 0.5f, y = py - n * 0.5f + 0.5f, r = n * 0.5f - 2f;
                float d = Mathf.Abs(x) + Mathf.Abs(y) - r;
                return G(1f, Soft(d + 5f));
            }, Vector4.zero);
        }

        // ------------------------------------------------------------------ cincin besar slot ultimate
        public static Sprite BigRing(int n = 256)
        {
            return Mk("bgr" + n, n, n, (px, py) =>
            {
                float x = px - n * 0.5f + 0.5f, y = py - n * 0.5f + 0.5f, r = Mathf.Sqrt(x * x + y * y), R = n * 0.5f - 6f;
                float t = (float)py / n, val = 0.6f + 0.4f * t, a = 0f;
                if (r < R && r > R - 9f) a = 1f;
                if (r < R - 15f && r > R - 18f) { a = 0.7f; val *= 0.7f; }
                float ang = Mathf.Atan2(y, x);
                for (int i = 0; i < 8; i++)
                {
                    float aa = i * Mathf.PI / 4f + Mathf.PI / 8f; float sx = Mathf.Cos(aa) * (R - 4.5f), sy = Mathf.Sin(aa) * (R - 4.5f);
                    float dd = Mathf.Sqrt((x - sx) * (x - sx) + (y - sy) * (y - sy));
                    if (dd < 7f) { a = 1f; val = 1f; }
                    float ox = Mathf.Cos(aa) * (R + 1f), oy = Mathf.Sin(aa) * (R + 1f);
                    if (Mathf.Abs(x - ox) + Mathf.Abs(y - oy) < 9f) { a = 1f; val = 1f; }
                }
                return G(val, a);
            }, Vector4.zero);
        }

        public static Sprite DiscFill(int n = 256)
        {
            return Mk("dsf" + n, n, n, (px, py) =>
            {
                float x = px - n * 0.5f + 0.5f, y = py - n * 0.5f + 0.5f, r = Mathf.Sqrt(x * x + y * y), R = n * 0.5f - 12f;
                return G(0.8f + 0.2f * ((float)py / n), Soft(r - R));
            }, Vector4.zero);
        }

        public static Sprite Glow(int n = 128)
        {
            return Mk("glw" + n, n, n, (px, py) =>
            {
                float x = (px - n * 0.5f + 0.5f) / (n * 0.5f), y = (py - n * 0.5f + 0.5f) / (n * 0.5f), r = Mathf.Sqrt(x * x + y * y);
                float a = Mathf.Clamp01(1f - r); a = a * a * (3f - 2f * a);
                return G(1f, a);
            }, Vector4.zero);
        }

        /// <summary>Ornamen penutup di tepi atas bar skill: berlian tengah + dua bilah meruncing + titik ujung.</summary>
        public static Sprite Crest()
        {
            const int W = 320, H = 44;
            return Mk("crest2", W, H, (px, py) =>
            {
                float x = Mathf.Abs(px + 0.5f - W * 0.5f), y = py + 0.5f - H * 0.5f;
                float a = 0f, val = 0.8f + 0.2f * (py / (float)H);
                float cd = x * 1.0f + Mathf.Abs(y) * 1.0f;
                if (cd < 19f) { a = 1f; val = 1f; }
                if (cd < 11f) { a = 1f; val = 0.28f; }
                if (cd < 4f) { a = 1f; val = 1f; }
                if (x > 24f && x < 150f)
                {
                    float th = Mathf.Lerp(4.5f, 0.9f, (x - 24f) / 126f);
                    if (Mathf.Abs(y) < th) { a = 1f; val = 0.95f; }
                    if (x > 34f && x < 118f && Mathf.Abs(Mathf.Abs(y) - 8f) < 0.9f) { a = 0.55f; val = 0.8f; }
                }
                if (Mathf.Abs(x - 156f) + Mathf.Abs(y) < 5f) { a = 1f; val = 1f; }
                return G(val, a);
            }, Vector4.zero);
        }

        /// <summary>Gradien vertikal hitam: atas pekat -> bawah transparan (dibalik lewat scale -1).</summary>
        public static Sprite Gradient()
        {
            return Mk("grad", 4, 64, (px, py) => { float t = py / 63f; return G(0f, t * t); }, Vector4.zero);
        }

        /// <summary>Garis pembagi tick bar: dipakai Image tiled di atas isi bar.</summary>
        public static Sprite BarTick()
        {
            return Mk("tick", 20, 8, (px, py) => G(0f, px >= 18 ? 0.45f : 0f), Vector4.zero);
        }

        // ------------------------------------------------------------------ ikon (glyph)
        static bool In(float x, float y, float cx, float cy, float hw, float hh) { return Mathf.Abs(x - cx) < hw && Mathf.Abs(y - cy) < hh; }
        static float Ring(float x, float y, float cx, float cy, float r, float th) { float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)); return Mathf.Abs(d - r) < th ? 1f : 0f; }
        static float Disc(float x, float y, float cx, float cy, float r) { float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)); return d < r ? 1f : 0f; }

        public static Sprite GlyphSprite(Glyph g)
        {
            const int N = 96;
            return Mk("gl" + g, N, N, (px, py) =>
            {
                float x = (px + 0.5f - N * 0.5f) / (N * 0.5f), y = (py + 0.5f - N * 0.5f) / (N * 0.5f);   // -1..1, y naik
                float a = 0f;
                switch (g)
                {
                    case Glyph.Sword:
                        a = In(x, y, 0f, 0.28f, 0.1f, 0.62f) ? 1f : 0f;
                        if (In(x, y, 0f, -0.28f, 0.42f, 0.07f)) a = 1f;
                        if (In(x, y, 0f, -0.55f, 0.07f, 0.24f)) a = 1f;
                        if (Disc(x, y, 0f, -0.82f, 0.12f) > 0f) a = 1f;
                        if (Mathf.Abs(x) < 0.1f - (y - 0.7f) * 0.5f && y > 0.7f && y < 0.9f) a = 1f;
                        if (In(x, y, 0f, 0.3f, 0.025f, 0.5f)) a = 0.4f;
                        break;
                    case Glyph.Bow:
                        a = Ring(x, y, -0.55f, 0f, 1.05f, 0.075f) > 0f && x > -0.1f ? 1f : 0f;
                        if (In(x, y, 0.38f, 0f, 0.02f, 0.78f)) a = 0.8f;
                        if (In(x, y, 0.05f, 0f, 0.72f, 0.04f)) a = 1f;
                        if (Mathf.Abs(y) < 0.2f * (0.95f - x) && x > 0.62f && x < 0.95f) a = 1f;
                        if (In(x, y, -0.62f, 0f, 0.06f, 0.2f) ) a = 1f;
                        break;
                    case Glyph.Flame:
                        {
                            float t = (y + 0.9f) / 1.8f; if (t < 0f || t > 1f) break;
                            float w = 0.55f * Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.7f)), 0.9f);
                            float bend = 0.18f * Mathf.Sin(t * 5f) * t;
                            a = Mathf.Abs(x - bend) < w ? 1f : 0f;
                            float t2 = (y + 0.7f) / 1.0f;
                            if (t2 > 0f && t2 < 1f && Mathf.Abs(x - bend * 0.5f) < 0.22f * Mathf.Sin(Mathf.PI * Mathf.Pow(t2, 0.75f))) a = 0.35f;
                        }
                        break;
                    case Glyph.Rock:
                        {
                            float d = HexD(x * 100f, y * 100f, 72f);
                            a = d < 0f ? 1f : 0f;
                            if (a > 0f) { if (Mathf.Abs(x) < 0.03f || Mathf.Abs(y + x * 0.5f) < 0.03f || Mathf.Abs(y - x * 0.5f) < 0.03f) a = 0.35f; }
                            float cy = HexD(x * 100f, y * 100f, 44f); if (cy < 0f && cy > -8f && a > 0f) a = Mathf.Min(a, 0.7f);
                        }
                        break;
                    case Glyph.Drop:
                        a = Disc(x, y, 0f, -0.25f, 0.55f);
                        if (y > -0.2f && y < 0.85f && Mathf.Abs(x) < 0.52f * (0.85f - y) / 1.05f) a = 1f;
                        if (Disc(x, y, -0.2f, -0.3f, 0.12f) > 0f) a = 0.4f;
                        break;
                    case Glyph.Wind:
                        for (int k = 0; k < 3; k++)
                        {
                            float cy = 0.52f - k * 0.5f;
                            float xs = -0.8f + k * 0.08f;
                            if (x > xs && x < 0.55f && Mathf.Abs(y - cy) < 0.07f) a = 1f;
                            if (Ring(x, y, 0.55f, cy - 0.17f, 0.17f, 0.07f) > 0f && x > 0.55f) a = 1f;
                        }
                        break;
                    case Glyph.Cross:
                        a = In(x, y, 0f, 0f, 0.13f, 0.62f) || In(x, y, 0f, 0.15f, 0.4f, 0.12f) ? 1f : 0f;
                        if (Ring(x, y, 0f, 0.1f, 0.82f, 0.05f) > 0f) a = 1f;
                        break;
                    case Glyph.Skull:
                        a = Disc(x, y, 0f, 0.12f, 0.62f);
                        if (In(x, y, 0f, -0.55f, 0.32f, 0.22f)) a = 1f;
                        if (Disc(x, y, -0.25f, 0.1f, 0.17f) > 0f || Disc(x, y, 0.25f, 0.1f, 0.17f) > 0f) a = 0f;
                        if (Mathf.Abs(x) < 0.09f && y < -0.1f && y > -0.25f - Mathf.Abs(x)) a = 0f;
                        if (a > 0f && y < -0.45f && (Mathf.Abs(x * 10f % 2f) < 0.4f)) a = 0.6f;
                        break;
                    case Glyph.Person:
                        a = Disc(x, y, 0f, 0.42f, 0.3f);
                        if (y < 0.05f && y > -0.75f && Mathf.Abs(x) < 0.7f * Mathf.Sqrt(Mathf.Max(0f, 1f - ((y + 0.7f) / 0.75f) * ((y + 0.7f) / 0.75f) * 0.0f)) && Mathf.Abs(x) < 0.55f + (y + 0.75f) * 0.2f) a = 1f;
                        break;
                    case Glyph.Star:
                        a = (Mathf.Sqrt(Mathf.Abs(x)) + Mathf.Sqrt(Mathf.Abs(y)) < 1.0f) ? 1f : 0f;
                        if (Mathf.Abs(x) + Mathf.Abs(y) < 0.2f) a = 0.4f;
                        break;
                    case Glyph.Bag:
                        a = In(x, y, 0f, -0.2f, 0.58f, 0.52f) ? 1f : 0f;
                        if (Ring(x, y, 0f, 0.38f, 0.3f, 0.07f) > 0f && y > 0.3f) a = 1f;
                        if (In(x, y, 0f, -0.1f, 0.2f, 0.07f)) a = 0.35f;
                        break;
                    case Glyph.Scroll:
                        a = In(x, y, 0f, 0f, 0.5f, 0.68f) ? 1f : 0f;
                        if (In(x, y, 0f, 0.65f, 0.6f, 0.1f) || In(x, y, 0f, -0.65f, 0.6f, 0.1f)) a = 1f;
                        for (int k = 0; k < 3; k++) if (In(x, y, 0f, 0.3f - k * 0.3f, 0.3f, 0.04f)) a = 0.3f;
                        break;
                    case Glyph.Book:
                        a = (In(x, y, -0.33f, 0f, 0.32f, 0.62f) || In(x, y, 0.33f, 0f, 0.32f, 0.62f)) ? 1f : 0f;
                        if (In(x, y, 0f, 0f, 0.03f, 0.62f)) a = 0.3f;
                        for (int k = 0; k < 3; k++) if (In(x, y, -0.33f, 0.3f - k * 0.3f, 0.2f, 0.035f) || In(x, y, 0.33f, 0.3f - k * 0.3f, 0.2f, 0.035f)) a = 0.3f;
                        break;
                    case Glyph.Gear:
                        {
                            float r = Mathf.Sqrt(x * x + y * y), ang = Mathf.Atan2(y, x);
                            float teeth = 0.62f + 0.14f * (Mathf.Sin(ang * 8f) > 0f ? 1f : 0f);
                            a = r < teeth && r > 0.24f ? 1f : 0f;
                        }
                        break;
                    case Glyph.Shield:
                        a = (Mathf.Abs(x) < 0.62f && y < 0.7f && y > -0.2f) || (y <= -0.2f && Mathf.Abs(x) < 0.62f * (1f + (y + 0.2f) / 0.75f) && y > -0.95f) ? 1f : 0f;
                        if (In(x, y, 0f, 0.1f, 0.05f, 0.5f) || In(x, y, 0f, 0.3f, 0.35f, 0.05f)) a = 0.3f;
                        break;
                    case Glyph.Sun:
                        {
                            float rr = Mathf.Sqrt(x * x + y * y), ang = Mathf.Atan2(y, x);
                            a = rr < 0.38f || (rr > 0.52f && rr < 0.88f && Mathf.Cos(ang * 8f) > 0.78f) ? 1f : 0f;
                        }
                        break;
                    case Glyph.Moon:
                        a = Disc(x, y, -0.05f, 0f, 0.7f) > 0f && Disc(x, y, 0.32f, 0.18f, 0.58f) <= 0f ? 1f : 0f;
                        break;
                    case Glyph.Cloud:
                        a = (Disc(x, y, -0.4f, -0.12f, 0.32f) > 0f || Disc(x, y, 0.02f, 0.12f, 0.42f) > 0f || Disc(x, y, 0.45f, -0.12f, 0.3f) > 0f || In(x, y, 0.02f, -0.28f, 0.72f, 0.16f)) ? 1f : 0f;
                        break;
                    default:
                        a = Mathf.Abs(x) + Mathf.Abs(y) < 0.8f ? 1f : 0f;
                        break;
                }
                return G(1f, a);
            }, Vector4.zero);
        }

        public static Glyph JobGlyph(string job)
        {
            switch (job)
            {
                case "Archer": return Glyph.Bow;
                case "FireMage": return Glyph.Flame;
                case "EarthMage": return Glyph.Rock;
                case "WaterMage": return Glyph.Drop;
                case "WindMage": return Glyph.Wind;
                case "Priest": return Glyph.Cross;
            }
            return Glyph.Shield;
        }
    }
}
