using System;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n31: kedalaman build.
    /// 1) Set bonus gear: pakai 2 / 4 bagian dari satu set untuk bonus (zirah area, gear boss rahasia).
    /// 2) Kartu Monster: kartu jatuh dari monster (disimpan permanen di album; kartu ganda menaikkan level kartu s/d 5). Pasang maks 3 kartu:
    ///    +6% damage ke monster itu per level dan -4% damage yang diterima darinya per level.
    /// 3) Loadout: simpan 3 susunan (gear terpasang + 7 slot skill) dan pasang kembali dengan satu tombol (gear harus ada di tas / sedang terpasang).
    /// Semua data disimpan di gs.stats / gs.flags (ikut save).
    /// </summary>
    public static class GearSets
    {
        public class SetDef { public string id, name; public string[] pieces; public Bonus two, four; }
        public class Bonus { public int atk, def; public string eff = ""; public float effVal; public string Text() { var l = new List<string>(); if (atk > 0) l.Add("ATK +" + atk); if (def > 0) l.Add("DEF +" + def); if (eff != "") l.Add(eff + " +" + effVal); return string.Join(", ", l.ToArray()); } }

        public static readonly List<SetDef> All = new List<SetDef>();

        static GearSets()
        {
            string[] nm = { "Perbatasan", "Bara", "Tulang Kuno", "Iblis" };
            string[] e4 = { "lifesteal", "crit", "cdr", "crit" };
            float[] v4 = { 3f, 6f, 6f, 9f };
            string[] sl = { "Body", "Head", "Legs", "Hands" };
            for (int a = 0; a < 4; a++)
            {
                var p = new string[4]; for (int i = 0; i < 4; i++) p[i] = "ga_" + a + "_" + sl[i];
                All.Add(new SetDef { id = "area" + a, name = "Set " + nm[a], pieces = p, two = new Bonus { def = 6 * (a + 1) }, four = new Bonus { atk = 5 * (a + 1), eff = e4[a], effVal = v4[a] } });
            }
            var h = new string[HiddenBosses.Defs.Length]; for (int i = 0; i < h.Length; i++) h[i] = HiddenBosses.Defs[i].GearId;
            All.Add(new SetDef { id = "hidden", name = "Set Pemburu Rahasia", pieces = h, two = new Bonus { eff = "crit", effVal = 4f }, four = new Bonus { def = 10, eff = "lifesteal", effVal = 4f } });
        }

        public static int Count(GameState gs, SetDef s)
        {
            int n = 0; var seenSlots = new HashSet<string>();
            foreach (var kv in gs.equipment)
            {
                if (Array.IndexOf(s.pieces, kv.Value) < 0) continue;
                if (seenSlots.Add(kv.Key)) n++;
            }
            return n;
        }

        static IEnumerable<Bonus> Active(GameState gs)
        {
            foreach (var s in All) { int n = Count(gs, s); if (n >= 2) yield return s.two; if (n >= 4) yield return s.four; }
        }
        public static int Atk(GameState gs) { int t = 0; foreach (var b in Active(gs)) t += b.atk; return t; }
        public static int Def(GameState gs) { int t = 0; foreach (var b in Active(gs)) t += b.def; return t; }
        public static float Effect(GameState gs, string eff) { float t = 0f; foreach (var b in Active(gs)) if (b.eff == eff) t += b.effVal; return t; }

        public static string Describe(GameState gs)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var s in All)
            {
                int n = Count(gs, s);
                sb.Append(s.name).Append("  (").Append(n).Append("/").Append(s.pieces.Length >= 4 ? 4 : s.pieces.Length).Append(")\n");
                sb.Append(n >= 2 ? "  [x] " : "  [ ] ").Append("2 bagian: ").Append(s.two.Text()).Append("\n");
                sb.Append(n >= 4 ? "  [x] " : "  [ ] ").Append("4 bagian: ").Append(s.four.Text()).Append("\n");
            }
            return sb.ToString();
        }
    }

    public static class Cards
    {
        public const int Slots = 3, MaxLv = 5;

        public static bool Eligible(MonsterDef d) { return d != null && !d.passive && d.id != "dummy" && !d.id.StartsWith("hb_") && !d.id.StartsWith("srank_"); }
        public static int Level(GameState gs, string id) { return Mathf.Clamp(gs.Stat("card_" + id), 0, MaxLv); }
        public static bool Equipped(GameState gs, string id) { return gs.flags.Contains("cardeq:" + id); }
        public static int EquippedCount(GameState gs) { int n = 0; foreach (var f in gs.flags) if (f.StartsWith("cardeq:")) n++; return n; }

        public static float DealtMul(string id)
        {
            var gs = GameState.I; if (gs == null || id == null || !Equipped(gs, id)) return 1f;
            return 1f + 0.06f * Level(gs, id);
        }
        public static float TakenMul(string id)
        {
            var gs = GameState.I; if (gs == null || id == null || !Equipped(gs, id)) return 1f;
            return 1f - 0.04f * Level(gs, id);
        }

        /// <summary>Dipanggil saat monster mati.</summary>
        public static void OnKill(MonsterDef d, bool boss)
        {
            var gs = GameState.I; if (gs == null || !Eligible(d)) return;
            float ch = boss ? 0.12f : d.large ? 0.04f : 0.02f;
            if (UnityEngine.Random.value > ch) return;
            int lv = Level(gs, d.id);
            if (lv >= MaxLv) return;
            gs.stats["card_" + d.id] = lv + 1;
            if (TownUI.I != null) TownUI.I.Toast(lv == 0 ? "KARTU BARU: " + d.name + "!" : "Kartu " + d.name + " naik ke Lv " + (lv + 1) + "!");
            Audio.Sfx("quest_reward");
        }

        public static void Open(Action back)
        {
            TownUI.I.Open("Kartu Monster",
                () =>
                {
                    var gs = GameState.I; int own = 0, tot = 0;
                    foreach (var d in MonsterDB.All()) if (Eligible(d)) { tot++; if (Level(gs, d.id) > 0) own++; }
                    return "Album: " + own + "/" + tot + " kartu.  Terpasang " + EquippedCount(gs) + "/" + Slots + ".\nSetiap level: +6% damage ke monster itu, -4% damage yang diterima darinya. Kartu jatuh dari monster (boss paling sering); kartu ganda menaikkan level (maks " + MaxLv + ").";
                },
                () =>
                {
                    var gs = GameState.I; var l = new List<UIEntry>();
                    var owned = new List<MonsterDef>();
                    foreach (var d in MonsterDB.All()) if (Eligible(d) && Level(gs, d.id) > 0) owned.Add(d);
                    owned.Sort((a, b) => Equipped(gs, b.id).CompareTo(Equipped(gs, a.id)));
                    if (owned.Count == 0) l.Add(new UIEntry("Belum punya kartu. Kalahkan monster untuk mendapatkannya.", () => { }, false));
                    foreach (var dd in owned)
                    {
                        var d = dd; bool eq = Equipped(gs, d.id); int lv = Level(gs, d.id);
                        l.Add(new UIEntry((eq ? "[Terpasang] " : "") + "Kartu " + d.name + "  Lv " + lv + "   (+" + 6 * lv + "% dmg, -" + 4 * lv + "% diterima)", () =>
                        {
                            var g = GameState.I;
                            if (Equipped(g, d.id)) g.flags.Remove("cardeq:" + d.id);
                            else if (EquippedCount(g) < Slots) g.flags.Add("cardeq:" + d.id);
                            else TownUI.I.Toast("Slot kartu penuh. Lepas salah satu dulu.");
                            Open(back);
                        }));
                    }
                    l.Add(new UIEntry("< Kembali", () => { if (back != null) back(); else TownUI.I.Close(); }));
                    return l;
                });
        }
    }

    public static class Loadouts
    {
        public const int Slots = 3;
        static string P(int n) { return "lo" + n + ":"; }
        public static bool Has(GameState gs, int n) { foreach (var f in gs.flags) if (f.StartsWith(P(n))) return true; return false; }

        public static void Save(GameState gs, int n)
        {
            gs.flags.RemoveWhere(f => f.StartsWith(P(n)));
            foreach (var kv in gs.equipment) gs.flags.Add(P(n) + "G|" + kv.Key + "|" + kv.Value);
            for (int i = 0; i < gs.skillSlots.Length; i++) if (!string.IsNullOrEmpty(gs.skillSlots[i])) gs.flags.Add(P(n) + "S|" + i + "|" + gs.skillSlots[i]);
        }

        public static string Apply(GameState gs, int n)
        {
            var missing = new List<string>(); int changed = 0;
            foreach (var f in new List<string>(gs.flags))
            {
                if (!f.StartsWith(P(n))) continue;
                var p = f.Substring(P(n).Length).Split('|');
                if (p.Length < 3) continue;
                if (p[0] == "G")
                {
                    string cur; if (gs.equipment.TryGetValue(p[1], out cur) && cur == p[2]) continue;
                    if (gs.Count(p[2]) > 0 && gs.EquipFromInventory(p[2])) changed++;
                    else { var d = ItemDB.Get(p[2]); missing.Add(d != null ? d.name : p[2]); }
                }
                else if (p[0] == "S")
                {
                    int slot; if (!int.TryParse(p[1], out slot) || slot < 0 || slot >= gs.skillSlots.Length) continue;
                    if (gs.skillSlots[slot] != p[2] && gs.learnedSkills.Contains(p[2])) { gs.AssignSkill(slot, p[2]); changed++; }
                }
            }
            return changed + " perubahan dipasang." + (missing.Count > 0 ? " Tidak ditemukan di tas: " + string.Join(", ", missing.ToArray()) + "." : "");
        }

        public static void Open(Action back)
        {
            TownUI.I.Open("Loadout",
                () => "Simpan susunan gear + skill, lalu pasang lagi dengan satu tombol. Gear harus ada di tas atau sedang terpasang. Tidak bisa dipakai saat bertarung.",
                () =>
                {
                    var gs = GameState.I; var l = new List<UIEntry>();
                    bool fight = PlayerController.InFight();
                    for (int i = 1; i <= Slots; i++)
                    {
                        int n = i; bool has = Has(gs, n);
                        l.Add(new UIEntry("Loadout " + n + (has ? ": terisi" : ": kosong") + " - simpan susunan sekarang", () => { Save(GameState.I, n); TownUI.I.Toast("Loadout " + n + " disimpan."); Open(back); }));
                        l.Add(new UIEntry("Loadout " + n + " - pasang", () => { string r = Apply(GameState.I, n); TownUI.I.Toast(r); Open(back); }, has && !fight));
                    }
                    l.Add(new UIEntry("< Kembali", () => { if (back != null) back(); else TownUI.I.Close(); }));
                    return l;
                });
        }
    }

    public static class BuildMenu
    {
        public static void Open(Action back)
        {
            TownUI.I.Open("Build",
                () => "Set bonus gear, Kartu Monster, dan Loadout.\n\n" + GearSets.Describe(GameState.I),
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Kartu Monster (" + Cards.EquippedCount(GameState.I) + "/" + Cards.Slots + " terpasang) >", () => Cards.Open(() => Open(back))));
                    l.Add(new UIEntry("Loadout (simpan/pasang susunan) >", () => Loadouts.Open(() => Open(back))));
                    l.Add(new UIEntry("< Kembali", () => { if (back != null) back(); else TownUI.I.Close(); }));
                    return l;
                });
        }
    }
}
