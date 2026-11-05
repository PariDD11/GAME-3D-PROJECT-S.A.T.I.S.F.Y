using System.Collections.Generic;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Kanon Padang Hantu / Padang Tulang (HuntArea_Bone; lihat Docs/LORE_BIBLE.md §8.7). Dipanggil dari BuildEldrathAreaCore() (hanya ea == 2)
/// sesudah EBuildEntrances() dan sebelum BuildPlayerAndManagers(), dan TIDAK memakai UnityEngine.Random, supaya sebaran pohon, batu, monster,
/// dan benda yang sudah ada tidak bergeser. Koordinat desain (x, z) dicerminkan ke dunia oleh G(); yaw adalah yaw DUNIA (lokal +Z -> dunia (sin, cos)).
///
///  1. Pos Barat: gerbang kayu "POS BARAT / JALAN NIAGA TERAKHIR", enam kios di tepi jalan, dan Peta Guild yang isinya satu kata: "kosong". (file ini)
///  2. Batu Dua Nama: alas batu bundar, bibir batu di mulut tangga 800 anak tangga, dan gantungan nama. (.Batu.cs)
///  3. Nisan tanpa nama berderet ke barat dari batu itu, masing-masing bergoresan tiga-tiga. (.Nisan.cs)
///  4. Gerbang Cerberus: dua anjing batu bertiga kepala di depan gerbang batu arena. (.Anjing.cs)
///  5. Makam Raja Malrek di belakang arena, menghadap arena. (.Makam.cs)
///
/// Batu Dua Nama itu sendiri, kedua namanya, lenteranya, dan pintu tangga 800 dibuat saat scene dimuat oleh StoryScenery.BuildBone();
/// kode ini hanya menambah benda di sekelilingnya dan tidak menimpa apa pun. Tidak ada NPC atau mekanik baru; hanya benda dan teks.
/// Teks tidak memakai "yang ketujuh", Jeda, si Sulung, Lucifer, atau nama dewa.
/// </summary>
public static partial class HuntSceneBuilder
{
    static int bnStalls, bnTags, bnStones, bnScratches, bnCleared, bnMoved;      // hitungan untuk uji

    static void BoneCanon()
    {
        var root = Group("KanonPadang");
        bnStalls = 0; bnTags = 0; bnStones = 0; bnScratches = 0; bnCleared = 0; bnMoved = 0;
        var slots = BnSlots();
        BnPosBarat(root, slots);
        BnBatu(root, slots);
        BnNisan(root, slots);
        BnAnjing(root, slots);
        BnMakam(root, slots);
    }

    // ================================================================== pembantu
    /// <summary>Slot khusus kanon: salinan eSlots dengan lentera hangat, kertas peta, segel merah, dan kain kios.</summary>
    static Material[] BnSlots()
    {
        var s = (Material[])eSlots.Clone();
        s[Sl.Glow] = TownMats.Emissive(Mat("BnLentera", new Color(1f, 0.82f, 0.5f)), new Color(0.6f, 0.38f, 0.14f));
        s[Sl.Hedge] = Mat("BnKertas", new Color(0.88f, 0.82f, 0.66f));
        s[Sl.Field] = Mat("BnSegel", new Color(0.6f, 0.13f, 0.11f));
        s[Sl.FieldB] = TownMats.Emissive(Mat("BnBiruHantu", new Color(0.55f, 0.8f, 1f)), new Color(0.22f, 0.4f, 0.6f));
        s[Sl.Dark] = Mat("BnGelap", new Color(0.08f, 0.07f, 0.08f));              // daun pintu makam, hidung, rongga mata (slot Dark bawaan hutan = batu biasa)
        s[Sl.Roof] = Mat("BnAtap", new Color(0.33f, 0.27f, 0.24f));                // atap sirap lapuk
        s[Sl.Cloth] = Mat("BnKain", new Color(0.56f, 0.43f, 0.31f));               // kanopi kios: kain lapuk, bukan merah-biru pasar
        s[Sl.ClothB] = Mat("BnKainB", new Color(0.42f, 0.45f, 0.49f));
        return s;
    }

    /// <summary>Hash deterministik 0..1 (pengganti UnityEngine.Random).</summary>
    static float BnHash(int a, int b)
    {
        unchecked
        {
            uint h = (uint)(a * 73856093) ^ (uint)(b * 19349663);
            h ^= h >> 13; h *= 1274126177u; h ^= h >> 16;
            return (h & 0xFFFFFFu) / 16777216f;
        }
    }

    /// <summary>Titik lokal (lx, ly, lz) pada kerangka yang berputar yaw (derajat) di titik o, ke dunia.</summary>
    static Vector3 BnLw(Vector3 o, float yawDeg, float lx, float ly, float lz)
    {
        float t = yawDeg * Mathf.Deg2Rad, c = Mathf.Cos(t), s = Mathf.Sin(t);
        return new Vector3(o.x + lx * c + lz * s, o.y + ly, o.z - lx * s + lz * c);
    }

    static InfoPoint BnInfo(Transform parent, string name, Vector3 world, string prompt, float radius, string title, string message)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = world;
        var ip = go.AddComponent<InfoPoint>();
        ip.prompt = prompt; ip.radius = radius; ip.title = title; ip.message = message;
        return ip;
    }

    static void BnCol(Transform parent, string name, Vector3 world, Vector3 size, float yawDeg)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = world;
        go.transform.rotation = Quaternion.Euler(0f, yawDeg, 0f);
        go.AddComponent<BoxCollider>().size = size;
    }

    // ================================================================== Pos Barat
    static void BnPosBarat(Transform root, Material[] slots)
    {
        var g = new GameObject("PosBarat").transform;
        g.SetParent(root, false);
        BnGate(g, slots);
        BnStalls(g, slots);
        BnPeta(g, slots);
    }

    /// <summary>Gerbang kayu di jalan masuk Pos Barat (desain (0, -372), menghadap ke selatan desain, ke pemain yang datang dari Eldrath).</summary>
    static void BnGate(Transform g, Material[] slots)
    {
        Vector3 p = G(0f, -372f);
        var b = new GeoBag();
        b.Push(p, 0f);
        foreach (float sx in new[] { -5.2f, 5.2f })
        {
            b.Box(Sl.Stone, new Vector3(sx, 0.15f, 0f), new Vector3(1.2f, 0.9f, 1.2f));                            // tapak batu
            b.Beam(Sl.Wood, new Vector3(sx, 0.5f, 0f), new Vector3(sx, 5.6f, 0f), 0.6f, 0.6f);                   // tiang
            b.Beam(Sl.Wood, new Vector3(sx * 0.82f, 0.3f, 0.45f), new Vector3(sx * 0.82f, 4.6f, 0.45f), 0.22f, 0.22f);
            b.Beam(Sl.Wood, new Vector3(sx, 4.2f, 0.3f), new Vector3(sx * 0.62f, 5.3f, 0.3f), 0.2f, 0.2f);       // penopang miring
        }
        b.Beam(Sl.Wood, new Vector3(-6.5f, 5.35f, 0f), new Vector3(6.5f, 5.35f, 0f), 0.56f, 0.5f);               // balok melintang
        b.Beam(Sl.Wood, new Vector3(-5.7f, 6.2f, 0f), new Vector3(5.7f, 6.2f, 0f), 0.4f, 0.4f);
        b.Beam(Sl.Wood, new Vector3(0f, 5.5f, 0f), new Vector3(0f, 6.4f, 0f), 0.3f, 0.3f);
        b.Gable(Sl.Roof, Sl.Wood, Sl.Wood, new Vector3(0f, 6.4f, 0f), 13.6f, 3.0f, 1.25f, 0.45f, true);
        // papan nama menggantung di bawah balok, di muka (+Z)
        b.Box(Sl.Wood, new Vector3(0f, 4.15f, 0.12f), new Vector3(8.2f, 1.7f, 0.2f));
        b.Box(Sl.WoodLight, new Vector3(0f, 5.02f, 0.12f), new Vector3(8.5f, 0.12f, 0.26f));
        b.Box(Sl.WoodLight, new Vector3(0f, 3.28f, 0.12f), new Vector3(8.5f, 0.12f, 0.26f));
        foreach (float sx in new[] { -3.6f, 3.6f }) b.Beam(Sl.Metal, new Vector3(sx, 5.1f, 0.12f), new Vector3(sx, 4.95f, 0.12f), 0.04f, 0.04f);
        // dua lentera kertas menggantung di sisi papan
        foreach (float sx in new[] { -4.55f, 4.55f })
        {
            b.Beam(Sl.Metal, new Vector3(sx, 5.1f, 0.55f), new Vector3(sx, 4.8f, 0.55f), 0.03f, 0.03f);
            b.Cyl(Sl.Glow, new Vector3(sx, 4.15f, 0.55f), 0.26f, 0.6f, 6, 0.2f, true, true);
            b.Cyl(Sl.Gold, new Vector3(sx, 4.75f, 0.55f), 0.14f, 0.06f, 6, 0.14f, true, false);
        }
        b.Pop();
        b.Emit(g, "GerbangPosBarat", slots);
        foreach (float sx in new[] { -5.2f, 5.2f }) BnCol(g, "TiangGerbangCol", p + new Vector3(sx, 3.0f, 0f), new Vector3(0.8f, 6f, 0.8f), 0f);

        Color cream = new Color(0.95f, 0.88f, 0.68f, 1f);
        BuildKit.Text(g, "POS BARAT", BnLw(p, 0f, 0f, 4.45f, 0.24f), 180f, 0.11f, cream);
        BuildKit.Text(g, "JALAN NIAGA TERAKHIR", BnLw(p, 0f, 0f, 3.7f, 0.24f), 180f, 0.05f, cream);
        ELight(g, p + new Vector3(0f, 4.3f, 1.8f), new Color(1f, 0.8f, 0.5f), 15f, 1.0f);

        BnInfo(g, "InteractPoint_PosBarat", p + new Vector3(0f, 0.1f, 3.2f), "Periksa gerbang pos", 6f, "Pos Barat",
            "Pos Barat berdiri di ujung jalan niaga terakhir sebelum dataran. Di luar gerbang kayu ini tak ada lagi kios, tak ada lagi harga, tak ada lagi " +
            "orang yang bisa ditagih. Di tepi jalan di bawah papan ini berderet kios lampu, tali, garam, dan bekal, dan semuanya menutup dagangan sebelum gelap.\n\n" +
            "Pedagang tua di kios paling dekat gerbang berkata ia sudah berhenti menghitung orang yang berangkat dari sini dan tak kembali. " +
            "\"Yang kembali membeli lampu lagi,\" katanya. \"Yang tidak kembali tidak perlu lampu.\"");
    }

    // ------------------------------------------------------------------ kios
    static readonly float[] BnStallZ = { -364.5f, -357f, -349.5f };

    static void BnStalls(Transform parent, Material[] slots)
    {
        var g = new GameObject("KiosJalanNiaga").transform;
        g.SetParent(parent, false);
        var b = new GeoBag();
        for (int side = 0; side < 2; side++)
            for (int k = 0; k < BnStallZ.Length; k++)
            {
                float dx = side == 0 ? 9.8f : -9.8f;                     // desain timur (+x), lalu barat (-x)
                float yaw = side == 0 ? 90f : -90f;                      // menghadap jalan (x dunia = -x desain)
                Vector3 p = G(dx, BnStallZ[k]);
                b.Push(p, yaw);
                BnStall(b, (k + side * 2) % 4);
                b.Pop();
                BnCol(g, "KiosCol_" + bnStalls, p + new Vector3(0f, 1.4f, 0f), new Vector3(4.4f, 2.8f, 3.2f), yaw);
                bnStalls++;
            }
        b.Emit(g, "Kios", slots);
        foreach (float dx in new[] { 9.0f, -9.0f })
            ELight(g, G(dx, -357f) + new Vector3(0f, 3.4f, 0f), new Color(1f, 0.78f, 0.5f), 10f, 0.8f);
    }

    /// <summary>Satu kios: depan +Z, titik asal di tanah tepat di tengah. Empat varian dagangan (karung, kain, periuk, lilin dan lentera).</summary>
    static void BnStall(GeoBag b, int v)
    {
        b.Box(Sl.WoodLight, new Vector3(0f, 0.05f, 0f), new Vector3(4.6f, 0.16f, 3.5f));                          // alas papan
        foreach (float sx in new[] { -2.05f, 2.05f })
        {
            b.Beam(Sl.Wood, new Vector3(sx, 0.1f, 1.45f), new Vector3(sx, 2.5f, 1.45f), 0.14f, 0.14f);
            b.Beam(Sl.Wood, new Vector3(sx, 0.1f, -1.5f), new Vector3(sx, 3.05f, -1.5f), 0.14f, 0.14f);
        }
        b.Box(Sl.Wood, new Vector3(0f, 1.45f, -1.55f), new Vector3(4.2f, 2.6f, 0.1f));                             // dinding belakang
        b.Box(Sl.Wood, new Vector3(0f, 0.55f, 0.95f), new Vector3(3.9f, 1.0f, 0.8f));                              // meja
        b.Box(Sl.WoodLight, new Vector3(0f, 1.09f, 0.95f), new Vector3(4.1f, 0.07f, 1.0f));
        int[] st = { Sl.Cloth, Sl.ClothB };
        for (int i = 0; i < 6; i++)                                                                                // kanopi bergaris
        {
            float x0 = -2.3f + i * 0.767f, x1 = x0 + 0.767f;
            b.QuadBoth(st[i % 2], new Vector3(x0, 3.05f, -1.65f), new Vector3(x1, 3.05f, -1.65f), new Vector3(x1, 2.5f, 1.75f), new Vector3(x0, 2.5f, 1.75f));
        }
        b.Beam(Sl.Wood, new Vector3(-2.35f, 2.5f, 1.75f), new Vector3(2.35f, 2.5f, 1.75f), 0.08f, 0.08f);
        // dagangan di atas meja
        float ty = 1.13f;
        if (v == 0)
        {
            for (int i = 0; i < 4; i++) b.Ball(Sl.Thatch, new Vector3(-1.4f + i * 0.9f, ty + 0.26f, 0.95f + (i % 2) * 0.1f), 0.3f, 3, 7, 0.9f);
            b.Box(Sl.Wood, new Vector3(1.4f, 0.3f, 1.9f), new Vector3(0.9f, 0.6f, 0.7f));
        }
        else if (v == 1)
        {
            for (int i = 0; i < 5; i++) b.Cyl(i % 2 == 0 ? Sl.Cloth : Sl.ClothB, new Vector3(-1.6f + i * 0.8f, ty, 0.95f), 0.13f, 0.62f, 8, 0.13f, true, false);
        }
        else if (v == 2)
        {
            for (int i = 0; i < 3; i++) { b.Cyl(Sl.DarkStone, new Vector3(-1.2f + i * 1.2f, ty, 0.95f), 0.22f, 0.3f, 8, 0.3f, false, false); b.Ball(Sl.DarkStone, new Vector3(-1.2f + i * 1.2f, ty + 0.3f, 0.95f), 0.26f, 3, 8, 0.8f); }
            b.Ball(Sl.DarkStone, new Vector3(1.6f, 0.3f, 1.95f), 0.34f, 3, 8, 0.9f);
        }
        else
        {
            for (int i = 0; i < 7; i++) b.Cyl(Sl.Plaster, new Vector3(-1.5f + i * 0.5f, ty, 0.95f + (i % 2) * 0.12f), 0.06f, 0.3f + 0.05f * (i % 3), 6, 0.06f, true, false);
            b.Cyl(Sl.Glow, new Vector3(1.55f, ty, 1.2f), 0.14f, 0.34f, 6, 0.11f, true, true);
        }
        // lentera menggantung di tepi kanopi
        b.Beam(Sl.Metal, new Vector3(0f, 2.5f, 1.75f), new Vector3(0f, 2.28f, 1.75f), 0.03f, 0.03f);
        b.Cyl(Sl.Glow, new Vector3(0f, 1.85f, 1.75f), 0.17f, 0.42f, 6, 0.13f, true, true);
    }

    // ------------------------------------------------------------------ Peta Guild
    /// <summary>Papan Peta Guild di mulut utara kemah: bingkai dan kertas peta kosong dengan satu kata, "kosong".</summary>
    static void BnPeta(Transform parent, Material[] slots)
    {
        var g = new GameObject("PetaGuild").transform;
        g.SetParent(parent, false);
        Vector3 p = G(-5.5f, -306f);
        const float yaw = -10f;                                           // menghadap selatan desain (ke kemah)
        var b = new GeoBag();
        b.Push(p, yaw);
        foreach (float sx in new[] { -1.75f, 1.75f }) b.Beam(Sl.Wood, new Vector3(sx, -0.4f, 0f), new Vector3(sx, 3.7f, 0f), 0.22f, 0.22f);
        b.Box(Sl.Wood, new Vector3(0f, 3.38f, 0f), new Vector3(3.9f, 0.24f, 0.24f));
        b.Box(Sl.Wood, new Vector3(0f, 1.02f, 0f), new Vector3(3.9f, 0.24f, 0.24f));
        b.Box(Sl.Hedge, new Vector3(0f, 2.2f, 0.02f), new Vector3(3.3f, 2.15f, 0.05f));                            // kertas peta
        b.Box(Sl.WoodLight, new Vector3(0f, 3.26f, 0.07f), new Vector3(3.4f, 0.07f, 0.07f));
        b.Box(Sl.WoodLight, new Vector3(0f, 1.14f, 0.07f), new Vector3(3.4f, 0.07f, 0.07f));
        foreach (float sx in new[] { -1.66f, 1.66f }) b.Box(Sl.WoodLight, new Vector3(sx, 2.2f, 0.07f), new Vector3(0.07f, 2.2f, 0.07f));
        b.QuadBoth(Sl.Field, new Vector3(0f, 3.1f, 0.075f), new Vector3(0.17f, 2.93f, 0.075f), new Vector3(0f, 2.76f, 0.075f), new Vector3(-0.17f, 2.93f, 0.075f));   // segel Guild
        b.Gable(Sl.Roof, Sl.Wood, Sl.Wood, new Vector3(0f, 3.5f, 0f), 4.6f, 1.5f, 0.75f, 0.25f, true);
        // lampu kecil di sisi kanan papan
        b.Beam(Sl.Metal, new Vector3(1.75f, 3.1f, 0f), new Vector3(2.35f, 3.1f, 0.3f), 0.05f, 0.05f);
        b.Cyl(Sl.Glow, new Vector3(2.35f, 2.55f, 0.3f), 0.17f, 0.45f, 6, 0.13f, true, true);
        b.Pop();
        b.Emit(g, "PetaGuildPapan", slots);
        BnCol(g, "PetaGuildCol", BnLw(p, yaw, 0f, 2.0f, 0f), new Vector3(3.9f, 4.0f, 0.5f), yaw);

        Color ink = new Color(0.2f, 0.14f, 0.09f, 1f);
        BuildKit.Text(g, "PETA GUILD: PADANG HANTU", BnLw(p, yaw, 0f, 2.58f, 0.1f), yaw + 180f, 0.024f, ink);
        BuildKit.Text(g, "kosong", BnLw(p, yaw, 0f, 2.0f, 0.1f), yaw + 180f, 0.085f, ink);
        ELight(g, BnLw(p, yaw, 2.35f, 2.9f, 0.9f), new Color(1f, 0.8f, 0.5f), 8f, 0.7f);

        BnInfo(g, "InteractPoint_PetaGuild", BnLw(p, yaw, 0f, 0.1f, 2.7f), "Periksa peta Guild", 3.6f, "Peta Guild",
            "Peta Guild untuk dataran di utara pos ini sama besarnya dengan peta daerah lain, dan sama rapi bingkainya. Hanya isinya satu kata, " +
            "ditulis tangan di tengah kertas yang kosong: kosong.\n\n" +
            "Tak ada garis jalan, tak ada nama bukit, tak ada tanda bahaya. Penjaga pos menyodorkan pena kepada pemburu yang mau menambahkan sesuatu. " +
            "Belum ada yang mau.");
    }
}
