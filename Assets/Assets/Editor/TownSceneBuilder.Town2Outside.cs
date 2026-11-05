using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Kota Ashwatch "Benteng": dunia di luar tembok (hutan perbukitan, batu di lereng) dan empat koridor keluar kota
/// (selatan = Hutan Lerai + cabang Desa Pemula, timur = Gurun Zahar, barat = Rawa Terkutuk, utara = jalan raya ke Kota Eldrath).
/// Nama zona transisi dan titik muncul SAMA dengan denah lama (ToForest, ToStartVillage, ToDesert, ToSwamp, ToDungeonCity,
/// SpawnFromForest, SpawnFromVillage, SpawnFromDesert, SpawnFromSwamp, SpawnFromDungeon), sehingga scene hutan/gurun/rawa/Eldrath tidak perlu diubah.
/// </summary>
public static partial class TownSceneBuilder
{
    // =====================================================================
    // Dunia di luar tembok
    // =====================================================================
    /// <summary>Titik di luar Tembok Luar (&gt;= wallGap), jauh dari lajur koridor (&gt;= corGap), bukan sungai.</summary>
    static T2.Road t2RoadTani, t2RoadKebun;

    static bool T2OutsideOk(float x, float z, float wallGap, float corGap)
    {
        if (T2.SignedDist(x, z, T2.OuterPoly) < wallGap) return false;
        for (int k = 0; k < T2.OutRoads.Count; k++) if (T2.DistPoly(x, z, T2.OutRoads[k]) < corGap) return false;
        if (T2.RiverBank(x, z) > 0.02f) return false;
        if (T2G(x, z) <= T2.WaterLevel + 0.7f) return false;
        // lahan pertanian di luar tembok: bangunan tani, petak ladang, kebun apel, jalan tani/kebun tetap bersih dari hutan acak
        if (T2.InBuildingLot(x, z, 6f)) return false;
        if (t2RoadTani == null) { t2RoadTani = T2.RoadOf("tani"); t2RoadKebun = T2.RoadOf("kebun"); }
        if (t2RoadTani != null && T2.DistPoly(x, z, t2RoadTani.pts) < t2RoadTani.width * 0.5f + 5f) return false;
        if (t2RoadKebun != null && T2.DistPoly(x, z, t2RoadKebun.pts) < t2RoadKebun.width * 0.5f + 5f) return false;
        for (int i = 0; i < T2.Lots.Count; i++)
        {
            var l = T2.Lots[i];
            if (l.kind != "field") continue;
            if (T2.DistToRect(x, z, l) < 4f) return false;
        }
        for (int i = 0; i < T2.Zones.Count; i++)
        {
            var zn = T2.Zones[i];
            if ((zn.kind == "field" || zn.id == "kebun_apel") && (T2.Inside(x, z, zn.poly) || T2.SignedDist(x, z, zn.poly) < 4f)) return false;
        }
        return true;
    }

    static void T2OutsideWorld()
    {
        t2RoadTani = null; t2RoadKebun = null;
        var g = Group("DuniaLuar", tEnv);
        var rng = new System.Random(8080);
        var occ = new HashSet<long>();
        int trees = 0, tries = 0;
        while (tries < 90000 && trees < 3200)
        {
            tries++;
            float x = (float)(rng.NextDouble() * 2.0 - 1.0) * (T2Half - 26f), z = (float)(rng.NextDouble() * 2.0 - 1.0) * (T2Half - 26f);
            if (!T2OutsideOk(x, z, T2.DitchFarEdge + 12f, 27f)) continue;           // glacis terbuka di depan tembok + parit
            float so = T2.SignedDist(x, z, T2.OuterPoly);
            float y = T2G(x, z);
            if (y > 34f || VSlopeDeg(x, z) > 30f) continue;                                   // lereng terjal & puncak: batu gundul
            float dens = T2.Nz(x, z, 0.02f, 311);
            float p = T2.Sm((dens - 0.34f) / 0.3f) * (0.3f + 0.7f * T2.Sm((so - 36f) / 90f)) * (1f - T2.Sm((y - 22f) / 12f));
            if (rng.NextDouble() > p) continue;
            long key = (long)(Mathf.FloorToInt(x / 5f) + 400) * 1000L + (Mathf.FloorToInt(z / 5f) + 400);
            if (!occ.Add(key)) continue;
            VPlaceTree(g, x, z, 1f + 0.3f * T2.Sm((so - 30f) / 220f));
            trees++;
        }
        VScatterBoulders(g, 240, T2Half - 30f, (bx, bz) => T2OutsideOk(bx, bz, T2.DitchFarEdge + 10f, 24f));
        Debug.Log("[Kota T2] Hutan luar tembok: " + trees + " pohon");
    }

    // =====================================================================
    // Alat koridor
    // =====================================================================
    /// <summary>Titik koridor: asal gerbang g + arah keluar o * s + lateral (searah jarum jam dari o) * t.</summary>
    static Vector2 T2Cp(Vector2 g, Vector2 o, float s, float t)
    {
        Vector2 cw = new Vector2(o.y, -o.x);
        return g + o * s + cw * t;
    }

    /// <summary>Dinding tak terlihat dari a ke b, tingginya mengikuti medan (tidak bisa dilompati).</summary>
    static void T2WallLine(Vector2 a, Vector2 b)
    {
        Vector2 m = (a + b) * 0.5f;
        float len = (b - a).magnitude;
        if (len < 0.5f) return;
        float gy = Mathf.Max(T2G(a.x, a.y), Mathf.Max(T2G(b.x, b.y), T2G(m.x, m.y)));
        InvisibleWallY(new Vector3(m.x, gy + 6f, m.y), new Vector3(1f, 20f, len + 1f), Mathf.Atan2(b.x - a.x, b.y - a.y) * Mathf.Rad2Deg);
    }

    /// <summary>Dua dinding sisi pada t = +-half dari s0 sampai s1, penutup di ujung s1; sisi gapSide (+1/-1) punya celah gapA..gapB (cabang jalan).</summary>
    static void T2CorridorWalls(Vector2 g, Vector2 o, float s0, float s1, float half, float gapSide, float gapA, float gapB)
    {
        foreach (float side in new[] { -1f, 1f })
        {
            if (gapSide != 0f && side == gapSide)
            {
                T2WallLine(T2Cp(g, o, s0, side * half), T2Cp(g, o, gapA, side * half));
                T2WallLine(T2Cp(g, o, gapB, side * half), T2Cp(g, o, s1, side * half));
            }
            else T2WallLine(T2Cp(g, o, s0, side * half), T2Cp(g, o, s1, side * half));
        }
        T2WallLine(T2Cp(g, o, s1, -half - 1f), T2Cp(g, o, s1, half + 1f));
    }

    static AreaTransition T2Transition(string name, Vector2 pos, Vector3 size)
    {
        var t = new GameObject(name).transform;
        t.SetParent(tTown, false);
        t.position = T2P(pos.x, pos.y);
        var at = t.gameObject.AddComponent<AreaTransition>();
        at.mode = AreaTransition.Mode.ToForest;
        at.size = size;
        return at;
    }

    static Transform T2Spawn(string name, Vector2 pos, float yaw)
    {
        var s = new GameObject(name).transform;
        s.SetParent(tTown, false);
        s.position = T2P(pos.x, pos.y);
        s.rotation = Quaternion.Euler(0f, yaw, 0f);
        return s;
    }

    /// <summary>Papan penunjuk jalan di atas tanah. yaw: terbaca dari sisi +Z lokal (pejalan yang keluar kota melihat dari belakang kota).</summary>
    static void T2RoadSign(string name, string text, string sub, Vector2 p, float yaw)
    {
        var s = new GameObject(name).transform;
        s.SetParent(tProps, false);
        s.position = T2P(p.x, p.y);
        s.rotation = Quaternion.Euler(0f, yaw, 0f);
        T2PostSign(s, text, sub, Vector3.zero);
        float cs = text.Length > 20 ? 0.05f : (text.Length > 12 ? 0.058f : 0.068f);
        float w = Mathf.Max(2.6f, text.Length * 3.84f * cs + 0.9f);
        foreach (float sx in new[] { -1f, 1f }) BoxCol(s, "TiangCol", new Vector3(sx * (w * 0.5f - 0.22f), 1.4f, 0f), new Vector3(0.3f, 2.8f, 0.3f), Vector3.zero);
        Reserve(p.x, p.y, 2.2f);
    }

    /// <summary>Batu penanda jarak: tapak, tiang, tutup piramida.</summary>
    static void T2Milestone(GeoBag b)
    {
        b.Box(Sl.Stone, new Vector3(0f, 0.17f, 0f), new Vector3(1.0f, 0.34f, 1.0f));
        b.Box(Sl.Stone, new Vector3(0f, 0.34f + 0.5f, 0f), new Vector3(0.66f, 1.0f, 0.66f));
        b.Push(new Vector3(0f, 1.34f, 0f), 45f);
        b.Cone(Sl.DarkStone, Vector3.zero, 0.5f, 0.36f, 4);
        b.Pop();
        b.Box(Sl.DarkStone, new Vector3(0f, 0.95f, 0.34f), new Vector3(0.36f, 0.16f, 0.03f));       // ukiran
    }

    static void T2PlaceMilestone(Transform g, Vector2 p, float yaw)
    {
        var go = PropKit.Place(PropKit.Get("T2_Milestone", T2Milestone), g, "BatuJarak", T2P(p.x, p.y, -0.02f), yaw, 1f, baseSlots);
        BoxCol(go.transform, "BatuCol", new Vector3(0f, 0.8f, 0f), new Vector3(0.9f, 1.6f, 0.9f), Vector3.zero);
    }

    static float T2Rnd(System.Random r, float a, float b) { return a + (float)r.NextDouble() * (b - a); }

    /// <summary>Pohon hutan (spesies sesuai ketinggian) + semak bawah di sepanjang koridor lurus.</summary>
    static void T2ForestRows(Transform g, Vector2 o, Vector2 gate, float s0, float s1, float step, System.Random rng, float gapA, float gapB, float gapSide)
    {
        for (float s = s0; s <= s1; s += step)
            foreach (float side in new[] { -1f, 1f })
            {
                bool inGap = gapSide != 0f && side == gapSide && s > gapA - 7f && s < gapB + 7f;           // cabang jalan: jangan ada pohon di lajur cabang
                if (inGap) continue;
                Vector2 p = T2Cp(gate, o, s, side * (15f + T2Rnd(rng, -0.8f, 0.8f)));
                VPlaceTree(g, p.x, p.y, 1f);
                Vector2 q = T2Cp(gate, o, s + 3.5f, side * (22f + T2Rnd(rng, -1f, 1f)));
                VPlaceTree(g, q.x, q.y, 1.1f);
                if (((int)(s / step) % 2) == 0)
                {
                    Vector2 u = T2Cp(gate, o, s + 1.5f, side * T2Rnd(rng, 10.6f, 12.8f));
                    var go = PropKit.Place(vTreeProtos[rng.Next(2) == 0 ? 3 : 6], g, "Semak", T2P(u.x, u.y, -0.05f), T2Rnd(rng, 0f, 360f), T2Rnd(rng, 0.9f, 1.4f), ForestKit.Slots);
                    if (go != null && rng.Next(3) == 0) T2Trunk(go, 1.4f, 0.5f);
                }
            }
    }

    // =====================================================================
    // Empat koridor + cabang Desa Pemula
    // =====================================================================
    static void T2Corridors()
    {
        T2CorridorSouth();
        T2CorridorEast();
        T2CorridorWest();
        T2CorridorNorth();
    }

    /// <summary>Selatan: jalan tanah di bawah pepohonan ke Hutan Lerai; cabang barat (z = -378) ke Desa Pemula.</summary>
    static void T2CorridorSouth()
    {
        Vector2 gate = T2.GateS, o = new Vector2(0f, -1f);
        var g = Group("KoridorSelatan", tEnv);
        var rng = new System.Random(55);
        T2ForestRows(g, o, gate, 34f, 82f, 7f, rng, 40f, 56f, 1f);

        // dinding tak terlihat: sisi barat (t = +19) dibelah di s = 40..56 = cabang jalan ke Desa Pemula
        T2CorridorWalls(gate, o, 4f, 88f, 19f, 1f, 40f, 56f);

        T2PlaceMilestone(g, T2Cp(gate, o, 46f, -8.2f), 0f);
        T2PlaceMilestone(g, T2Cp(gate, o, 62f, 8.2f), 180f);
        T2RoadSign("RoadSign", "HUTAN LERAI", "Wilayah perburuan", T2Cp(gate, o, 36f, -10f), 0f);

        // cabang Desa Pemula: dinding sepanjang cabang + ujung, pohon di kedua sisi, papan, zona transisi
        var br = Group("CabangDesaPemula", tEnv);
        T2WallLine(T2Cp(gate, o, 41.5f, 19f), T2Cp(gate, o, 41.5f, 72f));
        T2WallLine(T2Cp(gate, o, 54.5f, 19f), T2Cp(gate, o, 54.5f, 72f));
        T2WallLine(T2Cp(gate, o, 41f, 72f), T2Cp(gate, o, 55f, 72f));
        for (float t = 25f; t <= 66f; t += 8f)
            foreach (float sz in new[] { -1f, 1f })
            {
                Vector2 p = T2Cp(gate, o, 48f + sz * (8.6f + T2Rnd(rng, 0f, 2f)), t);
                VPlaceTree(br, p.x, p.y, 1f);
            }
        T2RoadSign("RoadSignDesa", "DESA PEMULA", "Jalan ke desa", T2Cp(gate, o, 38f, 12f), 0f);

        var atv = T2Transition("ToStartVillage", T2Cp(gate, o, 48f, 64f), new Vector3(4f, 14f, 30f));
        atv.destScene = StarterConfig.VillageScene;
        atv.destSpawn = "SpawnFromTown";
        atv.safeTravel = true;
        T2Spawn("SpawnFromVillage", T2Cp(gate, o, 48f, 40f), 90f);

        // zona transisi ke hutan (berjalan kaki, bukan tombol) + titik muncul saat kembali dari hutan
        T2Transition("ToForest", T2Cp(gate, o, 68f, 0f), new Vector3(36f, 14f, 4f));
        T2Spawn("SpawnFromForest", T2Cp(gate, o, 37f, 0f), 0f);           // di luar jembatan parit (berakhir di s = 28)
    }

    /// <summary>Timur: lajur pasir, palem, kaktus, agave dan batu ke Gurun Zahar.</summary>
    static void T2CorridorEast()
    {
        ForestKit.LoadDesert();
        Vector2 gate = T2.GateE, o = new Vector2(1f, 0f);
        var g = Group("KoridorTimur", tEnv);
        var rng = new System.Random(77);

        var sand = new GeoBag();
        var line = new List<Vector2> { T2Cp(gate, o, 30f, 0f), T2Cp(gate, o, 92f, 0f) };
        T2Strip(sand, Sl.Dirt, line, 31f, 0.03f, 12);
        sand.Emit(g, "PasirKoridor", ForestKit.DSlots, MeshDir + "/T2_CorSand.asset");

        for (float s = 34f; s <= 82f; s += 8f)
            foreach (float side in new[] { -1f, 1f })
            {
                Vector2 pin = T2Cp(gate, o, s, side * (15f + T2Rnd(rng, -0.8f, 0.8f)));
                int pk = rng.Next(3);
                var palm = PropKit.Place(PropKit.Get("TownDPalm" + pk, b => ForestKit.Palm(b, 10 + pk * 3, 1f, 1f)), g, "Palem", T2P(pin.x, pin.y), T2Rnd(rng, 0f, 360f), 1f, ForestKit.DSlots);
                T2Trunk(palm, 3f, 0.4f);
                Vector2 pout = T2Cp(gate, o, s + 3f, side * (22f + T2Rnd(rng, -1f, 1f)));
                int ok = rng.Next(4);
                GameObject go;
                if (ok == 0) go = PropKit.Place(PropKit.Get("TownDCactus", b => ForestKit.Cactus(b, 2, 1f)), g, "Kaktus", T2P(pout.x, pout.y), T2Rnd(rng, 0f, 360f), 1f, ForestKit.DSlots);
                else if (ok == 1) go = PropKit.Place(PropKit.Get("TownDShrub", b => ForestKit.DryShrub(b, 4, 1.3f)), g, "Semak", T2P(pout.x, pout.y), T2Rnd(rng, 0f, 360f), 1f, ForestKit.DSlots);
                else if (ok == 2) go = PropKit.Place(PropKit.Get("TownDBoulder", b => ForestKit.Boulder(b, 1, 1f, true)), g, "Batu", T2P(pout.x, pout.y), T2Rnd(rng, 0f, 360f), 1.4f, ForestKit.DSlots);
                else go = PropKit.Place(PropKit.Get("TownDAgave", b => ForestKit.Agave(b, 6)), g, "Agave", T2P(pout.x, pout.y), T2Rnd(rng, 0f, 360f), 1.3f, ForestKit.DSlots);
                if (go != null && ok != 1) T2Trunk(go, 1.4f, 0.6f);
                if (((int)(s / 8f) % 3) == 0)
                {
                    Vector2 pe = T2Cp(gate, o, s + 4f, side * T2Rnd(rng, 8.2f, 10.6f));
                    PropKit.Place(PropKit.Get("TownDShrub", b => ForestKit.DryShrub(b, 4, 1.3f)), g, "Semak", T2P(pe.x, pe.y, -0.04f), T2Rnd(rng, 0f, 360f), T2Rnd(rng, 0.7f, 1.0f), ForestKit.DSlots);
                }
            }
        T2CorridorWalls(gate, o, 4f, 88f, 19f, 0f, 0f, 0f);
        T2PlaceMilestone(g, T2Cp(gate, o, 46f, 8.2f), 270f);
        T2PlaceMilestone(g, T2Cp(gate, o, 60f, -8.2f), 90f);
        T2RoadSign("RoadSignTimur", "GURUN ZAHAR", "Padang pasir berbahaya", T2Cp(gate, o, 37f, -10f), 270f);

        var at = T2Transition("ToDesert", T2Cp(gate, o, 68f, 0f), new Vector3(4f, 14f, 36f));
        at.destScene = "HuntArea_Desert";
        at.townSpawn = "SpawnFromDesert";
        T2Spawn("SpawnFromDesert", T2Cp(gate, o, 55f, 0f), 270f);
    }

    /// <summary>Barat: lumpur, willow, sipres rawa, pohon mati, bakau, lentera hantu ke Rawa Terkutuk.</summary>
    static void T2CorridorWest()
    {
        ForestKit.LoadSwamp();
        Vector2 gate = T2.GateW, o = new Vector2(-1f, 0f);
        var g = Group("KoridorBarat", tEnv);
        var rng = new System.Random(131);

        var mud = new GeoBag();
        var line = new List<Vector2> { T2Cp(gate, o, 30f, 0f), T2Cp(gate, o, 92f, 0f) };
        T2Strip(mud, Sl.Dirt, line, 31f, 0.03f, 12);
        mud.Emit(g, "LumpurKoridor", ForestKit.SSlots, MeshDir + "/T2_CorMud.asset");

        for (float s = 34f; s <= 82f; s += 8f)
            foreach (float side in new[] { -1f, 1f })
            {
                Vector2 pin = T2Cp(gate, o, s, side * (15f + T2Rnd(rng, -0.8f, 0.8f)));
                int pk = rng.Next(3);
                GameObject tr;
                if (pk == 0) tr = PropKit.Place(PropKit.Get("TownSWillow", b => ForestKit.Willow(b, 4, 1f)), g, "Willow", T2P(pin.x, pin.y), T2Rnd(rng, 0f, 360f), 1f, ForestKit.SSlots);
                else if (pk == 1) tr = PropKit.Place(PropKit.Get("TownSCypress", b => ForestKit.BaldCypress(b, 6, 1f)), g, "Sipres", T2P(pin.x, pin.y), T2Rnd(rng, 0f, 360f), 1f, ForestKit.SSlots);
                else tr = PropKit.Place(PropKit.Get("TownSDead", b => ForestKit.SwampDeadTree(b, 3, 1f)), g, "PohonMati", T2P(pin.x, pin.y), T2Rnd(rng, 0f, 360f), 1f, ForestKit.SSlots);
                T2Trunk(tr, 3f, 0.45f);
                Vector2 pout = T2Cp(gate, o, s + 3f, side * (22f + T2Rnd(rng, -1f, 1f)));
                int ok = rng.Next(3);
                if (ok == 0) PropKit.Place(PropKit.Get("TownSMangrove", b => ForestKit.Mangrove(b, 5, 1f)), g, "Bakau", T2P(pout.x, pout.y), T2Rnd(rng, 0f, 360f), 1f, ForestKit.SSlots);
                else if (ok == 1) PropKit.Place(PropKit.Get("TownSCattail", b => ForestKit.Cattail(b, 2)), g, "Ilalang", T2P(pout.x, pout.y), T2Rnd(rng, 0f, 360f), 1.4f, ForestKit.SSlots);
                else T2Trunk(PropKit.Place(PropKit.Get("TownSBoulder", b => ForestKit.Boulder(b, 1, 1f, true)), g, "Batu", T2P(pout.x, pout.y), T2Rnd(rng, 0f, 360f), 1.3f, ForestKit.SSlots), 1.4f, 0.6f);
                if (((int)(s / 8f) % 3) == 1)
                {
                    Vector2 pe = T2Cp(gate, o, s + 4f, side * T2Rnd(rng, 8.4f, 10.8f));
                    PropKit.Place(PropKit.Get("TownSCattail", b => ForestKit.Cattail(b, 2)), g, "Ilalang", T2P(pe.x, pe.y, -0.04f), T2Rnd(rng, 0f, 360f), T2Rnd(rng, 0.9f, 1.3f), ForestKit.SSlots);
                }
            }
        var lamp = PropKit.Get("TownSLamp", b => ForestKit.LanternPost(b, true));
        for (float s = 40f; s <= 80f; s += 20f)
            foreach (float side in new[] { -1f, 1f })
            {
                Vector2 p = T2Cp(gate, o, s, side * 9f);
                Vector2 toRoad = T2Cp(gate, o, s, 0f) - p;
                PropKit.Place(lamp, g, "LenteraHantu", T2P(p.x, p.y, 0.05f), T2YawTo(p, p + toRoad) - 90f, 1f, ForestKit.SSlots);           // lengan lentera (+X lokal) menghadap jalan
            }
        T2CorridorWalls(gate, o, 4f, 88f, 19f, 0f, 0f, 0f);
        T2PlaceMilestone(g, T2Cp(gate, o, 46f, -8.2f), 90f);
        T2PlaceMilestone(g, T2Cp(gate, o, 60f, 8.2f), 270f);
        T2RoadSign("RoadSignBarat", "RAWA TERKUTUK", "Kabut tebal, waspada", T2Cp(gate, o, 37f, -10f), 90f);

        var at = T2Transition("ToSwamp", T2Cp(gate, o, 68f, 0f), new Vector3(4f, 14f, 36f));
        at.destScene = "HuntArea_Swamp";
        at.townSpawn = "SpawnFromSwamp";
        T2Spawn("SpawnFromSwamp", T2Cp(gate, o, 55f, 0f), 90f);
    }

    /// <summary>Utara: jalan raya kerajaan berpaving, berlampu, diapit sipres, ke Kota Eldrath (perjalanan aman tanpa quest).</summary>
    static void T2CorridorNorth()
    {
        Vector2 gate = T2.GateN, o = new Vector2(0f, 1f);
        var g = Group("KoridorUtara", tEnv);
        var rng = new System.Random(99);

        // jalan paving berbingkai batu di atas jalur tanah
        var pav = new GeoBag(); var kerb = new GeoBag();
        var line = new List<Vector2> { T2Cp(gate, o, 29f, 0f), T2Cp(gate, o, 90f, 0f) };
        T2Strip(pav, Sl.Pave, line, 6.4f, 0.08f, 4);
        foreach (float side in new[] { -1f, 1f })
        {
            var edge = new List<Vector2> { T2Cp(gate, o, 29f, side * 3.45f), T2Cp(gate, o, 90f, side * 3.45f) };
            T2Strip(kerb, Sl.Stone, edge, 0.5f, 0.1f, 1);
        }
        pav.Emit(g, "PavingRaya", baseSlots, MeshDir + "/T2_CorPave.asset");
        kerb.Emit(g, "BingkaiRaya", baseSlots, MeshDir + "/T2_CorKerb.asset");

        int n = 0;
        for (float s = 38f; s <= 82f; s += 12f)
            foreach (float side in new[] { -1f, 1f })
            {
                Vector2 p = T2Cp(gate, o, s, side * 5.6f);
                Vector2 toRoad = T2Cp(gate, o, s, 0f) - p;
                var go = T2PlaceLamp(T2P(p.x, p.y), T2YawTo(p, p + toRoad) - 90f, (n++ % 2) == 0);
                go.name = "LampuJalanRaya";
            }
        for (float s = 34f; s <= 88f; s += 8f)
            foreach (float side in new[] { -1f, 1f })
            {
                Vector2 p = T2Cp(gate, o, s, side * (14.5f + T2Rnd(rng, 0f, 1.4f)));
                T2Trunk(PropKit.Place(PropKit.TreeProto(3), g, "Sipres", T2P(p.x, p.y), T2Rnd(rng, 0f, 360f), 1f, baseSlots), 3f, 0.4f);
                Vector2 q = T2Cp(gate, o, s + 3f, side * (22f + T2Rnd(rng, -1f, 1f)));
                T2Trunk(PropKit.Place(PropKit.TreeProto(rng.Next(2)), g, "Pohon", T2P(q.x, q.y), T2Rnd(rng, 0f, 360f), 1f, baseSlots), 3f, 0.45f);
            }
        T2CorridorWalls(gate, o, 4f, 88f, 19f, 0f, 0f, 0f);
        T2PlaceMilestone(g, T2Cp(gate, o, 40f, 7.6f), 180f);
        T2RoadSign("RoadSignUtara", "KOTA ELDRATH", "Jalan raya kerajaan", T2Cp(gate, o, 37f, -10f), 180f);

        var at = T2Transition("ToDungeonCity", T2Cp(gate, o, 68f, 0f), new Vector3(36f, 14f, 4f));
        at.destScene = CityGate.DungeonCityScene;           // jalan raya utara langsung ke Kota Eldrath (muncul di koridor selatan Eldrath)
        at.destSpawn = "SpawnFromTown";
        at.safeTravel = true;
        T2Spawn("SpawnFromDungeon", T2Cp(gate, o, 55f, 0f), 180f);
    }
}
