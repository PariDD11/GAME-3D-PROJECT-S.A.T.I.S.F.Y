using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    public enum Season { Semi, Panas, Gugur, Dingin }
    public enum Wx { Cerah, Berawan, Hujan, Badai, Salju, Blizzard }
    public enum Climate { None, Temperate, Arid, Hell, Indoor }

    /// <summary>Kalender in-game: 28 hari per musim, 4 musim = 1 tahun (112 hari). Hari ke-14 tiap musim = festival.</summary>
    public static class GameCalendar
    {
        public const int DaysPerSeason = 28, DaysPerYear = 112, FestivalDay = 14;
        public static readonly string[] SeasonNames = { "Musim Semi", "Musim Panas", "Musim Gugur", "Musim Dingin" };
        public static readonly string[] FestivalNames = { "Festival Bunga", "Festival Bintang Panas", "Festival Panen", "Festival Cahaya Salju" };

        public static int Day { get { var g = GameState.I; return g != null ? Mathf.Max(1, g.day) : 1; } }
        public static int SeasonIndex(int day) { return ((day - 1) / DaysPerSeason) % 4; }
        public static Season Current { get { return (Season)SeasonIndex(Day); } }
        public static int DayOfSeason(int day) { return (day - 1) % DaysPerSeason + 1; }
        public static int Year(int day) { return (day - 1) / DaysPerYear + 1; }
        public static bool IsFestival(int day) { return DayOfSeason(day) == FestivalDay; }
        public static string Name(Season s) { return SeasonNames[(int)s]; }

        public static string Date
        {
            get { int d = Day; return SeasonNames[SeasonIndex(d)] + ", Hari " + DayOfSeason(d) + ", Tahun " + Year(d); }
        }

        public static string DateShort
        {
            get { int d = Day; string n = SeasonNames[SeasonIndex(d)].Substring(6); return n + " " + DayOfSeason(d) + "/" + DaysPerSeason + " \u00b7 Th" + Year(d); }
        }

        /// <summary>0..1 menuju musim berikutnya (peralihan pelan selama 3 hari terakhir tiap musim).</summary>
        public static float BlendToNext
        {
            get
            {
                var g = GameState.I; if (g == null) return 0f;
                float d = DayOfSeason(g.day) + g.timeOfDay / 24f;
                return Mathf.Clamp01((d - (DaysPerSeason - 3)) / 3f);
            }
        }

        /// <summary>Bahan alam yang hanya tumbuh di musim tertentu.</summary>
        public static bool Grows(string itemId, Season s)
        {
            switch (itemId)
            {
                case "herb": return s != Season.Dingin;
                case "mushroom": return s == Season.Semi || s == Season.Gugur;
                case "wild_berry": return s == Season.Panas || s == Season.Gugur;
                default: return true;
            }
        }
        public static int GatherBonus(Season s) { return s == Season.Gugur ? 1 : 0; }   // musim panen

        /// <summary>Pengali penurunan lapar/haus: dingin = cepat lapar, panas = cepat haus (di luar ruangan).</summary>
        public static float HungerMul { get { return SeasonRuntime.Outdoors && Current == Season.Dingin ? 1.25f : 1f; } }
        public static float ThirstMul { get { return SeasonRuntime.Outdoors && Current == Season.Panas ? 1.35f : 1f; } }
    }

    /// <summary>
    /// Musim, cuaca, dan suasana — semua dijalankan saat runtime (tanpa build ulang scene):
    /// warna matahari/ambient/kabut, warna dedaunan dan tanah (salju di musim dingin), hujan/badai/salju + suaranya,
    /// bahan musiman, dan hadiah festival.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class SeasonRuntime : MonoBehaviour
    {
        public static SeasonRuntime I;
        public static bool Outdoors { get { return I != null && I.climate != Climate.None && I.climate != Climate.Indoor; } }
        public static Wx Weather { get { return I != null ? I.shown : Wx.Cerah; } }
        public static string WeatherName
        {
            get
            {
                switch (Weather) { case Wx.Berawan: return "Berawan"; case Wx.Hujan: return "Hujan"; case Wx.Badai: return "Badai Petir"; case Wx.Blizzard: return "Badai Salju"; case Wx.Salju: return "Salju"; default: return "Cerah"; }
            }
        }

        // ---------- parameter per musim
        class SP { public Color sun, amb, fog, fol, snowCol, gMin, gMax; public float sunI, fogMul, snow; }
        static Color C(float r, float g, float b) { return new Color(r, g, b, 1f); }
        static readonly SP[] SPs =
        {
            new SP { sun = C(1f, 1f, 0.96f), sunI = 1f, amb = C(0.97f, 1.03f, 1f), fog = C(0.96f, 1.02f, 1f), fogMul = 1f, fol = C(0.85f, 1.18f, 0.8f), snow = 0f, snowCol = C(1, 1, 1), gMin = C(0, 0, 0), gMax = C(0.9f, 1.12f, 0.85f) },
            new SP { sun = C(1.1f, 1.03f, 0.9f), sunI = 1.1f, amb = C(1.05f, 1.02f, 0.95f), fog = C(1.05f, 1f, 0.9f), fogMul = 0.75f, fol = C(1f, 1.02f, 0.7f), snow = 0f, snowCol = C(1, 1, 1), gMin = C(0, 0, 0), gMax = C(1.12f, 1.05f, 0.8f) },
            new SP { sun = C(1.08f, 0.93f, 0.78f), sunI = 0.95f, amb = C(1.02f, 0.93f, 0.85f), fog = C(1.05f, 0.92f, 0.82f), fogMul = 1.2f, fol = C(2f, 0.85f, 0.3f), snow = 0f, snowCol = C(1, 1, 1), gMin = C(0, 0, 0), gMax = C(1.2f, 0.9f, 0.65f) },
            new SP { sun = C(0.86f, 0.95f, 1.12f), sunI = 0.8f, amb = C(0.9f, 0.98f, 1.12f), fog = C(0.88f, 0.94f, 1.06f), fogMul = 1.5f, fol = C(0.9f, 1f, 1.1f), snow = 0.95f, snowCol = C(0.94f, 0.96f, 0.99f), gMin = C(0.82f, 0.86f, 0.92f), gMax = C(1.25f, 1.28f, 1.36f) },
        };

        Climate climate;
        float snowScale = 1f;      // seberapa tebal salju menutupi scene ini (0 = tidak ada)
        float sceneTime;
        Light sun;
        float nextFindSun;
        bool foliageDone, terrainDone;

        // ---------- cuaca
        int rolledDay = -1; Wx dayWx; float wxStart, wxDur;
        Wx shown = Wx.Cerah, lastShown = Wx.Cerah;
        float windI;
        public static int forceWx = -1;          // debug
        float precipI, cloudI;
        float nextBolt = 12f, boltFlash, thunderAt = -1f;
        int lastSeason = -1;

        ParticleSystem rain, snowPs;
        AudioSource rainSrc, windSrc;
        AudioClip rainClip, windClip;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("SeasonRuntime");
            DontDestroyOnLoad(go);
            I = go.AddComponent<SeasonRuntime>();
            I.OnScene(SceneManager.GetActiveScene());
        }

        void OnEnable() { SceneManager.sceneLoaded += OnLoaded; }
        void OnDisable() { SceneManager.sceneLoaded -= OnLoaded; RestoreTerrain(); }
        void OnApplicationQuit() { RestoreTerrain(); }
        void OnLoaded(Scene s, LoadSceneMode m) { OnScene(s); }

        static Climate For(string s)
        {
            if (string.IsNullOrEmpty(s)) return Climate.None;
            if (s.Contains("Tower") || s == GuildConfig.ArenaScene) return Climate.None;
            if (InteriorInfo.IsInterior(s) || s == GuildConfig.InteriorScene) return Climate.Indoor;
            if (s.Contains("Desert")) return Climate.Arid;
            if (s.Contains("Volcano") || s.Contains("Demon") || s.Contains("Bone")) return Climate.Hell;
            return Climate.Temperate;
        }

        void OnScene(Scene s)
        {
            RestoreTerrain();
            climate = For(s.name);
            snowScale = climate == Climate.Temperate ? 1f : climate == Climate.Arid ? 0.6f : (climate == Climate.Hell && s.name.Contains("Bone")) ? 0.85f : 0f;   // Vulkanik & Lembah Iblis: dunia api, tanpa salju
            sceneTime = 0f; sun = null; foliageDone = false; terrainDone = false;
            fm.Clear(); clones.Clear(); layerSets.Clear();
            fogCaptured = false;
            lastSeason = -1;
        }

        // =====================================================================
        void Update()
        {
            var gs = GameState.I;
            if (gs == null) return;
            sceneTime += Time.unscaledDeltaTime;
            DebugKeys(gs);

            int si = GameCalendar.SeasonIndex(gs.day);
            if (si != lastSeason)
            {
                bool first = lastSeason < 0;
                lastSeason = si;
                RefreshGather();
                if (!first && climate != Climate.None) GameState.Say(GameCalendar.SeasonNames[si] + " telah tiba.");
            }
            if (gs.day != rolledDay) NewDay(gs);

            UpdateWeather(gs);
            if (climate != Climate.None) UpdateFestival(gs);
        }

        void DebugKeys(GameState gs)
        {
            if (!(Application.isEditor || Debug.isDebugBuild)) return;
            var kb = UnityEngine.InputSystem.Keyboard.current; if (kb == null || !kb.shiftKey.isPressed) return;
            if (kb.f10Key.wasPressedThisFrame) { gs.day += 1; GameState.Say("[debug] " + GameCalendar.Date); }
            if (kb.f11Key.wasPressedThisFrame) { gs.day += GameCalendar.DaysPerSeason; GameState.Say("[debug] " + GameCalendar.Date); }
            if (kb.f12Key.wasPressedThisFrame) { forceWx = (forceWx + 2) % 7 - 1; GameState.Say("[debug] cuaca paksa: " + (forceWx < 0 ? "otomatis" : ((Wx)forceWx).ToString())); }
        }

        // ---------- cuaca per hari (deterministik dari nomor hari)
        void NewDay(GameState gs)
        {
            rolledDay = gs.day;
            var rnd = new System.Random(gs.day * 7919 + 13);
            double p = rnd.NextDouble();
            Season s = (Season)GameCalendar.SeasonIndex(gs.day);
            double rain, storm, cloudy;
            switch (s)
            {
                case Season.Semi: rain = 0.26; storm = 0.36; cloudy = 0.5; break;
                case Season.Panas: rain = 0.08; storm = 0.20; cloudy = 0.35; break;
                case Season.Gugur: rain = 0.28; storm = 0.40; cloudy = 0.6; break;
                default: rain = 0.34; storm = 0.50; cloudy = 0.66; break;
            }
            if (p < rain) dayWx = s == Season.Dingin ? Wx.Salju : Wx.Hujan;
            else if (p < storm) dayWx = s == Season.Dingin ? Wx.Blizzard : Wx.Badai;
            else if (p < cloudy) dayWx = Wx.Berawan;
            else dayWx = Wx.Cerah;
            wxStart = 4f + (float)rnd.NextDouble() * 10f;
            wxDur = 4f + (float)rnd.NextDouble() * 8f;
            heavy = p >= rain && p < storm;
            arid3 = rnd.Next(3) == 0;
        }
        bool heavy, arid3;

        Wx Effective(GameState gs)
        {
            if (climate == Climate.None) return Wx.Cerah;
            Climate wc = climate == Climate.Indoor ? Climate.Temperate : climate;   // cuaca luar tetap dihitung untuk suara di dalam ruangan
            Wx w = forceWx >= 0 ? (Wx)forceWx : dayWx;
            if (forceWx < 0 && (w == Wx.Hujan || w == Wx.Badai || w == Wx.Salju || w == Wx.Blizzard))
            {
                float t = gs.timeOfDay;
                bool inside = (t >= wxStart && t <= wxStart + wxDur) || (t + 24f >= wxStart && t + 24f <= wxStart + wxDur);
                if (!inside) w = Wx.Berawan;
            }
            if (wc == Climate.Hell) return Wx.Cerah;
            if (wc == Climate.Arid)
            {
                if (w == Wx.Salju || w == Wx.Blizzard) return Wx.Berawan;
                if ((w == Wx.Hujan || w == Wx.Badai) && !arid3) return Wx.Berawan;
            }
            return w;
        }

        void UpdateWeather(GameState gs)
        {
            Wx target = Effective(gs);
            shown = target;
            if (shown != lastShown)
            {
                bool wet = shown == Wx.Hujan || shown == Wx.Badai || shown == Wx.Salju || shown == Wx.Blizzard;
                bool wasWet = lastShown == Wx.Hujan || lastShown == Wx.Badai || lastShown == Wx.Salju || lastShown == Wx.Blizzard;
                if (wet && !wasWet && climate != Climate.None) GameState.Say(shown == Wx.Salju ? "Salju mulai turun." : shown == Wx.Blizzard ? "Badai salju datang! Jarak pandang menipis." : shown == Wx.Badai ? "Badai petir datang! Hujan deras dan angin kencang." : "Hujan mulai turun.");
                else if (!wet && wasWet && climate != Climate.None) GameState.Say("Cuaca mulai reda.");
                lastShown = shown;
            }
            float tp = (shown == Wx.Hujan) ? 0.6f : (shown == Wx.Badai || shown == Wx.Blizzard) ? 1f : (shown == Wx.Salju) ? 0.6f : 0f;
            float tw = (shown == Wx.Badai || shown == Wx.Blizzard) ? 1f : shown == Wx.Hujan ? 0.2f : shown == Wx.Salju ? 0.15f : 0.03f;
            windI = Mathf.MoveTowards(windI, tw, Time.deltaTime * 0.15f);
            float tc = shown == Wx.Cerah ? 0f : shown == Wx.Berawan ? 0.4f : 0.85f;
            precipI = Mathf.MoveTowards(precipI, tp, Time.deltaTime * 0.12f);
            cloudI = Mathf.MoveTowards(cloudI, tc, Time.deltaTime * 0.08f);

            // petir
            if (shown == Wx.Badai && precipI > 0.7f)
            {
                nextBolt -= Time.deltaTime;
                if (nextBolt <= 0f)
                {
                    nextBolt = Random.Range(5f, 13f); boltFlash = 0.22f; thunderAt = Time.time + Random.Range(0.4f, 2.2f);
                }
            }
            if (boltFlash > 0f) boltFlash -= Time.deltaTime;
            if (thunderAt > 0f && Time.time >= thunderAt)
            {
                thunderAt = -1f;
                var cam = Camera.main;
                Audio.Sfx("thunder", cam != null ? cam.transform.position : Vector3.zero);
            }
        }

        // ---------- festival
        void UpdateFestival(GameState gs)
        {
            if (!GameCalendar.IsFestival(gs.day)) return;
            string sc = SceneManager.GetActiveScene().name;
            if (sc != GuildConfig.TownScene && sc != "StartVillage") return;
            string flag = "fest_" + gs.day;
            if (gs.flags.Contains(flag)) return;
            if (sceneTime < 2f) return;
            gs.flags.Add(flag);
            int s = GameCalendar.SeasonIndex(gs.day);
            int yr = GameCalendar.Year(gs.day);
            int g = 120 + 30 * yr;
            gs.gold += g;
            string gift;
            switch (s)
            {
                case 0: gs.AddItem("herb", 5); gift = "5 Herb"; break;
                case 1: gs.AddItem("potion", 2); gift = "2 Potion"; break;
                case 2: gs.AddItem("bread", 5); gift = "5 Roti"; break;
                default: gs.AddItem("potion", 3); gift = "3 Potion"; break;
            }
            Audio.Sfx("rankup");
            GameState.Say(GameCalendar.FestivalNames[s] + " hari ini! Warga membagikan " + g + " gold dan " + gift + ".");
        }

        // ---------- bahan musiman
        public static void RefreshGather()
        {
            var se = GameCalendar.Current;
            foreach (var n in GatherNode.Nodes)
            {
                if (n == null || n.visual == null) continue;
                bool ok = GameCalendar.Grows(n.itemId, se);
                if (!ok) n.visual.SetActive(false);
                else if (n.enabled) n.visual.SetActive(true);
            }
        }

        // =====================================================================
        // Visual: pencahayaan, dedaunan, tanah, partikel
        // =====================================================================
        Color lastAmbW, lastAmbB, lastFogW, lastFogB, lastSunW, lastSunB;
        float lastSunIW, lastSunIB, lastDensW, lastDensB;
        bool fogCaptured;

        static bool Same(Color a, Color b) { return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 0.0005f; }

        void LateUpdate()
        {
            var gs = GameState.I;
            if (gs == null) return;
            if (climate == Climate.None) { SetPrecip(0f, 0f); return; }

            float k = climate == Climate.Temperate ? 1f : climate == Climate.Arid ? 0.5f : climate == Climate.Indoor ? 0.35f : 0.2f;
            int si = GameCalendar.SeasonIndex(gs.day);
            float bt = GameCalendar.BlendToNext; bt = bt * bt * (3f - 2f * bt);
            SP a = SPs[si], b = SPs[(si + 1) % 4];
            Color sunM = Color.Lerp(a.sun, b.sun, bt), ambM = Color.Lerp(a.amb, b.amb, bt), fogM = Color.Lerp(a.fog, b.fog, bt);
            float sunIM = Mathf.Lerp(a.sunI, b.sunI, bt), fogMul = Mathf.Lerp(a.fogMul, b.fogMul, bt);
            sunM = Mul(sunM, SeasonEvents.SunMul); ambM = Mul(ambM, SeasonEvents.AmbMul); fogM = Mul(fogM, SeasonEvents.FogMul);

            if (sun == null && Time.time >= nextFindSun)
            {
                nextFindSun = Time.time + 1f;
                var dn = FindFirstObjectByType<TownDayNight>();
                if (dn != null) sun = dn.sun;
                if (sun == null) foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) { sun = l; break; }
            }

            float cl = climate == Climate.Indoor ? 0f : Mathf.Max(cloudI, precipI);
            bool snowy = shown == Wx.Salju || shown == Wx.Blizzard;
            bool storm = shown == Wx.Badai || shown == Wx.Blizzard;
            Color gray = snowy ? C(0.82f, 0.86f, 0.92f) : C(0.55f, 0.58f, 0.62f);

            if (sun != null)
            {
                Color sc = sun.color;
                Color baseC = Same(sc, lastSunW) ? lastSunB : sc;
                float baseI = Mathf.Approximately(sun.intensity, lastSunIW) ? lastSunIB : sun.intensity;
                Color o = Color.Lerp(baseC, Mul(baseC, sunM), k);
                o = Color.Lerp(o, gray * Mathf.Max(0.3f, baseC.maxColorComponent), cl * 0.45f);
                float oi = baseI * Mathf.Lerp(1f, sunIM, k) * Mathf.Lerp(1f, storm ? 0.28f : snowy ? 0.7f : 0.5f, cl) + boltFlash * 3.5f;
                sun.color = o; sun.intensity = oi;
                lastSunB = baseC; lastSunW = o; lastSunIB = baseI; lastSunIW = oi;
            }
            {
                Color ac = RenderSettings.ambientLight;
                Color baseC = Same(ac, lastAmbW) ? lastAmbB : ac;
                Color o = Color.Lerp(baseC, Mul(baseC, ambM), k);
                o = Color.Lerp(o, gray * baseC.maxColorComponent, cl * 0.35f);
                o += Color.white * (boltFlash * 1.2f);
                RenderSettings.ambientLight = o;
                lastAmbB = baseC; lastAmbW = o;
            }
            if (RenderSettings.fog)
            {
                Color fc = RenderSettings.fogColor;
                Color baseC = Same(fc, lastFogW) ? lastFogB : fc;
                Color o = Color.Lerp(baseC, Mul(baseC, fogM), k);
                o = Color.Lerp(o, gray * Mathf.Max(0.25f, baseC.maxColorComponent), cl * 0.5f);
                RenderSettings.fogColor = o;
                lastFogB = baseC; lastFogW = o;

                float d = RenderSettings.fogDensity;
                float baseD = Mathf.Approximately(d, lastDensW) ? lastDensB : d;
                float od = baseD * Mathf.Lerp(1f, fogMul, k) * Mathf.Lerp(1f, storm ? (snowy ? 3.6f : 2.5f) : snowy ? 2.2f : 1.7f, cl);
                RenderSettings.fogDensity = od;
                lastDensB = baseD; lastDensW = od;
            }

            // dedaunan & tanah (hanya daerah beriklim sedang)
            if (snowScale > 0f && climate != Climate.Indoor && sceneTime > 0.7f)
            {
                if (!foliageDone) { ScanFoliage(); foliageDone = true; lastTintKey = -1; }
                if (!terrainDone) { ScanTerrain(); terrainDone = true; lastTintKey = -1; }
                int key = gs.day * 24 + Mathf.FloorToInt(gs.timeOfDay);
                if (key != lastTintKey) { lastTintKey = key; ApplyTints(a, b, bt); }
                if (!Mathf.Approximately(lastBt, bt)) { lastBt = bt; }
            }

            SetPrecip(precipI, cloudI);
        }
        int lastTintKey = -1; float lastBt;

        static Color Mul(Color c, Color m) { return new Color(c.r * m.r, c.g * m.g, c.b * m.b, c.a); }

        // ---------- dedaunan
        class FM { public Material clone; public Color baseCol; public bool pine, soft, maple; public int mapleKind; public string prop; public int kind; public Texture tex; public string texProp; }
        readonly Dictionary<Material, FM> fm = new Dictionary<Material, FM>();
        readonly HashSet<Material> clones = new HashSet<Material>();
        readonly List<FM> tinted = new List<FM>();

        static bool IsFoliage(string n)
        {
            n = n.ToLowerInvariant();
            if (n.Contains("lotus") || n.Contains("leather")) return false;
            return n.Contains("leaf") || n.Contains("grass") || n.Contains("hedge") || n.Contains("foliage") || n.Contains("canopy")
                || n.Contains("needle") || n.Contains("bush") || n.Contains("fern") || n.Contains("blossom") || n.Contains("pine");
        }

        // 1 = permukaan datar yang tertutup salju (atap, jerami, tanah, rumput, ladang), 2 = permukaan keras (batu, jalan) hanya berdebu salju
        static int SnowKind(string n)
        {
            n = n.ToLowerInvariant();
            if (n.Contains("water") || n.Contains("glass") || n.Contains("glow") || n.Contains("lava") || n.Contains("flame") || n.Contains("gold") || n.Contains("cloth") || n.Contains("skin")) return 0;
            if (n.Contains("roof") || n.Contains("thatch") || n.Contains("dirt") || n.Contains("field") || n.Contains("moss") || n.Contains("litter") || n.Contains("sand") || n.Contains("ground")) return 1;
            if (n.Contains("cobble") || n.Contains("pave") || n.Contains("rock") || n.Contains("stone") || n.Contains("ash")) return n.Contains("darkstone") ? 0 : 2;
            return 0;
        }

        void ScanFoliage()
        {
            tinted.Clear();
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                var mats = r.sharedMaterials; bool ch = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null || clones.Contains(m)) continue;
                    FM f;
                    if (!fm.TryGetValue(m, out f))
                    {
                        f = null;
                        string prop = m.HasProperty("_BaseColor") ? "_BaseColor" : (m.HasProperty("_Color") ? "_Color" : null);
                        int kind = prop == null ? 0 : IsFoliage(m.name) ? 0 : SnowKind(m.name);
                        bool isF = prop != null && IsFoliage(m.name);
                        if (prop != null && (isF || kind > 0))
                        {
                            string tp = m.HasProperty("_BaseMap") ? "_BaseMap" : (m.HasProperty("_MainTex") ? "_MainTex" : null);
                            f = new FM { clone = new Material(m), baseCol = m.GetColor(prop), prop = prop, pine = m.name.ToLowerInvariant().Contains("pine") || m.name.ToLowerInvariant().Contains("needle"), soft = m.name.ToLowerInvariant().Contains("blossom"), maple = isF && (m.name.ToLowerInvariant().Contains("leafred") || m.name.ToLowerInvariant().Contains("leaforange")), mapleKind = m.name.ToLowerInvariant().Contains("leaforange") ? 1 : 0, kind = isF ? 0 : kind, texProp = tp, tex = tp != null ? m.GetTexture(tp) : null };
                            f.clone.name = m.name + "_Season";
                            clones.Add(f.clone); tinted.Add(f);
                        }
                        fm[m] = f;
                    }
                    if (f != null) { mats[i] = f.clone; ch = true; }
                }
                if (ch) r.sharedMaterials = mats;
            }
        }

        // Pohon maple (bahan LeafRed/LeafOrange): tidak lagi coklat gelap seperti batu. Semi/Panas = hijau segar, Gugur = merah & oranye menyala, Dingin = ranting kelabu.
        static readonly Color[][] MapleSeason =
        {
            new[] { new Color(0.40f, 0.68f, 0.24f, 1f), new Color(0.56f, 0.82f, 0.28f, 1f) },
            new[] { new Color(0.30f, 0.58f, 0.20f, 1f), new Color(0.44f, 0.72f, 0.22f, 1f) },
            new[] { new Color(0.96f, 0.24f, 0.12f, 1f), new Color(1.00f, 0.60f, 0.12f, 1f) },
            new[] { new Color(0.52f, 0.48f, 0.46f, 1f), new Color(0.60f, 0.54f, 0.48f, 1f) },
        };

        static Color Fol(FM f, SP s, float tintK, float snowK, int si = -1)
        {
            if (f.kind == 0 && f.maple && tintK > 0.5f && si >= 0 && si < 4)
            {
                Color mc = MapleSeason[si][Mathf.Clamp(f.mapleKind, 0, 1)];
                float mix2 = s.snow * snowK;
                mc = Color.Lerp(mc, s.snowCol, mix2);
                mc.a = f.baseCol.a;
                return mc;
            }
            if (f.kind == 0)
            {
                Color fo = Color.Lerp(Color.white, s.fol, tintK);
                Color mul = f.pine ? Color.Lerp(Color.white, fo, 0.35f) : f.soft ? Color.Lerp(Color.white, fo, 0.3f) : fo;   // sakura/bunga: tint musim lemah agar pink tidak menjadi coklat
                Color c = Mul(f.baseCol, mul);
                float mix = (f.pine ? s.snow * 0.55f : s.snow) * snowK;
                c = Color.Lerp(c, s.snowCol, mix);
                c.a = f.baseCol.a;
                return c;
            }
            float w = (f.kind == 1 ? s.snow : s.snow * 0.55f) * snowK;
            Color r = Color.Lerp(f.baseCol, s.snowCol, w);
            r.a = f.baseCol.a;
            return r;
        }

        // ---------- tanah (terrain layer)
        // Layer asli TIDAK diganti/di-instantiate (dulu: td.terrainLayers = salinan runtime, yang di Editor bisa
        // tertinggal di aset TerrainData dan membuat tanah jadi kotak-kotak abu-abu). Cukup ubah remap warna
        // lalu kembalikan nilai semula saat scene berganti atau game ditutup.
        class LayerSet { public TerrainData td; public TerrainLayer[] orig, use; public Vector4[] rMin, rMax; public Terrain t; public float density; }
        readonly List<LayerSet> layerSets = new List<LayerSet>();

        void ScanTerrain()
        {
            layerSets.Clear();
            foreach (var t in Terrain.activeTerrains)
            {
                if (t == null || t.terrainData == null) continue;
                var td = t.terrainData;
                var orig = td.terrainLayers;
                if (orig == null || orig.Length == 0) continue;
                var rMin = new Vector4[orig.Length]; var rMax = new Vector4[orig.Length];
                for (int i = 0; i < orig.Length; i++)
                    if (orig[i] != null) { rMin[i] = orig[i].diffuseRemapMin; rMax[i] = orig[i].diffuseRemapMax; }
                layerSets.Add(new LayerSet { td = td, orig = orig, use = orig, rMin = rMin, rMax = rMax, t = t, density = t.detailObjectDensity });
            }
        }

        void RestoreTerrain()
        {
            foreach (var ls in layerSets)
            {
                if (ls.orig != null)
                    for (int i = 0; i < ls.orig.Length; i++)
                        if (ls.orig[i] != null) { ls.orig[i].diffuseRemapMin = ls.rMin[i]; ls.orig[i].diffuseRemapMax = ls.rMax[i]; }
                if (ls.t != null) ls.t.detailObjectDensity = ls.density;
            }
            layerSets.Clear();
        }

        void ApplyTints(SP a, SP b, float bt)
        {
            float tintK = climate == Climate.Temperate ? 1f : 0f;
            float snowK = snowScale;
            float sf = Mathf.Lerp(a.snow, b.snow, bt) * snowK;
            int ia = System.Array.IndexOf(SPs, a), ib = System.Array.IndexOf(SPs, b);
            foreach (var f in tinted)
            {
                f.clone.SetColor(f.prop, Color.Lerp(Fol(f, a, tintK, snowK, ia), Fol(f, b, tintK, snowK, ib), bt));
                if (f.texProp != null && f.tex != null)
                {
                    bool white = f.kind == 1 ? sf > 0.45f : f.kind == 0 ? (!f.pine && sf > 0.55f) : false;
                    f.clone.SetTexture(f.texProp, white ? null : f.tex);
                }
            }
            Color gmin = Color.Lerp(a.gMin, b.gMin, bt), gmax = Color.Lerp(a.gMax, b.gMax, bt);
            float w = climate == Climate.Temperate ? 1f : Mathf.Clamp01(sf / 0.95f);
            gmin = Color.Lerp(Color.black, gmin, w); gmax = Color.Lerp(Color.white, gmax, w);
            foreach (var ls in layerSets)
            {
                foreach (var l in ls.use)
                {
                    if (l == null) continue;
                    l.diffuseRemapMin = new Vector4(gmin.r, gmin.g, gmin.b, 0f);
                    l.diffuseRemapMax = new Vector4(gmax.r, gmax.g, gmax.b, 1f);
                }
                if (ls.t != null) ls.t.detailObjectDensity = Mathf.Lerp(ls.density, ls.density * 0.08f, Mathf.Clamp01((sf - 0.3f) / 0.5f));   // rumput hilang tertutup salju
            }
        }

        // ---------- hujan / salju / suara
        void SetPrecip(float p, float cloud)
        {
            EnsureFx();
            bool rainOn = (shown == Wx.Hujan || shown == Wx.Badai) && climate != Climate.None;
            bool blz = shown == Wx.Blizzard;
            bool indoor = climate == Climate.Indoor;
            bool snowOn = (shown == Wx.Salju || blz) && climate != Climate.None;
            var cam = Camera.main;
            if (cam != null)
            {
                rain.transform.position = cam.transform.position + Vector3.up * 24f;
                snowPs.transform.position = cam.transform.position + Vector3.up * 14f;
            }
            float rainRate = shown == Wx.Badai ? 4200f : 1500f, snowRate = blz ? 1300f : 650f;
            Emit(rain, rainOn && !indoor ? p * rainRate : 0f);
            Emit(snowPs, snowOn && !indoor ? p * snowRate : 0f);
            // angin: makin kencang saat badai (hujan miring & memanjang, salju tertiup)
            var rv = rain.velocityOverLifetime; rv.x = new ParticleSystem.MinMaxCurve(-(2.5f + windI * 11f), -(3.5f + windI * 13f));
            var sv2 = snowPs.velocityOverLifetime; sv2.x = new ParticleSystem.MinMaxCurve(-(0.6f + windI * 10f), 0.6f - windI * 8f);
            sv2.y = new ParticleSystem.MinMaxCurve(-(1.7f + windI * 2.5f), -(1.2f + windI * 1.8f));
            var rr = rain.GetComponent<ParticleSystemRenderer>(); rr.lengthScale = 2.2f + windI * 2.6f;

            float sv = AudioManager.I != null ? AudioManager.I.sfxVol : 0.8f;
            float mute = indoor ? 0.3f : 1f;
            float rt = rainOn ? p * (shown == Wx.Badai ? 0.6f : 0.32f) * sv * mute : 0f;
            float wt = Mathf.Max(snowOn ? p * 0.22f * sv * mute : (climate == Climate.None ? 0f : cloud * 0.04f * sv * mute), climate == Climate.None ? 0f : windI * 0.45f * sv * mute);
            Fade(rainSrc, rt); Fade(windSrc, wt);
        }

        static void Emit(ParticleSystem ps, float rate)
        {
            var em = ps.emission; em.rateOverTime = rate;
            if (rate > 0.5f) { if (!ps.isPlaying) ps.Play(); }
            else if (ps.isPlaying && ps.particleCount == 0) ps.Stop();
        }

        static void Fade(AudioSource s, float target)
        {
            s.volume = Mathf.MoveTowards(s.volume, target, Time.deltaTime * 0.25f);
            if (s.volume > 0.003f) { if (!s.isPlaying) s.Play(); }
            else if (s.isPlaying) s.Pause();
        }

        void EnsureFx()
        {
            if (rain != null) return;
            rain = MakePs("Hujan", FxUtil.SoftDot(0.3f), true);
            snowPs = MakePs("Salju", FxUtil.SoftDot(0.7f), false);

            var rnd = new System.Random(5);
            var b = AudioManager.Buf(2.4f);
            AudioManager.Noise(b, 0, 2.4f, 0.55f, 0.45f, 0.45f, rnd);
            rainClip = MakeClip("rain", b);
            var w = AudioManager.Buf(3f);
            AudioManager.Noise(w, 0, 3f, 0.9f, 0.04f, 0.08f, rnd);
            windClip = MakeClip("wind", w);
            rainSrc = MakeSrc("RainLoop", rainClip); windSrc = MakeSrc("WindLoop", windClip);
        }

        static AudioClip MakeClip(string name, float[] data)
        {
            int n = Mathf.Min(1500, data.Length / 4);
            for (int i = 0; i < n; i++) { float k = i / (float)n; data[i] *= k; data[data.Length - 1 - i] *= k; }
            var c = AudioClip.Create(name, data.Length, 1, AudioManager.SampleRate, false);
            c.SetData(data, 0);
            return c;
        }

        AudioSource MakeSrc(string name, AudioClip clip)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.clip = clip; s.loop = true; s.spatialBlend = 0f; s.volume = 0f; s.playOnAwake = false;
            return s;
        }

        ParticleSystem MakePs(string name, Texture2D tex, bool isRain)
        {
            var ps = FxUtil.NewSystem(name, transform);
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = FxUtil.MakeMaterial(tex);
            var main = ps.main;
            main.loop = true; main.playOnAwake = false; main.prewarm = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = isRain ? 7000 : 12000;
            main.gravityModifier = 0f;
            main.startSpeed = 0f;
            if (isRain)
            {
                main.startLifetime = 1.1f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.09f);
                main.startColor = new Color(0.78f, 0.85f, 0.95f, 0.55f);
                r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = 2.2f; r.velocityScale = 0.02f;
                r.minParticleSize = 0f; r.maxParticleSize = 0.006f;   // tetes dekat kamera tidak boleh membesar jadi pilar putih
            }
            else
            {
                main.startLifetime = 9f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
                main.startColor = new Color(1f, 1f, 1f, 0.9f);
                r.renderMode = ParticleSystemRenderMode.Billboard;
                r.minParticleSize = 0f; r.maxParticleSize = 0.012f;
            }
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(44f, 1f, 44f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(isRain ? -2.5f : -0.6f, isRain ? -3.5f : 0.6f);
            vel.y = new ParticleSystem.MinMaxCurve(isRain ? -30f : -1.7f, isRain ? -26f : -1.2f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
            if (!isRain) { var nz = ps.noise; nz.enabled = true; nz.strength = 0.7f; nz.frequency = 0.4f; }
            var em = ps.emission; em.rateOverTime = 0f;
            // hujan/salju mati saat menabrak atap & benda padat -> area beratap tetap kering
            var col = ps.collision; col.enabled = true;
            col.type = ParticleSystemCollisionType.World; col.mode = ParticleSystemCollisionMode.Collision3D;
            col.quality = ParticleSystemCollisionQuality.Low; col.maxCollisionShapes = 64;
            col.collidesWith = ~0; col.dampen = 1f; col.bounce = 0f; col.lifetimeLoss = 1f;
            col.radiusScale = 0.5f; col.sendCollisionMessages = false;
            return ps;
        }
    }
}
