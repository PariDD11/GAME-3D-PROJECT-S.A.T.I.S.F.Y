using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Kota Ashwatch "Benteng": area terbuka. Ladang + kebun apel + ternak diam di luar tembok (tenggara/barat daya), taman kota di dalam tembok (sakura, timur).
/// Alat bantu ruang bebas (T2Free) memakai grid Reserve milik builder + jalan/alun-alun/sungai dari denah.
/// </summary>
public static partial class TownSceneBuilder
{
    static List<T2.Lot> t2PadLots;

    static void T2ResetAreas()
    {
        t2PadLots = null;
        t2LampGroup = null;
    }

    // =====================================================================
    // Ruang bebas
    // =====================================================================
    /// <summary>Titik (x,z) berada dekat jalan mana pun (kota, jalan keluar gerbang, jembatan) dalam jarak margin dari tepi jalan.</summary>
    static bool T2NearRoad(float x, float z, float margin)
    {
        for (int i = 0; i < T2.Roads.Count; i++)
        {
            var r = T2.Roads[i];
            if (r.dead) continue;
            var bx = t2RoadBox[i];
            float m = Mathf.Max(0f, margin - 6f);
            if (x < bx[0] - m || x > bx[1] + m || z < bx[2] - m || z > bx[3] + m) continue;
            if (T2.DistPoly(x, z, r.pts) < r.width * 0.5f + margin) return true;
        }
        for (int i = 0; i < T2.OutRoads.Count; i++)
        {
            var bx = t2OutBox[i];
            float m = Mathf.Max(0f, margin - 14f);
            if (x < bx[0] - m || x > bx[1] + m || z < bx[2] - m || z > bx[3] + m) continue;
            if (T2.DistPoly(x, z, T2.OutRoads[i]) < 4.5f + margin) return true;
        }
        return T2OnBridge(new Vector2(x, z));
    }

    static bool T2InPlaza(float x, float z, float margin)
    {
        for (int i = 0; i < T2.Plazas.Count; i++)
        {
            var p = T2.Plazas[i];
            float d = p.round ? Mathf.Sqrt((x - p.x) * (x - p.x) + (z - p.z) * (z - p.z)) - p.w * 0.5f : T2.DistToRect(x, z, p.x, p.z, p.yaw, p.w, p.d);
            if (d < margin) return true;
        }
        return false;
    }

    /// <summary>Ruang kosong berjari-jari r: bukan bangunan/prop terpasang, bukan jalan, bukan alun-alun, bukan tepi sungai, bukan pad gerbang/jembatan.</summary>
    static bool T2Free(float x, float z, float r, float roadGap = 1f, bool plazaOk = false)
    {
        if (!IsFree(x, z, r)) return false;
        if (T2NearRoad(x, z, r + roadGap)) return false;
        if (!plazaOk && T2InPlaza(x, z, r)) return false;
        if (T2.RiverBank(x, z) > 0.02f) return false;
        if (t2PadLots == null)
        {
            t2PadLots = new List<T2.Lot>();
            foreach (var l in T2.Lots) if (l.kind == "gatepad" || l.kind == "bridgepad") t2PadLots.Add(l);
        }
        for (int i = 0; i < t2PadLots.Count; i++) if (T2.DistToRect(x, z, t2PadLots[i]) < r + 0.8f) return false;
        return true;
    }

    /// <summary>Titik bebas terdekat dari want (pencarian melingkar keluar).</summary>
    static Vector2 T2FreeSpot(Vector2 want, float r, int maxRing = 14, float roadGap = 1f, bool plazaOk = false)
    {
        if (T2Free(want.x, want.y, r, roadGap, plazaOk)) return want;
        for (int ring = 1; ring <= maxRing; ring++)
        {
            int cnt = Mathf.Max(8, ring * 6);
            for (int k = 0; k < cnt; k++)
            {
                float a = k * Mathf.PI * 2f / cnt;
                Vector2 p = want + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * ring * 1.5f;
                if (T2Free(p.x, p.y, r, roadGap, plazaOk)) return p;
            }
        }
        return want;
    }

    /// <summary>Titik jangkar dari denah (T2.Pts) atau cadangan bila tidak ada.</summary>
    static Vector2 T2PtOr(string key, Vector2 fallback)
    {
        Vector2 p;
        return T2.Pts.TryGetValue(key, out p) ? p : fallback;
    }

    static void T2Trunk(GameObject go, float h = 3f, float r = 0.45f)
    {
        var cc = go.AddComponent<CapsuleCollider>();
        cc.center = new Vector3(0f, h * 0.5f, 0f); cc.radius = r; cc.height = h;
    }

    /// <summary>Cincin kotak penghalang (kolam air mancur): n kotak menyinggung lingkaran jari-jari R di sekitar titik asal induk.</summary>
    static void T2RingCols(Transform parent, float R, float h, int n)
    {
        float len = 2f * R * Mathf.Sin(Mathf.PI / n) * 1.12f;
        for (int k = 0; k < n; k++)
        {
            float a = k * 360f / n, ar = a * Mathf.Deg2Rad;
            BoxCol(parent, "KolamCol", new Vector3(Mathf.Sin(ar) * R, h * 0.5f, Mathf.Cos(ar) * R), new Vector3(len, h, 0.7f), new Vector3(0f, a, 0f));
        }
    }

    static List<Vector3> T2P3(IList<Vector2> pts, float dy = 0f)
    {
        var l = new List<Vector3>();
        foreach (var p in pts) l.Add(T2P(p.x, p.y, dy));
        return l;
    }

    // =====================================================================
    // Pertanian (tenggara): petak ladang, pagar, orang-orangan, jerami, ternak diam, kebun apel
    // =====================================================================
    static void T2Farm()
    {
        var g = Group("Pertanian", tFarm);
        T2FarmBuildings(g);
        T2Fields(g);
        T2FarmExtras(g);
        T2Orchard(g);
    }

    /// <summary>Sisi petak (0 = -X, 1 = +X, 2 = -Z, 3 = +Z lokal) yang paling dekat jalan: dibiarkan terbuka tanpa pagar.</summary>
    static int T2OpenSide(T2.Lot l)
    {
        float[] sx = { -1f, 1f, 0f, 0f }, sz = { 0f, 0f, -1f, 1f };
        int open = 0; float best = 1e9f;
        for (int s = 0; s < 4; s++)
        {
            Vector2 mid = T2.ToWorld(l, sx[s] * (l.w * 0.5f + 1f), sz[s] * (l.d * 0.5f + 1f));
            float dd = 1e9f;
            foreach (var r in T2.Roads) dd = Mathf.Min(dd, T2.DistPoly(mid.x, mid.y, r.pts) - r.width * 0.5f);
            if (dd < best) { best = dd; open = s; }
        }
        return open;
    }

    static void T2Fields(Transform g)
    {
        var ground = new GeoBag(); var crop = new GeoBag(); var fence = new GeoBag();
        var fcols = Group("PagarCol", g);
        int fieldIdx = 0;
        foreach (var l in T2.Lots)
        {
            if (l.kind != "field") continue;
            fieldIdx++;
            float y = l.py + 0.035f;
            Vector2 c0 = T2.ToWorld(l, -l.w * 0.5f, -l.d * 0.5f), c1 = T2.ToWorld(l, -l.w * 0.5f, l.d * 0.5f), c2 = T2.ToWorld(l, l.w * 0.5f, l.d * 0.5f), c3 = T2.ToWorld(l, l.w * 0.5f, -l.d * 0.5f);
            Vector2 rowDir = (c3 - c0).normalized;
            int v = Mathf.Abs(l.v) % 4;
            ground.Patch(v == 1 || v == 3 ? Sl.FieldB : Sl.Field, c0, c1, c2, c3, y, rowDir);
            crop.Push(new Vector3(l.x, l.py, l.z), l.yaw);
            int seed = (int)(l.x * 3f + l.z);
            if (v == 0)                    // gandum masak: barisan sepanjang X
            {
                int rows = Mathf.Max(3, (int)((l.d - 1.6f) / 0.75f));
                for (int r = 0; r < rows; r++)
                {
                    float z = -l.d * 0.5f + 0.8f + r * (l.d - 1.6f) / (rows - 1);
                    float h = 0.85f + 0.2f * T2Kit.Rnd(seed + r, 3);
                    crop.Box(Sl.Leaf, new Vector3(0f, 0.25f, z), new Vector3(l.w - 1.4f, 0.5f, 0.42f));
                    crop.Box(Sl.Thatch, new Vector3(0f, 0.25f + h * 0.55f, z), new Vector3(l.w - 1.5f, h * 0.7f, 0.36f));
                }
            }
            else if (v == 1)               // sayur: guludan + bola hijau
            {
                int rows = Mathf.Max(2, (int)((l.d - 1.4f) / 1.1f));
                for (int r = 0; r < rows; r++)
                {
                    float z = -l.d * 0.5f + 0.9f + r * (l.d - 1.8f) / Mathf.Max(1, rows - 1);
                    crop.Box(Sl.Dirt, new Vector3(0f, 0.12f, z), new Vector3(l.w - 1.4f, 0.24f, 0.55f));
                    int k = Mathf.Max(3, (int)((l.w - 2f) / 0.7f));
                    for (int i = 0; i < k; i++)
                        crop.Ball((i + r) % 4 == 0 ? Sl.Blossom : Sl.Hedge, new Vector3(-(l.w - 2f) * 0.5f + i * (l.w - 2f) / (k - 1), 0.42f, z), 0.2f + 0.05f * T2Kit.Rnd(seed + i, r), 3, 6, 0.9f);
                }
            }
            else if (v == 2)               // kubis
            {
                int cols = Mathf.Max(3, (int)((l.w - 1.6f) / 1.1f)), rows = Mathf.Max(2, (int)((l.d - 1.4f) / 1.1f));
                for (int r = 0; r < rows; r++)
                    for (int i = 0; i < cols; i++)
                        crop.Ball(Sl.Leaf, new Vector3(-(l.w - 1.8f) * 0.5f + i * (l.w - 1.8f) / Mathf.Max(1, cols - 1), 0.3f, -(l.d - 1.6f) * 0.5f + r * (l.d - 1.6f) / Mathf.Max(1, rows - 1)), 0.34f, 3, 7, 0.85f);
            }
            else                           // petak bajakan: alur melintang
            {
                int fr = Mathf.Max(4, (int)((l.w - 0.8f) / 0.6f));
                for (int i = 0; i < fr; i++)
                    crop.Box(i % 2 == 0 ? Sl.Field : Sl.Dirt, new Vector3(-(l.w - 0.8f) * 0.5f + i * (l.w - 0.8f) / (fr - 1), 0.07f, 0f), new Vector3(0.3f, 0.14f, l.d - 0.8f));
            }
            crop.Pop();

            // pagar tiga sisi
            int open = T2OpenSide(l);
            Vector2[] cor = { T2.ToWorld(l, -l.w * 0.5f - 1f, -l.d * 0.5f - 1f), T2.ToWorld(l, -l.w * 0.5f - 1f, l.d * 0.5f + 1f), T2.ToWorld(l, l.w * 0.5f + 1f, l.d * 0.5f + 1f), T2.ToWorld(l, l.w * 0.5f + 1f, -l.d * 0.5f - 1f) };
            int[][] sides = { new[] { 0, 1 }, new[] { 3, 2 }, new[] { 0, 3 }, new[] { 1, 2 } };
            for (int s = 0; s < 4; s++)
            {
                if (s == open) continue;
                var pts = new List<Vector3> { T2P(cor[sides[s][0]].x, cor[sides[s][0]].y), T2P(cor[sides[s][1]].x, cor[sides[s][1]].y) };
                V2Kit.RailFence(fence, pts, 1.05f, 2);
                V2FenceCols(fcols, pts, 1.0f);
            }
            ReserveBox(l.x, l.z, l.w + 2f, l.d + 2f, l.yaw, 0f);
            Reserve(l.x, l.z, Mathf.Max(l.w, l.d) * 0.5f + 1.5f);
            if (fieldIdx % 6 == 2)                                                                  // orang-orangan sawah di tiap enam petak
                V2PlaceProp("V2_Scarecrow", V2Kit.Scarecrow, g, "OrangOrangSawah", T2P(l.x, l.z), 20f + fieldIdx * 37f);
        }
        ground.Emit(g, "TanahLadang", baseSlots, MeshDir + "/T2_FieldGround.asset");
        crop.Emit(g, "Tanaman", baseSlots, MeshDir + "/T2_FieldCrop.asset");
        fence.Emit(g, "PagarLadang", baseSlots, MeshDir + "/T2_FieldFence.asset");
    }

    /// <summary>Tumpukan jerami, gerobak, peti, sumur, palung, dan ternak diam di halaman rumah tani & lumbung (kota ini tanpa ternak hidup).</summary>
    static void T2FarmExtras(Transform g)
    {
        var bag = new GeoBag();
        // jerami di sekeliling rumah tani dan lumbung
        Vector2[] hayAt = T2.HayAt.ToArray();
        int n = 0;
        foreach (var h in hayAt)
        {
            Vector2 p = T2FreeSpot(h, 2.6f, 10, 1.5f);
            if (!T2Free(p.x, p.y, 2.6f, 1.5f)) continue;
            Vector3 q = T2P(p.x, p.y);
            bag.Push(q, h.x * 7f);
            V2Kit.Haystack(bag);
            bag.Pop();
            BoxCol(g, "JeramiCol", q + Vector3.up * 1.2f, new Vector3(3f, 2.4f, 3f), Vector3.zero);
            Reserve(p.x, p.y, 2.6f);
            n++;
        }
        // gerobak + peti + tong dekat lumbung
        Vector2 cw = T2FreeSpot(T2PtOr("cart", new Vector2(262f, -208f)), 2.4f, 10, 1.5f);
        Vector3 cq = T2P(cw.x, cw.y);
        bag.Push(cq, 70f); V2Kit.Cart(bag); bag.Pop();
        BoxCol(g, "GerobakCol", cq + Vector3.up * 0.8f, new Vector3(2.6f, 1.6f, 1.6f), new Vector3(0f, 70f, 0f));
        Reserve(cw.x, cw.y, 2.4f);
        Vector2 kw = T2FreeSpot(T2PtOr("crates", new Vector2(122f, -236f)), 2f, 10, 1.5f);
        Vector3 kq = T2P(kw.x, kw.y);
        bag.Push(kq, 15f);
        V2Kit.Crate(bag, Vector3.zero, 0.9f, 20f); V2Kit.Crate(bag, new Vector3(1.1f, 0f, 0.2f), 0.8f, -12f); V2Kit.Barrel(bag, new Vector3(-1.2f, 0f, 0.1f)); V2Kit.Sack(bag, new Vector3(0.2f, 0f, 1.1f), 0.9f);
        bag.Pop();
        BoxCol(g, "PetiCol", kq + Vector3.up * 0.6f, new Vector3(3.6f, 1.2f, 2.6f), new Vector3(0f, 15f, 0f));
        Reserve(kw.x, kw.y, 2.4f);
        bag.Emit(g, "PerlengkapanTani", baseSlots, MeshDir + "/T2_FarmStuff.asset");

        // sumur tua di ladang luar: ditutup papan, diikat tali merah, digaris garam (kanon: penggali sumur selalu berhenti di kedalaman yang sama)
        Vector2 ww = T2FreeSpot(T2PtOr("well_tani", new Vector2(150f, -262f)), 3.6f, 12, 1.5f);
        var well = PropKit.Place(PropKit.Get("T2_PlankWell", T2Kit.PlankWell), g, "SumurTertutup", T2P(ww.x, ww.y), 20f, 1f, baseSlots);
        BoxCol(well.transform, "SumurCol", new Vector3(0f, 0.8f, 0f), new Vector3(2.8f, 1.6f, 2.8f), Vector3.zero);
        Reserve(ww.x, ww.y, 3.8f);
        var wip = VPoint<InfoPoint>(g, "InteractPoint_SumurTertutup", T2P(ww.x, ww.y - 3.4f), "Periksa Sumur Tua", 3.2f);
        wip.title = "Sumur Bertutup Papan";
        wip.message = "Lima papan menutup mulut sumur ini, ditindih batu dan diikat tali merah. Garam ditaburkan melingkar di sekelilingnya, dua jari lebar, tanpa putus.\n\nOrang-orang ladang menimba di sumur lain. Para penggali sumur Ashwatch selalu berhenti pada kedalaman yang sama, dan tak seorang pun mau berkata mengapa.";

        // ternak diam (domba di dekat rumah tani barat, sapi di dekat lumbung) + palung
        var sheepP = PropKit.Get("Sheep", PropKit.Sheep);
        var cowP = PropKit.Get("Cow", PropKit.Cow);
        var troughP = PropKit.Get("V2_Trough", V2Kit.Trough);
        var rng = new System.Random(8841);
        Vector2[] herds = T2.Herds.ToArray();
        for (int hI = 0; hI < herds.Length; hI++)
        {
            bool cows = hI >= 2;
            Vector2 c = T2FreeSpot(herds[hI], 7f, 10, 1.5f);
            int cnt = cows ? 3 : 4;
            for (int i = 0; i < cnt; i++)
            {
                Vector2 p = c + new Vector2((float)(rng.NextDouble() * 2 - 1), (float)(rng.NextDouble() * 2 - 1)) * 5f;
                if (!T2Free(p.x, p.y, 1.4f, 1f)) continue;
                var a = PropKit.Place(cows ? cowP : sheepP, g, cows ? "Sapi" : "Domba", T2P(p.x, p.y), (float)rng.NextDouble() * 360f, 1f, baseSlots);
                BoxCol(a.transform, "TernakCol", new Vector3(0f, 0.7f, 0f), cows ? new Vector3(1.0f, 1.4f, 2.2f) : new Vector3(0.8f, 1.0f, 1.5f), Vector3.zero);
                Reserve(p.x, p.y, 1.3f);
            }
            Vector2 tp = T2FreeSpot(c + new Vector2(5.5f, 0f), 1.8f, 6, 1f);
            var tr = PropKit.Place(troughP, g, "Palung", T2P(tp.x, tp.y), 90f, 1f, baseSlots);
            BoxCol(tr.transform, "PalungCol", new Vector3(0f, 0.35f, 0f), new Vector3(0.8f, 0.7f, 2.4f), Vector3.zero);
            Reserve(tp.x, tp.y, 1.8f);
        }
    }

    /// <summary>Kebun apel di sektor barat daya: pohon berderet di dalam poligon zona, menghindari jalan kebun dan sungai.</summary>
    static void T2Orchard(Transform g)
    {
        T2.Zone zone = null;
        foreach (var z in T2.Zones) if (z.id == "kebun_apel") zone = z;
        if (zone == null) return;
        var og = Group("KebunApel", g);
        int n = 0, placed = 0;
        var ob = T2Box(zone.poly, 0f);
        for (float x = ob[0]; x <= ob[1]; x += 8.5f)
            for (float z = ob[2]; z <= ob[3]; z += 8.5f)
            {
                int seed = n++;
                float jx = x + ((seed * 37) % 13 - 6) * 0.25f, jz = z + ((seed * 53) % 11 - 5) * 0.25f;
                if (!T2.Inside(jx, jz, zone.poly) || T2.SignedDist(jx, jz, zone.poly) > -2.5f) continue;
                if (!T2Free(jx, jz, 2.6f, 1.5f)) continue;
                var go = PropKit.Place(PropKit.TreeProto(5), og, "PohonApel", T2P(jx, jz), seed * 53f, 1f + 0.12f * (seed % 3), baseSlots);
                T2Trunk(go, 2.4f, 0.4f);
                Reserve(jx, jz, 2.6f);
                placed++;
            }
        if (placed == 0) return;
        // gerobak buah + peti di tepi kebun dekat jalan
        var bag = new GeoBag();
        Vector2 cp = T2FreeSpot(T2PtOr("orchard_cart", new Vector2(-250f, -240f)), 2.6f, 14, 1.2f);
        Vector3 cq = T2P(cp.x, cp.y);
        bag.Push(cq, 200f); V2Kit.Cart(bag); bag.Pop();
        Vector2 kp = T2FreeSpot(cp + new Vector2(3.4f, 1.2f), 1.8f, 8, 1f);
        bag.Push(T2P(kp.x, kp.y), 0f); V2Kit.Crate(bag, Vector3.zero, 0.9f, 20f); V2Kit.Barrel(bag, new Vector3(1.4f, 0f, 0.2f)); bag.Pop();
        bag.Emit(og, "GerobakBuah", baseSlots, MeshDir + "/T2_OrchardCart.asset");
        BoxCol(og, "GerobakCol", cq + Vector3.up * 0.8f, new Vector3(2.6f, 1.6f, 1.6f), new Vector3(0f, 200f, 0f));
        Reserve(cp.x, cp.y, 2.6f); Reserve(kp.x, kp.y, 1.8f);
    }

    // =====================================================================
    // Taman kota
    // =====================================================================
    static void T2Pavilion(GeoBag b)
    {
        const int N = 6;
        const float R = 3.0f;
        b.Cyl(Sl.Stone, Vector3.zero, R + 0.55f, 0.32f, 12, R + 0.45f, true);
        b.Cyl(Sl.Marble, new Vector3(0f, 0.32f, 0f), R + 0.3f, 0.12f, 12, R + 0.22f, true);
        for (int k = 0; k < N; k++)
        {
            float a0 = k * Mathf.PI * 2f / N, a1 = (k + 1) * Mathf.PI * 2f / N;
            Vector3 p0 = new Vector3(Mathf.Sin(a0) * R, 0.44f, Mathf.Cos(a0) * R), p1 = new Vector3(Mathf.Sin(a1) * R, 0.44f, Mathf.Cos(a1) * R);
            b.Cyl(Sl.Wood, p0, 0.17f, 3.1f, 8, 0.14f, true);
            b.Cyl(Sl.DarkStone, p0, 0.24f, 0.3f, 8, 0.2f, true);
            b.Beam(Sl.Wood, p0 + new Vector3(0f, 3.0f, 0f), p1 + new Vector3(0f, 3.0f, 0f), 0.2f, 0.2f);
            if (k != 0)                                                      // pagar rendah (k=0 = pintu masuk)
            {
                b.Beam(Sl.WoodLight, p0 + new Vector3(0f, 0.95f, 0f), p1 + new Vector3(0f, 0.95f, 0f), 0.09f, 0.09f);
                b.Beam(Sl.WoodLight, p0 + new Vector3(0f, 0.55f, 0f), p1 + new Vector3(0f, 0.55f, 0f), 0.07f, 0.07f);
            }
        }
        b.Cone(Sl.Roof, new Vector3(0f, 3.55f, 0f), R + 1.0f, 2.1f, N);
        b.Cone(Sl.RoofB, new Vector3(0f, 5.5f, 0f), 0.4f, 0.8f, N);
        b.Ball(Sl.Gold, new Vector3(0f, 6.35f, 0f), 0.2f, 3, 6);
        b.Box(Sl.WoodLight, new Vector3(0f, 0.9f, 0f), new Vector3(1.2f, 0.12f, 1.2f), 20f);       // meja bundar kecil di tengah
        b.Cyl(Sl.Wood, new Vector3(0f, 0.44f, 0f), 0.3f, 0.5f, 8, 0.22f, true);
    }

    static void T2Park(Transform root, T2.Zone z)
    {
        var g = Group("Taman_" + z.id, root);
        var poly = z.poly;
        Vector2 c = Vector2.zero;
        foreach (var q in poly) c += q;
        c /= poly.Count;
        bool sakura = z.id == "taman_sakura", timur = z.id == "taman_timur", utara = z.id == "taman_utara";
        var paths = new List<List<Vector2>>();

        // ---- pintu masuk: dua titik poligon yang paling dekat jalan (saling berjauhan)
        int e1 = 0, e2 = -1;
        float best = 1e9f;
        for (int i = 0; i < poly.Count; i++)
        {
            float d = 1e9f;
            foreach (var r in T2.Roads) if (!r.dead) d = Mathf.Min(d, T2.DistPoly(poly[i].x, poly[i].y, r.pts) - r.width * 0.5f);
            if (d < best) { best = d; e1 = i; }
        }
        best = 1e9f;
        for (int i = 0; i < poly.Count; i++)
        {
            int dd = Mathf.Abs(i - e1); dd = Mathf.Min(dd, poly.Count - dd);
            if (dd < poly.Count / 3) continue;
            float d = 1e9f;
            foreach (var r in T2.Roads) if (!r.dead) d = Mathf.Min(d, T2.DistPoly(poly[i].x, poly[i].y, r.pts) - r.width * 0.5f);
            if (d < best) { best = d; e2 = i; }
        }
        var gates = new List<Vector2> { poly[e1] };
        if (e2 >= 0) gates.Add(poly[e2]);

        // ---- pagar tanaman keliling dengan celah di pintu masuk
        var hedge = new GeoBag();
        var run = new List<Vector3>();
        var hcols = Group("PagarCol", g);
        for (int i = 0; i < poly.Count; i++)
        {
            Vector2 a = poly[i], b2 = poly[(i + 1) % poly.Count], mid = (a + b2) * 0.5f;
            bool skip = false;
            foreach (var gt in gates) if (Vector2.Distance(mid, gt) < 8.5f) skip = true;
            if (skip) { if (run.Count > 1) { V2Kit.HedgeRun(hedge, run, 1.2f, 1.0f, i); V2FenceCols(hcols, run, 1.1f); } run = new List<Vector3>(); continue; }
            if (run.Count == 0) run.Add(T2P(a.x, a.y));
            run.Add(T2P(b2.x, b2.y));
        }
        if (run.Count > 1) { V2Kit.HedgeRun(hedge, run, 1.2f, 1.0f, 99); V2FenceCols(hcols, run, 1.1f); }
        hedge.Emit(g, "PagarTanaman", baseSlots, MeshDir + "/T2_Hedge_" + z.id + ".asset");

        // ---- jalan setapak: pintu masuk -> pusat, cincin pusat
        var pathBag = new GeoBag();
        foreach (var gt in gates)
        {
            var pl = new List<Vector2>();
            Vector2 dir = (c - gt).normalized;
            for (int k = 0; k <= 12; k++)
            {
                float u = k / 12f;
                Vector2 p = Vector2.Lerp(gt, c, u);
                // sedikit lengkung agar tidak kaku (taman utara lurus)
                Vector2 nr = new Vector2(dir.y, -dir.x) * Mathf.Sin(u * Mathf.PI) * (utara ? 0f : 2.2f);
                pl.Add(p + nr);
            }
            paths.Add(pl);
            T2Strip(pathBag, Sl.Cobble, pl, 3.2f, 0.07f, 2);
            T2Strip(pathBag, Sl.Stone, pl, 3.9f, 0.055f, 2);
        }
        float ringR = utara ? 5.5f : 9f;
        var ring = new List<Vector2>();
        if (!utara)
        {
            for (int k = 0; k <= 28; k++) { float a = k * Mathf.PI * 2f / 28f; ring.Add(c + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * ringR); }
            paths.Add(ring);
            T2Strip(pathBag, Sl.Cobble, ring, 2.8f, 0.07f, 2);
            T2Strip(pathBag, Sl.Stone, ring, 3.4f, 0.055f, 2);
        }
        pathBag.Emit(g, "Setapak", baseSlots, MeshDir + "/T2_ParkPaths_" + z.id + ".asset");

        // ---- pusat taman
        if (sakura)
        {
            var pv = PropKit.Place(PropKit.Get("T2_Pavilion", T2Pavilion), g, "Paviliun", T2P(c.x, c.y, -0.04f), 0f, 1f, baseSlots);
            for (int k = 0; k < 6; k++)
            {
                float a = k * Mathf.PI * 2f / 6f;
                var cc = pv.AddComponent<CapsuleCollider>();            // satu tiang per kapsul
                cc.center = new Vector3(Mathf.Sin(a) * 3f, 1.6f, Mathf.Cos(a) * 3f); cc.radius = 0.25f; cc.height = 3.2f;
            }
            Reserve(c.x, c.y, 5.5f);
        }
        else if (timur)
        {
            var fo = PropKit.Place(PropKit.Get("T2_PlazaFountain", T2Kit.PlazaFountain), g, "MancurTaman", T2P(c.x, c.y, -0.05f), 0f, 1f, baseSlots);
            T2RingCols(fo.transform, 4.8f, 1.2f, 12);
            Reserve(c.x, c.y, 6.6f);
        }
        else
        {
            // taman utara: tiga plinth patung sepanjang sumbu panjang
            for (int k = -1; k <= 1; k++)
            {
                Vector2 pp = c + new Vector2(k * 12f, 0f);
                var pl = PropKit.Place(PropKit.Get("T2_Plinth", b => T2Kit.StatuePlinth(b, 0f)), g, "PatungTaman", T2P(pp.x, pp.y), 180f, 1.15f, baseSlots);
                BoxCol(pl.transform, "PlinthCol", new Vector3(0f, 1.2f, 0f), new Vector3(2.4f, 2.4f, 2.4f), Vector3.zero);
                Reserve(pp.x, pp.y, 2.4f);
            }
        }

        // ---- pohon: kisi berjitter di dalam poligon, jauh dari setapak
        float x0 = 1e9f, x1 = -1e9f, z0 = 1e9f, z1 = -1e9f;
        foreach (var q in poly) { x0 = Mathf.Min(x0, q.x); x1 = Mathf.Max(x1, q.x); z0 = Mathf.Min(z0, q.y); z1 = Mathf.Max(z1, q.y); }
        int placed = 0, idx = 0;
        float stepT = utara ? 6.5f : 6.4f;
        for (float x = x0; x <= x1; x += stepT)
            for (float zz = z0; zz <= z1; zz += stepT)
            {
                int seed = idx++;
                float jx = x + ((seed * 37) % 13 - 6) * 0.3f, jz = zz + ((seed * 53) % 11 - 5) * 0.3f;
                if (!T2.Inside(jx, jz, poly) || T2.SignedDist(jx, jz, poly) > -2.2f) continue;
                if ((seed * 7) % 10 < 2) continue;                                              // jarang-jarang agar tidak seragam
                if (utara && Mathf.Abs(jz - c.y) < 4.5f) continue;                             // sumbu patung dibiarkan terbuka
                bool nearPath = false;
                foreach (var pl in paths) if (T2.DistPoly(jx, jz, pl) < 3.4f) { nearPath = true; break; }
                if (nearPath) continue;
                if (Vector2.Distance(new Vector2(jx, jz), c) < (utara ? 3f : 6.8f)) continue;
                if (!T2Free(jx, jz, 1.9f, 1.0f, true)) continue;
                int kind = sakura ? ((seed % 5 == 0) ? 0 : 2) : (timur ? ((seed % 6 == 0) ? 4 : ((seed % 4 == 0) ? 5 : 0)) : 3);
                if (kind == 4) { PropKit.Place(PropKit.TreeProto(4), g, "Semak", T2P(jx, jz), seed * 41f, 1.1f, baseSlots); }
                else
                {
                    var go = PropKit.Place(PropKit.TreeProto(kind), g, "Pohon", T2P(jx, jz), seed * 41f, 0.95f + 0.1f * (seed % 4), baseSlots);
                    if (kind != 3) T2Trunk(go, 2.6f, 0.4f); else T2Trunk(go, 3f, 0.35f);
                }
                Reserve(jx, jz, 2.2f);
                placed++;
            }

        // ---- bangku menghadap pusat, petak bunga, lampu di setapak
        var bg = new GeoBag();
        var bench = PropKit.Get("T2_Bench", PropKit.Bench);
        int nb = utara ? 4 : 6;
        for (int k = 0; k < nb; k++)
        {
            Vector2 bp;
            float yaw;
            if (utara) { bp = c + new Vector2((k - 1.5f) * 12f, (k % 2 == 0 ? -1f : 1f) * 3.6f); yaw = k % 2 == 0 ? 0f : 180f; }
            else { float a = (k + 0.5f) * Mathf.PI * 2f / nb; bp = c + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * (ringR + 2.4f); yaw = a * Mathf.Rad2Deg + 180f; }
            if (!T2Free(bp.x, bp.y, 1.3f, 0.5f, true)) continue;
            var bo = PropKit.Place(bench, g, "Bangku", T2P(bp.x, bp.y), yaw, 1f, baseSlots);
            BoxCol(bo.transform, "BangkuCol", new Vector3(0f, 0.5f, 0f), new Vector3(2.0f, 1.0f, 0.8f), Vector3.zero);
            Reserve(bp.x, bp.y, 1.3f);
        }
        var beds = new GeoBag();
        int nBeds = utara ? 0 : 4;
        for (int k = 0; k < nBeds; k++)
        {
            float a = (k + 0.5f) * Mathf.PI * 0.5f + 0.78f;
            Vector2 bp = c + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * (ringR + 6.6f);
            if (!T2Free(bp.x, bp.y, 2.4f, 0.5f, true)) continue;
            beds.Push(T2P(bp.x, bp.y), a * Mathf.Rad2Deg + 90f);
            V2Kit.GardenBed(beds, 4.2f, 1.6f, k + 5);
            beds.Pop();
            Reserve(bp.x, bp.y, 2.4f);
        }
        beds.Emit(g, "PetakBunga", baseSlots, MeshDir + "/T2_ParkBeds_" + z.id + ".asset");
        int li = 0;
        foreach (var gt in gates)
            for (int k = 1; k <= 3; k++)
            {
                Vector2 p = Vector2.Lerp(gt, c, k / 4f);
                Vector2 tg = (c - gt).normalized, nr = new Vector2(tg.y, -tg.x);
                foreach (float sg in new[] { -1f, 1f })
                {
                    if (k == 2 && sg > 0f) continue;
                    Vector2 q = p + nr * sg * 2.7f;
                    if (!T2Free(q.x, q.y, 0.7f, 0.2f, true)) continue;
                    T2PlaceLamp(T2P(q.x, q.y), Mathf.Atan2(nr.y * sg, -nr.x * sg) * Mathf.Rad2Deg, (li++ % 2) == 0);
                }
            }
        Debug.Log("[Kota T2] Taman " + z.id + ": " + placed + " pohon");
    }

    static void T2Parks()
    {
        var g = Group("Taman", tProps);
        foreach (var z in T2.Zones) if (z.kind == "park") T2Park(g, z);
    }
}
