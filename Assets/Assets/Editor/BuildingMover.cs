using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using HunterGuild;

/// <summary>
/// Membuat SEMUA bangunan di scene kota/desa bisa dipindah manual satu per satu:
///  - tiap bangunan dibungkus objek "Bangunan_..." (pivot di dasar bangunan, sumbu +Z = sisi pintu) dengan komponen MovableBuilding;
///  - papan nama, pintu, bedeng bunga, pagar, lampu halaman, NPC penjaga yang berada di sekitar bangunan dijadikan anak objek itu;
///  - flag static batching dilepas supaya aman digeser;
///  - posisi yang kamu ubah disimpan ke Assets/HGLayout/{Scene}.json dan dipasang ulang otomatis di setiap build berikutnya.
/// </summary>
public static class BuildingMover
{
    [System.Serializable] public class Entry { public string name; public float x, y, z, yaw; }
    [System.Serializable] public class LayoutFile { public List<Entry> items = new List<Entry>(); }

    const string Dir = "Assets/HGLayout";
    static readonly Regex NameWhitelist = new Regex("^(Rumah|Kantor|Pandai|Kincir|Gazebo|Kios|Pondok|Penginapan|Gereja|Makam|Mausoleum|Lumbung|Kandang|Gudang|Menara|Pos|Warung|Kedai|Bangunan|Gerbang|Pagoda|Kuil|Pusat|Toko|Balai|Rumah)", RegexOptions.IgnoreCase);
    static readonly Regex NameSkip = new Regex("^(Pohon|Sipres|Semak|Sakura|Batu|Lamp|Lampu|Bench|Bangku|Tiang|Pagar|Rumput|Bunga|Kristal|Patung|Halaman)", RegexOptions.IgnoreCase);

    static string LayoutPath(string scene) { return Dir + "/" + scene + ".json"; }

    // ------------------------------------------------------------------ Prepare (dipanggil di akhir build scene)
    public static int Prepare(string sceneName, Transform[] buildingGroups, Transform[] poolGroups)
    {
        try { return PrepareImpl(sceneName, buildingGroups, poolGroups); }
        catch (System.Exception e) { Debug.LogWarning("[Layout] Penyiapan bangunan dilewati: " + e.Message); return 0; }
    }

    static int PrepareImpl(string sceneName, Transform[] buildingGroups, Transform[] poolGroups)
    {
        var used = new HashSet<string>();
        var roots = new List<Transform>();
        var containers = new List<Transform>();
        foreach (var g in buildingGroups) if (g != null) Scan(g, roots, containers, used, 0, false);

        // satelit: objek kecil di sekitar tiap bangunan ikut menjadi anak
        var pool = new List<Transform>();
        var rootSet = new HashSet<Transform>(roots);
        foreach (var c in containers) rootSet.Add(c);
        foreach (var g in poolGroups) if (g != null) CollectPool(g, rootSet, pool, 0);
        var boxes = new List<Bounds>(); var xfs = new List<Transform>();
        foreach (var r in roots) { boxes.Add(LocalBounds(r)); xfs.Add(r); }
        int moved = 0;
        var assign = new List<KeyValuePair<Transform, Transform>>();
        foreach (var cand in pool)
        {
            Vector3 center; Vector3 size;
            if (!CandidateBounds(cand, out center, out size)) continue;
            if (size.x > 16f || size.z > 16f) continue;
            int best = -1; float bestScore = 1e9f;
            for (int i = 0; i < xfs.Count; i++)
            {
                if (cand == xfs[i] || cand.IsChildOf(xfs[i])) { best = -2; break; }
                Vector3 lp = xfs[i].InverseTransformPoint(center);
                Bounds b = boxes[i];
                float ex = b.extents.x + 2.5f, zMin = b.min.z - 1.5f, zMax = b.max.z + 5.5f;
                float dx = Mathf.Abs(lp.x - b.center.x);
                if (dx > ex || lp.z < zMin || lp.z > zMax || Mathf.Abs(lp.y - b.center.y) > b.extents.y + 6f) continue;
                float score = dx / ex + Mathf.Abs(lp.z - b.center.z) / (b.extents.z + 7f);
                if (score < bestScore) { bestScore = score; best = i; }
            }
            if (best >= 0) assign.Add(new KeyValuePair<Transform, Transform>(cand, xfs[best]));
        }
        foreach (var kv in assign) { if (kv.Key != null) { kv.Key.SetParent(kv.Value, true); moved++; } }

        // komponen, flag, posisi bawaan (dicatat SEBELUM layout tersimpan dipasang)
        var layout = Load(sceneName);
        var dict = new Dictionary<string, Entry>();
        foreach (var e in layout.items) dict[e.name] = e;
        foreach (var c in containers)
        {
            var mb = c.GetComponent<MovableBuilding>();
            if (mb == null) mb = c.gameObject.AddComponent<MovableBuilding>();
            mb.buildingName = c.name; mb.isContainer = true;
            mb.localSize = new Vector3(6f, 3f, 6f); mb.localCenter = Vector3.zero;
            mb.defaultPosition = c.position; mb.defaultYaw = c.eulerAngles.y;
        }
        foreach (var r in roots)
        {
            var mb = r.GetComponent<MovableBuilding>();
            if (mb == null) mb = r.gameObject.AddComponent<MovableBuilding>();
            Bounds lb = LocalBounds(r);
            mb.buildingName = r.name; mb.localCenter = lb.center; mb.localSize = lb.size;
            mb.defaultPosition = r.position; mb.defaultYaw = r.eulerAngles.y;
            ClearBatching(r);
        }
        int applied = 0;
        foreach (var c in containers)            // komplek dulu, lalu bangunan di dalamnya (posisi tersimpan bersifat mutlak)
        {
            Entry en;
            if (dict.TryGetValue(c.name, out en)) { c.position = new Vector3(en.x, en.y, en.z); c.rotation = Quaternion.Euler(0f, en.yaw, 0f); applied++; }
        }
        foreach (var r in roots)
        {
            Entry en;
            if (dict.TryGetValue(r.name, out en)) { r.position = new Vector3(en.x, en.y, en.z); r.rotation = Quaternion.Euler(0f, en.yaw, 0f); applied++; }
        }
        Debug.Log("[Layout] " + sceneName + ": " + roots.Count + " bangunan bisa dipindah, " + moved + " objek pendamping ikut, " + applied + " posisi manual dipasang.");
        return roots.Count;
    }

    static bool IsContainer(Transform t) { return t.name.StartsWith("Komplek"); }

    static string Unique(HashSet<string> used, string n)
    {
        string r = n; int k = 2;
        while (used.Contains(r)) r = n + "_" + (k++);
        used.Add(r);
        return r;
    }

    static void Scan(Transform g, List<Transform> roots, List<Transform> containers, HashSet<string> used, int depth, bool strictName)
    {
        var kids = new List<Transform>();
        foreach (Transform c in g) kids.Add(c);
        foreach (var c in kids)
        {
            if (c == null) continue;
            if (c.GetComponent<MovableBuilding>() != null && !c.name.StartsWith("Komplek")) { roots.Add(c); continue; }
            if (IsContainer(c)) { containers.Add(c); used.Add(c.name); Scan(c, roots, containers, used, depth + 1, true); continue; }
            if (NameSkip.IsMatch(c.name)) continue;
            bool direct = depth == 0 || !strictName;
            if (IsBuilding(c, strictName))
            {
                roots.Add(Wrap(c, used));
                continue;
            }
            // grup di dalam grup bangunan (mis. FantasiDesa di Props): turun satu-dua tingkat dengan aturan nama ketat
            if (depth < 2 && c.childCount > 0 && c.GetComponent<MeshRenderer>() == null) Scan(c, roots, containers, used, depth + 1, true);
        }
    }

    static bool IsBuilding(Transform c, bool strictName)
    {
        { Bounds gb = WorldBounds(c); if (gb.size.x > 45f || gb.size.z > 45f) return false; }      // mesh gabungan raksasa bukan bangunan
        bool selfR = c.GetComponent<MeshRenderer>() != null;
        if (selfR)
        {
            bool col = c.GetComponent<Collider>() != null || c.GetComponentInChildren<Collider>() != null;
            if (!col) return false;
            if (strictName || c.parent == null) return NameWhitelist.IsMatch(c.name);
            return true;
        }
        // objek induk bergaya Landmark/Gereja: ada anak "Model"/"Mesh" ber-renderer + collider/Interactable
        Transform m = c.Find("Model");
        if (m == null) m = c.Find("Mesh");
        if (m == null) return false;
        if (m.GetComponentInChildren<MeshRenderer>() == null && m.GetComponent<MeshRenderer>() == null) return false;
        bool hasCol = c.GetComponentInChildren<Collider>() != null;
        bool hasInt = c.GetComponentInChildren<Interactable>() != null;
        if (!(hasCol || hasInt)) return false;
        if (strictName && !NameWhitelist.IsMatch(c.name)) return false;
        return true;
    }

    static Transform Wrap(Transform c, HashSet<string> used)
    {
        bool selfR = c.GetComponent<MeshRenderer>() != null;
        Bounds wb = WorldBounds(c);
        Vector3 piv = c.position;
        if (!selfR)
        {
            c.name = Unique(used, c.name);
            return c;                                          // sudah berupa induk bangunan
        }
        // objek bermesh langsung: bungkus dengan induk berpivot di dasar bangunan
        if (wb.size.sqrMagnitude > 0f && new Vector2(wb.center.x - piv.x, wb.center.z - piv.z).magnitude > 2.5f)
            piv = new Vector3(wb.center.x, wb.min.y, wb.center.z);     // mesh berkoordinat dunia (pivot di origin): pindahkan pivot ke tengah bangunan
        var root = new GameObject(Unique(used, "Bangunan_" + c.name)).transform;
        root.SetParent(c.parent, false);
        root.position = piv;
        root.rotation = Quaternion.Euler(0f, c.eulerAngles.y, 0f);
        root.SetSiblingIndex(c.GetSiblingIndex());
        c.SetParent(root, true);
        return root;
    }

    static void CollectPool(Transform g, HashSet<Transform> rootSet, List<Transform> pool, int depth)
    {
        var kids = new List<Transform>();
        foreach (Transform c in g) kids.Add(c);
        foreach (var c in kids)
        {
            if (c == null) continue;
            if (c.name.StartsWith("Komplek")) { CollectPool(c, rootSet, pool, depth); continue; }
            if (rootSet.Contains(c)) continue;
            if (c.CompareTag("Player") || c.CompareTag("MainCamera")) continue;
            if (c.GetComponent<Terrain>() != null || c.GetComponent<Camera>() != null) continue;
            if (c.GetComponent<WanderNPC>() != null) continue;
            if (c.GetComponentInParent<MovableBuilding>() != null) continue;
            if (NameSkip.IsMatch(c.name)) continue;
            bool unit = c.GetComponent<Renderer>() != null || c.GetComponent<Interactable>() != null || c.GetComponent<Light>() != null
                        || c.GetComponent<Collider>() != null || c.GetComponent<NpcActor>() != null || c.GetComponent<TextMesh>() != null;
            if (unit) { pool.Add(c); continue; }
            if (depth < 3 && c.childCount > 0) CollectPool(c, rootSet, pool, depth + 1);
        }
    }

    static bool CandidateBounds(Transform t, out Vector3 center, out Vector3 size)
    {
        Bounds b = WorldBounds(t);
        if (b.size.sqrMagnitude > 0f) { center = b.center; size = b.size; return true; }
        center = t.position; size = Vector3.zero; return true;
    }

    static Bounds WorldBounds(Transform t)
    {
        bool init = false; Bounds b = new Bounds(t.position, Vector3.zero);
        foreach (var r in t.GetComponentsInChildren<Renderer>(false))
        {
            if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
            if (!init) { b = r.bounds; init = true; } else b.Encapsulate(r.bounds);
        }
        return init ? b : new Bounds(t.position, Vector3.zero);
    }

    static Bounds LocalBounds(Transform root)
    {
        bool init = false; Bounds b = new Bounds(Vector3.zero, Vector3.zero);
        foreach (var r in root.GetComponentsInChildren<Renderer>(false))
        {
            if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
            if (r.GetComponent<TextMesh>() != null) continue;
            if (r.name == "Marker") continue;
            Bounds wb = r.bounds;
            if (wb.size.x > 60f || wb.size.z > 60f) continue;                // abaikan mesh raksasa (tanah/pagar)
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = wb.center + Vector3.Scale(wb.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                Vector3 lp = root.InverseTransformPoint(corner);
                if (!init) { b = new Bounds(lp, Vector3.zero); init = true; } else b.Encapsulate(lp);
            }
        }
        if (!init) b = new Bounds(new Vector3(0f, 2f, 0f), new Vector3(6f, 4f, 6f));
        return b;
    }

    static void ClearBatching(Transform root)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            var f = GameObjectUtility.GetStaticEditorFlags(t.gameObject);
            if ((f & StaticEditorFlags.BatchingStatic) != 0) GameObjectUtility.SetStaticEditorFlags(t.gameObject, f & ~StaticEditorFlags.BatchingStatic);
        }
    }

    // ------------------------------------------------------------------ simpan / muat
    static LayoutFile Load(string scene)
    {
        try
        {
            string p = LayoutPath(scene);
            if (File.Exists(p)) { var lf = JsonUtility.FromJson<LayoutFile>(File.ReadAllText(p)); if (lf != null && lf.items != null) return lf; }
        }
        catch { }
        return new LayoutFile();
    }

    static void Save(string scene, LayoutFile lf)
    {
        if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets", "HGLayout");
        File.WriteAllText(LayoutPath(scene), JsonUtility.ToJson(lf, true));
        AssetDatabase.ImportAsset(LayoutPath(scene));
    }

    /// <summary>Simpan hanya bangunan yang posisinya berbeda dari bawaan (kerja manualmu). Entri lain yang sudah tersimpan dipertahankan.</summary>
    public static int CaptureScene(Scene sc)
    {
        if (!sc.IsValid() || !sc.isLoaded) return 0;
        var lf = Load(sc.name);
        var dict = new Dictionary<string, Entry>();
        foreach (var e in lf.items) dict[e.name] = e;
        int n = 0;
        foreach (var root in sc.GetRootGameObjects())
            foreach (var mb in root.GetComponentsInChildren<MovableBuilding>(true))
            {
                Vector3 p = mb.transform.position; float yaw = mb.transform.eulerAngles.y;
                bool moved = (p - mb.defaultPosition).sqrMagnitude > 0.0025f || Mathf.Abs(Mathf.DeltaAngle(yaw, mb.defaultYaw)) > 0.5f;
                if (!moved) { dict.Remove(mb.buildingName); continue; }
                dict[mb.buildingName] = new Entry { name = mb.buildingName, x = p.x, y = p.y, z = p.z, yaw = yaw };
                n++;
            }
        lf.items = new List<Entry>(dict.Values);
        if (lf.items.Count > 0) Save(sc.name, lf);
        else if (File.Exists(LayoutPath(sc.name))) { AssetDatabase.DeleteAsset(LayoutPath(sc.name)); }
        return n;
    }

    /// <summary>Dipanggil di awal build: simpan posisi manual dari scene lama (terbuka atau dari file) sebelum ditimpa.</summary>
    public static void CaptureBeforeRebuild(string scenePath)
    {
        try
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null) return;
            var sc = SceneManager.GetSceneByPath(scenePath);
            bool opened = false;
            if (!sc.IsValid() || !sc.isLoaded) { sc = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive); opened = true; }
            int n = CaptureScene(sc);
            if (opened) EditorSceneManager.CloseScene(sc, true);
            if (n > 0) Debug.Log("[Layout] " + n + " posisi manual dari " + sc.name + " disimpan sebelum build ulang.");
        }
        catch (System.Exception e) { Debug.LogWarning("[Layout] Gagal menyimpan layout lama: " + e.Message); }
    }

    // ------------------------------------------------------------------ menu
    [MenuItem("Hunter Guild/Layout/Snap bangunan terpilih ke tanah")]
    static void SnapSelected()
    {
        int n = 0;
        foreach (var go in Selection.gameObjects)
        {
            var mb = go.GetComponentInParent<MovableBuilding>();
            if (mb == null) continue;
            Undo.RecordObject(mb.transform, "Snap ke tanah");
            mb.SnapToGround(); n++;
        }
        Debug.Log("[Layout] " + n + " bangunan di-snap ke tanah.");
    }

    [MenuItem("Hunter Guild/Layout/Simpan posisi manual (scene aktif)")]
    static void SaveActive()
    {
        var sc = EditorSceneManager.GetActiveScene();
        int n = CaptureScene(sc);
        EditorUtility.DisplayDialog("Layout", n + " bangunan yang kamu pindahkan disimpan ke " + LayoutPath(sc.name) + ".\nSetiap build ulang scene ini akan memasangnya kembali.", "OK");
    }

    [MenuItem("Hunter Guild/Layout/Terapkan layout tersimpan (scene aktif)")]
    static void ApplyActive()
    {
        var sc = EditorSceneManager.GetActiveScene();
        var lf = Load(sc.name);
        var dict = new Dictionary<string, Entry>();
        foreach (var e in lf.items) dict[e.name] = e;
        int n = 0;
        foreach (var root in sc.GetRootGameObjects())
            foreach (var mb in root.GetComponentsInChildren<MovableBuilding>(true))
            {
                Entry en;
                if (!dict.TryGetValue(mb.buildingName, out en)) continue;
                Undo.RecordObject(mb.transform, "Terapkan layout");
                mb.transform.position = new Vector3(en.x, en.y, en.z);
                mb.transform.rotation = Quaternion.Euler(0f, en.yaw, 0f);
                n++;
            }
        Debug.Log("[Layout] " + n + " posisi diterapkan.");
    }

    [MenuItem("Hunter Guild/Layout/Kembalikan semua bangunan ke posisi bawaan (scene aktif)")]
    static void ResetActive()
    {
        var sc = EditorSceneManager.GetActiveScene();
        int n = 0;
        foreach (var root in sc.GetRootGameObjects())
            foreach (var mb in root.GetComponentsInChildren<MovableBuilding>(true)) { Undo.RecordObject(mb.transform, "Reset layout"); mb.ResetToDefault(); n++; }
        Debug.Log("[Layout] " + n + " bangunan dikembalikan ke posisi bawaan. (File layout tersimpan tidak dihapus; pakai menu Hapus bila perlu.)");
    }

    [MenuItem("Hunter Guild/Layout/Hapus layout tersimpan (scene aktif)")]
    static void DeleteActive()
    {
        var sc = EditorSceneManager.GetActiveScene();
        if (File.Exists(LayoutPath(sc.name))) { AssetDatabase.DeleteAsset(LayoutPath(sc.name)); Debug.Log("[Layout] Layout " + sc.name + " dihapus."); }
    }
}
