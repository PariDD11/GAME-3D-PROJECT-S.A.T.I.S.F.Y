using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HunterGuild
{
    // =====================================================================================================
    // Fitur pendukung cerita "Nama Yang Dilarang": Buku Nama, Dinding Nama, Tugu Jeda, Lilin Soriel,
    // dan kesadaran cerita pada dialog warga. Semua progres disimpan di GameState.flags (ikut save).
    // =====================================================================================================

    /// <summary>Buku catatan Ari: nama orang yang pernah kamu ajak bicara. Nama dicatat lewat dialog NPC (tombol "Catat namanya") atau saat bicara dengan NPC cerita.</summary>
    public static class NameBook
    {
        const string P = "nm|";
        static readonly int[] Milestones = { 5, 10, 20, 30, 45 };

        public static bool Has(string name) { var g = GameState.I; if (g == null) return false; foreach (var f in g.flags) if (f.StartsWith(P) && NameOf(f) == name) return true; return false; }
        static string NameOf(string flag) { int i = flag.IndexOf('|', P.Length); return i < 0 ? flag.Substring(P.Length) : flag.Substring(P.Length, i - P.Length); }
        static string NoteOf(string flag) { int i = flag.IndexOf('|', P.Length); return i < 0 ? "" : flag.Substring(i + 1); }

        public static List<string[]> All()
        {
            var l = new List<string[]>(); var g = GameState.I; if (g == null) return l;
            foreach (var f in g.flags) if (f.StartsWith(P)) l.Add(new[] { NameOf(f), NoteOf(f) });
            l.Sort((a, b) => string.Compare(a[0], b[0], System.StringComparison.Ordinal));
            return l;
        }
        public static int Count { get { var g = GameState.I; if (g == null) return 0; int n = 0; foreach (var f in g.flags) if (f.StartsWith(P)) n++; return n; } }

        /// <summary>Catat satu nama (sekali per nama). note = satu kalimat tentang orang itu.</summary>
        public static bool Record(string name, string note)
        {
            var g = GameState.I; if (g == null || string.IsNullOrEmpty(name) || Has(name)) return false;
            note = (note ?? "").Replace("|", "/");
            g.flags.Add(P + name + "|" + note);
            Audio.Sfx("quest_accept");
            int n = Count;
            GameState.Say("Tercatat di Buku Nama: " + name + "  (" + n + ")");
            if (n == 1) GameState.Say("Buka Buku Nama kapan saja dengan tombol N.");
            foreach (int m in Milestones) if (n == m) Milestone(m);
            WorldQuests.OnEvent("name", name);
            return true;
        }

        static void Milestone(int n)
        {
            var g = GameState.I;
            g.gold += 150 * n; g.AddExp(60 * n);
            LoreDB.Unlock("nd_m" + n, false);
            GameState.Say("Halaman Buku Nama terisi: " + n + " nama!  +" + (150 * n) + "G");
        }

        public static void Open(System.Action back = null)
        {
            var ui = TownUI.I; if (ui == null) return;
            ui.Open("Buku Nama", () => Count + " nama tercatat.\n\"Orang yang namanya ditulis tidak benar-benar hilang.\"\n(Halaman terisi pada 5, 10, 20, 30, 45 nama.)", () =>
            {
                var l = new List<UIEntry>();
                foreach (var e in All())
                {
                    var en = e;
                    l.Add(new UIEntry(en[0], () => ui.Open(en[0], () => string.IsNullOrEmpty(en[1]) ? "(tanpa catatan)" : en[1], () => new List<UIEntry> { new UIEntry("< Kembali", () => Open(back)) })));
                }
                if (l.Count == 0) l.Add(new UIEntry("(belum ada nama. Ajak warga bicara lalu pilih \"Catat namanya\".)", () => { }, false));
                l.Add(new UIEntry("Jurnal Cerita", () => StoryJournal.Open(() => Open(back))));
                l.Add(new UIEntry(back != null ? "< Kembali" : "Tutup", back != null ? back : (System.Action)(() => ui.Close())));
                return l;
            });
        }
    }

    /// <summary>Pembaca tombol N (Buku Nama) dan pemicu kecil lain milik fitur cerita.</summary>
    public class StoryFeatureHost : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<StoryFeatureHost>() != null) return;
            var go = new GameObject("StoryFeatureHost"); DontDestroyOnLoad(go); go.AddComponent<StoryFeatureHost>();
        }

        void Update()
        {
            if (GameState.I == null || TownUI.I == null || TownUI.I.PanelOpen || Cutscene.Active || MiniGame.Active) return;
            bool n = false;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; n = k != null && k[Key.N].wasPressedThisFrame;
#else
            n = Input.GetKeyDown(KeyCode.N);
#endif
            if (n) NameBook.Open();
        }

        /// <summary>Hitung hari berlalu untuk ritual harian (satu per hari permainan).</summary>
        public static bool DailyDone(string key) { var g = GameState.I; return g != null && g.flags.Contains(key + "|" + g.day); }
        public static void DailyMark(string key) { var g = GameState.I; if (g == null) return; g.flags.Add(key + "|" + g.day); }
    }

    /// <summary>Tugu Jeda: ritual tujuh napas. Berdiri diam tujuh hitungan, lalu tubuh lebih tenang (damage diterima -12% selama 5 menit, stamina pulih). Sekali sehari.</summary>
    public class JedaStone : Interactable
    {
        bool running;
        void Awake() { prompt = "Tahan tujuh napas"; radius = 3.4f; }

        public override void Interact()
        {
            LoreDB.Unlock("nd_o_tugu"); WorldQuests.OnEvent("lore", "nd_o_tugu");
            if (running) return;
            if (StoryFeatureHost.DailyDone("jeda")) { TownUI.I.Toast("Napasmu sudah tenang hari ini. Datanglah besok."); return; }
            if (PlayerController.InFight()) { TownUI.I.Toast("Tidak bisa tenang selagi diserang."); return; }
            StartCoroutine(Ritual());
        }

        IEnumerator Ritual()
        {
            running = true;
            var pc = PlayerCombat.I; if (pc == null) { running = false; yield break; }
            Vector3 start = pc.transform.position;
            TownUI.I.Toast("Diam. Hitung napasmu, jangan bergerak.");
            for (int i = 1; i <= 7; i++)
            {
                yield return new WaitForSeconds(1.2f);
                if (pc == null || pc.IsDead || (pc.transform.position - start).sqrMagnitude > 2.2f * 2.2f || PlayerController.InFight())
                { TownUI.I.Toast("Hitunganmu terputus di napas ke-" + i + "."); running = false; yield break; }
                TownUI.I.Toast(i < 7 ? "... " + i : "Tujuh. Kosong.  (jangan isi)");
                Audio.Sfx("ui_open");
            }
            StoryFeatureHost.DailyMark("jeda");
            pc.Buff(0.88f, 300f);
            pc.stamina = pc.MaxStamina;
            TownUI.I.Toast("Napas Jeda: damage diterima -12% selama 5 menit.");
            running = false;
        }
    }

    /// <summary>Dinding Nama: tulis nama dari Buku Nama ke dinding. Tiap nama baru memberi EXP; kelipatan sepuluh memberi hadiah.</summary>
    public class NameWall : Interactable
    {
        const string W = "nw|";
        void Awake() { prompt = "Tulis nama di dinding"; radius = 3.4f; }
        static bool Written(string name) { var g = GameState.I; return g != null && g.flags.Contains(W + name); }
        static int WrittenCount { get { var g = GameState.I; int n = 0; foreach (var f in g.flags) if (f.StartsWith(W)) n++; return n; } }

        public override void Interact()
        {
            LoreDB.Unlock("nd_o_dinding"); WorldQuests.OnEvent("lore", "nd_o_dinding");
            Menu("Seorang anak menulisnya dengan kapur, dulu, karena tidak tahu cara lain untuk berkata aku di sini. Sekarang dindingnya penuh.");
        }

        void Menu(string text)
        {
            var ui = TownUI.I;
            ui.Open("Dinding Nama", () => text + "\n\nTertulis: " + WrittenCount + " nama.", () =>
            {
                var l = new List<UIEntry>();
                foreach (var e in NameBook.All())
                {
                    var en = e;
                    if (Written(en[0])) continue;
                    l.Add(new UIEntry("Tulis: " + en[0], () => Write(en[0])));
                }
                if (l.Count == 0) l.Add(new UIEntry("(tidak ada nama baru dari bukumu)", () => { }, false));
                l.Add(new UIEntry("Tutup", () => ui.Close()));
                return l;
            });
        }

        void Write(string name)
        {
            var g = GameState.I;
            g.flags.Add(W + name);
            g.AddExp(30 + g.level * 2);
            Audio.Sfx("quest_done");
            int n = WrittenCount; string extra = "";
            if (n % 10 == 0) { g.gold += 100 * n; extra = "\nKelipatan sepuluh: +" + (100 * n) + "G."; }
            if (n == 10) LoreDB.Unlock("nd_m_dinding", false);
            Menu("Kau menulis " + name + ". Tidak ada yang menghapusnya." + extra);
        }
    }

    /// <summary>Lilin Soriel: nyalakan satu lilin putih untuk yang pertama dibunuh. Memulihkan HP dan stamina. Sekali sehari per lilin.</summary>
    public class SorielCandle : Interactable
    {
        public string key = "lilin";
        void Awake() { prompt = "Nyalakan lilin"; radius = 3f; }

        public override void Interact()
        {
            LoreDB.Unlock("nd_o_lilin"); WorldQuests.OnEvent("lore", "nd_o_lilin");
            if (StoryFeatureHost.DailyDone(key)) { TownUI.I.Toast("Lilin ini masih menyala dari tadi. Datang lagi besok."); return; }
            StoryFeatureHost.DailyMark(key);
            var pc = PlayerCombat.I;
            if (pc != null) { pc.hp = pc.MaxHp; pc.stamina = pc.MaxStamina; }
            Audio.Sfx("quest_reward");
            TownUI.I.Toast("Lilin putih menyala. HP dan stamina pulih.");
        }
    }

    // =====================================================================================================
    // Pembuat objek dunia + kesadaran cerita pada dialog
    // =====================================================================================================
    public static class StoryFeatures
    {
        static GameObject Prim(PrimitiveType t, Transform par, Vector3 lp, Vector3 sc, Color c)
        {
            var g = GameObject.CreatePrimitive(t);
            var col = g.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
            g.transform.SetParent(par, false); g.transform.localPosition = lp; g.transform.localScale = sc;
            g.GetComponent<Renderer>().sharedMaterial = CombatFx.Solid(null, c);
            return g;
        }
        static void Label(Transform par, string txt, float y, Color c)
        {
            var go = new GameObject("Label"); go.transform.SetParent(par, false); go.transform.localPosition = new Vector3(0f, y, 0f);
            var tm = go.AddComponent<TextMesh>(); tm.text = txt; tm.characterSize = 0.08f; tm.fontSize = 48; tm.anchor = TextAnchor.MiddleCenter; tm.color = c;
            go.AddComponent<Billboard>();
        }
        static void Glow(Transform par, Vector3 lp, Color c, float range, float inten)
        {
            var l = new GameObject("Cahaya"); l.transform.SetParent(par, false); l.transform.localPosition = lp;
            var lt = l.AddComponent<Light>(); lt.type = LightType.Point; lt.range = range; lt.intensity = inten; lt.color = c;
        }

        public static GameObject MakeJeda(StorySpawn sp, Vector3 p)
        {
            var go = new GameObject("TuguJeda"); go.transform.position = p;
            var stone = new Color(0.62f, 0.62f, 0.66f);
            for (int i = 0; i < 7; i++)                                   // tujuh batu melingkar
            {
                float a = i * Mathf.PI * 2f / 7f;
                var s = Prim(PrimitiveType.Cube, go.transform, new Vector3(Mathf.Sin(a) * 1.6f, 0.9f, Mathf.Cos(a) * 1.6f), new Vector3(0.5f, 1.8f, 0.5f), stone);
                s.transform.localRotation = Quaternion.Euler(0f, a * Mathf.Rad2Deg, 0f);
            }
            Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0f, 0.06f, 0f), new Vector3(2.6f, 0.06f, 2.6f), new Color(0.35f, 0.35f, 0.4f));
            Glow(go.transform, new Vector3(0f, 1.4f, 0f), new Color(1f, 0.95f, 0.75f), 8f, 0.9f);
            Label(go.transform, string.IsNullOrEmpty(sp.name) ? "Tugu Jeda" : sp.name, 2.6f, new Color(1f, 0.95f, 0.7f));
            go.AddComponent<JedaStone>();
            return go;
        }

        public static GameObject MakeWall(StorySpawn sp, Vector3 p)
        {
            var go = new GameObject("DindingNama"); go.transform.position = p;
            Prim(PrimitiveType.Cube, go.transform, new Vector3(0f, 1.5f, 0f), new Vector3(7f, 3f, 0.5f), new Color(0.55f, 0.52f, 0.5f));
            for (int i = 0; i < 14; i++)                                   // coretan kapur
                Prim(PrimitiveType.Cube, go.transform, new Vector3(-3f + (i % 7) * 1f, 0.8f + (i / 7) * 1.1f, -0.27f), new Vector3(0.5f, 0.05f, 0.02f), new Color(0.95f, 0.95f, 0.9f));
            Label(go.transform, string.IsNullOrEmpty(sp.name) ? "Dinding Nama" : sp.name, 3.5f, new Color(0.9f, 0.95f, 1f));
            go.AddComponent<NameWall>();
            return go;
        }

        public static GameObject MakeCandle(StorySpawn sp, Vector3 p)
        {
            var go = new GameObject("LilinSoriel"); go.transform.position = p;
            Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0f, 0.3f, 0f), new Vector3(0.9f, 0.3f, 0.9f), new Color(0.6f, 0.6f, 0.62f));
            Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0f, 0.75f, 0f), new Vector3(0.18f, 0.28f, 0.18f), new Color(0.97f, 0.97f, 0.95f));
            Prim(PrimitiveType.Sphere, go.transform, new Vector3(0f, 1.1f, 0f), new Vector3(0.12f, 0.2f, 0.12f), new Color(1f, 0.85f, 0.4f));
            Glow(go.transform, new Vector3(0f, 1.3f, 0f), new Color(1f, 0.85f, 0.5f), 7f, 1.1f);
            Label(go.transform, string.IsNullOrEmpty(sp.name) ? "Lilin Soriel" : sp.name, 2.0f, new Color(1f, 0.95f, 0.8f));
            var c = go.AddComponent<SorielCandle>(); c.key = "lilin_" + sp.id;
            return go;
        }
    }

    /// <summary>Kalimat warga yang menyesuaikan perkembangan cerita pemain (dipakai NpcDialogueExtra / NpcTalk).</summary>
    public static class StoryAwareness
    {
        static bool Done(string q) { var g = GameState.I; return g != null && g.flags.Contains("wq_done_" + q); }

        public static List<string> Lines()
        {
            var l = new List<string>();
            if (GameState.I == null) return l;
            if (Done("nd_k4")) { l.Add("Anak dari Desa Pemula itu katanya membawa buku catatan ke mana-mana. Menulis nama, bukan peta."); l.Add("Ada yang mengaku melihat pemuda berjaket tambalan tujuh di gerbang selatan. Tidak ada yang berani bertanya namanya."); }
            if (Done("nd_k5")) { l.Add("Papan hitam di tiap sudut itu bikin aku menunduk tiap lewat. Kau juga, kan? Jangan bohong."); l.Add("Penjaga Jeda mengetuk lonceng kosongnya lagi tadi pagi. Satu nada pun tidak keluar. Justru itu yang bikin merinding."); }
            if (Done("nd_k7")) { l.Add("Orang-orang ladang barat itu... ada yang menamai tetangganya satu per satu supaya tidak lupa. Kudengar Hunter muda itu ikut mencatat."); }
            if (Done("nd_k9")) { l.Add("Kafilah yang hilang itu ketemu, katanya. Sebagian orangnya masih belum tahu namanya sendiri."); l.Add("Tiga puluh satu orang tidur di gudang belakang Guild. Brenna marah-marah, tapi dialah yang paling rajin membawakan selimut."); }
            if (Done("nd_k11")) { l.Add("Merana bangun lagi. Ayam jantannya berkokok setelah tiga bulan. Orang di pasar sampai menangis mendengarnya."); }
            if (Done("nd_k12")) { l.Add("Empat puluh satu orang dikubur di belakang Katedral. Aku ikut berdiri di sana dan tidak berani menarik napas keras."); l.Add("Hukum darurat itu tidak ada gunanya kalau penjaganya sendiri tidak membawa Telinga Jeda."); }
            if (Done("nd_k14")) { l.Add("Eldrath punya mulut di jalan-jalannya, kata pedagang yang baru pulang. Jangan injak retakan yang berbentuk kelopak."); }
            if (Done("nd_k15")) { l.Add("Odran Vael sudah tidak ada. Orang-orang Guild jadi lebih pendiam, dan tehnya Hesta jadi lebih pahit."); l.Add("Katanya dia gugur melindungi seorang anak berpecahan cahaya. Aku tidak tahu mana yang benar, tapi semua orang sepakat dia tidak lari."); }
            if (Done("nd_k16")) { l.Add("Padang Tulang. Tidak ada lagi yang mau lewat sana, kecuali kau ya. Hati-hati dengan obor yang bukan milik manusia."); }
            if (Done("nd_k17")) { l.Add("Katedral rata dengan tanah. Kami semua sempat berjalan dalam tidur malam itu. Aku bangun di depan sumur, tidak ingat kenapa."); l.Add("Semalam semua lilin di kota dinyalakan. Aku tidak punya lilin, jadi kunyalakan lampu kamar dan kubiarkan jendelanya terbuka."); }
            if (Done("nd_k18")) { l.Add("Balai Nama dibuka. Kau sudah menulis satu nama di sana? Aku menulis nama ibuku. Tidak ada yang menghapusnya."); l.Add("Ada yang bilang Hunter itu tidak tidur karena ada yang mengetuk dari dalam. Aku tidak mau menanyakannya. Aku bawakan sup saja."); }
            return l;
        }
    }
}
