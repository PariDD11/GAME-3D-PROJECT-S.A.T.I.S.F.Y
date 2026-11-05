using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// v35z102n73 "Kampung Tepi Kali": denah BARU Desa Pemula sebagai DATA MURNI (tanpa API Unity selain Vector2/Vector3/Mathf),
/// sehingga bisa dihitung, diperiksa, dan digambar di luar Unity. Builder (TownSceneBuilder.Village2*.cs) hanya membaca data ini.
/// Sumbu: x = timur, z = utara. Titik awal pemain (SpawnStart) tetap di (0,-30). Jalan utara lurus di x = 0 (menyambung ke Hutan Pemula
/// di z = 240), jalan timur lurus di z = -6 (menuju Ashwatch). Kali Embun mengalir barat-ke-timur di selatan desa.
/// </summary>
public static class V2
{
    // ------------------------------------------------------------------ noise sendiri (hasil sama di Unity dan di pratinjau luar)
    static float Hs(int x, int y, int s)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + s * 1442695041;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0xffffff) / (float)0x1000000;
        }
    }

    public static float Nz(float x, float z, float scale, int seed)
    {
        float fx = x * scale, fz = z * scale;
        int x0 = (int)Mathf.Floor(fx), z0 = (int)Mathf.Floor(fz);
        float tx = fx - x0, tz = fz - z0;
        tx = tx * tx * (3f - 2f * tx); tz = tz * tz * (3f - 2f * tz);
        float a = Hs(x0, z0, seed), b = Hs(x0 + 1, z0, seed), c = Hs(x0, z0 + 1, seed), d = Hs(x0 + 1, z0 + 1, seed);
        return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), tz);
    }

    public static float Sm(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

    // ------------------------------------------------------------------ geometri 2D
    public static float DistSeg(float px, float pz, float ax, float az, float bx, float bz)
    {
        float vx = bx - ax, vz = bz - az, l2 = vx * vx + vz * vz;
        float t = l2 < 1e-6f ? 0f : Mathf.Clamp01(((px - ax) * vx + (pz - az) * vz) / l2);
        float dx = px - (ax + vx * t), dz = pz - (az + vz * t);
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    public static float DistPoly(float x, float z, IList<Vector2> p)
    {
        float best = 1e9f;
        for (int i = 0; i + 1 < p.Count; i++)
        {
            float d = DistSeg(x, z, p[i].x, p[i].y, p[i + 1].x, p[i + 1].y);
            if (d < best) best = d;
        }
        return best;
    }

    /// <summary>Titik terdekat pada polyline + arah singgung (tx,tz) di sana.</summary>
    public static Vector2 NearestOnPoly(float x, float z, IList<Vector2> p, out Vector2 tangent)
    {
        float best = 1e9f; Vector2 bp = p[0]; tangent = new Vector2(1f, 0f);
        for (int i = 0; i + 1 < p.Count; i++)
        {
            float vx = p[i + 1].x - p[i].x, vz = p[i + 1].y - p[i].y, l2 = vx * vx + vz * vz;
            float t = l2 < 1e-6f ? 0f : Mathf.Clamp01(((x - p[i].x) * vx + (z - p[i].y) * vz) / l2);
            float qx = p[i].x + vx * t, qz = p[i].y + vz * t;
            float d = (qx - x) * (qx - x) + (qz - z) * (qz - z);
            if (d < best) { best = d; bp = new Vector2(qx, qz); tangent = new Vector2(vx, vz).normalized; }
        }
        return bp;
    }

    public static List<Vector2> Smooth(Vector2[] p, int sub)
    {
        var r = new List<Vector2>();
        for (int i = 0; i + 1 < p.Length; i++)
        {
            Vector2 p0 = p[Mathf.Max(0, i - 1)], p1 = p[i], p2 = p[i + 1], p3 = p[Mathf.Min(p.Length - 1, i + 2)];
            for (int k = 0; k < sub; k++)
            {
                float t = k / (float)sub, t2 = t * t, t3 = t2 * t;
                r.Add(((p1 * 2f) + (p2 - p0) * t + (p0 * 2f - p1 * 5f + p2 * 4f - p3) * t2 + (p1 * 3f - p0 - p2 * 3f + p3) * t3) * 0.5f);
            }
        }
        r.Add(p[p.Length - 1]);
        return r;
    }

    public static float Len(IList<Vector2> p) { float l = 0f; for (int i = 1; i < p.Count; i++) l += (p[i] - p[i - 1]).magnitude; return l; }

    /// <summary>Titik pada polyline di jarak d dari awal.</summary>
    public static Vector2 At(IList<Vector2> p, float d, out Vector2 tangent)
    {
        tangent = new Vector2(1f, 0f);
        float acc = 0f;
        for (int i = 1; i < p.Count; i++)
        {
            float s = (p[i] - p[i - 1]).magnitude;
            if (acc + s >= d || i == p.Count - 1)
            {
                float t = s < 1e-5f ? 0f : Mathf.Clamp01((d - acc) / s);
                tangent = (p[i] - p[i - 1]).normalized;
                return p[i - 1] + (p[i] - p[i - 1]) * t;
            }
            acc += s;
        }
        return p[p.Count - 1];
    }

    // ------------------------------------------------------------------ titik penting
    public static readonly Vector2 Spawn = new Vector2(0f, -30f);
    public static readonly Vector2 Junction = new Vector2(2f, 18f);          // simpang beringin: jalan utama, jalan timur, jalur barat
    public static readonly Vector2 Oak = new Vector2(-14f, 15f), Well = new Vector2(-4f, 27f), Founder = new Vector2(-9f, 5f), Notice = new Vector2(7f, -26f);
    public static readonly Vector2 GateN = new Vector2(0f, 140f);           // gerbang di mulut ngarai (tebing kiri-kanan merapat)
    public static readonly Vector2 GateE = new Vector2(140f, -6f);
    public static readonly Vector2 Knoll = new Vector2(-68f, 68f);           // bukit gereja
    public const float KnollTop = 6.2f, KnollFlat = 25f, KnollFoot = 44f;
    public static readonly Vector2 Basin = new Vector2(2f, 4f);              // pusat cekungan; tebing hutan mulai di tepinya
    public const float BasinRx = 128f, BasinRz = 117f;

    // ------------------------------------------------------------------ jalan & kali
    public class Road { public string id; public List<Vector2> pts; public float width; public bool cobble; }
    public static List<Vector2> Stream;
    static float sx0, sx1, sz0, sz1;                           // kotak pembatas kali (percepat Base)

    /// <summary>Jembatan di tempat jalan memotong kali. Jalur sepanjang +Z lokal; yaw = arah jalan; y = tinggi dek di ujung.</summary>
    public class Bridge { public string id; public Vector2 c; public float yaw, L, W, y, chord; public bool stone; }
    public static readonly List<Bridge> Bridges = new List<Bridge>();
    public static readonly List<Road> Roads = new List<Road>();
    static List<Vector2> roadN, roadE;

    // ------------------------------------------------------------------ petak / bangunan
    public class Lot
    {
        public string id, kind, label;
        public float x, z, yaw, w, d, py;     // py = tinggi tanah rata (pad)
        public int v;
        public bool pad = true;               // ratakan tanah di bawahnya
        public float blend = 3f;              // lebar peralihan pad ke medan
        public float hz;                      // ketinggian bangunan kira-kira (untuk penempatan atap/asap)
        public Vector2 P { get { return new Vector2(x, z); } }
    }
    public static readonly List<Lot> Lots = new List<Lot>();
    static bool built;

    static void Road_(string id, float width, bool cobble, int sub, params float[] xz)
    {
        var p = new Vector2[xz.Length / 2];
        for (int i = 0; i < p.Length; i++) p[i] = new Vector2(xz[i * 2], xz[i * 2 + 1]);
        Roads.Add(new Road { id = id, width = width, cobble = cobble, pts = Smooth(p, sub) });
    }

    static float YawTo(float x, float z, float tx, float tz) { return Mathf.Atan2(tx - x, tz - z) * Mathf.Rad2Deg; }

    // kind -> ukuran bawaan (w sepanjang X lokal, d sepanjang Z lokal; muka bangunan = +Z)
    static void Size(string kind, out float w, out float d, out float hz)
    {
        switch (kind)
        {
            case "elder": w = 14f; d = 6.6f; hz = 6.2f; break;
            case "inn": w = 13f; d = 9f; hz = 9f; break;
            case "smithy": w = 10f; d = 8f; hz = 6.5f; break;
            case "healer": w = 6f; d = 5.6f; hz = 4.8f; break;
            case "office": w = 9f; d = 7f; hz = 7f; break;
            case "chapel": w = 8.6f; d = 19f; hz = 15f; break;
            case "barn": w = 15f; d = 11f; hz = 10f; break;
            case "mill": w = 9f; d = 8f; hz = 8.5f; break;
            case "granary": w = 4.4f; d = 4.4f; hz = 4.6f; break;
            case "thatchA": w = 6f; d = 5.2f; hz = 5f; break;
            case "thatchB": w = 8f; d = 5.4f; hz = 5.2f; break;
            case "cabin": w = 6.4f; d = 5.4f; hz = 4.8f; break;
            case "farm": w = 8.6f; d = 6.6f; hz = 8f; break;
            case "tudor": w = 7.6f; d = 6.2f; hz = 8f; break;
            case "stall": w = 3.6f; d = 2.4f; hz = 3f; break;
            case "well": w = 2.6f; d = 2.6f; hz = 3.2f; break;
            case "shed": w = 4f; d = 3f; hz = 2.8f; break;
            default: w = 5f; d = 5f; hz = 4f; break;
        }
    }

    /// <summary>Tambah bangunan. (fx,fz) = titik yang dihadapi (muka +Z); NaN = hadap ke jalan terdekat.</summary>
    static Lot Add(string id, string kind, float x, float z, float fx, float fz, int v = 0, string label = null)
    {
        float w, d, hz; Size(kind, out w, out d, out hz);
        var l = new Lot { id = id, kind = kind, x = x, z = z, w = w, d = d, hz = hz, v = v, label = label };
        if (float.IsNaN(fx))
        {
            float bd = 1e9f; Vector2 bt = new Vector2(0f, 1f), bp = new Vector2(x, z);
            foreach (var r in Roads)
            {
                Vector2 tg; Vector2 q = NearestOnPoly(x, z, r.pts, out tg);
                float dd = (q - new Vector2(x, z)).magnitude;
                if (dd < bd) { bd = dd; bp = q; bt = tg; }
            }
            fx = bp.x; fz = bp.y;
        }
        l.yaw = YawTo(x, z, fx, fz);
        Lots.Add(l);
        return l;
    }

    static Lot Area(string id, string kind, float x, float z, float w, float d, float yaw, float blend = 3f, bool pad = true)
    {
        var l = new Lot { id = id, kind = kind, x = x, z = z, w = w, d = d, yaw = yaw, blend = blend, pad = pad };
        Lots.Add(l);
        return l;
    }

    static void MakeBridge(string id, Road r, bool stone, float W)
    {
        float total = Len(r.pts);
        float d0 = -1f, d1 = -1f;
        for (float d = 0f; d <= total; d += 0.5f)
        {
            Vector2 tg; Vector2 p = At(r.pts, d, out tg);
            if (DistPoly(p.x, p.y, Stream) < StreamHalfWidth(p.x, p.y) * 1.25f) { if (d0 < 0f) d0 = d; d1 = d; }
        }
        if (d0 < 0f) return;
        Vector2 tan; Vector2 c = At(r.pts, (d0 + d1) * 0.5f, out tan);
        float span = d1 - d0 + 1.5f;
        var br = new Bridge { id = id, c = c, W = W, stone = stone, yaw = Mathf.Atan2(tan.x, tan.y) * Mathf.Rad2Deg };
        br.chord = Mathf.Max(9f, span + 2f);
        br.L = stone ? br.chord + 8.5f : span + 7f;
        br.y = stone ? 0.55f : 0.35f;
        Bridges.Add(br);
        for (int sg = -1; sg <= 1; sg += 2)
        {
            Vector2 e = c + tan * (sg * (br.L * 0.5f + 1.5f));
            var pad = Area(id + (sg < 0 ? "_padA" : "_padB"), "bridgepad", e.x, e.y, W + 3f, 6f, br.yaw, 4f, true);
            pad.py = br.y;
        }
    }

    // ------------------------------------------------------------------ bangun data
    public static void Build()
    {
        if (built) return;
        built = true;
        Roads.Clear(); Lots.Clear();

        // ---- kali (barat -> timur), melewati selatan desa
        Stream = Smooth(new[]
        {
            new Vector2(-175f, -4f), new Vector2(-140f, -22f), new Vector2(-108f, -40f), new Vector2(-80f, -49f), new Vector2(-52f, -54f),
            new Vector2(-24f, -50f), new Vector2(2f, -56f), new Vector2(28f, -62f), new Vector2(56f, -60f), new Vector2(84f, -52f),
            new Vector2(112f, -60f), new Vector2(146f, -76f), new Vector2(190f, -84f)
        }, 8);

        // ---- jalan
        Road_("utama", 4.8f, true, 8, 2f, -64f, 2f, -52f, 3f, -38f, 1f, -24f, -3f, -10f, -1f, 4f, 2f, 18f, 0f, 34f, -4f, 52f, -3f, 72f, 0f, 92f, 0f, 112f, 0f, 135f, 0f, 160f, 0f, 240f);
        Road_("timur", 4.4f, true, 8, 2f, 18f, 22f, 17f, 42f, 11f, 64f, 3f, 86f, -3f, 108f, -6f, 133f, -6f, 160f, -6f, 192f, -6f);
        Road_("gereja", 3.2f, false, 8, -4f, 52f, -18f, 56f, -30f, 61f, -38f, 65f, -42f, 67f);
        Road_("kincir", 3.2f, false, 8, -3f, -10f, -20f, -14f, -38f, -20f, -52f, -27f, -60f, -31f);
        Road_("titian", 2.8f, false, 8, -38f, -22f, -38f, -40f, -38f, -56f, -35f, -66f, -30f, -73f);
        Road_("tani", 3.0f, false, 8, 46f, 10f, 50f, -4f, 54f, -18f, 58f, -32f);
        Road_("lumbung", 3.0f, false, 8, 64f, 3f, 69f, 22f, 67f, 44f, 61f, 62f, 60f, 70f);
        Road_("latihan", 3.4f, false, 8, 2f, -64f, -1f, -70f, -9f, -75f, -19f, -79f);
        Road_("pandai", 3.0f, false, 6, 2f, -26f, 8f, -26f, 14f, -24f);
        Road_("kebun", 2.6f, false, 6, -29f, 61f, -33f, 74f, -37f, 86f, -40f, 91f);
        // ngarai utara diteruskan lurus & rata melewati tepi terrain desa (z=240): penggabungan Hutan Pemula meminjam tinggi desa sampai z~285
        roadN = new List<Vector2>(Roads[0].pts);
        roadN.Add(new Vector2(0f, 262f)); roadN.Add(new Vector2(0f, 290f)); roadN.Add(new Vector2(0f, 340f));
        roadE = Roads[1].pts;
        Bridges.Clear();
        sx0 = sz0 = 1e9f; sx1 = sz1 = -1e9f;
        foreach (var q in Stream) { sx0 = Mathf.Min(sx0, q.x); sx1 = Mathf.Max(sx1, q.x); sz0 = Mathf.Min(sz0, q.y); sz1 = Mathf.Max(sz1, q.y); }
        sx0 -= 14f; sz0 -= 14f; sx1 += 14f; sz1 += 14f;
        MakeBridge("jembatan_utama", Roads[0], true, 5.2f);
        MakeBridge("jembatan_titian", Roads[4], false, 2.0f);

        // ---- fasilitas inti (id tetap dipakai builder)
        Add("elder", "elder", -27f, 31f, 2f, 21f, 1, "Rumah Veteran Brann");
        Add("inn", "inn", 32f, 30f, 12f, 20f, 2, "Penginapan");
        Add("smithy", "smithy", 23f, -22f, 2f, -22f, 3, "Pandai Besi");
        Add("healer", "healer", -20f, -2f, -2f, -2f, 4, "Tabib Mira");
        Add("office", "office", 15f, 54f, -2f, 54f, 5, "Kantor Properti");
        Add("chapel", "chapel", -56f, 69f, -30f, 69f, 0, "Gereja Desa");
        Add("barn", "barn", 76f, 70f, 56f, 70f, 0, "Lumbung & Kandang");
        Add("mill", "mill", -66f, -38f, -56f, -28f, 0, "Kincir Air");
        Add("granary", "granary", 70f, -27f, 60f, -27f, 0, "Lumbung Padi");
        // rumah dijual
        Add("home_hut", "cabin", -27f, -37f, -27f, -22f, 0, "Dijual: Gubuk Tepi Sungai");
        Add("home_cottage", "thatchA", 45f, -15f, 57f, -15f, 1, "Dijual: Pondok Kebun");
        Add("home_house", "farm", -24f, 49f, -24f, 60f, 0, "Dijual: Rumah Warga Dua Lantai");

        // ---- rumah warga
        Add("c1", "thatchA", -13f, 42f, 1f, 40f, 0);
        Add("c2", "tudor", 14f, 38f, 0f, 38f, 0);
        Add("c3", "thatchB", 14f, 74f, 0f, 74f, 1);
        Add("c4", "cabin", -16f, 80f, -1f, 80f, 0);
        Add("c5", "thatchA", 13f, 92f, 0f, 92f, 1);
        Add("c6", "farm", -15f, 96f, -1f, 96f, 1);
        Add("c7", "thatchA", -42f, -9f, -34f, -14f, 1);
        Add("c8", "tudor", -24f, -29f, -24f, -18f, 1);
        Add("c9", "thatchB", 38f, 2f, 38f, 12f, 0);
        Add("c10", "cabin", 74f, 12f, 74f, 3f, 1);
        Add("c11", "thatchA", 92f, 8f, 90f, -2f, 0);
        Add("c12", "farm", 66f, -40f, 58f, -34f, 1);
        Add("c13", "thatchA", 36f, 52f, 20f, 54f, 1);
        Add("c14", "cabin", -43f, 53f, -42f, 64f, 1);
        Add("c15", "thatchA", -12f, -38f, -12f, -28f, 0);
        Add("c16", "tudor", 14f, -41f, 8f, -32f, 1);
        // ---- bangunan kecil & kios
        Add("stall0", "stall", 8.5f, 2f, -5f, 2f, 0, "Toko Bram");
        Add("stall1", "stall", 9.5f, 8f, -5f, 8f, 1);
        Add("stall2", "stall", 10.5f, 14f, -5f, 14f, 0);
                Add("shed2", "shed", -50f, 38f, -50f, 48f, 0);

        // ---- area (pad datar / zona)
        Area("pasture", "pasture", 74f, 66f, 54f, 56f, 0f, 3f, false);
        Area("orchard", "orchard", -46f, 98.5f, 44f, 15f, 0f, 3f, false);
        Area("yard", "yard", -46f, -87f, 60f, 30f, 0f, 4f, true);
        Area("graveyard", "graveyard", -81f, 68f, 22f, 20f, 0f, 4f, true);
        Area("field1", "field", 89f, -18f, 24f, 14f, 0f, 3f, true).v = 0;     // gandum
        Area("field2", "field", 89f, -34f, 24f, 14f, 0f, 3f, true).v = 0;
        Area("field3", "field", 68f, -13f, 10f, 8f, 0f, 3f, true).v = 2;     // kubis
        Area("field4", "field", 40f, -39f, 18f, 11f, 6f, 3f, true).v = 1;     // sayur
        Area("field5", "field", 34f, -24f, 8f, 6f, -4f, 3f, true).v = 3;     // dibajak
        
        // tinggi pad = tinggi dasar medan di tengah
        foreach (var l in Lots) if (l.kind != "bridgepad") l.py = Base(l.x, l.z);
    }

    // ------------------------------------------------------------------ medan
    /// <summary>Jarak (>=0) dari titik ke tepi persegi panjang berputar; 0 bila di dalam.</summary>
    public static float DistToRect(float px, float pz, Lot l)
    {
        float a = l.yaw * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
        float dx = px - l.x, dz = pz - l.z;
        float lx = dx * c - dz * s, lz = dx * s + dz * c;       // sumbu lokal bangunan: X = lebar, Z = depan (yaw positif memutar +Z lokal ke +X dunia)
        float ex = Mathf.Max(0f, Mathf.Abs(lx) - l.w * 0.5f), ez = Mathf.Max(0f, Mathf.Abs(lz) - l.d * 0.5f);
        return Mathf.Sqrt(ex * ex + ez * ez);
    }

    /// <summary>Dari dunia ke koordinat lokal bangunan (lx lebar, lz kedalaman/depan).</summary>
    public static Vector2 ToLocal(float px, float pz, Lot l)
    {
        float a = l.yaw * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
        float dx = px - l.x, dz = pz - l.z;
        return new Vector2(dx * c - dz * s, dx * s + dz * c);
    }

    public static Vector2 ToWorld(Lot l, float lx, float lz)
    {
        float a = l.yaw * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
        return new Vector2(l.x + lx * c + lz * s, l.z - lx * s + lz * c);
    }

    public static float StreamHalfWidth(float x, float z) { return 3.0f + 1.3f * Nz(x, z, 0.03f, 71); }

    /// <summary>1 di tepi kali (lumpur basah boleh muncul), 0 jauh dari kali. Lantai cekungan yang kebetulan rendah
    /// bukan tepi kali, jadi tidak boleh dicat lumpur (dulu muncul bercak cokelat besar di padang).</summary>
    public static float StreamBank(float x, float z)
    {
        if (Stream == null) Build();
        if (x <= sx0 || x >= sx1 || z <= sz0 || z >= sz1) return 0f;
        float hw = StreamHalfWidth(x, z);
        float dS = DistPoly(x, z, Stream);
        return Sm(1f - (dS - (hw + 3f)) / 5f);
    }

    /// <summary>Medan dasar: cekungan, bukit gereja, tebing hutan, gunung jauh, kali. Tanpa pad bangunan.</summary>
    public static float Base(float x, float z)
    {
        if (Stream == null) Build();
        float r = Mathf.Sqrt(x * x + z * z);
        float edge = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
        // relief lembut (lebih datar di tengah desa)
        float n1 = (Nz(x, z, 0.012f, 3) - 0.5f) * 2f, n2 = (Nz(x, z, 0.04f, 9) - 0.5f) * 2f, n3 = (Nz(x, z, 0.11f, 5) - 0.5f) * 2f;
        float calm = 0.3f + 0.7f * Sm((r - 22f) / 45f);
        float h = (n1 * 1.1f + n2 * 0.42f + n3 * 0.1f) * calm;
        // bukit gereja (puncak datar)
        float dk = Mathf.Sqrt((x - Knoll.x) * (x - Knoll.x) + (z - Knoll.y) * (z - Knoll.y));
        h += KnollTop * Sm(1f - (dk - KnollFlat) / (KnollFoot - KnollFlat)) * (1f + 0.04f * n2);
        // tebing hutan melingkari cekungan + gunung jauh
        float bd = Mathf.Sqrt(((x - Basin.x) / BasinRx) * ((x - Basin.x) / BasinRx) + ((z - Basin.y) / BasinRz) * ((z - Basin.y) / BasinRz));
        float rid = 1f - Mathf.Abs(2f * Nz(x, z, 0.011f, 21) - 1f);
        float ring = Sm((bd - 0.97f) / 0.15f) * (19f + 4f * n1);
        float mount = Sm((edge - 148f) / 55f) * (22f + 26f * rid * rid + 8f * Nz(x, z, 0.03f, 2));
        float hill = (Nz(x, z, 0.02f, 13) - 0.5f) * 2f * 4.5f * Sm((bd - 1.05f) / 0.25f);
        float high = ring + mount + hill;
        // jalan = celah rata menembus tebing
        if (Mathf.Abs(high) > 0.0005f)
        {
            float dR = Mathf.Min(DistPoly(x, z, roadN), DistPoly(x, z, roadE));
            float pass = Mathf.Lerp(30f, 11f, Sm((edge - 126f) / 22f));       // makin jauh dari cekungan, celah makin sempit: ngarai di gerbang
            high *= Sm((dR - 6f) / pass);
            h += high;
        }
        // kali
        if (x > sx0 && x < sx1 && z > sz0 && z < sz1)
        {
            float dS = DistPoly(x, z, Stream);
            float hw = StreamHalfWidth(x, z);
            h = Mathf.Lerp(h, -3.2f, Sm(1f - (dS - hw * 0.4f) / (hw * 3.0f)));       // tebing landai (<= ~35 derajat) agar bisa keluar dari air
        }
        return h;
    }

    public static float H(float x, float z)
    {
        float h = Base(x, z);
        for (int i = 0; i < Lots.Count; i++)
        {
            var l = Lots[i];
            if (!l.pad) continue;
            if (Mathf.Abs(x - l.x) > l.w + l.d + 20f || Mathf.Abs(z - l.z) > l.w + l.d + 20f) continue;
            float dd = DistToRect(x, z, l) - 1.2f;                // bantalan 1,2 m di luar dinding
            float wgt = Sm(1f - dd / l.blend);
            if (wgt > 0f) h = Mathf.Lerp(h, l.py, wgt);
        }
        return h;
    }

    // ------------------------------------------------------------------ pemeriksaan
    public static List<string> Validate()
    {
        Build();
        var msg = new List<string>();
        for (int i = 0; i < Lots.Count; i++)
        {
            var a = Lots[i];
            if (a.kind == "pasture" || a.kind == "orchard" || a.kind == "bridgepad") continue;
            // jarak ke kali (sampel tepi persegi panjang)
            float dsMin = 1e9f;
            for (float lx = -a.w / 2f; lx <= a.w / 2f + 0.01f; lx += a.w / 4f)
                for (float lz = -a.d / 2f; lz <= a.d / 2f + 0.01f; lz += a.d / 4f)
                { Vector2 p = ToWorld(a, lx, lz); dsMin = Mathf.Min(dsMin, DistPoly(p.x, p.y, Stream) - StreamHalfWidth(p.x, p.y) * 1.8f); }
            if (dsMin < 2f && a.kind != "mill") msg.Add("KALI terlalu dekat: " + a.id + " (" + dsMin.ToString("0.0") + ")");
            // jarak ke jalan (kecuali bangunan yang memang menghadap jalan: minimal tepi jalan + 1.2)
            if (a.kind != "field" && a.kind != "yard" && a.kind != "graveyard" && a.kind != "stall")
            {
                float worst = 1e9f;
                for (float lx = -a.w / 2f; lx <= a.w / 2f + 0.01f; lx += a.w / 4f)
                    for (float lz = -a.d / 2f; lz <= a.d / 2f + 0.01f; lz += a.d / 4f)
                    {
                        Vector2 p = ToWorld(a, lx, lz);
                        foreach (var r in Roads)
                        {
                            float dd = DistPoly(p.x, p.y, r.pts) - r.width * 0.5f;
                            if (dd < worst) worst = dd;
                        }
                    }
                if (worst < 0.9f) msg.Add("JALAN tertimpa: " + a.id + " (" + worst.ToString("0.0") + ")");
            }
            for (int j = i + 1; j < Lots.Count; j++)
            {
                var b = Lots[j];
                if (b.kind == "pasture" || b.kind == "orchard" || b.kind == "bridgepad") continue;
                {
                    // dua persegi panjang berputar: uji titik sampel masing-masing terhadap yang lain
                    bool hit = false;
                    for (float lx = -a.w / 2f; lx <= a.w / 2f + 0.01f && !hit; lx += a.w / 4f)
                        for (float lz = -a.d / 2f; lz <= a.d / 2f + 0.01f && !hit; lz += a.d / 4f)
                        { Vector2 p = ToWorld(a, lx, lz); if (DistToRect(p.x, p.y, b) < 0.8f) hit = true; }
                    for (float lx = -b.w / 2f; lx <= b.w / 2f + 0.01f && !hit; lx += b.w / 4f)
                        for (float lz = -b.d / 2f; lz <= b.d / 2f + 0.01f && !hit; lz += b.d / 4f)
                        { Vector2 p = ToWorld(b, lx, lz); if (DistToRect(p.x, p.y, a) < 0.8f) hit = true; }
                    if (hit) msg.Add("TUMPANG TINDIH: " + a.id + " x " + b.id);
                }
            }
            // area awal pemain
            if (a.kind != "field" && DistToRect(Spawn.x, Spawn.y, a) < 9f) msg.Add("dekat SpawnStart: " + a.id);
            // batas cekungan
            float bd = Mathf.Sqrt(((a.x - Basin.x) / BasinRx) * ((a.x - Basin.x) / BasinRx) + ((a.z - Basin.y) / BasinRz) * ((a.z - Basin.y) / BasinRz));
            if (bd > 0.88f && a.kind != "pasture") msg.Add("dekat TEBING: " + a.id + " bd=" + bd.ToString("0.00"));
        }
        return msg;
    }
}
