using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Zona pemicu di jalan antara kota dan hutan. Berjalan melewatinya memuat scene tujuan dengan fade,
    /// lalu pemain muncul di titik spawn bernama (SpawnFromTown / SpawnFromForest) pada jalan yang sama.
    /// </summary>
    public class AreaTransition : MonoBehaviour
    {
        public enum Mode { ToForest, ToTown }

        public Mode mode = Mode.ToForest;
        public string destScene = "";            // kosong = ikut quest (default HuntArea_G); isi untuk gerbang tertentu (mis. HuntArea_Desert)
        public string townSpawn = "SpawnFromForest";
        public bool safeTravel;                        // true = jalan antar-kota (bukan ekspedisi): tanpa peringatan kompas hangus
        public string returnScene = "Town";         // tujuan mode ToTown (Ashwatch, Kota Eldrath, atau area lain)
        public string destSpawn = "SpawnFromTown";   // titik muncul di scene tujuan (dipakai perjalanan antar-kota)
        public Vector3 size = new Vector3(30f, 12f, 4f);

        Transform player;
        bool inside;
        float cooldown;

        void Update()
        {
            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p == null) return;
                player = p.transform;
            }
            if (ScreenFader.I.Busy) return;
            if (TownUI.I != null && TownUI.I.PanelOpen) return;

            Vector3 d = player.position - transform.position;
            bool now = Mathf.Abs(d.x) < size.x / 2f && Mathf.Abs(d.z) < size.z / 2f && Mathf.Abs(d.y) < size.y;
            if (now && !inside && Time.time > cooldown)
            {
                cooldown = Time.time + 1.5f;
                OnEnter();
            }
            inside = now;
        }

        void OnEnter()
        {
            if (mode == Mode.ToForest && (destScene == CityGate.DungeonCityScene || safeTravel))
            {
                // jalan raya ke Kota Dungeon: perjalanan aman, tanpa quest dan tanpa kompas
                SceneRouter.GoToSpawn(destScene, destSpawn);
                return;
            }
            if (mode == Mode.ToForest)
            {
                string msg, scene;
                if (!CityGate.CanLeave(destScene, out msg, out scene)) { TownUI.I.Toast(msg); return; }
                string sc = scene;
                GateGuard.Check(
                    () => SceneRouter.GoToSpawn(sc, "SpawnFromTown"),
                    () => GateGuard.PushBack(player, transform.position, 6f));
                return;
            }

            var q = GameState.I.activeQuest;
            if (safeTravel) q = null;      // kembali dari Kota Dungeon bukan akhir ekspedisi
            if (q != null && !q.completed)
            {
                TownUI.I.Open("Kembali ke Kota?",
                    () => "Quest \"" + q.title + "\" masih berjalan. Kompas/Peta Jelajahmu akan hangus; untuk kembali ke hutan kamu harus membelinya lagi di Gerbang Luar.",
                    () =>
                    {
                        var l = new List<UIEntry>();
                        l.Add(new UIEntry("Ya, kembali ke kota", () => SceneRouter.GoToSpawn(returnScene, townSpawn)));
                        l.Add(new UIEntry("Tidak, tetap di hutan", () => TownUI.I.Close()));
                        return l;
                    });
            }
            else SceneRouter.GoToSpawn(returnScene, townSpawn);
        }

    }
}
