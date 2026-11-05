using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HunterGuild;

/// <summary>Gunung Vulkanik: isi khusus (kuil api, gua magma, geyser, retakan lava, obsidian, tiang basalt, lentera jalur, papan arah, bahan gathering).</summary>
public static partial class HuntSceneBuilder
{
    static readonly Vector2[] VolcanoRiver2 = { new Vector2(50, 225), new Vector2(110, 245), new Vector2(190, 270), new Vector2(270, 300), new Vector2(350, 340), new Vector2(420, 385) };
    // x, z, radius (koordinat design)
    static readonly Vector3[] VolcanoPools = { new Vector3(150, -170, 38), new Vector3(-60, -250, 28), new Vector3(-290, 90, 40), new Vector3(330, -190, 34), new Vector3(-300, -170, 42), new Vector3(120, 330, 30) };

    /// <summary>Titik di sepanjang jalur: x,y = posisi design, z = arah (radian, atan2(dz,dx)).</summary>
    static List<Vector3> VWalk(Vector2[] pts, float step)
    {
        var l = new List<Vector3>();
        float carry = 0f;
        for (int i = 0; i + 1 < pts.Length; i++)
        {
            Vector2 a = pts[i], b = pts[i + 1];
            float len = Vector2.Distance(a, b);
            Vector2 d = (b - a) / Mathf.Max(0.001f, len);
            float ang = Mathf.Atan2(d.y, d.x);
            for (float s = carry; s < len; s += step) { Vector2 p = a + d * s; l.Add(new Vector3(p.x, p.y, ang)); carry = s + step - len; }
        }
        return l;
    }

    static bool VOk(float x, float z, float minTrail, float maxSlope)
    {
        if (Mathf.Abs(x) > 430f || Mathf.Abs(z) > 430f) return false;
        if (GroundY(x, z) < WaterY + 1.2f || Slope(x, z) > maxSlope || NearClearing(x, z)) return false;
        if (DistToTrails(x, z) < minTrail) return false;
        return true;
    }

    static void VolcanoExtras()
    {
        var rnd = new System.Random(555);
        var tV = Group("VulkanikDekor");

        // bahan khusus
        eSlots[Sl.Glass] = Mat("VObsidian", new Color(0.025f, 0.025f, 0.04f));
        var obs = eSlots[Sl.Glass];
        obs.SetFloat("_Glossiness", 0.9f); obs.SetFloat("_Smoothness", 0.9f); obs.SetFloat("_Metallic", 0.3f);
        Color flame = new Color(1f, 0.45f, 0.1f);

        // ---------- cahaya sungai lava kedua dan kolam
        for (int i = 0; i < VolcanoRiver2.Length; i++) ELight(tV, G(VolcanoRiver2[i].x, VolcanoRiver2[i].y) + Vector3.up * 3f, new Color(1f, 0.4f, 0.08f), 40f, 2f, false);
        for (int i = 0; i < VolcanoPools.Length; i++) ELight(tV, G(VolcanoPools[i].x, VolcanoPools[i].y) + Vector3.up * 4f, new Color(1f, 0.4f, 0.08f), VolcanoPools[i].z * 2.4f, 2.2f, false);

        // ---------- kuil api (Altar Pemuja Api)
        {
            Vector2 c = V(-200, 170);
            clearings.Add(new Vector3(c.x, c.y, 44f));
            var temple = EKit("VTemple", VTemple, tV, G(c.x, c.y), 90f, 1f, true);
            foreach (var lp in new[] { new Vector3(-5f, 3.6f, 12f), new Vector3(5f, 3.6f, 12f), new Vector3(0f, 6f, 20f) })
                ELight(tV, temple.transform.TransformPoint(lp), flame, 24f, 2.4f);
            ELight(tV, temple.transform.TransformPoint(new Vector3(0f, 4f, 24f)), new Color(1f, 0.35f, 0.08f), 36f, 3f, false);
        }

        // ---------- gua magma (dua mulut gua)
        foreach (var cv in new[] { new Vector3(-130f, 215f, -55f), new Vector3(230f, 110f, 90f) })
        {
            if (GroundY(cv.x, cv.y) < WaterY + 1f) continue;
            clearings.Add(new Vector3(cv.x, cv.y, 26f));
            var cave = EKit("VCave", VCave, tV, G(cv.x, cv.y), cv.z, 1f, true);
            ELight(tV, cave.transform.TransformPoint(new Vector3(0f, 3f, 15f)), new Color(1f, 0.38f, 0.1f), 30f, 2.8f, false);
            ELight(tV, cave.transform.TransformPoint(new Vector3(0f, 2f, 20f)), flame, 16f, 1.6f);
            // tiang api di depan gua
            foreach (float sx in new[] { -1f, 1f })
            {
                Vector3 wp = cave.transform.TransformPoint(new Vector3(sx * 7.5f, 0f, 19f));
                wp.y = GroundYW(wp.x, wp.z);
                var br = EKit("VBrazier", VBrazier, tV, wp, 0f, 1f, true);
                ELight(tV, wp + Vector3.up * 3.6f, flame, 18f, 1.8f);
            }
            if (GameObjectExists(cave)) { }
        }

        // ---------- geyser
        var geysers = new List<Vector2>();
        for (int i = 0; i < 4000 && geysers.Count < 14; i++)
        {
            float x = Random.Range(-400f, 400f), z = Random.Range(-300f, 330f);
            if (!VOk(x, z, 9f, 14f) || DistToTrails(x, z) > 70f) continue;
            if (Dist(x, z, 0f, 215f) < 70f) continue;
            bool near = false;
            foreach (var g in geysers) if (Dist(x, z, g.x, g.y) < 45f) { near = true; break; }
            if (near) continue;
            geysers.Add(V(x, z));
            clearings.Add(new Vector3(x, z, 7f));
            var vent = EKit("VVent", VVent, tV, G(x, z), Random.Range(0f, 360f), 1f, false);
            var top = new GameObject("Geyser").transform;
            top.SetParent(vent.transform, false);
            top.position = vent.transform.position + Vector3.up * 0.9f;
            var gc = top.gameObject.AddComponent<VolcanoGeyser>();
            gc.period = Random.Range(12f, 20f);
            // bahan belerang di sekitar geyser
            for (int k = 0; k < 2; k++)
            {
                Vector2 o = Random.insideUnitCircle.normalized * Random.Range(6f, 10f);
                if (GroundY(x + o.x, z + o.y) > WaterY + 1f && Slope(x + o.x, z + o.y) < 24f) VolcanoNode("brimstone", G(x + o.x, z + o.y));
            }
        }

        // ---------- retakan lava
        int cracks = 0;
        for (int i = 0; i < 3000 && cracks < 70; i++)
        {
            float x = Random.Range(-420f, 420f), z = Random.Range(-400f, 400f);
            if (!VOk(x, z, 5f, 26f) || Dist(x, z, ECamp.x, ECamp.y) < 75f) continue;
            float heading = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            int segs = Random.Range(6, 13);
            Vector2 cur = V(x, z);
            bool lit = cracks % 6 == 0;
            for (int s = 0; s < segs; s++)
            {
                heading += Random.Range(-0.65f, 0.65f);
                float len = Random.Range(4f, 7.5f);
                Vector2 nxt = cur + new Vector2(Mathf.Cos(heading), Mathf.Sin(heading)) * len;
                if (!VOk(nxt.x, nxt.y, 3.5f, 32f)) break;
                Vector3 a = G(cur.x, cur.y), b = G(nxt.x, nxt.y);
                var cube = Prim(PrimitiveType.Cube, "Retakan", tRocks, (a + b) * 0.5f + Vector3.up * 0.1f, new Vector3(Random.Range(0.45f, 1.3f) * (1f - s / (float)segs * 0.5f), 0.12f, Vector3.Distance(a, b) + 0.3f), mLava, false);
                Vector3 dir = (b - a);
                if (dir.sqrMagnitude > 0.001f) cube.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                cur = nxt;
            }
            if (lit) ELight(tRocks, G(x, z) + Vector3.up * 1.5f, new Color(1f, 0.38f, 0.08f), 13f, 1.3f, false);
            cracks++;
        }

        // ---------- obsidian dan tiang basalt
        int made = 0;
        for (int i = 0; i < 4000 && made < 90; i++)
        {
            float x = Random.Range(-430f, 430f), z = Random.Range(-420f, 420f);
            if (!VOk(x, z, 6f, 30f)) continue;
            int v = Random.Range(0, 4);
            var go = EKit("VObs" + v, b => VObsidian(b, v), tRocks, G(x, z), Random.Range(0f, 360f), Random.Range(0.9f, 2.3f), true);
            made++;
        }
        made = 0;
        for (int i = 0; i < 4000 && made < 40; i++)
        {
            float x = Random.Range(-430f, 430f), z = Random.Range(-420f, 420f);
            if (!VOk(x, z, 8f, 40f)) continue;
            bool lavaNear = DistToPoly(x, z, LavaRiver(1)) < 70f || DistToPoly(x, z, VolcanoRiver2) < 70f;
            if (!lavaNear && Random.value > 0.35f) continue;
            int v = Random.Range(0, 3);
            EKit("VBasalt" + v, b => VBasalt(b, v), tRocks, G(x, z), Random.Range(0f, 360f), Random.Range(1f, 2f), true);
            made++;
        }

        // ---------- lentera dan pos api di sepanjang jalur utama ke boss
        var main = ETrails(1)[0];
        var pts = VWalk(main, 24f);
        int n = 0;
        foreach (var pt in pts)
        {
            if (pt.y < -300f || pt.y > 300f) { n++; continue; }
            float side = n % 2 == 0 ? 1f : -1f;
            Vector2 perp = new Vector2(-Mathf.Sin(pt.z), Mathf.Cos(pt.z)) * 5.8f * side;
            float x = pt.x + perp.x, z = pt.y + perp.y;
            n++;
            if (GroundY(x, z) < WaterY + 1f || Slope(x, z) > 30f) continue;
            Vector3 wp = G(x, z);
            EKit("VLantern", VLantern, tV, wp, 0f, 1f, true);
            if (n % 3 == 0) ELight(tV, wp + Vector3.up * 3.7f, flame, 20f, 1.7f);
        }
        // gerbang pos api tua
        foreach (float zt in new[] { -160f, 30f })
        {
            Vector3 best = new Vector3(0f, zt, 0f); float bd = 9999f;
            foreach (var pt in VWalk(main, 4f)) { float d = Mathf.Abs(pt.y - zt); if (d < bd) { bd = d; best = pt; } }
            clearings.Add(new Vector3(best.x, best.y, 14f));
            var arch = EKit("VArch" + (int)zt, b => ForestKit.Archway(b, 6, 0.15f), tV, G(best.x, best.y), 0f, 1.25f, true);
            foreach (float sx in new[] { -1f, 1f })
            {
                Vector3 wp = G(best.x + sx * 11f, best.y);
                EKit("VBrazier", VBrazier, tV, wp, 0f, 1f, true);
                ELight(tV, wp + Vector3.up * 3.6f, flame, 20f, 1.8f);
            }
        }

        // ---------- papan arah
        VSign(V(8f, -282f), "SARANG IGNAROK >> LURUS KE UTARA", 7.2f);
        VSign(V(-14f, -92f), "<< SUNGAI LAVA", 4.6f);
        VSign(V(14f, -92f), "NGARAI BELERANG >>", 5.2f);
        VSign(V(-14f, 112f), "<< ALTAR API", 4.2f);
        VSign(V(14f, 112f), "SISI TIMUR >>", 4.4f);
        VSign(V(8f, 225f), "AWAS: SARANG IGNAROK", 5.6f);

        // ---------- bahan gathering khusus
        for (int i = 0, k = 0; i < 400 && k < 18; i++)
        {
            var zd = Zones[Random.Range(2, 6)];
            Vector3? p = FindLand(zd.x, zd.z, zd.r, 25f);
            if (!p.HasValue) continue;
            VolcanoNode("brimstone", p.Value); k++;
        }
        for (int i = 0, k = 0; i < 400 && k < 7; i++)
        {
            var zd = Zones[Random.Range(0, 2) == 0 ? 2 : 5];
            Vector3? p = FindLand(zd.x, zd.z, zd.r, 25f);
            if (!p.HasValue) continue;
            VolcanoNode("magma_core", p.Value); k++;
        }
    }

    static bool GameObjectExists(GameObject g) { return g != null; }

    static void VSign(Vector2 d, string text, float width)
    {
        if (GroundY(d.x, d.y) < WaterY + 1f) return;
        var sg = new GameObject("PapanArah").transform;
        sg.SetParent(tProps, false);
        sg.position = G(d.x, d.y);
        Prim(PrimitiveType.Cube, "Tiang", sg, new Vector3(0, 1.6f, 0), new Vector3(0.28f, 3.2f, 0.28f), mWood);
        Prim(PrimitiveType.Cube, "Papan", sg, new Vector3(0, 3.1f, 0), new Vector3(width, 1f, 0.16f), mDark, false);
        BuildKit.Text(sg, text, new Vector3(0, 3.1f, 0.12f), 180f, 0.075f, new Color(1f, 0.72f, 0.35f));
    }

    static void VolcanoNode(string itemId, Vector3 pos)
    {
        var n = new GameObject("Node_" + itemId);
        n.transform.SetParent(tNodes, false);
        n.transform.position = pos;
        var vis = new GameObject("Visual");
        vis.transform.SetParent(n.transform, false);
        var sulfur = TownMats.Emissive(Mat("VSulfur", new Color(0.9f, 0.78f, 0.12f)), new Color(0.35f, 0.28f, 0.02f));
        var rockM = Mat("VNodeRock", new Color(0.12f, 0.09f, 0.09f));
        string label;
        if (itemId == "brimstone")
        {
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad, r = i == 0 ? 0f : 0.35f;
                var c = Prim(PrimitiveType.Cylinder, "Kristal", vis.transform, new Vector3(Mathf.Cos(a) * r, 0.3f + (i == 0 ? 0.25f : 0f), Mathf.Sin(a) * r), new Vector3(0.14f, 0.35f + (i == 0 ? 0.25f : 0.05f * (i % 3)), 0.14f), sulfur, false);
                c.transform.localRotation = Quaternion.Euler(Random.Range(-18f, 18f), 0f, Random.Range(-18f, 18f));
            }
            Prim(PrimitiveType.Sphere, "Batu", vis.transform, new Vector3(0, 0.1f, 0), new Vector3(0.9f, 0.3f, 0.9f), rockM, false);
            label = "Kumpulkan Belerang";
        }
        else
        {
            Prim(PrimitiveType.Sphere, "Batu", vis.transform, new Vector3(0, 0.45f, 0), new Vector3(1.1f, 0.9f, 1f), rockM, false);
            Prim(PrimitiveType.Sphere, "Inti", vis.transform, new Vector3(0, 0.5f, 0.28f), new Vector3(0.55f, 0.55f, 0.4f), mLava, false);
            ELight(n.transform, pos + Vector3.up * 1.1f, new Color(1f, 0.4f, 0.1f), 9f, 1f, false);
            label = "Gali Inti Magma";
        }
        var gn = n.AddComponent<GatherNode>();
        gn.itemId = itemId; gn.visual = vis; gn.prompt = label; gn.radius = 2.8f;
        if (itemId == "magma_core") { gn.minAmount = 1; gn.maxAmount = 1; gn.respawnSeconds = 600f; }
    }

    // ------------------------------------------------------------------ model
    static void VTemple(GeoBag b)
    {
        b.Box(Sl.Stone, new Vector3(0, 0.15f, 12), new Vector3(36, 0.3f, 28));
        b.Box(Sl.DarkStone, new Vector3(0, 0.8f, 17), new Vector3(20, 1.2f, 10));
        b.Box(Sl.Stone, new Vector3(0, 0.35f, 10), new Vector3(12, 0.7f, 2.4f));
        b.Box(Sl.Stone, new Vector3(-10.5f, 6, 23), new Vector3(13, 12, 2.4f));
        b.Box(Sl.Stone, new Vector3(10.5f, 6, 23), new Vector3(13, 12, 2.4f));
        b.Box(Sl.DarkStone, new Vector3(0, 10, 23), new Vector3(8.4f, 4, 2.4f));
        b.Box(Sl.Glow, new Vector3(0, 4f, 23.6f), new Vector3(7.4f, 7.6f, 0.4f));
        b.Box(Sl.DarkStone, new Vector3(-17.5f, 7, 18), new Vector3(2.4f, 14, 11));
        b.Box(Sl.DarkStone, new Vector3(17.5f, 7, 18), new Vector3(2.4f, 14, 11));
        foreach (float sx in new[] { -1f, 1f })
        {
            foreach (float z in new[] { 2f, 8f, 14f })
            {
                b.Cyl(Sl.Marble, new Vector3(sx * 13f, 0.3f, z), 1.3f, 10f, 8, 1.05f, true);
                b.Box(Sl.DarkStone, new Vector3(sx * 13f, 10.8f, z), new Vector3(3.2f, 1f, 3.2f));
            }
            b.Box(Sl.Stone, new Vector3(sx * 13f, 11.9f, 8f), new Vector3(2.8f, 1.2f, 15f));
            b.Cyl(Sl.DarkStone, new Vector3(sx * 5f, 0.3f, 12f), 0.95f, 1.8f, 8, 0.7f, true);
            b.Ball(Sl.Glow, new Vector3(sx * 5f, 2.9f, 12f), 0.95f, 6, 8);
        }
        b.Disc(Sl.Glow, new Vector3(0, 0, 5), 4.5f, 32, 0.34f);
        b.Disc(Sl.DarkStone, new Vector3(0, 0, 5), 2.6f, 32, 0.36f);
        b.Cyl(Sl.DarkStone, new Vector3(0, 1.4f, 17), 1.6f, 2.2f, 8, 1.2f, true);
        b.Ball(Sl.Glow, new Vector3(0, 4.3f, 17), 1.2f, 6, 8);
        VTempleArt(b);
    }

    static void VCave(GeoBag b)
    {
        b.Cone(Sl.DarkStone, new Vector3(0, -2f, 0), 20f, 19f, 9);
        b.Cone(Sl.Stone, new Vector3(-9f, -2f, 4f), 9f, 11f, 7);
        b.Cone(Sl.Stone, new Vector3(9f, -2f, 3f), 10f, 9f, 7);
        b.Box(Sl.Dark, new Vector3(0, 3.6f, 14.1f), new Vector3(6.4f, 7.2f, 0.5f));
        foreach (float sx in new[] { -1f, 1f })
        {
            b.Cyl(Sl.Stone, new Vector3(sx * 4.2f, -0.3f, 14.4f), 1.1f, 8.4f, 7, 0.8f, true);
            b.Cyl(Sl.DarkStone, new Vector3(sx * 7.4f, -0.3f, 12.5f), 1.6f, 4.4f, 6, 1f, true);
        }
        b.Box(Sl.Stone, new Vector3(0, 8.4f, 14.4f), new Vector3(10.2f, 1.7f, 2.4f));
        b.Box(Sl.Glow, new Vector3(0, 0.12f, 15.4f), new Vector3(5.2f, 0.18f, 3.2f));
        for (int i = 0; i < 6; i++)
        {
            float a = (i - 2.5f) * 0.34f;
            b.Cyl(Sl.Glass, new Vector3(Mathf.Sin(a) * 16f, -0.3f, 12f + Mathf.Cos(a) * 3.5f + i % 2), 0.55f, 2.4f + (i % 3), 5, 0.02f, false);
        }
        VCaveArt(b);
    }

    static void VBrazier(GeoBag b)
    {
        b.Cyl(Sl.DarkStone, Vector3.zero, 0.7f, 2.4f, 8, 0.45f, true);
        b.Cyl(Sl.DarkStone, new Vector3(0, 2.4f, 0), 0.8f, 0.5f, 8, 1.1f, true);
        b.Ball(Sl.Glow, new Vector3(0, 3.3f, 0), 0.75f, 6, 8, 1.2f);
        VBrazierArt(b);
    }

    static void VLantern(GeoBag b)
    {
        b.Cyl(Sl.Stone, Vector3.zero, 0.5f, 0.5f, 8, 0.38f, true);
        b.Cyl(Sl.DarkStone, new Vector3(0, 0.5f, 0), 0.17f, 2.8f, 6, 0.14f, true);
        b.Cyl(Sl.DarkStone, new Vector3(0, 3.3f, 0), 0.34f, 0.18f, 6, 0.4f, true);
        b.Ball(Sl.Glow, new Vector3(0, 3.7f, 0), 0.36f, 6, 8, 1.2f);
        VLanternArt(b);
    }

    static void VVent(GeoBag b)
    {
        b.Cyl(Sl.DarkStone, Vector3.zero, 3.3f, 0.9f, 10, 2.3f, true);
        b.Disc(Sl.Glow, Vector3.zero, 1.5f, 14, 0.93f);
        for (int i = 0; i < 5; i++)
        {
            float a = i * 72f * Mathf.Deg2Rad;
            b.Cyl(Sl.Glass, new Vector3(Mathf.Cos(a) * 3.8f, -0.2f, Mathf.Sin(a) * 3.8f), 0.45f, 1.4f + (i % 2), 5, 0.02f, false);
        }
        VVentArt(b);
    }

    static void VObsidian(GeoBag b, int v)
    {
        var r = new System.Random(300 + v);
        int n = 5 + v;
        for (int i = 0; i < n; i++)
        {
            float a = (float)r.NextDouble() * 6.28f, rad = i == 0 ? 0f : 0.6f + (float)r.NextDouble() * 1.6f;
            float h = (i == 0 ? 4.5f : 1.6f) + (float)r.NextDouble() * 3.4f;
            b.Cyl(Sl.Glass, new Vector3(Mathf.Cos(a) * rad, -0.3f, Mathf.Sin(a) * rad), 0.35f + (float)r.NextDouble() * 0.55f, h, 5, 0.02f, false);
        }
        b.Cyl(Sl.DarkStone, new Vector3(0, -0.4f, 0), 1.9f, 0.7f, 8, 1.1f, true);
    }

    static void VBasalt(GeoBag b, int v)
    {
        var r = new System.Random(700 + v);
        int n = 7 + v * 2;
        for (int i = 0; i < n; i++)
        {
            float a = i * 2.4f, rad = i == 0 ? 0f : 1.2f + 0.9f * Mathf.Sqrt(i);
            float h = 3.5f + (float)r.NextDouble() * 9f - rad * 0.8f;
            if (h < 1.5f) h = 1.5f;
            b.Cyl(Sl.DarkStone, new Vector3(Mathf.Cos(a) * rad, -1f, Mathf.Sin(a) * rad), 1.0f + (float)r.NextDouble() * 0.5f, h + 1f, 6, -1f, true);
        }
    }
}
