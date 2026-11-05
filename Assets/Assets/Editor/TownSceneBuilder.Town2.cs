using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HunterGuild;

/// <summary>
/// v35z105n81 "Ashwatch Konsentris": Kota Ashwatch (scene Town) mengikuti kanon: TIGA tembok batu kelabu konsentris (luar paling rendah & lebar mengurung ladang hijau + parit berkilat,
/// tengah lebih tinggi berisi kota padat atap genteng merah, dalam paling gelap & tinggi di bukit mahkota dengan menara kastil emas), Katedral jarum putih di pusat, 12 gerbang.
/// (n76 "Ashwatch Benteng": satu tembok luar + tembok kastil; kerangka, parit/jembatan/gerbang/menara dipertahankan.)
/// Data denah: Town2Plan.cs + Town2PlanData.cs (kelas T2). Model: Town2Kit*.cs (T2Kit), Town2Gate*.cs (T2Gate), Town2Walls.cs (T2Walls).
/// Berkas ini: alur build, palet, medan + cat tanah, jalan, alun-alun, jembatan.
/// Berkas lain: Town2Walls (tembok, gerbang, lengkung sungai), Town2Lots (bangunan + pengait gameplay), Town2Areas (ladang, taman, prop, pohon, koridor),
/// Town2Life (NPC, NPC tersembunyi, kejadian acak, titik muncul, pemain).
/// Semua pengait gameplay lama dipertahankan (GuildHall, Inn, Blacksmith, GeneralShop, Library, Restaurant, BuildingDoor, CityGate, LevelGate, Komplek Properti,
/// AreaTransition + titik muncul, HiddenNpc, TownEventManager). Builder lama (3 tembok konsentris) tetap ada di menu Legacy.
/// </summary>
public static partial class TownSceneBuilder
{
    const string T2MarkerPath = "Assets/HGLayout/Town.v4";
    static readonly Color T2Gold = new Color(1f, 0.84f, 0.42f);
    const float T2Half = 520f;
    const int T2HRes = 1025, T2ARes = 1024;

    static float[,] t2Cache;
    static float t2Step;
    static float[][] t2RoadBox, t2OutBox;
    static Material[] t2Pal;

    // =====================================================================
    // Alur utama
    // =====================================================================
    public static void T2BuildTown()
    {
        V2Kit.SaltAtDoor = false;                     // n82: rumah jerami cincin ladang memakai kit Desa Pemula tetapi tanpa garis garam
        if (!VConfirm(ScenePath)) return;
        T2LayoutMigrate();
        complexScene = null;
        vStreetsActive = false;
        EditorUtility.DisplayProgressBar("Hunter Guild", "Ashwatch Benteng: bahan, medan & parit...", 0.04f);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        T2Init();
        T2.Build();
        var issues = T2.Validate();
        foreach (var s in issues) Debug.LogWarning("[Kota T2] denah: " + s);
        baseSlots = T2Palette();
        t2Pal = baseSlots;
        new GameObject("HG_DenahT2").transform.SetParent(tTown, false);          // penanda versi denah di scene
        T2Terrain();
        EditorUtility.DisplayProgressBar("Hunter Guild", "Jalan, alun-alun & jembatan...", 0.16f);
        T2RoadMeshes();
        T2PlazaMeshes();
        T2Bridges();
        EditorUtility.DisplayProgressBar("Hunter Guild", "Tembok, menara & gerbang...", 0.26f);
        T2WallsAndGates();
        EditorUtility.DisplayProgressBar("Hunter Guild", "Bangunan penting...", 0.42f);
        T2Landmarks();
        EditorUtility.DisplayProgressBar("Hunter Guild", "Komplek Properti & rumah warga...", 0.52f);
        T2PropertyComplex();
        T2FillHouses();
        EditorUtility.DisplayProgressBar("Hunter Guild", "Pertanian, taman & prop...", 0.66f);
        T2Farm();
        T2Parks();
        T2PlazaProps();
        T2Lamps();
        T2StreetTrees();
        T2LoreProps();
        T2CathedralCanon();                      // n89: lonceng enam dentang, dua InfoPoint di pelataran (retak muka ada di T2Kit.Cathedral)
        T2Cemetery();
        T2RiverBanks();
        T2OutsideWorld();
        EditorUtility.DisplayProgressBar("Hunter Guild", "Koridor & titik muncul...", 0.78f);
        T2Corridors();
        EditorUtility.DisplayProgressBar("Hunter Guild", "NPC, kejadian, pemain...", 0.86f);
        T2Npcs();
        T2HiddenNpcs();
        T2Events();
        T2PlayerAndManagers();

        MarkStatic(tEnv); MarkStatic(tWalls); MarkStatic(tGates); MarkStatic(tRoads); MarkStatic(tBuildings);
        MarkStatic(tHouses); MarkStatic(tNoble); MarkStatic(tFarm); MarkStatic(tProps);
        BuildingMover.Prepare("Town", new[] { tBuildings, tHouses, tNoble }, new[] { tProps, tBuildings, tHouses, tNoble });

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0011f;
        RenderSettings.fogColor = new Color(0.64f, 0.74f, 0.82f);

        EditorUtility.DisplayProgressBar("Hunter Guild", "Menyimpan scene...", 0.96f);
        AssetDatabase.SaveAssets();
        HeightBake.BackupScene(ScenePath); EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings(ScenePath);
        EditorUtility.ClearProgressBar();
        HGDlg.Show("Hunter Guild",
            "Kota Ashwatch 'Konsentris' dibuat: " + ScenePath + "\n12 gerbang + Gerbang Kastil, " + houseCount + " bangunan, " + npcCount + " NPC, 10 NPC tersembunyi, 8 kejadian acak.\nTiga tembok kelabu konsentris: ladang hijau berparit di luar, kota padat atap merah di tengah, Katedral jarum putih dan kastil di bukit mahkota. Tekan Play untuk mencoba.", "OK");
    }

    /// <summary>Denah lama (n76 "Benteng") dibuang: posisi manual tersimpannya tidak cocok dengan denah konsentris.</summary>
    static void T2LayoutMigrate()
    {
        try
        {
            if (File.Exists(T2MarkerPath)) { BuildingMover.CaptureBeforeRebuild(ScenePath); return; }       // scene terakhir sudah denah baru: simpan posisi manual
            string json = "Assets/HGLayout/Town.json";
            if (File.Exists(json)) { AssetDatabase.DeleteAsset(json); Debug.Log("[Kota T2] Posisi manual denah lama dibuang (denah kota diganti total)."); }
            if (!AssetDatabase.IsValidFolder("Assets/HGLayout")) AssetDatabase.CreateFolder("Assets", "HGLayout");
            File.WriteAllText(T2MarkerPath, "Denah Kota Ashwatch v4 (Konsentris: tiga tembok, ladang cincin luar, Katedral di pusat). Hapus berkas ini bila ingin membuang posisi manual saat build berikutnya.");
            AssetDatabase.ImportAsset(T2MarkerPath);
        }
        catch (System.Exception e) { Debug.LogWarning("[Kota T2] Migrasi layout dilewati: " + e.Message); }
    }

    static void T2Init()
    {
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Materials");
        EnsureFolder(MatDir);
        EnsureFolder("Assets/Meshes");
        EnsureFolder(MeshDir);
        Random.InitState(20261101);
        grid.Clear();
        solidBoxes.Clear();
        streets.Clear();
        houseCount = 0;
        npcCount = 0;
        TownMats.Load();
        NpcKit.Reset();
        GateKit.Reset();
        T2Gate.Reset();
        PropKit.Reset();
        ForestKit.Reset();
        ForestKit.Load();
        VLoadTreeProtos();
        vH = null;
        t2Cache = null;
        T2ResetAreas();
        mainGate = null;
        baseSlots = TownMats.Slots();
        mMarker = UMat("VMarker", new Color(1f, 0.85f, 0.3f));

        tTown = new GameObject("Town").transform;
        tEnv = Group("Environment", tTown);
        tWalls = Group("Walls", tTown);
        tGates = Group("Gates", tTown);
        tRoads = Group("Roads", tTown);
        tBuildings = Group("Buildings", tTown);
        tHouses = Group("Houses", tTown);
        tNoble = Group("Noble", tTown);
        tFarm = Group("Farmland", tTown);
        tProps = Group("Props", tTown);
        tNpcs = Group("NPCs", tTown);
        tWp = Group("NPC_Waypoints", tTown);
    }

    // =====================================================================
    // Palet: seluruh 37 slot terisi (tidak ada sub-mesh tanpa material)
    // =====================================================================
    static Material[] T2Palette()
    {
        var s = TownMats.Slots();
        s[Sl.SkinS] = UMat("T2_Skin", new Color(0.86f, 0.66f, 0.54f));
        s[Sl.Eye] = UMat("T2_Eye", new Color(0.08f, 0.07f, 0.06f));
        s[Sl.CoatA] = UMat("T2_CoatA", new Color(0.52f, 0.14f, 0.13f));
        s[Sl.CoatB] = UMat("T2_CoatB", new Color(0.2f, 0.28f, 0.46f));
        s[Sl.CoatC] = UMat("T2_CoatC", new Color(0.42f, 0.34f, 0.24f));
        s[Sl.Dark] = UMat("T2_Dark", new Color(0.09f, 0.08f, 0.08f));
        for (int i = 0; i < s.Length; i++) if (s[i] == null) s[i] = UMat("T2_Fallback", new Color(0.5f, 0.45f, 0.4f));
        return s;
    }

    static int T2Wrap(int i, int n) { return ((i % n) + n) % n; }

    /// <summary>Salinan palet dasar dengan plester/atap/kain tertentu (indeks berputar).</summary>
    static Material[] T2Mix(int plaster, int roof, int plasterB, int roofB, int cloth, int clothB)
    {
        var s = (Material[])baseSlots.Clone();
        int np = TownMats.Plaster.Length, nr = TownMats.Roofs.Length, nc = TownMats.Cloths.Length;
        s[Sl.Plaster] = TownMats.Plaster[T2Wrap(plaster, np)];
        s[Sl.PlasterB] = TownMats.Plaster[T2Wrap(plasterB, np)];
        s[Sl.Roof] = TownMats.Roofs[T2Wrap(roof, nr)];
        s[Sl.RoofB] = TownMats.Roofs[T2Wrap(roofB, nr)];
        s[Sl.Cloth] = TownMats.Cloths[T2Wrap(cloth, nc)];
        s[Sl.ClothB] = TownMats.Cloths[T2Wrap(clothB, nc)];
        return s;
    }

    static readonly int[] T2LowPlaster = { 0, 4, 2, 1, 3, 0, 4, 1 };
    static readonly int[] T2LowRoof = { 0, 1, 0, 5, 1, 0, 4, 5 };
    static readonly int[] T2UpPlaster = { 1, 6, 0, 5, 1, 6, 2, 1 };
    static readonly int[] T2UpRoof = { 2, 3, 4, 2, 3, 5, 2, 4 };

    /// <summary>Warna rumah warga menurut zona: kota bawah hangat (koral/madu), kota atas sejuk (kobalt/indigo/teal), desa tani madu.</summary>
    static Material[] T2HouseSlots(string zone, int variant)
    {
        int v = Mathf.Abs(variant);
        if (zone == "atas" || zone == "kastil")
            return T2Mix(T2UpPlaster[v % 8], T2UpRoof[(v / 3) % 8], T2UpPlaster[(v + 3) % 8], T2UpRoof[(v / 3 + 2) % 8], v % 6, (v + 3) % 6);
        if (zone == "tani")
            return T2Mix(T2LowPlaster[(v + 2) % 8], T2LowRoof[(v / 3 + 1) % 8], T2LowPlaster[(v + 5) % 8], 1, (v + 2) % 6, (v + 5) % 6);
        return T2Mix(T2LowPlaster[v % 8], T2LowRoof[(v / 3) % 8], T2LowPlaster[(v + 3) % 8], T2LowRoof[(v / 3 + 3) % 8], v % 6, (v + 3) % 6);
    }

    // =====================================================================
    // Medan: tinggi di-cache pada grid heightmap, sehingga semua objek berpijak persis di atas mesh terrain
    // =====================================================================
    static float T2G(float x, float z)
    {
        if (t2Cache == null) return T2.H(x, z);
        float fx = (x + T2Half) / t2Step, fz = (z + T2Half) / t2Step;
        int n = T2HRes;
        if (fx < 0f || fz < 0f || fx >= n - 1 || fz >= n - 1) return T2.H(x, z);
        int ix = (int)fx, iz = (int)fz;
        float tx = fx - ix, tz = fz - iz;
        float a = t2Cache[iz, ix], b = t2Cache[iz, ix + 1], c = t2Cache[iz + 1, ix], d = t2Cache[iz + 1, ix + 1];
        return (a + (b - a) * tx) * (1f - tz) + (c + (d - c) * tx) * tz;
    }

    static Vector3 T2P(float x, float z, float dy = 0f) { return new Vector3(x, T2G(x, z) + dy, z); }

    static void T2BuildCache()
    {
        int n = T2HRes;
        var cache = new float[n, n];
        float step = T2Half * 2f / (n - 1), half = T2Half;
        t2Step = step;
        try
        {
            System.Threading.Tasks.Parallel.For(0, n, zi =>
            {
                for (int xi = 0; xi < n; xi++) cache[zi, xi] = T2.H(-half + xi * step, -half + zi * step);
            });
        }
        catch (System.Exception)
        {
            for (int zi = 0; zi < n; zi++)
                for (int xi = 0; xi < n; xi++) cache[zi, xi] = T2.H(-half + xi * step, -half + zi * step);
        }
        t2Cache = cache;
    }

    static float[] T2Box(IList<Vector2> pts, float pad)
    {
        float x0 = 1e9f, x1 = -1e9f, z0 = 1e9f, z1 = -1e9f;
        foreach (var p in pts) { x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x); z0 = Mathf.Min(z0, p.y); z1 = Mathf.Max(z1, p.y); }
        return new[] { x0 - pad, x1 + pad, z0 - pad, z1 + pad };
    }

    static void T2Terrain()
    {
        T2BuildCache();
        t2RoadBox = new float[T2.Roads.Count][];
        for (int i = 0; i < T2.Roads.Count; i++) t2RoadBox[i] = T2Box(T2.Roads[i].pts, T2.Roads[i].width * 0.5f + 6f);
        t2OutBox = new float[T2.OutRoads.Count][];
        for (int i = 0; i < T2.OutRoads.Count; i++) t2OutBox[i] = T2Box(T2.OutRoads[i], 14f);
        vH = T2G;
        vGrassDens = 0.8f;
        VBuildTerrain("TownTerrain", T2Half, T2Paint, T2HRes, T2ARes);
        vGrassDens = 0.4f;
        VBoundary(T2Half - 6f);
    }

    /// <summary>(x, z, tinggi) -> bobot 7 lapisan: jalan batu bulat, tanah di tepi jalan & di bawah bangunan, alun-alun, ladang, jalan keluar gerbang, lumpur tepi sungai.</summary>
    static float[] T2Paint(float x, float z, float h)
    {
        float cob = 0f, dirt = 0f;
        var roads = T2.Roads;
        for (int i = 0; i < roads.Count; i++)
        {
            var bx = t2RoadBox[i];
            if (x < bx[0] || x > bx[1] || z < bx[2] || z > bx[3]) continue;
            var r = roads[i];
            float d = T2.DistPoly(x, z, r.pts) - r.width * 0.5f;
            if (r.mat == 1) dirt = Mathf.Max(dirt, T2.Sm(1f - d / 1.1f));
            else { cob = Mathf.Max(cob, T2.Sm(1f - d / 0.7f)); dirt = Mathf.Max(dirt, T2.Sm(1f - (d - 0.2f) / 2.2f) * 0.85f); }
        }
        for (int i = 0; i < T2.OutRoads.Count; i++)
        {
            var bx = t2OutBox[i];
            if (x < bx[0] || x > bx[1] || z < bx[2] || z > bx[3]) continue;
            float d = T2.DistPoly(x, z, T2.OutRoads[i]) - 4.5f;
            dirt = Mathf.Max(dirt, T2.Sm(1f - d / 1.8f));
        }
        var pls = T2.Plazas;
        for (int i = 0; i < pls.Count; i++)
        {
            var p = pls[i];
            float rr = Mathf.Max(p.w, p.d) * 0.5f + 6f;
            if (Mathf.Abs(x - p.x) > rr || Mathf.Abs(z - p.z) > rr) continue;
            float d = p.round ? Mathf.Sqrt((x - p.x) * (x - p.x) + (z - p.z) * (z - p.z)) - p.w * 0.5f : T2.DistToRect(x, z, p.x, p.z, p.yaw, p.w, p.d);
            cob = Mathf.Max(cob, T2.Sm(1f - d / 0.8f));
        }
        var lots = T2.Lots;
        for (int i = 0; i < lots.Count; i++)
        {
            var l = lots[i];
            float reach = Mathf.Max(l.w, l.d) * 0.5f + 8f;
            if (Mathf.Abs(x - l.x) > reach || Mathf.Abs(z - l.z) > reach) continue;
            float dr = T2.DistToRect(x, z, l);
            switch (l.kind)
            {
                case "field": dirt = Mathf.Max(dirt, T2.Sm(1f - dr / 1.2f) * 0.95f); break;
                case "komplek": cob = Mathf.Max(cob, T2.Sm(1f - dr / 0.8f)); break;
                case "bridgepad": case "gatepad": break;
                default: dirt = Mathf.Max(dirt, T2.Sm(1f - (dr - 0.6f) / 2.4f) * 0.5f); break;       // tanah pijakan di sekeliling bangunan
            }
        }
        var zs = T2.Zones;
        for (int i = 0; i < zs.Count; i++)
        {
            var zn = zs[i];
            if (zn.kind != "field") continue;
            if (T2.Inside(x, z, zn.poly)) dirt = Mathf.Max(dirt, 0.4f);
        }
        float bank = T2.RiverBank(x, z);
        float hp = Mathf.Lerp(Mathf.Max(h, VWaterLevel + 1.3f), h, bank);
        float so = T2.SdOuter(x, z), sm = T2.SdMid(x, z), si = T2.SdInner(x, z);
        // parit (tiga cincin): lereng & dasar berupa tanah/lumpur
        if (so > T2.DitchNear - 2f && so < T2.DitchFarEdge + 2f)
            dirt = Mathf.Max(dirt, Mathf.Clamp01(T2.DitchProfile(so, T2.DitchNear, T2.DitchRun, T2.DitchFloor) * 1.9f));
        if (sm > T2.DitchNear - 2f && sm < T2.DitchFarEdge + 2f)
            dirt = Mathf.Max(dirt, Mathf.Clamp01(T2.DitchProfile(sm, T2.DitchNear, T2.DitchRun, T2.DitchFloor) * 1.9f));
        if (si > T2.KDitchNear - 2f && si < T2.KDitchFarEdge + 2f)
            dirt = Mathf.Max(dirt, Mathf.Clamp01(T2.DitchProfile(si, T2.KDitchNear, T2.KDitchRun, T2.KDitchFloor) * 1.9f));
        float litter = Mathf.Lerp(0.05f, 1f, T2.Sm((so + 5f) / 40f));
        return VPaintStd(x, z, hp, dirt * (1f - cob), cob, litter);
    }

    // =====================================================================
    // Jalan: pita batu bulat / paving / tanah yang mengikuti medan
    // =====================================================================
    static bool T2OnBridge(Vector2 p)
    {
        foreach (var br in T2.Bridges)
        {
            float a = br.yaw * Mathf.Deg2Rad;
            Vector2 t = new Vector2(Mathf.Sin(a), Mathf.Cos(a)), n = new Vector2(t.y, -t.x), d = p - br.c;
            if (Mathf.Abs(Vector2.Dot(d, t)) < br.L * 0.5f + 0.4f && Mathf.Abs(Vector2.Dot(d, n)) < br.W + 1.5f) return true;
        }
        return false;
    }

    /// <summary>Pita sepanjang pts selebar width; tinggi tiap titik = medan + yOff; segmen di atas jembatan dilewati.</summary>
    static void T2Strip(GeoBag b, int slot, IList<Vector2> pts, float width, float yOff, int lat)
    {
        float total = T2.Len(pts), step = 1.5f;
        int n = Mathf.Max(2, Mathf.CeilToInt(total / step));
        Vector3[] prev = null;
        Vector2 prevP = Vector2.zero;
        for (int i = 0; i <= n; i++)
        {
            Vector2 tg; Vector2 p = T2.At(pts, total * i / n, out tg);
            Vector2 nr = new Vector2(tg.y, -tg.x);
            var row = new Vector3[lat + 1];
            for (int k = 0; k <= lat; k++)
            {
                float o = (k / (float)lat - 0.5f) * width;
                Vector2 q = p + nr * o;
                row[k] = new Vector3(q.x, T2G(q.x, q.y) + yOff, q.y);
            }
            if (prev != null && !T2OnBridge(p) && !T2OnBridge((p + prevP) * 0.5f))
                for (int k = 0; k < lat; k++) V2Kit.QUp(b, slot, prev[k], prev[k + 1], row[k + 1], row[k]);
            prev = row;
            prevP = p;
        }
    }

    static void T2RoadMeshes()
    {
        var g = Group("JalanKota", tRoads);
        var edge = new GeoBag(); var cob = new GeoBag(); var pav = new GeoBag(); var lane = new GeoBag();
        for (int i = 0; i < T2.Roads.Count; i++)
        {
            var r = T2.Roads[i];
            if (r.dead) continue;
            float k = i * 0.004f;
            int lat = Mathf.Max(3, Mathf.CeilToInt(r.width / 2.6f));
            if (r.mat == 0) { T2Strip(edge, Sl.Dirt, r.pts, r.width + 1.4f, 0.03f + k, 3); T2Strip(cob, Sl.Cobble, r.pts, r.width, 0.06f + k, lat); }
            else if (r.mat == 2) { T2Strip(edge, Sl.Dirt, r.pts, r.width + 1.0f, 0.03f + k, 3); T2Strip(pav, Sl.Pave, r.pts, r.width, 0.06f + k, lat); }
            else T2Strip(lane, Sl.Dirt, r.pts, r.width, 0.045f + k, 3);
        }
        // jalan keluar gerbang: jalur tanah selebar 9 m
        for (int i = 0; i < T2.OutRoads.Count; i++) T2Strip(lane, Sl.Dirt, T2.OutRoads[i], 9f, 0.05f + i * 0.004f, 3);
        edge.Emit(g, "TepiJalan", baseSlots, MeshDir + "/T2_RoadEdge.asset");
        lane.Emit(g, "JalanTanah", baseSlots, MeshDir + "/T2_Lanes.asset");
        pav.Emit(g, "JalanPaving", baseSlots, MeshDir + "/T2_Paving.asset");
        cob.Emit(g, "JalanBatu", baseSlots, MeshDir + "/T2_Cobble.asset");
    }

    // =====================================================================
    // Alun-alun: lantai paving mengikuti medan, bingkai batu, lingkar hias
    // =====================================================================
    static void T2RectPatch(GeoBag b, int slot, float cx, float cz, float yaw, float w, float d, float cell, float yOff)
    {
        int nx = Mathf.Max(1, Mathf.CeilToInt(w / cell)), nz = Mathf.Max(1, Mathf.CeilToInt(d / cell));
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
            {
                float x0 = -w * 0.5f + w * i / nx, x1 = -w * 0.5f + w * (i + 1) / nx, z0 = -d * 0.5f + d * j / nz, z1 = -d * 0.5f + d * (j + 1) / nz;
                Vector2 a = T2.ToWorld(cx, cz, yaw, x0, z0), c = T2.ToWorld(cx, cz, yaw, x0, z1), e = T2.ToWorld(cx, cz, yaw, x1, z1), f = T2.ToWorld(cx, cz, yaw, x1, z0);
                V2Kit.QUp(b, slot, T2P(a.x, a.y, yOff), T2P(c.x, c.y, yOff), T2P(e.x, e.y, yOff), T2P(f.x, f.y, yOff));
            }
    }

    static void T2RingPatch(GeoBag b, int slot, float cx, float cz, float r0, float r1, int seg, float yOff)
    {
        for (int i = 0; i < seg; i++)
        {
            float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
            Vector2 p00 = new Vector2(cx + Mathf.Sin(a0) * r0, cz + Mathf.Cos(a0) * r0), p01 = new Vector2(cx + Mathf.Sin(a0) * r1, cz + Mathf.Cos(a0) * r1);
            Vector2 p11 = new Vector2(cx + Mathf.Sin(a1) * r1, cz + Mathf.Cos(a1) * r1), p10 = new Vector2(cx + Mathf.Sin(a1) * r0, cz + Mathf.Cos(a1) * r0);
            V2Kit.QUp(b, slot, T2P(p00.x, p00.y, yOff), T2P(p01.x, p01.y, yOff), T2P(p11.x, p11.y, yOff), T2P(p10.x, p10.y, yOff));
        }
    }

    static void T2PlazaMeshes()
    {
        var g = Group("AlunAlun", tRoads);
        var pave = new GeoBag(); var cob = new GeoBag(); var stone = new GeoBag();
        foreach (var p in T2.Plazas)
        {
            if (p.round)
            {
                // alun-alun tengah: lantai paving bundar, cincin batu, jari-jari
                T2RingPatch(pave, Sl.Pave, p.x, p.z, 0.01f, p.w * 0.5f, 36, 0.05f);
                T2RingPatch(cob, Sl.Cobble, p.x, p.z, p.w * 0.5f - 4.5f, p.w * 0.5f - 1.5f, 36, 0.07f);
                T2RingPatch(stone, Sl.Stone, p.x, p.z, p.w * 0.5f - 0.5f, p.w * 0.5f + 0.4f, 36, 0.09f);
                T2RingPatch(stone, Sl.DarkStone, p.x, p.z, 7.2f, 7.9f, 28, 0.09f);
                for (int k = 0; k < 8; k++)
                {
                    float a = k * Mathf.PI * 0.25f + 0.39f;
                    Vector2 q0 = new Vector2(p.x + Mathf.Sin(a) * 8f, p.z + Mathf.Cos(a) * 8f), q1 = new Vector2(p.x + Mathf.Sin(a) * (p.w * 0.5f - 5f), p.z + Mathf.Cos(a) * (p.w * 0.5f - 5f));
                    Vector2 nrm = new Vector2(Mathf.Cos(a), -Mathf.Sin(a)) * 0.35f;
                    V2Kit.QUp(stone, Sl.Stone, T2P(q0.x - nrm.x, q0.y - nrm.y, 0.09f), T2P(q1.x - nrm.x, q1.y - nrm.y, 0.09f), T2P(q1.x + nrm.x, q1.y + nrm.y, 0.09f), T2P(q0.x + nrm.x, q0.y + nrm.y, 0.09f));
                }
                continue;
            }
            bool yard = p.kind == "halaman";
            T2RectPatch(yard ? cob : pave, yard ? Sl.Cobble : Sl.Pave, p.x, p.z, p.yaw, p.w, p.d, 3.2f, 0.05f);
            // bingkai batu di tepi dalam
            float e = 0.9f;
            T2RectPatch(stone, Sl.Stone, p.x, p.z, p.yaw, p.w, e, 3.2f, 0.08f);
            T2RectPatch(stone, Sl.Stone, p.x, p.z, p.yaw, e, p.d, 3.2f, 0.08f);
        }
        // lingkar hias di sekeliling air mancur bundaran alun-alun gerbang
        Vector2 fg = T2.FountainGate;
        T2RingPatch(stone, Sl.Stone, fg.x, fg.y, 8.2f, 9.2f, 32, 0.09f);
        T2RingPatch(cob, Sl.Cobble, fg.x, fg.y, 9.2f, 13.2f, 32, 0.075f);
        T2RingPatch(stone, Sl.DarkStone, fg.x, fg.y, 13.2f, 13.9f, 36, 0.09f);
        pave.Emit(g, "LantaiPaving", baseSlots, MeshDir + "/T2_PlazaPave.asset");
        cob.Emit(g, "LantaiBatu", baseSlots, MeshDir + "/T2_PlazaCobble.asset");
        stone.Emit(g, "BingkaiBatu", baseSlots, MeshDir + "/T2_PlazaStone.asset");
    }

    // =====================================================================
    // Jembatan batu lengkung di tiga penyeberangan Sungai Abu
    // =====================================================================
    static void T2Bridges()
    {
        var g = Group("Jembatan", tProps);
        int idx = 0;
        foreach (var br in T2.Bridges)
        {
            var b = new GeoBag();
            if (br.kind == "parit") { T2MoatBridge(g, br, b, ref idx); continue; }
            float springRel = VWaterLevel - br.y;
            b.Push(new Vector3(br.c.x, br.y, br.c.y), br.yaw);
            V2Kit.StoneBridge(b, br.L, br.W, br.wide ? 1.9f : 1.6f, springRel, br.chord);
            b.Pop();
            var go = b.Emit(g, "JembatanBatu_" + br.id, baseSlots, MeshDir + "/T2_Br_" + br.id + ".asset");
            var mf = go.GetComponent<MeshFilter>();
            if (mf != null) go.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            float a = br.yaw * Mathf.Deg2Rad;
            Vector2 t = new Vector2(Mathf.Sin(a), Mathf.Cos(a)), n = new Vector2(t.y, -t.x);
            foreach (float sg in new[] { -1f, 1f })
                foreach (float sd in new[] { -1f, 1f })
                {
                    Vector2 p = br.c + t * (sg * (br.L * 0.5f + 1.6f)) + n * (sd * (br.W * 0.5f + 1.1f));
                    T2PlaceLamp(new Vector3(p.x, T2G(p.x, p.y), p.y), br.yaw + (sd > 0 ? 90f : -90f) + (sg > 0 ? 0f : 0f), (idx++ % 2) == 0);
                }
        }
    }

    /// <summary>Jembatan batu berlengkung banyak di atas parit di depan gerbang: dek rata setinggi lantai gerbang, menurun halus bila tanah di luar parit lebih rendah.</summary>
    static void T2MoatBridge(Transform g, T2.Bridge br, GeoBag b, ref int idx)
    {
        float a = br.yaw * Mathf.Deg2Rad;
        Vector2 t = new Vector2(Mathf.Sin(a), Mathf.Cos(a)), n = new Vector2(t.y, -t.x);
        float y0 = T2G(br.gate.x, br.gate.y) + 0.02f;
        Vector2 far = br.c + t * (br.L * 0.5f + 0.6f);
        float drop = Mathf.Clamp(T2G(far.x, far.y) - y0 + 0.02f, -4.5f, 0.6f);
        float depth = br.depth + 0.6f;
        b.Push(new Vector3(br.c.x, y0, br.c.y), br.yaw);
        T2Kit.MoatBridge(b, br.L, br.W, drop, depth);
        b.Pop();
        var go = b.Emit(g, "JembatanParit_" + br.id, baseSlots, MeshDir + "/T2_Br_" + br.id + ".asset");
        var mf = go.GetComponent<MeshFilter>();
        if (mf != null) go.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
        // lampu besi di kedua tepi dek pada 1/4 dan 3/4 panjang (di atas dek)
        foreach (float f in new[] { 0.25f, 0.75f })
            foreach (float sd in new[] { -1f, 1f })
            {
                Vector2 p = br.c + t * ((f - 0.5f) * br.L) + n * (sd * (br.W * 0.5f - 0.7f));
                float yy = y0 + drop * f;
                var lampPos = new Vector3(p.x, yy + 0.02f, p.y);
                var proto = PropKit.Get("T2_Lamp", T2Kit.Lamp);
                var lg = PropKit.Place(proto, GetLampGroup(), "LampuJembatan", lampPos, br.yaw + (sd > 0 ? 90f : -90f), 1f, baseSlots);
                if ((idx++ % 2) == 0) AddLight(lg.transform, new Vector3(0.85f, 3.3f, 0f), 16f, 1.6f);
            }
    }

    /// <summary>Lampu besi kota di tanah; lengan lampu (+X lokal) menghadap yaw tertentu (ke arah jalan).</summary>
    static GameObject T2PlaceLamp(Vector3 pos, float yaw, bool light)
    {
        var proto = PropKit.Get("T2_Lamp", T2Kit.Lamp);
        var go = PropKit.Place(proto, GetLampGroup(), "Lampu", pos, yaw, 1f, baseSlots);
        BoxCol(go.transform, "TiangCol", new Vector3(0f, 1.6f, 0f), new Vector3(0.5f, 3.2f, 0.5f), Vector3.zero);
        Reserve(pos.x, pos.z, 0.7f);
        if (light) AddLight(go.transform, new Vector3(0.85f, 3.3f, 0f), 16f, 1.6f);
        return go;
    }

    static Transform t2LampGroup;
    static Transform GetLampGroup()
    {
        if (t2LampGroup == null) t2LampGroup = Group("Lampu", tProps);
        return t2LampGroup;
    }
}
