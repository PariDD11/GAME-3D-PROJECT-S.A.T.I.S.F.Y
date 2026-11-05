using System.Collections.Generic;
using UnityEngine;

/// <summary>Perlengkapan Desa Pemula: kios pasar, sumur, jerami, gerobak, lentera, rambu, orang-orangan, nisan, batu pendiri, bangku, papan pengumuman,
/// pagar/tembok batu/semak (mengikuti titik dunia), serta alat latihan. Semua hanya memakai GeoBag/Sl/Vector/Mathf.</summary>
public static partial class V2Kit
{
    // ------------------------------------------------------------------ barang kecil
    public static void Crate(GeoBag b, Vector3 p, float s, float yaw)
    {
        b.Box(Sl.WoodLight, p + new Vector3(0f, s * 0.5f, 0f), new Vector3(s, s, s), yaw);
        b.Box(Sl.Wood, p + new Vector3(0f, s * 0.5f, 0f), new Vector3(s + 0.04f, 0.08f, s + 0.04f), yaw);
        b.Box(Sl.Wood, p + new Vector3(0f, s * 0.12f, 0f), new Vector3(s + 0.04f, 0.08f, s + 0.04f), yaw);
        b.Box(Sl.Wood, p + new Vector3(0f, s * 0.88f, 0f), new Vector3(s + 0.04f, 0.08f, s + 0.04f), yaw);
    }

    public static void Barrel(GeoBag b, Vector3 p)
    {
        b.Cyl(Sl.Wood, p, 0.4f, 0.95f, 10, 0.34f, true);
        b.Cyl(Sl.Wood, p + new Vector3(0f, 0.3f, 0f), 0.42f, 0.35f, 10, 0.42f, false);
        b.Cyl(Sl.Metal, p + new Vector3(0f, 0.18f, 0f), 0.41f, 0.05f, 10, 0.41f, false);
        b.Cyl(Sl.Metal, p + new Vector3(0f, 0.7f, 0f), 0.37f, 0.05f, 10, 0.37f, false);
    }

    public static void Sack(GeoBag b, Vector3 p, float s)
    {
        b.Ball(Sl.Thatch, p + new Vector3(0f, s * 0.42f, 0f), s * 0.42f, 4, 7, 1.15f);
        b.Ball(Sl.Thatch, p + new Vector3(0f, s * 0.9f, 0f), s * 0.2f, 3, 6, 0.8f);
    }

    public static void Bale(GeoBag b, Vector3 p, float yaw)
    {
        b.Box(Sl.Thatch, p + new Vector3(0f, 0.3f, 0f), new Vector3(1.0f, 0.6f, 0.55f), yaw);
        b.Box(Sl.Wood, p + new Vector3(0f, 0.3f, 0f), new Vector3(0.05f, 0.62f, 0.57f), yaw);
    }

    public static void Haystack(GeoBag b)
    {
        b.Cyl(Sl.Thatch, Vector3.zero, 1.5f, 1.2f, 10, 1.5f, false);
        b.Cyl(Sl.Thatch, new Vector3(0f, 1.2f, 0f), 1.5f, 1.8f, 10, 0.12f, true);
        b.Cyl(Sl.Wood, new Vector3(0f, 2.8f, 0f), 0.05f, 0.7f, 5, 0.04f, true);
        b.Cyl(Sl.WoodLight, new Vector3(0f, 0.9f, 0f), 1.53f, 0.08f, 10, 1.53f, false);
    }

    public static void Trough(GeoBag b)
    {
        b.Box(Sl.Wood, new Vector3(0f, 0.28f, 0f), new Vector3(2.0f, 0.55f, 0.6f));
        b.Box(Sl.Dark, new Vector3(0f, 0.5f, 0f), new Vector3(1.8f, 0.06f, 0.42f));
        for (int sx = -1; sx <= 1; sx += 2) b.Box(Sl.Wood, new Vector3(sx * 0.85f, 0.14f, 0f), new Vector3(0.12f, 0.28f, 0.7f));
    }

    // ------------------------------------------------------------------ kios pasar (v: 0 serba ada, 1 sayur-buah, 2 roti & tembikar). Muka +Z, ukuran 3.6 x 2.4
    public static void Stall(GeoBag b, int v)
    {
        float w = 3.4f, d = 2.2f;
        for (int sx = -1; sx <= 1; sx += 2)
        {
            Post(b, Sl.Wood, new Vector3(sx * w * 0.5f, 0f, d * 0.5f), 2.5f, 0.07f);
            Post(b, Sl.Wood, new Vector3(sx * w * 0.5f, 0f, -d * 0.5f), 2.9f, 0.07f);
        }
        // atap kain bergaris, miring ke depan
        int stripes = 8;
        for (int i = 0; i < stripes; i++)
        {
            float x0 = -w * 0.5f - 0.25f + i * (w + 0.5f) / stripes, x1 = x0 + (w + 0.5f) / stripes;
            QUp(b, (i % 2 == 0) ? Sl.Cloth : Sl.PlasterB, new Vector3(x0, 2.5f, d * 0.5f + 0.4f), new Vector3(x1, 2.5f, d * 0.5f + 0.4f), new Vector3(x1, 2.95f, -d * 0.5f - 0.2f), new Vector3(x0, 2.95f, -d * 0.5f - 0.2f));
        }
        for (int i = 0; i < 8; i++)
        {
            float x0 = -w * 0.5f - 0.25f + i * (w + 0.5f) / 8f, x1 = x0 + (w + 0.5f) / 8f;
            b.Tri((i % 2 == 0) ? Sl.Cloth : Sl.PlasterB, new Vector3(x0, 2.5f, d * 0.5f + 0.4f), new Vector3(x1, 2.5f, d * 0.5f + 0.4f), new Vector3((x0 + x1) * 0.5f, 2.2f, d * 0.5f + 0.43f));
        }
        // meja jual + rak belakang
        b.Box(Sl.Wood, new Vector3(0f, 0.85f, d * 0.5f - 0.35f), new Vector3(w - 0.2f, 0.12f, 0.8f));
        b.Box(Sl.WoodLight, new Vector3(0f, 0.42f, d * 0.5f - 0.35f), new Vector3(w - 0.4f, 0.84f, 0.7f));
        b.Box(Sl.Cloth, new Vector3(0f, 0.92f, d * 0.5f - 0.3f), new Vector3(w - 0.3f, 0.04f, 0.9f), 0f);
        b.Box(Sl.Wood, new Vector3(0f, 1.2f, -d * 0.5f + 0.15f), new Vector3(w - 0.2f, 0.08f, 0.45f));
        b.Box(Sl.Wood, new Vector3(0f, 1.75f, -d * 0.5f + 0.15f), new Vector3(w - 0.2f, 0.08f, 0.45f));
        // dagangan
        if (v == 1)
        {
            int[] cs = { Sl.Leaf, Sl.Cloth, Sl.Gold, Sl.Blossom, Sl.Leaf, Sl.Gold };
            for (int k = 0; k < 6; k++)
            {
                float x = -1.3f + k * 0.52f;
                b.Box(Sl.Wood, new Vector3(x, 1.0f, d * 0.5f - 0.35f), new Vector3(0.44f, 0.14f, 0.5f));
                for (int j = 0; j < 4; j++) b.Ball(cs[(k + j) % 6], new Vector3(x + (j % 2 - 0.5f) * 0.18f, 1.14f + (j / 2) * 0.1f, d * 0.5f - 0.35f + (j % 3 - 1) * 0.1f), 0.1f, 3, 6);
            }
            Crate(b, new Vector3(-1.5f, 0f, -0.1f), 0.6f, 12f); Crate(b, new Vector3(1.4f, 0f, -0.2f), 0.55f, -8f);
        }
        else if (v == 2)
        {
            for (int k = 0; k < 5; k++)
            {
                float x = -1.2f + k * 0.55f;
                b.Ball(Sl.Thatch, new Vector3(x, 1.0f, d * 0.5f - 0.4f), 0.16f, 3, 6, 0.7f);
                b.Cyl(Sl.WoodLight, new Vector3(x - 0.1f, 1.2f, -d * 0.5f + 0.15f), 0.14f, 0.3f, 8, 0.1f, false);
                b.Ball(Sl.Stone, new Vector3(x + 0.1f, 1.95f, -d * 0.5f + 0.15f), 0.17f, 3, 6, 1.1f);
            }
            for (int k = 0; k < 3; k++) b.Cyl(Sl.WoodLight, new Vector3(-1.4f + k * 1.4f, 0f, -0.3f), 0.28f, 0.55f, 8, 0.22f, false);
        }
        else
        {
            for (int k = 0; k < 4; k++) b.Box(Sl.Plaster, new Vector3(-1.2f + k * 0.8f, 1.02f, d * 0.5f - 0.35f), new Vector3(0.4f, 0.2f, 0.4f), k * 11f);
            for (int k = 0; k < 4; k++) { b.Cyl(Sl.Stone, new Vector3(-1.3f + k * 0.7f, 1.28f, -d * 0.5f + 0.15f), 0.12f, 0.26f, 8, 0.09f, false); b.Ball(Sl.Leaf, new Vector3(-1.3f + k * 0.7f, 1.84f, -d * 0.5f + 0.15f), 0.13f, 3, 6); }
            Barrel(b, new Vector3(-1.5f, 0f, -0.2f)); Sack(b, new Vector3(1.4f, 0f, -0.3f), 0.7f); Sack(b, new Vector3(1.0f, 0f, -0.45f), 0.6f);
        }
        b.Box(Sl.WoodLight, new Vector3(0f, 2.3f, d * 0.5f + 0.04f), new Vector3(1.4f, 0.32f, 0.05f));            // papan nama kecil
    }

    // ------------------------------------------------------------------ sumur desa
    public static void Well(GeoBag b)
    {
        b.Cyl(Sl.Stone, Vector3.zero, 1.25f, 1.0f, 12, 1.12f, true);
        b.Cyl(Sl.DarkStone, new Vector3(0f, 0.95f, 0f), 1.32f, 0.12f, 12, 1.32f, true);
        b.Disc(Sl.Dark, new Vector3(0f, 0f, 0f), 0.98f, 12, 1.0f);
        for (int sx = -1; sx <= 1; sx += 2) Post(b, Sl.Wood, new Vector3(sx * 1.2f, 0.9f, 0f), 2.6f, 0.1f);
        b.Beam(Sl.Wood, new Vector3(-1.3f, 2.9f, 0f), new Vector3(1.3f, 2.9f, 0f), 0.18f, 0.18f);
        b.Push(new Vector3(0f, 3.0f, 0f), 90f);
        ShingleRoof(b, Sl.Roof, Sl.RoofB, Sl.Wood, Sl.Plaster, Vector3.zero, 2.2f, 3.2f, 1.1f, 0.35f, 0.3f, true, 9);
        b.Pop();
        b.Beam(Sl.Thatch, new Vector3(0f, 2.8f, 0f), new Vector3(0f, 1.7f, 0f), 0.03f, 0.03f);
        b.Cyl(Sl.Wood, new Vector3(0f, 1.2f, 0f), 0.2f, 0.5f, 8, 0.24f, true);
        b.Cyl(Sl.Metal, new Vector3(0f, 1.2f, 0f), 0.26f, 0.05f, 8, 0.26f, false);
        b.Beam(Sl.Wood, new Vector3(1.2f, 2.0f, 0f), new Vector3(1.55f, 2.0f, 0.5f), 0.08f, 0.08f);
    }

    // ------------------------------------------------------------------ gerobak tangan beratap jerami
    public static void Cart(GeoBag b)
    {
        b.Box(Sl.WoodLight, new Vector3(0f, 0.78f, 0f), new Vector3(2.4f, 0.12f, 1.3f));
        for (int sz = -1; sz <= 1; sz += 2) b.Box(Sl.Wood, new Vector3(0f, 1.1f, sz * 0.62f), new Vector3(2.4f, 0.55f, 0.08f));
        b.Box(Sl.Wood, new Vector3(-1.16f, 1.1f, 0f), new Vector3(0.08f, 0.55f, 1.3f));
        b.Beam(Sl.Wood, new Vector3(1.0f, 0.8f, -0.45f), new Vector3(3.2f, 0.55f, -0.45f), 0.12f, 0.1f);
        b.Beam(Sl.Wood, new Vector3(1.0f, 0.8f, 0.45f), new Vector3(3.2f, 0.55f, 0.45f), 0.12f, 0.1f);
        b.Beam(Sl.Wood, new Vector3(3.2f, 0.55f, -0.45f), new Vector3(3.2f, 0.55f, 0.45f), 0.1f, 0.1f);
        b.Beam(Sl.Wood, new Vector3(-0.2f, 0.74f, -0.6f), new Vector3(-0.2f, 0.74f, 0.6f), 0.12f, 0.12f);
        for (int sz = -1; sz <= 1; sz += 2)
        {
            int seg = 12;
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * 2f * Mathf.PI / seg, a1 = (i + 1) * 2f * Mathf.PI / seg;
                b.Beam(Sl.Wood, new Vector3(Mathf.Cos(a0) * 0.62f, 0.62f + Mathf.Sin(a0) * 0.62f, sz * 0.74f), new Vector3(Mathf.Cos(a1) * 0.62f, 0.62f + Mathf.Sin(a1) * 0.62f, sz * 0.74f), 0.12f, 0.08f, new Vector3(0f, 0f, 1f));
                if (i % 2 == 0) b.Beam(Sl.Wood, new Vector3(0f, 0.62f, sz * 0.74f), new Vector3(Mathf.Cos(a0) * 0.62f, 0.62f + Mathf.Sin(a0) * 0.62f, sz * 0.74f), 0.07f, 0.06f, new Vector3(0f, 0f, 1f));
            }
            b.Cyl(Sl.Metal, new Vector3(0f, 0.62f, sz * 0.74f - 0.05f), 0.1f, 0.1f, 6, 0.1f, true);
        }
        b.Beam(Sl.Metal, new Vector3(0f, 0.62f, -0.8f), new Vector3(0f, 0.62f, 0.8f), 0.05f, 0.05f);
        // muatan jerami
        b.Ball(Sl.Thatch, new Vector3(0f, 1.35f, 0f), 0.9f, 4, 8, 0.55f);
        b.Ball(Sl.Thatch, new Vector3(-0.5f, 1.55f, 0.1f), 0.55f, 4, 7, 0.6f);
    }

    // ------------------------------------------------------------------ lentera tiang (nama objek "LampuDesa" supaya dapat halo)
    public static void Lantern(GeoBag b)
    {
        b.Cyl(Sl.Stone, Vector3.zero, 0.22f, 0.3f, 6, 0.17f, true);
        Post(b, Sl.Wood, new Vector3(0f, 0.3f, 0f), 2.9f, 0.075f);
        b.Beam(Sl.Wood, new Vector3(0f, 2.9f, 0f), new Vector3(0.55f, 3.1f, 0f), 0.08f, 0.08f);
        b.Beam(Sl.Wood, new Vector3(0f, 2.4f, 0f), new Vector3(0.4f, 3.0f, 0f), 0.06f, 0.06f);
        b.Box(Sl.Metal, new Vector3(0.55f, 3.12f, 0f), new Vector3(0.3f, 0.04f, 0.3f));
        b.Box(Sl.Glow, new Vector3(0.55f, 2.9f, 0f), new Vector3(0.2f, 0.3f, 0.2f));
        for (int sx = -1; sx <= 1; sx += 2) for (int sz = -1; sz <= 1; sz += 2) b.Box(Sl.Metal, new Vector3(0.55f + sx * 0.11f, 2.9f, sz * 0.11f), new Vector3(0.03f, 0.34f, 0.03f));
        b.Cone(Sl.Metal, new Vector3(0.55f, 3.14f, 0f), 0.24f, 0.22f, 4);
        b.Box(Sl.Metal, new Vector3(0.55f, 2.7f, 0f), new Vector3(0.26f, 0.04f, 0.26f));
    }

    // ------------------------------------------------------------------ tiang rambu: n papan panah (yawDeg tiap panah, tinggi y)
    public static void SignPost(GeoBag b, float[] yaws, float[] ys)
    {
        b.Cyl(Sl.Stone, Vector3.zero, 0.34f, 0.3f, 6, 0.28f, true);
        Post(b, Sl.Wood, new Vector3(0f, 0.3f, 0f), 3.3f, 0.1f);
        b.Ball(Sl.Gold, new Vector3(0f, 3.68f, 0f), 0.12f, 3, 6);
        for (int i = 0; i < yaws.Length; i++)
        {
            b.Push(new Vector3(0f, ys[i], 0f), yaws[i]);
            b.Box(Sl.WoodLight, new Vector3(0.95f, 0f, 0f), new Vector3(1.7f, 0.34f, 0.07f));
            b.Tri(Sl.WoodLight, new Vector3(1.8f, 0.17f, 0.035f), new Vector3(1.8f, -0.17f, 0.035f), new Vector3(2.15f, 0f, 0.035f));
            b.Tri(Sl.WoodLight, new Vector3(1.8f, -0.17f, -0.035f), new Vector3(1.8f, 0.17f, -0.035f), new Vector3(2.15f, 0f, -0.035f));
            b.Box(Sl.Wood, new Vector3(0.95f, 0.2f, 0f), new Vector3(1.74f, 0.04f, 0.1f));
            b.Box(Sl.Wood, new Vector3(0.95f, -0.2f, 0f), new Vector3(1.74f, 0.04f, 0.1f));
            b.Pop();
        }
    }

    // ------------------------------------------------------------------ orang-orangan sawah
    public static void Scarecrow(GeoBag b)
    {
        Post(b, Sl.Wood, Vector3.zero, 2.2f, 0.06f);
        b.Beam(Sl.Wood, new Vector3(-0.9f, 1.55f, 0f), new Vector3(0.9f, 1.5f, 0f), 0.07f, 0.07f);
        b.Box(Sl.ClothB, new Vector3(0f, 1.25f, 0f), new Vector3(0.55f, 0.8f, 0.3f));
        b.Box(Sl.Cloth, new Vector3(0f, 1.2f, 0f), new Vector3(0.58f, 0.06f, 0.33f));
        for (int k = -1; k <= 1; k += 2) { b.Box(Sl.ClothB, new Vector3(k * 0.55f, 1.5f, 0f), new Vector3(0.5f, 0.2f, 0.14f)); b.Ball(Sl.Thatch, new Vector3(k * 0.98f, 1.46f, 0f), 0.11f, 3, 6); b.Box(Sl.Thatch, new Vector3(k * 0.22f, 0.7f, 0f), new Vector3(0.1f, 0.5f, 0.08f)); }
        b.Ball(Sl.Thatch, new Vector3(0f, 1.95f, 0f), 0.26f, 4, 8);
        b.Cyl(Sl.Wood, new Vector3(0f, 2.06f, 0f), 0.46f, 0.05f, 10, 0.46f, true);
        b.Cyl(Sl.Wood, new Vector3(0f, 2.1f, 0f), 0.22f, 0.36f, 10, 0.16f, true);
        b.Box(Sl.Cloth, new Vector3(0f, 2.13f, 0f), new Vector3(0.45f, 0.06f, 0.45f), 0f);
        b.Ball(Sl.Dark, new Vector3(-0.09f, 1.98f, 0.23f), 0.035f, 2, 4); b.Ball(Sl.Dark, new Vector3(0.09f, 1.98f, 0.23f), 0.035f, 2, 4);
    }

    // ------------------------------------------------------------------ nisan (v 0..4) di atas gundukan
    public static void Grave(GeoBag b, int v, int seed)
    {
        b.Box(Sl.Dirt, new Vector3(0f, 0.1f, 0.95f), new Vector3(1.0f, 0.2f, 1.8f));
        b.Box(Sl.Grass, new Vector3(0f, 0.2f, 0.95f), new Vector3(0.8f, 0.05f, 1.6f));
        switch (v % 5)
        {
            case 0:
                b.Box(Sl.Stone, new Vector3(0f, 0.55f, 0f), new Vector3(0.8f, 1.0f, 0.18f));
                DiscV(b, Sl.Stone, 0f, 1.05f, 0.4f, 0.09f, 10, 0f, 180f); DiscV(b, Sl.Stone, 0f, 1.05f, 0.4f, -0.09f, 10, 180f, 360f);
                b.Box(Sl.DarkStone, new Vector3(0f, 0.04f, 0f), new Vector3(1.0f, 0.1f, 0.36f));
                break;
            case 1:
                b.Box(Sl.Stone, new Vector3(0f, 0.7f, 0f), new Vector3(0.2f, 1.4f, 0.16f));
                b.Box(Sl.Stone, new Vector3(0f, 1.05f, 0f), new Vector3(0.78f, 0.2f, 0.16f));
                b.Box(Sl.DarkStone, new Vector3(0f, 0.08f, 0f), new Vector3(0.8f, 0.16f, 0.4f));
                break;
            case 2:
                b.Box(Sl.Stone, new Vector3(0f, 0.7f, 0f), new Vector3(0.6f, 1.4f, 0.2f));
                b.Tri(Sl.Stone, new Vector3(-0.3f, 1.4f, 0.1f), new Vector3(0.3f, 1.4f, 0.1f), new Vector3(0f, 1.8f, 0.1f));
                b.Tri(Sl.Stone, new Vector3(0.3f, 1.4f, -0.1f), new Vector3(-0.3f, 1.4f, -0.1f), new Vector3(0f, 1.8f, -0.1f));
                b.Box(Sl.DarkStone, new Vector3(0f, 0.06f, 0f), new Vector3(0.8f, 0.12f, 0.4f));
                break;
            case 3:
                b.Box(Sl.Stone, new Vector3(0f, 0.28f, 0.1f), new Vector3(0.7f, 0.56f, 0.14f), (seed % 7 - 3) * 2f);
                b.Box(Sl.Plaster, new Vector3(0f, 0.57f, 0.1f), new Vector3(0.74f, 0.04f, 0.18f), (seed % 7 - 3) * 2f);
                break;
            default:
                b.Box(Sl.DarkStone, new Vector3(0f, 0.45f, 0f), new Vector3(0.14f, 0.9f, 0.14f));
                b.Box(Sl.DarkStone, new Vector3(0f, 0.7f, 0f), new Vector3(0.5f, 0.12f, 0.14f));
                b.Ball(Sl.Stone, new Vector3(0f, 0.04f, 0.0f), 0.3f, 3, 6, 0.4f);
                break;
        }
        if (seed % 3 == 0) b.Ball(Sl.Blossom, new Vector3(0.28f, 0.28f, 0.7f), 0.14f, 3, 6);
    }

    // ------------------------------------------------------------------ batu pendiri
    public static void FoundersStone(GeoBag b)
    {
        b.Cyl(Sl.DarkStone, Vector3.zero, 1.5f, 0.3f, 10, 1.4f, true);
        b.Cyl(Sl.DarkStone, new Vector3(0f, 0.3f, 0f), 1.15f, 0.2f, 10, 1.05f, true);
        b.Cyl(Sl.Stone, new Vector3(0f, 0.5f, 0f), 0.8f, 2.6f, 5, 0.52f, true);
        b.Cone(Sl.Stone, new Vector3(0f, 3.1f, 0f), 0.52f, 0.5f, 5);
        b.Box(Sl.Metal, new Vector3(0f, 1.5f, 0.64f), new Vector3(1.0f, 0.75f, 0.05f));
        b.Box(Sl.Gold, new Vector3(0f, 1.5f, 0.67f), new Vector3(0.92f, 0.67f, 0.02f));
        for (int k = 0; k < 4; k++) b.Ball(k % 2 == 0 ? Sl.Blossom : Sl.Cloth, new Vector3(-0.9f + k * 0.6f, 0.52f, 1.12f - (k % 2) * 0.1f), 0.12f, 3, 6);
    }

    // ------------------------------------------------------------------ bangku taman dengan sandaran
    public static void Bench(GeoBag b)
    {
        b.Box(Sl.WoodLight, new Vector3(0f, 0.5f, 0f), new Vector3(1.8f, 0.08f, 0.5f));
        b.Box(Sl.WoodLight, new Vector3(0f, 0.95f, -0.22f), new Vector3(1.8f, 0.34f, 0.06f));
        for (int sx = -1; sx <= 1; sx += 2)
        {
            b.Box(Sl.Wood, new Vector3(sx * 0.78f, 0.25f, 0f), new Vector3(0.1f, 0.5f, 0.46f));
            b.Box(Sl.Wood, new Vector3(sx * 0.78f, 0.75f, -0.22f), new Vector3(0.1f, 0.8f, 0.08f));
        }
    }

    // ------------------------------------------------------------------ papan pengumuman beratap (muka baca +Z, teks dipasang oleh builder pada z = +0.12)
    public static void NoticeBoard(GeoBag b)
    {
        for (int sx = -1; sx <= 1; sx += 2) Post(b, Sl.Wood, new Vector3(sx * 1.45f, 0f, 0f), 3.0f, 0.12f);
        b.Box(Sl.Wood, new Vector3(0f, 1.95f, -0.02f), new Vector3(3.1f, 1.7f, 0.1f));
        b.Box(Sl.WoodLight, new Vector3(0f, 1.95f, 0.04f), new Vector3(2.8f, 1.4f, 0.06f));
        for (int sx = -1; sx <= 1; sx += 2) b.Box(Sl.Wood, new Vector3(sx * 1.45f, 1.95f, 0.03f), new Vector3(0.12f, 1.8f, 0.14f));
        b.Push(new Vector3(0f, 2.95f, 0f), 90f);
        ShingleRoof(b, Sl.Roof, Sl.RoofB, Sl.Wood, Sl.Plaster, Vector3.zero, 1.2f, 3.6f, 0.9f, 0.3f, 0.3f, true, 3);
        b.Pop();
        b.Box(Sl.Wood, new Vector3(0f, 0.14f, 0.5f), new Vector3(2.0f, 0.28f, 0.6f));
    }

    // ------------------------------------------------------------------ target panah jerami di rangka A (muka +Z)
    public static void ArcheryTarget(GeoBag b)
    {
        for (int sx = -1; sx <= 1; sx += 2)
        {
            b.Beam(Sl.Wood, new Vector3(sx * 0.9f, 0f, 0.5f), new Vector3(sx * 0.35f, 2.2f, 0f), 0.12f, 0.1f);
            b.Beam(Sl.Wood, new Vector3(sx * 0.9f, 0f, -0.5f), new Vector3(sx * 0.35f, 2.2f, 0f), 0.12f, 0.1f);
        }
        b.Beam(Sl.Wood, new Vector3(-0.4f, 2.1f, 0f), new Vector3(0.4f, 2.1f, 0f), 0.12f, 0.12f);
        Prism(b, Sl.Thatch, new Vector3(0f, 1.5f, -0.12f), new Vector3(0f, 1.5f, 0.2f), 0.95f, 0.95f, 14, true, true);
        DiscV(b, Sl.PlasterB, 0f, 1.5f, 0.9f, 0.21f, 14);
        DiscV(b, Sl.Cloth, 0f, 1.5f, 0.68f, 0.215f, 14);
        DiscV(b, Sl.PlasterB, 0f, 1.5f, 0.46f, 0.22f, 14);
        DiscV(b, Sl.ClothB, 0f, 1.5f, 0.28f, 0.225f, 14);
        DiscV(b, Sl.Gold, 0f, 1.5f, 0.12f, 0.23f, 12);
    }

    // ------------------------------------------------------------------ rak senjata latihan
    public static void WeaponRack(GeoBag b)
    {
        for (int sx = -1; sx <= 1; sx += 2) { b.Box(Sl.Wood, new Vector3(sx * 1.0f, 0.9f, 0f), new Vector3(0.12f, 1.8f, 0.4f)); b.Box(Sl.Wood, new Vector3(sx * 1.0f, 0.04f, 0f), new Vector3(0.14f, 0.08f, 0.9f)); }
        b.Box(Sl.Wood, new Vector3(0f, 1.0f, 0f), new Vector3(2.1f, 0.1f, 0.3f));
        b.Box(Sl.Wood, new Vector3(0f, 1.55f, 0f), new Vector3(2.1f, 0.1f, 0.3f));
        for (int k = 0; k < 5; k++)
        {
            float x = -0.8f + k * 0.4f;
            b.Beam(Sl.WoodLight, new Vector3(x, 0.15f, 0.06f), new Vector3(x + 0.03f, 1.75f, 0.06f), 0.07f, 0.05f, new Vector3(0f, 0f, 1f));
            b.Beam(Sl.Metal, new Vector3(x + 0.03f, 1.75f, 0.06f), new Vector3(x + 0.04f, 2.3f, 0.06f), 0.05f, 0.03f, new Vector3(0f, 0f, 1f));
        }
    }

    // ------------------------------------------------------------------ cincin batu rune (pelatihan sihir): menhir melingkar + garis cahaya (slot Glow = kristal)
    public static void RuneCircle(GeoBag b, float R)
    {
        b.Disc(Sl.Pave, Vector3.zero, R + 0.8f, 40, 0.04f);
        b.Annulus(Sl.Glow, Vector3.zero, R - 0.5f, R - 0.35f, 0f, 360f, 40, 0.07f);
        b.Annulus(Sl.Glow, Vector3.zero, R * 0.45f, R * 0.45f + 0.15f, 0f, 360f, 32, 0.07f);
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f * Mathf.Deg2Rad;
            b.Beam(Sl.Glow, new Vector3(Mathf.Sin(a) * R * 0.45f, 0.08f, Mathf.Cos(a) * R * 0.45f), new Vector3(Mathf.Sin(a) * (R - 0.5f), 0.08f, Mathf.Cos(a) * (R - 0.5f)), 0.14f, 0.03f);
        }
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad + 0.2f;
            float hgt = 2.2f + (i % 3) * 0.35f;
            Vector3 p = new Vector3(Mathf.Sin(a) * R, 0f, Mathf.Cos(a) * R);
            b.Cyl(Sl.DarkStone, p, 0.62f, hgt, 5, 0.38f, true);
            b.Box(Sl.Glow, p + new Vector3(-Mathf.Sin(a) * 0.38f, hgt * 0.55f, -Mathf.Cos(a) * 0.38f), new Vector3(0.1f, 0.7f, 0.1f), a * Mathf.Rad2Deg);
            b.Ball(Sl.Glow, p + new Vector3(0f, hgt + 0.35f, 0f), 0.14f, 3, 6, 1.4f);
        }
        b.Cyl(Sl.Stone, Vector3.zero, 1.2f, 0.7f, 10, 1.0f, true);
        b.Cyl(Sl.Marble, new Vector3(0f, 0.7f, 0f), 0.9f, 0.2f, 10, 0.9f, true);
    }

    // ------------------------------------------------------------------ pagar & tembok mengikuti titik dunia (y diambil dari titik)
    public static void RailFence(GeoBag b, IList<Vector3> pts, float h, int rails)
    {
        for (int i = 0; i + 1 < pts.Count; i++)
        {
            Vector3 a = pts[i], c = pts[i + 1];
            float len = Mathf.Sqrt((c.x - a.x) * (c.x - a.x) + (c.z - a.z) * (c.z - a.z));
            if (len < 0.05f) continue;
            int n = Mathf.Max(1, Mathf.RoundToInt(len / 2.0f));
            for (int k = 0; k <= n; k++)
            {
                if (k == n && i + 2 < pts.Count) break;
                Vector3 p = Vector3.Lerp(a, c, k / (float)n);
                Post(b, Sl.Wood, p + new Vector3(0f, -0.15f, 0f), h + 0.15f, 0.075f);
                b.Ball(Sl.WoodLight, p + new Vector3(0f, h + 0.02f, 0f), 0.085f, 2, 5);
            }
            for (int r = 0; r < rails; r++)
            {
                float f = (r + 1f) / (rails + 0.4f) * h;
                b.Beam(Sl.WoodLight, a + new Vector3(0f, f, 0f), c + new Vector3(0f, f, 0f), 0.07f, 0.1f, new Vector3(0f, 1f, 0f));
            }
        }
    }

    /// <summary>Pagar log runcing (palisade) dengan ikat rotan.</summary>
    public static void Palisade(GeoBag b, IList<Vector3> pts, float h)
    {
        for (int i = 0; i + 1 < pts.Count; i++)
        {
            Vector3 a = pts[i], c = pts[i + 1];
            float len = Mathf.Sqrt((c.x - a.x) * (c.x - a.x) + (c.z - a.z) * (c.z - a.z));
            if (len < 0.05f) continue;
            int n = Mathf.Max(1, Mathf.RoundToInt(len / 0.42f));
            for (int k = 0; k <= n; k++)
            {
                Vector3 p = Vector3.Lerp(a, c, k / (float)n);
                float hh = h + (Rnd(i * 31 + k, 4) - 0.5f) * 0.5f;
                b.Cyl(Sl.Wood, p + new Vector3(0f, -0.3f, 0f), 0.2f, hh + 0.3f - 0.4f, 6, 0.18f, false);
                b.Cone(Sl.WoodLight, p + new Vector3(0f, hh - 0.1f, 0f), 0.19f, 0.5f, 6);
            }
            for (int r = 0; r < 2; r++)
            {
                float f = h * (0.3f + 0.4f * r);
                b.Beam(Sl.Wood, a + new Vector3(0f, f, 0.2f), c + new Vector3(0f, f, 0.2f), 0.1f, 0.1f, new Vector3(0f, 1f, 0f));
            }
        }
    }

    /// <summary>Tembok batu kering setinggi h, tebal t.</summary>
    public static void DryWall(GeoBag b, IList<Vector3> pts, float h, float t, int seed)
    {
        int c = 0;
        for (int i = 0; i + 1 < pts.Count; i++)
        {
            Vector3 a = pts[i], d = pts[i + 1];
            float len = Mathf.Sqrt((d.x - a.x) * (d.x - a.x) + (d.z - a.z) * (d.z - a.z));
            if (len < 0.05f) continue;
            float yaw = Mathf.Atan2(d.x - a.x, d.z - a.z) * Mathf.Rad2Deg;
            int n = Mathf.Max(1, Mathf.RoundToInt(len / 0.75f));
            int courses = Mathf.Max(2, Mathf.RoundToInt(h / 0.32f));
            for (int k = 0; k < n; k++)
            {
                Vector3 p = Vector3.Lerp(a, d, (k + 0.5f) / n);
                for (int r = 0; r < courses; r++)
                {
                    c++;
                    float sz = 0.7f + 0.35f * Rnd(seed + c, 1);
                    float off = ((r % 2) * 0.35f) + (Rnd(seed + c, 2) - 0.5f) * 0.1f;
                    Vector3 dir = new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), 0f, Mathf.Cos(yaw * Mathf.Deg2Rad));
                    b.Box((c % 4 == 0) ? Sl.DarkStone : Sl.Stone, p + dir * off + new Vector3(0f, (r + 0.5f) * h / courses - 0.1f, 0f), new Vector3(t + (Rnd(seed + c, 3) - 0.5f) * 0.08f, h / courses * 0.98f, sz), yaw + (Rnd(seed + c, 4) - 0.5f) * 6f);
                }
            }
        }
        // batu penutup
        for (int i = 0; i + 1 < pts.Count; i++)
        {
            Vector3 a = pts[i], d = pts[i + 1];
            float len = Mathf.Sqrt((d.x - a.x) * (d.x - a.x) + (d.z - a.z) * (d.z - a.z));
            if (len < 0.05f) continue;
            b.Beam(Sl.DarkStone, a + new Vector3(0f, h - 0.1f, 0f), d + new Vector3(0f, h - 0.1f, 0f), t + 0.12f, 0.14f, new Vector3(0f, 1f, 0f));
        }
    }

    /// <summary>Pagar tanaman (semak) memanjang.</summary>
    public static void HedgeRun(GeoBag b, IList<Vector3> pts, float h, float w, int seed)
    {
        int c = 0;
        for (int i = 0; i + 1 < pts.Count; i++)
        {
            Vector3 a = pts[i], d = pts[i + 1];
            float len = Mathf.Sqrt((d.x - a.x) * (d.x - a.x) + (d.z - a.z) * (d.z - a.z));
            int n = Mathf.Max(1, Mathf.RoundToInt(len / (w * 0.9f)));
            for (int k = 0; k < n; k++)
            {
                c++;
                Vector3 p = Vector3.Lerp(a, d, (k + 0.5f) / n);
                float sc = 0.9f + 0.25f * Rnd(seed + c, 1);
                b.Ball((c % 5 == 0) ? Sl.Leaf : Sl.Hedge, p + new Vector3(0f, h * 0.45f, 0f), w * 0.6f * sc, 3, 7, h / (w * 1.2f));
            }
        }
    }

    // ------------------------------------------------------------------ bedeng sayur/bunga kecil
    public static void GardenBed(GeoBag b, float w, float d, int seed)
    {
        b.Box(Sl.Wood, new Vector3(0f, 0.16f, 0f), new Vector3(w, 0.32f, d));
        b.Box(Sl.Dirt, new Vector3(0f, 0.31f, 0f), new Vector3(w - 0.16f, 0.06f, d - 0.16f));
        int rows = Mathf.Max(2, (int)(d / 0.45f)), cols = Mathf.Max(3, (int)(w / 0.38f));
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                float x = -w * 0.5f + 0.28f + c * (w - 0.56f) / (cols - 1), z = -d * 0.5f + 0.28f + r * (d - 0.56f) / Mathf.Max(1, rows - 1);
                float k = Rnd(seed + r * 13 + c, 7);
                b.Ball(k < 0.15f ? Sl.Blossom : (k < 0.3f ? Sl.Gold : (k < 0.5f ? Sl.Leaf : Sl.Hedge)), new Vector3(x, 0.5f, z), 0.15f + 0.05f * k, 3, 6, 0.9f);
            }
    }

    public static void P_Props1(GeoBag b)
    {
        for (int v = 0; v < 3; v++) { b.Push(new Vector3(v * 5f, 0f, 0f), 0f); Stall(b, v); b.Pop(); }
        b.Push(new Vector3(-6f, 0f, 0f), 0f); Well(b); b.Pop();
        b.Push(new Vector3(18f, 0f, 0f), 0f); Cart(b); b.Pop();
        b.Push(new Vector3(24f, 0f, 0f), 0f); Haystack(b); b.Pop();
        b.Push(new Vector3(-12f, 0f, 0f), 0f); Lantern(b); b.Pop();
        b.Push(new Vector3(-15f, 0f, 0f), 0f); SignPost(b, new float[] { 20f, 110f, 200f }, new float[] { 3.0f, 2.55f, 2.1f }); b.Pop();
        b.Push(new Vector3(30f, 0f, 0f), 0f); Scarecrow(b); b.Pop();
        b.Push(new Vector3(33f, 0f, 0f), 0f); FoundersStone(b); b.Pop();
        b.Push(new Vector3(-6f, 0f, 6f), 0f); NoticeBoard(b); b.Pop();
        b.Push(new Vector3(0f, 0f, 6f), 0f); Bench(b); b.Pop();
        for (int g = 0; g < 5; g++) { b.Push(new Vector3(6f + g * 2.2f, 0f, 6f), 0f); Grave(b, g, g + 2); b.Pop(); }
        b.Push(new Vector3(20f, 0f, 6f), 0f); ArcheryTarget(b); b.Pop();
        b.Push(new Vector3(25f, 0f, 6f), 0f); WeaponRack(b); b.Pop();
        b.Push(new Vector3(30f, 0f, 6f), 0f); GardenBed(b, 3f, 1.6f, 4); b.Pop();
        b.Push(new Vector3(34f, 0f, 6f), 0f); Barrel(b, Vector3.zero); Crate(b, new Vector3(1.0f, 0f, 0f), 0.7f, 10f); Sack(b, new Vector3(2.0f, 0f, 0f), 0.7f); Bale(b, new Vector3(3.2f, 0f, 0f), 20f); b.Pop();
    }

    public static void P_Props2(GeoBag b)
    {
        var l = new List<Vector3>(); for (int i = 0; i <= 8; i++) l.Add(new Vector3(i * 2f, 0f, 0f));
        RailFence(b, l, 1.1f, 2);
        var l2 = new List<Vector3>(); for (int i = 0; i <= 8; i++) l2.Add(new Vector3(i * 2f, 0f, 4f));
        DryWall(b, l2, 1.0f, 0.5f, 5);
        var l3 = new List<Vector3>(); for (int i = 0; i <= 8; i++) l3.Add(new Vector3(i * 2f, 0f, 8f));
        HedgeRun(b, l3, 1.3f, 1.1f, 3);
        var l4 = new List<Vector3>(); for (int i = 0; i <= 8; i++) l4.Add(new Vector3(i * 2f, 0f, 12f));
        Palisade(b, l4, 2.6f);
        b.Push(new Vector3(8f, 0f, 22f), 0f); RuneCircle(b, 7f); b.Pop();
    }
}
