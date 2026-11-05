using UnityEngine;

namespace HunterGuild
{
    /// <summary>Aturan akses gerbang.</summary>
    public static class GateRules
    {
        /// <summary>Level minimum untuk melewati Gerbang Dalam (menuju Kediaman Bangsawan dan Kastil). Ubah sesuka hati.</summary>
        public static int InnerGateMinLevel = 20;
    }

    /// <summary>
    /// Pemeriksaan level di Gerbang Dalam. Gerbang tampak terbuka, tetapi sebuah penghalang tak terlihat menahan pemain
    /// yang levelnya kurang; penjaga memberi tahu syaratnya. NPC kota tidak terpengaruh.
    /// </summary>
    public class LevelGate : MonoBehaviour
    {
        public int minLevel = 0;                 // 0 = ikut GateRules.InnerGateMinLevel
        public string gateName = "Gerbang Dalam";
        public Collider blocker;
        public float warnRadius = 11f;

        Transform player;
        float nextWarn;

        int Need { get { return minLevel > 0 ? minLevel : GateRules.InnerGateMinLevel; } }

        void Update()
        {
            var gs = GameState.I;
            if (gs == null) return;
            bool ok = gs.level >= Need;
            if (blocker != null) blocker.enabled = !ok;

            if (ok) return;
            if (player == null) { var p = GameObject.FindGameObjectWithTag("Player"); if (p != null) player = p.transform; }
            if (player == null || Time.time < nextWarn) return;
            Vector3 d = player.position - transform.position; d.y = 0f;
            if (d.magnitude < warnRadius && TownUI.I != null)
            {
                nextWarn = Time.time + 6f;
                TownUI.I.Toast("Penjaga: \"" + gateName + " hanya untuk Hunter Level " + Need + " ke atas. Levelmu baru " + gs.level + ".\"");
                Audio.Sfx("error");
            }
        }
    }
}
