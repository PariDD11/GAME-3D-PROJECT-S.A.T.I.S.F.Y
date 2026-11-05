using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// Suasana Gunung Vulkanik, dibuat sepenuhnya saat runtime (tanpa perlu build ulang scene): langit gelap berasap, awan badai hitam,
    /// petir (garis + kilat + guntur), gunung api raksasa dengan kolom asap, pegunungan hitam yang menutup cakrawala,
    /// abu yang jatuh dan bara yang beterbangan. Mesh jauh memakai Sprites/Default tanpa kabut; urutan gambar diatur lewat antrian render.
    /// Koordinat dunia: arah boss (design +Z) = dunia -Z.
    /// </summary>
    public class VolcanoFx : MonoBehaviour
    {
        public static bool Active;
        public static readonly Vector3 VolcanoPos = new Vector3(0f, 0f, -1300f);
        const float DomeR = 3900f;

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
            if (SceneManager.GetActiveScene().name != "HuntArea_Volcano") return;
            if (FindFirstObjectByType<VolcanoFx>() != null) return;
            new GameObject("VolcanoFx").AddComponent<VolcanoFx>();
        }

        // ---------------------------------------------------------------- mesh bantu
        class MB
        {
            public List<Vector3> v = new List<Vector3>(); public List<Color> c = new List<Color>(); public List<int> t = new List<int>();
            public int Add(Vector3 p, Color col) { v.Add(p); c.Add(col); return v.Count - 1; }
            public void Quad(int a, int b, int c2, int d) { t.Add(a); t.Add(b); t.Add(c2); t.Add(a); t.Add(c2); t.Add(d); }
            public void Tri(int a, int b, int c2) { t.Add(a); t.Add(b); t.Add(c2); }
            public Mesh Build(string n)
            {
                var m = new Mesh { name = n, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                MeshSafe.Clean(v); m.SetVertices(v); m.SetColors(c); m.SetTriangles(t, 0);
                m.bounds = new Bounds(Vector3.zero, Vector3.one * 30000f);
                return m;
            }
        }

        static float Pn(float a, float b) { return Mathf.PerlinNoise(a + 500f, b + 500f); }

        static void Mountain(MB mb, Vector3 c, float r, float h, int segs, int rings, int seed, float craterR, float jag, Color lo, Color hi, float p, float streak, float haze, Color hazeCol)
        {
            var idx = new int[rings + 1, segs];
            for (int i = 0; i <= rings; i++)
            {
                float t = i / (float)rings;
                float rad = craterR + (r - craterR) * Mathf.Pow(1f - t, p);
                for (int j = 0; j < segs; j++)
                {
                    float a = j / (float)segs * Mathf.PI * 2f;
                    float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                    float rj = 1f + jag * (Pn(ca * 2.2f + seed, sa * 2.2f + t * 3f) - 0.5f) * 2f + 0.05f * Mathf.Sin(a * 9f + seed) * (1f - t);
                    float hj = h * t * (1f + 0.07f * (Pn(ca * 3f + seed * 1.3f, sa * 3f + 9f) - 0.5f));
                    Vector3 pos = c + new Vector3(ca * rad * rj, hj, sa * rad * rj);
                    float vary = 0.8f + 0.4f * Pn(ca * 6f + seed, sa * 6f + t * 8f);
                    Color col = Color.Lerp(lo, hi, Mathf.Clamp01(t * 1.1f)) * vary;
                    if (streak > 0f)
                    {
                        float s = Mathf.Clamp01((Pn(ca * 5.5f + seed, sa * 5.5f + 0.3f) - 0.6f) * 6f) * Mathf.SmoothStep(0.5f, 0.95f, t);
                        col = Color.Lerp(col, new Color(1f, 0.42f, 0.07f), s * streak);
                        float rim = Mathf.SmoothStep(0.93f, 1f, t);
                        col = Color.Lerp(col, new Color(1f, 0.5f, 0.1f), rim * 0.55f);
                    }
                    col = Color.Lerp(col, hazeCol, haze * (1f - t * 0.5f));
                    col.a = 1f;
                    idx[i, j] = mb.Add(pos, col);
                }
            }
            for (int i = 0; i < rings; i++)
                for (int j = 0; j < segs; j++)
                {
                    int j2 = (j + 1) % segs;
                    mb.Quad(idx[i, j], idx[i, j2], idx[i + 1, j2], idx[i + 1, j]);
                }
            if (craterR < 1f)
            {
                int tip = mb.Add(c + Vector3.up * h * 1.01f, Color.Lerp(hi, hazeCol, haze * 0.5f));
                for (int j = 0; j < segs; j++) mb.Tri(idx[rings, j], idx[rings, (j + 1) % segs], tip);
            }
            else
            {
                // kawah: dinding dalam dan lantai lava menyala
                var inner = new int[segs];
                for (int j = 0; j < segs; j++)
                {
                    float a = j / (float)segs * Mathf.PI * 2f;
                    inner[j] = mb.Add(c + new Vector3(Mathf.Cos(a) * craterR * 0.82f, h * 0.935f, Mathf.Sin(a) * craterR * 0.82f), new Color(0.85f, 0.28f, 0.05f));
                }
                int mid = mb.Add(c + Vector3.up * h * 0.925f, new Color(1f, 0.72f, 0.2f));
                for (int j = 0; j < segs; j++)
                {
                    int j2 = (j + 1) % segs;
                    mb.Quad(idx[rings, j], idx[rings, j2], inner[j2], inner[j]);
                    mb.Tri(inner[j], inner[j2], mid);
                }
            }
        }

        // ---------------------------------------------------------------- isi
        Material wallMat, farMat, cloudMat, domeMat, boltMat, ashMat;
        Transform domeT, cloudRoot, ashT, emberT;
        Camera cam; Transform player; PlayerCombat pc;
        float nextBolt = 5f, boltT = -1f, boltDelayedSfx = -1f; Vector3 boltPos;
        LineRenderer boltMain, boltGlow, boltBranch;
        float camTimer;
        readonly List<Material> flashMats = new List<Material>();

        void OnEnable() { Active = true; }
        void OnDisable() { Active = false; }

        public static string ShaderUsed = "-";
        public static int FarRenderers;
        static readonly List<Renderer> farRends = new List<Renderer>();

        Material NewFarMat(int queue)
        {
            // Sprites/Default dipakai karena terbukti tampil di proyek ini (sama dengan partikel); urutan gambar diatur lewat antrian render
            Shader sh = FxUtil.SpriteShader();
            if (sh == null) sh = Resources.Load<Shader>("FarUnlit");
            ShaderUsed = sh != null ? sh.name : "NULL";
            var m = new Material(sh);
            m.renderQueue = queue;
            flashMats.Add(m);
            return m;
        }

        GameObject MeshObj(string name, Mesh mesh, Material m, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            farRends.Add(r);
            return go;
        }

        public static string Diag()
        {
            int vis = 0;
            foreach (var r in farRends) if (r != null && r.isVisible) vis++;
            return "far " + vis + "/" + farRends.Count + " sh=" + ShaderUsed;
        }

        void Start()
        {
            farMat = NewFarMat(1010);
            wallMat = NewFarMat(1015);
            cloudMat = NewFarMat(1020);
            domeMat = NewFarMat(1000);
            boltMat = new Material(FxUtil.SpriteShader());
            boltMat.renderQueue = 3100;
            BuildDome();
            BuildFarScenery();
            BuildClouds();
            BuildVolcanoSmoke();
            BuildAsh();
            BuildBolts();
        }

        // ---------------------------------------------------------------- langit
        void BuildDome()
        {
            var mb = new MB();
            int rings = 18, segs = 48;
            var idx = new int[rings + 1, segs];
            Vector3 vdir = new Vector3(VolcanoPos.x, 0f, VolcanoPos.z).normalized;
            float vaz = Mathf.Atan2(vdir.z, vdir.x);
            for (int i = 0; i <= rings; i++)
            {
                float e = Mathf.Lerp(-12f, 90f, i / (float)rings);
                for (int j = 0; j < segs; j++)
                {
                    float a = j / (float)segs * Mathf.PI * 2f;
                    float er = Mathf.Max(e, 0f);
                    Color col;
                    if (er < 14f) col = Color.Lerp(new Color(0.30f, 0.105f, 0.065f), new Color(0.17f, 0.075f, 0.06f), er / 14f);
                    else if (er < 45f) col = Color.Lerp(new Color(0.17f, 0.075f, 0.06f), new Color(0.07f, 0.045f, 0.05f), (er - 14f) / 31f);
                    else col = Color.Lerp(new Color(0.07f, 0.045f, 0.05f), new Color(0.03f, 0.025f, 0.035f), (er - 45f) / 45f);
                    float da = Mathf.Atan2(Mathf.Sin(a - vaz), Mathf.Cos(a - vaz)) * Mathf.Rad2Deg;
                    float glow = Mathf.Exp(-(da * da) / (2f * 40f * 40f)) * Mathf.Exp(-er / 22f);
                    col += new Color(0.6f, 0.2f, 0.04f) * glow * 0.7f;
                    col.a = 1f;
                    float er2 = e * Mathf.Deg2Rad;
                    Vector3 p = new Vector3(Mathf.Cos(a) * Mathf.Cos(er2), Mathf.Sin(er2), Mathf.Sin(a) * Mathf.Cos(er2)) * DomeR;
                    idx[i, j] = mb.Add(p, col);
                }
            }
            for (int i = 0; i < rings; i++)
                for (int j = 0; j < segs; j++) { int j2 = (j + 1) % segs; mb.Quad(idx[i, j], idx[i, j2], idx[i + 1, j2], idx[i + 1, j]); }
            domeT = MeshObj("Langit", mb.Build("VolcanoSky"), domeMat, transform).transform;
        }

        // ---------------------------------------------------------------- gunung jauh
        void BuildFarScenery()
        {
            var mb = new MB();
            Color hazeCol = new Color(0.24f, 0.1f, 0.07f);
            bool volDone = false;
            // pegunungan di sekeliling
            var rnd = new System.Random(4321);
            var peaks = new List<Vector4>();                       // x, z, jarak, indeks
            for (int i = 0; i < 70; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f;
                float d = 800f + (float)rnd.NextDouble() * 1100f;
                peaks.Add(new Vector4(Mathf.Cos(a) * d, Mathf.Sin(a) * d, d, i));
            }
            peaks.Sort((p, q) => q.z.CompareTo(p.z));              // yang jauh digambar dulu
            foreach (var pk in peaks)
            {
                int i = (int)pk.w; float d = pk.z;
                if (!volDone && d < 1300f)
                {
                    volDone = true;                                            // gunung api raksasa digambar di urutan jaraknya
                    Mountain(mb, VolcanoPos, 820f, 780f, 84, 20, 11, 95f, 0.12f, new Color(0.09f, 0.07f, 0.07f), new Color(0.26f, 0.2f, 0.19f), 1.6f, 1f, 0.3f, hazeCol);
                }
                Vector3 c = new Vector3(pk.x, -10f, pk.y);
                if (Vector2.Distance(new Vector2(c.x, c.z), new Vector2(VolcanoPos.x, VolcanoPos.z)) < 1050f) continue;
                if (c.z > 250f && Mathf.Abs(c.x) < 750f) continue;                   // sisi gerbang kota: biarkan terbuka
                float r = 170f + (float)rnd.NextDouble() * 230f;
                float h = 170f + (float)rnd.NextDouble() * 260f + d * 0.05f;
                float haze = Mathf.Clamp01((d - 700f) / 1700f) * 0.7f;
                Mountain(mb, c, r, h, 9 + (i % 3), 6, i * 7, 0f, 0.35f, new Color(0.07f, 0.055f, 0.055f), new Color(0.2f, 0.15f, 0.14f), 1.25f, 0f, haze, hazeCol);
            }
            if (!volDone) Mountain(mb, VolcanoPos, 820f, 780f, 84, 20, 11, 95f, 0.12f, new Color(0.09f, 0.07f, 0.07f), new Color(0.26f, 0.2f, 0.19f), 1.6f, 1f, 0.3f, hazeCol);
            MeshObj("GunungJauh", mb.Build("VolcanoFar"), farMat, transform);

            // dinding pegunungan yang menutup tepi dunia (persegi, tepat di luar tepi terrain)
            var wb = new MB();
            int N = 260;
            int[] prev = null;
            for (int k = 0; k <= N; k++)
            {
                float th = (k % N) / (float)N * Mathf.PI * 2f;
                float dx = Mathf.Cos(th), dz = Mathf.Sin(th);
                float s = 1f / Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz));
                float h = 60f + 170f * Pn(k * 0.11f, 3.3f) + 60f * Pn(k * 0.43f, 8.1f);
                Vector3[] ps = { new Vector3(dx * 506f * s, 40f, dz * 506f * s), new Vector3(dx * 565f * s, 40f + h * 0.7f, dz * 565f * s), new Vector3(dx * 640f * s, 40f + h, dz * 640f * s), new Vector3(dx * 780f * s, -30f, dz * 780f * s) };
                Color[] cs = { new Color(0.05f, 0.04f, 0.04f), new Color(0.08f, 0.06f, 0.06f), new Color(0.16f, 0.11f, 0.1f), new Color(0.05f, 0.04f, 0.04f) };
                var cur = new int[4];
                for (int q = 0; q < 4; q++) cur[q] = wb.Add(ps[q], cs[q] * (0.85f + 0.3f * Pn(k * 0.7f, q * 2f)));
                bool gap = ps[2].z > 0f && Mathf.Abs(ps[2].x) < 260f;                // celah koridor gerbang kota
                if (prev != null && !gap) for (int q = 0; q < 3; q++) wb.Quad(prev[q], cur[q], cur[q + 1], prev[q + 1]);
                prev = cur;
            }
            MeshObj("DindingGunung", wb.Build("VolcanoWall"), wallMat, transform);
        }

        // ---------------------------------------------------------------- awan badai
        void BuildClouds()
        {
            var mb = new MB();
            var rnd = new System.Random(909);
            Vector3 vdirH = new Vector3(VolcanoPos.x, 0f, VolcanoPos.z).normalized;
            int made = 0;
            for (int n = 0; n < 400 && made < 64; n++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f;
                float d = Mathf.Sqrt((float)rnd.NextDouble()) * 2700f + 150f;
                Vector3 c = new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d);
                if (Vector2.Distance(new Vector2(c.x, c.z), new Vector2(VolcanoPos.x, VolcanoPos.z)) < 650f) continue;
                if (d > 1000f && Vector3.Dot(new Vector3(c.x, 0f, c.z).normalized, vdirH) > 0.8f) continue;     // jangan menutupi gunung api dari belakang
                c.y = 360f + (float)rnd.NextDouble() * 200f - d * 0.05f;
                float r = 230f + (float)rnd.NextDouble() * 300f;
                float flat = 0.14f + (float)rnd.NextDouble() * 0.1f;
                float shade = 0.05f + (float)rnd.NextDouble() * 0.06f;
                int rings = 10, segs = 16;
                var idx = new int[rings + 1, segs];
                for (int i = 0; i <= rings; i++)
                {
                    float la = Mathf.Lerp(-Mathf.PI / 2f, Mathf.PI / 2f, i / (float)rings);
                    for (int j = 0; j < segs; j++)
                    {
                        float lo = j / (float)segs * Mathf.PI * 2f;
                        float bump = 1f + 0.28f * (Pn(Mathf.Cos(lo) * 1.7f + n, Mathf.Sin(lo) * 1.7f + la * 2f) - 0.5f) * 2f;
                        Vector3 nrm = new Vector3(Mathf.Cos(lo) * Mathf.Cos(la), Mathf.Sin(la), Mathf.Sin(lo) * Mathf.Cos(la));
                        Vector3 p = c + new Vector3(nrm.x * r * bump, nrm.y * r * flat * bump, nrm.z * r * bump);
                        float top = Mathf.Max(nrm.y, 0f), bot = Mathf.Max(-nrm.y, 0f);
                        Color col = new Color(shade, shade * 0.95f, shade * 1.05f) * (0.75f + 0.6f * top);
                        float toVol = Mathf.Max(0f, Vector3.Dot(new Vector3(nrm.x, 0f, nrm.z).normalized, vdirH));
                        col += new Color(0.42f, 0.14f, 0.03f) * (bot * 0.55f + 0.18f * toVol * (1f - top));
                        col.a = 1f;
                        idx[i, j] = mb.Add(p, col);
                    }
                }
                for (int i = 0; i < rings; i++)
                    for (int j = 0; j < segs; j++) { int j2 = (j + 1) % segs; mb.Quad(idx[i, j], idx[i, j2], idx[i + 1, j2], idx[i + 1, j]); }
                made++;
            }
            cloudRoot = new GameObject("AwanBadai").transform;
            cloudRoot.SetParent(transform, false);
            MeshObj("Awan", mb.Build("VolcanoClouds"), cloudMat, cloudRoot);
        }

        // ---------------------------------------------------------------- partikel
        static Texture2D dot;
        Material ParticleMat()
        {
            if (dot == null) dot = FxUtil.SoftDot(1.2f);
            return FxUtil.MakeMaterial(dot);
        }

        static void FadeAlpha(ParticleSystem ps, float peak)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(peak, 0.2f), new GradientAlphaKey(peak, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
        }

        void BuildVolcanoSmoke()
        {
            var ps = FxUtil.NewSystem("AsapKawah", transform);
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat();
            ps.transform.position = VolcanoPos + Vector3.up * 760f;
            var main = ps.main;
            main.loop = true; main.playOnAwake = false; main.maxParticles = 140;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(30f, 46f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(22f, 34f);
            main.startSize = new ParticleSystem.MinMaxCurve(150f, 320f);
            main.startColor = new Color(0.1f, 0.085f, 0.085f, 1f);
            var em = ps.emission; em.rateOverTime = 3.4f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 60f; sh.rotation = new Vector3(-90f, 0f, 0f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(6f, 12f); vel.y = new ParticleSystem.MinMaxCurve(0f, 0f); vel.z = new ParticleSystem.MinMaxCurve(-3f, 3f);
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.6f));
            FadeAlpha(ps, 0.7f);
            ps.Play();
            // pendaran cahaya lava di atas kawah
            var gl = FxUtil.NewSystem("CahayaKawah", transform);
            gl.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat();
            gl.transform.position = VolcanoPos + Vector3.up * 770f;
            var gm = gl.main; gm.loop = true; gm.playOnAwake = false; gm.maxParticles = 12;
            gm.simulationSpace = ParticleSystemSimulationSpace.World;
            gm.startLifetime = new ParticleSystem.MinMaxCurve(4f, 6f); gm.startSpeed = 0f;
            gm.startSize = new ParticleSystem.MinMaxCurve(380f, 560f); gm.startColor = new Color(1f, 0.42f, 0.08f, 1f);
            var gem = gl.emission; gem.rateOverTime = 1.6f;
            var gsh = gl.shape; gsh.shapeType = ParticleSystemShapeType.Sphere; gsh.radius = 40f;
            FadeAlpha(gl, 0.4f);
            gl.Play();
        }

        void BuildAsh()
        {
            var a = FxUtil.NewSystem("AbuJatuh", transform);
            a.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat();
            ashT = a.transform;
            var m = a.main; m.loop = true; m.playOnAwake = false; m.maxParticles = 900;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.startLifetime = new ParticleSystem.MinMaxCurve(8f, 12f); m.startSpeed = 0f;
            m.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            m.startColor = new Color(0.62f, 0.56f, 0.54f, 0.75f);
            var e = a.emission; e.rateOverTime = 110f;
            var s = a.shape; s.shapeType = ParticleSystemShapeType.Box; s.scale = new Vector3(80f, 1f, 80f);
            var v = a.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World;
            v.x = new ParticleSystem.MinMaxCurve(1.2f, 2.2f); v.y = new ParticleSystem.MinMaxCurve(-2.6f, -1.3f); v.z = new ParticleSystem.MinMaxCurve(0.2f, 0.9f);
            a.Play();

            var b = FxUtil.NewSystem("Bara", transform);
            b.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMat();
            emberT = b.transform;
            var bm = b.main; bm.loop = true; bm.playOnAwake = false; bm.maxParticles = 300;
            bm.simulationSpace = ParticleSystemSimulationSpace.World;
            bm.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f); bm.startSpeed = 0f;
            bm.startSize = new ParticleSystem.MinMaxCurve(0.09f, 0.2f);
            bm.startColor = new Color(1f, 0.52f, 0.12f, 1f);
            var be = b.emission; be.rateOverTime = 26f;
            var bs = b.shape; bs.shapeType = ParticleSystemShapeType.Box; bs.scale = new Vector3(70f, 1f, 70f);
            var bv = b.velocityOverLifetime; bv.enabled = true; bv.space = ParticleSystemSimulationSpace.World;
            bv.x = new ParticleSystem.MinMaxCurve(0.6f, 1.8f); bv.y = new ParticleSystem.MinMaxCurve(1.2f, 3.4f); bv.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
            FadeAlpha(b, 1f);
            b.Play();
        }

        // ---------------------------------------------------------------- petir
        LineRenderer MakeLine(string name, float width, Color c)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = boltMat;
            lr.useWorldSpace = true; lr.widthMultiplier = width; lr.numCapVertices = 2;
            lr.startColor = c; lr.endColor = c;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.enabled = false;
            return lr;
        }

        void BuildBolts()
        {
            boltGlow = MakeLine("PetirCahaya", 9f, new Color(0.45f, 0.5f, 0.95f));
            boltMain = MakeLine("Petir", 3.2f, new Color(1f, 1f, 1f));
            boltBranch = MakeLine("PetirCabang", 1.6f, new Color(0.9f, 0.92f, 1f));
        }

        void Strike()
        {
            Vector3 pp = player != null ? player.position : (cam != null ? cam.transform.position : Vector3.zero);
            float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            // condong ke arah depan kamera agar sering terlihat
            if (cam != null && Random.value < 0.7f)
            {
                Vector3 f = cam.transform.forward; f.y = 0f; f.Normalize();
                ang = Mathf.Atan2(f.z, f.x) + Random.Range(-0.7f, 0.7f);
            }
            float d = Random.Range(260f, 950f);
            Vector3 ground = pp + new Vector3(Mathf.Cos(ang) * d, 0f, Mathf.Sin(ang) * d);
            RaycastHit hit;
            if (Physics.Raycast(ground + Vector3.up * 700f, Vector3.down, out hit, 1200f)) ground = hit.point; else ground.y = pp.y;
            Vector3 top = ground + new Vector3(Random.Range(-60f, 60f), 0f, Random.Range(-60f, 60f)); top.y = Random.Range(380f, 470f);
            int n = 16;
            var pts = new Vector3[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float k = i / (float)n;
                Vector3 p = Vector3.Lerp(top, ground, k);
                float jit = 26f * Mathf.Sin(k * Mathf.PI) + 4f;
                if (i > 0 && i < n) p += new Vector3(Random.Range(-jit, jit), 0f, Random.Range(-jit, jit));
                pts[i] = p;
            }
            boltMain.positionCount = n + 1; boltMain.SetPositions(pts);
            boltGlow.positionCount = n + 1; boltGlow.SetPositions(pts);
            float w = Mathf.Lerp(2.4f, 7f, d / 950f);
            boltMain.widthMultiplier = w; boltGlow.widthMultiplier = w * 3.2f;
            int bi = Random.Range(4, 9);
            var br = new Vector3[6];
            Vector3 dir = new Vector3(Random.Range(-1f, 1f), -0.5f, Random.Range(-1f, 1f)).normalized;
            br[0] = pts[bi];
            for (int i = 1; i < 6; i++) br[i] = br[i - 1] + dir * 34f + new Vector3(Random.Range(-14f, 14f), -Random.Range(2f, 16f), Random.Range(-14f, 14f));
            boltBranch.positionCount = 6; boltBranch.SetPositions(br); boltBranch.widthMultiplier = w * 0.5f;
            boltMain.enabled = boltGlow.enabled = boltBranch.enabled = true;
            boltT = 0f;
            boltPos = ground;
            boltDelayedSfx = Mathf.Min(d / 340f, 3.2f);
            var sv = FindFirstObjectByType<SceneVibe>();
            if (sv != null) sv.Strike();
        }

        static float Env(float t)
        {
            if (t < 0.08f) return 1f;
            if (t < 0.15f) return 0.3f;
            if (t < 0.26f) return 0.95f;
            return Mathf.Clamp01(1f - (t - 0.26f) / 0.5f) * 0.6f;
        }

        // ---------------------------------------------------------------- loop
        void Update()
        {
            camTimer -= Time.deltaTime;
            if (cam == null || !cam.isActiveAndEnabled)
            {
                if (camTimer <= 0f) { camTimer = 0.5f; cam = Camera.main; if (cam != null && cam.farClipPlane < 4300f) cam.farClipPlane = 4300f; }
            }
            if (player == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) { player = p.transform; pc = p.GetComponent<PlayerCombat>(); }
            }
            float dt = Time.deltaTime;
            if (Time.time >= nextBolt && boltT < 0f)
            {
                nextBolt = Time.time + Random.Range(5f, 15f);
                Strike();
                if (Random.value < 0.35f) nextBolt = Time.time + Random.Range(0.7f, 1.6f);       // sambaran berurutan
            }
            float f = 0f;
            if (boltT >= 0f)
            {
                boltT += dt;
                f = Env(boltT);
                bool show = boltT < 0.34f && !(boltT > 0.08f && boltT < 0.15f);
                boltMain.enabled = boltGlow.enabled = boltBranch.enabled = show;
                if (boltT > 0.8f) { boltT = -1f; boltMain.enabled = boltGlow.enabled = boltBranch.enabled = false; }
            }
            if (boltDelayedSfx >= 0f)
            {
                boltDelayedSfx -= dt;
                if (boltDelayedSfx < 0f) { boltDelayedSfx = -1f; Audio.Sfx("thunder", player != null ? player.position : boltPos); }
            }
            // kecerahan sesuai jam, plus kilat
            float dayK = 1f;
            var gs = GameState.I;
            if (gs != null) dayK = Mathf.Clamp01(Mathf.Sin((gs.timeOfDay - 6f) / 12f * Mathf.PI) * 0.7f + 0.3f);
            float b = Mathf.Lerp(0.5f, 1f, dayK);
            float k = b * (1f + f * 5f);
            Color tint = new Color(k * (1f - 0.1f * f), k * (1f - 0.05f * f), k, 1f);
            for (int i = 0; i < flashMats.Count; i++) flashMats[i].color = tint;
        }

        void LateUpdate()
        {
            if (cam == null) return;
            Vector3 cp = cam.transform.position;
            if (domeT != null) domeT.position = cp;
            if (cloudRoot != null)
            {
                cloudRoot.position = new Vector3(cp.x, 0f, cp.z);
                cloudRoot.Rotate(0f, 0.35f * Time.deltaTime, 0f, Space.World);
            }
            if (ashT != null) ashT.position = cp + Vector3.up * 16f;
            if (emberT != null) emberT.position = new Vector3(cp.x, cp.y - 6f, cp.z);
        }
    }

    /// <summary>Geyser: gelembung peringatan, lalu semburan uap panas yang melukai bila berdiri di atasnya.</summary>
    public class VolcanoGeyser : MonoBehaviour
    {
        public float period = 14f, eruptTime = 3.2f, warnTime = 1.8f, radius = 2.8f, damage = 14f, height = 26f;
        ParticleSystem steam, bubbles; Light glow; Transform player; PlayerCombat pc;
        float t; bool hurt;

        void Start()
        {
            t = Random.Range(0f, period);
            steam = FxUtil.NewSystem("Uap", transform);
            var tex = FxUtil.SoftDot(1.1f);
            steam.GetComponent<ParticleSystemRenderer>().sharedMaterial = FxUtil.MakeMaterial(tex);
            var m = steam.main; m.loop = true; m.playOnAwake = false; m.maxParticles = 260;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.6f); m.startSpeed = new ParticleSystem.MinMaxCurve(height * 0.45f, height * 0.7f);
            m.startSize = new ParticleSystem.MinMaxCurve(2.4f, 4.5f); m.startColor = new Color(0.9f, 0.86f, 0.82f, 0.8f);
            var e = steam.emission; e.rateOverTime = 0f;
            var sh = steam.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 6f; sh.radius = 0.9f; sh.rotation = new Vector3(-90f, 0f, 0f);
            var sz = steam.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 2.2f));
            var col = steam.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.8f, 0.55f), 0f), new GradientColorKey(new Color(0.85f, 0.85f, 0.88f), 0.5f), new GradientColorKey(new Color(0.6f, 0.6f, 0.64f), 1f) },
                      new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.6f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            steam.Play();

            bubbles = FxUtil.NewSystem("Gelembung", transform);
            bubbles.GetComponent<ParticleSystemRenderer>().sharedMaterial = FxUtil.MakeMaterial(tex);
            var bm = bubbles.main; bm.loop = true; bm.playOnAwake = false; bm.maxParticles = 60;
            bm.simulationSpace = ParticleSystemSimulationSpace.World;
            bm.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f); bm.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            bm.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.6f); bm.startColor = new Color(1f, 0.55f, 0.15f, 0.9f);
            var be = bubbles.emission; be.rateOverTime = 0f;
            var bs = bubbles.shape; bs.shapeType = ParticleSystemShapeType.Circle; bs.radius = 1.2f; bs.rotation = new Vector3(-90f, 0f, 0f);
            bubbles.Play();

            var lg = new GameObject("Cahaya").AddComponent<Light>();
            lg.transform.SetParent(transform, false); lg.transform.localPosition = Vector3.up * 2f;
            lg.type = LightType.Point; lg.color = new Color(1f, 0.55f, 0.2f); lg.range = 16f; lg.intensity = 0f;
            glow = lg;
        }

        void Update()
        {
            if (player == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) { player = p.transform; pc = p.GetComponent<PlayerCombat>(); }
            }
            t += Time.deltaTime;
            float ph = t % period;
            float warnStart = period - eruptTime - warnTime;
            bool warn = ph >= warnStart && ph < warnStart + warnTime;
            bool erupt = ph >= warnStart + warnTime;
            var se = steam.emission; se.rateOverTime = erupt ? 150f : 6f;
            var be = bubbles.emission; be.rateOverTime = warn ? 40f : (erupt ? 15f : 3f);
            glow.intensity = erupt ? 2.6f : warn ? 1.2f + Mathf.PingPong(Time.time * 6f, 0.8f) : 0.3f;
            if (!erupt) hurt = false;
            else if (!hurt && player != null && pc != null && !pc.IsDead)
            {
                Vector3 d = player.position - transform.position; d.y = 0f;
                if (d.magnitude < radius)
                {
                    hurt = true;
                    pc.TakeDamage(damage, transform.position);
                    if (TownUI.I != null) TownUI.I.Toast("Semburan geyser panas!");
                }
            }
        }
    }
}
