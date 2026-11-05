using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Kota Ashwatch "Benteng": prop kota. Air mancur bundaran + air mancur alun-alun tengah, 12 lapak pasar, papan pengumuman Guild, tiang spanduk, pot bunga,
/// patung, bangku, lampu jalan, pohon peneduh, alang-alang & batu tepi sungai.
/// </summary>
public static partial class TownSceneBuilder
{
    // =====================================================================
    // Alun-alun & prop jalan
    // =====================================================================
    static void T2Banner(Transform g, Vector2 p, float yaw, int clothIdx, int n)
    {
        if (!T2Free(p.x, p.y, 0.9f, 0.1f, true)) return;
        var slots = T2Mix(0, 0, 0, 0, clothIdx, clothIdx + 3);
        var go = PropKit.Place(PropKit.Get("T2_BannerPole", b => T2Kit.BannerPole(b, Sl.Cloth)), g, "TiangSpanduk" + n, T2P(p.x, p.y), yaw, 1f, slots);
        BoxCol(go.transform, "TiangCol", new Vector3(0f, 3.2f, 0f), new Vector3(0.9f, 6.4f, 0.9f), Vector3.zero);
        Reserve(p.x, p.y, 1.2f);
    }

    static float T2YawTo(Vector2 from, Vector2 to) { return Mathf.Atan2(to.x - from.x, to.y - from.y) * Mathf.Rad2Deg; }

    static void T2PlazaProps()
    {
        var g = Group("PropAlunAlun", tProps);

        // ---- air mancur bundaran di alun-alun gerbang (dinamai 'Mancur' agar tidak dianggap kolam renang oleh WaterZone)
        Vector2 fg = T2.FountainGate;
        var fgo = PropKit.Place(PropKit.Get("T2_Fountain", T2Kit.Fountain), g, "MancurGerbang", T2P(fg.x, fg.y, -0.04f), 0f, 1f, baseSlots);
        T2RingCols(fgo.transform, 7.65f, 1.2f, 16);
        Reserve(fg.x, fg.y, 9.6f);
        solidBoxes.Add(new[] { fg.x, fg.y, 17.5f, 17.5f, 0f });

        // ---- air mancur + patung di alun-alun tengah kota atas
        Vector2 pm = T2.PlazaMid;
        var pgo = PropKit.Place(PropKit.Get("T2_PlazaFountain", T2Kit.PlazaFountain), g, "MancurAlunAlun", T2P(pm.x, pm.y, -0.04f), 0f, 1f, baseSlots);
        T2RingCols(pgo.transform, 4.8f, 1.2f, 12);
        Reserve(pm.x, pm.y, 6.6f);
        solidBoxes.Add(new[] { pm.x, pm.y, 11f, 11f, 0f });

        // ---- bangku mengelilingi kedua air mancur, menghadap ke tengah
        var bench = PropKit.Get("T2_Bench", PropKit.Bench);
        int bn = 0;
        for (int k = 0; k < 10; k++)
        {
            float a = (k + 0.5f) * Mathf.PI * 2f / 10f;
            Vector2 bp = fg + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * 16.2f;
            if (!T2Free(bp.x, bp.y, 1.2f, 0.3f, true)) continue;
            var bo = PropKit.Place(bench, g, "Bangku" + (bn++), T2P(bp.x, bp.y), a * Mathf.Rad2Deg + 180f, 1f, baseSlots);
            BoxCol(bo.transform, "BangkuCol", new Vector3(0f, 0.5f, 0f), new Vector3(2.0f, 1.0f, 0.8f), Vector3.zero);
            Reserve(bp.x, bp.y, 1.3f);
        }
        for (int k = 0; k < 6; k++)
        {
            float a = (k + 0.5f) * Mathf.PI * 2f / 6f;
            Vector2 bp = pm + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * 8.8f;
            if (!T2Free(bp.x, bp.y, 1.2f, 0.3f, true)) continue;
            var bo = PropKit.Place(bench, g, "Bangku" + (bn++), T2P(bp.x, bp.y), a * Mathf.Rad2Deg + 180f, 1f, baseSlots);
            BoxCol(bo.transform, "BangkuCol", new Vector3(0f, 0.5f, 0f), new Vector3(2.0f, 1.0f, 0.8f), Vector3.zero);
            Reserve(bp.x, bp.y, 1.3f);
        }

        // ---- dua belas lapak pasar di alun-alun gerbang: baris utara menghadap selatan, baris selatan menghadap utara
        var gs = new GameObject("LapakPasar").transform;
        gs.SetParent(g, false);
        for (int i = 0; i < T2.Stalls.Count; i++)
        {
            Vector2 sp = T2.Stalls[i];
            float yaw = sp.y > fg.y ? 180f : 0f;
            int v = i % 4;
            var proto = PropKit.Get("T2_Stall" + v, b => T2Kit.Stall(b, v));
            var slots = T2Mix(i, 0, i + 2, 0, i * 2 + 1, i * 2 + 3);
            var st = PropKit.Place(proto, gs, "Lapak_" + (i + 1), T2P(sp.x, sp.y), yaw, 1f, slots);
            BoxCol(st.transform, "MejaCol", new Vector3(0f, 0.55f, 0.6f), new Vector3(3.5f, 1.1f, 1.1f), Vector3.zero);
            BoxCol(st.transform, "RakCol", new Vector3(0f, 0.9f, -1.05f), new Vector3(3.3f, 1.8f, 0.4f), Vector3.zero);
            ReserveBox(sp.x, sp.y, 4.4f, 3.4f, yaw, 0.2f);
            // tumpukan barang di samping lapak
            if (i % 3 == 0)
            {
                Vector2 gp = sp + new Vector2(i % 2 == 0 ? 4.4f : -4.4f, 0f);
                if (T2Free(gp.x, gp.y, 1.6f, 0.3f, true))
                {
                    var gb = PropKit.Place(PropKit.Get("T2_Goods" + (i % 2), b => T2Kit.Goods(b, i % 2)), gs, "Barang", T2P(gp.x, gp.y), yaw + 10f * i, 1f, baseSlots);
                    BoxCol(gb.transform, "BarangCol", new Vector3(0.3f, 0.7f, 0f), new Vector3(3.2f, 1.4f, 1.6f), Vector3.zero);
                    Reserve(gp.x, gp.y, 1.8f);
                }
            }
        }

        // ---- papan pengumuman Guild di depan Guild, menghadap selatan ke alun-alun
        {
            Vector2 np = T2.Notice;
            var nb = PropKit.Place(PropKit.Get("T2_Notice", T2Kit.NoticeBoard), g, "PapanPengumuman", T2P(np.x, np.y), 180f, 1f, baseSlots);
            BoxCol(nb.transform, "PapanCol", new Vector3(0f, 1.7f, 0f), new Vector3(4.6f, 3.4f, 1.0f), Vector3.zero);
            BuildKit.Text(nb.transform, "PENGUMUMAN GUILD", new Vector3(0f, 2.98f, 0.2f), 180f, 0.085f, T2Gold);
            ReserveBox(np.x, np.y, 5f, 1.6f, 180f, 0.4f);
            var ip = VPoint<InfoPoint>(g, "InteractPoint_PapanPengumuman", T2P(np.x, np.y - 2.9f), "Baca Papan Pengumuman", 3.4f);
            ip.title = "Papan Pengumuman Guild";
            ip.message = "PENGUMUMAN GUILD PETUALANG ASHWATCH\n\n• Permintaan berburu, pengantaran, dan penyelidikan diperbarui tiap pagi di Papan Misi di dalam Guild.\n• Ujian kenaikan pangkat diadakan di Arena Ujian; daftar lebih dulu pada resepsionis.\n• Dilarang menghunus senjata di dalam tembok kota.\n• Gerbang Kastil hanya dibuka untuk Hunter level " + GateRules.InnerGateMinLevel + " ke atas.\n\n— Kepala Guild Ashwatch";
        }

        // ---- tiang spanduk: jalan raya selatan, sudut alun-alun gerbang, lingkar alun-alun tengah, jalan menuju kastil
        var bg = Group("Spanduk", g);
        int bi = 0;
        float[] southZ = { -304f, -272f };
        foreach (float zz in southZ)
        {
            T2Banner(bg, new Vector2(-9.5f, zz), 90f, 0, bi++);
            T2Banner(bg, new Vector2(9.5f, zz), 270f, 0, bi++);
        }
        foreach (var pz in T2.Plazas)
        {
            if (pz.id == "gerbang")
            {
                foreach (float cx in new[] { -1f, 1f })
                    foreach (float cz in new[] { -1f, 1f })
                    {
                        Vector2 c = T2.ToWorld(pz.x, pz.z, pz.yaw, cx * (pz.w * 0.5f + 1.4f), cz * (pz.d * 0.5f + 1.4f));
                        T2Banner(bg, c, T2YawTo(c, fg), 1, bi++);
                    }
            }
        }
        for (int k = 0; k < 8; k++)
        {
            float a = (k + 0.5f) * Mathf.PI * 0.25f;
            Vector2 c = pm + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * 23.5f;
            T2Banner(bg, c, T2YawTo(c, pm), 4, bi++);
        }
        var kr = T2.RoadOf("kastil");
        if (kr != null)
        {
            float kl = T2.Len(kr.pts);
            for (float d = 22f; d < kl - 14f; d += 30f)
            {
                Vector2 tg; Vector2 c = T2.At(kr.pts, d, out tg);
                Vector2 nrm = new Vector2(tg.y, -tg.x);
                foreach (float sg in new[] { -1f, 1f })
                {
                    Vector2 q = c + nrm * sg * (kr.width * 0.5f + 1.5f);
                    T2Banner(bg, q, T2YawTo(q, c), 4, bi++);
                }
            }
        }

        // ---- pot bunga batu di depan bangunan penting (sisi kanan pintu)
        var pg = Group("PotBunga", g);
        string[] civ = { "guild", "inn", "smithy", "shop", "tavern", "library", "cathedral", "barracks", "stables", "magetower" };
        int pi = 0;
        foreach (var id in civ)
        {
            var l = T2.LotOf(id);
            if (l == null) continue;
            foreach (float sg in new[] { 1f, -1f })
            {
                Vector2 p = T2.ToWorld(l, sg * (l.w * 0.5f - 3.2f), l.d * 0.5f + 1.7f);
                if (!T2Free(p.x, p.y, 1.5f, 0.2f, true)) continue;
                var po = PropKit.Place(PropKit.Get("T2_Planter" + (pi % 3), b => T2Kit.Planter(b, pi % 3)), pg, "PotBunga_" + id, T2P(p.x, p.y), l.yaw, 1f, baseSlots);
                BoxCol(po.transform, "PotCol", new Vector3(0f, 0.45f, 0f), new Vector3(2.5f, 0.9f, 1.1f), Vector3.zero);
                Reserve(p.x, p.y, 1.5f);
                pi++;
                if (id != "guild" && id != "cathedral" && id != "library") break;          // gedung besar: pot di kedua sisi
            }
        }

        // ---- patung ksatria: dua di tiap parvis (katedral, akademi) menghadap bangunan, dan patung penjaga di sepanjang jalan menuju kastil
        var plinths = new List<float[]>();
        foreach (var id in new[] { "cathedral", "library" })
        {
            var lm = T2.LotOf(id);
            if (lm == null) continue;
            foreach (float sg in new[] { -1f, 1f })
            {
                Vector2 q = T2.ToWorld(lm, sg * 10f, lm.d * 0.5f + 11f);
                plinths.Add(new[] { q.x, q.y, lm.yaw + 180f, 1.3f });
            }
        }
        if (kr != null)
        {
            float kl = T2.Len(kr.pts);
            for (float d = kl - 24f; d > 40f && plinths.Count < 10; d -= 34f)
            {
                Vector2 tg; Vector2 c = T2.At(kr.pts, d, out tg);
                Vector2 nrm = new Vector2(tg.y, -tg.x);
                foreach (float sg in new[] { -1f, 1f })
                {
                    Vector2 q = c + nrm * sg * (kr.width * 0.5f + 3.3f);
                    plinths.Add(new[] { q.x, q.y, T2YawTo(q, c), 1.0f });
                }
            }
        }
        foreach (var pp in plinths)
        {
            if (!T2Free(pp[0], pp[1], 2.2f, 0.4f, true)) continue;
            var pl = PropKit.Place(PropKit.Get("T2_Plinth", b => T2Kit.StatuePlinth(b, 0f)), g, "PatungKsatria", T2P(pp[0], pp[1]), pp[2], pp[3], baseSlots);
            BoxCol(pl.transform, "PlinthCol", new Vector3(0f, 1.3f, 0f), new Vector3(2.4f, 2.6f, 2.4f), Vector3.zero);
            Reserve(pp[0], pp[1], 2.4f);
        }
    }

    // =====================================================================
    // Lampu jalan: dua sisi jalan lebar, selang-seling di jalan sempit; setiap lampu keempat menyala (cahaya titik)
    // =====================================================================
    static void T2Lamps()
    {
        int n = 0, placed = 0;
        var proto = PropKit.Get("T2_Lamp", T2Kit.Lamp);
        var g = GetLampGroup();
        for (int ri = 0; ri < T2.Roads.Count; ri++)
        {
            var rd = T2.Roads[ri];
            if (!rd.lamps || rd.dead) continue;
            bool wide = rd.width >= 8.5f;
            float spacing = wide ? 25f : 28f;
            float total = T2.Len(rd.pts);
            int sides = wide ? 2 : 1;
            for (int sd = 0; sd < sides; sd++)
            {
                float sgn = sd == 0 ? 1f : -1f;
                for (float d = spacing * (sd == 0 ? 0.5f : 1f) ; d < total - 5f; d += spacing)
                {
                    Vector2 tg; Vector2 c = T2.At(rd.pts, d, out tg);
                    Vector2 nrm = new Vector2(tg.y, -tg.x);
                    Vector2 p = c + nrm * sgn * (rd.width * 0.5f + 0.9f);
                    if (T2OnBridge(p) || T2.RiverBank(p.x, p.y) > 0.02f) continue;
                    if (!IsFree(p.x, p.y, 0.6f)) continue;
                    bool pad = false;
                    foreach (var l in T2.Lots) if ((l.kind == "gatepad" || l.kind == "bridgepad") && T2.DistToRect(p.x, p.y, l) < 1.2f) { pad = true; break; }
                    if (pad) continue;
                    bool onOther = false;
                    for (int rj = 0; rj < T2.Roads.Count; rj++)
                    {
                        if (rj == ri || T2.Roads[rj].dead) continue;
                        var bx = t2RoadBox[rj];
                        if (p.x < bx[0] || p.x > bx[1] || p.y < bx[2] || p.y > bx[3]) continue;
                        if (T2.DistPoly(p.x, p.y, T2.Roads[rj].pts) < T2.Roads[rj].width * 0.5f + 0.3f) { onOther = true; break; }
                    }
                    if (onOther) continue;
                    float yaw = Mathf.Atan2(nrm.y * sgn, -nrm.x * sgn) * Mathf.Rad2Deg;
                    var go = PropKit.Place(proto, g, "Lampu", T2P(p.x, p.y), yaw, 1f, baseSlots);
                    Reserve(p.x, p.y, 0.7f);
                    BoxCol(go.transform, "TiangCol", new Vector3(0f, 1.6f, 0f), new Vector3(0.5f, 3.2f, 0.5f), Vector3.zero);
                    if ((n++ % 4) == 0) AddLight(go.transform, new Vector3(0.85f, 3.3f, 0f), 16f, 1.6f);
                    placed++;
                }
            }
        }
        Debug.Log("[Kota T2] Lampu jalan: " + placed);
    }

    // =====================================================================
    // Pohon peneduh: deretan di jalan raya + sebaran di ruang kosong
    // =====================================================================
    static void T2StreetTrees()
    {
        var g = Group("PohonKota", tProps);
        int placed = 0;
        foreach (var rd in T2.Roads)
        {
            if (rd.id != "raya" && rd.id != "dermaga" && rd.id != "katedral" && rd.id != "akademi") continue;
            float total = T2.Len(rd.pts), spacing = rd.id == "raya" ? 17f : 21f;
            int k = 0;
            for (float d = 10f; d < total - 8f; d += spacing)
            {
                Vector2 tg; Vector2 c = T2.At(rd.pts, d, out tg);
                Vector2 nrm = new Vector2(tg.y, -tg.x);
                float sgn = (k++ % 2 == 0) ? 1f : -1f;
                Vector2 p = c + nrm * sgn * (rd.width * 0.5f + 3.6f);
                if (!T2Free(p.x, p.y, 2.4f, 0.8f)) continue;
                bool upper = p.y > -40f;
                int kind = upper ? ((k % 3 == 0) ? 3 : 2) : ((k % 3 == 0) ? 5 : 0);
                var go = PropKit.Place(PropKit.TreeProto(kind), g, "PohonJalan", T2P(p.x, p.y), k * 71f, 0.95f + 0.12f * (k % 3), baseSlots);
                T2Trunk(go, 3f, 0.42f);
                Reserve(p.x, p.y, 2.4f);
                placed++;
            }
        }
        // ruang kosong lain di ladang, kota tengah, dan bukit mahkota (bukan di tembok / parit)
        var rng = new System.Random(5150);
        int extra = 0;
        for (int i = 0; i < 5000 && extra < 80; i++)
        {
            float x = (float)(rng.NextDouble() * 2 - 1) * 318f, z = -322f + (float)rng.NextDouble() * 622f;
            if (T2.SignedDist(x, z, T2.OuterPoly) > -9f) continue;
            if (T2.InWallBand(x, z, 5f, 3f)) continue;                                      // bukan di atas tembok atau paritnya
            if (T2NearMakam(x, z, 3.6f)) continue;                                          // bukan di dalam / menempel dinding pemakaman
            if (!T2Free(x, z, 3.2f, 1.6f)) continue;
            bool upper = z > -40f;
            int kind = upper ? (rng.Next(4) == 0 ? 3 : (rng.Next(3) == 0 ? 2 : 0)) : (rng.Next(5) == 0 ? 5 : 0);
            var go = PropKit.Place(PropKit.TreeProto(kind), g, "Pohon", T2P(x, z), (float)rng.NextDouble() * 360f, 0.85f + (float)rng.NextDouble() * 0.3f, baseSlots);
            T2Trunk(go, 3f, 0.42f);
            Reserve(x, z, 3.2f);
            extra++;
        }
        Debug.Log("[Kota T2] Pohon kota: " + (placed + extra));
    }

    // =====================================================================
    // Tepi sungai: gerumbul alang-alang & batu di lereng tepi (di atas permukaan air)
    // =====================================================================
    static void T2RiverBanks()
    {
        var g = Group("TepiSungai", tProps);
        var rng = new System.Random(3131);
        var reedP = new[]
        {
            PropKit.Get("T2_Reeds1", b => ForestKit.Reeds(b, 1)),
            PropKit.Get("T2_Reeds2", b => ForestKit.Reeds(b, 2)),
            PropKit.Get("T2_Cattail1", b => ForestKit.Cattail(b, 2)),
        };
        var rockP = new[]
        {
            PropKit.Get("T2_RiverRock0", b => ForestKit.Boulder(b, 61, 1f, true)),
            PropKit.Get("T2_RiverRock1", b => ForestKit.Boulder(b, 62, 1f, false)),
        };
        float total = T2.Len(T2.River);
        int placed = 0;
        for (float d = 3f; d < total - 3f; d += 3.6f)
        {
            Vector2 tg; Vector2 c = T2.At(T2.River, d, out tg);
            Vector2 nrm = new Vector2(tg.y, -tg.x);
            float hw = T2.RiverHalfWidth(c.x, c.y);
            foreach (float sg in new[] { -1f, 1f })
            {
                float off = hw * 0.62f + 1.4f + (float)rng.NextDouble() * 4.2f;
                Vector2 p = c + nrm * sg * off;
                if (Mathf.Abs(p.x) > 500f || Mathf.Abs(p.y) > 500f) continue;
                float gy = T2G(p.x, p.y);
                if (gy < T2.WaterLevel + 0.18f || gy > 1.1f) continue;
                if (T2OnBridge(p) || T2NearRoad(p.x, p.y, 0.5f)) continue;
                if (!IsFree(p.x, p.y, 0.8f)) continue;
                if (rng.NextDouble() < 0.22)
                {
                    float s = 0.6f + (float)rng.NextDouble() * 0.9f;
                    PropKit.Place(rockP[rng.Next(2)], g, "BatuSungai", new Vector3(p.x, gy - 0.12f * s, p.y), (float)rng.NextDouble() * 360f, s, ForestKit.Slots);
                }
                else
                {
                    PropKit.Place(reedP[rng.Next(reedP.Length)], g, "Alang", new Vector3(p.x, gy - 0.05f, p.y), (float)rng.NextDouble() * 360f, 0.9f + (float)rng.NextDouble() * 0.8f, ForestKit.Slots);
                }
                placed++;
            }
        }
        Debug.Log("[Kota T2] Tepi sungai: " + placed + " gerumbul");
    }
}
