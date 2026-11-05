using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>Quest Investigation: terima kasus di Papan Pengumuman, cari 3 petunjuk berkilau di area kasus, lalu tuduh pelaku yang cocok dengan petunjuk.</summary>
    public static class Investigation
    {
        public class Suspect { public string name, traits; }
        public class Case
        {
            public string id, title, brief, scene, place, after; public string[] clues, witness; public Suspect[] suspects; public int culprit, gold, exp;
        }

        public static readonly Case[] Cases =
        {
            new Case { id = "lumbung", title = "Lumbung yang Dijarah", scene = "StartVillage", place = "Desa Pemula", gold = 120, exp = 40, culprit = 1,
                witness = new[] { "Sebelum fajar aku melihat bayangan kecil bergegas ke jalan pedagang sambil memanggul karung. Bukan orang bertubuh besar, aku yakin.", "Holt berjaga di gerbang semalaman bersamaku. Separuh desa melihatnya di sana." },
                brief = "Lumbung desa dijarah semalam: sekarung gandum dan seekor ayam hilang. Warga saling curiga. Cari petunjuk di sekitar desa lalu tentukan pelakunya.",
                clues = new[] { "Di tanah dekat lumbung ada jejak sepatu kecil berlumpur, jelas lebih kecil dari sepatu pria dewasa.", "Tersangkut di paku pagar: serat kain biru tua yang halus, bukan kain kasar milik penduduk desa.", "Karung yang ditinggalkan berbau rempah-rempah, bau dagangan keliling." },
                suspects = new[] { new Suspect { name = "Doran, petani", traits = "Bot kulit besar. Mantel coklat kasar. Tidak pernah keluar desa." }, new Suspect { name = "Mira, pedagang keliling", traits = "Sepatu kecil. Kerudung biru tua halus. Membawa rempah dagangan." }, new Suspect { name = "Holt, penjaga", traits = "Sepatu besar. Jubah biru tua. Berbau minyak senjata." } } },
            new Case { id = "kafilah", title = "Kafilah yang Hilang", scene = "HuntArea_G", place = "Hutan Lerai", gold = 200, exp = 60, culprit = 2,
                witness = new[] { "Kurt sempat bertanya padaku soal jadwal kafilah dua hari sebelumnya. Katanya sekadar penasaran, tapi ia menanyakan isi muatannya juga.", "Elwen bersamaku di kedai sepanjang malam sebelum kafilah berangkat. Dan busur panahnya sudah hilang sejak seminggu lalu." },
                brief = "Sebuah kafilah dagang lenyap di jalan hutan. Muatan dan barangnya raib, tetapi tidak ada mayat. Telusuri jalan hutan dan cari tahu siapa dalangnya.",
                clues = new[] { "Roda kereta meninggalkan bekas sangat dalam: muatan jauh lebih berat dari yang tercatat di manifes. Pelakunya tahu isi muatan sebenarnya.", "Jejak kuda tunggang yang sengaja disembunyikan keluar dari semak di cabang jalan. Pelakunya punya kuda.", "Sebatang panah berbulu elang hijau patah di tanah, tetapi ujungnya bersih dan tidak pernah ditembakkan. Itu umpan untuk menuduh pemanah." },
                suspects = new[] { new Suspect { name = "Brandt, kusir", traits = "Tidak punya kuda tunggang. Hanya tahu manifes tertulis (ia buta huruf)." }, new Suspect { name = "Elwen, pemandu elf", traits = "Pemanah, memakai bulu elang hijau. Tidak tahu isi muatan. Tidak punya kuda." }, new Suspect { name = "Kurt, pengawal", traits = "Punya kuda tunggang. Tahu isi muatan. Menyimpan bulu elang hijau sebagai koleksi." } } },
            new Case { id = "kunci", title = "Kunci Gudang Guild", scene = "Town", place = "Kota Ashwatch", gold = 160, exp = 50, culprit = 1,
                witness = new[] { "Menjelang tengah malam aku melihat seseorang berjubah putih keluar dari arah gudang sambil membawa karung berat.", "Boris tidur mendengkur di pos jaga sampai pagi. Aku sendiri yang membangunkannya." },
                brief = "Gudang bahan Guild kehilangan sejumlah besi dan permata, padahal gemboknya tidak rusak. Periksa sekitar kota untuk mencari jejaknya.",
                clues = new[] { "Gembok gudang tidak rusak: dibuka dengan kunci asli, jadi pelakunya memegang kunci.", "Di lantai ada tetes lilin biru, jenis yang hanya dipakai di kapel.", "Catatan jaga malam: hanya satu pemegang kunci yang berada di kapel malam itu." },
                suspects = new[] { new Suspect { name = "Boris, penjaga gudang", traits = "Pemegang kunci utama. Tidak pernah ke kapel. Tidur di pos malam itu." }, new Suspect { name = "Bruder Anselm, pendeta", traits = "Memegang kunci cadangan. Memimpin doa malam di kapel." }, new Suspect { name = "Vera, penilai", traits = "Tidak punya kunci. Rajin ke kapel, tetapi pulang sebelum tengah malam." } } },
            new Case { id = "ladang", title = "Ladang yang Dibakar", scene = "StartVillage", place = "Desa Pemula", after = "lumbung", gold = 180, exp = 60, culprit = 0,
                witness = new[] { "Aku melihat nyala kecil bergerak cepat di ladang, lalu seseorang berlari ke arah sungai dan menyeberang.", "Tadi pagi ada orang mencuci tangan di sungai dan bajunya berbau minyak." },
                brief = "Setelah kasus lumbung, ladang di tepi desa terbakar tengah malam. Seseorang tampaknya ingin menutupi sesuatu. Cari tahu siapa yang menyalakan api.",
                clues = new[] { "Di dekat ladang tercium minyak lampu, bukan jerami terbakar biasa. Api sengaja disiram.", "Pecahan botol minyak bercap pedagang keliling tertinggal di parit.", "Bekas telapak sepatu kecil yang sama seperti di kasus lumbung, tetapi kini disertai bekas lutut: pelaku sempat berlutut menyalakan api." },
                suspects = new[] { new Suspect { name = "Tarn, anak penggembala", traits = "Sepatu kecil. Biasa berlutut di ladang. Menyimpan botol minyak untuk lampu domba." },new Suspect { name = "Doran, petani", traits = "Sepatu besar. Ladangnya ikut terbakar. Tidak punya minyak." },new Suspect { name = "Mira, pedagang keliling", traits = "Sepatu bot sedang. Menjual minyak, tetapi sedang di kota semalam." } } },
            new Case { id = "peta", title = "Peta Perampok", scene = "HuntArea_G", place = "Hutan Lerai", after = "kafilah", gold = 260, exp = 80, culprit = 1,
                witness = new[] { "Pencatat kafilah selalu menulis dengan tangan kiri, dan baunya seperti cendana.", "Pengawal kafilah tidak bisa menulis sama sekali." },
                brief = "Dalang hilangnya kafilah sudah ditangkap, tetapi ia hanya pion. Peta jalur rahasia ditemukan dan tersebar di hutan. Cari tahu siapa yang menggambarnya.",
                clues = new[] { "Peta digambar dengan arang dan tangan kidal: guratan miring ke kiri.", "Kertas peta berasal dari manifes dagang resmi, hanya orang dalam kafilah yang memegangnya.", "Di tepi peta ada noda minyak wangi cendana yang sangat khas." },
                suspects = new[] { new Suspect { name = "Hurd, pengawal", traits = "Tidak bisa menulis. Tangan kanan. Bau keringat dan kulit." },new Suspect { name = "Seno, pencatat kafilah", traits = "Tangan kidal. Memegang manifes. Selalu memakai wewangian cendana." },new Suspect { name = "Lysa, tabib keliling", traits = "Tangan kanan. Tidak memegang manifes. Bau ramuan herbal." } } },
            new Case { id = "lelang", title = "Lelang Gelap", scene = "Town", place = "Kota Ashwatch", after = "kunci", gold = 300, exp = 100, culprit = 2,
                witness = new[] { "Pengurus lelang selalu berjubah putih dan memakai cincin bermahkota.", "Orang itu keluar masuk kapel lewat pintu samping setiap malam." },
                brief = "Besi dan permata Guild yang dicuri muncul di lelang gelap malam hari di belakang pasar. Telusuri kota dan temukan siapa pengelola lelang itu.",
                clues = new[] { "Papan lelang ditulis dengan tinta biru yang sama dengan tetes lilin di kasus kunci gudang.", "Daftar peserta memuat cap cincin bermahkota kecil, lambang keluarga bangsawan lama.", "Koin pembayaran dibungkus kain putih berbau dupa kapel." },
                suspects = new[] { new Suspect { name = "Boris, penjaga gudang", traits = "Tidak punya cincin. Tidak pernah ke kapel. Tinta tidak pernah dipakainya." },new Suspect { name = "Bruder pelayan kapel", traits = "Berjubah putih, tetapi tidak mampu membeli cincin. Tinta kapel biasa." },new Suspect { name = "Valen, pewaris bangsawan", traits = "Memakai cincin bermahkota. Berjubah putih. Rutin ke kapel lewat pintu samping." } } },
            new Case { id = "dalang", title = "Dalang di Balik Bayangan", scene = "Town", place = "Kota Ashwatch", after = "lelang", gold = 450, exp = 150, culprit = 0,
                witness = new[] { "Kepala arsip sering lembur sendirian, dan ia satu-satunya yang punya akses stempel administrasi.", "Juru bayar mengaku tidak pernah masuk ruang arsip." },
                brief = "Pewaris bangsawan hanya menjual barang. Seseorang yang lebih tinggi mendanai semuanya, dan jejaknya mengarah ke dalam Guild sendiri. Kasus terakhir: temukan dalangnya.",
                clues = new[] { "Catatan pembayaran lelang ditandatangani dengan huruf yang hanya dipakai di stempel administrasi Guild.", "Seseorang menukar kunci gudang dua minggu sebelum pencurian, tepat saat jadwal kunci diperbarui.", "Surat perintah palsu memakai kertas resmi Guild, hanya tersedia di ruang arsip." },
                suspects = new[] { new Suspect { name = "Kepala Arsip Guild", traits = "Akses stempel administrasi. Mengurus jadwal kunci. Memegang kertas resmi." },new Suspect { name = "Juru bayar Guild", traits = "Tidak punya akses arsip. Hanya membawa uang kas." },new Suspect { name = "Penjaga gerbang kota", traits = "Tidak punya akses Guild. Hanya berjaga di gerbang." } } },
        };

        // ---- status: 0 belum, 1 diterima, 2 selesai ----
        static string K(Case c, string w) { return "inv_" + c.id + "_" + w; }
        public static bool Unlocked(Case c)
        {
            if (string.IsNullOrEmpty(c.after)) return true;
            foreach (var o in Cases) if (o.id == c.after) return State(o) == 2;
            return true;
        }
        public static string AfterTitle(Case c) { foreach (var o in Cases) if (o.id == c.after) return o.title; return ""; }
        public static int State(Case c) { return GameState.I.Stat(K(c, "s")); }
        public static bool Found(Case c, int i) { return GameState.I.Stat(K(c, "c" + i)) == 1; }
        public static bool Asked(Case c, int k) { return GameState.I.Stat(K(c, "w" + k)) == 1; }
        public static int AskedCount(Case c) { int n = 0; for (int k = 0; k < c.witness.Length; k++) if (Asked(c, k)) n++; return n; }

        /// <summary>NPC saksi (dipilih tetap dari NPC bernama di scene kasus). null bila belum ada cukup NPC.</summary>
        public static NpcActor WitnessNpc(Case c, int k)
        {
            if (SceneManager.GetActiveScene().name != c.scene) return null;
            var l = Mail.Named(); if (l.Count < 3) return null;
            l.Sort((a, b) => string.CompareOrdinal(a.displayName, b.displayName));
            var seen = new List<string>(); var uniq = new List<NpcActor>(); foreach (var n in l) if (!seen.Contains(n.displayName)) { seen.Add(n.displayName); uniq.Add(n); }
            if (uniq.Count < 3) return null;
            int i0 = Mail.Hash(c.id + "w0") % uniq.Count, i1 = Mail.Hash(c.id + "w1") % uniq.Count; if (i1 == i0) i1 = (i1 + 1) % uniq.Count;
            return uniq[k == 0 ? i0 : i1];
        }

        /// <summary>Entri menu "tanya soal kasus" untuk NPC ini (kosong bila bukan saksi kasus aktif).</summary>
        public static List<UIEntry> WitnessEntries(NpcActor npc, System.Action back)
        {
            var l = new List<UIEntry>(); if (npc == null || GameState.I == null) return l;
            foreach (var cc in Cases)
            {
                var c = cc; if (State(c) != 1) continue;
                for (int kk = 0; kk < c.witness.Length; kk++)
                {
                    int k = kk; if (WitnessNpc(c, k) != npc) continue;
                    l.Add(new UIEntry("Tanya soal kasus: " + c.title + (Asked(c, k) ? "  (sudah)" : ""), () =>
                    {
                        GameState.I.stats[K(c, "w" + k)] = 1; Audio.Sfx("quest_progress");
                        TownUI.I.Open(npc.displayName + " - kesaksian", () => c.witness[k] + "\n\n(Kesaksian " + AskedCount(c) + "/" + c.witness.Length + " tercatat. Mewawancarai semua saksi = bonus hadiah 25%.)", () => { var q = new List<UIEntry>(); q.Add(new UIEntry("Terima kasih", () => { if (back != null) back(); else TownUI.I.Close(); })); return q; });
                    }));
                }
            }
            return l;
        }

        static float mpNext; static List<KeyValuePair<Vector3, string>> mpCache = new List<KeyValuePair<Vector3, string>>();
        public static List<KeyValuePair<Vector3, string>> MapPoints()
        {
            if (Time.time < mpNext) return mpCache;
            mpNext = Time.time + 0.5f;
            var res = new List<KeyValuePair<Vector3, string>>(); mpCache = res; var gs = GameState.I; if (gs == null) return res;
            string sc = SceneManager.GetActiveScene().name;
            foreach (var c in Cases)
            {
                if (State(c) != 1 || c.scene != sc) continue;
                foreach (var s in Object.FindObjectsByType<ClueSpot>(FindObjectsSortMode.None)) if (s != null && s.c == c && s.gameObject.activeInHierarchy) res.Add(new KeyValuePair<Vector3, string>(s.transform.position, "Petunjuk"));
                for (int k = 0; k < c.witness.Length; k++) if (!Asked(c, k)) { var w = WitnessNpc(c, k); if (w != null) res.Add(new KeyValuePair<Vector3, string>(w.transform.position, "Saksi: " + w.displayName)); }
            }
            return res;
        }

        public static int FoundCount(Case c) { int n = 0; for (int i = 0; i < c.clues.Length; i++) if (Found(c, i)) n++; return n; }

        static List<string> AskedWitness(Case c) { var l = new List<string>(); for (int k = 0; k < c.witness.Length; k++) if (Asked(c, k)) l.Add("Saksi: " + c.witness[k]); return l; }

        public static List<string> TrackerLines()
        {
            var l = new List<string>(); var gs = GameState.I; if (gs == null) return l;
            foreach (var c in Cases) if (State(c) == 1) l.Add("Investigasi: " + c.title + " " + FoundCount(c) + "/" + c.clues.Length + (FoundCount(c) >= c.clues.Length ? " - tuduh pelaku di Papan" : " (" + c.place + ")"));
            return l;
        }

        // ---- menu (dipanggil dari Papan Pengumuman) ----
        public static void OpenBoard(System.Action back)
        {
            var ui = TownUI.I; var gs = GameState.I;
            ui.Open("Kasus Investigasi",
                () => "Selidiki kasus: terima, cari petunjuk berkilau di lokasi, lalu tuduh pelaku. Salah menuduh = denda 30g.",
                () =>
                {
                    var l = new List<UIEntry>();
                    foreach (var cc in Cases)
                    {
                        var c = cc; int st = State(c);
                        string tag = !Unlocked(c) ? "[Terkunci] " : st == 2 ? "[Selesai] " : st == 1 ? "[" + FoundCount(c) + "/" + c.clues.Length + "] " : "[Baru] ";
                        l.Add(new UIEntry(tag + c.title + "  (" + c.place + ")  hadiah " + c.gold + "g" + (Unlocked(c) ? "" : "  (selesaikan: " + AfterTitle(c) + ")"), () => OpenCase(c, () => OpenBoard(back)), st != 2 && Unlocked(c)));
                    }
                    l.Add(new UIEntry("< Kembali", () => { if (back != null) back(); }));
                    return l;
                });
        }

        static void OpenCase(Case c, System.Action back)
        {
            var ui = TownUI.I; var gs = GameState.I;
            ui.Open(c.title,
                () =>
                {
                    var sb = new System.Text.StringBuilder(c.brief + "\nLokasi: " + c.place + "\n");
                    if (State(c) == 1) { sb.Append("\nCatatan petunjuk:\n"); for (int i = 0; i < c.clues.Length; i++) sb.Append(Found(c, i) ? "- " + c.clues[i] + "\n" : "- (belum ditemukan)\n"); sb.Append("\nKesaksian (tanya warga di " + c.place + "):\n"); for (int k = 0; k < c.witness.Length; k++) sb.Append(Asked(c, k) ? "- " + c.witness[k] + "\n" : "- (saksi " + (k + 1) + " belum ditanya)\n"); }
                    return sb.ToString();
                },
                () =>
                {
                    var l = new List<UIEntry>();
                    if (State(c) == 0) l.Add(new UIEntry("Terima kasus ini", () => { gs.stats[K(c, "s")] = 1; Audio.Sfx("quest_accept"); ui.Toast("Kasus diterima: " + c.title + ". Pergi ke " + c.place + " dan cari titik berkilau kuning."); OpenCase(c, back); }));
                    if (State(c) == 1)
                    {
                        bool all = FoundCount(c) >= c.clues.Length;
                        l.Add(new UIEntry(all ? "Tuduh pelaku >" : "Tuduh pelaku (cari semua petunjuk dulu)", () => OpenAccuse(c, back), all));
                        l.Add(new UIEntry("Batalkan kasus", () => { gs.stats[K(c, "s")] = 0; for (int i = 0; i < c.clues.Length; i++) gs.stats.Remove(K(c, "c" + i)); OpenCase(c, back); }));
                    }
                    l.Add(new UIEntry("< Kembali", () => { if (back != null) back(); }));
                    return l;
                });
        }

        static void OpenAccuse(Case c, System.Action back)
        {
            var ui = TownUI.I; var gs = GameState.I;
            ui.Open("Siapa pelakunya?",
                () => "Cocokkan petunjuk dengan ciri tiap tersangka.\n\n" + string.Join("\n", c.clues) + "\n" + string.Join("\n", AskedWitness(c)),
                () =>
                {
                    var l = new List<UIEntry>();
                    for (int i = 0; i < c.suspects.Length; i++)
                    {
                        int k = i; var s = c.suspects[i];
                        l.Add(new UIEntry(s.name + "\n" + s.traits, () =>
                        {
                            if (k == c.culprit)
                            {
                                bool full = AskedCount(c) >= c.witness.Length; int gold = full ? Mathf.RoundToInt(c.gold * 1.25f) : c.gold;
                                gs.stats[K(c, "s")] = 2; gs.gold += gold; gs.AddExp(c.exp); gs.AddStat("cases");
                                Audio.Sfx("quest_reward"); ui.Toast("Benar! " + s.name + " pelakunya. +" + gold + "g" + (full ? " (bonus saksi)" : "") + ", +" + c.exp + " EXP");
                                OpenBoard(back);
                            }
                            else
                            {
                                gs.gold = Mathf.Max(0, gs.gold - 30); Audio.Sfx("defeat"); ui.Toast("Salah tuduh! Denda 30g. Periksa lagi petunjuknya.");
                                OpenCase(c, back);
                            }
                        }));
                    }
                    l.Add(new UIEntry("< Kembali", () => OpenCase(c, back)));
                    return l;
                });
        }

        // ---- petunjuk di dunia ----
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { SceneManager.sceneLoaded -= OnLoaded; SceneManager.sceneLoaded += OnLoaded; Install(SceneManager.GetActiveScene()); }
        static void OnLoaded(Scene s, LoadSceneMode m) { Install(s); }
        static void Install(Scene s)
        {
            foreach (var c in Cases) if (c.scene == s.name && Object.FindFirstObjectByType<ClueSpawner>() == null) { new GameObject("ClueSpawner").AddComponent<ClueSpawner>(); return; }
        }
    }

    public class ClueSpawner : MonoBehaviour
    {
        float born; bool done; readonly List<ClueSpot> spots = new List<ClueSpot>();
        void Start() { born = Time.time; }
        void Update()
        {
            var gs = GameState.I; if (gs == null) return;
            if (!done)
            {
                if (Time.time - born < 5f) return;
                var p = GameObject.FindGameObjectWithTag("Player"); if (p == null) return;
                done = true;
                var anchors = new List<Vector3>();
                foreach (var it in Interactable.All) if (it != null && !(it is ClueSpot) && !(it is OreNode) && !(it is FarmPlot) && Vector3.Distance(it.transform.position, p.transform.position) < 140f) anchors.Add(it.transform.position);
                string scene = SceneManager.GetActiveScene().name;
                var anchorGo = GameObject.Find(StarterConfig.VillageSpawn);
                Vector3 anchor = scene == StarterConfig.VillageScene && anchorGo != null ? anchorGo.transform.position : p.transform.position;
                foreach (var c in Investigation.Cases)
                {
                    if (c.scene != scene) continue;
                    var rng = new System.Random(c.id.GetHashCode() * 31 + 5);
                    var placed = new List<Vector3>();
                    for (int i = 0; i < c.clues.Length; i++)
                    {
                        for (int a = 0; a < 300; a++)
                        {
                            // dekat titik interaksi yang ada (jalan/bangunan/pos) supaya pasti terjangkau; cadangan: sekitar titik awal
                            Vector3 basePt = anchors.Count >= 3 ? anchors[rng.Next(anchors.Count)] : anchor;
                            double ang = rng.NextDouble() * 6.2832, rad = anchors.Count >= 3 ? 5 + rng.NextDouble() * 8 : 14 + rng.NextDouble() * 50;
                            Vector3 q = basePt + new Vector3((float)(System.Math.Cos(ang) * rad), 0f, (float)(System.Math.Sin(ang) * rad)), g;
                            if (!FarmAnimals.Ground(q, out g)) continue;
                            float sy; if (WaterZone.Query(new Vector3(g.x, g.y, g.z), out sy) && sy > g.y - 0.3f) continue;
                            bool near = false; foreach (var pp in placed) if ((pp - g).sqrMagnitude < 14f * 14f) near = true;
                            if (near) continue;
                            placed.Add(g); spots.Add(ClueSpot.Make(c, i, g)); break;
                        }
                    }
                }
            }
            foreach (var s in spots) if (s != null) s.gameObject.SetActive(Investigation.State(s.c) == 1 && !Investigation.Found(s.c, s.i));
        }
    }

    public class ClueSpot : Interactable
    {
        public Investigation.Case c; public int i; GameObject orb;
        public static ClueSpot Make(Investigation.Case c, int i, Vector3 pos)
        {
            var go = new GameObject("Clue_" + c.id + i); go.transform.position = pos;
            var s = go.AddComponent<ClueSpot>(); s.c = c; s.i = i; s.radius = 2.4f; s.prompt = "Periksa petunjuk";
            s.orb = RT.Prim(PrimitiveType.Sphere, go.transform, new Vector3(0, 0.7f, 0), Vector3.one * 0.35f, new Color(1f, 0.9f, 0.3f));
            RT.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0, 0.02f, 0), new Vector3(1.1f, 0.02f, 1.1f), new Color(1f, 0.85f, 0.25f));
            return s;
        }
        float t;
        void Update() { t += Time.deltaTime; if (orb != null) { orb.transform.localPosition = new Vector3(0, 0.75f + Mathf.Sin(t * 2.5f) * 0.12f, 0); orb.transform.localScale = Vector3.one * (0.32f + Mathf.Sin(t * 5f) * 0.04f); } if (Mathf.Repeat(t, 1.6f) < 0.03f) CombatFx.Burst(transform.position + Vector3.up * 0.6f, new Color(1f, 0.92f, 0.4f), 3, 0.8f, 0.8f, 0.4f, 0.06f); }

        public override void Interact()
        {
            var gs = GameState.I; if (Investigation.State(c) != 1) return;
            gs.stats["inv_" + c.id + "_c" + i] = 1; Audio.Sfx("quest_progress", transform.position);
            string text = c.clues[i]; int n = Investigation.FoundCount(c);
            TownUI.I.Open("Petunjuk " + (i + 1), () => text + "\n\n(" + n + "/" + c.clues.Length + (n >= c.clues.Length ? " - semua petunjuk terkumpul! Tuduh pelaku di Papan Pengumuman Guild)" : ")"), () =>
            {
                var l = new List<UIEntry>(); l.Add(new UIEntry("Catat dan lanjut", () => TownUI.I.Close())); return l;
            });
            gameObject.SetActive(false);
        }
    }
}
