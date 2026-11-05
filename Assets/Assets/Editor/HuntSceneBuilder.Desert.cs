using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Menu: Hunter Guild > Build Hunt Area (Gurun Zahar)
/// Gurun pasir 1000 x 1000 m di timur kota (Gerbang Luar Timur): dune, ngarai merah, oasis, desa Zafir (hidup), desa Sahr (terbengkalai, sarang bandit),
/// kompleks kuil batu pasir (Lembah Kuil, Kuil Barat, Kuil Bayangan), monster gurun, event penyergapan bandit, pasir tertiup angin.
/// Disimpan ke Assets/Scenes/HuntArea_Desert.unity. Memakai fungsi bersama dengan Hutan Lerai (terrain, camp, pemain, UI).
/// </summary>
public static partial class HuntSceneBuilder
{
    const string DesertScenePath = "Assets/Scenes/HuntArea_Desert.unity";
    const string DPrefabDir = "Assets/Prefabs/Desert";

    static readonly ZoneDef[] DesertZones =
    {
        new ZoneDef { name = "Base Camp Oasis",            x = 0,    z = -370, r = 55,  density = 0.0f },
        new ZoneDef { name = "Padang Pasir Gerbang",       x = 0,    z = -320, r = 90,  density = 0.25f },
        new ZoneDef { name = "Oasis Zafir",                x = -185, z = -75,  r = 75,  density = 0.5f },
        new ZoneDef { name = "Dataran Kerikil",            x = 120,  z = -150, r = 95,  density = 0.3f },
        new ZoneDef { name = "Desa Sahr (Terbengkalai)",   x = 225,  z = -110, r = 60,  density = 0.15f },
        new ZoneDef { name = "Lembah Kuil",                x = 40,   z = 95,   r = 115, density = 0.2f },
        new ZoneDef { name = "Ngarai Merah",               x = 270,  z = 205,  r = 125, density = 0.2f },
        new ZoneDef { name = "Lautan Dune",                x = -110, z = 220,  r = 130, density = 0.25f },
        new ZoneDef { name = "Kuil Barat",                 x = -270, z = 200,  r = 85,  density = 0.3f },
        new ZoneDef { name = "Kuil Bayangan",              x = -50,  z = 335,  r = 95,  density = 0.15f },
        new ZoneDef { name = "Wadi Kering",                x = -300, z = -200, r = 120, density = 0.25f },
    };

    // koordinat desain
    static readonly Vector2 ZafirC = new Vector2(-165, -60);
    static readonly Vector2 SahrC = new Vector2(225, -110);
    static readonly Vector2 TempleC = new Vector2(40, 100);
    static readonly Vector2 WestTempleC = new Vector2(-270, 200);
    static readonly Vector2 ShadowC = new Vector2(-50, 335);
    static readonly Vector2 ScorpDen = new Vector2(115, 35);
    static readonly Vector2 GolemDen = new Vector2(300, 235);
    static readonly Vector3[] Oases = { new Vector3(-225, -110, 25f), new Vector3(22, -345, 9f), new Vector3(-300, 150, 12f), new Vector3(-20, 360, 10f) };
    static readonly Vector2[] Canyon = { new Vector2(175, 215), new Vector2(225, 200), new Vector2(270, 220), new Vector2(315, 245), new Vector2(345, 205) };

    [MenuItem("Hunter Guild/Build Hunt Area (Gurun Zahar)")]
    public static void BuildDesert()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DesertScenePath) != null)
        {
            if (!HGDlg.Show("Hunter Guild",
                "HuntArea_Desert.unity sudah ada.\nMembangun ulang akan MENIMPA scene itu.\n\nLanjutkan?", "Timpa", "Batal")) return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        foreach (var f in new[] { "Assets/Scenes", "Assets/Materials", MatDir, "Assets/Meshes", MeshDir, "Assets/Prefabs", DPrefabDir, TerrDir, "Assets/Textures", TexDir })
            EnsureFolder(f);

        EditorUtility.DisplayProgressBar("Hunter Guild", "Menyiapkan scene gurun...", 0.05f);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Random.InitState(9931);
        Zones = DesertZones;
        townSpawnName = "SpawnFromDesert";
        clearings.Clear();
        plan.Clear();
        LoadMaterials();
        NpcKit.Reset();
        ForestKit.Reset();
        ForestKit.ResetDesert();
        ForestKit.LoadDesert();
        DefineDesertTrails();
        PlanDesert();

        tRoot = new GameObject("HuntAreaDesert").transform;
        tCamp = Group("BaseCamp");
        tNodes = Group("GatherNodes");
        tRocks = Group("Rocks");
        tProps = Group("Props");

        clearings.Add(new Vector3(Camp.x, Camp.y, 42f));
        clearings.Add(new Vector3(ScorpDen.x, ScorpDen.y, 16f));
        clearings.Add(new Vector3(GolemDen.x, GolemDen.y, 18f));
        foreach (var o in Oases) clearings.Add(new Vector3(o.x, o.y, o.z * 1.5f));

        EditorUtility.DisplayProgressBar("Hunter Guild", "Membuat dune, ngarai, dan oasis...", 0.15f);
        BuildDesertTerrain();

        EditorUtility.DisplayProgressBar("Hunter Guild", "Menata desa, kuil, dan sarang bandit...", 0.6f);
        BuildWaterAndBounds();
        BuildDesertCamp();
        BuildTownApproach();
        BuildDesertSites();
        BuildDesertBandits();
        BuildDesertFx();
        ScatterDesert();
        BuildDesertMonsters();
        BuildDesertRocks();
        DesertCanon();   // kanon §8.4: Kuil Kaca, dua belas sapu dan jejak, Tugu Gerbang Pagi, kemah dasar sungai, surat Ngarai
        BuildPlayerAndManagers();
        var hm = Object.FindFirstObjectByType<HuntManager>();
        if (hm != null) hm.defaultZone = "Gurun Zahar";

        MarkStatic(tRocks);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0013f;
        RenderSettings.fogColor = new Color(0.86f, 0.76f, 0.6f);

        EditorUtility.DisplayProgressBar("Hunter Guild", "Menyimpan scene...", 0.95f);
        AssetDatabase.SaveAssets();
        HeightBake.BackupScene(DesertScenePath); EditorSceneManager.SaveScene(scene, DesertScenePath);
        AddToBuildSettings("Assets/Scenes/Town.unity", true);
        AddToBuildSettings(DesertScenePath, false);
        EditorUtility.ClearProgressBar();
        HGDlg.Show("Hunter Guild", "Gurun Zahar dibuat: " + DesertScenePath + "\nBangun ulang scene Town (Build Town Scene) supaya Gerbang Luar Timur aktif.", "OK");
    }

    // =====================================================================
    // Jalan setapak
    // =====================================================================
    static void DefineDesertTrails()
    {
        trails = new List<Vector2[]>
        {
            new[] { V(0, -470), V(0, -380), V(0, -330), V(20, -250), V(30, -170), V(10, -90), V(30, -20), V(40, 40), V(40, 100) },     // ke Lembah Kuil
            new[] { V(20, -250), V(-60, -200), V(-120, -150), V(-165, -60) },                                                          // ke Oasis Zafir
            new[] { V(30, -170), V(110, -150), V(170, -135), V(225, -110) },                                                           // jalan kafilah ke Desa Sahr
            new[] { V(40, 100), V(120, 170), V(190, 215), V(270, 215) },                                                               // ke Ngarai Merah
            new[] { V(40, 100), V(-30, 170), V(-80, 250), V(-50, 335) },                                                               // ke Kuil Bayangan
            new[] { V(-165, -60), V(-230, 20), V(-285, 100), V(-270, 200) },                                                           // jalan peziarah ke Kuil Barat
            new[] { V(110, 40), V(75, 70), V(40, 100) },                                                                               // sarang skorpion
        };
    }

    // =====================================================================
    // Terrain
    // =====================================================================
    static float DHeight(float x, float z)
    {
        float wx = x + (Noise(x, z, 0.004f, 3f) - 0.5f) * 140f;
        float wz = z + (Noise(x, z, 0.004f, 8f) - 0.5f) * 140f;
        float h = 0.30f;
        h += (Noise(wx, wz, 0.0025f, 11f) - 0.5f) * 0.12f;
        // dune: puncak sejajar arah angin, sisi lee lebih curam
        float d = wx * 0.906f + wz * 0.423f;
        float lam = 0.040f;
        float crest = Mathf.Sin(d * lam + Noise(x, z, 0.01f, 5f) * 3f);
        float asym = crest > 0f ? crest : crest * 0.55f;
        float duneMask = Smooth((Noise(x, z, 0.0035f, 21f) - 0.35f) / 0.3f);
        float big = Smooth(1f - Dist(x, z, -110f, 220f) / 150f);
        h += asym * (0.012f + 0.03f * duneMask + 0.08f * big);
        h += Mathf.Sin(d * lam * 2.3f + 1.7f + Noise(x, z, 0.02f, 9f) * 2f) * 0.006f * (0.4f + duneMask);
        h += (Noise(x, z, 0.05f, 73f) - 0.5f) * 0.006f;

        // Ngarai Merah: mesa bertingkat + ngarai berkelok
        float mm = Smooth(1f - Dist(x, z, 270f, 205f) / 150f);
        float hm = h + mm * 0.22f + (Noise(x, z, 0.02f, 17f) - 0.5f) * 0.05f * mm;
        float terr = Mathf.Floor(hm * 14f) / 14f;
        h = Mathf.Lerp(hm, terr + (hm - terr) * 0.18f, 0.88f * mm);
        float canyon = Smooth(1f - DistToPoly(x, z, Canyon) / 22f);
        h = Mathf.Lerp(h, Mathf.Min(h, 0.285f), canyon * Mathf.Clamp01(mm * 2f));

        // punggungan batu yang mengelilingi Lembah Kuil (terbuka di jalur)
        float ring = Smooth(1f - Mathf.Abs(Dist(x, z, TempleC.x, TempleC.y) - 150f) / 30f);
        ring *= Smooth(DistToTrails(x, z) / 28f);
        h += ring * (0.14f + (Noise(x, z, 0.03f, 41f) - 0.5f) * 0.1f);

        // oasis & kolam
        foreach (var o in Oases)
        {
            float dd = Dist(x, z, o.x, o.y);
            float k = dd < o.z ? 1f : Smooth(1f - (dd - o.z) / (o.z * 0.9f));
            h = Mathf.Lerp(h, 0.165f, k);
        }
        // wadi kering
        float wadi = Smooth(1f - Dist(x, z, -300f, -200f) / 120f);
        h = Mathf.Lerp(h, 0.275f + (Noise(x, z, 0.04f, 3f) - 0.5f) * 0.01f, wadi * 0.7f);

        float edge = Smooth((Mathf.Max(Mathf.Abs(x), z > 0f ? z : 0f) - 440f) / 50f);
        h += edge * 0.7f;
        h = Mathf.Lerp(h, 0.30f, Smooth(1f - Dist(x, z, Camp.x, Camp.y) / 50f));
        h = Plateau(h, x, z, ZafirC, 38f, 70f, 0.31f);
        h = Plateau(h, x, z, SahrC, 36f, 66f, 0.31f);
        h = Plateau(h, x, z, TempleC, 60f, 105f, 0.30f);
        h = Plateau(h, x, z, new Vector2(110f, 150f), 30f, 55f, 0.30f);
        h = Plateau(h, x, z, WestTempleC, 34f, 64f, 0.31f);
        h = Plateau(h, x, z, ShadowC, 40f, 75f, 0.31f);
        h = Mathf.Lerp(h, 0.30f, Smooth((-380f - z) / 40f));
        return Mathf.Clamp01(h);
    }

    static void BuildDesertTerrain()
    {
        td = new TerrainData();
        td.heightmapResolution = HRes;
        td.size = new Vector3(TSize, TH, TSize);
        HeightBake.NewAsset(td, TerrDir + "/DesertTerrain.asset");

        var hs = new float[HRes, HRes];
        float step = TSize / (HRes - 1);
        for (int zi = 0; zi < HRes; zi++)
            for (int xi = 0; xi < HRes; xi++)
                hs[zi, xi] = DHeight(M * (Origin + xi * step), M * (Origin + zi * step));
        FlattenPlan(hs, step);
        td.SetHeights(0, 0, hs);

        td.terrainLayers = MakeDesertLayers();
        td.alphamapResolution = 512;
        PaintDesertAlpha();
        PaintDesertDetail();

        treePrefabs = MakeDesertTreePrefabs();
        PlaceDesertTrees();

        var go = Terrain.CreateTerrainGameObject(td);
        go.name = "DesertTerrain";
        go.transform.SetParent(tRoot, false);
        go.transform.position = new Vector3(Origin, 0, Origin);
        terrain = go.GetComponent<Terrain>();
        HeightBake.Attach(go, td, hs, "DesertTerrain");
        terrain.treeDistance = 380f;
        terrain.treeBillboardDistance = 380f;
        terrain.treeMaximumFullLODCount = 350;
        terrain.detailObjectDistance = 70f;
        terrain.detailObjectDensity = 0.7f;
        terrain.heightmapPixelError = 6f;
        terrain.drawInstanced = true;
        EditorUtility.SetDirty(td);
    }

    static TerrainLayer[] MakeDesertLayers()
    {
        string[] names = { "DSand", "DDune", "DRock", "DCrack", "DGravel", "DDry", "DOasis" };
        Texture2D[] tex = { ForestKit.dSandT, ForestKit.dDuneT, ForestKit.dRockT, ForestKit.dCrackT, ForestKit.dGravelT, ForestKit.dDryGrassT, ForestKit.dOasisT };
        float[] tile = { 8f, 9f, 10f, 7f, 5f, 6f, 6f };
        var layers = new TerrainLayer[names.Length];
        for (int i = 0; i < layers.Length; i++)
        {
            var layer = new TerrainLayer();
            layer.diffuseTexture = tex[i];
            layer.tileSize = new Vector2(tile[i], tile[i]);
            layer.smoothness = i == 2 ? 0.2f : 0.02f;
            AssetDatabase.CreateAsset(layer, TerrDir + "/Layer_" + names[i] + ".terrainlayer");
            layers[i] = layer;
        }
        return layers;
    }

    static float OasisInfluence(float x, float z, float extra)
    {
        float best = 0f;
        foreach (var o in Oases) best = Mathf.Max(best, Smooth(1f - Dist(x, z, o.x, o.y) / (o.z * 1.9f + extra)));
        return best;
    }

    static void PaintDesertAlpha()
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
                float mm = Smooth(1f - Dist(wx, wz, 270f, 205f) / 150f);
                rock = Mathf.Max(rock, mm * 0.55f * Smooth((steep - 8f) / 10f));
                float gravel = Smooth(1f - DistToTrails(wx, wz) / 3.4f);
                gravel = Mathf.Max(gravel, Smooth(1f - DistSeg(wx, wz, 0f, -470f, 0f, -384f) / 9f));
                foreach (var c in clearings) gravel = Mathf.Max(gravel, Smooth(1f - Dist(wx, wz, c.x, c.y) / Mathf.Min(c.z, 14f)) * (c.z > 20f ? 0.8f : 0.5f));
                float crack = Smooth(1f - Mathf.Abs(hM - WaterY) / 3.2f) * 0.9f;
                crack = Mathf.Max(crack, Smooth(1f - Dist(wx, wz, -300f, -200f) / 110f) * (Noise(wx, wz, 0.03f, 4f) > 0.4f ? 0.85f : 0.3f));
                crack = Mathf.Max(crack, Smooth(1f - Dist(wx, wz, ZafirC.x, ZafirC.y) / 11f));                       // plaza desa
                crack = Mathf.Max(crack, Smooth(1f - Dist(wx, wz, SahrC.x, SahrC.y) / 10f) * 0.6f);
                float oasis = OasisInfluence(wx, wz, 0f);
                oasis = Mathf.Max(oasis, Smooth(1f - Dist(wx, wz, ZafirC.x - 30f, ZafirC.y - 8f) / 16f) * 0.9f);        // ladang
                float dry = Smooth((Noise(wx, wz, 0.02f, 5f) - 0.52f) / 0.2f) * 0.8f;
                dry = Mathf.Max(dry, OasisInfluence(wx, wz, 40f) * 0.7f);
                float dune = Smooth((Noise(wx, wz, 0.009f, 31f) - 0.5f) / 0.18f) * 0.85f;

                float rem = 1f;
                float wGravel = Mathf.Min(rem, gravel); rem -= wGravel;
                float wCrack = Mathf.Min(rem, crack); rem -= wCrack;
                float wRock = Mathf.Min(rem, rock); rem -= wRock;
                float wOasis = Mathf.Min(rem, oasis); rem -= wOasis;
                float wDry = rem * dry; rem -= wDry;
                float wDune = rem * dune; rem -= wDune;
                map[zi, xi, 0] = rem;
                map[zi, xi, 1] = wDune;
                map[zi, xi, 2] = wRock;
                map[zi, xi, 3] = wCrack;
                map[zi, xi, 4] = wGravel;
                map[zi, xi, 5] = wDry;
                map[zi, xi, 6] = wOasis;
            }
        alphaMap = map;
        td.SetAlphamaps(0, 0, map);
    }

    static void PaintDesertDetail()
    {
        td.detailPrototypes = new[]
        {
            new DetailPrototype
            {
                usePrototypeMesh = false, prototypeTexture = ForestKit.TexDryTuft(), renderMode = DetailRenderMode.GrassBillboard,
                minWidth = 0.8f, maxWidth = 1.6f, minHeight = 0.5f, maxHeight = 1.1f, noiseSpread = 0.4f,
                healthyColor = new Color(0.95f, 0.85f, 0.55f), dryColor = new Color(0.8f, 0.68f, 0.42f)
            },
            new DetailPrototype
            {
                usePrototypeMesh = false, prototypeTexture = ForestKit.TexGrassTuft(), renderMode = DetailRenderMode.GrassBillboard,
                minWidth = 0.9f, maxWidth = 1.6f, minHeight = 0.7f, maxHeight = 1.3f, noiseSpread = 0.35f,
                healthyColor = new Color(0.7f, 0.95f, 0.55f), dryColor = new Color(0.78f, 0.82f, 0.45f)
            }
        };
        int dr = 640;
        td.SetDetailResolution(dr, 16);
        var dry = new int[dr, dr];
        var green = new int[dr, dr];
        int ar = td.alphamapResolution;
        var rnd = new System.Random(5151);
        for (int z = 0; z < dr; z++)
            for (int x = 0; x < dr; x++)
            {
                int ax = Mathf.Clamp(Mathf.RoundToInt(x / (dr - 1f) * (ar - 1)), 0, ar - 1);
                int az = Mathf.Clamp(Mathf.RoundToInt(z / (dr - 1f) * (ar - 1)), 0, ar - 1);
                float gDry = alphaMap[az, ax, 5] * 0.9f + alphaMap[az, ax, 0] * 0.07f + alphaMap[az, ax, 1] * 0.05f;
                float gGreen = alphaMap[az, ax, 6];
                if (gGreen > 0.4f && rnd.NextDouble() < 0.5 * gGreen) green[z, x] = 1 + (rnd.NextDouble() < 0.4 ? 1 : 0);
                else if (rnd.NextDouble() < gDry * 0.45) dry[z, x] = 1;
            }
        td.SetDetailLayer(0, 0, 0, dry);
        td.SetDetailLayer(0, 0, 1, green);
    }

    // ---------- Pohon ----------
    // 0 palem A, 1 palem B, 2 palem C kecil, 3 kaktus A, 4 kaktus B, 5 agave, 6 semak A, 7 semak B, 8 pohon mati, 9 tumbleweed
    static GameObject[] MakeDesertTreePrefabs()
    {
        return new[]
        {
            DTreePrefab("DPalm_A", b => ForestKit.Palm(b, 3, 1f, 1f), 6f, 0.3f),
            DTreePrefab("DPalm_B", b => ForestKit.Palm(b, 8, 1.15f, 1.4f), 7f, 0.3f),
            DTreePrefab("DPalm_C", b => ForestKit.Palm(b, 14, 0.7f, 0.7f), 4f, 0.25f),
            DTreePrefab("DCactus_A", b => ForestKit.Cactus(b, 2, 1f), 4f, 0.4f),
            DTreePrefab("DCactus_B", b => ForestKit.Cactus(b, 5, 0.8f), 3.2f, 0.35f),
            DTreePrefab("DAgave", b => ForestKit.Agave(b, 6), 0f, 0f),
            DTreePrefab("DShrub_A", b => ForestKit.DryShrub(b, 4, 1f), 0f, 0f),
            DTreePrefab("DShrub_B", b => ForestKit.DryShrub(b, 9, 1.4f), 0f, 0f),
            DTreePrefab("DDeadTree", b => ForestKit.DeadTree(b, 7, 1.1f), 4f, 0.35f),
            DTreePrefab("DTumble", b => ForestKit.Tumbleweed(b, 3), 0f, 0f),
        };
    }

    static GameObject DTreePrefab(string name, System.Action<GeoBag> build, float colliderHeight, float colliderRadius)
    {
        var proto = PropKit.Get("Desert_" + name, build);
        var go = new GameObject(name);
        go.AddComponent<MeshFilter>().sharedMesh = proto.mesh;
        var mats = new Material[proto.used.Length];
        for (int i = 0; i < mats.Length; i++) mats[i] = ForestKit.DSlots[proto.used[i]];
        go.AddComponent<MeshRenderer>().sharedMaterials = mats;
        if (colliderHeight > 0f)
        {
            var cap = go.AddComponent<CapsuleCollider>();
            cap.radius = colliderRadius; cap.height = colliderHeight; cap.center = new Vector3(0, colliderHeight / 2f, 0);
        }
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, DPrefabDir + "/" + name + ".prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    static float OasisDistance(float x, float z)
    {
        float best = 99999f;
        foreach (var o in Oases) best = Mathf.Min(best, Dist(x, z, o.x, o.y) - o.z);
        return best;
    }

    static void PlaceDesertTrees()
    {
        var protos = new TreePrototype[treePrefabs.Length];
        for (int i = 0; i < protos.Length; i++) protos[i] = new TreePrototype { prefab = treePrefabs[i] };
        var list = new List<TreeInstance>();

        // deretan palem di tepi jalan dari gerbang kota
        for (float z = -462f; z <= -395f; z += 9f)
            foreach (float sx in new[] { -1f, 1f })
            {
                float dx = sx * (16f + Random.Range(-0.8f, 0.8f));
                float rx = (M * dx - Origin) / TSize, rz = (M * z - Origin) / TSize;
                list.Add(MakeTree(rx, rz, td.GetInterpolatedHeight(rx, rz), Random.value < 0.5f ? 0 : 2, 1f, Random.Range(0.95f, 1.25f)));
            }

        for (int i = 0; i < 120000 && list.Count < 2500; i++)
        {
            float wx = Random.Range(-445f, 445f), wz = Random.Range(-445f, 445f);
            float nx = (M * wx - Origin) / TSize, nz = (M * wz - Origin) / TSize;
            float hM = td.GetInterpolatedHeight(nx, nz);
            if (hM < WaterY + 0.6f) continue;
            if (td.GetSteepness(nx, nz) > 30f) continue;
            if (DistToTrails(wx, wz) < 4f) continue;
            if (NearClearing(wx, wz)) continue;
            if (Mathf.Abs(wx) < 30f && wz < -380f) continue;
            if (wz < -455f) continue;

            float od = OasisDistance(wx, wz);
            int proto; float s = Random.Range(0.8f, 1.4f);
            if (od < 70f)
            {
                float p = Mathf.Lerp(0.95f, 0.08f, Mathf.Clamp01(od / 70f));
                if (Random.value > p) continue;
                float r = Random.value;
                proto = r < 0.45f ? 0 : (r < 0.8f ? 1 : 2);
                if (Random.value < 0.2f) { proto = 6; s = Random.Range(0.8f, 1.3f); }
            }
            else
            {
                var zd = ZoneAt(wx, wz);
                float dens = (zd != null ? zd.density : 0.25f) * (0.25f + 1.1f * Noise(wx, wz, 0.02f, 5f));
                dens *= 0.35f;
                if (Random.value > dens) continue;
                float r = Random.value;
                if (r < 0.22f) proto = Random.value < 0.5f ? 3 : 4;
                else if (r < 0.35f) proto = 5;
                else if (r < 0.72f) proto = Random.value < 0.6f ? 6 : 7;
                else if (r < 0.84f) proto = 8;
                else proto = 9;
                if (proto == 8) s = Random.Range(0.9f, 1.4f);
            }
            list.Add(MakeTree(nx, nz, hM, proto, s, s * Random.Range(0.9f, 1.2f)));
        }
        CommitTrees(list, null);
    }

    // =====================================================================
    // Camp
    // =====================================================================
    static GameObject DKit(string key, System.Action<GeoBag> build, Transform parent, Vector3 pos, float worldYaw, float scale, bool collider)
    {
        var proto = PropKit.Get(key, build);
        var go = PropKit.Place(proto, parent, key, pos, worldYaw, scale, ForestKit.DSlots);
        if (collider) go.AddComponent<MeshCollider>().sharedMesh = proto.mesh;
        return go;
    }

    static void BuildDesertCamp()
    {
        Vector3 c = G(Camp.x, Camp.y);
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            int cs = i % 2 == 0 ? Sl.Cloth : Sl.ClothB;
            Vector3 p = Gw(c.x + Mathf.Sin(a) * 12f, c.z + Mathf.Cos(a) * 12f);
            var go = DKit("DTent_" + cs, b => ForestKit.Tent(b, 1, cs), tCamp, p, i * 90f + 45f, 1.3f, false);
            var bc = go.AddComponent<BoxCollider>();
            bc.center = new Vector3(0, 0.85f, 0); bc.size = new Vector3(2.0f, 1.7f, 3.0f);
        }
        var fire = new GameObject("Campfire").transform;
        fire.SetParent(tCamp, false);
        fire.position = Gw(c.x, c.z);
        var fm = DKit("DCampfire", b => ForestKit.Campfire(b, 3), fire, fire.position, 0f, 1.25f, false);
        var lg = new GameObject("FireLight").AddComponent<Light>();
        lg.transform.SetParent(fire, false);
        lg.transform.localPosition = new Vector3(0, 1.5f, 0);
        lg.type = LightType.Point; lg.range = 22f; lg.intensity = 1.8f; lg.color = new Color(1f, 0.6f, 0.25f);

        DKit("DCrate", b => ForestKit.Crate(b, 2), tCamp, Gw(c.x + 5f, c.z + 3f), 20f, 1.4f, true);
        DKit("DBarrel", b => ForestKit.Barrel(b), tCamp, Gw(c.x + 4.2f, c.z + 5.4f), 0f, 1.3f, true);
        DKit("DJars", b => ForestKit.Jars(b, 3), tCamp, Gw(c.x - 5.5f, c.z + 4f), 40f, 1.2f, false);
        DKit("DRugCamp", b => ForestKit.Rug(b, Vector3.zero, 3.4f, 2.2f, 0f), tCamp, Gw(c.x + 2.8f, c.z - 3.5f), 30f, 1f, false);
        foreach (var pp in new[] { new Vector2(-14f, 14f), new Vector2(15f, 12f), new Vector2(-20f, -4f), new Vector2(22f, -2f), new Vector2(3f, 24f) })
            DKit("DPalmCamp_" + (int)pp.x, b => ForestKit.Palm(b, (int)(pp.x * 3f + 40f), 1f, 1f), tCamp, Gw(c.x + pp.x, c.z + pp.y), pp.x * 17f, 1f, false)
                .AddComponent<CapsuleCollider>().height = 4f;
        var sign = new GameObject("TrailSign").transform;
        sign.SetParent(tCamp, false);
        sign.position = Gw(c.x, c.z + 22f);
        Prim(PrimitiveType.Cube, "Post", sign, new Vector3(0, 1.5f, 0), new Vector3(0.3f, 3f, 0.3f), mWood);
        Prim(PrimitiveType.Cube, "Board", sign, new Vector3(0, 2.7f, 0), new Vector3(2.6f, 0.9f, 0.15f), mDark, false);

        var ip = new GameObject("BaseCamp_Interact");
        ip.transform.SetParent(tCamp, false);
        ip.transform.position = Gw(c.x, c.z + 5.5f);
        var bcm = ip.AddComponent<BaseCamp>();
        bcm.prompt = "Base Camp Oasis";
        bcm.radius = 5f;
        Prim(PrimitiveType.Cylinder, "Marker", ip.transform, new Vector3(0, 0.05f, 0), new Vector3(2.6f, 0.03f, 2.6f), mGold, false);
        MakeSpring(Gw(c.x + 21f, c.z + 9f), "Mata Air Oasis");
    }

    // =====================================================================
    // Monster, gathering, batu
    // =====================================================================
    static void SpawnRing(Transform parent, string id, Vector2 c, float r, int n, float phase = 0f)
    {
        for (int i = 0; i < n; i++)
        {
            float a = (phase + i * 360f / n) * Mathf.Deg2Rad;
            float rr = r * (0.55f + 0.45f * ((i * 37) % 10) / 10f);
            Vector3 p = G(c.x + Mathf.Sin(a) * rr, c.y + Mathf.Cos(a) * rr);
            if (p.y < WaterY + 0.3f) continue;
            SpawnMonsterAt(parent, id, p);
        }
    }

    static void BuildDesertMonsters()
    {
        var root = Group("Monsters");
        SpawnMonster(root, "giant_scorpion", ScorpDen);
        SpawnMonster(root, "sand_golem", GolemDen);
        SpawnGroup(root, "sand_lizard", -185, -75, 70, 6);
        SpawnGroup(root, "sand_lizard", 0, -300, 60, 4);
        SpawnGroup(root, "sand_lizard", -300, -200, 70, 5);
        SpawnGroup(root, "sand_lizard", -110, 220, 90, 4);
        // penjaga kuil
        SpawnRing(root, "mummy", TempleC, 18f, 6, 20f);
        SpawnRing(root, "mummy", new Vector2(40f, 40f), 10f, 2, 0f);
        SpawnRing(root, "mummy", ShadowC, 16f, 5, 10f);
        SpawnRing(root, "mummy", WestTempleC, 14f, 3, 40f);
        SpawnRing(root, "mummy", new Vector2(110f, 150f), 12f, 2, 90f);
    }

    static void ScatterDesert()
    {
        // zona: 1 padang, 2 zafir, 3 kerikil, 5 lembah kuil, 6 ngarai, 7 dune, 10 wadi
        Scatter("cactus_fruit", 44, new[] { 1, 3, 7, 10, 6 });
        Scatter("herb", 18, new[] { 2, 0, 8 });
        Scatter("wild_berry", 12, new[] { 2, 8 });
        Tracks("giant_scorpion", ScorpDen);
        Tracks("sand_golem", GolemDen);
        foreach (var o in Oases)
        {
            Vector3? s = Shore(V(o.x, o.y), V(1, 0), 80f);
            if (s.HasValue && o.x < 0f && o.y < -100f) MakeSpring(s.Value, "Air Oasis", false);
        }
    }

    static void BuildDesertRocks()
    {
        int made = 0;
        for (int i = 0; i < 4000 && made < 200; i++)
        {
            float x = Random.Range(-440f, 440f), z = Random.Range(-440f, 440f);
            if (GroundY(x, z) < WaterY + 0.3f || NearClearing(x, z)) continue;
            if (DistToTrails(x, z) < 3f) continue;
            float s = Random.Range(0.7f, 2.0f);
            int v = Random.Range(0, 4);
            var go = DKit("DBoulder_" + v, b => ForestKit.Boulder(b, v, 1f, true), tRocks, G(x, z), Random.Range(0f, 360f), s, false);
            go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
            made++;
        }
        // hoodoo di ngarai dan lembah
        made = 0;
        for (int i = 0; i < 3000 && made < 38; i++)
        {
            float x = Random.Range(-440f, 440f), z = Random.Range(-440f, 440f);
            float mm = Smooth(1f - Dist(x, z, 270f, 205f) / 150f);
            bool ring = Mathf.Abs(Dist(x, z, TempleC.x, TempleC.y) - 135f) < 35f;
            if (mm < 0.2f && !ring && Noise(x, z, 0.02f, 7f) < 0.7f) continue;
            if (GroundY(x, z) < WaterY + 0.5f || NearClearing(x, z) || DistToTrails(x, z) < 6f || Slope(x, z) > 26f) continue;
            int v = Random.Range(0, 3);
            var go = DKit("DHoodoo_" + v, b => ForestKit.Hoodoo(b, v + 2, 5f + v * 1.6f), tRocks, G(x, z), Random.Range(0f, 360f), Random.Range(0.9f, 1.5f), false);
            go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
            made++;
        }
        // rangka makhluk, tumbleweed
        made = 0;
        for (int i = 0; i < 2000 && made < 9; i++)
        {
            float x = Random.Range(-420f, 420f), z = Random.Range(-420f, 420f);
            if (GroundY(x, z) < WaterY + 0.5f || NearClearing(x, z) || DistToTrails(x, z) < 8f || Slope(x, z) > 14f) continue;
            DKit("DBones", b => ForestKit.BeastSkeleton(b, 3), tRocks, G(x, z), Random.Range(0f, 360f), Random.Range(1f, 1.5f), false);
            made++;
        }
    }
}
