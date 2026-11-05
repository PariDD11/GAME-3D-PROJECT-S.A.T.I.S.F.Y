using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Menu: Hunter Guild > Build Dungeon Tower (Menara Eldrath).
/// Satu arena melingkar (radius 30 m) yang dipakai ulang untuk 100 lantai; isi tiap lantai di-spawn oleh TowerManager saat permainan berjalan.
/// Disimpan ke Assets/Scenes/DungeonTower.unity.
/// </summary>
public static partial class HuntSceneBuilder
{
    const string TowerScenePath = "Assets/Scenes/DungeonTower.unity";
    const float TowerR = 30f;

    [MenuItem("Hunter Guild/Build Dungeon Tower (Menara Eldrath)")]
    public static void BuildTower()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TowerScenePath) != null)
        {
            if (!HGDlg.Show("Hunter Guild", "DungeonTower.unity sudah ada.\nMembangun ulang akan MENIMPA scene itu.\n\nLanjutkan?", "Timpa", "Batal")) return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        foreach (var f in new[] { "Assets/Scenes", "Assets/Materials", MatDir }) EnsureFolder(f);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Random.InitState(1001);
        LoadMaterials();
        tRoot = new GameObject("DungeonTower").transform;

        var mFloor = Mat("TowerFloor", new Color(0.3f, 0.3f, 0.33f));
        var mWall = Mat("TowerWall", new Color(0.36f, 0.35f, 0.38f));
        var mPillar = Mat("TowerPillar", new Color(0.46f, 0.45f, 0.48f));
        var mDais = Mat("TowerDais", new Color(0.55f, 0.5f, 0.35f));
        var mTorch = Mat("TowerTorch", new Color(1f, 0.6f, 0.2f));

        // lantai
        var floor = Prim(PrimitiveType.Cylinder, "Floor", tRoot, new Vector3(0f, -0.5f, 0f), new Vector3(TowerR * 2f + 4f, 0.5f, TowerR * 2f + 4f), mFloor);
        { var cc = floor.GetComponent<Collider>(); if (cc != null) Object.DestroyImmediate(cc); var mc = floor.AddComponent<MeshCollider>(); mc.sharedMesh = floor.GetComponent<MeshFilter>().sharedMesh; }   // CapsuleCollider bawaan silinder = bola raksasa -> pemain jatuh
        // lingkaran dekoratif di lantai
        for (int i = 0; i < 3; i++)
            Prim(PrimitiveType.Cylinder, "FloorRing" + i, tRoot, new Vector3(0f, 0.02f + i * 0.01f, 0f), new Vector3(10f + i * 9f, 0.02f, 10f + i * 9f), i % 2 == 0 ? mDais : mFloor, false);

        // dinding melingkar + pilar
        int n = 28;
        float chord = 2f * Mathf.PI * (TowerR + 1.5f) / n;
        var walls = new GameObject("Walls").transform;
        walls.SetParent(tRoot, false);
        for (int i = 0; i < n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f;
            Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            var w = Prim(PrimitiveType.Cube, "Wall" + i, walls, dir * (TowerR + 1.5f) + Vector3.up * 8f, new Vector3(chord + 0.4f, 16f, 3f), mWall);
            w.transform.rotation = Quaternion.LookRotation(-dir);
            // pilar tiap sambungan
            float a2 = (i + 0.5f) / n * Mathf.PI * 2f;
            Vector3 d2 = new Vector3(Mathf.Cos(a2), 0f, Mathf.Sin(a2));
            Prim(PrimitiveType.Cylinder, "Pillar" + i, walls, d2 * (TowerR - 0.4f) + Vector3.up * 8f, new Vector3(2.2f, 8f, 2.2f), mPillar);
            if (i % 2 == 0)
            {
                Vector3 tp = d2 * (TowerR - 1.7f) + Vector3.up * 5f;
                Prim(PrimitiveType.Cube, "Brazier" + i, walls, tp, new Vector3(0.6f, 0.5f, 0.6f), mTorch, false);
                var lg = new GameObject("TorchLight" + i).AddComponent<Light>();
                lg.transform.SetParent(walls, false);
                lg.transform.position = tp + Vector3.up * 0.8f;
                lg.type = LightType.Point; lg.range = 16f; lg.intensity = 1.3f; lg.color = new Color(1f, 0.62f, 0.3f);
                lg.gameObject.AddComponent<FlickerLight>();
            }
        }
        // langit-langit gelap (tanpa collider)
        var ceiling = Prim(PrimitiveType.Cylinder, "Ceiling", tRoot, new Vector3(0f, 17f, 0f), new Vector3(TowerR * 2f + 12f, 0.5f, TowerR * 2f + 12f), Mat("TowerCeiling", new Color(0.08f, 0.08f, 0.1f)), false);
        // tapak naik di utara
        Prim(PrimitiveType.Cylinder, "UpDais", tRoot, new Vector3(0f, 0.05f, TowerR - 5f), new Vector3(8f, 0.1f, 8f), mDais, false);

        try { TowerVisual(tRoot); } catch (System.Exception e) { Debug.LogWarning("[Tower] visual dilewati: " + e.Message); }

        // titik spawn dan respawn (selatan, menghadap utara)
        var sp = new GameObject("SpawnFromTown").transform;
        sp.SetParent(tRoot, false);
        sp.position = new Vector3(0f, 0f, -TowerR + 5f);
        sp.rotation = Quaternion.identity;
        var rsp = new GameObject("RespawnPoint").transform;
        rsp.SetParent(tRoot, false);
        rsp.position = sp.position;
        spawnPos = sp.position;

        Zones = new[] { new ZoneDef { name = "Menara Eldrath", x = 0, z = 0, r = 60, density = 0f, tree = 0 } };
        townSpawnName = "SpawnFromTower";
        BuildPlayerAndManagers();

        var pl = GameObject.Find("Player");
        if (pl != null) pl.transform.rotation = Quaternion.identity;
#if UNITY_2023_1_OR_NEWER
        var hm = Object.FindFirstObjectByType<HuntManager>();
#else
        var hm = Object.FindObjectOfType<HuntManager>();
#endif
        if (hm != null) { hm.defaultZone = "Menara Eldrath"; hm.returnScene = "DungeonCity"; hm.townSpawn = "SpawnFromTower"; }
        var dn = GameObject.Find("DayNight");
        if (dn != null) Object.DestroyImmediate(dn);

        var tmGo = new GameObject("TowerManager");
        var tm = tmGo.AddComponent<TowerManager>();
        var center = new GameObject("ArenaCenter").transform;
        center.SetParent(tmGo.transform, false);
        center.position = Vector3.zero;
        tm.arenaCenter = center;
        tm.spawnPoint = sp;
        tm.arenaRadius = TowerR;
        tm.floorRenderer = floor.GetComponent<Renderer>();
        // n89: Lantai Seratus = aula pualam terbuka di bawah langit bintang (dibangun nonaktif; TowerManager menyalakannya hanya di lantai terakhir)
        try { TowerHall100(tRoot, tm, walls.gameObject, ceiling); } catch (System.Exception e) { Debug.LogWarning("[Menara n89] aula Lantai Seratus dilewati: " + e.Message); }

        // suasana
        var sun = GameObject.Find("Directional Light");
        if (sun != null) { var l = sun.GetComponent<Light>(); l.intensity = 0.15f; l.color = new Color(0.6f, 0.65f, 0.9f); l.shadows = LightShadows.None; }
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.22f, 0.22f, 0.26f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.012f;
        RenderSettings.fogColor = new Color(0.2f, 0.2f, 0.24f);
        var cam = Camera.main;
        if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.04f, 0.04f, 0.06f); }

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene, TowerScenePath);
        AddToBuildSettings("Assets/Scenes/Town.unity", true);
        AddToBuildSettings("Assets/Scenes/DungeonCity.unity", true);
        AddToBuildSettings(TowerScenePath, false);
        HGDlg.Show("Hunter Guild", "Menara Eldrath dibuat: " + TowerScenePath + "\nMasuk lewat pintu Menara di Kota Eldrath.", "OK");
    }
}
