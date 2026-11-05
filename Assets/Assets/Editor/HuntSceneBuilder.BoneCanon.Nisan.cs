using System.Collections.Generic;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Kanon Padang Hantu, bagian 3: nisan tanpa nama. Tujuh baris batu rendah berderet ke barat (desain +x) dari lingkar kerangka di sekitar Batu Dua Nama,
/// menghadap batu itu; di muka tiap nisan hanya ada goresan, tiga di kiri dan tiga di kanan. Posisi diambil dari hash (tanpa UnityEngine.Random),
/// dan titik yang bertabrakan dengan pohon, batu, atau kerangka yang sudah ada dilewati.
/// </summary>
public static partial class HuntSceneBuilder
{
    const int BnRows = 7;

    /// <summary>Posisi dunia (x, z) dan radius benda yang sudah ada di tProps dan tRocks; disimpan sebagai Vector3(x, z, r).</summary>
    static List<Vector3> BnObstacles()
    {
        var l = new List<Vector3>();
        foreach (var t in new[] { tProps, tRocks })
        {
            if (t == null) continue;
            for (int i = 0; i < t.childCount; i++)
            {
                var c = t.GetChild(i);
                float s = Mathf.Max(1f, c.localScale.x);
                l.Add(new Vector3(c.position.x, c.position.z, 0.9f + s));
            }
        }
        return l;
    }

    static bool BnFree(List<Vector3> obst, float x, float z)
    {
        if (Mathf.Abs(x) > 420f || Mathf.Abs(z) > 420f) return false;
        if (GroundY(x, z) < WaterY + 0.8f) return false;
        if (DistToTrails(x, z) < 4f) return false;
        float wx = -x, wz = -z;
        foreach (var o in obst)
        {
            float dx = o.x - wx, dz = o.y - wz;
            if (dx * dx + dz * dz < o.z * o.z) return false;
        }
        return true;
    }

    static void BnNisan(Transform root, Material[] slots)
    {
        var g = new GameObject("NisanTanpaNama").transform;
        g.SetParent(root, false);
        var obst = BnObstacles();
        var b = new GeoBag();
        GameObject cg = null;
        int inBatch = 0, part = 0;
        for (int row = 0; row < BnRows; row++)
        {
            float rz = BnSz + (row - 3) * 6.4f;
            int cols = 17 + (int)(BnHash(row, 11) * 8f);
            for (int c = 0; c < cols; c++)
            {
                int id = row * 40 + c;
                float x = 228f + c * 3.5f + (BnHash(id, 5) - 0.5f) * 0.9f;
                float z = rz + Mathf.Sin(x * 0.05f + row * 1.3f) * 1.5f + (BnHash(id, 9) - 0.5f) * 0.9f;
                if (!BnFree(obst, x, z)) continue;

                Vector3 p = G(x, z);
                float h = 0.62f + BnHash(id, 13) * 0.4f;
                int v = Mathf.Min(2, (int)(BnHash(id, 17) * 3f));
                float yaw = 90f + (BnHash(id, 21) - 0.5f) * 12f;                                              // menghadap batu (timur dunia)
                var rot = Quaternion.Euler((BnHash(id, 25) - 0.5f) * 5f, yaw, (BnHash(id, 29) - 0.5f) * 5f);
                b.PushTRS(p, rot, 1f);
                BnStone(b, h, v);
                b.Pop();

                if (cg == null) { cg = new GameObject("NisanCol_" + part); cg.transform.SetParent(g, false); }
                var bc = cg.AddComponent<BoxCollider>();                                                      // kotak sejajar sumbu dunia (yaw ~90)
                bc.center = new Vector3(p.x, p.y + (h + 0.3f) / 2f, p.z);
                bc.size = new Vector3(0.3f, h + 0.3f, 0.62f);

                bnStones++; bnScratches += 6;
                if (++inBatch == 24)
                {
                    b.Emit(g, "Nisan_" + part, slots);
                    b = new GeoBag(); cg = null; inBatch = 0; part++;
                }
            }
        }
        if (inBatch > 0) b.Emit(g, "Nisan_" + part, slots);

        BnInfo(g, "InteractPoint_Nisan", G(BnSx + 34f, BnSz), "Periksa nisan", 5.5f, "Nisan Tanpa Nama",
            "Batu-batu rendah berderet ke barat, jauh melewati tempat mata masih bisa menghitungnya. Tak satu pun memuat nama. " +
            "Di muka tiap batu hanya ada goresan, tiga di kiri dan tiga di kanan, dalam dan lurus seperti digores dengan satu gerakan.\n\n" +
            "Kata orang pos, goresan itu bukan buatan pemburu mana pun. Semuanya menghadap batu yang bernama, seperti menunggu giliran dipanggil.");
    }

    /// <summary>Satu nisan: titik asal di tanah, muka +Z. h = tinggi badan (0,62 sampai 1,02 m). v: 0 datar bertutup, 1 beratap pelana, 2 patah miring.</summary>
    static void BnStone(GeoBag b, float h, int v)
    {
        b.Box(Sl.DarkStone, new Vector3(0f, 0.04f, 0f), new Vector3(0.8f, 0.4f, 0.36f));                    // tapak, tertanam 16 cm
        float y0 = 0.24f;
        if (v == 0)
        {
            b.Box(Sl.Stone, new Vector3(0f, y0 + h / 2f, 0f), new Vector3(0.52f, h, 0.14f));
            b.Box(Sl.DarkStone, new Vector3(0f, y0 + h + 0.03f, 0f), new Vector3(0.6f, 0.07f, 0.19f));
        }
        else if (v == 1)
        {
            b.Box(Sl.Stone, new Vector3(0f, y0 + h / 2f, 0f), new Vector3(0.52f, h, 0.14f));
            float yk = y0 + h, yp = yk + 0.13f;                                                               // atap pelana tipis: dua lereng dan dua segitiga muka
            b.QuadBoth(Sl.DarkStone, new Vector3(-0.3f, yk, -0.1f), new Vector3(0f, yp, -0.1f), new Vector3(0f, yp, 0.1f), new Vector3(-0.3f, yk, 0.1f));
            b.QuadBoth(Sl.DarkStone, new Vector3(0f, yp, -0.1f), new Vector3(0.3f, yk, -0.1f), new Vector3(0.3f, yk, 0.1f), new Vector3(0f, yp, 0.1f));
            b.QuadBoth(Sl.DarkStone, new Vector3(-0.3f, yk, 0.1f), new Vector3(0.3f, yk, 0.1f), new Vector3(0f, yp, 0.1f), new Vector3(0f, yp, 0.1f));
            b.QuadBoth(Sl.DarkStone, new Vector3(-0.3f, yk, -0.1f), new Vector3(0.3f, yk, -0.1f), new Vector3(0f, yp, -0.1f), new Vector3(0f, yp, -0.1f));
        }
        else
        {
            float hh = h - 0.16f;
            b.Box(Sl.Stone, new Vector3(0f, y0 + hh / 2f, 0f), new Vector3(0.52f, hh, 0.14f), 0f, false);
            float yl = y0 + hh, yr = yl + 0.15f;                                                              // puncak patah miring: atas, dua segitiga muka, sisi kanan
            b.QuadBoth(Sl.Stone, new Vector3(-0.26f, yl, -0.07f), new Vector3(0.26f, yr, -0.07f), new Vector3(0.26f, yr, 0.07f), new Vector3(-0.26f, yl, 0.07f));
            b.QuadBoth(Sl.Stone, new Vector3(-0.26f, yl, 0.07f), new Vector3(0.26f, yl, 0.07f), new Vector3(0.26f, yr, 0.07f), new Vector3(0.26f, yr, 0.07f));
            b.QuadBoth(Sl.Stone, new Vector3(-0.26f, yl, -0.07f), new Vector3(0.26f, yl, -0.07f), new Vector3(0.26f, yr, -0.07f), new Vector3(0.26f, yr, -0.07f));
            b.QuadBoth(Sl.Stone, new Vector3(0.26f, yl, -0.07f), new Vector3(0.26f, yl, 0.07f), new Vector3(0.26f, yr, 0.07f), new Vector3(0.26f, yr, -0.07f));
        }
        // goresan: tiga di kiri, tiga di kanan, di muka depan
        float zf = 0.073f, yb = y0 + h * 0.3f, yt = y0 + h * 0.7f;
        for (int k = 0; k < 3; k++)
        {
            foreach (float x0 in new[] { -0.2f + k * 0.065f, 0.07f + k * 0.065f })
            {
                float dy = 0.02f * k;
                b.QuadBoth(Sl.DarkStone, new Vector3(x0 - 0.008f, yb + dy, zf), new Vector3(x0 + 0.008f, yb + dy, zf),
                    new Vector3(x0 + 0.038f, yt + dy, zf), new Vector3(x0 + 0.022f, yt + dy, zf));
            }
        }
    }
}
