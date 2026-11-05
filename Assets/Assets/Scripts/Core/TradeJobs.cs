using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n31: aktivitas di luar berburu, dibuka dari Papan Pengumuman Guild.
    /// Pasar Dagang: barang dagangan (6 jenis) punya harga berbeda tiap kota dan berubah tiap hari. Beli murah di satu kota, jual mahal di kota lain.
    /// Harga kota lain yang pernah kamu lihat diingat (info pasar). Kerja Sampingan: pekerjaan harian dengan waktu in-game (gaji kecil, aman).
    /// </summary>
    public static class TradeMarket
    {
        public class Good { public string id, name; public int basePrice; }
        public static readonly Good[] Goods =
        {
            new Good { id = "trade_grain", name = "Karung Gandum", basePrice = 20 },
            new Good { id = "trade_cloth", name = "Kain Sutra", basePrice = 60 },
            new Good { id = "trade_ore", name = "Ingot Dagang", basePrice = 45 },
            new Good { id = "trade_spice", name = "Rempah Langka", basePrice = 90 },
            new Good { id = "trade_wine", name = "Anggur Tua", basePrice = 120 },
            new Good { id = "trade_gem", name = "Batu Hias", basePrice = 200 },
        };
        static readonly string[] Towns = { "StartVillage", "Town", "DungeonCity" };
        static readonly string[] TownNames = { "Desa Pemula", "Ashwatch", "Eldrath" };

        public static void Register()
        {
            foreach (var g in Goods) ItemDB.AddRaw(new ItemDef { id = g.id, name = g.name, type = ItemType.Material, sellPrice = Mathf.RoundToInt(g.basePrice * 0.6f), stack = 20 });
        }

        static int H(string s) { int h = 17; foreach (char c in s) h = unchecked(h * 31 + c); return Mathf.Abs(h); }

        static string CurTown() { string sc = SceneManager.GetActiveScene().name; return Array.IndexOf(Towns, sc) >= 0 ? sc : "Town"; }

        /// <summary>Harga tengah barang di kota pada hari tertentu (sebelum selisih beli/jual).</summary>
        public static int MidPrice(Good g, string town, int day)
        {
            float f = 0.72f + (H(town + g.id) % 66) / 100f;                 // 0.72 .. 1.37 per kota & barang
            float wob = ((H(g.id + town) / 7 + day * 5) % 25 - 12) / 100f;   // +-12% berubah tiap hari
            return Mathf.Max(1, Mathf.RoundToInt(g.basePrice * f * (1f + wob)));
        }
        static int BuyPrice(Good g, string t, int day) { return Mathf.Max(1, Mathf.RoundToInt(MidPrice(g, t, day) * 1.06f)); }
        static int SellPrice(Good g, string t, int day) { return Mathf.Max(1, Mathf.RoundToInt(MidPrice(g, t, day) * 0.94f)); }

        static void Remember(GameState gs, string town)
        {
            foreach (var g in Goods) { gs.stats["tp_" + town + "_" + g.id] = MidPrice(g, town, gs.day); gs.stats["tpd_" + town] = gs.day; }
        }

        public static void Open(Action back)
        {
            var gs = GameState.I; string town = CurTown(); Remember(gs, town);
            TownUI.I.Open("Pasar Dagang - " + TownNames[Array.IndexOf(Towns, town)],
                () => "Harga hari ke-" + gs.day + ". Beli murah di satu kota, jual mahal di kota lain. Gold: " + gs.gold + "\nKeuntungan dagang total: " + gs.Stat("trade_profit") + "g",
                () =>
                {
                    var l = new List<UIEntry>();
                    foreach (var gg in Goods)
                    {
                        var g = gg; int buy = BuyPrice(g, town, gs.day), sell = SellPrice(g, town, gs.day), own = gs.Count(g.id);
                        l.Add(new UIEntry(g.name + "   punya " + own + "   beli " + buy + "g / jual " + sell + "g" + OtherTowns(gs, g, town),
                            () =>
                            {
                                var s = GameState.I;
                                if (!s.HasSpace(g.id, 1)) { TownUI.I.Toast("Tas penuh."); return; }
                                if (!s.TrySpend(buy)) return;
                                s.AddItem(g.id, 1); s.stats["tc_" + g.id] = s.Stat("tc_" + g.id) + buy; Open(back);
                            }, gs.gold >= buy));
                        if (own > 0) l.Add(new UIEntry("   Jual 1 " + g.name + " (" + sell + "g)", () =>
                        {
                            var s = GameState.I; int c = s.Stat("tc_" + g.id), n = s.Count(g.id);
                            int avg = n > 0 ? c / n : 0;
                            if (!s.RemoveItem(g.id, 1)) return;
                            s.gold += sell; s.stats["tc_" + g.id] = Mathf.Max(0, c - avg);
                            int prof = sell - avg; if (prof > 0) s.AddStat("trade_profit", prof);
                            Audio.Sfx("coin"); Open(back);
                        }));
                    }
                    l.Add(new UIEntry("< Kembali", () => { if (back != null) back(); else TownUI.I.Close(); }));
                    return l;
                });
        }

        static string OtherTowns(GameState gs, Good g, string cur)
        {
            var sb = new System.Text.StringBuilder("   (");
            bool any = false;
            for (int i = 0; i < Towns.Length; i++)
            {
                if (Towns[i] == cur) continue;
                int p = gs.Stat("tp_" + Towns[i] + "_" + g.id);
                sb.Append(any ? ", " : "").Append(TownNames[i]).Append(" ").Append(p > 0 ? "~" + p + "g" : "?"); any = true;
            }
            return sb.Append(")").ToString();
        }
    }

    public static class OddJobs
    {
        class Job { public string id, name, desc; public float hours; public int baseGold, perLevel, exp; public string drop; public float dropChance; }
        static readonly Job[] Jobs =
        {
            new Job { id = "porter", name = "Angkut Karung di Gudang Guild", desc = "2 jam. Gaji kecil, tidak ada risiko.", hours = 2f, baseGold = 28, perLevel = 2, exp = 3 },
            new Job { id = "gate", name = "Jaga Gerbang Kota", desc = "4 jam. Gaji menengah dan sedikit EXP.", hours = 4f, baseGold = 52, perLevel = 3, exp = 7 },
            new Job { id = "smith", name = "Bantu Pandai Besi", desc = "3 jam. Gaji + peluang bijih besi.", hours = 3f, baseGold = 40, perLevel = 2, exp = 4, drop = "iron_ore", dropChance = 0.5f },
            new Job { id = "herb", name = "Petik Tanaman Obat untuk Tabib", desc = "3 jam. Gaji + peluang herba.", hours = 3f, baseGold = 36, perLevel = 2, exp = 4, drop = "herb", dropChance = 0.6f },
        };
        const int DailyMax = 3;

        static int Today(GameState gs) { return gs.Stat("job_day") == gs.day ? gs.Stat("job_n") : 0; }

        public static void Open(Action back)
        {
            TownUI.I.Open("Kerja Sampingan",
                () => { var gs = GameState.I; return "Pekerjaan harian di kota (waktu berjalan, lapar & haus berkurang). Maks " + DailyMax + " pekerjaan per hari, hanya jam 06.00-20.00.\nHari ini: " + Today(gs) + "/" + DailyMax + "   Total pekerjaan: " + gs.Stat("jobs_done"); },
                () =>
                {
                    var gs = GameState.I; var l = new List<UIEntry>();
                    string why = null;
                    if (Today(gs) >= DailyMax) why = "Sudah " + DailyMax + " pekerjaan hari ini.";
                    else if (gs.timeOfDay < 6f || gs.timeOfDay > 20f) why = "Hanya jam 06.00-20.00.";
                    else if (gs.IsFatigued) why = "Kamu terlalu lelah. Tidur dulu.";
                    foreach (var jj in Jobs)
                    {
                        var j = jj; int pay = Difficulty.Gold(j.baseGold + j.perLevel * gs.level);
                        bool ok = why == null && gs.timeOfDay + j.hours <= 22f;
                        l.Add(new UIEntry(j.name + "   +" + pay + "g\n" + j.desc, () => Do(j, pay, back), ok));
                    }
                    if (why != null) l.Add(new UIEntry(why, () => { }, false));
                    l.Add(new UIEntry("< Kembali", () => { if (back != null) back(); else TownUI.I.Close(); }));
                    return l;
                });
        }

        static void Do(Job j, int pay, Action back)
        {
            var gs = GameState.I;
            gs.Advance(j.hours, 1.2f, true);
            gs.gold += pay; if (j.exp > 0) gs.AddExp(j.exp);
            gs.stats["job_n"] = Today(gs) + 1; gs.stats["job_day"] = gs.day; gs.AddStat("jobs_done");
            string extra = "";
            if (!string.IsNullOrEmpty(j.drop) && UnityEngine.Random.value < j.dropChance && gs.HasSpace(j.drop, 1)) { gs.AddItem(j.drop, 1); var d = ItemDB.Get(j.drop); extra = " + " + (d != null ? d.name : j.drop); }
            Audio.Sfx("quest_reward");
            TownUI.I.Toast(j.name + " selesai. +" + pay + "g" + extra);
            Open(back);
        }
    }
}
