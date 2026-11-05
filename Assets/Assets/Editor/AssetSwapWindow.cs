using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Hunter Guild > Ganti Aset 3D. Tab per area (Desa Pemula, Ashwatch, Eldrath, ...), tiap tab berisi bagian-bagian kecil scene
/// yang dipisah per kategori (Buildings, Houses, Landmarks, Props, Lights, Structure). Taruh prefab 3D milikmu di slot bagian itu;
/// saat game berjalan bagian asli disembunyikan dan prefabmu muncul di tempat yang sama. Tidak ada objek scene yang ditimpa/dihapus.
/// </summary>
public class AssetSwapWindow : EditorWindow
{
    const string TablePath = "Assets/Resources/AssetSwapTable.asset";
    static readonly Dictionary<string, string> Friendly = new Dictionary<string, string>
    {
        { "StartVillage", "Desa Pemula" }, { "Town", "Ashwatch" }, { "DungeonCity", "Eldrath" }, { "GuildInterior", "Guild (dalam)" },
        { "HuntArea_Tutorial", "Hutan Pemula" }, { "HuntArea_G", "Hutan Lerai" }, { "HuntArea_Desert", "Gurun Zahar" },
        { "HuntArea_Swamp", "Rawa Terkutuk" }, { "ArenaExam", "Arena Ujian" }, { "DungeonTower", "Menara" },
        { "Interior_GuildEldrath", "Guild Eldrath (dalam)" }
    };

    AssetSwapTable table;
    List<string> scenePaths = new List<string>();
    int tab, cat;       // cat 0 = Semua
    string search = "";
    Vector2 scroll, tabScroll;
    bool onlyFilled;

    [MenuItem("Hunter Guild/Ganti Aset 3D")]
    static void Open() { var w = GetWindow<AssetSwapWindow>("Ganti Aset 3D"); w.minSize = new Vector2(760f, 420f); w.Show(); }

    void OnEnable() { LoadTable(); RefreshScenes(); }

    void LoadTable()
    {
        table = AssetDatabase.LoadAssetAtPath<AssetSwapTable>(TablePath);
        if (table == null)
        {
            if (!Directory.Exists("Assets/Resources")) Directory.CreateDirectory("Assets/Resources");
            table = CreateInstance<AssetSwapTable>();
            AssetDatabase.CreateAsset(table, TablePath);
            AssetDatabase.SaveAssets();
        }
    }

    void RefreshScenes()
    {
        scenePaths.Clear();
        foreach (var g in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" })) scenePaths.Add(AssetDatabase.GUIDToAssetPath(g));
        scenePaths.Sort();
        if (tab >= scenePaths.Count) tab = 0;
    }

    static string SceneName(string path) { return Path.GetFileNameWithoutExtension(path); }
    static string Label(string path) { string n = SceneName(path); string f; return Friendly.TryGetValue(n, out f) ? f : n.Replace("_", " "); }

    void OnGUI()
    {
        if (table == null) LoadTable();
        if (EditorApplication.isPlaying) { EditorGUILayout.HelpBox("Keluar dari Play mode untuk mengatur penggantian aset.", MessageType.Info); return; }
        if (scenePaths.Count == 0)
        {
            EditorGUILayout.HelpBox("Belum ada scene di Assets/Scenes. Jalankan Hunter Guild > Bangun SEMUA scene dulu.", MessageType.Warning);
            if (GUILayout.Button("Muat ulang")) RefreshScenes();
            return;
        }

        // ---- tab area
        tabScroll = EditorGUILayout.BeginScrollView(tabScroll, GUILayout.Height(46));
        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < scenePaths.Count; i++)
        {
            bool on = i == tab;
            var st = new GUIStyle(EditorStyles.toolbarButton) { fontStyle = on ? FontStyle.Bold : FontStyle.Normal, fixedHeight = 26 };
            int filled = 0; foreach (var e in table.entries) if (e.scene == SceneName(scenePaths[i]) && e.prefab != null) filled++;
            if (GUILayout.Toggle(on, Label(scenePaths[i]) + (filled > 0 ? "  (" + filled + ")" : ""), st, GUILayout.MinWidth(90))) { if (!on) { tab = i; cat = 0; scroll = Vector2.zero; } }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();

        string scene = SceneName(scenePaths[tab]);

        // ---- kategori (tag)
        var cats = new List<string> { "Semua" };
        foreach (var g in AssetSwapRuntime.Groups) cats.Add(g);
        var counts = new int[cats.Count];
        foreach (var e in table.entries) if (e.scene == scene) { counts[0]++; int ci = cats.IndexOf(e.category); if (ci > 0) counts[ci]++; }
        var names = new string[cats.Count];
        for (int i = 0; i < cats.Count; i++) names[i] = cats[i] + " (" + counts[i] + ")";
        cat = GUILayout.Toolbar(cat, names);

        EditorGUILayout.BeginHorizontal();
        search = EditorGUILayout.TextField("Cari", search);
        onlyFilled = GUILayout.Toggle(onlyFilled, "Hanya yang terisi", GUILayout.Width(130));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Pindai scene ini")) Scan(new[] { scenePaths[tab] });
        if (GUILayout.Button("Pindai SEMUA scene")) Scan(scenePaths.ToArray());
        if (GUILayout.Button("Kosongkan tab ini", GUILayout.Width(130)) && EditorUtility.DisplayDialog("Kosongkan", "Hapus semua prefab pengganti di tab " + Label(scenePaths[tab]) + "? (objek scene tidak berubah)", "Ya", "Batal"))
        {
            foreach (var e in table.entries) if (e.scene == scene) e.prefab = null;
            Save();
        }
        EditorGUILayout.EndHorizontal();

        if (counts[0] == 0)
        {
            EditorGUILayout.HelpBox("Belum ada daftar bagian untuk area ini. Klik \"Pindai scene ini\" (scene dibuka sebentar lalu ditutup, scene aktifmu tidak berubah).", MessageType.Info);
            return;
        }

        // ---- daftar
        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        GUILayout.Label("Bagian (jumlah di scene)", EditorStyles.boldLabel, GUILayout.Width(220));
        GUILayout.Label("Prefab 3D pengganti", EditorStyles.boldLabel, GUILayout.Width(190));
        GUILayout.Label("Skala", EditorStyles.boldLabel, GUILayout.Width(46));
        GUILayout.Label("Naik", EditorStyles.boldLabel, GUILayout.Width(46));
        GUILayout.Label("Putar", EditorStyles.boldLabel, GUILayout.Width(46));
        GUILayout.Label("Opsi", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        string q = search.ToLowerInvariant();
        bool changed = false;
        foreach (var e in table.entries)
        {
            if (e.scene != scene) continue;
            if (cat > 0 && e.category != cats[cat]) continue;
            if (q.Length > 0 && e.key.ToLowerInvariant().IndexOf(q) < 0) continue;
            if (onlyFilled && e.prefab == null) continue;
            EditorGUILayout.BeginHorizontal();
            string nm = (cat == 0 ? "[" + e.category + "] " : "") + e.key + "  x" + e.count;
            GUILayout.Label(new GUIContent(nm, e.category + " / " + e.key), GUILayout.Width(220));
            EditorGUI.BeginChangeCheck();
            e.prefab = (GameObject)EditorGUILayout.ObjectField(e.prefab, typeof(GameObject), false, GUILayout.Width(190));
            e.scale = EditorGUILayout.FloatField(e.scale, GUILayout.Width(46));
            e.yOffset = EditorGUILayout.FloatField(e.yOffset, GUILayout.Width(46));
            e.rotY = EditorGUILayout.FloatField(e.rotY, GUILayout.Width(46));
            e.fitHeight = GUILayout.Toggle(e.fitHeight, new GUIContent("Cocokkan tinggi", "Skala prefab otomatis agar tingginya sama dengan objek asli"), GUILayout.Width(110));
            e.enabled = GUILayout.Toggle(e.enabled, "Aktif", GUILayout.Width(52));
            if (e.prefab != null && GUILayout.Button("x", GUILayout.Width(22))) e.prefab = null;
            if (EditorGUI.EndChangeCheck()) changed = true;
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        if (changed) Save();

        EditorGUILayout.HelpBox("Berlaku saat Play. Posisi prefab disejajarkan ke dasar objek asli; collider objek asli tetap dipakai. " +
                                "Kosongkan slot untuk kembali ke bentuk bawaan. NPC tidak termasuk (punya animasi sendiri).", MessageType.None);
    }

    void Save() { EditorUtility.SetDirty(table); AssetDatabase.SaveAssets(); SwapBackup.Backup(table); }

    // ---------------- pemindaian ----------------
    void Scan(string[] paths)
    {
        var active = EditorSceneManager.GetActiveScene().path;
        try
        {
            for (int i = 0; i < paths.Length; i++)
            {
                EditorUtility.DisplayProgressBar("Memindai scene", Label(paths[i]), i / (float)paths.Length);
                ScanOne(paths[i]);
            }
        }
        finally { EditorUtility.ClearProgressBar(); }
        Save();
        Repaint();
    }

    void ScanOne(string path)
    {
        string scene = SceneName(path);
        var sc = EditorSceneManager.GetSceneByPath(path);
        bool wasLoaded = sc.isLoaded;
        if (!wasLoaded) sc = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        var found = new Dictionary<string, int>();
        foreach (var root in sc.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.parent == null || !AssetSwapRuntime.IsGroup(t.parent.name)) continue;
                if (t.GetComponentInChildren<Renderer>(true) == null) continue;
                string k = t.parent.name + "|" + AssetSwapRuntime.Norm(t.name);
                int n; found[k] = found.TryGetValue(k, out n) ? n + 1 : 1;
            }
        if (!wasLoaded) EditorSceneManager.CloseScene(sc, true);

        // gabungkan ke tabel: pertahankan pengaturan yang ada, tambah yang baru, buang yang usang dan masih kosong
        var seen = new HashSet<string>();
        foreach (var kv in found)
        {
            int bar = kv.Key.IndexOf('|');
            string c = kv.Key.Substring(0, bar), k = kv.Key.Substring(bar + 1);
            seen.Add(kv.Key);
            var e = table.Find(scene, c, k);
            if (e == null) { e = new AssetSwapTable.Entry { scene = scene, category = c, key = k }; table.entries.Add(e); }
            e.count = kv.Value;
        }
        table.entries.RemoveAll(e => e.scene == scene && e.prefab == null && !seen.Contains(e.category + "|" + e.key));
        table.entries.Sort((a, b) =>
        {
            int s = string.CompareOrdinal(a.scene, b.scene); if (s != 0) return s;
            int c = System.Array.IndexOf(AssetSwapRuntime.Groups, a.category).CompareTo(System.Array.IndexOf(AssetSwapRuntime.Groups, b.category)); if (c != 0) return c;
            return string.CompareOrdinal(a.key, b.key);
        });
    }
}
