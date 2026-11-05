using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    // =====================================================================================================
    // FORESHADOWING (v35z102n77)
    // Tanda-tanda kecil di dunia (kind "tanda" di Resources/WorldStory.json) dan desas-desus warga yang menunjuk ke kejadian
    // di Kronik "Nama Yang Dilarang". Tiap tanda dapat diperiksa (E), membuka satu entri Arsip "Kronik - Pertanda", dan setelah
    // bab pembayarnya selesai entri itu mendapat satu halaman tambahan ("kini kau tahu").
    // Peta petunjuk -> pembayaran ada di Docs/LORE_BIBLE.md (bagian "Buku Besar Foreshadowing").
    // Aturan: petunjuk hanya menyiratkan; rahasia Arc 2-4 (nama empat wadah, kecemburuan Lucifer) tidak dibocorkan di sini.
    // =====================================================================================================

    /// <summary>Titik periksa foreshadowing. Mirip LoreObject, tetapi halaman penutupnya mengikuti progres cerita (StoryForeshadow.Reveal).</summary>
    public class FsMark : Interactable
    {
        public string loreId;
        void Awake() { prompt = "Periksa"; radius = 2.8f; }

        public override void Interact()
        {
            if (LoreDB.Get(loreId) == null) { TownUI.I.Toast("(Lore '" + loreId + "' tidak ada di WorldStory.json)"); return; }
            WorldQuests.UnlockLore(loreId); WorldQuests.OnEvent("lore", loreId);
            LoreCodex.Read(loreId, 0, null);
        }
    }

    public static class StoryForeshadow
    {
        // ------------------------------------------------------------------ buku besar: tanda -> (bab pembayar, halaman tambahan)
        sealed class Pay
        {
            public string quest, after;
            public Pay(string q, string a) { quest = q; after = a; }
        }

        static readonly Dictionary<string, Pay> Ledger = new Dictionary<string, Pay>
        {
            { "fs_sajak", new Pay("nd_k18", "Kini kau tahu baris yang dihapus itu: tutup mata.\n\nSeribu tahun anak-anak menurut. Mungkin itu kesalahan terbesar mereka.") },
            { "fs_hitung", new Pay("nd_k1", "Kabut itu memang tidak pernah mundur. Malam setelah Malam Tanpa Nama ia maju satu langkah, dan paginya seseorang menambah satu guratan pucat.\n\nOrang Desa Pemula tidak menghitung hari. Mereka menghitung jarak.") },
            { "fs_putus", new Pay("nd_k12", "Sejak satu nama diucapkan lantang di depan Katedral, kau mengerti kenapa garis ini tidak boleh putus satu titik pun.\n\nGaram hanya menahan. Yang mengundang adalah mulut.") },
            { "fs_lonceng", new Pay("nd_k5", "Mira mencatat soal Telinga Jeda: tidak pernah diuji secara terbuka, kita hanya percaya pada warnanya.\n\nLonceng ini sama. Tak seorang pun pernah mendengar suaranya, jadi tak seorang pun tahu apakah ia berfungsi.") },
            { "fs_enam", new Pay("nd_k12", "Kini kau tahu ruang kosong itu bukan kelalaian pemahat.\n\nItu hukum yang ditulis dengan tidak menulis.") },
            { "fs_goresan", new Pay("nd_k13", "Di tembok Kantor Pendaki Eldrath kau melihat ribuan goresan dengan irama yang sama.\n\nMereka tidak mengetuk. Mereka menghitung.") },
            { "fs_lingkaran", new Pay("nd_k6", "Ringkasan Mira tentang serbuk putih menjawabnya: Lingkaran Hampa memakainya sebagai penanda di tempat yang ingin mereka dengar.\n\nGaram putih dalam garis yang tak putus menahan mereka, tetapi hanya sampai tiga puluh langkah.") },
            { "fs_retak", new Pay("nd_k14", "Di Eldrath kau melihat retakan seperti ini membuka enam lengkung ke luar, dan di bawahnya tidak ada tanah.\n\nYang ini masih sehalus rambut. Itu tidak berarti ia tidur.") },
        };

        static readonly HashSet<string> extended = new HashSet<string>();

        static bool Done(string q) { var g = GameState.I; return g != null && g.flags.Contains("wq_done_" + q); }

        /// <summary>Dipanggil LoreCodex.Read: bila bab pembayar tanda ini sudah selesai, tambahkan halaman "kini kau tahu" (sekali per sesi).</summary>
        public static void Reveal(string id)
        {
            Pay pay;
            if (string.IsNullOrEmpty(id) || extended.Contains(id) || !Ledger.TryGetValue(id, out pay) || !Done(pay.quest)) return;
            var e = LoreDB.Get(id); if (e == null || e.pages == null) return;
            var np = new string[e.pages.Length + 1];
            System.Array.Copy(e.pages, np, e.pages.Length);
            np[e.pages.Length] = pay.after;
            e.pages = np; extended.Add(id);
        }

        // ------------------------------------------------------------------ desas-desus warga (masuk ke kumpulan dialog NPC)
        /// <summary>Kabar burung yang menyiratkan hal yang kelak terjadi. Sebagian baru muncul setelah bab tertentu dan hilang setelah dibayar.</summary>
        public static List<string> Rumors()
        {
            var l = new List<string>();
            if (GameState.I == null) return l;
            l.Add("Garam di ambang pintu itu dua jari lebar, tidak boleh putus satu titik pun. Kata nenekku, garis yang putus bukan kecelakaan. Itu undangan.");
            l.Add("Anak-anak menyanyikan sajak aneh: si Sulung mengetuk pintu, jangan buka, jangan buka. Tak ada yang tahu siapa yang mengajarkannya, dan orang dewasa selalu berhenti di baris kedua.");
            l.Add("Kalau ada yang menyapamu lebih dulu di tepi hutan, jangan dijawab. Pesan nenekku, dan nenekku tidak pernah salah soal hal yang membuat orang hilang.");
            l.Add("Katedral berdiri di atas retakan, kata orang tua. Retakan apa, tak ada yang mau menjawab. Penggali sumur di sini selalu berhenti di kedalaman yang sama dan menolak naik upah.");
            l.Add("Jangan bersalaman dengan orang yang tak mau membuka kepalan tangannya. Kudengar ada lingkaran kosong di telapak mereka.");
            l.Add("Kabut di tepi Desa Pemula maju selangkah tiap malam. Para tetua menghitungnya di tiang dengan pisau, dan berhenti bicara kalau ditanya angkanya.");
            l.Add("Enam nama disebut, tujuh napas dikosongkan. Aku pernah mencoba menghitung napas yang kedelapan. Lilinku padam sendiri.");
            l.Add("Tukang batu mana pun menolak merapikan ruang kosong di batu enam nama itu. Bahkan yang bayarannya paling mahal.");
            l.Add("Ada yang menggores tiang gerbang dengan tiga goresan pendek, jeda, tiga lagi. Sudah bertahun-tahun, dan tak pernah ketahuan siapa. Penjaga membiarkannya.");
            if (Done("nd_k4") && !Done("nd_k13"))
                l.Add("Pedagang dari timur bilang Menara Eldrath tak punya pintu. Yang masuk, masuk karena menara membiarkannya.");
            if (Done("nd_k12") && !Done("nd_k18"))
                l.Add("Sejak malam itu aku mengunci pintu dua kali dan tidak lagi menjawab ketukan yang terlalu sopan.");
            if (Done("nd_k16") && !Done("nd_k18"))
                l.Add("Kudengar Hunter muda itu tak lagi menutup mata saat anak-anak menyanyikan sajak. Katanya ia menghitung bersama mereka.");
            return l;
        }

        // ------------------------------------------------------------------ pembuat objek dunia
        static readonly Color Wood = new Color(0.36f, 0.27f, 0.18f), WoodLight = new Color(0.47f, 0.36f, 0.24f), Dark = new Color(0.12f, 0.09f, 0.07f);
        static readonly Color Stone = new Color(0.50f, 0.50f, 0.54f), StoneDark = new Color(0.34f, 0.34f, 0.38f), Salt = new Color(0.95f, 0.95f, 0.90f);
        static readonly Color Chalk = new Color(0.95f, 0.95f, 0.90f), Brass = new Color(0.86f, 0.70f, 0.22f), Cord = new Color(0.65f, 0.12f, 0.10f);

        static GameObject Prim(PrimitiveType t, Transform par, Vector3 lp, Vector3 sc, Color c)
        {
            var g = GameObject.CreatePrimitive(t);
            var col = g.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
            g.transform.SetParent(par, false); g.transform.localPosition = lp; g.transform.localScale = sc;
            g.GetComponent<Renderer>().sharedMaterial = CombatFx.Solid(null, c);
            return g;
        }

        static GameObject PrimR(PrimitiveType t, Transform par, Vector3 lp, Vector3 sc, Vector3 euler, Color c)
        {
            var g = Prim(t, par, lp, sc, c); g.transform.localRotation = Quaternion.Euler(euler); return g;
        }

        static void Label(Transform par, string txt, float y, Color c)
        {
            var go = new GameObject("Label"); go.transform.SetParent(par, false); go.transform.localPosition = new Vector3(0f, y, 0f);
            var tm = go.AddComponent<TextMesh>(); tm.text = txt; tm.characterSize = 0.08f; tm.fontSize = 48; tm.anchor = TextAnchor.MiddleCenter; tm.color = c;
            go.AddComponent<Billboard>();
        }

        public static GameObject MakeMark(StorySpawn sp, Vector3 p)
        {
            var go = new GameObject("Tanda_" + sp.id); go.transform.position = p;
            int h = 0; foreach (char ch in sp.id) h = unchecked(h * 31 + ch);
            go.transform.rotation = Quaternion.Euler(0f, Mathf.Abs(h) % 360, 0f);
            float top;
            switch ((sp.role ?? "").ToLowerInvariant())
            {
                case "garam": top = Garam(go.transform); break;
                case "kabut": top = Hitung(go.transform); break;
                case "sajak": top = Sajak(go.transform); break;
                case "lonceng": top = Lonceng(go.transform); break;
                case "batu": top = Batu(go.transform); break;
                case "goresan": top = Goresan(go.transform); break;
                case "lingkaran": top = Lingkaran(go.transform); break;
                case "retak": top = Retak(go.transform); break;
                default: top = Cairn(go.transform); break;
            }
            Label(go.transform, string.IsNullOrEmpty(sp.name) ? "Pertanda" : sp.name, top + 0.55f, new Color(0.95f, 0.90f, 0.72f));
            var m = go.AddComponent<FsMark>(); m.loreId = sp.id;
            return go;
        }

        /// <summary>Ambang pintu tanpa rumah: garis garam putus selebar telapak tangan, keranjang tergeletak.</summary>
        static float Garam(Transform t)
        {
            Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.06f, 0f), new Vector3(3.0f, 0.12f, 0.5f), Wood);
            Prim(PrimitiveType.Cube, t, new Vector3(-0.95f, 0.14f, 0f), new Vector3(1.0f, 0.03f, 0.14f), Salt);
            Prim(PrimitiveType.Cube, t, new Vector3(0.95f, 0.14f, 0f), new Vector3(1.0f, 0.03f, 0.14f), Salt);
            Prim(PrimitiveType.Sphere, t, new Vector3(-0.34f, 0.14f, 0.05f), new Vector3(0.08f, 0.04f, 0.08f), Salt);
            Prim(PrimitiveType.Sphere, t, new Vector3(0.30f, 0.14f, -0.06f), new Vector3(0.07f, 0.04f, 0.07f), Salt);
            PrimR(PrimitiveType.Cylinder, t, new Vector3(1.9f, 0.26f, 0.7f), new Vector3(0.5f, 0.26f, 0.5f), new Vector3(0f, 0f, 82f), new Color(0.55f, 0.42f, 0.22f));
            Prim(PrimitiveType.Cube, t, new Vector3(1.55f, 0.03f, 0.55f), new Vector3(0.7f, 0.02f, 0.3f), Salt);
            return 0.9f;
        }

        /// <summary>Tiang hitung kabut: guratan pisau (tua gelap, terbaru pucat) dan kabut tipis di kaki.</summary>
        static float Hitung(Transform t)
        {
            Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 1.1f, 0f), new Vector3(0.22f, 1.1f, 0.22f), Wood);
            for (int i = 0; i < 10; i++)
            {
                Color c = i < 9 ? Dark : new Color(0.88f, 0.78f, 0.58f);
                float y = 0.55f + i * 0.15f;
                Prim(PrimitiveType.Cube, t, new Vector3(0f, y, 0.115f), new Vector3(0.30f, 0.04f, 0.03f), c);
                Prim(PrimitiveType.Cube, t, new Vector3(0f, y, -0.115f), new Vector3(0.30f, 0.04f, 0.03f), c);
            }
            var mist = new Color(0.80f, 0.84f, 0.90f, 0.22f);
            for (int k = 0; k < 5; k++)
            {
                float a = k * 72f * Mathf.Deg2Rad;
                Prim(PrimitiveType.Sphere, t, new Vector3(Mathf.Cos(a) * 1.4f, 0.22f, Mathf.Sin(a) * 1.4f), new Vector3(1.6f, 0.45f, 1.6f), mist);
            }
            return 2.3f;
        }

        /// <summary>Papan pagar dengan sajak kapur; baris terakhir dihapus jempol.</summary>
        static float Sajak(Transform t)
        {
            Prim(PrimitiveType.Cube, t, new Vector3(-1.3f, 0.6f, 0f), new Vector3(0.16f, 1.2f, 0.16f), Wood);
            Prim(PrimitiveType.Cube, t, new Vector3(1.3f, 0.6f, 0f), new Vector3(0.16f, 1.2f, 0.16f), Wood);
            Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.28f, 0f), new Vector3(2.8f, 0.1f, 0.08f), Wood);
            Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.9f, 0f), new Vector3(2.8f, 0.1f, 0.08f), Wood);
            Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.62f, 0f), new Vector3(2.4f, 0.5f, 0.06f), WoodLight);
            for (int r = 0; r < 3; r++)
                for (int j = 0; j < 6; j++)
                {
                    float w = 0.20f + ((j * 7 + r * 3) % 3) * 0.05f; float x = -0.95f + j * 0.38f; float y = 0.74f - r * 0.14f;
                    Prim(PrimitiveType.Cube, t, new Vector3(x, y, 0.035f), new Vector3(w, 0.04f, 0.01f), Chalk);
                    Prim(PrimitiveType.Cube, t, new Vector3(x, y, -0.035f), new Vector3(w, 0.04f, 0.01f), Chalk);
                }
            Prim(PrimitiveType.Cube, t, new Vector3(0.1f, 0.4f, 0.036f), new Vector3(1.1f, 0.05f, 0.005f), new Color(0.62f, 0.50f, 0.36f));      // bekas jempol
            Prim(PrimitiveType.Cube, t, new Vector3(0.1f, 0.4f, -0.036f), new Vector3(1.1f, 0.05f, 0.005f), new Color(0.62f, 0.50f, 0.36f));
            return 1.4f;
        }

        /// <summary>Lonceng kuningan Penjaga Jeda: lidahnya diikat tali merah.</summary>
        static float Lonceng(Transform t)
        {
            Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 1.5f, 0f), new Vector3(0.14f, 1.5f, 0.14f), Wood);
            Prim(PrimitiveType.Cube, t, new Vector3(0.5f, 2.95f, 0f), new Vector3(1.2f, 0.12f, 0.12f), Wood);
            Prim(PrimitiveType.Cylinder, t, new Vector3(0.95f, 2.8f, 0f), new Vector3(0.03f, 0.15f, 0.03f), Dark);
            Prim(PrimitiveType.Sphere, t, new Vector3(0.95f, 2.4f, 0f), new Vector3(0.62f, 0.7f, 0.62f), Brass);
            Prim(PrimitiveType.Cylinder, t, new Vector3(0.95f, 2.08f, 0f), new Vector3(0.72f, 0.03f, 0.72f), new Color(0.66f, 0.52f, 0.16f));
            Prim(PrimitiveType.Cylinder, t, new Vector3(0.95f, 1.98f, 0f), new Vector3(0.03f, 0.12f, 0.03f), Cord);
            Prim(PrimitiveType.Sphere, t, new Vector3(0.95f, 1.84f, 0f), new Vector3(0.14f, 0.14f, 0.14f), Dark);
            Prim(PrimitiveType.Cube, t, new Vector3(0.95f, 1.9f, 0f), new Vector3(0.20f, 0.05f, 0.05f), Cord);
            return 3.1f;
        }

        /// <summary>Batu enam nama: enam baris pahatan dan satu ceruk polos untuk nama yang tidak ditulis.</summary>
        static float Batu(Transform t)
        {
            Prim(PrimitiveType.Cube, t, new Vector3(0f, 1.0f, 0f), new Vector3(1.3f, 2.0f, 0.35f), Stone);
            Prim(PrimitiveType.Cube, t, new Vector3(0f, 2.05f, 0f), new Vector3(1.5f, 0.12f, 0.5f), StoneDark);
            for (int i = 0; i < 6; i++)
            {
                float y = 1.75f - i * 0.21f; float w = 0.62f + ((i * 5) % 3) * 0.12f;
                Color c = i < 2 ? new Color(0.56f, 0.56f, 0.60f) : new Color(0.82f, 0.82f, 0.85f);      // dua teratas aus
                Prim(PrimitiveType.Cube, t, new Vector3(0f, y, 0.178f), new Vector3(w, 0.06f, 0.02f), c);
                Prim(PrimitiveType.Cube, t, new Vector3(0f, y, -0.178f), new Vector3(w, 0.06f, 0.02f), c);
            }
            Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.45f, 0.178f), new Vector3(0.95f, 0.24f, 0.02f), new Color(0.20f, 0.20f, 0.23f));
            Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.45f, -0.178f), new Vector3(0.95f, 0.24f, 0.02f), new Color(0.20f, 0.20f, 0.23f));
            return 2.3f;
        }

        /// <summary>Tiang batu dengan tiga goresan, jeda, tiga goresan.</summary>
        static float Goresan(Transform t)
        {
            Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.9f, 0f), new Vector3(1.4f, 1.8f, 0.5f), new Color(0.46f, 0.45f, 0.47f));
            Prim(PrimitiveType.Cube, t, new Vector3(0f, 1.85f, 0f), new Vector3(1.6f, 0.12f, 0.6f), StoneDark);
            float[] xs = { -0.50f, -0.36f, -0.22f, 0.22f, 0.36f, 0.50f };
            for (int i = 0; i < xs.Length; i++)
            {
                float tilt = ((i * 5) % 3 - 1) * 3f;
                PrimR(PrimitiveType.Cube, t, new Vector3(xs[i], 1.05f, 0.255f), new Vector3(0.035f, 0.5f, 0.02f), new Vector3(0f, 0f, tilt), new Color(0.86f, 0.85f, 0.80f));
                PrimR(PrimitiveType.Cube, t, new Vector3(xs[i], 1.05f, -0.255f), new Vector3(0.035f, 0.5f, 0.02f), new Vector3(0f, 0f, tilt), new Color(0.86f, 0.85f, 0.80f));
            }
            return 2.1f;
        }

        /// <summary>Lingkaran serbuk putih halus di batu jalan; bagian dalamnya kosong.</summary>
        static float Lingkaran(Transform t)
        {
            Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.015f, 0f), new Vector3(1.9f, 0.01f, 1.9f), new Color(0.86f, 0.85f, 0.80f, 0.95f));
            Prim(PrimitiveType.Cylinder, t, new Vector3(0f, 0.025f, 0f), new Vector3(1.62f, 0.01f, 1.62f), new Color(0.15f, 0.14f, 0.14f, 0.95f));
            return 1.2f;
        }

        /// <summary>Retakan sehalus rambut dengan tiga cabang; hangat dan menyala redup.</summary>
        static float Retak(Transform t)
        {
            float[][] seg = { new[] { 0f, 0f, 1.3f, 8f }, new[] { 1.1f, 0.15f, 1.0f, -26f }, new[] { 1.9f, -0.05f, 1.2f, 14f }, new[] { -1.0f, -0.2f, 0.9f, -14f }, new[] { 2.4f, 0.55f, 0.6f, 52f } };
            foreach (var s in seg)
            {
                PrimR(PrimitiveType.Cube, t, new Vector3(s[0] - 0.4f, 0.02f, s[1]), new Vector3(s[2], 0.03f, 0.07f), new Vector3(0f, s[3], 0f), new Color(0.05f, 0.04f, 0.05f));
                PrimR(PrimitiveType.Cube, t, new Vector3(s[0] - 0.4f, 0.035f, s[1]), new Vector3(s[2] * 0.92f, 0.01f, 0.025f), new Vector3(0f, s[3], 0f), new Color(0.80f, 0.30f, 0.10f, 0.85f));
            }
            return 0.9f;
        }

        /// <summary>Cadangan untuk role yang tak dikenal: tumpukan batu kecil.</summary>
        static float Cairn(Transform t)
        {
            Prim(PrimitiveType.Sphere, t, new Vector3(0f, 0.2f, 0f), new Vector3(0.7f, 0.4f, 0.7f), Stone);
            Prim(PrimitiveType.Sphere, t, new Vector3(0.05f, 0.5f, 0f), new Vector3(0.5f, 0.3f, 0.5f), StoneDark);
            Prim(PrimitiveType.Sphere, t, new Vector3(0f, 0.72f, 0.02f), new Vector3(0.3f, 0.2f, 0.3f), Stone);
            return 1.0f;
        }
    }
}
