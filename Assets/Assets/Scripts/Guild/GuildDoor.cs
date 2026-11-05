using UnityEngine.SceneManagement;

namespace HunterGuild
{
    public class GuildDoor : Interactable
    {
        public override void Interact()
        {
            if (SceneManager.GetActiveScene().name == InteriorInfo.Scene("GuildEldrath")) InteriorRoute.Exit();
            else SceneRouter.ExitGuild();
        }
    }
}
