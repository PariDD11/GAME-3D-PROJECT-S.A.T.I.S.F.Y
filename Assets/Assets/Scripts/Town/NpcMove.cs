using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Pemeriksaan tabrakan untuk NPC yang digerakkan lewat transform (WanderNPC, partner): NPC tidak boleh menembus tembok,
    /// bangunan, pagar, atau benda padat lain. Terrain, pemain, monster, dan sesama NPC diabaikan.
    /// </summary>
    public static class NpcMove
    {
        static readonly RaycastHit[] hitBuf = new RaycastHit[24];
        static readonly Collider[] colBuf = new Collider[24];

        static bool IsSolid(Collider c, Transform self)
        {
            if (c == null || c.isTrigger) return false;
            if (c.GetType().Name == "TerrainCollider") return false;   // tanpa referensi tipe: modul Terrain Physics boleh nonaktif
            if (c.transform.IsChildOf(self)) return false;
            if (c.GetComponentInParent<NpcActor>() != null) return false;
            if (c.GetComponentInParent<PlayerCombat>() != null) return false;
            if (c.GetComponentInParent<Monster>() != null) return false;
            if (c.GetComponentInParent<AllyUnit>() != null) return false;
            if (c.GetComponentInParent<LevelGate>() != null) return false;
            return true;
        }

        /// <summary>True bila langkah sejauh dist ke arah dir (datar) akan menabrak benda padat.</summary>
        public static bool Blocked(Transform self, Vector3 dir, float dist, float radius = 0.36f)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return false;
            dir.Normalize();
            Vector3 p = self.position;
            Vector3 p0 = p + Vector3.up * (radius + 0.35f);       // di atas tinggi anak tangga/kerb
            Vector3 p1 = p + Vector3.up * 1.5f;
            int n = Physics.CapsuleCastNonAlloc(p0, p1, radius, dir, hitBuf, dist + 0.05f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (hitBuf[i].distance <= 0.0001f) continue;      // sudah menempel/menumpuk: ditangani Depenetrate
                if (IsSolid(hitBuf[i].collider, self)) return true;
            }
            return false;
        }

        /// <summary>Dorong NPC keluar bila terlanjur berada di dalam collider padat (mis. spawn terlalu dekat dinding).</summary>
        public static void Depenetrate(Transform self, float radius = 0.34f)
        {
            Vector3 p = self.position;
            Vector3 p0 = p + Vector3.up * (radius + 0.35f), p1 = p + Vector3.up * 1.5f;
            int n = Physics.OverlapCapsuleNonAlloc(p0, p1, radius, colBuf, ~0, QueryTriggerInteraction.Ignore);
            var probe = self.GetComponent<CapsuleCollider>();
            for (int i = 0; i < n; i++)
            {
                var c = colBuf[i];
                if (!IsSolid(c, self)) continue;
                Vector3 push; float d;
                if (probe != null && Physics.ComputePenetration(probe, self.position, self.rotation, c, c.transform.position, c.transform.rotation, out push, out d))
                {
                    push.y = 0f;
                    self.position += push.normalized * (d + 0.02f);
                }
                else
                {
                    Vector3 away = p - c.ClosestPoint(p); away.y = 0f;
                    if (away.sqrMagnitude > 0.0001f) self.position += away.normalized * 0.15f;
                }
            }
        }
    }
}
