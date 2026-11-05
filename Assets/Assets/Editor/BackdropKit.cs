using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Latar kota untuk scene hutan: separuh selatan dari Ashwatch 3 Tembok (tembok luar/dalam/tengah, gerbang, sawah, rumah, kastil),
/// memakai kit yang sama dengan scene Town sehingga bentuknya identik. Tidak ada skrip atau NPC di sini, hanya model dan collider tembok.
/// Koordinat lokal = koordinat scene Town (pusat kota di (0,0), Gerbang Luar Selatan di z = -330).
/// </summary>
public static class BackdropKit
{
    const string MeshDir = "Assets/Meshes/Town3";

    static Transform bd;
    static Material[] baseSlots;

    public static Transform Build(Transform parent, Vector3 centerWorld)
    {
        TownMats.Load();
        GateKit.Reset();
        PropKit.Reset();
        baseSlots = TownMats.Slots();

        bd = new GameObject("TownBackdrop").transform;
        bd.SetParent(parent, false);
        bd.position = centerWorld;

        Ground();
        for (int tier = 0; tier < 3; tier++)
        {
            WallKit.BuildRing(bd, tier, GateKit.SlotsFor(tier), 6);       // busur Timur-Selatan dan Selatan-Barat
            foreach (int k in new[] { 1, 2, 3 })
            {
                float az = WallKit.GateAz[k];
                Vector2 p = WallKit.OnRing(tier, az);
                GateKit.Build(bd, "Gerbang_" + GateKit.TierName[tier] + "_" + WallKit.DirName[k], tier,
                    k == 2 && tier == 0 ? "ASHWATCH" : "GERBANG " + GateKit.TierName[tier].ToUpper(), "", new Vector3(p.x, 0f, p.y), az, tier == 0, false);
            }
        }
        Roads();
        Fields();
        Houses();
        Landmarks();
        return bd;
    }

    static void Ground()
    {
        var b = new GeoBag();
        b.Patch(Sl.Grass, new Vector2(-900f, -340f), new Vector2(-900f, 700f), new Vector2(900f, 700f), new Vector2(900f, -340f), -0.04f, new Vector2(1f, 0f));
        b.Emit(bd, "Ground", baseSlots);
    }

    static void Roads()
    {
        var bag = new GeoBag();
        float hd0 = GateKit.SpecFor(0).D / 2f, hd1 = GateKit.SpecFor(1).D / 2f, hd2 = GateKit.SpecFor(2).D / 2f;
        System.Action<float, float, float, int, float> axis = (r0, r1, w, slot, y) =>
        {
            var pts = new List<Vector2> { new Vector2(0f, -r0), new Vector2(0f, -r1) };
            bag.Ribbon(slot, pts, w, y);
        };
        axis(330f + hd0, 340f, 14f, Sl.Dirt, 0.1f);
        axis(215f + hd1, 330f - hd0, 12f, Sl.Dirt, 0.1f);
        axis(100f + hd2, 215f - hd1, 16f, Sl.Cobble, 0.1f);
        axis(40f, 100f - hd2, 16f, Sl.Pave, 0.1f);
        // lingkar-lingkar paling tampak
        foreach (float r in new[] { 122f, 196f })
        {
            var pts = new List<Vector2>();
            for (float a = 95f; a <= 265f; a += 3f) if (Mathf.Abs(a - 180f) > 24f && Mathf.Abs(a - 90f) > 24f && Mathf.Abs(a - 270f) > 24f) pts.Add(WallKit.Out(a) * r);
            if (pts.Count > 1) bag.Ribbon(Sl.Cobble, pts, 8f, 0.08f);
        }
        // alun-alun selatan, timur, barat
        foreach (int i in new[] { 0, 1, 3 })
        {
            Vector2 c = TownSceneBuilder.PlazaCenter(i);
            bag.Disc(Sl.Pave, new Vector3(c.x, 0f, c.y), TownSceneBuilder.PlazaR + 1.2f, 40, 0.11f);
            bag.Disc(Sl.Cobble, new Vector3(c.x, 0f, c.y), TownSceneBuilder.PlazaR, 40, 0.14f);
        }
        bag.Emit(bd, "Roads", baseSlots);
        var fountain = PropKit.Get("FountainL", bg => PropKit.Fountain(bg, Vector3.zero, 1.0f));
        Vector2 fc = TownSceneBuilder.PlazaCenter(0);
        Place(fountain, "Fountain", new Vector3(fc.x, 0.19f, fc.y), 0f, baseSlots);
    }

    static GameObject Place(PropKit.Proto p, string name, Vector3 pos, float yaw, Material[] slots)
    {
        var go = PropKit.Place(p, bd, name, bd.TransformPoint(pos), yaw, 1f, slots);
        return go;
    }

    static void Fields()
    {
        var rng = new System.Random(5);
        for (int q = 0; q < 2; q++)
        {
            float ac = q == 0 ? 135f : 225f;
            var slots = TownMats.Slots();
            slots[Sl.Field] = TownMats.FieldMats[q == 0 ? 1 : 2];
            slots[Sl.FieldB] = TownMats.FieldMats[q == 0 ? 2 : 3];
            var bag = new GeoBag();
            for (int band = 0; band < 2; band++)
            {
                float r0 = band == 0 ? 231f : 277f, r1 = band == 0 ? 267f : 321f, gap = band == 0 ? 7f : 3.4f;
                for (int h = -1; h <= 1; h += 2)
                {
                    float a0 = ac + h * gap, a1 = ac + h * 41f;
                    if (a0 > a1) { float t = a0; a0 = a1; a1 = t; }
                    int n = Mathf.CeilToInt((a1 - a0) / 5f);
                    float am = (a0 + a1) * 0.5f * Mathf.Deg2Rad;
                    Vector2 row = new Vector2(Mathf.Cos(am), -Mathf.Sin(am));
                    for (int i = 0; i < n; i++)
                    {
                        float x0 = Mathf.Lerp(a0, a1, i / (float)n), x1 = Mathf.Lerp(a0, a1, (i + 1) / (float)n);
                        bag.Patch((band + (h > 0 ? 1 : 0)) % 2 == 0 ? Sl.Field : Sl.FieldB, WallKit.Out(x0) * r0, WallKit.Out(x0) * r1, WallKit.Out(x1) * r1, WallKit.Out(x1) * r0, 0.06f, row);
                    }
                }
            }
            bag.Emit(bd, "Sawah_" + q, slots);
            // lumbung, rumah petani, kincir
            Vector2 pf = WallKit.Out(ac - 4.6f) * 249f, pb = WallKit.Out(ac + 5.6f) * 249f, axisPt = WallKit.Out(ac) * 249f;
            Place(PropKit.Get("Farmhouse", LandmarkKit.Farmhouse), "RumahPetani", new Vector3(pf.x, 0.04f, pf.y), Yaw(pf, axisPt), TownMats.Slots(TownMats.Plaster[(q * 2) % 7], TownMats.Roofs[(q + 1) % 6]));
            Place(PropKit.Get("Barn", LandmarkKit.Barn), "Lumbung", new Vector3(pb.x, 0.04f, pb.y), Yaw(pb, axisPt), TownMats.Slots(TownMats.Plaster[1], TownMats.Roofs[0]));
        }
        Vector2 mp = WallKit.Out(135f + 7.5f) * 300f;
        var body = Place(PropKit.Get("WindmillBody", LandmarkKit.WindmillBody), "KincirAngin", new Vector3(mp.x, 0.04f, mp.y), 135f, baseSlots);
        var blades = PropKit.Place(PropKit.Get("WindmillBlades", LandmarkKit.WindmillBlades), body.transform, "BilahKincir", Vector3.zero, 0f, 1f, baseSlots);
        blades.transform.SetParent(body.transform, false);
        blades.transform.localPosition = new Vector3(0f, 15.5f, 4.5f);
        blades.transform.localRotation = Quaternion.identity;
        var sp = blades.AddComponent<HunterGuild.SpinObject>();
        sp.axis = Vector3.forward;
        sp.degreesPerSecond = 18f;
    }

    static float Yaw(Vector2 from, Vector2 to) { Vector2 d = to - from; return Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg; }

    static void Houses()
    {
        HouseKit.BuildAll();
        var rng = new System.Random(11);
        var plazas = new List<Vector2> { TownSceneBuilder.PlazaCenter(0), TownSceneBuilder.PlazaCenter(1), TownSceneBuilder.PlazaCenter(3) };
        foreach (float r in new[] { 129f, 150f, 168f, 188f })
        {
            float a = 92f;
            while (a < 268f)
            {
                var def = HouseKit.Defs[rng.Next(HouseKit.Defs.Count)];
                Vector2 p = WallKit.Out(a) * r;
                float step = (def.w + 1.6f) / r * Mathf.Rad2Deg;
                bool skip = Mathf.Abs(p.x) < 14f;
                foreach (var c in plazas) if ((p - c).magnitude < TownSceneBuilder.PlazaR + 6f) skip = true;
                if (Mathf.Abs(Mathf.DeltaAngle(a, 225f)) < 12f || Mathf.Abs(Mathf.DeltaAngle(a, 135f)) < 12f) skip = true;     // barak dan istal
                if (p.x > 0f && p.x < 100f && p.y < -140f && p.y > -178f) skip = true;                                         // guild
                if (p.x < 0f && p.x > -64f && p.y < -140f && p.y > -178f) skip = true;                                         // penginapan
                if (!skip)
                {
                    float yaw = (r < 160f ? a : a + 180f);                       // menghadap jalan lingkar terdekat
                    yaw = Mathf.Repeat(yaw + (rng.Next(2) == 0 ? 0f : 180f), 360f);
                    int pi = rng.Next(TownMats.Plaster.Length), ri = rng.Next(TownMats.Roofs.Length);
                    var slots = TownMats.Slots(TownMats.Plaster[pi], TownMats.Roofs[ri], TownMats.Plaster[(pi + 3) % TownMats.Plaster.Length],
                        TownMats.Roofs[(ri + 2) % TownMats.Roofs.Length], TownMats.Cloths[rng.Next(6)], TownMats.Cloths[rng.Next(6)]);
                    if (def.kind == 3) slots[Sl.Roof] = TownMats.Thatch;
                    Place(def.proto, "Rumah", new Vector3(p.x, 0f, p.y), yaw, slots);
                }
                a += step;
            }
        }
    }

    static void Landmark(string name, Vector2 pos, float yaw, System.Action<GeoBag> build, Material[] slots)
    {
        var root = new GameObject(name).transform;
        root.SetParent(bd, false);
        root.localPosition = new Vector3(pos.x, 0f, pos.y);
        root.localRotation = Quaternion.Euler(0f, yaw, 0f);
        var bag = new GeoBag();
        build(bag);
        bag.Emit(root, "Mesh", slots, MeshDir + "/LM_" + name + ".asset");
    }

    static void Landmarks()
    {
        float yaw;
        Vector2 p;
        p = TownSceneBuilder.AroundPlaza(0, 90f, LandmarkKit.GuildD / 2f, out yaw);
        Landmark("GuildHall", p, yaw, LandmarkKit.Guild, TownMats.Slots());
        p = TownSceneBuilder.AroundPlaza(0, 270f, LandmarkKit.InnD / 2f, out yaw);
        Landmark("Inn", p, yaw, LandmarkKit.Inn, TownMats.Slots(TownMats.Plaster[2], TownMats.Roofs[1], null, null, TownMats.Cloths[3], TownMats.Cloths[4]));
        p = TownSceneBuilder.AroundPlaza(1, 0f, LandmarkKit.SmithD / 2f, out yaw);
        Landmark("Blacksmith", p, yaw, LandmarkKit.Smithy, TownMats.Slots(TownMats.Plaster[4], TownMats.Roofs[3]));
        p = TownSceneBuilder.AroundPlaza(1, 180f, LandmarkKit.ShopD / 2f, out yaw);
        Landmark("GeneralShop", p, yaw, LandmarkKit.Shop, TownMats.Slots(TownMats.Plaster[1], TownMats.Roofs[0], null, null, TownMats.Cloths[2], TownMats.Cloths[5]));
        p = TownSceneBuilder.AroundPlaza(2, 90f, LandmarkKit.AcadD / 2f, out yaw);
        Landmark("Library", p, yaw, LandmarkKit.Academy, TownMats.Slots(TownMats.Plaster[0], TownMats.Roofs[2]));
        p = TownSceneBuilder.AroundPlaza(2, 270f, LandmarkKit.MageR, out yaw);
        Landmark("MageTower", p, yaw, LandmarkKit.MageTower, TownMats.Slots(TownMats.Plaster[5], TownMats.Roofs[2]));
        Landmark("Cathedral", new Vector2(-150f, 64f), 180f, LandmarkKit.Cathedral, TownMats.Slots());
        p = TownSceneBuilder.AroundPlaza(3, 180f, LandmarkKit.TavernD / 2f, out yaw);
        Landmark("Restaurant", p, yaw, LandmarkKit.Tavern, TownMats.Slots(TownMats.Plaster[3], TownMats.Roofs[4], null, null, TownMats.Cloths[0], TownMats.Cloths[3]));
        p = WallKit.Out(225f) * 144f;
        Landmark("Barracks", p, Yaw(p, Vector2.zero), LandmarkKit.Barracks, TownMats.Slots());
        p = WallKit.Out(135f) * 144f;
        Landmark("Stables", p, Yaw(p, Vector2.zero), LandmarkKit.Stables, TownMats.Slots());
        Landmark("Castle", Vector2.zero, 180f, LandmarkKit.Castle, TownMats.Slots());
        for (int k = 0; k < 8; k++)
        {
            float az = 22.5f + k * 45f;
            int variant = k % 4, kk = k;
            Landmark("Manor_" + (k + 1), WallKit.Out(az) * 82f, az + 180f, b => LandmarkKit.Manor(b, variant),
                TownMats.Slots(TownMats.Plaster[(kk * 3) % 7], TownMats.Roofs[(kk * 2 + 1) % 6], null, null, TownMats.Cloths[kk % 6], TownMats.Cloths[(kk + 3) % 6]));
        }
    }
}
