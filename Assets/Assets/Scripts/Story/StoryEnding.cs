using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Ending cerita: ditentukan jumlah nama di Buku Nama + dua pilihan dialog (Teno, ketukan di Altar).</summary>
    public static class StoryEnding
    {
        public static string Tier()
        {
            int n = NameBook.Count;
            return n >= 45 ? "penuh" : n >= 30 ? "ramai" : n >= 10 ? "kecil" : "sunyi";
        }

        public static string TierTitle()
        {
            switch (Tier()) { case "penuh": return "Dinding yang Tak Cukup"; case "ramai": return "Balai yang Ramai"; case "kecil": return "Balai Kecil"; default: return "Balai yang Sunyi"; }
        }

        public static List<string> Lines()
        {
            var g = GameState.I; var l = new List<string>(); if (g == null) return l;
            switch (Tier())
            {
                case "penuh": l.Add("Dinding Nama tak cukup lagi. Orang menulis di pagar, di tangga, di pintu Balai. Tidak satu pun dihapus."); break;
                case "ramai": l.Add("Balai Nama ramai. Setiap pagi ada yang menulis, setiap malam ada yang membaca keras-keras."); break;
                case "kecil": l.Add("Balai Nama punya satu dinding penuh nama dan satu dapur yang tidak pernah dingin."); break;
                default: l.Add("Balai Nama berdiri kecil dan sepi. Dindingnya hampir kosong, tapi tidak ada yang menghapusnya."); break;
            }
            if (g.flags.Contains("teno_jaga")) l.Add("Lentera Teno masih menyala di Padang Hantu. Seseorang menggantinya setiap bulan.");
            else if (g.flags.Contains("teno_ambil")) l.Add("Lentera Teno tergantung di jendela Ari. Nyalanya sedikit lebih redup, tapi tidak pernah padam.");
            if (g.flags.Contains("ketuk_jawab")) l.Add("Kadang ia menjawab ketukan itu, pelan: aku di sini.");
            else if (g.flags.Contains("ketuk_diam")) l.Add("Ia tidak pernah menjawab. Tapi ia tidak pernah berhenti mendengarkan.");
            if (g.flags.Contains("dagan_gone")) l.Add("Setiap musim panas Ari menyalakan satu api kecil di halaman Guild. Hanya sedikit. Tidak membakar apa pun.");
            return l;
        }
    }

    /// <summary>Jurnal Cerita: daftar bab Kronik (terbuka / terkunci), Cap Eldrath, tujuan aktif, dan ending. Dibuka dari Buku Nama (N).</summary>
    public static class StoryJournal
    {
        class Grp { public string cat; public List<StoryLore> items = new List<StoryLore>(); }

        static List<Grp> Groups()
        {
            var res = new List<Grp>(); var map = new Dictionary<string, Grp>();
            foreach (var l in WorldStory.Root.lore)
            {
                if (l == null || string.IsNullOrEmpty(l.cat) || !l.cat.StartsWith("Kronik · ") || !l.id.StartsWith("nd_c")) continue;
                Grp g; if (!map.TryGetValue(l.cat, out g)) { g = new Grp { cat = l.cat }; map[l.cat] = g; res.Add(g); }
                g.items.Add(l);
            }
            return res;
        }

        public static void Open(System.Action back)
        {
            var ui = TownUI.I; if (ui == null) return;
            ui.Open("Jurnal Cerita", () =>
            {
                var sb = new System.Text.StringBuilder();
                int total = 0, read = 0; foreach (var g in Groups()) foreach (var l in g.items) { total++; if (LoreDB.Has(l.id)) read++; }
                sb.Append("Kronik terbuka: ").Append(read).Append(" / ").Append(total).Append("\n");
                sb.Append("Cap Eldrath: ").Append(StoryCutscenes.Caps).Append(" / 3");
                var rows = WorldQuests.TrackerRows();
                if (rows.Count > 0) sb.Append("\nTujuan sekarang: ").Append(rows[0][0]).Append(" - ").Append(rows[0][1]);
                else sb.Append("\nTidak ada quest cerita aktif.");
                if (StoryCutscenes.Seen("cs_epilog")) sb.Append("\nEnding: ").Append(StoryEnding.TierTitle());
                return sb.ToString();
            }, () =>
            {
                var l = new List<UIEntry>();
                foreach (var gg in Groups())
                {
                    var g = gg; int r = 0; foreach (var it in g.items) if (LoreDB.Has(it.id)) r++;
                    l.Add(new UIEntry(g.cat.Replace("Kronik · ", "") + "  (" + r + "/" + g.items.Count + ")", () => OpenGroup(g, back)));
                }
                l.Add(new UIEntry(back != null ? "< Kembali" : "Tutup", back != null ? back : (System.Action)(() => ui.Close())));
                return l;
            });
        }

        static void OpenGroup(Grp g, System.Action back)
        {
            var ui = TownUI.I;
            ui.Open(g.cat.Replace("Kronik · ", ""), () => "Pilih bab untuk dibaca ulang. Bab terbuka lewat quest cerita.", () =>
            {
                var l = new List<UIEntry>();
                foreach (var it in g.items)
                {
                    var x = it;
                    if (LoreDB.Has(x.id)) l.Add(new UIEntry("[v] " + x.title, () => LoreCodex.Read(x.id, 0, () => OpenGroup(g, back))));
                    else l.Add(new UIEntry("[ ] " + x.title + "  (terkunci)", () => { }, false));
                }
                l.Add(new UIEntry("< Kembali", () => Open(back)));
                return l;
            });
        }
    }
}
