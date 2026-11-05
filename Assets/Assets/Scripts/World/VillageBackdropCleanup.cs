using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// Menyembunyikan siluet bukit hijau polos di balik gerbang utara Desa Pemula ("SiluetHutanPemula") pada scene yang sudah
    /// dibangun, tanpa rebuild. Builder sudah tidak membuatnya lagi.
    /// </summary>
    public static class VillageBackdropCleanup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            SceneManager.sceneLoaded -= OnLoaded; SceneManager.sceneLoaded += OnLoaded;
            Hide();
        }
        static void OnLoaded(Scene s, LoadSceneMode m) { Hide(); }

        static void Hide()
        {
            var go = GameObject.Find("SiluetHutanPemula");
            if (go != null) go.SetActive(false);
        }
    }
}
