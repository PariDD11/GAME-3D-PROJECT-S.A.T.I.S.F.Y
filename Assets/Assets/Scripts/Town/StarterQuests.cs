using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Latihan dari Veteran Brann (5 tahap). Progres dihitung dari statistik (GameState.stats) sejak misi diterima,
    /// sehingga ikut tersimpan di save. Tidak memakai activeQuest milik Guild.
    /// </summary>
    public static class StarterQuests
    {
        public class Stage
        {
            public string id, title, desc, stat;
            public int count, gold, exp;
            public string[] items;
            public int[] itemCounts;
        }

        public static readonly Stage[] All =
        {
            new Stage { id = "s1", title = "Latihan Dasar", stat = "dummy_hits", count = 5, gold = 20, exp = 6,
                desc = "Pukul Boneka Latihan di tengah desa 5 kali. Klik kiri / J menyerang (3 serangan beruntun = kombo), Ctrl berguling, Spasi melompat, klik kanan menahan.",
                items = new[] { "bread" }, itemCounts = new[] { 2 } },
            new Stage { id = "s2", title = "Hama Slime", stat = "kill_slime", count = 3, gold = 30, exp = 12,
                desc = "Susuri jalan utara ke Hutan Pemula dan kalahkan 3 Slime. Awas HP-mu; minum ramuan/makan roti dari tas (Tab).",
                items = new[] { "water" }, itemCounts = new[] { 2 } },
            new Stage { id = "s3", title = "Mengumpulkan Herb", stat = "gather_herb", count = 4, gold = 25, exp = 14,
                desc = "Petik 4 Herb di hutan (semak hijau bercahaya, tekan E). Herb dijual di toko dan dipakai crafting.",
                items = new[] { "bread" }, itemCounts = new[] { 3 } },
            new Stage { id = "s4", title = "Goblin Pengganggu", stat = "kill_goblin", count = 4, gold = 60, exp = 24,
                desc = "Goblin mencuri hasil ladang desa. Kalahkan 4 Goblin; mereka menyerang lebih keras, jadi gunakan guling (Ctrl).",
                items = new[] { "grilled_meat" }, itemCounts = new[] { 2 } },
            new Stage { id = "s5", title = "Ujian Veteran", stat = "level", count = 5, gold = 150, exp = 0,
                desc = "Capai Level 5 (berburu di Hutan Pemula). Di Level 5 Brann akan membimbingmu memilih job permanen, lalu kamu boleh menempuh jalan ke Ashwatch.",
                items = new[] { "bread", "water" }, itemCounts = new[] { 5, 3 } },
        };

        static GameState GS { get { return GameState.I; } }
        static string Acc(Stage s) { return "stq_acc_" + s.id; }
        static string Done(Stage s) { return "stq_done_" + s.id; }
        static string Base(Stage s) { return "stq_base_" + s.id; }

        /// <summary>Indeks tahap yang sedang berjalan (belum diklaim); All.Length bila semua selesai.</summary>
        public static int Current
        {
            get
            {
                for (int i = 0; i < All.Length; i++) if (!GS.flags.Contains(Done(All[i]))) return i;
                return All.Length;
            }
        }

        public static bool Accepted(Stage s) { return GS.flags.Contains(Acc(s)); }

        public static int Progress(Stage s)
        {
            if (s.stat == "level") return Mathf.Min(s.count, GS.level);
            int v = GS.Stat(s.stat) - GS.Stat(Base(s));
            return Mathf.Clamp(v, 0, s.count);
        }

        public static bool Ready(Stage s) { return Accepted(s) && Progress(s) >= s.count; }

        public static void Accept(Stage s)
        {
            var gs = GS;
            gs.flags.Add(Acc(s));
            gs.stats[Base(s)] = s.stat == "level" ? 0 : gs.Stat(s.stat);
            Audio.Sfx("quest_accept");
            if (TownUI.I != null) {
                string where = s.stat == "dummy_hits" ? "Boneka Latihan di komplek pelatihan desa" : s.stat == "kill_slime" || s.stat == "kill_goblin" || s.stat == "gather_herb" ? "jalan utara menuju Hutan Pemula" : "Hutan Pemula";
                TownUI.I.Toast("Latihan diterima: " + s.title + ". Tujuan: " + where + ". Ikuti penanda biru di kompas & minimap.");
            }
        }

        public static string RewardText(Stage s)
        {
            string t = s.gold + " gold" + (s.exp > 0 ? ", " + s.exp + " EXP" : "");
            for (int i = 0; s.items != null && i < s.items.Length; i++)
            {
                var d = ItemDB.Get(s.items[i]);
                if (d != null) t += ", " + d.name + " x" + s.itemCounts[i];
            }
            return t;
        }

        public static void Claim(Stage s)
        {
            var gs = GS;
            if (!Ready(s)) return;
            gs.flags.Add(Done(s));
            gs.gold += s.gold;
            if (s.exp > 0) gs.AddExp(s.exp);
            for (int i = 0; s.items != null && i < s.items.Length; i++) gs.AddItem(s.items[i], s.itemCounts[i]);
            Audio.Sfx("quest_reward");
            if (TownUI.I != null) TownUI.I.Toast("Misi selesai: " + s.title + "! Hadiah: " + RewardText(s));
        }

        /// <summary>Dipanggil Monster/GatherNode lewat HuntManager.Report dan boneka latihan.</summary>
        public static void OnEvent(string kind, string id)
        {
            var gs = GS;
            if (gs == null) return;
            string key = kind == "kill" ? "kill_" + id : kind == "gather" ? "gather_" + id : kind == "dummy" ? "dummy_hits" : null;
            if (key == null) return;
            gs.AddStat(key);
            if (kind == "kill" || kind == "gather") WorldQuests.OnEvent(kind, id);
            int cur = Current;
            if (cur >= All.Length) return;
            var s = All[cur];
            if (!Accepted(s) || s.stat != key) return;
            int p = Progress(s);
            if (TownUI.I == null) return;
            if (p >= s.count) { Audio.Sfx("quest_done"); TownUI.I.Toast("Latihan selesai! Lapor ke Veteran Brann: " + s.title); }
            else if (key != "dummy_hits" || p == s.count - 1 || p % 2 == 0) TownUI.I.Toast("Latihan: " + s.title + " " + p + "/" + s.count);
        }
    }
}
