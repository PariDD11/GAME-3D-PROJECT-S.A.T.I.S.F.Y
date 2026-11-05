using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Profil Hunter. Dibuka dengan tombol H atau tombol "Profil" di HUD. Halaman: Ringkasan, Atribut, Perlengkapan, Job &amp; Skill, Catatan Perburuan, dan Ubah Penampilan.
    /// </summary>
    public static class ProfileUI
    {
        const string Blue = "<color=#9FD3FF>", End = "</color>";
        static string Gold { get { return "<color=#" + ColorUtility.ToHtmlStringRGB(UiStyle.Cur.Gold) + ">"; } }      // warna aksen mengikuti job
        static string Dim { get { return "<color=#" + ColorUtility.ToHtmlStringRGB(UiStyle.Cur.Muted) + ">"; } }

        static string N(int v) { return v.ToString("N0", System.Globalization.CultureInfo.InvariantCulture); }
        static string Row(string k, string v) { return Dim + k + End + "  " + v + "\n"; }

        public static void Open() { Summary(); }

        static List<UIEntry> Nav(int page)
        {
            var l = new List<UIEntry>();
            if (page != 0) l.Add(new UIEntry("Ringkasan", Summary));
            if (page != 1) l.Add(new UIEntry("Atribut & Statistik tempur >", Attributes));
            l.Add(new UIEntry("Upgrade Status dengan SP (" + GameState.I.skillPoints + " SP) >", () => StatUpgrade.Open(Attributes)));
            if (page != 2) l.Add(new UIEntry("Perlengkapan >", Gear));
            if (page != 3) l.Add(new UIEntry("Job & Skill >", Skills));
            if (page != 4) l.Add(new UIEntry("Catatan Perburuan >", Records));
            if (HuntManager.I == null) l.Add(new UIEntry("Ubah penampilan karakter >", () => { TownUI.I.Close(); CharacterCreatorUI.Open(false); }));
            l.Add(new UIEntry("Tutup (Esc)", () => TownUI.I.Close()));
            return l;
        }

        static string JobLine(GameState gs)
        {
            if (gs.NoJob || (gs.requireJobChoice && !gs.jobChosen)) return "Belum memilih job (Level 5, temui Veteran Brann)";
            return JobInfo.Display(gs);
        }

        // ------------------------------------------------------------------ 0 Ringkasan
        static void Summary()
        {
            TownUI.I.Open("Profil Hunter",
                () =>
                {
                    var gs = GameState.I; var cd = CharacterData.Current; var pc = PlayerCombat.I;
                    string nm = string.IsNullOrEmpty(cd.name) ? "Hunter" : cd.name;
                    int need = GameState.ExpToNext(Mathf.Max(1, gs.level));
                    var sb = new StringBuilder();
                    sb.Append(Gold).Append(nm).Append(End).Append("   Rank ").Append(gs.rank).Append("   Level ").Append(gs.level).Append("\n");
                    sb.Append(Row("Job", JobLine(gs)));
                    sb.Append(Row("EXP", gs.level >= GameState.LevelCap ? "MAX" : N(gs.exp) + " / " + N(need)));
                    if (pc != null)
                    {
                        sb.Append(Row("HP", N(Mathf.CeilToInt(pc.hp)) + " / " + N(Mathf.RoundToInt(pc.MaxHp))));
                        sb.Append(Row(gs.IsMage ? "MP" : "Stamina", gs.IsMage ? N(Mathf.RoundToInt(pc.mana)) + " / " + N(Mathf.RoundToInt(pc.MaxMana)) : N(Mathf.RoundToInt(pc.stamina)) + " / " + N(Mathf.RoundToInt(pc.MaxStamina))));
                    }
                    sb.Append(Row("Gold", N(gs.gold)));
                    sb.Append(Row("Skill Point", gs.skillPoints.ToString()));
                    sb.Append(Row("Waktu", "Hari " + gs.day + ", " + gs.Clock + (gs.IsFatigued ? "  " + "<color=#FF8866>[LELAH]</color>" : "")));
                    sb.Append(Row("Reputasi Guild", GuildData.I.reputation.ToString()));
                    return sb.ToString();
                },
                () => Nav(0));
        }

        // ------------------------------------------------------------------ 1 Atribut
        static void Attributes()
        {
            TownUI.I.Open("Atribut & Statistik",
                () =>
                {
                    var gs = GameState.I; var pc = PlayerCombat.I;
                    var sb = new StringBuilder();
                    sb.Append(Gold).Append("Tempur").Append(End).Append("\n");
                    sb.Append(Row("Serangan (ATK)", gs.TotalAtk.ToString()));
                    sb.Append(Row("Pertahanan (DEF)", gs.TotalDef.ToString()));
                    sb.Append(Row("Bonus damage pasif", "+" + Mathf.RoundToInt((gs.DmgMult - 1f) * 100f) + "%"));
                    if (pc != null) sb.Append(Row("HP maks", N(Mathf.RoundToInt(pc.MaxHp)))).Append(Row("Stamina maks", N(Mathf.RoundToInt(pc.MaxStamina))));
                    if (gs.IsMage)
                    {
                        sb.Append("\n").Append(Gold).Append("Sihir").Append(End).Append("\n");
                        sb.Append(Row("INT (mana maks + damage)", gs.intel.ToString()));
                        sb.Append(Row("WIS (pulih mana + hemat mana)", gs.wis.ToString()));
                        sb.Append(Row("Poin atribut tersisa", gs.statPoints.ToString()));
                        if (pc != null) sb.Append(Row("Mana maks", N(Mathf.RoundToInt(pc.MaxMana))));
                    }
                    sb.Append("\n").Append(Gold).Append("Bertahan hidup").Append(End).Append("\n");
                    sb.Append(Row("Lapar", Mathf.RoundToInt(gs.hunger) + "%"));
                    sb.Append(Row("Haus", Mathf.RoundToInt(gs.thirst) + "%"));
                    sb.Append(Row("Botol Minum", gs.canteen + "/" + GameState.CanteenMax));
                    sb.Append(Row("Buff makan", string.IsNullOrEmpty(gs.mealBuff) ? "-" : gs.mealBuff + " (" + Mathf.CeilToInt(MealBuff.Left) + " dtk)"));
                    return sb.ToString();
                },
                () => Nav(1));
        }

        // ------------------------------------------------------------------ 2 Perlengkapan
        static void Gear()
        {
            TownUI.I.Open("Perlengkapan",
                () =>
                {
                    var gs = GameState.I; var sb = new StringBuilder();
                    if (gs.equipment.Count == 0) return Dim + "Belum ada perlengkapan terpasang. Buka tas (Tab) untuk memasang senjata dan armor." + End;
                    foreach (var kv in gs.equipment)
                    {
                        var d = ItemDB.Get(kv.Value);
                        string name = d != null ? d.name : kv.Value;
                        string extra = "";
                        if (d != null)
                        {
                            if (d.atk > 0) extra += "  ATK +" + d.atk;
                            if (d.def > 0) extra += "  DEF +" + d.def;
                            if (d.mana > 0) extra += "  MP +" + d.mana;
                        }
                        float du, mx;
                        if (gs.durability.TryGetValue(kv.Key, out du) && gs.maxDurability.TryGetValue(kv.Key, out mx) && mx > 0f) extra += "  " + Blue + "Durabilitas " + Mathf.RoundToInt(du / mx * 100f) + "%" + End;
                        sb.Append(Dim).Append(kv.Key).Append(End).Append("  ").Append(name).Append(extra).Append("\n");
                    }
                    { string wid = WeaponLevel.EquippedId(gs); if (wid != null) sb.Append("\n").Append(Row("Level senjata", WeaponLevel.Level(gs, wid) + " / " + WeaponLevel.Max + "  [" + WeaponLevel.StageName(gs, wid) + "]")); }
                    sb.Append("\n").Append(GearSets.Describe(gs)).Append("\n").Append(Row("Total ATK", gs.TotalAtk.ToString())).Append(Row("Total DEF", gs.TotalDef.ToString()));
                    return sb.ToString();
                },
                () => Nav(2));
        }

        // ------------------------------------------------------------------ 3 Job & Skill
        static string[] SlotKeys = { "Z", "X", "C", "V (tengah)", "R", "T", "Q" };

        static void Skills()
        {
            TownUI.I.Open("Job & Skill",
                () =>
                {
                    var gs = GameState.I; var sb = new StringBuilder();
                    sb.Append(Row("Job", JobLine(gs)));
                    if (gs.jobChosen)
                    {
                        sb.Append(Row("Evolusi Lv" + JobInfo.EvoLevel, string.IsNullOrEmpty(gs.evo) ? (gs.CanEvolve ? Blue + "SIAP berevolusi! (tekan K)" + End : "belum (butuh Level " + JobInfo.EvoLevel + ")") : JobInfo.EvoName(gs.evo)));
                        if (!string.IsNullOrEmpty(gs.evo)) sb.Append(Row("Evolusi Lv" + JobInfo.Evo2Level, string.IsNullOrEmpty(gs.evo2) ? (gs.CanEvolve2 ? Blue + "SIAP berevolusi! (tekan K)" + End : "belum (butuh Level " + JobInfo.Evo2Level + ")") : JobInfo.EvoName(gs.evo2)));
                    }
                    int act = 0, pas = 0;
                    foreach (var id in gs.learnedSkills) { var sd = SkillDB.Get(id); if (sd == null) continue; if (sd.active) act++; else pas++; }
                    sb.Append(Row("Skill dipelajari", act + " aktif, " + pas + " pasif"));
                    sb.Append(Row("Skill Point", gs.skillPoints.ToString()));
                    sb.Append("\n").Append(Gold).Append("Slot skill").Append(End).Append("\n");
                    for (int i = 0; i < gs.skillSlots.Length; i++)
                    {
                        var sd = SkillDB.Get(gs.skillSlots[i]);
                        sb.Append(Dim).Append("[").Append(SlotKeys[i]).Append("]").Append(End).Append("  ").Append(sd != null ? sd.name + (sd.tier >= 3 ? "  " + Blue + "Ultimate" + End : "") : "-").Append("\n");
                    }
                    sb.Append("\n").Append(Dim).Append("Tekan K untuk mempelajari dan memasang skill." + End);
                    return sb.ToString();
                },
                () => Nav(3));
        }

        // ------------------------------------------------------------------ 4 Catatan
        static void Records()
        {
            TownUI.I.Open("Catatan Perburuan",
                () =>
                {
                    var gs = GameState.I; var sb = new StringBuilder();
                    int kills = 0, gathers = 0;
                    foreach (var kv in gs.stats)
                    {
                        if (kv.Key.StartsWith("kill_") || kv.Key.StartsWith("kills_")) kills += kv.Value;
                        else if (kv.Key.StartsWith("gather_")) gathers += kv.Value;
                    }
                    sb.Append(Row("Monster dikalahkan", N(kills)));
                    sb.Append(Row("Boss dikalahkan", N(gs.Stat("boss_kills"))));
                    sb.Append(Row("Bahan dikumpulkan", N(gathers)));
                    sb.Append(Row("Pukulan latihan", N(gs.Stat("dummy_hits"))));
                    sb.Append(Row("Quest Guild selesai", gs.questsDone.ToString()));
                    sb.Append("\n").Append(Gold).Append("Menara Eldrath").Append(End).Append("\n");
                    sb.Append(Row("Lantai tertinggi", gs.dungeonBest > 0 ? gs.dungeonBest.ToString() : "-"));
                    sb.Append(Row("Boss terakhir dikalahkan", gs.dungeonBossCleared > 0 ? "Lantai " + gs.dungeonBossCleared : "-"));
                    sb.Append(Row("Jumlah masuk", gs.towerRuns.ToString()));
                    sb.Append("\n").Append(Gold).Append("Latihan Brann").Append(End).Append("\n");
                    int done = 0; foreach (var s in StarterQuests.All) if (gs.flags.Contains("stq_done_" + s.id)) done++;
                    sb.Append(Row("Latihan selesai", done + "/" + StarterQuests.All.Length));
                    return sb.ToString();
                },
                () => Nav(4));
        }
    }
}
