using UnityEngine;

namespace HunterGuild
{
    /// <summary>Jejak monster (cakar, bulu, bekas makan). Mengumpulkan beberapa jejak mengaktifkan kompas ke buruan.</summary>
    public class TrackMark : Interactable
    {
        public string monsterId = "alpha_wolf";

        public override void Interact()
        {
            if (HuntManager.I == null) return;
            if (HuntManager.I.AddTrack(monsterId)) Destroy(gameObject);
            else TownUI.I.Toast("Jejak ini bukan milik buruan quest-mu (atau kamu belum punya quest hunt).");
        }
    }
}
