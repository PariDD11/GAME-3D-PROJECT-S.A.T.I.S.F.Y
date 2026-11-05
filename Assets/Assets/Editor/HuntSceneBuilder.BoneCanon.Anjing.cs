using UnityEngine;
using HunterGuild;

/// <summary>
/// Kanon Padang Hantu, bagian 4: Gerbang Cerberus. Gerbang batu (Archway) di desain (0, 262) dan monster Cerberus sudah ada; di sini ditambah dua anjing batu
/// bertiga kepala yang duduk di kiri dan kanan jalan, tepat di depan gerbang (dunia x = +-10,6, z = -257,0), skala 1,4, menghadap pemain dan sedikit menoleh ke tengah.
/// Alas dibenamkan sampai tanah terendah di bawahnya, jadi aman di lereng. Collider kotak per patung.
/// </summary>
public static partial class HuntSceneBuilder
{
    static void BnAnjing(Transform root, Material[] slots)
    {
        var g = new GameObject("AnjingGerbang").transform;
        g.SetParent(root, false);
        var b = new GeoBag();
        const float hs = 1.4f;                                                                               // skala patung (tinggi total sekitar 6,7 m)
        for (int k = 0; k < 2; k++)
        {
            float sx = k == 0 ? -1f : 1f;
            float wx = sx * 10.6f, wz = -257.0f, lo, hi;
            BnSpan(wx, wz, 1.65f * hs, 1.85f * hs, out lo, out hi);
            bnCleared += BnClearBox(wx - 3.4f, wx + 3.4f, wz - 3.6f, wz + 3.6f);                              // cabut pohon atau batu lama di bawah alas
            Vector3 p = new Vector3(wx, hi + 0.05f, wz);
            float yaw = -sx * 10f;
            b.PushTRS(p, Quaternion.Euler(0f, yaw, 0f), hs);
            BnHound(b, (p.y - lo + 0.6f) / hs);
            b.Pop();
            float cy0 = lo - 0.2f, cy1 = p.y + 6.2f;                                                         // collider dari tanah terendah sampai puncak kepala
            BnCol(g, "AnjingCol_" + k, new Vector3(p.x, (cy0 + cy1) / 2f, p.z), new Vector3(3.3f * hs, cy1 - cy0, 3.7f * hs), yaw);
        }
        b.Emit(g, "AnjingBatu", slots);

        BnInfo(g, "InteractPoint_AnjingBatu", Gw(0f, -257.5f) + new Vector3(0f, 0.1f, 0f), "Periksa anjing batu", 11f, "Anjing Batu Gerbang",
            "Dua anjing batu duduk di kiri dan kanan jalan, masing-masing dengan tiga kepala yang menghadap tiga arah sekaligus. Matanya cekung dan terisi cahaya biru pucat yang tak berkedip. " +
            "Tubuh batunya lebih lebar daripada pintu mana pun di dataran ini.\n\n" +
            "Penjaga pos tak tahu siapa yang memahatnya, atau apakah patung-patung ini ada lebih dahulu daripada penjaga hidup yang berkeliaran di depan gerbang. " +
            "Katanya, yang satu selalu tampak seperti ingatan tentang yang lain.");
    }

    /// <summary>Satu anjing batu duduk, muka +Z, titik asal di tanah. dd = kedalaman alas di bawah titik asal.</summary>
    static void BnHound(GeoBag b, float dd)
    {
        b.BoxB(Sl.Stone, new Vector3(0f, -dd, 0f), new Vector3(3.0f, dd + 0.9f, 3.4f));
        b.Box(Sl.DarkStone, new Vector3(0f, 0.93f, 0f), new Vector3(3.3f, 0.14f, 3.7f));                    // lempeng atas, puncak 1,0 m
        float y = 1.0f;
        b.Ball(Sl.Stone, new Vector3(0f, y + 1.35f, 0.05f), 0.85f, 6, 12, 1.45f);                            // badan duduk
        foreach (float sx in new[] { -1f, 1f })
        {
            b.Ball(Sl.Stone, new Vector3(sx * 0.58f, y + 0.55f, -0.4f), 0.62f, 5, 10, 0.95f);               // paha belakang
            b.Cyl(Sl.Stone, new Vector3(sx * 0.4f, y, 0.72f), 0.22f, 1.45f, 8, 0.17f, false);                // kaki depan
            b.Box(Sl.Stone, new Vector3(sx * 0.4f, y + 0.07f, 0.95f), new Vector3(0.44f, 0.14f, 0.55f));     // telapak depan
            b.Box(Sl.Stone, new Vector3(sx * 0.7f, y + 0.07f, 0.1f), new Vector3(0.5f, 0.14f, 0.8f));        // telapak belakang
        }
        b.Beam(Sl.Stone, new Vector3(0f, y + 0.25f, -0.9f), new Vector3(0f, y + 0.12f, -1.55f), 0.2f, 0.16f);   // ekor
        b.Beam(Sl.Stone, new Vector3(0f, y + 2.2f, 0.1f), new Vector3(0f, y + 2.8f, 0.4f), 0.7f, 0.7f);       // leher tengah
        BnHead(b, new Vector3(0f, y + 3.0f, 0.6f), 1.2f);
        foreach (float sx in new[] { -1f, 1f })                                                              // dua kepala samping
        {
            b.Beam(Sl.Stone, new Vector3(sx * 0.45f, y + 2.0f, 0.1f), new Vector3(sx * 0.9f, y + 2.5f, 0.45f), 0.55f, 0.55f);
            BnHead(b, new Vector3(sx * 1.0f, y + 2.7f, 0.65f), 0.92f);
        }
    }

    /// <summary>Kepala menghadap +Z: tengkorak, moncong, rahang bawah, taring, hidung, dua telinga, dua mata biru pucat. s = skala.</summary>
    static void BnHead(GeoBag b, Vector3 c, float s)
    {
        b.Ball(Sl.Stone, c, 0.4f * s, 5, 10, 0.9f);
        b.Ball(Sl.Stone, c + new Vector3(0f, -0.08f * s, 0.42f * s), 0.21f * s, 4, 8, 0.8f);
        b.Box(Sl.DarkStone, c + new Vector3(0f, -0.27f * s, 0.36f * s), new Vector3(0.3f * s, 0.07f * s, 0.46f * s));      // rahang bawah
        b.Ball(Sl.Dark, c + new Vector3(0f, -0.03f * s, 0.62f * s), 0.07f * s, 3, 6);
        foreach (float sx in new[] { -1f, 1f })
        {
            b.Cyl(Sl.Marble, c + new Vector3(sx * 0.09f * s, -0.36f * s, 0.5f * s), 0.01f * s, 0.14f * s, 4, 0.035f * s, false);   // taring
            b.Cone(Sl.Stone, c + new Vector3(sx * 0.2f * s, 0.28f * s, -0.04f * s), 0.11f * s, 0.38f * s, 4);
            b.Ball(Sl.FieldB, c + new Vector3(sx * 0.17f * s, 0.07f * s, 0.34f * s), 0.065f * s, 3, 6);
        }
        b.Box(Sl.DarkStone, c + new Vector3(0f, 0.16f * s, 0.29f * s), new Vector3(0.42f * s, 0.05f * s, 0.16f * s));      // alis, rahang tampak garang
    }
}
