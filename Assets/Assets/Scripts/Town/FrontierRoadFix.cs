using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// Pengaman runtime untuk scene HuntArea_Frontier lama (tanpa rebuild): ujung utara jalan raya Perbatasan harus tembus ke Ashwatch.
    /// Bila scene belum memiliki zona pindah ke "Town", dibuat otomatis di ujung jalan (di depan penghalang utara).
    /// </summary>
    public static class FrontierRoadFix
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            SceneManager.sceneLoaded -= OnLoaded; SceneManager.sceneLoaded += OnLoaded;
            Fix(SceneManager.GetActiveScene());
        }

        static void OnLoaded(Scene s, LoadSceneMode m) { Fix(s); }

        static void Fix(Scene s)
        {
            if (!s.IsValid() || s.name != "HuntArea_Frontier") return;
            foreach (var at in Object.FindObjectsByType<AreaTransition>(FindObjectsSortMode.None))
                if (at != null && at.mode == AreaTransition.Mode.ToTown && at.returnScene == "Town") return;      // sudah ada
            var sp = GameObject.Find("SpawnFromBorder");
            if (sp == null) return;
            var go = new GameObject("ToAshwatch_Runtime");
            go.transform.position = sp.transform.position + new Vector3(0f, 0f, 35f);
            var at2 = go.AddComponent<AreaTransition>();
            at2.mode = AreaTransition.Mode.ToTown;
            at2.returnScene = "Town"; at2.townSpawn = "SpawnFromDungeon"; at2.safeTravel = true;
            at2.size = new Vector3(40f, 14f, 6f);
            Debug.Log("[Hunter Guild] Zona jalan ke Ashwatch dibuat otomatis di HuntArea_Frontier.");
        }
    }
}
