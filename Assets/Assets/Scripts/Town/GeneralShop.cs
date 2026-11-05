using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Toko umum: gear siap pakai (mahal, durability 100%) dan item persiapan. Alternatif dari craft di Blacksmith.</summary>
    public class GeneralShop : Interactable
    {
        class Gear { public string id, slot, job; public int price; }

        static Gear G(string id, string slot, string job, int price) { return new Gear { id = id, slot = slot, job = job, price = price }; }

        static readonly Gear[] Stock =
        {
            G("iron_sword",    "Weapon", "Swordsman", 220),
            G("hunters_bow",   "Weapon", "Archer",    230),
            G("leather_armor", "Body",   "Any",       200),
            G("iron_cap",      "Head",   "Any",       150),
            G("leather_boots", "Legs",   "Any",       120),
            G("iron_boots",    "Legs",   "Any",       260),
            G("iron_armor",    "Body",   "Any",       480),
            G("knight_helm",   "Head",   "Any",       380),
            G("knight_armor",  "Body",   "Any",       900),
            G("knight_sword",  "Weapon", "Swordsman", 520),
            G("long_bow",      "Weapon", "Archer",    540),
            G("flame_staff",   "Weapon", "FireMage",  230),
            G("inferno_staff", "Weapon", "FireMage",  540),
            G("stone_staff",   "Weapon", "EarthMage", 230),
            G("terra_staff",   "Weapon", "EarthMage", 540),
            G("frost_staff",   "Weapon", "WaterMage", 230),
            G("abyss_staff",   "Weapon", "WaterMage", 540),
            G("gale_staff",    "Weapon", "WindMage",  230),
            G("tempest_staff", "Weapon", "WindMage",  540),
            G("sun_staff",     "Weapon", "Priest",    230),
            G("seraph_staff",  "Weapon", "Priest",    540),
            G("leather_gloves","Hands",  "Any",       110),
            G("iron_gauntlet", "Hands",  "Any",       340),
            G("ring_power",    "Ring",   "Any",       420),
            G("ring_guard",    "Ring",   "Any",       420),
            G("ring_hero",     "Ring",   "Any",       1100),
            G("amulet_vigor",  "Neck",   "Any",       380),
            G("amulet_fang",   "Neck",   "Any",       520),
            G("ring_sage",     "Ring",   "Any",       480),
            G("amulet_sage",   "Neck",   "Any",       650),
        };

        public override void Interact() { ShopHub.Open(this, OpenMain, "Lihat barang dagangan."); }

        void OpenMain()
        {
            var gs = GameState.I;
            TownUI.I.Open("Toko Umum",
                () => "Gold: " + gs.gold + "   Job: " + JobInfo.Name(gs.job) + "\nGear toko mahal tapi durability penuh dan tanpa bahan monster.",
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Gear siap pakai >", OpenGear));
                    l.Add(new UIEntry("Attachment gear >", OpenAttachments));
                    l.Add(new UIEntry("Ramuan Penyembuh - 30g   punya " + gs.Count("potion"), () => BuyItem("potion", 30)));
                    if (gs.IsMage) l.Add(new UIEntry("Ramuan Mana - 40g   punya " + gs.Count("mana_potion"), () => BuyItem("mana_potion", 40)));
                    l.Add(new UIEntry("Benih >", () => OpenSeeds()));
                    l.Add(new UIEntry("Cacing Tanah (umpan) - 3g   punya " + gs.Count("bait_worm"), () => BuyItem("bait_worm", 3)));
                    l.Add(new UIEntry("Umpan Emas (ikan langka) - 25g   punya " + gs.Count("bait_lure"), () => BuyItem("bait_lure", 25)));
                    foreach (var rd in FishCook.Rods) { var r2 = rd; l.Add(new UIEntry(r2.name + " (gigitan lebih lama, tarik lebih mudah) - " + r2.price + "g" + (gs.Count(r2.id) > 0 ? "   (dimiliki)" : ""), () => { if (gs.Count(r2.id) > 0) TownUI.I.Toast("Sudah punya."); else BuyItem(r2.id, r2.price); })); }
                    foreach (var pk in Activities.Picks) { var p2 = pk; l.Add(new UIEntry(p2.name + " (bijih +" + p2.bonus + ", hemat stamina, peluang permata naik) - " + p2.price + "g" + (gs.Count(p2.id) > 0 ? "   (dimiliki)" : ""), () => { if (gs.Count(p2.id) > 0) TownUI.I.Toast("Sudah punya."); else BuyItem(p2.id, p2.price); })); }
                    l.Add(new UIEntry("Pewarna / Paintball - 15g   punya " + gs.Count("paintball"), () => BuyItem("paintball", 15)));
                    return l;
                });
        }

        void OpenSeeds()
        {
            var gs = GameState.I; var se = GameCalendar.Current;
            TownUI.I.Open("Benih",
                () => "Gold: " + gs.gold + "   " + GameCalendar.Name(se) + "\nTanam di lahan Desa Pemula. Tiap benih hanya tumbuh di musimnya.",
                () =>
                {
                    var l = new List<UIEntry>();
                    foreach (var c in Activities.Crops)
                    {
                        var crop = c;
                        l.Add(new UIEntry("Benih " + crop.name + " - " + crop.seedPrice + "g   punya " + gs.Count(crop.seed) + (crop.seasons[(int)se] ? "" : "   (bukan musimnya)"), () => BuyItem(crop.seed, crop.seedPrice)));
                    }
                    l.Add(new UIEntry("< Kembali", OpenMain));
                    return l;
                });
        }

        void OpenGear()
        {
            var gs = GameState.I;
            TownUI.I.Open("Gear Siap Pakai",
                () => "Gold: " + gs.gold + "   Job: " + JobInfo.Name(gs.job),
                () =>
                {
                    var l = new List<UIEntry>();
                    foreach (var g in Stock)
                    {
                        var gear = g;
                        bool jobOk = gear.job == "Any" || gear.job == gs.job;
                        var def = ItemDB.Get(gear.id);
                        string label = def.name + " [" + gear.slot + (jobOk ? "" : ", " + JobInfo.Name(gear.job) + " saja") + "]  - " + gear.price + "g";
                        l.Add(new UIEntry(label, () => Buy(gear), jobOk && gs.gold >= gear.price));
                    }
                    l.Add(new UIEntry("< Kembali", OpenMain));
                    return l;
                });
        }

        void OpenAttachments()
        {
            var gs = GameState.I;
            TownUI.I.Open("Attachment Gear",
                () => "Gold: " + gs.gold + "\nAttachment dipasang di menu Tas (tab Equipment, pilih gear terpasang, tab Attachment). Slot terbuka bertahap sesuai level.",
                () =>
                {
                    var l = new List<UIEntry>();
                    foreach (var d in ItemDB.OfType(ItemType.Attachment))
                    {
                        if (d.attachKind == "Sihir") { if (!gs.IsMage || d.tier > 2) continue; }       // Sihir T1-T2 dijual (penyihir saja); T3 drop langka
                        else if (d.effect != "") continue;   // attachment efek khusus tidak dijual
                        var def = d;
                        l.Add(new UIEntry(def.name + " [" + def.attachKind + "]  ATK +" + def.atk + "  DEF +" + def.def + (def.effect != "" ? "  (" + def.EffectText() + ")" : "") + "  - " + def.buyPrice + "g   punya " + gs.Count(def.id),
                            () => BuyItem(def.id, def.buyPrice), gs.gold >= def.buyPrice));
                    }
                    l.Add(new UIEntry("< Kembali", OpenMain));
                    return l;
                });
        }

        void Buy(Gear g)
        {
            var gs = GameState.I;
            if (!gs.TrySpend(g.price)) return;
            gs.SetEquipment(g.slot, g.id, 100f, 100f);
            TownUI.I.Toast(ItemDB.Get(g.id).name + " dibeli dan langsung dipakai.");
        }

        void BuyItem(string id, int price)
        {
            var gs = GameState.I;
            if (!gs.HasSpace(id, 1)) { TownUI.I.Toast("Tas penuh."); return; }
            if (gs.TrySpend(price)) gs.AddItem(id, 1);
        }
    }
}
