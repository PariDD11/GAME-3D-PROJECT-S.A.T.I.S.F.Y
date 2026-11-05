using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Lapisan "mewah" untuk semua skill aktif non-Swordsman yang belum punya koreografi sendiri (Archer, 5 Mage, Priest + evolusinya).
    /// Tier 2: kilau besar sesuai jenis (bolt/cone/nova/ground/line/dash/buff/...). Tier 3 (ultimate): satu dari 10 pertunjukan berbeda,
    /// dipilih supaya tiap ultimate dalam satu elemen/job TIDAK memakai bentuk yang sama (meteor, pilar, spiral, tombak langit, kurungan,
    /// sapuan sabit, ledakan berantai, orbital, retakan tanah, halo). Hanya visual & guncangan kamera; damage tetap dari SkillExec.
    /// </summary>
    public static class SkillSpectacle
    {
        struct Pal { public Color m, h, k; public string job; }

        static Pal PalOf(SkillDef s)
        {
            var p = PalBase(s); p.job = s.job; return p;
        }

        static Pal PalBase(SkillDef s)
        {
            var q = SkillLook.Of(s);      // warna mengikuti tema nama skill, bukan sekadar elemen
            return new Pal { m = q.m, h = q.h, k = q.k };
        }

        // ------------------------------------------------------------------ penetapan bentuk ultimate (unik dalam satu elemen)
        static Dictionary<string, int> shapeOf;
        static void BuildShapes()
        {
            shapeOf = new Dictionary<string, int>();
            var groups = new SortedDictionary<string, List<string>>();
            foreach (var s in SkillDB.All)
            {
                if (!s.active || s.tier < 3 || s.job == "Swordsman") continue;
                string e = !string.IsNullOrEmpty(s.elem) ? s.elem : JobInfo.Elem(s.job);
                string key = e + "|" + s.job;
                if (!groups.ContainsKey(key)) groups[key] = new List<string>();
                groups[key].Add(s.id);
            }
            int offset = 0;
            foreach (var kv in groups)
            {
                kv.Value.Sort(string.CompareOrdinal);
                for (int i = 0; i < kv.Value.Count; i++) shapeOf[kv.Value[i]] = (i + offset) % 10;
                offset += 3;        // geser antar kelompok supaya urutan bentuk tiap job berbeda
            }
        }

        // pilihan tema per ultimate (0 meteor, 1 cincin pilar, 2 spiral, 3 tombak langit, 4 kisi, 5 sabit, 6 berantai, 7 orbital, 8 retakan, 9 halo, 10 naga/ular aura, 11 burung aura)
        static readonly Dictionary<string, int> Curated = new Dictionary<string, int>
        {
            {"ar_utl_6",4},{"ar_mob_6",0},{"e30_3_6",3},{"e30_4_6",6},{"e30_5_6",5},{"rg_6",10},{"ms_6",2},{"sh_6",7},
            {"fm_inf_6",0},{"fm_wrd_6",2},{"fm_stp_6",6},{"py_6",8},{"ph_6",9},{"ik_6",5},{"e30_6_6",7},{"e30_7_6",11},{"e30_8_6",4},
            {"em_stn_6",8},{"em_bed_6",4},{"em_flw_6",6},{"ge_6",1},{"gm_6",2},{"tg_6",0},{"e30_9_6",5},{"e30_10_6",9},{"e30_11_6",7},
            {"wm_tid_6",6},{"wm_fst_6",0},{"wm_mst_6",9},{"cr_6",4},{"td_6",10},{"sm_6",2},{"e30_12_6",1},{"e30_13_6",7},{"e30_14_6",3},
            {"wd_gal_6",2},{"wd_zep_6",6},{"wd_rzr_6",5},{"tl_6",7},{"bw_6",3},{"sd_6",9},{"e30_15_6",1},{"e30_16_6",0},{"e30_17_6",4},
            {"pr_rad_6",3},{"pr_mer_6",9},{"pr_aeg_6",4},{"pl_6",5},{"sa_6",1},{"cl_6",2},{"e30_18_6",0},{"e30_19_6",11},{"e30_20_6",7}
        };

        static int ShapeOf(string id)
        {
            int c; if (Curated.TryGetValue(id, out c)) return c;
            if (shapeOf == null) BuildShapes();
            int v; return shapeOf.TryGetValue(id, out v) ? v : 0;
        }

        public static void Colors(SkillDef s, out Color m, out Color h, out Color k) { var p = PalOf(s); m = p.m; h = p.h; k = p.k; }

        /// <summary>Sisa cahaya setelah efek utama: bara/serpihan khas elemen memudar bertahap supaya transisi ke gameplay halus.</summary>
        static IEnumerator Afterglow(Vector3 C, Pal pal, float secs, float R)
        {
            for (float t = 0f; t < secs; t += 0.12f)
            {
                Vector2 r = Random.insideUnitCircle * R; Vector3 q = C + new Vector3(r.x, 0f, r.y);
                CombatFx.Burst(q + Vector3.up * 0.2f, Color.Lerp(pal.m, pal.k, Random.value), 3, 0.5f, 2f, 1.1f, 0.12f);
                if (Random.value < 0.4f) CombatFx.Beam(q, q + Vector3.up * Random.Range(1.5f, 4f), pal.h, 0.05f, 0.5f);
                yield return W(0.12f);
            }
        }

        // ------------------------------------------------------------------ titik masuk
        public static void Play(SkillDef s, PlayerCombat pc)
        {
            if (s == null || pc == null || s.job == "Swordsman" || s.tier < 2 || !s.active) return;
            if (SkillDB.HasSig(s)) return;      // skill ini punya pola visual sendiri (SigFx), dipanggil dari RunGeneric
            var pal = PalOf(s);
            bool self = s.kind == "nova" || s.kind == "buff" || s.kind == "heal" || s.kind == "dash";
            float reach = Mathf.Min(s.range > 0f ? s.range : 22f, 34f);
            Vector3 C = self ? pc.transform.position - Vector3.up * 0.9f : SkillExec.AimGroundFor(pc, reach);
            Vector3 F = pc.transform.forward; F.y = 0f; F.Normalize();
            float sc = Mathf.Clamp(0.7f + s.radius / 10f, 0.8f, 1.8f);
            var host = CombatFx.Host.I;
            host.StartCoroutine(Afterglow(C, pal, s.tier >= 3 ? 3f : 1f, (s.tier >= 3 ? 8f : 3f) * sc));
            if (s.tier >= 3 && SkillSilhouette.Handles(s.id)) { SkillSilhouette.Start(s, pc, C, F, pal.m, pal.h, pal.k); AnimeFx.Play(s, pc, C, F); }
            else
            {
                bool anime = AnimeFx.Play(s, pc, C, F);   // api / air / tanah bergaya anime
                if (s.tier >= 3) host.StartCoroutine(Ultimate(s, pc, C, F, pal, sc));
                else if (!anime) host.StartCoroutine(Flourish(s, pc, C, F, pal, sc));
            }
        }

        // ------------------------------------------------------------------ pembantu
        static Vector3 Up(Vector3 p, float h) { return p + Vector3.up * h; }

        /// <summary>Hantaman khas job: Archer = hujan panah roh, FireMage = pilar api + lidah api, EarthMage = tombak batu + retakan,
        /// WaterMage = geyser + serpihan es, WindMage = puting beliung + sabit angin, Priest = pilar cahaya + halo + bulu. Job lain = kolom elemen.</summary>
        static void Spire(Vector3 p, Pal pal, float r, float h, float life)
        {
            switch (pal.job)
            {
                case "Archer":
                    for (int i = 0; i < 7; i++)
                    {
                        float a = i * 0.9f; Vector3 off = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r * (0.6f + 0.2f * (i % 3));
                        CombatFx.Beam(p + off + new Vector3(0.6f, h, 0.2f), p + off * 0.3f, i % 2 == 0 ? pal.m : pal.k, 0.1f, life * 0.5f);
                        CombatFx.Beam(p + off + new Vector3(0.6f, h, 0.2f), p + off * 0.3f, pal.h, 0.03f, life * 0.4f);
                    }
                    CombatFx.HitSpark(Up(p, 0.5f), pal.h, r * 0.6f, 14); CombatFx.ShockRing(p, r * 2.2f, pal.k, 0.25f);
                    break;
                case "FireMage":
                    MagicFx.Column(p, r, h, pal.m, life); CombatFx.Beam(p, p + Vector3.up * h, pal.h, r * 0.3f, life * 0.5f);
                    for (int i = 0; i < 5; i++) { float a = i * 1.26f; Vector3 q = p + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r * 1.4f; CombatFx.Beam(q, q + new Vector3(0f, h * 0.45f, 0f) + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r * 0.6f, pal.k, 0.4f * r, life * 0.5f); }
                    CombatFx.Burst(Up(p, 1f), pal.m, 12, r * 1.2f, h * 0.4f, 1.1f, 0.14f); CombatFx.ShockRing(p, r * 3f, pal.m, 0.35f);
                    break;
                case "EarthMage":
                    MagicFx.RockBurst(p, r, 8 + (int)(r * 3f)); MagicFx.Crack(p, r * 2.2f, 2.5f);
                    for (int i = 0; i < 6; i++) { float a = i * 1.05f; Vector3 q = p + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r * 1.1f; CombatFx.Beam(q, q + new Vector3(Mathf.Cos(a) * 0.3f, h * 0.35f, Mathf.Sin(a) * 0.3f), pal.k, 0.55f * r, life * 0.7f); CombatFx.Beam(q, q + Vector3.up * h * 0.35f, pal.m, 0.2f * r, life * 0.6f); }
                    CombatFx.Dust(p, 14); CombatFx.ShockRing(p, r * 2.6f, pal.k, 0.4f);
                    break;
                case "WaterMage":
                    MagicFx.Column(p, r * 0.8f, h * 0.8f, pal.m, life); 
                    for (int i = 0; i < 8; i++) { float a = i * 0.785f; Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)); CombatFx.Beam(Up(p, 0.3f), p + d * r * 1.8f + Vector3.up * h * 0.5f, pal.h, 0.05f, life * 0.5f); CombatFx.Burst(p + d * r * 1.8f + Vector3.up * h * 0.5f, pal.m, 3, 0.3f, -1.5f, 0.8f, 0.1f); }
                    CombatFx.ShockRing(p, r * 2.4f, pal.m, 0.5f); CombatFx.ShockRing(p, r * 3.4f, pal.h, 0.7f);
                    break;
                case "WindMage":
                    for (int i = 0; i < 14; i++)
                    {
                        float k = i / 14f, a = i * 0.9f, rad = r * (1.6f - k), y = k * h;
                        Vector3 q = p + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * rad + Vector3.up * y;
                        CombatFx.Beam(q, q + new Vector3(-Mathf.Sin(a), 0.2f, Mathf.Cos(a)) * (1.4f + r), i % 2 == 0 ? pal.m : pal.h, 0.12f, life * 0.6f);
                    }
                    CombatFx.SweepArc(Up(p, 0.5f), Quaternion.Euler(0f, Random.Range(0f, 360f), 15f), r * 2.2f, 25f, 0.2f, -90f, 90f, 0.18f, 0.35f, pal.m, false);
                    CombatFx.ShockRing(p, r * 2.5f, pal.h, 0.3f);
                    break;
                case "Priest":
                    CombatFx.Beam(p, p + Vector3.up * h, pal.h, r * 0.55f, life * 0.8f); CombatFx.Beam(p, p + Vector3.up * h, pal.m, r * 1.1f, life * 0.6f);
                    MagicFx.MagicCircle(p, r * 2.4f, pal.m, life); MagicFx.MagicCircle(Up(p, h * 0.6f), r * 1.2f, pal.h, life * 0.7f);
                    CombatFx.Burst(Up(p, 1.5f), pal.h, 10, r, h * 0.3f, 1.3f, 0.12f); CombatFx.ShockRing(p, r * 3f, pal.h, 0.5f);
                    break;
                default:
                    MagicFx.Column(p, r, h, pal.m, life);
                    CombatFx.Beam(p, p + Vector3.up * h, pal.h, r * 0.25f, life * 0.6f);
                    CombatFx.ShockRing(p, r * 3f, pal.m, 0.35f);
                    break;
            }
            CombatFx.Flash(p + Vector3.up * 1f, pal.h, 8f, 2f, 0.2f);
        }

        // ------------------------------------------------------------------ makhluk aura (naga / feniks) yang meluncur lurus & menghancurkan
        /// <summary>Aura naga/burung terbentuk dari tebasan dan terbang lurus sejauh range, merusak tanah di jalurnya. Dipakai Dragon King Slash dan ultimate bertema makhluk.</summary>
        public static void PlayBeast(Vector3 origin, Vector3 fwd, float range, Color main, Color hot, Color dark, bool wings, float width, string job)
        {
            var pal = new Pal { m = main, h = hot, k = dark, job = job };
            CombatFx.Host.I.StartCoroutine(Beast(origin, fwd, range, pal, wings, width));
        }

        static IEnumerator Beast(Vector3 o, Vector3 f, float range, Pal pal, bool wings, float width)
        {
            f.y = 0f; f.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, f);
            const int N = 12; const float dt = 0.05f;
            float speed = 36f, t = 0f, nextImpact = 0f; int side = 1;
            var seg = new Vector3[N];
            while (speed * t < range)
            {
                float dist = speed * t;
                Vector3 head = o + f * dist;
                for (int i = 0; i < N; i++)
                {
                    float sp = i * 1.35f * width;
                    seg[i] = head - f * sp + right * (i == 0 ? 0f : Mathf.Sin(t * 9f - i * 0.55f) * 1.3f * width) + Vector3.up * (1.6f + Mathf.Cos(t * 7f - i * 0.5f) * 0.6f * width);
                }
                for (int i = 0; i < N - 1; i++)
                {
                    float k = 1f - i / (float)N;
                    CombatFx.Beam(seg[i], seg[i + 1], pal.m, 2.4f * width * k + 0.2f, 0.22f);
                    CombatFx.Beam(seg[i], seg[i + 1], pal.h, 0.7f * width * k + 0.05f, 0.18f);
                    if (i % 2 == 0) CombatFx.Beam(seg[i], seg[i] + Vector3.up * 1.2f * width * k + right * 0.3f, pal.k, 0.3f * width * k, 0.2f);
                }
                Vector3 jawU = head + f * 2.2f * width + Vector3.up * (2.3f + 0.5f * Mathf.Sin(t * 12f)), jawL = head + f * 1.9f * width + Vector3.up * 1.0f;
                Vector3 hb = seg[0];
                CombatFx.Beam(hb, jawU, pal.m, 1.5f * width, 0.22f); CombatFx.Beam(hb, jawL, pal.k, 1.2f * width, 0.22f); CombatFx.Beam(hb, jawU, pal.h, 0.4f * width, 0.18f);
                CombatFx.Beam(hb, hb - f * 1.8f * width + Vector3.up * 1.6f * width + right * 0.9f * width, pal.h, 0.25f, 0.2f);
                CombatFx.Beam(hb, hb - f * 1.8f * width + Vector3.up * 1.6f * width - right * 0.9f * width, pal.h, 0.25f, 0.2f);
                CombatFx.Flash(hb + Vector3.up * 0.4f, pal.h, 10f, 3f, 0.12f);
                if (wings)
                {
                    float flap = Mathf.Sin(t * 16f);
                    for (int sd = -1; sd <= 1; sd += 2)
                        for (int fth = 0; fth < 4; fth++)
                        {
                            Vector3 root = seg[2];
                            Vector3 tip = root + right * sd * (4f + fth * 1.6f) * width + Vector3.up * (2.2f + flap * 2.4f - fth * 0.4f) - f * (2f + fth * 1.1f);
                            CombatFx.Beam(root, tip, fth % 2 == 0 ? pal.m : pal.h, 0.5f * width, 0.22f);
                        }
                }
                // jalur hancur: tanah retak, serpihan, ledakan job di kiri/kanan kepala
                Vector3 g = o + f * dist;
                CombatFx.Dust(g, 6); CombatFx.Burst(Up(g, 0.4f), pal.m, 6, 2f * width, 2.5f, 0.7f, 0.14f);
                if (t >= nextImpact)
                {
                    nextImpact = t + 0.1f; side = -side;
                    Spire(g + right * side * Random.Range(1.5f, 3.6f) * width, pal, 1.2f * width, 7f, 0.6f);
                    MagicFx.Crack(g, 2.4f * width, 2.5f); MagicFx.Debri(g, 2.6f * width, 8, 1f);
                    CombatFx.ShockRing(g, 5f * width, pal.m, 0.3f);
                    ThirdPersonCamera.Shake(0.22f, 0.12f);
                }
                yield return new WaitForSeconds(dt);
                t += dt;
            }
            Vector3 fin = o + f * range;
            Spire(fin, pal, 3.5f * width, 22f, 1.2f);
            CombatFx.ShockRing(fin, 12f * width, pal.h, 0.8f); CombatFx.Flash(Up(fin, 2f), pal.h, 30f, 6f, 0.5f);
            CombatFx.Burst(Up(fin, 2f), pal.m, 40, 5f * width, 8f, 1.4f, 0.2f);
            ThirdPersonCamera.Shake(0.6f, 0.5f);
        }

        /// <summary>Jejak khas job: Archer = jarum ganda tipis, Fire = pita api + bara, Earth = garis batu + debu, Water = gelombang sinus, Wind = goresan melengkung, Priest = untaian cahaya.</summary>
        static void TrailFx(Vector3 a, Vector3 b, Pal pal, int i)
        {
            Vector3 d = (b - a), r = Vector3.Cross(Vector3.up, d.normalized);
            switch (pal.job)
            {
                case "Archer": CombatFx.Beam(a + r * 0.12f, b + r * 0.12f, pal.m, 0.05f, 0.25f); CombatFx.Beam(a - r * 0.12f, b - r * 0.12f, pal.h, 0.03f, 0.25f); break;
                case "FireMage": CombatFx.Beam(a, b, pal.m, 0.8f, 0.4f); CombatFx.Beam(a, b, pal.h, 0.2f, 0.25f); CombatFx.Burst(b, pal.k, 3, 0.4f, 1.2f, 0.7f, 0.1f); break;
                case "EarthMage": CombatFx.Beam(a, b, pal.k, 0.9f, 0.35f); CombatFx.Beam(a, b, pal.m, 0.25f, 0.3f); CombatFx.Dust(b - Vector3.up * 0.5f, 4); break;
                case "WaterMage": CombatFx.Beam(a, a + d * 0.5f + r * 0.5f * Mathf.Sin(i), pal.m, 0.4f, 0.35f); CombatFx.Beam(a + d * 0.5f + r * 0.5f * Mathf.Sin(i), b, pal.h, 0.3f, 0.35f); CombatFx.Burst(b, pal.m, 2, 0.3f, -1f, 0.6f, 0.08f); break;
                case "WindMage": CombatFx.Beam(a + r * 0.4f, b - r * 0.4f, pal.m, 0.15f, 0.25f); CombatFx.Beam(a - r * 0.4f, b + r * 0.4f, pal.h, 0.1f, 0.25f); break;
                case "Priest": CombatFx.Beam(a, b, pal.h, 0.3f, 0.4f); CombatFx.Flash(b, pal.m, 4f, 1.4f, 0.15f); CombatFx.Burst(b, pal.h, 3, 0.4f, 1.5f, 0.8f, 0.08f); break;
                default: CombatFx.Beam(a, b, pal.m, 0.6f, 0.3f); CombatFx.Beam(a, b, pal.h, 0.15f, 0.2f); break;
            }
        }

        static IEnumerator W(float t) { yield return new WaitForSeconds(t); }

        static IEnumerator Flourish(SkillDef s, PlayerCombat pc, Vector3 C, Vector3 F, Pal pal, float sc)
        {
            Vector3 P = pc.transform.position + Vector3.up * 1.2f;
            ThirdPersonCamera.Shake(0.16f, 0.2f);
            switch (s.kind)
            {
                case "bolt":
                case "multi":
                    {
                        CombatFx.Flash(P + F, pal.h, 8f, 2.4f, 0.2f);
                        CombatFx.ShockRing(P + F, 2.2f, pal.m, 0.3f);
                        yield return W(0.12f);
                        for (int i = 0; i < 5; i++) { Vector3 a = Vector3.Lerp(P, Up(C, 1f), i / 5f), b = Vector3.Lerp(P, Up(C, 1f), (i + 1) / 5f); TrailFx(a, b, pal, i); yield return W(0.02f); }
                        Spire(C, pal, 1.2f * sc, 6f * sc, 0.6f); CombatFx.Burst(Up(C, 1f), pal.h, 14, 1.5f * sc, 3f, 0.8f, 0.14f);
                        break;
                    }
                case "cone":
                    for (int i = -3; i <= 3; i++)
                    {
                        Vector3 d = Quaternion.AngleAxis(i * 12f, Vector3.up) * F;
                        CombatFx.Beam(P, P + d * 7f * sc, pal.m, 0.35f, 0.35f); CombatFx.Beam(P, P + d * 6f * sc, pal.h, 0.08f, 0.25f);
                        CombatFx.Burst(P + d * 6f * sc, pal.m, 5, 0.8f, 1.5f, 0.6f, 0.12f);
                        yield return W(0.025f);
                    }
                    CombatFx.ShockRing(P + F * 3f, 5f * sc, pal.m, 0.4f);
                    break;
                case "nova":
                    for (int i = 0; i < 3; i++) { CombatFx.ShockRing(C + Vector3.up * 0.2f, (4f + 2f * i) * sc, i == 1 ? pal.h : pal.m, 0.4f + 0.1f * i); MagicFx.MagicCircle(C, (2.5f + 1.5f * i) * sc, pal.m, 0.6f); yield return W(0.1f); }
                    for (int i = 0; i < 12; i++) { float a = i * Mathf.PI / 6f; Vector3 q = C + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 5f * sc; CombatFx.Beam(C + Vector3.up * 0.5f, q + Vector3.up * 0.5f, pal.m, 0.2f, 0.3f); CombatFx.Burst(q, pal.h, 3, 0.4f, 1.5f, 0.5f, 0.1f); }
                    break;
                case "ground":
                case "summon":
                case "trap":
                    {
                        MagicFx.MagicCircle(C, 3f * sc, pal.m, 0.7f);
                        yield return W(0.15f);
                        Spire(C, pal, 1.4f * sc, 9f * sc, 0.8f);
                        for (int i = 0; i < 6; i++) { float a = i * Mathf.PI / 3f; Vector3 q = C + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 3.2f * sc; Spire(q, pal, 0.6f * sc, 4f * sc, 0.5f); yield return W(0.03f); }
                        MagicFx.Debri(C, 3f * sc, 10, 1f);
                        ThirdPersonCamera.Shake(0.28f, 0.3f);
                        break;
                    }
                case "line":
                    {
                        Vector3 e = P + F * Mathf.Min(s.range > 0f ? s.range : 24f, 30f);
                        for (int i = 1; i <= 8; i++) { Vector3 a = Vector3.Lerp(P, e, (i - 1) / 8f), b = Vector3.Lerp(P, e, i / 8f); TrailFx(a, b, pal, i); Spire(Vector3.Lerp(pc.transform.position, e, i / 8f) - Vector3.up * 0.9f, pal, 0.8f, 5f, 0.4f); yield return W(0.03f); }
                        break;
                    }
                case "dash":
                    for (int i = 0; i < 8; i++) { Vector3 p = pc.transform.position + Vector3.up * (0.4f + 0.2f * i); CombatFx.Beam(p - F * 4f, p, pal.m, 0.15f, 0.35f); yield return W(0.02f); }
                    CombatFx.ShockRing(pc.transform.position - Vector3.up * 0.8f, 3f, pal.m, 0.3f);
                    break;
                default:    // buff / heal / lainnya: spiral naik + kolom
                    for (int i = 0; i < 20; i++)
                    {
                        float a = i * 0.7f; Vector3 q = pc.transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 1.4f + Vector3.up * (-0.8f + i * 0.14f);
                        CombatFx.Burst(q, pal.m, 2, 0.2f, 0.8f, 0.5f, 0.1f); CombatFx.Flash(q, pal.h, 3f, 0.8f, 0.1f); yield return W(0.025f);
                    }
                    MagicFx.Column(pc.transform.position - Vector3.up * 0.9f, 1.4f, 7f, pal.m, 0.7f); CombatFx.ShockRing(pc.transform.position - Vector3.up * 0.8f, 3.5f, pal.h, 0.4f);
                    break;
            }
        }

        // ------------------------------------------------------------------ ULTIMATE: 10 bentuk
        static IEnumerator Ultimate(SkillDef s, PlayerCombat pc, Vector3 C, Vector3 F, Pal pal, float sc)
        {
            int shape = ShapeOf(s.id);
            Vector3 P = pc.transform.position;
            float R = 8f * Mathf.Max(1f, sc);
            ThirdPersonCamera.Shake(0.35f, 0.5f);
            MagicFx.MagicCircle(C, R * 0.7f, pal.m, 1.2f);
            yield return W(0.35f);
            switch (shape)
            {
                case 0: // hujan meteor
                    for (int i = 0; i < 16; i++)
                    {
                        Vector2 r = Random.insideUnitCircle * R; Vector3 t = C + new Vector3(r.x, 0f, r.y);
                        Vector3 from = t + new Vector3(Random.Range(-10f, 10f), 32f, Random.Range(-10f, 10f));
                        CombatFx.Beam(from, t, pal.m, 1.3f, 0.35f); CombatFx.Beam(from, t, pal.h, 0.35f, 0.3f);
                        CombatFx.Host.I.StartCoroutine(Late(0.18f, () => { Spire(t, pal, 1.3f, 8f, 0.6f); CombatFx.Burst(Up(t, 0.5f), pal.h, 10, 1.6f, 3f, 0.8f, 0.16f); MagicFx.Debri(t, 2f, 6, 1f); ThirdPersonCamera.Shake(0.22f, 0.2f); }));
                        yield return W(0.085f);
                    }
                    yield return W(0.3f); Spire(C, pal, 3.5f, 22f, 1.2f); CombatFx.ShockRing(C, R * 1.4f, pal.m, 0.7f); ThirdPersonCamera.Shake(0.6f, 0.6f);
                    break;
                case 1: // cincin pilar bertingkat
                    for (int ring = 1; ring <= 3; ring++)
                    {
                        int n = 4 + ring * 4; float rr = ring * R * 0.33f;
                        for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2f / n + ring; Spire(C + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * rr, pal, 0.9f, 10f + ring * 3f, 0.8f); yield return W(0.03f); }
                        ThirdPersonCamera.Shake(0.3f, 0.25f); yield return W(0.12f);
                    }
                    Spire(C, pal, 4f, 30f, 1.4f); CombatFx.ShockRing(C, R * 1.5f, pal.h, 0.8f); ThirdPersonCamera.Shake(0.65f, 0.6f);
                    break;
                case 2: // pusaran spiral naik
                    for (int i = 0; i < 60; i++)
                    {
                        float k = i / 60f, a = i * 0.55f, rad = Mathf.Lerp(R * 0.9f, 1f, k), h = k * 18f;
                        Vector3 p1 = C + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * rad + Vector3.up * h, p2 = C + new Vector3(Mathf.Cos(a + Mathf.PI), 0f, Mathf.Sin(a + Mathf.PI)) * rad + Vector3.up * h;
                        CombatFx.Beam(p1, Up(p1, 1.5f), pal.m, 0.5f, 0.5f); CombatFx.Beam(p2, Up(p2, 1.5f), pal.k, 0.5f, 0.5f);
                        CombatFx.Burst(p1, pal.h, 2, 0.4f, 0.6f, 0.5f, 0.1f);
                        if (i % 10 == 0) CombatFx.ShockRing(C, rad, pal.m, 0.4f);
                        yield return W(0.025f);
                    }
                    Spire(C, pal, 3f, 26f, 1.2f); ThirdPersonCamera.Shake(0.55f, 0.55f);
                    break;
                case 3: // tombak dari langit
                    for (int i = 0; i < 3; i++) { MagicFx.MagicCircle(C, R * (0.4f + 0.2f * i), pal.m, 0.5f); yield return W(0.12f); }
                    for (int w = 0; w < 4; w++) { CombatFx.Beam(C + Vector3.up * 45f, C, w % 2 == 0 ? pal.m : pal.h, 5f - w * 1.1f, 0.9f - w * 0.1f); }
                    CombatFx.Flash(Up(C, 2f), pal.h, 30f, 6f, 0.6f); Spire(C, pal, 3.5f, 40f, 1.2f);
                    for (int i = 0; i < 5; i++) { CombatFx.ShockRing(C + Vector3.up * 0.2f, R * (0.4f + 0.3f * i), i % 2 == 0 ? pal.m : pal.h, 0.5f); ThirdPersonCamera.Shake(0.5f - i * 0.07f, 0.2f); yield return W(0.1f); }
                    MagicFx.Debri(C, R * 0.6f, 24, 1.5f); MagicFx.Crack(C, R * 0.8f, 3f);
                    break;
                case 4: // kurungan kisi lalu runtuh
                    {
                        int lat = 5, lon = 10; float dome = R * 0.8f;
                        for (int a = 0; a < lat; a++)
                        {
                            for (int b = 0; b < lon; b++)
                            {
                                float p0 = (a + 1) / (float)lat * Mathf.PI * 0.5f, p1 = (a + 2) / (float)lat * Mathf.PI * 0.5f, t0 = b * Mathf.PI * 2f / lon, t1 = (b + 1) * Mathf.PI * 2f / lon;
                                Vector3 q0 = C + new Vector3(Mathf.Sin(p0) * Mathf.Cos(t0), Mathf.Cos(p0), Mathf.Sin(p0) * Mathf.Sin(t0)) * dome;
                                Vector3 q1 = C + new Vector3(Mathf.Sin(p0) * Mathf.Cos(t1), Mathf.Cos(p0), Mathf.Sin(p0) * Mathf.Sin(t1)) * dome;
                                Vector3 q2 = C + new Vector3(Mathf.Sin(p1) * Mathf.Cos(t0), Mathf.Cos(p1), Mathf.Sin(p1) * Mathf.Sin(t0)) * dome;
                                CombatFx.Beam(q0, q1, pal.m, 0.2f, 0.9f); CombatFx.Beam(q0, q2, pal.h, 0.15f, 0.9f);
                            }
                            yield return W(0.09f);
                        }
                        CombatFx.Beam(C + Vector3.up * dome, C, pal.h, 0.5f, 0.6f);
                        yield return W(0.3f);
                        CombatFx.Burst(Up(C, 1f), pal.h, 40, dome * 0.6f, 6f, 1.2f, 0.2f); Spire(C, pal, 3f, 24f, 1f); CombatFx.ShockRing(C, R * 1.6f, pal.m, 0.7f); ThirdPersonCamera.Shake(0.6f, 0.55f);
                        break;
                    }
                case 5: // sapuan sabit berlapis
                    for (int i = 0; i < 6; i++)
                    {
                        CombatFx.SweepArc(Up(C, 0.4f + 0.5f * i), Quaternion.Euler(0f, i * 60f, (i % 2 == 0 ? 18f : -18f)), R * (0.6f + 0.1f * i), 28f, 0.3f, -110f, 110f, 0.2f, 0.4f, i % 2 == 0 ? pal.m : pal.h, true);
                        ThirdPersonCamera.Shake(0.3f, 0.15f); yield return W(0.12f);
                    }
                    CombatFx.ShockRing(C, R * 1.4f, pal.m, 0.6f); Spire(C, pal, 3f, 20f, 1f); ThirdPersonCamera.Shake(0.55f, 0.5f);
                    break;
                case 6: // ledakan berantai sepanjang garis
                    {
                        Vector3 e = P + F * Mathf.Min(34f, Vector3.Distance(P, C) + 10f);
                        for (int i = 1; i <= 14; i++)
                        {
                            Vector3 p = Vector3.Lerp(P, e, i / 14f); float z = 1f + i * 0.1f;
                            Spire(p, pal, 1.2f * z, 8f * z, 0.7f); CombatFx.Burst(Up(p, 1f), pal.h, 10, 2f, 3f, 0.8f, 0.16f);
                            if (i % 3 == 0) ThirdPersonCamera.Shake(0.3f, 0.15f);
                            yield return W(0.07f);
                        }
                        Spire(e, pal, 4f, 28f, 1.3f); CombatFx.ShockRing(e, R * 1.3f, pal.h, 0.7f); ThirdPersonCamera.Shake(0.6f, 0.55f);
                        break;
                    }
                case 7: // bola orbital memusat
                    {
                        int n = 8;
                        for (int step = 0; step < 24; step++)
                        {
                            float k = step / 24f, rad = Mathf.Lerp(R * 0.9f, 0.5f, k * k), h = Mathf.Lerp(5f, 1.2f, k);
                            for (int i = 0; i < n; i++)
                            {
                                float a = i * Mathf.PI * 2f / n + step * 0.35f;
                                Vector3 o = C + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * rad + Vector3.up * h;
                                CombatFx.Flash(o, pal.h, 6f, 2f, 0.1f); CombatFx.Burst(o, pal.m, 2, 0.3f, 0.4f, 0.4f, 0.12f);
                                if (step % 4 == 0) CombatFx.Beam(o, Up(C, 1.2f), pal.m, 0.15f, 0.2f);
                            }
                            yield return W(0.04f);
                        }
                        CombatFx.Flash(Up(C, 1.5f), pal.h, 40f, 8f, 0.7f); Spire(C, pal, 4f, 30f, 1.3f); CombatFx.ShockRing(C, R * 1.6f, pal.m, 0.8f); ThirdPersonCamera.Shake(0.7f, 0.6f);
                        break;
                    }
                case 8: // retakan menjalar + letusan
                    {
                        MagicFx.Crack(C, R * 1.1f, 4f);
                        for (int step = 1; step <= 10; step++)
                        {
                            for (int i = 0; i < 8; i++)
                            {
                                float a = i * Mathf.PI / 4f + 0.2f; Vector3 p = C + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (R * step / 9f);
                                CombatFx.Beam(p, Up(p, 0.3f), pal.h, 0.3f, 0.3f);
                                if (step % 2 == 0) Spire(p, pal, 0.7f, 6f + step, 0.6f);
                            }
                            ThirdPersonCamera.Shake(0.25f, 0.1f); yield return W(0.07f);
                        }
                        Spire(C, pal, 4f, 32f, 1.4f); MagicFx.RockBurst(C, R * 0.5f, 22); ThirdPersonCamera.Shake(0.65f, 0.6f);
                        break;
                    }
                case 10: // naga aura meluncur lurus
                case 11: // burung aura meluncur lurus
                    PlayBeast(P - Vector3.up * 0.9f, F, Mathf.Min(34f, Vector3.Distance(P, C) + 14f), pal.m, pal.h, pal.k, shape == 11, 1.2f * Mathf.Max(1f, sc * 0.8f), pal.job);
                    yield return W(1.2f);
                    break;
                default: // halo + sinar
                    {
                        Vector3 top = C + Vector3.up * 14f;
                        for (int step = 1; step <= 12; step++)
                        {
                            float rad = R * step / 12f;
                            for (int i = 0; i < 24; i++) { float a = i * Mathf.PI / 12f; Vector3 q = top + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * rad; CombatFx.Flash(q, pal.h, 3f, 1.2f, 0.08f); }
                            if (step % 3 == 0) { MagicFx.MagicCircle(Up(C, 0.1f), rad, pal.m, 0.5f); }
                            yield return W(0.05f);
                        }
                        for (int i = 0; i < 20; i++)
                        {
                            float a = i * Mathf.PI / 10f; Vector3 q = top + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * R; Vector3 g = C + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * R * 0.45f;
                            CombatFx.Beam(q, g, i % 2 == 0 ? pal.m : pal.h, 0.8f, 0.6f); CombatFx.Burst(g, pal.h, 4, 0.8f, 2f, 0.7f, 0.12f); yield return W(0.025f);
                        }
                        CombatFx.Flash(Up(C, 3f), pal.h, 40f, 8f, 0.8f); Spire(C, pal, 4.5f, 34f, 1.4f); CombatFx.ShockRing(C, R * 1.7f, pal.m, 0.9f); ThirdPersonCamera.Shake(0.65f, 0.6f);
                        break;
                    }
            }
        }

        static IEnumerator Late(float t, System.Action a) { yield return new WaitForSeconds(t); if (a != null) a(); }
    }
}
