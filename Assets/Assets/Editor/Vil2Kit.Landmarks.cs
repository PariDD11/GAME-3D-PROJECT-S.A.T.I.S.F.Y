using System.Collections.Generic;
using UnityEngine;

/// <summary>Penanda wilayah Desa Pemula: gereja batu di bukit, lumbung besar, kincir air, jembatan batu & titian kayu, gerbang kayu beratap.</summary>
public static partial class V2Kit
{
    static Vector3 Zv(float z) { return new Vector3(0f, 0f, z); }

    /// <summary>Lingkaran/kipas tegak pada bidang XY di z (normal +Z), sudut a0..a1 derajat (0 = +X).</summary>
    public static void DiscV(GeoBag b, int s, float cx, float cy, float r, float z, int n, float a0 = 0f, float a1 = 360f)
    {
        for (int i = 0; i < n; i++)
        {
            float t0 = Mathf.Lerp(a0, a1, i / (float)n) * Mathf.Deg2Rad, t1 = Mathf.Lerp(a0, a1, (i + 1) / (float)n) * Mathf.Deg2Rad;
            b.Tri(s, new Vector3(cx, cy, z), new Vector3(cx + Mathf.Cos(t0) * r, cy + Mathf.Sin(t0) * r, z), new Vector3(cx + Mathf.Cos(t1) * r, cy + Mathf.Sin(t1) * r, z));
        }
    }

    /// <summary>Busur batu tebal pada bidang XY (diekstrusi dari z0 ke z1): r0 dalam, r1 luar, sudut a0..a1 (0 = +X, 90 = +Y).</summary>
    public static void ArchRing(GeoBag b, int s, float cx, float cy, float r0, float r1, float a0, float a1, int n, float z0, float z1)
    {
        for (int i = 0; i < n; i++)
        {
            float t0 = Mathf.Lerp(a0, a1, i / (float)n) * Mathf.Deg2Rad, t1 = Mathf.Lerp(a0, a1, (i + 1) / (float)n) * Mathf.Deg2Rad;
            float tm = (t0 + t1) * 0.5f;
            Vector3 mid = new Vector3(Mathf.Cos(tm), Mathf.Sin(tm), 0f);
            Vector3 i0 = new Vector3(cx + Mathf.Cos(t0) * r0, cy + Mathf.Sin(t0) * r0, 0f), i1 = new Vector3(cx + Mathf.Cos(t1) * r0, cy + Mathf.Sin(t1) * r0, 0f);
            Vector3 o0 = new Vector3(cx + Mathf.Cos(t0) * r1, cy + Mathf.Sin(t0) * r1, 0f), o1 = new Vector3(cx + Mathf.Cos(t1) * r1, cy + Mathf.Sin(t1) * r1, 0f);
            Vector3 f = Zv(z1), k = Zv(z0);
            QOut(b, s, i0 + f, i1 + f, o1 + f, o0 + f, new Vector3(0f, 0f, 1f));
            QOut(b, s, i0 + k, i1 + k, o1 + k, o0 + k, new Vector3(0f, 0f, -1f));
            QOut(b, s, i0 + k, i1 + k, i1 + f, i0 + f, -mid);
            QOut(b, s, o0 + k, o1 + k, o1 + f, o0 + f, mid);
        }
    }

    /// <summary>Jendela lancet (runcing) pada bidang z = 0 (muka +Z): x tengah, y0 dasar, w lebar, h tinggi bagian lurus.</summary>
    public static void Lancet(GeoBag b, float x, float y0, float w, float h)
    {
        float hw = w * 0.5f, pk = w * 0.62f;
        b.Box(Sl.DarkStone, new Vector3(x, y0 + h * 0.5f, 0.03f), new Vector3(w + 0.36f, h + 0.1f, 0.12f));
        b.Tri(Sl.DarkStone, new Vector3(x - hw - 0.18f, y0 + h + 0.05f, 0.09f), new Vector3(x + hw + 0.18f, y0 + h + 0.05f, 0.09f), new Vector3(x, y0 + h + 0.05f + pk + 0.2f, 0.09f));
        b.Box(Sl.Glass, new Vector3(x, y0 + h * 0.5f, 0.08f), new Vector3(w, h, 0.05f));
        b.Tri(Sl.Glass, new Vector3(x - hw, y0 + h, 0.11f), new Vector3(x + hw, y0 + h, 0.11f), new Vector3(x, y0 + h + pk, 0.11f));
        b.Box(Sl.DarkStone, new Vector3(x, y0 + h * 0.5f, 0.12f), new Vector3(0.07f, h, 0.05f));
        b.Box(Sl.DarkStone, new Vector3(x, y0 + h * 0.64f, 0.12f), new Vector3(w, 0.06f, 0.05f));
    }

    // ------------------------------------------------------------------ gereja batu (muka +Z, menara di depan, apsis di belakang)
    public static float Chapel(GeoBag b)
    {
        const float nw = 6.8f, zF = 4.2f, zB = -6.0f, wallH = 5.4f, base0 = 0.6f;
        float zc = (zF + zB) * 0.5f, nl = zF - zB;
        // alas batu gelap
        b.Box(Sl.DarkStone, new Vector3(0f, base0 * 0.5f, zc), new Vector3(nw + 0.7f, base0, nl + 0.7f));
        b.Cyl(Sl.DarkStone, new Vector3(0f, 0f, zB), 3.75f, base0, 12, 3.75f, true);
        b.Box(Sl.DarkStone, new Vector3(0f, base0 * 0.5f, zF + 2.4f), new Vector3(5.6f, base0, 5.6f));
        // dinding lambung
        for (int sx = -1; sx <= 1; sx += 2)
        {
            b.Box(Sl.Stone, new Vector3(sx * nw * 0.5f, base0 + wallH * 0.5f, zc), new Vector3(0.6f, wallH, nl));
            // penopang (buttress) bertingkat
            for (int k = 0; k < 4; k++)
            {
                float z = zB + 1.6f + k * 2.6f;
                b.Box(Sl.Stone, new Vector3(sx * (nw * 0.5f + 0.4f), base0 + 1.4f, z), new Vector3(0.5f, 2.8f, 0.55f));
                b.Box(Sl.Stone, new Vector3(sx * (nw * 0.5f + 0.34f), base0 + 3.2f, z), new Vector3(0.38f, 1.6f, 0.45f));
                b.Box(Sl.DarkStone, new Vector3(sx * (nw * 0.5f + 0.34f), base0 + 4.1f, z), new Vector3(0.44f, 0.18f, 0.52f));
            }
            // jendela lancet di antara penopang
            for (int k = 0; k < 3; k++)
            {
                float z = zB + 2.9f + k * 2.6f;
                b.Push(new Vector3(sx * (nw * 0.5f + 0.3f), 0f, z), sx > 0 ? 90f : 270f);
                Lancet(b, 0f, base0 + 1.7f, 0.74f, 2.2f);
                b.Pop();
            }
            // cornice
            b.Box(Sl.DarkStone, new Vector3(sx * (nw * 0.5f), base0 + wallH + 0.05f, zc), new Vector3(0.9f, 0.18f, nl + 0.2f));
        }
        // apsis (silinder 10 sisi) + atap kerucut
        b.Cyl(Sl.Stone, new Vector3(0f, base0, zB), 3.45f, wallH, 10, 3.45f, false);
        b.Cyl(Sl.DarkStone, new Vector3(0f, base0 + wallH, zB), 3.7f, 0.18f, 10, 3.7f, true);
        b.Spire(Sl.RoofB, new Vector3(0f, base0 + wallH + 0.18f, zB), 3.7f, 3.4f, 10, 0.1f);
        for (int k = 0; k < 3; k++)
        {
            float a = (k - 1) * 0.7f;
            b.Push(new Vector3(Mathf.Sin(a) * 3.5f, 0f, zB - Mathf.Cos(a) * 3.5f), 180f + a * Mathf.Rad2Deg);
            Lancet(b, 0f, base0 + 1.8f, 0.7f, 2.2f);
            b.Pop();
        }
        // atap lambung (punggung sepanjang Z)
        b.Push(new Vector3(0f, base0 + wallH, zc), 90f);
        ShingleRoof(b, Sl.RoofB, Sl.DarkStone, Sl.Wood, Sl.Stone, Vector3.zero, nl, nw + 0.2f, 3.1f, 0.5f, 0.35f, true, 31);
        b.Pop();
        // menara lonceng
        float tw = 4.8f, tz = zF + 2.4f, tb = base0, th1 = 8.6f;
        b.Box(Sl.Stone, new Vector3(0f, tb + th1 * 0.5f, tz), new Vector3(tw, th1, tw));
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                b.Box(Sl.Stone, new Vector3(sx * (tw * 0.5f + 0.18f), tb + 1.6f, tz + sz * (tw * 0.5f + 0.18f)), new Vector3(0.7f, 3.2f, 0.7f));
                b.Box(Sl.DarkStone, new Vector3(sx * (tw * 0.5f + 0.18f), tb + 3.3f, tz + sz * (tw * 0.5f + 0.18f)), new Vector3(0.8f, 0.2f, 0.8f));
            }
        b.Box(Sl.DarkStone, new Vector3(0f, tb + 3.6f, tz), new Vector3(tw + 0.3f, 0.2f, tw + 0.3f));
        b.Box(Sl.DarkStone, new Vector3(0f, tb + th1, tz), new Vector3(tw + 0.5f, 0.28f, tw + 0.5f));
        // pintu lengkung + tangga
        b.Push(new Vector3(0f, 0f, tz + tw * 0.5f), 0f);
        b.Box(Sl.DarkStone, new Vector3(0f, tb + 1.7f, 0.05f), new Vector3(2.7f, 3.4f, 0.16f));
        ArchRing(b, Sl.DarkStone, 0f, tb + 3.0f, 1.0f, 1.42f, 0f, 180f, 10, 0.0f, 0.2f);
        b.Box(Sl.Wood, new Vector3(0f, tb + 1.2f, 0.12f), new Vector3(2.0f, 2.4f, 0.08f));
        DiscV(b, Sl.Wood, 0f, tb + 2.4f, 1.0f, 0.12f, 10, 0f, 180f);
        for (int k = -2; k <= 2; k++) b.Box(Sl.WoodLight, new Vector3(k * 0.38f, tb + 1.2f, 0.17f), new Vector3(0.05f, 2.4f, 0.03f));
        b.Box(Sl.Metal, new Vector3(0f, tb + 1.0f, 0.2f), new Vector3(1.9f, 0.08f, 0.03f));
        b.Box(Sl.Metal, new Vector3(0f, tb + 1.7f, 0.2f), new Vector3(1.9f, 0.08f, 0.03f));
        b.Stairs(Sl.Stone, new Vector3(0f, 0f, 0.9f), 3.4f, 3, 0.2f, 0.45f, 180f);
        if (SaltAtDoor) Salt(b, 0f, 0.27f, tb + 0.012f, 17, 1.05f);                                  // garam di ambang gereja (n82)
        // jendela mawar di atas pintu
        b.Box(Sl.DarkStone, new Vector3(0f, tb + 5.9f, 0.04f), new Vector3(1.9f, 1.9f, 0.1f));
        DiscV(b, Sl.DarkStone, 0f, tb + 5.9f, 1.05f, 0.1f, 16);
        DiscV(b, Sl.Glass, 0f, tb + 5.9f, 0.82f, 0.13f, 16);
        for (int k = 0; k < 4; k++) { float a = k * 45f * Mathf.Deg2Rad; b.Beam(Sl.DarkStone, new Vector3(Mathf.Cos(a) * 0.82f, tb + 5.9f + Mathf.Sin(a) * 0.82f, 0.15f), new Vector3(-Mathf.Cos(a) * 0.82f, tb + 5.9f - Mathf.Sin(a) * 0.82f, 0.15f), 0.07f, 0.05f, new Vector3(0f, 0f, 1f)); }
        // lampu dinding dua sisi pintu
        for (int sx = -1; sx <= 1; sx += 2) { b.Box(Sl.Metal, new Vector3(sx * 1.9f, tb + 2.7f, 0.18f), new Vector3(0.06f, 0.5f, 0.06f)); b.Ball(Sl.Glow, new Vector3(sx * 1.9f, tb + 2.35f, 0.26f), 0.14f, 3, 6, 1.2f); }
        b.Pop();
        // ruang lonceng: empat tiang sudut, bukaan lengkung, lonceng
        float by0 = tb + th1 + 0.14f, bh = 3.0f;
        b.Box(Sl.Dark, new Vector3(0f, by0 + bh * 0.5f, tz), new Vector3(tw - 0.6f, bh, tw - 0.6f));
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                b.Box(Sl.Stone, new Vector3(sx * (tw * 0.5f - 0.3f), by0 + bh * 0.5f, tz + sz * (tw * 0.5f - 0.3f)), new Vector3(0.6f, bh, 0.6f));
        for (int sd = 0; sd < 4; sd++)
        {
            b.Push(new Vector3(0f, 0f, tz), sd * 90f);
            float zf = tw * 0.5f - 0.02f;
            b.Box(Sl.Stone, new Vector3(0f, by0 + bh * 0.5f - 0.3f, zf - 0.28f), new Vector3(tw - 1.2f, bh - 0.6f, 0.1f), 0f, true, false);
            ArchRing(b, Sl.DarkStone, 0f, by0 + bh - 1.3f, 0.95f, 1.3f, 0f, 180f, 8, zf - 0.34f, zf + 0.06f);
            b.Box(Sl.DarkStone, new Vector3(0f, by0 + 0.12f, zf - 0.12f), new Vector3(tw - 0.9f, 0.3f, 0.4f));
            b.Pop();
        }
        b.Cyl(Sl.Gold, new Vector3(0f, by0 + bh - 2.4f, tz), 0.55f, 1.0f, 8, 0.2f, true);
        b.Beam(Sl.Wood, new Vector3(-1.3f, by0 + bh - 1.5f, tz), new Vector3(1.3f, by0 + bh - 1.5f, tz), 0.22f, 0.22f);
        b.Box(Sl.DarkStone, new Vector3(0f, by0 + bh + 0.1f, tz), new Vector3(tw + 0.4f, 0.26f, tw + 0.4f));
        // puncak menara: lantai kayu & atap runcing bersegi delapan
        b.Spire(Sl.RoofB, new Vector3(0f, by0 + bh + 0.2f, tz), 3.25f, 6.4f, 8, 0.2f);
        b.Cyl(Sl.Gold, new Vector3(0f, by0 + bh + 6.5f, tz), 0.07f, 1.1f, 6, 0.05f, true);
        b.Box(Sl.Gold, new Vector3(0f, by0 + bh + 7.2f, tz), new Vector3(0.7f, 0.1f, 0.1f));
        b.Box(Sl.Gold, new Vector3(0f, by0 + bh + 7.0f, tz), new Vector3(0.1f, 0.5f, 0.1f));
        // salib di puncak atap lambung belakang
        b.Cyl(Sl.Gold, new Vector3(0f, base0 + wallH + 3.0f, zB + 1.2f), 0.06f, 1.0f, 6, 0.05f, true);
        b.Box(Sl.Gold, new Vector3(0f, base0 + wallH + 3.75f, zB + 1.2f), new Vector3(0.6f, 0.08f, 0.08f));
        return by0 + bh + 7.4f;
    }

    // ------------------------------------------------------------------ lumbung besar (badan memanjang + beranda angkut + gudang samping)
    public static float Barn(GeoBag b)
    {
        b.Push(new Vector3(1.5f, 0f, 0f), 0f);
        var s = new HSpec { w = 11f, d = 9f, floors = 1, wallH = 4.0f, rise = 3.8f, ov = 0.8f, wall = 3, roof = 1, seed = 811, windowsFront = 0, windowsSide = 0, chimney = -1, doorX = 4.2f, porch = false, lantern = false, woodpile = false, shutters = false, flowers = false };
        float h = House(b, s);
        float zf = s.d * 0.5f, y0 = 0.45f;
        // pintu angkut besar di tengah
        b.Box(Sl.DarkStone, new Vector3(0f, y0 + 1.7f, zf + 0.04f), new Vector3(3.9f, 3.4f, 0.14f));
        for (int k = -1; k <= 1; k += 2)
        {
            b.Box(Sl.WoodLight, new Vector3(k * 0.82f, y0 + 1.6f, zf + 0.1f), new Vector3(1.6f, 3.2f, 0.1f));
            b.Beam(Sl.Wood, new Vector3(k * 0.82f - 0.7f, y0 + 0.2f, zf + 0.17f), new Vector3(k * 0.82f + 0.7f, y0 + 3.0f, zf + 0.17f), 0.13f, 0.06f, new Vector3(0f, 0f, 1f));
            b.Beam(Sl.Wood, new Vector3(k * 0.82f + 0.7f, y0 + 0.2f, zf + 0.17f), new Vector3(k * 0.82f - 0.7f, y0 + 3.0f, zf + 0.17f), 0.13f, 0.06f, new Vector3(0f, 0f, 1f));
            b.Box(Sl.Metal, new Vector3(k * 1.5f, y0 + 0.7f, zf + 0.2f), new Vector3(0.5f, 0.07f, 0.03f));
            b.Box(Sl.Metal, new Vector3(k * 1.5f, y0 + 2.5f, zf + 0.2f), new Vector3(0.5f, 0.07f, 0.03f));
        }
        b.Box(Sl.Wood, new Vector3(0f, y0 + 3.55f, zf + 0.12f), new Vector3(4.2f, 0.2f, 0.26f));
        if (SaltAtDoor) Salt(b, 0f, zf + 0.55f, 0.312f, 91, 1.95f);                                // garam di ambang pintu angkut (n82): di lantai beranda (puncak 0,30 m), bukan di dalam daun pintu
        // beranda kecil beratap pelana di depan pintu angkut (punggung sepanjang Z)
        for (int sx = -1; sx <= 1; sx += 2) Post(b, Sl.Wood, new Vector3(sx * 2.4f, 0f, zf + 2.3f), 3.9f, 0.14f);
        b.Beam(Sl.Wood, new Vector3(-2.4f, 3.95f, zf + 2.3f), new Vector3(2.4f, 3.95f, zf + 2.3f), 0.26f, 0.22f);
        b.Beam(Sl.Wood, new Vector3(-2.4f, 3.95f, zf + 0.1f), new Vector3(-2.4f, 3.95f, zf + 2.3f), 0.2f, 0.2f);
        b.Beam(Sl.Wood, new Vector3(2.4f, 3.95f, zf + 0.1f), new Vector3(2.4f, 3.95f, zf + 2.3f), 0.2f, 0.2f);
        b.Push(new Vector3(0f, 4.1f, zf + 0.75f), 90f);
        ShingleRoof(b, Sl.Roof, Sl.RoofB, Sl.Wood, Sl.WoodLight, Vector3.zero, 3.4f, 5.5f, 1.9f, 0.35f, 0.4f, true, 17);
        b.Pop();
        b.Box(Sl.Wood, new Vector3(0f, 0.15f, zf + 1.4f), new Vector3(4.8f, 0.3f, 2.4f));
        // pintu loteng + balok katrol di gable depan
        b.Box(Sl.Wood, new Vector3(0f, 5.0f, zf + 2.4f), new Vector3(1.3f, 1.3f, 0.1f));
        b.Box(Sl.WoodLight, new Vector3(0f, 5.0f, zf + 2.46f), new Vector3(1.05f, 1.05f, 0.05f));
        b.Box(Sl.Wood, new Vector3(0f, 5.0f, zf + 2.5f), new Vector3(0.07f, 1.1f, 0.03f));
        b.Beam(Sl.Wood, new Vector3(0f, 5.9f, zf + 2.2f), new Vector3(0f, 5.8f, zf + 3.5f), 0.14f, 0.14f);
        b.Ball(Sl.Metal, new Vector3(0f, 5.65f, zf + 3.4f), 0.1f, 3, 6);
        b.Beam(Sl.Thatch, new Vector3(0f, 5.6f, zf + 3.4f), new Vector3(0f, 3.2f, zf + 3.4f), 0.03f, 0.03f);
        b.Box(Sl.Thatch, new Vector3(0f, 3.0f, zf + 3.4f), new Vector3(0.5f, 0.4f, 0.4f));
        b.Pop();
        // gudang ternak terbuka di sisi kiri (atap miring satu sisi)
        float xs0 = -4.0f, xs1 = -7.4f, zs0 = 3.9f, zs1 = -3.9f;
        for (int k = 0; k < 3; k++)
            Post(b, Sl.Wood, new Vector3(xs1, 0f, Mathf.Lerp(zs0, zs1, k * 0.5f)), 2.9f, 0.13f);
        b.Beam(Sl.Wood, new Vector3(xs1, 3.0f, zs0), new Vector3(xs1, 3.0f, zs1), 0.22f, 0.2f);
        QUp(b, Sl.Roof, new Vector3(xs1 - 0.5f, 3.0f, zs0 + 0.5f), new Vector3(xs1 - 0.5f, 3.0f, zs1 - 0.5f), new Vector3(xs0 + 0.1f, 4.4f, zs1 - 0.5f), new Vector3(xs0 + 0.1f, 4.4f, zs0 + 0.5f));
        QOut(b, Sl.Wood, new Vector3(xs1 - 0.5f, 2.85f, zs0 + 0.5f), new Vector3(xs1 - 0.5f, 2.85f, zs1 - 0.5f), new Vector3(xs0 + 0.1f, 4.25f, zs1 - 0.5f), new Vector3(xs0 + 0.1f, 4.25f, zs0 + 0.5f), new Vector3(0f, -1f, 0f));
        b.Box(Sl.WoodLight, new Vector3(xs1 + 0.2f, 0.45f, zs1 + 0.1f), new Vector3(0.1f, 0.9f, 3.0f));         // palung makan
        b.Box(Sl.Thatch, new Vector3(xs1 + 1.3f, 0.35f, zs1 + 1.2f), new Vector3(1.6f, 0.7f, 1.2f));
        b.Box(Sl.Thatch, new Vector3(xs1 + 1.2f, 1.05f, zs1 + 1.0f), new Vector3(1.2f, 0.6f, 1.0f));
        return h + 2.4f;
    }

    // ------------------------------------------------------------------ kincir air: bangunan (roda terpisah, lihat MillWheel)
    public static float Mill(GeoBag b)
    {
        var s = new HSpec { w = 7.6f, d = 6.6f, floors = 2, wallH = 2.9f, rise = 3.9f, ov = 0.75f, wall = 2, roof = 1, seed = 933, windowsFront = 2, windowsSide = 0, chimney = -1, doorX = -1.2f, porch = false, lantern = true, woodpile = false, jetty = false, shutters = true, flowers = false };
        float h = House(b, s);
        float zf = s.d * 0.5f, ytop = 0.45f + s.wallH;
        // pintu muat karung di lantai atas + balok katrol
        b.Box(Sl.Wood, new Vector3(1.4f, ytop + 1.45f, zf + 0.06f), new Vector3(1.1f, 1.7f, 0.12f));
        b.Box(Sl.WoodLight, new Vector3(1.4f, ytop + 1.4f, zf + 0.11f), new Vector3(0.9f, 1.5f, 0.05f));
        b.Beam(Sl.Wood, new Vector3(1.4f, ytop + 2.5f, zf + 0.1f), new Vector3(1.4f, ytop + 2.45f, zf + 1.5f), 0.16f, 0.16f);
        b.Beam(Sl.Thatch, new Vector3(1.4f, ytop + 2.4f, zf + 1.4f), new Vector3(1.4f, ytop - 0.1f, zf + 1.4f), 0.03f, 0.03f);
        b.Box(Sl.Thatch, new Vector3(1.4f, ytop - 0.4f, zf + 1.4f), new Vector3(0.55f, 0.55f, 0.4f));
        // tangga batu & tumpukan karung
        b.Stairs(Sl.Stone, new Vector3(-1.2f, 0f, zf + 1.2f), 1.9f, 2, 0.22f, 0.5f, 180f);
        for (int k = 0; k < 4; k++) b.Ball(Sl.Cloth, new Vector3(2.3f + (k % 2) * 0.55f, 0.35f + (k / 2) * 0.4f, zf + 0.7f), 0.3f, 4, 6, 1.1f);
        // dudukan poros di sisi +X: konsol batu + bantalan
        float ax = s.w * 0.5f, ay = 2.2f;
        b.Box(Sl.DarkStone, new Vector3(ax + 0.3f, ay, 0f), new Vector3(0.45f, 0.7f, 0.7f));
        b.Box(Sl.Stone, new Vector3(ax + 1.6f, 0.3f, 0f), new Vector3(3.0f, 0.6f, 4.6f));
        // talang kayu di atas roda (dari belakang ke roda), bertumpu pada kuda-kuda
        float fy = ay + 2.2f + 0.35f;
        for (int k = 0; k < 3; k++)
        {
            float z = -0.2f - k * 3.0f;
            b.Beam(Sl.Wood, new Vector3(ax + 0.9f - 0.5f, 0f, z), new Vector3(ax + 0.9f, fy - 0.25f, z), 0.18f, 0.18f);
            b.Beam(Sl.Wood, new Vector3(ax + 0.9f + 0.5f, 0f, z), new Vector3(ax + 0.9f, fy - 0.25f, z), 0.18f, 0.18f);
        }
        float flumeZ0 = 0.3f, flumeZ1 = -6.8f;
        b.Box(Sl.Wood, new Vector3(ax + 0.9f, fy - 0.12f, (flumeZ0 + flumeZ1) * 0.5f), new Vector3(0.9f, 0.1f, flumeZ0 - flumeZ1));
        b.Box(Sl.WoodLight, new Vector3(ax + 0.9f - 0.42f, fy + 0.1f, (flumeZ0 + flumeZ1) * 0.5f), new Vector3(0.08f, 0.5f, flumeZ0 - flumeZ1));
        b.Box(Sl.WoodLight, new Vector3(ax + 0.9f + 0.42f, fy + 0.1f, (flumeZ0 + flumeZ1) * 0.5f), new Vector3(0.08f, 0.5f, flumeZ0 - flumeZ1));
        b.Box(Sl.Glass, new Vector3(ax + 0.9f, fy + 0.12f, (flumeZ0 + flumeZ1) * 0.5f), new Vector3(0.74f, 0.04f, flumeZ0 - flumeZ1 - 0.1f));
        b.Box(Sl.Wood, new Vector3(ax + 0.9f, fy + 0.45f, flumeZ1 - 0.05f), new Vector3(1.0f, 0.9f, 0.14f));          // pintu air di ujung hulu
        return h;
    }

    /// <summary>Roda kincir (poros sepanjang X, pusat di titik asal). Radius 2.1 m, lebar 1.1 m.</summary>
    public static void MillWheel(GeoBag b)
    {
        const float R = 2.1f, hw = 0.5f;
        int seg = 16;
        for (int sx = -1; sx <= 1; sx += 2)
        {
            float x = sx * hw;
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * 2f * Mathf.PI / seg, a1 = (i + 1) * 2f * Mathf.PI / seg;
                Vector3 p0 = new Vector3(x, Mathf.Cos(a0) * R, Mathf.Sin(a0) * R), p1 = new Vector3(x, Mathf.Cos(a1) * R, Mathf.Sin(a1) * R);
                b.Beam(Sl.Wood, p0, p1, 0.2f, 0.12f, new Vector3(1f, 0f, 0f));
            }
            for (int k = 0; k < 8; k++)
            {
                float a = k * 2f * Mathf.PI / 8f + 0.2f;
                b.Beam(Sl.Wood, new Vector3(x, 0f, 0f), new Vector3(x, Mathf.Cos(a) * R, Mathf.Sin(a) * R), 0.16f, 0.1f, new Vector3(1f, 0f, 0f));
            }
        }
        for (int i = 0; i < seg; i++)
        {
            float a = (i + 0.5f) * 2f * Mathf.PI / seg;
            Vector3 r = new Vector3(0f, Mathf.Cos(a), Mathf.Sin(a)), t = new Vector3(0f, -Mathf.Sin(a), Mathf.Cos(a));
            b.Beam(Sl.WoodLight, r * (R + 0.18f), r * (R - 0.42f), hw * 2f + 0.1f, 0.07f, t);
        }
        Prism(b, Sl.Wood, new Vector3(-0.8f, 0f, 0f), new Vector3(0.8f, 0f, 0f), 0.3f, 0.3f, 8, true, true);
        Prism(b, Sl.Metal, new Vector3(-1.4f, 0f, 0f), new Vector3(1.4f, 0f, 0f), 0.11f, 0.11f, 6, true, true);
        for (int sx = -1; sx <= 1; sx += 2) Prism(b, Sl.Metal, new Vector3(sx * 0.62f, 0f, 0f), new Vector3(sx * 0.72f, 0f, 0f), 0.34f, 0.34f, 8, true, true);
    }

    // ------------------------------------------------------------------ jembatan batu lengkung (jalur sepanjang +Z; asal di tengah bentang; y = 0 di tinggi tepi dek)
    /// <summary>L panjang total, W lebar luar, hump tinggi punggung dek di tengah, springRel tinggi permukaan air relatif (kaki lengkung), chord lebar bukaan lengkung.</summary>
    public static void StoneBridge(GeoBag b, float L, float W, float hump, float springRel, float chord)
    {
        b.Push(Vector3.zero, 270f);                       // di dalam: jalur sepanjang X, lebar sepanjang Z
        float hw = W * 0.5f, inner = hw - 0.5f, para = 0.95f, yBot = springRel - 4f;
        float yc = hump - 0.9f, ha = Mathf.Max(0.8f, yc - springRel);
        float A = chord, r = (A * A * 0.25f + ha * ha) / (2f * ha), cy = yc - r;
        float ys = cy + Mathf.Sqrt(Mathf.Max(0.01f, r * r - A * A * 0.25f));
        var us = new List<float>();
        int N = 22;
        for (int i = 0; i <= N; i++) us.Add(-L * 0.5f + L * i / N);
        us.Add(-A * 0.5f); us.Add(A * 0.5f);
        us.Sort();
        for (int i = 0; i + 1 < us.Count; i++)
        {
            float u0 = us[i], u1 = us[i + 1];
            if (u1 - u0 < 0.02f) continue;
            float k0 = 2f * u0 / L, k1 = 2f * u1 / L;
            float d0 = hump * (1f - k0 * k0), d1 = hump * (1f - k1 * k1);
            float um = (u0 + u1) * 0.5f;
            bool inArch = Mathf.Abs(um) < A * 0.5f;
            float a0 = inArch ? cy + Mathf.Sqrt(Mathf.Max(0.01f, r * r - u0 * u0)) : yBot, a1 = inArch ? cy + Mathf.Sqrt(Mathf.Max(0.01f, r * r - u1 * u1)) : yBot;
            // permukaan jalan batu bulat
            QUp(b, Sl.Cobble, new Vector3(u0, d0, -inner), new Vector3(u1, d1, -inner), new Vector3(u1, d1, inner), new Vector3(u0, d0, inner));
            for (int sg = -1; sg <= 1; sg += 2)
            {
                float vo = sg * hw, vi = sg * inner;
                // dinding spandrel (muka luar) di bawah dek
                QOut(b, Sl.Stone, new Vector3(u0, a0, vo), new Vector3(u1, a1, vo), new Vector3(u1, d1, vo), new Vector3(u0, d0, vo), new Vector3(0f, 0f, sg));
                // pagar pembatas
                QOut(b, Sl.Stone, new Vector3(u0, d0, vo), new Vector3(u1, d1, vo), new Vector3(u1, d1 + para, vo), new Vector3(u0, d0 + para, vo), new Vector3(0f, 0f, sg));
                QOut(b, Sl.Stone, new Vector3(u0, d0, vi), new Vector3(u1, d1, vi), new Vector3(u1, d1 + para, vi), new Vector3(u0, d0 + para, vi), new Vector3(0f, 0f, -sg));
                b.Beam(Sl.DarkStone, new Vector3(u0, d0 + para + 0.06f, (vo + vi) * 0.5f), new Vector3(u1, d1 + para + 0.06f, (vo + vi) * 0.5f), (hw - inner) + 0.18f, 0.14f, new Vector3(0f, 1f, 0f));
            }
            if (inArch) QOut(b, Sl.DarkStone, new Vector3(u0, a0, -hw), new Vector3(u1, a1, -hw), new Vector3(u1, a1, hw), new Vector3(u0, a0, hw), new Vector3(0f, -1f, 0f));
        }
        // dinding pangkal di bawah kaki lengkung + tutup ujung
        for (int sd = -1; sd <= 1; sd += 2)
        {
            float u = sd * A * 0.5f;
            QOut(b, Sl.DarkStone, new Vector3(u, yBot, -hw), new Vector3(u, yBot, hw), new Vector3(u, ys, hw), new Vector3(u, ys, -hw), new Vector3(-sd, 0f, 0f));
            float ue = sd * L * 0.5f;
            QOut(b, Sl.Stone, new Vector3(ue, yBot, -hw), new Vector3(ue, yBot, hw), new Vector3(ue, 0f, hw), new Vector3(ue, 0f, -hw), new Vector3(sd, 0f, 0f));
            // pilar ujung pagar (newel)
            for (int sg = -1; sg <= 1; sg += 2)
            {
                b.Box(Sl.DarkStone, new Vector3(ue - sd * 0.35f, para * 0.5f + 0.1f, sg * (hw - 0.25f)), new Vector3(0.7f, para + 0.2f, 0.75f));
                b.Box(Sl.Stone, new Vector3(ue - sd * 0.35f, para + 0.28f, sg * (hw - 0.25f)), new Vector3(0.84f, 0.16f, 0.88f));
            }
        }
        // cincin batu pasak di kedua muka lengkung + batu kunci
        float a0d = 90f - Mathf.Asin(Mathf.Clamp01(A * 0.5f / r)) * Mathf.Rad2Deg, a1d = 90f + Mathf.Asin(Mathf.Clamp01(A * 0.5f / r)) * Mathf.Rad2Deg;
        for (int sg = -1; sg <= 1; sg += 2)
        {
            float zA = sg > 0 ? hw - 0.04f : -hw - 0.14f, zB = sg > 0 ? hw + 0.14f : -hw + 0.04f;
            ArchRing(b, Sl.DarkStone, 0f, cy, r, r + 0.5f, a0d, a1d, 14, zA, zB);
            b.Box(Sl.DarkStone, new Vector3(0f, yc + 0.28f, sg * (hw + 0.06f)), new Vector3(0.7f, 0.86f, 0.22f));
        }
        b.Pop();
    }

    // ------------------------------------------------------------------ titian kayu (rata, jalur sepanjang +Z, y = 0 permukaan papan)
    public static void PlankBridge(GeoBag b, float L, float W, float springRel)
    {
        b.Push(Vector3.zero, 270f);
        float hw = W * 0.5f, yBot = springRel - 2.5f;
        int np = Mathf.Max(6, (int)(L / 0.31f));
        for (int i = 0; i < np; i++)
        {
            float u = -L * 0.5f + (i + 0.5f) * L / np;
            float jx = (Rnd(i, 3) - 0.5f) * 0.05f;
            b.Box(i % 3 == 0 ? Sl.Wood : Sl.WoodLight, new Vector3(u + jx, -0.05f, 0f), new Vector3(L / np - 0.03f, 0.1f, W - 0.1f + (Rnd(i, 9) - 0.5f) * 0.12f), (Rnd(i, 5) - 0.5f) * 2.4f);
        }
        for (int sg = -1; sg <= 1; sg += 2)
        {
            b.Beam(Sl.Wood, new Vector3(-L * 0.5f, -0.3f, sg * (hw - 0.3f)), new Vector3(L * 0.5f, -0.3f, sg * (hw - 0.3f)), 0.2f, 0.3f, new Vector3(0f, 1f, 0f));
            // tiang pagar & tali
            int nposts = Mathf.Max(3, (int)(L / 2.4f) + 1);
            for (int k = 0; k < nposts; k++)
            {
                float u = -L * 0.5f + 0.3f + k * (L - 0.6f) / (nposts - 1);
                Post(b, Sl.Wood, new Vector3(u, -0.3f, sg * (hw - 0.08f)), 1.45f, 0.07f);
            }
            b.Beam(Sl.WoodLight, new Vector3(-L * 0.5f + 0.3f, 1.08f, sg * (hw - 0.08f)), new Vector3(L * 0.5f - 0.3f, 1.08f, sg * (hw - 0.08f)), 0.09f, 0.09f, new Vector3(0f, 1f, 0f));
            b.Beam(Sl.WoodLight, new Vector3(-L * 0.5f + 0.3f, 0.55f, sg * (hw - 0.08f)), new Vector3(L * 0.5f - 0.3f, 0.55f, sg * (hw - 0.08f)), 0.06f, 0.06f, new Vector3(0f, 1f, 0f));
        }
        // kuda-kuda penopang tiap ~5 m
        int ns = Mathf.Max(2, (int)(L / 5f));
        for (int k = 1; k < ns; k++)
        {
            float u = -L * 0.5f + k * L / ns;
            for (int sg = -1; sg <= 1; sg += 2) b.Cyl(Sl.Wood, new Vector3(u, yBot, sg * (hw - 0.35f)), 0.17f, -yBot - 0.35f, 6, 0.15f, true);
            b.Beam(Sl.Wood, new Vector3(u, -0.5f, -(hw - 0.35f)), new Vector3(u, -0.5f, hw - 0.35f), 0.16f, 0.16f, new Vector3(0f, 1f, 0f));
        }
        for (int sd = -1; sd <= 1; sd += 2)
        {
            b.Box(Sl.Stone, new Vector3(sd * (L * 0.5f + 0.2f), -0.2f, 0f), new Vector3(0.8f, 0.4f, W + 0.4f));
            for (int sg = -1; sg <= 1; sg += 2) { Post(b, Sl.Wood, new Vector3(sd * (L * 0.5f - 0.1f), -0.3f, sg * (hw - 0.08f)), 1.7f, 0.1f); b.Ball(Sl.Wood, new Vector3(sd * (L * 0.5f - 0.1f), 1.42f, sg * (hw - 0.08f)), 0.12f, 3, 6); }
        }
        b.Pop();
    }

    // ------------------------------------------------------------------ gerbang kayu beratap (muka +Z = sisi luar). Papan nama 7,8 x 1,5 m tepat di tengah (z = 0), sisi baca +/-Z.
    public static void LychGate(GeoBag b, bool lantern)
    {
        const float px = 4.15f, pz = 1.45f, ph = 4.2f;
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                b.Box(Sl.Stone, new Vector3(sx * px, 0.25f, sz * pz), new Vector3(1.1f, 0.5f, 1.1f));
                b.Box(Sl.DarkStone, new Vector3(sx * px, 0.53f, sz * pz), new Vector3(0.9f, 0.08f, 0.9f));
                b.Box(Sl.Wood, new Vector3(sx * px, 0.5f + ph * 0.5f, sz * pz), new Vector3(0.5f, ph, 0.5f));
                b.Beam(Sl.Wood, new Vector3(sx * px, 0.5f + ph * 0.55f, sz * pz), new Vector3(sx * (px - 0.9f), 0.5f + ph - 0.05f, sz * pz), 0.28f, 0.2f, new Vector3(0f, 0f, 1f));
            }
        for (int sz = -1; sz <= 1; sz += 1)
            b.Box(Sl.Wood, new Vector3(0f, 0.5f + ph - 0.1f, sz * pz), new Vector3(2f * px + 0.5f, 0.38f, 0.42f));
        for (int sx = -1; sx <= 1; sx += 2)
            b.Box(Sl.Wood, new Vector3(sx * px, 0.5f + ph - 0.1f, 0f), new Vector3(0.42f, 0.38f, 2f * pz + 0.5f));
        // papan nama (kedua sisi terbaca)
        b.Box(Sl.Wood, new Vector3(0f, 0.5f + ph - 1.0f, 0f), new Vector3(7.8f, 1.5f, 0.26f));
        b.Box(Sl.WoodLight, new Vector3(0f, 0.5f + ph - 1.0f, 0f), new Vector3(7.4f, 1.1f, 0.3f));
        b.Box(Sl.Gold, new Vector3(0f, 0.5f + ph - 0.22f, 0f), new Vector3(7.8f, 0.05f, 0.32f));
        for (int k = -1; k <= 1; k += 2) b.Beam(Sl.Metal, new Vector3(k * 3.2f, 0.5f + ph - 0.1f, 0f), new Vector3(k * 3.2f, 0.5f + ph - 0.3f, 0f), 0.05f, 0.05f);
        // atap sirap, punggung sepanjang Z
        b.Push(new Vector3(0f, 0.5f + ph + 0.1f, 0f), 90f);
        ShingleRoof(b, Sl.Roof, Sl.RoofB, Sl.Wood, Sl.PlasterB, Vector3.zero, 2f * pz + 0.6f, 2f * px + 0.8f, 2.3f, 0.7f, 0.55f, true, 5);
        b.Pop();
        if (lantern)
        {
            b.Beam(Sl.Metal, new Vector3(0f, 0.5f + ph - 0.25f, 0f), new Vector3(0f, 0.5f + ph - 0.5f, 0f), 0.04f, 0.04f);
        }
    }

    public static void P_Chapel(GeoBag b) { Chapel(b); }
    public static void P_Barn(GeoBag b) { Barn(b); }
    public static void P_Mill(GeoBag b) { Mill(b); b.Push(new Vector3(5.2f, 2.2f, 0f), 0f); MillWheel(b); b.Pop(); }
    public static void P_Bridge(GeoBag b) { StoneBridge(b, 20f, 5.2f, 1.7f, -2.0f, 11.5f); }
    public static void P_Plank(GeoBag b) { PlankBridge(b, 14f, 2.0f, -1.9f); }
    public static void P_Gate(GeoBag b) { LychGate(b, true); }
}
