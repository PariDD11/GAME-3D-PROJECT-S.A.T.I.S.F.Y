using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Pintu Guild Petualang di kota. Menekan E membawa pemain masuk ke scene interior (GuildInterior).
    /// Semua layanan (quest, klaim, jual, rekrut partner) sekarang ada di dalam interior: lihat Assets/Scripts/Guild.
    /// Bila scene interior belum dibuat, layanan yang sama dibuka sebagai menu cepat agar kota tetap bisa dipakai.
    /// </summary>
    public class GuildHall : Interactable
    {
        public override void Interact()
        {
            if (Application.CanStreamedLevelBeLoaded(GuildConfig.InteriorScene))
            {
                SceneRouter.EnterGuild();
                return;
            }
            TownUI.I.Toast("Interior Guild belum dibuat (menu Hunter Guild > Build Guild Interior Scene). Memakai menu cepat.");
            GuildMenus.OpenReception(true);
        }
    }
}
