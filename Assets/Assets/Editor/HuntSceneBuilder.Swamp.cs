using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Menu: Hunter Guild > Build Hunt Area (Rawa Terkutuk)
/// Rawa berkabut 1000 x 1000 m di barat kota (Gerbang Luar Barat): kolam dan lumpur, desa nelayan Kelabu (hidup, rumah panggung di tepi danau),
/// desa hantu Merana (kapel, kuburan), gubuk penyihir, lingkar ritual, menara nekromancer, monster undead, kabut dan cahaya hantu.
/// Disimpan ke Assets/Scenes/HuntArea_Swamp.unity. Memakai fungsi bersama dengan Hutan Lerai dan Gurun Zahar.
/// </summary>
public static partial class HuntSceneBuilder
{
    const string SwampScenePath = "Assets/Scenes/HuntArea_Swamp.unity";
    const string SPrefabDir = "Assets/Prefabs/Swamp";

    static readonly ZoneDef[] SwampZones =
    {
        new ZoneDef { name = "Pos Rawa",                    x = 0,    z = -370, r = 55,  density = 0.0f },
        new ZoneDef { name = "Gerbang Rawa",                x = 0,    z = -300, r = 90,  density = 0.35f },
        new ZoneDef { name = "Desa Nelayan Kelabu",         x = -125, z = -100, r = 60,  density = 0.15f },
        new ZoneDef { name = "Teluk Nelayan",               x = -205, z = -100, r = 65,  density = 0.2f },
        new ZoneDef { name = "Hutan Kabut",                 x = 20,   z = -110, r = 120, density = 1.0f },
        new ZoneDef { name = "Desa Merana (Hantu)",         x = 205,  z = -70,  r = 60,  density = 0.2f },
        new ZoneDef { name = "Rawa Hitam",                  x = 150,  z = 110,  r = 100, density = 0.6f },
        new ZoneDef { name = "Lahan Gambut",                x = -290, z = -260, r = 115, density = 0.7f },
        new ZoneDef { name = "Gubuk Penyihir",              x = -250, z = 170,  r = 75,  density = 0.5f },
        new ZoneDef { name = "Lingkar Ritual",              x = 30,   z = 200,  r = 65,  density = 0.35f },
        new ZoneDef { name = "Menara Maelis",               x = 130,  z = 330,  r = 100, density = 0.3f },
        new ZoneDef { name = "Hutan Mati",                  x = -80,  z = 320,  r = 110, density = 0.8f },
    };

    // koordinat desain
    static readonly Vector2 FishC = new Vector2(-120, -100);
    static readonly Vector2 GhostC = new Vector2(205, -70);
    static readonly Vector2 WitchC = new Vector2(-250, 170);
    static readonly Vector2 RitualC = new Vector2(30, 200);
    static readonly Vector2 NecroC = new Vector2(130, 330);
    static readonly Vector2 TrollDen = new Vector2(-290, -260);
    static readonly Vector3[] SwLakes = { new Vector3(-200, -100, 58f), new Vector3(150, 110, 66f) };
    static readonly Vector3[] SwSites =
    {
        new Vector3(0, -370, 60f), new Vector3(-120, -100, 55f), new Vector3(205, -70, 58f), new Vector3(-250, 170, 50f),
        new Vector3(30, 200, 42f), new Vector3(130, 330, 68f), new Vector3(0, -440, 40f), new Vector3(-290, -260, 24f),
    };

    static Material mFxMist;

    [MenuItem("Hunter Guild/Build Hunt Area (Rawa Terkutuk)")]
    public static void BuildSwamp()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SwampScenePath) != null)
        {
            if (!HGDlg.Show("Hunter Guild",
                "HuntArea_Swamp.unity sudah ada.\nMembangun ulang akan MENIMPA scene itu.\n\nLanjutkan?", "Timpa", "Batal")) return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        foreach (var f in new[] { "Assets/Scenes", "Assets/Materials", MatDir, "Assets/Meshes", MeshDir, "Assets/Prefabs", SPrefabDir, TerrDir, "Assets/Textures", TexDir })
            EnsureFolder(f);

        EditorUtility.DisplayProgressBar("Hunter Guild", "Menyiapkan scene rawa...", 0.05f);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Random.InitState(6613);
        Zones = SwampZones;
        townSpawnName = "SpawnFromSwamp";
        clearings.Clear();
        plan.Clear();
        LoadMaterials();
        NpcKit.Reset();
        ForestKit.Reset();
        ForestKit.ResetSwamp();
        ForestKit.LoadSwamp();
        DefineSwampTrails();
        PlanSwamp();

        tRoot = new GameObject("HuntAreaSwamp").transform;
        tCamp = Group("BaseCamp");
        tNodes = Group("GatherNodes");
        tRocks = Group("Rocks");
        tProps = Group("Props");

        clearings.Add(new Vector3(Camp.x, Camp.y, 42f));
        clearings.Add(new Vector3(TrollDen.x, TrollDen.y, 16f));

        EditorUtility.DisplayProgressBar("Hunter Guild", "Membuat rawa, danau, dan hutan kabut...", 0.15f);
        BuildSwampTerrain();

        EditorUtility.DisplayProgressBar("Hunter Guild", "Menata desa nelayan, desa hantu, dan menara...", 0.6f);
        BuildWaterAndBounds();
        var wt = tRoot.Find("Water");
        if (wt != null) wt.GetComponent<Renderer>().sharedMaterial = Mat("SwampWater", new Color(0.06f, 0.14f, 0.11f));
        BuildSwampCamp();
        BuildTownApproach();
        BuildSwampSites();
        BuildSwampFx();
        ScatterSwamp();
        BuildSwampMonsters();
        BuildSwampRocks();
        SwampCanon();   // kanon §8.5: penidur Merana, 1.400 lilin, bendungan jebol Kelabu, perahu rumbia
        BuildPlayerAndManagers();
        var hm = Object.FindFirstObjectByType<HuntManager>();
        if (hm != null) hm.defaultZone = "Rawa Terkutuk";

        MarkStatic(tRocks);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0075f;
        RenderSettings.fogColor = new Color(0.30f, 0.38f, 0.35f);
        RenderSettings.ambientLight = new Color(0.20f, 0.26f, 0.24f);
        var cam = Camera.main;
        if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = RenderSettings.fogColor; }
        var sun = GameObject.Find("Directional Light");
        if (sun != null) { var l = sun.GetComponent<Light>(); if (l != null) l.color = new Color(0.72f, 0.86f, 0.8f); }

        EditorUtility.DisplayProgressBar("Hunter Guild", "Menyimpan scene...", 0.95f);
        AssetDatabase.SaveAssets();
        HeightBake.BackupScene(SwampScenePath); EditorSceneManager.SaveScene(scene, SwampScenePath);
        AddToBuildSettings("Assets/Scenes/Town.unity", true);
        AddToBuildSettings(SwampScenePath, false);
        EditorUtility.ClearProgressBar();
        HGDlg.Show("Hunter Guild", "Rawa Terkutuk dibuat: " + SwampScenePath + "\nBangun ulang scene Town (Build Town Scene) supaya Gerbang Luar Barat aktif.", "OK");
    }

    // =====================================================================
    // Jalan setapak
    // =====================================================================
    static void DefineSwampTrails()
    {
        trails = new List<Vector2[]>
        {
            new[] { V(0, -470), V(0, -380), V(0, -330), V(-10, -260), V(15, -190), V(40, -110), V(42, -30), V(38, 60), V(30, 130), V(30, 200), V(60, 260), V(110, 310), V(130, 330) },   // tulang punggung ke menara
            new[] { V(-10, -260), V(-60, -190), V(-100, -135), V(-120, -100) },                                                          // ke Desa Nelayan
            new[] { V(40, -110), V(100, -95), V(160, -80), V(205, -70) },                                                                // ke Desa Merana
            new[] { V(38, 60), V(-40, 70), V(-130, 110), V(-210, 150), V(-250, 170) },                                                   // ke Gubuk Penyihir
            new[] { V(-120, -100), V(-170, -170), V(-240, -230), V(-290, -260) },                                                        // ke sarang troll
        };
    }

    // =====================================================================
    // Terrain
    // =====================================================================
    static float SiteFree(float x, float z)
    {
        float best = 9999f;
        foreach (var s in SwSites) best = Mathf.Min(best, Dist(x, z, s.x, s.y) - s.z);
        return Smooth(best / 16f);
    }

    static float SHeight(float x, float z)
    {
        float wx = x + (Noise(x, z, 0.004f, 3f) - 0.5f) * 100f;
        float wz = z + (Noise(x, z, 0.004f, 8f) - 0.5f) * 100f;
        float h = 0.216f + (Noise(wx, wz, 0.006f, 11f) - 0.5f) * 0.012f + (Noise(x, z, 0.03f, 5f) - 0.5f) * 0.006f + (Noise(x, z, 0.12f, 73f) - 0.5f) * 0.002f;
        h += Smooth((Noise(x, z, 0.008f, 21f) - 0.62f) / 0.15f) * 0.03f;             // gundukan tanah

        // kolam dan genangan
        float pool = Smooth((Noise(wx, wz, 0.011f, 5f) - 0.56f) / 0.07f);
        pool = Mathf.Max(pool, Smooth((Noise(wx, wz, 0.035f, 9f) - 0.68f) / 0.05f) * 0.85f);
        pool *= Smooth(DistToTrails(x, z) / 7f - 0.4f);
        pool *= SiteFree(x, z);
        h = Mathf.Lerp(h, 0.178f, pool);

        // dataran situs
        h = Mathf.Lerp(h, 0.222f, Smooth(1f - Dist(x, z, Camp.x, Camp.y) / 52f));
        h = Plateau(h, x, z, FishC, 34f, 62f, 0.224f);
        h = Plateau(h, x, z, GhostC, 38f, 66f, 0.224f);
        h = Plateau(h, x, z, WitchC, 30f, 58f, 0.224f);
        h = Plateau(h, x, z, RitualC, 24f, 50f, 0.224f);
        h = Plateau(h, x, z, NecroC, 44f, 84f, 0.226f);
        h = Plateau(h, x, z, TrollDen, 12f, 30f, 0.222f);

        // danau
        foreach (var lk in SwLakes)
        {
            float rr = lk.z * (1f + (Noise(x, z, 0.02f, 8f + lk.x) - 0.5f) * 0.35f);
            float dd = Dist(x, z, lk.x, lk.y);
            float k = dd < rr ? 1f : Smooth(1f - (dd - rr) / 18f);
            h = Mathf.Lerp(h, 0.162f - Smooth(1f - dd / (rr * 0.6f)) * 0.01f, k);
        }
        // pulau kecil di Rawa Hitam
        h = Mathf.Lerp(h, 0.228f, Smooth(1f - Dist(x, z, 160f, 120f) / 11f));

        float edge = Smooth((Mathf.Max(Mathf.Abs(x), z > 0f ? z : 0f) - 440f) / 50f);
        h += edge * 0.7f;
        h = Mathf.Lerp(h, 0.222f, Smooth((-380f - z) / 40f));
        return Mathf.Clamp01(h);
    }

    static void BuildSwampTerrain()
    {
        td = new TerrainData();
        td.heightmapResolution = HRes;
        td.size = new Vector3(TSize, TH, TSize);
        HeightBake.NewAsset(td, TerrDir + "/SwampTerrain.asset");

        var hs = new float[HRes, HRes];
        float step = TSize / (HRes - 1);
        for (int zi = 0; zi < HRes; zi++)
            for (int xi = 0; xi < HRes; xi++)
                hs[zi, xi] = SHeight(M * (Origin + xi * step), M * (Origin + zi * step));
        FlattenPlan(hs, step);
        td.SetHeights(0, 0, hs);

        td.terrainLayers = MakeSwampLayers();
        td.alphamapResolution = 512;
        PaintSwampAlpha();
        PaintSwampDetail();

        treePrefabs = MakeSwampTreePrefabs();
        PlaceSwampTrees();

        var go = Terrain.CreateTerrainGameObject(td);
        go.name = "SwampTerrain";
        go.transform.SetParent(tRoot, false);
        go.transform.position = new Vector3(Origin, 0, Origin);
        terrain = go.GetComponent<Terrain>();
        HeightBake.Attach(go, td, hs, "SwampTerrain");
        terrain.treeDistance = 340f;
        terrain.treeBillboardDistance = 300f;
        terrain.treeMaximumFullLODCount = 320;
        terrain.detailObjectDistance = 60f;
        terrain.detailObjectDensity = 0.8f;
        terrain.heightmapPixelError = 6f;
        terrain.drawInstanced = true;
        EditorUtility.SetDirty(td);
    }

    static TerrainLayer[] MakeSwampLayers()
    {
        string[] names = { "SGrass", "SMoss", "SPeat", "SMud", "SRock", "SPath", "SAsh" };
        Texture2D[] tex = { ForestKit.sGrassT, ForestKit.sMossT, ForestKit.sPeatT, ForestKit.sMudT, ForestKit.sRockT, ForestKit.sPathT, ForestKit.sAshT };
        float[] tile = { 7f, 6f, 8f, 6f, 10f, 5f, 7f };
        var layers = new TerrainLayer[names.Length];
        for (int i = 0; i < layers.Length; i++)
        {
            var layer = new TerrainLayer();
            layer.diffuseTexture = tex[i];
            layer.tileSize = new Vector2(tile[i], tile[i]);
            layer.smoothness = i == 3 ? 0.18f : 0.04f;
            AssetDatabase.CreateAsset(layer, TerrDir + "/Layer_" + names[i] + ".terrainlayer");
            layers[i] = layer;
        }
        return layers;
    }

    static float HauntInfluence(float x, float z)
    {
        float n = 0.65f + 0.35f * Noise(x, z, 0.05f, 3f);
        float v = Smooth(1f - Dist(x, z, GhostC.x, GhostC.y) / 56f) * 0.95f;
        v = Mathf.Max(v, Smooth(1f - Dist(x, z, NecroC.x, NecroC.y) / 72f) * 0.95f);
        v = Mathf.Max(v, Smooth(1f - Dist(x, z, RitualC.x, RitualC.y) / 28f));
        v = Mathf.Max(v, Smooth(1f - Dist(x, z, WitchC.x, WitchC.y) / 30f) * 0.6f);
        return v * n;
    }

    static void PaintSwampAlpha()
    {
        int res = td.alphamapResolution;
        const int L = 7;
        var map = new float[res, res, L];
        for (int zi = 0; zi < res; zi++)
            for (int xi = 0; xi < res; xi++)
            {
                float nx = xi / (res - 1f), nz = zi / (res - 1f);
                float wx = M * (Origin + nx * TSize), wz = M * (Origin + nz * TSize);
                float hM = td.GetInterpolatedHeight(nx, nz);
                float steep = td.GetSteepness(nx, nz);

                float rock = Smooth((steep - 24f) / 12f);
                float path = Smooth(1f - DistToTrails(wx, wz) / 3.2f);
                path = Mathf.Max(path, Smooth(1f - DistSeg(wx, wz, 0f, -470f, 0f, -384f) / 9f));
                foreach (var c in clearings) path = Mathf.Max(path, Smooth(1f - Dist(wx, wz, c.x, c.y) / Mathf.Min(c.z, 14f)) * (c.z > 20f ? 0.8f : 0.5f));
                path = Mathf.Max(path, Smooth(1f - Dist(wx, wz, FishC.x, FishC.y) / 12f));
                float mud = Smooth(1f - Mathf.Abs(hM - WaterY) / 3f) * 0.95f;
                if (hM < WaterY) mud = 1f;
                float ash = HauntInfluence(wx, wz);
                float peat = Smooth((Noise(wx, wz, 0.012f, 4f) - 0.5f) / 0.15f) * 0.9f;
                peat = Mathf.Max(peat, Smooth(1f - Dist(wx, wz, TrollDen.x, TrollDen.y) / 130f) * 0.8f);
                float moss = Smooth((Noise(wx, wz, 0.02f, 7f) - 0.5f) / 0.2f) * 0.85f;

                float rem = 1f;
                float wPath = Mathf.Min(rem, path); rem -= wPath;
                float wAsh = Mathf.Min(rem, ash); rem -= wAsh;
                float wRock = Mathf.Min(rem, rock); rem -= wRock;
                float wMud = Mathf.Min(rem, mud); rem -= wMud;
                float wPeat = rem * peat; rem -= wPeat;
                float wMoss = rem * moss; rem -= wMoss;
                map[zi, xi, 0] = rem;
                map[zi, xi, 1] = wMoss;
                map[zi, xi, 2] = wPeat;
                map[zi, xi, 3] = wMud;
                map[zi, xi, 4] = wRock;
                map[zi, xi, 5] = wPath;
                map[zi, xi, 6] = wAsh;
            }
        alphaMap = map;
        td.SetAlphamaps(0, 0, map);
    }

    static void PaintSwampDetail()
    {
        td.detailPrototypes = new[]
        {
            new DetailPrototype
            {
                usePrototypeMesh = false, prototypeTexture = ForestKit.TexBogTuft(), renderMode = DetailRenderMode.GrassBillboard,
                minWidth = 0.9f, maxWidth = 1.7f, minHeight = 0.6f, maxHeight = 1.3f, noiseSpread = 0.4f,
                healthyColor = new Color(0.6f, 0.85f, 0.55f), dryColor = new Color(0.5f, 0.6f, 0.32f)
            },
            new DetailPrototype
            {
                usePrototypeMesh = false, prototypeTexture = ForestKit.TexGrassTuft(), renderMode = DetailRenderMode.GrassBillboard,
                minWidth = 0.7f, maxWidth = 1.3f, minHeight = 0.4f, maxHeight = 0.9f, noiseSpread = 0.3f,
                healthyColor = new Color(0.35f, 0.5f, 0.3f), dryColor = new Color(0.35f, 0.35f, 0.22f)
            }
        };
        int dr = 640;
        td.SetDetailResolution(dr, 16);
        var tall = new int[dr, dr];
        var low = new int[dr, dr];
        int ar = td.alphamapResolution;
        var rnd = new System.Random(7171);
        for (int z = 0; z < dr; z++)
            for (int x = 0; x < dr; x++)
            {
                int ax = Mathf.Clamp(Mathf.RoundToInt(x / (dr - 1f) * (ar - 1)), 0, ar - 1);
                int az = Mathf.Clamp(Mathf.RoundToInt(z / (dr - 1f) * (ar - 1)), 0, ar - 1);
                float gTall = alphaMap[az, ax, 0] * 0.55f + alphaMap[az, ax, 1] * 0.65f + alphaMap[az, ax, 2] * 0.35f;
                float gLow = alphaMap[az, ax, 6] * 0.3f + alphaMap[az, ax, 3] * 0.15f;
                if (rnd.NextDouble() < gTall * 0.42) tall[z, x] = 1;
                else if (rnd.NextDouble() < gLow * 0.3) low[z, x] = 1;
            }
        td.SetDetailLayer(0, 0, 0, tall);
        td.SetDetailLayer(0, 0, 1, low);
    }

    // ---------- Pohon ----------
    // 0-1 bakau, 2-3 willow, 4-5 sipres rawa, 6-7 pohon mati, 8 semak, 9 pakis, 10 ilalang, 11 jamur menyala
    static GameObject[] MakeSwampTreePrefabs()
    {
        return new[]
        {
            STreePrefab("SMangrove_A", b => ForestKit.Mangrove(b, 3, 1f), 4f, 0.5f),
            STreePrefab("SMangrove_B", b => ForestKit.Mangrove(b, 8, 1.2f), 4.5f, 0.55f),
            STreePrefab("SWillow_A", b => ForestKit.Willow(b, 5, 1f), 5f, 0.6f),
            STreePrefab("SWillow_B", b => ForestKit.Willow(b, 11, 1.25f), 6f, 0.7f),
            STreePrefab("SCypress_A", b => ForestKit.BaldCypress(b, 2, 1f), 9f, 0.6f),
            STreePrefab("SCypress_B", b => ForestKit.BaldCypress(b, 7, 1.15f), 10f, 0.65f),
            STreePrefab("SDead_A", b => ForestKit.SwampDeadTree(b, 4, 1f), 4f, 0.4f),
            STreePrefab("SDead_B", b => ForestKit.SwampDeadTree(b, 9, 1.3f), 5f, 0.45f),
            STreePrefab("SBush", b => ForestKit.Bush(b, 6, ForestKit.LeafGreen, 1.1f), 0f, 0f),
            STreePrefab("SFern", b => ForestKit.Fern(b, 5), 0f, 0f),
            STreePrefab("SCattail", b => ForestKit.Cattail(b, 4), 0f, 0f),
            STreePrefab("SGlowShroom", b => ForestKit.GlowMushrooms(b, 3), 0f, 0f),
        };
    }

    static GameObject STreePrefab(string name, System.Action<GeoBag> build, float colliderHeight, float colliderRadius)
    {
        var proto = PropKit.Get("Swamp_" + name, build);
        var go = new GameObject(name);
        go.AddComponent<MeshFilter>().sharedMesh = proto.mesh;
        var mats = new Material[proto.used.Length];
        for (int i = 0; i < mats.Length; i++) mats[i] = ForestKit.SSlots[proto.used[i]];
        go.AddComponent<MeshRenderer>().sharedMaterials = mats;
        if (colliderHeight > 0f)
        {
            var cap = go.AddComponent<CapsuleCollider>();
            cap.radius = colliderRadius; cap.height = colliderHeight; cap.center = new Vector3(0, colliderHeight / 2f, 0);
        }
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, SPrefabDir + "/" + name + ".prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    static void PlaceSwampTrees()
    {
        var protos = new TreePrototype[treePrefabs.Length];
        for (int i = 0; i < protos.Length; i++) protos[i] = new TreePrototype { prefab = treePrefabs[i] };
        var list = new List<TreeInstance>();

        // sipres dan pohon mati di tepi jalan dari gerbang kota
        for (float z = -462f; z <= -395f; z += 9f)
            foreach (float sx in new[] { -1f, 1f })
            {
                float dx = sx * (16f + Random.Range(-0.8f, 0.8f));
                float rx = (M * dx - Origin) / TSize, rz = (M * z - Origin) / TSize;
                list.Add(MakeTree(rx, rz, td.GetInterpolatedHeight(rx, rz), Random.value < 0.6f ? 4 : 6, 1f, Random.Range(0.95f, 1.2f)));
            }

        for (int i = 0; i < 160000 && list.Count < 3500; i++)
        {
            float wx = Random.Range(-445f, 445f), wz = Random.Range(-445f, 445f);
            float nx = (M * wx - Origin) / TSize, nz = (M * wz - Origin) / TSize;
            float hM = td.GetInterpolatedHeight(nx, nz);
            if (td.GetSteepness(nx, nz) > 32f) continue;
            if (DistToTrails(wx, wz) < 4f) continue;
            if (NearClearing(wx, wz)) continue;
            if (Mathf.Abs(wx) < 30f && wz < -380f) continue;
            if (wz < -455f) continue;

            float depth = WaterY - hM;
            var zd = ZoneAt(wx, wz);
            float dens = (zd != null ? zd.density : 0.3f) * (0.25f + 1.2f * Noise(wx, wz, 0.02f, 5f));
            int proto; float s = Random.Range(0.8f, 1.4f);
            if (depth > 0f)
            {
                if (depth > 1.1f) continue;
                if (Random.value > Mathf.Min(0.35f, dens * 0.35f)) continue;
                float r = Random.value;
                if (r < 0.4f && depth < 0.8f) proto = 10;
                else if (r < 0.75f) proto = Random.value < 0.5f ? 0 : 1;
                else proto = Random.value < 0.5f ? 4 : 5;
            }
            else
            {
                if (hM < WaterY + 0.15f && Random.value < 0.6f) { if (Random.value > 0.5f) continue; }
                if (Random.value > dens * 0.5f) continue;
                float r = Random.value;
                if (r < 0.17f) proto = Random.value < 0.5f ? 0 : 1;
                else if (r < 0.34f) proto = Random.value < 0.5f ? 2 : 3;
                else if (r < 0.56f) proto = Random.value < 0.5f ? 4 : 5;
                else if (r < 0.68f) proto = Random.value < 0.5f ? 6 : 7;
                else if (r < 0.82f) proto = 8;
                else if (r < 0.9f) proto = 9;
                else if (r < 0.96f) proto = (depth > -0.8f) ? 10 : 8;
                else proto = 11;
            }
            list.Add(MakeTree(nx, nz, hM, proto, s, s * Random.Range(0.9f, 1.25f)));
        }
        CommitTrees(list, null);
    }

    // =====================================================================
    // Camp
    // =====================================================================
    static GameObject SKit(string key, System.Action<GeoBag> build, Transform parent, Vector3 pos, float worldYaw, float scale, bool collider)
    {
        var proto = PropKit.Get(key, build);
        var go = PropKit.Place(proto, parent, key, pos, worldYaw, scale, ForestKit.SSlots);
        if (collider) go.AddComponent<MeshCollider>().sharedMesh = proto.mesh;
        return go;
    }

    static void BuildSwampCamp()
    {
        Vector3 c = G(Camp.x, Camp.y);
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            int cs = i % 2 == 0 ? Sl.Cloth : Sl.ClothB;
            Vector3 p = Gw(c.x + Mathf.Sin(a) * 12f, c.z + Mathf.Cos(a) * 12f);
            var go = SKit("STent_" + cs, b => ForestKit.Tent(b, 1, cs), tCamp, p, i * 90f + 45f, 1.3f, false);
            var bc = go.AddComponent<BoxCollider>();
            bc.center = new Vector3(0, 0.85f, 0); bc.size = new Vector3(2.0f, 1.7f, 3.0f);
        }
        var fire = new GameObject("Campfire").transform;
        fire.SetParent(tCamp, false);
        fire.position = Gw(c.x, c.z);
        SKit("SCampfire", b => ForestKit.Campfire(b, 3), fire, fire.position, 0f, 1.25f, false);
        var lg = new GameObject("FireLight").AddComponent<Light>();
        lg.transform.SetParent(fire, false);
        lg.transform.localPosition = new Vector3(0, 1.5f, 0);
        lg.type = LightType.Point; lg.range = 24f; lg.intensity = 2.0f; lg.color = new Color(1f, 0.62f, 0.3f);

        SKit("SCrate", b => ForestKit.Crate(b, 2), tCamp, Gw(c.x + 5f, c.z + 3f), 20f, 1.4f, true);
        SKit("SBarrel", b => ForestKit.Barrel(b), tCamp, Gw(c.x + 4.2f, c.z + 5.4f), 0f, 1.3f, true);
        foreach (var pp in new[] { new Vector2(-14f, 14f), new Vector2(15f, 12f), new Vector2(-20f, -4f), new Vector2(22f, -2f), new Vector2(3f, 24f) })
            SKit("SCampTree_" + (int)pp.x, b => ForestKit.SwampDeadTree(b, (int)(pp.x * 3f + 40f), 1f), tCamp, Gw(c.x + pp.x, c.z + pp.y), pp.x * 17f, 1f, false)
                .AddComponent<CapsuleCollider>().height = 4f;
        foreach (float sx in new[] { -1f, 1f })
        {
            var lamp = SKit("SCampLamp", b => ForestKit.LanternPost(b, false), tCamp, Gw(c.x + sx * 9f, c.z + 16f), 0f, 1f, false);
            AddLight(lamp.transform, new Vector3(0.5f, 2.3f, 0), new Color(1f, 0.75f, 0.4f), 13f, 1.2f, "LampLight");
        }
        var sign = new GameObject("TrailSign").transform;
        sign.SetParent(tCamp, false);
        sign.position = Gw(c.x, c.z + 22f);
        Prim(PrimitiveType.Cube, "Post", sign, new Vector3(0, 1.5f, 0), new Vector3(0.3f, 3f, 0.3f), mWood);
        Prim(PrimitiveType.Cube, "Board", sign, new Vector3(0, 2.7f, 0), new Vector3(2.6f, 0.9f, 0.15f), mDark, false);

        var ip = new GameObject("BaseCamp_Interact");
        ip.transform.SetParent(tCamp, false);
        ip.transform.position = Gw(c.x, c.z + 5.5f);
        var bcm = ip.AddComponent<BaseCamp>();
        bcm.prompt = "Pos Rawa Terkutuk";
        bcm.radius = 5f;
        Prim(PrimitiveType.Cylinder, "Marker", ip.transform, new Vector3(0, 0.05f, 0), new Vector3(2.6f, 0.03f, 2.6f), mGold, false);
        MakeSpring(Gw(c.x + 21f, c.z + 9f), "Air Hujan Tampungan");
    }

    // =====================================================================
    // Monster, gathering, batu
    // =====================================================================
    static void BuildSwampMonsters()
    {
        var root = Group("Monsters");
        SpawnMonster(root, "swamp_troll", TrollDen);
        // undead nelayan di sekitar danau dan hutan
        SpawnGroup(root, "drowned", -205, -100, 78, 7);
        SpawnGroup(root, "drowned", -230, -190, 60, 4);
        SpawnGroup(root, "drowned", 20, -110, 90, 4);
        SpawnGroup(root, "drowned", 150, 110, 90, 3);
        // desa hantu dan kapel
        SpawnRing(root, "skeleton", GhostC, 24f, 7, 10f);
        SpawnRing(root, "wraith", GhostC, 16f, 3, 50f);
        SpawnRing(root, "wraith", Pt(GhostC, 0, 40), 8f, 2, 20f);
        SpawnRing(root, "skeleton", Pt(GhostC, 90, 36), 10f, 4, 0f);
        // penyihir
        SpawnRing(root, "witch", WitchC, 14f, 3, 30f);
        SpawnRing(root, "skeleton", WitchC, 22f, 3, 60f);
        SpawnRing(root, "witch", RitualC, 7f, 3, 0f);
        SpawnRing(root, "skeleton", RitualC, 18f, 3, 45f);
        // menara nekromancer
        Vector2 gate = NecroC + (V(60f, 270f) - NecroC).normalized * 30f;
        SpawnMonster(root, "necromancer", gate);
        SpawnRing(root, "skeleton", NecroC, 38f, 8, 15f);
        SpawnRing(root, "wraith", NecroC, 28f, 3, 70f);
        SpawnGroup(root, "skeleton", -80, 320, 80, 4);
        SpawnGroup(root, "wraith", -80, 320, 60, 2);
    }

    static void ScatterSwamp()
    {
        // zona: 1 gerbang, 4 hutan kabut, 7 gambut, 8 penyihir, 11 hutan mati
        Scatter("herb", 24, new[] { 1, 4, 8, 11 });
        Scatter("mushroom", 30, new[] { 4, 11, 8, 7 });
        Scatter("wild_berry", 14, new[] { 1, 4, 7 });
        // teratai rawa tumbuh di permukaan air dangkal (danau dan genangan)
        int made = 0;
        for (int i = 0; i < 20000 && made < 36; i++)
        {
            float x = Random.Range(-430f, 430f), z = Random.Range(-430f, 400f);
            float gy = GroundY(x, z);
            if (gy > WaterY - 0.25f || gy < WaterY - 1.3f) continue;
            if (Dist(x, z, Camp.x, Camp.y) < 60f) continue;
            MakeNode("swamp_lotus", new Vector3(M * x, WaterY + 0.05f, M * z));
            made++;
        }
        Tracks("swamp_troll", TrollDen);
        Vector3? s1 = Shore(V(-330f, -250f), V(1, 0), 100f);
        if (s1.HasValue) MakeSpring(s1.Value, "Air Rawa (keruh)", false);
    }

    static void BuildSwampRocks()
    {
        int made = 0;
        for (int i = 0; i < 4000 && made < 130; i++)
        {
            float x = Random.Range(-440f, 440f), z = Random.Range(-440f, 440f);
            if (GroundY(x, z) < WaterY + 0.3f || NearClearing(x, z)) continue;
            if (DistToTrails(x, z) < 3f) continue;
            float s = Random.Range(0.7f, 1.9f);
            int v = Random.Range(0, 4);
            var go = SKit("SBoulder_" + v, b => ForestKit.Boulder(b, v, 1f, true), tRocks, G(x, z), Random.Range(0f, 360f), s, false);
            go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
            made++;
        }
        // batang tumbang, tunggul, dan jamur bercahaya
        made = 0;
        for (int i = 0; i < 4000 && made < 70; i++)
        {
            float x = Random.Range(-440f, 440f), z = Random.Range(-440f, 440f);
            if (GroundY(x, z) < WaterY + 0.3f || NearClearing(x, z) || DistToTrails(x, z) < 4f || Slope(x, z) > 20f) continue;
            int v = Random.Range(0, 3);
            if (made % 3 == 0) SKit("SLog_" + v, b => ForestKit.FallenLog(b, v), tRocks, G(x, z), Random.Range(0f, 360f), 1.2f, false);
            else if (made % 3 == 1) SKit("SStump_" + v, b => ForestKit.Stump(b, v), tRocks, G(x, z), Random.Range(0f, 360f), 1.3f, false);
            else SKit("SMush_" + v, b => ForestKit.GlowMushrooms(b, v + 5), tRocks, G(x, z), Random.Range(0f, 360f), 1.6f, false);
            made++;
        }
        // rangka dan tengkorak tersebar
        made = 0;
        for (int i = 0; i < 2000 && made < 8; i++)
        {
            float x = Random.Range(-420f, 420f), z = Random.Range(-420f, 420f);
            if (GroundY(x, z) < WaterY + 0.5f || NearClearing(x, z) || DistToTrails(x, z) < 8f || Slope(x, z) > 14f) continue;
            SKit("SBones", b => ForestKit.BeastSkeleton(b, 3), tRocks, G(x, z), Random.Range(0f, 360f), Random.Range(1f, 1.4f), false);
            made++;
        }
    }
}
