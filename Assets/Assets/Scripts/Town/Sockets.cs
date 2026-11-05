using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Socket permata pada gear (Weapon/Head/Body/Legs/Hands). Jumlah socket = tier gear (1-3). Disimpan di GameState.stats "sock_&lt;itemId&gt;_&lt;n&gt;" = indeks permata + 1.</summary>
    public static class Sockets
    {
        public static readonly string[] Gems = { "gem_ruby", "gem_sapphire", "gem_emerald", "gem_topaz" };
        public const int RemoveCost = 40;

        public static string GemDesc(string id)
        {
            switch (id)
            {
                case "gem_ruby": return "Permata untuk socket gear (Blacksmith).\nSenjata: ATK +8. Gear lain: ATK +3.";
                case "gem_sapphire": return "Permata untuk socket gear (Blacksmith).\nMana maks +12, damage sihir +2% (senjata: +4%).";
                case "gem_emerald": return "Permata untuk socket gear (Blacksmith).\nGear non-senjata: DEF +4. Senjata: pemulihan HP 1.5% dari damage.";
                case "gem_topaz": return "Permata untuk socket gear (Blacksmith).\nPeluang kritis +2% (senjata: +4%), gold quest +2%.";
            }
            return "";
        }

        public static int SocketCount(string itemId) { var d = ItemDB.Get(itemId); return d == null ? 0 : Mathf.Clamp(d.tier, 1, 3); }
        static string Key(string itemId, int n) { return "sock_" + itemId + "_" + n; }
        public static int Gem(GameState gs, string itemId, int n) { return gs.Stat(Key(itemId, n)) - 1; }

        /// <summary>Jumlah permata jenis g pada gear yang sedang dipakai; weaponOnly: 1 = hanya senjata, 0 = hanya non-senjata, -1 = semua.</summary>
        static int Count(GameState gs, int g, int weaponOnly)
        {
            int c = 0;
            foreach (var s in GearTiers.EnchantSlots)
            {
                string id; if (!gs.equipment.TryGetValue(s, out id) || string.IsNullOrEmpty(id)) continue;
                bool w = s == "Weapon"; if (weaponOnly == 1 && !w) continue; if (weaponOnly == 0 && w) continue;
                int sc = SocketCount(id);
                for (int n = 0; n < sc; n++) if (Gem(gs, id, n) == g) c++;
            }
            return c;
        }

        public static int Atk(GameState gs) { return Count(gs, 0, 1) * 8 + Count(gs, 0, 0) * 3; }
        public static int Def(GameState gs) { return Count(gs, 2, 0) * 4; }
        public static float Effect(GameState gs, string eff)
        {
            switch (eff)
            {
                case "maxmana": return Count(gs, 1, -1) * 12f;
                case "spellpower": return Count(gs, 1, 1) * 4f + Count(gs, 1, 0) * 2f;
                case "lifesteal": return Count(gs, 2, 1) * 1.5f;
                case "crit": return Count(gs, 3, 1) * 4f + Count(gs, 3, 0) * 2f;
                case "gold": return Count(gs, 3, -1) * 2f;
            }
            return 0f;
        }

        static string GemName(int g) { return g < 0 ? "kosong" : ItemDB.Get(Gems[g]).name; }

        public static string Summary(GameState gs, string itemId)
        {
            int sc = SocketCount(itemId); var sb = new System.Text.StringBuilder();
            for (int n = 0; n < sc; n++) sb.Append("[").Append(GemName(Gem(gs, itemId, n))).Append("] ");
            return sb.ToString();
        }

        // ---------- menu ----------
        public static void OpenMenu(System.Action back)
        {
            var gs = GameState.I; var ui = TownUI.I;
            ui.Open("Socket Permata",
                () => "Gold: " + gs.gold + "   Bonus aktif: ATK +" + Atk(gs) + "  DEF +" + Def(gs) + "  Kritis +" + Effect(gs, "crit") + "%  Mana +" + Effect(gs, "maxmana") + "\nJumlah socket = tier gear (1-3). Melepas permata: " + RemoveCost + "g, permata kembali.",
                () =>
                {
                    var l = new List<UIEntry>();
                    foreach (var sl in GearTiers.EnchantSlots)
                    {
                        string slot = sl; string id;
                        if (!gs.equipment.TryGetValue(slot, out id) || string.IsNullOrEmpty(id)) continue;
                        var d = ItemDB.Get(id); string iid = id;
                        l.Add(new UIEntry(slot + ": " + (d != null ? d.name : id) + "   " + Summary(gs, id), () => OpenGear(slot, iid, () => OpenMenu(back))));
                    }
                    l.Add(new UIEntry("< Kembali", () => { if (back != null) back(); }));
                    return l;
                });
        }

        static void OpenGear(string slot, string itemId, System.Action back)
        {
            var gs = GameState.I; var ui = TownUI.I; var d = ItemDB.Get(itemId);
            ui.Open("Socket: " + (d != null ? d.name : itemId),
                () => "Gold: " + gs.gold + "\n" + Summary(gs, itemId) + (slot == "Weapon" ? "\nSenjata: efek permata dua kali lipat." : ""),
                () =>
                {
                    var l = new List<UIEntry>();
                    int sc = SocketCount(itemId);
                    for (int n = 0; n < sc; n++)
                    {
                        int k = n; int g = Gem(gs, itemId, n);
                        if (g >= 0)
                            l.Add(new UIEntry("Socket " + (n + 1) + ": " + GemName(g) + "  - lepas (" + RemoveCost + "g)", () =>
                            {
                                if (!gs.HasSpace(Gems[g], 1)) { ui.Toast("Tas penuh."); return; }
                                if (!gs.TrySpend(RemoveCost)) return;
                                gs.stats.Remove(Key(itemId, k)); gs.AddItem(Gems[g], 1); Audio.Sfx("gather", Vector3.zero); OpenGear(slot, itemId, back);
                            }));
                        else
                            for (int gi = 0; gi < Gems.Length; gi++)
                            {
                                int gg = gi; int have = gs.Count(Gems[gi]);
                                l.Add(new UIEntry("Socket " + (n + 1) + " kosong: pasang " + GemName(gi) + "  (punya " + have + ")", () =>
                                {
                                    if (!gs.RemoveItem(Gems[gg], 1)) return;
                                    gs.stats[Key(itemId, k)] = gg + 1; Audio.Sfx("quest_reward", Vector3.zero); ui.Toast(GemName(gg) + " terpasang."); OpenGear(slot, itemId, back);
                                }, have > 0));
                            }
                    }
                    l.Add(new UIEntry("< Kembali", () => { if (back != null) back(); }));
                    return l;
                });
        }
    }
}
