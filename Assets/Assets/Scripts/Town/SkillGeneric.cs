using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Eksekutor skill berbasis data (job Water/Wind/Priest dan semua evolusi): kind = bolt/multi/cone/nova/ground/line/dash/buff/heal/summon/trap,
    /// elem = fire/earth/water/wind/holy/storm/phys/arrow, parameter tambahan di SkillDef.fx. Visual memakai MagicFx / CombatFx.
    /// </summary>
    public static partial class SkillExec
    {
        static Color ElemColor(string el)
        {
            if (el == "phys") return CombatFx.SwordCol;
            if (el == "arrow") return CombatFx.BowCol;
            return MagicFx.ElemCol(el);
        }

        static readonly string[] BuffKeys = { "dmg", "spd", "dmgred", "cdr", "crit", "lifesteal" };

        static void ApplyBuffs(SkillDef s, GameState gs, PlayerCombat pc)
        {
            float dur = s.Fx("dur", 6f);
            foreach (var k in BuffKeys) if (s.HasFx("b_" + k)) gs.AddTempBuff(k, s.Fx("b_" + k), dur);
        }

        static Vector3 FlatFwd(PlayerCombat pc) { Vector3 f = pc.transform.forward; f.y = 0f; return f.sqrMagnitude < 0.01f ? Vector3.forward : f.normalized; }

        /// <summary>Damage + efek tambahan pada satu monster.</summary>
        public static bool LastCrit;
        static bool QuietHit;        // true saat skill berkoreografi memberi feedback hit sendiri
        static void GHit(SkillDef s, PlayerCombat pc, Monster m, float dmg, Vector3 origin, bool pullToOrigin = false)
        {
            if (m == null || m.IsDead) return;
            float crit = s.Fx("crit", 0f);
            LastCrit = false;
            if (crit > 0f && Random.value * 100f < crit) { dmg *= 1.6f; LastCrit = true; if (!QuietHit) CombatFx.ShockRing(m.HitPoint, 1.6f, CombatFx.SwordGold, 0.25f); }
            m.TakeDamage(dmg);
            if (m.IsDead) return;
            Vector3 d = m.transform.position - origin; d.y = 0f;
            if (d.sqrMagnitude < 0.01f) d = FlatFwd(pc);
            d.Normalize();
            if (s.HasFx("slow")) m.Slow(s.Fx("slow"), s.Fx2("slow", 2f));
            if (s.HasFx("stagger")) m.Stagger(s.Fx("stagger"));
            if (s.HasFx("knock")) m.Knock(d, s.Fx("knock"));
            if (pullToOrigin && s.HasFx("pull")) m.Knock(-d, Mathf.Min(s.Fx("pull") * 0.25f, Mathf.Max(0f, d.magnitude + 3f)));
            if (s.HasFx("burn")) Burn(m, dmg * s.Fx("burn"), s.Fx2("burn", 3f));
            if (s.HasFx("dot") && CombatFx.Host.I != null) CombatFx.Host.I.StartCoroutine(DotRoutine(m, dmg * s.Fx("dot"), s.Fx2("dot", 4f), SkillLook.Main(s)));
            if (s.HasFx("lifesteal") && pc != null) pc.Heal(dmg * s.Fx("lifesteal"));
        }

        static IEnumerator DotRoutine(Monster m, float dps, float secs, Color col)
        {
            float t = 0f;
            while (t < secs && m != null && !m.IsDead)
            {
                yield return new WaitForSeconds(0.5f);
                t += 0.5f;
                if (m == null || m.IsDead) break;
                CombatFx.Burst(m.HitPoint, col, 5, 0.4f, 1f, 0.5f, 0.1f);
                m.TakeDamage(dps * 0.5f);
            }
        }

        static void SelfFx(SkillDef s, PlayerCombat pc, GameState gs)
        {
            if (s.HasFx("heal") && s.kind != "heal") { pc.Heal(pc.MaxHp * s.Fx("heal")); CombatFx.DamageNumber(pc.transform.position + Vector3.up * 2.3f, pc.MaxHp * s.Fx("heal"), false, new Color(0.5f, 1f, 0.6f)); }
            if (s.HasFx("mana") && s.kind != "heal") pc.RestoreMana(pc.MaxMana * s.Fx("mana"));
        }

        /// <summary>Gerakan dash dikendalikan SkillAction; di sini hanya mendeteksi musuh yang dilewati selama jendela tabrakan.</summary>
        static IEnumerator QuietDash(SkillDef s, PlayerCombat pc, float dmg)
        {
            var hit = new HashSet<Monster>();
            float window = s.id == "e30_2_5" ? 1.2f : 0.4f;
            for (float t = 0f; t < window; t += 0.03f)
            {
                if (pc == null) yield break;
                Vector3 p = pc.transform.position;
                foreach (var m in InRadius(p, 2.2f))
                    if (hit.Add(m))
                    {
                        QuietHit = true; GHit(s, pc, m, dmg, p); QuietHit = false;
                        if (s.id == "bm_5") SkillAction.CutHitFx(m.HitPoint, FlatFwd(pc), LastCrit); else if (s.id == "e30_2_5") SkillAction.WyrmHitFx(m.HitPoint); else SkillAction.CrashFx(pc, m.HitPoint);
                    }
                yield return new WaitForSeconds(0.03f);
            }
        }

        /// <summary>Bilah menembus: bergerak lurus dengan kecepatan tinggi, memotong tiap musuh yang dilewati sekali; feedback hit/crit dari SkillAction.</summary>
        static IEnumerator QuietLine(SkillDef s, PlayerCombat pc, float dmg)
        {
            Vector3 fwd = FlatFwd(pc), o = pc.transform.position;
            float range = s.range > 0f ? s.range : 20f, width = Mathf.Max(0.8f, s.radius);
            var done = new HashSet<Monster>();
            int steps = 10;
            for (int i = 1; i <= steps; i++)
            {
                float reach = range * i / steps, prev = range * (i - 1) / steps;
                foreach (var m in new List<Monster>(Monster.All))
                {
                    if (m == null || m.IsDead || done.Contains(m)) continue;
                    Vector3 d = m.transform.position - o; d.y = 0f;
                    float along = Vector3.Dot(d, fwd);
                    if (along < -0.5f || along > reach + 1f || along < prev - 1f) continue;
                    float side = Mathf.Abs(Vector3.Dot(d, Vector3.Cross(Vector3.up, fwd)));
                    if (side > width * 0.5f + m.Radius + 0.4f) continue;
                    done.Add(m);
                    QuietHit = true;
                    GHit(s, pc, m, dmg, o);
                    QuietHit = false;
                    if (s.id == "e30_2_4") SkillAction.FangHitFx(m.HitPoint, fwd); else if (s.id == "e30_2_6") SkillAction.KingHitFx(m.HitPoint, fwd); else SkillAction.CutHitFx(m.HitPoint, fwd, LastCrit);
                }
                yield return new WaitForSeconds(0.03f);
            }
        }

        /// <summary>Titik bidik skill ground; SkillAction memanggil ini di awal supaya visual & damage memakai titik yang sama.</summary>
        public static Vector3 AimGroundFor(PlayerCombat pc, float range) { return AimGround(pc, range); }

        /// <summary>Ledakan/tick di titik bidik tanpa visual generik (visual dikerjakan SkillAction).</summary>
        static IEnumerator QuietGround(SkillDef s, PlayerCombat pc, float dmg)
        {
            Vector3 center = SkillAction.GroundCenterSet ? SkillAction.GroundCenter : AimGround(pc, s.range > 0f ? s.range : 26f);
            int count = Mathf.Max(1, Mathf.RoundToInt(s.Fx("count", 1)));
            float tick = s.Fx("tick", 0.4f);
            bool chain = s.id == "e30_1_6";
            for (int i = 0; i < count; i++)
            {
                if (chain)
                {
                    foreach (var m in SkillAction.ChainTargets(i, center)) GHit(s, pc, m, dmg, center, false);
                    if (i < count - 1) yield return new WaitForSeconds(tick);
                    continue;
                }
                Vector2 r = count > 1 ? Random.insideUnitCircle * s.radius * 0.7f : Vector2.zero;
                Vector3 p = Ground(center + new Vector3(r.x, 0f, r.y));
                foreach (var m in InRadius(count > 1 ? p : center, count > 1 ? Mathf.Max(2.2f, s.radius * 0.5f) : s.radius)) GHit(s, pc, m, dmg, center, true);
                if (s.HasFx("heal") && pc != null && Vector3.Distance(pc.transform.position, center) < s.radius + 1f) pc.Heal(pc.MaxHp * s.Fx("heal"));
                if (i < count - 1) yield return new WaitForSeconds(tick);
            }
            SkillAction.GroundCenterSet = false;
        }

        static IEnumerator QuietNova(SkillDef s, PlayerCombat pc, float dmg)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(s.Fx("count", 1)));
            float tick = s.Fx("tick", 0.3f);
            for (int i = 0; i < count; i++)
            {
                if (pc == null || pc.IsDead) yield break;
                Vector3 c = Feet(pc);
                foreach (var m in InRadius(c, s.radius))
                {
                    GHit(s, pc, m, dmg, c, true);
                    if (s.id == "bm_6") SkillAction.ThousandHitFx(m.HitPoint, i);
                }
                if (i < count - 1) yield return new WaitForSeconds(tick);
            }
            if (pc != null) SelfFx(s, pc, GameState.I);
        }

        static IEnumerator Wait(float t) { yield return new WaitForSeconds(t); }

        static void RunGeneric(SkillDef s, PlayerCombat pc, float dmg)
        {
            var gs = GameState.I;
            var host = CombatFx.Host.I;
            if (SkillDB.HasSig(s))
            {
                // skill evolusi: pola visual khas (SigFx). Jenis yang punya versi "quiet" hanya memberi damage/status di sini.
                SigFx.Cast(s, pc);
                if (s.kind == "cone" || s.kind == "nova" || s.kind == "line" || s.kind == "ground" || s.kind == "buff") { RunQuiet(s, pc, dmg); return; }
            }
            Color col = SkillLook.Main(s);
            bool caster = JobInfo.Elem(gs.job) != "";
            if (!SkillAction.IsPremium(s.id))
            {
                MagicFx.CastCircle(Feet(pc), col, s.tier >= 3 ? 3.4f : 2.2f, s.tier >= 3 ? 1.2f : 0.8f);
                if (caster) MagicFx.CastCharge(col, 0.28f, s.tier >= 3 ? 0.7f : 0.45f);
            }
            float inv = s.Fx("invuln");
            if (inv > 0f) pc.Invuln(inv);
            ApplyBuffs(s, gs, pc);

            switch (s.kind)
            {
                case "bolt":
                case "multi": host.StartCoroutine(GBolt(s, pc, dmg)); break;
                case "cone": host.StartCoroutine(GCone(s, pc, dmg)); break;
                case "nova": host.StartCoroutine(GNova(s, pc, dmg)); break;
                case "ground": host.StartCoroutine(GGround(s, pc, dmg)); break;
                case "line": host.StartCoroutine(GLine(s, pc, dmg)); break;
                case "dash": host.StartCoroutine(GDash(s, pc, dmg)); break;
                case "heal": host.StartCoroutine(GHeal(s, pc)); break;
                case "summon": GSummon(s, pc, dmg); break;
                case "trap": host.StartCoroutine(GTrap(s, pc, dmg)); break;
                case "buff":
                    MagicFx.Orbit(pc.transform, 4, 1.7f, 1.2f, s.elem == "fire", s.Fx("dur", 6f), 0.9f);
                    MagicFx.Column(Feet(pc), 1.3f, 5f, col, 0.7f);
                    SelfFx(s, pc, gs);
                    break;
            }
            if (s.kind != "heal" && s.kind != "buff") SelfFx(s, pc, gs);
        }

        /// <summary>
        /// Versi tanpa efek visual dari RunGeneric: hanya damage, status (stagger/knock/burn/...), buff.
        /// Dipakai skill yang sudah punya koreografi sendiri (SkillAction) supaya efek tidak dobel.
        /// </summary>
        static void RunQuiet(SkillDef s, PlayerCombat pc, float dmg)
        {
            var gs = GameState.I;
            float inv = s.Fx("invuln");
            if (inv > 0f) pc.Invuln(inv);
            ApplyBuffs(s, gs, pc);
            Vector3 fwd = FlatFwd(pc), o = pc.transform.position;
            switch (s.kind)
            {
                case "cone":
                    {
                        float range = s.range > 0f ? s.range : 5f, half = s.radius > 0f ? s.radius : 60f;
                        foreach (var m in InCone(o, fwd, range, half)) GHit(s, pc, m, dmg, o);
                    }
                    break;
                case "nova":
                    CombatFx.Host.I.StartCoroutine(QuietNova(s, pc, dmg));
                    break;
                case "dash":
                    CombatFx.Host.I.StartCoroutine(QuietDash(s, pc, dmg));
                    break;
                case "line":
                    CombatFx.Host.I.StartCoroutine(QuietLine(s, pc, dmg));
                    break;
                case "ground":
                    CombatFx.Host.I.StartCoroutine(QuietGround(s, pc, dmg));
                    break;
                case "buff":
                    SelfFx(s, pc, gs);
                    break;
                default:
                    RunGeneric(s, pc, dmg);      // belum ada versi quiet untuk jenis ini
                    break;
            }
        }

        // ---------------------------------------------------------------- bolt / multi
        static Monster RayMonster(Vector3 origin, Vector3 dir, float range, out Vector3 end)
        {
            end = origin + dir * range;
            var hits = Physics.SphereCastAll(origin, 0.5f, dir, range, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                if (h.collider.GetComponentInParent<PlayerCombat>() != null) continue;
                var m = h.collider.GetComponentInParent<Monster>();
                end = h.point;
                return m != null && !m.IsDead ? m : null;
            }
            return null;
        }

        static IEnumerator GBolt(SkillDef s, PlayerCombat pc, float dmg)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(s.Fx("count", 1)));
            float spread = s.Fx("spread", 0f);
            float range = s.range > 0f ? s.range : 40f;
            Vector3 origin = JobInfo.Elem(GameState.I.job) != "" ? MagicFx.Tip() : Muzzle(pc);
            var cam = pc.Cam;
            Vector3 aimDir = cam != null ? cam.forward : pc.transform.forward;
            // titik bidik: pakai sinar kamera, lalu arahkan dari ujung tongkat/busur
            Vector3 aimEnd;
            AimHits(pc, range, false, out aimEnd);
            Vector3 baseDir = (aimEnd - origin).normalized;
            if (baseDir.sqrMagnitude < 0.01f) baseDir = aimDir;
            int hitsLeftChain = Mathf.RoundToInt(s.Fx("chain", 0));
            for (int i = 0; i < count; i++)
            {
                float ang = count > 1 ? Mathf.Lerp(-spread * 0.5f, spread * 0.5f, i / (float)(count - 1)) : 0f;
                Vector3 dir = Quaternion.AngleAxis(ang, Vector3.up) * baseDir;
                Vector3 end;
                Monster target = RayMonster(origin, dir, range, out end);
                Vector3 e2 = end; Monster tgt = target;
                bool arrow = s.elem == "arrow" || s.elem == "phys";
                System.Action hit = () =>
                {
                    if (arrow) CombatFx.HitSpark(e2, SkillLook.Main(s), 0.8f, 8);
                    else MagicFx.ElemImpact(e2, s.elem, Mathf.Max(1.2f, s.radius * 0.7f));
                    if (s.radius > 0.5f)
                        foreach (var m in InRadius(e2, s.radius)) GHit(s, pc, m, m == tgt ? dmg : dmg * 0.6f, e2);
                    else if (tgt != null) GHit(s, pc, tgt, dmg, pc.transform.position);
                    if (tgt != null && hitsLeftChain > 0) CombatFx.Host.I.StartCoroutine(Chain(s, pc, tgt, dmg * 0.7f, hitsLeftChain));
                };
                if (arrow) CombatFx.Arrow(origin, end, SkillLook.Main(s), count > 6 ? 0.7f : 1.1f, 110f, hit);
                else MagicFx.BoltE(origin, end, s.elem, count > 4 ? 0.28f : 0.42f, 55f, hit);
                if (count > 1 && count <= 12) yield return new WaitForSeconds(0.04f);
            }
        }

        static IEnumerator Chain(SkillDef s, PlayerCombat pc, Monster from, float dmg, int n)
        {
            var done = new HashSet<Monster> { from };
            Monster cur = from;
            for (int i = 0; i < n; i++)
            {
                yield return new WaitForSeconds(0.1f);
                Monster best = null; float bd = 9f;
                foreach (var m in new List<Monster>(Monster.All))
                {
                    if (m == null || m.IsDead || done.Contains(m) || cur == null) continue;
                    float d = Vector3.Distance(m.transform.position, cur.transform.position);
                    if (d < bd) { bd = d; best = m; }
                }
                if (best == null || cur == null) yield break;
                CombatFx.Beam(cur.HitPoint, best.HitPoint, Color.white, 0.25f, 0.15f);
                MagicFx.ElemImpact(best.HitPoint, s.elem, 1.5f);
                GHit(s, pc, best, dmg, cur.transform.position);
                done.Add(best); cur = best;
            }
        }

        // ---------------------------------------------------------------- cone
        static IEnumerator GCone(SkillDef s, PlayerCombat pc, float dmg)
        {
            Vector3 fwd = FlatFwd(pc);
            float range = s.range > 0f ? s.range : 5f, half = s.radius > 0f ? s.radius : 60f;
            Color col = SkillLook.Main(s);
            Vector3 o = pc.transform.position;
            Vector3 c0 = Chest(pc);
            Audio.Sfx("swing", o);
            CombatFx.SlashArc(c0, pc.transform.rotation, Mathf.Max(2.6f, range * 0.85f), 20f, col, 0.2f, 1.1f);
            Vector3 mid = Ground(o + fwd * range * 0.65f);
            if (s.elem == "phys" || s.elem == "arrow") { CombatFx.ShockRing(mid, 2.8f, col, 0.3f); CombatFx.Dust(mid, 8); }
            else MagicFx.ElemImpact(mid, s.elem, Mathf.Clamp(range * 0.35f, 2f, 4.5f));
            yield return new WaitForSeconds(0.08f);
            foreach (var m in InCone(o, fwd, range, half)) GHit(s, pc, m, dmg, o);
        }

        // ---------------------------------------------------------------- nova
        static IEnumerator GNova(SkillDef s, PlayerCombat pc, float dmg)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(s.Fx("count", 1)));
            float tick = s.Fx("tick", 0.3f);
            Color col = SkillLook.Main(s);
            for (int i = 0; i < count; i++)
            {
                if (pc == null || pc.IsDead) yield break;
                Vector3 c = Feet(pc);
                Audio.Sfx(i % 2 == 0 ? "swing" : "swing2", pc.transform.position);
                if (s.elem == "phys" || s.elem == "arrow")
                {
                    CombatFx.ShockRing(c, s.radius, col, 0.35f);
                    CombatFx.SlashArc(Chest(pc) + Vector3.up * Random.Range(-0.2f, 0.4f), pc.transform.rotation * Quaternion.Euler(0f, Random.Range(-180f, 180f), 0f), Mathf.Max(2.6f, s.radius * 0.7f), Random.Range(-60f, 60f), col, 0.15f, 1.1f);
                    if (i == 0 || count == 1) CombatFx.Dust(c, 10);
                }
                else if (i == 0 || count == 1) MagicFx.ElemImpact(c, s.elem, s.radius * 0.8f);
                else CombatFx.ShockRing(c, s.radius, col, 0.3f);
                foreach (var m in InRadius(c, s.radius)) GHit(s, pc, m, dmg, c, true);
                if (i < count - 1) yield return new WaitForSeconds(tick);
            }
        }

        // ---------------------------------------------------------------- ground
        static IEnumerator GGround(SkillDef s, PlayerCombat pc, float dmg)
        {
            Vector3 center = AimGround(pc, s.range > 0f ? s.range : 26f);
            int count = Mathf.Max(1, Mathf.RoundToInt(s.Fx("count", 1)));
            float tick = s.Fx("tick", 0.4f);
            Color col = SkillLook.Main(s);
            Audio.Sfx("skill_cast", center);
            MagicFx.MagicCircle(center, s.radius, new Color(col.r, col.g, col.b, 0.95f), 0.5f + count * tick);
            if (s.HasFx("pull") || s.elem == "wind") MagicFx.Tornado(center, Mathf.Max(1.5f, s.radius * 0.45f), 8f, col, 0.6f + count * tick);
            yield return new WaitForSeconds(0.4f);
            for (int i = 0; i < count; i++)
            {
                Vector2 r = count > 1 ? Random.insideUnitCircle * s.radius * 0.7f : Vector2.zero;
                Vector3 p = Ground(center + new Vector3(r.x, 0f, r.y));
                if (s.elem == "arrow")
                {
                    Vector3 from = p + new Vector3(Random.Range(-2f, 2f), 14f, Random.Range(-2f, 2f));
                    Vector3 pp = p;
                    CombatFx.Arrow(from, pp, col, 0.8f, 70f, () => CombatFx.HitSpark(pp, col, 0.6f, 6));
                }
                else if (s.elem != "wind") MagicFx.ElemImpact(p, s.elem, Mathf.Clamp(s.radius * (count > 1 ? 0.35f : 0.8f), 1.5f, 6f));
                foreach (var m in InRadius(count > 1 ? p : center, count > 1 ? Mathf.Max(2.2f, s.radius * 0.5f) : s.radius)) GHit(s, pc, m, dmg, center, true);
                if (s.HasFx("pull")) foreach (var m in InRadius(center, s.radius)) { Vector3 d = center - m.transform.position; d.y = 0f; if (d.magnitude > 1.2f) m.Knock(d.normalized, Mathf.Min(s.Fx("pull") * 0.2f, d.magnitude - 0.8f)); }
                if (s.HasFx("heal") && pc != null && Vector3.Distance(pc.transform.position, center) < s.radius + 1f) pc.Heal(pc.MaxHp * s.Fx("heal"));
                if (s.HasFx("mana") && count > 1 && pc != null) pc.RestoreMana(pc.MaxMana * s.Fx("mana"));
                if (i < count - 1) yield return new WaitForSeconds(tick);
            }
        }

        // ---------------------------------------------------------------- line
        static IEnumerator GLine(SkillDef s, PlayerCombat pc, float dmg)
        {
            Vector3 fwd = FlatFwd(pc);
            float range = s.range > 0f ? s.range : 20f, width = Mathf.Max(0.6f, s.radius);
            Color col = SkillLook.Main(s);
            Vector3 o = pc.transform.position;
            Vector3 a = (JobInfo.Elem(GameState.I.job) != "") ? MagicFx.Tip() : Chest(pc);
            Audio.Sfx("heavy_swing", o);
            CombatFx.Beam(a, a + fwd * range, col, Mathf.Min(1.4f, width * 0.5f), 0.3f);
            int steps = Mathf.Clamp(Mathf.RoundToInt(range / 4f), 2, 9);
            for (int i = 1; i <= steps; i++)
            {
                Vector3 p = Ground(o + fwd * (range * i / steps));
                if (s.elem == "phys" || s.elem == "arrow") { CombatFx.ShockRing(p, Mathf.Max(1.8f, width), col, 0.25f); CombatFx.Dust(p, 3); }
                else MagicFx.ElemImpact(p, s.elem, Mathf.Clamp(width * 0.9f, 1.4f, 4.5f));
                var hitHere = new List<Monster>();
                foreach (var m in new List<Monster>(Monster.All))
                {
                    if (m == null || m.IsDead) continue;
                    Vector3 d = m.transform.position - o; d.y = 0f;
                    float along = Vector3.Dot(d, fwd);
                    if (along < -0.5f || along > range * i / steps + 1f || along < range * (i - 1) / steps - 1f) continue;
                    float side = Mathf.Abs(Vector3.Dot(d, Vector3.Cross(Vector3.up, fwd)));
                    if (side <= width * 0.5f + m.Radius) hitHere.Add(m);
                }
                foreach (var m in hitHere) { GHit(s, pc, m, dmg, o); }
                yield return new WaitForSeconds(0.05f);
            }
        }

        // ---------------------------------------------------------------- dash
        static IEnumerator GDash(SkillDef s, PlayerCombat pc, float dmg)
        {
            Vector3 fwd = FlatFwd(pc);
            float dist = s.range > 0f ? s.range : 8f;
            float speed = Mathf.Max(18f, dist / 0.28f);
            float dur = dist / speed;
            Color col = SkillLook.Main(s);
            Audio.Sfx("dodge", pc.transform.position);
            pc.DashTo(fwd, dur, speed);
            var hit = new HashSet<Monster>();
            float t = 0f;
            while (t < dur + 0.05f)
            {
                if (pc == null) yield break;
                Vector3 p = pc.transform.position;
                CombatFx.Burst(p - Vector3.up * 0.6f, col, 4, 0.5f, 0.6f, 0.4f, 0.14f);
                foreach (var m in InRadius(p, 2.0f)) if (hit.Add(m)) { GHit(s, pc, m, dmg, p); MagicFx.Sparks(m.HitPoint, col, 0.8f); }
                t += 0.04f;
                yield return new WaitForSeconds(0.04f);
            }
            if (pc != null) { MagicFx.ElemImpact(Feet(pc), s.elem == "phys" ? "wind" : s.elem, 2.2f); }
        }

        // ---------------------------------------------------------------- heal
        static IEnumerator GHeal(SkillDef s, PlayerCombat pc)
        {
            var gs = GameState.I;
            int count = Mathf.Max(1, Mathf.RoundToInt(s.Fx("count", 1)));
            float tick = s.Fx("tick", 1f);
            Color col = SkillLook.Main(s);
            MagicFx.MagicCircle(Feet(pc), Mathf.Max(2.5f, s.radius), new Color(col.r, col.g, col.b, 0.95f), 0.8f + count * tick * 0.6f);
            MagicFx.Column(Feet(pc), 1.4f, 6f, col, 0.8f);
            for (int i = 0; i < count; i++)
            {
                if (pc == null || pc.IsDead) yield break;
                float frac = s.Fx("heal", 0.2f);
                float amt = pc.MaxHp * frac;
                pc.Heal(amt);
                CombatFx.DamageNumber(pc.transform.position + Vector3.up * 2.4f, amt, false, new Color(0.5f, 1f, 0.6f));
                foreach (var a in new List<AllyUnit>(AllyUnit.All))
                    if (a != null && Vector3.Distance(a.transform.position, pc.transform.position) <= s.radius + 2f) a.Heal(a.maxHp * frac);
                if (i == 0 && s.HasFx("mana")) pc.RestoreMana(pc.MaxMana * s.Fx("mana"));
                CombatFx.Burst(pc.transform.position + Vector3.up * 1.0f, new Color(0.6f, 1f, 0.7f), 14, 1.1f, 2.4f, 0.9f, 0.14f);
                if (i < count - 1) yield return new WaitForSeconds(tick);
            }
        }

        // ---------------------------------------------------------------- trap
        static IEnumerator GTrap(SkillDef s, PlayerCombat pc, float dmg)
        {
            Vector3 p = AimGround(pc, s.range > 0f ? s.range : 14f);
            Color col = SkillLook.Main(s);
            float life = s.Fx("dur", 15f), r = Mathf.Max(1.8f, s.radius);
            MagicFx.MagicCircle(p, r, new Color(col.r, col.g, col.b, 0.8f), life);
            yield return new WaitForSeconds(0.6f);
            float t = 0f;
            while (t < life)
            {
                foreach (var m in InRadius(p, r))
                {
                    MagicFx.Sparks(p + Vector3.up * 0.3f, col, 1.4f);
                    CombatFx.ShockRing(p, r * 1.4f, col, 0.35f);
                    for (int i = 0; i < 6; i++) { float a = i * 60f * Mathf.Deg2Rad; MagicFx.Spike(p + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r * 0.6f, 1.2f, 0.28f, new Color(0.55f, 0.45f, 0.3f), 0f, 0.4f); }
                    foreach (var mm in InRadius(p, r + 0.6f)) GHit(s, pc, mm, dmg, p);
                    Audio.Sfx("block", p);
                    yield break;
                }
                t += 0.2f;
                yield return new WaitForSeconds(0.2f);
            }
        }

        // ---------------------------------------------------------------- summon
        static SummonPet currentPet;

        static void GSummon(SkillDef s, PlayerCombat pc, float dmg)
        {
            if (currentPet != null) Object.Destroy(currentPet.gameObject);
            string kind = s.FxS("pet");
            var go = new GameObject("FxPet_" + kind);
            go.transform.position = Ground(pc.transform.position + pc.transform.forward * 2.2f);
            var pet = go.AddComponent<SummonPet>();
            pet.Init(kind, SkillLook.Main(s), s.Fx("dur", 20f), dmg, pc, s);
            currentPet = pet;
            MagicFx.ElemImpact(go.transform.position, s.elem, 3f);
        }
    }

    /// <summary>Pendamping sementara (golem, serigala roh, kolosus): mengikuti pemain dan menyerang monster terdekat.</summary>
    public class SummonPet : MonoBehaviour
    {
        PlayerCombat owner; SkillDef skill; Color col; float life, dmg, nextAtk; string kind; float scale = 1f, speed = 6.5f;
        Transform body;

        static GameObject Cube(Transform p, Vector3 pos, Vector3 size, Color c)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(p, false);
            g.transform.localPosition = pos; g.transform.localScale = size;
            g.GetComponent<MeshRenderer>().sharedMaterial = CombatFx.Mat(null, c);
            return g;
        }

        public void Init(string k, Color c, float dur, float d, PlayerCombat pc, SkillDef s)
        {
            kind = k; col = c; life = dur; dmg = d; owner = pc; skill = s;
            body = new GameObject("Body").transform; body.SetParent(transform, false);
            Color stone = new Color(0.5f, 0.45f, 0.38f), dark = new Color(0.34f, 0.3f, 0.26f);
            if (k == "wolf")
            {
                Color fur = new Color(0.75f, 0.9f, 0.7f, 1f);
                Cube(body, new Vector3(0, 0.7f, 0), new Vector3(0.5f, 0.5f, 1.2f), fur);
                Cube(body, new Vector3(0, 0.95f, 0.75f), new Vector3(0.4f, 0.4f, 0.45f), fur);
                Cube(body, new Vector3(0, 0.9f, 1.05f), new Vector3(0.22f, 0.2f, 0.3f), Color.white);
                foreach (float lx in new[] { -0.18f, 0.18f }) foreach (float lz in new[] { -0.4f, 0.4f }) Cube(body, new Vector3(lx, 0.25f, lz), new Vector3(0.14f, 0.5f, 0.14f), fur);
                Cube(body, new Vector3(0, 0.85f, -0.7f), new Vector3(0.14f, 0.14f, 0.6f), fur).transform.localRotation = Quaternion.Euler(-30f, 0, 0);
                speed = 9f; scale = 1.1f;
            }
            else
            {
                bool big = k == "colossus";
                Cube(body, new Vector3(0, 1.3f, 0), new Vector3(1.3f, 1.5f, 0.9f), stone);
                Cube(body, new Vector3(0, 2.3f, 0), new Vector3(0.7f, 0.6f, 0.65f), dark);
                Cube(body, new Vector3(-0.95f, 1.4f, 0), new Vector3(0.5f, 1.4f, 0.55f), dark);
                Cube(body, new Vector3(0.95f, 1.4f, 0), new Vector3(0.5f, 1.4f, 0.55f), dark);
                Cube(body, new Vector3(-0.4f, 0.4f, 0), new Vector3(0.55f, 0.8f, 0.6f), stone);
                Cube(body, new Vector3(0.4f, 0.4f, 0), new Vector3(0.55f, 0.8f, 0.6f), stone);
                Cube(body, new Vector3(-0.18f, 2.35f, 0.35f), new Vector3(0.14f, 0.1f, 0.05f), new Color(0.4f, 0.9f, 1f));
                Cube(body, new Vector3(0.18f, 2.35f, 0.35f), new Vector3(0.14f, 0.1f, 0.05f), new Color(0.4f, 0.9f, 1f));
                scale = big ? 2.1f : 1f; speed = big ? 5f : 6f;
                if (big) Cube(body, new Vector3(0, 3.0f, 0), new Vector3(0.9f, 0.2f, 0.9f), new Color(1f, 0.8f, 0.3f));
            }
            body.localScale = Vector3.one * scale;
            var l = new GameObject("L").AddComponent<Light>(); l.transform.SetParent(transform, false);
            l.transform.localPosition = Vector3.up * 2f * scale; l.type = LightType.Point; l.color = col; l.range = 6f * scale; l.intensity = 1.1f;
        }

        void Update()
        {
            life -= Time.deltaTime;
            if (life <= 0f || owner == null) { Poof(); return; }
            Monster tgt = null; float bd = 16f;
            foreach (var m in Monster.All)
            {
                if (m == null || m.IsDead) continue;
                float d = Vector3.Distance(m.transform.position, transform.position);
                if (d < bd && Vector3.Distance(m.transform.position, owner.transform.position) < 24f) { bd = d; tgt = m; }
            }
            Vector3 goal; float stop;
            if (tgt != null) { goal = tgt.transform.position; stop = 2.4f * scale; }
            else { goal = owner.transform.position + owner.transform.right * 2.5f - owner.transform.forward * 1.5f; stop = 1.2f; }
            Vector3 dir = goal - transform.position; dir.y = 0f;
            if (dir.magnitude > stop)
            {
                transform.position += dir.normalized * speed * Time.deltaTime;
                float gy = RT.GroundY(transform.position);
                transform.position = new Vector3(transform.position.x, gy, transform.position.z);
            }
            if (dir.sqrMagnitude > 0.05f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir.normalized), 8f * Time.deltaTime);
            body.localPosition = Vector3.up * (Mathf.Abs(Mathf.Sin(Time.time * 8f)) * 0.12f * scale);
            if (tgt != null && dir.magnitude <= stop + 0.6f && Time.time >= nextAtk)
            {
                nextAtk = Time.time + 1.1f;
                float dd = dmg * (kind == "colossus" ? 1.2f : 1f);
                tgt.TakeDamage(dd);
                if (kind == "colossus" || kind == "golem") { tgt.Stagger(0.5f); MagicFx.RockBurst(tgt.transform.position, kind == "colossus" ? 3.5f : 2.2f, 6); }
                else CombatFx.HitSpark(tgt.HitPoint, col, 1f, 10);
                Audio.Sfx("hit_heavy", transform.position);
            }
        }

        void Poof()
        {
            MagicFx.ElemImpact(transform.position + Vector3.up, skill != null ? skill.elem : "earth", 2.5f);
            if (this != null) Destroy(gameObject);
        }
    }
}
