namespace HunterGuild
{
    /// <summary>Papan Pengumuman dinamis (rumor, harga event, misi opsional).</summary>
    public class NoticeBoardPoint : Interactable
    {
        public override void Interact() { GuildMenus.OpenNotices(); }
    }
}
