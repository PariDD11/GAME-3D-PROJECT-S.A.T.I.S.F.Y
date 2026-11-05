using UnityEngine;
using HunterGuild;

/// <summary>
/// Kanon Padang Hantu, bagian 2: sekeliling Batu Dua Nama. Batu itu, dua namanya, lenteranya, dan pintu tangga 800 dibuat saat scene dimuat
/// (StoryScenery.BuildBone): batu di desain (190, 190), pintu di (183, 190), tanpa putaran, lubang menurun ke arah -Z dunia.
/// Di sini hanya ditambah benda yang tidak menimpa keduanya: alas bundar rendah (tanpa collider supaya tinggi batu runtime tetap),
/// bibir batu dan dua tiang berlentera di mulut tangga, dan gantungan papan nama kosong di sisi barat batu.
/// </summary>
public static partial class HuntSceneBuilder
{
    const float BnSx = 190f, BnSz = 190f, BnDx = 183f, BnDz = 190f;

    static void BnBatu(Transform root, Material[] slots)
    {
        var g = new GameObject("BatuDuaNama").transform;
        g.SetParent(root, false);
        BnAlas(g, slots);
        BnBibir(g, slots);
        BnGantungan(g, slots);
    }

    // ------------------------------------------------------------------ alas bundar
    static void BnAlas(Transform g, Material[] slots)
    {
        Vector3 s = G(BnSx, BnSz);
        var b = new GeoBag();
        b.Push(s, 0f);
        b.Cyl(Sl.DarkStone, new Vector3(0f, -0.3f, 0f), 2.6f, 0.39f, 32, 2.5f, true);                  // alas rendah, puncak +0,09 m
        b.Disc(Sl.Stone, Vector3.zero, 2.15f, 32, 0.1f);
        b.Annulus(Sl.DarkStone, Vector3.zero, 1.7f, 1.78f, 0f, 360f, 32, 0.108f);
        b.Annulus(Sl.DarkStone, Vector3.zero, 1.25f, 1.31f, 0f, 360f, 32, 0.108f);
        for (int i = 0; i < 3; i++)                                                                        // lempeng pijakan ke bibir tangga (timur dunia)
            b.Box(Sl.DarkStone, new Vector3(3.1f + i * 0.75f, 0.03f, (i - 1) * 0.12f), new Vector3(0.62f, 0.07f, 0.8f), 6f * (i - 1));
        b.Pop();
        b.Emit(g, "AlasBatu", slots);
    }

    // ------------------------------------------------------------------ bibir mulut tangga
    static void BnBibir(Transform g, Material[] slots)
    {
        Vector3 d = G(BnDx, BnDz);
        var b = new GeoBag();
        b.Push(d, 0f);
        foreach (float sx in new[] { -1.72f, 1.72f })
        {
            b.BoxB(Sl.Stone, new Vector3(sx, -0.1f, 0f), new Vector3(0.56f, 0.46f, 3.7f));                  // bibir samping
            b.Box(Sl.DarkStone, new Vector3(sx, 0.37f, 0f), new Vector3(0.66f, 0.06f, 3.8f));
            b.BoxB(Sl.Stone, new Vector3(sx, -0.1f, -2.05f), new Vector3(0.5f, 1.6f, 0.5f));                // tiang di ujung terbuka
            b.Box(Sl.DarkStone, new Vector3(sx, 1.55f, -2.05f), new Vector3(0.64f, 0.12f, 0.64f));
            b.Cyl(Sl.Metal, new Vector3(sx, 1.61f, -2.05f), 0.2f, 0.05f, 6, 0.2f, true);
            b.Cyl(Sl.Glow, new Vector3(sx, 1.66f, -2.05f), 0.15f, 0.36f, 6, 0.12f, true, true);              // lentera tanpa sumbu
            b.Cone(Sl.Metal, new Vector3(sx, 2.02f, -2.05f), 0.2f, 0.2f, 6);
        }
        b.BoxB(Sl.Stone, new Vector3(0f, -0.1f, 1.78f), new Vector3(3.0f, 0.46f, 0.56f));                   // bibir belakang
        b.Box(Sl.DarkStone, new Vector3(0f, 0.37f, 1.78f), new Vector3(3.1f, 0.06f, 0.66f));
        for (int i = 0; i < 3; i++)                                                                          // lempeng ambang ke arah turun
            b.Box(Sl.DarkStone, new Vector3(0f, 0.03f, -2.7f - i * 0.75f), new Vector3(2.7f - i * 0.3f, 0.07f, 0.62f), (i - 1) * 4f);
        b.Pop();
        b.Emit(g, "BibirTangga", slots);

        foreach (float sx in new[] { -1.72f, 1.72f })
        {
            BnCol(g, "BibirSampingCol", d + new Vector3(sx, 0.13f, 0f), new Vector3(0.6f, 0.5f, 3.7f), 0f);
            BnCol(g, "TiangLenteraCol", d + new Vector3(sx, 0.7f, -2.05f), new Vector3(0.5f, 1.6f, 0.5f), 0f);
            ELight(g, d + new Vector3(sx, 1.9f, -2.05f), new Color(1f, 0.8f, 0.5f), 9f, 0.9f, false);
        }
        BnCol(g, "BibirBelakangCol", d + new Vector3(0f, 0.13f, 1.78f), new Vector3(3.0f, 0.5f, 0.56f), 0f);

        BnInfo(g, "InteractPoint_MulutTangga", d + new Vector3(0f, 0.1f, -3.9f), "Periksa mulut tangga", 4.6f, "Mulut Tangga",
            "Bibir batu yang rendah membingkai lubang hitam yang menurun ke dalam tanah. Dari bawah naik hawa dingin yang tak berbau apa pun. " +
            "Penjaga Pos Barat menyebut jumlahnya delapan ratus anak tangga, dan berkata tak seorang pun mau menghitungnya sendirian.\n\n" +
            "Lentera di dua tiang ini menyala tanpa sumbu dan tanpa minyak, sudah begitu sejak ada yang ingat. Kata orang pos, di dasar tangga ada gerbang makam, " +
            "dan batu bernama di sebelah barat turun ke dalam tanah setiap fajar sehingga tangga ini terbuka sebentar. Ia sendiri tak pernah menungguinya.");
    }

    // ------------------------------------------------------------------ gantungan papan nama kosong
    static void BnGantungan(Transform g, Material[] slots)
    {
        Vector3 p = G(BnSx + 5.6f, BnSz);
        const float yaw = 90f;                                                                              // +Z lokal -> +X dunia: menghadap batu
        var b = new GeoBag();
        b.Push(p, yaw);
        foreach (float sx in new[] { -2.0f, 2.0f })
        {
            b.BoxB(Sl.Stone, new Vector3(sx, -0.1f, 0f), new Vector3(0.7f, 0.35f, 0.7f));
            b.Beam(Sl.Wood, new Vector3(sx, 0.2f, 0f), new Vector3(sx, 2.9f, 0f), 0.2f, 0.2f);
        }
        b.Beam(Sl.Wood, new Vector3(-2.3f, 2.6f, 0f), new Vector3(2.3f, 2.6f, 0f), 0.16f, 0.16f);          // palang
        b.Gable(Sl.Roof, Sl.Wood, Sl.Wood, new Vector3(0f, 2.95f, 0f), 5.0f, 1.3f, 0.5f, 0.2f, true);
        for (int i = 0; i < 26; i++)
        {
            float x = -1.55f + i * 0.124f;
            float drop = 0.35f + 0.3f * (i % 3) + 0.1f * BnHash(i, 3);
            b.Beam(Sl.Wood, new Vector3(x, 2.57f, 0.03f), new Vector3(x, 2.57f - drop, 0.03f), 0.014f, 0.014f);
            b.Box(Sl.WoodLight, new Vector3(x, 2.57f - drop - 0.14f, 0.04f), new Vector3(0.16f, 0.28f, 0.025f), (BnHash(i, 7) - 0.5f) * 14f);
            bnTags++;
        }
        b.Pop();
        b.Emit(g, "GantunganPapan", slots);
        foreach (float sx in new[] { -2.0f, 2.0f }) BnCol(g, "GantunganCol", BnLw(p, yaw, sx, 1.4f, 0f), new Vector3(0.7f, 2.8f, 0.7f), yaw);

        BnInfo(g, "InteractPoint_Gantungan", BnLw(p, yaw, 0f, 0.1f, 2.4f), "Periksa gantungan papan", 4.2f, "Gantungan Papan",
            "Puluhan papan kayu sekecil telapak tangan tergantung pada tali di gantungan ini, dan semuanya polos. Tak ada tinta di satu pun. " +
            "Di batu sebelah hanya ada dua nama, ditulis dengan tinta emas; selebihnya di padang ini tidak bernama.\n\n" +
            "Penjaga pos berkata papan-papan itu sudah tergantung sebelum ia bertugas, dan jumlahnya tak pernah bertambah atau berkurang. " +
            "Pemburu yang akan turun ke tangga biasanya menyentuh satu, tetapi tak seorang pun menuliskan apa-apa.");
    }
}
