using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Pintu masuk Menara Eldrath (dungeon 100 lantai, boss tiap 10 lantai) di pusat Kota Dungeon.
    /// Pilih lantai awal: Lantai 1 atau checkpoint (lantai tepat setelah boss yang sudah dikalahkan).
    /// </summary>
    public class DungeonEntrance : Interactable
    {
        public const int Floors = TowerDB.Floors, BossEvery = TowerDB.BossEvery;
        public const string TowerScene = "DungeonTower";

        public override void Interact()
        {
            var gs = GameState.I;
            TownUI.I.Open("Menara Eldrath",
                () =>
                {
                    int nextBoss = Mathf.Min(Floors, (gs.dungeonBossCleared / BossEvery + 1) * BossEvery);
                    string quest = "";
                    var q = gs.activeQuest;
                    if (q != null)
                    {
                        var gq = GuildQuestDB.Find(q.id);
                        if (gq != null && gq.huntScene == TowerScene)
                            quest = "\n\nQuest aktif: " + q.title + (q.completed ? " (selesai, lapor ke Guild)" : "  [" + gs.questProgress + "/" + q.targetCount + "]");
                    }
                    return "Dungeon 100 lantai. Setiap 10 lantai ada boss penjaga; mengalahkannya menyimpan checkpoint.\n\n" +
                           "Lantai tertinggi: " + (gs.dungeonBest <= 0 ? "-" : gs.dungeonBest.ToString()) +
                           "\nBoss terakhir dikalahkan: lantai " + (gs.dungeonBossCleared <= 0 ? "-" : gs.dungeonBossCleared.ToString()) +
                           "\nBoss berikutnya: lantai " + nextBoss + "  (" + (MonsterDB.Get(TowerDB.BossId(nextBoss)) != null ? MonsterDB.Get(TowerDB.BossId(nextBoss)).name : "?") + ")" +
                           "\nParty: " + GuildData.I.party.Count + " partner ikut." + quest;
                },
                () =>
                {
                    var l = new List<UIEntry>();
                    foreach (int f in TowerDB.StartFloors(gs.dungeonBossCleared))
                    {
                        int fl = f;
                        string label = fl == 1 ? "Mulai dari Lantai 1  (" + TowerDB.TierName(1) + ")"
                                               : "Lanjut dari Checkpoint: Lantai " + fl + "  (" + TowerDB.TierName(fl) + ", Lv " + TowerDB.RecommendedLevel(fl) + "+)";
                        l.Add(new UIEntry(label, () => Enter(fl)));
                    }
                    l.Add(new UIEntry(TowerChallenge.Available ? "Tantangan Menara >" : "Tantangan Menara (terbuka setelah Putaran Baru)", () => TowerChallenge.OpenMenu(Interact), TowerChallenge.Available));
                    l.Add(new UIEntry("Papan Peringkat Menara >", () => GuildMenus.OpenRanking("tower")));
                    return l;
                });
        }

        public static void Enter(int floor)
        {
            if (!Application.CanStreamedLevelBeLoaded(TowerScene))
            {
                TownUI.I.Toast("Scene 'DungeonTower' belum dibuat (menu Hunter Guild > Build Dungeon Tower).");
                return;
            }
            var gsq = GameState.I.activeQuest;
            if (gsq != null && !gsq.completed)
            {
                var gq = GuildQuestDB.Find(gsq.id);
                string pm;
                if (gq != null && gq.huntScene == TowerScene && !GuildServices.PartyReady(out pm)) { TownUI.I.Toast(pm); return; }
                if (gq != null && gq.towerFloor > 0 && floor > gq.towerFloor) { TownUI.I.Toast("Lantai awal ini sudah melewati target quest (boss lantai " + gq.towerFloor + ")."); return; }
            }
            TowerRun.startFloor = floor;
            TownUI.I.Close();
            SceneRouter.GoToSpawn(TowerScene, "SpawnFromTown");
        }
    }
}
