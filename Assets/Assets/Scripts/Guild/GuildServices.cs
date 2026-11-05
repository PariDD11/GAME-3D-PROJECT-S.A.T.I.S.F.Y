using UnityEngine;

namespace HunterGuild
{
    /// <summary>Logika layanan Guild yang dipakai bersama oleh menu, papan quest, dan poster. Tanpa UI.</summary>
    public static class GuildServices
    {
        public const string ClosedMessage = "Resepsionis Guild tutup (00.00-05.00). Kembali setelah pukul 05.00.";

        public static void Say(string msg) { if (TownUI.I != null) TownUI.I.Toast(msg); }

        /// <summary>Resepsionis buka pukul 05.00-24.00 (GDD 4.4).</summary>
        public static bool DeskOpen
        {
            get { var gs = GameState.I; return gs == null || gs.timeOfDay >= GuildConfig.DeskOpenHour; }
        }

        public static string FormatHours(float h)
        {
            int total = Mathf.CeilToInt(h * 60f);
            int hh = total / 60, mm = total % 60;
            if (hh <= 0) return mm + "m";
            return mm == 0 ? hh + "j" : hh + "j " + mm + "m";
        }

        // ---------------- Jual item drop (GDD 5.4) ----------------
        public static string Rarity(ItemDef d) { return ItemGrades.Name(d); }

        /// <summary>Harga tetap per kelangkaan x modifier reputasi x bonus event notice.</summary>
        public static int SellPrice(ItemDef d)
        {
            return Mathf.Max(1, Mathf.RoundToInt(d.sellPrice * GuildData.I.SellMultiplier(d.id) * SeasonEvents.SellMul * GovernorChain.SellMul * SeasonEvents.IngredientMul(d.id)));
        }

        // ---------------- Rank ----------------
        /// <summary>Poin kenaikan rank: Kumpulkan/Basmi = 1, Buru = 3.</summary>
        public static int QuestPoints(string type) { return type == "hunt" ? 3 : 1; }

        public static string RankHint(GameState gs)
        {
            int idx = GameState.RankIndex(gs.rank);
            if (idx >= 8) return "Rank tertinggi sudah tercapai.";
            if (idx <= 2)
            {
                int[] need = { 3, 11, 23 };   // sama dengan GameState.CheckRankUp
                int step = need[idx] - (idx > 0 ? need[idx - 1] : 0);
                return "Naik ke " + GameState.Ranks[idx + 1] + ": kerjakan quest senilai " + step + " poin (kamu punya " + Mathf.Max(0, gs.questsDone - (idx > 0 ? need[idx - 1] : 0)) + ").\n" +
                       "Contoh: " + step + " quest Kumpulkan Herbal, atau " + Mathf.CeilToInt(step / 3f) + " quest Buru (mis. Giant Boar). Kumpulkan/Basmi = 1 poin, Buru = 3 poin.";
            }
            return "Naik ke " + GameState.Ranks[idx + 1] + ": lewat Ujian Rank (tanya Guild Master).";
        }

        // ---------------- Quest ----------------
        /// <summary>String kosong = quest boleh diterima; selain itu alasannya.</summary>
        public static string CanAccept(GuildQuest q)
        {
            var gs = GameState.I;
            var gd = GuildData.I;
            if (!DeskOpen) return ClosedMessage;
            if (gs.activeQuest != null) return "Selesaikan atau batalkan quest aktif dulu.";
            int pr = GameState.RankIndex(gs.rank);
            if (q.RankIdx > pr) return "Rank " + q.rank + " belum terbuka (rankmu " + gs.rank + ").";
            if (gd.TierIndex == 0 && q.RankIdx >= pr && q.RankIdx > 0)
                return "Reputasi Distrusted: akses quest rank tertinggimu dibatasi.";
            float cd;
            if (gd.RankOnCooldown(q.rank, out cd)) return "Cooldown quest rank " + q.rank + ": " + FormatHours(cd) + ".";
            if (gs.gold < q.deposit) return "Deposit " + q.deposit + "g belum cukup.";
            if (q.towerFloor > 0 && gs.dungeonBossCleared < q.towerFloor - TowerDB.BossEvery)
                return "Jalan ke lantai " + q.towerFloor + " belum terbuka: kalahkan boss lantai " + (q.towerFloor - TowerDB.BossEvery) + " dulu.";
            return "";
        }

        public static bool Accept(GuildQuest q)
        {
            string why = CanAccept(q);
            if (why != "") { Say(why); return false; }
            var gs = GameState.I;
            var gd = GuildData.I;
            gs.gold -= q.deposit;
            gd.depositHeld = q.deposit;
            string oType, oTarget; int oCount;
            GuildQuestDB.Objective(q.id, out oType, out oTarget, out oCount);
            gs.activeQuest = new QuestInfo { id = q.id, title = q.title, description = q.desc, reward = q.reward, exp = q.exp, type = oType, targetId = oTarget, targetCount = oCount };
            gs.questProgress = 0;
            string tail = q.huntScene == "DungeonTower"
                ? (q.PartyRequired ? " Quest ini wajib party: rekrut partner dulu, lalu masuk Menara dari Kota Eldrath." : " Menuju Kota Eldrath lewat Gerbang Utara, lalu masuk Menara.")
                : q.PartyRequired
                ? " Quest ini wajib party: rekrut partner di poster resepsionis, lalu ke Gerbang Kota."
                : " Siapkan bekal lalu menuju Gerbang Kota.";
            gs.SyncQuestItems();
            Say("Quest diterima: " + q.title + "." + tail);
            Audio.Sfx("quest_accept");
            Audio.Voice("receptionist", "Quest diterima. Hati-hati di luar sana.");
            return true;
        }

        /// <summary>Batal sebelum selesai: deposit kembali, party dibubarkan tanpa cooldown.</summary>
        public static void CancelActive()
        {
            var gs = GameState.I;
            var gd = GuildData.I;
            if (gs.activeQuest == null || gs.activeQuest.completed) return;
            gs.gold += gd.depositHeld;
            gd.depositHeld = 0;
            gd.party.Clear();
            gd.postersVersion++;
            gs.activeQuest = null;
            gs.SyncQuestItems();
            Say("Quest dibatalkan.");
        }

        /// <summary>Dipanggil CityGate sebelum berangkat: quest rank B ke atas wajib punya partner.</summary>
        public static bool PartyReady(out string msg)
        {
            msg = "";
            var gs = GameState.I;
            if (gs == null || gs.activeQuest == null) return true;
            var gq = GuildQuestDB.Find(gs.activeQuest.id);
            if (gq == null || !gq.PartyRequired) return true;
            int n = GuildData.I.party.Count;
            if (n >= gq.minPartners) return true;
            msg = "Quest rank " + gq.rank + " wajib party (min " + gq.minPartners + " partner, kamu punya " + n + "). Rekrut di poster resepsionis Guild.";
            return false;
        }

        public static string PartyRequirementText()
        {
            var gs = GameState.I;
            var gd = GuildData.I;
            if (gs.activeQuest == null) return "Belum ada quest aktif. Ambil quest di Papan Quest.";
            var gq = GuildQuestDB.Find(gs.activeQuest.id);
            if (gq != null && gq.PartyRequired)
                return "Quest aktif wajib party: min " + gq.minPartners + " partner (kamu punya " + gd.party.Count + ").";
            return "Quest aktif bersifat solo. Partner tidak wajib.";
        }

        public static int PartnerCutTotal(int reward)
        {
            int cut = 0;
            foreach (var p in GuildData.I.party) cut += Mathf.RoundToInt(reward * p.hiredCost / 100f);
            return Mathf.Min(cut, reward);
        }

        /// <summary>Klaim reward: bagi hasil ke partner, deposit kembali, EXP, reputasi, rank, ujian khusus.</summary>
        public static void Claim()
        {
            var gs = GameState.I;
            var gd = GuildData.I;
            var q = gs.activeQuest;
            if (q == null || !q.completed) return;
            if (!DeskOpen) { Say(ClosedMessage); return; }
            var gq = GuildQuestDB.Find(q.id);

            int baseReward = Mathf.RoundToInt(q.reward * Mathf.Max(0.1f, 1f - 0.15f * q.faints));   // GDD 3.6: tiap pingsan -15%
            int cut = PartnerCutTotal(baseReward);
            int refund = gd.depositHeld;
            gd.depositHeld = 0;
            baseReward = Mathf.RoundToInt(baseReward * (1f + gs.AttachEffect("gold") / 100f));
            baseReward = Difficulty.Gold(baseReward);
            cut = PartnerCutTotal(baseReward);
            gs.gold += baseReward - cut + refund;

            int exp = Mathf.RoundToInt(q.exp * (1f + gs.AttachEffect("exp") / 100f));
            if (gs.expBuffQuests > 0) { exp = Mathf.RoundToInt(exp * 1.05f); gs.expBuffQuests--; }
            int lvBefore = gs.level;
            string oldRank = gs.rank;
            gs.AddExp(exp);
            gs.questsDone += QuestPoints(q.type);
            gs.mealBuff = "";        // buff makanan berlaku untuk satu quest
            int faintsNote = q.faints;
            gs.AddStat("qd_" + q.id);
            gs.AddStat("quests_n");
            gs.AddStat("qrank_" + (gq != null ? gq.rank : "G"));
            gs.activeQuest = null;
            gs.questProgress = 0;
            gs.CheckRankUp();
            // hadiah tambahan: attachment (peluang naik bersama rank quest)
            int qri = gq != null ? gq.RankIdx : 0;
            string attRew = Random.value < 0.25f + 0.07f * qri ? gs.GiveRandomAttachment(qri >= 5 ? 2 : (qri >= 2 ? 1 : 0)) : null;
            gs.SyncQuestItems();
            Audio.Sfx("quest_reward");
            Audio.Voice("receptionist", "Kerja bagus! Ini hadiahmu.");
            if (gs.rank != oldRank) Audio.Sfx("rankup");

            int qr = gq != null ? gq.RankIdx : 0;
            gd.AddReputation(1 + qr);
            gd.ReleaseParty(qr, true);
            gd.RefreshPosters();     // poster dirotasi setelah quest selesai (GDD 5.2)

            string examNote = "";
            if (gq != null && !string.IsNullOrEmpty(gq.examTarget)) examNote = " " + PassExam(gs, GameState.RankIndex(gq.examTarget));
            if (gs.rank != oldRank) gd.rankUpPending = true;

            string msg = "Reward: +" + (baseReward - cut) + " gold" + (faintsNote > 0 ? " (dipotong pingsan " + faintsNote + "x)" : "");
            if (cut > 0) msg += " (bagi hasil partner -" + cut + "g)";
            if (refund > 0) msg += ", deposit " + refund + "g kembali";
            msg += ", +" + Mathf.RoundToInt(exp * Difficulty.ExpMul) + " EXP.";
            if (gs.level > lvBefore) msg += " Level " + gs.level + "!";
            if (gs.rank != oldRank && examNote == "") msg += " Rank naik ke " + gs.rank + "!";
            Say(msg + examNote);
        }

        // ---------------- Ujian kenaikan rank (GDD 3.4) ----------------
        public static int NextRankIdx(GameState gs) { return GameState.RankIndex(gs.rank) + 1; }

        /// <summary>auto: G-F-E-D, ringan: D->C dan C->B, menengah: B->A dan A->S, dramatis: S->SS.</summary>
        public static string ExamKind(int target)
        {
            return target <= 3 ? "auto" : target <= 5 ? "ringan" : target <= 7 ? "menengah" : "dramatis";
        }

        public static string ExamDescription(int target)
        {
            switch (ExamKind(target))
            {
                case "ringan": return "Ujian ringan: kalahkan 1 mini-boss di Arena Guild, atau selesaikan 1 quest khusus dalam batas waktu.";
                case "menengah": return "Ujian menengah: battle di Arena dengan syarat tambahan, yaitu harus SOLO tanpa partner.";
                case "dramatis": return "Ujian dramatis: battle royale melawan hunter S-rank NPC, atau Dungeon Trial khusus rank SS.";
                default: return "Kenaikan rank ini otomatis lewat jumlah quest, tanpa ujian formal.";
            }
        }

        public static string ExamOpponent(int target)
        {
            switch (ExamKind(target))
            {
                case "ringan": return "Arena Warden (mini-boss)";
                case "menengah": return "Gladiator Elite (solo)";
                default: return "Battle Royale: 3 hunter S-rank";
            }
        }

        public static string Mark(bool ok) { return ok ? "[OK] " : "[X] "; }

        public static string ExamRequirementText(GameState gs, int target, out bool met)
        {
            var gd = GuildData.I;
            bool lv = gs.level >= GuildConfig.ExamMinLevel[target];
            bool qs = gs.questsDone >= GuildConfig.ExamMinQuests[target];
            bool rep = gd.TierIndex >= GuildConfig.ExamMinTier[target];
            met = lv && qs && rep;
            return Mark(lv) + "Level " + GuildConfig.ExamMinLevel[target] + "   " +
                   Mark(qs) + "Poin quest " + gs.questsDone + "/" + GuildConfig.ExamMinQuests[target] + "   " +
                   Mark(rep) + "Reputasi " + GuildConfig.TierNames[GuildConfig.ExamMinTier[target]];
        }

        public static bool ExamApproved(GameState gs, int target) { return gs.flags.Contains("exam_ok_" + target); }

        public static string PassExam(GameState gs, int target)
        {
            if (target <= GameState.RankIndex(gs.rank) || target > 8) return "";
            gs.rank = GameState.Ranks[target];
            gs.flags.Remove("exam_ok_" + target);
            GuildData.I.AddReputation(5);
            GuildData.I.rankUpPending = true;
            Audio.Sfx("rankup");
            Audio.Voice("referee", "Kamu lulus ujian!");
            return "Ujian lulus! Rank naik ke " + gs.rank + ".";
        }

        /// <summary>Quest ujian khusus (D->C, C->B ringan; S->SS dungeon trial). Masuk ke Papan Quest sebagai quest biasa.</summary>
        public static GuildQuest AddExamQuest(int target)
        {
            var gd = GuildData.I;
            string id = "x_exam_" + target;
            foreach (var q in gd.extraQuests) if (q.id == id) return q;

            GuildQuest eq;
            if (target >= 8)
                eq = GuildQuestDB.Make(id, "S", "Ujian SS: Dungeon Trial", "Dungeon trial khusus rank SS. Tidak ada jalan mundur.", "Dungeon Trial", "secret", 1500, 600, "2-3 jam");
            else if (target == 5)
                eq = GuildQuestDB.Make(id, "C", "Ujian Rank B: Quest Khusus", "Selesaikan target utama sebelum waktu habis.", "Reruntuhan Kuno", "ruins", 280, 140, "Batas waktu 25 menit");
            else
                eq = GuildQuestDB.Make(id, "D", "Ujian Rank C: Quest Khusus", "Selesaikan target utama sebelum waktu habis.", "Hutan Dalam", "deepforest", 150, 75, "Batas waktu 25 menit");
            eq.examTarget = GameState.Ranks[target];
            if (target < 8) eq.timeLimitMin = 25;
            gd.extraQuests.Add(eq);
            return eq;
        }
    }
}
