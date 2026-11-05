using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    public class GearRecipe
    {
        public string result, slot, job;
        public int gold, area;
        public Dictionary<string, int> mats;
    }

    /// <summary>
    /// Gear tier area baru (Perbatasan, Vulkanik, Padang Tulang, Lembah Iblis), senjata eksklusif boss, enchant (+1..+10) dan imbue.
    /// Semua angka placeholder untuk di-tuning. Visual memakai model gear lama lewat VisualBase().
    /// </summary>
    public static class GearTiers
    {
        public static readonly string[] AreaNames = { "Hutan Perbatasan", "Gunung Vulkanik", "Padang Tulang", "Lembah Iblis" };
        static readonly string[] AreaShort = { "Perbatasan", "Bara", "Tulang Kuno", "Iblis" };
        static readonly string[] BossIds = { "behemoth", "ignarok", "cerberus", "demon_lord" };
        static readonly string[] BossTitle = { "Tanduk Behemoth", "Jantung Ignarok", "Taring Cerberus", "Takhta Zargoth" };
        static readonly int[] WAtk = { 32, 42, 46, 52 };
        static readonly int[] WMana = { 40, 70, 90, 120 };
        static readonly int[] WGold = { 600, 1400, 2200, 3500 };
        static readonly int[] ADefBody = { 24, 30, 34, 40 };
        static readonly int[] ADefHead = { 10, 13, 15, 18 };
        static readonly int[] ADefLegs = { 11, 14, 16, 19 };
        static readonly int[] ADefHands = { 9, 12, 14, 17 };
        static readonly string[] Jobs = { "Swordsman", "Archer", "FireMage", "EarthMage", "WaterMage", "WindMage", "Priest" };

        public static readonly List<GearRecipe> Recipes = new List<GearRecipe>();
        static readonly Dictionary<string, string> visual = new Dictionary<string, string>();

        /// <summary>Id model visual yang dipakai gear ini (gear tier baru meminjam model gear lama).</summary>
        public static void SetVisual(string id, string v) { visual[id] = v; }

        public static string VisualBase(string id)
        {
            string v;
            return id != null && visual.TryGetValue(id, out v) ? v : id;
        }

        static string JobWeaponVisual(string job)
        {
            switch (job)
            {
                case "Swordsman": return "knight_sword";
                case "Archer": return "long_bow";
                case "FireMage": return "inferno_staff";
                case "EarthMage": return "terra_staff";
                case "WaterMage": return "abyss_staff";
                case "WindMage": return "tempest_staff";
                default: return "seraph_staff";
            }
        }

        static readonly float[] SpAreaPower = { 8f, 12f, 16f, 20f };

        /// <summary>Efek kedua senjata area untuk mage, menurut elemen: api = kritis, tanah = regen mana, air = cooldown, angin = hemat mana, priest = regen mana.</summary>
        static string MageEff2(string job)
        {
            switch (job) { case "FireMage": return "crit"; case "EarthMage": return "manaregen"; case "WaterMage": return "cdr"; case "WindMage": return "manacost"; default: return "manaregen"; }
        }
        static float MageEff2Val(string job, int a)
        {
            switch (job) { case "FireMage": return 3f + a; case "EarthMage": return 10f + 5f * a; case "WaterMage": return 2f + a; case "WindMage": return 3f + a; default: return 15f + 5f * a; }
        }

        /// <summary>Senjata kelas evolusi hanya bisa dipakai bila kelas itu dipilih.</summary>
        public static bool EvoOk(GameState gs, ItemDef d)
        {
            if (d == null || string.IsNullOrEmpty(d.evoReq)) return true;
            return gs.evo == d.evoReq || gs.evo2 == d.evoReq;
        }

        static void RegisterEvo()
        {
            foreach (var e in JobInfo.Evos)
            {
                bool l2 = e.level == JobInfo.Evo2Level;
                bool mage = e.job != "Swordsman" && e.job != "Archer";
                string id = (l2 ? "ev2_" : "ev1_") + e.id;
                int atk = l2 ? 58 : 40;
                string e1, e2; float v1, v2;
                if (mage) { e1 = "spellpower"; v1 = l2 ? 26f : 15f; e2 = "cdr"; v2 = l2 ? 6f : 3f; }
                else if (e.job == "Swordsman") { e1 = "crit"; v1 = l2 ? 9f : 5f; e2 = "lifesteal"; v2 = l2 ? 4f : 2f; }
                else { e1 = "crit"; v1 = l2 ? 9f : 5f; e2 = "stamina"; v2 = l2 ? 30f : 15f; }
                AddItem(id, JobWeaponName(e.job) + " " + e.name, "Weapon", e.job, atk, 0, mage ? (l2 ? 150 : 100) : 0, l2 ? 7 : 6, JobWeaponVisual(e.job), e1, v1, e2, v2);
                ItemDB.Get(id).evoReq = e.id;
                Recipes.Add(new GearRecipe
                {
                    result = id, slot = "Weapon", job = e.job, area = l2 ? 11 : 10, gold = l2 ? 12000 : 4000,
                    mats = l2 ? M("magma_core", 3, "king_crown_shard", 3, "demon_heart", 3, "demon_horn", 6, "iron_ingot", 6)
                              : M("ogre_tusk", 4, "ember_scale", 4, "ancient_bone", 4, "demon_horn", 2, "iron_ingot", 4)
                });
            }
        }

        static string JobWeaponName(string job)
        {
            switch (job)
            {
                case "Swordsman": return "Pedang";
                case "Archer": return "Busur";
                case "FireMage": return "Tongkat Api";
                case "EarthMage": return "Tongkat Bumi";
                case "WaterMage": return "Tongkat Air";
                case "WindMage": return "Tongkat Angin";
                default: return "Tongkat Suci";
            }
        }

        static Dictionary<string, int> M(params object[] kv)
        {
            var d = new Dictionary<string, int>();
            for (int i = 0; i + 1 < kv.Length; i += 2) d[(string)kv[i]] = (int)kv[i + 1];
            return d;
        }

        static void AddItem(string id, string name, string slot, string job, int atk, int def, int mana, int tier, string vis, string e1 = "", float v1 = 0f, string e2 = "", float v2 = 0f)
        {
            var d = new ItemDef { id = id, name = name, type = ItemType.Gear, slot = slot, job = job, atk = atk, def = def, mana = mana, tier = tier, stack = 1,
                effect = e1, effVal = v1, effect2 = e2, effVal2 = v2, sellPrice = (atk + def) * 6 };
            ItemDB.AddRaw(d);
            visual[id] = vis;
        }

        static Dictionary<string, int> WeaponMats(int a, string job)
        {
            switch (a)
            {
                case 0:
                    return job == "Swordsman" ? M("ogre_tusk", 3, "iron_ingot", 2, "leather", 2)
                         : job == "Archer" ? M("ogre_tusk", 3, "iron_ingot", 2, "road_pelt", 3)
                         : M("ogre_tusk", 3, "iron_ingot", 2, "hex_cloth", 3);
                case 1: return M("ember_scale", 4, "brimstone", 3, "magma_core", 1, "iron_ingot", 2);
                case 2: return M("ancient_bone", 4, "grave_dust", 4, "king_crown_shard", 1, "ectoplasm", 2);
                default: return M("demon_horn", 3, "hellfire_ash", 4, "demon_heart", 1, "iron_ingot", 3);
            }
        }

        static Dictionary<string, int> ArmorMats(int a, int piece)   // piece: 0 body, 1 head, 2 legs, 3 hands
        {
            int[] n = { 6, 3, 4, 3 };
            switch (a)
            {
                case 0: return piece == 0 ? M("ogre_tusk", 2, "road_pelt", 4, "leather", 3) : M("road_pelt", piece == 2 ? 3 : 2, "leather", 2);
                case 1: { var d = M("ember_scale", n[piece], "brimstone", piece == 0 ? 3 : piece == 3 ? 1 : 2); if (piece == 0) d["magma_core"] = 1; return d; }
                case 2: { var d = M("ancient_bone", n[piece], "grave_dust", piece == 0 ? 4 : piece == 2 ? 3 : 2); if (piece == 0) d["king_crown_shard"] = 1; return d; }
                default: { var d = M("demon_horn", piece == 0 ? 4 : piece == 2 ? 3 : 2, "hellfire_ash", piece == 0 ? 5 : piece == 3 ? 2 : 3); if (piece == 0) d["demon_heart"] = 1; return d; }
            }
        }

        /// <summary>Dipanggil dari static constructor ItemDB.</summary>
        public static void Register()
        {
            for (int a = 0; a < 4; a++)
            {
                int tier = a + 2;
                string sfx = AreaShort[a];
                foreach (var job in Jobs)
                {
                    bool mage = job != "Swordsman" && job != "Archer";
                    string id = "gw_" + a + "_" + job;
                    if (mage) AddItem(id, JobWeaponName(job) + " " + sfx, "Weapon", job, WAtk[a], 0, WMana[a], tier, JobWeaponVisual(job), "spellpower", SpAreaPower[a], MageEff2(job), MageEff2Val(job, a));
                    else AddItem(id, JobWeaponName(job) + " " + sfx, "Weapon", job, WAtk[a], 0, 0, tier, JobWeaponVisual(job));
                    Recipes.Add(new GearRecipe { result = id, slot = "Weapon", job = job, area = a, gold = WGold[a], mats = WeaponMats(a, job) });

                    // senjata eksklusif boss area ini
                    string bid = "bw_" + BossIds[a] + "_" + job;
                    string e1, e2; float v1, v2;
                    switch (a)
                    {
                        case 0: e1 = "lifesteal"; v1 = 4f; e2 = "stamina"; v2 = 20f; break;
                        case 1: e1 = "crit"; v1 = 10f; e2 = "cdr"; v2 = 5f; break;
                        case 2: e1 = "cdr"; v1 = 8f; e2 = "manaregen"; v2 = 40f; break;
                        default: e1 = "crit"; v1 = 8f; e2 = "lifesteal"; v2 = 3f; break;
                    }
                    if (mage) AddItem(bid, JobWeaponName(job) + " " + BossTitle[a], "Weapon", job, WAtk[a] + 6, 0, WMana[a] + 10, 6, JobWeaponVisual(job), e1, v1, "spellpower", SpAreaPower[a] + 6f);
                    else AddItem(bid, JobWeaponName(job) + " " + BossTitle[a], "Weapon", job, WAtk[a] + 6, 0, 0, 6, JobWeaponVisual(job), e1, v1, e2, v2);
                }

                string[] slots = { "Body", "Head", "Legs", "Hands" };
                string[] kinds = { "Zirah", "Helm", "Sepatu", "Sarung Tangan" };
                int[] defs = { ADefBody[a], ADefHead[a], ADefLegs[a], ADefHands[a] };
                int[] gold = { 500, 300, 350, 300 };
                string[] vis = { "eld_body_" + a, "eld_head_" + a, "eld_legs_" + a, "leather_gloves" };
                for (int p = 0; p < 4; p++)
                {
                    string id = "ga_" + a + "_" + slots[p];
                    AddItem(id, kinds[p] + " " + sfx, slots[p], "Any", 0, defs[p], 0, tier, vis[p]);
                    Recipes.Add(new GearRecipe { result = id, slot = slots[p], job = "Any", area = a, gold = gold[p] * (a + 1), mats = ArmorMats(a, p) });
                }
            }
            RegisterEvo();
        }

        // ---------------------------------------------------------------- Boss drop
        public static string BossWeaponId(string bossId, string job) { return "bw_" + bossId + "_" + job; }

        /// <summary>Dipanggil saat boss mati. Kill pertama: pasti jatuh. Selanjutnya 20% selama belum punya.</summary>
        public static void BossDrop(string bossId)
        {
            var gs = GameState.I;
            if (gs == null) return;
            string id = BossWeaponId(bossId, gs.job);
            var def = ItemDB.Get(id);
            if (def == null) return;
            bool owns = gs.Count(id) > 0 || gs.equipment.ContainsValue(id);
            bool first = gs.Stat("bwd_" + bossId) == 0;
            if (!first && (owns || Random.value > 0.2f)) return;
            if (!gs.HasSpace(id, 1)) { if (TownUI.I != null) TownUI.I.Toast("Tas penuh! Senjata eksklusif boss tidak terbawa. Kosongkan tas lalu kalahkan lagi."); return; }
            gs.AddItem(id, 1);
            gs.AddStat("bwd_" + bossId);
            if (TownUI.I != null) TownUI.I.Toast("DROP EKSKLUSIF: " + def.name + "!");
            Audio.Sfx("victory");
        }

        // ---------------------------------------------------------------- Enchant & Imbue
        public static readonly string[] EnchantSlots = { "Weapon", "Head", "Body", "Legs", "Hands" };
        public const int MaxEnchant = 10;

        public static int EnchantLevel(GameState gs, string itemId) { return gs.Stat("enc_" + itemId); }

        static int PerLevel(ItemDef d)
        {
            return d.slot == "Weapon" ? Mathf.Max(1, d.atk / 12) : Mathf.Max(1, d.def / 10);
        }

        public static int EnchantSum(GameState gs, bool atk)
        {
            int t = 0;
            foreach (var s in EnchantSlots)
            {
                string id;
                if (!gs.equipment.TryGetValue(s, out id) || string.IsNullOrEmpty(id)) continue;
                var d = ItemDB.Get(id);
                if (d == null) continue;
                if (atk != (s == "Weapon")) continue;
                t += EnchantLevel(gs, id) * PerLevel(d);
            }
            return t;
        }

        /// <summary>Biaya naik dari level L ke L+1.</summary>
        public static Dictionary<string, int> EnchantMats(int L, out int gold)
        {
            if (L < 3) { gold = 80 + 60 * L; return M("iron_ingot", 2); }
            if (L < 5) { gold = 400; return M("ogre_tusk", 2, "ember_scale", 2); }
            if (L < 7) { gold = 800; return M("magma_core", 1, "ancient_bone", 3); }
            gold = 1500; return M("demon_heart", 1, "king_crown_shard", 1);
        }

        public static int EnchantChance(int L)
        {
            if (L < 5) return 100;
            return 80 - (L - 5) * 10;   // 5:80 6:70 7:60 8:50 9:40
        }

        public static readonly string[] ImbueNames = { "-", "Ketajaman (+6% kritis)", "Haus Darah (+3% lifesteal)", "Kelincahan (+25% stamina)", "Percepatan (-6% cooldown)" };
        static readonly string[] ImbueEff = { "", "crit", "lifesteal", "stamina", "cdr" };
        static readonly float[] ImbueVal = { 0f, 6f, 3f, 25f, 6f };

        public static int ImbueOf(GameState gs, string weaponId) { return gs.Stat("imb_" + weaponId); }

        public static void SetImbue(GameState gs, string weaponId, int type) { gs.stats["imb_" + weaponId] = type; }

        /// <summary>Total efek (persen) dari gear yang dipakai + imbue senjata.</summary>
        public static float GearEffect(GameState gs, string eff)
        {
            float t = 0f;
            foreach (var kv in gs.equipment)
            {
                var d = ItemDB.Get(kv.Value);
                if (d == null || d.type != ItemType.Gear) continue;
                if (d.effect == eff) t += d.effVal;
                if (d.effect2 == eff) t += d.effVal2;
                if (kv.Key == "Weapon")
                {
                    int im = ImbueOf(gs, kv.Value);
                    if (im > 0 && im < ImbueEff.Length && ImbueEff[im] == eff) t += ImbueVal[im];
                }
            }
            return t;
        }

        public static void ImbueApply(GameState gs, string weaponId, int type, string[] unused = null) { SetImbue(gs, weaponId, type); }
    }
}
