using UnityEngine;

namespace HunterGuild
{
    /// <summary>Memutar objek terus-menerus (bilah kincir angin, dll).</summary>
    public class SpinObject : MonoBehaviour
    {
        public Vector3 axis = Vector3.forward;
        public float degreesPerSecond = 25f;

        void Update() { transform.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.Self); }
    }
}
