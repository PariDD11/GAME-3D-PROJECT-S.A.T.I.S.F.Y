using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using HunterGuild;

/// <summary>
/// Menu: Hunter Guild > Build Guild Interior Scene
/// Membangun interior Guild Petualang (placeholder primitif) ke Assets/Scenes/GuildInterior.unity.
///
/// Denah (utara = +Z, pintu di selatan):
///   Lobi        x -16..16, z -14..14   pintu keluar (0,-14), resepsionis + 5 poster rekrutmen di utara,
///                                      Papan Quest di dinding barat, Papan Pengumuman di dinding timur,
///                                      Papan Peringkat di pojok barat laut, Penilai Bahan di pojok timur laut,
///                                      lounge + perapian di barat daya
///   Kantor GM   x 16..28,  z 4..14     lewat lengkung di dinding timur (z 7..11)
///   Arena       x 16..46,  z -28..-2   lewat bukaan di dinding timur (z -11..-5): area latihan + Pengawas Ujian
///
/// Hierarki: GuildInterior > Structure | Props | Boards | Lights | NPCs | NPC_Waypoints | Interactables
/// Semua bangunan placeholder; ganti dengan model sendiri dengan cara yang sama seperti Town (hapus kotak, taruh prefab).
/// </summary>
public static class GuildSceneBuilder
{
    static string ScenePath = "Assets/Scenes/GuildInterior.unity";
    const string EldrathPath = "Assets/Scenes/Interior_GuildEldrath.unity";
    static bool grand;                 // true = Balai Agung Guild Eldrath (mewah)
    static string P = "G";             // awalan nama material
    const string TownPath = "Assets/Scenes/Town.unity";
    const string MatDir = "Assets/Materials/Guild";

    // Lobi
    const float LX = 16f, LZ = 14f, T = 0.6f;
    static float H = 9f;

    static Transform tStruct, tProps, tBoards, tLights, tNpcs, tWay, tAct;
    static readonly List<Light> windowLights = new List<Light>();
    static readonly List<GameObject> deskOpen = new List<GameObject>();
    static readonly List<GameObject> deskClosed = new List<GameObject>();

    static Material mFloor, mFloorDark, mStone, mStoneDark, mWood, mDarkWood, mCarpet, mGold, mPaper, mPaperY, mPaperB;
    static Material mCloth, mMarker, mFire, mWindow, mIron, mSlate, mSand, mSkin, mRed;
    static Material[] mNpc;

    [MenuItem("Hunter Guild/Build Guild Interior Scene")]
    public static void Build() { Run(false); }

    [MenuItem("Hunter Guild/Build Guild Interior (Eldrath - Balai Agung)")]
    public static void BuildEldrath() { Run(true); }

    static void Run(bool g)
    {
        grand = g; H = g ? 12f : 9f; P = g ? "GE" : "G";
        ScenePath = g ? EldrathPath : "Assets/Scenes/GuildInterior.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            if (!HGDlg.Show("Hunter Guild",
                Path.GetFileName(ScenePath) + " sudah ada.\nMembangun ulang akan MENIMPA scene itu, termasuk perubahan yang sudah kamu buat.\n\nLanjutkan?",
                "Timpa", "Batal")) return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Materials");
        EnsureFolder(MatDir);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        windowLights.Clear(); deskOpen.Clear(); deskClosed.Clear();
        MakeMaterials();
        TownMats.Load();
        NpcKit.Reset();

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.45f, 0.40f, 0.36f);
        RenderSettings.fog = false;

        var root = new GameObject(grand ? "GuildInterior_Eldrath" : "GuildInterior").transform;
        tStruct = Group("Structure", root);
        tProps = Group("Props", root);
        tBoards = Group("Boards", root);
        tLights = Group("Lights", root);
        tNpcs = Group("NPCs", root);
        tWay = Group("NPC_Waypoints", root);
        tAct = Group("Interactables", root);

        BuildLobbyShell();
        BuildOfficeShell();
        BuildArenaShell();
        BuildLobbyProps();
        if (grand) BuildGrandExtras(); else BuildTownExtras();
        BuildReception();
        BuildBoards();
        BuildLounge();
        BuildAppraiser();
        BuildOffice();
        BuildArena();
        BuildNpcs();
        BuildPlayerAndManagers();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings(TownPath, 0);
        AddToBuildSettings(ScenePath, -1);
        HGDlg.Show("Hunter Guild",
            "Scene interior Guild dibuat: " + ScenePath + "\nTown.unity dan GuildInterior.unity sudah masuk Build Settings.\n\n" +
            "Mulai dari Town, jalan ke pintu Guild lalu tekan E. Atau buka GuildInterior.unity dan tekan Play.", "OK");
    }

    // =====================================================================
    // Material
    static void MakeMaterials()
    {
        mFloor = Mat(P + "Floor", grand ? new Color(0.86f, 0.84f, 0.80f) : new Color(0.36f, 0.24f, 0.14f));
        mFloorDark = Mat(P + "FloorDark", grand ? new Color(0.22f, 0.26f, 0.42f) : new Color(0.28f, 0.18f, 0.10f));
        mStone = Mat(P + "Stone", grand ? new Color(0.90f, 0.89f, 0.86f) : new Color(0.66f, 0.62f, 0.55f));
        mStoneDark = Mat(P + "StoneDark", grand ? new Color(0.34f, 0.36f, 0.48f) : new Color(0.40f, 0.38f, 0.36f));
        mWood = Mat(P + "Wood", new Color(0.45f, 0.30f, 0.16f));
        mDarkWood = Mat(P + "DarkWood", new Color(0.22f, 0.13f, 0.07f));
        mCarpet = Mat(P + "Carpet", grand ? new Color(0.10f, 0.16f, 0.52f) : new Color(0.55f, 0.10f, 0.12f));
        mGold = Mat(P + "Gold", grand ? new Color(0.98f, 0.80f, 0.28f) : new Color(0.85f, 0.68f, 0.20f));
        mPaper = Mat(P + "Paper", new Color(0.90f, 0.85f, 0.70f));
        mPaperY = Mat(P + "PaperY", new Color(0.92f, 0.82f, 0.45f));
        mPaperB = Mat(P + "PaperB", new Color(0.70f, 0.78f, 0.88f));
        mCloth = Mat(P + "Cloth", grand ? new Color(0.14f, 0.20f, 0.62f) : new Color(0.60f, 0.12f, 0.14f));
        mMarker = Mat(P + "Marker", new Color(1f, 0.85f, 0.2f));
        mFire = Mat(P + "Fire", new Color(1f, 0.55f, 0.15f));
        mWindow = Mat(P + "Window", new Color(0.55f, 0.70f, 0.95f));
        mIron = Mat(P + "Iron", new Color(0.25f, 0.25f, 0.28f));
        mSlate = Mat(P + "Slate", new Color(0.20f, 0.22f, 0.27f));
        mSand = Mat(P + "Sand", new Color(0.78f, 0.68f, 0.48f));
        mSkin = Mat(P + "Skin", new Color(0.85f, 0.68f, 0.55f));
        mRed = Mat(P + "Red", new Color(0.70f, 0.18f, 0.16f));
        mNpc = new[]
        {
            Mat(P + "NpcBlue", new Color(0.25f, 0.35f, 0.65f)), Mat(P + "NpcGreen", new Color(0.30f, 0.55f, 0.35f)),
            Mat(P + "NpcPurple", new Color(0.55f, 0.38f, 0.65f)), Mat(P + "NpcBrown", new Color(0.50f, 0.36f, 0.22f)),
            Mat(P + "NpcGrey", new Color(0.45f, 0.45f, 0.50f)), Mat(P + "NpcWhite", new Color(0.85f, 0.85f, 0.80f))
        };
    }

    // =====================================================================
    // Helper geometri
    static Transform Group(string name, Transform parent)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }

    static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Material m, bool collider = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = m;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    /// <summary>Silinder berdiri: basePos = titik dasar, tinggi dalam meter.</summary>
    static GameObject Cyl(string name, Transform parent, Vector3 basePos, float radius, float height, Material m, bool collider = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = basePos + Vector3.up * (height / 2f);
        go.transform.localScale = new Vector3(radius * 2f, height / 2f, radius * 2f);
        go.GetComponent<Renderer>().sharedMaterial = m;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static GameObject Sphere(string name, Transform parent, Vector3 pos, float diameter, Material m, bool collider = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = Vector3.one * diameter;
        go.GetComponent<Renderer>().sharedMaterial = m;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    // Dinding sepanjang sumbu Z (menghadap utara/selatan) dan sumbu X (menghadap timur/barat).
    static void WallZ(string name, float z, float x0, float x1, float y0, float y1, Material m = null)
    {
        Box(name, tStruct, new Vector3((x0 + x1) / 2f, (y0 + y1) / 2f, z), new Vector3(x1 - x0, y1 - y0, T), m != null ? m : mStone);
    }

    static void WallX(string name, float x, float z0, float z1, float y0, float y1, Material m = null)
    {
        Box(name, tStruct, new Vector3(x, (y0 + y1) / 2f, (z0 + z1) / 2f), new Vector3(T, y1 - y0, z1 - z0), m != null ? m : mStone);
    }

    /// <summary>Teks 3D yang terbaca oleh pengamat di arah viewerDir dari objek.</summary>
    static TextMesh Text3D(string name, string text, Transform parent, Vector3 pos, Vector3 viewerDir, float charSize, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.rotation = Quaternion.LookRotation(-viewerDir, Vector3.up);
        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 64;
        tm.characterSize = charSize;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
#if UNITY_2022_2_OR_NEWER
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
        tm.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        HunterGuild.TextMat.Apply(go.GetComponent<MeshRenderer>(), tm.font.material);
        return tm;
    }

    static Light AddLight(Transform parent, Vector3 pos, float range, float intensity, bool flicker = false)
    {
        var go = new GameObject("PointLight");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = range;
        l.intensity = intensity;
        l.color = new Color(1f, 0.8f, 0.5f);
        if (flicker) go.AddComponent<FlickerLight>();
        return l;
    }

    static void Torch(Vector3 pos, Vector3 outward)
    {
        var t = new GameObject("Torch").transform;
        t.SetParent(tLights, false);
        t.localPosition = pos;
        Box("Bracket", t, outward * 0.15f, new Vector3(0.12f, 0.5f, 0.12f), mIron, false);
        Sphere("Flame", t, outward * 0.15f + Vector3.up * 0.4f, 0.3f, mFire);
        AddLight(t, outward * 0.5f + Vector3.up * 0.5f, 7f, 1.0f, true);
    }

    static T MakeInteract<T>(string name, Vector3 pos, string prompt, float radius) where T : Interactable
    {
        var go = new GameObject("InteractPoint_" + name);
        go.transform.SetParent(tAct, false);
        go.transform.position = pos;
        var c = go.AddComponent<T>();
        c.prompt = prompt;
        c.radius = radius;
        Cyl("Marker", go.transform, new Vector3(0, 0.02f, 0), 1.2f, 0.03f, mMarker, false);
        return c;
    }

    static System.Random gRng = new System.Random(4242);

    static NpcKit.Spec GuildSpec(string name)
    {
        int o, race = -1, fem = -1;
        switch (name)
        {
            case "NPC_Resepsionis": o = NpcKit.OPetugas; race = NpcKit.Human; fem = 1; break;
            case "NPC_Penilai": o = NpcKit.OPelajar; race = NpcKit.Dwarf; fem = 0; break;
            case "NPC_GuildMaster": o = NpcKit.OBangsawan; race = NpcKit.Human; fem = 0; break;
            case "NPC_Pelatih": o = NpcKit.OPenjaga; race = NpcKit.Beast; fem = 0; break;
            case "NPC_PengawasUjian": o = NpcKit.OPetugas; race = NpcKit.Elf; fem = 1; break;
            case "NPC_Rival": o = NpcKit.OHunter; race = NpcKit.Human; fem = 0; break;
            default: o = NpcKit.OHunter; break;
        }
        var sp = NpcKit.RandomSpec(o, gRng, race, fem);
        switch (name)
        {
            case "NPC_Resepsionis": sp.name = "Lyra"; sp.hair = 2; sp.hairCol = 3; break;
            case "NPC_Penilai": sp.name = "Brannoc"; sp.beard = 3; sp.hairCol = 5; break;
            case "NPC_GuildMaster": sp.name = "Orwen"; sp.beard = 2; sp.hairCol = 6; sp.hair = 0; sp.hat = 0; sp.cA = 3; sp.cB = 5; break;
            case "NPC_Pelatih": sp.name = "Harkon"; sp.beast = 1; sp.hat = 0; sp.hair = 5; break;
            case "NPC_PengawasUjian": sp.name = "Vesna"; sp.hair = 1; sp.hairCol = 4; break;
            case "NPC_Rival": sp.name = "Corvin Thorne"; sp.hair = 5; sp.hairCol = 0; sp.hat = 0; sp.cA = 10; sp.cB = 4; break;
        }
        return sp;
    }

    static GameObject MakeNpc(string name, string label, Vector3 pos, float yaw, Material body, Transform[] route)
    {
        var spec = GuildSpec(name);
        var go = NpcKit.Spawn(tNpcs, name, pos, yaw, spec, true);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        var actor = go.GetComponent<NpcActor>();
        var lab = Text3D("Label", label, go.transform, new Vector3(0, actor.height + 0.3f, 0), Vector3.back, 0.05f, Color.white);
        var npc = go.AddComponent<WanderNPC>();
        npc.waypoints = route;
        npc.label = lab.transform;
        npc.speed = 1.1f;
        bool key = name != null && !name.StartsWith("NPC_Hunter");
        if (!key) go.AddComponent<NpcChat>();
        return go;
    }

    static Transform[] Route(string name, params Vector3[] pts)
    {
        var arr = new Transform[pts.Length];
        for (int i = 0; i < pts.Length; i++)
        {
            var g = new GameObject(name + "_" + i).transform;
            g.SetParent(tWay, false);
            g.position = pts[i];
            arr[i] = g;
        }
        return arr;
    }

    // =====================================================================
    // Cangkang ruangan
    static void BuildLobbyShell()
    {
        Box("Floor", tStruct, new Vector3(0, -0.25f, 0), new Vector3(2 * LX + 2 * T, 0.5f, 2 * LZ + 2 * T), mFloor);
        Box("Ceiling", tStruct, new Vector3(0, H + 0.25f, 0), new Vector3(2 * LX + 2 * T, 0.5f, 2 * LZ + 2 * T), mStoneDark);
        // Papan lantai
        for (int i = -7; i <= 7 && !grand; i++)
            Box("Plank", tProps, new Vector3(i * 2f, 0.005f, 0), new Vector3(0.04f, 0.012f, 2 * LZ), mFloorDark, false);

        WallZ("WallN", LZ + T / 2f, -LX - T, LX + T, 0, H);
        WallX("WallW", -LX - T / 2f, -LZ - T, LZ + T, 0, H);
        // Selatan: bukaan pintu x -2.8..2.8 setinggi 5 m
        WallZ("WallS_L", -LZ - T / 2f, -LX - T, -2.8f, 0, H);
        WallZ("WallS_R", -LZ - T / 2f, 2.8f, LX + T, 0, H);
        WallZ("WallS_Top", -LZ - T / 2f, -2.8f, 2.8f, 5f, H);
        // Timur: bukaan Arena (z -11..-5, tinggi 5,2) dan lengkung Kantor GM (z 7..11, tinggi 4,8)
        float ex = LX + T / 2f;
        WallX("WallE_1", ex, -LZ - T, -11f, 0, H);
        WallX("WallE_2", ex, -5f, 7f, 0, H);
        WallX("WallE_3", ex, 11f, LZ + T, 0, H);
        WallX("WallE_ArenaTop", ex, -11f, -5f, 5.2f, H);
        WallX("WallE_OfficeTop", ex, 7f, 11f, 4.8f, H);

        // Pintu ganda tertutup (keluar lewat E di titik interaksi)
        var door = Group("MainDoor", tStruct);
        Box("DoorL", door, new Vector3(-1.4f, 2.3f, -LZ - 0.1f), new Vector3(2.76f, 4.6f, 0.2f), mDarkWood);
        Box("DoorR", door, new Vector3(1.4f, 2.3f, -LZ - 0.1f), new Vector3(2.76f, 4.6f, 0.2f), mDarkWood);
        Box("Band1", door, new Vector3(0, 1.2f, -LZ + 0.02f), new Vector3(5.6f, 0.15f, 0.06f), mIron, false);
        Box("Band2", door, new Vector3(0, 3.4f, -LZ + 0.02f), new Vector3(5.6f, 0.15f, 0.06f), mIron, false);
        Box("PostL", door, new Vector3(-3.05f, 2.5f, -LZ - 0.15f), new Vector3(0.5f, 5f, 0.7f), mWood);
        Box("PostR", door, new Vector3(3.05f, 2.5f, -LZ - 0.15f), new Vector3(0.5f, 5f, 0.7f), mWood);
        Box("Arch", door, new Vector3(0, 5.2f, -LZ - 0.15f), new Vector3(6.6f, 0.5f, 0.7f), mWood);
        Text3D("ExitSign", "KELUAR KE KOTA", tProps, new Vector3(0, 6.3f, -LZ + 0.1f), Vector3.forward, 0.07f, new Color(1f, 0.9f, 0.6f));
        MakeInteract<GuildDoor>("Door", new Vector3(0, 0, -12.6f), "Keluar ke Kota", 2.0f);
    }

    static void BuildOfficeShell()
    {
        Box("OfficeFloor", tStruct, new Vector3(22f, -0.25f, 9f), new Vector3(12.6f, 0.5f, 10.6f), mFloor);
        Box("OfficeCeiling", tStruct, new Vector3(22f, 6.25f, 9f), new Vector3(12.6f, 0.5f, 10.6f), mStoneDark);
        WallZ("OfficeWallS", 3.7f, 16f, 28.6f, 0, 6f);
        WallZ("OfficeWallN", 14.3f, 16.6f, 28.6f, 0, 6f);
        WallX("OfficeWallE", 28.3f, 3.4f, 14.6f, 0, 6f);
    }

    static void BuildArenaShell()
    {
        Box("ArenaFloor", tStruct, new Vector3(31.3f, -0.25f, -15f), new Vector3(30.6f, 0.5f, 27.2f), mStoneDark);
        Box("ArenaCeiling", tStruct, new Vector3(31.3f, H + 0.25f, -15f), new Vector3(30.6f, 0.5f, 27.2f), mStoneDark);
        WallZ("ArenaWallN", -1.7f, 16.6f, 46.6f, 0, H);
        WallZ("ArenaWallS", -28.3f, 16f, 46.6f, 0, H);
        WallX("ArenaWallE", 46.3f, -28.6f, -1.4f, 0, H);
        WallX("ArenaWallW", LX + T / 2f, -28.6f, -14.6f, 0, H);
    }

    // =====================================================================
    // Lobi: karpet, lambang lantai, pilar, lampu gantung, obor, jendela, spanduk
    static void BuildLobbyProps()
    {
        // Karpet dari pintu ke resepsionis + garis emas
        Box("Carpet", tProps, new Vector3(0, 0.02f, -3.5f), new Vector3(3.6f, 0.04f, 21f), mCarpet, false);
        Box("CarpetEdgeL", tProps, new Vector3(-1.8f, 0.025f, -3.5f), new Vector3(0.15f, 0.04f, 21f), mGold, false);
        Box("CarpetEdgeR", tProps, new Vector3(1.8f, 0.025f, -3.5f), new Vector3(0.15f, 0.04f, 21f), mGold, false);

        // Lambang Guild di lantai
        Cyl("EmblemRing", tProps, new Vector3(0, 0.03f, -2f), 3.4f, 0.02f, mGold, false);
        Cyl("EmblemField", tProps, new Vector3(0, 0.05f, -2f), 3.1f, 0.02f, mCarpet, false);
        Cyl("EmblemCore", tProps, new Vector3(0, 0.07f, -2f), 1.6f, 0.02f, mGold, false);
        Cyl("EmblemHub", tProps, new Vector3(0, 0.09f, -2f), 1.2f, 0.02f, mDarkWood, false);
        Box("EmblemCrossA", tProps, new Vector3(0, 0.11f, -2f), new Vector3(0.35f, 0.02f, 5.8f), mGold, false);
        Box("EmblemCrossB", tProps, new Vector3(0, 0.12f, -2f), new Vector3(5.8f, 0.02f, 0.35f), mGold, false);

        // Pilar
        foreach (float px in new[] { -8.5f, 8.5f })
            foreach (float pz in new[] { -9f, -3f, 3f })
            {
                Cyl("Pillar", tProps, new Vector3(px, 0, pz), 0.55f, H - 0.4f, mStone);
                Box("PillarBase", tProps, new Vector3(px, 0.2f, pz), new Vector3(1.5f, 0.4f, 1.5f), mStoneDark);
                Box("PillarCap", tProps, new Vector3(px, H - 0.2f, pz), new Vector3(1.5f, 0.4f, 1.5f), mStoneDark);
            }

        // Lampu gantung
        foreach (float cz in new[] { -9f, -2f, 5f })
        {
            var ch = new GameObject("Chandelier").transform;
            ch.SetParent(tLights, false);
            ch.localPosition = new Vector3(0, 0, cz);
            Cyl("Chain", ch, new Vector3(0, 7.2f, 0), 0.04f, H - 7.2f, mIron, false);
            Cyl("Ring", ch, new Vector3(0, 6.9f, 0), 1.1f, 0.15f, mIron, false);
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                Sphere("Candle", ch, new Vector3(Mathf.Cos(a) * 1.0f, 7.2f, Mathf.Sin(a) * 1.0f), 0.18f, mFire);
            }
            if (grand)
            {
                Cyl("Ring2", ch, new Vector3(0, 6.6f, 0), 1.9f, 0.12f, mGold, false);
                for (int i = 0; i < 10; i++)
                {
                    float a = i / 10f * Mathf.PI * 2f;
                    Sphere("Candle2", ch, new Vector3(Mathf.Cos(a) * 1.8f, 6.85f, Mathf.Sin(a) * 1.8f), 0.2f, mFire);
                }
                Sphere("Crystal", ch, new Vector3(0, 5.9f, 0), 0.55f, mWindow);
                Cyl("Spire", ch, new Vector3(0, 6.1f, 0), 0.07f, 1.2f, mGold, false);
            }
            AddLight(ch, new Vector3(0, 6.3f, 0), 18f, 1.2f);
        }

        // Obor dinding
        Torch(new Vector3(-LX + 0.1f, 3.4f, -10f), Vector3.right);
        Torch(new Vector3(-LX + 0.1f, 3.4f, 6.2f), Vector3.right);
        Torch(new Vector3(LX - 0.1f, 3.4f, -12.6f), Vector3.left);
        Torch(new Vector3(LX - 0.1f, 3.4f, 5.6f), Vector3.left);
        Torch(new Vector3(-4.5f, 3.4f, -LZ + 0.1f), Vector3.forward);
        Torch(new Vector3(4.5f, 3.4f, -LZ + 0.1f), Vector3.forward);

        // Jendela tinggi (cahaya jendela mengikuti jam in-game lewat GuildAmbience)
        foreach (float wz in new[] { -9f, 0f, 9f })
            Box("WindowW", tProps, new Vector3(-LX + 0.02f, 7.2f, wz), new Vector3(0.12f, 2.2f, 1.6f), mWindow, false);
        foreach (float wz in new[] { -12.8f, 0f })   // dinding timur: hindari papan nama Arena dan Kantor GM
            Box("WindowE", tProps, new Vector3(LX - 0.02f, 7.2f, wz), new Vector3(0.12f, 2.2f, 1.6f), mWindow, false);
        foreach (float wz in new[] { -6f, 6f })
        {
            windowLights.Add(AddLight(tLights, new Vector3(-13.5f, 7f, wz), 14f, 1.2f));
            windowLights.Add(AddLight(tLights, new Vector3(13.5f, 7f, wz), 14f, 1.2f));
        }

        // Spanduk
        foreach (float bx in new[] { -13.5f, 13.5f })
            Banner(new Vector3(bx, 5.2f, LZ - 0.1f), Vector3.back);
        foreach (float bx in new[] { -9.5f, 9.5f })
            Banner(new Vector3(bx, 5.2f, -LZ + 0.1f), Vector3.forward);

        // Papan nama + lambang di atas resepsionis
        Box("GuildSign", tProps, new Vector3(0, 7.6f, LZ - 0.12f), new Vector3(9f, 1.1f, 0.15f), mDarkWood, false);
        if (grand)
        {
            Box("GuildSignTrim", tProps, new Vector3(0, 7.6f, LZ - 0.1f), new Vector3(12.4f, 1.7f, 0.12f), mGold, false);
            var crest = Cyl("Crest", tProps, new Vector3(0, 9.9f, LZ - 0.3f), 1.5f, 0.12f, mGold, false);
            crest.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var crest2 = Cyl("CrestCore", tProps, new Vector3(0, 9.9f, LZ - 0.4f), 1.0f, 0.12f, mCloth, false);
            crest2.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }
        Text3D("GuildSignText", grand ? "BALAI AGUNG GUILD ELDRATH" : "GUILD PETUALANG", tProps, new Vector3(0, 7.6f, LZ - 0.25f), Vector3.back, 0.11f, new Color(1f, 0.9f, 0.55f));

        // Rak senjata di dekat pintu, tong, dan peti
        WeaponRack(new Vector3(-6.5f, 0, -LZ + 0.5f));
        WeaponRack(new Vector3(6.5f, 0, -LZ + 0.5f));
    }

    // =====================================================================
    // Balai Agung (Eldrath): marmer, lantai catur, langit-langit kotak-kotak emas, kaca patri, patung penjaga, kristal melayang
    static void BuildGrandExtras()
    {
        var g = Group("Grand", tProps);
        for (int ix = -7; ix <= 7; ix++)
            for (int iz = -6; iz <= 6; iz++)
                if (((ix + iz) & 1) == 0)
                    Box("Tile", g, new Vector3(ix * 2f, 0.011f, iz * 2f), new Vector3(2f, 0.02f, 2f), mFloorDark, false);

        // lis emas bawah & mahkota dinding
        Box("TrimN", g, new Vector3(0, 0.3f, LZ - 0.05f), new Vector3(2 * LX, 0.6f, 0.1f), mGold, false);
        Box("TrimW", g, new Vector3(-LX + 0.05f, 0.3f, 0), new Vector3(0.1f, 0.6f, 2 * LZ), mGold, false);
        Box("TrimS1", g, new Vector3((-LX - 2.8f) / 2f, 0.3f, -LZ + 0.05f), new Vector3(LX - 2.8f, 0.6f, 0.1f), mGold, false);
        Box("TrimS2", g, new Vector3((LX + 2.8f) / 2f, 0.3f, -LZ + 0.05f), new Vector3(LX - 2.8f, 0.6f, 0.1f), mGold, false);
        Box("TrimE1", g, new Vector3(LX - 0.05f, 0.3f, (-LZ - 11f) / 2f), new Vector3(0.1f, 0.6f, LZ - 11f), mGold, false);
        Box("TrimE2", g, new Vector3(LX - 0.05f, 0.3f, 1f), new Vector3(0.1f, 0.6f, 12f), mGold, false);
        Box("TrimE3", g, new Vector3(LX - 0.05f, 0.3f, 12.5f), new Vector3(0.1f, 0.6f, 3f), mGold, false);
        Box("CrownN", g, new Vector3(0, H - 0.3f, LZ - 0.15f), new Vector3(2 * LX, 0.6f, 0.3f), mGold, false);
        Box("CrownS", g, new Vector3(0, H - 0.3f, -LZ + 0.15f), new Vector3(2 * LX, 0.6f, 0.3f), mGold, false);
        Box("CrownW", g, new Vector3(-LX + 0.15f, H - 0.3f, 0), new Vector3(0.3f, 0.6f, 2 * LZ), mGold, false);
        Box("CrownE", g, new Vector3(LX - 0.15f, H - 0.3f, 0), new Vector3(0.3f, 0.6f, 2 * LZ), mGold, false);

        // langit-langit kotak-kotak
        foreach (float c in new[] { -12f, -6f, 0f, 6f, 12f })
        {
            Box("BeamX", g, new Vector3(0, H - 0.3f, c), new Vector3(2 * LX, 0.5f, 0.5f), mStone, false);
            Box("BeamXGold", g, new Vector3(0, H - 0.58f, c), new Vector3(2 * LX, 0.06f, 0.56f), mGold, false);
            Box("BeamZ", g, new Vector3(c, H - 0.3f, 0), new Vector3(0.5f, 0.5f, 2 * LZ), mStone, false);
            Box("BeamZGold", g, new Vector3(c, H - 0.58f, 0), new Vector3(0.56f, 0.06f, 2 * LZ), mGold, false);
        }

        // cincin emas & spanduk di pilar
        foreach (float px in new[] { -8.5f, 8.5f })
            foreach (float pz in new[] { -9f, -3f, 3f })
            {
                Cyl("PillarBand", g, new Vector3(px, 1.1f, pz), 0.62f, 0.25f, mGold, false);
                Cyl("PillarBand", g, new Vector3(px, H - 1.4f, pz), 0.62f, 0.25f, mGold, false);
                Banner(new Vector3(px > 0 ? px - 0.7f : px + 0.7f, H - 3.2f, pz), px > 0 ? Vector3.left : Vector3.right);
            }

        // kaca patri tinggi
        Material[] glass =
        {
            Mat(P + "GlassA", new Color(0.25f, 0.45f, 0.95f)), Mat(P + "GlassB", new Color(0.85f, 0.25f, 0.25f)),
            Mat(P + "GlassC", new Color(0.95f, 0.8f, 0.25f)), Mat(P + "GlassD", new Color(0.3f, 0.8f, 0.45f))
        };
        int gi = 0;
        foreach (float wz in new[] { -9f, 0f, 9f })
        {
            Box("GlassW", g, new Vector3(-LX + 0.04f, 9.4f, wz), new Vector3(0.1f, 3.4f, 1.7f), glass[gi++ % 4], false);
            var cap = Box("GlassWCap", g, new Vector3(-LX + 0.04f, 11.2f, wz), new Vector3(0.1f, 1.2f, 1.2f), glass[gi % 4], false);
            cap.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
        }
        foreach (float wz in new[] { -12.8f, 0f, 9f })
        {
            Box("GlassE", g, new Vector3(LX - 0.04f, 9.4f, wz), new Vector3(0.1f, 3.4f, 1.7f), glass[gi++ % 4], false);
            var cap = Box("GlassECap", g, new Vector3(LX - 0.04f, 11.2f, wz), new Vector3(0.1f, 1.2f, 1.2f), glass[gi % 4], false);
            cap.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
        }
        foreach (float wz in new[] { -6f, 6f })
        {
            windowLights.Add(AddLight(tLights, new Vector3(-13.5f, 10f, wz), 16f, 1.3f));
            windowLights.Add(AddLight(tLights, new Vector3(13.5f, 10f, wz), 16f, 1.3f));
        }

        // patung penjaga
        Statue(new Vector3(-5.2f, 0, 4.6f), 180f);
        Statue(new Vector3(5.2f, 0, 4.6f), 180f);
        Statue(new Vector3(-11f, 0, -11.5f), 135f);
        Statue(new Vector3(11f, 0, -11.5f), -135f);

        // kristal melayang di atas lambang lantai
        var cr = Box("FloatCrystal", g, new Vector3(0, 3.1f, -2f), new Vector3(0.9f, 0.9f, 0.9f), mWindow, false);
        cr.transform.localRotation = Quaternion.Euler(45f, 20f, 45f);
        var ring = Cyl("CrystalRing", g, new Vector3(0, 3.1f, -2f), 0.9f, 0.04f, mGold, false);
        ring.transform.localRotation = Quaternion.Euler(20f, 0f, 15f);
        AddLight(g, new Vector3(0, 3.1f, -2f), 12f, 1.4f).color = new Color(0.6f, 0.8f, 1f);
        Cyl("CrystalBeam", g, new Vector3(0, 0.1f, -2f), 0.06f, 2.6f, mWindow, false);
    }

    static void Statue(Vector3 pos, float yaw)
    {
        var s = new GameObject("Statue").transform;
        s.SetParent(tProps, false);
        s.localPosition = pos;
        s.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Box("Pedestal", s, new Vector3(0, 0.55f, 0), new Vector3(1.6f, 1.1f, 1.6f), mStoneDark);
        Box("PedestalTop", s, new Vector3(0, 1.15f, 0), new Vector3(1.9f, 0.14f, 1.9f), mGold, false);
        Cyl("Legs", s, new Vector3(0, 1.2f, 0), 0.34f, 1.0f, mStone, false);
        Cyl("Torso", s, new Vector3(0, 2.2f, 0), 0.42f, 1.0f, mStone, false);
        Box("Shoulders", s, new Vector3(0, 3.05f, 0), new Vector3(1.3f, 0.3f, 0.6f), mStone, false);
        Sphere("Head", s, new Vector3(0, 3.5f, 0), 0.55f, mStone);
        Box("Helm", s, new Vector3(0, 3.78f, 0), new Vector3(0.12f, 0.35f, 0.5f), mGold, false);
        Box("Sword", s, new Vector3(0.6f, 2.5f, 0.35f), new Vector3(0.1f, 2.4f, 0.06f), mGold, false);
        Box("Guard", s, new Vector3(0.6f, 1.5f, 0.35f), new Vector3(0.5f, 0.1f, 0.1f), mGold, false);
        Box("Shield", s, new Vector3(-0.75f, 2.3f, 0.2f), new Vector3(0.18f, 1.2f, 0.8f), mCloth, false);
        Box("ShieldTrim", s, new Vector3(-0.85f, 2.3f, 0.2f), new Vector3(0.06f, 1.3f, 0.9f), mGold, false);
    }

    // Aula Guild perbatasan: panel kayu, balok langit-langit, hiasan pintu
    static void BuildTownExtras()
    {
        var g = Group("Rustic", tProps);
        Box("WainscotN", g, new Vector3(0, 0.7f, LZ - 0.05f), new Vector3(2 * LX, 1.4f, 0.1f), mDarkWood, false);
        Box("WainscotW", g, new Vector3(-LX + 0.05f, 0.7f, 0), new Vector3(0.1f, 1.4f, 2 * LZ), mDarkWood, false);
        Box("WainscotS1", g, new Vector3((-LX - 2.8f) / 2f, 0.7f, -LZ + 0.05f), new Vector3(LX - 2.8f, 1.4f, 0.1f), mDarkWood, false);
        Box("WainscotS2", g, new Vector3((LX + 2.8f) / 2f, 0.7f, -LZ + 0.05f), new Vector3(LX - 2.8f, 1.4f, 0.1f), mDarkWood, false);
        Box("WainscotE1", g, new Vector3(LX - 0.05f, 0.7f, (-LZ - 11f) / 2f), new Vector3(0.1f, 1.4f, LZ - 11f), mDarkWood, false);
        Box("WainscotE2", g, new Vector3(LX - 0.05f, 0.7f, 1f), new Vector3(0.1f, 1.4f, 12f), mDarkWood, false);
        foreach (float z in new[] { -10f, -4f, 2f, 8f })
            Box("Beam", g, new Vector3(0, H - 0.25f, z), new Vector3(2 * LX, 0.5f, 0.55f), mDarkWood, false);
        foreach (float x in new[] { -8.5f, 0f, 8.5f })
            Box("BeamLong", g, new Vector3(x, H - 0.25f, 0), new Vector3(0.55f, 0.5f, 2 * LZ), mDarkWood, false);
        // hiasan pedang silang & perisai di atas pintu keluar
        var sa = Box("CrossSwordA", g, new Vector3(0, 7.7f, -LZ + 0.2f), new Vector3(0.12f, 3.2f, 0.06f), mIron, false);
        sa.transform.localRotation = Quaternion.Euler(0f, 0f, 38f);
        var sb = Box("CrossSwordB", g, new Vector3(0, 7.7f, -LZ + 0.2f), new Vector3(0.12f, 3.2f, 0.06f), mIron, false);
        sb.transform.localRotation = Quaternion.Euler(0f, 0f, -38f);
        Box("Shield", g, new Vector3(0, 7.7f, -LZ + 0.12f), new Vector3(1.2f, 1.5f, 0.12f), mCloth, false);
        Box("ShieldTrim", g, new Vector3(0, 7.7f, -LZ + 0.08f), new Vector3(1.4f, 1.7f, 0.08f), mGold, false);
        // tong & peti dekat pintu
        foreach (var bp in new[] { new Vector3(-14.4f, 0, -12.6f), new Vector3(-13.3f, 0, -12.9f), new Vector3(14.4f, 0, -12.6f) })
        {
            Cyl("Barrel", g, bp, 0.45f, 1.0f, mWood);
            Cyl("BarrelBand", g, bp + Vector3.up * 0.3f, 0.47f, 0.08f, mIron, false);
        }
        Box("Crate", g, new Vector3(13.3f, 0.45f, -12.9f), new Vector3(0.9f, 0.9f, 0.9f), mWood);
        // karpet kecil di lounge & kepala-rusa-gaya trofi
        Box("Rug", g, new Vector3(-10.5f, 0.03f, -10f), new Vector3(5f, 0.04f, 3.4f), mCloth, false);
        Box("RugEdge", g, new Vector3(-10.5f, 0.035f, -10f), new Vector3(4.5f, 0.04f, 2.9f), mGold, false);
        Box("RugIn", g, new Vector3(-10.5f, 0.04f, -10f), new Vector3(4.3f, 0.04f, 2.7f), mCloth, false);
    }

    static void Banner(Vector3 pos, Vector3 facing)
    {
        var b = new GameObject("Banner").transform;
        b.SetParent(tProps, false);
        b.localPosition = pos;
        b.rotation = Quaternion.LookRotation(facing, Vector3.up);
        Box("Cloth", b, Vector3.zero, new Vector3(2.0f, 5f, 0.08f), mCloth, false);
        var emblem = Cyl("Emblem", b, new Vector3(0, 0.6f, -0.06f), 0.6f, 0.04f, mGold, false);
        emblem.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        emblem.transform.localPosition = new Vector3(0, 0.6f, -0.07f);
        Box("Rod", b, new Vector3(0, 2.6f, 0), new Vector3(2.4f, 0.12f, 0.12f), mDarkWood, false);
    }

    static void WeaponRack(Vector3 pos)
    {
        var r = new GameObject("WeaponRack").transform;
        r.SetParent(tProps, false);
        r.localPosition = pos;
        Box("Base", r, new Vector3(0, 0.08f, 0), new Vector3(2.4f, 0.16f, 0.6f), mDarkWood);
        Box("Back", r, new Vector3(0, 0.9f, 0.25f), new Vector3(2.4f, 1.6f, 0.1f), mWood, false);
        for (int i = 0; i < 3; i++)
        {
            var s = Box("Sword", r, new Vector3(-0.8f + i * 0.8f, 0.85f, 0.1f), new Vector3(0.08f, 1.4f, 0.05f), mIron, false);
            s.transform.localRotation = Quaternion.Euler(0, 0, (i - 1) * 6f);
        }
    }

    // =====================================================================
    // Resepsionis + 5 poster rekrutmen (GDD 5.2)
    static void BuildReception()
    {
        Box("Counter", tProps, new Vector3(0, 0.55f, 8.2f), new Vector3(14.4f, 1.1f, 1.2f), mDarkWood);
        Box("CounterTop", tProps, new Vector3(0, 1.14f, 8.2f), new Vector3(14.8f, 0.1f, 1.6f), mWood, false);
        Box("PartitionL", tProps, new Vector3(-7.2f, 1.5f, 11.4f), new Vector3(0.3f, 3.0f, 5.2f), mDarkWood);
        Box("PartitionR", tProps, new Vector3(7.2f, 1.5f, 11.4f), new Vector3(0.3f, 3.0f, 5.2f), mDarkWood);
        Box("Ledger", tProps, new Vector3(-2.5f, 1.25f, 8.2f), new Vector3(0.8f, 0.12f, 0.6f), mPaper, false);
        Box("FrontPlate", tProps, new Vector3(0, 0.75f, 7.58f), new Vector3(3.4f, 0.5f, 0.06f), mDarkWood, false);
        Text3D("DeskLabel", "RESEPSIONIS", tProps, new Vector3(0, 0.75f, 7.5f), Vector3.back, 0.05f, new Color(1f, 0.9f, 0.55f));

        // Papan TUTUP (aktif hanya saat 00.00-05.00)
        var closed = new GameObject("ClosedSign").transform;
        closed.SetParent(tProps, false);
        closed.localPosition = new Vector3(0, 0, 0);
        Box("ClosedBoard", closed, new Vector3(0, 1.8f, 7.55f), new Vector3(3.0f, 0.9f, 0.08f), mRed, false);
        Text3D("ClosedText", "TUTUP 00.00 - 05.00", closed, new Vector3(0, 1.8f, 7.48f), Vector3.back, 0.05f, Color.white);
        deskClosed.Add(closed.gameObject);

        // Resepsionis (hanya saat buka)
        var lyra = MakeNpc("NPC_Resepsionis", "Resepsionis Lyra", new Vector3(0, 0, 10.4f), 180f, mNpc[2], null);
        deskOpen.Add(lyra);
        MakeInteract<ReceptionDesk>("Reception", new Vector3(0, 0, 6.0f), "Resepsionis Guild", 3.6f);

        // 5 poster di dinding belakang (isi diisi runtime oleh PosterWall dari GuildData)
        var wall = new GameObject("PosterWall");
        wall.transform.SetParent(tBoards, false);
        var pw = wall.AddComponent<PosterWall>();
        pw.names = new TextMesh[5]; pw.roles = new TextMesh[5]; pw.stats = new TextMesh[5]; pw.costs = new TextMesh[5];
        pw.portraits = new Renderer[5];
        Color ink = new Color(0.18f, 0.11f, 0.05f);
        for (int i = 0; i < 5; i++)
        {
            float x = (i - 2) * 2.9f;
            var grp = Group("Poster_" + (i + 1), wall.transform);
            Box("Frame", grp, new Vector3(x, 4.8f, LZ - 0.12f), new Vector3(2.6f, 3.4f, 0.1f), mWood, false);
            Box("Paper", grp, new Vector3(x, 4.8f, LZ - 0.18f), new Vector3(2.4f, 3.2f, 0.06f), mPaper, false);
            var por = Box("Portrait", grp, new Vector3(x, 5.55f, LZ - 0.22f), new Vector3(1.5f, 1.25f, 0.05f), Mat("GPortrait" + (i + 1), new Color(0.5f, 0.5f, 0.5f)), false);
            pw.portraits[i] = por.GetComponent<Renderer>();
            pw.names[i] = Text3D("Name", "...", grp, new Vector3(x, 4.58f, LZ - 0.27f), Vector3.back, 0.034f, ink);
            pw.roles[i] = Text3D("Role", "...", grp, new Vector3(x, 4.26f, LZ - 0.27f), Vector3.back, 0.026f, ink);
            pw.stats[i] = Text3D("Stats", "...", grp, new Vector3(x, 3.96f, LZ - 0.27f), Vector3.back, 0.02f, ink);
            pw.costs[i] = Text3D("Cost", "...", grp, new Vector3(x, 3.66f, LZ - 0.27f), Vector3.back, 0.024f, ink);
        }
        Text3D("PosterTitle", "REKRUT PARTNER", tBoards, new Vector3(0, 6.75f, LZ - 0.2f), Vector3.back, 0.05f, new Color(1f, 0.9f, 0.55f));
        AddLight(tLights, new Vector3(-4.5f, 6.6f, 11.8f), 9f, 1.0f);
        AddLight(tLights, new Vector3(4.5f, 6.6f, 11.8f), 9f, 1.0f);
    }

    // =====================================================================
    // Papan Quest (barat), Papan Pengumuman (timur), Papan Peringkat (barat laut)
    static void BuildBoards()
    {
        // ---- Papan Quest ----
        Box("QuestBoard", tBoards, new Vector3(-LX + 0.25f, 3.2f, 0), new Vector3(0.3f, 4.8f, 10f), mWood);
        Box("QBFrameTop", tBoards, new Vector3(-LX + 0.4f, 5.7f, 0), new Vector3(0.5f, 0.3f, 10.6f), mDarkWood, false);
        Box("QBFrameBottom", tBoards, new Vector3(-LX + 0.4f, 0.75f, 0), new Vector3(0.5f, 0.3f, 10.6f), mDarkWood, false);
        Box("QBFrameN", tBoards, new Vector3(-LX + 0.4f, 3.2f, 5.15f), new Vector3(0.5f, 5.1f, 0.3f), mDarkWood, false);
        Box("QBFrameS", tBoards, new Vector3(-LX + 0.4f, 3.2f, -5.15f), new Vector3(0.5f, 5.1f, 0.3f), mDarkWood, false);
        Box("QBSign", tBoards, new Vector3(-LX + 0.5f, 6.6f, 0), new Vector3(0.35f, 1.0f, 6.4f), mDarkWood, false);
        Text3D("QBText", "PAPAN QUEST", tBoards, new Vector3(-LX + 0.7f, 6.6f, 0), Vector3.right, 0.11f, new Color(1f, 0.9f, 0.55f));
        Material[] papers = { mPaper, mPaperY, mPaperB };
        int n = 0;
        for (int row = 0; row < 3; row++)
            for (int col = 0; col < 6; col++)
            {
                float jz = Mathf.Sin(n * 12.9898f) * 0.25f;
                float jy = Mathf.Sin(n * 78.233f) * 0.15f;
                float z = -4.25f + col * 1.7f + jz;
                float y = 1.7f + row * 1.35f + jy;
                var note = Box("Note", tBoards, new Vector3(-LX + 0.43f, y, z), new Vector3(0.04f, 0.8f, 0.55f), papers[n % 3], false);
                note.transform.localRotation = Quaternion.Euler(Mathf.Sin(n * 3.1f) * 4f, 0, 0);
                n++;
            }
        AddLight(tLights, new Vector3(-13.6f, 6.2f, 0), 9f, 1.0f);
        MakeInteract<QuestBoardPoint>("QuestBoard", new Vector3(-12.8f, 0, 0), "Papan Quest", 4.2f);

        // ---- Papan Pengumuman ----
        Box("NoticeBoard", tBoards, new Vector3(LX - 0.25f, 3.0f, 1.0f), new Vector3(0.3f, 3.8f, 7.2f), mDarkWood);
        Box("NBFrameTop", tBoards, new Vector3(LX - 0.4f, 5.0f, 1.0f), new Vector3(0.5f, 0.25f, 7.6f), mWood, false);
        Box("NBFrameBottom", tBoards, new Vector3(LX - 0.4f, 1.1f, 1.0f), new Vector3(0.5f, 0.25f, 7.6f), mWood, false);
        Box("NBSign", tBoards, new Vector3(LX - 0.5f, 5.8f, 1.0f), new Vector3(0.35f, 0.9f, 5.6f), mCloth, false);
        Text3D("NBText", "PENGUMUMAN", tBoards, new Vector3(LX - 0.7f, 5.8f, 1.0f), Vector3.left, 0.1f, new Color(1f, 0.9f, 0.55f));
        for (int row = 0; row < 2; row++)
            for (int col = 0; col < 4; col++)
            {
                float z = -1.2f + col * 1.55f + Mathf.Sin(n * 12.9f) * 0.15f;
                float y = 2.0f + row * 1.5f;
                var note = Box("Note", tBoards, new Vector3(LX - 0.43f, y, z), new Vector3(0.04f, 1.0f, 0.75f), papers[(n + 1) % 3], false);
                note.transform.localRotation = Quaternion.Euler(Mathf.Sin(n * 2.3f) * 4f, 0, 0);
                n++;
            }
        AddLight(tLights, new Vector3(13.6f, 5.6f, 1.0f), 9f, 0.9f);
        MakeInteract<NoticeBoardPoint>("NoticeBoard", new Vector3(12.9f, 0, 1.0f), "Papan Pengumuman", 3.6f);

        // ---- Papan Peringkat Hunter ----
        Box("RankingBoard", tBoards, new Vector3(-LX + 0.25f, 3.4f, 10.5f), new Vector3(0.3f, 4.4f, 5.4f), mSlate);
        Box("RBFrame", tBoards, new Vector3(-LX + 0.4f, 5.7f, 10.5f), new Vector3(0.5f, 0.25f, 5.8f), mGold, false);
        Text3D("RBText", "PERINGKAT HUNTER", tBoards, new Vector3(-LX + 0.7f, 6.3f, 10.5f), Vector3.right, 0.075f, new Color(1f, 0.9f, 0.55f));
        for (int i = 0; i < 6; i++)
            Box("Plaque", tBoards, new Vector3(-LX + 0.43f, 1.9f + i * 0.6f, 10.5f), new Vector3(0.05f, 0.4f, 4.2f), mGold, false);
        AddLight(tLights, new Vector3(-13.6f, 5.8f, 10.5f), 8f, 0.9f);
        MakeInteract<RankingBoardPoint>("RankingBoard", new Vector3(-12.9f, 0, 10.5f), "Papan Peringkat Hunter", 3.4f);
    }

    // =====================================================================
    // Lounge hunter di barat daya + perapian
    static void BuildLounge()
    {
        foreach (var tp in new[] { new Vector3(-11f, 0, -8f), new Vector3(-6.5f, 0, -11f) })
        {
            Cyl("TableLeg", tProps, tp, 0.2f, 0.9f, mDarkWood);
            Cyl("TableTop", tProps, tp + Vector3.up * 0.9f, 1.15f, 0.1f, mWood);
            for (int i = 0; i < 4; i++)
            {
                float a = i / 4f * Mathf.PI * 2f + 0.4f;
                Cyl("Stool", tProps, tp + new Vector3(Mathf.Cos(a) * 1.7f, 0, Mathf.Sin(a) * 1.7f), 0.35f, 0.55f, mWood);
            }
            Cyl("Mug", tProps, tp + new Vector3(0.3f, 1.0f, 0.2f), 0.1f, 0.18f, mIron, false);
        }
        Box("Bench", tProps, new Vector3(-15f, 0.35f, -9f), new Vector3(1.0f, 0.7f, 5f), mWood);

        // Perapian di dinding selatan
        Box("FireplaceBody", tProps, new Vector3(-11f, 1.5f, -13.3f), new Vector3(4.2f, 3.0f, 1.2f), mStoneDark);
        Box("FireplaceHole", tProps, new Vector3(-11f, 0.9f, -12.65f), new Vector3(2.2f, 1.5f, 0.2f), mFloorDark, false);
        Sphere("Fire", tProps, new Vector3(-11f, 0.8f, -12.6f), 0.7f, mFire);
        Box("Mantle", tProps, new Vector3(-11f, 3.1f, -13.0f), new Vector3(4.8f, 0.2f, 1.6f), mDarkWood, false);
        AddLight(tLights, new Vector3(-11f, 1.4f, -11.8f), 9f, 1.3f, true);
        Text3D("LoungeSign", "RUANG ISTIRAHAT HUNTER", tProps, new Vector3(-11f, 4.4f, -LZ + 0.1f), Vector3.forward, 0.05f, new Color(1f, 0.9f, 0.55f));
    }

    // Penilai Bahan di timur laut
    static void BuildAppraiser()
    {
        Box("AppraiserCounter", tProps, new Vector3(11f, 0.55f, 11.5f), new Vector3(7f, 1.1f, 1.2f), mDarkWood);
        Box("AppraiserTop", tProps, new Vector3(11f, 1.14f, 11.5f), new Vector3(7.3f, 0.1f, 1.5f), mWood, false);
        Box("ScaleBeam", tProps, new Vector3(9.5f, 1.55f, 11.5f), new Vector3(1.2f, 0.06f, 0.06f), mIron, false);
        Box("ScalePost", tProps, new Vector3(9.5f, 1.3f, 11.5f), new Vector3(0.06f, 0.4f, 0.06f), mIron, false);
        Box("SignBoard", tProps, new Vector3(11f, 3.6f, 11.4f), new Vector3(4.4f, 0.9f, 0.12f), mDarkWood, false);
        Text3D("SignText", "PENILAI BAHAN", tProps, new Vector3(11f, 3.6f, 11.32f), Vector3.back, 0.07f, new Color(1f, 0.9f, 0.55f));
        for (int i = 0; i < 3; i++)
        {
            var pelt = Box("Pelt", tProps, new Vector3(8f + i * 1.8f, 3.4f, LZ - 0.1f), new Vector3(1.0f, 1.4f, 0.06f), i % 2 == 0 ? mFloor : mWood, false);
            pelt.transform.localRotation = Quaternion.Euler(0, 0, (i - 1.5f) * 4f);
        }
        Cyl("Barrel", tProps, new Vector3(14.6f, 0, 13.2f), 0.45f, 1.0f, mDarkWood);
        Cyl("Barrel", tProps, new Vector3(15.4f, 0, 12.1f), 0.45f, 1.0f, mDarkWood);
        Box("Crate", tProps, new Vector3(7.8f, 0.45f, 13.4f), new Vector3(0.9f, 0.9f, 0.9f), mWood);
        Box("Crate", tProps, new Vector3(8.8f, 0.45f, 13.4f), new Vector3(0.9f, 0.9f, 0.9f), mWood);
        Box("Crate", tProps, new Vector3(8.3f, 1.35f, 13.4f), new Vector3(0.9f, 0.9f, 0.9f), mWood);
        MakeNpc("NPC_Penilai", "Penilai Brannoc", new Vector3(11f, 0, 12.9f), 180f, mNpc[3], null);
        MakeInteract<AppraiserDesk>("Appraiser", new Vector3(11f, 0, 9.4f), "Jual bahan monster", 3.2f);
        AddLight(tLights, new Vector3(11f, 4.8f, 10f), 9f, 1.0f);
        Text3D("OfficeSign", "KANTOR GUILD MASTER", tProps, new Vector3(LX - 0.1f, 6.4f, 9f), Vector3.left, 0.06f, new Color(1f, 0.9f, 0.55f));
        Text3D("ArenaSign", "ARENA & LATIHAN", tProps, new Vector3(LX - 0.1f, 7.0f, -8f), Vector3.left, 0.07f, new Color(1f, 0.9f, 0.55f));
    }

    // Kantor Guild Master
    static void BuildOffice()
    {
        Box("OfficeRug", tProps, new Vector3(22f, 0.02f, 9f), new Vector3(6f, 0.04f, 4.5f), mCarpet, false);
        Box("Desk", tProps, new Vector3(24f, 0.5f, 9f), new Vector3(1.5f, 1.0f, 4.2f), mDarkWood);
        Box("DeskTop", tProps, new Vector3(24f, 1.04f, 9f), new Vector3(1.8f, 0.08f, 4.5f), mWood, false);
        Box("Chair", tProps, new Vector3(26.9f, 0.5f, 9f), new Vector3(0.7f, 1.0f, 0.7f), mWood);
        Box("Papers", tProps, new Vector3(23.9f, 1.12f, 8.2f), new Vector3(0.5f, 0.04f, 0.7f), mPaper, false);
        // Meja peta
        Box("MapTable", tProps, new Vector3(21f, 0.45f, 5.6f), new Vector3(2.6f, 0.9f, 1.8f), mDarkWood);
        Box("MapSheet", tProps, new Vector3(21f, 0.93f, 5.6f), new Vector3(2.3f, 0.03f, 1.5f), mPaperB, false);
        Sphere("MapPin", tProps, new Vector3(21.4f, 1.0f, 5.8f), 0.14f, mRed);
        // Rak buku
        foreach (float bx in new[] { 18.8f, 22f, 25.2f })
        {
            Box("Bookshelf", tProps, new Vector3(bx, 1.6f, 13.7f), new Vector3(2.6f, 3.2f, 0.5f), mDarkWood);
            for (int r = 0; r < 3; r++)
                Box("Books", tProps, new Vector3(bx, 0.9f + r * 0.9f, 13.42f), new Vector3(2.3f, 0.5f, 0.06f), r % 2 == 0 ? mRed : mSlate, false);
        }
        Banner(new Vector3(27.9f, 3.0f, 9f), Vector3.left);
        AddLight(tLights, new Vector3(22f, 4.6f, 9f), 12f, 1.3f);
        windowLights.Add(AddLight(tLights, new Vector3(22f, 5.2f, 5f), 10f, 1.2f));
        MakeNpc("NPC_GuildMaster", "Guild Master Orwen", new Vector3(25.7f, 0, 9f), 270f, mNpc[5], null);
        MakeInteract<GuildMasterNPC>("GuildMaster", new Vector3(21.8f, 0, 9f), "Bicara dengan Guild Master", 3.4f);
    }

    static void GuildDummy(Vector3 p)
    {
        var go = new GameObject("Monster_dummy");
        go.transform.SetParent(tProps, false);
        go.transform.position = p;
        go.AddComponent<HunterGuild.Monster>().defId = "dummy";
    }

    // Arena Guild + area latihan
    static void BuildArena()
    {
        Cyl("ArenaRing", tProps, new Vector3(34f, 0, -15f), 8.5f, 0.06f, mStone, false);
        Cyl("ArenaSand", tProps, new Vector3(34f, 0, -15f), 8f, 0.1f, mSand, false);
        for (int i = 0; i < 3; i++)
        {
            float h = 0.6f * (i + 1);
            Box("StandN" + i, tProps, new Vector3(34f, h / 2f, -5.2f + i * 1.2f), new Vector3(26f, h, 1.2f), mStone);
            Box("StandS" + i, tProps, new Vector3(34f, h / 2f, -24.8f - i * 1.2f), new Vector3(26f, h, 1.2f), mStone);
        }
        // Area latihan (barat daya): boneka sungguhan (bisa dipukul, tidak pernah hancur) + Pos Latihan Terukur
        Material mHay = Mat("GHay", new Color(0.78f, 0.65f, 0.28f));
        Material mBoard = Mat("GBoard", new Color(0.55f, 0.38f, 0.20f));
        for (int i = 0; i < 3; i++)
        {
            float z = -18f - i * 3f;
            Cyl("DummyBase", tProps, new Vector3(19f, 0, z), 0.55f, 0.08f, mStone, false);
            GuildDummy(new Vector3(19f, 0, z));
            Box("HayBack", tProps, new Vector3(17.6f, 0.35f, z), new Vector3(0.9f, 0.7f, 1.6f), mHay, false);
        }
        // boneka tambahan di pasir arena (untuk latihan jarak jauh / sihir)
        GuildDummy(new Vector3(29.5f, 0, -11.5f));
        GuildDummy(new Vector3(29.5f, 0, -18.5f));
        GuildDummy(new Vector3(38.5f, 0, -15f));
        // spanduk, bangku, lentera
        foreach (float bz in new[] { -15.2f, -26.2f })
        {
            Cyl("BannerPole", tProps, new Vector3(17.3f, 0, bz), 0.08f, 4.2f, mDarkWood, false);
            Box("BannerCloth", tProps, new Vector3(17.45f, 2.9f, bz), new Vector3(0.05f, 1.9f, 0.9f), mCloth, false);
            Box("BannerTrim", tProps, new Vector3(17.45f, 1.9f, bz), new Vector3(0.07f, 0.1f, 0.9f), mGold, false);
            Sphere("BannerTip", tProps, new Vector3(17.3f, 4.3f, bz), 0.24f, mGold, false);
        }
        Box("TrainBenchSeat", tProps, new Vector3(23.2f, 0.5f, -24.6f), new Vector3(2.2f, 0.12f, 0.5f), mWood);
        Box("TrainBenchLegL", tProps, new Vector3(22.3f, 0.25f, -24.6f), new Vector3(0.12f, 0.5f, 0.45f), mDarkWood, false);
        Box("TrainBenchLegR", tProps, new Vector3(24.1f, 0.25f, -24.6f), new Vector3(0.12f, 0.5f, 0.45f), mDarkWood, false);
        Cyl("TrainBarrel", tProps, new Vector3(25.4f, 0, -23.8f), 0.4f, 0.95f, mWood);
        Cyl("TrainLampPost", tProps, new Vector3(21f, 0, -26.8f), 0.07f, 2.6f, mDarkWood, false);
        Sphere("TrainLampGlow", tLights, new Vector3(21f, 2.5f, -26.8f), 0.36f, mFire);
        AddLight(tLights, new Vector3(21f, 2.6f, -26.5f), 8f, 1.0f);
        // Pos Latihan Terukur + papan skor 3D
        var st = MakeInteract<TrainingStation>("TrainStation", new Vector3(22.6f, 0, -17.4f), "Latihan Terukur: Guild", 3.6f);
        st.kind = "guild"; st.title = "Pos Latihan Guild"; st.duration = 30f; st.range = 28f;
        Box("ScorePostL", tProps, new Vector3(24.6f, 1.5f, -18.7f), new Vector3(0.18f, 3.0f, 0.18f), mDarkWood);
        Box("ScorePostR", tProps, new Vector3(24.6f, 1.5f, -16.1f), new Vector3(0.18f, 3.0f, 0.18f), mDarkWood);
        Box("ScoreBoard", tProps, new Vector3(24.6f, 2.3f, -17.4f), new Vector3(0.14f, 1.5f, 2.9f), mBoard, false);
        Box("ScoreTrim", tProps, new Vector3(24.6f, 3.1f, -17.4f), new Vector3(0.18f, 0.1f, 3.0f), mGold, false);
        var sk = Text3D("Skor", "PAPAN SKOR", st.transform, new Vector3(24.48f - 22.6f, 2.3f, 0f), Vector3.left, 0.036f, new Color(1f, 0.92f, 0.6f));
        WeaponRack(new Vector3(20.5f, 0, -15.5f));
        MakeNpc("NPC_Pelatih", "Pelatih Harkon", new Vector3(22f, 0, -20.5f), 90f, mNpc[4], null);
        MakeInteract<TrainerNPC>("Trainer", new Vector3(24.2f, 0, -20.5f), "Bicara dengan Pelatih Guild", 3.2f);
        Text3D("TrainSign", "AREA LATIHAN", tProps, new Vector3(19f, 3.6f, -21f), Vector3.right, 0.07f, new Color(1f, 0.9f, 0.55f));

        // Gerbang Ujian di timur
        Box("ExamGate", tProps, new Vector3(45.6f, 2.5f, -15f), new Vector3(0.8f, 5f, 6f), mDarkWood);
        for (int i = 0; i < 5; i++)
            Box("GateBar", tProps, new Vector3(45.1f, 2.5f, -17f + i * 1f), new Vector3(0.1f, 4.6f, 0.12f), mIron, false);
        Text3D("GateSign", "GERBANG UJIAN RANK", tProps, new Vector3(45.0f, 5.8f, -15f), Vector3.left, 0.07f, new Color(1f, 0.9f, 0.55f));
        MakeNpc("NPC_PengawasUjian", "Pengawas Ujian Vesna", new Vector3(43.4f, 0, -11.2f), 270f, mNpc[0], null);
        MakeInteract<ExamReferee>("Referee", new Vector3(42.2f, 0, -11.5f), "Pengawas Ujian", 3.4f);

        // Obor + cahaya atap
        foreach (float tx in new[] { 24f, 34f, 44f })
        {
            Torch(new Vector3(tx, 3.4f, -2.1f), Vector3.back);
            Torch(new Vector3(tx, 3.4f, -27.9f), Vector3.forward);
        }
        windowLights.Add(AddLight(tLights, new Vector3(34f, 8f, -10f), 20f, 1.2f));
        windowLights.Add(AddLight(tLights, new Vector3(34f, 8f, -20f), 20f, 1.2f));
        AddLight(tLights, new Vector3(22f, 4.5f, -9f), 9f, 1.0f);
    }

    // =====================================================================
    static void BuildNpcs()
    {
        // Hunter di lounge (diam) dan dekat papan
        MakeNpc("NPC_Hunter_C", "Hunter (C)", new Vector3(-11f, 0, -5.9f), 180f, mNpc[1], null);
        MakeNpc("NPC_Hunter_E", "Hunter (E)", new Vector3(-4.8f, 0, -8.8f), 270f, mNpc[3], null);
        MakeNpc("NPC_Rival", "Corvin Thorne (Rival)", new Vector3(-12.0f, 0, 7.4f), 270f, mNpc[2], null);

        // Hunter yang berjalan
        var routeA = Route("RouteA", new Vector3(-10.5f, 0, -4f), new Vector3(-10.5f, 0, 4.5f));
        var routeB = Route("RouteB", new Vector3(5.5f, 0, -11f), new Vector3(5.5f, 0, -5f), new Vector3(11.5f, 0, -5f), new Vector3(11.5f, 0, -1f));
        MakeNpc("NPC_Hunter_F", "Hunter (F)", new Vector3(-10.5f, 0, 0f), 0f, mNpc[0], routeA);
        MakeNpc("NPC_Hunter_D", "Hunter (D)", new Vector3(5.5f, 0, -8f), 0f, mNpc[4], routeB);
    }

    static void BuildPlayerAndManagers()
    {
        var pl = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        pl.name = "Player";
        pl.tag = "Player";
        pl.transform.position = GuildConfig.InteriorEntryPos;
        pl.GetComponent<Renderer>().sharedMaterial = Mat("GPlayer", new Color(0.2f, 0.4f, 0.85f));
        Object.DestroyImmediate(pl.GetComponent<CapsuleCollider>());
        var cc = pl.AddComponent<CharacterController>();
        cc.height = 2f; cc.radius = 0.4f; cc.center = Vector3.zero; cc.stepOffset = 0.4f;
        pl.AddComponent<PlayerController>();
        pl.AddComponent<HunterGuild.PlayerAvatar>();
        pl.AddComponent<HunterGuild.PlayerCombat>();        // agar bisa memukul boneka di area latihan Guild
        Box("FacingMarker", pl.transform, new Vector3(0, 0.5f, 0.45f), new Vector3(0.3f, 0.3f, 0.5f), mMarker, false);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.04f, 0.06f);
        cam.fieldOfView = 65f;
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = GuildConfig.InteriorEntryPos + new Vector3(0, 3f, -5f);
        var tpc = camGo.AddComponent<ThirdPersonCamera>();
        tpc.target = pl.transform;
        tpc.distance = 5f;      // lebih dekat dari kota karena ruangan tertutup
        tpc.height = 1.7f;

        new GameObject("GameManager").AddComponent<GameState>();
        new GameObject("GuildData").AddComponent<GuildData>();
        new GameObject("TownUI").AddComponent<TownUI>();
        new GameObject("BackpackUI").AddComponent<BackpackUI>();
        new GameObject("SkillUI").AddComponent<SkillUI>();

        var mgr = new GameObject("GuildManager");
        var amb = mgr.AddComponent<GuildAmbience>();
        amb.windowLights = windowLights.ToArray();
        amb.deskOpenObjects = deskOpen.ToArray();
        amb.deskClosedObjects = deskClosed.ToArray();
        mgr.AddComponent<GuildDebugKeys>();
        Selection.activeGameObject = pl;
    }

    // =====================================================================
    static Material Mat(string name, Color c)
    {
        string path = MatDir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) { m.color = c; return m; }

        Shader sh = GraphicsSettings.currentRenderPipeline != null
            ? Shader.Find("Universal Render Pipeline/Lit")
            : Shader.Find("Standard");
        if (sh == null) sh = Shader.Find("Standard");
        m = new Material(sh);
        m.color = c;
        m.name = System.IO.Path.GetFileNameWithoutExtension(path); AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int i = path.LastIndexOf('/');
        string parent = path.Substring(0, i);
        string leaf = path.Substring(i + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    /// <summary>index 0 = taruh di depan (Town jadi scene pertama), -1 = tambah di belakang.</summary>
    static void AddToBuildSettings(string path, int index)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) return;
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in list) if (s.path == path) return;
        var entry = new EditorBuildSettingsScene(path, true);
        if (index < 0) list.Add(entry); else list.Insert(index, entry);
        EditorBuildSettings.scenes = list.ToArray();
    }
}
