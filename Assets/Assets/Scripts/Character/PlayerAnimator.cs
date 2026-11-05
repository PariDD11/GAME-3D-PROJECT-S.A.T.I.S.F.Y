using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Animasi prosedural pemain: jalan/lari/diam, kombo tebasan 3x, memanah, guard, guling (roll), terkena hit, pingsan, minum.
    /// Semua pose dinyatakan sebagai sudut Euler tiap sendi (bahu, siku, badan, kepala, kaki) lalu dihaluskan.
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        public enum Clip { None, Slash0, Slash1, Slash2, Shoot, Roll, RollBack, Hurt, Faint, Drink, Spell, Custom }

        public class Pose
        {
            public Vector3 arR, fR, arL, fL, body, head, lgL, lgR;
            public float lunge, lift, pitch;
            public Pose Copy() { return (Pose)MemberwiseClone(); }
            public const int N = 27;
            public float[] ToArr()
            {
                var a = new float[N]; int i = 0;
                foreach (var v in new[] { arR, fR, arL, fL, body, head, lgL, lgR }) { a[i++] = v.x; a[i++] = v.y; a[i++] = v.z; }
                a[i++] = lunge; a[i++] = lift; a[i++] = pitch; return a;
            }
            public static Pose FromArr(float[] a)
            {
                var p = new Pose(); int i = 0;
                p.arR = new Vector3(a[i++], a[i++], a[i++]); p.fR = new Vector3(a[i++], a[i++], a[i++]);
                p.arL = new Vector3(a[i++], a[i++], a[i++]); p.fL = new Vector3(a[i++], a[i++], a[i++]);
                p.body = new Vector3(a[i++], a[i++], a[i++]); p.head = new Vector3(a[i++], a[i++], a[i++]);
                p.lgL = new Vector3(a[i++], a[i++], a[i++]); p.lgR = new Vector3(a[i++], a[i++], a[i++]);
                p.lunge = a[i++]; p.lift = a[i++]; p.pitch = a[i++]; return p;
            }
            public static Pose Lerp(Pose a, Pose b, float t)
            {
                var r = new Pose();
                r.arR = Vector3.Lerp(a.arR, b.arR, t); r.fR = Vector3.Lerp(a.fR, b.fR, t);
                r.arL = Vector3.Lerp(a.arL, b.arL, t); r.fL = Vector3.Lerp(a.fL, b.fL, t);
                r.body = Vector3.Lerp(a.body, b.body, t); r.head = Vector3.Lerp(a.head, b.head, t);
                r.lgL = Vector3.Lerp(a.lgL, b.lgL, t); r.lgR = Vector3.Lerp(a.lgR, b.lgR, t);
                r.lunge = Mathf.Lerp(a.lunge, b.lunge, t); r.lift = Mathf.Lerp(a.lift, b.lift, t); r.pitch = Mathf.Lerp(a.pitch, b.pitch, t);
                return r;
            }
        }

        struct Key { public float t; public Pose p; }
        class ClipData { public Key[] keys; public float dur; public bool hold; public bool legs = true; public float[][] v, m; }

        static Vector3 V(float x, float y = 0f, float z = 0f) { return new Vector3(x, y, z); }

        // pose dasar kuda-kuda tempur
        static Pose N()
        {
            return new Pose { arR = V(-20, -12, 8), fR = V(-65), arL = V(-10, 0, -8), fL = V(-45), body = V(0, 14, 0), head = V(0, -10, 0), lgL = V(-8), lgR = V(10) };
        }
        static Pose P(Pose b, System.Action<Pose> f) { var p = b.Copy(); f(p); return p; }
        static Key K(float t, Pose p) { return new Key { t = t, p = p }; }

        static readonly Dictionary<Clip, ClipData> clips = Build();

        static Dictionary<Clip, ClipData> Build()
        {
            var d = new Dictionary<Clip, ClipData>();
            Pose n = N();

            // Tebasan 1: diagonal dari kanan atas
            d[Clip.Slash0] = new ClipData
            {
                dur = 0.42f,
                keys = new[] {
                    K(0f, n),
                    K(0.28f, P(n, p => { p.arR = V(-120, -22, 24); p.fR = V(-20); p.arL = V(-30, 0, -22); p.body = V(-6, 28, 0); p.head = V(0, -20, 0); p.lgL = V(-4); p.lgR = V(18); })),
                    K(0.55f, P(n, p => { p.arR = V(-40, 38, -14); p.fR = V(-4); p.arL = V(-5, 0, -30); p.body = V(14, -34, 0); p.head = V(0, 25, 0); p.lunge = 0.24f; p.lgL = V(-38); p.lgR = V(24); })),
                    K(0.82f, P(n, p => { p.arR = V(-36, 30, -10); p.fR = V(-10); p.body = V(6, -20, 0); p.lunge = 0.2f; p.lgL = V(-30); p.lgR = V(20); })),
                    K(1f, n) }
            };
            // Tebasan 2: ayunan datar
            d[Clip.Slash1] = new ClipData
            {
                dur = 0.42f,
                keys = new[] {
                    K(0f, n),
                    K(0.28f, P(n, p => { p.arR = V(-84, 88, 0); p.fR = V(-20); p.arL = V(-20, 0, -28); p.body = V(0, 44, 0); p.head = V(0, -35, 0); p.lgL = V(10); p.lgR = V(-18); })),
                    K(0.55f, P(n, p => { p.arR = V(-86, -78, 0); p.fR = V(-4); p.arL = V(-40, 0, -50); p.body = V(6, -42, 0); p.head = V(0, 35, 0); p.lunge = 0.22f; p.lgL = V(-34); p.lgR = V(22); })),
                    K(0.82f, P(n, p => { p.arR = V(-80, -60, 0); p.fR = V(-10); p.body = V(2, -30, 0); p.lunge = 0.18f; p.lgL = V(-28); p.lgR = V(18); })),
                    K(1f, n) }
            };
            // Tebasan 3: hantaman dua tangan dari atas (kombo penutup)
            d[Clip.Slash2] = new ClipData
            {
                dur = 0.5f,
                keys = new[] {
                    K(0f, n),
                    K(0.32f, P(n, p => { p.arR = V(-108, 0, 14); p.fR = V(-20); p.arL = V(-104, 0, -14); p.fL = V(-20); p.body = V(-14, 0, 0); p.head = V(-12, 0, 0); p.lgL = V(12); p.lgR = V(-12); p.lift = 0.06f; })),
                    K(0.55f, P(n, p => { p.arR = V(-36, 0, 6); p.fR = V(-4); p.arL = V(-34, 0, -6); p.fL = V(-4); p.body = V(28, 0, 0); p.head = V(10, 0, 0); p.lunge = 0.34f; p.lgL = V(-40); p.lgR = V(30); p.lift = -0.05f; })),
                    K(0.85f, P(n, p => { p.arR = V(-34, 0, 6); p.fR = V(-8); p.arL = V(-32, 0, -6); p.fL = V(-8); p.body = V(22, 0, 0); p.lunge = 0.3f; p.lgL = V(-34); p.lgR = V(26); p.lift = -0.04f; })),
                    K(1f, n) }
            };
            // Mantra tongkat: angkat tongkat tinggi lalu hentakkan ke depan
            d[Clip.Spell] = new ClipData
            {
                dur = 0.5f,
                keys = new[] {
                    K(0f, n),
                    K(0.35f, P(n, p => { p.arR = V(-150, 0, 12); p.fR = V(-35); p.arL = V(-70, 0, -22); p.fL = V(-25); p.body = V(-10, 0, 0); p.head = V(-10, 0, 0); p.lgL = V(6); p.lgR = V(-6); p.lift = 0.04f; })),
                    K(0.6f, P(n, p => { p.arR = V(-82, 0, 0); p.fR = V(-4); p.arL = V(-88, 0, -10); p.fL = V(-6); p.body = V(14, 0, 0); p.head = V(6, 0, 0); p.lunge = 0.14f; p.lgL = V(-18); p.lgR = V(14); })),
                    K(0.85f, P(n, p => { p.arR = V(-70, 0, 4); p.fR = V(-8); p.arL = V(-60, 0, -8); p.body = V(6, 0, 0); p.lunge = 0.08f; })),
                    K(1f, n) }
            };
            // Memanah: tarik busur, lepas
            d[Clip.Shoot] = new ClipData
            {
                dur = 0.55f,
                keys = new[] {
                    K(0f, n),
                    K(0.25f, P(n, p => { p.arL = V(-92, -6, -4); p.fL = V(-4); p.arR = V(-88, 38, 8); p.fR = V(-132); p.body = V(0, -32, 0); p.head = V(0, 40, 0); p.lgL = V(4); p.lgR = V(-4); })),
                    K(0.5f, P(n, p => { p.arL = V(-92, -6, -4); p.fL = V(-2); p.arR = V(-88, 38, 8); p.fR = V(-136); p.body = V(0, -32, 0); p.head = V(0, 40, 0); p.lgL = V(4); p.lgR = V(-4); })),
                    K(0.58f, P(n, p => { p.arL = V(-94, -6, -4); p.fL = V(0); p.arR = V(-80, 62, 20); p.fR = V(-60); p.body = V(0, -26, 0); p.head = V(0, 36, 0); p.lgL = V(4); p.lgR = V(-4); })),
                    K(1f, n) }
            };
            // Guling ke depan / belakang
            Pose tuck = P(n, p => { p.arR = V(-60, 0, 10); p.fR = V(-110); p.arL = V(-60, 0, -10); p.fL = V(-110); p.body = V(35, 0, 0); p.head = V(25, 0, 0); p.lgL = V(-80); p.lgR = V(-80); });
            d[Clip.Roll] = new ClipData { dur = 0.32f, keys = new[] { K(0f, n), K(0.2f, P(tuck, p => p.pitch = 70)), K(0.6f, P(tuck, p => p.pitch = 220)), K(0.85f, P(tuck, p => p.pitch = 320)), K(1f, P(n, p => p.pitch = 360)) } };
            Pose tuckB = P(tuck, p => { p.body = V(35, 0, 0); });
            d[Clip.RollBack] = new ClipData { dur = 0.32f, keys = new[] { K(0f, n), K(0.2f, P(tuckB, p => p.pitch = -70)), K(0.6f, P(tuckB, p => p.pitch = -220)), K(0.85f, P(tuckB, p => p.pitch = -320)), K(1f, P(n, p => p.pitch = -360)) } };
            // Terkena hit
            d[Clip.Hurt] = new ClipData
            {
                dur = 0.36f,
                keys = new[] {
                    K(0f, n),
                    K(0.2f, P(n, p => { p.body = V(-20, 0, 0); p.head = V(-24, 0, 0); p.arR = V(-12, 0, 48); p.arL = V(-12, 0, -48); p.fR = V(-30); p.fL = V(-30); p.lunge = -0.16f; p.lgL = V(10); p.lgR = V(-14); })),
                    K(1f, n) }
            };
            // Pingsan: roboh telentang
            Pose down = new Pose { arR = V(-10, 0, 70), fR = V(-10), arL = V(-10, 0, -70), fL = V(-10), body = V(0), head = V(-12, 0, 0), lgL = V(-4), lgR = V(6), pitch = -86, lift = -0.52f, lunge = -0.35f };
            d[Clip.Faint] = new ClipData { dur = 0.9f, hold = true, keys = new[] { K(0f, n), K(0.25f, P(n, p => { p.body = V(-22, 0, 0); p.head = V(-28, 0, 0); p.arR = V(-5, 0, 50); p.arL = V(-5, 0, -50); p.pitch = -20; p.lunge = -0.12f; })), K(0.75f, down), K(1f, down) } };
            // Minum ramuan
            d[Clip.Drink] = new ClipData
            {
                dur = 0.9f,
                keys = new[] {
                    K(0f, n),
                    K(0.3f, P(n, p => { p.arR = V(-100, -25, 0); p.fR = V(-135); p.head = V(-6, 0, 0); })),
                    K(0.7f, P(n, p => { p.arR = V(-105, -25, 0); p.fR = V(-140); p.head = V(-22, 0, 0); p.body = V(-6, 14, 0); })),
                    K(1f, n) }
            };
            return d;
        }

        /// <summary>Pose kuda-kuda tempur dasar (untuk menyusun aksi skill).</summary>
        public static Pose Base() { return N(); }
        public static Pose Mk(Pose b, System.Action<Pose> f) { return P(b, f); }
        public static Vector3 Vec(float x, float y = 0f, float z = 0f) { return V(x, y, z); }

        /// <summary>Putar aksi kustom (timeline pose per skill): times 0..1 menaik, poses sama panjang.</summary>
        public void PlayCustom(float dur, float[] times, Pose[] poses)
        {
            var keys = new Key[times.Length];
            for (int i = 0; i < keys.Length; i++) keys[i] = K(times[i], poses[i]);
            clips[Clip.Custom] = new ClipData { dur = dur, keys = keys };
            Play(Clip.Custom);
        }

        /// <summary>
        /// Pose pada waktu ternormalisasi: spline Hermite monoton (C1) lewat semua pose kunci. Kecepatan gerak MENERUS di tiap kunci
        /// (tidak berhenti-mulai seperti smoothstep per segmen), dan tidak melampaui puncak ayunan (tangen 0 di titik balik).
        /// </summary>
        public static Pose Sample(Clip c, float norm)
        {
            var cd = clips[c];
            var k = cd.keys;
            norm = Mathf.Clamp01(norm);
            if (k.Length == 1) return k[0].p;
            if (cd.v == null)
            {
                int n = k.Length; cd.v = new float[n][]; cd.m = new float[n][];
                for (int i = 0; i < n; i++) cd.v[i] = k[i].p.ToArr();
                for (int i = 0; i < n; i++)
                {
                    var m = new float[Pose.N]; cd.m[i] = m;
                    if (i == 0 || i == n - 1) continue;           // ujung: tangen 0 (masuk/keluar dihaluskan terpisah)
                    float h0 = Mathf.Max(1e-4f, k[i].t - k[i - 1].t), h1 = Mathf.Max(1e-4f, k[i + 1].t - k[i].t);
                    for (int j = 0; j < Pose.N; j++)
                    {
                        float s0 = (cd.v[i][j] - cd.v[i - 1][j]) / h0, s1 = (cd.v[i + 1][j] - cd.v[i][j]) / h1;
                        if (s0 * s1 <= 0f) { m[j] = 0f; continue; }
                        float mm = (s0 * h1 + s1 * h0) / (h0 + h1);
                        float lim = 3f * Mathf.Min(Mathf.Abs(s0), Mathf.Abs(s1));
                        m[j] = Mathf.Sign(mm) * Mathf.Min(Mathf.Abs(mm), lim);
                    }
                }
            }
            int seg = k.Length - 2;
            for (int i = 0; i + 1 < k.Length; i++) if (norm <= k[i + 1].t) { seg = i; break; }
            float h = Mathf.Max(1e-4f, k[seg + 1].t - k[seg].t);
            float u = Mathf.Clamp01((norm - k[seg].t) / h);
            float u2 = u * u, u3 = u2 * u;
            float h00 = 2f * u3 - 3f * u2 + 1f, h10 = u3 - 2f * u2 + u, h01 = -2f * u3 + 3f * u2, h11 = u3 - u2;
            var o = new float[Pose.N];
            for (int j = 0; j < Pose.N; j++)
                o[j] = h00 * cd.v[seg][j] + h10 * h * cd.m[seg][j] + h01 * cd.v[seg + 1][j] + h11 * h * cd.m[seg + 1][j];
            return Pose.FromArr(o);
        }

        public static float Duration(Clip c) { return clips[c].dur; }

        /// <summary>Pose berjalan/berlari/diam. phase = fase langkah (radian), speed = m/s, drawn = senjata terhunus.</summary>
        public static Pose Locomotion(float phase, float speed, bool drawn, bool bow, float time, bool guard)
        {
            float walk = Mathf.Clamp01(speed / 1.4f);
            float run = Mathf.Clamp01((speed - 4f) / 3.5f);
            float amp = Mathf.Lerp(26f, 52f, run) * walk;
            float sw = Mathf.Sin(phase) * amp;
            float breath = Mathf.Sin(time * 1.8f);
            var p = new Pose();
            p.lgL = V(sw); p.lgR = V(-sw);
            if (drawn)
            {
                Pose n = N();
                p.arR = Vector3.Lerp(n.arR, V(-sw * 0.6f - 25f * run, -8, 8), walk); p.fR = Vector3.Lerp(n.fR, V(-70f - 25f * run), walk);
                p.arL = Vector3.Lerp(n.arL + V(breath * 1.5f), V(sw * 0.8f, 0, -8), walk); p.fL = Vector3.Lerp(n.fL, V(-30f - 40f * run), walk);
                p.body = Vector3.Lerp(n.body, V(6f * run, 0, 0), walk);
                p.head = Vector3.Lerp(n.head, V(0), walk);
                p.lgL = Vector3.Lerp(n.lgL, p.lgL, walk); p.lgR = Vector3.Lerp(n.lgR, p.lgR, walk);
            }
            else
            {
                p.arR = V(-sw * 0.8f + breath * 1.2f, 0, 6 + breath);
                p.arL = V(sw * 0.8f - breath * 1.2f, 0, -6 - breath);
                p.fR = V(-8f - 38f * run - 14f * walk); p.fL = V(-8f - 38f * run - 14f * walk);
                p.body = V(8f * run + breath * 0.6f, Mathf.Sin(phase) * 5f * walk, 0);
                p.head = V(0);
            }
            p.lift = Mathf.Abs(Mathf.Sin(phase)) * 0.045f * walk * (1f + run);
            p.pitch = 0f;
            if (guard) { p.arL = V(-88, -34, -12); p.fL = V(-78); p.arR = V(-52, -22, 10); p.fR = V(-84); p.body = V(0, -14, 0); }
            return p;
        }

        float swimPhase, climbPhase;

        Pose ClimbPose(float dt)
        {
            climbPhase += dt * 6f * PlayerController.ClimbSpd01;
            float s = Mathf.Sin(climbPhase), s2 = Mathf.Sin(climbPhase + Mathf.PI);
            var p = new Pose();
            p.arR = V(-150f + 28f * s, 0f, 12f); p.arL = V(-150f + 28f * s2, 0f, -12f);
            p.fR = V(-25f); p.fL = V(-25f);
            p.lgL = V(-35f + 32f * s2); p.lgR = V(-35f + 32f * s);
            p.body = V(0f, 0f, 0f); p.head = V(-20f, 0f, 0f);
            p.pitch = 8f; p.lift = 0f; p.lunge = 0f;
            return p;
        }

        Pose GlidePose(float t)
        {
            var p = new Pose();
            float fl = Mathf.Sin(t * 5f) * 3f;
            p.arR = V(-15f, 0f, 82f + fl); p.arL = V(-15f, 0f, -82f - fl);
            p.fR = V(-8f); p.fL = V(-8f);
            p.lgL = V(14f); p.lgR = V(10f);
            p.body = V(0f); p.head = V(-18f, 0f, 0f);
            p.pitch = 38f; p.lift = 0.05f; p.lunge = 0f;
            return p;
        }

        /// <summary>
        /// Pose di udara. Fase: tolak (0-0.14 dtk: badan memanjang, lengan terayun naik), naik, puncak (lutut ditekuk),
        /// turun, dan jatuh jauh (lengan menggapai). Saat berlari, lompatan memakai langkah lebar (satu kaki depan, satu belakang).
        /// </summary>
        Pose AirPose(float vy, float airT, float spd, float time)
        {
            var p = new Pose();
            bool run = spd > 3.2f;
            float fwd = Mathf.Clamp01((spd - 1f) / 5f);
            if (airT < 0.14f && vy > 0.5f)
            {   // tolak: badan lurus, tumit terangkat, lengan terayun ke atas
                float k = Mathf.Clamp01(airT / 0.14f);
                p.arR = V(Mathf.Lerp(20f, -150f, k), 0f, 14f); p.arL = V(Mathf.Lerp(20f, -150f, k), 0f, -14f);
                p.fR = V(-10f); p.fL = V(-10f);
                p.lgL = V(run ? -30f : -6f); p.lgR = V(run ? 22f : 10f);
                p.body = V(-6f + 10f * fwd, 0f, 0f); p.head = V(-6f, 0f, 0f);
                p.lift = 0.05f; p.pitch = 4f * fwd; p.lunge = 0.03f;
                return p;
            }
            if (vy > 1.8f)
            {   // naik
                if (run) { p.arR = V(34f, 0f, 14f); p.arL = V(-100f, 0f, -16f); p.lgL = V(-74f); p.lgR = V(32f); p.body = V(10f, 0f, 0f); p.pitch = 6f; }
                else { p.arR = V(-110f, 0f, 22f); p.arL = V(-110f, 0f, -22f); p.lgL = V(-48f); p.lgR = V(-8f); p.body = V(0f); p.pitch = 0f; }
                p.fR = V(-34f); p.fL = V(-34f); p.head = V(-8f, 0f, 0f);
            }
            else if (vy > -1.8f)
            {   // puncak: lutut ditekuk ke dada, lengan melebar
                p.arR = V(-60f, 0f, 48f); p.arL = V(-60f, 0f, -48f); p.fR = V(-24f); p.fL = V(-24f);
                p.lgL = V(run ? -62f : -56f); p.lgR = V(run ? -26f : -50f);
                p.body = V(4f + 8f * fwd, 0f, 0f); p.head = V(0f); p.pitch = 4f * fwd;
            }
            else if (vy > -9f)
            {   // turun: lengan melebar, kaki menjulur siap mendarat
                p.arR = V(-22f, 0f, 64f); p.arL = V(-22f, 0f, -64f); p.fR = V(-18f); p.fL = V(-18f);
                p.lgL = V(run ? -34f : 14f); p.lgR = V(run ? 20f : -12f);
                p.body = V(2f + 6f * fwd, 0f, 0f); p.head = V(6f, 0f, 0f); p.pitch = 2f * fwd;
            }
            else
            {   // jatuh jauh: lengan mengepak ke atas, badan condong ke depan
                float fl = Mathf.Sin(time * 14f) * 12f;
                p.arR = V(-150f + fl, 0f, 38f); p.arL = V(-150f - fl, 0f, -38f); p.fR = V(-12f); p.fL = V(-12f);
                p.lgL = V(-18f + fl * 0.6f); p.lgR = V(10f - fl * 0.6f);
                p.body = V(8f, 0f, 0f); p.head = V(-12f, 0f, 0f); p.pitch = 6f;
            }
            p.lift = 0f; p.lunge = 0f;
            return p;
        }

        // ---------------- mengendap (jongkok). Semua angka bisa disetel di sini bila pose perlu disesuaikan dengan modelmu.
        public static float SneakLift = -0.22f;      // seberapa rendah badan turun (m)
        public static float SneakLeg = -38f;         // sudut paha ke depan saat jongkok (derajat)
        public static float SneakLean = 16f;         // condong ke depan (derajat)
        public static float SneakStride = 14f;       // ayunan kaki saat berjalan mengendap

        /// <summary>Pose mengendap: badan rendah dan condong, senjata dipegang dekat badan, langkah pendek dan pelan; diam = menunggu waspada.</summary>
        static Pose SneakPose(float phase, float speed, bool drawn, float time)
        {
            float walk = Mathf.Clamp01(speed / 1.2f);
            float sw = Mathf.Sin(phase * 0.8f) * SneakStride * walk;
            float breath = Mathf.Sin(time * 1.6f);
            var p = new Pose();
            p.lgL = V(SneakLeg + sw); p.lgR = V(SneakLeg * 0.8f - sw);
            p.body = V(SneakLean + breath * 0.8f, Mathf.Sin(phase * 0.8f) * 4f * walk, 0f);
            p.head = V(-SneakLean * 0.7f + Mathf.Sin(time * 0.7f) * 4f, Mathf.Sin(time * 0.9f) * 14f * (1f - walk), 0f);   // kepala tegak dan menoleh waspada saat diam
            if (drawn)
            {
                p.arR = V(-40f - sw * 0.3f, -12f, 10f); p.fR = V(-92f);
                p.arL = V(-30f + sw * 0.3f, 0f, -14f); p.fL = V(-66f);
            }
            else
            {
                p.arR = V(-26f - sw * 0.5f, 0f, 10f); p.arL = V(-26f + sw * 0.5f, 0f, -10f);
                p.fR = V(-34f); p.fL = V(-34f);
            }
            p.lift = SneakLift - Mathf.Abs(Mathf.Sin(phase * 0.8f)) * 0.012f * walk;
            p.pitch = 6f; p.lunge = 0.1f;
            return p;
        }

        Pose LandPose(float k, float spd)
        {   // jongkok singkat saat mendarat; k 0..1. Mendarat sambil berlari: badan lebih condong dan satu kaki di depan, bukan jongkok diam.
            float fwd = Mathf.Clamp01((spd - 1f) / 5f);
            var p = new Pose();
            p.arR = V(-48f + 30f * fwd, 0f, 22f); p.arL = V(-48f - 20f * fwd, 0f, -22f); p.fR = V(-30f); p.fL = V(-30f);
            p.lgL = V((-48f - 14f * fwd) * k); p.lgR = V((-36f + 40f * fwd) * k);
            p.body = V(10f * fwd * k, 0f, 0f); p.head = V(10f * k, 0f, 0f);
            p.pitch = (14f + 10f * fwd) * k; p.lift = -0.2f * k; p.lunge = 0.05f + 0.06f * fwd;
            return p;
        }

        Pose RidePose(float t)
        {
            var p = new Pose();
            p.arR = V(-42f, 0f, 10f); p.arL = V(-42f, 0f, -10f); p.fR = V(-38f); p.fL = V(-38f);
            p.lgL = V(-82f, 0f, -14f); p.lgR = V(-82f, 0f, 14f);
            p.body = V(0f); p.head = V(0f);
            p.pitch = 4f; p.lift = 0.36f + Mathf.Abs(Mathf.Sin(t * 8f * Mathf.Clamp01(speedSm / 6f))) * 0.03f; p.lunge = 0f;
            return p;
        }

        /// <summary>Pose berenang: bergerak di permukaan = gaya bebas (badan telungkup, berguling ke sisi lengan yang menarik, kepala menoleh mengambil nafas);
        /// menyelam = gaya dada (lengan menyapu serempak, tendangan kodok, badan hampir lurus ke depan); diam = berpijak air (tegak, tangan menyapu, kaki menggayuh).</summary>
        Pose SwimPose(float dt, bool submerged)
        {
            float move = Mathf.Clamp01(speedSm / 0.9f);
            swimPhase += dt * Mathf.Lerp(2.4f, 5.2f, move) * (submerged ? 0.6f : 1f);
            float s = Mathf.Sin(swimPhase), s2 = Mathf.Sin(swimPhase + Mathf.PI);
            var p = new Pose();
            // --- gaya bebas ---
            Pose cr = new Pose();
            cr.arR = V(-95f + 75f * s, 0f, 8f); cr.arL = V(-95f + 75f * s2, 0f, -8f);
            cr.fR = V(-20f - 55f * Mathf.Max(0f, Mathf.Cos(swimPhase))); cr.fL = V(-20f - 55f * Mathf.Max(0f, Mathf.Cos(swimPhase + Mathf.PI)));
            cr.body = V(0f, Mathf.Sin(swimPhase) * 6f, Mathf.Sin(swimPhase) * 10f);            // berguling mengikuti lengan yang menarik
            cr.head = V(-48f, Mathf.Max(0f, Mathf.Sin(swimPhase * 0.5f)) * 34f, 0f);           // menoleh ambil nafas
            cr.lgL = V(Mathf.Sin(swimPhase * 2f) * 24f); cr.lgR = V(-Mathf.Sin(swimPhase * 2f) * 24f);
            cr.pitch = 74f; cr.lift = submerged ? 0.05f : 0.2f; cr.lunge = 0.1f;
            if (submerged)
            {   // gaya dada: lengan ke depan lalu menyapu ke samping bersamaan, kaki menekuk lalu menendang
                float b = Mathf.Sin(swimPhase * 0.9f), c = Mathf.Cos(swimPhase * 0.9f);
                cr.arR = V(-165f + 25f * c, -10f * b, 20f + 55f * Mathf.Max(0f, b)); cr.arL = V(-165f + 25f * c, 10f * b, -20f - 55f * Mathf.Max(0f, b));
                cr.fR = V(-12f - 30f * Mathf.Max(0f, -b)); cr.fL = cr.fR;
                cr.lgL = V(-12f - 38f * Mathf.Max(0f, -c)); cr.lgR = cr.lgL;
                cr.body = V(0f); cr.head = V(-26f + 10f * b, 0f, 0f); cr.pitch = 86f;
            }
            // --- berpijak air ---
            Pose tr = new Pose();
            tr.arR = V(-28f, 0f, 42f + 14f * s); tr.arL = V(-28f, 0f, -42f - 14f * s2);
            tr.fR = V(-40f); tr.fL = V(-40f);
            tr.body = V(0f); tr.head = V(Mathf.Sin(swimPhase * 0.5f) * 3f, 0f, 0f);
            tr.lgL = V(-28f * s); tr.lgR = V(28f * s);
            tr.pitch = 0f; tr.lift = 0.25f + Mathf.Sin(swimPhase * 0.7f) * 0.05f; tr.lunge = 0f;
            p = Pose.Lerp(tr, cr, move);
            return p;
        }

        // ---------------------------------------------------------------- instance
        PlayerAvatar avatar; CharacterRig rig; Transform pivot; float centerY;
        Clip cur = Clip.None; float t;
        Vector3 lastPos; float speedSm, phase;
        public bool Guarding;
        Pose smooth;

        public void Init(PlayerAvatar a, CharacterRig r, Transform piv, float cy)
        {
            avatar = a; rig = r; pivot = piv; centerY = cy;
            lastPos = a != null ? a.transform.position : Vector3.zero;
        }

        public void Play(Clip c) { cur = c; t = 0f; entry = smooth != null ? smooth.Copy() : null; }
        Pose entry; float exitT;
        float yawOff;
        /// <summary>Putaran badan yang tiba-tiba (hadap kamera saat menyerang) ditampilkan sebagai putaran halus: model memutar kembali ke arah baru.</summary>
        public void KickYaw(float deg) { yawOff = Mathf.Clamp(yawOff + deg, -140f, 140f); }
        public void Stop() { cur = Clip.None; }
        public bool Busy { get { return cur != Clip.None && !(clips[cur].hold && t >= clips[cur].dur); } }

        void LateUpdate()
        {
            if (rig == null || avatar == null) return;
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            Vector3 d = avatar.transform.position - lastPos; d.y = 0f; lastPos = avatar.transform.position;
            float v = Mathf.Min(d.magnitude / dt, 10f);
            speedSm = Mathf.Lerp(speedSm, v, 10f * dt);
            phase += speedSm * 2.3f * dt;

            bool drawn = avatar.Drawn;
            Pose loco = Locomotion(phase, speedSm, drawn, avatar.HasBow, Time.time, Guarding && avatar.HasShield);
            if (Stealth.Sneaking && !Guarding) loco = SneakPose(phase, speedSm, drawn, Time.time);
            Pose target = loco;
            if (cur == Clip.None)
            {
                if (PlayerController.Swimming) target = SwimPose(dt, PlayerController.Submerged);
                else if (MountSystem.Mounted) target = RidePose(Time.time);
                else if (PlayerController.Climbing) target = ClimbPose(dt);
                else if (PlayerController.Gliding) target = GlidePose(Time.time);
                else if (PlayerController.AirT > 0.05f && (PlayerController.AirT > 0.12f || PlayerController.VelY > 0.5f)) target = AirPose(PlayerController.VelY, PlayerController.AirT, speedSm, Time.time);
                else if (PlayerController.LandT > 0f) target = LandPose(Mathf.Clamp01(PlayerController.LandT / 0.25f), speedSm);
            }
            if (cur != Clip.None)
            {
                var cd = clips[cur];
                t += dt;
                if (!cd.hold && t >= cd.dur) { cur = Clip.None; exitT = 0.3f; }
                else
                {
                    target = Sample(cur, t / cd.dur);
                    const float blendIn = 0.09f;      // masuk ke aksi dari pose terakhir dengan mulus (bukan loncat ke kuda-kuda)
                    if (entry != null && t < blendIn) { float e = Mathf.Clamp01(t / blendIn); e = 1f - (1f - e) * (1f - e); target = Pose.Lerp(entry, target, e); }
                }
            }
            if (exitT > 0f) exitT -= dt;
            if (smooth == null) smooth = target.Copy();
            float k = cur != Clip.None ? 1f - Mathf.Exp(-40f * dt) : 1f - Mathf.Exp(-(exitT > 0f ? 9f : 16f) * dt);   // keluar dari aksi: kembali ke gerak biasa lebih lambat/halus
            yawOff *= Mathf.Exp(-14f * dt);
            if (Mathf.Abs(yawOff) < 0.05f) yawOff = 0f;
            smooth = Pose.Lerp(smooth, target, k);
            Apply(smooth);
            UpdateArrow();
        }

        public void ApplyPose(Pose p) { Apply(p); }

        void Apply(Pose p)
        {
            if (rig.armR) rig.armR.localRotation = Quaternion.Euler(p.arR);
            if (rig.foreR) rig.foreR.localRotation = Quaternion.Euler(p.fR);
            if (rig.armL) rig.armL.localRotation = Quaternion.Euler(p.arL);
            if (rig.foreL) rig.foreL.localRotation = Quaternion.Euler(p.fL);
            if (rig.body) rig.body.localRotation = Quaternion.Euler(p.body);
            if (rig.head) rig.head.localRotation = Quaternion.Euler(p.head);
            if (rig.legL) rig.legL.localRotation = Quaternion.Euler(p.lgL);
            if (rig.legR) rig.legR.localRotation = Quaternion.Euler(p.lgR);
            transform.localPosition = new Vector3(0f, -centerY + p.lift, p.lunge);
            if (pivot) pivot.localRotation = Quaternion.Euler(p.pitch, yawOff, 0f);
            if (rig.tail) rig.tail.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 2.1f) * 6f, Mathf.Sin(Time.time * 2.6f) * (14f + 10f * Mathf.Clamp01(speedSm)), 0f);
        }

        /// <summary>Aksi skill kustom memakai ini supaya anak panah terlihat di tangan selama menarik busur.</summary>
        public static bool HoldArrow;

        // anak panah terlihat saat busur ditarik
        void UpdateArrow()
        {
            var arrow = avatar.Arrow;
            if (arrow == null) return;
            bool show = (HoldArrow || (cur == Clip.Shoot && t / clips[Clip.Shoot].dur < 0.56f)) && avatar.BowObject != null;
            if (arrow.activeSelf != show) arrow.SetActive(show);
            if (show)
            {
                Vector3 a = rig.handR.position, bowGrip = avatar.BowObject.transform.position;
                Vector3 dir = bowGrip - a;
                if (dir.sqrMagnitude > 1e-4f) arrow.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                arrow.transform.position = a;
            }
        }
    }
}
