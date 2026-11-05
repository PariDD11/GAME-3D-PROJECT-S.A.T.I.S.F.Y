using UnityEngine;

namespace HunterGuild
{
    /// <summary>Sumber air: isi botol minum gratis dan minum sepuasnya (GDD 9: minum di lapangan gratis).</summary>
    public class WaterSource : Interactable
    {
        public override void Interact()
        {
            var gs = GameState.I;
            gs.RefillCanteen();
            gs.thirst = Mathf.Min(100f, gs.thirst + 50f);
            TownUI.I.Toast("Minum dan isi botol minum (Thirst +50, botol minum penuh).");
        }
    }
}
