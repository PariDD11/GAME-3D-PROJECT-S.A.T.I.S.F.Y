using System.Collections.Generic;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Kantor Pendaki (kanon §8.6: "ribuan orang menggores catatan di tembok Kantor Pendaki selama seribu tahun, semuanya tiga goresan pendek
/// sejajar, jeda, tiga lagi"). Aula gerbang berdinding batu gelap di sisi utara alun-alun menara, tepat di sumbu Avenue Agung Utara, dengan
/// lorong 7 m di tengahnya. Dinding lorong, muka utara, kedua ujung, dan muka selatan penuh goresan (lihat .Goresan.cs). Di tengah lorong
/// ada satu cahaya hangat yang berdenyut pelan ("denyut dari dalam dinding, seperti nadi orang yang sedang tidur").
/// Akar bergaya Landmark (root > Model + titik interaksi) di bawah tNoble, jadi ikut BuildingMover.
/// </summary>
public static partial class TownSceneBuilder
{
    const float KpW = 34f, KpD = 9f, KpH = 6.4f, KpPW = 7f, KpPH = 4.8f;      // lebar, dalam, tinggi dinding, lebar dan tinggi lorong
    const float KpZ = 63.5f;                                                   // Z dunia pusat aula (utara menara; aula menghadap selatan)
    const float KpBase = 0.22f;                                                // tinggi ambang di atas lantai alun-alun

    /// <summary>Titik lokal aula -> dunia (akar diputar 180 derajat: x -> -x, z -> -z).</summary>
    static Vector3 KpWorld(float lx, float ly, float lz) { return new Vector3(-lx, ly, KpZ - lz); }

    static void EcKantor(Transform root)
    {
        var kroot = new GameObject("D_KantorPendaki").transform;
        kroot.SetParent(tNoble, false);
        kroot.position = new Vector3(0f, 0f, KpZ);
        kroot.rotation = Quaternion.Euler(0f, 180f, 0f);                      // +Z lokal (muka depan) menghadap selatan, ke menara
        var model = new GameObject("Model").transform;                         // hapus child ini untuk mengganti dengan prefab sendiri
        model.SetParent(kroot, false);

        var b = new GeoBag();
        EcKantorHall(b);
        b.Emit(model, "Mesh", EcWarmSlots());
        EcKantorText(model);

        // collider: dua blok dan lintel (lorong tetap terbuka)
        float pw = KpPW / 2f, bw = KpW / 2f - pw;
        BoxCol(model, "ColKiri", new Vector3(-(pw + bw / 2f), KpH / 2f, 0f), new Vector3(bw, KpH, KpD), Vector3.zero);
        BoxCol(model, "ColKanan", new Vector3(pw + bw / 2f, KpH / 2f, 0f), new Vector3(bw, KpH, KpD), Vector3.zero);
        BoxCol(model, "ColLintel", new Vector3(0f, (KpPH + KpH) / 2f, 0f), new Vector3(KpPW, KpH - KpPH, KpD), Vector3.zero);

        // dua ribu empat ratus goresan 3+3
        EcGoresan(model);

        // cahaya berdenyut di tengah lorong
        var pl = EcLight(kroot, "CahayaDenyut", KpWorld(0f, 2.7f, 0f), 12f, 1.0f, new Color(1f, 0.86f, 0.62f, 1f), true);
        var fl = pl.GetComponent<FlickerLight>();
        if (fl != null) { fl.amount = 0.32f; fl.speed = 0.55f; }
        AddLight(kroot, new Vector3(-10f, 4.4f, KpD / 2f + 1.6f), 13f, 1.2f);          // lampu pintu depan

        // titik interaksi
        EcInfo(kroot, "InteractPoint", KpWorld(-10f, 0.14f, KpD / 2f + 2.4f), "Periksa Kantor Pendaki", 3.8f, "Kantor Pendaki",
            "Aula berdinding batu ini lebih tua daripada kota di sekelilingnya. Setiap pendaki yang hendak naik menuliskan namanya di buku besar di dalam, " +
            "lalu menggores catatannya sendiri di tembok luar sebelum berangkat. Tidak ada aturan yang menyuruhnya. Tidak seorang pun pernah mengajarinya.\n\n" +
            "Petugas kantor berkata bukunya hanya mencatat yang naik. \"Tembok itulah yang menghitung,\" katanya. Ia menolak ditanya siapa yang membuat goresan pertama, " +
            "dan ia tidak pernah berdiri di dalam lorong gerbang setelah hari gelap.", true);
        EcLore(kroot, "nd_o_goresan", KpWorld(-5.5f, 0.14f, -(KpD / 2f + 2.8f)));       // di depan muka utara, dekat jalur dari gerbang utara

        ReserveBox(0f, KpZ, KpW + 3f, KpD + 3f, 0f, 0f);
    }

    // ================================================================== aula
    static void EcKantorHall(GeoBag b)
    {
        float hw = KpW / 2f, hd = KpD / 2f, pw = KpPW / 2f, bw = hw - pw;
        // ambang rendah (juga lantai lorong)
        b.BoxB(Sl.DarkStone, new Vector3(0f, 0f, 0f), new Vector3(KpW + 1.0f, KpBase, KpD + 1.0f));
        // dua blok dinding dan lintel di atas lorong
        b.BoxB(Sl.DarkStone, new Vector3(-(pw + bw / 2f), 0f, 0f), new Vector3(bw, KpH, KpD));
        b.BoxB(Sl.DarkStone, new Vector3(pw + bw / 2f, 0f, 0f), new Vector3(bw, KpH, KpD));
        b.BoxB(Sl.DarkStone, new Vector3(0f, KpPH, 0f), new Vector3(KpPW, KpH - KpPH, KpD));
        // langit-langit kayu lorong dengan tiga balok
        b.Box(Sl.Wood, new Vector3(0f, KpPH + 0.05f, 0f), new Vector3(KpPW, 0.1f, KpD), 0f, true, true);
        foreach (float z in new[] { -3f, 0f, 3f })
            b.Box(Sl.Wood, new Vector3(0f, KpPH - 0.14f, z), new Vector3(KpPW, 0.28f, 0.32f));
        // kornis marmer dan pilaster sudut
        b.Box(Sl.Stone, new Vector3(0f, KpH + 0.1f, 0f), new Vector3(KpW + 0.7f, 0.34f, KpD + 0.7f));
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                b.BoxB(Sl.Stone, new Vector3(sx * (hw - 0.12f), 0f, sz * (hd - 0.12f)), new Vector3(0.7f, KpH, 0.7f));
        // bingkai lorong di kedua muka
        foreach (float sz in new[] { -1f, 1f })
        {
            foreach (float sx in new[] { -1f, 1f })
                b.BoxB(Sl.Stone, new Vector3(sx * (pw + 0.05f), KpBase, sz * (hd + 0.06f)), new Vector3(0.5f, KpPH - KpBase + 0.1f, 0.34f));
            b.Box(Sl.Stone, new Vector3(0f, KpPH + 0.14f, sz * (hd + 0.06f)), new Vector3(KpPW + 1.1f, 0.4f, 0.4f));
        }
        // papan nama di atas lorong, kedua muka (tulisan dipasang EcKantorText)
        foreach (float sz in new[] { -1f, 1f })
        {
            b.Box(Sl.Dark, new Vector3(0f, KpPH + 0.86f, sz * (hd + 0.1f)), new Vector3(5.2f, 0.74f, 0.1f));
            b.Box(Sl.Gold, new Vector3(0f, KpPH + 1.26f, sz * (hd + 0.1f)), new Vector3(5.34f, 0.07f, 0.12f));
            b.Box(Sl.Gold, new Vector3(0f, KpPH + 0.46f, sz * (hd + 0.1f)), new Vector3(5.34f, 0.07f, 0.12f));
        }
        // jendela muka depan (+Z): lantai bawah dan atas
        foreach (float wx in new[] { -14.2f, -5.9f, 6f, 10f, 14f })
            EcWindow(b, wx, 2.6f, 1.1f, 1.9f, hd);
        foreach (float wx in new[] { -14.2f, -10f, -5.9f, 6f, 10f, 14f })
            EcWindow(b, wx, 5.35f, 0.8f, 0.9f, hd);
        // pintu depan di x = -10
        foreach (float sx in new[] { -1f, 1f })
            b.BoxB(Sl.Stone, new Vector3(-10f + sx * 1.1f, KpBase, hd + 0.1f), new Vector3(0.34f, 3.1f, 0.3f));
        b.Box(Sl.Stone, new Vector3(-10f, KpBase + 3.25f, hd + 0.1f), new Vector3(2.9f, 0.34f, 0.34f));
        b.Box(Sl.Wood, new Vector3(-10f, KpBase + 1.5f, hd + 0.04f), new Vector3(1.9f, 3.0f, 0.12f));
        b.Box(Sl.Metal, new Vector3(-10f, KpBase + 1.5f, hd + 0.12f), new Vector3(1.9f, 0.09f, 0.05f));
        b.BoxB(Sl.Stone, new Vector3(-10f, 0f, hd + 0.5f + 0.4f), new Vector3(3.2f, 0.12f, 0.8f));
        // papan nama depan dan dua lentera berbraket
        b.Box(Sl.Dark, new Vector3(-10f, 4.25f, hd + 0.1f), new Vector3(5.0f, 0.8f, 0.1f));
        b.Box(Sl.Gold, new Vector3(-10f, 4.67f, hd + 0.1f), new Vector3(5.14f, 0.07f, 0.12f));
        b.Box(Sl.Gold, new Vector3(-10f, 3.83f, hd + 0.1f), new Vector3(5.14f, 0.07f, 0.12f));
        foreach (float lx in new[] { -12.6f, -7.4f })
        {
            b.Beam(Sl.Metal, new Vector3(lx, 3.1f, hd), new Vector3(lx, 3.1f, hd + 0.5f), 0.06f, 0.06f);
            BuildKit.Lantern(b, new Vector3(lx, 2.75f, hd + 0.5f));
        }
        // atap pelana dengan punggung sejajar sumbu panjang
        b.Gable(Sl.Roof, Sl.DarkStone, Sl.Wood, new Vector3(0f, KpH + 0.27f, 0f), KpW, KpD, 3.2f, 0.9f, true);
    }

    static void EcWindow(GeoBag b, float wx, float wy, float ww, float wh, float hd)
    {
        b.Box(Sl.Stone, new Vector3(wx, wy, hd + 0.05f), new Vector3(ww + 0.34f, wh + 0.34f, 0.14f));
        b.Box(Sl.Glass, new Vector3(wx, wy, hd + 0.12f), new Vector3(ww, wh, 0.05f));
        b.Box(Sl.Stone, new Vector3(wx, wy - wh / 2f - 0.2f, hd + 0.14f), new Vector3(ww + 0.6f, 0.12f, 0.3f));
    }

    static void EcKantorText(Transform model)
    {
        float hd = KpD / 2f;
        Color cream = new Color(0.95f, 0.9f, 0.74f, 1f);
        BuildKit.Text(model, "KANTOR PENDAKI", new Vector3(-10f, 4.25f, hd + 0.2f), 180f, 0.09f, cream);
        BuildKit.Text(model, "KANTOR PENDAKI", new Vector3(0f, KpPH + 0.86f, hd + 0.2f), 180f, 0.09f, cream);
        BuildKit.Text(model, "KANTOR PENDAKI", new Vector3(0f, KpPH + 0.86f, -(hd + 0.2f)), 0f, 0.09f, cream);
    }
}
