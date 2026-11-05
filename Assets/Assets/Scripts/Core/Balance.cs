using System;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n31: pusat angka keseimbangan + layar debug (Menu Jeda > Keseimbangan). Semua pengali bawaan = 1 (tidak mengubah angka lama),
    /// kecuali EXP perburuan (monster kini memberi sedikit EXP setelah Lv 5). Nilai disimpan di PlayerPrefs (per komputer, bukan per save)
    /// supaya bisa disetel saat bermain lalu dicatat ke angka tetap.
    /// </summary>
    public static class Balance
    {
        public class Knob { public string id, name, tip; public float def; public float[] steps; }

        public static readonly Knob[] Knobs =
        {
            new Knob { id = "monhp", name = "HP Monster", def = 1f, steps = new[] { 0.5f, 0.75f, 1f, 1.25f, 1.5f, 2f, 3f }, tip = "Pengali HP semua monster yang muncul berikutnya." },
            new Knob { id = "monatk", name = "Serangan Monster", def = 1f, steps = new[] { 0.5f, 0.75f, 1f, 1.25f, 1.5f, 2f }, tip = "Pengali damage monster ke pemain." },
            new Knob { id = "dmg", name = "Damage Pemain", def = 1f, steps = new[] { 0.75f, 1f, 1.25f, 1.5f, 2f }, tip = "Pengali semua damage serangan pemain." },
            new Knob { id = "exp", name = "Kurva EXP Level", def = 1f, steps = new[] { 0.5f, 0.75f, 1f, 1.5f, 2f, 3f }, tip = "Pengali EXP yang dibutuhkan tiap level (besar = lebih lama)." },
            new Knob { id = "kill", name = "EXP Perburuan", def = 1f, steps = new[] { 0f, 0.5f, 1f, 1.5f, 2f, 3f }, tip = "EXP dari membunuh monster setelah Lv 5 (0 = mati)." },
            new Knob { id = "gold", name = "Gold Hadiah", def = 1f, steps = new[] { 0.5f, 0.75f, 1f, 1.25f, 1.5f, 2f }, tip = "Pengali gold quest/Menara/cerita (lewat Difficulty.Gold)." },
            new Knob { id = "wexp", name = "EXP Senjata", def = 1f, steps = new[] { 0.5f, 1f, 1.5f, 2f, 3f, 5f }, tip = "Kecepatan level senjata (0-50)." },
            new Knob { id = "sp", name = "Biaya SP Status", def = 1f, steps = new[] { 0.5f, 1f, 1.5f, 2f }, tip = "Pengali biaya SP upgrade status." },
            new Knob { id = "evo", name = "Biaya Evolusi Senjata", def = 1f, steps = new[] { 0.5f, 1f, 1.5f, 2f, 3f }, tip = "Pengali gold/bahan evolusi senjata." },
        };

        static Dictionary<string, float> cache = new Dictionary<string, float>();

        public static float Get(string id)
        {
            float v; if (cache.TryGetValue(id, out v)) return v;
            float d = 1f; foreach (var k in Knobs) if (k.id == id) d = k.def;
            try { v = PlayerPrefs.GetFloat("hg_bal_" + id, d); } catch { v = d; }
            cache[id] = v; return v;
        }
        public static void Set(string id, float v) { cache[id] = v; try { PlayerPrefs.SetFloat("hg_bal_" + id, v); PlayerPrefs.Save(); } catch { } }
        public static void ResetAll() { foreach (var k in Knobs) Set(k.id, k.def); }

        public static float MonHp { get { return Get("monhp"); } }
        public static float MonAtk { get { return Get("monatk"); } }
        public static float PlayerDmg { get { return Get("dmg"); } }
        public static float ExpCurve { get { return Get("exp"); } }
        public static float KillRate { get { return Get("kill"); } }
        public static float GoldRate { get { return Get("gold"); } }
        public static float WeaponExp { get { return Get("wexp"); } }
        public static float SpCost { get { return Get("sp"); } }
        public static float EvoCost { get { return Get("evo"); } }

        /// <summary>EXP dari membunuh monster setelah Lv 5: akar HP x 0,25 (boss x2). Kecil dibanding quest, tapi berburu terasa maju.</summary>
        public static int KillExp(MonsterDef d, bool boss)
        {
            if (d == null || KillRate <= 0f) return 0;
            return Mathf.Max(1, Mathf.RoundToInt(Mathf.Sqrt(d.hp) * 0.25f * (boss ? 2f : 1f) * KillRate));
        }

        // ------------------------------------------------------------ layar debug
        public static void Open(Action back)
        {
            var ui = TownUI.I;
            ui.Open("Keseimbangan (Dev)",
                () => "Setel pengali lalu lihat tabel. Ketuk baris untuk menaikkan nilainya (berputar). Nilai tersimpan di komputer ini.\nCatat angka akhir yang enak dimainkan lalu kirim ke saya untuk dijadikan angka tetap.",
                () =>
                {
                    var l = new List<UIEntry>();
                    foreach (var kk in Knobs)
                    {
                        var k = kk;
                        l.Add(new UIEntry(k.name + ":  x" + Get(k.id).ToString("0.##") + "     " + k.tip, () => { Cycle(k); Open(back); }));
                    }
                    l.Add(new UIEntry("Tabel level & EXP >", () => LevelTable(back)));
                    l.Add(new UIEntry("Tabel monster: TTK & bahaya >", () => MonsterTable(back, 0)));
                    l.Add(new UIEntry("Tabel ekonomi & SP >", () => Economy(back)));
                    l.Add(new UIEntry("Kembalikan semua ke bawaan", () => { ResetAll(); Open(back); }));
                    l.Add(new UIEntry("< Kembali", () => { if (back != null) back(); else ui.Close(); }));
                    return l;
                });
        }

        static void Cycle(Knob k)
        {
            float cur = Get(k.id); int i = 0;
            for (int j = 0; j < k.steps.Length; j++) if (Mathf.Abs(k.steps[j] - cur) < 0.001f) i = j + 1;
            Set(k.id, k.steps[i % k.steps.Length]);
        }

        static void LevelTable(Action back)
        {
            TownUI.I.Open("Tabel Level & EXP",
                () =>
                {
                    var sb = new System.Text.StringBuilder("Lv   EXP ke berikutnya   Kumulatif   Tower: lantai bersih utk 1 level\n");
                    long cum = 0;
                    for (int lv = 1; lv < GameState.LevelCap; lv++)
                    {
                        int need = GameState.ExpToNext(lv); cum += need;
                        if (lv == 1 || lv % 5 == 0)
                        {
                            int fl = TowerDB.ClearExp(Mathf.Clamp(lv * 2, 1, 100));
                            sb.Append(lv.ToString().PadRight(5)).Append(need.ToString().PadRight(22)).Append(cum.ToString().PadRight(12)).Append(Mathf.CeilToInt(need / (float)Mathf.Max(1, fl))).Append(" lantai\n");
                        }
                    }
                    sb.Append("\nSP total sampai Lv 50: ").Append(GameState.LevelCap - 1).Append(" (1 per level) + poin atribut 2 per level (INT/WIS).");
                    return sb.ToString();
                },
                () => new List<UIEntry> { new UIEntry("< Kembali", () => Open(back)) });
        }

        static string[] Regions = { "HuntArea_G", "HuntArea_Desert", "HuntArea_Swamp", "HuntArea_Frontier", "HuntArea_Volcano", "HuntArea_Bone", "HuntArea_Demon" };

        static void MonsterTable(Action back, int page)
        {
            TownUI.I.Open("Tabel Monster",
                () =>
                {
                    var gs = GameState.I; var pc = PlayerCombat.I;
                    float hit = (10f + gs.TotalAtk) * gs.DmgMult * PlayerDmg;
                    float dps = hit * 1.4f;
                    float hp = pc != null ? pc.MaxHp : 100f;
                    var sb = new System.Text.StringBuilder();
                    sb.Append("Kamu: ATK ").Append(gs.TotalAtk).Append(", damage/pukulan ~").Append(Mathf.RoundToInt(hit)).Append(", DPS ~").Append(Mathf.RoundToInt(dps)).Append(", HP ").Append(Mathf.RoundToInt(hp)).Append(", DEF ").Append(gs.TotalDef).Append("\n");
                    sb.Append("TTK = detik untuk membunuh satu monster (perkiraan, serangan dasar). KO = jumlah serangan monster sampai kamu pingsan (tanpa DEF).\n\n");
                    var all = MonsterDB.All(); var shown = new List<MonsterDef>();
                    foreach (var d in all) if (d != null && !d.passive && d.id != "dummy" && !d.id.StartsWith("tbs_") && !d.id.StartsWith("hb_") && !d.id.StartsWith("srank_")) shown.Add(d);
                    shown.Sort((a, b) => a.hp.CompareTo(b.hp));
                    int per = 14; int pages = Mathf.Max(1, Mathf.CeilToInt(shown.Count / (float)per)); page = Mathf.Clamp(page, 0, pages - 1);
                    for (int i = page * per; i < Mathf.Min(shown.Count, (page + 1) * per); i++)
                    {
                        var d = shown[i];
                        float mhp = d.hp * Difficulty.HpMul * MonHp, matk = d.atk * Difficulty.AtkMul * MonAtk;
                        sb.Append(d.name.PadRight(34)).Append("HP ").Append(Mathf.RoundToInt(mhp).ToString().PadRight(7)).Append("ATK ").Append(Mathf.RoundToInt(matk).ToString().PadRight(5))
                          .Append("TTK ").Append((mhp / Mathf.Max(1f, dps)).ToString("0.0").PadRight(6)).Append("KO ").Append(Mathf.CeilToInt(hp / Mathf.Max(1f, matk))).Append("x   EXP+").Append(Mathf.Max(0, KillExp(d, d.large && d.carves >= 3 && d.rareChance >= 0.3f))).Append("\n");
                    }
                    sb.Append("\nHalaman ").Append(page + 1).Append("/").Append(pages);
                    return sb.ToString();
                },
                () =>
                {
                    var l = new List<UIEntry>();
                    int cnt = 0; foreach (var d in MonsterDB.All()) if (d != null && !d.passive && d.id != "dummy" && !d.id.StartsWith("tbs_") && !d.id.StartsWith("hb_") && !d.id.StartsWith("srank_")) cnt++;
                    int pages = Mathf.Max(1, Mathf.CeilToInt(cnt / 14f));
                    l.Add(new UIEntry("Halaman berikutnya >", () => MonsterTable(back, (page + 1) % pages)));
                    l.Add(new UIEntry("< Kembali", () => Open(back)));
                    return l;
                });
        }

        static void Economy(Action back)
        {
            TownUI.I.Open("Tabel Ekonomi & SP",
                () =>
                {
                    var sb = new System.Text.StringBuilder();
                    int sp = 0; foreach (var s in StatUpgrade.All) for (int r = 0; r < s.max; r++) sp += StatUpgrade.CostFor(r);
                    sb.Append("Biaya SP untuk MENAMATKAN semua stat: ").Append(sp).Append(" SP (x").Append(SpCost.ToString("0.##")).Append(" = ").Append(Mathf.CeilToInt(sp * SpCost)).Append(") vs SP dari level: ").Append(GameState.LevelCap - 1).Append(" + dari reset/item.\n");
                    long wexp = 0; for (int lv = 0; lv < 50; lv++) wexp += WeaponLevel.ExpNeeded(lv);
                    sb.Append("EXP senjata sampai Lv 50: ").Append(wexp).Append(" (per kill biasa ~").Append(Mathf.RoundToInt(300f / 12f)).Append(", boss x3).\n");
                    int evoGold = 0; for (int e = 0; e < 4; e++) evoGold += 400 * (e + 1) * (e + 1);
                    sb.Append("Total gold 4 evolusi senjata: ").Append(Mathf.RoundToInt(evoGold * EvoCost)).Append("\n\n");
                    sb.Append("Gold bersih dari Menara (sebelum pengali kesulitan):\n");
                    foreach (int f in new[] { 10, 25, 50, 75, 100 }) sb.Append("  lantai ").Append(f).Append(": ").Append(Difficulty.Gold(TowerDB.ClearGold(f))).Append("g, ").Append(TowerDB.ClearExp(f)).Append(" EXP\n");
                    sb.Append("\nPengali aktif: Gold x").Append(Difficulty.GoldMul.ToString("0.##")).Append(" (kesulitan) x").Append(GoldRate.ToString("0.##")).Append(" (Keseimbangan)");
                    return sb.ToString();
                },
                () => new List<UIEntry> { new UIEntry("< Kembali", () => Open(back)) });
        }
    }
}
