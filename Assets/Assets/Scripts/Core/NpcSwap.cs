using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// Aturan penggantian model + foto 2D dialog + aksi NPC (Assets/Resources/NpcSwapTable.asset).
    /// Dikelola lewat Hunter Guild > Ganti NPC. Aturan yang paling spesifik menang (nama > ras/pekerjaan > umum).
    /// </summary>
    public class NpcSwapTable : ScriptableObject
    {
        [Serializable]
        public class Rule
        {
            public string label = "Aturan baru";
            public bool enabled = true;
            // ---- kecocokan (kosong / 0 / -1 = bebas)
            public string npcName = "";         // nama persis NPC (aturan khusus 1 orang)
            public string scene = "";           // nama scene (kosong = semua)
            public int gender;                  // 0 bebas, 1 pria, 2 wanita
            public int race = -1;               // -1 bebas, 0 manusia, 1 dwarf, 2 elf, 3 beastfolk
            public int role = -1;               // -1 bebas, 0..15 = NpcKit.OutfitName
            public int importance;              // 0 bebas, 1 NPC penting, 2 NPC biasa
            // ---- hasil
            public List<GameObject> prefabs = new List<GameObject>();   // lebih dari satu = divariasikan per NPC
            public Texture2D portraitTex;       // foto 2D di kotak dialog: PNG/JPG biasa (tidak perlu diubah ke Sprite)
            public List<Texture2D> portraitTexes = new List<Texture2D>();   // variasi foto (dipilih per NPC)
            public float portraitScale = 1f, portraitX, portraitY; public bool portraitFlip; public int portraitSide;   // atur foto: skala, geser (px), balik, sisi (0 otomatis, 1 kiri, 2 kanan)
            public Sprite portrait;             // lama (Sprite), tetap dibaca bila ada
            public List<Sprite> portraits = new List<Sprite>();         // opsional: variasi foto (dipilih per NPC), dipakai bila portrait kosong
            public float heightScale = 1f;
            public float yOffset;
            // ---- aksi / animasi
            public RuntimeAnimatorController controller;
            public float moveSpeedMul = 1f;     // kecepatan jalan NPC di dunia (WanderNPC.speed x ini)
            public float animSpeed = 1f;        // kecepatan putar animasi (Animator.speed)
            public float speedParamScale = 1f;  // pengali nilai parameter Speed yang dikirim ke Animator (turunkan bila animasi jalan terlalu cepat / langsung berlari)
            public bool workWhenIdle;           // set bool Animator "Working" saat NPC diam dan tidak bicara
            public AnimationClip clipIdle, clipWalk, clipWave, clipTalk, clipWork;   // bahan untuk tombol "Buat Controller dari klip"
        }

        [Serializable]
        public class Seen
        {
            public string scene, npcName; public int race, role; public bool female, important;
        }

        public List<Rule> rules = new List<Rule>();
        public List<Seen> seen = new List<Seen>();          // daftar NPC hasil pindai (hanya untuk tampilan editor)
    }

    /// <summary>Komponen penanda NPC yang sudah diganti.</summary>
    public class NpcSwapMark : MonoBehaviour { public string rule; }

    public static class NpcSwapRuntime
    {
        public static readonly string[] RaceNames = { "Manusia", "Dwarf", "Elf", "Beastfolk" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            SceneManager.sceneLoaded -= OnLoaded; SceneManager.sceneLoaded += OnLoaded;
            Apply(SceneManager.GetActiveScene());
        }
        static void OnLoaded(Scene s, LoadSceneMode m) { Apply(s); }

        /// <summary>NPC penting = punya interaksi selain obrolan biasa (toko, guild, quest, dll) atau bernama khusus di aturan.</summary>
        public static bool IsImportant(NpcActor a)
        {
            if (a == null) return false;
            if (a.silent) return true;
            foreach (var it in a.GetComponentsInChildren<Interactable>(true))
                if (it != null && !(it is NpcChat)) return true;
            return false;
        }

        static int Score(NpcSwapTable.Rule r, NpcActor a, string scene, bool important)
        {
            if (r == null || !r.enabled) return -1;
            int sc = 0;
            if (!string.IsNullOrEmpty(r.npcName)) { if (!string.Equals(r.npcName.Trim(), (a.displayName ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) return -1; sc += 16; }
            if (!string.IsNullOrEmpty(r.scene)) { if (r.scene != scene) return -1; sc += 1; }
            if (r.gender != 0) { if (r.gender != (a.female ? 2 : 1)) return -1; sc += 2; }
            if (r.race >= 0) { if (r.race != a.race) return -1; sc += 3; }
            if (r.role >= 0) { if (r.role != a.role) return -1; sc += 4; }
            if (r.importance != 0) { if ((r.importance == 1) != important) return -1; sc += 2; }
            return sc;
        }

        public static NpcSwapTable.Rule Match(NpcSwapTable table, NpcActor a, string scene)
        {
            bool imp = IsImportant(a);
            NpcSwapTable.Rule best = null; int bs = -1;
            foreach (var r in table.rules) { int s = Score(r, a, scene, imp); if (s > bs) { bs = s; best = r; } }
            return best;
        }

        /// <summary>Terapkan aturan ke satu NPC yang dibuat saat runtime (mis. keramaian event).</summary>
        public static void ApplyTo(GameObject go)
        {
            var table = Resources.Load<NpcSwapTable>("NpcSwapTable");
            var a = go != null ? go.GetComponent<NpcActor>() : null;
            if (table == null || a == null || a.GetComponent<NpcSwapMark>() != null) return;
            var r = Match(table, a, go.scene.name);
            if (r != null) ApplyRule(a, r);
        }

        static void Apply(Scene s)
        {
            var table = Resources.Load<NpcSwapTable>("NpcSwapTable");
            if (table == null || table.rules.Count == 0 || !s.IsValid()) return;
            int done = 0;
            foreach (var root in s.GetRootGameObjects())
                foreach (var a in root.GetComponentsInChildren<NpcActor>(true))
                {
                    if (a.GetComponent<NpcSwapMark>() != null) continue;
                    var r = Match(table, a, s.name);
                    if (r == null) continue;
                    if (ApplyRule(a, r)) done++;
                }
            if (done > 0) Debug.Log("[NpcSwap] " + done + " NPC diubah di scene " + s.name);
        }

        static readonly Dictionary<Texture2D, Sprite> spriteCache = new Dictionary<Texture2D, Sprite>();
        /// <summary>Texture biasa -> Sprite untuk kotak dialog (pivot bawah-tengah, sama seperti Resources/Portraits).</summary>
        public static Sprite ToSprite(Texture2D tex)
        {
            if (tex == null) return null;
            Sprite sp;
            if (spriteCache.TryGetValue(tex, out sp) && sp != null) return sp;
            sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), 100f);
            spriteCache[tex] = sp; return sp;
        }

        public static bool ApplyRule(NpcActor a, NpcSwapTable.Rule r)
        {
            int seed = Mathf.Abs((a.displayName ?? "x").GetHashCode());
            var mark = a.gameObject.AddComponent<NpcSwapMark>(); mark.rule = r.label;

            // foto 2D
            var all = new List<Sprite>();
            if (r.portraitTex != null) all.Add(ToSprite(r.portraitTex));
            if (r.portraitTexes != null) foreach (var x in r.portraitTexes) if (x != null) all.Add(ToSprite(x));
            if (all.Count == 0 && r.portrait != null) all.Add(r.portrait);
            if (all.Count == 0 && r.portraits != null) foreach (var x in r.portraits) if (x != null) all.Add(x);
            Sprite pt = null;
            if (all.Count > 0)
            {
                // aturan untuk satu NPC bernama: semua foto dipakai bergantian tiap percakapan baru; aturan umum: satu foto tetap per NPC
                if (!string.IsNullOrEmpty(r.npcName) && all.Count > 1) { a.gallery = all.ToArray(); pt = all[0]; }
                else pt = all[seed % all.Count];
            }
            if (pt != null) { a.portrait = pt; a.pScale = r.portraitScale <= 0f ? 1f : r.portraitScale; a.pX = r.portraitX; a.pY = r.portraitY; a.pFlip = r.portraitFlip; a.pSide = r.portraitSide; }

            if (r.moveSpeedMul > 0f && Mathf.Abs(r.moveSpeedMul - 1f) > 0.001f) { var wn = a.GetComponent<WanderNPC>(); if (wn != null) wn.speed *= r.moveSpeedMul; }

            // model
            GameObject prefab = null;
            var list = new List<GameObject>(); foreach (var p in r.prefabs) if (p != null) list.Add(p);
            if (list.Count > 0) prefab = list[seed % list.Count];
            if (prefab != null) SwapModel(a, prefab, r);
            return true;
        }

        static void SwapModel(NpcActor a, GameObject prefab, NpcSwapTable.Rule r)
        {
            var tr = a.transform;
            // buang model lama (bagian prosedural atau model hasil wizard); label nama / bubble / collider akar tetap
            var kill = new List<GameObject>();
            foreach (Transform c in tr)
                if (c.name == "Body" || c.name == "LegL" || c.name == "LegR" || c.name == "Model") kill.Add(c.gameObject);
            foreach (var k in kill) UnityEngine.Object.DestroyImmediate(k);

            var model = UnityEngine.Object.Instantiate(prefab, tr, false);
            model.name = "Model";
            model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity;
            foreach (var c in model.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
            foreach (var rb in model.GetComponentsInChildren<Rigidbody>()) UnityEngine.Object.DestroyImmediate(rb);

            float h = Mathf.Max(0.5f, a.height * Mathf.Max(0.1f, r.heightScale));
            ExternalModelDriver.FitTo(model.transform, h);
            if (Mathf.Abs(r.yOffset) > 0.0001f) model.transform.position += Vector3.up * r.yOffset;

            var an = model.GetComponentInChildren<Animator>();
            var cfg = HGModelConfig.Load();
            RuntimeAnimatorController ctrl = r.controller != null ? r.controller : (an != null && an.runtimeAnimatorController != null ? an.runtimeAnimatorController : (cfg != null ? cfg.npcController : null));
            if (an != null && ctrl != null) an.runtimeAnimatorController = ctrl;

            var old = a.GetComponent<NpcAnimator>();
            ExternalNpcDriver drv = old as ExternalNpcDriver;
            if (drv == null)
            {
                if (old != null) UnityEngine.Object.DestroyImmediate(old);
                drv = a.gameObject.AddComponent<ExternalNpcDriver>();
            }
            drv.workWhenIdle = r.workWhenIdle;
            drv.animSpeed = r.animSpeed > 0f ? r.animSpeed : 1f; drv.speedScale = r.speedParamScale > 0f ? r.speedParamScale : 1f;
            drv.Init(an);
            a.RebindAnimator();
        }
    }
}
