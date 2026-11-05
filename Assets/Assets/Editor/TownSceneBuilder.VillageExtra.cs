using System.Collections.Generic;
using UnityEngine;
using HunterGuild;

/// <summary>Desa Pemula diperbesar: gereja, kompleks pemakaman, papan info tambahan.</summary>
public static partial class TownSceneBuilder
{
    // =====================================================================
    // Gereja
    // =====================================================================
    static void VChurch()
    {
        Vector3 pw = VPol(235f, VR - 20f);
        Vector2 p2 = new Vector2(pw.x, pw.z);
        float yaw = YawToward(p2, Vector2.zero);
        var h = new GameObject("GerejaDesa").transform;
        h.SetParent(tHouses, false);
        h.position = pw;
        h.rotation = Quaternion.Euler(0f, yaw, 0f);

        var b = new GeoBag();
        VillageKit.Church(b);
        b.Emit(h, "Mesh", baseSlots);

        BoxCol(h, "LambungCol", new Vector3(0f, 3.5f, 0f), new Vector3(10f, 7f, 20f), Vector3.zero);
        BoxCol(h, "MenaraCol", new Vector3(0f, 8f, 11.2f), new Vector3(6.2f, 16f, 6.2f), Vector3.zero);
        ReserveBox(pw.x, pw.z, 20f, 44f, yaw, 0f);
        VMapMark("Gereja Desa", pw, 3);
        Reserve(pw.x, pw.z, 14f);

        // papan nama + lampu
        BuildKit.Text(h, "GEREJA\nDESA PEMULA", new Vector3(0f, 5.6f, 14.5f), 180f, 0.05f, new Color(1f, 0.88f, 0.5f));
        AddLight(h, new Vector3(0f, 4f, 17f), 16f, 1.4f);
        AddLight(h, new Vector3(0f, 4f, -2f), 14f, 1.1f);
        var lamp = PropKit.LampProto();
        foreach (float sx in new[] { -3.4f, 3.4f })
            PropKit.Place(lamp, h, "LampuGereja", h.TransformPoint(new Vector3(sx, 0f, 17.5f)), 0f, 1f, baseSlots);

        // pendeta + titik bicara
        Vector3 front = h.TransformPoint(new Vector3(0f, 0f, 19f));
        var priest = VNpc("NPC_Pendeta", "Bapa Elric", "Pendeta Desa", "Peziarah", front + Fwd(yaw) * 0.5f, false);
        var ip = VAttach<InfoPoint>(priest, "Bicara: Bapa Elric", 3.6f);
        ip.title = "Bapa Elric";
        ip.message = "Selamat datang di rumah doa Desa Pemula, Nak.\n\n" +
            "Lonceng berbunyi tiga kali kalau ada Hunter yang gugur dan pulang dengan selamat. Bunyinya jarang, syukurlah.\n\n" +
            "Makam desa ada di selatan. Di sana beristirahat warga yang telah lebih dulu, dan satu-dua Hunter yang tak kembali dari hutan.\n\n" +
            "Hati-hati di Hutan Pemula, dan jangan terlalu bangga pada pedangmu.";
        VPoint<InfoPoint>(h, "InteractPoint_Gereja", front + Fwd(yaw) * 3.4f, "Lihat Gereja Desa", 3.6f).message =
            "GEREJA DESA PEMULA\nDibangun oleh para pendiri desa. Terbuka bagi semua peziarah.";
    }

    // =====================================================================
    // Kompleks pemakaman (besar, di barat desa; jalan barat berakhir di gerbangnya)
    // =====================================================================
    static void VCemetery()
    {
        Vector3 pw = VPol(270f, VR - 22f);
        Vector2 p2 = new Vector2(pw.x, pw.z);
        float yaw = YawToward(p2, Vector2.zero);
        var h = new GameObject("MakamDesa").transform;
        h.SetParent(tHouses, false);
        h.position = pw;
        h.rotation = Quaternion.Euler(0f, yaw, 0f);
        var rng = new System.Random(404);
        var b = new GeoBag();
        const float HW = 21f, HD = 17f;      // setengah lebar (x) dan setengah dalam (z); gerbang di z=+HD

        // tanah + jalan batu (poros tengah dan silang)
        b.Patch(Sl.Grass, new Vector2(-HW, -HD), new Vector2(-HW, HD), new Vector2(HW, HD), new Vector2(HW, -HD), 0.03f, new Vector2(0f, 1f));
        b.Patch(Sl.Cobble, new Vector2(-1.4f, -9f), new Vector2(-1.4f, HD + 3f), new Vector2(1.4f, HD + 3f), new Vector2(1.4f, -9f), 0.05f, new Vector2(0f, 1f));
        b.Patch(Sl.Cobble, new Vector2(-HW + 3f, -1.2f), new Vector2(-HW + 3f, 1.2f), new Vector2(HW - 3f, 1.2f), new Vector2(HW - 3f, -1.2f), 0.05f, new Vector2(1f, 0f));
        b.Disc(Sl.Cobble, new Vector3(0f, 0f, 0f), 4.6f, 28, 0.055f);
        // pagar tinggi dengan celah gerbang 6.6 m
        PropKit.Fence(b, new List<Vector3> { new Vector3(-HW, 0f, HD), new Vector3(-3.3f, 0f, HD) }, 1.7f);
        PropKit.Fence(b, new List<Vector3> { new Vector3(3.3f, 0f, HD), new Vector3(HW, 0f, HD), new Vector3(HW, 0f, -HD), new Vector3(-HW, 0f, -HD), new Vector3(-HW, 0f, HD) }, 1.7f);
        // gerbang batu besar dengan lengkung dan lentera
        CemeteryArt.GatePier(b, -3.6f, HD); CemeteryArt.GatePier(b, 3.6f, HD);
        CemeteryArt.IronGate(b, HD, 0.5f);
        CemeteryArt.Extras(b, HW, HD, rng);
        b.Beam(Sl.Metal, new Vector3(-3.6f, 4.3f, HD), new Vector3(3.6f, 4.3f, HD), 0.16f, 0.16f);
        b.Box(Sl.Wood, new Vector3(0f, 5.0f, HD), new Vector3(6.4f, 1.0f, 0.16f));
        // jalan masuk berundak: dua tangga kecil
        b.Box(Sl.Stone, new Vector3(0f, 0.1f, HD + 1.4f), new Vector3(6.0f, 0.2f, 1.2f));
        // mausoleum besar di belakang poros
        float mz = -11.5f;
        b.Box(Sl.Stone, new Vector3(0f, 0.35f, mz), new Vector3(14f, 0.7f, 8.4f));
        b.Box(Sl.Stone, new Vector3(0f, 3.9f, mz), new Vector3(11.4f, 6.4f, 6.6f));
        b.Gable(Sl.DarkStone, Sl.Stone, Sl.Wood, new Vector3(0f, 7.1f, mz), 11.4f, 6.6f, 3.2f, 0.6f, true);
        foreach (float sx in new[] { -4.8f, -1.6f, 1.6f, 4.8f }) b.Cyl(Sl.Marble, new Vector3(sx, 0.7f, mz + 3.9f), 0.45f, 5.6f, 10, 0.4f, true);
        b.Box(Sl.Marble, new Vector3(0f, 6.5f, mz + 3.9f), new Vector3(11.4f, 0.5f, 0.9f));
        b.Box(Sl.DarkStone, new Vector3(0f, 2.3f, mz + 3.34f), new Vector3(2.6f, 3.6f, 0.14f));
        b.Box(Sl.Glow, new Vector3(0f, 4.4f, mz + 3.36f), new Vector3(1.0f, 0.5f, 0.1f));
        b.Box(Sl.Stone, new Vector3(0f, 0.15f, mz + 4.8f), new Vector3(7f, 0.3f, 1.4f));
        b.Box(Sl.Gold, new Vector3(0f, 9.9f, mz), new Vector3(0.24f, 2.0f, 0.24f));
        b.Box(Sl.Gold, new Vector3(0f, 10.4f, mz), new Vector3(1.1f, 0.24f, 0.24f));
        CemeteryArt.Mausoleum(b, mz);
        // patung ksatria di persimpangan tengah
        b.Cyl(Sl.Stone, new Vector3(0f, 0.05f, 0f), 1.6f, 0.9f, 12, 1.4f, true);
        PropKit.KnightStatue(b, new Vector3(0f, 0.95f, 0f), 180f);
        // blok makam: 4 petak x (4 kolom x 3 baris) dengan pagar rendah antar-petak
        int gi = 0;
        int[] sxs = { -1, 1 }, szs = { -1, 1 };
        var graveCols = new List<Vector3>();
        foreach (int sx in sxs) foreach (int sz in szs)
        {
            float ox = sx * 11f, oz = sz > 0 ? 8.5f : -4f;
            for (int col = 0; col < 4; col++) for (int row = 0; row < 3; row++)
            {
                if (sz < 0 && row == 2 && Mathf.Abs(ox + (col - 1.5f) * 3.3f) < 8f && false) continue;
                float gx = ox + (col - 1.5f) * 3.3f + ((float)rng.NextDouble() - 0.5f) * 0.5f;
                float gz = oz + (row - 1f) * 3.4f + ((float)rng.NextDouble() - 0.5f) * 0.4f;
                if (sz < 0 && gz < -7.5f && Mathf.Abs(gx) < 8.5f) continue;                           // ruang mausoleum
                float ya = ((float)rng.NextDouble() - 0.5f) * 14f;
                Vector3 c = new Vector3(gx, 0f, gz);
                b.Box(Sl.Dirt, c + new Vector3(0f, 0.12f, 0.95f), new Vector3(1.1f, 0.24f, 1.9f), ya);
                CemeteryArt.Grave(b, c, ya * 0.35f, gi % 7, gi + sx * 3 + sz);
                graveCols.Add(c);
                gi++;
            }
        }
        // sarkofagus di dua sudut belakang
        foreach (float sx in new[] { -15.5f, 15.5f })
        {
            b.Box(Sl.Stone, new Vector3(sx, 0.5f, -12.5f), new Vector3(2.6f, 1.0f, 1.4f));
            b.Box(Sl.Marble, new Vector3(sx, 1.15f, -12.5f), new Vector3(2.8f, 0.3f, 1.6f));
            b.Beam(Sl.Gold, new Vector3(sx - 0.9f, 1.4f, -12.5f), new Vector3(sx + 0.9f, 1.4f, -12.5f), 0.08f, 0.06f);
        }
        b.Emit(h, "Mesh", baseSlots);

        BuildKit.Text(h, "MAKAM DESA PEMULA", new Vector3(0f, 5.0f, HD + 0.12f), 180f, 0.075f, new Color(0.95f, 0.93f, 0.85f));
        BuildKit.Text(h, "Yang pergi dengan berani, pulang dalam ingatan", new Vector3(0f, 4.45f, HD + 0.12f), 180f, 0.032f, new Color(0.85f, 0.82f, 0.72f));
        BoxCol(h, "PagarBelakang", new Vector3(0f, 0.85f, -HD), new Vector3(HW * 2f, 1.7f, 0.3f), Vector3.zero);
        BoxCol(h, "PagarKiri", new Vector3(-HW, 0.85f, 0f), new Vector3(0.3f, 1.7f, HD * 2f), Vector3.zero);
        BoxCol(h, "PagarKanan", new Vector3(HW, 0.85f, 0f), new Vector3(0.3f, 1.7f, HD * 2f), Vector3.zero);
        BoxCol(h, "PagarDepanKiri", new Vector3(-(HW + 3.3f) / 2f, 0.85f, HD), new Vector3(HW - 3.3f, 1.7f, 0.3f), Vector3.zero);
        BoxCol(h, "PagarDepanKanan", new Vector3((HW + 3.3f) / 2f, 0.85f, HD), new Vector3(HW - 3.3f, 1.7f, 0.3f), Vector3.zero);
        BoxCol(h, "MausoleumCol", new Vector3(0f, 3.5f, mz), new Vector3(11.4f, 7f, 6.6f), Vector3.zero);
        BoxCol(h, "PatungCol", new Vector3(0f, 1.5f, 0f), new Vector3(3.0f, 3f, 3.0f), Vector3.zero);
        for (int i = 0; i < graveCols.Count; i += 2) BoxCol(h, "NisanCol", graveCols[i] + new Vector3(0f, 0.6f, 0f), new Vector3(0.9f, 1.2f, 0.4f), Vector3.zero);
        ReserveBox(pw.x, pw.z, HD * 2f + 6f, HW * 2f + 6f, yaw, 0f);
        Reserve(pw.x, pw.z, 24f);
        VMapMark("Makam Desa", pw, 3);

        // sipres, lentera kristal, kabut ringan
        var cyp = PropKit.TreeProto(3);
        Vector3[] cp = { new Vector3(-17f, 0f, 14f), new Vector3(17f, 0f, 14f), new Vector3(-19f, 0f, 3f), new Vector3(19f, 0f, 3f), new Vector3(-19f, 0f, -6f), new Vector3(19f, 0f, -6f),
                         new Vector3(-12f, 0f, -14.5f), new Vector3(12f, 0f, -14.5f), new Vector3(-5f, 0f, HD + 3.5f), new Vector3(5f, 0f, HD + 3.5f) };
        for (int i = 0; i < cp.Length; i++)
            PropKit.Place(cyp, h, "Sipres", h.TransformPoint(cp[i]), i * 53f, 1.1f + (i % 3) * 0.12f, baseSlots);
        var csl = VCrystalSlots();
        var clamp = PropKit.Get("D_CrystalLamp", DungeonKit.CrystalLamp);
        foreach (var lp in new[] { new Vector3(-6f, 0f, HD + 3f), new Vector3(6f, 0f, HD + 3f), new Vector3(-4f, 0f, 4f), new Vector3(4f, 0f, 4f), new Vector3(-4f, 0f, -5f), new Vector3(4f, 0f, -5f) })
        {
            var go = PropKit.Place(clamp, h, "LenteraMakam", h.TransformPoint(lp), 0f, 1f, csl);
            VBlueLight(go.transform, new Vector3(0f, 3.6f, 0f));
        }

        var ipt = VPoint<InfoPoint>(h, "InteractPoint_Makam", h.TransformPoint(new Vector3(0f, 0f, HD + 5.5f)), "Baca papan Makam Desa", 3.6f);
        ipt.title = "Makam Desa Pemula";
        ipt.message = "MAKAM DESA PEMULA\n\nKompleks pemakaman tua di tepi barat desa. Di sini beristirahat penduduk desa turun-temurun dan beberapa Hunter muda yang tak kembali dari Hutan Pemula.\n\n" +
            "\"Yang pergi dengan berani, pulang dalam ingatan.\"\n\nDi tengah berdiri patung ksatria penjaga. Di ujung jalan berdiri mausoleum keluarga pendiri desa; pintunya terkunci sejak lama.";
        var keeper = VNpc("NPC_JuruKunci", "Pak Bram Tua", "Juru Kunci", "Peziarah", h.TransformPoint(new Vector3(5.5f, 0f, HD - 3f)), false);
        var kip = VAttach<InfoPoint>(keeper, "Bicara: Juru Kunci", 3.4f);
        kip.title = "Juru Kunci Makam";
        kip.message = "Jangan khawatir, Nak. Mereka yang tidur di sini tidak mengganggu yang hidup.\n\n" +
            "Kadang kabut turun di malam hari dan lentera kristal berpendar sendiri. Itu hanya doa lama yang belum selesai.\n\nJangan dekati mausoleum. Pintunya terkunci, dan itu lebih baik begitu.";
    }

    // =====================================================================
    // Papan info tambahan
    // =====================================================================
    static void VExtraBoards()
    {
        VInfoBoard(VPol(78f, VR - 12f), 270f, "INFO DESA",
            "INFO DESA\n\nTimur: Ashwatch\n(Hunter Level 5+)\n\nUtara: Hutan Pemula\n(Slime, Goblin, Herb)",
            "Papan Info Gerbang Timur",
            "Gerbang timur menuju Ashwatch hanya dibuka untuk Hunter Level 5 ke atas. Latihlah dirimu di Hutan Pemula dulu.");
    }

    static void VInfoBoard(Vector3 pos, float yaw, string head, string face, string title, string msg)
    {
        var h = new GameObject("PapanInfo_" + head).transform;
        h.SetParent(tProps, false);
        h.position = pos;
        h.rotation = Quaternion.Euler(0f, yaw, 0f);
        var b = new GeoBag();
        b.Cyl(Sl.Wood, new Vector3(-1.7f, 0f, 0f), 0.16f, 3.3f, 6, 0.14f, true, false);
        b.Cyl(Sl.Wood, new Vector3(1.7f, 0f, 0f), 0.16f, 3.3f, 6, 0.14f, true, false);
        b.Box(Sl.WoodLight, new Vector3(0f, 2.1f, 0f), new Vector3(4.2f, 2.4f, 0.14f));
        b.Box(Sl.Wood, new Vector3(0f, 2.1f, -0.04f), new Vector3(4.4f, 2.6f, 0.08f));
        b.Box(Sl.Roof, new Vector3(0f, 3.5f, 0f), new Vector3(4.8f, 0.14f, 1.0f));
        b.Emit(h, "Mesh", baseSlots);
        BuildKit.Text(h, face, new Vector3(0f, 2.15f, 0.1f), 180f, 0.06f, new Color(1f, 0.93f, 0.7f));
        BoxCol(h, "Col", new Vector3(0f, 1.7f, 0f), new Vector3(4.4f, 3.4f, 0.5f), Vector3.zero);
        AddLight(h, new Vector3(0f, 3.3f, 1.8f), 9f, 0.9f);
        var ip = VPoint<InfoPoint>(h, "InteractPoint_Info", h.TransformPoint(new Vector3(0f, 0f, 2.2f)), "Baca " + title, 3.4f);
        ip.title = title;
        ip.message = msg;
        Reserve(pos.x, pos.z, 3.2f);
    }
}
