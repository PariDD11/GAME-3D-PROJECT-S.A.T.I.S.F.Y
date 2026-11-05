using System.Collections.Generic;
using UnityEngine;

/// <summary>Mempercantik Desa Pemula: pohon, semak, pakis, bunga, kolam, kebun sayur, jerami, kayu, tong, bangku, sumur tambahan.
/// Semuanya ditaruh di ruang kosong (menghormati Reserve/jalan/jalur) dan dipanggil setelah pohon dan bangunan selesai.</summary>
public static partial class TownSceneBuilder
{
    static bool VOpenSpot(float x, float z, float r, float roadGap = 4.5f)
    {
        float rr = Mathf.Sqrt(x * x + z * z);
        if (rr < 13f || rr > VR - 4.5f) return false;
        if (VSlopeDeg(x, z) > 16f) return false;
        if (VDistToRoads(x, z) < roadGap) return false;
        if (VNearStreet(new Vector2(x, z), r * 0.6f)) return false;
        return IsFree(x, z, r);
    }

    static bool VPickOpen(float r, out float x, out float z, float roadGap = 4.5f)
    {
        for (int i = 0; i < 80; i++)
        {
            x = Random.Range(-VR + 5f, VR - 5f); z = Random.Range(-VR + 5f, VR - 5f);
            if (VOpenSpot(x, z, r, roadGap)) return true;
        }
        x = z = 0f; return false;
    }

    static Vector3 VLoc(float cx, float cz, float yaw, float lx, float lz)
    {
        Vector3 p = Quaternion.Euler(0f, yaw, 0f) * new Vector3(lx, 0f, lz);
        p.x += cx; p.z += cz; p.y = VGy(p.x, p.z);
        return p;
    }

    static void VBeautify()
    {
        var root = Group("Asri", tProps);
        var rnd = new System.Random(2026);
        float x, z;

        // ---- pohon rindang & pohon apel di sela rumah
        {
            var g = Group("PohonDesa", root);
            for (int i = 0, n = 0; i < 400 && n < 16; i++)
            {
                if (!VPickOpen(5.5f, out x, out z, 6f)) continue;
                Reserve(x, z, 4.5f);
                if (Random.value < 0.35f)
                {
                    var go = PropKit.Place(PropKit.TreeProto(5), g, "PohonApel", new Vector3(x, VGy(x, z), z), Random.Range(0f, 360f), Random.Range(0.9f, 1.2f), baseSlots);
                    var cc = go.AddComponent<CapsuleCollider>(); cc.center = new Vector3(0f, 1.5f, 0f); cc.radius = 0.5f; cc.height = 3f;
                }
                else VPlaceTree(g, x, z, Random.Range(0.75f, 1.05f));
                n++;
            }
        }

        // ---- semak dan pakis
        {
            var g = Group("SemakDesa", root);
            for (int i = 0, n = 0; i < 900 && n < 170; i++)
            {
                if (!VPickOpen(1.6f, out x, out z, 3.2f)) continue;
                Reserve(x, z, 1.2f);
                bool fern = Random.value < 0.4f;
                PropKit.Place(vTreeProtos[fern ? 6 : 3], g, fern ? "Pakis" : "Semak", new Vector3(x, VGy(x, z) - 0.05f, z), Random.Range(0f, 360f), Random.Range(0.8f, 1.3f), ForestKit.Slots);
                n++;
            }
        }

        // ---- bunga liar (dilebur per kelompok agar ringan)
        {
            var g = Group("BungaDesa", root);
            int[] cols = { Sl.Blossom, Sl.Cloth, Sl.ClothB, Sl.Gold, Sl.PlasterB };
            for (int chunk = 0; chunk < 6; chunk++)
            {
                var b = new GeoBag();
                int placed = 0;
                for (int i = 0; i < 400 && placed < 22; i++)
                {
                    if (!VPickOpen(1.8f, out x, out z, 3f)) continue;
                    Reserve(x, z, 1.3f);
                    int c1 = cols[rnd.Next(cols.Length)], c2 = cols[rnd.Next(cols.Length)];
                    int k = 9 + rnd.Next(8);
                    for (int f = 0; f < k; f++)
                    {
                        float a = (float)rnd.NextDouble() * 6.28f, rad = (float)rnd.NextDouble() * 1.5f;
                        float fx = x + Mathf.Cos(a) * rad, fz = z + Mathf.Sin(a) * rad, fy = VGy(fx, fz);
                        float h = 0.28f + (float)rnd.NextDouble() * 0.22f;
                        b.Cyl(Sl.Hedge, new Vector3(fx, fy, fz), 0.02f, h, 4, 0.02f, true, false);
                        b.Ball(f % 3 == 0 ? c2 : c1, new Vector3(fx, fy + h, fz), 0.11f + (float)rnd.NextDouble() * 0.05f, 3, 5);
                    }
                    placed++;
                }
                b.Emit(g, "Bunga" + chunk, baseSlots);
            }
        }

        // ---- kolam kecil dengan batu tepi, alang-alang, dan teratai
        if (VPickOpen(13f, out x, out z, 7f))
        {
            var g = Group("Kolam", root);
            float y = VGy(x, z);
            Reserve(x, z, 12f);
            var b = new GeoBag();
            b.Annulus(Sl.Stone, new Vector3(x, y, z), 7.6f, 9f, 0f, 360f, 28, y + 0.25f);
            b.Annulus(Sl.Dirt, new Vector3(x, y, z), 6.6f, 7.7f, 0f, 360f, 28, y + 0.05f);
            b.Disc(Sl.Water, new Vector3(x, y, z), 6.8f, 28, y + 0.14f);
            for (int i = 0; i < 9; i++)
            {
                float a = (float)rnd.NextDouble() * 6.28f, rad = (float)rnd.NextDouble() * 5.2f;
                b.Disc(Sl.Leaf, new Vector3(x + Mathf.Cos(a) * rad, y, z + Mathf.Sin(a) * rad), 0.55f, 8, y + 0.17f);
                if (i % 3 == 0) b.Ball(Sl.Blossom, new Vector3(x + Mathf.Cos(a) * rad, y + 0.22f, z + Mathf.Sin(a) * rad), 0.18f, 3, 6, 0.6f);
            }
            for (int i = 0; i < 26; i++)
            {
                float a = i / 26f * 6.28f + (float)rnd.NextDouble() * 0.2f, rad = 7.0f + (float)rnd.NextDouble() * 1.2f;
                if (i % 2 == 0) b.Cyl(Sl.Hedge, new Vector3(x + Mathf.Sin(a) * rad, y, z + Mathf.Cos(a) * rad), 0.04f, 1.2f + (float)rnd.NextDouble() * 0.8f, 4, 0.02f, true, false);
                else b.Ball(Sl.Stone, new Vector3(x + Mathf.Sin(a) * (rad + 0.8f), y + 0.25f, z + Mathf.Cos(a) * (rad + 0.8f)), 0.5f + (float)rnd.NextDouble() * 0.35f, 3, 6, 0.7f);
            }
            b.Emit(g, "KolamMesh", baseSlots);
            // bangku di tepi kolam
            for (int i = 0; i < 2; i++)
            {
                float a = (i * 180f + 40f) * Mathf.Deg2Rad;
                float bx = x + Mathf.Sin(a) * 11f, bz = z + Mathf.Cos(a) * 11f;
                if (!IsFree(bx, bz, 1.4f)) continue;
                var bp = PropKit.Place(PropKit.Get("Bench", PropKit.Bench), g, "Bangku", new Vector3(bx, VGy(bx, bz), bz), a * Mathf.Rad2Deg + 180f, 1f, baseSlots);
                bp.AddComponent<BoxCollider>().size = new Vector3(1.8f, 0.9f, 0.6f);
                Reserve(bx, bz, 1.5f);
            }
            AddLight(g, new Vector3(x, y + 3f, z), 14f, 0.5f);
            VMapMark("Kolam Desa", new Vector2(x, z), 0);
        }

        // ---- kebun sayur berpagar
        for (int gi = 0; gi < 3; gi++)
        {
            if (!VPickOpen(9f, out x, out z, 6f)) continue;
            var g = Group("KebunSayur" + gi, root);
            float y = VGy(x, z), yaw = Random.Range(0f, 360f);
            Reserve(x, z, 8f);
            var b = new GeoBag();
            var pts = new List<Vector3> { VLoc(x, z, yaw, -5f, -4f), VLoc(x, z, yaw, 5f, -4f), VLoc(x, z, yaw, 5f, 4f), VLoc(x, z, yaw, -5f, 4f), VLoc(x, z, yaw, -5f, -4f) };
            PropKit.Fence(b, pts, 1.0f);
            for (int row = 0; row < 4; row++)
            {
                float lz = -2.6f + row * 1.75f;
                for (int c = 0; c < 8; c++)
                {
                    Vector3 p = VLoc(x, z, yaw, -3.9f + c * 1.12f, lz);
                    b.Box(Sl.Field, p + Vector3.up * 0.12f, new Vector3(0.9f, 0.24f, 0.9f), yaw);
                    int kind = row % 3;
                    b.Ball(kind == 0 ? Sl.Leaf : kind == 1 ? Sl.Hedge : Sl.Cloth, p + Vector3.up * 0.5f, 0.38f, 4, 6, 0.9f);
                }
            }
            b.Emit(g, "Kebun", baseSlots);
            var sc = PropKit.Place(PropKit.Get("Scarecrow", PropKit.Scarecrow), g, "Orang-orangan", VLoc(x, z, yaw, 0f, 0f), yaw + 180f, 1f, baseSlots);
            sc.AddComponent<CapsuleCollider>().height = 2f;
            VMapMark("Kebun Sayur", new Vector2(x, z), 0);
        }

        // ---- jerami, tumpukan kayu, tong & peti
        {
            var g = Group("PernakPernik", root);
            var hay = PropKit.Get("Hay", bg => PropKit.Haystack(bg, Vector3.zero));
            for (int i = 0, n = 0; i < 200 && n < 7; i++)
            {
                if (!VPickOpen(3f, out x, out z, 5f)) continue;
                Reserve(x, z, 2.6f);
                var go = PropKit.Place(hay, g, "Jerami", new Vector3(x, VGy(x, z), z), Random.Range(0f, 360f), Random.Range(0.8f, 1.1f), baseSlots);
                go.AddComponent<CapsuleCollider>().radius = 1.4f;
                n++;
            }
            var wood = PropKit.Get("WoodPile", bg =>
            {
                for (int r = 0; r < 3; r++)
                    for (int c = 0; c < 4 - r; c++)
                        bg.Cyl(Sl.WoodLight, new Vector3(-0.9f + c * 0.5f + r * 0.25f, 0.2f + r * 0.38f, 0f), 0.2f, 1.6f, 7, 0.2f, true, true);
                bg.Box(Sl.Wood, new Vector3(-1.1f, 0.5f, -0.7f), new Vector3(0.12f, 1f, 0.12f));
                bg.Box(Sl.Wood, new Vector3(1.1f, 0.5f, -0.7f), new Vector3(0.12f, 1f, 0.12f));
            });
            var crates = PropKit.Get("CrateBarrel", bg =>
            {
                bg.Box(Sl.Wood, new Vector3(0f, 0.4f, 0f), new Vector3(0.9f, 0.8f, 0.9f), 12f);
                bg.Box(Sl.WoodLight, new Vector3(0.95f, 0.35f, 0.2f), new Vector3(0.7f, 0.7f, 0.7f), -20f);
                bg.Box(Sl.Wood, new Vector3(0.3f, 1.1f, 0f), new Vector3(0.6f, 0.6f, 0.6f), 30f);
                bg.Cyl(Sl.Wood, new Vector3(-1.0f, 0f, 0.3f), 0.45f, 0.95f, 10, 0.4f, true, true);
                bg.Cyl(Sl.Metal, new Vector3(-1.0f, 0.2f, 0.3f), 0.47f, 0.06f, 10, 0.47f, true, true);
                bg.Cyl(Sl.Metal, new Vector3(-1.0f, 0.7f, 0.3f), 0.47f, 0.06f, 10, 0.47f, true, true);
            });
            for (int i = 0, n = 0; i < 200 && n < 5; i++)
            {
                if (!VPickOpen(2.5f, out x, out z, 5f)) continue;
                Reserve(x, z, 2.4f);
                var go = PropKit.Place(wood, g, "TumpukanKayu", new Vector3(x, VGy(x, z), z), Random.Range(0f, 360f), 1f, baseSlots);
                go.AddComponent<BoxCollider>().size = new Vector3(2.4f, 1.4f, 1.8f);
                go.GetComponent<BoxCollider>().center = new Vector3(0f, 0.7f, 0f);
                n++;
            }
            for (int i = 0, n = 0; i < 200 && n < 8; i++)
            {
                if (!VPickOpen(2.2f, out x, out z, 5f)) continue;
                Reserve(x, z, 2.2f);
                var go = PropKit.Place(crates, g, "PetiTong", new Vector3(x, VGy(x, z), z), Random.Range(0f, 360f), 1f, baseSlots);
                go.AddComponent<BoxCollider>().size = new Vector3(2.4f, 1.6f, 1.4f);
                go.GetComponent<BoxCollider>().center = new Vector3(0f, 0.7f, 0.1f);
                n++;
            }
            // sumur tambahan
            if (VPickOpen(3f, out x, out z, 6f))
            {
                Reserve(x, z, 3f);
                var w = PropKit.Place(PropKit.Get("WellL", bg => PropKit.Well(bg, Vector3.zero)), g, "SumurDesa", new Vector3(x, VGy(x, z), z), Random.Range(0f, 360f), 0.9f, baseSlots);
                w.AddComponent<CapsuleCollider>().radius = 1.4f;
                VMapMark("Sumur", new Vector2(x, z), 0);
            }
        }

        // ---- bangku di sepanjang jalan utama
        {
            var g = Group("BangkuJalan", root);
            var bp = PropKit.Get("Bench", PropKit.Bench);
            for (int si = 0; si < Mathf.Min(2, vStreets.Count); si++)
            {
                var st = vStreets[si]; float acc = 0f; int k = 0;
                for (int i = 0; i + 1 < st.Count; i++)
                {
                    Vector2 a = st[i], c = st[i + 1];
                    acc += (c - a).magnitude;
                    if (acc < 32f) continue;
                    acc = 0f;
                    Vector2 tan = (c - a).normalized, nrm = new Vector2(tan.y, -tan.x);
                    float side = (k++ % 2 == 0) ? 1f : -1f;
                    Vector2 p = (a + c) / 2f + nrm * side * 4.3f;
                    if (p.magnitude < 20f || p.magnitude > VR - 8f || !IsFree(p.x, p.y, 1.2f)) continue;
                    Reserve(p.x, p.y, 1.3f);
                    float yaw = Mathf.Atan2(-nrm.x * side, -nrm.y * side) * Mathf.Rad2Deg;
                    var go = PropKit.Place(bp, g, "Bangku", new Vector3(p.x, VGy(p.x, p.y), p.y), yaw, 1f, baseSlots);
                    go.AddComponent<BoxCollider>().size = new Vector3(1.8f, 0.9f, 0.6f);
                }
            }
        }
    }
}
