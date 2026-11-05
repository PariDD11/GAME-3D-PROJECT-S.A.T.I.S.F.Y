using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    public static partial class SkillAction
    {
        static readonly Color ArrowGold = new Color(1f, 0.93f, 0.62f, 1f);
        static readonly Color ArrowPale = new Color(0.7f, 0.85f, 1f, 1f);

        public static Vector3 PierceEnd, PierceFrom;
        public static readonly List<Vector3> PierceHitPts = new List<Vector3>();

        static Vector3 BowTip(PlayerCombat pc) { return pc.transform.position + Vector3.up * 1.4f + pc.transform.forward * 0.7f; }

        static void RegisterArcher()
        {
            PiercingShot();
            Deadeye();
            Heartseeker();
        }

        // =====================================================================
        // Archer Precision — Piercing Shot  [ar_pre_4]   (2.0 s)
        // Stance -.15 | Draw -.32 | Nock -.48 | Power Draw -.72 | Anchor -.90 | Overdraw -1.08 | Armor-Line -1.20 | Dead Hold -1.30
        // RELEASE 1.30 | Flight -1.45 | First Contact 1.45 | PENETRATION (damage) 1.47 | Exit -1.68 | Follow-through -1.66 | Lower -1.84 | Reset -2.0
        // =====================================================================
        static void PiercingShot()
        {
            var d = new ActionDef { id = "ar_pre_4", dur = 2.0f, lockUntil = 1.95f, hitAt = 1.30f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            var stance = PlayerAnimator.Mk(n, p => { p.arL = V(-40f, -6f, -4f); p.fL = V(-8f, 0f, 0f); p.arR = V(-16f, 20f, 8f); p.fR = V(-20f, 0f, 0f); p.body = V(0f, -28f, 0f); p.head = V(0f, 36f, 0f); p.lgL = V(6f, 0f, 0f); p.lgR = V(-8f, 0f, 0f); });
            var draw = PlayerAnimator.Mk(stance, p => { p.arR = V(-150f, 20f, 12f); p.fR = V(-40f, 0f, 0f); p.arL = V(-70f, -6f, -4f); p.fL = V(-6f, 0f, 0f); });
            var nock = PlayerAnimator.Mk(draw, p => { p.arL = V(-92f, -6f, -4f); p.fL = V(-4f, 0f, 0f); p.arR = V(-88f, 38f, 6f); p.fR = V(-58f, 0f, 0f); p.body = V(0f, -32f, 0f); p.head = V(0f, 40f, 0f); });
            var power = PlayerAnimator.Mk(nock, p => { p.fR = V(-128f, 0f, 0f); p.arR = V(-88f, 38f, 8f); });
            var anchor = PlayerAnimator.Mk(power, p => { p.fR = V(-136f, 0f, 0f); });
            var over = PlayerAnimator.Mk(anchor, p => { p.arR = V(-88f, 46f, 8f); p.fR = V(-142f, 0f, 0f); p.body = V(0f, -35f, 0f); });
            var aim = PlayerAnimator.Mk(over, p => { p.head = V(5f, 40f, 0f); });
            var rel = PlayerAnimator.Mk(aim, p => { p.arR = V(-80f, 62f, 20f); p.fR = V(-60f, 0f, 0f); p.body = V(0f, -30f, 0f); });
            var follow = PlayerAnimator.Mk(rel, p => { p.arR = V(-76f, 70f, 22f); p.fR = V(-48f, 0f, 0f); });
            var lower = PlayerAnimator.Mk(follow, p => { p.arL = V(-40f, -6f, -4f); p.fL = V(-8f, 0f, 0f); p.arR = V(-26f, 22f, 8f); p.fR = V(-26f, 0f, 0f); p.body = V(0f, -14f, 0f); p.head = V(0f, 14f, 0f); });
            var reset = PlayerAnimator.Mk(lower, p => { p.arL = V(-20f, 0f, -8f); p.fL = V(-10f, 0f, 0f); p.arR = V(-10f, 24f, 10f); p.fR = V(-14f, 0f, 0f); p.body = V(0f, 0f, 0f); p.head = V(0f, 0f, 0f); p.lgL = V(0f, 0f, 0f); p.lgR = V(0f, 0f, 0f); });
            d.Key(0f, n).Key(0.15f, stance).Key(0.32f, draw).Key(0.48f, nock).Key(0.72f, power).Key(0.90f, anchor).Key(1.08f, over).Key(1.20f, aim).Key(1.30f, aim)
             .Key(1.34f, rel).Key(1.48f, follow).Key(1.66f, follow).Key(1.84f, lower).Key(2.0f, reset);

            d.At(0f, pc => { PlayerAnimator.HoldArrow = false; pc.DashTo(pc.transform.right, 0.15f, 2.6f); });
            d.At(0.32f, pc => { PlayerAnimator.HoldArrow = true; });
            d.At(0.48f, pc => { Audio.Sfx("click", pc.transform.position); });
            d.At(0.9f, pc => { CombatFx.Host.I.StartCoroutine(PierceEnergy(pc, 0.18f)); });
            d.At(1.08f, pc => { CombatFx.Flash(BowTip(pc), ArrowGold, 3f, 0.35f, 0.3f); });
            d.At(1.20f, pc => { Audio.Sfx("skill_cast", pc.transform.position); });
            d.At(1.299f, pc => { PlayerAnimator.HoldArrow = false; });
            // setelah ExecHit (hitAt 1.30) menyimpan jalur: panah tipis, kontak pertama, retak radial, tembus, flash keluar
            d.At(1.31f, pc =>
            {
                Vector3 from = PierceFrom, end = PierceEnd;
                Vector3 dir = (end - from); if (dir.sqrMagnitude < 0.01f) return; dir.Normalize();
                CombatFx.Arrow(from, end, ArrowPale, 0.55f, 160f);
                CombatFx.ShockRing(from + dir * 0.6f, 0.6f, ArrowGold, 0.12f);
                for (int i = 0; i < PierceHitPts.Count; i++)
                {
                    Vector3 pt = PierceHitPts[i];
                    float delay = Mathf.Min(0.25f, 0.15f + i * 0.03f);
                    CombatFx.Host.I.StartCoroutine(Late(delay, () => PierceContact(pt, dir, true)));
                    CombatFx.Host.I.StartCoroutine(Late(delay + 0.18f, () => PierceContact(pt, dir, false)));
                }
            });
            Add(d);
        }

        static IEnumerator PierceEnergy(PlayerCombat pc, float secs)
        {
            float t = 0f;
            while (t < secs && pc != null)
            {
                float k = t / secs;
                Vector3 tip = BowTip(pc), back = tip - pc.transform.forward * Mathf.Lerp(0.8f, 0.2f, k);
                CombatFx.Beam(back, tip, new Color(1f, 0.95f, 0.75f, 0.8f), 0.03f, 0.1f);
                yield return new WaitForSeconds(0.03f);
                t += 0.03f;
            }
        }

        /// <summary>Kontak pertama (titik putih), lalu retak radial dan penetrasi; entry=false untuk flash keluar di belakang target.</summary>
        static void PierceContact(Vector3 pt, Vector3 dir, bool entry)
        {
            if (entry)
            {
                CombatFx.Flash(pt - dir * 0.4f, Color.white, 5f, 0.5f, 0.06f);
                CombatFx.Host.I.StartCoroutine(Late(0.03f, () =>
                {
                    Audio.Sfx("block", pt);
                    Vector3 r = Vector3.Cross(Vector3.up, dir).normalized, u = Vector3.up;
                    Vector3 c = pt - dir * 0.3f;
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * Mathf.PI / 3f;
                        Vector3 v = (r * Mathf.Cos(a) + u * Mathf.Sin(a)) * 0.9f;
                        CombatFx.Beam(c + v * 0.15f, c + v, ArrowGold, 0.04f, 0.22f);
                    }
                    CombatFx.HitSpark(c, ArrowGold, 0.8f, 10);
                    CombatFx.Burst(c, ArrowPale, 6, 0.4f, 0.9f, 0.6f, 0.04f);
                }));
            }
            else
            {
                Vector3 e = pt + dir * 1.0f;
                CombatFx.Flash(e, ArrowGold, 4f, 0.7f, 0.12f);
                CombatFx.HitSpark(e, Color.white, 0.7f, 8);
                CombatFx.Beam(pt, e + dir * 1.5f, new Color(1f, 0.95f, 0.8f, 0.7f), 0.05f, 0.25f);
            }
        }

        // =====================================================================
        // Archer Precision — Deadeye  [ar_pre_5]   (2.2 s)
        // Stance -.15 | Quick Nock -.30 | Sight Raise -.48 | Weak Point Scan -.72 | Selection -.90 | Precision Draw -1.10 | Alignment -1.26 | DEAD LOCK -1.38
        // RELEASE 1.38 | Flight -1.52 | WEAK POINT HIT 1.52 (damage, crit/normal) | Follow -1.68 | Sight Release -1.84 | Lower -2.02 | Reset 2.18
        // =====================================================================
        static Monster deadeyeMon;

        public static Vector3 DeadeyeWeakPoint(Monster m) { return m.HitPoint; }

        static void Deadeye()
        {
            var d = new ActionDef { id = "ar_pre_5", dur = 2.2f, lockUntil = 2.12f, hitAt = 1.38f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            var stance = PlayerAnimator.Mk(n, p => { p.arL = V(-30f, -6f, -4f); p.fL = V(-6f, 0f, 0f); p.arR = V(-16f, 20f, 8f); p.fR = V(-20f, 0f, 0f); p.body = V(0f, -22f, 0f); p.head = V(0f, 30f, 0f); p.lgL = V(-8f, 0f, 0f); p.lgR = V(8f, 0f, 0f); });
            var quiver = PlayerAnimator.Mk(stance, p => { p.arR = V(-150f, 20f, 12f); p.fR = V(-40f, 0f, 0f); });
            var nock = PlayerAnimator.Mk(quiver, p => { p.arR = V(-76f, 30f, 6f); p.fR = V(-62f, 0f, 0f); p.arL = V(-62f, -6f, -4f); });
            var raise = PlayerAnimator.Mk(nock, p => { p.arL = V(-92f, -6f, -4f); p.fL = V(-4f, 0f, 0f); p.arR = V(-88f, 38f, 6f); p.fR = V(-60f, 0f, 0f); p.body = V(0f, -30f, 0f); p.head = V(-2f, 38f, 0f); });
            var s1 = PlayerAnimator.Mk(raise, p => { p.head = V(-5f, 44f, 0f); p.arL = V(-94f, -8f, -4f); });
            var s2 = PlayerAnimator.Mk(raise, p => { p.head = V(4f, 36f, 0f); p.arL = V(-90f, -4f, -4f); });
            var s3 = PlayerAnimator.Mk(raise, p => { p.head = V(0f, 41f, 0f); });
            var draw = PlayerAnimator.Mk(s3, p => { p.arR = V(-88f, 38f, 8f); p.fR = V(-134f, 0f, 0f); });
            var align = PlayerAnimator.Mk(draw, p => { p.head = V(2f, 40f, 0f); p.arL = V(-92f, -6f, -4f); });
            var rel = PlayerAnimator.Mk(align, p => { p.arR = V(-80f, 62f, 20f); p.fR = V(-60f, 0f, 0f); });
            var follow = PlayerAnimator.Mk(rel, p => { p.arR = V(-76f, 68f, 22f); p.fR = V(-50f, 0f, 0f); });
            var look = PlayerAnimator.Mk(follow, p => { p.head = V(16f, 38f, 0f); });
            var lower = PlayerAnimator.Mk(look, p => { p.arL = V(-34f, -6f, -4f); p.fL = V(-6f, 0f, 0f); p.arR = V(-26f, 22f, 8f); p.fR = V(-26f, 0f, 0f); p.body = V(0f, -12f, 0f); p.head = V(0f, 14f, 0f); p.lgL = V(0f, 0f, 0f); p.lgR = V(0f, 0f, 0f); });
            var reset = PlayerAnimator.Mk(lower, p => { p.arL = V(-20f, 0f, -8f); p.fL = V(-10f, 0f, 0f); p.arR = V(-120f, 24f, 10f); p.fR = V(-30f, 0f, 0f); p.body = V(0f, 0f, 0f); p.head = V(0f, 0f, 0f); });
            d.Key(0f, n).Key(0.15f, stance).Key(0.30f, quiver).Key(0.34f, nock).Key(0.48f, raise).Key(0.56f, s1).Key(0.64f, s2).Key(0.72f, s3).Key(0.90f, s3)
             .Key(1.10f, draw).Key(1.26f, align).Key(1.38f, align).Key(1.42f, rel).Key(1.52f, follow).Key(1.68f, look).Key(1.84f, look).Key(2.02f, lower).Key(2.18f, reset);

            d.At(0f, pc => { PlayerAnimator.HoldArrow = false; });
            d.At(0.15f, pc => { PlayerAnimator.HoldArrow = true; });
            d.At(0.30f, pc => { Audio.Sfx("click", pc.transform.position); });
            d.At(0.48f, pc => { deadeyeMon = SkillExec.AimMonster(pc); if (deadeyeMon != null) CombatFx.Host.I.StartCoroutine(DeadeyeReticle(pc, deadeyeMon)); });
            d.At(1.26f, pc => { CombatFx.Flash(BowTip(pc), Color.white, 2.5f, 0.2f, 0.15f); });
            d.At(1.30f, pc => { Audio.Sfx("skill_cast", pc.transform.position); });
            d.At(1.379f, pc => { PlayerAnimator.HoldArrow = false; });
            Add(d);
        }

        static void Diamond(Vector3 c, Vector3 r, Vector3 u, float s, Color col, float th)
        {
            Vector3 a = c + u * s, b = c + r * s, e = c - u * s, f = c - r * s;
            CombatFx.Beam(a, b, col, th, 0.06f); CombatFx.Beam(b, e, col, th, 0.06f);
            CombatFx.Beam(e, f, col, th, 0.06f); CombatFx.Beam(f, a, col, th, 0.06f);
        }

        /// <summary>Reticle kecil 3 kandidat -> 1 titik -> mengecil -> lock. Mengikuti badan monster; bukan crosshair FPS.</summary>
        static IEnumerator DeadeyeReticle(PlayerCombat pc, Monster m)
        {
            float t = 0f;
            while (t < 1.04f && pc != null && m != null && !m.IsDead)
            {
                Transform cam = pc.Cam;
                Vector3 r = cam != null ? cam.right : pc.transform.right, u = cam != null ? cam.up : Vector3.up;
                Vector3 c = m.HitPoint, up = Vector3.up * 0.45f, side = r * 0.35f;
                Vector3 head = c + up, gap = c + up * 0.5f + side;
                if (t < 0.24f)
                {
                    int hi = Mathf.Min(2, (int)(t / 0.08f));
                    Vector3[] pts = { head, c, gap };
                    for (int i = 0; i < 3; i++) Diamond(pts[i], r, u, 0.1f, new Color(1f, 0.95f, 0.75f, i == hi ? 0.55f : 0.22f), 0.025f);
                }
                else if (t < 0.42f)
                {
                    Diamond(c, r, u, 0.16f, new Color(1f, 0.95f, 0.75f, 0.85f), 0.035f);
                    CombatFx.Beam(c - r * 0.02f, c + r * 0.02f, Color.white, 0.06f, 0.06f);
                }
                else if (t < 0.78f)
                {
                    float k = (t - 0.42f) / 0.36f, s = Mathf.Lerp(0.16f, 0.09f, k);
                    Diamond(c, r, u, s, new Color(1f, 0.95f, 0.75f, 0.9f), 0.03f);
                    Diamond(c + r * 0.3f, r, u, 0.04f, new Color(1f, 0.95f, 0.75f, 0.5f), 0.02f);
                    Diamond(c - r * 0.3f, r, u, 0.04f, new Color(1f, 0.95f, 0.75f, 0.5f), 0.02f);
                }
                else
                {
                    if (t < 0.84f) Audio.Sfx("click", c);
                    CombatFx.Beam(c - r * 0.025f, c + r * 0.025f, Color.white, 0.07f, 0.06f);
                    Diamond(c, r, u, 0.06f, new Color(1f, 1f, 0.9f, 1f), 0.03f);
                }
                yield return new WaitForSeconds(0.04f);
                t += 0.04f;
            }
        }

        /// <summary>Weak point hit: normal = flash + 4 garis; crit = hit-stop, cross-diamond sempurna runtuh jadi needle-flash.</summary>
        public static void DeadeyeImpact(Vector3 pt, bool crit, bool hasTarget)
        {
            Transform cam = Camera.main != null ? Camera.main.transform : null;
            Vector3 r = cam != null ? cam.right : Vector3.right, u = cam != null ? cam.up : Vector3.up;
            Color gold = new Color(1f, 0.92f, 0.6f, 1f);
            Audio.Sfx("swing2", pt);
            CombatFx.Flash(pt, Color.white, crit ? 6f : 4f, crit ? 0.7f : 0.45f, 0.08f);
            if (!hasTarget) return;
            if (!crit)
            {
                Vector3 d1 = (r + u).normalized * 0.4f, d2 = (r - u).normalized * 0.4f;
                CombatFx.Beam(pt - d1, pt + d1, Color.white, 0.03f, 0.14f);
                CombatFx.Beam(pt - d2, pt + d2, Color.white, 0.03f, 0.14f);
                CombatFx.Beam(pt - u * 0.4f, pt + u * 0.4f, Color.white, 0.03f, 0.14f);
                CombatFx.Beam(pt - r * 0.4f, pt + r * 0.4f, Color.white, 0.03f, 0.14f);
                CombatFx.HitSpark(pt, Color.white, 0.5f, 6);
                return;
            }
            Audio.Sfx("click", pt);
            HitStop(0.07f);
            ThirdPersonCamera.Shake(0.12f, 0.12f);
            // cross-diamond sempurna lalu runtuh ke satu titik
            Diamond(pt, r, u, 0.34f, gold, 0.04f);
            CombatFx.Beam(pt - u * 0.7f, pt + u * 0.7f, gold, 0.03f, 0.16f);
            CombatFx.Beam(pt - r * 0.7f, pt + r * 0.7f, gold, 0.03f, 0.16f);
            CombatFx.Beam(pt - (r + u).normalized * 0.5f, pt + (r + u).normalized * 0.5f, gold, 0.03f, 0.16f);
            CombatFx.Beam(pt - (r - u).normalized * 0.5f, pt + (r - u).normalized * 0.5f, gold, 0.03f, 0.16f);
            CombatFx.Host.I.StartCoroutine(Late(0.07f, () =>
            {
                Diamond(pt, r, u, 0.14f, Color.white, 0.04f);
                CombatFx.Beam(pt - r * 1.8f, pt + r * 1.8f, new Color(1f, 0.97f, 0.85f, 1f), 0.08f, 0.2f);
                CombatFx.Flash(pt, gold, 7f, 0.9f, 0.12f);
                CombatFx.HitSpark(pt, gold, 0.7f, 10);
            }));
        }

        // =====================================================================
        // Archer Precision — Heartseeker  [ar_pre_6]  ULTIMATE (3.25 s)  "Vital Synchronization"
        // Stance -.20 | Silent Breath -.42 | Vital Sight -.68 | Seeking -.95 | Heart Found -1.12 | Arrow -1.35 | Sacred Nock -1.58 | Deep Draw -1.88 | Alignment -2.08
        // Synchronization -2.28 | Between Heartbeats -2.42 | RELEASE 2.42 | Flight -2.62 | CONTACT 2.62 (TIK, hit-stop) | HIT 2.74 (internal pulse) | Vital Shock -2.98
        // Follow -2.68 | Confirm -2.88 | Lower -3.08 | Reset 3.25
        // =====================================================================
        static Monster heartMon;

        static void Heartseeker()
        {
            var d = new ActionDef { id = "ar_pre_6", dur = 3.25f, lockUntil = 3.2f, hitAt = 2.42f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            var stance = PlayerAnimator.Mk(n, p => { p.arL = V(-24f, -6f, -4f); p.fL = V(-6f, 0f, 0f); p.arR = V(-8f, 14f, 8f); p.fR = V(-12f, 0f, 0f); p.body = V(2f, -18f, 0f); p.head = V(10f, 20f, 0f); p.lgL = V(-10f, 0f, 0f); p.lgR = V(10f, 0f, 0f); });
            var breath = PlayerAnimator.Mk(stance, p => { p.arL = V(-50f, -6f, -4f); p.fL = V(-16f, 0f, 0f); p.body = V(-2f, -18f, 0f); p.head = V(4f, 20f, 0f); });
            var sight = PlayerAnimator.Mk(breath, p => { p.head = V(-4f, 30f, 0f); p.arR = V(-84f, 24f, 8f); p.fR = V(-110f, 0f, 0f); });
            var seek = PlayerAnimator.Mk(sight, p => { p.arR = V(-30f, 24f, 8f); p.fR = V(-20f, 0f, 0f); p.head = V(2f, 36f, 0f); p.body = V(0f, -22f, 0f); });
            var found = PlayerAnimator.Mk(seek, p => { p.head = V(0f, 34f, 0f); });
            var quiver = PlayerAnimator.Mk(found, p => { p.arR = V(-150f, 20f, 12f); p.fR = V(-40f, 0f, 0f); });
            var nock = PlayerAnimator.Mk(quiver, p => { p.arL = V(-92f, -6f, -4f); p.fL = V(-4f, 0f, 0f); p.arR = V(-88f, 38f, 6f); p.fR = V(-60f, 0f, 0f); p.body = V(0f, -34f, 0f); p.head = V(0f, 40f, 0f); });
            var draw = PlayerAnimator.Mk(nock, p => { p.arR = V(-88f, 38f, 8f); p.fR = V(-136f, 0f, 0f); });
            var hold = PlayerAnimator.Mk(draw, p => { p.head = V(1f, 40f, 0f); });
            var rel = PlayerAnimator.Mk(hold, p => { p.arR = V(-80f, 62f, 20f); p.fR = V(-60f, 0f, 0f); });
            var follow = PlayerAnimator.Mk(rel, p => { p.arR = V(-78f, 66f, 22f); p.fR = V(-52f, 0f, 0f); });
            var breathe = PlayerAnimator.Mk(follow, p => { p.arL = V(-86f, -6f, -4f); p.body = V(0f, -30f, 0f); p.head = V(6f, 38f, 0f); });
            var lower = PlayerAnimator.Mk(breathe, p => { p.arL = V(-34f, -6f, -4f); p.fL = V(-6f, 0f, 0f); p.arR = V(-26f, 22f, 8f); p.fR = V(-26f, 0f, 0f); p.body = V(0f, -10f, 0f); p.head = V(0f, 10f, 0f); p.lgL = V(0f, 0f, 0f); p.lgR = V(0f, 0f, 0f); });
            var reset = PlayerAnimator.Mk(lower, p => { p.arL = V(-20f, 0f, -8f); p.fL = V(-10f, 0f, 0f); p.arR = V(-120f, 24f, 10f); p.fR = V(-30f, 0f, 0f); p.body = V(0f, 0f, 0f); p.head = V(0f, 0f, 0f); });
            d.Key(0f, n).Key(0.20f, stance).Key(0.42f, breath).Key(0.68f, sight).Key(0.95f, seek).Key(1.12f, found).Key(1.35f, quiver).Key(1.58f, nock).Key(1.88f, draw).Key(2.08f, hold).Key(2.42f, hold)
             .Key(2.46f, rel).Key(2.68f, follow).Key(2.88f, breathe).Key(3.08f, lower).Key(3.25f, reset);

            d.At(0f, pc => { PlayerAnimator.HoldArrow = false; });
            d.At(0.42f, pc => { heartMon = SkillExec.AimMonster(pc); Audio.Sfx("skill_cast", pc.transform.position); if (heartMon != null) CombatFx.Host.I.StartCoroutine(HeartReticle(pc, heartMon)); });
            d.At(1.12f, pc => { PlayerAnimator.HoldArrow = true; });
            d.At(1.35f, pc => { Audio.Sfx("click", pc.transform.position); CombatFx.Flash(BowTip(pc), new Color(1f, 0.8f, 0.5f, 1f), 3f, 0.35f, 0.25f); });
            d.At(1.58f, pc => { CombatFx.Host.I.StartCoroutine(HeartEnergyFlow(pc, 0.3f)); });
            d.At(2.28f, pc => { CombatFx.Flash(BowTip(pc), Color.white, 2f, 0.15f, 0.2f); });
            d.At(2.419f, pc => { PlayerAnimator.HoldArrow = false; });
            d.At(2.88f, pc => { CombatFx.Burst(pc.transform.position + Vector3.up * 1.5f, new Color(1f, 1f, 1f, 0.3f), 3, 0.3f, 0.4f, 0.6f, 0.04f); });
            Add(d);
        }

        static void Circle(Vector3 c, Vector3 r, Vector3 u, float s, Color col, float th)
        {
            Vector3 prev = c + r * s;
            for (int i = 1; i <= 12; i++)
            {
                float a = i * Mathf.PI * 2f / 12f;
                Vector3 cur = c + (r * Mathf.Cos(a) + u * Mathf.Sin(a)) * s;
                CombatFx.Beam(prev, cur, col, th, 0.06f);
                prev = cur;
            }
        }

        /// <summary>Vital Synchronization: silhouette samar -> garis scan ke core -> lingkaran tak sejajar -> sejajar -> ◉ berdenyut -> garis bidik -> ◉ jadi • di antara detak.</summary>
        static IEnumerator HeartReticle(PlayerCombat pc, Monster m)
        {
            float t = 0f; bool th1 = false, th2 = false, th3 = false, th4 = false;
            Color col = new Color(1f, 0.85f, 0.6f, 1f), crim = new Color(1f, 0.35f, 0.3f, 1f);
            while (t < 2.2f && pc != null && m != null && !m.IsDead)
            {
                Transform cam = pc.Cam;
                Vector3 r = cam != null ? cam.right : pc.transform.right, u = cam != null ? cam.up : Vector3.up;
                Vector3 c = m.HitPoint;
                float h = 1.6f;
                if (t < 0.26f)
                {
                    Circle(c, r, u, 0.5f, new Color(col.r, col.g, col.b, 0.12f), 0.02f);
                    CombatFx.Beam(c - u * h * 0.5f, c + u * h * 0.5f, new Color(col.r, col.g, col.b, 0.1f), 0.02f, 0.06f);
                }
                else if (t < 0.53f)
                {
                    float k = (t - 0.26f) / 0.27f;
                    for (int i = 0; i < 3; i++)
                    {
                        float a = i * 2.1f + t * 1.5f;
                        Vector3 start = c + (r * Mathf.Cos(a) + u * Mathf.Sin(a)) * 0.9f;
                        CombatFx.Beam(start, Vector3.Lerp(start, c, k), new Color(col.r, col.g, col.b, 0.3f), 0.02f, 0.06f);
                        Vector3 off = (r * Mathf.Cos(a + 2f) + u * Mathf.Sin(a + 2f)) * Mathf.Lerp(0.3f, 0f, k);
                        Circle(c + off, r, u, 0.12f + 0.04f * i, new Color(col.r, col.g, col.b, 0.3f + 0.4f * k), 0.02f);
                    }
                }
                else if (t < 0.70f)
                {
                    float p = (t - 0.53f) / 0.17f, pulse = 1f + 0.35f * Mathf.Sin(p * Mathf.PI * 4f);
                    if (!th1) { th1 = true; Audio.Sfx("hit_heavy", c); }
                    Circle(c, r, u, 0.16f * pulse, col, 0.03f);
                    CombatFx.Beam(c - r * 0.3f, c - r * 0.1f, col, 0.02f, 0.06f); CombatFx.Beam(c + r * 0.1f, c + r * 0.3f, col, 0.02f, 0.06f);
                    CombatFx.Beam(c + u * 0.1f, c + u * 0.3f, col, 0.02f, 0.06f); CombatFx.Beam(c - u * 0.3f, c - u * 0.1f, col, 0.02f, 0.06f);
                    Circle(c, r, u, 0.05f, Color.white, 0.04f);
                }
                else if (t < 1.66f)
                {
                    Circle(c, r, u, 0.13f, new Color(col.r, col.g, col.b, 0.5f), 0.025f);
                    Circle(c, r, u, 0.05f, new Color(1f, 1f, 1f, 0.6f), 0.03f);
                }
                else if (t < 1.86f)
                {
                    float p = (t - 1.66f) / 0.2f, pulse = 1f + 0.4f * Mathf.Sin(p * Mathf.PI);
                    if (!th2) { th2 = true; Audio.Sfx("hit_heavy", c); }
                    Circle(c, r, u, 0.13f * pulse, col, 0.03f); Circle(c, r, u, 0.05f, Color.white, 0.04f);
                    CombatFx.Beam(BowTip(pc), c, new Color(1f, 0.95f, 0.85f, 0.15f), 0.012f, 0.06f);
                }
                else if (t < 2.0f)
                {
                    Circle(c, r, u, 0.12f, new Color(col.r, col.g, col.b, 0.5f), 0.025f); Circle(c, r, u, 0.05f, Color.white, 0.03f);
                    CombatFx.Beam(BowTip(pc), c, new Color(1f, 0.95f, 0.85f, 0.15f), 0.012f, 0.06f);
                }
                else if (t < 2.2f)
                {
                    float p = (t - 2.0f) / 0.2f;
                    if (!th3 && p > 0.5f) { th3 = true; Audio.Sfx("hit_heavy", c); }
                    float s = p < 0.5f ? 0.12f : Mathf.Lerp(0.12f, 0.02f, (p - 0.5f) * 2f);
                    Circle(c, r, u, 0.13f * (1f + 0.4f * Mathf.Sin(Mathf.Min(1f, p * 2f) * Mathf.PI)), crim, 0.025f);
                    Circle(c, r, u, s, Color.white, 0.04f);
                    CombatFx.Beam(BowTip(pc), c, new Color(1f, 0.95f, 0.85f, 0.2f), 0.012f, 0.06f);
                }
                yield return new WaitForSeconds(0.04f);
                t += 0.04f;
            }
            // 2.28-2.42 (relatif 1.86..2.0 sudah lewat): setelah detak kedua reticle jadi titik; ditahan sampai release lewat loop pendek
            float tail = 0f;
            while (tail < 0.14f && pc != null && m != null && !m.IsDead)
            {
                Transform cam = pc.Cam;
                Vector3 r = cam != null ? cam.right : pc.transform.right;
                if (!th4) { th4 = true; }
                CombatFx.Beam(m.HitPoint - r * 0.02f, m.HitPoint + r * 0.02f, Color.white, 0.07f, 0.06f);
                yield return new WaitForSeconds(0.04f);
                tail += 0.04f;
            }
        }

        /// <summary>BOW LIMB -> STRING -> SHAFT -> ARROWHEAD; makin lama makin kecil (dikompresi).</summary>
        static IEnumerator HeartEnergyFlow(PlayerCombat pc, float secs)
        {
            float t = 0f;
            while (t < secs && pc != null)
            {
                float k = t / secs;
                Vector3 tip = BowTip(pc), limb = tip + Vector3.up * 0.7f - pc.transform.forward * 0.1f, str = tip - pc.transform.forward * 0.6f;
                Vector3 p = k < 0.5f ? Vector3.Lerp(limb, str, k * 2f) : Vector3.Lerp(str, tip, (k - 0.5f) * 2f);
                CombatFx.Beam(p, p + (tip - str).normalized * 0.15f, new Color(1f, 0.85f, 0.55f, 0.9f), 0.03f * (1f - 0.6f * k), 0.1f);
                yield return new WaitForSeconds(0.03f);
                t += 0.03f;
            }
        }

        /// <summary>Panah lurus + koreksi 5-10 derajat ke vital point; TIK, hit-stop, lalu pulse internal & Vital Shock.</summary>
        public static IEnumerator HeartseekerFlight(PlayerCombat pc, Vector3 from, Vector3 end, Monster m, float dmg)
        {
            Color needle = new Color(1f, 0.95f, 0.8f, 0.8f);
            Vector3 aimPt = m != null ? m.HitPoint : end;
            Vector3 mid = Vector3.Lerp(from, aimPt, 0.6f);
            CombatFx.Arrow(from, mid, needle, 0.4f, 150f);
            CombatFx.Beam(from, mid, new Color(1f, 0.95f, 0.8f, 0.5f), 0.02f, 0.15f);
            // gelombang detak jantung tipis di belakang kepala panah
            Vector3 dirF = (mid - from).normalized, rr = Vector3.Cross(Vector3.up, dirF);
            for (int i = 1; i <= 3; i++)
                CombatFx.Beam(from + dirF * (i * 0.8f) - rr * 0.1f, from + dirF * (i * 0.8f + 0.2f) + rr * 0.1f, new Color(1f, 0.5f, 0.45f, 0.35f), 0.015f, 0.1f);
            yield return new WaitForSeconds(0.1f);
            if (pc == null) yield break;
            Vector3 pt = (m != null && !m.IsDead) ? m.HitPoint : aimPt;
            float dist = (pt - mid).magnitude;
            CombatFx.Arrow(mid, pt, needle, 0.4f, Mathf.Max(60f, dist / 0.1f));
            yield return new WaitForSeconds(0.1f);
            // HEART CONTACT: TIK, satu titik, hit-stop
            Audio.Sfx("click", pt);
            CombatFx.Flash(pt, Color.white, 4f, 0.2f, 0.1f);
            HitStop(0.1f);
            yield return new WaitForSeconds(0.12f);
            // HEARTSEEKER HIT: garis energi menembus core, pulse internal, ring kecil dari dalam
            Vector3 dir = (pt - from).normalized;
            Audio.Sfx("hit_heavy", pt);
            CombatFx.Beam(pt - dir * 1.8f, pt + dir * 2.4f, Color.white, 0.05f, 0.18f);
            CombatFx.Flash(pt, new Color(1f, 0.9f, 0.7f, 1f), 6f, 0.8f, 0.14f);
            for (int i = 0; i < 3; i++)
                CombatFx.Host.I.StartCoroutine(Late(0.06f * i, () => CombatFx.ShockRing(pt, 1.3f + 0.4f * 0f, new Color(1f, 0.55f, 0.4f, 0.55f), 0.3f)));
            ThirdPersonCamera.Shake(0.2f, 0.2f);
            if (m != null && !m.IsDead)
            {
                m.TakeDamage(dmg);
                m.Stagger(1.2f);
                Vector3 back = m.transform.position - from; back.y = 0f;
                if (back.sqrMagnitude > 0.01f) m.Knock(back.normalized, 0.8f);
            }
        }
    }
}
