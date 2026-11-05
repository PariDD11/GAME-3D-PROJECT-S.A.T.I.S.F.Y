using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Quest gathering dari NPC di luar Guild (penjual/warga khusus): kumpulkan N bahan lalu serahkan ke NPC yang sama.
    /// Status disimpan di GameState.stats per nama NPC (shq_*). Satu quest per NPC per hari; isi bergilir tiap selesai.
    /// Tambah daftar baru di tabel Pool di bawah.
    /// </summary>
    public static class ShopQuests
    {
        class Req { public string item; public int count, gold, exp; public string ask; }

        static readonly Req[] Pool =
        {
            new Req { item = "herb",         count = 5, gold = 80,  exp = 20, ask = "Bisakah kamu mengumpulkan {n} {item}? Barang itu penting untuk persediaan kami." },
            new Req { item = "wolf_pelt",    count = 3, gold = 120, exp = 30, ask = "Aku butuh {n} {item} untuk dagangan musim ini. Bisa tolong carikan?" },
            new Req { item = "boar_tusk",    count = 3, gold = 130, exp = 32, ask = "Pesanan menumpuk. Bawakan aku {n} {item}, ya." },
            new Req { item = "monster_bone", count = 6, gold = 90,  exp = 24, ask = "Persediaan kami habis. Tolong kumpulkan {n} {item}." },
            new Req { item = "raw_chicken",  count = 4, gold = 90,  exp = 22, ask = "Menu hari ini butuh {n} {item}. Bisa tolong buruan ayam untukku?" },
            new Req { item = "raw_mutton",   count = 3, gold = 110, exp = 26, ask = "Pelanggan minta sup domba. Bawakan aku {n} {item}, ya." },
            new Req { item = "raw_beef",     count = 2, gold = 120, exp = 28, ask = "Stok daging kami habis. Carikan {n} {item} dari ternak di luar kota." },
            new Req { item = "raw_goat",     count = 3, gold = 105, exp = 25, ask = "Aku mau masak sate. Tolong dapatkan {n} {item}." },
            new Req { item = "fish_small",   count = 4, gold = 70,  exp = 18, ask = "Pelanggan minta ikan segar. Pancingkan {n} {item} untukku, ya." },
            new Req { item = "fish_carp",    count = 3, gold = 110, exp = 26, ask = "Aku mau masak sup ikan. Tolong pancingkan {n} {item}." },
            new Req { item = "fish_trout",   count = 2, gold = 160, exp = 36, ask = "Koki kami butuh {n} {item} untuk menu spesial. Ikan itu hanya ada di air jernih." },
            new Req { item = "fish_golden",  count = 1, gold = 400, exp = 90, ask = "Konon ada {item} di perairan sekitar sini. Kalau kamu dapat, aku beli mahal!" },
            new Req { item = "fish_soup",    count = 2, gold = 140, exp = 30, ask = "Aku lapar sekali. Masakkan {n} {item} untukku (bahan: ikan mas + herb)." },
            new Req { item = "iron_ore",     count = 5, gold = 100, exp = 24, ask = "Tungku kami kehabisan bahan. Tambangkan {n} {item} dari area berburu." },
            new Req { item = "gem_ruby",     count = 1, gold = 250, exp = 55, ask = "Seorang pelanggan mencari {item}. Kalau kamu menemukannya di tambang, aku bayar mahal." },
            new Req { item = "carrot",       count = 4, gold = 70,  exp = 18, ask = "Dapur kami butuh {n} {item}. Kamu bisa menanamnya di lahan desa." },
            new Req { item = "pumpkin",      count = 2, gold = 90,  exp = 22, ask = "Sup labu laris sekali! Bawakan {n} {item} hasil kebunmu." },
            new Req { item = "leather",      count = 4, gold = 100, exp = 26, ask = "Bisa carikan {n} {item}? Kami kehabisan stok." },
        };

        /// <summary>Baris untuk pelacak quest di HUD: semua permintaan NPC yang sedang diterima.</summary>
        public static List<string> TrackerLines()
        {
            var res = new List<string>();
            var gs = GameState.I; if (gs == null) return res;
            const string pre = "shq_acc_";
            var names = new List<string>();
            foreach (var kv in gs.stats) if (kv.Key.StartsWith(pre) && kv.Value == 1) names.Add(kv.Key.Substring(pre.Length));
            foreach (var n in names)
            {
                var r = Current(n); int have = Mathf.Min(gs.Count(r.item), r.count);
                res.Add("Bawakan " + ItemName(r.item) + " " + have + "/" + r.count + " (" + n + ")");
            }
            return res;
        }

        static string K(string what, string npc) { return "shq_" + what + "_" + npc; }
        static int Get(string what, string npc) { int v; return GameState.I.stats.TryGetValue(K(what, npc), out v) ? v : 0; }
        static void Set(string what, string npc, int v) { GameState.I.stats[K(what, npc)] = v; }

        static Req Current(string npc)
        {
            int h = 0; foreach (char ch in npc) h = (h * 31 + ch) & 0x7fffffff;
            return Pool[(h + Get("n", npc)) % Pool.Length];
        }

        static string ItemName(string id) { var d = ItemDB.Get(id); return d != null ? d.name : id; }

        /// <summary>Menampilkan percakapan quest NPC. back = kembali ke menu NPC.</summary>
        public static void Open(Interactable who, NpcActor npc, System.Action back)
        {
            string name = npc.displayName; var gs = GameState.I; var r = Current(name);
            bool accepted = Get("acc", name) == 1;
            if (!accepted && Get("done", name) == gs.day && Get("n", name) > 0)
            {
                Say(npc, "Terima kasih untuk hari ini. Kalau ada lagi, aku kabari besok.", back); return;
            }
            string iname = ItemName(r.item);
            if (!accepted) { Offer(npc, r, iname, back); return; }
            int have = gs.Count(r.item);
            TownUI.I.Open(name,
                () => have >= r.count ? "Kamu membawanya? Luar biasa, serahkan padaku." : "Masih mengumpulkan " + iname + "? Aku butuh " + r.count + ". Kamu baru punya " + have + ".",
                () =>
                {
                    DialogueUI.Card = null;
                    var l = new List<UIEntry>();
                    if (have >= r.count) l.Add(new UIEntry("Serahkan " + r.count + " " + iname, () => TurnIn(npc, r, back)));
                    else l.Add(new UIEntry("Belum cukup (" + have + "/" + r.count + ")", null, false));
                    l.Add(new UIEntry("< Kembali", back));
                    return l;
                });
        }

        static void Say(NpcActor npc, string text, System.Action back)
        {
            TownUI.I.Open(npc.displayName, () => text, () => { DialogueUI.Card = null; var l = new List<UIEntry>(); l.Add(new UIEntry("< Kembali", back)); return l; });
        }

        static void Offer(NpcActor npc, Req r, string iname, System.Action back)
        {
            string name = npc.displayName;
            string text = r.ask.Replace("{n}", r.count.ToString()).Replace("{item}", iname);
            TownUI.I.Open(name,
                () => text,
                () =>
                {
                    DialogueUI.Card = new DialogueCard { title = "Permintaan", desc = "Kumpulkan " + iname + " sebanyak " + r.count + ".", itemName = iname, have = GameState.I.Count(r.item), need = r.count, gold = r.gold, exp = r.exp, acceptPrefix = "Terima" };
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Terima permintaan", () => { Set("acc", name, 1); DialogueUI.Card = null; Audio.Sfx("quest_accept"); TownUI.I.Toast("Permintaan diterima: " + iname + " x" + r.count); back(); }));
                    l.Add(new UIEntry("< Kembali", back));
                    return l;
                });
        }

        static void TurnIn(NpcActor npc, Req r, System.Action back)
        {
            var gs = GameState.I; string name = npc.displayName;
            if (!gs.RemoveItem(r.item, r.count)) return;
            int rg = Difficulty.Gold(r.gold); gs.gold += rg; gs.AddExp(r.exp);
            Set("acc", name, 0); Set("n", name, Get("n", name) + 1); Set("done", name, gs.day);
            Audio.Sfx("quest_reward");
            TownUI.I.Toast("Permintaan selesai! +" + rg + " gold, +" + r.exp + " EXP");
            Say(npc, "Terima kasih banyak! Ini imbalanmu.", back);
        }
    }
}
