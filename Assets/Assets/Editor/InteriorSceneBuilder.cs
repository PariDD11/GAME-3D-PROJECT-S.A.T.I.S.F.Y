using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using HunterGuild;

/// <summary>
/// Menu: Hunter Guild > Build Interiors (Penginapan, Pandai Besi, Toko, Akademi, Restoran, Katedral)
/// Membuat 7 scene interior (termasuk Interior_Home untuk rumah milik pemain) yang bisa dijelajahi: Interior_Inn, Interior_Smithy, Interior_Shop, Interior_Academy, Interior_Tavern, Interior_Cathedral.
/// Koordinat: pintu di dinding selatan (z = 0), ruangan membentang ke +Z, x = -W/2..W/2. Pemain muncul di (0, 1.1, 2.6).
/// Fungsi bangunan (menu penginapan, pandai besi, toko, dll.) dipindahkan ke dalam interior; pintu di kota hanya membuka scene ini.
/// Semua furnitur placeholder primitif; ganti dengan model sendiri.
/// </summary>
public static class InteriorSceneBuilder
{
    const string MatDir = "Assets/Materials/Interior";
    const float T = 0.5f;

    static Transform tStruct, tProps, tLights, tNpcs, tAct;
    static float W, D, H;
    static System.Random rng;
    static Material mFloor, mWall, mCeil, mWood, mDark, mStone, mCloth, mRed, mGold, mIron, mGlow, mWindow, mPaper, mLeaf, mBlue, mGreen, mCream;

    [MenuItem("Hunter Guild/Build Interiors (Penginapan, Pandai Besi, Toko, Akademi, Restoran, Katedral)")]
    public static void BuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!HGDlg.Show("Hunter Guild", "Membuat 6 scene interior di Assets/Scenes/Interior_*.unity. Scene dengan nama sama akan DITIMPA.\n\nLanjutkan?", "Bangun", "Batal")) return;
        EnsureFolder("Assets/Scenes"); EnsureFolder("Assets/Materials"); EnsureFolder(MatDir);
        string[] ids = { "Inn", "Smithy", "Shop", "Academy", "Tavern", "Cathedral", "Home", "PropOffice" };
        foreach (var id in ids) BuildOne(id);
        AddToBuildSettings("Assets/Scenes/Town.unity", 0);
        HGDlg.Show("Hunter Guild", "8 interior selesai dan masuk Build Settings.\nBangun ulang scene Town dan Kota Eldrath agar pintu bangunan terpasang.", "OK");
    }

    static void BuildOne(string id)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        rng = new System.Random(id.GetHashCode());
        TownMats.Load();
        NpcKit.Reset();
        MakeMats();
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.42f, 0.38f, 0.34f);
        RenderSettings.fog = false;

        var root = new GameObject("Interior_" + id).transform;
        tStruct = Group("Structure", root); tProps = Group("Props", root); tLights = Group("Lights", root); tNpcs = Group("NPCs", root); tAct = Group("Interactables", root);

        switch (id)
        {
            case "Inn": BuildInn(); break;
            case "Smithy": BuildSmithy(); break;
            case "Shop": BuildShop(); break;
            case "Academy": BuildAcademy(); break;
            case "Tavern": BuildTavern(); break;
            case "Home": BuildHome(); break;
            case "PropOffice": BuildPropOffice(); break;
            default: BuildCathedral(); break;
        }
        try { Decorate(); } catch (System.Exception e) { Debug.LogWarning("[Interior] Decorate dilewati: " + e.Message); }
        BuildPlayerAndManagers();
        string path = "Assets/Scenes/Interior_" + id + ".unity";
        EditorSceneManager.SaveScene(scene, path);
        AddToBuildSettings(path, -1);
    }

    // =====================================================================
    // Cangkang ruangan
    static void Shell(float w, float d, float h, Material floor, Material wall, Material ceil)
    {
        W = w; D = d; H = h;
        Bx("Floor", tStruct, new Vector3(0, -0.1f, d / 2f), new Vector3(w + 2f, 0.2f, d + 1f), floor);
        Bx("Ceiling", tStruct, new Vector3(0, h + 0.1f, d / 2f), new Vector3(w + 2f, 0.2f, d + 1f), ceil, false);
        float hw = w / 2f;
        Bx("WallN", tStruct, new Vector3(0, h / 2f, d), new Vector3(w + T, h, T), wall);
        Bx("WallW", tStruct, new Vector3(-hw, h / 2f, d / 2f), new Vector3(T, h, d), wall);
        Bx("WallE", tStruct, new Vector3(hw, h / 2f, d / 2f), new Vector3(T, h, d), wall);
        // dinding selatan dengan pintu 3 m
        float seg = hw - 1.5f;
        Bx("WallS_L", tStruct, new Vector3(-1.5f - seg / 2f, h / 2f, 0), new Vector3(seg, h, T), wall);
        Bx("WallS_R", tStruct, new Vector3(1.5f + seg / 2f, h / 2f, 0), new Vector3(seg, h, T), wall);
        Bx("DoorLintel", tStruct, new Vector3(0, (3.3f + h) / 2f, 0), new Vector3(3f, h - 3.3f, T), wall);
        Bx("DoorLeaf", tStruct, new Vector3(0, 1.65f, 0), new Vector3(3f, 3.3f, 0.22f), mWood);
        Bx("DoorFrameL", tStruct, new Vector3(-1.62f, 1.7f, -0.02f), new Vector3(0.24f, 3.4f, T + 0.1f), mDark, false);
        Bx("DoorFrameR", tStruct, new Vector3(1.62f, 1.7f, -0.02f), new Vector3(0.24f, 3.4f, T + 0.1f), mDark, false);
        Bx("DoorHandle", tStruct, new Vector3(1.0f, 1.5f, 0.2f), new Vector3(0.12f, 0.12f, 0.12f), mGold, false);
        // balok langit-langit
        for (float z = 3f; z < d; z += 4f) Bx("Beam", tStruct, new Vector3(0, h - 0.25f, z), new Vector3(w, 0.4f, 0.4f), mDark, false);
        // plint
        Bx("TrimW", tStruct, new Vector3(-hw + 0.3f, 0.12f, d / 2f), new Vector3(0.1f, 0.24f, d), mDark, false);
        Bx("TrimE", tStruct, new Vector3(hw - 0.3f, 0.12f, d / 2f), new Vector3(0.1f, 0.24f, d), mDark, false);

        var ex = Act<InteriorExit>("Pintu Keluar", new Vector3(0, 0, 1.5f), "Keluar ke luar");
        ex.radius = 2.4f;
        Text3D("Exit", "KELUAR", tProps, new Vector3(0, 3.7f, 0.35f), Vector3.back * -1f, 0.07f, new Color(1f, 0.9f, 0.5f));
    }

    // =====================================================================
    // 1. Penginapan
    static void BuildInn()
    {
        Shell(20f, 16f, 5.5f, mFloor, mWall, mCeil);
        Rug(new Vector3(0, 0, 5.5f), 7f, 4f, mRed);
        // meja resepsi
        Bx("ReceptionCounter", tProps, new Vector3(0, 0.55f, 10.2f), new Vector3(7f, 1.1f, 1.3f), mWood);
        Bx("CounterTop", tProps, new Vector3(0, 1.13f, 10.2f), new Vector3(7.3f, 0.12f, 1.6f), mDark, false);
        Bx("KeyBoard", tProps, new Vector3(0, 2.4f, 15.7f), new Vector3(5f, 1.6f, 0.15f), mDark, false);
        for (int i = 0; i < 8; i++) Bx("Key", tProps, new Vector3(-2.1f + i * 0.6f, 2.3f + (i % 2) * 0.5f, 15.55f), new Vector3(0.14f, 0.28f, 0.05f), mGold, false);
        Shelf(new Vector3(-6.3f, 0, 15.5f), 3.4f, 3f, mWood); Shelf(new Vector3(6.3f, 0, 15.5f), 3.4f, 3f, mWood);
        Act<Inn>("Resepsionis Penginapan", new Vector3(0, 0, 8.6f), "Sewa kamar & tidur");
        Npc("Pemilik", NpcKit.OPelayan, new Vector3(0, 0, 11.7f), 180f);

        // kamar tidur sisi barat (3 ranjang dengan tirai)
        for (int i = 0; i < 3; i++)
        {
            float z = 3.6f + i * 3.7f;
            Bed(new Vector3(-8.5f, 0, z), 0f, i == 0 ? mBlue : i == 1 ? mGreen : mRed);
            Bx("Curtain" + i, tProps, new Vector3(-7.0f, 1.2f, z + 1.85f), new Vector3(3.2f, 2.4f, 0.12f), mCloth);
            Table(new Vector3(-9.3f, 0, z - 1.1f), 0.8f, 0.8f);
            Lamp(new Vector3(-9.3f, 1.05f, z - 1.1f), 5f, 0.9f);
        }
        // lounge sisi timur
        Fireplace(new Vector3(W / 2f - 0.6f, 0, 8f), -90f);
        Armchair(new Vector3(6.2f, 0, 5.2f), 90f); Armchair(new Vector3(6.2f, 0, 10.8f), 90f);
        Table(new Vector3(6.4f, 0, 8f), 1.2f, 1.6f);
        Table(new Vector3(4.0f, 0, 3.2f), 1.6f, 1.6f); Stool(new Vector3(3.0f, 0, 3.2f)); Stool(new Vector3(5.0f, 0, 3.2f)); Stool(new Vector3(4.0f, 0, 2.2f));
        Barrel(new Vector3(8.8f, 0, 2.3f)); Barrel(new Vector3(8.8f, 0, 3.3f));
        Window(new Vector3(-W / 2f + 0.3f, 3f, 11.5f), true); Window(new Vector3(W / 2f - 0.3f, 3f, 3.5f), true); Window(new Vector3(W / 2f - 0.3f, 3f, 12.5f), true);
        Lamp(new Vector3(-2.5f, 4.2f, 5f), 9f, 1.3f); Lamp(new Vector3(2.5f, 4.2f, 5f), 9f, 1.3f); Lamp(new Vector3(0f, 4.2f, 11f), 9f, 1.3f);
        Text3D("Sign", "PENGINAPAN", tProps, new Vector3(0, 4.1f, D - 0.4f), Vector3.back, 0.12f, new Color(1f, 0.88f, 0.5f));
        Npc("Tamu", NpcKit.OPengembara, new Vector3(6.6f, 0, 6.6f), 270f);
    }

    // =====================================================================
    // 2. Pandai Besi
    static void BuildSmithy()
    {
        Shell(18f, 15f, 6f, mStone, mWall, mCeil);
        Rug(new Vector3(-2.5f, 0, 4.2f), 5f, 2.5f, mDark);
        // tungku + cerobong
        Bx("ForgeBase", tProps, new Vector3(6.6f, 0.8f, 12.8f), new Vector3(3.4f, 1.6f, 2.4f), mStone);
        Bx("ForgeHood", tProps, new Vector3(6.6f, 2.6f, 13.3f), new Vector3(2.8f, 2f, 1.4f), mStone);
        Bx("Chimney", tProps, new Vector3(6.6f, 4.6f, 13.6f), new Vector3(1.2f, 4.6f, 1.2f), mStone, false);
        Bx("Embers", tProps, new Vector3(6.6f, 1.62f, 12.6f), new Vector3(2.4f, 0.1f, 1.6f), mGlow, false);
        var fl = AddLight(tLights, new Vector3(6.6f, 2.3f, 12f), 12f, 2.4f, new Color(1f, 0.55f, 0.2f)); fl.gameObject.AddComponent<FlickerLight>();
        Bx("Bellows", tProps, new Vector3(4.5f, 0.7f, 13.6f), new Vector3(0.9f, 0.9f, 1.2f), mWood);
        // landasan + palung pendingin
        Cyl("AnvilStump", tProps, new Vector3(3.2f, 0, 8.2f), 0.5f, 0.7f, mWood);
        Bx("Anvil", tProps, new Vector3(3.2f, 0.9f, 8.2f), new Vector3(0.9f, 0.35f, 0.4f), mIron);
        Bx("AnvilNeck", tProps, new Vector3(3.2f, 0.72f, 8.2f), new Vector3(0.35f, 0.2f, 0.3f), mIron, false);
        Bx("Trough", tProps, new Vector3(6.8f, 0.4f, 7f), new Vector3(2f, 0.8f, 1f), mDark);
        Bx("TroughWater", tProps, new Vector3(6.8f, 0.78f, 7f), new Vector3(1.8f, 0.05f, 0.8f), mBlue, false);
        // rak senjata & baju zirah sisi barat
        for (int i = 0; i < 3; i++) WeaponRack(new Vector3(-W / 2f + 0.5f, 0, 5f + i * 3.3f));
        ArmorStand(new Vector3(-6.3f, 0, 13f)); ArmorStand(new Vector3(-4.8f, 0, 13.2f));
        Bx("WorkBench", tProps, new Vector3(-6.4f, 0.55f, 9.9f), new Vector3(1.2f, 1.1f, 3.2f), mWood);
        // meja jual
        Bx("Counter", tProps, new Vector3(-2.2f, 0.55f, 7.2f), new Vector3(6f, 1.1f, 1.2f), mWood);
        Bx("CounterTop", tProps, new Vector3(-2.2f, 1.13f, 7.2f), new Vector3(6.3f, 0.12f, 1.5f), mDark, false);
        Act<Blacksmith>("Pandai Besi", new Vector3(-2.2f, 0, 5.7f), "Bicara dengan pandai besi (tempa, enchant, reparasi)");
        Act<GeneralShop>("Etalase Perlengkapan", new Vector3(-5.2f, 0, 5.7f), "Lihat etalase set perlengkapan");
        Npc("Pandai Besi", NpcKit.OPandai, new Vector3(-2.2f, 0, 8.6f), 180f);
        Barrel(new Vector3(8.0f, 0, 2.2f)); Barrel(new Vector3(8.0f, 0, 3.2f)); Crate(new Vector3(-8f, 0, 2.2f)); Crate(new Vector3(-8f, 0, 3.4f));
        Bx("CoalPile", tProps, new Vector3(8.2f, 0.3f, 10f), new Vector3(1.2f, 0.6f, 1.6f), mDark);
        for (int i = 0; i < 5; i++) Bx("Tool" + i, tProps, new Vector3(-3f + i * 1.2f, 3.6f, D - 0.3f), new Vector3(0.12f, 0.9f, 0.08f), mIron, false);
        Lamp(new Vector3(-3f, 4.6f, 5f), 10f, 1.2f); Lamp(new Vector3(2f, 4.6f, 10f), 10f, 1.2f);
        Text3D("Sign", "PANDAI BESI", tProps, new Vector3(0, 4.8f, D - 0.4f), Vector3.back, 0.12f, new Color(1f, 0.8f, 0.5f));
    }

    // =====================================================================
    // 3. Toko Umum
    static void BuildShop()
    {
        Shell(16f, 14f, 5f, mFloor, mCream, mCeil);
        Rug(new Vector3(0, 0, 4.5f), 5f, 3f, mBlue);
        for (int i = 0; i < 4; i++)
        {
            ShelfGoods(new Vector3(-W / 2f + 0.6f, 0, 3f + i * 2.6f), 90f);
            ShelfGoods(new Vector3(W / 2f - 0.6f, 0, 3f + i * 2.6f), -90f);
        }
        for (int i = 0; i < 3; i++) ShelfGoods(new Vector3(-4.5f + i * 4.5f, 0, D - 0.7f), 180f);
        Bx("Counter", tProps, new Vector3(0, 0.55f, 8.2f), new Vector3(5.6f, 1.1f, 1.3f), mWood);
        Bx("CounterTop", tProps, new Vector3(0, 1.13f, 8.2f), new Vector3(5.9f, 0.12f, 1.6f), mDark, false);
        Bx("Scale", tProps, new Vector3(-1.6f, 1.35f, 8.2f), new Vector3(0.5f, 0.3f, 0.3f), mGold, false);
        Bx("GoodsBox", tProps, new Vector3(1.4f, 1.3f, 8.2f), new Vector3(0.6f, 0.3f, 0.5f), mLeaf, false);
        Act<GeneralShop>("Toko Umum", new Vector3(0, 0, 6.7f), "Bicara dengan pemilik toko");
        Npc("Pemilik Toko", NpcKit.OPedagang, new Vector3(0, 0, 9.5f), 180f);
        Crate(new Vector3(6.2f, 0, 2.2f)); Crate(new Vector3(6.2f, 0.9f, 2.2f)); Sack(new Vector3(5.4f, 0, 2.2f)); Sack(new Vector3(-6f, 0, 2.2f)); Barrel(new Vector3(-6.8f, 0, 2.2f));
        for (int i = 0; i < 4; i++) Lamp(new Vector3(-4.5f + i * 3f, 3.9f, 4f + (i % 2) * 5f), 8f, 1.1f);
        Window(new Vector3(-W / 2f + 0.3f, 2.9f, 11.5f), true);
        Text3D("Sign", "TOKO UMUM", tProps, new Vector3(0, 3.9f, D - 0.4f), Vector3.back, 0.12f, new Color(1f, 0.9f, 0.5f));
        Npc("Pembeli", NpcKit.OWarga, new Vector3(3.5f, 0, 4f), 90f);
    }

    // =====================================================================
    // 4. Akademi & Perpustakaan
    static void BuildAcademy()
    {
        Shell(24f, 22f, 8.5f, mFloor, mWall, mCeil);
        Rug(new Vector3(0, 0, 9f), 5f, 14f, mBlue);
        for (int c = 0; c < 2; c++)
            for (int r = 0; r < 3; r++)
            {
                float x = (c == 0 ? -1f : 1f) * (r == 0 ? 10.2f : r == 1 ? 7.2f : 4.4f);
                BookStack(new Vector3(x, 0, 6.5f), 0.8f, 7.5f);
            }
        BookWall(new Vector3(-W / 2f + 0.55f, 0, 14.3f), 90f); BookWall(new Vector3(W / 2f - 0.55f, 0, 14.3f), -90f);
        // panggung dosen + podium
        Bx("Dais", tProps, new Vector3(0, 0.2f, 19.2f), new Vector3(10f, 0.4f, 5f), mStone);
        Bx("DaisStep", tProps, new Vector3(0, 0.1f, 16.4f), new Vector3(10f, 0.2f, 1f), mStone);
        Bx("Lectern", tProps, new Vector3(0, 0.95f, 18.2f), new Vector3(1.4f, 1.1f, 0.9f), mWood);
        Bx("LecternBook", tProps, new Vector3(0, 1.58f, 18.15f), new Vector3(0.7f, 0.08f, 0.5f), mPaper, false);
        Act<Library>("Perpustakaan", new Vector3(0, 0, 15.2f), "Bicara dengan pustakawan");
        Npc("Pustakawan", NpcKit.OPelajar, new Vector3(0, 0.4f, 19.6f), 180f);
        // orrery
        Cyl("OrreryBase", tProps, new Vector3(0, 0, 9.5f), 0.7f, 1.0f, mStone);
        var sp = Sphere("OrreryCore", tProps, new Vector3(0, 1.9f, 9.5f), 0.9f, mGlow);
        for (int i = 0; i < 3; i++)
        {
            var ring = Cyl("OrreryRing" + i, tProps, new Vector3(0, 1.9f, 9.5f), 0.9f + i * 0.35f, 0.04f, mGold, false);
            ring.transform.localRotation = Quaternion.Euler(i * 35f, i * 50f, i * 20f);
        }
        AddLight(tLights, new Vector3(0, 2.5f, 9.5f), 9f, 1.4f, new Color(0.6f, 0.75f, 1f));
        // meja baca
        for (int i = 0; i < 2; i++)
        {
            float x = i == 0 ? -7.2f : 7.2f;
            Table(new Vector3(x, 0, 3.4f), 3.4f, 1.4f); Chair(new Vector3(x - 0.9f, 0, 2.5f)); Chair(new Vector3(x + 0.9f, 0, 2.5f)); Chair(new Vector3(x, 0, 4.3f));
        }
        for (int i = 0; i < 3; i++) Window(new Vector3(-W / 2f + 0.3f, 5.2f, 4f + i * 6f), true);
        for (int i = 0; i < 3; i++) Window(new Vector3(W / 2f - 0.3f, 5.2f, 4f + i * 6f), true);
        Lamp(new Vector3(-3f, 7f, 6f), 12f, 1.3f); Lamp(new Vector3(3f, 7f, 6f), 12f, 1.3f); Lamp(new Vector3(0f, 7f, 14f), 14f, 1.4f);
        Text3D("Sign", "AKADEMI & PERPUSTAKAAN", tProps, new Vector3(0, 6.5f, D - 0.4f), Vector3.back, 0.14f, new Color(0.85f, 0.92f, 1f));
        Npc("Pelajar", NpcKit.OPelajar, new Vector3(-7.2f, 0, 4.4f), 180f); Npc("Guru Sihir", NpcKit.OPenyihir, new Vector3(-3.2f, 0, 12.5f), 90f);
        var mentor = Act<MageMentor>("Guru Sihir Akademi", new Vector3(-4.4f, 0, 12.5f), "Bicara: Guru Sihir"); mentor.radius = 3.4f;
        Act<ManaCrystal>("Kristal Mana", new Vector3(-9.2f, 0, 10.5f), "Serap energi Kristal Mana");
        Act<ManaCrystal>("Kristal Mana", new Vector3(9.2f, 0, 10.5f), "Serap energi Kristal Mana");
    }

    // =====================================================================
    // 5. Restoran / Kedai
    static void BuildTavern()
    {
        Shell(22f, 17f, 5.8f, mFloor, mWall, mCeil);
        Rug(new Vector3(0, 0, 6.5f), 8f, 5f, mRed);
        Bx("Bar", tProps, new Vector3(0, 0.55f, 12.4f), new Vector3(10f, 1.1f, 1.3f), mWood);
        Bx("BarTop", tProps, new Vector3(0, 1.13f, 12.4f), new Vector3(10.3f, 0.12f, 1.6f), mDark, false);
        for (int i = 0; i < 5; i++) Stool(new Vector3(-4f + i * 2f, 0, 11.2f));
        for (int i = 0; i < 4; i++) Barrel(new Vector3(-6f + i * 4f, 0, 16f));
        Shelf(new Vector3(-3f, 0, 16.5f), 5f, 3.2f, mWood); Shelf(new Vector3(3.2f, 0, 16.5f), 5f, 3.2f, mWood);
        for (int i = 0; i < 7; i++) Bx("Bottle" + i, tProps, new Vector3(-4.2f + i * 1.4f, 2.1f + (i % 2) * 0.8f, 16.5f), new Vector3(0.14f, 0.38f, 0.14f), i % 2 == 0 ? mGreen : mRed, false);
        Act<Restaurant>("Restoran", new Vector3(0, 0, 10.3f), "Pesan makanan");
        Npc("Koki", NpcKit.OKoki, new Vector3(0, 0, 13.9f), 180f);
        // meja makan
        float[,] tables = { { -7f, 3.5f }, { -7f, 8f }, { 6.5f, 3.2f }, { 2.5f, 3f }, { -2.5f, 3f } };
        for (int i = 0; i < tables.GetLength(0); i++)
        {
            float x = tables[i, 0], z = tables[i, 1];
            Table(new Vector3(x, 0, z), 1.8f, 1.8f);
            Stool(new Vector3(x - 1.3f, 0, z)); Stool(new Vector3(x + 1.3f, 0, z)); Stool(new Vector3(x, 0, z - 1.3f)); Stool(new Vector3(x, 0, z + 1.3f));
            Bx("Plate" + i, tProps, new Vector3(x, 0.95f, z), new Vector3(0.4f, 0.04f, 0.4f), mCream, false);
        }
        Fireplace(new Vector3(-W / 2f + 0.6f, 0, 12.5f), 90f);
        // panggung bard
        Bx("Stage", tProps, new Vector3(8f, 0.2f, 13f), new Vector3(5f, 0.4f, 6f), mDark);
        Npc("Bard", NpcKit.OBard, new Vector3(8f, 0.4f, 13f), 270f);
        Lamp(new Vector3(-5f, 4.6f, 5f), 10f, 1.2f); Lamp(new Vector3(0f, 4.6f, 8f), 10f, 1.2f); Lamp(new Vector3(5f, 4.6f, 5f), 10f, 1.2f); Lamp(new Vector3(8f, 4.6f, 12f), 9f, 1.2f);
        Window(new Vector3(W / 2f - 0.3f, 3f, 5f), true); Window(new Vector3(-W / 2f + 0.3f, 3f, 4f), true);
        Text3D("Sign", "RESTORAN", tProps, new Vector3(0, 4.8f, D - 0.4f), Vector3.back, 0.12f, new Color(1f, 0.85f, 0.5f));
        Npc("Tamu A", NpcKit.OWarga, new Vector3(-5.7f, 0, 3.5f), 90f); Npc("Tamu B", NpcKit.OPetani, new Vector3(2.5f, 0, 4.3f), 180f);
    }

    // =====================================================================
    // 6. Katedral
    static void BuildCathedral()
    {
        Shell(20f, 34f, 15f, mStone, mWall, mCeil);
        Rug(new Vector3(0, 0, 14f), 3f, 26f, mRed);
        for (int i = 0; i < 7; i++)
        {
            float z = 5.5f + i * 2.9f;
            Pew(new Vector3(-5.3f, 0, z)); Pew(new Vector3(5.3f, 0, z));
        }
        for (int i = 0; i < 6; i++)
        {
            float z = 4f + i * 5.2f;
            Cyl("PillarW" + i, tProps, new Vector3(-8.7f, 0, z), 0.65f, 13.5f, mStone);
            Cyl("PillarE" + i, tProps, new Vector3(8.7f, 0, z), 0.65f, 13.5f, mStone);
            Bx("CapW" + i, tProps, new Vector3(-8.7f, 13.7f, z), new Vector3(1.6f, 0.4f, 1.6f), mGold, false);
            Bx("CapE" + i, tProps, new Vector3(8.7f, 13.7f, z), new Vector3(1.6f, 0.4f, 1.6f), mGold, false);
            if (i % 2 == 0) { Window(new Vector3(-W / 2f + 0.3f, 7f, z + 2.6f), true, 1.8f, 6f); Window(new Vector3(W / 2f - 0.3f, 7f, z + 2.6f), true, 1.8f, 6f); }
        }
        // altar di panggung
        Bx("Chancel", tProps, new Vector3(0, 0.3f, 30.5f), new Vector3(17f, 0.6f, 7f), mStone);
        Bx("ChancelStep", tProps, new Vector3(0, 0.15f, 26.6f), new Vector3(17f, 0.3f, 0.8f), mStone);
        Bx("Altar", tProps, new Vector3(0, 1.15f, 30.6f), new Vector3(4f, 1.1f, 1.4f), mCream);
        Bx("AltarCloth", tProps, new Vector3(0, 1.72f, 30.6f), new Vector3(4.3f, 0.06f, 1.6f), mRed, false);
        Bx("CrossV", tProps, new Vector3(0, 7.5f, 33.6f), new Vector3(0.7f, 5f, 0.3f), mGlow, false);
        Bx("CrossH", tProps, new Vector3(0, 8.5f, 33.6f), new Vector3(3f, 0.7f, 0.3f), mGlow, false);
        AddLight(tLights, new Vector3(0, 8f, 31.5f), 18f, 2f, new Color(1f, 0.9f, 0.6f));
        Bx("GlassBlue", tProps, new Vector3(-5.2f, 7.5f, 33.7f), new Vector3(2.2f, 7f, 0.2f), mWindow, false);
        Bx("GlassRed", tProps, new Vector3(5.2f, 7.5f, 33.7f), new Vector3(2.2f, 7f, 0.2f), mRed, false);
        for (int i = 0; i < 4; i++)
        {
            float x = -6.5f + i * 4.3f; float z = i % 3 == 0 ? 29f : 31f;
            Candelabra(new Vector3(x, 0.6f, 28.2f + (i % 2) * 0.1f));
        }
        Candelabra(new Vector3(-2.7f, 0.6f, 30.6f)); Candelabra(new Vector3(2.7f, 0.6f, 30.6f));
        Act<Chapel>("Altar Katedral", new Vector3(0, 0.6f, 28.4f), "Berdoa di altar");
        Npc("Pendeta", NpcKit.OPendeta, new Vector3(0, 0.6f, 32.2f), 180f);
        Npc("Peziarah A", NpcKit.OWarga, new Vector3(-5.3f, 0, 8.4f), 0f); Npc("Peziarah B", NpcKit.OPetani, new Vector3(5.3f, 0, 14.2f), 0f);
        Npc("Biarawan", NpcKit.OPendeta, new Vector3(-6.8f, 0, 25f), 90f);
        Lamp(new Vector3(0, 12f, 8f), 20f, 1.4f); Lamp(new Vector3(0, 12f, 18f), 20f, 1.4f); Lamp(new Vector3(0, 12f, 27f), 20f, 1.6f);
        Text3D("Sign", "KATEDRAL CAHAYA SUCI", tProps, new Vector3(0, 5.6f, 0.4f), Vector3.forward, 0.16f, new Color(1f, 0.92f, 0.65f));
    }

    // =====================================================================
    // 7. Rumah pemain: semua furnitur dibangun lalu diaktifkan sesuai kepemilikan (HomeInterior)
    static void BuildHome()
    {
        Shell(14f, 12f, 4.2f, mFloor, mWall, mCeil);
        AddLight(tLights, new Vector3(0f, 3.6f, 6f), 14f, 1.1f, new Color(1f, 0.85f, 0.6f));
        Window(new Vector3(-W / 2f + 0.3f, 2.4f, 6f), true); Window(new Vector3(W / 2f - 0.3f, 2.4f, 4f), true);
        var title = Text3D("HouseName", "Rumahku", tProps, new Vector3(0, 3.5f, D - 0.4f), Vector3.back, 0.1f, new Color(1f, 0.88f, 0.5f));

        var fr = Group("Furniture", tProps);
        var keep = tProps;
        System.Func<string, Transform> F = n => { var g = Group("Furn_" + n, fr); tProps = g; return g; };

        var g0 = F("bed0");
        Bx("Pallet", g0, new Vector3(-5.2f, 0.08f, 10f), new Vector3(2.2f, 0.16f, 1.1f), mCream, false);
        Bx("Straw", g0, new Vector3(-5.2f, 0.18f, 10f), new Vector3(2.0f, 0.1f, 0.95f), mCream, false);

        F("bed1"); Bed(new Vector3(-5.2f, 0, 10f), 0f, mCream);
        F("bed2"); Bed(new Vector3(-5.2f, 0, 10f), 0f, mBlue);
        var g3 = F("bed3"); Bed(new Vector3(-5.2f, 0, 10f), 0f, mRed);
        foreach (var sx in new[] { -1f, 1f }) foreach (var sz in new[] { -1f, 1f })
            Bx("Post", g3, new Vector3(-5.2f + sx * 1.15f, 1.4f, 10f + sz * 0.65f), new Vector3(0.1f, 2.8f, 0.1f), mGold, false);
        Bx("Canopy", g3, new Vector3(-5.2f, 2.8f, 10f), new Vector3(2.5f, 0.1f, 1.5f), mRed, false);

        var gt = F("table"); Table(new Vector3(3f, 0, 8f), 1.8f, 1.0f); Stool(new Vector3(2.0f, 0, 8f)); Stool(new Vector3(4.0f, 0, 8f));
        var gc = F("chest"); Bx("Wardrobe", gc, new Vector3(-6.3f, 1.1f, 5.2f), new Vector3(1.0f, 2.2f, 1.6f), mWood); Bx("Handle", gc, new Vector3(-5.78f, 1.1f, 5.0f), new Vector3(0.06f, 0.3f, 0.06f), mGold, false);
        var gs = F("shelf"); Shelf(new Vector3(0f, 0, 11.6f), 3.2f, 3f, mWood); BookStack(new Vector3(-0.8f, 1.5f, 11.5f), 0.9f, 0.3f);
        F("rug"); Rug(new Vector3(0f, 0, 5.2f), 6f, 4f, mRed);
        var gl = F("lamp"); Bx("Cord", gl, new Vector3(0f, 3.7f, 6f), new Vector3(0.03f, 1.0f, 0.03f), mDark, false); Sphere("Lantern", gl, new Vector3(0f, 3.1f, 6f), 0.35f, mGlow);
        AddLight(gl, new Vector3(0f, 3.1f, 6f), 12f, 1.6f, new Color(1f, 0.8f, 0.5f));
        var gp = F("plant"); Cyl("Pot", gp, new Vector3(6.2f, 0, 2.4f), 0.35f, 0.5f, mWood); Sphere("Leaves", gp, new Vector3(6.2f, 1.0f, 2.4f), 0.95f, mLeaf);
        F("fire"); Fireplace(new Vector3(W / 2f - 0.6f, 0, 8f), -90f);
        var gr = F("trophy"); Bx("Pedestal", gr, new Vector3(6.0f, 0.5f, 11f), new Vector3(0.8f, 1.0f, 0.8f), mStone); Sphere("Cup", gr, new Vector3(6.0f, 1.25f, 11f), 0.4f, mGold); Cyl("CupStem", gr, new Vector3(6.0f, 1.0f, 11f), 0.08f, 0.2f, mGold, false);
        var gk = F("tank"); Bx("TankStand", gk, new Vector3(-2.6f, 0.4f, 11.7f), new Vector3(2.0f, 0.8f, 0.8f), mWood); Bx("TankGlass", gk, new Vector3(-2.6f, 1.2f, 11.7f), new Vector3(1.8f, 0.8f, 0.6f), mWindow, false);
        for (int i = 0; i < 4; i++) Bx("Fish", gk, new Vector3(-3.1f + i * 0.35f, 1.1f + (i % 2) * 0.25f, 11.7f), new Vector3(0.14f, 0.08f, 0.05f), mGold, false);
        tProps = keep;

        var bed = Act<HomeBed>("Ranjang (Tidur & Rumah)", new Vector3(-3.2f, 0, 9.2f), "Tidur / atur rumah");
        bed.radius = 2.6f;
        var hi = new GameObject("HomeInterior").AddComponent<HomeInterior>();
        hi.furniture = fr; hi.title = title;
    }

    // =====================================================================
    // 8. Kantor Properti: lobi, meja Agen Properti, meja Penjual Furnitur, maket rumah, papan daftar harga, ruang tunggu
    static void BuildPropOffice()
    {
        Shell(24f, 18f, 5.8f, mFloor, mWall, mCeil);
        Rug(new Vector3(0, 0, 6.5f), 9f, 6f, mBlue);
        Text3D("Sign", "KANTOR PROPERTI", tProps, new Vector3(0, 4.6f, D - 0.4f), Vector3.back, 0.14f, new Color(1f, 0.88f, 0.5f));
        Text3D("Sub", "Rumah  -  Tanah  -  Furnitur", tProps, new Vector3(0, 3.95f, D - 0.4f), Vector3.back, 0.06f, new Color(0.95f, 0.9f, 0.75f));

        // meja Agen Properti (tengah belakang)
        Bx("AgentCounter", tProps, new Vector3(-4.5f, 0.55f, 13.3f), new Vector3(6.2f, 1.1f, 1.3f), mWood);
        Bx("AgentTop", tProps, new Vector3(-4.5f, 1.13f, 13.3f), new Vector3(6.5f, 0.12f, 1.6f), mDark, false);
        Text3D("AgentLabel", "AGEN PROPERTI", tProps, new Vector3(-4.5f, 0.6f, 12.6f), Vector3.back, 0.06f, new Color(1f, 0.9f, 0.55f));
        Npc("Agen Properti", NpcKit.OPelayan, new Vector3(-4.5f, 0, 14.6f), 180f);
        var ag = Act<PropertyNpc>("Agen Properti", new Vector3(-4.5f, 0, 11.4f), "Bicara: Agen Properti");
        ag.furniture = false; ag.radius = 3.2f;

        // meja Penjual Furnitur
        Bx("FurnCounter", tProps, new Vector3(5.5f, 0.55f, 13.3f), new Vector3(6.2f, 1.1f, 1.3f), mWood);
        Bx("FurnTop", tProps, new Vector3(5.5f, 1.13f, 13.3f), new Vector3(6.5f, 0.12f, 1.6f), mDark, false);
        Text3D("FurnLabel", "TOKO FURNITUR", tProps, new Vector3(5.5f, 0.6f, 12.6f), Vector3.back, 0.06f, new Color(1f, 0.9f, 0.55f));
        Npc("Penjual Furnitur", NpcKit.OPedagang, new Vector3(5.5f, 0, 14.6f), 180f);
        var fu = Act<PropertyNpc>("Penjual Furnitur", new Vector3(5.5f, 0, 11.4f), "Bicara: Penjual Furnitur");
        fu.furniture = true; fu.radius = 3.2f;
        // contoh furnitur di belakang penjual
        Shelf(new Vector3(8.7f, 0, 17.4f), 3.4f, 3.2f, mWood);
        Bx("SampleChest", tProps, new Vector3(2.4f, 0.5f, 17.3f), new Vector3(1.3f, 1f, 0.8f), mWood);
        Lamp(new Vector3(3.8f, 1.9f, 17.4f), 3f, 0.8f);

        // meja maket: miniatur rumah-rumah yang dijual
        Bx("ModelTable", tProps, new Vector3(0, 0.55f, 6.8f), new Vector3(7.5f, 1.1f, 3.2f), mWood);
        Bx("ModelTop", tProps, new Vector3(0, 1.13f, 6.8f), new Vector3(7.8f, 0.1f, 3.5f), mCream, false);
        string[] names = { "Gubuk", "Kontrakan", "Rumah Batu", "Rumah Besar", "Loteng", "Menara", "Vila" };
        float[] hh = { 0.5f, 0.6f, 0.8f, 1.1f, 0.7f, 1.4f, 1.3f };
        Material[] roofs = { mWood, mRed, mBlue, mRed, mBlue, mDark, mGreen };
        for (int i = 0; i < names.Length; i++)
        {
            float mx = -3.1f + i * 1.03f;
            float h = hh[i];
            Bx("ModelBody", tProps, new Vector3(mx, 1.18f + h / 2f, 6.8f), new Vector3(0.7f, h, 0.7f), mCream, false);
            var roof = Bx("ModelRoof", tProps, new Vector3(mx, 1.18f + h + 0.18f, 6.8f), new Vector3(0.85f, 0.35f, 0.85f), roofs[i], false);
            roof.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            Bx("ModelDoor", tProps, new Vector3(mx, 1.3f, 6.43f), new Vector3(0.18f, 0.28f, 0.04f), mDark, false);
        }
        Text3D("ModelTitle", "MAKET RUMAH YANG DIJUAL", tProps, new Vector3(0, 2.5f, 6.8f), Vector3.back * -1f, 0.05f, new Color(1f, 0.88f, 0.5f));

        // papan daftar harga di dinding barat dan timur
        string villa = "DESA PEMULA\nGubuk Tepi Sungai  300g";
        string town = "KOTA GUILD\nRumah Kontrakan  1.200g\nRumah Batu  4.500g\nRumah Besar  12.000g";
        string eld = "KOTA ELDRATH\nKamar Loteng  2.000g\nRumah Menara  8.000g\nVila Penjelajah  25.000g";
        Bx("PriceBoardW", tProps, new Vector3(-W / 2f + 0.25f, 2.6f, 8f), new Vector3(0.12f, 2.6f, 5f), mDark, false);
        Text3D("PriceW", villa + "\n\n" + town, tProps, new Vector3(-W / 2f + 0.35f, 2.6f, 8f), Vector3.right, 0.05f, new Color(1f, 0.92f, 0.65f));
        Bx("PriceBoardE", tProps, new Vector3(W / 2f - 0.25f, 2.6f, 8f), new Vector3(0.12f, 2.6f, 5f), mDark, false);
        Text3D("PriceE", eld + "\n\nRumah tidak bisa dijual kembali.", tProps, new Vector3(W / 2f - 0.35f, 2.6f, 8f), Vector3.left, 0.05f, new Color(1f, 0.92f, 0.65f));

        // ruang tunggu
        Armchair(new Vector3(-8.6f, 0, 4.2f), 90f); Armchair(new Vector3(-8.6f, 0, 7.2f), 90f);
        Table(new Vector3(-8.9f, 0, 5.7f), 0.8f, 1.0f);
        Armchair(new Vector3(8.6f, 0, 4.2f), 270f); Armchair(new Vector3(8.6f, 0, 7.2f), 270f);
        Table(new Vector3(8.9f, 0, 5.7f), 0.8f, 1.0f);
        Cyl("PotL", tProps, new Vector3(-10.4f, 0, 2.6f), 0.4f, 0.6f, mWood); Sphere("LeafL", tProps, new Vector3(-10.4f, 1.1f, 2.6f), 1.0f, mLeaf);
        Cyl("PotR", tProps, new Vector3(10.4f, 0, 2.6f), 0.4f, 0.6f, mWood); Sphere("LeafR", tProps, new Vector3(10.4f, 1.1f, 2.6f), 1.0f, mLeaf);
        Window(new Vector3(-W / 2f + 0.3f, 3f, 13f), true); Window(new Vector3(W / 2f - 0.3f, 3f, 3f), true); Window(new Vector3(W / 2f - 0.3f, 3f, 13f), true);
        Lamp(new Vector3(-4f, 5.0f, 6f), 10f, 1.4f); Lamp(new Vector3(4f, 5.0f, 6f), 10f, 1.4f); Lamp(new Vector3(0f, 5.0f, 13f), 10f, 1.4f);
        Npc("Calon Pembeli", NpcKit.OPengembara, new Vector3(-2.4f, 0, 4.4f), 0f);
    }

    // =====================================================================
    // Furnitur
    static void Bed(Vector3 p, float yaw, Material blanket)
    {
        var g = Holder("Bed", tProps, p, yaw);
        Bx("Frame", g, new Vector3(0, 0.25f, 0), new Vector3(2.4f, 0.5f, 1.3f), mWood);
        Bx("Mattress", g, new Vector3(0.0f, 0.58f, 0), new Vector3(2.2f, 0.18f, 1.15f), mCream, false);
        Bx("Blanket", g, new Vector3(0.4f, 0.7f, 0), new Vector3(1.4f, 0.1f, 1.18f), blanket, false);
        Bx("Pillow", g, new Vector3(-0.85f, 0.75f, 0), new Vector3(0.45f, 0.15f, 0.9f), mCream, false);
        Bx("Headboard", g, new Vector3(-1.22f, 0.8f, 0), new Vector3(0.1f, 1.1f, 1.3f), mDark, false);
    }

    static void Table(Vector3 p, float w, float d)
    {
        var g = Holder("Table", tProps, p, 0f);
        Bx("Top", g, new Vector3(0, 0.85f, 0), new Vector3(w, 0.1f, d), mWood);
        foreach (var sx in new[] { -1f, 1f }) foreach (var sz in new[] { -1f, 1f })
            Bx("Leg", g, new Vector3(sx * (w / 2f - 0.1f), 0.4f, sz * (d / 2f - 0.1f)), new Vector3(0.1f, 0.8f, 0.1f), mDark, false);
    }

    static void Stool(Vector3 p) { Cyl("Stool", tProps, p, 0.25f, 0.55f, mWood); }

    static void Chair(Vector3 p)
    {
        var g = Holder("Chair", tProps, p, 0f);
        Bx("Seat", g, new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0.08f, 0.5f), mWood);
        Bx("Back", g, new Vector3(0, 0.9f, 0.23f), new Vector3(0.5f, 0.8f, 0.06f), mWood, false);
    }

    static void Armchair(Vector3 p, float yaw)
    {
        var g = Holder("Armchair", tProps, p, yaw);
        Bx("Seat", g, new Vector3(0, 0.35f, 0), new Vector3(1.1f, 0.6f, 1.1f), mRed);
        Bx("Back", g, new Vector3(0, 0.85f, -0.45f), new Vector3(1.1f, 0.9f, 0.25f), mRed, false);
        Bx("ArmL", g, new Vector3(-0.5f, 0.65f, 0), new Vector3(0.15f, 0.45f, 1.0f), mDark, false);
        Bx("ArmR", g, new Vector3(0.5f, 0.65f, 0), new Vector3(0.15f, 0.45f, 1.0f), mDark, false);
    }

    static void Barrel(Vector3 p) { Cyl("Barrel", tProps, p, 0.45f, 1.0f, mWood); Cyl("BarrelBand", tProps, p + Vector3.up * 0.3f, 0.47f, 0.08f, mIron, false); Cyl("BarrelBand", tProps, p + Vector3.up * 0.7f, 0.47f, 0.08f, mIron, false); }
    static void Crate(Vector3 p) { Bx("Crate", tProps, p + Vector3.up * 0.45f, new Vector3(0.9f, 0.9f, 0.9f), mWood); }
    static void Sack(Vector3 p) { Bx("Sack", tProps, p + Vector3.up * 0.35f, new Vector3(0.6f, 0.7f, 0.5f), mCream); }
    static void Rug(Vector3 c, float w, float d, Material m) { Bx("Rug", tProps, new Vector3(c.x, 0.02f, c.z), new Vector3(w, 0.04f, d), m, false); Bx("RugEdge", tProps, new Vector3(c.x, 0.03f, c.z), new Vector3(w - 0.5f, 0.04f, d - 0.5f), mGold, false); Bx("RugIn", tProps, new Vector3(c.x, 0.04f, c.z), new Vector3(w - 0.7f, 0.04f, d - 0.7f), m, false); }

    static void Shelf(Vector3 p, float w, float h, Material m)
    {
        Bx("ShelfBack", tProps, p + new Vector3(0, h / 2f, 0), new Vector3(w, h, 0.5f), mDark);
        for (int i = 1; i <= 3; i++) Bx("ShelfBoard", tProps, p + new Vector3(0, i * h / 4f, -0.15f), new Vector3(w, 0.08f, 0.5f), m, false);
    }

    static void ShelfGoods(Vector3 p, float yaw)
    {
        var g = Holder("ShelfGoods", tProps, p, yaw);
        Bx("Unit", g, new Vector3(0, 1.2f, 0), new Vector3(2.2f, 2.4f, 0.55f), mDark);
        Material[] goods = { mRed, mGreen, mBlue, mGold, mLeaf, mCloth };
        for (int row = 0; row < 3; row++)
        {
            Bx("Board", g, new Vector3(0, 0.45f + row * 0.8f, 0.05f), new Vector3(2.2f, 0.06f, 0.6f), mWood, false);
            for (int i = 0; i < 5; i++)
                Bx("Goods", g, new Vector3(-0.85f + i * 0.43f, 0.7f + row * 0.8f, 0.08f), new Vector3(0.28f, 0.28f + (float)rng.NextDouble() * 0.12f, 0.3f), goods[rng.Next(goods.Length)], false);
        }
    }

    static void BookStack(Vector3 p, float w, float len)
    {
        if (len < 0.1f) return;
        Bx("BookCase", tProps, p + new Vector3(0, 2.2f, len / 2f), new Vector3(w, 4.4f, len), mDark);
        Material[] spines = { mRed, mBlue, mGreen, mGold, mCloth, mCream };
        for (int row = 0; row < 5; row++)
            for (int i = 0; i < (int)(len / 0.9f); i++)
            {
                var m = spines[rng.Next(spines.Length)];
                Bx("Books", tProps, p + new Vector3(w / 2f + 0.01f, 0.5f + row * 0.8f, 0.5f + i * 0.9f), new Vector3(0.08f, 0.55f, 0.6f), m, false);
                Bx("Books", tProps, p + new Vector3(-w / 2f - 0.01f, 0.5f + row * 0.8f, 0.5f + i * 0.9f), new Vector3(0.08f, 0.55f, 0.6f), spines[rng.Next(spines.Length)], false);
            }
    }

    static void BookWall(Vector3 p, float yaw)
    {
        var g = Holder("BookWall", tProps, p, yaw);
        Bx("Case", g, new Vector3(0, 3.0f, 0), new Vector3(7f, 6f, 0.5f), mDark);
        Material[] spines = { mRed, mBlue, mGreen, mGold, mCloth, mCream };
        for (int row = 0; row < 6; row++)
            for (int i = 0; i < 12; i++)
                Bx("Books", g, new Vector3(-3.3f + i * 0.6f, 0.6f + row * 0.95f, 0.28f), new Vector3(0.4f, 0.65f, 0.1f), spines[rng.Next(spines.Length)], false);
    }

    static void WeaponRack(Vector3 p)
    {
        var g = Holder("WeaponRack", tProps, p, 90f);
        Bx("Back", g, new Vector3(0, 1.5f, 0), new Vector3(2.6f, 2.2f, 0.18f), mWood);
        for (int i = 0; i < 4; i++)
        {
            var s = Bx("Sword", g, new Vector3(-0.9f + i * 0.6f, 1.7f, 0.2f), new Vector3(0.1f, 1.5f, 0.04f), mIron, false);
            s.transform.localRotation = Quaternion.Euler(0, 0, (i % 2 == 0 ? 8f : -8f));
            Bx("Hilt", g, new Vector3(-0.9f + i * 0.6f, 0.95f, 0.2f), new Vector3(0.3f, 0.06f, 0.06f), mGold, false);
        }
    }

    static void ArmorStand(Vector3 p)
    {
        Cyl("StandPole", tProps, p, 0.06f, 1.4f, mWood);
        Bx("Torso", tProps, p + new Vector3(0, 1.45f, 0), new Vector3(0.7f, 0.8f, 0.4f), mIron);
        Sphere("Helm", tProps, p + new Vector3(0, 2.05f, 0), 0.4f, mIron);
        Bx("Base", tProps, p + new Vector3(0, 0.05f, 0), new Vector3(0.7f, 0.1f, 0.7f), mDark, false);
    }

    static void Fireplace(Vector3 p, float yaw)
    {
        var g = Holder("Fireplace", tProps, p, yaw);
        Bx("Hearth", g, new Vector3(0, 1.4f, 0), new Vector3(3.2f, 2.8f, 0.9f), mStone);
        Bx("Opening", g, new Vector3(0, 0.85f, 0.2f), new Vector3(1.8f, 1.5f, 0.6f), mDark, false);
        Bx("Fire", g, new Vector3(0, 0.55f, 0.25f), new Vector3(1.0f, 0.5f, 0.3f), mGlow, false);
        Bx("Mantle", g, new Vector3(0, 2.9f, 0.1f), new Vector3(3.6f, 0.2f, 1.2f), mWood, false);
        var l = AddLight(g, new Vector3(0, 1f, 1.2f), 11f, 2f, new Color(1f, 0.55f, 0.2f)); l.gameObject.AddComponent<FlickerLight>();
    }

    static void Pew(Vector3 p)
    {
        Bx("PewSeat", tProps, p + new Vector3(0, 0.4f, 0), new Vector3(5.4f, 0.12f, 0.7f), mWood);
        Bx("PewBack", tProps, p + new Vector3(0, 0.95f, -0.3f), new Vector3(5.4f, 0.9f, 0.1f), mWood, false);
        Bx("PewSideA", tProps, p + new Vector3(-2.6f, 0.3f, 0), new Vector3(0.12f, 0.6f, 0.7f), mDark, false);
        Bx("PewSideB", tProps, p + new Vector3(2.6f, 0.3f, 0), new Vector3(0.12f, 0.6f, 0.7f), mDark, false);
    }

    static void Candelabra(Vector3 p)
    {
        Cyl("CandPole", tProps, p, 0.08f, 1.5f, mGold);
        Cyl("CandBase", tProps, p, 0.3f, 0.08f, mGold, false);
        Sphere("Flame", tProps, p + new Vector3(0, 1.65f, 0), 0.18f, mGlow);
        var l = AddLight(tLights, p + new Vector3(0, 1.8f, 0), 7f, 0.9f, new Color(1f, 0.75f, 0.35f)); l.gameObject.AddComponent<FlickerLight>();
    }

    static void Window(Vector3 p, bool onSideWall, float w = 1.4f, float h = 2.2f)
    {
        var g = Holder("Window", tProps, p, p.x < 0 ? 90f : -90f);
        Bx("Frame", g, new Vector3(0, 0, 0), new Vector3(w + 0.3f, h + 0.3f, 0.1f), mDark, false);
        Bx("Glass", g, new Vector3(0, 0, 0.06f), new Vector3(w, h, 0.05f), mWindow, false);
        Bx("MullionV", g, new Vector3(0, 0, 0.1f), new Vector3(0.08f, h, 0.05f), mDark, false);
        Bx("MullionH", g, new Vector3(0, 0, 0.1f), new Vector3(w, 0.08f, 0.05f), mDark, false);
        var l = AddLight(g, new Vector3(0, 0, 1.2f), 7f, 0.5f, new Color(0.7f, 0.8f, 1f));
    }

    static void Lamp(Vector3 p, float range, float intensity)
    {
        Bx("LampCord", tProps, new Vector3(p.x, (p.y + H) / 2f, p.z), new Vector3(0.03f, H - p.y, 0.03f), mDark, false);
        Sphere("Lantern", tProps, p, 0.3f, mGlow);
        AddLight(tLights, p, range, intensity, new Color(1f, 0.82f, 0.55f));
    }

    // =====================================================================
    // Detail ruangan (n42): wainscot, cornice, pilaster sudut, balok silang, tanaman, lukisan, spanduk. Semua tanpa collider.
    static void Decorate()
    {
        var g = Group("Decor", tStruct.parent);
        float hw = W / 2f - T / 2f, zN = D - T / 2f, zS = T / 2f;
        // wainscot panel kayu + pegangan kursi
        float wh = 1.15f;
        Bx("WainN", g, new Vector3(0, wh / 2f, zN - 0.05f), new Vector3(W - 0.3f, wh, 0.1f), mWood, false);
        Bx("WainW", g, new Vector3(-hw + 0.05f, wh / 2f, D / 2f), new Vector3(0.1f, wh, D - 0.3f), mWood, false);
        Bx("WainE", g, new Vector3(hw - 0.05f, wh / 2f, D / 2f), new Vector3(0.1f, wh, D - 0.3f), mWood, false);
        float seg = W / 2f - 1.8f;
        Bx("WainSL", g, new Vector3(-1.8f - seg / 2f, wh / 2f, zS + 0.05f), new Vector3(seg, wh, 0.1f), mWood, false);
        Bx("WainSR", g, new Vector3(1.8f + seg / 2f, wh / 2f, zS + 0.05f), new Vector3(seg, wh, 0.1f), mWood, false);
        Bx("RailN", g, new Vector3(0, wh, zN - 0.08f), new Vector3(W - 0.2f, 0.1f, 0.16f), mDark, false);
        Bx("RailW", g, new Vector3(-hw + 0.08f, wh, D / 2f), new Vector3(0.16f, 0.1f, D - 0.2f), mDark, false);
        Bx("RailE", g, new Vector3(hw - 0.08f, wh, D / 2f), new Vector3(0.16f, 0.1f, D - 0.2f), mDark, false);
        for (int i = 0; i < Mathf.RoundToInt(W / 2.2f); i++)
        {
            float x = -W / 2f + 1.1f + i * 2.2f;
            if (x > W / 2f - 0.6f) break;
            Bx("PanelN" + i, g, new Vector3(x, wh * 0.5f, zN - 0.12f), new Vector3(1.5f, 0.8f, 0.04f), mDark, false);
        }
        // cornice bergigi
        Bx("CorniceN", g, new Vector3(0, H - 0.12f, zN - 0.12f), new Vector3(W - 0.2f, 0.24f, 0.24f), mDark, false);
        Bx("CorniceW", g, new Vector3(-hw + 0.12f, H - 0.12f, D / 2f), new Vector3(0.24f, 0.24f, D - 0.2f), mDark, false);
        Bx("CorniceE", g, new Vector3(hw - 0.12f, H - 0.12f, D / 2f), new Vector3(0.24f, 0.24f, D - 0.2f), mDark, false);
        Bx("CorniceS", g, new Vector3(0, H - 0.12f, zS + 0.12f), new Vector3(W - 0.2f, 0.24f, 0.24f), mDark, false);
        for (int i = 0; i < Mathf.RoundToInt(W / 0.8f); i++)
        {
            float x = -W / 2f + 0.5f + i * 0.8f;
            Bx("Dentil", g, new Vector3(x, H - 0.34f, zN - 0.1f), new Vector3(0.22f, 0.16f, 0.2f), mCream, false);
        }
        // pilaster sudut + balok silang & korbel
        foreach (float sx in new[] { -1f, 1f })
            foreach (float z in new[] { zS + 0.2f, zN - 0.2f })
            {
                Bx("Pilaster", g, new Vector3(sx * (hw - 0.2f), H / 2f, z), new Vector3(0.4f, H, 0.4f), mStone, false);
                Bx("PilasterCap", g, new Vector3(sx * (hw - 0.2f), H - 0.4f, z), new Vector3(0.55f, 0.2f, 0.55f), mDark, false);
                Bx("PilasterBase", g, new Vector3(sx * (hw - 0.2f), 0.15f, z), new Vector3(0.55f, 0.3f, 0.55f), mDark, false);
            }
        for (float x = -W / 2f + 3f; x < W / 2f - 1f; x += 4f)
        {
            Bx("CrossBeam", g, new Vector3(x, H - 0.45f, D / 2f), new Vector3(0.36f, 0.3f, D - 0.4f), mDark, false);
            for (float z = 3f; z < D - 1f; z += 4f) Bx("Corbel", g, new Vector3(x, H - 0.75f, z), new Vector3(0.5f, 0.3f, 0.5f), mWood, false);
        }
        // karpet jalan dari pintu & ubin tepi
        Bx("Runner", g, new Vector3(0, 0.025f, Mathf.Min(D * 0.35f, 6f)), new Vector3(2.2f, 0.03f, Mathf.Min(D * 0.7f, 12f)), mRed, false);
        Bx("RunnerEdge", g, new Vector3(0, 0.03f, Mathf.Min(D * 0.35f, 6f)), new Vector3(2.5f, 0.02f, Mathf.Min(D * 0.7f, 12f) + 0.2f), mGold, false);
        Bx("FloorBorderN", g, new Vector3(0, 0.02f, zN - 0.5f), new Vector3(W - 0.8f, 0.02f, 0.3f), mStone, false);
        Bx("FloorBorderW", g, new Vector3(-hw + 0.5f, 0.02f, D / 2f), new Vector3(0.3f, 0.02f, D - 0.8f), mStone, false);
        Bx("FloorBorderE", g, new Vector3(hw - 0.5f, 0.02f, D / 2f), new Vector3(0.3f, 0.02f, D - 0.8f), mStone, false);
        // lukisan & spanduk di dinding utara
        int np = Mathf.Max(2, Mathf.RoundToInt(W / 7f));
        for (int i = 0; i < np; i++)
        {
            float x = -W / 2f + (i + 0.5f) * W / np;
            Material cv = i % 3 == 0 ? mBlue : (i % 3 == 1 ? mGreen : mRed);
            Bx("PaintFrame", g, new Vector3(x, H * 0.62f, zN - 0.16f), new Vector3(1.6f, 1.2f, 0.08f), mGold, false);
            Bx("PaintCanvas", g, new Vector3(x, H * 0.62f, zN - 0.2f), new Vector3(1.4f, 1.0f, 0.04f), cv, false);
            Bx("PaintHill", g, new Vector3(x - 0.2f, H * 0.62f - 0.25f, zN - 0.23f), new Vector3(0.7f, 0.3f, 0.02f), mLeaf, false);
            Sphere("PaintSun", g, new Vector3(x + 0.35f, H * 0.62f + 0.25f, zN - 0.23f), 0.22f, mGold);
        }
        // sconce lilin di dinding samping
        for (int i = 0; i < 3; i++)
        {
            float z = D * (i + 1) / 4f;
            foreach (float sx in new[] { -1f, 1f })
            {
                Bx("SconceArm", g, new Vector3(sx * (hw - 0.15f), 2.5f, z), new Vector3(0.3f, 0.06f, 0.06f), mIron, false);
                Cyl("Candle", g, new Vector3(sx * (hw - 0.3f), 2.52f, z), 0.04f, 0.2f, mCream, false);
                Sphere("CandleFlame", g, new Vector3(sx * (hw - 0.3f), 2.8f, z), 0.1f, mGlow);
            }
        }
        // tanaman pot di sudut dekat pintu
        foreach (float sx in new[] { -1f, 1f })
        {
            Vector3 pp = new Vector3(sx * (hw - 0.7f), 0f, zS + 0.8f);
            Cyl("PlantPot", g, pp, 0.32f, 0.55f, mWood, false);
            for (int k = 0; k < 5; k++)
                Sphere("Leaf", g, pp + new Vector3(Mathf.Sin(k * 1.3f) * 0.22f, 0.8f + (k % 3) * 0.18f, Mathf.Cos(k * 1.3f) * 0.22f), 0.42f, mLeaf);
            Sphere("Bloom", g, pp + new Vector3(0.1f, 1.35f, 0.05f), 0.16f, mRed);
        }
        // ikat herba & bawang kering di balok
        for (int k = 0; k < 4; k++)
        {
            float x = -W / 2f + 2f + k * (W - 4f) / 3f;
            Bx("HerbString", g, new Vector3(x, H - 0.9f, 1.2f), new Vector3(0.03f, 0.8f, 0.03f), mCream, false);
            Sphere("Herb", g, new Vector3(x, H - 1.35f, 1.2f), 0.3f, mLeaf);
        }
    }

    // =====================================================================
    // Pemain & manajer
    static void BuildPlayerAndManagers()
    {
        var pl = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        pl.name = "Player"; pl.tag = "Player";
        pl.transform.position = InteriorRoute.EntryPos;
        pl.GetComponent<Renderer>().sharedMaterial = Mat("IPlayer", new Color(0.2f, 0.4f, 0.85f));
        Object.DestroyImmediate(pl.GetComponent<CapsuleCollider>());
        var cc = pl.AddComponent<CharacterController>();
        cc.height = 2f; cc.radius = 0.4f; cc.center = Vector3.zero; cc.stepOffset = 0.4f;
        pl.AddComponent<PlayerController>();
        pl.AddComponent<HunterGuild.PlayerAvatar>();

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.03f, 0.05f);
        cam.fieldOfView = 65f;
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = InteriorRoute.EntryPos + new Vector3(0, 2.5f, -3.5f);
        var tpc = camGo.AddComponent<ThirdPersonCamera>();
        tpc.target = pl.transform; tpc.distance = 4.2f; tpc.height = 1.7f;

        new GameObject("GameManager").AddComponent<GameState>();
        new GameObject("GuildData").AddComponent<GuildData>();
        new GameObject("TownUI").AddComponent<TownUI>();
        new GameObject("BackpackUI").AddComponent<BackpackUI>();
        new GameObject("SkillUI").AddComponent<SkillUI>();
    }

    static void Npc(string label, int outfit, Vector3 pos, float yaw)
    {
        var spec = NpcKit.RandomSpec(outfit, rng);
        var go = NpcKit.Spawn(tNpcs, "NPC_" + label, pos, yaw, spec, true);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        var actor = go.GetComponent<NpcActor>();
        actor.displayName = spec.name;
        var lab = BuildKit.Text(go.transform, spec.name + "\n<" + label + ">", new Vector3(0f, actor.height + 0.3f, 0f), 0f, 0.045f, Color.white);
        var w = go.AddComponent<WanderNPC>();
        w.waypoints = null; w.label = lab.transform; w.speed = 1.2f;
        if (outfit != NpcKit.OPenjaga && outfit != NpcKit.OKsatria) go.AddComponent<NpcChat>();
    }

    // =====================================================================
    // Primitif & utilitas
    static T Act<T>(string name, Vector3 pos, string prompt) where T : Interactable
    {
        var go = new GameObject(name);
        go.transform.SetParent(tAct, false);
        go.transform.localPosition = pos;
        var c = go.AddComponent<T>();
        c.prompt = prompt; c.radius = 3f;
        Cyl("Marker", go.transform, new Vector3(0, 0.02f, 0), 1.0f, 0.03f, mGlow, false);
        return c;
    }

    static Transform Holder(string name, Transform parent, Vector3 pos, float yaw)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false); t.localPosition = pos; t.localRotation = Quaternion.Euler(0, yaw, 0);
        return t;
    }

    static GameObject Bx(string name, Transform parent, Vector3 center, Vector3 size, Material m, bool collider = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name; go.transform.SetParent(parent, false);
        go.transform.localPosition = center; go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = m;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static GameObject Cyl(string name, Transform parent, Vector3 basePos, float radius, float height, Material m, bool collider = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name; go.transform.SetParent(parent, false);
        go.transform.localPosition = basePos + Vector3.up * (height / 2f);
        go.transform.localScale = new Vector3(radius * 2f, height / 2f, radius * 2f);
        go.GetComponent<Renderer>().sharedMaterial = m;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static GameObject Sphere(string name, Transform parent, Vector3 pos, float diameter, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name; go.transform.SetParent(parent, false);
        go.transform.localPosition = pos; go.transform.localScale = Vector3.one * diameter;
        go.GetComponent<Renderer>().sharedMaterial = m;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static Light AddLight(Transform parent, Vector3 pos, float range, float intensity, Color color)
    {
        var go = new GameObject("PointLight");
        go.transform.SetParent(parent, false); go.transform.localPosition = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point; l.range = range; l.intensity = intensity; l.color = color; l.shadows = LightShadows.None;
        return l;
    }

    static TextMesh Text3D(string name, string text, Transform parent, Vector3 pos, Vector3 viewerDir, float charSize, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false); go.transform.localPosition = pos;
        go.transform.rotation = Quaternion.LookRotation(-viewerDir, Vector3.up);
        var tm = go.AddComponent<TextMesh>();
        tm.text = text; tm.fontSize = 64; tm.characterSize = charSize;
        tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = color;
#if UNITY_2022_2_OR_NEWER
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
        tm.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        HunterGuild.TextMat.Apply(go.GetComponent<MeshRenderer>(), tm.font.material);
        return tm;
    }

    static Transform Group(string name, Transform parent) { var t = new GameObject(name).transform; t.SetParent(parent, false); return t; }

    static void MakeMats()
    {
        mFloor = Mat("IFloor", new Color(0.38f, 0.26f, 0.15f)); mWall = Mat("IWall", new Color(0.70f, 0.62f, 0.50f)); mCeil = Mat("ICeil", new Color(0.30f, 0.22f, 0.14f));
        mWood = Mat("IWood", new Color(0.46f, 0.31f, 0.17f)); mDark = Mat("IDark", new Color(0.20f, 0.12f, 0.07f)); mStone = Mat("IStone", new Color(0.52f, 0.50f, 0.48f));
        mCloth = Mat("ICloth", new Color(0.55f, 0.30f, 0.45f)); mRed = Mat("IRed", new Color(0.62f, 0.14f, 0.14f)); mGold = Mat("IGold", new Color(0.88f, 0.70f, 0.25f));
        mIron = Mat("IIron", new Color(0.30f, 0.31f, 0.34f)); mGlow = Mat("IGlow", new Color(1f, 0.72f, 0.3f), 2.2f); mWindow = Mat("IWindow", new Color(0.6f, 0.75f, 1f), 1.3f);
        mPaper = Mat("IPaper", new Color(0.92f, 0.88f, 0.74f)); mLeaf = Mat("ILeaf", new Color(0.45f, 0.62f, 0.28f)); mBlue = Mat("IBlue", new Color(0.22f, 0.34f, 0.68f));
        mGreen = Mat("IGreen", new Color(0.24f, 0.50f, 0.30f)); mCream = Mat("ICream", new Color(0.88f, 0.84f, 0.72f));
    }

    static Material Mat(string name, Color c, float glow = 0f)
    {
        string path = MatDir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            Shader sh = GraphicsSettings.currentRenderPipeline != null ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Standard");
            m = new Material(sh);
            m.name = System.IO.Path.GetFileNameWithoutExtension(path); AssetDatabase.CreateAsset(m, path);
        }
        m.color = c;
        if (glow > 0f) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * glow); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
        EditorUtility.SetDirty(m);
        return m;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int i = path.LastIndexOf('/');
        EnsureFolder(path.Substring(0, i));
        AssetDatabase.CreateFolder(path.Substring(0, i), path.Substring(i + 1));
    }

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
