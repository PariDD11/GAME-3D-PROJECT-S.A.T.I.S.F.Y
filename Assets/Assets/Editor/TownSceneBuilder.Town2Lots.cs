using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Kota Ashwatch "Konsentris" (n81): bangunan penting (Guild, Penginapan, Pandai Besi, Toko, Restoran, Akademi, Menara Penyihir, Katedral jarum putih di pusat, Barak, Istal,
/// Kastil beratap emas di bukit mahkota, 6 rumah bangsawan, Tempa Hadrik hitam jelaga di sudut utara Tembok Dalam) dengan SEMUA pengait gameplay lama
/// (GuildHall, Inn, Blacksmith, GeneralShop, Library, Restaurant, BuildingDoor, InfoPoint), Komplek Properti di lahan tetap, ratusan rumah warga dari denah (T2.Lots),
/// rumah tanah liat beratap jerami di ladang luar, bangunan pertanian.
/// </summary>
public static partial class TownSceneBuilder
{
    static readonly Dictionary<string, float> t2Heights = new Dictionary<string, float>();
    static float t2LmHeight;

    /// <summary>Arketipe mesh (dibuat sekali per kunci) + tinggi hasil bangun; tinggi disimpan agar tetap diketahui saat mesh diambil dari cache.</summary>
    static PropKit.Proto T2Proto(string key, System.Func<GeoBag, float> build, out float height)
    {
        float h = 0f;
        var p = PropKit.Get(key, b => { h = build(b); t2Heights[key] = h; });
        if (!t2Heights.TryGetValue(key, out height)) height = h;
        return p;
    }

    static bool T2IsHouse(string kind)
    {
        switch (kind)
        {
            case "row_a": case "row_b": case "twn_a": case "twn_b": case "shop_a": case "cot_a": case "corner": case "villa": case "store": case "workshop": case "thc_a": case "thc_b": return true;
        }
        return false;
    }

    // =====================================================================
    // Papan nama
    // =====================================================================
    /// <summary>Papan nama berdiri di atas dua tiang (terbaca dari +Z lokal induk). sub = baris kecil di bawah nama.</summary>
    static void T2PostSign(Transform parent, string text, string sub, Vector3 lp)
    {
        var s = new GameObject("PapanNama").transform;
        s.SetParent(parent, false);
        s.localPosition = lp;
        float cs = text.Length > 20 ? 0.05f : (text.Length > 12 ? 0.058f : 0.068f);
        float w = Mathf.Max(2.6f, text.Length * 3.84f * cs + 0.9f), h = sub == null ? 0.82f : 1.15f;
        Material wood = TownMats.Wood, plank = UMat("T2_PapanGelap", new Color(0.14f, 0.1f, 0.07f));
        foreach (float sx in new[] { -1f, 1f }) Prim(PrimitiveType.Cube, "Tiang", s, new Vector3(sx * (w * 0.5f - 0.22f), 1.4f, 0f), new Vector3(0.16f, 2.8f, 0.16f), wood, false);
        Prim(PrimitiveType.Cube, "Papan", s, new Vector3(0f, 2.5f, 0f), new Vector3(w, h, 0.1f), plank, false);
        Prim(PrimitiveType.Cube, "Bingkai", s, new Vector3(0f, 2.5f, -0.03f), new Vector3(w + 0.14f, h + 0.14f, 0.08f), wood, false);
        BuildKit.Text(s, text, new Vector3(0f, 2.5f + (sub != null ? 0.16f : 0f), 0.07f), 180f, cs, T2Gold);
        if (sub != null) BuildKit.Text(s, sub, new Vector3(0f, 2.5f - 0.27f, 0.07f), 180f, cs * 0.5f, new Color(0.95f, 0.9f, 0.76f));
    }

    // =====================================================================
    // Satu bangunan penting: root > Model (Mesh + Collider) + InteractPoint
    // =====================================================================
    static T T2Landmark<T>(string name, Transform parent, T2.Lot lot, System.Func<GeoBag, float> build, Material[] slots,
        string prompt, float forecourt, string signText, string signSub = null) where T : Interactable
    {
        var root = new GameObject(name).transform;
        root.SetParent(parent, false);
        root.position = new Vector3(lot.x, lot.py, lot.z);
        root.rotation = Quaternion.Euler(0f, lot.yaw, 0f);
        var model = new GameObject("Model").transform;        // hapus child ini untuk mengganti dengan prefab sendiri
        model.SetParent(root, false);
        var bag = new GeoBag();
        float h = build(bag);
        t2LmHeight = h;
        var mesh = bag.Emit(model, "Mesh", slots, MeshDir + "/T2_LM_" + name + ".asset");

        // jejak mesh di bidang XZ, dibatasi lahan + toleransi 2,5 m
        float x0 = -lot.w * 0.5f, x1 = lot.w * 0.5f, z0 = -lot.d * 0.5f, z1 = lot.d * 0.5f;
        var mf = mesh != null ? mesh.GetComponent<MeshFilter>() : null;
        if (mf != null && mf.sharedMesh != null)
        {
            var bb = mf.sharedMesh.bounds;
            x0 = Mathf.Max(bb.min.x, -(lot.w * 0.5f + 2.5f)); x1 = Mathf.Min(bb.max.x, lot.w * 0.5f + 2.5f);
            z0 = Mathf.Max(bb.min.z, -(lot.d * 0.5f + 2.5f)); z1 = Mathf.Min(bb.max.z, lot.d * 0.5f + 2.5f);
        }
        var col = new GameObject("Collider");
        col.transform.SetParent(model, false);
        var bc = col.AddComponent<BoxCollider>();
        bc.center = new Vector3((x0 + x1) * 0.5f, h * 0.5f, (z0 + z1) * 0.5f);
        bc.size = new Vector3(x1 - x0, h, z1 - z0);

        var ip = new GameObject("InteractPoint").transform;
        ip.SetParent(root, false);
        ip.localPosition = new Vector3(0f, 0f, z1 + forecourt);
        var comp = ip.gameObject.AddComponent<T>();
        comp.prompt = prompt;
        comp.radius = 3.8f;
        Prim(PrimitiveType.Cylinder, "Marker", ip, new Vector3(0f, 0.16f, 0f), new Vector3(2.4f, 0.03f, 2.4f), mMarker, false);
        AddLight(root, new Vector3(0f, 4.5f, z1 + 1.5f), 15f, 1.4f);
        if (signText != null) T2PostSign(root, signText, signSub, new Vector3(-(lot.w * 0.5f - 2.6f), 0f, z1 + 1.8f));
        ReserveBox(lot.x, lot.z, lot.w, lot.d, lot.yaw, 0.6f);
        return comp;
    }

    // =====================================================================
    // Bangunan penting
    // =====================================================================
    static void T2Landmarks()
    {
        T2.Lot l;
        // ---- Guild Petualang (nama emas di atas portal)
        l = T2.LotOf("guild");
        var guild = T2Landmark<GuildHall>("GuildHall", tBuildings, l, T2Kit.Guild, T2Mix(0, 2, 6, 3, 0, 1), "Guild Petualang", 3.4f, null);
        T2GuildPlaque(guild.transform.parent);

        // ---- Alun-alun Gerbang: Penginapan, Pandai Besi, Toko Umum, Restoran
        l = T2.LotOf("inn");
        var inn = T2Landmark<Inn>("Inn", tBuildings, l, T2Kit.Inn, T2Mix(2, 0, 4, 1, 3, 2), "Penginapan", 3.4f, "PENGINAPAN", "Kamar & Aula");
        Door(inn, "Inn");

        l = T2.LotOf("smithy");
        var smith = T2Landmark<Blacksmith>("Blacksmith", tBuildings, l, T2Kit.Smithy, T2Mix(3, 3, 0, 5, 0, 2), "Pandai Besi", 3.4f, "PANDAI BESI", "Senjata & Zirah");
        Door(smith, "Smithy");

        l = T2.LotOf("shop");
        var shop = T2Landmark<GeneralShop>("GeneralShop", tBuildings, l, T2Kit.Shop, T2Mix(1, 4, 0, 4, 2, 3), "Toko Umum", 3.4f, "TOKO UMUM", "Ramuan & Perbekalan");
        Door(shop, "Shop");

        l = T2.LotOf("tavern");
        var rest = T2Landmark<Restaurant>("Restaurant", tBuildings, l, T2Kit.Tavern, T2Mix(4, 0, 2, 1, 0, 4), "Restoran", 3.4f, "RESTORAN", "Makan & Minum");
        Door(rest, "Tavern");

        // ---- Barak (barat) dan Istal (timur) di antara gerbang selatan dan alun-alun
        l = T2.LotOf("barracks");
        var barr = T2Landmark<InfoPoint>("Barracks", tBuildings, l, T2Kit.Barracks, T2Mix(0, 3, 6, 3, 0, 4), "Barak Ksatria", 3.4f, "BARAK KSATRIA", null);
        barr.title = "Barak Ksatria";
        barr.message = "Markas penjaga kota. Para ksatria berlatih di halaman depan.";
        l = T2.LotOf("stables");
        var stab = T2Landmark<InfoPoint>("Stables", tBuildings, l, T2Kit.Stables, T2Mix(4, 1, 0, 1, 3, 4), "Istal", 3.4f, "ISTAL KOTA", null);
        stab.title = "Istal Kota";
        stab.message = "Kuda perang dan kuda tunggang disimpan di sini.";

        // ---- Kota dalam (bukit mahkota): Katedral putih ramping seperti jarum di pusat (dibangun di atas Retakan), Akademi (timur), Menara Penyihir
        l = T2.LotOf("cathedral");
        var cathSlots = T2Mix(1, 2, 6, 3, 4, 2);
        cathSlots[Sl.Stone] = TownMats.Marble; cathSlots[Sl.DarkStone] = TownMats.StoneMid; cathSlots[Sl.RoofB] = TownMats.PlasterWhite;
        var cath = T2Landmark<InfoPoint>("Cathedral", tBuildings, l, T2Kit.Cathedral, cathSlots, "Katedral", 4.2f, "KATEDRAL", "Cahaya Suci");
        cath.title = "Katedral Cahaya Suci";
        cath.message = "Katedral agung Ashwatch. Menaranya putih dan ramping seperti jarum, terlihat dari ketiga tembok. Orang tua bilang ia dibangun di atas retakan yang lebih tua dari kota, dan para penggali sumur selalu berhenti di kedalaman yang sama.\n\nLonceng berdentang enam kali tiap senja, lalu diam selama tujuh helaan napas. Tak ada yang menjelaskan alasannya.\n\n(Akan dibuka pada pembaruan berikutnya: berkat, penyembuhan kutukan, dan pernikahan.)";
        Door(cath, "Cathedral");

        l = T2.LotOf("library");
        var lib = T2Landmark<Library>("Library", tBuildings, l, T2Kit.Library, T2Mix(6, 2, 1, 3, 1, 4), "Akademi & Perpustakaan", 3.6f, "AKADEMI", "Perpustakaan Kota");
        Door(lib, "Academy");

        l = T2.LotOf("magetower");
        var mage = T2Landmark<InfoPoint>("MageTower", tBuildings, l, T2Kit.MageTower, T2Mix(6, 5, 1, 2, 5, 1), "Menara Penyihir", 3.2f, "MENARA SIHIR", null);
        mage.title = "Menara Penyihir";
        mage.message = "Menara para penyihir kota. Pintunya terkunci dengan segel sihir.\n\n(Akan dibuka pada pembaruan berikutnya: sihir, enchant, dan tugas penelitian.)";

        // ---- Kastil Penguasa (model asli 76 x 64 diperkecil seperlunya agar muat di halaman kastil, menara beratap emas) + 6 kediaman bangsawan
        l = T2.LotOf("castle");
        float cScale = l.w / 76f;
        var castleSlots = T2Mix(1, 2, 0, 3, 0, 1);
        castleSlots[Sl.Stone] = TownMats.DarkStone; castleSlots[Sl.DarkStone] = TownMats.StoneDeep; castleSlots[Sl.Roof] = TownMats.RoofGold; castleSlots[Sl.RoofB] = TownMats.RoofGold;
        var castle = T2Landmark<InfoPoint>("Castle", tNoble, l, b => { b.PushTRS(Vector3.zero, Quaternion.identity, cScale); float ch = T2Kit.Castle(b); b.Pop(); return ch * cScale; }, castleSlots, "Kastil Penguasa", 3.2f, null);
        castle.title = "Kastil Penguasa Perbatasan";
        castle.message = "Kediaman Lord penguasa Ashwatch, di puncak bukit mahkota. Menara-menara emasnya tampak dari seluruh kota.\n\nRakyat jelata tidak diizinkan masuk tanpa surat undangan.";

        for (int k = 0; k < 6; k++)
        {
            l = T2.LotOf("manor" + (k + 1));
            int mv = l.v;
            var m = T2Landmark<InfoPoint>("Manor_" + (k + 1), tNoble, l, b => T2Kit.Manor(b, mv),
                T2Mix(new[] { 1, 0, 6, 5, 1, 0 }[k], new[] { 2, 3, 4, 5, 2, 3 }[k], new[] { 0, 6, 1, 4, 0, 6 }[k], new[] { 3, 2, 5, 4, 3, 2 }[k], (k * 2 + 1) % 6, (k + 3) % 6),
                NobleFamilies[k], 4.2f, null);
            m.title = NobleFamilies[k];
            m.message = "Rumah bangsawan. Gerbang taman terkunci. Seorang pelayan mengintip dari balik tirai.";
        }

        // ---- Tempa Hadrik: bengkel rendah hitam jelaga di sudut utara Tembok Dalam (hiasan lore; Pandai Besi untuk berdagang tetap di Plaza Gerbang)
        l = T2.LotOf("forge");
        if (l != null)
        {
            var forgeSlots = T2Mix(3, 3, 0, 5, 0, 2);
            forgeSlots[Sl.Plaster] = TownMats.StoneDeep; forgeSlots[Sl.PlasterB] = TownMats.DarkStone; forgeSlots[Sl.Roof] = TownMats.Roofs[3]; forgeSlots[Sl.RoofB] = TownMats.Roofs[3];
            float fs = Mathf.Min(l.w / 13f, 1f);
            var forge = T2Landmark<InfoPoint>("Forge", tBuildings, l, b => { b.PushTRS(Vector3.zero, Quaternion.identity, fs); float fh = T2Kit.Smithy(b); b.Pop(); return fh * fs; }, forgeSlots, "Tempa Hadrik", 3.0f, "TEMPA HADRIK", "Pandai Batu");
            forge.title = "Tempa Hadrik";
            forge.message = "Bengkel tua di sudut utara Tembok Dalam. Dindingnya hitam jelaga, tungkunya tak pernah benar-benar padam.\n\nDari dalam terdengar palu: tiga kali, diam sejenak, tiga kali lagi. Hadrik Batu tidak melayani pembeli di sini; untuk senjata dan zirah, pergilah ke Pandai Besi di Plaza Gerbang.";
        }
    }

    /// <summary>Plakat nama emas "GUILD PETUALANG" di dinding depan, tepat di atas portal.</summary>
    static void T2GuildPlaque(Transform root)
    {
        var model = root.Find("Model");
        if (model == null) return;
        Material plank = UMat("T2_PlakGuild", new Color(0.13f, 0.09f, 0.06f));
        Prim(PrimitiveType.Cube, "PlakBingkai", model, new Vector3(0f, 13.65f, 11.17f), new Vector3(9.5f, 1.7f, 0.2f), TownMats.Gold, false);
        Prim(PrimitiveType.Cube, "PlakNama", model, new Vector3(0f, 13.65f, 11.3f), new Vector3(9.2f, 1.45f, 0.26f), plank, false);
        BuildKit.Text(model, "GUILD PETUALANG", new Vector3(0f, 13.68f, 11.48f), 180f, 0.14f, T2Gold);
    }

    // =====================================================================
    // Komplek Properti: lahan tetap "komplek" (50 x 52) di kota bawah barat; gerbang menghadap utara
    // =====================================================================
    static HouseKit.HouseDef T2Def(string key, string kind, int v)
    {
        float h;
        var p = T2Proto("T2H_" + kind + "_" + v, b => T2Kit.Kind(b, kind, v), out h);
        float w, d, hz; T2.Size(kind, out w, out d, out hz);
        return new HouseKit.HouseDef { key = key, kind = 0, w = w, d = d, height = h, proto = p };
    }

    static void T2PropertyComplex()
    {
        var lot = T2.LotOf("komplek");
        if (lot == null) return;
        try
        {
            var lay = new CxLayout();
            lay.t2 = true;
            foreach (var h in PropertyMenus.Houses) if (h.town == GuildConfig.TownScene && lay.homes.Count < 8) lay.homes.Add(h);
            lay.office = T2Def("kantor", "villa", 3);
            lay.officeSlots = T2Mix(3, 4, 1, 4, 3, 4);
            lay.homeSlots = new Material[8][];
            for (int i = 0; i < lay.homes.Count; i++)
            {
                string id = lay.homes[i].id;
                string kind = id == "town_rent" ? "cot_a" : (id == "town_manor" ? "villa" : "twn_a");
                lay.defs.Add(T2Def(id, kind, id == "town_rent" ? 1 : (id == "town_manor" ? 4 : 2)));
                lay.homeSlots[i] = T2Mix(new[] { 2, 4, 6, 1, 0, 3, 5, 2 }[i], new[] { 0, 1, 2, 3, 4, 5, 0, 1 }[i], new[] { 4, 1, 0, 6, 3, 2, 1, 5 }[i], 5, i + 1, i + 3);
            }
            lay.halfCourt = 11f;
            lay.L = lot.d;
            lay.W = lot.w;
            lay.halfW = lot.w * 0.5f;
            float[] cur = { -lay.L / 2f + 4f, -lay.L / 2f + 4f };
            for (int i = 0; i < lay.homes.Count; i++)
            {
                int col = i % 2;                                   // 0 = kiri (menghadap +X), 1 = kanan (menghadap -X)
                var d = lay.defs[i];
                lay.z[i] = cur[col] + d.w / 2f;
                cur[col] += d.w + 3.2f;
                lay.x[i] = (col == 0 ? -1f : 1f) * (lay.halfCourt + d.d / 2f);
                lay.yawOff[i] = col == 0 ? 90f : -90f;
            }
            BuildComplexAt(GuildConfig.TownScene, lay, lot.P, lot.yaw, lot.py, false);
        }
        catch (System.Exception e) { Debug.LogWarning("[Komplek Properti] gagal dibangun di denah baru: " + e.Message + "\n" + e.StackTrace); }
    }

    // =====================================================================
    // Rumah warga (330-an) dari denah
    // =====================================================================
    static void T2FillHouses()
    {
        var group = Group("RumahWarga", tHouses);
        foreach (var l in T2.Lots)
        {
            if (!T2IsHouse(l.kind)) continue;
            int vv = Mathf.Abs(l.v) % 16;
            string kind = l.kind;
            float h;
            var proto = T2Proto("T2H_" + kind + "_" + vv, b => T2Kit.Kind(b, kind, vv), out h);
            var go = PropKit.Place(proto, group, "Rumah_" + (++houseCount), new Vector3(l.x, l.py, l.z), l.yaw, 1f, T2HouseSlots(l.zone, l.v));
            var bc = go.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, h * 0.5f, 0f);
            bc.size = new Vector3(l.w, h, l.d);
            ReserveBox(l.x, l.z, l.w, l.d, l.yaw, 0.4f);
        }
    }

    // =====================================================================
    // Bangunan pertanian (sektor tenggara)
    // =====================================================================
    static void T2FarmBuildings(Transform g)
    {
        float h;
        foreach (var id in new[] { "farmhouse1", "farmhouse2" })
        {
            var l = T2.LotOf(id);
            if (l == null) continue;
            int v = l.v;
            var proto = T2Proto("T2H_farmhouse_" + v, b => T2Kit.Kind(b, "farmhouse", v), out h);
            var go = PropKit.Place(proto, g, "RumahPetani_" + id, new Vector3(l.x, l.py, l.z), l.yaw, 1f, T2HouseSlots("tani", 3 + v * 5));
            var bc = go.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, h * 0.5f, 0f); bc.size = new Vector3(l.w, h, l.d);
            ReserveBox(l.x, l.z, l.w, l.d, l.yaw, 0.6f);
        }
        var bl = T2.LotOf("barn1");
        if (bl != null)
        {
            const float S = 1.35f;
            var proto = T2Proto("T2H_barn", b => T2Kit.Kind(b, "store", 2), out h);
            var go = PropKit.Place(proto, g, "Lumbung_Kota", new Vector3(bl.x, bl.py, bl.z), bl.yaw, S, T2Mix(4, 1, 0, 5, 0, 4));
            var bc = go.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, h * 0.5f, 0f); bc.size = new Vector3(12f, h, 9f);
            ReserveBox(bl.x, bl.z, bl.w, bl.d, bl.yaw, 0.6f);
        }
        var gl = T2.LotOf("granary1");
        if (gl != null)
        {
            var proto = T2Proto("T2H_granary", T2Kit.Granary, out h);
            var go = PropKit.Place(proto, g, "Lumbung_Padi", new Vector3(gl.x, gl.py, gl.z), gl.yaw, 1f, T2Mix(4, 1, 0, 1, 0, 4));
            var bc = go.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 1.6f, 0f); bc.size = new Vector3(5.8f, 3.2f, 5.8f);
            ReserveBox(gl.x, gl.z, gl.w, gl.d, gl.yaw, 0.6f);
        }
        var wl = T2.LotOf("windmill");
        if (wl != null)
        {
            var proto = T2Proto("T2H_windmill", b => T2Kit.Windmill(b, false), out h);
            var body = PropKit.Place(proto, g, "KincirAngin", new Vector3(wl.x, wl.py, wl.z), wl.yaw, 1f, T2Mix(1, 3, 0, 5, 0, 4));
            var bc = body.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, h * 0.5f, 0f); bc.size = new Vector3(9.4f, h, 9.4f);
            var bladeProto = PropKit.Get("T2_WindmillBlades", b => T2Kit.WindmillBlades(b, Vector3.zero));
            var blades = PropKit.Place(bladeProto, g, "BilahKincir", Vector3.zero, 0f, 1f, T2Mix(1, 3, 0, 5, 0, 4));
            blades.transform.SetParent(body.transform, false);
            blades.transform.localPosition = new Vector3(0f, 15f, 4.3f);
            blades.transform.localRotation = Quaternion.identity;
            var sp = blades.AddComponent<SpinObject>();
            sp.axis = Vector3.forward;
            sp.degreesPerSecond = 16f;
            ReserveBox(wl.x, wl.z, wl.w + 2f, wl.d + 2f, wl.yaw, 0.6f);
        }
    }
}
