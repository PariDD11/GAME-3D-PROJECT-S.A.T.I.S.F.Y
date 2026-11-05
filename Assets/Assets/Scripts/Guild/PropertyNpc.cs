namespace HunterGuild
{
    /// <summary>NPC Agen Properti / Penjual Furnitur: membuka menu properti kota tempat dia berdiri.</summary>
    public class PropertyNpc : Interactable
    {
        public bool furniture;
        public override void Interact() { PropertyMenus.Open(null, furniture); }
    }
}
