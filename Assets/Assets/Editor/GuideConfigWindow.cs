using System.IO;
using UnityEditor;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Hunter Guild > Karakter Pemandu. Ganti gambar Lynx (pemandu avatar cowok) dan Kael (pemandu avatar cewek) di tutorial:
/// seret sprite, atau "Impor gambar..." dari file di komputer (otomatis disalin ke Assets/Resources/GuidePortraits dan dijadikan Sprite),
/// atur nama, zoom, dan posisi dengan pratinjau yang mirip layout dialog.
/// </summary>
public class GuideConfigWindow : EditorWindow
{
    const string AssetPath = "Assets/Resources/GuideConfig.asset";
    const string ImportFolder = "Assets/Resources/GuidePortraits";
    GuideConfig cfg; Vector2 scroll;

    [MenuItem("Hunter Guild/Karakter Pemandu...")]
    static void Open() { var w = GetWindow<GuideConfigWindow>("Karakter Pemandu"); w.minSize = new Vector2(560, 560); }
    void OnEnable() { cfg = AssetDatabase.LoadAssetAtPath<GuideConfig>(AssetPath); }

    GuideConfig Ensure()
    {
        if (cfg != null) return cfg;
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        cfg = CreateInstance<GuideConfig>();
        AssetDatabase.CreateAsset(cfg, AssetPath);
        cfg.GetOrAdd("Lynx"); cfg.GetOrAdd("Kael");
        EditorUtility.SetDirty(cfg); AssetDatabase.SaveAssets();
        return cfg;
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("Lynx memandu avatar COWOK, Kael memandu avatar CEWEK. Gambar kosong = pakai ilustrasi sementara.", MessageType.Info);
        if (cfg == null) { if (GUILayout.Button("Buat GuideConfig.asset", GUILayout.Height(30))) Ensure(); return; }
        scroll = EditorGUILayout.BeginScrollView(scroll);
        Draw("Lynx", "Pemandu untuk avatar cowok");
        Draw("Kael", "Pemandu untuk avatar cewek");
        EditorGUILayout.EndScrollView();
        if (GUILayout.Button("Simpan", GUILayout.Height(28))) { EditorUtility.SetDirty(cfg); AssetDatabase.SaveAssets(); }
    }

    void Draw(string id, string sub)
    {
        var g = cfg.GetOrAdd(id);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField(id + "  -  " + sub, EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUILayout.VerticalScope())
            {
                g.displayName = EditorGUILayout.TextField("Nama tampil", string.IsNullOrEmpty(g.displayName) ? id : g.displayName);
                g.portrait = (Sprite)EditorGUILayout.ObjectField("Gambar", g.portrait, typeof(Sprite), false);
                g.scale = EditorGUILayout.Slider("Zoom", g.scale <= 0.01f ? 1f : g.scale, 0.4f, 2.5f);
                g.offset.x = EditorGUILayout.Slider("Geser X", g.offset.x, -600f, 600f);
                g.offset.y = EditorGUILayout.Slider("Geser Y", g.offset.y, -600f, 600f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Impor gambar...")) Import(g);
                    if (GUILayout.Button("Reset posisi")) { g.scale = 1f; g.offset = Vector2.zero; }
                    if (GUILayout.Button("Kosongkan")) g.portrait = null;
                }
            }
            Preview(g);
        }
        if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(cfg);
        EditorGUILayout.EndVertical();
    }

    static void Preview(GuideConfig.Guide g)
    {
        var r = GUILayoutUtility.GetRect(150, 200, GUILayout.Width(150), GUILayout.Height(200));
        EditorGUI.DrawRect(r, new Color(0.1f, 0.14f, 0.2f));
        EditorGUI.DrawRect(new Rect(r.x, r.yMax - 50, r.width, 50), new Color(0.03f, 0.05f, 0.1f, 0.95f));   // kotak dialog di bawah
        if (g.portrait != null && g.portrait.texture != null)
        {
            var tex = g.portrait.texture; var tr = g.portrait.textureRect;
            float aspect = tr.width / Mathf.Max(1f, tr.height);
            float s = g.scale <= 0.01f ? 1f : g.scale;
            float h = r.height * 0.95f * s, w = h * aspect;
            var local = new Rect(r.width * 0.2f - w * 0.5f + g.offset.x * 0.15f, r.height - h + 20f - g.offset.y * 0.15f, w, h);
            var uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
            GUI.BeginClip(r);
            GUI.DrawTextureWithTexCoords(local, tex, uv);
            GUI.EndClip();
        }
        else GUI.Label(r, "ilustrasi\nsementara", new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleCenter });
        Handles.DrawSolidRectangleWithOutline(r, Color.clear, new Color(0.85f, 0.7f, 0.35f));
    }

    static void Import(GuideConfig.Guide g)
    {
        string src = EditorUtility.OpenFilePanel("Pilih gambar " + g.id, "", "png,jpg,jpeg");
        if (string.IsNullOrEmpty(src)) return;
        if (!AssetDatabase.IsValidFolder(ImportFolder)) AssetDatabase.CreateFolder("Assets/Resources", "GuidePortraits");
        string dest = ImportFolder + "/" + g.id + Path.GetExtension(src).ToLowerInvariant();
        File.Copy(src, dest, true);
        AssetDatabase.ImportAsset(dest);
        var imp = AssetImporter.GetAtPath(dest) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single; imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false; imp.maxTextureSize = 4096; imp.spritePivot = new Vector2(0.5f, 0f);
            var ts = new TextureImporterSettings(); imp.ReadTextureSettings(ts); ts.spriteAlignment = (int)SpriteAlignment.BottomCenter; imp.SetTextureSettings(ts);
            imp.SaveAndReimport();
        }
        g.portrait = AssetDatabase.LoadAssetAtPath<Sprite>(dest);
    }
}
