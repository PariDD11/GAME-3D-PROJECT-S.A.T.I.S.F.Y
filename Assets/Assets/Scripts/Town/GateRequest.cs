using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Titik "Minta izin" di dekat penjaga tiap gerbang kota. Gerbang selalu tertutup; penjaga baru membukanya
    /// kalau pemain memintanya (dan, untuk Gerbang Dalam/Tengah, levelnya cukup).
    /// </summary>
    public class GateRequest : Interactable
    {
        public AnimatedGate gate;
        public float openSeconds = 35f;

        public override void Interact()
        {
            var gs = GameState.I;
            if (gate == null || gs == null || TownUI.I == null) return;
            int need = Need();
            string name = string.IsNullOrEmpty(gate.displayName) ? "Gerbang" : gate.displayName;
            TownUI.I.Open("Penjaga - " + name,
                () =>
                {
                    if (gate.IsOpen) return "Penjaga: \"Gerbang sudah dibuka. Silakan lewat, Hunter.\"";
                    return "Penjaga: \"Gerbang ditutup demi keamanan kota. Apa keperluanmu, Hunter?\"" +
                           (need > 0 ? "\n\n(Hanya Hunter Level " + need + " ke atas. Levelmu: " + gs.level + ")" : "");
                },
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Minta izin untuk lewat", Request));
                    l.Add(new UIEntry("Tidak jadi", () => TownUI.I.Close()));
                    return l;
                });
        }

        int Need()
        {
            var lg = gate != null ? gate.GetComponentInChildren<LevelGate>() : null;
            if (lg == null) return 0;
            return lg.minLevel > 0 ? lg.minLevel : GateRules.InnerGateMinLevel;
        }

        void Request()
        {
            var gs = GameState.I;
            TownUI.I.Close();
            int need = Need();
            if (gs.level < need)
            {
                TownUI.I.Toast("Penjaga: \"Maaf, gerbang ini hanya untuk Hunter Level " + need + " ke atas. Kembali setelah kamu lebih kuat.\"");
                Audio.Sfx("error");
                return;
            }
            gate.HoldOpen(openSeconds);
            TownUI.I.Toast("Penjaga: \"Izin diberikan. Gerbang dibuka, jangan lama-lama.\"");
        }
    }
}
