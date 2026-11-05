using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// Suasana (vibe) tiap scene, diterapkan saat runtime berdasarkan nama scene — tanpa perlu build ulang scene.
    /// Mengatur kabut, cahaya sekitar, warna matahari, dan mekanik ringan (haus di gurun, racun rawa, lava, petir, bisikan).
    /// Semua angka adalah placeholder untuk di-tuning di Unity.
    /// </summary>
    public class SceneVibe : MonoBehaviour
    {
        public enum Kind { None, Warm, Dim, Swamp, Desert, Volcano, Bone, Demon, Fortress, Market }

        public Kind kind;
        Color fog, ambient, sunTint; float fogDensity, tintAmt;
        Light sun; Transform player; PlayerCombat pc;
        float tick, nextBolt = 8f, flash, nextWhisper = 20f, nextMsg;
        Light bolt;
        Camera cam;

        static Kind For(string s)
        {
            if (s.Contains("Tutorial")) return Kind.Warm;
            if (s.Contains("HuntArea_G")) return Kind.Dim;
            if (s.Contains("Swamp")) return Kind.Swamp;
            if (s.Contains("Desert")) return Kind.Desert;
            if (s.Contains("Volcano")) return Kind.Volcano;
            if (s.Contains("Bone")) return Kind.Bone;
            if (s.Contains("Demon")) return Kind.Demon;
            if (s.Contains("Frontier")) return Kind.Dim;
            if (s == "DungeonCity" || s.Contains("Tower")) return Kind.Fortress;
            if (s == "StartVillage") return Kind.Warm;
            if (s == "Town") return Kind.Market;
            return Kind.None;
        }

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
            if (Object.FindFirstObjectByType<SceneVibe>() != null) return;
            var k = For(SceneManager.GetActiveScene().name);
            if (k == Kind.None) return;
            new GameObject("SceneVibe").AddComponent<SceneVibe>().kind = k;
        }

        void Start()
        {
            // fog (warna, kepadatan), ambient, tint matahari
            switch (kind)
            {
                case Kind.Warm: fog = new Color(0.80f, 0.90f, 1f); fogDensity = 0.0009f; ambient = new Color(0.68f, 0.74f, 0.9f); sunTint = new Color(1f, 0.93f, 0.78f); tintAmt = 0.35f; break;
                case Kind.Dim: fog = new Color(0.66f, 0.82f, 0.86f); fogDensity = 0.0014f; ambient = new Color(0.52f, 0.64f, 0.72f); sunTint = new Color(0.95f, 0.98f, 0.82f); tintAmt = 0.35f; break;
                case Kind.Swamp: fog = new Color(0.3f, 0.38f, 0.3f); fogDensity = 0.0075f; ambient = new Color(0.2f, 0.27f, 0.22f); sunTint = new Color(0.6f, 0.75f, 0.6f); tintAmt = 0.6f; break;
                case Kind.Desert: fog = new Color(0.92f, 0.8f, 0.58f); fogDensity = 0.0012f; ambient = new Color(0.8f, 0.68f, 0.5f); sunTint = new Color(1f, 0.85f, 0.55f); tintAmt = 0.7f; break;
                case Kind.Volcano: fog = new Color(0.30f, 0.12f, 0.08f); fogDensity = 0.0011f; ambient = new Color(0.36f, 0.2f, 0.15f); sunTint = new Color(1f, 0.55f, 0.35f); tintAmt = 0.8f; break;
                case Kind.Bone: fog = new Color(0.55f, 0.62f, 0.72f); fogDensity = 0.0020f; ambient = new Color(0.3f, 0.36f, 0.46f); sunTint = new Color(0.7f, 0.8f, 1f); tintAmt = 0.75f; break;
                case Kind.Demon: fog = new Color(0.24f, 0.07f, 0.12f); fogDensity = 0.0018f; ambient = new Color(0.3f, 0.13f, 0.2f); sunTint = new Color(0.95f, 0.35f, 0.5f); tintAmt = 0.8f; break;
                case Kind.Fortress: fog = new Color(0.5f, 0.52f, 0.58f); fogDensity = 0.0016f; ambient = new Color(0.42f, 0.43f, 0.48f); sunTint = new Color(0.85f, 0.88f, 1f); tintAmt = 0.35f; break;
                case Kind.Market: fog = new Color(0.84f, 0.90f, 0.98f); fogDensity = 0.0008f; ambient = new Color(0.72f, 0.74f, 0.88f); sunTint = new Color(1f, 0.94f, 0.82f); tintAmt = 0.3f; break;
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fog;
            RenderSettings.fogDensity = fogDensity;
            var go = GameObject.Find("Directional Light");
            if (go != null) sun = go.GetComponent<Light>();
            if (kind == Kind.Volcano || kind == Kind.Demon || kind == Kind.Bone || kind == Kind.Swamp)
            {
                bolt = new GameObject("Bolt").AddComponent<Light>();
                bolt.transform.SetParent(transform, false);
                bolt.type = LightType.Directional; bolt.intensity = 0f;
                bolt.transform.rotation = Quaternion.Euler(60f, 20f, 0f);
                bolt.color = kind == Kind.Demon ? new Color(1f, 0.5f, 0.9f) : kind == Kind.Volcano ? new Color(1f, 0.6f, 0.3f) : new Color(0.7f, 0.85f, 1f);
            }
        }

        void LateUpdate()
        {
            // pewarnaan setiap frame (siklus siang-malam menimpa warna matahari)
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            float dayK = 1f;
            var gs = GameState.I;
            if (gs != null) dayK = Mathf.Clamp01(Mathf.Sin((gs.timeOfDay - 6f) / 12f * Mathf.PI) * 0.7f + 0.3f);
            RenderSettings.ambientLight = ambient * Mathf.Lerp(0.6f, 1.1f, dayK);
            Color fc = fog * Mathf.Lerp(0.22f, 1f, dayK); fc.a = 1f;
            RenderSettings.fogColor = fc;
            // langit area hunt = warna kabut, supaya cakrawala menyatu (sebelumnya langit skybox gelap + pita terang di cakrawala)
            if (kind != Kind.Warm && kind != Kind.Fortress && kind != Kind.Market && !(kind == Kind.Dim && FantasyGrade.On))
            {
                if (cam == null || !cam.isActiveAndEnabled) cam = Camera.main;
                if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = fc; }
                RenderSettings.skybox = null;
            }
            if (sun != null) sun.color = Color.Lerp(sun.color, sunTint, tintAmt);
            if (bolt != null) { flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 3.5f); bolt.intensity = flash * 1.6f; }
        }

        void Update()
        {
            if (player == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) { player = p.transform; pc = p.GetComponent<PlayerCombat>(); }
                return;
            }
            var gs = GameState.I;
            float dt = Time.deltaTime;
            tick += dt;

            // Gurun: panas, haus lebih cepat; berteduh/bermalam hampir tidak terasa
            if (kind == Kind.Desert && gs != null)
            {
                gs.thirst = Mathf.Max(0f, gs.thirst - 0.08f * dt);
                if (gs.thirst <= 0f && tick >= 3f && pc != null && !pc.IsDead) { tick = 0f; pc.TakeDamage(2f, player.position); }
                if (Time.time >= nextMsg && gs.thirst < 30f && gs.thirst > 0f) { nextMsg = Time.time + 45f; Say("Tenggorokanmu kering. Cari oasis atau minum dari botol minum."); }
                else if (Time.time >= nextMsg && gs.thirst >= 30f) { nextMsg = Time.time + 90f; Say("Panas menyengat; kamu cepat haus di gurun."); }
            }
            // Rawa: air beracun
            else if (kind == Kind.Swamp && pc != null && !pc.IsDead)
            {
                if (player.position.y < 16.55f && tick >= 1.5f) { tick = 0f; pc.TakeDamage(2f, player.position); if (Time.time >= nextMsg) { nextMsg = Time.time + 20f; Say("Air rawa beracun! Segera naik ke daratan."); } }
            }
            // Vulkanik / Lembah Iblis: lava melukai
            else if ((kind == Kind.Volcano || kind == Kind.Demon) && pc != null && !pc.IsDead)
            {
                if (player.position.y < 16.5f && tick >= 0.8f && !OnHighGround()) { tick = 0f; pc.TakeDamage(kind == Kind.Volcano ? 12f : 10f, player.position); if (Time.time >= nextMsg) { nextMsg = Time.time + 15f; Say("Lava membakar! Menjauh dari aliran lava."); } }
            }
            if (kind == Kind.Bone && Time.time >= nextWhisper)
            {
                nextWhisper = Time.time + Random.Range(35f, 70f);
                Say(Random.value < 0.5f ? "Terdengar bisikan dingin dari kejauhan..." : "Angin membawa suara rintihan para prajurit yang gugur.");
            }
            if (bolt != null && Time.time >= nextBolt && !(kind == Kind.Volcano && VolcanoFx.Active))
            {
                nextBolt = Time.time + (kind == Kind.Demon ? Random.Range(6f, 16f) : kind == Kind.Swamp ? Random.Range(25f, 55f) : Random.Range(14f, 35f));
                flash = 1f;
                Audio.Sfx("thunder", player.position);
            }
        }

        /// <summary>Dipanggil VolcanoFx saat petir visual menyambar (kilat cahaya tanpa suara ganda).</summary>
        public void Strike() { flash = 1f; }

        /// <summary>Terrain di bawah kaki di atas permukaan lava = bukan sedang di lava (hindari luka palsu).</summary>
        bool OnHighGround()
        {
            var t = Terrain.activeTerrain;
            if (t == null || player == null) return false;
            return t.SampleHeight(player.position) + t.transform.position.y > 16.8f && player.position.y > t.SampleHeight(player.position) + t.transform.position.y - 0.6f;
        }

        static void Say(string m) { if (TownUI.I != null) TownUI.I.Toast(m); }
    }
}
