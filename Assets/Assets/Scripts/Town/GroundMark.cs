using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Bekas efek di tanah: hangus, retakan, cakaran, es, genangan air, goresan angin, sigil suci, kawah, jejak petir, tapak kaki, jejak ular, lubang panah.
    /// Setiap skill (semua job, semua evolusi) meninggalkan bekas yang sesuai nama &amp; elemennya, lalu memudar perlahan.
    /// Bekas = quad pipih menempel di permukaan tanah (mengikuti kemiringan), tekstur prosedural, jumlah dibatasi.
    /// </summary>
    public static class GroundMark
    {
        // ------------------------------------------------------------------ tekstur prosedural
        static readonly Dictionary<string, Texture2D> texes = new Dictionary<string, Texture2D>();

        static float Noise(float x, float y) { return Mathf.PerlinNoise(x + 17.3f, y + 41.7f); }
        static float Smooth(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

        static float SegDist(float px, float py, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax, dy = by - ay, l2 = dx * dx + dy * dy;
            float t = l2 < 1e-6f ? 0f : Mathf.Clamp01(((px - ax) * dx + (py - ay) * dy) / l2);
            float cx = ax + dx * t - px, cy = ay + dy * t - py;
            return Mathf.Sqrt(cx * cx + cy * cy);
        }

        static float Line(float d, float w) { return 1f - Smooth(w * 0.4f, w, d); }

        static Texture2D Make(string key, int n, System.Func<float, float, float> f)
        {
            Texture2D t;
            if (texes.TryGetValue(key, out t) && t != null) return t;
            t = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 2 };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                    float a = Mathf.Clamp01(f(u, v)) * Mathf.Clamp01((1f - Mathf.Sqrt(u * u + v * v)) * 6f);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            t.SetPixels32(px); t.Apply();
            texes[key] = t;
            return t;
        }

        public static void Prewarm()
        {
            string[] ks = { "scorch", "crack", "claw", "frost", "puddle", "wind", "glyph", "rune", "crater", "bolt", "paw", "hoof", "slither", "pock", "skid" };
            foreach (var k in ks) Tex(k);
        }

        static Texture2D Tex(string kind)
        {
            switch (kind)
            {
                case "scorch":
                    return Make(kind, 128, (u, v) =>
                    {
                        float d = Mathf.Sqrt(u * u + v * v), ang = Mathf.Atan2(v, u);
                        float edge = 0.62f + 0.28f * Noise(Mathf.Cos(ang) * 1.6f + 3f, Mathf.Sin(ang) * 1.6f + 3f);
                        float a = 1f - Smooth(edge - 0.3f, edge, d);
                        a *= 0.55f + 0.45f * Noise(u * 4.5f, v * 4.5f);
                        return a + (1f - Smooth(0f, 0.3f, d)) * 0.35f;
                    });
                case "crack":
                    return Make(kind, 192, (u, v) =>
                    {
                        float d = Mathf.Sqrt(u * u + v * v), ang = Mathf.Atan2(v, u), a = 0f;
                        for (int i = 0; i < 9; i++)
                        {
                            float b0 = i / 9f * Mathf.PI * 2f + Noise(i * 3.1f, 0.5f) * 0.6f;
                            float jag = (Noise(d * 7f, i * 5.3f) - 0.5f) * 0.6f;
                            float dA = Mathf.Abs(Mathf.DeltaAngle((ang - b0 - jag) * Mathf.Rad2Deg, 0f)) * Mathf.Deg2Rad;
                            float reach = 0.55f + Noise(i * 1.7f, 2.2f) * 0.45f;
                            if (d < reach && d > 0.04f) a = Mathf.Max(a, Line(dA * d, 0.03f * (1.2f - d / reach)) * (1f - d / reach * 0.5f));
                        }
                        return Mathf.Max(a, Mathf.Clamp01(1f - d * 2.4f) * 0.55f);
                    });
                case "claw":
                    return Make(kind, 128, (u, v) =>
                    {
                        float a = 0f;
                        for (int i = -1; i <= 1; i++)
                        {
                            float ox = i * 0.42f;
                            float d = SegDist(u, v, ox - 0.35f, 0.85f, ox + 0.3f, -0.85f);
                            float along = Mathf.Clamp01((0.85f - v) / 1.7f);
                            float w = 0.11f * Mathf.Sin(along * Mathf.PI) + 0.012f;
                            a = Mathf.Max(a, 1f - Smooth(w * 0.5f, w, d));
                        }
                        return a * (0.7f + 0.3f * Noise(u * 6f, v * 6f));
                    });
                case "frost":
                    return Make(kind, 192, (u, v) =>
                    {
                        float d = Mathf.Sqrt(u * u + v * v), ang = Mathf.Atan2(v, u);
                        float fa = Mathf.Abs(Mathf.Repeat(ang, Mathf.PI / 3f) - Mathf.PI / 6f);
                        float spoke = Line(fa * d, 0.035f) * (1f - Smooth(0.5f, 0.95f, d));
                        float br = 0f;
                        for (int k = 1; k <= 3; k++)
                        {
                            float rr = k * 0.22f, wd = 0.2f * (1f - k * 0.2f);
                            br = Mathf.Max(br, Line(Mathf.Abs(fa * d - Mathf.Abs(d - rr) * 0.6f) * 1.0f, 0.04f) * (d > rr ? 1f - Smooth(rr, rr + wd, d) : 0f));
                        }
                        float disc = (1f - Smooth(0.35f, 0.75f, d)) * (0.35f + 0.3f * Noise(u * 5f, v * 5f));
                        return Mathf.Max(Mathf.Max(spoke, br * 0.8f), disc);
                    });
                case "puddle":
                    return Make(kind, 128, (u, v) =>
                    {
                        float d = Mathf.Sqrt(u * u + v * v), ang = Mathf.Atan2(v, u);
                        float edge = 0.55f + 0.3f * Noise(Mathf.Cos(ang) * 1.3f + 8f, Mathf.Sin(ang) * 1.3f + 8f);
                        float a = (1f - Smooth(edge - 0.08f, edge, d)) * 0.65f;
                        a += Line(Mathf.Abs(d - edge * 0.62f), 0.05f) * 0.5f * (d < edge ? 1f : 0f);
                        return a;
                    });
                case "wind":
                    return Make(kind, 160, (u, v) =>
                    {
                        float d = Mathf.Sqrt(u * u + v * v), ang = Mathf.Atan2(v, u), a = 0f;
                        for (int i = 0; i < 3; i++)
                        {
                            float arm = ang - d * 3.2f - i * Mathf.PI * 2f / 3f;
                            float dd = Mathf.Abs(Mathf.DeltaAngle(arm * Mathf.Rad2Deg, 0f)) * Mathf.Deg2Rad * d;
                            a = Mathf.Max(a, Line(dd, 0.07f * (1.1f - d)) * (1f - Smooth(0.45f, 0.95f, d)));
                        }
                        return a * (0.75f + 0.25f * Noise(u * 5f, v * 5f)) + (1f - Smooth(0f, 0.25f, d)) * 0.2f;
                    });
                case "glyph":
                    return Make(kind, 256, (u, v) =>
                    {
                        float d = Mathf.Sqrt(u * u + v * v), a = Line(Mathf.Abs(d - 0.93f), 0.025f);
                        a = Mathf.Max(a, Line(Mathf.Abs(d - 0.8f), 0.018f) * 0.8f);
                        a = Mathf.Max(a, Line(Mathf.Abs(d - 0.38f), 0.015f) * 0.7f);
                        for (int i = 0; i < 6; i++)
                        {
                            float a0 = i * Mathf.PI / 3f, a1 = (i + 2) * Mathf.PI / 3f;
                            a = Mathf.Max(a, Line(SegDist(u, v, Mathf.Cos(a0) * 0.8f, Mathf.Sin(a0) * 0.8f, Mathf.Cos(a1) * 0.8f, Mathf.Sin(a1) * 0.8f), 0.02f));
                            float tx = Mathf.Cos(a0 + Mathf.PI / 6f), ty = Mathf.Sin(a0 + Mathf.PI / 6f);
                            a = Mathf.Max(a, Line(SegDist(u, v, tx * 0.8f, ty * 0.8f, tx * 0.93f, ty * 0.93f), 0.02f) * 0.9f);
                        }
                        return Mathf.Max(a, (1f - Smooth(0f, 0.12f, d)) * 0.8f);
                    });
                case "rune":
                    return Make(kind, 256, (u, v) =>
                    {
                        float d = Mathf.Sqrt(u * u + v * v), a = Line(Mathf.Abs(d - 0.92f), 0.03f);
                        a = Mathf.Max(a, Line(Mathf.Abs(d - 0.7f), 0.02f) * 0.8f);
                        for (int i = 0; i < 5; i++)
                        {
                            float a0 = i * Mathf.PI * 2f / 5f + Mathf.PI / 2f, a1 = (i + 2) * Mathf.PI * 2f / 5f + Mathf.PI / 2f;
                            a = Mathf.Max(a, Line(SegDist(u, v, Mathf.Cos(a0) * 0.7f, Mathf.Sin(a0) * 0.7f, Mathf.Cos(a1) * 0.7f, Mathf.Sin(a1) * 0.7f), 0.022f));
                        }
                        float ang = Mathf.Atan2(v, u);
                        if (d > 0.74f && d < 0.9f) a = Mathf.Max(a, (Noise(ang * 6f, 1.3f) > 0.62f ? 0.9f : 0f));
                        return Mathf.Max(a, (1f - Smooth(0f, 0.1f, d)) * 0.8f);
                    });
                case "crater":
                    return Make(kind, 160, (u, v) =>
                    {
                        float d = Mathf.Sqrt(u * u + v * v), ang = Mathf.Atan2(v, u);
                        float rim = 0.5f + 0.1f * Noise(Mathf.Cos(ang) * 2f, Mathf.Sin(ang) * 2f);
                        float a = d < rim ? 0.75f : Mathf.Max(0f, 1f - Smooth(rim, rim + 0.18f, d)) * 0.9f;
                        float deb = Noise(u * 7f, v * 7f);
                        if (d > rim + 0.1f && d < 0.95f && deb > 0.68f) a = Mathf.Max(a, 0.65f);
                        return a * (0.8f + 0.2f * Noise(u * 5f, v * 5f));
                    });
                case "bolt":
                    return Make(kind, 160, (u, v) =>
                    {
                        float d = Mathf.Sqrt(u * u + v * v), ang = Mathf.Atan2(v, u), a = 0f;
                        for (int i = 0; i < 6; i++)
                        {
                            float b0 = i / 6f * Mathf.PI * 2f + Noise(i * 2.3f, 4.1f) * 1.0f;
                            float jag = (Noise(d * 12f, i * 7.7f) - 0.5f) * 0.9f;
                            float dA = Mathf.Abs(Mathf.DeltaAngle((ang - b0 - jag) * Mathf.Rad2Deg, 0f)) * Mathf.Deg2Rad;
                            float reach = 0.6f + Noise(i * 1.1f, 9.3f) * 0.4f;
                            if (d < reach) a = Mathf.Max(a, Line(dA * d, 0.035f) * (1f - d / reach * 0.4f));
                        }
                        return Mathf.Max(a, (1f - Smooth(0f, 0.32f, d)) * 0.7f);
                    });
                case "paw":
                    return Make(kind, 96, (u, v) =>
                    {
                        float pad = 1f - Smooth(0.7f, 1f, Mathf.Sqrt(Mathf.Pow(u / 0.55f, 2f) + Mathf.Pow((v + 0.28f) / 0.42f, 2f)));
                        float a = pad;
                        for (int i = 0; i < 4; i++)
                        {
                            float t = Mathf.Lerp(-0.95f, 0.95f, i / 3f), cx = t * 0.62f, cy = 0.38f + 0.28f * (1f - Mathf.Abs(t));
                            a = Mathf.Max(a, 1f - Smooth(0.7f, 1f, Mathf.Sqrt(Mathf.Pow((u - cx) / 0.2f, 2f) + Mathf.Pow((v - cy) / 0.26f, 2f))));
                        }
                        return a;
                    });
                case "hoof":
                    return Make(kind, 96, (u, v) =>
                    {
                        float a = 0f;
                        for (int s = -1; s <= 1; s += 2)
                        {
                            float cx = s * 0.32f;
                            float e = Mathf.Sqrt(Mathf.Pow((u - cx) / 0.3f, 2f) + Mathf.Pow(v / 0.78f, 2f));
                            float c = 1f - Smooth(0.7f, 1f, e);
                            float hole = 1f - Smooth(0.4f, 0.6f, Mathf.Sqrt(Mathf.Pow((u - cx) / 0.3f, 2f) + Mathf.Pow((v + 0.1f) / 0.78f, 2f)));
                            a = Mathf.Max(a, c * (1f - hole * 0.5f));
                        }
                        return a;
                    });
                case "slither":
                    return Make(kind, 160, (u, v) =>
                    {
                        float cx = 0.38f * Mathf.Sin(v * Mathf.PI * 1.5f);
                        float w = 0.2f * (1f - Mathf.Abs(v) * 0.6f);
                        float a = 1f - Smooth(w * 0.55f, w, Mathf.Abs(u - cx));
                        return a * (0.7f + 0.3f * Mathf.Sin(v * 40f) * 0.5f + 0.15f);
                    });
                case "pock":
                    return Make(kind, 128, (u, v) =>
                    {
                        float a = 0f;
                        for (int i = 0; i < 9; i++)
                        {
                            float px = (Noise(i * 3.7f, 1.1f) - 0.5f) * 1.5f, py = (Noise(i * 5.1f, 2.9f) - 0.5f) * 1.5f, rr = 0.08f + 0.07f * Noise(i * 1.3f, 7.7f);
                            float d = Mathf.Sqrt((u - px) * (u - px) + (v - py) * (v - py));
                            a = Mathf.Max(a, 1f - Smooth(rr * 0.6f, rr, d));
                            a = Mathf.Max(a, Line(Mathf.Abs(d - rr * 1.7f), 0.03f) * 0.45f);
                        }
                        return a;
                    });
                default: // skid
                    return Make("skid", 160, (u, v) =>
                    {
                        float a = 0f;
                        for (int i = -1; i <= 1; i++)
                        {
                            float yy = i * 0.28f + (Noise(i * 2f, 5f) - 0.5f) * 0.1f;
                            float w = 0.05f + 0.02f * Noise(u * 4f, i * 3f);
                            a = Mathf.Max(a, (1f - Smooth(w * 0.5f, w, Mathf.Abs(v - yy))) * (1f - Smooth(0.5f, 0.95f, Mathf.Abs(u))) * (0.6f + 0.4f * Noise(u * 9f, i * 5f)));
                        }
                        return a;
                    });
            }
        }

        // ------------------------------------------------------------------ pengelola bekas
        static readonly List<Fader> live = new List<Fader>();
        const int MaxLive = 80;

        sealed class Fader : MonoBehaviour
        {
            public float life = 8f, age, fadeFrom = 0.55f, pulse;
            public Material dark, glow;
            public Color darkC, glowC;
            public bool glyph;
            void Update()
            {
                age += Time.deltaTime;
                float t = age / life;
                if (t >= 1f) { Destroy(gameObject); return; }
                float appear = Mathf.Clamp01(age / 0.12f);
                float fade = t < fadeFrom ? 1f : 1f - (t - fadeFrom) / (1f - fadeFrom);
                if (dark != null) { var c = darkC; c.a *= appear * fade; dark.color = c; }
                if (glow != null)
                {
                    float g = glyph ? (0.75f + 0.25f * Mathf.Sin(age * pulse)) * fade : Mathf.Pow(Mathf.Clamp01(1f - age / (life * 0.5f)), 1.6f) * (0.35f + 0.65f * (age < 0.3f ? 1f : 0.55f));
                    var c = glowC; c.a *= appear * g; glow.color = c;
                }
            }
            void OnDestroy()
            {
                live.Remove(this);
                if (dark != null) Destroy(dark);
                if (glow != null) Destroy(glow);
            }
        }

        static bool IsLiving(Collider c)
        {
            return c.GetComponentInParent<Monster>() != null || c.GetComponentInParent<PlayerCombat>() != null || c.isTrigger;
        }

        /// <summary>Titik tanah (y + normal) di bawah pos. Bila tidak ada collider tanah, pakai y apa adanya.</summary>
        static Vector3 Ground(Vector3 pos, out Vector3 normal)
        {
            normal = Vector3.up;
            var hits = Physics.RaycastAll(pos + Vector3.up * 3f, Vector3.down, 12f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue; bool ok = false; Vector3 p = pos;
            foreach (var h in hits)
            {
                if (h.collider == null || IsLiving(h.collider)) continue;
                if (h.distance < best) { best = h.distance; p = h.point; normal = h.normal; ok = true; }
            }
            if (!ok) return pos;
            if (normal.y < 0.5f) normal = Vector3.up;
            return p;
        }

        static void Style(string kind, Color col, out Color dark, out float darkA, out bool hasDark, out bool hasGlow, out bool pulse)
        {
            hasDark = true; hasGlow = true; pulse = false; darkA = 0.85f; dark = new Color(0.035f, 0.03f, 0.03f);
            switch (kind)
            {
                case "scorch": darkA = 0.88f; break;
                case "crack": dark = new Color(0.07f, 0.055f, 0.045f); darkA = 0.92f; break;
                case "claw": darkA = 0.85f; break;
                case "crater": dark = new Color(0.08f, 0.065f, 0.05f); darkA = 0.9f; hasGlow = false; break;
                case "frost": dark = new Color(0.78f, 0.92f, 1f); darkA = 0.7f; break;
                case "puddle": dark = Color.Lerp(new Color(0.05f, 0.12f, 0.2f), col, 0.25f); darkA = 0.55f; hasGlow = false; break;
                case "wind": dark = new Color(0.8f, 0.78f, 0.7f); darkA = 0.28f; break;
                case "glyph": case "rune": hasDark = false; pulse = true; break;
                case "bolt": darkA = 0.85f; break;
                case "paw": case "hoof": darkA = 0.8f; break;
                case "slither": darkA = 0.7f; break;
                case "pock": darkA = 0.88f; break;
                case "skid": darkA = 0.5f; hasGlow = false; break;
            }
        }

        /// <summary>Tempel satu bekas di tanah. radius = setengah lebar bekas (dunia), yaw = arah putar (derajat; &lt;0 = acak).</summary>
        public static void Put(string kind, Vector3 pos, float radius, Color col, float life = 9f, float yaw = -1f)
        {
            if (radius < 0.15f) return;
            // bekas = goresan/luka di tanah, bukan lingkaran sihir: glyph/rune diganti retakan bercahaya / hangus, dan cepat memudar
            if (kind == "glyph") kind = "crack"; else if (kind == "rune") kind = "scorch";
            life = Mathf.Clamp(life * 0.45f, 3.5f, 9f);
            Vector3 n; Vector3 gp = Ground(pos, out n) + n * 0.07f;
            if (yaw < 0f) yaw = Random.Range(0f, 360f);
            while (live.Count >= MaxLive) { var old = live[0]; live.RemoveAt(0); if (old != null) Object.Destroy(old.gameObject); }

            Color dark; float darkA; bool hasDark, hasGlow, pulse;
            Style(kind, col, out dark, out darkA, out hasDark, out hasGlow, out pulse);
            var tex = Tex(kind);
            var root = new GameObject("FxMark_" + kind);
            root.transform.position = gp;
            root.transform.rotation = Quaternion.FromToRotation(Vector3.up, n) * Quaternion.Euler(90f, yaw, 0f);
            var f = root.AddComponent<Fader>();
            f.life = life; f.fadeFrom = kind == "puddle" ? 0.2f : 0.3f; f.pulse = 2.6f; f.glyph = pulse;
            float size = radius * 2f;
            if (hasDark)
            {
                var q = MakeQuad(root.transform, size, 0f);
                var m = CombatFx.Solid(tex, new Color(dark.r, dark.g, dark.b, 0f));
                q.sharedMaterial = m; f.dark = m; f.darkC = new Color(dark.r, dark.g, dark.b, darkA);
            }
            if (hasGlow)
            {
                var q = MakeQuad(root.transform, size * (kind == "glyph" || kind == "rune" ? 1f : 0.98f), 0.012f);
                Color gc = col; float mx = Mathf.Max(gc.r, Mathf.Max(gc.g, gc.b)); if (mx < 0.6f && mx > 0.01f) gc = new Color(gc.r / mx * 0.9f, gc.g / mx * 0.9f, gc.b / mx * 0.9f);
                var m = CombatFx.Mat(tex, new Color(gc.r, gc.g, gc.b, 0f));
                q.sharedMaterial = m; f.glow = m; f.glowC = new Color(gc.r, gc.g, gc.b, kind == "glyph" || kind == "rune" ? 0.9f : 0.8f);
            }
            live.Add(f);
        }

        static MeshRenderer MakeQuad(Transform parent, float size, float lift)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(parent, false);
            q.transform.localPosition = new Vector3(0f, 0f, -lift);
            q.transform.localScale = Vector3.one * size;
            var mr = q.GetComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return mr;
        }

        /// <summary>Beberapa bekas kecil berserakan di dalam lingkaran.</summary>
        public static void Scatter(string kind, Vector3 c, float area, int n, float rMin, float rMax, Color col, float life)
        {
            for (int i = 0; i < n; i++)
            {
                Vector2 q = Random.insideUnitCircle * area;
                Put(kind, c + new Vector3(q.x, 0f, q.y), Random.Range(rMin, rMax), col, life * Random.Range(0.8f, 1.1f));
            }
        }

        /// <summary>Jejak memanjang dari a ke b (tapak, goresan, jejak ular, jejak lompatan).</summary>
        public static void Trail(string kind, Vector3 a, Vector3 b, float spacing, float radius, Color col, float life, bool alternate = true)
        {
            Vector3 d = b - a; d.y = 0f; float len = d.magnitude;
            if (len < 0.3f) return;
            Vector3 dir = d / len, side = Vector3.Cross(Vector3.up, dir);
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            int n = Mathf.Clamp(Mathf.FloorToInt(len / Mathf.Max(0.5f, spacing)), 1, 14);
            for (int i = 0; i <= n; i++)
            {
                Vector3 p = Vector3.Lerp(a, b, i / (float)n) + (alternate ? side * (i % 2 == 0 ? 0.55f : -0.55f) * radius : Vector3.zero);
                Put(kind, p, radius, col, life, yaw);
            }
        }

        /// <summary>Satu bekas pada tiap titik di lingkaran.</summary>
        public static void RingOf(string kind, Vector3 c, float rad, int n, float radius, Color col, float life)
        {
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n + 0.3f;
                Put(kind, c + new Vector3(Mathf.Cos(a) * rad, 0f, Mathf.Sin(a) * rad), radius, col, life, a * Mathf.Rad2Deg + 90f);
            }
        }

        // ------------------------------------------------------------------ pemilihan bekas dari nama / elemen skill
        static bool Has(string n, params string[] ks) { foreach (var k in ks) if (n.Contains(k)) return true; return false; }

        /// <summary>Jenis bekas utama &amp; pendamping untuk sebuah skill, menurut nama lalu elemen/job.</summary>
        public static void Pick(SkillDef s, out string main, out string extra)
        {
            string n = " " + (s.name ?? "").ToLowerInvariant() + " ";
            string e = !string.IsNullOrEmpty(s.elem) ? s.elem : JobInfo.Elem(s.job);
            main = null; extra = null;
            if (s.tier <= 2 && s.active) { SkillLook.Marks(s, out main, out extra); if (main != null) return; }   // skill biasa: bekas dari tema nama (SkillLook)
            if (Has(n, "poison", "venom", "toxic", "plague")) { main = "puddle"; extra = "pock"; return; }
            if (Has(n, "magma", "lava", "volcan", "eruption", "meteor", "comet", "sun fall")) { main = "crater"; extra = "scorch"; return; }
            if (Has(n, "fire", "flame", "blaze", "burn", "inferno", "ember", "phoenix", " hell", "sear", "pyre", "armageddon", "cinder", "ignite", "scorch", "solar", "sunfire")) { main = "scorch"; extra = n.Contains("wall") || n.Contains("pillar") ? "crack" : "pock"; return; }
            if (Has(n, " ice", "frost", "glacial", "blizzard", "freez", "frozen", "snow", "absolute zero", "winter", "hail", "chill", "cryo")) { main = "frost"; extra = "crack"; return; }
            if (Has(n, "thunder", "lightning", "bolt", "chain", "volt", "shock", "spark", "raiju", "static", "storm arrow", "tempest rain")) { main = "bolt"; extra = "scorch"; return; }
            if (Has(n, "tsunami", "wave", "tide", "geyser", "torrent", "flood", "water", "aqua", "spring", "maelstrom", "whirlpool", "kraken", "leviathan", " rain", "bubble", "mist", "crashing")) { main = "puddle"; extra = "frost"; if (!(e == "water" || e == "storm")) extra = "pock"; if (Has(n, " rain") && e != "water") main = "pock"; return; }
            if (Has(n, "gale", "tornado", "typhoon", "cyclone", "zephyr", "vortex", "wind", "gust", "razor", "void cut", "whirl", "breeze", "sky", "slipstream")) { main = "wind"; extra = "claw"; return; }
            if (Has(n, "quake", "fissure", "terra", "stone", " rock", "boulder", "tremor", "earth", "crush", "smash", "slam", "breaker", "judgement slam", "colossus", "golem", "mountain", "continental", "titan", "bastion", "fortress", "bulwark")) { main = "crater"; extra = "crack"; return; }
            if (Has(n, "holy", "divine", "judg", "sanctuary", "bless", "prayer", "heal", "light", "radiance", "aegis", " ward", "warding", "sacred", "heaven", "miracle", "resurrection", "mercy", "benediction", "halo", "angel", "purify", "smite", "grace", "unicorn", "gate")) { main = "glyph"; extra = null; return; }
            if (Has(n, "shadow", "night", "dark", "death", "doom", "curse", "void", "soul", "abyss")) { main = "rune"; extra = "scorch"; return; }
            if (Has(n, "claw", "fang", "bite", "rend", "hunt", "feral", "rake", "talon", "tear", "savage", "maul", "bloodbath", "slaughter", "ravage")) { main = "claw"; extra = "pock"; return; }
            if (Has(n, "dash", "rush", "step", "charge", "leap", "dodge", "roll", "sprint", "phantom", "blink", "shift")) { main = "skid"; extra = "paw"; return; }
            if (Has(n, "arrow", "shot", "volley", "barrage", "pierce", "snipe", "quiver", "piercing", "bolt", "heartseeker", "skyward")) { main = "pock"; extra = "scorch"; return; }
            if (Has(n, "slash", "cut", "blade", "sword", "edge", "cleave", "strike", "sever", "thousand", "wyrm", "fury", "roar")) { main = "claw"; extra = "crack"; return; }
            if (Has(n, "serpent", "snake", "hydra", "ouroboros", "coil")) { main = "slither"; extra = "scorch"; return; }
            switch (e)
            {
                case "fire": main = "scorch"; extra = "pock"; break;
                case "earth": main = "crater"; extra = "crack"; break;
                case "water": main = "puddle"; extra = "frost"; break;
                case "storm": main = "bolt"; extra = "scorch"; break;
                case "wind": main = "wind"; extra = "claw"; break;
                case "holy": main = "glyph"; break;
                case "arrow": main = "pock"; extra = "scorch"; break;
                default: main = "claw"; extra = "crack"; break;
            }
        }

        public static Color MarkColor(SkillDef s)
        {
            if (s.tier <= 2) return SkillLook.Main(s);
            string e = !string.IsNullOrEmpty(s.elem) ? s.elem : JobInfo.Elem(s.job);
            if (e == "phys" || e == "") return CombatFx.SwordCol;
            if (e == "arrow") return CombatFx.BowCol;
            return MagicFx.ElemCol(e);
        }

        /// <summary>Tinggalkan bekas di tanah untuk skill yang baru dipakai (semua job &amp; evolusi).</summary>
        public static void Skill(SkillDef s, PlayerCombat pc, float delay)
        {
            if (s == null || pc == null || CombatFx.Host.I == null) return;
            CombatFx.Host.I.StartCoroutine(SkillR(s, pc, delay));
        }

        static IEnumerator SkillR(SkillDef s, PlayerCombat pc, float delay)
        {
            yield return new WaitForSeconds(Mathf.Max(0.05f, delay + 0.05f));
            if (pc == null) yield break;
            string main, extra; Pick(s, out main, out extra);
            Color col = MarkColor(s);
            Vector3 feet = pc.transform.position - Vector3.up * 0.9f;
            Vector3 F = pc.transform.forward; F.y = 0f; F = F.sqrMagnitude < 0.01f ? Vector3.forward : F.normalized;
            float reach = Mathf.Clamp(s.range > 0f ? s.range : 6f, 4f, 34f);
            string k = s.kind ?? "";
            bool self = k == "nova" || k == "buff" || k == "heal" || k == "dash" || k == "cone" || k == "line";
            Vector3 C = self ? feet : (SkillAction.GroundCenterSet ? SkillAction.GroundCenter : SkillExec.AimGroundFor(pc, Mathf.Min(reach, 34f)));
            float r = Mathf.Clamp(s.radius > 0f ? s.radius : 3.5f, 2f, 12f);
            bool ult = s.tier >= 3, big = s.tier >= 2;
            float life = ult ? 22f : big ? 13f : 9f;
            float sz = ult ? 1.25f : big ? 1f : 0.8f;
            int h = 0; foreach (char ch in s.id) h = h * 31 + ch; h &= 0x7fffffff;

            if (k == "buff" || k == "heal" || k == "summon") yield break;
            if (false)
            {
                string gk = (main == "glyph" || main == "rune") ? main : (JobInfo.Elem(s.job) == "holy" ? "glyph" : "rune");
                Put(gk, feet, (k == "summon" ? 3.2f : 3.6f) * sz + (ult ? 1.5f : 0f), col, Mathf.Max(life, s.Fx("dur", 6f)), 0f);
                if (ult) Put(gk, feet, 6.5f, col, life, 30f);
                yield break;
            }
            if (k == "dash")
            {
                Vector3 end = feet + F * Mathf.Min(reach, 14f);
                Put("skid", Vector3.Lerp(feet, end, 0.5f), Mathf.Min(reach, 14f) * 0.5f, col, life * 0.7f, Mathf.Atan2(F.x, F.z) * Mathf.Rad2Deg + 90f);
                GroundMark.Trail(extra != null && (extra == "paw" || extra == "claw") ? extra : "paw", feet, end, 1.9f, 0.55f * sz, col, life * 0.7f);
                Put(main == "skid" ? "crater" : main, end, 1.7f * sz, col, life, -1f);
                yield break;
            }
            if (k == "line" || k == "cone")
            {
                float len = Mathf.Min(reach, 16f); int n = Mathf.Clamp(Mathf.RoundToInt(len / 3f), 2, 6);
                Vector3 right = Vector3.Cross(Vector3.up, F);
                float yaw = Mathf.Atan2(F.x, F.z) * Mathf.Rad2Deg;
                for (int i = 1; i <= n; i++)
                {
                    float t = i / (float)n; Vector3 p = feet + F * len * t;
                    float spread = k == "cone" ? len * t * 0.3f : 0f;
                    int lanes = k == "cone" ? 3 : 1;
                    for (int j = 0; j < lanes; j++)
                    {
                        Vector3 pj = p + right * (lanes == 1 ? 0f : (j - 1) * spread);
                        Put(i % 2 == 0 && extra != null ? extra : main, pj, (0.9f + 0.45f * t) * sz * (lanes == 1 ? 1.2f : 0.9f), col, life, main == "claw" || main == "wind" || main == "slither" ? yaw : -1f);
                    }
                    yield return new WaitForSeconds(0.025f);
                }
                yield break;
            }
            // nova / ground / proyektil / lainnya
            Put(main, C, r * 0.95f * sz, col, life, -1f);
            int sc = (ult ? 7 : big ? 4 : 2) + (h % 3);
            Scatter(extra ?? main, C, r * 0.9f, sc, 0.5f * sz, 1.0f * sz, col, life * 0.8f);
            if (k == "nova" || ult)
            {
                string rk = main == "glyph" || main == "rune" ? main : "crack";
                RingOf(rk == "crack" ? "crack" : rk, C, r * 0.7f, ult ? 6 : 4, r * (rk == "crack" ? 0.55f : 0.9f), col, life * 0.8f);
            }
            yield return null;
        }
    }
}
