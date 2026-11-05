using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Hunter Guild > Ganti Monster (v35z102n78). Pasang model 3D buatanmu (FBX / prefab) sebagai pengganti model monster bawaan game,
/// per id monster, per bentuk dasar, atau per boss/biasa. Tidak mengubah scene: penggantian dilakukan saat game berjalan.
/// Aturan paling spesifik menang: id > bentuk > boss/biasa > scene. Hanya aturan yang punya prefab yang dipakai.
/// </summary>
public class MonsterSwapWindow : EditorWindow
{
    const string TablePath = "Assets/Resources/MonsterSwapTable.asset";
    static readonly string[] Tabs = { "Aturan", "Daftar Monster", "Bantuan" };
    static readonly string[] Tiers = { "Bebas", "Hanya boss", "Hanya bukan boss" };

    MonsterSwapTable table;
    List<MonsterDef> defs = new List<MonsterDef>();
    int tab;
    string search = "", rosterSearch = "", rosterScene = "";
    Vector2 scroll, rscroll;
    readonly HashSet<MonsterSwapTable.Rule> open = new HashSet<MonsterSwapTable.Rule>();

    [MenuItem("Hunter Guild/Ganti Monster")]
    static void Open() { var w = GetWindow<MonsterSwapWindow>("Ganti Monster"); w.minSize = new Vector2(720f, 460f); w.Show(); }

    void OnEnable() { Load(); }

    void Load()
    {
        table = AssetDatabase.LoadAssetAtPath<MonsterSwapTable>(TablePath);
        if (table == null)
        {
            if (!Directory.Exists("Assets/Resources")) Directory.CreateDirectory("Assets/Resources");
            table = CreateInstance<MonsterSwapTable>();
            AssetDatabase.CreateAsset(table, TablePath);
            AssetDatabase.SaveAssets();
        }
        defs = MonsterDB.Every();
    }

    void Save() { EditorUtility.SetDirty(table); AssetDatabase.SaveAssets(); SwapBackup.Backup(table); }

    void OnGUI()
    {
        if (table == null) Load();
        if (EditorApplication.isPlaying) { EditorGUILayout.HelpBox("Keluar dari Play mode untuk mengatur model monster.", MessageType.Info); return; }
        tab = GUILayout.Toolbar(tab, Tabs, GUILayout.Height(26));
        if (tab == 0) DrawRules();
        else if (tab == 1) DrawRoster();
        else DrawHelp();
    }

    // ------------------------------------------------------------ aturan
    void DrawRules()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Aturan baru", GUILayout.Width(110))) { var r = new MonsterSwapTable.Rule(); table.rules.Add(r); open.Add(r); Save(); }
        if (GUILayout.Button("+ Kerangka per Bentuk (" + MonsterSwapRuntime.Shapes.Length + ")", GUILayout.Width(200)))
        {
            foreach (var s in MonsterSwapRuntime.Shapes) Add("Bentuk: " + s, "", s, 0);
            Save();
        }
        if (GUILayout.Button("+ Boss / Biasa", GUILayout.Width(110))) { Add("Semua boss", "", "", 1); Add("Semua monster biasa", "", "", 2); Save(); }
        EditorGUILayout.EndHorizontal();
        search = EditorGUILayout.TextField("Cari", search);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        string q = search.ToLowerInvariant();
        MonsterSwapTable.Rule remove = null; bool changed = false;
        foreach (var r in table.rules)
        {
            if (q.Length > 0 && (r.label + " " + r.monsterId + " " + r.shape).ToLowerInvariant().IndexOf(q) < 0) continue;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            bool isOpen = open.Contains(r);
            bool now = EditorGUILayout.Foldout(isOpen, Summary(r), true);
            if (now != isOpen) { if (now) open.Add(r); else open.Remove(r); }
            GUILayout.FlexibleSpace();
            string st = (MonsterSwapRuntime.HasPrefab(r) ? "Model " : "(belum ada model) ") + (r.controller != null ? "Aksi" : "");
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

    static string Summary(MonsterSwapTable.Rule r)
    {
        var p = new List<string>();
        if (!string.IsNullOrEmpty(r.monsterId)) p.Add("id: " + r.monsterId);
        if (!string.IsNullOrEmpty(r.shape)) p.Add("bentuk: " + r.shape);
        if (r.tier != 0) p.Add(Tiers[r.tier]);
        if (!string.IsNullOrEmpty(r.scene)) p.Add("@" + r.scene);
        return r.label + (p.Count > 0 ? "   [" + string.Join(" / ", p.ToArray()) + "]" : "   [semua monster]");
    }

    void Add(string label, string id, string shape, int tier)
    {
        foreach (var e in table.rules) if (e.label == label) return;
        table.rules.Add(new MonsterSwapTable.Rule { label = label, monsterId = id, shape = shape, tier = tier });
    }

    string[] IdOptions()
    {
        var o = new string[defs.Count + 1]; o[0] = "(pilih dari daftar)";
        for (int i = 0; i < defs.Count; i++) o[i + 1] = defs[i].id + "  -  " + defs[i].name;
        return o;
    }

    static string[] ShapeOptions()
    {
        var o = new string[MonsterSwapRuntime.Shapes.Length + 1]; o[0] = "Bebas";
        for (int i = 0; i < MonsterSwapRuntime.Shapes.Length; i++) o[i + 1] = MonsterSwapRuntime.Shapes[i];
        return o;
    }

    void DrawRule(MonsterSwapTable.Rule r)
    {
        r.label = EditorGUILayout.TextField("Nama aturan", r.label);
        EditorGUILayout.LabelField("Berlaku untuk (kosong / Bebas = semua)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        r.monsterId = EditorGUILayout.TextField(new GUIContent("Id monster khusus", "Isi persis seperti id monster (lihat tab Daftar Monster). Aturan ber-id selalu menang."), r.monsterId);
        int pick = EditorGUILayout.Popup(0, IdOptions(), GUILayout.Width(190));
        if (pick > 0) r.monsterId = defs[pick - 1].id;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        int si = 0;
        if (!string.IsNullOrEmpty(r.shape)) { int k = System.Array.IndexOf(MonsterSwapRuntime.Shapes, r.shape); si = k >= 0 ? k + 1 : 0; }
        si = EditorGUILayout.Popup(new GUIContent("Bentuk dasar", "Semua monster dengan bentuk ini memakai model yang sama (mis. wolf = semua serigala)."), si, ShapeOptions());
        r.shape = si <= 0 ? "" : MonsterSwapRuntime.Shapes[si - 1];
        r.tier = EditorGUILayout.Popup("Boss?", r.tier, Tiers);
        EditorGUILayout.EndHorizontal();
        r.scene = EditorGUILayout.TextField(new GUIContent("Scene", "Contoh: Town, StartVillage, DungeonCity. Kosong = semua scene."), r.scene);

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("Model 3D (isi lebih dari satu = divariasikan per individu)", EditorStyles.boldLabel);
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
        r.heightScale = EditorGUILayout.Slider(new GUIContent("Skala tinggi", "1 = setinggi model bawaan monster itu (sudah termasuk ukuran monster). Model otomatis dicocokkan ke tinggi ini, telapak kaki di tanah."), r.heightScale <= 0f ? 1f : r.heightScale, 0.2f, 3f);
        EditorGUILayout.BeginHorizontal();
        r.yOffset = EditorGUILayout.FloatField(new GUIContent("Naik (Y)", "Geser model ke atas (meter). Berguna untuk monster terbang / melayang."), r.yOffset);
        r.yaw = EditorGUILayout.FloatField(new GUIContent("Putar (derajat)", "Isi 180 / 90 bila modelmu menghadap ke belakang / samping saat dipasang."), r.yaw);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("Aksi / animasi", EditorStyles.boldLabel);
        r.controller = (RuntimeAnimatorController)EditorGUILayout.ObjectField(new GUIContent("Animator Controller",
            "Parameter yang dikirim game: Speed (float, m/dtk), Telegraph (bool, ancang-ancang), Attack (trigger, awal ancang-ancang), Hurt (trigger), Dead (bool) atau Die (trigger)."), r.controller, typeof(RuntimeAnimatorController), false);
        r.animSpeed = EditorGUILayout.Slider(new GUIContent("Kecepatan animasi", "Animator.speed."), r.animSpeed <= 0f ? 1f : r.animSpeed, 0.2f, 2f);
        r.speedParamScale = EditorGUILayout.Slider(new GUIContent("Skala parameter Speed", "Speed (m/dtk) dikali ini sebelum dikirim ke Animator. Turunkan bila Blend Tree-mu menganggap Speed 0-1."), r.speedParamScale <= 0f ? 1f : r.speedParamScale, 0.1f, 2f);
        r.lifeMotion = EditorGUILayout.Toggle(new GUIContent("Gerak hidup prosedural", "Model tanpa Animator / tanpa controller digerakkan lewat kode: naik-turun saat berjalan, miring, menerjang saat menyerang."), r.lifeMotion);
        EditorGUILayout.LabelField("Atau buat Controller dari klip:", EditorStyles.miniBoldLabel);
        r.clipIdle = (AnimationClip)EditorGUILayout.ObjectField("Diam (Idle)", r.clipIdle, typeof(AnimationClip), false);
        r.clipWalk = (AnimationClip)EditorGUILayout.ObjectField("Jalan / lari", r.clipWalk, typeof(AnimationClip), false);
        r.clipAttack = (AnimationClip)EditorGUILayout.ObjectField("Serang", r.clipAttack, typeof(AnimationClip), false);
        r.clipHurt = (AnimationClip)EditorGUILayout.ObjectField("Terkena pukulan", r.clipHurt, typeof(AnimationClip), false);
        r.clipDie = (AnimationClip)EditorGUILayout.ObjectField("Mati", r.clipDie, typeof(AnimationClip), false);
        EditorGUI.BeginDisabledGroup(r.clipIdle == null);
        if (GUILayout.Button("Buat Controller dari klip ini")) { r.controller = BuildController(r); Save(); }
        EditorGUI.EndDisabledGroup();
    }

    // ------------------------------------------------------------ controller dari klip
    AnimatorController BuildController(MonsterSwapTable.Rule r)
    {
        string dir = "Assets/Resources/HG_Models";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        string safe = string.Concat(r.label.Split(Path.GetInvalidFileNameChars())).Trim();
        if (safe.Length == 0) safe = "Aturan";
        string path = AssetDatabase.GenerateUniqueAssetPath(dir + "/MonsterCtrl_" + safe + ".controller");
        var c = AnimatorController.CreateAnimatorControllerAtPath(path);
        c.AddParameter("Speed", AnimatorControllerParameterType.Float);
        c.AddParameter("Telegraph", AnimatorControllerParameterType.Bool);
        c.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        c.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        c.AddParameter("Dead", AnimatorControllerParameterType.Bool);
        var sm = c.layers[0].stateMachine;

        var idle = sm.AddState("Idle"); idle.motion = r.clipIdle; sm.defaultState = idle;
        if (r.clipWalk != null)
        {
            var walk = sm.AddState("Walk"); walk.motion = r.clipWalk;
            var a = idle.AddTransition(walk); a.hasExitTime = false; a.duration = 0.15f; a.AddCondition(AnimatorConditionMode.Greater, 0.3f, "Speed");
            var b = walk.AddTransition(idle); b.hasExitTime = false; b.duration = 0.15f; b.AddCondition(AnimatorConditionMode.Less, 0.3f, "Speed");
        }
        if (r.clipAttack != null)
        {
            var atk = sm.AddState("Attack"); atk.motion = r.clipAttack;
            var a = sm.AddAnyStateTransition(atk); a.hasExitTime = false; a.duration = 0.1f; a.canTransitionToSelf = false; a.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            var b = atk.AddTransition(idle); b.hasExitTime = true; b.exitTime = 0.95f; b.duration = 0.15f;
        }
        if (r.clipHurt != null)
        {
            var hurt = sm.AddState("Hurt"); hurt.motion = r.clipHurt;
            var a = sm.AddAnyStateTransition(hurt); a.hasExitTime = false; a.duration = 0.05f; a.canTransitionToSelf = false; a.AddCondition(AnimatorConditionMode.If, 0f, "Hurt");
            var b = hurt.AddTransition(idle); b.hasExitTime = true; b.exitTime = 0.9f; b.duration = 0.1f;
        }
        if (r.clipDie != null)
        {
            var die = sm.AddState("Die"); die.motion = r.clipDie;
            var a = sm.AddAnyStateTransition(die); a.hasExitTime = false; a.duration = 0.1f; a.canTransitionToSelf = false; a.AddCondition(AnimatorConditionMode.If, 0f, "Dead");
        }
        AssetDatabase.SaveAssets();
        return c;
    }

    // ------------------------------------------------------------ daftar monster
    void DrawRoster()
    {
        EditorGUILayout.BeginHorizontal();
        rosterSearch = EditorGUILayout.TextField("Cari", rosterSearch);
        rosterScene = EditorGUILayout.TextField(new GUIContent("Scene", "Opsional: pratinjau aturan yang berlaku di scene ini. Kosong = abaikan aturan ber-scene."), rosterScene, GUILayout.Width(300));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox("Daftar semua monster di game, beserta aturan Ganti Monster yang saat ini mengenainya. \"bawaan\" = model dari kode (MonsterKit).", MessageType.None);
        rscroll = EditorGUILayout.BeginScrollView(rscroll);
        string q = rosterSearch.ToLowerInvariant();
        foreach (var d in defs)
        {
            if (q.Length > 0 && (d.id + " " + d.name + " " + d.shape).ToLowerInvariant().IndexOf(q) < 0) continue;
            bool boss = BossBrain.Has(d.id);
            var rule = MonsterSwapRuntime.Match(table, d, boss, rosterScene);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(d.id, GUILayout.Width(150));
            GUILayout.Label(d.name + (boss ? "  [BOSS]" : "") + (d.passive ? "  [hewan]" : ""), GUILayout.Width(220));
            GUILayout.Label(d.shape, GUILayout.Width(80));
            GUILayout.Label(rule != null ? "model: " + rule.label : "bawaan", GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Buat aturan", GUILayout.Width(90)))
            {
                var nr = new MonsterSwapTable.Rule { label = d.name, monsterId = d.id };
                table.rules.Add(nr); open.Add(nr); Save(); tab = 0;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    // ------------------------------------------------------------ bantuan
    void DrawHelp()
    {
        EditorGUILayout.HelpBox(
            "CARA PAKAI\n" +
            "1. Tab Aturan > \"+ Aturan baru\" (atau \"+ Kerangka per Bentuk\" untuk 1 aturan tiap bentuk dasar), lalu seret prefab / FBX monstermu ke \"+ tambah model\".\n" +
            "2. Isi \"Id monster khusus\" untuk satu jenis monster saja (mis. alpha_wolf), atau \"Bentuk dasar\" untuk semua monster sebentuk (mis. wolf = semua serigala), atau pilih Boss / bukan boss.\n" +
            "   Aturan paling spesifik menang: id > bentuk > boss/biasa > scene. Aturan yang belum punya prefab diabaikan (monster tetap memakai model bawaan).\n" +
            "3. Ukuran: model otomatis dicocokkan ke tinggi model bawaan monster itu. \"Skala tinggi\" 1.2 = 20% lebih tinggi. \"Putar\" 180 bila modelmu menghadap ke belakang.\n" +
            "4. Aksi: pilih Animator Controller, atau isi klip lalu \"Buat Controller dari klip ini\". Parameter yang dikirim game:\n" +
            "   Speed (float, m/dtk)   Telegraph (bool, selama ancang-ancang serangan)   Attack (trigger, awal ancang-ancang)   Hurt (trigger, saat terkena)   Dead (bool) atau Die (trigger, saat mati).\n" +
            "   Parameter yang tidak ada dilewati tanpa error. Model tanpa Animator tetap hidup lewat gerak prosedural (naik-turun, miring, menerjang).\n" +
            "5. Tab Daftar Monster menampilkan semua monster beserta aturan yang mengenainya; tombol \"Buat aturan\" membuat aturan ber-id untuk monster itu.\n\n" +
            "Prefab: collider, rigidbody, dan skrip bawaannya dimatikan (monster digerakkan game). Warna kilat kena-pukul / ancang-ancang memakai MaterialPropertyBlock\n" +
            "pada shader yang punya _BaseColor atau _Color; shader lain mendapat denyut ukuran kecil. Bahan milik prefab tidak diubah.\n" +
            "Model aslinya hanya diganti saat game berjalan; scene tidak diubah. Non-aktifkan atau hapus aturan untuk kembali ke model bawaan.\n" +
            "Pengaturan ikut dicadangkan otomatis ke folder HG_UserData (di luar Assets), jadi aman kalau folder Assets ditimpa versi baru.", MessageType.Info);
    }
}
