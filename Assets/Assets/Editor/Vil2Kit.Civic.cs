using System.Collections.Generic;
using UnityEngine;

/// <summary>Bangunan khas Desa Pemula: penginapan, rumah kepala desa, kantor, tabib, pandai besi, lumbung padi, gereja, lumbung besar, kincir air.</summary>
public static partial class V2Kit
{
    public static void SignBoard(GeoBag b, float x, float y, float z, float w, float h)
    {
        b.Box(Sl.Wood, new Vector3(x, y, z), new Vector3(w + 0.14f, h + 0.14f, 0.08f));
        b.Box(Sl.WoodLight, new Vector3(x, y, z + 0.03f), new Vector3(w, h, 0.07f));
        b.Box(Sl.Gold, new Vector3(x, y + h * 0.5f + 0.07f, z + 0.02f), new Vector3(w + 0.14f, 0.05f, 0.1f));
    }

    // ------------------------------------------------------------------ penginapan "Pondok Kali Embun"
    public static float Inn(GeoBag b)
    {
        var s = new HSpec { saltY = 0.285f, w = 13f, d = 9f, floors = 2, wallH = 2.9f, rise = 4.3f, ov = 0.8f, wall = 0, roof = 1, seed = 301, windowsFront = 5, windowsSide = 2, chimney = 0, doorX = 0f, jetty = true, porch = false, lantern = true, woodpile = false };
        float h = House(b, s);
        // serambi lebar beratap sirap
        for (int sd = -1; sd <= 1; sd += 2) Post(b, Sl.Wood, new Vector3(sd * 2.4f, 0.2f, 4.5f + 2.2f), 2.7f, 0.12f);
        QUp(b, Sl.Roof, new Vector3(-3.1f, 2.95f, 4.5f + 2.5f), new Vector3(3.1f, 2.95f, 4.5f + 2.5f), new Vector3(3.1f, 3.5f, 4.5f - 0.2f), new Vector3(-3.1f, 3.5f, 4.5f - 0.2f));
        b.Box(Sl.Wood, new Vector3(0f, 2.82f, 4.5f + 2.2f), new Vector3(5.2f, 0.18f, 0.2f));
        b.Box(Sl.WoodLight, new Vector3(0f, 0.2f, 4.5f + 1.2f), new Vector3(5.6f, 0.14f, 2.6f));
        // tonggak ikat kuda + palung + bangku
        Post(b, Sl.Wood, new Vector3(5.2f, 0f, 5.6f), 1.2f, 0.09f); Post(b, Sl.Wood, new Vector3(6.4f, 0f, 5.6f), 1.2f, 0.09f);
        b.Beam(Sl.Wood, new Vector3(5.2f, 1.0f, 5.6f), new Vector3(6.4f, 1.0f, 5.6f), 0.1f, 0.1f);
        b.Box(Sl.Wood, new Vector3(-5.2f, 0.28f, 5.8f), new Vector3(1.8f, 0.4f, 0.6f)); b.Box(Sl.Glass, new Vector3(-5.2f, 0.43f, 5.8f), new Vector3(1.5f, 0.04f, 0.34f));
        // tonggak papan gantung di sudut kiri
        Post(b, Sl.Wood, new Vector3(-3.8f, 0f, 5.5f), 3.4f, 0.1f);
        b.Beam(Sl.Wood, new Vector3(-3.8f, 3.2f, 5.5f), new Vector3(-3.8f, 3.2f, 6.9f), 0.1f, 0.1f);
        b.Box(Sl.WoodLight, new Vector3(-3.8f, 2.6f, 6.8f), new Vector3(0.08f, 0.9f, 1.3f));
        b.Box(Sl.Gold, new Vector3(-3.8f, 2.6f, 6.8f), new Vector3(0.1f, 0.4f, 0.5f));       // lambang cangkir
        return h;
    }

    // ------------------------------------------------------------------ rumah kepala desa (rumah panjang)
    public static float Elder(GeoBag b)
    {
        var s = new HSpec { w = 14f, d = 6.6f, floors = 1, wallH = 3.0f, rise = 3.6f, ov = 0.9f, wall = 0, roof = 1, seed = 411, windowsFront = 4, windowsSide = 1, chimney = 0, doorX = 0f, porch = true, lantern = true, woodpile = true };
        float h = House(b, s);
        Chimney(b, new Vector3(5.2f, 2.4f, 0f), 0.9f, 4.4f);
        // tiang bendera di depan
        Post(b, Sl.Wood, new Vector3(4.2f, 0f, 4.4f), 5.8f, 0.1f);
        b.Box(Sl.ClothB, new Vector3(4.2f + 0.7f, 5.1f, 4.4f), new Vector3(1.3f, 0.9f, 0.04f));
        b.Box(Sl.Gold, new Vector3(4.2f + 0.04f, 5.62f, 4.4f), new Vector3(0.08f, 0.1f, 0.08f));
        // bangku di teras
        b.Box(Sl.WoodLight, new Vector3(-4.2f, 0.5f, 3.9f), new Vector3(2.2f, 0.1f, 0.5f)); b.Box(Sl.Wood, new Vector3(-4.9f, 0.25f, 3.9f), new Vector3(0.1f, 0.5f, 0.45f)); b.Box(Sl.Wood, new Vector3(-3.5f, 0.25f, 3.9f), new Vector3(0.1f, 0.5f, 0.45f));
        return h;
    }

    // ------------------------------------------------------------------ kantor properti
    public static float Office(GeoBag b)
    {
        var s = new HSpec { w = 9f, d = 7f, floors = 2, wallH = 2.8f, rise = 3.4f, ov = 0.8f, wall = 2, roof = 1, seed = 511, windowsFront = 3, windowsSide = 1, chimney = 1, doorX = 0f, porch = true, lantern = true, woodpile = false };
        float h = House(b, s);
        SignBoard(b, 0f, 3.75f, 3.5f + 0.55f, 4.0f, 0.8f);
        for (int sd = -1; sd <= 1; sd += 2) b.Beam(Sl.Metal, new Vector3(sd * 1.6f, 3.05f, 3.52f), new Vector3(sd * 1.6f, 3.6f, 3.99f), 0.05f, 0.05f);
        return h;
    }

    // ------------------------------------------------------------------ pondok tabib
    public static float Healer(GeoBag b)
    {
        var s = new HSpec { w = 6f, d = 5.6f, floors = 1, wallH = 2.5f, rise = 2.6f, ov = 0.75f, wall = 3, roof = 0, seed = 611, windowsFront = 2, windowsSide = 1, chimney = 1, doorX = 0.9f, porch = true, lantern = true, woodpile = false };
        float h = House(b, s);
        // rak jemuran ramuan di samping + salib hijau
        b.Beam(Sl.Wood, new Vector3(-3.6f, 1.9f, 1.2f), new Vector3(-3.6f, 1.9f, 3.8f), 0.07f, 0.07f);
        Post(b, Sl.Wood, new Vector3(-3.6f, 0f, 1.2f), 1.9f, 0.05f); Post(b, Sl.Wood, new Vector3(-3.6f, 0f, 3.8f), 1.9f, 0.05f);
        for (int k = 0; k < 7; k++)
        {
            float z = 1.4f + k * 0.38f;
            b.Beam(Sl.Wood, new Vector3(-3.6f, 1.9f, z), new Vector3(-3.6f, 1.45f, z), 0.015f, 0.015f);
            b.Ball((k % 3 == 0) ? Sl.Blossom : Sl.Hedge, new Vector3(-3.6f, 1.35f, z), 0.12f, 3, 5, 1.6f);
        }
        b.Box(Sl.Cloth, new Vector3(1.9f, 2.9f, 2.9f), new Vector3(0.08f, 0.8f, 0.24f)); b.Box(Sl.Cloth, new Vector3(1.9f, 2.9f, 2.9f), new Vector3(0.08f, 0.24f, 0.8f));
        // kuali ramuan di depan kanan
        b.Cyl(Sl.Metal, new Vector3(2.7f, 0.3f, 4.0f), 0.34f, 0.4f, 8, 0.4f, true);
        b.Disc(Sl.Leaf, new Vector3(2.7f, 0.7f, 4.0f), 0.3f, 8, 0.7f);
        return h;
    }

    // ------------------------------------------------------------------ pandai besi: rumah batu + bengkel terbuka di sisi kanan
    public static float Smithy(GeoBag b)
    {
        b.Push(new Vector3(-2f, 0f, 0f), 0f);
        var s = new HSpec { w = 6f, d = 6.2f, floors = 1, wallH = 3.0f, rise = 2.9f, ov = 0.6f, wall = 2, roof = 1, seed = 711, windowsFront = 1, windowsSide = 1, chimney = -1, doorX = -0.6f, porch = false, lantern = true, woodpile = false, shutters = true, flowers = false };
        House(b, s);
        b.Pop();
        // bengkel terbuka (x 1.2 .. 5), atap miring ke belakang
        float x0 = 1.3f, x1 = 4.9f, zf = 3.0f, zb = -3.0f;
        for (int k = 0; k < 2; k++) { Post(b, Sl.Wood, new Vector3(k == 0 ? x0 + 0.1f : x1, 0f, zf), 3.1f, 0.13f); }
        b.Box(Sl.Stone, new Vector3((x0 + x1) * 0.5f + 0.2f, 0.12f, 0f), new Vector3(x1 - x0 + 0.2f, 0.24f, zf - zb + 0.3f));
        b.Box(Sl.Stone, new Vector3(x1 + 0.1f, 1.55f, 0f), new Vector3(0.22f, 3.1f, zf - zb));                           // dinding samping kanan
        b.Box(Sl.Stone, new Vector3((x0 + x1) * 0.5f, 1.55f, zb - 0.1f), new Vector3(x1 - x0, 3.1f, 0.22f));             // dinding belakang
        QUp(b, Sl.Roof, new Vector3(x0 - 0.2f, 3.35f, zf + 0.7f), new Vector3(x1 + 0.45f, 3.35f, zf + 0.7f), new Vector3(x1 + 0.45f, 3.95f, zb - 0.4f), new Vector3(x0 - 0.2f, 3.95f, zb - 0.4f));
        QOut(b, Sl.Wood, new Vector3(x0 - 0.2f, 3.2f, zf + 0.7f), new Vector3(x1 + 0.45f, 3.2f, zf + 0.7f), new Vector3(x1 + 0.45f, 3.8f, zb - 0.4f), new Vector3(x0 - 0.2f, 3.8f, zb - 0.4f), new Vector3(0f, -1f, 0f));
        b.Beam(Sl.Wood, new Vector3(x0 - 0.1f, 3.15f, zf), new Vector3(x1 + 0.3f, 3.15f, zf), 0.2f, 0.2f);
        // tungku + cerobong lebar
        float fx = x1 - 1.0f;
        b.Box(Sl.DarkStone, new Vector3(fx, 0.55f, zb + 0.9f), new Vector3(1.9f, 1.1f, 1.4f));
        b.Box(Sl.Dark, new Vector3(fx, 1.12f, zb + 0.9f), new Vector3(1.5f, 0.05f, 1.0f));
        b.Box(Sl.Glow, new Vector3(fx, 1.16f, zb + 0.9f), new Vector3(1.0f, 0.07f, 0.6f));
        b.Box(Sl.Stone, new Vector3(fx, 2.4f, zb + 0.55f), new Vector3(1.5f, 2.6f, 0.7f));
        Chimney(b, new Vector3(fx, 3.4f, zb + 0.55f), 0.8f, 1.7f);
        // landasan + balok, bellow, tong air, rak alat
        b.Cyl(Sl.Wood, new Vector3(x0 + 0.9f, 0f, 0.4f), 0.34f, 0.72f, 8, 0.3f, true);
        b.Box(Sl.Metal, new Vector3(x0 + 0.9f, 0.82f, 0.4f), new Vector3(0.95f, 0.2f, 0.4f));
        b.Box(Sl.Metal, new Vector3(x0 + 0.52f, 0.78f, 0.4f), new Vector3(0.34f, 0.12f, 0.2f));
        b.Box(Sl.Metal, new Vector3(x0 + 0.9f, 0.96f, 0.4f), new Vector3(0.7f, 0.06f, 0.34f));
        b.Cyl(Sl.Wood, new Vector3(x0 + 0.5f, 0f, -1.5f), 0.4f, 0.8f, 8, 0.36f, true); b.Disc(Sl.Glass, new Vector3(x0 + 0.5f, 0.8f, -1.5f), 0.33f, 8, 0.74f);
        b.Box(Sl.WoodLight, new Vector3(fx - 1.3f, 0.45f, zb + 0.8f), new Vector3(0.5f, 0.9f, 0.7f)); b.Box(Sl.Cloth, new Vector3(fx - 1.3f, 0.95f, zb + 0.8f), new Vector3(0.45f, 0.1f, 0.6f));
        for (int k = 0; k < 5; k++) b.Beam(Sl.Metal, new Vector3(x0 + 0.5f + k * 0.5f, 2.6f, zb + 0.2f), new Vector3(x0 + 0.5f + k * 0.5f + 0.05f, 1.7f, zb + 0.2f), 0.05f, 0.05f);
        b.Beam(Sl.Wood, new Vector3(x0 + 0.3f, 2.7f, zb + 0.2f), new Vector3(x0 + 3.0f, 2.7f, zb + 0.2f), 0.12f, 0.1f);
        // papan nama bergantung
        b.Beam(Sl.Wood, new Vector3(x0 - 0.1f, 3.0f, zf + 0.5f), new Vector3(x0 - 0.1f, 3.0f, zf + 1.5f), 0.1f, 0.1f);
        b.Box(Sl.Metal, new Vector3(x0 - 0.1f, 2.3f, zf + 1.4f), new Vector3(0.08f, 0.9f, 0.8f));
        return 5.5f;
    }

    // ------------------------------------------------------------------ lumbung padi di atas batu penyangga
    public static float Granary(GeoBag b)
    {
        for (int ix = -1; ix <= 1; ix += 2)
            for (int iz = -1; iz <= 1; iz += 2)
            {
                b.Cyl(Sl.Stone, new Vector3(ix * 1.6f, 0f, iz * 1.6f), 0.28f, 0.7f, 8, 0.2f, true);
                b.Cyl(Sl.DarkStone, new Vector3(ix * 1.6f, 0.7f, iz * 1.6f), 0.5f, 0.12f, 8, 0.5f, true);
            }
        b.Box(Sl.Wood, new Vector3(0f, 0.9f, 0f), new Vector3(4.0f, 0.24f, 4.0f));
        for (int k = -1; k <= 1; k += 2) { b.Beam(Sl.Wood, new Vector3(-2f, 0.78f, k * 1.2f), new Vector3(2f, 0.78f, k * 1.2f), 0.18f, 0.2f); }
        b.Box(Sl.WoodLight, new Vector3(0f, 2.2f, 0f), new Vector3(3.6f, 2.4f, 3.6f));
        for (int k = 0; k <= 12; k++) { float x = -1.8f + k * 0.3f; b.Box(Sl.Wood, new Vector3(x, 2.2f, 1.82f), new Vector3(0.05f, 2.4f, 0.04f)); b.Box(Sl.Wood, new Vector3(x, 2.2f, -1.82f), new Vector3(0.05f, 2.4f, 0.04f)); b.Box(Sl.Wood, new Vector3(1.82f, 2.2f, x), new Vector3(0.04f, 2.4f, 0.05f)); b.Box(Sl.Wood, new Vector3(-1.82f, 2.2f, x), new Vector3(0.04f, 2.4f, 0.05f)); }
        b.Push(new Vector3(0f, 1.02f, 1.8f), 0f);
        b.Box(Sl.Wood, new Vector3(0f, 0.75f, 0.06f), new Vector3(1.0f, 1.5f, 0.1f)); b.Box(Sl.WoodLight, new Vector3(0f, 0.75f, 0.12f), new Vector3(0.84f, 1.36f, 0.05f));
        b.Pop();
        ShingleRoof(b, Sl.Roof, Sl.RoofB, Sl.Wood, Sl.WoodLight, new Vector3(0f, 3.4f, 0f), 3.6f, 3.6f, 1.7f, 0.65f, 0.55f, true, 7);
        b.Beam(Sl.Wood, new Vector3(-0.6f, 0.9f, 3.3f), new Vector3(-0.2f, 2.6f, 1.95f), 0.12f, 0.06f); b.Beam(Sl.Wood, new Vector3(0.0f, 0.9f, 3.3f), new Vector3(0.4f, 2.6f, 1.95f), 0.12f, 0.06f);
        for (int k = 0; k < 5; k++) b.Beam(Sl.Wood, new Vector3(-0.5f + k * 0.0f + 0.0f, 1.0f + k * 0.35f, 3.1f - k * 0.26f), new Vector3(0.25f, 1.0f + k * 0.35f, 3.1f - k * 0.26f), 0.04f, 0.04f);
        return 5.2f;
    }
}

public static partial class V2Kit
{
    public static void P_Inn(GeoBag b) { Inn(b); }
    public static void P_Elder(GeoBag b) { Elder(b); }
    public static void P_Office(GeoBag b) { Office(b); }
    public static void P_Healer(GeoBag b) { Healer(b); }
    public static void P_Smithy(GeoBag b) { Smithy(b); }
    public static void P_Granary(GeoBag b) { Granary(b); }
}
