using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Panel info dari HUD: "Gold &amp; Botol Minum" (klik panel emas) dan "Target" (petunjuk lokasi misi aktif).</summary>
    public static class HudInfoUI
    {
        const string Gold = "<color=#F0D58A>", Blue = "<color=#9FD3FF>", Dim = "<color=#B9B3A0>", End = "</color>";
        static readonly string[] Dir8 = { "utara", "timur laut", "timur", "tenggara", "selatan", "barat daya", "barat", "barat laut" };

        // ------------------------------------------------------------------ Gold & Botol Minum
        public static void OpenGold()
        {
            var ui = TownUI.I; if (ui == null || ui.PanelOpen) return;
            ui.Open("Gold & Botol Minum",
                () =>
                {
                    var gs = GameState.I; var sb = new StringBuilder();
                    sb.Append(Gold).Append("Gold  ").Append(End).Append(gs.gold.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)).Append("\n");
                    sb.Append(Gold).Append("Botol Minum  ").Append(End).Append(gs.canteen).Append(" / ").Append(GameState.CanteenMax).Append("   ").Append(Dim).Append("(tiap tegukan +20 Thirst, tombol F)").Append(End).Append("\n\n");
                    sb.Append(Blue).Append("Cara mendapat Gold").Append(End).Append("\n");
                    sb.Append("- Selesaikan quest Guild, lalu lapor ke resepsionis Guild.\n");
                    sb.Append("- Jual hasil buruan dan bahan ke Guild / Toko Umum.\n");
                    sb.Append("- Kalahkan monster dan boss; klaim hadiah Bestiari (B).\n");
                    sb.Append("- Latihan Veteran Brann, peti Menara Eldrath, dan NPC tersembunyi.\n\n");
                    sb.Append(Blue).Append("Cara mengisi Botol Minum").Append(End).Append("\n");
                    sb.Append("- Gratis di Kedai makan dan di Base Camp.\n");
                    sb.Append("- Di sumber air alami (sungai, danau, oasis): dekati lalu tekan E.\n");
                    sb.Append("- Botol kosong? Cari sumber air terdekat sebelum Thirst habis.");
                    return sb.ToString();
                },
                () => new List<UIEntry> { new UIEntry("Tutup (Esc)", () => TownUI.I.Close()) });
        }

        // ------------------------------------------------------------------ Target
        public static void OpenTarget()
        {
            var ui = TownUI.I; if (ui == null || ui.PanelOpen) return;
            ui.Open("Target Misi",
                () => Build(),
                () => new List<UIEntry>
                {
                    new UIEntry("Buka Jurnal Quest >", () => { TownUI.I.Close(); PlayerController.OpenWindow(3); }),
                    new UIEntry("Tutup (Esc)", () => TownUI.I.Close())
                });
        }

        static string Where(Vector3 p)
        {
            var pc = PlayerCombat.I; if (pc == null) return "";
            Vector3 d = p - pc.transform.position; d.y = 0f;
            float ang = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; if (ang < 0f) ang += 360f;
            return Mathf.RoundToInt(d.magnitude) + " m ke " + Dir8[Mathf.RoundToInt(ang / 45f) % 8];
        }

        static string Build()
        {
            var gs = GameState.I; var pc = PlayerCombat.I; var sb = new StringBuilder(); bool any = false;

            // 1) quest Guild
            var q = gs.activeQuest;
            if (q != null && !string.IsNullOrEmpty(q.title))
            {
                any = true;
                var gq = GuildQuestDB.Find(q.id);
                sb.Append(Gold).Append(q.title).Append(End).Append("\n");
                if (q.completed) sb.Append("Selesai. Kembali ke Guild dan lapor ke resepsionis.\n");
                else
                {
                    string tname = q.targetId; var md = MonsterDB.Get(q.targetId); var idf = ItemDB.Get(q.targetId);
                    if (md != null) tname = md.name; else if (idf != null) tname = idf.name;
                    sb.Append(q.type == "gather" ? "Kumpulkan " : q.type == "hunt" ? "Buru " : "Basmi ").Append(tname).Append("  ").Append(gs.questProgress).Append("/").Append(q.targetCount).Append("\n");
                    if (gq != null) sb.Append(Dim).Append("Wilayah: ").Append(GuildQuest.RegionName(gq.Region)).Append(End).Append("\n");
                    sb.Append(Blue).Append("Petunjuk").Append(End).Append("\n");
                    string hint = "";
                    if (md != null)
                    {
                        var m = Nearest(delegate (Monster x) { return x.defId == q.targetId; });
                        hint = m != null ? "- " + md.name + " terdekat: " + Where(m.transform.position) + ".\n" : "- " + md.name + " belum terlihat di sekitarmu; jelajahi area " + (gq != null ? gq.area : "quest") + ".\n";
                        if (q.type == "hunt") hint += "- Buruan besar butuh 3 jejak dulu; ikuti penanda di kompas.\n";
                    }
                    else if (idf != null)
                    {
                        GatherNode best = null; float bd = 1e9f;
                        if (pc != null) foreach (var n in GatherNode.Nodes) { if (n == null || n.itemId != q.targetId) continue; float d = (n.transform.position - pc.transform.position).sqrMagnitude; if (d < bd) { bd = d; best = n; } }
                        hint = best != null ? "- Titik ambil " + idf.name + " terdekat: " + Where(best.transform.position) + " (tekan E).\n" : "- " + idf.name + " belum terlihat di sekitarmu.\n";
                        var from = new List<string>();
                        foreach (var d2 in MonsterDB.All()) if (d2.loot != null && System.Array.IndexOf(d2.loot, q.targetId) >= 0) from.Add(d2.name);
                        if (from.Count > 0) hint += "- Juga bisa jatuh dari: " + string.Join(", ", from.ToArray()) + ".\n";
                    }
                    if (Minimap.Markers.Count > 0) { var m0 = Minimap.Markers[0]; hint += "- Penanda kompas: " + m0.label + ", " + Where(m0.pos) + ".\n"; }
                    if (hint == "") hint = "- Berangkat lewat gerbang sesuai wilayah quest, bawa bekal dan Botol Minum penuh.\n";
                    sb.Append(hint);
                }
                sb.Append("\n");
            }

            // 2) latihan pemandu / Veteran Brann
            if (Minimap.WpOn)
            {
                any = true;
                sb.Append(Gold).Append("Latihan Pemula").Append(End).Append("\n");
                sb.Append("Tujuan berikutnya: ").Append(Minimap.WpLabel).Append(" - ").Append(Where(Minimap.WpPos)).Append(".\n");
                sb.Append(Dim).Append("Ikuti penanda biru di kompas dan minimap. Tekan Y untuk mengulang penjelasan pemandu.").Append(End).Append("\n");
            }
            else if (StarterQuests.Current < StarterQuests.All.Length && StarterQuests.Accepted(StarterQuests.All[StarterQuests.Current]))
            {
                any = true; var s = StarterQuests.All[StarterQuests.Current];
                sb.Append(Gold).Append(s.title).Append(End).Append("\n").Append(s.desc).Append("\n");
            }

            if (!any)
            {
                sb.Append(Gold).Append("Belum ada misi yang berjalan.").Append(End).Append("\n");
                sb.Append("Ambil quest di Papan Quest Guild, atau temui Veteran Brann di Desa Pemula untuk latihan.\nSaat misi berjalan, petunjuk letak target akan muncul di sini.");
            }
            return sb.ToString();
        }

        static Monster Nearest(System.Predicate<Monster> ok)
        {
            var pc = PlayerCombat.I; if (pc == null) return null;
            Monster best = null; float bd = 1e9f;
            foreach (var m in Monster.All) { if (m == null || m.IsDead || !ok(m)) continue; float d = (m.transform.position - pc.transform.position).sqrMagnitude; if (d < bd) { bd = d; best = m; } }
            return best;
        }
    }
}
