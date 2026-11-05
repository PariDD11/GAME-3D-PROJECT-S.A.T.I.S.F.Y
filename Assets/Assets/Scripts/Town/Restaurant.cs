using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Kedai makan: makan/minum di tempat, bungkus bekal, isi botol minum gratis (GDD 9).</summary>
    public class Restaurant : Interactable
    {
        public override void Interact() { OpenMain(); }

        static string Status(GameState gs)
        {
            return "Gold: " + gs.gold + "\nHunger " + Mathf.RoundToInt(gs.hunger) + "   Thirst " + Mathf.RoundToInt(gs.thirst) +
                   "   Botol Minum " + gs.canteen + "/" + GameState.CanteenMax +
                   "\nMakan sebelum berangkat memberi buff untuk satu quest.";
        }

        void OpenMain()
        {
            var gs = GameState.I;
            TownUI.I.Open("Kedai Makan", () => Status(gs), () =>
            {
                var l = new List<UIEntry>();
                l.Add(new UIEntry("Dapur: masak sendiri >", () => FishCook.OpenCooking("Dapur Kedai", OpenMain)));
                l.Add(new UIEntry("Makan di tempat >", () => OpenMenu(ItemType.Food, false)));
                l.Add(new UIEntry("Minum di tempat >", () => OpenMenu(ItemType.Drink, false)));
                l.Add(new UIEntry("Bungkus bekal untuk quest >", () => OpenMenu(ItemType.Food, true)));
                l.Add(new UIEntry("Isi botol minum (gratis)  " + gs.canteen + "/" + GameState.CanteenMax, gs.RefillCanteen, gs.canteen < GameState.CanteenMax));
                return l;
            });
        }

        void OpenMenu(ItemType type, bool takeaway)
        {
            var gs = GameState.I;
            string title = takeaway ? "Bungkus Bekal" : (type == ItemType.Food ? "Menu Makanan" : "Menu Minuman");
            TownUI.I.Open(title, () => Status(gs), () =>
            {
                var l = new List<UIEntry>();
                foreach (var d in ItemDB.OfType(type))
                {
                    var def = d;
                    if (def.buyPrice <= 0) continue;                // masakan buatan sendiri tidak dijual di sini
                    l.Add(new UIEntry(def.name + "  (" + def.Describe() + ")  - " + def.buyPrice + "g", () => Buy(def, takeaway)));
                }
                l.Add(new UIEntry("< Kembali", OpenMain));
                return l;
            });
        }

        void Buy(ItemDef def, bool takeaway)
        {
            var gs = GameState.I;
            if (!takeaway)
            {
                bool foodFull = def.hunger > 0 && def.thirst == 0 && gs.hunger >= 98f;
                bool drinkFull = def.thirst > 0 && def.hunger == 0 && gs.thirst >= 98f;
                if (foodFull || drinkFull) { TownUI.I.Toast("Kamu masih kenyang."); return; }
            }
            if (takeaway && !gs.HasSpace(def.id, 1)) { TownUI.I.Toast("Tas penuh."); return; }
            if (!gs.TrySpend(def.buyPrice)) return;
            if (takeaway) { gs.AddItem(def.id, 1); TownUI.I.Toast(def.name + " dibungkus ke tas."); }
            else gs.ApplyFood(def);
        }
    }
}
