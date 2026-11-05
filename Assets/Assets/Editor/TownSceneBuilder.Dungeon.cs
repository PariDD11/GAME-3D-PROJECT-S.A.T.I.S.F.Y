using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Kota Eldrath: kota tembok tunggal yang lebih besar dan megah dari Ashwatch, berpusat pada Menara Eldrath
/// (pintu masuk dungeon 100 lantai). Memakai ulang kit tembok, gerbang, rumah, landmark, jalan, dan NPC milik Town.
/// Scene: Assets/Scenes/DungeonCity.unity. Dicapai dari Gerbang Luar Utara Ashwatch.
/// </summary>
public static partial class TownSceneBuilder
{
    const string DungeonScenePath = "Assets/Scenes/DungeonCity.unity";
    public const float DR = 250f;              // radius tembok Eldrath
    const float DPlazaR = 82f;                 // alun-alun menara
    const float DCorridorLen = 96f;

    static Material[] dSl;

    [MenuItem("Hunter Guild/Build Dungeon City (Eldrath)")]
    public static void BuildDungeonCity()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DungeonScenePath) != null)
        {
            if (!HGDlg.Show("Hunter Guild",
                "DungeonCity.unity sudah ada.\nMembangun ulang akan MENIMPA scene itu.\n\nLanjutkan?", "Timpa", "Batal")) return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BuildingMover.CaptureBeforeRebuild(DungeonScenePath); complexScene = null;

        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Materials");
        EnsureFolder(MatDir);
        EnsureFolder("Assets/Meshes");
        EnsureFolder(MeshDir);

        EditorUtility.DisplayProgressBar("Hunter Guild", "Bahan dan kit Eldrath...", 0.04f);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Random.InitState(20261018);
        grid.Clear();
        solidBoxes.Clear();
        streets.Clear();
        houseCount = 0;
        npcCount = 0;
        TownMats.Load();
        DungeonKit.Load();
        NpcKit.Reset();
        GateKit.Reset();
        PropKit.Reset();
        dSl = DungeonKit.Slots(0);
        baseSlots = dSl;
        mMarker = UMat("DMarker", new Color(0.4f, 0.9f, 1f));

        tTown = new GameObject("DungeonCity").transform;
        tEnv = Group("Environment", tTown);
        tWalls = Group("Walls", tTown);
        tGates = Group("Gates", tTown);
        tRoads = Group("Roads", tTown);
        tBuildings = Group("Buildings", tTown);
        tHouses = Group("Houses", tTown);
        tNoble = Group("Landmarks", tTown);
        tProps = Group("Props", tTown);
        tNpcs = Group("NPCs", tTown);
        tWp = Group("NPC_Waypoints", tTown);

        EditorUtility.DisplayProgressBar("Hunter Guild", "Tanah, tembok, gerbang...", 0.1f);
        DGround();
        DWallsAndGates();

        EditorUtility.DisplayProgressBar("Hunter Guild", "Menara Eldrath dan bangunan besar...", 0.25f);
        DReserveStructural();
        DBuildTower();
        DBuildLandmarks();

        EditorUtility.DisplayProgressBar("Hunter Guild", "Jalan dan alun-alun...", 0.45f);
        DBuildRoads();
        DBuildPlaza();

        EditorUtility.DisplayProgressBar("Hunter Guild", "Arkade, rumah warga...", 0.55f);
        DBuildArcades();
        PlacePropertyComplex(CityGate.DungeonCityScene, new Vector2(0f, -58f));   // Komplek Properti dipesan lebih dulu
        DBuildHouses();

        EditorUtility.DisplayProgressBar("Hunter Guild", "Lampu kristal, pohon arcane...", 0.7f);
        DBuildProps();
        DBuildOutside();

        EditorUtility.DisplayProgressBar("Hunter Guild", "Kanon Eldrath: garis putih, Kantor Pendaki, Jalan Pengrajin Lilin...", 0.8f);
        DungeonCanon();                                                            // tanpa UnityEngine.Random, jadi sebaran yang sudah ada tidak bergeser

        EditorUtility.DisplayProgressBar("Hunter Guild", "NPC dan pemain...", 0.85f);
        DBuildNpcs();
        DBuildPlayer();

        MarkStatic(tEnv); MarkStatic(tWalls); MarkStatic(tGates); MarkStatic(tRoads); MarkStatic(tBuildings);
        MarkStatic(tHouses); MarkStatic(tNoble); MarkStatic(tProps);
        BuildingMover.Prepare("DungeonCity", new[] { tBuildings, tHouses, tNoble }, new[] { tProps, tBuildings, tHouses, tNoble });

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0011f;
        RenderSettings.fogColor = new Color(0.58f, 0.7f, 0.82f);

        EditorUtility.DisplayProgressBar("Hunter Guild", "Menyimpan scene...", 0.96f);
        AssetDatabase.SaveAssets();
        HeightBake.BackupScene(DungeonScenePath); EditorSceneManager.SaveScene(scene, DungeonScenePath);
        AddToBuildSettings(DungeonScenePath);
        EditorUtility.ClearProgressBar();
        HGDlg.Show("Hunter Guild",
            "Kota Eldrath dibuat: " + DungeonScenePath + "\n" + houseCount + " rumah, " + npcCount + " NPC.\n" +
            "Bangun ulang scene Town (Build Town Scene) agar Gerbang Utara terhubung ke sini.", "OK");
    }

    // =====================================================================
    // Util
    // =====================================================================
    /// <summary>Collider silinder cembung (MeshCollider) agar bangunan bundar tidak diwakili kotak raksasa.</summary>
    static GameObject CylCollider(Transform parent, string name, Vector3 localBase, float r, float h)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localBase;
        var bag = new GeoBag();
        bag.Cyl(Sl.Stone, Vector3.zero, r, h, 24, r, true, true);
        int[] used;
        var mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh = bag.ToMesh("CylCol_" + name, null, out used);
        mc.convex = true;
        return go;
    }

    static GameObject BoxCol(Transform parent, string name, Vector3 localCenter, Vector3 size, Vector3 euler)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localCenter;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.AddComponent<BoxCollider>().size = size;
        return go;
    }

    // =====================================================================
    // Tanah, tembok, gerbang
    // =====================================================================
    static void DGround()
    {
        float h = 760f;
        var b = new GeoBag();
        b.Patch(Sl.Grass, V(-h, -h), V(-h, h), V(h, h), V(h, -h), 0f, V(1f, 0f));
        b.Emit(tEnv, "Ground", dSl);
        var col = new GameObject("GroundCollider");
        col.transform.SetParent(tEnv, false);
        col.transform.position = new Vector3(0f, -0.5f, 0f);
        col.AddComponent<BoxCollider>().size = new Vector3(h * 2f, 1f, h * 2f);

        // lantai marmer di dalam tembok
        var f = new GeoBag();
        f.Disc(Sl.Pave, Vector3.zero, DR - 3f, 72, 0.04f);
        f.Emit(tEnv, "Lantai", dSl);
    }

    static void DWallsAndGates()
    {
        float r0 = WallKit.R[2], h0 = WallKit.H[2], th0 = WallKit.Thick[2];
        WallKit.R[2] = DR; WallKit.H[2] = 26f; WallKit.Thick[2] = 8f;
        try
        {
            WallKit.BuildRing(tWalls, 2, dSl);
            var spec = GateKit.SpecFor(2);
            for (int k = 0; k < 4; k++)
            {
                float az = WallKit.GateAz[k];
                Vector2 p = WallKit.OnRing(2, az);
                string dir = WallKit.DirName[k];
                bool open = true;                        // semua gerbang Eldrath terbuka: S = Ashwatch (via Hutan Perbatasan), U/T/B = area hunting
                string[] dest = { "GUNUNG VULKANIK", "PADANG TULANG", "JALAN KE ASHWATCH", "LEMBAH IBLIS" };
                string[] gname = { "Gerbang Utara Eldrath", "Gerbang Timur Eldrath", "Gerbang Selatan Eldrath", "Gerbang Barat Eldrath" };
                string[] scn = { "HuntArea_Volcano", "HuntArea_Bone", "HuntArea_Frontier", "HuntArea_Demon" };
                var res = GateKit.Build(tGates, "Gerbang_" + dir, 2, "KOTA ELDRATH", dest[k], new Vector3(p.x, 0f, p.y), az, true, true, open);
                res.gate.displayName = gname[k];
                if (k != 2)
                {
                    // titik interaksi gerbang: syarat quest Guild Eldrath, lalu berangkat lewat koridor
                    var ipG = new GameObject("InteractPoint").transform;
                    ipG.SetParent(res.root.transform, false);
                    ipG.localPosition = new Vector3(0f, 0f, -(spec.D / 2f + 9f));
                    var cg = ipG.gameObject.AddComponent<CityGate>();
                    cg.prompt = gname[k];
                    cg.radius = 5f;
                    cg.huntSceneName = scn[k];
                    cg.gateDoor = res.gate;
                    Prim(PrimitiveType.Cylinder, "Marker", ipG, new Vector3(0f, 0.16f, 0f), new Vector3(3.5f, 0.03f, 3.5f), mMarker, false);
                }

                Vector2 outv = WallKit.Out(az);
                Vector2 cw = new Vector2(Mathf.Cos(az * Mathf.Deg2Rad), -Mathf.Sin(az * Mathf.Deg2Rad));
                Vector2 gp = p - outv * (spec.D / 2f + 3f);
                string role = open ? "Ksatria" : "Penjaga";
                MakeNpc("NPC_" + role + "_" + dir, role, V3(gp + cw * (spec.W / 2f + 1.6f)), null, open);
                MakeNpc("NPC_" + role + "_" + dir, role, V3(gp - cw * (spec.W / 2f + 1.6f)), null, open);
            }
        }
        finally { WallKit.R[2] = r0; WallKit.H[2] = h0; WallKit.Thick[2] = th0; }
    }

    static void DReserveStructural()
    {
        float th = 8f;
        int n = Mathf.RoundToInt(2f * Mathf.PI * DR / 7f);
        for (int i = 0; i < n; i++)
        {
            Vector2 p = WallKit.Out(i * 360f / n) * DR;
            Reserve(p.x, p.y, th / 2f + 3.4f);
        }
        var sp = GateKit.SpecFor(2);
        for (int k = 0; k < 4; k++)
        {
            float az = WallKit.GateAz[k];
            Vector2 p = WallKit.Out(az) * DR;       // WallKit.R[2] sudah dikembalikan ke nilai Ashwatch
            ReserveBox(p.x, p.y, sp.tX * 2f + sp.towerR * 2f, sp.D + 4f, az, 1f);
        }
        Reserve(0f, 0f, 49f);        // menara dan alasnya
    }

    // =====================================================================
    // Menara Eldrath
    // =====================================================================
    static void DBuildTower()
    {
        var root = new GameObject("MenaraEldrath").transform;
        root.SetParent(tNoble, false);
        root.position = Vector3.zero;
        root.rotation = Quaternion.Euler(0f, 180f, 0f);      // portal (+Z lokal) menghadap selatan, ke arah gerbang kota
        var model = new GameObject("Model").transform;
        model.SetParent(root, false);
        var bag = new GeoBag();
        DungeonKit.GrandTower(bag);
        bag.Emit(model, "Mesh", dSl, MeshDir + "/D_GrandTower.asset");

        var col = new GameObject("Collider").transform;
        col.SetParent(model, false);
        CylCol(col);

        // pintu masuk dungeon di teras depan portal
        var ip = new GameObject("InteractPoint").transform;
        ip.SetParent(root, false);
        ip.localPosition = new Vector3(0f, DungeonKit.TowerPlinthH, DungeonKit.TowerLandingZ);
        var ent = ip.gameObject.AddComponent<DungeonEntrance>();
        ent.prompt = "Masuk Menara Eldrath";
        ent.radius = 6f;
        Prim(PrimitiveType.Cylinder, "Marker", ip, new Vector3(0f, 0.16f, 0f), new Vector3(4.5f, 0.03f, 4.5f), mMarker, false);
        // titik muncul saat keluar dari Menara (scene DungeonTower): di teras, menghadap menjauhi pintu
        var spt = new GameObject("SpawnFromTower").transform;
        spt.SetParent(root, false);
        spt.localPosition = new Vector3(0f, DungeonKit.TowerPlinthH, DungeonKit.TowerLandingZ + 2.5f);
        spt.localRotation = Quaternion.identity;
        AddLight(root, new Vector3(0f, 14f, 36f), 40f, 2.2f);
        AddLight(root, new Vector3(0f, 8f, 52f), 30f, 1.6f);

        // pengawal elit di kaki tangga
        MakeNpc("NPC_PenjagaMenara", "Ksatria", V3(WorldFromTower(new Vector2(-9f, 58f))), null, true);
        MakeNpc("NPC_PenjagaMenara", "Ksatria", V3(WorldFromTower(new Vector2(9f, 58f))), null, true);
        PlacePropertyNpcs(V3(WorldFromTower(new Vector2(-18f, 58f))), V3(WorldFromTower(new Vector2(18f, 58f))), CityGate.DungeonCityScene);
        PlaceManaCrystals(V3(WorldFromTower(new Vector2(-27f, 58f))), V3(WorldFromTower(new Vector2(27f, 58f))), V3(WorldFromTower(new Vector2(-14f, 70f))), V3(WorldFromTower(new Vector2(14f, 70f))));
    }

    static Light DLight(Transform parent, Vector3 localPos, float range, float intensity, Color c)
    {
        AddLight(parent, localPos, range, intensity);
        var l = parent.GetChild(parent.childCount - 1).GetComponent<Light>();
        l.color = c;
        return l;
    }

    static Vector2 WorldFromTower(Vector2 local)
    {
        // yaw 180: (x,z) -> (-x,-z)
        return new Vector2(-local.x, -local.y);
    }

    static void CylCol(Transform col)
    {
        CylCollider(col, "Plinth", new Vector3(0f, 0f, 0f), 43f, DungeonKit.TowerPlinthH);
        CylCollider(col, "Body", new Vector3(0f, DungeonKit.TowerPlinthH, 0f), 26.5f, 96f);
        // landai tangga: naik 3,6 m sepanjang 14,1 m
        float dz = DungeonKit.TowerStairZ0 - DungeonKit.TowerStairZ1, dy = DungeonKit.TowerPlinthH;
        float len = Mathf.Sqrt(dz * dz + dy * dy);
        float ang = Mathf.Atan2(dy, dz) * Mathf.Rad2Deg;
        float zc = (DungeonKit.TowerStairZ0 + DungeonKit.TowerStairZ1) / 2f, yc = dy / 2f;
        Vector3 n = new Vector3(0f, Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));
        BoxCol(col, "Ramp", new Vector3(0f, yc, zc) - n * 0.5f, new Vector3(22f, 1f, len), new Vector3(ang, 0f, 0f));
        // pagar tangga
        for (int s = -1; s <= 1; s += 2)
            BoxCol(col, "Rail", new Vector3(s * 12.4f, yc + 0.8f, zc) - n * 0.2f, new Vector3(1.2f, 3f, len), new Vector3(ang, 0f, 0f));
    }

    // =====================================================================
    // Bangunan besar di cincin kedua
    // =====================================================================
    static Vector2 AtAz(float az, float r) { return WallKit.Out(az) * r; }

    static void DBuildLandmarks()
    {
        Vector2 p;
        float yaw;

        // ---- Cabang Guild Besar (tenggara dari gerbang)
        p = AtAz(155f, 142f); yaw = YawToward(p, Vector2.zero);
        var guild = Landmark<GuildBranch>("D_GuildBranch", tNoble, p, yaw, DungeonKit.GrandHall, DungeonKit.Slots(0),
            DungeonKit.GuildHallW, DungeonKit.GuildHallD, 22f, DungeonKit.GuildHallPorch + 12f, "Cabang Guild Eldrath");
        Sign(guild.transform.parent, "CABANG GUILD ELDRATH", new Vector3(0f, 16.3f, DungeonKit.GuildHallD / 2f + DungeonKit.GuildHallPorch + 0.9f), 0.5f);

        // ---- Penginapan (barat daya, dekat gerbang)
        p = AtAz(205f, 128f); yaw = YawToward(p, Vector2.zero);
        var inn = Landmark<Inn>("D_Inn", tNoble, p, yaw, LandmarkKit.Inn, DungeonKit.Slots(1),
            LandmarkKit.InnW, LandmarkKit.InnD, 14f, 3.5f, "Penginapan");
        Sign(inn.transform.parent, "PENGINAPAN CAHAYA BINTANG", new Vector3(0f, 6.4f, LandmarkKit.InnD / 2f + 1.0f), 0.24f);
        Door(inn, "Inn");

        // ---- Toko perlengkapan (timur-tenggara)
        p = AtAz(115f, 128f); yaw = YawToward(p, Vector2.zero);
        var shop = Landmark<GeneralShop>("D_Shop", tNoble, p, yaw, LandmarkKit.Shop, DungeonKit.Slots(2),
            LandmarkKit.ShopW, LandmarkKit.ShopD, 13f, 3.5f, "Toko Perlengkapan");
        Sign(shop.transform.parent, "PERLENGKAPAN DUNGEON", new Vector3(0f, 6.2f, LandmarkKit.ShopD / 2f + 1.0f), 0.26f);
        Door(shop, "Shop");

        // ---- Pandai besi (timur-timur laut)
        p = AtAz(65f, 128f); yaw = YawToward(p, Vector2.zero);
        var smith = Landmark<Blacksmith>("D_Smithy", tNoble, p, yaw, LandmarkKit.Smithy, DungeonKit.Slots(1),
            LandmarkKit.SmithW, LandmarkKit.SmithD, 13f, 3.5f, "Pandai Besi");
        Sign(smith.transform.parent, "PANDAI BESI ELDRATH", new Vector3(0f, 6.2f, LandmarkKit.SmithD / 2f + 1.0f), 0.26f);
        Door(smith, "Smithy");

        // ---- Perpustakaan (utara-timur laut)
        p = AtAz(25f, 128f); yaw = YawToward(p, Vector2.zero);
        var lib = Landmark<Library>("D_Library", tNoble, p, yaw, LandmarkKit.Academy, DungeonKit.Slots(0),
            LandmarkKit.AcadW, LandmarkKit.AcadD, 18f, 3.5f, "Perpustakaan Arkana");
        Sign(lib.transform.parent, "PERPUSTAKAAN", new Vector3(0f, 7.6f, LandmarkKit.AcadD / 2f + 1.0f), 0.3f);
        Door(lib, "Academy");

        // ---- Menara penyihir (utara-barat laut)
        p = AtAz(335f, 130f); yaw = YawToward(p, Vector2.zero);
        var mage = Landmark<InfoPoint>("D_MageTower", tNoble, p, yaw, LandmarkKit.MageTower, DungeonKit.Slots(2),
            LandmarkKit.MageR * 2f, LandmarkKit.MageR * 2f, 36f, 3f, "Menara Arkana");
        mage.title = "Menara Arkana";
        mage.message = "Para penyihir Eldrath meneliti inti kristal dungeon di sini.\n\n(Akan dibuka pada pembaruan berikutnya: enchant, identifikasi relik, dan tugas penelitian.)";

        // ---- Restoran (barat-barat laut)
        p = AtAz(295f, 128f); yaw = YawToward(p, Vector2.zero);
        var rest = Landmark<Restaurant>("D_Restaurant", tNoble, p, yaw, LandmarkKit.Tavern, DungeonKit.Slots(0),
            LandmarkKit.TavernW, LandmarkKit.TavernD, 13f, 3.5f, "Restoran");
        Sign(rest.transform.parent, "RESTORAN PENAKLUK", new Vector3(0f, 6.2f, LandmarkKit.TavernD / 2f + 1.0f), 0.26f);
        Door(rest, "Tavern");

        // ---- Kuil Cahaya (barat-barat daya)
        p = AtAz(245f, 150f); yaw = YawToward(p, Vector2.zero);
        var temple = Landmark<InfoPoint>("D_Temple", tNoble, p, yaw, LandmarkKit.Cathedral, DungeonKit.Slots(0),
            LandmarkKit.CathW, LandmarkKit.CathD, 34f, 4f, "Kuil Cahaya");
        temple.title = "Kuil Cahaya Eldrath";
        temple.message = "Tempat para penakluk menaruh doa sebelum turun ke dungeon.\n\n(Akan dibuka pada pembaruan berikutnya: berkat sebelum masuk dan pemurnian kutukan.)";
        Door(temple, "Cathedral");

        // ---- Koloseum / arena (timur laut, tepi luar)
        p = AtAz(58f, 190f); yaw = YawToward(p, Vector2.zero);
        var arena = Landmark<InfoPoint>("D_Coliseum", tNoble, p, yaw, DungeonKit.Coliseum, DungeonKit.Slots(0),
            72f, 72f, 18f, 12f, "Koloseum Eldrath");
        arena.title = "Koloseum Eldrath";
        arena.message = "Arena para penakluk menara. Spanduk turnamen sudah digantung.\n\n(Akan dibuka pada pembaruan berikutnya: duel arena, peringkat, dan hadiah musiman.)";
        Sign(arena.transform.parent, "KOLOSEUM", new Vector3(0f, 16.8f, 36f), 0.5f);

        // ---- Portal teleport antar-lantai (barat laut, tepi luar)
        p = AtAz(302f, 192f); yaw = YawToward(p, Vector2.zero);
        var portal = Landmark<InfoPoint>("D_Portal", tNoble, p, yaw, DungeonKit.PortalHub, DungeonKit.Slots(0),
            1.5f, 1.5f, 1f, 16f, "Portal Checkpoint");
        portal.title = "Portal Checkpoint";
        portal.message = "Lingkaran teleportasi ke lantai-lantai yang sudah kamu taklukkan.\n\n(Akan dibuka bersama sistem lantai dungeon: loncat ke checkpoint tiap 10 lantai.)";
        Reserve(p.x, p.y, 15f);

        // ---- Barak (barat daya jauh) dan Istal (tenggara jauh)
        p = AtAz(228f, 205f);
        var barr = Landmark<InfoPoint>("D_Barracks", tNoble, p, YawToward(p, Vector2.zero), LandmarkKit.Barracks, DungeonKit.Slots(1),
            LandmarkKit.BarrW, LandmarkKit.BarrD, 13f, 3.5f, "Barak Penjaga Menara");
        barr.title = "Barak Penjaga Menara";
        barr.message = "Markas ksatria yang berjaga di kaki menara. Mereka mencatat tiap Hunter yang masuk dan keluar.";
        p = AtAz(135f, 205f);
        var stab = Landmark<InfoPoint>("D_Stables", tNoble, p, YawToward(p, Vector2.zero), LandmarkKit.Stables, DungeonKit.Slots(1),
            LandmarkKit.StableW, LandmarkKit.StableD, 11f, 3.5f, "Istal");
        stab.title = "Istal Eldrath";
        stab.message = "Kuda-kuda pengangkut rampasan dungeon beristirahat di sini.";
    }

    // =====================================================================
    // Jalan
    // =====================================================================
    static void DBuildRoads()
    {
        roadC = new GeoBag(); roadD = new GeoBag(); roadP = new GeoBag();
        float hd = GateKit.SpecFor(2).D / 2f;

        // empat sumbu besar dari alun-alun menara ke gerbang
        for (int k = 0; k < 4; k++)
        {
            float az = WallKit.GateAz[k];
            string dn = WallKit.DirName[k];
            Axis(az, DPlazaR - 2f, DR - hd, 18f, 2, "AvenueAgung_" + dn, false);
        }
        // jalan dari tiap gerbang ke luar tembok (S = Hutan Perbatasan / Ashwatch, lainnya = area hunting)
        for (int k = 0; k < 4; k++)
        {
            var rd = NewStreet("JalanLuar_" + WallKit.DirName[k], 12f, 0, 0.1f, false, 0);
            Vector2 o = WallKit.Out(WallKit.GateAz[k]);
            rd.pts = Line(o * (DR + hd), o * (DR + DCorridorLen), 4f);
        }

        Ring("Lingkar98", 98f, 11f, 2, 0.08f, false, 0, 0f, 0f, 360f);
        Ring("Lingkar168", 168f, 8f, 0, 0.08f, true, 0, 0f, 0f, 360f);
        Ring("Lingkar218", 218f, 8f, 0, 0.08f, true, 0, 0f, 0f, 360f);
        for (int k = 0; k < 8; k++)
            Radial("Gang_" + k, 22.5f + k * 45f, 104f, 240f, 5f, 0, 0.06f, true);

        foreach (var s in streets)
        {
            if (s.pts.Count < 2) continue;
            var bag = BagFor(s.mat);
            roadP.Ribbon(Sl.Pave, s.pts, s.w + 0.9f, s.y - 0.025f);
            bag.Ribbon(s.mat == 0 ? Sl.Cobble : (s.mat == 1 ? Sl.Dirt : Sl.Pave), s.pts, s.w, s.y);
            if (s.mat == 2 && s.w >= 16f)
                bag.Ribbon(Sl.Cobble, s.pts, 2.4f, s.y + 0.01f);          // jalur tengah avenue
        }
        roadC.Emit(tRoads, "Roads_Cobble", dSl);
        roadD.Emit(tRoads, "Roads_Dirt", dSl);
        roadP.Emit(tRoads, "Roads_Pave", dSl);

        foreach (var s in streets)
            foreach (var pt in s.pts) Reserve(pt.x, pt.y, s.w / 2f + 0.4f);
        Reserve(0f, 0f, DPlazaR - 6f);
    }

    static void DBuildPlaza()
    {
        var b = new GeoBag();
        b.Disc(Sl.Pave, Vector3.zero, DPlazaR + 1.2f, 64, 0.11f);
        b.Disc(Sl.Cobble, Vector3.zero, DPlazaR, 64, 0.14f);
        b.Annulus(Sl.Pave, Vector3.zero, 47.5f, 52f, 0f, 360f, 56, 0.16f);
        b.Annulus(Sl.Pave, Vector3.zero, 60f, 62f, 0f, 360f, 56, 0.16f);
        b.Annulus(Sl.Pave, Vector3.zero, 76f, 80f, 0f, 360f, 64, 0.16f);
        // bintang delapan arah
        for (int k = 0; k < 8; k++)
        {
            float a = k * 45f * Mathf.Deg2Rad;
            Vector2 d = new Vector2(Mathf.Sin(a), Mathf.Cos(a)), n = new Vector2(d.y, -d.x);
            float w = 1.8f;
            b.Patch(Sl.Pave, d * 52f - n * w, d * 76f - n * w, d * 76f + n * w, d * 52f + n * w, 0.17f, d);
        }
        b.Emit(tRoads, "PlazaMenara", dSl);

        var root = Group("Alun-alun_Menara", tProps);
        // delapan obelisk
        var obelisk = PropKit.Get("D_Obelisk", DungeonKit.Obelisk);
        for (int k = 0; k < 8; k++)
        {
            Vector2 q = AtAz(22.5f + k * 45f, 66f);
            var go = PropKit.Place(obelisk, root, "Obelisk", V3(q, 0.15f), 45f * k, 1f, dSl);
            var bc = go.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 7f, 0f); bc.size = new Vector3(3.4f, 16f, 3.4f);
            Reserve(q.x, q.y, 3f);
        }
        // empat air mancur kristal
        var fountain = PropKit.Get("D_CrystalFountain", bg => DungeonKit.CrystalFountain(bg, 1f));
        for (int k = 0; k < 4; k++)
        {
            Vector2 q = AtAz(45f + k * 90f, 64f);
            PropKit.Place(fountain, root, "AirMancur", V3(q, 0.15f), 0f, 1f, dSl);
            CylCollider(root, "AirMancurCol", V3(q, 0.15f), 6.3f, 1.2f);
            DLight(root, V3(q, 4.4f), 18f, 1.6f, new Color(0.5f, 0.85f, 1f));
            Reserve(q.x, q.y, 7f);
        }
        // patung penjaga di mulut tiap avenue
        var statue = PropKit.Get("D_StatueK", bg => PropKit.KnightStatue(bg, Vector3.zero, 0f));
        for (int k = 0; k < 4; k++)
        {
            float az = k * 90f;
            Vector2 d = WallKit.Out(az), n = new Vector2(d.y, -d.x);
            foreach (float sx in new[] { -1f, 1f })
            {
                Vector2 q = d * 74f + n * sx * 11.5f;
                var go = PropKit.Place(statue, root, "Patung", V3(q, 0.15f), YawToward(q, Vector2.zero), 1.15f, dSl);
                CylCollider(root, "PatungCol", V3(q, 0.15f), 1.3f, 4f);
            }
        }
    }

    // =====================================================================
    // Arkade toko di tepi avenue
    // =====================================================================
    static void DBuildArcades()
    {
        var root = Group("Arkade", tBuildings);
        var proto = PropKit.Get("D_Arcade", DungeonKit.Arcade);
        int n = 0;
        for (int k = 0; k < 4; k++)
        {
            float az = k * 90f;
            Vector2 d = WallKit.Out(az), nrm = new Vector2(d.y, -d.x);
            foreach (float r in new[] { 138f })
                foreach (float sx in new[] { -1f, 1f })
                {
                    Vector2 c = d * r + nrm * sx * 16.5f;
                    float yaw = YawToward(c, d * r);
                    // arah panjang arkade (sumbu X lokal) sejajar avenue: yaw dari sisi ke tengah avenue
                    if (!BoxFree(c.x, c.y, 31f, 10f, yaw, 0f)) continue;
                    var go = PropKit.Place(proto, root, "Arkade_" + (++n), V3(c), yaw, 1f, DungeonKit.Slots(n % 3));
                    var bc = go.AddComponent<BoxCollider>();
                    bc.center = new Vector3(0f, 3f, 0f); bc.size = new Vector3(31f, 6f, 9.4f);
                    ReserveBox(c.x, c.y, 31f, 10f, yaw, 0f);
                }
        }
    }

    // =====================================================================
    // Rumah warga
    // =====================================================================
    static void DBuildHouses()
    {
        HouseKit.BuildAll();
        var rng = new System.Random(11);
        int rowCycle = 0;
        foreach (var st in streets)
        {
            if (!st.houses || st.pts.Count < 2) continue;
            var cum = new float[st.pts.Count];
            for (int i = 1; i < cum.Length; i++) cum[i] = cum[i - 1] + (st.pts[i] - st.pts[i - 1]).magnitude;
            float total = cum[cum.Length - 1];
            for (int side = -1; side <= 1; side += 2)
            {
                float sPos = 1f;
                int guard = 0;
                while (sPos < total - 2f && guard++ < 500)
                {
                    var def = HouseKit.Defs[(rowCycle++ * 2 + (side > 0 ? 1 : 0)) % HouseKit.Defs.Count];       // pola teratur
                    var fp = Footprint(def);
                    float fw = fp[0] * 2f;
                    float mid = sPos + fw / 2f;
                    if (mid > total) break;
                    Vector2 along, tan;
                    PointAlong(st.pts, cum, mid, out along, out tan);
                    Vector2 nrm = new Vector2(tan.y, -tan.x) * side;
                    Vector2 c = along + nrm * (st.w / 2f + 1.6f + fp[2]);
                    float yaw = YawToward(c, along);
                    Vector2 fc = FootCenter(c, yaw, fp);
                    if (!CornersInBand(fc.x, fc.y, fw, fp[2] - fp[1], yaw, 108f, DR - 10f)) { sPos += 3f; continue; }
                    if (!BoxFree(fc.x, fc.y, fw, fp[2] - fp[1], yaw, 0f)) { sPos += 3f; continue; }
                    DPlaceHouse(def, c, yaw, rng);
                    ReserveBox(fc.x, fc.y, fw, fp[2] - fp[1], yaw, 0f);
                    sPos += fw + 1.6f;
                }
            }
        }
    }

    static void DPlaceHouse(HouseKit.HouseDef def, Vector2 c, float yaw, System.Random rng)
    {
        int v = rng.Next(3);
        var slots = DungeonKit.Slots(v);
        slots[Sl.Cloth] = TownMats.Cloths[rng.Next(2) == 0 ? 1 : 4];
        slots[Sl.ClothB] = TownMats.Cloths[rng.Next(2) == 0 ? 2 : 1];
        if (def.kind == 3) slots[Sl.Roof] = TownMats.Thatch;
        var go = PropKit.Place(def.proto, tHouses, "Rumah_" + (++houseCount), V3(c), yaw, 1f, slots);
        var bc = go.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, def.height / 2f, 0f);
        bc.size = new Vector3(def.w, def.height, def.d);
    }

    // =====================================================================
    // Properti: lampu kristal, pohon arcane, banner
    // =====================================================================
    static void DBuildProps()
    {
        var lamps = Group("LampuKristal", tProps);
        var lamp = PropKit.Get("D_CrystalLamp", DungeonKit.CrystalLamp);
        int n = 0;
        foreach (var s in streets)
        {
            if (s.pts.Count < 2) continue;
            if (s.name == "JalanKePerbatasan") continue;
            bool wide = s.w >= 11f;
            float spacing = wide ? 22f : 30f;
            float acc = spacing * 0.5f;
            for (int i = 0; i + 1 < s.pts.Count; i++)
            {
                Vector2 a = s.pts[i], b = s.pts[i + 1];
                acc += (b - a).magnitude;
                if (acc < spacing) continue;
                acc = 0f;
                Vector2 tan = (b - a).normalized;
                Vector2 nrm = new Vector2(tan.y, -tan.x);
                for (int sd = 0; sd < 2; sd++)
                {
                    if (!wide && sd == 1) continue;
                    float sgn = sd == 0 ? 1f : -1f;
                    Vector2 p = (a + b) * 0.5f + nrm * sgn * (s.w / 2f + 0.6f);
                    var go = PropKit.Place(lamp, lamps, "Lampu", V3(p, s.y), 0f, 1f, dSl);
                    if ((n++ % 3) == 0)
                    {
                        DLight(go.transform, new Vector3(0f, 4.4f, 0f), 16f, 1.7f, new Color(0.55f, 0.85f, 1f));
                    }
                }
            }
        }

        // pohon arcane (daun biru-hijau dan sakura ungu) di ruang kosong
        var trees = Group("PohonArcane", tProps);
        int placed = 0;
        for (int i = 0; i < 4000 && placed < 120; i++)
        {
            float a = Random.Range(0f, 360f), r = Random.Range(106f, DR - 16f);
            Vector2 p = Pol(a, r);
            if (!IsFree(p.x, p.y, 3.4f)) continue;
            Reserve(p.x, p.y, 3.4f);
            int kind = Random.value < 0.5f ? 2 : (Random.value < 0.6f ? 0 : 5);
            var go = PropKit.Place(PropKit.TreeProto(kind), trees, "Pohon", V3(p), Random.Range(0f, 360f), Random.Range(0.85f, 1.15f), dSl);
            var cc = go.AddComponent<CapsuleCollider>();
            cc.center = new Vector3(0f, 1.5f, 0f); cc.radius = 0.5f; cc.height = 3f;
            placed++;
        }

        // gugusan kristal dekoratif
        var crystals = Group("Kristal", tProps);
        var cl = PropKit.Get("D_CrystalCluster", bg => DungeonKit.CrystalCluster(bg, 1.2f));
        int cn = 0;
        for (int i = 0; i < 1500 && cn < 30; i++)
        {
            float a = Random.Range(0f, 360f), r = Random.Range(104f, DR - 14f);
            Vector2 p = Pol(a, r);
            if (!IsFree(p.x, p.y, 2.6f)) continue;
            Reserve(p.x, p.y, 2.6f);
            PropKit.Place(cl, crystals, "Kristal", V3(p), Random.Range(0f, 360f), Random.Range(0.8f, 1.3f), dSl);
            cn++;
        }
    }

    // =====================================================================
    // Di luar tembok: jalan tamu, hutan gelap, pegunungan
    // =====================================================================
    static void DBuildOutside()
    {
        var g = Group("Luar", tEnv);
        var rng = new System.Random(808);

        // pohon di sekeliling tembok, kecuali jalur tamu
        int placed = 0;
        for (int i = 0; i < 4000 && placed < 560; i++)
        {
            float a = (float)rng.NextDouble() * 360f, r = DR + 20f + (float)rng.NextDouble() * 330f;
            Vector2 p = Pol(a, r);
            if ((Mathf.Abs(p.x) < 34f && Mathf.Abs(p.y) > DR) || (Mathf.Abs(p.y) < 34f && Mathf.Abs(p.x) > DR)) continue;
            if (Mathf.Abs(p.x) > 740f || Mathf.Abs(p.y) > 740f) continue;
            int kind = rng.Next(100) < 70 ? 1 : 3;
            PropKit.Place(PropKit.TreeProto(kind), g, "Pohon", V3(p), (float)rng.NextDouble() * 360f, 0.95f + (float)rng.NextDouble() * 0.6f, dSl);
            placed++;
        }

        // pegunungan jauh
        var mb = new GeoBag();
        for (int i = 0; i < 28; i++)
        {
            float a = i * 360f / 28f + (float)rng.NextDouble() * 8f;
            float dist = 640f + (float)rng.NextDouble() * 70f;
            Vector2 p = Pol(a, dist);
            float rr = 70f + (float)rng.NextDouble() * 50f, hh = 120f + (float)rng.NextDouble() * 120f;
            mb.Cyl(Sl.DarkStone, new Vector3(p.x, -2f, p.y), rr, hh * 0.72f, 7, rr * 0.35f, false);
            mb.Cyl(Sl.DarkStone, new Vector3(p.x, hh * 0.72f - 2f, p.y), rr * 0.35f, hh * 0.28f, 7, 0f, false);
            mb.Cone(Sl.Marble, new Vector3(p.x, hh * 0.86f - 2f, p.y), rr * 0.2f, hh * 0.14f, 7);
        }
        mb.Emit(g, "Pegunungan", dSl);

        // koridor keluar tiap gerbang: lampu kristal, cemara, tanda, transisi
        string[] areaLabel = { "GUNUNG VULKANIK", "PADANG TULANG", "HUTAN PERBATASAN", "LEMBAH IBLIS" };
        string[] areaScene = { "HuntArea_Volcano", "HuntArea_Bone", "HuntArea_Frontier", "HuntArea_Demon" };
        string[] areaSpawn = { "SpawnFromTown", "SpawnFromTown", "SpawnFromEldrath", "SpawnFromTown" };
        string[] backSpawn = { "SpawnFromVolcano", "SpawnFromBone", "SpawnFromTown", "SpawnFromDemon" };
        for (int k = 0; k < 4; k++)
        {
            Vector2 o = WallKit.Out(WallKit.GateAz[k]);
            Vector2 lat = new Vector2(-o.y, o.x);
            float yaw = Mathf.Atan2(o.x, o.y) * Mathf.Rad2Deg;
            var road = Group("JalanTamu_" + WallKit.DirName[k], g);
            var lamp = PropKit.Get("D_CrystalLamp", DungeonKit.CrystalLamp);
            for (float d = 14f; d <= 84f; d += 14f)
                foreach (float sx in new[] { -1f, 1f })
                {
                    Vector2 q = o * (DR + d) + lat * (sx * 8f);
                    var go = PropKit.Place(lamp, road, "Lampu", new Vector3(q.x, 0.1f, q.y), 0f, 1f, dSl);
                    if (((int)d / 14) % 2 == 0) DLight(go.transform, new Vector3(0f, 4.4f, 0f), 14f, 1.5f, new Color(0.55f, 0.85f, 1f));
                }
            for (float d = 10f; d <= 90f; d += 8f)
                foreach (float sx in new[] { -1f, 1f })
                {
                    Vector2 q = o * (DR + d) + lat * (sx * (14.5f + (float)(rng.NextDouble() * 1.4)));
                    PropKit.Place(PropKit.TreeProto(3), road, "Sipres", new Vector3(q.x, 0f, q.y), (float)rng.NextDouble() * 360f, 1f, dSl);
                }
            Vector2 w1 = o * (DR + 48f) + lat * -19f, w2 = o * (DR + 48f) + lat * 19f, w3 = o * (DR + DCorridorLen + 1f);
            InvisibleWallY(new Vector3(w1.x, 5f, w1.y), new Vector3(1f, 12f, 92f), yaw);
            InvisibleWallY(new Vector3(w2.x, 5f, w2.y), new Vector3(1f, 12f, 92f), yaw);
            InvisibleWallY(new Vector3(w3.x, 5f, w3.y), new Vector3(40f, 12f, 1f), yaw);
            Vector2 sg = o * (DR + 34f) + lat * -10f;
            RoadSignAt("Tanda_" + WallKit.DirName[k], areaLabel[k], sg, yaw);

            // zona transisi ke area tujuan (selatan = perjalanan aman, lainnya wajib quest Guild Eldrath)
            Vector2 tp = o * (DR + 80f);
            var t = new GameObject("To_" + areaScene[k]).transform;
            t.SetParent(tTown, false);
            t.position = new Vector3(tp.x, 0f, tp.y);
            t.rotation = Quaternion.Euler(0f, yaw, 0f);
            var at = t.gameObject.AddComponent<AreaTransition>();
            at.mode = AreaTransition.Mode.ToForest;
            at.destScene = areaScene[k];
            at.destSpawn = areaSpawn[k];
            at.safeTravel = k == 2;
            at.size = new Vector3(30f, 12f, 4f);

            // titik muncul saat datang kembali dari area tersebut (menghadap kota)
            Vector2 sp0 = o * (DR + 60f);
            var sp = new GameObject(backSpawn[k]).transform;
            sp.SetParent(tTown, false);
            sp.position = new Vector3(sp0.x, 0f, sp0.y);
            sp.rotation = Quaternion.Euler(0f, yaw + 180f, 0f);
        }
    }

    // =====================================================================
    // NPC dan pemain
    // =====================================================================
    static void DBuildNpcs()
    {
        // alun-alun menara
        string[] who = { "Hunter", "Peziarah", "Pedagang", "Pelajar" };
        for (int i = 0; i < 4; i++)
        {
            var r = RoutePts("Menara" + i, ArcPts(Vector2.zero, 56f + i * 6f, i * 90f + 5f, i * 90f + 85f, 9));
            Npcs(who[i], 3, r);
            Npcs("Hunter", 1, r);
        }
        Npcs("Hunter", 4, RoutePts("Lingkar98", ArcPts(Vector2.zero, 98f, 10f, 350f, 22)));
        Npcs("Warga", 3, RoutePts("Lingkar168a", ArcPts(Vector2.zero, 168f, 20f, 160f, 12)));
        Npcs("Pedagang", 3, RoutePts("Lingkar168b", ArcPts(Vector2.zero, 168f, 200f, 340f, 12)));
        Npcs("Warga", 3, RoutePts("Lingkar218", ArcPts(Vector2.zero, 218f, 15f, 345f, 22)));
        Npcs("Bangsawan", 2, RoutePts("AvenueTimur", Line(WallKit.Out(90f) * 90f, WallKit.Out(90f) * 235f, 20f)));
        Npcs("Pedagang", 2, RoutePts("AvenueBarat", Line(WallKit.Out(270f) * 90f, WallKit.Out(270f) * 235f, 20f)));
        Npcs("Hunter", 3, RoutePts("AvenueUtara", Line(WallKit.Out(0f) * 90f, WallKit.Out(0f) * 235f, 20f)));
        Npcs("Hunter", 3, RoutePts("AvenueSelatan", Line(WallKit.Out(180f) * 90f, WallKit.Out(180f) * 235f, 20f)));
    }

    static void DBuildPlayer()
    {
        float gz = -DR - 60f;
        Material playerMat = UMat("Player", new Color(0.2f, 0.4f, 0.85f));
        var pl = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        pl.name = "Player";
        pl.tag = "Player";
        pl.transform.position = new Vector3(0f, 1.1f, gz);
        pl.GetComponent<Renderer>().sharedMaterial = playerMat;
        Object.DestroyImmediate(pl.GetComponent<CapsuleCollider>());
        var cc = pl.AddComponent<CharacterController>();
        cc.height = 2f; cc.radius = 0.4f; cc.center = Vector3.zero; cc.stepOffset = 0.4f;
        pl.AddComponent<PlayerController>();
        pl.AddComponent<HunterGuild.PlayerAvatar>();
        Prim(PrimitiveType.Cube, "FacingMarker", pl.transform, new Vector3(0f, 0.5f, 0.45f), new Vector3(0.3f, 0.3f, 0.5f), mMarker, false);

        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0f, 3f, gz - 14f);
            cam.farClipPlane = 1800f;
            cam.gameObject.AddComponent<ThirdPersonCamera>().target = pl.transform;
        }

        new GameObject("GameManager").AddComponent<GameState>();
        new GameObject("TownUI").AddComponent<TownUI>();
        new GameObject("BackpackUI").AddComponent<BackpackUI>();
        new GameObject("SkillUI").AddComponent<SkillUI>();
        var sunGo = GameObject.Find("Directional Light");
        if (sunGo != null)
        {
            var dn = new GameObject("DayNight").AddComponent<TownDayNight>();
            dn.sun = sunGo.GetComponent<Light>();
        }
    }
}
