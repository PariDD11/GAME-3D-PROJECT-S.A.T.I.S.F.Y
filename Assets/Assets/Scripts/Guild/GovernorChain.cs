using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
/// <summary>
/// Rantai quest politik Ashwatch: Gubernur Roderic Hale vs Guild. Tiap tahap punya dua pilihan (Pihak Gubernur / Pihak Guild).
/// Hasil akhir mengubah harga toko (beli/jual) dan reputasi Guild. Diakses lewat Guild Master.
/// Flag: gov_done_N, gov_pick_N (g/u), gov_side_gov / gov_side_guild / gov_side_neutral.
/// </summary>
public static class GovernorChain
{
    class Stage
    {
        public string title, intro, needTxt, itemId; public int itemN, gold; public string govTxt, guildTxt, hook; public int minRank;
    }

    static readonly Stage[] S =
    {
        new Stage { title = "1. Pajak Perbatasan", minRank = 1, gold = 400,
            intro = "Gubernur Roderic Hale: \"Guild memungut untung dari setiap monster di tanah yang kujaga. Aku minta sumbangan 400 gold sebagai tanda kau menghormati pemerintah kota.\"",
            needTxt = "400 gold", govTxt = "Bayar pajak ke Gubernur", guildTxt = "Tolak dan lapor ke Orwen" },
        new Stage { title = "2. Bukti dari Hutan", minRank = 2, itemId = "ogre_tusk", itemN = 3,
            intro = "Gubernur: \"Pengawal kota kehilangan kontrol di jalan raya. Bawakan 3 Taring Ogre sebagai bukti Guild tidak becus menjaga Hutan Perbatasan. Atau, jika kau setia pada Guild, antar ke Orwen sebagai bukti kita bekerja sama.\"",
            needTxt = "3 Taring Ogre", govTxt = "Serahkan ke Gubernur", guildTxt = "Serahkan ke Orwen" },
        new Stage { title = "3. Bayang Hollow Circle", minRank = 3, itemId = "demon_horn", itemN = 2,
            intro = "Seorang pengawal berbisik: kultus pemuja Lucifer bernama Hollow Circle punya pemasok di kota ini. Gubernur menuduh Guild. Orwen menuduh Gubernur. Bawakan 2 Tanduk Iblis sebagai bukti: kau yang memutuskan siapa yang menerimanya.",
            needTxt = "2 Tanduk Iblis", govTxt = "Beri bukti ke Gubernur", guildTxt = "Beri bukti ke Orwen" },
        new Stage { title = "4. Keputusan Akhir", minRank = 4, gold = 0,
            intro = "Kedua pihak menunggu jawabanmu. Pilih sisi, atau tetap netral dan jaga jarak dari keduanya.",
            needTxt = "-", govTxt = "Berpihak pada Gubernur", guildTxt = "Berpihak pada Guild" },
    };

    public static bool Done(int i) { var g = GameState.I; return g != null && g.flags.Contains("gov_done_" + i); }
    public static int Step { get { int n = 0; while (n < S.Length && Done(n)) n++; return n; } }
    static bool Has(string f) { var g = GameState.I; return g != null && g.flags.Contains(f); }
    public static bool GovSide { get { return Has("gov_side_gov"); } }
    public static bool GuildSide { get { return Has("gov_side_guild"); } }

    /// <summary>Pengali harga beli di toko (Gubernur memberi diskon).</summary>
    public static float BuyMul { get { return GovSide ? 0.92f : 1f; } }
    /// <summary>Pengali harga jual (pihak Guild menaikkan harga jual).</summary>
    public static float SellMul { get { return GuildSide ? 1.08f : GovSide ? 0.97f : 1f; } }

    static int Favor(char c) { int n = 0; for (int i = 0; i < S.Length; i++) if (Has("gov_pick_" + i + c)) n++; return n; }

    public static void Open(System.Action back)
    {
        var gs = GameState.I;
        TownUI.I.Open("Politik Ashwatch: Gubernur vs Guild",
            () =>
            {
                int st = Step;
                string h = "<b>Gubernur Roderic Hale</b> dan Guild berebut pengaruh atas Ashwatch.\n";
                h += "Kemajuan: " + st + "/" + S.Length + "   Dukungan Gubernur: " + Favor('g') + "   Dukungan Guild: " + Favor('u') + "\n";
                if (GovSide) h += "<color=#ffd070>Kau berpihak pada Gubernur: harga beli -8%, harga jual -3%.</color>";
                else if (GuildSide) h += "<color=#8fd0ff>Kau berpihak pada Guild: harga jual +8%, reputasi Guild naik.</color>";
                else if (st >= S.Length) h += "Kau netral. Tidak ada pihak yang berutang padamu.";
                return h;
            },
            () =>
            {
                var l = new List<UIEntry>();
                int st = Step;
                if (st >= S.Length) { l.Add(new UIEntry("Semua tahap selesai.", null, false)); }
                else
                {
                    var s = S[st];
                    int ri = GameState.RankIndex(gs.rank);
                    l.Add(new UIEntry("Tahap " + s.title + " >", () => OpenStage(st, back), ri >= s.minRank));
                    if (ri < s.minRank) l.Add(new UIEntry("<color=#c08080>Butuh rank lebih tinggi.</color>", null, false));
                }
                if (Has("lore_hollow_circle") || st >= 3) l.Add(new UIEntry("Baca: Hollow Circle >", () => LoreCodex.Read("hollow_circle", 0, () => Open(back))));
                l.Add(new UIEntry("< Kembali", back));
                return l;
            });
    }

    static void OpenStage(int i, System.Action back)
    {
        var s = S[i];
        var gs = GameState.I;
        TownUI.I.Open(s.title,
            () => s.intro + "\n\nSyarat: " + s.needTxt,
            () =>
            {
                var l = new List<UIEntry>();
                if (i == 3)
                {
                    l.Add(new UIEntry(s.govTxt, () => Finish(i, 'g', back)));
                    l.Add(new UIEntry(s.guildTxt, () => Finish(i, 'u', back)));
                    l.Add(new UIEntry("Tetap netral", () => Finish(i, 'n', back)));
                }
                else
                {
                    bool ok = Can(s);
                    l.Add(new UIEntry(s.govTxt, () => { if (Pay(s)) Finish(i, 'g', back); }, ok));
                    l.Add(new UIEntry(s.guildTxt, () => { if (Pay(s)) Finish(i, 'u', back); }, ok));
                    if (!ok) l.Add(new UIEntry("<color=#c08080>Kamu belum punya: " + s.needTxt + "</color>", null, false));
                }
                l.Add(new UIEntry("< Kembali", () => Open(back)));
                return l;
            });
    }

    static bool Can(Stage s)
    {
        var gs = GameState.I;
        if (s.gold > 0 && gs.gold < s.gold) return false;
        if (s.itemId != null && gs.Count(s.itemId) < s.itemN) return false;
        return true;
    }

    static bool Pay(Stage s)
    {
        var gs = GameState.I;
        if (!Can(s)) return false;
        if (s.gold > 0) gs.gold -= s.gold;
        if (s.itemId != null) gs.RemoveItem(s.itemId, s.itemN);
        return true;
    }

    static void Finish(int i, char pick, System.Action back)
    {
        var gs = GameState.I;
        gs.flags.Add("gov_done_" + i);
        gs.flags.Add("gov_pick_" + i + pick);
        var gd = GuildData.I;
        if (pick == 'g') { gs.AddExp(120 * (i + 1)); GameState.Say("Gubernur puas. (+EXP)"); if (gd != null) gd.AddReputation(-5); }
        else if (pick == 'u') { gs.gold += 150 * (i + 1); GameState.Say("Orwen berterima kasih. (+Gold, +Reputasi)"); if (gd != null) gd.AddReputation(10 + 5 * i); }
        else GameState.Say("Kau memilih netral.");
        if (i == 2) LoreDB.Unlock("hollow_circle");
        if (i == 3)
        {
            int g = Favor('g'), u = Favor('u');
            if (pick == 'g' || (pick == 'n' && g > u)) { if (pick == 'g') gs.flags.Add("gov_side_gov"); else gs.flags.Add("gov_side_neutral"); }
            if (pick == 'u') gs.flags.Add("gov_side_guild");
            if (pick == 'n') gs.flags.Add("gov_side_neutral");
        }
        Open(back);
    }
}
}
