using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tiga tempat kecil kanon Gurun (§8.4):
///  - Tugu Gerbang Pagi di mulut jalan dari kota (matahari terbit, garis garam dua jari tak putus);
///  - kemah di dasar sungai kering (Wadi Kering): dasar berbatu bulat, tebing tanah merah, kayu hanyut, cincin api unggun dingin ("malam di gurun sedingin hukuman");
///  - surat yang tak terkirim di bawah batu pipih di ujung Ngarai Merah.
/// Semua dalam koordinat dunia, tanpa UnityEngine.Random, tanpa mengubah terrain.
/// </summary>
public static partial class HuntSceneBuilder
{
    /// <summary>Titik-titik halus (Catmull-Rom) di sepanjang polyline desain.</summary>
    static List<Vector2> DcSpline(Vector2[] p, int per)
    {
        var o = new List<Vector2>();
        for (int i = 0; i + 1 < p.Length; i++)
        {
            Vector2 p0 = p[Mathf.Max(0, i - 1)], p1 = p[i], p2 = p[i + 1], p3 = p[Mathf.Min(p.Length - 1, i + 2)];
            for (int k = 0; k < per; k++)
            {
                float t = k / (float)per, t2 = t * t, t3 = t2 * t;
                o.Add(0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
            }
        }
        o.Add(p[p.Length - 1]);
        return o;
    }

    /// <summary>Pita yang mengikuti tanah di sepanjang titik desain pts; lebar hw di kiri-kanan, diangkat lift dari tanah.</summary>
    static void DcGroundStrip(GeoBag b, int slot, List<Vector2> pts, float hw, float lift)
    {
        Vector3 pl = Vector3.zero, pr = Vector3.zero; bool has = false;
        for (int i = 0; i < pts.Count; i++)
        {
            Vector2 a = pts[Mathf.Max(0, i - 1)], c = pts[Mathf.Min(pts.Count - 1, i + 1)];
            Vector2 dir = (c - a).normalized, nrm = new Vector2(-dir.y, dir.x);
            float h = hw * (0.85f + 0.3f * Mathf.Abs(Mathf.Sin(i * 0.9f)));
            Vector3 l = G(pts[i].x + nrm.x * h, pts[i].y + nrm.y * h), r = G(pts[i].x - nrm.x * h, pts[i].y - nrm.y * h);
            l.y += lift; r.y += lift;
            if (has) DcQ(b, slot, pl, pr, r, l, Vector3.up);
            pl = l; pr = r; has = true;
        }
    }

    // ================================================================== Tugu Gerbang Pagi
    static void DcGateStele(Transform root)
    {
        var g = new GameObject("TuguGerbangPagi").transform;
        g.SetParent(root, false);
        Vector3 p = G(7.5f, -388f);
        Vector3 toRoad = G(0f, -388f) - p; toRoad.y = 0f;
        float yaw = DcYawOf(toRoad);
        var b = new GeoBag();
        b.Push(new Vector3(p.x, p.y, p.z), yaw);
        b.BoxB(Sl.Stone, new Vector3(0f, -0.3f, 0f), new Vector3(1.5f, 0.6f, 1.1f));
        b.BoxB(Sl.Marble, new Vector3(0f, 0.3f, 0f), new Vector3(0.95f, 2.3f, 0.55f));
        b.Box(Sl.Stone, new Vector3(0f, 2.68f, 0f), new Vector3(1.15f, 0.16f, 0.75f));
        b.Cyl(Sl.Stone, new Vector3(0f, 2.76f, 0f), 0.5f, 0.45f, 4, 0.12f, true, false);
        // matahari terbit: cakram dan dua belas sinar pendek di muka batu
        Vector3 c = new Vector3(0f, 1.85f, 0.29f);
        b.PushTRS(c, Quaternion.Euler(90f, 0f, 0f), 1f);
        b.Cyl(Sl.DarkStone, Vector3.zero, 0.3f, 0.03f, 20, 0.3f, true, false);
        b.Pop();
        for (int k = 0; k < 12; k++)
        {
            float a = k * 30f * Mathf.Deg2Rad;
            Vector3 d = new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f);
            b.Beam(Sl.DarkStone, c + d * 0.38f, c + d * (k % 2 == 0 ? 0.6f : 0.52f), 0.05f, 0.03f, Vector3.forward);
        }
        b.Beam(Sl.DarkStone, new Vector3(-0.4f, 1.55f, 0.28f), new Vector3(0.4f, 1.55f, 0.28f), 0.045f, 0.03f, Vector3.forward);     // garis cakrawala
        b.Pop();
        // garis garam dua jari, tak putus, di depan batu
        Vector3 e0 = p + Quaternion.Euler(0f, yaw, 0f) * new Vector3(-1.1f, 0f, 1.0f), e1 = p + Quaternion.Euler(0f, yaw, 0f) * new Vector3(1.1f, 0f, 1.0f);
        Vector3 sd = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0f, 0.035f);
        Vector3 a0 = e0 - sd, a1 = e0 + sd, b0 = e1 + sd, b1 = e1 - sd;
        a0.y = GroundYW(a0.x, a0.z) + 0.05f; a1.y = GroundYW(a1.x, a1.z) + 0.05f; b0.y = GroundYW(b0.x, b0.z) + 0.05f; b1.y = GroundYW(b1.x, b1.z) + 0.05f;
        DcQ(b, Sl.Marble, a0, a1, b0, b1, Vector3.up);
        var go = b.Emit(g, "Tugu", ForestKit.DSlots);
        // collider di objek anak yang diputar sesuai arah tugu (BoxCollider selalu sejajar sumbu objeknya)
        var colGo = new GameObject("TuguCollider");
        colGo.transform.SetParent(go.transform, false);
        colGo.transform.position = p + new Vector3(0f, 1.4f, 0f);
        colGo.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        var col = colGo.AddComponent<BoxCollider>();
        col.size = new Vector3(1.2f, 2.8f, 1.0f);
        DcInfo(g, "InteractPoint_TuguGerbangPagi", Gw(p.x, p.z) + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0f, 1.8f), "Periksa batu jalan", 4.5f, "Batu Jalan Gerbang Pagi",
            "Batu jalan ini menandai tempat kafilah meninggalkan kota. Di mukanya terpahat lingkaran dengan garis-garis pendek di tepinya: matahari yang baru terbit. Orang kota menamai gerbang timur mereka Gerbang Pagi, sebab dari ambangnya matahari pertama terlihat memanjat tembok.\n\n" +
            "Di kaki batu ada garis garam setebal dua jari, tak putus. Kafilah selalu menambahnya sebelum berangkat ke Gurun Zahar.");
    }

    // ================================================================== kemah di dasar sungai kering
    static readonly Vector2[] DcWadiPath =
    {
        new Vector2(-372f, -168f), new Vector2(-340f, -190f), new Vector2(-312f, -206f), new Vector2(-290f, -214f),
        new Vector2(-268f, -226f), new Vector2(-246f, -246f), new Vector2(-218f, -262f),
    };

    static void DcWadiCamp(Transform root)
    {
        var g = new GameObject("DasarSungaiKering").transform;
        g.SetParent(root, false);
        var line = DcSpline(DcWadiPath, 6);
        Vector2 camp = new Vector2(-288f, -203f);                                   // di tepi dalam kelokan, terlindung dinding tanah
        DcClear(G(camp.x, camp.y), 7f);
        var b = new GeoBag();
        DcGroundStrip(b, ForestKit.Rock, line, 4.2f, 0.05f);                        // dasar sungai berbatu
        for (int i = 0; i < line.Count; i++)
        {
            Vector2 a = line[Mathf.Max(0, i - 1)], c = line[Mathf.Min(line.Count - 1, i + 1)];
            Vector2 dir = (c - a).normalized, nrm = new Vector2(-dir.y, dir.x);
            for (int s = -1; s <= 1; s += 2)
            {
                if (i % 2 == (s > 0 ? 0 : 1) || i % 7 == 3) continue;               // tebing tanah merah berselang-seling, tidak rapat
                float off = 6.6f + DcHash(i, s + 5) * 1.6f;
                Vector2 q = line[i] + nrm * (s * off);
                if ((q - camp).magnitude < 5f) continue;
                Vector3 pw = G(q.x, q.y);
                ForestKit.Blob(b, ForestKit.Rock, new Vector3(pw.x, pw.y + 0.3f, pw.z), new Vector3(2.8f + DcHash(i, 41) * 1.2f, 0.9f + DcHash(i, 43) * 0.7f, 2.0f), i * 3 + s + 7, 0.3f, 4, 9, 0.6f);
            }
            // batu bulat di dasar
            for (int k = 0; k < 2; k++)
            {
                Vector2 q = line[i] + nrm * ((DcHash(i, 51 + k) - 0.5f) * 6.4f);
                if ((q - camp).magnitude < 3.2f) continue;
                Vector3 pw = G(q.x, q.y);
                float r = 0.15f + 0.3f * DcHash(i, 61 + k);
                ForestKit.Blob(b, ForestKit.Rock, new Vector3(pw.x, pw.y + r * 0.3f, pw.z), new Vector3(r, r * 0.65f, r * 0.85f), i * 5 + k, 0.25f, 3, 7, 0.4f);
            }
        }
        // kayu hanyut yang terdampar
        for (int k = 0; k < 4; k++)
        {
            int i = 3 + k * 6;
            if (i >= line.Count - 1) break;
            Vector2 q = line[i] + new Vector2(-(line[i + 1] - line[i]).y, (line[i + 1] - line[i]).x).normalized * ((k % 2 == 0 ? 1f : -1f) * 1.6f);
            float yw = (DcHash(k, 71) * 160f - 30f) * Mathf.Deg2Rad, len = 2.4f + 2.2f * DcHash(k, 73);
            Vector3 p0 = G(q.x, q.y), p1 = new Vector3(p0.x + Mathf.Sin(yw) * len, 0f, p0.z + Mathf.Cos(yw) * len);
            p1.y = GroundYW(p1.x, p1.z);
            float th = 0.16f + 0.1f * DcHash(k, 77);
            b.Beam(ForestKit.BarkDead, p0 + new Vector3(0f, th * 0.5f, 0f), p1 + new Vector3(0f, th * 0.8f + 0.1f, 0f), th, th);
            b.Beam(ForestKit.BarkDead, Vector3.Lerp(p0, p1, 0.4f) + new Vector3(0f, th, 0f), Vector3.Lerp(p0, p1, 0.55f) + new Vector3(Mathf.Cos(yw) * 0.5f, th + 0.5f, -Mathf.Sin(yw) * 0.5f), th * 0.5f, th * 0.5f);
        }
        // semak kering di tepian
        for (int k = 0; k < 6; k++)
        {
            Vector2 q = line[2 + k * 3] + new Vector2(DcHash(k, 81) * 8f - 4f, DcHash(k, 83) * 8f - 4f) + new Vector2(0f, 5.5f);
            if ((q - camp).magnitude < 4f) continue;
            Vector3 pw = G(q.x, q.y);
            b.PushTRS(pw, Quaternion.Euler(0f, DcHash(k, 85) * 360f, 0f), 1f);
            ForestKit.DryShrub(b, 4 + k, 0.9f);
            b.Pop();
        }
        // cincin api unggun yang sudah dingin
        Vector3 cw = G(camp.x, camp.y);
        for (int k = 0; k < 9; k++)
        {
            float a = k / 9f * Mathf.PI * 2f;
            Vector3 sp = Gw(cw.x + Mathf.Sin(a) * 0.72f, cw.z + Mathf.Cos(a) * 0.72f);
            ForestKit.Blob(b, ForestKit.Rock, new Vector3(sp.x, sp.y + 0.1f, sp.z), new Vector3(0.2f, 0.16f, 0.17f), 90 + k, 0.25f, 3, 7, 0.4f);
        }
        b.Disc(ForestKit.Ash, new Vector3(cw.x, 0f, cw.z), 0.55f, 14, cw.y + 0.045f);
        for (int k = 0; k < 4; k++)
        {
            float a = (k * 90f + 20f) * Mathf.Deg2Rad;
            b.Beam(ForestKit.Ash, new Vector3(cw.x, cw.y + 0.09f, cw.z), new Vector3(cw.x + Mathf.Sin(a) * 0.48f, cw.y + 0.07f, cw.z + Mathf.Cos(a) * 0.48f), 0.07f, 0.06f);
        }
        // tumpukan kayu bakar di sisi dinding
        Vector3 wp = Gw(cw.x - 1.6f, cw.z + 1.1f);
        for (int k = 0; k < 6; k++)
            b.Beam(ForestKit.BarkDead, new Vector3(wp.x - 0.7f, wp.y + 0.1f + (k / 3) * 0.14f, wp.z + (k % 3) * 0.17f), new Vector3(wp.x + 0.75f, wp.y + 0.12f + (k / 3) * 0.14f, wp.z + (k % 3) * 0.17f + 0.04f), 0.1f, 0.1f);
        b.Emit(g, "DasarSungai", ForestKit.DSlots);
        DcInfo(g, "InteractPoint_DasarSungaiKering", Gw(cw.x + 0.3f, cw.z - 1.2f), "Periksa bekas kemah", 5.0f, "Dasar Sungai Kering",
            "Dulu air mengalir di sini; kini hanya tersisa dinding tanah merah dan batu-batu bulat yang dulu digulirkannya. Para kafilah berkemah di dasarnya karena terlindung dari angin, memungut semak kering dan kayu hanyut yang terdampar di tepi. Abu di cincin batu itu sudah dingin.\n\n" +
            "Siang di gurun menindih tengkuk. Begitu matahari turun, panasnya lenyap sekaligus, seperti selimut yang ditarik, dan pasir berubah menjadi hamparan dingin yang berkerak di bawah bintang. Malam di gurun sedingin hukuman yang dijatuhkan untuk siangnya. Bawalah garam dan kayu bakar.");
    }

    // ================================================================== surat yang tak terkirim di Ngarai Merah
    static void DcLetter(Transform root)
    {
        var g = new GameObject("SuratNgarai").transform;
        g.SetParent(root, false);
        Vector3 c = G(270f, 220f);
        float g0 = c.y, best = -1e9f; int bd = 0;
        for (int d = 0; d < 8; d++)                                                  // arah dinding: tanah paling tinggi di sekitar ujung ngarai
        {
            float a = d * 45f * Mathf.Deg2Rad;
            float y = GroundYW(c.x + Mathf.Sin(a) * 9f, c.z + Mathf.Cos(a) * 9f);
            if (y > best) { best = y; bd = d; }
        }
        float ad = bd * 45f * Mathf.Deg2Rad;
        float rr = 2f;
        for (; rr < 12f; rr += 0.5f)
            if (GroundYW(c.x + Mathf.Sin(ad) * rr, c.z + Mathf.Cos(ad) * rr) > g0 + 0.9f) break;
        rr = Mathf.Max(2.5f, rr - 0.9f);
        Vector3 p = Gw(c.x + Mathf.Sin(ad) * rr, c.z + Mathf.Cos(ad) * rr);
        float yaw = ad * Mathf.Rad2Deg;
        var b = new GeoBag();
        b.Push(p, yaw);
        b.Box(Sl.Marble, new Vector3(0.1f, 0.02f, 0.2f), new Vector3(0.22f, 0.012f, 0.15f), 14f);                       // kertas terlipat
        b.Box(Sl.Marble, new Vector3(0.16f, 0.028f, 0.2f), new Vector3(0.2f, 0.012f, 0.13f), 35f);
        ForestKit.Blob(b, ForestKit.Rock, new Vector3(-0.08f, 0.1f, -0.02f), new Vector3(0.42f, 0.1f, 0.32f), 17, 0.2f, 3, 8, 0.7f);   // batu pipih menindihnya
        ForestKit.Blob(b, ForestKit.Rock, new Vector3(-0.45f, 0.06f, 0.25f), new Vector3(0.14f, 0.08f, 0.12f), 18, 0.2f, 3, 7, 0.5f);
        b.Pop();
        b.Emit(g, "KertasDanBatu", ForestKit.DSlots);
        DcInfo(g, "InteractPoint_SuratNgarai", p, "Periksa kertas terlipat", 3.2f, "Surat yang Tak Terkirim",
            "Di kaki dinding yang berlapis merah dan jingga, berlubang oleh angin selama ribuan tahun, terselip kertas terlipat kecil di bawah sebuah batu pipih. Tintanya memudar, tetapi masih terbaca:\n\n" +
            "\"Aku merindukanmu seperti orang merindukan roti panas di hari hujan.\"\n\n" +
            "Biasa saja. Tidak puitis. Hanya sangat banyak. Kertas ini tak pernah dikirim ke mana pun.");
    }
}
