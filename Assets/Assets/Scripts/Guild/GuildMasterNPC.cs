namespace HunterGuild
{
    /// <summary>Guild Master: dialog lore per Arc dan pengajuan Ujian Kenaikan Rank.</summary>
    public class GuildMasterNPC : Interactable
    {
        public override void Interact() { GuildMenus.OpenMaster(); }
    }
}
