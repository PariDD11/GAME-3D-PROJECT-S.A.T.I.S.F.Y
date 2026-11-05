using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Peti hadiah setelah boss Menara dikalahkan.</summary>
    public class TowerChest : Interactable
    {
        public int floor;
        bool opened;

        public override void Interact()
        {
            if (opened) return;
            Audio.Sfx("chest_open", transform.position);
            var gs = GameState.I;
            var def = MonsterDB.Get(TowerDB.BossId(floor));
            int gold = Difficulty.Gold(60 + floor * 12);
            gs.gold += gold;
            string got = gold + "g";
            if (def != null && def.loot != null)
            {
                string[] pick = { def.loot[0], def.rare != "" ? def.rare : def.loot[0] };
                foreach (var id in pick)
                {
                    if (!gs.HasSpace(id, 1)) continue;
                    gs.AddItem(id, 1);
                    got += ", " + ItemDB.Get(id).name;
                }
            }
            if (floor >= 50 && gs.HasSpace("potion", 2)) { gs.AddItem("potion", 2); got += ", 2 Ramuan"; }
            opened = true;
            prompt = "Peti kosong";
            enabled = false;
            gs.AddStat("tower_chests");
            TownUI.I.Toast("Peti boss lantai " + floor + ": " + got);
            Destroy(gameObject, 1.5f);
        }
    }
}
