namespace HunterGuild
{
    /// <summary>Pengawas Ujian di Arena Guild: memulai Ranking Exam yang sudah disetujui Guild Master.</summary>
    public class ExamReferee : Interactable
    {
        public override void Interact() { GuildMenus.OpenExamReferee(); }
    }
}
