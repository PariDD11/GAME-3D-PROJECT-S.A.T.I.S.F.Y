using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Hunter Guild > Potret HUD. Atur gambar potret hex di HUD kiri-atas untuk setiap job dan evolusinya (Lv15 &amp; Lv30),
/// terpisah untuk avatar cowok dan cewek. Mendukung: seret sprite ke slot, geser/zoom dengan pratinjau,
/// "Isi otomatis dari folder" (nama file = id, mis. Knight.png, Knight_F.png), impor tekstur jadi Sprite otomatis,
/// salin ke job/evolusi lain, kosongkan, dan daftar yang belum terisi.
/// </summary>
public class HudPortraitWindow : EditorWindow
{
    const string AssetPath = "Assets/Resources/HudPortraitConfig.asset";
    const string DefaultFolder = "Assets/Resources/HudPortraits";

    HudPortraitConfig cfg;
    Vector2 scroll;
    string filter = "";
    string folder = DefaultFolder;
    bool onlyMissing;
    readonly Dictionary<string, bool> fold = new Dictionary<string, bool>();
    string status = "";

    [MenuItem("Hunter Guild/Potret HUD...")]
    static void Open() { var w = GetWindow<HudPortraitWindow>("Potret HUD"); w.minSize = new Vector2(620, 480); w.Load(); }

    void OnEnable() { Load(); }

    void Load() { cfg = AssetDatabase.LoadAssetAtPath<HudPortraitConfig>(AssetPath); }

    HudPortraitConfig Ensure()
    {
        if (cfg != null) return cfg;
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        cfg = ScriptableObject.CreateInstance<HudPortraitConfig>();
        AssetDatabase.CreateAsset(cfg, AssetPath);
        AssetDatabase.SaveAssets();
        return cfg;
    }

    static string Label(string key)
    {
        foreach (var e in JobInfo.Evos) if (e.id == key) return e.name + (e.parent != "" ? "  (Lv30)" : "  (Lv15)");
        return JobInfo.Name(key) + "  (job dasar)";
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("Potret hex di HUD kiri-atas. Prioritas saat main: evolusi ke-2 > evolusi ke-1 > job. Slot kosong = HUD memakai ikon bawaan.", MessageType.Info);
        if (cfg == null)
        {
            if (GUILayout.Button("Buat HudPortraitConfig.asset", GUILayout.Height(32))) Ensure();
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            filter = EditorGUILayout.TextField("Cari", filter);
            onlyMissing = GUILayout.Toggle(onlyMissing, "Hanya yang kosong", GUILayout.Width(130));
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            folder = EditorGUILayout.TextField("Folder gambar", folder);
            if (GUILayout.Button("Pilih...", GUILayout.Width(60)))
            {
                string p = EditorUtility.OpenFolderPanel("Folder potret", Application.dataPath, "");
                string data = Application.dataPath.Replace('\\', '/');
                if (!string.IsNullOrEmpty(p) && p.Replace('\\', '/').StartsWith(data)) folder = "Assets" + p.Replace('\\', '/').Substring(data.Length);
            }
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Isi otomatis dari folder")) AutoFill();
            if (GUILayout.Button("Daftar yang kosong")) ReportMissing();
            if (GUILayout.Button("Simpan")) { EditorUtility.SetDirty(cfg); AssetDatabase.SaveAssets(); status = "Tersimpan."; }
        }
        if (!string.IsNullOrEmpty(status)) EditorGUILayout.LabelField(status, EditorStyles.miniLabel);
        EditorGUILayout.LabelField("Nama file: <id>.png (cowok), <id>_F.png (cewek). Contoh: Swordsman.png, Knight.png, PaladinLord_F.png", EditorStyles.miniLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (var job in JobInfo.All)
        {
            var keys = new List<string> { job };
            foreach (var e in JobInfo.Evos) if (e.job == job && e.parent == "") keys.Add(e.id);
            foreach (var e in JobInfo.Evos) if (e.job == job && e.parent != "") keys.Add(e.id);
            bool open; fold.TryGetValue(job, out open);
            open = EditorGUILayout.Foldout(open, JobInfo.Name(job) + "   (" + Filled(keys) + "/" + keys.Count + " terisi)", true);
            fold[job] = open;
            if (!open) continue;
            EditorGUI.indentLevel++;
            foreach (var k in keys) DrawEntry(k);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndScrollView();
    }

    int Filled(List<string> keys)
    {
        int n = 0; foreach (var k in keys) { var e = cfg.Find(k); if (e != null && (e.male != null || e.female != null)) n++; }
        return n;
    }

    void DrawEntry(string key)
    {
        var ex = cfg.Find(key);
        bool has = ex != null && (ex.male != null || ex.female != null);
        if (onlyMissing && has) return;
        string lab = Label(key);
        string f = (filter ?? "").ToLowerInvariant();
        if (f != "" && lab.ToLowerInvariant().IndexOf(f) < 0 && key.ToLowerInvariant().IndexOf(f) < 0) return;

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField(lab + "   [" + key + "]", EditorStyles.boldLabel);
        var e = cfg.GetOrAdd(key);
        EditorGUI.BeginChangeCheck();
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(150)))
            {
                EditorGUILayout.LabelField("Cowok", GUILayout.Width(60));
                e.male = (Sprite)EditorGUILayout.ObjectField(e.male, typeof(Sprite), false, GUILayout.Width(110), GUILayout.Height(60));
                EditorGUILayout.LabelField("Cewek (opsional)", GUILayout.Width(120));
                e.female = (Sprite)EditorGUILayout.ObjectField(e.female, typeof(Sprite), false, GUILayout.Width(110), GUILayout.Height(60));
            }
            using (new EditorGUILayout.VerticalScope())
            {
                e.scale = EditorGUILayout.Slider("Zoom", e.scale <= 0.01f ? 1f : e.scale, 0.5f, 3f);
                e.offset.x = EditorGUILayout.Slider("Geser X", e.offset.x, -60f, 60f);
                e.offset.y = EditorGUILayout.Slider("Geser Y", e.offset.y, -60f, 60f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Reset posisi")) { e.scale = 1f; e.offset = Vector2.zero; }
                    if (GUILayout.Button("Salin ke...", GUILayout.Width(90))) CopyMenu(key);
                    if (GUILayout.Button("Kosongkan", GUILayout.Width(90))) { e.male = null; e.female = null; }
                }
            }
            DrawPreview(e);
        }
        if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(cfg);
        EditorGUILayout.EndVertical();
    }

    static void DrawPreview(HudPortraitConfig.Entry e)
    {
        var r = GUILayoutUtility.GetRect(96, 96, GUILayout.Width(96), GUILayout.Height(96));
        EditorGUI.DrawRect(r, new Color(0.08f, 0.1f, 0.16f));
        var sp = e.male != null ? e.male : e.female;
        if (sp != null && sp.texture != null)
        {
            var inner = new Rect(r.x + 8, r.y + 8, r.width - 16, r.height - 16);
            float s = e.scale <= 0.01f ? 1f : e.scale;
            var tex = sp.texture; var tr = sp.textureRect;
            float aspect = tr.width / Mathf.Max(1f, tr.height);
            float w = inner.width * s, h = w / aspect;
            if (h < inner.height * s) { h = inner.height * s; w = h * aspect; }
            var local = new Rect(inner.width * 0.5f - w * 0.5f + e.offset.x * 0.8f, inner.height * 0.5f - h * 0.5f - e.offset.y * 0.8f, w, h);
            var uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
            GUI.BeginClip(inner);
            GUI.DrawTextureWithTexCoords(local, tex, uv);
            GUI.EndClip();
        }
        else GUI.Label(r, "kosong", new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleCenter });
        Handles.DrawSolidRectangleWithOutline(r, Color.clear, new Color(0.85f, 0.7f, 0.35f));
    }

    void CopyMenu(string from)
    {
        var m = new GenericMenu();
        foreach (var job in JobInfo.All)
        {
            var keys = new List<string> { job };
            foreach (var e in JobInfo.Evos) if (e.job == job) keys.Add(e.id);
            foreach (var k in keys)
            {
                if (k == from) continue; string to = k;
                m.AddItem(new GUIContent(JobInfo.Name(job) + "/" + Label(k)), false, () =>
                {
                    var s = cfg.GetOrAdd(from); var d = cfg.GetOrAdd(to);
                    d.male = s.male; d.female = s.female; d.offset = s.offset; d.scale = s.scale; EditorUtility.SetDirty(cfg); Repaint();
                });
            }
        }
        m.ShowAsContext();
    }

    void AutoFill()
    {
        if (!AssetDatabase.IsValidFolder(folder)) { status = "Folder tidak ditemukan: " + folder; return; }
        int n = 0;
        var keys = new List<string>(JobInfo.All);
        foreach (var e in JobInfo.Evos) keys.Add(e.id);
        foreach (var k in keys)
        {
            var m = LoadSprite(k); var f = LoadSprite(k + "_F");
            if (m == null && f == null) continue;
            var en = cfg.GetOrAdd(k); if (m != null) en.male = m; if (f != null) en.female = f; n++;
        }
        EditorUtility.SetDirty(cfg); AssetDatabase.SaveAssets();
        status = "Terisi otomatis: " + n + " entri dari " + folder;
    }

    Sprite LoadSprite(string name)
    {
        foreach (var ext in new[] { ".png", ".jpg", ".jpeg" })
        {
            string p = folder.TrimEnd('/') + "/" + name + ext;
            if (!File.Exists(p)) continue;
            var imp = AssetImporter.GetAtPath(p) as TextureImporter;
            if (imp != null && imp.textureType != TextureImporterType.Sprite)
            {
                imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single; imp.alphaIsTransparency = true; imp.mipmapEnabled = false;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(p);
        }
        return null;
    }

    void ReportMissing()
    {
        var miss = new List<string>();
        var keys = new List<string>(JobInfo.All); foreach (var e in JobInfo.Evos) keys.Add(e.id);
        foreach (var k in keys) { var e = cfg.Find(k); if (e == null || (e.male == null && e.female == null)) miss.Add(k); }
        status = miss.Count == 0 ? "Semua entri sudah terisi." : miss.Count + " kosong. Lihat Console.";
        Debug.Log("[Potret HUD] Kosong (" + miss.Count + "): " + string.Join(", ", miss.ToArray()));
    }
}
