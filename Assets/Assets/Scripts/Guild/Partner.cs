using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Satu partner yang bisa direkrut lewat poster resepsionis (GDD 5.2).</summary>
    [System.Serializable]
    public class Partner
    {
        public string key, name, rank, role;
        public int age, hp, atk, def, spd, costPct;
        public bool unique;
        public string[] lines;      // dialog progresif: makin akrab, makin dalam
        public int hiredCost;       // persen reward yang disepakati saat direkrut

        public int RankIdx { get { return GameState.RankIndex(rank); } }
        public Color PortraitColor { get { return PartnerFactory.RoleColor(role); } }

        public string Line(int familiarity)
        {
            if (lines == null || lines.Length == 0) return "";
            return lines[Mathf.Clamp(familiarity, 0, lines.Length - 1)];
        }
    }

    /// <summary>Partner unik dengan nama dan lore tetap (draft GDD 5.2). Muncul bila rank poster masuk rentang minRank..maxRank.</summary>
    public class UniqueDef
    {
        public string key, name, role, minRank, maxRank;
        public int age;
        public string[] lines;
    }

    public static class PartnerFactory
    {
        public static readonly string[] Roles = { "Tank", "DPS Melee", "DPS Ranged", "Support" };

        // 2 pool nama x pool nama keluarga = 16 x 2 x 13 = 416 kombinasi (GDD: 150-300+).
        static readonly string[] NamesM =
            { "Bram", "Dain", "Elric", "Gael", "Hale", "Joren", "Lorcan", "Nolan", "Osric", "Pike", "Rowan", "Soren", "Tarin", "Ulric", "Varek", "Wystan" };
        static readonly string[] NamesF =
            { "Aria", "Cora", "Fenna", "Isolde", "Kira", "Maren", "Orla", "Petra", "Quilla", "Rhea", "Sabine", "Talia", "Una", "Vessa", "Wren", "Yara" };
        static readonly string[] Surnames =
            { "Ashford", "Blackthorn", "Coldwater", "Duskmoor", "Emberly", "Frostwick", "Greymantle", "Highmere", "Ironvale", "Larkspur", "Moorcroft", "Stormrider", "Thornwood" };

        // Rentang stat per rank G..SS: HP min/max, ATK min/max, DEF min/max, SPD min/max (GDD 5.2).
        static readonly int[][] StatTable =
        {
            new[] { 60, 90, 10, 18, 5, 10, 40, 55 },        // G
            new[] { 90, 130, 18, 28, 10, 16, 45, 60 },      // F
            new[] { 130, 180, 28, 40, 16, 24, 50, 65 },     // E
            new[] { 180, 240, 40, 55, 24, 34, 55, 70 },     // D
            new[] { 240, 320, 55, 75, 34, 46, 60, 75 },     // C
            new[] { 320, 420, 75, 100, 46, 60, 65, 80 },    // B
            new[] { 420, 550, 100, 130, 60, 78, 70, 85 },   // A
            new[] { 550, 700, 130, 165, 78, 98, 75, 90 },   // S
            new[] { 700, 900, 165, 210, 98, 120, 80, 95 },  // SS
        };

        // Persen reward per rank: 15% (G) sampai 25% (SS) (GDD 5.2).
        static readonly int[] CostByRank = { 15, 16, 18, 19, 20, 21, 23, 24, 25 };

        static readonly string[][] GenericLines =
        {
            new[] { "Biar aku di depan. Kamu fokus pada tujuan.", "Tidak ada yang lewat selama aku masih berdiri.", "Sudah beberapa kali kita bekerja bersama. Aku percaya padamu." },          // Tank
            new[] { "Tunjukkan saja musuhnya.", "Kita cocok. Kamu yang mengatur, aku yang menebas.", "Kalau ada quest sulit lagi, panggil aku duluan." },                                      // DPS Melee
            new[] { "Selama aku punya anak panah, kamu aman di belakangku.", "Aku suka bekerja denganmu. Tidak banyak bicara.", "Jaga jarak, bidik akurat. Kita satu pikiran sekarang." }, // DPS Ranged
            new[] { "Aku siapkan ramuan dan perban. Jangan terlalu nekat, ya.", "Kamu sering sekali terluka. Biar aku yang merawat.", "Kita sudah seperti tim sungguhan. Aku senang." }   // Support
        };

        public static readonly List<UniqueDef> Uniques = new List<UniqueDef>
        {
            new UniqueDef { key = "u:kael", name = "Kael", age = 31, role = "DPS Melee", minRank = "D", maxRank = "D",
                lines = new[] { "Kael, mantan prajurit kerajaan. Saya bekerja sesuai kontrak, tidak lebih.",
                                "Disiplin menyelamatkan nyawa saya di medan perang. Anda pasti paham maksudnya.",
                                "Ada satu perintah dari atas yang tak pernah saya lupakan. Mungkin suatu hari saya ceritakan." } },
            new UniqueDef { key = "u:mira", name = "Mira", age = 22, role = "Support", minRank = "D", maxRank = "C",
                lines = new[] { "Halo! Aku Mira, herbalis. Aku kenal hampir semua herb di hutan, dan bisa memasaknya juga.",
                                "Bawakan aku daging serigala, nanti kumasakkan sesuatu yang bikin semangat lagi.",
                                "Ibuku bilang cahaya ungu di reruntuhan itu bukan cahaya biasa. Aku jadi penasaran." } },
            new UniqueDef { key = "u:draven", name = "Draven", age = 38, role = "Tank", minRank = "B", maxRank = "B",
                lines = new[] { "Draven. Dulu rank A. Jangan tanya kenapa aku turun.",
                                "Ada sesuatu di dalam dungeon yang bukan monster. Aku tak punya bukti, jadi aku diam.",
                                "Hunter yang mati di sana, energinya diambil. Aku pernah melihat intinya sendiri." } },
            new UniqueDef { key = "u:elysia", name = "Elysia", age = 27, role = "DPS Ranged", minRank = "B", maxRank = "A",
                lines = new[] { "Elysia. Jangan menghalangi garis tembakku, itu saja.",
                                "Archer di Guild ini bisa dihitung dengan jari. Aku ingin tahu seberapa bagus kamu.",
                                "Aku tidak suka kalah. Tapi untukmu, aku mau berbagi satu-dua trik." } },
            new UniqueDef { key = "u:torvin", name = "Torvin", age = 61, role = "Tank", minRank = "A", maxRank = "A",
                lines = new[] { "Torvin. Enam puluh satu musim dingin dan masih berdiri. Itu kualifikasiku.",
                                "Retakan pertama dulu terbuka di bukit timur. Orang bilang itu kecelakaan.",
                                "Kalau bertemu orang yang menyebut diri Hollow Circle, menjauhlah. Aku tidak bercanda." } },
            new UniqueDef { key = "u:nyx", name = "Nyx", age = 29, role = "DPS Melee", minRank = "S", maxRank = "S",
                lines = new[] { "Nyx. Kau tak perlu mempercayaiku. Cukup bayar tepat waktu.",
                                "Aku pernah berdiri di sisi yang salah. Aku tidak akan mengulanginya.",
                                "Tiap dungeon buatan punya inti gelap. Hancurkan intinya, dungeon itu mati." } },
            new UniqueDef { key = "u:seraphine", name = "Seraphine", age = 54, role = "Support", minRank = "SS", maxRank = "SS",
                lines = new[] { "Seraphine. Hunter yang hampir pensiun. Kau beruntung aku sedang mau bekerja.",
                                "Naga di dasar sana tidak lahir jahat. Ia dijaga, bukan dibiarkan bebas.",
                                "Pilihan terakhir akan jadi milikmu. Kuharap kau memilihnya dengan hati-hati." } },
        };

        public static Color RoleColor(string role)
        {
            switch (role)
            {
                case "Tank": return new Color(0.35f, 0.45f, 0.65f);
                case "DPS Melee": return new Color(0.70f, 0.30f, 0.25f);
                case "DPS Ranged": return new Color(0.30f, 0.60f, 0.35f);
                default: return new Color(0.80f, 0.70f, 0.30f);
            }
        }

        // Pergeseran distribusi stat per role: HP, ATK, DEF, SPD (Tank: HP/DEF tinggi, ATK rendah; DPS sebaliknya).
        static float[] RoleBias(string role)
        {
            switch (role)
            {
                case "Tank": return new[] { 0.30f, -0.30f, 0.35f, -0.20f };
                case "DPS Melee": return new[] { 0.00f, 0.30f, -0.05f, 0.05f };
                case "DPS Ranged": return new[] { -0.20f, 0.30f, -0.25f, 0.25f };
                default: return new[] { 0.00f, -0.30f, 0.00f, 0.15f };
            }
        }

        static string[] LinesFor(string role)
        {
            for (int i = 0; i < Roles.Length; i++) if (Roles[i] == role) return GenericLines[i];
            return GenericLines[0];
        }

        static int Roll(System.Random rng, int lo, int hi, float bias)
        {
            float t = 0.25f + (float)rng.NextDouble() * 0.5f + bias;
            return Mathf.RoundToInt(Mathf.Lerp(lo, hi, Mathf.Clamp01(t)));
        }

        static void FillStats(Partner p, System.Random rng, int rankIdx, float bonus)
        {
            int[] s = StatTable[rankIdx];
            float[] b = RoleBias(p.role);
            p.hp = Roll(rng, s[0], s[1], b[0] + bonus);
            p.atk = Roll(rng, s[2], s[3], b[1] + bonus);
            p.def = Roll(rng, s[4], s[5], b[2] + bonus);
            p.spd = Roll(rng, s[6], s[7], b[3] + bonus);
        }

        static int Hash(string s)
        {
            unchecked
            {
                int h = 17;
                foreach (char c in s) h = h * 31 + c;
                return h;
            }
        }

        /// <summary>Partner acak: nama dari pool, stat dari rentang rank, role menggeser distribusi.</summary>
        public static Partner Generate(System.Random rng, int rankIdx, string forcedRole)
        {
            rankIdx = Mathf.Clamp(rankIdx, 0, 8);
            string role = forcedRole != null ? forcedRole : Roles[rng.Next(Roles.Length)];
            bool female = rng.Next(2) == 0;
            string first = female ? NamesF[rng.Next(NamesF.Length)] : NamesM[rng.Next(NamesM.Length)];
            string name = first + " " + Surnames[rng.Next(Surnames.Length)];
            var p = new Partner
            {
                name = name,
                age = 19 + rng.Next(0, 8 + rankIdx * 3),
                rank = GameState.Ranks[rankIdx],
                role = role,
                costPct = CostByRank[rankIdx],
                lines = LinesFor(role)
            };
            p.key = "g:" + p.name + ":" + p.age + ":" + p.rank;
            FillStats(p, rng, rankIdx, 0f);
            return p;
        }

        public static List<UniqueDef> UniqueCandidates(int rankIdx, HashSet<string> excluded)
        {
            var l = new List<UniqueDef>();
            foreach (var u in Uniques)
            {
                if (excluded.Contains(u.key)) continue;
                if (rankIdx < GameState.RankIndex(u.minRank) || rankIdx > GameState.RankIndex(u.maxRank)) continue;
                l.Add(u);
            }
            return l;
        }

        /// <summary>Stat partner unik stabil (seed dari key + rank) dan sedikit lebih baik dari partner acak.</summary>
        public static Partner MakeUnique(UniqueDef u, int rankIdx)
        {
            rankIdx = Mathf.Clamp(rankIdx, 0, 8);
            var rng = new System.Random(Hash(u.key) + rankIdx * 97);
            var p = new Partner
            {
                key = u.key, name = u.name, age = u.age, role = u.role, unique = true,
                rank = GameState.Ranks[rankIdx], costPct = CostByRank[rankIdx], lines = u.lines
            };
            FillStats(p, rng, rankIdx, 0.12f);
            return p;
        }
    }
}
