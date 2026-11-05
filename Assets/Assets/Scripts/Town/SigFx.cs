using System.Collections;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Pola efek visual khas untuk skill evolusi (SkillDef.sig). Dua skill dengan jenis dan elemen yang sama tetap tampil berbeda:
    /// pola, warna (rona digeser per skill), jumlah, dan arah dibedakan dari id skill. Hanya visual; damage dikerjakan SkillExec.
    /// </summary>
    public static class SigFx
    {
        struct Ctx
        {
            public Vector3 C, feet, F, right;
            public Color col, hot;
            public float r, reach, dur;
            public int h;
        }

        static Color ElemColor(SkillDef s)
        {
            string e = !string.IsNullOrEmpty(s.elem) ? s.elem : JobInfo.Elem(s.job);
            if (e == "phys" || e == "") return CombatFx.SwordCol;
            if (e == "arrow") return CombatFx.BowCol;
            return MagicFx.ElemCol(e);
        }

        static int Hash(string s) { int h = 7; foreach (char c in s) h = h * 31 + c; return h & 0x7fffffff; }

        public static void Cast(SkillDef s, PlayerCombat pc)
        {
            if (s == null || pc == null || CombatFx.Host.I == null) return;
            int h = Hash(s.id);
            var pal = SkillLook.Of(s);   // warna ditentukan tema nama skill (api=oranye, es=biru muda, petir=kuning-ungu, racun=hijau, dst)
            Color col = pal.m;
            float hh, ss, vv; Color.RGBToHSV(col, out hh, out ss, out vv);
            col = Color.HSVToRGB(Mathf.Repeat(hh + ((h % 100) / 100f - 0.5f) * 0.03f, 1f), Mathf.Clamp01(ss), Mathf.Max(0.85f, vv));
            Vector3 feet = pc.transform.position - Vector3.up * 0.9f;
            Vector3 F = pc.transform.forward; F.y = 0f; F = F.sqrMagnitude < 0.01f ? Vector3.forward : F.normalized;
            bool self = s.kind == "nova" || s.kind == "buff" || s.kind == "heal" || s.kind == "dash" || s.kind == "cone" || s.kind == "line";
            float reach = Mathf.Clamp(s.range > 0f ? s.range : 6f, 4f, 34f);
            Vector3 C = self ? feet + (s.kind == "line" || s.kind == "cone" || s.kind == "dash" ? F * Mathf.Min(reach * 0.5f, 8f) : Vector3.zero)
                             : (SkillAction.GroundCenterSet ? SkillAction.GroundCenter : SkillExec.AimGroundFor(pc, Mathf.Min(reach, 34f)));
            float r = s.kind == "line" ? Mathf.Clamp(reach * 0.25f, 3f, 7f) : s.kind == "cone" ? Mathf.Clamp(reach * 0.7f, 3f, 7f) : Mathf.Clamp(s.radius > 0f ? s.radius : 4f, 2.5f, 12f);
            float dur = Mathf.Clamp(Mathf.Max(1f, s.Fx("count", 1f)) * s.Fx("tick", 0.4f), 0.6f, 4f);
            var c = new Ctx { C = C, feet = feet, F = F, right = Vector3.Cross(Vector3.up, F), col = col, hot = pal.h, r = r, reach = reach, dur = dur, h = h };
            CombatFx.Host.I.StartCoroutine(Run(s.sig, c));
        }

        static IEnumerator Wait(float t) { yield return new WaitForSeconds(t); }
        static Vector3 Ring(Vector3 c, float rad, float ang, float y = 0f) { return c + new Vector3(Mathf.Cos(ang) * rad, y, Mathf.Sin(ang) * rad); }

        static IEnumerator Run(string sig, Ctx c)
        {
            Vector3 up = Vector3.up;
            switch (sig)
            {
                case "spiral":
                    {
                        int n = 22 + c.h % 10;
                        for (int i = 0; i < n; i++)
                        {
                            float k = i / (float)n;
                            CombatFx.Burst(Ring(c.C, c.r * (0.25f + 0.75f * k), i * 0.6f + c.h, 0.2f + k * 5f), i % 2 == 0 ? c.col : c.hot, 7, 0.4f, 1.2f, 0.5f);
                            yield return Wait(Mathf.Max(0.02f, c.dur / n));
                        }
                        CombatFx.ShockRing(c.C, c.r, c.hot, 0.45f);
                        break;
                    }
                case "pillars":
                    {
                        int n = 5 + c.h % 4; float off = (c.h % 10) * 0.3f;
                        for (int i = 0; i < n; i++)
                        {
                            Vector3 p = Ring(c.C, c.r * 0.8f, off + i * Mathf.PI * 2f / n);
                            MagicFx.Column(p, 0.65f, 7f, i % 2 == 0 ? c.col : c.hot, 0.7f); CombatFx.ShockRing(p, 1.6f, c.col, 0.3f);
                            yield return Wait(Mathf.Max(0.03f, c.dur / n * 0.7f));
                        }
                        MagicFx.Column(c.C, 1.6f, 11f, c.hot, 0.9f); CombatFx.ShockRing(c.C, c.r, c.col, 0.5f); ThirdPersonCamera.Shake(0.25f, 0.2f);
                        break;
                    }
                case "rain":
                    {
                        int m = 10 + c.h % 7;
                        for (int i = 0; i < m; i++)
                        {
                            Vector2 q = Random.insideUnitCircle * c.r; Vector3 p = c.C + new Vector3(q.x, 0f, q.y);
                            CombatFx.Beam(p + up * 15f + c.right * 2f, p, c.hot, 0.22f, 0.16f); CombatFx.Burst(p, c.col, 6, 0.6f, 1.6f, 0.5f, 0.2f); CombatFx.ShockRing(p, 1.4f, c.col, 0.25f);
                            yield return Wait(Mathf.Max(0.03f, c.dur / m));
                        }
                        break;
                    }
                case "comet":
                    {
                        Vector3 from = c.C + (-c.F + up * 1.4f).normalized * 22f;
                        for (int i = -1; i <= 1; i++) CombatFx.Beam(from + c.right * i * 0.7f, c.C + c.right * i * 0.3f, i == 0 ? c.hot : c.col, i == 0 ? 0.9f : 0.35f, 0.3f);
                        yield return Wait(0.28f);
                        CombatFx.Flash(c.C + up * 1.5f, c.hot, 18f, 4f, 0.3f); CombatFx.ShockRing(c.C, c.r * 1.1f, c.hot, 0.5f); CombatFx.ShockRing(c.C, c.r * 0.6f, c.col, 0.35f);
                        CombatFx.Burst(c.C + up, c.col, 40, c.r * 0.5f, 7f, 1f, 0.24f); CombatFx.Dust(c.C, 14); ThirdPersonCamera.Shake(0.4f, 0.3f);
                        break;
                    }
                case "geyser":
                    {
                        int n = 6 + c.h % 3;
                        for (int i = 0; i < n; i++)
                        {
                            Vector3 p = c.C + c.F * (i - n * 0.5f) * (c.r * 0.45f) + c.right * Mathf.Sin(i * 1.7f + c.h) * 1.1f;
                            MagicFx.Column(p, 0.9f, 6.5f, i % 2 == 0 ? c.col : c.hot, 0.6f); CombatFx.Burst(p, c.col, 10, 0.8f, 3f, 0.7f, 0.2f);
                            yield return Wait(0.07f);
                        }
                        break;
                    }
                case "vortex":
                    MagicFx.Tornado(c.C, c.r * 0.45f, 7f, c.col, c.dur + 0.5f); CombatFx.ShockRing(c.C, c.r, c.hot, 0.5f);
                    break;
                case "lotus":
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            CombatFx.ShockRing(c.C, c.r * (0.4f + i * 0.28f), i % 2 == 0 ? c.col : c.hot, 0.45f);
                            CombatFx.Burst(c.C + up * 0.3f, i % 2 == 0 ? c.hot : c.col, 14, c.r * (0.3f + i * 0.2f), 3f + i, 0.8f, 0.22f);
                            yield return Wait(0.09f);
                        }
                        MagicFx.Column(c.C, 0.9f, 5f, c.hot, 0.6f);
                        break;
                    }
                case "chains":
                    {
                        int n = 6 + c.h % 3;
                        for (int i = 0; i < n; i++)
                        {
                            Vector3 p = Ring(c.C, c.r * 0.9f, i * Mathf.PI * 2f / n + c.h);
                            CombatFx.Beam(p + up * 0.2f, c.C + up * 1.2f, c.col, 0.14f, 0.7f); CombatFx.Burst(p, c.hot, 5, 0.3f, 1f, 0.5f, 0.16f);
                            yield return Wait(0.04f);
                        }
                        CombatFx.Flash(c.C + up, c.hot, 12f, 3f, 0.35f); CombatFx.ShockRing(c.C, 2.5f, c.hot, 0.4f);
                        break;
                    }
                case "shards":
                    {
                        int n = 14 + c.h % 6;
                        for (int i = 0; i < n; i++)
                        {
                            float a = Random.value * (Mathf.PI * 2f);
                            Vector3 e = Ring(c.C, c.r * Random.Range(0.6f, 1.1f), a, Random.Range(0.4f, 3.5f));
                            CombatFx.Beam(c.C + up * 0.4f, e, i % 3 == 0 ? c.hot : c.col, 0.2f, 0.28f); CombatFx.HitSpark(e, c.hot, 0.5f, 5);
                            if (i % 4 == 3) yield return Wait(0.03f);
                        }
                        CombatFx.ShockRing(c.C, c.r, c.col, 0.35f);
                        break;
                    }
                case "starburst":
                    {
                        int n = 10 + c.h % 7;
                        CombatFx.Flash(c.C + up * 1.5f, c.hot, 14f, 4f, 0.3f); CombatFx.HitSpark(c.C + up * 1.2f, c.hot, 2.2f, 24);
                        for (int i = 0; i < n; i++)
                        {
                            float a = i * Mathf.PI * 2f / n + c.h * 0.1f;
                            CombatFx.Beam(c.C + up * 1.2f, Ring(c.C, c.r * 1.1f, a, 1.2f + (i % 3) * 1.8f), i % 2 == 0 ? c.hot : c.col, 0.3f, 0.4f);
                        }
                        yield return Wait(0.12f);
                        CombatFx.ShockRing(c.C, c.r, c.col, 0.4f);
                        break;
                    }
                case "halo":
                    {
                        int n = 12;
                        for (int i = 0; i < n; i++) CombatFx.Beam(Ring(c.C, c.r * 0.65f, i * Mathf.PI * 2f / n), Ring(c.C, c.r * 0.65f, i * Mathf.PI * 2f / n, 5.5f), i % 2 == 0 ? c.col : c.hot, 0.2f, Mathf.Max(0.6f, c.dur));
                        for (int i = 0; i < 20; i++) CombatFx.Beam(Ring(c.C, c.r * 0.65f, i * 0.314f, 5.5f), Ring(c.C, c.r * 0.65f, (i + 1) * 0.314f, 5.5f), c.hot, 0.16f, Mathf.Max(0.6f, c.dur));
                        MagicFx.Column(c.C, 1.1f, 9f, c.col, 0.9f); CombatFx.ShockRing(c.C, c.r * 0.65f, c.hot, 0.5f);
                        break;
                    }
                case "pulse":
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            CombatFx.Flash(c.C + up * (1f + i * 0.6f), i % 2 == 0 ? c.hot : c.col, 12f + i * 4f, 3.5f, 0.2f);
                            CombatFx.ShockRing(c.C, c.r * (0.5f + i * 0.35f), i % 2 == 0 ? c.hot : c.col, 0.35f);
                            CombatFx.Burst(c.C + up, c.col, 18, 0.5f, 4f + i * 1.5f, 0.8f, 0.2f);
                            yield return Wait(0.11f);
                        }
                        break;
                    }
                case "orbit":
                    {
                        int n = 5 + c.h % 3; int steps = 8;
                        for (int s = 0; s <= steps; s++)
                        {
                            float k = s / (float)steps;
                            for (int i = 0; i < n; i++) CombatFx.Burst(Ring(c.C, c.r * 0.9f * (1f - k), i * Mathf.PI * 2f / n + k * 5f, 1f + Mathf.Sin(k * 3f) * 1.5f), c.hot, 4, 0.3f, 0.8f, 0.4f, 0.22f);
                            yield return Wait(0.05f);
                        }
                        CombatFx.HitSpark(c.C + up * 1.2f, c.hot, 1.8f, 20); CombatFx.ShockRing(c.C, c.r * 0.6f, c.col, 0.4f);
                        break;
                    }
                case "web":
                    {
                        int n = 7; Vector3 prev = c.C + up * 0.8f;
                        for (int i = 0; i < n; i++)
                        {
                            Vector2 q = Random.insideUnitCircle * c.r; Vector3 p = c.C + new Vector3(q.x, Random.Range(0.3f, 3f), q.y);
                            Vector3 a = prev;
                            for (int sgm = 1; sgm <= 3; sgm++)
                            {
                                Vector3 b = Vector3.Lerp(prev, p, sgm / 3f) + (sgm < 3 ? Random.insideUnitSphere * 0.9f : Vector3.zero);
                                CombatFx.Beam(a, b, sgm % 2 == 0 ? c.col : c.hot, 0.16f, 0.2f); a = b;
                            }
                            CombatFx.HitSpark(p, c.hot, 0.7f, 6); prev = p;
                            yield return Wait(0.04f);
                        }
                        CombatFx.Flash(c.C + up, c.hot, 10f, 3f, 0.25f);
                        break;
                    }
                case "waves":
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            CombatFx.HalfWave(c.feet + c.F * (0.8f + i * 0.5f), c.F, c.reach * (0.45f + i * 0.2f), i % 2 == 0 ? c.col : c.hot, 0.45f);
                            yield return Wait(0.1f);
                        }
                        break;
                    }
                case "crosscut":
                    {
                        int n = 3 + c.h % 2; Vector3 mid = c.C + up * 1.4f;
                        for (int i = 0; i < n; i++)
                        {
                            Vector3 d = Quaternion.AngleAxis((i - (n - 1) * 0.5f) * 38f + (i % 2 == 0 ? 8f : -8f), up) * c.F;
                            Vector3 tilt = Quaternion.AngleAxis((i % 2 == 0 ? 1 : -1) * 22f, c.F) * d;
                            CombatFx.Beam(mid - tilt * c.r * 1.3f, mid + tilt * c.r * 1.3f, i % 2 == 0 ? c.hot : c.col, 0.5f, 0.25f);
                            CombatFx.HitSpark(mid + tilt * c.r * 0.7f, c.hot, 0.8f, 8);
                            yield return Wait(0.07f);
                        }
                        CombatFx.HitSpark(mid, c.hot, 2f, 22); CombatFx.ShockRing(c.C, c.r * 0.7f, c.col, 0.3f);
                        break;
                    }
                case "claws":
                    {
                        int n = 3 + c.h % 2; float gap = 0.8f;
                        Vector3 o = c.C;
                        for (int i = 0; i < n; i++)
                        {
                            float off = (i - (n - 1) * 0.5f) * gap;
                            Vector3 a = o + c.right * off + up * 3.4f - c.F * 1.2f, b = o + c.right * off * 1.4f + up * 0.4f + c.F * 1.6f;
                            CombatFx.Beam(a, b, i % 2 == 0 ? c.hot : c.col, 0.4f, 0.28f); CombatFx.HitSpark(b, c.hot, 0.8f, 8);
                            yield return Wait(0.05f);
                        }
                        CombatFx.ShockRing(o, c.r * 0.6f, c.col, 0.3f); ThirdPersonCamera.Shake(0.2f, 0.12f);
                        break;
                    }
                case "bolts":
                    {
                        int n = 4 + c.h % 3;
                        for (int i = 0; i < n; i++)
                        {
                            Vector2 q = Random.insideUnitCircle * c.r; Vector3 p = c.C + new Vector3(q.x, 0f, q.y);
                            Vector3 a = p + up * 16f + c.right * Random.Range(-2f, 2f);
                            for (int sg = 1; sg <= 6; sg++)
                            {
                                Vector3 b = Vector3.Lerp(a, p, 1f / (7 - sg)) + (sg < 6 ? new Vector3(Random.Range(-1.2f, 1.2f), 0f, Random.Range(-1.2f, 1.2f)) : Vector3.zero);
                                CombatFx.Beam(a, b, sg % 2 == 0 ? c.col : c.hot, 0.2f, 0.22f); a = b;
                            }
                            CombatFx.Flash(p + up, c.hot, 9f, 3f, 0.15f); CombatFx.HitSpark(p, c.hot, 1.2f, 12); CombatFx.ShockRing(p, 1.8f, c.col, 0.25f);
                            yield return Wait(Mathf.Max(0.04f, c.dur / n * 0.6f));
                        }
                        ThirdPersonCamera.Shake(0.2f, 0.15f);
                        break;
                    }
                case "flames":
                    {
                        int n = 9 + c.h % 4;
                        for (int i = 0; i < n; i++)
                        {
                            float k = i / (float)n; float a = (k - 0.5f) * 1.9f;
                            Vector3 d = Quaternion.AngleAxis(a * Mathf.Rad2Deg, up) * c.F; Vector3 p = c.feet + d * (1.2f + k * 0f + c.r * (0.4f + 0.6f * Random.value));
                            CombatFx.Burst(p + up * 0.2f, i % 2 == 0 ? c.col : c.hot, 12, 0.7f, 4.5f, 0.7f, 0.3f);
                            MagicFx.Column(p, 0.7f, 3.2f + Random.value * 2f, c.col, 0.55f);
                            yield return Wait(0.035f);
                        }
                        CombatFx.Flash(c.feet + up * 1.2f + c.F * 2f, c.hot, 12f, 3f, 0.25f);
                        break;
                    }
                case "frost":
                    {
                        CombatFx.ShockRing(c.C, c.r, c.hot, 0.5f); CombatFx.ShockRing(c.C, c.r * 0.6f, c.col, 0.4f);
                        int n = 16;
                        for (int i = 0; i < n; i++)
                        {
                            float a = i * Mathf.PI * 2f / n + c.h * 0.1f; float rr = c.r * (0.45f + 0.5f * ((i % 3) / 2f));
                            Vector3 b = Ring(c.C, rr, a); Vector3 tip = b + up * (1.6f + (i % 4) * 0.7f);
                            CombatFx.Beam(b, tip, i % 2 == 0 ? c.hot : c.col, 0.34f, 0.8f); CombatFx.HitSpark(tip, c.hot, 0.5f, 5);
                            if (i % 4 == 3) yield return Wait(0.03f);
                        }
                        CombatFx.Burst(c.C + up * 0.4f, c.hot, 26, c.r * 0.6f, 2.2f, 1.1f, 0.14f);
                        break;
                    }
                case "blizzard":
                    {
                        int m = 22 + c.h % 8;
                        for (int i = 0; i < m; i++)
                        {
                            Vector2 q = Random.insideUnitCircle * c.r; Vector3 p = c.C + new Vector3(q.x, 0f, q.y);
                            CombatFx.Beam(p + up * 12f + c.right * 5f, p, i % 3 == 0 ? Color.white : c.hot, 0.1f, 0.3f);
                            if (i % 3 == 0) CombatFx.Burst(p, c.col, 5, 0.5f, 1.4f, 0.6f, 0.15f);
                            yield return Wait(Mathf.Max(0.02f, c.dur / m));
                        }
                        MagicFx.Tornado(c.C, c.r * 0.5f, 6f, c.hot, 1.2f);
                        break;
                    }
                case "cage":
                    {
                        int n = 10;
                        for (int i = 0; i < n; i++)
                        {
                            Vector3 p = Ring(c.C, c.r * 0.55f, i * Mathf.PI * 2f / n);
                            CombatFx.Beam(p, p + up * 4.5f, i % 2 == 0 ? c.col : c.hot, 0.22f, Mathf.Max(0.8f, c.dur));
                            yield return Wait(0.03f);
                        }
                        for (int k = 1; k <= 2; k++) for (int i = 0; i < 18; i++)
                            CombatFx.Beam(Ring(c.C, c.r * 0.55f, i * 0.349f, k * 1.7f), Ring(c.C, c.r * 0.55f, (i + 1) * 0.349f, k * 1.7f), c.hot, 0.12f, Mathf.Max(0.8f, c.dur));
                        CombatFx.Flash(c.C + up * 1.5f, c.hot, 10f, 2.5f, 0.3f);
                        break;
                    }
                case "blades":
                    {
                        int n = 5 + c.h % 3;
                        for (int i = 0; i < n; i++)
                        {
                            float a = (i - (n - 1) * 0.5f) * 14f; Vector3 d = Quaternion.AngleAxis(a, up) * c.F;
                            Vector3 s0 = c.feet + up * (0.8f + (i % 3) * 0.5f) + d * 1.2f, e0 = s0 + d * c.reach * 0.8f;
                            CombatFx.Beam(s0, e0, i % 2 == 0 ? c.hot : c.col, 0.12f, 0.3f);
                            CombatFx.HalfWave(s0 + d * 0.6f, d, c.reach * 0.5f, c.col, 0.3f);
                            yield return Wait(0.035f);
                        }
                        CombatFx.HitSpark(c.feet + c.F * c.reach * 0.7f + up, c.hot, 1.2f, 10);
                        break;
                    }
                case "smoke":
                    {
                        Color dk = Color.Lerp(c.col, Color.black, 0.45f);
                        for (int i = 0; i < 7; i++)
                        {
                            CombatFx.Burst(c.C + up * 0.6f, i % 2 == 0 ? dk : c.col, 16, c.r * (0.4f + i * 0.12f), 1.2f, 1.6f, 0.55f);
                            CombatFx.Dust(c.C, 6);
                            yield return Wait(0.07f);
                        }
                        CombatFx.Flash(c.C + up, dk, 8f, 1.5f, 0.2f);
                        break;
                    }
                case "poison":
                    {
                        Color gc = c.col;
                        for (int i = 0; i < 10; i++)
                        {
                            Vector2 q = Random.insideUnitCircle * c.r * 0.9f; Vector3 p = c.C + new Vector3(q.x, 0f, q.y);
                            CombatFx.Burst(p + up * 0.2f, i % 2 == 0 ? gc : c.hot, 8, 0.5f, 2.6f, 1.1f, 0.2f);
                            CombatFx.Beam(p + up * 3f, p, gc, 0.1f, 0.3f);
                            yield return Wait(0.05f);
                        }
                        CombatFx.ShockRing(c.C, c.r, gc, 0.5f);
                        break;
                    }
                case "roots":
                    {
                        int n = 8;
                        for (int i = 0; i < n; i++)
                        {
                            Vector3 p = Ring(c.C, c.r * (0.3f + 0.6f * Random.value), Random.value * 6.28f); Vector3 a = p;
                            for (int sg = 1; sg <= 4; sg++)
                            {
                                Vector3 b = p + up * sg * 0.9f + new Vector3(Random.Range(-0.5f, 0.5f), 0f, Random.Range(-0.5f, 0.5f)) * sg * 0.5f;
                                CombatFx.Beam(a, b, i % 2 == 0 ? c.col : c.hot, 0.2f - sg * 0.025f, 1f); a = b;
                            }
                            CombatFx.Burst(p, c.col, 6, 0.4f, 1.4f, 0.6f, 0.16f); CombatFx.Dust(p, 4);
                            yield return Wait(0.04f);
                        }
                        break;
                    }
                case "wall":
                    {
                        int n = 9;
                        for (int i = 0; i < n; i++)
                        {
                            Vector3 p = c.feet + c.F * Mathf.Min(c.reach * 0.5f, 7f) + c.right * (i - (n - 1) * 0.5f) * 1.4f;
                            MagicFx.Column(p, 0.8f, 5f + (i % 2) * 1.2f, i % 2 == 0 ? c.col : c.hot, 1.4f);
                            CombatFx.Dust(p, 3);
                            yield return Wait(0.035f);
                        }
                        break;
                    }
                case "wings":
                    {
                        for (int side = -1; side <= 1; side += 2)
                        {
                            int n = 8;
                            for (int i = 0; i < n; i++)
                            {
                                float k = i / (float)(n - 1);
                                Vector3 root = c.feet + up * 1.6f + c.right * side * 0.4f;
                                Vector3 tip = root + c.right * side * (2f + k * 2.2f) + up * (0.8f + Mathf.Sin(k * 2.4f) * 2.2f) - c.F * (0.3f + k * 1.2f);
                                CombatFx.Beam(root, tip, i % 2 == 0 ? c.col : c.hot, 0.3f - k * 0.1f, 0.7f); CombatFx.Burst(tip, c.col, 4, 0.3f, 1.2f, 0.6f, 0.16f);
                            }
                        }
                        CombatFx.Flash(c.feet + up * 1.8f, c.hot, 10f, 2.5f, 0.3f);
                        yield return Wait(0.15f);
                        CombatFx.ShockRing(c.feet, c.r * 0.8f, c.col, 0.4f);
                        break;
                    }
                case "ray":
                    {
                        Vector3 top = c.C + up * 22f - c.F * 4f;
                        CombatFx.Beam(top, c.C, c.hot, 1.1f, 0.5f); CombatFx.Beam(top, c.C, c.col, 2.0f, 0.4f);
                        yield return Wait(0.12f);
                        CombatFx.Flash(c.C + up * 1.5f, c.hot, 16f, 4f, 0.3f); CombatFx.ShockRing(c.C, c.r, c.col, 0.45f); CombatFx.Burst(c.C + up, c.hot, 30, c.r * 0.5f, 5f, 0.9f, 0.2f);
                        ThirdPersonCamera.Shake(0.3f, 0.2f);
                        break;
                    }
                case "smite":
                    {
                        int n = 3 + c.h % 2;
                        for (int i = 0; i < n; i++)
                        {
                            Vector3 p = i == 0 ? c.C : Ring(c.C, c.r * 0.7f, c.h + i * 2.1f);
                            CombatFx.Beam(p + up * 20f, p, c.hot, 0.6f, 0.4f);
                            MagicFx.Column(p, 0.7f, 8f, c.col, 0.7f); CombatFx.Flash(p + up, c.hot, 9f, 3f, 0.2f); CombatFx.ShockRing(p, 2f, c.col, 0.3f);
                            yield return Wait(0.09f);
                        }
                        break;
                    }
                case "bubble":
                    {
                        int n = 28;
                        for (int i = 0; i < n; i++)
                        {
                            float a = i * 2.399f, y = 0.2f + (i % 7) * 0.45f; float rr = c.r * 0.45f * Mathf.Sin(Mathf.Clamp01((y + 0.2f) / 3.4f) * 3.14f + 0.2f) + 0.5f;
                            CombatFx.Burst(Ring(c.feet, rr, a, y), i % 2 == 0 ? c.hot : c.col, 2, 0.08f, 0.2f, 1.2f, 0.28f);
                        }
                        CombatFx.ShockRing(c.feet, c.r * 0.5f, c.hot, 0.5f); CombatFx.Flash(c.feet + up * 1.2f, c.hot, 7f, 1.8f, 0.3f);
                        break;
                    }
                case "impact":
                    {
                        CombatFx.Flash(c.C + up, c.hot, 12f, 3f, 0.2f);
                        CombatFx.ShockRing(c.C, c.r, c.hot, 0.45f); CombatFx.ShockRing(c.C, c.r * 0.55f, c.col, 0.3f);
                        CombatFx.Burst(c.C + up * 0.3f, c.col, 24, c.r * 0.5f, 5f, 0.9f, 0.22f); CombatFx.Dust(c.C, 14);
                        ThirdPersonCamera.Shake(0.3f, 0.2f);
                        break;
                    }
                case "jaws":
                    {
                        int n = 7;
                        for (int i = 0; i < n; i++)
                        {
                            float k = i / (float)(n - 1) - 0.5f; Vector3 ax = c.C + c.F * k * c.r * 1.6f;
                            CombatFx.Beam(ax + c.right * (c.r * 0.9f) + up * 3.2f, ax + c.right * 0.25f + up * 0.2f, c.hot, 0.34f, 0.4f);
                            CombatFx.Beam(ax - c.right * (c.r * 0.9f) - up * 0f + up * 3.2f, ax - c.right * 0.25f + up * 0.2f, c.hot, 0.34f, 0.4f);
                        }
                        yield return Wait(0.18f);
                        CombatFx.Flash(c.C + up, c.col, 10f, 2.5f, 0.2f); CombatFx.HitSpark(c.C + up * 0.8f, c.hot, 1.8f, 20); ThirdPersonCamera.Shake(0.25f, 0.15f);
                        break;
                    }
                case "feathers":
                    {
                        int n = 7 + c.h % 3; Vector3 tip = c.feet + up * 1.7f;
                        for (int i = 0; i < n; i++)
                        {
                            float a = Mathf.Lerp(-55f, 55f, i / (float)(n - 1));
                            Vector3 d = Quaternion.AngleAxis(a, up) * c.F; Vector3 e = tip + d * c.reach * 0.7f + up * Mathf.Cos(a * Mathf.Deg2Rad) * 1.2f;
                            CombatFx.Beam(tip + d * 0.8f, e, i % 2 == 0 ? c.col : c.hot, 0.18f, 0.5f); CombatFx.Burst(e, c.hot, 8, 0.4f, 1.8f, 0.7f, 0.2f);
                            yield return Wait(0.025f);
                        }
                        break;
                    }
            }
        }
    }
}
