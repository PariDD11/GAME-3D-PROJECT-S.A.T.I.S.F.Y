using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n31: dunia bereaksi terhadap waktu, cuaca, dan perilaku pemain (melengkapi SeasonSystem/SeasonEvents yang sudah ada).
    /// - Malam: serangan monster +10%; monster hantu/mayat hidup +20% di malam hari dan -10% di siang hari.
    /// - Cuaca: hujan/badai melemahkan monster api (-15%); salju/badai salju menguatkan monster es (+20%).
    /// - Bulan Berdarah (event musim): semua monster +25% serangan.
    /// - Ekologi: memburu terlalu banyak di satu area dalam sehari menipiskan populasi (monster muncul ulang lebih lambat, sampai 2,2x);
    ///   area yang dibiarkan 3 hari lebih menjadi "ramai" (muncul ulang lebih cepat, 0,75x).
    /// - Gelombang Serbuan: hari tertentu (hari ke-3 tiap 6 hari) monster menyerbu area hunt dalam 3 gelombang; bertahan sampai habis untuk hadiah.
    /// </summary>
    public static class WorldReact
    {
        static readonly HashSet<string> Undead = new HashSet<string> { "skeleton", "wraith", "mummy", "bone_soldier", "bone_archer", "banshee", "ash_wraith", "necromancer", "bone_king", "grave_knight", "drowned" };
        static readonly HashSet<string> Fire = new HashSet<string> { "ember_imp", "lava_lizard", "ash_wraith", "fire_cultist", "magma_golem", "ignarok", "hellhound" };
        static readonly HashSet<string> Ice = new HashSet<string> { "frost_wolf" };

        public static bool IsNight { get { var gs = GameState.I; return gs != null && (gs.timeOfDay >= 19f || gs.timeOfDay < 5f); } }

        public static float AtkMul(string defId)
        {
            if (defId == null) return 1f;
            float m = 1f; bool night = IsNight;
            if (night) m *= 1.1f;
            if (Undead.Contains(defId)) m *= night ? 1.2f : 0.9f;
            var wx = SeasonRuntime.Weather;
            if (Fire.Contains(defId) && (wx == Wx.Hujan || wx == Wx.Badai)) m *= 0.85f;
            if (Ice.Contains(defId) && (wx == Wx.Salju || wx == Wx.Blizzard)) m *= 1.2f;
            if (SeasonEvents.Today != null && SeasonEvents.Today.id == "bulan_merah" && SeasonEvents.Active) m *= 1.25f;
            return m;
        }

        /// <summary>Ringkasan kondisi dunia untuk UI.</summary>
        public static string Conditions()
        {
            var l = new List<string>();
            l.Add(IsNight ? "Malam: monster lebih ganas (hantu +20%)." : "Siang: hantu/mayat hidup lebih lemah (-10%).");
            var wx = SeasonRuntime.Weather;
            if (wx == Wx.Hujan || wx == Wx.Badai) l.Add("Hujan: monster api melemah.");
            if (wx == Wx.Salju || wx == Wx.Blizzard) l.Add("Salju: monster es menguat.");
            if (SeasonEvents.Today != null && SeasonEvents.Active) l.Add("Event: " + SeasonEvents.Today.name + " - " + SeasonEvents.Today.desc);
            if (Invasion.IsDay()) l.Add("Hari ini ada Gelombang Serbuan di area hunt!");
            return string.Join("\n", l.ToArray());
        }

        // ---------------- ekologi
        static string Region() { return GameState.RegionKeyFor(SceneManager.GetActiveScene().name); }

        public static void OnKill()
        {
            var gs = GameState.I; if (gs == null || HuntManager.I == null) return;
            string r = Region();
            if (gs.Stat("eco_day_" + r) != gs.day) { gs.stats["eco_day_" + r] = gs.day; gs.stats["eco_n_" + r] = 0; }
            gs.AddStat("eco_n_" + r); gs.stats["eco_last_" + r] = gs.day;
        }

        public static int TodayKills(string region)
        {
            var gs = GameState.I; if (gs == null) return 0;
            return gs.Stat("eco_day_" + region) == gs.day ? gs.Stat("eco_n_" + region) : 0;
        }

        public static float RespawnMul()
        {
            var gs = GameState.I; if (gs == null) return 1f;
            string r = Region(); int n = TodayKills(r);
            float m = 1f + Mathf.Clamp01(n / 80f) * 1.2f;                       // 80 kill/hari = 2.2x lebih lambat
            int last = gs.Stat("eco_last_" + r);
            if (n == 0 && last > 0 && gs.day - last >= 3) m = 0.75f;           // dibiarkan -> ramai
            return m;
        }

        public static string Population(string region)
        {
            int n = TodayKills(region);
            var gs = GameState.I; int last = gs != null ? gs.Stat("eco_last_" + region) : 0;
            if (n >= 80) return "menipis";
            if (n >= 40) return "berkurang";
            if (n == 0 && last > 0 && gs.day - last >= 3) return "ramai";
            return "normal";
        }
    }

    /// <summary>Gelombang Serbuan di area hunt (hari ke-3 tiap 6 hari). 3 gelombang, hadiah gold + EXP bila semua dikalahkan.</summary>
    public class Invasion : MonoBehaviour
    {
        public static bool IsDay() { var gs = GameState.I; return gs != null && gs.day % 6 == 3; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { SceneManager.sceneLoaded -= OnLoaded; SceneManager.sceneLoaded += OnLoaded; Install(SceneManager.GetActiveScene()); }
        static void OnLoaded(Scene s, LoadSceneMode m) { Install(s); }
        static void Install(Scene s)
        {
            if (!s.name.StartsWith("HuntArea") || FindFirstObjectByType<Invasion>() != null) return;
            new GameObject("Invasion").AddComponent<Invasion>();
        }

        float born, nextWave; int wave; bool started, done; readonly List<Monster> alive = new List<Monster>(); List<string> pool;

        void Start() { born = Time.time; }

        void Update()
        {
            var gs = GameState.I; var pc = PlayerCombat.I; if (gs == null || pc == null || pc.IsDead) return;
            string key = "inv_" + SceneManager.GetActiveScene().name;
            if (done) return;
            if (!started)
            {
                if (!IsDay() || gs.level < 5 || gs.Stat(key) == gs.day) { if (Time.time - born > 8f) done = true; return; }
                if (HuntManager.I != null && HuntManager.I.defaultZone == "Arena Ujian") { done = true; return; }
                if (Time.time - born < 40f) return;
                pool = new List<string>();
                foreach (var m in Monster.All) { if (m == null || m.IsDead) continue; var d = MonsterDB.Get(m.DefId); if (d == null || d.passive || d.large || m.DefId == "dummy" || m.DefId.StartsWith("bandit")) continue; if (!pool.Contains(m.DefId)) pool.Add(m.DefId); }
                if (pool.Count == 0) { done = true; return; }
                started = true; gs.stats[key] = gs.day; wave = 0; nextWave = Time.time;
                if (TownUI.I != null) TownUI.I.Toast("GELOMBANG SERBUAN! Monster menyerbu area ini. Bertahan sampai 3 gelombang habis.");
                Audio.Sfx("boss_roar");
            }
            alive.RemoveAll(m => m == null || m.IsDead);
            if (wave < 3 && alive.Count == 0 && Time.time >= nextWave) { SpawnWave(pc.transform.position); wave++; nextWave = Time.time + 6f; }
            if (wave >= 3 && alive.Count == 0)
            {
                done = true;
                int gold = Difficulty.Gold(120 + 25 * gs.level), exp = 20 + gs.level * 4;
                gs.gold += gold; gs.AddExp(exp); gs.AddStat("invasions");
                if (TownUI.I != null) TownUI.I.Toast("Serbuan dipukul mundur! +" + gold + "g, +" + exp + " EXP");
                Audio.Sfx("quest_reward");
            }
        }

        void SpawnWave(Vector3 center)
        {
            int n = 4 + wave * 2;
            if (TownUI.I != null) TownUI.I.Toast("Gelombang " + (wave + 1) + "/3!");
            for (int i = 0; i < n; i++)
            {
                for (int a = 0; a < 20; a++)
                {
                    float ang = Random.value * 6.2832f, rad = Random.Range(16f, 26f);
                    Vector3 q = center + new Vector3(Mathf.Cos(ang) * rad, 0f, Mathf.Sin(ang) * rad), g;
                    if (!FarmAnimals.Ground(q, out g)) continue;
                    var go = new GameObject("Invader");
                    go.transform.position = g;
                    var m = go.AddComponent<Monster>();
                    m.defId = pool[Random.Range(0, pool.Count)]; m.noLeash = true;
                    m.hpMult = 1f + 0.1f * wave; m.atkMult = 1f + 0.05f * wave;
                    alive.Add(m); m.Alert();
                    break;
                }
            }
        }
    }
}
