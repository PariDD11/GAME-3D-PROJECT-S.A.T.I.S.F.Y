using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HunterGuild
{
    /// <summary>
    /// Pos Latihan Terukur: tekan E, pilih "Mulai", lalu pukul / tembak / sihir boneka latihan di sekitar pos selama 30 detik.
    /// Skor, DPS, jumlah pukulan, dan kombo dihitung; hasilnya diberi peringkat E..S (relatif terhadap level dan ATK-mu).
    /// Hadiah EXP/gold diberikan sekali untuk tiap peringkat per jenis pos. Selama sesi, stamina dan mana tak habis.
    /// Pos juga menyediakan pemulihan gratis dan papan rekor 3D (TextMesh bernama "Skor" bila ada di anaknya).
    /// kind: "sword" | "archery" | "magic" | "guild"
    /// </summary>
    public class TrainingStation : Interactable
    {
        public string kind = "sword";
        public string title = "Latihan Terukur";
        public float range = 26f;          // jarak maks boneka dari pos yang dihitung
        public float duration = 30f;

        static readonly string[] Ranks = { "E", "D", "C", "B", "A", "S" };
        static readonly float[] RankAt = { 0f, 0.45f, 0.8f, 1.15f, 1.6f, 2.2f };          // rasio skor/detik terhadap "par"
        static readonly int[] RewardExp = { 0, 20, 50, 100, 200, 400 };
        static readonly int[] RewardGold = { 0, 0, 30, 80, 200, 500 };

        // sesi aktif (satu pada satu waktu)
        static TrainingStation active;
        static float startT, endT, lastHitT, score, lastReport;
        static int hits, combo, maxCombo;
        static GameObject hud; static Text hudTxt; static RectTransform hudFill;

        TextMesh board;

        void Awake()
        {
            if (string.IsNullOrEmpty(prompt) || prompt == "Berinteraksi") prompt = "Latihan Terukur";
            foreach (var tm in GetComponentsInChildren<TextMesh>(true)) if (tm.name == "Skor") board = tm;
            RefreshBoard();
        }

        public override void Interact()
        {
            if (active == this) { Finish(false); return; }
            OpenMenu();
        }

        string KindName { get { return kind == "archery" ? "Memanah" : kind == "magic" ? "Sihir" : kind == "guild" ? "Arena Guild" : "Pedang"; } }
        string BestKey { get { return "train_best_" + kind; } }

        /// <summary>Skor per detik yang dianggap "setara" kemampuanmu saat ini (patokan peringkat).</summary>
        static float Par()
        {
            var gs = GameState.I;
            return 8f + 2.4f * gs.level + 0.7f * gs.TotalAtk;
        }

        static int RankIndex(float score, float dur)
        {
            float r = (score / Mathf.Max(1f, dur)) / Mathf.Max(1f, Par());
            int idx = 0;
            for (int i = 0; i < RankAt.Length; i++) if (r >= RankAt[i]) idx = i;
            return idx;
        }

        void RefreshBoard()
        {
            if (board == null) return;
            var gs = GameState.I;
            int best = gs != null ? gs.Stat(BestKey) : 0;
            board.text = "PAPAN SKOR - " + KindName.ToUpper() + "\n" + (best > 0 ? "Rekor: " + best : "Belum ada rekor") + "\nTekan E untuk mulai";
        }

        // ---------------------------------------------------------------- menu
        void OpenMenu()
        {
            var gs = GameState.I;
            TownUI.I.Open(title + " - " + KindName,
                () => Rules() + "\n\nRekor skor: " + gs.Stat(BestKey) + "   |   Percobaan: " + gs.Stat("train_runs_" + kind) +
                      "\nPatokan peringkat (skor/detik): D " + Mathf.RoundToInt(Par() * RankAt[1]) + "   C " + Mathf.RoundToInt(Par() * RankAt[2]) + "   B " + Mathf.RoundToInt(Par() * RankAt[3]) +
                      "   A " + Mathf.RoundToInt(Par() * RankAt[4]) + "   S " + Mathf.RoundToInt(Par() * RankAt[5]),
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Mulai Latihan (" + Mathf.RoundToInt(duration) + " detik)", () => { TownUI.I.Close(); Begin(); }, PlayerCombat.I != null));
                    l.Add(new UIEntry("Pulihkan HP, stamina, dan mana (gratis)", () => { Restore(true); TownUI.I.Toast("Tubuhmu terasa segar kembali."); }, PlayerCombat.I != null));
                    l.Add(new UIEntry("Hadiah peringkat\n" + RewardText(gs), null, false));
                    l.Add(new UIEntry("Tutup", () => TownUI.I.Close()));
                    return l;
                });
        }

        string Rules()
        {
            switch (kind)
            {
                case "archery": return "Berdirilah di belakang garis tembak. Panah ke boneka di depan sasaran: makin jauh targetnya, makin besar skornya.";
                case "magic": return "Rapalkan sihirmu ke boneka-boneka di busur depan lingkaran. Jarak jauh memberi skor lebih besar. Mana tak habis selama sesi.";
                case "guild": return "Pukul boneka di area latihan Guild. Kombo beruntun (jeda < 1,2 dtk) dicatat sebagai kombo terbaik.";
                default: return "Pukul boneka jerami di lapangan. Kombo beruntun (jeda < 1,2 dtk) dicatat sebagai kombo terbaik. Stamina tak habis selama sesi.";
            }
        }

        string RewardText(GameState gs)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 1; i < Ranks.Length; i++)
            {
                bool got = gs.flags.Contains("train_" + kind + "_" + Ranks[i]);
                sb.Append(got ? "[x] " : "[ ] ").Append(Ranks[i]).Append(": +").Append(RewardExp[i]).Append(" EXP");
                if (RewardGold[i] > 0) sb.Append(", +").Append(RewardGold[i]).Append("g");
                sb.Append(i < Ranks.Length - 1 ? "    " : "");
                if (i == 3) sb.Append("\n");
            }
            return sb.ToString();
        }

        static void Restore(bool hp)
        {
            var pc = PlayerCombat.I; if (pc == null || pc.IsDead) return;
            if (hp) pc.Heal(pc.MaxHp);
            pc.stamina = pc.MaxStamina;
            pc.RestoreMana(pc.MaxMana);
        }

        // ---------------------------------------------------------------- sesi
        void Begin()
        {
            if (PlayerCombat.I == null) { GameState.Say("Kamu belum bisa bertarung di sini."); return; }
            active = this;
            startT = Time.time; endT = startT + duration; lastHitT = -99f; lastReport = 0f;
            score = 0f; hits = 0; combo = 0; maxCombo = 0;
            Restore(false);
            Audio.Sfx("ui_open");
            GameState.Say("Latihan dimulai! " + Mathf.RoundToInt(duration) + " detik.");
            EnsureHud();
        }

        void Update()
        {
            if (active != this) return;
            var pc = PlayerCombat.I;
            if (pc == null) { Finish(true); return; }
            float rem = endT - Time.time;
            Restore(false);                                                   // sumber daya tak habis selama latihan
            if (Vector3.Distance(pc.transform.position, transform.position) > range + 10f) { GameState.Say("Terlalu jauh dari pos latihan."); Finish(true); return; }
            if (pc.IsDead) { Finish(true); return; }
            if (Time.time - lastHitT > 1.2f) combo = 0;
            if (rem <= 0f) { Finish(false); return; }
            UpdateHud(rem);
        }

        /// <summary>Dipanggil Monster.TakeDamage untuk boneka latihan.</summary>
        public static void OnDummyHit(Monster m, float dmg)
        {
            if (active == null || m == null || dmg <= 0f) return;
            if (Vector3.Distance(m.transform.position, active.transform.position) > active.range) return;
            float mult = 1f;
            var pc = PlayerCombat.I;
            if (active.kind != "sword" && active.kind != "guild" && pc != null)
                mult += Mathf.Clamp(Vector3.Distance(pc.transform.position, m.transform.position) - 3f, 0f, 20f) * 0.04f;
            score += dmg * mult;
            hits++;
            if (Time.time - lastHitT <= 1.2f) combo++; else combo = 1;
            lastHitT = Time.time;
            if (combo > maxCombo) maxCombo = combo;
        }

        void Finish(bool aborted)
        {
            if (active != this) return;
            active = null;
            DestroyHud();
            var gs = GameState.I;
            float dur = Mathf.Max(1f, Mathf.Min(duration, Time.time - startT));
            if (aborted || hits == 0)
            {
                GameState.Say(hits == 0 ? "Latihan selesai tanpa pukulan." : "Latihan dihentikan.");
                return;
            }
            int sc = Mathf.RoundToInt(score);
            int ri = RankIndex(score, duration);
            bool record = sc > gs.Stat(BestKey);
            if (record) gs.stats[BestKey] = sc;
            gs.AddStat("train_runs_" + kind);
            gs.AddStat("train_runs");
            // hadiah: sekali per peringkat per jenis pos (peringkat di bawah yang dicapai ikut dibayar bila belum diambil)
            int exp = 0, gold = 0; string got = "";
            for (int i = 1; i <= ri; i++)
            {
                string f = "train_" + kind + "_" + Ranks[i];
                if (gs.flags.Contains(f)) continue;
                gs.flags.Add(f); exp += RewardExp[i]; gold += RewardGold[i]; got += Ranks[i] + " ";
            }
            if (exp > 0) gs.AddExp(exp);
            if (gold > 0) gs.gold += gold;
            float dps = score / dur;
            string rank = Ranks[ri];
            string rewardLine = got != "" ? "Hadiah peringkat " + got.Trim() + ": +" + exp + " EXP" + (gold > 0 ? ", +" + gold + " gold" : "") : "Hadiah peringkat ini sudah pernah diambil.";
            Audio.Sfx("ui_open");
            RefreshBoard();
            var self = this;
            TownUI.I.Open("Hasil Latihan - " + KindName,
                () => "PERINGKAT  " + rank + (record ? "      REKOR BARU!" : "") +
                      "\n\nSkor  " + sc + "     (rekor " + gs.Stat(BestKey) + ")" +
                      "\nSkor / detik  " + dps.ToString("0.0") + "     (patokan C = " + Mathf.RoundToInt(Par() * RankAt[2]) + ")" +
                      "\nPukulan  " + hits + "     Kombo terbaik  " + maxCombo + "\n\n" + rewardLine,
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Ulangi", () => { TownUI.I.Close(); self.Begin(); }));
                    l.Add(new UIEntry("Tutup", () => TownUI.I.Close()));
                    return l;
                });
        }

        // ---------------------------------------------------------------- HUD sesi
        static void EnsureHud()
        {
            if (hud != null || TownUI.I == null || TownUI.I.Root == null) return;
            var fr = UiStyle.Frame("TrainHud", TownUI.I.Root);
            hud = fr.gameObject;
            var rt = fr.rectTransform;
            UIKit.Place(rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(560f, 128f));
            hudTxt = UiStyle.Label(fr.transform, "T", 24, TextAnchor.UpperCenter, UiStyle.Cream, 20, 12, 520, 70);
            Text bl;
            hudFill = UiStyle.Bar(fr.transform, 24, 92, 512, 20, UiStyle.Gold, out bl, 14);
            bl.text = "";
        }

        static void UpdateHud(float rem)
        {
            EnsureHud();
            if (hud == null) return;
            float el = Mathf.Max(0.5f, Time.time - startT);
            hudTxt.text = "LATIHAN  " + Mathf.CeilToInt(rem) + " dtk\nSkor " + Mathf.RoundToInt(score) + "   Skor/dtk " + (score / el).ToString("0")
                          + "   Pukulan " + hits + "   Kombo " + combo + " (maks " + maxCombo + ")";
            hudFill.anchorMax = new Vector2(Mathf.Clamp01(rem / Mathf.Max(1f, active != null ? active.duration : 30f)), 1f);
        }

        static void DestroyHud()
        {
            if (hud != null) UnityEngine.Object.Destroy(hud);
            hud = null; hudTxt = null; hudFill = null;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (active == this) { active = null; DestroyHud(); }
        }
    }
}
