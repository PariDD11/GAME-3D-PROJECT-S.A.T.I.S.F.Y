using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Kota Ashwatch "Konsentris": benda lore kanon (bagian 5 Lore Bible) yang dibangun oleh builder, bukan oleh WorldStory.json:
/// papan hitam "DILARANG MENGUCAPKAN YANG TAK DISEBUT" di sudut jalan dan di tiap gerbang, lonceng berlidah terikat tali merah + tiang goresan 3+3 di tiap gerbang,
/// serta Pemakaman Kota di bukit belakang Katedral (nisan polos tanpa nama, garam di ambang gerbang besi).
/// (Sumur bertutup papan ada di T2FarmExtras; pertanda fs_* dan benda lore lama dipasang runtime dari WorldStory.json di Plaza Gerbang.)
/// </summary>
public static partial class TownSceneBuilder
{
    const string T2BoardText = "DILARANG MENGUCAPKAN\nYANG TAK DISEBUT";
    static readonly Color T2Chalk = new Color(0.93f, 0.91f, 0.84f);
    static int t2Boards;

    // =====================================================================
    // Papan hitam
    // =====================================================================
    /// <summary>Satu papan hitam di titik p, muka depan menghadap yaw. Huruf putih di kedua sisi.</summary>
    static bool T2WarnBoard(Transform g, Vector2 p, float yaw)
    {
        if (!IsFree(p.x, p.y, 1.5f)) return false;
        var go = PropKit.Place(PropKit.Get("T2_WarnBoard", T2Kit.WarnBoard), g, "PapanHitam" + (t2Boards + 1), T2P(p.x, p.y), yaw, 1f, baseSlots);
        BoxCol(go.transform, "PapanCol", new Vector3(0f, 1.3f, 0f), new Vector3(2.9f, 2.6f, 0.5f), Vector3.zero);
        BuildKit.Text(go.transform, T2BoardText, new Vector3(0f, 1.62f, 0.07f), 180f, 0.03f, T2Chalk);
        BuildKit.Text(go.transform, T2BoardText, new Vector3(0f, 1.62f, -0.07f), 0f, 0.03f, T2Chalk);
        ReserveBox(p.x, p.y, 3.0f, 0.8f, yaw, 0.3f);
        t2Boards++;
        return true;
    }

    /// <summary>Papan hitam di sudut simpang: di pangkal gang yang bercabang dari jalan besar, di tepi jalan, menghadap jalan. Jarak antar papan minimal 42 m.</summary>
    static void T2CornerBoards(Transform g)
    {
        var majors = new HashSet<string> { "raya", "timur", "barat", "utara", "lintas", "lingkar_utara" };
        var placed = new List<Vector2>();
        foreach (var sp in T2.Roads)
        {
            if (sp.dead || sp.pts.Count < 3 || !sp.id.StartsWith("sp_")) continue;
            Vector2 p0 = sp.pts[0];
            float best = 1e9f; Vector2 tg = Vector2.up, q0 = p0; T2.Road rd = null;
            foreach (var r in T2.Roads)
            {
                if (r.dead || !majors.Contains(r.id)) continue;
                Vector2 tgg; Vector2 q = T2.NearestOnPoly(p0.x, p0.y, r.pts, out tgg);
                float d = (q - p0).magnitude;
                if (d < best) { best = d; tg = tgg; q0 = q; rd = r; }
            }
            if (rd == null || best > rd.width * 0.5f + 3f) continue;
            bool near = false;
            foreach (var q in placed) if ((q - p0).sqrMagnitude < 42f * 42f) { near = true; break; }
            if (near) continue;
            Vector2 nrm = new Vector2(tg.y, -tg.x);
            Vector2 toSpur = sp.pts[Mathf.Min(3, sp.pts.Count - 1)] - p0;
            float sgn = Vector2.Dot(toSpur, nrm) >= 0f ? 1f : -1f;                               // sisi tempat gang bermuara
            float along = sp.width * 0.5f + 2.6f;
            bool ok = false;
            for (int k = 0; k < 2 && !ok; k++)
            {
                Vector2 p = q0 + nrm * sgn * (rd.width * 0.5f + 1.7f) + tg * (k == 0 ? along : -along);
                if (T2NearRoad(p.x, p.y, 0.8f) || T2.RiverBank(p.x, p.y) > 0.02f || T2InPlaza(p.x, p.y, 0.5f)) continue;
                float yaw = T2YawTo(p, q0);
                ok = T2WarnBoard(g, p, yaw);
                if (ok) placed.Add(p0);
            }
        }
        Debug.Log("[Kota T2] Papan hitam di sudut jalan: " + placed.Count);
    }

    // =====================================================================
    // Gerbang: lonceng terikat, tiang goresan 3+3, papan hitam
    // =====================================================================
    /// <summary>Cari titik bebas di sisi sign (+1 = kiri orang yang masuk, -1 = kanan) di dalam gerbang: makin jauh dari jalan didahulukan, lalu makin masuk ke dalam.</summary>
    static bool T2GateSpot(T2.Gate gt, float sign, float r, out Vector2 spot, out float latUsed, out float inUsed)
    {
        float a = gt.az * Mathf.Deg2Rad;
        Vector2 t = new Vector2(Mathf.Sin(a), Mathf.Cos(a)), n = new Vector2(t.y, -t.x);               // t = arah luar gerbang, n = kanan arah luar
        float[] lats = { gt.tier == 0 ? 8.8f : 8.0f, 6.8f, 5.8f, 5.0f };
        foreach (float lat in lats)
            for (float inD = gt.tier == 0 ? 12f : 9f; inD < 36f; inD += 1.5f)
            {
                Vector2 p = gt.p - t * inD + n * (sign * lat);
                if (!IsFree(p.x, p.y, r)) continue;
                spot = p; latUsed = lat; inUsed = inD;
                return true;
            }
        spot = Vector2.zero; latUsed = 0f; inUsed = 0f;
        return false;
    }

    static void T2GateLore(Transform g)
    {
        var bellP = PropKit.Get("T2_TiedBell", T2Kit.TiedBell);
        var tallyP = PropKit.Get("T2_TallyPost", T2Kit.TallyPost);
        int bells = 0, tallies = 0;
        foreach (var gt in T2.Gates)
        {
            float a = gt.az * Mathf.Deg2Rad;
            Vector2 t = new Vector2(Mathf.Sin(a), Mathf.Cos(a)), n = new Vector2(t.y, -t.x);
            Vector2 pb, pt; float lb, ib, lt, it;
            if (T2GateSpot(gt, -1f, 0.8f, out pb, out lb, out ib) || T2GateSpot(gt, 1f, 0.8f, out pb, out lb, out ib))       // kanan orang yang masuk (atau kiri bila padat): lonceng terikat
            {
                var bell = PropKit.Place(bellP, g, "LoncengTerikat_" + gt.id, T2P(pb.x, pb.y), gt.az, 1f, baseSlots);
                BoxCol(bell.transform, "TiangCol", new Vector3(0f, 1.5f, 0f), new Vector3(0.6f, 3f, 0.6f), Vector3.zero);
                Reserve(pb.x, pb.y, 0.9f);
                bells++;
            }
            if (T2GateSpot(gt, 1f, 0.8f, out pt, out lt, out it) || T2GateSpot(gt, -1f, 0.8f, out pt, out lt, out it))       // kiri (atau kanan): tiang goresan 3+3
            {
                var tp = PropKit.Place(tallyP, g, "TiangGoresan_" + gt.id, T2P(pt.x, pt.y), gt.az, 1f, baseSlots);
                BoxCol(tp.transform, "TiangCol", new Vector3(0f, 0.8f, 0f), new Vector3(0.9f, 1.6f, 0.9f), Vector3.zero);
                Reserve(pt.x, pt.y, 0.8f);
                tallies++;
                for (float bd = it + 4.5f; bd < it + 16f; bd += 2.5f)                                      // papan hitam sedikit lebih ke dalam
                    if (T2WarnBoard(g, gt.p - t * bd + n * (lt + 0.4f), gt.az)) break;
            }
        }
        Debug.Log("[Kota T2] Gerbang: " + bells + " lonceng terikat, " + tallies + " tiang goresan dari " + T2.Gates.Count + " gerbang");
    }

    /// <summary>Titik lore kota: papan hitam, lonceng terikat, tiang goresan.</summary>
    static void T2LoreProps()
    {
        t2Boards = 0;
        var g = Group("LoreKota", tProps);
        T2GateLore(g);
        T2CornerBoards(g);
        Debug.Log("[Kota T2] Papan hitam total: " + t2Boards);
    }

    // =====================================================================
    // Pemakaman Kota (zona "makam", di bukit belakang Katedral)
    // =====================================================================
    static void T2WallLine(GeoBag w, Vector2 a, Vector2 b)
    {
        float len = (b - a).magnitude;
        int n = Mathf.Max(1, Mathf.CeilToInt(len / 3.5f));
        for (int i = 0; i < n; i++)
        {
            Vector2 p = Vector2.Lerp(a, b, i / (float)n), q = Vector2.Lerp(a, b, (i + 1) / (float)n);
            Vector3 pa = new Vector3(p.x, T2G(p.x, p.y) + 0.5f, p.y), pb = new Vector3(q.x, T2G(q.x, q.y) + 0.5f, q.y);
            w.Beam(Sl.Stone, pa, pb, 0.46f, 1.05f);
            w.Beam(Sl.DarkStone, pa + Vector3.up * 0.58f, pb + Vector3.up * 0.58f, 0.62f, 0.1f);
        }
    }

    /// <summary>True bila titik ada di dalam pemakaman atau lebih dekat dari m ke dindingnya (dipakai penebar pohon kota).</summary>
    static bool T2NearMakam(float x, float z, float m)
    {
        foreach (var zn in T2.Zones)
            if (zn.kind == "makam" && (T2.Inside(x, z, zn.poly) || T2.SignedDist(x, z, zn.poly) < m)) return true;
        return false;
    }

    static void T2Cemetery()
    {
        T2.Zone zone = null;
        foreach (var z in T2.Zones) if (z.kind == "makam") zone = z;
        if (zone == null) return;
        var g = Group("PemakamanKota", tProps);
        var poly = zone.poly;
        float minX = 1e9f, maxX = -1e9f, minZ = 1e9f, maxZ = -1e9f;
        foreach (var q in poly) { minX = Mathf.Min(minX, q.x); maxX = Mathf.Max(maxX, q.x); minZ = Mathf.Min(minZ, q.y); maxZ = Mathf.Max(maxZ, q.y); }
        float gx = (minX + maxX) * 0.5f, gz = minZ;                                                      // gerbang di sisi selatan (tengah, atau di ujung gang yang berakhir di situ)
        float bestGap = 4.2f;
        foreach (var rd in T2.Roads)
        {
            if (rd.dead || rd.pts.Count < 2) continue;
            foreach (var e in new[] { rd.pts[0], rd.pts[rd.pts.Count - 1] })
            {
                float dz = Mathf.Abs(e.y - gz);
                if (dz < bestGap && e.x > minX + 6f && e.x < maxX - 6f) { bestGap = dz; gx = e.x; }
            }
        }

        // ---- dinding rendah keliling, celah 6,6 m di gerbang selatan
        var wall = new GeoBag();
        for (int i = 0; i < poly.Count; i++)
        {
            Vector2 a = poly[i], b = poly[(i + 1) % poly.Count];
            if (Mathf.Abs(a.y - gz) < 0.01f && Mathf.Abs(b.y - gz) < 0.01f)
            {
                Vector2 l = a.x < b.x ? a : b, r = a.x < b.x ? b : a;
                T2WallLine(wall, l, new Vector2(gx - 3.3f, gz));
                T2WallLine(wall, new Vector2(gx + 3.3f, gz), r);
            }
            else T2WallLine(wall, a, b);
        }
        // ---- gerbang besi tempa + dua tiang batu
        wall.Push(T2P(gx, gz), 0f);
        CemeteryArt.GatePier(wall, -3.3f, 0f);
        CemeteryArt.GatePier(wall, 3.3f, 0f);
        CemeteryArt.IronGate(wall, 0f, 0f);
        wall.Pop();
        // garam dua jari di ambang gerbang, tak putus
        float gy = T2G(gx, gz) + 0.04f;
        wall.Ribbon(Sl.Marble, new List<Vector2> { new Vector2(gx - 3.3f, gz - 0.9f), new Vector2(gx + 3.3f, gz - 0.9f) }, 0.12f, gy);
        wall.Emit(g, "DindingPemakaman", baseSlots, MeshDir + "/T2_Cemetery_Wall.asset");

        // ---- nisan polos tanpa nama, tiga baris menghadap selatan (ke gerbang), lorong tengah dari gerbang ke utara
        var graves = new GeoBag();
        int count = 0;
        float[] rowZ = { gz + 3.2f, gz + 7.4f, gz + 11.6f };
        for (int r = 0; r < rowZ.Length; r++)
        {
            for (float x = minX + 3f; x <= maxX - 2f; x += 3.3f)
            {
                float jx = x + ((count * 37) % 7 - 3) * 0.09f, jz = rowZ[r] + ((count * 53) % 5 - 2) * 0.1f;
                if (Mathf.Abs(jx - gx) < 2.4f) continue;
                if (!T2.Inside(jx, jz, poly) || T2.SignedDist(jx, jz, poly) > -1.8f) continue;
                if (!T2Free(jx, jz, 1.1f, 0.2f, true)) continue;
                CemeteryArt.Grave(graves, T2P(jx, jz), 180f + ((count * 11) % 9 - 4) * 1.5f, (count * 5 + r * 3) % 7, count * 7 + r);
                Reserve(jx, jz, 1.2f);
                count++;
            }
        }
        graves.Emit(g, "Makam", baseSlots, MeshDir + "/T2_Cemetery_Graves.asset");

        // ---- cemara hitam di tepi utara
        int trees = 0;
        for (float x = minX + 5f; x <= maxX - 3f; x += 7.2f)
        {
            float z = maxZ;                                                                              // sisi utara miring (sejajar jalan peronda): turun dari atas sampai 1,3 m di dalam dinding
            while (z > minZ && (!T2.Inside(x, z, poly) || T2.SignedDist(x, z, poly) > -1.3f)) z -= 0.25f;
            if (z <= minZ || !T2Free(x, z, 1.2f, 0.3f, true)) continue;
            var go = PropKit.Place(PropKit.TreeProto(3), g, "Cemara", T2P(x, z), x * 31f, 1.05f + 0.1f * (trees % 3), baseSlots);
            T2Trunk(go, 3f, 0.45f);
            Reserve(x, z, 1.5f);
            trees++;
        }

        // ---- papan keterangan di gerbang
        var ip = VPoint<InfoPoint>(g, "InteractPoint_Pemakaman", T2P(gx, gz - 2.6f), "Periksa Pemakaman", 3.6f);
        ip.title = "Pemakaman Ashwatch";
        ip.message = "Pemakaman kota di bukit belakang Katedral. Nisan-nisannya polos, tak satu pun memuat nama. Di ambang gerbang besi ditaburkan garam, dua jari lebar, tanpa putus.\n\nDi sini orang mengubur yang mati dan tidak menyebut mereka.";
        Debug.Log("[Kota T2] Pemakaman Kota: " + count + " makam, " + trees + " cemara");
    }
}
