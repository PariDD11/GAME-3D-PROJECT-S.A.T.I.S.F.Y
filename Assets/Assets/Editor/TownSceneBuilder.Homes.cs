using System.Collections.Generic;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Rumah yang bisa dibeli Hunter (lihat PropertyMenus.Houses): dibangun fisik di tiap kota sebagai bangunan dengan papan
/// "DIJUAL / MILIKMU" dan pintu (HomeDoor). Setelah dibeli, pintu membawa pemain ke scene Interior_Home.
/// </summary>
public static partial class TownSceneBuilder
{
    /// <summary>Kristal Mana (HunterGuild.ManaCrystal): visual dibuat saat game berjalan.</summary>
    static void PlaceManaCrystals(params Vector3[] positions)
    {
        var g = new GameObject("KristalMana").transform;
        g.SetParent(tProps, false);
        foreach (var p in positions)
        {
            var go = new GameObject("KristalMana");
            go.transform.SetParent(g, false);
            go.transform.position = p;
            go.AddComponent<ManaCrystal>();
            Reserve(p.x, p.z, 2.2f);
        }
    }

    static int HomeKind(string id)
    {
        switch (id)
        {
            case "vil_hut": return 3;
            case "vil_cottage": return 2;
            case "vil_house": return 0;
            case "town_rent": return 2;
            case "town_stone": return 0;
            case "town_manor": return 5;
            case "eld_loft": return 2;
            case "eld_tower": return 4;
            default: return 5;       // eld_villa
        }
    }

    /// <summary>Cari tempat kosong terdekat dengan anchor di dalam cincin [rMin, rMax] dari pusat kota.</summary>
    static bool FindHomeSpot(Vector2 anchor, HouseKit.HouseDef def, float rMin, float rMax, out Vector2 pos, out float yaw)
    {
        pos = Vector2.zero; yaw = 0f;
        float best = float.MaxValue; bool found = false;
        // lintasan 1: normal; lintasan 2 (cadangan, kalau lahan habis): cincin lebih lebar, langkah lebih halus, margin lebih kecil
        for (int pass = 0; pass < 3 && !found; pass++)
        {
            float r0 = pass == 0 ? rMin : pass == 1 ? Mathf.Max(30f, rMin - 35f) : Mathf.Max(22f, rMin - 80f);
            float r1 = pass == 0 ? rMax : pass == 1 ? rMax + 15f : rMax + 60f;
            float rs = pass == 0 ? 3f : 2f, ast = pass == 0 ? 6f : 3f;
            float mw = pass == 0 ? 3f : pass == 1 ? 1.5f : 0.8f, md = pass == 0 ? 7f : pass == 1 ? 4f : 2.5f;
            for (float r = r0; r <= r1; r += rs)
                for (float a = 0f; a < 360f; a += ast)
                {
                    Vector2 p = new Vector2(Mathf.Sin(a * Mathf.Deg2Rad), Mathf.Cos(a * Mathf.Deg2Rad)) * r;
                    float dist = (p - anchor).sqrMagnitude;
                    if (dist >= best) continue;
                    if (vStreetsActive && VNearStreet(p, Mathf.Max(def.w, def.d) / 2f)) continue;      // rumah dijual tidak boleh menutup jalan
                    float y = YawToward(p, Vector2.zero);
                    if (!BoxFree(p.x, p.y, def.w + mw, def.d + md, y, 0.4f)) continue;
                    best = dist; pos = p; yaw = y; found = true;
                }
        }
        return found;
    }

    /// <summary>Kantor Properti: bangunan besar sendiri dengan papan nama dan pintu ke interior (Interior_PropOffice) berisi Agen Properti & Penjual Furnitur.</summary>
    static void PlacePropertyOffice(string townScene, Vector3 agentPos)
    {
        if (HouseKit.Defs.Count == 0) HouseKit.BuildAll();
        bool eld = townScene == CityGate.DungeonCityScene;
        float rMin = 20f, rMax = 64f;
        if (townScene == GuildConfig.TownScene) { rMin = 100f; rMax = 200f; }
        else if (eld) { rMin = 105f; rMax = 232f; }
        var def = VDef(5, 11);
        Vector2 pos; float yaw;
        if (!FindHomeSpot(new Vector2(agentPos.x, agentPos.z), def, rMin, rMax, out pos, out yaw)) { Debug.LogWarning("Tidak ada lahan untuk Kantor Properti"); return; }
        BuildOfficeAt(townScene, def, pos, yaw, tHouses, 0f);
    }

    /// <summary>Bangun Kantor Properti di posisi/arah tertentu (dipakai penempatan lama dan Komplek Properti). parent = grup induk, gy = tinggi tanah.</summary>
    /// <param name="slotsOverride">palet bahan sendiri (Kota Ashwatch baru); null = palet lama</param>
    /// <param name="freeSign">true = papan nama berdiri di depan pintu (di atas dua tiang), bukan menempel di dinding</param>
    static void BuildOfficeAt(string townScene, HouseKit.HouseDef def, Vector2 pos, float yaw, Transform parent, float gy, Material[] slotsOverride = null, bool freeSign = false)
    {
        bool eld = townScene == CityGate.DungeonCityScene;
        Material[] slots = slotsOverride != null ? slotsOverride : (eld ? DungeonKit.Slots(1) : TownMats.Slots(TownMats.Plaster[3 % TownMats.Plaster.Length], TownMats.Roofs[1 % TownMats.Roofs.Length]));
        if (townScene == StarterConfig.VillageScene) VMapMark("Kantor Properti", pos, 1);
        var go = PropKit.Place(def.proto, parent, "KantorProperti", V3(pos, gy), yaw, 1f, slots);
        var bc = go.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, def.height / 2f, 0f);
        bc.size = new Vector3(def.w, def.height, def.d);
        ReserveBox(pos.x, pos.y, def.w + 1.5f, def.d + 1.5f, yaw, 0f);
        Reserve(pos.x, pos.y, Mathf.Max(def.w, def.d) / 2f + 1f);
        Vector2 fwd = new Vector2(Mathf.Sin(yaw * Mathf.Deg2Rad), Mathf.Cos(yaw * Mathf.Deg2Rad));
        Vector2 doorPt = pos + fwd * (def.d / 2f + 1.6f);
        Reserve(doorPt.x, doorPt.y, 3.5f);

        var holder = new GameObject("Papan_KantorProperti").transform;
        holder.SetParent(parent, false);
        holder.position = V3(pos, gy);
        holder.rotation = Quaternion.Euler(0f, yaw, 0f);
        float py = freeSign ? 3.05f : Mathf.Min(def.height * 0.62f, 5.4f), pz = freeSign ? def.d / 2f + 3.6f : def.d / 2f + 0.15f;
        float pw = freeSign ? 6.4f : 9.4f, ph = freeSign ? 1.3f : 1.5f, csT = freeSign ? 0.105f : 0.17f, csS = freeSign ? 0.048f : 0.07f;
        var plate = Prim(PrimitiveType.Cube, "PlatNama", holder, new Vector3(0f, py, pz), new Vector3(pw, ph, freeSign ? 0.2f : 0.3f), UMat("KantorPlat", new Color(0.16f, 0.1f, 0.06f)), false);
        if (freeSign)
            foreach (float sx in new[] { -1f, 1f })
                Prim(PrimitiveType.Cube, "TiangPapan", holder, new Vector3(sx * (pw / 2f - 0.35f), py / 2f, pz), new Vector3(0.22f, py, 0.22f), UMat("KantorTiang", new Color(0.3f, 0.2f, 0.12f)), true);
        float tz = pz + (freeSign ? 0.12f : 0.19f);
        BuildKit.Text(holder, "KANTOR PROPERTI", new Vector3(0f, py + (freeSign ? 0.18f : 0f), tz), 180f, csT, new Color(1f, 0.86f, 0.42f));
        BuildKit.Text(holder, "Jual-beli rumah & furnitur", new Vector3(0f, py - (freeSign ? 0.38f : 0.85f), tz), 180f, csS, new Color(0.95f, 0.9f, 0.75f));
        // tiang lampu & pot di kiri-kanan pintu
        foreach (float sx in new[] { -2.6f, 2.6f })
        {
            Prim(PrimitiveType.Cylinder, "PotTanaman", holder, new Vector3(sx, 0.45f, def.d / 2f + 1.2f), new Vector3(0.9f, 0.45f, 0.9f), UMat("KantorPot", new Color(0.4f, 0.25f, 0.15f)), true);
            Prim(PrimitiveType.Sphere, "DaunPot", holder, new Vector3(sx, 1.3f, def.d / 2f + 1.2f), new Vector3(1.1f, 1.1f, 1.1f), UMat("KantorDaun", new Color(0.2f, 0.5f, 0.22f)), false);
        }

        var door = new GameObject("Pintu_KantorProperti").transform;
        door.SetParent(parent, false);
        door.position = V3(doorPt, gy);
        door.rotation = Quaternion.Euler(0f, yaw, 0f);
        var bd = door.gameObject.AddComponent<BuildingDoor>();
        bd.interiorId = "PropOffice"; bd.radius = 3.8f; bd.prompt = "Masuk Kantor Properti";
        AddLight(holder, new Vector3(0f, 3.5f, def.d / 2f + 1.5f), 14f, 1.3f);
    }

    static void PlaceHomes(string townScene, Vector2 anchor)
    {
        if (HouseKit.Defs.Count == 0) HouseKit.BuildAll();
        float rMin = 20f, rMax = 64f;
        bool eld = townScene == CityGate.DungeonCityScene;
        if (townScene == StarterConfig.VillageScene) { rMin = 50f; rMax = 92f; }
        if (townScene == GuildConfig.TownScene) { rMin = 110f; rMax = 200f; }
        else if (eld) { rMin = 115f; rMax = 232f; }
        int idx = 0;
        foreach (var h in PropertyMenus.Houses)
        {
            if (h.town != townScene) continue;
            var def = VDef(HomeKind(h.id), idx++);
            Vector2 pos; float yaw;
            if (!FindHomeSpot(anchor, def, rMin, rMax, out pos, out yaw)) { Debug.LogWarning("Tidak ada lahan untuk rumah " + h.id); continue; }

            BuildHomeAt(townScene, h, def, pos, yaw, idx, tHouses, 0f, 6.5f);
        }
    }

    /// <summary>Bangun satu rumah dijual (rumah + papan DIJUAL + pintu HomeDoor). signSide = jarak papan ke samping jalan masuk.</summary>
    static void BuildHomeAt(string townScene, PropertyMenus.House h, HouseKit.HouseDef def, Vector2 pos, float yaw, int idx, Transform parent, float gy, float signSide, Material[] slotsOverride = null)
    {
        bool eld = townScene == CityGate.DungeonCityScene;
        Material[] slots;
        if (slotsOverride != null) slots = slotsOverride;
        else if (eld) { slots = DungeonKit.Slots(idx % 3); }
        else slots = TownMats.Slots(TownMats.Plaster[idx % TownMats.Plaster.Length], TownMats.Roofs[(idx + 2) % TownMats.Roofs.Length]);
        if (slotsOverride == null && def.kind == 3) slots[Sl.Roof] = TownMats.Thatch;
        if (townScene == StarterConfig.VillageScene) VMapMark("Dijual: " + h.name, pos, 2);
        var go = PropKit.Place(def.proto, parent, "Rumah_" + h.id, V3(pos, gy), yaw, 1f, slots);
        var bc = go.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, def.height / 2f, 0f);
        bc.size = new Vector3(def.w, def.height, def.d);
        ReserveBox(pos.x, pos.y, def.w + 1.5f, def.d + 1.5f, yaw, 0f);
        Reserve(pos.x, pos.y, Mathf.Max(def.w, def.d) / 2f + 1f);
        Vector2 fwd = new Vector2(Mathf.Sin(yaw * Mathf.Deg2Rad), Mathf.Cos(yaw * Mathf.Deg2Rad));
        Vector2 doorPt = pos + fwd * (def.d / 2f + 1.6f);
        Reserve(doorPt.x, doorPt.y, 3f);

        // papan "DIJUAL" berdiri di samping jalan masuk (bukan menempel di dinding)
        var holder = new GameObject("Papan_" + h.id).transform;
        holder.SetParent(parent, false);
        Vector2 rgt = new Vector2(fwd.y, -fwd.x);
        Vector2 signPt = doorPt + rgt * signSide - fwd * 0.4f;     // jauh di samping jalan masuk supaya tidak menutupi jalan / pintu
        Reserve(signPt.x, signPt.y, 1.2f);
        holder.position = V3(signPt, gy);
        holder.rotation = Quaternion.Euler(0f, yaw, 0f);
        Material mPost = UMat("PapanTiang", new Color(0.3f, 0.2f, 0.12f));
        Material mPlate = UMat("PapanPlat", new Color(0.14f, 0.09f, 0.06f));
        Prim(PrimitiveType.Cube, "Tiang", holder, new Vector3(0f, 1.0f, 0f), new Vector3(0.16f, 2.0f, 0.16f), mPost, false);
        Prim(PrimitiveType.Cube, "Plat", holder, new Vector3(0f, 1.9f, 0f), new Vector3(2.6f, 1.3f, 0.1f), mPlate, false);
        Prim(PrimitiveType.Cube, "Bingkai", holder, new Vector3(0f, 1.9f, -0.02f), new Vector3(2.7f, 1.4f, 0.06f), mPost, false);
        var signGo = BuildKit.Text(holder, "DIJUAL", new Vector3(0f, 1.9f, 0.1f), 180f, 0.04f, new Color(1f, 0.85f, 0.4f));

        var door = new GameObject("Pintu_" + h.id).transform;
        door.SetParent(parent, false);
        door.position = V3(doorPt, gy);
        door.rotation = Quaternion.Euler(0f, yaw, 0f);
        var hd = door.gameObject.AddComponent<HomeDoor>();
        hd.houseId = h.id; hd.radius = 3.6f; hd.prompt = "Rumah dijual: " + h.name;
        hd.sign = signGo.GetComponent<TextMesh>();
        hd.center = V3(pos, gy) + Vector3.up * def.height * 0.45f;
        hd.viewDist = Mathf.Max(def.w, def.d) * 1.15f + 7f;
    }
}
