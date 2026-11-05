using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>Gerbang Kota: item persiapan (kompas/minimap dijual penjaga, lihat GateGuard), lalu berangkat ke area quest (GDD 8).</summary>
    public class CityGate : Interactable
    {
        public const string DungeonCityScene = "DungeonCity";
        public string huntSceneName = "HuntArea_G";      // area yang dilayani gerbang ini (Selatan = HuntArea_G, Timur = HuntArea_Desert, Barat = HuntArea_Swamp)
        public AnimatedGate gateDoor;                    // daun gerbang yang dibuka (kosong = gerbang utama)

        public override void Interact() { OpenMain(); }

        static bool Has(GameState gs, string flag) { return gs.flags.Contains(flag); }

        void OpenMain()
        {
            var gs = GameState.I;
            if (huntSceneName == DungeonCityScene) { OpenRoad(); return; }
            TownUI.I.Open("Gerbang Kota",
                () =>
                {
                    int food = gs.Count("bread") + gs.Count("herbal_stew") + gs.Count("roasted_fish") + gs.Count("grilled_meat");
                    return "Gold: " + gs.gold + "\nQuest: " + (gs.activeQuest == null ? "belum ada" : gs.activeQuest.title) +
                           "\nPerlengkapan jelajah: " + (gs.HasMinimap ? "Peta Jelajah" : (gs.HasKompas ? "Kompas" : "belum ada (dijual penjaga di Gerbang Luar)")) +
                           "\nBekal: makanan " + food + ", botol minum " + gs.canteen + "/" + GameState.CanteenMax +
                           ", ramuan " + gs.Count("potion") + ", pewarna " + gs.Count("paintball");
                },
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Pewarna / Paintball - 15g   punya " + gs.Count("paintball"), () => BuyItem("paintball", 15)));
                    l.Add(new UIEntry("Flare / Signal Scroll - 25g   punya " + gs.Count("flare"), () => BuyItem("flare", 25)));
                    l.Add(new UIEntry("Ramuan Penyembuh - 30g   punya " + gs.Count("potion"), () => BuyItem("potion", 30)));
                    l.Add(new UIEntry("Buka gerbang & berangkat ke " + AreaShort(huntSceneName) + " >", Depart));
                    return l;
                });
        }

        /// <summary>Gerbang Utara: jalan raya aman ke Kota Eldrath. Tidak perlu quest atau kompas.</summary>
        void OpenRoad()
        {
            var gs = GameState.I;
            TownUI.I.Open("Gerbang Luar Utara",
                () => "Penjaga: \"Jalan raya ke Eldrath, Kota Dungeon. Jalan raya melewati Hutan Perbatasan; patroli Guild menjaga jalur utama, tapi monster berkeliaran di luar jalan. Tidak perlu quest atau kompas.\"\n\nGold: " + gs.gold +
                      "\nLantai dungeon tertinggi: " + (gs.dungeonBest <= 0 ? "belum pernah masuk" : gs.dungeonBest.ToString()),
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Ramuan Penyembuh - 30g   punya " + gs.Count("potion"), () => BuyItem("potion", 30)));
                    l.Add(new UIEntry("Buka gerbang & berangkat ke Kota Eldrath >", () =>
                    {
                        TownUI.I.Close();
                        var door = gateDoor != null ? gateDoor : AnimatedGate.Main;
                        if (door != null) door.Open();
                        TownUI.I.Toast("Gerbang dibuka. Susuri jalan raya ke utara lewat Hutan Perbatasan menuju Kota Eldrath.");
                    }));
                    return l;
                });
        }

        void BuyItem(string id, int price)
        {
            var gs = GameState.I;
            if (!gs.HasSpace(id, 1)) { TownUI.I.Toast("Tas penuh."); return; }
            if (gs.TrySpend(price)) gs.AddItem(id, 1);
        }

        /// <summary>Syarat berangkat ke area hunt. Dipakai tombol gerbang dan zona transisi di jalan keluar.</summary>
        public static string AreaName(string scene)
        {
            if (scene == DungeonCityScene) return "Kota Eldrath (Gerbang Luar Utara)";
            if (scene == "DungeonTower") return "Menara Eldrath (lewat Gerbang Luar Utara, lalu masuk Kota Eldrath)";
            if (scene == "HuntArea_Swamp") return "Rawa Terkutuk (Gerbang Luar Barat)";
            if (scene == "HuntArea_Volcano") return "Gunung Vulkanik (Gerbang Utara Eldrath)";
            if (scene == "HuntArea_Bone") return "Padang Tulang (Gerbang Timur Eldrath)";
            if (scene == "HuntArea_Demon") return "Lembah Iblis (Gerbang Barat Eldrath)";
            if (scene == "HuntArea_Frontier") return "Hutan Perbatasan (jalan raya Perbatasan-Eldrath)";
            return scene == "HuntArea_Desert" ? "Gurun Zahar (Gerbang Luar Timur)" : "Hutan Lerai (Gerbang Luar Selatan)";
        }

        static string AreaShort(string scene)
        {
            if (scene == "HuntArea_Swamp") return "Rawa Terkutuk";
            if (scene == "HuntArea_Volcano") return "Gunung Vulkanik";
            if (scene == "HuntArea_Bone") return "Padang Tulang";
            if (scene == "HuntArea_Demon") return "Lembah Iblis";
            if (scene == "HuntArea_Frontier") return "Hutan Perbatasan";
            return scene == "HuntArea_Desert" ? "Gurun Zahar" : "Hutan Lerai";
        }

        public static bool CanLeave(out string msg, out string scene) { return CanLeave("", out msg, out scene); }

        /// <summary>gateScene kosong = gerbang mana saja; diisi = gerbang itu hanya melayani area tersebut.</summary>
        public static bool CanLeave(string gateScene, out string msg, out string scene)
        {
            var gs = GameState.I;
            msg = "";
            scene = "HuntArea_G";
            if (gateScene == DungeonCityScene)
            {
                scene = DungeonCityScene;
                if (!Application.CanStreamedLevelBeLoaded(scene)) { msg = "Scene 'DungeonCity' belum dibuat (menu Hunter Guild > Build Dungeon City)."; return false; }
                return true;
            }
            if (gs.activeQuest == null) { msg = "Ambil quest di Guild dulu."; return false; }
            if (gs.activeQuest.completed) { msg = "Quest sudah selesai. Lapor ke Guild."; return false; }
            string partyMsg;
            if (!GuildServices.PartyReady(out partyMsg)) { msg = partyMsg; return false; }   // quest rank B ke atas wajib party
            var gq = GuildQuestDB.Find(gs.activeQuest.id);
            if (gq != null && !string.IsNullOrEmpty(gq.huntScene)) scene = gq.huntScene;
            if (!string.IsNullOrEmpty(gateScene) && gateScene != scene)
            {
                msg = "Quest-mu di " + AreaName(scene) + ". Gerbang ini menuju " + AreaName(gateScene) + ".";
                return false;
            }
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                msg = "Scene '" + scene + "' belum dibuat / belum masuk Build Settings.";
                return false;
            }
            return true;
        }

        void Depart()
        {
            string msg, scene;
            if (!CanLeave(huntSceneName, out msg, out scene)) { TownUI.I.Toast(msg); return; }
            TownUI.I.Close();
            GateGuard.Check(OpenGate, null);
        }

        void OpenGate()
        {
            string msg, scene;
            if (!CanLeave(huntSceneName, out msg, out scene)) { TownUI.I.Toast(msg); return; }
            var door = gateDoor != null ? gateDoor : AnimatedGate.Main;
            if (door != null)
            {
                door.Open();
                TownUI.I.Toast(huntSceneName == "HuntArea_Volcano" ? "Gerbang dibuka. Susuri jalan ke utara menuju Gunung Vulkanik." :
                    huntSceneName == "HuntArea_Bone" ? "Gerbang dibuka. Susuri jalan ke timur menuju Padang Tulang." :
                    huntSceneName == "HuntArea_Demon" ? "Gerbang dibuka. Susuri jalan ke barat menuju Lembah Iblis." :
                    huntSceneName == "HuntArea_Swamp" ? "Gerbang dibuka. Susuri jalan ke barat menuju Rawa Terkutuk." :
                    huntSceneName == "HuntArea_Desert" ? "Gerbang dibuka. Susuri jalan ke timur menuju Gurun Zahar." : "Gerbang dibuka. Susuri jalan ke selatan menuju Hutan Lerai.");
            }
            else SceneRouter.GoToSpawn(scene, "SpawnFromTown");     // scene kota lama tanpa gerbang berdaun pintu
        }
    }
}
