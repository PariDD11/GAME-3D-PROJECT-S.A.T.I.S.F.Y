using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// Pindah antara Town dan interior Guild sambil menaruh pemain di titik yang benar.
    /// GameState sudah DontDestroyOnLoad, jadi gold, quest, dan tas ikut berpindah.
    /// </summary>
    public static class SceneRouter
    {
        static bool pending;
        static bool pendingGuildExit;
        static string pendingSpawn;
        static Vector3 pendingPos;
        static float pendingYaw;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
        }

        public static void EnterGuild() { Go(GuildConfig.InteriorScene, GuildConfig.InteriorEntryPos, 0f); }
        public static void ExitGuild()
        {
            pendingGuildExit = true;      // titik kembali dihitung dari posisi bangunan Guild di scene kota
            Go(GuildConfig.TownScene, GuildConfig.TownReturnPos, 180f);
        }

        /// <summary>Pindah scene dengan fade; pemain muncul di objek bernama spawnName pada scene tujuan.</summary>
        public static void GoToSpawn(string scene, string spawnName)
        {
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                if (TownUI.I != null) TownUI.I.Toast("Scene '" + scene + "' belum ada di Build Settings.");
                return;
            }
            if (ScreenFader.I.Busy) return;
            if (TownUI.I != null) TownUI.I.Close();
            ScreenFader.I.Transition(() =>
            {
                if (scene == "Town" && GameState.I != null) GameState.I.EndExpedition();
                pending = true;
                pendingSpawn = spawnName;
                pendingPos = Vector3.zero;
                pendingYaw = 0f;
                SceneManager.LoadScene(scene);
            }, 0.45f, 0.7f);
        }

        /// <summary>Seperti Go, tapi dengan fade layar. Dipakai pintu bangunan.</summary>
        public static void GoFade(string scene, Vector3 pos, float yaw)
        {
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                if (TownUI.I != null) TownUI.I.Toast("Scene '" + scene + "' belum ada di Build Settings.");
                return;
            }
            if (ScreenFader.I.Busy) return;
            if (TownUI.I != null) TownUI.I.Close();
            ScreenFader.I.Transition(() =>
            {
                if (scene == "Town" && GameState.I != null) GameState.I.EndExpedition();
                pending = true; pendingSpawn = null; pendingPos = pos; pendingYaw = yaw; pendingGuildExit = false;
                SceneManager.LoadScene(scene);
            }, 0.35f, 0.5f);
        }

        public static void Go(string scene, Vector3 pos, float yaw)
        {
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                pendingGuildExit = false;
                GuildServices.Say("Scene '" + scene + "' belum ada di Build Settings.");
                return;
            }
            pending = true;
            pendingPos = pos;
            pendingYaw = yaw;
            if (TownUI.I != null) TownUI.I.Close();
            SceneManager.LoadScene(scene);
        }

        static void OnLoaded(Scene s, LoadSceneMode mode)
        {
            if (!pending) return;
            pending = false;
            if (pendingGuildExit)
            {
                pendingGuildExit = false;
#if UNITY_2023_1_OR_NEWER
                var hall = Object.FindFirstObjectByType<GuildHall>();
#else
                var hall = Object.FindObjectOfType<GuildHall>();
#endif
                if (hall != null)
                {
                    Vector3 f = hall.transform.forward;
                    f.y = 0f;
                    f.Normalize();
                    pendingPos = hall.transform.position + f * 3f;
                    pendingPos.y = 1.1f;
                    pendingYaw = hall.transform.eulerAngles.y;     // menghadap menjauhi pintu
                }
            }
            if (pendingSpawn != null)
            {
                var sp = GameObject.Find(pendingSpawn);
                pendingSpawn = null;
                if (sp != null)
                {
                    pendingPos = sp.transform.position + Vector3.up * 1.1f;
                    pendingYaw = sp.transform.eulerAngles.y;
                }
            }
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return;
            var cc = p.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;          // CharacterController menolak teleport saat aktif
            p.transform.SetPositionAndRotation(pendingPos, Quaternion.Euler(0f, pendingYaw, 0f));
            if (cc != null) cc.enabled = true;
        }
    }
}
