using System;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Menu awal untuk NPC penjual (pandai besi, toko, penginapan, perpustakaan, penilai): sapaan -> pilihan di kanan
    /// (Lihat barang dagangan / Ada quest? / Ngobrol saja / Sampai jumpa), mengikuti referensi UI Lina.
    /// Bila interaksinya bukan milik NPC (mis. bangunan di luar), langsung membuka menu toko tanpa hub.
    /// </summary>
    public static class ShopHub
    {
        public static void Open(Interactable who, Action openShop, string shopLabel = "Lihat barang dagangan.")
        {
            NpcActor npc = who != null ? who.GetComponentInParent<NpcActor>() : null;
            if (npc == null) { openShop(); return; }
            Hub(who, npc, openShop, shopLabel, null);
        }

        static string Greet(NpcActor npc)
        {
            string[] g = { "Selamat datang! Ada yang bisa kubantu hari ini?", "Oh, pelanggan! Silakan, lihat-lihat dulu.", "Halo, Hunter. Butuh sesuatu?" };
            return g[Mathf.Abs(npc.GetInstanceID()) % g.Length];
        }

        static void Hub(Interactable who, NpcActor npc, Action openShop, string shopLabel, string say)
        {
            string title = string.IsNullOrEmpty(npc.displayName) ? "Penjual" : npc.displayName;
            TownUI.I.Open(title,
                () => say ?? Greet(npc),
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry(shopLabel, openShop));
                    l.Add(new UIEntry("Ada quest?", () => ShopQuests.Open(who, npc, () => Hub(who, npc, openShop, shopLabel, null))));
                    foreach (var we in Investigation.WitnessEntries(npc, () => Hub(who, npc, openShop, shopLabel, null))) l.Add(we);
                    l.Add(new UIEntry("Ada surat?", () => Mail.NpcMenu(npc, () => Hub(who, npc, openShop, shopLabel, null))));
                    l.Add(new UIEntry("Ngobrol saja.", () => Hub(who, npc, openShop, shopLabel, NpcDialogue.Greeting(npc.race, npc.role, false))));
                    l.Add(new UIEntry("Sampai jumpa.", () => TownUI.I.Close()));
                    return l;
                });
        }
    }
}
