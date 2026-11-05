using UnityEngine;

namespace HunterGuild
{
    /// <summary>Animasi prosedural NPC: ayun kaki/tangan dari kecepatan gerak, napas, goyang ekor, melambai.</summary>
    public class NpcAnimator : MonoBehaviour
    {
        public Transform body, head, armL, armR, legL, legR, tail;
        public float scale = 1f;

        Vector3 last; float phase, speedSm, waveT, t0, lookWeight;
        Vector3 bodyBase; Transform lookAt;

        void Start() { last = transform.position; t0 = Random.value * 10f; if (body != null) bodyBase = body.localPosition; }

        public virtual void Wave(float seconds = 2.2f) { waveT = seconds; }
        public virtual void LookAt(Transform t) { lookAt = t; }
        public virtual void Talk(bool on) { }

        protected virtual void LateUpdate()
        {
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            Vector3 d = transform.position - last; d.y = 0f; last = transform.position;
            float v = Mathf.Min(d.magnitude / dt, 6f);
            speedSm = Mathf.Lerp(speedSm, v, 8f * dt);
            float walk = Mathf.Clamp01(speedSm / 1.2f);
            phase += speedSm * 4.2f / Mathf.Max(scale, 0.6f) * dt;
            float sw = Mathf.Sin(phase) * 34f * walk;
            float tt = Time.time + t0;
            float breath = Mathf.Sin(tt * 1.7f);

            if (legL) legL.localRotation = Quaternion.Euler(sw, 0f, 0f);
            if (legR) legR.localRotation = Quaternion.Euler(-sw, 0f, 0f);
            if (armL) armL.localRotation = Quaternion.Euler(-sw * 0.8f + breath * 1.5f + Mathf.Sin(tt * 0.7f) * 1.5f, 0f, -5f - breath * 1.2f);
            if (armR)
            {
                if (waveT > 0f)
                {
                    waveT -= dt;
                    float k = Mathf.Clamp01(Mathf.Min(waveT, 2.2f - (2.2f - waveT)) * 4f);
                    armR.localRotation = Quaternion.Euler(-150f, 0f, 8f * Mathf.Sin(tt * 11f) * k + 6f);
                }
                else armR.localRotation = Quaternion.Euler(sw * 0.8f - breath * 1.5f + Mathf.Sin(tt * 0.6f + 1f) * 1.5f, 0f, 5f + breath * 1.2f);
            }
            if (body != null)
            {
                float bob = Mathf.Abs(Mathf.Sin(phase)) * 0.03f * walk * scale + breath * 0.004f * scale;
                float idle = 1f - walk;
                body.localPosition = bodyBase + new Vector3(Mathf.Sin(tt * 0.5f) * 0.012f * idle, bob, 0f);
                body.localRotation = Quaternion.Euler(walk * 3f + breath * 0.6f, Mathf.Sin(phase) * 3f * walk + Mathf.Sin(tt * 0.35f) * 4f * idle, Mathf.Sin(tt * 0.5f) * 1.6f * idle);
            }
            if (tail) tail.localRotation = Quaternion.Euler(Mathf.Sin(tt * 2.1f) * 6f, Mathf.Sin(tt * 2.6f + walk * phase) * (14f + 12f * walk), 0f);
            if (head)
            {
                float lk = 1f - Mathf.Clamp01(speedSm);
                Quaternion q = Quaternion.Euler(Mathf.Sin(tt * 0.9f) * 2.5f * lk, Mathf.Sin(tt * 0.45f) * 22f * lk, Mathf.Sin(tt * 0.6f) * 3f * lk);
                if (lookAt != null)
                {
                    Vector3 local = transform.InverseTransformPoint(lookAt.position + Vector3.up * 1.5f) - head.parent.localPosition;
                    float yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -55f, 55f);
                    q = Quaternion.Euler(0f, yaw, 0f);
                }
                head.localRotation = Quaternion.Slerp(head.localRotation, q, 6f * dt);
            }
        }
    }
}
