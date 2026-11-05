using UnityEngine;

/// <summary>Detail tambahan DungeonKit (n42): gerigi, kunci-lengkung, obor, penopang, relief, bunga, lentera.</summary>
public static class DungeonArt
{
    static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }

    public static void GrandHall(GeoBag b, float W, float D, float H, float y0, float pz)
    {
        // cornice bergigi (dentil) mengelilingi atap
        int nx = Mathf.RoundToInt(W / 1.6f), nz = Mathf.RoundToInt(D / 1.6f);
        float yd = y0 + H - 0.9f;
        for (int i = 0; i < nx; i++)
        {
            float x = -W / 2f + 0.8f + i * (W - 1.6f) / (nx - 1);
            b.Box(Sl.Marble, V(x, yd, D / 2f + 0.1f), V(0.7f, 0.5f, 0.5f));
            b.Box(Sl.Marble, V(x, yd, -D / 2f - 0.1f), V(0.7f, 0.5f, 0.5f));
        }
        for (int i = 0; i < nz; i++)
        {
            float z = -D / 2f + 0.8f + i * (D - 1.6f) / (nz - 1);
            b.Box(Sl.Marble, V(W / 2f + 0.1f, yd, z), V(0.5f, 0.5f, 0.7f));
            b.Box(Sl.Marble, V(-W / 2f - 0.1f, yd, z), V(0.5f, 0.5f, 0.7f));
        }
        // penopang + pinnacle sepanjang sisi panjang
        foreach (float sx in new[] { -1f, 1f })
            for (int k = 0; k < 6; k++)
            {
                float z = -D / 2f + 5f + k * (D - 10f) / 5f;
                b.BoxB(Sl.Stone, V(sx * (W / 2f + 0.9f), y0, z), V(1.8f, H - 3.4f, 1.6f));
                b.BoxB(Sl.DarkStone, V(sx * (W / 2f + 0.9f), y0 + H - 3.4f, z), V(2.1f, 0.5f, 1.9f));
                b.Cone(Sl.Gold, V(sx * (W / 2f + 0.9f), y0 + H - 2.9f, z), 0.7f, 2.4f, 4);
                b.Box(Sl.Glass, V(sx * (W / 2f - 0.02f), y0 + 7.5f, z + (D - 10f) / 10f), V(0.12f, 6f, 1.5f));
            }
        // taman & lampu di depan serambi
        foreach (float sx in new[] { -1f, 1f })
            for (int k = 0; k < 4; k++)
            {
                float x = sx * (22f + k * 4.2f);
                b.Cyl(Sl.Stone, V(x, 0f, pz + 9f), 1.0f, 0.7f, 10, 0.9f, true);
                b.Ball(Sl.Hedge, V(x, 0.8f, pz + 9f), 0.85f, 4, 8, 0.9f);
                b.Ball(k % 2 == 0 ? Sl.Blossom : Sl.Gold, V(x + 0.3f, 1.4f, pz + 9.2f), 0.28f, 3, 6);
                b.Ball(Sl.Cloth, V(x - 0.3f, 1.3f, pz + 8.8f), 0.24f, 3, 6);
            }
        for (int k = 0; k < 4; k++)
        {
            float x = -9f + k * 6f;
            b.Cyl(Sl.DarkStone, V(x, 0f, pz + 11f), 0.3f, 4.2f, 8, 0.2f, true);
            b.Ball(Sl.Glow, V(x, 4.5f, pz + 11f), 0.38f, 4, 8);
            b.Cyl(Sl.Gold, V(x, 4.2f, pz + 11f), 0.34f, 0.2f, 8, 0.34f, true);
        }
    }

    public static void Coliseum(GeoBag b, float R, int N, float ch)
    {
        // gerigi di puncak dinding luar + kunci lengkung + obor
        for (int i = 0; i < N * 2; i++)
        {
            float a = (i + 0.25f) * 180f / N;
            float r = a * Mathf.Deg2Rad;
            b.Push(V(Mathf.Sin(r) * (R + 0.9f), 18f, Mathf.Cos(r) * (R + 0.9f)), a);
            b.Box(Sl.Stone, V(0f, 0.55f, 0f), V(ch * 0.26f, 1.1f, 0.9f));
            b.Pop();
        }
        for (int i = 0; i < N; i++)
        {
            float a = (i + 0.5f) * 360f / N;
            if (Mathf.Abs(Mathf.DeltaAngle(a, 0f)) < 11f) continue;
            float r = a * Mathf.Deg2Rad;
            b.Push(V(Mathf.Sin(r) * (R + 1.2f), 0f, Mathf.Cos(r) * (R + 1.2f)), a);
            b.Box(Sl.Marble, V(0f, 7.15f, 0.12f), V(0.9f, 0.9f, 0.5f), 0f);
            b.Cone(Sl.Gold, V(0f, 7.6f, 0.15f), 0.3f, 0.6f, 4);
            b.Box(Sl.Marble, V(0f, 14.95f, 0.12f), V(0.9f, 0.9f, 0.5f), 0f);
            b.Beam(Sl.Metal, V(ch * 0.3f, 2.5f, 0.2f), V(ch * 0.3f, 3.1f, 0.6f), 0.07f, 0.07f);
            b.Ball(Sl.Glow, V(ch * 0.3f, 3.35f, 0.6f), 0.2f, 3, 6);
            b.Pop();
        }
        // alur pasir arena & pagar bawah tribun
        for (int k = 0; k < 4; k++) b.Annulus(Sl.PlasterB, V(0f, 0f, 0f), 6f + k * 3.6f, 6.15f + k * 3.6f, 0f, 360f, 36, 1.04f);
        b.Annulus(Sl.Gold, V(0f, 0f, 0f), 19f, 19.5f, 0f, 360f, 40, 1.06f);
        for (int i = 0; i < 24; i++)
        {
            float a = i * 15f * Mathf.Deg2Rad;
            b.Cyl(Sl.Marble, V(Mathf.Sin(a) * 19.7f, 1f, Mathf.Cos(a) * 19.7f), 0.2f, 1.3f, 6, 0.16f, true);
        }
    }

    public static void PortalHub(GeoBag b)
    {
        // batu rune melayang mengitari + obor kristal
        for (int k = 0; k < 6; k++)
        {
            float a = (k * 60f + 15f) * Mathf.Deg2Rad;
            float y = 4f + (k % 3) * 1.6f;
            Vector3 p = V(Mathf.Sin(a) * 10.5f, y, Mathf.Cos(a) * 10.5f);
            b.Cone(Sl.DarkStone, p, 0.5f, 1.2f, 5);
            b.Ball(Sl.Glow, p + V(0, 0.55f, 0), 0.14f, 3, 6);
        }
        for (int k = 0; k < 4; k++)
        {
            float a = (k * 90f + 45f) * Mathf.Deg2Rad;
            b.Cyl(Sl.Stone, V(Mathf.Sin(a) * 11.7f, 1f, Mathf.Cos(a) * 11.7f), 0.5f, 2.2f, 8, 0.38f, true);
            b.Cone(Sl.Glow, V(Mathf.Sin(a) * 11.7f, 3.1f, Mathf.Cos(a) * 11.7f), 0.38f, 1.2f, 6);
        }
        // tangga kecil di keliling
        b.Annulus(Sl.Gold, V(0f, 0f, 0f), 10.4f, 10.6f, 0f, 360f, 36, 1.11f);
        for (int k = 0; k < 12; k++)
        {
            float a = k * 30f * Mathf.Deg2Rad;
            b.Box(Sl.Glow, V(Mathf.Sin(a) * 10f, 1.12f, Mathf.Cos(a) * 10f), V(0.25f, 0.04f, 0.9f), k * 30f);
        }
        // ukiran pada pilar
        foreach (float sx in new[] { -1f, 1f })
            for (int g = 0; g < 4; g++)
                b.Box(Sl.Glow, V(sx * 4.2f, 3f + g * 1.9f, -2.25f), V(0.14f, 0.7f, 0.06f));
    }

    public static void Obelisk(GeoBag b)
    {
        for (int g = 0; g < 5; g++)
        {
            float y = 2.3f + g * 2.1f;
            float hw = Mathf.Lerp(1.38f, 0.82f, (y - 0.9f) / 14f) * 0.72f;
            for (int f = 0; f < 4; f++)
            {
                b.Push(V(0, 0, 0), f * 90f + 45f);
                b.Box(Sl.Glow, V(0f, y, hw + 0.04f), V(0.12f, 0.9f, 0.04f));
                b.Box(Sl.Glow, V(0.22f, y + 0.25f, hw + 0.04f), V(0.1f, 0.1f, 0.04f));
                b.Pop();
            }
        }
        for (int k = 0; k < 4; k++)
        {
            float a = (k * 90f + 45f) * Mathf.Deg2Rad;
            b.Cyl(Sl.Stone, V(Mathf.Sin(a) * 2.5f, 0f, Mathf.Cos(a) * 2.5f), 0.4f, 0.5f, 6, 0.3f, true);
        }
    }

    public static void Arcade(GeoBag b, float W, float D, float H)
    {
        // balok bergantung, papan nama, kotak bunga, lentera, tong
        for (int i = 0; i < 6; i++)
        {
            float x = -W / 2f + 3f + i * (W - 6f) / 5f;
            b.Box(Sl.Wood, V(x, H + 0.45f, D / 2f - 0.1f), V(2.6f, 0.7f, 0.12f));
            b.Box(i % 2 == 0 ? Sl.Gold : Sl.Cloth, V(x, H + 0.45f, D / 2f - 0.02f), V(0.8f, 0.4f, 0.06f));
            AnimeKit.HangLantern(b, V(x + 1.5f, H - 0.9f, D / 2f - 0.3f), 0.5f);
            FantasyKit.FlowerBox(b, V(x, 0.55f, D / 2f - 2.45f) + V(0, 0.5f, 0), 2.2f);
        }
        for (int k = 0; k < 4; k++)
        {
            float x = -W / 2f + 2.2f + k * (W - 4.4f) / 3f;
            b.Cyl(Sl.Wood, V(x, 0f, D / 2f + 1.2f), 0.45f, 0.9f, 10, 0.45f, true);
            b.Cyl(Sl.Metal, V(x, 0.2f, D / 2f + 1.2f), 0.47f, 0.07f, 10, 0.47f, true);
            b.Cyl(Sl.Metal, V(x, 0.65f, D / 2f + 1.2f), 0.47f, 0.07f, 10, 0.47f, true);
            b.Ball(Sl.Hedge, V(x, 0.95f, D / 2f + 1.2f), 0.4f, 3, 7, 0.8f);
        }
        for (int i = 0; i < 12; i++)
        {
            float x0 = -W / 2f + i * W / 12f;
            b.QuadBoth(i % 2 == 0 ? Sl.Cloth : Sl.ClothB, V(x0, H + 0.1f, D / 2f + 0.6f), V(x0 + W / 12f, H + 0.1f, D / 2f + 0.6f), V(x0 + W / 24f, H - 0.55f, D / 2f + 0.6f), V(x0 + W / 24f, H - 0.55f, D / 2f + 0.6f));
        }
        for (int s = -1; s <= 1; s += 2)
        {
            b.Cone(Sl.Gold, V(s * (W / 2f + 0.1f), H + 0.7f, D / 2f - 0.8f), 0.35f, 1.0f, 4);
            b.Cone(Sl.Gold, V(s * (W / 2f + 0.1f), H + 0.7f, -D / 2f + 0.8f), 0.35f, 1.0f, 4);
        }
    }

    public static void CrystalFountain(GeoBag b, float scale)
    {
        // lekuk ukiran + bunga teratai + kristal kecil mengitari + bangku
        for (int k = 0; k < 16; k++)
        {
            float a = k * 22.5f * Mathf.Deg2Rad;
            b.Box(Sl.Stone, V(Mathf.Sin(a) * 6.1f * scale, 0.45f, Mathf.Cos(a) * 6.1f * scale), V(0.5f, 0.55f, 0.5f), k * 22.5f);
            b.Ball(Sl.Gold, V(Mathf.Sin(a) * 6.1f * scale, 0.78f, Mathf.Cos(a) * 6.1f * scale), 0.13f, 3, 6);
        }
        for (int k = 0; k < 5; k++)
        {
            float a = (k * 72f + 20f) * Mathf.Deg2Rad;
            Vector3 p = V(Mathf.Sin(a) * 3.8f * scale, 1.07f, Mathf.Cos(a) * 3.8f * scale);
            b.Disc(Sl.Hedge, p, 0.5f, 8, 0f);
            b.Ball(Sl.Blossom, p + V(0, 0.1f, 0), 0.16f, 3, 6, 0.7f);
        }
        for (int k = 0; k < 4; k++)
        {
            float a = (k * 90f + 45f) * Mathf.Deg2Rad;
            float rr = 7.6f * scale;
            b.Push(V(Mathf.Sin(a) * rr, 0f, Mathf.Cos(a) * rr), k * 90f + 45f + 180f);
            b.Box(Sl.Stone, V(0, 0.45f, 0), V(2.4f, 0.12f, 0.7f));
            b.Box(Sl.Stone, V(-1f, 0.2f, 0), V(0.18f, 0.4f, 0.6f));
            b.Box(Sl.Stone, V(1f, 0.2f, 0), V(0.18f, 0.4f, 0.6f));
            b.Pop();
            b.Ball(Sl.Hedge, V(Mathf.Sin(a + 0.35f) * rr, 0.4f, Mathf.Cos(a + 0.35f) * rr), 0.5f, 3, 7, 0.8f);
        }
    }
}
