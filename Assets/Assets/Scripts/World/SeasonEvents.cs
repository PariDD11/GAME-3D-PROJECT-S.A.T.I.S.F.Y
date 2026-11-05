using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    public class SeasonEventDef
    {
        public string id, name, desc;
        public float h0, h1;                         // jam aktif (h0 > h1 = melewati tengah malam)
        public float buy = 1f, sell = 1f, exp = 1f; public int gather;
        public Color amb = Color.white, fog = Color.white, sun = Color.white;
        public SeasonEventDef(string id, string name, string desc, float h0, float h1) { this.id = id; this.name = name; this.desc = desc; this.h0 = h0; this.h1 = h1; }
    }

    /// <summary>
    /// Event acak per musim (peristiwa harian), dekorasi musiman di desa dan interior, kembang api, aurora, kupu-kupu,
    /// dan suara suasana (burung, jangkrik, angin). Semua dibuat saat runtime.
    /// </summary>
    [DefaultExecutionOrder(900)]
    public class SeasonEvents : MonoBehaviour
    {
        static SeasonEvents I;
        static readonly Color Blood = new Color(1.5f, 0.55f, 0.55f), Aur = new Color(0.7f, 1.25f, 1.1f);

        static readonly SeasonEventDef[][] Defs =
        {
            new[] { new SeasonEventDef("pasar_bunga", "Pasar Bunga", "Toko memberi diskon 15%.", 7f, 20f) { buy = 0.85f },
                    new SeasonEventDef("kupu", "Migrasi Kupu-kupu", "Bahan alam melimpah (+1).", 6f, 19f) { gather = 1 } },
            new[] { new SeasonEventDef("kembang_api", "Malam Kembang Api", "Harga jual +10% dan langit penuh kembang api.", 19f, 24f) { sell = 1.1f },
                    new SeasonEventDef("lomba", "Lomba Berburu", "EXP dari monster +25%.", 6f, 20f) { exp = 1.25f } },
            new[] { new SeasonEventDef("pasar_panen", "Pasar Panen", "Harga jual bahan +20%.", 7f, 20f) { sell = 1.2f },
                    new SeasonEventDef("bulan_merah", "Bulan Berdarah", "Malam merah: monster lebih ganas, EXP +40%.", 19f, 5f) { exp = 1.4f, amb = Blood, fog = Blood, sun = Blood } },
            new[] { new SeasonEventDef("aurora", "Aurora", "Cahaya hijau menari di langit malam.", 19f, 5f) { amb = Aur, fog = Aur },
                    new SeasonEventDef("pasar_es", "Pasar Es", "Toko memberi diskon 15%.", 8f, 18f) { buy = 0.85f } },
        };
        static readonly SeasonEventDef[] Festivals =
        {
            new SeasonEventDef("festival", "Festival Bunga", "Diskon 10%, EXP +20%, warga berpesta.", 6f, 24f) { buy = 0.9f, exp = 1.2f },
            new SeasonEventDef("festival", "Festival Bintang Panas", "Diskon 10%, EXP +20%, kembang api tiap malam.", 6f, 24f) { buy = 0.9f, exp = 1.2f },
            new SeasonEventDef("festival", "Festival Panen", "Harga jual +15%, EXP +20%, panen berlimpah.", 6f, 24f) { sell = 1.15f, exp = 1.2f, gather = 1 },
            new SeasonEventDef("festival", "Festival Cahaya Salju", "Diskon 10%, EXP +20%, lentera di mana-mana.", 6f, 24f) { buy = 0.9f, exp = 1.2f },
        };

        public static SeasonEventDef Today { get; private set; }
        public static bool Active { get; private set; }
        /// <summary>Harga bahan berfluktuasi per musim: panen murah, bahan langka musim dingin mahal.</summary>
        public static float IngredientMul(string id)
        {
            if (string.IsNullOrEmpty(id)) return 1f;
            bool plant = id.Contains("herb") || id.Contains("mushroom") || id.Contains("lotus") || id.Contains("berry") || id.Contains("wheat");
            bool animal = id.Contains("pelt") || id.Contains("hide") || id.Contains("fang") || id.Contains("meat");
            switch (GameCalendar.Current)
            {
                case Season.Semi: return plant ? 0.85f : 1f;
                case Season.Gugur: return plant ? 0.8f : 1f;
                case Season.Dingin: return plant ? 1.4f : animal ? 1.15f : 1f;
                default: return plant ? 1.1f : 1f;
            }
        }
        public static float BuyMul = 1f, SellMul = 1f, ExpMul = 1f; public static int GatherExtra;
        public static Color AmbMul = Color.white, FogMul = Color.white, SunMul = Color.white;

        public static string Summary
        {
            get
            {
                if (Today == null) return "";
                return Today.name + (Active ? " (berlangsung)" : " (mulai jam " + Mathf.FloorToInt(Today.h0) + ":00)");
            }
        }

        int day = -1; bool announced; bool wasActive;
        float sceneTime; string sceneName = "";
        bool outdoorTown, isInterior;
        Transform decor; int decorKey = -999;
        float nextFirework, nextFind;
        ParticleSystem butterflies; Transform aurora; Material auroraMat;
        AudioSource birdSrc, cricketSrc, windSrc2; AudioClip birdClip, cricketClip, windClip2;
        readonly List<Material> matCache = new List<Material>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("SeasonEvents");
            DontDestroyOnLoad(go);
            I = go.AddComponent<SeasonEvents>();
            I.OnScene(SceneManager.GetActiveScene());
        }

        void OnEnable() { SceneManager.sceneLoaded += OnLoaded; }
        void OnDisable() { SceneManager.sceneLoaded -= OnLoaded; }
        void OnLoaded(Scene s, LoadSceneMode m) { OnScene(s); }

        void OnScene(Scene s)
        {
            sceneName = s.name; sceneTime = 0f; decor = null; decorKey = -999;
            isInterior = InteriorInfo.IsInterior(s.name) || s.name == GuildConfig.InteriorScene;
            outdoorTown = s.name == GuildConfig.TownScene || s.name == "StartVillage";
            announced = false;
        }

        static bool InWindow(float t, SeasonEventDef e)
        {
            return e.h0 <= e.h1 ? (t >= e.h0 && t < e.h1) : (t >= e.h0 || t < e.h1);
        }

        // =====================================================================
        void Update()
        {
            var gs = GameState.I;
            if (gs == null) return;
            sceneTime += Time.unscaledDeltaTime;

            if (gs.day != day)
            {
                day = gs.day; announced = false;
                int s = GameCalendar.SeasonIndex(day);
                var rnd = new System.Random(day * 104729 + 7);
                if (GameCalendar.IsFestival(day)) Today = Festivals[s];
                else if (rnd.NextDouble() < 0.45) Today = Defs[s][rnd.Next(2)];
                else Today = null;
            }
            bool act = Today != null && InWindow(gs.timeOfDay, Today);
            Active = act;
            BuyMul = act ? Today.buy : 1f; SellMul = act ? Today.sell : 1f; ExpMul = act ? Today.exp : 1f; GatherExtra = act ? Today.gather : 0;
            AmbMul = act ? Today.amb : Color.white; FogMul = act ? Today.fog : Color.white; SunMul = act ? Today.sun : Color.white;

            bool visible = SeasonRuntime.Outdoors || isInterior;
            if (visible && sceneTime > 1.5f)
            {
                if (!announced && Today != null && !act) { announced = true; GameState.Say("Hari ini: " + Today.name + " mulai jam " + Mathf.FloorToInt(Today.h0) + ":00."); }
                if (act && !wasActive) { announced = true; GameState.Say(Today.name + "! " + Today.desc); Audio.Sfx("quest_accept"); }
            }
            wasActive = act;

            UpdateDecor(gs);
            AnimateDancers();
            UpdateFireworks(gs);
            UpdateButterflies(gs);
            UpdateAurora(gs);
            UpdateAmbience(gs);
        }

        // ---------------- dekorasi
        static Material Mk(Color c, bool glow, Material tpl)
        {
            var m = new Material(tpl);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            m.color = c;
            if (glow) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * 2.2f); }
            return m;
        }

        Material tpl;
        Material Tpl()
        {
            if (tpl == null)
            {
                var t = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tpl = t.GetComponent<Renderer>().sharedMaterial;
                Destroy(t);
            }
            return tpl;
        }

        GameObject P(PrimitiveType t, Transform par, Vector3 pos, Vector3 scale, Color c, bool glow = false)
        {
            var g = GameObject.CreatePrimitive(t);
            var col = g.GetComponent<Collider>(); if (col != null) Destroy(col);
            g.transform.SetParent(par, false); g.transform.position = pos; g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = Mk(c, glow, Tpl());
            return g;
        }

        static Vector3 Ground(Vector3 p)
        {
            RaycastHit h;
            if (Physics.Raycast(p + Vector3.up * 25f, Vector3.down, out h, 60f, ~0, QueryTriggerInteraction.Ignore)) return h.point;
            return p;
        }

        void Prop(Season s, Transform par, Vector3 g, System.Random r, float sc)
        {
            switch (s)
            {
                case Season.Semi:
                    P(PrimitiveType.Cube, par, g + Vector3.up * 0.12f * sc, new Vector3(1.1f, 0.24f, 0.5f) * sc, new Color(0.35f, 0.22f, 0.12f));
                    for (int i = 0; i < 6; i++)
                    {
                        Color c = i % 3 == 0 ? new Color(1f, 0.6f, 0.75f) : i % 3 == 1 ? new Color(1f, 0.95f, 0.8f) : new Color(1f, 0.85f, 0.3f);
                        P(PrimitiveType.Sphere, par, g + new Vector3(-0.42f + i * 0.17f, 0.34f, (i % 2 - 0.5f) * 0.15f) * sc, Vector3.one * 0.2f * sc, c);
                    }
                    break;
                case Season.Panas:
                    P(PrimitiveType.Cylinder, par, g + Vector3.up * 0.45f * sc, new Vector3(0.7f, 0.45f, 0.7f) * sc, new Color(0.45f, 0.3f, 0.16f));
                    P(PrimitiveType.Sphere, par, g + Vector3.up * 1.15f * sc, Vector3.one * 0.32f * sc, new Color(1f, 0.8f, 0.35f), true);
                    P(PrimitiveType.Cube, par, g + Vector3.up * 0.97f * sc, new Vector3(0.06f, 0.4f, 0.06f) * sc, new Color(0.15f, 0.12f, 0.1f));
                    break;
                case Season.Gugur:
                    for (int i = 0; i < 3; i++)
                        P(PrimitiveType.Sphere, par, g + new Vector3(-0.4f + i * 0.4f, 0.22f * sc * (i == 1 ? 1.2f : 1f), (i % 2) * 0.2f) , new Vector3(0.5f, 0.38f, 0.5f) * sc * (i == 1 ? 1.25f : 1f), new Color(0.95f, 0.45f, 0.08f));
                    P(PrimitiveType.Cube, par, g + new Vector3(0.9f * sc, 0.25f * sc, 0f), new Vector3(0.7f, 0.5f, 0.5f) * sc, new Color(0.85f, 0.7f, 0.3f));
                    break;
                default:
                    P(PrimitiveType.Cylinder, par, g + Vector3.up * 0.15f * sc, new Vector3(0.25f, 0.15f, 0.25f) * sc, new Color(0.3f, 0.2f, 0.12f));
                    P(PrimitiveType.Cylinder, par, g + Vector3.up * 0.55f * sc, new Vector3(1.0f, 0.3f, 1.0f) * sc, new Color(0.1f, 0.35f, 0.18f));
                    P(PrimitiveType.Cylinder, par, g + Vector3.up * 1.0f * sc, new Vector3(0.75f, 0.28f, 0.75f) * sc, new Color(0.1f, 0.38f, 0.2f));
                    P(PrimitiveType.Cylinder, par, g + Vector3.up * 1.4f * sc, new Vector3(0.45f, 0.26f, 0.45f) * sc, new Color(0.12f, 0.4f, 0.22f));
                    P(PrimitiveType.Sphere, par, g + Vector3.up * 1.8f * sc, Vector3.one * 0.2f * sc, new Color(1f, 0.9f, 0.4f), true);
                    for (int i = 0; i < 4; i++)
                    {
                        float a = i * 90f * Mathf.Deg2Rad + (float)r.NextDouble();
                        P(PrimitiveType.Sphere, par, g + new Vector3(Mathf.Cos(a) * 0.4f, 0.7f + i * 0.2f, Mathf.Sin(a) * 0.4f) * sc, Vector3.one * 0.09f * sc, i % 2 == 0 ? new Color(1f, 0.25f, 0.2f) : new Color(1f, 0.9f, 0.5f), true);
                    }
                    break;
            }
        }

        // ---------------- plaza festival: panggung, kios, tali lentera, penari
        readonly List<Transform> dancers = new List<Transform>();
        readonly List<Vector3> dancerBase = new List<Vector3>();

        void BuildFestivalPlaza(int se)
        {
            dancers.Clear(); dancerBase.Clear();
            var doors = FindObjectsByType<BuildingDoor>(FindObjectsSortMode.None);
            if (doors.Length == 0) return;
            Vector3 cen = Vector3.zero;
            foreach (var d in doors) cen += d.transform.position;
            cen /= doors.Length;
            Vector3 spot = cen; bool found = false;
            for (int i = 0; i < 24 && !found; i++)
            {
                float a = i * 0.9f; float rad = 6f + i * 1.5f;
                Vector3 c = cen + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (i == 0 ? 0f : rad);
                c = Ground(c);
                if (!Physics.CheckBox(c + Vector3.up * 2.2f, new Vector3(7f, 2f, 5f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)) { spot = c; found = true; }
            }
            if (!found) return;
            var root = new GameObject("FestivalPlaza").transform;
            root.SetParent(decor, false);
            Color[] main = { new Color(1f, 0.6f, 0.75f), new Color(1f, 0.8f, 0.3f), new Color(0.9f, 0.45f, 0.15f), new Color(0.6f, 0.85f, 1f) };
            Color mc = main[Mathf.Clamp(se, 0, 3)];
            // panggung
            P(PrimitiveType.Cube, root, spot + new Vector3(0, 0.3f, 0), new Vector3(7f, 0.6f, 4f), new Color(0.45f, 0.3f, 0.18f));
            P(PrimitiveType.Cube, root, spot + new Vector3(0, 2.2f, 1.9f), new Vector3(7f, 3.2f, 0.2f), mc);
            P(PrimitiveType.Cube, root, spot + new Vector3(-3.4f, 2.4f, -1.8f), new Vector3(0.2f, 4f, 0.2f), new Color(0.35f, 0.22f, 0.12f));
            P(PrimitiveType.Cube, root, spot + new Vector3(3.4f, 2.4f, -1.8f), new Vector3(0.2f, 4f, 0.2f), new Color(0.35f, 0.22f, 0.12f));
            P(PrimitiveType.Cube, root, spot + new Vector3(0, 4.2f, -1.8f), new Vector3(7f, 0.25f, 0.25f), mc);
            // tali lentera
            for (int i = 0; i < 9; i++)
            {
                float x = -3.4f + i * 0.85f;
                P(PrimitiveType.Sphere, root, spot + new Vector3(x, 3.9f - Mathf.Sin(i / 8f * Mathf.PI) * -0.0f, -1.8f), Vector3.one * 0.28f, i % 2 == 0 ? mc : new Color(1f, 0.95f, 0.7f), true);
            }
            // kios
            for (int k = 0; k < 4; k++)
            {
                float a = (k - 1.5f) * 0.55f + Mathf.PI * 1.5f;
                Vector3 sp = spot + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 8.5f;
                sp = Ground(sp);
                if (Physics.CheckBox(sp + Vector3.up * 1.2f, new Vector3(1.2f, 1f, 1f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)) continue;
                P(PrimitiveType.Cube, root, sp + new Vector3(0, 0.55f, 0), new Vector3(2.2f, 1.1f, 1.0f), new Color(0.5f, 0.35f, 0.2f));
                for (int st = 0; st < 5; st++)
                    P(PrimitiveType.Cube, root, sp + new Vector3(-0.96f + st * 0.48f, 2.3f, 0), new Vector3(0.46f, 0.08f, 1.5f), st % 2 == 0 ? mc : Color.white);
                P(PrimitiveType.Cube, root, sp + new Vector3(-1.05f, 1.2f, -0.6f), new Vector3(0.08f, 2.4f, 0.08f), new Color(0.35f, 0.22f, 0.12f));
                P(PrimitiveType.Cube, root, sp + new Vector3(1.05f, 1.2f, -0.6f), new Vector3(0.08f, 2.4f, 0.08f), new Color(0.35f, 0.22f, 0.12f));
                for (int g = 0; g < 3; g++) P(PrimitiveType.Sphere, root, sp + new Vector3(-0.6f + g * 0.6f, 1.25f, 0), Vector3.one * 0.25f, Color.HSVToRGB((k * 0.2f + g * 0.15f) % 1f, 0.7f, 0.95f));
            }
            // penari pesta
            for (int n = 0; n < 6; n++)
            {
                float a = n * Mathf.PI * 2f / 6f;
                Vector3 dp = spot + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 3.8f + new Vector3(0, 0, -2.5f);
                dp = Ground(dp);
                var person = new GameObject("Penari" + n).transform;
                person.SetParent(root, false);
                person.position = dp;
                Color cl = Color.HSVToRGB(n / 6f, 0.55f, 0.9f);
                P(PrimitiveType.Capsule, person, dp + Vector3.up * 0.9f, new Vector3(0.5f, 0.7f, 0.5f), cl);
                P(PrimitiveType.Sphere, person, dp + Vector3.up * 1.75f, Vector3.one * 0.38f, new Color(0.9f, 0.72f, 0.58f));
                dancers.Add(person); dancerBase.Add(dp);
            }
        }

        void AnimateDancers()
        {
            for (int i = 0; i < dancers.Count; i++)
            {
                if (dancers[i] == null) continue;
                float t = Time.time * 4f + i;
                dancers[i].position = dancerBase[i] + Vector3.up * Mathf.Abs(Mathf.Sin(t)) * 0.25f;
                dancers[i].rotation = Quaternion.Euler(0f, Time.time * 40f + i * 60f, Mathf.Sin(t * 0.5f) * 8f);
            }
        }

        void UpdateDecor(GameState gs)
        {
            if (sceneTime < 1.0f) return;
            if (!outdoorTown && !isInterior) { if (decor != null) { Destroy(decor.gameObject); decor = null; decorKey = -999; } return; }
            int se = GameCalendar.SeasonIndex(gs.day);
            bool big = Today != null && (Today.id == "festival" || Active);
            int key = se * 10 + (big ? 1 : 0) + (Today != null && Today.id == "festival" ? 5 : 0);
            if (key == decorKey && decor != null) return;
            decorKey = key;
            if (decor != null) Destroy(decor.gameObject);
            decor = new GameObject("SeasonDecor").transform;
            var r = new System.Random(77 + se);
            float sc = big ? 1.3f : 1f;

            if (outdoorTown)
            {
                foreach (var d in FindObjectsByType<BuildingDoor>(FindObjectsSortMode.None))
                {
                    Vector3 dp = d.transform.position, right = d.transform.right;
                    right.y = 0f; right.Normalize();
                    Prop((Season)se, decor, Ground(dp + right * 1.9f), r, sc);
                    if (big) Prop((Season)se, decor, Ground(dp - right * 1.9f), r, sc);
                }
                if (Today != null && Today.id == "festival") BuildFestivalPlaza(se);
            }
            else
            {
                Bounds b = new Bounds(); bool has = false;
                foreach (var rd in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                {
                    if (!has) { b = rd.bounds; has = true; } else b.Encapsulate(rd.bounds);
                }
                if (has)
                {
                    float ix = Mathf.Min(1.8f, b.extents.x * 0.3f), iz = Mathf.Min(1.8f, b.extents.z * 0.3f);
                    Vector3[] corners =
                    {
                        new Vector3(b.min.x + ix, 0, b.min.z + iz), new Vector3(b.max.x - ix, 0, b.min.z + iz),
                        new Vector3(b.min.x + ix, 0, b.max.z - iz), new Vector3(b.max.x - ix, 0, b.max.z - iz)
                    };
                    for (int i = 0; i < corners.Length; i++)
                    {
                        if (!big && i > 1) break;
                        Vector3 c = corners[i]; c.y = b.min.y;
                        RaycastHit h;
                        if (Physics.Raycast(new Vector3(c.x, b.min.y + 3f, c.z), Vector3.down, out h, 6f, ~0, QueryTriggerInteraction.Ignore)) c.y = h.point.y;
                        Prop((Season)se, decor, c, r, 1f);
                    }
                    // cahaya suasana: hangat di gugur/dingin, terang sejuk di semi/panas
                    var lg = new GameObject("MoodLight");
                    lg.transform.SetParent(decor, false);
                    lg.transform.position = new Vector3(b.center.x, b.min.y + Mathf.Min(4f, b.size.y * 0.5f), b.center.z);
                    var l = lg.AddComponent<Light>(); l.type = LightType.Point;
                    l.range = Mathf.Max(b.extents.x, b.extents.z) * 1.6f; l.shadows = LightShadows.None;
                    Color[] mood = { new Color(1f, 0.85f, 0.9f), new Color(1f, 0.95f, 0.75f), new Color(1f, 0.6f, 0.3f), new Color(1f, 0.75f, 0.45f) };
                    l.color = mood[se]; l.intensity = se >= 2 ? 1.1f : 0.7f;
                }
            }
        }

        // ---------------- kembang api
        bool FireworksNow(GameState gs)
        {
            if (!SeasonRuntime.Outdoors || Today == null || !Active) return false;
            if (GameCalendar.SeasonIndex(gs.day) != 1) return false;
            return Today.id == "kembang_api" || Today.id == "festival";
        }

        void UpdateFireworks(GameState gs)
        {
            if (!FireworksNow(gs) || gs.timeOfDay < 19f && gs.timeOfDay > 5f) return;
            var cam = Camera.main; if (cam == null) return;
            nextFirework -= Time.deltaTime;
            if (nextFirework > 0f) return;
            nextFirework = Random.Range(1.2f, 3.6f);
            Vector2 d = Random.insideUnitCircle.normalized * Random.Range(55f, 95f);
            Vector3 pos = cam.transform.position + new Vector3(d.x, Random.Range(35f, 60f), d.y);
            Color c = Color.HSVToRGB(Random.value, 0.75f, 1f);
            var ps = FxUtil.NewSystem("Kembang Api", null);
            ps.transform.position = pos;
            var r = ps.GetComponent<UnityEngine.ParticleSystemRenderer>();
            r.sharedMaterial = FxUtil.MakeMaterial(FxUtil.SoftDot(0.5f));
            var main = ps.main;
            main.loop = false; main.playOnAwake = false; main.duration = 0.3f;
            main.startLifetime = 1.8f; main.startSpeed = 16f; main.startSize = 1.4f; main.startColor = c * 2f;
            main.gravityModifier = 0.35f; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 0f; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 90) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.3f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var gr = new Gradient();
            gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(c, 0.4f), new GradientColorKey(c, 1f) },
                       new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = gr;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
            ps.Play();
            Audio.Sfx("hit_heavy", pos);
            Destroy(ps.gameObject, 4f);
        }

        // ---------------- kupu-kupu (event Migrasi Kupu-kupu & festival semi)
        void UpdateButterflies(GameState gs)
        {
            bool on = SeasonRuntime.Outdoors && Active && Today != null && (Today.id == "kupu" || (Today.id == "festival" && GameCalendar.SeasonIndex(gs.day) == 0)) && gs.timeOfDay > 6f && gs.timeOfDay < 19f;
            if (butterflies == null)
            {
                if (!on) return;
                butterflies = FxUtil.NewSystem("Kupu-kupu", null);
                var r = butterflies.GetComponent<ParticleSystemRenderer>();
                r.sharedMaterial = FxUtil.MakeMaterial(FxUtil.SoftDot(0.9f));
                var main = butterflies.main;
                main.loop = true; main.playOnAwake = false; main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.startLifetime = 14f; main.startSpeed = 0.4f; main.startSize = 0.16f; main.maxParticles = 160;
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.6f, 0.8f), new Color(0.6f, 0.8f, 1f));
                var sh = butterflies.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(36f, 4f, 36f);
                var nz = butterflies.noise; nz.enabled = true; nz.strength = 1.6f; nz.frequency = 0.5f;
                var em = butterflies.emission; em.rateOverTime = 0f;
            }
            var cam = Camera.main;
            if (cam != null) butterflies.transform.position = cam.transform.position + Vector3.up * 1.5f;
            var e = butterflies.emission; e.rateOverTime = on ? 12f : 0f;
            if (on && !butterflies.isPlaying) butterflies.Play();
        }

        // ---------------- aurora
        void UpdateAurora(GameState gs)
        {
            bool on = SeasonRuntime.Outdoors && Active && Today != null && Today.id == "aurora" && (gs.timeOfDay >= 19f || gs.timeOfDay < 5f);
            if (aurora == null)
            {
                if (!on) return;
                var tex = new Texture2D(8, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < 64; y++)
                {
                    float t = y / 63f; float a = Mathf.Sin(t * Mathf.PI); a *= a;
                    Color c = Color.Lerp(new Color(0.2f, 1f, 0.6f), new Color(0.4f, 0.5f, 1f), t);
                    for (int x = 0; x < 8; x++) tex.SetPixel(x, y, new Color(c.r, c.g, c.b, a * 0.75f));
                }
                tex.Apply();
                auroraMat = FxUtil.MakeMaterial(tex);
                aurora = new GameObject("Aurora").transform;
                for (int i = 0; i < 3; i++)
                {
                    var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    Destroy(q.GetComponent<Collider>());
                    q.transform.SetParent(aurora, false);
                    q.transform.localPosition = new Vector3((i - 1) * 260f, 190f + i * 14f, 520f + i * 60f);
                    q.transform.localScale = new Vector3(420f, 130f, 1f);
                    q.GetComponent<Renderer>().sharedMaterial = auroraMat;
                    q.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
            aurora.gameObject.SetActive(on);
            if (!on) return;
            var cam = Camera.main;
            if (cam != null) aurora.position = new Vector3(cam.transform.position.x, 0f, cam.transform.position.z);
            float p = 0.6f + 0.4f * Mathf.Sin(Time.time * 0.5f);
            auroraMat.color = new Color(1f, 1f, 1f, p);
            for (int i = 0; i < aurora.childCount; i++)
                aurora.GetChild(i).localPosition += new Vector3(Mathf.Sin(Time.time * 0.15f + i) * 0.05f, 0f, 0f);
        }

        // ---------------- suara suasana
        AudioClip Clip(string n, float[] d)
        {
            int k = Mathf.Min(1500, d.Length / 4);
            for (int i = 0; i < k; i++) { float f = i / (float)k; d[i] *= f; d[d.Length - 1 - i] *= f; }
            var c = AudioClip.Create(n, d.Length, 1, AudioManager.SampleRate, false);
            c.SetData(d, 0);
            return c;
        }

        AudioSource Src(string n, AudioClip c)
        {
            var go = new GameObject(n); go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>(); s.clip = c; s.loop = true; s.spatialBlend = 0f; s.volume = 0f; s.playOnAwake = false;
            return s;
        }

        void EnsureAudio()
        {
            if (birdSrc != null) return;
            var r = new System.Random(11);
            var b = AudioManager.Buf(5f);
            for (int i = 0; i < 9; i++)
            {
                float t0 = (float)r.NextDouble() * 4.4f, f = 2200f + (float)r.NextDouble() * 1800f;
                int n = 2 + r.Next(3);
                for (int k = 0; k < n; k++) AudioManager.Tone(b, t0 + k * 0.09f, 0.07f, f + k * 160f, f + 500f + k * 80f, 0.11f);
            }
            birdClip = Clip("birds", b);
            var cr = AudioManager.Buf(2f);
            for (int i = 0; i < 24; i++) AudioManager.Tone(cr, i * 0.0833f, 0.035f, 4300f, 4300f, 0.1f);
            cricketClip = Clip("crickets", cr);
            var w = AudioManager.Buf(4f);
            AudioManager.Noise(w, 0, 4f, 0.9f, 0.03f, 0.07f, r);
            windClip2 = Clip("wind2", w);
            birdSrc = Src("Birds", birdClip); cricketSrc = Src("Crickets", cricketClip); windSrc2 = Src("AutumnWind", windClip2);
        }

        static void Fade(AudioSource s, float target)
        {
            s.volume = Mathf.MoveTowards(s.volume, target, Time.deltaTime * 0.15f);
            if (s.volume > 0.003f) { if (!s.isPlaying) s.Play(); } else if (s.isPlaying) s.Pause();
        }

        void UpdateAmbience(GameState gs)
        {
            EnsureAudio();
            float sv = AudioManager.I != null ? AudioManager.I.sfxVol : 0.8f;
            bool out_ = SeasonRuntime.Outdoors && !(SeasonRuntime.Weather == Wx.Hujan || SeasonRuntime.Weather == Wx.Badai || SeasonRuntime.Weather == Wx.Blizzard);
            var se = GameCalendar.Current;
            float t = gs.timeOfDay; bool day = t >= 6f && t < 19f;
            float bird = out_ && day && (se == Season.Semi ? true : se == Season.Panas) ? (se == Season.Semi ? 0.18f : 0.1f) : 0f;
            float cric = out_ && !day && se == Season.Panas ? 0.09f : 0f;
            float wnd = out_ && se == Season.Gugur ? 0.07f : 0f;
            Fade(birdSrc, bird * sv * 2f); Fade(cricketSrc, cric * sv * 2f); Fade(windSrc2, wnd * sv * 2f);
        }
    }
}
