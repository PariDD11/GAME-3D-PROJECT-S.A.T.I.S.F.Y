using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Kota Ashwatch "Konsentris" (n81): tiga tembok kelabu konsentris (luar rendah mengurung ladang, tengah berisi kota atap merah, dalam di bukit mahkota bermenara emas)
/// + tembok pembatas halaman kastil, menara, lengkung sungai, 12 gerbang kanon + Gerbang Kastil, beranimasi dengan semua pengait lama
/// (CityGate selatan/utara/timur/barat di gerbang luar, LevelGate di Gerbang Dalam Utara dan Gerbang Kastil, dua penjaga di tiap gerbang).
/// Geometri tembok: Town2Walls.cs (T2Walls) + Town2Kit.Wall.cs; gerbang: Town2Gate*.cs (T2Gate).
/// </summary>
public static partial class TownSceneBuilder
{
    /// <summary>Slot bahan per tingkat tembok; sel yang kosong (kulit/mantel) diisi dari palet dasar supaya tidak ada sub-mesh tanpa material.</summary>
    static Material[] T2TierSlots(int tier)
    {
        var a = T2Gate.SlotsFor(tier);
        for (int i = 0; i < a.Length; i++) if (a[i] == null) a[i] = baseSlots[i];
        return a;
    }

    static void T2AddWallCols(Transform parent, List<T2Walls.Col> cols)
    {
        int n = 0;
        foreach (var c in cols)
        {
            if (c.capsule)
            {
                var go = new GameObject("ColMenara" + (n++));
                go.transform.SetParent(parent, false);
                go.transform.position = c.c;
                var cc = go.AddComponent<CapsuleCollider>();
                cc.center = Vector3.zero; cc.radius = c.r; cc.height = Mathf.Max(c.h, c.r * 2f + 0.1f);
            }
            else BoxCol(parent, "ColTembok" + (n++), c.c, c.size, new Vector3(0f, c.yaw, 0f));
        }
    }

    static void T2WallsAndGates()
    {
        var arches = T2Walls.ArchesOf(T2G);
        var gWalls = Group("TembokKota", tWalls);
        foreach (var w in T2.Walls)
        {
            var res = new T2Walls.Result();
            if (w.tier == 0) res.arches.AddRange(arches);
            var bag = new GeoBag();
            T2Walls.BuildWall(w, bag, res, T2G, null);
            foreach (var t in res.towers) T2Kit.WallTower(bag, t.x, t.gy, t.z, t.r, t.top - t.gy, t.cone, t.seed);
            var g = Group("Tembok_" + w.id, gWalls);
            bag.Emit(g, "Tembok", T2TierSlots(w.tier), MeshDir + "/T2_Wall_" + w.id + ".asset");
            T2AddWallCols(Group("Collider", g), res.cols);
            // jalan peronda: lampu obor di menara (hanya beberapa agar ringan)
            int li = 0;
            foreach (var t in res.towers)
                if ((li++ % 3) == 0) AddLight(g, new Vector3(t.x, t.top - 1.5f, t.z), 18f, 1.2f);
            foreach (var t in res.towers) solidBoxes.Add(new[] { t.x, t.z, t.r * 2f + 2f, t.r * 2f + 2f, 0f });
        }

        // lengkung sungai di Tembok Luar (air mengalir di bawahnya, terali besi menahan perenang)
        int ai = 0;
        var gArch = Group("LengkungSungai", tWalls);
        foreach (var a in arches)
        {
            var b = new GeoBag();
            b.Push(new Vector3(a.p.x, a.yA, a.p.y), a.yaw);
            T2Walls.ArchGeometry(b, a);
            b.Pop();
            var g = Group("Lengkung_" + (++ai), gArch);
            b.Emit(g, "Lengkung", T2TierSlots(0), MeshDir + "/T2_WaterArch_" + ai + ".asset");
            var cols = new List<T2Walls.Col>();
            T2Walls.ArchColliders(a, cols);
            T2AddWallCols(Group("Collider", g), cols);
        }

        foreach (var gt in T2.Gates) T2Gate1(gt);
    }

    static void T2Gate1(T2.Gate gt)
    {
        var sp = T2Gate.SpecFor(gt.tier);
        float gy = T2G(gt.p.x, gt.p.y);
        var res = T2Gate.Build(tGates, gt.id, gt.tier, gt.signOut, gt.signIn, new Vector3(gt.p.x, gy, gt.p.y), gt.az, true, true);
        float az = gt.az * Mathf.Deg2Rad;
        Vector2 outv = new Vector2(Mathf.Sin(az), Mathf.Cos(az));
        Vector2 cw = new Vector2(Mathf.Cos(az), -Mathf.Sin(az));

        string dispName = null; string prompt = null; string hunt = null; bool door = false;
        switch (gt.hook)
        {
            case "main": res.gate.isMain = true; dispName = "Gerbang Luar Selatan"; mainGate = res.gate; prompt = "Gerbang Selatan"; break;
            case "north": dispName = "Gerbang Luar Utara"; prompt = "Gerbang Utara"; hunt = CityGate.DungeonCityScene; door = true; break;
            case "east": dispName = "Gerbang Luar Timur"; prompt = "Gerbang Timur"; hunt = "HuntArea_Desert"; door = true; break;
            case "west": dispName = "Gerbang Luar Barat"; prompt = "Gerbang Barat"; hunt = "HuntArea_Swamp"; door = true; break;
        }
        if (dispName != null) res.gate.displayName = dispName;
        if (prompt != null)
        {
            // titik interaksi Gerbang Kota (peta, perbekalan, berangkat) di sisi dalam gerbang
            var ip = new GameObject("InteractPoint").transform;
            ip.SetParent(res.root.transform, false);
            ip.localPosition = new Vector3(0f, 0f, -(sp.D / 2f + 9f));
            var cg = ip.gameObject.AddComponent<CityGate>();
            cg.prompt = prompt;
            cg.radius = 5f;
            if (hunt != null) cg.huntSceneName = hunt;
            if (door) cg.gateDoor = res.gate;
            Prim(PrimitiveType.Cylinder, "Marker", ip, new Vector3(0f, 0.16f, 0f), new Vector3(3.5f, 0.03f, 3.5f), mMarker, false);
        }
        if (gt.hook == "level") T2AddLevelGate(res, sp, gt);

        // dua penjaga: gerbang luar di sisi dalam (jembatan parit ada di sisi luar); gerbang tengah, dalam & kastil di sisi datang (sisi dalamnya ditempati sepasang patung batu)
        float side = gt.tier == 0 ? -1f : 1f;
        Vector2 gp = gt.p + outv * (side * (sp.D / 2f + 3f));
        float face = gt.az;                                   // menghadap searah +Z gerbang: penjaga luar menatap keluar lewat gerbang, penjaga dalam/kastil menatap pengunjung dari selatan
        string role = gt.tier == 2 ? "Ksatria" : "Penjaga";
        string nm = "NPC_" + role + "_" + gt.id.Replace("Gerbang_", "");
        foreach (float sg in new[] { -1f, 1f })
        {
            Vector2 q = gp + cw * (sg * (sp.W / 2f + 1.7f));
            MakeNpc(nm, role, new Vector3(q.x, T2G(q.x, q.y), q.y), null, gt.tier == 2, face);
        }
        ReserveBox(gt.p.x, gt.p.y, sp.tX * 2f + sp.towerR * 2f, sp.D + 4f, gt.az, 1f);
        if (gt.tier >= 1)
            foreach (float sg in new[] { -1f, 1f })
            {
                Vector2 q = gt.p + outv * (-(sp.D * 0.5f + 2.6f)) + cw * (sg * (sp.W * 0.5f + 3.0f));       // patung batu di sisi dalam (lihat T2Gate.Model)
                Reserve(q.x, q.y, 2.6f);
            }
    }

    /// <summary>Pemeriksaan level di Gerbang Kastil: penghalang tak terlihat + papan syarat berdiri di sisi kota bawah.</summary>
    static void T2AddLevelGate(GateKit.Result res, T2Gate.Spec sp, T2.Gate gt)
    {
        var go = new GameObject("LevelGate");
        go.transform.SetParent(res.root.transform, false);
        var bc = go.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, (sp.Hd + 2f) / 2f, 0f);
        bc.size = new Vector3(sp.W + 1.2f, sp.Hd + 2f, 0.8f);
        var lg = go.AddComponent<LevelGate>();
        lg.blocker = bc;
        lg.gateName = "Gerbang Kastil";

        // papan syarat dipasang pada muka pilar gerbang (sisi luar, menghadap jembatan parit) — tidak berdiri di tepi parit
        var board = new GameObject("PapanSyaratLevel").transform;
        board.SetParent(res.root.transform, false);
        board.localPosition = new Vector3(-(sp.W / 2f + 1.95f), 0f, sp.D / 2f + 0.12f);
        board.localRotation = Quaternion.identity;
        Prim(PrimitiveType.Cube, "Papan", board, new Vector3(0f, 3.3f, 0.06f), new Vector3(3.2f, 1.25f, 0.14f), UMat("T2_PapanGelap", new Color(0.14f, 0.1f, 0.07f)), false);
        Prim(PrimitiveType.Cube, "Bingkai", board, new Vector3(0f, 3.3f, 0.03f), new Vector3(3.36f, 1.4f, 0.08f), TownMats.Wood, false);
        Prim(PrimitiveType.Cube, "Gantung", board, new Vector3(0f, 4.1f, 0.0f), new Vector3(2.6f, 0.08f, 0.08f), TownMats.Iron, false);
        BuildKit.Text(board, "HANYA HUNTER LEVEL " + GateRules.InnerGateMinLevel + "+", new Vector3(0f, 3.55f, 0.16f), 180f, 0.062f, new Color(1f, 0.82f, 0.35f));
        BuildKit.Text(board, "Kediaman Penguasa Ashwatch", new Vector3(0f, 3.1f, 0.16f), 180f, 0.037f, new Color(0.95f, 0.9f, 0.78f));
    }
}
