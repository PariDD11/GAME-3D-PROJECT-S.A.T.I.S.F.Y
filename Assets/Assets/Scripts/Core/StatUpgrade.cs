using System;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n29: Upgrade Status memakai SP (Skill Point) + Level Senjata (11-50).
    /// Stat (rank disimpan di gs.stats "up_&lt;id&gt;", INT/WIS memakai gs.intel/gs.wis yang sudah ada):
    ///   HP (+12 HP maks), ATK (+1 ATK), DEF (+1 DEF), INT (+6 mana & +1% damage sihir), WIS (+4% pulih mana & -1% biaya sihir),
    ///   STA (+3 stamina maks), CRIT (+0,5% peluang kritis).
    /// Biaya per rank = 1 + rank/10 SP. Reset: kembalikan semua SP, bayar gold.
    /// Level Senjata: lihat WeaponLevel di bawah.
    /// </summary>
    public static class StatUpgrade
    {
        public class Stat { public string id, name, desc; public int max; public string unit; }

        public static readonly Stat[] All =
        {
            new Stat { id = "hp",   name = "HP",       max = 60, desc = "+12 HP maksimum per rank." },
            new Stat { id = "atk",  name = "ATK",      max = 50, desc = "+1 serangan per rank (semua job, termasuk sihir fisik)." },
            new Stat { id = "def",  name = "DEF",      max = 50, desc = "+1 pertahanan per rank." },
            new Stat { id = "int",  name = "INT",      max = 60, desc = "+6 mana maks dan +1% damage sihir per rank." },
            new Stat { id = "wis",  name = "WIS",      max = 45, desc = "+4% pemulihan mana dan -1% biaya sihir per rank (maks -45%)." },
            new Stat { id = "sta",  name = "STA",      max = 30, desc = "+3 stamina maksimum per rank (lari, hindar, serangan)." },
            new Stat { id = "crit", name = "KRITIS",   max = 30, desc = "+0,5% peluang serangan kritis per rank." },
        };

        public static int Rank(GameState gs, string id)
        {
            if (id == "int") return gs.intel;
            if (id == "wis") return gs.wis;
            return gs.Stat("up_" + id);
        }
        static void SetRank(GameState gs, string id, int v)
        {
            if (id == "int") gs.intel = v; else if (id == "wis") gs.wis = v; else gs.stats["up_" + id] = v;
        }

        public static int CostFor(int rank) { return Mathf.Max(1, Mathf.CeilToInt((1 + rank / 10) * Balance.SpCost)); }
        static int TotalCost(int rank) { int t = 0; for (int r = 0; r < rank; r++) t += CostFor(r); return t; }

        // ---- bonus (dipanggil GameState / PlayerCombat) ----
        public static int AtkBonus(GameState gs) { return gs.Stat("up_atk"); }
        public static int DefBonus(GameState gs) { return gs.Stat("up_def"); }
        public static float HpBonus(GameState gs) { return gs.Stat("up_hp") * 12f; }
        public static float StaBonus(GameState gs) { return gs.Stat("up_sta") * 3f; }
        public static float CritBonus(GameState gs) { return gs.Stat("up_crit") * 0.5f; }

        public static int SpentSp(GameState gs)
        {
            int t = gs.Stat("up_spent");
            if (t > 0) return t;
            foreach (var s in All) t += TotalCost(gs.Stat("up_" + s.id));   // save lama (n29) tanpa catatan SP terpakai: perkiraan dari rank
            return t;
        }

        public static bool Buy(GameState gs, Stat s)
        {
            int r = Rank(gs, s.id);
            if (r >= s.max) return false;
            int c = CostFor(r);
            if (gs.skillPoints < c) return false;
            gs.skillPoints -= c; gs.AddStat("up_spent", c);
            SetRank(gs, s.id, r + 1);
            if (s.id == "int" || s.id == "wis") gs.AddStat("up_" + s.id);
            Audio.Sfx("quest_progress");
            return true;
        }

        const int ResetGold = 300;

        static string Effect(GameState gs, Stat s)
        {
            int r = Rank(gs, s.id);
            switch (s.id)
            {
                case "hp": return "+" + (int)(r * 12) + " HP";
                case "atk": return "+" + r + " ATK";
                case "def": return "+" + r + " DEF";
                case "int": return "+" + r * 6 + " mana, +" + r + "% dmg sihir";
                case "wis": return "+" + r * 4 + "% regen, -" + Mathf.Min(45, r) + "% biaya";
                case "sta": return "+" + r * 3 + " stamina";
                default: return "+" + (r * 0.5f).ToString("0.0") + "% kritis";
            }
        }

        public static void Open(Action back)
        {
            var ui = TownUI.I;
            ui.Open("Upgrade Status",
                () =>
                {
                    var gs = GameState.I; var pc = PlayerCombat.I;
                    return "Skill Point (SP): " + gs.skillPoints + "     SP terpakai di status: " + SpentSp(gs) + "\n" +
                           "Total  ATK " + gs.TotalAtk + "   DEF " + gs.TotalDef + (pc != null ? "   HP " + Mathf.RoundToInt(pc.MaxHp) + "   Stamina " + Mathf.RoundToInt(pc.MaxStamina) : "") +
                           "   Kritis +" + (gs.Pas("crit")).ToString("0.#") + "%\n" +
                           "Biaya per rank = 1 SP, naik +1 tiap 10 rank. SP juga dipakai untuk skill, jadi pilih dengan bijak.";
                },
                () =>
                {
                    var gs = GameState.I; var l = new List<UIEntry>();
                    foreach (var ss in All)
                    {
                        var s = ss; int r = Rank(gs, s.id); bool full = r >= s.max; int c = CostFor(r);
                        l.Add(new UIEntry(s.name + "   rank " + r + "/" + s.max + "   (" + Effect(gs, s) + ")\n" + s.desc + (full ? "   [MAKS]" : "   Biaya " + c + " SP"),
                            () => { Buy(GameState.I, s); Open(back); }, !full && gs.skillPoints >= c));
                    }
                    int spent = SpentSp(gs);
                    l.Add(new UIEntry("Reset status (kembalikan " + spent + " SP, biaya " + ResetGold + "g)", () => Reset(back), spent > 0 && gs.gold >= ResetGold));
                    l.Add(new UIEntry("< Kembali", () => { if (back != null) back(); else ui.Close(); }));
                    return l;
                });
        }

        static void Reset(Action back)
        {
            var gs = GameState.I; int spent = SpentSp(gs);
            if (spent <= 0 || gs.gold < ResetGold) return;
            gs.gold -= ResetGold; gs.skillPoints += spent; gs.stats["up_spent"] = 0;
            foreach (var s in All)
            {
                if (s.id == "int") gs.intel = Mathf.Max(0, gs.intel - gs.Stat("up_int"));
                else if (s.id == "wis") gs.wis = Mathf.Max(0, gs.wis - gs.Stat("up_wis"));
                gs.stats["up_" + s.id] = 0;
            }
            if (TownUI.I != null) TownUI.I.Toast("Status direset. +" + spent + " SP kembali.");
            Open(back);
        }
    }

    /// <summary>
    /// Level Senjata 0-50. Senjata yang dipakai mendapat EXP senjata dari membunuh monster. Setiap 10 level (10, 20, 30, 40) level tertahan
    /// sampai senjata dievolusikan di Pandai Besi (gold + Iron Ingot + Golem Core); lalu lanjut Lv 11, 21, 31, 41 sampai Lv 50.
    /// Bonus: +0,8 ATK per level dan +4 ATK per evolusi. Data per senjata di gs.stats: wl_ (level), wx_ (exp), we_ (jumlah evolusi).
    /// </summary>
    public static class WeaponLevel
    {
        public const int Min = 0, Max = 50;
        static readonly string[] Stage = { "Tempaan", "Terasah", "Mulia", "Agung", "Legenda" };

        public static string EquippedId(GameState gs) { string id; return gs.equipment.TryGetValue("Weapon", out id) ? id : null; }
        public static int Level(GameState gs, string id) { return Mathf.Clamp(gs.Stat("wl_" + id), 0, Max); }
        public static int Evo(GameState gs, string id) { return Mathf.Clamp(gs.Stat("we_" + id), 0, 4); }
        public static int Cap(GameState gs, string id) { return Mathf.Min(Max, 10 * (Evo(gs, id) + 1)); }
        public static string StageName(GameState gs, string id) { return Stage[Evo(gs, id)]; }
        public static int ExpNeeded(int lv) { return 30 + Mathf.RoundToInt(lv * lv * 2.5f); }
        public static bool NeedsEvo(GameState gs, string id) { return Level(gs, id) >= Cap(gs, id) && Level(gs, id) < Max; }

        public static int AtkBonus(GameState gs)
        {
            string id = EquippedId(gs); if (id == null) return 0;
            return Mathf.RoundToInt(Level(gs, id) * 0.8f + Evo(gs, id) * 4f);
        }

        /// <summary>Dipanggil saat monster mati (Monster.Die).</summary>
        public static void OnKill(MonsterDef def, bool boss)
        {
            var gs = GameState.I; if (gs == null || def == null || def.id == "dummy") return;
            string id = EquippedId(gs); if (id == null) return;
            int lv = Level(gs, id), cap = Cap(gs, id);
            if (lv >= cap)
            {
                if (lv < Max && gs.Stat("wn_" + id + "_" + lv) == 0)
                {
                    gs.stats["wn_" + id + "_" + lv] = 1;
                    if (TownUI.I != null) TownUI.I.Toast("Senjata Lv " + lv + " siap dievolusikan! Temui Hadrik (Pandai Besi).");
                }
                return;
            }
            int gain = Mathf.Max(1, Mathf.RoundToInt(def.hp / 12f * Balance.WeaponExp)) * (boss ? 3 : 1);
            int xp = gs.Stat("wx_" + id) + gain;
            bool up = false;
            while (lv < cap && xp >= ExpNeeded(lv)) { xp -= ExpNeeded(lv); lv++; up = true; }
            if (lv >= cap) xp = 0;
            gs.stats["wx_" + id] = xp; gs.stats["wl_" + id] = lv;
            if (up && TownUI.I != null)
            {
                var d = ItemDB.Get(id);
                TownUI.I.Toast((d != null ? d.name : "Senjata") + " naik ke Level " + lv + (lv >= cap && lv < Max ? "  (siap evolusi di Hadrik)" : "") + "!");
                Audio.Sfx("levelup");
            }
        }

        static int GoldFor(int evo) { return Mathf.RoundToInt(400 * (evo + 1) * (evo + 1) * Balance.EvoCost); }
        static int IngotFor(int evo) { return Mathf.Max(1, Mathf.RoundToInt((3 + 2 * evo) * Balance.EvoCost)); }
        static int CoreFor(int evo) { return 1 + evo; }

        public static void OpenMenu(Action back)
        {
            var ui = TownUI.I;
            ui.Open("Evolusi & Level Senjata",
                () =>
                {
                    var gs = GameState.I; string id = EquippedId(gs);
                    if (id == null) return "Pasang senjata dulu di tas (Tab).";
                    var d = ItemDB.Get(id); int lv = Level(gs, id), cap = Cap(gs, id);
                    string expLine = lv >= Max ? "Level maksimal." : lv >= cap ? "Level tertahan di " + cap + ": EVOLUSIKAN senjata untuk lanjut ke Lv " + (lv + 1) + "." : "EXP senjata " + gs.Stat("wx_" + id) + " / " + ExpNeeded(lv) + "  (dapat dari membunuh monster)";
                    return (d != null ? d.name : id) + "  [" + StageName(gs, id) + "]   Level " + lv + " / " + Max + "\n" + expLine +
                           "\nBonus senjata: +" + AtkBonus(gs) + " ATK  (+0,8 per level, +4 per evolusi)\n" +
                           "Gold: " + gs.gold + "   Iron Ingot: " + gs.Count("iron_ingot") + "   Golem Core: " + gs.Count("golem_core") + "\n" +
                           "Senjata mulai dari Level 0. Evolusi di Lv 10, 20, 30, 40 lalu lanjut sampai Lv 50.";
                },
                () =>
                {
                    var gs = GameState.I; var l = new List<UIEntry>(); string id = EquippedId(gs);
                    if (id != null)
                    {
                        int evo = Evo(gs, id);
                        if (Level(gs, id) >= Max) l.Add(new UIEntry("Senjata sudah Level maksimal", () => { }, false));
                        else if (NeedsEvo(gs, id))
                        {
                            int g = GoldFor(evo), ing = IngotFor(evo), core = CoreFor(evo);
                            bool ok = gs.gold >= g && gs.Count("iron_ingot") >= ing && gs.Count("golem_core") >= core;
                            l.Add(new UIEntry("Evolusikan ke tahap " + Stage[evo + 1] + "   (" + g + "g + " + ing + " Iron Ingot + " + core + " Golem Core)", () =>
                            {
                                var s = GameState.I;
                                if (s.gold < g || s.Count("iron_ingot") < ing || s.Count("golem_core") < core) return;
                                s.RemoveItem("iron_ingot", ing); s.RemoveItem("golem_core", core); s.gold -= g;
                                s.stats["we_" + id] = evo + 1; s.stats["wl_" + id] = Level(s, id) + 1; s.stats["wx_" + id] = 0;
                                Audio.Sfx("quest_reward"); TownUI.I.Toast("Senjata berevolusi menjadi " + Stage[evo + 1] + "! Level " + Level(s, id) + ".");
                                OpenMenu(back);
                            }, ok));
                        }
                        else l.Add(new UIEntry("Belum perlu evolusi (kalahkan monster untuk menambah EXP senjata)", () => { }, false));
                    }
                    l.Add(new UIEntry("< Kembali", () => { if (back != null) back(); else ui.Close(); }));
                    return l;
                });
        }
    }
}
