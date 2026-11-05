using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Efek visual sihir yang megah untuk Penyihir Api & Penyihir Tanah: lingkaran sihir berputar, pengisian tongkat, proyektil berekor,
    /// ledakan api berlapis (inti, nyala, bara, asap, bekas hangus), serpihan batu, duri batu, tiang api, bola pengorbit, dan guncangan kamera.
    /// Semua dibuat prosedural dengan shader "Sprites/Default" (jalan di Built-in dan URP); tidak butuh aset.
    /// </summary>
    public static partial class MagicFx
    {
        public static readonly Color FireHot = new Color(1f, 0.92f, 0.55f);
        public static readonly Color FireMid = new Color(1f, 0.5f, 0.12f);
        public static readonly Color FireDeep = new Color(0.75f, 0.12f, 0.05f);
        public static readonly Color RockA = new Color(0.62f, 0.48f, 0.3f);
        public static readonly Color RockB = new Color(0.42f, 0.34f, 0.26f);

        static Texture2D circleTex, crackTex;
        static Mesh coneMesh;

        // =====================================================================
        // Tekstur & mesh prosedural
        // =====================================================================
        static float Line(float d, float w) { return Mathf.Clamp01(1f - d / w); }

        static float SegDist(float px, float py, float ax, float ay, float bx, float by)
        {
            float abx = bx - ax, aby = by - ay;
            float t = Mathf.Clamp01(((px - ax) * abx + (py - ay) * aby) / (abx * abx + aby * aby));
            float dx = px - (ax + abx * t), dy = py - (ay + aby * t);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        static Texture2D CircleTex()
        {
            if (circleTex != null) return circleTex;
            const int n = 512;
            circleTex = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 2 };
            var px = new Color[n * n];
            // dua segitiga (heksagram) berjari-jari 0.8
            float R = 0.8f;
            float[][] tri = new float[6][];
            for (int i = 0; i < 6; i++)
            {
                float a0 = (i % 2 == 0 ? 0f : 60f) + (i < 3 ? 0f : 0f);
                // segitiga A: sudut 90,210,330; segitiga B: 30,150,270
            }
            float[] angA = { 90f, 210f, 330f }, angB = { 30f, 150f, 270f };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(u * u + v * v);
                    float ang = Mathf.Atan2(v, u);
                    float a = 0f;
                    a = Mathf.Max(a, Line(Mathf.Abs(d - 0.97f), 0.014f));
                    a = Mathf.Max(a, Line(Mathf.Abs(d - 0.90f), 0.010f) * 0.9f);
                    a = Mathf.Max(a, Line(Mathf.Abs(d - 0.60f), 0.010f) * 0.8f);
                    a = Mathf.Max(a, Line(Mathf.Abs(d - 0.28f), 0.012f));
                    // tanda rune di antara cincin luar
                    float tick = Mathf.Repeat(ang / (Mathf.PI * 2f) * 36f, 1f);
                    if (d > 0.915f && d < 0.96f && tick < 0.35f) a = Mathf.Max(a, 0.95f);
                    float tick2 = Mathf.Repeat(ang / (Mathf.PI * 2f) * 12f + 0.5f, 1f);
                    if (d > 0.62f && d < 0.88f && Mathf.Abs(tick2 - 0.5f) < 0.012f) a = Mathf.Max(a, 0.6f);
                    // heksagram
                    for (int t = 0; t < 2; t++)
                    {
                        float[] ang3 = t == 0 ? angA : angB;
                        for (int e = 0; e < 3; e++)
                        {
                            float a1 = ang3[e] * Mathf.Deg2Rad, a2 = ang3[(e + 1) % 3] * Mathf.Deg2Rad;
                            float dist = SegDist(u, v, Mathf.Cos(a1) * R, Mathf.Sin(a1) * R, Mathf.Cos(a2) * R, Mathf.Sin(a2) * R);
                            a = Mathf.Max(a, Line(dist, 0.012f) * 0.95f);
                        }
                    }
                    // titik rune di sudut bintang
                    for (int e = 0; e < 6; e++)
                    {
                        float a1 = (30f + e * 60f) * Mathf.Deg2Rad;
                        float dd = Mathf.Sqrt((u - Mathf.Cos(a1) * R) * (u - Mathf.Cos(a1) * R) + (v - Mathf.Sin(a1) * R) * (v - Mathf.Sin(a1) * R));
                        a = Mathf.Max(a, Line(dd, 0.045f));
                    }
                    a = Mathf.Max(a, Mathf.Clamp01(1f - d * 5f) * 0.55f);       // pendar di pusat
                    a *= Mathf.Clamp01((1f - d) * 30f);
                    px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
                }
            circleTex.SetPixels(px); circleTex.Apply();
            return circleTex;
        }

        static Texture2D CrackTex()
        {
            if (crackTex != null) return crackTex;
            const int n = 192;
            crackTex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[n * n];
            const int rays = 9;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(u * u + v * v);
                    float ang = Mathf.Atan2(v, u);
                    float a = 0f;
                    for (int i = 0; i < rays; i++)
                    {
                        float baseA = i / (float)rays * Mathf.PI * 2f + Mathf.PerlinNoise(i * 3.1f, 0.5f) * 0.6f;
                        float jag = (Mathf.PerlinNoise(d * 7f, i * 5.3f) - 0.5f) * 0.55f;
                        float dA = Mathf.Abs(Mathf.DeltaAngle((ang - baseA - jag) * Mathf.Rad2Deg, 0f)) * Mathf.Deg2Rad;
                        float arc = dA * d;
                        float reach = 0.55f + Mathf.PerlinNoise(i * 1.7f, 2.2f) * 0.45f;
                        if (d < reach && d > 0.04f) a = Mathf.Max(a, Line(arc, 0.014f * (1.2f - d / reach)) * (1f - d / reach * 0.6f));
                    }
                    a = Mathf.Max(a, Mathf.Clamp01(1f - d * 2.2f) * 0.5f);
                    a *= Mathf.Clamp01((1f - d) * 8f);
                    px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
                }
            crackTex.SetPixels(px); crackTex.Apply();
            return crackTex;
        }

        static Mesh Cone()
        {
            if (coneMesh != null) return coneMesh;
            const int seg = 8;
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            v.Add(new Vector3(0f, 1f, 0f)); c.Add(new Color(1f, 1f, 1f, 1f));
            for (int i = 0; i < seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                float jit = 0.85f + 0.3f * Mathf.PerlinNoise(i * 1.3f, 0.7f);
                v.Add(new Vector3(Mathf.Cos(a) * jit, 0f, Mathf.Sin(a) * jit));
                c.Add(new Color(0.55f, 0.55f, 0.55f, 1f));
            }
            for (int i = 0; i < seg; i++)
            {
                int a = 1 + i, b = 1 + (i + 1) % seg;
                t.Add(0); t.Add(b); t.Add(a);
                t.Add(0); t.Add(a); t.Add(b);          // dua sisi
            }
            coneMesh = new Mesh { name = "FxCone" };
            MeshSafe.Clean(v); coneMesh.SetVertices(v); coneMesh.SetColors(c); coneMesh.SetTriangles(t, 0);
            coneMesh.RecalculateBounds();
            return coneMesh;
        }

        // =====================================================================
        // Pembantu umum
        // =====================================================================
        public static Vector3 Tip()
        {
            if (PlayerAvatar.I != null) return PlayerAvatar.I.StaffTip;
            return PlayerCombat.I != null ? PlayerCombat.I.transform.position + Vector3.up * 1.5f : Vector3.zero;
        }

        static GameObject Quad(string name, Texture2D tex, Color col, Vector3 pos, Vector3 euler, float size)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            UnityEngine.Object.Destroy(q.GetComponent<Collider>());
            q.name = name;
            q.transform.position = pos;
            q.transform.rotation = Quaternion.Euler(euler);
            q.transform.localScale = Vector3.one * size;
            var mr = q.GetComponent<MeshRenderer>();
            mr.sharedMaterial = CombatFx.Mat(tex, col);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return q;
        }

        static GameObject Ball(string name, Vector3 pos, float size, Color col, Transform parent = null)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            UnityEngine.Object.Destroy(s.GetComponent<Collider>());
            s.name = name;
            if (parent != null) s.transform.SetParent(parent, false);
            s.transform.position = pos;
            s.transform.localScale = Vector3.one * size;
            var mr = s.GetComponent<MeshRenderer>();
            mr.sharedMaterial = CombatFx.Mat(null, col);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return s;
        }

        static ParticleSystem PS(Vector3 pos, int count, float lifeMin, float lifeMax, float spdMin, float spdMax, float szMin, float szMax,
                                 Color c0, Color c1, float gravity, float shapeR, bool shrink)
        {
            var go = new GameObject("FxMagicPS");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 0.5f; main.loop = false; main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(spdMin, spdMax);
            main.startSize = new ParticleSystem.MinMaxCurve(szMin, szMax);
            main.startColor = Color.white;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var em = ps.emission; em.enabled = true; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)FxQuality.N(count)) });
            var sp = ps.shape; sp.enabled = true; sp.shapeType = ParticleSystemShapeType.Sphere; sp.radius = shapeR;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(c0, 0f), new GradientColorKey(c1, 1f) },
                      new[] { new GradientAlphaKey(c0.a, 0f), new GradientAlphaKey(Mathf.Min(c0.a, c1.a) * 0.8f + 0.05f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, shrink ? AnimationCurve.Linear(0f, 1f, 1f, 0.15f) : AnimationCurve.Linear(0f, 0.55f, 1f, 1.5f));
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = CombatFx.Mat(CombatFx.Soft(), Color.white);
            ps.Play();
            return ps;
        }

        // =====================================================================
        // Lingkaran sihir
        // =====================================================================
        class CircleAnim : MonoBehaviour
        {
            public float life = 1f, age, spin = 40f, fullSize = 4f;
            public Color color = Color.white;
            public Transform inner;
            Material mat, matIn;

            void Start()
            {
                mat = GetComponent<Renderer>().material;
                if (inner != null) matIn = inner.GetComponent<Renderer>().material;
            }

            void Update()
            {
                age += Time.deltaTime;
                float t = Mathf.Clamp01(age / life);
                float grow = Mathf.Clamp01(age / 0.25f); grow = 1f - (1f - grow) * (1f - grow);
                float fade = t < 0.65f ? 1f : Mathf.Clamp01((1f - t) / 0.35f);
                transform.localScale = Vector3.one * fullSize * (0.25f + 0.75f * grow) * (t > 0.85f ? 1f + (t - 0.85f) * 1.5f : 1f);
                transform.Rotate(0f, 0f, -spin * Time.deltaTime, Space.Self);
                var c = color; c.a = color.a * fade * Mathf.Clamp01(age / 0.12f);
                if (mat != null) mat.color = c;
                if (inner != null)
                {
                    inner.Rotate(0f, 0f, spin * 2.4f * Time.deltaTime, Space.Self);
                    var ci = Color.Lerp(color, Color.white, 0.5f); ci.a = c.a * 0.9f;
                    if (matIn != null) matIn.color = ci;
                }
                if (age >= life) Destroy(gameObject);
            }
        }

        /// <summary>Lingkaran sihir rune berputar di tanah (dua lapis berlawanan arah). radius = jari-jari dunia.</summary>
        public static void MagicCircle(Vector3 pos, float radius, Color col, float life = 1f)
        {
            var q = Quad("FxMagicCircle", CircleTex(), col, pos + Vector3.up * 0.07f, new Vector3(90f, 0f, 0f), radius * 2f);
            var inner = Quad("Inner", CircleTex(), col, pos + Vector3.up * 0.09f, new Vector3(90f, 0f, 0f), 1f);
            inner.transform.SetParent(q.transform, true);
            inner.transform.localPosition = new Vector3(0f, 0f, -0.002f);
            inner.transform.localScale = Vector3.one * 0.62f;
            var a = q.AddComponent<CircleAnim>();
            a.life = life; a.fullSize = radius * 2f; a.color = col; a.inner = inner.transform;
            CombatFx.Flash(pos + Vector3.up * 0.4f, col, radius * 2.2f, 1.2f, Mathf.Min(0.5f, life * 0.6f));
        }

        /// <summary>Lingkaran sihir + pancaran partikel naik di kaki pemain saat merapal skill.</summary>
        public static void CastCircle(Vector3 feet, Color col, float radius = 2.4f, float life = 0.9f)
        {
            MagicCircle(feet, radius, col, life);
            PS(feet + Vector3.up * 0.1f, 24, 0.6f, 1.1f, 0.4f, 2.6f, 0.08f, 0.16f, Color.Lerp(col, Color.white, 0.5f), col, -0.3f, radius * 0.7f, true);
        }

        // =====================================================================
        // Pengisian tongkat
        // =====================================================================
        /// <summary>Bola energi di ujung tongkat yang membesar sambil menyerap percikan; berhenti sendiri setelah dur detik.</summary>
        public static void CastCharge(Color col, float dur = 0.35f, float size = 0.5f)
        {
            CombatFx.Host.I.StartCoroutine(ChargeRoutine(col, dur, size));
        }

        static IEnumerator ChargeRoutine(Color col, float dur, float size)
        {
            var orb = Ball("FxCharge", Tip(), 0.05f, Color.Lerp(col, Color.white, 0.6f));
            var halo = Ball("Halo", Tip(), 0.1f, new Color(col.r, col.g, col.b, 0.35f), orb.transform);
            halo.transform.localScale = Vector3.one * 2f;
            var lg = new GameObject("L").AddComponent<Light>(); lg.transform.SetParent(orb.transform, false);
            lg.type = LightType.Point; lg.color = col; lg.range = 6f; lg.intensity = 0f;
            float t = 0f, spark = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                Vector3 tip = Tip();
                orb.transform.position = tip;
                float k = Mathf.Clamp01(t / dur);
                orb.transform.localScale = Vector3.one * (0.05f + size * k);
                lg.intensity = 2.2f * k;
                spark -= Time.deltaTime;
                if (spark <= 0f)
                {
                    spark = 0.035f;
                    Vector3 off = UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(0.9f, 1.5f);
                    var s = Quad("FxSpark", CombatFx.Soft(), Color.Lerp(col, Color.white, 0.5f), tip + off, Vector3.zero, 0.14f);
                    var fx = s.AddComponent<CombatFx.FxAnim>();
                    fx.life = 0.24f; fx.billboard = true; fx.color = Color.Lerp(col, Color.white, 0.5f);
                    fx.s0 = Vector3.one * 0.16f; fx.s1 = Vector3.one * 0.03f; fx.vel = -off / 0.24f;
                }
                yield return null;
            }
            if (orb != null) UnityEngine.Object.Destroy(orb);
        }

        // =====================================================================
        // Proyektil sihir
        // =====================================================================
        class BoltMover : MonoBehaviour
        {
            public Vector3 target; public float speed; public Action onHit; public Transform spin;
            ParticleSystem trail;
            bool done;
            void Start() { trail = GetComponentInChildren<ParticleSystem>(); }
            void Update()
            {
                if (done) return;
                if (spin != null) spin.Rotate(300f * Time.deltaTime, 410f * Time.deltaTime, 0f);
                Vector3 d = target - transform.position;
                float step = speed * Time.deltaTime;
                if (d.magnitude <= step)
                {
                    transform.position = target; done = true;
                    try { if (onHit != null) onHit(); } catch (Exception ex) { Debug.LogException(ex); }   // v35z102n10: error di onHit tidak boleh membuat bolt/batu menggantung selamanya
                    foreach (var r in GetComponentsInChildren<Renderer>()) if (!(r is ParticleSystemRenderer) && !(r is TrailRenderer)) r.enabled = false;
                    var l = GetComponentInChildren<Light>(); if (l != null) l.enabled = false;
                    if (trail != null) { var em = trail.emission; em.enabled = false; }
                    Destroy(gameObject, 1f);
                    return;
                }
                transform.position += d.normalized * step;
            }
        }

        public static Color ElemCol(string e)
        {
            if (e == "storm") return new Color(0.75f, 0.85f, 1f);
            return JobInfo.ElemColor(e);
        }

        static void Hot(string e, out Color hot, out Color deep)
        {
            switch (e)
            {
                case "fire": hot = FireHot; deep = FireDeep; break;
                case "earth": hot = RockA; deep = RockB; break;
                case "water": hot = new Color(0.85f, 0.97f, 1f); deep = new Color(0.1f, 0.35f, 0.9f); break;
                case "wind": hot = Color.white; deep = new Color(0.35f, 0.9f, 0.7f); break;
                case "holy": hot = new Color(1f, 1f, 0.88f); deep = new Color(1f, 0.72f, 0.18f); break;
                case "storm": hot = Color.white; deep = new Color(0.45f, 0.55f, 1f); break;
                case "arrow": hot = new Color(0.95f, 1f, 0.8f); deep = new Color(0.4f, 0.7f, 0.3f); break;
                default: hot = Color.white; deep = new Color(0.7f, 0.75f, 0.85f); break;
            }
        }

        public static void Bolt(Vector3 origin, Vector3 target, bool fire, float size, float speed, Action onHit)
        {
            BoltE(origin, target, fire ? "fire" : "earth", size, speed, onHit);
        }

        /// <summary>Proyektil sihir berekor per elemen: api, tanah (batu berputar), air, angin, cahaya, petir.</summary>
        public static void BoltE(Vector3 origin, Vector3 target, string elem, float size, float speed, Action onHit)
        {
            Vector3 dir = target - origin;
            if (dir.sqrMagnitude < 0.01f) { if (onHit != null) onHit(); return; }
            bool fire = elem == "fire", rocky = elem == "earth";
            Color col = ElemCol(elem); Color hot, deep; Hot(elem, out hot, out deep);
            var go = new GameObject("FxBolt_" + elem);
            go.transform.position = origin;
            go.transform.rotation = Quaternion.LookRotation(dir.normalized);
            var core = Ball("Core", origin, size, hot, go.transform);
            var halo = Ball("Halo", origin, size * 2.2f, new Color(col.r, col.g, col.b, 0.32f), go.transform);
            halo.GetComponent<MeshRenderer>().sharedMaterial = CombatFx.Mat(CombatFx.Soft(), new Color(col.r, col.g, col.b, 0.5f));
            Transform spin = null;
            if (rocky || elem == "wind")
            {
                spin = new GameObject("Spin").transform; spin.SetParent(go.transform, false);
                for (int i = 0; i < 4; i++)
                {
                    var c = GameObject.CreatePrimitive(rocky ? PrimitiveType.Cube : PrimitiveType.Sphere);
                    UnityEngine.Object.Destroy(c.GetComponent<Collider>());
                    c.transform.SetParent(spin, false);
                    c.transform.localPosition = UnityEngine.Random.onUnitSphere * size * (rocky ? 0.9f : 1.5f);
                    c.transform.localRotation = UnityEngine.Random.rotation;
                    c.transform.localScale = Vector3.one * size * (rocky ? UnityEngine.Random.Range(0.28f, 0.5f) : 0.22f);
                    c.GetComponent<MeshRenderer>().sharedMaterial = (rocky ? CombatFx.Solid(null, i % 2 == 0 ? RockA : RockB) : CombatFx.Mat(null, new Color(1f, 1f, 1f, 0.8f)));
                }
            }
            var tr = go.AddComponent<TrailRenderer>();
            tr.time = fire ? 0.38f : 0.3f; tr.minVertexDistance = 0.06f;
            tr.widthCurve = new AnimationCurve(new Keyframe(0f, size * 1.2f), new Keyframe(1f, 0f));
            tr.sharedMaterial = CombatFx.Mat(CombatFx.Soft(), Color.white);
            var gr = new Gradient();
            gr.SetKeys(new[] { new GradientColorKey(hot, 0f), new GradientColorKey(col, 0.4f), new GradientColorKey(deep, 1f) },
                       new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0f, 1f) });
            tr.colorGradient = gr;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var pgo = new GameObject("Trail"); pgo.transform.SetParent(go.transform, false);
            var ps = pgo.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.playOnAwake = false; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, fire ? 0.8f : 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.25f, size * 0.7f);
            main.startColor = Color.white; main.gravityModifier = fire || elem == "holy" ? -0.2f : (elem == "water" ? 0.6f : 0.3f);
            var em = ps.emission; em.enabled = true; em.rateOverTime = elem == "holy" || elem == "storm" ? 110f : 70f;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = size * 0.5f;
            var col2 = ps.colorOverLifetime; col2.enabled = true;
            var g2 = new Gradient();
            g2.SetKeys(new[] { new GradientColorKey(hot, 0f), new GradientColorKey(deep, 1f) }, new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
            col2.color = g2;
            pgo.GetComponent<ParticleSystemRenderer>().sharedMaterial = CombatFx.Mat(CombatFx.Soft(), Color.white);
            ps.Play();
            var l = new GameObject("L").AddComponent<Light>(); l.transform.SetParent(go.transform, false);
            l.type = LightType.Point; l.color = col; l.range = 5f + size * 4f; l.intensity = 1.8f;
            var m = go.AddComponent<BoltMover>();
            m.target = target; m.speed = speed; m.onHit = onHit; m.spin = spin;
            UnityEngine.Object.Destroy(go, dir.magnitude / Mathf.Max(1f, speed) + 4f);   // pengaman: bolt apa pun hilang paling lama beberapa detik setelah semestinya tiba
        }

        /// <summary>Dampak sihir per elemen di titik pos (api/tanah memakai ledakan khusus).</summary>
        public static void ElemImpact(Vector3 pos, string elem, float radius)
        {
            switch (elem)
            {
                case "fire": Fireball(pos, radius); return;
                case "earth": RockBurst(pos, radius, 8 + Mathf.RoundToInt(radius * 2f)); return;
            }
            Color col = ElemCol(elem); Color hot, deep; Hot(elem, out hot, out deep);
            float k = Mathf.Max(0.6f, radius / 4f);
            CombatFx.ShockRing(pos, radius * 1.3f, new Color(col.r, col.g, col.b, 0.9f), 0.4f);
            CombatFx.Flash(pos + Vector3.up * 0.8f, col, radius * 3f, 2.4f, 0.35f);
            PS(pos + Vector3.up * 0.4f, Mathf.RoundToInt(26 * k), 0.4f, 0.9f, 1f * k + 0.5f, 5f * k + 1f, 0.3f * k + 0.1f, 0.8f * k + 0.2f, hot, deep, elem == "holy" ? -0.3f : 0.4f, radius * 0.3f, true);
            PS(pos + Vector3.up * 0.4f, Mathf.RoundToInt(14 * k), 0.7f, 1.4f, 2f * k + 1f, 7f * k + 2f, 0.05f, 0.12f, Color.white, col, elem == "water" ? 1.2f : 0.2f, 0.3f, true);
            if (elem == "water")
            {
                for (int i = 0; i < 5; i++)
                {
                    Vector2 r = UnityEngine.Random.insideUnitCircle * radius * 0.6f;
                    Spike(pos + new Vector3(r.x, 0f, r.y), UnityEngine.Random.Range(1.2f, 2.4f) * Mathf.Max(0.8f, k * 0.7f), 0.5f * Mathf.Max(0.8f, k * 0.6f), new Color(0.7f, 0.92f, 1f), i * 0.04f, 0.5f);
                }
            }
            else if (elem == "wind" || elem == "storm")
            {
                if (elem == "storm") CombatFx.Beam(pos + Vector3.up * 14f, pos, Color.white, 0.35f * k + 0.1f, 0.18f);
                var ring = Quad("FxWindRing", CircleTex(), new Color(col.r, col.g, col.b, 0.8f), pos + Vector3.up * 0.15f, new Vector3(90f, 0f, 0f), 1f);
                var f = ring.AddComponent<CombatFx.FxAnim>(); f.life = 0.5f; f.color = new Color(col.r, col.g, col.b, 0.8f);
                f.s0 = Vector3.one * radius * 0.6f; f.s1 = Vector3.one * radius * 2.6f; f.spin = 260f;
            }
            else if (elem == "holy")
            {
                CombatFx.Beam(pos, pos + Vector3.up * 14f, new Color(1f, 0.95f, 0.7f), 0.5f * k + 0.2f, 0.5f);
                MagicCircle(pos, radius * 1.1f, new Color(1f, 0.9f, 0.5f, 0.95f), 0.9f);
            }
            Grand(pos, elem, radius);
            ThirdPersonCamera.Shake(Mathf.Min(0.25f, 0.03f * radius), 0.2f);
        }

        /// <summary>Tornado: tabung transparan berputar dengan partikel spiral.</summary>
        public static void Tornado(Vector3 pos, float radius, float height, Color col, float life)
        {
            CombatFx.Host.I.StartCoroutine(TornadoRoutine(pos, radius, height, col, life));
        }

        static IEnumerator TornadoRoutine(Vector3 pos, float radius, float height, Color col, float life)
        {
            var parts = new List<GameObject>();
            for (int i = 0; i < 4; i++)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                UnityEngine.Object.Destroy(c.GetComponent<Collider>());
                c.name = "FxTornado";
                c.GetComponent<MeshRenderer>().sharedMaterial = CombatFx.Mat(null, new Color(col.r, col.g, col.b, 0.22f));
                parts.Add(c);
            }
            var ps = PS(pos + Vector3.up * 0.2f, 1, life, life, 0f, 0f, 0.1f, 0.2f, col, col, 0f, 0.1f, true);
            float t = 0f;
            while (t < life)
            {
                t += Time.deltaTime;
                float fade = t < 0.3f ? t / 0.3f : (t > life - 0.4f ? Mathf.Clamp01((life - t) / 0.4f) : 1f);
                for (int i = 0; i < parts.Count; i++)
                {
                    float h = height * (0.55f + i * 0.15f);
                    float rr = radius * (0.4f + i * 0.28f) * fade;
                    parts[i].transform.position = pos + Vector3.up * h * 0.5f;
                    parts[i].transform.localScale = new Vector3(rr * 2f, h * 0.5f, rr * 2f);
                    parts[i].transform.Rotate(0f, (360f + i * 160f) * Time.deltaTime, 0f);
                }
                if (Mathf.Repeat(t, 0.1f) < Time.deltaTime)
                    PS(pos + new Vector3(UnityEngine.Random.Range(-1f, 1f) * radius, 0.3f, UnityEngine.Random.Range(-1f, 1f) * radius), 3, 0.5f, 0.9f, 3f, 7f, 0.1f, 0.25f, Color.white, col, -0.6f, 0.2f, true);
                yield return null;
            }
            foreach (var p in parts) if (p != null) UnityEngine.Object.Destroy(p);
        }

        /// <summary>Tiang cahaya berwarna (versi umum FirePillar), mis. cahaya suci atau pilar air.</summary>
        public static void Column(Vector3 pos, float radius, float height, Color col, float life = 0.9f)
        {
            var outer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            UnityEngine.Object.Destroy(outer.GetComponent<Collider>());
            outer.name = "FxColumn";
            outer.transform.position = pos + Vector3.up * height * 0.5f;
            outer.GetComponent<MeshRenderer>().sharedMaterial = CombatFx.Mat(null, new Color(col.r, col.g, col.b, 0.45f));
            var fo = outer.AddComponent<CombatFx.FxAnim>(); fo.life = life; fo.color = new Color(col.r, col.g, col.b, 0.45f);
            fo.s0 = new Vector3(radius * 0.5f, height * 0.5f, radius * 0.5f); fo.s1 = new Vector3(radius * 1.6f, height * 0.5f, radius * 1.6f);
            CombatFx.Beam(pos, pos + Vector3.up * (height + 4f), Color.white, radius * 0.4f, life * 0.5f);
            PS(pos + Vector3.up * 0.2f, 30, 0.7f, 1.2f, 3f, 9f, 0.2f, 0.6f, Color.white, col, -0.1f, radius * 0.6f, true);
            CombatFx.Flash(pos + Vector3.up * 2f, col, 12f, 2.4f, 0.5f);
        }

        /// <summary>Pengganti CombatFx.Orb: warna kemerahan = api, selain itu = tanah.</summary>
        public static void Orb(Vector3 origin, Vector3 target, Color col, float size, float speed, Action onHit)
        {
            Bolt(origin, target, col.r > 0.9f && col.b < 0.4f, size * 0.55f, speed, onHit);
        }

        // =====================================================================
        // Ledakan api
        // =====================================================================
        /// <summary>Ledakan api berlapis: kilat inti, bola api mengembang, nyala, bara beterbangan, asap, bekas hangus, kilatan cahaya, guncangan.</summary>
        public static void Fireball(Vector3 pos, float radius)
        {
            float k = radius / 4f;
            // kilat inti
            var core = Quad("FxCore", CombatFx.Soft(), new Color(1f, 0.95f, 0.7f, 1f), pos + Vector3.up * 0.5f, Vector3.zero, 1f);
            var f0 = core.AddComponent<CombatFx.FxAnim>(); f0.life = 0.2f; f0.billboard = true; f0.color = new Color(1f, 0.95f, 0.7f, 1f);
            f0.s0 = Vector3.one * radius * 0.4f; f0.s1 = Vector3.one * radius * 2.4f;
            // bola api mengembang
            var sph = Ball("FxFireDome", pos + Vector3.up * 0.3f, 0.3f, new Color(1f, 0.5f, 0.12f, 0.55f));
            var f1 = sph.AddComponent<CombatFx.FxAnim>(); f1.life = 0.45f; f1.color = new Color(1f, 0.5f, 0.12f, 0.55f);
            f1.s0 = Vector3.one * 0.4f; f1.s1 = Vector3.one * radius * 1.7f;
            // nyala, bara, asap
            PS(pos + Vector3.up * 0.3f, Mathf.RoundToInt(34 * Mathf.Max(0.6f, k)), 0.4f, 0.9f, 1f * k + 0.5f, 5.5f * k + 1f, 0.7f * k + 0.3f, 1.5f * k + 0.5f, FireHot, FireDeep, -0.25f, 0.4f * radius * 0.4f, true);
            PS(pos + Vector3.up * 0.3f, Mathf.RoundToInt(22 * Mathf.Max(0.6f, k)), 0.8f, 1.6f, 3f * k + 1f, 9f * k + 2f, 0.07f, 0.16f, FireHot, FireMid, 1.1f, 0.3f, true);
            PS(pos + Vector3.up * 0.8f, Mathf.RoundToInt(10 * Mathf.Max(0.6f, k)), 1.3f, 2.4f, 0.4f, 1.6f, 1.1f * k + 0.6f, 2.1f * k + 0.8f, new Color(0.25f, 0.22f, 0.2f, 0.5f), new Color(0.1f, 0.1f, 0.1f, 0.3f), -0.12f, 0.5f, false);
            // bekas hangus
            var sc = Quad("FxScorch", CombatFx.Soft(), new Color(0.04f, 0.02f, 0.01f, 0.7f), pos + Vector3.up * 0.05f, new Vector3(90f, 0f, 0f), 1f);
            var f2 = sc.AddComponent<CombatFx.FxAnim>(); f2.life = 4f; f2.color = new Color(0.04f, 0.02f, 0.01f, 0.7f);
            f2.s0 = Vector3.one * radius * 2.2f; f2.s1 = Vector3.one * radius * 2.4f;
            CombatFx.ShockRing(pos, radius * 1.5f, new Color(1f, 0.7f, 0.3f, 0.9f), 0.45f);
            CombatFx.ShockRing(pos, radius * 0.9f, Color.white, 0.25f);
            CombatFx.Flash(pos + Vector3.up, FireMid, radius * 4f, 3f, 0.45f);
            ThirdPersonCamera.Shake(Mathf.Min(0.4f, 0.05f * radius), 0.3f);
        }

        /// <summary>Semburan api kecil (dampak bola dasar, percikan).</summary>
        public static void Sparks(Vector3 pos, Color col, float scale = 1f)
        {
            PS(pos, 14, 0.3f, 0.7f, 1f * scale, 4f * scale, 0.25f * scale, 0.6f * scale, FireHot, col, -0.2f, 0.2f, true);
            PS(pos, 10, 0.6f, 1.1f, 2f * scale, 6f * scale, 0.05f, 0.1f, Color.white, col, 1.2f, 0.15f, true);
            CombatFx.Flash(pos, col, 6f * scale, 1.8f, 0.2f);
        }

        // =====================================================================
        // Batu & tanah
        // =====================================================================
        class Debris : MonoBehaviour
        {
            public Vector3 vel; public Vector3 spin; public float groundY, life = 1.4f, age;
            void Update()
            {
                age += Time.deltaTime;
                vel += Vector3.down * 16f * Time.deltaTime;
                transform.position += vel * Time.deltaTime;
                transform.Rotate(spin * Time.deltaTime);
                if (transform.position.y <= groundY && vel.y < 0f) { transform.position = new Vector3(transform.position.x, groundY, transform.position.z); vel = Vector3.zero; spin = Vector3.zero; }
                float shrink = Mathf.Clamp01((life - age) / 0.35f);
                if (age > life - 0.35f) transform.localScale = baseScale * shrink;
                if (age >= life) Destroy(gameObject);
            }
            Vector3 baseScale;
            void Start() { baseScale = transform.localScale; }
        }

        /// <summary>Serpihan batu beterbangan lalu jatuh; dibuat dari kubus acak.</summary>
        public static void Debri(Vector3 pos, float radius, int count, float power = 1f)
        {
            count = Mathf.Min(count, 18); power = Mathf.Min(power, 1.3f);       // batasi: skill besar tidak boleh menyebar kubus raksasa
            for (int i = 0; i < count; i++)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                UnityEngine.Object.Destroy(c.GetComponent<Collider>());
                c.name = "FxDebris";
                float s = UnityEngine.Random.Range(0.1f, 0.26f) * Mathf.Clamp(radius / 3f, 0.6f, 1.3f);
                Vector2 r = UnityEngine.Random.insideUnitCircle * radius * 0.35f;
                c.transform.position = pos + new Vector3(r.x, 0.2f, r.y);
                c.transform.rotation = UnityEngine.Random.rotation;
                c.transform.localScale = new Vector3(s, s * UnityEngine.Random.Range(0.6f, 1.3f), s * UnityEngine.Random.Range(0.7f, 1.2f));
                var mr = c.GetComponent<MeshRenderer>();
                mr.sharedMaterial = CombatFx.Solid(null, Color.Lerp(RockA, RockB, UnityEngine.Random.value));
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var d = c.AddComponent<Debris>();
                Vector3 outDir = new Vector3(r.x, 0f, r.y).normalized;
                if (outDir.sqrMagnitude < 0.1f) outDir = UnityEngine.Random.insideUnitSphere; outDir.y = 0f;
                d.vel = outDir.normalized * UnityEngine.Random.Range(1.5f, 5.5f) * power + Vector3.up * UnityEngine.Random.Range(4f, 10f) * power;
                d.spin = UnityEngine.Random.insideUnitSphere * 540f;
                d.groundY = pos.y; d.life = UnityEngine.Random.Range(1.2f, 1.8f);
                UnityEngine.Object.Destroy(c, d.life + 1.5f);
            }
        }

        /// <summary>Hantaman batu: serpihan, awan debu, retakan tanah, cincin kejut, guncangan.</summary>
        public static void RockBurst(Vector3 pos, float radius, int chunks = 14)
        {
            Debri(pos, radius, chunks);
            CombatFx.Dust(pos, 18);
            PS(pos + Vector3.up * 0.3f, 14, 0.8f, 1.5f, 0.6f, 2.6f, 0.9f, 1.8f, new Color(0.7f, 0.62f, 0.48f, 0.5f), new Color(0.5f, 0.45f, 0.38f, 0.2f), -0.05f, radius * 0.4f, false);
            Crack(pos, radius * 1.15f, 3.5f);
            CombatFx.ShockRing(pos, radius * 1.3f, new Color(0.85f, 0.7f, 0.45f, 0.9f), 0.4f);
            CombatFx.Flash(pos + Vector3.up * 0.5f, new Color(1f, 0.85f, 0.55f), radius * 2.5f, 1.4f, 0.25f);
            ThirdPersonCamera.Shake(Mathf.Min(0.45f, 0.06f * radius), 0.35f);
        }

        /// <summary>Retakan tanah memancar yang memudar perlahan.</summary>
        public static void Crack(Vector3 pos, float radius, float life = 3f)
        {
            var q = Quad("FxCrack", CrackTex(), new Color(0.12f, 0.08f, 0.05f, 0.9f), pos + Vector3.up * 0.05f, new Vector3(90f, UnityEngine.Random.Range(0f, 360f), 0f), 1f);
            var f = q.AddComponent<CombatFx.FxAnim>(); f.life = life; f.color = new Color(0.12f, 0.08f, 0.05f, 0.9f);
            f.s0 = Vector3.one * radius * 1.7f; f.s1 = Vector3.one * radius * 2f;
        }

        class SpikeAnim : MonoBehaviour
        {
            public float height, rise = 0.14f, hold = 0.55f, age, delay;
            public float baseR;
            Vector3 baseScale;
            void Start() { transform.localScale = new Vector3(baseR, 0.001f, baseR); }
            void Update()
            {
                age += Time.deltaTime;
                float a = age - delay;
                if (a < 0f) { transform.localScale = new Vector3(baseR, 0.001f, baseR); return; }
                float h;
                if (a < rise) { float t = a / rise; h = height * (1f - (1f - t) * (1f - t)); }
                else if (a < rise + hold) h = height;
                else { float t = (a - rise - hold) / 0.3f; h = height * (1f - Mathf.Clamp01(t)); if (t >= 1f) Destroy(gameObject); }
                transform.localScale = new Vector3(baseR, Mathf.Max(0.001f, h), baseR);
            }
        }

        /// <summary>Duri batu runcing yang menyembul dari tanah lalu tenggelam. delay = jeda sebelum muncul.</summary>
        public static void Spike(Vector3 pos, float height, float radius, Color col, float delay = 0f, float hold = 0.55f)
        {
            var go = new GameObject("FxSpike");
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(-10f, 10f));
            go.AddComponent<MeshFilter>().sharedMesh = Cone();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = CombatFx.Mat(null, col);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var s = go.AddComponent<SpikeAnim>();
            s.height = height; s.baseR = radius; s.delay = delay; s.hold = hold;
            UnityEngine.Object.Destroy(go, delay + hold + 2f);
            if (delay <= 0f) { Debri(pos, radius * 2f, 4, 0.7f); CombatFx.Dust(pos, 4); }
        }

        // =====================================================================
        // Tiang api
        // =====================================================================
        /// <summary>Tiang api raksasa: tabung transparan berlapis, partikel spiral, cincin dasar, lingkaran sihir, kilatan, guncangan.</summary>
        public static void FirePillar(Vector3 pos, float radius, float height)
        {
            var outer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            UnityEngine.Object.Destroy(outer.GetComponent<Collider>());
            outer.name = "FxPillar";
            outer.transform.position = pos + Vector3.up * height * 0.5f;
            outer.GetComponent<MeshRenderer>().sharedMaterial = CombatFx.Mat(null, new Color(1f, 0.45f, 0.1f, 0.5f));
            var fo = outer.AddComponent<CombatFx.FxAnim>(); fo.life = 0.95f; fo.color = new Color(1f, 0.45f, 0.1f, 0.5f);
            fo.s0 = new Vector3(radius * 0.4f, height * 0.5f, radius * 0.4f); fo.s1 = new Vector3(radius * 1.9f, height * 0.5f, radius * 1.9f); fo.spin = 240f;
            var inner = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            UnityEngine.Object.Destroy(inner.GetComponent<Collider>());
            inner.name = "FxPillarCore";
            inner.transform.position = pos + Vector3.up * height * 0.5f;
            inner.GetComponent<MeshRenderer>().sharedMaterial = CombatFx.Mat(null, new Color(1f, 0.95f, 0.7f, 0.85f));
            var fi = inner.AddComponent<CombatFx.FxAnim>(); fi.life = 0.7f; fi.color = new Color(1f, 0.95f, 0.7f, 0.85f);
            fi.s0 = new Vector3(radius * 0.2f, height * 0.5f, radius * 0.2f); fi.s1 = new Vector3(radius * 0.9f, height * 0.5f, radius * 0.9f);
            // partikel menjulang
            var ps = PS(pos + Vector3.up * 0.2f, 60, 0.7f, 1.3f, 6f, 14f, 0.5f, 1.2f, FireHot, FireDeep, -0.1f, radius * 0.6f, true);
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = radius * 0.7f;
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
            vel.orbitalY = new ParticleSystem.MinMaxCurve(4f);
            PS(pos + Vector3.up * 0.4f, 22, 1f, 1.8f, 3f, 9f, 0.06f, 0.14f, FireHot, FireMid, 0.2f, radius * 0.5f, true);
            MagicCircle(pos, radius * 1.6f, new Color(1f, 0.5f, 0.15f, 0.95f), 1.2f);
            CombatFx.ShockRing(pos, radius * 2.4f, new Color(1f, 0.7f, 0.3f, 0.9f), 0.5f);
            CombatFx.Flash(pos + Vector3.up * 3f, FireMid, 18f, 3.4f, 0.7f);
            CombatFx.Beam(pos, pos + Vector3.up * (height + 6f), Color.white, radius * 0.5f, 0.35f);
            ThirdPersonCamera.Shake(0.3f, 0.45f);
        }

        // =====================================================================
        // Bola pengorbit (perisai api / batu)
        // =====================================================================
        public static void Orbit(Transform center, int count, float radius, float height, bool fire, float secs, float rpm = 0.8f)
        {
            CombatFx.Host.I.StartCoroutine(OrbitRoutine(center, count, radius, height, fire, secs, rpm));
        }

        static IEnumerator OrbitRoutine(Transform center, int count, float radius, float height, bool fire, float secs, float rpm)
        {
            var objs = new List<GameObject>();
            Color col = fire ? CombatFx.FireCol : CombatFx.EarthCol;
            for (int i = 0; i < count; i++)
            {
                GameObject o;
                if (fire)
                {
                    o = Ball("FxOrbitFlame", center.position, 0.38f, FireHot);
                    var h = Ball("Halo", center.position, 0.9f, new Color(col.r, col.g, col.b, 0.35f), o.transform);
                    h.GetComponent<MeshRenderer>().sharedMaterial = CombatFx.Mat(CombatFx.Soft(), new Color(col.r, col.g, col.b, 0.55f));
                    var l = new GameObject("L").AddComponent<Light>(); l.transform.SetParent(o.transform, false);
                    l.type = LightType.Point; l.color = col; l.range = 5f; l.intensity = 1.2f;
                }
                else
                {
                    o = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    UnityEngine.Object.Destroy(o.GetComponent<Collider>());
                    o.name = "FxOrbitRock";
                    o.transform.localScale = new Vector3(0.5f, 0.42f, 0.46f);
                    o.GetComponent<MeshRenderer>().sharedMaterial = CombatFx.Solid(null, i % 2 == 0 ? RockA : RockB);
                }
                UnityEngine.Object.Destroy(o, secs + 1.5f);      // v35z102n15: pengaman; batu pasti hilang walau coroutine terhenti
                objs.Add(o);
            }
            float t = 0f, emit = 0f;
            try
            {
            while (t < secs && center != null)
            {
                t += Time.deltaTime;
                float fade = t < 0.25f ? t / 0.25f : (t > secs - 0.35f ? Mathf.Clamp01((secs - t) / 0.35f) : 1f);
                for (int i = 0; i < objs.Count; i++)
                {
                    if (objs[i] == null) continue;
                    float a = (t * rpm * Mathf.PI * 2f) + i * Mathf.PI * 2f / objs.Count;
                    float y = height + Mathf.Sin(t * 3f + i) * 0.15f;
                    objs[i].transform.position = center.position + new Vector3(Mathf.Cos(a) * radius, y - 0.9f, Mathf.Sin(a) * radius);
                    float baseS = fire ? 0.38f : 0.5f;
                    if (fire) objs[i].transform.localScale = Vector3.one * baseS * fade;
                    else { objs[i].transform.localScale = new Vector3(0.5f, 0.42f, 0.46f) * fade; objs[i].transform.Rotate(120f * Time.deltaTime, 200f * Time.deltaTime, 0f); }
                }
                emit -= Time.deltaTime;
                if (emit <= 0f && objs.Count > 0)
                {
                    emit = 0.12f;
                    var o = objs[UnityEngine.Random.Range(0, objs.Count)];
                    if (o != null)
                    {
                        if (fire) PS(o.transform.position, 4, 0.3f, 0.6f, 0.2f, 1.2f, 0.2f, 0.4f, FireHot, FireDeep, -0.3f, 0.1f, true);
                        else CombatFx.Dust(o.transform.position - Vector3.up * 0.2f, 2);
                    }
                }
                yield return null;
            }
            }
            finally
            {
                foreach (var o in objs) if (o != null) UnityEngine.Object.Destroy(o);     // juga jalan bila coroutine dihentikan / error
            }
        }
    }
}
