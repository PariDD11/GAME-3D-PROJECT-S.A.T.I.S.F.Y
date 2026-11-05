using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HunterGuild;

/// <summary>
/// v35z102n73 "Kampung Tepi Kali": Desa Pemula DIBANGUN ULANG TOTAL (denah, medan, bangunan, prop). Data denah ada di Vil2Plan.cs (kelas V2),
/// model bangunan di Vil2Kit*.cs (kelas V2Kit). Berkas ini: alur build, palet, medan + cat tanah, jalan, jembatan, gerbang, titik muncul.
/// Berkas lain: TownSceneBuilder.Village2Lots.cs (bangunan + pengait gameplay), TownSceneBuilder.Village2Areas.cs (ladang, padang, kebun, makam, lapangan latihan, pohon, lampu, NPC).
/// Semua pengait gameplay lama dipertahankan (SpawnStart, RespawnPoint, SpawnFromForest/Town, Elder, Inn, Healer, Smithy, Property, TrainingStation, LevelGate, VillageExit).
/// </summary>
public static partial class TownSceneBuilder
{
    const string V2MarkerPath = "Assets/HGLayout/StartVillage.v2";
    static readonly Color V2Gold = new Color(1f, 0.84f, 0.42f);

    // =====================================================================
    // Alur utama
    // =====================================================================
    public static void V2BuildVillage()
    {
        V2Kit.SaltAtDoor = true;                    // n82: garis garam di ambang hanya dibuat saat membangun Desa Pemula (kit rumah dipakai juga oleh Kota)
        try { V2BuildVillageCore(); }
        finally { V2Kit.SaltAtDoor = false; }
    }

    static void V2BuildVillageCore()
    {
        if (!VConfirm(VillageScenePath)) return;
        V2LayoutMigrate();
        complexScene = null;
        vStreetsActive = false;
        vMerged = true;                      // Desa + Hutan Pemula tetap SATU scene (hutan disambung di ujung jalan utara)
        EditorUtility.DisplayProgressBar("Hunter Guild", "Kampung Tepi Kali: bahan & medan...", 0.05f);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        VInit("DesaPemula");
        V2.Build();
        baseSlots = V2Palette();
        vMap.Clear();
        new GameObject("HG_DenahV2").transform.SetParent(tTown, false);          // penanda versi denah di scene
        V2Terrain();
        EditorUtility.DisplayProgressBar("Hunter Guild", "Jalan, jembatan & gerbang...", 0.2f);
        V2RoadMeshes();
        V2Bridges();
        V2Gates();
        EditorUtility.DisplayProgressBar("Hunter Guild", "Bangunan & fasilitas...", 0.4f);
        V2Lots();
        V2Areas();
        EditorUtility.DisplayProgressBar("Hunter Guild", "Pohon, lampu, NPC, pemain...", 0.65f);
        V2Lamps();
        V2Trees();
        V2Scatter();
        V2Canon();                           // n82: tali merah, ikatan beringin, meja Basra, rumah bergaris garam (kanon Desa Pemula); setelah sebaran acak agar urutan acaknya tidak bergeser
        V2Npcs();
        V2Spawns();
        V2BuildMapBoard();
        VNorthRoadTrees();
        EditorUtility.DisplayProgressBar("Hunter Guild", "Hutan Pemula (disambung ke jalan utara)...", 0.8f);
        VMergeForest();                      // setelah ini vH = fungsi hutan: kode desa memakai V2.H langsung
        VPlayer(new Vector3(0f, V2.H(V2.Spawn.x, V2.Spawn.y) + 0.05f, V2.Spawn.y), 0f, false);
        VFinishPre(0.0011f, new Color(0.66f, 0.78f, 0.88f));
        BuildingMover.Prepare("StartVillage", new[] { tGates, tBuildings, tHouses, tNoble }, new[] { tProps, tBuildings, tHouses, tNoble });
        HeightBake.BackupScene(VillageScenePath); EditorSceneManager.SaveScene(scene, VillageScenePath);
        vMerged = false;
        VFinishPost(VillageScenePath,
            "Desa Pemula 'Kampung Tepi Kali' + Hutan Pemula dibuat sebagai SATU scene: " + VillageScenePath + "\n" + houseCount + " rumah, " + npcCount + " NPC.\nDenah, medan, bangunan, dan prop desa semuanya baru.");
    }

    /// <summary>Denah lama (n72 ke bawah) dibuang: posisi manual tersimpannya tidak cocok dengan denah baru.</summary>
    static void V2LayoutMigrate()
    {
        try
        {
            if (File.Exists(V2MarkerPath)) { BuildingMover.CaptureBeforeRebuild(VillageScenePath); return; }       // scene terakhir sudah denah baru: simpan posisi manual
            string json = "Assets/HGLayout/StartVillage.json";
            if (File.Exists(json)) { AssetDatabase.DeleteAsset(json); Debug.Log("[Desa V2] Posisi manual denah lama dibuang (denah desa diganti total)."); }
            if (!AssetDatabase.IsValidFolder("Assets/HGLayout")) AssetDatabase.CreateFolder("Assets", "HGLayout");
            File.WriteAllText(V2MarkerPath, "Denah Desa Pemula v2 (Kampung Tepi Kali). Hapus berkas ini bila ingin membuang posisi manual saat build berikutnya.");
            AssetDatabase.ImportAsset(V2MarkerPath);
        }
        catch (System.Exception e) { Debug.LogWarning("[Desa V2] Migrasi layout dilewati: " + e.Message); }
    }

    // =====================================================================
    // Palet: seluruh 37 slot terisi (tidak ada sub-mesh tanpa material). Leather/Fur/Hair/Lip/EyeW tetap bahan pohon dari TownMats.Slots()
    // =====================================================================
    static Material[] V2Palette()
    {
        var s = TownMats.Slots();
        s[Sl.SkinS] = UMat("V2_Skin", new Color(0.86f, 0.66f, 0.54f));
        s[Sl.Eye] = UMat("V2_Eye", new Color(0.08f, 0.07f, 0.06f));
        s[Sl.CoatA] = UMat("V2_CoatA", new Color(0.38f, 0.27f, 0.19f));
        s[Sl.CoatB] = UMat("V2_CoatB", new Color(0.26f, 0.32f, 0.42f));
        s[Sl.CoatC] = UMat("V2_CoatC", new Color(0.42f, 0.34f, 0.24f));
        s[Sl.Dark] = UMat("V2_Dark", new Color(0.09f, 0.08f, 0.08f));
        for (int i = 0; i < s.Length; i++) if (s[i] == null) s[i] = UMat("V2_Fallback", new Color(0.5f, 0.45f, 0.4f));
        return s;
    }

    /// <summary>Salinan palet dengan plester/atap berbeda per bangunan (variasi warna kampung).</summary>
    static Material[] V2Slots(int variant)
    {
        var s = (Material[])baseSlots.Clone();
        int np = TownMats.Plaster.Length, nr = TownMats.Roofs.Length;
        s[Sl.Plaster] = TownMats.Plaster[((variant % np) + np) % np];
        s[Sl.PlasterB] = TownMats.Plaster[(((variant + 2) % np) + np) % np];
        s[Sl.Roof] = TownMats.Roofs[(((variant + 1) % nr) + nr) % nr];
        s[Sl.RoofB] = TownMats.Roofs[(((variant + 2) % nr) + nr) % nr];
        return s;
    }

    // =====================================================================
    // Medan
    // =====================================================================
    static float[][] v2RoadBox;

    static void V2Terrain()
    {
        v2RoadBox = new float[V2.Roads.Count][];
        for (int i = 0; i < V2.Roads.Count; i++)
        {
            float x0 = 1e9f, x1 = -1e9f, z0 = 1e9f, z1 = -1e9f;
            foreach (var p in V2.Roads[i].pts) { x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x); z0 = Mathf.Min(z0, p.y); z1 = Mathf.Max(z1, p.y); }
            v2RoadBox[i] = new[] { x0 - 9f, x1 + 9f, z0 - 9f, z1 + 9f };
        }
        vH = (x, z) => V2.H(x, z);
        vGrassDens = 0.8f;
        VBuildTerrain("VillageTerrain", 240f, V2Paint);
        vGrassDens = 0.4f;
    }

    static float[] V2Paint(float x, float z, float h)
    {
        float cob = 0f, dirt = 0f;
        for (int i = 0; i < V2.Roads.Count; i++)
        {
            var bx = v2RoadBox[i];
            if (x < bx[0] || x > bx[1] || z < bx[2] || z > bx[3]) continue;
            var r = V2.Roads[i];
            float d = V2.DistPoly(x, z, r.pts) - r.width * 0.5f;
            if (r.cobble) { cob = Mathf.Max(cob, V2.Sm(1f - d / 0.7f)); dirt = Mathf.Max(dirt, V2.Sm(1f - (d - 0.2f) / 2.2f) * 0.85f); }
            else dirt = Mathf.Max(dirt, V2.Sm(1f - d / 1.1f));
        }
        var lots = V2.Lots;
        for (int i = 0; i < lots.Count; i++)
        {
            var l = lots[i];
            if (Mathf.Abs(x - l.x) > l.w + l.d + 8f || Mathf.Abs(z - l.z) > l.w + l.d + 8f) continue;
            float dr = V2.DistToRect(x, z, l);
            switch (l.kind)
            {
                case "yard": dirt = Mathf.Max(dirt, V2.Sm(1f - dr / 1.5f)); break;
                case "field": dirt = Mathf.Max(dirt, V2.Sm(1f - dr / 1.2f) * 0.95f); break;
                case "pasture": case "orchard": case "bridgepad": case "graveyard": break;
                default: dirt = Mathf.Max(dirt, V2.Sm(1f - (dr - 0.6f) / 2.4f) * 0.55f); break;       // tanah pijakan di sekeliling bangunan
            }
        }
        float bd = Mathf.Sqrt(((x - V2.Basin.x) / V2.BasinRx) * ((x - V2.Basin.x) / V2.BasinRx) + ((z - V2.Basin.y) / V2.BasinRz) * ((z - V2.Basin.y) / V2.BasinRz));
        float litter = Mathf.Lerp(0.1f, 1f, V2.Sm((bd - 0.92f) / 0.3f));
        // lumpur/lumut basah hanya di tepi kali: lantai cekungan yang rendah tetap rumput (bukan bercak cokelat)
        float bank = V2.StreamBank(x, z);
        float hp = Mathf.Lerp(Mathf.Max(h, VWaterLevel + 1.3f), h, bank);
        return VPaintStd(x, z, hp, dirt * (1f - cob), cob, litter);
    }

    // =====================================================================
    // Jalan: pita batu bulat / tanah yang mengikuti medan
    // =====================================================================
    static bool V2OnBridge(Vector2 p)
    {
        foreach (var br in V2.Bridges)
        {
            float a = br.yaw * Mathf.Deg2Rad;
            Vector2 t = new Vector2(Mathf.Sin(a), Mathf.Cos(a)), n = new Vector2(t.y, -t.x), d = p - br.c;
            if (Mathf.Abs(Vector2.Dot(d, t)) < br.L * 0.5f + 0.4f && Mathf.Abs(Vector2.Dot(d, n)) < br.W + 1.5f) return true;
        }
        return false;
    }

    /// <summary>Pita sepanjang pts selebar width; tinggi tiap titik = medan + yOff; segmen di atas jembatan dilewati.</summary>
    static void V2Strip(GeoBag b, int slot, IList<Vector2> pts, float width, float yOff, int lat)
    {
        float total = V2.Len(pts), step = 1.3f;
        int n = Mathf.Max(2, Mathf.CeilToInt(total / step));
        Vector3[] prev = null;
        for (int i = 0; i <= n; i++)
        {
            Vector2 tg; Vector2 p = V2.At(pts, total * i / n, out tg);
            Vector2 nr = new Vector2(tg.y, -tg.x);
            var row = new Vector3[lat + 1];
            for (int k = 0; k <= lat; k++)
            {
                float o = (k / (float)lat - 0.5f) * width;
                Vector2 q = p + nr * o;
                row[k] = new Vector3(q.x, V2.H(q.x, q.y) + yOff, q.y);
            }
            if (prev != null && !V2OnBridge(p))
                for (int k = 0; k < lat; k++) V2Kit.QUp(b, slot, prev[k], prev[k + 1], row[k + 1], row[k]);
            prev = row;
        }
    }

    static void V2RoadMeshes()
    {
        var g = Group("JalanDesa", tRoads);
        var edge = new GeoBag(); var cob = new GeoBag(); var lane = new GeoBag();
        for (int i = 0; i < V2.Roads.Count; i++)
        {
            var r = V2.Roads[i];
            if (r.cobble)
            {
                V2Strip(edge, Sl.Dirt, r.pts, r.width + 1.5f, 0.03f + i * 0.004f, 3);
                V2Strip(cob, Sl.Cobble, r.pts, r.width, 0.06f + i * 0.004f, 4);
            }
            else V2Strip(lane, Sl.Dirt, r.pts, r.width, 0.045f + i * 0.004f, 3);
        }
        edge.Emit(g, "TepiJalan", baseSlots, MeshDir + "/V2_RoadEdge.asset");
        lane.Emit(g, "JalanTanah", baseSlots, MeshDir + "/V2_Lanes.asset");
        cob.Emit(g, "JalanBatu", baseSlots, MeshDir + "/V2_Cobble.asset");
    }

    // =====================================================================
    // Jembatan (batu lengkung di jalan utama, titian kayu di jalur latihan)
    // =====================================================================
    static void V2Bridges()
    {
        var g = Group("Jembatan", tProps);
        foreach (var br in V2.Bridges)
        {
            var b = new GeoBag();
            float springRel = VWaterLevel - br.y;
            b.Push(new Vector3(br.c.x, br.y, br.c.y), br.yaw);
            if (br.stone) V2Kit.StoneBridge(b, br.L, br.W, 1.7f, springRel, br.chord);
            else V2Kit.PlankBridge(b, br.L, br.W, springRel);
            b.Pop();
            var go = b.Emit(g, br.stone ? "JembatanBatuKaliEmbun" : "TitianKayuKaliEmbun", baseSlots, MeshDir + "/V2_" + br.id + ".asset");
            var mf = go.GetComponent<MeshFilter>();
            if (mf != null) go.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            VMapMark(br.stone ? "Jembatan Batu" : "Titian Kayu", new Vector2(br.c.x, br.c.y), 4);
            // lentera di kedua ujung jembatan batu
            if (br.stone)
            {
                float a = br.yaw * Mathf.Deg2Rad;
                Vector2 t = new Vector2(Mathf.Sin(a), Mathf.Cos(a)), n = new Vector2(t.y, -t.x);
                foreach (float sg in new[] { -1f, 1f })
                {
                    Vector2 p = br.c + t * (sg * (br.L * 0.5f + 2.6f)) + n * (br.W * 0.5f + 1.1f);
                    V2PlaceLantern(p.x, p.y, br.yaw + 90f, true);
                }
            }
        }
    }

    // =====================================================================
    // Gerbang kayu di mulut ngarai + penjaga + penghalang level + keluar scene
    // =====================================================================
    static Transform V2OneGate(string name, Vector2 gp, float az, string textIn, string textOut)
    {
        Vector3 c = new Vector3(gp.x, V2.H(gp.x, gp.y), gp.y);
        var root = new GameObject(name).transform;
        root.SetParent(tGates, false);
        root.position = c;
        root.rotation = Quaternion.Euler(0f, az, 0f);
        var b = new GeoBag();
        V2Kit.LychGate(b, true);
        b.Emit(root, "Kayu", baseSlots, MeshDir + "/V2_" + name + ".asset");
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                BoxCol(root, "TiangCol", new Vector3(sx * 4.15f, 2.6f, sz * 1.45f), new Vector3(1.1f, 5.2f, 1.1f), Vector3.zero);
        // papan nama: sisi dalam dibaca dari dalam desa, sisi luar dibaca dari luar (kunci teks dipakai VisualPolish.FitSigns)
        BuildKit.Text(root, textIn, new Vector3(0f, 3.7f, -0.19f), 0f, 0.075f, V2Gold);
        BuildKit.Text(root, textOut, new Vector3(0f, 3.7f, 0.19f), 180f, 0.075f, V2Gold);
        AddLight(root, new Vector3(0f, 3.3f, -2.4f), 14f, 1.3f);
        AddLight(root, new Vector3(0f, 3.3f, 2.4f), 14f, 1.1f);
        return root;
    }

    static void V2Gates()
    {
        var gateN = V2OneGate("Gerbang_Utara", V2.GateN, 0f, "GERBANG UTARA\nHutan Pemula (Lv 0-5)", "HUTAN PEMULA\nLatihan s/d Lv 5");
        var gateE = V2OneGate("Gerbang_Timur", V2.GateE, 90f, "GERBANG TIMUR\nAshwatch (Lv 5+)", "ASHWATCH\nHunter Lv 5+");
        VMapMark("Gerbang Utara (Hutan)", V2.GateN, 5);
        VMapMark("Gerbang Timur (Kota)", V2.GateE, 5);

        // penghalang level di gerbang timur: selebar mulut ngarai
        Vector3 gE = new Vector3(V2.GateE.x, V2.H(V2.GateE.x, V2.GateE.y), V2.GateE.y);
        var lgObj = BoxCol(tGates, "LevelGate_Timur", gE + Vector3.up * 4f, new Vector3(23f, 8f, 0.9f), new Vector3(0f, 90f, 0f));
        var lg = lgObj.AddComponent<LevelGate>();
        lg.minLevel = StarterConfig.CityMinLevel;
        lg.gateName = "Gerbang Timur";
        lg.blocker = lgObj.GetComponent<BoxCollider>();
        lg.warnRadius = 12f;
        lgObj.transform.SetParent(gateE, true);

        // penjaga: dua per gerbang, di dalam desa, mengapit jalan
        V2Guard("Penjaga Utara", V2.GateN + new Vector2(-5.2f, -3.2f), 0f);
        V2Guard("Penjaga Utara", V2.GateN + new Vector2(5.2f, -3.2f), 0f);
        V2Guard("Penjaga Timur", V2.GateE + new Vector2(-3.2f, 5.2f), 90f);
        V2Guard("Penjaga Timur", V2.GateE + new Vector2(-3.2f, -5.2f), 90f);
    }

    static void V2Guard(string display, Vector2 p, float yaw)
    {
        var go = VNpc("NPC_Penjaga", display, "Penjaga Desa", "Penjaga", new Vector3(p.x, V2.H(p.x, p.y), p.y), true);
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    // =====================================================================
    // Titik muncul + keluar scene
    // =====================================================================
    static void V2Spawns()
    {
        Vector2 sp = V2.Spawn;
        VMark("SpawnStart", new Vector3(sp.x, V2.H(sp.x, sp.y) + 0.05f, sp.y), 0f);
        VMark("RespawnPoint", new Vector3(0f, V2.H(0f, -20f) + 0.05f, -20f), 0f);
        VMark("SpawnFromForest", new Vector3(0f, V2.H(0f, V2.GateN.y + 12f) + 0.05f, V2.GateN.y + 12f), 180f);          // kembali dari Hutan Pemula, menghadap desa
        VMark("SpawnFromTown", new Vector3(V2.GateE.x + 12f, V2.H(V2.GateE.x + 12f, V2.GateE.y) + 0.05f, V2.GateE.y), 270f);   // kembali dari kota, menghadap barat
        Vector3 ee = new Vector3(V2.GateE.x + 32f, V2.H(V2.GateE.x + 32f, V2.GateE.y), V2.GateE.y);
        var x2 = VExit("Exit_KotaPerbatasan", ee, 90f, StarterConfig.CityScene, "SpawnFromVillage", StarterConfig.CityMinLevel);
        x2.deniedText = "Penjaga: \"Jalan ke Ashwatch penuh bandit. Kembalilah setelah Level " + StarterConfig.CityMinLevel + ".\"";
    }
}
