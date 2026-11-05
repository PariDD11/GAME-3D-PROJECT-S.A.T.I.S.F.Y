using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Hunter Guild > Ganti NPC. Atur model 3D, foto 2D dialog, dan aksi/animasi NPC berdasarkan gender, ras, pekerjaan, penting/biasa, atau nama khusus.
/// Tidak mengubah scene: penggantian dilakukan saat game berjalan (aturan paling spesifik menang).
/// </summary>
public class NpcSwapWindow : EditorWindow
{
    const string TablePath = "Assets/Resources/NpcSwapTable.asset";
    static readonly string[] Tabs = { "Aturan", "Daftar NPC (hasil pindai)", "Bantuan" };
    static readonly string[] Genders = { "Bebas", "Pria", "Wanita" };
    static readonly string[] Imps = { "Bebas", "NPC penting", "NPC biasa" };
    static readonly string[] Filters = { "Semua", "Khusus nama", "Pria", "Wanita", "Penting", "Biasa" };

    NpcSwapTable table;
    int tab, filter, raceF = -1;
    string search = "";
    Vector2 scroll;
    HashSet<NpcSwapTable.Rule> open = new HashSet<NpcSwapTable.Rule>();
    string rosterScene = "";

    [MenuItem("Hunter Guild/Ganti NPC")]
    static void Open() { var w = GetWindow<NpcSwapWindow>("Ganti NPC"); w.minSize = new Vector2(760f, 460f); w.Show(); }

    void OnEnable() { Load(); }

    void Load()
    {
        table = AssetDatabase.LoadAssetAtPath<NpcSwapTable>(TablePath);
        if (table == null)
        {
            if (!Directory.Exists("Assets/Resources")) Directory.CreateDirectory("Assets/Resources");
            table = CreateInstance<NpcSwapTable>();
            AssetDatabase.CreateAsset(table, TablePath);
            AssetDatabase.SaveAssets();
        }
    }

    void Save() { EditorUtility.SetDirty(table); AssetDatabase.SaveAssets(); SwapBackup.Backup(table); }

    static string[] RoleNames()
    {
        var n = new string[NpcKit.Count + 1]; n[0] = "Bebas";
        for (int i = 0; i < NpcKit.Count; i++) n[i + 1] = NpcKit.OutfitName[i];
        return n;
    }
    static string[] RaceOpts() { return new[] { "Bebas", "Manusia", "Dwarf", "Elf", "Beastfolk" }; }

    void OnGUI()
    {
        if (table == null) Load();
        if (EditorApplication.isPlaying) { EditorGUILayout.HelpBox("Keluar dari Play mode untuk mengatur NPC.", MessageType.Info); return; }
        tab = GUILayout.Toolbar(tab, Tabs, GUILayout.Height(26));
        if (tab == 0) DrawRules();
        else if (tab == 1) DrawRoster();
        else DrawHelp();
    }

    // ------------------------------------------------------------ aturan
    void DrawRules()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Aturan baru", GUILayout.Width(110))) { var r = new NpcSwapTable.Rule(); table.rules.Add(r); open.Add(r); Save(); }
        if (GUILayout.Button("+ Kerangka Gender x Ras (8)", GUILayout.Width(190))) { Skeleton(true); }
        if (GUILayout.Button("+ Kerangka per Pekerjaan (16)", GUILayout.Width(190))) { Skeleton(false); }
        if (GUILayout.Button("+ Penting / Biasa", GUILayout.Width(120)))
        {
            Add("NPC penting", 0, -1, -1, 1); Add("NPC biasa", 0, -1, -1, 2); Save();
        }
        EditorGUILayout.EndHorizontal();

        filter = GUILayout.Toolbar(filter, Filters);
        EditorGUILayout.BeginHorizontal();
        search = EditorGUILayout.TextField("Cari", search);
        raceF = EditorGUILayout.Popup(raceF + 1, RaceOpts(), GUILayout.Width(110)) - 1;
        EditorGUILayout.EndHorizontal();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        string q = search.ToLowerInvariant();
        NpcSwapTable.Rule remove = null; bool changed = false;
        foreach (var r in table.rules)
        {
            if (!PassFilter(r, q)) continue;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            bool isOpen = open.Contains(r);
            bool now = EditorGUILayout.Foldout(isOpen, Summary(r), true);
            if (now != isOpen) { if (now) open.Add(r); else open.Remove(r); }
            GUILayout.FlexibleSpace();
            string st = (r.prefabs.Count > 0 ? "Model " : "") + (r.portraitTex != null || r.portraitTexes.Count > 0 || r.portrait != null || r.portraits.Count > 0 ? "Foto " : "") + (r.controller != null ? "Aksi" : "");
            GUILayout.Label(st, EditorStyles.miniLabel);
            r.enabled = GUILayout.Toggle(r.enabled, "Aktif", GUILayout.Width(52));
            if (GUILayout.Button("Hapus", GUILayout.Width(52)) && EditorUtility.DisplayDialog("Hapus aturan", "Hapus \"" + r.label + "\"?", "Ya", "Batal")) remove = r;
            EditorGUILayout.EndHorizontal();
            if (open.Contains(r)) { EditorGUI.BeginChangeCheck(); DrawRule(r); if (EditorGUI.EndChangeCheck()) changed = true; }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();
        if (remove != null) { table.rules.Remove(remove); changed = true; }
        if (changed) Save();
    }

    bool PassFilter(NpcSwapTable.Rule r, string q)
    {
        if (filter == 1 && string.IsNullOrEmpty(r.npcName)) return false;
        if (filter == 2 && r.gender != 1) return false;
        if (filter == 3 && r.gender != 2) return false;
        if (filter == 4 && r.importance != 1) return false;
        if (filter == 5 && r.importance != 2) return false;
        if (raceF >= 0 && r.race != raceF) return false;
        if (q.Length > 0 && (r.label + " " + r.npcName).ToLowerInvariant().IndexOf(q) < 0) return false;
        return true;
    }

    static string Summary(NpcSwapTable.Rule r)
    {
        var p = new List<string>();
        if (!string.IsNullOrEmpty(r.npcName)) p.Add("Nama: " + r.npcName);
        if (r.gender != 0) p.Add(Genders[r.gender]);
        if (r.race >= 0) p.Add(NpcSwapRuntime.RaceNames[r.race]);
        if (r.role >= 0 && r.role < NpcKit.Count) p.Add(NpcKit.OutfitName[r.role]);
        if (r.importance != 0) p.Add(Imps[r.importance]);
        if (!string.IsNullOrEmpty(r.scene)) p.Add("@" + r.scene);
        return r.label + (p.Count > 0 ? "   [" + string.Join(" / ", p.ToArray()) + "]" : "   [semua NPC]");
    }

    void DrawRule(NpcSwapTable.Rule r)
    {
        r.label = EditorGUILayout.TextField("Nama aturan", r.label);
        EditorGUILayout.LabelField("Berlaku untuk (kosong/Bebas = semua)", EditorStyles.boldLabel);
        r.npcName = EditorGUILayout.TextField(new GUIContent("Nama NPC khusus", "Isi persis seperti nama NPC (lihat tab Daftar NPC). Aturan bernama selalu menang."), r.npcName);
        r.scene = EditorGUILayout.TextField(new GUIContent("Scene", "Contoh: Town, StartVillage, DungeonCity. Kosong = semua scene."), r.scene);
        EditorGUILayout.BeginHorizontal();
        r.gender = EditorGUILayout.Popup("Jenis kelamin", r.gender, Genders);
        r.importance = EditorGUILayout.Popup("Penting?", r.importance, Imps);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        r.race = EditorGUILayout.Popup("Ras", r.race + 1, RaceOpts()) - 1;
        r.role = EditorGUILayout.Popup("Pekerjaan", r.role + 1, RoleNames()) - 1;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("Model 3D (isi lebih dari satu = divariasikan per NPC)", EditorStyles.boldLabel);
        int del = -1;
        for (int i = 0; i < r.prefabs.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            r.prefabs[i] = (GameObject)EditorGUILayout.ObjectField(r.prefabs[i], typeof(GameObject), false);
            if (GUILayout.Button("x", GUILayout.Width(22))) del = i;
            EditorGUILayout.EndHorizontal();
        }
        if (del >= 0) r.prefabs.RemoveAt(del);
        var drop = (GameObject)EditorGUILayout.ObjectField("+ tambah model", null, typeof(GameObject), false);
        if (drop != null) r.prefabs.Add(drop);
        EditorGUILayout.BeginHorizontal();
        r.heightScale = EditorGUILayout.FloatField(new GUIContent("Skala tinggi", "1 = tinggi NPC aslinya. Model otomatis dicocokkan ke tinggi ini."), r.heightScale);
        r.yOffset = EditorGUILayout.FloatField("Naik (Y)", r.yOffset);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("Foto 2D di dialog", EditorStyles.boldLabel);
        if (r.portrait != null && r.portraitTex == null) { r.portraitTex = r.portrait.texture; r.portrait = null; }      // migrasi dari Sprite lama
        if (r.portraits.Count > 0) { foreach (var sp in r.portraits) if (sp != null) r.portraitTexes.Add(sp.texture); r.portraits.Clear(); }
        r.portraitTex = (Texture2D)EditorGUILayout.ObjectField(new GUIContent("Foto utama", "Seret PNG/JPG biasa ke sini, tidak perlu diubah jadi Sprite. Kosong = pakai variasi di bawah atau Resources/Portraits/NamaNPC.png."), r.portraitTex, typeof(Texture2D), false);
        del = -1;
        for (int i = 0; i < r.portraitTexes.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            r.portraitTexes[i] = (Texture2D)EditorGUILayout.ObjectField("Variasi " + (i + 1), r.portraitTexes[i], typeof(Texture2D), false);
            if (GUILayout.Button("x", GUILayout.Width(22))) del = i;
            EditorGUILayout.EndHorizontal();
        }
        if (del >= 0) r.portraitTexes.RemoveAt(del);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Tambah slot foto", GUILayout.Width(150))) r.portraitTexes.Add(null);
        var dropP = (Texture2D)EditorGUILayout.ObjectField("atau seret PNG ke sini", null, typeof(Texture2D), false);
        if (dropP != null) r.portraitTexes.Add(dropP);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox(string.IsNullOrEmpty(r.npcName)
            ? "Aturan umum: tiap NPC mendapat satu foto dari daftar ini (tetap)."
            : "Aturan NPC khusus: semua foto dipakai bergantian, ganti tiap percakapan baru.", MessageType.None);
        EditorGUILayout.LabelField("Atur tampilan foto (lihat pratinjau di bawah)", EditorStyles.miniBoldLabel);
        r.portraitScale = EditorGUILayout.Slider(new GUIContent("Ukuran", "1 = normal. Lebih besar = foto lebih besar."), r.portraitScale <= 0f ? 1f : r.portraitScale, 0.4f, 1.8f);
        r.portraitX = EditorGUILayout.Slider(new GUIContent("Geser kanan/kiri", "px; positif = ke kanan"), r.portraitX, -300f, 300f);
        r.portraitY = EditorGUILayout.Slider(new GUIContent("Geser atas/bawah", "px; positif = naik"), r.portraitY, -300f, 300f);
        EditorGUILayout.BeginHorizontal();
        r.portraitFlip = EditorGUILayout.Toggle("Balik horizontal", r.portraitFlip);
        r.portraitSide = EditorGUILayout.Popup("Posisi", r.portraitSide, new[] { "Otomatis", "Kiri", "Kanan" });
        if (GUILayout.Button("Reset", GUILayout.Width(60))) { r.portraitScale = 1f; r.portraitX = 0f; r.portraitY = 0f; r.portraitFlip = false; r.portraitSide = 0; }
        EditorGUILayout.EndHorizontal();
        DrawPortraitPreview(r);

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("Aksi / animasi", EditorStyles.boldLabel);
        r.controller = (RuntimeAnimatorController)EditorGUILayout.ObjectField(new GUIContent("Animator Controller", "Parameter yang dikirim game: Speed (float), Wave (trigger), Talking (bool), Working (bool)."), r.controller, typeof(RuntimeAnimatorController), false);
        EditorGUILayout.LabelField("Kecepatan jalan (kalau NPC terlihat terlalu cepat)", EditorStyles.miniBoldLabel);
        r.moveSpeedMul = EditorGUILayout.Slider(new GUIContent("Kecepatan gerak NPC", "Pengali kecepatan NPC berpindah. 1 = bawaan, 0.6 = lebih lambat."), r.moveSpeedMul <= 0f ? 1f : r.moveSpeedMul, 0.2f, 2f);
        r.animSpeed = EditorGUILayout.Slider(new GUIContent("Kecepatan animasi", "Animator.speed. Turunkan bila kaki bergerak terlalu cepat dibanding laju jalan."), r.animSpeed <= 0f ? 1f : r.animSpeed, 0.2f, 2f);
        r.speedParamScale = EditorGUILayout.Slider(new GUIContent("Skala parameter Speed", "Nilai Speed (m/s) dikali ini sebelum dikirim ke Animator. Turunkan (mis. 0.4) bila Blend Tree-mu menganggap Speed 0-1 dan NPC langsung berlari."), r.speedParamScale <= 0f ? 1f : r.speedParamScale, 0.1f, 2f);
        r.workWhenIdle = EditorGUILayout.Toggle(new GUIContent("Bekerja saat diam", "Mengaktifkan bool Working saat NPC berdiri diam dan tidak sedang bicara (pandai besi menempa, koki memasak, dll)."), r.workWhenIdle);
        EditorGUILayout.LabelField("Atau buat Controller dari klip:", EditorStyles.miniBoldLabel);
        r.clipIdle = (AnimationClip)EditorGUILayout.ObjectField("Diam (Idle)", r.clipIdle, typeof(AnimationClip), false);
        r.clipWalk = (AnimationClip)EditorGUILayout.ObjectField("Jalan", r.clipWalk, typeof(AnimationClip), false);
        r.clipWave = (AnimationClip)EditorGUILayout.ObjectField("Melambai / menyapa", r.clipWave, typeof(AnimationClip), false);
        r.clipTalk = (AnimationClip)EditorGUILayout.ObjectField("Bicara (saat dialog)", r.clipTalk, typeof(AnimationClip), false);
        r.clipWork = (AnimationClip)EditorGUILayout.ObjectField("Bekerja", r.clipWork, typeof(AnimationClip), false);
        EditorGUI.BeginDisabledGroup(r.clipIdle == null);
        if (GUILayout.Button("Buat Controller dari klip ini"))
        {
            r.controller = BuildController(r);
            r.workWhenIdle = r.workWhenIdle || r.clipWork != null;
            Save();
        }
        EditorGUI.EndDisabledGroup();
    }

    // pratinjau kasar kotak dialog (tinggi kotak dianggap 340) dengan foto di posisi hasil pengaturan
    void DrawPortraitPreview(NpcSwapTable.Rule r)
    {
        Texture2D tex = r.portraitTex;
        if (tex == null) foreach (var t in r.portraitTexes) if (t != null) { tex = t; break; }
        if (tex == null) { EditorGUILayout.HelpBox("Isi foto dulu untuk melihat pratinjau.", MessageType.None); return; }
        const float boxW = 1340f, boxH = 340f;
        float pw = Mathf.Min(position.width - 60f, 620f), k = pw / boxW;
        Rect rc = GUILayoutUtility.GetRect(pw, boxH * k + 4f);
        rc.width = pw; rc.height = boxH * k;
        EditorGUI.DrawRect(rc, new Color(0.08f, 0.07f, 0.1f));
        // garis teks tiruan
        for (int i = 0; i < 4; i++) EditorGUI.DrawRect(new Rect(rc.x + (r.portraitSide == 2 ? 30f : 170f) * k * 2f, rc.y + (40f + i * 55f) * k, 380f * k, 22f * k), new Color(0.35f, 0.28f, 0.12f));
        bool right = r.portraitSide == 2;
        float s = Mathf.Clamp(r.portraitScale <= 0f ? 1f : r.portraitScale, 0.4f, 1.8f);
        float cw = 300f * s, ch = cw * tex.height / (float)tex.width;
        float cx = right ? boxW - 24f - cw : 24f;
        var pr = new Rect(rc.x + (cx + r.portraitX) * k, rc.y + (boxH - 10f - ch - r.portraitY) * k, cw * k, ch * k);
        GUI.BeginClip(rc);
        pr.x -= rc.x; pr.y -= rc.y;
        if (r.portraitFlip) { var m = GUI.matrix; GUIUtility.ScaleAroundPivot(new Vector2(-1f, 1f), pr.center); GUI.DrawTexture(pr, tex, ScaleMode.ScaleToFit); GUI.matrix = m; }
        else GUI.DrawTexture(pr, tex, ScaleMode.ScaleToFit);
        GUI.EndClip();
    }

    void Add(string label, int gender, int race, int role, int imp)
    {
        foreach (var e in table.rules) if (e.label == label) return;
        table.rules.Add(new NpcSwapTable.Rule { label = label, gender = gender, race = race, role = role, importance = imp });
    }

    void Skeleton(bool genderRace)
    {
        if (genderRace)
            for (int g = 1; g <= 2; g++)
                for (int r = 0; r < 4; r++) Add(Genders[g] + " " + NpcSwapRuntime.RaceNames[r], g, r, -1, 0);
        else
            for (int i = 0; i < NpcKit.Count; i++) Add("Pekerjaan: " + NpcKit.OutfitName[i], 0, -1, i, 0);
        Save();
    }

    // ------------------------------------------------------------ controller dari klip
    AnimatorController BuildController(NpcSwapTable.Rule r)
    {
        string dir = "Assets/Resources/HG_Models";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        string safe = string.Concat(r.label.Split(Path.GetInvalidFileNameChars())).Trim();
        if (safe.Length == 0) safe = "Aturan";
        string path = AssetDatabase.GenerateUniqueAssetPath(dir + "/NpcCtrl_" + safe + ".controller");
        var c = AnimatorController.CreateAnimatorControllerAtPath(path);
        c.AddParameter("Speed", AnimatorControllerParameterType.Float);
        c.AddParameter("Wave", AnimatorControllerParameterType.Trigger);
        c.AddParameter("Talking", AnimatorControllerParameterType.Bool);
        c.AddParameter("Working", AnimatorControllerParameterType.Bool);
        var sm = c.layers[0].stateMachine;

        var idle = sm.AddState("Idle"); idle.motion = r.clipIdle; sm.defaultState = idle;
        if (r.clipWalk != null)
        {
            var walk = sm.AddState("Walk"); walk.motion = r.clipWalk;
            var a = idle.AddTransition(walk); a.hasExitTime = false; a.duration = 0.15f; a.AddCondition(AnimatorConditionMode.Greater, 0.2f, "Speed");
            var b = walk.AddTransition(idle); b.hasExitTime = false; b.duration = 0.15f; b.AddCondition(AnimatorConditionMode.Less, 0.2f, "Speed");
        }
        if (r.clipTalk != null)
        {
            var talk = sm.AddState("Talk"); talk.motion = r.clipTalk;
            var a = idle.AddTransition(talk); a.hasExitTime = false; a.duration = 0.2f; a.AddCondition(AnimatorConditionMode.If, 0f, "Talking");
            var b = talk.AddTransition(idle); b.hasExitTime = false; b.duration = 0.2f; b.AddCondition(AnimatorConditionMode.IfNot, 0f, "Talking");
        }
        if (r.clipWork != null)
        {
            var work = sm.AddState("Work"); work.motion = r.clipWork;
            var a = idle.AddTransition(work); a.hasExitTime = false; a.duration = 0.2f; a.AddCondition(AnimatorConditionMode.If, 0f, "Working");
            var b = work.AddTransition(idle); b.hasExitTime = false; b.duration = 0.2f; b.AddCondition(AnimatorConditionMode.IfNot, 0f, "Working");
        }
        if (r.clipWave != null)
        {
            var wave = sm.AddState("Wave"); wave.motion = r.clipWave;
            var a = sm.AddAnyStateTransition(wave); a.hasExitTime = false; a.duration = 0.15f; a.canTransitionToSelf = false; a.AddCondition(AnimatorConditionMode.If, 0f, "Wave");
            var b = wave.AddTransition(idle); b.hasExitTime = true; b.exitTime = 0.95f; b.duration = 0.15f;
        }
        AssetDatabase.SaveAssets();
        return c;
    }

    // ------------------------------------------------------------ daftar NPC
    void DrawRoster()
    {
        var scenes = new List<string> { "" };
        foreach (var g in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" })) scenes.Add(Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g)));
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Pindai scene ini ->", GUILayout.Width(130)) && rosterScene.Length > 0) Scan(new[] { rosterScene });
        int si = Mathf.Max(0, scenes.IndexOf(rosterScene));
        var names = scenes.ToArray(); names[0] = "(semua scene)";
        si = EditorGUILayout.Popup(si, names);
        rosterScene = scenes[si];
        if (GUILayout.Button("Pindai SEMUA scene", GUILayout.Width(150))) { var all = new List<string>(scenes); all.RemoveAt(0); Scan(all.ToArray()); }
        EditorGUILayout.EndHorizontal();
        search = EditorGUILayout.TextField("Cari nama", search);

        if (table.seen.Count == 0) { EditorGUILayout.HelpBox("Belum ada data. Klik \"Pindai SEMUA scene\" (scene harus sudah dibangun lewat Bangun SEMUA scene).", MessageType.Info); return; }

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        GUILayout.Label("Nama", EditorStyles.boldLabel, GUILayout.Width(170));
        GUILayout.Label("Scene", EditorStyles.boldLabel, GUILayout.Width(130));
        GUILayout.Label("Gender", EditorStyles.boldLabel, GUILayout.Width(54));
        GUILayout.Label("Ras", EditorStyles.boldLabel, GUILayout.Width(74));
        GUILayout.Label("Pekerjaan", EditorStyles.boldLabel, GUILayout.Width(100));
        GUILayout.Label("Penting", EditorStyles.boldLabel, GUILayout.Width(52));
        GUILayout.Label("Aturan yang kena", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        string q = search.ToLowerInvariant();
        foreach (var s in table.seen)
        {
            if (rosterScene.Length > 0 && s.scene != rosterScene) continue;
            if (q.Length > 0 && s.npcName.ToLowerInvariant().IndexOf(q) < 0) continue;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(s.npcName, GUILayout.Width(170));
            GUILayout.Label(s.scene, GUILayout.Width(130));
            GUILayout.Label(s.female ? "Wanita" : "Pria", GUILayout.Width(54));
            GUILayout.Label(NpcSwapRuntime.RaceNames[Mathf.Clamp(s.race, 0, 3)], GUILayout.Width(74));
            GUILayout.Label(s.role >= 0 && s.role < NpcKit.Count ? NpcKit.OutfitName[s.role] : "-", GUILayout.Width(100));
            GUILayout.Label(s.important ? "ya" : "-", GUILayout.Width(52));
            var m = BestRule(s);
            GUILayout.Label(m != null ? m.label : "(model bawaan)", GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Aturan khusus", GUILayout.Width(100)))
            {
                var r = new NpcSwapTable.Rule { label = s.npcName, npcName = s.npcName, gender = s.female ? 2 : 1, race = -1, role = -1 };
                r.gender = 0;
                table.rules.Add(r); open.Add(r); Save(); tab = 0; filter = 1; search = s.npcName;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    NpcSwapTable.Rule BestRule(NpcSwapTable.Seen s)
    {
        NpcSwapTable.Rule best = null; int bs = -1;
        foreach (var r in table.rules)
        {
            if (!r.enabled) continue;
            int sc = 0;
            if (!string.IsNullOrEmpty(r.npcName)) { if (!string.Equals(r.npcName, s.npcName, System.StringComparison.OrdinalIgnoreCase)) continue; sc += 16; }
            if (!string.IsNullOrEmpty(r.scene)) { if (r.scene != s.scene) continue; sc += 1; }
            if (r.gender != 0) { if (r.gender != (s.female ? 2 : 1)) continue; sc += 2; }
            if (r.race >= 0) { if (r.race != s.race) continue; sc += 3; }
            if (r.role >= 0) { if (r.role != s.role) continue; sc += 4; }
            if (r.importance != 0) { if ((r.importance == 1) != s.important) continue; sc += 2; }
            if (sc > bs) { bs = sc; best = r; }
        }
        return best;
    }

    void Scan(string[] sceneNames)
    {
        try
        {
            for (int i = 0; i < sceneNames.Length; i++)
            {
                string path = "Assets/Scenes/" + sceneNames[i] + ".unity";
                if (!File.Exists(path)) continue;
                EditorUtility.DisplayProgressBar("Memindai NPC", sceneNames[i], i / (float)sceneNames.Length);
                var sc = EditorSceneManager.GetSceneByPath(path);
                bool was = sc.isLoaded;
                if (!was) sc = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                table.seen.RemoveAll(x => x.scene == sceneNames[i]);
                foreach (var root in sc.GetRootGameObjects())
                    foreach (var a in root.GetComponentsInChildren<NpcActor>(true))
                        table.seen.Add(new NpcSwapTable.Seen { scene = sceneNames[i], npcName = a.displayName, race = a.race, role = a.role, female = a.female, important = NpcSwapRuntime.IsImportant(a) });
                if (!was) EditorSceneManager.CloseScene(sc, true);
            }
        }
        finally { EditorUtility.ClearProgressBar(); }
        table.seen.Sort((x, y) => { int c = string.CompareOrdinal(x.scene, y.scene); return c != 0 ? c : string.CompareOrdinal(x.npcName, y.npcName); });
        Save(); Repaint();
    }

    // ------------------------------------------------------------ bantuan
    void DrawHelp()
    {
        EditorGUILayout.HelpBox(
            "CARA PAKAI\n" +
            "1. Tab Aturan > klik \"+ Kerangka Gender x Ras\" (atau per Pekerjaan / Penting-Biasa), lalu seret prefab 3D ke tiap aturan.\n" +
            "2. Aturan paling spesifik menang: Nama khusus > Pekerjaan > Ras > Penting > Gender > Scene. Aturan \"semua NPC\" jadi cadangan.\n" +
            "3. Foto 2D dialog: seret PNG/JPG biasa ke \"Foto utama\" (tidak perlu jadi Sprite). Bisa per aturan atau per nama NPC. Kalau kosong, game memakai Resources/Portraits/NamaNPC.png, lalu placeholder.\n" +
            "4. Aksi: pilih Animator Controller, atau isi klip lalu \"Buat Controller dari klip\". Parameter yang dikirim game: Speed (float), Wave (trigger, saat menyapa), Talking (bool, saat dialog terbuka), Working (bool, bila \"Bekerja saat diam\").\n" +
            "5. Tab Daftar NPC menampilkan semua NPC hasil pindai beserta aturan yang mengenainya.\n\n" +
            "NPC PENTING = NPC yang punya interaksi selain obrolan biasa (toko, guild, quest, penjaga kunci).\n" +
            "Model aslinya hanya diganti saat game berjalan; scene tidak diubah. Kosongkan/non-aktifkan aturan untuk kembali ke model bawaan.\n" +
            "Model 3D otomatis dicocokkan ke tinggi NPC dan collider bawaan prefab dibuang.", MessageType.Info);
    }
}
