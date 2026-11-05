using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Eksekusi skill aktif (tombol Z X C V) untuk 2 job: damage, efek status, dan efek visual per skill.
    /// Skill pasif (Tier 1) belum berefek di combat. Biaya stamina: Tier 2 = 20, Tier 3 = 40. Cooldown mengikuti SkillDB.
    /// </summary>
    public static partial class SkillExec
    {
        static readonly Dictionary<string, float> ready = new Dictionary<string, float>();

        public static float CooldownLeft(string id)
        {
            float t;
            return ready.TryGetValue(id, out t) ? Mathf.Max(0f, t - Time.time) : 0f;
        }

        public static void ResetCooldowns() { ready.Clear(); }

        public static void Use(int slot)
        {
            var gs = GameState.I;
            if (gs == null) return;
            var s = SkillDB.Get(gs.skillSlots[slot]);
            if (s == null) { GameState.Say("Slot skill kosong."); return; }
            var pc = PlayerCombat.I;
            if (pc == null) { GameState.Say("Skill hanya bisa dipakai di area berburu atau latihan."); return; }
            if (pc.IsDead) return;
            float left = DevTools.NoCooldown ? 0f : CooldownLeft(s.id);
            if (left > 0f) { GameState.Say(s.name + " belum siap (" + left.ToString("0.0") + " dtk)."); return; }
            if (gs.IsMage)
            {
                float mc = (s.tier >= 3 ? 50f : 25f) * gs.ManaCostMult;
                if (pc.mana < mc) { GameState.Say("Mana kurang untuk " + s.name + " (" + Mathf.CeilToInt(mc) + ")."); return; }
                pc.SpendMana(mc);
            }
            else
            {
                float cost = s.tier >= 3 ? 40f : 20f;
                if (pc.stamina < cost) { GameState.Say("Stamina kurang untuk " + s.name + "."); return; }
                pc.SpendStamina(cost);
            }
            ready[s.id] = DevTools.NoCooldown ? 0f : Time.time + s.cooldown * (1f - Mathf.Min(0.5f, gs.Pas("cdr") + gs.AttachEffect("cdr") / 100f));
            pc.FaceAim();
            Combat2.OnSkillCast(s);
            bool choreo = SkillAction.Has(s.id);
            bool prem = SkillAction.IsPremium(s.id);
            if (!choreo) { pc.LockAttack(0.5f); if (PlayerAvatar.I != null) PlayerAvatar.I.Cast(); }
            Audio.Sfx("skill_cast", pc.transform.position);
            if (TownUI.I != null) TownUI.I.Toast(s.name + "!");
            if (s.tier >= 3)
            {
                string ue = !string.IsNullOrEmpty(s.elem) ? s.elem : JobInfo.Elem(s.job);
                Color uc = (ue == "" || ue == "phys" || ue == "arrow") ? JobInfo.JobColor(s.job) : MagicFx.ElemCol(ue);
                UltimateFx.Play(uc, s.name);
                AnimeLook.Ultimate(uc, s.name);
                MagicFx.UltimateCast(pc.transform.position - Vector3.up * 0.9f, pc.transform.forward, uc);
                bool selfC = s.kind == "buff" || s.kind == "nova" || s.id == "fm_wrd_6" || s.id == "em_stn_6" || s.id == "sw_def_6";
                if (!choreo) MagicFx.UltimateGrand(pc.transform.position - Vector3.up * 0.9f, pc.transform.forward, ue, uc, selfC);
            }
            if (s.tier >= 3 && !string.IsNullOrEmpty(s.beast) && !SkillSilhouette.Handles(s.id))
                SkillSilhouette.BeastStrike(s, pc, SkillAction.HitDelay(s.id));
            GroundMark.Skill(s, pc, SkillAction.HitDelay(s.id));
            float baseDmg = gs.job == "Archer" ? 8f + gs.TotalAtk * 0.9f : (gs.IsMage ? MageBase(gs) : 10f + gs.TotalAtk);
            if (!gs.IsMage) baseDmg *= gs.DmgMult;
            float fullDmg = baseDmg * s.mult;
            if (prem) SkillAction.Play(s, pc, () =>
            {
                if (pc == null || pc.IsDead) return;
                // lepas muatan: efek mewah + (ultimate) intro besar, lalu damage/efek skill
                SkillSpectacle.Play(s, pc);
                if (s.tier >= 3)
                {
                    string ue2 = !string.IsNullOrEmpty(s.elem) ? s.elem : JobInfo.Elem(s.job);
                    Color uc2 = (ue2 == "" || ue2 == "phys" || ue2 == "arrow") ? JobInfo.JobColor(s.job) : MagicFx.ElemCol(ue2);
                    bool selfC2 = s.kind == "buff" || s.kind == "nova";
                    MagicFx.UltimateGrand(pc.transform.position - Vector3.up * 0.9f, pc.transform.forward, ue2, uc2, selfC2);
                }
                if (!string.IsNullOrEmpty(s.kind)) RunGeneric(s, pc, fullDmg); else Run(s, pc, fullDmg);
            });
            else if (choreo) SkillAction.Play(s, pc, () => { if (!string.IsNullOrEmpty(s.kind)) RunQuiet(s, pc, fullDmg); else Run(s, pc, fullDmg); });
            else if (!string.IsNullOrEmpty(s.kind)) RunGeneric(s, pc, fullDmg);
            else Run(s, pc, fullDmg);
        }

        /// <summary>Damage dasar sihir: tongkat + 10% bila passive elemen dipelajari.</summary>
        public static float MageBase(GameState gs)
        {
            float b = (9f + gs.TotalAtk * 0.95f) * gs.SpellPower * gs.DmgMult;
            if (gs.job == "FireMage" && gs.Knows("fm_inf_1")) b *= 1.1f;
            if (gs.job == "EarthMage" && gs.Knows("em_stn_1")) b *= 1.1f;
            return b;
        }

        // =====================================================================
        // Pembantu
        // =====================================================================
        static Vector3 Chest(PlayerCombat pc) { return pc.transform.position + Vector3.up * 1.0f; }
        static Vector3 Feet(PlayerCombat pc) { return pc.transform.position - Vector3.up * 0.9f; }
        static Vector3 Muzzle(PlayerCombat pc) { return pc.transform.position + Vector3.up * 1.4f + pc.transform.forward * 0.7f; }

        static List<Monster> InCone(Vector3 o, Vector3 fwd, float range, float halfAngle)
        {
            var l = new List<Monster>();
            foreach (var m in new List<Monster>(Monster.All))
            {
                if (m == null || m.IsDead) continue;
                Vector3 d = m.HitPoint - o; d.y = 0f;
                float dist = d.magnitude;
                if (dist > range + m.Radius) continue;
                if (dist > m.Radius + 0.5f && Vector3.Angle(fwd, d) > halfAngle) continue;
                l.Add(m);
            }
            return l;
        }

        static List<Monster> InRadius(Vector3 c, float r)
        {
            var l = new List<Monster>();
            foreach (var m in new List<Monster>(Monster.All))
            {
                if (m == null || m.IsDead) continue;
                Vector3 d = m.transform.position - c; d.y = 0f;
                if (d.magnitude <= r + m.Radius) l.Add(m);
            }
            return l;
        }

        /// <summary>Sinar bidik dari kamera: kumpulan monster yang kena (urut dari dekat) dan titik akhir.</summary>
        static List<Monster> AimHits(PlayerCombat pc, float maxDist, bool pierce, out Vector3 end)
        {
            var res = new List<Monster>();
            var cam = pc.Cam;
            end = pc.transform.position + pc.transform.forward * maxDist;
            if (cam == null) return res;
            end = cam.position + cam.forward * maxDist;
            var hits = Physics.RaycastAll(cam.position, cam.forward, maxDist, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                if (h.collider.GetComponentInParent<PlayerCombat>() != null) continue;
                var m = h.collider.GetComponentInParent<Monster>();
                if (m != null && !m.IsDead)
                {
                    res.Add(m);
                    end = h.point;
                    if (!pierce) break;
                    continue;
                }
                end = h.point;
                break;
            }
            return res;
        }

        /// <summary>Damage Piercing Shot masuk ±0.17 s setelah release: saat panah berhasil menembus lapisan pertahanan, bukan saat menyentuh.</summary>
        static IEnumerator PierceDamage(List<Monster> hits, float dmg)
        {
            for (int i = 0; i < hits.Count; i++)
            {
                yield return new WaitForSeconds(i == 0 ? 0.17f : 0.03f);
                if (hits[i] != null && !hits[i].IsDead) hits[i].TakeDamage(dmg);
            }
        }

        static IEnumerator DeadeyeDamage(Monster m, Vector3 pt, bool crit, float dmg)
        {
            yield return new WaitForSeconds(0.14f);
            SkillAction.DeadeyeImpact(pt, crit, m != null);
            if (m != null && !m.IsDead)
            {
                m.TakeDamage(crit ? dmg * 2f : dmg);
                if (crit && TownUI.I != null) TownUI.I.Toast("CRITICAL!");
            }
        }

        /// <summary>Monster pertama di garis bidik (untuk reticle Deadeye).</summary>
        public static Monster AimMonster(PlayerCombat pc)
        {
            Vector3 end; var h = AimHits(pc, 70f, false, out end);
            return h.Count > 0 ? h[0] : null;
        }

        static Vector3 Ground(Vector3 p)
        {
            p.y = RT.GroundY(p);
            return p;
        }

        /// <summary>Titik di tanah yang dibidik pemain (atau monster terdekat di depan bila ada).</summary>
        static Vector3 AimGround(PlayerCombat pc, float maxDist)
        {
            Vector3 end;
            var hits = AimHits(pc, maxDist, false, out end);
            if (hits.Count > 0) return Ground(hits[0].transform.position);
            return Ground(end);
        }

        static void Beam(Vector3 a, Vector3 b, Color c, float w, float life) { CombatFx.Beam(a, b, c, w, life); }

        // =====================================================================
        // Skill
        // =====================================================================
        static void Run(SkillDef s, PlayerCombat pc, float dmg)
        {
            var host = CombatFx.Host.I;
            Vector3 fwd = pc.transform.forward;
            switch (s.id)
            {
                // ------------------------------ Swordsman: Offense
                case "sw_off_4":    // Cleave: tebasan lebar
                    {
                        // efek & gerak tubuh Cleave ada di SkillAction (SkillActions.Swordsman.cs); di sini hanya damage
                        foreach (var m in InCone(pc.transform.position, fwd, 4.6f, 95f)) m.TakeDamage(dmg);
                    }
                    break;
                case "sw_off_5":    // Rage Strike: satu tebasan vertikal sangat berat
                    {
                        // efek & gerak tubuh ada di SkillAction (SkillActions.Swordsman.cs); di sini hanya biaya stamina dan damage
                        pc.SpendStamina(10f);
                        foreach (var m in InCone(pc.transform.position, fwd, 4.4f, 65f)) m.TakeDamage(dmg * 1.4f);
                    }
                    break;
                case "sw_off_6":    // Executioner's Arc (ultimate)
                    // gerak & efek di SkillActions.Swordsman.cs (ExecutionerArc); di sini hanya damage
                    foreach (var m in InCone(pc.transform.position, fwd, 6.4f, 105f)) { m.TakeDamage(dmg * 1.6f); m.Knock(fwd, 1.4f); }
                    break;

                // ------------------------------ Swordsman: Defense
                case "sw_def_4":    // Perfect Parry (gerak & efek di SkillActions.Swordsman.cs)
                    pc.ParryWindow(0.95f);
                    break;
                case "sw_def_5":    // Shield Bash (gerak & efek di SkillActions.Swordsman.cs; di sini damage, stagger, dorong)
                    foreach (var m in InCone(pc.transform.position, fwd, 3.2f, 65f))
                    {
                        m.TakeDamage(dmg);
                        m.Stagger(2.6f);
                        m.Knock(fwd, 1.6f);
                    }
                    break;
                case "sw_def_6":    // Bulwark (gerak & efek di SkillActions.Swordsman.cs; di sini buff 10 dtk -70% damage + taunt)
                    pc.Buff(0.3f, 10f);
                    pc.BulwarkFor(10f);
                    foreach (var m in new List<Monster>(Monster.All)) if (m != null && !m.IsDead && Vector3.Distance(m.transform.position, pc.transform.position) < 30f) m.Alert();
                    if (TownUI.I != null) TownUI.I.Toast("Bulwark: damage masuk -70% selama 10 dtk!");
                    break;

                // ------------------------------ Swordsman: Mobility
                case "sw_mob_4":    // Blink Strike
                    {
                        // dipanggil pada momen Explosive Launch (0.22 s); damage jatuh terlambat di 0.65 s (Delayed Slash Impact)
                        Vector3 a = pc.transform.position;
                        pc.Invuln(0.5f);
                        pc.DashTo(fwd, 0.26f, 30f);
                        host.StartCoroutine(BlinkHit(pc, a, dmg));
                    }
                    break;
                case "sw_mob_5":    // Riposte (gerak & efek di SkillActions.Swordsman.cs)
                    pc.RiposteWindow(3.5f);
                    break;
                case "sw_mob_6":    // Phantom Flurry (ultimate)
                    // gerak & efek di SkillActions.Swordsman.cs (PhantomFlurry); damage multi-hit tertunda di sini (Delayed Multi-Hit Impact)
                    host.StartCoroutine(FlurryHits(SkillAction.FlurryCenter, dmg));
                    break;

                // ------------------------------ Archer: Precision
                case "ar_pre_4":    // Piercing Shot: menembus semua monster segaris (visual & gerak di SkillActions.Archer.cs)
                    {
                        Vector3 end; var hits = AimHits(pc, 70f, true, out end);
                        Audio.Sfx("bow_shot", pc.transform.position);
                        SkillAction.PierceEnd = end; SkillAction.PierceFrom = Muzzle(pc);
                        SkillAction.PierceHitPts.Clear();
                        foreach (var m in hits) SkillAction.PierceHitPts.Add(m.HitPoint);
                        host.StartCoroutine(PierceDamage(hits, dmg));
                    }
                    break;
                case "ar_pre_5":    // Deadeye: titik lemah; crit ditentukan saat rilis, damage masuk saat panah mengenai reticle (visual di SkillActions.Archer.cs)
                    {
                        Vector3 end; var hits = AimHits(pc, 70f, false, out end);
                        Audio.Sfx("bow_shot", pc.transform.position);
                        bool crit = Random.value < 0.7f;
                        Vector3 from = Muzzle(pc);
                        Monster tgt = hits.Count > 0 ? hits[0] : null;
                        Vector3 pt = tgt != null ? SkillAction.DeadeyeWeakPoint(tgt) : end;
                        float dist = (pt - from).magnitude, spd = Mathf.Max(60f, dist / 0.14f);
                        CombatFx.Arrow(from, pt, new Color(1f, 0.95f, 0.78f, 0.6f), 0.45f, spd);
                        host.StartCoroutine(DeadeyeDamage(tgt, pt, crit, dmg));
                    }
                    break;
                case "ar_pre_6":    // Heartseeker (ultimate): satu panah lurus dengan koreksi kecil; visual di SkillActions.Archer.cs
                    {
                        Vector3 end; var hits = AimHits(pc, 90f, false, out end);
                        Audio.Sfx("bow_shot", pc.transform.position);
                        host.StartCoroutine(SkillAction.HeartseekerFlight(pc, Muzzle(pc), end, hits.Count > 0 ? hits[0] : null, dmg * 2.2f));
                    }
                    break;

                // ------------------------------ Archer: Utility
                case "ar_utl_4":    // Frost Arrow
                    {
                        Vector3 end; var hits = AimHits(pc, 70f, false, out end);
                        Audio.Sfx("bow_shot", pc.transform.position);
                        CombatFx.Muzzle(Muzzle(pc), CombatFx.IceCol);
                        Vector3 e2 = end;
                        CombatFx.Arrow(Muzzle(pc), e2, CombatFx.IceCol, 1.5f, 110f, () =>
                        {
                            CombatFx.Burst(e2, CombatFx.IceCol, 22, 0.9f, 2.4f, 0.9f, 0.2f);
                            CombatFx.ShockRing(Ground(e2), 2.2f, CombatFx.IceCol, 0.4f);
                        });
                        if (hits.Count > 0) { hits[0].TakeDamage(dmg); hits[0].Slow(0.35f, 7f); }
                    }
                    break;
                case "ar_utl_5":    // Net Trap
                    {
                        Vector3 c = AimGround(pc, 18f);
                        Audio.Sfx("paper", c);
                        CombatFx.Zone(c, 4.5f, new Color(0.6f, 1f, 0.5f, 0.9f), 4f);
                        CombatFx.ShockRing(c, 4.5f, new Color(0.6f, 1f, 0.5f, 0.9f), 0.4f);
                        CombatFx.Burst(c + Vector3.up * 0.3f, new Color(0.7f, 1f, 0.6f), 26, 3.6f, 1.4f, 1.2f, 0.16f);
                        foreach (var m in InRadius(c, 4.5f)) { m.Stagger(4f); m.TakeDamage(dmg * 0.4f); }
                    }
                    break;
                case "ar_utl_6":    // Snare Field (ultimate)
                    host.StartCoroutine(SnareField(pc, AimGround(pc, 22f), dmg));
                    break;

                // ------------------------------ Archer: Mobility
                case "ar_mob_4":    // Retreat Shot
                    {
                        pc.Invuln(0.25f);
                        pc.DashTo(-fwd, 0.25f, 20f);
                        CombatFx.Dust(Feet(pc), 14);
                        Vector3 end; var hits = AimHits(pc, 70f, false, out end);
                        Audio.Sfx("bow_shot", pc.transform.position);
                        CombatFx.Muzzle(Muzzle(pc), CombatFx.BowCol);
                        CombatFx.Arrow(Muzzle(pc), end, CombatFx.BowCol, 1.4f, 120f);
                        if (hits.Count > 0) hits[0].TakeDamage(dmg);
                    }
                    break;
                case "ar_mob_5":    // Aerial Volley
                    host.StartCoroutine(Volley(pc, AimGround(pc, 40f), dmg, 6, 0.07f, 2.4f, 0.4f));
                    break;
                case "ar_mob_6":    // Storm Barrage (ultimate)
                    host.StartCoroutine(Volley(pc, AimGround(pc, 45f), dmg, 40, 0.07f, 8f, 0.1f));
                    break;

                default:
                    if (!RunMage(s, pc, dmg)) CombatFx.Burst(Chest(pc), CombatFx.SwordCol, 10, 0.5f, 1f);
                    break;
            }
        }

        // =====================================================================
        // Penyihir Api & Penyihir Tanah
        // =====================================================================
        static readonly Dictionary<int, float> burning = new Dictionary<int, float>();

        /// <summary>Membakar monster: damage berkelanjutan (dps) selama secs detik; menyalakan ulang menambah durasi saja.</summary>
        public static void Burn(Monster m, float dps, float secs)
        {
            if (m == null || m.IsDead) return;
            int id = m.GetInstanceID();
            float until;
            bool running = burning.TryGetValue(id, out until) && until > Time.time;
            burning[id] = Time.time + secs;
            if (!running && CombatFx.Host.I != null) CombatFx.Host.I.StartCoroutine(BurnRoutine(m, id, dps));
        }

        static IEnumerator BurnRoutine(Monster m, int id, float dps)
        {
            while (m != null && !m.IsDead && burning.ContainsKey(id) && Time.time < burning[id])
            {
                yield return new WaitForSeconds(0.5f);
                if (m == null || m.IsDead) break;
                CombatFx.Burst(m.HitPoint, CombatFx.FireCol, 6, 0.5f, 1.4f, 0.6f, 0.12f);
                m.TakeDamage(dps * 0.5f);
            }
            burning.Remove(id);
        }

        static void Explode(Vector3 at, float radius, Color col, bool earth)
        {
            CombatFx.ShockRing(Ground(at), radius, col, 0.4f);
            if (earth) MagicFx.RockBurst(Ground(at), radius, 8 + Mathf.RoundToInt(radius * 2f));
            else MagicFx.Fireball(Ground(at), radius);
        }

        static bool RunMage(SkillDef s, PlayerCombat pc, float dmg)
        {
            var host = CombatFx.Host.I;
            Vector3 fwd = pc.transform.forward;
            Color fire = CombatFx.FireCol, earth = CombatFx.EarthCol;
            bool isFire = s.id.StartsWith("fm_");
            MagicFx.CastCircle(Feet(pc), isFire ? fire : earth, s.tier >= 3 ? 3.4f : 2.4f, s.tier >= 3 ? 1.2f : 0.8f);
            MagicFx.CastCharge(isFire ? fire : earth, 0.3f, s.tier >= 3 ? 0.7f : 0.45f);
            switch (s.id)
            {
                // ---------------------------------------------- Penyihir Api
                case "fm_inf_4":    // Fireball
                    {
                        Vector3 end; var hits = AimHits(pc, 60f, false, out end);
                        Audio.Sfx("skill_cast", pc.transform.position);
                        CombatFx.Muzzle(Muzzle(pc), fire);
                        Vector3 e2 = end;
                        MagicFx.Orb(Muzzle(pc), e2, fire, 0.9f, 34f, () =>
                        {
                            Explode(e2, 4.2f, fire, false);
                            foreach (var m in InRadius(e2, 4.2f)) { m.TakeDamage(dmg); Burn(m, dmg * 0.12f, 4f); }
                        });
                    }
                    return true;
                case "fm_inf_5":    // Flame Wave
                    {
                        Audio.Sfx("heavy_swing", pc.transform.position);
                        for (int i = 0; i < 3; i++) CombatFx.SlashArc(Feet(pc) + Vector3.up * (0.5f + i * 0.5f), pc.transform.rotation, 5.2f + i * 0.8f, 0f, i == 1 ? CombatFx.SwordGold : fire, 0.4f, 1.6f);
                        CombatFx.Burst(Chest(pc) + fwd * 3f, fire, 40, 2.4f, 2f, 0.9f, 0.26f);
                        CombatFx.ShockRing(Feet(pc) + fwd * 3f, 6f, fire, 0.4f);
                        foreach (var m in InCone(pc.transform.position, fwd, 7.5f, 70f)) { m.TakeDamage(dmg * 0.9f); Burn(m, dmg * 0.1f, 4f); m.Knock(fwd, 0.8f); }
                    }
                    return true;
                case "fm_inf_6":    // Meteor Fall
                    host.StartCoroutine(MeteorFall(pc, AimGround(pc, 45f), dmg));
                    return true;
                case "fm_wrd_4":    // Flame Barrier
                    pc.Buff(0.5f, 5f);
                    host.StartCoroutine(AuraTicks(pc, 5f, 3.4f, fire, dmg * 0.14f, true));
                    if (TownUI.I != null) TownUI.I.Toast("Flame Barrier: damage masuk -50% selama 5 dtk.");
                    return true;
                case "fm_wrd_5":    // Searing Ring
                    host.StartCoroutine(AuraTicks(pc, 5f, 5.2f, CombatFx.MagmaCol, dmg * 0.22f, true));
                    return true;
                case "fm_wrd_6":    // Phoenix Rebirth
                    {
                        pc.Invuln(1.2f);
                        pc.Heal(pc.MaxHp * 0.6f);
                        Audio.Sfx("potion", pc.transform.position);
                        Explode(pc.transform.position, 8f, fire, false);
                        CombatFx.Burst(Chest(pc), CombatFx.SwordGold, 40, 1.2f, 6f, 1.2f, 0.28f);
                        foreach (var m in InRadius(pc.transform.position, 8f)) { m.TakeDamage(dmg * 1.1f); Burn(m, dmg * 0.12f, 5f); m.Knock((m.transform.position - pc.transform.position).normalized, 1.5f); }
                        if (TownUI.I != null) TownUI.I.Toast("Phoenix Rebirth!");
                    }
                    return true;
                case "fm_stp_4":    // Fire Dash
                    {
                        Vector3 a = pc.transform.position;
                        pc.Invuln(0.35f);
                        pc.DashTo(fwd, 0.22f, 36f);
                        host.StartCoroutine(FireDash(pc, a, dmg));
                    }
                    return true;
                case "fm_stp_5":    // Flame Pillar
                    {
                        Vector3 c = AimGround(pc, 30f);
                        Audio.Sfx("heavy_swing", c);
                        CombatFx.Zone(c, 3f, fire, 1.2f);
                        host.StartCoroutine(Pillar(c, dmg));
                    }
                    return true;
                case "fm_stp_6":    // Blazing Comet
                    host.StartCoroutine(Comet(pc, dmg));
                    return true;

                // ---------------------------------------------- Penyihir Tanah
                case "em_stn_4":    // Rock Spike
                    host.StartCoroutine(RockSpikes(pc, dmg));
                    return true;
                case "em_stn_5":    // Boulder Toss
                    {
                        Vector3 end; var hits = AimHits(pc, 45f, false, out end);
                        Audio.Sfx("skill_cast", pc.transform.position);
                        Vector3 e2 = end;
                        MagicFx.Orb(Muzzle(pc) + Vector3.up * 0.6f, e2, earth, 1.5f, 24f, () =>
                        {
                            Explode(e2, 4f, earth, true);
                            Audio.Sfx("heavy_swing", e2);
                            foreach (var m in InRadius(e2, 4f)) { m.TakeDamage(dmg); m.Stagger(1.2f); }
                        });
                    }
                    return true;
                case "em_stn_6":    // Earthquake
                    host.StartCoroutine(Earthquake(pc, dmg));
                    return true;
                case "em_bed_4":    // Stone Skin
                    pc.Buff(0.4f, 6f);
                    CombatFx.ShockRing(Feet(pc), 3f, earth, 0.5f);
                    CombatFx.Burst(Chest(pc), earth, 24, 0.9f, 1.6f, 0.9f, 0.16f);
                    if (TownUI.I != null) TownUI.I.Toast("Stone Skin: damage masuk -60% selama 6 dtk.");
                    return true;
                case "em_bed_5":    // Stone Wall
                    {
                        Vector3 c = Ground(pc.transform.position + fwd * 3.6f);
                        Audio.Sfx("block", c);
                        Vector3 side = Vector3.Cross(Vector3.up, fwd);
                        for (int i = -3; i <= 3; i++)
                        {
                            Vector3 p = c + side * (i * 1.0f);
                            CombatFx.Beam(Ground(p), Ground(p) + Vector3.up * (2.6f - Mathf.Abs(i) * 0.25f), earth, 0.9f, 3f);
                            CombatFx.Dust(Ground(p), 4);
                        }
                        CombatFx.ShockRing(c, 4f, earth, 0.4f);
                        foreach (var m in InRadius(c, 3.6f)) { m.TakeDamage(dmg * 0.6f); m.Stagger(3f); m.Knock(fwd, 1.8f); }
                    }
                    return true;
                case "em_bed_6":    // Titan Bulwark
                    host.StartCoroutine(TitanBulwark(pc));
                    return true;
                case "em_flw_4":    // Quicksand
                    host.StartCoroutine(Quicksand(AimGround(pc, 30f), dmg));
                    return true;
                case "em_flw_5":    // Root Bind
                    {
                        Vector3 c = AimGround(pc, 30f);
                        Color root = new Color(0.45f, 0.75f, 0.3f, 0.9f);
                        Audio.Sfx("paper", c);
                        CombatFx.Zone(c, 6f, root, 4f);
                        CombatFx.ShockRing(c, 6f, root, 0.5f);
                        CombatFx.Burst(c + Vector3.up * 0.2f, root, 36, 5.5f, 2.2f, 1.2f, 0.18f);
                        foreach (var m in InRadius(c, 6f)) { m.TakeDamage(dmg * 0.5f); m.Stagger(4f); }
                    }
                    return true;
                case "em_flw_6":    // Terra Surge
                    host.StartCoroutine(TerraSurge(pc, dmg));
                    return true;
            }
            return false;
        }

        static IEnumerator AuraTicks(PlayerCombat pc, float secs, float radius, Color col, float tickDmg, bool burn)
        {
            for (float t = 0f; t < secs; t += 0.5f)
            {
                if (pc == null || pc.IsDead) yield break;
                CombatFx.ShockRing(Feet(pc), radius, col, 0.5f);
                CombatFx.Burst(Chest(pc), col, 10, radius * 0.5f, 2f, 0.8f, 0.16f);
                foreach (var m in InRadius(pc.transform.position, radius)) { m.TakeDamage(tickDmg); if (burn) Burn(m, tickDmg * 0.6f, 2f); }
                yield return new WaitForSeconds(0.5f);
            }
        }

        static IEnumerator MeteorFall(PlayerCombat pc, Vector3 c, float dmg)
        {
            Audio.Sfx("boss_roar", c);
            CombatFx.Zone(c, 8f, CombatFx.MagmaCol, 3.2f);
            CombatFx.Flash(c + Vector3.up * 2f, CombatFx.FireCol, 20f, 2f, 0.5f);
            yield return new WaitForSeconds(0.8f);
            for (int i = 0; i < 7; i++)
            {
                Vector2 r = i == 0 ? Vector2.zero : Random.insideUnitCircle * 6.5f;
                Vector3 tgt = Ground(c + new Vector3(r.x, 0f, r.y));
                Vector3 from = tgt + new Vector3(Random.Range(-6f, 6f), 30f, Random.Range(-6f, 6f));
                MagicFx.Orb(from, tgt, CombatFx.FireCol, 1.6f, 44f, () =>
                {
                    Explode(tgt, 3.6f, CombatFx.FireCol, false);
                    Audio.Sfx("heavy_swing", tgt);
                    foreach (var m in InRadius(tgt, 3.6f)) { m.TakeDamage(dmg * 0.45f); Burn(m, dmg * 0.08f, 4f); }
                });
                yield return new WaitForSeconds(0.28f);
            }
        }

        static IEnumerator FireDash(PlayerCombat pc, Vector3 start, float dmg)
        {
            yield return new WaitForSeconds(0.24f);
            if (pc == null) yield break;
            Vector3 end = pc.transform.position;
            Beam(start + Vector3.up, end + Vector3.up, new Color(1f, 0.6f, 0.2f, 0.9f), 0.9f, 0.5f);
            for (int i = 0; i <= 5; i++) CombatFx.Burst(Vector3.Lerp(start, end, i / 5f), CombatFx.FireCol, 8, 0.7f, 1.8f, 0.8f, 0.2f);
            Audio.Sfx("heavy_swing", end);
            foreach (var m in new List<Monster>(Monster.All))
            {
                if (m == null || m.IsDead) continue;
                Vector3 p = m.transform.position; p.y = 0f;
                Vector3 a = start; a.y = 0f; Vector3 b = end; b.y = 0f;
                Vector3 ab = b - a;
                float t = ab.sqrMagnitude < 0.01f ? 0f : Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
                if (Vector3.Distance(p, a + ab * t) <= 2.4f + m.Radius) { m.TakeDamage(dmg); Burn(m, dmg * 0.12f, 4f); }
            }
        }

        static IEnumerator Pillar(Vector3 c, float dmg)
        {
            yield return new WaitForSeconds(0.55f);
            Audio.Sfx("boss_roar", c);
            CombatFx.Beam(c, c + Vector3.up * 12f, CombatFx.FireCol, 2.6f, 0.9f);
            CombatFx.Beam(c, c + Vector3.up * 12f, Color.white, 1.2f, 0.6f);
            Explode(c, 3.4f, CombatFx.FireCol, false);
            foreach (var m in InRadius(c, 3.2f)) { m.TakeDamage(dmg * 1.2f); m.Stagger(1.6f); Burn(m, dmg * 0.14f, 5f); }
        }

        static IEnumerator Comet(PlayerCombat pc, float dmg)
        {
            pc.Invuln(0.8f);
            Vector3 fwd = pc.transform.forward;
            pc.DashTo(fwd, 0.3f, 16f);
            CombatFx.Burst(Chest(pc), CombatFx.FireCol, 24, 0.8f, 4f, 0.8f, 0.2f);
            yield return new WaitForSeconds(0.42f);
            if (pc == null) yield break;
            Vector3 c = Ground(pc.transform.position);
            Audio.Sfx("boss_roar", c);
            Explode(c, 8f, CombatFx.FireCol, false);
            CombatFx.ShockRing(c, 11f, CombatFx.SwordGold, 0.5f);
            CombatFx.Beam(c, c + Vector3.up * 9f, CombatFx.FireCol, 3f, 0.5f);
            foreach (var m in InRadius(c, 8f)) { m.TakeDamage(dmg * 1.4f); Burn(m, dmg * 0.14f, 5f); m.Knock((m.transform.position - c).normalized, 1.6f); }
        }

        static IEnumerator RockSpikes(PlayerCombat pc, float dmg)
        {
            Vector3 o = pc.transform.position, fwd = pc.transform.forward;
            Audio.Sfx("heavy_swing", o);
            var hit = new HashSet<Monster>();
            for (int i = 1; i <= 8; i++)
            {
                Vector3 p = Ground(o + fwd * (i * 1.7f));
                Vector3 side = Vector3.Cross(Vector3.up, fwd);
                for (int k = -1; k <= 1; k++)
                {
                    Vector3 pp = p + side * (k * 0.7f);
                    CombatFx.Beam(Ground(pp), Ground(pp) + Vector3.up * (1.8f + 0.3f * i * 0.2f), CombatFx.EarthCol, 0.55f, 0.7f);
                }
                CombatFx.Dust(p, 6);
                foreach (var m in InRadius(p, 1.8f)) if (hit.Add(m)) { m.TakeDamage(dmg); m.Stagger(1f); }
                yield return new WaitForSeconds(0.05f);
            }
        }

        static IEnumerator Earthquake(PlayerCombat pc, float dmg)
        {
            Vector3 c = Ground(pc.transform.position);
            Audio.Sfx("boss_roar", c);
            for (int i = 0; i < 4; i++)
            {
                float r = 4f + i * 2.6f;
                CombatFx.ShockRing(c, r, CombatFx.EarthCol, 0.5f);
                CombatFx.Dust(c, 14);
                CombatFx.Burst(c + Vector3.up * 0.2f, CombatFx.EarthCol, 20, r * 0.8f, 2.4f, 0.9f, 0.24f);
                foreach (var m in InRadius(c, r)) { m.TakeDamage(dmg * 0.5f); m.Stagger(1f); }
                yield return new WaitForSeconds(0.4f);
            }
        }

        static IEnumerator TitanBulwark(PlayerCombat pc)
        {
            pc.Buff(0.25f, 10f);
            pc.Heal(pc.MaxHp * 0.2f);
            foreach (var m in new List<Monster>(Monster.All)) if (m != null && !m.IsDead && Vector3.Distance(m.transform.position, pc.transform.position) < 30f) m.Alert();
            Audio.Sfx("block", pc.transform.position);
            if (TownUI.I != null) TownUI.I.Toast("Titan Bulwark: damage masuk -75% selama 10 dtk, musuh mengincarmu!");
            for (float t = 0f; t < 10f; t += 0.5f)
            {
                if (pc == null || pc.IsDead) yield break;
                CombatFx.ShockRing(Feet(pc), 2.8f, CombatFx.EarthCol, 0.6f);
                CombatFx.Burst(Chest(pc), CombatFx.EarthCol, 5, 1f, 1.2f, 0.7f, 0.14f);
                yield return new WaitForSeconds(0.5f);
            }
        }

        static IEnumerator Quicksand(Vector3 c, float dmg)
        {
            Color col = new Color(0.85f, 0.7f, 0.4f, 0.9f);
            Audio.Sfx("paper", c);
            CombatFx.Zone(c, 5.5f, col, 6f);
            CombatFx.ShockRing(c, 5.5f, col, 0.5f);
            for (float t = 0f; t < 6f; t += 0.5f)
            {
                CombatFx.Burst(c + Vector3.up * 0.2f, col, 8, 5f, 1f, 0.9f, 0.16f);
                foreach (var m in InRadius(c, 5.5f)) { m.Slow(0.2f, 0.8f); m.TakeDamage(dmg * 0.1f); }
                yield return new WaitForSeconds(0.5f);
            }
        }

        static IEnumerator TerraSurge(PlayerCombat pc, float dmg)
        {
            Vector3 o = pc.transform.position, fwd = pc.transform.forward;
            Audio.Sfx("boss_roar", o);
            var hit = new HashSet<Monster>();
            for (int i = 1; i <= 12; i++)
            {
                Vector3 p = Ground(o + fwd * (i * 1.6f));
                CombatFx.ShockRing(p, 3.4f, CombatFx.EarthCol, 0.4f);
                CombatFx.Burst(p + Vector3.up * 0.3f, CombatFx.EarthCol, 12, 2.2f, 4f, 0.9f, 0.3f);
                CombatFx.Beam(p, p + Vector3.up * 3f, CombatFx.EarthCol, 1.4f, 0.5f);
                CombatFx.Dust(p, 8);
                foreach (var m in InRadius(p, 3f)) if (hit.Add(m)) { m.TakeDamage(dmg * 1.8f); m.Stagger(2f); m.Knock(fwd, 1.5f); }
                yield return new WaitForSeconds(0.05f);
            }
        }

        // =====================================================================
        // Skill berbentuk rangkaian
        // =====================================================================
        static IEnumerator ExecutionerArc(PlayerCombat pc, float dmg)
        {
            Vector3 fwd = pc.transform.forward;
            Audio.Sfx("heavy_swing", pc.transform.position);
            CombatFx.Flash(Chest(pc), CombatFx.SwordGold, 14f, 2.4f, 0.5f);
            CombatFx.ShockRing(Feet(pc), 3f, CombatFx.SwordGold, 0.4f);
            yield return new WaitForSeconds(0.25f);
            if (pc == null) yield break;
            Audio.Sfx("heavy_swing", pc.transform.position);
            for (int i = 0; i < 3; i++)
                CombatFx.SlashArc(Chest(pc) + Vector3.up * (i * 0.15f), pc.transform.rotation, 4.6f + i * 1.0f, -20f + i * 20f, i == 1 ? Color.white : CombatFx.SwordGold, 0.34f, 1.35f);
            CombatFx.ShockRing(Feet(pc) + fwd * 2.5f, 6.5f, CombatFx.SwordGold, 0.5f);
            CombatFx.Dust(Feet(pc) + fwd * 3f, 22);
            CombatFx.Flash(Chest(pc) + fwd * 3f, CombatFx.SwordGold, 16f, 3f, 0.3f);
            foreach (var m in InCone(pc.transform.position, fwd, 6.2f, 100f)) { m.TakeDamage(dmg * 1.6f); m.Knock(fwd, 1.2f); }
        }

        static IEnumerator Bulwark(PlayerCombat pc)
        {
            pc.Buff(0.3f, 10f);
            foreach (var m in new List<Monster>(Monster.All)) if (m != null && !m.IsDead && Vector3.Distance(m.transform.position, pc.transform.position) < 30f) m.Alert();
            Audio.Sfx("block", pc.transform.position);
            if (TownUI.I != null) TownUI.I.Toast("Bulwark: damage masuk -70% selama 10 dtk, musuh mengincarmu!");
            for (float t = 0f; t < 10f; t += 0.5f)
            {
                if (pc == null || pc.IsDead) yield break;
                CombatFx.ShockRing(Feet(pc), 2.4f, new Color(0.5f, 0.75f, 1f, 0.8f), 0.6f);
                CombatFx.Burst(Chest(pc), new Color(0.6f, 0.8f, 1f), 4, 0.9f, 1.2f, 0.7f, 0.1f);
                yield return new WaitForSeconds(0.5f);
            }
        }

        static IEnumerator BlinkHit(PlayerCombat pc, Vector3 start, float dmg)
        {
            yield return new WaitForSeconds(0.43f);
            if (pc == null) yield break;
            Vector3 end = pc.transform.position;
            Audio.Sfx("swing2", end);
            // semua monster di jalur dash + di titik tujuan
            foreach (var m in new List<Monster>(Monster.All))
            {
                if (m == null || m.IsDead) continue;
                Vector3 p = m.transform.position; p.y = 0f;
                Vector3 a = start; a.y = 0f; Vector3 b = end; b.y = 0f;
                Vector3 ab = b - a;
                float t = ab.sqrMagnitude < 0.01f ? 0f : Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
                float d = Vector3.Distance(p, a + ab * t);
                if (d <= 2.2f + m.Radius || Vector3.Distance(p, b) <= 3f + m.Radius) m.TakeDamage(dmg);
            }
        }

        static IEnumerator FlurryHits(Vector3 center, float dmg)
        {
            for (int i = 0; i < 4; i++)
            {
                foreach (var m in InRadius(center, 3.6f)) m.TakeDamage(dmg * 0.45f);
                yield return new WaitForSeconds(0.03f);
            }
            foreach (var m in InRadius(center, 3.8f)) { m.TakeDamage(dmg * 1.5f); m.Knock((m.transform.position - center).normalized, 1.2f); }
        }

        static IEnumerator Flurry(PlayerCombat pc, float dmg)
        {
            pc.Invuln(1.4f);
            Audio.Sfx("heavy_swing", pc.transform.position);
            CombatFx.Flash(Chest(pc), CombatFx.IceCol, 12f, 2f, 0.4f);
            for (int i = 0; i < 8; i++)
            {
                if (pc == null || pc.IsDead) yield break;
                pc.FaceAim();
                Audio.Sfx(i % 2 == 0 ? "swing" : "swing2", pc.transform.position);
                float roll = Random.Range(-70f, 70f);
                CombatFx.SlashArc(Chest(pc) + Vector3.up * Random.Range(-0.2f, 0.4f), pc.transform.rotation * Quaternion.Euler(0f, Random.Range(-25f, 25f), 0f), Random.Range(3f, 4.2f), roll, i % 3 == 0 ? Color.white : CombatFx.IceCol, 0.16f, 1.1f);
                foreach (var m in InCone(pc.transform.position, pc.transform.forward, 4.2f, 90f)) m.TakeDamage(dmg * 0.34f);
                yield return new WaitForSeconds(0.14f);
            }
            if (pc != null)
            {
                CombatFx.ShockRing(Feet(pc), 5f, CombatFx.IceCol, 0.4f);
                foreach (var m in InCone(pc.transform.position, pc.transform.forward, 4.5f, 100f)) m.TakeDamage(dmg * 0.6f);
            }
        }

        static IEnumerator SnareField(PlayerCombat pc, Vector3 c, float dmg)
        {
            Audio.Sfx("paper", c);
            var col = new Color(0.55f, 1f, 0.6f, 0.9f);
            CombatFx.Zone(c, 8f, col, 8f);
            CombatFx.ShockRing(c, 8f, col, 0.5f);
            CombatFx.Flash(c + Vector3.up, col, 14f, 2f, 0.4f);
            for (float t = 0f; t < 8f; t += 0.5f)
            {
                CombatFx.Burst(c + Vector3.up * 0.2f, col, 6, 7f, 1f, 0.9f, 0.14f);
                foreach (var m in InRadius(c, 8f)) { m.Slow(0.25f, 0.8f); m.TakeDamage(dmg * 0.12f); }
                yield return new WaitForSeconds(0.5f);
            }
        }

        static IEnumerator Volley(PlayerCombat pc, Vector3 c, float dmg, int arrows, float gap, float radius, float hitMult)
        {
            Audio.Sfx("bow_shot", pc.transform.position);
            CombatFx.Zone(c, radius, new Color(1f, 0.85f, 0.4f, 0.7f), 0.9f + arrows * gap);
            if (arrows > 10) CombatFx.Flash(c + Vector3.up * 3f, CombatFx.SwordGold, 18f, 2f, 0.5f);
            for (int i = 0; i < arrows; i++)
            {
                Vector2 r = Random.insideUnitCircle * radius;
                Vector3 tgt = Ground(c + new Vector3(r.x, 0f, r.y));
                Vector3 from = tgt + new Vector3(Random.Range(-3f, 3f), 22f, Random.Range(-3f, 3f));
                float splash = Mathf.Max(1.6f, radius * 0.3f);
                float dd = dmg * hitMult * (arrows > 10 ? 1f : 1f);
                Vector3 t2 = tgt;
                CombatFx.Arrow(from, t2, i % 4 == 0 ? Color.white : CombatFx.BowCol, 1.2f, 70f, () =>
                {
                    CombatFx.HitSpark(t2, CombatFx.BowCol, 0.7f, 6);
                    CombatFx.Dust(t2, 3);
                    foreach (var m in InRadius(t2, splash)) m.TakeDamage(dd);
                });
                if (i % 5 == 0) Audio.Sfx("bow_shot", c);
                yield return new WaitForSeconds(gap);
            }
        }

        // =====================================================================
        // Counter Riposte (dipanggil PlayerCombat.TakeDamage saat jendela riposte aktif)
        // =====================================================================
        public static void Counter(PlayerCombat pc)
        {
            var gs = GameState.I;
            float dmg = (10f + (gs != null ? gs.TotalAtk : 0)) * 2.4f;
            Monster best = null; float bd = 7f;
            foreach (var m in Monster.All)
            {
                if (m == null || m.IsDead) continue;
                float d = Vector3.Distance(m.transform.position, pc.transform.position);
                if (d < bd) { bd = d; best = m; }
            }
            CombatFx.ShockRing(Feet(pc), 3f, CombatFx.SwordGold, 0.3f);
            if (best == null) return;
            Vector3 d2 = best.transform.position - pc.transform.position; d2.y = 0f;
            if (d2.sqrMagnitude > 0.01f) pc.transform.rotation = Quaternion.LookRotation(d2.normalized);
            CombatFx.SlashArc(Chest(pc), pc.transform.rotation, 3.5f, 90f, CombatFx.SwordGold, 0.28f, 1.1f);
            CombatFx.SlashArc(best.HitPoint, Quaternion.LookRotation(d2.sqrMagnitude > 0.01f ? d2.normalized : Vector3.forward), 2.2f, 30f, Color.white, 0.22f, 1f);
            best.TakeDamage(dmg);
            best.Stagger(1.2f);
            if (TownUI.I != null) TownUI.I.Toast("RIPOSTE!");
        }
    }
}
