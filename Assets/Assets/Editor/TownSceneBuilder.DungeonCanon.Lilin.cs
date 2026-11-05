using System.Collections.Generic;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Jalan Pengrajin Lilin (kanon §8.6: "Mulut pertama terbuka di Jalan Pengrajin Lilin, dan yang pertama menjerit seekor anjing kuning kurus
/// di depan toko roti. Batu jalan retak seperti kelopak membuka, enam lengkung ke luar"). Dibangun di busur barat daya Lingkar98 (az 207-245):
/// satu deret enam toko sempit di jalur kosong antara alun-alun dan jalan (toko roti di tengah, lima pengrajin lilin), dua gapura bertanda di
/// ujung busur, dan di jalan di depan pintu toko roti sebuah retakan TERTUTUP berbentuk enam kelopak, dilingkari tali merah dengan enam lilin
/// di ujung kelopaknya. Seekor anjing kuning kurus duduk di anak tangga toko roti menatap retakan itu (.Anjing.cs). Retakan sengaja tertutup:
/// mulut yang membuka adalah peristiwa cerita, bukan keadaan tetap.
/// Jarak: pejalan kaki Lingkar98 berjalan di sepanjang tali busur r ~ 97,0-98 (RoutePts), jadi tepi luar lingkar tali (r 95,85) dan seluruh
/// isi toko (r <= 91,2) dijaga tetap di dalamnya. Lampu kristal Lingkar98 yang kebetulan jatuh di dalam lingkar tali dicabut (EcClearLamps).
/// </summary>
public static partial class TownSceneBuilder
{
    const float LlRowR = 87.0f, LlW = 6.8f, LlD = 5.6f;                       // jari-jari pusat deret (84,2-89,8), lebar dan dalam toko
    const float LlCrackAz = 225.0f, LlCrackR = 93.9f;                         // pusat retakan: tepat di depan pintu toko roti
    const float LlCrackPost = 1.95f;                                          // jari-jari lingkar tali (tepi luar 95,85)
    const int LlBakery = 2;                                                   // indeks toko roti
    static readonly float[] LlAz = { 213.0f, 218.26f, 223.52f, 228.78f, 234.04f, 239.3f };
    static readonly string[] LlSign = { "LILIN MADU", "PENGRAJIN LILIN", "TOKO ROTI", "SUMBU DAN LILIN", "LILIN DOA", "PENGRAJIN LILIN" };
    const float LlDoorX = 2.3f;                                               // x lokal pintu toko roti

    /// <summary>Titik lokal toko -> dunia (akar diputar sebesar azimut; x lokal = arah singgung, z lokal = keluar dari alun-alun).</summary>
    static Vector3 LlWorld(int shop, float lx, float ly, float lz)
    {
        float az = LlAz[shop] * Mathf.Deg2Rad;
        Vector2 p = AtAz(LlAz[shop], LlRowR);
        return new Vector3(p.x + lx * Mathf.Cos(az) + lz * Mathf.Sin(az), ly, p.y - lx * Mathf.Sin(az) + lz * Mathf.Cos(az));
    }

    /// <summary>Yaw TextMesh agar teks terbaca oleh orang yang berdiri di arah f (bidang x,z) dari teks.</summary>
    static float EcFaceYaw(Vector2 f) { return Mathf.Atan2(-f.x, -f.y) * Mathf.Rad2Deg; }

    /// <summary>Titik di sekitar pusat retakan pada kerangka jalan: u searah singgung (azimut membesar), v keluar dari pusat kota.</summary>
    static Vector2 EcLp(Vector2 c, Vector2 tn, Vector2 ov, float u, float v) { return c + tn * u + ov * v; }

    static void EcLilin(Transform root)
    {
        var lane = Group("JalanPengrajinLilin", root);
        var slots = EcWarmSlots();
        slots[Sl.Fur] = UMat("EldAnjingKuning", new Color(0.86f, 0.68f, 0.30f, 1f));
        slots[Sl.Lip] = UMat("EldTaliMerah", new Color(0.62f, 0.10f, 0.09f, 1f));
        var win = UMat("EldJendelaToko", new Color(0.95f, 0.78f, 0.45f, 1f));
        slots[Sl.Cobble] = TownMats.Emissive(win, new Color(0.55f, 0.37f, 0.17f, 1f));          // kaca toko yang menyala hangat (redup, bukan silau)

        EcClearLamps(AtAz(LlCrackAz, LlCrackR), LlCrackPost + 1.1f);
        for (int i = 0; i < LlAz.Length; i++) EcShop(i, slots);
        EcLaneGates(lane, slots);
        EcCrack(lane, slots);
    }

    /// <summary>Cabut lampu kristal (grup LampuKristal) yang jaraknya kurang dari rad dari titik c.</summary>
    static void EcClearLamps(Vector2 c, float rad)
    {
        if (tProps == null) return;
        var lamps = tProps.Find("LampuKristal");
        if (lamps == null) return;
        for (int i = lamps.childCount - 1; i >= 0; i--)
        {
            Transform t = lamps.GetChild(i);
            float dx = t.position.x - c.x, dz = t.position.z - c.y;
            if (dx * dx + dz * dz < rad * rad) Object.DestroyImmediate(t.gameObject);
        }
    }

    // ================================================================== toko
    static void EcShop(int i, Material[] slots)
    {
        bool bakery = i == LlBakery;
        Vector2 p = AtAz(LlAz[i], LlRowR);
        var shop = new GameObject("D_Lilin_" + (i + 1)).transform;
        shop.SetParent(tBuildings, false);
        shop.position = new Vector3(p.x, 0f, p.y);
        shop.rotation = Quaternion.Euler(0f, LlAz[i], 0f);                    // +Z lokal = keluar, ke arah jalan
        var model = new GameObject("Model").transform;
        model.SetParent(shop, false);
        var b = new GeoBag();
        EcShopBody(b, i, bakery);
        b.Emit(model, "Mesh", slots);
        float hd = LlD / 2f;
        Color cream = new Color(0.95f, 0.9f, 0.74f, 1f);
        BuildKit.Text(model, LlSign[i], new Vector3(0f, 3.55f, hd + 0.52f), 180f, 0.07f, cream);
        BoxCol(model, "Col", new Vector3(0f, 3.5f, 0f), new Vector3(LlW, 7f, LlD), Vector3.zero);
        EcLight(shop, "CahayaToko", LlWorld(i, 0f, 2.6f, hd + 1.1f), 9f, 0.95f, new Color(1f, 0.82f, 0.55f, 1f), true);
        ReserveBox(p.x, p.y, LlW + 1f, LlD + 1f, LlAz[i], 0f);
        if (bakery) EcBakeryInfo(shop, i);
    }

    static void EcShopBody(GeoBag b, int i, bool bakery)
    {
        float hw = LlW / 2f, hd = LlD / 2f, fz = hd + 0.35f;                    // fz = bidang depan lantai atas (menjorok 0,35 m)
        int wall = (i % 2 == 0) ? Sl.Plaster : Sl.PlasterB;
        // dasar batu, lantai bawah, lantai atas yang menjorok (alasnya digambar supaya kolong menjorok tidak tembus pandang)
        b.BoxB(Sl.DarkStone, Vector3.zero, new Vector3(LlW + 0.3f, 0.45f, LlD + 0.3f));
        b.BoxB(wall, new Vector3(0f, 0.45f, 0f), new Vector3(LlW, 2.85f, LlD));
        b.Box(wall, new Vector3(0f, 4.45f, 0.175f), new Vector3(LlW + 0.1f, 2.3f, LlD + 0.35f), 0f, true, true);
        // rangka kayu: balok lantai, tiang sudut, tiang tengah
        b.Box(Sl.Wood, new Vector3(0f, 3.22f, hd + 0.17f), new Vector3(LlW + 0.14f, 0.22f, 0.3f));
        foreach (float sx in new[] { -1f, 1f })
        {
            b.BoxB(Sl.Wood, new Vector3(sx * (hw - 0.1f), 0.45f, hd + 0.02f), new Vector3(0.2f, 2.85f, 0.2f));
            b.BoxB(Sl.Wood, new Vector3(sx * (hw + 0.02f), 3.3f, fz - 0.02f), new Vector3(0.18f, 2.3f, 0.18f));
            b.BoxB(Sl.Wood, new Vector3(sx * 0.9f, 3.3f, fz + 0.01f), new Vector3(0.1f, 2.3f, 0.1f));
        }
        // jendela toko (kaca menyala hangat, Sl.Cobble) dan pintu
        b.Box(Sl.Wood, new Vector3(-1.0f, 1.95f, hd + 0.04f), new Vector3(3.3f, 1.5f, 0.06f));
        b.Box(Sl.Cobble, new Vector3(-1.0f, 1.95f, hd + 0.08f), new Vector3(3.0f, 1.2f, 0.05f));
        b.Box(Sl.Wood, new Vector3(-1.0f, 1.95f, hd + 0.11f), new Vector3(0.07f, 1.2f, 0.04f));
        b.Box(Sl.Wood, new Vector3(LlDoorX, 1.55f, hd + 0.04f), new Vector3(1.15f, 2.2f, 0.1f));
        b.Box(Sl.Stone, new Vector3(LlDoorX, 2.72f, hd + 0.06f), new Vector3(1.5f, 0.16f, 0.14f));
        b.BoxB(Sl.Stone, new Vector3(LlDoorX, 0f, hd + 0.4f), new Vector3(1.5f, 0.22f, 0.8f));
        // kanopi bergaris di atas jendela
        for (int k = 0; k < 4; k++)
        {
            float x0 = -2.7f + k * 0.85f, x1 = x0 + 0.85f;
            Vector3 a = new Vector3(x0, 2.95f, hd + 0.06f), c = new Vector3(x1, 2.95f, hd + 0.06f);
            Vector3 d = new Vector3(x0, 2.5f, hd + 0.62f), e = new Vector3(x1, 2.5f, hd + 0.62f);
            b.QuadBoth((k & 1) == 0 ? Sl.Cloth : Sl.ClothB, a, c, e, d);
        }
        foreach (float x in new[] { -2.7f, 0.7f })
            b.Beam(Sl.Wood, new Vector3(x, 2.5f, hd + 0.62f), new Vector3(x, 0.45f, hd + 0.62f), 0.05f, 0.05f, Vector3.forward);
        // jendela lantai atas dengan daun jendela
        foreach (float sx in new[] { -1.8f, 1.8f })
        {
            b.Box(Sl.Wood, new Vector3(sx, 4.6f, fz + 0.03f), new Vector3(1.1f, 1.1f, 0.06f));
            b.Box(Sl.Glass, new Vector3(sx, 4.6f, fz + 0.07f), new Vector3(0.85f, 0.85f, 0.05f));
            b.Box(Sl.Wood, new Vector3(sx - 0.62f, 4.6f, fz + 0.06f), new Vector3(0.3f, 1.0f, 0.05f));
            b.Box(Sl.Wood, new Vector3(sx + 0.62f, 4.6f, fz + 0.06f), new Vector3(0.3f, 1.0f, 0.05f));
        }
        // papan nama di fasad lantai atas
        b.Box(Sl.Dark, new Vector3(0f, 3.55f, fz + 0.1f), new Vector3(4.8f, 0.55f, 0.08f));
        b.Box(Sl.Gold, new Vector3(0f, 3.85f, fz + 0.1f), new Vector3(4.9f, 0.05f, 0.1f));
        b.Box(Sl.Gold, new Vector3(0f, 3.25f, fz + 0.1f), new Vector3(4.9f, 0.05f, 0.1f));
        // atap pelana menghadap jalan (punggung sejajar Z) dan cerobong
        b.Gable(Sl.Roof, wall, Sl.Wood, new Vector3(0f, 5.6f, 0.175f), LlW + 0.1f, LlD + 0.35f, 3.0f, 0.5f, false);
        b.BoxB(Sl.DarkStone, new Vector3(-2.2f, 5.0f, -1.2f), new Vector3(0.7f, 3.9f, 0.7f));
        b.Box(Sl.DarkStone, new Vector3(-2.2f, 8.95f, -1.2f), new Vector3(0.95f, 0.14f, 0.95f));
        // sisi belakang (menghadap alun-alun): dua jendela di tiap lantai dan pintu belakang, supaya tidak jadi tembok polos
        foreach (float sx in new[] { -1.7f, 1.7f })
        {
            b.Box(Sl.Wood, new Vector3(sx, 1.9f, -hd - 0.03f), new Vector3(1.1f, 1.2f, 0.07f));
            b.Box(Sl.Glass, new Vector3(sx, 1.9f, -hd - 0.07f), new Vector3(0.85f, 0.95f, 0.05f));
            b.Box(Sl.Wood, new Vector3(sx, 4.5f, -hd - 0.03f), new Vector3(1.0f, 1.1f, 0.07f));
            b.Box(Sl.Glass, new Vector3(sx, 4.5f, -hd - 0.07f), new Vector3(0.78f, 0.88f, 0.05f));
        }
        b.Box(Sl.Wood, new Vector3(0f, 1.4f, -hd - 0.04f), new Vector3(1.1f, 1.9f, 0.1f));
        b.Box(Sl.Stone, new Vector3(0f, 2.46f, -hd - 0.06f), new Vector3(1.45f, 0.16f, 0.14f));
        // barang dagangan di depan toko (semua di z <= hd + 1,2 supaya tidak menyentuh lampu jalan di r ~ 91,8)
        if (bakery) { EcBakeryGoods(b, hd); EcDogLocal(b, i); }
        else EcChandlerGoods(b, hd, i);
    }

    /// <summary>Satu lilin menyala (batang putih dan nyala dari dua belah ketupat bersilang).</summary>
    static void EcCandle(GeoBag b, Vector3 p, float h)
    {
        b.Box(Sl.Stone, new Vector3(p.x, p.y + h * 0.5f, p.z), new Vector3(0.05f, h, 0.05f));
        Vector3 f = new Vector3(p.x, p.y + h + 0.004f, p.z);
        const float s = 0.018f, fh = 0.07f;
        b.QuadBoth(Sl.Glow, f, f + new Vector3(s, fh * 0.35f, 0f), f + new Vector3(0f, fh, 0f), f + new Vector3(-s, fh * 0.35f, 0f));
        b.QuadBoth(Sl.Glow, f, f + new Vector3(0f, fh * 0.35f, s), f + new Vector3(0f, fh, 0f), f + new Vector3(0f, fh * 0.35f, -s));
    }

    /// <summary>Pengrajin lilin: rak lilin celup yang digantung, tong, meja dengan tiga lilin menyala.</summary>
    static void EcChandlerGoods(GeoBag b, float hd, int i)
    {
        float rz = hd + 0.95f;
        foreach (float x in new[] { -3.1f, -0.1f })
            b.BoxB(Sl.Wood, new Vector3(x, 0f, rz), new Vector3(0.08f, 1.75f, 0.08f));
        b.Beam(Sl.Wood, new Vector3(-3.1f, 1.7f, rz), new Vector3(-0.1f, 1.7f, rz), 0.07f, 0.07f);
        for (int k = 0; k < 10; k++)
        {
            float x = -2.85f + k * 0.28f;
            float h = 0.5f + 0.12f * EcHash(i * 20 + k, 5);
            b.Cyl(Sl.Stone, new Vector3(x, 1.68f - h, rz), 0.022f, h, 5, 0.017f, true);
        }
        BuildKit.Barrel(b, new Vector3(1.5f, 0f, hd + 0.75f));
        float tz = hd + 0.8f;
        b.Box(Sl.Wood, new Vector3(2.9f, 0.62f, tz), new Vector3(0.9f, 0.06f, 0.5f));
        foreach (float sx in new[] { -0.38f, 0.38f })
            foreach (float sz in new[] { -0.2f, 0.2f })
                b.BoxB(Sl.Wood, new Vector3(2.9f + sx, 0f, tz + sz), new Vector3(0.05f, 0.62f, 0.05f));
        EcCandle(b, new Vector3(2.65f, 0.65f, tz), 0.18f);
        EcCandle(b, new Vector3(2.9f, 0.65f, tz + 0.05f), 0.24f);
        EcCandle(b, new Vector3(3.15f, 0.65f, tz - 0.03f), 0.15f);
    }

    /// <summary>Toko roti: meja dengan keranjang roti, karung tepung, bangku.</summary>
    static void EcBakeryGoods(GeoBag b, float hd)
    {
        float tz = hd + 0.8f;
        b.Box(Sl.Wood, new Vector3(-1.1f, 0.78f, tz), new Vector3(2.6f, 0.07f, 0.6f));
        foreach (float x in new[] { -2.3f, 0.1f })
            foreach (float z in new[] { tz - 0.22f, tz + 0.22f })
                b.BoxB(Sl.Wood, new Vector3(x, 0f, z), new Vector3(0.07f, 0.78f, 0.07f));
        for (int k = 0; k < 3; k++)
        {
            float x = -1.9f + k * 0.8f;
            b.Cyl(Sl.WoodLight, new Vector3(x, 0.81f, tz), 0.26f, 0.15f, 10, 0.22f, false);
            for (int l = 0; l < 4; l++)
                b.Ball(Sl.Thatch, new Vector3(x + (l - 1.5f) * 0.1f, 0.98f + 0.02f * (l % 2), tz + (l % 2 == 0 ? 0.06f : -0.06f)), 0.075f, 4, 6, 0.65f);
        }
        foreach (float sx in new[] { 1.1f, 1.5f })
            b.Ball(Sl.Thatch, new Vector3(sx, 0.3f, hd + 0.75f), 0.3f, 5, 8, 0.85f);                // karung tepung (warna jerami)
        b.Box(Sl.Wood, new Vector3(3.9f, 0.42f, hd + 0.75f), new Vector3(1.1f, 0.07f, 0.4f));
        b.BoxB(Sl.Wood, new Vector3(3.45f, 0f, hd + 0.75f), new Vector3(0.07f, 0.42f, 0.35f));
        b.BoxB(Sl.Wood, new Vector3(4.35f, 0f, hd + 0.75f), new Vector3(0.07f, 0.42f, 0.35f));
    }

    // ================================================================== gapura
    /// <summary>Dua gapura kayu melintasi jalan di kedua ujung busur; papan "JALAN PENGRAJIN LILIN" menghadap ke dua arah jalan.</summary>
    static void EcLaneGates(Transform lane, Material[] slots)
    {
        var b = new GeoBag();
        float[] gaz = { 207.6f, 244.7f };
        Color cream = new Color(0.95f, 0.9f, 0.74f, 1f);
        for (int g = 0; g < 2; g++)
        {
            float az = gaz[g], ar = az * Mathf.Deg2Rad;
            Vector2 o = WallKit.Out(az);
            Vector3 dir = new Vector3(o.x, 0f, o.y);                                   // keluar dari pusat kota
            Vector3 tan = new Vector3(Mathf.Cos(ar), 0f, -Mathf.Sin(ar));             // ke arah azimut yang membesar
            Vector3 a = V3(AtAz(az, 91.3f)), c = V3(AtAz(az, 104.7f)), m = V3(AtAz(az, 98f));
            Vector3 up = Vector3.up;
            b.BoxB(Sl.Stone, a, new Vector3(0.75f, 0.35f, 0.75f), az);
            b.BoxB(Sl.Stone, c, new Vector3(0.75f, 0.35f, 0.75f), az);
            b.BoxB(Sl.Wood, a + up * 0.35f, new Vector3(0.38f, 4.6f, 0.38f), az);
            b.BoxB(Sl.Wood, c + up * 0.35f, new Vector3(0.38f, 4.6f, 0.38f), az);
            b.Beam(Sl.Wood, a + up * 4.85f, c + up * 4.85f, 0.36f, 0.32f);
            b.Beam(Sl.Wood, a + up * 3.7f, a + dir * 1.3f + up * 4.8f, 0.18f, 0.18f);
            b.Beam(Sl.Wood, c + up * 3.7f, c - dir * 1.3f + up * 4.8f, 0.18f, 0.18f);
            // atap kecil di atas balok
            b.Push(m + up * 5.0f, az);
            b.Gable(Sl.Roof, Sl.DarkStone, Sl.Wood, Vector3.zero, 1.1f, 13.0f, 0.5f, 0.3f, false);
            b.Pop();
            // papan nama tergantung rapat di bawah balok, dua lentera di kiri-kanannya
            b.Box(Sl.Dark, m + up * 4.3f, new Vector3(0.1f, 0.78f, 5.4f), az);
            b.Box(Sl.Gold, m + up * 4.68f, new Vector3(0.14f, 0.05f, 5.5f), az);
            b.Box(Sl.Gold, m + up * 3.92f, new Vector3(0.14f, 0.05f, 5.5f), az);
            BuildKit.Lantern(b, a + dir * 3.0f + up * 4.1f);
            BuildKit.Lantern(b, c - dir * 3.0f + up * 4.1f);
            // papan menghadap +singgung dibaca dari sisi itu; satunya lagi sebaliknya
            BuildKit.Text(lane, "JALAN PENGRAJIN LILIN", m + up * 4.3f + tan * 0.08f, az - 90f, 0.06f, cream);
            BuildKit.Text(lane, "JALAN PENGRAJIN LILIN", m + up * 4.3f - tan * 0.08f, az + 90f, 0.06f, cream);
            EcLight(lane, "CahayaGapura" + (g + 1), m + up * 3.4f, 11f, 1.0f, new Color(1f, 0.82f, 0.55f, 1f), false);
            BoxCol(lane, "TiangCol" + (g + 1) + "a", a + up * 2.4f, new Vector3(0.45f, 4.8f, 0.45f), new Vector3(0f, az, 0f));
            BoxCol(lane, "TiangCol" + (g + 1) + "b", c + up * 2.4f, new Vector3(0.45f, 4.8f, 0.45f), new Vector3(0f, az, 0f));
        }
        b.Emit(lane, "Gapura", slots);
        Vector2 w = AtAz(210.5f, 98f);
        EcInfo(lane, "InteractPoint_JalanPengrajinLilin", V3(w), "Periksa jalan ini", 6.5f, "Jalan Pengrajin Lilin",
            "Bau madu dan lemak hangat memenuhi busur jalan ini sebelum fajar. Para pengrajin merendam sumbu berkali-kali, dan menjelang musim dingin " +
            "mereka tak sempat tidur: seluruh kota memesan lilin dan lentera untuk Festival Cahaya Salju.\n\n" +
            "Pengrajin di sini tidak pernah menjual satu batang saja. Sudah menjadi kebiasaan menjual enam, dan tak seorang pun ingat sebabnya. " +
            "Di tengah busur ada toko roti, dan di jalan di depannya batu pavingnya dilingkari tali merah.", false);
    }

    // ================================================================== retakan enam kelopak
    static void EcCrack(Transform lane, Material[] slots)
    {
        Vector2 c = AtAz(LlCrackAz, LlCrackR);
        Vector2 ov = WallKit.Out(LlCrackAz);
        float azr = LlCrackAz * Mathf.Deg2Rad;
        Vector2 tn = new Vector2(Mathf.Cos(azr), -Mathf.Sin(azr));
        var b = new GeoBag();

        // enam kelopak tertutup (garis tipis dua sisi), tiga cabang rambut di ujungnya, lilin di luar ujungnya
        const int N = 8;
        const float PL = 1.45f, PW = 0.40f;
        for (int k = 0; k < 6; k++)
        {
            float phi = (30f + 60f * k) * Mathf.Deg2Rad;
            Vector2 e = new Vector2(Mathf.Cos(phi), Mathf.Sin(phi)), pr = new Vector2(-e.y, e.x);     // (u, v)
            var pts = new List<Vector2>();
            for (int s = 0; s <= N; s++)
            {
                float t = s / (float)N, off = PW * Mathf.Sin(Mathf.PI * t) * (1f - 0.25f * t);
                Vector2 q = e * (PL * t) + pr * off;
                pts.Add(EcLp(c, tn, ov, q.x, q.y));
            }
            for (int s = N - 1; s >= 0; s--)
            {
                float t = s / (float)N, off = PW * Mathf.Sin(Mathf.PI * t) * (1f - 0.25f * t);
                Vector2 q = e * (PL * t) - pr * off;
                pts.Add(EcLp(c, tn, ov, q.x, q.y));
            }
            b.Ribbon(Sl.Dark, pts, 0.07f, 0.105f);
            Vector2 tipUV = e * PL;
            for (int br = -1; br <= 1; br++)
            {
                float ph = phi + br * 0.5f;
                Vector2 d = new Vector2(Mathf.Cos(ph), Mathf.Sin(ph)) * 0.4f;
                b.Ribbon(Sl.Dark, new List<Vector2> { EcLp(c, tn, ov, tipUV.x, tipUV.y), EcLp(c, tn, ov, tipUV.x + d.x, tipUV.y + d.y) }, 0.035f, 0.105f);
            }
            Vector2 cp = EcLp(c, tn, ov, e.x * (PL + 0.17f), e.y * (PL + 0.17f));
            EcCandle(b, new Vector3(cp.x, 0.1f, cp.y), 0.14f + 0.03f * (k % 3));
        }
        b.Disc(Sl.Dark, new Vector3(c.x, 0f, c.y), 0.14f, 10, 0.106f);

        // tali merah: dua belas tiang, tali melorot sedikit di antaranya
        const int NP = 12;
        var post = new Vector3[NP];
        for (int k = 0; k < NP; k++)
        {
            float a = (15f + 30f * k) * Mathf.Deg2Rad;
            Vector2 pp = EcLp(c, tn, ov, Mathf.Cos(a) * LlCrackPost, Mathf.Sin(a) * LlCrackPost);
            post[k] = new Vector3(pp.x, 0.1f, pp.y);
            b.BoxB(Sl.Wood, post[k], new Vector3(0.09f, 0.9f, 0.09f));
            b.Ball(Sl.Wood, post[k] + Vector3.up * 0.93f, 0.06f, 3, 6);
        }
        for (int k = 0; k < NP; k++)
        {
            Vector3 p0 = post[k] + Vector3.up * 0.74f, p1 = post[(k + 1) % NP] + Vector3.up * 0.74f;
            Vector3 mid = (p0 + p1) * 0.5f - Vector3.up * 0.09f;
            b.Beam(Sl.Lip, p0, mid, 0.045f, 0.045f);
            b.Beam(Sl.Lip, mid, p1, 0.045f, 0.045f);
        }

        // dua tiang papan peringatan di sisi singgung (satu menghadap tiap arah jalan)
        for (int sd = 0; sd < 2; sd++)
        {
            float sg = sd == 0 ? 1f : -1f;
            Vector2 sp = EcLp(c, tn, ov, sg * (LlCrackPost + 0.45f), 0f);
            Vector3 bw = new Vector3(sp.x, 0.1f, sp.y);
            Vector2 face = tn * sg;                                                     // arah hadap papan (menjauhi retakan)
            float yawBoard = Mathf.Atan2(face.x, face.y) * Mathf.Rad2Deg;
            b.BoxB(Sl.Wood, bw, new Vector3(0.1f, 1.5f, 0.1f));
            b.Box(Sl.Wood, bw + new Vector3(0f, 1.2f, 0f), new Vector3(1.05f, 0.52f, 0.07f), yawBoard);
            b.Box(Sl.Dark, bw + new Vector3(face.x, 0f, face.y) * 0.04f + new Vector3(0f, 1.2f, 0f), new Vector3(0.95f, 0.44f, 0.04f), yawBoard);
        }
        b.Emit(lane, "RetakanKelopak", slots);
        Color cream = new Color(0.93f, 0.86f, 0.7f, 1f);
        for (int sd = 0; sd < 2; sd++)
        {
            float sg = sd == 0 ? 1f : -1f;
            Vector2 face = tn * sg;
            Vector2 sp = EcLp(c, tn, ov, sg * (LlCrackPost + 0.45f), 0f) + face * 0.08f;
            float yaw = EcFaceYaw(face);
            BuildKit.Text(lane, "JANGAN INJAK", new Vector3(sp.x, 1.3f, sp.y), yaw, 0.017f, cream);
            BuildKit.Text(lane, "RETAKAN KELOPAK", new Vector3(sp.x, 1.12f, sp.y), yaw, 0.0145f, cream);
        }

        EcLight(lane, "CahayaRetakan", new Vector3(c.x, 1.4f, c.y), 7f, 0.7f, new Color(1f, 0.8f, 0.55f, 1f), true);
        Reserve(c.x, c.y, LlCrackPost + 1.0f);

        // benda baca quest nd_k14 ("Mulut di Jalan Pengrajin Lilin"), di sisi jalan dekat tali
        Vector2 lp = EcLp(c, tn, ov, -(LlCrackPost + 1.6f), 0.4f);
        EcLore(lane, "nd_o_lapar", new Vector3(lp.x, 0.1f, lp.y));
    }
}
