using UnityEngine;

namespace HunterGuild
{
    /// <summary>Zona bahaya lingkaran (perangkap, kabut racun, retakan api): melukai pemain secara berkala selama berada di dalamnya.</summary>
    public class HazardZone : MonoBehaviour
    {
        public float radius = 4f, dps = 8f, tick = 0.7f;
        public string warn = "Bahaya!";
        float next;
        static float warnAt;

        void Update()
        {
            var pc = PlayerCombat.I;
            if (pc == null || pc.IsDead) return;
            Vector3 d = pc.transform.position - transform.position; d.y = 0f;
            if (d.magnitude > radius) return;
            if (Mathf.Abs(pc.transform.position.y - transform.position.y) > 4f) return;
            if (Time.time < next) return;
            next = Time.time + tick;
            pc.TakeDamage(dps * tick, transform.position);
            if (Time.time > warnAt)
            {
                warnAt = Time.time + 5f;
                if (TownUI.I != null) TownUI.I.Toast(warn);
            }
        }
    }
}
