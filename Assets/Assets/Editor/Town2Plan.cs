using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// v35z105n81 "Ashwatch Konsentris" (kanon LORE_BIBLE §8.1): denah KOTA BENTENG ABAD PERTENGAHAN sebagai DATA MURNI (hanya Vector2/Mathf), sehingga bisa
/// dihitung, diperiksa, dan digambar di luar Unity. Builder (TownSceneBuilder.Town2*.cs) hanya membaca data ini.
/// Sumbu: x = timur, z = utara. Gerbang Luar Selatan tetap di (0,-330) (koridor hutan di selatan bergantung padanya).
/// Tiga tembok kelabu konsentris: Tembok Luar (paling rendah, paling lebar) mengurung ladang hijau berparit dengan rumah tanah liat beratap jerami;
/// Tembok Tengah (lebih tinggi) mengurung kota padat atap merah; Tembok Dalam (paling gelap, paling tinggi) mengelilingi bukit mahkota dengan
/// Katedral jarum putih di pusat dan kastil bermenara emas. Dua belas gerbang (empat per tembok), tiap tembok berparit di luarnya;
/// Sungai Abu masuk lewat lengkung air dan mengisi parit luar; tembok pembatas halaman kastil melintang di belakang Katedral.
/// File ini: alat geometri + medan. Data denah ada di Town2PlanData.cs.
/// </summary>
public static partial class T2
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

    /// <summary>Jarak ke batas poligon TERTUTUP.</summary>
    public static float DistRing(float x, float z, IList<Vector2> p)
    {
        float best = 1e9f;
        for (int i = 0; i < p.Count; i++)
        {
            Vector2 a = p[i], b = p[(i + 1) % p.Count];
            float d = DistSeg(x, z, a.x, a.y, b.x, b.y);
            if (d < best) best = d;
        }
        return best;
    }

    public static bool Inside(float x, float z, IList<Vector2> p)
    {
        bool c = false;
        for (int i = 0, j = p.Count - 1; i < p.Count; j = i++)
        {
            if (((p[i].y > z) != (p[j].y > z)) && (x < (p[j].x - p[i].x) * (z - p[i].y) / (p[j].y - p[i].y) + p[i].x)) c = !c;
        }
        return c;
    }

    /// <summary>Jarak bertanda ke poligon tertutup: negatif di dalam.</summary>
    public static float SignedDist(float x, float z, IList<Vector2> p)
    {
        float d = DistRing(x, z, p);
        return Inside(x, z, p) ? -d : d;
    }

    public static float Area(IList<Vector2> p)
    {
        float a = 0f;
        for (int i = 0; i < p.Count; i++) { Vector2 u = p[i], v = p[(i + 1) % p.Count]; a += u.x * v.y - v.x * u.y; }
        return a * 0.5f;
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

    /// <summary>Titik pada polyline di jarak d dari awal (+ arah singgung).</summary>
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

    /// <summary>Jarak sepanjang polyline (dari awal) ke titik terdekat dengan (x,z).</summary>
    public static float ArcOf(float x, float z, IList<Vector2> p)
    {
        float best = 1e9f, arc = 0f, acc = 0f;
        for (int i = 0; i + 1 < p.Count; i++)
        {
            float vx = p[i + 1].x - p[i].x, vz = p[i + 1].y - p[i].y, l2 = vx * vx + vz * vz, sl = Mathf.Sqrt(l2);
            float t = l2 < 1e-6f ? 0f : Mathf.Clamp01(((x - p[i].x) * vx + (z - p[i].y) * vz) / l2);
            float qx = p[i].x + vx * t, qz = p[i].y + vz * t;
            float d = (qx - x) * (qx - x) + (qz - z) * (qz - z);
            if (d < best) { best = d; arc = acc + sl * t; }
            acc += sl;
        }
        return arc;
    }

    public static float YawTo(float x, float z, float tx, float tz) { return Mathf.Atan2(tx - x, tz - z) * Mathf.Rad2Deg; }

    // ------------------------------------------------------------------ persegi panjang berputar (petak/plaza)
    public static float DistToRect(float px, float pz, float cx, float cz, float yaw, float w, float d)
    {
        float a = yaw * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
        float dx = px - cx, dz = pz - cz;
        float lx = dx * c - dz * s, lz = dx * s + dz * c;
        float ex = Mathf.Max(0f, Mathf.Abs(lx) - w * 0.5f), ez = Mathf.Max(0f, Mathf.Abs(lz) - d * 0.5f);
        return Mathf.Sqrt(ex * ex + ez * ez);
    }

    public static float DistToRect(float px, float pz, Lot l) { return DistToRect(px, pz, l.x, l.z, l.yaw, l.w, l.d); }

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

    public static Vector2 ToWorld(float cx, float cz, float yaw, float lx, float lz)
    {
        float a = yaw * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
        return new Vector2(cx + lx * c + lz * s, cz - lx * s + lz * c);
    }

    /// <summary>Dua persegi panjang berputar (tepi dilebarkan margin) saling tumpang? (uji sumbu pemisah)</summary>
    public static bool RectsOverlap(float ax, float az, float ayaw, float aw, float ad, float bx, float bz, float byaw, float bw, float bd, float margin)
    {
        float[] ang = { ayaw, ayaw + 90f, byaw, byaw + 90f };
        float hax = aw * 0.5f + margin, haz = ad * 0.5f + margin, hbx = bw * 0.5f + margin, hbz = bd * 0.5f + margin;
        Vector2[] ca = Corners(ax, az, ayaw, hax, haz), cb = Corners(bx, bz, byaw, hbx, hbz);
        for (int k = 0; k < 4; k++)
        {
            float r = ang[k] * Mathf.Deg2Rad;
            float nx = Mathf.Sin(r), nz = Mathf.Cos(r);
            float a0 = 1e9f, a1 = -1e9f, b0 = 1e9f, b1 = -1e9f;
            for (int i = 0; i < 4; i++)
            {
                float pa = ca[i].x * nx + ca[i].y * nz, pb = cb[i].x * nx + cb[i].y * nz;
                if (pa < a0) a0 = pa; if (pa > a1) a1 = pa;
                if (pb < b0) b0 = pb; if (pb > b1) b1 = pb;
            }
            if (a1 < b0 || b1 < a0) return false;
        }
        return true;
    }

    static Vector2[] Corners(float cx, float cz, float yaw, float hx, float hz)
    {
        return new[] { ToWorld(cx, cz, yaw, -hx, -hz), ToWorld(cx, cz, yaw, hx, -hz), ToWorld(cx, cz, yaw, hx, hz), ToWorld(cx, cz, yaw, -hx, hz) };
    }

    // ------------------------------------------------------------------ medan
    public const float WaterLevel = -1.6f;                    // sama dengan VWaterLevel di builder
    public const float RiverBed = -3.3f;                      // dasar alur sungai (lihat Base)
    /// <summary>Kenaikan kota konsentris (n81): ladang rata (0) -> naik landai ke Tembok Tengah (+CityRise) -> bukit mahkota di dalam Tembok Dalam (+CrownRise).
    /// Kenaikan dimulai CityFar / CrownFar m di luar tembok dan selesai CityIn / CrownIn m di dalamnya. Parit tengah tetap kering karena tanah di sana sudah naik; parit luar menggenang.</summary>
    public const float CityRise = 6.0f, CityFar = 40f, CityIn = 12f;
    public const float CrownRise = 8.5f, CrownFar = 76f, CrownIn = 6f;
    /// <summary>Parit Tembok Luar dan Tembok Tengah (jarak dari garis tembok): tepi dalam, lereng, lantai, lebar lantai. Parit luar terisi air (tanah ladang rendah), parit tengah kering.</summary>
    public const float DitchNear = 8f, DitchRun = 7f, DitchFloor = 3f, DitchDepth = 3.0f;
    public const float DitchFarEdge = DitchNear + DitchRun * 2f + DitchFloor;
    /// <summary>Parit Tembok Dalam (selalu kering; tanah di sana tinggi).</summary>
    public const float KDitchNear = 6f, KDitchRun = 5f, KDitchFloor = 2f, KDitchDepth = 2.6f;
    public const float KDitchFarEdge = KDitchNear + KDitchRun * 2f + KDitchFloor;

    /// <summary>0 di luar 'far', 1 di dalam 'inn' (sd = jarak bertanda ke tembok, negatif di dalam); lereng hampir lurus dengan ujung dihaluskan.</summary>
    static float Rise(float sd, float far, float inn)
    {
        float t = Mathf.Clamp01((far - sd) / (far + inn));
        return Mathf.Lerp(t, t * t * (3f - 2f * t), 0.4f);
    }

    public static float RiverHalfWidth(float x, float z) { return 6.4f + 1.2f * Nz(x, z, 0.02f, 71); }

    /// <summary>Profil parit 0..1 (1 = dasar) pada jarak d dari garis tembok: lereng hampir lurus (maks ~27 derajat) agar bisa didaki pemain.</summary>
    public static float DitchProfile(float d, float near, float run, float floor)
    {
        float up = Mathf.Clamp01((d - near) / run), dn = Mathf.Clamp01((near + run + floor + run - d) / run);
        float t = Mathf.Min(up, dn);
        return Mathf.Lerp(t, t * t * (3f - 2f * t), 0.4f);
    }

    /// <summary>0..1: seberapa dekat ke sungai (1 di dasar alur). Dipakai cat tanah (lumpur tepi) dan penempatan.</summary>
    public static float RiverBank(float x, float z)
    {
        if (River == null) Build();
        if (x <= rx0 || x >= rx1 || z <= rz0 || z >= rz1) return 0f;
        float hw = RiverHalfWidth(x, z);
        float d = DistPoly(x, z, River);
        return Sm(1f - (d - (hw + 1.5f)) / 5f);
    }

    /// <summary>Teras rata (alun-alun kota atas, parvis, pusat taman): dataran persegi dengan lereng halus ke medan sekitarnya. Rumah & jalan memakai tinggi ini juga.</summary>
    public class Terrace { public float x, z, yaw, w, d, py, blend; }
    public static readonly List<Terrace> Terraces = new List<Terrace>();

    /// <summary>Medan dasar + teras alun-alun (tanpa pad bangunan).</summary>
    public static float Base(float x, float z)
    {
        float h = BaseRaw(x, z);
        for (int i = 0; i < Terraces.Count; i++)
        {
            var t = Terraces[i];
            float reach = (t.w + t.d) * 0.5f + t.blend + 4f;
            if (Mathf.Abs(x - t.x) > reach || Mathf.Abs(z - t.z) > reach) continue;
            float dd = DistToRect(x, z, t.x, t.z, t.yaw, t.w, t.d) - 1f;
            float wgt = Sm(1f - dd / t.blend);
            if (wgt > 0f) h = Mathf.Lerp(h, t.py, wgt);
        }
        return h;
    }

    /// <summary>Medan dasar tanpa pad bangunan: ladang rata, kenaikan landai ke kota tengah, bukit mahkota, perbukitan di luar tembok, gunung pembatas, parit tiga cincin, alur sungai.</summary>
    public static float BaseRaw(float x, float z)
    {
        if (River == null) Build();
        float so = SdOuter(x, z), sm = SdMid(x, z), si = SdInner(x, z);
        float h = CityRise * Rise(sm, CityFar, CityIn) + CrownRise * Rise(si, CrownFar, CrownIn);
        // relief halus (sangat kecil di dalam tembok agar jalan & bangunan rata)
        float inside = Sm(1f - (so + 6f) / 34f);                           // 1 di dalam tembok luar, 0 jauh di luar
        float n1 = (Nz(x, z, 0.012f, 3) - 0.5f) * 2f, n2 = (Nz(x, z, 0.04f, 9) - 0.5f) * 2f;
        h += (n1 * 0.10f + n2 * 0.05f) * inside;
        // di luar tembok: perbukitan + gunung pembatas (jalan keluar tetap celah rata)
        float outside = 1f - inside;
        if (outside > 0.001f)
        {
            float edge = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
            float rid = 1f - Mathf.Abs(2f * Nz(x, z, 0.011f, 21) - 1f);
            float hill = (n1 * 4.6f + n2 * 1.6f + (Nz(x, z, 0.11f, 5) - 0.5f) * 0.7f) * Sm((so - 45f) / 90f);          // 45 m tepi tembok datar (glacis), baru berbukit
            float mount = Sm((edge - 395f) / 75f) * (24f + 26f * rid * rid + 8f * Nz(x, z, 0.03f, 2));
            float high = (hill + mount) * outside;
            float dR = 1e9f;
            for (int i = 0; i < OutRoads.Count; i++) dR = Mathf.Min(dR, DistPoly(x, z, OutRoads[i]));
            float pass = Mathf.Lerp(26f, 12f, Sm((edge - 340f) / 90f));
            high *= Sm((dR - 6f) / pass);
            h += high;
        }
        // parit di luar Tembok Luar (berair), Tembok Tengah (kering), Tembok Dalam (kering)
        if (so > DitchNear - 1f && so < DitchFarEdge + 1f) h -= DitchDepth * DitchProfile(so, DitchNear, DitchRun, DitchFloor);
        if (sm > DitchNear - 1f && sm < DitchFarEdge + 1f) h -= DitchDepth * DitchProfile(sm, DitchNear, DitchRun, DitchFloor);
        if (si > KDitchNear - 1f && si < KDitchFarEdge + 1f) h -= KDitchDepth * DitchProfile(si, KDitchNear, KDitchRun, KDitchFloor);
        // sungai
        if (x > rx0 && x < rx1 && z > rz0 && z < rz1)
        {
            float d = DistPoly(x, z, River);
            float hw = RiverHalfWidth(x, z);
            float core = hw * 0.62f, bank = 6.0f;
            float f = Sm(1f - (d - core) / bank);
            h = Mathf.Lerp(h, RiverBed, f);
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
            float reach = (l.w > l.d ? l.w : l.d) + 22f;
            if (Mathf.Abs(x - l.x) > reach || Mathf.Abs(z - l.z) > reach) continue;
            float dd = DistToRect(x, z, l) - 1.0f;
            float wgt = Sm(1f - dd / l.blend);
            if (wgt > 0f) h = Mathf.Lerp(h, l.py, wgt);
        }
        return h;
    }
}
