using System.Collections.Generic;
using UnityEngine;
using HunterGuild;

/// <summary>
/// n89 (Docs/LORE_BIBLE.md 8.9): Lantai Seratus Menara Eldrath. Satu arena bundar dipakai ulang untuk seratus lantai (TowerManager); pada lantai terakhir
/// dinding, langit-langit, dan hiasan lantai bundar disembunyikan (TowerManager.ApplyHall) dan aula ini dinyalakan:
///  - lantai pualam r 420 m (dengan collider) di bawah langit bintang (640 bintang pada kubah 495 m, tanpa bayangan);
///  - Gerbang Terakhir: lengkung putih berdiri sendiri di (0, -31.5), tepat di belakang titik awal pemain;
///  - Tangga Fajar: 72 anak tangga cahaya yang melingkar naik dari balik lengkung (hanya tampilan, tanpa collider);
///  - satu pecahan bilah putih di tengah lantai (tanpa tokoh; Eldrath tidak digambar);
///  - sisa dinding lama: tunggul tembok dan tiang patah di jejak dinding r 31,5 (ber-collider), terbuka di selatan dan utara;
///  - satu retakan hitam setipis rambut di ujung utara (z 35 sampai 42), di luar arena;
///  - tiga InfoPoint berregister mendeskripsikan (tanpa nama tokoh, tanpa isi Pintu atau Orvane).
/// Tanpa UnityEngine.Random: semua letak berasal dari TcHash.
/// </summary>
public static partial class HuntSceneBuilder
{
    public static string TcReport = "";

    const float TcFloorR = 420f;       // jari-jari lantai pualam
    const float TcStarD = 495f;        // jarak bintang dari pusat aula (kamera utama far = 1000)
    const float TcGateZ = -31.5f;      // Gerbang Terakhir; titik awal pemain di z -25, menghadap utara
    static int tcStars, tcSteps, tcCrackSegs, tcRuinBlocks, tcInfos, tcLights;

    static float TcHash(int i)
    {
        float s = Mathf.Sin(i * 12.9898f + 78.233f) * 43758.5453f;
        return s - Mathf.Floor(s);
    }

    /// <summary>Slot khusus aula (salinan slot hutan; benda lama tidak ikut berubah).</summary>
    static Material[] TcSlots()
    {
        ForestKit.Load();
        var s = (Material[])ForestKit.Slots.Clone();
        s[Sl.Marble] = Mat("TcPualam", new Color(0.93f, 0.94f, 0.97f));
        s[Sl.Stone] = Mat("TcPualamRuas", new Color(0.7f, 0.71f, 0.77f));
        s[Sl.DarkStone] = Mat("TcBatuSisa", new Color(0.42f, 0.42f, 0.48f));
        s[Sl.Glow] = TownMats.Emissive(Mat("TcTangga", new Color(1f, 0.9f, 0.66f)), new Color(1.5f, 1.08f, 0.5f));
        s[Sl.Glass] = TownMats.Emissive(Mat("TcBintang", new Color(0.86f, 0.9f, 1f)), new Color(2.3f, 2.5f, 3.1f));
        s[Sl.Gold] = TownMats.Emissive(Mat("TcBintangHangat", new Color(1f, 0.92f, 0.74f)), new Color(2.5f, 2.1f, 1.35f));
        s[Sl.Metal] = Mat("TcRetak", new Color(0.012f, 0.012f, 0.018f));
        return s;
    }

    static InfoPoint TcInfo(Transform parent, string name, Vector3 local, string prompt, float radius, string title, string message)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = local;
        var ip = go.AddComponent<InfoPoint>();
        ip.prompt = prompt; ip.radius = radius; ip.title = title; ip.message = message;
        tcInfos++;
        return ip;
    }

    static void TcLight(Transform parent, string name, Vector3 local, Color col, float range, float intensity)
    {
        var lg = new GameObject(name).AddComponent<Light>();
        lg.transform.SetParent(parent, false);
        lg.transform.localPosition = local;
        lg.type = LightType.Point; lg.range = range; lg.intensity = intensity; lg.color = col; lg.shadows = LightShadows.None;
        tcLights++;
    }

    /// <summary>Dipanggil dari BuildTower sesudah TowerManager dibuat. Aula dibangun nonaktif; TowerManager menyalakannya hanya di lantai terakhir.</summary>
    static void TowerHall100(Transform root, TowerManager tm, GameObject walls, GameObject ceiling)
    {
        tcStars = tcSteps = tcCrackSegs = tcRuinBlocks = tcInfos = tcLights = 0;
        var slots = TcSlots();
        var hall = new GameObject("Aula_LantaiSeratus").transform;
        hall.SetParent(root, false);

        TcFloor(hall, slots);
        TcGate(hall, slots);
        TcStairs(hall, slots);
        TcShard(hall, slots);
        TcRuins(hall, slots);
        TcCrack(hall, slots);
        TcStars(hall, slots);
        TcLights(hall);
        TcTexts(hall);

        // yang disembunyikan di lantai terakhir: dinding bundar (collider ikut mati), langit-langit, tampilan dinding/kubah, cincin lantai bundar
        var hide = new List<GameObject>();
        if (walls != null) hide.Add(walls);
        if (ceiling != null) hide.Add(ceiling);
        foreach (var nm in new[] { "TowerHall", "TowerVault", "FloorRing0", "FloorRing1", "FloorRing2" })
        {
            var t = root.Find(nm);
            if (t != null) hide.Add(t.gameObject);
        }
        tm.hall100 = hall.gameObject;
        tm.hideInHall = hide.ToArray();
        hall.gameObject.SetActive(false);

        TcReport = "ALA 100: bintang=" + tcStars + " | anakTangga=" + tcSteps + " | retakSegmen=" + tcCrackSegs + " | sisaDinding=" + tcRuinBlocks +
                   " | InfoPoint=" + tcInfos + " | lampu=" + tcLights + " | disembunyikan=" + hide.Count;
        Debug.Log("[Menara n89] " + TcReport);
    }

    static void TcFloor(Transform hall, Material[] slots)
    {
        var b = new GeoBag();
        b.Cyl(Sl.Marble, new Vector3(0f, -1.5f, 0f), TcFloorR, 1.48f, 72, TcFloorR, true);        // puncak y -0,02: tersembunyi di bawah lantai lama (r 32, puncak y 0)
        var go = b.Emit(hall, "LantaiPualam", slots);
        var mf = go.GetComponent<MeshFilter>();
        if (mf != null) go.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;

        // tiga cincin ruas batu di lantai (isyarat skala); sampai r 145 saja agar tak berkedip di kejauhan
        var r = new GeoBag();
        float[] rr = { 47f, 95f, 143f };
        for (int k = 0; k < rr.Length; k++) r.Annulus(Sl.Stone, Vector3.zero, rr[k], rr[k] + 0.7f + 0.35f * k, 0f, 360f, 96, -0.004f);
        r.Emit(hall, "RuasLantai", slots);
    }

    static void TcGate(Transform hall, Material[] slots)
    {
        var b = new GeoBag();
        b.Push(new Vector3(0f, 0f, TcGateZ), 0f);
        V2Kit.ArchRing(b, Sl.Marble, 0f, 8.4f, 2.45f, 3.95f, 0f, 180f, 16, -0.85f, 0.85f);      // r dalam 2,45; r luar 3,95; tinggi total 12,35 m
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * 3.2f;
            b.Box(Sl.Marble, new Vector3(x, 4.15f, 0f), new Vector3(1.5f, 8.5f, 1.7f));          // tiang (dasar y -0,1 supaya tak ada celah di luar lantai lama)
            b.Box(Sl.Marble, new Vector3(x, 0.25f, 0f), new Vector3(2.0f, 0.7f, 2.2f));          // alas tiang
        }
        b.Box(Sl.Marble, new Vector3(0f, 11.6f, 0f), new Vector3(1.0f, 1.9f, 1.9f));             // batu kunci
        b.Box(Sl.Stone, new Vector3(0f, 0.08f, 0f), new Vector3(4.9f, 0.16f, 1.8f));             // ambang
        b.Pop();
        var go = b.Emit(hall, "GerbangTerakhir", slots);
        for (int side = -1; side <= 1; side += 2)                                                // hanya kedua tiang yang padat; bukaan lengkung bebas dilalui
        {
            var bc = go.AddComponent<BoxCollider>();
            bc.center = new Vector3(side * 3.2f, 4.2f, TcGateZ);
            bc.size = new Vector3(2.0f, 8.4f, 2.2f);
        }
    }

    static void TcStairs(Transform hall, Material[] slots)
    {
        var b = new GeoBag();
        Vector3 c = new Vector3(0f, 0f, TcGateZ - 11f);                // pusat ulir 11 m di belakang lengkung; anak tangga pertama persis di balik ambang
        const float R = 9f;
        const int n = 72;
        for (int k = 0; k < n; k++)
        {
            float th = k * 12f, rad = th * Mathf.Deg2Rad;
            Vector3 p = new Vector3(c.x + Mathf.Sin(rad) * R, 0.3f + k * 0.8f, c.z + Mathf.Cos(rad) * R);
            b.Box(Sl.Glow, p, new Vector3(2.0f, 0.28f, 3.4f), th);   // lebar 3,4 m ke arah jari-jari
        }
        b.Cyl(Sl.Glow, new Vector3(c.x, 0f, c.z), 0.16f, 66f, 6, 0.08f, true);   // jarum cahaya di poros ulir
        b.Emit(hall, "TanggaFajar", slots);
        tcSteps = n;
    }

    static void TcShard(Transform hall, Material[] slots)
    {
        var b = new GeoBag();
        b.Beam(Sl.Marble, new Vector3(0f, -0.1f, 0f), new Vector3(0.12f, 1.5f, 0.02f), 0.46f, 0.08f);
        b.Beam(Sl.Marble, new Vector3(0.12f, 1.45f, 0.02f), new Vector3(0.22f, 2.75f, 0.04f), 0.38f, 0.075f);
        b.Beam(Sl.Marble, new Vector3(0.22f, 2.7f, 0.04f), new Vector3(0.3f, 3.6f, 0.05f), 0.28f, 0.07f);         // ujung patah rata
        b.Beam(Sl.Glow, new Vector3(0.03f, 0.25f, -0.045f), new Vector3(0.27f, 3.4f, 0.03f), 0.05f, 0.02f);       // garis tipis berpendar di muka selatan
        b.Beam(Sl.Marble, new Vector3(1.35f, 0.14f, 0.7f), new Vector3(2.0f, 0.14f, 1.55f), 0.22f, 0.06f);        // serpih lepas di lantai
        b.Annulus(Sl.Glow, Vector3.zero, 0.95f, 1.07f, 0f, 360f, 40, 0.13f);                                      // cincin pendar tipis di kaki bilah
        b.Emit(hall, "PecahanBilah", slots);
    }

    static void TcRuins(Transform hall, Material[] slots)
    {
        var b = new GeoBag();
        int n = 28;
        float R = TowerR;
        float chord = 2f * Mathf.PI * (R + 1.5f) / n;
        int blocks = 0;
        for (int i = 0; i < n; i++)
        {
            if (TcGap(i)) continue;
            float a = i / (float)n * Mathf.PI * 2f;
            Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            float yaw = Mathf.Atan2(-dir.x, -dir.z) * Mathf.Rad2Deg;
            float h = 0.3f + 3.4f * Mathf.Pow(TcHash(i * 3 + 1), 3f);                          // kebanyakan rendah, sedikit yang menjulang
            b.Box(Sl.DarkStone, dir * (R + 1.5f) + new Vector3(0f, h * 0.5f - 0.05f, 0f), new Vector3(chord + 0.4f, h + 0.1f, 3f), yaw);
            blocks++;
        }
        for (int i = 0; i < n; i += 2)
        {
            if (TcGap(i)) continue;
            float a2 = (i + 0.5f) / n * Mathf.PI * 2f;
            Vector3 d2 = new Vector3(Mathf.Cos(a2), 0f, Mathf.Sin(a2));
            float h = 1.3f + 7.2f * Mathf.Pow(TcHash(i * 5 + 2), 2f);
            b.Cyl(Sl.Marble, d2 * (R - 0.4f) + new Vector3(0f, -0.05f, 0f), 1.1f, h, 10, 0.95f, true);
            blocks++;
        }
        var go = b.Emit(hall, "SisaDinding", slots);
        var mf = go.GetComponent<MeshFilter>();
        if (mf != null) go.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
        tcRuinBlocks = blocks;
    }

    /// <summary>Tiga segmen dinding di selatan (lengkung dan tangga cahaya) dan tiga di utara (retakan) dibiarkan terbuka.</summary>
    static bool TcGap(int i) { return (i >= 20 && i <= 22) || (i >= 6 && i <= 8); }

    static void TcCrack(Transform hall, Material[] slots)
    {
        var b = new GeoBag();
        const int N = 22;
        Vector3 p = new Vector3(-15f, -0.005f, 38.6f);
        int segs = 0;
        Vector3 branchFrom1 = p, branchFrom2 = p;
        for (int k = 0; k < N; k++)
        {
            float dx = 1.0f + 0.9f * TcHash(k * 7 + 3);
            float dz = (TcHash(k * 11 + 5) - 0.5f) * 1.5f;
            Vector3 q = new Vector3(p.x + dx, p.y, Mathf.Clamp(p.z + dz, 35.6f, 41.6f));
            float t = (k + 0.5f) / N;
            float w = 0.03f + 0.11f * Mathf.Sin(Mathf.PI * t);                                   // setipis rambut di ujung, sedikit melebar di tengah
            b.Beam(Sl.Metal, p, q, w, 0.02f, Vector3.up);
            segs++;
            if (k == 6) branchFrom1 = q;
            if (k == 14) branchFrom2 = q;
            p = q;
        }
        for (int br = 0; br < 2; br++)                                                           // dua cabang pendek, makin tipis
        {
            Vector3 s = br == 0 ? branchFrom1 : branchFrom2;
            float dir = br == 0 ? 1f : -1f;
            for (int k = 0; k < 4; k++)
            {
                Vector3 q = new Vector3(s.x + (0.5f + 0.5f * TcHash(br * 29 + k * 3 + 90)) * (br == 0 ? 1f : -0.6f), s.y, s.z + dir * (0.7f + 0.6f * TcHash(br * 31 + k * 5 + 91)));
                b.Beam(Sl.Metal, s, q, Mathf.Lerp(0.045f, 0.022f, k / 3f), 0.02f, Vector3.up);
                segs++;
                s = q;
            }
        }
        b.Emit(hall, "RetakUjungAula", slots);
        tcCrackSegs = segs;
    }

    static void TcStars(Transform hall, Material[] slots)
    {
        var b = new GeoBag();
        const int n = 640;
        for (int i = 0; i < n; i++)
        {
            float az = TcHash(i * 5 + 11) * Mathf.PI * 2f;
            float el = Mathf.Lerp(2f, 86f, TcHash(i * 5 + 12)) * Mathf.Deg2Rad;
            float size = 0.5f + 0.7f * Mathf.Pow(TcHash(i * 5 + 13), 2.2f);
            float kind = TcHash(i * 5 + 14);
            if (kind > 0.93f) size *= 1.8f;                                                      // sedikit bintang terang
            int slot = kind < 0.68f ? Sl.Glass : Sl.Gold;
            float ce = Mathf.Cos(el);
            b.Ball(slot, new Vector3(Mathf.Sin(az) * ce, Mathf.Sin(el), Mathf.Cos(az) * ce) * TcStarD, size, 2, 4);
        }
        var go = b.Emit(hall, "Bintang", slots);
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null) { mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false; }
        tcStars = n;
    }

    static void TcLights(Transform hall)
    {
        TcLight(hall, "LampuGerbang", new Vector3(0f, 5f, TcGateZ - 4.5f), new Color(1f, 0.86f, 0.58f), 42f, 2.0f);
        TcLight(hall, "LampuPecahan", new Vector3(0.8f, 3.4f, -1.0f), new Color(0.82f, 0.9f, 1f), 18f, 1.6f);
        TcLight(hall, "LampuRetak", new Vector3(0f, 1.6f, 36.5f), new Color(0.7f, 0.8f, 1f), 14f, 0.7f);
    }

    static void TcTexts(Transform hall)
    {
        TcInfo(hall, "InteractPoint_GerbangTerakhir", new Vector3(0f, 0.1f, TcGateZ + 3.6f), "Periksa Lengkung Putih", 4.5f, "Gerbang Terakhir",
            "Sebuah lengkung putih berdiri sendirian di ujung selatan aula, tanpa tembok di kiri maupun kanannya. Dari aula, orang melihat tangga cahaya melingkar naik di balik lengkung itu, makin tinggi, sampai hilang di antara bintang.\n\n" +
            "Para pendaki yang pernah sampai di sini menyebutnya Gerbang Terakhir. Tak seorang pun mau menaruh telapak tangan di batunya, sebab batu itu tidak dingin; ia hangat, seperti sesuatu yang baru saja ditinggalkan.");
        TcInfo(hall, "InteractPoint_PecahanBilah", new Vector3(0f, 0.1f, 0f), "Periksa Pecahan Bilah", 4.2f, "Pecahan di Tengah Aula",
            "Sepotong bilah putih tertancap miring di tengah lantai, setinggi dua orang berdiri. Ujungnya patah rata, gagangnya tak ada, dan pualam di sekelilingnya tak punya satu pun retak. Satu garis tipis di sepanjang bilah itu berpendar sepucat bintang-bintang di atas.\n\n" +
            "Pendaki yang pernah sampai di sini jarang mau menceritakannya. Yang mau hanya berkata bahwa bilah itu tak pernah terasa seperti benda mati.");
        TcInfo(hall, "InteractPoint_RetakAula", new Vector3(0f, 0.1f, 38.6f), "Periksa Retakan", 4.5f, "Retakan di Ujung Aula",
            "Di ujung utara aula, sebuah garis hitam setipis rambut membelah pualam dari kiri ke kanan lalu berhenti begitu saja. Tak ada bekas pukulan, tak ada serpihan, dan debu pun tak singgah di dalamnya. Pendaki yang mencoba menyelipkan ujung pisau ke celah itu mengaku pisaunya tak masuk, padahal celahnya jelas terlihat.\n\n" +
            "Batu di kedua sisinya hangat, seperti lantai tempat seseorang baru saja bangun.");
    }
}
