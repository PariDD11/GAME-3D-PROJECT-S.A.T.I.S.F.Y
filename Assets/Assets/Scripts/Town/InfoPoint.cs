using System.Collections.Generic;

namespace HunterGuild
{
    /// <summary>Titik interaksi informasi: menampilkan teks (papan pengumuman, penjaga, gereja, kediaman bangsawan, dll).</summary>
    public class InfoPoint : Interactable
    {
        public string title = "Info";
        public string message = "";

        public override void Interact()
        {
            TownUI.I.Open(title, () => message, () => new List<UIEntry>());
        }
    }
}
