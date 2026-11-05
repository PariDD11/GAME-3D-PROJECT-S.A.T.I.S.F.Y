using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Kota Eldrath: palet dan bangunan yang lebih megah dari Ashwatch.
/// Marmer putih + emas + atap biru laut + kristal biru bercahaya. Memakai GeoBag, BuildKit, dan slot Sl.* yang sama dengan Ashwatch.
/// Bingkai lokal tiap bangunan: pintu depan menghadap +Z, origin di tengah dasar.
/// </summary>
public static class DungeonKit
{
    public static Material Crystal, Arcane, Obsidian, RoofNavy, CrystalGlass, LeafTeal, BlossomViolet, WaterArcane;
    static Color C(float r, float g, float b) { return new Color(r, g, b); }
    static Vector3 P(float x, float y, float z) { return new Vector3(x, y, z); }

    /// <summary>Panggil sesudah TownMats.Load().</summary>
    public static void Load()
    {
        Texture2D roofTex = TownMats.Roofs[2].mainTexture as Texture2D;
        Crystal = TownMats.Emissive(TownMats.Mk("D_Crystal", C(0.35f, 0.85f, 1f), null, 1f, 0.9f), C(0.25f, 1.2f, 1.9f));
        Arcane = TownMats.Emissive(TownMats.Mk("D_Arcane", C(0.62f, 0.45f, 1f), null, 1f, 0.9f), C(0.9f, 0.5f, 1.8f));
        Obsidian = TownMats.Mk("D_Obsidian", C(0.1f, 0.1f, 0.16f), null, 1f, 0.85f, 0.35f);
        RoofNavy = TownMats.Mk("D_RoofNavy", C(0.2f, 0.32f, 0.66f), roofTex, 2f, 0.35f);
        CrystalGlass = TownMats.Emissive(TownMats.Mk("D_Glass", C(0.4f, 0.75f, 0.95f), null, 1f, 0.9f), C(0.08f, 0.22f, 0.34f));
        LeafTeal = TownMats.Mk("D_LeafTeal", C(0.3f, 0.78f, 0.7f), TownMats.Leaf.mainTexture as Texture2D, 3f, 0.08f);
        BlossomViolet = TownMats.Mk("D_BlossomViolet", C(0.68f, 0.6f, 1f), TownMats.Blossom.mainTexture as Texture2D, 3f, 0.08f);
        WaterArcane = TownMats.Mk("D_Water", C(0.2f, 0.72f, 0.92f), null, 1f, 0.95f);
    }

    /// <summary>Slot bahan kota dungeon: marmer, atap biru laut, kristal sebagai cahaya.</summary>
    public static Material[] Slots(int variant = 0)
    {
        var plasterA = new[] { TownMats.Plaster[1], TownMats.Plaster[6], TownMats.Plaster[0] };
        var roofs = new[] { RoofNavy, TownMats.Roofs[3], TownMats.Roofs[4] };
        var a = TownMats.Slots(plasterA[variant % 3], roofs[variant % 3], plasterA[(variant + 1) % 3], roofs[(variant + 1) % 3],
                               TownMats.Cloths[1], TownMats.Cloths[4]);
        a[Sl.Stone] = TownMats.Marble;
        a[Sl.DarkStone] = TownMats.Stone;
        a[Sl.Glow] = Crystal;
        a[Sl.Glass] = CrystalGlass;
        a[Sl.Dark] = Obsidian;
        a[Sl.Leaf] = LeafTeal;
        a[Sl.Blossom] = BlossomViolet;
        a[Sl.Water] = WaterArcane;
        a[Sl.Pave] = TownMats.Pave;
        a[Sl.Fur] = LeafTeal; a[Sl.Hair] = LeafTeal;                  // nada daun TreeGen mengikuti palet teal
        a[Sl.Lip] = BlossomViolet; a[Sl.EyeW] = BlossomViolet;        // sakura Eldrath ungu
        return a;
    }

    // ====================================================================== utilitas
    static float Chord(float r, int n) { return 2f * r * Mathf.Sin(Mathf.PI / n) + 0.25f; }

    /// <summary>Cincin dinding dari kotak-kotak mengelilingi lingkaran. Berlaku untuk sudut a0..a1 (derajat, 0 = +Z).</summary>
    static void RingWall(GeoBag b, int slot, float r, float thick, float y0, float h, int n, float skipFrom = 999f, float skipTo = -999f)
    {
        float ch = Chord(r, n);
        for (int i = 0; i < n; i++)
        {
            float a = i * 360f / n;
            float da = Mathf.Abs(Mathf.DeltaAngle(a, 0f));
            if (da <= skipFrom && da >= skipTo && skipFrom < 900f) continue;
            float rad = a * Mathf.Deg2Rad;
            b.Box(slot, P(Mathf.Sin(rad) * r, y0 + h / 2f, Mathf.Cos(rad) * r), new Vector3(ch, h, thick), a);
        }
    }

    static void RingBeams(GeoBag b, int slot, Vector3 c, float r, int n, float th)
    {
        for (int i = 0; i < n; i++)
        {
            float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
            b.Beam(slot, c + P(Mathf.Sin(a0) * r, 0f, Mathf.Cos(a0) * r), c + P(Mathf.Sin(a1) * r, 0f, Mathf.Cos(a1) * r), th, th);
        }
    }

    /// <summary>Lengkung setengah lingkaran dari balok (bidang XY, pusat c, menghadap +Z).</summary>
    static void ArchBeams(GeoBag b, int slot, Vector3 c, float r, int n, float tw, float th)
    {
        for (int k = 0; k < n; k++)
        {
            float a0 = Mathf.PI * k / n, a1 = Mathf.PI * (k + 1) / n;
            b.Beam(slot, c + P(Mathf.Cos(a0) * r, Mathf.Sin(a0) * r, 0f), c + P(Mathf.Cos(a1) * r, Mathf.Sin(a1) * r, 0f), tw, th, Vector3.forward);
        }
    }

    static void FillArch(GeoBag b, int slot, Vector3 c, float r, int n)
    {
        for (int k = 0; k < n; k++)
        {
            float a0 = Mathf.PI * k / n, a1 = Mathf.PI * (k + 1) / n;
            Vector3 p0 = c + P(Mathf.Cos(a0) * r, Mathf.Sin(a0) * r, 0f), p1 = c + P(Mathf.Cos(a1) * r, Mathf.Sin(a1) * r, 0f);
            b.Tri(slot, c, p1, p0);
            b.Tri(slot, c, p0, p1);
        }
    }

    // ====================================================================== MENARA ELDRATH
    public const float TowerR = 26f, TowerPlinthR = 43f, TowerPlinthH = 3.6f, TowerH = 100f;
    public const float TowerStairZ0 = 54.1f, TowerStairZ1 = 40.5f, TowerLandingZ = 36f;

    /// <summary>Menara raksasa di pusat kota. Pintu portal menghadap +Z, tangga besar naik ke teras di depannya.</summary>
    public static void GrandTower(GeoBag b)
    {
        // alas bertingkat
        b.Cyl(Sl.DarkStone, P(0f, 0f, 0f), 46.5f, 1.2f, 36, 45.5f, true);
        b.Cyl(Sl.Stone, P(0f, 1.2f, 0f), 44f, 1.2f, 36, 43f, true);
        b.Cyl(Sl.Stone, P(0f, 2.4f, 0f), 41.5f, 1.2f, 36, 40.5f, true);
        b.Cyl(Sl.Gold, P(0f, 3.55f, 0f), 40.6f, 0.1f, 36, 40.6f, true);

        // tubuh: lima tingkat ala kue pengantin
        float[] rad = { 26f, 22f, 18f, 14f, 10f };
        float[] hgt = { 24f, 22f, 20f, 18f, 12f };
        float y = TowerPlinthH;
        for (int t = 0; t < 5; t++)
        {
            float r = rad[t], h = hgt[t];
            b.Cyl(Sl.Stone, P(0f, y, 0f), r, h, 28, r * 0.97f, false);
            b.Cyl(Sl.DarkStone, P(0f, y + h - 0.3f, 0f), r + 1.9f, 1.1f, 28, r + 1.2f, true);
            b.Cyl(Sl.Gold, P(0f, y + h + 0.8f, 0f), r + 1.25f, 0.5f, 28, r + 1.25f, true);
            b.Cyl(Sl.DarkStone, P(0f, y + 0.0f, 0f), r + 0.5f, 1.6f, 28, r + 0.1f, true);

            // jendela tinggi bercahaya
            int nw = 12 - t * 2;
            for (int k = 0; k < nw; k++)
            {
                float a = (k + 0.5f) * 360f / nw;
                if (t == 0 && Mathf.Abs(Mathf.DeltaAngle(a, 0f)) < 24f) continue;      // area portal
                float rr = a * Mathf.Deg2Rad;
                b.Push(P(Mathf.Sin(rr) * (r * 0.97f + 0.1f), y + h * 0.5f, Mathf.Cos(rr) * (r * 0.97f + 0.1f)), a);
                b.Box(Sl.Glass, P(0f, 0f, 0.05f), new Vector3(1.8f, h * 0.5f, 0.3f));
                b.Box(Sl.Gold, P(0f, h * 0.25f + 0.25f, 0.1f), new Vector3(2.6f, 0.4f, 0.5f));
                b.Box(Sl.Gold, P(0f, -h * 0.25f - 0.15f, 0.12f), new Vector3(2.9f, 0.4f, 0.7f));
                b.Pop();
            }
            // lajur batu melingkar halus + konsol di bawah pita emas + spanduk
            for (float yy = y + 2.2f; yy < y + h - 1.5f; yy += 1.3f)
                b.Cyl(Sl.DarkStone, P(0f, yy, 0f), r * 0.97f + 0.05f, 0.09f, 28, r * 0.97f + 0.05f, true);
            for (int k = 0; k < 28; k++)
            {
                float ka = k * 360f / 28f;
                b.Push(P(Mathf.Sin(ka * Mathf.Deg2Rad) * (r + 0.45f), y + h - 1.2f, Mathf.Cos(ka * Mathf.Deg2Rad) * (r + 0.45f)), ka);
                b.Box(Sl.DarkStone, P(0f, 0f, 0f), new Vector3(0.7f, 0.9f, 0.8f));
                b.Pop();
            }
            for (int k = 0; k < 6 - t; k++)
            {
                float ka = (k + 0.25f) * 360f / (6 - t);
                if (t == 0 && Mathf.Abs(Mathf.DeltaAngle(ka, 0f)) < 30f) continue;
                b.Push(P(Mathf.Sin(ka * Mathf.Deg2Rad) * (r * 0.97f + 0.15f), 0f, Mathf.Cos(ka * Mathf.Deg2Rad) * (r * 0.97f + 0.15f)), ka);
                BuildKit.Banner(b, P(0f, y + h - 3f, 0f), 2.0f, Mathf.Min(10f, h - 6f), Sl.Cloth, true);
                b.Pop();
            }
            y += h;
        }
        float topY = y;

        // penopang melayang: delapan, di antara tingkat 1 dan 3
        for (int k = 0; k < 8; k++)
        {
            float a = (22.5f + k * 45f) * Mathf.Deg2Rad;
            Vector3 d = P(Mathf.Sin(a), 0f, Mathf.Cos(a));
            b.Push(P(0f, 0f, 0f), 0f);
            b.Beam(Sl.Stone, d * 33f + P(0f, TowerPlinthH, 0f), d * 22.5f + P(0f, TowerPlinthH + 44f, 0f), 2.0f, 2.4f);
            b.BoxB(Sl.DarkStone, d * 33f + P(0f, TowerPlinthH, 0f), new Vector3(3.6f, 5f, 3.6f), a * Mathf.Rad2Deg);
            b.Ball(Sl.Glow, d * 22.2f + P(0f, TowerPlinthH + 45.2f, 0f), 0.9f, 5, 8);
            b.Pop();
        }

        // portal besar di muka +Z
        float pz = 26f;
        b.Box(Sl.Dark, P(0f, TowerPlinthH + 9f, pz + 0.1f), new Vector3(13f, 18f, 0.6f));
        FillArch(b, Sl.Dark, P(0f, TowerPlinthH + 18f, pz + 0.4f), 6.5f, 14);
        b.QuadBoth(Sl.Glow, P(-4.8f, TowerPlinthH, pz + 0.55f), P(4.8f, TowerPlinthH, pz + 0.55f), P(4.8f, TowerPlinthH + 17f, pz + 0.55f), P(-4.8f, TowerPlinthH + 17f, pz + 0.55f));
        for (int k = 0; k < 10; k++)
        {
            float a0 = Mathf.PI * k / 10f, a1 = Mathf.PI * (k + 1) / 10f;
            Vector3 c = P(0f, TowerPlinthH + 17f, pz + 0.55f);
            Vector3 p0 = c + P(Mathf.Cos(a0) * 4.8f, Mathf.Sin(a0) * 4.8f, 0f), p1 = c + P(Mathf.Cos(a1) * 4.8f, Mathf.Sin(a1) * 4.8f, 0f);
            b.Tri(Sl.Glow, c, p1, p0);
            b.Tri(Sl.Glow, c, p0, p1);
        }
        ArchBeams(b, Sl.Gold, P(0f, TowerPlinthH + 18f, pz + 0.7f), 6.8f, 14, 1.0f, 1.0f);
        BuildKit.Column(b, P(-9.2f, TowerPlinthH, pz + 2f), 1.25f, 21.5f, Sl.Stone);
        BuildKit.Column(b, P(9.2f, TowerPlinthH, pz + 2f), 1.25f, 21.5f, Sl.Stone);
        b.Box(Sl.Gold, P(0f, TowerPlinthH + 22.4f, pz + 2f), new Vector3(23.5f, 1.2f, 3.4f));
        b.Box(Sl.Stone, P(0f, TowerPlinthH + 23.6f, pz + 2f), new Vector3(22.5f, 1.4f, 3.0f));
        b.Gable(Sl.RoofB, Sl.Stone, Sl.Gold, P(0f, TowerPlinthH + 24.3f, pz + 2f), 23.5f, 4.2f, 5.2f, 0.4f, false);
        b.Ball(Sl.Glow, P(0f, TowerPlinthH + 29f, pz + 3.1f), 1.5f, 6, 10);
        b.Box(Sl.Gold, P(0f, TowerPlinthH + 29f, pz + 2.6f), new Vector3(0.5f, 3.4f, 0.3f));

        // tangga besar ke teras
        b.Stairs(Sl.Stone, P(0f, 0f, TowerStairZ0), 22f, 18, 0.2f, 0.8f, 0f);
        for (int s = -1; s <= 1; s += 2)
        {
            b.Beam(Sl.Stone, P(s * 11.8f, 0.9f, TowerStairZ0 + 0.4f), P(s * 11.8f, TowerPlinthH + 0.9f, TowerStairZ1 - 0.4f), 1.2f, 1.8f);
            // perapian kristal di kaki tangga
            b.Cyl(Sl.Dark, P(s * 13.6f, 0f, TowerStairZ0 + 1.5f), 1.1f, 1.6f, 10, 1.4f, true);
            b.Ball(Sl.Glow, P(s * 13.6f, 2.1f, TowerStairZ0 + 1.5f), 0.9f, 5, 8, 1.4f);
        }

        // mahkota: menara kristal + cincin melayang
        b.Cyl(Sl.Gold, P(0f, topY, 0f), 9f, 1.0f, 20, 8f, true);
        b.Spire(Sl.Glow, P(0f, topY + 1f, 0f), 6.5f, 24f, 12, 0.05f);
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI / 3f;
            b.Cone(Sl.Glow, P(Mathf.Sin(a) * 11f, topY + 6f + (i % 2) * 3f, Mathf.Cos(a) * 11f), 0.9f, 4.5f, 5);
        }
        RingBeams(b, Sl.Gold, P(0f, topY + 12f, 0f), 12f, 28, 0.55f);
        RingBeams(b, Sl.Gold, P(0f, topY + 18f, 0f), 9f, 24, 0.5f);
        RingBeams(b, Sl.Glow, P(0f, topY + 24f, 0f), 6f, 20, 0.4f);
    }

    // ====================================================================== GUILD BESAR
    public const float GuildHallW = 56f, GuildHallD = 34f, GuildHallPorch = 11f;

    public static void GrandHall(GeoBag b)
    {
        float W = GuildHallW, D = GuildHallD, H = 15f, y0 = 1.2f;
        b.BoxB(Sl.DarkStone, P(0f, 0f, 0f), new Vector3(W + 4f, y0, D + 4f));
        FantasyKit.Wall(b, Sl.Stone, P(0f, y0, 0f), new Vector3(W, H, D), 1);
        b.BoxB(Sl.DarkStone, P(0f, y0 + H - 0.2f, 0f), new Vector3(W + 1.4f, 0.9f, D + 1.4f));
        b.BoxB(Sl.Gold, P(0f, y0 + H + 0.65f, 0f), new Vector3(W + 0.8f, 0.4f, D + 0.8f));
        b.BoxB(Sl.DarkStone, P(0f, y0 + 1.6f, 0f), new Vector3(W + 0.9f, 0.5f, D + 0.9f));
        b.Hip(Sl.Roof, Sl.Wood, P(0f, y0 + H + 1.05f, 0f), W + 2f, D + 2f, 6.5f, 0.3f);

        // kubah tengah
        float cy = y0 + H + 5f;
        b.Cyl(Sl.Stone, P(0f, cy - 1f, 0f), 10f, 6f, 24, 10f, false);
        b.Cyl(Sl.Gold, P(0f, cy + 4.6f, 0f), 10.4f, 0.5f, 24, 10.4f, true);
        b.Dome(Sl.Roof, P(0f, cy + 5f, 0f), 10.2f, 8, 24, 0.95f);
        b.Spire(Sl.Gold, P(0f, cy + 14.6f, 0f), 1.4f, 7f, 8);
        b.Ball(Sl.Glow, P(0f, cy + 22.2f, 0f), 0.8f, 5, 8);
        for (int k = 0; k < 8; k++)
        {
            float a = (k + 0.5f) * 45f;
            b.Push(P(Mathf.Sin(a * Mathf.Deg2Rad) * 10.05f, cy + 2f, Mathf.Cos(a * Mathf.Deg2Rad) * 10.05f), a);
            b.Box(Sl.Glass, P(0f, 0f, 0f), new Vector3(1.4f, 3.2f, 0.25f));
            b.Pop();
        }

        // serambi bertiang di muka
        float pz = D / 2f;
        float pd = GuildHallPorch;
        b.Stairs(Sl.Stone, P(0f, 0f, pz + pd + 1.2f), 30f, 6, 0.2f, 1.6f, 0f);
        b.BoxB(Sl.Stone, P(0f, 0f, pz + pd / 2f + 0.4f), new Vector3(38f, y0, pd));
        int nc = 8;
        float colH = 13.2f;
        for (int i = 0; i < nc; i++)
        {
            float x = -16.5f + i * 33f / (nc - 1);
            BuildKit.Column(b, P(x, y0, pz + pd - 1.2f), 0.85f, colH, Sl.Marble);
        }
        float ey = y0 + colH + 0.4f;
        b.BoxB(Sl.Marble, P(0f, ey, pz + pd / 2f + 0.4f), new Vector3(37.4f, 1.2f, pd));
        b.BoxB(Sl.Gold, P(0f, ey + 1.2f, pz + pd / 2f + 0.4f), new Vector3(37.8f, 0.3f, pd + 0.4f));
        b.Gable(Sl.Roof, Sl.Marble, Sl.Wood, P(0f, ey + 1.5f, pz + pd / 2f + 0.4f), 37.8f, pd, 5.6f, 0.3f, false);
        // lambang di pedimen
        float zf = pz + pd + 0.6f;
        b.Box(Sl.Gold, P(0f, ey + 4.1f, zf), new Vector3(3.4f, 3.6f, 0.2f));
        b.Box(Sl.ClothB, P(0f, ey + 4.1f, zf + 0.1f), new Vector3(2.6f, 2.8f, 0.15f));
        b.Ball(Sl.Glow, P(0f, ey + 4.1f, zf + 0.3f), 0.7f, 5, 8);

        // pintu dan jendela
        BuildKit.DoorFront(b, P(0f, y0, pz), 6.2f, 8.4f, true);
        foreach (float x in new[] { -24f, -17f, -10.5f, 10.5f, 17f, 24f })
            BuildKit.ArchWindow(b, P(x, y0 + 3.4f, pz), 3.0f, 8.4f, Sl.Gold);
        foreach (float x in new[] { -13.5f, 13.5f })
            BuildKit.Banner(b, P(x, y0 + 12.6f, pz + 0.1f), 2.6f, 7.5f, Sl.ClothB, true);

        // dua menara sudut
        foreach (float sx in new[] { -1f, 1f })
        {
            BuildKit.RoundTower(b, P(sx * (W / 2f - 2f), 0f, pz - 2f), 4.4f, 25f, Sl.Stone, Sl.DarkStone, Sl.RoofB, 13f, false, true, Sl.ClothB);
            BuildKit.RoundTower(b, P(sx * (W / 2f - 2f), 0f, -pz + 2f), 4.4f, 25f, Sl.Stone, Sl.DarkStone, Sl.RoofB, 13f, false, false, Sl.ClothB);
        }
        DungeonArt.GrandHall(b, W, D, H, y0, pz);
    }

    // ====================================================================== KOLOSEUM (ARENA)
    public const float ColiseumR = 36f;

    public static void Coliseum(GeoBag b)
    {
        const float R = 34f;
        const int N = 36;
        float ch = Chord(R, N);
        b.Cyl(Sl.DarkStone, P(0f, 0f, 0f), R + 2.2f, 1.0f, 36, R + 1.7f, true);
        b.Disc(Sl.Dirt, P(0f, 0f, 0f), 19f, 32, 1.02f);
        // tribun
        for (int k = 0; k < 4; k++)
            RingWall(b, k % 2 == 0 ? Sl.Stone : Sl.DarkStone, 21.2f + k * 3.4f, 3.5f, 0f, 4.2f + k * 3.2f, 28);
        // dinding luar
        RingWall(b, Sl.Stone, R, 2.2f, 0f, 18f, N);
        RingWall(b, Sl.Gold, R + 0.35f, 3f, 17.4f, 0.6f, N);
        RingWall(b, Sl.DarkStone, R + 0.2f, 2.8f, 0f, 1.6f, N);
        for (int i = 0; i < N; i++)
        {
            float a = (i + 0.5f) * 360f / N;
            float da = Mathf.Abs(Mathf.DeltaAngle(a, 0f));
            float rr = a * Mathf.Deg2Rad;
            Vector3 pos = P(Mathf.Sin(rr) * (R + 1.12f), 0f, Mathf.Cos(rr) * (R + 1.12f));
            // tiang antar-lengkung
            float ab = i * 360f / N * Mathf.Deg2Rad;
            b.Push(P(Mathf.Sin(ab) * (R + 1.1f), 0f, Mathf.Cos(ab) * (R + 1.1f)), i * 360f / N);
            b.BoxB(Sl.Marble, P(0f, 0f, 0f), new Vector3(0.9f, 17.6f, 0.9f));
            b.Pop();
            if (da < 11f) continue;
            b.Push(pos, a);
            b.Box(Sl.Dark, P(0f, 4.4f, 0f), new Vector3(ch * 0.52f, 5.6f, 0.3f));
            b.Box(Sl.Gold, P(0f, 7.4f, 0.05f), new Vector3(ch * 0.62f, 0.35f, 0.5f));
            b.Box(Sl.Dark, P(0f, 12.6f, 0f), new Vector3(ch * 0.52f, 4.6f, 0.3f));
            b.Box(Sl.Glass, P(0f, 12.6f, 0.1f), new Vector3(ch * 0.2f, 3.6f, 0.12f));
            b.Box(Sl.Gold, P(0f, 15.2f, 0.05f), new Vector3(ch * 0.62f, 0.35f, 0.5f));
            b.Pop();
            if (i % 3 == 0)
            {
                b.Push(P(Mathf.Sin(rr) * (R + 0.8f), 18.1f, Mathf.Cos(rr) * (R + 0.8f)), a);
                b.Ball(Sl.Glow, P(0f, 0.7f, 0f), 0.6f, 5, 8);
                b.Cyl(Sl.Gold, P(0f, 0f, 0f), 0.45f, 0.6f, 8, 0.3f, true);
                b.Pop();
            }
        }
        // gerbang utama di +Z
        float gz = R + 1.3f;
        b.Box(Sl.Dark, P(0f, 5.5f, gz), new Vector3(9f, 11f, 0.5f));
        FillArch(b, Sl.Dark, P(0f, 11f, gz + 0.1f), 4.5f, 12);
        ArchBeams(b, Sl.Gold, P(0f, 11f, gz + 0.3f), 4.9f, 12, 0.9f, 0.9f);
        BuildKit.Column(b, P(-6.2f, 0f, gz + 0.8f), 0.9f, 15f, Sl.Marble);
        BuildKit.Column(b, P(6.2f, 0f, gz + 0.8f), 0.9f, 15f, Sl.Marble);
        b.Box(Sl.Gold, P(0f, 15.6f, gz + 0.8f), new Vector3(15f, 1f, 2.2f));
        BuildKit.Banner(b, P(-9f, 16.5f, gz + 0.6f), 2.4f, 8f, Sl.ClothB, true);
        BuildKit.Banner(b, P(9f, 16.5f, gz + 0.6f), 2.4f, 8f, Sl.ClothB, true);
        b.Stairs(Sl.Stone, P(0f, 0f, gz + 7f), 12f, 5, 0.2f, 1.2f, 0f);
        // tiang bendera di puncak
        for (int k = 0; k < 4; k++)
        {
            float a = (k * 90f + 45f) * Mathf.Deg2Rad;
            BuildKit.Flag(b, P(Mathf.Sin(a) * R, 18f, Mathf.Cos(a) * R), 7f, 4f, 2.4f, Sl.ClothB);
        }
        // kristal besar di tengah arena
        b.Cyl(Sl.Dark, P(0f, 1f, 0f), 2.6f, 1.2f, 10, 2.0f, true);
        b.Cone(Sl.Glow, P(0f, 2.2f, 0f), 1.4f, 6f, 6);
        DungeonArt.Coliseum(b, R, N, ch);
    }

    // ====================================================================== PORTAL TELEPORT
    public static void PortalHub(GeoBag b)
    {
        b.Cyl(Sl.DarkStone, P(0f, 0f, 0f), 13f, 0.6f, 32, 12.6f, true);
        b.Cyl(Sl.Stone, P(0f, 0.6f, 0f), 11f, 0.5f, 32, 10.6f, true);
        b.Annulus(Sl.Gold, P(0f, 0f, 0f), 8.8f, 9.2f, 0f, 360f, 32, 1.13f);
        b.Annulus(Sl.Gold, P(0f, 0f, 0f), 5.6f, 5.9f, 0f, 360f, 28, 1.13f);
        for (int k = 0; k < 8; k++)
        {
            float a = k * 45f * Mathf.Deg2Rad;
            b.Cone(Sl.Glow, P(Mathf.Sin(a) * 7.2f, 1.1f, Mathf.Cos(a) * 7.2f), 0.35f, 1.0f, 4);
        }
        // lengkung portal (menghadap +Z)
        float ay = 1.1f;
        BuildKit.Column(b, P(-4.2f, ay, -3f), 0.8f, 9.5f, Sl.Stone);
        BuildKit.Column(b, P(4.2f, ay, -3f), 0.8f, 9.5f, Sl.Stone);
        ArchBeams(b, Sl.Gold, P(0f, ay + 9.8f, -3f), 4.6f, 14, 1.0f, 1.0f);
        FillArch(b, Sl.Glow, P(0f, ay + 9.8f, -3f), 4.2f, 14);
        b.QuadBoth(Sl.Glow, P(-4.2f, ay, -3f), P(4.2f, ay, -3f), P(4.2f, ay + 9.8f, -3f), P(-4.2f, ay + 9.8f, -3f));
        b.Ball(Sl.Glow, P(0f, ay + 15f, -3f), 0.9f, 5, 8);
        DungeonArt.PortalHub(b);
    }

    // ====================================================================== PROP
    public static void Obelisk(GeoBag b)
    {
        b.Cyl(Sl.DarkStone, P(0f, 0f, 0f), 2.2f, 0.9f, 4, 1.9f, true);
        b.Cyl(Sl.Stone, P(0f, 0.9f, 0f), 1.4f, 14f, 4, 0.8f, false);
        b.Cyl(Sl.Glow, P(0f, 5.6f, 0f), 1.12f, 0.5f, 4, 1.1f, true);
        b.Cyl(Sl.Gold, P(0f, 13.8f, 0f), 0.9f, 0.4f, 4, 0.9f, true);
        b.Cone(Sl.Gold, P(0f, 14.9f, 0f), 0.8f, 1.8f, 4);
        DungeonArt.Obelisk(b);
    }

    public static void CrystalLamp(GeoBag b)
    {
        b.Cyl(Sl.Stone, P(0f, 0f, 0f), 0.36f, 0.6f, 8, 0.26f, true);
        b.Cyl(Sl.Stone, P(0f, 0.6f, 0f), 0.14f, 3.3f, 8, 0.11f, true);
        b.Cyl(Sl.Gold, P(0f, 3.8f, 0f), 0.3f, 0.2f, 8, 0.22f, true);
        b.Cyl(Sl.Glow, P(0f, 4.0f, 0f), 0.0f, 0.55f, 6, 0.3f, false);
        b.Cone(Sl.Glow, P(0f, 4.55f, 0f), 0.3f, 0.7f, 6);
    }

    public static void CrystalCluster(GeoBag b, float scale)
    {
        b.Cyl(Sl.DarkStone, P(0f, 0f, 0f), 1.0f * scale, 0.4f * scale, 8, 0.8f * scale, true);
        b.Cone(Sl.Glow, P(0f, 0.3f * scale, 0f), 0.45f * scale, 2.6f * scale, 6);
        b.Cone(Sl.Glow, P(0.55f * scale, 0.3f * scale, 0.2f * scale), 0.3f * scale, 1.7f * scale, 5);
        b.Cone(Sl.Glow, P(-0.45f * scale, 0.3f * scale, -0.3f * scale), 0.3f * scale, 1.4f * scale, 5);
    }

    /// <summary>Deretan toko beratap (arkade). Lebar 30, dalam 9, muka menghadap +Z.</summary>
    public static void Arcade(GeoBag b)
    {
        float W = 30f, D = 9f, H = 5.2f;
        b.BoxB(Sl.DarkStone, P(0f, 0f, 0f), new Vector3(W + 1f, 0.5f, D + 1f));
        b.BoxB(Sl.Plaster, P(0f, 0.5f, -D / 2f + 0.6f), new Vector3(W, H, 1.2f));
        b.BoxB(Sl.PlasterB, P(-W / 2f + 0.6f, 0.5f, 0f), new Vector3(1.2f, H, D));
        b.BoxB(Sl.PlasterB, P(W / 2f - 0.6f, 0.5f, 0f), new Vector3(1.2f, H, D));
        for (int i = 0; i < 7; i++)
        {
            float x = -W / 2f + 1.5f + i * (W - 3f) / 6f;
            BuildKit.Column(b, P(x, 0.5f, D / 2f - 0.8f), 0.32f, H - 0.4f, Sl.Marble);
        }
        b.BoxB(Sl.Marble, P(0f, H + 0.1f, D / 2f - 0.8f), new Vector3(W, 0.7f, 1.2f));
        b.Gable(Sl.Roof, Sl.PlasterB, Sl.Wood, P(0f, H + 0.8f, 0f), W + 1f, D + 1f, 2.6f, 0.4f, true);
        for (int i = 0; i < 6; i++)
        {
            float x = -W / 2f + 3f + i * (W - 6f) / 5f;
            BuildKit.Awning(b, P(x, H - 0.8f, D / 2f - 0.6f), 3.6f, 2.0f, 0.9f, i % 2 == 0 ? Sl.Cloth : Sl.ClothB, Sl.Plaster);
            b.BoxB(Sl.Wood, P(x, 0.5f, D / 2f - 1.8f), new Vector3(3.0f, 1.0f, 1.0f));
            BuildKit.Crate(b, P(x - 0.8f, 1.5f, D / 2f - 1.8f), 0.6f, 12f * i);
            b.Ball(Sl.Glow, P(x + 0.8f, 1.35f, D / 2f - 1.8f), 0.28f, 4, 6);
        }
        DungeonArt.Arcade(b, W, D, H);
    }

    /// <summary>Kolam air kristal dengan pilar tengah.</summary>
    public static void CrystalFountain(GeoBag b, float scale)
    {
        b.Cyl(Sl.DarkStone, P(0f, 0f, 0f), 6.4f * scale, 0.7f, 28, 6.1f * scale, true);
        b.Cyl(Sl.Stone, P(0f, 0.7f, 0f), 6.0f * scale, 0.5f, 28, 5.7f * scale, false);
        b.Disc(Sl.Water, P(0f, 0f, 0f), 5.6f * scale, 28, 1.05f);
        b.Cyl(Sl.Stone, P(0f, 0.7f, 0f), 1.6f, 1.6f, 12, 1.1f, true);
        b.Cyl(Sl.Gold, P(0f, 2.3f, 0f), 0.5f, 1.4f, 10, 0.35f, true);
        b.Cone(Sl.Glow, P(0f, 3.5f, 0f), 0.8f, 2.8f, 6);
        for (int k = 0; k < 6; k++)
        {
            float a = k * Mathf.PI / 3f;
            b.Cone(Sl.Glow, P(Mathf.Sin(a) * 2.6f * scale, 1.0f, Mathf.Cos(a) * 2.6f * scale), 0.3f, 1.4f, 5);
        }
        DungeonArt.CrystalFountain(b, scale);
    }
}
