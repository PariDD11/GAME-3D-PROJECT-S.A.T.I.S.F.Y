using UnityEngine;

/// <summary>n43: kuburan desa: nisan beraneka bentuk (lengkung, salib keltik, malaikat, obelisk, peti batu), mausoleum berelief, gerbang besi tempa, lampu, yew.</summary>
public static class CemeteryArt
{
    static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }

    /// <summary>Satu makam. c = kepala makam, yaw = arah nisan menghadap +Z (lokal).</summary>
    public static void Grave(GeoBag b, Vector3 c, float yaw, int kind, int rnd)
    {
        b.Push(c, yaw);
        // gundukan tanah + bingkai batu + rumput
        b.Box(Sl.Dirt, V(0, 0.1f, 0.95f), V(1.0f, 0.2f, 1.8f));
        b.Box(Sl.Stone, V(-0.55f, 0.1f, 0.95f), V(0.1f, 0.2f, 1.9f));
        b.Box(Sl.Stone, V(0.55f, 0.1f, 0.95f), V(0.1f, 0.2f, 1.9f));
        b.Box(Sl.Stone, V(0, 0.1f, 1.88f), V(1.2f, 0.2f, 0.1f));
        b.Ball(Sl.Hedge, V(-0.2f, 0.22f, 1.1f), 0.2f, 3, 6, 0.4f);
        b.Ball(Sl.Hedge, V(0.25f, 0.22f, 1.5f), 0.18f, 3, 6, 0.4f);
        b.Box(Sl.Stone, V(0, 0.08f, -0.1f), V(1.1f, 0.16f, 0.5f));       // alas nisan
        switch (kind % 7)
        {
            case 0: // nisan lengkung dengan bingkai
                b.Box(Sl.Stone, V(0, 0.72f, 0), V(0.82f, 1.1f, 0.2f));
                b.Ball(Sl.Stone, V(0, 1.27f, 0), 0.41f, 3, 8, 0.65f);
                b.Box(Sl.Marble, V(0, 0.9f, 0.11f), V(0.5f, 0.06f, 0.02f));
                b.Box(Sl.Marble, V(0, 0.75f, 0.11f), V(0.4f, 0.05f, 0.02f));
                b.Box(Sl.Marble, V(0, 0.62f, 0.11f), V(0.45f, 0.05f, 0.02f));
                break;
            case 1: // salib kayu miring dengan ikatan
                b.Box(Sl.Wood, V(0.03f, 0.85f, 0), V(0.16f, 1.7f, 0.16f), 4f);
                b.Box(Sl.Wood, V(0.03f, 1.3f, 0), V(0.85f, 0.14f, 0.14f), 4f);
                b.Ball(Sl.Cloth, V(0.03f, 1.3f, 0.1f), 0.1f, 3, 5);
                break;
            case 2: // obelisk kecil
                b.Box(Sl.Stone, V(0, 0.3f, 0), V(0.8f, 0.5f, 0.6f));
                b.Cyl(Sl.Marble, V(0, 0.55f, 0), 0.26f, 1.5f, 4, 0.14f, false);
                b.Cone(Sl.Marble, V(0, 2.05f, 0), 0.15f, 0.3f, 4);
                break;
            case 3: // malaikat berdoa
                b.Box(Sl.Stone, V(0, 0.35f, 0), V(0.9f, 0.6f, 0.7f));
                b.Cyl(Sl.Marble, V(0, 0.65f, 0), 0.26f, 0.9f, 8, 0.2f, true);
                b.Ball(Sl.Marble, V(0, 1.7f, 0), 0.17f, 4, 8);
                b.Beam(Sl.Marble, V(-0.1f, 1.45f, -0.05f), V(-0.55f, 1.9f, -0.2f), 0.2f, 0.04f, Vector3.forward);
                b.Beam(Sl.Marble, V(0.1f, 1.45f, -0.05f), V(0.55f, 1.9f, -0.2f), 0.2f, 0.04f, Vector3.forward);
                b.Annulus(Sl.Gold, V(0, 0, 0), 0.12f, 0.18f, 0f, 360f, 8, 1.92f);
                break;
            case 4: // salib batu keltik
                b.Box(Sl.Stone, V(0, 0.75f, 0), V(0.3f, 1.5f, 0.2f));
                b.Box(Sl.Stone, V(0, 1.2f, 0), V(0.9f, 0.26f, 0.2f));
                b.Annulus(Sl.Marble, V(0, 0, 0), 0.26f, 0.36f, 0f, 360f, 12, 1.21f);
                b.Box(Sl.Stone, V(0, 0.2f, 0), V(0.8f, 0.4f, 0.55f));
                break;
            case 5: // nisan pelana dengan ukiran
                b.Box(Sl.Marble, V(0, 0.55f, 0), V(0.9f, 1.1f, 0.22f));
                b.Gable(Sl.Stone, Sl.Stone, Sl.Stone, V(0, 1.1f, 0), 1.0f, 0.3f, 0.3f, 0.02f, true);
                b.Box(Sl.Glow, V(0, 0.65f, 0.12f), V(0.08f, 0.35f, 0.02f));
                b.Box(Sl.Glow, V(0, 0.7f, 0.12f), V(0.3f, 0.08f, 0.02f));
                break;
            default: // peti batu dengan tutup + karangan bunga
                b.Box(Sl.Stone, V(0, 0.28f, 0.95f), V(0.95f, 0.55f, 1.7f));
                b.Box(Sl.Marble, V(0, 0.6f, 0.95f), V(1.05f, 0.12f, 1.8f));
                b.Box(Sl.Stone, V(0, 0.55f, 0), V(0.7f, 1.0f, 0.16f));
                break;
        }
        if ((rnd & 3) == 0)
        {
            b.Ball(Sl.Hedge, V(0.4f, 0.2f, 0.2f), 0.2f, 3, 6, 0.7f);
            b.Ball(Sl.Blossom, V(0.42f, 0.38f, 0.22f), 0.1f, 3, 5);
            b.Ball(Sl.Cloth, V(0.34f, 0.34f, 0.12f), 0.08f, 3, 5);
        }
        else if ((rnd & 3) == 1)
        {
            b.Cyl(Sl.Marble, V(-0.45f, 0.2f, 0.4f), 0.05f, 0.18f, 6, 0.04f, true);
            b.Ball(Sl.Glow, V(-0.45f, 0.4f, 0.4f), 0.06f, 3, 5);
        }
        b.Pop();
    }

    public static void GatePier(GeoBag b, float x, float z)
    {
        b.Box(Sl.DarkStone, V(x, 0.3f, z), V(1.7f, 0.6f, 1.7f));
        b.Cyl(Sl.Stone, V(x, 0.6f, z), 0.7f, 4.0f, 8, 0.62f, true);
        for (int k = 0; k < 3; k++) b.Cyl(Sl.DarkStone, V(x, 1.4f + k * 1.0f, z), 0.74f, 0.14f, 8, 0.74f, false);
        b.Cyl(Sl.DarkStone, V(x, 4.6f, z), 0.92f, 0.35f, 8, 0.82f, true);
        b.Ball(Sl.Stone, V(x, 5.2f, z), 0.58f, 4, 8, 1.15f);
        b.Cone(Sl.Gold, V(x, 5.7f, z), 0.12f, 0.5f, 5);
        b.Cyl(Sl.Metal, V(x, 3.2f, z + 0.8f), 0.04f, 0.5f, 5, 0.04f, true);
        b.Box(Sl.Metal, V(x, 3.9f, z + 0.95f), V(0.4f, 0.04f, 0.04f));
        b.Box(Sl.Glow, V(x, 3.55f, z + 0.95f), V(0.3f, 0.5f, 0.3f));
        b.Cone(Sl.Metal, V(x, 3.8f, z + 0.95f), 0.25f, 0.28f, 4);
    }

    /// <summary>Pintu gerbang besi tempa (dua daun terbuka sedikit).</summary>
    public static void IronGate(GeoBag b, float z, float halfOpen)
    {
        foreach (float s in new[] { -1f, 1f })
        {
            b.Push(V(s * 3.0f, 0, z), s * 28f);
            for (int k = 0; k < 9; k++)
            {
                float x = -s * k * 0.28f;
                b.Box(Sl.Metal, V(x, 1.5f, 0), V(0.06f, 3.0f, 0.06f));
                b.Cone(Sl.Metal, V(x, 3.0f, 0), 0.06f, 0.26f, 4);
                b.Ball(Sl.Gold, V(x, 3.28f, 0), 0.04f, 3, 5);
            }
            b.Box(Sl.Metal, V(-s * 1.12f, 0.5f, 0), V(2.4f, 0.08f, 0.08f));
            b.Box(Sl.Metal, V(-s * 1.12f, 2.5f, 0), V(2.4f, 0.08f, 0.08f));
            for (int k = 0; k < 4; k++) b.Annulus(Sl.Metal, V(-s * (0.3f + k * 0.55f), 0, 0), 0.14f, 0.2f, 0f, 360f, 8, 1.5f);
            b.Pop();
        }
    }

    public static void Mausoleum(GeoBag b, float mz)
    {
        // pilaster sudut + tangga + pintu lengkung berukir + mawar kaca + pinnacle + patung di atap
        foreach (float sx in new[] { -1f, 1f })
        {
            b.BoxB(Sl.DarkStone, V(sx * 5.9f, 0.7f, mz + 3.4f), V(0.9f, 6.4f, 0.9f));
            b.BoxB(Sl.DarkStone, V(sx * 5.9f, 0.7f, mz - 3.4f), V(0.9f, 6.4f, 0.9f));
            b.Cone(Sl.Marble, V(sx * 5.9f, 7.1f, mz + 3.4f), 0.55f, 1.8f, 4);
            b.Cone(Sl.Marble, V(sx * 5.9f, 7.1f, mz - 3.4f), 0.55f, 1.8f, 4);
            for (int k = 0; k < 4; k++)
                b.Box(Sl.Stone, V(sx * 5.75f, 1.6f + k * 1.4f, mz + (k % 2 == 0 ? 3.4f : 3.35f)), V(1.0f, 0.2f, 1.0f));
            // jendela lancip di dinding samping
            for (int k = 0; k < 2; k++)
            {
                b.Push(V(sx * 5.8f, 0, mz + (k - 0.5f) * 3.2f), sx * 90f);
                BuildKit.ArchWindow(b, V(0, 2.6f, 0), 0.9f, 2.4f, Sl.Marble);
                b.Pop();
            }
        }
        for (int k = 0; k < 11; k++)
        {
            float a = Mathf.PI * k / 10f, a2 = Mathf.PI * (k + 1) / 10f;
            b.Beam(Sl.Marble, V(-1.4f * Mathf.Cos(a), 3.6f + Mathf.Sin(a) * 1.3f, mz + 3.45f), V(-1.4f * Mathf.Cos(a2), 3.6f + Mathf.Sin(a2) * 1.3f, mz + 3.45f), 0.3f, 0.3f, Vector3.forward);
        }
        b.Ball(Sl.Glow, V(0, 5.6f, mz + 3.4f), 0.45f, 3, 12, 0.25f);
                for (int k = 0; k < 8; k++)
        {
            float a = k * 45f * Mathf.Deg2Rad;
            b.Beam(Sl.Marble, V(0, 5.6f, mz + 3.45f), V(Mathf.Sin(a) * 0.5f, 5.6f + Mathf.Cos(a) * 0.5f, mz + 3.45f), 0.05f, 0.05f, Vector3.forward);
        }
        b.Stairs(Sl.Stone, V(0, 0, mz + 5.5f), 5.2f, 3, 0.1f, 0.5f, 0f);
        foreach (float sx in new[] { -1f, 1f })
        {
            b.Box(Sl.DarkStone, V(sx * 3.1f, 0.5f, mz + 4.6f), V(0.9f, 1.0f, 0.9f));
            b.Cone(Sl.Marble, V(sx * 3.1f, 1.0f, mz + 4.6f), 0.28f, 1.0f, 6);
            b.Ball(Sl.Glow, V(sx * 2.4f, 3.3f, mz + 3.8f), 0.18f, 3, 6);
        }
        // atap: genteng berbanding + bubungan emas + dua gargoyle
        for (int k = 0; k < 10; k++) b.Box(Sl.Stone, V(-5.2f + k * 1.15f, 7.65f + (k % 2) * 0.06f, mz), V(0.5f, 0.12f, 6.6f));
        foreach (float sx in new[] { -1f, 1f })
        {
            b.Box(Sl.DarkStone, V(sx * 5.4f, 6.8f, mz + 3.7f), V(0.6f, 0.5f, 1.0f));
            b.Ball(Sl.DarkStone, V(sx * 5.4f, 6.9f, mz + 4.3f), 0.28f, 3, 6);
        }
        b.Box(Sl.Gold, V(0, 8.55f, mz), V(0.2f, 0.2f, 6.9f));
    }

    public static void Extras(GeoBag b, float HW, float HD, System.Random rng)
    {
        // bangku batu
        foreach (float sx in new[] { -1f, 1f })
        {
            b.Push(V(sx * 5.5f, 0, 0.0f), -sx * 90f);
            b.Box(Sl.Stone, V(0, 0.5f, 0), V(1.8f, 0.14f, 0.55f));
            b.Box(Sl.Stone, V(-0.7f, 0.25f, 0), V(0.14f, 0.5f, 0.5f));
            b.Box(Sl.Stone, V(0.7f, 0.25f, 0), V(0.14f, 0.5f, 0.5f));
            b.Pop();
        }
        // semak & rumput liar
        for (int i = 0; i < 12; i++)
        {
            float x = (float)(rng.NextDouble() * 2 - 1) * (HW - 1.5f), z = (float)(rng.NextDouble() * 2 - 1) * (HD - 1.5f);
            if (Mathf.Abs(x) < 2.5f) continue;
            TreeKit.GrassTuft(b, Sl.Hedge, V(x, 0, z), 0.5f, i);
        }
        // sumur air dan pot bunga di dekat gerbang
        foreach (float sx in new[] { -1f, 1f })
        {
            b.Cyl(Sl.Stone, V(sx * 6.3f, 0, HD - 1.5f), 0.55f, 0.6f, 8, 0.5f, true);
            b.Ball(Sl.Hedge, V(sx * 6.3f, 0.75f, HD - 1.5f), 0.45f, 3, 7, 0.8f);
            b.Ball(Sl.Blossom, V(sx * 6.3f + 0.15f, 1.0f, HD - 1.45f), 0.14f, 3, 5);
            b.Ball(Sl.Cloth, V(sx * 6.3f - 0.15f, 0.95f, HD - 1.6f), 0.12f, 3, 5);
        }
    }
}
