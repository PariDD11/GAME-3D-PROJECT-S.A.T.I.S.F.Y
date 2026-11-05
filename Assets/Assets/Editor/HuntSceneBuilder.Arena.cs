using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HunterGuild;

/// <summary>Menu: Hunter Guild > Build Arena Ujian. Arena melingkar (radius 24 m) bertribun untuk ujian kenaikan rank. Disimpan ke Assets/Scenes/ArenaExam.unity (scene baru, tidak menimpa scene lain).</summary>
public static partial class HuntSceneBuilder
{
    const string ArenaScenePath = "Assets/Scenes/ArenaExam.unity";
    const float ArenaR = 24f;

    [MenuItem("Hunter Guild/Build Arena Ujian (ArenaExam)")]
    public static void BuildArenaExam()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ArenaScenePath) != null)
        {
            if (!HGDlg.Show("Hunter Guild", "ArenaExam.unity sudah ada.\nMembangun ulang akan MENIMPA scene itu.\n\nLanjutkan?", "Timpa", "Batal")) return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BuildArenaExamCore();
        HGDlg.Show("Hunter Guild", "Arena Ujian dibuat: " + ArenaScenePath + "\nMasuk lewat Pengawas Ujian di Arena Guild.", "OK");
    }

    /// <summary>Tanpa dialog: dipakai BUILD SEMUA SCENE.</summary>
    public static void BuildArenaExamAuto() { BuildArenaExamCore(); }

    static void BuildArenaExamCore()
    {
        foreach (var f in new[] { "Assets/Scenes", "Assets/Materials", MatDir }) EnsureFolder(f);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Random.InitState(2024);
        LoadMaterials();
        tRoot = new GameObject("ArenaExam").transform;

        var mSand = Mat("ArenaSand", new Color(0.78f, 0.67f, 0.46f));
        var mStone = Mat("ArenaStone", new Color(0.62f, 0.6f, 0.56f));
        var mDark = Mat("ArenaDark", new Color(0.32f, 0.3f, 0.3f));
        var mSeat = Mat("ArenaSeat", new Color(0.5f, 0.42f, 0.36f));
        var mRed = Mat("ArenaBannerR", new Color(0.7f, 0.12f, 0.12f));
        var mBlue = Mat("ArenaBannerB", new Color(0.15f, 0.25f, 0.65f));
        var mGold = Mat("ArenaGold", new Color(0.9f, 0.72f, 0.2f));
        var mTorch = Mat("ArenaTorch", new Color(1f, 0.6f, 0.2f));

        // lantai pasir + cincin batu
        Prim(PrimitiveType.Cylinder, "Floor", tRoot, new Vector3(0f, -0.5f, 0f), new Vector3(ArenaR * 2f + 12f, 0.5f, ArenaR * 2f + 12f), mDark);
        Prim(PrimitiveType.Cylinder, "Sand", tRoot, new Vector3(0f, 0.02f, 0f), new Vector3(ArenaR * 2f, 0.04f, ArenaR * 2f), mSand, false);
        for (int i = 0; i < 2; i++)
            Prim(PrimitiveType.Cylinder, "Ring" + i, tRoot, new Vector3(0f, 0.05f + i * 0.01f, 0f), new Vector3(ArenaR * (i == 0 ? 0.9f : 0.35f), 0.02f, ArenaR * (i == 0 ? 0.9f : 0.35f)), i == 0 ? mStone : mGold, false);
        // garis tengah bersilang (kompas)
        Prim(PrimitiveType.Cube, "LineNS", tRoot, new Vector3(0f, 0.06f, 0f), new Vector3(0.5f, 0.02f, ArenaR * 1.8f), mStone, false);
        Prim(PrimitiveType.Cube, "LineEW", tRoot, new Vector3(0f, 0.06f, 0f), new Vector3(ArenaR * 1.8f, 0.02f, 0.5f), mStone, false);

        // dinding arena (tebal, pembatas) dan tribun bertingkat
        int n = 32;
        float chord = 2f * Mathf.PI * (ArenaR + 1.2f) / n;
        var walls = new GameObject("Walls").transform; walls.SetParent(tRoot, false);
        for (int i = 0; i < n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f;
            Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            var w = Prim(PrimitiveType.Cube, "Wall" + i, walls, dir * (ArenaR + 1.2f) + Vector3.up * 2.2f, new Vector3(chord + 0.3f, 4.4f, 2.4f), mStone);
            w.transform.rotation = Quaternion.LookRotation(-dir);
            for (int r = 0; r < 4; r++)           // tribun: cincin tempat duduk makin tinggi ke luar
            {
                var s = Prim(PrimitiveType.Cube, "Seat" + i + "_" + r, walls, dir * (ArenaR + 3.4f + r * 2.2f) + Vector3.up * (4.6f + r * 1.5f), new Vector3(chord * (1f + r * 0.09f), 0.8f, 2.0f), r % 2 == 0 ? mSeat : mDark, false);
                s.transform.rotation = Quaternion.LookRotation(-dir);
            }
            if (i % 4 == 0)                        // pilar + spanduk + obor
            {
                float a2 = (i + 0.5f) / n * Mathf.PI * 2f;
                Vector3 d2 = new Vector3(Mathf.Cos(a2), 0f, Mathf.Sin(a2));
                Prim(PrimitiveType.Cylinder, "Pillar" + i, walls, d2 * (ArenaR + 0.4f) + Vector3.up * 4.5f, new Vector3(1.6f, 4.5f, 1.6f), mStone);
                var ban = Prim(PrimitiveType.Cube, "Banner" + i, walls, d2 * (ArenaR - 0.5f) + Vector3.up * 6.2f, new Vector3(1.6f, 3f, 0.08f), (i / 4) % 2 == 0 ? mRed : mBlue, false);
                ban.transform.rotation = Quaternion.LookRotation(-d2);
                Vector3 tp = d2 * (ArenaR - 0.6f) + Vector3.up * 4.2f;
                Prim(PrimitiveType.Cube, "Brazier" + i, walls, tp, new Vector3(0.6f, 0.5f, 0.6f), mTorch, false);
                var lg = new GameObject("TorchLight" + i).AddComponent<Light>();
                lg.transform.SetParent(walls, false);
                lg.transform.position = tp + Vector3.up * 0.8f;
                lg.type = LightType.Point; lg.range = 14f; lg.intensity = 1.1f; lg.color = new Color(1f, 0.65f, 0.3f);
                lg.gameObject.AddComponent<FlickerLight>();
            }
        }
        // gerbang masuk lawan (utara) dan pemain (selatan): lengkung batu
        foreach (float sz in new[] { -1f, 1f })
        {
            Vector3 gp = new Vector3(0f, 0f, sz * (ArenaR + 1.2f));
            Prim(PrimitiveType.Cube, "GateL" + sz, tRoot, gp + new Vector3(-3.6f, 3f, 0f), new Vector3(1.6f, 6f, 2.6f), mDark, false);
            Prim(PrimitiveType.Cube, "GateR" + sz, tRoot, gp + new Vector3(3.6f, 3f, 0f), new Vector3(1.6f, 6f, 2.6f), mDark, false);
            Prim(PrimitiveType.Cube, "GateTop" + sz, tRoot, gp + new Vector3(0f, 6.4f, 0f), new Vector3(9f, 1.6f, 2.6f), mGold, false);
        }
        // papan nama di tribun utara
        var sign = Prim(PrimitiveType.Cube, "Plaque", tRoot, new Vector3(0f, 8.4f, ArenaR + 3f), new Vector3(10f, 2.2f, 0.3f), mDark, false);
        BuildKit.Text(tRoot, "ARENA UJIAN GUILD", new Vector3(0f, 8.4f, ArenaR + 2.8f), 180f, 0.2f, new Color(1f, 0.88f, 0.5f));

        try { ArenaVisual(tRoot); } catch (System.Exception e) { Debug.LogWarning("[Arena] visual dilewati: " + e.Message); }

        var sp = new GameObject("SpawnFromTown").transform;
        sp.SetParent(tRoot, false);
        sp.position = new Vector3(0f, 0f, -ArenaR + 5f);
        sp.rotation = Quaternion.identity;
        var rsp = new GameObject("RespawnPoint").transform;
        rsp.SetParent(tRoot, false);
        rsp.position = sp.position;
        spawnPos = sp.position;

        Zones = new[] { new ZoneDef { name = "Arena Ujian", x = 0, z = 0, r = 60, density = 0f, tree = 0 } };
        townSpawnName = "SpawnFromDungeon";
        BuildPlayerAndManagers();
        var pl = GameObject.Find("Player");
        if (pl != null) pl.transform.rotation = Quaternion.identity;
#if UNITY_2023_1_OR_NEWER
        var hm = Object.FindFirstObjectByType<HuntManager>();
#else
        var hm = Object.FindObjectOfType<HuntManager>();
#endif
        if (hm != null) { hm.defaultZone = "Arena Ujian"; hm.returnScene = GuildConfig.TownScene; hm.townSpawn = "SpawnFromDungeon"; }
        var dn = GameObject.Find("DayNight");
        if (dn != null) Object.DestroyImmediate(dn);

        var emGo = new GameObject("ExamManager");
        var em = emGo.AddComponent<ExamManager>();
        var center = new GameObject("ArenaCenter").transform;
        center.SetParent(emGo.transform, false);
        center.position = Vector3.zero;
        em.arenaCenter = center; em.spawnPoint = sp; em.arenaRadius = ArenaR;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.004f;
        RenderSettings.fogColor = new Color(0.72f, 0.78f, 0.86f);
        var cam = Camera.main;
        if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.6f, 0.74f, 0.9f); }

        AssetDatabase.SaveAssets();
        HeightBake.BackupScene(ArenaScenePath);
        EditorSceneManager.SaveScene(scene, ArenaScenePath);
        AddToBuildSettings("Assets/Scenes/Town.unity", true);
        AddToBuildSettings(ArenaScenePath, false);
    }
}
