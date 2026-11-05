using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Berjalan menyusuri titik-titik rute lalu menonaktifkan diri (kereta dagang, pencuri, parade, kucing...).</summary>
    public class PathMover : MonoBehaviour
    {
        public Transform[] path;
        public float speed = 2f, startDelay, sideOffset;
        public bool bobbing;
        /// <summary>true = tinggi mengikuti titik rute (medan berbukit, lengkung jembatan); false = tinggi awal dipertahankan (kota datar lama).</summary>
        public bool followPathY;
        int idx; float delay; bool active;

        public void Begin()
        {
            if (path == null || path.Length < 2) return;
            transform.position = path[0].position + (sideOffset != 0f && path.Length > 1 ? Vector3.Cross(Vector3.up, (path[1].position - path[0].position).normalized) * sideOffset : Vector3.zero); idx = 1; delay = startDelay;
            Vector3 d = path[1].position - path[0].position; d.y = 0f;
            if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
            gameObject.SetActive(true); active = true;
            AnimatedGate.Walkers.Add(transform);
        }
        void OnDisable() { AnimatedGate.Walkers.Remove(transform); active = false; }

        void Update()
        {
            if (!active) return;
            if (delay > 0f) { delay -= Time.deltaTime; return; }
            Vector3 t = path[idx].position;
            if (!followPathY) t.y = transform.position.y;
            if (sideOffset != 0f)
            {
                Vector3 seg = path[idx].position - path[idx - 1].position; seg.y = 0f;
                if (seg.sqrMagnitude > 0.01f) t += Vector3.Cross(Vector3.up, seg.normalized) * sideOffset;
            }
            Vector3 d = t - transform.position;
            if (d.magnitude < 0.3f) { idx++; if (idx >= path.Length) { gameObject.SetActive(false); return; } return; }
            transform.position += d.normalized * speed * Time.deltaTime;
            Vector3 flat = new Vector3(d.x, 0f, d.z);                 // arah hadap datar (tidak menunduk/mendongak di tanjakan)
            if (flat.sqrMagnitude > 0.0004f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), 5f * Time.deltaTime);
        }
    }
}
