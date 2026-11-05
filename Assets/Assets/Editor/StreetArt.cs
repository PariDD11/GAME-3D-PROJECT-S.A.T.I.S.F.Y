using UnityEngine;

/// <summary>n43: perabot jalan & ternak lebih halus: lampu besi tempa, bangku berlengan, tumpukan jerami berikat, domba berbulu, sapi berbercak, orang-orangan sawah.</summary>
public static class StreetArt
{
    static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }

    public static void Lamp(GeoBag b)
    {
        // alas berundak, tiang berulir, lengan melengkung, sangkar lentera, pot bunga gantung
        b.Cyl(Sl.DarkStone, V(0, 0, 0), 0.34f, 0.25f, 8, 0.3f, true);
        b.Cyl(Sl.Metal, V(0, 0.25f, 0), 0.26f, 0.4f, 8, 0.15f, true);
        b.Cyl(Sl.Metal, V(0, 0.65f, 0), 0.1f, 3.2f, 8, 0.075f, true);
        for (int k = 0; k < 4; k++) b.Cyl(Sl.Metal, V(0, 1.1f + k * 0.65f, 0), 0.13f, 0.06f, 8, 0.13f, false);
        b.Ball(Sl.Metal, V(0, 3.85f, 0), 0.14f, 3, 8);
        for (int s = 0; s < 6; s++)
        {
            float t = s / 6f, t2 = (s + 1) / 6f;
            b.Beam(Sl.Metal, V(0.75f * Mathf.Sin(t * 1.5f), 3.5f + 0.5f * (1 - Mathf.Cos(t * 1.5f)) * 0.9f, 0), V(0.75f * Mathf.Sin(t2 * 1.5f), 3.5f + 0.5f * (1 - Mathf.Cos(t2 * 1.5f)) * 0.9f, 0), 0.06f, 0.06f, Vector3.forward);
        }
        b.Beam(Sl.Metal, V(0.1f, 3.1f, 0), V(0.55f, 3.6f, 0), 0.05f, 0.05f);
        Vector3 lp = V(0.75f, 3.55f, 0);
        b.Cyl(Sl.Metal, lp + V(0, -0.45f, 0), 0.15f, 0.06f, 6, 0.12f, true);
        b.Box(Sl.Glow, lp + V(0, -0.2f, 0), V(0.22f, 0.4f, 0.22f));
        for (int k = 0; k < 4; k++)
        {
            float a = (k * 90f + 45f) * Mathf.Deg2Rad;
            b.Beam(Sl.Metal, lp + V(Mathf.Sin(a) * 0.14f, -0.42f, Mathf.Cos(a) * 0.14f), lp + V(Mathf.Sin(a) * 0.1f, -0.0f, Mathf.Cos(a) * 0.1f), 0.03f, 0.03f);
        }
        b.Cone(Sl.Metal, lp + V(0, 0f, 0), 0.22f, 0.3f, 4);
        b.Ball(Sl.Gold, lp + V(0, 0.32f, 0), 0.05f, 3, 5);
        // keranjang bunga di lengan kedua
        Vector3 bp = V(-0.45f, 3.0f, 0);
        b.Beam(Sl.Metal, V(0, 3.0f, 0), bp, 0.05f, 0.05f);
        b.Cyl(Sl.WoodLight, bp + V(0, -0.38f, 0), 0.17f, 0.22f, 8, 0.12f, true);
        b.Ball(Sl.Hedge, bp + V(0, -0.14f, 0), 0.2f, 3, 7, 0.7f);
        b.Ball(Sl.Blossom, bp + V(0.08f, -0.04f, 0.06f), 0.07f, 3, 5);
        b.Ball(Sl.Cloth, bp + V(-0.09f, -0.06f, -0.04f), 0.07f, 3, 5);
    }

    public static void Bench(GeoBag b)
    {
        for (int k = 0; k < 4; k++) b.Box(Sl.WoodLight, V(0, 0.5f, -0.2f + k * 0.13f), V(1.9f, 0.05f, 0.11f));
        for (int k = 0; k < 3; k++) b.Box(Sl.WoodLight, V(0, 0.75f + k * 0.16f, -0.26f), V(1.9f, 0.1f, 0.05f), -8f);
        foreach (float x in new[] { -0.85f, 0.85f })
        {
            b.Box(Sl.Metal, V(x, 0.25f, 0f), V(0.07f, 0.5f, 0.55f));
            b.Beam(Sl.Metal, V(x, 0.5f, -0.28f), V(x, 1.1f, -0.36f), 0.07f, 0.07f);
            b.Beam(Sl.Metal, V(x, 0.6f, 0.2f), V(x, 0.6f, -0.2f), 0.06f, 0.06f);
            b.Beam(Sl.Metal, V(x, 0.7f, 0.2f), V(x, 0.7f, 0.28f), 0.06f, 0.06f);
            b.Ball(Sl.Metal, V(x, 0.72f, 0.3f), 0.05f, 3, 5);
        }
    }

    public static void Haystack(GeoBag b, Vector3 c)
    {
        b.Push(c, 0f);
        b.Cyl(Sl.Thatch, V(0, 0, 0), 1.5f, 1.4f, 12, 1.42f, false);
        b.Dome(Sl.Thatch, V(0, 1.4f, 0), 1.45f, 5, 12, 1.0f);
        for (int k = 0; k < 3; k++) b.Cyl(Sl.WoodLight, V(0, 0.4f + k * 0.5f, 0), 1.53f - k * 0.02f, 0.06f, 12, 1.53f - k * 0.02f, false);
        for (int k = 0; k < 14; k++)
        {
            float a = k * 25.7f * Mathf.Deg2Rad, h = 0.2f + (k % 4) * 0.35f;
            b.Beam(Sl.Thatch, V(Mathf.Sin(a) * 1.46f, h, Mathf.Cos(a) * 1.46f), V(Mathf.Sin(a) * 1.7f, h - 0.1f, Mathf.Cos(a) * 1.7f), 0.05f, 0.03f, Vector3.up);
        }
        b.Cone(Sl.Thatch, V(0, 2.2f, 0), 0.12f, 0.45f, 5);
        b.Pop();
    }

    public static void Sheep(GeoBag b)
    {
        for (int k = 0; k < 9; k++)
        {
            float a = k * 40f * Mathf.Deg2Rad;
            b.Ball(Sl.PlasterB, V(Mathf.Sin(a) * 0.3f, 0.78f + (k % 3) * 0.06f, Mathf.Cos(a) * 0.38f), 0.3f, 3, 6, 0.9f);
        }
        b.Ball(Sl.PlasterB, V(0, 0.9f, 0), 0.55f, 4, 8, 0.9f);
        b.Ball(Sl.PlasterB, V(0, 1.05f, -0.1f), 0.38f, 3, 7, 0.8f);
        b.Ball(Sl.DarkStone, V(0, 0.95f, 0.62f), 0.26f, 4, 7, 1.1f);
        b.Ball(Sl.DarkStone, V(0, 0.9f, 0.82f), 0.14f, 3, 6, 0.8f);
        foreach (float sx in new[] { -1f, 1f })
        {
            b.Box(Sl.DarkStone, V(sx * 0.2f, 1.05f, 0.62f), V(0.1f, 0.06f, 0.2f), sx * 25f);
            b.Ball(Sl.Marble, V(sx * 0.09f, 1.02f, 0.82f), 0.04f, 3, 5);
        }
        b.Ball(Sl.PlasterB, V(0, 0.82f, -0.62f), 0.14f, 3, 6);
        foreach (float x in new[] { -0.25f, 0.25f })
            foreach (float z in new[] { -0.3f, 0.3f })
            {
                b.Cyl(Sl.DarkStone, V(x, 0f, z), 0.06f, 0.45f, 5, 0.05f, true);
                b.Cyl(Sl.DarkStone, V(x, 0f, z), 0.075f, 0.06f, 5, 0.075f, true);
            }
    }

    public static void Cow(GeoBag b)
    {
        b.Ball(Sl.PlasterB, V(0, 1.1f, 0), 0.6f, 4, 8, 0.85f);
        b.Box(Sl.PlasterB, V(0, 1.05f, 0), V(0.95f, 0.85f, 1.8f));
        b.Box(Sl.DarkStone, V(0.22f, 1.2f, 0.2f), V(0.55f, 0.5f, 0.75f));
        b.Box(Sl.DarkStone, V(-0.3f, 1.35f, -0.5f), V(0.35f, 0.3f, 0.5f));
        b.Ball(Sl.PlasterB, V(0, 1.1f, 1.15f), 0.36f, 4, 8, 0.85f);
        b.Box(Sl.PlasterB, V(0, 0.9f, 1.55f), V(0.38f, 0.3f, 0.3f));
        b.Box(Sl.Cloth, V(0, 0.82f, 1.7f), V(0.3f, 0.12f, 0.06f));
        foreach (float sx in new[] { -1f, 1f })
        {
            b.Beam(Sl.Marble, V(sx * 0.2f, 1.5f, 1.1f), V(sx * 0.42f, 1.75f, 1.1f), 0.08f, 0.08f);
            b.Box(Sl.PlasterB, V(sx * 0.32f, 1.4f, 1.1f), V(0.2f, 0.08f, 0.16f), sx * 20f);
            b.Ball(Sl.DarkStone, V(sx * 0.15f, 1.25f, 1.45f), 0.05f, 3, 5);
        }
        b.Ball(Sl.Cloth, V(0, 0.65f, -0.15f), 0.18f, 3, 6, 0.8f);
        b.Beam(Sl.PlasterB, V(0, 1.5f, -0.95f), V(0, 0.7f, -1.1f), 0.08f, 0.06f);
        b.Ball(Sl.DarkStone, V(0, 0.65f, -1.1f), 0.1f, 3, 5);
        foreach (float x in new[] { -0.3f, 0.3f })
            foreach (float z in new[] { -0.7f, 0.7f })
            {
                b.Cyl(Sl.PlasterB, V(x, 0.12f, z), 0.1f, 0.6f, 6, 0.08f, true);
                b.Cyl(Sl.DarkStone, V(x, 0f, z), 0.1f, 0.14f, 6, 0.1f, true);
            }
    }

    public static void Scarecrow(GeoBag b)
    {
        b.Cyl(Sl.Wood, Vector3.zero, 0.07f, 2.3f, 6, 0.07f, true);
        b.Beam(Sl.Wood, V(-0.95f, 1.7f, 0f), V(0.95f, 1.7f, 0f), 0.09f, 0.09f);
        b.Box(Sl.Cloth, V(0, 1.4f, 0), V(0.66f, 0.85f, 0.32f));
        b.Box(Sl.ClothB, V(-0.15f, 1.45f, 0.17f), V(0.22f, 0.22f, 0.02f));
        b.Box(Sl.Thatch, V(0, 1.0f, 0), V(0.55f, 0.1f, 0.3f));
        foreach (float sx in new[] { -1f, 1f })
        {
            b.Beam(Sl.Cloth, V(sx * 0.3f, 1.65f, 0), V(sx * 0.85f, 1.62f, 0), 0.2f, 0.18f);
            for (int k = 0; k < 4; k++) b.Beam(Sl.Thatch, V(sx * 0.9f, 1.62f, 0), V(sx * (1.1f + k * 0.02f), 1.5f - k * 0.08f, (k - 1.5f) * 0.05f), 0.03f, 0.02f);
            for (int k = 0; k < 3; k++) b.Beam(Sl.Thatch, V(sx * 0.18f, 1.0f, 0), V(sx * 0.2f, 0.75f - k * 0.05f, (k - 1) * 0.06f), 0.03f, 0.02f);
        }
        b.Ball(Sl.Thatch, V(0, 2.12f, 0), 0.27f, 4, 7);
        b.Ball(Sl.Dark, V(-0.08f, 2.16f, 0.24f), 0.04f, 3, 5);
        b.Ball(Sl.Dark, V(0.08f, 2.16f, 0.24f), 0.04f, 3, 5);
        b.Box(Sl.Dark, V(0, 2.04f, 0.26f), V(0.14f, 0.025f, 0.02f));
        b.Cyl(Sl.Wood, V(0, 2.28f, 0), 0.43f, 0.05f, 10, 0.43f, true);
        b.Cyl(Sl.Wood, V(0, 2.32f, 0), 0.24f, 0.3f, 10, 0.17f, true);
        b.Box(Sl.Cloth, V(0, 2.4f, 0), V(0.34f, 0.07f, 0.34f));
        b.Ball(Sl.Dark, V(0.95f, 1.86f, 0), 0.1f, 3, 6, 0.8f);      // burung gagak
        b.Cone(Sl.Dark, V(0.95f, 1.75f, -0.1f), 0.06f, 0.2f, 4);
    }
}
