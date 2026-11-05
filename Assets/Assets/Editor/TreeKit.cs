using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pohon dan semak kit kota: sekarang dibangun TreeGen (lihat TreeGen.cs). File ini menyimpan alat kecil yang masih dipakai kit lain
/// (kartu daun, rumput pangkal) dan pintu masuk Build(kind, variant) untuk PropKit.TreeProto.
/// </summary>
public static class TreeKit
{
    static float H(int a, int b, int seed)
    {
        unchecked
        {
            int h = a * 374761393 + b * 668265263 + seed * 1442695041;
            h = (h ^ (h >> 13)) * 1274126177; h ^= h >> 16;
            return (h & 0xffffff) / (float)0x1000000;
        }
    }
    static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }

    /// <summary>Titik acak di belahan atas bola (dengan bias ke luar). up=bias ke atas 0..1.</summary>
    static Vector3 Dir(int k, int seed, float lowest)
    {
        float yaw = (k * 137.5f + H(k, 3, seed) * 40f) * Mathf.Deg2Rad;
        float y = Mathf.Lerp(lowest, 1f, H(k, 7, seed));
        float rr = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
        return V(Mathf.Sin(yaw) * rr, y, Mathf.Cos(yaw) * rr);
    }

    /// <summary>Kartu daun runcing menempel pada permukaan elipsoid (c, rad), menunjuk keluar dan sedikit turun.</summary>
    public static void LeafCards(GeoBag b, int slot, Vector3 c, Vector3 rad, int count, int seed, float size, float lowest = -0.35f, float droop = 0.28f)
    {
        for (int k = 0; k < count; k++)
        {
            Vector3 d = Dir(k, seed, lowest);
            Vector3 p = c + V(d.x * rad.x, d.y * rad.y, d.z * rad.z) * 0.94f;
            Vector3 t1 = Vector3.Cross(d, Vector3.up); if (t1.sqrMagnitude < 1e-4f) t1 = V(1f, 0f, 0f); t1 = t1.normalized;
            float s = size * (0.75f + 0.5f * H(k, 11, seed));
            Vector3 tip = p + d * s * 1.1f + V(0f, -droop * s, 0f);
            Vector3 mid = p + d * s * 0.5f + V(0f, 0.08f * s, 0f);
            Vector3 L = mid - t1 * s * 0.5f, R = mid + t1 * s * 0.5f;
            b.Tri(slot, p, R, L); b.Tri(slot, p, L, R);
            b.Tri(slot, L, R, tip); b.Tri(slot, R, L, tip);
        }
    }

    public static void GrassTuft(GeoBag b, int slot, Vector3 p, float h, int seed)
    {
        int n = 6;
        for (int k = 0; k < n; k++)
        {
            float a = (k * 60f + H(k, 1, seed) * 30f) * Mathf.Deg2Rad;
            float lean = 0.15f + 0.25f * H(k, 2, seed);
            Vector3 tip = p + V(Mathf.Sin(a) * h * lean, h * (0.7f + 0.5f * H(k, 3, seed)), Mathf.Cos(a) * h * lean);
            Vector3 side = V(Mathf.Cos(a), 0f, -Mathf.Sin(a)) * h * 0.07f;
            b.Tri(slot, p - side, p + side, tip); b.Tri(slot, p + side, p - side, tip);
        }
    }

    /// <summary>kind PropKit: 0 berdaun lebar (ek), 1 cemara, 2 sakura, 3 sipres, 4 semak berbunga, 5 pohon apel. variant 0..3 = bentuk berbeda.
    /// Bentuk dibuat TreeGen (batang berakar melebar, dahan terlihat, mahkota gumpalan halus); palet slot kota: Leather = kulit kayu,
    /// Hedge/Fur/Leaf = daun gelap/tengah/terang, Hair = daun hangat, Lip/Blossom/EyeW = sakura tua/biasa/pucat.</summary>
    public static void Build(GeoBag b, int kind, int variant = 0)
    {
        int vs = variant * 17;
        float sc = 1.12f + 0.08f * variant;
        switch (kind)
        {
            case 0: TreeGen.Oak(b, 11 + vs, TreeGen.Town, sc, variant); break;
            case 1: TreeGen.Spruce(b, 5 + vs, TreeGen.Town, 7.4f * sc, 2.5f * sc, 8); break;
            case 2: TreeGen.Sakura(b, 23 + vs, TreeGen.TownBlossom, sc, variant); break;
            case 3: TreeGen.Cypress(b, 7 + vs, TreeGen.Town, 8.0f * sc, 1.15f * sc); break;
            case 4: TreeGen.Bush(b, 3 + vs, new TreeGen.Pal(Sl.Leather, Sl.Hedge, Sl.Fur, Sl.Leaf, Sl.Blossom, -1), 1f, 8); break;
            default: TreeGen.Apple(b, 31 + vs, TreeGen.Town, 1.0f + 0.05f * variant, variant); break;
        }
    }
}
