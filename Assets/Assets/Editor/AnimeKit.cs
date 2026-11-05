using UnityEngine;

/// <summary>
/// Hiasan bangunan bergaya anime fantasi: bunting warna-warni, tanaman rambat, semak bunga, jendela bulat menyala,
/// lentera gantung, tonggak bendera kecil, dan lis atap bergelombang. Semua menghadap +Z lokal GeoBag (pakai b.Push untuk memutar).
/// </summary>
public static class AnimeKit
{
    static readonly int[] Fest = { Sl.Cloth, Sl.ClothB, Sl.Gold, Sl.Blossom, Sl.Leaf };

    static void Tri2(GeoBag b, int s, Vector3 a, Vector3 c, Vector3 d)
    {
        b.Tri(s, a, c, d);
        b.Tri(s, d, c, a);
    }

    /// <summary>Tali bunting melengkung dari a ke b dengan bendera segitiga warna-warni.</summary>
    public static void Bunting(GeoBag b, Vector3 a, Vector3 c, float sag, int flags, int seed)
    {
        Vector3 prev = a;
        for (int i = 1; i <= flags * 2; i++)
        {
            float t = i / (float)(flags * 2);
            Vector3 p = Vector3.Lerp(a, c, t);
            p.y -= Mathf.Sin(t * Mathf.PI) * sag;
            b.Beam(Sl.Wood, prev, p, 0.035f, 0.035f);
            if (i % 2 == 0)
            {
                Vector3 m = (prev + p) * 0.5f;
                int slot = Fest[(seed + i / 2) % Fest.Length];
                Vector3 d = (p - prev).normalized * 0.22f;
                Tri2(b, slot, m - d, m + d, m + new Vector3(0f, -0.5f, 0.02f));
            }
            prev = p;
        }
    }

    /// <summary>Tanaman rambat: gumpalan daun kecil dan bunga di dinding. base = titik dasar di permukaan dinding.</summary>
    public static void Ivy(GeoBag b, Vector3 baseP, float height, float width, int seed)
    {
        int n = Mathf.Max(5, Mathf.RoundToInt(height * 3f));
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)(n - 1);
            float hx = Mathf.Sin(seed * 1.7f + i * 2.3f) * width * 0.5f * (0.4f + 0.6f * (1f - t));
            float y = t * height;
            Vector3 p = baseP + new Vector3(hx, y, 0.1f + 0.05f * Mathf.Sin(i * 1.9f));
            float r = 0.2f + 0.12f * Mathf.Abs(Mathf.Sin(seed + i * 1.3f));
            b.Ball(Sl.Hedge, p, r, 3, 6, 0.8f);
            if (i % 3 == 1) b.Ball(Sl.Blossom, p + new Vector3(0.08f, 0.05f, 0.14f), 0.085f, 3, 5);
        }
    }

    /// <summary>Semak bunga bundar (hijau + titik bunga) untuk di depan dinding.</summary>
    public static void FlowerBush(GeoBag b, Vector3 c, float r, int flowerSlot)
    {
        b.Ball(Sl.Hedge, c + new Vector3(0f, r * 0.55f, 0f), r, 4, 8, 0.75f);
        b.Ball(Sl.Leaf, c + new Vector3(r * 0.45f, r * 0.5f, r * 0.2f), r * 0.65f, 3, 6, 0.8f);
        for (int i = 0; i < 7; i++)
        {
            float a = i * 2.4f;
            Vector3 p = c + new Vector3(Mathf.Cos(a) * r * 0.75f, r * (0.55f + 0.35f * Mathf.Sin(a * 1.3f + 1f)), Mathf.Sin(a) * r * 0.75f);
            b.Ball(flowerSlot, p, r * 0.16f, 3, 5);
        }
    }

    /// <summary>Jendela bulat dengan cahaya hangat (loteng/gable). c = pusat.</summary>
    public static void RoundWindow(GeoBag b, Vector3 c, float r)
    {
        b.Box(Sl.Wood, c + new Vector3(0f, 0f, 0.02f), new Vector3(r * 2f + 0.3f, r * 2f + 0.3f, 0.1f));
        b.Ball(Sl.Glow, c + new Vector3(0f, 0f, 0.1f), r * 0.85f, 4, 10, 1f);
        b.Box(Sl.Wood, c + new Vector3(0f, 0f, 0.22f), new Vector3(0.07f, r * 1.8f, 0.07f));
        b.Box(Sl.Wood, c + new Vector3(0f, 0f, 0.22f), new Vector3(r * 1.8f, 0.07f, 0.07f));
    }

    /// <summary>Lentera kertas bulat menggantung dari lengan besi.</summary>
    public static void HangLantern(GeoBag b, Vector3 wallPt, float arm)
    {
        b.Beam(Sl.Metal, wallPt, wallPt + new Vector3(0f, 0f, arm), 0.06f, 0.06f);
        Vector3 e = wallPt + new Vector3(0f, 0f, arm);
        b.Beam(Sl.Metal, e, e + new Vector3(0f, -0.28f, 0f), 0.025f, 0.025f);
        b.Ball(Sl.Glow, e + new Vector3(0f, -0.55f, 0f), 0.26f, 4, 8, 1.2f);
        b.Cyl(Sl.Gold, e + new Vector3(0f, -0.3f, 0f), 0.12f, 0.06f, 8, 0.12f, true);
    }

    /// <summary>Lis atap bergelombang (sisik) sepanjang tepi depan. y = tinggi tepi, z = posisi tepi, ov = tonjolan.</summary>
    public static void EaveScallops(GeoBag b, float x0, float x1, float y, float z, int slot)
    {
        float len = x1 - x0;
        int n = Mathf.Max(4, Mathf.RoundToInt(len / 0.5f));
        float step = len / n;
        for (int i = 0; i < n; i++)
            b.Ball(slot, new Vector3(x0 + step * (i + 0.5f), y, z), step * 0.55f, 3, 6, 0.55f);
    }

    /// <summary>Bendera pita kecil di puncak atap.</summary>
    public static void PennantTop(GeoBag b, Vector3 top, int slot)
    {
        b.Beam(Sl.Metal, top, top + new Vector3(0f, 1.6f, 0f), 0.05f, 0.05f);
        Vector3 q = top + new Vector3(0f, 1.55f, 0f);
        Tri2(b, slot, q, q + new Vector3(0f, -0.55f, 0f), q + new Vector3(1.0f, -0.28f, 0.1f));
        b.Ball(Sl.Gold, top + new Vector3(0f, 1.62f, 0f), 0.1f, 3, 5);
    }
}
