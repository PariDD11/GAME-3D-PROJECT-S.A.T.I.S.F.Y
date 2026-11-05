using UnityEngine;
using System.Collections.Generic;

namespace HunterGuild
{
    public enum ItemType { Food, Drink, Material, Consumable, Gear, Attachment, QuestItem }

    public class ItemDef
    {
        public string id, name, buff = "";
        public ItemType type;
        public int buyPrice, sellPrice, hunger, thirst;
        public int stack = 10;                 // maks jumlah per slot pouch
        public string slot = "", job = "Any";  // khusus gear
        public int atk, def;                   // khusus gear dan attachment
        public int mana;                       // bonus mana maksimum (staf, cincin, kalung)
        public string effect = "";             // efek khusus attachment: lifesteal / crit / stamina / gold / exp
        public float effVal;                   // besar efek (persen)
        public string effect2 = "";           // efek kedua (senjata boss)
        public float effVal2;
        public int tier;                       // attachment Sihir: 1-3
        public string evoReq = "";             // khusus senjata kelas evolusi: id evolusi yang dibutuhkan
        public string note = "";               // v35z102n32: deskripsi bebas untuk item khusus (pelapis, penawar, dst.)
        public string attachKind = "";        // khusus attachment: Serangan / Pertahanan / Utility

        public string Describe()
        {
            var parts = new List<string>();
            if (hunger != 0) parts.Add("Hunger +" + hunger);
            if (thirst != 0) parts.Add("Thirst +" + thirst);
            if (!string.IsNullOrEmpty(buff)) parts.Add(buff);
            return string.Join(", ", parts.ToArray());
        }

        public string EffectText() { return EffText(effect, effVal); }
        public string AllEffects() { string a = EffText(effect, effVal); string b = EffText(effect2, effVal2); return a != "" && b != "" ? a + "\n" + b : a + b; }

        public static string EffText(string effect, float effVal)
        {
            switch (effect)
            {
                case "lifesteal": return "Pulihkan HP " + effVal + "% dari damage yang diberikan";
                case "crit": return "+" + effVal + "% peluang serangan kritis (x1.6)";
                case "stamina": return "Pemulihan stamina +" + effVal + "%";
                case "gold": return "Gold hadiah quest +" + effVal + "%";
                case "exp": return "EXP hadiah quest +" + effVal + "%";
                case "maxmana": return "Mana maksimum +" + effVal;
                case "manaregen": return "Pemulihan mana +" + effVal + "%";
                case "spellpower": return "Damage sihir +" + effVal + "%";
                case "manacost": return "Biaya mana -" + effVal + "%";
                case "cdr": return "Cooldown skill -" + effVal + "%";
                case "manakill": return "Pulihkan " + effVal + " mana setiap monster dikalahkan";
            }
            return "";
        }

        string SocketLine()
        {
            if (GameState.I == null || slot == "Shield" || slot == "Neck" || slot == "Ring") return "";
            int sc = Sockets.SocketCount(id); if (sc <= 0) return "";
            return "\nSocket (" + sc + "): " + Sockets.Summary(GameState.I, id) + "(pasang di Blacksmith)";
        }

        public string Desc()
        {
            switch (type)
            {
                case ItemType.Food: return "Makanan. " + Describe() + ".";
                case ItemType.Drink: return "Minuman. " + Describe() + ".";
                case ItemType.Material: if (id.StartsWith("gem_")) return Sockets.GemDesc(id); if (id.StartsWith("pick_")) { foreach (var pk in Activities.Picks) if (pk.id == id) return "Beliung. Dipakai otomatis saat menambang (yang terbaik di tas).\nBijih +" + pk.bonus + " per tambang, stamina " + pk.stam + ", peluang permata +" + Mathf.RoundToInt(pk.gem * 100) + "%."; } if (id.StartsWith("rod_")) { foreach (var r in FishCook.Rods) if (r.id == id) return "Joran. Dipakai otomatis saat memancing (yang terbaik di tas).\nGigitan bertahan " + r.window + " detik, menunggu " + Mathf.RoundToInt((1f - r.waitMul) * 100) + "% lebih cepat, tarikan lebih mudah."; } return "Bahan dari monster atau pedagang. Dijual di Guild " + sellPrice + "g. Dipakai untuk craft gear.";
                case ItemType.Consumable: return !string.IsNullOrEmpty(note) ? note : id == "mana_potion" ? "Memulihkan 50% mana maksimum (min. 40). Pakai lewat quickbar atau tombol Pakai." : id == "potion" ? "Memulihkan 60 HP." : "Item persiapan hunt. Dipakai saat berburu.";
                case ItemType.Attachment: return "Attachment " + attachKind + ". Dipasang ke slot attachment gear yang sedang dipakai.\nATK " + atk + "   DEF " + def + (effect != "" ? "\nEfek: " + EffectText() : "") + (job == "Mage" ? "\nKHUSUS PENYIHIR (Tier " + tier + "): efek tidak aktif untuk job lain." : "");
                case ItemType.QuestItem: return (buff != "" ? buff + "\n\n" : "") + "Item quest. Tidak bisa dijual atau dibuang, dan tidak memakan slot tas.";
                case ItemType.Gear: return (evoReq != "" ? "KHUSUS KELAS: " + JobInfo.EvoName(evoReq) + "\n" : "") + "Slot " + slot + (job == "Any" ? "" : ", khusus " + JobInfo.Name(job)) + "\nATK " + atk + "   DEF " + def + (mana > 0 ? "   Mana +" + mana : "") + (AllEffects() != "" ? "\nEfek: " + AllEffects() : "") + (tier >= 2 ? "\nGear Tier " + tier + " (bisa di-enchant di Blacksmith)" : "") + SocketLine();
            }
            return "";
        }
    }

    /// <summary>Database item sederhana (hardcoded). Ganti ke ScriptableObject saat sistem item sudah matang.</summary>
    public static class ItemDB
    {
        static readonly Dictionary<string, ItemDef> items = new Dictionary<string, ItemDef>();
        static readonly List<ItemDef> ordered = new List<ItemDef>();

        static ItemDB()
        {
            // Makanan (GDD 9: 5-10 gold)
            Add("bread", "Roti Bekal", ItemType.Food, 5, 1, 20, 0, "");
            FishCook.Register();
            Add("herbal_stew", "Herbal Stew", ItemType.Food, 7, 2, 25, 0, "Regen HP kecil");
            Add("roasted_fish", "Roasted Fish", ItemType.Food, 8, 2, 30, 10, "");
            Add("grilled_meat", "Grilled Meat", ItemType.Food, 10, 3, 40, 0, "Regen Stamina");
            // Minuman (GDD 9: 3-8 gold)
            Add("water", "Air Minum", ItemType.Drink, 3, 0, 0, 20, "");
            Add("herbal_tea", "Teh Herbal", ItemType.Drink, 4, 1, 0, 30, "");
            Add("berry_juice", "Berry Juice", ItemType.Drink, 6, 2, 0, 40, "");
            // Bahan
            Add("wolf_pelt", "Wolf Pelt", ItemType.Material, 0, 12, 0, 0, "");
            Add("boar_tusk", "Boar Tusk", ItemType.Material, 0, 15, 0, 0, "");
            Add("monster_bone", "Monster Bone", ItemType.Material, 0, 6, 0, 0, "");
            Add("orc_tusk", "Orc Tusk", ItemType.Material, 0, 25, 0, 0, "");
            Add("spider_silk", "Spider Silk", ItemType.Material, 0, 20, 0, 0, "");
            Add("herb", "Herb", ItemType.Material, 0, 3, 0, 0, "");
            // hasil buruan hewan ternak (dijual ke Penilai atau diantar ke NPC)
            Add("raw_chicken", "Daging Ayam", ItemType.Material, 0, 10, 0, 0, "");
            Add("raw_mutton", "Daging Domba", ItemType.Material, 0, 18, 0, 0, "");
            Add("raw_goat", "Daging Kambing", ItemType.Material, 0, 16, 0, 0, "");
            Add("raw_beef", "Daging Sapi", ItemType.Material, 0, 28, 0, 0, "");
            Add("feather", "Bulu Ayam", ItemType.Material, 0, 4, 0, 0, "");
            Add("wool", "Wol", ItemType.Material, 0, 10, 0, 0, "");
            Add("cow_hide", "Kulit Sapi", ItemType.Material, 0, 14, 0, 0, "");
            Add("leather", "Leather", ItemType.Material, 20, 8, 0, 0, "");
            Add("iron_ingot", "Iron Ingot", ItemType.Material, 30, 10, 0, 0, "");
            Add("slime_jelly", "Slime Jelly", ItemType.Material, 0, 4, 0, 0, "");
            Add("goblin_ear", "Goblin Ear", ItemType.Material, 0, 8, 0, 0, "");
            Add("wolf_fang", "Wolf Fang", ItemType.Material, 0, 30, 0, 0, "");
            Add("mushroom", "Mushroom", ItemType.Material, 0, 4, 0, 0, "");
            Add("cactus_fruit", "Buah Kaktus", ItemType.Material, 0, 5, 0, 0, "");
            Add("scorpion_stinger", "Sengat Skorpion", ItemType.Material, 0, 28, 0, 0, "");
            Add("lizard_hide", "Kulit Kadal Pasir", ItemType.Material, 0, 9, 0, 0, "");
            Add("mummy_wrap", "Perban Kuno", ItemType.Material, 0, 11, 0, 0, "");
            Add("golem_core", "Inti Golem Pasir", ItemType.Material, 0, 60, 0, 0, "");
            Add("wild_berry", "Wild Berry", ItemType.Material, 0, 3, 0, 0, "");
            Add("swamp_lotus", "Teratai Rawa", ItemType.Material, 0, 6, 0, 0, "");
            Add("grave_dust", "Debu Kuburan", ItemType.Material, 0, 5, 0, 0, "");
            Add("ectoplasm", "Ektoplasma", ItemType.Material, 0, 15, 0, 0, "");
            Add("hex_cloth", "Kain Kutukan", ItemType.Material, 0, 10, 0, 0, "");
            Add("troll_hide", "Kulit Troll Rawa", ItemType.Material, 0, 48, 0, 0, "");
            Add("necro_core", "Inti Nekromancer", ItemType.Material, 0, 95, 0, 0, "");
            // Area hunting Eldrath
            Add("ember_scale", "Sisik Bara", ItemType.Material, 0, 22, 0, 0, "");
            Add("magma_core", "Inti Magma", ItemType.Material, 0, 70, 0, 0, "");
            Add("brimstone", "Belerang", ItemType.Material, 0, 16, 0, 0, "");
            Add("ancient_bone", "Tulang Kuno", ItemType.Material, 0, 24, 0, 0, "");
            Add("king_crown_shard", "Pecahan Mahkota Tulang", ItemType.Material, 0, 110, 0, 0, "");
            Add("behemoth_horn", "Tanduk Behemoth", ItemType.Material, 0, 90, 0, 0, "");
            Add("cerberus_fang", "Taring Cerberus", ItemType.Material, 0, 120, 0, 0, "");
            Add("demon_horn", "Tanduk Iblis", ItemType.Material, 0, 36, 0, 0, "");
            Add("hellfire_ash", "Abu Neraka", ItemType.Material, 0, 28, 0, 0, "");
            Add("demon_heart", "Jantung Iblis", ItemType.Material, 0, 130, 0, 0, "");
            Add("ogre_tusk", "Taring Ogre", ItemType.Material, 0, 30, 0, 0, "");
            Add("road_pelt", "Kulit Serigala Jalan", ItemType.Material, 0, 14, 0, 0, "");
            // Consumable (persiapan quest)
            Add("paintball", "Pewarna (Paintball)", ItemType.Consumable, 15, 5, 0, 0, "");
            Add("flare", "Flare / Signal Scroll", ItemType.Consumable, 25, 8, 0, 0, "");
            Add("potion", "Ramuan Penyembuh", ItemType.Consumable, 30, 10, 0, 0, "");
            // Gear Tier 1. Stat mengikuti GDD 10.2 (ATK senjata ~ batas atas partner rank F: 28; DEF set lengkap ~16).
            AddGear("hunter_blade", "Hunter Blade", "Weapon", "Swordsman", 18, 0);     // craft
            AddGear("bone_bow", "Bone Bow", "Weapon", "Archer", 18, 0);                 // craft
            AddGear("wolfhide_armor", "Wolfhide Armor", "Body", "Any", 0, 9);           // craft (+5% speed nanti)
            AddGear("leather_cap", "Leather Cap", "Head", "Any", 0, 3);                 // craft
            AddGear("iron_sword", "Iron Sword", "Weapon", "Swordsman", 20, 0);          // toko
            AddGear("hunters_bow", "Hunter's Bow", "Weapon", "Archer", 20, 0);          // toko
            AddGear("leather_armor", "Leather Armor", "Body", "Any", 0, 10);            // toko
            AddGear("iron_cap", "Iron Cap", "Head", "Any", 0, 4);                       // toko
            AddGear("leather_boots", "Leather Boots", "Legs", "Any", 0, 5);             // toko
            // Gear lanjutan (tampil di tubuh karakter lewat NpcKit.Armor / NpcKit.Weapon)
            AddGear("knight_sword", "Knight Sword", "Weapon", "Swordsman", 26, 0);       // toko
            AddGear("long_bow", "Long Bow", "Weapon", "Archer", 26, 0);                  // toko
            AddGear("iron_armor", "Iron Plate Armor", "Body", "Any", 0, 16);             // toko
            AddGear("knight_armor", "Knight Armor", "Body", "Any", 0, 22);               // toko
            AddGear("knight_helm", "Knight Helm", "Head", "Any", 0, 8);                  // toko
            AddGear("iron_boots", "Iron Greaves", "Legs", "Any", 0, 9);                  // toko
            // Tongkat sihir (Penyihir Api / Penyihir Tanah)
            AddGear("tide_rod", "Tongkat Arus", "Weapon", "WaterMage", 18, 0);
            AddGear("frost_staff", "Tongkat Es", "Weapon", "WaterMage", 20, 0);
            AddGear("abyss_staff", "Tongkat Samudra", "Weapon", "WaterMage", 26, 0);
            AddGear("breeze_rod", "Tongkat Angin", "Weapon", "WindMage", 18, 0);
            AddGear("gale_staff", "Tongkat Badai", "Weapon", "WindMage", 20, 0);
            AddGear("tempest_staff", "Tongkat Tempest", "Weapon", "WindMage", 26, 0);
            AddGear("prayer_rod", "Tongkat Doa", "Weapon", "Priest", 16, 0);
            AddGear("sun_staff", "Tongkat Surya", "Weapon", "Priest", 19, 0);
            AddGear("seraph_staff", "Tongkat Serafim", "Weapon", "Priest", 25, 0);
            AddGear("ember_rod", "Tongkat Bara", "Weapon", "FireMage", 18, 0);          // craft (bahan sihir rawa)
            AddGear("flame_staff", "Tongkat Api", "Weapon", "FireMage", 20, 0);         // toko
            AddGear("inferno_staff", "Tongkat Inferno", "Weapon", "FireMage", 26, 0);   // toko
            AddGear("oak_staff", "Tongkat Ek Tua", "Weapon", "EarthMage", 18, 0);       // craft (inti golem)
            AddGear("stone_staff", "Tongkat Batu", "Weapon", "EarthMage", 20, 0);       // toko
            AddGear("terra_staff", "Tongkat Terra", "Weapon", "EarthMage", 26, 0);      // toko
            // Slot tambahan: sarung tangan, cincin, kalung
            AddGear("leather_gloves", "Leather Gloves", "Hands", "Any", 0, 3);
            AddGear("iron_gauntlet", "Iron Gauntlet", "Hands", "Any", 0, 7);
            Mana("ember_rod", 15); Mana("flame_staff", 30); Mana("inferno_staff", 60);
            Mana("oak_staff", 15); Mana("stone_staff", 30); Mana("terra_staff", 60);
            AddGear("ring_sage", "Cincin Cendekia", "Ring", "Any", 0, 0); Mana("ring_sage", 30);
            AddGear("amulet_sage", "Kalung Bijak", "Neck", "Any", 0, 2); Mana("amulet_sage", 55);
            Add("mana_potion", "Ramuan Mana", ItemType.Consumable, 40, 14, 0, 0, "");
            AddGear("ring_power", "Cincin Kekuatan", "Ring", "Any", 4, 0);
            AddGear("ring_guard", "Cincin Penjaga", "Ring", "Any", 0, 4);
            AddGear("ring_hero", "Cincin Pahlawan", "Ring", "Any", 8, 2);
            AddGear("amulet_vigor", "Kalung Vitalitas", "Neck", "Any", 0, 5);
            AddGear("amulet_fang", "Kalung Taring", "Neck", "Any", 6, 0);
            // Attachment (dipasang ke slot attachment gear; tidak terpakai habis saat dilepas)
            AddAtt("att_whet", "Batu Asah", "Serangan", 3, 0, 120);
            AddAtt("att_fang", "Taring Tajam", "Serangan", 6, 0, 300);
            AddAtt("att_rune_atk", "Rune Kekuatan", "Serangan", 10, 0, 700);
            AddAtt("att_plate", "Pelat Besi", "Pertahanan", 0, 3, 120);
            AddAtt("att_guard", "Lapis Penjaga", "Pertahanan", 0, 6, 300);
            AddAtt("att_rune_def", "Rune Perlindungan", "Pertahanan", 0, 10, 700);
            AddAtt("att_charm", "Jimat Seimbang", "Utility", 2, 2, 250);
            AddAtt("att_charm2", "Jimat Pemburu", "Utility", 4, 4, 600);
            // Attachment efek khusus: tidak dijual, didapat dari drop monster / hadiah quest
            AddAtt("att_vamp", "Taring Vampir", "Serangan", 4, 0, 900, "lifesteal", 4f);
            AddAtt("att_keen", "Mata Elang", "Serangan", 3, 0, 800, "crit", 8f);
            AddAtt("att_swift", "Bulu Angin", "Utility", 0, 2, 700, "stamina", 25f);
            AddAtt("att_coin", "Koin Hoki", "Utility", 0, 0, 700, "gold", 10f);
            AddAtt("att_scroll", "Gulungan Ilmu", "Utility", 0, 0, 700, "exp", 8f);
            AddAtt("att_mana", "Permata Mana", "Utility", 0, 0, 800, "maxmana", 25f);
            AddAtt("att_focus", "Batu Fokus", "Utility", 0, 0, 800, "manaregen", 30f);
            // Attachment khusus penyihir (kind "Sihir"), 3 tier. T1-T2 dijual di Toko Umum (hanya untuk penyihir), T3 hanya drop langka.
            MagicAtt("att_m_power", "Kristal Kekuatan", "spellpower", 6f, 12f, 20f);
            MagicAtt("att_m_core", "Inti Mana", "maxmana", 40f, 90f, 160f);
            MagicAtt("att_m_flow", "Batu Aliran", "manaregen", 40f, 80f, 140f);
            MagicAtt("att_m_thrift", "Sigil Hemat", "manacost", 5f, 10f, 16f);
            MagicAtt("att_m_hourglass", "Jam Pasir Arkana", "cdr", 4f, 8f, 13f);
            MagicAtt("att_m_siphon", "Pecahan Penyerap", "manakill", 3f, 6f, 10f);
            // Item quest
            GearTiers.Register();
            HiddenBosses.Register();
            TradeMarket.Register();
            Combat2.Register();
            AddQ("guild_card", "Kartu Guild", "Tanda pengenal hunter Guild. Dibawa terus.");
            AddQ("quest_letter", "Surat Quest", "Surat tugas dari resepsionis untuk quest yang sedang aktif.");
            AddQ("quest_proof", "Bukti Penyelesaian", "Bukti quest selesai. Serahkan ke resepsionis untuk klaim hadiah.");
        }

        static void Add(string id, string name, ItemType type, int buy, int sell, int hunger, int thirst, string buff)
        {
            var d = new ItemDef { id = id, name = name, type = type, buyPrice = buy, sellPrice = sell, hunger = hunger, thirst = thirst, buff = buff };
            if (type == ItemType.Material) d.stack = 20;
            items[id] = d;
            ordered.Add(d);
        }

        static void Mana(string id, int n) { ItemDef d; if (items.TryGetValue(id, out d)) d.mana = n; }

        static void AddQ(string id, string name, string desc)
        {
            var d = new ItemDef { id = id, name = name, type = ItemType.QuestItem, stack = 1, buff = desc };
            items[id] = d;
            ordered.Add(d);
        }

        static void AddAtt(string id, string name, string kind, int atk, int def, int price, string effect = "", float effVal = 0f)
        {
            var d = new ItemDef { id = id, name = name, type = ItemType.Attachment, attachKind = kind, atk = atk, def = def, buyPrice = price, sellPrice = price / 4, stack = 10, effect = effect, effVal = effVal };
            items[id] = d;
            ordered.Add(d);
        }

        static readonly string[] TierRoman = { "", "I", "II", "III" };
        static readonly int[] TierPrice = { 0, 500, 1600, 4200 };

        static void MagicAtt(string id, string name, string effect, float v1, float v2, float v3)
        {
            float[] v = { 0f, v1, v2, v3 };
            for (int t = 1; t <= 3; t++)
            {
                var d = new ItemDef { id = id + "_" + t, name = name + " " + TierRoman[t], type = ItemType.Attachment, attachKind = "Sihir", job = "Mage", tier = t, atk = t - 1, def = 0,
                    buyPrice = TierPrice[t], sellPrice = TierPrice[t] / 4, stack = 10, effect = effect, effVal = v[t] };
                items[d.id] = d;
                ordered.Add(d);
            }
        }

        static void AddGear(string id, string name, string slot, string job, int atk, int def)
        {
            var d = new ItemDef { id = id, name = name, type = ItemType.Gear, slot = slot, job = job, atk = atk, def = def, stack = 1 };
            items[id] = d;
            ordered.Add(d);
        }

        /// <summary>Daftarkan item gear dari luar (GearTiers).</summary>
        public static void AddRaw(ItemDef d) { items[d.id] = d; ordered.Add(d); }

        public static ItemDef Get(string id)
        {
            ItemDef d;
            return id != null && items.TryGetValue(id, out d) ? d : null;
        }

        public static List<ItemDef> OfType(ItemType t)
        {
            var l = new List<ItemDef>();
            foreach (var d in ordered) if (d.type == t) l.Add(d);
            return l;
        }
    }
}
