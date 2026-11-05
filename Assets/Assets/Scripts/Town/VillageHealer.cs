using UnityEngine;

namespace HunterGuild
{
    /// <summary>Tabib desa: pemulihan HP/stamina gratis (untuk pemula).</summary>
    public class VillageHealer : Interactable
    {
        public override void Interact()
        {
            var gs = GameState.I;
            if (PlayerCombat.I != null) PlayerCombat.I.FullRestore();
            if (gs != null) { gs.hunger = Mathf.Max(gs.hunger, 70f); gs.thirst = Mathf.Max(gs.thirst, 70f); }
            Audio.Voice("priest", "Semoga Cahaya menyertaimu.");
            Audio.Sfx("heal", transform.position);
            if (TownUI.I != null) TownUI.I.Toast("Tabib merawat lukamu. HP dan stamina pulih.");
        }
    }
}
