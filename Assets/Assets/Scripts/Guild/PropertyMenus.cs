using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// Properti untuk Hunter: beli rumah di tiap kota (Desa Pemula, Kota, Eldrath), isi dengan furnitur dari toko furnitur,
    /// lalu tidur di rumah sendiri (gratis, pemulihan lebih baik makin nyaman). Kepemilikan disimpan di GameState.flags.
    /// Diakses lewat Resepsionis Guild ("Properti & Rumah").
    /// </summary>
    public static class PropertyMenus
    {
        public class House { public string id, town, name, desc; public int price, rankIdx, baseComfort; }
        public class Furn { public string id, name; public int price, comfort; }

        public static readonly House[] Houses =
        {
            new House { id = "vil_hut",   town = StarterConfig.VillageScene,   name = "Gubuk Tepi Sungai",   price = 300,   rankIdx = 0, baseComfort = 2,  desc = "Gubuk kayu kecil di Desa Pemula. Murah, cukup untuk tidur." },
            new House { id = "vil_cottage", town = StarterConfig.VillageScene, name = "Pondok Kebun",         price = 900,   rankIdx = 0, baseComfort = 3,  desc = "Pondok mungil dengan kebun kecil di tepi jalan Desa Pemula." },
            new House { id = "vil_house", town = StarterConfig.VillageScene,   name = "Rumah Warga Dua Lantai", price = 2400, rankIdx = 1, baseComfort = 6,  desc = "Rumah dua lantai dekat gereja. Luas dan hangat." },
            new House { id = "town_rent", town = GuildConfig.TownScene,        name = "Rumah Kontrakan",     price = 1200,  rankIdx = 0, baseComfort = 4,  desc = "Satu kamar dekat alun-alun kota." },
            new House { id = "town_stone",town = GuildConfig.TownScene,        name = "Rumah Batu",          price = 4500,  rankIdx = 1, baseComfort = 8,  desc = "Rumah batu dua kamar dengan halaman." },
            new House { id = "town_manor",town = GuildConfig.TownScene,        name = "Rumah Besar",         price = 12000, rankIdx = 3, baseComfort = 14, desc = "Rumah keluarga bangsawan kecil, banyak ruang." },
            new House { id = "eld_loft",  town = CityGate.DungeonCityScene,    name = "Kamar Loteng Eldrath",price = 2000,  rankIdx = 1, baseComfort = 5,  desc = "Loteng di dekat gerbang menara, praktis untuk Hunter dungeon." },
            new House { id = "eld_tower", town = CityGate.DungeonCityScene,    name = "Rumah Menara",        price = 8000,  rankIdx = 2, baseComfort = 11, desc = "Rumah tinggi dengan jendela menghadap menara." },
            new House { id = "eld_villa", town = CityGate.DungeonCityScene,    name = "Vila Penjelajah",     price = 25000, rankIdx = 4, baseComfort = 20, desc = "Vila mewah untuk Hunter papan atas." },
        };

        public static readonly Furn[] Shop =
        {
            new Furn { id = "bed1",   name = "Kasur Jerami",       price = 80,   comfort = 2 },
            new Furn { id = "bed2",   name = "Kasur Empuk",        price = 350,  comfort = 5 },
            new Furn { id = "bed3",   name = "Ranjang Kerajaan",   price = 1500, comfort = 10 },
            new Furn { id = "table",  name = "Meja Kayu",          price = 120,  comfort = 1 },
            new Furn { id = "chest",  name = "Lemari Pakaian",     price = 200,  comfort = 2 },
            new Furn { id = "shelf",  name = "Rak Buku",           price = 260,  comfort = 2 },
            new Furn { id = "rug",    name = "Karpet Anyaman",     price = 150,  comfort = 2 },
            new Furn { id = "lamp",   name = "Lampu Gantung",      price = 220,  comfort = 2 },
            new Furn { id = "plant",  name = "Tanaman Hias",       price = 60,   comfort = 1 },
            new Furn { id = "fire",   name = "Perapian Batu",      price = 500,  comfort = 4 },
            new Furn { id = "trophy", name = "Piala & Trofi",      price = 400,  comfort = 3 },
            new Furn { id = "tank",   name = "Akuarium",           price = 900,  comfort = 5 },
            new Furn { id = "stash",  name = "Peti Simpanan",      price = 300,  comfort = 1 },
            new Furn { id = "coop",   name = "Kandang Ayam",       price = 450,  comfort = 1 },
            new Furn { id = "sheep",  name = "Kandang Domba",      price = 900,  comfort = 1 },
            new Furn { id = "cow",    name = "Kandang Sapi",       price = 1300, comfort = 2 },
            new Furn { id = "paint",  name = "Lukisan Pemandangan",price = 350,  comfort = 2 },
            new Furn { id = "clock",  name = "Jam Dinding",        price = 280,  comfort = 1 },
            new Furn { id = "statue", name = "Patung Marmer",      price = 1100, comfort = 4 },
            new Furn { id = "rug2",   name = "Karpet Kerajaan",    price = 700,  comfort = 4 },
            new Furn { id = "vase",   name = "Vas Bunga Segar",    price = 140,  comfort = 1 },
        };

        // ---------------- helper ----------------
        public static string TownName(string scene)
        {
            if (scene == StarterConfig.VillageScene) return "Desa Pemula";
            if (scene == CityGate.DungeonCityScene) return "Kota Eldrath";
            return "Kota Guild";
        }

        public static string CurrentTown()
        {
            string sc = SceneManager.GetActiveScene().name;
            if (sc == StarterConfig.VillageScene || sc == CityGate.DungeonCityScene || sc == GuildConfig.TownScene) return sc;
            if (InteriorRoute.valid && !string.IsNullOrEmpty(InteriorRoute.returnScene)) return InteriorRoute.returnScene;
            return GuildConfig.TownScene;
        }

        static Furn FindFurn(string id) { foreach (var f in Shop) if (f.id == id) return f; return null; }
        public static House Find(string id) { foreach (var h in Houses) if (h.id == id) return h; return null; }
        public static bool HasFurn(House h, string furnId) { return GameState.I != null && GameState.I.flags.Contains("fur:" + h.id + ":" + furnId); }

        /// <summary>Penawaran beli dari pintu rumah (tanpa lewat agen).</summary>
        /// <summary>Perabot yang tampil di rumah contoh (tur): semakin mewah rumahnya, semakin lengkap.</summary>
        public static bool ShowroomHas(House h, string furnId)
        {
            switch (furnId)
            {
                case "table": case "rug": case "plant": return true;
                case "chest": case "lamp": return h.baseComfort >= 4;
                case "shelf": return h.baseComfort >= 5;
                case "fire": case "trophy": return h.baseComfort >= 11;
                case "tank": return h.baseComfort >= 14;
            }
            return false;
        }

        public static void OpenOffer(House h) { OpenOffer(h, null, true); }

        public static void OpenOffer(House h, Action back, bool canTour)
        {
            var gs = GameState.I;
            PropertyPreview.Show(h.id, h.name + (Owns(h) ? "  (milikmu)" : "  -  " + h.price + "g"));
            bool rankOk = GameState.RankIndex(gs.rank) >= h.rankIdx;
            TownUI.I.Open(h.name + " - DIJUAL",
                () => h.desc + "\nKenyamanan dasar " + h.baseComfort + "   Harga " + h.price + "g\nGold: " + gs.gold + "   Rank " + gs.rank + (rankOk ? "" : "\nButuh rank " + GameState.Ranks[h.rankIdx] + ".") + "\nRumah tidak bisa dijual kembali.",
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Beli rumah ini - " + h.price + "g", () =>
                    {
                        if (Owns(h) || !gs.TrySpend(h.price)) return;
                        gs.flags.Add(HouseFlag(h));
                        InteriorRoute.preview = false;
                        SaveSystem.Save();
                        TownUI.I.Close();
                        TownUI.I.Toast("Selamat! " + h.name + " sekarang milikmu. Tekan E di pintunya untuk masuk.");
                    }, !Owns(h) && rankOk && gs.gold >= h.price));
                    if (canTour && !Owns(h))
                        l.Add(new UIEntry("Lihat bagian dalam (tur rumah contoh)", () =>
                        {
                            if (!Application.CanStreamedLevelBeLoaded(InteriorInfo.Scene("Home"))) { TownUI.I.Toast("Scene 'Interior_Home' belum dibuat (Hunter Guild > Build Interiors)."); return; }
                            HomeDoor door = null;
                            foreach (var d in UnityEngine.Object.FindObjectsOfType<HomeDoor>()) if (d.houseId == h.id) { door = d; break; }
                            TownUI.I.Close();
                            Vector3 p = door != null ? door.transform.position : GameObject.FindGameObjectWithTag("Player") != null ? GameObject.FindGameObjectWithTag("Player").transform.position : Vector3.zero;
                            InteriorRoute.EnterHome(h.id, p, door != null ? door.transform.eulerAngles.y : 0f, true);
                        }));
                    if (back != null) l.Add(new UIEntry("< Kembali", back)); else l.Add(new UIEntry("Tutup", () => TownUI.I.Close()));
                    return l;
                });
        }

        public static void OpenHomeMenu(House h) { OpenHome(h, null); }

        static string HouseFlag(House h) { return "prop:" + h.id; }
        static string FurnFlag(House h, Furn f) { return "fur:" + h.id + ":" + f.id; }
        public static bool Owns(House h) { return GameState.I.flags.Contains(HouseFlag(h)); }
        public static bool Has(House h, Furn f) { return GameState.I.flags.Contains(FurnFlag(h, f)); }

        public static House OwnedIn(string town)
        {
            House best = null;
            foreach (var h in Houses) if (h.town == town && Owns(h) && (best == null || h.baseComfort > best.baseComfort)) best = h;
            return best;
        }

        public static int Comfort(House h)
        {
            int c = h.baseComfort;
            foreach (var f in Shop) if (Has(h, f)) c += f.comfort;
            return c;
        }

        static string ComfortLabel(int c) { return c >= 40 ? "Mewah" : c >= 25 ? "Sangat nyaman" : c >= 15 ? "Nyaman" : c >= 8 ? "Cukup" : "Sederhana"; }

        static void Back(List<UIEntry> l, Action back) { l.Add(new UIEntry(back != null ? "< Kembali" : "Tutup", back != null ? back : (Action)(() => TownUI.I.Close()))); }

        // ---------------- menu utama ----------------
        public static void Open(Action back) { Open(back, false); }

        public static void Open(Action back, bool shopFirst)
        {
            if (shopFirst) { OpenFurnitureShop(() => Open(back, false)); return; }
            var gs = GameState.I;
            string town = CurrentTown();
            var own = OwnedIn(town);
            TownUI.I.Open("Properti & Rumah - " + TownName(town),
                () => "Gold: " + gs.gold + "   Rank " + gs.rank + "\n" + (own == null
                    ? "Kamu belum punya rumah di " + TownName(town) + ". Hubungi agen properti Guild di bawah."
                    : "Rumahmu: " + own.name + "  (kenyamanan " + Comfort(own) + " - " + ComfortLabel(Comfort(own)) + ")"),
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Agen Properti: beli rumah >", () => OpenAgent(() => Open(back))));
                    if (own != null) l.Add(new UIEntry("Rumah Saya >", () => OpenHome(own, () => Open(back))));
                    l.Add(new UIEntry("Toko Furnitur >", () => OpenFurnitureShop(() => Open(back))));
                    Back(l, back);
                    return l;
                });
        }

        // ---------------- agen properti ----------------
        static void OpenAgent(Action back)
        {
            var gs = GameState.I;
            string town = CurrentTown();
            TownUI.I.Open("Agen Properti - " + TownName(town),
                () => "Gold: " + gs.gold + "   Rank " + gs.rank + "\nPilih rumah untuk dibeli di " + TownName(town) + ". Rumah tidak bisa dijual kembali.",
                () =>
                {
                    var l = new List<UIEntry>();
                    foreach (var hh in Houses)
                    {
                        var h = hh;
                        if (h.town != town) continue;
                        bool owned = Owns(h);
                        bool rankOk = GameState.RankIndex(gs.rank) >= h.rankIdx;
                        string tag = owned ? "  [DIMILIKI]" : !rankOk ? "  [butuh rank " + GameState.Ranks[h.rankIdx] + "]" : "";
                        l.Add(new UIEntry(h.name + "  - " + h.price + "g" + tag + "\n" + h.desc + " (nyaman " + h.baseComfort + ")",
                            () => OpenOffer(h, () => OpenAgent(back), true)));
                    }
                    Back(l, back);
                    return l;
                });
        }

        // ---------------- toko furnitur ----------------
        static void OpenFurnitureShop(Action back)
        {
            var gs = GameState.I;
            string town = CurrentTown();
            var own = OwnedIn(town);
            TownUI.I.Open("Toko Furnitur",
                () => "Gold: " + gs.gold + "\n" + (own == null ? "Beli rumah di kota ini dulu supaya furnitur bisa dipasang." : "Furnitur dipasang otomatis di " + own.name + ". Kenyamanan: " + Comfort(own)),
                () =>
                {
                    var l = new List<UIEntry>();
                    foreach (var ff in Shop)
                    {
                        var f = ff;
                        bool has = own != null && Has(own, f);
                        l.Add(new UIEntry(f.name + "  - " + f.price + "g   (+" + f.comfort + " nyaman)" + (has ? "  [TERPASANG]" : ""),
                            () =>
                            {
                                if (!gs.TrySpend(f.price)) return;
                                gs.flags.Add(FurnFlag(own, f));
                                TownUI.I.Toast(f.name + " dipasang di rumahmu.");
                                SaveSystem.Save();
                                OpenFurnitureShop(back);
                            }, own != null && !has && gs.gold >= f.price));
                    }
                    Back(l, back);
                    return l;
                });
        }

        // ---------------- rumah saya ----------------
        static void OpenHome(House h, Action back)
        {
            var gs = GameState.I;
            TownUI.I.Open(h.name,
                () =>
                {
                    int c = Comfort(h);
                    return "Kenyamanan " + c + " (" + ComfortLabel(c) + ")   Jam " + gs.Clock +
                           "\nTidur di rumah gratis: HP/stamina pulih, lapar & haus +" + Mathf.Min(40, c) + (c >= 15 ? ", buff +5% EXP 3 quest" : "") + "." +
                           "\nFurnitur: " + FurnList(h);
                },
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Tidur sampai pagi", () => Sleep(h)));
                    l.Add(new UIEntry("Kotak Surat (" + Mail.InboxCount() + ")", () => Mail.OpenInbox(() => OpenHome(h, back))));
                    if (Has(h, FindFurn("stash"))) l.Add(new UIEntry("Peti Simpanan (" + PropertyExtra.StashCount + ")", () => PropertyExtra.OpenStash(() => OpenHome(h, back))));
                    if (Has(h, FindFurn("trophy"))) l.Add(new UIEntry("Rak Trofi >", () => PropertyExtra.OpenTrophies(() => OpenHome(h, back))));
                    if (Has(h, FindFurn("tank"))) l.Add(new UIEntry("Akuarium >", () => PropertyExtra.OpenDisplay(() => OpenHome(h, back), true)));
                    if (Has(h, FindFurn("table"))) l.Add(new UIEntry("Meja Masakan >", () => PropertyExtra.OpenDisplay(() => OpenHome(h, back), false)));
                    HomeLife.AddEntries(l, h, () => OpenHome(h, back));
                    foreach (var ff in Shop)
                    {
                        var f = ff;
                        if (!Has(h, f)) continue;
                        int refund = f.price / 2;
                        l.Add(new UIEntry("Jual " + f.name + " (+" + refund + "g)", () =>
                        {
                            gs.flags.Remove(FurnFlag(h, f));
                            gs.gold += refund;
                            OpenHome(h, back);
                        }));
                    }
                    Back(l, back);
                    return l;
                });
        }

        static string FurnList(House h)
        {
            var parts = new List<string>();
            foreach (var f in Shop) if (Has(h, f)) parts.Add(f.name);
            return parts.Count == 0 ? "(kosong - belanja di Toko Furnitur)" : string.Join(", ", parts.ToArray());
        }

        static void Sleep(House h)
        {
            var gs = GameState.I;
            int c = Comfort(h);
            gs.SleepUntilMorning();
            float gain = Mathf.Min(40f, c);
            gs.hunger = Mathf.Min(100f, gs.hunger + gain);
            gs.thirst = Mathf.Min(100f, gs.thirst + gain);
            if (c >= 15) gs.expBuffQuests = Mathf.Max(gs.expBuffQuests, 3);
            if (PlayerCombat.I != null) { PlayerCombat.I.Heal(9999f); PlayerCombat.I.stamina = PlayerCombat.I.MaxStamina; PlayerCombat.I.mana = PlayerCombat.I.MaxMana; }
            SaveSystem.Save();
            TownUI.I.Close();
            TownUI.I.Toast("Tidur nyenyak di " + h.name + ". Hari " + gs.day + ", pukul " + gs.Clock + ".");
        }
    }
}
