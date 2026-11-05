using UnityEngine;

namespace HunterGuild
{
    /// <summary>Menyalakan lampu (lampu jalan, lampu pintu) saat malam, mengikuti jam in-game.</summary>
    [RequireComponent(typeof(Light))]
    public class NightLight : MonoBehaviour
    {
        Light lamp;

        void Awake() { lamp = GetComponent<Light>(); }

        void Update()
        {
            var gs = GameState.I;
            if (gs == null) return;
            float t = gs.timeOfDay;
            lamp.enabled = t >= 17.5f || t < 6.5f;
        }
    }
}
