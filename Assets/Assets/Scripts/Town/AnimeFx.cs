using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Efek api, air, dan tanah bergaya anime (Katon / Suiton / Doton, Fire Force, Avatar).
    /// Semuanya ribbon LineRenderer yang dianimasikan per frame (heliks berputar, ujung runcing, inti putih-panas),
    /// ditambah pilar batu padat, gelombang air berlapis, pusaran spiral, dan lidah api yang meledak dari tanah.
    /// Dipanggil dari SkillSpectacle.Play untuk skill ber-elemen fire / water / earth.
    /// </summary>
    public static class AnimeFx
    {
        public struct Pal { public Color m, h, k, w; public Pal(Color m, Color h, Color k, Color w) { this.m = m; this.h = h; this.k = k; this.w = w; } }

        // m = badan, h = inti panas, k = lapisan gelap/luar, w = putih (busa / percik)
        public static readonly Pal Fire = new Pal(new Color(1.0f, 0.46f, 0.08f, 0.95f), new Color(1.0f, 0.93f, 0.55f, 1f), new Color(0.85f, 0.1f, 0.04f, 0.85f), new Color(1f, 1f, 0.9f, 1f));
        public static readonly Pal Water = new Pal(new Color(0.12f, 0.5f, 1.0f, 0.92f), new Color(0.55f, 0.95f, 1.0f, 1f), new Color(0.04f, 0.2f, 0.75f, 0.85f), new Color(0.95f, 1f, 1f, 1f));
        public static readonly Pal Ice = new Pal(new Color(0.55f, 0.88f, 1.0f, 0.95f), new Color(0.9f, 1f, 1f, 1f), new Color(0.25f, 0.55f, 0.95f, 0.85f), new Color(1f, 1f, 1f, 1f));
        public static readonly Pal Earth = new Pal(new Color(0.62f, 0.46f, 0.28f, 1f), new Color(0.95f, 0.8f, 0.5f, 1f), new Color(0.32f, 0.24f, 0.17f, 1f), new Color(0.85f, 0.78f, 0.62f, 1f));

        static readonly AnimationCurve JetCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.3f, 0.85f), new Keyframe(0.8f, 1f), new Keyframe(1f, 0.3f));
        static readonly AnimationCurve TongueCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.5f, 0.72f), new Keyframe(1f, 0f));
        static readonly AnimationCurve FlatCurve = new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0.6f));

        static IEnumerator W(float t) { yield return new WaitForSeconds(t); }
        static float Ease(float x) { x = Mathf.Clamp01(x); return 1f - (1f - x) * (1f - x) * (1f - x); }
        static MonoBehaviour Host { get { return CombatFx.Host.I; } }

        // ------------------------------------------------------------------ penampung ribbon
        sealed class Rig
        {
            public readonly GameObject root = new GameObject("FxAnime");
            readonly List<Material> mats = new List<Material>();
            public LineRenderer[] Group(int n, Color c, AnimationCurve curve, int pts, bool loop = false)
            {
                var mat = CombatFx.Mat(null, c); mats.Add(mat);
                var arr = new LineRenderer[n];
                for (int i = 0; i < n; i++)
                {
                    var go = new GameObject("r"); go.transform.SetParent(root.transform, false);
                    var lr = go.AddComponent<LineRenderer>();
                    lr.useWorldSpace = true; lr.sharedMaterial = mat; lr.positionCount = pts; lr.loop = loop;
                    lr.numCapVertices = 3; lr.numCornerVertices = 2; lr.widthCurve = curve;
                    lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    arr[i] = lr;
                }
                return arr;
            }
            public void Kill()
            {
                for (int i = 0; i < mats.Count; i++) if (mats[i] != null) Object.Destroy(mats[i]);
                if (root != null) Object.Destroy(root);
            }
        }

        static void Wid(LineRenderer[] g, float w) { for (int i = 0; i < g.Length; i++) if (g[i] != null) g[i].widthMultiplier = w; }

        // ------------------------------------------------------------------ API
        /// <summary>Semburan api heliks yang menjulur ke depan (Katon): lapisan merah gelap, oranye, inti kuning-putih, bara beterbangan.</summary>
        public static void FlameJet(Vector3 o, Vector3 dir, float len, float width, float secs, Pal p, int n = 6) { Host.StartCoroutine(FlameJetR(o, dir, len, width, secs, p, n)); }

        static IEnumerator FlameJetR(Vector3 o, Vector3 dir, float len, float width, float secs, Pal p, int n)
        {
            dir.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, dir); if (right.sqrMagnitude < 1e-4f) right = Vector3.right; right.Normalize();
            Vector3 up2 = Vector3.Cross(dir, right).normalized;
            var rig = new Rig();
            var outer = rig.Group(n, p.k, JetCurve, 30);
            var mid = rig.Group(n, p.m, JetCurve, 30);
            var core = rig.Group(Mathf.Max(2, n / 2), p.h, JetCurve, 30);
            float t = 0f, emb = 0f;
            while (t < secs)
            {
                t += Time.deltaTime; float k = t / secs;
                float head = len * Ease(t / (secs * 0.42f));
                float tail = len * Mathf.Clamp01((k - 0.5f) / 0.5f); if (tail > head - 0.3f) tail = Mathf.Max(0f, head - 0.3f);
                float fade = 1f - Mathf.Clamp01((k - 0.78f) / 0.22f);
                for (int g = 0; g < 3; g++)
                {
                    var grp = g == 0 ? outer : (g == 1 ? mid : core);
                    float rf = g == 0 ? 1f : (g == 1 ? 0.62f : 0.25f);
                    for (int i = 0; i < grp.Length; i++)
                    {
                        float ph = i * Mathf.PI * 2f / grp.Length + g * 0.7f;
                        for (int j = 0; j < 30; j++)
                        {
                            float u = j / 29f, s = Mathf.Lerp(tail, head, u);
                            float r = width * (0.12f + 0.88f * (s / len)) * rf;
                            float ang = ph + s * 3.1f - t * 17f;
                            float wob = (Mathf.PerlinNoise(s * 1.6f + i * 3f, t * 7f) - 0.5f) * width * 0.7f * rf;
                            Vector3 q = o + dir * s + right * (Mathf.Cos(ang) * r + wob) + up2 * Mathf.Sin(ang) * r + Vector3.up * (s / len) * width * 0.55f;
                            grp[i].SetPosition(j, q);
                        }
                    }
                    Wid(grp, width * (g == 0 ? 0.55f : g == 1 ? 0.4f : 0.22f) * fade);
                }
                emb += Time.deltaTime;
                if (emb > 0.045f)
                {
                    emb = 0f; Vector3 hp = o + dir * head;
                    CombatFx.Burst(hp, p.m, 3, width * 0.6f, 2.2f, 0.8f, 0.12f);
                    if (Random.value < 0.4f) CombatFx.Burst(o + dir * Random.Range(0f, head), p.w, 1, width * 0.3f, 1.6f, 0.6f, 0.07f);
                }
                yield return null;
            }
            rig.Kill();
        }

        /// <summary>Pusaran api / air bertingkat: ribbon heliks naik makin lebar ke atas, cincin menjulang, partikel berputar.</summary>
        public static void Vortex(Vector3 c, Transform follow, float r, float h, float secs, Pal p, bool water, int n = 6, float turns = 2.2f) { Host.StartCoroutine(VortexR(c, follow, r, h, secs, p, water, n, turns)); }

        static IEnumerator VortexR(Vector3 c0, Transform follow, float r, float h, float secs, Pal p, bool water, int n, float turns)
        {
            var rig = new Rig();
            const int P = 26;
            var outer = rig.Group(n, p.k, FlatCurve, P);
            var mid = rig.Group(n, p.m, FlatCurve, P);
            var core = rig.Group(Mathf.Max(2, n / 2), p.h, FlatCurve, P);
            var rings = rig.Group(3, water ? p.w : p.h, FlatCurve, 40, true);
            float t = 0f, emb = 0f;
            while (t < secs)
            {
                t += Time.deltaTime; float k = t / secs;
                Vector3 c = follow != null ? follow.position - Vector3.up * 0.9f : c0;
                float grow = Ease(k / 0.3f);
                float fade = 1f - Mathf.Clamp01((k - 0.8f) / 0.2f);
                for (int g = 0; g < 3; g++)
                {
                    var grp = g == 0 ? outer : (g == 1 ? mid : core);
                    float rf = g == 0 ? 1f : (g == 1 ? 0.7f : 0.38f);
                    for (int i = 0; i < grp.Length; i++)
                    {
                        float ph = i * Mathf.PI * 2f / grp.Length + g * 0.5f;
                        for (int j = 0; j < P; j++)
                        {
                            float u = j / (float)(P - 1);
                            float y = u * h * grow;
                            float rad = r * (0.3f + 0.7f * u) * rf * (water ? 1f + 0.12f * Mathf.Sin(u * 9f - t * 6f) : 1f);
                            float ang = ph + u * turns * Mathf.PI * 2f + t * (water ? 7f : 10f);
                            float wob = (Mathf.PerlinNoise(u * 2f + i, t * 5f) - 0.5f) * r * 0.25f;
                            grp[i].SetPosition(j, c + new Vector3(Mathf.Cos(ang) * (rad + wob), y, Mathf.Sin(ang) * (rad + wob)));
                        }
                    }
                    Wid(grp, (water ? 0.5f : 0.6f) * r * 0.5f * (g == 0 ? 1f : g == 1 ? 0.7f : 0.38f) * fade);
                }
                for (int i = 0; i < rings.Length; i++)
                {
                    float u2 = Mathf.Repeat(t * 0.9f + i * 0.33f, 1f);
                    float rr = r * (0.45f + 0.9f * u2); float yy = h * grow * u2;
                    for (int j = 0; j < 40; j++) { float a = j / 40f * Mathf.PI * 2f; rings[i].SetPosition(j, c + new Vector3(Mathf.Cos(a) * rr, yy, Mathf.Sin(a) * rr)); }
                    rings[i].widthMultiplier = r * 0.07f * (1f - u2) * fade;
                }
                emb += Time.deltaTime;
                if (emb > 0.04f)
                {
                    emb = 0f; float a = Random.value * 6.28f, hh = Random.value * h * grow;
                    Vector3 q = c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r * (0.3f + 0.7f * hh / Mathf.Max(0.1f, h)) + Vector3.up * hh;
                    CombatFx.Burst(q, water ? p.h : p.m, 2, 0.3f, 1.5f, 0.7f, 0.12f);
                    if (water && Random.value < 0.3f) CombatFx.Flash(q, p.w, 2.2f, 1.1f, 0.12f);
                }
                yield return null;
            }
            rig.Kill();
            Vector3 ce = follow != null ? follow.position - Vector3.up * 0.9f : c0;
            CombatFx.ShockRing(ce, r * 2.2f, water ? p.h : p.m, 0.4f);
            CombatFx.Burst(ce + Vector3.up * 0.5f, water ? p.w : p.h, 14, r, 5f, 0.9f, 0.15f);
        }

        /// <summary>Ledakan lidah api dari tanah: banyak lidah runcing menyembul berurutan dari pusat ke tepi, lalu terlepas naik.</summary>
        public static void Eruption(Vector3 c, float r, int count, float h, Pal p) { Host.StartCoroutine(EruptionR(c, r, count, h, p)); }

        static IEnumerator EruptionR(Vector3 c, float r, int count, float h, Pal p)
        {
            MagicFx.MagicCircle(c, r * 1.15f, p.m, 1.1f);
            CombatFx.ShockRing(c, r * 1.6f, p.m, 0.45f); CombatFx.Flash(c + Vector3.up, p.h, r * 3f, 3f, 0.4f);
            var rig = new Rig();
            const int P = 14;
            var mid = rig.Group(count, p.m, TongueCurve, P);
            var core = rig.Group(count, p.h, TongueCurve, P);
            var back = rig.Group(count, p.k, TongueCurve, P);
            var pos = new Vector3[count]; var hh = new float[count]; var dl = new float[count]; var lean = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float a = Random.value * 6.28f, d = Mathf.Sqrt(Random.value) * r;
                pos[i] = c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * d;
                hh[i] = h * Random.Range(0.55f, 1.15f) * (1f - 0.45f * d / r);
                dl[i] = d / r * 0.32f + Random.value * 0.1f;
                lean[i] = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Random.Range(0.1f, 0.6f);
            }
            float life = 1.0f, t = 0f, tot = life + 0.5f;
            while (t < tot)
            {
                t += Time.deltaTime;
                for (int i = 0; i < count; i++)
                {
                    float k = (t - dl[i]) / life;
                    bool on = k > 0f && k < 1f;
                    mid[i].enabled = core[i].enabled = back[i].enabled = on;
                    if (!on) continue;
                    float grow = Ease(k / 0.28f), lift = Mathf.Clamp01((k - 0.55f) / 0.45f), fade = 1f - Mathf.Clamp01((k - 0.7f) / 0.3f);
                    float H = hh[i] * grow;
                    for (int j = 0; j < P; j++)
                    {
                        float u = j / (float)(P - 1);
                        float y = Mathf.Lerp(u * H, hh[i] * 0.9f * lift + u * H * (1f - 0.4f * lift), lift * 0.7f);
                        Vector3 sway = lean[i] * u * u * H + new Vector3(Mathf.Sin(u * 5f + t * 12f + i), 0f, Mathf.Cos(u * 5f + t * 11f + i * 2f)) * 0.22f * u;
                        Vector3 q = pos[i] + Vector3.up * y + sway;
                        back[i].SetPosition(j, q); mid[i].SetPosition(j, q + Vector3.up * 0.02f); core[i].SetPosition(j, q + Vector3.up * 0.04f);
                    }
                    float w = Mathf.Max(0.35f, r * 0.2f) * fade;
                    back[i].widthMultiplier = w * 1.25f; mid[i].widthMultiplier = w * 0.85f; core[i].widthMultiplier = w * 0.4f;
                }
                if (Random.value < 0.6f) CombatFx.Burst(c + Random.insideUnitSphere * r * 0.7f + Vector3.up * 0.4f, p.m, 2, 0.4f, 3f, 1f, 0.14f);
                yield return null;
            }
            rig.Kill();
        }

        // ------------------------------------------------------------------ AIR
        /// <summary>Ular air: tiga ribbon biru-cyan-putih yang berkelok dan melesat dari a ke b, memercik saat tiba.</summary>
        public static void Serpent(Vector3 a, Vector3 b, float secs, float width, Pal p, int strands = 3, float amp = 1.2f, bool splash = true) { Host.StartCoroutine(SerpentR(a, b, secs, width, p, strands, amp, splash)); }

        static IEnumerator SerpentR(Vector3 a, Vector3 b, float secs, float width, Pal p, int strands, float amp, bool splash)
        {
            Vector3 d = b - a; float dist = Mathf.Max(0.5f, d.magnitude); Vector3 fw = d / dist;
            Vector3 side = Vector3.Cross(Vector3.up, fw); if (side.sqrMagnitude < 1e-4f) side = Vector3.right; side.Normalize();
            var rig = new Rig();
            const int P = 34;
            var outer = rig.Group(strands, p.k, JetCurve, P);
            var mid = rig.Group(strands, p.m, JetCurve, P);
            var foam = rig.Group(Mathf.Max(2, strands - 1), p.w, JetCurve, P);
            float total = secs * 1.5f, t = 0f, emb = 0f; bool hit = false;
            while (t < total)
            {
                t += Time.deltaTime; float k = t / total;
                float head = Ease(t / secs), tail = Mathf.Clamp01((k - 0.45f) / 0.55f) * 0.9f; if (tail > head - 0.05f) tail = Mathf.Max(0f, head - 0.05f);
                float fade = 1f - Mathf.Clamp01((k - 0.8f) / 0.2f);
                for (int g = 0; g < 3; g++)
                {
                    var grp = g == 0 ? outer : (g == 1 ? mid : foam);
                    for (int i = 0; i < grp.Length; i++)
                    {
                        float ph = i * 2.1f + g * 0.8f;
                        for (int j = 0; j < P; j++)
                        {
                            float u = Mathf.Lerp(tail, head, j / (float)(P - 1));
                            float env = Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI);
                            float wave = Mathf.Sin(u * 11f - t * 15f + ph) * amp * (0.25f + 0.75f * env);
                            float lift = Mathf.Sin(u * 9f - t * 13f + ph) * amp * 0.5f * env + env * dist * 0.06f;
                            grp[i].SetPosition(j, a + fw * (u * dist) + side * wave + Vector3.up * lift + side * (i - (grp.Length - 1) * 0.5f) * width * 0.35f);
                        }
                    }
                    Wid(grp, width * (g == 0 ? 1.2f : g == 1 ? 0.8f : 0.3f) * fade);
                }
                emb += Time.deltaTime;
                if (emb > 0.035f) { emb = 0f; CombatFx.Burst(a + fw * (head * dist), p.h, 3, width * 0.7f, 2.4f, 0.7f, 0.1f); }
                if (!hit && head >= 0.98f && splash)
                {
                    hit = true;
                    CombatFx.ShockRing(b, width * 5f, p.h, 0.4f); CombatFx.Burst(b + Vector3.up * 0.3f, p.w, 16, width * 2.5f, 6f, 0.9f, 0.16f);
                    CombatFx.Flash(b + Vector3.up * 0.5f, p.h, width * 7f, 2.2f, 0.25f);
                    Host.StartCoroutine(Splash(b, width * 2.5f, p));
                }
                yield return null;
            }
            rig.Kill();
        }

        /// <summary>Mahkota percikan air: lengkung ribbon melompat keluar dari titik hantaman dan jatuh.</summary>
        static IEnumerator Splash(Vector3 c, float r, Pal p)
        {
            int n = 9; var rig = new Rig();
            var g1 = rig.Group(n, p.m, TongueCurve, 12); var g2 = rig.Group(n, p.w, TongueCurve, 12);
            float t = 0f, life = 0.7f;
            while (t < life)
            {
                t += Time.deltaTime; float k = t / life;
                for (int i = 0; i < n; i++)
                {
                    float a = i / (float)n * 6.28f + 0.3f; Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    for (int j = 0; j < 12; j++)
                    {
                        float u = j / 11f, x = Mathf.Lerp(0.2f, 1f, u) * r * Ease(k * 1.6f);
                        float y = Mathf.Sin(u * Mathf.PI) * r * 0.9f * (1f - k * 0.6f);
                        Vector3 q = c + dir * x + Vector3.up * y;
                        g1[i].SetPosition(j, q); g2[i].SetPosition(j, q + Vector3.up * 0.03f);
                    }
                }
                Wid(g1, r * 0.22f * (1f - k)); Wid(g2, r * 0.09f * (1f - k));
                yield return null;
            }
            rig.Kill();
        }

        /// <summary>Gelombang pasang berlapis: tembok air melengkung (curl) menyapu ke depan dengan busa putih di puncak dan semburan percikan.</summary>
        public static void Wave(Vector3 o, Vector3 fwd, float width, float length, float height, float secs, Pal p) { Host.StartCoroutine(WaveR(o, fwd, width, length, height, secs, p)); }

        static IEnumerator WaveR(Vector3 o, Vector3 fwd, float width, float length, float height, float secs, Pal p)
        {
            fwd.y = 0f; if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward; fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            var rig = new Rig();
            const int LAYERS = 3, ROWS = 6, P = 26;
            var rows = new LineRenderer[LAYERS][];
            for (int l = 0; l < LAYERS; l++)
            {
                Color c = l == 0 ? p.k : (l == 1 ? p.m : p.h);
                rows[l] = rig.Group(ROWS, c, FlatCurve, P);
            }
            var foam = rig.Group(2, p.w, FlatCurve, P);
            float t = 0f, emb = 0f;
            while (t < secs)
            {
                t += Time.deltaTime; float k = t / secs;
                float front = length * Ease(k / 0.75f);
                float env = Mathf.Clamp01(k / 0.15f) * (1f - Mathf.Clamp01((k - 0.8f) / 0.2f));
                for (int l = 0; l < LAYERS; l++)
                {
                    float back = l * height * 0.55f; float H = height * env * (1f - l * 0.18f);
                    for (int rI = 0; rI < ROWS; rI++)
                    {
                        float fr = rI / (float)(ROWS - 1);
                        float curl = Mathf.Pow(fr, 2.3f) * H * 0.75f;
                        for (int j = 0; j < P; j++)
                        {
                            float x = j / (float)(P - 1) * 2f - 1f;
                            float arch = 1f - 0.45f * x * x;
                            float rip = 1f + 0.12f * Mathf.Sin(x * 8f + t * 11f + rI);
                            Vector3 q = o + fwd * (front - back + curl * arch) + right * (x * width * 0.5f) + Vector3.up * (fr * H * arch * rip + 0.1f);
                            rows[l][rI].SetPosition(j, q);
                        }
                        rows[l][rI].widthMultiplier = height * 0.14f * env * (l == 0 ? 1.3f : 1f);
                    }
                }
                for (int f = 0; f < foam.Length; f++)
                    for (int j = 0; j < P; j++)
                    {
                        float x = j / (float)(P - 1) * 2f - 1f, arch = 1f - 0.45f * x * x;
                        float H = height * env;
                        Vector3 q = o + fwd * (front + H * 0.8f * arch + f * 0.4f) + right * (x * width * 0.5f) + Vector3.up * (H * arch * (1.02f + 0.1f * Mathf.Sin(x * 12f + t * 14f + f)) + 0.1f);
                        foam[f].SetPosition(j, q);
                    }
                Wid(foam, height * 0.12f * env);
                emb += Time.deltaTime;
                if (emb > 0.04f)
                {
                    emb = 0f;
                    for (int i = 0; i < 3; i++) CombatFx.Burst(o + fwd * (front + height * 0.6f) + right * Random.Range(-0.5f, 0.5f) * width + Vector3.up * height * env * Random.Range(0.6f, 1.1f), p.w, 2, 0.5f, 3f, 0.8f, 0.14f);
                    if (Random.value < 0.35f) CombatFx.ShockRing(o + fwd * front, height * 1.5f, p.h, 0.25f);
                }
                yield return null;
            }
            rig.Kill();
        }

        // ------------------------------------------------------------------ TANAH
        static Material stoneA, stoneB, stoneC;
        static Material Stone(int i)
        {
            if (stoneA == null) stoneA = CombatFx.Solid(null, new Color(0.40f, 0.31f, 0.22f));
            if (stoneB == null) stoneB = CombatFx.Solid(null, new Color(0.56f, 0.44f, 0.30f));
            if (stoneC == null) stoneC = CombatFx.Solid(null, new Color(0.78f, 0.66f, 0.46f));
            return i == 0 ? stoneA : (i == 1 ? stoneB : stoneC);
        }

        static GameObject Slab(Vector3 pos, Vector3 scale, Quaternion rot, int mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = "FxStone"; go.transform.position = pos; go.transform.rotation = rot; go.transform.localScale = scale;
            var mr = go.GetComponent<MeshRenderer>(); mr.sharedMaterial = Stone(mat); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        /// <summary>Pilar batu bersegi: badan gelap + sisi terang + tutup runcing, menyembul cepat (overshoot), bertahan, lalu tenggelam.</summary>
        public static void Pillar(Vector3 pos, float h, float w, float delay, float hold, bool big = false) { Host.StartCoroutine(PillarR(pos, h, w, delay, hold, big)); }

        static IEnumerator PillarR(Vector3 pos, float h, float w, float delay, float hold, bool big)
        {
            if (delay > 0f) yield return W(delay);
            Quaternion rot = Quaternion.Euler(Random.Range(-9f, 9f), Random.Range(0f, 360f), Random.Range(-9f, 9f));
            var body = Slab(pos, new Vector3(w, 0.01f, w), rot, 0);
            var face = Slab(pos, new Vector3(w * 0.45f, 0.01f, w * 1.04f), rot * Quaternion.Euler(0f, 0f, 0f), 1);
            var cap = Slab(pos, new Vector3(w * 0.7f, 0.01f, w * 0.7f), rot * Quaternion.Euler(0f, 45f, 0f), 2);
            CombatFx.Dust(pos, big ? 8 : 4); MagicFx.Debri(pos, w * 1.4f, big ? 5 : 3, 0.7f);
            if (big) { CombatFx.ShockRing(pos, w * 3f, new Color(0.85f, 0.7f, 0.45f, 0.9f), 0.3f); MagicFx.Crack(pos, w * 2.2f, 2.5f); }
            float rise = 0.16f, down = 0.4f, t = 0f, total = rise + hold + down;
            while (t < total && body != null)
            {
                t += Time.deltaTime; float cur;
                if (t < rise) { float u = t / rise; cur = h * (1.12f * (1f - (1f - u) * (1f - u)) + 0f); }
                else if (t < rise + hold) cur = h * (1f + 0.04f * Mathf.Sin((t - rise) * 30f) * Mathf.Clamp01(1f - (t - rise) * 4f));
                else cur = h * (1f - Mathf.Clamp01((t - rise - hold) / down));
                cur = Mathf.Max(0.01f, cur);
                body.transform.position = pos + rot * Vector3.up * (cur * 0.5f); body.transform.localScale = new Vector3(w, cur, w);
                face.transform.position = pos + rot * (Vector3.up * (cur * 0.5f) + Vector3.right * w * 0.28f); face.transform.localScale = new Vector3(w * 0.45f, cur * 0.98f, w * 1.04f);
                cap.transform.position = pos + rot * Vector3.up * (cur + w * 0.1f); cap.transform.localScale = new Vector3(w * 0.7f, w * 0.34f, w * 0.7f);
                yield return null;
            }
            if (body != null) Object.Destroy(body); if (face != null) Object.Destroy(face); if (cap != null) Object.Destroy(cap);
        }

        /// <summary>Hutan pilar batu: tinggi bervariasi, muncul berurutan dari pusat ke tepi, cincin kejut dan retakan.</summary>
        public static void PillarField(Vector3 c, float r, int count, float h, float hold = 0.9f)
        {
            MagicFx.MagicCircle(c, r * 1.1f, Earth.h, 0.9f); MagicFx.Crack(c, r * 1.3f, 3.5f);
            CombatFx.ShockRing(c, r * 1.8f, new Color(0.85f, 0.7f, 0.45f, 0.9f), 0.5f); ThirdPersonCamera.Shake(0.35f, 0.5f);
            for (int i = 0; i < count; i++)
            {
                float a = i * 2.399f, d = Mathf.Sqrt((i + 0.5f) / count) * r;
                Vector3 q = c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * d;
                float hh = h * Random.Range(0.55f, 1.2f) * (1.1f - 0.5f * d / r);
                Pillar(q, hh, Random.Range(0.9f, 1.8f) * Mathf.Clamp(r / 6f, 0.8f, 1.6f), d / r * 0.4f + Random.value * 0.08f, hold, i % 4 == 0);
            }
        }

        /// <summary>Gelombang tanah: pilar menyembul berurutan melaju lurus ke depan, makin besar di ujung (Doton).</summary>
        public static void RockWave(Vector3 o, Vector3 fwd, float len, float spread, float h)
        {
            fwd.y = 0f; fwd.Normalize(); Vector3 right = Vector3.Cross(Vector3.up, fwd);
            int n = Mathf.Max(5, (int)(len / 1.6f));
            for (int i = 0; i < n; i++)
            {
                float u = (i + 1) / (float)n;
                for (int s = -1; s <= 1; s += 2)
                {
                    if (i % 2 == 0 && s == 1) continue;
                    Vector3 q = o + fwd * (u * len) + right * s * spread * (0.15f + 0.85f * u) * (i % 2 == 0 ? 0.2f : 1f);
                    Pillar(q, h * (0.4f + 0.9f * u), 0.9f + 0.9f * u, u * 0.35f, 0.5f, i == n - 1);
                }
            }
            MagicFx.Crack(o + fwd * len * 0.5f, len * 0.5f, 3f);
        }

        /// <summary>Batu melayang: terangkat dari tanah, mengorbit, lalu (opsional) dilempar ke sasaran. follow != null: tetap mengelilingi pemain.</summary>
        public static void FloatingRocks(Vector3 c, Transform follow, float r, int count, float secs, Vector3? hurlTo) { Host.StartCoroutine(FloatingRocksR(c, follow, r, count, secs, hurlTo)); }

        static IEnumerator FloatingRocksR(Vector3 c0, Transform follow, float r, int count, float secs, Vector3? hurl)
        {
            var rocks = new GameObject[count]; var ph = new float[count]; var sz = new float[count]; var hy = new float[count];
            for (int i = 0; i < count; i++)
            {
                sz[i] = Random.Range(0.35f, 0.9f); ph[i] = i * Mathf.PI * 2f / count; hy[i] = Random.Range(0.8f, 2.6f);
                rocks[i] = Slab(c0, Vector3.one * sz[i], Random.rotation, i % 3); Object.Destroy(rocks[i], secs + 2f);
            }
            CombatFx.Dust(c0, 12); MagicFx.Crack(c0, r * 0.9f, 2.5f);
            float t = 0f, orbitT = hurl.HasValue ? secs * 0.65f : secs;
            Vector3[] hurlStart = new Vector3[count]; bool launched = false;
            try
            {
            while (t < secs)
            {
                t += Time.deltaTime;
                Vector3 c = follow != null ? follow.position - Vector3.up * 0.9f : c0;
                for (int i = 0; i < count; i++)
                {
                    if (rocks[i] == null) continue;
                    if (t < orbitT || !hurl.HasValue)      // v35z102n15: tanpa target lempar, jangan baca hurl.Value (penyebab batu menggantung)
                    {
                        float k = Ease(t / (orbitT * 0.4f)); float a = ph[i] + t * 3.2f;
                        Vector3 q = c + new Vector3(Mathf.Cos(a) * r, hy[i] * k + 0.4f * Mathf.Sin(t * 4f + i), Mathf.Sin(a) * r);
                        rocks[i].transform.position = q; rocks[i].transform.Rotate(new Vector3(90f, 140f, 60f) * Time.deltaTime);
                        hurlStart[i] = q;
                    }
                    else
                    {
                        if (!launched) { launched = true; ThirdPersonCamera.Shake(0.2f, 0.25f); }
                        float u = Mathf.Clamp01((t - orbitT - i * 0.04f) / 0.35f);
                        Vector3 tg = hurl.Value + Random.insideUnitSphere * 0.0f;
                        rocks[i].transform.position = Vector3.Lerp(hurlStart[i], tg + Vector3.up * 0.4f, u * u);
                        rocks[i].transform.Rotate(new Vector3(500f, 300f, 200f) * Time.deltaTime);
                        if (u >= 1f)
                        {
                            Vector3 hp = rocks[i].transform.position;
                            Object.Destroy(rocks[i]); rocks[i] = null;
                            CombatFx.Burst(hp, Earth.h, 5, 1.2f, 4f, 0.7f, 0.14f); MagicFx.Debri(hp, 1.4f, 2, 0.8f);
                            if (i % 3 == 0) CombatFx.ShockRing(hp, 2.6f, new Color(0.85f, 0.7f, 0.45f, 0.9f), 0.3f);
                        }
                    }
                }
                yield return null;
            }
            }
            finally
            {
                for (int i = 0; i < count; i++) if (rocks[i] != null) Object.Destroy(rocks[i]);
            }
        }

        // ------------------------------------------------------------------ pemilih skill
        static bool IsIce(SkillDef s)
        {
            string n = (s.name ?? "").ToLowerInvariant();
            return n.Contains("ice") || n.Contains("frost") || n.Contains("glacial") || n.Contains("blizzard") || n.Contains("absolute") || n.Contains("zero") || n.Contains("freez");
        }

        /// <summary>Panggil dari SkillSpectacle.Play. Mengembalikan true bila skill ini bergaya anime (api / air / tanah).</summary>
        public static bool Play(SkillDef s, PlayerCombat pc, Vector3 C, Vector3 F)
        {
            string e = !string.IsNullOrEmpty(s.elem) ? s.elem : JobInfo.Elem(s.job);
            if (e != "fire" && e != "water" && e != "earth") return false;
            bool ult = s.tier >= 3; string k = SkillAction.KindOf(s);
            float sc = ult ? 1.7f : 1f;
            float rad = Mathf.Max(3f, s.radius > 0f ? s.radius : 4f);
            Vector3 feet = pc.transform.position - Vector3.up * 0.9f;
            Vector3 tip = MagicFx.Tip();
            Vector3 toC = C - tip; Vector3 dir = toC.sqrMagnitude > 1f ? toC.normalized : F;
            float dist = Mathf.Clamp(Vector3.Distance(feet, C), 6f, 40f);
            if (e == "fire") PlayFire(s, pc, k, ult, sc, rad, feet, tip, C, F, dir, dist);
            else if (e == "water") PlayWater(s, pc, k, ult, sc, rad, feet, tip, C, F, dir, dist);
            else PlayEarth(s, pc, k, ult, sc, rad, feet, tip, C, F, dir, dist);
            return true;
        }

        static void PlayFire(SkillDef s, PlayerCombat pc, string k, bool ult, float sc, float rad, Vector3 feet, Vector3 tip, Vector3 C, Vector3 F, Vector3 dir, float dist)
        {
            var p = Fire;
            switch (k)
            {
                case "bolt": case "multi": case "line":
                    FlameJet(tip, dir, Mathf.Min(dist, 28f), 1.1f * sc, 0.85f, p, ult ? 9 : 6);
                    Host.StartCoroutine(Later(0.22f, () => Eruption(C, ult ? rad * 1.2f : Mathf.Min(rad, 4f), ult ? 26 : 12, ult ? 7f : 3.8f, p)));
                    if (ult) FlameJet(tip, dir, Mathf.Min(dist, 34f), 2.2f, 1.1f, p, 10);
                    break;
                case "cone":
                    for (int i = -2; i <= 2; i++) FlameJet(tip, Quaternion.Euler(0f, i * 16f, 0f) * dir, 11f * sc, 1.0f * sc, 0.8f, p, 5);
                    if (ult) Vortex(feet + F * 4f, null, 3.5f, 9f, 1.4f, p, false);
                    break;
                case "nova":
                    Eruption(feet, rad * sc, ult ? 34 : 18, ult ? 7f : 4f, p);
                    Vortex(feet, null, Mathf.Min(rad * 0.7f, 4.5f) * sc, ult ? 12f : 7f, ult ? 1.8f : 1.1f, p, false);
                    break;
                case "ground": case "trap": case "summon":
                    Eruption(C, rad * sc, ult ? 36 : 20, ult ? 8f : 5f, p);
                    Vortex(C, null, rad * 0.55f * sc, ult ? 14f : 8f, ult ? 2f : 1.2f, p, false, ult ? 8 : 6);
                    break;
                case "dash":
                    FlameJet(feet + Vector3.up * 0.8f, F, 10f, 1.3f, 0.7f, p, 6);
                    Eruption(feet + F * 2f, 2.5f, 8, 2.5f, p);
                    break;
                case "buff": case "heal":
                    Vortex(feet, pc.transform, 1.8f * sc, 4.5f * sc, ult ? 2.4f : 1.4f, p, false, 5, 1.6f);
                    Eruption(feet, 2.6f * sc, 10, 2.8f, p);
                    break;
                default:
                    FlameJet(tip, dir, Mathf.Min(dist, 26f), 1.1f * sc, 0.8f, p, 6);
                    break;
            }
        }

        static void PlayWater(SkillDef s, PlayerCombat pc, string k, bool ult, float sc, float rad, Vector3 feet, Vector3 tip, Vector3 C, Vector3 F, Vector3 dir, float dist)
        {
            var p = IsIce(s) ? Ice : Water;
            switch (k)
            {
                case "bolt": case "multi": case "line":
                    Serpent(tip, C + Vector3.up * 0.6f, 0.38f, 0.55f * sc, p, ult ? 5 : 3, 1.4f * sc);
                    if (k == "multi" || ult) for (int i = 0; i < (ult ? 4 : 2); i++) { float ang = (i + 1) * (i % 2 == 0 ? 1f : -1f) * 12f; Vector3 off = Quaternion.Euler(0f, ang, 0f) * (C - tip); Serpent(tip, tip + off + Vector3.up * 0.6f, 0.42f + i * 0.05f, 0.4f * sc, p, 3, 1.6f * sc); }
                    break;
                case "cone":
                    Wave(feet + F * 1.2f, F, 9f * sc, 16f * sc, 2.8f * sc, 1.1f, p);
                    break;
                case "nova":
                    Vortex(feet, null, Mathf.Min(rad * 0.6f, 4.5f) * sc, ult ? 12f : 7f, ult ? 1.8f : 1.2f, p, true);
                    for (int i = 0; i < 6; i++) { float a = i * 60f; Serpent(feet + Vector3.up * 0.8f, feet + Quaternion.Euler(0f, a, 0f) * Vector3.forward * rad * sc + Vector3.up * 0.4f, 0.5f, 0.45f * sc, p, 2, 0.9f); }
                    break;
                case "ground": case "trap": case "summon":
                    Vortex(C, null, rad * 0.5f * sc, ult ? 14f : 8f, ult ? 2f : 1.3f, p, true, ult ? 8 : 6);
                    Host.StartCoroutine(Later(0.15f, () => Wave(C - F * rad * sc, F, rad * 1.6f * sc, rad * 2f * sc, 2.4f * sc, 1f, p)));
                    break;
                case "dash":
                    Serpent(feet + Vector3.up * 0.7f, feet + F * 10f + Vector3.up * 0.7f, 0.35f, 0.7f, p, 3, 0.8f, false);
                    CombatFx.Burst(feet + F * 1.5f, p.w, 14, 1.4f, 5f, 0.8f, 0.16f);
                    break;
                case "buff": case "heal":
                    Vortex(feet, pc.transform, 1.9f * sc, 4.6f * sc, ult ? 2.4f : 1.5f, p, true, 5, 1.6f);
                    break;
                default:
                    Serpent(tip, C + Vector3.up * 0.6f, 0.38f, 0.55f * sc, p, 3, 1.3f);
                    break;
            }
            if (ult) Host.StartCoroutine(Later(0.25f, () => Wave(feet + F * 1.5f, F, 20f, 30f, 5.5f, 1.8f, p)));
        }

        static void PlayEarth(SkillDef s, PlayerCombat pc, string k, bool ult, float sc, float rad, Vector3 feet, Vector3 tip, Vector3 C, Vector3 F, Vector3 dir, float dist)
        {
            switch (k)
            {
                case "bolt": case "multi": case "line":
                    RockWave(feet + F * 1.5f, F, Mathf.Min(dist, 24f), 3.5f * sc, 3.2f * sc);
                    if (ult) FloatingRocks(feet, null, 4.5f, 12, 1.5f, C);
                    else FloatingRocks(feet, null, 2.6f, 4, 1.0f, C);
                    break;
                case "cone":
                    for (int i = -2; i <= 2; i++) RockWave(feet + F * 1.2f, Quaternion.Euler(0f, i * 17f, 0f) * F, 11f * sc, 1.2f, 2.8f * sc);
                    break;
                case "nova":
                    PillarField(feet, rad * sc, ult ? 30 : 16, ult ? 8f : 4.5f);
                    break;
                case "ground": case "trap": case "summon":
                    PillarField(C, rad * sc, ult ? 40 : 20, ult ? 10f : 5.5f, ult ? 1.3f : 0.9f);
                    FloatingRocks(C, null, Mathf.Min(rad, 6f) * 0.6f, ult ? 12 : 6, 1.3f, C);
                    break;
                case "dash":
                    RockWave(feet, F, 10f, 2.5f, 2.2f);
                    break;
                case "buff": case "heal":
                    FloatingRocks(feet, pc.transform, 2.4f * sc, ult ? 10 : 6, ult ? 3f : 2f, null);
                    PillarField(feet, 2.8f * sc, 8, 2.8f);
                    break;
                default:
                    RockWave(feet + F * 1.5f, F, Mathf.Min(dist, 22f), 3f, 3f);
                    break;
            }
            if (ult) Host.StartCoroutine(Later(0.3f, () => PillarField(C, rad * 1.5f, 36, 11f, 1.4f)));
        }

        static IEnumerator Later(float t, System.Action a) { yield return new WaitForSeconds(t); if (a != null) a(); }
    }
}
