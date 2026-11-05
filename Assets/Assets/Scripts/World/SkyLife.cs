using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// Kehidupan langit untuk semua scene luar ruangan: matahari, bulan + bintang saat malam, awan yang terus bergeser,
    /// dan kawanan burung yang terbang berputar di siang hari. Dipasang otomatis saat scene dimuat (butuh TownDayNight sebagai
    /// sumber arah matahari), tanpa perlu build ulang scene.
    /// </summary>
    public class SkyLife : MonoBehaviour
    {
        // ---------- pemasangan otomatis ----------
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
            Install();
        }
        static void OnLoaded(Scene s, LoadSceneMode m) { Install(); }

        static void Install()
        {
            if (FindFirstObjectByType<SkyLife>() != null) return;
            var dn = FindFirstObjectByType<TownDayNight>();
            if (dn == null || dn.sun == null) return;
            if (FindFirstObjectByType<TowerManager>() != null) return;
            if (Resources.Load<Shader>("SkyDisc") == null) return;
            var go = new GameObject("SkyLife");
            go.AddComponent<SkyLife>().sun = dn.sun;
        }

        // ---------- isi ----------
        public Light sun;
        Camera cam; Material mat; float R = 600f;
        Transform sunT, sunGlowT, moonT, moonGlowT, starsT;
        Material sunMat, sunGlowMat, moonMat, moonGlowMat, starMat, birdMat;
        class Cloud { public Transform t; public float az, el, speed, size, aspect; public Material m; }
        readonly List<Cloud> clouds = new List<Cloud>();
        class Bird { public Transform root, wl, wr; public float phase, flap, rOff, aOff, hOff; }
        class Flock { public List<Bird> birds = new List<Bird>(); public float ang, rad, alt, spd, cx, cz; public Vector2 drift; }
        readonly List<Flock> flocks = new List<Flock>();
        Transform birdRoot;
        float windAz;

        static Mesh quad;
        static Mesh Quad()
        {
            if (quad != null) return quad;
            quad = new Mesh { name = "SkyQuad" };
            quad.vertices = new[] { new Vector3(-.5f, -.5f, 0), new Vector3(-.5f, .5f, 0), new Vector3(.5f, .5f, 0), new Vector3(.5f, -.5f, 0) };
            quad.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
            quad.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            quad.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            quad.RecalculateBounds();
            return quad;
        }

        static Texture2D Tex(int w, int h, System.Func<float, float, Color> f)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) px[y * w + x] = f((x + .5f) / w, (y + .5f) / h);
            t.SetPixels(px); t.Apply();
            return t;
        }

        Material NewMat(Texture2D tex)
        {
            var m = new Material(mat);
            m.mainTexture = tex;
            return m;
        }

        Transform MakeQuad(string name, Transform parent, Material m)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = Quad();
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go.transform;
        }

        void Start()
        {
            cam = Camera.main;
            mat = new Material(Resources.Load<Shader>("SkyDisc"));
            R = Mathf.Clamp((cam != null ? cam.farClipPlane : 1000f) * 0.8f, 150f, 800f);

            // matahari
            sunMat = NewMat(Tex(64, 64, (u, v) => { float d = Vector2.Distance(new Vector2(u, v), new Vector2(.5f, .5f)) * 2f; return new Color(1, 1, 1, Mathf.Clamp01((1f - d) * 14f)); }));
            sunGlowMat = NewMat(Tex(64, 64, (u, v) => { float d = Vector2.Distance(new Vector2(u, v), new Vector2(.5f, .5f)) * 2f; float a = Mathf.Clamp01(1f - d); return new Color(1, 1, 1, a * a * a); }));
            // bulan (dengan kawah samar)
            moonMat = NewMat(Tex(96, 96, (u, v) =>
            {
                var p = new Vector2(u, v); float d = Vector2.Distance(p, new Vector2(.5f, .5f)) * 2f;
                float a = Mathf.Clamp01((1f - d) * 16f);
                float c = 1f - 0.18f * Crater(p, .38f, .62f, .12f) - 0.14f * Crater(p, .62f, .4f, .16f) - 0.1f * Crater(p, .5f, .25f, .09f) - 0.1f * Crater(p, .3f, .35f, .07f);
                return new Color(0.92f * c, 0.94f * c, 1f * c, a);
            }));
            moonGlowMat = NewMat(sunGlowMat.mainTexture as Texture2D);
            starMat = NewMat(Tex(16, 16, (u, v) => { float d = Vector2.Distance(new Vector2(u, v), new Vector2(.5f, .5f)) * 2f; return new Color(1, 1, 1, Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d)); }));

            sunT = MakeQuad("Sun", transform, sunMat);
            sunGlowT = MakeQuad("SunGlow", transform, sunGlowMat);
            moonT = MakeQuad("Moon", transform, moonMat);
            moonGlowT = MakeQuad("MoonGlow", transform, moonGlowMat);
            BuildStars();
            BuildClouds();
            BuildBirds();
        }

        static float Crater(Vector2 p, float cx, float cy, float r) { float d = Vector2.Distance(p, new Vector2(cx, cy)); return Mathf.Clamp01(1f - d / r); }

        void BuildStars()
        {
            var go = new GameObject("Stars"); go.transform.SetParent(transform, false); starsT = go.transform;
            var rnd = new System.Random(77);
            var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var cols = new List<Color>(); var tris = new List<int>();
            for (int i = 0; i < 260; i++)
            {
                float az = (float)rnd.NextDouble() * 360f, el = Mathf.Lerp(6f, 88f, (float)rnd.NextDouble());
                Vector3 dir = Quaternion.Euler(-el, az, 0f) * Vector3.forward;
                Vector3 c = dir * R * 0.97f;
                Vector3 right = Vector3.Cross(Vector3.up, dir).normalized, up = Vector3.Cross(dir, right);
                float s = R * (0.0035f + 0.006f * (float)rnd.NextDouble());
                float br = 0.45f + 0.55f * (float)rnd.NextDouble();
                int b = verts.Count;
                verts.Add(c - right * s - up * s); verts.Add(c - right * s + up * s); verts.Add(c + right * s + up * s); verts.Add(c + right * s - up * s);
                uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(0, 1)); uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(1, 0));
                var col = new Color(0.85f + 0.15f * br, 0.9f + 0.1f * br, 1f, br);
                for (int k = 0; k < 4; k++) cols.Add(col);
                tris.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            }
            var m = new Mesh { name = "Stars" };
            MeshSafe.Clean(verts); m.SetVertices(verts); m.SetUVs(0, uvs); m.SetColors(cols); m.SetTriangles(tris, 0);
            m.bounds = new Bounds(Vector3.zero, Vector3.one * R * 3f);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = starMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        }

        /// <summary>Awan kumulus bergaya anime: gumpalan bulat bertepi tegas, dasar rata, sisi bawah berbayang biru-lavender.</summary>
        static Texture2D CumulusTex(int seed)
        {
            var rnd = new System.Random(seed);
            int np = 8 + (seed % 3);
            var cx = new float[np]; var cy = new float[np]; var rr0 = new float[np];
            for (int i = 0; i < np; i++)
            {
                float t = (i + 0.5f) / np;
                float body = Mathf.Sin(t * Mathf.PI);                                  // gumpalan tengah paling besar
                rr0[i] = (0.085f + 0.17f * body) * (0.75f + 0.25f * (float)rnd.NextDouble());
                cx[i] = (0.16f + 0.68f * t + ((float)rnd.NextDouble() - 0.5f) * 0.05f) * 2f;      // satuan sumbu-v (tekstur 2:1)
                cy[i] = 0.2f + rr0[i] * 0.72f;
            }
            const int W = 256, H = 128;
            var tx = new Texture2D(W, H, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[W * H];
            Color lit = new Color(1f, 0.995f, 0.97f), shade = new Color(0.78f, 0.84f, 0.97f), warmTop = new Color(1f, 0.97f, 0.9f);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = (x + 0.5f) / W * 2f, v = (y + 0.5f) / H;
                    float best = -1f; int bi = 0;
                    for (int i = 0; i < np; i++)
                    {
                        float dx = u - cx[i], dy = v - cy[i];
                        float e = rr0[i] - Mathf.Sqrt(dx * dx + dy * dy);
                        if (e > best) { best = e; bi = i; }
                    }
                    float flat = Mathf.Clamp01((v - 0.2f) / 0.012f);                      // dasar awan rata
                    float a = Mathf.Clamp01(best / 0.016f) * flat;
                    if (a <= 0f) { px[y * W + x] = new Color(1f, 1f, 1f, 0f); continue; }
                    float local = (v - (cy[bi] - rr0[bi])) / (2f * rr0[bi]);              // 0 dasar gumpalan .. 1 puncak
                    float side = (u - cx[bi]) / rr0[bi];                                  // -1 kiri .. 1 kanan
                    float band = 0.34f - 0.12f * side;                                    // sumber cahaya dari kiri-atas
                    float sh = Mathf.Clamp01((band - local) / 0.045f);
                    Color c = Color.Lerp(Color.Lerp(lit, warmTop, Mathf.Clamp01((local - 0.6f) * 2.5f)), shade, sh);
                    px[y * W + x] = new Color(c.r, c.g, c.b, a * 0.97f);
                }
            tx.SetPixels(px); tx.Apply();
            return tx;
        }

        void BuildClouds()
        {
            // awan lebih kecil, lebih sedikit, tersebar rata di keliling langit (dulu: 22 awan raksasa saling menimpa = "langit bercak putih")
            var tex = new Texture2D[6];
            for (int k = 0; k < tex.Length; k++) tex[k] = CumulusTex(11 + k * 7);
            var rr = new System.Random(5);
            int n = 16;
            for (int i = 0; i < n; i++)
            {
                var c = new Cloud();
                c.m = NewMat(tex[i % tex.Length]);
                c.t = MakeQuad("Cloud" + i, transform, c.m);
                c.az = (i + (float)rr.NextDouble() * 0.7f) * (360f / n);
                c.el = Mathf.Lerp(7f, 40f, Mathf.Pow((float)rr.NextDouble(), 0.85f));
                c.speed = Mathf.Lerp(0.6f, 1.4f, (float)rr.NextDouble());
                c.size = R * Mathf.Lerp(0.15f, 0.29f, (float)rr.NextDouble());
                c.aspect = Mathf.Lerp(0.4f, 0.56f, (float)rr.NextDouble());
                clouds.Add(c);
            }
        }

        void BuildBirds()
        {
            birdRoot = new GameObject("Burung").transform; birdRoot.SetParent(transform, false);
            birdMat = new Material(mat); birdMat.color = new Color(0.12f, 0.12f, 0.14f, 1f);
            var rnd = new System.Random(31);
            var wing = new Mesh { name = "BirdWing" };
            wing.vertices = new[] { new Vector3(0, 0, 0.35f), new Vector3(0, 0, -0.3f), new Vector3(1.1f, 0.05f, -0.25f), new Vector3(1.0f, 0.02f, 0.05f) };
            wing.uv = new[] { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };
            wing.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            wing.triangles = new[] { 0, 1, 2, 0, 2, 3 }; wing.RecalculateBounds();
            var body = new Mesh { name = "BirdBody" };
            body.vertices = new[] { new Vector3(0, 0, 0.5f), new Vector3(-0.13f, 0, -0.35f), new Vector3(0.13f, 0, -0.35f), new Vector3(0, 0.1f, -0.1f) };
            body.uv = new[] { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };
            body.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            body.triangles = new[] { 0, 1, 3, 0, 3, 2, 0, 2, 1, 1, 2, 3 }; body.RecalculateBounds();
            for (int f = 0; f < 3; f++)
            {
                var fl = new Flock { ang = (float)rnd.NextDouble() * 360f, rad = 28f + f * 22f, alt = 22f + f * 9f, spd = (f % 2 == 0 ? 1f : -1f) * (7f + f * 1.5f) };
                int nb = 4 + f;
                for (int b = 0; b < nb; b++)
                {
                    var bd = new Bird { phase = (float)rnd.NextDouble() * 6.28f, flap = 7f + (float)rnd.NextDouble() * 3f, rOff = (float)(rnd.NextDouble() - .5) * 9f, aOff = (float)(rnd.NextDouble() - .5) * 14f, hOff = (float)(rnd.NextDouble() - .5) * 5f };
                    var go = new GameObject("Burung"); go.transform.SetParent(birdRoot, false); bd.root = go.transform;
                    go.AddComponent<MeshFilter>().sharedMesh = body;
                    var r0 = go.AddComponent<MeshRenderer>(); r0.sharedMaterial = birdMat; r0.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    var wl = new GameObject("SayapKiri"); wl.transform.SetParent(go.transform, false);
                    wl.AddComponent<MeshFilter>().sharedMesh = wing; var r1 = wl.AddComponent<MeshRenderer>(); r1.sharedMaterial = birdMat; r1.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    wl.transform.localScale = new Vector3(-1f, 1f, 1f);
                    var wr = new GameObject("SayapKanan"); wr.transform.SetParent(go.transform, false);
                    wr.AddComponent<MeshFilter>().sharedMesh = wing; var r2 = wr.AddComponent<MeshRenderer>(); r2.sharedMaterial = birdMat; r2.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    bd.wl = wl.transform; bd.wr = wr.transform;
                    fl.birds.Add(bd);
                }
                flocks.Add(fl);
            }
        }

        void LateUpdate()
        {
            if (cam == null || !cam.isActiveAndEnabled) { cam = Camera.main; if (cam == null) return; }
            if (sun == null) return;
            transform.position = cam.transform.position;

            Vector3 sd = -sun.transform.forward;                  // arah menuju matahari
            float dayF = Mathf.Clamp01((sd.y + 0.12f) / 0.4f);    // 0 malam .. 1 siang
            float nightF = 1f - dayF;

            // matahari
            float sunVis = Mathf.Clamp01((sd.y + 0.08f) / 0.1f);
            Color warm = Color.Lerp(new Color(1f, 0.55f, 0.25f), new Color(1f, 0.96f, 0.82f), Mathf.Clamp01(sd.y * 2.5f));
            Place(sunT, sd, R * 0.92f, R * 0.075f, new Color(warm.r, warm.g, warm.b, sunVis), sunMat);
            Place(sunGlowT, sd, R * 0.93f, R * 0.3f, new Color(warm.r, warm.g, warm.b, 0.36f * sunVis), sunGlowMat);

            // bulan: berlawanan arah dengan matahari, sedikit digeser
            Vector3 md = Quaternion.Euler(0f, 24f, 0f) * (-sd);
            float moonVis = Mathf.Clamp01((md.y + 0.05f) / 0.12f) * Mathf.Clamp01(nightF * 1.6f);
            Place(moonT, md, R * 0.91f, R * 0.065f, new Color(1f, 1f, 1f, moonVis), moonMat);
            Place(moonGlowT, md, R * 0.92f, R * 0.32f, new Color(0.6f, 0.7f, 1f, 0.35f * moonVis), moonGlowMat);

            // bintang berputar pelan
            starsT.rotation = Quaternion.Euler(0f, Time.time * 0.15f, 0f);
            Color sc = Color.white; sc.a = Mathf.Clamp01(nightF * 1.3f - 0.1f);
            starMat.color = sc;
            starsT.gameObject.SetActive(sc.a > 0.01f);

            // awan bergeser tertiup angin. Warna: sisi menghadap matahari hangat (jingga-merah muda saat senja/fajar),
            // sisi lain biru-lavender, malam disinari bulan; jadi awan terlihat tersinari, bukan bercak datar.
            windAz += Time.deltaTime * 0.45f;
            float dusk = Mathf.Clamp01(1f - Mathf.Abs(sd.y) / 0.30f);
            Color dayLit = Color.Lerp(Color.white, sun.color, 0.30f), dayShade = new Color(0.70f, 0.79f, 0.97f);
            Color nightLit = new Color(0.42f, 0.48f, 0.70f), nightShade = new Color(0.13f, 0.16f, 0.30f);
            Color duskLit = new Color(1f, 0.68f, 0.48f), duskShade = new Color(0.50f, 0.45f, 0.72f);
            Color lit = Color.Lerp(Color.Lerp(nightLit, dayLit, Mathf.Clamp01(dayF * 1.25f)), duskLit, dusk * 0.9f);
            Color shade = Color.Lerp(Color.Lerp(nightShade, dayShade, Mathf.Clamp01(dayF * 1.25f)), duskShade, dusk * 0.8f);
            foreach (var c in clouds)
            {
                float az = c.az + windAz * c.speed;
                Vector3 dir = Quaternion.Euler(-c.el, az, 0f) * Vector3.forward;
                var t = c.t;
                t.position = transform.position + dir * R * 0.85f;
                t.rotation = Quaternion.LookRotation(t.position - cam.transform.position, Vector3.up);
                t.localScale = new Vector3(c.size, c.size * c.aspect, 1f);
                float edge = Mathf.Clamp01((c.el - 6f) / 8f);
                float near = 1f - Mathf.Clamp01(Vector3.Angle(dir, sd) / 105f);
                var cc = Color.Lerp(shade, lit, Mathf.SmoothStep(0f, 1f, near * 1.2f));
                cc.a = edge * Mathf.Lerp(0.5f, 0.95f, dayF);
                c.m.color = cc;
            }

            // burung (hanya siang)
            bool birdsOn = dayF > 0.25f;
            birdRoot.gameObject.SetActive(birdsOn);
            if (birdsOn)
            {
                Vector3 cp = cam.transform.position;
                float t = Time.time;
                foreach (var f in flocks)
                {
                    f.ang += f.spd / f.rad * Mathf.Rad2Deg * Time.deltaTime;
                    foreach (var b in f.birds)
                    {
                        float a = (f.ang + b.aOff / f.rad * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                        float rr = f.rad + b.rOff;
                        Vector3 p = new Vector3(cp.x + Mathf.Sin(a) * rr, cp.y + f.alt + b.hOff + Mathf.Sin(t * 0.6f + b.phase) * 1.5f, cp.z + Mathf.Cos(a) * rr);
                        Vector3 tang = new Vector3(Mathf.Cos(a), 0f, -Mathf.Sin(a)) * Mathf.Sign(f.spd);
                        b.root.position = p;
                        b.root.rotation = Quaternion.LookRotation(tang, Vector3.up) * Quaternion.Euler(0f, 0f, -Mathf.Sign(f.spd) * 12f);
                        float fl = Mathf.Sin(t * b.flap + b.phase) * 38f;
                        b.wr.localRotation = Quaternion.Euler(0f, 0f, fl);
                        b.wl.localRotation = Quaternion.Euler(0f, 0f, -fl);
                    }
                }
            }
        }

        void Place(Transform t, Vector3 dir, float dist, float size, Color c, Material m)
        {
            bool vis = c.a > 0.01f;
            t.gameObject.SetActive(vis);
            if (!vis) return;
            t.position = transform.position + dir * dist;
            t.rotation = Quaternion.LookRotation(t.position - cam.transform.position, Vector3.up);
            t.localScale = new Vector3(size, size, 1f);
            m.color = c;
        }
    }
}
