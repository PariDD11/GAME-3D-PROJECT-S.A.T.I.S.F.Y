using UnityEngine;

namespace HunterGuild
{
    /// <summary>Pintu Cabang Guild Eldrath: masuk ke Balai Agung (scene Interior_GuildEldrath); bila belum dibuat, buka menu resepsionis.</summary>
    public class GuildBranch : Interactable
    {
        public override void Interact()
        {
            if (Application.CanStreamedLevelBeLoaded(InteriorInfo.Scene("GuildEldrath")))
            {
                InteriorRoute.EnterGuildEldrath(transform.position, transform.eulerAngles.y);
                return;
            }
            if (TownUI.I != null) TownUI.I.Toast("Balai Agung belum dibuat (Hunter Guild > Build Guild Interior (Eldrath)). Memakai menu cepat.");
            GuildMenus.OpenReception(true);
        }
    }
}
