using UnityEngine;
using HunterGuild;

/// <summary>
/// Kanon Padang Hantu, bagian 5: Makam Raja Malrek. Arena (desain (0, 300) r 36) dan monster Malrek sudah ada; di belakang arena, pusat desain (0, 356),
/// berdiri makam yang menghadap arena: pelataran, dua tingkat batu, aula dengan serambi empat tiang, pintu ganda tersegel lingkaran biru pucat,
/// piramida atap berundak dengan mahkota duri tulang. Makam tidak bisa dimasuki. Ketinggian pelataran dihitung dari tanah tertinggi di jejaknya,
/// fondasi dibenamkan sampai tanah terendah (aman di lereng), dan bila tanah di depan lebih rendah ditambah tangga. Benda lama (pohon, batu, kerangka)
/// yang berdiri di jejak dicabut, dan monster yang kebetulan berdiri di sana dipindah ke depan tangga.
/// </summary>
public static partial class HuntSceneBuilder
{
    const float BnMz = 356f;                                           // pusat desain; +Z lokal = -z desain (menghadap arena)

    /// <summary>Tanah terendah dan tertinggi pada kotak dunia (wx +- hx, wz +- hz).</summary>
    static void BnSpan(float wx, float wz, float hx, float hz, out float lo, out float hi)
    {
        lo = 9999f; hi = -9999f;
        for (int i = 0; i <= 4; i++)
            for (int j = 0; j <= 4; j++)
            {
                float y = GroundYW(wx + (i / 2f - 1f) * hx, wz + (j / 2f - 1f) * hz);
                if (y < lo) lo = y;
                if (y > hi) hi = y;
            }
    }

    /// <summary>Cabut benda lama di tProps dan tRocks yang berdiri di kotak dunia; mengembalikan jumlahnya.</summary>
    static int BnClearBox(float x0, float x1, float z0, float z1)
    {
        int n = 0;
        foreach (var t in new[] { tProps, tRocks })
        {
            if (t == null) continue;
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var ch = t.GetChild(i);
                Vector3 q = ch.position;
                if (q.x >= x0 && q.x <= x1 && q.z >= z0 && q.z <= z1) { Object.DestroyImmediate(ch.gameObject); n++; }
            }
        }
        return n;
    }

    /// <summary>Pindahkan monster yang berdiri di kotak dunia ke depan jejak (z dunia toWz); mengembalikan jumlahnya.</summary>
    static int BnMoveMonsters(float x0, float x1, float z0, float z1, float toWz)
    {
        Transform mg = tRoot != null ? tRoot.Find("Monsters") : null;
        if (mg == null) return 0;
        int n = 0;
        for (int i = 0; i < mg.childCount; i++)
        {
            var ch = mg.GetChild(i);
            Vector3 q = ch.position;
            if (q.x >= x0 && q.x <= x1 && q.z >= z0 && q.z <= z1) { ch.position = Gw(q.x, toWz); n++; }
        }
        return n;
    }

    static void BnMakam(Transform root, Material[] slots)
    {
        var g = new GameObject("MakamRaja").transform;
        g.SetParent(root, false);
        Vector3 c = G(0f, BnMz);
        float lo, hi;
        BnSpan(c.x, c.z + 2.25f, 13.5f, 12f, out lo, out hi);
        float top = hi + 0.12f;
        Vector3 o = new Vector3(c.x, top, c.z);
        float fd = top - (lo - 0.8f);
        bnCleared += BnClearBox(o.x - 15f, o.x + 15f, o.z - 11f, o.z + 16f);
        bnMoved = BnMoveMonsters(o.x - 15f, o.x + 15f, o.z - 11f, o.z + 16f, o.z + 19f);

        var b = new GeoBag();
        b.Push(o, 0f);
        BnTomb(b, fd);
        float drop = top - GroundYW(o.x, o.z + 14.6f);                                                    // tangga dari tanah ke pelataran bila perlu
        if (drop > 0.25f)
        {
            int n = Mathf.CeilToInt(drop / 0.2f);
            const float run = 0.6f;
            b.Stairs(Sl.Stone, new Vector3(0f, -drop, 14f + run / 2f + (n - 1) * run), 9f, n, drop / n, run, 0f);
        }
        b.Pop();
        var go = b.Emit(g, "MakamRajaMalrek", slots);
        go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;

        Color ghost = new Color(0.45f, 0.78f, 1f);
        ELight(g, o + new Vector3(0f, 4.6f, 6.8f), ghost, 24f, 1.5f, false);                              // cahaya segel
        foreach (float sx in new[] { -5.5f, 5.5f }) ELight(g, o + new Vector3(sx, 2.3f, 11.2f), ghost, 18f, 1.3f);

        BnInfo(g, "InteractPoint_MakamRaja", new Vector3(o.x, top + 0.1f, o.z + 15.8f), "Periksa makam", 6.5f, "Makam Raja",
            "Makam ini lebih tinggi daripada apa pun di dataran, dan satu-satunya yang berdiri tegak. Pintu gandanya dirapatkan dari luar dan dibubuhi lingkaran segel yang masih bersinar pucat. " +
            "Tak ada rantai, tak ada gembok, dan tak ada jejak orang yang pernah membukanya.\n\n" +
            "Tulisan pada ambang pintu tinggal kerak; hanya satu nama yang masih terbaca di antara guratan yang terkikis: Malrek. " +
            "Para pemburu yang pulang dari arena berkata makam ini tak pernah terasa kosong, dan tak pernah terasa dijaga.");
    }

    /// <summary>Badan makam. Titik asal di tengah atas pelataran, muka +Z. fd = tinggi fondasi di bawah pelataran.</summary>
    static void BnTomb(GeoBag b, float fd)
    {
        b.BoxB(Sl.DarkStone, new Vector3(0f, -fd, 2.25f), new Vector3(27f, fd, 23.5f));                    // pelataran + fondasi
        b.BoxB(Sl.Stone, new Vector3(0f, 0f, 0f), new Vector3(23f, 1.2f, 16.5f));                          // tingkat 1
        b.BoxB(Sl.Stone, new Vector3(0f, 1.2f, -0.4f), new Vector3(18.5f, 1.1f, 12.2f));                   // tingkat 2
        b.Box(Sl.DarkStone, new Vector3(0f, 2.28f, -0.4f), new Vector3(18.9f, 0.1f, 12.6f));
        for (int i = -9; i <= 9; i++) b.Box(Sl.Marble, new Vector3(i * 1.15f, 0.8f, 8.27f), new Vector3(0.5f, 0.32f, 0.06f));   // fris gigi tulang
        b.Stairs(Sl.Stone, new Vector3(0f, 0f, 5.7f + 0.31f + 10 * 0.62f), 6.0f, 11, 2.3f / 11f, 0.62f, 0f);
        foreach (float sx in new[] { -10.6f, 10.6f })                                                       // tiang tengkorak di sudut tingkat 1
            foreach (float sz in new[] { -7.4f, 7.4f })
            {
                b.Cyl(Sl.Marble, new Vector3(sx, 1.2f, sz), 0.22f, 1.3f, 8, 0.18f, true);
                b.Ball(Sl.Marble, new Vector3(sx, 2.68f, sz), 0.34f, 4, 8, 1.0f);
            }

        // aula
        b.BoxB(Sl.Stone, new Vector3(0f, 2.3f, -0.8f), new Vector3(13.4f, 5.2f, 8.6f));
        b.Box(Sl.DarkStone, new Vector3(0f, 7.6f, -0.8f), new Vector3(14.2f, 0.3f, 9.4f));
        foreach (float px in new[] { -5f, 0f, 5f }) b.Box(Sl.DarkStone, new Vector3(px, 4.9f, -5.25f), new Vector3(0.6f, 5.2f, 0.3f));
        foreach (float sx in new[] { -6.85f, 6.85f })
            foreach (float pz in new[] { -3.5f, -0.8f, 1.9f }) b.Box(Sl.DarkStone, new Vector3(sx, 4.9f, pz), new Vector3(0.3f, 5.2f, 0.6f));

        // serambi empat tiang
        foreach (float px in new[] { -5.4f, -2.2f, 2.2f, 5.4f })
        {
            b.Cyl(Sl.DarkStone, new Vector3(px, 2.3f, 4.9f), 0.68f, 0.3f, 10, 0.62f, true);
            b.Cyl(Sl.Marble, new Vector3(px, 2.6f, 4.9f), 0.5f, 4.4f, 10, 0.44f, true);
            b.Cyl(Sl.DarkStone, new Vector3(px, 7.0f, 4.9f), 0.62f, 0.3f, 10, 0.7f, true);
        }
        b.Box(Sl.DarkStone, new Vector3(0f, 7.45f, 4.9f), new Vector3(13.4f, 0.55f, 1.5f));                // arsitrave
        b.Ball(Sl.Marble, new Vector3(0f, 7.45f, 5.68f), 0.3f, 4, 8, 1.0f);                                  // tengkorak di arsitrave
        b.Ball(Sl.Dark, new Vector3(-0.1f, 7.5f, 5.93f), 0.06f, 3, 6);
        b.Ball(Sl.Dark, new Vector3(0.1f, 7.5f, 5.93f), 0.06f, 3, 6);

        // pintu ganda tersegel
        foreach (float sx in new[] { -1.95f, 1.95f }) b.Box(Sl.DarkStone, new Vector3(sx, 4.3f, 3.56f), new Vector3(0.5f, 4.0f, 0.22f));
        b.Box(Sl.DarkStone, new Vector3(0f, 6.55f, 3.56f), new Vector3(4.4f, 0.5f, 0.26f));
        foreach (float sx in new[] { -0.85f, 0.85f })
        {
            b.Box(Sl.Dark, new Vector3(sx, 4.3f, 3.53f), new Vector3(1.66f, 4.0f, 0.1f));
            for (int k = 0; k < 3; k++) b.Box(Sl.Metal, new Vector3(sx, 3.2f + k * 1.1f, 3.6f), new Vector3(1.55f, 0.1f, 0.05f));
        }
        b.PushTRS(new Vector3(0f, 4.3f, 3.6f), Quaternion.Euler(90f, 0f, 0f), 1f);                           // lingkaran segel tegak
        b.Cyl(Sl.FieldB, Vector3.zero, 0.95f, 0.05f, 20, 0.95f, true, true);
        b.Cyl(Sl.Dark, Vector3.zero, 0.72f, 0.07f, 20, 0.72f, true, true);
        b.Cyl(Sl.FieldB, Vector3.zero, 0.12f, 0.09f, 8, 0.12f, true, true);
        b.Pop();

        // atap berundak, puncak, mahkota duri tulang
        b.Box(Sl.DarkStone, new Vector3(0f, 8.05f, -0.4f), new Vector3(15.8f, 0.5f, 11.6f));
        b.Box(Sl.Stone, new Vector3(0f, 8.55f, -0.7f), new Vector3(12.4f, 0.5f, 8.8f));
        b.Box(Sl.DarkStone, new Vector3(0f, 9.05f, -0.8f), new Vector3(9.0f, 0.5f, 6.2f));
        b.Box(Sl.Stone, new Vector3(0f, 9.55f, -0.8f), new Vector3(5.8f, 0.5f, 3.9f));
        b.Cone(Sl.DarkStone, new Vector3(0f, 9.8f, -0.8f), 2.2f, 2.4f, 4);
        for (int i = 0; i < 9; i++)
        {
            float a = i * 40f * Mathf.Deg2Rad;
            b.Cone(Sl.Marble, new Vector3(Mathf.Sin(a) * 1.2f, 10.35f, -0.8f + Mathf.Cos(a) * 1.2f), 0.2f, 1.7f - (i % 2) * 0.5f, 5);
        }
    }
}
