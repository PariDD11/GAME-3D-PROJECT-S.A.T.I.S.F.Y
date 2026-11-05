using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HunterGuild;

public static partial class TownSceneBuilder
{
    // =====================================================================
    // Sektor pertanian (antara Tembok Dalam dan Tembok Luar)
    // =====================================================================
    static Vector2 Pol(float az, float r) { return WallKit.Out(az) * r; }

    static void AnnPatch(GeoBag bag, int slot, float r0, float r1, float a0, float a1, float y)
    {
        int n = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(a1 - a0) / 5f));
        float am = (a0 + a1) * 0.5f * Mathf.Deg2Rad;
        Vector2 row = new Vector2(Mathf.Cos(am), -Mathf.Sin(am));          // baris tanaman mengikuti garis singgung
        for (int i = 0; i < n; i++)
        {
            float x0 = Mathf.Lerp(a0, a1, i / (float)n), x1 = Mathf.Lerp(a0, a1, (i + 1) / (float)n);
            bag.Patch(slot, Pol(x0, r0), Pol(x0, r1), Pol(x1, r1), Pol(x1, r0), y, row);
        }
    }

    static List<Vector3> ArcFence(float r0, float r1, float a0, float a1)
    {
        var l = new List<Vector3>();
        int n = Mathf.Max(2, Mathf.CeilToInt(Mathf.Abs(a1 - a0) / 6f));
        for (int i = 0; i <= n; i++) l.Add(V3(Pol(Mathf.Lerp(a0, a1, i / (float)n), r0)));
        l.Add(V3(Pol(a1, r1)));
        for (int i = n; i >= 0; i--) l.Add(V3(Pol(Mathf.Lerp(a0, a1, i / (float)n), r1)));
        l.Add(V3(Pol(a0, r0)));
        return l;
    }

    static void BuildFarmland()
    {
        float[] quad = { 45f, 135f, 225f, 315f };
        string[] qn = { "TimurLaut_Peternakan", "TenggaraGandum", "BaratDaya_Sayur", "BaratLaut_Kebun" };
        var sheepP = PropKit.Get("Sheep", PropKit.Sheep);
        var cowP = PropKit.Get("Cow", PropKit.Cow);
        var hayP = PropKit.Get("Hay", bg => PropKit.Haystack(bg, Vector3.zero));
        var scareP = PropKit.Get("Scarecrow", PropKit.Scarecrow);
        var farmP = PropKit.Get("Farmhouse", LandmarkKit.Farmhouse);
        var barnP = PropKit.Get("Barn", LandmarkKit.Barn);
        var rng = new System.Random(99);

        for (int q = 0; q < 4; q++)
        {
            float ac = quad[q];
            var g = Group(qn[q], tFarm);
            var slots = TownMats.Slots();
            slots[Sl.Field] = TownMats.FieldMats[q % 4];
            slots[Sl.FieldB] = TownMats.FieldMats[(q + 1) % 4];
            var fieldBag = new GeoBag();
            var fenceBag = new GeoBag();
            var vineBag = new GeoBag();

            for (int band = 0; band < 2; band++)
            {
                float r0 = band == 0 ? 231f : 277f, r1 = band == 0 ? 267f : 321f;
                float gap = band == 0 ? 7f : 3.4f;
                for (int h = -1; h <= 1; h += 2)
                {
                    float a0 = ac + h * gap, a1 = ac + h * 41f;
                    if (a0 > a1) { float t = a0; a0 = a1; a1 = t; }
                    // jenis petak: 0 sawah A, 1 sawah B, 2 padang rumput, 3 kebun buah, 4 kebun anggur
                    int kind;
                    if (q == 0) kind = 2;
                    else if (q == 1 || q == 2) kind = (band + (h > 0 ? 1 : 0)) % 2;
                    else kind = band == 0 ? 3 : 4;

                    if (kind <= 1) AnnPatch(fieldBag, kind == 0 ? Sl.Field : Sl.FieldB, r0, r1, a0, a1, 0.06f);
                    else AnnPatch(fieldBag, Sl.Grass, r0, r1, a0, a1, 0.05f);

                    float amid = (a0 + a1) / 2f, rmid = (r0 + r1) / 2f;
                    if (kind == 2)
                    {
                        PropKit.Fence(fenceBag, ArcFence(r0 + 1f, r1 - 1f, a0 + 0.8f, a1 - 0.8f), 1.1f);
                        int cnt = band == 0 ? 16 : 10;
                        for (int i = 0; i < cnt; i++)
                        {
                            Vector2 p = Pol(Mathf.Lerp(a0 + 2f, a1 - 2f, (float)rng.NextDouble()), Mathf.Lerp(r0 + 3f, r1 - 3f, (float)rng.NextDouble()));
                            PropKit.Place(band == 0 ? sheepP : cowP, g, band == 0 ? "Domba" : "Sapi", V3(p, 0.05f), (float)rng.NextDouble() * 360f, 1f, baseSlots);
                        }
                    }
                    else if (kind == 3)
                    {
                        for (float r = r0 + 5f; r < r1 - 3f; r += 9f)
                            for (float a = a0 + 3f; a < a1 - 2f; a += 9f / r * Mathf.Rad2Deg)
                            {
                                Vector2 p = Pol(a, r);
                                var go = PropKit.Place(PropKit.TreeProto(5), g, "PohonApel", V3(p, 0.05f), (float)rng.NextDouble() * 360f, 1f, baseSlots);
                                var cc = go.AddComponent<CapsuleCollider>();
                                cc.center = new Vector3(0f, 1.2f, 0f); cc.radius = 0.4f; cc.height = 2.4f;
                            }
                    }
                    else if (kind == 4)
                    {
                        for (float r = r0 + 4f; r < r1 - 3f; r += 5f)
                            for (float a = a0 + 2f; a < a1 - 2f; a += 3.2f / r * Mathf.Rad2Deg)
                            {
                                Vector2 p = Pol(a, r);
                                vineBag.Box(Sl.Hedge, new Vector3(p.x, 0.65f, p.y), new Vector3(0.9f, 1.3f, 3.0f), a + 90f);
                                vineBag.Box(Sl.Wood, new Vector3(p.x, 0.9f, p.y), new Vector3(0.12f, 1.8f, 0.12f), a + 90f);
                            }
                    }
                    else
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            Vector2 p = Pol(Mathf.Lerp(a0 + 2f, a1 - 2f, (float)rng.NextDouble()), Mathf.Lerp(r0 + 3f, r1 - 3f, (float)rng.NextDouble()));
                            PropKit.Place(scareP, g, "OrangOrangSawah", V3(p, 0.06f), (float)rng.NextDouble() * 360f, 1f, baseSlots);
                        }
                        for (int i = 0; i < 3; i++)
                        {
                            Vector2 p = Pol(Mathf.Lerp(a0 + 2f, a1 - 2f, (float)rng.NextDouble()), Mathf.Lerp(r0 + 3f, r1 - 3f, (float)rng.NextDouble()));
                            PropKit.Place(hayP, g, "Jerami", V3(p, 0.06f), (float)rng.NextDouble() * 360f, 1f, baseSlots);
                        }
                    }
                }
            }
            fieldBag.Emit(g, "Petak", slots);
            fenceBag.Emit(g, "Pagar", baseSlots);
            vineBag.Emit(g, "KebunAnggur", baseSlots);

            // ---- pertanian kecil di sisi jalan petak diagonal: rumah petani + lumbung (+ sumur)
            Vector2 axisPt = Pol(ac, 249f);
            Vector2 pf = Pol(ac - 4.6f, 249f), pb = Pol(ac + 5.6f, 249f);
            var fh = PropKit.Place(farmP, g, "RumahPetani", V3(pf, 0.04f), YawToward(pf, axisPt), 1f,
                TownMats.Slots(TownMats.Plaster[(q * 2) % 7], TownMats.Roofs[(q + 1) % 6]));
            AddBox(fh, 12f, 5f, 8f, 0f);
            Reserve(pf.x, pf.y, 8f);
            var bn = PropKit.Place(barnP, g, "Lumbung", V3(pb, 0.04f), YawToward(pb, axisPt), 1f, TownMats.Slots(TownMats.Plaster[1], TownMats.Roofs[0]));
            AddBox(bn, 16f, 6f, 11f, 0f);
            Reserve(pb.x, pb.y, 9f);
            Vector2 wp = Pol(ac, 262f) + new Vector2(5f, 0f);
            PropKit.Place(PropKit.Get("WellL", bg => PropKit.Well(bg, Vector3.zero)), g, "Sumur", V3(wp, 0.04f), 0f, 0.9f, baseSlots);
            Reserve(wp.x, wp.y, 2f);

            if (q == 1 || q == 0)        // kincir angin
            {
                Vector2 mp = Pol(ac + 7.5f * (q == 1 ? 1f : -1f), 300f);
                var body = PropKit.Place(PropKit.Get("WindmillBody", LandmarkKit.WindmillBody), g, "KincirAngin", V3(mp, 0.04f), ac, 1f, baseSlots);
                AddBox(body, 11f, 12f, 11f, 0f);
                Reserve(mp.x, mp.y, 7f);
                var blades = PropKit.Place(PropKit.Get("WindmillBlades", LandmarkKit.WindmillBlades), g, "BilahKincir", Vector3.zero, 0f, 1f, baseSlots);
                blades.transform.SetParent(body.transform, false);
                blades.transform.localPosition = new Vector3(0f, 15.5f, 4.5f);
                blades.transform.localRotation = Quaternion.identity;
                var sp = blades.AddComponent<SpinObject>();
                sp.axis = Vector3.forward;
                sp.degreesPerSecond = 18f;
            }
        }
    }

    static void AddBox(GameObject go, float w, float h, float d, float cy)
    {
        var bc = go.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, h / 2f + cy, 0f);
        bc.size = new Vector3(w, h, d);
    }

    // =====================================================================
    // Di luar Tembok Luar: hutan di sekeliling + 3 jalan buntu
    // =====================================================================
    static void InvisibleWallY(Vector3 pos, Vector3 size, float yaw)
    {
        var go = new GameObject("InvisibleWall");
        go.transform.SetParent(tEnv, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        go.AddComponent<BoxCollider>().size = size;
    }

    static void RoadSignAt(string name, string text, Vector2 p, float yaw)
    {
        var sign = new GameObject(name).transform;
        sign.SetParent(tProps, false);
        sign.position = V3(p);
        sign.rotation = Quaternion.Euler(0f, yaw, 0f);
        Prim(PrimitiveType.Cube, "Post", sign, new Vector3(0f, 1.5f, 0f), new Vector3(0.3f, 3f, 0.3f), TownMats.Wood);
        Prim(PrimitiveType.Cube, "Board", sign, new Vector3(0f, 2.7f, 0f), new Vector3(4.6f, 1.2f, 0.15f), TownMats.DarkStone, false);
        BuildKit.Text(sign, text, new Vector3(0f, 2.7f, 0.12f), 180f, 0.07f, Color.white);
    }

    static void BuildOutside()
    {
        // hutan melingkar di luar Tembok Luar
        var g = Group("HutanLuar", tEnv);
        int placed = 0;
        var rng = new System.Random(2024);
        for (int i = 0; i < 4000 && placed < 650; i++)
        {
            float a = (float)rng.NextDouble() * 360f, r = 350f + (float)rng.NextDouble() * 330f;
            Vector2 p = Pol(a, r);
            if (Mathf.Abs(p.x) < 34f && p.y < 0f) continue;                       // koridor selatan
            if (p.x > 0f && Mathf.Abs(p.y) < 34f) continue;                       // koridor timur (gurun)
            if (p.y > 0f && Mathf.Abs(p.x) < 34f) continue;                       // koridor utara (Kota Eldrath)
            if (p.x < 0f && Mathf.Abs(p.y) < 34f) continue;                       // koridor barat (Rawa Terkutuk)
            bool nearAxis = false;
            for (int k = 0; k < 4; k++) if (Mathf.Abs(Mathf.DeltaAngle(a, WallKit.GateAz[k])) < Mathf.Rad2Deg * 22f / r) nearAxis = true;
            if (nearAxis) continue;
            if (Mathf.Abs(p.x) > 700f || Mathf.Abs(p.y) > 700f) continue;
            int kind = rng.Next(100) < 62 ? 0 : (rng.Next(100) < 70 ? 1 : 3);
            PropKit.Place(PropKit.TreeProto(kind), g, "Pohon", V3(p), (float)rng.NextDouble() * 360f, 0.9f + (float)rng.NextDouble() * 0.5f, baseSlots);
            placed++;
        }

        // tiga jalan buntu di Utara, Timur, Barat
        for (int k = 0; k < 4; k++)
        {
            if (k == 2 || k == 1 || k == 0 || k == 3) continue;       // selatan = Hutan Lerai, timur = Gurun Zahar, utara = Kota Eldrath, barat = Rawa Terkutuk (koridor sendiri)
            float az = WallKit.GateAz[k];
            Vector2 o = WallKit.Out(az), cw = new Vector2(Mathf.Cos(az * Mathf.Deg2Rad), -Mathf.Sin(az * Mathf.Deg2Rad));
            float rEnd = 392f;
            for (float r = 350f; r <= rEnd; r += 7f)
                foreach (float sx in new[] { -1f, 1f })
                {
                    Vector2 p = o * r + cw * sx * (14f + (float)rng.NextDouble() * 1.5f);
                    PropKit.Place(PropKit.TreeProto(rng.Next(2)), g, "Pohon", V3(p), (float)rng.NextDouble() * 360f, 1f, baseSlots);
                }
            // palang penghalang
            var bag = new GeoBag();
            bag.Box(Sl.Wood, new Vector3(-6f, 0.6f, 0f), new Vector3(0.3f, 1.2f, 0.3f));
            bag.Box(Sl.Wood, new Vector3(6f, 0.6f, 0f), new Vector3(0.3f, 1.2f, 0.3f));
            bag.Beam(Sl.WoodLight, new Vector3(-6f, 1.0f, 0f), new Vector3(6f, 1.0f, 0f), 0.2f, 0.12f);
            bag.Beam(Sl.WoodLight, new Vector3(-6f, 0.5f, 0f), new Vector3(6f, 0.5f, 0f), 0.2f, 0.12f);
            var bar = bag.Emit(g, "Palang", baseSlots);
            bar.transform.position = V3(o * (rEnd - 2f), 0f);
            bar.transform.rotation = Quaternion.Euler(0f, az, 0f);
            RoadSignAt("TandaJalanTertutup", "JALAN DITUTUP", o * (rEnd - 6f) + cw * 8.5f, az + 180f);
            // dinding tak terlihat
            float len = rEnd - 340f;
            Vector2 mid = o * (340f + len / 2f);
            InvisibleWallY(V3(mid + cw * 17f, 5f), new Vector3(1f, 12f, len), az);
            InvisibleWallY(V3(mid - cw * 17f, 5f), new Vector3(1f, 12f, len), az);
            InvisibleWallY(V3(o * (rEnd + 1f), 5f), new Vector3(40f, 12f, 1f), az);
        }
    }

    // =====================================================================
    // Jalan keluar kota -> hutan (selatan): gerbang luar -> koridor -> zona transisi
    // =====================================================================
    static void BuildCorridor()
    {
        float gz = GateSouthZ;
        var g = Group("Koridor", tEnv);
        var rng = new System.Random(55);
        for (float d = 14f; d <= 82f; d += 7f)
            foreach (float sx in new[] { -1f, 1f })
            {
                PropKit.Place(PropKit.TreeProto(rng.Next(2)), g, "Pohon", new Vector3(sx * (15f + (float)(rng.NextDouble() * 1.6 - 0.8)), 0f, gz - d), (float)rng.NextDouble() * 360f, 1f, baseSlots);
                PropKit.Place(PropKit.TreeProto(rng.Next(2)), g, "Pohon", new Vector3(sx * (22f + (float)(rng.NextDouble() * 2 - 1)), 0f, gz - d - 3f), (float)rng.NextDouble() * 360f, 1f, baseSlots);
            }
        // dinding tak terlihat: koridor sempit sepanjang jalan dan penutup di ujungnya
        // dinding barat dibelah: celah di gz-40..gz-56 = cabang jalan ke Desa Pemula
        InvisibleWall(new Vector3(-19f, 5f, gz - 22f), new Vector3(1f, 12f, 36f));
        InvisibleWall(new Vector3(-19f, 5f, gz - 72f), new Vector3(1f, 12f, 32f));
        InvisibleWall(new Vector3(19f, 5f, gz - 46f), new Vector3(1f, 12f, 84f));
        InvisibleWall(new Vector3(0f, 5f, gz - 88f), new Vector3(40f, 12f, 1f));

        RoadSignAt("RoadSign", "HUTAN LERAI", new Vector2(10f, gz - 30f), 0f);

        // cabang jalan ke Desa Pemula (barat dari koridor selatan), dua arah
        var br = Group("CabangDesaPemula", tEnv);
        Prim(PrimitiveType.Cube, "Jalan", br, new Vector3(-45f, 0.05f, gz - 48f), new Vector3(52f, 0.1f, 10f), TownMats.Dirt, false);
        InvisibleWall(new Vector3(-45f, 5f, gz - 41.5f), new Vector3(52f, 12f, 1f));
        InvisibleWall(new Vector3(-45f, 5f, gz - 54.5f), new Vector3(52f, 12f, 1f));
        InvisibleWall(new Vector3(-72f, 5f, gz - 48f), new Vector3(1f, 12f, 14f));
        for (float d = 6f; d <= 50f; d += 11f)
            foreach (float sz in new[] { -1f, 1f })
                PropKit.Place(PropKit.TreeProto(rng.Next(2)), br, "Pohon", new Vector3(-19f - d, 0f, gz - 48f + sz * (8.5f + (float)rng.NextDouble() * 2f)), (float)rng.NextDouble() * 360f, 1f, baseSlots);
        RoadSignAt("RoadSignDesa", "DESA PEMULA", new Vector2(-12f, gz - 38f), 0f);
        var tv = new GameObject("ToStartVillage").transform;
        tv.SetParent(tTown, false);
        tv.position = new Vector3(-64f, 0f, gz - 48f);
        var atv = tv.gameObject.AddComponent<AreaTransition>();
        atv.mode = AreaTransition.Mode.ToForest;
        atv.destScene = StarterConfig.VillageScene;
        atv.destSpawn = "SpawnFromTown";
        atv.safeTravel = true;
        atv.size = new Vector3(4f, 12f, 30f);
        var spv = new GameObject("SpawnFromVillage").transform;
        spv.SetParent(tTown, false);
        spv.position = new Vector3(-40f, 0f, gz - 48f);
        spv.rotation = Quaternion.Euler(0f, 90f, 0f);

        // zona transisi ke hutan (berjalan kaki, bukan tombol)
        var t = new GameObject("ToForest").transform;
        t.SetParent(tTown, false);
        t.position = new Vector3(0f, 0f, gz - 68f);
        var at = t.gameObject.AddComponent<AreaTransition>();
        at.mode = AreaTransition.Mode.ToForest;
        at.size = new Vector3(30f, 12f, 4f);

        // titik muncul saat kembali dari hutan: di jalan di luar gerbang, menghadap kota
        var sp = new GameObject("SpawnFromForest").transform;
        sp.SetParent(tTown, false);
        sp.position = new Vector3(0f, 0f, gz - 18f);
        sp.rotation = Quaternion.identity;
    }

    // =====================================================================
    // Jalan keluar kota -> gurun (timur): gerbang luar timur -> koridor berpasir -> zona transisi
    // =====================================================================
    static void BuildCorridorEast()
    {
        ForestKit.LoadDesert();
        float gx = WallKit.R[0];
        var g = Group("KoridorTimur", tEnv);
        var rng = new System.Random(77);

        var sand = new GeoBag();
        sand.Patch(Sl.Dirt, new Vector2(gx + 6f, -30f), new Vector2(gx + 6f, 30f), new Vector2(gx + 95f, 30f), new Vector2(gx + 95f, -30f), 0.03f, new Vector2(1f, 0f));
        sand.Emit(g, "PasirKoridor", ForestKit.DSlots);

        for (float d = 14f; d <= 82f; d += 8f)
            foreach (float sx in new[] { -1f, 1f })
            {
                float zIn = sx * (15f + (float)(rng.NextDouble() * 1.6 - 0.8));
                int pk = rng.Next(3);
                PropKit.Place(PropKit.Get("TownDPalm" + pk, b => ForestKit.Palm(b, 10 + pk * 3, 1f, 1f)), g, "Palem", new Vector3(gx + d, 0f, zIn), (float)rng.NextDouble() * 360f, 1f, ForestKit.DSlots);
                int ok = rng.Next(4);
                float zOut = sx * (22f + (float)(rng.NextDouble() * 2 - 1));
                if (ok == 0) PropKit.Place(PropKit.Get("TownDCactus", b => ForestKit.Cactus(b, 2, 1f)), g, "Kaktus", new Vector3(gx + d + 3f, 0f, zOut), (float)rng.NextDouble() * 360f, 1f, ForestKit.DSlots);
                else if (ok == 1) PropKit.Place(PropKit.Get("TownDShrub", b => ForestKit.DryShrub(b, 4, 1.3f)), g, "Semak", new Vector3(gx + d + 3f, 0f, zOut), (float)rng.NextDouble() * 360f, 1f, ForestKit.DSlots);
                else if (ok == 2) PropKit.Place(PropKit.Get("TownDBoulder", b => ForestKit.Boulder(b, 1, 1f, true)), g, "Batu", new Vector3(gx + d + 3f, 0f, zOut), (float)rng.NextDouble() * 360f, 1.4f, ForestKit.DSlots);
                else PropKit.Place(PropKit.Get("TownDAgave", b => ForestKit.Agave(b, 6)), g, "Agave", new Vector3(gx + d + 3f, 0f, zOut), (float)rng.NextDouble() * 360f, 1.3f, ForestKit.DSlots);
            }
        InvisibleWall(new Vector3(gx + 46f, 5f, -19f), new Vector3(84f, 12f, 1f));
        InvisibleWall(new Vector3(gx + 46f, 5f, 19f), new Vector3(84f, 12f, 1f));
        InvisibleWall(new Vector3(gx + 88f, 5f, 0f), new Vector3(1f, 12f, 40f));

        RoadSignAt("RoadSignTimur", "GURUN ZAHAR", new Vector2(gx + 30f, 10f), 270f);

        var t = new GameObject("ToDesert").transform;
        t.SetParent(tTown, false);
        t.position = new Vector3(gx + 68f, 0f, 0f);
        var at = t.gameObject.AddComponent<AreaTransition>();
        at.mode = AreaTransition.Mode.ToForest;
        at.destScene = "HuntArea_Desert";
        at.townSpawn = "SpawnFromDesert";
        at.size = new Vector3(4f, 12f, 30f);

        var sp = new GameObject("SpawnFromDesert").transform;
        sp.SetParent(tTown, false);
        sp.position = new Vector3(gx + 55f, 0f, 0f);
        sp.rotation = Quaternion.Euler(0f, 270f, 0f);
    }

    // =====================================================================
    // Jalan raya ke Kota Dungeon (utara): gerbang luar utara -> jalan berlampu kristal -> zona transisi
    // =====================================================================
    static void BuildCorridorNorth()
    {
        float gz = WallKit.R[0];
        var g = Group("KoridorUtara", tEnv);
        var rng = new System.Random(99);
        var lamp = PropKit.Get("TownRoyalLamp", b =>
        {
            b.Cyl(Sl.Stone, Vector3.zero, 0.36f, 0.6f, 8, 0.26f, true);
            b.Cyl(Sl.Stone, new Vector3(0f, 0.6f, 0f), 0.14f, 3.3f, 8, 0.11f, true);
            b.Cyl(Sl.Gold, new Vector3(0f, 3.8f, 0f), 0.3f, 0.2f, 8, 0.22f, true);
            b.Cone(Sl.Glow, new Vector3(0f, 4.0f, 0f), 0.3f, 0.9f, 6);
        });
        for (float d = 14f; d <= 82f; d += 12f)
            foreach (float sx in new[] { -1f, 1f })
                PropKit.Place(lamp, g, "LampuJalanRaya", new Vector3(sx * 8.5f, 0.1f, gz + d), 0f, 1f, baseSlots);
        for (float d = 10f; d <= 88f; d += 8f)
            foreach (float sx in new[] { -1f, 1f })
            {
                PropKit.Place(PropKit.TreeProto(3), g, "Sipres", new Vector3(sx * (14.5f + (float)(rng.NextDouble() * 1.4)), 0f, gz + d), (float)rng.NextDouble() * 360f, 1f, baseSlots);
                PropKit.Place(PropKit.TreeProto(rng.Next(2)), g, "Pohon", new Vector3(sx * (22f + (float)(rng.NextDouble() * 2 - 1)), 0f, gz + d + 3f), (float)rng.NextDouble() * 360f, 1f, baseSlots);
            }
        InvisibleWall(new Vector3(-19f, 5f, gz + 46f), new Vector3(1f, 12f, 84f));
        InvisibleWall(new Vector3(19f, 5f, gz + 46f), new Vector3(1f, 12f, 84f));
        InvisibleWall(new Vector3(0f, 5f, gz + 88f), new Vector3(40f, 12f, 1f));

        RoadSignAt("RoadSignUtara", "KOTA ELDRATH", new Vector2(10f, gz + 30f), 0f);

        var t = new GameObject("ToDungeonCity").transform;
        t.SetParent(tTown, false);
        t.position = new Vector3(0f, 0f, gz + 68f);
        var at = t.gameObject.AddComponent<AreaTransition>();
        at.mode = AreaTransition.Mode.ToForest;
        at.destScene = CityGate.DungeonCityScene;      // jalan raya utara langsung ke Kota Eldrath (muncul di koridor selatan Eldrath)
        at.destSpawn = "SpawnFromTown";
        at.safeTravel = true;
        at.size = new Vector3(30f, 12f, 4f);

        var sp = new GameObject("SpawnFromDungeon").transform;
        sp.SetParent(tTown, false);
        sp.position = new Vector3(0f, 0f, gz + 55f);
        sp.rotation = Quaternion.Euler(0f, 180f, 0f);
    }


    // =====================================================================
    // Jalan keluar kota -> Rawa Terkutuk (barat): gerbang luar barat -> koridor berlumpur dan berkabut -> zona transisi
    // =====================================================================
    static void BuildCorridorWest()
    {
        ForestKit.LoadSwamp();
        float gx = WallKit.R[0];
        var g = Group("KoridorBarat", tEnv);
        var rng = new System.Random(131);

        var mud = new GeoBag();
        mud.Patch(Sl.Dirt, new Vector2(-gx - 6f, 30f), new Vector2(-gx - 6f, -30f), new Vector2(-gx - 95f, -30f), new Vector2(-gx - 95f, 30f), 0.03f, new Vector2(1f, 0f));
        mud.Emit(g, "LumpurKoridor", ForestKit.SSlots);

        for (float d = 14f; d <= 82f; d += 8f)
            foreach (float sx in new[] { -1f, 1f })
            {
                float zIn = sx * (15f + (float)(rng.NextDouble() * 1.6 - 0.8));
                int pk = rng.Next(3);
                if (pk == 0) PropKit.Place(PropKit.Get("TownSWillow", b => ForestKit.Willow(b, 4, 1f)), g, "Willow", new Vector3(-gx - d, 0f, zIn), (float)rng.NextDouble() * 360f, 1f, ForestKit.SSlots);
                else if (pk == 1) PropKit.Place(PropKit.Get("TownSCypress", b => ForestKit.BaldCypress(b, 6, 1f)), g, "Sipres", new Vector3(-gx - d, 0f, zIn), (float)rng.NextDouble() * 360f, 1f, ForestKit.SSlots);
                else PropKit.Place(PropKit.Get("TownSDead", b => ForestKit.SwampDeadTree(b, 3, 1f)), g, "PohonMati", new Vector3(-gx - d, 0f, zIn), (float)rng.NextDouble() * 360f, 1f, ForestKit.SSlots);
                int ok = rng.Next(3);
                float zOut = sx * (22f + (float)(rng.NextDouble() * 2 - 1));
                if (ok == 0) PropKit.Place(PropKit.Get("TownSMangrove", b => ForestKit.Mangrove(b, 5, 1f)), g, "Bakau", new Vector3(-gx - d - 3f, 0f, zOut), (float)rng.NextDouble() * 360f, 1f, ForestKit.SSlots);
                else if (ok == 1) PropKit.Place(PropKit.Get("TownSCattail", b => ForestKit.Cattail(b, 2)), g, "Ilalang", new Vector3(-gx - d - 3f, 0f, zOut), (float)rng.NextDouble() * 360f, 1.4f, ForestKit.SSlots);
                else PropKit.Place(PropKit.Get("TownSBoulder", b => ForestKit.Boulder(b, 1, 1f, true)), g, "Batu", new Vector3(-gx - d - 3f, 0f, zOut), (float)rng.NextDouble() * 360f, 1.3f, ForestKit.SSlots);
            }
        var lamp = PropKit.Get("TownSLamp", b => ForestKit.LanternPost(b, true));
        for (float d = 20f; d <= 80f; d += 20f)
            foreach (float sx in new[] { -1f, 1f })
                PropKit.Place(lamp, g, "LenteraHantu", new Vector3(-gx - d, 0.05f, sx * 9f), sx > 0f ? 0f : 180f, 1f, ForestKit.SSlots);
        InvisibleWall(new Vector3(-gx - 46f, 5f, -19f), new Vector3(84f, 12f, 1f));
        InvisibleWall(new Vector3(-gx - 46f, 5f, 19f), new Vector3(84f, 12f, 1f));
        InvisibleWall(new Vector3(-gx - 88f, 5f, 0f), new Vector3(1f, 12f, 40f));

        RoadSignAt("RoadSignBarat", "RAWA TERKUTUK", new Vector2(-gx - 30f, -10f), 90f);

        var t = new GameObject("ToSwamp").transform;
        t.SetParent(tTown, false);
        t.position = new Vector3(-gx - 68f, 0f, 0f);
        var at = t.gameObject.AddComponent<AreaTransition>();
        at.mode = AreaTransition.Mode.ToForest;
        at.destScene = "HuntArea_Swamp";
        at.townSpawn = "SpawnFromSwamp";
        at.size = new Vector3(4f, 12f, 30f);

        var sp = new GameObject("SpawnFromSwamp").transform;
        sp.SetParent(tTown, false);
        sp.position = new Vector3(-gx - 55f, 0f, 0f);
        sp.rotation = Quaternion.Euler(0f, 90f, 0f);
    }

    static void InvisibleWall(Vector3 pos, Vector3 size)
    {
        InvisibleWallY(pos, size, 0f);
    }
}
