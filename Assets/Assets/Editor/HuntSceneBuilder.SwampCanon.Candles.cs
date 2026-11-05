using UnityEngine;
using HunterGuild;

/// <summary>
/// Seribu empat ratus lilin putih (kanon §8.5), semuanya menyala dan tak satu pun padam: dua dermaga Kelabu (724), satu lilin di kaki tiap
/// penidur Merana, tiga baris di kaki menara kapel, dan sisanya berbaris di kedua tepi jalan dari alun-alun ke menara. Jumlah akhir tepat
/// SwCandleTotal (dihitung kode). Lilin tanpa collider; hanya beberapa lampu titik hangat dan lembut (bukan satu lampu per lilin).
/// </summary>
public static partial class HuntSceneBuilder
{
    static Material[] SwCandleSlots()
    {
        var s = (Material[])ForestKit.SSlots.Clone();
        s[Sl.Marble] = DcMat("RawaLilinPutih", new Color(0.93f, 0.90f, 0.82f, 1f), 0.25f);
        var fl = DcMat("RawaApiLilin", new Color(1f, 0.86f, 0.55f, 1f), 0.1f);
        s[Sl.Glow] = TownMats.Emissive(fl, new Color(1.2f, 0.98f, 0.58f, 1f));          // hangat, tidak menyilaukan
        return s;
    }

    /// <summary>Satu lilin: batang putih (kotak) dan nyala dari dua belah belah ketupat bersilang.</summary>
    static void SwCandle(GeoBag b, Vector3 p, float h, int seed)
    {
        swCandles++;
        b.Box(Sl.Marble, new Vector3(p.x, p.y + h * 0.5f, p.z), new Vector3(0.045f, h, 0.045f), DcHash(seed, 61) * 90f);
        Vector3 f = new Vector3(p.x, p.y + h + 0.004f, p.z);
        float s = 0.017f, fh = 0.065f;
        DcQ(b, Sl.Glow, f, f + new Vector3(s, fh * 0.35f, 0f), f + new Vector3(0f, fh, 0f), f + new Vector3(-s, fh * 0.35f, 0f), Vector3.forward);
        DcQ(b, Sl.Glow, f, f + new Vector3(0f, fh * 0.35f, s), f + new Vector3(0f, fh, 0f), f + new Vector3(0f, fh * 0.35f, -s), Vector3.right);
    }

    static void SwEmitCandles(Transform parent, GeoBag b, string name)
    {
        b.Emit(parent, name, SwCandleSlots());
    }

    static void SwWarmLight(Transform parent, string name, Vector3 pos)
    {
        var lg = new GameObject(name);
        lg.transform.SetParent(parent, false);
        lg.transform.position = pos;
        var l = lg.AddComponent<Light>();
        l.type = LightType.Point; l.range = 7f; l.intensity = 0.8f; l.color = new Color(1f, 0.82f, 0.55f, 1f);
    }

    // ================================================================== dermaga Kelabu
    static void SwDocks(Transform root)
    {
        var g = new GameObject("DermagaLilin").transform;
        g.SetParent(root, false);
        // sama dengan PlanFish(): dermaga 1 dan 2 (x, z desain, panjang); di dunia memanjang ke barat desain, ke dalam Teluk Nelayan
        var docks = new[] { new Vector3(-138f, -128f, 17f), new Vector3(-138f, -72f, 14f) };
        Vector3 infoAt = Vector3.zero;
        for (int d = 0; d < docks.Length; d++)
        {
            Vector3 org = G(docks[d].x, docks[d].y); org.y = WaterY;
            Quaternion q = Quaternion.Euler(0f, -90f + (M < 0f ? 180f : 0f), 0f);
            float len = docks[d].z;
            int n = Mathf.FloorToInt((len - 1.1f) / 0.16f) + 1;
            var bag = new GeoBag();
            foreach (float rx in new[] { -0.95f, -0.72f, 0.72f, 0.95f })
                for (int k = 0; k < n; k++)
                {
                    Vector3 p = org + q * new Vector3(rx, 0.60f, 0.5f + k * 0.16f);
                    SwCandle(bag, p, 0.10f + 0.12f * DcHash(d * 1000 + k, (int)(rx * 100f)), d * 500 + k);
                }
            SwEmitCandles(g, bag, "Lilin_Dermaga_" + (d + 1));
            for (int j = 1; j <= 3; j++) SwWarmLight(g, "Cahaya_Dermaga_" + (d + 1) + "_" + j, org + q * new Vector3(0f, 1.9f, len * j / 4f));
            if (d == 0) infoAt = org + q * new Vector3(0f, 0.6f, 3.0f);
        }
        DcInfo(g, "InteractPoint_DermagaLilin", infoAt, "Periksa lilin di dermaga", 6.5f, "Dermaga Lilin",
            "Lilin putih berderet di sepanjang dermaga, dua baris di tiap sisi, terlalu rapat untuk dihitung dalam semalam. Semuanya menyala. Hujan semalam tak memadamkan satu pun, dan lilin di ujung dermaga lebih tinggi dari yang lain, seperti baru diganti.\n\n" +
            "Para nelayan tak mau ditanya siapa yang menyalakannya. \"Sudah dari dulu,\" kata mereka, dan lekas membereskan jala.");
    }

    // ================================================================== menara kapel Merana
    /// <summary>Tiga baris lilin di kaki menara, lalu dua kali dua baris di tepi jalan alun-alun menuju menara sampai jumlah tepat 1.400.</summary>
    static void SwTowerCandles(Transform g, GeoBag cand, Vector2 tw)
    {
        float yaw = FaceYaw(Pt(GhostC, 0f, 40f), GhostC);
        for (int r = 0; r < 3; r++)
            for (int k = 0; k < 9; k++)
            {
                Vector2 c = Local(tw, yaw, -1.5f + k * 0.375f, 2.2f + 0.2f * r);
                Vector3 p = G(c.x, c.y);
                SwCandle(cand, p, 0.12f + 0.1f * DcHash(r * 9 + k, 71), 3000 + r * 9 + k);
            }
        int rest = SwCandleTotal - swCandles;
        int n = rest / 4, extra = rest - 4 * n;
        float z0 = GhostC.y + 9f, z1 = Local(tw, yaw, 0f, 4.1f).y;                       // dari alun-alun sampai 1,5 m sebelum baris di kaki menara
        int idx = 0;
        foreach (float rx in new[] { -1.15f, -0.9f, 0.9f, 1.15f })
            for (int k = 0; k < n; k++)
            {
                float z = Mathf.Lerp(z0, z1, k / (float)Mathf.Max(1, n - 1));
                Vector3 p = G(GhostC.x + rx, z);
                SwCandle(cand, p, 0.10f + 0.12f * DcHash(4000 + idx, k), 4000 + idx++);
            }
        for (int k = 0; k < extra; k++)                                                   // sisa pembulatan: baris keempat di kaki menara
        {
            Vector2 c = Local(tw, yaw, -0.4f + k * 0.4f, 2.8f);
            SwCandle(cand, G(c.x, c.y), 0.14f, 5000 + k);
        }
        SwEmitCandles(g, cand, "Lilin_Merana");
        foreach (float dz in new[] { 14f, 22f, 29f })
            SwWarmLight(g, "Cahaya_JalanKapel_" + (int)dz, G(GhostC.x, GhostC.y + dz) + new Vector3(0f, 1.7f, 0f));
    }
}
