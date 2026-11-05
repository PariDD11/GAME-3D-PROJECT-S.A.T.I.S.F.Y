namespace HunterGuild
{
    /// <summary>Pelatih Guild: latihan lanjutan (SP) dan reset skill.</summary>
    public class TrainerNPC : Interactable
    {
        public override void Interact() { GuildMenus.OpenTrainer(null); }
    }
}
