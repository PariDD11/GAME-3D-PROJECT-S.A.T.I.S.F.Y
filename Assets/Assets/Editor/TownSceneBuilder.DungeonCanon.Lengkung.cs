using UnityEngine;
using HunterGuild;

/// <summary>
/// Lengkung Menara (kanon §8.6: "Menara Eldrath tidak punya pintu... ketika tujuh orang berdiri di depannya, lengkung itu menghitam lalu kosong";
/// "lantai 10-30 menguji kemampuan menghitung"; "lantai 31 tidak punya cahaya"). Gerbang besar berbingkai emas di muka menara dibiarkan apa adanya
/// (tampilan lama), tetapi di teras depannya kini ada tujuh pijakan marmer berbentuk busur, aus oleh kaki orang yang menunggu, dan di sisi
/// timur teras berdiri Papan Lantai. Menghitamnya lengkung belum menjadi mekanik (hanya teks). Koordinat menara: akar diputar 180 derajat,
/// jadi lokal (x, z) = dunia (-x, -z); teras setinggi 3,65 m; muka lengkung di lokal z = 26.
/// </summary>
public static partial class TownSceneBuilder
{
    const float LkY = 3.66f;                                      // permukaan teras emas + sedikit
    static Vector3 TwWorld(float lx, float ly, float lz) { return new Vector3(-lx, ly, -lz); }

    static void EcLengkung(Transform root)
    {
        var g = Group("LengkungMenara", root);
        var b = new GeoBag();

        // tujuh pijakan: busur 8,4 m dari dasar lengkung, sudut -54..+54 derajat
        for (int i = 0; i < 7; i++)
        {
            float a = (-54f + 18f * i) * Mathf.Deg2Rad;
            Vector3 p = TwWorld(8.4f * Mathf.Sin(a), LkY, 26f + 8.4f * Mathf.Cos(a));
            b.Cyl(Sl.Stone, p, 0.64f, 0.06f, 16, 0.62f, true);                          // cincin marmer
            b.Cyl(Sl.DarkStone, p + new Vector3(0f, 0.058f, 0f), 0.48f, 0.012f, 16, 0.48f, true);   // lekuk yang licin oleh kaki
        }

        // Papan Lantai di timur teras, menghadap selatan (ke tangga)
        Vector3 bc = TwWorld(-14f, LkY, 33f);
        b.BoxB(Sl.DarkStone, bc, new Vector3(2.9f, 0.45f, 0.8f));
        b.BoxB(Sl.Stone, bc + new Vector3(0f, 0.45f, 0f), new Vector3(2.6f, 3.2f, 0.5f));
        b.BoxB(Sl.Dark, bc + new Vector3(0f, 0.6f, -0.2f), new Vector3(2.25f, 2.85f, 0.14f));
        b.Box(Sl.Gold, bc + new Vector3(0f, 3.62f, -0.12f), new Vector3(2.7f, 0.1f, 0.62f));
        // tiga puluh satu goresan lantai di dasar papan; yang ketiga puluh satu hampir tak terlihat
        for (int f = 0; f < 31; f++)
        {
            float hgt = f % 10 == 9 ? 0.22f : 0.12f;
            b.Box(f == 30 ? Sl.DarkStone : Sl.Stone, bc + new Vector3(-0.93f + f * 0.062f, 0.72f + hgt / 2f, -0.285f), new Vector3(0.014f, hgt, 0.01f));
        }
        b.Emit(g, "PijakanDanPapan", dSl);
        BoxCol(g, "PapanCol", bc + new Vector3(0f, 1.9f, 0f), new Vector3(2.9f, 3.8f, 0.8f), Vector3.zero);

        Color cream = new Color(0.93f, 0.9f, 0.78f, 1f);
        Vector3 tf = bc + new Vector3(0f, 0f, -0.3f);
        BuildKit.Text(g, "PAPAN LANTAI", tf + new Vector3(0f, 3.1f, 0f), 0f, 0.034f, cream);
        BuildKit.Text(g, "10 - 30   UJIAN HITUNG", tf + new Vector3(0.1f, 2.45f, 0f), 0f, 0.027f, cream);
        BuildKit.Text(g, "31   TANPA CAHAYA", tf + new Vector3(0.1f, 1.9f, 0f), 0f, 0.027f, cream);
        BuildKit.Text(g, "Hitung. Jangan lupa satu pun.", tf + new Vector3(0.1f, 1.2f, 0f), 0f, 0.02f, cream);

        // benda baca quest nd_k15 ("Lantai Tiga Puluh Satu") dan teks lengkung
        EcLore(g, "nd_o_lantai", TwWorld(-14f, LkY, 35.3f));
        EcInfo(g, "InteractPoint_LengkungMenara", TwWorld(0f, LkY, 31.2f), "Periksa lengkung menara", 3.6f, "Lengkung Menara",
            "Gerbang besar berbingkai emas di muka menara ini bukan pintu. Tidak ada engsel, tidak ada ambang, tidak ada kunci. Orang masuk karena menara membiarkannya.\n\n" +
            "Bila tujuh orang berdiri di depannya, cahaya di lengkungnya menghitam lalu kosong. Tidak terbuka. Hilang, seperti kulit yang menarik diri dari luka.\n\n" +
            "Tujuh pijakan putih di lantai ini licin oleh kaki orang yang menunggu giliran. Tak seorang pun tahu siapa yang memasangnya.", false);
    }
}
