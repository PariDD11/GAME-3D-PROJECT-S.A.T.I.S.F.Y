using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Data memancing dan memasak: item ikan/masakan, resep, efek makan, menu dapur.</summary>
    public static class FishCook
    {
        /// <summary>where = potongan nama scene (kosong = di mana saja); season = pengali bobot per musim [Semi, Panas, Gugur, Dingin]; rare = ikan langka (terbantu umpan).</summary>
        public class Fish { public string id; public int weight; public float diff; public string[] where = new string[0]; public float[] season = { 1, 1, 1, 1 }; public bool rare; public string place; }
        public static readonly Fish[] Fishes =
        {
            new Fish { id = "fish_small", weight = 50, diff = 0.3f, place = "di mana saja" },
            new Fish { id = "fish_carp", weight = 30, diff = 0.5f, where = new[] { "Town", "StartVillage", "HuntArea_G", "Tutorial", "Frontier" }, season = new float[] { 1.2f, 1.4f, 1f, 0.2f }, place = "sungai dan danau (Kota, Desa, Hutan); jarang di musim dingin" },
            new Fish { id = "fish_trout", weight = 16, diff = 0.75f, where = new[] { "Town", "StartVillage", "HuntArea_G", "Tutorial", "Frontier" }, season = new float[] { 1.3f, 0.3f, 1.3f, 1.6f }, rare = true, place = "air jernih (Hutan, Desa); musim sejuk" },
            new Fish { id = "fish_catfish", weight = 40, diff = 0.55f, where = new[] { "Swamp" }, place = "Rawa Terkutuk" },
            new Fish { id = "fish_oasis", weight = 40, diff = 0.5f, where = new[] { "Desert" }, season = new float[] { 1, 1.3f, 1, 0.7f }, place = "Oasis Gurun Zahar" },
            new Fish { id = "fish_magma", weight = 35, diff = 0.85f, where = new[] { "Volcano", "Demon", "Bone" }, rare = true, place = "Gunung Vulkanik dan Lembah Iblis" },
            new Fish { id = "fish_golden", weight = 3, diff = 1f, rare = true, place = "di mana saja, lebih sering malam" },
        };

        public class Bait { public string id, name; public float rareMul, waitMul; public int price; }
        public static readonly Bait[] Baits =
        {
            new Bait { id = "bait_lure", name = "Umpan Emas", rareMul = 3f, waitMul = 0.4f, price = 25 },
            new Bait { id = "bait_worm", name = "Cacing Tanah", rareMul = 1.6f, waitMul = 0.65f, price = 3 },
        };
        /// <summary>Umpan terbaik yang dimiliki (null bila tidak ada).</summary>
        public static Bait BestBait() { var gs = GameState.I; if (gs == null) return null; foreach (var b in Baits) if (gs.Count(b.id) > 0) return b; return null; }

        /// <summary>Joran: yang terbaik di tas dipakai otomatis. Tanpa joran = joran bambu bawaan.</summary>
        public class Rod { public string id, name; public float waitMul, window, diffMul, rareMul; public int price; }
        public static readonly Rod[] Rods =
        {
            new Rod { id = "rod_master", name = "Joran Master", waitMul = 0.7f, window = 2.2f, diffMul = 0.75f, rareMul = 1.3f, price = 650 },
            new Rod { id = "rod_fine", name = "Joran Halus", waitMul = 0.85f, window = 1.7f, diffMul = 0.88f, rareMul = 1.1f, price = 140 },
        };
        static readonly Rod DefaultRod = new Rod { id = "", name = "Joran Bambu", waitMul = 1f, window = 1.2f, diffMul = 1f, rareMul = 1f };
        public static Rod BestRod() { var gs = GameState.I; if (gs != null) foreach (var r in Rods) if (gs.Count(r.id) > 0) return r; return DefaultRod; }

        /// <summary>Ikan yang bisa didapat di scene ini pada musim sekarang (nama, dipisah koma).</summary>
        public static string CatchList(string scene)
        {
            var l = new List<string>();
            foreach (var f in Fishes) if (Weight(f, scene, 1f) > 0.5f) { var d = ItemDB.Get(f.id); l.Add(d != null ? d.name : f.id); }
            return string.Join(", ", l);
        }

        public class Recipe { public string name, output; public string[] ids; public int[] n; public int heal, stam; }
        public static readonly Recipe[] Recipes =
        {
            new Recipe { name = "Ikan Bakar", output = "roasted_fish", ids = new[] { "fish_small" }, n = new[] { 1 }, heal = 10 },
            new Recipe { name = "Sup Ikan", output = "fish_soup", ids = new[] { "fish_carp", "herb" }, n = new[] { 1, 1 }, heal = 40 },
            new Recipe { name = "Steak Ikan Trout", output = "fish_steak", ids = new[] { "fish_trout", "herb" }, n = new[] { 1, 1 }, heal = 30, stam = 30 },
            new Recipe { name = "Sup Jamur", output = "mushroom_soup", ids = new[] { "mushroom", "herb" }, n = new[] { 2, 1 }, heal = 25 },
            new Recipe { name = "Ayam Panggang", output = "grilled_chicken", ids = new[] { "raw_chicken" }, n = new[] { 1 }, heal = 15 },
            new Recipe { name = "Daging Sapi Panggang", output = "grilled_meat", ids = new[] { "raw_beef" }, n = new[] { 1 }, stam = 20 },
            new Recipe { name = "Domba Panggang", output = "grilled_meat", ids = new[] { "raw_mutton" }, n = new[] { 1 }, stam = 20 },
            new Recipe { name = "Pai Berry", output = "berry_pie", ids = new[] { "wild_berry", "herb" }, n = new[] { 3, 1 }, heal = 15 },
            new Recipe { name = "Roti Gandum", output = "bread", ids = new[] { "wheat" }, n = new[] { 2 } },
            new Recipe { name = "Rebusan Sayur", output = "veggie_stew", ids = new[] { "carrot", "tomato" }, n = new[] { 2, 1 }, heal = 30 },
            new Recipe { name = "Sup Labu", output = "pumpkin_soup", ids = new[] { "pumpkin", "herb" }, n = new[] { 1, 1 }, heal = 35, stam = 20 },
            new Recipe { name = "Bubur Daun Bawang", output = "leek_porridge", ids = new[] { "frost_leek", "wheat" }, n = new[] { 2, 1 }, heal = 30, stam = 20 },
            new Recipe { name = "Telur Dadar", output = "egg_omelet", ids = new[] { "egg", "herb" }, n = new[] { 2, 1 }, heal = 25 },
            new Recipe { name = "Puding Susu", output = "milk_pudding", ids = new[] { "milk", "strawberry" }, n = new[] { 1, 1 }, heal = 20, stam = 10 },
            new Recipe { name = "Jagung Bakar", output = "corn_roast", ids = new[] { "corn" }, n = new[] { 2 }, heal = 15 },
            new Recipe { name = "Jus Stroberi", output = "berry_juice", ids = new[] { "strawberry" }, n = new[] { 3 }, stam = 25 },
            new Recipe { name = "Salad Buah", output = "fruit_salad", ids = new[] { "melon", "strawberry" }, n = new[] { 1, 2 }, heal = 20, stam = 15 },
            new Recipe { name = "Pancake Susu", output = "pancake", ids = new[] { "wheat", "egg", "milk" }, n = new[] { 1, 1, 1 }, heal = 45, stam = 25 },
            new Recipe { name = "Pesta Emas", output = "golden_feast", ids = new[] { "fish_golden", "raw_beef", "herb" }, n = new[] { 1, 1, 1 }, heal = 100, stam = 60 },
        };

        public static void Register()
        {
            Activities.Register();
            Mat("fish_small", "Ikan Kecil", 6); Mat("fish_carp", "Ikan Mas", 14); Mat("fish_trout", "Ikan Trout", 26); Mat("fish_golden", "Ikan Emas", 90);
            Mat("fish_catfish", "Lele Rawa", 18); Mat("fish_oasis", "Ikan Oasis", 20); Mat("fish_magma", "Ikan Magma", 48);
            ItemDB.AddRaw(new ItemDef { id = "bait_worm", name = "Cacing Tanah", type = ItemType.Material, buyPrice = 3, sellPrice = 1, stack = 30 });
            ItemDB.AddRaw(new ItemDef { id = "bait_lure", name = "Umpan Emas", type = ItemType.Material, buyPrice = 25, sellPrice = 8, stack = 30 });
            ItemDB.AddRaw(new ItemDef { id = "rod_fine", name = "Joran Halus", type = ItemType.Material, buyPrice = 140, sellPrice = 50, stack = 1 });
            ItemDB.AddRaw(new ItemDef { id = "rod_master", name = "Joran Master", type = ItemType.Material, buyPrice = 650, sellPrice = 240, stack = 1 });
            Dish("fish_soup", "Sup Ikan", 45, 20, "Regen HP"); Dish("fish_steak", "Steak Ikan Trout", 55, 5, "Regen HP & Stamina");
            Dish("mushroom_soup", "Sup Jamur", 28, 10, "Regen HP"); Dish("grilled_chicken", "Ayam Panggang", 32, 0, "Regen HP");
            Dish("berry_pie", "Pai Berry", 30, 15, "Regen HP"); Dish("golden_feast", "Pesta Emas", 85, 30, "Regen HP & Stamina besar");
            Dish("veggie_stew", "Rebusan Sayur", 38, 15, "Regen HP"); Dish("pumpkin_soup", "Sup Labu", 42, 20, "Regen HP & Stamina"); Dish("leek_porridge", "Bubur Daun Bawang", 40, 15, "Regen HP & Stamina");
            Dish("egg_omelet", "Telur Dadar", 34, 0, "Regen HP"); Dish("milk_pudding", "Puding Susu", 26, 18, "Regen HP & Stamina"); Dish("corn_roast", "Jagung Bakar", 30, 0, "Regen HP");
            Dish("berry_juice", "Jus Stroberi", 10, 38, "Regen Stamina"); Dish("fruit_salad", "Salad Buah", 24, 30, "Regen HP & Stamina"); Dish("pancake", "Pancake Susu", 50, 12, "Regen HP & Stamina");
            Mat("egg", "Telur", 6); Mat("milk", "Susu", 9);
            Dish("burnt_food", "Masakan Gosong", 8, 0, "");
        }
        static void Mat(string id, string name, int sell) { ItemDB.AddRaw(new ItemDef { id = id, name = name, type = ItemType.Material, sellPrice = sell, stack = 20 }); }
        static void Dish(string id, string name, int hunger, int thirst, string buff) { ItemDB.AddRaw(new ItemDef { id = id, name = name, type = ItemType.Food, buyPrice = 0, sellPrice = Mathf.Max(1, hunger / 6), hunger = hunger, thirst = thirst, buff = buff }); }

        /// <summary>Dipanggil saat memakan item: pulihkan HP / stamina untuk masakan buatan sendiri.</summary>
        public static void OnEat(string id)
        {
            foreach (var r in Recipes)
                if (r.output == id)
                {
                    var pc = PlayerCombat.I; if (pc == null) return;
                    if (r.heal > 0) pc.hp = Mathf.Min(pc.MaxHp, pc.hp + r.heal);
                    if (r.stam > 0) pc.stamina = Mathf.Min(pc.MaxStamina, pc.stamina + r.stam);
                    return;
                }
        }

        public static Fish Roll()
        {
            return Roll(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, 1f);
        }

        public static float Weight(Fish f, string scene, float rareMul)
        {
            var gs = GameState.I; float night = gs != null && (gs.timeOfDay < 6f || gs.timeOfDay > 19f) ? 2f : 1f;
            if (f.where.Length > 0) { bool ok = false; foreach (var w in f.where) if (scene.Contains(w)) ok = true; if (!ok) return 0f; }
            else if (scene.Contains("Swamp") || scene.Contains("Desert") || scene.Contains("Volcano") || scene.Contains("Demon") || scene.Contains("Bone"))
            { if (f.id == "fish_small") return f.weight * 0.3f; }       // ikan kecil jarang di wilayah ekstrem
            float w2 = f.weight * f.season[(int)GameCalendar.Current];
            if (f.id == "fish_golden") w2 *= night;
            if (f.rare) w2 *= rareMul;
            return w2;
        }

        public static Fish Roll(string scene, float rareMul)
        {
            float total = 0f; foreach (var f in Fishes) total += Weight(f, scene, rareMul);
            if (total <= 0f) return Fishes[0];
            float r = Random.value * total;
            foreach (var f in Fishes) { r -= Weight(f, scene, rareMul); if (r <= 0f) return f; }
            return Fishes[0];
        }

        // ---------- menu dapur ----------
        public static void OpenCooking(string title, System.Action back)
        {
            var gs = GameState.I; var ui = TownUI.I;
            ui.Open(title,
                () => "Pilih resep. Bahan dari memancing, berburu, dan gathering.\nMini-game: tekan di zona hijau. Emas = 2 porsi, meleset = gosong.",
                () =>
                {
                    var l = new List<UIEntry>();
                    foreach (var rc in Recipes)
                    {
                        var r = rc; bool ok = true; var sb = new System.Text.StringBuilder();
                        for (int i = 0; i < r.ids.Length; i++)
                        {
                            int have = gs.Count(r.ids[i]); if (have < r.n[i]) ok = false;
                            var d = ItemDB.Get(r.ids[i]);
                            sb.Append(i > 0 ? ", " : "").Append(d != null ? d.name : r.ids[i]).Append(" ").Append(have).Append("/").Append(r.n[i]);
                        }
                        l.Add(new UIEntry(r.name + "   [" + sb + "]", () => Cook(r), ok));
                    }
                    l.Add(new UIEntry("< Kembali", () => { if (back != null) back(); }));
                    return l;
                });
        }

        static void Cook(Recipe r)
        {
            var gs = GameState.I;
            for (int i = 0; i < r.ids.Length; i++) if (gs.Count(r.ids[i]) < r.n[i]) return;
            for (int i = 0; i < r.ids.Length; i++) gs.RemoveItem(r.ids[i], r.n[i]);
            TownUI.I.Close();
            MiniGame.Timing("Memasak: " + r.name, 0.9f, 0.26f, res =>
            {
                string outId = res == 0 ? "burnt_food" : r.output; int qty = res == 2 ? 2 : 1;
                if (!gs.HasSpace(outId, qty)) { TownUI.I.Toast("Tas penuh, hasil masakan hilang."); return; }
                gs.AddItem(outId, qty);
                gs.AddStat("cooked"); if (res != 0) gs.AddStat("cc_" + outId);
                Audio.Sfx("gather", Vector3.zero);
                var d = ItemDB.Get(outId);
                TownUI.I.Toast(res == 0 ? "Gosong! " + d.name : res == 2 ? "Sempurna! " + d.name + " x2" : "Jadi: " + d.name);
            });
        }
    }
}
