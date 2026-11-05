using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Definisi quest di Papan Quest. QuestInfo di GameState hanya menyimpan quest yang sedang aktif.</summary>
    [System.Serializable]
    public class GuildQuest
    {
        public string id, rank, title, desc, area, theme, time, huntScene = "";
        public int reward, exp, minPartners, deposit;
        public bool fromNotice;                 // quest opsional dari Papan Pengumuman
        public float expiresAt = -1f;           // jam absolut (GuildData.Now); -1 = tidak kedaluwarsa
        public string examTarget = "";          // diisi = menyelesaikan quest ini berarti lulus ujian ke rank tersebut
        public int timeLimitMin;                // > 0 = quest berbatas waktu (ujian khusus)
        public int towerFloor;                  // > 0 = quest boss Menara Eldrath (lantai boss yang harus dikalahkan)

        /// <summary>Wilayah gerbang: forest (Selatan), desert (Timur), tower (Utara, Menara Eldrath), swamp (Barat).</summary>
        public string Region { get { return GameState.RegionKeyFor(huntScene); } }
        public static string RegionName(string key)
        {
            switch (key)
            {
                case "desert": return "Gurun Zahar (Timur)";
                case "swamp": return "Rawa Terkutuk (Barat)";
                case "tower": return "Menara Eldrath (Utara)";
                case "volcano": return "Gunung Vulkanik (Gerbang Utara Eldrath)";
                case "bone": return "Padang Tulang (Gerbang Timur Eldrath)";
                case "demon": return "Lembah Iblis (Gerbang Barat Eldrath)";
                case "frontier": return "Hutan Perbatasan (jalan Eldrath-Perbatasan)";
                default: return "Hutan Lerai (Selatan)";
            }
        }

        public int RankIdx { get { return GameState.RankIndex(rank); } }
        public bool PartyRequired { get { return minPartners > 0; } }
        public int PenaltyRep { get { return 2 + RankIdx; } }

        /// <summary>Cooldown rank setelah gagal (GDD 5.3). G-F tanpa cooldown.</summary>
        public float PenaltyCooldownHours
        {
            get { int r = RankIdx; return r >= 5 ? 3f : r >= 4 ? 2f : r >= 2 ? 1f : 0f; }
        }

        public string PenaltyText()
        {
            var parts = new List<string>();
            parts.Add("reputasi guild turun");
            if (deposit > 0) parts.Add("deposit " + deposit + "g hangus");
            if (PenaltyCooldownHours > 0f) parts.Add("cooldown quest rank " + rank + " selama " + GuildServices.FormatHours(PenaltyCooldownHours));
            return string.Join(", ", parts.ToArray());
        }
    }

    /// <summary>
    /// Daftar quest Guild. Reward dan EXP berada di dalam rentang tabel GDD 5.1.
    /// Rank B ke atas wajib party (B-A min 1 partner, S-SS min 2: asumsi). Deposit 10% reward mulai rank C (GDD 5.3: "jika ada deposit").
    /// </summary>
    public static class GuildQuestDB
    {
        static readonly List<GuildQuest> all = new List<GuildQuest>();

        static GuildQuestDB()
        {
            // ----- Rank G (solo, 5-10 menit) -----
            Add("g_wolf", "G", "Buru: Alpha Wolf Muda", "Pemimpin kawanan serigala mengganggu jalur dagang Hutan Lerai.", "Hutan Barat", "forest", 35, 20, "8-10 menit", "HuntArea_G");
            Add("g_boar", "G", "Buru: Giant Boar", "Babi hutan raksasa merusak ladang dekat kota.", "Bukit Berbatu", "plains", 30, 18, "6-8 menit", "HuntArea_G");
            Add("g_slime", "G", "Basmi: 5 Slime", "Slime di padang rumput makin banyak.", "Padang Gerbang", "plains", 22, 10, "5-7 menit", "HuntArea_G");
            Add("g_herb", "G", "Kumpulkan: 5 Herb", "Apotek kota kehabisan Herb.", "Hutan Tengah", "forest", 20, 10, "5-8 menit", "HuntArea_G");
            // ----- Rank F (solo, 10-15 menit) -----
            Add("f_dire", "F", "Buru: Dire Wolf Dewasa", "Serigala dewasa terlihat di tepi hutan dalam.", "Hutan Gelap", "forest", 55, 30, "12-15 menit", "HuntArea_G");
            Add("f_gob", "F", "Basmi: Goblin Pengintai", "Goblin mengintai kafilah pedagang.", "Perkemahan Goblin", "plains", 48, 25, "10-13 menit", "HuntArea_G");
            Add("f_slime", "F", "Basmi: 8 Slime", "Slime berkembang biak di padang selatan dan merusak ladang.", "Padang Gerbang", "plains", 62, 28, "10-14 menit", "HuntArea_G");
            // ----- Gurun Zahar (Gerbang Luar Timur) -----
            Add("f_scorp", "F", "Buru: Skorpion Raksasa", "Skorpion sebesar kereta menyerang kafilah di Lembah Kuil.", "Lembah Kuil", "desert", 75, 38, "12-16 menit", "HuntArea_Desert");
            Add("f_lizard", "F", "Basmi: 6 Kadal Pasir", "Kadal pasir berkeliaran dekat Oasis Zafir dan menakuti pedagang.", "Oasis Zafir", "desert", 58, 28, "10-14 menit", "HuntArea_Desert");
            Add("f_cactus", "F", "Kumpulkan: 6 Buah Kaktus", "Tabib Oasis Zafir butuh buah kaktus untuk obat demam.", "Lautan Dune", "desert", 52, 24, "10-14 menit", "HuntArea_Desert");
            Add("e_mummy", "E", "Basmi: 5 Mumi Penjaga", "Penjaga kuil kuno bangkit dan menyerang penjelajah.", "Lembah Kuil", "desert", 125, 62, "18-25 menit", "HuntArea_Desert");
            Add("e_sandbandit", "E", "Basmi: 8 Bandit Gurun", "Bandit menduduki Desa Sahr yang ditinggalkan dan merampok kafilah.", "Desa Sahr", "desert", 140, 68, "20-28 menit", "HuntArea_Desert");
            Add("d_golem", "D", "Buru: Golem Pasir", "Golem batu pasir terbangun di Ngarai Merah.", "Ngarai Merah", "desert", 230, 115, "28-38 menit", "HuntArea_Desert");
            // ----- Rawa Terkutuk (Gerbang Luar Barat) -----
            Add("f_lotus", "F", "Kumpulkan: 6 Teratai Rawa", "Tabib Desa Kelabu butuh teratai rawa untuk penawar demam kabut.", "Rawa Hitam", "swamp", 56, 26, "10-14 menit", "HuntArea_Swamp");
            Add("f_drowned", "F", "Basmi: 6 Nelayan Tenggelam", "Nelayan yang hilang di danau kembali berjalan di malam hari.", "Teluk Nelayan", "swamp", 70, 32, "10-15 menit", "HuntArea_Swamp");
            Add("e_skeleton", "E", "Basmi: 7 Kerangka Rawa", "Kerangka penduduk Desa Merana bangkit dari kuburan mereka.", "Desa Merana", "swamp", 130, 64, "18-25 menit", "HuntArea_Swamp");
            Add("e_wraith", "E", "Basmi: 4 Hantu Rawa", "Roh penasaran mengusik jalan di sekitar kapel tua.", "Desa Merana", "swamp", 150, 72, "20-28 menit", "HuntArea_Swamp");
            Add("d_witch", "D", "Basmi: 3 Penyihir Rawa", "Sekte penyihir mengutuk ikan dan air desa nelayan.", "Gubuk Penyihir", "swamp", 210, 105, "25-35 menit", "HuntArea_Swamp");
            Add("d_troll", "D", "Buru: Troll Rawa", "Troll raksasa menyeret perahu nelayan ke dasar rawa.", "Lahan Gambut", "swamp", 260, 130, "28-38 menit", "HuntArea_Swamp");
            Add("c_necro", "C", "Taklukkan: Maelis sang Nekromancer", "Dalang di balik kutukan rawa bersemayam di menara tulang di utara.", "Menara Maelis", "swamp", 520, 260, "45-60 menit", "HuntArea_Swamp");
            // ----- Area sekitar Kota Eldrath (diambil di Guild Eldrath) -----
            Add("fr_wolf", "E", "Basmi: 6 Serigala Jalan", "Kawanan serigala menyerang kafilah di Hutan Perbatasan.", "Hutan Serigala", "deepforest", 120, 60, "15-22 menit", "HuntArea_Frontier");
            Add("fr_bandit", "D", "Basmi: 8 Bandit Jalan Raya", "Bandit merampok pedagang antara Ashwatch dan Eldrath.", "Kamp Bandit", "deepforest", 230, 115, "22-30 menit", "HuntArea_Frontier");
            Add("fr_ogre", "D", "Basmi: 4 Ogre", "Ogre menyerbu kebun dan ladang di sepanjang jalan raya.", "Sarang Ogre", "deepforest", 280, 140, "25-35 menit", "HuntArea_Frontier");
            Add("fr_grumak", "C", "Taklukkan: Grumak sang Penguasa Hutan", "Kepala ogre yang menguasai Hutan Perbatasan.", "Sarang Ogre", "deepforest", 560, 280, "40-55 menit", "HuntArea_Frontier");
            Add("fr_behemoth", "B", "Taklukkan: Behemoth Penghancur Hutan", "Banteng raksasa yang menghancurkan jalan raya Ashwatch-Eldrath.", "Sarang Behemoth", "deepforest", 900, 450, "40-55 menit", "HuntArea_Frontier");
            Add("vo_lizard", "C", "Basmi: 8 Kadal Lava", "Kadal lava menghalangi para penambang belerang.", "Sungai Lava", "volcano", 430, 215, "30-40 menit", "HuntArea_Volcano");
            Add("vo_imp", "C", "Basmi: 10 Imp Bara", "Imp bara membakar perkemahan di kaki Gunung Vulkanik.", "Lereng Abu", "volcano", 480, 240, "30-40 menit", "HuntArea_Volcano");
            Add("vo_golem", "B", "Buru: Golem Magma", "Golem magma terbangun di ngarai belerang.", "Ngarai Belerang", "volcano", 900, 450, "40-55 menit", "HuntArea_Volcano");
            Add("vo_ignarok", "A", "Taklukkan: Ignarok, Naga Magma", "Naga magma bersemayam di kawah gunung dan memuntahkan api ke ladang jauh.", "Sarang Ignarok", "volcano", 2100, 1000, "60-80 menit", "HuntArea_Volcano");
            Add("bo_soldier", "C", "Basmi: 10 Prajurit Tulang", "Prajurit tulang bangkit dari medan perang lama.", "Padang Tulang", "bone", 450, 225, "28-38 menit", "HuntArea_Bone");
            Add("bo_banshee", "B", "Basmi: 5 Banshee", "Jeritan banshee membuat penjelajah kehilangan arah di Padang Hantu.", "Padang Hantu", "bone", 850, 420, "35-50 menit", "HuntArea_Bone");
            Add("bo_knight", "B", "Buru: Ksatria Makam", "Ksatria makam menjaga pemakaman perang.", "Pemakaman Perang", "bone", 950, 470, "40-55 menit", "HuntArea_Bone");
            Add("bo_malrek", "A", "Taklukkan: Malrek, Raja Tulang", "Raja Tulang bangkit dan memimpin pasukan undead di Makam Raja.", "Makam Raja Malrek", "bone", 2200, 1050, "60-80 menit", "HuntArea_Bone");
            Add("bo_cerberus", "A", "Taklukkan: Cerberus, Penjaga Gerbang", "Anjing tiga kepala penjaga gerbang makam Malrek.", "Gerbang Makam", "bone", 2600, 1200, "60-80 menit", "HuntArea_Bone");
            Add("de_imp", "B", "Basmi: 10 Imp Jurang", "Imp jurang muncul dari retakan Lembah Iblis.", "Gerbang Lembah", "demon", 880, 440, "35-50 menit", "HuntArea_Demon");
            Add("de_cult", "A", "Basmi: 6 Pemuja Gelap", "Pemuja gelap memanggil iblis di altar ritual.", "Altar Ritual", "demon", 1500, 740, "45-60 menit", "HuntArea_Demon");
            Add("de_brute", "A", "Buru: Brute Iblis", "Brute iblis mengamuk di dataran retakan.", "Dataran Retakan", "demon", 1700, 840, "50-65 menit", "HuntArea_Demon");
            Add("de_zargoth", "S", "Taklukkan: Zargoth, Lord Iblis", "Lord Iblis duduk di takhtanya, memerintah pasukan neraka.", "Takhta Zargoth", "demon", 3600, 1700, "70-100 menit", "HuntArea_Demon");
            // ----- Menara Eldrath (Gerbang Luar Utara): 100 lantai, boss tiap 10 lantai -----
            Add("t_warm", "G", "Latihan Menara: Basmi 8 Monster", "Pemula boleh berlatih di lantai bawah Menara Eldrath. Basmi 8 monster di lantai mana pun.", "Menara, Lantai 1-9", "dungeon", 45, 22, "8-12 menit", "DungeonTower");
            AddTower(10, "F", 120, 60);
            AddTower(20, "E", 230, 115);
            AddTower(30, "D", 380, 190);
            AddTower(40, "C", 620, 310);
            AddTower(50, "B", 980, 490);
            AddTower(60, "B", 1350, 670);
            AddTower(70, "A", 1950, 950);
            AddTower(80, "S", 2850, 1350);
            AddTower(90, "S", 3900, 1750);
            AddTower(100, "SS", 6200, 2600);
            // ----- Rank E (solo, 15-25 menit) -----
            Add("e_orc", "E", "Basmi: Orc Penjarah", "Orc menjarah gudang kayu di hutan dalam.", "Hutan Dalam", "deepforest", 95, 48, "18-25 menit");
            Add("e_spider", "E", "Buru: Giant Spider", "Koloni laba-laba menutup mulut gua dangkal.", "Gua Dangkal", "cave", 85, 42, "15-22 menit");
            // ----- Rank D (solo, 25-40 menit) -----
            Add("d_bandit", "D", "Basmi: Kamp Bandit", "Bandit membangun kamp di jalur pedagang utama.", "Hutan Dalam", "deepforest", 160, 80, "30-40 menit");
            Add("d_orcwar", "D", "Buru: Orc Prajurit", "Orc bersenjata lengkap terlihat bergerak di gua dangkal.", "Gua Dangkal", "cave", 185, 95, "28-38 menit");
            // ----- Rank C (solo, 40-60 menit) -----
            Add("c_ogre", "C", "Buru: Ogre Reruntuhan", "Sesuatu mengusik makhluk penghuni reruntuhan kuno.", "Reruntuhan Kuno", "ruins", 300, 150, "45-60 menit");
            Add("c_undead", "C", "Bersihkan: Gua Undead", "Kerangka dan ghoul bangkit di gua dalam setiap malam.", "Gua Dalam", "cave", 240, 120, "40-55 menit");
            // ----- Rank B (WAJIB PARTY, 1-1,5 jam) -----
            Add("b_alpha", "B", "Buru: Alpha Wolf Elite", "Alpha Wolf raksasa memimpin kawanan di Hutan Utara.", "Hutan Utara", "deepforest", 480, 240, "1-1,5 jam");
            Add("b_ruins", "B", "Selidiki: Cahaya Ungu", "Cahaya ungu berdenyut dari reruntuhan utara. Selidiki dan laporkan.", "Reruntuhan Utara", "ruins", 420, 200, "1-1,5 jam");
            // ----- Rank A / S / SS (WAJIB PARTY) -----
            Add("a_drake", "A", "Buru: Dragon Muda", "Seekor dragon muda bersarang di dungeon berbahaya.", "Dungeon Berbahaya", "dungeon", 800, 400, "1,5-2,5 jam");
            Add("s_tower", "S", "Taklukkan: Menara Terkutuk", "Menara kuno mengeluarkan monster tiap malam bulan baru.", "Menara Terkutuk", "tower", 1500, 700, "2,5-4 jam");
            Add("ss_ancient", "SS", "Dungeon Legendaris: Sang Penjaga", "Area rahasia tempat entitas purba dijaga.", "Area Rahasia", "secret", 3500, 1200, "4+ jam");
        }

        static readonly string[] BossTitle =
        {
            "Grakk, Raja Goblin", "Fenrath, Serigala Neraka", "Kapten Voss si Pengkhianat", "Anubra, Raja Mumi", "Gorgath, Troll Batu",
            "Skorra, Ratu Kalajengking", "Morgath, Golem Obsidian", "Lady Nyxara, Penyihir Abyss", "Malvek, Raja Tengkorak", "Eldrath, Penjaga Menara"
        };

        static void AddTower(int floor, string rank, int reward, int exp)
        {
            int tier = floor / 10 - 1;
            string time = floor <= 30 ? "20-35 menit" : floor <= 60 ? "40-70 menit" : "1,5-3 jam";
            var q = Make("t_" + floor, rank, "Menara: Kalahkan " + BossTitle[tier], "Capai lantai " + floor + " Menara Eldrath dan kalahkan penjaganya. Boss sebelumnya (lantai " + (floor - 10) + ") harus sudah dikalahkan agar jalannya terbuka.",
                "Menara, Lantai " + floor, "dungeon", reward, exp, time, "DungeonTower");
            q.towerFloor = floor;
            all.Add(q);
        }

        /// <summary>Objektif quest yang dimainkan di area hunt (Hutan Lerai). Quest tanpa objektif belum punya area hunt.</summary>
        public static bool Objective(string id, out string type, out string target, out int count)
        {
            type = ""; target = ""; count = 0;
            switch (id)
            {
                case "g_wolf":  type = "hunt";   target = "alpha_wolf"; count = 1; return true;
                case "g_boar":  type = "hunt";   target = "giant_boar"; count = 1; return true;
                case "g_slime": type = "exterm"; target = "slime";      count = 5; return true;
                case "g_herb":  type = "gather"; target = "herb";       count = 5; return true;
                case "f_dire":  type = "hunt";   target = "dire_wolf";  count = 1; return true;
                case "f_gob":   type = "exterm"; target = "goblin";     count = 4; return true;
                case "f_slime": type = "exterm"; target = "slime";      count = 8; return true;
                case "f_scorp":  type = "hunt";   target = "giant_scorpion"; count = 1; return true;
                case "f_lizard": type = "exterm"; target = "sand_lizard";    count = 6; return true;
                case "f_cactus": type = "gather"; target = "cactus_fruit";   count = 6; return true;
                case "e_mummy":  type = "exterm"; target = "mummy";          count = 5; return true;
                case "e_sandbandit": type = "exterm"; target = "bandit";     count = 8; return true;
                case "d_golem":  type = "hunt";   target = "sand_golem";     count = 1; return true;
                case "t_warm":     type = "exterm"; target = "tower_any";   count = 8; return true;
                case "f_lotus":    type = "gather"; target = "swamp_lotus"; count = 6; return true;
                case "f_drowned":  type = "exterm"; target = "drowned";     count = 6; return true;
                case "e_skeleton": type = "exterm"; target = "skeleton";    count = 7; return true;
                case "e_wraith":   type = "exterm"; target = "wraith";      count = 4; return true;
                case "d_witch":    type = "exterm"; target = "witch";       count = 3; return true;
                case "d_troll":    type = "hunt";   target = "swamp_troll"; count = 1; return true;
                case "c_necro":    type = "hunt";   target = "necromancer"; count = 1; return true;
                case "fr_wolf":    type = "exterm"; target = "road_wolf";      count = 6; return true;
                case "fr_bandit":  type = "exterm"; target = "highway_bandit"; count = 8; return true;
                case "fr_ogre":    type = "exterm"; target = "ogre";           count = 4; return true;
                case "fr_behemoth": type = "hunt"; target = "behemoth"; count = 1; return true;
                case "bo_cerberus": type = "hunt"; target = "cerberus"; count = 1; return true;
                case "fr_grumak":  type = "hunt";   target = "ogre_chief";     count = 1; return true;
                case "vo_lizard":  type = "exterm"; target = "lava_lizard";    count = 8; return true;
                case "vo_imp":     type = "exterm"; target = "ember_imp";      count = 10; return true;
                case "vo_golem":   type = "hunt";   target = "magma_golem";    count = 1; return true;
                case "vo_ignarok": type = "hunt";   target = "ignarok";        count = 1; return true;
                case "bo_soldier": type = "exterm"; target = "bone_soldier";   count = 10; return true;
                case "bo_banshee": type = "exterm"; target = "banshee";        count = 5; return true;
                case "bo_knight":  type = "hunt";   target = "grave_knight";   count = 1; return true;
                case "bo_malrek":  type = "hunt";   target = "bone_king";      count = 1; return true;
                case "de_imp":     type = "exterm"; target = "imp";            count = 10; return true;
                case "de_cult":    type = "exterm"; target = "dark_cultist";   count = 6; return true;
                case "de_brute":   type = "hunt";   target = "demon_brute";    count = 1; return true;
                case "de_zargoth": type = "hunt";   target = "demon_lord";     count = 1; return true;
            }
            if (id.StartsWith("t_") && id.Length > 2)
            {
                int fl;
                if (int.TryParse(id.Substring(2), out fl)) { type = "hunt"; target = TowerDB.BossId(fl); count = 1; return true; }
            }
            return false;
        }

        static void Add(string id, string rank, string title, string desc, string area, string theme, int reward, int exp, string time, string hunt = "")
        {
            all.Add(Make(id, rank, title, desc, area, theme, reward, exp, time, hunt));
        }

        /// <summary>Dipakai juga untuk quest dinamis (notice, ujian) agar aturan party dan deposit konsisten.</summary>
        public static GuildQuest Make(string id, string rank, string title, string desc, string area, string theme, int reward, int exp, string time, string hunt = "")
        {
            var q = new GuildQuest
            {
                id = id, rank = rank, title = title, desc = desc, area = area, theme = theme,
                reward = reward, exp = exp, time = time, huntScene = hunt
            };
            int r = q.RankIdx;
            q.minPartners = r >= 7 ? 2 : r >= 5 ? 1 : 0;
            q.deposit = r >= 4 ? Mathf.RoundToInt(reward * 0.1f / 5f) * 5 : 0;
            return q;
        }

        /// <summary>Quest tetap + quest opsional dari Papan Pengumuman + quest ujian, urut rank lalu reward.</summary>
        public static List<GuildQuest> Visible()
        {
            var l = new List<GuildQuest>(all);
            var gs = GameState.I;
            float now = GuildData.Now;
            foreach (var q in GuildData.I.extraQuests)
            {
                bool active = gs != null && gs.activeQuest != null && gs.activeQuest.id == q.id;
                if (q.expiresAt < 0f || q.expiresAt > now || active) l.Add(q);
            }
            l.Sort((a, b) =>
            {
                int c = a.RankIdx.CompareTo(b.RankIdx);
                return c != 0 ? c : a.reward.CompareTo(b.reward);
            });
            return l;
        }

        public static GuildQuest Find(string id)
        {
            foreach (var q in all) if (q.id == id) return q;
            foreach (var q in GuildData.I.extraQuests) if (q.id == id) return q;
            return null;
        }
    }
}
