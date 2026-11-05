using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Ikon skill buatan kode (64x64): warna per elemen, simbol per jenis skill, bingkai per tier. Tiap skill beda.</summary>
    public static class SkillIcons
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        const int N = 64;

        public static Sprite For(SkillDef s)
        {
            if (s == null) return null;
            Sprite sp;
            if (cache.TryGetValue(s.id, out sp) && sp != null) return sp;
            sp = Make(s);
            cache[s.id] = sp;
            return sp;
        }

        static int Hash(string str)
        {
            unchecked { int h = 17; foreach (char c in str) h = h * 31 + c; return h & 0x7fffffff; }
        }

        static string ElemOf(SkillDef s)
        {
            string e = !string.IsNullOrEmpty(s.elem) ? s.elem : JobInfo.Elem(s.job);
            return e ?? "";
        }

        // kode simbol
        const int Orb = 0, Fan = 1, Nova = 2, Spikes = 3, Arrow = 4, Chevron = 5, Up = 6, Cross = 7, Star = 8, Dots = 9, Tri = 10, Slash = 11, Shield = 12, Count = 13;

        static int GlyphOf(SkillDef s)
        {
            switch (s.kind)
            {
                case "bolt": return Orb;
                case "cone": return Fan;
                case "nova": return Nova;
                case "ground": return Spikes;
                case "line": return Arrow;
                case "dash": return Chevron;
                case "buff": return Up;
                case "heal": return Cross;
                case "summon": return Star;
                case "multi": return Dots;
                case "trap": return Tri;
            }
            string n = (s.name ?? "").ToLower();
            if (n.Contains("tebas") || n.Contains("slash") || n.Contains("pedang") || n.Contains("potong")) return Slash;
            if (n.Contains("perisai") || n.Contains("guard") || n.Contains("tameng") || n.Contains("bertahan")) return Shield;
            if (n.Contains("panah") || n.Contains("tembak") || n.Contains("shot")) return Arrow;
            if (n.Contains("heal") || n.Contains("sembuh") || n.Contains("berkat")) return Cross;
            if (n.Contains("hujan") || n.Contains("badai") || n.Contains("ledak") || n.Contains("nova")) return Nova;
            if (n.Contains("bola") || n.Contains("api") || n.Contains("petir")) return Orb;
            if (n.Contains("lari") || n.Contains("dash") || n.Contains("lompat")) return Chevron;
            if (!s.active) return (Hash(s.id) % 3 == 0) ? Shield : (Hash(s.id) % 3 == 1 ? Up : Star);
            return Hash(s.id) % Count;
        }

        static float SegD(float px, float py, float ax, float ay, float bx, float by)
        {
            float abx = bx - ax, aby = by - ay;
            float l = abx * abx + aby * aby;
            float t = l < 1e-6f ? 0f : Mathf.Clamp01(((px - ax) * abx + (py - ay) * aby) / l);
            float dx = px - (ax + abx * t), dy = py - (ay + aby * t);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        static float Cov(float d, float w) { return Mathf.Clamp01((w - d) / 0.035f + 0.5f); }

        // jarak ke simbol (koordinat -1..1, y ke atas); mengembalikan coverage 0..1
        static float Glyph(int g, string elem, float u, float v, int seed)
        {
            float a = 0f, r = Mathf.Sqrt(u * u + v * v);
            switch (g)
            {
                case Orb:
                    a = Mathf.Max(a, Cov(r, 0.28f));
                    a = Mathf.Max(a, Cov(Mathf.Abs(r - 0.42f), 0.04f) * 0.8f);
                    a = Mathf.Max(a, Cov(SegD(u, v, -0.85f, -0.7f, -0.25f, -0.2f), 0.07f) * 0.8f);
                    a = Mathf.Max(a, Cov(SegD(u, v, -0.7f, -0.35f, -0.2f, -0.05f), 0.05f) * 0.6f);
                    break;
                case Fan:
                    for (int i = -2; i <= 2; i++)
                    {
                        float ang = (90f + i * 22f) * Mathf.Deg2Rad;
                        a = Mathf.Max(a, Cov(SegD(u, v, 0f, -0.75f, Mathf.Cos(ang) * 0.95f, -0.75f + Mathf.Sin(ang) * 1.6f), 0.06f));
                    }
                    a = Mathf.Max(a, Cov(r, 0.14f));
                    break;
                case Nova:
                    a = Mathf.Max(a, Cov(Mathf.Abs(r - 0.55f), 0.07f));
                    a = Mathf.Max(a, Cov(r, 0.16f));
                    for (int i = 0; i < 8; i++)
                    {
                        float ang = i * 45f * Mathf.Deg2Rad;
                        a = Mathf.Max(a, Cov(SegD(u, v, Mathf.Cos(ang) * 0.68f, Mathf.Sin(ang) * 0.68f, Mathf.Cos(ang) * 0.92f, Mathf.Sin(ang) * 0.92f), 0.05f));
                    }
                    break;
                case Spikes:
                    for (int i = -2; i <= 2; i++)
                    {
                        float cx = i * 0.34f, h = 0.75f - Mathf.Abs(i) * 0.18f;
                        float tw = Mathf.Max(0f, 1f - (v + 0.8f) / (h + 0.8f)) * 0.16f;
                        if (v > -0.8f && v < h && Mathf.Abs(u - cx) < tw) a = 1f;
                    }
                    a = Mathf.Max(a, Cov(SegD(u, v, -0.95f, -0.82f, 0.95f, -0.82f), 0.06f));
                    break;
                case Arrow:
                    a = Mathf.Max(a, Cov(SegD(u, v, -0.85f, -0.85f, 0.7f, 0.7f), 0.06f));
                    a = Mathf.Max(a, Cov(SegD(u, v, 0.85f, 0.85f, 0.3f, 0.8f), 0.07f));
                    a = Mathf.Max(a, Cov(SegD(u, v, 0.85f, 0.85f, 0.8f, 0.3f), 0.07f));
                    a = Mathf.Max(a, Cov(SegD(u, v, -0.85f, -0.85f, -0.95f, -0.55f), 0.05f));
                    a = Mathf.Max(a, Cov(SegD(u, v, -0.85f, -0.85f, -0.55f, -0.95f), 0.05f));
                    break;
                case Chevron:
                    for (int i = 0; i < 3; i++)
                    {
                        float ox = -0.55f + i * 0.5f;
                        float w = 0.09f;
                        a = Mathf.Max(a, Cov(SegD(u, v, ox - 0.2f, 0.6f, ox + 0.2f, 0f), w));
                        a = Mathf.Max(a, Cov(SegD(u, v, ox - 0.2f, -0.6f, ox + 0.2f, 0f), w));
                    }
                    break;
                case Up:
                    a = Mathf.Max(a, Cov(SegD(u, v, 0f, -0.75f, 0f, 0.7f), 0.1f));
                    a = Mathf.Max(a, Cov(SegD(u, v, -0.5f, 0.2f, 0f, 0.75f), 0.1f));
                    a = Mathf.Max(a, Cov(SegD(u, v, 0.5f, 0.2f, 0f, 0.75f), 0.1f));
                    a = Mathf.Max(a, Cov(SegD(u, v, -0.4f, -0.2f, 0f, 0.3f), 0.06f) * 0.7f);
                    break;
                case Cross:
                    a = Mathf.Max(a, Cov(SegD(u, v, 0f, -0.7f, 0f, 0.7f), 0.2f));
                    a = Mathf.Max(a, Cov(SegD(u, v, -0.7f, 0f, 0.7f, 0f), 0.2f));
                    break;
                case Star:
                    for (int i = 0; i < 5; i++)
                    {
                        float a1 = (90f + i * 144f) * Mathf.Deg2Rad, a2 = (90f + (i + 1) * 144f) * Mathf.Deg2Rad;
                        a = Mathf.Max(a, Cov(SegD(u, v, Mathf.Cos(a1) * 0.85f, Mathf.Sin(a1) * 0.85f, Mathf.Cos(a2) * 0.85f, Mathf.Sin(a2) * 0.85f), 0.06f));
                    }
                    a = Mathf.Max(a, Cov(r, 0.14f));
                    break;
                case Dots:
                    a = Mathf.Max(a, Cov(Mathf.Sqrt(u * u + (v - 0.45f) * (v - 0.45f)), 0.2f));
                    a = Mathf.Max(a, Cov(Mathf.Sqrt((u + 0.5f) * (u + 0.5f) + (v + 0.35f) * (v + 0.35f)), 0.2f));
                    a = Mathf.Max(a, Cov(Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v + 0.35f) * (v + 0.35f)), 0.2f));
                    break;
                case Tri:
                    a = Mathf.Max(a, Cov(SegD(u, v, -0.8f, -0.6f, 0.8f, -0.6f), 0.07f));
                    a = Mathf.Max(a, Cov(SegD(u, v, -0.8f, -0.6f, 0f, 0.8f), 0.07f));
                    a = Mathf.Max(a, Cov(SegD(u, v, 0.8f, -0.6f, 0f, 0.8f), 0.07f));
                    a = Mathf.Max(a, Cov(r, 0.12f));
                    break;
                case Slash:
                    a = Mathf.Max(a, Cov(SegD(u, v, -0.8f, 0.8f, 0.8f, -0.8f), 0.1f));
                    a = Mathf.Max(a, Cov(SegD(u, v, -0.8f, 0.45f, 0.45f, -0.8f), 0.05f) * 0.7f);
                    a = Mathf.Max(a, Cov(SegD(u, v, -0.45f, 0.8f, 0.8f, -0.45f), 0.05f) * 0.7f);
                    break;
                case Shield:
                    {
                        float hw = 0.62f * Mathf.Clamp01(1.1f - Mathf.Max(0f, -v - 0.1f) * 0.9f);
                        if (v > -0.8f && v < 0.8f && Mathf.Abs(u) < hw) a = Mathf.Abs(u) > hw - 0.12f || v > 0.68f ? 1f : 0.35f;
                        a = Mathf.Max(a, Cov(SegD(u, v, 0f, -0.35f, 0f, 0.45f), 0.06f));
                    }
                    break;
            }
            // hiasan elemen (pojok kanan atas)
            float eu = u - 0.62f, ev = v - 0.62f;
            float ea = 0f;
            switch (elem)
            {
                case "fire": // api: tetesan runcing
                    if (ev > -0.2f && ev < 0.25f && Mathf.Abs(eu) < (0.25f - ev) * 0.55f * Mathf.Clamp01((ev + 0.2f) * 6f + 0.3f)) ea = 1f; break;
                case "water":
                    ea = Mathf.Max(Cov(Mathf.Sqrt(eu * eu + (ev + 0.05f) * (ev + 0.05f)), 0.16f), (ev > 0.08f && ev < 0.3f && Mathf.Abs(eu) < (0.3f - ev) * 0.7f) ? 1f : 0f); break;
                case "wind":
                    ea = Mathf.Max(Cov(Mathf.Abs(Mathf.Sqrt(eu * eu + ev * ev) - 0.15f), 0.04f), Cov(SegD(eu, ev, 0.1f, -0.2f, 0.3f, -0.2f), 0.04f)); break;
                case "earth":
                    ea = (Mathf.Abs(eu) + Mathf.Abs(ev) < 0.22f) ? 1f : 0f; break;
                case "holy":
                    ea = Mathf.Max(Cov(SegD(eu, ev, 0f, -0.22f, 0f, 0.22f), 0.045f), Cov(SegD(eu, ev, -0.22f, 0f, 0.22f, 0f), 0.045f)); break;
            }
            return Mathf.Max(a, ea);
        }

        static Sprite Make(SkillDef s)
        {
            string elem = ElemOf(s);
            Color baseCol = (elem == "" || elem == "phys" || elem == "arrow") ? JobInfo.JobColor(s.job) : JobInfo.ElemColor(elem);
            int h = Hash(s.id);
            // variasi warna kecil per skill supaya tetap berbeda walau satu elemen
            float hue, sat, val; Color.RGBToHSV(baseCol, out hue, out sat, out val);
            hue = Mathf.Repeat(hue + ((h % 11) - 5) * 0.006f, 1f);
            Color c1 = Color.HSVToRGB(hue, Mathf.Clamp01(sat * 0.9f), Mathf.Clamp01(val * 0.95f));
            Color c0 = Color.Lerp(c1, Color.black, 0.72f);
            int g = GlyphOf(s);
            bool passive = !s.active;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[N * N];
            Color gold = new Color(1f, 0.82f, 0.35f);
            Color rim = s.tier >= 3 ? gold : (s.tier == 2 ? new Color(0.78f, 0.85f, 1f) : new Color(0.55f, 0.55f, 0.6f));
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v);
                    // bentuk: pasif = persegi membulat, aktif = lingkaran
                    float edge = passive ? Mathf.Max(Mathf.Abs(u), Mathf.Abs(v)) : r;
                    float lim = passive ? 0.94f : 0.97f;
                    if (edge > lim) { px[y * N + x] = Color.clear; continue; }
                    Color col = Color.Lerp(c1 * 0.75f, c0, Mathf.Clamp01(r * 0.95f));
                    col.a = 1f;
                    // kilau dasar
                    col += c1 * 0.25f * Mathf.Clamp01(1f - Mathf.Sqrt((u + 0.2f) * (u + 0.2f) + (v - 0.35f) * (v - 0.35f)) * 1.3f);
                    float ga = Glyph(g, elem, u, v, h);
                    if (ga > 0f)
                    {
                        // bayangan lembut
                        Color ink = Color.Lerp(Color.white, c1, 0.25f);
                        col = Color.Lerp(col, ink, ga);
                    }
                    // bingkai
                    float rimW = s.tier >= 3 ? 0.12f : 0.07f;
                    if (edge > lim - rimW) col = Color.Lerp(col, rim, 0.92f);
                    // tanda tier III: bintang kecil di bawah
                    if (s.tier >= 3 && Mathf.Abs(u) < 0.5f && v < -0.62f && v > -0.84f)
                    {
                        float d = Mathf.Min(Mathf.Abs(u + 0.3f), Mathf.Min(Mathf.Abs(u), Mathf.Abs(u - 0.3f)));
                        if (d < 0.09f) col = gold;
                    }
                    col.a = Mathf.Clamp01((lim - edge) / 0.03f + 0.2f);
                    px[y * N + x] = col;
                }
            tex.SetPixels(px);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
