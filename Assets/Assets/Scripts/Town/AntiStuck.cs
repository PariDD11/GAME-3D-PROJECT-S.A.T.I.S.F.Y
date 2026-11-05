using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Pengaman player tersangkut ("gancet"): memantau pemain yang menekan tombol gerak tetapi tidak bergeser, atau terjepit di dalam collider.
    /// Bertahap: (1) dorong naik/menyamping (tangga & ambang), (2) cari titik bebas terdekat, (3) kembali ke titik aman terakhir.
    /// Otomatis aktif di semua scene; tidak perlu rebuild.
    /// </summary>
    public class AntiStuck : MonoBehaviour
    {
        static AntiStuck I;
        CharacterController cc;
        Vector3 safePos; bool hasSafe; float safeAt;
        Vector3 winStart; float winT, stuckT; int stage;
        float embedT;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("AntiStuck"); DontDestroyOnLoad(go); I = go.AddComponent<AntiStuck>();
        }

        void LateUpdate()
        {
            var pc = PlayerCombat.I;
            if (pc == null) { cc = null; hasSafe = false; return; }
            if (cc == null || cc.gameObject != pc.gameObject) { cc = pc.GetComponent<CharacterController>(); hasSafe = false; winT = 0f; stuckT = 0f; stage = 0; }
            if (cc == null || !cc.enabled) { winT = 0f; return; }
            var ui = TownUI.I;
            bool busy = (ui != null && ui.PanelOpen) || MiniGame.Active || FishingSystem.Busy || Cutscene.Active || PlayerController.Climbing || MountSystem.Mounted || pc.IsDead;
            Vector3 pos = transform.position; pos = pc.transform.position;
            if (busy) { winT = 0f; stuckT = 0f; stage = 0; winStart = pos; return; }

            // titik aman: posisi bebas, menapak tanah, tidak berenang, diperbarui tiap 1.5 dtk
            if (cc.isGrounded && !PlayerController.Swimming && Time.time - safeAt > 1.5f && !Embedded(pc.transform, pos))
            { safePos = pos; hasSafe = true; safeAt = Time.time; }

            // terjepit di dalam collider
            if (Embedded(pc.transform, pos)) { embedT += Time.deltaTime; if (embedT > 0.5f) { embedT = 0f; Free(pc.transform, 1); } }
            else embedT = 0f;

            // menekan gerak tapi tidak bergeser
            Vector2 mv = TownInput.Move();
            if (mv.sqrMagnitude < 0.1f || PlayerController.Swimming) { winT = 0f; stuckT = 0f; stage = 0; winStart = pos; return; }
            winT += Time.deltaTime;
            if (winT >= 0.5f)
            {
                Vector3 d = pos - winStart; d.y = 0f;
                if (d.magnitude < 0.12f) stuckT += winT; else { stuckT = 0f; stage = 0; }
                winT = 0f; winStart = pos;
            }
            if (stuckT > 0.9f && stage == 0) { stage = 1; Nudge(pc.transform); }
            else if (stuckT > 1.8f && stage == 1) { stage = 2; Free(pc.transform, 2); }
            else if (stuckT > 3.6f && stage == 2) { stage = 3; Teleport(pc.transform, hasSafe ? safePos : pos + Vector3.up * 1.5f); stuckT = 0f; }
        }

        void Caps(Vector3 pos, float shrink, out Vector3 p1, out Vector3 p2, out float r)
        {
            r = Mathf.Max(0.1f, cc.radius - shrink);
            Vector3 c = pos + cc.center;
            float half = Mathf.Max(r, cc.height * 0.5f - 0.05f);
            p1 = c + Vector3.up * (half - r); p2 = c - Vector3.up * (half - r);
        }

        bool Blocked(Transform pt, Vector3 pos, float shrink)
        {
            Vector3 a, b; float r; Caps(pos, shrink, out a, out b, out r);
            var hits = Physics.OverlapCapsule(a, b, r, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (h == null || h == cc || h.transform.IsChildOf(pt)) continue;
                if (h.GetComponentInParent<Monster>() != null || h.GetComponentInParent<NpcActor>() != null) continue;
                return true;
            }
            return false;
        }
        bool Embedded(Transform pt, Vector3 pos) { return Blocked(pt, pos, 0.12f); }

        void Teleport(Transform pt, Vector3 to)
        {
            cc.enabled = false; pt.position = to; cc.enabled = true;
            winStart = to; stuckT = 0f;
        }

        // dorongan kecil: naik 0.5 m lalu geser ke arah gerak (tangga/ambang)
        void Nudge(Transform pt)
        {
            Vector2 mv = TownInput.Move(); var cam = Camera.main;
            Vector3 f = cam != null ? cam.transform.forward : pt.forward; f.y = 0f; f.Normalize();
            Vector3 r = new Vector3(f.z, 0f, -f.x);
            Vector3 dir = (f * mv.y + r * mv.x); if (dir.sqrMagnitude < 0.01f) dir = f; dir.Normalize();
            for (float up = 0.45f; up <= 1.1f; up += 0.33f)
            {
                Vector3 to = pt.position + Vector3.up * up + dir * 0.55f;
                if (!Blocked(pt, to, 0.05f)) { Teleport(pt, to); return; }
            }
        }

        // cari titik bebas terdekat (cincin makin lebar), dengan tanah di bawahnya
        void Free(Transform pt, int level)
        {
            Vector3 p = pt.position;
            for (float rad = 0.6f; rad <= (level == 1 ? 2.4f : 5f); rad += 0.6f)
                for (int k = 0; k < 12; k++)
                {
                    float a = k * 30f * Mathf.Deg2Rad;
                    for (int yo = 0; yo < 3; yo++)
                    {
                        Vector3 q = p + new Vector3(Mathf.Sin(a) * rad, yo * 0.6f, Mathf.Cos(a) * rad);
                        if (Blocked(pt, q, 0.05f)) continue;
                        float gy = RT.WalkY(q, pt);
                        if (q.y - gy > 3f) continue;
                        Teleport(pt, q); return;
                    }
                }
            if (level == 2 && hasSafe) Teleport(pt, safePos);
        }
    }
}
