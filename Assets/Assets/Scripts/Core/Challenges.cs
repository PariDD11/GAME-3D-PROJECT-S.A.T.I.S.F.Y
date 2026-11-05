using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>v35z102n32: Tantangan Dunia (aturan tambahan yang bisa dinyalakan/dimatikan kapan saja; menambah hadiah EXP &amp; gold selama aktif),
    /// Boss Tanpa Goresan (kalahkan boss tanpa terkena damage sedikit pun), dan hadiah khusus tiap Putaran Baru (NG+).
    /// Status di GameState.stats (ch_{id}, flawless_boss, ng_reward_n) sehingga ikut save. Angka tebakan awal.</summary>
    public static class Challenges
    {
        public class Rule { public string id, name, desc; public float reward; }
        public static readonly Rule[] Rules =
        {
            new Rule { id = "nopot", name = "Tanpa Ramuan", desc = "Ramuan HP dan Mana tidak bisa dipakai.", reward = 0.20f },
            new Rule { id = "glass", name = "Meriam Kaca", desc = "Damage yang kamu terima x2, damage yang kamu beri x1.5.", reward = 0.25f },
            new Rule { id = "elite", name = "Dunia Elit", desc = "Semua monster: HP x1.3, serangan x1.2.", reward = 0.30f },
        };

        public static bool Active(string id) { return GameState.I != null && GameState.I.Stat("ch_" + id) == 1; }
        public static int ActiveCount { get { int n = 0; foreach (var r in Rules) if (Active(r.id)) n++; return n; } }
        public static float RewardMul { get { float m = 1f; foreach (var r in Rules) if (Active(r.id)) m += r.reward; return m; } }
        public static float TakenMul { get { return Active("glass") ? 2f : 1f; } }
        public static float DealtMul { get { return Active("glass") ? 1.5f : 1f; } }
        public static float EliteHp { get { return Active("elite") ? 1.3f : 1f; } }
        public static float EliteAtk { get { return Active("elite") ? 1.2f : 1f; } }

        public static bool BlockPotion()
        {
            if (!Active("nopot")) return false;
            if (TownUI.I != null) TownUI.I.Toast("Tantangan 'Tanpa Ramuan' aktif: ramuan tidak bisa dipakai.");
            return true;
        }

        // ---- boss tanpa goresan ----
        static float lastDamaged = -99f;
        public static void OnPlayerDamaged() { lastDamaged = Time.time; }

        public static void OnBossKill(Monster m)
        {
            var gs = GameState.I; if (gs == null || m == null) return;
            var mc = m.GetComponent<MonCombat>();
            if (mc == null || mc.engagedAt < 0f || lastDamaged > mc.engagedAt) return;      // pernah terkena damage selama bertarung
            gs.AddStat("flawless_boss");
            int gold = Difficulty.Gold(250 + gs.level * 10);
            gs.gold += gold;
            if (TownUI.I != null) TownUI.I.Toast("TANPA GORESAN! " + m.Name + " dikalahkan tanpa terkena damage. +" + gold + "g (total " + gs.Stat("flawless_boss") + ")");
            if (gs.Stat("flawless_boss") >= 1) Titles.Unlock("flawless1");
            if (gs.Stat("flawless_boss") >= 10) Titles.Unlock("flawless10");
        }

        // ---- NG+ ----
        public static void OnNg(int round)
        {
            var gs = GameState.I; if (gs == null || round <= 0) return;
            int sp = 2, gold = 1500 * round, att = Mathf.Min(3, round);
            gs.skillPoints += sp; gs.gold += gold;
            for (int i = 0; i < att; i++) gs.GiveRandomAttachment(2);
            gs.AddStat("ng_reward_n");
            if (TownUI.I != null) TownUI.I.Toast("Hadiah Putaran Baru " + round + ": +" + sp + " SP, +" + gold + "g, " + att + " attachment tinggi.");
            if (round >= 1) Titles.Unlock("ng1");
            if (round >= 3) Titles.Unlock("ng3");
            SaveSystem.RequestSave();
        }

        public static void Open(System.Action back)
        {
            var gs = GameState.I; if (gs == null) return;
            TownUI.I.Open("Tantangan Dunia",
                () => "Aturan tambahan yang menaikkan hadiah EXP & gold (sekarang x" + RewardMul.ToString("0.00") + ") selama aktif. Bisa dinyalakan/dimatikan kapan saja.\n" +
                      "Boss tanpa goresan: " + gs.Stat("flawless_boss") + "     Putaran Baru: " + Difficulty.NgLevel + (Difficulty.NgAvailable ? "  (siap: bersihkan Menara lantai 100 sudah tercapai)" : "  (butuh Menara lantai 100)") +
                      "\nBunuh boss tanpa kena damage satu kali pun = bonus gold dan gelar.",
                () =>
                {
                    var l = new List<UIEntry>();
                    foreach (var rr in Rules)
                    {
                        var r = rr;
                        l.Add(new UIEntry((Active(r.id) ? "[AKTIF] " : "[mati] ") + r.name + "  (+" + Mathf.RoundToInt(r.reward * 100) + "% hadiah)\n" + r.desc, () => { gs.stats["ch_" + r.id] = Active(r.id) ? 0 : 1; Open(back); }));
                    }
                    if (back != null) l.Add(new UIEntry("< Kembali", back));
                    return l;
                });
        }
    }
}
