using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kota Ashwatch "Konsentris": rute jalan warga + titik sembunyi NPC tersembunyi (data murni, bisa diuji di luar Unity).
/// Tiap rute dihitung dari jalan / alun-alun denah, lalu disaring: titik di jembatan (NPC mengikuti medan, tidak naik jembatan), di sungai,
/// di pita tembok atau parit (tiga tembok + tembok pembatas kastil), atau di dalam lahan bangunan dibuang dan rute dipotong
/// (diambil bagian terpanjang yang seluruhnya bisa dilalui). Jadi tiap rute tinggal di SATU cincin: ladang, kota tengah, atau bukit mahkota.
/// </summary>
public static partial class T2
{
    public class NpcRoute { public string key; public string[] who; public List<Vector2> pts = new List<Vector2>(); public float length; }
    public static readonly List<NpcRoute> NpcRoutes = new List<NpcRoute>();
    public static readonly List<string> RouteNotes = new List<string>();

    /// <summary>Titik tiap ~step m sepanjang jalan dari jarak d0 ke d1, digeser lateral off meter (positif = kanan arah jalan).</summary>
    public static List<Vector2> RoadSeg(string id, float d0, float d1, float off, float step)
    {
        var l = new List<Vector2>();
        var r = RoadOf(id);
        if (r == null) { RouteNotes.Add("jalan tidak ada: " + id); return l; }
        float total = Len(r.pts);
        d0 = Mathf.Clamp(d0, 0f, total); d1 = Mathf.Clamp(d1, 0f, total);
        int n = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(d1 - d0) / step));
        for (int i = 0; i <= n; i++)
        {
            Vector2 tg; Vector2 p = At(r.pts, Mathf.Lerp(d0, d1, i / (float)n), out tg);
            l.Add(p + new Vector2(tg.y, -tg.x) * off);
        }
        return l;
    }

    /// <summary>Busur lingkaran (sudut dari utara searah jarum jam, derajat).</summary>
    public static List<Vector2> ArcPts(Vector2 c, float r, float a0, float a1, int n)
    {
        var l = new List<Vector2>();
        for (int i = 0; i < n; i++)
        {
            float a = Mathf.Lerp(a0, a1, i / (float)Mathf.Max(1, n - 1)) * Mathf.Deg2Rad;
            l.Add(c + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * r);
        }
        return l;
    }

    public static List<Vector2> Line(params float[] xz)
    {
        var l = new List<Vector2>();
        for (int i = 0; i + 1 < xz.Length; i += 2) l.Add(new Vector2(xz[i], xz[i + 1]));
        return l;
    }

    // ------------------------------------------------------------------ uji lahan
    public static bool OnBridge(float x, float z)
    {
        for (int i = 0; i < Bridges.Count; i++)
        {
            var br = Bridges[i];
            float a = br.yaw * Mathf.Deg2Rad;
            Vector2 t = new Vector2(Mathf.Sin(a), Mathf.Cos(a)), n = new Vector2(t.y, -t.x), d = new Vector2(x, z) - br.c;
            if (Mathf.Abs(Vector2.Dot(d, t)) < br.L * 0.5f + 0.4f && Mathf.Abs(Vector2.Dot(d, n)) < br.W + 1.5f) return true;
        }
        return false;
    }

    /// <summary>Titik di dalam lahan bangunan (rumah, gedung penting, komplek) berjarak pad dari tepi.</summary>
    public static bool InBuildingLot(float x, float z, float pad)
    {
        for (int i = 0; i < Lots.Count; i++)
        {
            var l = Lots[i];
            if (l.kind == "field" || l.kind == "gatepad" || l.kind == "bridgepad") continue;
            float reach = Mathf.Max(l.w, l.d) * 0.5f + pad + 2f;
            if (Mathf.Abs(x - l.x) > reach || Mathf.Abs(z - l.z) > reach) continue;
            if (DistToRect(x, z, l) < pad) return true;
        }
        return false;
    }

    /// <summary>Titik dalam jarak d dari tembok pembatas kastil (garis terbuka di belakang Katedral).</summary>
    static bool NearChord(float x, float z, float d)
    {
        return Chord.Count > 1 && DistPoly(x, z, Chord) < d;
    }

    /// <summary>Bisa dilalui NPC: di dalam Tembok Luar, bukan di sungai / jembatan / pita tembok / parit / lahan bangunan.</summary>
    public static bool WalkOk(float x, float z)
    {
        if (OnBridge(x, z)) return false;
        if (RiverBank(x, z) > 0.2f) return false;
        if (!Inside(x, z, OuterPoly)) return false;
        float so = SdOuter(x, z); if (so > -9f) return false;                                           // jalur dalam Tembok Luar dipakai peronda lain
        float sm = SdMid(x, z); if (sm > -4.6f && sm < DitchFarEdge + 1f) return false;                  // Tembok Tengah + parit kering
        float si = SdInner(x, z); if (si > -4.6f && si < KDitchFarEdge + 1f) return false;              // Tembok Dalam + parit kering
        if (NearChord(x, z, 4.8f)) return false;                                                         // tembok pembatas halaman kastil
        return !InBuildingLot(x, z, 0.8f);
    }

    static bool SegOk(Vector2 a, Vector2 b)
    {
        float len = (b - a).magnitude;
        int n = Mathf.Max(1, Mathf.CeilToInt(len / 1.5f));
        for (int i = 1; i < n; i++)
        {
            Vector2 p = Vector2.Lerp(a, b, i / (float)n);
            if (!WalkOk(p.x, p.y)) return false;
        }
        return true;
    }

    static void AddRoute(string key, List<Vector2> pts, params string[] who)
    {
        List<Vector2> best = new List<Vector2>(), cur = new List<Vector2>();
        for (int i = 0; i < pts.Count; i++)
        {
            bool ok = WalkOk(pts[i].x, pts[i].y);
            if (ok && cur.Count > 0 && !SegOk(cur[cur.Count - 1], pts[i])) { if (cur.Count > best.Count) best = cur; cur = new List<Vector2>(); }
            if (ok) cur.Add(pts[i]);
            else { if (cur.Count > best.Count) best = cur; cur = new List<Vector2>(); }
        }
        if (cur.Count > best.Count) best = cur;
        if (best.Count < 2) { RouteNotes.Add("rute dibuang (tidak ada bagian yang bisa dilalui): " + key); return; }
        if (best.Count < pts.Count) RouteNotes.Add("rute dipotong " + key + ": " + best.Count + "/" + pts.Count + " titik");
        var r = new NpcRoute { key = key, who = who, pts = best, length = Len(best) };
        NpcRoutes.Add(r);
    }

    // ------------------------------------------------------------------ rute
    static List<Vector2> RoadAll(string id, float cut0, float cut1, float off, float step)
    {
        var r = RoadOf(id);
        if (r == null) { RouteNotes.Add("jalan tidak ada: " + id); return new List<Vector2>(); }
        float total = Len(r.pts);
        return RoadSeg(id, cut0, total - cut1, off, step);
    }

    public static void BuildRoutes()
    {
        NpcRoutes.Clear();
        RouteNotes.Clear();
        if (River == null) Build();
        Vector2 fg = FountainGate, pm = PlazaMid;
        float aPlazaS = ArcAt("raya", 2f, -284f), aPlazaN = ArcAt("raya", 4f, -240f), aBr = ArcAt("raya", 6f, -172f);

        // ---- Alun-alun Gerbang (selatan sungai): cincin air mancur + muka lapak pasar (tidak berubah)
        AddRoute("MancurA", ArcPts(fg, 11.4f, 12f, 168f, 9), "Warga", "Peziarah", "Hunter");
        AddRoute("MancurB", ArcPts(fg, 11.4f, 192f, 348f, 9), "Warga", "Pedagang", "Pelajar");
        AddRoute("PasarUtara", Line(10f, -239f, 22f, -239f, 34f, -239f, 46f, -239f), "Pedagang", "Warga");
        AddRoute("PasarSelatan", Line(10f, -270f, 22f, -270f, 34f, -270f, 46f, -270f), "Warga", "Hunter");
        AddRoute("RayaSelatanTimur", RoadSeg("raya", 24f, aPlazaS, 4.2f, 11f), "Pedagang", "Warga");
        AddRoute("RayaSelatanBarat", RoadSeg("raya", 24f, aPlazaS, -4.2f, 11f), "Warga", "Pelajar");
        AddRoute("PasarBarat", RoadAll("pasar_barat", 4f, 8f, 0f, 10f), "Warga", "Pelajar");
        AddRoute("PasarTimur", RoadAll("pasar_timur", 4f, 8f, 0f, 10f), "Pedagang", "Warga");
        AddRoute("RayaGuild", RoadSeg("raya", aPlazaN + 2f, aBr - 18f, 3.8f, 10f), "Warga", "Hunter");

        // ---- Cincin ladang (antara Tembok Luar dan Tembok Tengah): petani di jalan tani, dekat gerbang luar
        var tc = RoadOf("tani_cincin0");
        if (tc != null)
        {
            float lt = Len(tc.pts);
            int nf = Mathf.Max(3, Mathf.FloorToInt(lt / 230f));
            for (int i = 0; i < nf; i++)
            {
                float c = lt * (i + 0.5f) / nf;
                AddRoute("Tani" + i, RoadSeg("tani_cincin0", c - 60f, c + 60f, 0f, 10f), "Petani", i % 3 == 1 ? "Warga" : "Petani");
            }
        }
        AddRoute("TimurTani", RoadSeg("timur", ArcAt("timur", 297f, -18f), ArcAt("timur", 250f, -10f), 3f, 11f), "Petani", "Pedagang");
        AddRoute("BaratTani", RoadSeg("barat", ArcAt("barat", -297f, -18f), ArcAt("barat", -250f, -10f), 3f, 11f), "Petani", "Warga");
        AddRoute("UtaraTani", RoadSeg("utara", ArcAt("utara", 0f, 278f), ArcAt("utara", 0f, 250f), 3f, 10f), "Petani", "Hunter");

        // ---- Kota tengah, atap merah (antara Tembok Tengah dan Tembok Dalam)
        float aMidS = ArcAt("raya", 0f, -146f), aSimpang = ArcAt("raya", 0f, -106f);
        AddRoute("RayaBawahT", RoadSeg("raya", aMidS + 8f, aSimpang - 14f, 3.8f, 10f), "Warga", "Pedagang");
        AddRoute("RayaBawahB", RoadSeg("raya", aMidS + 8f, aSimpang - 14f, -3.8f, 10f), "Warga", "Pelajar");
        AddRoute("Simpang", ArcPts(new Vector2(0f, -106f), 8.5f, 0f, 360f, 12), "Pedagang", "Warga");
        float ll = Len(RoadOf("lintas").pts), lu = Len(RoadOf("lingkar_utara").pts);
        AddRoute("LintasBarat", RoadSeg("lintas", 8f, ll * 0.46f, 3.3f, 14f), "Warga", "Warga", "Pedagang");
        AddRoute("LintasTimur", RoadSeg("lintas", ll * 0.54f, ll - 8f, -3.3f, 14f), "Warga", "Pedagang", "Hunter");
        AddRoute("LingkarTimur", RoadSeg("lingkar_utara", 8f, lu * 0.46f, 3.1f, 14f), "Warga", "Peziarah");
        AddRoute("LingkarBarat", RoadSeg("lingkar_utara", lu * 0.54f, lu - 8f, -3.1f, 14f), "Warga", "Pelajar");
        AddRoute("TimurBawah", RoadSeg("timur", ArcAt("timur", 212f, -4f), ArcAt("timur", 130f, 30f), 3.4f, 12f), "Warga", "Pedagang");
        AddRoute("BaratBawah", RoadSeg("barat", ArcAt("barat", -212f, -4f), ArcAt("barat", -130f, 30f), -3.4f, 12f), "Warga", "Pelajar");
        AddRoute("UtaraBawah", RoadSeg("utara", ArcAt("utara", 0f, 212f), ArcAt("utara", 0f, 168f), 3f, 11f), "Warga", "Hunter");
        AddRoute("KomplekAkses", RoadAll("komplek_akses", 2f, 2f, 0f, 8f), "Warga");
        AddRoute("Makam", Line(-70f, 184f, -58f, 184f, -46f, 184f, -34f, 184f, -24f, 184f), "Peziarah", "Peziarah");
        AddRoute("TamanBarat", ArcPts(Pts["kelopak"], 12f, 0f, 360f, 14), "Warga", "Peziarah");
        AddRoute("TamanTimur", ArcPts(new Vector2(150f, 118f), 12f, 0f, 360f, 14), "Warga", "Pelajar");

        // ---- Jalan peronda di dalam Tembok Tengah (prajurit & penjaga) — tiga potongan terpanjang
        var rings = new List<Road>();
        foreach (var r in Roads) if (r.id.StartsWith("benteng")) rings.Add(r);
        rings.Sort((p, q) => Len(q.pts).CompareTo(Len(p.pts)));
        for (int i = 0; i < rings.Count && i < 3; i++)
            AddRoute("Peronda" + i, RoadAll(rings[i].id, 8f, 8f, 0f, 14f), i == 0 ? new[] { "Prajurit", "Penjaga" } : new[] { "Penjaga" });

        // ---- Bukit mahkota (di dalam Tembok Dalam): alun-alun batu poles, parvis Katedral & Akademi, jalan ke kastil
        AddRoute("TengahA", ArcPts(pm, 14.5f, 8f, 172f, 9), "Warga", "Pelajar");
        AddRoute("TengahB", ArcPts(pm, 14.5f, 188f, 352f, 9), "Bangsawan", "Peziarah", "Warga");
        AddRoute("TengahLuar", ArcPts(pm, 24f, 0f, 360f, 16), "Warga", "Pedagang");
        AddRoute("ParvisKatedral", Line(-22f, 10f, -11f, 11.5f, 0f, 12f, 11f, 11.5f, 22f, 10f), "Peziarah", "Peziarah", "Peziarah");
        AddRoute("ParvisAkademi", ArcPts(new Vector2(50f, -6f), 6f, 0f, 360f, 13), "Pelajar", "Pelajar", "Pelajar");
        AddRoute("TimurAtas", RoadSeg("timur", ArcAt("timur", 102f, 31f), ArcAt("timur", 38f, 0f), 3f, 11f), "Warga", "Pelajar");
        AddRoute("BaratAtas", RoadSeg("barat", ArcAt("barat", -102f, 31f), ArcAt("barat", -38f, 0f), -3f, 11f), "Warga", "Peziarah");
        AddRoute("FlankTimur", RoadAll("flank_t", 6f, 6f, 3f, 12f), "Warga", "Pelajar");
        AddRoute("FlankBarat", RoadAll("flank_b", 6f, 6f, -3f, 12f), "Warga", "Peziarah");
        AddRoute("UtaraKastil", RoadAll("utara_kastil", 8f, 8f, 3f, 12f), "Bangsawan", "Pelayan", "Warga");
        AddRoute("KastilDepan", RoadAll("kastil", 5f, 3f, 3f, 9f), "Bangsawan", "Pelayan");
    }

    // ------------------------------------------------------------------ NPC tersembunyi: titik sembunyi yang diinginkan (builder mencari titik bebas terdekat)
    /// <summary>Urutan h1..h10: taman sakura, belakang restoran, belakang pandai besi, istal, menara sihir, taman timur, katedral, kediaman bangsawan, ladang, tepi ladang utara.</summary>
    public static Vector2[] HiddenWant
    {
        get
        {
            if (River == null) Build();
            Vector2 h9; if (!Pts.TryGetValue("h9", out h9)) h9 = new Vector2(200f, -330f);
            return new[]
            {
                new Vector2(-166f, 126f), new Vector2(-46f, -250f), new Vector2(73f, -278f), new Vector2(64f, -302f), new Vector2(98f, 84f),
                new Vector2(160f, 126f), new Vector2(-20f, 66f), new Vector2(-80f, 60f), h9, new Vector2(22f, 266f)
            };
        }
    }
}
