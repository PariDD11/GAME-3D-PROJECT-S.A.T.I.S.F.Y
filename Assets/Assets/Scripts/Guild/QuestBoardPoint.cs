namespace HunterGuild
{
    /// <summary>Papan Quest di dinding barat. Bisa dilihat kapan saja; menerima quest butuh resepsionis buka.</summary>
    public class QuestBoardPoint : Interactable
    {
        public override void Interact() { QuestBoardUI.Get().Show(); }
    }
}
