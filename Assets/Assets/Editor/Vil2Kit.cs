using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// v35z102n73 "Kampung Tepi Kali": kit aset khusus Desa Pemula. File ini = alat dasar (acak, segiempat, prisma, balok bulat).
/// Atap: Vil2Kit.Roof.cs; jendela/pintu/cerobong/fondasi: Vil2Kit.Parts.cs; rumah lengkap: Vil2Kit.House.cs. Hanya memakai GeoBag/Sl/Vector/Mathf supaya bisa dipratinjau di luar Unity.
/// Konvensi: muka bangunan = +Z lokal, lebar = X lokal, alas di y = 0.
/// </summary>
public static partial class V2Kit
{
    // ------------------------------------------------------------------ util
    public static float Rnd(int seed, int k)
    {
        unchecked
        {
            int h = seed * 73856093 ^ k * 19349663 ^ 0x5bd1e995;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0xffff) / 65535f;
        }
    }

    /// <summary>Segiempat dengan normal menghadap ke atas (permukaan atap, tanah).</summary>
    public static void QUp(GeoBag g, int s, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        Vector3 n = Vector3.Cross(b - a, c - a);
        if (n.y >= 0f) g.Quad(s, a, b, c, d); else g.Quad(s, d, c, b, a);
    }

    /// <summary>Segiempat dengan normal menghadap searah 'outward'.</summary>
    public static void QOut(GeoBag g, int s, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
    {
        Vector3 n = Vector3.Cross(b - a, c - a);
        if (Vector3.Dot(n, outward) >= 0f) g.Quad(s, a, b, c, d); else g.Quad(s, d, c, b, a);
    }

    /// <summary>Prisma / batang bundar dari a ke c (r0 di a, r1 di c), n sisi, tutup ujung opsional.</summary>
    public static void Prism(GeoBag b, int s, Vector3 a, Vector3 c, float r0, float r1, int n, bool capA = false, bool capC = false)
    {
        Vector3 ax = c - a;
        float L = ax.magnitude;
        if (L < 1e-4f) return;
        ax = ax / L;
        Vector3 hint = Mathf.Abs(ax.y) > 0.95f ? new Vector3(1f, 0f, 0f) : new Vector3(0f, 1f, 0f);
        Vector3 u = Vector3.Cross(hint, ax).normalized;
        Vector3 v = Vector3.Cross(ax, u).normalized;
        for (int i = 0; i < n; i++)
        {
            float t0 = i * 2f * Mathf.PI / n, t1 = (i + 1) * 2f * Mathf.PI / n, tm = (t0 + t1) * 0.5f;
            Vector3 d0 = u * Mathf.Cos(t0) + v * Mathf.Sin(t0), d1 = u * Mathf.Cos(t1) + v * Mathf.Sin(t1), dm = u * Mathf.Cos(tm) + v * Mathf.Sin(tm);
            Vector3 A0 = a + d0 * r0, A1 = a + d1 * r0, C1 = c + d1 * r1, C0 = c + d0 * r1;
            QOut(b, s, A0, A1, C1, C0, dm);
            if (capA) b.Tri(s, a, A1, A0);
            if (capC) b.Tri(s, c, C0, C1);
        }
    }

    public static void LogH(GeoBag b, int s, Vector3 a, Vector3 c, float r) { Prism(b, s, a, c, r, r, 7, true, true); }

    /// <summary>Tiang kayu tegak (silinder) dengan sedikit mengecil.</summary>
    public static void Post(GeoBag b, int s, Vector3 baseP, float h, float r) { b.Cyl(s, baseP, r, h, 6, r * 0.88f, true, false); }
}
