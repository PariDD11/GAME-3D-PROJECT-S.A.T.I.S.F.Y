using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>v35z102n32: Misi Harian (3, ganti tiap hari game) dan Misi Mingguan (3, ganti tiap 7 hari game). Kemajuan dihitung dari statistik yang sudah ada
    /// (kill, memancing, menambang, dst.) dikurangi nilai awal saat misi dibuat. Semua status disimpan di GameState.stats (ikut save):
    /// dq_day, dq_wk, dq_k{i} (templat), dq_t{i} (target), dq_b{i} (nilai awal), dq_c{i} (sudah diklaim), dq_n{i} (sudah diberi tahu), dq_streak, dq_lastfull.
    /// Slot 0-2 harian, 3-5 mingguan. Klaim di Resepsionis Guild (Menu Jeda: lihat saja). Angka hadiah tebakan awal.</summary>
    public static class DailyQuests
    {
        class T { public string stat, text; public int baseN, perLvlDiv, cap, gold; public bool needTower; }

        static readonly T[] Daily =
        {
            new T { stat = "kills", text = "Kalahkan {0} monster", baseN = 8, perLvlDiv = 2, cap = 40, gold = 70 },
            new T { stat = "animals_hunted", text = "Buru {0} hewan buruan", baseN = 2, perLvlDiv = 12, cap = 5, gold = 55 },
            new T { stat = "fished", text = "Tangkap {0} ikan", baseN = 3, perLvlDiv = 10, cap = 8, gold = 55 },
            new T { stat = "mined", text = "Tambang {0} kali", baseN = 5, perLvlDiv = 5, cap = 14, gold = 60 },
            new T { stat = "harvested", text = "Panen / kumpulkan {0} bahan", baseN = 5, perLvlDiv = 5, cap = 14, gold = 55 },
            new T { stat = "cooked", text = "Masak {0} hidangan", baseN = 2, perLvlDiv = 15, cap = 5, gold = 50 },
            new T { stat = "jobs_done", text = "Selesaikan {0} kerja sampingan", baseN = 1, perLvlDiv = 30, cap = 2, gold = 80 },
            new T { stat = "big_kills", text = "Kalahkan {0} monster besar", baseN = 2, perLvlDiv = 8, cap = 6, gold = 100 },
            new T { stat = "tower_kills", text = "Kalahkan {0} monster di Menara", baseN = 8, perLvlDiv = 3, cap = 25, gold = 110, needTower = true },
        };
        static readonly T[] Weekly =
        {
            new T { stat = "kills", text = "Kalahkan {0} monster minggu ini", baseN = 60, perLvlDiv = 1, cap = 150, gold = 420 },
            new T { stat = "boss_kills", text = "Kalahkan {0} boss", baseN = 1, perLvlDiv = 25, cap = 3, gold = 600 },
            new T { stat = "big_kills", text = "Kalahkan {0} monster besar", baseN = 10, perLvlDiv = 4, cap = 25, gold = 450 },
            new T { stat = "fished", text = "Tangkap {0} ikan", baseN = 15, perLvlDiv = 3, cap = 40, gold = 380 },
            new T { stat = "mined", text = "Tambang {0} kali", baseN = 25, perLvlDiv = 2, cap = 60, gold = 400 },
            new T { stat = "harvested", text = "Panen / kumpulkan {0} bahan", baseN = 25, perLvlDiv = 2, cap = 60, gold = 380 },
            new T { stat = "cases", text = "Selesaikan {0} kasus investigasi", baseN = 1, perLvlDiv = 30, cap = 2, gold = 520 },
            new T { stat = "trade_profit", text = "Raih untung dagang {0} gold", baseN = 400, perLvlDiv = 1, cap = 2500, gold = 450 },
            new T { stat = "jobs_done", text = "Selesaikan {0} kerja sampingan", baseN = 5, perLvlDiv = 10, cap = 10, gold = 420 },
            new T { stat = "tower_kills", text = "Kalahkan {0} monster di Menara", baseN = 40, perLvlDiv = 1, cap = 120, gold = 480, needTower = true },
        };

        static GameState G { get { return GameState.I; } }
        static int Week { get { return (Mathf.Max(1, GameCalendar.Day) - 1) / 7; } }

        static T[] Pool(int slot) { return slot < 3 ? Daily : Weekly; }
        static int Target(T t, int slot)
        {
            int n = t.baseN + (t.perLvlDiv > 0 ? G.level / t.perLvlDiv : 0);
            if (t.stat == "trade_profit") n = t.baseN + G.level * 20;
            else if (t.stat == "kills" && slot >= 3) n = t.baseN + G.level * 2;
            return Mathf.Clamp(n, 1, t.cap);
        }

        /// <summary>Buat ulang misi bila hari / minggu berganti. Dipanggil host tiap detik dan sebelum membuka menu.</summary>
        public static void Ensure()
        {
            var gs = G; if (gs == null) return;
            int day = Mathf.Max(1, GameCalendar.Day);
            if (gs.Stat("dq_day") != day) { Roll(0, day * 7919, day, true); gs.stats["dq_day"] = day; }
            if (gs.Stat("dq_wk") != Week + 1) { Roll(3, (Week + 1) * 104729, Week + 1, false); gs.stats["dq_wk"] = Week + 1; }
        }

        static void Roll(int first, int seed, int stamp, bool daily)
        {
            var gs = G;
            if (daily && gs.Stat("dq_lastfull") < stamp - 1) gs.stats["dq_streak"] = 0;      // hari kemarin tidak lengkap: streak putus
            var rnd = new System.Random(seed);
            var pool = Pool(first);
            var pick = new List<int>();
            for (int i = 0; i < pool.Length; i++) if (!pool[i].needTower || gs.dungeonBest > 0 || gs.level >= 15) pick.Add(i);
            for (int i = pick.Count - 1; i > 0; i--) { int j = rnd.Next(i + 1); int tmp = pick[i]; pick[i] = pick[j]; pick[j] = tmp; }
            for (int s = 0; s < 3; s++)
            {
                int slot = first + s; var t = pool[pick[s % pick.Count]];
                gs.stats["dq_k" + slot] = pick[s % pick.Count];
                gs.stats["dq_t" + slot] = Target(t, slot);
                gs.stats["dq_b" + slot] = gs.Stat(t.stat);
                gs.stats["dq_c" + slot] = 0; gs.stats["dq_n" + slot] = 0;
            }
        }

        static T Tpl(int slot) { var p = Pool(slot); int k = G.Stat("dq_k" + slot); return p[Mathf.Clamp(k, 0, p.Length - 1)]; }
        public static int Progress(int slot) { var t = Tpl(slot); return Mathf.Clamp(G.Stat(t.stat) - G.Stat("dq_b" + slot), 0, G.Stat("dq_t" + slot)); }
        public static int TargetOf(int slot) { return Mathf.Max(1, G.Stat("dq_t" + slot)); }
        public static bool Done(int slot) { return Progress(slot) >= TargetOf(slot); }
        public static bool Claimed(int slot) { return G.Stat("dq_c" + slot) == 1; }
        public static string Text(int slot) { return string.Format(Tpl(slot).text, TargetOf(slot)); }
        public static int RewardGold(int slot)
        {
            float g = Tpl(slot).gold * (1f + G.level * 0.04f);
            if (slot < 3) g *= 1f + Mathf.Min(7, G.Stat("dq_streak")) * 0.05f;
            return Difficulty.Gold(Mathf.RoundToInt(g));
        }
        public static int RewardExp(int slot) { return Mathf.RoundToInt(Tpl(slot).gold * 0.6f * (1f + G.level * 0.05f)); }

        public static int Claimable() { Ensure(); int n = 0; for (int i = 0; i < 6; i++) if (Done(i) && !Claimed(i)) n++; return n; }

        public static bool Claim(int slot)
        {
            Ensure();
            var gs = G; if (gs == null || !Done(slot) || Claimed(slot)) return false;
            gs.stats["dq_c" + slot] = 1;
            int gold = RewardGold(slot), exp = RewardExp(slot);
            gs.gold += gold; if (gs.level >= 5 && gs.level < GameState.LevelCap) gs.AddExp(exp);
            string msg = "Misi selesai: +" + gold + "g" + (exp > 0 && gs.level >= 5 ? ", +" + exp + " EXP" : "");
            // bonus melengkapi ketiga misi
            int first = slot < 3 ? 0 : 3;
            if (Claimed(first) && Claimed(first + 1) && Claimed(first + 2))
            {
                if (first == 0)
                {
                    int streak = gs.Stat("dq_lastfull") == Mathf.Max(1, GameCalendar.Day) - 1 ? gs.Stat("dq_streak") + 1 : 1;
                    gs.stats["dq_streak"] = streak; gs.stats["dq_lastfull"] = Mathf.Max(1, GameCalendar.Day);
                    int bonus = Difficulty.Gold(100 + gs.level * 4);
                    gs.gold += bonus; gs.AddItem("potion", 1);
                    msg += "\nSEMUA HARIAN LENGKAP! +" + bonus + "g, +1 Ramuan. Streak " + streak + " hari (+" + Mathf.Min(7, streak) * 5 + "% hadiah).";
                    gs.AddStat("dq_full_total");
                }
                else
                {
                    gs.skillPoints += 1; gs.AddItem("potion", 3);
                    int bonus = Difficulty.Gold(500 + gs.level * 15); gs.gold += bonus;
                    string att = gs.GiveRandomAttachment(1);
                    msg += "\nSEMUA MINGGUAN LENGKAP! +" + bonus + "g, +1 SP, +3 Ramuan" + (string.IsNullOrEmpty(att) || ItemDB.Get(att) == null ? "" : ", attachment: " + ItemDB.Get(att).name) + ".";
                    gs.AddStat("wk_full_total");
                }
            }
            Audio.Sfx("claim");
            if (TownUI.I != null) TownUI.I.Toast(msg);
            SaveSystem.RequestSave();
            return true;
        }

        public static void ClaimAll() { for (int i = 0; i < 6; i++) Claim(i); }

        // ---------------- UI ----------------
        public static void Open(System.Action back, bool canClaim = true)
        {
            var gs = G; if (gs == null) return;
            Ensure();
            TownUI.I.Open("Misi Harian & Mingguan",
                () =>
                {
                    int streak = gs.Stat("dq_streak");
                    int dleft = 7 - (Mathf.Max(1, GameCalendar.Day) - 1) % 7;
                    return "Harian ganti tiap hari game, mingguan tiap 7 hari (sisa " + dleft + " hari).\nStreak harian: " + streak + " hari (+" + Mathf.Min(7, streak) * 5 + "% hadiah harian)." +
                           (canClaim ? "" : "\nKlaim hadiah di Resepsionis Guild.");
                },
                () =>
                {
                    var l = new List<UIEntry>();
                    for (int i = 0; i < 6; i++)
                    {
                        int s = i;
                        string head = s == 0 ? "-- HARIAN --\n" : s == 3 ? "-- MINGGUAN --\n" : "";
                        string st = Claimed(s) ? "[DIKLAIM] " : Done(s) ? "[SELESAI - klaim] " : "";
                        l.Add(new UIEntry(head + st + Text(s) + "   " + Progress(s) + "/" + TargetOf(s) + "   (" + RewardGold(s) + "g, " + RewardExp(s) + " EXP)", () => { if (canClaim) Claim(s); Open(back, canClaim); }, canClaim && Done(s) && !Claimed(s)));
                    }
                    if (canClaim && Claimable() > 1) l.Add(new UIEntry("Klaim semua yang selesai", () => { ClaimAll(); Open(back, canClaim); }));
                    if (back != null) l.Add(new UIEntry("< Kembali", back));
                    return l;
                });
        }

        // ---------------- pemantau ----------------
        class Host : MonoBehaviour
        {
            float t;
            void Update()
            {
                t += Time.unscaledDeltaTime; if (t < 1f) return; t = 0f;
                var gs = G; if (gs == null || !SaveSystem.Ready) return;
                Ensure();
                for (int i = 0; i < 6; i++)
                    if (Done(i) && gs.Stat("dq_n" + i) == 0)
                    {
                        gs.stats["dq_n" + i] = 1;
                        if (TownUI.I != null) TownUI.I.Toast((i < 3 ? "Misi harian" : "Misi mingguan") + " selesai: " + Text(i) + ". Klaim di Resepsionis Guild.");
                    }
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { var go = new GameObject("DailyQuestsHost"); Object.DontDestroyOnLoad(go); go.AddComponent<Host>(); }
    }
}
