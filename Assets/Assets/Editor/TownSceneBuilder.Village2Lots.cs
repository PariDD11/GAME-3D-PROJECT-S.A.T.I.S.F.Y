using System.Collections.Generic;
using UnityEngine;
using HunterGuild;

/// <summary>Kampung Tepi Kali: semua bangunan (model V2Kit) + pengait gameplay lama (Elder, Inn, Healer, Smithy, Kantor Properti, rumah dijual, Toko Bram, gereja).</summary>
public static partial class TownSceneBuilder
{
    static Vector3 V2Local(V2.Lot l, float lx, float lz, float dy = 0f)
    {
        Vector2 p = V2.ToWorld(l, lx, lz);
        return new Vector3(p.x, V2.H(p.x, p.y) + dy, p.y);
    }

    static float V2DoorX(V2.Lot l, int v)
    {
        switch (l.kind)
        {
            case "thatchA": return v % 2 == 0 ? 0.6f : -0.6f;
            case "thatchB": return -1f;
            case "tudor": return v == 0 ? -1.2f : 1.2f;
            case "healer": return 0.9f;
            case "smithy": return -2.6f;
            case "mill": return -1.2f;
            default: return 0f;
        }
    }

    static System.Action<GeoBag> V2Builder(string kind, int v)
    {
        switch (kind)
        {
            case "elder": return b => V2Kit.Elder(b);
            case "inn": return b => V2Kit.Inn(b);
            case "smithy": return b => V2Kit.Smithy(b);
            case "healer": return b => V2Kit.Healer(b);
            case "office": return b => V2Kit.Office(b);
            case "chapel": return b => V2Kit.Chapel(b);
            case "barn": return b => V2Kit.Barn(b);
            case "mill": return b => V2Kit.Mill(b);
            case "granary": return b => V2Kit.Granary(b);
            case "thatchA": return b => V2Kit.ThatchA(b, v);
            case "thatchB": return b => V2Kit.ThatchB(b, v);
            case "cabin": return b => V2Kit.Cabin(b, v);
            case "farm": return b => V2Kit.Farm(b, v);
            case "tudor": return b => V2Kit.Tudor(b, v);
            case "stall": return b => V2Kit.Stall(b, v);
            case "shed": return b => V2Kit.House(b, new V2Kit.HSpec { w = 4f, d = 3f, floors = 1, wallH = 2.2f, rise = 1.3f, wall = 3, roof = 0, seed = 5, windowsFront = 0, windowsSide = 0, chimney = -1, doorX = 0f, porch = false, flowers = false, shutters = false, lantern = false, woodpile = true, noSalt = true });
        }
        return null;
    }

    static string V2ObjName(V2.Lot l, int n)
    {
        switch (l.id)
        {
            case "elder": return "RumahKepalaDesa";
            case "inn": return "Penginapan";
            case "smithy": return "PandaiBesi";
            case "healer": return "PondokTabib";
            case "office": return "KantorProperti";
            case "chapel": return "GerejaDesa";
            case "barn": return "LumbungBesar";
            case "mill": return "KincirAir";
            case "granary": return "LumbungPadi";
        }
        if (l.id.StartsWith("home_")) return "Rumah_" + l.id.Replace("home_", "vil_");
        if (l.kind == "stall") return "Kios" + l.id.Replace("stall", "");
        if (l.kind == "shed") return "GudangKayu";
        return "RumahWarga" + n;
    }

    static GameObject V2Place(V2.Lot l, string name, Transform parent, int v, int slotVariant)
    {
        var build = V2Builder(l.kind, v);
        if (build == null) return null;
        var proto = PropKit.Get("V2_" + l.kind + "_" + v, build);
        var go = PropKit.Place(proto, parent, name, new Vector3(l.x, l.py, l.z), l.yaw, 1f, V2Slots(slotVariant));
        if (l.kind != "stall")
        {
            var bc = go.AddComponent<BoxCollider>();
            float hz = l.kind == "chapel" ? 9f : l.hz;                    // menara gereja memakai collider sendiri
            bc.center = new Vector3(0f, hz / 2f, 0f);
            bc.size = new Vector3(l.w, hz, l.d);
        }
        ReserveBox(l.x, l.z, l.w + 1.5f, l.d + 1.5f, l.yaw, 0f);
        Reserve(l.x, l.z, Mathf.Max(l.w, l.d) / 2f + 1f);
        houseCount++;
        return go;
    }

    /// <summary>Papan kecil dari kayu dengan teks, dipasang pada bangunan (koordinat lokal bangunan; teks terbaca dari +Z lokal).</summary>
    static void V2Plaque(Transform host, string text, Vector3 localPos, float w, float h, float size, Color col)
    {
        var gb = new GeoBag();
        V2Kit.SignBoard(gb, 0f, 0f, 0f, w, h);
        var go = gb.Emit(host, "PapanNama", baseSlots);
        go.transform.localPosition = localPos;
        BuildKit.Text(host, text, localPos + new Vector3(0f, 0f, 0.1f), 180f, size, col);
    }

    static void V2DoorLight(Transform host, V2.Lot l, float doorX)
    {
        AddLight(host, new Vector3(doorX + 0.9f, 2.5f, l.d * 0.5f + 0.5f), 8f, 1.1f);
    }

    static void V2Lots()
    {
        var yardG = Group("HalamanRumah", tProps);
        v2LotDoor.Clear();
        int idx = 0, n = 0;
        foreach (var l in V2.Lots)
        {
            if (l.kind == "pasture" || l.kind == "orchard" || l.kind == "yard" || l.kind == "graveyard" || l.kind == "field" || l.kind == "bridgepad") continue;
            bool cottage = l.id.Length >= 2 && l.id[0] == 'c' && char.IsDigit(l.id[1]);
            bool home = l.id.StartsWith("home_");
            idx++;
            int v = cottage ? l.v + 2 * (idx % 3) : l.v;
            int sv = idx * 3 + l.v;
            if (cottage) n++;
            string nm = V2ObjName(l, n);
            var parent = l.kind == "stall" ? tProps : tHouses;
            var go = V2Place(l, nm, parent, v, sv);
            if (go == null) continue;
            float dx = V2DoorX(l, v);
            v2LotDoor[l.id] = dx;                                          // n82: V2Canon (meja Basra) membaca letak pintu per lot
            Transform t = go.transform;

            switch (l.id)
            {
                case "elder": V2Elder(l, go, dx); break;
                case "inn": V2Inn(l, go); break;
                case "smithy": V2Smithy(l, go); break;
                case "healer": V2Healer(l, go, dx); break;
                case "office": V2Office(l, go); break;
                case "chapel": V2Chapel(l, go); break;
                case "barn": V2Barn(l, go); break;
                case "mill": V2Mill(l, go); break;
                case "granary":
                    VMapMark("Lumbung Padi", new Vector2(l.x, l.z), 4);
                    break;
                default:
                    if (home) V2HomeDoor(l, go, v, dx);
                    else if (l.id == "stall0") V2Shop(l, go);
                    else if (cottage) V2Yard(l, yardG, n, dx);
                    break;
            }
        }
    }

    // ------------------------------------------------------------------ rumah warga: kebun kecil + barang
    static void V2Yard(V2.Lot l, Transform g, int n, float doorX)
    {
        var b = new GeoBag();
        float side = (n % 2 == 0) ? 1f : -1f;
        if (doorX * side > 0.3f) side = -side;                       // jangan di sisi pintu
        b.Push(new Vector3(l.x, l.py, l.z), l.yaw);
        b.Push(new Vector3(side * (l.w * 0.5f + 1.9f), 0f, l.d * 0.5f - 0.2f), 0f);
        V2Kit.GardenBed(b, 2.4f, 1.1f, n * 5 + 1);
        b.Pop();
        if (n % 3 == 0) { V2Kit.Barrel(b, new Vector3(-side * (l.w * 0.5f + 0.9f), 0f, l.d * 0.5f + 0.6f)); V2Kit.Crate(b, new Vector3(-side * (l.w * 0.5f + 1.8f), 0f, l.d * 0.5f + 0.5f), 0.7f, 15f); }
        if (n % 4 == 1) V2Kit.Bale(b, new Vector3(-side * (l.w * 0.5f + 1.4f), 0f, -l.d * 0.2f), 20f);
        b.Pop();
        b.Emit(g, "Halaman_RumahWarga" + n, baseSlots);
    }

    // ------------------------------------------------------------------ rumah kepala desa
    static void V2Elder(V2.Lot l, GameObject go, float dx)
    {
        VMapMark("Rumah Veteran Brann", new Vector2(l.x, l.z), 0);
        V2Plaque(go.transform, "VETERAN BRANN", new Vector3(dx, 3.55f, l.d * 0.5f + 0.1f), 3.3f, 0.7f, 0.065f, V2Gold);
        V2DoorLight(go.transform, l, dx);
        Vector3 np = V2Local(l, dx + 2.4f, l.d * 0.5f + 2.2f);
        var elder = VNpc("NPC_KepalaDesa", "Veteran Brann", "Petualang Veteran", "Hunter", np, false);
        elder.transform.rotation = Quaternion.Euler(0f, l.yaw + 20f, 0f);
        VAttach<VillageElder>(elder, "Bicara: Veteran Brann", 3.6f);
    }

    // ------------------------------------------------------------------ penginapan
    static void V2Inn(V2.Lot l, GameObject go)
    {
        VMapMark("Penginapan", new Vector2(l.x, l.z), 0);
        // papan gantung di tonggak sudut kiri (x -3.8, y 2.6, z 6.8): dua muka
        var tr = go.transform;
        BuildKit.Text(tr, "PENGINAPAN\nKali Embun", new Vector3(-3.8f + 0.07f, 2.62f, 6.8f), -90f, 0.034f, new Color(0.18f, 0.1f, 0.05f));
        BuildKit.Text(tr, "PENGINAPAN\nKali Embun", new Vector3(-3.8f - 0.07f, 2.62f, 6.8f), 90f, 0.034f, new Color(0.18f, 0.1f, 0.05f));
        AddLight(tr, new Vector3(0f, 2.6f, l.d * 0.5f + 2.6f), 11f, 1.3f);
        AddLight(tr, new Vector3(-3.0f, 1.8f, -l.d * 0.5f - 1f), 8f, 0.9f);
        Vector3 dp = V2Local(l, 0f, l.d * 0.5f + 3.0f);
        VPoint<Inn>(tHouses, "InteractPoint_Inn", dp, "Penginapan Desa (tidur & simpan)", 3.6f);
        var deco = new GeoBag();
        deco.Push(new Vector3(l.x, l.py, l.z), l.yaw);
        V2Kit.Barrel(deco, new Vector3(6.6f, 0f, 3.0f)); V2Kit.Crate(deco, new Vector3(6.8f, 0f, 1.8f), 0.8f, 12f);
        deco.Pop();
        deco.Emit(tProps, "DekorPenginapan", baseSlots);
    }

    // ------------------------------------------------------------------ pandai besi (pintu ke interior "Smithy")
    static void V2Smithy(V2.Lot l, GameObject go)
    {
        VMapMark("Pandai Besi & Toko", new Vector2(l.x, l.z), 1);
        var tr = go.transform;
        BuildKit.Text(tr, "PANDAI\nBESI", new Vector3(1.2f + 0.07f, 2.3f, 4.4f), -90f, 0.03f, new Color(1f, 0.82f, 0.5f));
        BuildKit.Text(tr, "PANDAI\nBESI", new Vector3(1.2f - 0.07f, 2.3f, 4.4f), 90f, 0.03f, new Color(1f, 0.82f, 0.5f));
        AddLight(tr, new Vector3(3.8f, 1.6f, -1.2f), 9f, 1.6f);                      // bara tungku
        AddLight(tr, new Vector3(-2.6f, 2.5f, 3.9f), 8f, 1.0f);
        var door = new GameObject("Pintu_PandaiBesi").transform;
        door.SetParent(tHouses, false);
        door.position = V2Local(l, -2.6f, 3.1f + 1.7f);
        door.rotation = Quaternion.Euler(0f, l.yaw, 0f);
        var bd = door.gameObject.AddComponent<BuildingDoor>();
        bd.interiorId = "Smithy"; bd.radius = 3.8f; bd.prompt = "Masuk Pandai Besi";
        var deco = new GeoBag();
        deco.Push(new Vector3(l.x, l.py, l.z), l.yaw);
        V2Kit.Crate(deco, new Vector3(-4.6f, 0f, 3.6f), 0.8f, 8f); V2Kit.Barrel(deco, new Vector3(5.7f, 0f, 3.8f));
        deco.Pop();
        deco.Emit(tProps, "DekorPandaiBesi", baseSlots);
    }

    // ------------------------------------------------------------------ pondok tabib
    static void V2Healer(V2.Lot l, GameObject go, float dx)
    {
        VMapMark("Tabib Mira", new Vector2(l.x, l.z), 0);
        V2Plaque(go.transform, "TABIB", new Vector3(dx, 2.95f, l.d * 0.5f + 0.1f), 1.5f, 0.5f, 0.07f, new Color(0.85f, 1f, 0.8f));
        V2DoorLight(go.transform, l, dx);
        Vector3 np = V2Local(l, dx - 1.8f, l.d * 0.5f + 2.2f);
        var healer = VNpc("NPC_Tabib", "Tabib Mira", "Tabib Desa", "Peziarah", np, false);
        healer.transform.rotation = Quaternion.Euler(0f, l.yaw - 15f, 0f);
        VAttach<VillageHealer>(healer, "Bicara: Tabib Mira", 3.4f);
    }

    // ------------------------------------------------------------------ kantor properti (pintu ke interior "PropOffice") + agen di dalam
    static void V2Office(V2.Lot l, GameObject go)
    {
        VMapMark("Kantor Properti", new Vector2(l.x, l.z), 1);
        BuildKit.Text(go.transform, "KANTOR PROPERTI", new Vector3(0f, 3.75f, l.d * 0.5f + 0.05f + 0.55f + 0.1f), 180f, 0.066f, new Color(1f, 0.88f, 0.5f));
        BuildKit.Text(go.transform, "Jual-beli rumah & furnitur", new Vector3(0f, 3.28f, l.d * 0.5f + 0.05f + 0.55f + 0.1f), 180f, 0.028f, new Color(0.95f, 0.9f, 0.75f));
        V2DoorLight(go.transform, l, 0f);
        var door = new GameObject("Pintu_KantorProperti").transform;
        door.SetParent(tHouses, false);
        door.position = V2Local(l, 0f, l.d * 0.5f + 2.4f);
        door.rotation = Quaternion.Euler(0f, l.yaw, 0f);
        var bd = door.gameObject.AddComponent<BuildingDoor>();
        bd.interiorId = "PropOffice"; bd.radius = 3.8f; bd.prompt = "Masuk Kantor Properti";
    }

    // ------------------------------------------------------------------ rumah dijual (HomeDoor + papan DIJUAL)
    static void V2HomeDoor(V2.Lot l, GameObject go, int v, float dx)
    {
        string id = l.id.Replace("home_", "vil_");
        var h = System.Array.Find(PropertyMenus.Houses, q => q.id == id);
        VMapMark("Dijual: " + (h != null ? h.name : id), new Vector2(l.x, l.z), 2);
        if (h == null) return;
        Vector3 doorPt = V2Local(l, dx, l.d * 0.5f + 2.5f);
        // papan DIJUAL berdiri di samping jalan masuk
        float sgn = dx > 0.3f ? -1f : 1f;
        Vector3 sp = V2Local(l, dx + sgn * 3.0f, l.d * 0.5f + 2.2f);
        var holder = new GameObject("Papan_" + l.id).transform;
        holder.SetParent(tHouses, false);
        holder.position = sp;
        holder.rotation = Quaternion.Euler(0f, l.yaw, 0f);
        var sb = new GeoBag();
        sb.Cyl(Sl.Wood, new Vector3(-0.9f, 0f, 0f), 0.07f, 1.9f, 6, 0.06f, true, false);
        sb.Cyl(Sl.Wood, new Vector3(0.9f, 0f, 0f), 0.07f, 1.9f, 6, 0.06f, true, false);
        V2Kit.SignBoard(sb, 0f, 1.55f, 0f, 2.1f, 0.8f);
        sb.Emit(holder, "Mesh", baseSlots);
        var signGo = BuildKit.Text(holder, "DIJUAL", new Vector3(0f, 1.55f, 0.1f), 180f, 0.04f, new Color(0.35f, 0.08f, 0.05f));
        var door = new GameObject("Pintu_" + l.id).transform;
        door.SetParent(tHouses, false);
        door.position = doorPt;
        door.rotation = Quaternion.Euler(0f, l.yaw, 0f);
        var hd = door.gameObject.AddComponent<HomeDoor>();
        hd.houseId = h.id; hd.radius = 3.6f; hd.prompt = "Rumah dijual: " + h.name;
        hd.sign = signGo.GetComponent<TextMesh>();
        hd.center = new Vector3(l.x, l.py, l.z) + Vector3.up * l.hz * 0.45f;
        hd.viewDist = Mathf.Max(l.w, l.d) * 1.15f + 7f;
        V2DoorLight(go.transform, l, dx);
    }

    // ------------------------------------------------------------------ kios pertama = Toko Bram
    static void V2Shop(V2.Lot l, GameObject go)
    {
        VMapMark("Toko Bram", new Vector2(l.x, l.z), 1);
        BoxCol(tProps, "KiosCol", V2Local(l, 0f, 0f, 1f), new Vector3(3.4f, 2f, 2.2f), new Vector3(0f, l.yaw, 0f));
        VPoint<GeneralShop>(tProps, "InteractPoint_Toko", V2Local(l, 0f, l.d * 0.5f + 2.3f), "Toko Bram (perlengkapan dasar)", 3.4f);
        var bram = VNpc("NPC_Pedagang", "Pedagang Bram", "Pedagang", "Pedagang", V2Local(l, 0f, -0.5f), false);
        bram.transform.rotation = Quaternion.Euler(0f, l.yaw, 0f);
        BuildKit.Text(go.transform, "TOKO BRAM", new Vector3(0f, 2.3f, l.d * 0.5f - 0.1f + 0.1f), 180f, 0.05f, new Color(0.2f, 0.1f, 0.05f));
        AddLight(go.transform, new Vector3(0f, 3.0f, 1.2f), 8f, 1.2f);
    }

    // ------------------------------------------------------------------ gereja di bukit + pendeta
    static void V2Chapel(V2.Lot l, GameObject go)
    {
        VMapMark("Gereja Desa", new Vector2(l.x, l.z), 3);
        float zf = l.d * 0.5f;
        BoxCol(go.transform, "MenaraCol", new Vector3(0f, 8f, 6.6f), new Vector3(5.6f, 16f, 5.6f), Vector3.zero);
        AddLight(go.transform, new Vector3(-2.6f, 3.2f, zf + 1.6f), 12f, 1.3f);
        AddLight(go.transform, new Vector3(2.6f, 3.2f, zf + 1.6f), 12f, 1.3f);
        Vector3 front = V2Local(l, 2.0f, zf + 2.4f);
        var priest = VNpc("NPC_Pendeta", "Bapa Elric", "Pendeta Desa", "Peziarah", front, false);
        priest.transform.rotation = Quaternion.Euler(0f, l.yaw + 160f, 0f);
        var ip = VAttach<InfoPoint>(priest, "Bicara: Bapa Elric", 3.6f);
        ip.title = "Bapa Elric";
        ip.message = "Selamat datang di rumah doa Desa Pemula, Nak.\n\n" +
            "Lonceng berbunyi tiga kali kalau ada Hunter yang gugur dan pulang dengan selamat. Bunyinya jarang, syukurlah.\n\n" +
            "Makam desa ada tepat di belakang gereja ini. Di sana beristirahat warga yang telah lebih dulu, dan satu-dua Hunter yang tak kembali dari hutan.\n\n" +
            "Hati-hati di Hutan Pemula, dan jangan terlalu bangga pada pedangmu.";
        var gp = VPoint<InfoPoint>(tHouses, "InteractPoint_Gereja", V2Local(l, -1.8f, zf + 4.2f), "Lihat Gereja Desa", 3.6f);
        gp.title = "Gereja Desa Pemula";
        gp.message = "GEREJA DESA PEMULA\nBerdiri di atas bukit kecil di barat laut desa, dibangun oleh para pendiri kampung di tepi Kali Embun. Terbuka bagi semua peziarah.";
    }

    // ------------------------------------------------------------------ lumbung besar
    static void V2Barn(V2.Lot l, GameObject go)
    {
        VMapMark("Lumbung & Kandang", new Vector2(l.x, l.z), 4);
        AddLight(go.transform, new Vector3(0f, 3.4f, l.d * 0.5f + 1.2f), 11f, 1.1f);
        var ip = VPoint<InfoPoint>(tProps, "InteractPoint_Lumbung", V2Local(l, 0f, l.d * 0.5f + 3.2f), "Lihat Lumbung & Kandang", 3.6f);
        ip.title = "Lumbung & Kandang Desa";
        ip.message = "Lumbung besar milik bersama warga kampung. Hasil panen disimpan di loteng, sapi dan domba merumput di padang sebelah.\n\nPeternak sering menitipkan pesan di papan dekat pintu: kadang ada yang butuh bantuan mengusir slime dari kebun.";
    }

    // ------------------------------------------------------------------ kincir air + roda berputar
    static void V2Mill(V2.Lot l, GameObject go)
    {
        VMapMark("Kincir Air", new Vector2(l.x, l.z), 4);
        var wheel = PropKit.Get("V2_MillWheel", V2Kit.MillWheel);
        Vector3 wp = V2Local(l, 5.2f, 0f, 0f); wp.y = l.py + 2.2f;
        var w = PropKit.Place(wheel, go.transform, "RodaKincir", wp, l.yaw, 1f, baseSlots);
        var sp = w.AddComponent<SpinObject>();
        sp.axis = Vector3.right; sp.degreesPerSecond = 24f;
        BoxCol(go.transform, "RodaCol", new Vector3(5.2f, 2.2f, 0f), new Vector3(1.2f, 4.0f, 4.0f), Vector3.zero);
        BoxCol(go.transform, "KonsolCol", new Vector3(5.4f, 0.4f, 0f), new Vector3(3.0f, 0.8f, 4.6f), Vector3.zero);
        AddLight(go.transform, new Vector3(-1.2f, 2.4f, l.d * 0.5f + 1.0f), 8f, 1.0f);
    }
}
