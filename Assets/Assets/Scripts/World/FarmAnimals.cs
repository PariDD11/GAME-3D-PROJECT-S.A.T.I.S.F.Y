using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// Hewan ternak/buruan (ayam, domba, kambing, sapi) di desa dan area perburuan (BUKAN di dalam kota Ashwatch/Eldrath). Muncul otomatis saat scene dimuat (tanpa rebuild),
    /// berkelompok di sekitar titik awal pemain (tidak di dalam bangunan), dan muncul kembali setelah diburu bila pemain jauh.
    /// Hewan dibuat dari Monster pasif (lihat MonsterDef.passive): diburu lalu di-carve untuk daging dan bahan.
    /// Atur jumlah per scene di tabel Plan.
    /// </summary>
    public class FarmAnimals : MonoBehaviour
    {
        class Spec { public string scene; public int chicken, sheep, goat, cow; }
        static readonly Spec[] Plan =
        {
            new Spec { scene = "StartVillage",       chicken = 8, sheep = 6, goat = 4, cow = 4 },
            // Ashwatch (Town) dan Eldrath (DungeonCity) sengaja tanpa ternak: kota hanya ramai oleh warga (lihat EventCrowd).
            new Spec { scene = "HuntArea_Tutorial",  chicken = 4, sheep = 5, goat = 4, cow = 3 },
            new Spec { scene = "HuntArea_G",         chicken = 4, sheep = 5, goat = 4, cow = 3 },
            new Spec { scene = "HuntArea_Desert",    chicken = 0, sheep = 0, goat = 6, cow = 0 },
            new Spec { scene = "HuntArea_Frontier",  chicken = 0, sheep = 4, goat = 5, cow = 2 },
        };

        class Slot { public string id; public Vector3 pos; public GameObject live; public float deadAt = -1f; }
        readonly List<Slot> slots = new List<Slot>();
        Spec spec; Transform player; float born; bool spawned;
        const float RespawnDelay = 150f, RespawnFar = 40f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            SceneManager.sceneLoaded -= OnLoaded; SceneManager.sceneLoaded += OnLoaded;
            Install(SceneManager.GetActiveScene());
        }
        static void OnLoaded(Scene s, LoadSceneMode m) { Install(s); }

        static void Install(Scene s)
        {
            if (FindFirstObjectByType<FarmAnimals>() != null) return;
            foreach (var sp in Plan)
                if (sp.scene == s.name) { var go = new GameObject("FarmAnimals"); go.AddComponent<FarmAnimals>().spec = sp; return; }
        }

        void Start() { born = Time.time; }

        void Update()
        {
            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
                return;
            }
            if (!spawned)
            {
                if (Time.time - born < 2.5f) return;      // tunggu terrain/bangunan siap
                spawned = true;
                Populate("chicken", spec.chicken, 4); Populate("sheep", spec.sheep, 5);
                Populate("goat", spec.goat, 4); Populate("cow", spec.cow, 3);
                return;
            }
            foreach (var s in slots)
            {
                if (s.live != null) continue;
                if (s.deadAt < 0f) { s.deadAt = Time.time; continue; }
                if (Time.time - s.deadAt < RespawnDelay) continue;
                Vector3 d = player.position - s.pos; d.y = 0f;
                if (d.magnitude < RespawnFar) continue;
                s.live = Spawn(s.id, s.pos); s.deadAt = -1f;
            }
        }

        static GameObject Spawn(string id, Vector3 pos)
        {
            var go = new GameObject("Animal_" + id);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            go.AddComponent<Monster>().defId = id;
            return go;
        }

        /// <summary>Titik jangkar kawanan buatan builder: objek "Ternak_&lt;id&gt;" di bawah "TernakAnchors" (mis. padang ternak Desa Pemula). Kosong bila scene tidak punya.</summary>
        static List<Vector3> AnchorsFor(string id)
        {
            var list = new List<Vector3>();
            var root = GameObject.Find("TernakAnchors");
            if (root == null) return list;
            string key = "Ternak_" + id;
            foreach (Transform t in root.transform) if (t != null && t.name == key) list.Add(t.position);
            return list;
        }

        // bagi jumlah ke beberapa kelompok kecil
        void Populate(string id, int count, int herdMax)
        {
            int left = count;
            var anchors = AnchorsFor(id);
            int ai = Random.Range(0, 1000);
            while (left > 0)
            {
                int n = Mathf.Min(left, Random.Range(2, herdMax + 1));
                Vector3 c;
                if (anchors.Count > 0) c = anchors[(ai++) % anchors.Count];                 // kawanan di tempat yang ditentukan builder
                else if (!FindSpot(out c)) { left -= n; continue; }                        // selain itu: acak di sekitar pemain
                for (int i = 0; i < n; i++)
                {
                    Vector3 p = c + new Vector3(Random.Range(-4f, 4f), 0f, Random.Range(-4f, 4f));
                    Vector3 g;
                    if (!Ground(p, out g)) g = c;
                    slots.Add(new Slot { id = id, pos = g, live = Spawn(id, g) });
                }
                left -= n;
            }
        }

        bool FindSpot(out Vector3 pos)
        {
            for (int a = 0; a < 60; a++)
            {
                Vector2 r = Random.insideUnitCircle.normalized * Random.Range(26f, 100f);
                Vector3 p = player.position + new Vector3(r.x, 0f, r.y);
                if (Ground(p, out pos)) return true;
            }
            pos = player.position; return false;
        }

        // permukaan terbuka (terrain/lantai), bebas bangunan/pohon/NPC di sekitarnya.
        // Tidak memakai TerrainCollider (modul Terrain Physics bisa tidak aktif): tinggi tanah dari Terrain.SampleHeight.
        /// <summary>Terrain yang memuat titik p (scene Desa+Hutan Pemula punya dua terrain berdampingan); cadangan: Terrain.activeTerrain.</summary>
        internal static Terrain TerrainAt(Vector3 p)
        {
            var ts = Terrain.activeTerrains;
            for (int i = 0; i < ts.Length; i++)
            {
                var t = ts[i];
                if (t == null || t.terrainData == null) continue;
                Vector3 o = t.transform.position, sz = t.terrainData.size;
                if (p.x >= o.x && p.x <= o.x + sz.x && p.z >= o.z && p.z <= o.z + sz.z) return t;
            }
            return Terrain.activeTerrain;
        }

        internal static bool Ground(Vector3 p, out Vector3 ground)
        {
            ground = p;
            var terr = TerrainAt(p);
            RaycastHit hit;
            bool any = Physics.Raycast(new Vector3(p.x, p.y + 80f, p.z), Vector3.down, out hit, 200f, ~0, QueryTriggerInteraction.Ignore);
            Vector3 g;
            if (terr != null)
            {
                float h = terr.SampleHeight(p) + terr.transform.position.y;
                if (any && hit.point.y > h + 1.0f) return false;                       // ada atap/bangunan di atas tanah
                g = new Vector3(p.x, h, p.z);
                if (h < -1.2f) return false;                                          // dasar kali / kolam: bukan tempat ternak
            }
            else
            {
                if (!any || hit.normal.y < 0.92f) return false;
                var pl = GameObject.FindGameObjectWithTag("Player");
                if (pl != null && Mathf.Abs(hit.point.y - pl.transform.position.y) > 1.5f) return false;
                if (hit.collider.GetComponentInParent<Monster>() != null || hit.collider.GetComponentInParent<NpcActor>() != null) return false;
                g = hit.point;
            }
            var cols = Physics.OverlapSphere(g + Vector3.up * 1.1f, 1.3f, ~0, QueryTriggerInteraction.Ignore);
            foreach (var c in cols)
            {
                if (any && c == hit.collider && terr == null) continue;
                if (c.GetComponent<Terrain>() != null) continue;
                return false;
            }
            ground = g; return true;
        }
    }
}
