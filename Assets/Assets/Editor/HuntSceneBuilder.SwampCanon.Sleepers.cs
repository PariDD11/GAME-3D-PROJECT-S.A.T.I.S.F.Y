using UnityEngine;

/// <summary>
/// Kursi kayu dan penidur bermata terbuka untuk Merana (kanon §8.5: "tiga ratus warga tidur dengan mata terbuka di kursi masing-masing").
/// Bingkai lokal: titik asal di tanah, depan = +Z (arah menara kapel). Semuanya dibangun ke GeoBag pemanggil.
/// </summary>
public static partial class HuntSceneBuilder
{
    static int swLimbSeed;

    /// <summary>
    /// Larik bahan untuk kursi dan penidur. Larik rawa meminjam slot kulit, rambut, mata, dan mantel untuk kayu, daun, dan batu (lihat
    /// ForestKit.cs), jadi slot yang dipakai penidur ditimpa di salinan ini dengan warna tertentu: kulit pucat, rambut gelap, mata putih,
    /// pupil hitam, dan tiga warna baju kusam.
    /// </summary>
    static Material[] SwSleeperSlots()
    {
        var s = (Material[])ForestKit.SSlots.Clone();
        s[Sl.SkinS] = DcMat("RawaKulitPenidur", new Color(0.80f, 0.72f, 0.62f, 1f), 0.08f);
        s[Sl.EyeW] = DcMat("RawaMataPenidur", new Color(0.95f, 0.94f, 0.90f, 1f), 0.35f);
        s[Sl.Dark] = DcMat("RawaPupilPenidur", new Color(0.05f, 0.05f, 0.06f, 1f), 0.5f);
        s[Sl.Hair] = DcMat("RawaRambutPenidur", new Color(0.17f, 0.13f, 0.10f, 1f), 0.05f);
        s[Sl.CoatA] = DcMat("RawaBajuA", new Color(0.50f, 0.44f, 0.34f, 1f), 0.03f);
        s[Sl.CoatB] = DcMat("RawaBajuB", new Color(0.34f, 0.30f, 0.26f, 1f), 0.03f);
        s[Sl.CoatC] = DcMat("RawaBajuC", new Color(0.30f, 0.36f, 0.40f, 1f), 0.03f);
        return s;
    }

    static void SwLimb(GeoBag b, int slot, Vector3 a, Vector3 c, float r0, float r1)
    {
        ForestKit.Limb(b, slot, a, c, r0, r1, ++swLimbSeed, 6, 0f);
    }

    static void SwChair(GeoBag b)
    {
        b.Box(Sl.Wood, new Vector3(0f, 0.45f, 0f), new Vector3(0.48f, 0.05f, 0.46f));
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                b.BoxB(Sl.Wood, new Vector3(sx * 0.2f, 0f, sz * 0.19f), new Vector3(0.045f, 0.45f, 0.045f));
        b.BoxB(Sl.Wood, new Vector3(-0.2f, 0.45f, -0.21f), new Vector3(0.04f, 0.62f, 0.04f));
        b.BoxB(Sl.Wood, new Vector3(0.2f, 0.45f, -0.21f), new Vector3(0.04f, 0.62f, 0.04f));
        b.Box(Sl.Wood, new Vector3(0f, 1.0f, -0.21f), new Vector3(0.46f, 0.07f, 0.04f));
        b.Box(Sl.Wood, new Vector3(0f, 0.74f, -0.21f), new Vector3(0.40f, 0.05f, 0.03f));
    }

    /// <summary>Penidur duduk tegak sedikit bersandar, tangan di lutut, kepala sedikit menengadah ke menara, mata terbuka.</summary>
    static void SwSleeper(GeoBag b, int i)
    {
        int[] cl = { Sl.Cloth, Sl.ClothB, Sl.CoatA, Sl.CoatB, Sl.CoatC, Sl.Leather };
        int top = cl[(int)(DcHash(i, 41) * cl.Length) % cl.Length];
        int low = cl[(int)(DcHash(i, 43) * cl.Length) % cl.Length];
        // paha dan betis, sepatu
        foreach (float sx in new[] { -1f, 1f })
        {
            SwLimb(b, low, new Vector3(sx * 0.11f, 0.54f, -0.03f), new Vector3(sx * 0.12f, 0.52f, 0.40f), 0.085f, 0.07f);
            SwLimb(b, low, new Vector3(sx * 0.12f, 0.52f, 0.40f), new Vector3(sx * 0.12f, 0.07f, 0.46f), 0.065f, 0.05f);
            b.Box(Sl.Leather, new Vector3(sx * 0.12f, 0.03f, 0.53f), new Vector3(0.09f, 0.06f, 0.2f));
        }
        // badan dan leher
        SwLimb(b, top, new Vector3(0f, 0.60f, -0.06f), new Vector3(0f, 1.10f, -0.13f), 0.17f, 0.2f);
        SwLimb(b, Sl.SkinS, new Vector3(0f, 1.10f, -0.13f), new Vector3(0f, 1.24f, -0.135f), 0.06f, 0.055f);
        // lengan: tangan bertumpu di lutut
        foreach (float sx in new[] { -1f, 1f })
        {
            SwLimb(b, top, new Vector3(sx * 0.21f, 1.05f, -0.12f), new Vector3(sx * 0.26f, 0.80f, -0.04f), 0.055f, 0.047f);
            SwLimb(b, top, new Vector3(sx * 0.26f, 0.80f, -0.04f), new Vector3(sx * 0.17f, 0.62f, 0.22f), 0.047f, 0.04f);
            b.Ball(Sl.SkinS, new Vector3(sx * 0.17f, 0.60f, 0.25f), 0.04f, 3, 6, 1f);
        }
        // kepala menengadah 14 derajat
        b.PushTRS(new Vector3(0f, 1.36f, -0.13f), Quaternion.Euler(-14f, 0f, 0f), 1f);
        b.Ball(Sl.SkinS, Vector3.zero, 0.115f, 5, 8, 1.08f);
        b.Ball(Sl.Hair, new Vector3(0f, 0.035f, -0.03f), 0.122f, 4, 8, 0.95f);
        b.Ball(Sl.SkinS, new Vector3(0f, -0.015f, 0.113f), 0.018f, 3, 5, 1f);
        foreach (float sx in new[] { -1f, 1f })
        {
            b.Ball(Sl.EyeW, new Vector3(sx * 0.042f, 0.015f, 0.10f), 0.02f, 3, 6, 1f);          // mata terbuka lebar
            b.Ball(Sl.Dark, new Vector3(sx * 0.042f, 0.018f, 0.113f), 0.011f, 3, 6, 1f);
        }
        b.Pop();
    }
}
