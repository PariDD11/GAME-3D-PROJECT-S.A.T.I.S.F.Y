using System.Collections.Generic;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Kanon Gurun Zahar (HuntArea_Desert; lihat Docs/LORE_BIBLE.md §8.4). Dipanggil dari BuildDesert() sesudah BuildDesertRocks() dan
/// TIDAK memakai UnityEngine.Random, supaya sebaran pohon, batu, sarang dan monster yang sudah ada tidak bergeser.
///
///  1. Kuil Kaca di tepi barat dataran Lembah Kuil: aula bersisi delapan berdinding kaca, tujuh relung lengkung (enam berpatung dan bercahaya
///     lembut, satu kosong dan polos), lantai bersih tanpa pasir, satu pintu. (file ini + .Statues.cs)
///  2. Dua belas sapu bersandar pada tonggak batu, melingkari kuil, semuanya condong ke dalam; jejak kaki dua belas orang mengitari
///     dinding tetapi tak ada yang naik ke tangga. (.Ring.cs)
///  3. Tugu Gerbang Pagi di mulut jalan dari kota, kemah di dasar sungai kering (Wadi Kering) dan surat yang tak terkirim di Ngarai Merah. (.Sites.cs)
///
/// Tidak ada penyapu (NPC) atau mekanik runtime di sini; hanya benda, jejak, dan teks. Teks tidak memakai "yang ketujuh", Jeda, si Sulung, atau Lucifer.
/// </summary>
public static partial class HuntSceneBuilder
{
    // ------------------------------------------------------------------ data kanon
    static readonly Vector2 DcSite = new Vector2(-8f, 108f);      // pusat Kuil Kaca (koordinat desain), tepi barat dataran Lembah Kuil
    const float DcAi = 9.0f;          // jarak pusat ke bidang dalam dinding (apotema dalam)
    const float DcT = 1.9f;           // tebal dinding
    const float DcH = 8.4f;           // tinggi dinding
    const float DcDn = 1.15f;         // kedalaman relung
    const float DcPlinth = 12.9f;     // jari-jari alas bundar
    static readonly string[] DcNama = { "Kosong", "Emas", "BiruMuda", "HijauMuda", "MerahMuda", "Ungu", "Jingga" };
    static readonly Color[] DcCahaya =                              // urutan kanon enam cahaya, searah jarum jam dari relung kosong
    {
        new Color(0f, 0f, 0f, 1f),
        new Color(1.00f, 0.80f, 0.30f, 1f),
        new Color(0.55f, 0.80f, 1.00f, 1f),
        new Color(0.60f, 1.00f, 0.55f, 1f),
        new Color(1.00f, 0.60f, 0.72f, 1f),
        new Color(0.72f, 0.50f, 1.00f, 1f),
        new Color(1.00f, 0.58f, 0.22f, 1f),
    };
    static readonly float[] DcTerang = { 0f, 1.0f, 1.0f, 1.0f, 1.0f, 0.6f, 1.0f };   // yang memegang lilin paling redup ("paling sedikit diingat")

    // bingkai Kuil Kaca: pusat lantai (dunia), arah pintu (+Z lokal) dan alas
    static Vector3 dcC;
    static float dcYaw, dcLowRel;
    static Quaternion dcRot;

    /// <summary>Acak deterministik 0..1 (tanpa UnityEngine.Random).</summary>
    static float DcHash(int a, int b)
    {
        unchecked
        {
            int h = a * 374761393 + b * 668265263 + 977;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0xffffff) / (float)0x1000000;
        }
    }

    static void DesertCanon()
    {
        var root = Group("KanonGurun");
        DcPrepareSite();
        DcHall(root);
        DcRing(root);
        DcGateStele(root);
        DcWadiCamp(root);
        DcLetter(root);
    }

    // ------------------------------------------------------------------ bantu
    /// <summary>Titik tanah (dunia) pada koordinat lokal kuil (lx, lz); +Z lokal = arah pintu.</summary>
    static Vector3 DcP(float lx, float lz)
    {
        Vector3 p = dcC + dcRot * new Vector3(lx, 0f, lz);
        p.y = GroundYW(p.x, p.z);
        return p;
    }

    /// <summary>Titik tanah (dunia) pada sudut polar lokal (0 = pintu, searah jarum jam dari atas) dan jari-jari r.</summary>
    static Vector3 DcPolar(float r, float angDeg)
    {
        float a = angDeg * Mathf.Deg2Rad;
        return DcP(Mathf.Sin(a) * r, Mathf.Cos(a) * r);
    }

    static float DcYawOf(Vector3 dir) { return Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg; }

    /// <summary>Segiempat yang dijamin menghadap arah want (tanpa harus menghitung urutan titik).</summary>
    static void DcQ(GeoBag b, int s, Vector3 a, Vector3 bb, Vector3 c, Vector3 d, Vector3 want)
    {
        if (Vector3.Dot(Vector3.Cross(bb - a, c - a), want) < 0f) b.Quad(s, d, c, bb, a); else b.Quad(s, a, bb, c, d);
    }

    static void DcT3(GeoBag b, int s, Vector3 a, Vector3 bb, Vector3 c, Vector3 want)
    {
        if (Vector3.Dot(Vector3.Cross(bb - a, c - a), want) < 0f) b.Tri(s, a, c, bb); else b.Tri(s, a, bb, c);
    }

    /// <summary>Tinggi lengkung lancip pada posisi x: y = ys di x = +-w (garis pegas) dan ys + rise di puncak.</summary>
    static float DcArchY(float x, float w, float ys, float rise)
    {
        return ys + rise * Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(x) / w), 0.62f);
    }

    static Material DcMat(string name, Color c, float smooth)
    {
        var m = Mat(name, c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        return m;
    }

    static Material DcGlowMat(int i, float k)
    {
        Color c = DcCahaya[i];
        var m = Mat("GurunCeruk_" + DcNama[i], new Color(c.r * 0.45f, c.g * 0.45f, c.b * 0.45f, 1f));
        return TownMats.Emissive(m, new Color(c.r * k, c.g * k, c.b * k, 1f));
    }

    static Material[] DcSlots()
    {
        var s = (Material[])ForestKit.DSlots.Clone();
        var kaca = DcMat("GurunKacaKuil", new Color(0.62f, 0.80f, 0.82f, 1f), 0.88f);
        s[Sl.Glass] = TownMats.Emissive(kaca, new Color(0.05f, 0.09f, 0.10f, 1f));       // dingin dan tidak menyilaukan
        return s;
    }

    /// <summary>Nomor cahaya (1..6) untuk sisi f; 0 = relung kosong; -1 = pintu. Searah jarum jam dari relung kosong: emas ... jingga.</summary>
    static int DcCerukOf(int f)
    {
        switch (f)
        {
            case 4: return 0;
            case 5: return 1;
            case 6: return 2;
            case 7: return 3;
            case 1: return 4;
            case 2: return 5;
            case 3: return 6;
        }
        return -1;
    }

    /// <summary>Buang pohon terrain, batu, simpul panen dan rumput kecil hasil sebar acak dalam radius r (dunia) dari titik w.</summary>
    static void DcClear(Vector3 w, float r)
    {
        if (td != null && td.treeInstances != null && td.treeInstances.Length > 0)
        {
            var keep = new List<TreeInstance>();
            foreach (var t in td.treeInstances)
            {
                float wx = Origin + t.position.x * td.size.x, wz = Origin + t.position.z * td.size.z;
                if ((wx - w.x) * (wx - w.x) + (wz - w.z) * (wz - w.z) >= r * r) keep.Add(t);
            }
            td.treeInstances = keep.ToArray();
        }
        foreach (var grp in new[] { tRocks, tNodes })
        {
            if (grp == null) continue;
            for (int i = grp.childCount - 1; i >= 0; i--)
            {
                var t = grp.GetChild(i);
                float dx = t.position.x - w.x, dz = t.position.z - w.z;
                if (dx * dx + dz * dz < r * r) Object.DestroyImmediate(t.gameObject);
            }
        }
        if (td != null && td.detailPrototypes != null && td.detailPrototypes.Length > 0 && td.detailResolution > 0)
        {
            int res = td.detailResolution;
            float cell = td.size.x / res;
            int x0 = Mathf.Clamp(Mathf.FloorToInt((w.x - r - Origin) / cell), 0, res - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt((w.x + r - Origin) / cell), 0, res - 1);
            int z0 = Mathf.Clamp(Mathf.FloorToInt((w.z - r - Origin) / cell), 0, res - 1);
            int z1 = Mathf.Clamp(Mathf.CeilToInt((w.z + r - Origin) / cell), 0, res - 1);
            int cw = x1 - x0 + 1, chh = z1 - z0 + 1;
            for (int layer = 0; layer < td.detailPrototypes.Length; layer++)
            {
                var map = td.GetDetailLayer(x0, z0, cw, chh, layer);
                for (int zz = 0; zz < chh; zz++)
                    for (int xx = 0; xx < cw; xx++)
                    {
                        float px = Origin + (x0 + xx + 0.5f) * cell, pz = Origin + (z0 + zz + 0.5f) * cell;
                        if ((px - w.x) * (px - w.x) + (pz - w.z) * (pz - w.z) < r * r) map[zz, xx] = 0;
                    }
                td.SetDetailLayer(x0, z0, layer, map);
            }
        }
    }

    /// <summary>Hitung bingkai Kuil Kaca (lantai cukup tinggi agar tak terkubur) dan bersihkan sebar acak di sekitarnya.</summary>
    static void DcPrepareSite()
    {
        Vector3 c = G(DcSite.x, DcSite.y);
        Vector3 toward = G(28f, 64f) - c; toward.y = 0f;                      // pintu menghadap jalan prosesi Kuil Agung
        dcYaw = DcYawOf(toward);
        dcRot = Quaternion.Euler(0f, dcYaw, 0f);
        float hi = -1e9f, lo = 1e9f;
        for (int r = 0; r < 3; r++)
            for (int k = 0; k < (r == 0 ? 1 : 12); k++)
            {
                float rad = r * 6.2f, a = k / 12f * Mathf.PI * 2f;
                float y = GroundYW(c.x + Mathf.Sin(a) * rad, c.z + Mathf.Cos(a) * rad);
                hi = Mathf.Max(hi, y); lo = Mathf.Min(lo, y);
            }
        float floorY = hi + 0.14f;
        dcC = new Vector3(c.x, floorY, c.z);
        dcLowRel = lo - floorY - 0.4f;
        DcClear(c, 19.5f);
    }

    // ================================================================== 1. Kuil Kaca
    static void DcHall(Transform root)
    {
        var hall = new GameObject("KuilKaca").transform;
        hall.SetParent(root, false);
        hall.position = dcC;
        hall.rotation = dcRot;
        var slots = DcSlots();
        DcFloor(hall, slots);
        DcWalls(hall, slots);
        for (int f = 1; f < 8; f++) DcNiche(hall, f);
        DcHallInfo(hall);
    }

    /// <summary>Alas bundar dua tingkat, lantai dalam yang bersih, tangga di depan pintu. Dengan MeshCollider.</summary>
    static void DcFloor(Transform hall, Material[] slots)
    {
        var b = new GeoBag();
        b.Cyl(Sl.Stone, new Vector3(0f, dcLowRel, 0f), DcPlinth, -0.22f - dcLowRel, 44, DcPlinth - 0.2f, true, false);
        b.Cyl(Sl.Marble, new Vector3(0f, -0.24f, 0f), 12.1f, 0.22f, 44, 12.0f, true, false);
        b.Disc(Sl.Marble, Vector3.zero, 9.82f, 40, 0.0f);
        b.Annulus(Sl.Stone, Vector3.zero, 1.05f, 1.28f, 0f, 360f, 36, 0.03f);
        // tangga di depan pintu: dari tanah ke lantai
        Vector3 dp = DcP(0f, 13.6f);
        float g0 = dp.y - dcC.y;
        int n = Mathf.Clamp(Mathf.CeilToInt((-0.02f - g0 + 0.1f) / 0.2f), 1, 7);
        float yBase = g0 - 0.1f, rise = Mathf.Max(0.06f, (-0.02f - yBase) / n), run = 0.55f;
        b.Stairs(Sl.Marble, new Vector3(0f, yBase, 12.0f + (n - 0.5f) * run), 5.4f, n, rise, run, 0f);
        dcStairs = n;
        var go = b.Emit(hall, "AlasDanLantai", slots);
        var mf = go.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null) go.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
    }
    static int dcStairs;

    /// <summary>Delapan sisi: tujuh relung lengkung + satu pintu; pilaster sudut, cornice. Satu mesh dengan MeshCollider.</summary>
    static void DcWalls(Transform hall, Material[] slots)
    {
        var b = new GeoBag();
        float Ao = DcAi + DcT;
        float s = 2f * Ao * Mathf.Tan(22.5f * Mathf.Deg2Rad);
        for (int f = 0; f < 8; f++)
        {
            float phi = f * 45f, ph = phi * Mathf.Deg2Rad;
            b.Push(new Vector3(Mathf.Sin(ph) * DcAi, 0f, Mathf.Cos(ph) * DcAi), phi);        // +Z lokal sisi = keluar; bidang dalam di z = 0
            if (f == 0)
            {
                DcArchFace(b, s, DcH, DcT, DcT, 1.85f, 4.6f, 1.5f, 0f);
                DcTrim(b, 1.85f, 4.6f, 1.5f, 0f, -0.07f);
                DcTrim(b, 1.85f, 4.6f, 1.5f, 0f, DcT + 0.07f);
            }
            else
            {
                DcArchFace(b, s, DcH, DcT, DcDn, 1.75f, 5.2f, 1.6f, 0.7f);
                DcTrim(b, 1.75f, 5.2f, 1.6f, 0.7f, -0.07f);
                // sisi luar: pita datar dan sambungan panel kaca
                b.Box(Sl.Marble, new Vector3(0f, DcH * 0.62f, DcT + 0.02f), new Vector3(s - 0.1f, 0.22f, 0.07f));
                foreach (float sx in new[] { -3.0f, -1.5f, 1.5f, 3.0f })
                    b.Box(Sl.DarkStone, new Vector3(sx, DcH * 0.5f, DcT + 0.015f), new Vector3(0.05f, DcH - 0.9f, 0.04f));
            }
            b.Box(Sl.Marble, new Vector3(0f, DcH + 0.15f, DcT / 2f - 0.05f), new Vector3(s + 0.2f, 0.30f, DcT + 0.5f));      // cornice
            b.Pop();
        }
        float Ro = Ao / Mathf.Cos(22.5f * Mathf.Deg2Rad), Ri = DcAi / Mathf.Cos(22.5f * Mathf.Deg2Rad);
        for (int k = 0; k < 8; k++)
        {
            float tdeg = 22.5f + 45f * k, th = tdeg * Mathf.Deg2Rad;
            // pilaster sudut di sisi dalam: memecah dinding kaca polos dan menutup sambungan sisi
            b.Box(Sl.Marble, new Vector3(Mathf.Sin(th) * (Ri - 0.08f), (DcH - 0.2f) / 2f, Mathf.Cos(th) * (Ri - 0.08f)), new Vector3(0.42f, DcH - 0.2f, 0.42f), tdeg);
            Vector3 cp = new Vector3(Mathf.Sin(th) * Ro, 0f, Mathf.Cos(th) * Ro);
            b.Box(Sl.Stone, cp + new Vector3(0f, (DcH + 0.55f) / 2f, 0f), new Vector3(0.62f, DcH + 0.55f, 0.62f), tdeg);
            b.Box(Sl.Marble, cp + new Vector3(0f, DcH + 0.55f + 0.16f, 0f), new Vector3(0.92f, 0.32f, 0.92f), tdeg);
        }
        var go = b.Emit(hall, "Dinding", slots);
        var mf = go.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null) go.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
    }

    /// <summary>
    /// Satu sisi dinding dengan bukaan lengkung (relung bila dn &lt; t, pintu tembus bila dn = t). Bingkai sisi: bidang dalam di z = 0
    /// (menghadap -Z, ke pusat), dinding menebal ke +Z. s = lebar sisi luar, w = setengah lebar bukaan, ys = garis pegas, rise = tinggi lengkung.
    /// </summary>
    static void DcArchFace(GeoBag b, float s, float H, float t, float dn, float w, float ys, float rise, float sill)
    {
        bool through = dn >= t - 0.001f;
        float half = s / 2f, px = (half + w) / 2f, pw = half - w;
        if (!through) b.Box(Sl.Glass, new Vector3(0f, H / 2f, dn + (t - dn) / 2f), new Vector3(s, H, t - dn));      // dinding luar + dasar relung
        b.Box(Sl.Glass, new Vector3(-px, H / 2f, dn / 2f), new Vector3(pw, H, dn));
        b.Box(Sl.Glass, new Vector3(px, H / 2f, dn / 2f), new Vector3(pw, H, dn));
        if (sill > 0.01f) b.Box(Sl.Marble, new Vector3(0f, sill / 2f, dn / 2f), new Vector3(2f * w, sill, dn));       // lantai relung
        const int N = 14;
        float xp = -w, yp = DcArchY(-w, w, ys, rise);
        for (int i = 1; i <= N; i++)
        {
            float x = -w + 2f * w * i / N, y = DcArchY(x, w, ys, rise);
            DcQ(b, Sl.Stone, new Vector3(xp, yp, 0f), new Vector3(x, y, 0f), new Vector3(x, H, 0f), new Vector3(xp, H, 0f), Vector3.back);
            Vector3 mid = new Vector3((xp + x) / 2f, (yp + y) / 2f, 0f);
            DcQ(b, Sl.Stone, new Vector3(xp, yp, 0f), new Vector3(x, y, 0f), new Vector3(x, y, dn), new Vector3(xp, yp, dn), new Vector3(-mid.x, ys - mid.y, 0f));
            if (!through) DcQ(b, Sl.Glass, new Vector3(xp, yp, dn), new Vector3(x, y, dn), new Vector3(x, H, dn), new Vector3(xp, H, dn), Vector3.back);
            xp = x; yp = y;
        }
    }

    /// <summary>Bingkai lengkung menonjol (balok tipis di sepanjang lengkung dan sisi tegak) pada bidang z = z0.</summary>
    static void DcTrim(GeoBag b, float w, float ys, float rise, float sill, float z0)
    {
        const int N = 14;
        float xp = -w, yp = DcArchY(-w, w, ys, rise);
        for (int i = 1; i <= N; i++)
        {
            float x = -w + 2f * w * i / N, y = DcArchY(x, w, ys, rise);
            b.Beam(Sl.Marble, new Vector3(xp, yp + 0.02f, z0), new Vector3(x, y + 0.02f, z0), 0.30f, 0.14f, Vector3.forward);
            xp = x; yp = y;
        }
        foreach (float sx in new[] { -w - 0.12f, w + 0.12f })
            b.Beam(Sl.Marble, new Vector3(sx, sill, z0), new Vector3(sx, ys + 0.02f, z0), 0.30f, 0.14f, Vector3.forward);
    }

    /// <summary>Panel cahaya berbentuk relung (persegi + lengkung) pada bidang z di bingkai sisi, menghadap pusat.</summary>
    static void DcArchPanel(GeoBag b, int slot, float w, float y0, float ys, float rise, float z)
    {
        DcQ(b, slot, new Vector3(-w, y0, z), new Vector3(w, y0, z), new Vector3(w, ys, z), new Vector3(-w, ys, z), Vector3.back);
        const int N = 10;
        float xp = -w, yp = ys;
        for (int i = 1; i <= N; i++)
        {
            float x = -w + 2f * w * i / N, y = DcArchY(x, w, ys, rise);
            DcT3(b, slot, new Vector3(0f, ys, z), new Vector3(xp, yp, z), new Vector3(x, y, z), Vector3.back);
            xp = x; yp = y;
        }
    }

    /// <summary>Isi satu relung: panel cahaya dan patung (bila berisi), sinar di lantai ke arah relung, lampu titik lembut.</summary>
    static void DcNiche(Transform hall, int f)
    {
        int ci = DcCerukOf(f);
        float phi = f * 45f, ph = phi * Mathf.Deg2Rad;
        Vector3 axis = new Vector3(Mathf.Sin(ph), 0f, Mathf.Cos(ph)), perp = new Vector3(axis.z, 0f, -axis.x);
        var b = new GeoBag();
        if (ci > 0)
        {
            b.Push(axis * DcAi, phi);
            DcArchPanel(b, Sl.Glow, 1.45f, 1.05f, 4.95f, 1.15f, DcDn - 0.03f);
            b.Pop();
            Vector3 n0 = axis * 1.45f, n1 = axis * (DcAi - 1.0f);
            DcQ(b, Sl.Glow, n0 - perp * 0.12f, n0 + perp * 0.12f, n1 + perp * 0.12f, n1 - perp * 0.12f, Vector3.up);
            b.Push(axis * (DcAi + DcDn * 0.5f) + new Vector3(0f, 0.7f, 0f), phi + 180f);
            DcStatue(b, ci);
            b.Pop();
        }
        else
        {
            // relung kosong: lantai relung polos dan satu lingkaran pucat, bekas sesuatu yang pernah berdiri di situ; sinar lantai padam
            Vector3 c0 = axis * (DcAi + DcDn * 0.5f);
            b.Disc(Sl.Plaster, new Vector3(c0.x, 0f, c0.z), 0.95f, 20, 0.7f + 0.004f);
            Vector3 n0 = axis * 1.45f, n1 = axis * (DcAi - 1.0f);
            DcQ(b, Sl.DarkStone, n0 - perp * 0.12f, n0 + perp * 0.12f, n1 + perp * 0.12f, n1 - perp * 0.12f, Vector3.up);
        }
        var slots = DcSlots();
        if (ci > 0) slots[Sl.Glow] = DcGlowMat(ci, 0.65f);
        b.Emit(hall, "Relung_" + f + "_" + DcNama[ci], slots);
        if (ci > 0)
        {
            var lg = new GameObject("Cahaya_" + DcNama[ci]);
            lg.transform.SetParent(hall, false);
            lg.transform.localPosition = axis * (DcAi - 1.3f) + new Vector3(0f, 3.4f, 0f);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point; l.range = 10.5f; l.intensity = 1.0f * DcTerang[ci]; l.color = DcCahaya[ci];
        }
    }

    static InfoPoint DcInfo(Transform parent, string name, Vector3 world, string prompt, float radius, string title, string message)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = world;
        var ip = go.AddComponent<InfoPoint>();
        ip.prompt = prompt; ip.radius = radius; ip.title = title; ip.message = message;
        return ip;
    }

    static void DcHallInfo(Transform hall)
    {
        Vector3 door = DcP(0f, 15.4f);
        DcInfo(hall, "InteractPoint_KuilKaca", door, "Periksa kuil berdinding kaca", 5.5f, "Kuil Kaca",
            "Dinding kuil ini licin dan jernih seperti kaca. Langit gurun tampak berenang di dalamnya sampai ke puncak, dan delapan sisinya mengelilingi lantai yang bersih: tak ada sebutir pasir pun, padahal angin di luar tak pernah berhenti.\n\n" +
            "Di sisi dalam berderet relung tinggi. Enam berisi patung dewa berukuran raksasa, masing-masing diterangi cahaya lembut yang warnanya lain. Satu relung dibiarkan kosong. Patung yang memegang lilin kecil bercahaya paling redup, dan lantai di kakinya paling mengilap, seakan lebih sering diusap daripada yang lain.\n\n" +
            "Kafilah yang singgah di oasis bercerita bahwa kuil ini disapu dua belas orang tiap pagi, turun-temurun, dan tak seorang pun ingat mengapa.");
        Vector3 inner = DcP(0f, -(DcAi - 3.3f));
        inner.y = dcC.y;
        DcInfo(hall, "InteractPoint_RelungKosong", inner, "Periksa relung yang kosong", 3.8f, "Relung yang Dikerat",
            "Relung ini setinggi dan selebar yang lain, tetapi dindingnya halus seperti telapak tangan: tanpa ukiran, tanpa debu, tanpa cahaya. Di dasar relung-relung lain masih terbaca tulisan kuno; di sini seseorang pernah mengerat semuanya sampai batunya rata.\n\n" +
            "Di lantai hanya tersisa lingkaran pucat, bekas sesuatu yang pernah berdiri di situ. Orang-orang oasis tak mau menyebut apa yang dulu ada di sini, dan tak ada yang berani menuliskannya.");
    }
}
