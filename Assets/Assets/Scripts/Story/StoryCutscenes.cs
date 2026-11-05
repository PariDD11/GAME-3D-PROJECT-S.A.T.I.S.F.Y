using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Tiga cutscene cerita (memakai sistem Cutscene yang ada): Teno menghilang di Batu Dua Nama, Dagan gugur di Takhta Zargoth,
    /// dan Ari menutup Pintu di akhir cerita. Masing-masing hanya sekali per save (flag cs_teno / cs_dagan / cs_pintu), bisa dilewati.
    /// </summary>
    public static class StoryCutscenes
    {
        public static bool Seen(string k) { var g = GameState.I; return g != null && g.flags.Contains(k); }
        static void Mark(string k) { var g = GameState.I; if (g != null) g.flags.Add(k); }

        /// <summary>Dipanggil WorldQuests.OnEvent untuk setiap kejadian.</summary>
        public static void OnEvent(string kind, string id)
        {
            if (kind == "kill" && id == "demon_lord") Dagan();
        }

        /// <summary>Dipanggil LoreObject sebelum membaca. Return true bila cutscene diputar (baca ditunda sampai selesai).</summary>
        public static bool OnLore(string loreId, Transform obj, System.Action then)
        {
            if (loreId == "nd_o_altar" && !Seen("ketuk_jawab") && !Seen("ketuk_diam") && !Cutscene.Active) { KetukChoice(obj, then); return true; }
            if (loreId != "nd_o_ketukan" || Seen("cs_teno") || Cutscene.Active || obj == null) return false;
            Mark("cs_teno");
            Teno(obj.position, then);
            return true;
        }

        // ------------------------------------------------------------------ pilihan dialog
        public static void Choice(string title, string text, string a, System.Action ra, string b, System.Action rb)
        {
            var ui = TownUI.I; if (ui == null) { if (ra != null) ra(); return; }
            ui.Open(title, () => text, () => new System.Collections.Generic.List<UIEntry>
            {
                new UIEntry(a, () => { ui.Close(); if (ra != null) ra(); }),
                new UIEntry(b, () => { ui.Close(); if (rb != null) rb(); }),
            });
        }

        static void TenoChoice(System.Action then)
        {
            Choice("Lentera Teno", "Lentera itu menyala sendiri di antara dua nama.\n\"Jaga ini. Itu cuma permintaan.\"",
                "Aku akan menjaga lenteramu.", () => { Mark("teno_jaga"); NameBook.Record("Teno Kest", "Memegang lentera di Padang Hantu. Aku berjanji menjaganya."); GameState.Say("Kamu berjanji menjaga lentera Teno."); if (then != null) then(); },
                "Lentera ini kubawa pulang.", () => { Mark("teno_ambil"); NameBook.Record("Teno Kest", "Memegang lentera di Padang Hantu. Lenteranya kubawa pulang."); GameState.Say("Kamu membawa nyala lentera Teno di buku catatan."); if (then != null) then(); });
        }

        static void KetukChoice(Transform obj, System.Action then)
        {
            Choice("Ketukan di Bawah Altar", "Dari bawah batu Telinga terdengar tiga ketukan pendek. Jeda. Tiga lagi.\nTarn berbisik: \"Itu bukan untuk kita. Jangan jawab.\"",
                "Jawab dengan tiga ketukan.", () => { Mark("ketuk_jawab"); GameState.Say("Ketukan itu berhenti. Lalu terdengar satu tawa pendek, kering."); var pc = PlayerCombat.I; if (pc != null) StoryScenery.Alarm(pc.transform.position, "Telinga mendengar jawabanmu!"); if (then != null) then(); },
                "Tetap diam.", () => { Mark("ketuk_diam"); var g = GameState.I; if (g != null) g.AddExp(250); GameState.Say("Kamu menahan napas. Ketukan itu memudar. +250 EXP"); if (then != null) then(); });
        }

        // ------------------------------------------------------------------ Cap Eldrath
        public static int Caps { get { int n = 0; for (int i = 1; i <= 3; i++) if (Seen("cap" + i)) n++; return n; } }
        public static void GiveCap(int n)
        {
            if (Seen("cap" + n)) return;
            Mark("cap" + n);
            Audio.Sfx("quest_reward");
            GameState.Say("Cap Eldrath ke-" + n + " didapat  (" + Caps + "/3).");
        }

        // ------------------------------------------------------------------ Odran gugur
        static void Odran()
        {
            if (Seen("cs_odran") || Cutscene.Active) return;
            var pc = PlayerCombat.I; if (pc == null) return;
            Mark("cs_odran");
            Vector3 c = pc.transform.position;
            var ghost = Ghost.Make(c + pc.transform.right * 2f, new Color(0.7f, 0.8f, 1f), 1f);
            ghost.fadeStart = 7f; ghost.fadeDur = 5f; ghost.rise = 0.5f;
            var l = new System.Collections.Generic.List<Cutscene.Shot>
            {
                Cutscene.Orbit(c, 9f, 7f, 3.2f, 2.6f, 20f, 70f, 4.5f, "Festival Cahaya Salju. Ribuan lentera diangkat bersama di lapangan terbuka.", 1.6f),
                Cutscene.Orbit(c, 5.5f, 4.5f, 2f, 1.8f, 70f, 110f, 4.5f, "Odran Vael gugur malam itu, melindungi Ari. Tidak ada pidato. Itu permintaan Brenna.", 1.5f),
                Cutscene.Orbit(c, 5f, 8f, 1.8f, 4.5f, 110f, 170f, 4.5f, "Hanya satu koin perak yang diletakkan Ari di dadanya, sebelum tutup peti dipaku.", 1.6f),
            };
            Cutscene.Play(l, () => { ghost.Kill(); GameState.Say("Odran Vael gugur."); });
        }

        // ------------------------------------------------------------------ Epilog: ketukan
        public static void Tick()
        {
            if (!Seen("cs_pintu") || Seen("cs_epilog") || Cutscene.Active) return;
            var pc = PlayerCombat.I; if (pc == null || pc.IsDead || TownUI.I == null || TownUI.I.PanelOpen) return;
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != GuildConfig.TownScene) return;
            Epilog(pc.transform.position);
        }

        static void Epilog(Vector3 c)
        {
            Mark("cs_epilog"); Mark("ending_" + StoryEnding.Tier());
            var l = new System.Collections.Generic.List<Cutscene.Shot>();
            l.Add(Cutscene.Orbit(c, 10f, 8f, 3.5f, 4f, 0f, 40f, 4f, "Satu tahun kemudian. Ashwatch bangun lebih lambat daripada kota mana pun, dan lebih keras kepala.", 1.6f));
            float a = 40f;
            foreach (var line in StoryEnding.Lines()) { l.Add(Cutscene.Orbit(c, 8f, 8f, 4f, 4.4f, a, a + 35f, 5f, line, 1.8f)); a += 35f; }
            l.Add(Cutscene.Orbit(c, 8f, 12f, 4.4f, 9f, a, a + 50f, 6f, "Setiap malam Ari membiarkan matanya terbuka. \"Aku di sini. Aku melihatmu.\"   Tok. Tok. Tok.   Ia masih mengetuk.", 2f));
            Cutscene.Play(l, () => GameState.Say("Ending: " + StoryEnding.TierTitle() + "  (" + NameBook.Count + " nama di Buku Nama)"));
        }

        public static void OnQuestDone(string questId)
        {
            if (questId == "nd_k14") GiveCap(1);
            if (questId == "nd_k15") Odran();
            if (questId == "nd_k16") GiveCap(2);
            if (questId == "nd_k17") GiveCap(3);
            if (questId == "nd_k18") Pintu();
        }

        // ------------------------------------------------------------------ Teno
        static void Teno(Vector3 stone, System.Action then)
        {
            var ghost = Ghost.Make(stone + new Vector3(1.1f, 0f, 0.4f), new Color(0.75f, 0.9f, 1f), 0.85f);
            ghost.fadeStart = 7.5f; ghost.fadeDur = 4.5f;
            var l = new List<Cutscene.Shot>
            {
                Cutscene.Orbit(stone, 7f, 5.5f, 2.6f, 2.0f, 200f, 240f, 4.5f, "Padang Hantu. Satu batu, dua nama, satu lentera yang tidak pernah padam.", 1.4f),
                Cutscene.Orbit(stone, 4.8f, 4.2f, 1.9f, 1.7f, 120f, 150f, 4.5f, "\"Aku menjaga ini entah berapa lama. Yang sulit bukan sepi. Yang sulit adalah tidak tahu apakah menunggu ini masih tugas, atau cuma kebiasaan.\"", 1.4f),
                Cutscene.Orbit(stone, 4.8f, 6f, 1.8f, 3.0f, 150f, 200f, 4.5f, "\"Jaga lentera ini. Jangan bilang itu perintah. Itu cuma permintaan.\"", 1.5f),
                Cutscene.Orbit(stone, 6f, 8f, 3.0f, 4.0f, 200f, 250f, 4f, "Teno Kest. Tujuh belas tahun. Memegang lentera.", 1.6f),
            };
            Cutscene.Play(l, () => { ghost.Kill(); GameState.Say("Lentera Teno kini menyala di antara dua nama."); TenoChoice(then); });
        }

        // ------------------------------------------------------------------ Dagan
        static void Dagan()
        {
            if (Seen("cs_dagan") || Cutscene.Active) return;
            var pc = PlayerCombat.I; if (pc == null) return;
            Mark("cs_dagan");
            Vector3 c = pc.transform.position;
            var g = Ghost.Make(c + pc.transform.right * 1.8f, new Color(1f, 0.55f, 0.15f), 1f);
            g.fadeStart = 6f; g.fadeDur = 6f; g.rise = 0.9f; g.sparks = true;
            var l = new List<Cutscene.Shot>
            {
                Cutscene.Orbit(c, 7f, 5f, 2.4f, 2.0f, 30f, 80f, 4f, "Dagan naik ke puncak tanpa disuruh. Api yang selama ini ia tahan akhirnya ia lepaskan.", 1.4f),
                Cutscene.Orbit(c, 5f, 4.2f, 1.9f, 1.8f, 80f, 120f, 4f, "\"Selesai.\"", 1.5f),
                Cutscene.Orbit(c, 4.5f, 5f, 1.8f, 2.4f, 120f, 150f, 4.5f, "\"Jangan lupa jalan pulang.\"   \"Tidak akan.\"   \"Aku sudah menemukannya.\"", 1.6f),
                Cutscene.Orbit(c, 6f, 9f, 2.6f, 5.5f, 150f, 210f, 5f, "Tubuhnya naik pelan dalam butir cahaya keemasan. Langit merah berubah kuning pucat seperti subuh.", 3.5f),
                Cutscene.Orbit(c, 9f, 9f, 5.5f, 5.5f, 210f, 240f, 3.5f, "Dagan Roe. Dua puluh tiga tahun. Menyalakan api hanya sedikit.", 3f),
            };
            Cutscene.Play(l, () =>
            {
                g.Kill();
                var gs = GameState.I; if (gs != null) gs.flags.Add("dagan_gone");
                GameState.Say("Dagan Roe gugur. Ia tidak akan kembali ke party.");
            });
        }

        // ------------------------------------------------------------------ Pintu
        static void Pintu()
        {
            if (Seen("cs_pintu") || Cutscene.Active) return;
            var pc = PlayerCombat.I; if (pc == null) return;
            Mark("cs_pintu");
            Vector3 c = pc.transform.position;
            var l = new List<Cutscene.Shot>
            {
                Cutscene.Orbit(c, 8f, 6f, 2.6f, 2.2f, 0f, 60f, 5f, "Ari menutup pintu. Tanpa bunyi, dua daun cahaya bertemu di tengah, seperti dua telapak tangan terkatup.", 1.5f),
                Cutscene.Orbit(c, 6f, 6f, 2.2f, 3.5f, 60f, 130f, 5f, "Gelap menyusut jadi titik, jadi garis, jadi retakan sehalus rambut. Lalu tak tampak.", 1.6f),
                Cutscene.Orbit(c, 6f, 11f, 3.5f, 7f, 130f, 200f, 5.5f, "Satu tahun kemudian, Ashwatch membuka dapur umum di halaman Guild. Papan di pagar: Makan dulu. Bertengkar nanti.", 1.8f),
                Cutscene.Orbit(c, 11f, 11f, 7f, 7f, 200f, 230f, 5f, "Setiap malam Ari membiarkan matanya terbuka. \"Aku di sini. Aku melihatmu.\"", 1.8f),
                Cutscene.Orbit(c, 11f, 12f, 7f, 8f, 230f, 250f, 5f, "Tok. Tok. Tok.    Ia masih mengetuk.", 1.8f),
            };
            Cutscene.Play(l, () => GameState.Say("Cerita utama selesai. Buku Nama tetap terbuka: tulislah satu nama lagi hari ini."));
        }

        // ------------------------------------------------------------------ lentera Teno (permanen)
        public static void AddLantern(Transform stone)
        {
            if (stone == null || stone.Find("LenteraTeno") != null) return;
            var go = new GameObject("LenteraTeno"); go.transform.SetParent(stone, false); go.transform.localPosition = new Vector3(0f, 2.55f, 0f);
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(s.GetComponent<Collider>());
            s.transform.SetParent(go.transform, false); s.transform.localScale = Vector3.one * 0.22f;
            s.GetComponent<Renderer>().sharedMaterial = CombatFx.Solid(null, new Color(1f, 0.9f, 0.55f));
            var lt = go.AddComponent<Light>(); lt.type = LightType.Point; lt.color = new Color(1f, 0.85f, 0.5f); lt.range = 9f; lt.intensity = 1.3f;
            go.AddComponent<LanternFlicker>();
        }

        class LanternFlicker : MonoBehaviour
        {
            Light lt; float seed;
            void Start() { lt = GetComponent<Light>(); seed = Random.value * 10f; }
            void Update() { if (lt != null) lt.intensity = 1.2f + 0.25f * Mathf.PerlinNoise(Time.time * 2.2f, seed); }
        }

        /// <summary>Sosok hantu sederhana (kapsul + kepala + cahaya) yang memudar / naik; diganti model sendiri bila perlu.</summary>
        public class Ghost : MonoBehaviour
        {
            public float fadeStart = 5f, fadeDur = 4f, rise = 0.35f; public bool sparks;
            float t0; Transform body, fig; Light lt; Vector3 baseScale; List<Transform> sp = new List<Transform>();

            public static Ghost Make(Vector3 pos, Color c, float scale)
            {
                RaycastHit h; if (Physics.Raycast(pos + Vector3.up * 20f, Vector3.down, out h, 60f, ~0, QueryTriggerInteraction.Ignore)) pos = h.point;
                var go = new GameObject("Hantu"); go.transform.position = pos;
                var gh = go.AddComponent<Ghost>(); gh.t0 = Time.unscaledTime;
                // figur NpcKit sungguhan (wajah, rambut, jubah) lalu dibuat tembus pandang bercahaya
                var sp0 = NpcKit.RandomSpec(NpcKit.OPengembara, new System.Random(Mathf.Abs((int)(pos.x * 31f + pos.z * 17f)) + 7));
                sp0.hat = 0; sp0.prop = 0; sp0.cA = 9; sp0.cB = 11; sp0.cC = 9;
                var fig = NpcKit.Spawn(go.transform, "Wujud", pos, 0f, sp0, false, false);
                fig.transform.localPosition = Vector3.zero; fig.transform.localScale = Vector3.one * scale;
                var mg = CombatFx.Solid(null, new Color(c.r, c.g, c.b, 0.55f));
                foreach (var r in fig.GetComponentsInChildren<Renderer>())
                {
                    var ms = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                    for (int i = 0; i < ms.Length; i++) ms[i] = mg;
                    r.sharedMaterials = ms;
                }
                gh.fig = fig.transform;
                // kabut di bawah lutut + dua cincin cahaya agar kaki memudar seperti arwah
                for (int i = 0; i < 3; i++)
                {
                    var m = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(m.GetComponent<Collider>());
                    m.transform.SetParent(go.transform, false); m.transform.localPosition = new Vector3(0f, 0.12f + i * 0.1f, 0f);
                    m.transform.localScale = new Vector3(1.3f - i * 0.25f, 0.18f, 1.3f - i * 0.25f) * scale;
                    m.GetComponent<Renderer>().sharedMaterial = CombatFx.Solid(null, new Color(c.r, c.g, c.b, 0.25f - i * 0.05f));
                }
                gh.body = go.transform; gh.baseScale = go.transform.localScale;
                gh.lt = go.AddComponent<Light>(); gh.lt.type = LightType.Point; gh.lt.color = c; gh.lt.range = 8f; gh.lt.intensity = 1.6f;
                gh.lt.transform.localPosition = Vector3.up * 1.4f;
                return gh;
            }

            void Start()
            {
                if (!sparks) return;
                for (int i = 0; i < 14; i++)
                {
                    var s = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(s.GetComponent<Collider>());
                    s.transform.SetParent(transform, false); s.transform.localScale = Vector3.one * Random.Range(0.06f, 0.14f);
                    s.transform.localPosition = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.2f, 2f), Random.Range(-0.5f, 0.5f));
                    s.GetComponent<Renderer>().sharedMaterial = CombatFx.Solid(null, new Color(1f, 0.7f, 0.25f));
                    sp.Add(s.transform);
                }
            }

            void Update()
            {
                float t = Time.unscaledTime - t0;
                if (fig != null && Camera.main != null)
                {
                    var d = Camera.main.transform.position - transform.position; d.y = 0f;
                    if (d.sqrMagnitude > 0.01f) fig.rotation = Quaternion.Slerp(fig.rotation, Quaternion.LookRotation(d), 3f * Time.unscaledDeltaTime);
                    fig.localPosition = new Vector3(0f, 0.15f + Mathf.Sin(Time.unscaledTime * 1.3f) * 0.08f, 0f);
                }
                float f = Mathf.Clamp01((t - fadeStart) / Mathf.Max(0.1f, fadeDur));
                if (f > 0f)
                {
                    transform.localScale = baseScale * Mathf.Lerp(1f, 0.2f, f);
                    transform.position += Vector3.up * rise * Time.unscaledDeltaTime;
                    if (lt != null) lt.intensity = Mathf.Lerp(1.6f, 3.2f, Mathf.Sin(f * Mathf.PI)) * (1f - f * 0.6f);
                }
                foreach (var s in sp) if (s != null) s.localPosition += new Vector3(0f, 0.6f * Time.unscaledDeltaTime, 0f);
            }

            public void Kill() { if (this != null) Destroy(gameObject); }
        }
    }
}
