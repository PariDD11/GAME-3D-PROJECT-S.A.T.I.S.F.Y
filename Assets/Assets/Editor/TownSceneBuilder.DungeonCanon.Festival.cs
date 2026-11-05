using System.Collections.Generic;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Festival Cahaya Salju (kanon §8.6: "ribuan lentera diangkat di ladang terbuka"). Di kota, sisa festival itu tetap tergantung: empat tali
/// lentera dibentangkan di antara tiap dua obelisk alun-alun menara (delapan bentang), jadi lebih dari seribu lima ratus lentera menaungi alun-alun.
/// Enam warna bergantian dalam urutan tetap (emas, biru muda, hijau muda, merah muda, ungu, jingga), lalu SATU KAIT KOSONG, lalu enam warna lagi.
/// Warna lentera memakai enam slot ground yang tidak terpakai di mesh ini (Sl.Cobble, Dirt, Grass, Field, FieldB, Hedge) yang ditimpa bahan emisif lembut.
/// Tidak ada lampu per lentera: hanya delapan lampu hangat redup, satu di bawah tiap bentang. Deterministik, tanpa UnityEngine.Random.
/// </summary>
public static partial class TownSceneBuilder
{
    const float FsR = 66f;                                                    // jari-jari obelisk (lihat DBuildPlaza)
    const int FsPos = 56;                                                     // posisi gantung per tali: 8 kelompok (enam lentera + satu kait kosong)
    static readonly float[] FsAttach = { 12.6f, 12.0f, 11.4f, 10.8f };        // tinggi ujung tali (kerucut emas obelisk di 13,8)
    static readonly float[] FsSag = { 1.4f, 1.9f, 2.4f, 2.9f };               // lendutan di tengah: dasar lentera terendah 7,4 m (puncak air mancur 6,3 m)
    static readonly float[] FsLat = { -0.45f, -0.15f, 0.15f, 0.45f };         // geser menyamping tiap tali
    static readonly int[] FsPhase = { 0, 3, 5, 1 };                           // geser pola 6+1 antar tali supaya warna tidak berbaris lurus
    const float FsMinY = 7.4f;                                                // dasar lentera terendah di mana pun (puncak air mancur 6,3 m)
    const int FsKantorSpan = 7;                                               // bentang obelisk 337,5 -> 22,5 derajat melintasi atap Kantor Pendaki (puncak atap ~8,5 m di z 61)
    const float FsKantorMinY = 9.3f;
    static int ecLanternCount, ecHookCount;

    static void EcFestival(Transform root)
    {
        var g = Group("FestivalCahayaSalju", root);
        var slots = (Material[])dSl.Clone();
        int[] ls = { Sl.Cobble, Sl.Dirt, Sl.Grass, Sl.Field, Sl.FieldB, Sl.Hedge };
        string[] nm = { "Emas", "BiruMuda", "HijauMuda", "MerahMuda", "Ungu", "Jingga" };
        Color[] bc =
        {
            new Color(1.00f, 0.82f, 0.36f, 1f), new Color(0.56f, 0.78f, 1.00f, 1f), new Color(0.62f, 0.92f, 0.56f, 1f),
            new Color(1.00f, 0.64f, 0.76f, 1f), new Color(0.74f, 0.58f, 0.95f, 1f), new Color(1.00f, 0.67f, 0.32f, 1f)
        };
        Color[] em =
        {
            new Color(0.50f, 0.38f, 0.10f, 1f), new Color(0.17f, 0.31f, 0.50f, 1f), new Color(0.20f, 0.42f, 0.18f, 1f),
            new Color(0.50f, 0.20f, 0.29f, 1f), new Color(0.29f, 0.19f, 0.50f, 1f), new Color(0.55f, 0.28f, 0.08f, 1f)
        };
        for (int k = 0; k < 6; k++)
            slots[ls[k]] = TownMats.Emissive(UMat("EldLentera" + nm[k], bc[k]), em[k]);

        ecLanternCount = 0; ecHookCount = 0;
        EcFestivalCollars(g, slots);
        for (int sp = 0; sp < 8; sp++) EcFestivalSpan(g, slots, ls, sp);

        // titik periksa di mulut avenue selatan, tempat pemain pertama kali masuk alun-alun
        Vector2 ip = AtAz(180f, 61f);
        EcInfo(g, "InteractPoint_TaliLentera", V3(ip), "Periksa tali lentera", 4.5f, "Tali Lentera Festival",
            "Tali-tali lentera ini dibentangkan di antara delapan obelisk setiap menjelang Festival Cahaya Salju, dan tak seorang pun pernah menurunkannya " +
            "sesudah festival usai. Enam warna bergantian: emas, biru muda, hijau muda, merah muda, ungu, jingga. Lalu satu kait kosong. Lalu enam warna lagi.\n\n" +
            "Tukang gantung lentera berkata kait kosong itu bukan kelalaian. \"Pesanannya begitu sejak tali yang pertama,\" katanya. " +
            "\"Satu kait selalu dibiarkan menunggu.\" Ia tidak tahu apa yang ditunggu, dan ia tidak suka ditanya.", false);
    }

    /// <summary>Selubung emas di bagian atas tiap obelisk (kerangka tempat tali dikaitkan). Searah dengan obelisk yang diputar 45 derajat per urutan.</summary>
    static void EcFestivalCollars(Transform g, Material[] slots)
    {
        var b = new GeoBag();
        for (int k = 0; k < 8; k++)
        {
            Vector2 q = AtAz(22.5f + 45f * k, FsR);
            b.Push(new Vector3(q.x, 0.15f, q.y), 45f * k);
            b.Cyl(Sl.Gold, new Vector3(0f, 10.35f, 0f), 1.07f, 0.2f, 4, 1.07f, true, true);
            b.Cyl(Sl.Gold, new Vector3(0f, 12.75f, 0f), 0.98f, 0.2f, 4, 0.98f, true, true);
            b.Pop();
        }
        b.Emit(g, "SelubungObelisk", slots);
    }

    static void EcFestivalSpan(Transform g, Material[] slots, int[] ls, int sp)
    {
        Vector2 qa = AtAz(22.5f + 45f * sp, FsR), qb = AtAz(22.5f + 45f * ((sp + 1) % 8), FsR);
        Vector2 dir = (qb - qa).normalized, nrm = new Vector2(dir.y, -dir.x);
        var b = new GeoBag();
        const int NS = 14;
        for (int j = 0; j < FsAttach.Length; j++)
        {
            Vector2 pa = qa + dir * 1.25f + nrm * FsLat[j], pb = qb - dir * 1.25f + nrm * FsLat[j];
            float h = FsAttach[j], sag = Mathf.Min(FsSag[j], h - 0.58f - (sp == FsKantorSpan ? FsKantorMinY : FsMinY));          // bentang di atas Kantor Pendaki ditarik lebih tegang
            // lengan kuningan pendek dari obelisk ke ujung tali
            b.Beam(Sl.Gold, new Vector3(qa.x + dir.x * 0.75f + nrm.x * FsLat[j], h, qa.y + dir.y * 0.75f + nrm.y * FsLat[j]), new Vector3(pa.x, h, pa.y), 0.05f, 0.05f);
            b.Beam(Sl.Gold, new Vector3(qb.x - dir.x * 0.75f + nrm.x * FsLat[j], h, qb.y - dir.y * 0.75f + nrm.y * FsLat[j]), new Vector3(pb.x, h, pb.y), 0.05f, 0.05f);
            // tali (parabola): NS ruas
            Vector3 prev = Vector3.zero;
            for (int s = 0; s <= NS; s++)
            {
                float t = s / (float)NS;
                Vector2 xz = Vector2.Lerp(pa, pb, t);
                Vector3 p = new Vector3(xz.x, h - sag * 4f * t * (1f - t), xz.y);
                if (s > 0) b.Beam(Sl.Wood, prev, p, 0.04f, 0.04f);
                prev = p;
            }
            // lentera: urutan enam warna lalu satu kait kosong, digeser per tali
            for (int p = 0; p < FsPos; p++)
            {
                float t = (p + 0.5f) / FsPos;
                Vector2 xz = Vector2.Lerp(pa, pb, t);
                float y = h - sag * 4f * t * (1f - t);
                int ph = (p + FsPhase[j]) % 7;
                if (ph == 6)
                {
                    b.Cyl(Sl.Gold, new Vector3(xz.x, y - 0.2f, xz.y), 0.016f, 0.19f, 4, 0.016f, false, false);
                    b.Ball(Sl.Gold, new Vector3(xz.x, y - 0.22f, xz.y), 0.04f, 3, 5);
                    ecHookCount++;
                }
                else
                {
                    // lentera kertas segi enam berbentuk tong (terlebar di tengah) dengan tutup kuningan kecil di atas
                    b.Cyl(ls[ph], new Vector3(xz.x, y - 0.58f, xz.y), 0.15f, 0.24f, 6, 0.25f, false, true);
                    b.Cyl(ls[ph], new Vector3(xz.x, y - 0.34f, xz.y), 0.25f, 0.21f, 6, 0.10f, true, false);
                    b.Cyl(Sl.Gold, new Vector3(xz.x, y - 0.13f, xz.y), 0.10f, 0.05f, 6, 0.10f, true, false);
                    ecLanternCount++;
                }
            }
        }
        b.Emit(g, "TaliLentera_" + (sp + 1), slots);
        Vector2 mid = (qa + qb) * 0.5f;
        EcLight(g, "CahayaFestival_" + (sp + 1), new Vector3(mid.x, 6.0f, mid.y), 13f, 0.6f, new Color(1f, 0.86f, 0.66f, 1f), false);
    }
}
