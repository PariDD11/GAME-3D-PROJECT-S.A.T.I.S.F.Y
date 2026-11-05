using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    [System.Serializable]
    public class ZoneInfo
    {
        public string name;
        public Vector3 center;
        public float radius;
    }

    /// <summary>
    /// Pengelola area hunt: progres quest, jejak monster, target kompas, deteksi zona, gagal quest, kembali ke kota.
    /// Kompas (jarum arah target setelah jejak cukup) dan Minimap (peta + petunjuk area) dibeli di Gerbang Luar tiap berangkat, hangus saat pulang.
    /// </summary>
    public class HuntManager : MonoBehaviour
    {
        public static HuntManager I { get; private set; }

        public ZoneInfo[] zones;
        public int tracksNeeded = 3;
        public string defaultZone = "Hutan Lerai";
        public string townSpawn = "SpawnFromForest";      // titik muncul di scene Town saat pulang (Hutan: selatan, Gurun: timur)
        public string returnScene = "Town";               // scene tujuan saat pulang (Menara: DungeonCity)

        public string CurrentZone { get; private set; }

        readonly Dictionary<string, int> tracks = new Dictionary<string, int>();
        Transform player;
        string lastZone = "";

        /// <summary>v35z102n36: bila diisi, "pulang ke kota" = teleport (dengan fade) ke titik ini di scene yang sama (Hutan Pemula menyatu dengan desa).</summary>
        public Transform localReturn;

        void Awake() { I = this; CurrentZone = defaultZone; }
        // objek ini bisa dinyalakan/dimatikan oleh SeamlessHunt: I hanya ada selama mode berburu aktif
        void OnEnable() { I = this; lastZone = ""; }
        void OnDisable() { if (I == this) I = null; }
        void OnDestroy() { if (I == this) I = null; }

        void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            if (TownUI.I != null) TownUI.I.Toast(defaultZone == "Arena Ujian" ? "Arena Ujian Guild. Ujian dimulai sebentar lagi." : defaultZone == "Menara Eldrath" ? "Memasuki Menara Eldrath. Tidak ada kompas di sini; kalahkan semua musuh untuk membuka jalan naik." : defaultZone == "Hutan Pemula" ? "Hutan Pemula: bunuh Slime dan Goblin, kumpulkan Herb, capai Level 5. Pondok di dekat gerbang adalah tempat pulang." : defaultZone == "Rawa Terkutuk" ? "Tiba di Pos Rawa. Kabut tebal; waspadai suara di balik pepohonan." : defaultZone == "Gurun Zahar" ? "Tiba di Base Camp Oasis, Gurun Zahar." : "Tiba di Base Camp Hutan Lerai.");
        }

        void Update()
        {
            if (player == null || zones == null) return;
            string z = ZoneNameAt(player.position);
            CurrentZone = z;
            if (z != lastZone)
            {
                lastZone = z;
                if (TownUI.I != null) TownUI.I.Toast("Memasuki: " + z);
            }
        }

        public string ZoneNameAt(Vector3 p)
        {
            if (zones == null) return defaultZone;
            string best = defaultZone;
            float bestRatio = 1f;
            foreach (var z in zones)
            {
                Vector3 d = p - z.center;
                d.y = 0f;
                float ratio = d.magnitude / Mathf.Max(1f, z.radius);
                if (ratio < bestRatio) { bestRatio = ratio; best = z.name; }
            }
            return best;
        }

        // ---------------- Progres quest ----------------
        public static void Report(string kind, string id)
        {
            var gs = GameState.I;
            if (gs == null) return;
            StarterQuests.OnEvent(kind, id);
            var q = gs.activeQuest;
            if (q == null || q.completed) return;
            bool match =
                (kind == "kill" && (q.type == "hunt" || q.type == "exterm") && q.targetId == id) ||
                (kind == "gather" && q.type == "gather" && q.targetId == id);
            if (!match) return;

            gs.questProgress++;
            if (gs.questProgress >= q.targetCount)
            {
                q.completed = true;
                Audio.Sfx("quest_done");
                if (TownUI.I != null) TownUI.I.Toast("QUEST SELESAI! Kembali ke Base Camp, pulang ke kota, lalu lapor ke Guild.");
            }
            else { Audio.Sfx("quest_progress"); if (TownUI.I != null) TownUI.I.Toast("Progres quest: " + gs.questProgress + "/" + q.targetCount); }
        }

        // ---------------- Jejak ----------------
        public int Tracks(string monsterId)
        {
            int n;
            return tracks.TryGetValue(monsterId, out n) ? n : 0;
        }

        /// <summary>Mengembalikan true bila jejak ini milik buruan quest aktif.</summary>
        public bool AddTrack(string monsterId)
        {
            var q = GameState.I.activeQuest;
            if (q == null || q.completed || q.type != "hunt" || q.targetId != monsterId) return false;
            tracks[monsterId] = Tracks(monsterId) + 1;
            Audio.Sfx("track_found");
            int n = tracks[monsterId];
            if (TownUI.I != null)
                TownUI.I.Toast(n >= tracksNeeded ? "Jejak cukup! Kompas kini menunjuk ke arah buruanmu." : "Jejak ditemukan (" + n + "/" + tracksNeeded + ").");
            return true;
        }

        // ---------------- Petunjuk target ----------------
        Monster NearestMonster(string id)
        {
            Monster best = null;
            float bd = float.MaxValue;
            if (player == null) return null;
            foreach (var m in Monster.All)
            {
                if (m.IsDead || m.DefId != id) continue;
                float d = (m.transform.position - player.position).sqrMagnitude;
                if (d < bd) { bd = d; best = m; }
            }
            return best;
        }

        GatherNode NearestNode(string itemId)
        {
            GatherNode best = null;
            float bd = float.MaxValue;
            if (player == null) return null;
            foreach (var n in GatherNode.Nodes)
            {
                if (n.itemId != itemId) continue;
                float d = (n.transform.position - player.position).sqrMagnitude;
                if (d < bd) { bd = d; best = n; }
            }
            return best;
        }

        bool TargetPosition(out Vector3 pos, out string label)
        {
            pos = Vector3.zero;
            label = "";
            var q = GameState.I.activeQuest;
            if (q == null || q.completed) return false;
            if (q.type == "gather")
            {
                var n = NearestNode(q.targetId);
                if (n == null) return false;
                pos = n.transform.position;
                label = ItemDB.Get(q.targetId).name;
                return true;
            }
            var m = NearestMonster(q.targetId);
            if (m == null) return false;
            pos = m.transform.position;
            label = m.Name;
            return true;
        }

        public bool CompassActive(out Vector3 pos, out string label)
        {
            pos = Vector3.zero;
            label = "";
            var gs = GameState.I;
            var q = gs.activeQuest;
            if (q == null || q.completed) return false;
            if (!gs.HasKompas) return false;          // tanpa kompas/minimap jelajah, jarum tidak ada
            bool ready = q.type != "hunt" || Tracks(q.targetId) >= tracksNeeded;
            if (!ready) return false;
            return TargetPosition(out pos, out label);
        }

        /// <summary>Peta Standard ke atas: menampilkan nama area tempat target berada.</summary>
        public string AreaHint()
        {
            var gs = GameState.I;
            if (!gs.HasMinimap) return "";
            Vector3 p;
            string label;
            if (!TargetPosition(out p, out label)) return "";
            return "Area target: " + ZoneNameAt(p);
        }

        // ---------------- Gagal & kembali ----------------
        public void FailQuest()
        {
            var gs = GameState.I;
            string msg = GuildData.I != null ? GuildData.I.FailActiveQuest() : "";
            gs.activeQuest = null;
            gs.questProgress = 0;
            if (TownUI.I != null) TownUI.I.Toast("Pingsan 3 kali. " + msg + " Kamu dibawa pulang ke kota.");
            StartCoroutine(DelayedReturn(3.5f));
        }

        IEnumerator DelayedReturn(float s)
        {
            yield return new WaitForSeconds(s);
            ReturnToTown();
        }

        public void ReturnToTown()
        {
            if (localReturn != null)
            {
                if (ScreenFader.I.Busy) return;
                if (TownUI.I != null) TownUI.I.Close();
                var dest = localReturn;
                ScreenFader.I.Transition(() =>
                {
                    var p = GameObject.FindGameObjectWithTag("Player");
                    if (p == null) return;
                    var cc = p.GetComponent<CharacterController>();
                    if (cc != null) cc.enabled = false;
                    p.transform.SetPositionAndRotation(dest.position + Vector3.up * 1.1f, Quaternion.Euler(0f, dest.eulerAngles.y, 0f));
                    if (cc != null) cc.enabled = true;
                }, 0.45f, 0.7f);
                return;
            }
            if (!Application.CanStreamedLevelBeLoaded(returnScene))
            {
                if (TownUI.I != null) TownUI.I.Toast("Scene '" + returnScene + "' belum ada di Build Settings.");
                return;
            }
            SceneRouter.GoToSpawn(returnScene, townSpawn);     // muncul di jalan di luar gerbang luar yang sesuai
        }
    }
}
