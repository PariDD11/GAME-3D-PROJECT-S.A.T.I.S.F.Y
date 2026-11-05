using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Base camp: istirahat, isi botol minum, pulang ke kota. Titik respawn saat pingsan (GDD 5.3).</summary>
    public class BaseCamp : Interactable
    {
        public override void Interact() { OpenMain(); }

        void OpenMain()
        {
            var gs = GameState.I;
            TownUI.I.Open(HuntManager.I != null && HuntManager.I.defaultZone == "Gunung Vulkanik" ? "Base Camp Kaki Gunung" : HuntManager.I != null && HuntManager.I.defaultZone == "Padang Tulang" ? "Base Camp Medan Perang" : HuntManager.I != null && HuntManager.I.defaultZone == "Lembah Iblis" ? "Base Camp Jurang" : HuntManager.I != null && HuntManager.I.defaultZone == "Hutan Perbatasan" ? "Base Camp Perbatasan" : HuntManager.I != null && HuntManager.I.defaultZone == "Hutan Pemula" ? "Pondok Pemburu Hutan Pemula" : HuntManager.I != null && HuntManager.I.defaultZone == "Rawa Terkutuk" ? "Pos Rawa Terkutuk" : HuntManager.I != null && HuntManager.I.defaultZone == "Gurun Zahar" ? "Base Camp Oasis Gurun Zahar" : "Base Camp Hutan Lerai",
                () =>
                {
                    var q = gs.activeQuest;
                    string qs = q == null ? "Belum ada quest aktif." : (q.completed ? "QUEST SELESAI. Pulang dan lapor ke Guild." : q.title + "  " + gs.questProgress + "/" + q.targetCount);
                    return "Quest: " + qs + "\nHunger " + Mathf.RoundToInt(gs.hunger) + "   Thirst " + Mathf.RoundToInt(gs.thirst) + "   Botol Minum " + gs.canteen + "/" + GameState.CanteenMax;
                },
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Istirahat di tenda (skip 6 jam, HP & stamina pulih)", Rest));
                    l.Add(new UIEntry("Masak di api unggun >", () => FishCook.OpenCooking("Api Unggun", OpenMain)));
                    l.Add(new UIEntry("Isi botol minum (gratis)", gs.RefillCanteen, gs.canteen < GameState.CanteenMax));
                    l.Add(new UIEntry("Pulang ke kota (jalan pintas)", () => { if (HuntManager.I != null) HuntManager.I.ReturnToTown(); }));
                    return l;
                });
        }

        void Rest()
        {
            var gs = GameState.I;
            gs.Advance(6f, 0.5f, false);
            gs.hoursAwake = Mathf.Max(0f, gs.hoursAwake - 12f);
            if (PlayerCombat.I != null) PlayerCombat.I.FullRestore();
            TownUI.I.Toast("Kamu beristirahat. HP dan stamina pulih. Pukul " + gs.Clock + ".");
        }
    }
}
