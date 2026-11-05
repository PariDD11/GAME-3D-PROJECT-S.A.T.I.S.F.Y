using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Kabar di Papan Pengumuman (GDD 21.5). kind: price / sighting / rumor.</summary>
    [System.Serializable]
    public class Notice
    {
        public string id, kind, title, body, itemId = "", questId = "";
        public float pct, expiresAt;

        public string BodyNow
        {
            get
            {
                if (kind != "price") return body;
                var d = ItemDB.Get(itemId);
                string n = d != null ? d.name : itemId;
                return "Guild membeli " + n + " +" + Mathf.RoundToInt(pct * 100f) + "% selama " +
                       GuildServices.FormatHours(Mathf.Max(0.1f, expiresAt - GuildData.Now)) + " lagi.";
            }
        }
    }

    public class RankEntry
    {
        public string name, rank;
        public int quests;
        public int score;               // nilai papan khusus (lantai Menara, jumlah buruan wilayah)
        public string detail = "";
        public bool isPlayer, isRival;
    }

    /// <summary>
    /// Data Guild yang bertahan lintas scene (DontDestroyOnLoad, seperti GameState): reputasi, party, poster,
    /// cooldown, notice, ranking. Belum ada save/load di proyek ini; saat save dibuat, serialisasikan field publik di sini.
    /// </summary>
    public class GuildData : MonoBehaviour
    {
        static GuildData inst;

        public static GuildData I
        {
            get
            {
                if (inst == null) inst = new GameObject("GuildData").AddComponent<GuildData>();
                return inst;
            }
        }

        /// <summary>Jam in-game absolut sejak Hari 1 pukul 00.00.</summary>
        public static float Now
        {
            get { var gs = GameState.I; return gs == null ? 0f : (gs.day - 1) * 24f + gs.timeOfDay; }
        }

        void Awake()
        {
            if (inst != null && inst != this) { Destroy(gameObject); return; }
            inst = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy() { if (inst == this) inst = null; }

        static void Say(string msg) { if (TownUI.I != null) TownUI.I.Toast(msg); }

        // ================= Reputasi (GDD 11) =================
        public int reputation = GuildConfig.StartReputation;
        public bool rankUpPending;
        public string lastGreetedRank = "";
        public int depositHeld;

        public int TierIndex
        {
            get
            {
                int t = 0;
                for (int i = 0; i < GuildConfig.TierMinPoints.Length; i++)
                    if (reputation >= GuildConfig.TierMinPoints[i]) t = i;
                return t;
            }
        }

        public string TierName { get { return GuildConfig.TierNames[TierIndex]; } }
        public float SellMod { get { return GuildConfig.TierSellMod[TierIndex]; } }

        public void AddReputation(int n) { reputation += n; }

        // ================= Cooldown rank (GDD 5.3) =================
        public readonly Dictionary<string, float> rankCooldown = new Dictionary<string, float>();

        public bool RankOnCooldown(string rank, out float hoursLeft)
        {
            hoursLeft = 0f;
            float until;
            if (!rankCooldown.TryGetValue(rank, out until)) return false;
            hoursLeft = until - Now;
            if (hoursLeft <= 0f) { rankCooldown.Remove(rank); hoursLeft = 0f; return false; }
            return true;
        }

        /// <summary>Panggil dari scene hunt saat quest gagal: reputasi turun, deposit hangus, cooldown rank, partner ikut cooldown.</summary>
        public string FailActiveQuest()
        {
            var gs = GameState.I;
            if (gs == null || gs.activeQuest == null) return "";
            var q = GuildQuestDB.Find(gs.activeQuest.id);
            gs.activeQuest = null;
            depositHeld = 0;
            ReleaseParty(q != null ? q.RankIdx : 0, false);
            if (q != null)
            {
                AddReputation(-q.PenaltyRep);
                if (q.PenaltyCooldownHours > 0f) rankCooldown[q.rank] = Now + q.PenaltyCooldownHours;
            }
            Audio.Sfx("quest_fail");
            string msg = "Quest gagal." + (q != null ? " Penalti: " + q.PenaltyText() + "." : "");
            Say(msg);
            return msg;
        }

        // ================= Party & partner (GDD 5.2) =================
        public readonly List<Partner> party = new List<Partner>();
        public Partner favorite;                                     // Slot Favorit
        public readonly Dictionary<string, int> hired = new Dictionary<string, int>();
        public readonly Dictionary<string, float> partnerCooldown = new Dictionary<string, float>();

        public Partner[] posters = new Partner[GuildConfig.PosterCount];
        public bool exclusiveLocked = true;
        public float postersAt = -999f;
        public int postersVersion;      // naik tiap ada perubahan; PosterWall membandingkan nilai ini
        public int refreshCounter;

        public int HireCount(string key) { int n; return hired.TryGetValue(key, out n) ? n : 0; }
        public int FamiliarityLevel(string key) { return Mathf.Min(5, HireCount(key)); }
        public int FamiliarityBonusPct(string key) { return FamiliarityLevel(key) * 2; }   // +2% damage/heal per level, maks +10%

        public float PartnerCooldownLeft(string key)
        {
            float until;
            if (!partnerCooldown.TryGetValue(key, out until)) return 0f;
            float left = until - Now;
            return left > 0f ? left : 0f;
        }

        public bool InParty(Partner p) { return party.Exists(x => x.key == p.key); }

        /// <summary>Partner yang direkrut saat reputasi Distrusted lebih mahal (+10 poin persen): "partner sulit direkrut".</summary>
        public int CostFor(Partner p) { return p.costPct + (TierIndex == 0 ? 10 : 0); }

        /// <summary>Rank partner A ke atas butuh reputasi tertentu (GDD 5.2: "partner rank tinggi hanya jika reputasi cukup").</summary>
        public static int RequiredTier(int partnerRankIdx) { return partnerRankIdx >= 8 ? 4 : partnerRankIdx >= 7 ? 3 : partnerRankIdx >= 6 ? 2 : 0; }

        /// <summary>Cooldown partner setelah quest: G-D singkat, C-B menengah, A ke atas panjang (GDD 5.2).</summary>
        public static float PartnerCooldownHours(int questRankIdx)
        {
            if (questRankIdx <= 3) return 0.25f;
            if (questRankIdx == 4) return 1f;
            if (questRankIdx == 5) return 3f;
            return questRankIdx == 6 ? 6f : questRankIdx == 7 ? 9f : 12f;
        }

        /// <summary>String kosong = boleh direkrut; selain itu alasan penolakan.</summary>
        public string RecruitBlockReason(Partner p)
        {
            var gs = GameState.I;
            if (gs == null) return "Data permainan belum siap.";
            if (!gs.jobChosen && gs.level < 5) return "Rekrutmen dibuka setelah Level 5 / memilih job.";
            if (!GuildServices.DeskOpen) return "Resepsionis tutup.";
            if (InParty(p)) return "Sudah di party.";
            if (party.Count >= GuildConfig.MaxPartners) return "Slot party penuh.";
            float cd = PartnerCooldownLeft(p.key);
            if (cd > 0f) return "Cooldown " + GuildServices.FormatHours(cd) + ".";
            int need = RequiredTier(p.RankIdx);
            if (TierIndex < need) return "Butuh reputasi " + GuildConfig.TierNames[need] + ".";
            return "";
        }

        public bool Recruit(Partner p)
        {
            string why = RecruitBlockReason(p);
            if (why != "") { Say(why); return false; }
            p.hiredCost = CostFor(p);
            party.Add(p);
            postersVersion++;
            Say(p.name + " bergabung (" + p.hiredCost + "% reward).");
            return true;
        }

        public void Dismiss(Partner p)
        {
            party.RemoveAll(x => x.key == p.key);
            postersVersion++;
        }

        /// <summary>Quest selesai atau gagal: partner dilepas, kena cooldown, dan (bila sukses) naik familiarity.</summary>
        public void ReleaseParty(int questRankIdx, bool success)
        {
            float cd = PartnerCooldownHours(questRankIdx);
            foreach (var p in party)
            {
                if (success)
                {
                    hired[p.key] = HireCount(p.key) + 1;
                    if (HireCount(p.key) >= GuildConfig.FavoriteMinHires &&
                        (favorite == null || HireCount(p.key) > HireCount(favorite.key))) favorite = p;
                }
                partnerCooldown[p.key] = Now + cd;
            }
            party.Clear();
            postersVersion++;
        }

        // ================= Poster rekrutmen =================
        public float HoursToPosterRefresh { get { return Mathf.Max(0f, GuildConfig.PosterRefreshHours - (Now - postersAt)); } }

        public void EnsurePosters()
        {
            if (GameState.I == null) return;
            if (posters[0] == null || Now - postersAt >= GuildConfig.PosterRefreshHours) RefreshPosters();
        }

        static int RollRank(System.Random rng, int playerRank)
        {
            double d = rng.NextDouble();
            int off = d < 0.35 ? 0 : d < 0.60 ? -1 : d < 0.80 ? 1 : d < 0.90 ? -2 : d < 0.97 ? 2 : 3;
            return Mathf.Clamp(playerRank + off, 0, 8);
        }

        Partner MakePoster(System.Random rng, int rank, HashSet<string> used, double uniqueChance)
        {
            var cands = PartnerFactory.UniqueCandidates(rank, used);
            if (cands.Count > 0 && rng.NextDouble() < uniqueChance)
                return PartnerFactory.MakeUnique(cands[rng.Next(cands.Count)], rank);
            Partner g;
            int guard = 0;
            do { g = PartnerFactory.Generate(rng, rank, null); } while (used.Contains(g.key) && ++guard < 10);
            return g;
        }

        /// <summary>4 poster biasa di sekitar rank pemain + 1 slot reputasi eksklusif (Trusted Hunter ke atas).</summary>
        public void RefreshPosters()
        {
            var gs = GameState.I;
            if (gs == null) return;
            int pr = GameState.RankIndex(gs.rank);
            var rng = new System.Random(unchecked(refreshCounter * 7919 + gs.day * 131 + (int)(gs.timeOfDay * 10f)));
            refreshCounter++;

            var used = new HashSet<string>();
            foreach (var member in party) used.Add(member.key);

            int last = GuildConfig.PosterCount - 1;
            for (int i = 0; i < last; i++)
            {
                var np = MakePoster(rng, RollRank(rng, pr), used, 0.4);
                used.Add(np.key);
                posters[i] = np;
            }
            if (TierIndex >= 3)
            {
                posters[last] = MakePoster(rng, Mathf.Min(8, pr + 2), used, 0.85);
                exclusiveLocked = false;
            }
            else
            {
                posters[last] = null;
                exclusiveLocked = true;
            }
            postersAt = Now;
            postersVersion++;
        }

        // ================= Papan Pengumuman dinamis (GDD 21.5) =================
        public readonly List<Notice> notices = new List<Notice>();
        public readonly List<GuildQuest> extraQuests = new List<GuildQuest>();
        public int noticeDay = -1;

        static readonly string[] PriceItems = { "wolf_pelt", "boar_tusk", "monster_bone", "orc_tusk", "spider_silk", "herb" };

        class SightingDef
        {
            public int season = -1;
            public string key, rank, theme, area, title, notice, desc;
            public int reward, exp;
            public string time;
        }

        static readonly SightingDef[] Sightings =
        {
            new SightingDef { season = 0, key = "spring_boar", rank = "F", theme = "plains", area = "Ladang Utara", title = "Musim Semi: Hama Tunas", notice = "Hama menyerbu tunas muda di ladang saat musim semi.", desc = "Usir hama yang merusak tunas bunga menjelang Festival Bunga.", reward = 60, exp = 30, time = "8-10 menit" },
            new SightingDef { season = 1, key = "summer_lizard", rank = "D", theme = "deepforest", area = "Jalan Perbatasan", title = "Musim Panas: Serigala Kehausan", notice = "Kemarau membuat serigala liar turun ke jalan raya.", desc = "Serigala kelaparan menyerang kafilah. Guild membayar ekstra.", reward = 150, exp = 70, time = "10-15 menit" },
            new SightingDef { season = 2, key = "autumn_ogre", rank = "C", theme = "ruins", area = "Hutan Perbatasan", title = "Musim Gugur: Ogre Panen", notice = "Ogre mengintai gudang panen di musim gugur.", desc = "Lindungi hasil panen dari ogre yang turun gunung.", reward = 260, exp = 120, time = "15-20 menit" },
            new SightingDef { season = 3, key = "winter_wolf", rank = "E", theme = "deepforest", area = "Hutan Utara", title = "Musim Dingin: Serigala Salju", notice = "Serigala kelaparan mengendus desa saat salju turun.", desc = "Kawanan serigala mendekati desa. Halau sebelum menyerang ternak.", reward = 110, exp = 55, time = "10-12 menit" },
            new SightingDef { key = "boarherd", rank = "G", theme = "plains", area = "Ladang Utara", title = "Notice: Kawanan Babi Hutan", notice = "Kawanan babi hutan merusak ladang utara.", desc = "Petani menawarkan bonus bagi yang mengusir kawanan babi hutan.", reward = 40, exp = 20, time = "8-10 menit" },
            new SightingDef { key = "alpha", rank = "E", theme = "deepforest", area = "Hutan Utara", title = "Notice: Alpha Wolf", notice = "Waspada, Alpha Wolf terlihat di Hutan Utara.", desc = "Alpha Wolf terlihat memimpin kawanan. Guild membayar ekstra untuk kepalanya.", reward = 110, exp = 55, time = "20-25 menit" },
            new SightingDef { key = "spider", rank = "E", theme = "cave", area = "Gua Dangkal", title = "Notice: Sarang Laba-laba", notice = "Sarang Giant Spider baru ditemukan di gua dangkal.", desc = "Sarang baru berpotensi membanjiri jalur tambang. Segera dibersihkan.", reward = 100, exp = 50, time = "18-24 menit" },
            new SightingDef { key = "orcscout", rank = "D", theme = "deepforest", area = "Dekat Gerbang Timur", title = "Notice: Orc Pengintai", notice = "Orc pengintai terlihat dekat jalur timur.", desc = "Orc pengintai mencatat jadwal kafilah. Singkirkan sebelum menyerang.", reward = 195, exp = 98, time = "30-40 menit" },
            new SightingDef { key = "ogre", rank = "C", theme = "ruins", area = "Reruntuhan Kuno", title = "Notice: Ogre Mengamuk", notice = "Ogre mengamuk di sekitar reruntuhan kuno.", desc = "Ogre tampak gelisah seolah dipanggil sesuatu. Guild menawarkan bonus.", reward = 340, exp = 170, time = "45-60 menit" },
        };

        class RumorDef { public int minRank; public string title, body; }

        static readonly RumorDef[] Rumors =
        {
            new RumorDef { minRank = 0, title = "Pesan Resepsionis", body = "Laporkan quest selesai sebelum tengah malam. Meja tutup pukul 00.00 sampai 05.00." },
            new RumorDef { minRank = 0, title = "Kondisi jalan", body = "Hutan Lerai aman siang hari. Malam hari, serigala lebih berani." },
            new RumorDef { minRank = 0, title = "Peringatan survival", body = "Bawa bekal dan air. Hunter yang kehabisan makanan di lapangan tidak akan ditolong." },
            new RumorDef { minRank = 1, title = "Kabar pasar", body = "Pedagang di plaza mencari pemasok Herb segar." },
            new RumorDef { minRank = 2, title = "Rumor hutan dalam", body = "Beberapa hunter melihat Orc berbaris rapi. Orc tidak biasa berbaris." },
            new RumorDef { minRank = 3, title = "Cahaya di utara", body = "Cahaya ungu terlihat dari arah reruntuhan utara pada malam hari." },
            new RumorDef { minRank = 4, title = "Tim survey hilang", body = "Tim survey Guild belum kembali dari reruntuhan. Rank C ke atas diminta waspada." },
            new RumorDef { minRank = 5, title = "Hunter tak kembali", body = "Dua hunter rank B mengambil quest dungeon baru dan tidak pernah terlihat lagi." },
            new RumorDef { minRank = 6, title = "Pesan Guild Master", body = "Hasil survei dungeon hanya lewat meja Guild Master. Hunter dilarang menyelidiki sendiri." },
            new RumorDef { minRank = 7, title = "Menara berbisik", body = "Menara terkutuk bersuara pada malam bulan baru." },
            new RumorDef { minRank = 8, title = "Nyanyian naga", body = "Penjelajah SS bersumpah mendengar nyanyian dari kedalaman dungeon legendaris." },
        };

        int CountKind(string kind)
        {
            int n = 0;
            foreach (var x in notices) if (x.kind == kind) n++;
            return n;
        }

        bool HasNotice(string id)
        {
            foreach (var x in notices) if (x.id == id) return true;
            return false;
        }

        /// <summary>Panggil sebelum menampilkan papan. Notice kedaluwarsa dibuang; notice baru dibuat sekali per hari in-game.</summary>
        public void EnsureNotices()
        {
            var gs = GameState.I;
            if (gs == null) return;
            float now = Now;
            notices.RemoveAll(n => n.expiresAt <= now);
            extraQuests.RemoveAll(q => q.expiresAt >= 0f && q.expiresAt <= now &&
                !(gs.activeQuest != null && gs.activeQuest.id == q.id));
            if (noticeDay == gs.day) return;
            noticeDay = gs.day;

            var rng = new System.Random(gs.day * 7919 + 13);
            int pr = GameState.RankIndex(gs.rank);

            if (CountKind("price") < 2) AddPriceNotice(rng);
            if (CountKind("sighting") < 1) AddSighting(rng, pr);
            int guard = 0;
            while (notices.Count < 4 && guard++ < 20) AddRumor(rng, pr);
        }

        void AddPriceNotice(System.Random rng)
        {
            string item = PriceItems[rng.Next(PriceItems.Length)];
            var def = ItemDB.Get(item);
            if (def == null) return;
            foreach (var n in notices) if (n.kind == "price" && n.itemId == item) return;
            int pct = 20 + rng.Next(0, 4) * 10;                // +20..50%
            float hours = 24f + rng.Next(0, 3) * 12f;          // 24-48 jam
            notices.Add(new Notice
            {
                id = "p_" + item + "_" + noticeDay, kind = "price", itemId = item, pct = pct / 100f, expiresAt = Now + hours,
                title = "Harga " + def.name + " naik"
            });
        }

        void AddSighting(System.Random rng, int pr)
        {
            var pool = new List<SightingDef>();
            foreach (var s in Sightings)
            {
                int r = GameState.RankIndex(s.rank);
                if (s.season >= 0 && s.season != (int)GameCalendar.Current) continue;      // quest musiman
                if (r >= pr - 1 && r <= pr + 1) pool.Add(s);
            }
            if (pool.Count == 0) return;
            var pick = pool[rng.Next(pool.Count)];
            string qid = "n_" + pick.key + "_" + noticeDay;
            var q = GuildQuestDB.Make(qid, pick.rank, pick.title, pick.desc, pick.area, pick.theme, pick.reward, pick.exp, pick.time);
            q.fromNotice = true;
            q.expiresAt = Now + 30f;
            extraQuests.Add(q);
            notices.Add(new Notice
            {
                id = "s_" + pick.key + "_" + noticeDay, kind = "sighting", questId = qid, expiresAt = q.expiresAt,
                title = "Misi opsional tersedia", body = pick.notice + " Lihat Papan Quest."
            });
        }

        void AddRumor(System.Random rng, int pr)
        {
            var pool = new List<int>();
            for (int i = 0; i < Rumors.Length; i++)
                if (Rumors[i].minRank <= pr && !HasNotice("r_" + i)) pool.Add(i);
            if (pool.Count == 0) return;
            int idx = pool[rng.Next(pool.Count)];
            notices.Add(new Notice { id = "r_" + idx, kind = "rumor", title = Rumors[idx].title, body = Rumors[idx].body, expiresAt = Now + 24f });
        }

        /// <summary>Bonus harga beli Guild dari notice aktif (GDD 5.4: event butuh item spesifik).</summary>
        public float EventMultiplier(string itemId)
        {
            float m = 1f;
            foreach (var n in notices)
                if (n.kind == "price" && n.itemId == itemId && n.expiresAt > Now) m *= 1f + n.pct;
            return m;
        }

        public float SellMultiplier(string itemId) { return (1f + SellMod) * EventMultiplier(itemId); }

        // ================= Ranking Hunter lokal (GDD 21.6) =================
        class HunterRow { public string name, rank; public int baseQuests; public float perDay; }

        static readonly HunterRow[] Hunters =
        {
            new HunterRow { name = "Seraphine", rank = "SS", baseQuests = 610, perDay = 0.8f },
            new HunterRow { name = "Nyx", rank = "S", baseQuests = 305, perDay = 1.0f },
            new HunterRow { name = "Torvin", rank = "A", baseQuests = 240, perDay = 0.7f },
            new HunterRow { name = "Elysia", rank = "A", baseQuests = 205, perDay = 1.2f },
            new HunterRow { name = "Soren Coldwater", rank = "B", baseQuests = 96, perDay = 0.9f },
            new HunterRow { name = "Draven", rank = "B", baseQuests = 88, perDay = 0.4f },
            new HunterRow { name = "Bram Ironvale", rank = "C", baseQuests = 72, perDay = 0.8f },
            new HunterRow { name = "Kael", rank = "D", baseQuests = 31, perDay = 0.6f },
            new HunterRow { name = "Mira", rank = "D", baseQuests = 28, perDay = 0.5f },
            new HunterRow { name = "Tarin Larkspur", rank = "E", baseQuests = 19, perDay = 0.5f },
            new HunterRow { name = "Una Frostwick", rank = "F", baseQuests = 9, perDay = 0.4f },
        };

        public const string RivalName = "Corvin Thorne";
        static readonly int[] RivalOffset = { 2, -1, 1, -2, 3, 0 };

        /// <summary>Hunter NPC dan rival; urut rank lalu jumlah quest. Rival selalu dekat dengan pemain (saling susul).</summary>
        public List<RankEntry> BuildRanking()
        {
            var gs = GameState.I;
            var list = new List<RankEntry>();
            if (gs == null) return list;
            foreach (var h in Hunters)
                list.Add(new RankEntry { name = h.name, rank = h.rank, quests = h.baseQuests + Mathf.FloorToInt(h.perDay * (gs.day - 1)) });
            list.Add(new RankEntry { name = RivalName, rank = gs.rank, quests = Mathf.Max(0, gs.questsDone + RivalOffset[gs.day % RivalOffset.Length]), isRival = true });
            list.Add(new RankEntry { name = "Kamu", rank = gs.rank, quests = gs.questsDone, isPlayer = true });
            list.Sort((a, b) =>
            {
                int c = GameState.RankIndex(b.rank).CompareTo(GameState.RankIndex(a.rank));
                if (c != 0) return c;
                c = b.quests.CompareTo(a.quests);
                if (c != 0) return c;
                return a.isPlayer ? -1 : b.isPlayer ? 1 : 0;
            });
            return list;
        }

        // ================= Papan khusus: Menara dan perburuan wilayah =================
        static readonly float[] RegionFactor = { 1.0f, 0.8f, 0.6f, 0.5f };   // forest, desert, swamp (kills per quest hunter NPC); tower tidak dipakai

        static int Hash01(string s, int mod)
        {
            unchecked { int h = 17; foreach (char c in s) h = h * 31 + c; return Mathf.Abs(h % Mathf.Max(1, mod)); }
        }

        /// <summary>kind: "tower" (lantai tertinggi) atau forest / desert / swamp (jumlah monster dikalahkan di wilayah itu).</summary>
        public List<RankEntry> BuildBoard(string kind)
        {
            var gs = GameState.I;
            var list = new List<RankEntry>();
            if (gs == null) return list;
            foreach (var h in Hunters)
            {
                var e = new RankEntry { name = h.name, rank = h.rank };
                int ri = GameState.RankIndex(h.rank);
                if (kind == "tower")
                {
                    // hunter rank tinggi menembus lantai lebih jauh; sedikit kemajuan tiap hari
                    int baseFloor = Mathf.Clamp(ri * 12 + Hash01(h.name, 9), 1, 100);
                    e.score = Mathf.Min(100, baseFloor + Mathf.FloorToInt(h.perDay * (gs.day - 1) * 0.25f));
                    e.detail = "lantai " + e.score + "  (boss " + (e.score / 10 * 10) + ")";
                }
                else
                {
                    int rg = kind == "desert" ? 1 : kind == "swamp" ? 2 : 0;
                    e.score = Mathf.RoundToInt(h.baseQuests * 2.2f * RegionFactor[rg]) + Hash01(h.name + kind, 25) + Mathf.FloorToInt(h.perDay * (gs.day - 1) * 1.5f);
                    e.detail = e.score + " buruan";
                }
                list.Add(e);
            }
            var me = new RankEntry { name = "Kamu", rank = gs.rank, isPlayer = true };
            var rival = new RankEntry { name = RivalName, rank = gs.rank, isRival = true };
            if (kind == "tower")
            {
                me.score = gs.dungeonBest;
                me.detail = gs.dungeonBest <= 0 ? "belum masuk" : "lantai " + gs.dungeonBest + "  (boss " + gs.dungeonBossCleared + ")";
                rival.score = Mathf.Clamp(gs.dungeonBest + RivalOffset[gs.day % RivalOffset.Length], 0, 100);
                rival.detail = rival.score <= 0 ? "belum masuk" : "lantai " + rival.score;
            }
            else
            {
                me.score = gs.Stat("kills_" + kind);
                me.detail = me.score + " buruan";
                rival.score = Mathf.Max(0, me.score + RivalOffset[gs.day % RivalOffset.Length] * 3);
                rival.detail = rival.score + " buruan";
            }
            list.Add(me);
            list.Add(rival);
            list.Sort((a, b) =>
            {
                int c = b.score.CompareTo(a.score);
                if (c != 0) return c;
                return a.isPlayer ? -1 : b.isPlayer ? 1 : 0;
            });
            return list;
        }
    }
}
