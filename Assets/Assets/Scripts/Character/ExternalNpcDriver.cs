using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Menggerakkan Animator model NPC asli: parameter float Speed, trigger Wave. Menggantikan NpcAnimator prosedural.</summary>
    public class ExternalNpcDriver : NpcAnimator
    {
        Animator an; readonly HashSet<string> pars = new HashSet<string>();
        Vector3 last; float speedSm; Transform look; bool talking;
        public bool workWhenIdle;
        public float animSpeed = 1f, speedScale = 1f;

        public void Init(Animator a)
        {
            an = a; last = transform.position;
            if (an != null) { an.applyRootMotion = false; an.speed = animSpeed; foreach (var p in an.parameters) pars.Add(p.name); }
        }

        public override void Wave(float seconds = 2.2f) { if (an != null && pars.Contains("Wave")) an.SetTrigger("Wave"); }
        public override void LookAt(Transform t) { look = t; }
        public override void Talk(bool on) { talking = on; if (an != null && pars.Contains("Talking")) an.SetBool("Talking", on); }

        protected override void LateUpdate()
        {
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            Vector3 d = transform.position - last; d.y = 0f; last = transform.position;
            speedSm = Mathf.Lerp(speedSm, Mathf.Min(d.magnitude / dt, 10f), 8f * dt);
            if (an != null && pars.Contains("Speed")) an.SetFloat("Speed", speedSm * speedScale);
            if (an != null && pars.Contains("Working")) an.SetBool("Working", workWhenIdle && !talking && speedSm < 0.2f);
            if (look != null && speedSm < 0.2f)
            {
                Vector3 to = look.position - transform.position; to.y = 0f;
                if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), 180f * dt);
            }
        }
    }
}
