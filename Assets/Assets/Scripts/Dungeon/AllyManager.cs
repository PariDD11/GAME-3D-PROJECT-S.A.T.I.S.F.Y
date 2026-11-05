using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>Memasang partner di scene hunt dan Menara secara otomatis (tanpa perlu membangun ulang scene).</summary>
    public class AllyManager : MonoBehaviour
    {
        public static AllyManager I { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
        }

        static bool Allowed(string n)
        {
            return n.StartsWith("HuntArea") || n == "DungeonTower" || n == GuildConfig.TownScene || n == StarterConfig.VillageScene || n == CityGate.DungeonCityScene;
        }

        static void OnLoaded(Scene s, LoadSceneMode mode)
        {
            if (!Allowed(s.name)) return;
            if (FindFirstObjectByType<AllyManager>() != null) return;
            new GameObject("AllyManager").AddComponent<AllyManager>();
        }

        void Awake() { I = this; }
        void OnDestroy() { if (I == this) I = null; }

        float next; bool announced;

        /// <summary>Partner yang sudah dipilih (party) langsung muncul dan mengikuti pemain di kota, desa, area hunt, dan Menara; yang dilepas dihapus.</summary>
        void Update()
        {
            next -= Time.deltaTime; if (next > 0f) return; next = 0.5f;
            Sync();
        }

        void Sync()
        {
            var gd = GuildData.I;
            var player = GameObject.FindGameObjectWithTag("Player");
            if (gd == null || player == null) return;
            foreach (var a in new List<AllyUnit>(AllyUnit.All))
                if (a != null && (a.partner == null || !gd.InParty(a.partner))) Destroy(a.gameObject);
            int spawned = 0;
            for (int i = 0; i < gd.party.Count; i++)
            {
                var p = gd.party[i]; bool has = false;
                foreach (var a in AllyUnit.All) if (a != null && a.partner != null && a.partner.key == p.key) has = true;
                if (has) continue;
                var go = new GameObject("Ally");
                var au = go.AddComponent<AllyUnit>();
                au.Init(p, i);
                au.Teleport(player.transform.position + new Vector3(i % 2 == 0 ? -2.4f : 2.4f, 0f, -2.2f));
                spawned++;
                if (TownUI.I != null) TownUI.I.Toast(p.name + " (" + p.role + ") ikut bersamamu dan akan bertarung di sisimu.");
            }
        }

        /// <summary>Dipakai Menara saat pindah lantai: partner bangkit dan menyusul pemain.</summary>
        public static void ReviveAndGather(Vector3 near)
        {
            int i = 0;
            foreach (var a in new List<AllyUnit>(AllyUnit.All))
            {
                a.Revive(1f);
                a.Teleport(near + new Vector3(i % 2 == 0 ? -2.4f : 2.4f, 0f, -2.2f));
                i++;
            }
        }
    }
}
