using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HunterGuild;

public static partial class TownSceneBuilder
{
    static Vector2 V(float x, float z) { return new Vector2(x, z); }

    // =====================================================================
    // Tanah
    // =====================================================================
    static void BuildGround()
    {
        float h = 720f;
        var b = new GeoBag();
        b.Patch(Sl.Grass, V(-h, -h), V(-h, h), V(h, h), V(h, -h), 0f, V(1f, 0f));
        b.Emit(tEnv, "Ground", baseSlots);
        var col = new GameObject("GroundCollider");
        col.transform.SetParent(tEnv, false);
        col.transform.position = new Vector3(0f, -0.5f, 0f);
        col.AddComponent<BoxCollider>().size = new Vector3(h * 2f, 1f, h * 2f);
    }

    // =====================================================================
    // 3 tembok + 12 gerbang
    // =====================================================================
    static AnimatedGate mainGate;

    static readonly string[] TierUpper = { "LUAR", "DALAM", "TENGAH" };
    static readonly string[] TierInside = { "SEKTOR PERTANIAN", "DISTRIK PERDAGANGAN", "KEDIAMAN BANGSAWAN" };

    static void BuildWallsAndGates()
    {
        for (int tier = 0; tier < 3; tier++)
        {
            WallKit.BuildRing(tWalls, tier, GateKit.SlotsFor(tier));
            var spec = GateKit.SpecFor(tier);
            for (int k = 0; k < 4; k++)
            {
                float az = WallKit.GateAz[k];
                Vector2 p = WallKit.OnRing(tier, az);
                string dir = WallKit.DirName[k];
                var res = GateKit.Build(tGates, "Gerbang_" + GateKit.TierName[tier] + "_" + dir, tier,
                    "GERBANG " + TierUpper[tier] + " " + dir.ToUpper(), TierInside[tier], new Vector3(p.x, 0f, p.y), az, true, true);     // semua gerbang beranimasi dan selalu tertutup sampai penjaga memberi izin

                if (tier == 2) AddLevelGate(res, spec);

                if (tier == 0 && k == 2)
                {
                    res.gate.isMain = true;
                    res.gate.displayName = "Gerbang Luar Selatan";
                    mainGate = res.gate;
                    // titik interaksi Gerbang Kota (peta, perbekalan, berangkat) di sisi dalam gerbang
                    var ip = new GameObject("InteractPoint").transform;
                    ip.SetParent(res.root.transform, false);
                    ip.localPosition = new Vector3(0f, 0f, -(spec.D / 2f + 9f));
                    var gate = ip.gameObject.AddComponent<CityGate>();
                    gate.prompt = "Gerbang Selatan";
                    gate.radius = 5f;
                    Prim(PrimitiveType.Cylinder, "Marker", ip, new Vector3(0f, 0.16f, 0f), new Vector3(3.5f, 0.03f, 3.5f), mMarker, false);
                }

                if (tier == 0 && k == 0)
                {
                    // Gerbang Luar Utara: jalan raya ke Kota Eldrath
                    res.gate.displayName = "Gerbang Luar Utara";
                    var ipN = new GameObject("InteractPoint").transform;
                    ipN.SetParent(res.root.transform, false);
                    ipN.localPosition = new Vector3(0f, 0f, -(spec.D / 2f + 9f));
                    var gateN = ipN.gameObject.AddComponent<CityGate>();
                    gateN.prompt = "Gerbang Utara";
                    gateN.radius = 5f;
                    gateN.huntSceneName = CityGate.DungeonCityScene;
                    gateN.gateDoor = res.gate;
                    Prim(PrimitiveType.Cylinder, "Marker", ipN, new Vector3(0f, 0.16f, 0f), new Vector3(3.5f, 0.03f, 3.5f), mMarker, false);
                }

                if (tier == 0 && k == 1)
                {
                    // Gerbang Luar Timur: jalan ke Gurun Zahar
                    res.gate.displayName = "Gerbang Luar Timur";
                    var ipE = new GameObject("InteractPoint").transform;
                    ipE.SetParent(res.root.transform, false);
                    ipE.localPosition = new Vector3(0f, 0f, -(spec.D / 2f + 9f));
                    var gateE = ipE.gameObject.AddComponent<CityGate>();
                    gateE.prompt = "Gerbang Timur";
                    gateE.radius = 5f;
                    gateE.huntSceneName = "HuntArea_Desert";
                    gateE.gateDoor = res.gate;
                    Prim(PrimitiveType.Cylinder, "Marker", ipE, new Vector3(0f, 0.16f, 0f), new Vector3(3.5f, 0.03f, 3.5f), mMarker, false);
                }

                if (tier == 0 && k == 3)
                {
                    // Gerbang Luar Barat: jalan ke Rawa Terkutuk
                    res.gate.displayName = "Gerbang Luar Barat";
                    var ipW = new GameObject("InteractPoint").transform;
                    ipW.SetParent(res.root.transform, false);
                    ipW.localPosition = new Vector3(0f, 0f, -(spec.D / 2f + 9f));
                    var gateW = ipW.gameObject.AddComponent<CityGate>();
                    gateW.prompt = "Gerbang Barat";
                    gateW.radius = 5f;
                    gateW.huntSceneName = "HuntArea_Swamp";
                    gateW.gateDoor = res.gate;
                    Prim(PrimitiveType.Cylinder, "Marker", ipW, new Vector3(0f, 0.16f, 0f), new Vector3(3.5f, 0.03f, 3.5f), mMarker, false);
                }

                // dua penjaga di sisi dalam tiap gerbang
                Vector2 outv = WallKit.Out(az);
                Vector2 cw = new Vector2(Mathf.Cos(az * Mathf.Deg2Rad), -Mathf.Sin(az * Mathf.Deg2Rad));
                Vector2 gp = p - outv * (spec.D / 2f + 3f);
                string role = tier == 2 ? "Ksatria" : "Penjaga";
                MakeNpc("NPC_" + role + "_" + GateKit.TierName[tier] + "_" + dir, role, V3(gp + cw * (spec.W / 2f + 1.6f)), null, tier == 2);
                MakeNpc("NPC_" + role + "_" + GateKit.TierName[tier] + "_" + dir, role, V3(gp - cw * (spec.W / 2f + 1.6f)), null, tier == 2);
            }
        }
    }

    /// <summary>Tembok, gerbang, kastil, dan plaza dicadangkan sebelum rumah dan jalan ditata.</summary>
    static void ReserveStructural()
    {
        for (int tier = 0; tier < 3; tier++)
        {
            float r = WallKit.R[tier], th = WallKit.Thick[tier];
            int n = Mathf.RoundToInt(2f * Mathf.PI * r / 7f);
            for (int i = 0; i < n; i++)
            {
                Vector2 p = WallKit.OnRing(tier, i * 360f / n);
                Reserve(p.x, p.y, th / 2f + 3.2f);
            }
            var sp = GateKit.SpecFor(tier);
            for (int k = 0; k < 4; k++)
            {
                float az = WallKit.GateAz[k];
                Vector2 p = WallKit.OnRing(tier, az);
                ReserveBox(p.x, p.y, sp.tX * 2f + sp.towerR * 2f, sp.D + 4f, az, 1f);
            }
        }
        Reserve(0f, 0f, 58f);       // kastil dan terasnya
    }

    // =====================================================================
    // Jalan
    // =====================================================================
    class Street
    {
        public string name;
        public List<Vector2> pts = new List<Vector2>();
        public float w;
        public int mat;            // 0 cobble, 1 tanah, 2 paving
        public float y;
        public bool houses;
        public int sides;          // 0 dua sisi, -1 hanya sisi dalam (menghadap luar)
        public float rmin = 0f, rmax = 9999f;
    }

    static readonly List<Street> streets = new List<Street>();
    static GeoBag roadC, roadD, roadP;

    static List<Vector2> Line(Vector2 a, Vector2 b, float step)
    {
        var l = new List<Vector2>();
        int n = Mathf.Max(1, Mathf.RoundToInt((b - a).magnitude / step));
        for (int i = 0; i <= n; i++) l.Add(Vector2.Lerp(a, b, i / (float)n));
        return l;
    }

    static Street NewStreet(string name, float w, int mat, float y, bool houses, int sides)
    {
        var s = new Street { name = name, w = w, mat = mat, y = y, houses = houses, sides = sides };
        streets.Add(s);
        return s;
    }

    static GeoBag BagFor(int mat) { return mat == 0 ? roadC : (mat == 1 ? roadD : roadP); }

    static void Axis(float az, float r0, float r1, float w, int mat, string name, bool houses = false)
    {
        var s = NewStreet(name, w, mat, 0.10f, houses, 0);
        s.pts = Line(WallKit.Out(az) * r0, WallKit.Out(az) * r1, 4f);
    }

    /// <summary>Jalan lingkar. Titik yang bertabrakan dengan bangunan dilewati, sehingga jalan terputus rapi di depan bangunan.</summary>
    static void Ring(string name, float R, float w, int mat, float y, bool houses, int sides, float axisSkipDeg, float aFrom, float aTo)
    {
        Street cur = null;
        float step = 2.5f / R * Mathf.Rad2Deg;
        int idx = 0;
        for (float a = aFrom; a <= aTo + 0.001f; a += step)
        {
            Vector2 p = WallKit.Out(a) * R;
            bool skip = false;
            if (axisSkipDeg > 0f)
            {
                for (int k = 0; k < 4; k++)
                    if (Mathf.Abs(Mathf.DeltaAngle(a, WallKit.GateAz[k])) < axisSkipDeg) skip = true;
            }
            if (!skip && !IsFree(p.x, p.y, w / 2f)) skip = true;
            if (skip) { cur = null; continue; }
            if (cur == null)
            {
                cur = NewStreet(name + "_" + (idx++), w, mat, y, houses, sides);
            }
            cur.pts.Add(p);
        }
    }

    static void Radial(string name, float az, float r0, float r1, float w, int mat, float y, bool houses)
    {
        Street cur = null;
        int idx = 0;
        Vector2 a = WallKit.Out(az) * r0, b = WallKit.Out(az) * r1;
        int n = Mathf.Max(1, Mathf.RoundToInt((b - a).magnitude / 2.5f));
        for (int i = 0; i <= n; i++)
        {
            Vector2 p = Vector2.Lerp(a, b, i / (float)n);
            if (!IsFree(p.x, p.y, w / 2f)) { cur = null; continue; }
            if (cur == null) cur = NewStreet(name + "_" + (idx++), w, mat, y, houses, 0);
            cur.pts.Add(p);
        }
    }

    static void BuildRoads()
    {
        roadC = new GeoBag(); roadD = new GeoBag(); roadP = new GeoBag();
        var gateSpec = new[] { GateKit.SpecFor(0), GateKit.SpecFor(1), GateKit.SpecFor(2) };
        float hd0 = gateSpec[0].D / 2f, hd1 = gateSpec[1].D / 2f, hd2 = gateSpec[2].D / 2f;

        // jalan sumbu: gerbang luar -> gerbang dalam -> gerbang tengah -> kastil
        for (int k = 0; k < 4; k++)
        {
            float az = WallKit.GateAz[k];
            string dn = WallKit.DirName[k];
            float outEnd = (k == 2) ? 412f : 400f;
            Axis(az, WallKit.R[0] + hd0, outEnd, 14f, 1, "JalanLuar_" + dn);
            Axis(az, WallKit.R[1] + hd1, WallKit.R[0] - hd0, 12f, 1, "JalanTani_" + dn);
            Axis(az, WallKit.R[2] + hd2, WallKit.R[1] - hd1, 16f, 0, "JalanNiaga_" + dn, true);
            Axis(az, 40f, WallKit.R[2] - hd2, 16f, 2, "JalanBangsawan_" + dn);
        }

        // lingkar ekonomi
        Ring("Lingkar122", 122f, 8f, 0, 0.08f, true, 0, 0f, 0f, 360f);
        Ring("Lingkar159", 159f, 7f, 0, 0.08f, true, 0, 27f, 0f, 360f);
        Ring("Lingkar196", 196f, 8f, 0, 0.08f, true, -1, 0f, 0f, 360f);
        // gang penghubung (di antara sumbu)
        for (int k = 0; k < 8; k++)
            Radial("Gang_" + k, 22.5f + k * 45f, 126f, 192f, 5f, 0, 0.06f, true);

        // lingkar bangsawan
        Ring("Promenade", 66f, 9f, 2, 0.08f, false, 0, 0f, 0f, 360f);
        // lingkar pertanian + jalan petak diagonal
        Ring("JalanLadang", 272f, 6f, 1, 0.08f, false, 0, 0f, 0f, 360f);
        for (int k = 0; k < 4; k++)
            Radial("JalanPetak_" + k, 45f + k * 90f, 226f, 322f, 5f, 1, 0.06f, false);

        // gambar semua jalan
        foreach (var s in streets)
        {
            if (s.pts.Count < 2) continue;
            var bag = BagFor(s.mat);
            // tepi paving sedikit lebih lebar di bawah permukaan jalan
            roadP.Ribbon(Sl.Pave, s.pts, s.w + 0.9f, s.y - 0.025f);
            bag.Ribbon(s.mat == 0 ? Sl.Cobble : (s.mat == 1 ? Sl.Dirt : Sl.Pave), s.pts, s.w, s.y);
        }
        roadC.Emit(tRoads, "Roads_Cobble", baseSlots);
        roadD.Emit(tRoads, "Roads_Dirt", baseSlots);
        roadP.Emit(tRoads, "Roads_Pave", baseSlots);

        // gerbang: lantai paving sudah ada di model gerbang; cadangkan jalur jalan agar rumah tidak menutup
        foreach (var s in streets)
            foreach (var p in s.pts) Reserve(p.x, p.y, s.w / 2f + 0.4f);
        for (int i = 0; i < 4; i++) Reserve(PlazaCenter(i).x, PlazaCenter(i).y, PlazaR + 1f);
    }

    // =====================================================================
    // Alun-alun (cakram paving + hiasan pusat)
    // =====================================================================
    static void BuildPlazaSurfaces()
    {
        var b = new GeoBag();
        for (int i = 0; i < 4; i++)
        {
            Vector2 c = PlazaCenter(i);
            b.Disc(Sl.Pave, V3(c), PlazaR + 1.2f, 40, 0.11f);
            b.Disc(Sl.Cobble, V3(c), PlazaR, 40, 0.14f);
            b.Disc(Sl.Pave, V3(c), 21f, 36, 0.17f);
            b.Annulus(Sl.Cobble, V3(c), 21f, 23f, 0f, 360f, 36, 0.18f);
            // bintang jalan di tengah
            b.Disc(Sl.Cobble, V3(c), 9.5f, 24, 0.19f);
        }
        b.Emit(tRoads, "Plazas", baseSlots);

        for (int i = 0; i < 4; i++)
        {
            Vector2 c = PlazaCenter(i);
            var root = Group(PlazaName[i].Replace(" ", "_"), tProps);
            switch (i)
            {
                case 0: PlazaGuild(root, c); break;
                case 1: PlazaMarket(root, c); break;
                case 2: PlazaAcademy(root, c); break;
                default: PlazaCathedral(root, c); break;
            }
            // tiang bendera dan lampu di tepi
            for (int k = 0; k < 6; k++)
            {
                float a = (k * 60f + 30f) * Mathf.Deg2Rad;
                Vector2 p = c + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * (PlazaR - 3f);
                PropKit.Place(PropKit.LampProto(), root, "Lamp", V3(p, 0.17f), (k * 60f + 30f) + 90f, 1f, baseSlots);
            }
        }
    }

    static void PlazaGuild(Transform root, Vector2 c)
    {
        var fountain = PropKit.Get("FountainL", bag => PropKit.Fountain(bag, Vector3.zero, 1.0f));
        PropKit.Place(fountain, root, "Fountain", V3(c, 0.19f), 0f, 1f, baseSlots);
        var col = new GameObject("FountainCollider");
        col.transform.SetParent(root, false);
        col.transform.position = V3(c, 0.5f);
        col.AddComponent<CapsuleCollider>().radius = 8.4f;
        Reserve(c.x, c.y, 9f);
        // papan pengumuman
        var board = new GameObject("NoticeBoard").transform;
        board.SetParent(tBuildings, false);
        Vector2 bp = c + V(10f, -14f);
        board.position = V3(bp);
        board.rotation = Quaternion.Euler(0f, YawToward(bp, c), 0f);
        var bag2 = new GeoBag();
        bag2.Box(Sl.Wood, new Vector3(-1.4f, 1.5f, 0f), new Vector3(0.3f, 3f, 0.3f));
        bag2.Box(Sl.Wood, new Vector3(1.4f, 1.5f, 0f), new Vector3(0.3f, 3f, 0.3f));
        bag2.Box(Sl.WoodLight, new Vector3(0f, 2.3f, 0f), new Vector3(3.8f, 1.9f, 0.22f));
        bag2.Box(Sl.PlasterB, new Vector3(-0.9f, 2.3f, 0.14f), new Vector3(0.9f, 1.1f, 0.04f));
        bag2.Box(Sl.PlasterB, new Vector3(0.8f, 2.4f, 0.14f), new Vector3(0.9f, 1.0f, 0.04f));
        bag2.Gable(Sl.Roof, Sl.Wood, Sl.Wood, new Vector3(0f, 3.25f, 0f), 4.4f, 1.2f, 0.7f, 0.2f, true);
        var nm = new GameObject("Model").transform;
        nm.SetParent(board, false);
        bag2.Emit(nm, "Mesh", baseSlots);
        var ip = new GameObject("InteractPoint").transform;
        ip.SetParent(board, false);
        ip.localPosition = new Vector3(0f, 0f, 2.4f);
        var info = ip.gameObject.AddComponent<InfoPoint>();
        info.prompt = "Papan Pengumuman";
        info.title = "Papan Pengumuman";
        info.message = "Selamat datang di kota benteng berlapis tiga!\n\nTembok Luar: sektor pertanian.\nTembok Dalam: kota dan perekonomian (Guild, pasar, akademi, katedral).\nTembok Tengah: kediaman bangsawan dan Kastil Penguasa.\n\nHunter baru melapor ke Guild Petualang di sisi timur alun-alun ini.";
        Prim(PrimitiveType.Cylinder, "Marker", ip, new Vector3(0f, 0.2f, 0f), new Vector3(2f, 0.03f, 2f), mMarker, false);
        Reserve(bp.x, bp.y, 3f);
        for (int k = 0; k < 4; k++)
        {
            float a = (k * 90f + 45f) * Mathf.Deg2Rad;
            Vector2 p = c + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * 12.5f;
            PropKit.Place(PropKit.Get("Bench", PropKit.Bench), root, "Bench", V3(p, 0.19f), YawToward(p, c), 1f, baseSlots);
        }
    }

    static void PlazaMarket(Transform root, Vector2 c)
    {
        var well = PropKit.Get("WellL", bag => PropKit.Well(bag, Vector3.zero));
        PropKit.Place(well, root, "Well", V3(c, 0.19f), 0f, 1f, baseSlots);
        var col = new GameObject("WellCollider");
        col.transform.SetParent(root, false);
        col.transform.position = V3(c, 0.6f);
        col.AddComponent<CapsuleCollider>().radius = 1.8f;
        // lapak: dua lingkaran
        for (int ring = 0; ring < 2; ring++)
        {
            int n = ring == 0 ? 6 : 10;
            float rad = ring == 0 ? 10f : 24f;
            for (int k = 0; k < n; k++)
            {
                float a = (k + ring * 0.5f) / n * Mathf.PI * 2f;
                Vector2 p = c + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * rad;
                int ci = (k + ring) % 6;
                var proto = PropKit.Get("Stall" + ci, bag => PropKit.Stall(bag, Vector3.zero, 0f, ci % 2 == 0 ? Sl.Cloth : Sl.ClothB, Sl.Plaster));
                var go = PropKit.Place(proto, root, "Stall", V3(p, 0.19f), YawToward(p, c) + 180f, 1f, StallSlots(ci));
                go.AddComponent<BoxCollider>().center = new Vector3(0f, 1.4f, 0f);
                go.GetComponent<BoxCollider>().size = new Vector3(3.6f, 2.8f, 2.0f);
            }
        }
    }

    static Material[] StallSlots(int i)
    {
        var s = TownMats.Slots();
        s[Sl.Cloth] = TownMats.Cloths[i % 6];
        s[Sl.ClothB] = TownMats.Cloths[(i + 2) % 6];
        return s;
    }

    static void PlazaAcademy(Transform root, Vector2 c)
    {
        var gz = PropKit.Get("GazeboL", LandmarkKit.Gazebo);
        PropKit.Place(gz, root, "Gazebo", V3(c, 0.19f), 0f, 1f, baseSlots);
        for (int k = 0; k < 8; k++)
        {
            float a = (k / 8f) * Mathf.PI * 2f;
            Vector2 p = c + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * 17f;
            PropKit.Place(PropKit.TreeProto(2), root, "Sakura", V3(p, 0.19f), a * 57f, 0.85f, baseSlots);
            Reserve(p.x, p.y, 2f);
            var tc = new GameObject("TreeCol");
            tc.transform.SetParent(root, false);
            tc.transform.position = V3(p, 1.5f);
            tc.AddComponent<CapsuleCollider>().radius = 0.5f;
        }
        for (int k = 0; k < 4; k++)
        {
            float a = (k * 90f + 22.5f) * Mathf.Deg2Rad;
            Vector2 p = c + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * 12f;
            PropKit.Place(PropKit.Get("Bench", PropKit.Bench), root, "Bench", V3(p, 0.19f), YawToward(p, c), 1f, baseSlots);
        }
        var col = new GameObject("GazeboCollider");
        col.transform.SetParent(root, false);
        col.transform.position = V3(c, 1f);
        col.AddComponent<CapsuleCollider>().radius = 3.8f;
    }

    static void PlazaCathedral(Transform root, Vector2 c)
    {
        // Pohon Suci (sakura raksasa) dalam bak batu bundar + patung ksatria + bangku
        var tree = PropKit.TreeProto(2);
        PropKit.Place(tree, root, "PohonSuci", V3(c, 0.5f), 0f, 2.6f, baseSlots);
        var bag = new GeoBag();
        bag.Cyl(Sl.Marble, Vector3.zero, 6.5f, 0.9f, 20, 6.2f, true);
        bag.Cyl(Sl.DarkStone, Vector3.zero, 6.9f, 0.4f, 20, 6.9f, true);
        bag.Cyl(Sl.Grass, new Vector3(0f, 0.85f, 0f), 6.0f, 0.06f, 20, 6.0f, true);
        bag.Emit(root, "Planter", baseSlots).transform.position = V3(c, 0.19f);
        var col = new GameObject("TreeCollider");
        col.transform.SetParent(root, false);
        col.transform.position = V3(c, 1.5f);
        col.AddComponent<CapsuleCollider>().radius = 6.5f;
        for (int k = 0; k < 4; k++)
        {
            float a = (k * 90f + 45f) * Mathf.Deg2Rad;
            Vector2 p = c + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * 11f;
            var sb = PropKit.Get("StatueK", bg => PropKit.KnightStatue(bg, Vector3.zero, 0f));
            PropKit.Place(sb, root, "Statue", V3(p, 0.19f), YawToward(p, c), 0.7f, baseSlots);
            var cc = new GameObject("StatueCol");
            cc.transform.SetParent(root, false);
            cc.transform.position = V3(p, 1.5f);
            cc.AddComponent<CapsuleCollider>().radius = 1.2f;
        }
    }
}

public static partial class TownSceneBuilder
{
    /// <summary>Pasang pemeriksaan level di Gerbang Dalam: penghalang tak terlihat + papan syarat.</summary>
    static void AddLevelGate(GateKit.Result res, GateKit.Spec spec)
    {
        var go = new GameObject("LevelGate");
        go.transform.SetParent(res.root.transform, false);
        var bc = go.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, (spec.Hdoor + 2f) / 2f, 0f);
        bc.size = new Vector3(spec.W + 1.2f, spec.Hdoor + 2f, 0.8f);
        var lg = go.AddComponent<LevelGate>();
        lg.blocker = bc;
        lg.gateName = "Gerbang Dalam";
        BuildKit.Text(res.root.transform, "HANYA HUNTER LEVEL " + GateRules.InnerGateMinLevel + "+", new Vector3(0f, 2.4f, spec.D / 2f + 0.45f), 180f, 0.11f, new Color(1f, 0.82f, 0.35f));
        BuildKit.Text(res.root.transform, "HANYA HUNTER LEVEL " + GateRules.InnerGateMinLevel + "+", new Vector3(0f, 2.4f, -spec.D / 2f - 0.45f), 0f, 0.11f, new Color(1f, 0.82f, 0.35f));
    }
}
