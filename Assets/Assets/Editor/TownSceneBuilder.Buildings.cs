using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HunterGuild;

public static partial class TownSceneBuilder
{
    // =====================================================================
    // Bangunan penting di keempat alun-alun (distrik ekonomi, di dalam Tembok Dalam)
    // =====================================================================
    internal static Vector2 AroundPlaza(int i, float worldAz, float halfDepth, out float yaw)
    {
        Vector2 c = PlazaCenter(i);
        Vector2 dir = new Vector2(Mathf.Sin(worldAz * Mathf.Deg2Rad), Mathf.Cos(worldAz * Mathf.Deg2Rad));
        Vector2 p = c + dir * (PlazaR + 4f + halfDepth);
        yaw = YawToward(p, c);
        return p;
    }

    static void BuildPlazaBuildings()
    {
        float yaw;
        Vector2 p;

        // ---- Alun-alun Guild (selatan): Guild di timur, Penginapan di barat
        p = AroundPlaza(0, 90f, LandmarkKit.GuildD / 2f, out yaw);
        var guild = Landmark<GuildHall>("GuildHall", tBuildings, p, yaw, LandmarkKit.Guild, TownMats.Slots(),
            LandmarkKit.GuildW, LandmarkKit.GuildD, 20f, LandmarkKit.GuildPorch + 1.5f, "Guild Petualang");
        Sign(guild.transform.parent, "GUILD PETUALANG", new Vector3(0f, 8.2f, LandmarkKit.GuildD / 2f + 1.0f), 0.35f);

        p = AroundPlaza(0, 270f, LandmarkKit.InnD / 2f, out yaw);
        var inn = Landmark<Inn>("Inn", tBuildings, p, yaw, LandmarkKit.Inn,
            TownMats.Slots(TownMats.Plaster[2], TownMats.Roofs[1], null, null, TownMats.Cloths[3], TownMats.Cloths[4]),
            LandmarkKit.InnW, LandmarkKit.InnD, 14f, 3.5f, "Penginapan");
        Sign(inn.transform.parent, "PENGINAPAN", new Vector3(0f, 6.4f, LandmarkKit.InnD / 2f + 1.0f), 0.3f);
        Door(inn, "Inn");

        // ---- Alun-alun Pasar (timur): Pandai Besi di utara, Toko Umum di selatan
        p = AroundPlaza(1, 0f, LandmarkKit.SmithD / 2f, out yaw);
        var smith = Landmark<Blacksmith>("Blacksmith", tBuildings, p, yaw, LandmarkKit.Smithy,
            TownMats.Slots(TownMats.Plaster[4], TownMats.Roofs[3]), LandmarkKit.SmithW, LandmarkKit.SmithD, 13f, 3.5f, "Pandai Besi");
        Sign(smith.transform.parent, "PANDAI BESI", new Vector3(0f, 6.2f, LandmarkKit.SmithD / 2f + 1.0f), 0.3f);
        Door(smith, "Smithy");

        p = AroundPlaza(1, 180f, LandmarkKit.ShopD / 2f, out yaw);
        var shop = Landmark<GeneralShop>("GeneralShop", tBuildings, p, yaw, LandmarkKit.Shop,
            TownMats.Slots(TownMats.Plaster[1], TownMats.Roofs[0], null, null, TownMats.Cloths[2], TownMats.Cloths[5]),
            LandmarkKit.ShopW, LandmarkKit.ShopD, 13f, 3.5f, "Toko Umum");
        Sign(shop.transform.parent, "TOKO UMUM", new Vector3(0f, 6.2f, LandmarkKit.ShopD / 2f + 1.0f), 0.3f);
        Door(shop, "Shop");

        // ---- Alun-alun Akademi (utara): Perpustakaan di timur, Menara Penyihir di barat
        p = AroundPlaza(2, 90f, LandmarkKit.AcadD / 2f, out yaw);
        var lib = Landmark<Library>("Library", tBuildings, p, yaw, LandmarkKit.Academy,
            TownMats.Slots(TownMats.Plaster[0], TownMats.Roofs[2]), LandmarkKit.AcadW, LandmarkKit.AcadD, 18f, 3.5f, "Akademi & Perpustakaan");
        Sign(lib.transform.parent, "AKADEMI", new Vector3(0f, 7.6f, LandmarkKit.AcadD / 2f + 1.0f), 0.3f);
        Door(lib, "Academy");

        p = AroundPlaza(2, 270f, LandmarkKit.MageR, out yaw);
        var mage = Landmark<InfoPoint>("MageTower", tBuildings, p, yaw, LandmarkKit.MageTower,
            TownMats.Slots(TownMats.Plaster[5], TownMats.Roofs[2]), LandmarkKit.MageR * 2f, LandmarkKit.MageR * 2f, 36f, 3f, "Menara Penyihir");
        mage.title = "Menara Penyihir";
        mage.message = "Menara para penyihir kota. Pintunya terkunci dengan segel sihir.\n\n(Akan dibuka pada pembaruan berikutnya: sihir, enchant, dan tugas penelitian.)";

        // ---- Alun-alun Katedral (barat): Katedral di utara, Restoran di selatan
        Vector2 cp = new Vector2(-150f, 64f);
        yaw = 180f;
        var cath = Landmark<InfoPoint>("Cathedral", tBuildings, cp, yaw, LandmarkKit.Cathedral,
            TownMats.Slots(), LandmarkKit.CathW, LandmarkKit.CathD, 34f, 4f, "Katedral");
        cath.title = "Katedral Cahaya Suci";
        cath.message = "Katedral agung Ashwatch. Lonceng berbunyi tiap fajar dan senja.\n\n(Akan dibuka pada pembaruan berikutnya: berkat, penyembuhan kutukan, dan pernikahan.)";
        Door(cath, "Cathedral");

        p = AroundPlaza(3, 180f, LandmarkKit.TavernD / 2f, out yaw);
        var rest = Landmark<Restaurant>("Restaurant", tBuildings, p, yaw, LandmarkKit.Tavern,
            TownMats.Slots(TownMats.Plaster[3], TownMats.Roofs[4], null, null, TownMats.Cloths[0], TownMats.Cloths[3]),
            LandmarkKit.TavernW, LandmarkKit.TavernD, 13f, 3.5f, "Restoran");
        Sign(rest.transform.parent, "RESTORAN", new Vector3(0f, 6.2f, LandmarkKit.TavernD / 2f + 1.0f), 0.3f);
        Door(rest, "Tavern");

        // ---- Barak (barat daya) dan Istal (tenggara) di tepi distrik
        p = WallKit.Out(225f) * 144f;
        var barr = Landmark<InfoPoint>("Barracks", tBuildings, p, YawToward(p, Vector2.zero), LandmarkKit.Barracks,
            TownMats.Slots(), LandmarkKit.BarrW, LandmarkKit.BarrD, 13f, 3.5f, "Barak Ksatria");
        barr.title = "Barak Ksatria";
        barr.message = "Markas penjaga kota. Para ksatria berlatih di halaman depan.";
        p = WallKit.Out(135f) * 144f;
        var stab = Landmark<InfoPoint>("Stables", tBuildings, p, YawToward(p, Vector2.zero), LandmarkKit.Stables,
            TownMats.Slots(), LandmarkKit.StableW, LandmarkKit.StableD, 11f, 3.5f, "Istal");
        stab.title = "Istal Kota";
        stab.message = "Kuda perang dan kuda tunggang disimpan di sini.";
    }

    // =====================================================================
    // Distrik bangsawan (di dalam Tembok Tengah)
    // =====================================================================
    static readonly string[] NobleFamilies =
    {
        "Kediaman Keluarga Valeheart", "Kediaman Keluarga Arkwright", "Kediaman Keluarga Lunaris", "Kediaman Keluarga Drachenfels",
        "Kediaman Keluarga Silverbrook", "Kediaman Keluarga Rosenthal", "Kediaman Keluarga Ashcroft", "Kediaman Keluarga Eisenhart"
    };

    static void BuildNobleBuildings()
    {
        var castle = Landmark<InfoPoint>("Castle", tNoble, Vector2.zero, 180f, LandmarkKit.Castle,
            TownMats.Slots(), LandmarkKit2CastleW, LandmarkKit2CastleD, 46f, 9f, "Kastil Penguasa");
        castle.title = "Kastil Penguasa Perbatasan";
        castle.message = "Kediaman Lord penguasa Ashwatch. Penjaga elit berdiri di setiap gerbang.\n\nRakyat jelata tidak diizinkan masuk tanpa surat undangan.";

        for (int k = 0; k < 8; k++)
        {
            float az = 22.5f + k * 45f;
            Vector2 p = WallKit.Out(az) * 82f;
            int variant = k % 4;
            var m = Landmark<InfoPoint>("Manor_" + (k + 1), tNoble, p, az + 180f, b => LandmarkKit.Manor(b, variant),
                TownMats.Slots(TownMats.Plaster[(k * 3) % 7], TownMats.Roofs[(k * 2 + 1) % 6], null, null, TownMats.Cloths[k % 6], TownMats.Cloths[(k + 3) % 6]),
                LandmarkKit.ManorW, LandmarkKit.ManorD, 15f, 4f, NobleFamilies[k]);
            m.title = NobleFamilies[k];
            m.message = "Rumah bangsawan. Gerbang taman terkunci. Seorang pelayan mengintip dari balik tirai.";
        }
    }

    const float LandmarkKit2CastleW = 76f, LandmarkKit2CastleD = 64f;

    // =====================================================================
    // Rumah warga
    // =====================================================================
    static float RadiusOf(float x, float z) { return Mathf.Sqrt(x * x + z * z); }

    static bool CornersInBand(float cx, float cz, float w, float d, float yaw, float r0, float r1)
    {
        float s = Mathf.Sin(yaw * Mathf.Deg2Rad), c = Mathf.Cos(yaw * Mathf.Deg2Rad);
        for (int i = 0; i < 4; i++)
        {
            float lx = (i & 1) == 0 ? -w / 2f : w / 2f, lz = (i & 2) == 0 ? -d / 2f : d / 2f;
            float wx = cx + lx * c + lz * s, wz = cz - lx * s + lz * c;
            float r = RadiusOf(wx, wz);
            if (r < r0 || r > r1) return false;
        }
        return true;
    }

    static int dbgTry, dbgBand, dbgBlocked, dbgPlaced;

    static void PointAlong(List<Vector2> pts, float[] cum, float s, out Vector2 p, out Vector2 tan)
    {
        int i = 0;
        while (i + 2 < cum.Length && cum[i + 1] < s) i++;
        float seg = Mathf.Max(0.0001f, cum[i + 1] - cum[i]);
        float t = Mathf.Clamp01((s - cum[i]) / seg);
        p = Vector2.Lerp(pts[i], pts[i + 1], t);
        // garis singgung dirata-ratakan agar rumah tidak bergoyang pada jalan lengkung
        int j0 = Mathf.Max(0, i - 2), j1 = Mathf.Min(pts.Count - 1, i + 3);
        tan = (pts[j1] - pts[j0]).normalized;
    }

    static void BuildHouses()
    {
        HouseKit.BuildAll();
        var rng = new System.Random(7);
        int rowCycle = 0;
        foreach (var st in streets)
        {
            if (!st.houses || st.pts.Count < 2) continue;
            var cum = new float[st.pts.Count];
            for (int i = 1; i < cum.Length; i++) cum[i] = cum[i - 1] + (st.pts[i] - st.pts[i - 1]).magnitude;
            float total = cum[cum.Length - 1];
            for (int side = -1; side <= 1; side += 2)
            {
                if (st.sides == -1 && side == -1) continue;
                float sPos = 1f;
                int guard = 0;
                while (sPos < total - 2f && guard++ < 400)
                {
                    var def = HouseKit.Defs[(rowCycle++ * 2 + (side > 0 ? 1 : 0)) % HouseKit.Defs.Count];       // jenis rumah berpola teratur (bukan acak)
                    var fp = Footprint(def);
                    float fw = fp[0] * 2f;
                    float mid = sPos + fw / 2f;
                    if (mid > total) break;
                    Vector2 along, tan;
                    PointAlong(st.pts, cum, mid, out along, out tan);
                    Vector2 nrm = new Vector2(tan.y, -tan.x) * side;
                    Vector2 c = along + nrm * (st.w / 2f + 1.6f + fp[2]);
                    float yaw = YawToward(c, along);
                    Vector2 fc = FootCenter(c, yaw, fp);
                    dbgTry++;
                    bool inb = CornersInBand(fc.x, fc.y, fw, fp[2] - fp[1], yaw, 107f, 203f);
                    if (!inb) { dbgBand++; sPos += 3f; continue; }
                    if (!BoxFree(fc.x, fc.y, fw, fp[2] - fp[1], yaw, 0f)) { dbgBlocked++; sPos += 3f; continue; }
                    PlaceHouse(def, c, yaw, rng);
                    ReserveBox(fc.x, fc.y, fw, fp[2] - fp[1], yaw, 0f);
                    dbgPlaced++;
                    sPos += fw + 1.6f;                       // jarak antar-rumah tetap, deret rapi
                }
            }
        }
    }

    public static string HouseDebug() { return "placed=" + dbgPlaced + " try=" + dbgTry + " outOfBand=" + dbgBand + " blocked=" + dbgBlocked + " streets=" + streets.Count; }

    static void PlaceHouse(HouseKit.HouseDef def, Vector2 c, float yaw, System.Random rng)
    {
        int pi = rng.Next(TownMats.Plaster.Length), ri = rng.Next(TownMats.Roofs.Length);
        var slots = TownMats.Slots(TownMats.Plaster[pi], TownMats.Roofs[ri], TownMats.Plaster[(pi + 3) % TownMats.Plaster.Length],
            TownMats.Roofs[(ri + 2) % TownMats.Roofs.Length], TownMats.Cloths[rng.Next(6)], TownMats.Cloths[rng.Next(6)]);
        if (def.kind == 3) slots[Sl.Roof] = TownMats.Thatch;
        var go = PropKit.Place(def.proto, tHouses, "Rumah_" + (++houseCount), V3(c), yaw, 1f, slots);
        var bc = go.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, def.height / 2f, 0f);
        bc.size = new Vector3(def.w, def.height, def.d);
    }

    // =====================================================================
    // Taman bangsawan
    // =====================================================================
    static void BuildNobleGardens()
    {
        var t = Group("Taman", tNoble);
        int placed = 0;
        for (int i = 0; i < 1500 && placed < 130; i++)
        {
            float a = Random.Range(0f, 360f), r = Random.Range(38f, 92f);
            Vector2 p = WallKit.Out(a) * r;
            if (!IsFree(p.x, p.y, 3.2f)) continue;
            Reserve(p.x, p.y, 3.2f);
            int kind = Random.value < 0.55f ? 2 : (Random.value < 0.5f ? 3 : 5);
            var go = PropKit.Place(PropKit.TreeProto(kind), t, "Pohon", V3(p), Random.Range(0f, 360f), Random.Range(0.85f, 1.2f), baseSlots);
            var cc = go.AddComponent<CapsuleCollider>();
            cc.center = new Vector3(0f, 1.5f, 0f); cc.radius = 0.5f; cc.height = 3f;
            placed++;
        }
        // semak/pagar hidup di sisi dalam Tembok Tengah
        for (float a = 0f; a < 360f; a += 4f)
        {
            Vector2 p = WallKit.Out(a) * 91f;
            if (!IsFree(p.x, p.y, 1.5f)) continue;
            PropKit.Place(PropKit.TreeProto(4), t, "Semak", V3(p), a, 1f, baseSlots);
        }
        // patung ksatria di persimpangan promenade
        for (int k = 0; k < 4; k++)
        {
            float a = 45f + k * 90f;
            Vector2 p = WallKit.Out(a) * 56f;
            if (!IsFree(p.x, p.y, 1.5f)) continue;
            PropKit.Place(PropKit.Get("StatueK", bg => PropKit.KnightStatue(bg, Vector3.zero, 0f)), t, "Patung", V3(p), a + 180f, 0.8f, baseSlots);
            Reserve(p.x, p.y, 2f);
        }
    }
}
