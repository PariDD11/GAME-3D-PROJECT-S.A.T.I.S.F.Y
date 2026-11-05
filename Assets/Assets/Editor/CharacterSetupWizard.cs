using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using ReorderableList = UnityEditorInternal.ReorderableList;
using UnityEngine;
using HunterGuild;
using ACtrl = UnityEditor.Animations.AnimatorController;
using ABlend = UnityEditor.Animations.BlendTree;

/// <summary>
/// Wizard: pilih asset modelmu (FBX/prefab) untuk Player dan NPC, lalu tekan "TERAPKAN" — config, Animator Controller,
/// pengaturan rig Humanoid, dan (opsional) pembangunan ulang scene dibuat otomatis.
/// Menu: Hunter Guild > Setup Karakter &amp; NPC
/// </summary>
public class CharacterSetupWizard : EditorWindow
{
    const string Dir = "Assets/Resources/HG_Models";
    const string CfgPath = Dir + "/Config.asset";

    HGModelConfig cfg;
    Vector2 scroll;
    ReorderableList npcList;
    bool foldClips, foldNpcClips, foldProps, foldArmor, foldAdv;
    bool forceHumanoid = true, rebuildTown = true, rebuildGuild = true, rebuildHunt = true;
    string log = "";

    // slot klip animasi (Object: bisa FBX berisi klip atau file .anim)
    static readonly string[] PClipNames = { "Idle", "Walk", "Run", "Slash0", "Slash1", "Slash2", "Shoot", "Roll", "RollBack", "Hurt", "Faint", "Drink" };
    static readonly string[] NClipNames = { "Idle", "Walk", "Wave" };
    UnityEngine.Object[] pClips = new UnityEngine.Object[PClipNames.Length];
    UnityEngine.Object[] nClips = new UnityEngine.Object[NClipNames.Length];

    [MenuItem("Hunter Guild/Setup Karakter & NPC")]
    public static void Open() { var w = GetWindow<CharacterSetupWizard>("Setup Karakter & NPC"); w.minSize = new Vector2(520, 640); w.Load(); }

    void OnEnable() { Load(); }

    void Load()
    {
        cfg = AssetDatabase.LoadAssetAtPath<HGModelConfig>(CfgPath);
        if (cfg == null) cfg = CreateInstance<HGModelConfig>();
        BuildList();
    }

    void BuildList()
    {
        npcList = new ReorderableList(cfg.npcs, typeof(HGModelConfig.NpcEntry), true, true, true, true);
        npcList.drawHeaderCallback = r => EditorGUI.LabelField(r, "Model NPC (prefab / FBX)      Gender      Ras");
        npcList.elementHeight = EditorGUIUtility.singleLineHeight + 6;
        npcList.onAddCallback = l => cfg.npcs.Add(new HGModelConfig.NpcEntry());
        npcList.drawElementCallback = (r, i, a, f) =>
        {
            var e = cfg.npcs[i]; if (e == null) { e = new HGModelConfig.NpcEntry(); cfg.npcs[i] = e; }
            r.y += 2; r.height = EditorGUIUtility.singleLineHeight;
            float w = r.width;
            e.prefab = (GameObject)EditorGUI.ObjectField(new Rect(r.x, r.y, w * 0.46f, r.height), e.prefab, typeof(GameObject), false);
            e.gender = EditorGUI.Popup(new Rect(r.x + w * 0.48f, r.y, w * 0.24f, r.height), e.gender, new[] { "Bebas", "Pria", "Wanita" });
            int rr = EditorGUI.Popup(new Rect(r.x + w * 0.74f, r.y, w * 0.26f, r.height), e.race + 1, new[] { "Semua ras", "Manusia", "Dwarf", "Elf", "Beastfolk" });
            e.race = rr - 1;
        };
    }

    void OnGUI()
    {
        if (cfg == null) Load();
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.HelpBox("Pilih asset modelmu di bawah, lalu klik TERAPKAN. Model harus berupa karakter humanoid (FBX/prefab). Tidak diisi = tetap pakai karakter prosedural.", MessageType.Info);

        Header("1. PLAYER");
        cfg.playerMale = (GameObject)EditorGUILayout.ObjectField("Model pria", cfg.playerMale, typeof(GameObject), false);
        cfg.playerFemale = (GameObject)EditorGUILayout.ObjectField("Model wanita", cfg.playerFemale, typeof(GameObject), false);
        EditorGUILayout.LabelField("(cukup isi satu; gender di layar G memilih antara keduanya)", EditorStyles.miniLabel);
        cfg.playerHeight = EditorGUILayout.Slider("Tinggi player (m)", cfg.playerHeight, 1.2f, 2.3f);
        cfg.playerController = (RuntimeAnimatorController)EditorGUILayout.ObjectField("Animator Controller (opsional)", cfg.playerController, typeof(RuntimeAnimatorController), false);
        foldClips = EditorGUILayout.Foldout(foldClips, "Atau: buat Controller otomatis dari klip animasi (Mixamo dll)", true);
        if (foldClips)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Klip yang kosong dilewati. Idle/Walk/Run otomatis di-loop.", EditorStyles.miniLabel);
            for (int i = 0; i < PClipNames.Length; i++) pClips[i] = EditorGUILayout.ObjectField(PClipNames[i], pClips[i], typeof(UnityEngine.Object), false);
            EditorGUI.indentLevel--;
        }

        Header("2. NPC");
        cfg.npcHeight = EditorGUILayout.Slider("Tinggi NPC rata-rata (m)", cfg.npcHeight, 1.2f, 2.3f);
        npcList.DoLayoutList();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Dari folder terpilih di Project")) AddFromSelectedFolder();
        if (GUILayout.Button("+ Dari objek terpilih")) AddFromSelection();
        if (GUILayout.Button("Kosongkan")) { cfg.npcs.Clear(); BuildList(); }
        EditorGUILayout.EndHorizontal();
        cfg.npcController = (RuntimeAnimatorController)EditorGUILayout.ObjectField("Animator Controller NPC (opsional)", cfg.npcController, typeof(RuntimeAnimatorController), false);
        foldNpcClips = EditorGUILayout.Foldout(foldNpcClips, "Atau: buat Controller NPC otomatis dari klip", true);
        if (foldNpcClips)
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < NClipNames.Length; i++) nClips[i] = EditorGUILayout.ObjectField(NClipNames[i], nClips[i], typeof(UnityEngine.Object), false);
            EditorGUI.indentLevel--;
        }

        Header("3. SENJATA & ARMOR (opsional)");
        foldProps = EditorGUILayout.Foldout(foldProps, "Model senjata sendiri (kosong = pakai bawaan)", true);
        if (foldProps)
        {
            EditorGUI.indentLevel++;
            cfg.swordProp = (GameObject)EditorGUILayout.ObjectField("Pedang", cfg.swordProp, typeof(GameObject), false);
            cfg.bowProp = (GameObject)EditorGUILayout.ObjectField("Busur", cfg.bowProp, typeof(GameObject), false);
            cfg.shieldProp = (GameObject)EditorGUILayout.ObjectField("Perisai", cfg.shieldProp, typeof(GameObject), false);
            foldAdv = EditorGUILayout.Foldout(foldAdv, "Posisi di tangan (lanjutan)", true);
            if (foldAdv)
            {
                cfg.swordPos = EditorGUILayout.Vector3Field("Pedang pos", cfg.swordPos); cfg.swordEuler = EditorGUILayout.Vector3Field("Pedang rot", cfg.swordEuler);
                cfg.bowPos = EditorGUILayout.Vector3Field("Busur pos", cfg.bowPos); cfg.bowEuler = EditorGUILayout.Vector3Field("Busur rot", cfg.bowEuler);
                cfg.shieldPos = EditorGUILayout.Vector3Field("Perisai pos", cfg.shieldPos); cfg.shieldEuler = EditorGUILayout.Vector3Field("Perisai rot", cfg.shieldEuler);
            }
            EditorGUI.indentLevel--;
        }
        foldArmor = EditorGUILayout.Foldout(foldArmor, "Varian model per armor (ganti seluruh model saat armor dipakai)", true);
        if (foldArmor)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Tier: 1 Kulit, 2 Kulit serigala, 3 Besi, 4 Ksatria", EditorStyles.miniLabel);
            for (int i = 0; i < cfg.armorVariants.Count; i++)
            {
                var v = cfg.armorVariants[i];
                EditorGUILayout.BeginHorizontal();
                v.prefab = (GameObject)EditorGUILayout.ObjectField(v.prefab, typeof(GameObject), false);
                v.tier = EditorGUILayout.IntPopup(v.tier, new[] { "Kulit", "Serigala", "Besi", "Ksatria" }, new[] { 1, 2, 3, 4 }, GUILayout.Width(80));
                v.gender = EditorGUILayout.Popup(v.gender, new[] { "Bebas", "Pria", "Wanita" }, GUILayout.Width(70));
                if (GUILayout.Button("x", GUILayout.Width(22))) { cfg.armorVariants.RemoveAt(i); EditorGUILayout.EndHorizontal(); break; }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("+ Tambah varian armor")) cfg.armorVariants.Add(new HGModelConfig.ArmorVariant());
            EditorGUI.indentLevel--;
        }

        Header("4. OPSI");
        forceHumanoid = EditorGUILayout.ToggleLeft("Paksa rig FBX jadi Humanoid (di-reimport otomatis)", forceHumanoid);
        rebuildTown = EditorGUILayout.ToggleLeft("Bangun ulang scene Town setelah terapkan", rebuildTown);
        rebuildGuild = EditorGUILayout.ToggleLeft("Bangun ulang scene Guild", rebuildGuild);
        rebuildHunt = EditorGUILayout.ToggleLeft("Bangun ulang scene Hunt", rebuildHunt);

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Validasi", GUILayout.Height(30))) Validate();
        GUI.backgroundColor = new Color(0.4f, 0.9f, 0.5f);
        if (GUILayout.Button("TERAPKAN & GENERATE", GUILayout.Height(30))) Apply();
        GUI.backgroundColor = new Color(1f, 0.6f, 0.5f);
        if (GUILayout.Button("Kembali ke prosedural", GUILayout.Height(30))) ResetToProcedural();
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(log)) EditorGUILayout.HelpBox(log, MessageType.None);
        EditorGUILayout.EndScrollView();
    }

    static void Header(string t) { EditorGUILayout.Space(); var r = EditorGUILayout.GetControlRect(false, 22); EditorGUI.DrawRect(r, new Color(0.2f, 0.25f, 0.35f)); EditorGUI.LabelField(r, "  " + t, EditorStyles.boldLabel); }

    // ---------------------------------------------------------------- tambah massal
    void AddFromSelectedFolder()
    {
        var folders = Selection.GetFiltered<DefaultAsset>(SelectionMode.Assets).Select(AssetDatabase.GetAssetPath).Where(AssetDatabase.IsValidFolder).ToArray();
        if (folders.Length == 0) { log = "Pilih dulu sebuah folder di jendela Project."; return; }
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:GameObject", folders))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (go == null || !IsCharacter(go)) continue;
            AddNpc(go); n++;
        }
        log = n + " model ditambahkan dari folder.";
    }

    void AddFromSelection()
    {
        int n = 0;
        foreach (var go in Selection.gameObjects)
        {
            var path = AssetDatabase.GetAssetPath(go);
            if (string.IsNullOrEmpty(path)) continue;
            AddNpc(go); n++;
        }
        log = n + " model ditambahkan dari seleksi.";
    }

    static bool IsCharacter(GameObject go) { return go.GetComponentInChildren<SkinnedMeshRenderer>(true) != null; }

    void AddNpc(GameObject go)
    {
        foreach (var e in cfg.npcs) if (e != null && e.prefab == go) return;
        string n = go.name.ToLowerInvariant();
        var e2 = new HGModelConfig.NpcEntry { prefab = go };
        if (n.Contains("female") || n.Contains("woman") || n.Contains("girl") || n.Contains("wanita") || n.Contains("_f_") || n.EndsWith("_f")) e2.gender = 2;
        else if (n.Contains("male") || n.Contains("man") || n.Contains("boy") || n.Contains("pria") || n.Contains("_m_") || n.EndsWith("_m")) e2.gender = 1;
        if (n.Contains("dwarf")) e2.race = 1; else if (n.Contains("elf")) e2.race = 2;
        else if (n.Contains("beast") || n.Contains("cat") || n.Contains("wolf") || n.Contains("fox") || n.Contains("rabbit")) e2.race = 3;
        cfg.npcs.Add(e2);
    }

    // ---------------------------------------------------------------- validasi
    void Validate()
    {
        var sb = new System.Text.StringBuilder();
        Action<string, GameObject> chk = (label, go) =>
        {
            if (go == null) return;
            var path = AssetDatabase.GetAssetPath(go);
            var mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (!IsCharacter(go)) sb.AppendLine("! " + label + ": tidak ada SkinnedMeshRenderer (bukan karakter berbone?).");
            else if (mi != null && mi.animationType != ModelImporterAnimationType.Human) sb.AppendLine("! " + label + ": rig bukan Humanoid (akan diubah otomatis bila opsi aktif).");
            else sb.AppendLine("OK " + label);
        };
        chk("Player pria", cfg.playerMale); chk("Player wanita", cfg.playerFemale);
        int i = 0; foreach (var e in cfg.npcs) { i++; if (e != null) chk("NPC " + i + " (" + (e.prefab != null ? e.prefab.name : "kosong") + ")", e.prefab); }
        if (!cfg.HasPlayer && !cfg.HasNpcs) sb.AppendLine("Belum ada model: game tetap pakai karakter prosedural.");
        log = sb.ToString();
    }

    // ---------------------------------------------------------------- terapkan
    void Apply()
    {
        try
        {
            EditorUtility.DisplayProgressBar("Setup", "Menyiapkan folder...", 0.05f);
            EnsureDir("Assets/Resources"); EnsureDir(Dir);

            if (forceHumanoid)
            {
                var all = new List<GameObject> { cfg.playerMale, cfg.playerFemale };
                all.AddRange(cfg.npcs.Where(e => e != null).Select(e => e.prefab));
                all.AddRange(cfg.armorVariants.Where(v => v != null).Select(v => v.prefab));
                int k = 0; foreach (var go in all.Where(g => g != null).Distinct()) { EditorUtility.DisplayProgressBar("Setup", "Rig Humanoid: " + go.name, 0.1f + 0.3f * (k++ / (float)Math.Max(1, all.Count))); MakeHumanoid(go); }
            }

            EditorUtility.DisplayProgressBar("Setup", "Membuat Animator Controller...", 0.5f);
            if (pClips.Any(c => c != null)) cfg.playerController = BuildPlayerController();
            if (nClips.Any(c => c != null)) cfg.npcController = BuildNpcController();

            cfg.npcs.RemoveAll(e => e == null || e.prefab == null);
            cfg.armorVariants.RemoveAll(v => v == null || v.prefab == null);

            EditorUtility.DisplayProgressBar("Setup", "Menyimpan config...", 0.7f);
            var existing = AssetDatabase.LoadAssetAtPath<HGModelConfig>(CfgPath);
            if (existing == null) AssetDatabase.CreateAsset(cfg, CfgPath);
            else if (existing != cfg) { EditorUtility.CopySerialized(cfg, existing); cfg = existing; }
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            BuildList();

            string msg = "Config tersimpan: " + CfgPath + "\nPlayer: " + (cfg.HasPlayer ? "model asli" : "prosedural") + " | NPC: " + cfg.npcs.Count + " model";
            if (cfg.HasPlayer && cfg.playerController == null) msg += "\n! Player belum punya Animator Controller (isi di atas atau pakai klip otomatis) — tanpa animasi model akan T-pose.";
            if (cfg.HasNpcs && cfg.npcController == null) msg += "\n! NPC belum punya Animator Controller — NPC akan diam T-pose.";
            log = msg;

            EditorUtility.DisplayProgressBar("Setup", "Membangun ulang scene...", 0.8f);
            if (rebuildTown) EditorApplication.ExecuteMenuItem("Hunter Guild/Build Town Scene");
            if (rebuildGuild) EditorApplication.ExecuteMenuItem("Hunter Guild/Build Guild Interior Scene");
            if (rebuildHunt) EditorApplication.ExecuteMenuItem("Hunter Guild/Build Hunt Area (Hutan Lerai)");
        }
        catch (Exception ex) { log = "Gagal: " + ex.Message; Debug.LogException(ex); }
        finally { EditorUtility.ClearProgressBar(); }
    }

    void ResetToProcedural()
    {
        if (!EditorUtility.DisplayDialog("Kembali ke prosedural", "Hapus config model asli? (asset modelmu tidak dihapus)", "Ya", "Batal")) return;
        if (AssetDatabase.LoadAssetAtPath<HGModelConfig>(CfgPath) != null) AssetDatabase.DeleteAsset(CfgPath);
        cfg = CreateInstance<HGModelConfig>(); BuildList();
        log = "Config dihapus. Jalankan ulang builder scene agar NPC prosedural dibuat lagi.";
    }

    static void EnsureDir(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    // ---------------------------------------------------------------- importer
    static void MakeHumanoid(GameObject go)
    {
        var path = AssetDatabase.GetAssetPath(go);
        var mi = AssetImporter.GetAtPath(path) as ModelImporter;
        if (mi == null) return;       // prefab biasa: abaikan
        if (mi.animationType != ModelImporterAnimationType.Human)
        {
            mi.animationType = ModelImporterAnimationType.Human;
            mi.SaveAndReimport();
        }
    }

    static AnimationClip ResolveClip(UnityEngine.Object o, bool loop)
    {
        if (o == null) return null;
        var path = AssetDatabase.GetAssetPath(o);
        AnimationClip clip = o as AnimationClip;
        if (clip == null)
        {
            clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        }
        if (clip == null) return null;
        var mi = AssetImporter.GetAtPath(path) as ModelImporter;
        if (mi != null)
        {
            bool changed = false;
            if (mi.animationType != ModelImporterAnimationType.Human) { mi.animationType = ModelImporterAnimationType.Human; changed = true; }
            var arr = mi.clipAnimations; if (arr == null || arr.Length == 0) arr = mi.defaultClipAnimations;
            for (int i = 0; i < arr.Length; i++) if (arr[i].loopTime != loop) { arr[i].loopTime = loop; changed = true; }
            if (changed) { mi.clipAnimations = arr; mi.SaveAndReimport(); }
            clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__")) ?? clip;
        }
        else
        {
            var st = AnimationUtility.GetAnimationClipSettings(clip);
            if (st.loopTime != loop) { st.loopTime = loop; AnimationUtility.SetAnimationClipSettings(clip, st); EditorUtility.SetDirty(clip); }
        }
        return clip;
    }

    // ---------------------------------------------------------------- Animator Controller
    ACtrl BuildPlayerController()
    {
        string path = Dir + "/PlayerController.controller";
        if (AssetDatabase.LoadAssetAtPath<ACtrl>(path) != null) AssetDatabase.DeleteAsset(path);
        var c = ACtrl.CreateAnimatorControllerAtPath(path);
        c.AddParameter("Speed", AnimatorControllerParameterType.Float);
        c.AddParameter("Drawn", AnimatorControllerParameterType.Bool);
        c.AddParameter("Bow", AnimatorControllerParameterType.Bool);
        c.AddParameter("Dead", AnimatorControllerParameterType.Bool);
        foreach (var t in new[] { "Slash0", "Slash1", "Slash2", "Shoot", "Roll", "RollBack", "Hurt", "Faint", "Drink" }) c.AddParameter(t, AnimatorControllerParameterType.Trigger);
        var sm = c.layers[0].stateMachine;

        var idle = ResolveClip(pClips[0], true); var walk = ResolveClip(pClips[1], true); var run = ResolveClip(pClips[2], true);
        var loco = sm.AddState("Locomotion");
        var tree = new ABlend { name = "Loco", blendParameter = "Speed", useAutomaticThresholds = false };
        AssetDatabase.AddObjectToAsset(tree, c);
        if (idle != null) tree.AddChild(idle, 0f);
        if (walk != null) tree.AddChild(walk, 2.5f);
        if (run != null) tree.AddChild(run, 5.5f);
        if (tree.children.Length == 0) tree.AddChild(idle ?? walk ?? run ?? new AnimationClip(), 0f);
        loco.motion = tree;
        sm.defaultState = loco;

        for (int i = 3; i < PClipNames.Length; i++)
        {
            var clip = ResolveClip(pClips[i], false);
            if (clip == null) continue;
            string n = PClipNames[i];
            var st = sm.AddState(n); st.motion = clip;
            var any = sm.AddAnyStateTransition(st);
            any.AddCondition(AnimatorConditionMode.If, 0, n);
            any.hasExitTime = false; any.duration = 0.08f; any.canTransitionToSelf = false;
            if (n == "Faint")
            {
                var back = st.AddTransition(loco);
                back.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
                back.hasExitTime = false; back.duration = 0.2f;
            }
            else
            {
                var back = st.AddTransition(loco);
                back.hasExitTime = true; back.exitTime = 0.9f; back.duration = 0.15f;
            }
        }
        EditorUtility.SetDirty(c); AssetDatabase.SaveAssets();
        return c;
    }

    ACtrl BuildNpcController()
    {
        string path = Dir + "/NpcController.controller";
        if (AssetDatabase.LoadAssetAtPath<ACtrl>(path) != null) AssetDatabase.DeleteAsset(path);
        var c = ACtrl.CreateAnimatorControllerAtPath(path);
        c.AddParameter("Speed", AnimatorControllerParameterType.Float);
        c.AddParameter("Wave", AnimatorControllerParameterType.Trigger);
        var sm = c.layers[0].stateMachine;
        var idle = ResolveClip(nClips[0], true); var walk = ResolveClip(nClips[1], true); var wave = ResolveClip(nClips[2], false);
        var loco = sm.AddState("Locomotion");
        var tree = new ABlend { name = "Loco", blendParameter = "Speed", useAutomaticThresholds = false };
        AssetDatabase.AddObjectToAsset(tree, c);
        if (idle != null) tree.AddChild(idle, 0f);
        if (walk != null) tree.AddChild(walk, 1.2f);
        if (tree.children.Length == 0) tree.AddChild(idle ?? walk ?? new AnimationClip(), 0f);
        loco.motion = tree; sm.defaultState = loco;
        if (wave != null)
        {
            var st = sm.AddState("Wave"); st.motion = wave;
            var any = sm.AddAnyStateTransition(st); any.AddCondition(AnimatorConditionMode.If, 0, "Wave"); any.hasExitTime = false; any.duration = 0.1f;
            var back = st.AddTransition(loco); back.hasExitTime = true; back.exitTime = 0.9f; back.duration = 0.15f;
        }
        EditorUtility.SetDirty(c); AssetDatabase.SaveAssets();
        return c;
    }
}
