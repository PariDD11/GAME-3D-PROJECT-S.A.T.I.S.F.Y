using System.Collections.Generic;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Kanon Lembah Iblis, bagian 2: Dataran Retakan (pusat desain (-190, 230), r 90). Dua puluh enam retakan hitam berbara yang berkelok mengikuti tanah,
/// sisik batu heksagonal di tepinya dan lepas di dataran, serta sepuluh kawah berkerak belerang kuning dengan lampu redup. Tanpa collider
/// (pita datar dan sisik rendah), titik di air, di luar peta, atau dekat jalan setapak dilewati.
/// </summary>
public static partial class HuntSceneBuilder
{
    static void DmRetakan(Transform root, Material[] slots)
    {
        var g = new GameObject("DataranRetakan").transform;
        g.SetParent(root, false);
        var b = new GeoBag();
        const float cx = -190f, cz = 230f;
        var vents = new List<Vector2>();
        for (int f = 0; f < 26; f++)
        {
            float rr = 6f + 74f * Mathf.Sqrt(DmH(f, 1));
            float a0 = DmH(f, 2) * 6.2832f;
            float x = cx + Mathf.Sin(a0) * rr, z = cz + Mathf.Cos(a0) * rr;
            float hd = DmH(f, 3) * 360f;
            int ns = 4 + (int)(DmH(f, 4) * 4f);
            float w = 0.8f + DmH(f, 5) * 1.0f;
            if (f < 10 && DmOk(x, z)) vents.Add(new Vector2(x, z));
            bool drew = false;
            for (int i = 0; i < ns; i++)
            {
                float len = 5f + DmH(f, 10 + i) * 6f;
                hd += (DmH(f, 30 + i) - 0.5f) * 60f;
                float sa = Mathf.Sin(hd * Mathf.Deg2Rad), ca = Mathf.Cos(hd * Mathf.Deg2Rad);
                float nx = x + sa * len, nz = z + ca * len;
                float w1 = Mathf.Max(0.25f, w * (1f - (i + 1f) / ns * 0.75f));
                if (DmOk(x, z) && DmOk(nx, nz))
                {
                    DmRibbon(b, Sl.Dark, -x, -z, -nx, -nz, w, w1, 0.05f);
                    if (i < ns - 1) DmRibbon(b, Sl.Glow, -x, -z, -nx, -nz, Mathf.Max(0.1f, w * 0.26f), Mathf.Max(0.08f, w1 * 0.26f), 0.09f);
                    drew = true;
                    for (int s = -1; s <= 1; s += 2)                                                       // sisik di kedua tepi retakan
                    {
                        float off = w * 0.5f + 0.8f + DmH(f * 11 + i, 40 + s) * 1.6f;
                        float mx = (x + nx) * 0.5f + ca * off * s, mz = (z + nz) * 0.5f - sa * off * s;
                        if (DmOk(mx, mz)) DmTile(b, mx, mz, f * 31 + i * 2 + (s + 1) / 2);
                    }
                }
                x = nx; z = nz;
            }
            if (drew) dmFissures++;
        }
        for (int k = 0; k < 90; k++)                                                                       // sisik lepas di dataran
        {
            float rr = 78f * Mathf.Sqrt(DmH(k, 81)), a = DmH(k, 82) * 6.2832f;
            float x = cx + Mathf.Sin(a) * rr, z = cz + Mathf.Cos(a) * rr;
            if (DmOk(x, z)) DmTile(b, x, z, 900 + k);
        }
        b.Emit(g, "RetakanSisik", slots);

        // kawah belerang: corong kuning berkerak, lubang gelap, bongkah kerak; lampu kuning redup di tiap kawah kedua
        var bv = new GeoBag();
        for (int i = 0; i < vents.Count; i++)
        {
            Vector3 p = G(vents[i].x, vents[i].y);
            bv.Push(new Vector3(p.x, p.y - 0.15f, p.z), 0f);
            bv.Cyl(Sl.Field, Vector3.zero, 0.95f, 0.62f, 8, 0.6f, true);
            bv.Disc(Sl.Dark, Vector3.zero, 0.36f, 12, 0.63f);
            for (int k = 0; k < 3; k++)
            {
                float ka = (k * 120f + DmH(i, 90 + k) * 50f) * Mathf.Deg2Rad;
                bv.Ball(Sl.Field, new Vector3(Mathf.Sin(ka) * 1.15f, 0.1f, Mathf.Cos(ka) * 1.15f), 0.26f + 0.08f * DmH(i, 95 + k), 3, 6, 0.6f);
            }
            bv.Pop();
            dmVents++;
            if (i % 2 == 0) ELight(g, p + Vector3.up * 1.0f, new Color(0.9f, 0.82f, 0.3f), 9f, 0.8f, false);
        }
        bv.Emit(g, "KawahBelerang", slots);

        BnInfo(g, "InteractPoint_Retakan", G(cx, cz) + new Vector3(0f, 0.1f, 0f), "Periksa tanah", 12f, "Dataran Retakan",
            "Tanah dataran ini pecah berpetak-petak seperti sisik yang mengering, merah di tepinya dan hitam di dalam celah. " +
            "Dari beberapa celah naik uap kuning yang berbau belerang, dari yang lain hanya naik hangat, dan dari satu dua celah yang paling lebar terlihat cahaya jingga jauh di bawah. " +
            "Tak ada angin; uapnya naik lurus seperti tiang.\n\n" +
            "Para pemburu hafal petak mana yang aman diinjak, tetapi tak seorang pun ingat siapa yang pertama kali menunjukkannya.");
    }

    /// <summary>Satu sisik batu heksagonal rendah di titik desain (x, z), sedikit miring, setengah terbenam.</summary>
    static void DmTile(GeoBag b, float x, float z, int id)
    {
        float r = 0.7f + DmH(id, 51) * 0.9f, h = 0.1f + DmH(id, 52) * 0.14f;
        int slot = id % 3 == 0 ? Sl.DarkStone : (id % 3 == 1 ? ForestKit.Rock : Sl.Stone);
        b.PushTRS(new Vector3(-x, GroundY(x, z) - 0.04f, -z), Quaternion.Euler((DmH(id, 53) - 0.5f) * 10f, DmH(id, 54) * 360f, (DmH(id, 55) - 0.5f) * 10f), 1f);
        b.Cyl(slot, Vector3.zero, r, h, 6, r * 0.72f, true);
        b.Pop();
        dmScales++;
    }
}
