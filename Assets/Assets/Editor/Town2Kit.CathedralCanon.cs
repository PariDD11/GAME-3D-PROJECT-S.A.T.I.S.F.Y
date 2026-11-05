using UnityEngine;

/// <summary>
/// n89 (Docs/LORE_BIBLE.md 8.9): retak setipis rambut yang merayap di muka Katedral Ashwatch (batu putih yang "bernapas", Kronik Bab 33).
/// Dipanggil dari T2Kit.Cathedral() sesudah tangga depan. Murni geometri (balok tipis gelap yang menempel 3 cm di muka tembok);
/// tanpa UnityEngine.Random (hash deterministik), jadi bentuknya sama tiap kali dibangun.
/// Retak dihindarkan dari portal, dua jendela tinggi, jendela mawar, galeri relung, dan tangga depan.
/// </summary>
public static partial class T2Kit
{
    static float CcHash(int i)
    {
        float s = Mathf.Sin(i * 12.9898f + 78.233f) * 43758.5453f;
        return s - Mathf.Floor(s);
    }

    /// <summary>Bagian muka yang tidak boleh dilintasi retak (koordinat lokal Katedral: x ke kanan, y dari tanah).</summary>
    static bool CcBlocked(float x, float y, float P)
    {
        float ax = Mathf.Abs(x);
        if (ax < 3.7f && y < P + 8.6f) return true;                                          // portal runcing dan bingkainya
        if (ax > 4.6f && ax < 7.2f && y > P + 3.2f && y < P + 11.8f) return true;           // dua jendela tinggi
        if (x * x + (y - (P + 19.5f)) * (y - (P + 19.5f)) < 3.9f * 3.9f) return true;       // jendela mawar
        if (ax < 5.9f && y > P + 13.4f && y < P + 17.4f) return true;                        // galeri relung patung
        if (ax < 7.4f && y < P + 2.6f) return true;                                          // tangga depan menutupi kaki tembok
        return false;
    }

    static int CcSeg(GeoBag b, Vector3 p, Vector3 q, float w, float P)
    {
        Vector3 m = (p + q) * 0.5f;
        if (CcBlocked(m.x, m.y, P) || CcBlocked(q.x, q.y, P)) return 0;
        b.Beam(Sl.Metal, p, q, w, 0.034f, Vector3.forward);
        return 1;
    }

    /// <summary>Satu retak naik dari (x0, y0) setinggi h dengan zigzag deterministik; lebar menyempit ke ujung. Mengembalikan jumlah ruas.</summary>
    static int CcCrack(GeoBag b, float P, float zF, float x0, float y0, float h, int seed, float w0)
    {
        float z = zF + 0.017f;
        Vector3 p = new Vector3(x0, y0, z);
        int n = 0;
        for (int k = 0; k < 24 && p.y < y0 + h; k++)
        {
            float step = 0.9f + 1.3f * CcHash(seed * 31 + k * 7 + 1);
            float dx = (CcHash(seed * 17 + k * 13 + 5) - 0.5f) * 1.1f;
            Vector3 q = new Vector3(Mathf.Clamp(p.x + dx, -13.5f, 13.5f), p.y + step, z);
            float t = Mathf.Clamp01((p.y - y0) / h);
            float w = Mathf.Lerp(w0, 0.045f, t);
            n += CcSeg(b, p, q, w, P);
            // cabang pendek (dua ruas) ke samping-atas
            if (k > 1 && CcHash(seed * 5 + k * 3 + 2) > 0.7f)
            {
                float side = CcHash(seed * 3 + k) > 0.5f ? 1f : -1f;
                Vector3 c1 = new Vector3(Mathf.Clamp(q.x + side * (0.5f + 0.5f * CcHash(seed + k * 11)), -13.5f, 13.5f), q.y + 0.7f, z);
                Vector3 c2 = new Vector3(Mathf.Clamp(c1.x + side * 0.45f, -13.5f, 13.5f), c1.y + 0.9f, z);
                n += CcSeg(b, q, c1, w * 0.7f, P);
                n += CcSeg(b, c1, c2, w * 0.45f, P);
            }
            p = q;
        }
        return n;
    }

    /// <summary>Retak-retak di muka depan Katedral (z = zF). Mengembalikan jumlah ruas yang dipasang (untuk laporan uji).</summary>
    public static int CathedralCracks(GeoBag b, float P, float zF)
    {
        int n = 0;
        n += CcCrack(b, P, zF, -12.8f, P + 0.15f, 10.5f, 11, 0.17f);
        n += CcCrack(b, P, zF, -10.7f, P + 0.15f, 6.4f, 12, 0.14f);
        n += CcCrack(b, P, zF, -8.4f, P + 0.15f, 13.4f, 13, 0.16f);
        n += CcCrack(b, P, zF, 8.1f, P + 0.15f, 12.2f, 14, 0.16f);
        n += CcCrack(b, P, zF, 10.9f, P + 0.15f, 7.6f, 15, 0.14f);
        n += CcCrack(b, P, zF, 13.0f, P + 0.15f, 11.0f, 16, 0.17f);
        n += CcCrack(b, P, zF, -3.95f, P + 2.9f, 4.6f, 17, 0.12f);
        n += CcCrack(b, P, zF, 4.15f, P + 2.9f, 5.2f, 18, 0.12f);
        return n;
    }
}
