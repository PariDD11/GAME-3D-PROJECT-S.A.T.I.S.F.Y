using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Efek visual combat buatan kode (tanpa aset): tebasan melengkung, percikan kena, cincin kejut, debu dodge,
    /// panah terbang berekor, angka damage, dan efek skill. Semua memakai shader "Sprites/Default" (jalan di Built-in maupun URP).
    /// Palet: Swordsman = putih-biru baja (combo 3 = emas), Archer = hijau-kuning (skill es = biru muda, ultimate = emas).
    /// </summary>
    public static class CombatFx
    {
        public static readonly Color SwordCol = new Color(0.78f, 0.9f, 1f);
        public static readonly Color SwordGold = new Color(1f, 0.82f, 0.35f);
        public static readonly Color BowCol = new Color(0.75f, 1f, 0.55f);
        public static readonly Color IceCol = new Color(0.55f, 0.88f, 1f);
        public static readonly Color FireCol = new Color(1f, 0.55f, 0.2f);
        public static readonly Color EarthCol = new Color(0.72f, 0.55f, 0.32f);
        public static readonly Color MagmaCol = new Color(1f, 0.3f, 0.1f);
        public static readonly Color HurtCol = new Color(1f, 0.25f, 0.2f);

        static Shader sh;
        static Texture2D soft, ring, star;
        static Font font;

        static Shader Sh { get { if (sh == null) { sh = Shader.Find("Sprites/Default"); if (sh == null) sh = Shader.Find("Unlit/Transparent"); } return sh; } }

        static Shader glow; static bool glowTried;
        static Shader Glow { get { if (!glowTried) { glowTried = true; glow = Shader.Find("HunterGuild/Glow"); } return glow; } }

        /// <summary>Material efek: aditif HDR bercahaya. Warna gelap (asap, bekas hangus) otomatis memakai campuran alfa biasa.</summary>
        internal static Material Mat(Texture2D tex, Color c)
        {
            float mx = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            if (Glow == null || mx < 0.5f) return Solid(tex, c);
            var m = new Material(Glow);
            if (tex != null) m.mainTexture = tex;
            m.color = c;
            m.SetFloat("_Intensity", FxQuality.Glow);
            return m;
        }

        /// <summary>Material campuran alfa biasa (batu, debu, asap, bekas hangus).</summary>
        internal static Material Solid(Texture2D tex, Color c)
        {
            var m = new Material(Sh);
            if (tex != null) m.mainTexture = tex;
            m.color = c;
            return m;
        }

        // ---------------- tekstur prosedural ----------------
        static Texture2D MakeTex(int n, System.Func<float, float, float> f)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 2 };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                    byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(f(u, v)) * 255f);
                    px[y * n + x] = new Color32(255, 255, 255, a);
                }
            t.SetPixels32(px); t.Apply(true, true);
            return t;
        }

        /// <summary>Bola cahaya lembut beresolusi tinggi: inti panas + halo gaussian.</summary>
        internal static Texture2D Soft()
        {
            if (soft != null) return soft;
            soft = MakeTex(256, (u, v) =>
            {
                float d = Mathf.Sqrt(u * u + v * v);
                if (d >= 1f) return 0f;
                float halo = Mathf.Exp(-d * d * 5.5f);
                float core = Mathf.Pow(1f - d, 3f);
                float edge = 1f - Mathf.SmoothStep(0.7f, 1f, d);
                return (halo * 0.7f + core * 0.55f) * edge;
            });
            return soft;
        }

        /// <summary>Cincin gelombang kejut: garis tajam + pendar luar dan dalam.</summary>
        internal static Texture2D Ring()
        {
            if (ring != null) return ring;
            ring = MakeTex(512, (u, v) =>
            {
                float d = Mathf.Sqrt(u * u + v * v);
                if (d >= 1f) return 0f;
                float dd = Mathf.Abs(d - 0.86f);
                float line = Mathf.Exp(-dd * dd * 1700f);
                float glowOut = Mathf.Exp(-dd * dd * 120f) * 0.45f;
                float inner = d < 0.86f ? Mathf.Pow(d / 0.86f, 5f) * 0.18f : 0f;
                float edge = 1f - Mathf.SmoothStep(0.93f, 1f, d);
                return (line + glowOut + inner) * edge;
            });
            return ring;
        }

        /// <summary>Kilatan bintang: inti terang, 4 sinar panjang + 4 sinar diagonal pendek.</summary>
        internal static Texture2D Star()
        {
            if (star != null) return star;
            star = MakeTex(256, (u, v) =>
            {
                float ax = Mathf.Abs(u), ay = Mathf.Abs(v);
                float d = Mathf.Sqrt(u * u + v * v);
                if (d >= 1f) return 0f;
                float cross = Mathf.Exp(-Mathf.Min(ax, ay) * 38f) * Mathf.Pow(1f - Mathf.Max(ax, ay), 1.6f);
                float ur = (u + v) * 0.7071f, vr = (u - v) * 0.7071f;
                float diag = Mathf.Exp(-Mathf.Min(Mathf.Abs(ur), Mathf.Abs(vr)) * 55f) * Mathf.Pow(1f - d, 2.5f) * 0.55f;
                float core = Mathf.Exp(-d * d * 22f);
                float halo = Mathf.Exp(-d * d * 4f) * 0.25f;
                return cross + diag + core + halo;
            });
            return star;
        }

        // ---------------- host animasi sederhana ----------------
        internal class FxAnim : MonoBehaviour
        {
            public float life = 0.2f, age;
            public Vector3 s0 = Vector3.one, s1 = Vector3.one;
            public Vector3 vel;
            public float spin;
            public bool billboard;
            public Color color = Color.white;
            public float alpha0 = 1f;
            public new Light light;
            public float light0;
            Renderer[] rs;

            TextMesh tmx;
            void Start() { rs = GetComponentsInChildren<Renderer>(); tmx = GetComponent<TextMesh>(); }

            void Update()
            {
                age += Time.deltaTime;
                float t = Mathf.Clamp01(age / life);
                transform.localScale = Vector3.Lerp(s0, s1, 1f - (1f - t) * (1f - t));
                transform.position += vel * Time.deltaTime;
                if (spin != 0f) transform.Rotate(0f, spin * Time.deltaTime, 0f, Space.Self);
                if (billboard && Camera.main != null) transform.rotation = Camera.main.transform.rotation;
                float a = alpha0 * (1f - t * t);
                if (tmx != null) { var tc = color; tc.a *= a; tmx.color = tc; }
                else if (rs != null)
                    foreach (var r in rs)
                        if (r != null && r.material != null) { var c = color; c.a *= a; r.material.color = c; }
                if (light != null) light.intensity = light0 * (1f - t);
                if (age >= life) Destroy(gameObject);
            }
        }

        public class Host : MonoBehaviour
        {
            static Host inst;
            public static Host I
            {
                get
                {
                    if (inst == null) { var go = new GameObject("CombatFxHost"); inst = go.AddComponent<Host>(); }
                    return inst;
                }
            }
        }

        // ---------------- tebasan melengkung ----------------
        static Mesh crescent;

        static Mesh Crescent()
        {
            if (crescent != null) return crescent;
            const int seg = 20;
            var v = new List<Vector3>(); var c = new List<Color>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float u = i / (float)seg;                            // 0..1 sepanjang busur
                float a = Mathf.Lerp(-0.5f, 0.5f, u) * Mathf.PI;     // 180 derajat
                float thick = Mathf.Sin(u * Mathf.PI);              // menipis di ujung
                float rOut = 1f, rIn = 1f - 0.5f * thick;
                v.Add(new Vector3(Mathf.Sin(a) * rOut, 0f, Mathf.Cos(a) * rOut));
                v.Add(new Vector3(Mathf.Sin(a) * rIn, 0f, Mathf.Cos(a) * rIn));
                float al = Mathf.Pow(thick, 0.6f);
                c.Add(new Color(1f, 1f, 1f, al)); c.Add(new Color(1f, 1f, 1f, al * 0.25f));
                uv.Add(new Vector2(u, 1f)); uv.Add(new Vector2(u, 0f));
                if (i < seg)
                {
                    int b = i * 2;
                    t.Add(b); t.Add(b + 2); t.Add(b + 1);
                    t.Add(b + 1); t.Add(b + 2); t.Add(b + 3);
                    t.Add(b); t.Add(b + 1); t.Add(b + 2);      // sisi belakang
                    t.Add(b + 1); t.Add(b + 3); t.Add(b + 2);
                }
            }
            crescent = new Mesh { name = "FxCrescent" };
            MeshSafe.Clean(v); crescent.SetVertices(v); crescent.SetColors(c); crescent.SetUVs(0, uv);
            // sisi depan + belakang digambar dengan winding berbeda
            var tris = new List<int>();
            for (int i = 0; i < seg; i++)
            {
                int b = i * 2;
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 1); tris.Add(b + 1); tris.Add(b + 2); tris.Add(b + 3);
                tris.Add(b); tris.Add(b + 1); tris.Add(b + 2); tris.Add(b + 1); tris.Add(b + 3); tris.Add(b + 2);
            }
            crescent.SetTriangles(tris, 0);
            crescent.RecalculateBounds();
            return crescent;
        }

        /// <summary>Tebasan melengkung di depan pemain. rollDeg: 0 = mendatar, 90 = vertikal (tebasan dari atas). radius = jangkauan visual.</summary>
        public static void SlashArc(Vector3 pos, Quaternion rot, float radius, float rollDeg, Color col, float life = 0.2f, float widthMul = 1f)
        {
            var go = new GameObject("FxSlash");
            go.transform.position = pos;
            go.transform.rotation = rot * Quaternion.Euler(0f, 0f, rollDeg);
            var mf = go.AddComponent<MeshFilter>(); mf.sharedMesh = Crescent();
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = Mat(null, col);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var fx = go.AddComponent<FxAnim>();
            fx.life = life; fx.color = col;
            fx.s0 = new Vector3(radius * 0.75f, 1f, radius * 0.75f * widthMul);
            fx.s1 = new Vector3(radius * 1.12f, 1f, radius * 1.12f * widthMul);
            fx.spin = 0f;
            // inti putih-panas yang lebih tipis di dalam tebasan
            Color hot = Color.Lerp(col, Color.white, 0.75f);
            var g2 = new GameObject("FxSlashCore");
            g2.transform.position = pos;
            g2.transform.rotation = go.transform.rotation;
            g2.AddComponent<MeshFilter>().sharedMesh = Crescent();
            var mr2 = g2.AddComponent<MeshRenderer>(); mr2.sharedMaterial = Mat(null, hot);
            mr2.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var f2 = g2.AddComponent<FxAnim>();
            f2.life = life * 0.85f; f2.color = hot;
            f2.s0 = new Vector3(radius * 0.78f, 1f, radius * 0.78f * widthMul * 0.55f);
            f2.s1 = new Vector3(radius * 1.1f, 1f, radius * 1.1f * widthMul * 0.55f);
            // percikan bara sepanjang tebasan
            Burst(pos + rot * Vector3.forward * radius * 0.7f, col, 10, radius * 0.45f, 2.4f, 0.5f, 0.1f);
        }

        // ---------------- tebasan menyapu (SweepArc) ----------------
        static readonly Dictionary<string, Mesh> arcMeshes = new Dictionary<string, Mesh>();

        /// <summary>Baji busur: kepala (a=0) paling terang, ekor (a=deg) memudar. Berada di bidang XZ, kepala menghadap +Z.</summary>
        static Mesh ArcMesh(float deg, float thick)
        {
            string key = Mathf.RoundToInt(deg) + "_" + Mathf.RoundToInt(thick * 100f);
            Mesh m;
            if (arcMeshes.TryGetValue(key, out m) && m != null) return m;
            int seg = Mathf.Max(6, Mathf.RoundToInt(deg / 4f));
            var v = new List<Vector3>(); var c = new List<Color>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float u = i / (float)seg;                       // 0 kepala .. 1 ekor
                float a = u * deg * Mathf.Deg2Rad;
                float rOut = 1f, rIn = 1f - thick * (1f - u * 0.55f);
                v.Add(new Vector3(Mathf.Sin(a) * rOut, 0f, Mathf.Cos(a) * rOut));
                v.Add(new Vector3(Mathf.Sin(a) * rIn, 0f, Mathf.Cos(a) * rIn));
                float al = Mathf.Pow(1f - u, 1.6f);
                c.Add(new Color(1f, 1f, 1f, al)); c.Add(new Color(1f, 1f, 1f, al * 0.2f));
                uv.Add(new Vector2(u, 1f)); uv.Add(new Vector2(u, 0f));
                if (i < seg) { int b = i * 2; t.Add(b); t.Add(b + 2); t.Add(b + 1); t.Add(b + 1); t.Add(b + 2); t.Add(b + 3); t.Add(b); t.Add(b + 1); t.Add(b + 2); t.Add(b + 1); t.Add(b + 3); t.Add(b + 2); }
            }
            m = new Mesh { name = "HGArc" + key };
            MeshSafe.Clean(v); m.SetVertices(v); m.SetColors(c); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateBounds();
            arcMeshes[key] = m; return m;
        }

        class ArcSweep : MonoBehaviour
        {
            public Transform follow; public Vector3 offset; public float yaw0, yaw1, dur, fade, radius, age; public Color color, hot; public bool sparks;
            MeshRenderer mr; float nextSpark;
            void Start() { mr = GetComponent<MeshRenderer>(); }
            void Update()
            {
                age += Time.deltaTime;
                float u = Mathf.Clamp01(age / Mathf.Max(0.01f, dur));
                float e = 1f - (1f - u) * (1f - u) * (1f - u);
                float yaw = Mathf.Lerp(yaw0, yaw1, e);
                var q = Quaternion.Euler(0f, yaw, 0f);
                transform.rotation = baseRot * q;
                float a = age < dur ? 1f : 1f - Mathf.Clamp01((age - dur) / Mathf.Max(0.01f, fade));
                if (mr != null) { var c = color; c.a *= a; mr.material.color = c; }
                if (sparks && age < dur && age >= nextSpark)
                {
                    nextSpark = age + 0.035f;
                    Vector3 head = transform.position + (baseRot * Quaternion.Euler(0f, yaw, 0f)) * Vector3.forward * radius;
                    Burst(head, hot, 3, 0.1f, 2f, 0.45f, 0.1f);
                }
                if (age >= dur + fade) Destroy(gameObject);
            }
            public Quaternion baseRot;
        }

        /// <summary>
        /// Tebasan menyapu: baji busur yang kepalanya bergerak dari yaw0 ke yaw1 (derajat relatif depan, positif = kanan), kepala terang, ekor memudar,
        /// percikan di ujung. wedge = panjang ekor (derajat), thick = ketebalan 0..1.
        /// </summary>
        public static void SweepArc(Vector3 pos, Quaternion baseRot, float radius, float wedge, float thick, float yaw0, float yaw1, float dur, float fade, Color col, bool sparks = true)
        {
            var go = new GameObject("FxSweepArc");
            go.transform.position = pos;
            go.transform.rotation = baseRot * Quaternion.Euler(0f, yaw0, 0f);
            go.transform.localScale = new Vector3(radius, 1f, radius);
            go.AddComponent<MeshFilter>().sharedMesh = ArcMesh(wedge, thick);
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = Mat(null, col);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var a = go.AddComponent<ArcSweep>();
            a.baseRot = baseRot; a.yaw0 = yaw0; a.yaw1 = yaw1; a.dur = dur; a.fade = fade; a.radius = radius;
            a.color = col; a.hot = Color.Lerp(col, Color.white, 0.6f); a.sparks = sparks;
        }

        static Texture2D halfRing;
        /// <summary>Gelombang kejut setengah lingkaran di tanah, menghadap arah forward.</summary>
        public static void HalfWave(Vector3 pos, Vector3 forward, float maxRadius, Color col, float life = 0.35f)
        {
            if (halfRing == null)
                halfRing = MakeTex(256, (u, v) =>
                {
                    float d = Mathf.Sqrt(u * u + v * v);
                    if (d >= 1f || v < -0.02f) return 0f;
                    float dd = Mathf.Abs(d - 0.86f);
                    float line = Mathf.Exp(-dd * dd * 900f) + Mathf.Exp(-dd * dd * 90f) * 0.4f;
                    return line * Mathf.SmoothStep(-0.02f, 0.12f, v) * (1f - Mathf.SmoothStep(0.93f, 1f, d));
                });
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(q.GetComponent<Collider>());
            q.name = "FxHalfWave";
            float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            q.transform.position = pos + Vector3.up * 0.08f;
            q.transform.rotation = Quaternion.Euler(90f, yaw, 0f);
            q.GetComponent<MeshRenderer>().sharedMaterial = Mat(halfRing, col);
            var fx = q.AddComponent<FxAnim>();
            fx.life = life; fx.s0 = Vector3.one * 0.5f; fx.s1 = Vector3.one * maxRadius * 2f; fx.color = col;
        }

        /// <summary>Warna kedua (geser rona sedikit, lebih terang) supaya percikan tampak berwarna-warni.</summary>
        internal static Color Shift(Color c, float hue = 0.06f)
        {
            float h, s, v; Color.RGBToHSV(c, out h, out s, out v);
            Color r = Color.HSVToRGB(Mathf.Repeat(h + hue, 1f), Mathf.Clamp01(s * 0.85f), 1f);
            r.a = c.a; return r;
        }

        // ---------------- percikan & kilatan ----------------
        public static void HitSpark(Vector3 pos, Color col, float size = 1f, int count = 14)
        {
            // kilatan bintang
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(q.GetComponent<Collider>());
            q.name = "FxStar";
            q.transform.position = pos;
            q.GetComponent<MeshRenderer>().sharedMaterial = Mat(Star(), col);
            var fx = q.AddComponent<FxAnim>();
            fx.life = 0.14f; fx.s0 = Vector3.one * 0.5f * size; fx.s1 = Vector3.one * 1.8f * size; fx.billboard = true; fx.color = col;
            // percikan
            var go = new GameObject("FxSparks");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 0.4f; main.loop = false; main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f * size, 8f * size);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f * size, 0.17f * size);
            main.startColor = new ParticleSystem.MinMaxGradient(col, Shift(col));
            main.gravityModifier = 1.4f;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var em = ps.emission; em.enabled = true; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)FxQuality.N(count)) });
            var sp = ps.shape; sp.enabled = true; sp.shapeType = ParticleSystemShapeType.Sphere; sp.radius = 0.1f;
            var col2 = ps.colorOverLifetime; col2.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(col, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col2.color = g;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mat(Soft(), Color.white);
            ps.Play();
            RoyalFx.HitExtra(pos, col, size);   // v35z102n6: kilau + halo + serpihan emas
        }

        public static void ShockRing(Vector3 pos, float maxRadius, Color col, float life = 0.35f)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(q.GetComponent<Collider>());
            q.name = "FxRing";
            q.transform.position = pos + Vector3.up * 0.08f;
            q.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            q.GetComponent<MeshRenderer>().sharedMaterial = Mat(Ring(), col);
            var fx = q.AddComponent<FxAnim>();
            fx.life = life; fx.s0 = Vector3.one * 0.4f; fx.s1 = Vector3.one * maxRadius * 2f; fx.color = col;
            RoyalFx.RingExtra(pos, maxRadius, col);   // v35z102n6: gelombang kedua emas
        }

        /// <summary>Cahaya titik singkat (kilatan senjata / ledakan).</summary>
        public static void Flash(Vector3 pos, Color col, float range, float intensity, float life)
        {
            var go = new GameObject("FxLight");
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point; l.color = col; l.range = range; l.intensity = intensity; l.shadows = LightShadows.None;
            var fx = go.AddComponent<FxAnim>();
            fx.life = life; fx.light = l; fx.light0 = intensity; fx.alpha0 = 0f;
        }

        public static void Dust(Vector3 pos, int count = 10)
        {
            var go = new GameObject("FxDust");
            go.transform.position = pos + Vector3.up * 0.1f;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 0.5f; main.loop = false; main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 2.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startColor = new Color(0.78f, 0.72f, 0.6f, 0.55f);
            main.gravityModifier = -0.05f;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var em = ps.emission; em.enabled = true; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var sp = ps.shape; sp.enabled = true; sp.shapeType = ParticleSystemShapeType.Circle; sp.radius = 0.4f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.4f));
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = Solid(Soft(), Color.white);
            ps.Play();
        }

        /// <summary>Semburan partikel naik (aura buff, penyembuhan, es).</summary>
        public static void Burst(Vector3 pos, Color col, int count, float radius, float up, float life = 0.9f, float size = 0.18f)
        {
            var go = new GameObject("FxBurst");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 0.5f; main.loop = false; main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, up);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(col, Shift(col));
            main.gravityModifier = -0.2f;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var em = ps.emission; em.enabled = true; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)FxQuality.N(count)) });
            var sp = ps.shape; sp.enabled = true; sp.shapeType = ParticleSystemShapeType.Circle; sp.radius = radius;
            var cl = ps.colorOverLifetime; cl.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(col, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            cl.color = g;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = Mat(Soft(), Color.white);
            ps.Play();
        }

        // ---------------- angka damage ----------------
        /// <summary>Label melayang kecil ("HEADSHOT!", "CRIT!").</summary>
        public static void PopText(Vector3 pos, string text, Color col)
        {
            if (font == null)
            {
#if UNITY_2022_2_OR_NEWER
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
            }
            if (font == null) return;
            var go = new GameObject("FxPop");
            go.transform.position = pos;
            var tm = go.AddComponent<TextMesh>();
            tm.font = font; tm.text = text; tm.fontSize = 64; tm.characterSize = 0.06f;
            tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.fontStyle = FontStyle.Bold; tm.color = col;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            var fx = go.AddComponent<FxAnim>();
            fx.life = 0.9f; fx.vel = new Vector3(0f, 1.3f, 0f); fx.billboard = true; fx.color = col;
            fx.s0 = Vector3.one * 1.5f; fx.s1 = Vector3.one;
        }

        public static void DamageNumber(Vector3 pos, float dmg, bool big, Color col)
        {
            if (font == null)
            {
#if UNITY_2022_2_OR_NEWER
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
            }
            if (font == null) return;
            var go = new GameObject("FxDmg");
            go.transform.position = pos + new Vector3(Random.Range(-0.3f, 0.3f), 0.3f, Random.Range(-0.3f, 0.3f));
            var tm = go.AddComponent<TextMesh>();
            bool anime = AnimeLook.On;
            tm.font = font; tm.text = Mathf.RoundToInt(dmg).ToString() + (anime && big ? "!" : "");
            tm.fontSize = 64; tm.characterSize = big ? (anime ? 0.095f : 0.075f) : (anime ? 0.058f : 0.05f);
            tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
            tm.fontStyle = big ? FontStyle.Bold : FontStyle.Normal;
            if (big && RoyalFx.On) { tm.color = Color.Lerp(col, RoyalFx.Gold, 0.45f); tm.characterSize *= 1.18f; col = tm.color; }   // v35z102n6: krit keemasan lebih besar
            else tm.color = col;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            var fx = go.AddComponent<FxAnim>();
            fx.life = big ? 0.9f : 0.7f; fx.vel = new Vector3(0f, 1.6f, 0f); fx.billboard = true; fx.color = col;
            fx.s0 = Vector3.one * (big ? 1.4f : 1f); fx.s1 = Vector3.one * (big ? 1f : 0.85f);
            if (anime)
            {
                // gaya anime: angka membesar mendadak lalu mengecil ("pop"), kontur gelap tebal, naik lebih cepat
                fx.s0 = Vector3.one * (big ? 2.3f : 1.45f); fx.vel = new Vector3(0f, big ? 2.1f : 1.8f, 0f);
                Color oc = big ? new Color(0.38f, 0.05f, 0.02f) : new Color(0.04f, 0.05f, 0.13f);
                float o = big ? 0.045f : 0.03f;
                for (int i = 0; i < 4; i++)
                {
                    var ch = new GameObject("Ol"); ch.transform.SetParent(go.transform, false);
                    ch.transform.localPosition = new Vector3(i < 2 ? o : -o, (i % 2 == 0 ? o : -o), 0.02f);
                    var ct = ch.AddComponent<TextMesh>();
                    ct.font = font; ct.text = tm.text; ct.fontSize = tm.fontSize; ct.characterSize = tm.characterSize; ct.anchor = tm.anchor; ct.alignment = tm.alignment; ct.fontStyle = tm.fontStyle; ct.color = oc;
                    ch.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                    var f = ch.AddComponent<OutlineFade>(); f.src = tm; f.me = ct; f.col = oc;
                }
            }
        }

        /// <summary>Kontur angka damage ikut memudar bersama angka utamanya.</summary>
        class OutlineFade : MonoBehaviour
        {
            public TextMesh src, me; public Color col;
            void LateUpdate() { if (src == null || me == null) return; var c = col; c.a = src.color.a; me.color = c; }
        }

        // ---------------- panah terbang ----------------
        class Projectile : MonoBehaviour
        {
            public Vector3 target; public float speed = 95f; public System.Action onHit; public Color col;
            bool done;
            void Update()
            {
                if (done) return;
                Vector3 d = target - transform.position;
                float step = speed * Time.deltaTime;
                if (d.magnitude <= step)
                {
                    transform.position = target; done = true;
                    try { if (onHit != null) onHit(); } catch (System.Exception ex) { Debug.LogException(ex); }   // v35z102n10
                    foreach (var r in GetComponentsInChildren<MeshRenderer>()) r.enabled = false;
                    Destroy(gameObject, 0.5f);
                    return;
                }
                transform.position += d.normalized * step;
            }
        }

        /// <summary>Panah berekor terbang dari origin ke target. thick: 1 = normal, &gt;1 = tebal (skill).</summary>
        public static void Arrow(Vector3 origin, Vector3 target, Color col, float thick = 1f, float speed = 95f, System.Action onHit = null)
        {
            Vector3 dir = target - origin;
            if (dir.sqrMagnitude < 0.01f) { if (onHit != null) onHit(); return; }
            var go = new GameObject("FxArrow");
            go.transform.position = origin;
            go.transform.rotation = Quaternion.LookRotation(dir.normalized);
            var shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(shaft.GetComponent<Collider>());
            shaft.transform.SetParent(go.transform, false);
            shaft.transform.localScale = new Vector3(0.035f * thick, 0.035f * thick, 0.75f);
            shaft.GetComponent<MeshRenderer>().sharedMaterial = Mat(null, Color.Lerp(col, Color.white, 0.6f));
            var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(head.GetComponent<Collider>());
            head.transform.SetParent(go.transform, false);
            head.transform.localPosition = new Vector3(0f, 0f, 0.4f);
            head.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            head.transform.localScale = new Vector3(0.09f * thick, 0.09f * thick, 0.16f);
            head.GetComponent<MeshRenderer>().sharedMaterial = Mat(null, Color.white);
            var tr = go.AddComponent<TrailRenderer>();
            tr.time = 0.22f; tr.minVertexDistance = 0.1f;
            tr.widthCurve = new AnimationCurve(new Keyframe(0f, 0.16f * thick), new Keyframe(1f, 0f));
            tr.sharedMaterial = Mat(Soft(), Color.white);
            var gr = new Gradient();
            gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(col, 0.4f), new GradientColorKey(col, 1f) },
                       new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0f, 1f) });
            tr.colorGradient = gr;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var p = go.AddComponent<Projectile>();
            p.target = target; p.speed = speed; p.col = col; p.onHit = onHit;
        }

        /// <summary>Bola sihir terbang berekor (api / batu). size: diameter bola dalam meter.</summary>
        public static void Orb(Vector3 origin, Vector3 target, Color col, float size, float speed = 40f, System.Action onHit = null)
        {
            Vector3 dir = target - origin;
            if (dir.sqrMagnitude < 0.01f) { if (onHit != null) onHit(); return; }
            var go = new GameObject("FxOrb");
            go.transform.position = origin;
            go.transform.rotation = Quaternion.LookRotation(dir.normalized);
            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(core.GetComponent<Collider>());
            core.transform.SetParent(go.transform, false);
            core.transform.localScale = Vector3.one * size;
            core.GetComponent<MeshRenderer>().sharedMaterial = Mat(null, Color.Lerp(col, Color.white, 0.55f));
            var halo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(halo.GetComponent<Collider>());
            halo.transform.SetParent(go.transform, false);
            halo.transform.localScale = Vector3.one * size * 1.8f;
            var hc = col; hc.a = 0.35f;
            halo.GetComponent<MeshRenderer>().sharedMaterial = Mat(Soft(), hc);
            var tr = go.AddComponent<TrailRenderer>();
            tr.time = 0.3f; tr.minVertexDistance = 0.08f;
            tr.widthCurve = new AnimationCurve(new Keyframe(0f, size * 0.9f), new Keyframe(1f, 0f));
            tr.sharedMaterial = Mat(Soft(), Color.white);
            var gr = new Gradient();
            gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(col, 0.35f), new GradientColorKey(col, 1f) },
                       new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0f, 1f) });
            tr.colorGradient = gr;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var l = new GameObject("L").AddComponent<Light>(); l.transform.SetParent(go.transform, false);
            l.type = LightType.Point; l.color = col; l.range = 5f + size * 3f; l.intensity = 1.6f;
            var p = go.AddComponent<Projectile>();
            p.target = target; p.speed = speed; p.col = col; p.onHit = onHit;
        }

        /// <summary>Kilatan di ujung busur.</summary>
        public static void Muzzle(Vector3 pos, Color col)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(q.GetComponent<Collider>());
            q.name = "FxMuzzle";
            q.transform.position = pos;
            q.GetComponent<MeshRenderer>().sharedMaterial = Mat(Soft(), col);
            var fx = q.AddComponent<FxAnim>();
            fx.life = 0.12f; fx.s0 = Vector3.one * 0.3f; fx.s1 = Vector3.one * 0.9f; fx.billboard = true; fx.color = col;
            Flash(pos, col, 6f, 1.2f, 0.12f);
        }

        // ---------------- pola kait ----------------
        /// <summary>Cakram bertanda (zona perangkap / jaring) di tanah selama life detik.</summary>
        public static GameObject Zone(Vector3 pos, float radius, Color col, float life)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(q.GetComponent<Collider>());
            q.name = "FxZone";
            q.transform.position = pos + Vector3.up * 0.1f;
            q.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            q.transform.localScale = Vector3.one * radius * 2f;
            q.GetComponent<MeshRenderer>().sharedMaterial = Mat(Ring(), col);
            var inner = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(inner.GetComponent<Collider>());
            inner.transform.SetParent(q.transform, false);
            inner.transform.localPosition = new Vector3(0f, 0f, -0.001f);
            var ic = col; ic.a = 0.25f;
            inner.GetComponent<MeshRenderer>().sharedMaterial = Mat(Soft(), ic);
            Object.Destroy(q, life);
            return q;
        }

        /// <summary>Garis cahaya lurus (tembakan menembus, sinar ultimate).</summary>
        public static void Beam(Vector3 a, Vector3 b, Color col, float width, float life)
        {
            Vector3 d = b - a;
            if (d.sqrMagnitude < 0.01f) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = "FxBeam";
            go.transform.position = (a + b) / 2f;
            go.transform.rotation = Quaternion.LookRotation(d.normalized);
            go.GetComponent<MeshRenderer>().sharedMaterial = Mat(null, col);
            var fx = go.AddComponent<FxAnim>();
            fx.life = life; fx.s0 = new Vector3(width, width, d.magnitude); fx.s1 = new Vector3(width * 0.15f, width * 0.15f, d.magnitude); fx.color = col;
        }
    }
}
