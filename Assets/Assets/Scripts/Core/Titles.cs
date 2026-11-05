using System;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n27: Gelar kosmetik + pelacak koleksi. Gelar TIDAK memberi kekuatan; hanya teks yang tampil di menu jeda dan ringkasan "Perjalananmu".
    /// Terbuka tersimpan sebagai flag "title_&lt;id&gt;" (ikut save). Gelar dipilih disimpan di stat "title_sel" (indeks + 1).
    /// Sumber gelar: 100% Bestiary, 100% Kodeks Lore, semua boss rahasia, semua kasus investigasi, semua pencapaian, semua akhir cerita, dan Tantangan Menara.
    /// </summary>
    public class Titles : MonoBehaviour
    {
        public class Title { public string id, name, how; public Func<GameState, bool> auto; }
        public static readonly List<Title> All = new List<Title>();

        static void T(string id, string name, string how, Func<GameState, bool> auto = null) { All.Add(new Title { id = id, name = name, how = how, auto = auto }); }

        static Titles()
        {
            T("bestiary", "Ahli Bestiari", "Temukan 100% monster di Bestiary.", g => BestiaryPercent(g) >= 100);
            T("lore", "Penjaga Ingatan", "Buka 100% Kodeks Lore.", g => LoreDB.All.Length > 0 && LoreDB.UnlockedCount >= LoreDB.All.Length);
            T("hidden", "Pemburu Rahasia", "Kalahkan semua boss rahasia.", g => HiddenBosses.DefeatedCount() >= HiddenBosses.Defs.Length);
            T("cases", "Detektif Guild", "Selesaikan semua kasus investigasi.", g => { foreach (var c in Investigation.Cases) if (Investigation.State(c) != 2) return false; return true; });
            T("ach", "Legenda Guild", "Buka semua pencapaian.", g => Achievements.UnlockedCount(g) >= Achievements.Total);
            T("endings", "Saksi Tiga Pintu", "Lihat ketiga akhir cerita.", g => Endings.Seen >= Endings.Total);
            T("flawless1", "Tak Tersentuh", "Kalahkan satu boss tanpa terkena damage.");
            T("flawless10", "Bayangan Tanpa Luka", "Kalahkan 10 boss tanpa terkena damage.");
            T("ng1", "Pengulang Takdir", "Mulai Putaran Baru (NG+) pertama.");
            T("ng3", "Penempa Legenda", "Capai Putaran Baru ke-3.");
            foreach (var r in TowerChallenge.Rules)
            {
                T("tc_" + r.id + "_1", r.title + " I", "Tantangan Menara '" + r.name + "': bersihkan lantai 10.");
                T("tc_" + r.id + "_2", r.title + " II", "Tantangan Menara '" + r.name + "': bersihkan lantai 25.");
                T("tc_" + r.id + "_3", r.title + " III", "Tantangan Menara '" + r.name + "': bersihkan lantai 50.");
            }
        }

        public static int BestiaryPercent(GameState g)
        {
            var all = MonsterDB.All(); int n = 0, tot = 0;
            foreach (var d in all) { if (d == null || d.passive || d.id == "dummy") continue; tot++; if (g.Stat("bk_" + d.id) > 0) n++; }
            return tot == 0 ? 0 : Mathf.FloorToInt(100f * n / tot);
        }

        public static bool Has(string id) { return GameState.I != null && GameState.I.flags.Contains("title_" + id); }
        public static int UnlockedCount { get { int n = 0; foreach (var t in All) if (Has(t.id)) n++; return n; } }

        public static void Unlock(string id)
        {
            var gs = GameState.I; if (gs == null || Has(id)) return;
            Title tt = null; foreach (var t in All) if (t.id == id) tt = t; if (tt == null) return;
            gs.flags.Add("title_" + id);
            try { Audio.Sfx("quest_reward"); if (TownUI.I != null) TownUI.I.Toast("Gelar baru: " + tt.name); } catch { }
        }

        public static string Current
        {
            get
            {
                var gs = GameState.I; if (gs == null) return "";
                int i = gs.Stat("title_sel") - 1;
                return i >= 0 && i < All.Count && Has(All[i].id) ? All[i].name : "";
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<Titles>() != null) return;
            var go = new GameObject("Titles"); DontDestroyOnLoad(go); go.AddComponent<Titles>();
        }

        float t = 6f;
        void Update()
        {
            t -= Time.unscaledDeltaTime; if (t > 0f) return; t = 5f;
            var gs = GameState.I; if (gs == null) return;
            foreach (var x in All) if (x.auto != null && !Has(x.id)) { bool ok = false; try { ok = x.auto(gs); } catch { } if (ok) Unlock(x.id); }
        }

        // ------------------------------------------------------------ menu
        public static void OpenCollection(Action back)
        {
            var ui = TownUI.I;
            ui.Open("Koleksi & Gelar",
                () =>
                {
                    var gs = GameState.I;
                    int lt = LoreDB.All.Length, caseDone = 0; foreach (var c in Investigation.Cases) if (Investigation.State(c) == 2) caseDone++;
                    return "Gelar aktif: " + (Current == "" ? "-" : Current) + "   (gelar hanya kosmetik, tidak menambah kekuatan)\n\n" +
                           "Bestiary: " + BestiaryPercent(gs) + "%    Kodeks Lore: " + LoreDB.UnlockedCount + "/" + lt + "\n" +
                           "Boss rahasia: " + HiddenBosses.DefeatedCount() + "/" + HiddenBosses.Defs.Length + "    Kasus investigasi: " + caseDone + "/" + Investigation.Cases.Length + "\n" +
                           "Pencapaian: " + Achievements.UnlockedCount(gs) + "/" + Achievements.Total + "    Akhir cerita: " + Endings.Seen + "/" + Endings.Total + "\n" +
                           "Gelar terbuka: " + UnlockedCount + "/" + All.Count;
                },
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Tanpa gelar", () => { GameState.I.stats["title_sel"] = 0; OpenCollection(back); }));
                    for (int i = 0; i < All.Count; i++)
                    {
                        int k = i; var tt = All[i]; bool has = Has(tt.id);
                        l.Add(new UIEntry(has ? (Current == tt.name ? "[Dipakai] " : "") + tt.name + "  -  " + tt.how : "??? terkunci  -  " + tt.how, () => { GameState.I.stats["title_sel"] = k + 1; OpenCollection(back); }, has));
                    }
                    l.Add(new UIEntry("< Kembali", () => { if (back != null) back(); }));
                    return l;
                });
        }
    }
}
