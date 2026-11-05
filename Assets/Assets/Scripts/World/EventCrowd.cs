using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// Keramaian kota saat event (festival, pasar, kembang api, lomba berburu, aurora, migrasi kupu-kupu): warga tambahan bermunculan
    /// dan berjalan di rute warga yang sudah ada (jadi pasti di jalan yang bisa dilewati), lalu pergi saat event usai.
    /// Hanya di kota luar ruangan (Ashwatch = Town, Eldrath = DungeonCity, StartVillage). Otomatis, tanpa rebuild scene.
    /// Atur jumlah di tabel Size.
    /// </summary>
    public class EventCrowd : MonoBehaviour
    {
        static readonly string[] Scenes = { "Town", "DungeonCity", "StartVillage" };
        const int Cap = 36;

        // jumlah warga tambahan per id event (festival terbesar); bulan_merah = 0 (malam berbahaya, warga masuk rumah)
        static int Size(string id, string sceneName)
        {
            float k = sceneName == "StartVillage" ? 0.6f : 1f;
            int n;
            switch (id)
            {
                case "festival": n = 30; break;
                case "kembang_api": n = 22; break;
                case "pasar_bunga": case "pasar_panen": case "pasar_es": n = 16; break;
                case "lomba": n = 14; break;
                case "aurora": n = 12; break;
                case "kupu": n = 8; break;
                default: n = 0; break;
            }
            return Mathf.RoundToInt(n * k);
        }

        // jenis warga per event
        static int[] Mix(string id)
        {
            switch (id)
            {
                case "festival": return new[] { NpcKit.OWarga, NpcKit.OWarga, NpcKit.OBard, NpcKit.OPedagang, NpcKit.OKoki, NpcKit.OPelayan, NpcKit.OBangsawan, NpcKit.OPelajar };
                case "pasar_bunga": case "pasar_panen": case "pasar_es": return new[] { NpcKit.OPedagang, NpcKit.OPedagang, NpcKit.OWarga, NpcKit.OWarga, NpcKit.OPetani, NpcKit.OKoki };
                case "kembang_api": return new[] { NpcKit.OWarga, NpcKit.OWarga, NpcKit.OBangsawan, NpcKit.OBard, NpcKit.OPengembara, NpcKit.OPelajar };
                case "lomba": return new[] { NpcKit.OHunter, NpcKit.OHunter, NpcKit.OWarga, NpcKit.OPenjaga, NpcKit.OKsatria, NpcKit.OPedagang };
                default: return new[] { NpcKit.OWarga, NpcKit.OPengembara, NpcKit.OPelajar, NpcKit.OPenyihir };
            }
        }

        class Guest { public GameObject go; public float leaveAt = -1f; }
        readonly List<Guest> guests = new List<Guest>();
        readonly List<Transform[]> routes = new List<Transform[]>();
        Transform parent;
        float scanT, spawnT; string scene = ""; int seed; string forId = "";
        static bool made;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (made) return; made = true;
            var go = new GameObject("EventCrowd");
            DontDestroyOnLoad(go);
            go.AddComponent<EventCrowd>();
        }

        void OnEnable() { SceneManager.sceneLoaded += OnLoaded; }
        void OnDisable() { SceneManager.sceneLoaded -= OnLoaded; }
        void OnLoaded(Scene s, LoadSceneMode m)
        {
            guests.Clear(); routes.Clear(); parent = null;       // objek lama ikut hilang bersama scene
            scene = s.name; scanT = 2f; spawnT = 0f;
        }

        static bool CityScene(string n) { foreach (var s in Scenes) if (s == n) return true; return false; }

        void Update()
        {
            if (string.IsNullOrEmpty(scene)) scene = SceneManager.GetActiveScene().name;
            var gs = GameState.I;
            if (gs == null || !CityScene(scene) || !SeasonRuntime.Outdoors) return;

            bool want = SeasonEvents.Active && SeasonEvents.Today != null;
            string id = want ? SeasonEvents.Today.id : "";
            int target = want ? Mathf.Min(Cap, Size(id, scene)) : 0;

            // event berganti/usai: tamu lama dijadwalkan pergi
            if (id != forId)
            {
                foreach (var g in guests) if (g.leaveAt < 0f) g.leaveAt = Time.time + Random.Range(2f, 25f);
                forId = id; seed = gs.day * 7919 + id.GetHashCode();
            }

            int alive = 0;
            for (int i = guests.Count - 1; i >= 0; i--)
            {
                var g = guests[i];
                if (g.go == null) { guests.RemoveAt(i); continue; }
                if (g.leaveAt >= 0f && Time.time >= g.leaveAt && !Visible(g.go.transform.position)) { Destroy(g.go); guests.RemoveAt(i); continue; }
                if (g.leaveAt < 0f) alive++;
            }

            if (alive >= target) return;
            spawnT -= Time.deltaTime;
            if (spawnT > 0f) return;
            spawnT = 0.5f;
            scanT -= 0.5f;
            if (routes.Count == 0 || scanT <= 0f) { ScanRoutes(); scanT = 20f; }
            if (routes.Count == 0) return;
            SpawnOne(id);
        }

        static bool Visible(Vector3 p)
        {
            var cam = Camera.main; if (cam == null) return false;
            Vector3 v = cam.WorldToViewportPoint(p + Vector3.up);
            return v.z > 0f && v.x > -0.05f && v.x < 1.05f && v.y > -0.05f && v.y < 1.05f && (cam.transform.position - p).sqrMagnitude < 90f * 90f;
        }

        void ScanRoutes()
        {
            routes.Clear();
            foreach (var w in FindObjectsByType<WanderNPC>(FindObjectsSortMode.None))
            {
                if (w.waypoints == null || w.waypoints.Length < 2) continue;
                if (w.GetComponent<NpcSwapMark>() == null && w.GetComponent<NpcActor>() == null) continue;
                bool dup = false;
                foreach (var r in routes) if (r == w.waypoints) { dup = true; break; }
                if (!dup) routes.Add(w.waypoints);
            }
        }

        void SpawnOne(string id)
        {
            var rt = routes[Random.Range(0, routes.Count)];
            Transform start = rt[Random.Range(0, rt.Length)];
            if (start == null) return;
            // jangan muncul tepat di depan hidung pemain
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 d = start.position - cam.transform.position;
                if (d.sqrMagnitude < 12f * 12f && Visible(start.position)) return;
            }
            if (parent == null) { var g = new GameObject("EventCrowdNPCs"); parent = g.transform; }
            var mix = Mix(id);
            var rng = new System.Random(seed + guests.Count * 31 + Random.Range(0, 1000));
            int outfit = mix[rng.Next(mix.Length)];
            var spec = NpcKit.RandomSpec(outfit, rng);
            Vector3 pos = start.position + new Vector3(Random.Range(-0.6f, 0.6f), 0f, Random.Range(-0.6f, 0.6f));
            var go = NpcKit.Spawn(parent, "Tamu_" + spec.name, pos, Random.Range(0f, 360f), spec, true);
            go.transform.position = pos;
            var actor = go.GetComponent<NpcActor>();
            if (actor != null) actor.displayName = spec.name;
            var lab = MakeLabel(go.transform, spec.name + "\n<" + NpcKit.OutfitName[outfit] + ">", actor != null ? actor.height : 1.75f);
            var w = go.AddComponent<WanderNPC>();
            w.waypoints = rt; w.label = lab; w.speed = 1.1f + Random.value * 0.6f;
            if (outfit != NpcKit.OPenjaga && outfit != NpcKit.OKsatria) go.AddComponent<NpcChat>();
            guests.Add(new Guest { go = go });
            // pasang aturan Ganti NPC (model/foto/aksi) bila ada
            NpcSwapRuntime.ApplyTo(go);
        }

        static Transform MakeLabel(Transform parent, string text, float h)
        {
            var g = new GameObject("Label");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = new Vector3(0f, h + 0.3f, 0f);
            var tm = g.AddComponent<TextMesh>();
            tm.text = text; tm.fontSize = 48; tm.characterSize = 0.045f; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = Color.white;
#if UNITY_2022_2_OR_NEWER
            tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            tm.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
            TextMat.Apply(g.GetComponent<MeshRenderer>(), tm.font.material);
            return g.transform;
        }
    }
}
