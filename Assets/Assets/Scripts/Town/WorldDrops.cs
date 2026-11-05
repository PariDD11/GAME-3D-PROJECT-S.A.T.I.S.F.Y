using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>Catatan satu tumpukan item di tanah (disimpan di file save, jadi TIDAK hilang walau pindah scene / keluar game).</summary>
    [Serializable]
    public class DropRec
    {
        public string scene, id, src;     // src = id monster asal (untuk Bestiary), kosong bila dijatuhkan pemain
        public int n, mine;               // mine 1 = dijatuhkan pemain (ambil dengan E), 0 = drop monster (terambil otomatis saat didekati)
        public float x, y, z;
    }

    /// <summary>
    /// Item di dunia: drop monster (bahan penempaan) dan item yang dibuang pemain dari tas.
    /// Semua tercatat di daftar Records, dibuat ulang saat scene dimuat, dan ikut disimpan di save; hilang hanya bila diambil.
    /// </summary>
    public static class WorldDrops
    {
        public static readonly List<DropRec> Records = new List<DropRec>();
        const int MaxRecords = 240;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            SceneManager.sceneLoaded += (s, m) => SpawnForScene(s.name);
            SpawnForScene(SceneManager.GetActiveScene().name);
        }

        public static void Clear() { Records.Clear(); foreach (var p in new List<DropPickup>(DropPickup.AllPickups)) if (p != null) UnityEngine.Object.Destroy(p.gameObject); }

        public static List<DropRec> Snapshot() { return new List<DropRec>(Records); }

        public static void Restore(List<DropRec> list)
        {
            Clear();
            if (list != null) foreach (var r in list) if (r != null && !string.IsNullOrEmpty(r.id) && r.n > 0) Records.Add(r);
            SpawnForScene(SceneManager.GetActiveScene().name);
        }

        static void SpawnForScene(string scene)
        {
            foreach (var r in Records) if (r.scene == scene && !DropPickup.Has(r)) DropPickup.Create(r, false);
        }

        static void Trim()
        {
            while (Records.Count > MaxRecords)
            {
                int k = Records.FindIndex(r => r.mine == 0);       // buang catatan drop monster paling lama dulu; item pemain dipertahankan
                if (k < 0) k = 0;
                var rec = Records[k]; Records.RemoveAt(k);
                foreach (var p in new List<DropPickup>(DropPickup.AllPickups)) if (p != null && p.rec == rec) UnityEngine.Object.Destroy(p.gameObject);
            }
        }

        /// <summary>Jatuhkan item di dunia (drop monster atau buangan pemain).</summary>
        public static void Spawn(string id, int n, Vector3 pos, string src, bool byPlayer)
        {
            if (string.IsNullOrEmpty(id) || n <= 0 || ItemDB.Get(id) == null) return;
            var rec = new DropRec { scene = SceneManager.GetActiveScene().name, id = id, n = n, src = src ?? "", mine = byPlayer ? 1 : 0, x = pos.x, y = pos.y, z = pos.z };
            Records.Add(rec);
            Trim();
            DropPickup.Create(rec, true);
        }

        /// <summary>Buang item dari tas: dijatuhkan di depan pemain dan tetap ada di sana sampai diambil lagi.</summary>
        public static bool PlayerDrop(string id, int n)
        {
            var gs = GameState.I; var pc = PlayerCombat.I;
            if (gs == null || pc == null || n <= 0 || gs.Count(id) < n) return false;
            gs.RemoveItem(id, n);
            Vector3 f = pc.transform.forward; f.y = 0f; f = f.sqrMagnitude < 0.01f ? Vector3.forward : f.normalized;
            Vector3 p = pc.transform.position - Vector3.up * 0.9f + f * 1.7f + Vector3.up * 1.0f;
            Spawn(id, n, p, "", true);
            var d = ItemDB.Get(id);
            if (TownUI.I != null) TownUI.I.Toast("Dijatuhkan: " + (d != null ? d.name : id) + (n > 1 ? " x" + n : "") + "  (ambil lagi dengan E)");
            return true;
        }

        // ------------------------------------------------------------------ drop monster
        static Color Tone(ItemDef d)
        {
            if (d.type == ItemType.Gear || d.type == ItemType.Attachment) return new Color(0.75f, 0.5f, 1f);
            if (d.sellPrice >= 90) return new Color(1f, 0.6f, 0.2f);
            if (d.sellPrice >= 40) return new Color(1f, 0.85f, 0.35f);
            if (d.sellPrice >= 15) return new Color(0.5f, 0.9f, 1f);
            return new Color(0.85f, 0.95f, 0.9f);
        }
        internal static Color ToneOf(ItemDef d) { return ItemGrades.Of(d) >= ItemGrade.Uncommon ? ItemGrades.Tint(d) : Tone(d); }   // grade Uncommon+ bercahaya sesuai warna grade

        /// <summary>
        /// Monster mati -> menjatuhkan bahan (penempaan): monster biasa 1-2 bahan, monster besar 3-4 (+ bahan utama lebih banyak), boss 4-6.
        /// Bahan diambil dari daftar loot monster itu (loot[0] = bahan utama), ditambah peluang drop langka (def.rare).
        /// </summary>
        public static void MonsterDeath(MonsterDef def, Vector3 pos, bool boss)
        {
            if (def == null || def.loot == null || def.loot.Length == 0 || def.passive || def.id == "dummy") return;
            var tally = new Dictionary<string, int>();
            Action<string, int> add = (id, q) => { if (!string.IsNullOrEmpty(id) && ItemDB.Get(id) != null) { int v; tally.TryGetValue(id, out v); tally[id] = v + q; } };
            bool big = boss || def.large || def.carves >= 3;
            // bahan utama dijamin
            add(def.loot[0], boss ? UnityEngine.Random.Range(1, 3) : big ? UnityEngine.Random.Range(1, 3) : 1);
            if (big)
            {
                for (int i = 1; i < def.loot.Length; i++) if (UnityEngine.Random.value < 0.85f) add(def.loot[i], UnityEngine.Random.Range(1, 3));
                int extra = boss ? UnityEngine.Random.Range(1, 3) : UnityEngine.Random.Range(0, 2);
                for (int i = 0; i < extra; i++) add(def.loot[UnityEngine.Random.Range(0, def.loot.Length)], 1);
            }
            else
            {
                if (def.loot.Length > 1 && UnityEngine.Random.value < 0.55f) add(def.loot[UnityEngine.Random.Range(1, def.loot.Length)], 1);
            }
            // drop langka
            if (!string.IsNullOrEmpty(def.rare) && UnityEngine.Random.value < def.rareChance * (boss ? 1.5f : 1f)) add(def.rare, 1);
            int k = 0;
            foreach (var kv in tally)
            {
                float a = UnityEngine.Random.value * Mathf.PI * 2f, r = 0.6f + 0.35f * k;
                Spawn(kv.Key, kv.Value, pos + new Vector3(Mathf.Cos(a) * r, 1.0f, Mathf.Sin(a) * r), def.id, false);
                k++;
            }
        }
    }

    /// <summary>Objek item di tanah: melompat keluar, berkilau, dan terambil otomatis (drop monster) / lewat E (buangan pemain).</summary>
    public class DropPickup : Interactable
    {
        public static readonly List<DropPickup> AllPickups = new List<DropPickup>();
        public DropRec rec;
        ItemDef def; Color tone;
        Transform icon, glow; Vector3 vel; bool landed; float born, groundY;
        Material glowMat;

        public static bool Has(DropRec r) { foreach (var p in AllPickups) if (p != null && p.rec == r) return true; return false; }

        public static DropPickup Create(DropRec r, bool pop)
        {
            var d = ItemDB.Get(r.id); if (d == null) return null;
            var go = new GameObject("Drop_" + r.id);
            var p = go.AddComponent<DropPickup>();
            p.rec = r; p.def = d; p.tone = WorldDrops.ToneOf(d);
            go.transform.position = new Vector3(r.x, r.y, r.z);
            p.Build(pop);
            return p;
        }

        float Ground(Vector3 at)
        {
            // tanah sungguhan: abaikan pemain, monster, dan pemicu (supaya drop tidak "mendarat" di kepala pemain/bangkai)
            var hits = Physics.RaycastAll(at + Vector3.up * 3f, Vector3.down, 60f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue; float y = float.NaN;
            foreach (var h in hits)
            {
                if (h.collider.GetComponentInParent<CharacterController>() != null || h.collider.GetComponentInParent<Monster>() != null || h.collider.GetComponentInParent<DropPickup>() != null) continue;
                if (h.distance < best) { best = h.distance; y = h.point.y; }
            }
            if (float.IsNaN(y) && Terrain.activeTerrain != null) y = RT.GroundY(at);
            return y;      // NaN = tanah belum siap
        }

        void Build(bool pop)
        {
            born = Time.time;
            prompt = "Ambil " + def.name + (rec.n > 1 ? " x" + rec.n : "");
            radius = 2.4f;
            var sp = ItemIcons.For(def);
            var ic = new GameObject("icon"); ic.transform.SetParent(transform, false);
            var sr = ic.AddComponent<SpriteRenderer>(); sr.sprite = sp; sr.sortingOrder = 5;
            // ukuran dunia tetap ~0.6 m berapa pun resolusi gambar (icon custom 256 px tidak boleh jadi raksasa)
            float bw = sp != null ? Mathf.Max(0.01f, Mathf.Max(sp.bounds.size.x, sp.bounds.size.y)) : 0.64f;
            ic.transform.localScale = Vector3.one * (0.61f / bw); icon = ic.transform;
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "glow";
            var col = q.GetComponent<Collider>(); if (col != null) Destroy(col);
            q.transform.SetParent(transform, false); q.transform.localScale = Vector3.one * 1.35f; glow = q.transform;
            glowMat = CombatFx.Mat(CombatFx.Soft(), new Color(tone.r, tone.g, tone.b, 0.55f));
            var gr = q.GetComponent<MeshRenderer>(); gr.sharedMaterial = glowMat; gr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; gr.receiveShadows = false;
            float gy = Ground(transform.position);
            groundY = float.IsNaN(gy) ? (pop ? transform.position.y - 1f : rec.y - 0.55f) : gy;
            if (pop) { vel = new Vector3(UnityEngine.Random.Range(-1.6f, 1.6f), UnityEngine.Random.Range(3.5f, 5.5f), UnityEngine.Random.Range(-1.6f, 1.6f)); }
            else { Vector3 pp = transform.position; pp.y = groundY + 0.55f; transform.position = pp; landed = true; }
            AllPickups.Add(this);
        }

        protected override void OnEnable() { base.OnEnable(); }
        protected override void OnDisable() { base.OnDisable(); AllPickups.Remove(this); }

        void Update()
        {
            if (rec == null) { Destroy(gameObject); return; }
            float dt = Time.deltaTime;
            if (!landed)
            {
                vel.y -= 14f * dt;
                transform.position += vel * dt;
                if (transform.position.y <= groundY + 0.55f && vel.y < 0f)
                {
                    Vector3 p = transform.position; p.y = groundY + 0.55f; transform.position = p;
                    if (Mathf.Abs(vel.y) > 2.2f) { vel = new Vector3(vel.x * 0.4f, -vel.y * 0.38f, vel.z * 0.4f); }
                    else { landed = true; vel = Vector3.zero; }
                }
            }
            else
            {
                if (Time.time - born < 1.5f) { float g3 = Ground(transform.position); if (!float.IsNaN(g3)) groundY = g3; }    // scene baru dimuat: tanah mungkin belum siap saat dibuat
                // melayang pelan + kilau berdenyut
                var p = transform.position; p.y = groundY + 0.55f + Mathf.Sin(Time.time * 2.4f + born) * 0.09f; transform.position = p;
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 3f + born * 2f);
                glow.localScale = Vector3.one * (1.25f + 0.2f * pulse);
                // simpan posisi mendarat di catatan
                rec.x = p.x; rec.y = groundY + 0.55f; rec.z = p.z;
            }
            var cam = Camera.main;
            if (cam != null) { Quaternion r = cam.transform.rotation; icon.rotation = r; glow.rotation = r; }

            // drop monster: terambil otomatis saat pemain mendekat (ada efek tertarik), buangan pemain: tekan E
            var pcm = PlayerCombat.I;
            if (landed && rec.mine == 0 && pcm != null && Time.time - born > 0.9f)
            {
                Vector3 to = pcm.transform.position - transform.position; to.y = 0f;
                float d = to.magnitude;
                float rng = 4.2f * (Access.WideLoot ? 2.2f : 1f);
                if (d < rng && GameState.I != null && GameState.I.HasSpace(rec.id, rec.n))
                {
                    transform.position += to.normalized * Mathf.Lerp(0.5f, Access.WideLoot ? 11f : 7f, 1f - d / rng) * dt;
                    float g2 = Ground(transform.position); if (!float.IsNaN(g2)) groundY = g2;
                    if (d < 1.3f) Pickup(true);
                }
            }
        }

        public override void Interact() { Pickup(false); }

        void Pickup(bool auto)
        {
            var gs = GameState.I;
            if (gs == null || rec == null) return;
            if (!gs.HasSpace(rec.id, rec.n))
            {
                if (!auto && TownUI.I != null) TownUI.I.Toast("Tas penuh! Buang sesuatu dulu (Tab).");
                return;
            }
            gs.AddItem(rec.id, rec.n);
            if (!string.IsNullOrEmpty(rec.src)) gs.AddStat("bd_" + rec.src + "_" + rec.id);
            gs.AddStat("drops_taken");
            if (TownUI.I != null) TownUI.I.Toast("+ " + def.name + (rec.n > 1 ? " x" + rec.n : ""));
            Audio.Sfx("coin", transform.position);
            CombatFx.HitSpark(transform.position, tone, 0.6f, 8);
            WorldDrops.Records.Remove(rec);
            rec = null;
            Destroy(gameObject);
        }
    }
}
