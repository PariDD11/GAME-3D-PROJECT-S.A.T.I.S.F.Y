using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Komponen bangunan yang dipakai berulang (jendela, pintu, cerobong, bendera, lentera...).
/// Semua menghadap +Z lokal dari GeoBag yang sedang aktif (gunakan bag.Push untuk memutar).
/// </summary>
public static class BuildKit
{
    /// <summary>Jendela berbingkai kayu, kaca bersekat, ambang batu, dan opsional daun jendela + kotak bunga.</summary>
    public static void Window(GeoBag b, Vector3 c, float w, float h, bool shutters, bool flowers)
    {
        // jendela berbingkai lengkung menyala (isekai): c = tengah jendela
        Vector3 bs = c + new Vector3(0f, -h / 2f, 0f);
        ArchWindow(b, bs, w, h, Sl.WoodLight);
        b.Box(Sl.Stone, c + new Vector3(0f, -h / 2f - 0.2f, 0.14f), new Vector3(w + 0.8f, 0.18f, 0.4f));
        if (shutters)
        {
            float sw = w * 0.5f;
            b.Box(Sl.ClothB, c + new Vector3(-(w / 2f + sw / 2f + 0.3f), -0.05f, 0.12f), new Vector3(sw, h * 0.92f, 0.08f));
            b.Box(Sl.ClothB, c + new Vector3((w / 2f + sw / 2f + 0.3f), -0.05f, 0.12f), new Vector3(sw, h * 0.92f, 0.08f));
            b.Box(Sl.Wood, c + new Vector3(-(w / 2f + sw / 2f + 0.3f), -0.05f, 0.18f), new Vector3(0.05f, h * 0.8f, 0.03f));
            b.Box(Sl.Wood, c + new Vector3((w / 2f + sw / 2f + 0.3f), -0.05f, 0.18f), new Vector3(0.05f, h * 0.8f, 0.03f));
        }
        if (flowers) FantasyKit.FlowerBox(b, c + new Vector3(0f, -h / 2f - 0.15f, 0.1f), w + 0.5f);
    }

    /// <summary>Jendela lengkung (gereja, bangsawan). c = tengah bawah.</summary>
    public static void ArchWindow(GeoBag b, Vector3 c, float w, float h, int trimSlot)
    {
        float r = w / 2f;
        float hs = h - r;                       // tinggi bagian lurus
        float z = c.z + 0.08f;
        b.Quad(Sl.Glass, new Vector3(c.x - r, c.y, z), new Vector3(c.x + r, c.y, z), new Vector3(c.x + r, c.y + hs, z), new Vector3(c.x - r, c.y + hs, z));
        int n = 8;
        Vector3 cen = new Vector3(c.x, c.y + hs, z);
        for (int k = 0; k < n; k++)
        {
            float a0 = Mathf.PI - Mathf.PI * k / n, a1 = Mathf.PI - Mathf.PI * (k + 1) / n;
            Vector3 p0 = cen + new Vector3(Mathf.Cos(a0) * r, Mathf.Sin(a0) * r, 0f);
            Vector3 p1 = cen + new Vector3(Mathf.Cos(a1) * r, Mathf.Sin(a1) * r, 0f);
            b.Tri(Sl.Glass, cen, p1, p0);
            Vector3 q0 = cen + new Vector3(Mathf.Cos(a0) * (r + 0.28f), Mathf.Sin(a0) * (r + 0.28f), 0.04f);
            Vector3 q1 = cen + new Vector3(Mathf.Cos(a1) * (r + 0.28f), Mathf.Sin(a1) * (r + 0.28f), 0.04f);
            b.Beam(trimSlot, (p0 + q0) / 2f, (p1 + q1) / 2f, 0.3f, 0.24f, Vector3.forward);
        }
        b.Box(trimSlot, new Vector3(c.x - r - 0.14f, c.y + hs / 2f, z), new Vector3(0.28f, hs, 0.24f));
        b.Box(trimSlot, new Vector3(c.x + r + 0.14f, c.y + hs / 2f, z), new Vector3(0.28f, hs, 0.24f));
        b.Box(Sl.Wood, new Vector3(c.x, c.y + hs / 2f, z + 0.05f), new Vector3(0.08f, hs + r, 0.06f));
        b.Box(Sl.Wood, new Vector3(c.x, c.y + hs * 0.55f, z + 0.05f), new Vector3(w, 0.08f, 0.06f));
        b.Box(trimSlot, new Vector3(c.x, c.y - 0.12f, z + 0.06f), new Vector3(w + 0.8f, 0.2f, 0.4f));
    }

    /// <summary>Celah panah sempit (menara, tembok).</summary>
    public static void Slit(GeoBag b, Vector3 c, float h)
    {
        b.Box(Sl.Glass, c, new Vector3(0.28f, h, 0.1f));
        b.Box(Sl.DarkStone, c + new Vector3(0f, 0f, -0.02f), new Vector3(0.7f, h + 0.5f, 0.08f));
    }

    /// <summary>Pintu rumah kayu berbingkai. c = tengah bawah pintu.</summary>
    public static void DoorFront(GeoBag b, Vector3 c, float w, float h, bool arch)
    {
        float r = w / 2f, hs = Mathf.Max(0.5f, h - r);
        b.Box(Sl.Stone, c + new Vector3(-r - 0.24f, hs / 2f + 0.1f, 0.06f), new Vector3(0.48f, hs + 0.2f, 0.26f));
        b.Box(Sl.Stone, c + new Vector3(r + 0.24f, hs / 2f + 0.1f, 0.06f), new Vector3(0.48f, hs + 0.2f, 0.26f));
        int n = 10;
        Vector3 cen = c + new Vector3(0f, hs, 0.12f);
        for (int kk = 0; kk < n; kk++)
        {
            float a0 = Mathf.PI - Mathf.PI * kk / n, a1 = Mathf.PI - Mathf.PI * (kk + 1) / n;
            Vector3 p0 = cen + new Vector3(Mathf.Cos(a0) * r, Mathf.Sin(a0) * r, 0f), p1 = cen + new Vector3(Mathf.Cos(a1) * r, Mathf.Sin(a1) * r, 0f);
            b.Tri(Sl.Wood, cen, p1, p0);
            Vector3 q0 = cen + new Vector3(Mathf.Cos(a0) * (r + 0.48f), Mathf.Sin(a0) * (r + 0.48f), -0.04f), q1 = cen + new Vector3(Mathf.Cos(a1) * (r + 0.48f), Mathf.Sin(a1) * (r + 0.48f), -0.04f);
            b.Beam(Sl.Stone, (p0 + q0) / 2f, (p1 + q1) / 2f, 0.5f, 0.28f, Vector3.forward);
        }
        b.Box(Sl.Wood, c + new Vector3(0f, hs / 2f, 0.12f), new Vector3(w, hs, 0.12f));
        int pl = Mathf.Max(3, Mathf.RoundToInt(w / 0.45f));
        for (int i2 = 1; i2 < pl; i2++) b.Box(Sl.Wood, c + new Vector3(-r + i2 * w / pl, hs / 2f, 0.19f), new Vector3(0.05f, hs, 0.04f));
        b.Box(Sl.Metal, c + new Vector3(0f, hs * 0.25f, 0.22f), new Vector3(w, 0.1f, 0.05f));
        b.Box(Sl.Metal, c + new Vector3(0f, hs * 0.75f, 0.22f), new Vector3(w, 0.1f, 0.05f));
        b.Ball(Sl.Gold, c + new Vector3(r * 0.55f, hs * 0.5f, 0.24f), 0.09f, 3, 6);
        b.Box(Sl.Stone, c + new Vector3(0f, 0.09f, 0.45f), new Vector3(w + 1f, 0.18f, 0.9f));
        if (arch) b.Box(Sl.Gold, c + new Vector3(0f, h + 0.55f, 0.1f), new Vector3(0.3f, 0.3f, 0.14f));
    }

    public static void Chimney(GeoBag b, Vector3 baseC, float w, float h)
    {
        FantasyKit.RoundChimney(b, baseC, w * 0.5f, h);
    }

    /// <summary>Lukarne (jendela atap) menghadap +Z. baseC = dasar di permukaan atap.</summary>
    public static void Dormer(GeoBag b, Vector3 baseC, float w, float h, int roofSlot, int wallSlot)
    {
        b.BoxB(wallSlot, baseC, new Vector3(w, h, w * 0.8f));
        b.Gable(roofSlot, wallSlot, Sl.Wood, baseC + new Vector3(0f, h, 0f), w, w * 0.8f, w * 0.5f, 0.25f, false);
        Window(b, baseC + new Vector3(0f, h * 0.5f, w * 0.4f), w * 0.5f, h * 0.55f, false, false);
    }

    public static void Finial(GeoBag b, Vector3 baseC, float h, int slot)
    {
        b.Cyl(slot, baseC, 0.1f, h * 0.55f, 6, 0.05f, false);
        b.Ball(slot, baseC + new Vector3(0f, h * 0.55f, 0f), 0.2f, 4, 8);
        b.Cyl(slot, baseC + new Vector3(0f, h * 0.55f + 0.15f, 0f), 0.12f, h * 0.45f, 6, 0f, false);
    }

    public static void Lantern(GeoBag b, Vector3 c)
    {
        b.Box(Sl.Metal, c + new Vector3(0f, 0.32f, 0f), new Vector3(0.34f, 0.08f, 0.34f));
        b.Box(Sl.Glow, c, new Vector3(0.26f, 0.5f, 0.26f));
        b.Box(Sl.Metal, c + new Vector3(0f, -0.3f, 0f), new Vector3(0.3f, 0.08f, 0.3f));
        b.Cone(Sl.Metal, c + new Vector3(0f, 0.36f, 0f), 0.24f, 0.22f, 4);
    }

    /// <summary>Spanduk vertikal berekor walet, digantung dari batang emas. top = titik tengah sisi atas, menghadap +Z.</summary>
    public static void Banner(GeoBag b, Vector3 top, float w, float h, int clothSlot, bool emblem)
    {
        b.Box(Sl.Gold, top + new Vector3(0f, 0.08f, 0.05f), new Vector3(w + 0.4f, 0.12f, 0.12f));
        Vector3 tl = top + new Vector3(-w / 2f, 0f, 0f), tr = top + new Vector3(w / 2f, 0f, 0f);
        Vector3 bl = tl + new Vector3(0f, -h, 0f), br = tr + new Vector3(0f, -h, 0f);
        Vector3 bm = top + new Vector3(0f, -h + w * 0.35f, 0f);
        b.QuadBoth(clothSlot, bl, br, tr, tl);
        b.Tri(clothSlot, bl, bm, br);
        b.Tri(clothSlot, br, bm, bl);
        if (emblem)
        {
            b.Box(Sl.Gold, top + new Vector3(0f, -h * 0.4f, 0.03f), new Vector3(w * 0.5f, w * 0.5f, 0.05f), 0f);
            b.Box(clothSlot, top + new Vector3(0f, -h * 0.4f, 0.07f), new Vector3(w * 0.32f, w * 0.32f, 0.05f), 0f);
        }
    }

    /// <summary>Bendera pada tiang. Berkibar ke arah +X lokal.</summary>
    public static void Flag(GeoBag b, Vector3 baseC, float poleH, float flagW, float flagH, int clothSlot)
    {
        b.Cyl(Sl.Metal, baseC, 0.07f, poleH, 6, 0.05f, true);
        b.Ball(Sl.Gold, baseC + new Vector3(0f, poleH + 0.05f, 0f), 0.14f, 4, 6);
        Vector3 p = baseC + new Vector3(0f, poleH - 0.2f, 0f);
        b.QuadBoth(clothSlot, p + new Vector3(0.05f, -flagH, 0f), p + new Vector3(flagW, -flagH * 0.9f, 0.18f), p + new Vector3(flagW, -0.1f, 0.1f), p + new Vector3(0.05f, 0f, 0f));
    }

    /// <summary>Menara bulat berkonsep anime: dasar batu, cincin, panggung menjorok, atap kerucut bertingkat dengan pucuk emas.</summary>
    public static void RoundTower(GeoBag b, Vector3 baseC, float r, float h, int shaftSlot, int bandSlot, int roofSlot, float roofH, bool crenel, bool flag, int flagSlot)
    {
        int n = 16;
        b.Cyl(bandSlot, baseC, r + 0.55f, 1.4f, n, r + 0.2f, true);
        b.Cyl(shaftSlot, baseC + new Vector3(0f, 1.4f, 0f), r, h - 1.4f, n, r, false);
        for (int i = 1; i <= 2; i++)
            b.Cyl(bandSlot, baseC + new Vector3(0f, h * i / 3f, 0f), r + 0.18f, 0.5f, n, r + 0.18f, true);
        // lajur batu bata melingkar + celah panah
        for (float yy = 2.0f; yy < h - 0.6f; yy += 0.95f)
            b.Cyl(shaftSlot == Sl.Stone ? Sl.DarkStone : Sl.Stone, baseC + new Vector3(0f, yy, 0f), r + 0.045f, 0.11f, n, r + 0.045f, true);
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            for (float yy = 3.2f; yy < h - 2.5f; yy += 3.4f)
            {
                Vector3 sp = baseC + new Vector3(Mathf.Sin(a) * (r + 0.05f), yy, Mathf.Cos(a) * (r + 0.05f));
                b.Box(Sl.Glow, sp, new Vector3(0.22f, 0.9f, 0.1f), a * Mathf.Rad2Deg);
                b.Box(Sl.DarkStone, sp, new Vector3(0.46f, 1.15f, 0.07f), a * Mathf.Rad2Deg);
            }
        }
        // panggung menjorok
        float py = baseC.y + h;
        b.Cyl(bandSlot, new Vector3(baseC.x, py - 0.1f, baseC.z), r, 1.3f, n, r + 1.0f, true);
        b.Cyl(shaftSlot, new Vector3(baseC.x, py + 1.2f, baseC.z), r + 1.0f, 1.0f, n, r + 1.0f, true);
        if (crenel)
        {
            int m = 12;
            for (int i = 0; i < m; i++)
            {
                float a = i / (float)m * Mathf.PI * 2f;
                Vector3 pp = new Vector3(baseC.x + Mathf.Sin(a) * (r + 0.8f), py + 2.2f + 0.6f, baseC.z + Mathf.Cos(a) * (r + 0.8f));
                b.Box(shaftSlot, pp, new Vector3(1.1f, 1.2f, 0.7f), a * Mathf.Rad2Deg);
            }
        }
        else
        {
            // atap kerucut bertingkat
            Vector3 rb = new Vector3(baseC.x, py + 2.2f, baseC.z);
            b.Cyl(roofSlot, rb, r + 1.9f, 0.5f, n, r + 1.3f, false);
            b.Spire(roofSlot, rb + new Vector3(0f, 0.4f, 0f), r + 1.3f, roofH, n, 0.0f);
            Finial(b, rb + new Vector3(0f, 0.4f + roofH - 0.1f, 0f), 2.2f, Sl.Gold);
            if (flag) Flag(b, rb + new Vector3(0f, 0.4f + roofH + 1.9f, 0f), 3.5f, 2.6f, 1.5f, flagSlot);
        }
        // celah panah
        for (int k = 0; k < 4; k++)
        {
            float a = k * Mathf.PI / 2f + 0.35f;
            for (int lv = 0; lv < 2; lv++)
            {
                float yy = baseC.y + h * (0.28f + 0.3f * lv);
                Vector3 pp = new Vector3(baseC.x + Mathf.Sin(a) * (r + 0.02f), yy, baseC.z + Mathf.Cos(a) * (r + 0.02f));
                b.Push(pp, a * Mathf.Rad2Deg);
                Slit(b, Vector3.zero, 2.2f);
                b.Pop();
            }
        }
    }

    /// <summary>Pilar bulat dengan kapital dan alas.</summary>
    public static void Column(GeoBag b, Vector3 baseC, float r, float h, int slot)
    {
        b.Cyl(slot, baseC, r * 1.5f, 0.35f, 10, r * 1.4f, true);
        b.Cyl(slot, baseC + new Vector3(0f, 0.35f, 0f), r, h - 0.9f, 10, r * 0.9f, false);
        b.Cyl(slot, baseC + new Vector3(0f, h - 0.55f, 0f), r * 1.3f, 0.55f, 10, r * 1.6f, true);
        b.Box(slot, baseC + new Vector3(0f, h + 0.1f, 0f), new Vector3(r * 3.6f, 0.2f, r * 3.6f));
    }

    /// <summary>Bingkai kayu hias (setengah kayu) di muka dinding +Z: tiang sudut, balok lantai, dan tiang antar jendela. Dipakai dengan Push ke bidang dinding.</summary>
    public static void TimberFront(GeoBag b, float w, float y0, float y1, float zFace, bool braces)
    {
        float h = y1 - y0;
        float t = 0.2f;
        b.Box(Sl.Wood, new Vector3(0f, y0 + t / 2f, zFace + 0.05f), new Vector3(w + 0.1f, t, 0.18f));
        b.Box(Sl.Wood, new Vector3(0f, y1 - t / 2f, zFace + 0.05f), new Vector3(w + 0.1f, t, 0.18f));
        int posts = Mathf.Max(2, Mathf.RoundToInt(w / 2.2f) + 1);
        for (int i = 0; i < posts; i++)
        {
            float x = -w / 2f + i * w / (posts - 1);
            b.Box(Sl.Wood, new Vector3(x, (y0 + y1) / 2f, zFace + 0.05f), new Vector3(t, h, 0.18f));
        }
        if (braces)
        {
            float seg = w / (posts - 1);
            for (int i = 0; i < posts - 1; i += 2)
            {
                float x0 = -w / 2f + i * seg, x1 = x0 + seg;
                b.Beam(Sl.Wood, new Vector3(x0, y0 + t, zFace + 0.05f), new Vector3(x1, y1 - t, zFace + 0.05f), 0.16f, 0.18f, Vector3.forward);
            }
        }
    }

    /// <summary>Kotak simpel bertingkat untuk tangga masuk. Naik ke arah -Z lokal.</summary>
    public static void Steps(GeoBag b, Vector3 baseC, float w, int n, float rise, float run, int slot)
    {
        b.Stairs(slot, baseC, w, n, rise, run, 0f);
    }

    /// <summary>Balustrade pendek (pagar teras) sepanjang sumbu X pada z tetap.</summary>
    public static void Balustrade(GeoBag b, Vector3 a, float len, float h, int slot)
    {
        b.Box(slot, a + new Vector3(len / 2f, h, 0f), new Vector3(len, 0.2f, 0.34f));
        b.Box(slot, a + new Vector3(len / 2f, 0.1f, 0f), new Vector3(len, 0.2f, 0.4f));
        int n = Mathf.Max(2, Mathf.RoundToInt(len / 0.45f));
        for (int i = 0; i <= n; i++)
            b.Cyl(slot, a + new Vector3(i * len / n, 0.2f, 0f), 0.07f, h - 0.3f, 6, 0.06f, true);
    }

    public static void Barrel(GeoBag b, Vector3 baseC)
    {
        b.Cyl(Sl.Wood, baseC, 0.42f, 0.95f, 10, 0.42f, true);
        b.Cyl(Sl.Wood, baseC + new Vector3(0f, 0.28f, 0f), 0.5f, 0.38f, 10, 0.5f, false);
        b.Cyl(Sl.Metal, baseC + new Vector3(0f, 0.12f, 0f), 0.44f, 0.08f, 10, 0.44f, false);
        b.Cyl(Sl.Metal, baseC + new Vector3(0f, 0.78f, 0f), 0.44f, 0.08f, 10, 0.44f, false);
    }

    public static void Crate(GeoBag b, Vector3 baseC, float s, float yaw)
    {
        b.BoxB(Sl.WoodLight, baseC, new Vector3(s, s, s), yaw);
        b.BoxB(Sl.Wood, baseC + new Vector3(0f, 0f, 0f), new Vector3(s + 0.05f, 0.1f, s + 0.05f), yaw);
        b.BoxB(Sl.Wood, baseC + new Vector3(0f, s - 0.1f, 0f), new Vector3(s + 0.05f, 0.1f, s + 0.05f), yaw);
    }

    /// <summary>Lengkung atap berselang (kanopi bergaris) menghadap +Z. Menjorok ke depan dan miring ke bawah.</summary>
    public static void Awning(GeoBag b, Vector3 topC, float w, float depth, float drop, int slotA, int slotB)
    {
        int n = Mathf.Max(4, Mathf.RoundToInt(w / 0.7f));
        float sw = w / n;
        for (int i = 0; i < n; i++)
        {
            float x0 = topC.x - w / 2f + i * sw, x1 = x0 + sw;
            Vector3 a = new Vector3(x0, topC.y - drop, topC.z + depth);
            Vector3 bq = new Vector3(x1, topC.y - drop, topC.z + depth);
            Vector3 c = new Vector3(x1, topC.y, topC.z);
            Vector3 d = new Vector3(x0, topC.y, topC.z);
            b.QuadBoth(i % 2 == 0 ? slotA : slotB, a, bq, c, d);
        }
        b.Box(Sl.Wood, topC + new Vector3(0f, -drop, depth), new Vector3(w + 0.1f, 0.1f, 0.1f));
    }

    /// <summary>Papan toko tergantung pada braket besi, menghadap +Z lokal (kedua sisi).</summary>
    public static void HangingSign(GeoBag b, Vector3 wallPt, float len, int emblemSlot)
    {
        b.Box(Sl.Metal, wallPt + new Vector3(len / 2f, 0f, 0.05f), new Vector3(len, 0.08f, 0.08f));
        b.Box(Sl.Wood, wallPt + new Vector3(len / 2f, -0.7f, 0.05f), new Vector3(len * 0.8f, 0.9f, 0.1f));
        b.Box(emblemSlot, wallPt + new Vector3(len / 2f, -0.7f, 0.12f), new Vector3(0.42f, 0.42f, 0.05f));
        b.Box(Sl.Metal, wallPt + new Vector3(len * 0.25f, -0.25f, 0.05f), new Vector3(0.04f, 0.5f, 0.04f));
        b.Box(Sl.Metal, wallPt + new Vector3(len * 0.75f, -0.25f, 0.05f), new Vector3(0.04f, 0.5f, 0.04f));
    }

    /// <summary>TextMesh bawaan Unity. Teks terbaca dari sisi yang menghadap -Z objek ini; putar yaw 180 agar terbaca dari +Z.</summary>
    public static GameObject Text(Transform parent, string text, Vector3 localPos, float yaw, float charSize, Color color)
    {
        var go = new GameObject("Text_" + text);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 64;
        tm.characterSize = charSize;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
#if UNITY_2022_2_OR_NEWER
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
        tm.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        HunterGuild.TextMat.Apply(go.GetComponent<MeshRenderer>(), tm.font.material);
        return go;
    }
}
