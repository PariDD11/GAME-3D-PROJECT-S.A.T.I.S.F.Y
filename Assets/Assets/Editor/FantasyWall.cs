using System.Collections.Generic;
using UnityEngine;

public static partial class FantasyKit
{
    /// <summary>
    /// Dinding bersudut membulat (bukan kotak kaku). dress: 0 = hanya sudut bulat, 1 = batu: lajur batu halus, kornis + konsol di bawah atap,
    /// penopang sudut, plus pita bawah. size.y = tinggi, alas di baseCenter.y.
    /// </summary>
    public static void Wall(GeoBag b, int slot, Vector3 baseCenter, Vector3 size, int dress = 0)
    {
        float w = size.x, h = size.y, d = size.z;
        float r = Mathf.Clamp(Mathf.Min(w, d) * 0.05f, 0.18f, 0.55f);
        int seg = 3;
        Vector3[] ringPts = new Vector3[4 * (seg + 1)];
        float[] cx = { w / 2f - r, w / 2f - r, -w / 2f + r, -w / 2f + r };
        float[] cz = { d / 2f - r, -d / 2f + r, -d / 2f + r, d / 2f - r };
        float[] a0 = { 0f, 90f, 180f, 270f };      // sudut mulai tiap sudut (searah jarum jam dilihat dari atas)
        int n = 0;
        for (int c = 0; c < 4; c++)
            for (int k = 0; k <= seg; k++)
            {
                float a = (a0[c] + 90f * k / seg) * Mathf.Deg2Rad;
                ringPts[n++] = new Vector3(cx[c] + Mathf.Sin(a) * r, 0f, cz[c] + Mathf.Cos(a) * r);
            }
        // dua cincin: bawah (sedikit lebih lebar) dan atas, agar normal menghadap keluar urutan dari bawah ke atas
        var rings = new List<Vector3[]>();
        for (int q = 0; q < 2; q++)
        {
            var ring = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                Vector3 p = ringPts[i];
                ring[i] = baseCenter + new Vector3(p.x, q * h, p.z);
            }
            rings.Add(ring);
        }
        b.Loft(slot, rings, false, true, false);
        if (dress <= 0) return;
        int acc = slot == Sl.Marble || slot == Sl.Stone ? Sl.DarkStone : Sl.Wood;
        // lajur batu halus (menonjol sangat tipis, di bawah bidang jendela)
        float step = 0.9f;
        for (float y = step; y < h - 0.6f; y += step)
        {
            float yy = baseCenter.y + y;
            b.Box(acc, baseCenter + new Vector3(0f, y, d / 2f), new Vector3(w - 2f * r, 0.06f, 0.03f));
            b.Box(acc, baseCenter + new Vector3(0f, y, -d / 2f), new Vector3(w - 2f * r, 0.06f, 0.03f));
            b.Box(acc, baseCenter + new Vector3(w / 2f, y, 0f), new Vector3(0.03f, 0.06f, d - 2f * r));
            b.Box(acc, baseCenter + new Vector3(-w / 2f, y, 0f), new Vector3(0.03f, 0.06f, d - 2f * r));
        }
        // kornis dan konsol di bawah tepi atas
        float ty = baseCenter.y + h;
        b.Box(acc, new Vector3(baseCenter.x, ty - 0.22f, baseCenter.z), new Vector3(w + 0.5f, 0.44f, d + 0.5f));
        int nc = Mathf.Max(4, Mathf.RoundToInt(w / 0.8f));
        for (int i = 0; i < nc; i++)
        {
            float x = -w / 2f + (i + 0.5f) * w / nc;
            b.Box(acc, new Vector3(baseCenter.x + x, ty - 0.62f, baseCenter.z + d / 2f + 0.1f), new Vector3(0.3f, 0.36f, 0.3f));
            b.Box(acc, new Vector3(baseCenter.x + x, ty - 0.62f, baseCenter.z - d / 2f - 0.1f), new Vector3(0.3f, 0.36f, 0.3f));
        }
        int nd = Mathf.Max(4, Mathf.RoundToInt(d / 0.8f));
        for (int i = 0; i < nd; i++)
        {
            float z = -d / 2f + (i + 0.5f) * d / nd;
            b.Box(acc, new Vector3(baseCenter.x + w / 2f + 0.1f, ty - 0.62f, baseCenter.z + z), new Vector3(0.3f, 0.36f, 0.3f));
            b.Box(acc, new Vector3(baseCenter.x - w / 2f - 0.1f, ty - 0.62f, baseCenter.z + z), new Vector3(0.3f, 0.36f, 0.3f));
        }
        // penopang sudut bertingkat (quoin) setinggi dinding
        if (h > 5f)
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                {
                    for (float y = 0.5f; y < h - 1.2f; y += 1.5f)
                        b.Box(Sl.PlasterB, baseCenter + new Vector3(sx * (w / 2f - 0.05f), y + 0.4f, sz * (d / 2f - 0.05f)), new Vector3(0.7f, 0.65f, 0.7f));
                }
    }
}

public static partial class FantasyKit
{
    /// <summary>
    /// Detail satu ruas tembok kota (sebelum/diluar kotak dasar): lajur batu halus dua sisi, konsol di bawah parapet,
    /// celah panah, tanaman rambat, semak di kaki, obor dan spanduk berkala. m = tengah ruas di tanah, outward = arah keluar kota (datar).
    /// </summary>
    public static void RampartDetail(GeoBag b, Vector3 m, float yaw, float th, float h, float len, Vector3 outward, int tier, int idx)
    {
        Vector3 xAxis = new Vector3(Mathf.Cos(yaw * Mathf.Deg2Rad), 0f, -Mathf.Sin(yaw * Mathf.Deg2Rad));
        float s = Vector3.Dot(xAxis, outward) >= 0f ? 1f : -1f;      // +1: sisi luar di +x lokal
        int acc = tier == 2 ? Sl.Marble : Sl.DarkStone;
        b.Push(m, yaw);
        // lajur batu (kedua sisi)
        for (float y = 2.6f; y < h - 3.4f; y += 1.0f)
        {
            b.Box(acc, P(s * (th / 2f + 0.012f), y, 0f), P(0.03f, 0.07f, len));
            b.Box(acc, P(-s * (th / 2f + 0.012f), y, 0f), P(0.03f, 0.07f, len));
        }
        // konsol (corbel) di bawah parapet sisi luar
        int nc = Mathf.Max(3, Mathf.RoundToInt(len / 0.9f));
        for (int i = 0; i < nc; i++)
        {
            float z = -len / 2f + (i + 0.5f) * len / nc;
            b.Box(acc, P(s * (th / 2f + 0.2f), h - 3.6f, z), P(0.4f, 0.5f, 0.38f));
            b.Box(acc, P(s * (th / 2f + 0.34f), h - 3.95f, z), P(0.22f, 0.3f, 0.3f));
        }
        // celah panah (sisi luar)
        if (idx % 2 == 0)
        {
            b.Box(Sl.DarkStone, P(s * (th / 2f + 0.02f), h * 0.58f, 0f), P(0.08f, 2.0f, 0.62f));
            b.Box(Sl.Metal, P(s * (th / 2f + 0.06f), h * 0.58f, 0f), P(0.05f, 1.55f, 0.2f));
            b.Box(Sl.Glow, P(s * (th / 2f + 0.065f), h * 0.58f - 0.2f, 0f), P(0.04f, 0.8f, 0.1f));
        }
        // obor dinding di sisi dalam
        if (idx % 3 == 1)
        {
            b.Push(P(-s * (th / 2f), 0f, 0f), -s * 90f);
            AnimeKit.HangLantern(b, P(0f, 4.6f, 0.0f), 0.55f);
            b.Pop();
        }
        // spanduk di sisi luar
        if (idx % 4 == 2)
        {
            b.Push(P(s * (th / 2f + 0.05f), 0f, 0f), s * 90f);
            BuildKit.Banner(b, P(0f, h - 5f, 0.02f), 1.5f, 6f, Sl.Cloth, true);
            b.Pop();
        }
        // tanaman rambat dan semak di kaki
        if (idx % 3 == 0)
        {
            b.Push(P(s * (th / 2f + 0.05f), 0f, 0f), s * 90f);
            AnimeKit.Ivy(b, P(0f, 2.0f, 0f), Mathf.Min(h - 5f, 8f), 2.2f, idx * 7);
            b.Pop();
        }
        if (idx % 2 == 1)
            AnimeKit.FlowerBush(b, P(s * (th / 2f + 1.7f), 0.1f, (idx % 4 - 1.5f) * len * 0.25f), 0.9f, idx % 4 == 1 ? Sl.Blossom : Sl.Gold);
        b.Pop();
    }
}
