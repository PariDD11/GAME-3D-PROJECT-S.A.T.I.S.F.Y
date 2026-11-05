using System;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n15: pencapaian lokal (offline). Dihitung dari statistik yang sudah ada di GameState (stats/flags/level/rank/Menara).
    /// Terbuka disimpan sebagai flag "ach_&lt;id&gt;" di gs.flags, jadi ikut tersimpan di save slot tanpa perubahan SaveSystem.
    /// Tidak ada hadiah; hanya catatan. Dibuka dari menu jeda: "Pencapaian >".
    /// </summary>
    public class Achievements : MonoBehaviour
    {
        class Ach
        {
            public string id, cat, name, desc;
            public Func<GameState, int> cur; public int max;
        }

        static readonly List<Ach> All = new List<Ach>();
        static readonly string[] Cats = { "Perburuan", "Perjalanan", "Menara", "Dunia" };

        static void A(string id, string cat, string name, string desc, int max, Func<GameState, int> cur)
        {
            All.Add(new Ach { id = id, cat = cat, name = name, desc = desc, max = max, cur = cur });
        }
        static Func<GameState, int> S(string key) { return g => g.Stat(key); }

        static Achievements()
        {
            A("kill1", "Perburuan", "Darah Pertama", "Kalahkan 1 monster.", 1, S("kills"));
            A("kill100", "Perburuan", "Pemburu Sejati", "Kalahkan 100 monster.", 100, S("kills"));
            A("kill500", "Perburuan", "Momok Hutan", "Kalahkan 500 monster.", 500, S("kills"));
            A("kill2000", "Perburuan", "Legenda Perburuan", "Kalahkan 2000 monster.", 2000, S("kills"));
            A("boss1", "Perburuan", "Penakluk Boss", "Kalahkan 1 boss.", 1, S("boss_kills"));
            A("boss10", "Perburuan", "Pembunuh Raja", "Kalahkan 10 boss.", 10, S("boss_kills"));
            A("big10", "Perburuan", "Pemburu Raksasa", "Kalahkan 10 monster besar.", 10, S("big_kills"));
            A("arena5", "Perburuan", "Gladiator", "Menangkan 5 pertarungan Arena.", 5, S("arena_wins"));

            A("lv10", "Perjalanan", "Petualang Muda", "Capai level 10.", 10, g => g.level);
            A("lv30", "Perjalanan", "Veteran", "Capai level 30.", 30, g => g.level);
            A("lv60", "Perjalanan", "Pahlawan", "Capai level 60.", 60, g => g.level);
            A("quest1", "Perjalanan", "Anggota Guild", "Selesaikan 1 quest.", 1, g => g.questsDone);
            A("quest25", "Perjalanan", "Andalan Guild", "Selesaikan 25 quest.", 25, g => g.questsDone);
            A("quest100", "Perjalanan", "Tulang Punggung Guild", "Selesaikan 100 quest.", 100, g => g.questsDone);
            A("rankD", "Perjalanan", "Rank D", "Capai rank D.", 1, g => GameState.RankIndex(g.rank) >= GameState.RankIndex("D") ? 1 : 0);
            A("rankB", "Perjalanan", "Rank B", "Capai rank B.", 1, g => GameState.RankIndex(g.rank) >= GameState.RankIndex("B") ? 1 : 0);
            A("rankS", "Perjalanan", "Rank S", "Capai rank S.", 1, g => GameState.RankIndex(g.rank) >= GameState.RankIndex("S") ? 1 : 0);

            A("tw10", "Menara", "Kaki Menara", "Capai lantai 10 Menara.", 10, g => g.dungeonBest);
            A("tw50", "Menara", "Separuh Jalan", "Capai lantai 50 Menara.", 50, g => g.dungeonBest);
            A("tw100", "Menara", "Puncak Eldrath", "Capai lantai 100 Menara.", 100, g => g.dungeonBest);
            A("chest20", "Menara", "Pemburu Harta", "Buka 20 peti Menara.", 20, S("tower_chests"));
            A("ng1", "Menara", "Putaran Baru", "Mulai Putaran Baru (NG+) pertama.", 1, S("ngplus"));
            A("ng3", "Menara", "Tak Kenal Henti", "Capai Putaran Baru ke-3.", 3, S("ngplus"));

            A("end1", "Dunia", "Satu Akhir", "Temukan satu akhir cerita.", 1, g => Endings.Seen);
            A("end3", "Dunia", "Tiga Pintu", "Temukan ketiga akhir cerita.", 3, g => Endings.Seen);
            A("mine50", "Dunia", "Penambang", "Tambang 50 kali.", 50, S("mined"));
            A("herb50", "Dunia", "Peramu", "Panen 50 tanaman/herb.", 50, S("harvested"));
            A("fish25", "Dunia", "Pemancing", "Tangkap 25 ikan.", 25, S("fished"));
            A("cook25", "Dunia", "Juru Masak", "Masak 25 hidangan.", 25, S("cooked"));
            A("mail10", "Dunia", "Kurir Andal", "Antar 10 surat.", 10, S("letters"));
            A("case3", "Dunia", "Detektif", "Selesaikan 3 kasus penyelidikan.", 3, S("cases"));
            A("lore10", "Dunia", "Penjaga Kisah", "Buka 10 catatan lore.", 10, g => LoreDB.UnlockedCount);
        }

        public static bool Has(GameState gs, string id) { return gs != null && gs.flags.Contains("ach_" + id); }
        public static int Total { get { return All.Count; } }
        public static int UnlockedCount(GameState gs)
        {
            int n = 0; foreach (var a in All) if (Has(gs, a.id)) n++; return n;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<Achievements>() != null) return;
            var go = new GameObject("Achievements"); DontDestroyOnLoad(go); go.AddComponent<Achievements>();
        }

        float t = 4f;
        void Update()
        {
            t -= Time.unscaledDeltaTime;
            if (t > 0f) return;
            t = 3f;
            var gs = GameState.I; if (gs == null) return;
            var fresh = new List<string>();
            foreach (var a in All)
            {
                if (gs.flags.Contains("ach_" + a.id)) continue;
                int c; try { c = a.cur(gs); } catch { continue; }
                if (c >= a.max) { gs.flags.Add("ach_" + a.id); fresh.Add(a.name); }
            }
            if (fresh.Count == 0) return;
            try { Audio.Sfx("quest_reward"); } catch { }
            if (TownUI.I != null)
                TownUI.I.Toast(fresh.Count == 1 ? "Pencapaian terbuka: " + fresh[0] : "Pencapaian terbuka: " + fresh[0] + " dan " + (fresh.Count - 1) + " lainnya");
        }

        // ---------- Menu ----------
        public static void Open(Action back)
        {
            var ui = TownUI.I; if (ui == null) return;
            ui.Open("PENCAPAIAN",
                () => "Terbuka " + UnlockedCount(GameState.I) + " dari " + Total + ". Pilih kategori.",
                () =>
                {
                    var l = new List<UIEntry>();
                    var gs = GameState.I;
                    l.Add(new UIEntry("Rekor Pribadi >", () => OpenRecords(back)));
                    l.Add(new UIEntry("Perjalananmu (ringkasan & akhir cerita) >", () => Endings.Summary(() => Open(back))));
                    foreach (var c0 in Cats)
                    {
                        var c = c0; int n = 0, tot = 0;
                        foreach (var a in All) if (a.cat == c) { tot++; if (Has(gs, a.id)) n++; }
                        l.Add(new UIEntry(c + "  (" + n + "/" + tot + ") >", () => OpenCat(c, back)));
                    }
                    l.Add(new UIEntry("< Kembali", () => { if (back != null) back(); }));
                    return l;
                });
        }

        /// <summary>v35z102n22: catatan pribadi pemain (menggantikan peringkat pemain lain di game offline).</summary>
        static void OpenRecords(Action back)
        {
            var ui = TownUI.I;
            ui.Open("REKOR PRIBADI",
                () =>
                {
                    var gs = GameState.I;
                    var sb = new System.Text.StringBuilder();
                    sb.Append("Level ").Append(gs.level).Append("   Rank ").Append(gs.rank).Append("   Hari ").Append(gs.day).Append('\n');
                    sb.Append("Quest Guild selesai: ").Append(gs.questsDone).Append('\n');
                    sb.Append("Monster dikalahkan: ").Append(gs.Stat("kills")).Append("   Boss: ").Append(gs.Stat("boss_kills")).Append("   Besar: ").Append(gs.Stat("big_kills")).Append('\n');
                    sb.Append("Menara: lantai tertinggi ").Append(gs.dungeonBest).Append(" dari 100   Peti dibuka: ").Append(gs.Stat("tower_chests")).Append('\n');
                    sb.Append("Kemenangan Arena: ").Append(gs.Stat("arena_wins")).Append('\n');
                    sb.Append("Putaran Baru (NG+): ").Append(gs.Stat("ngplus")).Append("   Kesulitan: ").Append(Difficulty.Name).Append(Difficulty.Permadeath ? " (Mati Permanen)" : "").Append('\n');
                    sb.Append("Gold: ").Append(gs.gold).Append("   Lore terbuka: ").Append(LoreDB.UnlockedCount).Append('\n');
                    sb.Append("Pencapaian: ").Append(UnlockedCount(gs)).Append('/').Append(Total);
                    return sb.ToString();
                },
                () => new List<UIEntry> { new UIEntry("< Kembali", () => Open(back)) });
        }

        static void OpenCat(string cat, Action back)
        {
            var ui = TownUI.I;
            ui.Open("PENCAPAIAN: " + cat.ToUpper(),
                () => "[x] = terbuka. Angka di kanan = progres.",
                () =>
                {
                    var l = new List<UIEntry>();
                    var gs = GameState.I;
                    foreach (var a in All)
                    {
                        if (a.cat != cat) continue;
                        bool got = Has(gs, a.id);
                        int c = 0; try { c = Mathf.Min(a.max, a.cur(gs)); } catch { }
                        string prog = got ? "selesai" : (a.max > 1 ? c + "/" + a.max : "belum");
                        l.Add(new UIEntry((got ? "[x] " : "[ ] ") + a.name + " - " + a.desc + "  (" + prog + ")", () => { }));
                    }
                    l.Add(new UIEntry("< Kembali", () => Open(back)));
                    return l;
                });
        }
    }
}
