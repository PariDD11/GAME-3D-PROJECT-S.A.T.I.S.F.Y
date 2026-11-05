using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rumah warga Kota Ashwatch: lantai bawah batu atau etalase toko, lantai atas rangka kayu + plester (menjorok ke jalan), atap genteng berundak.
/// 10 jenis dari denah (T2.Size): row_a row_b twn_a twn_b shop_a cot_a corner villa store workshop. Beban sengaja ringan (~1.2-2.6 rb simpul) karena ratusan rumah.
/// </summary>
public static partial class T2Kit
{
    public class TSpec
    {
        public float w = 8f, d = 9f, gh = 3f, fh = 2.7f, pitch = 0.8f;
        public int floors = 2;
        public int gnd;                 // 0 batu, 1 etalase toko, 2 plester berangka, 3 bukaan lebar (gudang/bengkel)
        public int upper;               // 0 rangka kayu + plester, 1 batu, 2 papan
        public int roof;                // 0 pelana sejajar jalan (punggung sepanjang X), 1 pelana menghadap jalan (punggung sepanjang Z), 2 limas
        public bool jetty, sideL = true, sideR = true, awning, sign, turret, balcony, planters, shutters = true, archDoor, hoist;
        public int dormers, chimney = -1, seed = 1;
        public float doorX;
        public float Height;
    }

    static int WinCount(float w) { return w < 6.6f ? 2 : (w < 10.2f ? 3 : 4); }

    // ------------------------------------------------------------------ satu panel dinding (bidang z = 0, muka +Z), lebar W, dari y0 setinggi H
    static void Panel(GeoBag b, TSpec s, int style, float W, float y0, float H, bool front, int floorIdx, int seed, bool exposed)
    {
        int slotWall = style == 1 ? Sl.Stone : (style == 2 ? Sl.WoodLight : (floorIdx >= 2 ? Sl.PlasterB : Sl.Plaster));
        float x0 = -W * 0.5f, x1 = W * 0.5f;
        int n = front ? WinCount(W) : (exposed ? Mathf.Max(1, WinCount(W) - 1) : 0);
        float cell = W / Mathf.Max(1, n);
        bool ground = floorIdx == 0;
        b.Quad(slotWall, new Vector3(x0, y0, 0f), new Vector3(x1, y0, 0f), new Vector3(x1, y0 + H, 0f), new Vector3(x0, y0 + H, 0f));
        // rangka
        if (style == 0)
        {
            float ys = y0 + H * 0.31f, yh = y0 + H * 0.84f;
            Strip(b, Sl.Wood, x0 - 0.05f, x1 + 0.05f, y0, y0 + 0.16f, 0f, 0.09f);
            Strip(b, Sl.Wood, x0 - 0.05f, x1 + 0.05f, y0 + H - 0.2f, y0 + H, 0f, 0.1f);
            if (n >= 1)
            {
                Strip(b, Sl.Wood, x0, x1, ys - 0.06f, ys + 0.06f, 0f, 0.06f);
                Strip(b, Sl.Wood, x0, x1, yh - 0.06f, yh + 0.06f, 0f, 0.06f);
            }
            int np = n >= 1 ? n + 1 : Mathf.Max(2, Mathf.RoundToInt(W / 2.2f) + 1);
            for (int k = 0; k < np; k++)
            {
                float x = x0 + 0.07f + k * (W - 0.14f) / (np - 1);
                Strip(b, Sl.Wood, x - 0.07f, x + 0.07f, y0, y0 + H, 0f, 0.08f);
            }
            // sokong siku di sudut bidang
            float bl = Mathf.Min(0.75f, H * 0.28f);
            Brace(b, Sl.Wood, x0 + 0.14f, y0 + H - 0.22f - bl, x0 + 0.14f + bl, y0 + H - 0.22f, 0f, 0.1f, 0.07f);
            Brace(b, Sl.Wood, x1 - 0.14f, y0 + H - 0.22f - bl, x1 - 0.14f - bl, y0 + H - 0.22f, 0f, 0.1f, 0.07f);
            if (n >= 3 && W > 7f)
            {
                float xm = x0 + cell;
                Brace(b, Sl.Wood, xm + 0.1f, y0 + 0.2f, xm + cell * 0.5f - 0.1f, ys - 0.1f, 0f, 0.09f, 0.06f);
            }
        }
        else if (style == 1)
        {
            // batu: pilaster sudut + lis atas
            Strip(b, Sl.DarkStone, x0, x0 + 0.34f, y0, y0 + H, 0f, 0.07f);
            Strip(b, Sl.DarkStone, x1 - 0.34f, x1, y0, y0 + H, 0f, 0.07f);
            Strip(b, Sl.DarkStone, x0, x1, y0 + H - 0.16f, y0 + H, 0f, 0.1f);
            if (!ground) Strip(b, Sl.DarkStone, x0, x1, y0, y0 + 0.14f, 0f, 0.09f);
        }
        else if (style == 2)
        {
            Strip(b, Sl.Wood, x0, x1, y0, y0 + 0.14f, 0f, 0.07f);
            Strip(b, Sl.Wood, x0, x1, y0 + H - 0.14f, y0 + H, 0f, 0.07f);
            int nb = Mathf.Max(4, Mathf.RoundToInt(W / 0.62f));
            for (int k = 0; k <= nb; k++) Strip(b, Sl.Wood, x0 + k * W / nb - 0.03f, x0 + k * W / nb + 0.03f, y0 + 0.14f, y0 + H - 0.14f, 0f, 0.03f);
        }
        // bukaan
        float wy = y0 + H * (ground ? 0.55f : 0.56f), wh = Mathf.Min(1.2f, H * 0.4f), ww = Mathf.Min(0.95f, cell * 0.5f);
        int wst = (s.shutters && !ground ? 1 : (s.shutters && ground ? 1 : 0)) | (style == 1 || ground ? 2 : 0) | (s.planters && front && !ground && floorIdx == 1 ? 4 : 0);
        for (int k = 0; k < n; k++)
        {
            float cx = x0 + (k + 0.5f) * cell;
            if (front && ground)
            {
                if (s.gnd == 1)
                {
                    // etalase toko: dilewati di sini, digambar terpisah
                    continue;
                }
                if (Mathf.Abs(cx - s.doorX) < cell * 0.45f) continue;
            }
            Win(b, cx, wy, ww, wh, seed + k, wst);
        }
        if (front && ground)
        {
            if (s.gnd == 1) ShopFront(b, s, W, y0, H, seed);
            else if (s.gnd == 3) { DoorT(b, s.doorX, 2.3f, 2.5f, seed, 1, true); }
            else DoorT(b, s.doorX, 1.15f, 2.15f, seed, 0, true);
        }
    }

    /// <summary>Etalase toko: dua pilar batu, kusen kayu dengan kaca berpetak, dinding rendah, papan nama, pintu di sisi doorX.</summary>
    static void ShopFront(GeoBag b, TSpec s, float W, float y0, float H, int seed)
    {
        float x0 = -W * 0.5f, x1 = W * 0.5f;
        float dx = s.doorX;
        float doorW = 1.2f;
        float cl = x0 + 0.45f, cr = x1 - 0.45f;
        // pilar batu di ujung & sebelah pintu
        b.Box(Sl.DarkStone, new Vector3(x0 + 0.3f, y0 + H * 0.5f, 0.12f), new Vector3(0.6f, H, 0.28f));
        b.Box(Sl.DarkStone, new Vector3(x1 - 0.3f, y0 + H * 0.5f, 0.12f), new Vector3(0.6f, H, 0.28f));
        // pintu
        DoorT(b, dx, doorW, 2.2f, seed, 0, false);
        // etalase di kedua sisi pintu (yang lebih sempit dari 1 m dilewati)
        float gh = H - 1.05f - 0.55f;
        float gy0 = y0 + 0.75f, gy1 = gy0 + gh;
        for (int q = 0; q < 2; q++)
        {
            float a0 = q == 0 ? cl : dx + doorW * 0.5f + 0.35f;
            float a1 = q == 0 ? dx - doorW * 0.5f - 0.35f : cr;
            if (a1 - a0 < 1.0f) continue;
            b.Quad(Sl.Glass, new Vector3(a0, gy0, 0.03f), new Vector3(a1, gy0, 0.03f), new Vector3(a1, gy1, 0.03f), new Vector3(a0, gy1, 0.03f));
            Strip(b, Sl.Wood, a0 - 0.08f, a0, gy0 - 0.08f, gy1 + 0.1f, 0f, 0.1f);
            Strip(b, Sl.Wood, a1, a1 + 0.08f, gy0 - 0.08f, gy1 + 0.1f, 0f, 0.1f);
            Strip(b, Sl.Wood, a0, a1, gy1, gy1 + 0.1f, 0f, 0.1f);
            Strip(b, Sl.Wood, a0 - 0.08f, a1 + 0.08f, gy0 - 0.12f, gy0, 0f, 0.14f);
            int nm = Mathf.Max(1, Mathf.RoundToInt((a1 - a0) / 1.0f));
            for (int k = 1; k < nm; k++) Strip(b, Sl.Wood, a0 + k * (a1 - a0) / nm - 0.025f, a0 + k * (a1 - a0) / nm + 0.025f, gy0, gy1, 0f, 0.06f);
            Strip(b, Sl.Wood, a0, a1, gy0 + (gy1 - gy0) * 0.7f - 0.025f, gy0 + (gy1 - gy0) * 0.7f + 0.025f, 0f, 0.06f);
            b.Box(Sl.Stone, new Vector3((a0 + a1) * 0.5f, y0 + 0.36f, 0.06f), new Vector3(a1 - a0 + 0.2f, 0.72f, 0.14f));
            for (int k = 0; k < nm; k++)
                b.Box(k % 2 == 0 ? Sl.ClothB : Sl.Gold, new Vector3(a0 + (k + 0.5f) * (a1 - a0) / nm, gy0 + 0.2f, -0.05f), new Vector3(0.34f, 0.26f, 0.2f));
        }
        // papan nama di atas etalase
        float bw = W - 1.3f;
        b.Box(Sl.Wood, new Vector3(0f, y0 + H - 0.28f, 0.1f), new Vector3(bw, 0.5f, 0.12f));
        b.Box(Sl.WoodLight, new Vector3(0f, y0 + H - 0.28f, 0.17f), new Vector3(bw - 0.2f, 0.34f, 0.02f));
        if (s.awning) Awning(b, 0f, W - 0.9f, y0 + H - 0.5f, 0.55f, 1.35f, Sl.Cloth, Sl.ClothB);
    }

    // ------------------------------------------------------------------ menara sudut kecil (oriel) dengan atap kerucut
    static void Turret(GeoBag b, float cx, float cz, float yBase, float yTop, float r, int seed)
    {
        b.Cyl(Sl.DarkStone, new Vector3(cx, yBase - 1.15f, cz), r * 0.4f, 1.15f, 8, r * 1.02f, false, false);
        b.Cyl(Sl.PlasterB, new Vector3(cx, yBase, cz), r, yTop - yBase, 8, r * 0.98f, false, false);
        for (int k = 0; k < 8; k++)
        {
            float a = k * Mathf.PI * 2f / 8f;
            b.Box(Sl.Wood, new Vector3(cx + Mathf.Sin(a) * r * 1.0f, (yBase + yTop) * 0.5f, cz + Mathf.Cos(a) * r * 1.0f), new Vector3(0.11f, yTop - yBase, 0.11f), a * Mathf.Rad2Deg);
        }
        int nf = Mathf.Max(1, Mathf.RoundToInt((yTop - yBase) / 2.7f));
        for (int f = 0; f < nf; f++)
        {
            float yy = yBase + (f + 0.52f) * (yTop - yBase) / nf;
            for (int k = -1; k <= 1; k++)
            {
                float a = (k * 0.62f) * 1f;
                float ca = Mathf.Sin(a), sa = Mathf.Cos(a);
                b.Push(new Vector3(cx + ca * r, yy, cz + sa * r), a * Mathf.Rad2Deg);
                Win(b, 0f, 0f, 0.5f, 0.85f, seed + f * 3 + k, 8 | 1 | 2);
                b.Pop();
            }
        }
        b.Cyl(Sl.Wood, new Vector3(cx, yTop - 0.05f, cz), r + 0.18f, 0.18f, 8, r + 0.08f, true, false);
        b.Cyl(Sl.RoofB, new Vector3(cx, yTop + 0.1f, cz), r + 0.5f, r * 2.1f, 8, 0.05f, false, false);
        b.Ball(Sl.Gold, new Vector3(cx, yTop + 0.1f + r * 2.1f + 0.05f, cz), 0.12f, 3, 6);
    }

    // ------------------------------------------------------------------ balkon kayu di muka depan
    static void Balcony(GeoBag b, float cx, float yFloor, float zWall, float w, float depth)
    {
        b.Box(Sl.Wood, new Vector3(cx, yFloor - 0.07f, zWall + depth * 0.5f), new Vector3(w, 0.14f, depth));
        for (int k = 0; k < 3; k++)
            b.Beam(Sl.Wood, new Vector3(cx - w * 0.5f + 0.2f + k * (w - 0.4f) / 2f, yFloor - 0.14f, zWall), new Vector3(cx - w * 0.5f + 0.2f + k * (w - 0.4f) / 2f, yFloor - 0.7f, zWall + depth - 0.1f), 0.1f, 0.1f);
        float rh = 0.95f;
        b.Box(Sl.Wood, new Vector3(cx, yFloor + rh, zWall + depth - 0.05f), new Vector3(w, 0.08f, 0.1f));
        b.Box(Sl.Wood, new Vector3(cx - w * 0.5f + 0.05f, yFloor + rh * 0.5f, zWall + depth * 0.5f), new Vector3(0.1f, rh, depth));
        b.Box(Sl.Wood, new Vector3(cx + w * 0.5f - 0.05f, yFloor + rh * 0.5f, zWall + depth * 0.5f), new Vector3(0.1f, rh, depth));
        int nb = Mathf.Max(4, Mathf.RoundToInt(w / 0.22f));
        for (int k = 0; k <= nb; k++)
            b.Box(Sl.Wood, new Vector3(cx - w * 0.5f + 0.1f + k * (w - 0.2f) / nb, yFloor + rh * 0.5f, zWall + depth - 0.05f), new Vector3(0.05f, rh, 0.05f));
        b.Box(Sl.Wood, new Vector3(cx, yFloor + 0.2f, zWall + depth - 0.05f), new Vector3(w, 0.06f, 0.08f));
    }

    /// <summary>Dinding berangka kayu + plester berjendela pada bidang z = 0 (untuk ruang jaga gerbang, dsb.).</summary>
    public static void FramedWall(GeoBag b, TSpec s, float W, float y0, float H, int seed)
    {
        Panel(b, s, 0, W, y0, H, true, 1, seed, true);
    }

    // ------------------------------------------------------------------ rumah lengkap
    public static float TownHouse(GeoBag b, TSpec s)
    {
        float w = s.w, d = s.d;
        float jet = s.jetty && s.floors > 1 ? 0.55f : 0f;
        Plinth(b, w, d, 0f, BaseY);
        float y = BaseY;
        for (int f = 0; f < s.floors; f++)
        {
            float H = f == 0 ? s.gh : s.fh;
            float zF = d * 0.5f + (f > 0 ? jet : 0f), zB = -d * 0.5f;
            float zc = (zF + zB) * 0.5f, dd = zF - zB;
            int style = f == 0 ? (s.gnd == 0 || s.gnd == 1 || s.gnd == 3 ? 1 : 0) : (s.upper == 1 ? 1 : (s.upper == 2 ? 2 : 0));
            if (f > 0 && jet > 0f)
            {
                // balok penopang lantai menjorok
                int nk = Mathf.Max(3, Mathf.RoundToInt(w / 1.3f));
                for (int k = 0; k < nk; k++)
                {
                    float bx = -w * 0.5f + (k + 0.5f) * w / nk;
                    b.Beam(Sl.Wood, new Vector3(bx, y - 0.5f, d * 0.5f - 0.1f), new Vector3(bx, y - 0.1f, d * 0.5f + jet), 0.15f, 0.14f);
                }
                b.Box(Sl.Wood, new Vector3(0f, y - 0.08f, d * 0.5f + jet * 0.5f - 0.03f), new Vector3(w + 0.1f, 0.18f, jet + 0.16f));
            }
            else if (f > 0) Strip(b, Sl.Wood, -w * 0.5f - 0.06f, w * 0.5f + 0.06f, y - 0.1f, y + 0.1f, d * 0.5f - 0.02f, 0.14f);
            b.Push(new Vector3(0f, 0f, zF), 0f); Panel(b, s, style, w, y, H, true, f, s.seed + f * 7, true); b.Pop();
            b.Push(new Vector3(0f, 0f, zB), 180f); Panel(b, s, style, w, y, H, false, f, s.seed + f * 7 + 1, true); b.Pop();
            b.Push(new Vector3(w * 0.5f, 0f, zc), 90f); Panel(b, s, f == 0 ? 1 : (s.upper == 1 ? 1 : (s.upper == 2 ? 2 : 0)), dd, y, H, false, f, s.seed + f * 7 + 2, s.sideR); b.Pop();
            b.Push(new Vector3(-w * 0.5f, 0f, zc), 270f); Panel(b, s, f == 0 ? 1 : (s.upper == 1 ? 1 : (s.upper == 2 ? 2 : 0)), dd, y, H, false, f, s.seed + f * 7 + 3, s.sideL); b.Pop();
            y += H;
        }
        float topY = y;
        // atap
        float zc0 = jet * 0.5f;
        float rise = 0f;
        int sEnd = Sl.PlasterB;
        if (s.roof == 0)
        {
            float span = d + jet, hw = span * 0.5f; rise = hw * s.pitch;
            TileRoof(b, Sl.Roof, Sl.RoofB, Sl.Wood, sEnd, new Vector3(0f, topY, zc0), w, span, rise, 0.62f, 0.28f, s.seed, 0.42f, true);
            for (int k = 0; k < s.dormers; k++)
            {
                float dx = s.dormers == 1 ? (s.chimney == 0 ? 1f : -1f) * w * 0.12f : (k == 0 ? -1f : 1f) * w * 0.24f;
                Dormer(b, Sl.Roof, Sl.Wood, Sl.PlasterB, new Vector3(0f, topY, zc0), dx, hw * 0.52f, hw, rise, 1.5f, 1.15f, s.seed + k * 5);
            }
        }
        else if (s.roof == 1)
        {
            float span = w, hw = span * 0.5f; rise = hw * s.pitch;
            float len = d + jet;
            b.Push(new Vector3(0f, topY, zc0), 90f);
            TileRoof(b, Sl.Roof, Sl.RoofB, Sl.Wood, sEnd, Vector3.zero, len, span, rise, 0.5f, 0.3f, s.seed, 0.42f, true);
            b.Pop();
            // hiasan gable depan: rangka, jendela loteng, balok kerek
            float zg = d * 0.5f + jet + 0.0f;
            b.Push(new Vector3(0f, topY, zg), 0f);
            Strip(b, Sl.Wood, -hw, hw, -0.02f, 0.18f, 0f, 0.1f);
            Strip(b, Sl.Wood, -0.07f, 0.07f, 0.1f, rise - 0.2f, 0f, 0.08f);
            Brace(b, Sl.Wood, -hw + 0.3f, 0.2f, -0.35f, rise * 0.55f, 0f, 0.1f, 0.07f);
            Brace(b, Sl.Wood, hw - 0.3f, 0.2f, 0.35f, rise * 0.55f, 0f, 0.1f, 0.07f);
            Win(b, 0f, Mathf.Min(1.7f, rise * 0.42f), 0.7f, 0.85f, s.seed, (s.shutters ? 1 : 0) | 2);
            if (s.hoist)
            {
                b.Beam(Sl.Wood, new Vector3(0f, rise * 0.62f, 0.05f), new Vector3(0f, rise * 0.62f, 1.3f), 0.14f, 0.14f);
                b.Box(Sl.Metal, new Vector3(0f, rise * 0.62f - 0.12f, 1.25f), new Vector3(0.08f, 0.3f, 0.08f));
            }
            b.Pop();
        }
        else
        {
            float span = d + jet, hw = span * 0.5f; rise = hw * s.pitch;
            HipRoof(b, Sl.Roof, Sl.RoofB, Sl.Wood, new Vector3(0f, topY, zc0), w, span, rise, 0.6f, s.seed, 0.45f);
        }
        // cerobong
        if (s.chimney >= 0)
        {
            float cx = (s.chimney == 0 ? -1f : 1f) * (w * 0.5f - 0.95f);
            float cz = s.roof == 1 ? -d * 0.18f : zc0;
            Chimney(b, new Vector3(cx, topY - 0.2f, cz), 0.95f, rise * (s.roof == 1 ? 0.55f : 0.95f) + 0.55f, 0f);
        }
        // menara sudut + balkon + papan gantung
        if (s.turret) Turret(b, -w * 0.5f + 0.35f, d * 0.5f + jet - 0.35f, BaseY + s.gh + 0.15f, topY + rise * 0.15f, 1.15f, s.seed);
        if (s.balcony)
        {
            float yb = BaseY + s.gh;
            Balcony(b, s.doorX, yb + 0.03f, d * 0.5f + 0.02f, 2.7f, 1.05f);
        }
        if (s.sign) HangSign(b, (s.doorX < 0f ? 1f : -1f) * (w * 0.5f - 0.55f) + 0f, BaseY + s.gh - 0.2f, 1.25f, s.seed, (s.seed % 2 == 0) ? Sl.Cloth : Sl.ClothB);
        s.Height = topY + rise + 0.3f;
        return s.Height;
    }

    // ------------------------------------------------------------------ jenis dari denah (key = T2 kind, v = variasi)
    public static TSpec SpecOf(string kind, int v)
    {
        var s = new TSpec();
        s.seed = 3 + v * 7;
        bool a = v % 2 == 0;
        switch (kind)
        {
            case "row_a":
                s.w = 6f; s.d = 9f; s.floors = 3; s.gh = 2.8f; s.fh = 2.5f; s.roof = 1; s.pitch = 1.0f; s.jetty = a; s.sideL = s.sideR = false;
                s.doorX = a ? -1.5f : 1.5f; s.chimney = v % 3 == 0 ? -1 : (a ? 1 : 0); s.shutters = v % 3 != 1; s.hoist = v % 4 == 3;
                if (v % 3 == 2) { s.gnd = 1; s.awning = true; s.doorX = a ? 1.5f : -1.5f; }
                break;
            case "row_b":
                s.w = 7f; s.d = 9.5f; s.floors = 3; s.gh = 2.9f; s.fh = 2.55f; s.roof = a ? 0 : 1; s.pitch = a ? 0.8f : 0.95f; s.jetty = !a; s.sideL = s.sideR = false;
                s.doorX = a ? 1.9f : -1.9f; s.chimney = a ? 0 : -1; s.upper = v % 5 == 4 ? 2 : 0; s.planters = v % 3 == 0;
                if (a) s.dormers = 1;
                break;
            case "twn_a":
                s.w = 8.4f; s.d = 10f; s.floors = 2; s.gh = 3f; s.fh = 2.8f; s.roof = a ? 0 : 1; s.pitch = a ? 0.78f : 0.92f; s.jetty = v % 3 != 0;
                s.doorX = v % 3 == 1 ? 0f : -2.1f; s.chimney = v % 2; s.dormers = a ? (v % 4 == 0 ? 2 : 1) : 0; s.planters = v % 3 == 2; s.hoist = !a && v % 4 == 1;
                s.upper = v % 7 == 6 ? 1 : 0;
                break;
            case "twn_b":
                s.w = 9.6f; s.d = 9f; s.floors = 3; s.gh = 2.9f; s.fh = 2.6f; s.roof = 0; s.pitch = 0.74f; s.jetty = a; s.sideL = v % 3 == 0; s.sideR = v % 3 != 0;
                s.doorX = a ? 2.4f : -2.4f; s.chimney = a ? 0 : 1; s.dormers = 2; s.planters = !a;
                break;
            case "shop_a":
                s.w = 9f; s.d = 9.5f; s.floors = 2; s.gh = 3.2f; s.fh = 2.7f; s.roof = 0; s.pitch = 0.75f; s.gnd = 1; s.awning = true; s.sign = true; s.jetty = true;
                s.doorX = a ? -2.6f : 2.6f; s.chimney = a ? 1 : 0; s.sideL = v % 2 == 0; s.sideR = v % 2 == 1; s.dormers = v % 3 == 0 ? 1 : 0;
                break;
            case "cot_a":
                s.w = 7f; s.d = 7.5f; s.floors = 1; s.gh = 3.1f; s.roof = 0; s.pitch = 0.95f; s.upper = 0; s.gnd = v % 3 == 0 ? 2 : 0; s.doorX = a ? -1.2f : 1.2f; s.chimney = v % 2;
                s.dormers = 1; s.sideL = s.sideR = v % 3 != 0; s.shutters = true; s.planters = v % 4 == 1;
                break;
            case "corner":
                s.w = 10.5f; s.d = 10.5f; s.floors = 3; s.gh = 2.9f; s.fh = 2.55f; s.roof = 2; s.pitch = 0.72f; s.jetty = true; s.turret = true; s.doorX = 2.2f; s.chimney = 1;
                s.sideL = s.sideR = true; s.sign = v % 2 == 0;
                break;
            case "villa":
                s.w = 11.5f; s.d = 10f; s.floors = 2; s.gh = 3.1f; s.fh = 2.9f; s.roof = 2; s.pitch = 0.75f; s.upper = v % 3 == 0 ? 1 : 0; s.doorX = 0f; s.balcony = true; s.chimney = v % 2;
                s.sideL = s.sideR = true; s.planters = true;
                break;
            case "store":
                s.w = 12f; s.d = 9f; s.floors = 2; s.gh = 3.4f; s.fh = 2.8f; s.gnd = 3; s.upper = 2; s.roof = 0; s.pitch = 0.62f; s.doorX = a ? -2.6f : 2.6f; s.hoist = false; s.chimney = -1;
                s.sideL = s.sideR = true;
                break;
            case "workshop":
                s.w = 9f; s.d = 8f; s.floors = 2; s.gh = 3.2f; s.fh = 2.4f; s.gnd = 3; s.upper = 0; s.roof = 0; s.pitch = 0.7f; s.doorX = a ? -1.9f : 1.9f; s.chimney = a ? 1 : 0; s.awning = false;
                s.sideL = s.sideR = true; s.shutters = true;
                break;
            case "farmhouse":
                s.w = 12f; s.d = 9f; s.floors = 1; s.gh = 3.3f; s.roof = 0; s.pitch = 0.9f; s.upper = 0; s.gnd = 2; s.doorX = a ? -2.2f : 2.2f; s.chimney = a ? 1 : 0;
                s.dormers = 2; s.sideL = s.sideR = true; s.shutters = true; s.planters = true;
                break;
            default:
                s.w = 8f; s.d = 8f; break;
        }
        return s;
    }

    /// <summary>Rumah tanah liat beratap jerami di ladang luar (thc_a / thc_b) memakai model Desa Pemula; lainnya rumah kota.</summary>
    public static float Kind(GeoBag b, string kind, int v)
    {
        if (kind == "thc_a") return V2Kit.ThatchA(b, v % 2);
        if (kind == "thc_b") return V2Kit.ThatchB(b, v % 2);
        return TownHouse(b, SpecOf(kind, v));
    }
}
