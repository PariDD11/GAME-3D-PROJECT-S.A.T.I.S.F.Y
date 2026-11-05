using System.Collections;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Tontonan ultimate: lingkaran sihir bertingkat, ring pilar cahaya, pusaran partikel naik, hujan efek bertema elemen
    /// (meteor / kilat / pecahan es / duri batu / pilar suci / hujan pedang), lalu ledakan penutup raksasa. Murni visual.
    /// </summary>
    public static partial class MagicFx
    {
        public static void Prewarm() { CircleTex(); CrackTex(); CombatFx.Soft(); CombatFx.Ring(); GroundMark.Prewarm(); }

        static Vector3 GroundAt(Vector3 p)
        {
            RaycastHit h;
            if (Physics.Raycast(p + Vector3.up * 40f, Vector3.down, out h, 100f, ~0, QueryTriggerInteraction.Ignore)) return h.point;
            return p;
        }

        /// <summary>Dipanggil saat ultimate dirapal. selfCentered = efek berpusat di pemain (buff / nova), bila tidak di titik 9 m di depan.</summary>
        public static void UltimateGrand(Vector3 feet, Vector3 forward, string elem, Color col, bool selfCentered)
        {
            if (FxQuality.Level == 0 || CombatFx.Host.I == null) return;
            CombatFx.Host.I.StartCoroutine(UltRoutine(feet, forward, elem, col, selfCentered));
        }

        static IEnumerator UltRoutine(Vector3 feet, Vector3 fwd, string elem, Color col, bool self)
        {
            float q = FxQuality.Level >= 2 ? 1f : 0.55f;
            Color hot = Color.Lerp(col, Color.white, 0.75f), alt = CombatFx.Shift(col, 0.12f);
            Vector3 f = fwd; f.y = 0f; if (f.sqrMagnitude < 0.01f) f = Vector3.forward; f.Normalize();
            Vector3 center = self ? feet : GroundAt(feet + f * 9f);

            // ---- fase 1: lingkaran sihir bertingkat + ring pilar cahaya
            MagicCircle(feet, 7f, new Color(col.r, col.g, col.b, 0.95f), 2.8f);
            After(0.15f, () => MagicCircle(feet, 10f, new Color(alt.r, alt.g, alt.b, 0.85f), 2.6f));
            if (!self)
            {
                MagicCircle(center, 8.5f, new Color(col.r, col.g, col.b, 0.95f), 3.4f);
                After(0.25f, () => MagicCircle(center, 13f, new Color(hot.r, hot.g, hot.b, 0.8f), 3f));
            }
            int pillars = Mathf.RoundToInt(10 * q);
            for (int i = 0; i < pillars; i++)
            {
                float a = i / (float)pillars * Mathf.PI * 2f;
                Vector3 p = GroundAt(center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (self ? 8f : 9.5f));
                Color pc = i % 2 == 0 ? col : alt;
                After(0.05f * i, () => { LightPillar(p, 14f + Random.value * 6f, 0.45f, pc, 1.0f); CombatFx.ShockRing(p, 2.2f, new Color(pc.r, pc.g, pc.b, 0.8f), 0.4f); });
            }
            CombatFx.Host.I.StartCoroutine(SpiralRise(feet, col, alt, 1.6f));
            ThirdPersonCamera.Shake(0.3f, 0.5f);
            yield return new WaitForSeconds(0.35f);

            // ---- fase 2: hujan efek bertema elemen
            int strikes = Mathf.RoundToInt(30 * q);
            for (int i = 0; i < strikes; i++)
            {
                Vector2 r = Random.insideUnitCircle * (self ? 9f : 11f);
                Vector3 pt = GroundAt(center + new Vector3(r.x, 0f, r.y));
                Strike(elem, pt, col, hot, alt, i);
                if (i % 6 == 0) ThirdPersonCamera.Shake(0.22f, 0.22f);
                yield return new WaitForSeconds(0.08f);
            }
            yield return new WaitForSeconds(0.12f);

            // ---- fase 3: ledakan penutup
            Vector3 c = center;
            LightPillar(c, 70f, 3.4f, col, 1.3f);
            CombatFx.ShockRing(c, 12f, new Color(hot.r, hot.g, hot.b, 0.95f), 0.6f);
            After(0.12f, () => CombatFx.ShockRing(c, 20f, new Color(col.r, col.g, col.b, 0.8f), 0.8f));
            After(0.26f, () => CombatFx.ShockRing(c, 30f, new Color(alt.r, alt.g, alt.b, 0.6f), 1f));
            MagicCircle(c, 15f, new Color(hot.r, hot.g, hot.b, 0.9f), 1.4f);
            CombatFx.Burst(c + Vector3.up * 0.3f, hot, 140, 6f, 11f, 1.6f, 0.2f);
            CombatFx.Burst(c + Vector3.up * 0.3f, alt, 90, 9f, 5f, 1.4f, 0.26f);
            CombatFx.Burst(c + Vector3.up * 3f, col, 70, 3f, 16f, 1.8f, 0.22f);
            CombatFx.Flash(c + Vector3.up * 3f, col, 45f, 6f, 0.9f);
            ThirdPersonCamera.Shake(0.8f, 0.9f);
        }

        // partikel naik berputar seperti pusaran di sekitar pemain
        static IEnumerator SpiralRise(Vector3 feet, Color a, Color b, float dur)
        {
            float t = 0f, ang = 0f;
            while (t < dur)
            {
                ang += 0.9f; t += 0.035f;
                float h = Mathf.Lerp(0.2f, 10f, t / dur);
                float rad = Mathf.Lerp(3.8f, 1.2f, t / dur);
                for (int k = 0; k < 2; k++)
                {
                    float aa = ang + k * Mathf.PI;
                    Vector3 p = feet + new Vector3(Mathf.Cos(aa) * rad, h, Mathf.Sin(aa) * rad);
                    CombatFx.Burst(p, k == 0 ? a : b, 5, 0.15f, 1.2f, 0.7f, 0.16f);
                }
                yield return new WaitForSeconds(0.035f);
            }
        }

        static void Strike(string elem, Vector3 pt, Color col, Color hot, Color alt, int i)
        {
            Vector3 sky = pt + new Vector3(Random.Range(-5f, 5f), 36f, Random.Range(-5f, 5f));
            switch (elem)
            {
                case "fire":
                    CombatFx.Beam(sky, pt, new Color(1f, 0.55f, 0.15f, 0.9f), 0.7f, 0.2f);
                    CombatFx.Beam(sky, pt, new Color(1f, 0.95f, 0.7f, 1f), 0.25f, 0.2f);
                    After(0.2f, () =>
                    {
                        if (i % 5 == 0) Fireball(pt, 3.2f); else Sparks(pt, col, 1.6f);
                        CombatFx.ShockRing(pt, 4f, new Color(hot.r, hot.g, hot.b, 0.8f), 0.4f);
                        CombatFx.Burst(pt + Vector3.up * 0.3f, hot, 22, 2f, 7f, 0.9f, 0.2f);
                    });
                    break;
                case "water":
                    Spike(pt, 3.5f + Random.value * 3f, 0.9f, new Color(0.7f, 0.92f, 1f), 0f, 1f);
                    CombatFx.Burst(pt + Vector3.up * 0.3f, hot, 22, 1.8f, 6f, 1f, 0.18f);
                    CombatFx.ShockRing(pt, 3.4f, new Color(col.r, col.g, col.b, 0.8f), 0.4f);
                    if (i % 4 == 0) CombatFx.Flash(pt + Vector3.up, col, 9f, 2f, 0.25f);
                    break;
                case "wind":
                case "storm":
                    {
                        Vector3 prev = sky;
                        for (int k = 1; k <= 4; k++)
                        {
                            Vector3 np = k == 4 ? pt : Vector3.Lerp(sky, pt, k / 4f) + new Vector3(Random.Range(-2.5f, 2.5f), 0f, Random.Range(-2.5f, 2.5f));
                            CombatFx.Beam(prev, np, new Color(col.r, col.g, col.b, 0.9f), 0.5f, 0.16f);
                            CombatFx.Beam(prev, np, Color.white, 0.18f, 0.16f);
                            prev = np;
                        }
                        CombatFx.Flash(pt + Vector3.up * 2f, col, 14f, 3f, 0.2f);
                        CombatFx.Burst(pt + Vector3.up * 0.3f, hot, 18, 1.6f, 6f, 0.8f, 0.16f);
                        CombatFx.ShockRing(pt, 3.6f, new Color(hot.r, hot.g, hot.b, 0.8f), 0.35f);
                        if (i % 8 == 0) Tornado(pt, 2.2f, 8f, col, 1.8f);
                    }
                    break;
                case "earth":
                    Spike(pt, 4f + Random.value * 3f, 1.0f, RockA, 0f, 1f);
                    RockBurst(pt, 2.5f, 8);
                    Debri(pt, 3f, 8, 1.2f);
                    CombatFx.ShockRing(pt, 4f, new Color(RockA.r, RockA.g, RockA.b, 0.85f), 0.4f);
                    break;
                case "holy":
                    LightPillar(pt, 28f, 0.85f, new Color(1f, 0.9f, 0.5f), 0.7f);
                    CombatFx.Burst(pt + Vector3.up * 0.3f, new Color(1f, 0.95f, 0.65f), 28, 1.6f, 10f, 1.2f, 0.18f);
                    CombatFx.ShockRing(pt, 3.6f, new Color(1f, 0.92f, 0.6f, 0.85f), 0.45f);
                    break;
                default:
                    CombatFx.Beam(sky, pt, new Color(1f, 0.9f, 0.5f, 0.9f), 0.35f, 0.16f);
                    CombatFx.Beam(sky, pt, Color.white, 0.12f, 0.16f);
                    After(0.16f, () =>
                    {
                        CombatFx.SlashArc(pt + Vector3.up * 1.4f, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), 4f, Random.Range(-40f, 40f), col, 0.3f, 1.2f);
                        CombatFx.HitSpark(pt + Vector3.up * 0.5f, hot, 1.6f, 20);
                        CombatFx.ShockRing(pt, 3.6f, new Color(col.r, col.g, col.b, 0.85f), 0.4f);
                    });
                    break;
            }
        }
    }
}
