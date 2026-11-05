using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// PROLOG (v35z102n77): latar cerita sebelum bab 1, dibawakan sebagai "arsip Mira Talley" lewat 15 adegan 3D bernarasi.
    /// Panggung dibangun dari primitif di langit tinggi (y = 3000) pada scene yang sedang aktif, kamera dikendalikan sistem Cutscene
    /// (letterbox, subtitle, bisa dilewati E / Esc / klik). Tidak butuh aset atau rebuild scene; semuanya dibersihkan saat selesai.
    /// Hanya sekali per save (stat cs_prolog), dimulai dari Cutscene.LateUpdate tepat sebelum intro Desa Pemula.
    /// Isi mengikuti lore asli: Tujuh, Jeda, retakan, empat wadah (hanya samar, rahasia rinci tetap di Arc 2-4), Sang Perintis,
    /// lalu Malam Tanpa Nama (pembuka Kronik). Matikan lewat PlayerPrefs "hg_prolog" = 0.
    /// </summary>
    public static class Prologue
    {
        public const string StatKey = "cs_prolog";
        static PrologueStage stage;

        public static bool Enabled
        {
            get { return PlayerPrefs.GetInt("hg_prolog", 1) == 1; }
            set { PlayerPrefs.SetInt("hg_prolog", value ? 1 : 0); }
        }

        public static bool Seen { get { var g = GameState.I; return g != null && g.Stat(StatKey) != 0; } }

        /// <summary>Memutar prolog. then dipanggil tepat sekali: setelah selesai, dilewati, atau bila prolog tidak bisa diputar.</summary>
        public static void Play(System.Action then) { Play(then, false); }

        /// <summary>Putar ulang dari Arsip Lore (mengabaikan pengaturan hg_prolog).</summary>
        public static void Replay() { Play(null, true); }

        static void Play(System.Action then, bool force)
        {
            var gs = GameState.I; var cam = Camera.main;
            if (gs == null || cam == null || (!force && !Enabled) || Cutscene.Active || stage != null) { if (then != null) then(); return; }
            gs.stats[StatKey] = 1;
            try
            {
                var go = new GameObject("PrologueStage");
                stage = go.AddComponent<PrologueStage>();
                var shots = stage.Build(cam);
                Cutscene.Play(shots, () => End(then));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Prolog] tidak bisa diputar: " + e.Message);
                End(then);
            }
        }

        static void End(System.Action then)
        {
            if (stage != null) { var s = stage; stage = null; s.Teardown(); Object.Destroy(s.gameObject); }
            UnlockArchive();
            if (then != null) then();
        }

        /// <summary>Isi prolog juga bisa dibaca ulang di Arsip Lore (kategori "Prolog").</summary>
        static void UnlockArchive()
        {
            bool any = false;
            foreach (var id in new[] { "pro_00", "pro_01", "pro_02" })
            {
                if (LoreDB.Get(id) == null || LoreDB.Has(id)) continue;
                LoreDB.Unlock(id, false); any = true;
            }
            if (any) GameState.Say("Arsip Prolog ditambahkan ke Arsip Lore.");
        }
    }

    /// <summary>Panggung prolog: objek prosedural + animasi berbasis waktu. Dibuat dan dihancurkan oleh Prologue.</summary>
    public class PrologueStage : MonoBehaviour
    {
        // durasi tiap shot (detik) dan teks narasinya; indeks shot = indeks adegan di bawah
        static readonly float[] D = { 6.5f, 6.0f, 5.5f, 6.5f, 7.0f, 6.5f, 7.0f, 6.5f, 7.5f, 6.0f, 7.5f, 6.5f, 6.5f, 6.5f, 6.0f };
        static readonly string[] Cap =
        {
            "Dari arsip Mira Talley, Guild Ashwatch.\nSebelum ada manusia, sebelum ada nama untuk tanah, laut, atau langit, hanya ada Tujuh.",
            "Enam dari mereka membangun Surga dan berbagi cahaya yang sama. Yang ketujuh, yang paling muda dan paling bersinar, tidak.",
            "Ia diturunkan ke dalam gelap. Sendirian. Selamanya.",
            "Kitab Enam Cahaya hanya menyebut enam nama. Di tempat nama ketujuh, tujuh napas dibiarkan kosong. Itulah Jeda.",
            "Kitab bercerita bahwa Sang Sulung menyingkirkan saudara-saudaranya. Arsip Guild bercerita bahwa yang ketujuh dijatuhkan. Kedua cerita sepakat: namanya tak boleh ditulis.",
            "Dari kesepian itu lahir sesuatu yang mencari jalan keluar: sebuah retakan, tertua di antara dua dunia, merekah pelan di bawah tanah yang kelak dinamai Ashwatch.",
            "Empat wadah ditempa dari kehendaknya, lalu ditidurkan. Mereka menunggu manusia yang cukup berani menggali terlalu dalam.",
            "Ribuan tahun berlalu. Di tepi retakan itu berdiri sebuah pos kecil. Manusia menyebutnya Ashwatch, dan mengira mereka hanya menjaga perbatasan.",
            "Lalu datang seorang rakyat biasa yang pulang dengan kepala jernih dari tempat yang membuat orang lain gila. Namanya hilang dari catatan. Ia dikenang sebagai Sang Perintis.",
            "Ia menciptakan sistem Rank. Rank G bukan berarti misi termudah. Ia berarti jarak teraman dari kegelapan.",
            "Malam terakhir musim Dingin. Pintu-pintu Desa Pemula ditutup sebelum senja. Di setiap ambang ditaburkan garam, dua jari lebar, tidak boleh putus satu titik pun.",
            "Anak yang disuruh berlari membawa keranjang garam malam itu bernama Ari. Di pinggangnya belum ada pedang; pedang ibunya masih ditahan orang lain. Di lehernya, liontin besi yang kadang terasa hangat.",
            "Enam nama disebut di seluruh desa. Di tempat nama ketujuh, semua menahan napas. Siapa mengisinya, mengundang yang mengosongkannya.",
            "Tok.     Tok.     Tok.\nTidak ada yang menjawab. Di dada Ari, liontin besi berdenyut sekali, lalu sunyi.",
            "Pagi datang tanpa pedang. Di sinilah ceritamu dimulai."
        };

        static readonly Vector3 Base = new Vector3(0f, 3000f, 0f);
        static readonly Color Night = new Color(0.008f, 0.008f, 0.02f, 1f), Dawn = new Color(0.50f, 0.36f, 0.34f, 1f);

        readonly float[] S = new float[16];
        readonly List<System.Action<float>> anims = new List<System.Action<float>>();
        readonly List<Material> mats = new List<Material>();
        Camera cam; CameraClearFlags oldFlags; Color oldBg; float oldFar; bool oldFog; GameObject sky;
        float t0, T;
        Transform[] g;

        // ------------------------------------------------------------------ siklus
        public List<Cutscene.Shot> Build(Camera c)
        {
            cam = c; oldFlags = c.clearFlags; oldBg = c.backgroundColor; oldFar = c.farClipPlane; oldFog = RenderSettings.fog;
            c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = Night; c.farClipPlane = Mathf.Min(oldFar, 300f);
            RenderSettings.fog = false;
            var sl = FindFirstObjectByType<SkyLife>(); if (sl != null) { sky = sl.gameObject; sky.SetActive(false); }
            float acc = 0f; for (int i = 0; i < D.Length; i++) { S[i] = acc; acc += D[i]; }
            S[D.Length] = acc;
            t0 = Time.unscaledTime;
            g = new Transform[10];
            for (int i = 0; i < g.Length; i++)
            {
                var go = new GameObject("Adegan" + i); go.transform.SetParent(transform, false);
                go.transform.position = Base + new Vector3(i * 400f, 0f, 0f); g[i] = go.transform;
            }
            ActSeven(g[0]); ActSix(g[1]); ActFall(g[2]); ActJeda(g[3]); ActCrack(g[4]); ActEyes(g[5]);
            ActOutpost(g[6]); ActPioneer(g[7]); ActRank(g[8]); ActVillage(g[9]);
            return Shots();
        }

        public void Teardown()
        {
            if (cam != null) { cam.clearFlags = oldFlags; cam.backgroundColor = oldBg; cam.farClipPlane = oldFar; }
            RenderSettings.fog = oldFog;
            if (sky != null) sky.SetActive(true);
            foreach (var m in mats) if (m != null) Destroy(m);
            mats.Clear(); anims.Clear();
        }

        void Update()
        {
            T = Time.unscaledTime - t0;
            for (int i = 0; i < anims.Count; i++)
            {
                try { anims[i](T); }
                catch (System.Exception e) { Debug.LogWarning("[Prolog] animasi " + i + " dimatikan: " + e.Message); anims[i] = NoOp; }
            }
            if (cam != null)
            {
                float k = Mathf.Clamp01((T - S[14] - 0.4f) / 5f);
                cam.backgroundColor = Color.Lerp(Night, Dawn, k * k);
            }
        }

        // ------------------------------------------------------------------ pembantu
        void Anim(System.Action<float> a) { anims.Add(a); }
        float L(float t, int shot) { return Mathf.Max(0f, t - S[shot]); }
        static float Ease(float a) { a = Mathf.Clamp01(a); return a * a * (3f - 2f * a); }
        Vector3 C(int i) { return Base + new Vector3(i * 400f, 0f, 0f); }
        static void NoOp(float t) { }

        Material Glow(Color c) { var m = CombatFx.Mat(null, c); mats.Add(m); return m; }
        Material Dark(Color c) { var m = CombatFx.Solid(null, c); mats.Add(m); return m; }
        // Bahan Sprites/Default tidak menulis kedalaman, urutan gambar ditentukan jarak pusat objek. Tanah besar yang pusatnya dekat kamera
        // akan menimpa benda yang lebih jauh, jadi latar (bintang, bulan, matahari) digambar dulu (2400), lalu tanah (2500), lalu sisanya (3000).
        static Material Back(Material m, int queue) { m.renderQueue = queue; return m; }

        static GameObject Prim(PrimitiveType t, Transform par, Vector3 lp, Vector3 sc, Material m)
        {
            var o = GameObject.CreatePrimitive(t);
            var col = o.GetComponent<Collider>(); if (col != null) Destroy(col);
            o.transform.SetParent(par, false); o.transform.localPosition = lp; o.transform.localScale = sc;
            o.GetComponent<Renderer>().sharedMaterial = m;
            return o;
        }

        void Stars(Transform par, int n, float r, int seed)
        {
            var rng = new System.Random(seed * 977 + 13);
            var m = Back(Dark(new Color(0.9f, 0.92f, 1f, 0.85f)), 2400);
            for (int i = 0; i < n; i++)
            {
                double u = rng.NextDouble() * 2.0 - 1.0, a = rng.NextDouble() * 6.2831853;
                double s = System.Math.Sqrt(1.0 - u * u);
                var dir = new Vector3((float)(s * System.Math.Cos(a)), (float)u, (float)(s * System.Math.Sin(a)));
                float sz = 0.15f + (float)rng.NextDouble() * 0.35f;
                Prim(PrimitiveType.Sphere, par, dir * r, Vector3.one * sz, m);
            }
        }

        /// <summary>Rumah kecil: dinding, atap pelana (kubus diputar 45 derajat), pintu, dua jendela bercahaya. Mengembalikan akar; jendela di anak "Jendela".</summary>
        Transform Hut(Transform par, Vector3 lp, float yaw, float sc, Color wall, Color roof, Material window)
        {
            var root = new GameObject("Rumah").transform; root.SetParent(par, false);
            root.localPosition = lp; root.localRotation = Quaternion.Euler(0f, yaw, 0f); root.localScale = Vector3.one * sc;
            Prim(PrimitiveType.Cube, root, new Vector3(0f, 1.1f, 0f), new Vector3(3f, 2.2f, 3f), Dark(wall));
            var rf = Prim(PrimitiveType.Cube, root, new Vector3(0f, 2.2f, 0f), new Vector3(2.4f, 2.4f, 3.6f), Dark(roof));
            rf.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.8f, 1.52f), new Vector3(0.8f, 1.6f, 0.08f), Dark(new Color(0.12f, 0.08f, 0.06f)));
            var w = new GameObject("Jendela").transform; w.SetParent(root, false);
            Prim(PrimitiveType.Cube, w, new Vector3(-1.0f, 1.4f, 1.52f), new Vector3(0.5f, 0.5f, 0.08f), window);
            Prim(PrimitiveType.Cube, w, new Vector3(1.0f, 1.4f, 1.52f), new Vector3(0.5f, 0.5f, 0.08f), window);
            return root;
        }

        // ------------------------------------------------------------------ adegan 1: Tujuh
        void ActSeven(Transform p)
        {
            Stars(p, 70, 90f, 1);
            Color[] cols = { new Color(1f, 0.88f, 0.55f), new Color(0.6f, 0.85f, 1f), new Color(0.65f, 1f, 0.72f), new Color(1f, 0.65f, 0.75f),
                             new Color(0.8f, 0.68f, 1f), new Color(1f, 0.78f, 0.42f), new Color(1f, 0.42f, 0.38f) };
            for (int i = 0; i < 7; i++)
            {
                int k = i;
                var o = Prim(PrimitiveType.Sphere, p, Vector3.zero, Vector3.zero, Glow(cols[k])).transform;
                Anim(t =>
                {
                    float lt = L(t, 0), a = Ease((lt - 0.1f - k * 0.55f) / 1.0f);
                    float ang = (k * 360f / 7f + lt * 9f) * Mathf.Deg2Rad, rr = k == 6 ? 8.5f : 5.2f;
                    o.localPosition = new Vector3(Mathf.Sin(ang) * rr, (k == 6 ? 2.5f : 0f) + Mathf.Sin(lt * 0.7f + k) * 0.5f, Mathf.Cos(ang) * rr);
                    o.localScale = Vector3.one * (k == 6 ? 1.5f : 0.9f) * a;
                });
            }
        }

        // ------------------------------------------------------------------ adegan 2: enam dan satu
        void ActSix(Transform p)
        {
            Stars(p, 60, 90f, 2);
            var core = Prim(PrimitiveType.Sphere, p, Vector3.zero, Vector3.one * 1.8f, Glow(new Color(1f, 0.93f, 0.7f))).transform;
            Anim(t => { float lt = L(t, 1); core.localScale = Vector3.one * (1.7f + Mathf.Sin(lt * 1.3f) * 0.12f) * Ease(lt / 1.2f); });
            Color[] cols = { new Color(1f, 0.88f, 0.55f), new Color(0.6f, 0.85f, 1f), new Color(0.65f, 1f, 0.72f), new Color(1f, 0.65f, 0.75f), new Color(0.8f, 0.68f, 1f), new Color(1f, 0.78f, 0.42f) };
            for (int i = 0; i < 6; i++)
            {
                int k = i;
                var o = Prim(PrimitiveType.Sphere, p, Vector3.zero, Vector3.one * 0.9f, Glow(cols[k])).transform;
                Anim(t => { float ang = (k * 60f + L(t, 1) * 14f) * Mathf.Deg2Rad; o.localPosition = new Vector3(Mathf.Sin(ang) * 4.2f, Mathf.Sin(L(t, 1) + k) * 0.3f, Mathf.Cos(ang) * 4.2f); });
            }
            var halo = Glow(new Color(1f, 0.9f, 0.65f));
            var ring = new Transform[18];
            for (int i = 0; i < 18; i++) ring[i] = Prim(PrimitiveType.Sphere, p, Vector3.zero, Vector3.one * 0.22f, halo).transform;
            Anim(t => { for (int i = 0; i < 18; i++) { float a = (i * 20f + L(t, 1) * 5f) * Mathf.Deg2Rad; ring[i].localPosition = new Vector3(Mathf.Sin(a) * 6.8f, 0f, Mathf.Cos(a) * 6.8f); } });
            var seventh = Prim(PrimitiveType.Sphere, p, new Vector3(10.5f, 3.2f, -6f), Vector3.one * 0.85f, Glow(new Color(1f, 0.4f, 0.36f))).transform;
            Anim(t => { float lt = L(t, 1); seventh.localPosition = new Vector3(10.5f, 3.2f + Mathf.Sin(lt * 0.9f) * 0.3f, -6f); seventh.localScale = Vector3.one * Mathf.Lerp(0.95f, 0.6f, Ease(lt / D[1])); });
        }

        // ------------------------------------------------------------------ adegan 3: jatuh
        void ActFall(Transform p)
        {
            Stars(p, 50, 90f, 3);
            var topM = Glow(new Color(1f, 0.9f, 0.65f));
            for (int i = 0; i < 6; i++) { float a = i * 60f * Mathf.Deg2Rad; Prim(PrimitiveType.Sphere, p, new Vector3(Mathf.Sin(a) * 4f, 17f, Mathf.Cos(a) * 4f), Vector3.one * 0.8f, topM); }
            var fm = Glow(new Color(1f, 0.5f, 0.45f));
            var fall = Prim(PrimitiveType.Sphere, p, new Vector3(0f, 14f, 0f), Vector3.one * 1.3f, fm).transform;
            var tm = Dark(new Color(0.4f, 0.12f, 0.45f, 0.7f));
            var trail = new Transform[10];
            for (int i = 0; i < 10; i++) trail[i] = Prim(PrimitiveType.Sphere, p, new Vector3(0f, 14f, 0f), Vector3.one * 0.5f, tm).transform;
            Anim(t =>
            {
                float lt = L(t, 2), u = Mathf.Clamp01(lt / D[2]);
                fall.localPosition = new Vector3(Mathf.Sin(u * 2f) * 1.5f, Mathf.Lerp(14f, -26f, Ease(u)), 0f);        // easing sama dengan kamera (smoothstep) agar cahaya tetap di tengah bingkai
                fall.localScale = Vector3.one * Mathf.Lerp(1.3f, 0.7f, u);
                fm.color = Color.Lerp(new Color(1f, 0.5f, 0.45f), new Color(0.5f, 0.14f, 0.55f), u);
                for (int j = 0; j < trail.Length; j++)
                {
                    float uj = Mathf.Clamp01((lt - 0.13f * (j + 1)) / D[2]);
                    trail[j].localPosition = new Vector3(Mathf.Sin(uj * 2f) * 1.5f, Mathf.Lerp(14f, -26f, Ease(uj)), 0f);
                    trail[j].localScale = Vector3.one * (0.55f * (1f - j / 10f)) * (lt > 0.4f ? 1f : 0f);
                }
            });
        }

        // ------------------------------------------------------------------ adegan 4-5: Jeda
        Vector3 slotPos(int i) { float a = (66f - i * 22f) * Mathf.Deg2Rad; return new Vector3(Mathf.Sin(a) * 9f, 0f, Mathf.Cos(a) * 9f); }

        void ActJeda(Transform p)
        {
            Stars(p, 60, 90f, 4);
            var stone = Dark(new Color(0.35f, 0.34f, 0.38f));
            Color[] cols = { new Color(1f, 0.88f, 0.55f), new Color(0.6f, 0.85f, 1f), new Color(0.65f, 1f, 0.72f), new Color(1f, 0.65f, 0.75f), new Color(0.8f, 0.68f, 1f), new Color(1f, 0.78f, 0.42f) };
            for (int i = 0; i < 7; i++)
            {
                int k = i; Vector3 sp = slotPos(i);
                Prim(PrimitiveType.Cylinder, p, sp + new Vector3(0f, 0.9f, 0f), new Vector3(0.9f, 0.9f, 0.9f), stone);
                if (k < 6)
                {
                    var o = Prim(PrimitiveType.Sphere, p, sp + new Vector3(0f, 2.5f, 0f), Vector3.zero, Glow(cols[k])).transform;
                    Anim(t => { float a = Ease((L(t, 3) - 0.6f - k * 0.7f) / 1.0f); o.localScale = Vector3.one * 0.9f * a * (1f + Mathf.Sin(t * 1.5f + k) * 0.05f); });
                }
            }
            // ruang ketujuh: cincin titik yang kosong, berdenyut tujuh kali selama shot ke-5 (satu denyut per napas)
            Vector3 e = slotPos(6) + new Vector3(0f, 2.5f, 0f);
            var rm = Glow(new Color(0.75f, 0.6f, 1f));
            var dots = new Transform[16];
            for (int i = 0; i < 16; i++) dots[i] = Prim(PrimitiveType.Sphere, p, e, Vector3.one * 0.1f, rm).transform;
            Anim(t =>
            {
                float l4 = L(t, 4), l3 = L(t, 3);
                float pulse = (t >= S[4] && l4 < 6.3f) ? Mathf.Pow(Mathf.Abs(Mathf.Sin(l4 * Mathf.PI / 0.9f)), 6f) : 0f;
                float vis = Ease((l3 - 4.2f) / 1.5f);
                for (int i = 0; i < 16; i++)
                {
                    float a = i * 22.5f * Mathf.Deg2Rad, rad = 0.95f + pulse * 0.25f;
                    dots[i].localPosition = e + new Vector3(Mathf.Cos(a) * rad, Mathf.Sin(a) * rad, 0f);
                    dots[i].localScale = Vector3.one * (0.09f + 0.15f * pulse) * vis;
                }
            });
        }

        // ------------------------------------------------------------------ adegan 6: retakan
        void ActCrack(Transform p)
        {
            Stars(p, 40, 90f, 5);
            Prim(PrimitiveType.Cube, p, new Vector3(0f, -0.1f, 0f), new Vector3(90f, 0.2f, 90f), Back(Dark(new Color(0.07f, 0.05f, 0.07f)), 2500));
            var cm = Glow(new Color(1f, 0.38f, 0.1f));
            var main = Prim(PrimitiveType.Cube, p, new Vector3(0f, 0.02f, 0f), new Vector3(44f, 0.1f, 0.2f), cm).transform;
            var b1 = Prim(PrimitiveType.Cube, p, new Vector3(-6f, 0.02f, 3f), new Vector3(16f, 0.1f, 0.1f), cm).transform; b1.localRotation = Quaternion.Euler(0f, 35f, 0f);
            var b2 = Prim(PrimitiveType.Cube, p, new Vector3(8f, 0.02f, -3.5f), new Vector3(14f, 0.1f, 0.1f), cm).transform; b2.localRotation = Quaternion.Euler(0f, -32f, 0f);
            var sm = Dark(new Color(0.28f, 0.16f, 0.34f, 0.5f));
            const int N = 26;
            var smoke = new Transform[N];
            for (int i = 0; i < N; i++) smoke[i] = Prim(PrimitiveType.Sphere, p, Vector3.zero, Vector3.zero, sm).transform;
            Anim(t =>
            {
                float lt = L(t, 5), w = Mathf.Lerp(0.15f, 2.2f, Ease(lt / 6f));
                main.localScale = new Vector3(44f, 0.1f, w); b1.localScale = new Vector3(16f, 0.1f, w * 0.45f); b2.localScale = new Vector3(14f, 0.1f, w * 0.4f);
                float open = Ease(lt / 3f);
                for (int i = 0; i < N; i++)
                {
                    float ph = Mathf.Repeat(lt * 0.5f + i * 0.173f, 1f);
                    float x = -19f + ((i * 37) % 38), z = ((i * 53) % 7) - 3f;
                    smoke[i].localPosition = new Vector3(x, ph * 8f, z * w * 0.4f);
                    smoke[i].localScale = Vector3.one * Mathf.Sin(ph * Mathf.PI) * (1.2f + (i % 3) * 0.5f) * open;
                }
            });
        }

        // ------------------------------------------------------------------ adegan 7: empat pasang mata
        void Eyes(Transform p, Vector3 c, float spacing, Vector3 size, Color col, float start)
        {
            var m = Glow(col);
            for (int s = -1; s <= 1; s += 2)
            {
                int sg = s; var o = Prim(PrimitiveType.Sphere, p, c + new Vector3(sg * spacing * 0.5f, 0f, 0f), Vector3.zero, m).transform;
                Anim(t =>
                {
                    float open = Ease((L(t, 6) - start) / 1.2f);
                    o.localScale = new Vector3(size.x, Mathf.Max(0.02f, size.y * open), size.z) * (0.92f + 0.08f * Mathf.Sin(t * 2f + sg));
                });
            }
        }

        void ActEyes(Transform p)
        {
            Stars(p, 40, 90f, 6);
            Eyes(p, new Vector3(-15f, 9f, 6f), 2.4f, new Vector3(0.7f, 1.3f, 0.6f), new Color(1f, 0.5f, 0.12f), 0.8f);        // naga
            for (int i = -1; i <= 1; i++) Eyes(p, new Vector3(-5f + i * 3.6f, 3.2f, -10f), 1.3f, new Vector3(0.75f, 0.75f, 0.55f), new Color(1f, 0.22f, 0.16f), 2.0f + (i + 1) * 0.25f);   // cerberus
            Eyes(p, new Vector3(11f, 3f, 5f), 5.0f, new Vector3(1.7f, 1.3f, 1.3f), new Color(0.95f, 0.42f, 0.25f), 3.2f);         // behemoth
            Eyes(p, new Vector3(1f, 8f, 15f), 1.5f, new Vector3(0.95f, 0.24f, 0.4f), new Color(1f, 0.9f, 0.85f), 4.5f);          // sang demon
        }

        // ------------------------------------------------------------------ adegan 8: pos Ashwatch
        void ActOutpost(Transform p)
        {
            Stars(p, 60, 90f, 7);
            Prim(PrimitiveType.Cylinder, p, new Vector3(0f, -0.1f, 0f), new Vector3(34f, 0.1f, 34f), Dark(new Color(0.12f, 0.11f, 0.10f)));
            var cm = Glow(new Color(0.95f, 0.32f, 0.1f));
            Prim(PrimitiveType.Cube, p, new Vector3(0f, 0.03f, 0f), new Vector3(30f, 0.06f, 0.5f), cm);
            Vector3[] pos = { new Vector3(-6f, 0f, 5f), new Vector3(5f, 0f, 6f), new Vector3(-9f, 0f, -5f), new Vector3(8f, 0f, -6f), new Vector3(0f, 0f, 11f) };
            float[] yaw = { 160f, 200f, 30f, -20f, 180f };
            var wm = Glow(new Color(1f, 0.78f, 0.4f));
            for (int i = 0; i < 5; i++)
            {
                int k = i;
                var h = Hut(p, pos[k], yaw[k], 1f, new Color(0.34f, 0.26f, 0.19f), new Color(0.46f, 0.17f, 0.12f), wm);
                Anim(t => { float a = Ease((L(t, 7) - 0.3f - k * 1.0f) / 0.8f); h.localScale = Vector3.one * a; });
            }
            Prim(PrimitiveType.Cylinder, p, new Vector3(2.5f, 0.8f, 2.5f), new Vector3(0.12f, 0.8f, 0.12f), Dark(new Color(0.25f, 0.18f, 0.12f)));
            var fire = Prim(PrimitiveType.Sphere, p, new Vector3(2.5f, 1.75f, 2.5f), Vector3.one * 0.55f, Glow(new Color(1f, 0.6f, 0.2f))).transform;
            Anim(t => { fire.localScale = Vector3.one * (0.5f + 0.12f * Mathf.Sin(t * 9f) + 0.06f * Mathf.Sin(t * 14f)); });
        }

        // ------------------------------------------------------------------ adegan 9: Sang Perintis
        void ActPioneer(Transform p)
        {
            Stars(p, 50, 90f, 8);
            Prim(PrimitiveType.Cylinder, p, new Vector3(0f, -0.1f, 0f), new Vector3(40f, 0.1f, 40f), Back(Dark(new Color(0.10f, 0.10f, 0.09f)), 2500));
            Prim(PrimitiveType.Sphere, p, new Vector3(0f, 16f, 62f), Vector3.one * 9f, Back(Glow(new Color(0.75f, 0.82f, 1f)), 2400));       // bulan: di balik tujuh pilar; kamera orbit dari sisi -z memandang ke +z, jadi bulan tetap di dalam bingkai sepanjang shot
            var pil = Dark(new Color(0.52f, 0.50f, 0.47f));
            float[] hs = { 7f, 9f, 5f, 8f, 3.5f, 6.5f, 9f };
            for (int i = 0; i < 7; i++)
            {
                float a = (-70f + i * 23f) * Mathf.Deg2Rad, h = hs[i];
                Prim(PrimitiveType.Cylinder, p, new Vector3(Mathf.Sin(a) * 9f, h * 0.5f, 5f + Mathf.Cos(a) * 9f), new Vector3(1.1f, h * 0.5f, 1.1f), pil);
            }
            Prim(PrimitiveType.Cube, p, new Vector3(2f, 0.4f, 12f), new Vector3(4.6f, 0.8f, 1.3f), pil).transform.localRotation = Quaternion.Euler(0f, 25f, 4f);
            var gh = StoryCutscenes.Ghost.Make(p.position, new Color(1f, 0.92f, 0.65f), 1f);
            gh.fadeStart = 9999f; gh.transform.SetParent(p, true); gh.transform.localPosition = Vector3.zero;
            var chest = Prim(PrimitiveType.Sphere, p, new Vector3(0f, 1.35f, 0.25f), Vector3.one * 0.35f, Glow(new Color(1f, 0.92f, 0.6f))).transform;
            Anim(t => { chest.localScale = Vector3.one * (0.34f + 0.08f * Mathf.Sin(t * 2.2f)); });
            var sm = Dark(new Color(0.30f, 0.16f, 0.36f, 0.55f));
            const int N = 16;
            var smoke = new Transform[N];
            for (int i = 0; i < N; i++) smoke[i] = Prim(PrimitiveType.Sphere, p, Vector3.zero, Vector3.one * 1.3f, sm).transform;
            Anim(t =>
            {
                float lt = L(t, 8);
                for (int i = 0; i < N; i++)
                {
                    float r = 3.2f + 10f * Mathf.Repeat(1f - lt * 0.09f + i * 0.0625f, 1f);        // mendekat lalu berbalik: tak pernah lebih dekat dari 3,2 m
                    float a = (i * 22.5f + lt * 4f) * Mathf.Deg2Rad;
                    smoke[i].localPosition = new Vector3(Mathf.Sin(a) * r, 0.8f + (i % 4) * 0.5f, Mathf.Cos(a) * r);
                }
            });
        }

        // ------------------------------------------------------------------ adegan 10: Rank G
        void ActRank(Transform p)
        {
            Stars(p, 40, 90f, 9);
            Prim(PrimitiveType.Cylinder, p, new Vector3(0f, -0.1f, 0f), new Vector3(60f, 0.1f, 60f), Back(Dark(new Color(0.09f, 0.09f, 0.10f)), 2500));
            var stone = Dark(new Color(0.42f, 0.41f, 0.40f));
            for (int i = 0; i < 9; i++) Prim(PrimitiveType.Cylinder, p, new Vector3(Mathf.Sin(i * 1.7f) * 0.6f, 0.07f, -16f + i * 4f), new Vector3(1.7f, 0.07f, 1.7f), stone);
            Prim(PrimitiveType.Cube, p, new Vector3(0f, 0.03f, -22f), new Vector3(7f, 0.06f, 0.6f), Glow(new Color(1f, 0.3f, 0.1f)));
            Prim(PrimitiveType.Cylinder, p, new Vector3(0f, 1.3f, 22f), new Vector3(0.15f, 1.3f, 0.15f), Dark(new Color(0.2f, 0.15f, 0.1f)));
            var lamp = Prim(PrimitiveType.Sphere, p, new Vector3(0f, 2.9f, 22f), Vector3.one * 1.1f, Glow(new Color(1f, 0.82f, 0.45f))).transform;
            Anim(t => { lamp.localScale = Vector3.one * (1.05f + 0.08f * Mathf.Sin(t * 3f)); });
            var go = new GameObject("HurufG"); go.transform.SetParent(p, false); go.transform.localPosition = new Vector3(0f, 3.6f, -4f);
            var tm = go.AddComponent<TextMesh>(); tm.text = "G"; tm.fontSize = 96; tm.characterSize = 0.28f; tm.anchor = TextAnchor.MiddleCenter; tm.fontStyle = FontStyle.Bold; tm.color = new Color(1f, 0.85f, 0.4f);
            var gt = go.transform;
            Anim(t => { if (cam != null) gt.rotation = cam.transform.rotation; gt.localPosition = new Vector3(0f, 3.6f + Mathf.Sin(t * 1.2f) * 0.15f, -4f); });
        }

        // ------------------------------------------------------------------ adegan 11-15: Malam Tanpa Nama
        const float VR = 15f;
        Vector3 houseDir(int i) { float a = (10f + i * 60f) * Mathf.Deg2Rad; return new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)); }
        Vector3 doorWorld(int i) { return C(9) + houseDir(i) * (VR - 1.6f); }
        Vector3 ariAt(float u)
        {
            float a = Mathf.Lerp(190f, 10f, Ease(u)) * Mathf.Deg2Rad;
            return C(9) + new Vector3(Mathf.Sin(a) * 9f, 0f, Mathf.Cos(a) * 9f);
        }

        void ActVillage(Transform p)
        {
            Stars(p, 90, 95f, 10);
            Prim(PrimitiveType.Cylinder, p, new Vector3(0f, -0.1f, 0f), new Vector3(70f, 0.1f, 70f), Back(Dark(new Color(0.05f, 0.07f, 0.07f)), 2500));
            var salt = Dark(new Color(0.96f, 0.96f, 0.92f));
            var wins = new Material[6];
            for (int i = 0; i < 6; i++)
            {
                int k = i; var wm = Glow(new Color(1f, 0.74f, 0.36f)); wins[k] = wm;
                Vector3 d = houseDir(k);
                var h = Hut(p, d * VR, 10f + k * 60f + 180f, 1f, new Color(0.22f, 0.20f, 0.22f), new Color(0.26f, 0.14f, 0.16f), wm);
                Prim(PrimitiveType.Cube, h, new Vector3(0f, 0.03f, 2.5f), new Vector3(2.2f, 0.05f, 0.28f), salt);      // garam di ambang: dua jari lebar, tanpa putus
            }
            var mm = Dark(new Color(0.72f, 0.76f, 0.82f, 0.22f));
            const int NM = 12;
            var mist = new Transform[NM];
            for (int i = 0; i < NM; i++) mist[i] = Prim(PrimitiveType.Sphere, p, Vector3.zero, new Vector3(9f, 2.2f, 9f), mm).transform;
            Anim(t =>
            {
                float u = Mathf.Clamp01((t - S[10]) / (S[15] - S[10]));
                float r = Mathf.Lerp(37f, 31f, u);                                                // kabut maju selangkah
                for (int i = 0; i < NM; i++) { float a = (200f + i * 9f) * Mathf.Deg2Rad; mist[i].localPosition = new Vector3(Mathf.Sin(a) * r, 1.1f + (i % 3) * 0.2f, Mathf.Cos(a) * r); }
            });
            // Ari kecil membawa keranjang garam, liontin besi di dadanya
            var ari = StoryCutscenes.Ghost.Make(p.position, new Color(1f, 0.86f, 0.62f), 0.72f);
            ari.fadeStart = 9999f; ari.transform.SetParent(p, true); var at = ari.transform;
            var pend = Prim(PrimitiveType.Sphere, p, Vector3.zero, Vector3.one * 0.16f, Glow(new Color(1f, 0.6f, 0.3f))).transform;
            Anim(t =>
            {
                float u = Mathf.Clamp01((t - S[11]) / (D[11] - 0.8f));
                at.position = ariAt(u); at.position = new Vector3(at.position.x, Base.y, at.position.z);
                pend.position = at.position + new Vector3(0f, 1.1f, 0f);
                float b = (t >= S[13] + 4.0f) ? Mathf.Exp(-Mathf.Pow((t - S[13] - 4.3f) / 0.35f, 2f)) : 0.15f * (0.5f + 0.5f * Mathf.Sin(t * 1.4f));
                pend.localScale = Vector3.one * (0.14f + 0.5f * b);
            });
            // jendela padam satu per satu saat seisi desa menahan napas
            Anim(t =>
            {
                float l12 = L(t, 12);
                for (int i = 0; i < 6; i++) wins[i].color = Color.Lerp(new Color(1f, 0.74f, 0.36f), Color.black, Ease((l12 - 0.5f - i * 0.65f) / 0.5f));
            });
            // tiga ketukan di pintu pertama
            var kn = Prim(PrimitiveType.Sphere, p, Vector3.zero, Vector3.zero, Glow(new Color(1f, 0.7f, 0.35f))).transform;
            float[] kt = { 0.9f, 2.1f, 3.3f };
            Anim(t =>
            {
                float l13 = L(t, 13), best = 0f;
                for (int i = 0; i < 3; i++) best = Mathf.Max(best, Mathf.Exp(-Mathf.Pow((l13 - kt[i]) / 0.16f, 2f)));
                kn.position = doorWorld(0) + new Vector3(0f, 1.1f, 0f);
                kn.localScale = Vector3.one * (best * 1.1f);
            });
            // matahari terbit di akhir
            var sun = Prim(PrimitiveType.Sphere, p, new Vector3(63f, -15f, 63f), Vector3.one * 14f, Back(Glow(new Color(1f, 0.66f, 0.4f)), 2400)).transform;      // di arah pandang shot terakhir (azimut ~45 derajat)
            // tersembunyi sampai shot terakhir; baru di sana ia naik dari balik tepi pulau (tanah menutupinya di awal shot)
            Anim(t => { float u = Ease((t - S[14] - 0.3f) / 5f); sun.localPosition = new Vector3(63f, Mathf.Lerp(-15f, 4f, u), 63f); sun.localScale = Vector3.one * (t >= S[14] ? 14f : 0f); });
        }

        // ------------------------------------------------------------------ urutan kamera
        List<Cutscene.Shot> Shots()
        {
            var l = new List<Cutscene.Shot>();
            Vector3 c0 = C(0), c1 = C(1), c2 = C(2), c3 = C(3), c4 = C(4), c5 = C(5), c6 = C(6), c7 = C(7), c8 = C(8), c9 = C(9);
            l.Add(Cutscene.Orbit(c0, 22f, 15f, 5f, 3f, 200f, 260f, D[0], Cap[0], 0.5f));
            l.Add(Cutscene.Orbit(c1, 24f, 17f, 6f, 4f, 150f, 215f, D[1], Cap[1], 1f));
            l.Add(Cutscene.Lerp(c2 + new Vector3(6f, 12f, 18f), c2 + new Vector3(4f, -20f, 16f), c2 + new Vector3(0f, 13f, 0f), c2 + new Vector3(0f, -24f, 0f), D[2], Cap[2]));
            l.Add(Cutscene.Orbit(c3 + new Vector3(0f, 1.5f, 0f), 26f, 20f, 5f, 4f, -12f, 12f, D[3], Cap[3], 0.5f));
            Vector3 tg = c3 + slotPos(6) + new Vector3(0f, 2.5f, 0f);
            l.Add(Cutscene.Lerp(tg + new Vector3(-2f, 1.8f, 15f), tg + new Vector3(-0.6f, 0.4f, 5.5f), tg + new Vector3(3f, 0f, 0f), tg, D[4], Cap[4]));
            l.Add(Cutscene.Orbit(c4, 24f, 15f, 14f, 6f, 200f, 250f, D[5], Cap[5], 0f));
            l.Add(Cutscene.Orbit(c5 + new Vector3(0f, 4f, 0f), 14f, 10f, 5f, 6f, 20f, 300f, D[6], Cap[6], 0f));
            l.Add(Cutscene.Orbit(c6 + new Vector3(0f, 1.5f, 0f), 34f, 24f, 16f, 9f, 20f, 90f, D[7], Cap[7], 1f));
            l.Add(Cutscene.Orbit(c7 + new Vector3(0f, 1.4f, 0f), 12f, 7f, 3.2f, 2.2f, 150f, 215f, D[8], Cap[8], 1.5f));
            l.Add(Cutscene.Orbit(c8 + new Vector3(0f, 2f, 0f), 28f, 22f, 6f, 4f, 80f, 115f, D[9], Cap[9], 1.2f));       // dari samping: retakan di satu sisi, lentera di sisi lain
            l.Add(Cutscene.Orbit(c9 + new Vector3(0f, 1.5f, 0f), 40f, 26f, 22f, 14f, 30f, 100f, D[10], Cap[10], 1f));
            l.Add(Cutscene.Orbit(c9 + new Vector3(0f, 1.2f, 0f), 24f, 16f, 8f, 5f, 60f, 150f, D[11], Cap[11], 1f));
            l.Add(Cutscene.Orbit(c9 + new Vector3(0f, 1.5f, 0f), 30f, 28f, 20f, 18f, 150f, 200f, D[12], Cap[12], 1f));
            Vector3 dr = doorWorld(0), to = -houseDir(0), side = new Vector3(to.z, 0f, -to.x);
            // kamera mendekat dari samping: Ari berdiri tepat di jalur lurus ke pintu (4,4 m di depan pintu), jangan menembus tubuhnya
            l.Add(Cutscene.Lerp(dr + to * 7f + side * 1.8f + new Vector3(0f, 1.6f, 0f), dr + to * 4.0f + side * 1.5f + new Vector3(0f, 1.4f, 0f), dr + new Vector3(0f, 1.3f, 0f), dr + new Vector3(0f, 1.3f, 0f), D[13], Cap[13]));
            l.Add(Cutscene.Orbit(c9 + new Vector3(0f, 1.5f, 0f), 34f, 60f, 12f, 32f, 200f, 250f, D[14], Cap[14], 4f));
            return l;
        }
    }
}
