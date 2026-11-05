using System;
using System.Collections.Generic;
using UnityEngine;

public static partial class ForestKit
{
    public struct Op
    {
        public float cx, w, yb, yt;
        public Op(float cx, float w, float yb, float yt) { this.cx = cx; this.w = w; this.yb = yb; this.yt = yt; }
    }

    // ------------------------------------------------------------------ dinding
    static void WallBox(GeoBag b, int slot, float xa, float xb, float zc, float ya, float yb2, float th)
    {
        if (xb - xa < 0.02f || yb2 - ya < 0.02f) return;
        b.Box(slot, new Vector3((xa + xb) / 2f, (ya + yb2) / 2f, zc), new Vector3(xb - xa, yb2 - ya, th), 0f, true, false);
    }

    /// <summary>Dinding sepanjang X lokal di z=zc. ruin 0..1 = seberapa runtuh (puncak bergerigi, lintel hilang).</summary>
    public static void Wall(GeoBag b, int slot, float x0, float x1, float zc, float y0, float y1, float th, List<Op> ops, float ruin, int seed)
    {
        ops.Sort((p, q) => p.cx.CompareTo(q.cx));
        int counter = 0;
        Action<float, float> solid = (xa, xb) =>
        {
            if (xb - xa < 0.02f) return;
            if (ruin <= 0.01f) { WallBox(b, slot, xa, xb, zc, y0, y1, th); return; }
            int n = Mathf.Max(1, Mathf.RoundToInt((xb - xa) / 0.85f));
            float w = (xb - xa) / n;
            for (int i = 0; i < n; i++)
            {
                float f = R01(seed * 13 + counter++, i, 7);
                float keep = Mathf.Lerp(1f, 0.12f + 0.55f * f, Mathf.Clamp01(ruin * (0.4f + 0.9f * R01(seed, counter, 8))));
                float hh = Mathf.Max(0.45f, (y1 - y0) * keep);
                WallBox(b, slot, xa + i * w, xa + (i + 1) * w + 0.01f, zc, y0, y0 + hh, th);
            }
        };
        float cur = x0;
        foreach (var op in ops)
        {
            solid(cur, op.cx - op.w / 2f);
            if (op.yb > y0 + 0.01f) WallBox(b, slot, op.cx - op.w / 2f, op.cx + op.w / 2f, zc, y0, op.yb, th);
            if (op.yt < y1 - 0.01f && (ruin < 0.35f || R01(seed, counter++, 9) > 0.55f)) WallBox(b, slot, op.cx - op.w / 2f, op.cx + op.w / 2f, zc, op.yt, y1, th);
            cur = op.cx + op.w / 2f;
        }
        solid(cur, x1);
    }

    static bool Hits(List<Op> ops, float x0, float x1)
    {
        foreach (var o in ops) if (x1 > o.cx - o.w / 2f - 0.1f && x0 < o.cx + o.w / 2f + 0.1f) return true;
        return false;
    }

    /// <summary>Rangka kayu (half-timber) di muka dinding.</summary>
    public static void Timber(GeoBag b, int slot, float x0, float x1, float zc, float y0, float y1, float th, float side, List<Op> ops)
    {
        float zo = zc + side * (th / 2f + 0.02f);
        int n = Mathf.Max(1, Mathf.RoundToInt((x1 - x0) / 1.6f));
        float[] xs = new float[n + 1];
        for (int i = 0; i <= n; i++) xs[i] = x0 + (x1 - x0) * i / n;
        foreach (float x in xs) if (!Hits(ops, x - 0.05f, x + 0.05f)) b.Box(slot, new Vector3(x, (y0 + y1) / 2f, zo), new Vector3(0.15f, y1 - y0, 0.07f));
        b.Box(slot, new Vector3((x0 + x1) / 2f, y0 + 0.1f, zo), new Vector3(x1 - x0, 0.2f, 0.08f));
        b.Box(slot, new Vector3((x0 + x1) / 2f, y1 - 0.1f, zo), new Vector3(x1 - x0, 0.2f, 0.08f));
        for (int i = 0; i < n; i++)
            if (!Hits(ops, xs[i], xs[i + 1]))
                b.Beam(slot, new Vector3(xs[i] + 0.08f, y0 + 0.2f, zo), new Vector3(xs[i + 1] - 0.08f, y1 - 0.2f, zo), 0.07f, 0.11f);
    }

    static void Window(GeoBag b, float x, float yb, float yt, float zc, float th, float side, bool shutters, bool box, int seed)
    {
        float zf = zc + side * (th / 2f);
        float w = 0.85f, h = yt - yb;
        b.Box(Sl.Glass, new Vector3(x, (yb + yt) / 2f, zc + side * 0.02f), new Vector3(w, h, 0.04f));
        b.Box(Sl.Wood, new Vector3(x, yb - 0.04f, zf + side * 0.08f), new Vector3(w + 0.3f, 0.09f, 0.22f));                 // ambang
        b.Box(Sl.Wood, new Vector3(x, (yb + yt) / 2f, zf + side * 0.03f), new Vector3(0.06f, h, 0.06f));                   // kisi
        b.Box(Sl.Wood, new Vector3(x, (yb + yt) / 2f, zf + side * 0.03f), new Vector3(w, 0.06f, 0.06f));
        b.Box(Sl.Wood, new Vector3(x, yt + 0.05f, zf + side * 0.03f), new Vector3(w + 0.2f, 0.12f, 0.1f));
        if (shutters)
        {
            b.Box(Sl.Wood, new Vector3(x - w / 2f - 0.24f, (yb + yt) / 2f, zf + side * 0.05f), new Vector3(0.42f, h, 0.05f));
            b.Box(Sl.Wood, new Vector3(x + w / 2f + 0.24f, (yb + yt) / 2f, zf + side * 0.05f), new Vector3(0.42f, h, 0.05f));
        }
        if (box)
        {
            b.Box(Sl.Wood, new Vector3(x, yb - 0.17f, zf + side * 0.2f), new Vector3(w + 0.1f, 0.17f, 0.24f));
            for (int k = 0; k < 4; k++)
                Blob(b, k % 2 == 0 ? LeafOrange : LeafRed, new Vector3(x - 0.33f + k * 0.22f, yb - 0.02f, zf + side * 0.2f), new Vector3(0.12f, 0.1f, 0.12f), seed + k, 0.2f, 3, 6, 0f);
        }
    }

    static void IvyPatch(GeoBag b, Vector3 c, float w, float h, int seed, float side = 1f)
    {
        int n = Mathf.Max(2, Mathf.RoundToInt(w * h * 1.2f));
        for (int i = 0; i < n; i++)
        {
            Vector3 p = c + new Vector3((R01(seed, i, 141) - 0.5f) * w, (R01(seed, i, 142) - 0.5f) * h, side * 0.05f * R01(seed, i, 143));
            float s = 0.25f + R01(seed, i, 144) * 0.3f;
            Blob(b, Ivy, p, new Vector3(s, s, 0.1f), seed * 11 + i, 0.3f, 3, 7, 0f);
        }
    }

    public static void Rubble(GeoBag b, Vector3 c, float radius, int n, int seed)
    {
        for (int i = 0; i < n; i++)
        {
            float a = R01(seed, i, 151) * 6.28f, r = R01(seed, i + 40, 152) * radius;
            float s = 0.18f + R01(seed, i, 153) * 0.45f;
            b.Box(i % 3 == 0 ? Sl.DarkStone : Sl.Stone, c + new Vector3(Mathf.Sin(a) * r, s * 0.35f, Mathf.Cos(a) * r), new Vector3(s * 1.3f, s * 0.7f, s), R01(seed, i, 154) * 180f);
        }
    }

    // ------------------------------------------------------------------ rumah
    /// <summary>Posisi X pintu depan rumah (sama dengan yang dipakai House()), untuk menaruh titik interaksi.</summary>
    public static float DoorX(int seed, float w) { return (R01(seed, 1, 161) - 0.5f) * w * 0.35f; }

    /// <summary>Rumah desa. Depan menghadap +Z. ruin 0 = terawat; 1 = reruntuhan. roofKind: 0 jerami, 1 genteng.</summary>
    public static void House(GeoBag b, int seed, float w, float d, float wh, float ruin, int roofKind, bool flowers)
    {
        bool lived = ruin < 0.15f;
        int wallSlot = ruin > 0.15f ? Grime : Sl.Plaster;
        int woodSlot = ruin > 0.15f ? Weathered : Sl.Wood;
        float th = 0.3f, fy = 0.5f, y0 = fy, y1 = fy + wh;
        b.BoxB(Sl.Stone, new Vector3(0, -0.3f, 0), new Vector3(w + 0.34f, fy + 0.3f, d + 0.34f));       // pondasi batu
        b.Box(Sl.Wood, new Vector3(0, y0 - 0.04f, 0), new Vector3(w - 0.2f, 0.1f, d - 0.2f));              // lantai
        float dx = (R01(seed, 1, 161) - 0.5f) * w * 0.35f;

        // depan (+Z)
        var front = new List<Op> { new Op(dx, 1.05f, y0, y0 + 2.05f) };
        var winX = new List<float>();
        foreach (float sx in new[] { -w * 0.31f, w * 0.31f })
            if (Mathf.Abs(sx - dx) > 1.35f && Mathf.Abs(sx) < w / 2f - 0.8f) { front.Add(new Op(sx, 0.85f, y0 + 0.95f, y0 + 1.95f)); winX.Add(sx); }
        float xa = -w / 2f - th / 2f, xb = w / 2f + th / 2f;
        Wall(b, wallSlot, xa, xb, d / 2f, y0, y1, th, front, ruin, seed + 1);
        if (ruin < 0.7f) Timber(b, woodSlot, xa, xb, d / 2f, y0, y1, th, 1f, front);

        // belakang (-Z)
        var back = new List<Op>();
        if (w > 4.6f) back.Add(new Op((R01(seed, 2, 162) - 0.5f) * w * 0.4f, 0.85f, y0 + 0.95f, y0 + 1.95f));
        Wall(b, wallSlot, xa, xb, -d / 2f, y0, y1, th, back, ruin, seed + 2);
        if (ruin < 0.7f) Timber(b, woodSlot, xa, xb, -d / 2f, y0, y1, th, -1f, back);

        // samping
        for (int sdx = -1; sdx <= 1; sdx += 2)
        {
            b.Push(new Vector3(sdx * w / 2f, 0f, 0f), 90f);
            var side = new List<Op>();
            if (d > 3.6f) side.Add(new Op(0f, 0.85f, y0 + 0.95f, y0 + 1.95f));
            Wall(b, wallSlot, -d / 2f + th / 2f, d / 2f - th / 2f, 0f, y0, y1, th, side, ruin, seed + 3 + sdx);
            b.Pop();
        }

        // pintu & jendela
        if (ruin < 0.6f)
        {
            b.Box(woodSlot, new Vector3(dx, y0 + 1.02f, d / 2f - 0.02f), new Vector3(0.95f, 2.0f, 0.07f));
            for (int k = 0; k < 3; k++) b.Box(Sl.Metal, new Vector3(dx, y0 + 0.4f + k * 0.65f, d / 2f + 0.03f), new Vector3(0.85f, 0.05f, 0.03f));
            b.Box(Sl.Metal, new Vector3(dx + 0.32f, y0 + 1.0f, d / 2f + 0.05f), new Vector3(0.06f, 0.06f, 0.05f));
            b.Box(woodSlot, new Vector3(dx, y0 + 2.12f, d / 2f), new Vector3(1.35f, 0.16f, 0.4f));
            b.Box(woodSlot, new Vector3(dx - 0.58f, y0 + 1.05f, d / 2f), new Vector3(0.1f, 2.1f, 0.34f));
            b.Box(woodSlot, new Vector3(dx + 0.58f, y0 + 1.05f, d / 2f), new Vector3(0.1f, 2.1f, 0.34f));
        }
        b.Box(Sl.Stone, new Vector3(dx, y0 - 0.12f, d / 2f + 0.45f), new Vector3(1.5f, 0.22f, 0.8f));          // undakan
        if (ruin < 0.6f)
        {
            foreach (float sx in winX) Window(b, sx, y0 + 0.95f, y0 + 1.95f, d / 2f, th, 1f, true, flowers && lived, seed + (int)(sx * 10));
            if (back.Count > 0) Window(b, back[0].cx, y0 + 0.95f, y0 + 1.95f, -d / 2f, th, -1f, true, false, seed + 5);
        }

        // atap
        float rise = Mathf.Min(2.4f, d * 0.42f);
        if (ruin < 0.15f)
        {
            b.Gable(roofKind == 0 ? Sl.Thatch : Sl.Roof, wallSlot, woodSlot, new Vector3(0, y1, 0), w, d, rise, 0.55f, true);
            // porch kecil
            b.Beam(Sl.Roof, new Vector3(dx, y0 + 2.3f, d / 2f + 1.25f), new Vector3(dx, y0 + 2.75f, d / 2f + 0.05f), 2.3f, 0.1f, Vector3.right);
            b.Box(Sl.Wood, new Vector3(dx - 1.0f, y0 + 1.1f, d / 2f + 1.15f), new Vector3(0.14f, 2.2f, 0.14f));
            b.Box(Sl.Wood, new Vector3(dx + 1.0f, y0 + 1.1f, d / 2f + 1.15f), new Vector3(0.14f, 2.2f, 0.14f));
            // lampu gantung
            b.Box(Sl.Glow, new Vector3(dx + 0.85f, y0 + 1.95f, d / 2f + 1.1f), new Vector3(0.14f, 0.2f, 0.14f));
        }
        else
        {
            int rafters = Mathf.Max(3, Mathf.RoundToInt(w / 1.15f));
            for (int i = 0; i <= rafters; i++)
            {
                float x = -w / 2f + w * i / rafters;
                if (R01(seed, i, 171) < ruin * 0.65f) continue;                                    // kuda-kuda yang runtuh
                float cut = R01(seed, i, 172) < ruin ? 0.55f : 1f;
                b.Beam(Weathered, new Vector3(x, y1, -d / 2f), new Vector3(x, y1 + rise * cut, -d / 2f * (1f - cut)), 0.14f, 0.18f, Vector3.right);
                b.Beam(Weathered, new Vector3(x, y1, d / 2f), new Vector3(x, y1 + rise * cut, d / 2f * (1f - cut)), 0.14f, 0.18f, Vector3.right);
                if (cut > 0.9f && R01(seed, i, 173) > 0.3f) b.Box(Weathered, new Vector3(x, y1 + rise - 0.1f, 0f), new Vector3(0.16f, 0.16f, d * 0.2f));
            }
            // sisa atap yang masih menempel
            if (ruin < 0.8f)
            {
                float px = -w / 2f + 0.2f, pw = w * (0.25f + 0.2f * R01(seed, 9, 174));
                b.Quad(roofKind == 0 ? OldThatch : OldRoof, new Vector3(px, y1 + 0.02f, -d / 2f - 0.3f), new Vector3(px + pw, y1 + 0.02f, -d / 2f - 0.3f), new Vector3(px + pw, y1 + rise + 0.05f, 0f), new Vector3(px, y1 + rise + 0.05f, 0f));
            }
            // reruntuhan di dalam & sekitar
            Rubble(b, new Vector3(0, y0, 0), Mathf.Min(w, d) * 0.4f, 6 + (int)(ruin * 6), seed + 31);
            Rubble(b, new Vector3(0, y0 - 0.3f, d / 2f + 1.2f), 1.6f, 4, seed + 32);
            b.Beam(Weathered, new Vector3(-w * 0.2f, y0 + 0.1f, -d * 0.2f), new Vector3(w * 0.25f, y0 + 0.6f, d * 0.25f), 0.2f, 0.2f, Vector3.up);
            for (int i = 0; i < 3; i++) IvyPatch(b, new Vector3((R01(seed, i, 175) - 0.5f) * w * 0.8f, y0 + 1.1f, d / 2f + th / 2f), 1.4f, 1.6f, seed + i);
            Blob(b, Moss, new Vector3(-w * 0.2f, y0 + 0.9f, -d / 2f), new Vector3(1.4f, 0.4f, 0.4f), seed + 77, 0.3f, 3, 8, 0.2f);
        }

        if (lived)
        {
            FantasyKit.DrainPipe(b, new Vector3(w / 2f + 0.12f, y1 - 0.1f, d / 2f + 0.12f), 0.25f, (seed & 1) == 0);
            FantasyKit.HangPot(b, new Vector3(dx - 0.95f, y0 + 2.2f, d / 2f + 0.02f), (seed & 2) == 0 ? Sl.Blossom : Sl.Cloth);
            FantasyKit.HerbBundle(b, new Vector3(dx + 0.95f, y0 + 2.25f, d / 2f + 0.16f), seed);
            FantasyKit.StepStones(b, new Vector3(dx, y0 - 0.45f, d / 2f + 1.6f), 3, seed);
            AnimeKit.FlowerBush(b, new Vector3(-w / 2f + 0.5f, 0f, d / 2f + 0.8f), 0.6f, Sl.Blossom);
            AnimeKit.FlowerBush(b, new Vector3(w / 2f - 0.5f, 0f, d / 2f + 0.8f), 0.5f, Sl.Gold);
        }

        // cerobong
        float cxh = w / 2f - 1.0f;
        float chTop = y1 + rise + 0.7f;
        float chH = ruin > 0.6f ? (R01(seed, 7, 176) * 0.5f + 0.4f) : 1f;
        if (ruin < 0.95f)
        {
            b.BoxB(Sl.Stone, new Vector3(cxh, y0 - 0.2f, 0.0f), new Vector3(0.8f, (chTop - y0 + 0.2f) * chH, 0.8f));
            if (chH > 0.9f) b.Box(Sl.DarkStone, new Vector3(cxh, chTop + 0.1f, 0.0f), new Vector3(1.0f, 0.2f, 1.0f));
        }
    }

    public static void Barn(GeoBag b, int seed, float ruin)
    {
        float w = 9.5f, d = 6.6f, wh = 3.7f, y0 = 0.4f, y1 = y0 + wh, th = 0.3f;
        int wallSlot = ruin > 0.15f ? Weathered : Sl.Wood;
        b.BoxB(Sl.Stone, new Vector3(0, -0.3f, 0), new Vector3(w + 0.3f, y0 + 0.3f, d + 0.3f));
        var front = new List<Op> { new Op(0f, 3.4f, y0, y0 + 3.0f), new Op(0f, 1.0f, y0 + 3.15f, y0 + 3.55f) };
        float xa = -w / 2f - th / 2f, xb = w / 2f + th / 2f;
        Wall(b, wallSlot, xa, xb, d / 2f, y0, y1, th, front, ruin, seed + 1);
        Wall(b, wallSlot, xa, xb, -d / 2f, y0, y1, th, new List<Op>(), ruin, seed + 2);
        for (int sdx = -1; sdx <= 1; sdx += 2)
        {
            b.Push(new Vector3(sdx * w / 2f, 0f, 0f), 90f);
            Wall(b, wallSlot, -d / 2f + th / 2f, d / 2f - th / 2f, 0f, y0, y1, th, new List<Op> { new Op(0f, 0.9f, y0 + 1.6f, y0 + 2.5f) }, ruin, seed + 3 + sdx);
            b.Pop();
        }
        if (ruin < 0.6f)
        {
            b.Box(Sl.Wood, new Vector3(-0.85f, y0 + 1.5f, d / 2f - 0.02f), new Vector3(1.6f, 3.0f, 0.09f));
            b.Box(Sl.Wood, new Vector3(0.85f, y0 + 1.5f, d / 2f - 0.02f), new Vector3(1.6f, 3.0f, 0.09f));
            b.Beam(Sl.Wood, new Vector3(-1.55f, y0 + 0.2f, d / 2f + 0.04f), new Vector3(-0.15f, y0 + 2.8f, d / 2f + 0.04f), 0.05f, 0.18f);
            b.Beam(Sl.Wood, new Vector3(1.55f, y0 + 0.2f, d / 2f + 0.04f), new Vector3(0.15f, y0 + 2.8f, d / 2f + 0.04f), 0.05f, 0.18f);
        }
        if (ruin < 0.15f) b.Gable(Sl.Roof, wallSlot, Sl.Wood, new Vector3(0, y1, 0), w, d, 2.5f, 0.6f, true);
        else
        {
            int rafters = 8;
            for (int i = 0; i <= rafters; i++)
            {
                float x = -w / 2f + w * i / rafters;
                if (R01(seed, i, 181) < ruin * 0.6f) continue;
                b.Beam(Weathered, new Vector3(x, y1, -d / 2f), new Vector3(x, y1 + 2.5f, 0f), 0.16f, 0.2f, Vector3.right);
                b.Beam(Weathered, new Vector3(x, y1, d / 2f), new Vector3(x, y1 + 2.5f, 0f), 0.16f, 0.2f, Vector3.right);
            }
            Rubble(b, new Vector3(0, y0, 0), 3f, 9, seed + 41);
        }
    }

    public static void Inn(GeoBag b, int seed)
    {
        float w = 9.2f, d = 6.8f, y0 = 0.5f, wh = 2.7f, th = 0.3f;
        House(b, seed, w, d, wh, 0f, 1, true);
        // lantai atas menjorok
        float yf = y0 + wh;
        float w2 = w + 0.7f, d2 = d + 0.7f, wh2 = 2.4f;
        b.Box(Sl.Wood, new Vector3(0, yf + 0.1f, 0), new Vector3(w2, 0.3f, d2));
        var op = new List<Op> { new Op(-w2 * 0.25f, 0.9f, yf + 0.7f, yf + 1.85f), new Op(w2 * 0.25f, 0.9f, yf + 0.7f, yf + 1.85f), new Op(0f, 0.9f, yf + 0.7f, yf + 1.85f) };
        float xa = -w2 / 2f - th / 2f, xb = w2 / 2f + th / 2f;
        Wall(b, Sl.Plaster, xa, xb, d2 / 2f, yf + 0.25f, yf + 0.25f + wh2, th, op, 0f, seed + 9);
        Timber(b, Sl.Wood, xa, xb, d2 / 2f, yf + 0.25f, yf + 0.25f + wh2, th, 1f, op);
        foreach (var o in op) Window(b, o.cx, o.yb, o.yt, d2 / 2f, th, 1f, true, false, seed);
        Wall(b, Sl.Plaster, xa, xb, -d2 / 2f, yf + 0.25f, yf + 0.25f + wh2, th, new List<Op>(), 0f, seed + 10);
        for (int sdx = -1; sdx <= 1; sdx += 2)
        {
            b.Push(new Vector3(sdx * w2 / 2f, 0f, 0f), 90f);
            Wall(b, Sl.Plaster, -d2 / 2f + th / 2f, d2 / 2f - th / 2f, 0f, yf + 0.25f, yf + 0.25f + wh2, th, new List<Op> { new Op(0f, 0.9f, yf + 0.7f, yf + 1.85f) }, 0f, seed + 11 + sdx);
            b.Pop();
        }
        b.Gable(Sl.Roof, Sl.Plaster, Sl.Wood, new Vector3(0, yf + 0.25f + wh2, 0), w2, d2, 2.6f, 0.6f, true);
        // papan nama gantung
        b.Beam(Sl.Wood, new Vector3(w / 2f - 1.2f, y0 + 2.2f, d / 2f + 0.1f), new Vector3(w / 2f - 1.2f, y0 + 2.2f, d / 2f + 1.3f), 0.1f, 0.1f);
        b.Box(Sl.Wood, new Vector3(w / 2f - 1.2f, y0 + 1.85f, d / 2f + 1.25f), new Vector3(0.08f, 0.55f, 0.8f));
        b.Box(Sl.Gold, new Vector3(w / 2f - 1.17f, y0 + 1.85f, d / 2f + 1.25f), new Vector3(0.03f, 0.3f, 0.45f));
    }

    public static void Shrine(GeoBag b, int seed, float ruin)
    {
        float w = 5.2f, d = 8.4f, wh = 3.6f, y0 = 0.4f, y1 = y0 + wh, th = 0.4f;
        b.BoxB(Sl.DarkStone, new Vector3(0, -0.3f, 0), new Vector3(w + 0.5f, y0 + 0.3f, d + 0.5f));
        int ws = ruin > 0.15f ? Sl.DarkStone : Sl.Stone;
        var front = new List<Op> { new Op(0f, 1.5f, y0, y0 + 2.6f) };
        Wall(b, ws, -w / 2f - th / 2f, w / 2f + th / 2f, d / 2f, y0, y1, th, front, ruin, seed + 1);
        Wall(b, ws, -w / 2f - th / 2f, w / 2f + th / 2f, -d / 2f, y0, y1, th, new List<Op> { new Op(0f, 0.9f, y0 + 1.5f, y0 + 3.0f) }, ruin, seed + 2);
        for (int sdx = -1; sdx <= 1; sdx += 2)
        {
            b.Push(new Vector3(sdx * w / 2f, 0f, 0f), 90f);
            var ops = new List<Op> { new Op(-2.2f, 0.9f, y0 + 1.3f, y0 + 3.0f), new Op(0f, 0.9f, y0 + 1.3f, y0 + 3.0f), new Op(2.2f, 0.9f, y0 + 1.3f, y0 + 3.0f) };
            Wall(b, ws, -d / 2f + th / 2f, d / 2f - th / 2f, 0f, y0, y1, th, ops, ruin, seed + 3 + sdx);
            b.Pop();
        }
        if (ruin < 0.15f)
        {
            b.Gable(Sl.Roof, Sl.Stone, Sl.Wood, new Vector3(0, y1, 0), w, d, 2.3f, 0.5f, false);
            // menara kecil + lonceng
            b.BoxB(Sl.Stone, new Vector3(0, y1 + 1.2f, d / 2f - 0.8f), new Vector3(1.7f, 2.2f, 1.7f));
            b.Spire(Sl.Roof, new Vector3(0, y1 + 3.4f, d / 2f - 0.8f), 1.3f, 2.4f, 4);
            b.Box(Sl.Gold, new Vector3(0, y1 + 2.5f, d / 2f - 0.8f), new Vector3(0.5f, 0.5f, 0.5f));
        }
        else
        {
            for (int i = 0; i <= 4; i++)
            {
                float z = -d / 2f + d * i / 4f;
                if (R01(seed, i, 191) < ruin * 0.6f) continue;
                b.Beam(Weathered, new Vector3(-w / 2f, y1, z), new Vector3(0f, y1 + 2.2f, z), 0.18f, 0.22f, Vector3.forward);
                b.Beam(Weathered, new Vector3(w / 2f, y1, z), new Vector3(0f, y1 + 2.2f, z), 0.18f, 0.22f, Vector3.forward);
            }
            Rubble(b, new Vector3(0, y0, 0), 3f, 10, seed + 51);
            IvyPatch(b, new Vector3(w / 2f + 0.3f, y0 + 2f, 0f), 3f, 2.4f, seed + 5, 1f);
        }
        // altar
        b.Box(Sl.Marble, new Vector3(0, y0 + 0.5f, -d / 2f + 1.3f), new Vector3(1.8f, 1.0f, 0.9f));
        if (ruin > 0.15f) { b.Box(Crystal, new Vector3(0, y0 + 1.25f, -d / 2f + 1.3f), new Vector3(0.28f, 0.5f, 0.28f)); }
        else b.Box(Sl.Glow, new Vector3(0, y0 + 1.1f, -d / 2f + 1.3f), new Vector3(0.22f, 0.3f, 0.22f));
    }

    public static void RuinTower(GeoBag b, int seed)
    {
        float r = 3.2f;
        var rings = new List<Vector3[]>();
        int n = 14;
        for (int i = 0; i <= 7; i++)
        {
            float t = i / 7f;
            var ring = new Vector3[n];
            for (int j = 0; j < n; j++)
            {
                float a = j * Mathf.PI * 2f / n;
                float rr = r * (1f - 0.12f * t) * (1f + (R01(j, i, seed) - 0.5f) * 0.05f);
                float broken = (i == 7) ? (0.2f + R01(j, 3, seed) * 1.3f) : 0f;       // puncak bergerigi
                ring[j] = new Vector3(Mathf.Sin(a) * rr, -0.3f + t * 13.5f - broken, Mathf.Cos(a) * rr);
            }
            rings.Add(ring);
        }
        b.Loft(Sl.Stone, rings, false, false, true);
        var inner = new List<Vector3[]>();
        for (int i = 0; i <= 7; i++) { var rg = new Vector3[n]; for (int j = 0; j < n; j++) { rg[j] = rings[i][n - 1 - j]; rg[j] = new Vector3(rg[j].x * 0.8f, rg[j].y, rg[j].z * 0.8f); } inner.Add(rg); }
        b.Loft(Sl.DarkStone, inner, false, false, true);
        IvyPatch(b, new Vector3(0, 3.5f, r + 0.1f), 3f, 6f, seed + 1);
        IvyPatch(b, new Vector3(r * 0.8f, 6f, -r * 0.4f), 2.5f, 5f, seed + 2);
        Rubble(b, Vector3.zero, 6.5f, 14, seed + 3);
        Blob(b, Moss, new Vector3(0, 0.2f, 0), new Vector3(r * 1.15f, 0.5f, r * 1.15f), seed, 0.2f, 3, 12, 0.2f);
    }

    // ------------------------------------------------------------------ properti desa
    public static void WindmillBody(GeoBag b)
    {
        var rings = new List<Vector3[]>();
        int n = 12;
        float[] ys = { -0.3f, 0.2f, 7.0f };
        float[] rs = { 2.5f, 2.4f, 1.6f };
        for (int i = 0; i < 3; i++)
        {
            var ring = new Vector3[n];
            for (int j = 0; j < n; j++) { float a = j * Mathf.PI * 2f / n; ring[j] = new Vector3(Mathf.Sin(a) * rs[i], ys[i], Mathf.Cos(a) * rs[i]); }
            rings.Add(ring);
        }
        b.Loft(Sl.Plaster, rings, false, false, true);
        b.Cyl(Sl.Stone, new Vector3(0, -0.3f, 0), 2.7f, 1.1f, 12, 2.6f, true);
        b.Cyl(Sl.Roof, new Vector3(0, 6.95f, 0), 2.1f, 0.4f, 12, 1.9f, true);
        b.Cone(Sl.Roof, new Vector3(0, 7.3f, 0), 2.0f, 2.6f, 12);
        b.Box(Sl.Wood, new Vector3(0, 1.0f, 2.4f), new Vector3(1.0f, 2.0f, 0.2f));
        b.Box(Sl.Wood, new Vector3(0, 4.8f, 1.8f), new Vector3(0.9f, 1.0f, 0.15f));
        b.Box(Sl.Glass, new Vector3(0, 3.2f, 2.05f), new Vector3(0.7f, 0.8f, 0.08f));
        b.Box(Sl.Wood, new Vector3(0, 6.3f, 1.55f), new Vector3(0.5f, 0.5f, 0.9f));
    }

    /// <summary>Baling-baling: berputar pada sumbu Z lokal; pusat di origin.</summary>
    public static void WindmillBlades(GeoBag b)
    {
        b.Cyl(Sl.Wood, new Vector3(0, 0, 0), 0.28f, 0.5f, 8, 0.28f, true);
        for (int k = 0; k < 4; k++)
        {
            float a = k * 90f * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f);
            Vector3 side = new Vector3(Mathf.Cos(a), -Mathf.Sin(a), 0f);
            b.Beam(Sl.Wood, dir * 0.3f, dir * 5.0f, 0.16f, 0.2f, Vector3.forward);
            b.QuadBoth(Sl.Cloth, dir * 1.2f + side * 0.1f + new Vector3(0, 0, 0.12f), dir * 4.8f + side * 0.1f + new Vector3(0, 0, 0.12f), dir * 4.8f + side * 1.15f + new Vector3(0, 0, 0.12f), dir * 1.2f + side * 1.15f + new Vector3(0, 0, 0.12f));
        }
    }

    public static void Well(GeoBag b, int seed, float ruin)
    {
        var rings = new List<Vector3[]>();
        int n = 12;
        for (int i = 0; i < 3; i++)
        {
            var ring = new Vector3[n];
            for (int j = 0; j < n; j++) { float a = j * Mathf.PI * 2f / n; float r = i == 1 ? 1.0f : 0.95f; ring[j] = new Vector3(Mathf.Sin(a) * r, -0.2f + i * 0.55f, Mathf.Cos(a) * r); }
            rings.Add(ring);
        }
        b.Loft(Sl.Stone, rings, false, false, true);
        b.Annulus(Sl.DarkStone, new Vector3(0, 0.9f, 0), 0.62f, 1.0f, 0f, 360f, 12, 0f);
        b.Disc(Sl.Dark, new Vector3(0, 0.55f, 0), 0.7f, 10, 0f);
        for (int sx = -1; sx <= 1; sx += 2) b.Box(ruin > 0.2f ? Weathered : Sl.Wood, new Vector3(sx * 0.95f, 1.6f, 0f), new Vector3(0.14f, 2.2f, 0.14f));
        b.Beam(ruin > 0.2f ? Weathered : Sl.Wood, new Vector3(-1.0f, 2.7f, 0f), new Vector3(1.0f, 2.7f, 0f), 0.12f, 0.12f);
        if (ruin < 0.2f)
        {
            b.Gable(Sl.Thatch, Sl.Wood, Sl.Wood, new Vector3(0, 2.7f, 0), 1.4f, 2.4f, 0.8f, 0.3f, false);
            b.Box(Sl.Wood, new Vector3(0.45f, 1.9f, 0f), new Vector3(0.3f, 0.35f, 0.3f));
        }
        else Rubble(b, new Vector3(0, 0, 0), 1.5f, 5, seed);
    }

    public static void Stall(GeoBag b, int seed)
    {
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                b.Box(Sl.Wood, new Vector3(sx * 1.2f, 1.1f, sz * 0.8f), new Vector3(0.12f, 2.2f, 0.12f));
        b.Box(Sl.Wood, new Vector3(0, 0.85f, 0.7f), new Vector3(2.6f, 0.9f, 0.6f));
        b.Box(Sl.Wood, new Vector3(0, 1.32f, 0.7f), new Vector3(2.7f, 0.07f, 0.75f));
        b.QuadBoth(seed % 2 == 0 ? Sl.Cloth : Sl.ClothB, new Vector3(-1.5f, 2.15f, 1.15f), new Vector3(1.5f, 2.15f, 1.15f), new Vector3(1.5f, 2.55f, -0.95f), new Vector3(-1.5f, 2.55f, -0.95f));
        for (int k = 0; k < 6; k++) Blob(b, k % 2 == 0 ? LeafRed : LeafOrange, new Vector3(-0.9f + k * 0.36f, 1.45f, 0.7f), new Vector3(0.14f, 0.12f, 0.14f), seed + k, 0.1f, 3, 6, 0f);
        b.Box(Sl.Wood, new Vector3(-1.0f, 0.3f, -0.3f), new Vector3(0.6f, 0.6f, 0.6f), 20f);
        b.Box(Sl.Wood, new Vector3(-0.3f, 0.25f, -0.4f), new Vector3(0.5f, 0.5f, 0.5f), -10f);
    }

    public static void Fence(GeoBag b, int seed, float len = 3f, float ruin = 0f)
    {
        int posts = Mathf.Max(2, Mathf.RoundToInt(len / 1.5f) + 1);
        for (int i = 0; i < posts; i++)
        {
            float x = -len / 2f + len * i / (posts - 1);
            float tilt = ruin > 0f ? (R01(seed, i, 201) - 0.5f) * 0.3f * ruin : 0f;
            b.Beam(ruin > 0.1f ? Weathered : Sl.Wood, new Vector3(x, -0.15f, 0), new Vector3(x + tilt, 1.15f, 0), 0.12f, 0.12f, Vector3.forward);
        }
        for (int k = 0; k < 2; k++)
        {
            if (ruin > 0.3f && R01(seed, k, 202) < ruin * 0.6f) continue;
            b.Box(ruin > 0.1f ? Weathered : Sl.Wood, new Vector3(0, 0.45f + k * 0.4f, 0.05f), new Vector3(len, 0.09f, 0.07f));
        }
    }

    public static void Lamp(GeoBag b)
    {
        b.Cyl(Sl.Wood, new Vector3(0, 0, 0), 0.07f, 2.8f, 6, 0.06f, true);
        b.Beam(Sl.Wood, new Vector3(0, 2.7f, 0), new Vector3(0.45f, 2.65f, 0), 0.07f, 0.07f);
        b.Box(Sl.Metal, new Vector3(0.45f, 2.5f, 0), new Vector3(0.24f, 0.05f, 0.24f));
        b.Box(Sl.Glow, new Vector3(0.45f, 2.38f, 0), new Vector3(0.18f, 0.22f, 0.18f));
        b.Cone(Sl.Metal, new Vector3(0.45f, 2.5f, 0), 0.17f, 0.2f, 6);
    }

    public static void HayBale(GeoBag b, int seed)
    {
        b.Cyl(Sl.Thatch, new Vector3(0, 0.0f, 0), 0.6f, 1.1f, 10, 0.6f, true, true);
        b.Box(Sl.Wood, new Vector3(0, 0.3f, 0), new Vector3(1.28f, 0.03f, 0.03f));
    }

    public static void Barrel(GeoBag b)
    {
        b.Cyl(Sl.Wood, new Vector3(0, 0, 0), 0.38f, 0.9f, 10, 0.38f, true);
        b.Cyl(Sl.Metal, new Vector3(0, 0.15f, 0), 0.4f, 0.05f, 10, 0.4f, false);
        b.Cyl(Sl.Metal, new Vector3(0, 0.7f, 0), 0.4f, 0.05f, 10, 0.4f, false);
    }

    public static void Crate(GeoBag b, int seed)
    {
        b.Box(Sl.Wood, new Vector3(0, 0.3f, 0), new Vector3(0.62f, 0.6f, 0.62f), R01(seed, 1, 211) * 40f);
        b.Box(Sl.Wood, new Vector3(0.05f, 0.9f, 0f), new Vector3(0.5f, 0.5f, 0.5f), R01(seed, 2, 211) * 40f);
    }

    public static void Cart(GeoBag b, int seed, float ruin)
    {
        int ws = ruin > 0.1f ? Weathered : Sl.Wood;
        b.Box(ws, new Vector3(0, 0.95f, 0), new Vector3(1.7f, 0.14f, 3f));
        b.Box(ws, new Vector3(-0.8f, 1.25f, 0), new Vector3(0.1f, 0.5f, 3f));
        b.Box(ws, new Vector3(0.8f, 1.25f, ruin > 0.5f ? 0.6f : 0f), new Vector3(0.1f, 0.5f, ruin > 0.5f ? 1.6f : 3f));
        for (int sx = -1; sx <= 1; sx += 2)
        {
            if (ruin > 0.5f && sx > 0) continue;
            for (int k = 0; k < 6; k++)
            {
                float a = k * 30f * Mathf.Deg2Rad;
                b.Beam(ws, new Vector3(sx * 0.95f, 0.62f - Mathf.Cos(a) * 0.62f, -Mathf.Sin(a) * 0.62f), new Vector3(sx * 0.95f, 0.62f + Mathf.Cos(a) * 0.62f, Mathf.Sin(a) * 0.62f), 0.07f, 0.1f, Vector3.right);
            }
        }
        b.Beam(ws, new Vector3(0, 0.9f, 1.4f), new Vector3(0, 0.6f, 3.2f), 0.12f, 0.12f, Vector3.right);
    }

    public static void CropRows(GeoBag b, int seed, float w, float d, int kind)
    {
        b.Box(Sl.Dirt, new Vector3(0, 0.05f, 0), new Vector3(w, 0.1f, d));
        int rows = Mathf.Max(2, Mathf.RoundToInt(w / 0.7f));
        for (int i = 0; i < rows; i++)
        {
            float x = -w / 2f + 0.35f + i * (w - 0.7f) / (rows - 1);
            int slot = kind == 0 ? Sl.Thatch : (kind == 1 ? LeafGreen : LeafYellow);
            float h = kind == 0 ? 0.9f : 0.35f;
            b.Box(slot, new Vector3(x, 0.1f + h / 2f, 0), new Vector3(0.38f, h, d - 0.4f));
        }
    }

    public static void Scarecrow(GeoBag b)
    {
        b.Box(Sl.Wood, new Vector3(0, 0.95f, 0), new Vector3(0.1f, 1.9f, 0.1f));
        b.Box(Sl.Wood, new Vector3(0, 1.45f, 0), new Vector3(1.5f, 0.09f, 0.09f));
        b.Box(Sl.Cloth, new Vector3(0, 1.2f, 0), new Vector3(0.55f, 0.7f, 0.25f));
        Blob(b, Sl.Thatch, new Vector3(0, 1.85f, 0), new Vector3(0.22f, 0.24f, 0.22f), 3, 0.1f, 4, 8, 0f);
        b.Cone(Sl.Wood, new Vector3(0, 2.0f, 0), 0.34f, 0.3f, 8);
    }

    public static void NoticeBoard(GeoBag b)
    {
        b.Box(Sl.Wood, new Vector3(-0.7f, 1.0f, 0), new Vector3(0.14f, 2.0f, 0.14f));
        b.Box(Sl.Wood, new Vector3(0.7f, 1.0f, 0), new Vector3(0.14f, 2.0f, 0.14f));
        b.Box(Sl.Wood, new Vector3(0, 1.5f, 0), new Vector3(1.6f, 0.9f, 0.08f));
        b.Box(Sl.Plaster, new Vector3(-0.3f, 1.55f, 0.06f), new Vector3(0.45f, 0.55f, 0.01f), 4f);
        b.Box(Sl.Plaster, new Vector3(0.3f, 1.5f, 0.06f), new Vector3(0.4f, 0.5f, 0.01f), -3f);
        b.Gable(Sl.Roof, Sl.Wood, Sl.Wood, new Vector3(0, 2.0f, 0), 1.9f, 0.7f, 0.4f, 0.1f, false);
    }

    // ------------------------------------------------------------------ bandit
    public static void BanditTent(GeoBag b, int seed) { Tent(b, seed, Sl.ClothB); }

    public static void Tent(GeoBag b, int seed, int cloth)
    {
        float w = 2.2f, d = 3.2f, h = 1.7f;
        b.QuadBoth(cloth, new Vector3(-w / 2f, 0f, d / 2f), new Vector3(-w / 2f, 0f, -d / 2f), new Vector3(0f, h, -d / 2f), new Vector3(0f, h, d / 2f));
        b.QuadBoth(cloth, new Vector3(w / 2f, 0f, -d / 2f), new Vector3(w / 2f, 0f, d / 2f), new Vector3(0f, h, d / 2f), new Vector3(0f, h, -d / 2f));
        b.Tri(Sl.Dark, new Vector3(w / 2f, 0f, d / 2f), new Vector3(-w / 2f, 0f, d / 2f), new Vector3(0f, h, d / 2f));
        b.Tri(Sl.Dark, new Vector3(-w / 2f, 0f, -d / 2f), new Vector3(w / 2f, 0f, -d / 2f), new Vector3(0f, h, -d / 2f));
        b.Beam(Sl.Wood, new Vector3(0, h + 0.1f, -d / 2f - 0.2f), new Vector3(0, h + 0.1f, d / 2f + 0.2f), 0.09f, 0.09f);
        b.Beam(Sl.Wood, new Vector3(0, -0.05f, d / 2f + 0.2f), new Vector3(0, h + 0.1f, d / 2f), 0.09f, 0.09f, Vector3.right);
        b.Beam(Sl.Wood, new Vector3(0, -0.05f, -d / 2f - 0.2f), new Vector3(0, h + 0.1f, -d / 2f), 0.09f, 0.09f, Vector3.right);
    }

    public static void Campfire(GeoBag b, int seed)
    {
        for (int i = 0; i < 9; i++)
        {
            float a = i * 40f * Mathf.Deg2Rad;
            Blob(b, Rock, new Vector3(Mathf.Sin(a) * 0.85f, 0.15f, Mathf.Cos(a) * 0.85f), new Vector3(0.28f, 0.2f, 0.26f), seed + i, 0.3f, 3, 7, 0.3f);
        }
        for (int i = 0; i < 4; i++) b.Beam(Sl.Wood, new Vector3(Mathf.Sin(i * 1.6f) * 0.6f, 0.1f, Mathf.Cos(i * 1.6f) * 0.6f), new Vector3(-Mathf.Sin(i * 1.6f) * 0.1f, 0.5f, -Mathf.Cos(i * 1.6f) * 0.1f), 0.14f, 0.14f);
        b.Cone(Fire, new Vector3(0, 0.25f, 0), 0.35f, 0.9f, 7);
        b.Cone(Sl.Glow, new Vector3(0, 0.25f, 0), 0.2f, 1.25f, 6);
    }

    public static void Barricade(GeoBag b, int seed, float len)
    {
        int n = Mathf.Max(3, Mathf.RoundToInt(len / 0.7f));
        for (int i = 0; i < n; i++)
        {
            float x = -len / 2f + len * i / (n - 1);
            float lean = (R01(seed, i, 221) - 0.5f) * 0.5f;
            b.Beam(Sl.Wood, new Vector3(x, -0.1f, 0.0f), new Vector3(x + lean * 0.5f, 1.7f + R01(seed, i, 222) * 0.5f, 0.35f + lean), 0.16f, 0.16f, Vector3.forward);
        }
        b.Beam(Sl.Wood, new Vector3(-len / 2f, 0.5f, 0.1f), new Vector3(len / 2f, 0.75f, 0.1f), 0.12f, 0.14f);
        b.Beam(Sl.Wood, new Vector3(-len / 2f, 1.1f, 0.1f), new Vector3(len / 2f, 1.0f, 0.1f), 0.12f, 0.14f);
    }

    public static void BannerPole(GeoBag b, int seed)
    {
        b.Cyl(Sl.Wood, Vector3.zero, 0.09f, 4.2f, 6, 0.07f, true);
        b.Beam(Sl.Wood, new Vector3(-0.05f, 3.9f, 0), new Vector3(1.0f, 3.9f, 0), 0.06f, 0.06f);
        b.QuadBoth(Sl.ClothB, new Vector3(0.0f, 3.85f, 0), new Vector3(0.95f, 3.85f, 0.05f), new Vector3(0.9f, 2.75f, 0.1f), new Vector3(0.0f, 2.8f, 0));
        Blob(b, Sl.Marble, new Vector3(0.48f, 3.4f, 0.07f), new Vector3(0.16f, 0.15f, 0.04f), seed, 0.05f, 3, 7, 0f);
    }

    /// <summary>Test harness: bangunan.</summary>
    public static void TestBuildImpl2(GeoBag b, string name, int seed)
    {
        switch (name)
        {
            case "cottage": House(b, seed, 5.6f, 4.6f, 2.5f, 0f, 0, true); break;
            case "cottageT": House(b, seed, 6.4f, 5f, 2.5f, 0f, 1, true); break;
            case "ruin": House(b, seed, 5.6f, 4.6f, 2.5f, 0.75f, 0, false); break;
            case "ruin2": House(b, seed, 6.4f, 5f, 2.5f, 1f, 1, false); break;
            case "barn": Barn(b, seed, 0f); break;
            case "ruinbarn": Barn(b, seed, 0.8f); break;
            case "inn": Inn(b, seed); break;
            case "shrine": Shrine(b, seed, 0f); break;
            case "ruinshrine": Shrine(b, seed, 0.85f); break;
            case "tower": RuinTower(b, seed); break;
            case "windmill": WindmillBody(b); break;
            case "blades": WindmillBlades(b); break;
            case "well": Well(b, seed, 0f); break;
            case "ruinwell": Well(b, seed, 0.9f); break;
            case "stall": Stall(b, seed); break;
            case "fence": Fence(b, seed, 4f, 0f); break;
            case "lamp": Lamp(b); break;
            case "hay": HayBale(b, seed); break;
            case "barrel": Barrel(b); break;
            case "cart": Cart(b, seed, 0f); break;
            case "ruincart": Cart(b, seed, 0.8f); break;
            case "crops": CropRows(b, seed, 5f, 4f, 0); break;
            case "scarecrow": Scarecrow(b); break;
            case "board": NoticeBoard(b); break;
            case "tent": BanditTent(b, seed); break;
            case "fire": Campfire(b, seed); break;
            case "barricade": Barricade(b, seed, 5f); break;
            case "banner": BannerPole(b, seed); break;
        }
    }
}
