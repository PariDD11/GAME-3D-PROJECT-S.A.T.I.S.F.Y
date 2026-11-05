using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Implementasi "Katalog Skill REVISI VFX": semua skill aktif Archer / Fire / Earth / Water / Wind Mage / Priest yang belum punya koreografi sendiri
    /// otomatis mendapat aksi pemain bertahap (stance -> kumpul energi -> puncak charge -> release -> follow-through -> recovery) dan
    /// VFX charge tebal. Skill biasa: charge singkat. Ultimate: charge panjang; ultimate SIHIR membuat pemain melayang, magic circle berlapis,
    /// partikel lingkungan tertarik ke pusat, telegraph area, kamera mundur-naik sinematik, lalu turun mendarat satu kaki lebih dulu.
    /// Skill Swordsman dan skill yang sudah punya koreografi (mis. Piercing Shot, Deadeye, Heartseeker) tidak diubah.
    /// </summary>
    public static partial class SkillAction
    {
        static readonly HashSet<string> premium = new HashSet<string>();
        public static bool IsPremium(string id) { Ensure(); return premium.Contains(id); }

        static void RegisterPremium()
        {
            foreach (var s in SkillDB.All)
            {
                if (!s.active || s.job == "Swordsman" || defs.ContainsKey(s.id)) continue;
                var d = s.job == "Archer" ? BuildArcher(s) : BuildCaster(s);
                if (d == null) continue;
                Add(d); premium.Add(s.id);
            }
        }

        static System.Func<float, float, float, Vector3> VV = (x, y, z) => PlayerAnimator.Vec(x, y, z);

        // ------------------------------------------------------------------ pustaka gerakan: tiap skill mendapat gerakan unik
        sealed class Gest
        {
            public string name; public float len, hit;
            public System.Action<PlayerAnimator.Pose>[] edits; public float[] at;
            public Gest(string n, float len, float hit, float[] at, params System.Action<PlayerAnimator.Pose>[] edits) { name = n; this.len = len; this.hit = hit; this.at = at; this.edits = edits; }
        }

        static Vector3 V3(float x, float y = 0f, float z = 0f) { return PlayerAnimator.Vec(x, y, z); }

        static readonly Dictionary<string, Gest> casterG = new Dictionary<string, Gest>
        {
            {"thrust", new Gest("thrust", 0.55f, 0.2f, new[]{0.10f,0.20f,0.55f},
                p=>{ p.arR=V3(-120,0,12); p.arL=V3(-100,0,-12); p.body=V3(-10); },
                p=>{ p.arR=V3(-82,0,0); p.fR=V3(-4); p.arL=V3(-88,0,-10); p.fL=V3(-6); p.body=V3(14); p.lunge=0.14f; p.lgL=V3(-18); p.lgR=V3(14); },
                p=>{ p.lunge=0.1f; })},
            {"push", new Gest("push", 0.6f, 0.22f, new[]{0.12f,0.22f,0.6f},
                p=>{ p.arR=V3(-70,10,8); p.arL=V3(-70,-10,-8); p.fR=V3(-95); p.fL=V3(-95); p.body=V3(-12); p.lgL=V3(8); },
                p=>{ p.arR=V3(-92,0,4); p.arL=V3(-92,0,-4); p.fR=V3(0); p.fL=V3(0); p.body=V3(18); p.lunge=0.2f; p.lgL=V3(-26); p.lgR=V3(16); },
                p=>{ p.lunge=0.16f; })},
            {"slam", new Gest("slam", 0.65f, 0.24f, new[]{0.12f,0.24f,0.65f},
                p=>{ p.arR=V3(-175,0,10); p.arL=V3(-165,0,-10); p.body=V3(-22); },
                p=>{ p.arR=V3(-10,0,4); p.arL=V3(-14,0,-4); p.fR=V3(-6); p.fL=V3(-6); p.body=V3(32); p.lunge=0.12f; p.lgL=V3(-24); },
                p=>{ p.body=V3(28); })},
            {"sweep", new Gest("sweep", 0.65f, 0.26f, new[]{0.12f,0.28f,0.65f},
                p=>{ p.body=V3(0,-50,0); p.arR=V3(-70,60,20); p.arL=V3(-60,40,-10); },
                p=>{ p.body=V3(6,55,0); p.arR=V3(-80,-50,10); p.arL=V3(-70,-40,-12); p.lunge=0.1f; },
                p=>{ p.lunge=0.06f; })},
            {"backhand", new Gest("backhand", 0.65f, 0.26f, new[]{0.12f,0.28f,0.65f},
                p=>{ p.body=V3(0,50,0); p.arR=V3(-70,-60,10); p.arL=V3(-50,-40,-10); },
                p=>{ p.body=V3(0,-55,0); p.arR=V3(-80,60,20); p.arL=V3(-60,40,-12); p.lunge=0.1f; },
                p=>{ p.lunge=0.06f; })},
            {"spin", new Gest("spin", 0.95f, 0.66f, new[]{0.16f,0.32f,0.48f,0.64f,0.95f},
                p=>{ p.body=V3(0,90,0); p.arR=V3(-40,0,60); p.arL=V3(-40,0,-60); },
                p=>{ p.body=V3(0,180,0); },
                p=>{ p.body=V3(0,270,0); },
                p=>{ p.body=V3(10,360,0); p.arR=V3(-85,0,0); p.arL=V3(-90,0,-8); p.lunge=0.14f; },
                p=>{ p.lunge=0.1f; })},
            {"stab", new Gest("stab", 0.7f, 0.26f, new[]{0.12f,0.26f,0.7f},
                p=>{ p.arR=V3(-130,0,10); p.arL=V3(-100,0,-10); p.body=V3(-12); },
                p=>{ p.arR=V3(35,0,0); p.arL=V3(10,0,-6); p.body=V3(34); p.head=V3(20); p.lgL=V3(-34); p.lgR=V3(24); p.lift=-0.2f; },
                p=>{ p.lift=-0.16f; })},
            {"burst", new Gest("burst", 0.65f, 0.22f, new[]{0.10f,0.22f,0.65f},
                p=>{ p.arR=V3(-80,-20,0); p.arL=V3(-80,20,0); p.fR=V3(-100); p.fL=V3(-100); p.body=V3(6); },
                p=>{ p.arR=V3(-35,0,80); p.arL=V3(-35,0,-80); p.fR=V3(-8); p.fL=V3(-8); p.body=V3(-6); p.head=V3(-12); },
                p=>{ p.head=V3(-6); })},
            {"clap", new Gest("clap", 0.7f, 0.3f, new[]{0.10f,0.20f,0.30f,0.7f},
                p=>{ p.arR=V3(-150,0,10); p.arL=V3(-150,0,-10); },
                p=>{ p.arR=V3(-80,30,0); p.arL=V3(-80,-30,0); p.fR=V3(-60); p.fL=V3(-60); p.body=V3(6); },
                p=>{ p.arR=V3(-14,0,6); p.arL=V3(-14,0,-6); p.fR=V3(-8); p.fL=V3(-8); p.body=V3(28); p.lgL=V3(-20); },
                p=>{ p.body=V3(24); })},
            {"throw", new Gest("throw", 0.6f, 0.26f, new[]{0.14f,0.28f,0.6f},
                p=>{ p.arR=V3(-30,-40,40); p.arL=V3(-80,10,-20); p.body=V3(0,-40,0); },
                p=>{ p.arR=V3(-60,20,0); p.arL=V3(-40,10,-10); p.body=V3(14,35,0); p.lunge=0.18f; p.lgL=V3(-22); p.lgR=V3(16); },
                p=>{ p.arR=V3(10,0,0); p.lunge=0.1f; })},
            {"skypull", new Gest("skypull", 0.65f, 0.26f, new[]{0.14f,0.26f,0.65f},
                p=>{ p.lift=p.lift+0.2f; },
                p=>{ p.arR=V3(-30,0,20); p.arL=V3(-30,0,-20); p.fR=V3(-110); p.fL=V3(-110); p.body=V3(30); p.head=V3(18); p.lgL=V3(-30); p.lgR=V3(30); p.lift=-0.15f; },
                p=>{ p.body=V3(26); })},
            {"scoop", new Gest("scoop", 0.65f, 0.26f, new[]{0.10f,0.26f,0.65f},
                p=>{ p.arR=V3(25,0,10); p.arL=V3(25,0,-10); p.body=V3(30); p.lift=-0.1f; },
                p=>{ p.arR=V3(-120,0,10); p.arL=V3(-110,0,-10); p.body=V3(-14); p.lift=0.1f; },
                p=>{ p.lift=0.06f; })},
            {"twirl", new Gest("twirl", 0.85f, 0.58f, new[]{0.12f,0.24f,0.36f,0.48f,0.6f,0.85f},
                p=>{ p.arR=V3(-170,0,10); },
                p=>{ p.arR=V3(-170,90,10); p.body=V3(0,40,0); },
                p=>{ p.arR=V3(-170,180,10); p.body=V3(0,120,0); },
                p=>{ p.arR=V3(-120,270,10); p.body=V3(0,200,0); },
                p=>{ p.arR=V3(-82,360,0); p.body=V3(14,360,0); p.lunge=0.14f; },
                p=>{ p.lunge=0.1f; })},
            {"embrace", new Gest("embrace", 0.7f, 0.3f, new[]{0.14f,0.30f,0.7f},
                p=>{ p.arR=V3(-30,0,75); p.arL=V3(-30,0,-75); p.head=V3(-14); },
                p=>{ p.arR=V3(-70,-35,0); p.arL=V3(-70,35,0); p.fR=V3(-100); p.fL=V3(-100); p.head=V3(12); p.body=V3(6); },
                p=>{ p.head=V3(8); })},
            {"pray", new Gest("pray", 0.75f, 0.34f, new[]{0.16f,0.34f,0.75f},
                p=>{ p.arR=V3(-75,30,6); p.arL=V3(-75,-30,-6); p.fR=V3(-100); p.fL=V3(-100); p.head=V3(10); },
                p=>{ p.arR=V3(-170,10,12); p.arL=V3(-170,-10,-12); p.fR=V3(-20); p.fL=V3(-20); p.head=V3(-24); p.body=V3(-12); p.lift=p.lift+0.35f; },
                p=>{ p.head=V3(-18); })},
            {"leap", new Gest("leap", 0.7f, 0.34f, new[]{0.10f,0.22f,0.34f,0.55f},
                p=>{ p.body=V3(20); p.lift=-0.25f; p.lgL=V3(-30); p.lgR=V3(30); },
                p=>{ p.lift=0.8f; p.arR=V3(-110,0,10); p.arL=V3(-100,0,-10); p.lgL=V3(10); p.lgR=V3(-10); },
                p=>{ p.arR=V3(-82,0,0); p.arL=V3(-88,0,-10); p.body=V3(18); p.lunge=0.16f; },
                p=>{ p.lift=0.1f; })},
            {"stomp", new Gest("stomp", 0.6f, 0.2f, new[]{0.10f,0.20f,0.6f},
                p=>{ p.lgL=V3(-70); p.arR=V3(-160,0,10); p.arL=V3(-130,0,-10); p.body=V3(-14); },
                p=>{ p.lgL=V3(6); p.body=V3(24); p.arR=V3(-30,0,6); p.arL=V3(-30,0,-6); p.lift=-0.14f; },
                p=>{ p.lift=-0.08f; })},
            {"flick", new Gest("flick", 0.4f, 0.14f, new[]{0.06f,0.14f,0.4f},
                p=>{ p.arR=V3(-100,10,6); },
                p=>{ p.arR=V3(-70,-10,0); p.arL=V3(-30,0,-10); p.body=V3(6); p.lunge=0.05f; },
                p=>{ p.lunge=0.03f; })},
        };

        static readonly Dictionary<string, string[]> casterByKind = new Dictionary<string, string[]>
        {
            {"bolt", new[]{"thrust","throw","flick","push","leap","twirl","spin"}},
            {"multi", new[]{"flick","spin","twirl","throw","push","leap","thrust"}},
            {"cone", new[]{"sweep","backhand","scoop","push","twirl"}},
            {"nova", new[]{"burst","clap","stomp","spin","skypull"}},
            {"ground", new[]{"slam","stab","skypull","stomp","clap","scoop"}},
            {"line", new[]{"push","thrust","leap","sweep","throw"}},
            {"dash", new[]{"leap","stomp","thrust","spin"}},
            {"buff", new[]{"embrace","pray","burst","stab"}},
            {"heal", new[]{"embrace","pray","burst"}},
            {"summon", new[]{"stab","scoop","slam","clap","stomp"}},
            {"trap", new[]{"stab","scoop","slam","clap","stomp"}},
        };

        public static string KindOf(SkillDef s) { return InferKind(s); }

        static string InferKind(SkillDef s)
        {
            if (!string.IsNullOrEmpty(s.kind)) return s.kind;
            string t = ((s.name ?? "") + " " + (s.desc ?? "")).ToLowerInvariant();
            if (t.Contains("pulih") || t.Contains("heal") || t.Contains("sembuh") || t.Contains("rebirth") || t.Contains("regen")) return "heal";
            if (t.Contains("damage masuk") || t.Contains("tabir") || t.Contains("perisai") || t.Contains("barrier") || t.Contains("kebal") || t.Contains("shield") || t.Contains("buff")) return "buff";
            if (t.Contains("melompat") || t.Contains("meluncur") || t.Contains("dash") || t.Contains("terjang") || t.Contains("mundur") || t.Contains("step") || t.Contains("comet")) return "dash";
            if (t.Contains("kerucut") || t.Contains("gelombang") || t.Contains("semburan") || t.Contains("menyapu") || t.Contains("wave")) return "cone";
            if (t.Contains("cincin") || t.Contains("sekeliling") || t.Contains("sekitar") || t.Contains("ring") || t.Contains("nova")) return "nova";
            if (t.Contains("hujan") || t.Contains("meteor") || t.Contains("gempa") || t.Contains("tanah") || t.Contains("area") || t.Contains("titik") || t.Contains("quake") || t.Contains("rain")) return "ground";
            if (t.Contains("menembus") || t.Contains("garis") || t.Contains("sinar") || t.Contains("lurus") || t.Contains("jet") || t.Contains("beam")) return "line";
            if (t.Contains("perangkap") || t.Contains("jaring") || t.Contains("trap") || t.Contains("snare")) return "trap";
            if (t.Contains("panggil") || t.Contains("summon") || t.Contains("hunt")) return "summon";
            return "bolt";
        }

        // penugasan unik: dalam satu (job, tier) tidak ada gerakan kembar selama masih ada kandidat; varian gerak & charge ikut berputar
        static readonly Dictionary<string, Dictionary<string, int>> used = new Dictionary<string, Dictionary<string, int>>();
        static readonly Dictionary<string, int> counter = new Dictionary<string, int>();

        static string PickG(SkillDef s, string[] cand, out int variant)
        {
            string grp = s.job + (s.tier >= 3 ? "|U" : "|N");
            if (!used.ContainsKey(grp)) used[grp] = new Dictionary<string, int>();
            int c; counter.TryGetValue(grp, out c); counter[grp] = c + 1;
            variant = c;
            string best = cand[0]; int bu = int.MaxValue;
            for (int i = 0; i < cand.Length; i++)
            {
                int u; used[grp].TryGetValue(cand[i], out u);
                if (u < bu) { bu = u; best = cand[i]; }
            }
            used[grp][best] = bu + 1;
            return best;
        }

        // ------------------------------------------------------------------ penyihir (Fire/Earth/Water/Wind/Priest)
        static ActionDef BuildCaster(SkillDef s)
        {
            bool ult = s.tier >= 3, priest = s.job == "Priest";
            string k = InferKind(s);
            string[] cand; if (!casterByKind.TryGetValue(k, out cand)) cand = casterByKind["bolt"];
            if (priest && (k == "bolt" || k == "multi" || k == "line")) cand = new[] { "pray", "push", "thrust", "burst" };
            int variant; var g = casterG[PickG(s, cand, out variant)];
            float charge = ult ? 2.6f : 0.55f, rec = ult ? 1.2f : 0.5f, hold = g.len;
            var d = new ActionDef { id = s.id };
            var n = PlayerAnimator.Base();
            var stance = PlayerAnimator.Mk(n, p => { p.arR = VV(-30f, 18f, 10f); p.fR = VV(-40f, 0f, 0f); p.arL = VV(-24f, -12f, -10f); p.fL = VV(-30f, 0f, 0f); p.body = VV(2f, -14f, 0f); p.head = VV(4f, 0f, 0f); p.lgL = VV(-8f, 0f, 0f); p.lgR = VV(10f, 0f, 0f); p.lift = -0.04f; });
            if (priest) stance = PlayerAnimator.Mk(stance, p => { p.arR = VV(-70f, 30f, 10f); p.fR = VV(-100f, 0f, 0f); p.arL = VV(-70f, -30f, -10f); p.fL = VV(-100f, 0f, 0f); p.head = VV(8f, 0f, 0f); });
            var gather = PlayerAnimator.Mk(stance, p => { p.arR = VV(-72f, 14f, 8f); p.fR = VV(-96f, 0f, 0f); p.arL = VV(-62f, -18f, -8f); p.fL = VV(-98f, 0f, 0f); p.body = VV(4f, 0f, 0f); p.head = VV(10f, 0f, 0f); });
            PlayerAnimator.Pose mid = gather, peak;
            if (ult)
            {
                mid = PlayerAnimator.Mk(gather, p => { p.arR = VV(-110f, 10f, 12f); p.arL = VV(-100f, -10f, -12f); p.body = VV(-4f, 0f, 0f); p.lift = 0.9f; p.lgL = VV(4f, 0f, 0f); p.lgR = VV(-4f, 0f, 0f); });
                peak = PlayerAnimator.Mk(mid, p => { p.arR = VV(-165f, 0f, 16f); p.fR = VV(-20f, 0f, 0f); p.arL = VV(-150f, 0f, -16f); p.fL = VV(-20f, 0f, 0f); p.body = VV(-14f, 0f, 0f); p.head = VV(-16f, 0f, 0f); p.lift = 2.2f; p.lgL = VV(8f, 0f, 0f); p.lgR = VV(-8f, 0f, 0f); });
            }
            else
                peak = PlayerAnimator.Mk(gather, p => { p.arR = VV(-120f, 8f, 10f); p.fR = VV(-50f, 0f, 0f); p.arL = VV(-90f, -10f, -12f); p.fL = VV(-40f, 0f, 0f); p.body = VV(-6f, 0f, 0f); p.head = VV(-6f, 0f, 0f); p.lift = 0.04f; });
            d.Key(0f, n).Key(charge * 0.18f, stance).Key(charge * 0.5f, ult ? mid : gather).Key(charge, peak);
            // gerakan release unik skill ini
            var prev = peak;
            for (int i = 0; i < g.edits.Length; i++)
            {
                var edit = g.edits[i]; float lf = ult ? 1.25f : 1f;
                prev = PlayerAnimator.Mk(prev, p => edit(p));
                d.Key(charge + g.at[i] * lf, prev);
            }
            float t0 = charge, endG = charge + hold * (ult ? 1.25f : 1f);
            d.hitAt = charge + g.hit * (ult ? 1.25f : 1f);
            // mendarat satu kaki lebih dulu
            var land1 = PlayerAnimator.Mk(prev, p => { p.arR = VV(-60f, 10f, 20f); p.arL = VV(-50f, -10f, -20f); p.lift = ult ? 0.5f : 0f; p.body = VV(8f, 0f, 0f); p.lgL = VV(-30f, 0f, 0f); p.lgR = VV(12f, 0f, 0f); p.lunge = 0.05f; p.head = VV(0f, 0f, 0f); });
            var land2 = PlayerAnimator.Mk(land1, p => { p.lift = -0.06f; p.lgL = VV(-14f, 0f, 0f); p.lgR = VV(4f, 0f, 0f); p.body = VV(10f, 0f, 0f); });
            var ready = PlayerAnimator.Mk(n, p => { p.arR = VV(-26f, 22f, 10f); p.fR = VV(-46f, 0f, 0f); p.arL = VV(-36f, 8f, -10f); p.body = VV(2f, 0f, 0f); });
            if (ult) d.Key(endG + rec * 0.55f, land1).Key(endG + rec * 0.8f, land2); else d.Key(endG + rec * 0.5f, land1);
            d.dur = endG + rec; d.Key(d.dur, ready);
            d.lockUntil = d.dur - (ult ? 0.35f : 0.12f);

            Color m, h, kk; SkillSpectacle.Colors(s, out m, out h, out kk);
            int cv = variant % 5, mv = ult ? 0 : (variant / 2) % 4;
            d.At(0f, pc =>
            {
                Vector3 feet = pc.transform.position - Vector3.up * 0.9f;
                if (k == "ground" || k == "trap" || k == "summon" || (k == "multi" && s.radius > 0f)) { Vector3 C = SkillExec.AimGroundFor(pc, Mathf.Min(s.range > 0f ? s.range : 22f, 34f)); MagicFx.MagicCircle(C, Mathf.Max(2f, s.radius), m, charge); MagicFx.MagicCircle(C, Mathf.Max(2f, s.radius) * 0.62f, h, charge); }
                else if (k == "nova") { MagicFx.MagicCircle(feet, Mathf.Max(3f, s.radius), m, charge); MagicFx.MagicCircle(feet, Mathf.Max(3f, s.radius) * 0.6f, h, charge); }
                MagicFx.MagicCircle(feet, ult ? 3.2f : 2.4f, m, charge + 0.3f);
                if (!ult) MagicFx.MagicCircle(feet, 1.5f, h, charge + 0.2f);
                if (ult)
                {
                    CombatFx.Host.I.StartCoroutine(LateP(charge * 0.2f, () => { if (pc != null) MagicFx.MagicCircle(pc.transform.position - Vector3.up * 0.9f, 5.2f, h, charge * 0.8f + 0.3f); }));
                    CombatFx.Host.I.StartCoroutine(LateP(charge * 0.4f, () => { if (pc != null) MagicFx.MagicCircle(pc.transform.position - Vector3.up * 0.9f, 7.6f, m, charge * 0.6f + 0.3f); }));
                    CombatFx.Host.I.StartCoroutine(LateP(charge * 0.6f, () => { if (pc != null) MagicFx.MagicCircle(pc.transform.position - Vector3.up * 0.9f, 10f, kk, charge * 0.4f + 0.3f); }));
                    ThirdPersonCamera.Cinematic(0.9f, 1.6f, 14f, charge * 0.45f, charge * 0.55f + hold + 0.4f, 1.4f);
                }
                CombatFx.Host.I.StartCoroutine(ChargeFx(pc, s, charge, ult, m, h, kk, true, cv));
            });
            if (ult)
            {
                d.At(charge * 0.35f, pc => { Vector3 f = pc.transform.position - Vector3.up * 0.9f; CombatFx.Dust(f, 20); CombatFx.ShockRing(f, 6f, m, 0.5f); CombatFx.ShockRing(f, 9f, h, 0.7f); ThirdPersonCamera.Shake(0.2f, 0.4f); Audio.Sfx("skill_cast", pc.transform.position); });
                d.At(charge * 0.8f, pc => { CombatFx.Flash(pc.transform.position + Vector3.up * 1.2f, h, 24f, 6f, 0.5f); ThirdPersonCamera.Shake(0.25f, 0.5f); });
                d.At(endG + rec * 0.7f, pc => { Vector3 f = pc.transform.position - Vector3.up * 0.9f; CombatFx.Dust(f, 18); CombatFx.ShockRing(f, 5f, kk, 0.4f); });
            }
            d.At(charge, pc => { CombatFx.Flash(MagicFx.Tip(), h, 18f, 5f, 0.3f); CombatFx.ShockRing(pc.transform.position - Vector3.up * 0.8f, ult ? 7f : 4f, m, 0.4f); ThirdPersonCamera.Shake(ult ? 0.4f : 0.2f, 0.3f); });
            // peristiwa khas gerakan
            float hitT = g.hit * (ult ? 1.25f : 1f);
            if (g.name == "slam" || g.name == "stab" || g.name == "stomp" || g.name == "clap" || g.name == "skypull")
                d.At(charge + hitT - 0.01f, pc => { Vector3 f = pc.transform.position - Vector3.up * 0.9f + pc.transform.forward * 0.8f; CombatFx.ShockRing(f, 4.5f, m, 0.35f); CombatFx.Dust(f, 12); CombatFx.Burst(f, kk, 8, 1.5f, 2.5f, 0.7f, 0.14f); ThirdPersonCamera.Shake(0.3f, 0.2f); });
            if (g.name == "spin" || g.name == "twirl")
                d.At(charge + 0.12f, pc => { CombatFx.Host.I.StartCoroutine(SpinTrail(pc, m, h, 0.5f * (ult ? 1.25f : 1f))); });
            if (g.name == "sweep" || g.name == "backhand")
                d.At(charge + 0.14f, pc => { CombatFx.SweepArc(MagicFx.Tip(), pc.transform.rotation, 3.2f, 28f, 0.22f, g.name == "sweep" ? -70f : 70f, g.name == "sweep" ? 70f : -70f, 0.18f, 0.35f, m, true); });
            if (g.name == "leap") d.At(charge + 0.1f, pc => { pc.DashTo(pc.transform.forward, 0.25f, 4f); CombatFx.Dust(pc.transform.position - Vector3.up * 0.8f, 10); });
            if (g.name == "pray" || g.name == "embrace") d.At(charge + 0.2f, pc => { CombatFx.Host.I.StartCoroutine(RisingMotes(pc, h, m, 0.6f)); });
            // variasi gerak badan antar skill dalam gerakan yang sama
            if (mv == 1) d.At(charge + hitT - 0.05f, pc => pc.DashTo(pc.transform.forward, 0.18f, 6f));
            else if (mv == 2) d.At(charge + hitT, pc => { pc.DashTo(-pc.transform.forward, 0.2f, 7f); CombatFx.Dust(pc.transform.position - Vector3.up * 0.8f, 6); });
            else if (mv == 3) d.At(charge + hitT - 0.05f, pc => pc.DashTo(pc.transform.right * (variant % 2 == 0 ? 1f : -1f), 0.2f, 5f));
            return d;
        }

        static IEnumerator SpinTrail(PlayerCombat pc, Color m, Color h, float secs)
        {
            for (float t = 0f; t < secs && pc != null; t += 0.04f)
            {
                float a = t * 14f; Vector3 c = pc.transform.position + Vector3.up * 1.1f;
                Vector3 p1 = c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 1.9f, p2 = c + new Vector3(Mathf.Cos(a - 0.5f), 0f, Mathf.Sin(a - 0.5f)) * 1.9f;
                CombatFx.Beam(p2, p1, m, 0.3f, 0.3f); CombatFx.Beam(p2, p1, h, 0.1f, 0.25f);
                yield return new WaitForSeconds(0.04f);
            }
        }

        static IEnumerator RisingMotes(PlayerCombat pc, Color h, Color m, float secs)
        {
            for (float t = 0f; t < secs && pc != null; t += 0.05f)
            {
                float a = t * 9f; Vector3 b = pc.transform.position - Vector3.up * 0.8f + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 1.3f;
                CombatFx.Beam(b, b + Vector3.up * (1.5f + t * 4f), t % 0.1f < 0.05f ? h : m, 0.12f, 0.35f);
                CombatFx.Burst(b + Vector3.up * t * 3f, h, 2, 0.2f, 1.5f, 0.9f, 0.1f);
                yield return new WaitForSeconds(0.05f);
            }
        }

        // ------------------------------------------------------------------ pemanah: gerakan unik
        sealed class AG { public string name; public System.Action<PlayerAnimator.Pose> hold; public System.Action<PlayerAnimator.Pose>[] rel; public float[] at; public float len, hit; public AG(string n, System.Action<PlayerAnimator.Pose> hold, float len, float hit, float[] at, params System.Action<PlayerAnimator.Pose>[] rel) { name = n; this.hold = hold; this.len = len; this.hit = hit; this.at = at; this.rel = rel; } }

        static readonly System.Action<PlayerAnimator.Pose> aRel = p => { p.arR = V3(-80, 62, 20); p.fR = V3(-60); };
        static readonly System.Action<PlayerAnimator.Pose> aFol = p => { p.arR = V3(-76, 68, 22); p.fR = V3(-50); };

        static readonly Dictionary<string, AG> archerG = new Dictionary<string, AG>
        {
            {"std", new AG("std", p=>{}, 0.45f, 0.05f, new[]{0.05f,0.25f}, aRel, aFol)},
            {"kneel", new AG("kneel", p=>{ p.body=V3(26,-26,0); p.lgL=V3(-44); p.lgR=V3(40); p.lift=-0.4f; p.head=V3(-6,40,0); }, 0.5f, 0.05f, new[]{0.05f,0.3f}, aRel, aFol)},
            {"lob", new AG("lob", p=>{ p.body=V3(-24,-30,0); p.arL=V3(-140,-6,-4); p.head=V3(-20,40,0); p.lgL=V3(-12); p.lgR=V3(14); }, 0.5f, 0.05f, new[]{0.05f,0.3f}, aRel, aFol)},
            {"hip", new AG("hip", p=>{ p.arL=V3(-62,-6,-4); p.arR=V3(-60,34,8); p.fR=V3(-100); p.body=V3(4,-20,0); }, 0.35f, 0.04f, new[]{0.04f,0.2f}, aRel, aFol)},
            {"spin", new AG("spin", p=>{}, 0.9f, 0.62f, new[]{0.12f,0.25f,0.38f,0.5f,0.62f,0.9f}, p=>{ p.body=V3(0,60,0); p.arR=V3(-40,20,50); p.arL=V3(-40,0,-50); }, p=>{ p.body=V3(0,160,0); }, p=>{ p.body=V3(0,260,0); }, p=>{ p.body=V3(0,330,0); p.arR=V3(-88,38,8); p.arL=V3(-92,-6,-4); p.fR=V3(-130); }, aRel, aFol)},
            {"backflip", new AG("backflip", p=>{}, 0.8f, 0.3f, new[]{0.10f,0.22f,0.3f,0.5f,0.8f}, p=>{ p.lift=-0.3f; p.body=V3(20,-28,0); }, p=>{ p.lift=1.0f; p.body=V3(-40,-28,0); }, aRel, p=>{ p.lift=0.5f; p.body=V3(-10,-28,0); }, aFol)},
            {"stabarrow", new AG("stabarrow", p=>{ p.body=V3(24,-20,0); p.lgL=V3(-28); p.lgR=V3(28); p.lift=-0.2f; }, 0.55f, 0.18f, new[]{0.1f,0.18f,0.5f}, p=>{ p.arR=V3(-150,20,10); p.fR=V3(-60); }, p=>{ p.arR=V3(30,10,0); p.fR=V3(-10); p.arL=V3(-20,0,-6); p.body=V3(34,-10,0); }, p=>{ p.body=V3(28,-10,0); })},
            {"sky", new AG("sky", p=>{ p.body=V3(-36,-24,0); p.arL=V3(-155,-6,-4); p.head=V3(-30,40,0); p.lift=0.05f; }, 0.5f, 0.05f, new[]{0.05f,0.3f}, aRel, aFol)},
            {"sprint", new AG("sprint", p=>{ p.lunge=0.3f; p.lgL=V3(-34); p.lgR=V3(28); p.body=V3(14,-26,0); }, 0.55f, 0.05f, new[]{0.05f,0.3f}, p=>{ p.arR=V3(-80,62,20); p.fR=V3(-60); p.lunge=0.45f; }, p=>{ p.arR=V3(-76,68,22); p.lunge=0.3f; })},
            {"point", new AG("point", p=>{ p.arL=V3(-100,-14,-4); p.body=V3(0,-36,0); }, 0.5f, 0.05f, new[]{0.05f,0.3f}, aRel, aFol)},
            {"overpull", new AG("overpull", p=>{ p.body=V3(-8,-36,0); p.lift=0.5f; p.arR=V3(-88,44,8); p.fR=V3(-146); }, 0.7f, 0.06f, new[]{0.06f,0.4f}, aRel, aFol)},
        };

        static readonly Dictionary<string, string[]> archerByKind = new Dictionary<string, string[]>
        {
            {"bolt", new[]{"std","point","hip","kneel","sprint","sky","lob"}},
            {"multi", new[]{"hip","spin","lob","sky","std","sprint","point"}},
            {"ground", new[]{"lob","stabarrow","kneel","sky"}},
            {"trap", new[]{"stabarrow","kneel","hip","lob"}},
            {"line", new[]{"std","point","sprint","kneel"}},
            {"nova", new[]{"spin","backflip","stabarrow"}},
            {"dash", new[]{"backflip","sprint","spin"}},
            {"buff", new[]{"point","hip","std"}},
            {"heal", new[]{"point","hip"}},
            {"summon", new[]{"stabarrow","sky","lob","point"}},
        };

        static ActionDef BuildArcher(SkillDef s)
        {
            bool ult = s.tier >= 3, mobile = s.kind == "dash" || s.id == "ar_mob_4" || s.id == "ar_mob_5";
            string k = InferKind(s);
            if (s.id == "ar_mob_4") k = "dash"; if (s.id == "ar_mob_5") k = "dash";
            string[] cand; if (!archerByKind.TryGetValue(k, out cand)) cand = archerByKind["bolt"];
            if (ult) cand = new[] { "overpull", "sky", "lob", "spin", "kneel", "stabarrow", "sprint", "point", "std" };
            int variant; var g = archerG[PickG(s, cand, out variant)];
            float charge = ult ? 1.9f : (mobile ? 0.3f : 0.6f), rec = ult ? 0.9f : 0.5f;
            var d = new ActionDef { id = s.id };
            var n = PlayerAnimator.Base();
            var stance = PlayerAnimator.Mk(n, p => { p.arL = VV(-40f, -6f, -4f); p.fL = VV(-8f, 0f, 0f); p.arR = VV(-16f, 20f, 8f); p.fR = VV(-20f, 0f, 0f); p.body = VV(0f, -28f, 0f); p.head = VV(0f, 36f, 0f); p.lgL = VV(6f, 0f, 0f); p.lgR = VV(-8f, 0f, 0f); });
            var draw = PlayerAnimator.Mk(stance, p => { p.arL = VV(-92f, -6f, -4f); p.fL = VV(-4f, 0f, 0f); p.arR = VV(-88f, 38f, 8f); p.fR = VV(-128f, 0f, 0f); p.body = VV(0f, -32f, 0f); p.head = VV(0f, 40f, 0f); });
            draw = PlayerAnimator.Mk(draw, p => g.hold(p));
            var anchor = PlayerAnimator.Mk(draw, p => { p.fR = VV(-136f, 0f, 0f); });
            d.Key(0f, n).Key(charge * 0.22f, PlayerAnimator.Mk(stance, p => g.hold(p))).Key(charge * 0.65f, draw).Key(charge, anchor);
            var prev = anchor;
            for (int i = 0; i < g.rel.Length; i++) { var e = g.rel[i]; prev = PlayerAnimator.Mk(prev, p => e(p)); d.Key(charge + g.at[i], prev); }
            d.hitAt = charge + g.hit;
            var lower = PlayerAnimator.Mk(prev, p => { p.arL = VV(-36f, -6f, -4f); p.arR = VV(-24f, 22f, 8f); p.fR = VV(-26f, 0f, 0f); p.body = VV(0f, -12f, 0f); p.head = VV(0f, 12f, 0f); p.lift = 0f; p.lgL = VV(0f, 0f, 0f); p.lgR = VV(0f, 0f, 0f); p.lunge = 0f; });
            var ready = PlayerAnimator.Mk(lower, p => { p.arL = VV(-20f, 0f, -8f); p.fL = VV(-10f, 0f, 0f); p.arR = VV(-120f, 24f, 10f); p.fR = VV(-30f, 0f, 0f); p.body = VV(0f, 0f, 0f); p.head = VV(0f, 0f, 0f); });
            float endG = charge + g.len;
            d.Key(endG + rec * 0.6f, lower); d.dur = endG + rec; d.Key(d.dur, ready); d.lockUntil = d.dur - 0.1f;

            Color m, h, kk; SkillSpectacle.Colors(s, out m, out h, out kk);
            int cv = variant % 5;
            d.At(0f, pc =>
            {
                Vector3 feet = pc.transform.position - Vector3.up * 0.9f;
                if (k == "ground" || k == "trap" || k == "summon" || (k == "multi" && s.radius > 0f)) { Vector3 C = SkillExec.AimGroundFor(pc, Mathf.Min(s.range > 0f ? s.range : 24f, 34f)); MagicFx.MagicCircle(C, Mathf.Max(2.5f, s.radius), m, charge); MagicFx.MagicCircle(C, Mathf.Max(2.5f, s.radius) * 0.6f, h, charge); }
                else if (k == "nova") MagicFx.MagicCircle(feet, Mathf.Max(3f, s.radius), m, charge);
                MagicFx.MagicCircle(feet, ult ? 3.4f : 2.2f, m, charge + 0.2f);
                if (ult) { CombatFx.Host.I.StartCoroutine(LateP(charge * 0.35f, () => { if (pc != null) MagicFx.MagicCircle(pc.transform.position - Vector3.up * 0.9f, 5.4f, h, charge * 0.65f + 0.3f); })); CombatFx.Host.I.StartCoroutine(LateP(charge * 0.55f, () => { if (pc != null) MagicFx.MagicCircle(pc.transform.position - Vector3.up * 0.9f, 8f, kk, charge * 0.45f + 0.3f); })); ThirdPersonCamera.Cinematic(0.25f, 0.6f, 8f, charge * 0.5f, charge * 0.5f + 0.4f, 0.9f); }
                CombatFx.Host.I.StartCoroutine(ChargeFx(pc, s, charge, ult, m, h, kk, false, cv));
            });
            d.At(charge * 0.25f, pc => { PlayerAnimator.HoldArrow = true; Audio.Sfx("click", pc.transform.position); });
            d.At(charge - 0.001f, pc => { PlayerAnimator.HoldArrow = false; });
            d.At(charge, pc => { CombatFx.Flash(pc.transform.position + Vector3.up * 1.4f + pc.transform.forward * 0.7f, h, 16f, 4.5f, 0.25f); CombatFx.ShockRing(pc.transform.position - Vector3.up * 0.8f, ult ? 6f : 3.5f, m, 0.35f); ThirdPersonCamera.Shake(ult ? 0.35f : 0.16f, 0.25f); });
            if (g.name == "backflip") d.At(charge + 0.1f, pc => { pc.DashTo(-pc.transform.forward, 0.4f, 8f); CombatFx.Dust(pc.transform.position - Vector3.up * 0.8f, 10); });
            if (g.name == "sprint") d.At(charge - 0.05f, pc => { pc.DashTo(pc.transform.forward, 0.25f, 7f); CombatFx.Dust(pc.transform.position - Vector3.up * 0.8f, 8); });
            if (g.name == "spin") d.At(charge + 0.1f, pc => { CombatFx.Host.I.StartCoroutine(SpinTrail(pc, m, h, 0.5f)); });
            if (g.name == "stabarrow") d.At(charge + g.hit - 0.01f, pc => { Vector3 f = pc.transform.position + pc.transform.forward * 1.2f - Vector3.up * 0.9f; CombatFx.ShockRing(f, 4f, m, 0.3f); CombatFx.Dust(f, 10); ThirdPersonCamera.Shake(0.2f, 0.2f); });
            return d;
        }

        // ------------------------------------------------------------------ VFX charge (energi tersedot ke senjata + lingkungan)
        static IEnumerator LateP(float t, System.Action a) { yield return new WaitForSeconds(t); if (a != null) a(); }

        static IEnumerator ChargeFx(PlayerCombat pc, SkillDef s, float secs, bool ult, Color m, Color h, Color k, bool caster, int variant)
        {
            string e = !string.IsNullOrEmpty(s.elem) ? s.elem : JobInfo.Elem(s.job);
            float t = 0f, step = ult ? 0.04f : 0.05f;
            while (t < secs && pc != null)
            {
                float u = t / secs;
                Vector3 tip = caster ? MagicFx.Tip() : pc.transform.position + Vector3.up * 1.4f + pc.transform.forward * 0.7f;
                Vector3 body = pc.transform.position + Vector3.up * 0.2f;
                float R = (ult ? 5.5f : 2.6f) * (1.1f - 0.8f * u);
                float th = ult ? 0.26f : 0.16f;
                switch (variant)
                {
                    case 0:   // aliran partikel tersedot ke senjata
                        for (int i = 0; i < (ult ? 6 : 3); i++) { Vector3 o = tip + Random.onUnitSphere * R; CombatFx.Beam(o, Vector3.Lerp(o, tip, 0.55f), Random.value < 0.5f ? m : h, th, 0.16f); CombatFx.Beam(o, Vector3.Lerp(o, tip, 0.55f), k, th * 2.4f, 0.12f); }
                        break;
                    case 1:   // cincin spiral naik mengelilingi badan
                        for (int i = 0; i < 3; i++) { float a = t * 7f + i * 2.1f, y = Mathf.Repeat(t * 1.8f + i * 0.7f, 2.4f); Vector3 q = body + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (1.3f + 0.3f * Mathf.Sin(t * 3f)) + Vector3.up * y; CombatFx.Beam(q, q + new Vector3(-Mathf.Sin(a), 0.35f, Mathf.Cos(a)) * 0.9f, m, th * 1.4f, 0.28f); CombatFx.Burst(q, h, 1, 0.2f, 0.5f, 0.6f, 0.1f); }
                        break;
                    case 2:   // bola orbit mengitari pemain, makin rapat
                        for (int i = 0; i < 4; i++) { float a = t * 5f + i * Mathf.PI / 2f; float rr = Mathf.Lerp(2.6f, 0.9f, u); Vector3 q = body + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * rr + Vector3.up * (1f + 0.4f * Mathf.Sin(a * 2f)); CombatFx.Flash(q, h, 5f, 1.6f, 0.1f); CombatFx.Burst(q, m, 2, 0.25f, 0.4f, 0.5f, 0.13f); CombatFx.Beam(q, tip, m, th * 0.5f, 0.12f); }
                        break;
                    case 3:   // gelombang energi merayap dari tanah ke badan
                        for (int i = 0; i < 4; i++) { float a = Random.value * 6.28f; Vector3 g = body + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * R * 1.4f; CombatFx.Beam(g, Vector3.Lerp(g, body + Vector3.up * 0.3f, 0.7f), m, th * 1.3f, 0.2f); CombatFx.Burst(g, k, 1, 0.3f, 0.6f, 0.6f, 0.12f); }
                        break;
                    default:  // pilar kecil berdenyut di sekeliling
                        if (Mathf.Repeat(t, 0.3f) < step) for (int i = 0; i < 4; i++) { float a = i * Mathf.PI / 2f + t; Vector3 q = body + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (2f + (ult ? 1.5f : 0f)); MagicFx.Column(q, 0.35f, 2.2f + 2f * u, m, 0.4f); CombatFx.Beam(q, q + Vector3.up * (2.6f + 2f * u), h, th * 0.8f, 0.3f); }
                        break;
                }
                CombatFx.Flash(tip, h, 5f + (ult ? 14f : 6f) * u, 1.2f + (ult ? 3.5f : 2f) * u, 0.12f);
                if (Random.value < (ult ? 0.95f : 0.55f)) EnvParticle(pc, e, ult, m, h, k, u);
                if (ult && Mathf.Repeat(t, 0.5f) < step) CombatFx.ShockRing(pc.transform.position - Vector3.up * 0.8f, 3f + 4f * u, m, 0.35f);
                yield return new WaitForSeconds(step);
                t += step;
            }
        }

        /// <summary>Partikel lingkungan khas elemen tertarik ke pusat: bara, debris, tetes air, daun/angin, cahaya.</summary>
        static void EnvParticle(PlayerCombat pc, string e, bool ult, Color m, Color h, Color k, float u)
        {
            Vector3 c = pc.transform.position - Vector3.up * 0.9f;
            Vector2 r = Random.insideUnitCircle.normalized * Random.Range(2f, ult ? 9f : 4f);
            Vector3 g = c + new Vector3(r.x, 0f, r.y);
            Vector3 to = Vector3.Lerp(g, c + Vector3.up * 1.2f, 0.6f);
            switch (e)
            {
                case "fire": CombatFx.Burst(g, m, 2, 0.3f, 2f + 2f * u, 0.9f, 0.12f); CombatFx.Beam(g, to, k, 0.08f, 0.3f); break;
                case "earth": MagicFx.Debri(g, 0.8f, 2, 0.5f); CombatFx.Dust(g, 2); CombatFx.Beam(g, to, k, 0.1f, 0.3f); break;
                case "water": CombatFx.Burst(g + Vector3.up * 2f, h, 2, 0.3f, -1f, 0.8f, 0.08f); CombatFx.Beam(g, to, m, 0.07f, 0.35f); break;
                case "wind": CombatFx.Beam(g + Vector3.up, g + Vector3.up + new Vector3(-r.y, 0f, r.x).normalized * 2.4f, h, 0.05f, 0.25f); CombatFx.Burst(g, m, 1, 0.3f, 0.5f, 0.7f, 0.1f); break;
                case "holy": CombatFx.Burst(g, h, 2, 0.2f, 3f, 1.2f, 0.09f); CombatFx.Beam(g, g + Vector3.up * 3f, m, 0.04f, 0.4f); break;
                default: CombatFx.Burst(g, m, 2, 0.3f, 1.5f, 0.8f, 0.1f); CombatFx.Beam(g, to, h, 0.04f, 0.3f); break;
            }
        }
    }
}
