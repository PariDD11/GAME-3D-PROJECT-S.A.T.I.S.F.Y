using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Desa Pemula (titik awal Level 0) dan Hutan Pemula (area tutorial sampai Level 5).
/// Desa dikelilingi pagar kayu glondongan ala anime dengan dua jalan keluar: UTARA ke Hutan Pemula, TIMUR ke Ashwatch (Level 5+).
/// Scene: Assets/Scenes/StartVillage.unity dan Assets/Scenes/HuntArea_Tutorial.unity. Memakai ulang kit Town (rumah, NPC, prop).
/// </summary>
public static partial class TownSceneBuilder
{
    const string VillageScenePath = "Assets/Scenes/StartVillage.unity";
    const string TutorialScenePath = "Assets/Scenes/HuntArea_Tutorial.unity";

    const float VR = 100f;
    const float VS = 1.0f;                                                        // skala tata letak dalam desa
    const float VEnd = 130f;                                                      // ujung jalan (lengkung gerbang di VEnd-6)
    static Vector2 S(float x, float z) { return new Vector2(x * VS, z * VS); }                                                         // jarak tengah pagar ke pusat desa
    static readonly float VRv = VR / Mathf.Cos(11.25f * Mathf.Deg2Rad);          // jari-jari titik sudut segi-16
    static readonly Color VGold = new Color(1f, 0.82f, 0.35f);

    // =====================================================================
    // Menu
    // =====================================================================
    [MenuItem("Hunter Guild/Build Desa Pemula (Start Village)")]
    public static void BuildVillage()
    {
        V2BuildVillage();            // v35z102n73: Desa Pemula "Kampung Tepi Kali" (denah, medan, bangunan, dan prop baru). Denah lama: menu Legacy di bawah.
    }

    [MenuItem("Hunter Guild/Legacy/Build Desa Pemula LAMA (denah n72)")]
    public static void BuildVillageLegacy()
    {
        if (!VConfirm(VillageScenePath)) return;
        BuildingMover.CaptureBeforeRebuild(VillageScenePath); complexScene = null;
        vMerged = true;                      // v35z102n36: Desa + Hutan Pemula = SATU scene (hutan disambung di ujung jalan utara)
        EditorUtility.DisplayProgressBar("Hunter Guild", "Desa Pemula: bahan & tanah...", 0.05f);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        VInit("DesaPemula");
        vMap.Clear();
        VillageTerrain();
        EditorUtility.DisplayProgressBar("Hunter Guild", "Pagar glondongan & gerbang...", 0.2f);
        VPalisade();
        VGatesAndRoads();
        EditorUtility.DisplayProgressBar("Hunter Guild", "Rumah & fasilitas...", 0.45f);
        VTrainingYard();
        VFarmAndPens();
        VBuildings();
        VLamps();
        VMedievalExtras();
        VBuildMapBoard();
        EditorUtility.DisplayProgressBar("Hunter Guild", "Pohon, NPC, pemain...", 0.7f);
        VVillageTrees();
        VBeautify();
        VWow();
        VVillageNpcs();
        // VVillageForestBackdrop();   // dimatikan: bukit hijau polos di balik gerbang utara tidak dipakai lagi
        VSpawns();
        VNorthRoadTrees();
        EditorUtility.DisplayProgressBar("Hunter Guild", "Hutan Pemula (disambung ke jalan utara)...", 0.8f);
        VMergeForest();
        VPlayer(new Vector3(0f, 0f, -30f), 0f, false);
        VFinishPre(0.0011f, new Color(0.66f, 0.78f, 0.88f));
        BuildingMover.Prepare("StartVillage", new[] { tGates, tBuildings, tHouses, tNoble }, new[] { tProps, tBuildings, tHouses, tNoble });
        HeightBake.BackupScene(VillageScenePath); EditorSceneManager.SaveScene(scene, VillageScenePath);
        vStreetsActive = false;
        vMerged = false;
        VFinishPost(VillageScenePath,
            "Desa Pemula + Hutan Pemula dibuat sebagai SATU scene: " + VillageScenePath + "\n" + houseCount + " rumah, " + npcCount + " NPC.\nJalan utara desa langsung menyambung ke hutan tanpa loading.");
    }

    [MenuItem("Hunter Guild/Build Hutan Pemula (Tutorial Hunt)")]
    public static void BuildTutorialForest()
    {
        // v35z102n36: Hutan Pemula kini bagian dari scene StartVillage (dibangun oleh "Build Desa Pemula").
        Debug.Log("Hutan Pemula sudah digabung ke scene StartVillage. Jalankan 'Build Desa Pemula (Start Village)' untuk membangun keduanya.");
        HGDlg.Show("Hunter Guild", "Hutan Pemula sekarang menyatu dengan Desa Pemula dalam satu scene.\nCukup jalankan 'Build Desa Pemula (Start Village)'.", "OK");
    }

    // =====================================================================
    // Penggabungan Hutan Pemula ke scene Desa Pemula
    // =====================================================================
    static bool vMerged;
    const float VSeamZ = 240f;                         // tepi utara terrain desa (half = 240)
    const float VForestHalf = 170f;                    // setengah lebar terrain hutan
    const float VForestZ0 = VSeamZ + VForestHalf;      // geser hutan: tepi selatannya tepat bertemu tepi utara desa
    static System.Func<float, float, float> vHVillage;

    /// <summary>Pohon & batu di kiri-kanan jalan utara antara gerbang desa dan hutan (jalan itu kini panjang menembus celah gunung).</summary>
    static void VNorthRoadTrees()
    {
        var g = Group("PohonJalanUtara", tProps);
        int placed = 0;
        for (int i = 0; i < 6000 && placed < 170; i++)
        {
            float x = Random.Range(-90f, 90f), z = Random.Range(VEnd + 12f, VSeamZ - 6f);
            float y = VGy(x, z);
            if (y < 0.6f || VSlopeDeg(x, z) > 34f) continue;
            if (Mathf.Abs(x) < 9f) continue;
            if (!IsFree(x, z, 3.2f)) continue;
            Reserve(x, z, 3.2f);
            VPlaceTree(g, x, z, 1f);
            placed++;
        }
    }

    /// <summary>Bangun Hutan Pemula (koordinat aslinya) di bawah akar baru lalu geser ke utara desa.</summary>
    static void VMergeForest()
    {
        // simpan grup desa
        var kTown = tTown; var kEnv = tEnv; var kWalls = tWalls; var kGates = tGates; var kRoads = tRoads; var kBld = tBuildings;
        var kHouses = tHouses; var kNoble = tNoble; var kProps = tProps; var kNpcs = tNpcs; var kWp = tWp;
        vHVillage = vH;
        grid.Clear(); solidBoxes.Clear(); streets.Clear();
        Random.InitState(20261020);

        tTown = new GameObject("HutanPemula").transform;
        tEnv = Group("Environment", tTown); tWalls = Group("Palisade", tTown); tGates = Group("Gates", tTown); tRoads = Group("Roads", tTown);
        tBuildings = Group("Buildings", tTown); tHouses = Group("Houses", tTown); tNoble = Group("Landmarks", tTown);
        tProps = Group("Props", tTown); tNpcs = Group("NPCs", tTown); tWp = Group("NPC_Waypoints", tTown);

        ForestTerrain();
        VForestTrails();
        VForestCamp();
        VForestTrees();
        VForestMonsters();
        VForestHerbs();
        VForestSpawns();
        var hm = VForestManager();
        var forestTerrain = vTerrain;

        MarkStatic(tEnv); MarkStatic(tWalls); MarkStatic(tGates); MarkStatic(tRoads); MarkStatic(tBuildings);
        MarkStatic(tHouses); MarkStatic(tNoble); MarkStatic(tProps);
        tTown.position = new Vector3(0f, 0f, VForestZ0);

        // pengendali: mode berburu aktif otomatis begitu pemain melewati lengkung gerbang hutan
        var ctl = new GameObject("SeamlessHunt").AddComponent<SeamlessHunt>();
        ctl.hunt = hm;
        ctl.gateZ = VForestZ0 - 122f;
        ctl.campRespawn = tTown.Find("RespawnPoint_Hutan") != null ? tTown.Find("RespawnPoint_Hutan").position : new Vector3(0f, 0f, VForestZ0 - 86f);
        var vr = GameObject.Find("RespawnPoint");
        ctl.villageRespawn = vr != null ? vr.transform.position : new Vector3(0f, 0f, -20f);
        hm.localReturn = GameObject.Find("SpawnFromForest") != null ? GameObject.Find("SpawnFromForest").transform : null;
        hm.gameObject.SetActive(false);

        // kembalikan grup desa (dipakai VFinishPre)
        tTown = kTown; tEnv = kEnv; tWalls = kWalls; tGates = kGates; tRoads = kRoads; tBuildings = kBld;
        tHouses = kHouses; tNoble = kNoble; tProps = kProps; tNpcs = kNpcs; tWp = kWp;
    }

    // =====================================================================
    // Util umum
    // =====================================================================
    static bool VConfirm(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
        {
            if (!HGDlg.Show("Hunter Guild", path + " sudah ada.\nMembangun ulang akan MENIMPA scene itu.\n\nLanjutkan?", "Timpa", "Batal")) return false;
        }
        return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
    }

    static void VInit(string rootName)
    {
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Materials");
        EnsureFolder(MatDir);
        EnsureFolder("Assets/Meshes");
        EnsureFolder(MeshDir);
        Random.InitState(20261019);
        grid.Clear();
        solidBoxes.Clear();
        streets.Clear();
        houseCount = 0;
        npcCount = 0;
        TownMats.Load();
        NpcKit.Reset();
        GateKit.Reset();
        PropKit.Reset();
        ForestKit.Reset();
        ForestKit.Load();
        VLoadTreeProtos();
        vH = null;
        HouseKit.BuildAll();
        baseSlots = TownMats.Slots();
        mMarker = UMat("VMarker", new Color(1f, 0.85f, 0.3f));

        tTown = new GameObject(rootName).transform;
        tEnv = Group("Environment", tTown);
        tWalls = Group("Palisade", tTown);
        tGates = Group("Gates", tTown);
        tRoads = Group("Roads", tTown);
        tBuildings = Group("Buildings", tTown);
        tHouses = Group("Houses", tTown);
        tNoble = Group("Landmarks", tTown);
        tProps = Group("Props", tTown);
        tNpcs = Group("NPCs", tTown);
        tWp = Group("NPC_Waypoints", tTown);
    }

    static void VFinishPre(float fogDensity, Color fog)
    {
        MarkStatic(tEnv); MarkStatic(tWalls); MarkStatic(tGates); MarkStatic(tRoads); MarkStatic(tBuildings);
        MarkStatic(tHouses); MarkStatic(tNoble); MarkStatic(tProps);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.fogColor = fog;
        EditorUtility.DisplayProgressBar("Hunter Guild", "Menyimpan scene...", 0.96f);
        AssetDatabase.SaveAssets();
    }

    static void VFinishPost(string path, string msg)
    {
        VAddSceneLast(path);
        EditorUtility.ClearProgressBar();
        HGDlg.Show("Hunter Guild", msg, "OK");
    }

    /// <summary>Tambahkan scene di AKHIR daftar Build Settings (Town tetap index 0 = scene awal / menu).</summary>
    static void VAddSceneLast(string path)
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in list) if (s.path == path) return;
        list.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    static Vector3 VPol(float azDeg, float r)
    {
        float a = azDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(a) * r, 0f, Mathf.Cos(a) * r);
    }

    static float VYawOf(Vector3 dir) { return Mathf.Atan2(-dir.z, dir.x) * Mathf.Rad2Deg; }     // yaw agar sumbu X lokal sejajar dir

    static void VGround(float half, string tag)
    {
        var b = new GeoBag();
        b.Patch(Sl.Grass, V(-half, -half), V(-half, half), V(half, half), V(half, -half), 0f, V(1f, 0f));
        b.Emit(tEnv, "Ground", baseSlots, MeshDir + "/" + tag + "_Ground.asset");
        var col = new GameObject("GroundCollider");
        col.transform.SetParent(tEnv, false);
        col.transform.position = new Vector3(0f, -0.5f, 0f);
        col.AddComponent<BoxCollider>().size = new Vector3(half * 2f + 40f, 1f, half * 2f + 40f);
    }

    static void VBoundary(float half, bool skipN = false, bool skipS = false)
    {
        var g = Group("Boundary", tEnv);
        if (!skipN) BoxCol(g, "N", new Vector3(0f, 20f, half), new Vector3(half * 2f, 60f, 2f), Vector3.zero);
        if (!skipS) BoxCol(g, "S", new Vector3(0f, 20f, -half), new Vector3(half * 2f, 60f, 2f), Vector3.zero);
        BoxCol(g, "E", new Vector3(half, 20f, 0f), new Vector3(2f, 60f, half * 2f), Vector3.zero);
        BoxCol(g, "W", new Vector3(-half, 20f, 0f), new Vector3(2f, 60f, half * 2f), Vector3.zero);
    }

    static T VPoint<T>(Transform parent, string name, Vector3 pos, string prompt, float radius = 3.4f, bool marker = true) where T : Interactable
    {
        var ip = new GameObject(name).transform;
        ip.SetParent(parent, false);
        ip.position = pos;
        var c = ip.gameObject.AddComponent<T>();
        c.prompt = prompt;
        c.radius = radius;
        if (marker) Prim(PrimitiveType.Cylinder, "Marker", ip, new Vector3(0f, 0.16f, 0f), new Vector3(2.2f, 0.03f, 2.2f), mMarker, false);
        return c;
    }

    static T VAttach<T>(GameObject host, string prompt, float radius = 3.4f) where T : Interactable
    {
        var ip = new GameObject("InteractPoint").transform;
        ip.SetParent(host.transform, false);
        ip.localPosition = Vector3.zero;
        var c = ip.gameObject.AddComponent<T>();
        c.prompt = prompt;
        c.radius = radius;
        return c;
    }

    static GameObject VNpc(string name, string display, string title, string label, Vector3 pos, bool elite)
    {
        var go = MakeNpc(name, label, pos, null, elite);
        var actor = go.GetComponent<NpcActor>();
        if (actor != null) actor.displayName = display;
        var tm = go.GetComponentInChildren<TextMesh>();
        if (tm != null) tm.text = display + "\n<" + title + ">";
        var chat = go.GetComponent<NpcChat>();
        if (chat != null) Object.DestroyImmediate(chat);
        return go;
    }

    static Vector3 VRoadAt(float az, float d)
    {
        float st = VR + 8f;
        float off = d < st ? 0f : 6f * Mathf.Sin((d - st) / 22f) * Mathf.Min(1f, (d - st) / 16f) * Mathf.Clamp01((VEnd - 14f - d) / 18f);
        float a = az * Mathf.Deg2Rad;
        Vector3 o = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
        Vector3 t = new Vector3(Mathf.Cos(a), 0f, -Mathf.Sin(a));
        return o * d + t * off;
    }

    static float VDistToRoads(float x, float z)
    {
        float best = 9999f;
        for (int r = 0; r < 2; r++)
            for (float d = 0f; d <= VEnd + 10f; d += 4f)
            {
                Vector3 p = VRoadAt(r * 90f, d);
                float dd = Mathf.Sqrt((p.x - x) * (p.x - x) + (p.z - z) * (p.z - z));
                if (dd < best) best = dd;
            }
        if (vMerged && z > VEnd) best = Mathf.Min(best, Mathf.Abs(x));         // jalan utara lanjut lurus sampai hutan
        return best;
    }

    // =====================================================================
    // Pagar glondongan
    // =====================================================================
    static Vector3 VVert(int k)
    {
        float a = (11.25f + 22.5f * k) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(a) * VRv, 0f, Mathf.Cos(a) * VRv);
    }

    static void VPalisade()
    {
        var b = new GeoBag();
        var rng = new System.Random(5);
        for (int k = 0; k < 16; k++)
        {
            Vector3 a = VVert(k), c = VVert((k + 1) % 16);
            Vector3 mid = (a + c) / 2f;
            bool gate = (k == 15) || (k == 3);                // gerbang utara (az 0) dan timur (az 90)
            Vector3 dir = c - a;
            float len = dir.magnitude;
            dir /= len;
            Vector3 inward = -mid.normalized;
            float gap = gate ? 5.2f : 0f;
            int n = Mathf.RoundToInt(len / 0.6f);
            for (int i = 0; i <= n; i++)
            {
                Vector3 p = a + dir * (len * i / n);
                if (gate && Vector3.Distance(p, mid) < gap) continue;
                float h = 4.3f + (float)rng.NextDouble() * 0.9f;
                p += inward * ((float)rng.NextDouble() * 0.14f - 0.07f);
                VillageKit.Log(b, p, h, i);
            }
            // balok pengikat di sisi dalam, dua tingkat
            var segs = new List<Vector2>();
            if (gate)
            {
                segs.Add(new Vector2(0f, len / 2f - gap));
                segs.Add(new Vector2(len / 2f + gap, len));
            }
            else segs.Add(new Vector2(0f, len));
            foreach (var sg in segs)
            {
                Vector3 s0 = a + dir * sg.x + inward * 0.45f, s1 = a + dir * sg.y + inward * 0.45f;
                b.Beam(Sl.Wood, s0 + Vector3.up * 1.3f, s1 + Vector3.up * 1.3f, 0.22f, 0.3f);
                b.Beam(Sl.Wood, s0 + Vector3.up * 3.2f, s1 + Vector3.up * 3.2f, 0.22f, 0.3f);
                VillageKit.PalisadeSegment(b, s0, s1, k);
                Vector3 m = (s0 + s1) / 2f;
                float sl = (s1 - s0).magnitude;
                BoxCol(tWalls, "PalisadeCol", m + Vector3.up * 2.6f - inward * 0.45f, new Vector3(sl, 5.2f, 0.9f), new Vector3(0f, VYawOf(dir), 0f));
            }
        }
        // menara jaga di empat sudut
        int[] towers = { 2, 6, 10, 14 };
        foreach (int k in towers) VWatchtower(b, k);
        b.Emit(tWalls, "Palisade", baseSlots, MeshDir + "/Vil_Palisade.asset");
    }

    static void VWatchtower(GeoBag b, int k)
    {
        Vector3 v = VVert(k);
        Vector3 c = v - v.normalized * 1.6f;
        float yaw = VYawOf(new Vector3(-v.z, 0f, v.x).normalized);
        VillageKit.Watchtower(b, c);
        BoxCol(tWalls, "TowerCol", c + Vector3.up * 4f, new Vector3(4.2f, 8f, 4.2f), Vector3.zero);
        var lt = new GameObject("TowerLight").transform;
        lt.SetParent(tWalls, false);
        lt.position = c;
        AddLight(lt, new Vector3(0f, 8.6f, 2.0f), 22f, 1.6f);
    }

    // =====================================================================
    // Gerbang kayu & jalan
    // =====================================================================
    static void VGateFrame(GeoBag b, Vector3 c, float az, float halfOpen)
    {
        float a = az * Mathf.Deg2Rad, px = halfOpen + 0.9f;
        Vector3 tx = new Vector3(Mathf.Cos(a), 0f, -Mathf.Sin(a)), tz = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
        float[] g = new float[2];
        for (int k = 0; k < 2; k++)
        {
            Vector3 pc = c + tx * ((k == 0 ? -1f : 1f) * px);
            float lo = 99f;
            foreach (var o in new[] { Vector3.zero, tx * 1.15f, -tx * 1.15f, tz * 1.15f, -tz * 1.15f })
                lo = Mathf.Min(lo, VGy(pc.x + o.x, pc.z + o.z));
            g[k] = lo - c.y;
        }
        VillageKit.TimberGate(b, c, az, halfOpen, g[0], g[1]);
    }

    static void VSignBoth(Transform parent, Vector3 c, float az, string text, float size)
    {
        float a = az * Mathf.Deg2Rad;
        Vector3 o = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
        Vector3 pos = c + Vector3.up * 5.0f;
        BuildKit.Text(parent, text, pos - o * 0.12f, az, size, VGold);              // dibaca dari dalam (menghadap keluar)
        BuildKit.Text(parent, text, pos + o * 0.12f, az + 180f, size, VGold);       // dibaca dari luar
    }

    static readonly List<List<Vector2>> vStreets = new List<List<Vector2>>();

    static bool vStreetsActive;

    /// <summary>Apakah titik (pusat bangunan berjari-jari half) terlalu dekat dengan jalan desa.</summary>
    static bool VNearStreet(Vector2 p, float half)
    {
        for (int i = 0; i < vStreets.Count; i++)
        {
            float need = (i < 2 ? 4.6f : 3.7f) + half;
            if (VDistPoly(p.x, p.y, vStreets[i]) < need) return true;
            var st = vStreets[i];
            if (st.Count >= 2)
            {
                Vector2 e = st[st.Count - 1], tn = (e - st[st.Count - 2]).normalized;
                for (float d = 3.5f; d <= 14f; d += 3.5f)
                    if (Vector2.Distance(p, e + tn * d) < 6.5f + half) return true;
            }
        }
        return false;
    }

    static void VDefineStreets()
    {
        vStreetsActive = true;
        vStreets.Clear();
        // denah teratur & simetris: sumbu utara-selatan dan barat-timur lurus, lorong selatan & barat lurus,
        // dua lorong diagonal ke pasar (kembar kiri-kanan), dua busur jalan lingkar kembar (r = 68)
        vStreets.Add(VSmoothPoly(new[] { V(0f, VR), V(0f, 60f), V(0f, 30f), V(0f, 15f) }, 6));
        vStreets.Add(VSmoothPoly(new[] { V(VR, 0f), V(60f, 0f), V(30f, 0f), V(15f, 0f) }, 6));
        // lorong selatan ke kompleks pelatihan
        vStreets.Add(VSmoothPoly(new[] { V(0f, -15f), V(0f, -35f), V(0f, -62f) }, 6));
        // lorong barat ke gereja
        vStreets.Add(VSmoothPoly(new[] { V(-15f, 0f), V(-35f, 0f), V(-62f, 0f) }, 6));
        vStreets.Add(VSmoothPoly(new[] { V(-48f, 0f), V(-48f, -18f), V(-48f, -36f) }, 6));
        // jalur ke kompleks pelatihan Hunter
        vStreets.Add(VSmoothPoly(new[] { V(0f, -62f), V(-22f, -62f), V(-44f, -62f) }, 6));
        // lorong diagonal pasar timur laut & barat laut (cermin)
        vStreets.Add(VSmoothPoly(new[] { V(9f, 9f), V(28f, 28f), V(48f, 48f) }, 6));
        vStreets.Add(VSmoothPoly(new[] { V(-9f, 9f), V(-28f, 28f), V(-48f, 48f) }, 6));
        // jalan lingkar: dua busur kembar di utara (lingkaran sempurna r = 68)
        var ringE = new List<Vector2>();
        for (float az = 20f; az <= 70f; az += 10f) { var q = VPol(az, 68f); ringE.Add(new Vector2(q.x, q.z)); }
        vStreets.Add(VSmoothPoly(ringE.ToArray(), 6));
        var ringW = new List<Vector2>();
        for (float az = 340f; az >= 290f; az -= 10f) { var q = VPol(az, 68f); ringW.Add(new Vector2(q.x, q.z)); }
        vStreets.Add(VSmoothPoly(ringW.ToArray(), 6));
    }

    /// <summary>Satu gerbang kayu sebagai objek mandiri "Gerbang_*" (bisa digeser/diputar manual). Mesh dibuat di koordinat dunia lalu dipindah jadi anak.</summary>
    static Transform VOneGate(string name, Vector3 c, float az, float halfOpen, string sign, float signSize, string asset)
    {
        int n0 = tGates.childCount;
        var gb = new GeoBag();
        VGateFrame(gb, c, az, halfOpen);
        gb.Emit(tGates, name + "_Kayu", baseSlots, MeshDir + "/" + asset + ".asset");
        VSignBoth(tGates, c, az, sign, signSize);
        // tiang kiri-kanan sebagai penghalang fisik (mesh gerbang sendiri tanpa collider)
        float px = halfOpen + 0.9f, a = az * Mathf.Deg2Rad;
        Vector3 tx = new Vector3(Mathf.Cos(a), 0f, -Mathf.Sin(a));
        foreach (float sg in new[] { -1f, 1f })
            BoxCol(tGates, "TiangCol", c + tx * (sg * px) + Vector3.up * 3f, new Vector3(1.5f, 6f, 1.5f), new Vector3(0f, az, 0f));
        var kids = new List<Transform>();
        for (int i = n0; i < tGates.childCount; i++) kids.Add(tGates.GetChild(i));
        var root = new GameObject(name).transform;
        root.SetParent(tGates, false);
        root.position = c;
        root.rotation = Quaternion.Euler(0f, az, 0f);
        foreach (var k in kids) k.SetParent(root, true);
        root.gameObject.AddComponent<MovableBuilding>();
        return root;
    }

    static void VGatesAndRoads()
    {
        // gerbang utama pagar
        Vector3 gN = new Vector3(0f, 0f, VR), gE = new Vector3(VR, 0f, 0f);
        Vector3 aN = VRoadAt(0f, VEnd - 6f), aE = VRoadAt(90f, VEnd - 6f);
        // tiap gerbang = satu objek sendiri (pivot di tengah gerbang, +Z = arah keluar) supaya bisa digeser manual
        var gateN = VOneGate("Gerbang_Utara", gN, 0f, 5.2f, "GERBANG UTARA\nHutan Pemula (Lv 0-5)", 0.12f, "Vil_GateN");
        var gateE = VOneGate("Gerbang_Timur", gE, 90f, 5.2f, "GERBANG TIMUR\nAshwatch (Lv 5+)", 0.12f, "Vil_GateE");
        VOneGate("Gerbang_HutanPemula", aN, 0f, 4.2f, "HUTAN PEMULA\nLatihan s/d Lv 5", 0.11f, "Vil_GateAN");
        VOneGate("Gerbang_Ashwatch", aE, 90f, 4.2f, "ASHWATCH\nHunter Lv 5+", 0.11f, "Vil_GateAE");

        var lgObj = BoxCol(tGates, "LevelGate_Timur", gE + Vector3.up * 4f, new Vector3(11.2f, 8f, 0.9f), new Vector3(0f, 90f, 0f));
        var lg = lgObj.AddComponent<LevelGate>();
        lg.minLevel = StarterConfig.CityMinLevel;
        lg.gateName = "Gerbang Timur";
        lg.blocker = lgObj.GetComponent<BoxCollider>();
        lg.warnRadius = 12f;
        lgObj.transform.SetParent(gateE, true);

        VGuard("Penjaga Utara", gN + new Vector3(-4.2f, 0f, -2.5f));
        VGuard("Penjaga Utara", gN + new Vector3(4.2f, 0f, -2.5f));
        VGuard("Penjaga Timur", gE + new Vector3(-2.5f, 0f, 4.2f));
        VGuard("Penjaga Timur", gE + new Vector3(-2.5f, 0f, -4.2f));

        // jalan tanah di luar gerbang (di dalam desa jalannya berbatu)
        var rd = new GeoBag();
        for (int r = 0; r < 2; r++)
        {
            var pts = new List<Vector2>();
            for (float d = VR - 2f; d <= VR + 12f; d += 4f) { Vector3 p = VRoadAt(r * 90f, d); pts.Add(new Vector2(p.x, p.z)); }
            rd.Ribbon(Sl.Dirt, pts, 6.5f, 0.03f);
        }
        rd.Emit(tRoads, "JalanLuar", baseSlots, MeshDir + "/Vil_Roads.asset");

        // jalan batu bulat berkelok di dalam desa (tepi tanah + permukaan batu)
        VDefineStreets();
        var edge = new GeoBag(); var stone = new GeoBag();
        for (int i = 0; i < vStreets.Count; i++)
        {
            float w = i < 2 ? 5.4f : 3.8f;
            edge.Ribbon(Sl.Dirt, vStreets[i], w + 1.6f, 0.025f);
            stone.Ribbon(Sl.Cobble, vStreets[i], w, 0.045f);
        }
        edge.Emit(tRoads, "TepiJalan", baseSlots, MeshDir + "/Vil_StreetEdge.asset");
        stone.Emit(tRoads, "JalanBatu", baseSlots, MeshDir + "/Vil_Streets.asset");

        VPlazaAndFountain();           // alun-alun radial + air mancur bertingkat (TownSceneBuilder.VillagePlaza.cs)
    }

    static void VGuard(string display, Vector3 pos)
    {
        var go = VNpc("NPC_Penjaga", display, "Penjaga Desa", "Penjaga", pos, true);
        Quaternion q = go.transform.rotation;
        go.transform.rotation = q;
    }

    // =====================================================================
    // Bangunan
    // =====================================================================
    static HouseKit.HouseDef VDef(int kind, int i)
    {
        var l = HouseKit.Defs.FindAll(d => d.kind == kind);
        if (l.Count == 0) return HouseKit.Defs[0];
        return l[i % l.Count];
    }

    /// <summary>Pasang rumah menghadap titik look; kembalikan titik tepat di depan pintunya.</summary>
    static Vector2 VHouse(HouseKit.HouseDef def, Vector2 pos, Vector2 look, string name, int variant, out float yaw)
    {
        yaw = YawToward(pos, look);
        var slots = TownMats.Slots(TownMats.Plaster[variant % TownMats.Plaster.Length], TownMats.Roofs[(variant + 1) % TownMats.Roofs.Length]);
        if (def.kind == 3 || def.kind == 5) slots[Sl.Roof] = TownMats.Thatch;
        var go = PropKit.Place(def.proto, tHouses, name, V3(pos), yaw, 1f, slots);
        var bc = go.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, def.height / 2f, 0f);
        bc.size = new Vector3(def.w, def.height, def.d);
        ReserveBox(pos.x, pos.y, def.w + 1.5f, def.d + 1.5f, yaw, 0f);
        Reserve(pos.x, pos.y, Mathf.Max(def.w, def.d) / 2f + 1f);
        houseCount++;
        Vector2 fwd = new Vector2(Mathf.Sin(yaw * Mathf.Deg2Rad), Mathf.Cos(yaw * Mathf.Deg2Rad));
        return pos + fwd * (def.d / 2f + 1.6f);
    }

    static void VHouseSign(string text, Vector2 pos, float yaw, HouseKit.HouseDef def, float y)
    {
        var holder = new GameObject("Papan_" + text.Replace(" ", "_")).transform;
        holder.SetParent(tHouses, false);
        holder.position = V3(pos);
        holder.rotation = Quaternion.Euler(0f, yaw, 0f);
        BuildKit.Text(holder, text, new Vector3(0f, y, def.d / 2f + 0.25f), 180f, 0.16f, Color.white);
    }

    static void VBuildings()
    {
        float yaw;
        // papan pengumuman dekat titik mulai (7,-24): dipesan DULU supaya rumah tidak menimpanya
        ReserveBox(7f, -24f, 4f, 1.5f, 0f, 1.5f);
        Reserve(7f, -26f, 3.2f);
        VChurch();
        VCemetery();
        VExtraBoards();
        // rumah Kepala Desa (rumah panjang) menghadap alun-alun, dengan bendera
        var eld = VDef(5, 0);
        Vector2 ep = V(-25f, 17f);
        Vector2 ed = VHouse(eld, ep, V(0f, 0f), "RumahKepalaDesa", 1, out yaw);
        VHouseSign("VETERAN BRANN", ep, yaw, eld, eld.height * 0.55f);
        VMapMark("Rumah Veteran Brann", ep, 0);
        var elder = VNpc("NPC_KepalaDesa", "Veteran Brann", "Petualang Veteran", "Hunter", V3(ed) + Fwd(yaw) * 1.2f, false);
        VAttach<VillageElder>(elder, "Bicara: Veteran Brann", 3.6f);

        // penginapan dua lantai di timur alun-alun
        var inn = VDef(0, 2);
        Vector2 ip = V(27f, -15f);
        Vector2 id = VHouse(inn, ip, V(0f, 0f), "Penginapan", 2, out yaw);
        VHouseSign("PENGINAPAN", ip, yaw, inn, inn.height * 0.5f);
        VMapMark("Penginapan", ip, 0);
        VPoint<Inn>(tHouses, "InteractPoint_Inn", V3(id), "Penginapan Desa (tidur & simpan)", 3.6f);
        var deco = new GeoBag();
        BuildKit.Barrel(deco, V3(ip) + Fwd(yaw) * (inn.d / 2f + 0.8f) + Quaternion.Euler(0f, yaw, 0f) * new Vector3(3.2f, 0f, 0f));
        BuildKit.Crate(deco, V3(ip) + Fwd(yaw) * (inn.d / 2f + 0.8f) + Quaternion.Euler(0f, yaw, 0f) * new Vector3(-3.4f, 0f, 0f), 0.9f, yaw);

        // pondok tabib di lorong selatan
        var hut = VDef(3, 1);
        Vector2 hp = V(-15f, -27f);
        Vector2 hd = VHouse(hut, hp, V(0f, -27f), "PondokTabib", 3, out yaw);
        VHouseSign("TABIB", hp, yaw, hut, hut.height * 0.5f);
        VMapMark("Tabib Mira", hp, 0);
        var healer = VNpc("NPC_Tabib", "Tabib Mira", "Tabib Desa", "Peziarah", V3(hd) + Fwd(yaw) * 1.1f, false);
        VAttach<VillageHealer>(healer, "Bicara: Tabib Mira", 3.4f);

        PlacePropertyNpcs(VPol(305f, 12f), VPol(335f, 12f), StarterConfig.VillageScene);

        // tiga kios di tepi alun-alun
        float[] sa = { 45f, 135f, 225f, 315f };
        for (int i = 0; i < 4; i++)
        {
            Vector3 sp = VPol(sa[i], 11.5f);
            float sy = sa[i] + 180f;
            var sb = new GeoBag();
            PropKit.Stall(sb, sp, sy, i % 2 == 0 ? Sl.Cloth : Sl.ClothB, i % 2 == 0 ? Sl.ClothB : Sl.Cloth);
            sb.Emit(tProps, "Kios" + i, baseSlots, MeshDir + "/Vil_Stall" + i + ".asset");
            BoxCol(tProps, "KiosCol", sp + Vector3.up * 1f, new Vector3(3.4f, 2f, 2.2f), new Vector3(0f, sy, 0f));
            if (i == 0)
            {
                Vector3 sf = Fwd(sy);
                VPoint<GeneralShop>(tProps, "InteractPoint_Toko", sp + sf * 2.6f, "Toko Bram (perlengkapan dasar)", 3.4f);
                VMapMark("Toko Bram", sp, 1);
                VNpc("NPC_Pedagang", "Pedagang Bram", "Pedagang", "Pedagang", sp - sf * 1.4f, false);
            }
            else BuildKit.Crate(deco, sp + Fwd(sy) * 2f, 0.8f, sy);
        }
        deco.Emit(tProps, "DekorBarang", baseSlots, MeshDir + "/Vil_Deco.asset");

        // papan pengumuman dekat titik mulai
        var nb = new GeoBag();
        Vector3 bp = new Vector3(7f, 0f, -24f);
        nb.Cyl(Sl.Wood, bp + new Vector3(-1.1f, 0f, 0f), 0.14f, 2.8f, 6, 0.13f, true, false);
        nb.Cyl(Sl.Wood, bp + new Vector3(1.1f, 0f, 0f), 0.14f, 2.8f, 6, 0.13f, true, false);
        nb.Box(Sl.WoodLight, bp + new Vector3(0f, 1.9f, 0f), new Vector3(2.8f, 1.5f, 0.12f), 0f);
        nb.Box(Sl.Roof, bp + new Vector3(0f, 2.85f, 0f), new Vector3(3.2f, 0.12f, 0.8f), 0f);
        nb.Emit(tProps, "PapanDesa", baseSlots, MeshDir + "/Vil_Board.asset");
        BuildKit.Text(tProps, "DESA PEMULA\nUtara: Hutan Pemula\nTimur: Kota (Lv 5)", bp + new Vector3(0f, 1.9f, -0.1f), 0f, 0.075f, Color.white);
        BuildKit.Text(tProps, "DESA PEMULA\nUtara: Hutan Pemula\nTimur: Kota (Lv 5)", bp + new Vector3(0f, 1.9f, 0.1f), 180f, 0.075f, Color.white);
        BoxCol(tProps, "PapanCol", bp + Vector3.up * 1.4f, new Vector3(3f, 2.8f, 0.5f), Vector3.zero);
        var info = VPoint<InfoPoint>(tProps, "InteractPoint_Papan", bp + new Vector3(0f, 0f, -1.8f), "Baca papan pengumuman");
        info.title = "Papan Desa Pemula";
        info.message = "Selamat datang, calon Hunter!\n\n" +
            "1. Bicaralah dengan Veteran Brann (rumah panjang di barat laut alun-alun); dia membimbingmu dari Level 1 sampai 5 dan membantumu memilih job.\n" +
            "2. Latihan di boneka jerami (barat daya) bersama Pelatih Garr.\n" +
            "3. FASILITAS: Kantor Properti (rumah dijual: 3 rumah), Gereja di barat daya, Makam Desa di ujung jalan barat.\n" +
            "4. JALAN UTARA: Hutan Pemula, tempat berburu Slime & Goblin sampai Level 5.\n" +
            "5. JALAN TIMUR: Ashwatch. Gerbangnya baru terbuka untuk Hunter Level 5 ke atas.\n\n" +
            "Penginapan untuk tidur dan menyimpan, Tabib Mira menyembuhkan gratis.";

        // kincir angin dan gazebo
        Vector3 wp = new Vector3(46f, 0.04f, -38f);
        var wbody = PropKit.Place(PropKit.Get("V_WindmillBody", LandmarkKit.WindmillBody), tNoble, "KincirAngin", wp, 200f, 1f, baseSlots);
        var wb = PropKit.Place(PropKit.Get("V_WindmillBlades", LandmarkKit.WindmillBlades), wbody.transform, "BilahKincir", Vector3.zero, 0f, 1f, baseSlots);
        wb.transform.SetParent(wbody.transform, false);
        wb.transform.localPosition = new Vector3(0f, 15.5f, 4.5f);
        wb.transform.localRotation = Quaternion.identity;
        var spin = wb.AddComponent<SpinObject>();
        spin.axis = Vector3.forward; spin.degreesPerSecond = 20f;
        BoxCol(wbody.transform, "KincirCol", new Vector3(0f, 6f, 0f), new Vector3(7f, 12f, 7f), Vector3.zero);
        ReserveBox(wp.x, wp.z, 12f, 12f, 0f, 0f);
        VMapMark("Kincir Angin", wp, 4);
        Vector3 gz = new Vector3(40f, 0.04f, 30f);
        var gaz = PropKit.Place(PropKit.Get("V_Gazebo", LandmarkKit.Gazebo), tNoble, "Gazebo", gz, 0f, 0.8f, baseSlots);
        BoxCol(gaz.transform, "GazeboCol", new Vector3(0f, 1f, 0f), new Vector3(6f, 2f, 6f), Vector3.zero);
        ReserveBox(gz.x, gz.z, 9f, 9f, 0f, 0f);

        VStreetHouses();
    }

    /// <summary>Isi kedua sisi jalan batu dengan rumah warga yang menghadap jalan, lengkap dengan bedeng bunga.</summary>
    static void VStreetHouses()
    {
        var rng = new System.Random(11);
        // ujung jalan buntu dan sumbu lurus ke depannya harus bebas (tidak boleh ada rumah yang menutup jalan)
        foreach (var st0 in vStreets)
        {
            Vector2 e = st0[st0.Count - 1], pv = st0[st0.Count - 2];
            Vector2 tn = (e - pv).normalized;
            for (float d = 0f; d <= 14f; d += 3.5f) Reserve(e.x + tn.x * d, e.y + tn.y * d, 6.5f);
        }
        // Tata ruang rapi: rumah berderet teratur di kedua sisi jalan, jarak antar-rumah tetap, setback seragam,
        // semuanya menghadap tegak lurus ke jalan; jenis rumah berpola (pondok - rumah kota - toko - pondok ...), warna bergiliran.
        int[] kinds = { 3, 0, 3, 1, 3, 0, 5, 3 };
        int n = 0, slot = 0;
        for (int si = 0; si < vStreets.Count; si++)
        {
            var st = vStreets[si];
            var cum = new float[st.Count];
            for (int i = 1; i < st.Count; i++) cum[i] = cum[i - 1] + (st[i] - st[i - 1]).magnitude;
            float total = cum[st.Count - 1];
            for (float sPos = 9f; sPos < total - 5f; sPos += 13f)         // jarak antar-rumah tetap
            {
                int si2 = 0;
                while (si2 + 2 < st.Count && cum[si2 + 1] < sPos) si2++;
                Vector2 a = st[si2], b = st[si2 + 1];
                float segLen = (b - a).magnitude;
                float tt = segLen > 0.001f ? Mathf.Clamp01((sPos - cum[si2]) / segLen) : 0f;
                Vector2 tan = (b - a).normalized, nrm = new Vector2(tan.y, -tan.x);
                Vector2 mid = Vector2.Lerp(a, b, tt);
                if (mid.magnitude < 22f) continue;
                slot++;
                for (int side = -1; side <= 1; side += 2)
                {
                    var def = VDef(kinds[slot % kinds.Length], slot * 3);          // kedua sisi jalan kembar (simetris)
                    float rh = si < 2 ? 4.4f : 3.8f;
                    float off = rh + def.d / 2f + 3.4f;            // setback seragam + halaman depan
                    Vector2 pos = mid + nrm * side * off;
                    float rad = Mathf.Max(def.w, def.d) / 2f + 1.2f;
                    if (pos.magnitude > VR - 9f || pos.magnitude < 22f) continue;
                    if (!IsFree(pos.x, pos.y, rad)) continue;
                    float yaw0;
                    VHouse(def, pos, mid, "RumahWarga" + (++n), slot % 7, out yaw0);
                    Vector3 f = Fwd(yaw0);
                    Vector3 bc = V3(pos) + f * (def.d / 2f + 0.7f);
                    var garden = new GeoBag();           // taman + pagar tiap rumah = objek sendiri (ikut rumahnya saat digeser)
                    VFlowerBed(garden, bc, yaw0, Mathf.Min(def.w * 0.7f, 4.5f), rng);
                    // pagar kayu pendek rapi di sisi jalan + tiang lentera kecil di tepi halaman
                    VFrontFence(garden, V3(pos) + f * (off - rh - 0.9f), yaw0, def.w + 1.5f);
                    garden.Emit(tProps, "Taman_RumahWarga" + n, baseSlots, MeshDir + "/Vil_Garden" + n + ".asset");
                }
            }
        }
    }

    /// <summary>Pagar kayu rendah (tiang + dua palang) sepanjang len, melintang terhadap arah pandang yaw.</summary>
    static void VFrontFence(GeoBag b, Vector3 c, float yaw, float len)
    {
        Vector3 along = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
        int posts = Mathf.Max(4, Mathf.RoundToInt(len / 1.6f));
        foreach (int half in new[] { -1, 1 })           // dua bagian, celah 2.6 m di tengah untuk jalan masuk ke pintu
        {
            float a0 = 1.3f, a1 = len / 2f;
            if (a1 <= a0 + 0.4f) continue;
            for (int i = 0; i < posts / 2; i++)
            {
                float t = Mathf.Lerp(a0, a1, i / (float)Mathf.Max(1, posts / 2 - 1)) * half;
                b.Cyl(Sl.Wood, c + along * t, 0.07f, 0.85f, 6, 0.06f, true, false);
            }
            Vector3 mc = c + along * ((a0 + a1) / 2f * half);
            b.Box(Sl.WoodLight, mc + Vector3.up * 0.7f, new Vector3(a1 - a0, 0.06f, 0.07f), yaw);
            b.Box(Sl.WoodLight, mc + Vector3.up * 0.4f, new Vector3(a1 - a0, 0.06f, 0.07f), yaw);
        }
    }

    static void VFlowerBed(GeoBag b, Vector3 c, float yaw, float len, System.Random rng)
    {
        b.Box(Sl.Wood, c + Vector3.up * 0.18f, new Vector3(len, 0.36f, 0.6f), yaw);
        b.Box(Sl.Field, c + Vector3.up * 0.37f, new Vector3(len - 0.1f, 0.04f, 0.5f), yaw);
        Vector3 along = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
        int k = Mathf.Max(3, Mathf.RoundToInt(len / 0.45f));
        int[] cols = { Sl.Blossom, Sl.Cloth, Sl.ClothB, Sl.Gold };
        for (int i = 0; i < k; i++)
        {
            float t = (i / (float)(k - 1) - 0.5f) * (len - 0.4f);
            b.Ball(cols[rng.Next(cols.Length)], c + along * t + Vector3.up * (0.55f + (float)rng.NextDouble() * 0.1f), 0.2f + (float)rng.NextDouble() * 0.08f, 3, 6);
        }
    }

    static Vector3 Fwd(float yaw) { return new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), 0f, Mathf.Cos(yaw * Mathf.Deg2Rad)); }

    // =====================================================================
    // Lapangan latihan, ladang, kandang
    // =====================================================================
    static void VTrainingYard() { VTrainingComplex(); }

    static void VFarmAndPens()
    {
        // ladang kecil
        var fb = new GeoBag();
        Vector2 fc = V(22f, -50f);
        fb.Patch(Sl.Field, fc + V(-7f, -5f), fc + V(-7f, 5f), fc + V(0f, 5f), fc + V(0f, -5f), 0.03f, V(0f, 1f));
        fb.Patch(Sl.FieldB, fc + V(0f, -5f), fc + V(0f, 5f), fc + V(7f, 5f), fc + V(7f, -5f), 0.03f, V(0f, 1f));
        var fp = new List<Vector3>();
        fp.Add(new Vector3(fc.x - 8f, 0f, fc.y - 6f)); fp.Add(new Vector3(fc.x - 8f, 0f, fc.y + 6f));
        fp.Add(new Vector3(fc.x + 8f, 0f, fc.y + 6f)); fp.Add(new Vector3(fc.x + 8f, 0f, fc.y - 6f));
        fp.Add(new Vector3(fc.x - 8f, 0f, fc.y - 6f));
        PropKit.Fence(fb, fp, 1.1f);
        fb.Emit(tProps, "Ladang", baseSlots, MeshDir + "/Vil_Farm.asset");
        var sc = PropKit.Get("V_Scarecrow", PropKit.Scarecrow);
        PropKit.Place(sc, tProps, "Orang2an", V3(fc), 20f, 1f, baseSlots);
        var hs = PropKit.Get("V_Hay", bb => PropKit.Haystack(bb, Vector3.zero));
        var h1 = PropKit.Place(hs, tProps, "Jerami", V3(fc + V(11f, 2f)), 0f, 1f, baseSlots);
        h1.AddComponent<CapsuleCollider>().radius = 1.3f;
        PropKit.Place(hs, tProps, "Jerami", V3(fc + V(11f, -3f)), 40f, 1f, baseSlots).AddComponent<CapsuleCollider>().radius = 1.3f;
        ReserveBox(fc.x, fc.y, 18f, 14f, 0f, 0f);
        VMapMark("Ladang", fc, 4);
        for (int i = 0; i + 1 < fp.Count; i++)
        {
            Vector3 a = fp[i], d = fp[i + 1];
            BoxCol(tProps, "PagarCol", (a + d) / 2f + Vector3.up * 0.5f, new Vector3((d - a).magnitude, 1f, 0.3f), new Vector3(0f, VYawOf((d - a).normalized), 0f));
        }

        // kandang domba & sapi
        Vector3 pc = new Vector3(-40f, 0f, 38f);
        var pb = new GeoBag();
        var pp = new List<Vector3>();
        for (int i = 0; i < 17; i++)
        {
            float a = (i * 22.5f) * Mathf.Deg2Rad;
            pp.Add(pc + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * 6.5f);
        }
        PropKit.Fence(pb, pp, 1.2f);
        pb.Emit(tProps, "Kandang", baseSlots, MeshDir + "/Vil_Pen.asset");
        for (int i = 0; i + 1 < pp.Count; i++)
        {
            Vector3 a = pp[i], d = pp[i + 1];
            BoxCol(tProps, "PagarCol", (a + d) / 2f + Vector3.up * 0.6f, new Vector3((d - a).magnitude, 1.2f, 0.3f), new Vector3(0f, VYawOf((d - a).normalized), 0f));
        }
        var sheep = PropKit.Get("V_Sheep", PropKit.Sheep);
        var cow = PropKit.Get("V_Cow", PropKit.Cow);
        for (int i = 0; i < 4; i++)
            PropKit.Place(sheep, tProps, "Domba", pc + new Vector3(-3.4f + i * 2f, 0f, (i % 2) * 2.4f - 1.4f), i * 70f, 1f, baseSlots);
        for (int i = 0; i < 2; i++)
            PropKit.Place(cow, tProps, "Sapi", pc + new Vector3(-1.5f + i * 3f, 0f, 3.2f - i * 0.6f), 120f + i * 80f, 1f, baseSlots);
        ReserveBox(pc.x, pc.z, 15f, 15f, 0f, 0f);
        VMapMark("Kandang Ternak", pc, 4);
    }

    static void VBlueLight(Transform parent, Vector3 localPos)
    {
        var l = new GameObject("CahayaKristal").transform;
        l.SetParent(parent, false);
        l.localPosition = localPos;
        var lt = l.gameObject.AddComponent<Light>();
        lt.type = LightType.Point; lt.range = 15f; lt.intensity = 1.6f; lt.color = new Color(0.55f, 0.82f, 1f);
    }

    static void VLamps()
    {
        var lamp = PropKit.LampProto();
        var csl = VCrystalSlots();
        var clamp = PropKit.Get("D_CrystalLamp", DungeonKit.CrystalLamp);
        var g = Group("Lampu", tProps);
        int n = 0;
        for (int si = 0; si < vStreets.Count; si++)
        {
            var st = vStreets[si];
            float acc = 6f;
            int side = 1;
            for (int i = 0; i + 1 < st.Count; i++)
            {
                Vector2 a = st[i], b = st[i + 1];
                acc += (b - a).magnitude;
                if (acc < 15f) continue;
                acc = 0f;
                Vector2 tan = (b - a).normalized, nrm = new Vector2(tan.y, -tan.x);
                Vector2 p = (a + b) / 2f + nrm * side * (si < 2 ? 3.9f : 3.2f);
                side = -side;
                if (p.magnitude < 16f || !IsFree(p.x, p.y, 0.8f)) continue;
                if (Vector2.Distance(p, new Vector2(VR, 0f)) < 9f || Vector2.Distance(p, new Vector2(0f, VR)) < 9f) continue;      // jangan menutupi papan gerbang
                Reserve(p.x, p.y, 0.8f);
                bool cry = si < 2 || si == 7 || si == 8;
                var go = PropKit.Place(cry ? clamp : lamp, g, "Lampu", V3(p), 0f, 1f, cry ? csl : baseSlots);
                if ((n++ % 2) == 0) { if (cry) VBlueLight(go.transform, new Vector3(0f, 3.8f, 0f)); else AddLight(go.transform, new Vector3(0f, 3.6f, 0f), 15f, 1.5f); }
            }
        }
        for (int i = 0; i < 6; i++)
        {
            var go = PropKit.Place(clamp, g, "LampuAlun", VPol(i * 60f + 30f, 13.4f), 0f, 1f, csl);
            if ((i % 2) == 0) VBlueLight(go.transform, new Vector3(0f, 3.8f, 0f));
        }
    }

    // =====================================================================
    // Pohon, NPC, spawn
    // =====================================================================
    static void VVillageTrees()
    {
        var g = Group("Pohon", tProps);
        int placed = 0;
        for (int i = 0; i < 9000 && placed < 400; i++)
        {
            float x = Random.Range(-232f, 232f), z = Random.Range(-232f, 232f);
            float rr = Mathf.Sqrt(x * x + z * z);
            if (rr < VRv + 6f) continue;
            float y = VGy(x, z);
            if (y < 0.5f || VSlopeDeg(x, z) > 34f) continue;
            if (VDistToRoads(x, z) < 11f) continue;
            if (!IsFree(x, z, 3.2f)) continue;
            Reserve(x, z, 3.2f);
            VPlaceTree(g, x, z, 1f);
            placed++;
        }
        VScatterBoulders(Group("Batu", tProps), 60, 232f, (x, z) => Mathf.Sqrt(x * x + z * z) > VRv + 10f && VDistToRoads(x, z) > 9f);
        // beberapa pohon di dalam desa
        Vector2[] inside = { V(-50f, 24f), V(50f, 8f), V(-56f, -10f), V(14f, 46f), V(8f, -56f), V(52f, 36f), V(-18f, 52f), V(-26f, -44f) };
        foreach (var p in inside)
        {
            var go = PropKit.Place(PropKit.TreeProto(5), g, "PohonApel", V3(p), Random.Range(0f, 360f), 1f, baseSlots);
            var cc = go.AddComponent<CapsuleCollider>();
            cc.center = new Vector3(0f, 1.5f, 0f); cc.radius = 0.5f; cc.height = 3f;
        }
        VBoundary(234f, vMerged);
    }

    static void VVillageNpcs()
    {
        Npcs("Warga", 4, RoutePts("Alun", ArcPts(Vector2.zero, 18f, 0f, 360f, 16)));
        Npcs("Pelajar", 1, RoutePts("AlunDalam", ArcPts(Vector2.zero, 9f, 0f, 360f, 12)));
        Npcs("Warga", 2, RoutePts("Utara", Line(V(3f, 24f), V(1f, 58f), 6f)));
        Npcs("Hunter", 2, RoutePts("Timur", Line(V(24f, 2f), V(58f, -2f), 6f)));
        Npcs("Petani", 2, RoutePts("Ladang", ArcPts(V(22f, -50f), 11f, 0f, 360f, 12)));
        Npcs("Warga", 1, RoutePts("Barat", Line(V(-26f, 3f), V(-56f, 0f), 6f)));
    }

    static void VSpawns()
    {
        VMark("SpawnStart", new Vector3(0f, 0f, -30f), 0f);
        VMark("SpawnFromForest", VRoadAt(0f, VR + 20f), 180f);           // kembali dari Hutan Pemula, menghadap desa
        VMark("SpawnFromTown", VRoadAt(90f, VR + 20f), 270f);            // kembali dari kota, menghadap barat (desa)
        VMark("RespawnPoint", new Vector3(0f, 0f, -20f), 0f);

        Vector3 ne = VRoadAt(0f, VEnd + 3f), ee = VRoadAt(90f, VEnd + 3f);
        if (!vMerged) VExit("Exit_HutanPemula", ne, 0f, StarterConfig.TutorialScene, "SpawnFromVillage", 0);
        var x2 = VExit("Exit_KotaPerbatasan", ee, 90f, StarterConfig.CityScene, "SpawnFromVillage", StarterConfig.CityMinLevel);
        x2.deniedText = "Penjaga: \"Jalan ke Ashwatch penuh bandit. Kembalilah setelah Level " + StarterConfig.CityMinLevel + ".\"";
    }

    static Transform VMark(string name, Vector3 pos, float yaw)
    {
        var t = new GameObject(name).transform;
        t.SetParent(tTown, false);
        t.position = pos;
        t.rotation = Quaternion.Euler(0f, yaw, 0f);
        return t;
    }

    static VillageExit VExit(string name, Vector3 pos, float yaw, string scene, string spawn, int minLevel)
    {
        var go = new GameObject(name);
        go.transform.SetParent(tTown, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        var ex = go.AddComponent<VillageExit>();
        ex.destScene = scene;
        ex.destSpawn = spawn;
        ex.minLevel = minLevel;
        ex.size = new Vector3(18f, 10f, 6f);
        return ex;
    }

    // =====================================================================
    // Pemain & manajer (dipakai kedua scene)
    // =====================================================================
    static void VPlayer(Vector3 start, float yaw, bool forest)
    {
        Material playerMat = UMat("Player", new Color(0.2f, 0.4f, 0.85f));
        var pl = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        pl.name = "Player";
        pl.tag = "Player";
        pl.transform.position = start + Vector3.up * 1.1f;
        pl.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        pl.GetComponent<Renderer>().sharedMaterial = playerMat;
        Object.DestroyImmediate(pl.GetComponent<CapsuleCollider>());
        var cc = pl.AddComponent<CharacterController>();
        cc.height = 2f; cc.radius = 0.4f; cc.center = Vector3.zero; cc.stepOffset = 0.4f;
        pl.AddComponent<PlayerController>();
        pl.AddComponent<HunterGuild.PlayerAvatar>();
        pl.AddComponent<PlayerCombat>();
        Prim(PrimitiveType.Cube, "FacingMarker", pl.transform, new Vector3(0f, 0.5f, 0.45f), new Vector3(0.3f, 0.3f, 0.5f), mMarker, false);

        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 back = -Fwd(yaw) * 8f;
            cam.transform.position = start + back + Vector3.up * 3.5f;
            cam.farClipPlane = forest ? 900f : 1200f;
            cam.gameObject.AddComponent<ThirdPersonCamera>().target = pl.transform;
        }
        new GameObject("GameManager").AddComponent<GameState>();
        new GameObject("TownUI").AddComponent<TownUI>();
        new GameObject("BackpackUI").AddComponent<BackpackUI>();
        new GameObject("SkillUI").AddComponent<SkillUI>();
        new GameObject("HuntHUD").AddComponent<HuntHUD>();
        var sunGo = GameObject.Find("Directional Light");
        if (sunGo != null)
        {
            var dn = new GameObject("DayNight").AddComponent<TownDayNight>();
            dn.sun = sunGo.GetComponent<Light>();
        }
    }

    // =====================================================================
    // HUTAN PEMULA
    // =====================================================================
    static readonly Vector2 FCamp = new Vector2(0f, -100f);

    static void VForestTrails()
    {
        // jalur sudah dicat di terrain; di sini hanya jembatan di setiap perpotongan dengan air
        for (int i = 0; i < vTrails.Count; i++)
        {
            float total;
            var at = VPolyAt(vTrails[i], out total);
            VBridgesAlong("Jalur" + i, at, 0f, total, i == 0 ? 4.6f : 3.6f);
        }
    }

    static float VDistToTrails(float x, float z)
    {
        float best = 9999f;
        foreach (var t in vTrails) best = Mathf.Min(best, VDistPoly(x, z, t));
        Vector2 p = V(x, z);
        Vector2[] branches = { V(-30f, -58f), V(32f, -50f), V(22f, 28f), V(-34f, 50f), V(40f, 82f) };
        foreach (var br in branches) best = Mathf.Min(best, Mathf.Max(0f, (p - br).magnitude - 14f));
        return best;
    }

    static void VForestCamp()
    {
        Vector3 c = V3(FCamp);
        var b = new GeoBag();
        // dua tenda
        Vector3[] tp = { c + new Vector3(-9f, 0.15f, 6f), c + new Vector3(-9f, 0.15f, -4f) };
        for (int i = 0; i < tp.Length; i++) b.Gable(Sl.Cloth, Sl.ClothB, Sl.Wood, tp[i], 3.4f, 4.2f, 2.3f, 0.1f, false);
        // api unggun: lingkar batu + kayu
        Vector3 fc = c + new Vector3(0f, 0f, 6f);
        for (int i = 0; i < 9; i++)
        {
            float a = i * 40f * Mathf.Deg2Rad;
            b.Box(Sl.Stone, fc + new Vector3(Mathf.Sin(a) * 1.1f, 0.18f, Mathf.Cos(a) * 1.1f), new Vector3(0.45f, 0.36f, 0.45f), i * 40f);
        }
        b.Beam(Sl.Wood, fc + new Vector3(-0.6f, 0.2f, -0.4f), fc + new Vector3(0.6f, 0.5f, 0.4f), 0.16f, 0.16f);
        b.Beam(Sl.Wood, fc + new Vector3(0.6f, 0.2f, -0.4f), fc + new Vector3(-0.6f, 0.5f, 0.4f), 0.16f, 0.16f);
        b.Box(Sl.Glow, fc + new Vector3(0f, 0.55f, 0f), new Vector3(0.5f, 0.4f, 0.5f), 45f);
        // bangku kayu & peti
        b.Box(Sl.WoodLight, fc + new Vector3(2.2f, 0.25f, 0f), new Vector3(0.5f, 0.5f, 2f), 0f);
        b.Box(Sl.WoodLight, fc + new Vector3(-2.2f, 0.25f, 0f), new Vector3(0.5f, 0.5f, 2f), 0f);
        // gerbang lengkung kayu di ujung selatan
        Vector3 arch = new Vector3(0f, 0f, -122f);
        VGateFrame(b, arch, 180f, 4.2f);
        b.Emit(tProps, "Kemah", baseSlots, MeshDir + "/Tut_Camp.asset");
        VSignBoth(tProps, arch, 180f, vMerged ? "HUTAN PEMULA\nLatihan s/d Lv 5" : "DESA PEMULA\nJalan pulang", 0.12f);
        BoxCol(tProps, "TendaCol", tp[0] + Vector3.up, new Vector3(3.4f, 2f, 4.2f), Vector3.zero);
        BoxCol(tProps, "TendaCol", tp[1] + Vector3.up, new Vector3(3.4f, 2f, 4.2f), Vector3.zero);

        var fl = new GameObject("Api_Unggun").transform;
        fl.SetParent(tProps, false);
        fl.position = fc + Vector3.up * 1.0f;
        var light = fl.gameObject.AddComponent<Light>();
        light.type = LightType.Point; light.range = 14f; light.intensity = 1.8f; light.color = new Color(1f, 0.6f, 0.25f);
        fl.gameObject.AddComponent<FlickerLight>();

        // pondok pemburu (tempat pulang)
        var cab = VDef(3, 2);
        float yaw;
        Vector2 cp = FCamp + V(12f, -2f);
        Vector2 door = VHouse(cab, cp, FCamp, "PondokPemburu", 6, out yaw);
        VHouseSign("PONDOK PEMBURU", cp, yaw, cab, cab.height * 0.5f);
        var bc = VPoint<BaseCamp>(tProps, "InteractPoint_Pondok", V3(door), "Pondok Pemburu (istirahat / pulang ke desa)", 3.6f);
        var guide = VNpc("NPC_PemanduHutan", "Pemandu Rowan", "Pemandu Hutan", "Hunter", V3(door) + Fwd(yaw) * 2f + Vector3.right * 2f, true);
        var gi = VAttach<InfoPoint>(guide, "Bicara: Pemandu Rowan", 3.4f);
        gi.title = "Pemandu Rowan";
        gi.message = "Selamat datang di Hutan Pemula!\n\n" +
            "- Padang Slime (barat laut kemah): Slime lemah, bagus untuk Level 0-2.\n" +
            "- Semak Herb: petik Herb dengan E.\n" +
            "- Hutan Goblin (utara): lebih galak, hati-hati. Bawa ramuan.\n\n" +
            "Setiap monster memberi EXP. Capai Level 5 lalu pilih job di desa, setelah itu jalan timur ke Ashwatch terbuka.\n" +
            "Kalau tumbang kau bangun lagi di kemah ini.";
        var board = VPoint<InfoPoint>(tProps, "InteractPoint_Papan", c + new Vector3(-4f, 0f, -2f), "Baca papan pemburu");
        board.title = "Papan Hutan Pemula";
        board.message = "Latihan berburu:\n1. Kalahkan 3 Slime, 4 Goblin\n2. Kumpulkan Herb\n3. Capai Level 5\n\nLapor ke Veteran Brann di desa untuk hadiah latihan.";
        Reserve(c.x, c.z, 20f);
        ReserveBox(c.x, c.z, 32f, 32f, 0f, 0f);

        if (!vMerged)
        {
            var vx = VExit("Exit_Desa", new Vector3(0f, 0f, -129f), 180f, StarterConfig.VillageScene, "SpawnFromForest", 0);
            vx.size = new Vector3(18f, 10f, 6f);
        }
    }

    static void VForestTrees()
    {
        var g = Group("Pohon", tProps);
        int placed = 0;
        Vector2[] clear = { V(-30f, -58f), V(32f, -50f), V(22f, 28f), V(-34f, 50f), V(40f, 82f), V(2f, -72f) };
        for (int i = 0; i < 24000 && placed < 650; i++)
        {
            float x = Random.Range(-164f, 164f), z = Random.Range(-164f, 164f);
            float y = VGy(x, z);
            if (y < 0.6f || VSlopeDeg(x, z) > 36f) continue;
            float edge = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
            if (edge < 112f)
            {
                if (VDistToTrails(x, z) < 6f) continue;
                if (Mathf.Abs(x - FCamp.x) < 24f && Mathf.Abs(z - FCamp.y) < 24f) continue;
                bool inClear = false;
                foreach (var cl in clear) if ((V(x, z) - cl).magnitude < 16f && Random.value < 0.85f) { inClear = true; break; }
                if (inClear) continue;
            }
            if (z < -112f && Mathf.Abs(x) < 16f) continue;                  // koridor gerbang selalu bebas pohon
            if (!IsFree(x, z, 3.0f)) continue;
            Reserve(x, z, 3.0f);
            VPlaceTree(g, x, z, 1f);
            placed++;
        }
        VScatterBoulders(Group("Batu", tProps), 90, 164f, (x, z) => VDistToTrails(x, z) > 5f && !(z < -112f && Mathf.Abs(x) < 20f) && (Mathf.Abs(x - FCamp.x) > 26f || Mathf.Abs(z - FCamp.y) > 26f));
        VBoundary(162f, false, vMerged);
    }

    static void VSpawnGroup(Transform parent, string id, float cx, float cz, float r, int n)
    {
        int made = 0;
        for (int i = 0; i < n * 40 && made < n; i++)
        {
            float a = Random.Range(0f, 6.28f), d = Mathf.Sqrt(Random.value) * r;
            float x = cx + Mathf.Sin(a) * d, z = cz + Mathf.Cos(a) * d;
            if (VGy(x, z) < VWaterLevel + 1.4f || VSlopeDeg(x, z) > 28f) continue;
            var go = new GameObject("Monster_" + id);
            go.transform.SetParent(parent, false);
            go.transform.position = VG(x, z);
            go.AddComponent<Monster>().defId = id;
            made++;
        }
    }

    static void VForestMonsters()
    {
        var root = Group("Monsters", tTown);
        VSpawnGroup(root, "slime", -30f, -58f, 20f, 7);
        VSpawnGroup(root, "slime", 32f, -50f, 18f, 5);
        VSpawnGroup(root, "goblin", 22f, 28f, 24f, 6);
        VSpawnGroup(root, "goblin", -34f, 50f, 20f, 4);
        VSpawnGroup(root, "goblin", 40f, 82f, 20f, 4);
    }

    static void VForestHerbs()
    {
        var root = Group("GatherNodes", tTown);
        Material leaf = UMat("HerbLeaf", new Color(0.25f, 0.62f, 0.27f));
        Material flower = UMat("HerbFlower", new Color(0.95f, 0.95f, 0.85f));
        for (int i = 0; i < 14; i++)
        {
            Vector2 cen = i < 9 ? V(2f, -72f) : V(0f, 40f);
            Vector3 pos = Vector3.zero;
            for (int tries = 0; tries < 60; tries++)
            {
                float a = Random.Range(0f, 6.28f), d = Mathf.Sqrt(Random.value) * (i < 9 ? 20f : 26f);
                float hx = cen.x + Mathf.Sin(a) * d, hz = cen.y + Mathf.Cos(a) * d;
                if (VGy(hx, hz) < VWaterLevel + 1.4f || VSlopeDeg(hx, hz) > 25f) continue;
                pos = VG(hx, hz);
                break;
            }
            if (pos == Vector3.zero) continue;
            var n = new GameObject("Herb");
            n.transform.SetParent(root, false);
            n.transform.position = pos;
            var vis = new GameObject("Visual");
            vis.transform.SetParent(n.transform, false);
            Prim(PrimitiveType.Sphere, "Daun", vis.transform, new Vector3(0f, 0.3f, 0f), new Vector3(0.8f, 0.55f, 0.8f), leaf, false);
            Prim(PrimitiveType.Sphere, "Daun2", vis.transform, new Vector3(0.25f, 0.2f, 0.15f), new Vector3(0.5f, 0.4f, 0.5f), leaf, false);
            Prim(PrimitiveType.Sphere, "Bunga", vis.transform, new Vector3(0f, 0.65f, 0f), new Vector3(0.2f, 0.14f, 0.2f), flower, false);
            var gn = n.AddComponent<GatherNode>();
            gn.itemId = "herb";
            gn.visual = vis;
            gn.prompt = "Kumpulkan Herb";
            gn.radius = 2.8f;
        }
    }

    static void VForestSpawns()
    {
        VMark("SpawnFromVillage", new Vector3(0f, 0f, -114f), 0f);
        VMark(vMerged ? "RespawnPoint_Hutan" : "RespawnPoint", V3(FCamp + V(0f, 14f)), 180f);
    }

    static HuntManager VForestManager()
    {
        var hm = new GameObject("HuntManager").AddComponent<HuntManager>();
        if (vMerged) hm.transform.SetParent(tTown, false);
        Vector3 zo = vMerged ? new Vector3(0f, 0f, VForestZ0) : Vector3.zero;
        hm.defaultZone = "Hutan Pemula";
        hm.returnScene = StarterConfig.VillageScene;
        hm.townSpawn = "SpawnFromForest";
        hm.zones = new ZoneInfo[]
        {
            new ZoneInfo { name = "Pondok Pemburu", center = V3(FCamp) + zo, radius = 28f },
            new ZoneInfo { name = "Padang Slime", center = new Vector3(-30f, 0f, -58f) + zo, radius = 28f },
            new ZoneInfo { name = "Padang Slime Timur", center = new Vector3(32f, 0f, -50f) + zo, radius = 26f },
            new ZoneInfo { name = "Semak Herb", center = new Vector3(2f, 0f, -72f) + zo, radius = 24f },
            new ZoneInfo { name = "Hutan Goblin", center = new Vector3(22f, 0f, 28f) + zo, radius = 38f },
            new ZoneInfo { name = "Hutan Dalam", center = new Vector3(10f, 0f, 80f) + zo, radius = 55f },
        };
        return hm;
    }
}
