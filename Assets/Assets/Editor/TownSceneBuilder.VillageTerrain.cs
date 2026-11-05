using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Bentuk medan Desa Pemula (dataran rata di dalam pagar, bukit dan gunung di luar, sungai melintasi kedua jalan)
/// dan Hutan Pemula (bukit, pegunungan pembatas dengan celah di gerbang selatan, sungai, anak sungai, kolam).</summary>
public static partial class TownSceneBuilder
{
    static float VBump(float x, float z, float cx, float cz, float r, float h)
    {
        float dx = x - cx, dz = z - cz;
        return h * VSm(1f - Mathf.Sqrt(dx * dx + dz * dz) / r);
    }

    static float[] VPaintStd(float x, float z, float h, float dirt, float cobble, float litterScale = 1f)
    {
        float slope = VSlopeDeg(x, z);
        float rock = Mathf.Max(VSm((slope - 27f) / 12f), VSm((h - 24f) / 10f) * 0.85f);
        float mud = Mathf.Clamp01(1f - (h - VWaterLevel) / 0.9f);
        float litter = VSm((VN(x, z, 0.022f, 5f) - 0.42f) / 0.25f) * 0.9f * litterScale;
        float moss = VSm((VN(x, z, 0.03f, 9f) - 0.6f) / 0.2f) * 0.8f;
        if (h < VWaterLevel + 1.2f) moss = Mathf.Max(moss, 0.4f);
        var w = new float[7];
        float rem = 1f;
        float wDirt = Mathf.Min(rem, dirt); rem -= wDirt;
        float wCob = Mathf.Min(rem, cobble); rem -= wCob;
        float wRock = Mathf.Min(rem, rock); rem -= wRock;
        float wMud = Mathf.Min(rem, mud); rem -= wMud;
        float wLit = rem * litter; rem -= wLit;
        float wMoss = rem * moss; rem -= wMoss;
        w[0] = rem; w[1] = wDirt; w[2] = wRock; w[3] = wMud; w[4] = wLit; w[5] = wMoss; w[6] = wCob;
        return w;
    }

    static System.Func<float, Vector3> VPolyAt(List<Vector2> pts, out float total)
    {
        var cum = new List<float> { 0f };
        for (int i = 1; i < pts.Count; i++) cum.Add(cum[i - 1] + (pts[i] - pts[i - 1]).magnitude);
        total = cum[cum.Count - 1];
        var c = cum; var p = pts;
        return d =>
        {
            d = Mathf.Clamp(d, 0f, c[c.Count - 1]);
            int i = 1;
            while (i < c.Count - 1 && c[i] < d) i++;
            float t = (d - c[i - 1]) / Mathf.Max(0.0001f, c[i] - c[i - 1]);
            Vector2 q = Vector2.Lerp(p[i - 1], p[i], t);
            return new Vector3(q.x, 0f, q.y);
        };
    }

    // =====================================================================
    // Desa Pemula
    // =====================================================================
    static List<Vector2> vRoadN, vRoadE;

    static void VillageTerrain()
    {
        vRoadN = new List<Vector2>(); vRoadE = new List<Vector2>();
        for (float d = 0f; d <= (vMerged ? VSeamZ + 12f : VEnd + 12f); d += 4f)
        {
            Vector3 a = VRoadAt(0f, d), b = VRoadAt(90f, d);
            vRoadN.Add(new Vector2(a.x, a.z)); vRoadE.Add(new Vector2(b.x, b.z));
        }
        var riverBase = new[] { V(-190f, 125f), V(-105f, 104f), V(-38f, 90f), V(2f, 84f), V(54f, 87f), V(80f, 68f), V(86f, 28f),
            V(84f, -4f), V(88f, -48f), V(115f, -98f), V(190f, -122f) };
        for (int ri = 0; ri < riverBase.Length; ri++) riverBase[ri] *= 1.45f;      // desa lebih besar: sungai digeser keluar
        var river = VSmoothPoly(riverBase, 8);
        var rN = vRoadN; var rE = vRoadE;
        vH = (x, z) =>
        {
            float r = Mathf.Sqrt(x * x + z * z);
            float edge = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
            float hill = (VN(x, z, 0.006f, 3f) - 0.5f) * 2f * 7f + (VN(x, z, 0.02f, 9f) - 0.5f) * 2f * 2.2f + (VN(x, z, 0.05f, 5f) - 0.5f) * 0.8f;
            if (hill < 0f) hill *= 0.4f;
            float rid = 1f - Mathf.Abs(2f * VN(x, z, 0.011f, 21f) - 1f);
            float mount = VSm((edge - 148f) / 55f) * (26f + 30f * rid * rid + 10f * VN(x, z, 0.03f, 2f));
            float h = (hill + mount) * VSm((r - 114f) / 34f);
            float dR = Mathf.Min(VDistPoly(x, z, rN), VDistPoly(x, z, rE));
            h *= VSm((dR - 9f) / 32f);                                              // jalan = lembah/celah rata
            h = Mathf.Lerp(h, -4.5f, VSm(1f - VDistPoly(x, z, river) / 13f));          // sungai
            return h;
        };
        vGrassDens = 0.78f;   // rumput detail lebih rapat di desa
        VBuildTerrain("VillageTerrain", 240f, (x, z, h) =>
        {
            float dR = Mathf.Min(VDistPoly(x, z, rN), VDistPoly(x, z, rE));
            float dirt = VSm(1f - dR / 4.2f);
            float cob = VSm((15f - Mathf.Sqrt(x * x + z * z)) / 2f);
            return VPaintStd(x, z, h, dirt, cob, Mathf.Lerp(0.12f, 1f, VSm((Mathf.Sqrt(x * x + z * z) - 100f) / 25f)));   // di dalam desa: rumput hijau, bukan bercak cokelat
        });
        vGrassDens = 0.4f;
        VBridgesAlong("Utara", d => VRoadAt(0f, d), VR + 4f, VEnd, 5.2f);
        VBridgesAlong("Timur", d => VRoadAt(90f, d), VR + 4f, VEnd, 5.2f);
    }

    // =====================================================================
    // Hutan Pemula
    // =====================================================================
    static readonly List<List<Vector2>> vTrails = new List<List<Vector2>>();
    static float TrailX(float z) { return 14f * Mathf.Sin(z / 45f) * Mathf.Clamp01((z + 90f) / 45f); }

    static void VDefineTrails()
    {
        vTrails.Clear();
        var main = new List<Vector2>();
        for (float z = -175f; z <= 100f; z += 5f) main.Add(V(TrailX(z), z));
        vTrails.Add(main);
        vTrails.Add(new List<Vector2> { V(TrailX(-80f), -80f), V(-18f, -66f), V(-30f, -58f) });
        vTrails.Add(new List<Vector2> { V(TrailX(-70f), -70f), V(20f, -60f), V(32f, -50f) });
        vTrails.Add(new List<Vector2> { V(TrailX(-6f), -6f), V(18f, 10f), V(22f, 28f) });
        vTrails.Add(new List<Vector2> { V(TrailX(24f), 24f), V(-12f, 38f), V(-34f, 50f) });
        vTrails.Add(new List<Vector2> { V(TrailX(70f), 70f), V(30f, 78f), V(40f, 82f) });
    }

    static void ForestTerrain()
    {
        VDefineTrails();
        var river = VSmoothPoly(new[] { V(-175f, -5f), V(-110f, -14f), V(-50f, -30f), V(-7f, -24f), V(35f, -14f), V(90f, -30f), V(130f, -48f), V(175f, -40f) }, 8);
        var creek = VSmoothPoly(new[] { V(-175f, 70f), V(-110f, 58f), V(-50f, 52f), V(-8f, 62f), V(30f, 60f), V(80f, 72f), V(120f, 64f), V(175f, 80f) }, 8);
        Vector2 camp = FCamp;
        vH = (x, z) =>
        {
            float edge = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
            float hill = (VN(x, z, 0.006f, 3f) - 0.5f) * 2f * 8f + (VN(x, z, 0.02f, 9f) - 0.5f) * 2f * 2.5f + (VN(x, z, 0.05f, 5f) - 0.5f) * 0.9f;
            if (hill < 0f) hill *= 0.4f;
            float rid = 1f - Mathf.Abs(2f * VN(x, z, 0.011f, 21f) - 1f);
            float mount = VSm((edge - 105f) / 55f) * (26f + 32f * rid * rid + 10f * VN(x, z, 0.03f, 2f));
            float bumps = VBump(x, z, -95f, 10f, 38f, 18f) + VBump(x, z, 90f, 48f, 32f, 14f) + VBump(x, z, -72f, -100f, 28f, 12f) + VBump(x, z, 82f, -100f, 28f, 12f);
            float h = hill + mount + bumps;
            float dc = Mathf.Sqrt((x - camp.x) * (x - camp.x) + (z - camp.y) * (z - camp.y));
            float flatC = 1f - VSm((dc - 26f) / 28f);
            float flatG = 1f - VSm((VDistSeg(x, z, 0f, -200f, 0f, -80f) - 12f) / 26f);
            h *= 1f - Mathf.Max(flatC, flatG);
            h = Mathf.Lerp(h, -4.5f, VSm(1f - VDistPoly(x, z, river) / 12f));
            h = Mathf.Lerp(h, -3.6f, VSm(1f - VDistPoly(x, z, creek) / 6f));
            float dp = Mathf.Sqrt((x - 80f) * (x - 80f) + (z - 10f) * (z - 10f));
            h = Mathf.Lerp(h, -4f, VSm(1f - dp / 22f));
            if (vMerged && vHVillage != null)
            {
                // sambung mulus ke terrain desa: di tepi selatan hutan tinggi = tinggi desa pada koordinat dunia yang sama
                float w = 1f - VSm((z + VForestHalf) / 45f);
                if (w > 0f) h = Mathf.Lerp(h, vHVillage(x, z + VForestZ0), w);
            }
            return h;
        };
        VBuildTerrain("ForestTerrain", 170f, (x, z, h) =>
        {
            float dmin = 9999f;
            foreach (var t in vTrails) dmin = Mathf.Min(dmin, VDistPoly(x, z, t));
            float dirt = VSm(1f - dmin / 3.2f);
            float dc = Mathf.Sqrt((x - camp.x) * (x - camp.x) + (z - camp.y) * (z - camp.y));
            dirt = Mathf.Max(dirt, 1f - VSm((dc - 14f) / 6f));
            return VPaintStd(x, z, h, dirt, 0f);
        });
    }
}
