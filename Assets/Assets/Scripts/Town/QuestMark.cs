namespace HunterGuild
{
    /// <summary>
    /// v35z102n11: NPC yang benar-benar memberi/menerima quest. QuestMarkState: 0 = tidak ada, 1 = ada quest baru ("!"), 2 = quest siap dilaporkan ("?").
    /// Penanda di atas nama NPC (DialogueUI.UpdateMarker) hanya menampilkan "!" / "?" bila nilainya 1 / 2; selain itu tampil "..." (hanya bicara).
    /// </summary>
    public interface IQuestGiver { int QuestMarkState(); }
}
