namespace HunterGuild
{
    /// <summary>Meja resepsionis: klaim reward, poster rekrutmen, info rank. Tutup 00.00-05.00.</summary>
    public class ReceptionDesk : Interactable, IQuestGiver
    {
        void Awake() { if (string.IsNullOrEmpty(speakerName)) { speakerName = "Resepsionis Guild"; speakerRole = "Guild Hunter"; } }
        /// <summary>1 = belum ada quest aktif (ambil di Papan Quest), 2 = quest selesai dan siap diklaim, 0 = quest sedang berjalan.</summary>
        public int QuestMarkState()
        {
            var gs = GameState.I; if (gs == null) return 0;
            if (gs.activeQuest == null) return 1;
            return gs.activeQuest.completed ? 2 : 0;
        }
        public override void Interact() { GuildMenus.OpenReception(false); }
    }
}
