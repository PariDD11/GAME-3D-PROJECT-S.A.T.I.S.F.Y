using UnityEngine;
using HunterGuild;

/// <summary>
/// Anjing kuning kurus di anak tangga toko roti (kanon §8.6). Dirakit dari tabung dan gumpalan ForestKit di kerangka toko roti, jadi ikut
/// pindah bila bangunannya dipindah. Duduk, tegak, telinga naik, menatap retakan kelopak di jalan (arah dihitung dari pusat retakan).
/// Kurus: badan sempit, tulang pinggul menonjol, kaki panjang. Bulu memakai slot Sl.Fur yang ditimpa kuning (EcLilin); hidung dan mata Sl.Dark.
/// </summary>
public static partial class TownSceneBuilder
{
    /// <summary>Tempat duduk di kerangka toko roti: separuh kanan anak tangga, agar pintu tetap kelihatan.</summary>
    static Vector3 EcDogSeat() { return new Vector3(LlDoorX + 0.3f, 0.22f, LlD / 2f + 0.3f); }

    static void EcDogLocal(GeoBag b, int shop)
    {
        Vector3 seat = EcDogSeat();
        float az = LlAz[shop] * Mathf.Deg2Rad;
        Vector2 cr = AtAz(LlCrackAz, LlCrackR) - AtAz(LlAz[shop], LlRowR);
        float lx = cr.x * Mathf.Cos(az) - cr.y * Mathf.Sin(az);          // pusat retakan di kerangka toko
        float lz = cr.x * Mathf.Sin(az) + cr.y * Mathf.Cos(az);
        float yaw = Mathf.Atan2(lx - seat.x, lz - seat.z) * Mathf.Rad2Deg;
        b.Push(seat, yaw);
        EcDog(b);
        b.Pop();
    }

    /// <summary>Anjing duduk menghadap +Z lokal; titik asal di tanah tepat di bawah pantat.</summary>
    static void EcDog(GeoBag b)
    {
        const int F = Sl.Fur;
        // pantat dan badan (tabung sempit dari pinggul sampai dada)
        ForestKit.Blob(b, F, new Vector3(0f, 0.13f, -0.03f), new Vector3(0.09f, 0.10f, 0.13f), 3, 0.08f, 4, 8);
        ForestKit.Tube(b, F,
            new[] { new Vector3(0f, 0.16f, -0.03f), new Vector3(0f, 0.26f, 0.07f), new Vector3(0f, 0.36f, 0.18f), new Vector3(0f, 0.42f, 0.26f) },
            new[] { 0.078f, 0.078f, 0.088f, 0.074f }, 8, 7, 0f, true);
        // leher dan kepala terangkat
        ForestKit.Tube(b, F,
            new[] { new Vector3(0f, 0.42f, 0.26f), new Vector3(0f, 0.52f, 0.32f), new Vector3(0f, 0.60f, 0.37f) },
            new[] { 0.064f, 0.052f, 0.046f }, 7, 9, 0f, true);
        ForestKit.Blob(b, F, new Vector3(0f, 0.64f, 0.41f), new Vector3(0.05f, 0.05f, 0.062f), 4, 0.08f, 4, 8);
        ForestKit.Tube(b, F,
            new[] { new Vector3(0f, 0.63f, 0.44f), new Vector3(0f, 0.62f, 0.50f), new Vector3(0f, 0.615f, 0.555f) },
            new[] { 0.037f, 0.03f, 0.025f }, 7, 10, 0f, true);
        b.Ball(Sl.Dark, new Vector3(0f, 0.617f, 0.565f), 0.016f, 4, 6);                                  // hidung

        foreach (float s in new[] { -1f, 1f })
        {
            int sd = s < 0f ? 0 : 1;
            // mata
            b.Ball(Sl.Dark, new Vector3(s * 0.036f, 0.655f, 0.44f), 0.0105f, 3, 6);
            // telinga tegak (segitiga dua sisi)
            Vector3 ea = new Vector3(s * 0.02f, 0.675f, 0.40f), eb = new Vector3(s * 0.058f, 0.655f, 0.42f), et = new Vector3(s * 0.055f, 0.745f, 0.385f);
            b.Tri(F, ea, eb, et);
            b.Tri(F, ea, et, eb);
            // tulang pinggul menonjol
            ForestKit.Blob(b, F, new Vector3(s * 0.085f, 0.225f, 0.0f), new Vector3(0.032f, 0.03f, 0.04f), 5 + sd, 0.1f, 3, 6);
            // kaki belakang saat duduk: paha ke depan, betis ke belakang, telapak rata di tanah
            ForestKit.Limb(b, F, new Vector3(s * 0.085f, 0.21f, 0.0f), new Vector3(s * 0.115f, 0.15f, 0.20f), 0.055f, 0.034f, 11 + sd, 6, 0.04f);
            ForestKit.Limb(b, F, new Vector3(s * 0.115f, 0.15f, 0.20f), new Vector3(s * 0.10f, 0.04f, 0.02f), 0.032f, 0.022f, 13 + sd, 6, 0.04f);
            ForestKit.Limb(b, F, new Vector3(s * 0.10f, 0.04f, 0.02f), new Vector3(s * 0.10f, 0.025f, 0.21f), 0.022f, 0.02f, 15 + sd, 5, 0f);
            // kaki depan lurus dan panjang, telapak kecil
            ForestKit.Limb(b, F, new Vector3(s * 0.06f, 0.38f, 0.25f), new Vector3(s * 0.06f, 0.05f, 0.31f), 0.04f, 0.024f, 17 + sd, 6, 0.03f);
            ForestKit.Blob(b, F, new Vector3(s * 0.06f, 0.022f, 0.345f), new Vector3(0.03f, 0.022f, 0.05f), 19 + sd, 0.05f, 3, 6);
        }

        // ekor tipis tergeletak melingkar di tanah
        ForestKit.Tube(b, F,
            new[] { new Vector3(0f, 0.09f, -0.11f), new Vector3(0.06f, 0.05f, -0.22f), new Vector3(0.17f, 0.03f, -0.29f), new Vector3(0.27f, 0.025f, -0.25f) },
            new[] { 0.03f, 0.024f, 0.018f, 0.012f }, 5, 21, 0f, true);
    }

    /// <summary>Titik periksa di depan toko roti (anjing dan retakan). Anak dari akar toko, jadi ikut pindah bersamanya.</summary>
    static void EcBakeryInfo(Transform shop, int i)
    {
        float hd = LlD / 2f;
        EcInfo(shop, "InteractPoint_TokoRoti", LlWorld(i, LlDoorX - 0.3f, 0.14f, hd + 1.2f), "Periksa toko roti", 2.8f, "Toko Roti dan Anjing Kuning",
            "Anjing kuning yang kurus ini duduk di anak tangga toko roti. Pemilik toko berkata ia datang tanpa ada yang memanggil, tidak pernah mengemis, " +
            "dan menolak roti yang disodorkan padanya. Ia tidak menatap pintu, tidak menatap orang yang lewat. Ia menatap batu jalan yang retak di depan toko, " +
            "yang kini dilingkari tali merah dan enam lilin.\n\n" +
            "\"Dia sudah begitu sejak batu itu retak,\" kata pemilik toko. \"Telinganya bergerak setiap kali tanah di bawah kakimu berdenyut. " +
            "Aku sendiri tidak pernah merasakannya.\"", true);
    }
}
