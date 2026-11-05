using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Siluet hewan mitologi untuk SEMUA ultimate (SkillDef.beast): serigala, singa bersayap, kuda bersayap, burung raksasa, naga, ular raksasa,
    /// kura-kura raksasa, kraken, banteng, rusa suci. Siluet muncul di samping pemain lalu menerjang / menyambar / menghantam hingga 4 musuh terdekat.
    /// Hanya visual; damage ultimate tetap dari SkillExec.
    /// </summary>
    public static partial class SkillSilhouette
    {
        public static void BeastStrike(SkillDef s, PlayerCombat pc, float delay)
        {
            if (s == null || pc == null || CombatFx.Host.I == null) return;
            string e = !string.IsNullOrEmpty(s.elem) ? s.elem : JobInfo.Elem(s.job);
            Color m = (e == "" || e == "phys" || e == "arrow") ? JobInfo.JobColor(s.job) : MagicFx.ElemCol(e);
            var col = new Col { m = m, h = Color.Lerp(m, Color.white, 0.6f), k = Color.Lerp(m, Color.black, 0.5f) };
            if (MyStrike(s, pc, col, delay)) return;      // makhluk khusus per nama (SkillSilhouette.MythRun)
            CombatFx.Host.I.StartCoroutine(BeastR(s.beast, pc, col, delay));
        }

        // ------------------------------------------------------------------ bentuk baru (x = arah hadap, y = atas, kaki di y = 0)
        static void Leg(List<Vector2[]> l, float x, float bot, float len, float swing, float dx)
        {
            Vector2 hip = V(x, bot);
            l.Add(new[] { hip, hip + V(Mathf.Sin(swing) * 0.7f + dx, -len * 0.5f), hip + V(Mathf.Sin(swing) * 1.3f + dx * 1.6f, -len) });
        }

        static List<Vector2[]> Quad(string t, float ph)
        {
            var l = new List<Vector2[]>();
            bool tall = t == "horse" || t == "stag", bull = t == "bull", lion = t == "lion";
            float leg = tall ? 2.4f : bull ? 1.6f : 1.5f, bh = bull ? 4.2f : tall ? 4.0f : 3.8f, bodyH = bull ? 2.7f : lion ? 2.2f : 2.3f;
            float bot = leg, top = leg + bodyH, neck = tall ? 2.0f : bull ? 0.2f : 0.8f, headL = t == "horse" ? 2.4f : t == "stag" ? 1.7f : 1.7f, hump = bull ? 0.9f : 0f;
            l.Add(new[] {
                V(-bh, top - 0.4f), V(-bh * 0.4f, top), V(bh * 0.45f, top + hump), V(bh * 0.8f, top + neck * 0.8f + hump * 0.4f),
                V(bh + headL * 0.35f, top + neck + 0.25f), V(bh + headL, top + neck - 0.25f), V(bh + headL * 0.9f, top + neck - 0.7f),
                V(bh + headL * 0.4f, top + neck - 0.75f), V(bh * 0.75f, top + neck * 0.1f - 0.2f), V(bh * 0.6f, bot + 0.1f), V(0.2f, bot - 0.1f),
                V(-bh * 0.6f, bot), V(-bh, top - 1.2f), V(-bh, top - 0.4f) });
            float a = Mathf.Sin(ph) * 0.9f, b = Mathf.Sin(ph + Mathf.PI) * 0.9f;
            Leg(l, bh * 0.55f, bot, leg, a, 0.4f); Leg(l, bh * 0.55f - 0.3f, bot, leg, b, 0.6f); Leg(l, -bh * 0.6f, bot, leg, b, -0.4f); Leg(l, -bh * 0.6f + 0.3f, bot, leg, a, -0.6f);
            Vector2 hd = V(bh + headL * 0.55f, top + neck + 0.1f);
            l.Add(new[] { hd, hd + V(0.2f, 0.1f) });                                                       // mata
            l.Add(new[] { V(-bh, top - 0.5f), V(-bh - 1.4f, top - 1.0f + Mathf.Sin(ph) * 0.3f), V(-bh - 2.4f, top - 2.3f + Mathf.Sin(ph) * 0.4f) });   // ekor
            if (lion) for (int i = 0; i < 12; i++)                                                         // surai
            {
                float an = i * Mathf.PI * 2f / 12f; Vector2 c = V(bh * 0.88f, top + neck * 0.6f + 0.1f), d = V(Mathf.Cos(an), Mathf.Sin(an));
                l.Add(new[] { c + d * 1.0f, c + d * (1.8f + (i % 2) * 0.5f) });
            }
            if (bull)
            {
                Vector2 hx = V(bh + headL * 0.3f, top + neck + 0.2f);
                l.Add(new[] { hx, hx + V(0.1f, 0.9f), hx + V(0.9f, 1.4f) }); l.Add(new[] { hx + V(-0.3f, 0f), hx + V(-0.3f, 0.9f), hx + V(0.5f, 1.5f) });
            }
            if (t == "stag")
                for (int s = 0; s < 2; s++)
                {
                    Vector2 hx = V(bh + headL * 0.3f - s * 0.5f, top + neck + 0.3f);
                    l.Add(new[] { hx, hx + V(-0.5f, 1.5f), hx + V(-0.2f, 3.0f) });
                    for (int k = 1; k <= 3; k++) l.Add(new[] { hx + V(-0.3f + k * 0.1f, k * 0.8f), hx + V(0.5f + k * 0.2f, k * 0.8f + 0.7f) });
                }
            if (t == "horse")
            {
                for (int i = 0; i < 6; i++) l.Add(new[] { V(bh * 0.8f - i * 0.25f, top + neck * 0.9f - i * 0.3f), V(bh * 0.8f - i * 0.25f - 0.9f, top + neck * 0.9f - i * 0.3f + 0.1f + Mathf.Sin(ph + i) * 0.2f) });   // surai
                l.Add(new[] { V(bh + headL * 0.45f, top + neck + 0.3f), V(bh + headL * 0.65f, top + neck + 1.6f) });     // tanduk
            }
            if (t == "horse" || lion)                                                                          // sayap
                for (int k = 0; k < 7; k++)
                {
                    float an = Mathf.Deg2Rad * (Mathf.Lerp(105f, 15f, k / 6f) + Mathf.Sin(ph * 0.6f) * 22f), len = 5.5f - Mathf.Abs(k - 2) * 0.35f;
                    Vector2 root = V(bh * 0.3f - k * 0.1f, top + 0.1f), tip = root + V(Mathf.Cos(an), Mathf.Sin(an)) * len;
                    l.Add(new[] { root, root + (tip - root) * 0.55f + V(-0.4f, 0.3f), tip });
                }
            return l;
        }

        static List<Vector2[]> Bird(float flap)
        {
            var l = new List<Vector2[]>();
            var body = new Vector2[16];
            for (int i = 0; i < 16; i++) { float a = i * Mathf.PI * 2f / 15f; body[i] = V(Mathf.Cos(a) * 2.5f, Mathf.Sin(a) * 1.0f); }
            l.Add(body);
            l.Add(new[] { V(2.2f, 0.5f), V(3.0f, 1.2f), V(3.7f, 0.9f), V(4.6f, 0.4f), V(3.5f, 0.3f), V(2.9f, 0.1f) });     // kepala & paruh
            l.Add(new[] { V(3.3f, 0.95f), V(3.45f, 1.05f) });
            for (int k = 0; k < 8; k++)
            {
                float a = Mathf.Deg2Rad * (Mathf.Lerp(85f, 0f, k / 7f) + flap * 38f), len = 8f - Mathf.Abs(k - 3) * 0.55f;
                Vector2 root = V(0.4f - k * 0.1f, 0.6f), tip = root + V(Mathf.Cos(a), Mathf.Sin(a)) * len;
                l.Add(new[] { root, root + (tip - root) * 0.55f + V(-0.5f, 0.3f), tip });
            }
            for (int j = -1; j <= 1; j++) l.Add(new[] { V(-2.3f, 0f), V(-4f, j * 0.35f), V(-5.6f, j * 0.9f - 0.2f) });          // ekor pendek
            l.Add(new[] { V(0.8f, -0.9f), V(1.0f, -1.9f), V(1.5f, -2.2f) }); l.Add(new[] { V(0.2f, -0.9f), V(0.3f, -1.9f), V(0.8f, -2.2f) });   // cakar
            return l;
        }

        static List<Vector2[]> Serpent(float ph)
        {
            var l = new List<Vector2[]>();
            const int n = 22;
            var top = new Vector2[n]; var bot = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float s = i / (float)(n - 1), x = -s * 15f - 0.6f, y = 6.8f * Mathf.Pow(1f - s, 1.5f) + 1f * Mathf.Sin(s * 8f - ph) * s + 0.9f, th = 0.95f * (1f - 0.7f * s) + 0.15f;
                top[i] = V(x, y + th); bot[i] = V(x, y - th);
                if (i % 3 == 1 && i < n - 3) l.Add(new[] { V(x, y + th), V(x - 0.2f, y - th) });          // sisik
            }
            l.Add(top); l.Add(bot);
            float hy = 7.7f;
            l.Add(new[] { V(-0.6f, hy + 0.9f), V(1.2f, hy + 1.1f), V(2.8f, hy + 0.5f), V(2.9f, hy + 0.1f), V(0.2f, hy - 0.2f) });           // rahang atas
            l.Add(new[] { V(0.2f, hy - 0.2f), V(2.4f, hy - 1.1f), V(2.2f, hy - 1.3f), V(-0.5f, hy - 0.9f) });                             // rahang bawah
            l.Add(new[] { V(2.5f, hy + 0.0f), V(2.3f, hy - 0.7f) }); l.Add(new[] { V(1.6f, hy - 0.05f), V(1.5f, hy - 0.6f) });           // taring
            l.Add(new[] { V(2.7f, hy - 0.5f), V(4.0f, hy - 0.4f), V(4.6f, hy - 0.1f) }); l.Add(new[] { V(4.0f, hy - 0.4f), V(4.6f, hy - 0.8f) });   // lidah bercabang
            l.Add(new[] { V(1.0f, hy + 0.55f), V(1.5f, hy + 0.7f) });
            return l;
        }

        static List<Vector2[]> Turtle(float ph)
        {
            var l = new List<Vector2[]>();
            const int n = 18; var dome = new Vector2[n];
            for (int i = 0; i < n; i++) { float a = Mathf.PI * (1f - i / (float)(n - 1)); dome[i] = V(Mathf.Cos(a) * 4.6f, 1.6f + Mathf.Sin(a) * 3.2f); }
            l.Add(dome); l.Add(new[] { V(-4.6f, 1.6f), V(4.6f, 1.6f) });
            for (int k = -2; k <= 2; k++) l.Add(new[] { V(k * 1.5f, 1.6f), V(k * 1.25f, 1.6f + Mathf.Sqrt(Mathf.Max(0f, 1f - (k * 1.5f / 4.6f) * (k * 1.5f / 4.6f))) * 3.0f) });
            l.Add(new[] { V(-3.9f, 3.0f), V(-1.3f, 4.1f), V(1.3f, 4.1f), V(3.9f, 3.0f) });
            l.Add(new[] { V(-2.6f, 4.0f), V(0f, 4.55f), V(2.6f, 4.0f) });
            l.Add(new[] { V(4.2f, 2.3f), V(5.4f, 2.7f), V(6.1f, 2.6f), V(6.3f, 2.2f), V(5.6f, 1.9f), V(4.4f, 1.9f) });                      // kepala
            l.Add(new[] { V(5.7f, 2.45f), V(5.85f, 2.5f) });
            float a1 = Mathf.Sin(ph) * 0.5f, a2 = Mathf.Sin(ph + Mathf.PI) * 0.5f;
            foreach (var p in new[] { new Vector2(3f, a1), new Vector2(2.4f, a2), new Vector2(-2.4f, a1), new Vector2(-3f, a2) })
                l.Add(new[] { V(p.x - 0.5f, 1.6f), V(p.x - 0.6f + Mathf.Sin(p.y) * 0.4f, 0.8f), V(p.x + 0.3f + Mathf.Sin(p.y) * 0.5f, 0f), V(p.x + 0.8f + Mathf.Sin(p.y) * 0.5f, 0f) });
            l.Add(new[] { V(-4.4f, 2.0f), V(-5.6f, 1.5f) });
            return l;
        }

        static List<Vector2[]> Kraken(float ph)
        {
            var l = new List<Vector2[]>();
            var mantle = new Vector2[20];
            for (int i = 0; i < 20; i++) { float a = i * Mathf.PI * 2f / 19f; mantle[i] = V(Mathf.Cos(a) * 2.4f, 8.2f + Mathf.Sin(a) * 3.4f); }
            l.Add(mantle);
            for (int sd = -1; sd <= 1; sd += 2) { Vector2 e = V(1.0f * sd + 0.2f, 6.2f); l.Add(new[] { e + V(-0.4f, 0f), e + V(0f, 0.45f), e + V(0.4f, 0f), e + V(0f, -0.35f), e + V(-0.4f, 0f) }); }
            l.Add(new[] { V(-1.6f, 10f), V(0f, 10.8f), V(1.6f, 10f) });
            for (int i = 0; i < 8; i++)
            {
                float x0 = -2f + i * 0.57f; var t = new Vector2[11];
                bool reach = i == 3 || i == 4;
                for (int k = 0; k <= 10; k++)
                {
                    float u = k / 10f;
                    float x = x0 + (i - 3.5f) * 0.75f * u + Mathf.Sin(ph + i * 0.9f + u * 5f) * 0.8f * u + (reach ? u * u * 8f : 0f);
                    float y = 5.2f - (reach ? 3.8f : 5.0f) * Mathf.Pow(u, 0.9f) + (reach ? Mathf.Sin(u * Mathf.PI) * 2.5f : 0f);
                    t[k] = V(x, Mathf.Max(0.1f, y));
                }
                l.Add(t);
            }
            return l;
        }

        static List<Vector2[]> Shape(string b, float ph)
        {
            switch (b)
            {
                case "wolf": return Wolf(ph);
                case "dragon": return Dragon(ph);
                case "phoenix": return Phoenix(Mathf.Sin(ph));
                case "leviathan": return Leviathan(ph, 0.5f);
                case "bird": return Bird(Mathf.Sin(ph));
                case "serpent": return Serpent(ph);
                case "turtle": return Turtle(ph);
                case "kraken": return Kraken(ph);
                default: return Quad(b, ph);
            }
        }

        static float ScaleOf(string b)
        {
            switch (b) { case "wolf": return 0.9f; case "dragon": return 0.8f; case "phoenix": return 0.8f; case "leviathan": return 0.65f; case "bird": return 0.95f; case "serpent": return 0.9f; case "turtle": return 0.9f; case "kraken": return 0.9f; default: return 1f; }
        }

        static string Motion(string b)
        {
            switch (b) { case "bird": case "dragon": case "phoenix": return "fly"; case "serpent": case "kraken": case "leviathan": return "coil"; case "turtle": return "drop"; default: return "run"; }
        }

        static List<Vector3> Targets(Vector3 P, Vector3 F)
        {
            var found = new List<Monster>();
            foreach (var m in new List<Monster>(Monster.All))
            {
                if (m == null || m.IsDead) continue;
                Vector3 d = m.transform.position - P; d.y = 0f;
                if (d.magnitude < 36f) found.Add(m);
            }
            found.Sort((a, b) => (a.transform.position - P).sqrMagnitude.CompareTo((b.transform.position - P).sqrMagnitude));
            var res = new List<Vector3>();
            for (int i = 0; i < found.Count && res.Count < 4; i++) res.Add(found[i].transform.position);
            if (res.Count == 0) res.Add(P + F * 14f);
            return res;
        }

        static void Impact(string b, Vector3 pos, Vector3 dir, Col c)
        {
            Vector3 up = Vector3.up, right = Vector3.Cross(up, dir);
            CombatFx.HitSpark(pos + up * 1.2f, c.h, 1.7f, 20); CombatFx.ShockRing(pos, 4.5f, c.m, 0.4f);
            CombatFx.Burst(pos + up, c.m, 26, 1.8f, 5f, 0.9f, 0.2f); ThirdPersonCamera.Shake(0.35f, 0.2f);
            switch (Motion(b))
            {
                case "run":
                    for (int i = -1; i <= 1; i++) CombatFx.Beam(pos + right * i * 0.7f + up * 3.2f - dir * 1.4f, pos + right * i * 0.9f + up * 0.4f + dir * 1.4f, i == 0 ? c.h : c.m, 0.4f, 0.3f);
                    CombatFx.Dust(pos, 10); break;
                case "fly":
                    for (int i = -1; i <= 1; i++) CombatFx.Beam(pos + right * i * 0.8f + up * 5f, pos + right * i * 0.8f + up * 0.2f, i == 0 ? c.h : c.m, 0.35f, 0.3f);
                    CombatFx.Flash(pos + up * 2f, c.h, 10f, 3f, 0.25f); break;
                case "coil":
                    MagicFx.Column(pos, 1.5f, 12f, c.m, 0.8f); MagicFx.Debri(pos, 3f, 8, 1f); break;
                default:
                    MagicFx.Debri(pos, 4f, 10, 1.3f); MagicFx.Crack(pos, 4f, 3f); CombatFx.ShockRing(pos, 8f, c.h, 0.5f); break;
            }
        }

        static IEnumerator BeastR(string b, PlayerCombat pc, Col c, float delay)
        {
            if (delay > 0f) yield return W(delay);
            if (pc == null) yield break;
            Vector3 up = Vector3.up;
            Vector3 P = pc.transform.position - up * 0.9f, F = Flat(pc.transform.forward);
            var tg = Targets(P, F);
            float sc = ScaleOf(b), ph = 0f;
            var v = new View(c);
            MagicFx.MagicCircle(P, 5.5f, c.m, 0.9f); CombatFx.Flash(P + up * 2f, c.h, 14f, 3f, 0.3f);
            string mot = Motion(b);
            Vector3 pos, dir = F;
            if (mot == "run")
            {
                pos = P - F * 7f;
                for (float t = 0f; t < 0.3f; t += 0.05f) { v.Set(Shape(b, ph), pos, dir, sc * Mathf.Lerp(0.3f, 1f, t / 0.3f), 0.3f); ph += 0.9f; yield return W(0.05f); }
                foreach (var tp in tg)
                {
                    for (int it = 0; it < 90; it++)
                    {
                        Vector3 d = tp - pos; d.y = 0f;
                        if (d.magnitude < 2.4f) break;
                        dir = d.normalized; pos += dir * 1.7f; pos.y = Mathf.Lerp(pos.y, tp.y, 0.25f) + Mathf.Abs(Mathf.Sin(ph * 0.7f)) * 0.12f;
                        v.Set(Shape(b, ph), pos, dir, sc, 0.3f); ph += 1.1f;
                        if (it % 2 == 0) CombatFx.Dust(pos, 2);
                        yield return W(0.05f);
                    }
                    Impact(b, tp, dir, c); yield return W(0.1f);
                }
                for (int i = 0; i < 6; i++) { pos += dir * 2f; v.Set(Shape(b, ph), pos, dir, sc * (1f - i / 7f), 0.3f); ph += 1.1f; yield return W(0.05f); }
            }
            else if (mot == "fly")
            {
                pos = P + up * 16f - F * 14f;
                foreach (var tp in tg)
                {
                    Vector3 toT = tp - pos; toT.y = 0f; Vector3 d0 = toT.sqrMagnitude < 1f ? F : toT.normalized;
                    Vector3 apex = tp + up * 11f - d0 * 9f;
                    for (int it = 0; it < 60 && (apex - pos).magnitude > 2.5f; it++)
                    {
                        Vector3 mv = apex - pos; pos += mv.normalized * Mathf.Min(2.0f, mv.magnitude); Vector3 hd = mv; hd.y = 0f; if (hd.sqrMagnitude > 0.1f) dir = hd.normalized;
                        v.Set(Shape(b, ph), pos, dir, sc, 0.3f); ph += 0.9f; yield return W(0.05f);
                    }
                    Vector3 st = pos, en = tp + up * 1.2f;
                    for (int i = 1; i <= 6; i++)
                    {
                        pos = Vector3.Lerp(st, en, i / 6f * i / 6f); Vector3 hd = en - st; hd.y = 0f; if (hd.sqrMagnitude > 0.1f) dir = hd.normalized;
                        v.Set(Shape(b, ph), pos, dir, sc, 0.3f); ph += 0.6f; yield return W(0.04f);
                    }
                    Impact(b, tp, dir, c);
                    for (int i = 0; i < 4; i++) { pos += dir * 2.2f + up * 2f; v.Set(Shape(b, ph), pos, dir, sc, 0.3f); ph += 0.9f; yield return W(0.04f); }
                }
                for (int i = 0; i < 6; i++) { pos += dir * 3f + up * 1.5f; v.Set(Shape(b, ph), pos, dir, sc * (1f - i / 7f), 0.3f); ph += 0.9f; yield return W(0.05f); }
            }
            else if (mot == "coil")
            {
                Vector3 cen = Vector3.zero; foreach (var tp in tg) cen += tp; cen /= tg.Count;
                Vector3 toC = cen - P; toC.y = 0f; dir = toC.sqrMagnitude < 1f ? F : toC.normalized;
                Vector3 anchor = cen - dir * 5f;
                for (float t = 0f; t < 0.5f; t += 0.05f) { float k = t / 0.5f; v.Set(Shape(b, ph), anchor - up * (1f - k) * 6f, dir, sc * Mathf.Lerp(0.3f, 1f, k), 0.3f); ph += 0.7f; yield return W(0.05f); }
                MagicFx.Column(anchor, 2.5f, 14f, c.m, 0.8f); CombatFx.ShockRing(anchor, 6f, c.h, 0.5f);
                foreach (var tp in tg)
                {
                    Vector3 d = tp - anchor; d.y = 0f; dir = d.sqrMagnitude < 1f ? dir : d.normalized;
                    Vector3 goal = tp - dir * 3.2f;
                    for (int i = 1; i <= 5; i++) { pos = Vector3.Lerp(anchor, goal, i / 5f); v.Set(Shape(b, ph), pos, dir, sc, 0.3f); ph += 0.7f; yield return W(0.04f); }
                    Impact(b, tp, dir, c);
                    for (int i = 4; i >= 0; i--) { pos = Vector3.Lerp(anchor, goal, i / 5f); v.Set(Shape(b, ph), pos, dir, sc, 0.3f); ph += 0.7f; yield return W(0.04f); }
                }
                for (float t = 0f; t < 0.4f; t += 0.05f) { v.Set(Shape(b, ph), anchor - up * (t / 0.4f) * 6f, dir, sc * (1f - t / 0.5f), 0.3f); ph += 0.7f; yield return W(0.05f); }
            }
            else   // drop: kura-kura raksasa jatuh dari langit
            {
                Vector3 tp0 = tg[0]; Vector3 toT = tp0 - P; toT.y = 0f; dir = toT.sqrMagnitude < 1f ? F : toT.normalized;
                for (int i = 1; i <= 8; i++) { float k = i / 8f; pos = tp0 + up * Mathf.Lerp(28f, 0f, k * k); v.Set(Shape(b, ph), pos, dir, sc, 0.3f); ph += 0.2f; yield return W(0.04f); }
                Impact(b, tp0, dir, c); ThirdPersonCamera.Shake(0.8f, 0.5f);
                for (int i = 0; i < 3; i++) { CombatFx.ShockRing(tp0, 7f + i * 4f, i % 2 == 0 ? c.m : c.h, 0.5f); yield return W(0.09f); }
                for (int i = 1; i < tg.Count; i++) { Impact(b, tg[i], (tg[i] - tp0).normalized, c); yield return W(0.1f); }
                pos = tp0; yield return W(0.4f);
                for (int i = 0; i < 5; i++) { v.Set(Shape(b, ph), tp0 + up * (i * 3f), dir, sc * (1f - i / 6f), 0.3f); yield return W(0.04f); }
            }
            v.Kill();
        }
    }
}
