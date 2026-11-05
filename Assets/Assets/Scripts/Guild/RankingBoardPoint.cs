namespace HunterGuild
{
    /// <summary>Papan Peringkat Hunter lokal (NPC hunter dan rival).</summary>
    public class RankingBoardPoint : Interactable
    {
        public override void Interact() { GuildMenus.OpenRanking(); }
    }
}
