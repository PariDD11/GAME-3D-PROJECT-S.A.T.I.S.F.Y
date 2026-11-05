using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>Semua menu Guild berbasis panel TownUI. Dipanggil oleh komponen Interactable di interior (atau pintu Guild di kota sebagai fallback).</summary>
    public static class GuildMenus
    {
        static void Back(List<UIEntry> l, Action back, string label = "< Kembali")
        {
            if (back != null) l.Add(new UIEntry(label, back));
        }

        // ================= Resepsionis =================
        /// <param name="full">true = fallback di pintu kota tanpa interior: tampilkan juga jual bahan dan pelatih.</param>
        public static void OpenReception(bool full)
        {
            var gs = GameState.I;
            var gd = GuildData.I;
            if (!GuildServices.DeskOpen) { TownUI.I.Toast(GuildServices.ClosedMessage); return; }
            gd.EnsurePosters();
            TownUI.I.Open("Resepsionis Guild",
                () => "Rank " + gs.rank + "   Lv " + gs.level + "   Poin quest: " + gs.questsDone +
                      "\nReputasi: " + gd.TierName + "   Gold: " + gs.gold +
                      "\n" + GuildServices.RankHint(gs) +
                      "\nQuest: " + (gs.activeQuest == null ? "-" : gs.activeQuest.title + (gs.activeQuest.completed ? " (selesai)" : "")),
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Papan Quest >", () => { TownUI.I.Close(); QuestBoardUI.Get().Show(); }));
                    bool canClaim = gs.activeQuest != null && gs.activeQuest.completed;
                    string claimLabel = "Klaim reward (belum ada quest selesai)";
                    if (canClaim)
                    {
                        int cut = GuildServices.PartnerCutTotal(gs.activeQuest.reward);
                        claimLabel = "Klaim reward: " + gs.activeQuest.title + "  (+" + (gs.activeQuest.reward - cut) + "g" + (cut > 0 ? ", partner -" + cut + "g" : "") + ")";
                    }
                    l.Add(new UIEntry(claimLabel, GuildServices.Claim, canClaim));
                    l.Add(new UIEntry("Poster Rekrutmen Partner >  (party " + (gd.party.Count + 1) + "/" + (GuildConfig.MaxPartners + 1) + ")",
                        () => { TownUI.I.Close(); PartyRecruitUI.Get().Show(); }));
                    l.Add(new UIEntry("Misi Harian & Mingguan >" + (DailyQuests.Claimable() > 0 ? "  (" + DailyQuests.Claimable() + " siap klaim)" : ""), () => DailyQuests.Open(() => OpenReception(full))));
                    l.Add(new UIEntry("Papan Peringkat >", () => OpenRanking()));
                    l.Add(new UIEntry("Properti & Rumah >  (beli rumah, toko furnitur)", () => PropertyMenus.Open(() => OpenReception(full))));
                    l.Add(new UIEntry("Info Kenaikan Rank >", () => OpenRankInfo(() => OpenReception(full))));
                    if (full)
                    {
                        l.Add(new UIEntry("Jual bagian monster >", () => OpenSell(() => OpenReception(true))));
                        l.Add(new UIEntry("Pelatih Guild >", () => OpenTrainer(() => OpenReception(true))));
                    }
                    return l;
                });
        }

        // ================= Info rank =================
        public static void OpenRankInfo(Action back)
        {
            var gs = GameState.I;
            TownUI.I.Open("Kenaikan Rank",
                () =>
                {
                    int target = GuildServices.NextRankIdx(gs);
                    if (target > 8) return "Kamu sudah mencapai rank tertinggi, Hunter rank SS.";
                    string s = "Rank " + gs.rank + " > " + GameState.Ranks[target] + "\n" + GuildServices.ExamDescription(target);
                    if (GuildServices.ExamKind(target) != "auto")
                    {
                        bool met;
                        s += "\n" + GuildServices.ExamRequirementText(gs, target, out met);
                    }
                    else s += "\n" + GuildServices.RankHint(gs);
                    return s;
                },
                () =>
                {
                    var l = new List<UIEntry>();
                    Back(l, back);
                    return l;
                });
        }

        // ================= Penilai bahan (jual item drop) =================
        public static void OpenSell(Action back)
        {
            var gs = GameState.I;
            var gd = GuildData.I;
            gd.EnsureNotices();
            TownUI.I.Open("Penilai Bahan Guild",
                () =>
                {
                    string s = "Gold: " + gs.gold + "\nHarga tetap per kelangkaan. Reputasi " + gd.TierName + ": " +
                               (gd.SellMod >= 0f ? "+" : "") + Mathf.RoundToInt(gd.SellMod * 100f) + "%";
                    foreach (var n in gd.notices)
                        if (n.kind == "price" && n.expiresAt > GuildData.Now) s += "\nEvent: " + n.BodyNow;
                    return s;
                },
                () =>
                {
                    var l = new List<UIEntry>();
                    int total = 0, shown = 0;
                    foreach (var kv in new List<KeyValuePair<string, int>>(gs.inventory))
                    {
                        var def = ItemDB.Get(kv.Key);
                        if (def == null || def.type != ItemType.Material || def.sellPrice <= 0) continue;
                        int price = GuildServices.SellPrice(def);
                        total += price * kv.Value;
                        if (shown >= 5) continue;
                        shown++;
                        string id = kv.Key;
                        bool boosted = gd.EventMultiplier(id) > 1f;
                        l.Add(new UIEntry("Jual 1x " + def.name + " [" + GuildServices.Rarity(def) + "]  " + price + "g" + (boosted ? " (event)" : "") + "   punya " + kv.Value,
                            () => { if (gs.RemoveItem(id, 1)) gs.gold += GuildServices.SellPrice(ItemDB.Get(id)); }));
                    }
                    if (l.Count == 0) l.Add(new UIEntry("(Tidak ada bahan untuk dijual)", null, false));
                    else l.Add(new UIEntry("Jual semua bahan  (+" + total + "g)", SellAll));
                    Back(l, back);
                    return l;
                });
        }

        static void SellAll()
        {
            var gs = GameState.I;
            int total = 0;
            foreach (var kv in new List<KeyValuePair<string, int>>(gs.inventory))
            {
                var def = ItemDB.Get(kv.Key);
                if (def == null || def.type != ItemType.Material || def.sellPrice <= 0) continue;
                total += GuildServices.SellPrice(def) * kv.Value;
                gs.inventory.Remove(kv.Key);
            }
            gs.gold += total;
            TownUI.I.Toast("Terjual semua bahan: +" + total + " gold.");
        }

        // ================= Pelatih Guild =================
        public static void OpenTrainer(Action back)
        {
            var gs = GameState.I;
            TownUI.I.Open("Pelatih Guild Harkon",
                () => "Skill Point: " + gs.skillPoints + "   Gold: " + gs.gold +
                      "\nLatihan lanjutan memberi +1 SP sekali per rank (sumber SP: latihan dan mentor, GDD 3.3)." +
                      "\nReset skill mengembalikan semua SP.",
                () =>
                {
                    var l = new List<UIEntry>();
                    int idx = GameState.RankIndex(gs.rank);
                    string flag = "train_" + gs.rank;
                    int price = 30 * (idx + 1);
                    bool done = gs.flags.Contains(flag);
                    l.Add(new UIEntry(done ? "Latihan lanjutan Rank " + gs.rank + " (sudah diambil)" : "Latihan lanjutan Rank " + gs.rank + ": +1 SP  - " + price + "g",
                        () => { if (gs.TrySpend(price)) { gs.flags.Add(flag); gs.skillPoints++; TownUI.I.Toast("Latihan selesai. +1 SP."); } },
                        !done && gs.jobChosen && gs.gold >= price));
                    l.Add(new UIEntry("Reset skill (" + gs.RespecCost + "g)", () => gs.RespecSkills(), gs.learnedSkills.Count > 0));
                    Back(l, back);
                    return l;
                });
        }

        // ================= Papan Pengumuman =================
        public static void OpenNotices()
        {
            var gd = GuildData.I;
            gd.EnsureNotices();
            TownUI.I.Open("Papan Pengumuman",
                () => "Rumor dan kabar sementara dari seluruh wilayah. Kabar berganti tiap hari.",
                () =>
                {
                    var l = new List<UIEntry>();
                    foreach (var n in gd.notices)
                    {
                        string tag = n.kind == "price" ? "[Harga] " : n.kind == "sighting" ? "[Misi] " : "[Kabar] ";
                        l.Add(new UIEntry(tag + n.title + "\n" + n.BodyNow, null));
                    }
                    l.Insert(0, new UIEntry("Kasus Investigasi >", () => Investigation.OpenBoard(() => OpenNotices())));
                    l.Insert(1, new UIEntry("Pasar Dagang (beli murah, jual mahal) >", () => TradeMarket.Open(() => OpenNotices())));
                    l.Insert(2, new UIEntry("Kerja Sampingan (gaji harian) >", () => OddJobs.Open(() => OpenNotices())));
                    if (l.Count == 3) l.Add(new UIEntry("(Belum ada pengumuman)", null, false));
                    return l;
                });
        }

        // ================= Ranking Hunter =================

        public static void OpenRanking(string board = "rank")
        {
            LeaderboardUI.Open(board);
        }

        // ================= Guild Master =================
        static int Arc(int rankIdx) { return rankIdx <= 3 ? 1 : rankIdx <= 5 ? 2 : rankIdx <= 7 ? 3 : 4; }

        static readonly string[][] ArcLines =
        {
            new[] {
                "Ashwatch berdiri di ujung dunia untuk menjaga sebuah retakan tua tetap tertutup. Itulah alasan Guild ini ada, dan alasan kau ada di sini.",
                "Quest kecil hari ini adalah latihan hidup dan mati esok. Jangan remehkan serigala muda.",
                "Dungeon baru muncul lebih sering belakangan ini. Guild belum bisa menjelaskan sebabnya." },
            new[] {
                "Reruntuhan di utara memancarkan cahaya ungu. Tiga tim survey belum kembali.",
                "Jika kau melihat batu hitam berdenyut di dalam dungeon, jangan sentuh. Laporkan langsung padaku.",
                "Ada kultus pemuja Lucifer bernama Hollow Circle. Gubernur menuduh Guild, aku menuduh Gubernur. Tanyakan soal politik Ashwatch padaku kalau kau berani." },
            new[] {
                "Kau sudah cukup tinggi untuk mendengar ini: hasil survei dungeon selalu lewat mejaku. Percayalah pada laporan Guild.",
                "Beberapa hunter gugur di dungeon yang kami nyatakan aman. Kami menyelidikinya. Sedang.",
                "Reputasi adalah perisai di Guild ini, Hunter. Jaga baik-baik." },
            new[] {
                "Dungeon SS adalah sumber dari semua dungeon lain. Yang dijaga di sana bukan sekadar monster.",
                "Ada keputusan yang hanya bisa kau buat sendiri di dalam sana." }
        };

        static string MasterGreeting(GameState gs, GuildData gd)
        {
            int arc = Arc(GameState.RankIndex(gs.rank));
            string[] lines = ArcLines[arc - 1];
            if (gd.lastGreetedRank == "")
            {
                gd.lastGreetedRank = gs.rank;
                LoreDB.Unlock("prolog", false);
                return "Selamat datang di Guild Petualang. Aku Orwen Valk, pemimpin cabang ini. " + lines[0];
            }
            if (gd.lastGreetedRank != gs.rank)
            {
                gd.lastGreetedRank = gs.rank;
                gd.rankUpPending = false;
                return "Rank " + gs.rank + ". Kenaikan yang pantas, Hunter. " + lines[0];
            }
            return "Ada yang bisa kubantu, Hunter?";
        }

        public static void OpenMaster()
        {
            var gs = GameState.I;
            var gd = GuildData.I;
            string greet = MasterGreeting(gs, gd);
            TownUI.I.Open("Guild Master Orwen Valk",
                () => greet,
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Dengar kabar Guild >", () => OpenMasterTalk(0)));
                    l.Add(new UIEntry("Tanya kisah Guild dan Ashwatch >", () => LoreCodex.OpenMasterLore(OpenMaster)));
                    l.Add(new UIEntry("Politik Ashwatch: Gubernur vs Guild >", () => GovernorChain.Open(OpenMaster)));
                    l.Add(new UIEntry("Ujian Kenaikan Rank >", OpenExamInfo));
                    return l;
                });
        }

        static void OpenMasterTalk(int i)
        {
            var gs = GameState.I;
            string[] lines = ArcLines[Arc(GameState.RankIndex(gs.rank)) - 1];
            TownUI.I.Open("Guild Master Orwen Valk",
                () => lines[i % lines.Length],
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Lanjut >", () => OpenMasterTalk(i + 1)));
                    l.Add(new UIEntry("< Kembali", OpenMaster));
                    return l;
                });
        }

        static void OpenExamInfo()
        {
            var gs = GameState.I;
            TownUI.I.Open("Ujian Kenaikan Rank",
                () =>
                {
                    int target = GuildServices.NextRankIdx(gs);
                    if (target > 8) return "Tidak ada rank di atas SS. Jalan terakhir adalah menjadi pemimpin Guild.";
                    bool met;
                    string s = "Rank " + gs.rank + " > " + GameState.Ranks[target] + "\n" + GuildServices.ExamDescription(target);
                    if (GuildServices.ExamKind(target) != "auto") s += "\n" + GuildServices.ExamRequirementText(gs, target, out met);
                    return s;
                },
                () =>
                {
                    var l = new List<UIEntry>();
                    int target = GuildServices.NextRankIdx(gs);
                    if (target <= 8 && GuildServices.ExamKind(target) != "auto")
                    {
                        bool met;
                        GuildServices.ExamRequirementText(gs, target, out met);
                        bool ok = GuildServices.ExamApproved(gs, target);
                        l.Add(new UIEntry(ok ? "Ujian disetujui. Temui Pengawas Ujian di Arena Guild."
                                             : "Ajukan ujian ke Rank " + GameState.Ranks[target],
                            () => { gs.flags.Add("exam_ok_" + target); TownUI.I.Toast("Ujian disetujui. Temui Pengawas Ujian di Arena Guild."); },
                            met && !ok));
                    }
                    l.Add(new UIEntry("< Kembali", OpenMaster));
                    return l;
                });
        }

        // ================= Pengawas Ujian (Arena) =================
        public static void OpenExamReferee()
        {
            var gs = GameState.I;
            var gd = GuildData.I;
            TownUI.I.Open("Pengawas Ujian Vesna",
                () =>
                {
                    int target = GuildServices.NextRankIdx(gs);
                    if (target > 8) return "Tidak ada ujian lagi untukmu, Hunter SS.";
                    if (GuildServices.ExamKind(target) == "auto") return "Rank ini naik otomatis lewat quest. Arena ini untuk ujian rank C ke atas.";
                    if (!GuildServices.ExamApproved(gs, target)) return "Kamu belum punya persetujuan ujian untuk Rank " + GameState.Ranks[target] + ". Temui Guild Master dulu.";
                    return "Ujian ke Rank " + GameState.Ranks[target] + " disetujui.\n" + GuildServices.ExamDescription(target);
                },
                () =>
                {
                    var l = new List<UIEntry>();
                    int target = GuildServices.NextRankIdx(gs);
                    if (target > 8 || GuildServices.ExamKind(target) == "auto" || !GuildServices.ExamApproved(gs, target)) return l;
                    string kind = GuildServices.ExamKind(target);
                    l.Add(new UIEntry("Mulai ujian arena: " + GuildServices.ExamOpponent(target), () => StartArenaExam(target)));
                    if (kind == "ringan" || kind == "dramatis")
                    {
                        string label = kind == "ringan" ? "Ambil quest ujian khusus (berbatas waktu)" : "Ambil Dungeon Trial SS";
                        l.Add(new UIEntry(label, () =>
                        {
                            GuildServices.AddExamQuest(target);
                            TownUI.I.Toast("Quest ujian ada di Papan Quest Guild.");
                        }));
                    }
                    return l;
                });
        }

        static void StartArenaExam(int target)
        {
            var gs = GameState.I;
            var gd = GuildData.I;
            if (GuildServices.ExamKind(target) == "menengah" && gd.party.Count > 0)
            {
                TownUI.I.Toast("Ujian ini wajib SOLO. Lepaskan partnermu dulu.");
                return;
            }
            if (Application.CanStreamedLevelBeLoaded(GuildConfig.ArenaScene))
            {
                gs.flags.Add("exam_running_" + target);
                TownUI.I.Close();
                SceneManager.LoadScene(GuildConfig.ArenaScene);
                return;
            }
            TownUI.I.Open("Arena Belum Siap",
                () => "Scene '" + GuildConfig.ArenaScene + "' belum dibuat.\nJalankan menu Hunter Guild > Build Arena Ujian, lalu coba lagi.",
                () =>
                {
                    var l = new List<UIEntry>();
                    if (GuildConfig.DebugAllowExamSkip)
                        l.Add(new UIEntry("[DEBUG] Anggap ujian lulus", () =>
                        {
                            string msg = GuildServices.PassExam(gs, target);
                            TownUI.I.Close();
                            TownUI.I.Toast(msg);
                        }));
                    l.Add(new UIEntry("< Kembali", OpenExamReferee));
                    return l;
                });
        }
    }
}
