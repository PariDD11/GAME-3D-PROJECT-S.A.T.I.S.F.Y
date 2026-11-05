using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Penjaga yang mengantar Hunter ke Ashwatch (gratis). Dipakai di base camp Hutan Perbatasan.</summary>
    public class TownEscort : Interactable
    {
        public string destScene = "Town";
        public string destSpawn = "SpawnFromDungeon";
        public string destName = "Ashwatch";

        void Start()
        {
            // dialog umum NPC (NpcChat) yang berada di titik yang sama jangan merebut tombol interaksi
            foreach (var c in GetComponentsInParent<NpcChat>(true)) if (c != null && c.enabled) c.enabled = false;
        }

        public override void Interact()
        {
            TownUI.I.Open("Penjaga Perbatasan",
                () => "\"Jalan ke " + destName + " cukup jauh. Aku bisa mengantarmu, tanpa biaya, asal kau sudah selesai dengan urusanmu di hutan.\"",
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Antar aku ke " + destName + " (gratis)", () => { if (GameState.I != null) GameState.I.EndExpedition(); SceneRouter.GoToSpawn(destScene, destSpawn); }));
                    l.Add(new UIEntry("Nanti saja", () => TownUI.I.Close()));
                    return l;
                });
        }
    }
}
