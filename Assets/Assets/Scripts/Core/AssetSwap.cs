using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// Tabel penggantian aset 3D (Assets/Resources/AssetSwapTable.asset). Dikelola lewat jendela Hunter Guild > Ganti Aset 3D.
    /// Tiap baris = satu bagian scene (nama objek di dalam grup Buildings/Houses/Landmarks/Props/Lights/Structure) yang bisa
    /// diganti prefab 3D milikmu. Penggantian dilakukan saat game berjalan: objek asli hanya disembunyikan (tidak dihapus/ditimpa),
    /// jadi aman dan bisa dibatalkan kapan saja dengan mengosongkan slot prefab.
    /// </summary>
    public class AssetSwapTable : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string scene = "", category = "", key = "";
            public GameObject prefab;
            public bool enabled = true;
            public float scale = 1f, yOffset = 0f, rotY = 0f;
            public bool fitHeight = false;          // skala prefab otomatis agar tingginya sama dengan objek asli
            public bool keepPrefabColliders = false;
            public int count = 0;                   // jumlah objek berkunci sama di scene saat pemindaian terakhir
        }

        public List<Entry> entries = new List<Entry>();

        public Entry Find(string scene, string category, string key)
        {
            foreach (var e in entries) if (e.scene == scene && e.category == category && e.key == key) return e;
            return null;
        }
    }

    /// <summary>Penerap penggantian aset saat scene dimuat.</summary>
    public static class AssetSwapRuntime
    {
        /// <summary>Grup induk tempat bagian-bagian scene diletakkan oleh builder.</summary>
        public static readonly string[] Groups = { "Buildings", "Houses", "Landmarks", "Props", "Lights", "Structure" };

        class Mark : MonoBehaviour { }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            SceneManager.sceneLoaded -= OnLoaded; SceneManager.sceneLoaded += OnLoaded;
            Apply(SceneManager.GetActiveScene());
        }
        static void OnLoaded(Scene s, LoadSceneMode m) { Apply(s); }

        public static bool IsGroup(string n)
        {
            foreach (var g in Groups) if (g == n) return true;
            return false;
        }

        /// <summary>Menormalkan nama objek: buang "(Clone)", "(n)", dan angka di belakang, supaya objek sejenis memakai kunci yang sama.</summary>
        public static string Norm(string n)
        {
            if (string.IsNullOrEmpty(n)) return "";
            n = n.Replace("(Clone)", "").Trim();
            int p = n.LastIndexOf(" (");
            if (p > 0 && n.EndsWith(")")) n = n.Substring(0, p);
            int e = n.Length;
            while (e > 1 && char.IsDigit(n[e - 1])) e--;
            n = n.Substring(0, e).TrimEnd('_', ' ', '-');
            return n;
        }

        public static void Apply(Scene s)
        {
            var table = Resources.Load<AssetSwapTable>("AssetSwapTable");
            if (table == null || !s.IsValid()) return;
            var map = new Dictionary<string, AssetSwapTable.Entry>();
            foreach (var e in table.entries)
                if (e.enabled && e.prefab != null && e.scene == s.name) map[e.category + "|" + e.key] = e;
            if (map.Count == 0) return;
            int done = 0;
            foreach (var root in s.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.parent == null || !IsGroup(t.parent.name)) continue;
                    AssetSwapTable.Entry en;
                    if (!map.TryGetValue(t.parent.name + "|" + Norm(t.name), out en)) continue;
                    if (t.GetComponent<Mark>() != null) continue;
                    if (Swap(t, en)) done++;
                }
            if (done > 0) Debug.Log("[AssetSwap] " + done + " bagian diganti di scene " + s.name);
        }

        static bool Swap(Transform t, AssetSwapTable.Entry en)
        {
            var rends = t.GetComponentsInChildren<Renderer>(true);
            if (rends.Length == 0) return false;
            Bounds b = new Bounds(); bool has = false;
            foreach (var r in rends)
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
            }
            if (!has) return false;
            Vector3 baseP = new Vector3(b.center.x, b.min.y, b.center.z);
            float origH = Mathf.Max(0.01f, b.size.y);

            var inst = UnityEngine.Object.Instantiate(en.prefab);
            inst.name = en.prefab.name + "_swap";
            inst.transform.SetParent(t.parent, true);
            inst.transform.rotation = Quaternion.Euler(0f, t.eulerAngles.y + en.rotY, 0f);
            float k = Mathf.Max(0.01f, en.scale);
            inst.transform.localScale = en.prefab.transform.localScale * k;
            if (en.fitHeight)
            {
                var pb = RenderBounds(inst);
                if (pb.size.y > 0.001f) inst.transform.localScale *= origH / pb.size.y;
            }
            var ib = RenderBounds(inst);
            Vector3 ibase = new Vector3(ib.center.x, ib.min.y, ib.center.z);
            inst.transform.position += baseP + Vector3.up * en.yOffset - ibase;
            if (!en.keepPrefabColliders)
                foreach (var c in inst.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.Destroy(c);
            inst.AddComponent<Mark>();
            // objek asli: sembunyikan tampilan saja (collider, interaksi, cahaya tetap)
            foreach (var r in rends)
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                r.enabled = false;
            }
            t.gameObject.AddComponent<Mark>();
            return true;
        }

        static Bounds RenderBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            Bounds b = new Bounds(go.transform.position, Vector3.zero); bool has = false;
            foreach (var r in rs) { if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds); }
            return b;
        }
    }
}
