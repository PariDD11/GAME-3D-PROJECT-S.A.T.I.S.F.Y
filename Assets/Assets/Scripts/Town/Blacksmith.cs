using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Blacksmith: craft gear Tier 1 dari bagian monster, repair, dan beli bahan dasar (GDD 10).</summary>
    public class Blacksmith : Interactable
    {
        class Recipe
        {
            public string result, slot, job, effect;
            public int gold;
            public Dictionary<string, int> mats;
        }

        static readonly Recipe[] Recipes =
        {
            new Recipe { result = "wolfhide_armor", slot = "Body", job = "Any", gold = 150, effect = "DEF set Tier 1, +5% kecepatan gerak",
                mats = new Dictionary<string, int> { { "wolf_pelt", 3 }, { "leather", 1 } } },
            new Recipe { result = "leather_cap", slot = "Head", job = "Any", gold = 100, effect = "DEF dasar Tier 1",
                mats = new Dictionary<string, int> { { "leather", 3 } } },
            new Recipe { result = "hunter_blade", slot = "Weapon", job = "Swordsman", gold = 120, effect = "ATK Tier 1",
                mats = new Dictionary<string, int> { { "boar_tusk", 2 }, { "iron_ingot", 1 } } },
            new Recipe { result = "ember_rod", slot = "Weapon", job = "FireMage", gold = 130, effect = "ATK Tier 1, tongkat bara dari sihir rawa",
                mats = new Dictionary<string, int> { { "hex_cloth", 3 }, { "ectoplasm", 1 } } },
            new Recipe { result = "oak_staff", slot = "Weapon", job = "EarthMage", gold = 130, effect = "ATK Tier 1, tongkat berinti batu gurun",
                mats = new Dictionary<string, int> { { "golem_core", 1 }, { "monster_bone", 3 } } },
            new Recipe { result = "tide_rod", slot = "Weapon", job = "WaterMage", gold = 130, effect = "ATK Tier 1, tongkat arus dari bahan rawa",
                mats = new Dictionary<string, int> { { "hex_cloth", 3 }, { "ectoplasm", 1 } } },
            new Recipe { result = "breeze_rod", slot = "Weapon", job = "WindMage", gold = 130, effect = "ATK Tier 1, tongkat angin dari bahan gurun",
                mats = new Dictionary<string, int> { { "golem_core", 1 }, { "monster_bone", 3 } } },
            new Recipe { result = "prayer_rod", slot = "Weapon", job = "Priest", gold = 130, effect = "ATK Tier 1, tongkat doa",
                mats = new Dictionary<string, int> { { "herb", 5 }, { "monster_bone", 2 } } },
            new Recipe { result = "bone_bow", slot = "Weapon", job = "Archer", gold = 130, effect = "ATK Tier 1, durability lebih rendah",
                mats = new Dictionary<string, int> { { "monster_bone", 5 }, { "leather", 1 } } },
        };

        public class RecipeRow { public string result, slot, job, area; public int gold; public Dictionary<string, int> mats; }

        /// <summary>Semua resep craft (Tier 1 + Tier 2-5) untuk daftar di jurnal.</summary>
        public static List<RecipeRow> AllRecipes()
        {
            var l = new List<RecipeRow>();
            foreach (var r in Recipes) l.Add(new RecipeRow { result = r.result, slot = r.slot, job = r.job, gold = r.gold, mats = r.mats, area = "Tier 1 (Desa/Kota)" });
            foreach (var r in GearTiers.Recipes)
                l.Add(new RecipeRow { result = r.result, slot = r.slot, job = r.job, gold = r.gold, mats = r.mats,
                    area = r.area >= 0 && r.area < GearTiers.AreaNames.Length ? GearTiers.AreaNames[r.area] : "Tier 2+" });
            return l;
        }

        /// <summary>Monster yang menjatuhkan bahan ini (maks 3 nama).</summary>
        public static string SourceOf(string matId)
        {
            var names = new List<string>();
            foreach (var d in MonsterDB.All())
            {
                if (d == null || names.Count >= 3) break;
                bool has = d.rare == matId;
                if (d.loot != null) foreach (var x in d.loot) if (x == matId) has = true;
                if (has) names.Add(d.name);
            }
            return names.Count == 0 ? "toko / hadiah quest / peti" : string.Join(", ", names);
        }

        public override void Interact() { ShopHub.Open(this, OpenMain, "Lihat layanan pandai besi."); }

        void OpenMain()
        {
            var gs = GameState.I;
            TownUI.I.Open("Blacksmith",
                () => "Gold: " + gs.gold + "   Job: " + JobInfo.Name(gs.job) + "\nCraft gear dari bagian monster. Gear craft murah tapi durability lebih rendah dari toko.",
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Daftar Resep (juga di Jurnal > RESEP) >", () => { TownUI.I.Close(); if (JournalUI.I != null) JournalUI.I.ShowTab(7); }));
                    l.Add(new UIEntry("Toko Alkimia: pelapis elemen & penawar >", Combat2.OpenShop));
                    l.Add(new UIEntry("Craft gear Tier 1 >", OpenCraft));
                    l.Add(new UIEntry("Craft gear area baru (Tier 2-5) >", OpenTierList));
                    l.Add(new UIEntry("Senjata kelas evolusi >", OpenEvoWeapons));
                    l.Add(new UIEntry("Evolusi & Level Senjata (Lv 0-50) >", () => WeaponLevel.OpenMenu(OpenMain)));
                    l.Add(new UIEntry("Enchant gear (+1 s/d +10) >", OpenEnchant));
                    l.Add(new UIEntry("Imbue senjata (efek tambahan) >", OpenImbue));
                    int cost = RepairCost(gs);
                    l.Add(new UIEntry(cost > 0 ? "Repair semua gear (" + cost + "g)" : "Repair (semua gear masih bagus)", () => Repair(cost), cost > 0));
                    l.Add(new UIEntry("Socket permata (Rubi, Safir, Zamrud, Topas) >", () => Sockets.OpenMenu(OpenMain)));
                    int ore = gs.Count("iron_ore");
                    l.Add(new UIEntry("Lebur bijih: 2 Bijih Besi + 5g = 1 Iron Ingot   (punya " + ore + " bijih)", () => Smelt(), ore >= 2 && gs.gold >= 5));
                    l.Add(new UIEntry("Beli bahan dasar >", OpenShop));
                    return l;
                });
        }

        void Smelt()
        {
            var gs = GameState.I; int n = Mathf.Min(gs.Count("iron_ore") / 2, gs.gold / 5);
            if (n <= 0) return;
            if (!gs.HasSpace("iron_ingot", n)) { TownUI.I.Toast("Tas penuh."); return; }
            gs.RemoveItem("iron_ore", n * 2); gs.TrySpend(n * 5); gs.AddItem("iron_ingot", n);
            Audio.Sfx("gather", transform.position); TownUI.I.Toast("Meleburkan " + n * 2 + " bijih jadi Iron Ingot x" + n + ".");
            OpenMain();
        }

        void OpenCraft()
        {
            var gs = GameState.I;
            TownUI.I.Open("Craft Gear (Tier 1)",
                () => "Gold: " + gs.gold + "\nBahan monster didapat dari hunt. (Debug: tekan F9 untuk bahan uji.)",
                () =>
                {
                    var l = new List<UIEntry>();
                    foreach (var r in Recipes)
                    {
                        var recipe = r;
                        var def = ItemDB.Get(recipe.result);
                        bool jobOk = recipe.job == "Any" || recipe.job == gs.job;
                        bool has = jobOk && gs.gold >= recipe.gold;
                        string req = "";
                        foreach (var m in recipe.mats)
                        {
                            var md = ItemDB.Get(m.Key);
                            if (gs.Count(m.Key) < m.Value) has = false;
                            req += md.name + " " + gs.Count(m.Key) + "/" + m.Value + "  ";
                        }
                        string label = def.name + " [" + recipe.slot + (jobOk ? "" : ", " + JobInfo.Name(recipe.job) + " saja") + "]  " + recipe.gold + "g\n" + req;
                        l.Add(new UIEntry(label, () => Craft(recipe), has));
                    }
                    l.Add(new UIEntry("< Kembali", OpenMain));
                    return l;
                });
        }

        // ---------------- Tier area baru ----------------
        void OpenTierList()
        {
            var gs = GameState.I;
            TownUI.I.Open("Craft Gear Area Baru",
                () => "Gold: " + gs.gold + "\nPilih area asal bahan. Senjata otomatis sesuai job-mu (" + JobInfo.Name(gs.job) + ").\nSenjata unik boss tidak bisa dicraft; hanya jatuh dari bosnya.",
                () =>
                {
                    var l = new List<UIEntry>();
                    for (int i = 0; i < GearTiers.AreaNames.Length; i++)
                    {
                        int a = i;
                        l.Add(new UIEntry("Tier " + (a + 2) + " - bahan " + GearTiers.AreaNames[a] + " >", () => OpenTier(a)));
                    }
                    l.Add(new UIEntry("< Kembali", OpenMain));
                    return l;
                });
        }

        void OpenEvoWeapons()
        {
            var gs = GameState.I;
            TownUI.I.Open("Senjata Kelas Evolusi",
                () => "Gold: " + gs.gold + "\nKelas: " + JobInfo.Display(gs) + "\nSenjata khusus kelas evolusi Lv.15 dan Lv.30. Bentuk dan efeknya unik untuk tiap kelas.",
                () =>
                {
                    var l = new List<UIEntry>();
                    bool any = false;
                    foreach (var r in GearTiers.Recipes)
                    {
                        if (r.area < 10) continue;
                        var def = ItemDB.Get(r.result);
                        if (def == null || !GearTiers.EvoOk(gs, def) || r.job != gs.job) continue;
                        any = true;
                        var recipe = r;
                        bool has = gs.gold >= recipe.gold;
                        string req = "";
                        foreach (var m in recipe.mats)
                        {
                            if (gs.Count(m.Key) < m.Value) has = false;
                            req += ItemDB.Get(m.Key).name + " " + gs.Count(m.Key) + "/" + m.Value + "  ";
                        }
                        string stat = "ATK " + def.atk + (def.mana > 0 ? "  Mana +" + def.mana : "");
                        l.Add(new UIEntry(def.name + "  " + stat + "  " + recipe.gold + "g\n" + def.AllEffects().Replace("\n", " | ") + "\n" + req, () => CraftTier(recipe), has));
                    }
                    if (!any) l.Add(new UIEntry("Belum ada senjata kelas (pilih Evolusi Lv.15 / Lv.30 dulu)", null, false));
                    l.Add(new UIEntry("< Kembali", OpenMain));
                    return l;
                });
        }

        void OpenTier(int area)
        {
            var gs = GameState.I;
            TownUI.I.Open("Tier " + (area + 2) + " - " + GearTiers.AreaNames[area],
                () => "Gold: " + gs.gold,
                () =>
                {
                    var l = new List<UIEntry>();
                    foreach (var r in GearTiers.Recipes)
                    {
                        if (r.area != area) continue;
                        if (r.job != "Any" && r.job != gs.job) continue;
                        var recipe = r;
                        var def = ItemDB.Get(recipe.result);
                        bool has = gs.gold >= recipe.gold;
                        string req = "";
                        foreach (var m in recipe.mats)
                        {
                            if (gs.Count(m.Key) < m.Value) has = false;
                            req += ItemDB.Get(m.Key).name + " " + gs.Count(m.Key) + "/" + m.Value + "  ";
                        }
                        string stat = (def.atk > 0 ? "ATK " + def.atk : "DEF " + def.def) + (def.mana > 0 ? "  Mana +" + def.mana : "");
                        l.Add(new UIEntry(def.name + " [" + recipe.slot + "]  " + stat + "  " + recipe.gold + "g\n" + req, () => CraftTier(recipe), has));
                    }
                    l.Add(new UIEntry("< Kembali", OpenTierList));
                    return l;
                });
        }

        void CraftTier(GearRecipe r)
        {
            var gs = GameState.I;
            foreach (var m in r.mats) if (gs.Count(m.Key) < m.Value) return;
            if (!gs.TrySpend(r.gold)) return;
            foreach (var m in r.mats) gs.RemoveItem(m.Key, m.Value);
            gs.SetEquipment(r.slot, r.result, 100f, 100f);
            TownUI.I.Toast(ItemDB.Get(r.result).name + " selesai dibuat dan langsung dipakai.");
        }

        // ---------------- Enchant ----------------
        void OpenEnchant()
        {
            var gs = GameState.I;
            TownUI.I.Open("Enchant Gear",
                () => "Gold: " + gs.gold + "\nTingkatkan gear yang sedang dipakai sampai +" + GearTiers.MaxEnchant + ". Sampai +5 pasti berhasil; setelah itu bisa gagal (bahan & gold hangus, level tidak turun).",
                () =>
                {
                    var l = new List<UIEntry>();
                    foreach (var slot in GearTiers.EnchantSlots)
                    {
                        string sl = slot, id;
                        if (!gs.equipment.TryGetValue(sl, out id) || string.IsNullOrEmpty(id)) { l.Add(new UIEntry(sl + ": (kosong)", null, false)); continue; }
                        var def = ItemDB.Get(id);
                        int L = GearTiers.EnchantLevel(gs, id);
                        if (L >= GearTiers.MaxEnchant) { l.Add(new UIEntry(def.name + " +" + L + "  (MAKS)", null, false)); continue; }
                        int gold; var mats = GearTiers.EnchantMats(L, out gold);
                        bool ok = gs.gold >= gold; string req = "";
                        foreach (var m in mats) { if (gs.Count(m.Key) < m.Value) ok = false; req += ItemDB.Get(m.Key).name + " " + gs.Count(m.Key) + "/" + m.Value + "  "; }
                        l.Add(new UIEntry(def.name + " +" + L + " > +" + (L + 1) + "   peluang " + GearTiers.EnchantChance(L) + "%   " + gold + "g\n" + req, () => DoEnchant(sl), ok));
                    }
                    l.Add(new UIEntry("< Kembali", OpenMain));
                    return l;
                });
        }

        void DoEnchant(string slot)
        {
            var gs = GameState.I;
            string id;
            if (!gs.equipment.TryGetValue(slot, out id) || string.IsNullOrEmpty(id)) return;
            int L = GearTiers.EnchantLevel(gs, id);
            if (L >= GearTiers.MaxEnchant) return;
            int gold; var mats = GearTiers.EnchantMats(L, out gold);
            foreach (var m in mats) if (gs.Count(m.Key) < m.Value) return;
            if (!gs.TrySpend(gold)) return;
            foreach (var m in mats) gs.RemoveItem(m.Key, m.Value);
            if (Random.Range(0, 100) < GearTiers.EnchantChance(L))
            {
                gs.AddStat("enc_" + id);
                Audio.Sfx("victory");
                TownUI.I.Toast(ItemDB.Get(id).name + " naik ke +" + (L + 1) + "!");
            }
            else TownUI.I.Toast("Enchant gagal... bahan hangus. (" + ItemDB.Get(id).name + " tetap +" + L + ")");
        }

        // ---------------- Imbue ----------------
        void OpenImbue()
        {
            var gs = GameState.I;
            string wid; gs.equipment.TryGetValue("Weapon", out wid);
            TownUI.I.Open("Imbue Senjata",
                () => "Gold: " + gs.gold + "\nSenjata: " + (string.IsNullOrEmpty(wid) ? "(tidak ada)" : ItemDB.Get(wid).name) + "\nImbue saat ini: " + (string.IsNullOrEmpty(wid) ? "-" : GearTiers.ImbueNames[GearTiers.ImbueOf(gs, wid)]) + "\nBiaya: 1200g + Sisik Bara 3 + Tulang Kuno 2. Imbue baru menimpa yang lama.",
                () =>
                {
                    var l = new List<UIEntry>();
                    bool ok = !string.IsNullOrEmpty(wid) && gs.gold >= 1200 && gs.Count("ember_scale") >= 3 && gs.Count("ancient_bone") >= 2;
                    for (int i = 1; i < GearTiers.ImbueNames.Length; i++)
                    {
                        int t = i;
                        l.Add(new UIEntry(GearTiers.ImbueNames[t], () =>
                        {
                            if (!gs.TrySpend(1200)) return;
                            gs.RemoveItem("ember_scale", 3); gs.RemoveItem("ancient_bone", 2);
                            GearTiers.SetImbue(gs, wid, t);
                            TownUI.I.Toast("Senjata di-imbue: " + GearTiers.ImbueNames[t]);
                        }, ok));
                    }
                    l.Add(new UIEntry("< Kembali", OpenMain));
                    return l;
                });
        }

        void Craft(Recipe r)
        {
            var gs = GameState.I;
            foreach (var m in r.mats) if (gs.Count(m.Key) < m.Value) return;
            if (!gs.TrySpend(r.gold)) return;
            foreach (var m in r.mats) gs.RemoveItem(m.Key, m.Value);
            gs.SetEquipment(r.slot, r.result, 70f, 70f);   // gear craft: murah tapi durability lebih rendah (GDD 10.3)
            TownUI.I.Toast(ItemDB.Get(r.result).name + " selesai dibuat dan langsung dipakai.");
        }

        static int RepairCost(GameState gs)
        {
            float missing = 0f;
            foreach (var kv in gs.durability)
            {
                float max;
                if (!gs.maxDurability.TryGetValue(kv.Key, out max)) max = 100f;
                missing += max - kv.Value;
            }
            return Mathf.CeilToInt(missing * 0.4f);
        }

        void Repair(int cost)
        {
            var gs = GameState.I;
            if (!gs.TrySpend(cost)) return;
            foreach (var k in new List<string>(gs.durability.Keys))
            {
                float max;
                if (!gs.maxDurability.TryGetValue(k, out max)) max = 100f;
                gs.durability[k] = max;
            }
            TownUI.I.Toast("Semua gear diperbaiki.");
        }

        void OpenShop()
        {
            var gs = GameState.I;
            TownUI.I.Open("Bahan Dasar",
                () => "Gold: " + gs.gold,
                () =>
                {
                    var l = new List<UIEntry>();
                    foreach (var id in new[] { "leather", "iron_ingot" })
                    {
                        var def = ItemDB.Get(id);
                        string itemId = id;
                        int price = def.buyPrice;
                        l.Add(new UIEntry("Beli " + def.name + " (" + price + "g)   punya " + gs.Count(id),
                            () =>
                            {
                                if (!gs.HasSpace(itemId, 1)) { TownUI.I.Toast("Tas penuh."); return; }
                                if (gs.TrySpend(price)) gs.AddItem(itemId, 1);
                            }));
                    }
                    l.Add(new UIEntry("< Kembali", OpenMain));
                    return l;
                });
        }
    }
}
