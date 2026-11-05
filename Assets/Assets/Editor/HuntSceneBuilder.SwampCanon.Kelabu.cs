using System.Collections.Generic;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Desa Kelabu (kanon §8.5: "bendungan jebol, asal Selin"; "perahu rumbia"): tanggul tanah berlapis batu di tepi utara Teluk Nelayan
/// dengan celah jebol di tengahnya (balok patah, bukan terkikis), parit kerikil di bawahnya, dan dua perahu beratap rumbia di air.
/// Semua dalam koordinat dunia, tanpa UnityEngine.Random. Tanggul punya MeshCollider (bisa dinaiki); perahu tanpa collider.
/// </summary>
public static partial class HuntSceneBuilder
{
    static Vector3 swDamC; static float swDamDy;

    /// <summary>Titik dunia pada bingkai tanggul: lx sepanjang tanggul, lz ke hilir (menjauhi danau); y = tinggi tanah.</summary>
    static Vector3 SwDamW(float lx, float lz)
    {
        Vector2 c = Local(new Vector2(swDamC.x, swDamC.z), swDamDy, lx, lz);
        return G(c.x, c.y);
    }

    static void SwDam(Transform root)
    {
        var g = new GameObject("BendunganJebol").transform;
        g.SetParent(root, false);
        Vector2 lake = new Vector2(SwLakes[0].x, SwLakes[0].y);
        float lr = SwLakes[0].z;
        swDamDy = 20f;                                                           // arah radial dari danau ke tanggul (derajat desain)
        Vector2 dc = Pt(lake, swDamDy, lr + 4f);
        swDamC = new Vector3(dc.x, 0f, dc.y);
        DcClear(G(dc.x, dc.y), 26f);

        // tinggi mercu tetap (bendungan): tertinggi tanah di bentang + 1,9 m, minimal di atas air
        float hi = WaterY;
        for (int i = -11; i <= 11; i++) hi = Mathf.Max(hi, SwDamW(i * 2f, 0f).y);
        float crest = hi + 1.9f;

        var b = new GeoBag();
        Vector3 toLake = SwDamW(0f, -6f) - SwDamW(0f, 0f); toLake.y = 0f; toLake.Normalize();
        Vector3 xDir = SwDamW(2f, 0f) - SwDamW(0f, 0f); xDir.y = 0f; xDir.Normalize();
        var prev = new Vector3[4]; bool prevBreach = false; bool has = false;
        for (int i = -11; i <= 11; i++)
        {
            float x = i * 2f, ax = Mathf.Abs(x);
            float floor = Mathf.Max(SwDamW(x, 0f).y, WaterY - 0.4f) + 0.1f;
            float cr = crest;
            bool breach = ax <= 2f;
            if (breach) cr = floor;
            else if (ax <= 4f) cr = Mathf.Lerp(floor, crest, 0.35f + 0.25f * DcHash(i, 91));       // tepi jebol bergerigi
            Vector3 lb = SwDamW(x, -3.4f), lc = SwDamW(x, -1.1f), rc = SwDamW(x, 1.1f), rb = SwDamW(x, 3.4f);
            lb.y = Mathf.Min(lb.y - 0.4f, cr - 0.3f); rb.y = Mathf.Min(rb.y - 0.4f, cr - 0.3f);
            lc.y = cr; rc.y = cr;
            var cur = new[] { lb, lc, rc, rb };
            if (has && !(breach && prevBreach))
            {
                DcQ(b, Sl.Dirt, prev[0], prev[1], cur[1], cur[0], toLake + Vector3.up * 0.9f);            // lereng ke danau
                DcQ(b, Sl.Grass, prev[1], prev[2], cur[2], cur[1], Vector3.up);                             // mercu
                DcQ(b, Sl.Dirt, prev[2], prev[3], cur[3], cur[2], -toLake + Vector3.up * 0.9f);           // lereng ke hilir
                // sisipan batu di kaki lereng danau, selang-seling
                if ((i & 1) == 0)
                    DcQ(b, Sl.Cobble, prev[0] + (prev[1] - prev[0]) * 0.0f + Vector3.up * 0.02f, prev[0] + (prev[1] - prev[0]) * 0.55f + Vector3.up * 0.02f,
                        cur[0] + (cur[1] - cur[0]) * 0.55f + Vector3.up * 0.02f, cur[0] + Vector3.up * 0.02f, toLake + Vector3.up * 0.9f);
            }
            // penutup ujung tanggul menghadap celah
            if (has && breach != prevBreach)
            {
                var e = breach ? prev : cur;                                           // irisan terakhir yang masih utuh
                DcQ(b, Sl.Dirt, e[0], e[1], e[2], e[3], breach ? xDir : -xDir);        // normal menunjuk ke tengah celah
            }
            prev = cur; prevBreach = breach; has = true;
        }

        // sisa tiang pintu air, balok patah, batu, puing
        foreach (float sx in new[] { -4.6f, 4.6f })
        {
            Vector3 p = SwDamW(sx, 0f);
            b.BoxB(Sl.Wood, new Vector3(p.x, p.y - 0.3f, p.z), new Vector3(0.34f, crest - p.y + 0.9f, 0.34f), DcHash((int)sx, 93) * 50f);
        }
        for (int k = 0; k < 6; k++)
        {
            float sx = (k % 2 == 0 ? -1f : 1f) * (3.2f + 1.3f * DcHash(k, 95));
            Vector3 a = SwDamW(sx, 0.3f); a.y = crest - 0.6f - 0.3f * DcHash(k, 96);
            Vector3 c = SwDamW(sx * 0.45f + (DcHash(k, 97) - 0.5f) * 2f, 2.6f + 1.15f * k);
            c.y += 0.3f;
            b.Beam(k % 3 == 0 ? ForestKit.Weathered : Sl.Wood, a, c, 0.26f, 0.18f);
        }
        for (int k = 0; k < 7; k++)
            ForestKit.Blob(b, ForestKit.Rock, SwDamW((DcHash(k, 98) - 0.5f) * 5f, 2f + 2.4f * k), new Vector3(0.55f, 0.32f, 0.5f) * (0.7f + 0.6f * DcHash(k, 99)), 700 + k, 0.3f, 3, 8, 0.5f);
        ForestKit.Rubble(b, SwDamW(0f, 1.5f) + Vector3.up * 0.2f, 2.6f, 8, 77);

        // parit kerikil di bawah celah (mengikuti tanah)
        var pts = new List<Vector2>();
        for (int t = -2; t <= 18; t += 2) pts.Add(Local(new Vector2(swDamC.x, swDamC.z), swDamDy, 0f, t));
        DcGroundStrip(b, ForestKit.Rock, pts, 2.3f, 0.05f);

        var go = b.Emit(g, "Tanggul", ForestKit.SSlots);
        var mf = go != null ? go.GetComponent<MeshFilter>() : null;
        if (mf != null && mf.sharedMesh != null) go.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;

        SwBoats(g, lake, lr);
        DcInfo(g, "InteractPoint_BendunganJebol", SwDamW(0f, 6f), "Periksa bendungan yang jebol", 8f, "Bendungan Jebol",
            "Tanggul tanah berlapis batu ini dulu menahan Teluk Nelayan supaya sawah di bawahnya tetap kering. Di tengahnya ada celah selebar tujuh langkah. Dasar celah penuh balok yang patah, bukan terkikis, dan kerikilnya licin oleh air yang bertahun-tahun mengalir lewat situ.\n\n" +
            "Para nelayan memperbaikinya tiap musim hujan, dan tiap musim hujan celah itu terbuka lagi di tempat yang sama. Dua perahu beratap rumbia ditambatkan di bawah tanggul. \"Cuma perahu yang dipercaya tanah di sini,\" kata mereka. \"Jangan percaya tanah yang terlalu rata.\"");
    }

    // ================================================================== perahu rumbia
    static void SwBoats(Transform g, Vector2 lake, float lr)
    {
        var b = new GeoBag();
        int placed = 0;
        for (int k = 0; k < 2; k++)
        {
            float ang = swDamDy + (k == 0 ? -13f : 11f);
            Vector2 sp = Vector2.zero; bool ok = false;
            for (float r = lr + 1f; r > lr - 24f; r -= 2f)                      // cari air sungguhan: dari tepi ke tengah danau
            {
                Vector2 c = Pt(lake, ang, r);
                if (G(c.x, c.y).y < WaterY - 0.45f) { sp = c; ok = true; break; }
            }
            if (!ok) continue;
            Vector3 pw = G(sp.x, sp.y); pw.y = WaterY;
            float ta = (ang + 90f) * Mathf.Deg2Rad;
            float yaw = DcYawOf(new Vector3(M * Mathf.Sin(ta), 0f, M * Mathf.Cos(ta))) + (k == 0 ? 8f : -14f);
            b.Push(pw, yaw);
            ForestKit.Boat(b, 40 + k, 0f);
            SwCanopy(b);
            b.Pop();
            placed++;
        }
        if (placed > 0) b.Emit(g, "PerahuRumbia", ForestKit.SSlots);
    }

    /// <summary>Atap rumbia melengkung di atas bagian tengah perahu: empat rusuk bambu dan hamparan jerami.</summary>
    static void SwCanopy(GeoBag b)
    {
        var arc = new[] { new Vector2(-0.6f, 0.42f), new Vector2(-0.56f, 0.8f), new Vector2(-0.3f, 1.08f), new Vector2(0f, 1.16f),
                          new Vector2(0.3f, 1.08f), new Vector2(0.56f, 0.8f), new Vector2(0.6f, 0.42f) };
        float[] zr = { -0.95f, -0.32f, 0.32f, 0.95f };
        foreach (float z in zr)
            for (int i = 0; i + 1 < arc.Length; i++)
                b.Beam(Sl.WoodLight, new Vector3(arc[i].x, arc[i].y, z), new Vector3(arc[i + 1].x, arc[i + 1].y, z), 0.05f, 0.05f);
        for (int i = 0; i + 1 < arc.Length; i++)
        {
            Vector2 m = (arc[i] + arc[i + 1]) * 0.5f - new Vector2(0f, 0.3f);
            Vector2 n = m.normalized * 0.05f;
            Vector3 a0 = new Vector3(arc[i].x + n.x, arc[i].y + n.y, -1.25f), a1 = new Vector3(arc[i + 1].x + n.x, arc[i + 1].y + n.y, -1.25f);
            Vector3 b0 = new Vector3(a0.x, a0.y, 1.25f), b1 = new Vector3(a1.x, a1.y, 1.25f);
            DcQ(b, Sl.Thatch, a0, a1, b1, b0, new Vector3(m.x, m.y, 0f));
        }
    }
}
