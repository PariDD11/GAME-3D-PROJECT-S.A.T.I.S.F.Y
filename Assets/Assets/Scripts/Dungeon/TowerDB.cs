using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Status satu kali masuk Menara (dipakai lintas scene DungeonCity -> DungeonTower).</summary>
    public static class TowerRun
    {
        public static int startFloor = 1;
    }

    /// <summary>
    /// Data Menara Eldrath: 100 lantai dalam 10 tier, boss tiap 10 lantai, checkpoint setelah tiap boss.
    /// Angka (jumlah musuh, skala HP/ATK, reward) adalah placeholder untuk di-tuning setelah combat dites.
    /// </summary>
    public static class TowerDB
    {
        public const int Floors = 100, BossEvery = 10;

        public static readonly string[] TierNames =
        {
            "Gerbang Batu", "Lorong Berdebu", "Katakombe Sunyi", "Makam Pasir", "Aula Bayangan",
            "Perpustakaan Terkutuk", "Jantung Obsidian", "Taman Abyss", "Singgasana Tulang", "Puncak Eldrath"
        };

        static readonly Color[] TierFog =
        {
            new Color(0.20f, 0.20f, 0.24f), new Color(0.24f, 0.20f, 0.16f), new Color(0.12f, 0.20f, 0.18f), new Color(0.30f, 0.24f, 0.12f),
            new Color(0.14f, 0.10f, 0.22f), new Color(0.22f, 0.10f, 0.10f), new Color(0.08f, 0.08f, 0.12f), new Color(0.18f, 0.06f, 0.22f),
            new Color(0.26f, 0.26f, 0.22f), new Color(0.32f, 0.26f, 0.08f)
        };

        static readonly string[][] Pools =
        {
            new[] { "slime", "goblin", "sand_lizard" },
            new[] { "goblin", "sand_lizard", "skeleton", "bandit" },
            new[] { "skeleton", "drowned", "bandit", "bandit_archer" },
            new[] { "mummy", "skeleton", "bandit_archer", "wraith" },
            new[] { "wraith", "witch", "drowned", "mummy" },
            new[] { "witch", "mummy", "wraith", "bandit_leader" },
            new[] { "bandit_leader", "wraith", "witch", "mummy" },
            new[] { "witch", "wraith", "skeleton", "bandit_leader" },
            new[] { "skeleton", "wraith", "bandit_leader", "mummy" },
            new[] { "wraith", "witch", "bandit_leader", "skeleton" },
        };

        static readonly string[][] Elites =
        {
            new[] { "alpha_wolf" }, new[] { "alpha_wolf", "giant_boar" }, new[] { "giant_boar", "dire_wolf" }, new[] { "dire_wolf", "giant_scorpion" },
            new[] { "giant_scorpion", "swamp_troll" }, new[] { "swamp_troll", "sand_golem" }, new[] { "sand_golem", "swamp_troll" },
            new[] { "sand_golem", "dire_wolf" }, new[] { "sand_golem", "necromancer" }, new[] { "necromancer", "sand_golem" },
        };

        public static int Tier(int floor) { return Mathf.Clamp((floor - 1) / BossEvery, 0, 9); }
        public static string TierName(int floor) { return TierNames[Tier(floor)]; }
        public static Color TierColor(int floor) { return TierFog[Tier(floor)]; }
        public static bool IsBoss(int floor) { return floor % BossEvery == 0; }
        public static string BossId(int floor) { return "tbs_" + floor; }

        public static string[] Pool(int floor) { return Pools[Tier(floor)]; }
        public static string[] ElitePool(int floor) { return Elites[Tier(floor)]; }

        public static int MobCount(int floor) { return Mathf.Min(11, 3 + floor / 11); }
        public static int EliteCount(int floor)
        {
            if (IsBoss(floor)) return 0;
            if (floor % 5 == 0) return 1;
            return floor >= 60 && floor % 2 == 1 ? 1 : 0;
        }
        public static int EscortCount(int floor) { return Mathf.Min(6, 2 + floor / 25); }

        public static float HpMult(int floor) { return 1f + floor * 0.05f; }
        public static float AtkMult(int floor) { return 1f + floor * 0.03f; }
        public static float EliteHpMult(int floor) { return 0.8f + floor * 0.025f; }

        public static int ClearGold(int floor) { return 8 + floor * 3 + (IsBoss(floor) ? 150 + floor * 8 : 0); }
        public static int ClearExp(int floor) { return 6 + floor * 2 + (IsBoss(floor) ? 60 + floor * 4 : 0); }

        /// <summary>Level yang disarankan untuk lantai ini (placeholder).</summary>
        public static int RecommendedLevel(int floor) { return Mathf.Clamp(5 + Mathf.RoundToInt(floor * 0.45f), 5, 50); }

        /// <summary>Lantai awal yang boleh dipilih: 1, lalu lantai tepat setelah tiap boss yang sudah dikalahkan.</summary>
        public static List<int> StartFloors(int bossCleared)
        {
            var l = new List<int> { 1 };
            for (int b = BossEvery; b <= bossCleared && b < Floors; b += BossEvery) l.Add(b + 1);
            return l;
        }
    }
}
