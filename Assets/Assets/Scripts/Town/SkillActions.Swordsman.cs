using System.Collections;
using System.Linq;
using UnityEngine;

namespace HunterGuild
{
    public static partial class SkillAction
    {
        static readonly Color Gold = new Color(1f, 0.72f, 0.22f), GoldHot = new Color(1f, 0.93f, 0.62f);

        static void RegisterAll()
        {
            Cleave();
            RegisterArcher();
            RageStrike();
            ExecutionerArc();
            PerfectParry();
            ShieldBash();
            Bulwark();
            BlinkStrike();
            RiposteSkill();
            PhantomFlurry();
            RegisterEvolutions();
            RegisterPremium();
        }

        // =====================================================================
        // Swordsman — Offense — Cleave (tebasan lebar)  [sw_off_4]
        // Ready 0-0.15 | Step 0.15-0.30 | Initial Swing 0.30-0.45 | Main 0.45-0.70 | Impact 0.70-0.82 | Follow 0.82-1.10 | Recovery 1.10-1.40
        // =====================================================================
        static void Cleave()
        {
            var d = new ActionDef { id = "sw_off_4", dur = 1.4f, lockUntil = 1.1f, hitAt = 0.58f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            // Ready: pusat tubuh turun, kaki depan ditekuk, pedang ditarik ke belakang, bahu & pinggang memutar
            var ready = PlayerAnimator.Mk(n, p => { p.arR = V(-70f, 78f, 10f); p.fR = V(-30f, 0f, 0f); p.arL = V(-30f, 0f, -30f); p.body = V(10f, 38f, 0f); p.head = V(0f, -26f, 0f); p.lgL = V(-22f, 0f, 0f); p.lgR = V(16f, 0f, 0f); p.lift = -0.1f; });
            // Step Forward: hentakan kaki depan, berat pindah ke depan, pedang tetap di belakang
            var step = PlayerAnimator.Mk(ready, p => { p.lunge = 0.28f; p.lift = -0.04f; p.lgL = V(-42f, 0f, 0f); p.lgR = V(24f, 0f, 0f); p.body = V(14f, 40f, 0f); });
            // Initial Swing: pinggang berputar lebih dulu, lalu bahu dan tangan
            var init = PlayerAnimator.Mk(step, p => { p.body = V(8f, 8f, 0f); p.head = V(0f, -6f, 0f); p.arR = V(-86f, 46f, 0f); p.fR = V(-12f, 0f, 0f); p.lunge = 0.3f; p.lift = 0f; });
            // Main Swing: seluruh badan berputar, arc lebar ~170 derajat
            var main = PlayerAnimator.Mk(init, p => { p.body = V(6f, -52f, 0f); p.head = V(0f, 40f, 0f); p.arR = V(-88f, -82f, 0f); p.fR = V(-4f, 0f, 0f); p.arL = V(-40f, 0f, -52f); p.lunge = 0.36f; p.lgL = V(-34f, 0f, 0f); p.lgR = V(24f, 0f, 0f); });
            // Impact: tertahan sejenak seperti menghantam benda berat
            var hit = PlayerAnimator.Mk(main, p => { p.body = V(10f, -54f, 0f); p.lunge = 0.38f; p.lift = -0.05f; p.arR = V(-86f, -86f, 0f); });
            // Follow Through: pedang terus lewat, kaki belakang bergeser menjaga keseimbangan
            var follow = PlayerAnimator.Mk(hit, p => { p.body = V(4f, -66f, 0f); p.head = V(0f, 44f, 0f); p.arR = V(-76f, -112f, 0f); p.lunge = 0.32f; p.lift = 0f; p.lgL = V(-28f, 0f, 0f); p.lgR = V(28f, 0f, 0f); });
            d.Key(0f, n).Key(0.15f, ready).Key(0.30f, step).Key(0.45f, init).Key(0.70f, main).Key(0.82f, hit).Key(1.10f, follow).Key(1.40f, n);

            // Ready: glow tipis pada bilah + sedikit partikel cahaya
            d.At(0f, pc =>
            {
                Vector3 tip = Tip(pc);
                CombatFx.Flash(tip, Gold, 3.5f, 0.9f, 0.3f);
                CombatFx.Burst(tip, GoldHot, 8, 0.25f, 1.2f, 0.5f, 0.08f);
                Audio.Sfx("skill_cast", pc.transform.position);
            });
            // Step Forward: langkah agresif ke target, debu kecil + jejak tipis
            d.At(0.15f, pc =>
            {
                pc.DashTo(pc.transform.forward, 0.15f, 9f);
                CombatFx.Dust(Feet(pc), 8);
                CombatFx.Burst(Chest(pc) - pc.transform.forward * 0.3f, new Color(1f, 0.8f, 0.4f, 0.6f), 6, 0.2f, 0.4f, 0.35f, 0.1f);
            });
            // Initial Swing: garis cahaya tipis mulai melengkung dari ujung pedang
            d.At(0.30f, pc =>
            {
                Audio.Sfx("swing", pc.transform.position);
                CombatFx.SweepArc(Chest(pc), pc.transform.rotation, 3.4f, 28f, 0.14f, 120f, 62f, 0.15f, 0.12f, new Color(1f, 0.85f, 0.5f, 0.8f), false);
            });
            // Main Swing: arc emas/oranye lebar, depan lebih terang, trail panjang + spark
            d.At(0.45f, pc =>
            {
                Audio.Sfx("heavy_swing", pc.transform.position);
                Vector3 c = Chest(pc);
                CombatFx.SweepArc(c, pc.transform.rotation, 4.3f, 100f, 0.42f, 92f, -92f, 0.25f, 0.2f, new Color(1f, 0.6f, 0.14f, 0.95f), true);
                CombatFx.SweepArc(c + Vector3.up * 0.05f, pc.transform.rotation, 4.0f, 55f, 0.24f, 92f, -92f, 0.24f, 0.16f, new Color(1f, 0.95f, 0.7f, 1f), false);
                CombatFx.Flash(c + pc.transform.forward * 1.5f, Gold, 8f, 1.6f, 0.25f);
            });
            // Impact: flash, percikan tajam, gelombang kejut setengah lingkaran, debu, pecahan, retakan bila kena tanah
            d.At(0.70f, pc =>
            {
                Vector3 f = pc.transform.forward;
                foreach (var m in InFront(pc, 4.6f, 95f))
                {
                    CombatFx.HitSpark(m.HitPoint, GoldHot, 1.5f, 22);
                    CombatFx.Flash(m.HitPoint, Gold, 6f, 1.8f, 0.15f);
                }
                Vector3 g; bool ground = GroundFront(pc, 2.2f, out g);
                CombatFx.HalfWave(Feet(pc) + f * 0.8f, f, 4.4f, new Color(1f, 0.7f, 0.25f, 0.95f), 0.38f);
                CombatFx.Dust(g, 14);
                MagicFx.Debri(g, 1.8f, 8, 0.8f);
                if (ground) MagicFx.Crack(g, 1.5f, 2.5f);
                ThirdPersonCamera.Shake(0.14f, 0.18f);
            });
            // Follow Through: arc memudar dari belakang ke depan, spark kecil dan partikel cahaya padam perlahan
            d.At(0.82f, pc =>
            {
                CombatFx.SweepArc(Chest(pc), pc.transform.rotation, 4.1f, 40f, 0.12f, -92f, -128f, 0.28f, 0.22f, new Color(1f, 0.75f, 0.35f, 0.5f), true);
                CombatFx.Burst(Chest(pc) + pc.transform.forward * 1.6f - pc.transform.right * 1.6f, GoldHot, 10, 0.5f, 1.4f, 0.8f, 0.09f);
            });
            // Recovery: debu terakhir, tanpa glow besar
            d.At(1.10f, pc => CombatFx.Dust(Feet(pc), 4));
            Add(d);
        }

        // =====================================================================
        // Swordsman — Offense — Rage Strike (hantaman vertikal berat)  [sw_off_5]
        // Stance 0-0.20 | Pull Back 0.20-0.40 | Raise 0.40-0.60 | Explosive Step 0.60-0.70 | STRIKE 0.70-0.90 | IMPACT 0.90-1.02 | Heavy Follow 1.02-1.30 | Recovery 1.30-1.70
        // =====================================================================
        static readonly Color Rage = new Color(1f, 0.22f, 0.1f), RageHot = new Color(1f, 0.62f, 0.2f);

        static void RageStrike()
        {
            var d = new ActionDef { id = "sw_off_5", dur = 1.7f, lockUntil = 1.35f, hitAt = 0.92f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            // Rage Stance: badan rendah, kaki lebar, lutut menekuk, kepala menunduk, dua tangan mencengkeram, bahu naik
            var stance = PlayerAnimator.Mk(n, p => { p.arR = V(-52f, -8f, 12f); p.fR = V(-85f, 0f, 0f); p.arL = V(-56f, 0f, -12f); p.fL = V(-85f, 0f, 0f); p.body = V(12f, 0f, 0f); p.head = V(14f, 0f, 0f); p.lgL = V(-20f, 0f, 0f); p.lgR = V(20f, 0f, 0f); p.lift = -0.14f; });
            // Pull Back: pedang ke belakang, dada terbuka, tubuh melengkung ke belakang, kaki belakang jadi tumpuan
            var pull = PlayerAnimator.Mk(stance, p => { p.arR = V(8f, 32f, 14f); p.fR = V(-62f, 0f, 0f); p.arL = V(0f, 26f, -14f); p.fL = V(-62f, 0f, 0f); p.body = V(-20f, 10f, 0f); p.head = V(-10f, 0f, 0f); p.lgL = V(-8f, 0f, 0f); p.lgR = V(24f, 0f, 0f); p.lift = -0.12f; });
            // Raise Sword: pedang tinggi di atas kepala (siku tidak lurus penuh), badan condong ke belakang, lutut lebih dalam
            var raise = PlayerAnimator.Mk(pull, p => { p.arR = V(-152f, 0f, 12f); p.fR = V(-32f, 0f, 0f); p.arL = V(-148f, 0f, -12f); p.fL = V(-32f, 0f, 0f); p.body = V(-14f, 0f, 0f); p.head = V(-14f, 0f, 0f); p.lgL = V(-14f, 0f, 0f); p.lgR = V(16f, 0f, 0f); p.lift = -0.2f; });
            // Explosive Step: hentakan kaki belakang, satu langkah keras, pinggul lebih dulu
            var step = PlayerAnimator.Mk(raise, p => { p.lunge = 0.32f; p.lift = -0.06f; p.lgL = V(-44f, 0f, 0f); p.lgR = V(24f, 0f, 0f); p.body = V(-4f, 0f, 0f); p.arR = V(-150f, 0f, 12f); p.arL = V(-146f, 0f, -12f); });
            // RAGE STRIKE: kaki -> pinggul -> pinggang -> bahu -> lengan -> pedang; badan membungkuk agresif, lutut depan dalam, kaki belakang memanjang
            var strike = PlayerAnimator.Mk(step, p => { p.arR = V(-34f, 0f, 6f); p.fR = V(-4f, 0f, 0f); p.arL = V(-32f, 0f, -6f); p.fL = V(-4f, 0f, 0f); p.body = V(42f, 0f, 0f); p.head = V(18f, 0f, 0f); p.lunge = 0.4f; p.lift = -0.16f; p.lgL = V(-48f, 0f, 0f); p.lgR = V(30f, 0f, 0f); });
            // IMPACT: berhenti mendadak di posisi rendah, pedang hampir menyentuh tanah, kepala menunduk
            var impact = PlayerAnimator.Mk(strike, p => { p.body = V(46f, 0f, 0f); p.head = V(24f, 0f, 0f); p.lunge = 0.43f; p.lift = -0.2f; p.arR = V(-28f, 0f, 6f); p.arL = V(-26f, 0f, -6f); });
            // Heavy Follow Through: tetap rendah, bahu turun, condong ke depan
            var follow = PlayerAnimator.Mk(impact, p => { p.body = V(38f, 0f, 0f); p.head = V(20f, 0f, 0f); p.lunge = 0.4f; p.lift = -0.16f; p.arR = V(-30f, 0f, 10f); p.arL = V(-28f, 0f, -10f); });
            d.Key(0f, n).Key(0.20f, stance).Key(0.40f, pull).Key(0.60f, raise).Key(0.70f, step).Key(0.90f, strike).Key(1.02f, impact).Key(1.30f, follow).Key(1.70f, n);

            // Stance: aura merah tipis, partikel merah naik, pedang mulai berpendar
            d.At(0f, pc =>
            {
                Audio.Sfx("skill_cast", pc.transform.position);
                Aura(pc, Rage, 0.4f, 0.8f);
                CombatFx.Flash(Tip(pc), Rage, 4f, 1f, 0.3f);
            });
            // Pull Back: aura makin kuat, energi berkumpul di pedang
            d.At(0.20f, pc =>
            {
                Aura(pc, RageHot, 0.4f, 1.3f);
                CombatFx.ShockRing(Feet(pc), 2.4f, new Color(1f, 0.25f, 0.1f, 0.6f), 0.35f);
            });
            // Raise Sword: pedang jadi merah-oranye terang, aura menyala dan partikel berputar menuju bilah
            d.At(0.40f, pc =>
            {
                Aura(pc, RageHot, 0.3f, 1.8f);
                Converge(pc, new Color(1f, 0.5f, 0.15f), 0.28f, 1.5f);
                CombatFx.Flash(Tip(pc), RageHot, 7f, 2.2f, 0.4f);
                CombatFx.Burst(Tip(pc), RageHot, 14, 0.3f, 1.8f, 0.6f, 0.1f);
            });
            // Explosive Step: dorongan keras ke depan, debu meledak kecil, retakan kecil di pijakan
            d.At(0.60f, pc =>
            {
                pc.DashTo(pc.transform.forward, 0.1f, 12f);
                Vector3 foot = Feet(pc);
                CombatFx.Dust(foot, 16);
                MagicFx.Crack(foot, 0.9f, 2.2f);
                CombatFx.ShockRing(foot, 2.2f, new Color(1f, 0.55f, 0.25f, 0.5f), 0.25f);
            });
            // RAGE STRIKE: trail vertikal merah/oranye sangat tebal di belakang pedang
            d.At(0.70f, pc =>
            {
                Audio.Sfx("heavy_swing", pc.transform.position);
                Quaternion vr = pc.transform.rotation * Quaternion.Euler(0f, 0f, 90f);      // bidang vertikal: kepala busur jatuh dari atas ke depan-bawah
                Vector3 piv = Chest(pc) + Vector3.up * 0.3f;
                CombatFx.SweepArc(piv, vr, 3.6f, 78f, 0.55f, 160f, -38f, 0.2f, 0.28f, new Color(1f, 0.28f, 0.08f, 0.95f), true);
                CombatFx.SweepArc(piv, vr, 3.3f, 52f, 0.3f, 160f, -38f, 0.2f, 0.22f, new Color(1f, 0.75f, 0.35f, 1f), false);
            });
            // IMPACT: hit-stop, flash putih-merah, debu, pecahan batu ke samping, retakan memanjang ke depan, percikan merah, shockwave pendek
            d.At(0.90f, pc =>
            {
                Vector3 f = pc.transform.forward, r = pc.transform.right;
                Vector3 g; bool ground = GroundFront(pc, 2.3f, out g);
                HitStop(0.08f);
                CombatFx.Flash(g + Vector3.up * 0.8f, Color.white, 12f, 4f, 0.12f);
                CombatFx.Flash(g + Vector3.up * 0.6f, Rage, 14f, 3f, 0.3f);
                foreach (var m in InFront(pc, 4.4f, 65f)) CombatFx.HitSpark(m.HitPoint, Color.Lerp(Rage, Color.white, 0.4f), 1.7f, 26);
                CombatFx.HitSpark(g + Vector3.up * 0.3f, new Color(1f, 0.4f, 0.2f), 2.0f, 30);
                CombatFx.Dust(g, 22);
                MagicFx.RockBurst(g, 2.2f, 12);
                MagicFx.Debri(g, 2.4f, 12, 1.3f);
                if (ground)
                {
                    MagicFx.Crack(g, 1.6f, 4f);
                    MagicFx.Crack(g + f * 1.9f, 1.3f, 3.6f);
                    MagicFx.Crack(g + f * 3.6f, 1.0f, 3.2f);
                    MagicFx.Crack(g + f * 0.6f + r * 1.4f, 0.8f, 3f);
                    MagicFx.Crack(g + f * 0.6f - r * 1.4f, 0.8f, 3f);
                }
                CombatFx.ShockRing(g, 3.4f, new Color(1f, 0.3f, 0.12f, 0.9f), 0.28f);
                CombatFx.Burst(g + Vector3.up * 0.2f, Rage, 30, 1.2f, 7f, 0.9f, 0.16f);
                ThirdPersonCamera.Shake(0.3f, 0.3f);
            });
            // Heavy Follow Through: aura tebasan memudar, batu kecil jatuh, debu menyebar, bara merah beterbangan
            d.At(1.02f, pc =>
            {
                Vector3 g; GroundFront(pc, 2.3f, out g);
                CombatFx.Burst(g + Vector3.up * 0.4f, RageHot, 18, 1.4f, 2.4f, 1.4f, 0.1f);
                MagicFx.Debri(g, 2.0f, 6, 0.5f);
                CombatFx.Dust(g, 10);
            });
            Add(d);
        }

        // =====================================================================
        // Swordsman — ULTIMATE Executioner's Arc  [sw_off_6]
        // Stance 0-0.25 | Charge 0.25-0.55 | Leap 0.55-0.72 | Mid-Air Rot 0.72-0.90 | ARC 0.90-1.15 | Impact 1.15-1.28 | Follow 1.28-1.60 | Recovery 1.60-2.10
        // =====================================================================
        static void ExecutionerArc()
        {
            var d = new ActionDef { id = "sw_off_6", dur = 2.1f, lockUntil = 1.6f, hitAt = 1.16f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            var stance = PlayerAnimator.Mk(n, p => { p.arR = V(8f, 40f, 12f); p.fR = V(-30f, 0f, 0f); p.arL = V(-20f, 30f, -10f); p.fL = V(-60f, 0f, 0f); p.body = V(14f, -14f, 0f); p.head = V(14f, 0f, 0f); p.lgL = V(-26f, 0f, 0f); p.lgR = V(22f, 0f, 0f); p.lift = -0.16f; });
            var charge = PlayerAnimator.Mk(stance, p => { p.arR = V(22f, 62f, 14f); p.fR = V(-20f, 0f, 0f); p.body = V(20f, -34f, 0f); p.head = V(16f, 12f, 0f); p.lgL = V(-34f, 0f, 0f); p.lgR = V(26f, 0f, 0f); p.lift = -0.26f; });
            var leap = PlayerAnimator.Mk(charge, p => { p.arR = V(20f, 60f, 14f); p.body = V(10f, -28f, 0f); p.head = V(0f, 0f, 0f); p.lgL = V(-60f, 0f, 0f); p.lgR = V(36f, 0f, 0f); p.lift = 0.5f; p.lunge = 0.3f; });
            var rot = PlayerAnimator.Mk(leap, p => { p.arR = V(-110f, 70f, 14f); p.fR = V(-40f, 0f, 0f); p.arL = V(-90f, 40f, -10f); p.body = V(0f, 24f, 0f); p.head = V(0f, 20f, 0f); p.lgL = V(-40f, 0f, 0f); p.lgR = V(30f, 0f, 0f); p.lift = 0.65f; p.lunge = 0.35f; });
            var arc = PlayerAnimator.Mk(rot, p => { p.arR = V(-40f, -62f, 8f); p.fR = V(-6f, 0f, 0f); p.arL = V(-30f, -30f, -8f); p.body = V(32f, -76f, 0f); p.head = V(12f, -40f, 0f); p.lgL = V(-48f, 0f, 0f); p.lgR = V(30f, 0f, 0f); p.lift = 0.2f; p.lunge = 0.4f; });
            var impact = PlayerAnimator.Mk(arc, p => { p.body = V(40f, -80f, 0f); p.head = V(20f, -40f, 0f); p.lift = -0.22f; p.lunge = 0.44f; p.lgL = V(-52f, 0f, 0f); p.lgR = V(30f, 0f, 0f); p.arR = V(-20f, -66f, 8f); });
            var follow = PlayerAnimator.Mk(impact, p => { p.body = V(34f, -70f, 0f); p.arR = V(-4f, -84f, 10f); p.arL = V(-10f, -50f, -8f); p.lift = -0.18f; p.lunge = 0.5f; });
            d.Key(0f, n).Key(0.25f, stance).Key(0.55f, charge).Key(0.72f, leap).Key(0.90f, rot).Key(1.15f, arc).Key(1.28f, impact).Key(1.60f, follow).Key(2.10f, n);

            d.At(0f, pc =>
            {
                Aura(pc, Rage, 0.3f, 1f);
                Converge(pc, Rage, 0.25f, 1.2f);
            });
            d.At(0.25f, pc =>
            {
                Vector3 foot = Feet(pc);
                Aura(pc, Rage, 0.3f, 1.8f);
                Converge(pc, Color.Lerp(Rage, Color.white, 0.5f), 0.3f, 1.5f);
                CombatFx.Flash(Tip(pc), Color.Lerp(Rage, Color.white, 0.5f), 7f, 2.2f, 0.4f);
                MagicFx.Crack(foot, 1.0f, 2.5f);
                CombatFx.Dust(foot, 10);
                CombatFx.ShockRing(foot, 2.8f, new Color(1f, 0.25f, 0.1f, 0.6f), 0.4f);
            });
            d.At(0.55f, pc =>
            {
                Vector3 foot = Feet(pc);
                pc.DashTo(pc.transform.forward, 0.35f, 9f);
                CombatFx.Dust(foot, 22);
                MagicFx.RockBurst(foot, 1.4f, 6);
                MagicFx.Debri(foot, 1.6f, 8, 0.8f);
                CombatFx.ShockRing(foot, 3f, new Color(1f, 0.5f, 0.2f, 0.6f), 0.3f);
                Aura(pc, Rage, 0.4f, 1.5f);
            });
            d.At(0.72f, pc =>
            {
                Audio.Sfx("heavy_swing", pc.transform.position);
                CombatFx.SweepArc(Chest(pc), pc.transform.rotation * Quaternion.Euler(0f, 0f, 24f), 3.0f, 36f, 0.25f, 150f, 40f, 0.18f, 0.25f, new Color(1f, 0.3f, 0.12f, 0.7f), false);
            });
            d.At(0.90f, pc =>
            {
                Audio.Sfx("heavy_swing", pc.transform.position);
                Quaternion br = pc.transform.rotation * Quaternion.Euler(0f, 0f, 24f);     // diagonal-horizontal
                Vector3 piv = Chest(pc);
                CombatFx.SweepArc(piv, br, 4.8f, 80f, 0.8f, 120f, -105f, 0.25f, 0.45f, new Color(1f, 0.22f, 0.1f, 0.95f), true);
                CombatFx.SweepArc(piv, br, 4.5f, 50f, 0.4f, 120f, -105f, 0.25f, 0.35f, new Color(1f, 0.92f, 0.85f, 1f), true);
                CombatFx.SweepArc(piv, br, 5.2f, 36f, 0.25f, 120f, -105f, 0.25f, 0.3f, new Color(1f, 0.5f, 0.3f, 0.8f), false);
                CombatFx.Flash(Tip(pc), Color.white, 10f, 3f, 0.3f);
            });
            d.At(1.16f, pc =>
            {
                Vector3 f = pc.transform.forward, r = pc.transform.right;
                Vector3 g; bool ground = GroundFront(pc, 2.6f, out g);
                HitStop(0.11f);
                MagicFx.UltimateGrand(Feet(pc), f, "phys", Rage, true);
                CombatFx.Flash(g + Vector3.up * 0.9f, Color.white, 16f, 5f, 0.14f);
                CombatFx.Flash(g + Vector3.up * 0.7f, Rage, 18f, 4f, 0.35f);
                foreach (var m in InFront(pc, 6.4f, 105f)) CombatFx.HitSpark(m.HitPoint, Color.Lerp(Rage, Color.white, 0.5f), 2.2f, 34);
                CombatFx.HitSpark(g + Vector3.up * 0.4f, new Color(1f, 0.45f, 0.25f), 2.6f, 40);
                CombatFx.Dust(g, 30);
                MagicFx.RockBurst(g, 3.2f, 16);
                MagicFx.Debri(g, 3.4f, 16, 1.6f);
                if (ground)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * 60f * Mathf.Deg2Rad;
                        MagicFx.Crack(g + (f * Mathf.Cos(a) + r * Mathf.Sin(a)) * 2.2f, 1.2f, 3.6f);
                    }
                    MagicFx.Crack(g, 2f, 4f);
                }
                CombatFx.ShockRing(g, 6.5f, new Color(1f, 0.3f, 0.12f, 0.9f), 0.4f);
                CombatFx.ShockRing(g, 4.2f, new Color(1f, 0.95f, 0.85f, 0.9f), 0.28f);
                CombatFx.Burst(g + Vector3.up * 0.2f, Rage, 40, 1.6f, 8f, 1f, 0.18f);
                ThirdPersonCamera.Shake(0.5f, 0.45f);
            });
            d.At(1.28f, pc =>
            {
                Vector3 g; GroundFront(pc, 2.6f, out g);
                for (int i = 0; i < 4; i++)
                    CombatFx.Beam(g + Vector3.up * 0.5f, g + new Vector3(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(0.8f, 3f), UnityEngine.Random.Range(-3f, 3f)), new Color(1f, 0.4f, 0.2f, 0.9f), 0.12f, 0.35f);
                CombatFx.Burst(g + Vector3.up * 0.5f, RageHot, 26, 2.2f, 3f, 1.5f, 0.1f);
                CombatFx.Dust(g, 14);
            });
            d.At(1.60f, pc =>
            {
                Aura(pc, Rage, 0.4f, 0.5f);
                CombatFx.Burst(Chest(pc), RageHot, 10, 0.8f, 1.4f, 1.4f, 0.09f);
            });
            Add(d);
        }

        // =====================================================================
        // Swordsman — Defense — Perfect Parry  [sw_def_4]
        // Stance 0-0.15 | Read/Wait 0.15-0.30 | Incoming 0.30-0.38 | (saat serangan mengenai: ParryHit) Parry 0.38-0.48 | Deflect -0.62 | Stagger -0.80 | Counter -1.05 | Recovery -1.25
        // =====================================================================
        static readonly Color ParryBlue = new Color(0.45f, 0.75f, 1f);

        static void PerfectParry()
        {
            var d = new ActionDef { id = "sw_def_4", dur = 1.25f, lockUntil = 0.5f, hitAt = 0f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            var stance = PlayerAnimator.Mk(n, p => { p.arR = V(-50f, 14f, 8f); p.fR = V(-70f, 0f, 0f); p.arL = V(-56f, 8f, -8f); p.fL = V(-70f, 0f, 0f); p.body = V(6f, -26f, 0f); p.head = V(0f, 14f, 0f); p.lgL = V(-18f, 0f, 0f); p.lgR = V(14f, 0f, 0f); p.lift = -0.08f; });
            var wait = PlayerAnimator.Mk(stance, p => { p.arR = V(-40f, 14f, 8f); p.arL = V(-46f, 8f, -8f); p.fR = V(-86f, 0f, 0f); p.fL = V(-86f, 0f, 0f); p.lgL = V(-10f, 0f, 0f); p.lgR = V(20f, 0f, 0f); p.lift = -0.1f; p.lunge = -0.06f; });
            var inc = PlayerAnimator.Mk(wait, p => { p.body = V(10f, -34f, 0f); p.arR = V(-34f, 24f, 8f); p.lgL = V(-24f, 0f, 0f); p.lift = -0.14f; });
            d.Key(0f, n).Key(0.15f, stance).Key(0.30f, wait).Key(0.38f, inc).Key(1.05f, inc).Key(1.25f, n);
            d.At(0f, pc => { CombatFx.Flash(Tip(pc), ParryBlue, 3f, 0.6f, 0.9f); });
            d.At(0.15f, pc => { CombatFx.Flash(Tip(pc), ParryBlue, 4f, 1f, 0.9f); });
            Add(d);
        }

        /// <summary>Dipanggil PlayerCombat saat serangan musuh jatuh tepat di jendela Perfect Parry.</summary>
        public static void ParryHit(PlayerCombat pc, Vector3 from)
        {
            Vector3 toFrom = from - pc.transform.position; toFrom.y = 0f;
            float side = Vector3.Dot(toFrom, pc.transform.right) >= 0f ? 1f : -1f;       // +1 = serangan datang dari kanan
            Vector3 contact = pc.transform.position + (toFrom.sqrMagnitude > 0.01f ? toFrom.normalized : pc.transform.forward) * 1.0f + Vector3.up * 0.2f;
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            var n = PlayerAnimator.Base();
            // 0.38-0.48 : tepisan pendek diagonal keluar (tubuh tetap menahan, pinggang & bahu sedikit berputar)
            var parry = PlayerAnimator.Mk(n, p => { p.arR = V(-70f, 20f * side, 8f); p.fR = V(-50f, 0f, 0f); p.arL = V(-64f, 10f * side, -8f); p.fL = V(-50f, 0f, 0f); p.body = V(8f, -30f * side, 0f); p.head = V(0f, 14f, 0f); p.lgL = V(-24f, 0f, 0f); p.lgR = V(18f, 0f, 0f); p.lift = -0.12f; });
            // 0.48-0.62 : Deflect, pedang diteruskan ke luar, bahu terbuka, TIDAK terdorong mundur
            var deflect = PlayerAnimator.Mk(parry, p => { p.arR = V(-92f, 52f * side, 10f); p.arL = V(-80f, 30f * side, -8f); p.body = V(6f, -50f * side, 0f); p.head = V(0f, 24f * side, 0f); p.lift = -0.1f; });
            // 0.62-0.80 : maju setengah langkah, pedang ditarik ke sisi, bahu kembali menghadap musuh
            var stagger = PlayerAnimator.Mk(deflect, p => { p.arR = V(-30f, 24f, 10f); p.fR = V(-60f, 0f, 0f); p.arL = V(-30f, 14f, -8f); p.body = V(10f, -10f, 0f); p.head = V(6f, 0f, 0f); p.lunge = 0.18f; p.lgL = V(-34f, 0f, 0f); p.lgR = V(22f, 0f, 0f); p.lift = -0.12f; });
            // 0.80-1.05 : Counter Window, pedang di sisi pinggang, rendah, pandangan terkunci
            var counter = PlayerAnimator.Mk(stagger, p => { p.arR = V(12f, 36f, 12f); p.fR = V(-40f, 0f, 0f); p.arL = V(0f, 20f, -8f); p.body = V(14f, -6f, 0f); p.head = V(10f, 0f, 0f); p.lunge = 0.2f; p.lift = -0.18f; });
            float dur = 0.87f;     // tabel di bawah memakai waktu asli, dikurangi 0.38 (momen kontak)
            float[] tm = { 0f, 0.10f, 0.24f, 0.42f, 0.67f, 0.87f };
            var poses = new PlayerAnimator.Pose[] { parry, parry, deflect, stagger, counter, n };
            if (PlayerAvatar.I != null) PlayerAvatar.I.PlayCustom(dur, tm.Select(t => t / dur).ToArray(), poses);
            CombatFx.Host.I.StartCoroutine(ParryRoutine(pc, contact, side));
        }

        static IEnumerator ParryRoutine(PlayerCombat pc, Vector3 hitPt, float side)
        {
            // PERFECT PARRY: pedang bertemu pedang musuh, hit-stop 0.05 s
            HitStop(0.05f);
            CombatFx.Flash(hitPt, new Color(0.8f, 0.95f, 1f), 10f, 3f, 0.15f);
            CombatFx.HitSpark(hitPt, new Color(0.85f, 0.95f, 1f), 1.8f, 28);
            CombatFx.ShockRing(hitPt - Vector3.up * 0.2f, 2.4f, new Color(0.6f, 0.85f, 1f, 0.9f), 0.25f);
            CombatFx.SweepArc(Chest(pc), pc.transform.rotation * Quaternion.Euler(0f, 0f, 30f * side), 2.6f, 30f, 0.18f, -20f * side, 55f * side, 0.1f, 0.2f, new Color(0.6f, 0.85f, 1f, 0.9f), false);
            CombatFx.ShockRing(Feet(pc), 5f, ParryBlue, 0.35f);
            yield return new WaitForSeconds(0.12f);
            if (pc == null) yield break;
            // Deflect: busur biru mengikuti ujung pedang, percikan terlempar searah senjata musuh
            CombatFx.SweepArc(Chest(pc), pc.transform.rotation * Quaternion.Euler(0f, 0f, 30f * side), 3.2f, 36f, 0.22f, 55f * side, 110f * side, 0.16f, 0.25f, new Color(0.55f, 0.82f, 1f, 0.9f), true);
            CombatFx.Burst(hitPt, new Color(0.8f, 0.95f, 1f), 12, 0.8f, 1.5f, 0.6f, 0.08f);
            yield return new WaitForSeconds(0.38f);
            if (pc == null) yield break;
            // Counter Window: glow biru-putih di pedang selama jendela terbuka
            for (float t = 0f; t < 0.45f && pc != null; t += 0.15f)
            {
                CombatFx.Flash(Tip(pc), new Color(0.8f, 0.95f, 1f), 4f, 1.2f, 0.2f);
                yield return new WaitForSeconds(0.15f);
            }
        }

        // =====================================================================
        // Swordsman — Defense — Shield Bash  [sw_def_5]
        // Guard 0-0.15 | Step 0.15-0.30 | Brace 0.30-0.45 | BASH 0.45-0.65 | Impact 0.65-0.80 | Follow 0.80-1.00 | Recovery 1.00-1.30
        // =====================================================================
        static void ShieldBash()
        {
            var d = new ActionDef { id = "sw_def_5", dur = 1.3f, lockUntil = 1.0f, hitAt = 0.66f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            // Guard: perisai (lengan kiri) di depan dada, pedang diturunkan ke samping, badan agak menyamping
            var guard = PlayerAnimator.Mk(n, p => { p.arL = V(-62f, -10f, -14f); p.fL = V(-95f, 0f, 0f); p.arR = V(-8f, 26f, 14f); p.fR = V(-24f, 0f, 0f); p.body = V(6f, -24f, 0f); p.head = V(0f, 12f, 0f); p.lgL = V(-18f, 0f, 0f); p.lgR = V(14f, 0f, 0f); p.lift = -0.08f; });
            // Step: langkah cepat kaki depan, badan merendah, perisai tetap dekat tubuh
            var step = PlayerAnimator.Mk(guard, p => { p.lunge = 0.22f; p.lift = -0.16f; p.lgL = V(-34f, 0f, 0f); p.lgR = V(24f, 0f, 0f); p.body = V(14f, -20f, 0f); });
            // Brace: perisai & bahu kiri ditarik sedikit, pinggang berputar kecil, pedang ke belakang untuk keseimbangan
            var brace = PlayerAnimator.Mk(step, p => { p.arL = V(-46f, -4f, -14f); p.fL = V(-110f, 0f, 0f); p.arR = V(14f, 40f, 14f); p.fR = V(-16f, 0f, 0f); p.body = V(16f, 8f, 0f); p.head = V(8f, 0f, 0f); p.lunge = 0.18f; p.lift = -0.2f; });
            // Bash: kaki -> pinggul -> torso -> bahu -> perisai; lengan kiri hampir lurus, badan sangat condong, kaki belakang memanjang
            var bash = PlayerAnimator.Mk(brace, p => { p.arL = V(-86f, -4f, -10f); p.fL = V(-8f, 0f, 0f); p.arR = V(8f, 34f, 14f); p.body = V(38f, -26f, 0f); p.head = V(14f, 0f, 0f); p.lunge = 0.46f; p.lift = -0.18f; p.lgL = V(-44f, 0f, 0f); p.lgR = V(32f, 0f, 0f); });
            // Impact: berhenti mendadak, perisai ditekan, kepala menunduk
            var impact = PlayerAnimator.Mk(bash, p => { p.body = V(42f, -26f, 0f); p.head = V(20f, 0f, 0f); p.lunge = 0.5f; p.arL = V(-90f, -4f, -10f); });
            // Follow Through: masih mendorong setengah langkah, tetap rendah
            var follow = PlayerAnimator.Mk(impact, p => { p.lunge = 0.62f; p.body = V(36f, -22f, 0f); p.lift = -0.14f; p.lgR = V(14f, 0f, 0f); });
            d.Key(0f, n).Key(0.15f, guard).Key(0.30f, step).Key(0.45f, brace).Key(0.65f, bash).Key(0.80f, impact).Key(1.0f, follow).Key(1.30f, n);

            d.At(0.15f, pc =>
            {
                pc.DashTo(pc.transform.forward, 0.15f, 6f);
                CombatFx.Dust(Feet(pc) - pc.transform.forward * 0.4f, 8);
                CombatFx.Burst(Chest(pc) + pc.transform.forward * 0.6f, new Color(1f, 0.85f, 0.6f, 0.6f), 4, 0.4f, 0.3f, 0.4f, 0.1f);
            });
            d.At(0.45f, pc =>
            {
                pc.DashTo(pc.transform.forward, 0.2f, 15f);
                Audio.Sfx("block", pc.transform.position);
                CombatFx.Dust(Feet(pc) - pc.transform.forward * 0.3f, 14);
                CombatFx.Beam(Chest(pc), Chest(pc) + pc.transform.forward * 2f, new Color(1f, 0.85f, 0.55f, 0.7f), 0.5f, 0.2f);
            });
            d.At(0.65f, pc =>
            {
                Vector3 f = pc.transform.forward;
                Vector3 pt = Chest(pc) + f * 1.6f;
                HitStop(0.08f);
                CombatFx.Flash(pt, new Color(1f, 0.85f, 0.6f), 10f, 3f, 0.15f);
                CombatFx.Flash(pt, Color.white, 6f, 2f, 0.08f);
                CombatFx.HitSpark(pt, new Color(1f, 0.7f, 0.3f), 1.8f, 26);
                CombatFx.ShockRing(Feet(pc) + f * 1.6f, 3.4f, new Color(1f, 0.7f, 0.3f, 0.9f), 0.28f);
                CombatFx.Dust(Feet(pc) + f * 0.4f, 14);
                ThirdPersonCamera.Shake(0.2f, 0.2f);
                // indikator stun (bintang kecil) di atas kepala musuh yang terkena
                foreach (var m in InFront(pc, 3.2f, 65f))
                    CombatFx.Burst(m.HitPoint + Vector3.up * 1.1f, new Color(1f, 0.92f, 0.4f), 6, 0.35f, 0.3f, 1.2f, 0.12f);
            });
            d.At(0.80f, pc =>
            {
                Vector3 f = pc.transform.forward;
                CombatFx.Burst(Feet(pc) + f * 1.4f, new Color(1f, 0.7f, 0.3f), 10, 0.9f, 1.4f, 0.8f, 0.08f);
                CombatFx.Dust(Feet(pc) + f * 1.2f, 8);
            });
            Add(d);
        }

        // =====================================================================
        // Swordsman — Defense — ULTIMATE Bulwark  [sw_def_6]
        // Brace 0-0.20 | Plant 0.20-0.40 | Raise Shield 0.40-0.65 | ACTIVATION 0.65-0.90 | Fortress 0.90-1.20 | Ready 1.20-1.50 ; buff 10 dtk: pasif tipis + hex flash saat kena
        // =====================================================================
        static readonly Color BulGold = new Color(1f, 0.82f, 0.35f), BulHot = new Color(1f, 0.96f, 0.75f);

        static void Bulwark()
        {
            var d = new ActionDef { id = "sw_def_6", dur = 1.5f, lockUntil = 1.2f, hitAt = 0.92f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            var brace = PlayerAnimator.Mk(n, p => { p.arL = V(-60f, -8f, -14f); p.fL = V(-100f, 0f, 0f); p.arR = V(-6f, 28f, 14f); p.fR = V(-20f, 0f, 0f); p.body = V(14f, -22f, 0f); p.head = V(14f, 0f, 0f); p.lgL = V(-30f, 0f, 0f); p.lgR = V(28f, 0f, 0f); p.lift = -0.22f; });
            var plant = PlayerAnimator.Mk(brace, p => { p.body = V(20f, -20f, 0f); p.lunge = 0.2f; p.lgL = V(-40f, 0f, 0f); p.lgR = V(30f, 0f, 0f); p.lift = -0.2f; });
            var raise = PlayerAnimator.Mk(plant, p => { p.arL = V(-98f, -6f, -10f); p.fL = V(-60f, 0f, 0f); p.arR = V(10f, 34f, 14f); p.fR = V(-30f, 0f, 0f); p.body = V(18f, -26f, 0f); p.head = V(18f, 0f, 0f); p.lunge = 0.2f; p.lift = -0.22f; });
            var act = PlayerAnimator.Mk(raise, p => { p.arL = V(-90f, -4f, -8f); p.fL = V(-14f, 0f, 0f); p.body = V(24f, -20f, 0f); p.lunge = 0.34f; p.lift = -0.2f; });
            var fort = PlayerAnimator.Mk(act, p => { p.body = V(10f, -14f, 0f); p.head = V(8f, 0f, 0f); p.lunge = 0.3f; p.lift = -0.1f; p.fL = V(-40f, 0f, 0f); });
            var ready = PlayerAnimator.Mk(fort, p => { p.arL = V(-60f, -8f, -12f); p.fL = V(-80f, 0f, 0f); p.arR = V(-40f, 12f, 8f); p.fR = V(-60f, 0f, 0f); p.body = V(2f, -8f, 0f); p.head = V(0f, 0f, 0f); p.lunge = 0.1f; p.lift = -0.06f; });
            d.Key(0f, n).Key(0.20f, brace).Key(0.40f, plant).Key(0.65f, raise).Key(0.90f, act).Key(1.20f, fort).Key(1.50f, ready);

            d.At(0f, pc => { CombatFx.Burst(Feet(pc), BulGold, 8, 0.8f, 0.8f, 0.9f, 0.08f); });
            d.At(0.20f, pc =>
            {
                Vector3 foot = Feet(pc) + pc.transform.forward * 0.5f;
                CombatFx.Dust(foot, 10);
                MagicFx.Crack(foot, 0.7f, 2.4f);
                MagicFx.MagicCircle(Feet(pc) + Vector3.up * 0.05f, 3f, new Color(BulGold.r, BulGold.g, BulGold.b, 0.8f), 1.6f);
                Audio.Sfx("block", pc.transform.position);
            });
            d.At(0.40f, pc => { Converge(pc, BulGold, 0.25f, 1.6f); CombatFx.Burst(Feet(pc), BulGold, 12, 1f, 2f, 0.8f, 0.08f); });
            d.At(0.65f, pc => { CombatFx.Beam(Feet(pc), Chest(pc) + pc.transform.forward * 0.6f, new Color(1f, 0.85f, 0.4f, 0.8f), 0.18f, 0.3f); });
            d.At(0.90f, pc =>
            {
                Vector3 f = pc.transform.forward;
                Vector3 sh = Chest(pc) + f * 0.8f;
                CombatFx.Flash(sh, BulHot, 14f, 4f, 0.35f);
                CombatFx.Flash(sh, Color.white, 8f, 3f, 0.12f);
                CombatFx.ShockRing(Feet(pc), 7f, new Color(1f, 0.82f, 0.35f, 0.9f), 0.45f);
                CombatFx.ShockRing(Feet(pc), 4f, new Color(1f, 0.96f, 0.75f, 0.9f), 0.3f);
                MagicFx.MagicCircle(Feet(pc) + Vector3.up * 0.05f, 6f, new Color(BulGold.r, BulGold.g, BulGold.b, 0.95f), 2.2f);
                CombatFx.Burst(sh, BulGold, 34, 2.4f, 5f, 1.0f, 0.14f);
                HitStop(0.04f);
                ThirdPersonCamera.Shake(0.18f, 0.25f);
                // buff mulai aktif bersama Fortress Formation
                CombatFx.Host.I.StartCoroutine(BulwarkPassive(pc));
            });
            d.At(0.95f, pc =>
            {
                // Fortress Formation: potongan armor energi menyusul bahu -> dada -> lengan -> kaki -> perisai
                Vector3 b = Feet(pc);
                float[] hs = { 1.55f, 1.2f, 0.9f, 0.2f };
                for (int i = 0; i < hs.Length; i++)
                {
                    float h = hs[i];
                    CombatFx.Host.I.StartCoroutine(Late(0.07f * i, () =>
                    {
                        if (pc == null) return;
                        HexFlash(pc.transform.position - Vector3.up * 0.9f + Vector3.up * h, pc.transform.forward, 0.8f, 0.3f);
                        CombatFx.Burst(pc.transform.position - Vector3.up * 0.9f + Vector3.up * h, BulGold, 6, 0.5f, 0.5f, 0.6f, 0.07f);
                    }));
                }
                HexFlash(Chest(pc) + pc.transform.forward * 0.9f, pc.transform.forward, 1.0f, 0.45f);
            });
            Add(d);
        }

        static IEnumerator Late(float t, System.Action a) { yield return new WaitForSeconds(t); if (a != null) a(); }

        /// <summary>Panel segi-enam emas berpendar sesaat (potongan perisai energi).</summary>
        static void HexFlash(Vector3 pos, Vector3 facing, float size, float life)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            UnityEngine.Object.Destroy(q.GetComponent<Collider>());
            q.name = "FxHex";
            q.transform.position = pos;
            q.transform.rotation = Quaternion.LookRotation(-facing);
            q.GetComponent<MeshRenderer>().sharedMaterial = CombatFx.Mat(HexTex(), BulGold);
            var fx = q.AddComponent<CombatFx.FxAnim>();
            fx.life = life; fx.color = BulGold; fx.s0 = Vector3.one * size * 0.6f; fx.s1 = Vector3.one * size;
        }

        static Texture2D hexTex;
        static Texture2D HexTex()
        {
            if (hexTex != null) return hexTex;
            int n = 128;
            hexTex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            hexTex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x / (n - 1f)) * 2f - 1f, v = (y / (n - 1f)) * 2f - 1f;
                    float ax = Mathf.Abs(u), ay = Mathf.Abs(v);
                    float hexd = Mathf.Max(ax * 0.866f + ay * 0.5f, ay);          // jarak segi-enam
                    float edge = Mathf.Clamp01(1f - Mathf.Abs(hexd - 0.82f) / 0.07f);
                    float fill = hexd < 0.82f ? 0.22f * (1f - hexd) : 0f;
                    float a = Mathf.Clamp01(edge + fill);
                    hexTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            hexTex.Apply();
            return hexTex;
        }

        /// <summary>Selama buff 10 detik: outline/partikel emas tipis, makin redup di detik 8-10, padam di detik 10.</summary>
        static IEnumerator BulwarkPassive(PlayerCombat pc)
        {
            float t = 0f;
            while (t < 10f)
            {
                if (pc == null || pc.IsDead) yield break;
                float k = t < 8f ? 1f : Mathf.Lerp(1f, 0.25f, (t - 8f) / 2f);
                bool flicker = t > 8f && Mathf.Repeat(t, 0.4f) < 0.2f;
                if (!flicker)
                {
                    CombatFx.Burst(pc.transform.position - Vector3.up * 0.3f, BulGold, Mathf.Max(1, Mathf.RoundToInt(2f * k)), 0.6f, 0.5f, 0.9f, 0.06f);
                    CombatFx.Flash(Tip(pc), BulGold, 2.5f * k, 0.5f * k, 0.5f);
                }
                yield return new WaitForSeconds(0.4f);
                t += 0.4f;
            }
            if (pc == null) yield break;
            CombatFx.Burst(pc.transform.position, BulGold, 16, 0.9f, 2.2f, 1f, 0.08f);
            CombatFx.ShockRing(Feet(pc), 2.4f, new Color(1f, 0.82f, 0.35f, 0.5f), 0.4f);
        }

        /// <summary>Dipanggil PlayerCombat saat player menerima serangan selama Bulwark: hex flash, tubuh tidak goyah.</summary>
        public static void BulwarkHit(PlayerCombat pc, Vector3 from)
        {
            Vector3 dir = from - pc.transform.position; dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = pc.transform.forward;
            dir.Normalize();
            Vector3 pt = Chest(pc) + dir * 0.7f;
            HexFlash(pt, dir, 1.1f, 0.18f);
            CombatFx.Flash(pt, BulHot, 6f, 1.6f, 0.12f);
            CombatFx.HitSpark(pt, BulGold, 1.2f, 14);
            CombatFx.Burst(pt, BulGold, 8, 0.5f, 0.8f, 0.5f, 0.06f);
        }

        // =====================================================================
        // Swordsman — Mobility — Blink Strike  [sw_mob_4]
        // Lock 0-0.12 | Compress 0.12-0.22 | Launch 0.22-0.30 | BLINK 0.30-0.38 | Strike 0.38-0.48 | Pass 0.48-0.62 | Delayed Impact 0.62-0.75 | Recovery 0.75-1.05
        // =====================================================================
        static readonly Color BlinkBlue = new Color(0.6f, 0.85f, 1f);
        static Vector3 blinkTarget;

        static void BlinkStrike()
        {
            var d = new ActionDef { id = "sw_mob_4", dur = 1.05f, lockUntil = 0.8f, hitAt = 0.22f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            var lockp = PlayerAnimator.Mk(n, p => { p.arR = V(16f, 40f, 12f); p.fR = V(-14f, 0f, 0f); p.arL = V(-30f, 16f, -8f); p.fL = V(-60f, 0f, 0f); p.body = V(26f, -10f, 0f); p.head = V(10f, 0f, 0f); p.lgL = V(-28f, 0f, 0f); p.lgR = V(24f, 0f, 0f); p.lift = -0.18f; });
            var comp = PlayerAnimator.Mk(lockp, p => { p.arR = V(24f, 54f, 12f); p.body = V(34f, -14f, 0f); p.lgL = V(-38f, 0f, 0f); p.lgR = V(32f, 0f, 0f); p.lift = -0.3f; });
            var launch = PlayerAnimator.Mk(comp, p => { p.body = V(62f, -6f, 0f); p.head = V(20f, 0f, 0f); p.lunge = 0.45f; p.lift = -0.25f; p.lgL = V(-52f, 0f, 0f); p.lgR = V(40f, 0f, 0f); });
            var blink = PlayerAnimator.Mk(launch, p => { p.body = V(66f, -4f, 0f); p.head = V(24f, 0f, 0f); p.lunge = 0.55f; p.arR = V(30f, 58f, 12f); });
            var strike = PlayerAnimator.Mk(blink, p => { p.arR = V(-78f, -50f, 10f); p.fR = V(-6f, 0f, 0f); p.body = V(58f, -46f, 0f); p.head = V(18f, -20f, 0f); p.lunge = 0.58f; });
            var pass = PlayerAnimator.Mk(strike, p => { p.arR = V(-30f, -62f, 10f); p.body = V(50f, -36f, 0f); p.lunge = 0.35f; p.lift = -0.26f; p.lgL = V(-40f, 0f, 0f); p.lgR = V(30f, 0f, 0f); });
            var hold = PlayerAnimator.Mk(pass, p => { p.arR = V(-24f, -60f, 10f); p.body = V(36f, -30f, 0f); p.lunge = 0.1f; p.lift = -0.22f; });
            var turn = PlayerAnimator.Mk(hold, p => { p.arR = V(-50f, 6f, 10f); p.fR = V(-60f, 0f, 0f); p.body = V(10f, -4f, 0f); p.head = V(0f, 0f, 0f); p.lunge = 0f; p.lift = -0.08f; });
            d.Key(0f, n).Key(0.12f, lockp).Key(0.22f, comp).Key(0.30f, launch).Key(0.38f, blink).Key(0.48f, strike).Key(0.62f, pass).Key(0.75f, hold).Key(1.05f, turn);

            d.At(0f, pc => { CombatFx.Flash(Tip(pc), BlinkBlue, 4f, 1.2f, 0.3f); CombatFx.Burst(Chest(pc) + Vector3.up * 0.3f, BlinkBlue, 3, 0.15f, 0.2f, 0.4f, 0.05f); });
            d.At(0.12f, pc =>
            {
                Converge(pc, BlinkBlue, 0.1f, 1.0f);
                CombatFx.Dust(Feet(pc), 6);
            });
            d.At(0.22f, pc =>
            {
                // Explosive Launch: debu meledak di titik awal + garis kecepatan, lalu afterimage 0.22-0.56
                Vector3 f = pc.transform.forward;
                CombatFx.Dust(Feet(pc), 18);
                CombatFx.ShockRing(Feet(pc), 2.4f, new Color(0.6f, 0.85f, 1f, 0.7f), 0.25f);
                CombatFx.Beam(Chest(pc), Chest(pc) + f * 8f, new Color(0.75f, 0.92f, 1f, 0.5f), 0.12f, 0.3f);
                Audio.Sfx("swing2", pc.transform.position);
                CombatFx.Host.I.StartCoroutine(Afterimages(pc, 0.34f));
            });
            d.At(0.36f, pc =>
            {
                // cari target yang baru dilewati, untuk garis tebasan
                Monster best = null; float bd = 99f;
                foreach (var m in Monster.All)
                {
                    if (m == null || m.IsDead) continue;
                    float dd = Vector3.Distance(m.transform.position, pc.transform.position);
                    if (dd < bd) { bd = dd; best = m; }
                }
                blinkTarget = best != null && bd < 5f ? best.HitPoint : pc.transform.position + pc.transform.forward * 1.2f + Vector3.up * 1f;
            });
            d.At(0.40f, pc =>
            {
                // satu garis tebasan biru-putih tipis dan tajam melewati tubuh musuh
                Vector3 r = pc.transform.right;
                CombatFx.Beam(blinkTarget - r * 1.8f + Vector3.up * 0.4f, blinkTarget + r * 1.8f - Vector3.up * 0.4f, new Color(0.9f, 0.97f, 1f, 1f), 0.09f, 0.25f);
                CombatFx.SweepArc(Chest(pc), pc.transform.rotation * Quaternion.Euler(0f, 0f, 14f), 2.4f, 18f, 0.1f, 70f, -70f, 0.1f, 0.2f, new Color(0.85f, 0.95f, 1f, 1f), false);
            });
            d.At(0.50f, pc =>
            {
                CombatFx.Dust(Feet(pc), 8);
                CombatFx.ShockRing(Feet(pc), 1.8f, new Color(0.7f, 0.9f, 1f, 0.5f), 0.2f);
            });
            d.At(0.62f, pc =>
            {
                // Delayed Slash Impact (~0.1 s setelah berhenti): SHING! garis tebasan menyala di musuh
                Audio.Sfx("swing", blinkTarget);
                CombatFx.Flash(blinkTarget, new Color(0.9f, 0.97f, 1f), 7f, 2.4f, 0.14f);
                CombatFx.HitSpark(blinkTarget, new Color(0.85f, 0.95f, 1f), 1.4f, 20);
                Vector3 r = pc.transform.right;
                CombatFx.Beam(blinkTarget - r * 1.5f + Vector3.up * 0.35f, blinkTarget + r * 1.5f - Vector3.up * 0.35f, Color.white, 0.12f, 0.18f);
            });
            Add(d);
        }

        /// <summary>Bayangan biru transparan yang tertinggal di jalur dash.</summary>
        static IEnumerator Afterimages(PlayerCombat pc, float secs)
        {
            float t = 0f;
            while (t < secs && pc != null)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                UnityEngine.Object.Destroy(q.GetComponent<Collider>());
                q.name = "FxAfterimage";
                q.transform.position = pc.transform.position;
                q.GetComponent<MeshRenderer>().sharedMaterial = CombatFx.Mat(CombatFx.Soft(), BlinkBlue);
                var fx = q.AddComponent<CombatFx.FxAnim>();
                fx.life = 0.28f; fx.billboard = true; fx.color = BlinkBlue; fx.alpha0 = 0.7f;
                fx.s0 = new Vector3(1.0f, 2.1f, 1f); fx.s1 = new Vector3(0.5f, 1.8f, 1f);
                yield return new WaitForSeconds(0.04f);
                t += 0.04f;
            }
        }

        // =====================================================================
        // Swordsman — Mobility — Riposte  [sw_mob_5]
        // Aktivasi: Ready 0-0.18 | Bait 0.18-0.40 | Window 3.5 s (glow tipis)
        // Saat diserang (RiposteHit): Read 0.08 | Slip 0.08-0.24 | Redirect 0.24-0.34 | RIPOSTE 0.34 | Pass 0.34-0.50 | Follow 0.50-0.65 | Recovery -0.95
        // =====================================================================
        static void RiposteSkill()
        {
            var d = new ActionDef { id = "sw_mob_5", dur = 0.4f, lockUntil = 0.25f, hitAt = 0f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            var ready = PlayerAnimator.Mk(n, p => { p.arR = V(10f, 38f, 12f); p.fR = V(-18f, 0f, 0f); p.arL = V(-24f, 14f, -8f); p.fL = V(-50f, 0f, 0f); p.body = V(8f, -14f, 0f); p.head = V(0f, 6f, 0f); p.lgL = V(-18f, 0f, 0f); p.lgR = V(16f, 0f, 0f); p.lift = -0.14f; p.lunge = -0.1f; });
            var bait = PlayerAnimator.Mk(ready, p => { p.arR = V(4f, 30f, 12f); p.body = V(6f, -34f, 0f); p.head = V(0f, 20f, 0f); p.lift = -0.12f; p.lunge = -0.06f; });
            d.Key(0f, n).Key(0.18f, ready).Key(0.40f, bait);
            d.At(0f, pc => { CombatFx.Flash(Tip(pc), BlinkBlue, 4f, 1.4f, 0.25f); });
            d.At(0.40f, pc => { CombatFx.Host.I.StartCoroutine(RiposteGlow(pc)); });
            Add(d);
        }

        /// <summary>Selama jendela 3.5 s: bilah berpendar biru tipis, kilau kecil, partikel sedikit di kaki.</summary>
        static IEnumerator RiposteGlow(PlayerCombat pc)
        {
            while (pc != null && pc.RiposteActive)
            {
                CombatFx.Flash(Tip(pc), BlinkBlue, 3f, 0.7f, 0.5f);
                CombatFx.Burst(Feet(pc), BlinkBlue, 1, 0.4f, 0.3f, 0.6f, 0.05f);
                if (UnityEngine.Random.value < 0.3f) CombatFx.Flash(Chest(pc) + Vector3.up * 0.5f, Color.white, 1.5f, 0.8f, 0.1f);
                yield return new WaitForSeconds(0.45f);
            }
            if (pc != null) CombatFx.Burst(Chest(pc), BlinkBlue, 5, 0.5f, 0.6f, 0.6f, 0.05f);     // glow padam
        }

        /// <summary>Serangan musuh tepat saat jendela Riposte: sidestep -> redirect -> tebasan balik dalam satu aliran.</summary>
        public static void RiposteHit(PlayerCombat pc, Vector3 from)
        {
            CombatFx.Host.I.StartCoroutine(RiposteRoutine(pc, from));
        }

        static IEnumerator RiposteRoutine(PlayerCombat pc, Vector3 from)
        {
            var gs = GameState.I;
            float dmg = (10f + (gs != null ? gs.TotalAtk : 0)) * 2.4f;
            Monster best = null; float bd = 7f;
            foreach (var m in Monster.All)
            {
                if (m == null || m.IsDead) continue;
                float dd = Vector3.Distance(m.transform.position, pc.transform.position);
                if (dd < bd) { bd = dd; best = m; }
            }
            Vector3 toT = best != null ? best.transform.position - pc.transform.position : from - pc.transform.position;
            toT.y = 0f;
            if (toT.sqrMagnitude < 0.01f) toT = pc.transform.forward;
            toT.Normalize();
            pc.transform.rotation = Quaternion.LookRotation(toT);
            float side = Random.value < 0.5f ? 1f : -1f;
            Vector3 slip = Quaternion.AngleAxis(40f * side, Vector3.up) * toT;
            pc.Invuln(0.6f);
            pc.LockAttack(0.55f);

            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            var n = PlayerAnimator.Base();
            var read = PlayerAnimator.Mk(n, p => { p.arR = V(8f, 40f, 12f); p.body = V(10f, -20f * side, 0f); p.head = V(0f, 20f * side, 0f); p.lgL = V(-22f, 0f, 0f); p.lgR = V(18f, 0f, 0f); p.lift = -0.14f; });
            var slipP = PlayerAnimator.Mk(read, p => { p.arR = V(14f, 52f, 12f); p.body = V(30f, 40f * side, 0f); p.head = V(10f, -20f * side, 0f); p.lunge = 0.35f; p.lift = -0.22f; p.lgL = V(-40f, 0f, 0f); p.lgR = V(30f, 0f, 0f); });
            var redirect = PlayerAnimator.Mk(slipP, p => { p.arR = V(-8f, 32f, 12f); p.body = V(26f, 24f * side, 0f); p.lunge = 0.3f; });
            var slash = PlayerAnimator.Mk(redirect, p => { p.arR = V(-110f, -48f, 10f); p.fR = V(-10f, 0f, 0f); p.body = V(26f, -50f * side, 0f); p.head = V(10f, -20f * side, 0f); p.lunge = 0.42f; p.lift = -0.2f; });
            var pass = PlayerAnimator.Mk(slash, p => { p.arR = V(-120f, -56f, 10f); p.body = V(22f, -40f * side, 0f); p.lunge = 0.5f; });
            var follow = PlayerAnimator.Mk(pass, p => { p.arR = V(-84f, -30f, 10f); p.body = V(18f, -22f * side, 0f); p.lunge = 0.2f; p.lift = -0.14f; });
            float dur = 0.95f;
            float[] tm = { 0f, 0.08f, 0.24f, 0.34f, 0.50f, 0.65f, 0.95f };
            var poses = new PlayerAnimator.Pose[] { n, read, slipP, redirect, slash, follow, n };
            // (pass dipakai sebagai pose tengah momentum; urutan ringkas agar tidak terlalu kaku)
            poses[4] = pass;
            if (PlayerAvatar.I != null) PlayerAvatar.I.PlayCustom(dur, tm.Select(t => t / dur).ToArray(), poses);

            // Attack Read
            CombatFx.Flash(Chest(pc), BlinkBlue, 3f, 1f, 0.12f);
            yield return new WaitForSeconds(0.08f);
            if (pc == null) yield break;
            // Slip / Sidestep diagonal 30-45 derajat, afterimage biru + garis kecepatan
            pc.DashTo(slip, 0.16f, 9f);
            CombatFx.Host.I.StartCoroutine(Afterimages(pc, 0.16f));
            CombatFx.Beam(Chest(pc), Chest(pc) + slip * 3f, new Color(0.75f, 0.92f, 1f, 0.5f), 0.1f, 0.2f);
            yield return new WaitForSeconds(0.16f);
            if (pc == null) yield break;
            // Blade Redirect: TING! ringan satu spark kecil
            Audio.Sfx("block", pc.transform.position);
            Vector3 tp = Chest(pc) + toT * 0.9f;
            CombatFx.HitSpark(tp, new Color(0.85f, 0.95f, 1f), 0.9f, 10);
            CombatFx.Flash(tp, Color.white, 4f, 1.5f, 0.08f);
            yield return new WaitForSeconds(0.10f);
            if (pc == null) yield break;
            // RIPOSTE: pinggul memutar, tebasan diagonal cepat dari bawah ke atas
            if (best != null && !best.IsDead)
            {
                Vector3 sd = best.transform.position - pc.transform.position; sd.y = 0f;
                if (sd.sqrMagnitude > 0.01f) pc.transform.rotation = Quaternion.LookRotation(sd.normalized);
            }
            Audio.Sfx("swing2", pc.transform.position);
            CombatFx.SweepArc(Feet(pc) + Vector3.up * 1.0f, pc.transform.rotation * Quaternion.Euler(0f, 0f, 38f * side), 3.0f, 52f, 0.35f, -90f, 80f, 0.12f, 0.28f, new Color(0.7f, 0.9f, 1f, 0.95f), true);
            CombatFx.SweepArc(Chest(pc), pc.transform.rotation * Quaternion.Euler(0f, 0f, 38f * side), 2.8f, 26f, 0.18f, -90f, 80f, 0.12f, 0.22f, Color.white, false);
            pc.DashTo(pc.transform.forward, 0.18f, 7f);        // momentum membawa player melewati sisi musuh
            if (best != null && !best.IsDead)
            {
                best.TakeDamage(dmg);
                best.Stagger(1.2f);
                CombatFx.Flash(best.HitPoint, Color.white, 8f, 2.6f, 0.12f);
                CombatFx.HitSpark(best.HitPoint, new Color(0.8f, 0.95f, 1f), 1.6f, 24);
            }
            if (TownUI.I != null) TownUI.I.Toast("RIPOSTE!");
            yield return new WaitForSeconds(0.3f);
            if (pc == null) yield break;
            // Follow Through: arc memudar jadi partikel kecil
            CombatFx.Burst(Tip(pc), BlinkBlue, 8, 0.6f, 0.8f, 0.6f, 0.06f);
        }

        // =====================================================================
        // Swordsman — Mobility — ULTIMATE Phantom Flurry  [sw_mob_6]
        // Stance 0-0.20 | Flash Step 0.20-0.35 | First Flurry 0.35-0.80 | Phantom Circle 0.80-1.30 | High-Speed 1.30-1.80 | Disappear 1.80-1.95
        // FINISH 1.95-2.20 | Delayed Multi-Hit 2.20-2.40 | Brake 2.40-2.60 | Recovery 2.60-2.90
        // =====================================================================
        public static Vector3 FlurryCenter;
        static readonly Color PhantomCol = new Color(0.55f, 0.82f, 1f);

        static void PhantomFlurry()
        {
            var d = new ActionDef { id = "sw_mob_6", dur = 2.9f, lockUntil = 2.55f, hitAt = 2.2f };
            var n = PlayerAnimator.Base();
            var V = new System.Func<float, float, float, Vector3>((x, y, z) => PlayerAnimator.Vec(x, y, z));
            var stance = PlayerAnimator.Mk(n, p => { p.arR = V(22f, 50f, 12f); p.fR = V(-16f, 0f, 0f); p.arL = V(-40f, 20f, -10f); p.fL = V(-50f, 0f, 0f); p.body = V(40f, -8f, 0f); p.head = V(18f, 0f, 0f); p.lgL = V(-44f, 0f, 0f); p.lgR = V(36f, 0f, 0f); p.lift = -0.3f; p.lunge = 0.1f; });
            var dash = PlayerAnimator.Mk(stance, p => { p.body = V(62f, -6f, 0f); p.lunge = 0.5f; p.lift = -0.26f; p.lgL = V(-52f, 0f, 0f); p.lgR = V(40f, 0f, 0f); });
            var sA = PlayerAnimator.Mk(dash, p => { p.arR = V(-70f, -56f, 10f); p.fR = V(-8f, 0f, 0f); p.arL = V(-30f, -20f, -8f); p.body = V(48f, -50f, 0f); p.head = V(12f, -20f, 0f); p.lunge = 0.45f; p.lift = -0.24f; p.lgL = V(-46f, 0f, 0f); p.lgR = V(34f, 0f, 0f); });
            var sB = PlayerAnimator.Mk(dash, p => { p.arR = V(-70f, 56f, 10f); p.fR = V(-8f, 0f, 0f); p.arL = V(-30f, 20f, -8f); p.body = V(48f, 50f, 0f); p.head = V(12f, 20f, 0f); p.lunge = 0.45f; p.lift = -0.24f; p.lgL = V(-46f, 0f, 0f); p.lgR = V(34f, 0f, 0f); });
            d.Key(0f, n).Key(0.20f, stance).Key(0.35f, dash);
            // First Flurry & Phantom Circle & High-Speed: slash kiri/kanan bergantian tanpa pose berhenti
            float t = 0.45f; bool flip = false;
            while (t < 1.78f) { d.Key(t, flip ? sB : sA); flip = !flip; t += t < 0.8f ? 0.2f : (t < 1.3f ? 0.125f : 0.1f); }
            var gone = PlayerAnimator.Mk(dash, p => { p.body = V(54f, 0f, 0f); p.lunge = 0.3f; p.lift = -0.24f; p.arR = V(24f, 54f, 12f); });
            var wind = PlayerAnimator.Mk(stance, p => { p.arR = V(34f, 62f, 12f); p.body = V(46f, -16f, 0f); p.lift = -0.3f; });
            var fin = PlayerAnimator.Mk(dash, p => { p.arR = V(-110f, -64f, 10f); p.fR = V(-6f, 0f, 0f); p.arL = V(-50f, -30f, -8f); p.body = V(40f, -88f, 0f); p.head = V(12f, -40f, 0f); p.lunge = 0.55f; p.lift = -0.22f; });
            var hold = PlayerAnimator.Mk(fin, p => { p.arR = V(-20f, -70f, 10f); p.body = V(42f, -50f, 0f); p.lunge = 0.2f; p.lift = -0.26f; });
            var brake = PlayerAnimator.Mk(hold, p => { p.lgL = V(-52f, 0f, 0f); p.lgR = V(30f, 0f, 0f); p.lunge = 0.1f; });
            d.Key(1.84f, gone).Key(1.95f, wind).Key(2.10f, fin).Key(2.20f, hold).Key(2.40f, brake).Key(2.90f, n);

            d.At(0f, pc =>
            {
                pc.Invuln(2.6f);
                // pilih target: musuh terdekat di depan; kalau tidak ada, titik 6 m di depan
                Monster best = null; float bd = 14f;
                foreach (var m in InFront(pc, 14f, 70f))
                {
                    float dd = Vector3.Distance(m.transform.position, pc.transform.position);
                    if (dd < bd) { bd = dd; best = m; }
                }
                FlurryCenter = best != null ? best.transform.position : pc.transform.position + pc.transform.forward * 6f;
                Audio.Sfx("heavy_swing", pc.transform.position);
                Converge(pc, PhantomCol, 0.2f, 1.4f);
                CombatFx.Flash(Tip(pc), PhantomCol, 5f, 1.4f, 0.4f);
                CombatFx.Host.I.StartCoroutine(FlurryRoutine(pc));
            });
            Add(d);
        }

        static void PSlash(PlayerCombat pc, float roll, bool big)
        {
            Vector3 c = FlurryCenter;
            Vector3 to = c - pc.transform.position; to.y = 0f;
            if (to.sqrMagnitude > 0.01f) pc.transform.rotation = Quaternion.LookRotation(to.normalized);
            Audio.Sfx(Random.value < 0.5f ? "swing" : "swing2", pc.transform.position);
            Quaternion br = pc.transform.rotation * Quaternion.Euler(0f, 0f, roll);
            Color col = big ? new Color(0.7f, 0.9f, 1f, 0.95f) : new Color(0.6f, 0.85f, 1f, 0.8f);
            CombatFx.SweepArc(Chest(pc), br, big ? 4.6f : 3.0f, big ? 70f : 22f, big ? 0.7f : 0.12f, 110f, -110f, big ? 0.22f : 0.1f, big ? 0.4f : 0.18f, col, big);
            if (!big) CombatFx.HitSpark(c + Vector3.up * 1.0f, new Color(0.85f, 0.95f, 1f), 0.8f, 6);
        }

        static void PDash(PlayerCombat pc, Vector3 target, float dur)
        {
            Vector3 dir = target - pc.transform.position; dir.y = 0f;
            float dist = dir.magnitude;
            if (dist < 0.05f) return;
            pc.transform.rotation = Quaternion.LookRotation(dir.normalized);
            pc.DashTo(dir.normalized, dur, dist / dur);
            CombatFx.Host.I.StartCoroutine(Afterimages(pc, dur + 0.06f));
            CombatFx.Dust(Feet(pc), 3);
        }

        static IEnumerator FlurryRoutine(PlayerCombat pc)
        {
            Vector3 c = FlurryCenter;
            Vector3 u = pc.transform.position - c; u.y = 0f;
            if (u.sqrMagnitude < 0.01f) u = -pc.transform.forward;
            u.Normalize();
            Vector3 r = Vector3.Cross(Vector3.up, u);
            yield return new WaitForSeconds(0.20f);                         // Flash Step
            if (pc == null) yield break;
            CombatFx.Dust(Feet(pc), 20);
            CombatFx.ShockRing(Feet(pc), 2.4f, new Color(0.6f, 0.85f, 1f, 0.7f), 0.25f);
            PDash(pc, c + u * 1.6f, 0.15f);
            yield return new WaitForSeconds(0.15f);
            if (pc == null) yield break;
            PDash(pc, c - u * 2.2f, 0.2f);                                  // First Flurry: tebasan #1 sambil melewati sisi musuh
            yield return new WaitForSeconds(0.08f);
            PSlash(pc, 35f, false);
            yield return new WaitForSeconds(0.22f);
            if (pc == null) yield break;
            PDash(pc, c + r * 2.2f, 0.12f);                                 // land -> pivot -> dash -> tebasan #2
            yield return new WaitForSeconds(0.12f);
            PSlash(pc, -35f, false);
            yield return new WaitForSeconds(0.05f);
            // Phantom Circle: depan -> kanan -> belakang -> kiri, setiap sisi satu tebasan
            Vector3[] ring = { u * 2f, r * 2f, -u * 2f, -r * 2f };
            for (int i = 0; i < 4; i++)
            {
                if (pc == null) yield break;
                PDash(pc, c + ring[i], 0.07f);
                yield return new WaitForSeconds(0.07f);
                PSlash(pc, i % 2 == 0 ? 30f : -30f, false);
                yield return new WaitForSeconds(0.055f);
            }
            // High-Speed Flurry: horizontal -> diagonal -> reverse -> vertikal, dari sudut berbeda, banyak trail tipis
            float[] rolls = { 0f, 35f, -35f, 85f };
            for (int i = 0; i < 4; i++)
            {
                if (pc == null) yield break;
                Vector3 p = Quaternion.AngleAxis(45f + i * 90f, Vector3.up) * u * 2.2f;
                PDash(pc, c + p, 0.06f);
                for (int k = 0; k < 3; k++)
                    CombatFx.Beam(c + Vector3.up * 1f + Random.insideUnitSphere * 2f, c + Vector3.up * 1f + Random.insideUnitSphere * 2f, new Color(0.7f, 0.9f, 1f, 0.5f), 0.04f, 0.25f);
                yield return new WaitForSeconds(0.06f);
                PSlash(pc, rolls[i], false);
                yield return new WaitForSeconds(0.04f);
            }
            // Phantom Disappear: menghilang, muncul 3 m di depan musuh dengan pedang ditarik jauh
            if (pc == null) yield break;
            yield return new WaitForSeconds(0.04f);
            CombatFx.Flash(Chest(pc), PhantomCol, 8f, 2.5f, 0.2f);
            CombatFx.Burst(Chest(pc), PhantomCol, 14, 0.6f, 1f, 0.5f, 0.07f);
            PDash(pc, c + u * 3f, 0.08f);
            yield return new WaitForSeconds(0.11f);
            if (pc == null) yield break;
            // FINISHING SLASH: hentak -> dash lurus melewati target -> tebasan diagonal besar
            CombatFx.Dust(Feet(pc), 24);
            CombatFx.ShockRing(Feet(pc), 3f, new Color(0.6f, 0.85f, 1f, 0.8f), 0.3f);
            Audio.Sfx("heavy_swing", pc.transform.position);
            PDash(pc, c - u * 2.6f, 0.25f);
            yield return new WaitForSeconds(0.13f);
            if (pc == null) yield break;
            PSlash(pc, 40f, true);
            CombatFx.Flash(c + Vector3.up, Color.white, 14f, 4f, 0.15f);
            HitStop(0.08f);
            yield return new WaitForSeconds(0.1f);
            // Delayed Multi-Hit Impact: SHING x4 lalu BOOM
            for (int i = 0; i < 4; i++)
            {
                Vector3 a = Quaternion.AngleAxis(i * 45f + 20f, Vector3.up) * u * 1.8f;
                CombatFx.Beam(c + Vector3.up * 1.0f + a, c + Vector3.up * 1.0f - a, i % 2 == 0 ? Color.white : new Color(0.7f, 0.9f, 1f, 1f), 0.08f, 0.3f);
                CombatFx.Flash(c + Vector3.up, PhantomCol, 6f, 2f, 0.1f);
                CombatFx.HitSpark(c + Vector3.up, new Color(0.85f, 0.95f, 1f), 1.3f, 14);
                Audio.Sfx("swing2", c);
                yield return new WaitForSeconds(0.03f);
            }
            yield return new WaitForSeconds(0.07f);
            CombatFx.Flash(c + Vector3.up, Color.white, 18f, 5f, 0.2f);
            CombatFx.ShockRing(c, 5.5f, new Color(0.7f, 0.9f, 1f, 0.9f), 0.35f);
            CombatFx.HitSpark(c + Vector3.up, Color.white, 2.2f, 36);
            ThirdPersonCamera.Shake(0.28f, 0.3f);
            yield return new WaitForSeconds(0.2f);
            // Momentum Brake: debu memanjang dan sisa energi biru
            if (pc == null) yield break;
            CombatFx.Dust(Feet(pc), 16);
            CombatFx.Burst(Chest(pc), PhantomCol, 10, 0.6f, 1f, 0.8f, 0.07f);
        }
    }
}