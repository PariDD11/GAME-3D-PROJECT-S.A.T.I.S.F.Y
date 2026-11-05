using System.Collections.Generic;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Kanon Hutan Lerai (HuntArea_G; lihat Docs/LORE_BIBLE.md §8.3). Dipanggil dari Build() sesudah BuildRocks() dan
/// TIDAK memakai UnityEngine.Random, supaya urutan acak pohon, batu, sarang dan monster yang sudah ada tidak bergeser.
///
///  1. Kuil tua di jantung hutan: tujuh tiang batu melingkari Pohon Kuno. Tiap tiang punya ceruk setinggi dada.
///     Enam ceruk berisi batu cahaya lembut (emas, biru muda, hijau muda, merah muda, ungu, jingga); satu ceruk kosong dan bersih.
///     Dinding di antara tiang tinggal serpihan setinggi lutut; lantai batu pecah dan puing mengelilingi akar pohon.
///  2. Ukiran tiga goresan pada batang Pohon Kuno (sisi jalan) + prasasti kodeks "kutukan_hutan".
///  3. Tiga pohon mati penanda di tepi jalan utama, masing-masing bergores tiga garis, dengan tali merah Guild
///     (batas jarak teraman Rank G) yang putus satu bentang; kedua ujungnya masih berembun.
///
/// Tidak ada mimpi/bisikan runtime di sini (hanya penanda dan teks); sarang bandit Lembah Rorik tidak disentuh.
/// </summary>
public static partial class HuntSceneBuilder
{
    // ------------------------------------------------------------------ data kanon
    const float FcRing = 13.5f;                                                        // jari-jari lingkaran tiang (m, koordinat desain)
    static readonly float[] FcPillarH = { 3.3f, 4.2f, 3.2f, 3.9f, 3.0f, 4.1f, 3.5f };   // tinggi sisa tiap tiang
    static readonly string[] FcCeruk = { "Kosong", "Emas", "BiruMuda", "HijauMuda", "MerahMuda", "Ungu", "Jingga" };
    static readonly Color[] FcCahaya =                                                  // urutan kanon enam cahaya; indeks 0 = ceruk kosong
    {
        new Color(0f, 0f, 0f, 1f),
        new Color(1.00f, 0.80f, 0.30f, 1f),
        new Color(0.55f, 0.80f, 1.00f, 1f),
        new Color(0.60f, 1.00f, 0.55f, 1f),
        new Color(1.00f, 0.60f, 0.72f, 1f),
        new Color(0.72f, 0.50f, 1.00f, 1f),
        new Color(1.00f, 0.58f, 0.22f, 1f),
    };

    /// <summary>Acak deterministik 0..1 (tanpa UnityEngine.Random).</summary>
    static float FcHash(int a, int b)
    {
        unchecked
        {
            int h = a * 374761393 + b * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0xffffff) / (float)0x1000000;
        }
    }

    static void ForestCanon()
    {
        var root = Group("KanonHutan");
        FcTemple(root);
        FcTrunkMark(root);
        FcTrailMarkers(root);
    }

    // ------------------------------------------------------------------ bantu
    /// <summary>Titik desain pada lingkaran tiang untuk sudut tertentu (0 = utara desain, searah jarum jam).</summary>
    static Vector2 FcRingPt(float angDeg)
    {
        float a = angDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(a) * FcRing, Mathf.Cos(a) * FcRing);
    }

    static float FcYawOf(Vector3 worldDir)
    {
        return Mathf.Atan2(worldDir.x, worldDir.z) * Mathf.Rad2Deg;
    }

    /// <summary>Buang pohon, semak, dan batu hasil sebar acak yang berada dalam radius r (dunia) dari titik w.</summary>
    static void FcClearAround(Vector3 w, float r)
    {
        foreach (string gname in new[] { "Pohon", "Semak", "Rocks" })
        {
            var grp = tRoot.Find(gname);
            if (grp == null) continue;
            for (int i = grp.childCount - 1; i >= 0; i--)
            {
                var t = grp.GetChild(i);
                float dx = t.position.x - w.x, dz = t.position.z - w.z;
                if (dx * dx + dz * dz < r * r) Object.DestroyImmediate(t.gameObject);
            }
        }
    }

    static Material FcGlowMat(int i)
    {
        Color c = FcCahaya[i];
        var m = Mat("KanonCeruk_" + FcCeruk[i], new Color(c.r * 0.55f, c.g * 0.55f, c.b * 0.55f, 1f));
        return TownMats.Emissive(m, new Color(c.r * 1.25f, c.g * 1.25f, c.b * 1.25f, 1f));
    }

    // ================================================================== 1. kuil tua, tujuh tiang
    static void FcTemple(Transform root)
    {
        var g = new GameObject("KuilTua_TujuhTiang").transform;
        g.SetParent(root, false);
        var angs = new float[7];
        for (int i = 0; i < 7; i++) angs[i] = 12f + i * (360f / 7f);       // celah di selatan (jalan masuk) dan utara (jalan keluar)
        FcFloor(g, angs);
        FcWalls(g, angs);
        for (int i = 0; i < 7; i++) FcPillar(g, i, angs[i]);
    }

    /// <summary>Lantai batu pecah (dua-tiga cincin, sebagian hilang) + sisa pecahan batu. Satu mesh dalam koordinat dunia, tanpa collider.</summary>
    static void FcFloor(Transform parent, float[] angs)
    {
        var b = new GeoBag();
        int made = 0;
        for (int ring = 0; ring < 3; ring++)
        {
            float r0 = 8.0f + ring * 2.1f;                                  // mulai di luar akar papan Pohon Kuno (sampai ~7 m)
            int cnt = 9 + ring * 4;
            for (int k = 0; k < cnt; k++)
            {
                float h1 = FcHash(ring * 101 + k, 11), h2 = FcHash(ring * 57 + k, 23), h3 = FcHash(ring * 29 + k, 37), h4 = FcHash(ring * 13 + k, 41);
                if (h1 < 0.32f + ring * 0.08f) continue;                      // lempeng hilang atau tertimbun
                float aa = (k + (h2 - 0.5f) * 0.6f) / cnt * 360f * Mathf.Deg2Rad;
                float rr = r0 + (h3 - 0.5f) * 1.1f;
                Vector2 d = new Vector2(Mathf.Sin(aa) * rr, Mathf.Cos(aa) * rr);
                bool nearPillar = false;
                for (int j = 0; j < angs.Length; j++) if ((FcRingPt(angs[j]) - d).magnitude < 2.2f) nearPillar = true;
                if (nearPillar) continue;
                Vector3 p = G(d.x, d.y);
                float w = 0.9f + h3 * 0.9f, dd = 0.8f + h4 * 0.7f;
                float top = p.y + 0.04f + h1 * 0.06f;
                b.Box(h4 < 0.5f ? Sl.Pave : Sl.Stone, new Vector3(p.x, top - 0.25f, p.z), new Vector3(w, 0.5f, dd), h2 * 360f);
                if (h3 > 0.62f)
                    ForestKit.Blob(b, ForestKit.Moss, new Vector3(p.x + (h1 - 0.5f) * 0.4f, top + 0.01f, p.z + (h4 - 0.5f) * 0.3f),
                        new Vector3(0.38f, 0.07f, 0.30f), made + 5, 0.3f, 3, 7, 0.4f);
                made++;
            }
        }
        // timbunan pecahan di kaki tiang yang patah dan di antara lempeng
        for (int i = 0; i < angs.Length; i++)
        {
            if (FcPillarH[i] > 3.4f) continue;
            Vector2 d = FcRingPt(angs[i]) * 0.86f;
            ForestKit.Rubble(b, G(d.x, d.y), 1.5f, 7, 400 + i);
        }
        var go = b.Emit(parent, "LantaiDanPuing", ForestKit.Slots);
        go.transform.position = Vector3.zero;
    }

    /// <summary>Sisa dinding setinggi lutut di antara tiang (celah selatan dan utara dibiarkan terbuka untuk jalan) + drum tiang yang jatuh. Dengan MeshCollider.</summary>
    static void FcWalls(Transform parent, float[] angs)
    {
        var b = new GeoBag();
        int[] gapStart = { 0, 1, 2, 4, 5 };                                    // 3-4 = celah selatan, 6-0 = celah utara
        foreach (int i in gapStart)
        {
            Vector2 a = FcRingPt(angs[i]), c = FcRingPt(angs[(i + 1) % 7]);
            Vector2 dir = (c - a).normalized;
            float len = (c - a).magnitude;
            float s = 1.15f, s1 = len - 1.15f;
            int k = 0;
            while (s < s1 - 0.8f)
            {
                float h1 = FcHash(i * 17 + k, 61), h2 = FcHash(i * 23 + k, 67), h3 = FcHash(i * 7 + k, 71);
                float cl = 1.8f + h1 * 1.7f;
                if (s + cl > s1) cl = s1 - s;
                if (h2 > 0.22f)
                {
                    Vector3 w0 = G(a.x + dir.x * s, a.y + dir.y * s);
                    Vector3 w1 = G(a.x + dir.x * (s + cl), a.y + dir.y * (s + cl));
                    Vector3 wm = (w0 + w1) * 0.5f; wm.y = GroundYW(wm.x, wm.z);
                    float gLo = Mathf.Min(wm.y, Mathf.Min(w0.y, w1.y));
                    float hh = 0.55f + h3 * 0.65f;
                    Vector3 wdir = new Vector3(-dir.x, 0f, -dir.y);          // desain -> dunia (M = -1)
                    float yaw = FcYawOf(wdir);
                    b.Box(Sl.Stone, new Vector3(wm.x, gLo - 0.3f + (hh + 0.3f) * 0.5f, wm.z), new Vector3(0.5f, hh + 0.3f, cl), yaw);
                    // pecahan di atas salah satu ujung supaya siluetnya tidak rata
                    Vector3 shard = wm + wdir * (cl * 0.5f - 0.35f) * (h1 > 0.5f ? 1f : -1f);
                    b.Box(Sl.DarkStone, new Vector3(shard.x, gLo + hh + 0.1f, shard.z), new Vector3(0.42f, 0.3f, 0.5f), yaw + 12f);
                    ForestKit.Blob(b, ForestKit.Moss, new Vector3(wm.x, gLo + hh + 0.02f, wm.z), new Vector3(0.3f, 0.08f, cl * 0.35f), i * 10 + k, 0.3f, 3, 7, 0.4f);
                }
                s += cl + 0.5f + h3 * 1.6f;
                k++;
            }
        }
        // drum tiang yang jatuh di dekat tiang yang paling pendek
        foreach (int i in new[] { 2, 4, 0 })
        {
            Vector2 d = FcRingPt(angs[i] + (i == 4 ? 9f : -8f)) * 0.82f;
            Vector3 p = G(d.x, d.y);
            b.PushTRS(new Vector3(p.x, p.y + 0.45f, p.z), Quaternion.Euler(0f, FcHash(i, 77) * 180f, 90f), 1f);
            b.Cyl(Sl.Stone, Vector3.zero, 0.55f, 1.15f, 10, 0.55f, true, true);
            b.Pop();
        }
        var go = b.Emit(parent, "DindingDanDrum", ForestKit.Slots);
        go.transform.position = Vector3.zero;
        var mf = go.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null) go.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
    }

    /// <summary>Satu tiang: alas bertingkat, batang bulat, rumah ceruk di sisi dalam (menghadap pohon). i = 0 -> ceruk kosong.</summary>
    static void FcPillar(Transform parent, int i, float angDeg)
    {
        Vector2 dp = FcRingPt(angDeg);
        Vector3 p = G(dp.x, dp.y);
        Vector3 inw = G(0f, 0f) - p; inw.y = 0f; inw.Normalize();
        Quaternion rot = Quaternion.Euler(0f, FcYawOf(inw), 0f);

        float lo = 0f;                                                       // tanah terendah di bawah alas, supaya tidak melayang di lereng
        for (int k = 0; k < 4; k++)
        {
            Vector3 w = p + rot * new Vector3((k & 1) == 0 ? -0.95f : 0.95f, 0f, (k & 2) == 0 ? -0.95f : 0.95f);
            lo = Mathf.Min(lo, GroundYW(w.x, w.z) - p.y);
        }

        var go = new GameObject("Tiang_" + i + "_" + FcCeruk[i]);
        go.transform.SetParent(parent, false);
        go.transform.position = p;
        go.transform.rotation = rot;

        float H = FcPillarH[i];
        bool crowned = H > 3.7f;
        float y0 = lo - 0.25f;
        var b = new GeoBag();
        b.Box(Sl.Pave, new Vector3(0f, (y0 + 0.2f) * 0.5f, 0f), new Vector3(1.9f, 0.2f - y0, 1.9f), 4f - i * 1.7f);
        b.Box(Sl.Stone, new Vector3(0f, 0.32f, 0f), new Vector3(1.5f, 0.24f, 1.5f), -3f + i * 2.1f);
        float rb = 0.62f, rt = 0.54f;
        b.Cyl(Sl.Stone, new Vector3(0f, 0.44f, 0f), rb, H - 0.44f, 12, rt, true, false);
        foreach (float yj in new[] { 0.9f, 2.82f, 3.45f })                   // garis sambungan drum (di luar bagian ceruk)
        {
            if (yj > H - 0.25f) continue;
            float rj = Mathf.Lerp(rb, rt, (yj - 0.44f) / (H - 0.44f)) + 0.018f;
            b.Cyl(Sl.DarkStone, new Vector3(0f, yj, 0f), rj, 0.04f, 12, rj, false, false);
        }
        if (crowned)
        {
            b.Cyl(Sl.Stone, new Vector3(0f, H, 0f), rt, 0.16f, 12, 0.86f, true, false);
            b.Box(Sl.Stone, new Vector3(0f, H + 0.28f, 0f), new Vector3(1.75f, 0.24f, 1.75f), 7f + i * 5f);
        }
        else
        {
            for (int s = 0; s < 3; s++)                                      // pecahan bergerigi miring di ujung yang patah
            {
                float h = FcHash(i * 5 + s, 91), h2 = FcHash(i * 5 + s, 93);
                float a = (s * 120f + h * 50f) * Mathf.Deg2Rad;
                Vector3 bs = new Vector3(Mathf.Sin(a) * 0.30f, H - 0.02f, Mathf.Cos(a) * 0.30f);
                Vector3 tp = bs + new Vector3(Mathf.Sin(a) * 0.14f * (h2 - 0.2f), 0.16f + h * 0.40f, Mathf.Cos(a) * 0.14f * (h2 - 0.2f));
                b.Beam(Sl.Stone, bs, tp, 0.30f + h2 * 0.10f, 0.20f, Vector3.forward);
            }
        }

        // rumah ceruk: bingkai batu menonjol di sisi dalam (+Z lokal = menghadap Pohon Kuno)
        const float zc = 0.64f, zd = 0.68f;
        b.Box(Sl.Stone, new Vector3(-0.365f, 1.825f, zc), new Vector3(0.27f, 1.35f, zd));
        b.Box(Sl.Stone, new Vector3(0.365f, 1.825f, zc), new Vector3(0.27f, 1.35f, zd));
        b.Box(Sl.Stone, new Vector3(0f, 1.30f, zc), new Vector3(0.46f, 0.30f, zd));              // ambang bawah
        b.Box(Sl.Stone, new Vector3(0f, 2.365f, zc), new Vector3(0.46f, 0.27f, zd));             // ambang atas
        b.Box(Sl.DarkStone, new Vector3(0f, 1.84f, 0.46f), new Vector3(0.46f, 0.78f, 0.32f));    // dinding belakang ceruk
        b.Box(Sl.Stone, new Vector3(0f, 2.58f, zc + 0.04f), new Vector3(1.18f, 0.16f, 0.78f));   // atap kecil
        b.Box(Sl.Stone, new Vector3(0f, 1.07f, zc + 0.02f), new Vector3(1.02f, 0.14f, 0.74f));   // penopang bawah

        if (i > 0)
        {
            // enam ceruk menyala: panel di dinding belakang + batu cahaya di atas cawan kecil
            b.Quad(Sl.Glow, new Vector3(-0.17f, 1.50f, 0.632f), new Vector3(0.17f, 1.50f, 0.632f), new Vector3(0.17f, 2.16f, 0.632f), new Vector3(-0.17f, 2.16f, 0.632f));
            b.Cyl(Sl.DarkStone, new Vector3(0f, 1.45f, 0.80f), 0.15f, 0.07f, 8, 0.11f, true, false);
            b.Ball(Sl.Glow, new Vector3(0f, 1.78f, 0.80f), 0.15f, 4, 8, 1.35f);
        }
        else
        {
            // ceruk kosong: hanya lingkaran pucat bekas sesuatu yang pernah diletakkan; bersih, tanpa debu atau lumut
            b.Disc(Sl.WoodLight, new Vector3(0f, 0f, 0.80f), 0.13f, 14, 1.4525f);
        }

        // lumut di sisi luar dan sulur di punggung tiang
        ForestKit.Blob(b, ForestKit.Moss, new Vector3(0.2f, 0.62f, -0.5f), new Vector3(0.5f, 0.42f, 0.38f), i * 7 + 1, 0.3f, 3, 8, 0.3f);
        ForestKit.Blob(b, ForestKit.Ivy, new Vector3(-0.25f, 1.9f, -0.52f), new Vector3(0.35f, 0.7f, 0.12f), i * 7 + 2, 0.3f, 3, 7, 0f);

        var slots = (Material[])ForestKit.Slots.Clone();
        if (i > 0) slots[Sl.Glow] = FcGlowMat(i);
        b.Emit(go.transform, "Batu", slots);

        var col = go.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, H * 0.5f, 0.03f);
        col.size = new Vector3(1.9f, H, 2.0f);

        if (i > 0)
        {
            var lg = new GameObject("CahayaCeruk");
            lg.transform.SetParent(go.transform, false);
            lg.transform.localPosition = new Vector3(0f, 1.85f, 1.45f);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point; l.range = 6.5f; l.intensity = 0.9f; l.color = FcCahaya[i];
        }
        else
        {
            var ipo = new GameObject("InteractPoint_CerukKosong");
            ipo.transform.SetParent(go.transform, false);
            Vector3 q = p + inw * 2.6f;
            ipo.transform.position = new Vector3(q.x, GroundYW(q.x, q.z), q.z);
            var ip = ipo.AddComponent<InfoPoint>();
            ip.prompt = "Periksa ceruk yang kosong"; ip.radius = 3.6f; ip.title = "Kuil Tua di Jantung Hutan";
            ip.message = "Tujuh tiang batu berdiri melingkari Pohon Kuno, jauh lebih tua dari jalan setapak dan dari kebun mana pun di sekitar sini. Dinding di antaranya tinggal serpihan setinggi lutut, dan lumut menutup sisi luarnya.\n\n" +
                         "Setiap tiang punya ceruk setinggi dada. Enam ceruk berisi batu yang memancarkan cahaya lembut, masing-masing warnanya lain: emas, biru muda, hijau muda, merah muda, ungu, dan jingga. Satu ceruk dibiarkan kosong. " +
                         "Tak ada debu atau lumut di dasarnya, hanya bekas lingkaran pucat, seakan sesuatu pernah diletakkan di situ lalu diangkat.";
        }
    }

    // ================================================================== 2. ukiran tiga goresan di Pohon Kuno
    static void FcTrunkMark(Transform root)
    {
        var g = new GameObject("Ukiran_TigaGoresan").transform;
        g.SetParent(root, false);
        Vector3 t = G(0f, 0f);                                              // pangkal Pohon Kuno (GiantTree)
        var b = new GeoBag();
        // bekas kupasan kulit kayu setinggi dada di sisi jalan (+Z dunia = selatan desain); batang kira-kira r 2,0 m pada ketinggian ini
        b.Box(ForestKit.Weathered, new Vector3(0.05f, 2.15f, 1.97f), new Vector3(1.0f, 1.10f, 0.30f));
        for (int k = 0; k < 3; k++)
        {
            float x = -0.28f + k * 0.28f;
            b.Beam(ForestKit.Ash, new Vector3(x + 0.03f, 2.55f - k * 0.02f, 2.135f), new Vector3(x - 0.03f, 1.72f + k * 0.03f, 2.135f), 0.075f, 0.05f, Vector3.forward);
        }
        var go = b.Emit(g, "Ukiran", ForestKit.Slots);
        go.transform.position = t;

        var ls = new GameObject("Prasasti_kutukan_hutan");
        ls.transform.SetParent(g, false);
        ls.transform.position = Gw(t.x, t.z + 4.7f);
        var st = ls.AddComponent<LoreStone>();
        st.loreId = "kutukan_hutan"; st.prompt = "Baca ukiran di batang Pohon Kuno"; st.radius = 3.5f;
    }

    // ================================================================== 3. tiga pohon penanda dan tali merah putus
    /// <summary>Titik pada jalur utama (desain) di z tertentu + arah jalur.</summary>
    static Vector2 FcTrailPoint(float z, out Vector2 tangent)
    {
        var tr = trails[0];
        for (int i = 0; i + 1 < tr.Length; i++)
        {
            Vector2 a = tr[i], c = tr[i + 1];
            if ((z >= a.y && z <= c.y) || (z >= c.y && z <= a.y))
            {
                float t = Mathf.Abs(c.y - a.y) < 1e-4f ? 0f : (z - a.y) / (c.y - a.y);
                tangent = (c - a).normalized;
                return Vector2.Lerp(a, c, t);
            }
        }
        tangent = new Vector2(0f, 1f);
        return new Vector2(0f, z);
    }

    static float FcSnagLean(int k) { return (FcHash(k, 5) - 0.5f) * 0.5f; }

    /// <summary>Posisi x lokal garis tengah batang pada ketinggian y (mengikuti lintasan Tube di FcSnag).</summary>
    static float FcSnagX(int k, float y, float H)
    {
        float l = FcSnagLean(k);
        if (y <= 1.2f) return Mathf.Lerp(0f, l * 0.3f, Mathf.Clamp01((y + 0.35f) / 1.55f));
        if (y <= 2.5f) return Mathf.Lerp(l * 0.3f, l * 0.7f, (y - 1.2f) / 1.3f);
        return Mathf.Lerp(l * 0.7f, l, Mathf.Clamp01((y - 2.5f) / Mathf.Max(0.1f, H - 2.5f)));
    }

    static void FcSnag(Transform parent, int k, Vector3 pos, Quaternion rot, float H)
    {
        var go = new GameObject("Penanda_" + (k + 1));
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.rotation = rot;
        float l = FcSnagLean(k);
        var b = new GeoBag();
        var pts = new List<Vector3> { new Vector3(0f, -0.35f, 0f), new Vector3(l * 0.3f, 1.2f, 0f), new Vector3(l * 0.7f, 2.5f, 0.02f), new Vector3(l, H, 0f) };
        var rad = new List<float> { 0.38f, 0.31f, 0.24f, 0.14f };
        ForestKit.Tube(b, ForestKit.BarkDead, pts, rad, 8, 300 + k, 0.2f, true);
        b.Box(ForestKit.BarkDead, new Vector3(l, H + 0.05f, 0f), new Vector3(0.2f, 0.3f, 0.17f), 25f + k * 13f);        // ujung patah
        ForestKit.Limb(b, ForestKit.BarkDead, new Vector3(FcSnagX(k, 2.4f, H), 2.4f, 0f), new Vector3(FcSnagX(k, 2.4f, H) - 0.75f, 3.0f, -0.2f), 0.08f, 0.03f, 310 + k);
        ForestKit.Limb(b, ForestKit.BarkDead, new Vector3(FcSnagX(k, 2.8f, H), 2.8f, 0f), new Vector3(FcSnagX(k, 2.8f, H) + 0.6f, 3.2f, 0.25f), 0.07f, 0.025f, 320 + k);
        // bekas kupasan + tiga goresan, menghadap jalan (+Z lokal)
        float px = FcSnagX(k, 1.65f, H);
        b.Box(ForestKit.Weathered, new Vector3(px, 1.65f, 0.27f), new Vector3(0.34f, 0.60f, 0.14f));
        for (int s = 0; s < 3; s++)
        {
            float x = px - 0.09f + s * 0.09f;
            b.Beam(ForestKit.Ash, new Vector3(x + 0.012f, 1.90f, 0.348f), new Vector3(x - 0.012f, 1.42f, 0.348f), 0.028f, 0.022f, Vector3.forward);
        }
        b.Emit(go.transform, "Batang", ForestKit.Slots);
        var cc = go.AddComponent<CapsuleCollider>();
        cc.radius = 0.32f; cc.height = H; cc.center = new Vector3(l * 0.4f, H * 0.5f, 0f);
    }

    /// <summary>Lilitan tali di batang: sepuluh balok tipis melingkar + simpul.</summary>
    static void FcRopeRing(GeoBag b, Vector3 c, float r)
    {
        const int n = 10;
        for (int j = 0; j < n; j++)
        {
            float a0 = j * Mathf.PI * 2f / n, a1 = (j + 1) * Mathf.PI * 2f / n;
            Vector3 p0 = c + new Vector3(Mathf.Sin(a0) * r, 0.03f * Mathf.Sin(a0), Mathf.Cos(a0) * r);
            Vector3 p1 = c + new Vector3(Mathf.Sin(a1) * r, 0.03f * Mathf.Sin(a1), Mathf.Cos(a1) * r);
            b.Beam((j & 1) == 0 ? Sl.Cloth : Sl.ClothB, p0, p1, 0.07f, 0.07f);
        }
        b.Ball(Sl.ClothB, c + new Vector3(0f, 0.04f, r), 0.085f, 3, 6);
    }

    /// <summary>Bentang tali utuh dengan kendur (sag) parabola; warna dua untai selang-seling supaya terbaca sebagai tali pintal.</summary>
    static void FcRopeSpan(GeoBag b, Vector3 a, Vector3 c, float sag, int n)
    {
        Vector3 prev = a;
        for (int i = 1; i <= n; i++)
        {
            float t = i / (float)n;
            Vector3 p = Vector3.Lerp(a, c, t) + Vector3.down * (sag * 4f * t * (1f - t));
            b.Beam((i & 1) == 0 ? Sl.Cloth : Sl.ClothB, prev, p, 0.065f, 0.065f);
            prev = p;
        }
    }

    /// <summary>
    /// Ujung tali yang putus. start = simpul di batang (titik 0, tingginya dipakai apa adanya); titik ke-1 dst: (jarak sepanjang dir, tinggi di atas tanah, geser samping).
    /// Ujung bertabur serat terurai dan titik embun. Mengembalikan ujungnya.
    /// </summary>
    static Vector3 FcRopeEnd(GeoBag b, Vector3 start, Vector3 dir, Vector3 side, float[] dist, float[] hgt, float[] lat, int seed)
    {
        Vector3 prev = start, prev2 = start;
        for (int i = 0; i < dist.Length; i++)
        {
            Vector3 p = start + dir * dist[i] + side * lat[i];
            p.y = GroundYW(p.x, p.z) + hgt[i];
            b.Beam((i & 1) == 0 ? Sl.Cloth : Sl.ClothB, prev, p, 0.065f, 0.065f);
            prev2 = prev; prev = p;
        }
        Vector3 tip = prev, td2 = (prev - prev2).normalized;
        float segLen = (prev - prev2).magnitude;
        for (int f = 0; f < 5; f++)                                          // serat terurai (bukan irisan rapi, bukan lapuk)
        {
            float a = f * 1.26f + FcHash(seed, f) * 0.8f;
            Vector3 off = td2 * (0.07f + 0.05f * FcHash(seed + 1, f)) + new Vector3(Mathf.Sin(a), 0.3f * Mathf.Cos(a), Mathf.Cos(a)) * 0.045f;
            b.Beam(Sl.Cloth, tip, tip + off, 0.014f, 0.014f);
        }
        for (int e = 0; e < 6; e++)                                          // titik embun pada 35 cm terakhir
        {
            Vector3 p = tip - td2 * Mathf.Min(e * 0.07f, segLen);
            Vector3 o = new Vector3(Mathf.Sin(e * 2.1f + seed), 0.9f, Mathf.Cos(e * 2.1f + seed)) * 0.03f;
            b.Ball(Sl.EyeW, p + o, 0.026f, 3, 6);
        }
        return tip;
    }

    static void FcTrailMarkers(Transform root)
    {
        var g = new GameObject("TaliPenanda_RankG").transform;
        g.SetParent(root, false);
        float[] zs = { -106f, -97.5f, -89f };                                // z desain; di sisi timur jalan utama (tepi jalur tanpa pohon)
        float[] hs = { 3.7f, 4.2f, 3.4f };
        float[] ha = { 1.15f, 1.2f, 1.1f };                                  // tinggi ikatan tali
        var pos = new Vector3[3];
        var cw = new Vector3[3];
        for (int k = 0; k < 3; k++)
        {
            Vector2 tan;
            Vector2 tp = FcTrailPoint(zs[k], out tan);
            Vector2 nrm = new Vector2(tan.y, -tan.x);
            if (nrm.x < 0f) nrm = -nrm;
            Vector2 d = tp + nrm * 3.7f;
            pos[k] = G(d.x, d.y);
            Vector3 toTrail = G(tp.x, tp.y) - pos[k]; toTrail.y = 0f; toTrail.Normalize();
            Quaternion rot = Quaternion.Euler(0f, FcYawOf(toTrail), 0f);
            FcClearAround(pos[k], 1.3f);
            FcSnag(g, k, pos[k], rot, hs[k]);
            cw[k] = pos[k] + rot * new Vector3(FcSnagX(k, ha[k], hs[k]), ha[k], 0f);
        }

        var rope = new GeoBag();
        const float ra = 0.31f;
        for (int k = 0; k < 3; k++) FcRopeRing(rope, cw[k], ra);

        // bentang 1-2 utuh
        Vector3 d01 = cw[1] - cw[0]; d01.y = 0f; d01.Normalize();
        FcRopeSpan(rope, cw[0] + d01 * ra, cw[1] - d01 * ra, 0.42f, 12);

        // bentang 2-3 putus: dua ujung jatuh ke rumput, celah sekitar dua meter di antaranya
        Vector3 d12 = cw[2] - cw[1]; d12.y = 0f; d12.Normalize();
        Vector3 side = new Vector3(d12.z, 0f, -d12.x);
        Vector3 aS = cw[1] + d12 * ra;
        Vector3 bS = cw[2] - d12 * ra;
        // ujung A (dari penanda 2): menjuntai lalu tergeletak, ujungnya sedikit melengkung
        Vector3 tipA = FcRopeEnd(rope, aS, d12, side,
            new[] { 0.25f, 0.5f, 1.2f, 1.9f, 2.4f },
            new[] { 0.55f, 0.10f, 0.05f, 0.04f, 0.09f },
            new[] { 0.0f, 0.02f, 0.10f, 0.32f, 0.46f }, 501);
        // ujung B (dari penanda 3): melorot miring ke tanah
        Vector3 tipB = FcRopeEnd(rope, bS, -d12, side,
            new[] { 0.8f, 1.8f, 2.9f, 3.8f },
            new[] { 0.80f, 0.42f, 0.14f, 0.05f },
            new[] { 0.0f, -0.04f, -0.12f, -0.20f }, 502);

        var slots = (Material[])ForestKit.Slots.Clone();
        slots[Sl.Cloth] = Mat("KanonTaliMerah", new Color(0.58f, 0.14f, 0.10f));
        slots[Sl.ClothB] = Mat("KanonTaliMerahB", new Color(0.70f, 0.19f, 0.13f));
        var dew = Mat("KanonEmbun", new Color(0.82f, 0.90f, 0.95f));
        if (dew.HasProperty("_Smoothness")) dew.SetFloat("_Smoothness", 0.95f);
        slots[Sl.EyeW] = dew;
        var rgo = rope.Emit(g, "TaliMerah", slots);
        rgo.transform.position = Vector3.zero;

        Vector3 mid = (tipA + tipB) * 0.5f;
        var ipo = new GameObject("InteractPoint_TaliPutus");
        ipo.transform.SetParent(g, false);
        ipo.transform.position = new Vector3(mid.x, GroundYW(mid.x, mid.z), mid.z);
        var ip = ipo.AddComponent<InfoPoint>();
        ip.prompt = "Periksa tali yang putus"; ip.radius = 5.0f; ip.title = "Tali Penanda Guild";
        ip.message = "Tali merah Guild ini menandai jarak teraman bagi hunter Rank G dari jalan utama: di balik tali, bantuan Guild sulit menjangkau. Tiga batang pohon mati dijadikan tiang, masing-masing bergores tiga garis sejajar, tanda jalan yang katanya peninggalan Sang Perintis.\n\n" +
                     "Satu bentang putus. Seratnya tidak lapuk dan tidak terpotong rapi, dan kedua ujungnya masih basah oleh embun. Tanah di sekitarnya kosong dari jejak.";
    }
}
