using UnityEngine;
using HunterGuild;

/// <summary>
/// Kanon Lembah Iblis, bagian 4: Altar Ritual (pusat desain (-170, 80)). Telinga, peziarah (runtime), api, dan lingkar tiang lama dibiarkan. Ditambah dua lingkaran
/// serbuk putih mengikuti tanah: luar r 21,6 m (terbuka di sisi selatan desain, arah datang pemburu) dan dalam r 4,6 m (terbuka di sisi utara), sebelas
/// lingkaran kecil berongga di lingkaran luar, serta dua tiang gerbang obsidian di celah dengan lingkaran kosong putih di mukanya dan satu lampu pucat.
/// </summary>
public static partial class HuntSceneBuilder
{
    /// <summary>Busur serbuk mengikuti tanah di sekeliling (cx, cz) desain, sudut a0 sampai a1 derajat (0 = utara desain +z).</summary>
    static void DmArc(GeoBag b, int slot, float cx, float cz, float r, float w, float a0, float a1)
    {
        int n = Mathf.Max(6, Mathf.CeilToInt((a1 - a0) * Mathf.Deg2Rad * r / 2.5f));
        for (int i = 0; i < n; i++)
        {
            float t0 = (a0 + (a1 - a0) * i / n) * Mathf.Deg2Rad, t1 = (a0 + (a1 - a0) * (i + 1) / n) * Mathf.Deg2Rad;
            DmStrip(b, slot, -(cx + Mathf.Sin(t0) * r), -(cz + Mathf.Cos(t0) * r), -(cx + Mathf.Sin(t1) * r), -(cz + Mathf.Cos(t1) * r), w, w, 0.035f);
        }
    }

    static void DmAltar(Transform root, Material[] slots)
    {
        var g = new GameObject("LingkaranAltar").transform;
        g.SetParent(root, false);
        var b = new GeoBag();
        const float ax = -170f, az = 80f, R = 21.6f;
        DmArc(b, Sl.Hedge, ax, az, R, 0.7f, 200f, 520f);                                                    // luar: celah 160 sampai 200 derajat (selatan desain)
        DmArc(b, Sl.Hedge, ax, az, 4.6f, 0.45f, 20f, 340f);                                                 // dalam: celah di utara desain
        for (int i = 0; i < 12; i++)                                                                        // lingkaran kecil berongga tiap 30 derajat, kecuali di celah
        {
            float ang = i * 30f;
            if (ang > 150f && ang < 210f) continue;
            float t = ang * Mathf.Deg2Rad;
            float wx = -(ax + Mathf.Sin(t) * R), wz = -(az + Mathf.Cos(t) * R);
            b.Annulus(Sl.Hedge, new Vector3(wx, 0f, wz), 0.22f, 0.36f, 0f, 360f, 14, GroundYW(wx, wz) + 0.05f);
            dmRings++;
        }
        foreach (float ang in new[] { 168f, 192f })                                                         // dua tiang gerbang di celah luar
        {
            float t = ang * Mathf.Deg2Rad;
            Vector3 p = G(ax + Mathf.Sin(t) * R, az + Mathf.Cos(t) * R);
            b.Cyl(Sl.Dark, new Vector3(p.x, p.y - 0.2f, p.z), 0.42f, 2.5f, 6, 0.34f, true);
            b.Box(Sl.Glow, new Vector3(p.x, p.y + 2.35f, p.z), new Vector3(0.3f, 0.1f, 0.3f));
            b.PushTRS(new Vector3(p.x, p.y + 1.5f, p.z + 0.4f), Quaternion.Euler(90f, 0f, 0f), 1f);         // lingkaran kosong tegak, menghadap pemburu yang datang
            b.Cyl(Sl.Hedge, Vector3.zero, 0.46f, 0.05f, 16, 0.46f, true, true);
            b.Cyl(Sl.Dark, Vector3.zero, 0.32f, 0.07f, 16, 0.32f, true, true);
            b.Pop();
            BnCol(g, "TiangGerbangCol_" + (int)ang, p + new Vector3(0f, 1.2f, 0f), new Vector3(0.9f, 2.4f, 0.9f), 0f);
        }
        b.Emit(g, "LingkaranSerbuk", slots);

        Vector3 gc = G(ax, az - R);
        ELight(g, gc + Vector3.up * 3f, new Color(0.9f, 0.86f, 0.78f), 14f, 0.9f, false);
        BnInfo(g, "InteractPoint_LingkaranAltar", gc + new Vector3(0f, 0.1f, 0f), "Periksa lingkaran", 7f, "Lingkaran Serbuk",
            "Serbuk putih ditabur membentuk dua lingkaran di tanah, satu besar mengelilingi altar dan satu kecil di pusatnya, dan keduanya sengaja dibiarkan terbuka di satu sisi. " +
            "Serbuk itu bukan garam; tak ada yang mau menjelaskan bedanya. Di kedua tiang gerbang ada lingkaran kosong berwarna putih, sebesar telapak tangan orang dewasa.\n\n" +
            "Setiap pagi serbuknya tampak baru ditaburkan, tetapi para pemburu yang berjaga semalaman bersumpah tak melihat siapa pun yang melakukannya.");
    }
}
