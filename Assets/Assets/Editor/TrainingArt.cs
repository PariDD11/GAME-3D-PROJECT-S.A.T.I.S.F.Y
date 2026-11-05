using UnityEngine;

/// <summary>n43: detail arena latihan Desa Pemula: gazebo senjata beratap, tonggak latihan, tali ring, jerami, tribun panah beratap, papan skor, menara kristal sihir.</summary>
public static class TrainingArt
{
    static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }

    static void Post(GeoBag b, Vector3 p, float h)
    {
        b.Box(Sl.Stone, p + V(0, 0.1f, 0), V(0.4f, 0.2f, 0.4f));
        b.Box(Sl.Wood, p + V(0, h / 2f, 0), V(0.2f, h, 0.2f));
        b.Box(Sl.Wood, p + V(0, h - 0.05f, 0), V(0.3f, 0.1f, 0.3f));
    }

    public static void SwordYard(GeoBag b, Vector3 c)
    {
        // gazebo senjata (atap genteng + empat tiang) di atas rak
        Vector3 gp = c + V(-4.8f, 0f, 5.4f);
        foreach (float sx in new[] { -1f, 1f }) foreach (float sz in new[] { -1f, 1f }) Post(b, gp + V(sx * 2.3f, 0, sz * 1.0f), 2.8f);
        b.Hip(Sl.Roof, Sl.Wood, gp + V(0, 2.9f, 0), 5.2f, 2.8f, 1.1f, 0.4f);
        b.Box(Sl.Wood, gp + V(0, 2.75f, 0), V(5.0f, 0.12f, 0.12f));
        AnimeKit.HangLantern(b, gp + V(2.3f, 2.7f, 1.0f), 0.5f);
        b.QuadBoth(Sl.Cloth, gp + V(-2.2f, 2.7f, 1.05f), gp + V(2.2f, 2.7f, 1.05f), gp + V(2.2f, 2.3f, 1.05f), gp + V(-2.2f, 2.3f, 1.05f));
        // perisai di dinding belakang
        for (int k = 0; k < 4; k++)
        {
            Vector3 sp = gp + V(-1.8f + k * 1.2f, 1.7f, -0.95f);
            b.Ball(k % 2 == 0 ? Sl.Cloth : Sl.ClothB, sp, 0.38f, 3, 8, 0.25f);
            b.Ball(Sl.Metal, sp + V(0, 0, 0.1f), 0.1f, 3, 6);
        }
        // tiang latihan bersilang (pell) dengan jerami + tameng kayu
        for (int k = 0; k < 4; k++)
        {
            float a = (190f + k * 42f) * Mathf.Deg2Rad;
            Vector3 p = c + V(Mathf.Sin(a) * 7.0f, 0, Mathf.Cos(a) * 7.0f);
            b.Cyl(Sl.Wood, p, 0.14f, 2.0f, 7, 0.12f, true);
            b.Box(Sl.Thatch, p + V(0, 1.4f, 0.0f), V(0.4f, 0.7f, 0.4f));
            b.Beam(Sl.WoodLight, p + V(-0.4f, 1.55f, 0), p + V(0.4f, 1.55f, 0), 0.08f, 0.08f);
            b.Ball(Sl.Cloth, p + V(0, 1.45f, 0.22f), 0.15f, 3, 6, 0.4f);
        }
        // tali ring, tong air, tumpukan jerami, obor
        for (int k = 0; k < 4; k++)
        {
            float a = k * 90f * Mathf.Deg2Rad;
            b.Cyl(Sl.Wood, c + V(Mathf.Sin(a) * 3.3f, 0, Mathf.Cos(a) * 3.3f), 0.07f, 0.6f, 6, 0.06f, true);
        }
        b.Cyl(Sl.Wood, c + V(6.6f, 0, 3.4f), 0.5f, 0.75f, 10, 0.5f, true);
        b.Disc(Sl.Water, c + V(6.6f, 0.75f, 3.4f), 0.44f, 10, 0f);
        b.Cyl(Sl.Metal, c + V(6.6f, 0.2f, 3.4f), 0.52f, 0.05f, 10, 0.52f, false);
        PropKit.Haystack(b, c + V(-7.0f, 0, -2.2f));
        for (int k = 0; k < 2; k++)
        {
            Vector3 tp = c + V(-5.9f + k * 11.8f, 0, 6.2f);
            b.Cyl(Sl.DarkStone, tp, 0.14f, 2.4f, 6, 0.1f, true);
            b.Cyl(Sl.Metal, tp + V(0, 2.4f, 0), 0.2f, 0.2f, 8, 0.28f, true);
            b.Cone(Sl.Glow, tp + V(0, 2.55f, 0), 0.2f, 0.5f, 6);
        }
        for (int k = 0; k < 7; k++) TreeKit.GrassTuft(b, Sl.Hedge, c + V(Mathf.Sin(k * 1.1f) * 8.7f, 0, Mathf.Cos(k * 1.1f) * 8.7f), 0.45f, k);
    }

    public static void Archery(GeoBag b, float cx, float zS)
    {
        // pondok pemanah beratap di belakang garis tembak
        Vector3 pp = V(cx + 0f, 0f, zS + 3.2f);
        foreach (float sx in new[] { -1f, 1f }) foreach (float sz in new[] { -1f, 1f }) Post(b, pp + V(sx * 4.6f, 0, sz * 1.6f), 3.2f);
        b.Gable(Sl.Roof, Sl.Plaster, Sl.Wood, pp + V(0, 3.3f, 0), 10.2f, 4.2f, 1.6f, 0.45f, true);
        b.Box(Sl.Wood, pp + V(0, 0.45f, 1.55f), V(9.0f, 0.12f, 0.5f));
        b.Box(Sl.Wood, pp + V(0, 0.25f, 1.55f), V(8.8f, 0.4f, 0.45f));
        for (int k = 0; k < 5; k++) b.Cyl(Sl.WoodLight, pp + V(-3.4f + k * 1.7f, 0.5f, -1.2f), 0.2f, 0.8f, 8, 0.17f, true);
        for (int k = 0; k < 5; k++) for (int j = 0; j < 4; j++) b.Beam(Sl.Marble, pp + V(-3.4f + k * 1.7f + (j - 1.5f) * 0.07f, 1.3f, -1.2f), pp + V(-3.4f + k * 1.7f + (j - 1.5f) * 0.09f, 1.75f, -1.2f), 0.02f, 0.02f);
        AnimeKit.HangLantern(b, pp + V(-4.6f, 3.0f, 1.6f), 0.5f);
        AnimeKit.HangLantern(b, pp + V(4.6f, 3.0f, 1.6f), 0.5f);
        for (int k = 0; k < 10; k++)
        {
            float x0 = -4.6f + k * 0.92f;
            b.Tri(k % 2 == 0 ? Sl.Cloth : Sl.ClothB, pp + V(x0, 3.0f, 1.65f), pp + V(x0 + 0.85f, 3.0f, 1.65f), pp + V(x0 + 0.42f, 2.45f, 1.65f));
            b.Tri(k % 2 == 0 ? Sl.Cloth : Sl.ClothB, pp + V(x0 + 0.85f, 3.0f, 1.65f), pp + V(x0, 3.0f, 1.65f), pp + V(x0 + 0.42f, 2.45f, 1.65f));
        }
        // papan skor + kantung jerami berbentuk badan di tiap target
        b.Box(Sl.Wood, V(cx + 7.4f, 1.3f, zS - 6f), V(0.14f, 2.6f, 0.14f));
        b.Box(Sl.Wood, V(cx + 7.4f, 2.5f, zS - 6f), V(1.6f, 1.0f, 0.1f));
        b.Box(Sl.Plaster, V(cx + 7.4f, 2.5f, zS - 5.94f), V(1.4f, 0.8f, 0.03f));
        for (int k = 0; k < 5; k++) b.Box(Sl.Cloth, V(cx + 6.9f + k * 0.2f, 2.5f, zS - 5.92f), V(0.04f, 0.5f, 0.02f));
        // tiang bendera angin
        b.Cyl(Sl.Wood, V(cx - 7.5f, 0, -76f), 0.07f, 4.4f, 6, 0.06f, true);
        for (int k = 0; k < 4; k++)
            b.Cyl(k % 2 == 0 ? Sl.Cloth : Sl.Plaster, V(cx - 7.5f + 0.3f + k * 0.34f, 4.1f - k * 0.04f, -76f), 0.2f - k * 0.03f, 0.34f, 8, 0.18f - k * 0.03f, false);
        // penanda jarak berlabel batu
        foreach (float z in new[] { -67f, -73f, -79f })
        {
            b.Box(Sl.Stone, V(cx - 6.4f, 0.25f, z + 1.4f), V(0.5f, 0.5f, 0.5f));
            b.Box(Sl.Marble, V(cx - 6.4f, 0.52f, z + 1.4f), V(0.55f, 0.06f, 0.55f));
        }
        for (int k = 0; k < 6; k++) TreeKit.GrassTuft(b, Sl.Hedge, V(cx - 6.6f, 0, -63f - k * 4f), 0.5f, k);
        for (int k = 0; k < 6; k++) TreeKit.GrassTuft(b, Sl.Hedge, V(cx + 6.6f, 0, -63f - k * 4f), 0.5f, k + 9);
    }

    public static void Magic(GeoBag b, Vector3 c)
    {
        // enam menhir rune berlilitan akar cahaya + kristal melayang + lampu
        for (int k = 0; k < 6; k++)
        {
            float a = (k * 60f) * Mathf.Deg2Rad;
            Vector3 p = c + V(Mathf.Sin(a) * 9.8f, 0, Mathf.Cos(a) * 9.8f);
            b.Push(p, k * 60f + 180f);
            b.Box(Sl.DarkStone, V(0, 0.2f, 0), V(1.4f, 0.4f, 1.1f));
            b.Beam(Sl.Stone, V(0, 0.2f, 0), V(0.05f, 3.4f, 0), 0.95f, 0.65f, Vector3.forward);
            b.Box(Sl.Glow, V(0, 2.0f, 0.34f), V(0.1f, 0.9f, 0.03f));
            b.Box(Sl.Glow, V(0, 2.2f, 0.34f), V(0.5f, 0.08f, 0.03f));
            b.Box(Sl.Glow, V(0.17f, 1.6f, 0.34f), V(0.2f, 0.07f, 0.03f));
            b.Cone(Sl.Glow, V(0.0f, 3.5f, 0), 0.26f, 0.7f, 5);
            b.Pop();
        }
        for (int k = 0; k < 3; k++)
        {
            float a = (k * 120f) * Mathf.Deg2Rad;
            Vector3 p = c + V(Mathf.Sin(a) * 2.4f, 2.6f + k * 0.4f, Mathf.Cos(a) * 2.4f);
            b.Cone(Sl.Glow, p, 0.28f, 0.8f, 5);
            b.Cone(Sl.Glow, p + V(0, 0f, 0), 0.28f, 0.8f, 5);
        }
        // bangku lingkar & papan target sihir
        for (int k = 0; k < 4; k++)
        {
            float a = (k * 90f + 45f) * Mathf.Deg2Rad;
            b.Push(c + V(Mathf.Sin(a) * 6.2f, 0, Mathf.Cos(a) * 6.2f), k * 90f + 45f + 180f);
            b.Box(Sl.Stone, V(0, 0.4f, 0), V(1.6f, 0.12f, 0.5f));
            b.Box(Sl.Stone, V(-0.6f, 0.2f, 0), V(0.14f, 0.4f, 0.45f));
            b.Box(Sl.Stone, V(0.6f, 0.2f, 0), V(0.14f, 0.4f, 0.45f));
            b.Pop();
        }
        b.Annulus(Sl.Gold, c, 1.0f, 1.2f, 0f, 360f, 24, 0.1f);
        b.Cyl(Sl.Marble, c + V(0, 0.06f, 0), 0.5f, 0.5f, 10, 0.4f, true);
        b.Cone(Sl.Glow, c + V(0, 0.6f, 0), 0.32f, 1.1f, 6);
    }
}
