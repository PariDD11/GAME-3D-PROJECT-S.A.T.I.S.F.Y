using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using HunterGuild;

/// <summary>
/// Menu: Hunter Guild > Build Hunt Area (Hutan Lerai)
/// Membangun area berburu: hutan raksasa 1000 x 1000 m dengan Terrain (bukit, danau, sungai, rawa, jalur setapak),
/// ~14.000 pohon, 8 zona, base camp, sarang monster, jejak, titik gathering, landmark, dan monster placeholder.
/// Disimpan ke Assets/Scenes/HuntArea_G.unity (nama sama dengan tombol Berangkat di Gerbang Kota).
/// </summary>
public static partial class HuntSceneBuilder
{
    const string ScenePath = "Assets/Scenes/HuntArea_G.unity";
    const string MatDir = "Assets/Materials/Hunt";
    const string TerrDir = "Assets/Terrain";
    const string MeshDir = "Assets/Meshes/Forest";
    const string PrefabDir = "Assets/Prefabs/Forest";
    const string TexDir = "Assets/Textures/Forest";

    const int HRes = 513;
    const float TSize = 1000f, TH = 80f, WaterY = 16f, Origin = -500f;

    class ZoneDef { public string name; public float x, z, r, density; public int tree; }

    // tree: 0 bulat, 1 pinus, 2 semak, 3 pohon mati
    static ZoneDef[] Zones = ForestZones;
    static string townSpawnName = "SpawnFromForest";

    static readonly ZoneDef[] ForestZones =
    {
        new ZoneDef { name = "Base Camp",           x = 0,    z = -370, r = 55,  density = 0.0f,  tree = 0 },
        new ZoneDef { name = "Padang Gerbang",      x = 0,    z = -320, r = 90,  density = 0.10f, tree = 0 },
        new ZoneDef { name = "Hutan Barat",         x = -250, z = -190, r = 130, density = 1.0f,  tree = 1 },
        new ZoneDef { name = "Sungai dan Danau",    x = 200,  z = -150, r = 120, density = 0.35f, tree = 0 },
        new ZoneDef { name = "Hutan Tengah",        x = 0,    z = 0,    r = 130, density = 0.9f,  tree = 0 },
        new ZoneDef { name = "Bukit Berbatu",       x = 300,  z = 170,  r = 130, density = 0.45f, tree = 1 },
        new ZoneDef { name = "Rawa Berkabut",       x = -300, z = 250,  r = 120, density = 0.25f, tree = 3 },
        new ZoneDef { name = "Hutan Gelap",         x = 20,   z = 330,  r = 130, density = 1.0f,  tree = 1 },
        new ZoneDef { name = "Perkemahan Goblin",   x = 300,  z = -300, r = 90,  density = 0.5f,  tree = 0 },
        new ZoneDef { name = "Desa Alder",          x = -125, z = 70,   r = 52,  density = 0.04f, tree = 0 },
        new ZoneDef { name = "Desa Marrow (Terbengkalai)", x = 175, z = 255, r = 58, density = 0.30f, tree = 3 },
    };

    // Orientasi: koordinat desain dicerminkan ke dunia (M = -1) supaya jalan dari Gerbang Selatan kota masuk dari sisi UTARA peta
    // dan hutan terbentang ke SELATAN (-Z), sama seperti di scene Town. Semua angka desain di bawah tetap dipakai apa adanya.
    const float M = -1f;
    static Vector3 spawnPos;
    static readonly Vector2 Camp = new Vector2(0, -370);
    static readonly Vector2 WolfDen = new Vector2(-260, -180);
    static readonly Vector2 BoarDen = new Vector2(300, 170);
    static readonly Vector2 DireDen = new Vector2(40, 330);
    static readonly Vector2 GoblinCamp = new Vector2(300, -300);
    static readonly Vector2 LakeC = new Vector2(200, -150);
    static readonly Vector2 AlderC = new Vector2(-125, 70);       // desa kecil yang masih hidup
    static readonly Vector2 MarrowC = new Vector2(175, 255);      // desa terbengkalai (sarang bandit)
    static readonly Vector2[] Stream = { new Vector2(300, 170), new Vector2(240, 125), new Vector2(170, 95), new Vector2(120, 45), new Vector2(112, -30), new Vector2(140, -100), new Vector2(190, -140) };

    static List<Vector2[]> trails;
    static readonly List<Vector3> clearings = new List<Vector3>();   // x, z, radius
    static Terrain terrain;
    static TerrainData td;
    static GameObject[] treePrefabs;
    static Mesh pyramidMesh;
    static Material mWood, mDark, mStone, mWater, mGlow, mWhite, mLeaf, mRock, mGold, mHay, mMud;
    static Material[] cloths;
    static Transform tRoot, tCamp, tNodes, tRocks, tProps;

    [MenuItem("Hunter Guild/Build Hunt Area (Hutan Lerai)")]
    public static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            if (!HGDlg.Show("Hunter Guild",
                "HuntArea_G.unity sudah ada.\nMembangun ulang akan MENIMPA scene itu.\n\nLanjutkan?", "Timpa", "Batal")) return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        foreach (var f in new[] { "Assets/Scenes", "Assets/Materials", MatDir, "Assets/Meshes", MeshDir, "Assets/Prefabs", PrefabDir, TerrDir, "Assets/Textures", TexDir })
            EnsureFolder(f);

        EditorUtility.DisplayProgressBar("Hunter Guild", "Menyiapkan scene...", 0.05f);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Random.InitState(7742);
        Zones = ForestZones;
        townSpawnName = "SpawnFromForest";
        clearings.Clear();
        LoadMaterials();
        NpcKit.Reset();
        ForestKit.Reset();
        ForestKit.Load();
        pyramidMesh = MakePyramidMesh();
        DefineTrails();
        PlanVillages();

        tRoot = new GameObject("HuntArea").transform;
        tCamp = Group("BaseCamp");
        tNodes = Group("GatherNodes");
        tRocks = Group("Rocks");
        tProps = Group("Props");

        // pembersihan area: sarang, camp, landmark
        clearings.Add(new Vector3(Camp.x, Camp.y, 42f));
        clearings.Add(new Vector3(WolfDen.x, WolfDen.y, 16f));
        clearings.Add(new Vector3(BoarDen.x, BoarDen.y, 16f));
        clearings.Add(new Vector3(DireDen.x, DireDen.y, 16f));
        clearings.Add(new Vector3(GoblinCamp.x, GoblinCamp.y, 32f));
        clearings.Add(new Vector3(0, 0, 24f));
        clearings.Add(new Vector3(-40, 300, 14f));
        clearings.Add(new Vector3(-300, 250, 16f));
        clearings.Add(new Vector3(330, 150, 18f));

        EditorUtility.DisplayProgressBar("Hunter Guild", "Membuat terrain dan tekstur...", 0.15f);
        BuildTerrain();

        EditorUtility.DisplayProgressBar("Hunter Guild", "Menata camp, sarang, dan landmark...", 0.7f);
        BuildWaterAndBounds();
        BuildCamp();
        BuildBridges();
        BuildTownApproach();
        BuildLandmarks();
        BuildDens();
        BuildVillages();
        BuildBanditSites();
        BuildWorldFx();
        ScatterAll();
        BuildMonsters();
        BuildRocks();
        ForestCanon();                       // kanon §8.3 (tanpa UnityEngine.Random: urutan acak di atas tidak bergeser)
        BuildPlayerAndManagers();

        MarkStatic(tRocks);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0017f;
        RenderSettings.fogColor = new Color(0.64f, 0.72f, 0.72f);

        EditorUtility.DisplayProgressBar("Hunter Guild", "Menyimpan scene...", 0.95f);
        AssetDatabase.SaveAssets();
        HeightBake.BackupScene(ScenePath); EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings("Assets/Scenes/Town.unity", true);
        AddToBuildSettings(ScenePath, false);
        EditorUtility.ClearProgressBar();
        HGDlg.Show("Hunter Guild", "Hutan Lerai dibuat: " + ScenePath + "\nTekan Play untuk mencoba, atau berangkat dari Gerbang Kota di scene Town.", "OK");
    }

    // =====================================================================
    // Fungsi matematika terrain
    // =====================================================================
    static Vector2 V(float x, float z) { return new Vector2(x, z); }
    static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }
    static float Dist(float x, float z, float cx, float cz) { float dx = x - cx, dz = z - cz; return Mathf.Sqrt(dx * dx + dz * dz); }
    static float Noise(float x, float z, float s, float o) { return Mathf.PerlinNoise(x * s + o + 1000f, z * s + o * 1.7f + 1000f); }

    static float DistSeg(float px, float pz, float ax, float az, float bx, float bz)
    {
        float vx = bx - ax, vz = bz - az;
        float l2 = vx * vx + vz * vz;
        float t = l2 < 0.0001f ? 0f : Mathf.Clamp01(((px - ax) * vx + (pz - az) * vz) / l2);
        return Dist(px, pz, ax + vx * t, az + vz * t);
    }

    static float DistToTrails(float x, float z)
    {
        float best = 99999f;
        foreach (var tr in trails)
            for (int i = 0; i + 1 < tr.Length; i++)
            {
                float d = DistSeg(x, z, tr[i].x, tr[i].y, tr[i + 1].x, tr[i + 1].y);
                if (d < best) best = d;
            }
        return best;
    }

    static bool NearClearing(float x, float z)
    {
        foreach (var c in clearings) if (Dist(x, z, c.x, c.y) < c.z) return true;
        return false;
    }

    static ZoneDef ZoneAt(float x, float z)
    {
        ZoneDef best = null;
        float bestRatio = 1f;
        foreach (var zd in Zones)
        {
            float ratio = Dist(x, z, zd.x, zd.z) / zd.r;
            if (ratio < bestRatio) { bestRatio = ratio; best = zd; }
        }
        return best;
    }

    static float DistToPoly(float x, float z, Vector2[] pts)
    {
        float best = 99999f;
        for (int i = 0; i + 1 < pts.Length; i++)
        {
            float d = DistSeg(x, z, pts[i].x, pts[i].y, pts[i + 1].x, pts[i + 1].y);
            if (d < best) best = d;
        }
        return best;
    }

    static float Plateau(float h, float x, float z, Vector2 c, float flatR, float blendR, float target)
    {
        float d = Dist(x, z, c.x, c.y);
        float k = 1f - Smooth((d - flatR) / (blendR - flatR));
        return Mathf.Lerp(h, target, k);
    }

    static float Height01(float x, float z)
    {
        // domain warping supaya lembah dan punggungan tidak terlihat seperti noise biasa
        float wx = x + (Noise(x, z, 0.0045f, 3f) - 0.5f) * 120f;
        float wz = z + (Noise(x, z, 0.0045f, 8f) - 0.5f) * 120f;
        float h = 0.30f;
        h += (Noise(wx, wz, 0.0028f, 11f) - 0.5f) * 0.27f;                              // lembah dan punggungan besar
        float rd = 1f - Mathf.Abs(2f * Noise(wx, wz, 0.0062f, 29f) - 1f);               // punggungan tajam
        h += (rd * rd - 0.35f) * 0.105f;
        h += (Noise(x, z, 0.012f, 37f) - 0.5f) * 0.05f;
        h += (Noise(x, z, 0.045f, 73f) - 0.5f) * 0.014f;
        h += Smooth(1f - Dist(x, z, 300f, 170f) / 130f) * 0.20f;                        // bukit berbatu
        float lake = Smooth(1f - Dist(x, z, LakeC.x, LakeC.y) / 75f);
        h = Mathf.Lerp(h, 0.14f, lake);                                                 // danau
        float riv = Smooth(1f - DistSeg(x, z, 200f, -150f, 520f, -260f) / 16f);
        h = Mathf.Lerp(h, Mathf.Min(h, 0.16f), riv);                                    // sungai ke timur
        // anak sungai berkelok dari bukit ke danau; jalan setapak membuatnya dangkal (tempat menyeberang)
        float bed = Smooth(1f - DistToPoly(x, z, Stream) / 26f);
        float ford = Smooth(1f - DistToTrails(x, z) / 7f);
        h = Mathf.Lerp(h, Mathf.Min(h, 0.185f), bed * (1f - ford * 0.3f));
        float swamp = Smooth(1f - Dist(x, z, -300f, 250f) / 100f);
        h = Mathf.Lerp(h, 0.205f + (Noise(x, z, 0.05f, 5f) - 0.5f) * 0.03f, swamp * 0.85f);   // rawa
        float edge = Smooth((Mathf.Max(Mathf.Abs(x), z > 0f ? z : 0f) - 440f) / 50f);       // sisi kota (z < 0) tanpa gunung
        h += edge * 0.7f;                                                               // pegunungan pembatas
        float camp = Smooth(1f - Dist(x, z, Camp.x, Camp.y) / 50f);
        h = Mathf.Lerp(h, 0.30f, camp);                                                 // lahan datar camp
        h = Plateau(h, x, z, AlderC, 36f, 68f, 0.315f);                                 // dataran desa
        h = Plateau(h, x, z, MarrowC, 34f, 64f, 0.315f);                                // dataran desa terbengkalai
        float strip = Smooth((-372f - z) / 22f);
        h = Mathf.Lerp(h, 0.30f, strip);                                                // dataran di depan tembok kota
        return Mathf.Clamp01(h);
    }

    static void DefineTrails()
    {
        trails = new List<Vector2[]>
        {
            new[] { V(0, -470), V(0, -380), V(0, -330), V(20, -250), V(30, -150), V(0, -60), V(0, 0), V(0, 150), V(40, 330) },
            new[] { V(0, -330), V(-120, -270), V(-260, -180) },
            new[] { V(30, -150), V(120, -150), V(140, -150) },
            new[] { V(0, 0), V(150, 60), V(300, 170) },
            new[] { V(-260, -180), V(-280, 0), V(-300, 250) },
            new[] { V(20, -250), V(150, -290), V(300, -300) },
            new[] { V(0, 0), V(-60, 35), V(-125, 70) },                              // ke Desa Alder
            new[] { V(0, 150), V(45, 168), V(90, 190), V(140, 225), V(175, 255) },   // jalan tua ke Desa Marrow
            new[] { V(-125, 70), V(-190, 20), V(-260, -60) },                        // jalan desa ke hutan barat
        };
    }

    // =====================================================================
    // Terrain
    // =====================================================================
    static void BuildTerrain()
    {
        td = new TerrainData();
        td.heightmapResolution = HRes;
        td.size = new Vector3(TSize, TH, TSize);
        HeightBake.NewAsset(td, TerrDir + "/HuntTerrain.asset");

        var hs = new float[HRes, HRes];
        float step = TSize / (HRes - 1);
        for (int zi = 0; zi < HRes; zi++)
            for (int xi = 0; xi < HRes; xi++)
                hs[zi, xi] = Height01(M * (Origin + xi * step), M * (Origin + zi * step));
        FlattenPlan(hs, step);
        td.SetHeights(0, 0, hs);

        td.terrainLayers = MakeLayers();
        td.alphamapResolution = 512;
        PaintAlphamap();
        PaintGrassDetail();

        treePrefabs = MakeTreePrefabs();
        PlaceTrees();

        var go = Terrain.CreateTerrainGameObject(td);
        go.name = "ForestTerrain";
        go.transform.SetParent(tRoot, false);
        go.transform.position = new Vector3(Origin, 0, Origin);
        terrain = go.GetComponent<Terrain>();
        HeightBake.Attach(go, td, hs, "HuntTerrain");
        terrain.treeDistance = 420f;
        terrain.treeBillboardDistance = 420f;
        terrain.treeMaximumFullLODCount = 400;
        terrain.detailObjectDistance = 85f;
        terrain.detailObjectDensity = 0.85f;
        terrain.heightmapPixelError = 6f;
        terrain.drawInstanced = true;
        EditorUtility.SetDirty(td);
    }

    static float[,,] alphaMap;

    static TerrainLayer[] MakeLayers()
    {
        string[] names = { "Grass", "Dirt", "Rock", "Mud", "Litter", "Moss", "Cobble" };
        Texture2D[] tex = { ForestKit.tGrass, ForestKit.tDirt, ForestKit.tRock, ForestKit.tMud, ForestKit.tLitter, ForestKit.tMoss, ForestKit.tCobble };
        float[] tile = { 7f, 6f, 9f, 6f, 5f, 6f, 5f };
        var layers = new TerrainLayer[names.Length];
        for (int i = 0; i < layers.Length; i++)
        {
            var layer = new TerrainLayer();
            layer.diffuseTexture = tex[i];
            layer.tileSize = new Vector2(tile[i], tile[i]);
            layer.smoothness = i == 2 ? 0.25f : 0.05f;
            AssetDatabase.CreateAsset(layer, TerrDir + "/Layer_" + names[i] + ".terrainlayer");
            layers[i] = layer;
        }
        return layers;
    }

    static void PaintAlphamap()
    {
        int res = td.alphamapResolution;
        const int L = 7;
        var map = new float[res, res, L];
        for (int zi = 0; zi < res; zi++)
            for (int xi = 0; xi < res; xi++)
            {
                float nx = xi / (res - 1f), nz = zi / (res - 1f);
                float wx = M * (Origin + nx * TSize), wz = M * (Origin + nz * TSize);       // koordinat desain
                float hM = td.GetInterpolatedHeight(nx, nz);
                float steep = td.GetSteepness(nx, nz);

                float rock = Smooth((steep - 26f) / 12f);
                float mud = Mathf.Clamp01(1f - Mathf.Abs(hM - WaterY) / 1.8f);
                float dirt = Smooth(1f - DistToTrails(wx, wz) / 3.2f);
                dirt = Mathf.Max(dirt, Smooth(1f - DistSeg(wx, wz, 0f, -470f, 0f, -384f) / 9f));    // jalan lebar dari gerbang kota ke camp
                foreach (var c in clearings) dirt = Mathf.Max(dirt, Smooth(1f - Dist(wx, wz, c.x, c.y) / Mathf.Min(c.z, 14f)) * (c.z > 20f ? 1f : 0.55f));

                // desa: plaza berbatu bulat; di desa terbengkalai batunya pecah dan berlumut
                float dA = Dist(wx, wz, AlderC.x, AlderC.y), dM = Dist(wx, wz, MarrowC.x, MarrowC.y);
                float cobble = Smooth(1f - dA / 11f);
                float broken = Noise(wx, wz, 0.09f, 21f);
                cobble = Mathf.Max(cobble, Smooth(1f - dM / 10f) * (broken > 0.42f ? 1f : 0.25f));
                float forest = Noise(wx, wz, 0.022f, 5f);
                float litter = Smooth((forest - 0.42f) / 0.25f) * 0.9f;
                litter = Mathf.Max(litter, Smooth(1f - dM / 70f) * 0.6f);
                float moss = Smooth((Noise(wx, wz, 0.03f, 9f) - 0.6f) / 0.2f) * 0.8f;
                if (hM < WaterY + 2.5f) moss = Mathf.Max(moss, 0.55f);
                moss = Mathf.Max(moss, Smooth(1f - dM / 60f) * 0.5f * Noise(wx, wz, 0.05f, 14f));
                if (dA < 40f) { litter *= 0.35f; moss *= 0.3f; }

                float rem = 1f;
                float wDirt = Mathf.Min(rem, dirt); rem -= wDirt;
                float wCob = Mathf.Min(rem, cobble); rem -= wCob;
                float wRock = Mathf.Min(rem, rock); rem -= wRock;
                float wMud = Mathf.Min(rem, mud); rem -= wMud;
                float wLit = rem * litter; rem -= wLit;
                float wMoss = rem * moss; rem -= wMoss;
                map[zi, xi, 0] = rem;
                map[zi, xi, 1] = wDirt;
                map[zi, xi, 2] = wRock;
                map[zi, xi, 3] = wMud;
                map[zi, xi, 4] = wLit;
                map[zi, xi, 5] = wMoss;
                map[zi, xi, 6] = wCob;
            }
        alphaMap = map;
        td.SetAlphamaps(0, 0, map);
    }

    // rumput detail: helai rumput billboard di atas lapisan rumput (lebih rapat di tepi hutan)
    static void PaintGrassDetail()
    {
        var tuft = ForestKit.TexGrassTuft();
        td.detailPrototypes = new[]
        {
            new DetailPrototype
            {
                usePrototypeMesh = false, prototypeTexture = tuft, renderMode = DetailRenderMode.GrassBillboard,
                minWidth = 0.9f, maxWidth = 1.7f, minHeight = 0.7f, maxHeight = 1.3f, noiseSpread = 0.35f,
                healthyColor = new Color(0.78f, 0.95f, 0.65f), dryColor = new Color(0.85f, 0.78f, 0.45f)
            }
        };
        int dr = 768;
        td.SetDetailResolution(dr, 16);
        var det = new int[dr, dr];
        int ar = td.alphamapResolution;
        var rnd = new System.Random(4242);
        for (int z = 0; z < dr; z++)
            for (int x = 0; x < dr; x++)
            {
                int ax = Mathf.Clamp(Mathf.RoundToInt(x / (dr - 1f) * (ar - 1)), 0, ar - 1);
                int az = Mathf.Clamp(Mathf.RoundToInt(z / (dr - 1f) * (ar - 1)), 0, ar - 1);
                float g = alphaMap[az, ax, 0] + alphaMap[az, ax, 4] * 0.25f + alphaMap[az, ax, 5] * 0.4f;
                if (g < 0.45f) continue;
                if (rnd.NextDouble() < 0.5 * g) det[z, x] = 1 + (rnd.NextDouble() < 0.4 ? 1 : 0);
            }
        td.SetDetailLayer(0, 0, 0, det);
    }

    // ---------- Pohon ----------
    // indeks prototipe: 0 ek A, 1 cemara A, 2 semak, 3 pohon mati, 4 birch A, 5 maple A, 6 pakis, 7 ek B, 8 cemara B, 9 birch B, 10 maple B
    static GameObject[] MakeTreePrefabs()
    {
        return new[]
        {
            TreePrefab("TreeOak_A",    b => ForestKit.Oak(b, 3), 5f, 0.5f),
            TreePrefab("TreeSpruce_A", b => ForestKit.Spruce(b, 5), 6f, 0.4f),
            TreePrefab("TreeBush",     b => ForestKit.Bush(b, 7), 0f, 0f),
            TreePrefab("TreeDead",     b => ForestKit.DeadTree(b, 9), 5f, 0.4f),
            TreePrefab("TreeBirch_A",  b => ForestKit.Birch(b, 11), 5f, 0.25f),
            TreePrefab("TreeMaple_A",  b => ForestKit.Oak(b, 13, ForestKit.LeafOrange, ForestKit.LeafRed, 0.95f), 5f, 0.5f),
            TreePrefab("TreeFern",     b => ForestKit.Fern(b, 15), 0f, 0f),
            TreePrefab("TreeOak_B",    b => ForestKit.Oak(b, 21, ForestKit.LeafGreen, ForestKit.LeafYellow, 1.1f), 5f, 0.55f),
            TreePrefab("TreeSpruce_B", b => ForestKit.Spruce(b, 25, 1.1f), 6f, 0.45f),
            TreePrefab("TreeBirch_B",  b => ForestKit.Birch(b, 31, 1.1f), 5f, 0.25f),
            TreePrefab("TreeMaple_B",  b => ForestKit.Oak(b, 33, ForestKit.LeafRed, ForestKit.LeafOrange, 1.05f), 5f, 0.5f),
        };
    }

    static GameObject TreePrefab(string name, System.Action<GeoBag> build, float colliderHeight, float colliderRadius)
    {
        var proto = PropKit.Get("Hunt_" + name, build);
        var go = new GameObject(name);
        go.AddComponent<MeshFilter>().sharedMesh = proto.mesh;
        var mats = new Material[proto.used.Length];
        for (int i = 0; i < mats.Length; i++) mats[i] = ForestKit.Slots[proto.used[i]];
        go.AddComponent<MeshRenderer>().sharedMaterials = mats;
        if (colliderHeight > 0f)
        {
            var cap = go.AddComponent<CapsuleCollider>();
            cap.radius = colliderRadius;
            cap.height = colliderHeight;
            cap.center = new Vector3(0, colliderHeight / 2f, 0);
        }
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/" + name + ".prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    static TreeInstance MakeTree(float nx, float nz, float hM, int proto, float s, float hs)
    {
        return new TreeInstance
        {
            position = new Vector3(nx, hM / TH, nz),
            prototypeIndex = proto,
            widthScale = s,
            heightScale = hs,
            color = Color.white,
            lightmapColor = Color.white,
            rotation = Random.Range(0f, 6.28f)
        };
    }

    /// <summary>
    /// Pasang pohon sebagai objek prefab biasa (terrain tree dengan material non-tree memicu error
    /// "must use Nature/Soft Occlusion shader"). asObjects = null: semua prototipe jadi objek; selain itu hanya indeks tsb, sisanya terrain tree.
    /// </summary>
    static void CommitTrees(List<TreeInstance> list, int[] asObjects)
    {
        var obj = new HashSet<int>();
        if (asObjects == null) for (int i = 0; i < treePrefabs.Length; i++) obj.Add(i); else foreach (var i in asObjects) obj.Add(i);
        var map = new int[treePrefabs.Length];
        var protos = new List<TreePrototype>();
        for (int i = 0; i < treePrefabs.Length; i++)
        {
            if (obj.Contains(i)) { map[i] = -1; continue; }
            map[i] = protos.Count;
            protos.Add(new TreePrototype { prefab = treePrefabs[i] });
        }
        td.treePrototypes = protos.ToArray();
        var keep = new List<TreeInstance>();
        Transform parent = obj.Count > 0 ? Group(asObjects == null ? "Pohon" : "Semak") : null;
        Vector3 org = new Vector3(Origin, 0f, Origin);       // terrain baru dibuat setelah pohon ditata
        Vector3 sz = td.size;
        foreach (var t in list)
        {
            int m = map[t.prototypeIndex];
            if (m >= 0) { var k = t; k.prototypeIndex = m; keep.Add(k); continue; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(treePrefabs[t.prototypeIndex], parent);
            go.transform.position = org + new Vector3(t.position.x * sz.x, t.position.y * sz.y, t.position.z * sz.z);
            go.transform.rotation = Quaternion.Euler(0f, t.rotation * Mathf.Rad2Deg, 0f);
            go.transform.localScale = new Vector3(t.widthScale, t.heightScale, t.widthScale);
            go.isStatic = true;
        }
        td.SetTreeInstances(keep.ToArray(), true);
    }

    static void PlaceTrees()
    {
        var protos = new TreePrototype[treePrefabs.Length];
        for (int i = 0; i < protos.Length; i++) protos[i] = new TreePrototype { prefab = treePrefabs[i] };

        var list = new List<TreeInstance>();

        // deretan pohon di tepi jalan dari gerbang kota (sama dengan deretan di scene Town, supaya terlihat menyambung)
        for (float z = -462f; z <= -395f; z += 7f)
            foreach (float sx in new[] { -1f, 1f })
                foreach (float xo in new[] { 15f, 22f })
                {
                    float dx = sx * (xo + Random.Range(-0.8f, 0.8f));
                    float dz = z - (xo > 20f ? 3f : 0f);
                    float rx = (M * dx - Origin) / TSize, rz = (M * dz - Origin) / TSize;
                    list.Add(MakeTree(rx, rz, td.GetInterpolatedHeight(rx, rz), Random.value < 0.4f ? 1 : 0, 1f, Random.Range(0.95f, 1.3f)));
                }

        for (int i = 0; i < 90000 && list.Count < 3500; i++)
        {
            float wx = Random.Range(-445f, 445f), wz = Random.Range(-445f, 445f);
            float nx = (M * wx - Origin) / TSize, nz = (M * wz - Origin) / TSize;
            float hM = td.GetInterpolatedHeight(nx, nz);
            if (hM < WaterY + 0.8f) continue;
            if (td.GetSteepness(nx, nz) > 32f) continue;
            if (DistToTrails(wx, wz) < 4.5f) continue;
            if (NearClearing(wx, wz)) continue;
            if (Mathf.Abs(wx) < 30f && wz < -380f) continue;      // koridor jalan dari gerbang kota
            if (wz < -455f) continue;                              // depan tembok kota

            var zd = ZoneAt(wx, wz);
            float dens = zd != null ? zd.density : 0.8f;
            dens *= 0.6f + 0.8f * Noise(wx, wz, 0.02f, 5f);
            if (Random.value > dens) continue;

            int kind = zd != null ? zd.tree : (Noise(wx, wz, 0.01f, 9f) > 0.5f ? 0 : 1);
            int proto;
            float s = Random.Range(0.8f, 1.5f);
            float r = Random.value;
            if (kind == 3 && zd != null && zd.name.StartsWith("Desa Marrow"))
                proto = r < 0.65f ? 3 : (r < 0.85f ? 5 : 0);                       // sekitar desa terbengkalai: pohon mati dan maple gugur
            else if (kind == 3) proto = 3;                                           // rawa
            else if (kind == 1) proto = r < 0.5f ? 1 : 8;                           // cemara
            else proto = r < 0.18f ? 4 : (r < 0.30f ? 9 : (r < 0.50f ? 5 : (r < 0.64f ? 10 : (r < 0.82f ? 0 : 7))));
            if (proto != 3 && Random.value < 0.14f) { proto = 2; s = Random.Range(0.8f, 1.4f); }
            else if (proto != 3 && Random.value < 0.12f) { proto = 6; s = Random.Range(0.8f, 1.5f); }
            list.Add(MakeTree(nx, nz, hM, proto, s, s * Random.Range(0.9f, 1.25f)));
        }
        CommitTrees(list, null);
    }

    // =====================================================================
    // Air, batas peta, camp
    // =====================================================================
    // x,z = koordinat DESAIN (dicerminkan ke dunia). Gw/GroundYW = koordinat DUNIA.
    static float GroundYW(float wx, float wz) { return terrain.SampleHeight(new Vector3(wx, 0, wz)) + terrain.transform.position.y; }
    static float GroundY(float x, float z) { return GroundYW(M * x, M * z); }
    static Vector3 G(float x, float z) { return new Vector3(M * x, GroundY(x, z), M * z); }
    static Vector3 Gw(float wx, float wz) { return new Vector3(wx, GroundYW(wx, wz), wz); }
    static float Slope(float x, float z) { return td.GetSteepness((M * x - Origin) / TSize, (M * z - Origin) / TSize); }

    static void BuildWaterAndBounds()
    {
        Prim(PrimitiveType.Cube, "Water", tRoot, new Vector3(0, WaterY - 0.1f, 0), new Vector3(1000, 0.2f, 1000), mWater, false);
        foreach (float s in new[] { -1f, 1f })
        {
            Prim(PrimitiveType.Cube, "BoundX", tRoot, new Vector3(497f * s, 60, 0), new Vector3(6, 200, 1000), mStone, false).GetComponent<Renderer>().enabled = false;
            Prim(PrimitiveType.Cube, "BoundZ", tRoot, new Vector3(0, 60, 497f * s), new Vector3(1000, 200, 6), mStone, false).GetComponent<Renderer>().enabled = false;
        }
        Prim(PrimitiveType.Cube, "BoundTown", tRoot, new Vector3(0, 60, M * -436f), new Vector3(1000, 200, 2), mStone, false).GetComponent<Renderer>().enabled = false;
        foreach (var c in tRoot.GetComponentsInChildren<Transform>())
            if (c.name.StartsWith("Bound") && c.GetComponent<Collider>() == null) c.gameObject.AddComponent<BoxCollider>();
    }

    static void BuildCamp()
    {
        Vector3 c = G(Camp.x, Camp.y);
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            Tent(tCamp, Gw(c.x + Mathf.Sin(a) * 12f, c.z + Mathf.Cos(a) * 12f), i * 90f + 45f, cloths[i % 3], 4.6f);
        }
        // api unggun
        var fire = new GameObject("Campfire").transform;
        fire.SetParent(tCamp, false);
        fire.position = Gw(c.x, c.z);
        var fireMesh = PropKit.Place(PropKit.Get("HuntCampfire", b => ForestKit.Campfire(b, 3)), fire, "FireMesh", fire.position, 0f, 1.25f, ForestKit.Slots);
        fireMesh.transform.SetParent(fire, true);
        var lg = new GameObject("FireLight").AddComponent<Light>();
        lg.transform.SetParent(fire, false);
        lg.transform.localPosition = new Vector3(0, 1.5f, 0);
        lg.type = LightType.Point; lg.range = 22f; lg.intensity = 1.8f; lg.color = new Color(1f, 0.6f, 0.25f);

        // kotak perbekalan & papan
        KitAtWorld("HuntCrate", b => ForestKit.Crate(b, 2), tCamp, Gw(c.x + 5f, c.z + 3f), 20f, 1.4f, true);
        KitAtWorld("HuntCrate2", b => ForestKit.Crate(b, 5), tCamp, Gw(c.x + 6.4f, c.z + 4.6f), -15f, 1.2f, true);
        KitAtWorld("HuntBarrel", b => ForestKit.Barrel(b), tCamp, Gw(c.x + 4.2f, c.z + 5.4f), 0f, 1.3f, true);
        KitAtWorld("HuntHay", b => ForestKit.HayBale(b, 1), tCamp, Gw(c.x - 6f, c.z + 4f), 40f, 1.2f, true);
        var sign = new GameObject("TrailSign").transform;
        sign.SetParent(tCamp, false);
        sign.position = Gw(c.x, c.z + 22f);
        Prim(PrimitiveType.Cube, "Post", sign, new Vector3(0, 1.5f, 0), new Vector3(0.3f, 3f, 0.3f), mWood);
        Prim(PrimitiveType.Cube, "Board", sign, new Vector3(0, 2.7f, 0), new Vector3(2.6f, 0.9f, 0.15f), mDark, false);

        // interaksi base camp
        var ip = new GameObject("BaseCamp_Interact");
        ip.transform.SetParent(tCamp, false);
        ip.transform.position = Gw(c.x, c.z + 5.5f);
        var bc = ip.AddComponent<BaseCamp>();
        bc.prompt = "Base Camp";
        bc.radius = 5f;
        Prim(PrimitiveType.Cylinder, "Marker", ip.transform, new Vector3(0, 0.05f, 0), new Vector3(2.6f, 0.03f, 2.6f), mGold, false);

        // mata air dekat camp
        MakeSpring(Gw(c.x + 20f, c.z + 6f), "Mata Air");
    }

    static void Tent(Transform parent, Vector3 pos, float yaw, Material cloth, float size)
    {
        bool dark = cloth == cloths[1];       // tenda goblin = kain gelap
        int slot = dark ? Sl.ClothB : Sl.Cloth;
        var proto = PropKit.Get(dark ? "HuntTentDark" : "HuntTentCamp", b => ForestKit.Tent(b, 1, slot));
        var go = PropKit.Place(proto, parent, "Tent", pos, yaw, size / 3.4f, ForestKit.Slots);
        var bc = go.AddComponent<BoxCollider>();
        bc.center = new Vector3(0, 0.85f, 0);
        bc.size = new Vector3(2.0f, 1.7f, 3.0f);
    }

    static void MakeSpring(Vector3 pos, string label, bool ring = true)
    {
        clearings.Add(new Vector3(M * pos.x, M * pos.z, 4.5f));   // batu/pohon dekoratif tidak menumpuk di mata air
        var s = new GameObject("Spring").transform;
        s.SetParent(tProps, false);
        s.position = pos;
        if (ring)
        {
            // cincin batu menumpang di titik tanah tertinggi sekitar, dan kakinya menembus ke titik terendah (tidak melayang di lereng)
            float lo = pos.y, hi = pos.y;
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                float y = GroundYW(pos.x + Mathf.Cos(a) * 1.7f, pos.z + Mathf.Sin(a) * 1.7f);
                lo = Mathf.Min(lo, y); hi = Mathf.Max(hi, y);
            }
            float top = hi + 0.28f - pos.y;
            float bottom = lo - 0.2f - pos.y;
            float h = top - bottom;
            Prim(PrimitiveType.Cylinder, "Ring", s, new Vector3(0, (top + bottom) * 0.5f, 0), new Vector3(3.4f, h * 0.5f, 3.4f), mStone);
            Prim(PrimitiveType.Cylinder, "SpringWater", s, new Vector3(0, top - 0.04f, 0), new Vector3(2.8f, 0.03f, 2.8f), mWater, false);
        }
        var ws = s.gameObject.AddComponent<WaterSource>();
        ws.prompt = label;
        ws.radius = ring ? 4f : 5f;
    }

    // =====================================================================
    // Landmark & sarang
    // =====================================================================
    static void BuildLandmarks()
    {
        // Pohon Kuno raksasa di pusat hutan (akar papan, mahkota bercahaya)
        var gtp = PropKit.Get("HuntAncientTree", b => ForestKit.AncientTree(b, 5));
        var gt = PropKit.Place(gtp, tProps, "GiantTree", G(0, 0), 0f, 1f, ForestKit.Slots);
        var gcol = gt.AddComponent<CapsuleCollider>();
        gcol.radius = 3.2f; gcol.height = 22f; gcol.center = new Vector3(0, 11f, 0);
        var gl = new GameObject("AncientGlow").AddComponent<Light>();
        gl.transform.SetParent(gt.transform, false);
        gl.transform.localPosition = new Vector3(0, 19f, 0);
        gl.type = LightType.Point; gl.range = 38f; gl.intensity = 1.2f; gl.color = new Color(0.55f, 1f, 0.8f);

        // Menara reruntuhan di Hutan Gelap
        var rtp = PropKit.Get("HuntRuinTower", b => ForestKit.RuinTower(b, 7));
        var rt = PropKit.Place(rtp, tProps, "RuinedTower", G(-40, 300), 0f, 1f, ForestKit.Slots);
        rt.AddComponent<MeshCollider>().sharedMesh = rtp.mesh;

        // Lingkaran batu berukir di rawa
        var sc = new GameObject("StoneCircle").transform;
        sc.SetParent(tProps, false);
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            int seed = 30 + i;
            var st = PropKit.Place(PropKit.Get("HuntStone" + i, b => ForestKit.StandingStone(b, seed, 3f + (seed % 3) * 0.6f)), sc,
                "Pillar", G(-300 + Mathf.Sin(a) * 10f, 250 + Mathf.Cos(a) * 10f), -i * 45f + 180f, 1f, ForestKit.Slots);
            st.AddComponent<BoxCollider>().size = new Vector3(1.2f, 4f, 0.8f);
            ((BoxCollider)st.GetComponent<BoxCollider>()).center = new Vector3(0, 1.8f, 0);
        }
        var cc = PropKit.Place(PropKit.Get("HuntCrystal", b => ForestKit.CrystalCluster(b, 4)), sc, "Crystal", G(-300, 250), 0f, 1.8f, ForestKit.Slots);
        var cl = new GameObject("CrystalLight").AddComponent<Light>();
        cl.transform.SetParent(cc.transform, false);
        cl.transform.localPosition = new Vector3(0, 2.2f, 0);
        cl.type = LightType.Point; cl.range = 22f; cl.intensity = 1.6f; cl.color = new Color(0.4f, 0.9f, 1f);

        // Gerbang batu di bukit: dua batu besar dan satu penutup
        var arch = new GameObject("RockArch").transform;
        arch.SetParent(tProps, false);
        Vector3 ap = G(330, 150);
        var pl = PropKit.Place(PropKit.Get("HuntArchRock", b => ForestKit.Boulder(b, 41, 1f, true)), arch, "PillarL", ap + new Vector3(-9f, -1f, 0), 10f, 4.2f, ForestKit.Slots);
        var pr = PropKit.Place(PropKit.Get("HuntArchRock", b => ForestKit.Boulder(b, 41, 1f, true)), arch, "PillarR", ap + new Vector3(9f, -1f, 0), 200f, 4.2f, ForestKit.Slots);
        var top = PropKit.Place(PropKit.Get("HuntArchRock", b => ForestKit.Boulder(b, 41, 1f, true)), arch, "Top", ap + new Vector3(0, 9.5f, 0), 90f, 3.4f, ForestKit.Slots);
        top.transform.localScale = new Vector3(6.2f, 2.0f, 3.2f);
        foreach (var t in new[] { pl, pr, top }) t.AddComponent<MeshCollider>().sharedMesh = t.GetComponent<MeshFilter>().sharedMesh;

        // Perkemahan goblin: tenda gelap, totem tengkorak, pagar kayu runcing
        var gc = new GameObject("GoblinCamp").transform;
        gc.SetParent(tProps, false);
        Vector3 g0 = G(GoblinCamp.x, GoblinCamp.y);
        gc.position = g0;
        for (int i = 0; i < 5; i++)
        {
            float a = (i * 72f + 20f) * Mathf.Deg2Rad;
            Tent(gc, Gw(g0.x + Mathf.Sin(a) * 16f, g0.z + Mathf.Cos(a) * 16f), i * 72f + 20f, cloths[1], 4f);
        }
        Prim(PrimitiveType.Cylinder, "Totem", gc, new Vector3(0, 3.5f, 0), new Vector3(1f, 3.5f, 1f), mWood);
        Prim(PrimitiveType.Cube, "Skull", gc, new Vector3(0, 7.4f, 0), new Vector3(1.4f, 1.2f, 1.4f), mWhite, false);
        KitAtWorld("HuntCampfireGob", b => ForestKit.Campfire(b, 8), gc, Gw(g0.x + 5f, g0.z + 3f), 0f, 1.3f, false);
        for (int i = 0; i < 12; i++)
        {
            if (i == 3 || i == 4) continue;                                 // celah pintu masuk
            float a = (i * 30f) * Mathf.Deg2Rad;
            Vector3 p = Gw(g0.x + Mathf.Sin(a) * 28f, g0.z + Mathf.Cos(a) * 28f);
            KitAtWorld("HuntPalisade", b => ForestKit.Barricade(b, 6, 6.5f), gc, p, i * 30f + 90f, 1f, true);
        }
    }

    /// <summary>Menaruh mesh kit pada posisi dunia; yaw dalam derajat dunia.</summary>
    static GameObject KitAtWorld(string key, System.Action<GeoBag> build, Transform parent, Vector3 pos, float worldYaw, float scale, bool collider)
    {
        var proto = PropKit.Get(key, build);
        var go = PropKit.Place(proto, parent, key, pos, worldYaw, scale, ForestKit.Slots);
        if (collider) go.AddComponent<MeshCollider>().sharedMesh = proto.mesh;
        return go;
    }

    static void BuildDens()
    {
        DenRocks(WolfDen, 6);
        DenRocks(BoarDen, 6);
        DenRocks(DireDen, 8);
        // sarang babi: kubangan lumpur & jerami
        Vector3 b = G(BoarDen.x, BoarDen.y);
        Prim(PrimitiveType.Cylinder, "MudWallow", tProps, b + new Vector3(8f, 0.05f, 4f), new Vector3(9f, 0.05f, 7f), mMud, false);
        Prim(PrimitiveType.Cylinder, "HayNest", tProps, b + new Vector3(-6f, 0.4f, -4f), new Vector3(5f, 0.4f, 5f), mHay, false);
        // sarang serigala: tulang-belulang
        Vector3 w = G(WolfDen.x, WolfDen.y);
        for (int i = 0; i < 6; i++)
            Prim(PrimitiveType.Cube, "Bone", tProps, w + new Vector3(Random.Range(-6f, 6f), 0.15f, Random.Range(-6f, 6f)), new Vector3(0.2f, 0.2f, Random.Range(0.8f, 1.6f)), mWhite, false).transform.rotation = Quaternion.Euler(0, Random.Range(0f, 180f), 0);
    }

    static void DenRocks(Vector2 c, int n)
    {
        for (int i = 0; i < n; i++)
        {
            float a = i * (360f / n) * Mathf.Deg2Rad;
            float r = Random.Range(10f, 14f);
            Vector3 p = G(c.x + Mathf.Sin(a) * r, c.y + Mathf.Cos(a) * r);
            float s = Random.Range(2.5f, 5f);
            var rk = Prim(PrimitiveType.Sphere, "DenRock", tRocks, p + Vector3.up * s * 0.3f, new Vector3(s * 1.3f, s * 0.9f, s), mRock);
            rk.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        }
    }

    // =====================================================================
    // Gathering, jejak, sumber air
    // =====================================================================
    static Vector3? FindLand(float cx, float cz, float r, float maxSlope, bool inClearing = false)
    {
        for (int tries = 0; tries < 30; tries++)
        {
            Vector2 o = Random.insideUnitCircle * r;
            float x = cx + o.x, z = cz + o.y;
            if (Mathf.Abs(x) > 430f || Mathf.Abs(z) > 430f) continue;
            if (GroundY(x, z) < WaterY + 0.4f) continue;
            if (Slope(x, z) > maxSlope) continue;
            if (!inClearing && NearClearing(x, z)) continue;
            return G(x, z);
        }
        return null;
    }

    static void ScatterAll()
    {
        // zona: 1 padang, 2 hutan barat, 3 danau, 4 tengah, 5 bukit, 6 rawa, 7 gelap, 8 goblin
        Scatter("herb", 70, new[] { 1, 2, 3, 4, 7 });
        Scatter("mushroom", 35, new[] { 6, 7, 2 });
        Scatter("wild_berry", 35, new[] { 1, 3, 4 });

        // jejak untuk tiap monster besar (lokasi acak 25-110 m dari sarang)
        Tracks("alpha_wolf", WolfDen);
        Tracks("giant_boar", BoarDen);
        Tracks("dire_wolf", DireDen);

        // sumber air: tepi danau, tepi sungai, tepi rawa
        Vector3? lake = Shore(LakeC, V(-1, 0), 110f);
        if (lake.HasValue) MakeSpring(lake.Value, "Air Danau", false);
        Vector3? river = Shore(V(340, -196), V(0, 1), 60f);
        if (river.HasValue) MakeSpring(river.Value, "Air Sungai", false);
        Vector3? swamp = Shore(V(-300, 250), V(1, 0), 120f);
        if (swamp.HasValue) MakeSpring(swamp.Value, "Air Rawa", false);
    }

    static Vector3? Shore(Vector2 from, Vector2 dir, float maxR)
    {
        for (float r = 2f; r < maxR; r += 1f)
        {
            float x = from.x + dir.x * r, z = from.y + dir.y * r;
            if (GroundY(x, z) > WaterY + 0.5f && Slope(x, z) < 30f) return G(x, z);
        }
        return null;
    }

    static void Scatter(string itemId, int count, int[] zoneIdx)
    {
        int made = 0;
        for (int i = 0; i < count * 6 && made < count; i++)
        {
            var zd = Zones[zoneIdx[Random.Range(0, zoneIdx.Length)]];
            Vector3? p = FindLand(zd.x, zd.z, zd.r, 25f);
            if (!p.HasValue) continue;
            MakeNode(itemId, p.Value);
            made++;
        }
    }

    static void MakeNode(string itemId, Vector3 pos)
    {
        var n = new GameObject("Node_" + itemId);
        n.transform.SetParent(tNodes, false);
        n.transform.position = pos;
        var vis = new GameObject("Visual");
        vis.transform.SetParent(n.transform, false);
        Material green = Mat("HerbGreen", new Color(0.25f, 0.65f, 0.25f));
        Material red = Mat("BerryRed", new Color(0.75f, 0.1f, 0.15f));
        Material cap = Mat("MushCap", new Color(0.8f, 0.2f, 0.15f));
        string label = "Kumpulkan";
        if (itemId == "herb")
        {
            for (int i = 0; i < 4; i++)
                Prim(PrimitiveType.Sphere, "Leaf", vis.transform, new Vector3(Random.Range(-0.4f, 0.4f), 0.3f, Random.Range(-0.4f, 0.4f)), new Vector3(0.45f, 0.5f, 0.45f), green, false);
            Prim(PrimitiveType.Sphere, "Flower", vis.transform, new Vector3(0, 0.75f, 0), new Vector3(0.25f, 0.25f, 0.25f), mWhite, false);
            label = "Kumpulkan Herb";
        }
        else if (itemId == "mushroom")
        {
            Prim(PrimitiveType.Cylinder, "Stem", vis.transform, new Vector3(0, 0.25f, 0), new Vector3(0.25f, 0.25f, 0.25f), mWhite, false);
            Prim(PrimitiveType.Sphere, "Cap", vis.transform, new Vector3(0, 0.55f, 0), new Vector3(0.7f, 0.35f, 0.7f), cap, false);
            label = "Kumpulkan Mushroom";
        }
        else if (itemId == "swamp_lotus")
        {
            Material lg = Mat("LotusLeaf", new Color(0.18f, 0.38f, 0.2f));
            Material lp = Mat("LotusPetal", new Color(0.7f, 0.5f, 0.9f));
            Prim(PrimitiveType.Cylinder, "Pad", vis.transform, new Vector3(0, 0.05f, 0), new Vector3(1.1f, 0.03f, 1.1f), lg, false);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                Prim(PrimitiveType.Sphere, "Petal", vis.transform, new Vector3(Mathf.Sin(a) * 0.14f, 0.28f, Mathf.Cos(a) * 0.14f), new Vector3(0.12f, 0.3f, 0.12f), lp, false);
            }
            Prim(PrimitiveType.Sphere, "Glow", vis.transform, new Vector3(0, 0.3f, 0), new Vector3(0.12f, 0.12f, 0.12f), mGlow, false);
            label = "Petik Teratai Rawa";
        }
        else if (itemId == "cactus_fruit")
        {
            Material cg = Mat("CactusGreen", new Color(0.3f, 0.5f, 0.28f));
            Prim(PrimitiveType.Sphere, "Pad", vis.transform, new Vector3(0, 0.4f, 0), new Vector3(0.8f, 0.7f, 0.25f), cg, false);
            Prim(PrimitiveType.Sphere, "Pad2", vis.transform, new Vector3(0.28f, 0.85f, 0), new Vector3(0.55f, 0.5f, 0.22f), cg, false);
            for (int i = 0; i < 4; i++)
                Prim(PrimitiveType.Sphere, "Fruit", vis.transform, new Vector3(-0.25f + i * 0.2f, 0.8f + (i % 2) * 0.2f, 0.15f), new Vector3(0.16f, 0.2f, 0.16f), red, false);
            label = "Petik Buah Kaktus";
        }
        else
        {
            Prim(PrimitiveType.Sphere, "Bush", vis.transform, new Vector3(0, 0.45f, 0), new Vector3(1f, 0.9f, 1f), mLeaf, false);
            for (int i = 0; i < 5; i++)
                Prim(PrimitiveType.Sphere, "Berry", vis.transform, new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(0.4f, 0.8f), Random.Range(-0.4f, 0.4f)), new Vector3(0.14f, 0.14f, 0.14f), red, false);
            label = "Kumpulkan Wild Berry";
        }
        var gn = n.AddComponent<GatherNode>();
        gn.itemId = itemId;
        gn.visual = vis;
        gn.prompt = label;
        gn.radius = 2.8f;
    }

    static void Tracks(string monsterId, Vector2 den)
    {
        int made = 0;
        for (int i = 0; i < 80 && made < 5; i++)
        {
            float a = Random.Range(0f, 6.28f);
            float r = Random.Range(25f, 110f);
            float x = den.x + Mathf.Sin(a) * r, z = den.y + Mathf.Cos(a) * r;
            if (Mathf.Abs(x) > 430f || Mathf.Abs(z) > 430f) continue;
            if (GroundY(x, z) < WaterY + 0.4f || Slope(x, z) > 25f) continue;

            var m = new GameObject("Track_" + monsterId);
            m.transform.SetParent(tNodes, false);
            m.transform.position = G(x, z);
            Material dark = Mat("TrackDark", new Color(0.15f, 0.1f, 0.07f));
            for (int k = 0; k < 4; k++)
                Prim(PrimitiveType.Cylinder, "Pad", m.transform, new Vector3((k % 2) * 0.7f - 0.3f, 0.04f, k * 0.5f), new Vector3(0.35f, 0.03f, 0.35f), dark, false);
            Prim(PrimitiveType.Cube, "Bone", m.transform, new Vector3(1.2f, 0.1f, 0.5f), new Vector3(0.15f, 0.12f, 0.9f), mWhite, false);
            var tm = m.AddComponent<TrackMark>();
            tm.monsterId = monsterId;
            tm.prompt = "Periksa jejak";
            tm.radius = 3f;
            made++;
        }
    }

    // =====================================================================
    // Monster
    // =====================================================================
    static void BuildMonsters()
    {
        var root = Group("Monsters");
        SpawnMonster(root, "alpha_wolf", WolfDen);
        SpawnMonster(root, "giant_boar", BoarDen);
        SpawnMonster(root, "dire_wolf", DireDen);

        SpawnGroup(root, "slime", 0, -320, 70, 8);
        SpawnGroup(root, "slime", -300, 250, 70, 6);
        SpawnGroup(root, "slime", 110, -110, 40, 4);
        // Perkemahan Goblin: ramai (2-3 goblin per tenda, kerumunan di api unggun dan totem, penjaga di luar pagar)
        for (int i = 0; i < 5; i++)
        {
            float a = (i * 72f + 20f) * Mathf.Deg2Rad;
            SpawnGroup(root, "goblin", GoblinCamp.x + Mathf.Sin(a) * 16f, GoblinCamp.y + Mathf.Cos(a) * 16f, 7, 3, true);
        }
        SpawnGroup(root, "goblin", GoblinCamp.x + 5f, GoblinCamp.y + 3f, 6, 5, true);
        SpawnGroup(root, "goblin", GoblinCamp.x, GoblinCamp.y, 5, 3, true);
        SpawnGroup(root, "goblin", GoblinCamp.x, GoblinCamp.y, 48, 8, true);
    }

    static void SpawnGroup(Transform parent, string id, float cx, float cz, float r, int n, bool inClearing = false)
    {
        int made = 0;
        for (int i = 0; i < n * 8 && made < n; i++)
        {
            Vector3? p = FindLand(cx, cz, r, 20f, inClearing);
            if (!p.HasValue) continue;
            SpawnMonsterAt(parent, id, p.Value);
            made++;
        }
    }

    static void SpawnMonster(Transform parent, string id, Vector2 designPos) { SpawnMonsterAt(parent, id, G(designPos.x, designPos.y)); }

    static void SpawnMonsterAt(Transform parent, string id, Vector3 worldPos)
    {
        var go = new GameObject("Monster_" + id);
        go.transform.SetParent(parent, false);
        go.transform.position = worldPos;
        go.AddComponent<Monster>().defId = id;
    }

    static void BuildRocks()
    {
        int made = 0;
        for (int i = 0; i < 3000 && made < 230; i++)
        {
            float x = Random.Range(-440f, 440f), z = Random.Range(-440f, 440f);
            if (GroundY(x, z) < WaterY + 0.2f || NearClearing(x, z)) continue;
            if (DistToTrails(x, z) < 3f) continue;
            float s = Random.Range(0.6f, 3.2f);
            int v = Random.Range(0, 4);
            var go = KitAtWorld("HuntBoulder" + v, b => ForestKit.Boulder(b, 50 + v * 7, 1f, v % 2 == 0), tRocks, G(x, z) + Vector3.down * 0.15f * s, Random.Range(0f, 360f), s, s > 1.3f);
            go.transform.rotation = Quaternion.Euler(Random.Range(-6f, 6f), go.transform.eulerAngles.y, Random.Range(-6f, 6f));
            made++;
        }
        // batang kayu tumbang, tunggul, jamur bercahaya, kristal kecil
        for (int i = 0; i < 2500 && made < 520; i++)
        {
            float x = Random.Range(-440f, 440f), z = Random.Range(-440f, 440f);
            if (GroundY(x, z) < WaterY + 0.4f || NearClearing(x, z) || DistToTrails(x, z) < 4f || Slope(x, z) > 24f) continue;
            float r = Random.value;
            var zd = ZoneAt(x, z);
            bool dark = zd != null && (zd.name == "Hutan Gelap" || zd.name == "Rawa Berkabut" || zd.name.StartsWith("Desa Marrow"));
            if (r < 0.40f) KitAtWorld("HuntLog" + (made % 3), b => ForestKit.FallenLog(b, 60 + made % 3), tRocks, G(x, z), Random.Range(0f, 360f), Random.Range(0.8f, 1.4f), false);
            else if (r < 0.75f) KitAtWorld("HuntStump" + (made % 3), b => ForestKit.Stump(b, 70 + made % 3), tRocks, G(x, z), Random.Range(0f, 360f), Random.Range(0.8f, 1.5f), true);
            else if (dark && r < 0.93f) KitAtWorld("HuntMush" + (made % 3), b => ForestKit.GlowMushrooms(b, 80 + made % 3), tRocks, G(x, z), Random.Range(0f, 360f), Random.Range(1f, 1.8f), false);
            else if (dark) KitAtWorld("HuntCrystalS" + (made % 2), b => ForestKit.CrystalCluster(b, 90 + made % 2), tRocks, G(x, z), Random.Range(0f, 360f), Random.Range(0.6f, 1.2f), false);
            else continue;
            made++;
        }
    }

    // =====================================================================
    // Pemain & manajer
    // =====================================================================
    static void BuildPlayerAndManagers()
    {
        Material playerMat = Mat("Player", new Color(0.2f, 0.4f, 0.85f));
        var pl = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        pl.name = "Player";
        pl.tag = "Player";
        Vector3 start = spawnPos;      // ditentukan BuildTownApproach (70 m dari gerbang kota)
        pl.transform.position = start + Vector3.up * 1.1f;
        pl.transform.rotation = Quaternion.Euler(0, 180f, 0);      // menghadap hutan (-Z)
        pl.GetComponent<Renderer>().sharedMaterial = playerMat;
        Object.DestroyImmediate(pl.GetComponent<CapsuleCollider>());
        var cc = pl.AddComponent<CharacterController>();
        cc.height = 2f; cc.radius = 0.4f; cc.center = Vector3.zero; cc.stepOffset = 0.5f; cc.slopeLimit = 50f;
        pl.AddComponent<PlayerController>();
        pl.AddComponent<HunterGuild.PlayerAvatar>();
        pl.AddComponent<PlayerCombat>();
        Prim(PrimitiveType.Cube, "FacingMarker", pl.transform, new Vector3(0, 0.5f, 0.45f), new Vector3(0.3f, 0.3f, 0.5f), mGold, false);

        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = start + new Vector3(0, 3, 6);
            cam.farClipPlane = 2000f;
            cam.gameObject.AddComponent<ThirdPersonCamera>().target = pl.transform;
        }

        new GameObject("GameManager").AddComponent<GameState>();
        new GameObject("TownUI").AddComponent<TownUI>();
        new GameObject("BackpackUI").AddComponent<BackpackUI>();
        new GameObject("SkillUI").AddComponent<SkillUI>();
        new GameObject("HuntHUD").AddComponent<HuntHUD>();

        var hm = new GameObject("HuntManager").AddComponent<HuntManager>();
        var zi = new ZoneInfo[Zones.Length];
        for (int i = 0; i < Zones.Length; i++)
            zi[i] = new ZoneInfo { name = Zones[i].name, center = new Vector3(M * Zones[i].x, 0, M * Zones[i].z), radius = Zones[i].r };
        hm.zones = zi;
        hm.townSpawn = townSpawnName;

        var sunGo = GameObject.Find("Directional Light");
        if (sunGo != null)
        {
            var dn = new GameObject("DayNight").AddComponent<TownDayNight>();
            dn.sun = sunGo.GetComponent<Light>();
        }
    }

    // =====================================================================
    // Pendekatan dari kota: latar Ashwatch 3 Tembok (kit yang sama dengan scene Town), jalan, titik spawn & zona kembali
    // =====================================================================
    static void BuildTownApproach()
    {

        // Gerbang Luar Selatan kota berada di z = -470 (koordinat desain), sama dengan z = -330 di scene Town.
        // Pusat kota = 330 m dari gerbang ke arah dalam kota (+Z dunia). Orientasi sama persis dengan scene Town (tanpa rotasi).
        Vector3 g = G(0f, -470f);
        Vector3 center = new Vector3(0f, g.y, g.z + 330f);
        BackdropKit.Build(tRoot, center);

        // jalan dari gerbang ke camp = jalur tanah di terrain (trail pertama, dicat di alphamap); tanpa mesh terpisah agar tidak melayang

        // titik spawn dari kota (70 m dari gerbang), titik respawn di camp, zona kembali ke kota (55 m dari gerbang)
        spawnPos = G(0f, -400f);
        var sp = new GameObject("SpawnFromTown").transform;
        sp.SetParent(tRoot, false);
        sp.position = spawnPos;
        sp.rotation = Quaternion.Euler(0, 180f, 0);
        var rsp = new GameObject("RespawnPoint").transform;
        rsp.SetParent(tRoot, false);
        rsp.position = G(Camp.x, Camp.y - 10f);
        var tr = new GameObject("ToTown").transform;
        tr.SetParent(tRoot, false);
        tr.position = G(0f, -415f);
        var at = tr.gameObject.AddComponent<AreaTransition>();
        at.mode = AreaTransition.Mode.ToTown;
        at.townSpawn = townSpawnName;
        at.size = new Vector3(30f, 14f, 4f);
    }

    // =====================================================================
    // Util
    // =====================================================================
    static void LoadMaterials()
    {
        mWood = Mat("Wood", new Color(0.35f, 0.22f, 0.12f));
        mDark = Mat("DarkWood", new Color(0.2f, 0.12f, 0.07f));
        mStone = Mat("Stone", new Color(0.55f, 0.55f, 0.55f));
        mWater = Mat("Water", new Color(0.15f, 0.42f, 0.6f));
        mGlow = Mat("Glow", new Color(1f, 0.55f, 0.15f));
        mWhite = Mat("White", new Color(0.92f, 0.92f, 0.9f));
        mLeaf = Mat("Leaf", new Color(0.15f, 0.4f, 0.15f));
        mRock = Mat("Rock", new Color(0.5f, 0.5f, 0.52f));
        mGold = Mat("Gold", new Color(0.9f, 0.75f, 0.25f));
        mHay = Mat("Hay", new Color(0.75f, 0.65f, 0.3f));
        mMud = Mat("Mud", new Color(0.28f, 0.2f, 0.12f));
        cloths = new[] { Mat("ClothRed", new Color(0.7f, 0.2f, 0.2f)), Mat("ClothGreen", new Color(0.3f, 0.45f, 0.25f)), Mat("ClothBlue", new Color(0.2f, 0.35f, 0.7f)) };
    }

    static Mesh MakePyramidMesh()
    {
        string path = MeshDir + "/Pyramid.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null) return existing;

        var v = new List<Vector3>();
        var t = new List<int>();
        Vector3[] c = { new Vector3(-0.5f, 0, 0.5f), new Vector3(0.5f, 0, 0.5f), new Vector3(0.5f, 0, -0.5f), new Vector3(-0.5f, 0, -0.5f) };
        Vector3 apex = new Vector3(0, 1, 0);
        for (int k = 0; k < 4; k++)
        {
            int i = v.Count;
            v.Add(c[k]); v.Add(c[(k + 1) % 4]); v.Add(apex);
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
        }
        int b = v.Count;
        v.Add(c[0]); v.Add(c[3]); v.Add(c[2]); v.Add(c[1]);
        t.Add(b); t.Add(b + 1); t.Add(b + 2);
        t.Add(b); t.Add(b + 2); t.Add(b + 3);

        var m = new Mesh();
        m.SetVertices(v);
        m.SetTriangles(t, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        m.name = System.IO.Path.GetFileNameWithoutExtension(path); AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static Transform Group(string name)
    {
        var t = new GameObject(name).transform;
        t.SetParent(tRoot, false);
        return t;
    }

    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material m, bool collider = true)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static GameObject MeshObj(Transform parent, string name, Mesh mesh, Vector3 localPos, Vector3 scale, Material m)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = m;
        return go;
    }

    static void MarkStatic(Transform group)
    {
        foreach (var t in group.GetComponentsInChildren<Transform>(true))
            if (t.GetComponent<MeshRenderer>() != null)
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
    }

    static Material Mat(string name, Color c)
    {
        string path = MatDir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) { m.color = c; m.enableInstancing = true; return m; }

        Shader sh = GraphicsSettings.currentRenderPipeline != null
            ? Shader.Find("Universal Render Pipeline/Lit")
            : Shader.Find("Standard");
        if (sh == null) sh = Shader.Find("Standard");
        m = new Material(sh);
        m.color = c;
        m.enableInstancing = true;
        m.name = System.IO.Path.GetFileNameWithoutExtension(path); AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int i = path.LastIndexOf('/');
        EnsureFolder(path.Substring(0, i));
        AssetDatabase.CreateFolder(path.Substring(0, i), path.Substring(i + 1));
    }

    static void AddToBuildSettings(string path, bool onlyIfExists)
    {
        if (onlyIfExists && AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) return;
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in list) if (s.path == path) return;
        list.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
