using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// Transisi sinematik saat memasuki kota/desa dari scene lain: layar hitam memudar menjadi pemandangan DRONE yang mengitari
    /// kawasan dari ketinggian, turun mendekat, lalu mendarat di belakang pemain; judul tempat tampil di tengah layar.
    /// Memakai sistem Cutscene (bilah sinema, bisa dilewati dengan E/Esc/klik). Otomatis, tanpa build ulang scene.
    /// Tidak diputar saat keluar masuk bangunan (interior), saat membuka game pertama kali, atau bila baru saja diputar (&lt; 90 dtk).
    /// Matikan lewat PlayerPrefs "hg_sceneintro" = 0.
    /// </summary>
    public class SceneIntro : MonoBehaviour
    {
        class Spot { public string title, sub; public Vector3 center; public float r0, r1, h0, h1, lookH; }
        static readonly Dictionary<string, Spot> spots = new Dictionary<string, Spot>();
        static string lastScene = "";
        static readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
        static SceneIntro inst;
        string title = "", sub = ""; float titleT = -99f;

        public static bool Enabled
        {
            get { return PlayerPrefs.GetInt("hg_sceneintro", 1) == 1; }
            set { PlayerPrefs.SetInt("hg_sceneintro", value ? 1 : 0); }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (inst != null) return;
            spots[GuildConfig.TownScene] = new Spot { title = "KOTA ASHWATCH", sub = "Benteng para Hunter", center = new Vector3(0f, 0f, -20f), r0 = 250f, r1 = 165f, h0 = 130f, h1 = 82f, lookH = 8f };      // denah Bukit Abu (n75): kota memanjang selatan-utara, pusat di sekitar sungai
            spots[StarterConfig.VillageScene] = new Spot { title = "DESA PEMULA", sub = "Awal perjalanan seorang Hunter", center = Vector3.zero, r0 = 125f, r1 = 82f, h0 = 78f, h1 = 50f, lookH = 2f };
            spots[CityGate.DungeonCityScene] = new Spot { title = "KOTA ELDRATH", sub = "Kota di kaki menara", center = Vector3.zero, r0 = 250f, r1 = 165f, h0 = 135f, h1 = 88f, lookH = 18f };
            var go = new GameObject("SceneIntro"); DontDestroyOnLoad(go);
            inst = go.AddComponent<SceneIntro>();
            SceneManager.sceneLoaded += (s, m) => { if (inst != null) inst.OnLoaded(s, m); };
        }

        void OnLoaded(Scene s, LoadSceneMode mode)
        {
            string prev = lastScene; lastScene = s.name;
            if (mode != LoadSceneMode.Single || !Enabled || prev == "" || prev == s.name) return;
            Spot sp;
            if (!spots.TryGetValue(s.name, out sp)) return;
            if (InteriorInfo.IsInterior(prev) || prev == GuildConfig.InteriorScene) return;            // keluar bangunan: tanpa drone
            float last;
            if (lastPlayed.TryGetValue(s.name, out last) && Time.realtimeSinceStartup - last < 90f) return;
            StartCoroutine(Run(s.name, sp));
        }

        IEnumerator Run(string scene, Spot sp)
        {
            GameObject p = null;
            for (int i = 0; i < 90 && p == null; i++) { yield return null; p = GameObject.FindGameObjectWithTag("Player"); }
            if (p == null) yield break;
            yield return new WaitForSecondsRealtime(0.3f);                  // beri waktu SceneRouter menaruh pemain di titik masuk
            if (SceneManager.GetActiveScene().name != scene || Cutscene.Active) yield break;
            p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) yield break;
            lastPlayed[scene] = Time.realtimeSinceStartup;

            Vector3 pp = p.transform.position;
            Vector3 fwd = p.transform.forward; fwd.y = 0f; if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward; fwd.Normalize();
            float pa = Mathf.Atan2(pp.x - sp.center.x, pp.z - sp.center.z) * Mathf.Rad2Deg;      // sudut pemain terhadap pusat kawasan
            float a0 = pa - 150f, a1 = pa - 90f, a2 = pa;
            float r2 = Mathf.Max(30f, sp.r1 * 0.5f), h2 = sp.h1 * 0.42f;
            // shot 3 mulai tepat di akhir shot 2 supaya gerakan tidak melompat
            float ea = a2 * Mathf.Deg2Rad;
            Vector3 endPos = sp.center + new Vector3(Mathf.Sin(ea) * r2, h2, Mathf.Cos(ea) * r2);
            Vector3 landPos = pp - fwd * 6.5f + Vector3.up * 3.6f;
            var shots = new List<Cutscene.Shot>
            {
                Cutscene.Orbit(sp.center, sp.r0, sp.r1, sp.h0, sp.h1, a0, a1, 4.8f, "", sp.lookH),
                Cutscene.Orbit(sp.center, sp.r1, r2, sp.h1, h2, a1, a2, 3.8f, sp.sub, sp.lookH),
                Cutscene.Lerp(endPos, landPos, sp.center + Vector3.up * sp.lookH, pp + Vector3.up * 1.6f, 3.0f, ""),
            };
            title = sp.title; sub = sp.sub; titleT = Time.unscaledTime;
            Cutscene.Play(shots, () => { titleT = -99f; });
        }

        static GUIStyle big, small;
        void OnGUI()
        {
            if (!Cutscene.Active || titleT < 0f) return;
            float t = Time.unscaledTime - titleT;
            if (t > 6.2f) return;
            float a = Mathf.Clamp01(Mathf.Min((t - 0.5f) / 1.0f, (6.2f - t) / 1.2f));
            if (a <= 0.01f) return;
            if (big == null)
            {
                big = new GUIStyle(GUI.skin.label) { fontSize = 58, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                small = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleCenter };
            }
            GUI.depth = -200;
            float cx = Screen.width * 0.5f, cy = Screen.height * 0.4f;
            var old = GUI.color;
            string spaced = Loc.T(title);
            var r = new Rect(0, cy - 40f, Screen.width, 80f);
            GUI.color = new Color(0f, 0f, 0f, 0.65f * a); GUI.Label(new Rect(r.x + 3, r.y + 3, r.width, r.height), spaced, big);
            GUI.color = new Color(1f, 0.93f, 0.7f, a); GUI.Label(r, spaced, big);
            float lw = Mathf.Lerp(0f, Screen.width * 0.34f, a);
            GUI.color = new Color(1f, 0.82f, 0.4f, 0.9f * a);
            GUI.DrawTexture(new Rect(cx - lw / 2f, cy + 44f, lw, 2f), Texture2D.whiteTexture);
            var r2 = new Rect(0, cy + 52f, Screen.width, 34f);
            GUI.color = new Color(0f, 0f, 0f, 0.6f * a); GUI.Label(new Rect(r2.x + 2, r2.y + 2, r2.width, r2.height), Loc.T(sub), small);
            GUI.color = new Color(0.95f, 0.95f, 0.9f, a); GUI.Label(r2, Loc.T(sub), small);
            GUI.color = old;
        }
    }
}
