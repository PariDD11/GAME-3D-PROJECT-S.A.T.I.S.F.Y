using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerbang kota Ashwatch baru (geometri murni, bisa dipratinjau): kepala lengkung setengah lingkaran, dua menara bulat berkorbel + atap kerucut,
/// ruang jaga setengah kayu di atas lengkung, benteng, spanduk, lentera, papan nama, daun pintu berpita besi dan terali.
/// Bingkai lokal: +Z = arah keluar dari pusat kota, titik asal di tengah gerbang pada garis tembok, alas tanah y = 0.
/// n81: tier 0 = Tembok Luar (batu terang), 1 = Tembok Tengah (batu abu, genteng merah), 2 = Tembok Dalam + tembok pembatas kastil (batu gelap, menara beratap emas).
/// Daun pintu = pelat batu kelabu berukir sisik (kanon Gerbang Selatan); gerbang tengah & dalam dijaga sepasang patung batu bermata kosong di sisi dalam.
/// </summary>
public static partial class T2Gate
{
    public class Spec
    {
        public int tier;
        public float W, Hr, D, towerR, towerH, capH, Hb, chamberH, tX;
        public float Hd { get { return Hr + W * 0.5f; } }
        public float WallThick, WallH;
    }

    public static Spec SpecFor(int tier)
    {
        var s = new Spec { tier = tier };
        // n81: gerbang menjulang melampaui tembok masing-masing (luar 14 m, tengah 17,5 m, dalam 21 m, pembatas kastil 16 m)
        switch (tier)
        {
            case 0: s.W = 10.2f; s.Hr = 5.4f; s.D = 9.0f; s.towerR = 5.8f; s.towerH = 27f; s.capH = 11f; s.Hb = 18.5f; s.chamberH = 5.2f; s.WallThick = 7.4f; s.WallH = 14f; break;
            case 1: s.W = 9.0f; s.Hr = 4.8f; s.D = 8.2f; s.towerR = 5.2f; s.towerH = 29f; s.capH = 10.5f; s.Hb = 21.5f; s.chamberH = 4.6f; s.WallThick = 6.6f; s.WallH = 17.5f; break;
            default: s.W = 9.0f; s.Hr = 4.8f; s.D = 8.2f; s.towerR = 5.0f; s.towerH = 34f; s.capH = 13f; s.Hb = 24.5f; s.chamberH = 4.8f; s.WallThick = 6.4f; s.WallH = 21f; break;
        }
        s.tX = s.W * 0.5f + s.towerR + 1.0f;
        return s;
    }

    static void WallLamp(GeoBag b, float x, float y, float z)
    {
        b.Beam(Sl.Metal, new Vector3(x, y + 0.4f, z - 0.45f), new Vector3(x, y + 0.4f, z), 0.08f, 0.08f);
        b.Box(Sl.Metal, new Vector3(x, y, z), new Vector3(0.34f, 0.6f, 0.34f));
        b.Box(Sl.Glow, new Vector3(x, y, z), new Vector3(0.24f, 0.46f, 0.24f));
        b.Box(Sl.Metal, new Vector3(x, y + 0.34f, z), new Vector3(0.42f, 0.1f, 0.42f));
    }

    // ------------------------------------------------------------------ badan gerbang tetap
    public static void Model(GeoBag b, int tier)
    {
        var sp = SpecFor(tier);
        float W = sp.W, hw = W * 0.5f, D = sp.D, hd = D * 0.5f, Hr = sp.Hr, Hd = sp.Hd, Hb = sp.Hb, tX = sp.tX, tR = sp.towerR;
        int trim = tier == 0 ? Sl.DarkStone : Sl.Marble;
        float pierOut = tX + tR * 0.55f;
        // pilar batu kiri & kanan (pangkal menara menyatu)
        foreach (float sx in new[] { -1f, 1f })
        {
            float xa = sx * hw, xb = sx * pierOut;
            T2Kit.StoneBlock(b, Mathf.Min(xa, xb), Mathf.Max(xa, xb), -hd, hd, 0f, Hb, Sl.Stone);
        }
        b.Box(Sl.Pave, new Vector3(0f, 0.06f, 0f), new Vector3(W, 0.12f, D + 2f));
        // kepala lengkung
        const int N = 18;
        var ax = new float[N + 1]; var ay = new float[N + 1];
        for (int k = 0; k <= N; k++) { float th = Mathf.PI - Mathf.PI * k / N; ax[k] = hw * Mathf.Cos(th); ay[k] = Hr + hw * Mathf.Sin(th); }
        for (int k = 0; k < N; k++)
        {
            b.Quad(Sl.Stone, new Vector3(ax[k], ay[k], hd), new Vector3(ax[k + 1], ay[k + 1], hd), new Vector3(ax[k + 1], Hb, hd), new Vector3(ax[k], Hb, hd));
            b.Quad(Sl.Stone, new Vector3(ax[k + 1], ay[k + 1], -hd), new Vector3(ax[k], ay[k], -hd), new Vector3(ax[k], Hb, -hd), new Vector3(ax[k + 1], Hb, -hd));
            b.Quad(Sl.DarkStone, new Vector3(ax[k + 1], ay[k + 1], hd), new Vector3(ax[k], ay[k], hd), new Vector3(ax[k], ay[k], -hd), new Vector3(ax[k + 1], ay[k + 1], -hd));
        }
        foreach (float sx in new[] { -1f, 1f })
            V2Kit.QOut(b, Sl.Stone, new Vector3(sx * hw, 0f, -hd), new Vector3(sx * hw, 0f, hd), new Vector3(sx * hw, Hr, hd), new Vector3(sx * hw, Hr, -hd), new Vector3(-sx, 0f, 0f));
        b.Quad(Sl.Stone, new Vector3(-hw, Hb, hd), new Vector3(hw, Hb, hd), new Vector3(hw, Hb, -hd), new Vector3(-hw, Hb, -hd));
        // cincin batu lengkung + batu kunci
        foreach (float zs in new[] { 1f, -1f })
        {
            float z = zs * (hd + 0.14f);
            float ro = hw + 1.05f;
            for (int k = 0; k < N; k++)
            {
                float th0 = Mathf.PI - Mathf.PI * k / N, th1 = Mathf.PI - Mathf.PI * (k + 1) / N;
                Vector3 pi0 = new Vector3(Mathf.Cos(th0) * (hw + 0.05f), Hr + Mathf.Sin(th0) * (hw + 0.05f), z);
                Vector3 po0 = new Vector3(Mathf.Cos(th0) * ro, Hr + Mathf.Sin(th0) * ro, z);
                Vector3 pi1 = new Vector3(Mathf.Cos(th1) * (hw + 0.05f), Hr + Mathf.Sin(th1) * (hw + 0.05f), z);
                Vector3 po1 = new Vector3(Mathf.Cos(th1) * ro, Hr + Mathf.Sin(th1) * ro, z);
                b.Beam(k % 2 == 0 ? trim : Sl.Stone, (pi0 + pi1) * 0.5f, (po0 + po1) * 0.5f, (pi1 - pi0).magnitude * 0.92f, 0.34f, Vector3.forward);
            }
            b.Box(Sl.Gold, new Vector3(0f, Hr + ro - 0.2f, z + zs * 0.06f), new Vector3(1.0f, 1.3f, 0.5f));
        }
        // pengisi setengah lingkaran di atas daun pintu (tympanum), lambang
        float tz = hd - 1.4f;
        Vector3 tc = new Vector3(0f, Hr, tz);
        for (int k = 0; k < N; k++)
        {
            float th0 = Mathf.PI - Mathf.PI * k / N, th1 = Mathf.PI - Mathf.PI * (k + 1) / N;
            Vector3 p0 = tc + new Vector3(Mathf.Cos(th0) * hw, Mathf.Sin(th0) * hw, 0f), p1 = tc + new Vector3(Mathf.Cos(th1) * hw, Mathf.Sin(th1) * hw, 0f);
            b.Tri(Sl.WoodLight, tc, p1, p0);
            b.Tri(Sl.WoodLight, tc, p0, p1);
        }
        b.Box(Sl.Metal, tc + new Vector3(0f, hw * 0.45f, 0.08f), new Vector3(0.15f, hw * 0.9f, 0.1f));
        b.Ball(Sl.Gold, tc + new Vector3(0f, hw * 0.55f, 0.16f), 0.9f, 4, 8, 1.1f);
        b.Ball(Sl.Cloth, tc + new Vector3(0f, hw * 0.55f, 0.2f), 0.62f, 3, 8, 1.1f);
        // garis hias + benteng depan/belakang
        foreach (float zs in new[] { 1f, -1f })
        {
            b.Box(trim, new Vector3(0f, Hb - 1.2f, zs * (hd + 0.1f)), new Vector3(tX * 2f - 2f, 0.55f, 0.28f));
            b.Box(tier == 2 ? Sl.Gold : Sl.DarkStone, new Vector3(0f, Hb - 1.55f, zs * (hd + 0.2f)), new Vector3(tX * 2f - 2f, 0.16f, 0.14f));
            b.Crenellate(Sl.Stone, new Vector3(-tX + tR * 0.6f, Hb, zs * (hd - 0.35f)), new Vector3(tX - tR * 0.6f, Hb, zs * (hd - 0.35f)), 1.5f, 0.8f, 1.9f, 1.3f);
        }
        // menara bulat
        foreach (float sx in new[] { -1f, 1f })
            T2Kit.RoundTower(b, sx * tX, 0f, tR, 0f, sp.towerH, sp.capH, Sl.Stone, Sl.RoofB, 5 + (int)sx + tier * 3, 7);
        // ruang jaga setengah kayu di atas lengkung
        float cw = 2f * (tX - tR) - 1.0f, chD = D - 2.2f, ch = sp.chamberH;
        b.Box(Sl.Plaster, new Vector3(0f, Hb + ch * 0.5f, 0f), new Vector3(cw, ch, chD));
        var ts = new T2Kit.TSpec { shutters = true };
        foreach (float zs in new[] { 1f, -1f })
        {
            b.Push(new Vector3(0f, 0f, zs * chD * 0.5f), zs > 0f ? 0f : 180f);
            T2Kit.FramedWall(b, ts, cw, Hb, ch, 11 + tier);
            b.Pop();
        }
        T2Kit.TileRoof(b, Sl.RoofB, Sl.DarkStone, Sl.Wood, Sl.Plaster, new Vector3(0f, Hb + ch, 0f), cw, chD, chD * 0.5f * 0.75f, 0.85f, 0.5f, 5 + tier, 0.45f, true);
        foreach (float zs in new[] { 1f, -1f })
            b.Ball(Sl.Gold, new Vector3(0f, Hb + ch + chD * 0.5f * 0.75f + 0.2f, 0f), 0.22f, 3, 6);
        // spanduk, lentera & papan nama di muka depan dan belakang
        foreach (float zs in new[] { 1f, -1f })
        {
            b.Push(new Vector3(0f, 0f, zs * (hd + 0.1f)), zs > 0f ? 0f : 180f);
            float bx = hw + 1.7f;
            T2Kit.Banner(b, new Vector3(-bx, Hd + 3.8f, 0.05f), 1.6f, 6.2f, Sl.Cloth, Sl.Gold);
            T2Kit.Banner(b, new Vector3(bx, Hd + 3.8f, 0.05f), 1.6f, 6.2f, Sl.Cloth, Sl.Gold);
            WallLamp(b, -hw - 0.35f, 5.4f, 0.6f);
            WallLamp(b, hw + 0.35f, 5.4f, 0.6f);
            float signY = Hd + 2.5f;
            b.Box(Sl.Wood, new Vector3(0f, signY, 0.2f), new Vector3(W * 1.35f, 2.0f, 0.3f));
            b.Box(Sl.Gold, new Vector3(0f, signY + 1.05f, 0.22f), new Vector3(W * 1.35f + 0.3f, 0.14f, 0.3f));
            b.Box(Sl.Gold, new Vector3(0f, signY - 1.05f, 0.22f), new Vector3(W * 1.35f + 0.3f, 0.14f, 0.3f));
            b.Pop();
        }
        if (tier >= 1)
        {
            // sepasang patung batu bermata kosong di sisi dalam gerbang, menghadap keluar (ke peziarah yang datang); tidak menutup badan jalan
            foreach (float sx in new[] { -1f, 1f }) T2Kit.HollowStatue(b, new Vector3(sx * (hw + 3.0f), 0f, -(hd + 2.6f)), 0f);
        }
    }

    // ------------------------------------------------------------------ daun pintu (dir = +1 daun kiri: memanjang ke +X dari engsel; -1 daun kanan)
    /// <summary>Satu belah sisik terukir: belah ketupat tegak menghadap zs (+1 sisi luar, -1 sisi dalam).</summary>
    static void ScaleCarve(GeoBag b, int slot, float x, float y, float z, float hx, float hy, float zs)
    {
        Vector3 l = new Vector3(x - hx, y, z), t = new Vector3(x, y + hy, z), r = new Vector3(x + hx, y, z), d = new Vector3(x, y - hy, z);
        if (zs > 0f) b.Quad(slot, l, d, r, t); else b.Quad(slot, r, d, l, t);
    }

    /// <summary>Daun pintu batu kelabu berukir sisik (kanon): pelat tebal berbingkai batu, sisik bertingkat setengah-geser, dua pita besi, kenop emas.</summary>
    public static void Leaf(GeoBag b, int tier, float dir)
    {
        var sp = SpecFor(tier);
        float hw = sp.W * 0.5f, Hr = sp.Hr;
        float lw = hw - 0.1f, cx = dir * (lw * 0.5f + 0.05f), fw = 0.38f;
        b.Box(Sl.DarkStone, new Vector3(cx, Hr * 0.5f, 0f), new Vector3(lw, Hr, 0.46f));
        b.Box(Sl.Stone, new Vector3(cx, fw * 0.5f, 0f), new Vector3(lw + 0.04f, fw, 0.52f));
        b.Box(Sl.Stone, new Vector3(cx, Hr - fw * 0.5f, 0f), new Vector3(lw + 0.04f, fw, 0.52f));
        b.Box(Sl.Stone, new Vector3(dir * (0.05f + fw * 0.5f), Hr * 0.5f, 0f), new Vector3(fw, Hr, 0.52f));
        b.Box(Sl.Stone, new Vector3(dir * (lw + 0.05f - fw * 0.5f), Hr * 0.5f, 0f), new Vector3(fw, Hr, 0.52f));
        float sw = 0.66f, sh = 0.78f;
        float x0 = 0.05f + fw + sw * 0.5f, x1 = lw + 0.05f - fw - sw * 0.5f;
        float y0 = fw + sh * 0.5f, y1 = Hr - fw - sh * 0.5f;
        int cols = Mathf.Max(2, Mathf.FloorToInt((x1 - x0) / sw) + 1);
        int rows = Mathf.Max(2, Mathf.FloorToInt((y1 - y0) / (sh * 0.5f)) + 1);
        foreach (float zs in new[] { 1f, -1f })
        {
            float z = zs * 0.245f;
            for (int r = 0; r < rows; r++)
            {
                float y = y0 + r * sh * 0.5f, off = (r % 2 == 0) ? 0f : sw * 0.5f;
                for (int c = 0; c < cols; c++)
                {
                    float x = x0 + off + c * sw;
                    if (x > x1 + 0.01f) continue;
                    ScaleCarve(b, Sl.Stone, dir * x, y, z, sw * 0.46f, sh * 0.46f, zs);
                }
            }
            foreach (float yy in new[] { 1.25f, Hr - 1.25f })
            {
                b.Box(Sl.Metal, new Vector3(cx, yy, zs * 0.27f), new Vector3(lw - 0.2f, 0.26f, 0.07f));
                for (int i = 0; i < 5; i++) b.Ball(Sl.Gold, new Vector3(dir * (0.7f + i * (lw - 1.4f) / 4f), yy, zs * 0.31f), 0.09f, 3, 5);
            }
            b.Ball(Sl.Gold, new Vector3(dir * (lw - 0.95f), Hr * 0.45f, zs * 0.3f), 0.23f, 3, 8);
        }
    }

    // ------------------------------------------------------------------ terali besi (naik-turun)
    public static void Portcullis(GeoBag b, int tier)
    {
        var sp = SpecFor(tier);
        float W = sp.W, Hd = sp.Hd, Hr = sp.Hr, hw = W * 0.5f;
        int n = Mathf.FloorToInt((W - 0.6f) / 0.7f);
        float x0 = -(n - 1) * 0.7f * 0.5f;
        for (int i = 0; i < n; i++)
        {
            float x = x0 + i * 0.7f;
            float top = Hr + Mathf.Sqrt(Mathf.Max(0.01f, hw * hw - x * x)) - 0.3f;
            b.Box(Sl.Metal, new Vector3(x, top * 0.5f + 0.4f, 0f), new Vector3(0.18f, top - 0.2f, 0.18f));
            b.Cyl(Sl.Metal, new Vector3(x, 0f, 0f), 0f, 0.5f, 4, 0.13f, false);
        }
        foreach (float yy in new[] { 1.2f, 2.8f, 4.4f, Hr + 1.2f })
        {
            float xe = Mathf.Min(hw - 0.2f, Mathf.Sqrt(Mathf.Max(0.01f, hw * hw - (yy - Hr) * (yy - Hr) * (yy > Hr ? 1f : 0f))));
            b.Box(Sl.Metal, new Vector3(0f, yy, 0f), new Vector3(xe * 2f - 0.2f, 0.2f, 0.24f));
        }
    }
}
