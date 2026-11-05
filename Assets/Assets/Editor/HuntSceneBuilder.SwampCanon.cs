using System.Collections.Generic;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Kanon Rawa Terkutuk (HuntArea_Swamp; lihat Docs/LORE_BIBLE.md §8.5). Dipanggil dari BuildSwamp() sesudah BuildSwampRocks() dan
/// TIDAK memakai UnityEngine.Random, supaya sebaran pohon, batu, sarang dan monster yang sudah ada tidak bergeser.
///
///  1. Merana: kursi di depan tiap rumah (dan di alun-alun serta jalan kapel), semuanya menghadap menara lonceng kapel; di tiap kursi duduk
///     seorang penidur bermata terbuka. (file ini + .Sleepers.cs)
///  2. Seribu empat ratus lilin putih yang semuanya menyala: dua dermaga Kelabu dan jalan menuju menara kapel Merana. (.Candles.cs)
///  3. Bendungan jebol di tepi Teluk Nelayan dan dua perahu rumbia. (.Kelabu.cs)
///
/// Tidak ada NPC atau mekanik runtime di sini; hanya benda dan teks. Teks tidak memakai "yang ketujuh", Jeda, si Sulung, Lucifer, atau nama dewa.
/// </summary>
public static partial class HuntSceneBuilder
{
    const int SwCandleTotal = 1400;       // jumlah lilin kanon
    static int swCandles;                 // hitungan lilin yang sudah dipasang

    static void SwampCanon()
    {
        var root = Group("KanonRawa");
        swCandles = 0;
        SwDocks(root);                    // dulu: hitungan dermaga menentukan sisa lilin jalan kapel
        SwMerana(root);
        SwDam(root);
    }

    /// <summary>Pusat dasar menara lonceng kapel Merana (koordinat desain): 7,3 m di depan kapel, ke arah alun-alun.</summary>
    static Vector2 SwTower()
    {
        Vector2 chap = Pt(GhostC, 0f, 40f);
        return Local(chap, FaceYaw(chap, GhostC), 0f, 7.3f);
    }

    // ================================================================== Merana
    static void SwMerana(Transform root)
    {
        var g = new GameObject("MeranaPenidur").transform;
        g.SetParent(root, false);
        Vector2 tw = SwTower();
        Vector3 towerW = G(tw.x, tw.y);

        // posisi kursi (desain): empat per rumah di depan pintu, empat di alun-alun, enam mengapit jalan kapel
        var chairs = new List<Vector2>();
        for (int i = 0; i < 9; i++)
        {
            float ang = i * 40f + 12f;
            if (Mathf.Abs(Mathf.DeltaAngle(ang, 0f)) < 22f) continue;                // sama dengan PlanGhost(): ruang kapel di utara
            Vector2 p = Pt(GhostC, ang, 26f + (i % 3) * 2.5f);
            float yaw = FaceYaw(p, GhostC), d = 4.6f + (i % 2) * 0.7f;
            for (int k = 0; k < 4; k++) chairs.Add(Local(p, yaw, (k - 1.5f) * 1.5f, d / 2f + 2.0f + 0.35f * (k % 2)));
        }
        for (int k = 0; k < 4; k++) chairs.Add(Pt(GhostC, k * 90f + 45f, 6.0f));
        foreach (float dz in new[] { 12f, 20f, 28f })
        {
            chairs.Add(new Vector2(GhostC.x - 3.6f, GhostC.y + dz));
            chairs.Add(new Vector2(GhostC.x + 3.6f, GhostC.y + dz));
        }

        var cand = new GeoBag();
        var bag = new GeoBag();
        var sleepSlots = SwSleeperSlots();
        int made = 0, part = 0;
        for (int i = 0; i < chairs.Count; i++)
        {
            Vector3 cw = G(chairs[i].x, chairs[i].y);
            Vector3 to = towerW - cw; to.y = 0f;
            float yaw = DcYawOf(to) + (DcHash(i, 5) - 0.5f) * 5f;                   // hampir sejajar: semuanya menghadap menara
            bag.Push(cw, yaw);
            SwChair(bag);
            SwSleeper(bag, i);
            bag.Pop();
            Vector3 f = cw + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0.14f, 0f, 0.9f);
            f.y = GroundYW(f.x, f.z);
            SwCandle(cand, f, 0.12f + 0.1f * DcHash(i, 7), i);                      // satu lilin di kaki tiap penidur
            if (++made % 14 == 0) { bag.Emit(g, "Kursi_Penidur_" + (++part), sleepSlots); bag = new GeoBag(); }
        }
        if (made % 14 != 0) bag.Emit(g, "Kursi_Penidur_" + (++part), sleepSlots);

        SwTowerCandles(g, cand, tw);

        // teks
        Vector2 lane = new Vector2(GhostC.x + 1.6f, GhostC.y + 11f);
        DcInfo(g, "InteractPoint_PenghuniMerana", G(lane.x, lane.y), "Periksa orang-orang di kursi", 7.5f, "Penghuni Merana",
            "Kursi-kursi di depan tiap rumah dihadapkan ke satu arah: menara kapel. Orang-orang yang duduk di atasnya tidak mati. Dada mereka naik dan turun, pelan dan serempak seperti ombak yang malas, dan mata mereka terbuka lebar menatap menara tanpa berkedip. Embun menempel di bulu mata.\n\n" +
            "Kau melambai di depan wajah seseorang; tak ada yang berubah. Di dekat kaki tiap orang menyala sebatang lilin putih, dan nyalanya tak bergoyang walau angin rawa menggoyang ilalang di tepi jalan.");
        Vector2 front = Local(tw, FaceYaw(Pt(GhostC, 0f, 40f), GhostC), 0f, 3.6f);
        DcInfo(g, "InteractPoint_MenaraKapelMerana", G(front.x, front.y), "Periksa kaki menara kapel", 5.5f, "Menara Kapel Merana",
            "Menara lonceng itu sudah lama tak berlonceng. Semua kursi di desa ini menghadap ke sini. Di kakinya, lilin-lilin putih berbaris dari alun-alun sampai ke tangga, rapat seperti butir gandum, dan tak satu pun padam.\n\n" +
            "Seseorang pernah mencoba menghitungnya dan menyebut angka seribu empat ratus. Orang lain menghitung lagi dan mendapat angka yang sama.");
    }
}
