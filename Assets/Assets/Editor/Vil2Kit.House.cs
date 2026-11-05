using System.Collections.Generic;
using UnityEngine;

/// <summary>Rumah warga parametris untuk Desa Pemula: dinding (bingkai kayu + plester bantal / balok / batu tak rata / papan), lantai bertingkat dengan teras menjorok, atap jerami atau sirap.</summary>
public static partial class V2Kit
{
    public class HSpec
    {
        public float w = 6f, d = 5f, wallH = 2.6f, rise = 2.2f, ov = 0.7f;
        public int floors = 1, wall, roof, seed = 1, windowsFront = 2, windowsSide = 1, chimney = 1;     // wall: 0 plaster+rangka, 1 balok, 2 batu, 3 papan; roof: 0 jerami, 1 sirap; chimney: -1 tak ada, 0 kiri, 1 kanan
        public bool jetty, porch, shutters = true, flowers = true, ridgeAlongZ, lantern = true, woodpile = true;
        public bool hood, ivy, archWin, archDoor;      // tudung pintu, sulur ivy, jendela & pintu lengkung
        public bool noSalt;                              // true = tanpa garis garam di ambang (gudang kayu, bukan tempat tinggal)
        public float saltY = -1f;                        // tinggi garis garam bila lantai di depan pintu bukan lempeng tangga bawaan (mis. serambi lebar penginapan); <0 = otomatis
        public int dormers;                              // jumlah lukarne di lereng depan (hanya atap sirap, punggung sepanjang X)
        public int eyebrows;                             // jumlah "alis" jendela bulat pada atap jerami
        public float doorX;
        public float Height { get { return 0.45f + wallH * floors + rise + 0.3f; } }
    }

    // ------------------------------------------------------------------ bidang plester menggembung lembut di antara rangka kayu
    static void PillowBay(GeoBag b, int slot, float xa, float xb, float ya, float yb, float amp, float uOff, float vOff, int seed)
    {
        const int N = 5;
        var P = new Vector3[N, N];
        for (int i = 0; i < N; i++)
            for (int j = 0; j < N; j++)
            {
                float u = i / (float)(N - 1), v = j / (float)(N - 1);
                float bulge = amp * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * u)), 0.55f) * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * v)), 0.55f);
                if (i > 0 && i < N - 1 && j > 0 && j < N - 1) bulge += (Rnd(seed, i * 7 + j) - 0.5f) * amp * 0.3f;
                P[i, j] = new Vector3(Mathf.Lerp(xa, xb, u), Mathf.Lerp(ya, yb, v), bulge);
            }
        Grid(b, slot, P, false, false, 1f, 1f, null, uOff + xa, vOff + ya);
    }

    /// <summary>Dinding batu berombak: titik acak menggeser permukaan sedikit keluar, tepi tetap rata.</summary>
    static void StoneGrid(GeoBag b, int slot, float xa, float xb, float ya, float yb, int seed)
    {
        int nx = Mathf.Max(3, Mathf.CeilToInt((xb - xa) / 0.55f)) + 1, ny = Mathf.Max(3, Mathf.CeilToInt((yb - ya) / 0.55f)) + 1;
        var P = new Vector3[nx, ny];
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < ny; j++)
            {
                float u = i / (float)(nx - 1), v = j / (float)(ny - 1);
                float fade = Mathf.Min(1f, Mathf.Min(u, 1f - u) * (xb - xa) / 0.35f) * Mathf.Min(1f, Mathf.Min(v, 1f - v) * (yb - ya) / 0.35f);
                float bulge = fade * (0.025f + 0.035f * Rnd(seed, i * 13 + j));
                P[i, j] = new Vector3(Mathf.Lerp(xa, xb, u), Mathf.Lerp(ya, yb, v), bulge);
            }
        Grid(b, slot, P, false, false, 1f, 1f, null, xa + seed * 0.37f, ya);
    }

    static List<float> WindowXs(HSpec s, float W, bool front, int floorIdx)
    {
        var xs = new List<float>();
        int nw = front ? s.windowsFront : s.windowsSide;
        if (front && floorIdx == 0)
        {
            for (int k = 0; k < nw; k++)
            {
                float x = (k % 2 == 0 ? -1f : 1f) * (W * 0.5f - 1.15f - (k / 2) * 1.5f);
                if (Mathf.Abs(x - s.doorX) < 1.5f) continue;
                xs.Add(x);
            }
            xs.Add(s.doorX);
        }
        else
            for (int k = 0; k < nw; k++) xs.Add(nw == 1 ? 0f : -W * 0.5f + W * (k + 1f) / (nw + 1f));
        return xs;
    }

    // ------------------------------------------------------------------ satu panel dinding (bidang z=0, muka +Z), lebar W, dari y0 setinggi H
    static void Panel(GeoBag b, HSpec s, int style, float W, float y0, float H, bool front, int floorIdx, int panelSeed, int side)
    {
        int slotWall = style == 2 ? Sl.Stone : (style == 3 ? Sl.WoodLight : (floorIdx > 0 ? Sl.PlasterB : Sl.Plaster));
        if (style == 0)
        {
            // rangka kayu: sill, plat atas, tiang tiap ~1,9 m, palang tengah, braket diagonal; plester menggembung di tiap petak
            int np = Mathf.Max(2, Mathf.RoundToInt(W / 1.9f) + 1);
            var wxs = WindowXs(s, W, front, floorIdx);
            bool mid = H > 2.3f;
            float yLo = y0 + 0.14f, yHi = y0 + H - 0.17f, yMid = y0 + H * 0.52f;
            for (int k = 0; k + 1 < np; k++)
            {
                float xa = -W * 0.5f + 0.07f + k * (W - 0.14f) / (np - 1) + 0.065f, xb = -W * 0.5f + 0.07f + (k + 1) * (W - 0.14f) / (np - 1) - 0.065f;
                if (mid)
                {
                    PillowBay(b, slotWall, xa, xb, yLo, yMid - 0.05f, 0.035f, panelSeed * 1.7f, 0f, panelSeed * 11 + k * 3);
                    PillowBay(b, slotWall, xa, xb, yMid + 0.05f, yHi, 0.035f, panelSeed * 1.7f, 0f, panelSeed * 11 + k * 3 + 1);
                }
                else PillowBay(b, slotWall, xa, xb, yLo, yHi, 0.035f, panelSeed * 1.7f, 0f, panelSeed * 11 + k * 3);
            }
            b.Box(Sl.Wood, new Vector3(0f, y0 + 0.07f, 0.045f), new Vector3(W + 0.05f, 0.14f, 0.09f));
            b.Box(Sl.Wood, new Vector3(0f, y0 + H - 0.08f, 0.05f), new Vector3(W + 0.1f, 0.18f, 0.1f));
            for (int k = 0; k < np; k++)
            {
                float x = -W * 0.5f + 0.07f + k * (W - 0.14f) / (np - 1);
                b.Box(Sl.Wood, new Vector3(x, y0 + H * 0.5f, 0.045f), new Vector3(0.14f, H, 0.09f));
            }
            if (mid) b.Box(Sl.Wood, new Vector3(0f, y0 + H * 0.52f, 0.04f), new Vector3(W, 0.1f, 0.08f));
            for (int k = 0; k + 1 < np; k++)
            {
                if ((k + panelSeed) % 2 != 0) continue;
                float xa = -W * 0.5f + 0.07f + k * (W - 0.14f) / (np - 1), xb = -W * 0.5f + 0.07f + (k + 1) * (W - 0.14f) / (np - 1);
                bool blocked = false;
                foreach (float wx in wxs) if (wx > xa - 0.55f && wx < xb + 0.55f) blocked = true;
                if (blocked) continue;
                b.Beam(Sl.Wood, new Vector3(xa + 0.08f, y0 + H - 0.14f, 0.05f), new Vector3(xb - 0.08f, y0 + 0.18f, 0.05f), 0.1f, 0.07f, new Vector3(0f, 0f, 1f));
            }
        }
        else if (style == 1)
        {
            // dinding balok: tumpukan batang bundar, ujung menonjol di sudut
            int rows = Mathf.Max(3, Mathf.RoundToInt(H / 0.29f));
            float r = H / rows * 0.5f;
            for (int k = 0; k < rows; k++)
            {
                float yy = y0 + r + k * 2f * r, ex = 0.2f + 0.05f * Rnd(panelSeed, k);
                LogH(b, (k % 3 == 2) ? Sl.WoodLight : Sl.Wood, new Vector3(-W * 0.5f - ex, yy, 0.02f), new Vector3(W * 0.5f + ex, yy, 0.02f), r * 1.05f);
            }
        }
        else if (style == 2)
        {
            StoneGrid(b, Sl.Stone, -W * 0.5f, W * 0.5f, y0, y0 + H, panelSeed);
            // pojok batu besar (quoin) berselang-seling
            int nq = Mathf.Max(3, Mathf.RoundToInt(H / 0.55f));
            for (int k = 0; k < nq; k++)
            {
                float yy = y0 + (k + 0.5f) * H / nq, ww = (k % 2 == 0) ? 0.62f : 0.42f;
                b.Box(Sl.DarkStone, new Vector3(-W * 0.5f + ww * 0.5f, yy, 0.05f), new Vector3(ww, H / nq * 0.92f, 0.12f));
                b.Box(Sl.DarkStone, new Vector3(W * 0.5f - ww * 0.5f, yy, 0.05f), new Vector3(ww, H / nq * 0.92f, 0.12f));
            }
            b.Box(Sl.DarkStone, new Vector3(0f, y0 + H - 0.05f, 0.05f), new Vector3(W, 0.12f, 0.12f));
        }
        else
        {
            // papan tegak + tutup celah (board & batten)
            b.Quad(slotWall, new Vector3(-W * 0.5f, y0, 0f), new Vector3(W * 0.5f, y0, 0f), new Vector3(W * 0.5f, y0 + H, 0f), new Vector3(-W * 0.5f, y0 + H, 0f));
            int nb = Mathf.Max(4, Mathf.RoundToInt(W / 0.55f));
            for (int k = 0; k <= nb; k++) b.Box(Sl.Wood, new Vector3(-W * 0.5f + k * W / nb, y0 + H * 0.5f, 0.03f), new Vector3(0.07f, H, 0.05f));
            b.Box(Sl.Wood, new Vector3(0f, y0 + 0.1f, 0.04f), new Vector3(W, 0.2f, 0.07f));
            b.Box(Sl.Wood, new Vector3(0f, y0 + H - 0.08f, 0.04f), new Vector3(W, 0.16f, 0.07f));
        }

        // jendela dan pintu (dinding balok: permukaan efektif ~0,14 m di depan bidang z = 0)
        bool pushed = style == 1;
        if (pushed) b.Push(new Vector3(0f, 0f, 0.13f), 0f);
        int nw = front ? s.windowsFront : s.windowsSide;
        float wy = y0 + H * 0.55f;
        float wh = Mathf.Min(1.05f, H * 0.4f), ww2 = 0.84f;
        if (front && floorIdx == 0)
        {
            Door(b, s.doorX, 1.15f, 2.1f, s.archDoor, s.lantern, panelSeed, style == 2);
            if (s.hood && !s.porch) DoorHood(b, s.doorX, 1.15f, 2.1f, Sl.RoofB, Sl.Wood, panelSeed, y0 + H - 0.35f);
            for (int k = 0; k < nw; k++)
            {
                float x = (k % 2 == 0 ? -1f : 1f) * (W * 0.5f - 1.15f - (k / 2) * 1.5f);
                if (Mathf.Abs(x - s.doorX) < 1.5f) continue;
                Window(b, x, wy, ww2, wh, s.shutters, s.flowers && k == 0, panelSeed + k, s.archWin);
            }
        }
        else if (nw > 0)
        {
            for (int k = 0; k < nw; k++)
            {
                float x = nw == 1 ? 0f : -W * 0.5f + W * (k + 1f) / (nw + 1f);
                if (front && floorIdx == 0 && Mathf.Abs(x - s.doorX) < 1.5f) continue;
                Window(b, x, wy, ww2, wh, s.shutters && floorIdx == 0, front && floorIdx > 0 && s.flowers && k == 0, panelSeed + k, s.archWin && floorIdx == 0);
            }
        }
        if (s.ivy && side == 2 && floorIdx == 0) Ivy(b, W * 0.22f, y0 + 0.3f, 1.5f, Mathf.Min(2.3f, H * 0.85f), panelSeed);
        if (pushed) b.Pop();
    }

    // ------------------------------------------------------------------ bangunan lengkap
    /// <summary>n82 (kanon Desa Pemula): true hanya selama TownSceneBuilder.V2BuildVillage berjalan, sehingga rumah di Desa Pemula mendapat garis garam di ambang
    /// sedangkan pemakai lain kit ini (rumah jerami cincin ladang Kota Ashwatch) tetap tanpa garam.</summary>
    public static bool SaltAtDoor;

    /// <summary>Garis garam melintang di ambang (dua jari lebar, tidak boleh putus satu titik pun): pita putih tipis rata di lantai + butir garam di tepinya.
    /// x = tengah pintu, z = bidang depan, y = permukaan lantai, half = setengah panjang garis.</summary>
    public static void Salt(GeoBag b, float x, float z, float y, int seed, float half = 0.74f)
    {
        var pts = new List<Vector2>();
        const int N = 7;
        for (int i = 0; i < N; i++) pts.Add(new Vector2(x - half + 2f * half * i / (N - 1), z + (Rnd(seed + 3, i) - 0.5f) * 0.02f));
        b.Ribbon(Sl.Marble, pts, 0.14f, y);
        int grains = Mathf.Max(6, Mathf.RoundToInt(half * 12f));
        for (int i = 0; i < grains; i++)
        {
            float gx = x - half + 2f * half * (i + Rnd(seed + 7, i) * 0.7f) / grains;
            float gz = z + (Rnd(seed + 9, i) - 0.5f) * 0.14f;
            b.Ball(Sl.Marble, new Vector3(gx, y + 0.008f, gz), 0.02f + 0.012f * Rnd(seed + 11, i), 2, 4, 0.7f);
        }
    }

    public static float House(GeoBag b, HSpec s)
    {
        float w = s.w, d = s.d;
        float baseY = 0.45f;
        Foundation(b, w, d, baseY, s.seed, s.doorX);
        float y = baseY;
        float topW = w, topD = d;
        for (int f = 0; f < s.floors; f++)
        {
            float fw = w, fd = d;
            if (s.jetty && f > 0) { fw = w + 0.7f; fd = d + 0.7f; }
            int style = s.wall;
            if (f > 0 && s.wall == 2) style = 0;                        // lantai atas rumah batu: bingkai kayu & plester
            if (f > 0 && s.wall == 1) style = 3;
            // lantai atas menjorok: balok penopang
            if (s.jetty && f > 0)
            {
                for (int k = 0; k < 6; k++)
                {
                    float bx = -w * 0.5f + (k + 0.5f) * w / 6f;
                    b.Box(Sl.Wood, new Vector3(bx, y - 0.14f, d * 0.5f + 0.2f), new Vector3(0.16f, 0.26f, 0.8f));
                    b.Box(Sl.Wood, new Vector3(bx, y - 0.14f, -d * 0.5f - 0.2f), new Vector3(0.16f, 0.26f, 0.8f));
                }
                b.Box(Sl.Wood, new Vector3(0f, y - 0.02f, 0f), new Vector3(fw, 0.12f, fd));
            }
            else if (f > 0) b.Box(Sl.Wood, new Vector3(0f, y - 0.03f, 0f), new Vector3(fw + 0.14f, 0.14f, fd + 0.14f));       // balok pembagi lantai
            // sisi: depan(+Z) belakang(-Z) kanan(+X) kiri(-X)
            b.Push(new Vector3(0f, 0f, fd * 0.5f), 0f); Panel(b, s, style, fw, y, s.wallH, true, f, s.seed + f * 7, 0); b.Pop();
            b.Push(new Vector3(0f, 0f, -fd * 0.5f), 180f); Panel(b, s, style, fw, y, s.wallH, false, f, s.seed + f * 7 + 1, 1); b.Pop();
            b.Push(new Vector3(fw * 0.5f, 0f, 0f), 90f); Panel(b, s, style, fd, y + (style == 1 ? 0.145f : 0f), s.wallH, false, f, s.seed + f * 7 + 2, 2); b.Pop();
            b.Push(new Vector3(-fw * 0.5f, 0f, 0f), 270f); Panel(b, s, style, fd, y + (style == 1 ? 0.145f : 0f), s.wallH, false, f, s.seed + f * 7 + 3, 3); b.Pop();
            y += s.wallH;
            topW = fw; topD = fd;
        }
        // atap
        Vector3 rc = new Vector3(0f, y, 0f);
        bool alongZ = s.ridgeAlongZ;
        float len = alongZ ? topD : topW, span = alongZ ? topW : topD;
        int wallSlot = s.wall == 2 && s.floors == 1 ? Sl.Stone : Sl.PlasterB;
        if (alongZ) b.Push(rc, 90f);
        Vector3 rcl = alongZ ? Vector3.zero : rc;
        if (s.roof == 0)
        {
            float[] ey = null;
            if (s.eyebrows > 0 && !alongZ)
            {
                float cs = s.chimney == 0 ? -1f : 1f;
                ey = s.eyebrows == 1 ? new[] { -cs * len * 0.2f } : new[] { -len * 0.25f, len * 0.25f };
            }
            ThatchRoof(b, Sl.Thatch, Sl.Thatch, Sl.Wood, wallSlot, rcl, len, span, s.rise, s.ov, s.seed * 13 + 5, 0.28f, ey);
        }
        else
        {
            float[] dm = null;
            if (s.dormers > 0 && !alongZ)
            {
                float cs = s.chimney == 0 ? -1f : 1f;
                dm = s.dormers == 1 ? new[] { -cs * len * 0.2f } : new[] { -cs * len * 0.3f, -cs * len * 0.02f };
            }
            ShingleRoof(b, Sl.Roof, Sl.RoofB, Sl.Wood, wallSlot, rcl, len, span, s.rise, s.ov, 0.35f, true, s.seed, true, dm);
        }
        if (alongZ) b.Pop();
        // cerobong
        if (s.chimney >= 0)
        {
            float cx = (s.chimney == 0 ? -1f : 1f) * (w * 0.5f - 0.9f);
            float cy = y + s.rise * 0.45f;
            Chimney(b, new Vector3(cx, cy, 0f), 0.85f, s.rise * 0.55f + 0.95f);
        }
        // serambi kecil
        if (s.porch) Porch(b, s.doorX, d * 0.5f, 2.4f, 1.8f, 2.5f);
        // garam di ambang: melintang di lempeng tangga depan pintu (Door(): lempeng z 0,1..1,0 dari dinding, permukaan y = 0,18; serambi kecil menutupinya pada y = 0,22)
        if (SaltAtDoor && !s.noSalt) Salt(b, s.doorX, d * 0.5f + 0.45f, s.saltY >= 0f ? s.saltY : (s.porch ? 0.232f : 0.192f), s.seed, 0.8f);
        // tumpukan kayu bakar di sisi
        if (s.woodpile) WoodPile(b, new Vector3(-(w * 0.5f + 0.4f), baseY - 0.4f + 0.05f, -0.3f), 1.7f, 0.9f, 90f, s.seed);
        return s.Height;
    }
}
