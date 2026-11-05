using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n27: Mode "Tantangan Menara". Terbuka setelah menamatkan cerita (Putaran Baru / NG+ 1+). Selalu mulai dari lantai 1 dengan satu aturan khusus.
    /// Tidak mengubah checkpoint Menara biasa (dungeonBest / dungeonBossCleared). Hadiah: gelar kosmetik (lantai 10 / 25 / 50 per aturan).
    /// </summary>
    public static class TowerChallenge
    {
        public class Rule { public string id, name, title, desc; }

        public static readonly Rule[] Rules =
        {
            new Rule { id = "nopotion", name = "Tanpa Ramuan", title = "Tangan Kosong", desc = "Ramuan HP dan Mana tidak bisa dipakai di dalam Menara." },
            new Rule { id = "timer", name = "Batas Waktu", title = "Pemburu Kilat", desc = "Tiap lantai punya batas waktu (2 menit, boss 4 menit). Waktu habis = tantangan gagal." },
            new Rule { id = "oneshot", name = "Satu Nyawa", title = "Tanpa Ampun", desc = "Pingsan sekali langsung gagal, tidak ada kesempatan kedua." },
        };

        public static string Active = "";
        public static bool On { get { return !string.IsNullOrEmpty(Active); } }
        public static bool Available { get { return Difficulty.NgLevel >= 1; } }
        public static bool Is(string id) { return Active == id; }

        public static bool BlockPotion()
        {
            if (Challenges.BlockPotion()) return true;
            if (!Is("nopotion")) return false;
            if (TownUI.I != null) TownUI.I.Toast("Tantangan 'Tanpa Ramuan': ramuan tidak boleh dipakai.");
            return true;
        }

        public static float TimeLimit(int floor) { return Is("timer") ? (TowerDB.IsBoss(floor) ? 240f : 120f) : 0f; }

        public static int Best(string id) { return GameState.I == null ? 0 : GameState.I.Stat("tc_best_" + id); }

        /// <summary>Dipanggil saat satu lantai bersih di mode tantangan.</summary>
        public static void RecordClear(int floor)
        {
            var gs = GameState.I; if (gs == null || !On) return;
            if (floor > gs.Stat("tc_best_" + Active)) gs.stats["tc_best_" + Active] = floor;
            if (floor >= 10) Titles.Unlock("tc_" + Active + "_1");
            if (floor >= 25) Titles.Unlock("tc_" + Active + "_2");
            if (floor >= 50) Titles.Unlock("tc_" + Active + "_3");
        }

        public static void OpenMenu(System.Action back)
        {
            var ui = TownUI.I;
            ui.Open("Tantangan Menara",
                () => (Available ? "Mulai dari lantai 1 dengan satu aturan khusus. Checkpoint Menara biasa tidak berubah. Hadiah: gelar kosmetik di lantai 10, 25, dan 50." :
                    "Terkunci. Tantangan Menara terbuka setelah kamu memulai Putaran Baru (NG+) lewat menu jeda setelah menamatkan cerita."),
                () =>
                {
                    var l = new List<UIEntry>();
                    foreach (var rr in Rules)
                    {
                        var r = rr;
                        l.Add(new UIEntry(r.name + "  (rekor lantai " + Best(r.id) + ")\n" + r.desc, () => { Active = r.id; TowerRun.startFloor = 1; DungeonEntrance.Enter(1); if (!SceneManagerIsTower()) Active = ""; }, Available));
                    }
                    l.Add(new UIEntry("< Kembali", () => { if (back != null) back(); }));
                    return l;
                });
        }

        static bool SceneManagerIsTower() { return true; }   // Enter() memuat scene secara asinkron; Active di-reset saat TowerManager dihancurkan
    }
}
