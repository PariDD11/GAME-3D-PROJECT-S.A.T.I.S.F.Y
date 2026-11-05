using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bagian bangunan megah Kota Ashwatch: gerbang portal runcing, jendela mawar, menara bulat berkorbel, spanduk, penopang, atap pelana besar.
/// Dipakai oleh Guild, katedral, kastil, akademi, barak, rumah bangsawan.
/// </summary>
public static partial class T2Kit
{
    // ------------------------------------------------------------------ poligon isi di bidang z (kipas dari titik pusat)
    static void FanPoly(GeoBag b, int s, Vector3 c, List<Vector3> pts, bool front)
    {
        for (int i = 0; i + 1 < pts.Count; i++)
        {
            if (front) b.Tri(s, c, pts[i], pts[i + 1]);
            else b.Tri(s, c, pts[i + 1], pts[i]);
        }
    }

    /// <summary>Titik busur runcing (lengkung segitiga sama sisi) dari pegas kiri ke kanan: lebar w, pegas di y = ys, bidang z.</summary>
    static List<Vector3> PointedArcPts(float cx, float ys, float w, float z, int n)
    {
        var pts = new List<Vector3>();
        float hw = w * 0.5f;
        for (int i = 0; i <= n; i++)
        {
            float a = Mathf.Lerp(180f, 120f, i / (float)n) * Mathf.Deg2Rad;              // lengkung kiri: pusat di pegas kanan
            pts.Add(new Vector3(cx + hw + Mathf.Cos(a) * w, ys + Mathf.Sin(a) * w, z));
        }
        for (int i = 1; i <= n; i++)
        {
            float a = Mathf.Lerp(60f, 0f, i / (float)n) * Mathf.Deg2Rad;                  // lengkung kanan: pusat di pegas kiri
            pts.Add(new Vector3(cx - hw + Mathf.Cos(a) * w, ys + Mathf.Sin(a) * w, z));
        }
        return pts;
    }

    /// <summary>Portal runcing bertingkat di bidang z = 0 (muka +Z): lebar bukaan w, tinggi pegas hs, jumlah lapis lengkung. Daun pintu kayu berpita besi.</summary>
    public static void PointedPortal(GeoBag b, float cx, float w, float hs, int layers, bool doors)
    {
        float hw = w * 0.5f;
        var arc = PointedArcPts(cx, hs, w, 0.03f, 8);
        // rongga gelap + daun pintu
        b.Quad(Sl.Dark, new Vector3(cx - hw, 0f, 0.03f), new Vector3(cx + hw, 0f, 0.03f), new Vector3(cx + hw, hs, 0.03f), new Vector3(cx - hw, hs, 0.03f));
        FanPoly(b, Sl.Dark, new Vector3(cx, hs, 0.03f), arc, true);
        if (doors)
        {
            int np = Mathf.Max(6, Mathf.RoundToInt(w / 0.4f));
            for (int k = 0; k < np; k++)
            {
                float a = cx - hw + k * w / np + 0.012f, c = cx - hw + (k + 1) * w / np - 0.012f;
                b.Quad(k % 2 == 0 ? Sl.Wood : Sl.WoodLight, new Vector3(a, 0f, 0.1f), new Vector3(c, 0f, 0.1f), new Vector3(c, hs, 0.1f), new Vector3(a, hs, 0.1f));
            }
            var arc2 = PointedArcPts(cx, hs, w - 0.04f, 0.1f, 8);
            FanPoly(b, Sl.Wood, new Vector3(cx, hs, 0.1f), arc2, true);
            Strip(b, Sl.Metal, cx - hw, cx + hw, hs * 0.2f, hs * 0.2f + 0.14f, 0.1f, 0.04f);
            Strip(b, Sl.Metal, cx - hw, cx + hw, hs * 0.62f, hs * 0.62f + 0.14f, 0.1f, 0.04f);
            Strip(b, Sl.Metal, cx - 0.03f, cx + 0.03f, 0f, hs + 0.9f, 0.1f, 0.05f);
            b.Ball(Sl.Metal, new Vector3(cx - 0.25f, hs * 0.5f, 0.17f), 0.1f, 3, 6);
            b.Ball(Sl.Metal, new Vector3(cx + 0.25f, hs * 0.5f, 0.17f), 0.1f, 3, 6);
        }
        // lapis lengkung batu
        for (int i = 0; i < layers; i++)
        {
            float r0 = w + i * 0.3f, r1 = r0 + 0.3f, z0 = -0.02f + i * 0.02f, z1 = 0.16f + i * 0.04f;
            int s = i % 2 == 0 ? Sl.DarkStone : Sl.Stone;
            V2Kit.ArchRing(b, s, cx + hw, hs, r0, r1, 120f, 180f, 8, z0, z1);
            V2Kit.ArchRing(b, s, cx - hw, hs, r0, r1, 0f, 60f, 8, z0, z1);
            float jx = hw + i * 0.3f;
            b.Box(s, new Vector3(cx - jx - 0.15f, hs * 0.5f, (z0 + z1) * 0.5f), new Vector3(0.3f, hs, z1 - z0));
            b.Box(s, new Vector3(cx + jx + 0.15f, hs * 0.5f, (z0 + z1) * 0.5f), new Vector3(0.3f, hs, z1 - z0));
        }
        // batu kunci di puncak
        b.Box(Sl.Gold, new Vector3(cx, hs + w * 0.866f + layers * 0.3f + 0.1f, 0.14f), new Vector3(0.22f, 0.3f, 0.1f));
    }

    /// <summary>Jendela mawar bundar pada bidang z = 0.</summary>
    public static void RoseWindow(GeoBag b, float cx, float cy, float r, int spokes)
    {
        V2Kit.DiscV(b, Sl.Dark, cx, cy, r, 0.04f, 20);
        V2Kit.DiscV(b, Sl.Glass, cx, cy, r * 0.96f, 0.08f, 20);
        V2Kit.ArchRing(b, Sl.DarkStone, cx, cy, r * 0.9f, r * 1.22f, 0f, 360f, 20, 0f, 0.2f);
        for (int k = 0; k < spokes; k++)
        {
            float a = k * Mathf.PI / spokes;
            b.Beam(Sl.DarkStone, new Vector3(cx + Mathf.Cos(a) * r * 0.94f, cy + Mathf.Sin(a) * r * 0.94f, 0.12f), new Vector3(cx - Mathf.Cos(a) * r * 0.94f, cy - Mathf.Sin(a) * r * 0.94f, 0.12f), 0.08f, 0.07f, new Vector3(0f, 0f, 1f));
        }
        V2Kit.ArchRing(b, Sl.Gold, cx, cy, r * 0.22f, r * 0.34f, 0f, 360f, 10, 0.06f, 0.2f);
    }

    /// <summary>Jendela runcing tinggi berbingkai (panjang h bagian lurus) pada bidang z = 0.</summary>
    public static void TallWindow(GeoBag b, float cx, float y0, float w, float h, int trace)
    {
        V2Kit.Lancet(b, cx, y0, w, h);
        if (trace > 0)
        {
            b.Box(Sl.DarkStone, new Vector3(cx - w * 0.25f, y0 + h * 0.84f, 0.12f), new Vector3(0.05f, h * 0.3f, 0.04f));
            b.Box(Sl.DarkStone, new Vector3(cx + w * 0.25f, y0 + h * 0.84f, 0.12f), new Vector3(0.05f, h * 0.3f, 0.04f));
            b.Box(Sl.DarkStone, new Vector3(cx, y0 + h * 0.3f, 0.12f), new Vector3(w, 0.05f, 0.04f));
        }
        b.Box(Sl.DarkStone, new Vector3(cx, y0 - 0.06f, 0.1f), new Vector3(w + 0.55f, 0.14f, 0.22f));
    }

    /// <summary>Penopang batu bertingkat menempel di dinding (pada bidang z = 0, menonjol +Z).</summary>
    public static void Buttress(GeoBag b, float x, float y0, float h, float proj)
    {
        b.Box(Sl.Stone, new Vector3(x, y0 + h * 0.5f, proj * 0.5f), new Vector3(0.7f, h, proj));
        b.Box(Sl.Stone, new Vector3(x, y0 + h * 0.78f, proj * 0.5f + 0.1f), new Vector3(0.58f, h * 0.44f, proj * 0.8f));
        // atas miring
        b.Beam(Sl.DarkStone, new Vector3(x, y0 + h * 0.98f, 0.1f), new Vector3(x, y0 + h * 0.62f, proj * 0.95f), 0.66f, 0.1f, new Vector3(0f, 1f, 0f));
        b.Box(Sl.DarkStone, new Vector3(x, y0 + h * 0.5f, proj + 0.02f), new Vector3(0.78f, 0.1f, 0.1f));
    }

    /// <summary>Spanduk tegak berekor garpu digantung pada tiang; muka +Z. top = titik gantung atas.</summary>
    public static void Banner(GeoBag b, Vector3 top, float w, float h, int slot, int slotTrim)
    {
        b.Beam(Sl.Metal, new Vector3(top.x - w * 0.6f, top.y, top.z), new Vector3(top.x + w * 0.6f, top.y, top.z), 0.07f, 0.07f);
        b.Ball(Sl.Gold, new Vector3(top.x - w * 0.6f, top.y, top.z), 0.07f, 3, 5);
        b.Ball(Sl.Gold, new Vector3(top.x + w * 0.6f, top.y, top.z), 0.07f, 3, 5);
        float zz = top.z + 0.04f;
        b.Quad(slot, new Vector3(top.x - w * 0.5f, top.y - h * 0.82f, zz), new Vector3(top.x + w * 0.5f, top.y - h * 0.82f, zz), new Vector3(top.x + w * 0.5f, top.y - 0.04f, zz), new Vector3(top.x - w * 0.5f, top.y - 0.04f, zz));
        b.Tri(slot, new Vector3(top.x - w * 0.5f, top.y - h * 0.82f, zz), new Vector3(top.x, top.y - h * 0.62f, zz), new Vector3(top.x, top.y - h, zz));
        b.Tri(slot, new Vector3(top.x, top.y - h * 0.62f, zz), new Vector3(top.x + w * 0.5f, top.y - h * 0.82f, zz), new Vector3(top.x, top.y - h, zz));
        b.Quad(slotTrim, new Vector3(top.x - w * 0.5f, top.y - 0.16f, zz + 0.01f), new Vector3(top.x + w * 0.5f, top.y - 0.16f, zz + 0.01f), new Vector3(top.x + w * 0.5f, top.y - 0.04f, zz + 0.01f), new Vector3(top.x - w * 0.5f, top.y - 0.04f, zz + 0.01f));
        b.Ball(Sl.Gold, new Vector3(top.x, top.y - h * 0.42f, zz + 0.03f), w * 0.2f, 3, 6, 1.1f);
    }

    /// <summary>Menara bulat berkorbel dan beratap kerucut (atau berbenteng terbuka). y0 = dasar, yb = puncak badan.</summary>
    public static void RoundTower(GeoBag b, float cx, float cz, float r, float y0, float yb, float capH, int sWall, int sCap, int seed, int wins)
    {
        b.Cyl(Sl.DarkStone, new Vector3(cx, y0 - 0.6f, cz), r * 1.2f, 1.6f, 14, r * 1.1f, true);
        b.Cyl(Sl.DarkStone, new Vector3(cx, y0 + 0.9f, cz), r * 1.08f, 0.9f, 14, r * 1.01f, false);
        b.Cyl(sWall, new Vector3(cx, y0 + 1.7f, cz), r, yb - y0 - 1.7f - 1.1f, 14, r * 0.985f, false);
        // cincin korbel (machicolation) di bawah puncak
        b.Cyl(Sl.DarkStone, new Vector3(cx, yb - 1.35f, cz), r * 0.98f, 0.7f, 14, r * 1.18f, false);
        b.Cyl(sWall, new Vector3(cx, yb - 0.65f, cz), r * 1.18f, 0.95f, 14, r * 1.18f, true);
        b.Cyl(Sl.DarkStone, new Vector3(cx, yb + 0.28f, cz), r * 1.22f, 0.16f, 14, r * 1.22f, true);
        // celah panah + jendela
        for (int k = 0; k < wins; k++)
        {
            float a = (k * 360f / Mathf.Max(1, wins) + Rnd(seed, k) * 25f) * Mathf.Deg2Rad;
            float yy = y0 + 3.0f + (yb - y0 - 7f) * (k % 3) / 3f * 1.1f + Rnd(seed, k + 9) * 1.2f;
            b.Push(new Vector3(cx + Mathf.Sin(a) * (r + 0.02f), yy, cz + Mathf.Cos(a) * (r + 0.02f)), a * Mathf.Rad2Deg);
            b.Box(Sl.Dark, new Vector3(0f, 0.55f, 0.03f), new Vector3(0.22f, 1.1f, 0.08f));
            b.Box(Sl.DarkStone, new Vector3(0f, 1.2f, 0.04f), new Vector3(0.42f, 0.14f, 0.12f));
            b.Pop();
        }
        if (capH > 0.1f)
        {
            b.Spire(sCap, new Vector3(cx, yb + 0.36f, cz), r * 1.34f, capH, 14, 0.18f);
            b.Cyl(Sl.Gold, new Vector3(cx, yb + 0.36f + capH - 0.2f, cz), 0.07f, 1.5f, 6, 0.04f, true);
            b.Ball(Sl.Gold, new Vector3(cx, yb + 0.36f + capH + 1.3f, cz), 0.16f, 3, 6);
        }
        else
        {
            int nm = Mathf.Max(8, Mathf.RoundToInt(r * 2.8f));
            for (int k = 0; k < nm; k += 1)
            {
                if (k % 2 == 1) continue;
                float a = k * Mathf.PI * 2f / nm;
                b.Box(sWall, new Vector3(cx + Mathf.Sin(a) * r * 1.16f, yb + 0.28f + 0.5f, cz + Mathf.Cos(a) * r * 1.16f), new Vector3(r * 1.6f / nm * 3.1f, 1.0f, 0.55f), a * Mathf.Rad2Deg);
            }
        }
    }

    /// <summary>Pelana besar menghadap muka (punggung sepanjang Z): lebar span (X), panjang len (Z), pusat (cx, y, cz). Dinding gable depan dibiarkan terbuka (ditutup pemanggil).</summary>
    public static float GableZ(GeoBag b, float cx, float y, float cz, float len, float span, float pitch, float ov, float ovEnd, int seed, int sRoof, int sRidge, bool fill)
    {
        float rise = span * 0.5f * pitch;
        b.Push(new Vector3(cx, y, cz), 90f);
        TileRoof(b, sRoof, sRidge, Sl.Wood, Sl.Stone, Vector3.zero, len, span, rise, ov, ovEnd, seed, 0.5f, fill);
        b.Pop();
        return rise;
    }

    /// <summary>Balok dinding batu bergaris (dasar bangunan megah): alas y0..y1, dengan lis dan sudut batu besar.</summary>
    public static void StoneBlock(GeoBag b, float x0, float x1, float z0, float z1, float y0, float y1, int slot)
    {
        b.Box(slot, new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, y1 - y0, z1 - z0));
        // lis dasar & lis atas
        b.Box(Sl.DarkStone, new Vector3((x0 + x1) * 0.5f, y0 + 0.35f, (z0 + z1) * 0.5f), new Vector3(x1 - x0 + 0.3f, 0.7f, z1 - z0 + 0.3f));
        b.Box(Sl.DarkStone, new Vector3((x0 + x1) * 0.5f, y1 - 0.12f, (z0 + z1) * 0.5f), new Vector3(x1 - x0 + 0.5f, 0.28f, z1 - z0 + 0.5f));
    }

    /// <summary>Tangga batu lebar menuju bidang z=zTop (turun ke +Z): n anak tangga, tinggi total rise.</summary>
    public static void StairsFront(GeoBag b, float cx, float w, float zTop, float yTop, int n, float run)
    {
        float rise = yTop / n;
        for (int j = 0; j < n; j++)
        {
            float hh = yTop - j * rise;
            b.Box(Sl.Stone, new Vector3(cx, hh * 0.5f, zTop + (j + 0.5f) * run), new Vector3(w, hh, run));
        }
    }

    /// <summary>Obor/brazier berkaki di tanah (tinggi h).</summary>
    public static void Brazier(GeoBag b, Vector3 p, float h)
    {
        b.Cyl(Sl.DarkStone, p, 0.28f, 0.2f, 8, 0.22f, true);
        b.Cyl(Sl.Metal, p + new Vector3(0f, 0.2f, 0f), 0.07f, h - 0.5f, 6, 0.05f, true);
        b.Cyl(Sl.Metal, p + new Vector3(0f, h - 0.32f, 0f), 0.2f, 0.28f, 8, 0.34f, false);
        b.Ball(Sl.Glow, p + new Vector3(0f, h - 0.02f, 0f), 0.24f, 4, 7, 1.3f);
    }
}
