using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n72 "Poles Visual": perbaikan tampilan yang berjalan sendiri setiap scene dimuat (TIDAK perlu build ulang scene).
    ///
    /// 1) Papan gerbang desa. Tulisan "terpotong" karena palang kayu di belakang papan setebal ±0,17 m, sedangkan teks hanya 0,12 m dari papan
    ///    (baris atas tertanam di dalam palang), dan teks dua baris (±1,8 m) lebih tinggi daripada papan (1,5 m). Teks digeser ke depan palang,
    ///    diperkecil secukupnya agar muat di papan, dan diberi bayangan gelap supaya terbaca di atas kayu.
    /// 2) Penanda interaksi. Silinder kuning pipih (2–3,5 m) di tiap titik interaksi tampak seperti "oval kuning" besar dari kamera miring.
    ///    Silinder disembunyikan; sebagai gantinya cincin emas tipis yang menyala halus hanya saat pemain mendekat.
    /// 3) Lampu jalan. Saat malam tiap lampu diberi halo hangat + kolam cahaya di tanah. Seluruh lampu dipegang oleh DUA sistem partikel
    ///    (satu halo, satu kolam), jadi biayanya dua draw call berapa pun jumlah lampunya.
    ///
    /// Semua langkah dibungkus try/catch dan idempoten. Matikan dengan PlayerPrefs "hg_polish" = 0.
    /// </summary>
    public class VisualPolish : MonoBehaviour
    {
        static VisualPolish I;
        static Transform player; static float playerT = -99f;

        public static bool On { get { return PlayerPrefs.GetInt("hg_polish", 1) == 1; } }
        public static void Toggle() { PlayerPrefs.SetInt("hg_polish", On ? 0 : 1); }

        /// <summary>Transform pemain (dipakai cincin penanda); dicari ulang paling cepat tiap 1,5 detik.</summary>
        public static Transform Player()
        {
            if (player == null && Time.unscaledTime - playerT > 1.5f)
            {
                playerT = Time.unscaledTime;
                var pc = FindFirstObjectByType<PlayerController>();
                if (pc != null) player = pc.transform;
            }
            return player;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("VisualPolish");
            DontDestroyOnLoad(go);
            I = go.AddComponent<VisualPolish>();
        }

        void OnEnable() { SceneManager.sceneLoaded += Loaded; }
        void OnDisable() { SceneManager.sceneLoaded -= Loaded; }
        void Start() { Kick(); }

        Coroutine run;
        void Loaded(Scene s, LoadSceneMode m)
        {
            if (m == LoadSceneMode.Single) ResetState();
            Kick();
        }

        void Kick()
        {
            if (run != null) StopCoroutine(run);
            run = StartCoroutine(Pass());
        }

        void ResetState()
        {
            fitted.Clear(); ringed.Clear(); lamps.Clear(); lastNf = -1f;
            if (halo != null) halo.Clear();
            if (pool != null) pool.Clear();
        }

        // ------------------------------------------------------------------ jadwal
        IEnumerator Pass()
        {
            float[] at = { 0.7f, 2.0f, 5.0f, 12.0f };
            float prev = 0f;
            for (int i = 0; i < at.Length; i++)
            {
                yield return new WaitForSecondsRealtime(at[i] - prev);
                prev = at[i];
                if (!On) continue;
                int a = 0, b = 0;
                try { a = FitSigns(); } catch (System.Exception e) { Warn("papan", e); }
                try { b = SoftMarkers(); } catch (System.Exception e) { Warn("penanda", e); }
                yield return null;
                if (i == 0 || i == 2) yield return StartCoroutine(ScanLamps());
                if (a + b > 0) Debug.Log("[HG-Polish] pass " + i + ": papan=" + a + " penanda=" + b + " lampu(total)=" + lamps.Count);
            }
        }

        static void Warn(string what, System.Exception e) { Debug.LogWarning("[HG-Polish] " + what + " dilewati: " + e.Message); }

        float markT = 20f;
        void Update()
        {
            if (!On) return;
            markT -= Time.unscaledDeltaTime;
            if (markT <= 0f) { markT = 20f; try { SoftMarkers(); } catch { } }
            fadeT -= Time.unscaledDeltaTime;
            if (fadeT <= 0f) { fadeT = 0.3f; try { FadeLamps(); } catch { } }
        }

        static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        // ------------------------------------------------------------------ 1) papan gerbang
        static readonly string[] SignKeys = { "GERBANG UTARA", "GERBANG TIMUR", "HUTAN PEMULA", "ASHWATCH\n", "DESA PEMULA" };
        readonly HashSet<int> fitted = new HashSet<int>();

        int FitSigns()
        {
            int n = 0;
            var all = FindObjectsByType<TextMesh>(FindObjectsSortMode.None);
            for (int q = 0; q < all.Length; q++)
            {
                var tm = all[q];
                if (tm == null) continue;
                string t = tm.text;
                if (string.IsNullOrEmpty(t) || t.IndexOf('\n') < 0) continue;
                bool key = false;
                for (int i = 0; i < SignKeys.Length; i++) if (t.StartsWith(SignKeys[i], System.StringComparison.Ordinal)) { key = true; break; }
                if (!key) continue;
                int id = tm.GetInstanceID();
                if (fitted.Contains(id)) continue;
                var mr = tm.GetComponent<MeshRenderer>();
                if (mr == null) continue;
                Bounds b = mr.bounds;
                if (b.size.sqrMagnitude < 0.01f) continue;                 // mesh teks belum dibangun: coba lagi di pass berikut
                Transform tr = tm.transform;
                Vector3 right = tr.right;
                float w = Mathf.Abs(b.size.x * right.x) + Mathf.Abs(b.size.z * right.z);
                float h = b.size.y;
                if (w < 0.1f || h < 0.1f) continue;

                // papan: lebar = halfOpen*1.5 (gerbang besar 5,2 → 7,8 m; gerbang kecil 4,2 → 6,3 m), tinggi 1,5 m
                float boardW = (t.StartsWith("GERBANG", System.StringComparison.Ordinal) ? 5.2f : 4.2f) * 1.5f;
                float k = Mathf.Min(1f, Mathf.Min(boardW * 0.88f / w, 1.30f / h));

                tr.position += -tr.forward * 0.11f;                         // 0,12 → ±0,23 m dari bidang papan: di depan palang kayu (±0,17 m)
                tr.localScale = tr.localScale * k;
                fitted.Add(id);

                // bayangan gelap (kopian teks sedikit ke kanan-bawah dan ke belakang)
                var sh = Instantiate(tm.gameObject, tr.parent, false);
                sh.name = "SignShadow";
                fitted.Add(sh.GetInstanceID());
                var stm = sh.GetComponent<TextMesh>();
                if (stm != null) stm.color = new Color(0.10f, 0.06f, 0.02f, 0.92f);
                sh.transform.position += tr.TransformVector(new Vector3(0.03f, -0.03f, 0.02f));
                n++;
            }
            return n;
        }

        // ------------------------------------------------------------------ 2) penanda interaksi → cincin tipis
        readonly HashSet<int> ringed = new HashSet<int>();

        int SoftMarkers()
        {
            int n = 0;
            var arr = Interactable.All.ToArray();
            for (int i = 0; i < arr.Length; i++)
            {
                var it = arr[i];
                if (it == null) continue;
                Transform mk = it.transform.Find("Marker");
                if (mk == null) continue;
                int id = mk.GetInstanceID();
                if (!ringed.Add(id)) continue;
                var mr = mk.GetComponent<MeshRenderer>();
                if (mr == null || !mr.enabled) continue;
                Vector3 ls = mk.lossyScale;
                if (ls.y > 0.25f || ls.x < 1f || ls.x > 6f) continue;      // hanya silinder pipih berdiameter 1–6 m
                Color col = new Color(1f, 0.82f, 0.38f);
                var sm = mr.sharedMaterial;
                if (sm != null)
                {
                    if (sm.HasProperty("_BaseColor")) col = sm.GetColor("_BaseColor");
                    else if (sm.HasProperty("_Color")) col = sm.GetColor("_Color");
                }
                col = Color.Lerp(col, Color.white, 0.2f); col.a = 1f;
                mr.enabled = false;
                var go = new GameObject("MarkerRing");
                go.transform.position = mk.position + Vector3.down * 0.05f;
                go.AddComponent<MarkerRing>().Init(it, ls.x, col);
                n++;
            }
            return n;
        }

        // ------------------------------------------------------------------ 3) lampu malam
        readonly HashSet<int> lamps = new HashSet<int>();
        ParticleSystem halo, pool;
        ParticleSystem.Particle[] buf = new ParticleSystem.Particle[1024];
        float fadeT, lastNf = -1f;

        IEnumerator ScanLamps()
        {
            var arr = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            int added = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] != null) added += TryLamp(arr[i]);
                if ((i & 255) == 255) yield return null;
            }
            if (added > 0) lastNf = -1f;
        }

        int TryLamp(MeshRenderer r)
        {
            try
            {
                string nm = r.name;
                if (nm.Length < 4 || nm[0] != 'L' || nm[1] != 'a' || nm[2] != 'm' || nm[3] != 'p') return 0;
                int id = r.GetInstanceID();
                if (lamps.Contains(id)) return 0;
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) return 0;
                var mesh = mf.sharedMesh;
                var mats = r.sharedMaterials;
                int gi = -1;
                for (int i = 0; i < mats.Length && i < mesh.subMeshCount; i++)
                    if (mats[i] != null && mats[i].name.IndexOf("glow", System.StringComparison.OrdinalIgnoreCase) >= 0) { gi = i; break; }
                if (gi < 0) return 0;
                lamps.Add(id);

                Vector3 head = r.transform.TransformPoint(mesh.GetSubMesh(gi).bounds.center);
                float s = Mathf.Max(0.01f, r.transform.lossyScale.x);

                // warna halo mengikuti material "Glow" lampu itu sendiri (jingga hangat untuk lentera, biru untuk kristal)
                Color gc = new Color(1f, 0.72f, 0.38f);
                try
                {
                    var gm = mats[gi];
                    if (gm.HasProperty("_EmissionColor") && Mathf.Max(gm.GetColor("_EmissionColor").r, Mathf.Max(gm.GetColor("_EmissionColor").g, gm.GetColor("_EmissionColor").b)) > 0.05f) gc = gm.GetColor("_EmissionColor");
                    else if (gm.HasProperty("_BaseColor")) gc = gm.GetColor("_BaseColor");
                    else if (gm.HasProperty("_Color")) gc = gm.GetColor("_Color");
                }
                catch { }
                float mxc = Mathf.Max(0.001f, Mathf.Max(gc.r, Mathf.Max(gc.g, gc.b)));
                gc = Color.Lerp(new Color(gc.r / mxc, gc.g / mxc, gc.b / mxc, 1f), Color.white, 0.2f);
                Color32 c32 = gc; c32.a = 0;

                EnsureLampSystems();
                var ep = new ParticleSystem.EmitParams();
                ep.velocity = Vector3.zero; ep.startLifetime = 1000000f; ep.startColor = c32;
                ep.position = head; ep.startSize = 2.6f * s;
                halo.Emit(ep, 1);
                ep.position = new Vector3(head.x, r.transform.position.y + 0.07f, head.z); ep.startSize = 7f * s;
                pool.Emit(ep, 1);
                return 1;
            }
            catch { return 0; }
        }

        void EnsureLampSystems()
        {
            if (halo != null && pool != null) return;
            halo = MakeSystem("LampHalo", CombatFx.Mat(CombatFx.Soft(), Color.white), ParticleSystemRenderMode.Billboard);
            pool = MakeSystem("LampPool", CombatFx.Mat(CombatFx.Soft(), Color.white), ParticleSystemRenderMode.HorizontalBillboard);
        }

        ParticleSystem MakeSystem(string nm, Material mat, ParticleSystemRenderMode mode)
        {
            var go = new GameObject(nm);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 1000000f; main.startSpeed = 0f; main.startSize = 2.6f;
            main.gravityModifier = 0f; main.maxParticles = 1024;
            var em = ps.emission; em.enabled = true; em.rateOverTime = 0f;
            var sh = ps.shape; sh.enabled = false;
            var pr = go.GetComponent<ParticleSystemRenderer>();
            pr.sharedMaterial = mat; pr.renderMode = mode;
            pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; pr.receiveShadows = false;
            pr.maxParticleSize = 10f;                                     // jangan menciut saat kamera mendekat (kolam 7 m)
            ps.Play();
            return ps;
        }

        static float Night()
        {
            var gs = GameState.I;
            if (gs == null) return 0f;
            float t = gs.timeOfDay;
            if (t >= 12f) return Smooth(17.0f, 19.0f, t);               // senja → malam
            return 1f - Smooth(5.0f, 7.0f, t);                          // dini hari → pagi
        }

        void FadeLamps()
        {
            if (halo == null || pool == null) return;
            float nf = Night();
            bool dark = nf >= 0.01f;
            var hr = halo.GetComponent<ParticleSystemRenderer>(); if (hr != null && hr.enabled != dark) hr.enabled = dark;
            var pr = pool.GetComponent<ParticleSystemRenderer>(); if (pr != null && pr.enabled != dark) pr.enabled = dark;
            if (!dark && lastNf < 0.01f && lastNf >= 0f) return;        // siang: tidak ada yang perlu diperbarui
            lastNf = nf;
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 cp = cam.transform.position;
            FadeSystem(halo, nf, cp, 0.55f, 50f, 95f, 5f);
            FadeSystem(pool, nf, cp, 0.20f, 30f, 62f, 3f);
        }

        void FadeSystem(ParticleSystem ps, float nf, Vector3 cp, float maxA, float d0, float d1, float nearD)
        {
            int n = ps.GetParticles(buf);
            for (int i = 0; i < n; i++)
            {
                float d = Vector3.Distance(buf[i].position, cp);
                float a = nf * (1f - Smooth(d0, d1, d)) * Smooth(nearD * 0.4f, nearD, d);   // memudar di kejauhan, dan saat kamera menempel ke lampu
                Color c = buf[i].startColor;                              // RGB tetap (warna lampu), hanya alfa yang diubah
                c.a = a * maxA;
                buf[i].startColor = c;
            }
            ps.SetParticles(buf, n);
        }
    }

    /// <summary>Cincin penanda interaksi: tipis, berpendar halus, hanya terlihat saat pemain di sekitar titik interaksi (pengganti "oval kuning").</summary>
    public class MarkerRing : MonoBehaviour
    {
        Interactable it; MeshRenderer mr; Material mat; Color col; float phase;

        public void Init(Interactable owner, float diameter, Color c)
        {
            it = owner; col = c; phase = Random.value * 6.28f;
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var cd = q.GetComponent<Collider>(); if (cd != null) Destroy(cd);
            q.name = "RingQuad";
            q.transform.SetParent(transform, false);
            q.transform.localPosition = Vector3.zero;
            q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            q.transform.localScale = new Vector3(diameter, diameter, 1f);
            mr = q.GetComponent<MeshRenderer>();
            mat = CombatFx.Mat(CombatFx.Ring(), new Color(c.r, c.g, c.b, 0f));
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            mr.enabled = false;
        }

        void Update()
        {
            if (it == null) { Destroy(gameObject); return; }
            if (mr == null || mat == null) return;
            if (!it.isActiveAndEnabled) { mr.enabled = false; return; }
            Transform p = VisualPolish.Player();
            Vector3 pp = p != null ? p.position : (Camera.main != null ? Camera.main.transform.position : transform.position);
            Vector3 d = pp - transform.position; d.y = 0f;
            float dist = d.magnitude;
            float near = it.radius + 2f;
            float t = Mathf.Clamp01((dist - near) / 12f);
            float a = 1f - t * t * (3f - 2f * t);
            if (a < 0.01f) { mr.enabled = false; return; }
            float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 2.2f + phase);
            Color c = col; c.a = a * 0.6f * pulse;
            mat.color = c;
            mr.enabled = true;
        }

        void OnDestroy() { if (mat != null) Destroy(mat); }
    }
}
