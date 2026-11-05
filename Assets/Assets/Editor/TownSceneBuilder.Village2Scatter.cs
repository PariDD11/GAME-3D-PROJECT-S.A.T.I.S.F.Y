using System.Collections.Generic;
using UnityEngine;
using HunterGuild;

/// <summary>Kampung Tepi Kali: tumbuhan di sela-sela desa (pohon peneduh, pohon apel, semak, pakis, bunga liar, rumpun rumput).
/// Hanya di ruang kosong dalam cekungan: jauh dari jalan, kali, bangunan, dan area yang sudah dipakai (Reserve).</summary>
public static partial class TownSceneBuilder
{
    static bool V2OpenSpot(float x, float z, float r, float roadGap)
    {
        float ex = (x - V2.Basin.x) / V2.BasinRx, ez = (z - V2.Basin.y) / V2.BasinRz;
        if (ex * ex + ez * ez > 0.84f * 0.84f) return false;                 // hanya di dalam cekungan (lereng hutan diisi V2Trees)
        if (!IsFree(x, z, r)) return false;
        foreach (var rd in V2.Roads) if (V2.DistPoly(x, z, rd.pts) < rd.width * 0.5f + roadGap) return false;
        if (V2.DistPoly(x, z, V2.Stream) < V2.StreamHalfWidth(x, z) + 2.5f) return false;
        if (V2OnBridge(new Vector2(x, z))) return false;
        if (V2.H(x, z) < -0.3f || VSlopeDeg(x, z) > 18f) return false;
        return true;
    }

    static bool V2PickOpen(float r, out float x, out float z, float roadGap)
    {
        for (int i = 0; i < 80; i++)
        {
            x = V2.Basin.x + Random.Range(-1f, 1f) * V2.BasinRx * 0.84f;
            z = V2.Basin.y + Random.Range(-1f, 1f) * V2.BasinRz * 0.84f;
            if (V2OpenSpot(x, z, r, roadGap)) return true;
        }
        x = z = 0f; return false;
    }

    static void V2Scatter()
    {
        var root = Group("Asri", tProps);
        var rnd = new System.Random(2026);
        float x, z;

        // ---- pohon peneduh & pohon apel di padang rumput
        {
            var g = Group("PohonDesa", root);
            for (int i = 0, n = 0; i < 600 && n < 22; i++)
            {
                if (!V2PickOpen(5.5f, out x, out z, 6f)) continue;
                Reserve(x, z, 4.5f);
                if (Random.value < 0.35f)
                {
                    var go = PropKit.Place(PropKit.TreeProto(5), g, "PohonApel", new Vector3(x, V2.H(x, z), z), Random.Range(0f, 360f), Random.Range(0.9f, 1.2f), baseSlots);
                    var cc = go.AddComponent<CapsuleCollider>(); cc.center = new Vector3(0f, 1.5f, 0f); cc.radius = 0.5f; cc.height = 3f;
                }
                else VPlaceTree(g, x, z, Random.Range(0.75f, 1.05f));
                n++;
            }
        }

        // ---- semak dan pakis
        {
            var g = Group("SemakDesa", root);
            for (int i = 0, n = 0; i < 1500 && n < 220; i++)
            {
                if (!V2PickOpen(1.6f, out x, out z, 2.4f)) continue;
                Reserve(x, z, 1.2f);
                bool fern = Random.value < 0.4f;
                PropKit.Place(vTreeProtos[fern ? 6 : 3], g, fern ? "Pakis" : "Semak", new Vector3(x, V2.H(x, z) - 0.05f, z), Random.Range(0f, 360f), Random.Range(0.8f, 1.3f), ForestKit.Slots);
                n++;
            }
        }

        // ---- bunga liar (dilebur per kelompok agar ringan)
        {
            var g = Group("BungaDesa", root);
            int[] cols = { Sl.Blossom, Sl.Cloth, Sl.ClothB, Sl.Gold, Sl.PlasterB };
            for (int chunk = 0; chunk < 8; chunk++)
            {
                var b = new GeoBag();
                int placed = 0;
                for (int i = 0; i < 500 && placed < 24; i++)
                {
                    if (!V2PickOpen(1.8f, out x, out z, 2.6f)) continue;
                    Reserve(x, z, 1.3f);
                    int c1 = cols[rnd.Next(cols.Length)], c2 = cols[rnd.Next(cols.Length)];
                    int k = 9 + rnd.Next(8);
                    for (int f = 0; f < k; f++)
                    {
                        float a = (float)rnd.NextDouble() * 6.28f, rad = (float)rnd.NextDouble() * 1.5f;
                        float fx = x + Mathf.Cos(a) * rad, fz = z + Mathf.Sin(a) * rad, fy = V2.H(fx, fz);
                        float h = 0.28f + (float)rnd.NextDouble() * 0.22f;
                        b.Cyl(Sl.Hedge, new Vector3(fx, fy, fz), 0.02f, h, 4, 0.02f, true, false);
                        b.Ball(f % 3 == 0 ? c2 : c1, new Vector3(fx, fy + h, fz), 0.11f + (float)rnd.NextDouble() * 0.05f, 3, 5);
                    }
                    placed++;
                }
                b.Emit(g, "Bunga" + chunk, baseSlots);
            }
        }

        // ---- rumpun rumput tinggi (bilah segitiga) agar padang tidak terlihat rata
        {
            var g = Group("RumputDesa", root);
            for (int chunk = 0; chunk < 8; chunk++)
            {
                var b = new GeoBag();
                int placed = 0;
                for (int i = 0; i < 400 && placed < 60; i++)
                {
                    if (!V2PickOpen(0.6f, out x, out z, 1.6f)) continue;
                    int blades = 5 + rnd.Next(4);
                    for (int f = 0; f < blades; f++)
                    {
                        float a = (float)rnd.NextDouble() * 6.28f, rad = (float)rnd.NextDouble() * 0.45f;
                        float fx = x + Mathf.Cos(a) * rad, fz = z + Mathf.Sin(a) * rad;
                        b.Cone(f % 3 == 0 ? Sl.Hedge : Sl.Leaf, new Vector3(fx, V2.H(fx, fz) - 0.02f, fz), 0.05f, 0.35f + (float)rnd.NextDouble() * 0.4f, 3);
                    }
                    placed++;
                }
                b.Emit(g, "Rumput" + chunk, baseSlots);
            }
        }
    }
}
