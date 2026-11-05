using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n36: Desa Pemula dan Hutan Pemula kini satu scene. Pengendali ini menyalakan mode berburu (HuntManager: HUD hunt, kompas,
    /// jejak, nama zona, titik bangun di kemah) begitu pemain melewati lengkung gerbang hutan, dan mematikannya saat kembali ke desa.
    /// </summary>
    public class SeamlessHunt : MonoBehaviour
    {
        public HuntManager hunt;
        public float gateZ;                  // z dunia lengkung gerbang hutan
        public Vector3 campRespawn;          // titik bangun saat tumbang di hutan
        public Vector3 villageRespawn;       // titik bangun saat tumbang di desa

        Transform player; bool inForest; float next; bool firstCheck = true;

        void Update()
        {
            if (hunt == null) return;
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + 0.2f;
            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p == null) return;
                player = p.transform;
            }
            float z = player.position.z;
            bool want = inForest ? z > gateZ - 5f : z > gateZ + 5f;     // histeresis supaya tidak berkedip di gerbang
            if (want != inForest || firstCheck) Set(want, !firstCheck);
            firstCheck = false;
        }

        void Set(bool forest, bool announce)
        {
            inForest = forest;
            if (hunt != null && hunt.gameObject.activeSelf != forest) hunt.gameObject.SetActive(forest);
            var pc = PlayerCombat.I;
            if (pc != null) pc.respawnPos = (forest ? campRespawn : villageRespawn) + Vector3.up * 1.1f;
            if (announce && TownUI.I != null) TownUI.I.Toast(forest ? "Memasuki Hutan Pemula. Mode berburu aktif." : "Kembali ke wilayah Desa Pemula.");
        }
    }
}
