using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Penginapan: tidur (skip ke pukul 06.00), pulihkan Lelah, jadi save/spawn point (GDD 9.2).</summary>
    public class Inn : Interactable
    {
        class Room { public string name, desc; public int price, hungerGain, thirstGain; public bool expBuff; }

        static readonly Room[] Rooms =
        {
            new Room { name = "Kamar Sederhana", price = 20,  desc = "Recovery HP/stamina standar." },
            new Room { name = "Kamar Nyaman",    price = 50,  desc = "Recovery lebih cepat, Hunger/Thirst +10.", hungerGain = 10, thirstGain = 10 },
            new Room { name = "Suite Guild",     price = 125, desc = "Recovery penuh, Hunger/Thirst +25, buff +5% EXP untuk 3 quest.", hungerGain = 25, thirstGain = 25, expBuff = true },
        };

        public override void Interact() { ShopHub.Open(this, OpenMain, "Sewa kamar."); }

        void OpenMain()
        {
            var gs = GameState.I;
            TownUI.I.Open("Penginapan",
                () => "Gold: " + gs.gold + "   Jam " + gs.Clock + (gs.IsFatigued ? "\nKamu LELAH (stamina maks -10%). Tidur untuk memulihkan." : "") +
                      "\nTidur melewati waktu sampai pukul 06.00 (lapar/haus berkurang setengah laju). Titik respawn kota: " + gs.spawnPoint + ".",
                () =>
                {
                    var l = new List<UIEntry>();
                    foreach (var r in Rooms)
                    {
                        var room = r;
                        l.Add(new UIEntry(room.name + "  - " + room.price + "g\n" + room.desc, () => Sleep(room), gs.gold >= room.price));
                    }
                    return l;
                });
        }

        void Sleep(Room r)
        {
            var gs = GameState.I;
            if (!gs.TrySpend(r.price)) return;
            gs.SleepUntilMorning();
            gs.hunger = Mathf.Min(100f, gs.hunger + r.hungerGain);
            gs.thirst = Mathf.Min(100f, gs.thirst + r.thirstGain);
            if (r.expBuff) gs.expBuffQuests = 3;
            gs.spawnPoint = "Inn";
            SaveSystem.Save();
            TownUI.I.Close();
            TownUI.I.Toast("Selamat pagi! Hari " + gs.day + ", pukul " + gs.Clock + ".");
        }
    }
}
