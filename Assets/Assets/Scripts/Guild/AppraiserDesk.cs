namespace HunterGuild
{
    /// <summary>Penilai bahan: jual item drop dengan harga tetap + modifier reputasi dan event.</summary>
    public class AppraiserDesk : Interactable
    {
        public override void Interact() { ShopHub.Open(this, () => GuildMenus.OpenSell(null), "Jual bahan monster."); }
    }
}
