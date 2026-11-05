using System.Collections.Generic;
using UnityEngine;
using HunterGuild;

/// <summary>
/// KOMPLEK PROPERTI: satu kawasan khusus (halaman batu berpagar lengkung gerbang) yang berisi Kantor Properti di bagian belakang
/// dan semua rumah dijual berjajar rapi di sisi kiri-kanan menghadap halaman. Dibuat terpisah dari rumah-rumah warga di
/// Desa Pemula, Kota Ashwatch, dan Eldrath. Seluruh komplek ("KomplekProperti") dan tiap bangunan di dalamnya bisa digeser manual.
/// </summary>
public static partial class TownSceneBuilder
{
    /// <summary>Nama scene yang komplek propertinya sudah dibangun pada build ini (null = belum).</summary>
    static string complexScene;

    /// <summary>Koordinat lokal komplek (lx ke kanan, lz ke depan/arah gerbang) -> koordinat dunia.</summary>
    static Vector2 CLoc(Vector2 c, float yaw, float lx, float lz)
    {
        float a = yaw * Mathf.Deg2Rad, s = Mathf.Sin(a), co = Mathf.Cos(a);
        return new Vector2(c.x + lx * co + lz * s, c.y - lx * s + lz * co);
    }

    class CxLayout
    {
        public float halfCourt, L, halfW, W;
        public List<PropertyMenus.House> homes = new List<PropertyMenus.House>();
        public List<HouseKit.HouseDef> defs = new List<HouseKit.HouseDef>();
        public HouseKit.HouseDef office;
        public float[] z = new float[8];       // pusat z tiap rumah (lokal)
        public float[] x = new float[8];       // pusat x tiap rumah (lokal)
        public float[] yawOff = new float[8];  // yaw relatif komplek
        public bool t2;                        // Kota Ashwatch baru: lampu T2, palet sendiri, papan kantor berdiri bebas
        public Material[] officeSlots;
        public Material[][] homeSlots;
    }

    static CxLayout CxPlan(string townScene, bool compact)
    {
        var lay = new CxLayout();
        foreach (var h in PropertyMenus.Houses) if (h.town == townScene && lay.homes.Count < 8) lay.homes.Add(h);
        lay.office = VDef(5, 11);
        for (int i = 0; i < lay.homes.Count; i++) lay.defs.Add(VDef(HomeKind(lay.homes[i].id), i));
        float[] colLen = new float[2];
        float maxD = 0f;
        for (int i = 0; i < lay.homes.Count; i++) { colLen[i % 2] += lay.defs[i].w + 3.2f; maxD = Mathf.Max(maxD, lay.defs[i].d); }
        lay.halfCourt = compact ? Mathf.Max(8f, lay.office.w / 2f + 2f) : Mathf.Max(11f, lay.office.w / 2f + 3f);
        lay.L = Mathf.Max(Mathf.Max(44f, lay.office.d + 14f), Mathf.Max(colLen[0], colLen[1]) + 16f);
        lay.halfW = lay.halfCourt + maxD + 1.5f;
        lay.W = lay.halfW * 2f + 2f;
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
        return lay;
    }

    static bool CxVillageOk(Vector2 c, float yaw, float W, float L, out float baseY)
    {
        baseY = VGy(c.x, c.y);
        float lo = baseY, hi = baseY;
        for (float lx = -W / 2f; lx <= W / 2f + 0.01f; lx += 4f)
            for (float lz = -L / 2f; lz <= L / 2f + 0.01f; lz += 4f)
            {
                Vector2 p = CLoc(c, yaw, lx, lz);
                if (p.magnitude > VR - 7f) return false;
                float g = VGy(p.x, p.y);
                lo = Mathf.Min(lo, g); hi = Mathf.Max(hi, g);
                if (vStreetsActive && VNearStreet(p, 1.5f)) return false;
            }
        return hi - lo <= 1.6f && lo > -0.6f;
    }

    static bool PlacePropertyComplex(string townScene, Vector2 anchor)
    {
        try { return PlacePropertyComplexImpl(townScene, anchor); }
        catch (System.Exception e) { Debug.LogWarning("[Komplek Properti] gagal, memakai penempatan lama: " + e.Message); return false; }
    }

    static bool PlacePropertyComplexImpl(string townScene, Vector2 anchor)
    {
        if (HouseKit.Defs.Count == 0) HouseKit.BuildAll();
        bool eld = townScene == CityGate.DungeonCityScene, vil = townScene == StarterConfig.VillageScene, town = townScene == GuildConfig.TownScene;
        float rMin = vil ? 36f : town ? 100f : 105f, rMax = vil ? 72f : town ? 215f : 235f;

        CxLayout lay = null; Vector2 C = Vector2.zero; float Y = 0f, baseY = 0f; bool found = false;
        for (int pass = 0; pass < 2 && !found; pass++)
        {
            lay = CxPlan(townScene, pass == 1);
            var cand = new List<Vector2>();
            for (float r = rMin; r <= rMax; r += pass == 0 ? 4f : 3f)
                for (float a = 0f; a < 360f; a += pass == 0 ? 4f : 3f)
                    cand.Add(new Vector2(Mathf.Sin(a * Mathf.Deg2Rad), Mathf.Cos(a * Mathf.Deg2Rad)) * r);
            cand.Sort((p, q) => (p - anchor).sqrMagnitude.CompareTo((q - anchor).sqrMagnitude));
            foreach (var p in cand)
            {
                float yaw = YawToward(p, Vector2.zero);            // gerbang komplek menghadap pusat kota
                float by = 0f;
                if (vil && !CxVillageOk(p, yaw, lay.W, lay.L, out by)) continue;
                if (!BoxFree(p.x, p.y, lay.W + 8f, lay.L + 8f, yaw, pass == 0 ? 2.5f : 1.5f)) continue;       // jarak lega ke bangunan lain (tidak bertumpuk)
                C = p; Y = yaw; baseY = vil ? by : 0f; found = true; break;
            }
        }
        if (!found) { Debug.LogWarning("[Komplek Properti] tidak ada lahan cukup di " + townScene + "; memakai penempatan rumah satu per satu."); return false; }

        BuildComplexAt(townScene, lay, C, Y, baseY, vil);
        return true;
    }

    /// <summary>Bangun komplek di tempat yang sudah ditentukan: hasil pencarian (kota/desa lama) atau lahan tetap Kota Ashwatch baru (lay.t2).</summary>
    static void BuildComplexAt(string townScene, CxLayout lay, Vector2 C, float Y, float baseY, bool vil)
    {
        float L = lay.L, hc = lay.halfCourt;
        ReserveBox(C.x, C.y, lay.W, L, Y, 0f);
        Vector2 gate = CLoc(C, Y, 0f, L / 2f + 5f);
        Reserve(gate.x, gate.y, 5f);

        var cont = new GameObject("KomplekProperti").transform;
        cont.SetParent(tHouses, false);
        cont.position = V3(C, baseY);
        cont.rotation = Quaternion.Euler(0f, Y, 0f);

        // ---------------- halaman batu, jalur tengah, cincin patung, lengkung gerbang (satu mesh, dunia -> anak komplek)
        var g = new GeoBag();
        float gy = baseY + 0.05f;
        Vector2 fw2 = new Vector2(Mathf.Sin(Y * Mathf.Deg2Rad), Mathf.Cos(Y * Mathf.Deg2Rad));
        float ex = hc + 1.2f;
        g.Patch(Sl.Cobble, CLoc(C, Y, -ex, -L / 2f + 0.3f), CLoc(C, Y, -ex, L / 2f + 3f), CLoc(C, Y, ex, L / 2f + 3f), CLoc(C, Y, ex, -L / 2f + 0.3f), gy, fw2);
        float pathBack = -L / 2f + lay.office.d + 3f;
        g.Patch(Sl.Pave, CLoc(C, Y, -1.7f, pathBack), CLoc(C, Y, -1.7f, L / 2f + 3f), CLoc(C, Y, 1.7f, L / 2f + 3f), CLoc(C, Y, 1.7f, pathBack), gy + 0.025f, fw2);
        Vector2 mid = CLoc(C, Y, 0f, 1f);
        g.Annulus(Sl.Pave, new Vector3(mid.x, 0f, mid.y), 3.0f, 4.2f, 0f, 360f, 36, gy + 0.03f);
        g.Disc(Sl.Stone, new Vector3(mid.x, 0f, mid.y), 1.9f, 24, gy + 0.18f);
        // pinggiran batu di depan (kiri-kanan lengkung)
        foreach (float sg in new[] { -1f, 1f })
        {
            float len = ex - 6.6f;
            if (len > 0.6f)
            {
                Vector2 cp = CLoc(C, Y, sg * (6.6f + len / 2f), L / 2f + 2.6f);
                g.Box(Sl.Stone, new Vector3(cp.x, baseY + 0.14f, cp.y), new Vector3(len, 0.28f, 0.55f), Y);
            }
        }
        // lengkung gerbang batu + atap kayu
        float gz = L / 2f + 0.5f;
        foreach (float sg in new[] { -1f, 1f })
        {
            Vector2 pp = CLoc(C, Y, sg * 5.4f, gz);
            g.Box(Sl.Stone, new Vector3(pp.x, baseY + 3.1f, pp.y), new Vector3(1.5f, 6.2f, 1.5f), Y);
            g.Box(Sl.Pave, new Vector3(pp.x, baseY + 0.25f, pp.y), new Vector3(2.0f, 0.5f, 2.0f), Y);
            g.Box(Sl.Pave, new Vector3(pp.x, baseY + 6.3f, pp.y), new Vector3(1.9f, 0.3f, 1.9f), Y);
            g.Ball(Sl.Gold, new Vector3(pp.x, baseY + 6.75f, pp.y), 0.28f, 3, 6);
        }
        Vector2 gc = CLoc(C, Y, 0f, gz);
        g.Box(Sl.Wood, new Vector3(gc.x, baseY + 5.65f, gc.y), new Vector3(12.4f, 1.3f, 1.1f), Y);
        g.Box(Sl.WoodLight, new Vector3(gc.x, baseY + 6.4f, gc.y), new Vector3(13.2f, 0.3f, 1.7f), Y);
        var mesh = g.Emit(tHouses, "Halaman_Komplek", baseSlots, MeshDir + "/Komplek_" + townScene + ".asset");
        if (mesh != null) mesh.transform.SetParent(cont, true);

        // tulisan di lengkung (terbaca dari luar dan dari dalam) -- koordinat lokal komplek
        var gold = new Color(1f, 0.86f, 0.42f);
        BuildKit.Text(cont, "KOMPLEK PROPERTI", new Vector3(0f, 5.85f, gz + 0.62f), 180f, 0.15f, gold);
        BuildKit.Text(cont, "Jual-beli rumah & furnitur", new Vector3(0f, 5.25f, gz + 0.62f), 180f, 0.07f, new Color(0.95f, 0.9f, 0.75f));
        BuildKit.Text(cont, "KOMPLEK PROPERTI", new Vector3(0f, 5.85f, gz - 0.62f), 0f, 0.15f, gold);
        foreach (float sg in new[] { -1f, 1f })
            BoxCol(cont, "TiangGerbangCol", new Vector3(sg * 5.4f, 3.1f, gz), new Vector3(1.5f, 6.2f, 1.5f), Vector3.zero);

        // ---------------- perabot halaman: patung, lampu, bangku, pohon hias
        var statue = PropKit.Get("PC_Patung", b => PropKit.KnightStatue(b, Vector3.zero, 0f));
        var stGo = PropKit.Place(statue, cont, "PatungKomplek", V3(mid, baseY + 0.2f), Y + 180f, 1f, baseSlots);
        var scol = stGo.AddComponent<CapsuleCollider>();
        scol.center = new Vector3(0f, 1.5f, 0f); scol.radius = 1.3f; scol.height = 3f;
        var lamp = lay.t2 ? PropKit.Get("T2_Lamp", T2Kit.Lamp) : PropKit.LampProto();
        float[] lz = { L / 2f - 4f, 8f, -4f };
        int li = 0;
        foreach (float z0 in lz)
            foreach (float sg in new[] { -1f, 1f })
            {
                Vector2 lp = CLoc(C, Y, sg * 3.4f, z0);
                var lg = PropKit.Place(lamp, cont, "Lampu", V3(lp, baseY), sg < 0 ? Y : Y + 180f, 1f, baseSlots);
                if (li++ % 2 == 0) AddLight(lg.transform, new Vector3(0.7f, 3.3f, 0f), 15f, 1.6f);
            }
        var bench = PropKit.Get("PC_Bangku", PropKit.Bench);
        foreach (float sg in new[] { -1f, 1f })
        {
            Vector2 bp = CLoc(C, Y, sg * 6.4f, 5f);
            PropKit.Place(bench, cont, "Bangku", V3(bp, baseY), Y + (sg < 0 ? 90f : -90f), 1f, baseSlots);
        }
        var tree = PropKit.TreeProto(2);
        foreach (float sg in new[] { -1f, 1f })
        {
            Vector2 tp = CLoc(C, Y, sg * (hc - 1.8f), L / 2f - 6f);
            var tg = PropKit.Place(tree, cont, "PohonHias", V3(tp, baseY), sg < 0 ? 20f : 200f, 0.9f, baseSlots);
            var cc = tg.AddComponent<CapsuleCollider>();
            cc.center = new Vector3(0f, 1.5f, 0f); cc.radius = 0.45f; cc.height = 3f;
        }

        // ---------------- bangunan: kantor di belakang, rumah dijual berjajar kiri-kanan menghadap halaman
        Vector2 op = CLoc(C, Y, 0f, -L / 2f + lay.office.d / 2f + 1.5f);
        if (vil) VMapMark("Komplek Properti", C, 1);
        BuildOfficeAt(townScene, lay.office, op, Y, cont, baseY, lay.officeSlots, lay.t2);
        for (int i = 0; i < lay.homes.Count; i++)
        {
            Vector2 hp = CLoc(C, Y, lay.x[i], lay.z[i]);
            BuildHomeAt(townScene, lay.homes[i], lay.defs[i], hp, Y + lay.yawOff[i], i + 1, cont, baseY, lay.defs[i].w / 2f + 1.6f, lay.homeSlots != null ? lay.homeSlots[i] : null);
        }
        complexScene = townScene;
        Debug.Log("[Komplek Properti] " + townScene + ": " + lay.homes.Count + " rumah + kantor di (" + C.x.ToString("0") + ", " + C.y.ToString("0") + "), ukuran " + lay.W.ToString("0") + "x" + L.ToString("0") + ".");
    }
}
