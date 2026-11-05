using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data denah Ashwatch "Benteng" (lihat Town2Plan.cs untuk alat geometri + medan): kota benteng abad pertengahan di atas bukit.
/// Urutan bangun: tembok -> sungai -> jalan keluar -> teras -> alun-alun + bangunan penting -> jalan utama -> jembatan parit -> cabang jalan
/// (pertumbuhan jalan acak terarah) -> jalan lingkar dalam tembok -> pertanian di luar tembok -> rumah rapat di sepanjang semua jalan.
/// </summary>
public static partial class T2
{
    public class Wall { public string id; public int tier; public bool closed; public List<Vector2> pts; public float thick, height; }
    public class Gate { public string id, dir, label, signOut, signIn, hook; public int tier; public Vector2 p; public float az; }
    public class Road { public string id; public List<Vector2> pts; public float width; public int mat; public bool lamps; public bool dead; public bool trees; }      // mat: 0 batu bulat, 1 tanah, 2 paving
    public class Plaza { public string id, kind; public float x, z, w, d, yaw; public bool round; }
    public class Bridge { public string id, kind = "sungai"; public Vector2 c, gate; public float yaw, L, W, y, chord, depth; public bool wide; }       // kind: "sungai" (lengkung batu) / "parit" (jembatan datar di atas parit; depth = kedalaman parit)
    public class Lot
    {
        public string id, kind, label, zone;
        public float x, z, yaw, w, d, py, hz, blend = 3f;
        public int v;
        public bool pad = true;
        public Vector2 P { get { return new Vector2(x, z); } }
    }
    public class Zone { public string id, kind; public List<Vector2> poly = new List<Vector2>(); public int v; }
    public class BannerSpot { public Vector2 p; public float yaw; public int cloth; }
    public class StatueSpot { public Vector2 p; public float yaw, scale; }

    public static readonly List<Vector2> OuterPoly = new List<Vector2>();       // Tembok Luar (tier 0): paling rendah & lebar, mengurung ladang
    public static readonly List<Vector2> MidPoly = new List<Vector2>();         // Tembok Tengah (tier 1): lebih tinggi, mengurung kota atap merah
    public static readonly List<Vector2> InnerPoly = new List<Vector2>();       // Tembok Dalam (tier 2): paling gelap & tinggi, di bukit mahkota
    public static readonly List<Vector2> Chord = new List<Vector2>();           // tembok pembatas halaman kastil di belakang Katedral (tier 2, garis terbuka)
    public static List<Vector2> River;
    public static readonly List<List<Vector2>> OutRoads = new List<List<Vector2>>();
    public static readonly List<Wall> Walls = new List<Wall>();
    public static readonly List<Gate> Gates = new List<Gate>();
    public static readonly List<Road> Roads = new List<Road>();
    public static readonly List<Plaza> Plazas = new List<Plaza>();
    public static readonly List<Bridge> Bridges = new List<Bridge>();
    public static readonly List<Lot> Lots = new List<Lot>();
    public static readonly List<Zone> Zones = new List<Zone>();
    public static readonly List<Vector2> Stalls = new List<Vector2>();
    public static readonly List<float> StallYaw = new List<float>();
    public static readonly List<BannerSpot> Banners = new List<BannerSpot>();
    public static readonly List<StatueSpot> Statues = new List<StatueSpot>();
    public static readonly List<Vector2> WaterArches = new List<Vector2>();      // titik tembok luar yang dilintasi sungai
    public static readonly List<Vector2> HayAt = new List<Vector2>();
    public static readonly List<Vector2> Herds = new List<Vector2>();
    public static readonly Dictionary<string, Vector2> Pts = new Dictionary<string, Vector2>();
    static float rx0, rx1, rz0, rz1;
    static bool built;
    static System.Random frng;

    // titik-titik penting (dipakai builder)
    public static readonly Vector2 GateS = new Vector2(0f, -330f), GateE = new Vector2(310f, -20f), GateW = new Vector2(-310f, -20f), GateN = new Vector2(0f, 290f);       // empat gerbang Tembok Luar
    public static Vector2 FountainGate = new Vector2(20f, -258f);       // air mancur utama (bundaran alun-alun gerbang)
    public static Vector2 PlazaMid = new Vector2(0f, -16f);             // alun-alun batu poles di depan Katedral (kota dalam)
    public static Vector2 Notice = new Vector2(8f, -241f);              // papan pengumuman di depan Guild (menghadap selatan, ke alun-alun); dihitung ulang di Build
    public static Vector2 GuildFront;                                   // titik muncul di depan pintu Guild (TownReturnPos)

    // ------------------------------------------------------------------ ukuran bangunan: kind -> (lebar muka, kedalaman, tinggi)
    public static void Size(string kind, out float w, out float d, out float hz)
    {
        switch (kind)
        {
            case "guild": w = 42f; d = 30f; hz = 24f; break;
            case "inn": w = 24f; d = 18f; hz = 14f; break;
            case "smithy": w = 16f; d = 13f; hz = 10f; break;
            case "shop": w = 17f; d = 14f; hz = 11f; break;
            case "tavern": w = 20f; d = 15f; hz = 12f; break;
            case "barracks": w = 32f; d = 20f; hz = 13f; break;
            case "stables": w = 30f; d = 18f; hz = 10f; break;
            case "library": w = 40f; d = 28f; hz = 22f; break;
            case "magetower": w = 18f; d = 18f; hz = 44f; break;
            case "cathedral": w = 28f; d = 64f; hz = 52f; break;
            case "castle": w = 42f; d = 35f; hz = 28f; break;                      // model asli 76 x 64 x 51 diperkecil 0,55
            case "forge": w = 14f; d = 12f; hz = 8f; break;
            case "thc_a": w = 6f; d = 5.2f; hz = 6.8f; break;                      // rumah tanah liat beratap jerami (V2Kit.ThatchA)
            case "thc_b": w = 8f; d = 5.4f; hz = 6.9f; break;                      // (V2Kit.ThatchB)
            case "manor": w = 26f; d = 22f; hz = 15f; break;
            case "farmhouse": w = 12f; d = 9f; hz = 9f; break;
            case "barn": w = 18f; d = 12f; hz = 11f; break;
            case "windmill": w = 10f; d = 10f; hz = 24f; break;
            case "granary": w = 6f; d = 6f; hz = 6f; break;
            case "row_a": w = 6f; d = 9f; hz = 11f; break;
            case "row_b": w = 7f; d = 9.5f; hz = 11.5f; break;
            case "twn_a": w = 8.4f; d = 10f; hz = 10.5f; break;
            case "twn_b": w = 9.6f; d = 9f; hz = 12f; break;
            case "shop_a": w = 9f; d = 9.5f; hz = 10f; break;
            case "cot_a": w = 7f; d = 7.5f; hz = 7.5f; break;
            case "corner": w = 10.5f; d = 10.5f; hz = 12.5f; break;
            case "villa": w = 11.5f; d = 10f; hz = 11f; break;
            case "store": w = 12f; d = 9f; hz = 9f; break;
            case "workshop": w = 9f; d = 8f; hz = 7.5f; break;
            default: w = 8f; d = 8f; hz = 8f; break;
        }
    }

    static Lot Add(string id, string kind, float x, float z, float yaw, int v = 0, string label = null, string zone = "bawah", float blend = 3f)
    {
        float w, d, hz; Size(kind, out w, out d, out hz);
        var l = new Lot { id = id, kind = kind, x = x, z = z, w = w, d = d, hz = hz, yaw = yaw, v = v, label = label, zone = zone, blend = blend };
        Lots.Add(l);
        return l;
    }

    static Lot AddFacing(string id, string kind, float x, float z, float fx, float fz, int v = 0, string label = null, string zone = "bawah", float blend = 3f)
    {
        return Add(id, kind, x, z, YawTo(x, z, fx, fz), v, label, zone, blend);
    }

    static Lot Area(string id, string kind, float x, float z, float w, float d, float yaw, float blend = 3f, bool pad = true)
    {
        var l = new Lot { id = id, kind = kind, x = x, z = z, w = w, d = d, yaw = yaw, blend = blend, pad = pad, zone = "tani" };
        Lots.Add(l);
        return l;
    }

    static Road Road_(string id, float width, int mat, int sub, bool lamps, params float[] xz)
    {
        var p = new Vector2[xz.Length / 2];
        for (int i = 0; i < p.Length; i++) p[i] = new Vector2(xz[i * 2], xz[i * 2 + 1]);
        var r = new Road { id = id, width = width, mat = mat, lamps = lamps, pts = Smooth(p, sub) };
        Roads.Add(r);
        return r;
    }

    public static Road RoadOf(string id) { foreach (var r in Roads) if (r.id == id) return r; return null; }

    static Plaza Plaza_(string id, string kind, float x, float z, float w, float d, float yaw, bool round = false)
    {
        var p = new Plaza { id = id, kind = kind, x = x, z = z, w = w, d = d, yaw = yaw, round = round };
        Plazas.Add(p);
        return p;
    }

    /// <summary>Titik di depan sisi plaza persegi: side 0 = utara (bangunan menghadap selatan), 1 = timur (hadap barat), 2 = selatan (hadap utara), 3 = barat (hadap timur).
    /// along = geseran sepanjang sisi; kembalikan pusat bangunan yang mukanya berjarak gap dari tepi plaza.</summary>
    public static Vector2 Around(Plaza p, int side, float along, float gap, float depth, out float yaw)
    {
        float lx, lz;
        switch (side)
        {
            case 0: lx = along; lz = p.d * 0.5f + gap + depth * 0.5f; yaw = 180f; break;
            case 1: lx = p.w * 0.5f + gap + depth * 0.5f; lz = along; yaw = 270f; break;
            case 2: lx = along; lz = -(p.d * 0.5f + gap + depth * 0.5f); yaw = 0f; break;
            default: lx = -(p.w * 0.5f + gap + depth * 0.5f); lz = along; yaw = 90f; break;
        }
        yaw += p.yaw;
        return ToWorld(p.x, p.z, p.yaw, lx, lz);
    }

    static Plaza PlazaOf(string id) { foreach (var p in Plazas) if (p.id == id) return p; return null; }

    public static Lot LotOf(string id) { foreach (var l in Lots) if (l.id == id) return l; return null; }

    // ------------------------------------------------------------------ jembatan
    /// <summary>Jembatan di tempat jalan memotong sungai (lengkung batu).</summary>
    static void MakeBridge(string id, Road r, float W, bool wide)
    {
        float total = Len(r.pts);
        float d0 = -1f, d1 = -1f;
        for (float d = 0f; d <= total; d += 0.5f)
        {
            Vector2 tg; Vector2 p = At(r.pts, d, out tg);
            float wl = RiverHalfWidth(p.x, p.y) * 0.62f + 3.0f;                     // setengah lebar permukaan air
            if (DistPoly(p.x, p.y, River) < wl + 0.8f) { if (d0 < 0f) d0 = d; d1 = d; }
        }
        if (d0 < 0f) return;
        Vector2 tan; Vector2 c = At(r.pts, (d0 + d1) * 0.5f, out tan);
        float span = d1 - d0;
        var br = new Bridge { id = id, c = c, W = W, wide = wide, yaw = Mathf.Atan2(tan.x, tan.y) * Mathf.Rad2Deg };
        br.chord = Mathf.Max(12f, span + 3f);
        br.L = br.chord + 10f;
        br.y = 0.6f;
        Bridges.Add(br);
        for (int sg = -1; sg <= 1; sg += 2)
        {
            Vector2 e = c + tan * (sg * (br.L * 0.5f + 2f));
            var pad = Area(id + (sg < 0 ? "_padA" : "_padB"), "bridgepad", e.x, e.y, W + 4f, 7f, br.yaw, 4f, true);
            pad.py = br.y;
        }
    }

    /// <summary>Jembatan batu datar di atas parit di depan sebuah gerbang: dari muka gerbang (jarak a dari garis tembok) sampai b, searah luar gerbang az.</summary>
    static void MakeMoatBridge(string id, Vector2 gate, float az, float a, float b, float W)
    {
        float r = az * Mathf.Deg2Rad;
        Vector2 o = new Vector2(Mathf.Sin(r), Mathf.Cos(r));
        var br = new Bridge { id = id, kind = "parit", c = gate + o * ((a + b) * 0.5f), gate = gate, yaw = az, L = b - a, W = W, wide = false, y = 0f, chord = b - a, depth = W > 9f ? DitchDepth : KDitchDepth };
        Bridges.Add(br);
    }

    // ------------------------------------------------------------------ pengisi rumah otomatis di sepanjang jalan
    // Kerapatan kota abad pertengahan: jarak antar rumah hampir nol (dinding bersama), jarak ke jalan kecil.
    static float gapMin = 0.15f, gapRnd = 0.55f;
    static float clrWall = 8.4f, clrChord = 8.0f, clrRiver = 4.5f, slopeTol = 1.7f, clrRoad = 0.7f, clrLot = 0.3f;

    static bool CornersOk(Lot c, System.Func<float, float, bool> allow)
    {
        for (int i = 0; i < 9; i++)
        {
            float lx = (i % 3 - 1) * 0.5f * c.w, lz = (i / 3 - 1) * 0.5f * c.d;
            Vector2 p = ToWorld(c, lx, lz);
            if (allow != null && !allow(p.x, p.y)) return false;
        }
        return true;
    }

    static bool LotClear(Lot c, string roadId, float setback)
    {
        // sampel 5x5 tepi + isi
        float baseC = Base(c.x, c.z);
        for (int i = 0; i < 25; i++)
        {
            float lx = (i % 5 - 2) * 0.25f * c.w, lz = (i / 5 - 2) * 0.25f * c.d;
            Vector2 p = ToWorld(c, lx, lz);
            // sungai (tepian promenade)
            if (DistPoly(p.x, p.y, River) < RiverHalfWidth(p.x, p.y) + clrRiver + 3f) return false;
            // tembok + parit tiga cincin + tembok pembatas kastil
            if (InWallBand(p.x, p.y, clrWall, 3f)) return false;
            if (DistPoly(p.x, p.y, Chord) < clrChord) return false;
            // jalan lain
            foreach (var r in Roads)
            {
                if (r.dead) continue;
                float dd = DistPoly(p.x, p.y, r.pts) - r.width * 0.5f;
                if (r.id == roadId) { if (dd < setback - 0.45f) return false; }
                else if (dd < clrRoad) return false;
            }
            // plaza
            foreach (var pl in Plazas)
            {
                float dd = pl.round ? Mathf.Sqrt((p.x - pl.x) * (p.x - pl.x) + (p.y - pl.z) * (p.y - pl.z)) - pl.w * 0.5f : DistToRect(p.x, p.y, pl.x, pl.z, pl.yaw, pl.w, pl.d);
                if (dd < 1.0f) return false;
            }
            // zona terlarang (sawah, taman)
            foreach (var zn in Zones) if (zn.kind != "ok" && Inside(p.x, p.y, zn.poly)) return false;
            // lereng
            if (Mathf.Abs(Base(p.x, p.y) - baseC) > slopeTol) return false;
        }
        foreach (var o in Bridges) if ((o.c - c.P).magnitude < o.L * 0.5f + 12f) return false;
        foreach (var o in Lots)
        {
            if (o.kind == "bridgepad") continue;
            float rr = (o.w > o.d ? o.w : o.d) + (c.w > c.d ? c.w : c.d);
            if (Mathf.Abs(o.x - c.x) > rr || Mathf.Abs(o.z - c.z) > rr) continue;
            if (RectsOverlap(c.x, c.z, c.yaw, c.w, c.d, o.x, o.z, o.yaw, o.w, o.d, o.kind == "field" || o.kind == "yard" || o.kind == "park" ? 0.5f : clrLot)) return false;
        }
        return true;
    }

    /// <summary>Isi satu sisi jalan dengan rumah acak. side +1 = kanan arah jalan, -1 = kiri.</summary>
    static int Fill(string roadId, int side, float t0, float t1, float setback, string[] kinds, string zone, float skip, System.Func<float, float, bool> allow)
    {
        var r = RoadOf(roadId);
        if (r == null) return 0;
        float total = Len(r.pts);
        if (t1 < 0f || t1 > total) t1 = total;
        int made = 0;
        float s = t0;
        int guard = 0;
        while (s < t1 - 3f && guard++ < 600)
        {
            string kind = kinds[frng.Next(kinds.Length)];
            float w, d, hz; Size(kind, out w, out d, out hz);
            float sc = s + w * 0.5f + 0.3f;
            if (sc > t1) break;
            Vector2 tg; Vector2 p = At(r.pts, sc, out tg);
            Vector2 nrm = new Vector2(tg.y, -tg.x) * side;
            Vector2 c = p + nrm * (r.width * 0.5f + setback + d * 0.5f);
            var lot = new Lot { id = "h" + Lots.Count, kind = kind, w = w, d = d, hz = hz, zone = zone, v = frng.Next(1000), x = c.x, z = c.y, yaw = YawTo(c.x, c.y, p.x, p.y) + ((float)frng.NextDouble() - 0.5f) * 3f };
            bool ok = frng.NextDouble() >= skip && CornersOk(lot, allow) && LotClear(lot, roadId, setback);
            if (ok)
            {
                lot.py = Base(lot.x, lot.z);
                Lots.Add(lot);
                made++;
                s += w + gapMin + (float)frng.NextDouble() * gapRnd;
            }
            else s += 2.4f;
        }
        return made;
    }

    public static float ArcAt(string roadId, float x, float z) { return ArcOf(x, z, RoadOf(roadId).pts); }

    static void FillBoth(string road, float t0, float t1, float setback, string[] kinds, string zone, float skip, System.Func<float, float, bool> allow)
    {
        Fill(road, 1, t0, t1, setback, kinds, zone, skip, allow);
        Fill(road, -1, t0, t1, setback, kinds, zone, skip, allow);
    }

    // ------------------------------------------------------------------ pertumbuhan jalan (cabang acak terarah)
    static bool SpurOk(Vector2 p, Road parent, float distFromStart, float width)
    {
        if (!Inside(p.x, p.y, OuterPoly)) return false;
        if (InWallBand(p.x, p.y, clrWall + width * 0.5f + 1.5f, 5f + width * 0.5f)) return false;
        if (DistPoly(p.x, p.y, Chord) < clrChord + width * 0.5f) return false;
        if ((spurMask & (1 << RingAt(p.x, p.y))) == 0) return false;
        if (DistPoly(p.x, p.y, River) < RiverHalfWidth(p.x, p.y) + 7f) return false;
        foreach (var pl in Plazas)
        {
            float dd = pl.round ? Mathf.Sqrt((p.x - pl.x) * (p.x - pl.x) + (p.y - pl.z) * (p.y - pl.z)) - pl.w * 0.5f : DistToRect(p.x, p.y, pl.x, pl.z, pl.yaw, pl.w, pl.d);
            if (dd < 1.5f) return false;
        }
        foreach (var l in Lots)
        {
            if (l.kind == "bridgepad" || l.kind == "gatepad" || l.kind == "field" || l.kind == "komplek") { if (l.kind != "komplek") continue; }
            if (Mathf.Abs(p.x - l.x) > 40f || Mathf.Abs(p.y - l.z) > 40f) continue;
            if (DistToRect(p.x, p.y, l) < width * 0.5f + 2.2f) return false;
        }
        foreach (var zn in Zones) if (zn.kind != "ok" && Inside(p.x, p.y, zn.poly)) return false;
        foreach (var br in Bridges) if ((br.c - p).magnitude < br.L * 0.5f + 10f) return false;
        return true;
    }

    /// <summary>Cabang dari jalan 'parent' di jarak d (side +1 = ke kanan), sudut angle derajat dari arah jalan, berbelok turn derajat per 30 m.
    /// Berhenti bila membentur sesuatu; bila mendekati jalan lain, disambung (pertigaan).</summary>
    static Road Spur(string id, Road parent, float d, int side, float angle, float maxLen, float turn, float width, int mat, bool lamps, float minLen = 16f)
    {
        Vector2 tg; Vector2 p0 = At(parent.pts, d, out tg);
        float head = Mathf.Atan2(tg.x, tg.y) + side * angle * Mathf.Deg2Rad;
        var pts = new List<Vector2> { p0 };
        Vector2 cur = p0;
        float step = 3.5f, run = 0f;
        float tr = turn * Mathf.Deg2Rad / 30f * step;
        bool joined = false;
        while (run < maxLen)
        {
            head += tr + ((float)frng.NextDouble() - 0.5f) * 0.02f;
            Vector2 next = cur + new Vector2(Mathf.Sin(head), Mathf.Cos(head)) * step;
            run += step;
            if (!SpurOk(next, parent, run, width)) break;
            // jalan lain di dekatnya -> sambung
            Road hit = null; Vector2 hp = Vector2.zero;
            foreach (var r in Roads)
            {
                if (r.dead) continue;
                if (r == parent && run < 14f) continue;
                Vector2 tn; Vector2 q = NearestOnPoly(next.x, next.y, r.pts, out tn);
                float dd = (q - next).magnitude - r.width * 0.5f;
                if (dd < width * 0.5f + 3.2f) { hit = r; hp = q; break; }
            }
            if (hit != null && run > 10f)
            {
                pts.Add(hp);
                joined = true;
                break;
            }
            else if (hit != null) break;
            pts.Add(next);
            cur = next;
        }
        float len = Len(pts);
        if (len < minLen || pts.Count < 3) return null;
        var rd = new Road { id = id, width = width, mat = mat, lamps = lamps, pts = pts };
        if (!joined && pts.Count > 3) rd.pts = pts;
        Roads.Add(rd);
        return rd;
    }

    /// <summary>Hamparan cabang sepanjang jalan induk setiap 'every' m (selang-seling sisi), panjang acak lenMin..lenMax.</summary>
    static int Spurs(string prefix, string parentId, float from, float to, float every, int firstSide, float lenMin, float lenMax, float width, int mat, bool lamps)
    {
        var par = RoadOf(parentId);
        if (par == null) return 0;
        float total = Len(par.pts);
        if (to < 0f || to > total) to = total;
        int n = 0, side = firstSide;
        for (float d = from; d < to; d += every * (0.85f + (float)frng.NextDouble() * 0.3f))
        {
            float ang = 78f + (float)frng.NextDouble() * 24f;
            float turn = ((float)frng.NextDouble() - 0.5f) * 24f;
            var r = Spur(prefix + n, par, d, side, ang, lenMin + (float)frng.NextDouble() * (lenMax - lenMin), turn, width, mat, lamps);
            if (r != null) n++;
            side = -side;
        }
        return n;
    }

    /// <summary>Poligon digeser ke dalam (ccw: kiri) sejauh dist, sudut dengan miter dibatasi.</summary>
    static List<Vector2> Inset(IList<Vector2> poly, float dist)
    {
        int n = poly.Count;
        var res = new List<Vector2>();
        for (int i = 0; i < n; i++)
        {
            Vector2 a = poly[(i + n - 1) % n], b = poly[i], c = poly[(i + 1) % n];
            Vector2 u = (b - a).normalized, v = (c - b).normalized;
            Vector2 nu = new Vector2(-u.y, u.x), nv = new Vector2(-v.y, v.x);
            Vector2 m = nu + nv;
            float ml = m.magnitude;
            if (ml < 0.1f) { res.Add(b + nu * dist); continue; }
            m /= ml;
            float cs = Mathf.Max(0.55f, Vector2.Dot(m, nu));
            res.Add(b + m * (dist / cs));
        }
        return res;
    }

    /// <summary>Jalan lingkar di dalam sebuah tembok (poligon poly, ke dalam sejauh dist). Bagian yang dekat sungai, gerbang, jalan lain, atau bangunan dipotong.</summary>
    static void RingLane(List<Vector2> poly, float dist, float width, string prefix)
    {
        var ins = Inset(poly, dist);
        // resampel tiap ~5 m
        var dense = new List<Vector2>();
        for (int i = 0; i < ins.Count; i++)
        {
            Vector2 a = ins[i], b = ins[(i + 1) % ins.Count];
            int m = Mathf.Max(1, Mathf.CeilToInt((b - a).magnitude / 5f));
            for (int k = 0; k < m; k++) dense.Add(Vector2.Lerp(a, b, k / (float)m));
        }
        int nn = dense.Count;
        var bad = new bool[nn];
        for (int i = 0; i < nn; i++)
        {
            Vector2 p = dense[i];
            if (DistPoly(p.x, p.y, River) < RiverHalfWidth(p.x, p.y) + 9f) bad[i] = true;
            if (InWallBand(p.x, p.y, 3f, 1f)) bad[i] = true;                  // tidak menyentuh pita tembok/parit cincin lain
            foreach (var g in Gates) if ((g.p - p).magnitude < 15f) bad[i] = true;
            if (!bad[i] && NearBuilt(p, 1.6f)) bad[i] = true;
            if (!bad[i])
                foreach (var r in Roads)
                {
                    if (r.dead || r.id.StartsWith(prefix)) continue;
                    if (DistPoly(p.x, p.y, r.pts) < r.width * 0.5f + width * 0.5f + 3.5f) { bad[i] = true; break; }
                }
        }
        int start = 0;
        for (int i = 0; i < nn; i++) if (bad[i]) { start = i; break; }
        int idx = 0;
        var cur = new List<Vector2>();
        for (int k = 1; k <= nn; k++)
        {
            int i = (start + k) % nn;
            if (!bad[i]) cur.Add(dense[i]);
            if (bad[i] || k == nn)
            {
                if (cur.Count >= 5)
                    Roads.Add(new Road { id = prefix + (idx++), width = width, mat = 2, lamps = false, pts = new List<Vector2>(cur) });
                cur = new List<Vector2>();
            }
        }
    }

    // ------------------------------------------------------------------ tiga cincin tembok konsentris
    public class RingInfo { public List<Vector2> p; public float cx, cz, rmin, rmax; }
    static RingInfo ringO, ringM, ringI;
    /// <summary>Cincin tempat gang boleh tumbuh (bit 0 = ladang, 1 = kota tengah, 2 = kota dalam).</summary>
    static int spurMask = 2;
    static float farmRoadOff = 38f;                 // jalan tani melingkar: jarak ke luar dari Tembok Tengah

    /// <summary>Poligon tertutup berlawanan arah jarum jam mulai dari titik selatan; empat jari-jari (timur, barat, utara, selatan); simpangan kecil nol di empat arah mata angin (gerbang).</summary>
    static RingInfo MakeRing(List<Vector2> dst, float cx, float cz, float rE, float rW, float rN, float rS, int n, float wob, float ph)
    {
        dst.Clear();
        for (int k = 0; k < n; k++)
        {
            float f = -Mathf.PI * 0.5f + k * Mathf.PI * 2f / n;
            float c = Mathf.Cos(f), s = Mathf.Sin(f);
            float s2 = Mathf.Sin(2f * f); s2 *= s2;
            float w = wob * s2 * (0.6f * Mathf.Sin(3f * f + ph) + 0.4f * Mathf.Sin(5f * f + ph * 1.7f));
            dst.Add(new Vector2(cx + c * (c > 0f ? rE : rW) * (1f + w), cz + s * (s > 0f ? rN : rS) * (1f + w)));
        }
        var ri = new RingInfo { p = dst, cx = cx, cz = cz, rmin = 1e9f, rmax = 0f };
        foreach (var q in dst) { float d = (q - new Vector2(cx, cz)).magnitude; ri.rmin = Mathf.Min(ri.rmin, d); ri.rmax = Mathf.Max(ri.rmax, d); }
        ri.rmin -= 3f; ri.rmax += 3f;
        return ri;
    }

    static float SdRing(RingInfo r, float x, float z, float cap)
    {
        float dx = x - r.cx, dz = z - r.cz, d = Mathf.Sqrt(dx * dx + dz * dz);
        if (d > r.rmax + cap) return cap;
        if (d < r.rmin - cap) return -cap;
        return Mathf.Clamp(SignedDist(x, z, r.p), -cap, cap);
    }

    /// <summary>Jarak bertanda ke Tembok Luar / Tengah / Dalam (negatif di dalam), dibatasi agar murah dihitung jauh dari tembok.</summary>
    public static float SdOuter(float x, float z) { if (ringO == null) Build(); return SdRing(ringO, x, z, 140f); }
    public static float SdMid(float x, float z) { if (ringM == null) Build(); return SdRing(ringM, x, z, 70f); }
    public static float SdInner(float x, float z) { if (ringI == null) Build(); return SdRing(ringI, x, z, 110f); }

    /// <summary>-1 di luar Tembok Luar, 0 ladang (antara Tembok Luar dan Tengah), 1 kota tengah, 2 kota dalam (di dalam Tembok Dalam).</summary>
    public static int RingAt(float x, float z)
    {
        if (!Inside(x, z, OuterPoly)) return -1;
        if (!Inside(x, z, MidPoly)) return 0;
        if (!Inside(x, z, InnerPoly)) return 1;
        return 2;
    }

    public static string ZoneAt(float x, float z)
    {
        int r = RingAt(x, z);
        return r == 2 ? "atas" : (r == 1 ? "bawah" : "tani");
    }

    /// <summary>Titik berada di pita tembok atau parit manapun: dari 'inner' m di dalam garis tembok sampai 'outer' m di luar tepi jauh parit.</summary>
    public static bool InWallBand(float x, float z, float inner, float outer)
    {
        float so = SdOuter(x, z); if (so > -inner && so < DitchFarEdge + outer) return true;
        float sm = SdMid(x, z); if (sm > -inner && sm < DitchFarEdge + outer) return true;
        float si = SdInner(x, z); if (si > -inner && si < KDitchFarEdge + outer) return true;
        return false;
    }

    // ------------------------------------------------------------------ bangun data
    public static void Build()
    {
        if (built) return;
        built = true;
        Walls.Clear(); Gates.Clear(); Roads.Clear(); Plazas.Clear(); Bridges.Clear(); Lots.Clear(); Zones.Clear(); Stalls.Clear(); StallYaw.Clear(); WaterArches.Clear();
        Banners.Clear(); Statues.Clear(); HayAt.Clear(); Herds.Clear(); Pts.Clear();
        OuterPoly.Clear(); MidPoly.Clear(); InnerPoly.Clear(); Chord.Clear(); OutRoads.Clear(); Terraces.Clear();
        frng = new System.Random(20261181);
        spurMask = 2;

        // ---- Tiga tembok konsentris: luar (paling rendah, mengurung ladang), tengah (kota atap merah), dalam (di bukit mahkota)
        ringO = MakeRing(OuterPoly, 0f, -20f, 310f, 310f, 310f, 310f, 80, 0.028f, 0.9f);
        ringM = MakeRing(MidPoly, 0f, -5f, 220f, 220f, 225f, 141f, 72, 0.022f, 2.1f);
        ringI = MakeRing(InnerPoly, 0f, 32f, 108f, 108f, 112f, 100f, 56, 0.016f, 4.0f);
        // ---- Tembok pembatas halaman kastil (garis terbuka melintang di belakang Katedral, ujungnya menyatu dengan Tembok Dalam)
        {
            const float cz = 90.5f;
            float xl = 1e9f, xr = -1e9f;
            for (int i = 0; i < InnerPoly.Count; i++)
            {
                Vector2 a = InnerPoly[i], b = InnerPoly[(i + 1) % InnerPoly.Count];
                if ((a.y - cz) * (b.y - cz) < 0f)
                {
                    float x = a.x + (b.x - a.x) * (cz - a.y) / (b.y - a.y);
                    xl = Mathf.Min(xl, x); xr = Mathf.Max(xr, x);
                }
            }
            xl -= 1.5f; xr += 1.5f;
            int n = 14;
            for (int i = 0; i <= n; i++) Chord.Add(new Vector2(Mathf.Lerp(xl, xr, i / (float)n), cz + 0.9f * Mathf.Sin(i * 1.9f) * (i == 0 || i == n ? 0f : 1f)));
        }

        Walls.Add(new Wall { id = "luar", tier = 0, closed = true, pts = new List<Vector2>(OuterPoly), thick = 7.4f, height = 14f });
        Walls.Add(new Wall { id = "tengah", tier = 1, closed = true, pts = new List<Vector2>(MidPoly), thick = 6.6f, height = 17.5f });
        Walls.Add(new Wall { id = "dalam", tier = 2, closed = true, pts = new List<Vector2>(InnerPoly), thick = 6.4f, height = 21f });
        Walls.Add(new Wall { id = "kastil", tier = 2, closed = false, pts = new List<Vector2>(Chord), thick = 5.6f, height = 16f });

        // ---- dua belas gerbang (empat per tembok) + gerbang kastil (tembok pembatas, bukan hitungan kanon)
        Gates.Add(new Gate { id = "Gerbang_Luar_Selatan", dir = "Selatan", tier = 0, p = GateS, az = 180f, hook = "main", signOut = "GERBANG LUAR SELATAN", signIn = "KOTA ASHWATCH" });
        Gates.Add(new Gate { id = "Gerbang_Luar_Timur", dir = "Timur", tier = 0, p = GateE, az = 90f, hook = "east", signOut = "GERBANG LUAR TIMUR", signIn = "KOTA ASHWATCH" });
        Gates.Add(new Gate { id = "Gerbang_Luar_Barat", dir = "Barat", tier = 0, p = GateW, az = 270f, hook = "west", signOut = "GERBANG LUAR BARAT", signIn = "KOTA ASHWATCH" });
        Gates.Add(new Gate { id = "Gerbang_Luar_Utara", dir = "Utara", tier = 0, p = GateN, az = 0f, hook = "north", signOut = "GERBANG LUAR UTARA", signIn = "KOTA ASHWATCH" });
        Gates.Add(new Gate { id = "Gerbang_Tengah_Selatan", dir = "Selatan", tier = 1, p = new Vector2(0f, -146f), az = 180f, hook = "", signOut = "GERBANG TENGAH", signIn = "KOTA TENGAH" });
        Gates.Add(new Gate { id = "Gerbang_Tengah_Timur", dir = "Timur", tier = 1, p = new Vector2(220f, -5f), az = 90f, hook = "", signOut = "GERBANG TENGAH TIMUR", signIn = "KOTA TENGAH" });
        Gates.Add(new Gate { id = "Gerbang_Tengah_Barat", dir = "Barat", tier = 1, p = new Vector2(-220f, -5f), az = 270f, hook = "", signOut = "GERBANG TENGAH BARAT", signIn = "KOTA TENGAH" });
        Gates.Add(new Gate { id = "Gerbang_Tengah_Utara", dir = "Utara", tier = 1, p = new Vector2(0f, 220f), az = 0f, hook = "", signOut = "GERBANG TENGAH UTARA", signIn = "KOTA TENGAH" });
        Gates.Add(new Gate { id = "Gerbang_Dalam_Selatan", dir = "Selatan", tier = 2, p = new Vector2(0f, -68f), az = 180f, hook = "", signOut = "GERBANG DALAM", signIn = "ALUN-ALUN KATEDRAL" });
        Gates.Add(new Gate { id = "Gerbang_Dalam_Timur", dir = "Timur", tier = 2, p = new Vector2(108f, 32f), az = 90f, hook = "", signOut = "GERBANG DALAM TIMUR", signIn = "ALUN-ALUN KATEDRAL" });
        Gates.Add(new Gate { id = "Gerbang_Dalam_Barat", dir = "Barat", tier = 2, p = new Vector2(-108f, 32f), az = 270f, hook = "", signOut = "GERBANG DALAM BARAT", signIn = "ALUN-ALUN KATEDRAL" });
        Gates.Add(new Gate { id = "Gerbang_Dalam_Utara", dir = "Utara", tier = 2, p = new Vector2(0f, 144f), az = 0f, hook = "level", signOut = "GERBANG DALAM UTARA", signIn = "HALAMAN KASTIL" });
        Gates.Add(new Gate { id = "Gerbang_Kastil", dir = "Selatan", tier = 2, p = new Vector2(28f, 90.5f), az = 180f, hook = "level", signOut = "GERBANG KASTIL", signIn = "KEDIAMAN PENGUASA" });

        // ---- Sungai Abu: masuk dari timur lewat lengkung air, melintang di ladang selatan, keluar ke barat daya (mengisi parit luar)
        River = Smooth(new[]
        {
            new Vector2(340f, -180f), new Vector2(270f, -178f), new Vector2(205f, -172f), new Vector2(160f, -176f), new Vector2(110f, -184f),
            new Vector2(60f, -190f), new Vector2(10f, -190f), new Vector2(-40f, -186f), new Vector2(-90f, -192f), new Vector2(-128f, -210f),
            new Vector2(-154f, -238f), new Vector2(-172f, -272f), new Vector2(-196f, -312f), new Vector2(-232f, -350f), new Vector2(-270f, -380f)
        }, 8);
        rx0 = rz0 = 1e9f; rx1 = rz1 = -1e9f;
        foreach (var q in River) { rx0 = Mathf.Min(rx0, q.x); rx1 = Mathf.Max(rx1, q.x); rz0 = Mathf.Min(rz0, q.y); rz1 = Mathf.Max(rz1, q.y); }
        rx0 -= 40f; rz0 -= 40f; rx1 += 40f; rz1 += 40f;
        // lintasan sungai dengan Tembok Luar (lengkung air di tembok)
        for (int i = 0; i < OuterPoly.Count; i++)
        {
            Vector2 a = OuterPoly[i], b = OuterPoly[(i + 1) % OuterPoly.Count];
            for (int k = 0; k + 1 < River.Count; k++)
            {
                Vector2 c, u = b - a, v = River[k + 1] - River[k];
                float den = u.x * v.y - u.y * v.x;
                if (Mathf.Abs(den) < 1e-5f) continue;
                Vector2 w = River[k] - a;
                float t = (w.x * v.y - w.y * v.x) / den, s = (w.x * u.y - w.y * u.x) / den;
                if (t >= 0f && t <= 1f && s >= 0f && s <= 1f) { c = a + u * t; WaterArches.Add(c); }
            }
        }

        // ---- jalan keluar dari tiap gerbang luar (koridor + pelurus medan)
        OutRoads.Add(new List<Vector2> { new Vector2(0f, -330f), new Vector2(0f, -450f) });
        OutRoads.Add(new List<Vector2> { new Vector2(0f, -378f), new Vector2(-84f, -378f) });
        OutRoads.Add(new List<Vector2> { new Vector2(310f, -20f), new Vector2(480f, -20f) });
        OutRoads.Add(new List<Vector2> { new Vector2(-310f, -20f), new Vector2(-480f, -20f) });
        OutRoads.Add(new List<Vector2> { new Vector2(0f, 290f), new Vector2(0f, 430f) });

        BuildPlazasAndLandmarks();
        BuildRoads();
        BuildBridges();
        BuildRingRoads();
        BuildSites();
        BuildSpurs();
        BuildFields();
        BuildHousing();

        // pad rata di tiap gerbang (lantai gerbang & jalan tidak miring), digeser ke dalam agar parit di luar tidak ikut diratakan.
        // Disisipkan di AWAL daftar agar pad bangunan lain menang bila tumpang tindih.
        var gpads = new List<Lot>();
        foreach (var g in Gates)
        {
            float r = g.az * Mathf.Deg2Rad;
            Vector2 o = new Vector2(Mathf.Sin(r), Mathf.Cos(r));
            float shift = g.tier == 0 ? 6f : (g.tier == 2 ? 8f : 6f);
            Vector2 c = g.p - o * shift;
            gpads.Add(new Lot { id = "pad_" + g.id, kind = "gatepad", x = c.x, z = c.y, w = g.tier == 0 ? 36f : 32f, d = g.tier == 0 ? 20f : 22f, yaw = g.az, blend = g.tier == 2 ? 5f : 6f, pad = true, zone = "tani" });
        }
        Lots.InsertRange(0, gpads);

        // tinggi pad = tinggi dasar medan di tengah
        foreach (var l in Lots) if (l.kind != "bridgepad") l.py = Base(l.x, l.z);
    }

    // ------------------------------------------------------------------ alun-alun & bangunan penting
    static void AddManor(int k, float x, float z, float yaw)
    {
        var l = Add("manor" + k, "manor", x, z, yaw, (k - 1) % 4, null, "kastil", 4f);
        l.label = null;
    }

    static void BuildPlazasAndLandmarks()
    {
        float yaw; Vector2 p;
        // ===== Alun-alun Gerbang (pasar + Guild), ladang selatan tepat di dalam Gerbang Luar Selatan — TIDAK BERUBAH dari n76 (titik muncul cerita bergantung padanya)
        var pg = Plaza_("gerbang", "pasar", 16f, -258f, 70f, 40f, 0f);
        FountainGate = new Vector2(26f, -258f);
        p = Around(pg, 0, 16f, 5f, 30f, out yaw);                     // sisi utara: Guild di timur jalan raya
        Add("guild", "guild", p.x, p.y, yaw, 0, "Guild Petualang", "bawah", 6f);
        p = Around(pg, 0, -30f, 5f, 18f, out yaw);                    // dan penginapan di barat jalan raya
        Add("inn", "inn", p.x, p.y, yaw, 2, "Penginapan", "bawah", 5f);
        p = Around(pg, 1, -15f, 6f, 13f, out yaw);                    // sisi timur: pandai besi + toko umum (gang pasar timur di antaranya)
        Add("smithy", "smithy", p.x, p.y, yaw, 4, "Pandai Besi");
        p = Around(pg, 1, 11f, 6f, 14f, out yaw);
        Add("shop", "shop", p.x, p.y, yaw, 1, "Toko Umum");
        p = Around(pg, 3, 9f, 6f, 15f, out yaw);                      // sisi barat: restoran (gang pasar barat di selatannya)
        Add("tavern", "tavern", p.x, p.y, yaw, 3, "Restoran");
        // barak (barat) & istal (timur) di dekat gerbang selatan
        Plaza_("barak", "halaman", -21f, -301f, 24f, 26f, 0f);
        Add("barracks", "barracks", -45f, -301f, 90f, 0, "Barak Ksatria", "bawah", 4f);
        Plaza_("istal", "halaman", 25f, -301f, 24f, 26f, 0f);
        Add("stables", "stables", 49f, -301f, 270f, 0, "Istal Kota", "bawah", 4f);
        // simpang pasar kecil di kota tengah: raya bertemu Jalan Lintas
        Plaza_("simpang", "pasar", 0f, -106f, 26f, 22f, 0f);

        // ===== Kota dalam, di atas bukit mahkota: alun-alun batu poles tua, Katedral jarum putih di sisi utaranya
        Plaza_("tengah", "air_mancur", PlazaMid.x, PlazaMid.y, 62f, 62f, 0f, true);
        Add("cathedral", "cathedral", 0f, 50f, 180f, 0, "Katedral Cahaya Suci", "atas", 8f);
        Plaza_("akademi", "parvis", 50f, -6f, 16f, 40f, 0f);
        Add("library", "library", 74f, -6f, 270f, 0, "Akademi & Perpustakaan", "atas", 6f);
        Add("magetower", "magetower", 84f, 62f, 180f, 0, "Menara Penyihir", "atas", 5f);     // menghadap selatan; gang 'menara' dari Jalan Timur ke tangganya (n81: dulu menghadap punggung kediaman, tak terjangkau)
        // enam kediaman bangsawan mengapit Katedral dan alun-alun
        AddManor(1, 58f, 43.5f, 270f); AddManor(2, 58f, 70f, 270f);
        AddManor(3, -58f, 43.5f, 90f); AddManor(4, -58f, 70f, 90f);
        AddManor(5, -68f, 0f, 90f); AddManor(6, -56f, -28f, 90f);
        // halaman kastil di balik tembok pembatas: kastil penguasa (model diperkecil 0,55)
        Add("castle", "castle", -18f, 118f, 180f, 0, "Kastil Penguasa", "kastil", 10f);
        // tempa Hadrik (rendah, hitam jelaga) di sudut utara tembok dalam, di kota tengah
        Add("forge", "forge", 40f, 188f, 180f, 0, "Tempa Hadrik", "bawah", 4f);

        var gl = LotOf("guild");
        GuildFront = ToWorld(gl, 0f, gl.d * 0.5f + 6f);        // titik kembali dari interior Guild: 6 m di depan muka podium
        Notice = ToWorld(gl, -12f, gl.d * 0.5f + 4.2f);
    }

    // ------------------------------------------------------------------ jalan utama
    static void BuildRoads()
    {
        // jalan raya: gerbang luar selatan -> alun-alun Gerbang -> jembatan -> gerbang tengah -> simpang -> gerbang dalam -> alun-alun batu poles
        Road_("raya", 8f, 0, 7, true, 0f, -332f, 1f, -306f, 2f, -284f, 3f, -262f, 4f, -242f, 4.5f, -224f, 5f, -206f, 6f, -190f, 6f, -172f, 3f, -158f, 0f, -146f, 0f, -128f, 0f, -106f,
            0f, -86f, 0f, -68f, 0f, -48f);
        // jalan timur dan barat: gerbang luar -> gerbang tengah -> gerbang dalam -> alun-alun
        float[] te = { 312f, -20f, 286f, -18f, 256f, -13f, 232f, -8f, 220f, -5f, 198f, -1f, 174f, 8f, 150f, 22f, 130f, 30f, 108f, 32f, 86f, 29f, 66f, 26f, 48f, 20f, 34f, 10f, 28f, -2f };
        var tb = new float[te.Length];
        for (int i = 0; i < te.Length; i += 2) { tb[i] = -te[i]; tb[i + 1] = te[i + 1]; }
        Road_("timur", 7f, 0, 7, true, te);
        Road_("barat", 7f, 0, 7, true, tb);
        // jalan utara: gerbang luar -> gerbang tengah -> gerbang dalam utara (halaman kastil)
        Road_("utara", 6.4f, 0, 7, true, 0f, 292f, 0f, 262f, 0f, 240f, 0f, 220f, 0f, 200f, 0f, 182f, 0f, 162f, 0f, 144f);
        Road_("utara_kastil", 5.2f, 2, 5, false, 0f, 144f, 0f, 140f, 8f, 137.5f, 19f, 133f, 25f, 122f, 26f, 110f, 27f, 97f, 28f, 93f);
        // Gerbang Kastil -> pintu kastil
        Road_("kastil", 6f, 2, 5, false, 28f, 93f, 20f, 96.5f, 6f, 97f, -18f, 96.5f);
        // jalan pengapit Katedral (dari jalan timur / barat ke Gerbang Kastil)
        Road_("flank_t", 5.6f, 0, 6, true, 48f, 20f, 42f, 28f, 36f, 42f, 34f, 60f, 30f, 78f, 28f, 88f);
        Road_("flank_b", 5.6f, 0, 6, true, -48f, 20f, -42f, 28f, -36f, 42f, -34f, 60f, -30f, 78f, -28f, 86f);
        // gang pasar dari alun-alun Gerbang ke ladang barat dan timur
        Road_("pasar_barat", 5.6f, 0, 7, false, -19f, -268f, -50f, -269f, -84f, -264f, -112f, -254f, -130f, -244f);
        Road_("pasar_timur", 5.6f, 0, 7, false, 51f, -260f, 80f, -261f, 110f, -257f, 142f, -251f, 168f, -244f, 178f, -240f);
    }
    static bool NearBuilt(Vector2 p, float m)
    {
        foreach (var l in Lots)
        {
            if (l.kind == "bridgepad" || l.kind == "gatepad" || l.kind == "field") continue;
            if (Mathf.Abs(p.x - l.x) > 50f || Mathf.Abs(p.y - l.z) > 50f) continue;
            if (DistToRect(p.x, p.y, l) < m) return true;
        }
        foreach (var pl in Plazas)
        {
            float dd = pl.round ? Mathf.Sqrt((p.x - pl.x) * (p.x - pl.x) + (p.y - pl.z) * (p.y - pl.z)) - pl.w * 0.5f : DistToRect(p.x, p.y, pl.x, pl.z, pl.yaw, pl.w, pl.d);
            if (dd < m) return true;
        }
        return false;
    }

    // ------------------------------------------------------------------ jembatan & lahan tetap
    static void BuildBridges()
    {
        Area("komplek", "komplek", -176f, 54f, 50f, 52f, 180f, 4f, true);
        Road_("komplek_akses", 6f, 0, 4, false, -176f, 6f, -176f, 16f, -176f, 27f);
        MakeBridge("jembatan_raya", RoadOf("raya"), 8.6f, true);
        foreach (var g in Gates)
        {
            if (g.id == "Gerbang_Kastil") continue;                                   // tembok pembatas tidak berparit
            if (g.tier <= 1) MakeMoatBridge("jembatan_" + g.id, g.p, g.az, 4.6f, DitchFarEdge + 3f, 9.2f);
            else MakeMoatBridge("jembatan_" + g.id, g.p, g.az, 3.8f, KDitchFarEdge + 2.5f, 8.4f);
        }
    }

    // ------------------------------------------------------------------ jalan lingkar
    static List<Vector2> DenseRing(IList<Vector2> ring, float step)
    {
        var d = new List<Vector2>();
        for (int i = 0; i < ring.Count; i++)
        {
            Vector2 a = ring[i], b = ring[(i + 1) % ring.Count];
            int m = Mathf.Max(1, Mathf.CeilToInt((b - a).magnitude / step));
            for (int k = 0; k < m; k++) d.Add(Vector2.Lerp(a, b, k / (float)m));
        }
        return d;
    }

    /// <summary>Jalan lingkar kaki bukit (Jalan Lintas = setengah selatan, Lingkar Utara), jalan peronda dalam Tembok Tengah, dan jalan tani melingkar di ladang.</summary>
    static void BuildRingRoads()
    {
        var kr = DenseRing(Inset(InnerPoly, -38f), 5f);
        int M = kr.Count, kE = 0, kN = 0, kW = 0;
        for (int k = 0; k < M; k++)
        {
            if (kr[k].x > kr[kE].x) kE = k;
            if (kr[k].y > kr[kN].y) kN = k;
            if (kr[k].x < kr[kW].x) kW = k;
        }
        var lin = new List<Vector2>(); var lut = new List<Vector2>();
        for (int k = kW; k < M; k++) lin.Add(kr[k]);
        for (int k = 0; k <= kE; k++) lin.Add(kr[k]);
        for (int k = kE; k <= kW; k++) lut.Add(kr[k]);
        Roads.Add(new Road { id = "lintas", width = 7f, mat = 0, lamps = true, pts = lin });
        Roads.Add(new Road { id = "lingkar_utara", width = 6.4f, mat = 0, lamps = true, pts = lut });

        // jalan peronda di dalam Tembok Tengah
        RingLane(MidPoly, 10.5f, 4.6f, "benteng");

        // jalan tani melingkar mengelilingi Tembok Tengah di ladang (terputus di sungai dan di selatan)
        var fr = DenseRing(Inset(MidPoly, -farmRoadOff), 5f);
        int nn = fr.Count;
        var bad = new bool[nn];
        for (int i = 0; i < nn; i++)
        {
            Vector2 p = fr[i];
            if (p.y < -148f) bad[i] = true;
            if (DistPoly(p.x, p.y, River) < RiverHalfWidth(p.x, p.y) + 10f) bad[i] = true;
            if (InWallBand(p.x, p.y, 3f, 1f)) bad[i] = true;
            if (RingAt(p.x, p.y) != 0) bad[i] = true;
        }
        int start = 0;
        for (int i = 0; i < nn; i++) if (bad[i]) { start = i; break; }
        int idx = 0;
        var cur = new List<Vector2>();
        for (int k = 1; k <= nn; k++)
        {
            int i = (start + k) % nn;
            if (!bad[i]) cur.Add(fr[i]);
            if (bad[i] || k == nn)
            {
                if (cur.Count >= 6) Roads.Add(new Road { id = "tani_cincin" + (idx++), width = 5.5f, mat = 1, lamps = false, pts = new List<Vector2>(cur) });
                cur = new List<Vector2>();
            }
        }
    }

    // ------------------------------------------------------------------ gang berliku di kota tengah
    static void BuildSpurs()
    {
        spurMask = 2;
        int g0 = Roads.Count;
        Spurs("sp_ra", "raya", ArcAt("raya", 0f, -142f), ArcAt("raya", 0f, -72f), 22f, 1, 20f, 44f, 4.6f, 0, false);
        Spurs("sp_li", "lintas", 10f, -1f, 24f, 1, 20f, 48f, 4.6f, 0, false);
        Spurs("sp_lu", "lingkar_utara", 10f, -1f, 24f, -1, 20f, 44f, 4.6f, 0, false);
        Spurs("sp_ti", "timur", ArcAt("timur", 216f, -4f), ArcAt("timur", 112f, 31f), 24f, 1, 20f, 48f, 4.6f, 0, false);
        Spurs("sp_ba", "barat", ArcAt("barat", -216f, -4f), ArcAt("barat", -112f, 31f), 24f, -1, 20f, 48f, 4.6f, 0, false);
        Spurs("sp_ut", "utara", ArcAt("utara", 0f, 216f), ArcAt("utara", 0f, 146f), 24f, 1, 20f, 44f, 4.6f, 0, false);
        var snap = new List<Road>(Roads);
        foreach (var r in snap) if (r.id.StartsWith("benteng")) Spurs("sp_" + r.id, r.id, 8f, -1f, 26f, 1, 18f, 44f, 4.4f, 0, false);
        // generasi kedua dan ketiga: gang kecil dari gang (membuat blok berpetak tidak beraturan)
        int g1 = Roads.Count;
        for (int i = g0; i < g1; i++) SubSpurs(Roads[i], "c", 0.85, i);
        int g2 = Roads.Count;
        for (int i = g1; i < g2; i++) SubSpurs(Roads[i], "d", 0.6, i);
        Infill(90, 24f, 4.4f);
    }
    // ------------------------------------------------------------------ gang pengisi: titik terjauh dari jalan ditarik gang baru
    static float igx0 = -228f, igz0 = -152f, igc = 6f;
    static int ign, igm;
    static float[] igd;
    static bool[] igok;

    static Road LaneTo(string id, Vector2 p0, Vector2 target, Road parent, float width)
    {
        var pts = new List<Vector2> { p0 };
        Vector2 cur = p0;
        Vector2 ptg; NearestOnPoly(p0.x, p0.y, parent.pts, out ptg);
        Vector2 nrm = new Vector2(ptg.y, -ptg.x);
        if (Vector2.Dot(nrm, target - p0) < 0f) nrm = -nrm;
        float head = Mathf.Atan2(nrm.x, nrm.y);
        float step = 3.5f, run = 0f;
        while (run < 96f)
        {
            Vector2 toT = target - cur;
            if (toT.magnitude < 4f) break;
            float want = Mathf.Atan2(toT.x, toT.y);
            float dh = Mathf.DeltaAngle(head * Mathf.Rad2Deg, want * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            head += Mathf.Clamp(dh, -0.09f, 0.09f) + ((float)frng.NextDouble() - 0.5f) * 0.05f;
            Vector2 next = cur + new Vector2(Mathf.Sin(head), Mathf.Cos(head)) * step;
            run += step;
            if (!SpurOk(next, parent, run, width)) break;
            Road hit = null; Vector2 hp = Vector2.zero;
            foreach (var r in Roads)
            {
                if (r.dead) continue;
                if (r == parent && run < 14f) continue;
                Vector2 tn; Vector2 q = NearestOnPoly(next.x, next.y, r.pts, out tn);
                if ((q - next).magnitude - r.width * 0.5f < width * 0.5f + 3.2f) { hit = r; hp = q; break; }
            }
            if (hit != null && run > 10f) { pts.Add(hp); break; }
            else if (hit != null) break;
            pts.Add(next);
            cur = next;
        }
        if (Len(pts) < 16f || pts.Count < 3) return null;
        var rd = new Road { id = id, width = width, mat = 1, lamps = false, pts = pts };
        Roads.Add(rd);
        return rd;
    }

    static int Infill(int maxN, float thresh, float width)
    {
        ign = Mathf.CeilToInt((228f - igx0) / igc) + 1; igm = Mathf.CeilToInt((226f - igz0) / igc) + 1;
        igd = new float[ign * igm]; igok = new bool[ign * igm];
        for (int j = 0; j < igm; j++)
            for (int i = 0; i < ign; i++)
            {
                Vector2 p = new Vector2(igx0 + i * igc, igz0 + j * igc);
                igok[j * ign + i] = SpurOk(p, null, 0f, width) && !NearBuilt(p, 3.5f);
                float dr = 1e9f;
                foreach (var r in Roads) { if (r.dead) continue; float dd = DistPoly(p.x, p.y, r.pts) - r.width * 0.5f; if (dd < dr) dr = dd; }
                igd[j * ign + i] = dr;
            }
        int made = 0;
        for (int it = 0; it < maxN; it++)
        {
            int bi = -1; float bd = thresh;
            for (int k = 0; k < igd.Length; k++) if (igok[k] && igd[k] > bd) { bd = igd[k]; bi = k; }
            if (bi < 0) break;
            Vector2 tgt = new Vector2(igx0 + (bi % ign) * igc, igz0 + (bi / ign) * igc);
            Road bestR = null; Vector2 bestP = Vector2.zero; float bestD = 1e9f;
            foreach (var r in Roads)
            {
                if (r.dead) continue;
                Vector2 tn; Vector2 q = NearestOnPoly(tgt.x, tgt.y, r.pts, out tn);
                float dd = (q - tgt).magnitude;
                if (dd < bestD) { bestD = dd; bestR = r; bestP = q; }
            }
            var lane = bestR == null ? null : LaneTo("sp_in" + made, bestP, tgt, bestR, width);
            igok[bi] = false;
            if (lane == null) continue;
            made++;
            for (int k = 0; k < igd.Length; k++)
            {
                Vector2 p = new Vector2(igx0 + (k % ign) * igc, igz0 + (k / ign) * igc);
                if (igd[k] <= 0f) continue;
                float dd = DistPoly(p.x, p.y, lane.pts) - lane.width * 0.5f;
                if (dd < igd[k]) igd[k] = dd;
            }
        }
        return made;
    }

    static void SubSpurs(Road r, string tag, double prob, int salt)
    {
        float L = Len(r.pts);
        if (L < 30f) return;
        int nsub = L > 60f ? 2 : 1;
        for (int k = 0; k < nsub; k++)
        {
            if (frng.NextDouble() > prob) continue;
            float d = L * (nsub == 1 ? 0.35f + (float)frng.NextDouble() * 0.3f : (k == 0 ? 0.3f : 0.65f) + (float)frng.NextDouble() * 0.12f);
            Spur(r.id + tag + k, r, d, (k + salt) % 2 == 0 ? 1 : -1, 80f + (float)frng.NextDouble() * 16f, 14f + (float)frng.NextDouble() * 22f,
                ((float)frng.NextDouble() - 0.5f) * 20f, 4.2f, 1, false, 14f);
        }
    }

    // ------------------------------------------------------------------ ladang cincin luar, kebun, taman, pemakaman
    /// <summary>Titik tembus sinar dari (cx,cz) pada sudut deg (derajat dari timur berlawanan arah jarum jam) dengan poligon tertutup (titik terjauh).</summary>
    static Vector2 RayHit(IList<Vector2> ring, float cx, float cz, float deg)
    {
        float a = deg * Mathf.Deg2Rad, dx = Mathf.Cos(a), dz = Mathf.Sin(a);
        float best = -1f;
        for (int i = 0; i < ring.Count; i++)
        {
            Vector2 p = ring[i], q = ring[(i + 1) % ring.Count], e = q - p;
            float den = dx * e.y - dz * e.x;
            if (Mathf.Abs(den) < 1e-6f) continue;
            float t = ((p.x - cx) * e.y - (p.y - cz) * e.x) / den, u = ((p.x - cx) * dz - (p.y - cz) * dx) / den;
            if (t > 0f && u >= 0f && u <= 1f && t > best) best = t;
        }
        return new Vector2(cx + dx * best, cz + dz * best);
    }

    static Road NearestRoad(string prefix, Vector2 target, out Vector2 q)
    {
        Road br = null; q = target; float bd = 1e9f;
        foreach (var r in Roads)
        {
            if (!r.id.StartsWith(prefix)) continue;
            Vector2 tn; Vector2 c = NearestOnPoly(target.x, target.y, r.pts, out tn);
            float d = (c - target).magnitude;
            if (d < bd) { bd = d; br = r; q = c; }
        }
        return br;
    }

    /// <summary>Bangunan di sisi LUAR (menjauhi pusat kota) jalan 'r' di titik terdekat ke target; menghadap jalan.</summary>
    static Lot AddOut(string id, string kind, Road r, Vector2 target, float off, int v, string label)
    {
        float arc = ArcOf(target.x, target.y, r.pts);
        Vector2 tg; Vector2 p = At(r.pts, arc, out tg);
        Vector2 n = new Vector2(tg.y, -tg.x);
        if (Vector2.Dot(n, p - new Vector2(0f, -5f)) < 0f) n = -n;
        float w, d, hz; Size(kind, out w, out d, out hz);
        Vector2 c = p + n * (r.width * 0.5f + off + d * 0.5f);
        return AddFacing(id, kind, c.x, c.y, p.x, p.y, v, label, "tani");
    }

    static void BuildSites()
    {
        // ---- taman bunga (kota tengah barat laut & timur laut), kebun apel (ladang barat daya), pemakaman (kota tengah utara)
        var park = new Zone { id = "taman_sakura", kind = "park" };
        for (int i = 0; i < 18; i++) { float a = i * Mathf.PI * 2f / 18f; park.poly.Add(new Vector2(-150f + Mathf.Sin(a) * 20f, 118f + Mathf.Cos(a) * 19f)); }
        Zones.Add(park);
        var parkE = new Zone { id = "taman_timur", kind = "park" };
        for (int i = 0; i < 18; i++) { float a = i * Mathf.PI * 2f / 18f; parkE.poly.Add(new Vector2(150f + Mathf.Sin(a) * 20f, 118f + Mathf.Cos(a) * 19f)); }
        Zones.Add(parkE);
        Pts["kelopak"] = new Vector2(-150f, 118f);
        var orch = new Zone { id = "kebun_apel", kind = "ok" };
        orch.poly.AddRange(new[] { new Vector2(-238f, -96f), new Vector2(-204f, -92f), new Vector2(-196f, -128f), new Vector2(-230f, -138f) });
        Zones.Add(orch);
        var makam = new Zone { id = "makam", kind = "makam" };
        makam.poly.AddRange(new[] { new Vector2(-72f, 188f), new Vector2(-22f, 188f), new Vector2(-22f, 204f), new Vector2(-60f, 204f), new Vector2(-72f, 196f) });
        Zones.Add(makam);
        Pts["makam"] = new Vector2(-46f, 196f);

        // ---- dusun ladang timur: rumah petani, lumbung, lumbung padi; kincir angin di timur laut; rumah petani kedua di barat laut
        var ringE = Inset(MidPoly, -farmRoadOff);
        Vector2 q; Road rd;
        rd = NearestRoad("tani_cincin", RayHit(ringE, 0f, -5f, 14f), out q);
        if (rd != null)
        {
            AddOut("farmhouse1", "farmhouse", rd, q, 3f, 0, "Rumah Petani");
            rd = NearestRoad("tani_cincin", RayHit(ringE, 0f, -5f, 22f), out q);
            if (rd != null) AddOut("barn1", "barn", rd, q, 4f, 0, "Lumbung Kota");
            rd = NearestRoad("tani_cincin", RayHit(ringE, 0f, -5f, 8f), out q);
            if (rd != null) AddOut("granary1", "granary", rd, q, 3f, 0, "Lumbung Padi");
        }
        rd = NearestRoad("tani_cincin", RayHit(ringE, 0f, -5f, 62f), out q);
        if (rd != null) AddOut("windmill", "windmill", rd, q, 7f, 0, "Kincir Angin");
        rd = NearestRoad("tani_cincin", RayHit(ringE, 0f, -5f, 160f), out q);
        if (rd != null) AddOut("farmhouse2", "farmhouse", rd, q, 3f, 1, "Rumah Petani");

        // titik prop pertanian (jerami, gerobak, peti, sumur berpenutup papan, ternak)
        var fh1 = LotOf("farmhouse1"); var fh2 = LotOf("farmhouse2"); var bn = LotOf("barn1"); var gr = LotOf("granary1"); var wm = LotOf("windmill");
        if (fh1 != null) { HayAt.Add(ToWorld(fh1, fh1.w * 0.5f + 3.6f, 2f)); HayAt.Add(ToWorld(fh1, -(fh1.w * 0.5f + 3.6f), -4f)); Pts["crates"] = ToWorld(fh1, -(fh1.w * 0.5f + 5f), fh1.d * 0.5f + 1f); }
        if (bn != null) { HayAt.Add(ToWorld(bn, bn.w * 0.5f + 4f, -3f)); HayAt.Add(ToWorld(bn, -(bn.w * 0.5f + 4f), 2f)); Pts["h9"] = ToWorld(bn, -(bn.w * 0.5f + 12f), 4f); Pts["cart"] = ToWorld(bn, bn.w * 0.5f + 5.5f, bn.d * 0.5f + 2f); }
        if (fh2 != null) { HayAt.Add(ToWorld(fh2, fh2.w * 0.5f + 3.6f, -3f)); Pts["well_tani"] = ToWorld(fh2, 0f, -(fh2.d * 0.5f + 9f)); Herds.Add(ToWorld(fh2, 0f, -(fh2.d * 0.5f + 20f))); Herds.Add(ToWorld(fh2, 16f, -(fh2.d * 0.5f + 16f))); }
        if (gr != null) HayAt.Add(ToWorld(gr, 5f, -5f));
        if (wm != null) HayAt.Add(ToWorld(wm, 8f, 8f));
        if (bn != null) { Herds.Add(ToWorld(bn, 0f, -(bn.d * 0.5f + 22f))); Herds.Add(ToWorld(bn, -18f, -(bn.d * 0.5f + 18f))); }
        if (!Pts.ContainsKey("well_tani") && fh1 != null) Pts["well_tani"] = ToWorld(fh1, 0f, -(fh1.d * 0.5f + 9f));

        // lorong kebun apel: dari jalan tani terdekat ke kebun
        Vector2 oc = new Vector2(-216f, -114f), kq;
        var kr = NearestRoad("tani_cincin", oc, out kq);
        if (kr != null)
        {
            var kb = new Road { id = "kebun", width = 5f, mat = 1, lamps = false, pts = new List<Vector2> { kq, Vector2.Lerp(kq, oc, 0.5f), oc } };
            Roads.Add(kb);
            Pts["orchard_cart"] = oc + new Vector2(4f, 5f);
        }
    }

    // ------------------------------------------------------------------ ladang (petak) di sepanjang jalan tani
    static bool FieldOk(Lot f, string parentId)
    {
        for (int i = 0; i < 9; i++)
        {
            float lx = (i % 3 - 1) * 0.5f * f.w, lz = (i / 3 - 1) * 0.5f * f.d;
            Vector2 p = ToWorld(f, lx, lz);
            if (RingAt(p.x, p.y) != 0) return false;
            if (InWallBand(p.x, p.y, 5f, 2f)) return false;
            if (DistPoly(p.x, p.y, River) < RiverHalfWidth(p.x, p.y) + 9f) return false;
            foreach (var g in Gates) if ((g.p - p).magnitude < 30f) return false;
            foreach (var r in Roads)
            {
                if (r.dead) continue;
                float dd = DistPoly(p.x, p.y, r.pts) - r.width * 0.5f;
                if (r.id == parentId) { if (dd < 1.5f) return false; }
                else if (dd < 1.0f) return false;
            }
        }
        foreach (var pl in Plazas)
        {
            float dd = pl.round ? Mathf.Sqrt((f.x - pl.x) * (f.x - pl.x) + (f.z - pl.z) * (f.z - pl.z)) - pl.w * 0.5f - 0.5f * Mathf.Max(f.w, f.d) : DistToRect(f.x, f.z, pl.x, pl.z, pl.yaw, pl.w, pl.d) - 0.5f * Mathf.Min(f.w, f.d);
            if (dd < 2f) return false;
        }
        foreach (var zn in Zones) if (Inside(f.x, f.z, zn.poly) || SignedDist(f.x, f.z, zn.poly) < 3f) return false;
        foreach (var o in Lots)
        {
            if (o.kind == "gatepad" || o.kind == "bridgepad") continue;
            float rr = (o.w > o.d ? o.w : o.d) + (f.w > f.d ? f.w : f.d);
            if (Mathf.Abs(o.x - f.x) > rr || Mathf.Abs(o.z - f.z) > rr) continue;
            if (RectsOverlap(f.x, f.z, f.yaw, f.w, f.d, o.x, o.z, o.yaw, o.w, o.d, 1.5f)) return false;
        }
        return true;
    }

    static int FieldsRing(string roadId, int side, int rows, float w, float d, int v0)
    {
        var r = RoadOf(roadId);
        return r == null ? 0 : FieldsAlong(r, roadId, side, rows, w, d, v0);
    }

    /// <summary>Petak ladang berbaris di satu sisi sebuah garis pandu (jalan sungguhan atau garis maya di sisi dalam tembok).</summary>
    static int FieldsAlong(Road r, string tag, int side, int rows, float w, float d, int v0)
    {
        float total = Len(r.pts);
        int made = 0;
        for (int row = 0; row < rows; row++)
        {
            float off = 3f + row * (d + 2.5f);
            float s = 4f; int i = 0, guard = 0;
            while (s + w < total - 4f && guard++ < 600)
            {
                Vector2 tg; Vector2 p = At(r.pts, s + w * 0.5f, out tg);
                Vector2 n = new Vector2(tg.y, -tg.x) * side;
                Vector2 c = p + n * (r.width * 0.5f + off + d * 0.5f);
                float yaw = Mathf.Atan2(-tg.y, tg.x) * Mathf.Rad2Deg;
                var f = new Lot { id = "field_" + tag + (side > 0 ? "p" : "m") + row + "_" + i, kind = "field", x = c.x, z = c.y, w = w, d = d, yaw = yaw, blend = 3f, pad = true, zone = "tani", v = (v0 + i + row * 2) % 4 };
                if (FieldOk(f, tag)) { f.py = Base(f.x, f.z); Lots.Add(f); s += w + 2.5f; i++; made++; }
                else s += 4f;
            }
        }
        return made;
    }

    static string[] mixThatch = { "thc_a", "thc_b", "thc_a", "thc_b", "thc_a" };

    static void BuildFields()
    {
        System.Func<float, float, bool> ring0 = (x, z) => RingAt(x, z) == 0;
        var snap = new List<Road>(Roads);
        // dusun rumah tanah liat beratap jerami: kelompok kecil di jalan tani melingkar (celah di antaranya untuk ladang)
        foreach (var r in snap)
        {
            if (r.id.StartsWith("tani_cincin"))
            {
                float L = Len(r.pts);
                int nh = Mathf.Max(1, Mathf.RoundToInt(L / 170f));
                for (int k = 0; k < nh; k++)
                {
                    float c = L * (k + 0.5f) / nh + ((float)frng.NextDouble() - 0.5f) * 24f;
                    FillBoth(r.id, Mathf.Max(0f, c - 24f), Mathf.Min(L, c + 24f), 2.6f, mixThatch, "tani", 0.15f, ring0);
                }
            }
            else if (r.id == "pasar_barat" || r.id == "pasar_timur" || r.id == "timur" || r.id == "barat" || r.id == "utara")
                FillBoth(r.id, 0f, -1f, 2.6f, mixThatch, "tani", 0.7f, ring0);
        }
        // petak ladang: pertama berderet di sisi dalam Tembok Luar (ladang membungkus kota), lalu mengisi celah di sepanjang jalan
        int v = 0;
        var og = Inset(OuterPoly, 8f);
        og.Add(og[0]);
        FieldsAlong(new Road { id = "pandu_luar", pts = og, width = 0f }, "luar", -1, 3, 30f, 17f, 5);
        foreach (var r in snap)
        {
            if (r.id.StartsWith("tani_cincin")) { FieldsRing(r.id, 1, 2, 30f, 17f, v++); FieldsRing(r.id, -1, 1, 30f, 17f, v++); }
            else if (r.id == "timur" || r.id == "barat" || r.id == "utara") { FieldsRing(r.id, 1, 2, 30f, 17f, v++); FieldsRing(r.id, -1, 2, 30f, 17f, v++); }
            else if (r.id == "pasar_barat" || r.id == "pasar_timur") { FieldsRing(r.id, 1, 2, 30f, 17f, v++); FieldsRing(r.id, -1, 2, 30f, 17f, v++); }
            else if (r.id == "raya") { FieldsRing(r.id, 1, 2, 30f, 17f, v++); FieldsRing(r.id, -1, 2, 30f, 17f, v++); }
        }
    }
    /// <summary>Bangunan di sisi jalan pada jarak arc: rumah menghadap jalan.</summary>
    static Lot AddBeside(string id, string kind, string roadId, float arc, int side, float off, int v, string label, string zone)
    {
        var r = RoadOf(roadId);
        float w, d, hz; Size(kind, out w, out d, out hz);
        Vector2 tg; Vector2 p = At(r.pts, arc, out tg);
        Vector2 n = new Vector2(tg.y, -tg.x) * side;
        Vector2 c = p + n * (r.width * 0.5f + off + d * 0.5f);
        return AddFacing(id, kind, c.x, c.y, p.x, p.y, v, label, zone);
    }

    /// <summary>Barisan rumah menghadap sisi plaza persegi (side 0 utara .. 3 barat) dari along from sampai to.</summary>
    static int RowAround(Plaza p, int side, float from, float to, float gap, string[] kinds, float skip, System.Func<float, float, bool> allow)
    {
        int made = 0; float along = from; int guard = 0;
        while (along < to - 3f && guard++ < 200)
        {
            string kind = kinds[frng.Next(kinds.Length)];
            float w, d, hz; Size(kind, out w, out d, out hz);
            float yaw; Vector2 c = Around(p, side, along + w * 0.5f, gap, d, out yaw);
            var lot = new Lot { id = "h" + Lots.Count, kind = kind, w = w, d = d, hz = hz, zone = ZoneAt(c.x, c.y), v = frng.Next(1000), x = c.x, z = c.y, yaw = yaw };
            if (frng.NextDouble() >= skip && CornersOk(lot, allow) && LotClear(lot, null, 0f)) { lot.py = Base(lot.x, lot.z); Lots.Add(lot); made++; along += w + gapMin + (float)frng.NextDouble() * gapRnd; }
            else along += 2.4f;
        }
        return made;
    }

    /// <summary>Rumah mengelilingi alun-alun bundar dari sudut a0 ke a1 (derajat dari utara, searah jarum jam), menghadap pusat.</summary>
    static int RingAround(Plaza p, float a0, float a1, float gap, string[] kinds, float skip, System.Func<float, float, bool> allow)
    {
        int made = 0; float a = a0; int guard = 0;
        while (a < a1 && guard++ < 400)
        {
            string kind = kinds[frng.Next(kinds.Length)];
            float w, d, hz; Size(kind, out w, out d, out hz);
            float rin = p.w * 0.5f + gap;
            float step = (w + 0.3f) / (rin + 0.5f) * Mathf.Rad2Deg;
            float am = (a + step * 0.5f) * Mathf.Deg2Rad;
            float R = rin + d * 0.5f;
            Vector2 c = new Vector2(p.x + Mathf.Sin(am) * R, p.z + Mathf.Cos(am) * R);
            var lot = new Lot { id = "h" + Lots.Count, kind = kind, w = w, d = d, hz = hz, zone = ZoneAt(c.x, c.y), v = frng.Next(1000), x = c.x, z = c.y, yaw = YawTo(c.x, c.y, p.x, p.z) };
            if (frng.NextDouble() >= skip && CornersOk(lot, allow) && LotClear(lot, null, 0f)) { lot.py = Base(lot.x, lot.z); Lots.Add(lot); made++; a += step + 0.4f; }
            else a += 3f;
        }
        return made;
    }

    static string[] mixLow = { "row_a", "row_b", "twn_a", "twn_b", "shop_a", "cot_a", "corner", "row_a", "twn_b" };
    static string[] mixShops = { "shop_a", "shop_a", "twn_b", "row_b", "shop_a", "store" };
    static string[] mixUp = { "twn_a", "twn_b", "villa", "corner", "row_b", "twn_a", "villa" };
    static string[] mixRes = { "cot_a", "row_a", "twn_a", "cot_a", "row_b", "workshop" };
    static string[] mixCraft = { "workshop", "store", "twn_a", "cot_a", "shop_a", "workshop" };

    static string[] MixFor(Road r, int ring)
    {
        bool spur = r.id.StartsWith("sp_");
        if (!spur) return ring == 2 ? mixUp : mixShops;
        int hsh = 0; foreach (char ch in r.id) hsh += ch;
        if (ring == 1) return (hsh & 1) == 0 ? mixRes : mixCraft;
        return (hsh & 1) == 0 ? mixUp : mixRes;
    }

    static void BuildHousing()
    {
        System.Func<float, float, bool> r1 = (x, z) => RingAt(x, z) == 1;
        System.Func<float, float, bool> r2 = (x, z) => RingAt(x, z) == 2;
        var ps = PlazaOf("simpang");
        var pm = PlazaOf("tengah");

        // ---- mengelilingi alun-alun (muka bangunan ke arah alun-alun)
        RowAround(ps, 0, -13f, 13f, 2.4f, mixShops, 0.05f, r1);
        RowAround(ps, 2, -13f, 13f, 2.4f, mixShops, 0.05f, r1);
        RingAround(pm, 98f, 262f, 2.6f, mixUp, 0.05f, r2);

        // ---- sepanjang jalan kota tengah, jalan besar dulu (raya = jalan pasar di dalam gerbang)
        string[] order1 = { "raya", "lintas", "lingkar_utara", "timur", "barat", "utara" };
        foreach (string id in order1)
        {
            var r = RoadOf(id);
            if (r != null) FillBoth(id, 0f, -1f, 2.4f, MixFor(r, 1), "x", 0.06f, r1);
        }
        // ---- kota dalam: rumah di sepanjang jalan masuk ke alun-alun
        FillBoth("raya", ArcAt("raya", 0f, -66f), -1f, 2.6f, mixUp, "x", 0.08f, r2);
        FillBoth("timur", ArcAt("timur", 106f, 32f), -1f, 2.6f, mixUp, "x", 0.12f, r2);
        FillBoth("barat", ArcAt("barat", -106f, 32f), -1f, 2.6f, mixUp, "x", 0.12f, r2);

        // ---- gang cabang lalu jalan peronda
        var snap = new List<Road>(Roads);
        foreach (var r in snap)
        {
            if (!r.id.StartsWith("sp_") && !r.id.StartsWith("benteng")) continue;
            bool ring = r.id.StartsWith("benteng");
            FillBoth(r.id, 0f, -1f, ring ? 1.2f : 2.2f, ring ? mixRes : MixFor(r, 1), "x", ring ? 0.12f : 0.08f, r1);
        }
        // zona rumah menurut cincin
        foreach (var l in Lots) if (l.zone == "x") l.zone = ZoneAt(l.x, l.z);

        // ---- gang pendek dari Jalan Timur ke tangga Menara Penyihir. Ditambahkan SESUDAH semua pengacakan di atas (indeks jalan & urutan acak
        //      tak bergeser, jadi tata letak lain tetap sama); rumah acak yang menghalangi koridornya dibuang.
        {
            var mr = Road_("menara", 4.6f, 0, 4, false, 84f, 28.5f, 84f, 38f, 84f, 46f, 84f, 50.3f);
            for (int i = Lots.Count - 1; i >= 0; i--)
            {
                var h = Lots[i];
                if (!h.id.StartsWith("h") || h.kind == "bridgepad" || h.kind == "gatepad" || h.kind == "field" || h.kind == "komplek") continue;
                bool hit = false;
                for (int k = 0; k < 25 && !hit; k++)
                {
                    Vector2 p = ToWorld(h, (k % 5 - 2) * 0.25f * h.w, (k / 5 - 2) * 0.25f * h.d);
                    if (DistPoly(p.x, p.y, mr.pts) - mr.width * 0.5f < 1.4f) hit = true;
                }
                if (hit) Lots.RemoveAt(i);
            }
        }

        // ---- pemakaman: denah awal (dipakai saat rumah & gang disebar) dipangkas SESUDAH itu supaya dindingnya tak menimpa jalan peronda
        //      (benteng2 lewat miring di sisi utara) dan tak menempel rumah di barat; sisi utara sejajar jalan peronda, jarak >= 2,8 m dari tepi jalan.
        foreach (var zn in Zones)
        {
            if (zn.kind != "makam") continue;
            zn.poly.Clear();
            zn.poly.AddRange(new[] { new Vector2(-69.5f, 188f), new Vector2(-22f, 188f), new Vector2(-22f, 203f), new Vector2(-69.5f, 193.4f) });
        }

        // ---- lapak pasar di alun-alun Gerbang (sisi timur jalan raya): baris utara menghadap selatan, baris selatan menghadap utara
        float[] sx = { 10f, 20f, 31f, 42f };
        foreach (float x in sx)
        {
            Stalls.Add(new Vector2(x, -243f)); StallYaw.Add(180f);
            Stalls.Add(new Vector2(x, -273f)); StallYaw.Add(0f);
        }
        // titik penting (dipakai builder)
        Pts["bard"] = new Vector2(-10f, -258f);
        Pts["mana_a"] = new Vector2(-12f, -246f);
        Pts["mana_b"] = new Vector2(47f, -258f);
        Pts["guard_barak_a"] = new Vector2(-26f, -301f);
        Pts["guard_barak_b"] = new Vector2(-26f, -291f);
        Pts["guard_istal"] = new Vector2(33f, -297f);
        Pts["sihir"] = new Vector2(20f, 4f);                                    // pertunjukan sihir: sisi timur alun-alun batu, di depan Katedral
    }

    // ------------------------------------------------------------------ pemeriksaan
    public static List<string> Validate()
    {
        Build();
        var msg = new List<string>();
        // pemakaman: dinding (tepi zona) tak boleh menimpa jalan; sisi selatan dikecualikan (gang berujung di gerbang)
        foreach (var zn in Zones)
        {
            if (zn.kind != "makam") continue;
            float zmin = 1e9f;
            foreach (var q in zn.poly) zmin = Mathf.Min(zmin, q.y);
            for (int i = 0; i < zn.poly.Count; i++)
            {
                Vector2 pa = zn.poly[i], pb = zn.poly[(i + 1) % zn.poly.Count];
                if (Mathf.Abs(pa.y - zmin) < 0.01f && Mathf.Abs(pb.y - zmin) < 0.01f) continue;
                int ns = Mathf.CeilToInt((pb - pa).magnitude);
                for (int k = 0; k <= ns; k++)
                {
                    Vector2 p = Vector2.Lerp(pa, pb, k / (float)Mathf.Max(1, ns));
                    foreach (var r in Roads)
                    {
                        if (DistPoly(p.x, p.y, r.pts) - r.width * 0.5f < 1.2f) { msg.Add("MAKAM dekat jalan: " + r.id); break; }
                    }
                }
            }
        }
        for (int i = 0; i < Lots.Count; i++)
        {
            var a = Lots[i];
            if (a.kind == "bridgepad" || a.kind == "gatepad" || a.kind == "field" || a.kind == "komplek") continue;
            for (int k = 0; k < 25; k++)
            {
                float lx = (k % 5 - 2) * 0.25f * a.w, lz = (k / 5 - 2) * 0.25f * a.d;
                Vector2 p = ToWorld(a, lx, lz);
                if (DistPoly(p.x, p.y, River) < RiverHalfWidth(p.x, p.y) * 0.62f + 4f) { msg.Add("SUNGAI: " + a.id + " (" + a.kind + ")"); break; }
            }
            bool landmark = a.id.Length > 0 && !a.id.StartsWith("h");
            for (int k = 0; k < 25; k++)
            {
                float lx = (k % 5 - 2) * 0.25f * a.w, lz = (k / 5 - 2) * 0.25f * a.d;
                Vector2 p = ToWorld(a, lx, lz);
                bool bad = false;
                foreach (var r in Roads)
                {
                    float dd = DistPoly(p.x, p.y, r.pts) - r.width * 0.5f;
                    if (dd < 0.2f) { msg.Add("JALAN tertimpa: " + a.id + " x " + r.id + " (" + dd.ToString("0.0") + ")"); bad = true; break; }
                }
                if (bad) break;
            }
            if (a.zone != "tani" && !Inside(a.x, a.z, OuterPoly)) msg.Add("DI LUAR TEMBOK: " + a.id);
            if (a.kind != "field" && InWallBand(a.x, a.z, 0f, 0f)) msg.Add("DI PITA TEMBOK/PARIT: " + a.id);
            for (int j = i + 1; j < Lots.Count; j++)
            {
                var b = Lots[j];
                if (b.kind == "bridgepad" || b.kind == "gatepad") continue;
                if (RectsOverlap(a.x, a.z, a.yaw, a.w, a.d, b.x, b.z, b.yaw, b.w, b.d, 0.1f)) msg.Add("TUMPANG TINDIH: " + a.id + " x " + b.id);
            }
            foreach (var pl in Plazas)
            {
                if (pl.kind == "halaman" && (a.kind == "barracks" || a.kind == "stables" || a.kind == "castle" || a.kind == "manor")) continue;
                float dd = pl.round ? Mathf.Sqrt((a.x - pl.x) * (a.x - pl.x) + (a.z - pl.z) * (a.z - pl.z)) - pl.w * 0.5f - 0.5f * Mathf.Max(a.w, a.d)
                    : DistToRect(a.x, a.z, pl.x, pl.z, pl.yaw, pl.w, pl.d) - 0.5f * Mathf.Min(a.w, a.d);
                if (dd < -0.2f && !landmark) msg.Add("PLAZA tertimpa: " + a.id + " x " + pl.id);
            }
        }
        return msg;
    }
}
