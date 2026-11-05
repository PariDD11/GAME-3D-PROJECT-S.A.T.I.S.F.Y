using System.Collections.Generic;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Kanon Lembah Iblis (HuntArea_Demon; lihat Docs/LORE_BIBLE.md §8.8). Dipanggil dari BuildEldrathAreaCore() (hanya ea == 3) sesudah EBuildEntrances()
/// dan sebelum BuildPlayerAndManagers(), dan TIDAK memakai UnityEngine.Random (hash BnHash), supaya sebaran yang sudah ada tidak bergeser.
/// Koordinat desain (x, z) dicerminkan ke dunia oleh G(); pembantu BnInfo, BnCol, BnHash, BnSpan, BnClearBox, BnMoveMonsters, BnObstacles, BnFree
/// dipakai ulang dari HuntSceneBuilder.BoneCanon*.cs.
///
///  1. Jantung Obsidian: dua tiang obsidian bersandar dengan celah berbara, busur pecahan, tungku roti hangus, bawang, jemuran. (file ini)
///  2. Dataran Retakan: retakan berbara, sisik batu, kawah belerang. (.Retakan.cs)
///  3. Seratus anak tangga: tepian dan tiang obsidian; Takhta Iblis: pelataran dan takhta di balik arena Zargoth. (.Takhta.cs)
///  4. Altar Ritual: dua lingkaran serbuk putih yang terbuka di satu sisi, tiang gerbang berlingkaran kosong. (.Altar.cs)
///
/// Pondok Varra, dapur, imp, seratus anak tangga datar, Telinga, peziarah (StoryScenery.BuildDemon, saat runtime) dan Zargoth (builder) tidak diubah.
/// Tidak ada NPC atau mekanik baru; hanya benda, cahaya, dan teks. Teks tidak memakai "yang ketujuh", Jeda, si Sulung, Lucifer, nama dewa, atau nama wadah.
/// </summary>
public static partial class HuntSceneBuilder
{
    static int dmShards, dmFissures, dmScales, dmVents, dmPosts, dmLoaves, dmCleared, dmMoved, dmRings;      // hitungan untuk uji

    static void DemonCanon()
    {
        var root = Group("KanonLembahIblis");
        dmShards = 0; dmFissures = 0; dmScales = 0; dmVents = 0; dmPosts = 0; dmLoaves = 0; dmCleared = 0; dmMoved = 0; dmRings = 0;
        var slots = DmSlots();
        DmJantung(root, slots);
        DmRetakan(root, slots);
        DmJalan(root, slots);
        DmTakhta(root, slots);
        DmAltar(root, slots);
    }

    /// <summary>Slot khusus kanon (salinan eSlots; benda lama tidak ikut berubah).</summary>
    static Material[] DmSlots()
    {
        var s = (Material[])eSlots.Clone();
        s[Sl.Dark] = Mat("DmObsidian", new Color(0.05f, 0.04f, 0.085f));                                              // obsidian
        s[Sl.Glow] = TownMats.Emissive(Mat("DmBara", new Color(0.6f, 0.18f, 0.05f)), new Color(1.1f, 0.32f, 0.06f));    // bara, lebih redup daripada lava
        s[Sl.Hedge] = Mat("DmSerbuk", new Color(0.84f, 0.82f, 0.78f));                                                 // serbuk putih
        s[Sl.Field] = Mat("DmBelerang", new Color(0.72f, 0.62f, 0.14f));                                               // kerak belerang
        s[Sl.FieldB] = Mat("DmRotiGosong", new Color(0.1f, 0.06f, 0.04f));
        s[Sl.Cloth] = Mat("DmCelemek", new Color(0.66f, 0.58f, 0.46f));
        s[Sl.ClothB] = Mat("DmKaosKaki", new Color(0.72f, 0.12f, 0.1f));
        s[Sl.Leather] = Mat("DmBawang", new Color(0.62f, 0.4f, 0.16f));
        return s;
    }

    static float DmH(int a, int b) { return BnHash(a, b); }

    /// <summary>Tinggi tanah di (lx, lz) relatif terhadap titik asal o (bingkai tanpa putaran).</summary>
    static float DmLy(Vector3 o, float lx, float lz) { return GroundYW(o.x + lx, o.z + lz) - o.y; }

    /// <summary>Titik desain (x, z) layak dipakai: di dalam peta, di atas tanah kering, jauh dari jalan setapak.</summary>
    static bool DmOk(float x, float z)
    {
        return Mathf.Abs(x) < 420f && Mathf.Abs(z) < 420f && GroundY(x, z) > WaterY + 0.8f && DistToTrails(x, z) > 2.5f;
    }

    /// <summary>Satu keping datar mengikuti tanah, koordinat DUNIA; dy = tinggi di atas tanah.</summary>
    static void DmStrip(GeoBag b, int slot, float wx0, float wz0, float wx1, float wz1, float w0, float w1, float dy)
    {
        float dx = wx1 - wx0, dz = wz1 - wz0, l = Mathf.Sqrt(dx * dx + dz * dz);
        if (l < 0.01f) return;
        float nx = -dz / l, nz = dx / l;
        float ax = wx0 + nx * w0 * 0.5f, az = wz0 + nz * w0 * 0.5f, bx = wx0 - nx * w0 * 0.5f, bz = wz0 - nz * w0 * 0.5f;
        float cx = wx1 - nx * w1 * 0.5f, cz = wz1 - nz * w1 * 0.5f, ex = wx1 + nx * w1 * 0.5f, ez = wz1 + nz * w1 * 0.5f;
        b.QuadBoth(slot, new Vector3(ax, GroundYW(ax, az) + dy, az), new Vector3(bx, GroundYW(bx, bz) + dy, bz),
            new Vector3(cx, GroundYW(cx, cz) + dy, cz), new Vector3(ex, GroundYW(ex, ez) + dy, ez));
    }

    /// <summary>Pita mengikuti tanah, dipecah tiap sekitar 3 m, lebar berubah linear dari w0 ke w1 (koordinat DUNIA).</summary>
    static void DmRibbon(GeoBag b, int slot, float wx0, float wz0, float wx1, float wz1, float w0, float w1, float dy)
    {
        float dx = wx1 - wx0, dz = wz1 - wz0;
        int n = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(dx * dx + dz * dz) / 3f));
        for (int i = 0; i < n; i++)
        {
            float t0 = (float)i / n, t1 = (float)(i + 1) / n;
            DmStrip(b, slot, wx0 + dx * t0, wz0 + dz * t0, wx0 + dx * t1, wz0 + dz * t1, Mathf.Lerp(w0, w1, t0), Mathf.Lerp(w0, w1, t1), dy);
        }
    }

    // ================================================================== Jantung Obsidian
    static void DmJantung(Transform root, Material[] slots)
    {
        var g = new GameObject("JantungObsidian").transform;
        g.SetParent(root, false);
        Vector3 hut = G(-18f, -190f);                                      // pondok Varra (StoryScenery): kubus 6 x 3 x 5, pintu menghadap dunia -z
        dmCleared += BnClearBox(hut.x - 5f, hut.x + 11f, hut.z - 8f, hut.z + 14f);
        var obst = BnObstacles();
        var b = new GeoBag();
        b.Push(hut, 0f);

        // tungku roti di sisi pondok (dunia +x) dan rak roti hangus di depannya
        float oy = DmLy(hut, 7.2f, -0.8f);
        b.Cyl(Sl.DarkStone, new Vector3(7.2f, oy - 0.25f, -0.8f), 1.75f, 0.75f, 10, 1.65f, true);
        b.Ball(Sl.Stone, new Vector3(7.2f, oy + 0.5f, -0.8f), 1.5f, 5, 10, 0.75f);
        b.Box(Sl.Dark, new Vector3(7.2f, oy + 0.55f, -2.2f), new Vector3(1.0f, 0.8f, 0.5f));
        b.Box(Sl.Glow, new Vector3(7.2f, oy + 0.4f, -2.45f), new Vector3(0.62f, 0.26f, 0.08f));
        b.Cyl(Sl.Stone, new Vector3(7.5f, oy + 1.1f, 0.2f), 0.38f, 2.2f, 8, 0.3f, true);
        float ry = DmLy(hut, 7.2f, -5.2f);
        foreach (float lx in new[] { -0.9f, 0.9f })
            foreach (float lz in new[] { -0.45f, 0.45f })
                b.Box(Sl.Wood, new Vector3(7.2f + lx, ry + 0.48f, -5.2f + lz), new Vector3(0.1f, 0.96f, 0.1f));
        b.Box(Sl.Wood, new Vector3(7.2f, ry + 0.98f, -5.2f), new Vector3(2.0f, 0.08f, 1.0f));
        for (int i = 0; i < 8; i++)                                        // delapan roti hangus, sama bentuknya
        {
            b.Ball(Sl.FieldB, new Vector3(7.2f - 0.72f + (i % 4) * 0.48f, ry + 1.14f, -5.2f - 0.2f + (i / 4) * 0.4f), 0.21f, 3, 6, 0.55f);
            dmLoaves++;
        }

        // bawang bergantung di muka pondok, pintu dibiarkan lapang
        float hy = DmLy(hut, 0f, -2.9f);
        b.Beam(Sl.Wood, new Vector3(-2.7f, hy + 2.8f, -2.9f), new Vector3(2.7f, hy + 2.8f, -2.9f), 0.09f, 0.09f);
        foreach (float lx in new[] { -2.7f, 2.7f }) b.Box(Sl.Wood, new Vector3(lx, hy + 2.8f, -2.7f), new Vector3(0.08f, 0.08f, 0.4f));
        int sid = 0;
        foreach (float lx in new[] { -2.2f, -1.55f, 1.55f, 2.2f })
        {
            b.Beam(Sl.Leather, new Vector3(lx, hy + 2.8f, -2.9f), new Vector3(lx, hy + 1.35f, -2.9f), 0.03f, 0.03f);
            for (int k = 0; k < 6; k++)
                b.Ball(Sl.Leather, new Vector3(lx + (k % 2 == 0 ? -0.06f : 0.06f), hy + 2.62f - k * 0.24f, -2.9f), 0.115f + 0.03f * DmH(sid++, 61), 3, 6, 0.9f);
        }

        // jemuran di belakang pondok: celemek dan kaus kaki merah kecil
        float jl = DmLy(hut, -3.6f, 4.6f), jr = DmLy(hut, 3.6f, 4.6f);
        b.Box(Sl.Wood, new Vector3(-3.6f, jl + 1.3f, 4.6f), new Vector3(0.12f, 2.6f, 0.12f));
        b.Box(Sl.Wood, new Vector3(3.6f, jr + 1.3f, 4.6f), new Vector3(0.12f, 2.6f, 0.12f));
        float yl = jl + 2.5f, yr = jr + 2.5f;
        b.Beam(Sl.Leather, new Vector3(-3.6f, yl, 4.6f), new Vector3(3.6f, yr, 4.6f), 0.03f, 0.03f);
        float ya = Mathf.Lerp(yl, yr, 0.28f);
        b.QuadBoth(Sl.Cloth, new Vector3(-1.6f, ya, 4.6f), new Vector3(-0.9f, ya, 4.6f), new Vector3(-0.9f, ya - 0.78f, 4.6f), new Vector3(-1.6f, ya - 0.78f, 4.6f));
        for (int k = 0; k < 6; k++)
        {
            float lx = 0.2f + k * 0.5f, yy = Mathf.Lerp(yl, yr, (lx + 3.6f) / 7.2f);
            b.QuadBoth(Sl.ClothB, new Vector3(lx, yy, 4.6f), new Vector3(lx + 0.14f, yy, 4.6f), new Vector3(lx + 0.14f, yy - 0.34f, 4.6f), new Vector3(lx, yy - 0.34f, 4.6f));
        }

        // jantung: dua tiang obsidian bersandar di belakang pondok, celah berbara di antaranya
        const float hz = 8.6f;
        float py = DmLy(hut, 0f, hz);
        foreach (float lx in new[] { -2.6f, 2.6f })
        {
            b.PushTRS(new Vector3(lx, DmLy(hut, lx, hz) - 0.9f, hz), Quaternion.Euler(0f, 0f, lx > 0f ? 11f : -11f), 1f);
            b.Cyl(Sl.Dark, Vector3.zero, 2.0f, 10.8f, 6, 0.45f, true);
            b.Pop();
            dmShards++;
        }
        b.Box(Sl.Glow, new Vector3(0f, py + 2.8f, hz - 0.1f), new Vector3(0.22f, 5.2f, 0.22f));
        b.Pop();

        // busur pecahan obsidian di barat dan selatan pondok; sisi utara (pintu, dapur) dan timur (jalan) dibiarkan terbuka
        for (int i = 0; i < 12; i++)
        {
            float t = (130f + 170f * (i + DmH(i, 71) * 0.7f) / 12f) * Mathf.Deg2Rad;
            float r = 10f + DmH(i, 72) * 5.5f;
            float x = -18f + Mathf.Cos(t) * r, z = -190f + Mathf.Sin(t) * r;
            if (!BnFree(obst, x, z)) continue;
            Vector3 p = G(x, z);
            float h = 2.6f + DmH(i, 73) * 4.4f, rad = 0.5f + DmH(i, 74) * 0.6f, tilt = 7f + DmH(i, 75) * 8f;
            float wdx = (hut.x - p.x), wdz = (hut.z - p.z), wl = Mathf.Max(0.01f, Mathf.Sqrt(wdx * wdx + wdz * wdz));
            wdx /= wl; wdz /= wl;                                                                         // arah dunia ke pondok: pecahan condong ke dalam
            b.PushTRS(new Vector3(p.x, p.y - 0.7f, p.z), Quaternion.Euler(tilt * wdz, 0f, -tilt * wdx), 1f);
            b.Cyl(Sl.Dark, Vector3.zero, rad, h, 5 + (i % 2), rad * 0.3f, true);
            b.Pop();
            if (rad > 0.85f) BnCol(g, "PecahanCol_" + i, p + new Vector3(0f, 1.5f, 0f), new Vector3(rad * 1.5f, 3f, rad * 1.5f), 0f);
            dmShards++;
        }
        b.Emit(g, "MeshJantung", slots);

        BnCol(g, "TungkuCol", hut + new Vector3(7.2f, oy + 1.2f, -0.8f), new Vector3(3.4f, 2.6f, 3.4f), 0f);
        BnCol(g, "JantungCol", hut + new Vector3(0f, py + 2.5f, hz), new Vector3(6.4f, 5.5f, 4.2f), 0f);
        ELight(g, hut + new Vector3(0f, py + 3.4f, hz - 0.9f), new Color(1f, 0.42f, 0.12f), 20f, 1.5f);                // celah berbara
        ELight(g, hut + new Vector3(7.2f, oy + 1.3f, -3.4f), new Color(1f, 0.5f, 0.2f), 10f, 1.1f);                    // mulut tungku

        BnInfo(g, "InteractPoint_Jantung", hut + new Vector3(0f, DmLy(hut, 0f, 6f) + 0.1f, 6f), "Periksa batu hitam", 6.5f, "Jantung Obsidian",
            "Dua tiang obsidian hitam bersandar satu sama lain di belakang pondok, dan di celah sempit di antaranya ada cahaya jingga yang berdenyut pelan, " +
            "seperti bara yang tak pernah padam dan tak pernah membesar. Batu di sekitarnya licin dan hangat, seolah baru saja disentuh orang.\n\n" +
            "Para pemburu yang lewat berkata mereka tak pernah melihat Varra mencari kayu bakar, dan tak pernah melihat tumpukan kayunya berkurang.");
        BnInfo(g, "InteractPoint_TungkuRoti", hut + new Vector3(7.2f, DmLy(hut, 7.2f, -3.8f) + 0.1f, -3.8f), "Periksa tungku", 4.2f, "Tungku Roti",
            "Tungku batu pendek dengan mulut yang masih hangat. Di rak depannya berderet delapan roti yang hangus sampai hitam, sama bentuknya satu dengan yang lain. " +
            "Bau gosongnya sampai ke tepi jurang, bercampur bau bawang yang bergantung di muka pondok.\n\n" +
            "Tak seorang pun tahu siapa yang memanggangnya atau untuk siapa. Kata pemburu yang pernah bertanya, satu roti selalu hilang dari rak sebelum ia selesai bertanya.");
    }
}
