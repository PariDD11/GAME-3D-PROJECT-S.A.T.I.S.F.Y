using System.Collections;
using System.Linq;
using UnityEngine;

namespace HunterGuild
{
    // Koreografi skill evolusi Swordsman (Knight, Berserker, Blademaster), ditambah satu per satu.
    public static partial class SkillAction
    {
        static void RegisterEvolutions()
        {
            ShieldSlam();
            TauntingRoar();
            Fortress();
            SavageCleave();
            RageLeap();
            Bloodbath();
            WindCutter();
            FlashStep();
            ThousandCuts();
            JudgementSlam();
            DivineRoar();
            AscendedFortress();
            GroundBreaker();
            BattleFury();
            ArmageddonSlash();
            DragonFang();
            WyrmDash();
            DragonKingSlash();
        }

        // =====================================================================
        // Knight — Shield Slam  [kn_4]
        // Brace 0-0.15 | Lift 0.15-0.35 | Heavy Step 0.35-0.48 | SLAM 0.48-0.68 | Crushing Impact 0.68-0.82 | Sweep Through 0.82-1.02 | Reset 1.02-1.30
        // =====================================================================
        static void ShieldSlam()
        {
            var d = new ActionDef { id = "kn_4", dur = 1.3f, lockUntil = 1.0f, hitAt = 0.68f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            var brace = PlayerAnimator.Mk(n, p => { p.arL = V(-62f, -10f, -14f); p.fL = V(-95f, 0f, 0f); p.arR = V(8f, 30f, 14f); p.fR = V(-16f, 0f, 0f); p.body = V(6f, -14f, 0f); p.head = V(0f, 8f, 0f); p.lgL = V(-24f, 0f, 0f); p.lgR = V(22f, 0f, 0f); p.lift = -0.14f; });
            // Shield Lift: perisai ditarik ke samping lalu diangkat setinggi bahu/kepala, torso memutar 30-45 derajat
            var lift = PlayerAnimator.Mk(brace, p => { p.arL = V(-130f, 32f, -30f); p.fL = V(-70f, 0f, 0f); p.body = V(0f, 42f, 0f); p.head = V(0f, 26f, 0f); p.lgL = V(-10f, 0f, 0f); p.lgR = V(30f, 0f, 0f); p.lift = -0.18f; p.lunge = -0.1f; });
            var step = PlayerAnimator.Mk(lift, p => { p.arL = V(-142f, 24f, -26f); p.body = V(10f, 24f, 0f); p.lunge = 0.26f; p.lift = -0.12f; p.lgL = V(-44f, 0f, 0f); p.lgR = V(22f, 0f, 0f); });
            var slam = PlayerAnimator.Mk(step, p => { p.arL = V(-60f, -48f, -6f); p.fL = V(-20f, 0f, 0f); p.body = V(34f, -40f, 0f); p.head = V(14f, -16f, 0f); p.lunge = 0.42f; p.lift = -0.22f; p.lgL = V(-50f, 0f, 0f); p.lgR = V(28f, 0f, 0f); });
            var impact = PlayerAnimator.Mk(slam, p => { p.body = V(38f, -44f, 0f); p.head = V(18f, -18f, 0f); p.lunge = 0.46f; p.lift = -0.24f; });
            var sweep = PlayerAnimator.Mk(impact, p => { p.arL = V(-30f, -86f, -4f); p.fL = V(-30f, 0f, 0f); p.body = V(30f, -62f, 0f); p.lunge = 0.52f; p.lift = -0.2f; });
            var reset = PlayerAnimator.Mk(sweep, p => { p.arL = V(-62f, -10f, -14f); p.fL = V(-95f, 0f, 0f); p.arR = V(-30f, 12f, 10f); p.fR = V(-60f, 0f, 0f); p.body = V(8f, -10f, 0f); p.lunge = 0.2f; p.lift = -0.1f; });
            d.Key(0f, n).Key(0.15f, brace).Key(0.35f, lift).Key(0.48f, step).Key(0.68f, slam).Key(0.82f, impact).Key(1.02f, sweep).Key(1.30f, reset);

            d.At(0.35f, pc =>
            {
                pc.DashTo(pc.transform.forward, 0.12f, 6f);       // Heavy Step: langkah berat, bukan dash
            });
            d.At(0.48f, pc =>
            {
                Vector3 foot = Feet(pc) + pc.transform.forward * 0.5f;
                CombatFx.Dust(foot, 10);
                MagicFx.Crack(foot, 0.6f, 2.2f);
                Audio.Sfx("block", pc.transform.position);
                CombatFx.Beam(Chest(pc) + Vector3.up * 0.6f, Chest(pc) + pc.transform.forward * 1.8f - Vector3.up * 0.4f, new Color(1f, 0.85f, 0.5f, 0.6f), 0.35f, 0.2f);
            });
            d.At(0.68f, pc =>
            {
                Vector3 f = pc.transform.forward;
                Vector3 pt = Chest(pc) + f * 1.7f;
                Vector3 g; GroundFront(pc, 2.2f, out g);
                HitStop(0.1f);
                CombatFx.Flash(pt, GoldHot, 12f, 3.5f, 0.18f);
                CombatFx.Flash(pt, Color.white, 6f, 2f, 0.08f);
                CombatFx.HitSpark(pt, Gold, 2.2f, 34);
                CombatFx.ShockRing(g, 3.8f, new Color(1f, 0.75f, 0.3f, 0.9f), 0.3f);
                CombatFx.HalfWave(g, f, 4.5f, new Color(1f, 0.8f, 0.4f, 0.8f), 0.35f);       // shockwave berbentuk kipas ke depan
                CombatFx.Dust(g, 16);
                MagicFx.Debri(g, 1.6f, 8, 0.8f);
                ThirdPersonCamera.Shake(0.22f, 0.22f);
            });
            d.At(0.82f, pc =>
            {
                Vector3 g; GroundFront(pc, 2.6f, out g);
                CombatFx.Burst(g + Vector3.up * 0.3f, Gold, 12, 1.2f, 1.6f, 0.8f, 0.08f);
                CombatFx.Dust(g, 8);
            });
            Add(d);
        }

        // =====================================================================
        // Knight — Taunting Roar  [kn_5]
        // Brace 0-0.18 | Shield Plant 0.18-0.38 | Power Rise 0.38-0.58 | ROAR 0.58-0.90 | Taunt Pulse 0.90-1.10 | Fortress Brace 1.10-1.35 | Ready 1.35-1.55 ; buff 5 dtk
        // =====================================================================
        public static float GuardUntil;
        static Color guardMain = new Color(1f, 0.72f, 0.22f), guardHot = new Color(1f, 0.93f, 0.62f);
        public static bool GuardActive { get { return Time.time < GuardUntil; } }

        static void TauntingRoar()
        {
            var d = new ActionDef { id = "kn_5", dur = 1.55f, lockUntil = 1.25f, hitAt = 0.66f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            var brace = PlayerAnimator.Mk(n, p => { p.arL = V(-60f, -8f, -14f); p.fL = V(-95f, 0f, 0f); p.arR = V(-8f, 22f, 14f); p.fR = V(-30f, 0f, 0f); p.body = V(4f, -12f, 0f); p.head = V(10f, 0f, 0f); p.lgL = V(-22f, 0f, 0f); p.lgR = V(22f, 0f, 0f); p.lift = -0.12f; });
            var plant = PlayerAnimator.Mk(brace, p => { p.arL = V(-40f, -4f, -12f); p.fL = V(-40f, 0f, 0f); p.arR = V(4f, 26f, 14f); p.fR = V(-14f, 0f, 0f); p.body = V(10f, -8f, 0f); p.head = V(12f, 0f, 0f); p.lift = -0.2f; });
            var rise = PlayerAnimator.Mk(plant, p => { p.arL = V(-20f, -46f, -34f); p.fL = V(-40f, 0f, 0f); p.arR = V(-14f, 46f, 34f); p.fR = V(-10f, 0f, 0f); p.body = V(-8f, 0f, 0f); p.head = V(-6f, 0f, 0f); p.lift = -0.06f; });
            var roar = PlayerAnimator.Mk(rise, p => { p.arL = V(-36f, -58f, -40f); p.fL = V(-30f, 0f, 0f); p.arR = V(-30f, 58f, 40f); p.fR = V(-10f, 0f, 0f); p.body = V(-14f, 0f, 0f); p.head = V(-22f, 0f, 0f); p.lgL = V(-18f, 0f, 0f); p.lgR = V(18f, 0f, 0f); p.lift = -0.04f; });
            var fort = PlayerAnimator.Mk(roar, p => { p.arL = V(-70f, -6f, -14f); p.fL = V(-100f, 0f, 0f); p.arR = V(-10f, 20f, 12f); p.fR = V(-30f, 0f, 0f); p.body = V(14f, -8f, 0f); p.head = V(8f, 0f, 0f); p.lgL = V(-26f, 0f, 0f); p.lgR = V(22f, 0f, 0f); p.lift = -0.14f; });
            d.Key(0f, n).Key(0.18f, brace).Key(0.38f, plant).Key(0.58f, rise).Key(0.66f, roar).Key(0.90f, roar).Key(1.10f, roar).Key(1.35f, fort).Key(1.55f, n);

            d.At(0.18f, pc => { Audio.Sfx("block", pc.transform.position); });
            d.At(0.34f, pc =>
            {
                // Shield Plant: THUD, debu kecil dan lingkaran emas tipis di tanah
                Vector3 g = Feet(pc) + pc.transform.forward * 0.7f;
                CombatFx.Dust(g, 10);
                MagicFx.Crack(g, 0.5f, 2f);
                MagicFx.MagicCircle(Feet(pc) + Vector3.up * 0.05f, 2.8f, new Color(Gold.r, Gold.g, Gold.b, 0.7f), 1.2f);
                Audio.Sfx("heavy_swing", pc.transform.position);
            });
            d.At(0.62f, pc =>
            {
                // ROAR: 2-3 cincin emas membesar dari Knight (bukan dari shield) + camera shake kecil
                Audio.Sfx("heavy_swing", pc.transform.position);
                Vector3 f = Feet(pc);
                CombatFx.ShockRing(f, 9f, new Color(1f, 0.78f, 0.3f, 0.9f), 0.5f);
                CombatFx.Host.I.StartCoroutine(Late(0.1f, () => { if (pc != null) CombatFx.ShockRing(Feet(pc), 7f, new Color(1f, 0.9f, 0.55f, 0.9f), 0.45f); }));
                CombatFx.Host.I.StartCoroutine(Late(0.2f, () => { if (pc != null) CombatFx.ShockRing(Feet(pc), 5f, new Color(1f, 0.65f, 0.2f, 0.9f), 0.4f); }));
                CombatFx.Flash(Chest(pc), GoldHot, 12f, 3f, 0.4f);
                CombatFx.Burst(Chest(pc), Gold, 26, 2.2f, 2f, 0.9f, 0.12f);
                ThirdPersonCamera.Shake(0.16f, 0.3f);
            });
            d.At(0.90f, pc =>
            {
                // Taunt Pulse: musuh tersentak, energi tipis tertarik ke Knight (tanpa teleport), simbol aggro di atas kepala
                foreach (var m in Monster.All)
                {
                    if (m == null || m.IsDead) continue;
                    if (Vector3.Distance(m.transform.position, pc.transform.position) > 10f) continue;
                    m.Alert();
                    CombatFx.Beam(m.HitPoint, Chest(pc), new Color(1f, 0.75f, 0.3f, 0.5f), 0.08f, 0.45f);
                    CombatFx.Burst(m.HitPoint + Vector3.up * 1.0f, new Color(1f, 0.35f, 0.15f), 5, 0.25f, 0.4f, 0.5f, 0.14f);
                    CombatFx.Flash(m.HitPoint, Gold, 3f, 1f, 0.2f);
                }
            });
            d.At(1.10f, pc =>
            {
                // Fortress Brace -> buff 5 dtk (damage masuk -35%) dengan cincin tipis di kaki + rune di perisai
                GuardUntil = Time.time + 5f;
                guardMain = Gold; guardHot = GoldHot;
                CombatFx.Flash(Chest(pc), Gold, 6f, 1.6f, 0.25f);
                CombatFx.Host.I.StartCoroutine(GuardPassive(pc, 5f));
            });
            Add(d);
        }

        static IEnumerator GuardPassive(PlayerCombat pc, float secs)
        {
            float t = 0f;
            while (t < secs)
            {
                if (pc == null || pc.IsDead) yield break;
                float k = t < secs - 1.5f ? 1f : Mathf.Lerp(1f, 0.2f, (t - (secs - 1.5f)) / 1.5f);      // rune meredup di akhir
                MagicFx.MagicCircle(Feet(pc) + Vector3.up * 0.05f, 2.0f, new Color(Gold.r, Gold.g, Gold.b, 0.55f * k), 0.6f);
                CombatFx.Flash(Tip(pc) - Vector3.up * 0.3f, Gold, 2.5f * k, 0.5f * k, 0.5f);
                CombatFx.Burst(Chest(pc), Gold, Mathf.Max(1, Mathf.RoundToInt(2 * k)), 0.5f, 0.3f, 0.7f, 0.05f);
                yield return new WaitForSeconds(0.5f);
                t += 0.5f;
            }
            GuardUntil = 0f;
            if (pc != null) CombatFx.Burst(Feet(pc), Gold, 10, 1.0f, 0.8f, 0.8f, 0.06f);
        }

        /// <summary>Flash emas singkat di tubuh saat damage sedang dikurangi buff Knight.</summary>
        public static void GuardFlash(PlayerCombat pc, Vector3 from)
        {
            Vector3 dir = from - pc.transform.position; dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = pc.transform.forward;
            Vector3 pt = Chest(pc) + dir.normalized * 0.6f;
            CombatFx.Flash(pt, guardHot, 5f, 1.4f, 0.12f);
            CombatFx.HitSpark(pt, guardMain, 0.9f, 8);
        }

        // =====================================================================
        // Knight — ULTIMATE Fortress  [kn_6]  (buff 10 dtk: damage masuk -70%, damage +20%)
        // Lock 0-0.20 | Cross Guard 0.20-0.45 | Compression 0.45-0.70 | RELEASE 0.70-0.95 | Armor Lock 0.95-1.25 | Weapon 1.25-1.45 | Fortress Step 1.45-1.65 | Stance -1.85
        // =====================================================================
        static readonly Color FortSword = new Color(1f, 0.38f, 0.14f);
        public static float FortressUntil;
        public static bool FortressActive { get { return Time.time < FortressUntil; } }

        static void Fortress()
        {
            var d = new ActionDef { id = "kn_6", dur = 1.85f, lockUntil = 1.55f, hitAt = 0.95f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            var lockp = PlayerAnimator.Mk(n, p => { p.arL = V(-52f, -6f, -12f); p.fL = V(-100f, 0f, 0f); p.arR = V(-14f, 20f, 10f); p.fR = V(-70f, 0f, 0f); p.body = V(4f, 0f, 0f); p.head = V(10f, 0f, 0f); p.lgL = V(-8f, 0f, 0f); p.lgR = V(8f, 0f, 0f); p.lift = -0.04f; });
            var cross = PlayerAnimator.Mk(lockp, p => { p.arL = V(-74f, -2f, -10f); p.fL = V(-125f, 0f, 0f); p.arR = V(-68f, -36f, 6f); p.fR = V(-118f, 0f, 0f); p.body = V(8f, 0f, 0f); p.head = V(12f, 0f, 0f); p.lift = -0.06f; });
            var comp = PlayerAnimator.Mk(cross, p => { p.arL = V(-66f, -2f, -8f); p.fL = V(-140f, 0f, 0f); p.arR = V(-60f, -30f, 4f); p.fR = V(-132f, 0f, 0f); p.body = V(14f, 0f, 0f); p.head = V(20f, 0f, 0f); p.lgL = V(-18f, 0f, 0f); p.lgR = V(18f, 0f, 0f); p.lift = -0.12f; });
            var release = PlayerAnimator.Mk(comp, p => { p.arL = V(-30f, -62f, -44f); p.fL = V(-40f, 0f, 0f); p.arR = V(-30f, 62f, 44f); p.fR = V(-30f, 0f, 0f); p.body = V(-8f, 0f, 0f); p.head = V(-8f, 0f, 0f); p.lgL = V(-10f, 0f, 0f); p.lgR = V(10f, 0f, 0f); p.lift = -0.05f; });
            var lockd = PlayerAnimator.Mk(release, p => { p.arL = V(-40f, -50f, -34f); p.arR = V(-40f, 50f, 34f); p.body = V(0f, 0f, 0f); p.head = V(0f, 0f, 0f); p.lift = -0.02f; });
            var weapon = PlayerAnimator.Mk(lockd, p => { p.arL = V(-60f, -8f, -12f); p.fL = V(-90f, 0f, 0f); p.arR = V(-56f, 22f, 8f); p.fR = V(-40f, 0f, 0f); p.body = V(2f, 0f, 0f); p.lift = -0.04f; });
            var stp = PlayerAnimator.Mk(weapon, p => { p.arL = V(-80f, -6f, -10f); p.fL = V(-100f, 0f, 0f); p.arR = V(-30f, 24f, 12f); p.fR = V(-40f, 0f, 0f); p.body = V(8f, 0f, 0f); p.lunge = 0.22f; p.lgL = V(-34f, 0f, 0f); p.lgR = V(18f, 0f, 0f); p.lift = -0.1f; });
            var stance = PlayerAnimator.Mk(stp, p => { p.arL = V(-74f, -6f, -12f); p.fL = V(-96f, 0f, 0f); p.arR = V(-48f, 18f, 10f); p.fR = V(-50f, 0f, 0f); p.body = V(4f, -4f, 0f); p.lunge = 0.12f; p.lift = -0.06f; });
            d.Key(0f, n).Key(0.20f, lockp).Key(0.45f, cross).Key(0.70f, comp).Key(0.95f, release).Key(1.25f, lockd).Key(1.45f, weapon).Key(1.65f, stp).Key(1.85f, stance);

            d.At(0f, pc =>
            {
                CombatFx.Burst(Chest(pc), new Color(1f, 0.55f, 0.25f), 6, 0.45f, 0.6f, 0.7f, 0.05f);     // cahaya tipis dari celah armor
                CombatFx.Flash(Chest(pc), new Color(1f, 0.55f, 0.25f), 3f, 0.8f, 0.5f);
            });
            d.At(0.45f, pc =>
            {
                CombatFx.Host.I.StartCoroutine(ConvergeBody(pc, Gold, -1f, 0.25f));         // energi emas dari perisai
                CombatFx.Host.I.StartCoroutine(ConvergeBody(pc, FortSword, 1f, 0.25f));     // energi merah-oranye dari pedang
            });
            d.At(0.70f, pc =>
            {
                // RELEASE: tangan membuka eksplosif, WHOOOM dari dalam armor (tanpa shockwave besar, tanpa roar)
                Audio.Sfx("heavy_swing", pc.transform.position);
                CombatFx.Flash(Chest(pc), GoldHot, 10f, 3.5f, 0.4f);
                CombatFx.Burst(Chest(pc), Gold, 18, 0.9f, 1.4f, 0.8f, 0.09f);
                CombatFx.ShockRing(Feet(pc), 2.6f, new Color(1f, 0.7f, 0.3f, 0.5f), 0.3f);
            });
            d.At(0.95f, pc =>
            {
                FortressUntil = GuardUntil = Time.time + 10f;
                guardMain = Gold; guardHot = GoldHot;
                CombatFx.Host.I.StartCoroutine(FortressPassive(pc));
                // Armor Lock: kaki -> pinggang -> dada -> bahu -> lengan -> perisai -> pedang, flash CLICK satu per satu
                float[] hs = { 0.1f, 0.65f, 1.15f, 1.5f };
                for (int i = 0; i < hs.Length; i++)
                {
                    float h = hs[i];
                    CombatFx.Host.I.StartCoroutine(Late(0.06f * i, () =>
                    {
                        if (pc == null) return;
                        Vector3 b = pc.transform.position - Vector3.up * 0.9f + Vector3.up * h;
                        CombatFx.Flash(b, GoldHot, 3.5f, 2f, 0.12f);
                        CombatFx.HitSpark(b + pc.transform.forward * 0.3f, Gold, 0.6f, 6);
                        HexFlash(b + pc.transform.forward * 0.35f, pc.transform.forward, 0.6f, 0.25f);
                        Audio.Sfx("block", pc.transform.position);
                    }));
                }
                CombatFx.Host.I.StartCoroutine(Late(0.26f, () => { if (pc != null) CombatFx.Flash(Chest(pc) - pc.transform.right * 0.5f, Gold, 3.5f, 2f, 0.14f); }));
                CombatFx.Host.I.StartCoroutine(Late(0.30f, () => { if (pc != null) CombatFx.Flash(Tip(pc), FortSword, 4f, 2f, 0.16f); }));
            });
            d.At(1.25f, pc =>
            {
                // Weapon Empowerment: energi merah-oranye mengalir gagang -> ujung; perisai bersinar emas
                Vector3 tip = Tip(pc), grip = Chest(pc) + pc.transform.right * 0.3f;
                CombatFx.Beam(grip, tip, new Color(1f, 0.45f, 0.18f, 0.9f), 0.12f, 0.3f);
                CombatFx.Flash(tip, FortSword, 6f, 2.2f, 0.35f);
                CombatFx.Flash(Chest(pc) - pc.transform.right * 0.5f, Gold, 4f, 1.4f, 0.4f);
            });
            d.At(1.45f, pc =>
            {
                // THE FORTRESS STEP: satu langkah tenang, THUMP + shake kecil
                pc.DashTo(pc.transform.forward, 0.14f, 4f);
            });
            d.At(1.58f, pc =>
            {
                Audio.Sfx("block", pc.transform.position);
                CombatFx.Dust(Feet(pc) + pc.transform.forward * 0.4f, 8);
                CombatFx.ShockRing(Feet(pc), 2.6f, new Color(1f, 0.7f, 0.3f, 0.6f), 0.3f);
                CombatFx.Flash(Feet(pc) + Vector3.up * 0.2f, Gold, 3f, 1.2f, 0.2f);
                ThirdPersonCamera.Shake(0.12f, 0.2f);
            });
            Add(d);
        }

        /// <summary>Partikel energi mengalir menuju badan; side -1 = dari perisai (kiri), +1 = dari pedang (kanan).</summary>
        static IEnumerator ConvergeBody(PlayerCombat pc, Color col, float side, float secs)
        {
            float t = 0f;
            while (t < secs && pc != null)
            {
                Vector3 src = Chest(pc) + pc.transform.right * side * 0.9f + pc.transform.forward * 0.4f + Vector3.up * Random.Range(-0.2f, 0.3f);
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                UnityEngine.Object.Destroy(q.GetComponent<Collider>());
                q.name = "FxConvergeBody";
                q.transform.position = src;
                q.GetComponent<MeshRenderer>().sharedMaterial = CombatFx.Mat(CombatFx.Soft(), col);
                var fx = q.AddComponent<CombatFx.FxAnim>();
                fx.life = 0.25f; fx.billboard = true; fx.color = col;
                fx.s0 = Vector3.one * 0.3f; fx.s1 = Vector3.one * 0.05f;
                fx.vel = (Chest(pc) - src) / 0.25f;
                yield return new WaitForSeconds(0.04f);
                t += 0.04f;
            }
        }

        /// <summary>10 detik: armor berpendar tipis, glow tiap langkah, melemah dan berkedip di detik 8-10, lalu pecah jadi partikel.</summary>
        static IEnumerator FortressPassive(PlayerCombat pc)
        {
            float t = 0f; Vector3 last = pc.transform.position; float stepAcc = 0f;
            while (t < 10f)
            {
                if (pc == null || pc.IsDead) { FortressUntil = GuardUntil = 0f; yield break; }
                float k = t < 8f ? 1f : Mathf.Lerp(1f, 0.25f, (t - 8f) / 2f);
                bool blink = t > 8f && Mathf.Repeat(t, 0.4f) < 0.2f;
                Vector3 pos = pc.transform.position;
                stepAcc += Vector3.Distance(new Vector3(pos.x, 0f, pos.z), new Vector3(last.x, 0f, last.z));
                last = pos;
                if (stepAcc > 1.6f) { stepAcc = 0f; CombatFx.Flash(Feet(pc) + Vector3.up * 0.2f, Gold, 2.5f * k, 0.9f * k, 0.15f); CombatFx.Dust(Feet(pc), 2); }
                if (!blink)
                {
                    CombatFx.Flash(Chest(pc) + Vector3.up * 0.4f, Gold, 3f * k, 0.5f * k, 0.3f);
                    CombatFx.Flash(Tip(pc), FortSword, 2.5f * k, 0.6f * k, 0.3f);
                    CombatFx.Burst(Chest(pc), Gold, Mathf.Max(1, Mathf.RoundToInt(2 * k)), 0.5f, 0.2f, 0.6f, 0.05f);
                }
                yield return new WaitForSeconds(0.25f);
                t += 0.25f;
            }
            FortressUntil = GuardUntil = 0f;
            if (pc != null)
            {
                CombatFx.Burst(Chest(pc), Gold, 26, 0.9f, 1.8f, 1f, 0.08f);       // armor energy pecah jadi partikel
                CombatFx.Burst(Chest(pc), FortSword, 12, 0.9f, 1.4f, 0.8f, 0.07f);
                CombatFx.ShockRing(Feet(pc), 2.6f, new Color(1f, 0.7f, 0.3f, 0.5f), 0.4f);
            }
        }

        /// <summary>Tiap tebasan selama Fortress: trail emas-merah tipis dan spark saat mengenai musuh.</summary>
        public static void FortressSlash(PlayerCombat pc)
        {
            CombatFx.SweepArc(Chest(pc), pc.transform.rotation * Quaternion.Euler(0f, 0f, 20f), 2.9f, 28f, 0.15f, 70f, -70f, 0.12f, 0.2f, new Color(1f, 0.6f, 0.22f, 0.8f), false);
            CombatFx.Host.I.StartCoroutine(Late(0.12f, () =>
            {
                if (pc == null) return;
                foreach (var m in InFront(pc, 3.4f, 70f)) { CombatFx.HitSpark(m.HitPoint, new Color(1f, 0.55f, 0.2f), 1.1f, 12); CombatFx.Flash(m.HitPoint, Gold, 3f, 1f, 0.1f); }
            }));
        }

        // =====================================================================
        // Berserker — Savage Cleave  [bz_4]
        // Feral Stance 0-0.15 | Wind-Up 0.15-0.35 | Feral Step 0.35-0.48 | SAVAGE CLEAVE 0.48-0.75 | Impact 0.75-0.88 | Blood Reclaim 0.88-1.05 | Over-Swing 1.05-1.20 | Recovery 1.20-1.50
        // =====================================================================
        static readonly Color Crimson = new Color(0.85f, 0.06f, 0.1f), CrimsonHot = new Color(1f, 0.3f, 0.2f);
        static readonly System.Collections.Generic.List<Vector3> savageHits = new System.Collections.Generic.List<Vector3>();

        static void SavageCleave()
        {
            var d = new ActionDef { id = "bz_4", dur = 1.5f, lockUntil = 1.3f, hitAt = 0.76f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            var feral = PlayerAnimator.Mk(n, p => { p.arR = V(-8f, 44f, 12f); p.fR = V(-30f, 0f, 0f); p.arL = V(-10f, 30f, -10f); p.fL = V(-44f, 0f, 0f); p.body = V(22f, -16f, 0f); p.head = V(16f, 0f, 0f); p.lgL = V(-30f, 0f, 0f); p.lgR = V(30f, 0f, 0f); p.lift = -0.22f; });
            var wind = PlayerAnimator.Mk(feral, p => { p.arR = V(24f, 82f, 14f); p.fR = V(-16f, 0f, 0f); p.arL = V(10f, 66f, -10f); p.fL = V(-24f, 0f, 0f); p.body = V(14f, -62f, 0f); p.head = V(10f, -22f, 0f); p.lgL = V(-16f, 0f, 0f); p.lgR = V(34f, 0f, 0f); p.lift = -0.26f; p.lunge = -0.12f; });
            var step = PlayerAnimator.Mk(wind, p => { p.lunge = 0.34f; p.body = V(26f, -56f, 0f); p.lgL = V(-48f, 0f, 0f); p.lgR = V(26f, 0f, 0f); p.lift = -0.2f; });
            var cleave = PlayerAnimator.Mk(step, p => { p.arR = V(-62f, -70f, 8f); p.fR = V(-6f, 0f, 0f); p.arL = V(-52f, -50f, -8f); p.fL = V(-10f, 0f, 0f); p.body = V(30f, 70f, 0f); p.head = V(12f, 36f, 0f); p.lunge = 0.46f; p.lift = -0.24f; p.lgL = V(-54f, 0f, 0f); p.lgR = V(48f, 0f, 0f); });
            var over = PlayerAnimator.Mk(cleave, p => { p.arR = V(-92f, -92f, 8f); p.arL = V(-80f, -70f, -8f); p.body = V(26f, 96f, 0f); p.head = V(8f, 50f, 0f); p.lunge = 0.52f; p.lift = -0.18f; p.lgL = V(-40f, 0f, 0f); p.lgR = V(-4f, 0f, 0f); });
            var rec = PlayerAnimator.Mk(over, p => { p.arR = V(-10f, 40f, 12f); p.fR = V(-34f, 0f, 0f); p.arL = V(-10f, 28f, -10f); p.fL = V(-46f, 0f, 0f); p.body = V(18f, -10f, 0f); p.head = V(10f, 0f, 0f); p.lunge = 0.12f; p.lgL = V(-24f, 0f, 0f); p.lgR = V(28f, 0f, 0f); p.lift = -0.18f; });
            d.Key(0f, n).Key(0.15f, feral).Key(0.35f, wind).Key(0.48f, step).Key(0.75f, cleave).Key(0.88f, cleave).Key(1.05f, over).Key(1.20f, over).Key(1.50f, rec);

            d.At(0f, pc => { Aura(pc, Crimson, 0.15f, 0.6f); });
            d.At(0.15f, pc =>
            {
                Converge(pc, CrimsonHot, 0.2f, 1.2f);
                CombatFx.Dust(Feet(pc), 6);
                CombatFx.Flash(Tip(pc), Crimson, 4f, 1.2f, 0.3f);
            });
            d.At(0.35f, pc =>
            {
                pc.DashTo(pc.transform.forward, 0.13f, 10f);
                CombatFx.Dust(Feet(pc) - pc.transform.forward * 0.4f, 16);
                CombatFx.Beam(Chest(pc), Chest(pc) + pc.transform.forward * 2.5f, new Color(1f, 0.25f, 0.15f, 0.5f), 0.2f, 0.2f);
            });
            d.At(0.48f, pc =>
            {
                Audio.Sfx("heavy_swing", pc.transform.position);
                // lintasan diagonal naik: bawah-samping -> depan -> atas-samping, arc bergerigi (tiga arc tidak sejajar)
                Quaternion b0 = pc.transform.rotation * Quaternion.Euler(0f, 0f, -26f);
                Vector3 c = Chest(pc);
                CombatFx.SweepArc(c, b0, 4.0f, 62f, 0.5f, -120f, 60f, 0.26f, 0.35f, new Color(0.8f, 0.05f, 0.08f, 0.95f), true);
                CombatFx.SweepArc(c, pc.transform.rotation * Quaternion.Euler(0f, 0f, -14f), 3.7f, 34f, 0.28f, -118f, 64f, 0.24f, 0.3f, new Color(1f, 0.35f, 0.25f, 0.9f), false);
                CombatFx.SweepArc(c, pc.transform.rotation * Quaternion.Euler(0f, 0f, -38f), 4.3f, 24f, 0.2f, -124f, 56f, 0.28f, 0.28f, new Color(0.6f, 0.02f, 0.05f, 0.85f), false);
            });
            d.At(0.76f, pc =>
            {
                Vector3 f = pc.transform.forward;
                savageHits.Clear();
                foreach (var m in InFront(pc, 4.2f, 70f)) savageHits.Add(m.HitPoint);
                HitStop(0.08f);
                Vector3 g; GroundFront(pc, 2.4f, out g);
                CombatFx.Flash(Chest(pc) + f * 2f, Crimson, 10f, 3f, 0.2f);
                foreach (var hp in savageHits) { CombatFx.HitSpark(hp, new Color(1f, 0.25f, 0.2f), 1.8f, 28); CombatFx.Burst(hp, Crimson, 14, 0.8f, 1.6f, 0.6f, 0.1f); }
                CombatFx.Dust(g, 14);
                MagicFx.Debri(g, 2.0f, 8, 0.9f);
                ThirdPersonCamera.Shake(0.2f, 0.22f);
            });
            d.At(0.88f, pc =>
            {
                // Blood Reclaim: serpihan energi merah tertarik dari musuh menuju Berserker, lalu flash merah di dada
                foreach (var hp in savageHits)
                    CombatFx.Host.I.StartCoroutine(CrimsonFragments(pc, hp));
                CombatFx.Host.I.StartCoroutine(Late(0.2f, () =>
                {
                    if (pc == null) return;
                    CombatFx.Flash(Chest(pc), CrimsonHot, 6f, 2f, 0.25f);
                    CombatFx.Burst(Chest(pc), Crimson, 8, 0.5f, 0.8f, 0.5f, 0.07f);
                }));
            });
            d.At(1.05f, pc => { CombatFx.Dust(Feet(pc), 8); });
            Add(d);
        }

        static IEnumerator CrimsonFragments(PlayerCombat pc, Vector3 from)
        {
            for (int i = 0; i < 6; i++)
            {
                if (pc == null) yield break;
                Vector3 src = from + Random.insideUnitSphere * 0.5f;
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                UnityEngine.Object.Destroy(q.GetComponent<Collider>());
                q.name = "FxFragment";
                q.transform.position = src;
                q.GetComponent<MeshRenderer>().sharedMaterial = CombatFx.Mat(CombatFx.Soft(), Crimson);
                var fx = q.AddComponent<CombatFx.FxAnim>();
                fx.life = 0.3f; fx.billboard = true; fx.color = CrimsonHot;
                fx.s0 = Vector3.one * 0.22f; fx.s1 = Vector3.one * 0.06f;
                fx.vel = (Chest(pc) - src) / 0.3f;
                yield return new WaitForSeconds(0.035f);
            }
        }

        // =====================================================================
        // Berserker — Rage Leap  [bz_5]   (tubuh sebagai proyektil, bukan pedang)
        // Compression 0-0.18 | Launch 0.18-0.35 | Low Leap 0.35-0.55 | Charge Pose 0.55-0.80 | BODY CRASH 0.80-1.15 | Landing 1.15-1.35 | Drag -1.50 | Rise -1.75 | Recovery -1.95
        // =====================================================================
        static void RageLeap()
        {
            var d = new ActionDef { id = "bz_5", dur = 1.95f, lockUntil = 1.6f, hitAt = 0.8f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            var comp = PlayerAnimator.Mk(n, p => { p.arR = V(24f, 54f, 12f); p.fR = V(-14f, 0f, 0f); p.arL = V(-30f, 12f, -10f); p.fL = V(-30f, 0f, 0f); p.body = V(50f, -10f, 0f); p.head = V(26f, 0f, 0f); p.lgL = V(-46f, 0f, 0f); p.lgR = V(38f, 0f, 0f); p.lift = -0.32f; });
            var launch = PlayerAnimator.Mk(comp, p => { p.body = V(60f, -8f, 0f); p.lunge = 0.42f; p.lgL = V(-54f, 0f, 0f); p.lgR = V(42f, 0f, 0f); p.lift = -0.26f; });
            var leap = PlayerAnimator.Mk(launch, p => { p.body = V(62f, -14f, 0f); p.lunge = 0.52f; p.lift = 0.3f; p.lgL = V(-62f, 0f, 0f); p.lgR = V(40f, 0f, 0f); });
            var charge = PlayerAnimator.Mk(leap, p => { p.arR = V(30f, 60f, 12f); p.arL = V(-50f, -30f, -18f); p.body = V(58f, -34f, 0f); p.head = V(30f, -10f, 0f); p.lgL = V(-72f, 0f, 0f); p.lgR = V(30f, 0f, 0f); p.lunge = 0.58f; p.lift = 0.38f; });
            var crashA = PlayerAnimator.Mk(charge, p => { p.body = V(58f, -48f, 14f); p.head = V(28f, -22f, 0f); p.lift = 0.3f; });
            var crashB = PlayerAnimator.Mk(charge, p => { p.body = V(58f, -22f, -14f); p.head = V(28f, 0f, 0f); p.lift = 0.22f; });
            // Rage Landing: tiga titik (kaki depan + tangan kosong menyentuh tanah), pedang di belakang
            var land = PlayerAnimator.Mk(crashB, p => { p.arL = V(-100f, -8f, -10f); p.fL = V(-6f, 0f, 0f); p.arR = V(34f, 60f, 12f); p.body = V(70f, -20f, 0f); p.head = V(34f, 0f, 0f); p.lgL = V(-88f, 0f, 0f); p.lgR = V(50f, 0f, 0f); p.lift = -0.36f; p.lunge = 0.5f; });
            var drag = PlayerAnimator.Mk(land, p => { p.lunge = 0.6f; p.body = V(66f, -16f, 0f); });
            var rise = PlayerAnimator.Mk(drag, p => { p.arL = V(-30f, 10f, -10f); p.fL = V(-40f, 0f, 0f); p.arR = V(6f, 44f, 12f); p.body = V(34f, -8f, 0f); p.head = V(14f, 0f, 0f); p.lgL = V(-36f, 0f, 0f); p.lgR = V(32f, 0f, 0f); p.lift = -0.22f; p.lunge = 0.24f; });
            d.Key(0f, n).Key(0.18f, comp).Key(0.35f, launch).Key(0.55f, leap).Key(0.80f, charge).Key(0.93f, crashA).Key(1.04f, crashB).Key(1.15f, crashA).Key(1.35f, land).Key(1.50f, drag).Key(1.75f, rise).Key(1.95f, n);

            d.At(0f, pc => { Aura(pc, Crimson, 0.18f, 1.0f); CombatFx.Flash(Chest(pc) - Vector3.up * 0.4f, Crimson, 3.5f, 1.2f, 0.3f); });
            d.At(0.18f, pc =>
            {
                Audio.Sfx("heavy_swing", pc.transform.position);
                pc.DashTo(pc.transform.forward, 0.17f, 6f);
                CombatFx.Dust(Feet(pc), 24);
                CombatFx.ShockRing(Feet(pc), 3f, new Color(1f, 0.3f, 0.15f, 0.7f), 0.3f);
                MagicFx.Debri(Feet(pc), 1.6f, 8, 0.8f);
            });
            d.At(0.35f, pc =>
            {
                pc.Invuln(1.0f);
                pc.DashTo(pc.transform.forward, 0.45f, 10f);
                CombatFx.Dust(Feet(pc), 12);
                CombatFx.Host.I.StartCoroutine(CrimsonTrail(pc, 0.8f));
            });
            d.At(0.55f, pc => { CombatFx.Beam(Chest(pc), Chest(pc) + pc.transform.forward * 4f, new Color(1f, 0.3f, 0.2f, 0.4f), 0.2f, 0.3f); });
            d.At(0.80f, pc =>
            {
                pc.DashTo(pc.transform.forward, 0.35f, 8f);                  // melewati musuh sambil menabrak
                CombatFx.Flash(Chest(pc), CrimsonHot, 6f, 2f, 0.2f);
            });
            d.At(1.15f, pc =>
            {
                Vector3 f = pc.transform.forward;
                Audio.Sfx("block", pc.transform.position);
                CombatFx.Dust(Feet(pc) + f * 0.5f, 22);
                CombatFx.Burst(Feet(pc) + f * 0.4f, new Color(0.7f, 0.6f, 0.5f), 10, 0.8f, 1.2f, 0.7f, 0.12f);
                MagicFx.Debri(Feet(pc) + f * 0.4f, 1.6f, 8, 0.7f);
                ThirdPersonCamera.Shake(0.16f, 0.2f);
            });
            d.At(1.35f, pc =>
            {
                CombatFx.Beam(Feet(pc) - pc.transform.forward * 1.2f + Vector3.up * 0.1f, Feet(pc) + Vector3.up * 0.1f, new Color(0.75f, 0.65f, 0.5f, 0.6f), 0.25f, 0.3f);    // SKRRRT
                CombatFx.Dust(Feet(pc), 12);
            });
            Add(d);
        }

        /// <summary>Jejak merah di belakang tubuh Berserker selama terkam + 1-2 afterimage kasar.</summary>
        static IEnumerator CrimsonTrail(PlayerCombat pc, float secs)
        {
            float t = 0f;
            while (t < secs && pc != null)
            {
                CombatFx.Burst(Chest(pc) - pc.transform.forward * 0.4f, Crimson, 3, 0.3f, 0.2f, 0.5f, 0.2f);
                if (Mathf.Repeat(t, 0.16f) < 0.04f) CombatFx.Host.I.StartCoroutine(Afterimages(pc, 0.04f));
                yield return new WaitForSeconds(0.04f);
                t += 0.04f;
            }
        }

        /// <summary>Benturan bahu pada musuh yang dilewati: flash merah, spark, debris, indikator stagger.</summary>
        public static void CrashFx(PlayerCombat pc, Vector3 pt)
        {
            Audio.Sfx("block", pt);
            CombatFx.Flash(pt, CrimsonHot, 7f, 2.4f, 0.14f);
            CombatFx.HitSpark(pt, new Color(1f, 0.35f, 0.2f), 1.5f, 22);
            CombatFx.Burst(pt, Crimson, 10, 0.7f, 1.2f, 0.5f, 0.1f);
            CombatFx.Burst(pt + Vector3.up * 1.0f, new Color(1f, 0.9f, 0.4f), 4, 0.25f, 0.3f, 0.6f, 0.1f);
            ThirdPersonCamera.Shake(0.1f, 0.12f);
        }

        // =====================================================================
        // Berserker — ULTIMATE Bloodbath  [bz_6]  (mode 8 dtk: damage +30%, lifesteal 25%)
        // Feral Stop 0-0.15 | Trigger 0.15-0.38 | Rage Break 0.38-0.55 | FIRST SLASH 0.55-0.72 | Reverse 0.72-0.88 | SPIN 0.88-1.15 | FINAL 1.15-1.38 | Reclaim 1.38-1.60 | MODE 1.60
        // =====================================================================
        public static float BloodbathUntil;
        public static bool BloodbathActive { get { return Time.time < BloodbathUntil; } }
        static float bloodPulseAt;
        static readonly System.Collections.Generic.List<Vector3> bloodTargets = new System.Collections.Generic.List<Vector3>();

        static void Bloodbath()
        {
            var d = new ActionDef { id = "bz_6", dur = 1.75f, lockUntil = 1.4f, hitAt = 0.6f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            var stop = PlayerAnimator.Mk(n, p => { p.arR = V(8f, 34f, 12f); p.fR = V(-30f, 0f, 0f); p.arL = V(-12f, 20f, -10f); p.fL = V(-50f, 0f, 0f); p.body = V(22f, 0f, 0f); p.head = V(22f, 0f, 0f); p.lgL = V(-30f, 0f, 0f); p.lgR = V(30f, 0f, 0f); p.lift = -0.2f; });
            var trig = PlayerAnimator.Mk(stop, p => { p.arR = V(-30f, 4f, 8f); p.fR = V(-62f, 0f, 0f); p.arL = V(-40f, -6f, -6f); p.fL = V(-130f, 0f, 0f); p.body = V(26f, 0f, 0f); p.head = V(24f, 0f, 0f); p.lift = -0.22f; });
            var brk = PlayerAnimator.Mk(trig, p => { p.arR = V(-20f, 46f, 14f); p.fR = V(-18f, 0f, 0f); p.arL = V(14f, 52f, -12f); p.fL = V(-30f, 0f, 0f); p.body = V(-10f, 0f, 0f); p.head = V(-18f, 0f, 0f); p.lift = -0.12f; });
            var s1 = PlayerAnimator.Mk(brk, p => { p.arR = V(-108f, -68f, 8f); p.fR = V(-6f, 0f, 0f); p.arL = V(-90f, -50f, -8f); p.body = V(24f, 74f, 0f); p.head = V(8f, 40f, 0f); p.lunge = 0.3f; p.lift = -0.18f; p.lgL = V(-40f, 0f, 0f); p.lgR = V(30f, 0f, 0f); });
            var s2 = PlayerAnimator.Mk(s1, p => { p.arR = V(-30f, 70f, 8f); p.arL = V(-24f, 52f, -8f); p.body = V(30f, -70f, 0f); p.head = V(10f, -36f, 0f); p.lgL = V(-30f, 0f, 0f); p.lgR = V(40f, 0f, 0f); p.lift = -0.22f; });
            var spin = PlayerAnimator.Mk(s2, p => { p.arR = V(-72f, -90f, 8f); p.arL = V(-62f, -70f, -8f); p.body = V(34f, 150f, 0f); p.head = V(10f, 60f, 0f); p.lift = -0.2f; p.lgL = V(-44f, 0f, 0f); p.lgR = V(36f, 0f, 0f); });
            var fin = PlayerAnimator.Mk(spin, p => { p.arR = V(-150f, 52f, 8f); p.arL = V(-130f, 40f, -8f); p.body = V(20f, 34f, 0f); p.head = V(0f, 20f, 0f); p.lunge = 0.4f; p.lift = -0.16f; p.lgL = V(-52f, 0f, 0f); p.lgR = V(30f, 0f, 0f); });
            var mode = PlayerAnimator.Mk(fin, p => { p.arR = V(6f, 36f, 14f); p.fR = V(-24f, 0f, 0f); p.arL = V(-18f, 40f, -22f); p.fL = V(-24f, 0f, 0f); p.body = V(28f, -6f, 0f); p.head = V(20f, 8f, 0f); p.lunge = 0.1f; p.lgL = V(-40f, 0f, 0f); p.lgR = V(38f, 0f, 0f); p.lift = -0.22f; });
            d.Key(0f, n).Key(0.15f, stop).Key(0.38f, trig).Key(0.55f, brk).Key(0.72f, s1).Key(0.88f, s2).Key(1.15f, spin).Key(1.38f, fin).Key(1.60f, mode).Key(1.75f, mode);

            d.At(0f, pc => { Aura(pc, Crimson, 0.15f, 0.6f); });
            d.At(0.15f, pc =>
            {
                Audio.Sfx("skill_cast", pc.transform.position);
                Converge(pc, CrimsonHot, 0.23f, 1.2f);
                CombatFx.Burst(Chest(pc), Crimson, 6, 0.4f, 0.5f, 0.6f, 0.07f);
            });
            d.At(0.38f, pc =>
            {
                Audio.Sfx("heavy_swing", pc.transform.position);
                CombatFx.Flash(Chest(pc), CrimsonHot, 8f, 3f, 0.3f);
                CombatFx.Burst(Chest(pc), Crimson, 22, 1.0f, 1.6f, 0.7f, 0.12f);          // ledakan merah dekat tubuh, bukan shockwave radial
                ThirdPersonCamera.Shake(0.1f, 0.15f);
            });
            // rentetan tebasan 360 (damage tick di 0.6, 0.79, 0.98, 1.17 oleh nova)
            d.At(0.57f, pc => BloodSlashArc(pc, -26f, -120f, 60f, 4.0f));
            d.At(0.76f, pc => { BloodSlashArc(pc, 26f, 120f, -60f, 4.0f); CombatFx.Dust(Feet(pc), 6); });
            d.At(0.92f, pc =>
            {
                // spin 360: lingkaran crimson kasar
                Audio.Sfx("heavy_swing", pc.transform.position);
                CombatFx.SweepArc(Chest(pc), pc.transform.rotation, 4.2f, 34f, 0.4f, 0f, 360f, 0.26f, 0.3f, new Color(0.85f, 0.05f, 0.08f, 0.95f), true);
                CombatFx.SweepArc(Chest(pc) + Vector3.up * 0.15f, pc.transform.rotation * Quaternion.Euler(0f, 0f, 8f), 3.8f, 20f, 0.22f, 20f, 380f, 0.26f, 0.26f, new Color(1f, 0.35f, 0.25f, 0.85f), false);
                CombatFx.ShockRing(Feet(pc), 5f, new Color(0.9f, 0.1f, 0.12f, 0.7f), 0.35f);
            });
            d.At(1.15f, pc =>
            {
                bloodTargets.Clear();
                foreach (var m in Monster.All)
                    if (m != null && !m.IsDead && Vector3.Distance(m.transform.position, pc.transform.position) < 7.5f) bloodTargets.Add(m.HitPoint);
                Audio.Sfx("heavy_swing", pc.transform.position);
                BloodSlashArc(pc, -30f, -110f, 90f, 4.6f);
                HitStop(0.07f);
                CombatFx.Flash(Chest(pc) + pc.transform.forward * 2f, CrimsonHot, 12f, 4f, 0.2f);
                CombatFx.Dust(Feet(pc), 14);
                ThirdPersonCamera.Shake(0.22f, 0.25f);
            });
            d.At(1.38f, pc =>
            {
                // Blood Reclaim: serpihan crimson dari semua musuh yang terkena masuk ke dada
                foreach (var hp in bloodTargets) CombatFx.Host.I.StartCoroutine(CrimsonFragments(pc, hp));
                CombatFx.Host.I.StartCoroutine(Late(0.25f, () => { if (pc != null) { CombatFx.Flash(Chest(pc), CrimsonHot, 7f, 2.4f, 0.3f); CombatFx.Burst(Chest(pc), Crimson, 12, 0.6f, 0.8f, 0.6f, 0.08f); } }));
            });
            d.At(1.60f, pc =>
            {
                BloodbathUntil = Time.time + 8f;
                CombatFx.Host.I.StartCoroutine(BloodbathPassive(pc));
            });
            Add(d);
        }

        static void BloodSlashArc(PlayerCombat pc, float roll, float y0, float y1, float radius)
        {
            Audio.Sfx(Random.value < 0.5f ? "swing" : "swing2", pc.transform.position);
            Vector3 c = Chest(pc);
            CombatFx.SweepArc(c, pc.transform.rotation * Quaternion.Euler(0f, 0f, roll), radius, 54f, 0.4f, y0, y1, 0.18f, 0.28f, new Color(0.85f, 0.05f, 0.08f, 0.95f), true);
            CombatFx.SweepArc(c, pc.transform.rotation * Quaternion.Euler(0f, 0f, roll * 0.5f), radius * 0.92f, 26f, 0.2f, y0 - 6f, y1 + 6f, 0.18f, 0.22f, new Color(1f, 0.4f, 0.28f, 0.85f), false);
        }

        /// <summary>Selama 8 dtk: aura crimson tipis dan nyala di bilah; detik 6-8 melemah; akhir: padam, tarik napas.</summary>
        static IEnumerator BloodbathPassive(PlayerCombat pc)
        {
            float t = 0f;
            while (t < 8f)
            {
                if (pc == null || pc.IsDead) { BloodbathUntil = 0f; yield break; }
                float k = t < 6f ? 1f : Mathf.Lerp(1f, 0.2f, (t - 6f) / 2f);
                CombatFx.Burst(Chest(pc), Crimson, Mathf.Max(1, Mathf.RoundToInt(3 * k)), 0.55f, 0.6f, 0.7f, 0.1f * (0.6f + 0.4f * k));
                CombatFx.Flash(Tip(pc), Crimson, 3f * k, 0.9f * k, 0.3f);
                if (Random.value < 0.5f * k) CombatFx.Burst(Tip(pc), new Color(1f, 0.45f, 0.2f), 2, 0.2f, 0.5f, 0.4f, 0.12f * k);
                yield return new WaitForSeconds(0.2f);
                t += 0.2f;
            }
            BloodbathUntil = 0f;
            if (pc != null)
            {
                CombatFx.Burst(Tip(pc), new Color(1f, 0.45f, 0.2f), 10, 0.4f, 0.8f, 0.6f, 0.08f);       // api padam menjadi percikan
                CombatFx.Burst(Chest(pc), Crimson, 10, 0.7f, 1.0f, 0.8f, 0.07f);
            }
        }

        /// <summary>Tiap tebasan selama Bloodbath: crimson flame trail bergerigi (tidak stabil), sedikit over-swing.</summary>
        public static void BloodSlash(PlayerCombat pc)
        {
            Vector3 c = Chest(pc);
            float j = Random.Range(-18f, 18f);
            CombatFx.SweepArc(c, pc.transform.rotation * Quaternion.Euler(0f, 0f, j), 3.2f, 32f, 0.22f, 80f, -78f, 0.12f, 0.22f, new Color(0.9f, 0.08f, 0.1f, 0.9f), true);
            CombatFx.SweepArc(c, pc.transform.rotation * Quaternion.Euler(0f, 0f, -j * 0.7f), 3.0f, 18f, 0.14f, 76f, -84f, 0.12f, 0.18f, new Color(1f, 0.5f, 0.25f, 0.85f), false);
        }

        /// <summary>Lifesteal: serpihan crimson masuk ke dada + pulse merah singkat (dibatasi agar tidak berlebihan).</summary>
        public static void BloodPulse(PlayerCombat pc, Vector3 from)
        {
            if (Time.time < bloodPulseAt) return;
            bloodPulseAt = Time.time + 0.12f;
            CombatFx.Host.I.StartCoroutine(CrimsonFragments(pc, from));
            CombatFx.Flash(Chest(pc), Crimson, 4f, 1f, 0.15f);
        }

        // =====================================================================
        // Blademaster — Wind Cutter  [bm_4]   (langkah kecil, bilah angin 12 m menembus)
        // Focused 0-0.18 | Compression 0.18-0.38 | Precision Step 0.38-0.50 | CUT 0.50-0.66 | Snap + Wind Projectile 0.66-0.95 | Follow 0.66-0.82 | Return 0.82-1.05 | Ready 1.15
        // =====================================================================
        static readonly Color WindWhite = new Color(0.85f, 0.95f, 1f), WindBlue = new Color(0.55f, 0.8f, 1f);

        static void WindCutter()
        {
            var d = new ActionDef { id = "bm_4", dur = 1.15f, lockUntil = 0.9f, hitAt = 0.66f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            var focus = PlayerAnimator.Mk(n, p => { p.arR = V(6f, 36f, 12f); p.fR = V(-24f, 0f, 0f); p.arL = V(-50f, 10f, -12f); p.fL = V(-70f, 0f, 0f); p.body = V(2f, -36f, 0f); p.head = V(0f, 30f, 0f); p.lgL = V(-12f, 0f, 0f); p.lgR = V(14f, 0f, 0f); p.lift = -0.06f; });
            var comp = PlayerAnimator.Mk(focus, p => { p.arR = V(14f, 58f, 12f); p.fR = V(-44f, 0f, 0f); p.arL = V(-60f, 16f, -14f); p.body = V(4f, -62f, 0f); p.head = V(0f, 44f, 0f); p.lift = -0.1f; });
            var step = PlayerAnimator.Mk(comp, p => { p.lunge = 0.12f; p.lgL = V(-24f, 0f, 0f); p.lgR = V(14f, 0f, 0f); p.lift = -0.1f; });
            var cut = PlayerAnimator.Mk(step, p => { p.arR = V(-44f, -34f, 8f); p.fR = V(-4f, 0f, 0f); p.arL = V(-30f, -14f, -12f); p.body = V(8f, 32f, 0f); p.head = V(2f, 18f, 0f); p.lunge = 0.2f; p.lift = -0.1f; });
            var hold = PlayerAnimator.Mk(cut, p => { p.arR = V(-46f, -38f, 8f); p.body = V(8f, 36f, 0f); });
            var ret = PlayerAnimator.Mk(hold, p => { p.arR = V(-30f, 24f, 10f); p.fR = V(-50f, 0f, 0f); p.arL = V(-40f, 10f, -10f); p.body = V(4f, -10f, 0f); p.head = V(0f, 6f, 0f); p.lunge = 0.06f; p.lift = -0.06f; });
            d.Key(0f, n).Key(0.18f, focus).Key(0.38f, comp).Key(0.50f, step).Key(0.66f, cut).Key(0.82f, hold).Key(1.05f, ret).Key(1.15f, n);

            d.At(0.18f, pc => { CombatFx.Beam(Tip(pc), Tip(pc) - pc.transform.forward * 0.9f, new Color(0.8f, 0.95f, 1f, 0.5f), 0.04f, 0.25f); });
            d.At(0.30f, pc => { Converge(pc, WindWhite, 0.08f, 0.9f); });
            d.At(0.38f, pc => { pc.DashTo(pc.transform.forward, 0.12f, 3.5f); CombatFx.Dust(Feet(pc), 3); });      // tap -> plant, hanya 30-50 cm
            d.At(0.52f, pc =>
            {
                Audio.Sfx("swing2", pc.transform.position);
                // tebasan ringkas ~90 derajat, naik 25 derajat; tipis putih-biru
                CombatFx.SweepArc(Chest(pc), pc.transform.rotation * Quaternion.Euler(0f, 0f, 25f), 2.8f, 14f, 0.1f, -50f, 45f, 0.12f, 0.2f, new Color(0.8f, 0.95f, 1f, 0.95f), false);
            });
            d.At(0.66f, pc =>
            {
                // SNAP: tekanan udara dilepas dari bilah -> bilah angin 12 m
                Audio.Sfx("heavy_swing", pc.transform.position);
                CombatFx.Flash(Tip(pc), Color.white, 5f, 2f, 0.1f);
                CombatFx.Host.I.StartCoroutine(WindBlade(pc, 12f));
            });
            Add(d);
        }

        static IEnumerator WindBlade(PlayerCombat pc, float range)
        {
            Vector3 f = pc.transform.forward; f.y = 0f; f.Normalize();
            Vector3 o = Tip(pc);
            int steps = 10;
            for (int i = 0; i < steps; i++)
            {
                Vector3 a = o + f * (range * i / steps);
                Vector3 b = a + f * (range / steps * 1.5f);
                CombatFx.Beam(a, b, new Color(0.85f, 0.96f, 1f, 0.9f), 0.08f, 0.14f);
                CombatFx.Beam(a + Vector3.up * 0.12f, b + Vector3.up * 0.12f, new Color(0.55f, 0.8f, 1f, 0.35f), 0.2f, 0.12f);
                CombatFx.Burst(a, WindBlue, 1, 0.2f, 0.2f, 0.3f, 0.06f);
                yield return new WaitForSeconds(0.03f);
            }
        }

        /// <summary>Hit tembus: SHING flash tipis; crit -> garis tebasan kedua sepersekian detik kemudian + flash tajam.</summary>
        public static void CutHitFx(Vector3 pt, Vector3 fwd, bool crit)
        {
            Audio.Sfx(crit ? "swing" : "swing2", pt);
            CombatFx.Flash(pt, Color.white, 4f, 1.6f, 0.08f);
            CombatFx.HitSpark(pt, WindWhite, 0.8f, 8);
            Vector3 r = Vector3.Cross(Vector3.up, fwd);
            CombatFx.Beam(pt - r * 0.9f + Vector3.up * 0.3f, pt + r * 0.9f - Vector3.up * 0.3f, new Color(0.9f, 0.98f, 1f, 0.95f), 0.05f, 0.18f);
            if (crit)
                CombatFx.Host.I.StartCoroutine(Late(0.07f, () =>
                {
                    Audio.Sfx("swing", pt);
                    CombatFx.Flash(pt, Color.white, 8f, 3f, 0.1f);
                    CombatFx.Beam(pt - r * 1.1f - Vector3.up * 0.3f, pt + r * 1.1f + Vector3.up * 0.3f, Color.white, 0.07f, 0.2f);
                    CombatFx.HitSpark(pt, Color.white, 1.2f, 14);
                }));
        }

        // =====================================================================
        // Blademaster — Flash Step  [bm_5]   (meluncur 8 m lurus, damage saat berpapasan, rem presisi)
        // Razor Stance 0-0.12 | Stillness 0.12-0.22 | Foot Release 0.22-0.30 | FLASH STEP 0.30-0.60 (kebal 0.3) | Crossing Cut ~0.45 | Pass 0.58-0.65 | Brake 0.65-0.78 | Lock 0.78-0.92 | Turn 0.92-1.10
        // =====================================================================
        static void FlashStep()
        {
            var d = new ActionDef { id = "bm_5", dur = 1.1f, lockUntil = 0.85f, hitAt = 0.30f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            var razor = PlayerAnimator.Mk(n, p => { p.arR = V(24f, 72f, 12f); p.fR = V(-8f, 0f, 0f); p.arL = V(-40f, 12f, -10f); p.fL = V(-30f, 0f, 0f); p.body = V(30f, -12f, 0f); p.head = V(8f, 0f, 0f); p.lgL = V(-28f, 0f, 0f); p.lgR = V(24f, 0f, 0f); p.lift = -0.16f; });
            var still = PlayerAnimator.Mk(razor, p => { p.lgR = V(30f, 0f, 0f); p.lift = -0.16f; });
            var rel = PlayerAnimator.Mk(still, p => { p.lunge = 0.3f; p.lgL = V(-40f, 0f, 0f); p.lgR = V(34f, 0f, 0f); p.body = V(34f, -10f, 0f); p.lift = -0.14f; });
            var flash = PlayerAnimator.Mk(rel, p => { p.arR = V(26f, 74f, 12f); p.body = V(38f, -8f, 0f); p.lunge = 0.5f; p.lift = -0.14f; p.lgL = V(-50f, 0f, 0f); p.lgR = V(40f, 0f, 0f); });
            var cut = PlayerAnimator.Mk(flash, p => { p.arR = V(-40f, -42f, 8f); p.fR = V(-6f, 0f, 0f); p.arL = V(-30f, -14f, -10f); p.body = V(36f, 26f, 0f); p.head = V(8f, 14f, 0f); p.lunge = 0.52f; });
            var brake = PlayerAnimator.Mk(cut, p => { p.arR = V(-44f, -46f, 8f); p.body = V(18f, 28f, 0f); p.lunge = 0.14f; p.lift = -0.2f; p.lgL = V(-38f, 0f, 0f); p.lgR = V(22f, 0f, 0f); });
            var lockp = PlayerAnimator.Mk(brake, p => { p.body = V(12f, 30f, 0f); p.lunge = 0.06f; p.lift = -0.12f; });
            var turn = PlayerAnimator.Mk(lockp, p => { p.arR = V(-30f, 24f, 10f); p.fR = V(-50f, 0f, 0f); p.arL = V(-40f, 10f, -10f); p.body = V(4f, -6f, 0f); p.head = V(0f, 0f, 0f); p.lunge = 0f; p.lift = -0.06f; });
            d.Key(0f, n).Key(0.12f, razor).Key(0.22f, still).Key(0.30f, rel).Key(0.45f, flash).Key(0.52f, cut).Key(0.60f, cut).Key(0.72f, brake).Key(0.92f, lockp).Key(1.10f, turn);

            d.At(0.12f, pc => { CombatFx.Beam(Feet(pc) + Vector3.up * 0.3f, Tip(pc), new Color(0.8f, 0.95f, 1f, 0.35f), 0.04f, 0.25f); });
            d.At(0.22f, pc => { Audio.Sfx("swing", pc.transform.position); CombatFx.Burst(Feet(pc), WindWhite, 2, 0.3f, 0.2f, 0.3f, 0.05f); });
            d.At(0.30f, pc =>
            {
                Vector3 f = pc.transform.forward; f.y = 0f; f.Normalize();
                Audio.Sfx("dodge", pc.transform.position);
                pc.DashTo(f, 0.3f, 26.7f);
                CombatFx.Host.I.StartCoroutine(Afterimages(pc, 0.04f));          // satu afterimage di titik awal
                // satu garis putih-biru tipis di belakang + distorsi: jalur 8 m
                CombatFx.Beam(Chest(pc) - Vector3.up * 0.2f, Chest(pc) + f * 8f - Vector3.up * 0.2f, new Color(0.85f, 0.97f, 1f, 0.8f), 0.05f, 0.4f);
                CombatFx.Beam(Chest(pc) - Vector3.up * 0.2f, Chest(pc) + f * 8f - Vector3.up * 0.2f, new Color(0.55f, 0.8f, 1f, 0.3f), 0.18f, 0.3f);
            });
            d.At(0.46f, pc =>
            {
                // Crossing Draw Cut: satu kilatan bilah sangat tajam, pendek 60-80 derajat
                Audio.Sfx("swing2", pc.transform.position);
                CombatFx.SweepArc(Chest(pc), pc.transform.rotation * Quaternion.Euler(0f, 0f, 28f), 2.6f, 12f, 0.08f, -45f, 45f, 0.08f, 0.16f, Color.white, false);
                CombatFx.Flash(Tip(pc), Color.white, 4f, 2f, 0.08f);
            });
            d.At(0.65f, pc =>
            {
                Audio.Sfx("block", pc.transform.position);        // TAP!
                CombatFx.Dust(Feet(pc), 3);
                CombatFx.ShockRing(Feet(pc), 1.3f, new Color(0.8f, 0.95f, 1f, 0.4f), 0.15f);
            });
            d.At(0.80f, pc =>
            {
                Vector3 f = pc.transform.forward; f.y = 0f; f.Normalize();
                CombatFx.Beam(Chest(pc) - f * 0.4f, Chest(pc) + f * 3f, new Color(0.85f, 0.97f, 1f, 0.7f), 0.05f, 0.25f);        // garis angin yang mengejar tubuh
                CombatFx.Flash(Tip(pc), WindWhite, 3f, 1.2f, 0.12f);
            });
            Add(d);
        }

        // =====================================================================
        // Blademaster — ULTIMATE Thousand Cuts  [bm_6]   (14 tebasan x 0.12 dtk dari satu pusat, footwork kecil)
        // Focus 0-0.25 | Centering 0.25-0.45 | CUTS 0.45-2.13 | Blade Freeze 2.13 | Reveal 2.15-2.35 | Reset 2.35-2.60
        // =====================================================================
        static void ThousandCuts()
        {
            var d = new ActionDef { id = "bm_6", dur = 2.6f, lockUntil = 2.3f, hitAt = 0.45f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            var focus = PlayerAnimator.Mk(n, p => { p.arR = V(8f, 24f, 10f); p.fR = V(-20f, 0f, 0f); p.arL = V(-50f, 10f, -12f); p.fL = V(-90f, 0f, 0f); p.body = V(4f, -4f, 0f); p.head = V(10f, 0f, 0f); p.lgL = V(-12f, 0f, 0f); p.lgR = V(12f, 0f, 0f); p.lift = -0.08f; });
            var center = PlayerAnimator.Mk(focus, p => { p.arR = V(-70f, 8f, 8f); p.fR = V(-40f, 0f, 0f); p.arL = V(-40f, 10f, -12f); p.fL = V(-100f, 0f, 0f); p.body = V(0f, 0f, 0f); p.head = V(-4f, 0f, 0f); p.lift = -0.06f; });
            var cutR = PlayerAnimator.Mk(center, p => { p.arR = V(-56f, -52f, 8f); p.fR = V(-6f, 0f, 0f); p.arL = V(-40f, -20f, -10f); p.body = V(10f, 30f, 0f); p.head = V(4f, 16f, 0f); p.lift = -0.1f; p.lgL = V(-18f, 0f, 0f); p.lgR = V(16f, 0f, 0f); });
            var cutL = PlayerAnimator.Mk(center, p => { p.arR = V(-56f, 52f, 8f); p.fR = V(-6f, 0f, 0f); p.arL = V(-40f, 20f, -10f); p.body = V(10f, -30f, 0f); p.head = V(4f, -16f, 0f); p.lift = -0.1f; p.lgL = V(-18f, 0f, 0f); p.lgR = V(16f, 0f, 0f); });
            var low = PlayerAnimator.Mk(cutR, p => { p.body = V(26f, 24f, 0f); p.lift = -0.3f; p.arR = V(-30f, -50f, 8f); });
            var high = PlayerAnimator.Mk(cutL, p => { p.body = V(-4f, -20f, 0f); p.lift = -0.02f; p.arR = V(-130f, 40f, 8f); });
            var freeze = PlayerAnimator.Mk(cutR, p => { p.arR = V(-110f, -30f, 8f); p.body = V(8f, 20f, 0f); p.lift = -0.08f; });
            var reset = PlayerAnimator.Mk(freeze, p => { p.arR = V(-30f, 24f, 10f); p.fR = V(-50f, 0f, 0f); p.arL = V(-40f, 10f, -10f); p.body = V(4f, -6f, 0f); p.head = V(0f, 0f, 0f); p.lift = -0.06f; });
            d.Key(0f, n).Key(0.25f, focus).Key(0.45f, center);
            for (int i = 0; i < 14; i++)
            {
                var pz = i % 2 == 0 ? cutR : cutL;
                if (i == 10) pz = low; else if (i == 11) pz = high;
                d.Key(0.45f + 0.12f * (i + 1) - 0.06f, pz);
            }
            d.Key(2.13f, freeze).Key(2.35f, freeze).Key(2.60f, reset);

            d.At(0f, pc => { thousandLines.Clear(); thousandStartRot = pc.transform.rotation; Converge(pc, WindWhite, 0.25f, 3.0f); });
            d.At(0.35f, pc =>
            {
                CombatFx.ShockRing(Feet(pc), 5f, new Color(0.75f, 0.92f, 1f, 0.45f), 0.15f);       // CUTTING RANGE 5 m
                Audio.Sfx("skill_cast", pc.transform.position);
            });
            // 14 tebasan: ritme 1-2 / 3-4 / 5-6 / 7-8 / 9-10 / 11-12 / 13 / 14
            float[] roll = { 0f, 0f, 30f, 30f, -30f, -30f, 0f, 30f, 0f, 0f, 0f, 40f, 0f, 35f };
            float[] y0 = { -60f, 60f, -55f, 55f, 55f, -55f, -60f, 55f, 60f, -60f, -60f, -50f, -90f, -55f };
            float[] y1 = { 60f, -60f, 55f, -55f, -55f, 55f, 60f, -55f, -60f, 60f, 60f, 55f, 90f, 55f };
            float[] pivot = { 0f, 0f, 0f, 0f, 45f, 0f, 90f, 0f, 0f, 180f, 0f, 0f, 180f, 0f };      // putaran jalur terpendek, tidak ada spin 360 penuh
            float[] hgt = { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, -0.6f, 0.45f, 0f, 0f };
            for (int i = 0; i < 14; i++)
            {
                int k = i;
                float t = 0.45f + 0.12f * k + (k == 13 ? 0.05f : 0f);            // jeda kecil sebelum tebasan ke-14
                d.At(t, pc =>
                {
                    if (pivot[k] != 0f) pc.transform.rotation = pc.transform.rotation * Quaternion.Euler(0f, pivot[k] * 0.5f, 0f);
                    if (k % 2 == 0 || k > 11) Audio.Sfx(k == 13 ? "heavy_swing" : "swing2", pc.transform.position);
                    if (k % 3 == 0) pc.DashTo(pc.transform.right * (k % 2 == 0 ? 1f : -1f), 0.08f, 5f);     // micro-footwork 20-60 cm
                    bool last = k == 13, full = k == 12;
                    Quaternion br = pc.transform.rotation * Quaternion.Euler(0f, 0f, roll[k]);
                    Vector3 c = Chest(pc) + Vector3.up * hgt[k];
                    Color col = last ? Color.white : new Color(0.75f, 0.92f, 1f, 0.9f);
                    CombatFx.SweepArc(c, br, full ? 4.6f : 3.4f, last ? 8f : 12f, last ? 0.1f : 0.07f, y0[k], y1[k], full ? 0.14f : 0.08f, last ? 0.3f : 0.15f, col, last);
                    CombatFx.Flash(Tip(pc), col, 2.5f, 1.2f, 0.06f);
                    if (pivot[k] != 0f) pc.transform.rotation = pc.transform.rotation * Quaternion.Euler(0f, pivot[k] * 0.5f, 0f);
                    // garis potong yang tertinggal di area (nanti ditampilkan saat Reveal)
                    thousandLines.Add(new Vector3[] { c + Random.insideUnitSphere * 0.4f, (Quaternion.Euler(Random.Range(-35f, 35f), Random.Range(0f, 360f), 0f) * Vector3.forward) });
                });
            }
            d.At(2.13f, pc => { HitStop(0.06f); CombatFx.Flash(Chest(pc), Color.white, 6f, 2f, 0.1f); });
            d.At(2.15f, pc =>
            {
                // Thousand-Cut Reveal: banyak garis tipis memenuhi radius 5 m, lalu hilang bersamaan (SHING x4)
                Vector3 c0 = Chest(pc);
                foreach (var ln in thousandLines)
                {
                    Vector3 mid = ln[0], dir = ln[1];
                    CombatFx.Beam(mid - dir * 2.8f, mid + dir * 2.8f, new Color(0.85f, 0.97f, 1f, 0.9f), 0.04f, 0.28f);
                }
                for (int i = 0; i < 10; i++)
                {
                    Vector3 a = Quaternion.Euler(0f, i * 36f, 0f) * Vector3.forward * 5f;
                    CombatFx.Beam(c0 + a * 0.2f, c0 + a, new Color(0.7f, 0.9f, 1f, 0.6f), 0.03f, 0.25f);
                }
                thousandLines.Clear();
                for (int i = 0; i < 4; i++) CombatFx.Host.I.StartCoroutine(Late(0.04f * i, () => { Audio.Sfx("swing2", c0); CombatFx.Flash(c0, WindWhite, 9f, 2f, 0.08f); }));
                foreach (var m in Monster.All)
                {
                    if (m == null || m.IsDead) continue;
                    if (Vector3.Distance(m.transform.position, pc.transform.position) > 5.5f) continue;
                    CombatFx.HitSpark(m.HitPoint, WindWhite, 1.2f, 16);
                    CombatFx.Flash(m.HitPoint, Color.white, 5f, 1.6f, 0.1f);
                }
                CombatFx.ShockRing(Feet(pc), 5.5f, new Color(0.8f, 0.95f, 1f, 0.5f), 0.25f);
            });
            d.At(2.35f, pc => { pc.transform.rotation = thousandStartRot; Audio.Sfx("block", pc.transform.position); CombatFx.Flash(Tip(pc), WindWhite, 3f, 1.2f, 0.1f); });      // SNAP
            Add(d);
        }

        static Quaternion thousandStartRot = Quaternion.identity;
        static readonly System.Collections.Generic.List<Vector3[]> thousandLines = new System.Collections.Generic.List<Vector3[]>();

        /// <summary>Hit kecil tiap tebasan: percikan putih tipis; dibatasi agar tidak berlebihan.</summary>
        public static void ThousandHitFx(Vector3 pt, int cut)
        {
            CombatFx.HitSpark(pt, WindWhite, 0.7f, 5);
            if (cut % 2 == 0) CombatFx.Flash(pt, Color.white, 3f, 1f, 0.06f);
        }

        // =====================================================================
        // Paladin Lord — Judgement Slam  [e30_0_4]   (senjata dibanting ke tanah, Holy Force merambat sebagai cone 4.5 m)
        // Stance 0-0.20 | Sacred Draw 0.20-0.42 | Raise 0.42-0.68 | Lock 0.68-0.82 | SLAM 0.82-1.05 | Ground Break 1.05 | Eruption 1.05-1.25 | Hold 1.25-1.45 | Recovery 1.45-1.70 | Lord Stance -1.85
        // =====================================================================
        static readonly Color HolyWhite = new Color(1f, 0.99f, 0.92f), PaleGold = new Color(1f, 0.88f, 0.5f), HolyBlue = new Color(0.8f, 0.9f, 1f);

        static void JudgementSlam()
        {
            var d = new ActionDef { id = "e30_0_4", dur = 1.85f, lockUntil = 1.5f, hitAt = 1.12f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            var stance = PlayerAnimator.Mk(n, p => { p.arL = V(-52f, -8f, -12f); p.fL = V(-92f, 0f, 0f); p.arR = V(-4f, 20f, 10f); p.fR = V(-30f, 0f, 0f); p.body = V(0f, -10f, 0f); p.head = V(0f, 6f, 0f); p.lgL = V(-18f, 0f, 0f); p.lgR = V(20f, 0f, 0f); p.lift = -0.08f; });
            var draw = PlayerAnimator.Mk(stance, p => { p.arR = V(-64f, -30f, 8f); p.fR = V(-90f, 0f, 0f); p.arL = V(-60f, -6f, -12f); p.fL = V(-100f, 0f, 0f); p.body = V(2f, 0f, 0f); p.head = V(0f, 0f, 0f); p.lift = -0.06f; });
            var raise = PlayerAnimator.Mk(draw, p => { p.arR = V(-172f, 0f, 8f); p.fR = V(-6f, 0f, 0f); p.arL = V(-70f, -4f, -12f); p.fL = V(-100f, 0f, 0f); p.body = V(-4f, 0f, 0f); p.head = V(-16f, 0f, 0f); p.lift = -0.02f; });
            var lockp = PlayerAnimator.Mk(raise, p => { p.arR = V(-176f, 0f, 8f); p.arL = V(-50f, -4f, -12f); p.fL = V(-110f, 0f, 0f); p.body = V(6f, 0f, 0f); p.head = V(-6f, 0f, 0f); p.lgL = V(-30f, 0f, 0f); p.lgR = V(26f, 0f, 0f); p.lift = -0.16f; });
            var slam = PlayerAnimator.Mk(lockp, p => { p.arR = V(-44f, 0f, 8f); p.fR = V(-4f, 0f, 0f); p.arL = V(-30f, -8f, -12f); p.fL = V(-60f, 0f, 0f); p.body = V(34f, 0f, 0f); p.head = V(18f, 0f, 0f); p.lunge = 0.36f; p.lift = -0.2f; p.lgL = V(-48f, 0f, 0f); p.lgR = V(28f, 0f, 0f); });
            var hold = PlayerAnimator.Mk(slam, p => { p.body = V(36f, 0f, 0f); p.head = V(16f, 0f, 0f); });
            var rec = PlayerAnimator.Mk(hold, p => { p.arR = V(-30f, 22f, 10f); p.fR = V(-50f, 0f, 0f); p.arL = V(-56f, -8f, -12f); p.fL = V(-96f, 0f, 0f); p.body = V(6f, -4f, 0f); p.head = V(0f, 0f, 0f); p.lunge = 0.1f; p.lift = -0.06f; });
            d.Key(0f, n).Key(0.20f, stance).Key(0.42f, draw).Key(0.68f, raise).Key(0.82f, lockp).Key(1.05f, slam).Key(1.45f, hold).Key(1.70f, rec).Key(1.85f, n);

            d.At(0f, pc => { CombatFx.Flash(Tip(pc), HolyWhite, 3f, 1f, 0.5f); });
            d.At(0.20f, pc =>
            {
                Audio.Sfx("skill_cast", pc.transform.position);
                CombatFx.Beam(Chest(pc) + pc.transform.right * 0.3f, Tip(pc), new Color(1f, 0.95f, 0.75f, 0.7f), 0.1f, 0.4f);      // tangan -> gagang -> bilah
                CombatFx.Flash(Tip(pc), PaleGold, 4f, 1.4f, 0.4f);
            });
            d.At(0.50f, pc =>
            {
                // cahaya turun dari atas menuju bilah + rune kecil di ujung senjata
                Vector3 t = Tip(pc);
                CombatFx.Beam(t + Vector3.up * 6f, t, new Color(1f, 0.97f, 0.85f, 0.8f), 0.2f, 0.5f);
                CombatFx.Flash(t, HolyWhite, 6f, 2f, 0.5f);
                CombatFx.Burst(t, PaleGold, 12, 0.5f, 0.8f, 0.8f, 0.07f);
                HexFlash(t + Vector3.up * 0.4f, Camera.main != null ? -Camera.main.transform.forward : pc.transform.forward, 0.7f, 0.5f);
            });
            d.At(0.72f, pc =>
            {
                Converge(pc, HolyWhite, 0.1f, 1.2f);
                CombatFx.Flash(Tip(pc), HolyWhite, 9f, 3.4f, 0.2f);
            });
            d.At(0.88f, pc =>
            {
                pc.DashTo(pc.transform.forward, 0.14f, 4f);      // satu langkah berat
                Audio.Sfx("heavy_swing", pc.transform.position);
            });
            d.At(1.0f, pc =>
            {
                // KRAAAM: senjata menyentuh tanah 0.5-1 m di depan
                Vector3 f = pc.transform.forward;
                Vector3 g; GroundFront(pc, 1.0f, out g);
                HitStop(0.1f);
                CombatFx.Flash(g + Vector3.up * 0.4f, HolyWhite, 12f, 4f, 0.16f);
                CombatFx.Flash(g + Vector3.up * 0.4f, PaleGold, 14f, 2.5f, 0.35f);
                CombatFx.HitSpark(g + Vector3.up * 0.1f, HolyWhite, 2f, 28);
                CombatFx.Dust(g, 8);
                ThirdPersonCamera.Shake(0.2f, 0.25f);
                // Holy Ground Break: retakan bercahaya merambat sempit -> melebar sampai 4.5 m
                for (int i = 0; i < 7; i++)
                {
                    float ang = Mathf.Lerp(-42f, 42f, i / 6f);
                    Vector3 dir = Quaternion.AngleAxis(ang, Vector3.up) * f;
                    for (int j = 1; j <= 4; j++)
                    {
                        float dist = 0.9f * j + (Mathf.Abs(ang) > 20f ? 0.3f : 0f);
                        Vector3 pt = Feet(pc) + dir * dist;
                        CombatFx.Host.I.StartCoroutine(Late(0.03f * j, () => { MagicFx.Crack(pt, 0.5f + 0.06f * j, 2.5f); CombatFx.Flash(pt + Vector3.up * 0.1f, PaleGold, 2.5f, 0.9f, 0.18f); }));
                    }
                }
                CombatFx.HalfWave(Feet(pc), f, 4.8f, new Color(1f, 0.95f, 0.75f, 0.85f), 0.4f);
            });
            d.At(1.12f, pc =>
            {
                // Judgement Eruption: vertical holy burst pendek dari tanah di dalam cone (musuh terkena dari bawah/depan)
                Vector3 f = pc.transform.forward;
                for (int i = 0; i < 6; i++)
                {
                    float ang = Random.Range(-38f, 38f), dist = Random.Range(1.5f, 4.3f);
                    Vector3 pt = Feet(pc) + Quaternion.AngleAxis(ang, Vector3.up) * f * dist;
                    CombatFx.Host.I.StartCoroutine(Late(0.03f * i, () =>
                    {
                        CombatFx.Beam(pt, pt + Vector3.up * 3.2f, new Color(1f, 0.97f, 0.85f, 0.85f), 0.45f, 0.3f);
                        CombatFx.Flash(pt + Vector3.up * 0.5f, HolyWhite, 5f, 2f, 0.15f);
                        CombatFx.Burst(pt, PaleGold, 8, 0.4f, 2.4f, 0.6f, 0.08f);
                    }));
                }
                foreach (var m in InFront(pc, 4.5f, 50f))
                {
                    CombatFx.HitSpark(m.HitPoint, HolyWhite, 1.5f, 20);
                    CombatFx.Flash(m.HitPoint, HolyBlue, 5f, 1.8f, 0.14f);
                }
            });
            d.At(1.45f, pc => { CombatFx.Burst(Feet(pc) + pc.transform.forward * 2.5f, PaleGold, 8, 1.5f, 0.5f, 0.8f, 0.06f); });
            Add(d);
        }

        // =====================================================================
        // Paladin Lord — Divine Roar  [e30_0_5]   (otoritas suci: cahaya TURUN dari atas, area 10 m ditekan; buff -40% 6 dtk)
        // Brace 0-0.18 | Sacred Draw 0.18-0.40 | Raise Authority 0.40-0.65 | Inhale 0.65-0.78 | ROAR 0.78-1.02 | Dominion 1.02-1.22 | Suppression 1.22-1.42 | Guard 1.42-1.62 | Ready 1.80
        // =====================================================================
        static void DivineRoar()
        {
            var d = new ActionDef { id = "e30_0_5", dur = 1.8f, lockUntil = 1.5f, hitAt = 1.1f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            var brace = PlayerAnimator.Mk(n, p => { p.arL = V(-56f, -6f, -12f); p.fL = V(-96f, 0f, 0f); p.arR = V(-4f, 20f, 10f); p.fR = V(-30f, 0f, 0f); p.body = V(0f, -8f, 0f); p.head = V(0f, 0f, 0f); p.lgL = V(-16f, 0f, 0f); p.lgR = V(14f, 0f, 0f); p.lift = -0.06f; });
            var draw = PlayerAnimator.Mk(brace, p => { p.arR = V(-66f, -24f, 8f); p.fR = V(-70f, 0f, 0f); p.arL = V(-50f, -4f, -12f); p.fL = V(-120f, 0f, 0f); p.body = V(-6f, 0f, 0f); p.head = V(-4f, 0f, 0f); p.lift = -0.04f; });
            var raise = PlayerAnimator.Mk(draw, p => { p.arR = V(-150f, 28f, 20f); p.fR = V(-10f, 0f, 0f); p.arL = V(-30f, -26f, -22f); p.fL = V(-60f, 0f, 0f); p.body = V(-8f, 12f, 0f); p.head = V(-14f, 14f, 0f); p.lift = -0.02f; });
            var inhale = PlayerAnimator.Mk(raise, p => { p.arL = V(-40f, -40f, -30f); p.body = V(-12f, 0f, 0f); p.head = V(-8f, 0f, 0f); p.lgL = V(-24f, 0f, 0f); p.lgR = V(22f, 0f, 0f); p.lift = -0.1f; });
            var roar = PlayerAnimator.Mk(inhale, p => { p.arR = V(-60f, 64f, 40f); p.fR = V(-12f, 0f, 0f); p.arL = V(-40f, -64f, -40f); p.fL = V(-40f, 0f, 0f); p.body = V(-14f, 0f, 0f); p.head = V(-18f, 0f, 0f); p.lift = -0.04f; });
            var guard = PlayerAnimator.Mk(roar, p => { p.arL = V(-80f, -6f, -12f); p.fL = V(-104f, 0f, 0f); p.arR = V(-12f, 24f, 10f); p.fR = V(-40f, 0f, 0f); p.body = V(6f, -6f, 0f); p.head = V(4f, 0f, 0f); p.lgL = V(-28f, 0f, 0f); p.lgR = V(20f, 0f, 0f); p.lift = -0.12f; p.lunge = 0.12f; });
            var ready = PlayerAnimator.Mk(guard, p => { p.arL = V(-64f, -6f, -12f); p.fL = V(-96f, 0f, 0f); p.arR = V(-30f, 18f, 10f); p.fR = V(-50f, 0f, 0f); p.body = V(2f, -4f, 0f); p.lunge = 0.06f; p.lift = -0.06f; });
            d.Key(0f, n).Key(0.18f, brace).Key(0.40f, draw).Key(0.65f, raise).Key(0.78f, inhale).Key(1.02f, roar).Key(1.22f, roar).Key(1.42f, roar).Key(1.62f, guard).Key(1.80f, ready);

            d.At(0.18f, pc =>
            {
                Audio.Sfx("skill_cast", pc.transform.position);
                CombatFx.Beam(Chest(pc) - pc.transform.right * 0.4f, Chest(pc) + Vector3.up * 0.2f, new Color(1f, 0.95f, 0.75f, 0.6f), 0.1f, 0.3f);     // shield -> dada
                CombatFx.Flash(Chest(pc), PaleGold, 4f, 1.3f, 0.4f);
            });
            d.At(0.40f, pc => { CombatFx.Beam(Chest(pc), Tip(pc), new Color(1f, 0.97f, 0.85f, 0.7f), 0.12f, 0.3f); CombatFx.Flash(Tip(pc), HolyWhite, 5f, 1.8f, 0.4f); });
            d.At(0.65f, pc => { Converge(pc, HolyWhite, 0.13f, 2.0f); });
            d.At(0.78f, pc =>
            {
                // Divine Inhale: partikel holy tertarik ke dada (jeda hening singkat)
                CombatFx.Host.I.StartCoroutine(ConvergeBody(pc, HolyWhite, -1f, 0.1f));
                CombatFx.Host.I.StartCoroutine(ConvergeBody(pc, PaleGold, 1f, 0.1f));
            });
            d.At(0.82f, pc =>
            {
                Audio.Sfx("heavy_swing", pc.transform.position);
                CombatFx.Flash(Chest(pc), HolyWhite, 7f, 2.4f, 0.25f);     // seruan singkat HAAAH, tanpa shockwave emas dari tubuh
            });
            d.At(1.02f, pc =>
            {
                // Divine Dominion: cahaya putih TURUN dari atas, lalu tekanan holy menyebar di tanah sampai 10 m
                Vector3 c = Feet(pc);
                CombatFx.Beam(c + Vector3.up * 14f, c + Vector3.up * 0.2f, new Color(1f, 0.98f, 0.9f, 0.9f), 1.2f, 0.45f);
                CombatFx.Flash(c + Vector3.up * 2f, HolyWhite, 14f, 3.5f, 0.4f);
                MagicFx.MagicCircle(c + Vector3.up * 0.05f, 10f, new Color(1f, 0.93f, 0.65f, 0.9f), 2.6f);
                for (int i = 0; i < 3; i++)
                {
                    float r = 4f + i * 3f; float dl = 0.07f * i;
                    CombatFx.Host.I.StartCoroutine(Late(dl, () => { if (pc != null) CombatFx.ShockRing(Feet(pc), r, new Color(1f, 0.95f, 0.75f, 0.7f), 0.4f); }));
                }
                ThirdPersonCamera.Shake(0.08f, 0.2f);
            });
            d.At(1.22f, pc =>
            {
                // Enemy Suppression: tersentak, tertahan, simbol holy kecil ~0.4 s; tanpa knockback
                foreach (var m in Monster.All)
                {
                    if (m == null || m.IsDead) continue;
                    if (Vector3.Distance(m.transform.position, pc.transform.position) > 10f) continue;
                    m.Alert();
                    HexFlash(m.HitPoint + Vector3.up * 1.2f, Camera.main != null ? -Camera.main.transform.forward : Vector3.back, 0.6f, 0.45f);
                    CombatFx.Flash(m.HitPoint, HolyWhite, 4f, 1.4f, 0.2f);
                    CombatFx.Beam(m.HitPoint + Vector3.up * 5f, m.HitPoint, new Color(1f, 0.97f, 0.85f, 0.5f), 0.15f, 0.3f);
                }
            });
            d.At(1.42f, pc =>
            {
                // Divine Guard: buff -40% aktif, shield maju sekali
                GuardUntil = Time.time + 6f;
                guardMain = PaleGold; guardHot = HolyWhite;
                Audio.Sfx("block", pc.transform.position);
                CombatFx.Flash(Chest(pc) + pc.transform.forward * 0.5f, HolyWhite, 5f, 1.6f, 0.2f);
                CombatFx.Host.I.StartCoroutine(DivineProtection(pc, 6f));
            });
            Add(d);
        }

        /// <summary>6 dtk: simbol holy tipis melayang di atas bahu, outline putih-pale gold, cahaya shield; pecah jadi partikel di akhir.</summary>
        static IEnumerator DivineProtection(PlayerCombat pc, float secs)
        {
            float t = 0f;
            while (t < secs)
            {
                if (pc == null || pc.IsDead) yield break;
                float k = t < secs - 1.5f ? 1f : Mathf.Lerp(1f, 0.2f, (t - (secs - 1.5f)) / 1.5f);
                Vector3 sym = Chest(pc) + Vector3.up * 0.95f - pc.transform.forward * 0.3f + pc.transform.right * 0.35f;
                if (Mathf.Repeat(t, 1f) < 0.25f) HexFlash(sym, Camera.main != null ? -Camera.main.transform.forward : pc.transform.forward, 0.45f * k, 1.0f);
                CombatFx.Flash(Chest(pc), PaleGold, 3f * k, 0.5f * k, 0.5f);
                CombatFx.Burst(Chest(pc), HolyWhite, Mathf.Max(1, Mathf.RoundToInt(2 * k)), 0.5f, 0.3f, 0.7f, 0.05f);
                yield return new WaitForSeconds(0.5f);
                t += 0.5f;
            }
            if (pc != null) { CombatFx.Burst(Chest(pc) + Vector3.up * 0.6f, HolyWhite, 16, 0.6f, 1.2f, 1f, 0.07f); CombatFx.Burst(Chest(pc), PaleGold, 8, 0.7f, 0.8f, 0.8f, 0.06f); }
        }

        // =====================================================================
        // Paladin Lord — ULTIMATE Ascended Fortress  [e30_0_6]   (10 dtk: -75% damage masuk, +25% damage; kebal singkat; heal 30%)
        // Stance 0-0.18 | Offering 0.18-0.40 | Release 0.40-0.62 | ASCENSION 0.62-0.82 | Restoration 0.82-1.05 | Heart Pulse 1.05-1.22 | Manifestation 1.22-1.42 | Descent 1.42-1.58 | Draw 1.58-1.78 | ACTIVE 1.78
        // =====================================================================
        public static float AscendedUntil;
        public static bool AscendedActive { get { return Time.time < AscendedUntil; } }

        static void AscendedFortress()
        {
            var d = new ActionDef { id = "e30_0_6", dur = 1.95f, lockUntil = 1.6f, hitAt = 1.78f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            var stance = PlayerAnimator.Mk(n, p => { p.arL = V(-12f, 6f, -10f); p.fL = V(-20f, 0f, 0f); p.arR = V(8f, 10f, 8f); p.fR = V(-14f, 0f, 0f); p.body = V(4f, 0f, 0f); p.head = V(12f, 0f, 0f); p.lift = -0.02f; });
            var offer = PlayerAnimator.Mk(stance, p => { p.arR = V(-30f, 6f, 8f); p.fR = V(-60f, 0f, 0f); p.arL = V(-30f, 6f, -10f); p.fL = V(-50f, 0f, 0f); p.body = V(6f, 0f, 0f); p.head = V(14f, 0f, 0f); p.lift = -0.04f; });
            var release = PlayerAnimator.Mk(offer, p => { p.arR = V(-30f, 70f, 40f); p.fR = V(-8f, 0f, 0f); p.arL = V(-30f, -70f, -40f); p.fL = V(-8f, 0f, 0f); p.body = V(-6f, 0f, 0f); p.head = V(-8f, 0f, 0f); p.lift = -0.02f; });
            var asc = PlayerAnimator.Mk(release, p => { p.arR = V(-34f, 74f, 44f); p.arL = V(-34f, -74f, -44f); p.body = V(-10f, 0f, 0f); p.head = V(-16f, 0f, 0f); p.lift = 0.35f; p.lgL = V(2f, 0f, 0f); p.lgR = V(-2f, 0f, 0f); });
            var rest = PlayerAnimator.Mk(asc, p => { p.body = V(-18f, 0f, 0f); p.head = V(-22f, 0f, 0f); p.lift = 0.38f; });
            var heart = PlayerAnimator.Mk(rest, p => { p.arR = V(-60f, 20f, 10f); p.fR = V(-90f, 0f, 0f); p.arL = V(-36f, -50f, -30f); p.fL = V(-20f, 0f, 0f); p.body = V(0f, 0f, 0f); p.head = V(-6f, 0f, 0f); p.lift = 0.36f; });
            var manif = PlayerAnimator.Mk(heart, p => { p.arR = V(-40f, 56f, 36f); p.arL = V(-40f, -56f, -36f); p.fR = V(-12f, 0f, 0f); p.fL = V(-12f, 0f, 0f); p.body = V(-12f, 0f, 0f); p.head = V(-12f, 0f, 0f); p.lift = 0.32f; });
            var desc = PlayerAnimator.Mk(manif, p => { p.arR = V(-30f, 30f, 18f); p.arL = V(-40f, -30f, -18f); p.body = V(2f, 0f, 0f); p.head = V(0f, 0f, 0f); p.lift = -0.04f; });
            var draw = PlayerAnimator.Mk(desc, p => { p.arL = V(-80f, -6f, -12f); p.fL = V(-104f, 0f, 0f); p.arR = V(-40f, 20f, 10f); p.fR = V(-46f, 0f, 0f); p.body = V(8f, -6f, 0f); p.lgL = V(-26f, 0f, 0f); p.lgR = V(20f, 0f, 0f); p.lift = -0.12f; });
            var active = PlayerAnimator.Mk(draw, p => { p.arL = V(-66f, -6f, -12f); p.fL = V(-96f, 0f, 0f); p.arR = V(-30f, 18f, 10f); p.fR = V(-50f, 0f, 0f); p.body = V(2f, -4f, 0f); p.lunge = 0.08f; p.lift = -0.06f; });
            d.Key(0f, n).Key(0.18f, stance).Key(0.40f, offer).Key(0.62f, release).Key(0.82f, asc).Key(1.05f, rest).Key(1.22f, heart).Key(1.42f, manif).Key(1.58f, desc).Key(1.78f, draw).Key(1.95f, active);

            d.At(0.18f, pc => { Audio.Sfx("skill_cast", pc.transform.position); CombatFx.Burst(Feet(pc), HolyWhite, 8, 0.6f, 1.0f, 0.9f, 0.07f); CombatFx.Beam(Feet(pc), Feet(pc) + Vector3.up * 2.4f, new Color(1f, 0.97f, 0.85f, 0.4f), 0.5f, 0.4f); });
            d.At(0.40f, pc => { MagicFx.MagicCircle(Feet(pc) + Vector3.up * 0.05f, 3.2f, new Color(1f, 0.93f, 0.65f, 0.8f), 1.8f); CombatFx.Flash(Chest(pc), HolyWhite, 6f, 2f, 0.5f); });
            d.At(0.62f, pc =>
            {
                pc.Invuln(0.95f);                          // kebal dari mulai terangkat sampai kaki menyentuh tanah (TAP)
                Audio.Sfx("heavy_swing", pc.transform.position);
                CombatFx.Beam(Feet(pc) + Vector3.up * 12f, Feet(pc), new Color(1f, 0.98f, 0.9f, 0.85f), 0.9f, 0.9f);
                CombatFx.Flash(Chest(pc), HolyWhite, 10f, 3f, 0.6f);
                CombatFx.Burst(Feet(pc), PaleGold, 22, 1.0f, 2.2f, 1.1f, 0.08f);
            });
            d.At(0.82f, pc =>
            {
                // Divine Restoration: pita energi dari tanah dan dari atas menuju dada, heal 30% (pulse putih-hijau tipis)
                for (int i = 0; i < 8; i++)
                {
                    float a = i * 45f * Mathf.Deg2Rad;
                    Vector3 gp = Feet(pc) + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 2.0f;
                    CombatFx.Beam(gp, Chest(pc), new Color(1f, 0.93f, 0.7f, 0.55f), 0.1f, 0.3f);
                    CombatFx.Beam(Chest(pc) + new Vector3(Mathf.Cos(a), 4f, Mathf.Sin(a)) * 0.6f, Chest(pc), new Color(1f, 0.98f, 0.9f, 0.55f), 0.1f, 0.3f);
                }
                float healAmt = pc.MaxHp * 0.3f;
                pc.Heal(healAmt);
                CombatFx.DamageNumber(pc.transform.position + Vector3.up * 2.3f, healAmt, false, new Color(0.7f, 1f, 0.8f));
                CombatFx.Flash(Chest(pc), new Color(0.85f, 1f, 0.9f), 6f, 1.8f, 0.4f);
            });
            d.At(1.05f, pc => { CombatFx.Flash(Chest(pc), Color.white, 9f, 3.5f, 0.18f); CombatFx.ShockRing(Chest(pc) - Vector3.up * 0.4f, 1.8f, new Color(1f, 0.95f, 0.8f, 0.6f), 0.18f); Audio.Sfx("block", pc.transform.position); });
            d.At(1.22f, pc =>
            {
                // Divine Manifestation: siluet sayap/halo cahaya di belakang tubuh ~0.2 s
                Vector3 b = pc.transform.position - pc.transform.forward * 0.5f;
                for (int i = 0; i < 6; i++)
                {
                    float up = 0.2f + i * 0.2f;
                    CombatFx.Beam(b + Vector3.up * up, b + pc.transform.right * (1.0f + i * 0.35f) + Vector3.up * (up + 0.8f), new Color(1f, 0.97f, 0.85f, 0.8f), 0.14f, 0.25f);
                    CombatFx.Beam(b + Vector3.up * up, b - pc.transform.right * (1.0f + i * 0.35f) + Vector3.up * (up + 0.8f), new Color(1f, 0.97f, 0.85f, 0.8f), 0.14f, 0.25f);
                }
                HexFlash(b + Vector3.up * 1.7f, pc.transform.forward, 1.3f, 0.3f);
                CombatFx.Flash(b + Vector3.up * 1.2f, HolyWhite, 9f, 2.5f, 0.3f);
            });
            d.At(1.58f, pc =>
            {
                Audio.Sfx("block", pc.transform.position);        // TAP (lembut): tanpa retak/hentakan
                CombatFx.Burst(Feet(pc), HolyWhite, 8, 0.7f, 0.4f, 0.6f, 0.06f);
            });
            d.At(1.78f, pc =>
            {
                AscendedUntil = GuardUntil = Time.time + 10f;
                guardMain = PaleGold; guardHot = HolyWhite;
                CombatFx.Flash(Chest(pc), HolyWhite, 7f, 2f, 0.25f);
                CombatFx.Host.I.StartCoroutine(AscendedPassive(pc));
            });
            Add(d);
        }

        static Texture2D haloTex;
        static Texture2D HaloTex()
        {
            if (haloTex != null) return haloTex;
            int n = 128;
            haloTex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            haloTex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x / (n - 1f)) * 2f - 1f, v = (y / (n - 1f)) * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v);
                    float a = Mathf.Clamp01(1f - Mathf.Abs(r - 0.8f) / 0.07f) + 0.15f * Mathf.Clamp01(1f - Mathf.Abs(r - 0.8f) / 0.2f);
                    haloTex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
                }
            haloTex.Apply();
            return haloTex;
        }

        /// <summary>10 dtk: halo suci tipis vertikal di belakang tubuh, cahaya dari celah armor; terpecah di detik 8-10; pecah jadi partikel di akhir.</summary>
        static IEnumerator AscendedPassive(PlayerCombat pc)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            go.name = "FxHalo";
            var rend = go.GetComponent<MeshRenderer>();
            rend.sharedMaterial = CombatFx.Mat(HaloTex(), PaleGold);
            Material inst = rend.material;
            float t = 0f, tick = 0f;
            while (t < 10f)
            {
                if (pc == null || pc.IsDead || go == null) { AscendedUntil = GuardUntil = 0f; if (go != null) UnityEngine.Object.Destroy(go); yield break; }
                float k = t < 8f ? 1f : Mathf.Lerp(1f, 0.15f, (t - 8f) / 2f);
                bool flick = t > 8f && Mathf.Repeat(t, 0.3f) < 0.12f;
                Vector3 pos = Chest(pc) + Vector3.up * 0.75f - pc.transform.forward * 0.45f;
                go.transform.position = pos;
                go.transform.rotation = Camera.main != null ? Camera.main.transform.rotation : pc.transform.rotation;
                float sz = 1.5f * (0.6f + 0.4f * k);
                go.transform.localScale = new Vector3(sz, sz, 1f);
                Color c = PaleGold; c.a = flick ? 0.05f : 0.55f * k * (0.85f + 0.15f * Mathf.Sin(t * 5f));
                inst.color = c;
                tick += Time.deltaTime;
                if (tick > 0.3f)
                {
                    tick = 0f;
                    CombatFx.Flash(Chest(pc), HolyWhite, 3f * k, 0.6f * k, 0.3f);
                    CombatFx.Flash(Tip(pc), HolyWhite, 2.5f * k, 0.8f * k, 0.3f);
                    CombatFx.Burst(Chest(pc), HolyWhite, 1, 0.5f, 0.3f, 0.7f, 0.05f);
                }
                yield return null;
                t += Time.deltaTime;
            }
            AscendedUntil = GuardUntil = 0f;
            if (go != null)
            {
                CombatFx.Burst(go.transform.position, PaleGold, 24, 0.9f, 2.0f, 1.2f, 0.07f);        // halo pecah menjadi partikel naik
                CombatFx.Burst(go.transform.position, HolyWhite, 12, 0.6f, 1.6f, 1.0f, 0.06f);
                UnityEngine.Object.Destroy(go);
            }
        }

        /// <summary>Tiap kontak bilah selama Ascended: flash putih-emas sekunder sepersekian detik setelah impact.</summary>
        public static void AscendedSlash(PlayerCombat pc)
        {
            CombatFx.Host.I.StartCoroutine(Late(0.16f, () =>
            {
                if (pc == null) return;
                foreach (var m in InFront(pc, 3.4f, 70f)) { CombatFx.Flash(m.HitPoint, HolyWhite, 4f, 1.4f, 0.1f); CombatFx.HitSpark(m.HitPoint, PaleGold, 1f, 8); }
            }));
        }

        // =====================================================================
        // Warlord — Ground Breaker  [e30_1_4]   (ayunan overhead, tekanan melesat 26 m, tanah jauh meledak radius 5 m)
        // Lock 0-0.18 | Heavy Draw 0.18-0.40 | Overhead Load 0.40-0.62 | Ground Mark 0.62-0.78 | RELEASE 0.78-1.02 | Wave 1.02-1.25 | Compression 1.25-1.38 | BREAK 1.38-1.62 | Follow -1.82 | Drag -2.08 | Ready 2.25
        // =====================================================================
        public static Vector3 GroundCenter;
        public static bool GroundCenterSet;

        static void GroundBreaker()
        {
            var d = new ActionDef { id = "e30_1_4", dur = 2.25f, lockUntil = 1.9f, hitAt = 1.38f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            var lockp = PlayerAnimator.Mk(n, p => { p.arR = V(-8f, 36f, 12f); p.fR = V(-34f, 0f, 0f); p.arL = V(-12f, 24f, -10f); p.fL = V(-44f, 0f, 0f); p.body = V(16f, -10f, 0f); p.head = V(14f, 0f, 0f); p.lgL = V(-26f, 0f, 0f); p.lgR = V(30f, 0f, 0f); p.lift = -0.16f; });
            var draw = PlayerAnimator.Mk(lockp, p => { p.arR = V(22f, 86f, 14f); p.fR = V(-16f, 0f, 0f); p.arL = V(10f, 70f, -10f); p.fL = V(-24f, 0f, 0f); p.body = V(10f, -70f, 0f); p.head = V(8f, -26f, 0f); p.lgL = V(-12f, 0f, 0f); p.lgR = V(34f, 0f, 0f); p.lift = -0.2f; p.lunge = -0.1f; });
            var load = PlayerAnimator.Mk(draw, p => { p.arR = V(-150f, 40f, 18f); p.fR = V(-60f, 0f, 0f); p.arL = V(-140f, 30f, -14f); p.fL = V(-62f, 0f, 0f); p.body = V(-10f, -24f, 0f); p.head = V(-6f, -10f, 0f); p.lgL = V(-18f, 0f, 0f); p.lgR = V(30f, 0f, 0f); p.lift = -0.18f; });
            var mark = PlayerAnimator.Mk(load, p => { p.body = V(0f, -14f, 6f); p.lgL = V(-30f, 0f, 0f); p.lgR = V(38f, 0f, 0f); p.lift = -0.28f; });
            var rel = PlayerAnimator.Mk(mark, p => { p.arR = V(-30f, -30f, 8f); p.fR = V(-6f, 0f, 0f); p.arL = V(-28f, -20f, -8f); p.fL = V(-8f, 0f, 0f); p.body = V(36f, 20f, 0f); p.head = V(18f, 8f, 0f); p.lunge = 0.44f; p.lift = -0.22f; p.lgL = V(-52f, 0f, 0f); p.lgR = V(34f, 0f, 0f); });
            var follow = PlayerAnimator.Mk(rel, p => { p.arR = V(-24f, -36f, 8f); p.arL = V(-24f, -26f, -8f); p.body = V(46f, 24f, 0f); p.lunge = 0.52f; p.lift = -0.26f; });
            var drag = PlayerAnimator.Mk(follow, p => { p.arR = V(4f, 30f, 12f); p.fR = V(-30f, 0f, 0f); p.arL = V(0f, 20f, -10f); p.fL = V(-40f, 0f, 0f); p.body = V(24f, 0f, 0f); p.head = V(8f, 0f, 0f); p.lunge = 0.2f; p.lift = -0.16f; p.lgL = V(-34f, 0f, 0f); p.lgR = V(30f, 0f, 0f); });
            var ready = PlayerAnimator.Mk(drag, p => { p.arR = V(-8f, 36f, 12f); p.fR = V(-34f, 0f, 0f); p.arL = V(-12f, 24f, -10f); p.fL = V(-44f, 0f, 0f); p.body = V(8f, -4f, 0f); p.lunge = 0.06f; p.lift = -0.1f; });
            d.Key(0f, n).Key(0.18f, lockp).Key(0.40f, draw).Key(0.62f, load).Key(0.78f, mark).Key(1.02f, rel).Key(1.62f, follow).Key(1.82f, follow).Key(2.08f, drag).Key(2.25f, ready);

            d.At(0f, pc =>
            {
                GroundCenter = SkillExec.AimGroundFor(pc, 26f);
                GroundCenterSet = true;
                Vector3 to = GroundCenter - pc.transform.position; to.y = 0f;
                if (to.sqrMagnitude > 0.01f) pc.transform.rotation = Quaternion.LookRotation(to.normalized);
                CombatFx.Flash(Tip(pc), Crimson, 3f, 1f, 0.3f);
            });
            d.At(0.18f, pc => { Audio.Sfx("skill_cast", pc.transform.position); Converge(pc, new Color(0.9f, 0.25f, 0.1f), 0.22f, 1.4f); });
            d.At(0.40f, pc => { CombatFx.Flash(Tip(pc), new Color(1f, 0.45f, 0.15f), 6f, 2f, 0.4f); CombatFx.Burst(Tip(pc), new Color(1f, 0.35f, 0.12f), 12, 0.5f, 0.8f, 0.7f, 0.08f); });
            d.At(0.62f, pc =>
            {
                // Ground Mark: reticle/retakan merah samar radius 5 m di titik bidik
                Vector3 c = GroundCenter;
                MagicFx.MagicCircle(c + Vector3.up * 0.05f, 5f, new Color(0.9f, 0.12f, 0.08f, 0.7f), 0.95f);
                CombatFx.ShockRing(c, 5f, new Color(1f, 0.3f, 0.12f, 0.5f), 0.5f);
                CombatFx.Flash(Tip(pc), new Color(1f, 0.55f, 0.2f), 8f, 3f, 0.3f);
            });
            d.At(0.85f, pc => { pc.DashTo(pc.transform.forward, 0.15f, 5f); Audio.Sfx("heavy_swing", pc.transform.position); });
            d.At(0.98f, pc =>
            {
                // bilah di titik terendah (tanpa menyentuh tanah): pressure projectile dilepas
                Audio.Sfx("heavy_swing", pc.transform.position);
                CombatFx.Flash(Tip(pc), Color.white, 9f, 3.4f, 0.14f);
                CombatFx.SweepArc(Chest(pc) + Vector3.up * 0.4f, pc.transform.rotation * Quaternion.Euler(0f, 0f, 90f), 3.6f, 60f, 0.5f, 150f, -30f, 0.2f, 0.3f, new Color(0.9f, 0.18f, 0.08f, 0.95f), true);
                ThirdPersonCamera.Shake(0.16f, 0.2f);
            });
            d.At(1.02f, pc => { CombatFx.Host.I.StartCoroutine(BreakerWave(pc, GroundCenter, 0.23f)); });
            d.At(1.25f, pc => { CombatFx.Host.I.StartCoroutine(GroundSuck(GroundCenter, 5f, 0.13f)); });
            d.At(1.38f, pc =>
            {
                // GROUND BREAK: tanah meledak dari bawah, retakan radial 5 m, batu & debu naik, pressure ring rendah
                Vector3 c = GroundCenter;
                CombatFx.Flash(c + Vector3.up * 1.0f, Color.white, 16f, 4.5f, 0.14f);
                CombatFx.Flash(c + Vector3.up * 0.8f, new Color(1f, 0.4f, 0.12f), 18f, 3f, 0.4f);
                CombatFx.Dust(c, 30);
                MagicFx.RockBurst(c, 4.5f, 20);
                MagicFx.Debri(c, 5f, 18, 1.8f);
                for (int i = 0; i < 8; i++)
                {
                    float a = i * 45f * Mathf.Deg2Rad;
                    Vector3 pt = c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 3f;
                    MagicFx.Crack(pt, 1.2f, 3.5f);
                }
                MagicFx.Crack(c, 2.2f, 4f);
                CombatFx.ShockRing(c, 6.5f, new Color(1f, 0.35f, 0.12f, 0.9f), 0.4f);
                CombatFx.ShockRing(c, 4f, new Color(1f, 0.92f, 0.8f, 0.9f), 0.28f);
                CombatFx.Burst(c + Vector3.up * 0.2f, Crimson, 34, 2.2f, 8f, 1.1f, 0.18f);
                foreach (var m in Monster.All)
                    if (m != null && !m.IsDead && Vector3.Distance(m.transform.position, c) < 5.5f) CombatFx.HitSpark(m.HitPoint, new Color(1f, 0.45f, 0.2f), 1.6f, 22);
                ThirdPersonCamera.Shake(0.3f, 0.35f);
                Audio.Sfx("heavy_swing", c);
            });
            d.At(1.62f, pc => { CombatFx.Burst(GroundCenter + Vector3.up * 0.4f, new Color(1f, 0.5f, 0.2f), 14, 2.5f, 1.2f, 1.4f, 0.1f); CombatFx.Dust(GroundCenter, 12); });
            d.At(1.9f, pc => { CombatFx.Dust(Feet(pc), 4); });
            Add(d);
        }

        /// <summary>Garis tekanan merah-oranye merobek udara rendah dari Warlord menuju titik bidik.</summary>
        static IEnumerator BreakerWave(PlayerCombat pc, Vector3 target, float travel)
        {
            Vector3 o = Feet(pc) + Vector3.up * 0.5f;
            Vector3 dir = target - o; dir.y = 0f;
            float dist = dir.magnitude;
            if (dist < 0.5f) yield break;
            dir.Normalize();
            int steps = 12;
            for (int i = 0; i < steps; i++)
            {
                Vector3 a = o + dir * (dist * i / steps);
                Vector3 b = o + dir * (dist * (i + 1.6f) / steps);
                CombatFx.Beam(a, b, new Color(1f, 0.92f, 0.8f, 0.9f), 0.12f, 0.16f);
                CombatFx.Beam(a, b, new Color(0.9f, 0.2f, 0.08f, 0.6f), 0.4f, 0.2f);
                CombatFx.Dust(a - Vector3.up * 0.4f, 2);
                yield return new WaitForSeconds(travel / steps);
            }
        }

        /// <summary>Micro-delay sebelum meledak: debu/tanah di radius tertarik ke pusat (peringatan untuk musuh).</summary>
        static IEnumerator GroundSuck(Vector3 c, float radius, float secs)
        {
            for (int k = 0; k < 4; k++)
            {
                for (int i = 0; i < 6; i++)
                {
                    float a = (i * 60f + k * 20f) * Mathf.Deg2Rad;
                    Vector3 p = c + new Vector3(Mathf.Cos(a), 0.1f, Mathf.Sin(a)) * radius * 0.9f;
                    var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    UnityEngine.Object.Destroy(q.GetComponent<Collider>());
                    q.name = "FxSuck";
                    q.transform.position = p;
                    q.GetComponent<MeshRenderer>().sharedMaterial = CombatFx.Mat(CombatFx.Soft(), new Color(0.75f, 0.6f, 0.45f, 0.7f));
                    var fx = q.AddComponent<CombatFx.FxAnim>();
                    fx.life = 0.18f; fx.billboard = true; fx.color = new Color(0.75f, 0.6f, 0.45f, 0.7f);
                    fx.s0 = Vector3.one * 0.5f; fx.s1 = Vector3.one * 0.2f;
                    fx.vel = (c - p) / 0.18f;
                }
                yield return new WaitForSeconds(secs / 4f);
            }
        }

        // =====================================================================
        // Warlord — Battle Fury  [e30_1_5]   (pedang tertanam, torsi dikompres, satu putaran 360 berat, cincin crimson 8 m)
        // Lock 0-0.18 | Reverse Grip 0.18-0.38 | Anchor 0.38-0.58 | Compression 0.58-0.78 | Torque 0.78-0.94 | BREAK FREE 0.94-1.08 | SWEEP 1.08-1.32 | RELEASE 1.32 | Ring -1.50 | Over-Brake -1.72 | Recenter -1.95 | Ready 2.10
        // =====================================================================
        static void BattleFury()
        {
            var d = new ActionDef { id = "e30_1_5", dur = 2.1f, lockUntil = 1.75f, hitAt = 1.32f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            var lockp = PlayerAnimator.Mk(n, p => { p.arR = V(-10f, 34f, 12f); p.fR = V(-40f, 0f, 0f); p.arL = V(-14f, 22f, -10f); p.fL = V(-50f, 0f, 0f); p.body = V(8f, -30f, 0f); p.head = V(8f, 20f, 0f); p.lgL = V(-22f, 0f, 0f); p.lgR = V(26f, 0f, 0f); p.lift = -0.12f; });
            var grip = PlayerAnimator.Mk(lockp, p => { p.arR = V(-60f, 20f, 12f); p.fR = V(-70f, 0f, 0f); p.arL = V(-56f, 10f, -10f); p.fL = V(-80f, 0f, 0f); p.body = V(14f, -20f, 0f); p.head = V(12f, 10f, 0f); p.lgL = V(-28f, 0f, 0f); p.lgR = V(34f, 0f, 0f); p.lift = -0.2f; });
            var anchor = PlayerAnimator.Mk(grip, p => { p.arR = V(-28f, 8f, 10f); p.fR = V(-4f, 0f, 0f); p.arL = V(-24f, 6f, -8f); p.fL = V(-6f, 0f, 0f); p.body = V(26f, -16f, 0f); p.head = V(16f, 0f, 0f); p.lunge = 0.1f; p.lift = -0.26f; });
            var comp = PlayerAnimator.Mk(anchor, p => { p.body = V(34f, -22f, 0f); p.head = V(24f, 0f, 0f); p.lgL = V(-44f, 0f, 0f); p.lgR = V(42f, 0f, 0f); p.lift = -0.34f; });
            var torque = PlayerAnimator.Mk(comp, p => { p.body = V(30f, -76f, 0f); p.head = V(14f, -30f, 0f); p.lift = -0.34f; });
            var freeP = PlayerAnimator.Mk(torque, p => { p.arR = V(-70f, 66f, 12f); p.arL = V(-60f, 50f, -10f); p.body = V(18f, 40f, 0f); p.head = V(8f, 20f, 0f); p.lift = -0.24f; });
            var sweep = PlayerAnimator.Mk(freeP, p => { p.arR = V(-76f, -70f, 8f); p.fR = V(-6f, 0f, 0f); p.arL = V(-68f, -50f, -8f); p.fL = V(-8f, 0f, 0f); p.body = V(22f, 340f, 0f); p.head = V(8f, 90f, 0f); p.lift = -0.2f; p.lgL = V(-34f, 0f, 0f); p.lgR = V(30f, 0f, 0f); });
            var brake = PlayerAnimator.Mk(sweep, p => { p.arR = V(-60f, -90f, 8f); p.arL = V(-52f, -70f, -8f); p.body = V(26f, 330f, 0f); p.lunge = 0.2f; p.lift = -0.24f; p.lgL = V(-46f, 0f, 0f); p.lgR = V(34f, 0f, 0f); });
            var recenter = PlayerAnimator.Mk(brake, p => { p.arR = V(-20f, 30f, 12f); p.fR = V(-44f, 0f, 0f); p.arL = V(-20f, 20f, -10f); p.fL = V(-50f, 0f, 0f); p.body = V(14f, 0f, 0f); p.head = V(6f, 0f, 0f); p.lunge = 0.06f; p.lift = -0.14f; });
            d.Key(0f, n).Key(0.18f, lockp).Key(0.38f, grip).Key(0.58f, anchor).Key(0.78f, comp).Key(0.94f, torque).Key(1.08f, freeP).Key(1.32f, sweep).Key(1.72f, brake).Key(1.95f, recenter).Key(2.10f, n);

            d.At(0.38f, pc => { Audio.Sfx("block", pc.transform.position); CombatFx.Dust(Feet(pc), 6); });
            d.At(0.50f, pc =>
            {
                // Weapon Anchor: ujung pedang menancap sedikit ke tanah, KRAK!
                Vector3 g; GroundFront(pc, 1.0f, out g);
                MagicFx.Crack(g, 0.5f, 3f);
                CombatFx.HitSpark(g + Vector3.up * 0.05f, new Color(1f, 0.75f, 0.4f), 0.8f, 10);
                CombatFx.Dust(g, 6);
            });
            d.At(0.58f, pc =>
            {
                // Power Compression: energi crimson-oranye naik dari tanah ke pedang; debu tertarik ke Warlord
                CombatFx.Host.I.StartCoroutine(GroundSuck(Feet(pc), 2.4f, 0.2f));
                CombatFx.Host.I.StartCoroutine(ConvergeBody(pc, new Color(1f, 0.4f, 0.12f), 1f, 0.2f));
                CombatFx.Burst(Feet(pc), Crimson, 10, 0.8f, 1.6f, 0.8f, 0.08f);
            });
            d.At(0.78f, pc => { CombatFx.Flash(Chest(pc), Crimson, 4f, 1.2f, 0.3f); });
            d.At(0.94f, pc =>
            {
                // BREAK FREE: KRAANG! pecahan batu kecil dari titik tertanam
                Vector3 g; GroundFront(pc, 1.0f, out g);
                Audio.Sfx("heavy_swing", pc.transform.position);
                MagicFx.Debri(g, 1.2f, 8, 0.8f);
                CombatFx.HitSpark(g + Vector3.up * 0.1f, new Color(1f, 0.8f, 0.5f), 1.2f, 16);
                ThirdPersonCamera.Shake(0.1f, 0.15f);
            });
            d.At(1.08f, pc =>
            {
                // Sweep 360: satu rotasi bersih setinggi pinggang-dada (bidang tebas melingkar)
                Audio.Sfx("heavy_swing", pc.transform.position);
                CombatFx.SweepArc(Chest(pc) - Vector3.up * 0.1f, pc.transform.rotation, 4.2f, 40f, 0.45f, 0f, 360f, 0.24f, 0.3f, new Color(0.95f, 0.22f, 0.08f, 0.95f), true);
                CombatFx.SweepArc(Chest(pc) - Vector3.up * 0.1f, pc.transform.rotation, 3.9f, 22f, 0.2f, 12f, 372f, 0.24f, 0.26f, new Color(1f, 0.9f, 0.75f, 0.95f), false);
            });
            d.At(1.32f, pc =>
            {
                // FURY RELEASE: cincin tekanan destruktif sangat rendah & tajam (pusat putih-oranye, pinggir crimson) sampai 8 m
                Vector3 c = Feet(pc);
                Audio.Sfx("heavy_swing", c);
                HitStop(0.05f);
                CombatFx.Flash(c + Vector3.up * 0.8f, Color.white, 12f, 3.5f, 0.15f);
                CombatFx.ShockRing(c, 8.5f, new Color(0.9f, 0.15f, 0.08f, 0.95f), 0.4f);
                CombatFx.ShockRing(c, 7f, new Color(1f, 0.55f, 0.2f, 0.9f), 0.34f);
                CombatFx.ShockRing(c, 5.5f, new Color(1f, 0.95f, 0.85f, 0.9f), 0.28f);
                for (int i = 0; i < 4; i++)
                {
                    float r = 2f + i * 2f;
                    CombatFx.Host.I.StartCoroutine(Late(0.055f * i, () => { if (pc != null) { CombatFx.Dust(Feet(pc) + RingPoint(r, 0f), 6); CombatFx.Dust(Feet(pc) + RingPoint(r, 120f), 6); CombatFx.Dust(Feet(pc) + RingPoint(r, 240f), 6); } }));
                }
                CombatFx.Burst(c + Vector3.up * 0.15f, new Color(0.9f, 0.5f, 0.2f), 40, 3.5f, 0.6f, 0.9f, 0.1f);
                MagicFx.Debri(c, 6f, 14, 0.9f);
                ThirdPersonCamera.Shake(0.26f, 0.3f);
            });
            d.At(1.5f, pc =>
            {
                // Enemy Break Reaction + Over-Brake (skid pendek)
                foreach (var m in Monster.All)
                {
                    if (m == null || m.IsDead) continue;
                    if (Vector3.Distance(m.transform.position, pc.transform.position) > 8.5f) continue;
                    CombatFx.HitSpark(m.HitPoint, new Color(1f, 0.45f, 0.2f), 1.3f, 16);
                    CombatFx.Flash(m.HitPoint, Crimson, 4f, 1.4f, 0.14f);
                }
                pc.DashTo(pc.transform.forward, 0.1f, 2f);
                CombatFx.Dust(Feet(pc), 8);
            });
            Add(d);
        }

        static Vector3 RingPoint(float r, float deg) { float a = deg * Mathf.Deg2Rad; return new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r; }

        // =====================================================================
        // Warlord — ULTIMATE Armageddon Slash  [e30_1_6]   (satu tebasan "world-cut", 8 ledakan beruntun tiap 0.6 dtk mengikuti jaringan retakan, radius 11 m)
        // Lock 0-0.20 | Draw 0.20-0.45 | Charge 0.45-0.72 | Compression 0.72-0.95 | Dead Silence 0.95-1.10 | SLASH 1.10-1.38 | World-Cut 1.38 | Line/Cracks 1.38-1.62 | Seal 1.62-1.85 | Explosions 1.85 + 0.6 x 7 | Ready 2.30
        // =====================================================================
        static Vector3 chainFwd = Vector3.forward;

        /// <summary>Titik tiap ledakan berantai (0 pusat, 1 depan, 2 kiri, 3 kanan, 4 belakang, 5 persilangan, 6 cincin luar, 7 finale).</summary>
        static Vector3 ChainPoint(int i, Vector3 c)
        {
            Vector3 f = chainFwd, r = Vector3.Cross(Vector3.up, f);
            switch (i)
            {
                case 1: return c + f * 4.2f;
                case 2: return c - r * 4.2f;
                case 3: return c + r * 4.2f;
                case 4: return c - f * 4.2f;
                default: return c;
            }
        }

        /// <summary>Musuh yang terkena ledakan ke-i (dipakai oleh damage di SkillExec.QuietGround).</summary>
        public static System.Collections.Generic.List<Monster> ChainTargets(int i, Vector3 c)
        {
            var res = new System.Collections.Generic.List<Monster>();
            foreach (var m in Monster.All)
            {
                if (m == null || m.IsDead) continue;
                Vector3 dd = m.transform.position - ChainPoint(i, c); dd.y = 0f;
                Vector3 dc = m.transform.position - c; dc.y = 0f;
                bool hit;
                if (i == 0) hit = dd.magnitude <= 4.8f + m.Radius;
                else if (i >= 1 && i <= 4) hit = dd.magnitude <= 3.8f + m.Radius;
                else if (i == 5) hit = dc.magnitude <= 5.6f + m.Radius;
                else if (i == 6) hit = dc.magnitude >= 4.5f && dc.magnitude <= 11f + m.Radius;
                else hit = dc.magnitude <= 11f + m.Radius;
                if (hit) res.Add(m);
            }
            return res;
        }

        static void Eruption(Vector3 p, float size, bool big)
        {
            CombatFx.Flash(p + Vector3.up * 0.8f, big ? Color.white : new Color(1f, 0.6f, 0.3f), big ? 18f : 12f, big ? 5f : 3f, big ? 0.3f : 0.2f);
            CombatFx.Flash(p + Vector3.up * 0.6f, Crimson, 14f, 2.5f, 0.35f);
            CombatFx.Dust(p, big ? 26 : 16);
            MagicFx.RockBurst(p, size, big ? 18 : 10);
            MagicFx.Debri(p, size * 1.2f, big ? 16 : 9, big ? 1.8f : 1.1f);
            CombatFx.ShockRing(p, size * 1.8f, new Color(1f, 0.35f, 0.12f, 0.9f), 0.35f);
            CombatFx.Burst(p + Vector3.up * 0.2f, Crimson, big ? 36 : 22, size * 0.7f, big ? 8f : 5f, 1f, 0.16f);
            CombatFx.Beam(p, p + Vector3.up * (big ? 6f : 4f), new Color(1f, 0.4f, 0.15f, 0.7f), big ? 1.0f : 0.6f, 0.3f);
            Audio.Sfx("heavy_swing", p);
            ThirdPersonCamera.Shake(big ? 0.3f : 0.14f, big ? 0.35f : 0.2f);
            foreach (var m in Monster.All)
                if (m != null && !m.IsDead && Vector3.Distance(m.transform.position, p) < size + 1.2f) CombatFx.HitSpark(m.HitPoint, new Color(1f, 0.5f, 0.25f), 1.4f, 16);
        }

        static void ArmageddonSlash()
        {
            var d = new ActionDef { id = "e30_1_6", dur = 2.3f, lockUntil = 2.0f, hitAt = 1.85f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            var lockp = PlayerAnimator.Mk(n, p => { p.arR = V(-4f, 40f, 12f); p.fR = V(-30f, 0f, 0f); p.arL = V(-8f, 28f, -10f); p.fL = V(-40f, 0f, 0f); p.body = V(8f, -16f, 0f); p.head = V(6f, 0f, 0f); p.lgL = V(-30f, 0f, 0f); p.lgR = V(36f, 0f, 0f); p.lift = -0.18f; });
            var draw = PlayerAnimator.Mk(lockp, p => { p.arR = V(20f, 90f, 14f); p.fR = V(-14f, 0f, 0f); p.arL = V(8f, 74f, -10f); p.fL = V(-22f, 0f, 0f); p.body = V(8f, -56f, 0f); p.head = V(6f, -24f, 0f); p.lgL = V(-18f, 0f, 0f); p.lgR = V(40f, 0f, 0f); p.lift = -0.22f; p.lunge = -0.1f; });
            var charge = PlayerAnimator.Mk(draw, p => { p.arR = V(24f, 94f, 14f); p.arL = V(10f, 78f, -10f); p.body = V(10f, -60f, 0f); p.lift = -0.26f; });
            var comp = PlayerAnimator.Mk(charge, p => { p.arR = V(-30f, 100f, 16f); p.fR = V(-40f, 0f, 0f); p.arL = V(-24f, 84f, -12f); p.fL = V(-44f, 0f, 0f); p.body = V(16f, -66f, 0f); p.head = V(8f, -28f, 0f); p.lgL = V(-40f, 0f, 0f); p.lgR = V(42f, 0f, 0f); p.lift = -0.32f; });
            var slash = PlayerAnimator.Mk(comp, p => { p.arR = V(-40f, -80f, 8f); p.fR = V(-4f, 0f, 0f); p.arL = V(-34f, -60f, -8f); p.fL = V(-6f, 0f, 0f); p.body = V(30f, 56f, 0f); p.head = V(10f, 30f, 0f); p.lunge = 0.5f; p.lift = -0.26f; p.lgL = V(-54f, 0f, 0f); p.lgR = V(36f, 0f, 0f); });
            var follow = PlayerAnimator.Mk(slash, p => { p.arR = V(-60f, -96f, 8f); p.arL = V(-52f, -76f, -8f); p.body = V(34f, 78f, 0f); p.lunge = 0.56f; });
            var brake = PlayerAnimator.Mk(follow, p => { p.body = V(22f, 30f, 0f); p.lunge = 0.3f; p.lgL = V(-40f, 0f, 0f); p.lgR = V(24f, 0f, 0f); p.arR = V(-30f, -50f, 8f); p.arL = V(-26f, -36f, -8f); });
            var recover = PlayerAnimator.Mk(brake, p => { p.arR = V(-12f, 34f, 12f); p.fR = V(-40f, 0f, 0f); p.arL = V(-14f, 22f, -10f); p.fL = V(-46f, 0f, 0f); p.body = V(10f, -4f, 0f); p.head = V(6f, 0f, 0f); p.lunge = 0.06f; p.lift = -0.12f; });
            d.Key(0f, n).Key(0.20f, lockp).Key(0.45f, draw).Key(0.72f, charge).Key(0.95f, comp).Key(1.10f, comp).Key(1.38f, slash).Key(1.62f, follow).Key(1.85f, brake).Key(2.10f, recover).Key(2.30f, n);

            d.At(0f, pc =>
            {
                GroundCenter = SkillExec.AimGroundFor(pc, 32f);
                GroundCenterSet = true;
                Vector3 to = GroundCenter - pc.transform.position; to.y = 0f;
                if (to.sqrMagnitude > 0.01f) { chainFwd = to.normalized; pc.transform.rotation = Quaternion.LookRotation(chainFwd); } else chainFwd = pc.transform.forward;
                CombatFx.Flash(Tip(pc), Crimson, 3f, 1f, 0.3f);
            });
            d.At(0.20f, pc => { Audio.Sfx("skill_cast", pc.transform.position); Converge(pc, new Color(0.9f, 0.25f, 0.1f), 0.25f, 1.6f); });
            d.At(0.45f, pc =>
            {
                // Armageddon Charge: energi dari kaki -> blade, tanah di kaki retak kecil
                CombatFx.Host.I.StartCoroutine(ConvergeBody(pc, new Color(1f, 0.4f, 0.12f), 1f, 0.27f));
                MagicFx.Crack(Feet(pc), 0.8f, 3f);
                CombatFx.Dust(Feet(pc), 6);
                CombatFx.Flash(Tip(pc), new Color(1f, 0.45f, 0.15f), 6f, 2f, 0.5f);
            });
            d.At(0.72f, pc =>
            {
                // Blade Compression: energi dipadatkan jadi garis tipis white-hot di mata pedang
                CombatFx.Beam(Tip(pc), Tip(pc) - pc.transform.forward * 0.1f + Vector3.up * 0.9f, new Color(1f, 0.97f, 0.9f, 0.9f), 0.08f, 0.35f);
                CombatFx.Flash(Tip(pc), Color.white, 8f, 3f, 0.35f);
            });
            d.At(0.95f, pc => { CombatFx.Flash(Tip(pc), Color.white, 5f, 3.2f, 0.18f); });       // Dead Silence: hanya mata pedang yang terang
            d.At(1.12f, pc => { pc.DashTo(pc.transform.forward, 0.18f, 5f); Audio.Sfx("heavy_swing", pc.transform.position); });
            d.At(1.20f, pc =>
            {
                CombatFx.SweepArc(Chest(pc) - Vector3.up * 0.2f, pc.transform.rotation * Quaternion.Euler(0f, 0f, 24f), 5.2f, 70f, 0.7f, 150f, -130f, 0.22f, 0.35f, new Color(0.95f, 0.18f, 0.08f, 0.95f), true);
                CombatFx.SweepArc(Chest(pc) - Vector3.up * 0.2f, pc.transform.rotation * Quaternion.Euler(0f, 0f, 24f), 4.9f, 34f, 0.3f, 150f, -130f, 0.22f, 0.3f, Color.white, false);
            });
            d.At(1.38f, pc =>
            {
                // World-Cut: garis tekanan panjang & rendah menyapu ke titik bidik 32 m
                Vector3 o = Feet(pc) + Vector3.up * 0.4f, c = GroundCenter;
                Audio.Sfx("heavy_swing", pc.transform.position);
                ThirdPersonCamera.Shake(0.2f, 0.25f);
                CombatFx.Host.I.StartCoroutine(BreakerWave(pc, c, 0.16f));
                CombatFx.Beam(o, new Vector3(c.x, o.y, c.z), new Color(1f, 0.95f, 0.85f, 0.9f), 0.14f, 0.3f);
            });
            d.At(1.5f, pc =>
            {
                // jaringan retakan bercabang di tanah radius 11 m, menyala dari pusat ke pinggir
                Vector3 c = GroundCenter, f = chainFwd, r = Vector3.Cross(Vector3.up, f);
                Vector3[] dirs = { f, -f, r, -r, (f + r).normalized, (f - r).normalized, (-f + r).normalized, (-f - r).normalized };
                for (int k = 0; k < dirs.Length; k++)
                    for (int j = 1; j <= 5; j++)
                    {
                        Vector3 pt = c + dirs[k] * (j * 2.1f);
                        float dl = 0.03f * j + 0.01f * k;
                        CombatFx.Host.I.StartCoroutine(Late(dl, () => { MagicFx.Crack(pt, 0.9f, 6f); CombatFx.Flash(pt + Vector3.up * 0.1f, new Color(1f, 0.4f, 0.12f), 3f, 1.2f, 0.4f); }));
                    }
                MagicFx.Crack(c, 2.5f, 6f);
            });
            d.At(1.7f, pc =>
            {
                // Armageddon Seal: retakan menyala crimson-orange, batu kecil melayang, debu naik
                Vector3 c = GroundCenter;
                CombatFx.Burst(c + Vector3.up * 0.3f, new Color(1f, 0.4f, 0.12f), 30, 6f, 1.4f, 1.6f, 0.1f);
                CombatFx.Dust(c, 20);
                MagicFx.Debri(c, 9f, 10, 0.3f);
                CombatFx.ShockRing(c, 11f, new Color(0.9f, 0.2f, 0.08f, 0.5f), 0.7f);
            });
            // 8 ledakan beruntun pada 1.85 + 0.6 i, berpindah mengikuti jaringan retakan
            for (int i = 0; i < 8; i++)
            {
                int k = i;
                float t = 1.85f + 0.6f * k;
                if (k >= 1 && k <= 4) d.At(t - 0.3f, pc => { Vector3 a = ChainPoint(k, GroundCenter); CombatFx.Beam(GroundCenter + Vector3.up * 0.15f, a + Vector3.up * 0.15f, new Color(1f, 0.55f, 0.2f, 0.8f), 0.3f, 0.3f); CombatFx.Flash(a + Vector3.up * 0.2f, new Color(1f, 0.5f, 0.15f), 5f, 2f, 0.3f); });
                d.At(t, pc =>
                {
                    Vector3 c = GroundCenter;
                    if (k == 5)
                    {
                        Eruption(c, 4.8f, false);       // Cross Rupture: ledakan lebih lebar di persimpangan
                    }
                    else if (k == 6)
                    {
                        // Ring Collapse: titik-titik luar radius 11 m pecah hampir bersamaan
                        for (int j = 0; j < 6; j++)
                        {
                            Vector3 pt = c + RingPoint(8.5f, j * 60f + 15f);
                            CombatFx.Host.I.StartCoroutine(Late(0.04f * j, () => Eruption(pt, 2.6f, false)));
                        }
                    }
                    else if (k == 7)
                    {
                        // FINALE: semua retakan white-hot, jeda 0.08 s, lalu BOOM dari banyak retakan menuju pusat
                        for (int j = 0; j < 8; j++)
                        {
                            Vector3 pt = c + RingPoint(2.5f + (j % 3) * 3f, j * 45f);
                            CombatFx.Flash(pt + Vector3.up * 0.2f, Color.white, 6f, 3f, 0.1f);
                        }
                        HitStop(0.08f);
                        CombatFx.Host.I.StartCoroutine(Late(0.09f, () =>
                        {
                            for (int j = 0; j < 6; j++)
                            {
                                Vector3 pt = c + RingPoint(7f - j * 0.9f, j * 60f);
                                CombatFx.Host.I.StartCoroutine(Late(0.03f * j, () => Eruption(pt, 3f, false)));
                            }
                            Eruption(c, 6f, true);
                        }));
                    }
                    else Eruption(ChainPoint(k, c), k == 0 ? 5f : 4f, k == 0);
                });
            }
            Add(d);
        }

        // =====================================================================
        // Dragon Blade — Dragon Fang  [e30_2_4]   (chamber -> lunge -> thrust -> wrist turn -> rip -> api terkompresi menembus 20 m)
        // Focus 0-0.16 | Chamber 0.16-0.36 | Ignition 0.36-0.55 | Compression 0.55-0.68 | LUNGE 0.68-0.86 | Bite 0.86-0.98 | RELEASE 0.98 | Rush 0.98-1.25 | Recoil -1.38 | Snap -1.58 | Ready 1.72
        // =====================================================================
        static readonly Color FireWhite = new Color(1f, 0.95f, 0.8f), FireOrange = new Color(1f, 0.5f, 0.15f), FireCrimson = new Color(0.9f, 0.15f, 0.06f);

        static void DragonFang()
        {
            var d = new ActionDef { id = "e30_2_4", dur = 1.72f, lockUntil = 1.45f, hitAt = 0.98f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            var focus = PlayerAnimator.Mk(n, p => { p.arR = V(-4f, 30f, 12f); p.fR = V(-30f, 0f, 0f); p.arL = V(-46f, 10f, -12f); p.fL = V(-60f, 0f, 0f); p.body = V(10f, -40f, 0f); p.head = V(4f, 32f, 0f); p.lgL = V(-22f, 0f, 0f); p.lgR = V(26f, 0f, 0f); p.lift = -0.12f; });
            var chamber = PlayerAnimator.Mk(focus, p => { p.arR = V(8f, 52f, 12f); p.fR = V(-70f, 0f, 0f); p.arL = V(-50f, 14f, -12f); p.body = V(12f, -56f, 0f); p.head = V(4f, 40f, 0f); p.lgL = V(-16f, 0f, 0f); p.lgR = V(34f, 0f, 0f); p.lift = -0.14f; });
            var ign = PlayerAnimator.Mk(chamber, p => { p.arR = V(8f, 54f, 24f); p.fR = V(-72f, 0f, 0f); p.body = V(12f, -56f, 0f); });
            var comp = PlayerAnimator.Mk(ign, p => { p.arL = V(10f, 44f, -12f); p.fL = V(-30f, 0f, 0f); p.body = V(22f, -52f, 0f); p.lgL = V(-30f, 0f, 0f); p.lgR = V(42f, 0f, 0f); p.lift = -0.24f; });
            var lunge = PlayerAnimator.Mk(comp, p => { p.arR = V(-84f, 4f, 8f); p.fR = V(-4f, 0f, 0f); p.arL = V(-30f, -30f, -12f); p.fL = V(-40f, 0f, 0f); p.body = V(30f, -24f, 0f); p.head = V(8f, 18f, 0f); p.lunge = 0.44f; p.lift = -0.2f; p.lgL = V(-50f, 0f, 0f); p.lgR = V(30f, 0f, 0f); });
            var bite = PlayerAnimator.Mk(lunge, p => { p.arR = V(-122f, -10f, 8f); p.fR = V(-14f, 0f, 0f); p.body = V(26f, -20f, 0f); p.lunge = 0.46f; });
            var recoil = PlayerAnimator.Mk(bite, p => { p.arR = V(-110f, -6f, 8f); p.fR = V(-44f, 0f, 0f); p.body = V(20f, -24f, 0f); p.lunge = 0.38f; });
            var snap = PlayerAnimator.Mk(recoil, p => { p.arR = V(-30f, 24f, 10f); p.fR = V(-50f, 0f, 0f); p.arL = V(-40f, 10f, -10f); p.body = V(12f, -12f, 0f); p.lunge = 0.2f; p.lift = -0.12f; });
            d.Key(0f, n).Key(0.16f, focus).Key(0.36f, chamber).Key(0.55f, ign).Key(0.68f, comp).Key(0.86f, lunge).Key(0.98f, bite).Key(1.18f, bite).Key(1.38f, recoil).Key(1.58f, snap).Key(1.72f, n);

            d.At(0.16f, pc => { Audio.Sfx("skill_cast", pc.transform.position); CombatFx.Beam(Chest(pc) + pc.transform.right * 0.3f, Tip(pc), new Color(1f, 0.5f, 0.15f, 0.6f), 0.06f, 0.3f); CombatFx.Burst(Tip(pc), FireOrange, 5, 0.2f, 0.4f, 0.5f, 0.05f); });
            d.At(0.36f, pc => { CombatFx.Host.I.StartCoroutine(BladeSpiral(pc, 0.19f)); });
            d.At(0.55f, pc => { CombatFx.Host.I.StartCoroutine(Late(0.0f, () => { })); Converge(pc, FireWhite, 0.12f, 1.0f); CombatFx.Flash(Tip(pc), FireWhite, 6f, 2.6f, 0.2f); });
            d.At(0.68f, pc => { pc.DashTo(pc.transform.forward, 0.18f, 7f); Audio.Sfx("swing2", pc.transform.position); CombatFx.Dust(Feet(pc), 6); });
            d.At(0.86f, pc =>
            {
                // wrist turn -> rip diagonal ke atas
                CombatFx.SweepArc(Chest(pc), pc.transform.rotation * Quaternion.Euler(0f, 0f, 30f), 2.4f, 14f, 0.09f, -35f, 35f, 0.1f, 0.18f, FireOrange, false);
            });
            d.At(0.98f, pc =>
            {
                // FANG RELEASE: dua garis api (rahang) menyatu menjadi satu lance-shaped fire slash
                Vector3 f = pc.transform.forward, r = pc.transform.right, t = Tip(pc);
                Audio.Sfx("heavy_swing", pc.transform.position);
                CombatFx.Beam(t + r * 0.7f + Vector3.up * 0.2f, t + f * 1.6f, new Color(1f, 0.55f, 0.18f, 0.95f), 0.22f, 0.12f);
                CombatFx.Beam(t - r * 0.7f - Vector3.up * 0.2f, t + f * 1.6f, new Color(1f, 0.55f, 0.18f, 0.95f), 0.22f, 0.12f);
                CombatFx.Flash(t, FireWhite, 9f, 3.4f, 0.15f);
                CombatFx.Host.I.StartCoroutine(FangRush(pc, 20f, 0.27f));
                ThirdPersonCamera.Shake(0.12f, 0.2f);
            });
            d.At(1.18f, pc => { CombatFx.Burst(Tip(pc), FireOrange, 14, 0.3f, 1.2f, 0.7f, 0.06f); CombatFx.Flash(Tip(pc), FireOrange, 5f, 1.6f, 0.2f); });
            d.At(1.38f, pc =>
            {
                Audio.Sfx("swing", pc.transform.position);       // SNAP: sisa api terputus jadi bara
                CombatFx.Flash(Tip(pc), FireWhite, 5f, 2f, 0.1f);
                CombatFx.Burst(Tip(pc), FireCrimson, 12, 0.4f, 0.8f, 0.8f, 0.05f);
            });
            Add(d);
        }

        /// <summary>Api rapat berputar spiral dari gagang menuju ujung bilah, di ujung muncul bentuk dua taring kecil.</summary>
        static IEnumerator BladeSpiral(PlayerCombat pc, float secs)
        {
            float t = 0f;
            while (t < secs && pc != null)
            {
                float k = t / secs;
                Vector3 grip = Chest(pc) + pc.transform.right * 0.3f + pc.transform.forward * 0.3f, tip = Tip(pc);
                Vector3 p = Vector3.Lerp(grip, tip, k);
                float ang = k * 18f;
                Vector3 off = (pc.transform.right * Mathf.Cos(ang) + Vector3.up * Mathf.Sin(ang)) * 0.12f;
                CombatFx.Burst(p + off, FireOrange, 3, 0.1f, 0.2f, 0.4f, 0.06f);
                CombatFx.Flash(p, FireOrange, 3f, 1.2f, 0.12f);
                yield return new WaitForSeconds(0.03f);
                t += 0.03f;
            }
            if (pc != null)
            {
                Vector3 tp = Tip(pc), f = pc.transform.forward, r = pc.transform.right;
                CombatFx.Beam(tp, tp + f * 0.4f + r * 0.22f, new Color(1f, 0.6f, 0.2f, 0.95f), 0.06f, 0.35f);       // dua taring api kecil
                CombatFx.Beam(tp, tp + f * 0.4f - r * 0.22f, new Color(1f, 0.6f, 0.2f, 0.95f), 0.06f, 0.35f);
            }
        }

        /// <summary>Lance api sempit & terkompresi (inti putih-panas, pinggir orange-crimson) merobek udara sejauh range.</summary>
        static IEnumerator FangRush(PlayerCombat pc, float range, float travel)
        {
            Vector3 f = pc.transform.forward; f.y = 0f; f.Normalize();
            Vector3 o = Tip(pc);
            int steps = 14;
            for (int i = 0; i < steps; i++)
            {
                Vector3 a = o + f * (range * i / steps);
                Vector3 b = a + f * (range / steps * 2.2f);
                CombatFx.Beam(a, b, new Color(1f, 0.96f, 0.85f, 1f), 0.16f, 0.14f);
                CombatFx.Beam(a, b, new Color(1f, 0.5f, 0.15f, 0.8f), 0.7f, 0.18f);
                CombatFx.Beam(a, b, new Color(0.9f, 0.15f, 0.06f, 0.4f), 1.4f, 0.22f);
                CombatFx.Burst(a, FireOrange, 2, 0.35f, 0.5f, 0.6f, 0.07f);        // bara tertinggal di belakang
                yield return new WaitForSeconds(travel / steps);
            }
        }

        /// <summary>Tiap musuh yang ditembus: bekas cakar/taring diagonal, tanpa ledakan.</summary>
        public static void FangHitFx(Vector3 pt, Vector3 fwd)
        {
            Audio.Sfx("swing2", pt);
            Vector3 r = Vector3.Cross(Vector3.up, fwd);
            CombatFx.Flash(pt, FireWhite, 5f, 2f, 0.1f);
            CombatFx.Beam(pt - r * 0.8f - Vector3.up * 0.5f, pt + r * 0.8f + Vector3.up * 0.5f, new Color(1f, 0.85f, 0.55f, 0.95f), 0.08f, 0.22f);
            CombatFx.Beam(pt - r * 0.5f - Vector3.up * 0.55f, pt + r * 0.5f + Vector3.up * 0.55f, new Color(1f, 0.5f, 0.15f, 0.8f), 0.07f, 0.22f);
            CombatFx.HitSpark(pt, FireOrange, 1.1f, 14);
        }

        // =====================================================================
        // Dragon Blade — Wyrm Dash  [e30_2_5]   (9 m, dua lilitan kiri-kanan 30-50 cm, dua irisan, trail api berkelok seperti wyrm)
        // Stance 0-0.15 | Coil 0.15-0.32 | Compression 0.32-0.46 | Kick 0.46-0.54 | DASH 0.54-0.72 | Cut1 0.72-0.90 | Reverse 0.90-1.08 | Cut2 1.08-1.27 | Surge 1.27-1.42 | Breakthrough 1.42-1.60 | Exit 1.60-1.72 | Tail 1.72-1.90 | Collapse -2.08 | Ready 2.20
        // =====================================================================
        static readonly System.Collections.Generic.List<Vector3> wyrmPath = new System.Collections.Generic.List<Vector3>();

        static void WyrmDash()
        {
            var d = new ActionDef { id = "e30_2_5", dur = 2.2f, lockUntil = 1.85f, hitAt = 0.54f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            var stance = PlayerAnimator.Mk(n, p => { p.arR = V(24f, 66f, 12f); p.fR = V(-12f, 0f, 0f); p.arL = V(-46f, 6f, -12f); p.fL = V(-60f, 0f, 0f); p.body = V(26f, -14f, 0f); p.head = V(4f, 0f, 0f); p.lgL = V(-24f, 0f, 0f); p.lgR = V(32f, 0f, 0f); p.lift = -0.18f; });
            var coil = PlayerAnimator.Mk(stance, p => { p.body = V(26f, -40f, 0f); p.head = V(4f, 22f, 0f); });
            var comp = PlayerAnimator.Mk(coil, p => { p.arR = V(30f, 72f, 12f); p.arL = V(-30f, 12f, -10f); p.body = V(34f, -30f, 0f); p.lgL = V(-34f, 0f, 0f); p.lgR = V(42f, 0f, 0f); p.lift = -0.26f; });
            var kick = PlayerAnimator.Mk(comp, p => { p.body = V(40f, -20f, 0f); p.lunge = 0.3f; p.lgL = V(-44f, 0f, 0f); p.lgR = V(38f, 0f, 0f); });
            var dash = PlayerAnimator.Mk(kick, p => { p.arL = V(-50f, -16f, -14f); p.body = V(46f, 12f, 0f); p.lunge = 0.5f; p.lift = -0.2f; });
            var cut1 = PlayerAnimator.Mk(dash, p => { p.arR = V(-36f, -52f, 8f); p.body = V(40f, 36f, 0f); p.lunge = 0.5f; });
            var rev = PlayerAnimator.Mk(cut1, p => { p.arR = V(24f, 66f, 12f); p.body = V(44f, -24f, 0f); });
            var cut2 = PlayerAnimator.Mk(rev, p => { p.arR = V(-100f, -40f, 8f); p.body = V(40f, 34f, 0f); p.lunge = 0.5f; });
            var surge = PlayerAnimator.Mk(cut2, p => { p.arR = V(-4f, 40f, 12f); p.fR = V(-40f, 0f, 0f); p.body = V(44f, -2f, 0f); p.lunge = 0.56f; });
            var brk = PlayerAnimator.Mk(surge, p => { p.arR = V(-30f, 30f, 10f); p.body = V(48f, 0f, 0f); p.lunge = 0.6f; });
            var tail = PlayerAnimator.Mk(brk, p => { p.arR = V(-30f, -70f, 8f); p.body = V(24f, 90f, 0f); p.lunge = 0.2f; p.lgL = V(-40f, 0f, 0f); p.lgR = V(24f, 0f, 0f); });
            var ready = PlayerAnimator.Mk(tail, p => { p.arR = V(-26f, 22f, 10f); p.fR = V(-46f, 0f, 0f); p.arL = V(-36f, 8f, -10f); p.body = V(8f, -6f, 0f); p.lunge = 0.04f; p.lift = -0.08f; });
            d.Key(0f, n).Key(0.15f, stance).Key(0.32f, coil).Key(0.46f, comp).Key(0.54f, kick).Key(0.72f, dash).Key(0.90f, cut1).Key(1.08f, rev).Key(1.27f, cut2).Key(1.42f, surge).Key(1.60f, brk).Key(1.90f, tail).Key(2.20f, ready);

            d.At(0.15f, pc => { Audio.Sfx("skill_cast", pc.transform.position); CombatFx.Host.I.StartCoroutine(FlameCoil(pc, 0.17f)); });
            d.At(0.32f, pc => { CombatFx.Flash(Chest(pc) - pc.transform.forward * 0.3f, FireOrange, 5f, 1.6f, 0.3f); });
            d.At(0.46f, pc => { Audio.Sfx("swing2", pc.transform.position); CombatFx.Dust(Feet(pc) - pc.transform.forward * 0.3f, 6); CombatFx.Burst(Feet(pc), FireOrange, 8, 0.4f, 0.6f, 0.5f, 0.05f); });
            // dash 9 m dengan kelokan 30-50 cm: kiri -> kanan -> lurus -> burst -> exit
            d.At(0.54f, pc =>
            {
                wyrmPath.Clear(); wyrmStartRot = pc.transform.rotation;
                pc.Invuln(0.2f);
                pc.DashTo(Quaternion.AngleAxis(-10f, Vector3.up) * pc.transform.forward, 0.36f, 7.5f);
                CombatFx.Host.I.StartCoroutine(WyrmTrail(pc, 1.4f));
                Audio.Sfx("dodge", pc.transform.position);
            });
            d.At(0.72f, pc => { Audio.Sfx("swing", pc.transform.position); CombatFx.SweepArc(Chest(pc) - Vector3.up * 0.5f, pc.transform.rotation * Quaternion.Euler(0f, 0f, 22f), 2.8f, 22f, 0.16f, 60f, -50f, 0.14f, 0.24f, new Color(1f, 0.5f, 0.15f, 0.95f), true); });
            d.At(0.90f, pc => { pc.DashTo(Quaternion.AngleAxis(10f, Vector3.up) * wyrmStartRot * Vector3.forward, 0.37f, 7.5f); });
            d.At(1.08f, pc => { Audio.Sfx("swing2", pc.transform.position); CombatFx.SweepArc(Chest(pc), pc.transform.rotation * Quaternion.Euler(0f, 0f, -26f), 2.8f, 22f, 0.16f, -60f, 55f, 0.14f, 0.24f, new Color(1f, 0.45f, 0.12f, 0.95f), true); });
            d.At(1.27f, pc =>
            {
                pc.DashTo(wyrmStartRot * Vector3.forward, 0.15f, 7.5f);
                // Dragon Surge: kurva kiri-kanan menyatu di belakang tubuh; siluet rahang abstrak sesaat
                Vector3 b = Chest(pc) - pc.transform.forward * 1.2f;
                CombatFx.Beam(b - pc.transform.right * 0.6f, Chest(pc), new Color(1f, 0.5f, 0.15f, 0.8f), 0.4f, 0.2f);
                CombatFx.Beam(b + pc.transform.right * 0.6f, Chest(pc), new Color(1f, 0.5f, 0.15f, 0.8f), 0.4f, 0.2f);
            });
            d.At(1.42f, pc => { pc.DashTo(wyrmStartRot * Vector3.forward, 0.18f, 10f); CombatFx.Flash(Tip(pc), FireWhite, 6f, 2.4f, 0.2f); });
            d.At(1.6f, pc => { pc.DashTo(wyrmStartRot * Vector3.forward, 0.12f, 4f); CombatFx.Dust(Feet(pc), 8); CombatFx.ShockRing(Feet(pc), 1.8f, new Color(1f, 0.5f, 0.15f, 0.5f), 0.2f); });
            d.At(1.72f, pc =>
            {
                // Wyrm Tail Sweep: setengah putaran kecil 90, pedang menyusul seperti ekor naga, api masuk ke bilah
                CombatFx.SweepArc(Chest(pc), pc.transform.rotation, 3.0f, 20f, 0.2f, 20f, 100f, 0.18f, 0.3f, new Color(1f, 0.5f, 0.15f, 0.8f), false);
                CombatFx.Flash(Tip(pc), FireOrange, 6f, 2f, 0.25f);
            });
            d.At(1.9f, pc => { CombatFx.Host.I.StartCoroutine(WyrmCollapse(0.18f)); });
            Add(d);
        }

        static Quaternion wyrmStartRot = Quaternion.identity;

        static IEnumerator FlameCoil(PlayerCombat pc, float secs)
        {
            float t = 0f;
            while (t < secs && pc != null)
            {
                float k = t / secs;
                Vector3 a = Tip(pc), b = Chest(pc) + Vector3.up * 0.35f, c = Chest(pc) - pc.transform.forward * 0.3f, e = Feet(pc) + Vector3.up * 0.4f - pc.transform.forward * 0.3f;
                Vector3 p = k < 0.33f ? Vector3.Lerp(a, b, k * 3f) : (k < 0.66f ? Vector3.Lerp(b, c, (k - 0.33f) * 3f) : Vector3.Lerp(c, e, (k - 0.66f) * 3f));
                CombatFx.Burst(p, FireOrange, 3, 0.12f, 0.2f, 0.5f, 0.07f);
                CombatFx.Flash(p, FireOrange, 3f, 1.2f, 0.12f);
                yield return new WaitForSeconds(0.03f);
                t += 0.03f;
            }
        }

        /// <summary>Trail api panjang berkelok sepanjang lintasan (siluet wyrm), titik jalur disimpan untuk Flame Collapse.</summary>
        static IEnumerator WyrmTrail(PlayerCombat pc, float secs)
        {
            float t = 0f; Vector3 prev = Chest(pc) - Vector3.up * 0.3f;
            while (t < secs && pc != null)
            {
                Vector3 cur = Chest(pc) - Vector3.up * 0.3f;
                if ((cur - prev).sqrMagnitude > 0.01f)
                {
                    CombatFx.Beam(prev, cur, new Color(1f, 0.92f, 0.75f, 0.9f), 0.12f, 0.3f);
                    CombatFx.Beam(prev, cur, new Color(1f, 0.45f, 0.12f, 0.75f), 0.55f, 0.45f);
                    CombatFx.Beam(prev, cur, new Color(0.85f, 0.12f, 0.05f, 0.35f), 1.0f, 0.55f);
                    wyrmPath.Add(cur);
                    if (wyrmPath.Count % 3 == 0) CombatFx.Burst(cur, FireCrimson, 1, 0.3f, 0.4f, 0.8f, 0.06f);
                    prev = cur;
                }
                yield return new WaitForSeconds(0.04f);
                t += 0.04f;
            }
        }

        /// <summary>Flame Collapse: trail padam dari titik awal menuju karakter, bara kecil tersisa.</summary>
        static IEnumerator WyrmCollapse(float secs)
        {
            int n = wyrmPath.Count;
            if (n < 2) yield break;
            int per = Mathf.Max(1, n / 6);
            for (int i = 0; i < n; i += per)
            {
                CombatFx.Burst(wyrmPath[i], FireOrange, 3, 0.4f, 0.5f, 0.8f, 0.05f);
                yield return new WaitForSeconds(secs / 6f);
            }
        }

        public static void WyrmHitFx(Vector3 pt)
        {
            Audio.Sfx("swing2", pt);
            CombatFx.Flash(pt, FireWhite, 5f, 2f, 0.1f);
            CombatFx.HitSpark(pt, FireOrange, 1.3f, 16);
            CombatFx.Burst(pt, FireCrimson, 8, 0.5f, 0.8f, 0.6f, 0.06f);
        }

        // =====================================================================
        // Dragon Blade — Dragon King Slash  [e30_2_6]  ULTIMATE (3.2 s)
        // Stance 0-.18 | Focus -.38 | Invocation -.62 | Chamber -.82 | Silence -.96 | KING'S STEP -1.18 | Ascending ↗ -1.36 | Dragon Rise -1.58 | Turn 180 -1.78
        // Alignment -1.94 | KING SLASH ↘ -2.18 | Impact (hit-stop, X) -2.34 | Pass -2.58 | Blade Return -2.82 | Dissolution -3.05 | Ready 3.2
        // =====================================================================
        static Vector3 kingFwd = Vector3.forward, kingOrigin;

        static void DragonKingSlash()
        {
            var d = new ActionDef { id = "e30_2_6", dur = 3.2f, lockUntil = 2.82f, hitAt = 2.28f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            var stance = PlayerAnimator.Mk(n, p => { p.arR = V(18f, 20f, 10f); p.fR = V(-8f, 0f, 0f); p.arL = V(-50f, 6f, -10f); p.fL = V(-70f, 0f, 0f); p.body = V(2f, -6f, 0f); p.lgL = V(-12f, 0f, 0f); p.lgR = V(16f, 0f, 0f); p.lift = -0.05f; });
            var focus = PlayerAnimator.Mk(stance, p => { p.arL = V(-40f, 14f, -8f); p.fL = V(-40f, 0f, 0f); p.head = V(10f, 0f, 0f); p.body = V(4f, -6f, 0f); });
            var invoke = PlayerAnimator.Mk(focus, p => { p.arL = V(-20f, 24f, -8f); p.fL = V(-14f, 0f, 0f); p.arR = V(14f, 26f, 10f); });
            var chamber = PlayerAnimator.Mk(invoke, p => { p.arR = V(28f, 70f, 12f); p.fR = V(-20f, 0f, 0f); p.arL = V(-60f, -18f, -12f); p.fL = V(-30f, 0f, 0f); p.body = V(14f, -38f, 0f); p.lgL = V(-26f, 0f, 0f); p.lgR = V(30f, 0f, 0f); p.lift = -0.16f; });
            var step = PlayerAnimator.Mk(chamber, p => { p.lunge = 0.45f; p.lgL = V(-44f, 0f, 0f); p.lgR = V(34f, 0f, 0f); p.body = V(22f, -34f, 0f); p.lift = -0.18f; });
            var asc = PlayerAnimator.Mk(step, p => { p.arR = V(-120f, -20f, 8f); p.fR = V(-20f, 0f, 0f); p.arL = V(-20f, -10f, -10f); p.body = V(10f, 30f, 0f); p.lunge = 0.5f; });
            var rise = PlayerAnimator.Mk(asc, p => { p.arR = V(-160f, 10f, 8f); p.fR = V(-30f, 0f, 0f); p.body = V(-8f, 50f, 0f); p.lunge = 0.3f; p.lift = 0f; });
            var turn = PlayerAnimator.Mk(rise, p => { p.arR = V(-150f, 60f, 8f); p.body = V(0f, 190f, 0f); p.lunge = 0.2f; p.lift = -0.1f; });
            var align = PlayerAnimator.Mk(turn, p => { p.arR = V(-170f, 80f, 8f); p.fR = V(-40f, 0f, 0f); p.arL = V(-90f, 20f, -10f); p.body = V(6f, 190f, -10f); p.lgL = V(-34f, 0f, 0f); p.lgR = V(10f, 0f, 0f); p.lift = -0.2f; });
            var slash = PlayerAnimator.Mk(align, p => { p.arR = V(10f, -50f, 8f); p.fR = V(-12f, 0f, 0f); p.arL = V(-10f, -30f, -10f); p.body = V(38f, 160f, 8f); p.lunge = 0.5f; p.lift = -0.18f; });
            var pass = PlayerAnimator.Mk(slash, p => { p.arR = V(30f, -30f, 8f); p.body = V(30f, 170f, 0f); p.lunge = 0.35f; });
            var ret = PlayerAnimator.Mk(pass, p => { p.arR = V(10f, 10f, 10f); p.arL = V(-40f, 6f, -10f); p.body = V(14f, 180f, 0f); p.lunge = 0.15f; });
            var diss = PlayerAnimator.Mk(ret, p => { p.arR = V(8f, 8f, 10f); p.body = V(4f, 180f, 0f); p.lunge = 0.05f; p.lift = 0f; });
            var ready = PlayerAnimator.Mk(diss, p => { p.arR = V(-26f, 22f, 10f); p.fR = V(-46f, 0f, 0f); p.arL = V(-36f, 8f, -10f); p.body = V(8f, 180f, 0f); p.lunge = 0.04f; p.lift = -0.08f; });
            d.Key(0f, n).Key(0.18f, stance).Key(0.38f, focus).Key(0.62f, invoke).Key(0.82f, chamber).Key(0.96f, chamber).Key(1.18f, step)
             .Key(1.36f, asc).Key(1.58f, rise).Key(1.78f, turn).Key(1.94f, align).Key(2.18f, slash).Key(2.58f, pass).Key(2.82f, ret).Key(3.05f, diss).Key(3.2f, ready);

            d.At(0f, pc => { kingOrigin = pc.transform.position; kingFwd = pc.transform.forward; kingFwd.y = 0f; kingFwd.Normalize(); pc.DashTo(-pc.transform.forward, 0.18f, 1.2f); });
            d.At(0.18f, pc => { CombatFx.Host.I.StartCoroutine(KingEmbers(pc, 0.2f, false)); });
            // Invocation: kaki -> pinggang -> dada -> bahu -> lengan -> pedang, crimson -> orange -> emas-putih
            d.At(0.38f, pc => { Audio.Sfx("skill_cast", pc.transform.position); CombatFx.Host.I.StartCoroutine(KingInvocation(pc, 0.24f)); });
            d.At(0.62f, pc => { CombatFx.Flash(Tip(pc), FireWhite, 4f, 0.8f, 0.2f); });
            d.At(0.82f, pc => { CombatFx.Beam(Tip(pc) - pc.transform.forward * 0.6f, Tip(pc), new Color(1f, 0.95f, 0.8f, 0.9f), 0.06f, 0.2f); });
            d.At(0.96f, pc => { Audio.Sfx("dodge", pc.transform.position); pc.DashTo(kingFwd, 0.22f, 7f); CombatFx.Dust(Feet(pc), 5); });
            d.At(1.18f, pc =>
            {
                Audio.Sfx("swing", pc.transform.position);
                CombatFx.SweepArc(Chest(pc) - Vector3.up * 0.5f, pc.transform.rotation * Quaternion.Euler(0f, 0f, 24f), 3.0f, 22f, 0.14f, 50f, -60f, 0.14f, 0.24f, new Color(1f, 0.92f, 0.7f, 1f), true);
            });
            d.At(1.36f, pc => { CombatFx.Host.I.StartCoroutine(KingSpirit(pc, 0.7f, true)); });
            d.At(1.58f, pc => { pc.DashTo(Quaternion.AngleAxis(180f, Vector3.up) * kingFwd, 0.01f, 0.1f); });
            d.At(1.78f, pc => { SkillSilhouette.DragonHover(pc.transform.position - Vector3.up * 0.9f, kingFwd, 0.5f, new SkillSilhouette.Col { m = new Color(1f, 0.5f, 0.12f, 1f), h = new Color(1f, 0.95f, 0.75f, 1f), k = new Color(0.85f, 0.1f, 0.05f, 1f) }, 0.9f); });
            d.At(1.94f, pc => { CombatFx.Flash(Chest(pc) - kingFwd, FireOrange, 6f, 2.8f, 0.3f); });
            d.At(2.18f, pc =>
            {
                Audio.Sfx("swing2", pc.transform.position);
                pc.DashTo(-kingFwd, 0.1f, 1f);
                CombatFx.SweepArc(Chest(pc), pc.transform.rotation * Quaternion.Euler(0f, 0f, -35f), 3.4f, 26f, 0.2f, -80f, 70f, 0.16f, 0.3f, new Color(1f, 0.55f, 0.15f, 1f), true);
            });
            // hit-stop 0.1 s sebelum impact (dipanggil sedikit sebelum hitAt)
            d.At(2.2f, pc => { HitStop(0.1f); });
            d.At(2.28f, pc => { ThirdPersonCamera.Shake(0.55f, 0.5f); CombatFx.Host.I.StartCoroutine(KingWave(pc, 32f)); });
            d.At(2.34f, pc => { pc.DashTo(-kingFwd, 0.24f, 1.4f); });
            d.At(2.58f, pc => { CombatFx.Burst(Tip(pc), FireCrimson, 10, 0.8f, 1.2f, 0.9f, 0.05f); CombatFx.Flash(Tip(pc), FireOrange, 3f, 1f, 0.2f); });
            d.At(2.82f, pc => { CombatFx.Host.I.StartCoroutine(KingEmbers(pc, 0.4f, true)); });
            Add(d);
        }

        static IEnumerator KingEmbers(PlayerCombat pc, float secs, bool fromBlade)
        {
            float t = 0f;
            while (t < secs && pc != null)
            {
                Vector3 p = fromBlade ? Tip(pc) : Feet(pc) + Random.insideUnitSphere * 0.5f;
                CombatFx.Burst(p, FireOrange, 2, 0.3f, 0.7f, 0.7f, 0.04f);
                yield return new WaitForSeconds(0.05f);
                t += 0.05f;
            }
        }

        static IEnumerator KingInvocation(PlayerCombat pc, float secs)
        {
            float t = 0f;
            while (t < secs && pc != null)
            {
                float k = t / secs;
                Vector3 p = Vector3.Lerp(Feet(pc), Tip(pc), k);
                Color c = k < 0.4f ? FireCrimson : (k < 0.75f ? FireOrange : new Color(1f, 0.9f, 0.55f, 1f));
                CombatFx.Burst(p, c, 3, 0.25f, 0.5f, 0.5f, 0.06f);
                CombatFx.Flash(p, c, 3f, 1f, 0.1f);
                yield return new WaitForSeconds(0.03f);
                t += 0.03f;
            }
        }

        /// <summary>Roh naga samar: terbentuk dari jejak tebasan, naik, lalu melengkung di belakang pendekar. Bukan summon penuh.</summary>
        static IEnumerator KingSpirit(PlayerCombat pc, float secs, bool rising)
        {
            float t = 0f; Vector3 prev = Tip(pc);
            while (t < secs && pc != null)
            {
                Vector3 cur = Tip(pc) + Vector3.up * (t * 1.5f);
                CombatFx.Beam(prev, cur, new Color(1f, 0.9f, 0.6f, 0.7f), 0.2f, 0.3f);
                CombatFx.Beam(prev, cur, new Color(1f, 0.45f, 0.1f, 0.55f), 0.8f, 0.5f);
                prev = cur;
                yield return new WaitForSeconds(0.04f);
                t += 0.04f;
            }
        }

        static IEnumerator KingWave(PlayerCombat pc, float range)
        {
            if (pc == null) yield break;
            // tebasan terakhir melahirkan AURA NAGA yang terbang lurus ke depan dan menghancurkan area di jalurnya
            SkillSilhouette.DragonCharge(kingOrigin, kingFwd, range, new SkillSilhouette.Col { m = new Color(1f, 0.5f, 0.12f, 1f), h = new Color(1f, 0.95f, 0.75f, 1f), k = new Color(0.85f, 0.1f, 0.05f, 1f) }, 1.1f, "Swordsman");
            yield break;
        }

        /// <summary>White-hot X (↗ + ↘) di titik kena; api mengikuti jalur tebasan, bukan ledakan bulat.</summary>
        public static void KingHitFx(Vector3 pt, Vector3 fwd)
        {
            Audio.Sfx("swing2", pt);
            Vector3 r = Vector3.Cross(Vector3.up, fwd);
            Color w = new Color(1f, 0.97f, 0.85f, 1f);
            CombatFx.Beam(pt - r * 1.6f - Vector3.up * 1.2f, pt + r * 1.6f + Vector3.up * 1.2f, w, 0.18f, 0.35f);
            CombatFx.Beam(pt - r * 1.6f + Vector3.up * 1.2f, pt + r * 1.6f - Vector3.up * 1.2f, w, 0.18f, 0.35f);
            CombatFx.Beam(pt - r * 1.8f - Vector3.up * 1.3f, pt + r * 1.8f + Vector3.up * 1.3f, new Color(1f, 0.45f, 0.1f, 0.7f), 0.7f, 0.5f);
            CombatFx.Beam(pt - r * 1.8f + Vector3.up * 1.3f, pt + r * 1.8f - Vector3.up * 1.3f, new Color(1f, 0.45f, 0.1f, 0.7f), 0.7f, 0.5f);
            CombatFx.Flash(pt, FireWhite, 7f, 2.4f, 0.15f);
            CombatFx.HitSpark(pt, FireOrange, 1.6f, 20);
            CombatFx.Burst(pt, FireCrimson, 10, 0.6f, 1.0f, 0.7f, 0.07f);
        }
    }
}
