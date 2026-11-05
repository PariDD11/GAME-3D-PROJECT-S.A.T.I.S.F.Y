using System;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Penjaga Gerbang Luar menghadang pemain yang mau keluar dan menawarkan Kompas atau Minimap jelajah.
    /// Perlengkapan ini hanya berlaku selama satu penjelajahan: hangus otomatis saat pulang ke kota (GameState.EndExpedition).
    /// </summary>
    public static class GateGuard
    {
        public const int KompasPrice = 60;
        public const int MinimapPrice = 160;
        public const int UpgradePrice = 100;      // kompas -> minimap

        /// <summary>true = pemain WAJIB punya kompas/minimap untuk keluar. false = boleh menolak dan tetap lewat.</summary>
        public static bool RequireNavItem = true;

        public static bool Ready { get { var gs = GameState.I; return gs != null && gs.HasKompas; } }

        /// <summary>Jika sudah punya perlengkapan, langsung lanjut. Jika belum, penjaga menghadang dan membuka tawaran.</summary>
        public static void Check(Action proceed, Action decline)
        {
            var gs = GameState.I;
            if (gs.HasKompas)
            {
                if (TownUI.I != null) TownUI.I.Toast("Penjaga: \"Jaga dirimu di luar sana, Hunter.\"");
                proceed();
                return;
            }
            Offer(proceed, decline);
        }

        static void Offer(Action proceed, Action decline)
        {
            var gs = GameState.I;
            TownUI.I.Open("Penjaga Gerbang Luar",
                () => "\"Berhenti! Hutan di luar tidak punya papan petunjuk. Banyak Hunter tersesat dan tak kembali.\n" +
                      "Bawa Kompas atau Peta Jelajah. Barangnya hangus begitu kamu pulang, jadi beli lagi tiap berangkat.\"\n\n" +
                      "Gold: " + gs.gold + "    Status: " + Status(gs) +
                      (RequireNavItem ? "\n(Tanpa salah satunya, gerbang tidak akan dibuka.)" : ""),
                () =>
                {
                    var l = new List<UIEntry>();
                    if (gs.HasMinimap)
                        l.Add(new UIEntry("Peta Jelajah (sudah dimiliki)", null, false));
                    else
                        l.Add(new UIEntry("Peta Jelajah (minimap + petunjuk area + kompas) - " + (gs.HasKompas ? UpgradePrice : MinimapPrice) + "g",
                            () => Buy(GameState.FlagMinimap, gs.HasKompas ? UpgradePrice : MinimapPrice)));
                    l.Add(new UIEntry(gs.HasKompas ? "Kompas (sudah dimiliki)" : "Kompas (jarum arah buruan) - " + KompasPrice + "g",
                        () => Buy(GameState.FlagKompas, KompasPrice), !gs.HasKompas));
                    if (gs.HasKompas)
                        l.Add(new UIEntry("Lanjut keluar gerbang >", () => { TownUI.I.Close(); proceed(); }));
                    else if (!RequireNavItem)
                        l.Add(new UIEntry("Tolak, pergi tanpa perlengkapan >", () => { TownUI.I.Close(); proceed(); }));
                    l.Add(new UIEntry("Kembali ke kota", () => { TownUI.I.Close(); if (decline != null) decline(); }));
                    return l;
                });
        }

        static string Status(GameState gs)
        {
            return gs.HasMinimap ? "Peta Jelajah" : (gs.HasKompas ? "Kompas" : "belum punya apa pun");
        }

        static void Buy(string flag, int price)
        {
            var gs = GameState.I;
            if (!gs.TrySpend(price)) return;
            if (flag == GameState.FlagMinimap) gs.flags.Remove(GameState.FlagKompas);
            gs.flags.Add(flag);
            TownUI.I.Toast(flag == GameState.FlagMinimap ? "Peta Jelajah dibeli. Berlaku sampai kamu pulang ke kota." : "Kompas dibeli. Berlaku sampai kamu pulang ke kota.");
        }

        /// <summary>Dorong pemain beberapa meter kembali ke arah kota saat menolak/ditahan penjaga.</summary>
        public static void PushBack(Transform player, Vector3 gatePos, float meters)
        {
            if (player == null) return;
            Vector3 back = player.position - gatePos;
            back.y = 0f;
            back = back.sqrMagnitude < 0.01f ? -player.forward : back.normalized;
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.position += back * meters;
            if (cc != null) cc.enabled = true;
        }
    }
}
