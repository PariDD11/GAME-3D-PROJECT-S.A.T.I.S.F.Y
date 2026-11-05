using UnityEngine;

namespace HunterGuild
{
    /// <summary>Referensi ke sendi-sendi karakter 3D buatan NpcKit (dipakai PlayerAnimator dan NpcAnimator).</summary>
    public class CharacterRig : MonoBehaviour
    {
        public Transform body, head, armL, armR, foreL, foreR, legL, legR, tail, handL, handR;
        public float height = 1.5f, scale = 1f;
        public bool IsSplit { get { return foreL != null; } }
    }
}
