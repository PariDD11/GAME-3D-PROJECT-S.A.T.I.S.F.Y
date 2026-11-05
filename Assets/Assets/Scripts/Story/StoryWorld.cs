using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>NPC cerita: menawarkan world quest dan menerima laporan tahap "talk".</summary>
    public partial class StoryGiver : Interactable, IQuestGiver
    {
        public string npcId, npcName;
        void Awake() { prompt = "Bicara"; radius = 3f; }

        /// <summary>1 = ada quest tersedia dari NPC ini, 0 = tidak ada.</summary>
        public int QuestMarkState()
        {
            try
            {
                if (WorldStory.Root == null || WorldStory.Root.quests == null) return 0;
                foreach (var q in WorldStory.Root.quests) if (q != null && q.giver == npcId && WorldQuests.Available(q)) return 1;
            }
            catch { }
            return 0;
        }

        public override void Interact()
        {
            string name = string.IsNullOrEmpty(npcName) ? npcId : npcName;
            NameBook.Record(name, NoteFor(npcId));
            WorldQuests.OnEvent("talk", npcId);    // laporan tahap "talk" (bisa memajukan / menyelesaikan quest)
            foreach (var q in WorldStory.Root.quests)
            {
                if (q == null || q.giver != npcId || !WorldQuests.Done(q) || q.finishPages == null || q.finishPages.Length == 0) continue;
                if (GameState.I.flags.Contains("wq_fin_" + q.id)) continue;
                GameState.I.flags.Add("wq_fin_" + q.id);
                Pages(name, q.title, q.finishPages, 0, () => Open(name, null));
                return;
            }
            Open(name, null);
        }

        static string NoteFor(string id) { var s = WorldStory.FindGiver(id); return s != null && !string.IsNullOrEmpty(s.role) ? s.role + "." : ""; }

        void Open(string name, string say)
        {
            string text = say;
            if (text == null)
            {
                text = "...";
                foreach (var q in WorldStory.Root.quests)
                    if (q != null && WorldQuests.Done(q) && q.giver == npcId && !string.IsNullOrEmpty(q.finish)) text = q.finish;
            }
            var gs = GameState.I;
            TownUI.I.Open(name, () => text, () =>
            {
                var l = new List<UIEntry>();
                foreach (var q in WorldStory.Root.quests)
                {
                    if (q == null || q.giver != npcId) continue;
                    var qq = q;
                    if (WorldQuests.Available(q)) l.Add(new UIEntry("Quest: " + q.title, () => { Offer(name, qq); }));
                    else if (WorldQuests.Active(q)) l.Add(new UIEntry("(Berjalan) " + q.title + " - " + WorldQuests.StepText(q), null, false));
                    else if (!WorldQuests.Accepted(q) && gs.level < q.minLevel) l.Add(new UIEntry("(Butuh Level " + q.minLevel + ") " + q.title, null, false));
                }
                l.Add(new UIEntry("Tutup", () => TownUI.I.Close()));
                return l;
            });
        }

        /// <summary>Tampilkan halaman dialog satu per satu, lalu panggil done.</summary>
        void Pages(string name, string title, string[] pages, int i, System.Action done)
        {
            pages = TextFit.Expand(pages, 150);      // dialog panjang dipecah di batas kalimat supaya muat di kotak kecil
            if (i >= pages.Length) { if (done != null) done(); return; }
            TownUI.I.Open(name + " - " + title + "  (" + (i + 1) + "/" + pages.Length + ")", () => pages[i], () => new List<UIEntry>
            {
                new UIEntry(i + 1 < pages.Length ? "Lanjut >" : "Selesai", () => { if (i + 1 < pages.Length) Pages(name, title, pages, i + 1, done); else if (done != null) done(); }),
            });
        }

        void Offer(string name, WorldQuest q)
        {
            if (q.offerPages != null && q.offerPages.Length > 0 && !GameState.I.flags.Contains("wq_off_" + q.id))
            {
                GameState.I.flags.Add("wq_off_" + q.id);
                Pages(name, q.title, q.offerPages, 0, () => OfferFinal(name, q));
                return;
            }
            OfferFinal(name, q);
        }

        void OfferFinal(string name, WorldQuest q)
        {
            string text = (string.IsNullOrEmpty(q.offer) ? q.summary : q.offer) + "\n\nHadiah: " + q.rewardGold + " gold" + (q.rewardExp > 0 ? ", " + q.rewardExp + " EXP" : "");
            TownUI.I.Open(name + " - " + q.title, () => text, () =>
            {
                var l = new List<UIEntry>();
                if (q.choices != null)
                    foreach (var cc in q.choices)
                    {
                        var c = cc; if (c == null) continue;
                        l.Add(new UIEntry("\"" + c.text + "\"", () => TownUI.I.Open(name, () => c.reply, () => new List<UIEntry> { new UIEntry("< Kembali", () => OfferFinal(name, q)) })));
                    }
                l.Add(new UIEntry("Terima quest", () => { WorldQuests.Accept(q); ChapterCard(q); Open(name, string.IsNullOrEmpty(q.offer) ? "Baik. Semoga berhasil." : "Semoga beruntung, Hunter."); }));
                l.Add(new UIEntry("< Kembali", () => Open(name, null)));
                return l;
            });
        }
    }

    public partial class StoryGiver
    {
        /// <summary>Kartu judul bab: kamera berputar pelan mengelilingi NPC dengan teks "BAB n". Dilewati bila sedang bertarung.</summary>
        void ChapterCard(WorldQuest q)
        {
            if (Cutscene.Active || PlayerController.InFight()) return;
            TownUI.I.Close();
            var c = transform.position;
            Cutscene.Play(new List<Cutscene.Shot> { Cutscene.Orbit(c, 5.5f, 4.2f, 2.2f, 1.8f, 20f, 70f, 4.5f, q.title, 1.6f) });
        }
    }

    /// <summary>Benda lore (prasasti, buku, patung): dibaca untuk membuka entri Kodeks Lore.</summary>
    public class LoreObject : Interactable
    {
        public string loreId;
        void Awake() { prompt = "Periksa"; radius = 3f; }
        void Start() { if (loreId == "nd_o_ketukan") StoryCutscenes.AddLantern(transform); }

        public override void Interact()
        {
            if (LoreDB.Get(loreId) == null) { TownUI.I.Toast("(Lore '" + loreId + "' tidak ada di WorldStory.json / LoreDB)"); return; }
            string id = loreId;
            System.Action read = () => { WorldQuests.UnlockLore(id); WorldQuests.OnEvent("lore", id); LoreCodex.Read(id, 0, null); };
            if (StoryCutscenes.OnLore(id, transform, read)) { TownUI.I.Close(); return; }
            read();
        }
    }

    /// <summary>Memunculkan NPC cerita / benda lore sesuai WorldStory.json di scene aktif, dan memeriksa tahap "reach".</summary>
    public class StoryWorld : MonoBehaviour
    {
        readonly List<GameObject> spawned = new List<GameObject>();
        Transform player; float nextCheck;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<StoryWorld>() != null) return;
            var go = new GameObject("StoryWorld"); DontDestroyOnLoad(go); go.AddComponent<StoryWorld>();
        }

        void OnEnable() { SceneManager.sceneLoaded += OnLoaded; }
        void OnDisable() { SceneManager.sceneLoaded -= OnLoaded; }
        void Start() { Spawn(); }
        void OnLoaded(Scene s, LoadSceneMode m) { player = null; Spawn(); }

        void Spawn()
        {
            foreach (var g in spawned) if (g != null) Destroy(g);
            spawned.Clear();
            string sc = SceneManager.GetActiveScene().name;
            foreach (var sp in WorldStory.Root.spawns)
            {
                if (sp == null || !(string.IsNullOrEmpty(sp.scene) || sp.scene == sc)) continue;
                Vector3 p = new Vector3(sp.x, sp.y, sp.z);
                if (!string.IsNullOrEmpty(sp.anchor))
                {
                    Vector3 basePos; bool okA = false; basePos = Vector3.zero;
                    if (sp.anchor == "GuildFront") { basePos = GuildConfig.TownReturnPos; okA = true; }
                    else { var ag = GameObject.Find(sp.anchor); if (ag != null) { basePos = ag.transform.position; okA = true; } }
                    if (!okA) continue;      // jangkar tidak ada di scene ini: lewati, jangan taruh di (0,0,0)
                    p = basePos + p;
                }
                RaycastHit h;
                if (Physics.Raycast(p + Vector3.up * 40f, Vector3.down, out h, 120f, ~0, QueryTriggerInteraction.Ignore)) p = h.point;
                if (sp.kind == "giver") spawned.Add(MakeGiver(sp, p));
                else if (sp.kind == "lore") spawned.Add(MakeLore(sp, p));
                else if (sp.kind == "jeda") spawned.Add(StoryFeatures.MakeJeda(sp, p));
                else if (sp.kind == "dinding") spawned.Add(StoryFeatures.MakeWall(sp, p));
                else if (sp.kind == "lilin") spawned.Add(StoryFeatures.MakeCandle(sp, p));
                else if (sp.kind == "tanda") spawned.Add(StoryForeshadow.MakeMark(sp, p));       // v35z102n77: foreshadowing
            }
            StoryScenery.Build(sc);
        }

        static GameObject Prim(PrimitiveType t, Transform par, Vector3 lp, Vector3 sc, Color c)
        {
            var g = GameObject.CreatePrimitive(t);
            var col = g.GetComponent<Collider>(); if (col != null) Destroy(col);
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

        // Placeholder sederhana: ganti dengan model sendiri dengan menaruh komponen StoryGiver / LoreObject pada model di scene.
        // v35z102n32: NPC cerita memakai NpcKit (perakit karakter game) -> otomatis memakai model asli bila HGModelConfig diisi.
        static int OutfitFor(string role, int h)
        {
            string r = (role ?? "").ToLowerInvariant();
            if (r.Contains("masak") || r.Contains("dapur") || r.Contains("koki")) return NpcKit.OKoki;
            if (r.Contains("pemburu") || r.Contains("pemanah") || r.Contains("pendaki") || r.Contains("pengembara")) return NpcKit.OPengembara;
            if (r.Contains("penjaga") || r.Contains("pelatih") || r.Contains("prajurit")) return NpcKit.OPenjaga;
            if (r.Contains("pendeta") || r.Contains("biarawan")) return NpcKit.OPendeta;
            if (r.Contains("penyihir") || r.Contains("dukun")) return NpcKit.OPenyihir;
            if (r.Contains("pandai") || r.Contains("tukang")) return NpcKit.OPandai;
            if (r.Contains("pedagang")) return NpcKit.OPedagang;
            if (r.Contains("petani")) return NpcKit.OPetani;
            if (r.Contains("bard")) return NpcKit.OBard;
            if (r.Contains("pelajar") || r.Contains("sarjana") || r.Contains("penulis")) return NpcKit.OPelajar;
            int[] any = { NpcKit.OWarga, NpcKit.OPengembara, NpcKit.OPelajar, NpcKit.OPedagang };
            return any[Mathf.Abs(h) % any.Length];
        }

        static GameObject MakeGiver(StorySpawn sp, Vector3 p)
        {
            int h = 0; foreach (char ch in sp.id) h = unchecked(h * 31 + ch);
            var spec = NpcKit.RandomSpec(OutfitFor(sp.role, h), new System.Random(h));
            spec.name = string.IsNullOrEmpty(sp.name) ? sp.id : sp.name;
            if (!string.IsNullOrEmpty(sp.role)) spec.role = sp.role;
            var go = NpcKit.Spawn(null, "StoryGiver_" + sp.id, p, (Mathf.Abs(h) % 360), spec, true);
            go.transform.position = p;
            var actor = go.GetComponent<NpcActor>();
            float top = actor != null && actor.height > 0.5f ? actor.height + 0.55f : 2.7f;
            Label(go.transform, (string.IsNullOrEmpty(sp.name) ? sp.id : sp.name) + (string.IsNullOrEmpty(sp.role) ? "" : "\n<" + sp.role + ">"), top, new Color(1f, 0.93f, 0.6f));
            var g = go.AddComponent<StoryGiver>(); g.npcId = sp.id; g.npcName = sp.name;
            go.AddComponent<NpcIdle>();
            return go;
        }

        static GameObject MakeLore(StorySpawn sp, Vector3 p)
        {
            var go = new GameObject("LoreObject_" + sp.id); go.transform.position = p;
            Prim(PrimitiveType.Cube, go.transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.9f, 1.8f, 0.35f), new Color(0.45f, 0.45f, 0.5f, 1f));
            Prim(PrimitiveType.Sphere, go.transform, new Vector3(0f, 2.1f, 0f), Vector3.one * 0.3f, new Color(0.5f, 0.85f, 1f, 1f));
            Label(go.transform, string.IsNullOrEmpty(sp.name) ? "Prasasti" : sp.name, 2.7f, new Color(0.6f, 0.9f, 1f));
            var l = go.AddComponent<LoreObject>(); l.loreId = sp.id;
            return go;
        }

        void Update()
        {
            StoryCompanion.Tick();
            StoryScenery.Tick();
            StoryCutscenes.Tick();
            if (Time.unscaledTime < nextCheck) return; nextCheck = Time.unscaledTime + 0.5f;
            if (player == null) { var pc = FindFirstObjectByType<PlayerController>(); if (pc != null) player = pc.transform; if (player == null) return; }
            WorldQuests.CheckReach(player.position);
        }
    }

    /// <summary>NPC cerita menoleh ke pemain bila dekat (animasi idle ditangani NpcKit / model asli).</summary>
    class NpcIdle : MonoBehaviour
    {
        void Update()
        {
            var pc = PlayerCombat.I;
            if (pc != null)
            {
                Vector3 d = pc.transform.position - transform.position; d.y = 0f;
                if (d.sqrMagnitude < 12f * 12f && d.sqrMagnitude > 0.5f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 3f * Time.deltaTime);
            }
        }
    }

    class Billboard : MonoBehaviour
    {
        void LateUpdate() { if (Camera.main != null) transform.rotation = Camera.main.transform.rotation; }
    }
}

namespace HunterGuild
{
    /// <summary>Memecah teks dialog panjang menjadi halaman pendek di batas kalimat (bukan di tengah kata).</summary>
    public static class TextFit
    {
        public static string[] Expand(string[] pages, int max)
        {
            if (pages == null) return new string[0];
            var res = new System.Collections.Generic.List<string>();
            foreach (var p in pages) Split(p, max, res);
            return res.ToArray();
        }

        public static void Split(string text, int max, System.Collections.Generic.List<string> res)
        {
            if (string.IsNullOrEmpty(text)) return;
            text = text.Trim();
            while (text.Length > max)
            {
                int cut = -1;
                for (int i = max; i > max / 2; i--)            // cari akhir kalimat terdekat sebelum batas
                {
                    char c = text[i - 1];
                    if ((c == '.' || c == '!' || c == '?' || c == '\u2026') && (i >= text.Length || text[i] == ' ' || text[i] == '"' || text[i] == '\n')) { cut = i; break; }
                }
                if (cut < 0)
                    for (int i = max; i > max / 2; i--) if (text[i - 1] == ',' || text[i - 1] == ';' || text[i - 1] == ':') { cut = i; break; }
                if (cut < 0) { cut = text.LastIndexOf(' ', max); if (cut <= max / 2) cut = max; }
                res.Add(text.Substring(0, cut).Trim());
                text = text.Substring(cut).Trim();
            }
            if (text.Length > 0) res.Add(text);
        }
    }
}
