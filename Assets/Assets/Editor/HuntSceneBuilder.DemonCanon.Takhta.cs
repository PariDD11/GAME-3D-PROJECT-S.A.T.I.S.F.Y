using UnityEngine;
using HunterGuild;

/// <summary>
/// Kanon Lembah Iblis, bagian 3: seratus anak tangga dan Takhta Iblis. Anak tangga datar buatan StoryScenery (desain x 0, z 212 sampai 272, lebar 7 m)
/// diberi tepian obsidian rendah dan dua puluh tiang berpucuk bara di kiri-kanan, sejajar lampu-lampu runtime tiap sepuluh anak tangga. Takhta berdiri di balik
/// arena Zargoth (boss di desain (0, 320)): pelataran 20 x 18 m di (0, 346) menghadap selatan, dua tingkat, takhta obsidian dengan sandaran 9,6 m, lima tanduk,
/// retakan berbara, dan di dudukannya cekungan bundar pendar ungu pucat seukuran cap. Obelisk dan lampu ungu lama di (+-12, 341) tetap berdiri di kiri-kanannya.
/// </summary>
public static partial class HuntSceneBuilder
{
    static void DmJalan(Transform root, Material[] slots)
    {
        var g = new GameObject("JalanSeratus").transform;
        g.SetParent(root, false);
        var b = new GeoBag();
        for (int side = -1; side <= 1; side += 2)
        {
            float wx = -side * 4.4f;                                                                       // desain x = +-4,4
            for (int k = 0; k < 20; k++)
            {
                float zc = -(212f + k * 3f + 1.5f), y = GroundYW(wx, zc);
                b.Box(Sl.Dark, new Vector3(wx, y + 0.18f, zc), new Vector3(0.5f, 0.56f, 3.04f));
            }
            for (int k = 0; k < 10; k++)
            {
                float zc = -(217.7f + k * 6f), y = GroundYW(wx, zc);
                b.Cyl(Sl.Dark, new Vector3(wx, y - 0.15f, zc), 0.34f, 2.15f, 6, 0.26f, true);
                b.Box(Sl.Glow, new Vector3(wx, y + 2.05f, zc), new Vector3(0.36f, 0.12f, 0.36f));
                dmPosts++;
            }
        }
        b.Emit(g, "TepianTangga", slots);
        BnInfo(g, "InteractPoint_SeratusTangga", G(0f, 207f) + new Vector3(0f, 0.1f, 0f), "Periksa anak tangga", 7f, "Seratus Anak Tangga",
            "Anak tangga obsidian ini dihitung seratus oleh setiap orang yang menaikinya, tetapi tak dua orang pun mendapat hitungan yang sama di tengah jalan. " +
            "Tak satu pun bergetar di bawah kaki, bahkan ketika tanah di dataran sekitarnya berguncang, dan lampu ungu di sisinya tak pernah berkedip.\n\n" +
            "Penjaga gerbang lembah berkata tangga ini sudah begitu sebelum lembah ini diberi nama.");
    }

    static void DmTakhta(Transform root, Material[] slots)
    {
        var g = new GameObject("TakhtaIblis").transform;
        g.SetParent(root, false);
        Vector3 c = G(0f, 346f);                                                                           // dunia (0, -346); +Z lokal menghadap selatan desain
        float lo, hi;
        BnSpan(c.x, c.z, 10f, 9f, out lo, out hi);
        float top = hi + 0.12f;
        Vector3 o = new Vector3(c.x, top, c.z);
        float fd = top - (lo - 0.8f);
        dmCleared += BnClearBox(o.x - 10.2f, o.x + 10.2f, o.z - 9.6f, o.z + 9.6f);                         // satu tiang lingkar lama di (0, 352) tercabut; obelisk di +-12 tetap
        dmMoved += BnMoveMonsters(o.x - 10.2f, o.x + 10.2f, o.z - 9.6f, o.z + 9.6f, o.z + 14f);

        var b = new GeoBag();
        b.Push(o, 0f);
        b.BoxB(Sl.DarkStone, new Vector3(0f, -fd, 0f), new Vector3(20f, fd, 18f));                          // pelataran + fondasi
        b.BoxB(Sl.Stone, new Vector3(0f, 0f, -1.2f), new Vector3(15.6f, 0.7f, 12f));                        // tingkat A
        b.BoxB(Sl.DarkStone, new Vector3(0f, 0.7f, -2.4f), new Vector3(10.4f, 0.7f, 8.4f));                 // tingkat B, puncak 1,4 m
        b.Stairs(Sl.Stone, new Vector3(0f, 0f, 6.97f), 6f, 4, 0.175f, 0.62f, 0f);
        b.Stairs(Sl.DarkStone, new Vector3(0f, 0.7f, 3.97f), 4.4f, 4, 0.175f, 0.62f, 0f);

        // takhta: dudukan, cekungan cap, sandaran, tanduk, retakan berbara, sandaran tangan
        b.BoxB(Sl.DarkStone, new Vector3(0f, 1.4f, -4.0f), new Vector3(5.0f, 0.8f, 3.8f));                  // dudukan, puncak 2,2 m
        b.Annulus(ForestKit.Crystal, new Vector3(0f, 0f, -3.9f), 0.34f, 0.5f, 0f, 360f, 24, 2.215f);        // cekungan bundar di tempat pangkuan, berpendar ungu pucat
        b.Disc(Sl.Dark, new Vector3(0f, 0f, -3.9f), 0.34f, 24, 2.212f);
        b.BoxB(Sl.Dark, new Vector3(0f, 2.2f, -5.45f), new Vector3(5.4f, 7.4f, 0.9f));                      // sandaran, puncak 9,6 m
        b.Cone(Sl.Dark, new Vector3(0f, 9.6f, -5.45f), 0.6f, 3.4f, 5);
        foreach (float sx in new[] { -1f, 1f })
        {
            b.PushTRS(new Vector3(sx * 2.35f, 9.5f, -5.45f), Quaternion.Euler(0f, 0f, -sx * 18f), 1f);
            b.Cone(Sl.Dark, Vector3.zero, 0.5f, 4.8f, 5);
            b.Pop();
            b.PushTRS(new Vector3(sx * 1.2f, 9.55f, -5.45f), Quaternion.Euler(0f, 0f, -sx * 9f), 1f);
            b.Cone(Sl.Dark, Vector3.zero, 0.42f, 3.8f, 5);
            b.Pop();
            b.BoxB(Sl.DarkStone, new Vector3(sx * 2.55f, 2.2f, -3.9f), new Vector3(0.9f, 1.5f, 3.0f));
            b.Ball(Sl.Marble, new Vector3(sx * 2.55f, 3.95f, -2.65f), 0.34f, 4, 8, 1f);
        }
        float[][] cr = { new[] { -1.4f, -1.2f, -1.5f, -1.25f, -1.4f }, new[] { 0.2f, 0.4f, 0.1f, 0.35f, 0.2f }, new[] { 1.6f, 1.4f, 1.65f, 1.45f, 1.6f } };
        for (int k = 0; k < 3; k++)
            for (int i = 0; i < 4; i++)
            {
                float y0 = 2.9f + i * 1.5f, y1 = y0 + 1.5f;
                b.QuadBoth(Sl.Glow, new Vector3(cr[k][i] - 0.05f, y0, -4.98f), new Vector3(cr[k][i] + 0.05f, y0, -4.98f),
                    new Vector3(cr[k][i + 1] + 0.05f, y1, -4.98f), new Vector3(cr[k][i + 1] - 0.05f, y1, -4.98f));
            }
        foreach (float sx in new[] { -6.2f, 6.2f })                                                         // dua perapian di tingkat A
        {
            b.Cyl(Sl.DarkStone, new Vector3(sx, 0.7f, -1.5f), 0.45f, 1.4f, 8, 0.35f, true);
            b.Cyl(Sl.DarkStone, new Vector3(sx, 2.1f, -1.5f), 0.8f, 0.45f, 10, 1.05f, true);
            b.Cone(Sl.Glow, new Vector3(sx, 2.5f, -1.5f), 0.72f, 1.2f, 6);
        }
        float drop = top - GroundYW(o.x, o.z + 11f);                                                        // tangga dari tanah ke pelataran bila perlu
        if (drop > 0.25f)
        {
            int n = Mathf.CeilToInt(drop / 0.2f);
            const float run = 0.6f;
            b.Stairs(Sl.Stone, new Vector3(0f, -drop, 9f + run / 2f + (n - 1) * run), 8f, n, drop / n, run, 0f);
        }
        b.Pop();
        var go = b.Emit(g, "MeshTakhta", slots);
        go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;

        Color ember = new Color(1f, 0.5f, 0.18f);
        ELight(g, o + new Vector3(-6.2f, 3.3f, -1.5f), ember, 20f, 1.5f);
        ELight(g, o + new Vector3(6.2f, 3.3f, -1.5f), ember, 20f, 1.5f);
        ELight(g, o + new Vector3(0f, 3.2f, 1.2f), new Color(0.75f, 0.2f, 0.95f), 24f, 1.3f, false);        // ungu, serasi dengan obelisk lama

        BnInfo(g, "InteractPoint_Takhta", new Vector3(o.x, top + 0.1f, o.z + 8.2f), "Periksa takhta", 8f, "Takhta Iblis",
            "Takhta ini terlalu tinggi untuk satu orang dan terlalu sepi untuk yang lain. Sandarannya hitam dan retak, dan dari retakan itu naik cahaya jingga yang tak berkedip. " +
            "Di dudukannya ada cekungan bundar dangkal seukuran cap segel, tepat di tempat pangkuan akan berada. Tak ada debu di dalamnya.\n\n" +
            "Pemburu yang pulang dari sini tak mau bercerita siapa yang biasa duduk di sana. Mereka hanya bilang kursi itu selalu terasa hangat.");
    }
}
