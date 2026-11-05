using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Pendeta Katedral: doa pemulihan, persembahan penghapus lelah, dan berkat EXP.</summary>
    public class Chapel : Interactable
    {
        public override void Interact()
        {
            var gs = GameState.I;
            TownUI.I.Open("Katedral Cahaya Suci",
                () => "Pendeta: \"Cahaya menyertai para pemburu.\"\nGold: " + gs.gold + (gs.IsFatigued ? "\nKamu LELAH; persembahan dapat memulihkannya." : "") +
                      (gs.expBuffQuests > 0 ? "\nBerkat aktif untuk " + gs.expBuffQuests + " quest." : ""),
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Doa pemulihan (gratis)\nPulihkan HP dan stamina sepenuhnya.", () =>
                    {
                        if (PlayerCombat.I != null) PlayerCombat.I.FullRestore();
                        TownUI.I.Toast("Cahaya hangat memulihkan tubuhmu.");
                        Audio.Sfx("potion");
                    }));
                    l.Add(new UIEntry("Persembahan 30g\nHilangkan rasa lelah (jam terjaga di-reset).", () =>
                    {
                        if (gs.TrySpend(30)) { gs.hoursAwake = 0f; TownUI.I.Toast("Kelelahanmu sirna."); }
                    }, gs.gold >= 30 && gs.hoursAwake > 1f));
                    l.Add(new UIEntry("Berkat Pemburu 80g\n+EXP untuk 2 quest berikutnya.", () =>
                    {
                        if (gs.TrySpend(80)) { gs.expBuffQuests = Mathf.Max(gs.expBuffQuests, 2); TownUI.I.Toast("Berkat diterima."); Audio.Sfx("quest"); }
                    }, gs.gold >= 80));
                    return l;
                });
        }
    }
}
