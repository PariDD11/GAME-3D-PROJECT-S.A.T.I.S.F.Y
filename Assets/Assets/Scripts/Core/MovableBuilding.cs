using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Penanda satu bangunan yang bisa digeser/diputar manual di Editor (pilih objek "Bangunan_..." lalu pakai Move/Rotate tool).
    /// Papan nama, pintu, bedeng bunga, NPC penjaga di sekitar bangunan sudah menjadi anak objek ini, jadi ikut berpindah.
    /// Posisi buatanmu disimpan lewat Hunter Guild > Layout > Simpan posisi manual, dan dipasang ulang otomatis setiap build scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class MovableBuilding : MonoBehaviour
    {
        public string buildingName;
        public Vector3 localCenter;
        public Vector3 localSize = new Vector3(8f, 6f, 8f);
        public Vector3 defaultPosition;
        public float defaultYaw;
        public bool isContainer;           // induk komplek (mis. Komplek Properti): menggeser induk menggeser semua isinya

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            Gizmos.DrawSphere(transform.position + Vector3.up * 0.15f, 0.35f);
        }

        void OnDrawGizmosSelected()
        {
            var m = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.2f, 1f, 0.5f, 0.9f);
            Gizmos.DrawWireCube(localCenter, localSize);
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.95f);       // panah depan (+Z) = sisi pintu
            Vector3 f = new Vector3(0f, 0.5f, localCenter.z + localSize.z / 2f);
            Gizmos.DrawLine(f, f + Vector3.forward * 3f);
            Gizmos.DrawLine(f + Vector3.forward * 3f, f + new Vector3(0.8f, 0f, 2.2f));
            Gizmos.DrawLine(f + Vector3.forward * 3f, f + new Vector3(-0.8f, 0f, 2.2f));
            Gizmos.matrix = m;
        }

        /// <summary>Tinggi tanah di titik p: terrain bila ada, selain itu raycast ke collider datar (bukan bangunan lain).</summary>
        public static float GroundHeight(Vector3 p, Transform self)
        {
            var t = RT.TerrainAt(p);
            if (t != null && t.terrainData != null)
            {
                Vector3 o = t.transform.position, sz = t.terrainData.size;
                if (p.x >= o.x && p.x <= o.x + sz.x && p.z >= o.z && p.z <= o.z + sz.z)
                    return t.SampleHeight(p) + o.y;
            }
            Physics.SyncTransforms();
            float best = float.NegativeInfinity;
            var hits = Physics.RaycastAll(new Vector3(p.x, p.y + 8f, p.z), Vector3.down, 80f, ~0, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
            {
                if (h.normal.y < 0.7f) continue;
                if (self != null && h.collider.transform.IsChildOf(self)) continue;
                if (h.collider.GetComponentInParent<MovableBuilding>() != null) continue;
                if (h.point.y > best) best = h.point.y;
            }
            return float.IsNegativeInfinity(best) ? p.y : best;
        }

        [ContextMenu("Snap ke tanah")]
        public void SnapToGround()
        {
            Vector3 p = transform.position;
            p.y = GroundHeight(p, transform);
            transform.position = p;
        }

        [ContextMenu("Kembalikan ke posisi bawaan")]
        public void ResetToDefault()
        {
            transform.position = defaultPosition;
            transform.rotation = Quaternion.Euler(0f, defaultYaw, 0f);
        }
    }
}
