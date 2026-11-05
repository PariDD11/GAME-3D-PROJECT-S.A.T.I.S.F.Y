using System.Collections.Generic;
using UnityEngine;

/// <summary>Bagian kedua LandmarkKit: Barak, Istal, Menara Penyihir, Kastil, Kediaman Bangsawan, Kincir Angin, Pertanian.</summary>
public static partial class LandmarkKit
{
    public const float CastleW = 76f, CastleD = 64f;
    public const float ManorW = 26f, ManorD = 17f;

    // ====================================================================== BARAK
    public static void BarracksOld(GeoBag b)
    {
        float w = BarrW, d = BarrD, h = 8.5f, y0 = 0.8f;
        b.BoxB(Sl.DarkStone, P(0, 0, 0), new Vector3(w + 0.8f, y0, d + 0.8f));
        b.BoxB(Sl.Stone, P(0, y0, 0), new Vector3(w, h, d));
        b.BoxB(Sl.DarkStone, P(0, y0 + h, 0), new Vector3(w + 1.2f, 0.7f, d + 1.2f));
        float yt = y0 + h + 0.7f;
        Vector3 a = P(-w / 2f - 0.4f, yt, d / 2f + 0.4f), c = P(w / 2f + 0.4f, yt, d / 2f + 0.4f), e = P(w / 2f + 0.4f, yt, -d / 2f - 0.4f), f = P(-w / 2f - 0.4f, yt, -d / 2f - 0.4f);
        b.Crenellate(Sl.Stone, a, c, 1.5f, 0.8f, 1.8f, 1.2f);
        b.Crenellate(Sl.Stone, c, e, 1.5f, 0.8f, 1.8f, 1.2f);
        b.Crenellate(Sl.Stone, e, f, 1.5f, 0.8f, 1.8f, 1.2f);
        b.Crenellate(Sl.Stone, f, a, 1.5f, 0.8f, 1.8f, 1.2f);
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                BuildKit.RoundTower(b, P(sx * (w / 2f), 0f, sz * (d / 2f)), 2.9f, 15f, Sl.Stone, Sl.DarkStone, Sl.Roof, 9f, false, true, Sl.Cloth);
        // gerbang masuk
        b.BoxB(Sl.DarkStone, P(0, 0, d / 2f + 0.3f), new Vector3(7.6f, 6.4f, 0.8f));
        BuildKit.DoorFront(b, P(0, y0, d / 2f + 0.6f), 3.2f, 3.8f, true);
        b.Box(Sl.Gold, P(0, y0 + 6.2f, d / 2f + 0.15f), new Vector3(2.0f, 2.4f, 0.14f));
        b.Box(Sl.Cloth, P(0, y0 + 6.2f, d / 2f + 0.24f), new Vector3(1.5f, 1.9f, 0.1f));
        foreach (float x in new[] { -9f, -4.8f, 4.8f, 9f })
        {
            BuildKit.Slit(b, P(x, y0 + 4.6f, d / 2f + 0.02f), 2.4f);
            BuildKit.Slit(b, P(x, y0 + 7.2f, d / 2f + 0.02f), 1.8f);
        }
        BuildKit.Banner(b, P(-8f, y0 + 6.6f, d / 2f + 0.1f), 1.6f, 4.0f, Sl.Cloth, true);
        BuildKit.Banner(b, P(8f, y0 + 6.6f, d / 2f + 0.1f), 1.6f, 4.0f, Sl.Cloth, true);
        // tempat latihan: boneka sasaran & rak senjata
        for (int i = 0; i < 3; i++)
        {
            float x = -8f + i * 4.5f, z = d / 2f + 4.2f;
            b.Cyl(Sl.Wood, P(x, 0f, z), 0.12f, 2.2f, 6, 0.12f, true);
            b.Beam(Sl.Wood, P(x - 0.8f, 1.7f, z), P(x + 0.8f, 1.7f, z), 0.12f, 0.12f);
            b.Ball(Sl.Thatch, P(x, 2.35f, z), 0.3f, 4, 6);
            b.Box(Sl.Cloth, P(x, 1.4f, z), new Vector3(0.6f, 0.9f, 0.25f));
        }
        b.Box(Sl.Wood, P(9f, 1.0f, d / 2f + 2.4f), new Vector3(3.2f, 0.18f, 0.5f));
        for (int i = 0; i < 5; i++)
            b.Beam(Sl.Metal, P(7.8f + i * 0.6f, 0.2f, d / 2f + 2.4f), P(7.8f + i * 0.6f, 2.6f, d / 2f + 2.2f), 0.12f, 0.06f);
        BuildKit.Chimney(b, P(-w / 2f + 3f, yt, -3f), 1.2f, 2f);
    }

    // ====================================================================== ISTAL
    public static void StablesOld(GeoBag b)
    {
        float w = StableW, d = StableD, h = 5.2f, y0 = 0.6f, rise = 6.2f;
        b.BoxB(Sl.Stone, P(0, 0, 0), new Vector3(w + 0.4f, y0, d + 0.4f));
        b.BoxB(Sl.Wood, P(0, y0, -1f), new Vector3(w, h, d - 2f));
        b.Push(P(0, y0, d / 2f - 1f), 0f); BuildKit.TimberFront(b, w, 0f, h, 0f, false); b.Pop();
        b.Gable(Sl.RoofB, Sl.Wood, Sl.Wood, P(0, y0 + h, -1f), w, d - 2f, rise, 0.9f, true);
        // kandang terbuka di depan: tiang + sekat
        for (int i = 0; i < 5; i++)
        {
            float x = -w / 2f + 2f + i * (w - 4f) / 4f;
            b.BoxB(Sl.Wood, P(x, 0f, d / 2f - 0.3f), new Vector3(0.4f, 3.6f, 0.4f));
            b.Box(Sl.Wood, P(x, 0.9f, d / 2f - 3.0f), new Vector3(0.18f, 1.8f, 5.4f));
        }
        b.Box(Sl.Wood, P(0, 3.6f, d / 2f - 0.3f), new Vector3(w - 2f, 0.4f, 0.5f));
        b.Quad(Sl.RoofB, P(-w / 2f + 1f, 3.4f, d / 2f + 0.8f), P(w / 2f - 1f, 3.4f, d / 2f + 0.8f), P(w / 2f - 1f, 5.2f, -1f), P(-w / 2f + 1f, 5.2f, -1f));
        // jerami
        for (int i = 0; i < 4; i++) PropKit.Haystack(b, P(-9f + i * 5.6f, 0f, d / 2f - 4.2f));
        // dua kuda sederhana
        foreach (float x in new[] { -4f, 6f })
            Horse(b, P(x, 0f, d / 2f + 3.6f), x < 0f ? 30f : -20f);
        // palang air
        b.Box(Sl.Wood, P(-w / 2f + 2f, 0.45f, d / 2f + 1.2f), new Vector3(3.4f, 0.8f, 0.9f));
        b.Box(Sl.Water, P(-w / 2f + 2f, 0.88f, d / 2f + 1.2f), new Vector3(3.0f, 0.04f, 0.6f));
        // pintu loteng
        b.Box(Sl.Wood, P(0, y0 + h + 1.4f, d / 2f - 3.1f), new Vector3(1.8f, 2.0f, 0.1f));
        BuildKit.HangingSign(b, P(w / 2f - 0.4f, y0 + 4.0f, d / 2f - 0.8f), 1.8f, Sl.Metal);
    }

    public static void Horse(GeoBag b, Vector3 baseC, float yaw)
    {
        b.Push(baseC, yaw);
        b.Box(Sl.WoodLight, P(0, 1.55f, 0), new Vector3(0.9f, 0.95f, 2.2f));
        b.Beam(Sl.WoodLight, P(0, 1.9f, 0.9f), P(0, 2.7f, 1.45f), 0.5f, 0.5f);
        b.Box(Sl.WoodLight, P(0, 2.8f, 1.65f), new Vector3(0.38f, 0.45f, 0.8f));
        b.Box(Sl.Wood, P(0, 2.55f, 0.78f), new Vector3(0.14f, 0.9f, 0.5f));
        b.Beam(Sl.Wood, P(0, 1.8f, -1.05f), P(0, 0.9f, -1.5f), 0.16f, 0.16f);
        foreach (float x in new[] { -0.28f, 0.28f })
            foreach (float z in new[] { -0.8f, 0.8f })
                b.Cyl(Sl.WoodLight, P(x, 0f, z), 0.1f, 1.15f, 6, 0.08f, true);
        b.Pop();
    }

    // ====================================================================== MENARA PENYIHIR
    public static void MageTower(GeoBag b)
    {
        float r = MageR, h = 38f;
        b.Cyl(Sl.DarkStone, P(0, 0, 0), r + 1.2f, 2.0f, 16, r + 0.4f, true);
        b.Cyl(Sl.Stone, P(0, 2f, 0), r + 0.3f, h * 0.55f, 16, r - 1.3f, false);
        b.Cyl(Sl.PlasterB, P(0, 2f + h * 0.55f, 0), r - 1.3f, h * 0.35f, 16, r - 2.0f, false);
        for (int i = 1; i <= 5; i++)
        {
            float t = i / 6f;
            float rr = Mathf.Lerp(r + 0.3f, r - 2.0f, t) + 0.4f;
            b.Cyl(Sl.Marble, P(0, 2f + h * 0.9f * t, 0), rr, 0.5f, 16, rr, true);
        }
        // balkon pengamat
        float by = 2f + h * 0.9f;
        b.Cyl(Sl.Marble, P(0, by, 0), r - 2.0f, 1.0f, 16, r + 1.2f, true);
        b.Cyl(Sl.Marble, P(0, by + 1.0f, 0), r + 1.2f, 0.4f, 16, r + 1.2f, true);
        for (int i = 0; i < 16; i++)
        {
            float a = i / 16f * Mathf.PI * 2f;
            b.Cyl(Sl.Gold, P(Mathf.Sin(a) * (r + 0.9f), by + 1.4f, Mathf.Cos(a) * (r + 0.9f)), 0.07f, 1.0f, 5, 0.06f, true);
        }
        // menara puncak berkubah + kristal
        b.Cyl(Sl.PlasterB, P(0, by + 1.4f, 0), r - 2.6f, 5f, 16, r - 3.0f, false);
        b.Cyl(Sl.RoofB, P(0, by + 6.2f, 0), r - 1.4f, 0.6f, 16, r - 2.2f, false);
        b.Spire(Sl.RoofB, P(0, by + 6.6f, 0), r - 2.2f, 11f, 16, 0.05f);
        b.Ball(Sl.Glow, P(0, by + 20f, 0), 0.9f, 5, 8);
        b.Ball(Sl.Glow, P(0, by + 21.8f, 0), 0.5f, 4, 6);
        // jendela spiral
        for (int i = 0; i < 9; i++)
        {
            float a = i * 1.2f;
            float t = (i + 0.5f) / 10f;
            float rr = Mathf.Lerp(r + 0.3f, r - 2.0f, t * 0.9f);
            b.Push(P(Mathf.Sin(a) * (rr - 0.12f), 4.5f + i * 3.5f, Mathf.Cos(a) * (rr - 0.12f)), a * Mathf.Rad2Deg);
            BuildKit.ArchWindow(b, Vector3.zero, 1.2f, 2.6f, Sl.Marble);
            b.Pop();
        }
        // pintu + tangga
        b.Push(P(0, 0, r + 0.15f), 0f);
        BuildKit.DoorFront(b, P(0, 1.0f, 0f), 2.4f, 3.4f, true);
        b.Stairs(Sl.Stone, P(0, 0, 2.2f), 3.4f, 2, 0.5f, 0.6f, 0f);
        b.Pop();
        // menara sudut kecil
        foreach (float a in new[] { 0.9f, 2.5f, 4.1f })
            BuildKit.RoundTower(b, P(Mathf.Sin(a) * (r + 2.2f), 0f, Mathf.Cos(a) * (r + 2.2f)), 1.8f, 11f, Sl.Stone, Sl.DarkStone, Sl.RoofB, 7f, false, false, Sl.ClothB);
        BuildKit.Banner(b, P(0f, 14f, r - 1.2f), 1.8f, 5f, Sl.ClothB, true);
    }

    // ====================================================================== KASTIL
    public static void Castle(GeoBag b)
    {
        float W = CastleW, D = CastleD;
        // teras bertingkat
        b.BoxB(Sl.DarkStone, P(0, 0, 0), new Vector3(W + 6f, 1.6f, D + 6f));
        b.BoxB(Sl.Stone, P(0, 1.6f, 0), new Vector3(W + 5.2f, 1.4f, D + 5.2f));
        float y0 = 3.0f;

        // ---- keep utama
        float kw = 22f, kd = 20f, kh = 34f, kz = -6f;
        b.BoxB(Sl.Marble, P(0, y0, kz), new Vector3(kw, kh, kd));
        foreach (float yy in new[] { 12f, 22f, 31f })
            b.BoxB(Sl.DarkStone, P(0, y0 + yy, kz), new Vector3(kw + 0.5f, 0.6f, kd + 0.5f));
        b.Hip(Sl.RoofB, Sl.Wood, P(0, y0 + kh, kz), kw, kd, 10f, 1.2f);
        BuildKit.Finial(b, P(0, y0 + kh + 10f, kz), 4f, Sl.Gold);
        BuildKit.Flag(b, P(0, y0 + kh + 14f, kz), 5f, 4.2f, 2.4f, Sl.Cloth);
        for (int i = 0; i < 3; i++)
            foreach (float x in new[] { -6f, 0f, 6f })
            {
                BuildKit.ArchWindow(b, P(x, y0 + 4f + i * 9.5f, kz + kd / 2f + 0.02f), 2.0f, 4.4f, Sl.Gold);
                b.Push(P(x, y0 + 4f + i * 9.5f, kz - kd / 2f), 180f); BuildKit.ArchWindow(b, Vector3.zero, 2.0f, 4.4f, Sl.Gold); b.Pop();
            }
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                BuildKit.RoundTower(b, P(sx * kw / 2f, y0, kz + sz * kd / 2f), 3.4f, kh + 8f, Sl.Marble, Sl.DarkStone, Sl.RoofB, 15f, false, true, Sl.Cloth);

        // ---- sayap aula kiri-kanan
        foreach (float sx in new[] { -1f, 1f })
        {
            float wx = sx * 27f;
            FantasyKit.Wall(b, Sl.Marble, P(wx, y0, 3f), new Vector3(24f, 17f, 26f), 1);
            b.BoxB(Sl.DarkStone, P(wx, y0 + 9f, 3f), new Vector3(24.5f, 0.5f, 26.5f));
            b.Gable(Sl.RoofB, Sl.Marble, Sl.Wood, P(wx, y0 + 17f, 3f), 24f, 26f, 9.5f, 1.0f, false);
            BuildKit.Finial(b, P(wx, y0 + 26.5f, 16.5f), 2.4f, Sl.Gold);
            foreach (float x in new[] { -6.5f, 0f, 6.5f })
            {
                BuildKit.ArchWindow(b, P(wx + x, y0 + 3f, 16.02f), 2.2f, 5.2f, Sl.Gold);
                BuildKit.ArchWindow(b, P(wx + x, y0 + 11f, 16.02f), 1.8f, 3.6f, Sl.Gold);
            }
            for (int i = 0; i < 3; i++)
            {
                b.Push(P(wx + sx * 12f, y0 + 3f, -4f + i * 7f), sx > 0f ? 90f : -90f);
                BuildKit.ArchWindow(b, Vector3.zero, 2.0f, 5.0f, Sl.Gold);
                b.Pop();
            }
            // menara kecil di ujung
            BuildKit.RoundTower(b, P(sx * 35.5f, y0, -9f), 4.6f, 27f, Sl.Marble, Sl.DarkStone, Sl.RoofB, 15f, false, true, Sl.Cloth);
            BuildKit.RoundTower(b, P(sx * 35.5f, y0, 16f), 4.6f, 27f, Sl.Marble, Sl.DarkStone, Sl.RoofB, 15f, false, true, Sl.Cloth);
            // balustrade teras
            BuildKit.Balustrade(b, P(wx - 11f, y0, 17.2f), 22f, 1.1f, Sl.Marble);
        }

        // ---- gerbang depan (bangunan masuk)
        float gz = 22f;
        FantasyKit.Wall(b, Sl.Marble, P(0, y0, gz), new Vector3(18f, 15f, 10f), 1);
        b.BoxB(Sl.DarkStone, P(0, y0 + 11f, gz), new Vector3(18.5f, 0.6f, 10.5f));
        b.Gable(Sl.RoofB, Sl.Marble, Sl.Wood, P(0, y0 + 15f, gz), 18f, 10f, 8f, 0.8f, false);
        BuildKit.Finial(b, P(0, y0 + 23f, gz + 5.5f), 2.6f, Sl.Gold);
        BuildKit.DoorFront(b, P(0, y0, gz + 5f), 5.6f, 7.2f, true);
        b.Box(Sl.Wood, P(0, y0 + 3.6f, gz + 5.12f), new Vector3(5.6f, 7.2f, 0.12f));
        b.Box(Sl.Gold, P(0, y0 + 12.2f, gz + 5.1f), new Vector3(3.0f, 3.4f, 0.16f));
        b.Box(Sl.Cloth, P(0, y0 + 12.2f, gz + 5.2f), new Vector3(2.3f, 2.7f, 0.1f));
        b.Box(Sl.Gold, P(0, y0 + 12.2f, gz + 5.28f), new Vector3(0.4f, 2.2f, 0.06f));
        b.Box(Sl.Gold, P(0, y0 + 12.8f, gz + 5.28f), new Vector3(1.7f, 0.36f, 0.06f));
        foreach (float x in new[] { -6.2f, 6.2f })
        {
            BuildKit.Banner(b, P(x, y0 + 12.5f, gz + 5.1f), 1.8f, 7.5f, Sl.Cloth, true);
            BuildKit.Lantern(b, P(x * 0.55f, y0 + 5.2f, gz + 5.5f));
        }
        // tangga megah: turun dari tepi depan teras ke tanah (anak tangga teratas rapat dengan tepi teras)
        float edgeZ = D / 2f + 2.6f;                       // tepi depan lapisan batu teras
        float stairTopZ = edgeZ + 0.7f;
        b.Stairs(Sl.Marble, P(0, 0, stairTopZ + 5f * 1.4f), 16f, 6, 0.5f, 1.4f, 0f);
        foreach (float sx in new[] { -1f, 1f })
        {
            b.BoxB(Sl.Marble, P(sx * 8.4f, 0f, stairTopZ + 2.5f * 1.4f), new Vector3(0.8f, 3.0f, 8.4f));
            PropKit.KnightStatue(b, P(sx * 11.5f, y0, gz + 5f + 3.5f), 180f);
        }

        // ---- tembok tirai + menara sudut besar
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                BuildKit.RoundTower(b, P(sx * (W / 2f - 3.5f), y0, sz * (D / 2f - 3.5f)), 5.0f, 30f, Sl.Marble, Sl.DarkStone, Sl.RoofB, 17f, false, true, Sl.Cloth);
        b.BoxB(Sl.Marble, P(0, y0, -D / 2f + 2f), new Vector3(W - 8f, 13f, 2.4f));
        b.Crenellate(Sl.Marble, P(-W / 2f + 5f, y0 + 13f, -D / 2f + 2f), P(W / 2f - 5f, y0 + 13f, -D / 2f + 2f), 1.4f, 0.9f, 1.8f, 1.2f);
        // taman di dalam teras: pagar rendah depan
        BuildKit.Balustrade(b, P(-W / 2f + 6f, y0, D / 2f - 0.5f), W / 2f - 18f, 1.0f, Sl.Marble);
        BuildKit.Balustrade(b, P(12f, y0, D / 2f - 0.5f), W / 2f - 18f, 1.0f, Sl.Marble);
            // hiasan taman: semak bunga di tepi teras, tanaman rambat di menara depan
        for (int i = 0; i < 6; i++)
            foreach (float sx in new[] { -1f, 1f })
                AnimeKit.FlowerBush(b, P(sx * (12f + i * 5.2f), y0, D / 2f - 0.2f), 0.9f, (i % 2 == 0) ? Sl.Blossom : Sl.Gold);
        foreach (float sx in new[] { -1f, 1f })
            AnimeKit.Ivy(b, P(sx * (W / 2f - 3.5f), y0, D / 2f - 3.5f + 5.05f), 14f, 3.2f, 3 + (int)sx);
    }

    // ====================================================================== KEDIAMAN BANGSAWAN
    public static void Manor(GeoBag b, int variant)
    {
        float w = ManorW, d = ManorD, fh = 4.6f, y0 = 1.0f;
        b.BoxB(Sl.DarkStone, P(0, 0, 0), new Vector3(w + 0.8f, y0, d + 0.8f));
        b.BoxB(Sl.Marble, P(0, y0, 0), new Vector3(w, fh * 2f, d));
        b.Box(Sl.Gold, P(0, y0 + fh, d / 2f + 0.06f), new Vector3(w + 0.2f, 0.2f, 0.12f));
        // sudut batu (quoin)
        foreach (float sx in new[] { -1f, 1f })
            for (int i = 0; i < 8; i++)
                b.BoxB(Sl.Stone, P(sx * (w / 2f), y0 + i * 1.15f, d / 2f), new Vector3(0.7f, 0.95f, 0.5f));
        float yT = y0 + 2f * fh;
        b.Hip(Sl.RoofB, Sl.Wood, P(0, yT, 0), w, d, 6.4f, 1.0f);
        // pedimen tengah menonjol
        b.BoxB(Sl.Marble, P(0, 0, d / 2f + 1.4f), new Vector3(9.5f, y0 + 2f * fh, 2.8f));   // dari tanah (tidak melayang 1 m)
        b.Gable(Sl.RoofB, Sl.Marble, Sl.Wood, P(0, yT, d / 2f + 1.4f), 9.5f, 2.8f, 3.4f, 0.4f, false);
        b.Box(Sl.Gold, P(0, yT + 1.4f, d / 2f + 2.82f), new Vector3(1.3f, 1.5f, 0.1f));
        BuildKit.DoorFront(b, P(0, y0, d / 2f + 2.8f), 2.6f, 3.4f, true);
        // balkon lantai 2 bertumpu pada dua tiang; tangga menempel langsung ke pintu (tanpa celah)
        float bz = d / 2f + 2.8f;
        b.BoxB(Sl.Marble, P(0, y0 + fh - 0.3f, bz + 0.8f), new Vector3(9.5f, 0.3f, 1.6f));
        foreach (float sx in new[] { -1f, 1f })
            BuildKit.Column(b, P(sx * 4.2f, 0f, bz + 1.3f), 0.4f, y0 + fh - 0.3f, Sl.Marble);
        b.Stairs(Sl.Marble, P(0, 0, bz + 0.35f + 2f * 0.7f), 6.6f, 3, 0.33f, 0.7f, 0f);
        BuildKit.Balustrade(b, P(-4.5f, y0 + fh, bz + 1.45f), 9f, 1.0f, Sl.Marble);
        BuildKit.DoorFront(b, P(0, y0 + fh, d / 2f + 2.8f), 1.5f, 2.6f, true);
        foreach (float x in new[] { -10f, -6.4f, 6.4f, 10f })
        {
            BuildKit.ArchWindow(b, P(x, y0 + 0.8f, d / 2f + 0.02f), 1.5f, 3.3f, Sl.Gold);
            BuildKit.ArchWindow(b, P(x, y0 + fh + 0.8f, d / 2f + 0.02f), 1.4f, 3.1f, Sl.Gold);
        }
        for (int i = 0; i < 3; i++)
        {
            float z = -4.5f + i * 4.5f;
            b.Push(P(w / 2f, y0 + 0.8f, z), 90f); BuildKit.ArchWindow(b, Vector3.zero, 1.4f, 3.1f, Sl.Gold); b.Pop();
            b.Push(P(-w / 2f, y0 + 0.8f, z), -90f); BuildKit.ArchWindow(b, Vector3.zero, 1.4f, 3.1f, Sl.Gold); b.Pop();
            b.Push(P(w / 2f, y0 + fh + 0.8f, z), 90f); BuildKit.ArchWindow(b, Vector3.zero, 1.4f, 3.1f, Sl.Gold); b.Pop();
            b.Push(P(-w / 2f, y0 + fh + 0.8f, z), -90f); BuildKit.ArchWindow(b, Vector3.zero, 1.4f, 3.1f, Sl.Gold); b.Pop();
        }
        BuildKit.Chimney(b, P(-w / 2f + 3f, yT - 0.5f, 0f), 1.4f, 5.5f);
        BuildKit.Chimney(b, P(w / 2f - 3f, yT - 0.5f, 0f), 1.4f, 5.5f);
        if (variant % 2 == 0)
        {
            // menara kubah di sudut
            BuildKit.RoundTower(b, P(-w / 2f, 0f, -d / 2f + 1f), 3.1f, 14f, Sl.Marble, Sl.DarkStone, Sl.RoofB, 9f, false, true, Sl.ClothB);
            BuildKit.RoundTower(b, P(w / 2f, 0f, -d / 2f + 1f), 3.1f, 14f, Sl.Marble, Sl.DarkStone, Sl.RoofB, 9f, false, true, Sl.ClothB);
        }
        else
        {
            // sayap rumah kaca
            b.BoxB(Sl.Marble, P(w / 2f + 4f, 0f, -1f), new Vector3(8f, 1.2f, 10f));
            // dinding kaca di tiga sisi (depan, belakang, ujung) + rangka emas, supaya atap kaca tidak melayang
            float gx0 = w / 2f, gx1 = w / 2f + 8f, gz0 = -6f, gz1 = 4f, gy0 = 1.2f, gy1 = 5.5f;
            b.QuadBoth(Sl.Glass, P(gx0, gy0, gz1), P(gx1, gy0, gz1), P(gx1, gy1, gz1), P(gx0, gy1, gz1));
            b.QuadBoth(Sl.Glass, P(gx0, gy0, gz0), P(gx1, gy0, gz0), P(gx1, gy1, gz0), P(gx0, gy1, gz0));
            b.QuadBoth(Sl.Glass, P(gx1, gy0, gz0), P(gx1, gy0, gz1), P(gx1, gy1, gz1), P(gx1, gy1, gz0));
            for (int gi = 0; gi <= 4; gi++)
            {
                float gxx = gx0 + gi * 2f;
                b.Box(Sl.Gold, P(gxx, (gy0 + gy1) / 2f, gz1), new Vector3(0.14f, gy1 - gy0, 0.14f));
                b.Box(Sl.Gold, P(gxx, (gy0 + gy1) / 2f, gz0), new Vector3(0.14f, gy1 - gy0, 0.14f));
            }
            for (int gi = 1; gi <= 4; gi++)
                b.Box(Sl.Gold, P(gx1, (gy0 + gy1) / 2f, gz0 + gi * 2.0f), new Vector3(0.14f, gy1 - gy0, 0.14f));
            foreach (float gyy in new[] { gy0 + 0.1f, 3.3f, gy1 - 0.1f })
            {
                b.Box(Sl.Gold, P((gx0 + gx1) / 2f, gyy, gz1), new Vector3(gx1 - gx0, 0.12f, 0.14f));
                b.Box(Sl.Gold, P((gx0 + gx1) / 2f, gyy, gz0), new Vector3(gx1 - gx0, 0.12f, 0.14f));
                b.Box(Sl.Gold, P(gx1, gyy, (gz0 + gz1) / 2f), new Vector3(0.14f, 0.12f, gz1 - gz0));
            }
            b.Gable(Sl.Glass, Sl.Glass, Sl.Gold, P(w / 2f + 4f, 5.5f, -1f), 8f, 10f, 2.6f, 0.2f, true);
            b.Dome(Sl.Gold, P(0, yT + 6.6f, 0), 1.1f, 3, 8);
        }
        foreach (float x in new[] { -9f, 9f })
            BuildKit.Banner(b, P(x, y0 + 7.8f, d / 2f + 0.1f), 1.5f, 4.4f, Sl.ClothB, true);
    }

    // ====================================================================== PERTANIAN
    public static void WindmillBodyOld(GeoBag b)
    {
        b.Cyl(Sl.Stone, P(0, 0, 0), 5.2f, 1.5f, 14, 4.9f, true);
        b.Cyl(Sl.Plaster, P(0, 1.5f, 0), 4.9f, 15.5f, 14, 3.3f, false);
        for (int i = 1; i < 4; i++)
        {
            float t = i / 4f;
            b.Cyl(Sl.Wood, P(0, 1.5f + 15.5f * t, 0), Mathf.Lerp(4.9f, 3.3f, t) + 0.1f, 0.35f, 14, Mathf.Lerp(4.9f, 3.3f, t) + 0.1f, true);
        }
        b.Cyl(Sl.Wood, P(0, 17f, 0), 3.6f, 0.7f, 14, 3.9f, true);
        b.Cyl(Sl.RoofB, P(0, 17.7f, 0), 3.9f, 0.5f, 14, 3.4f, false);
        b.Cone(Sl.RoofB, P(0, 18.1f, 0), 3.4f, 5.2f, 14);
        BuildKit.Finial(b, P(0, 23.2f, 0), 1.6f, Sl.Gold);
        b.Box(Sl.Wood, P(0, 15.5f, 3.6f), new Vector3(1.0f, 1.0f, 1.5f));    // poros
        BuildKit.DoorFront(b, P(0, 1.2f, 4.7f), 1.6f, 2.5f, true);
        foreach (float a in new[] { 1.6f, 3.2f, 4.7f })
        {
            b.Push(P(Mathf.Sin(a) * 4.0f, 8.5f, Mathf.Cos(a) * 4.0f), a * Mathf.Rad2Deg);
            BuildKit.Window(b, Vector3.zero, 0.9f, 1.2f, true, false);
            b.Pop();
        }
    }

    public static void WindmillBlades(GeoBag b)
    {
        b.Box(Sl.Metal, P(0, 0, 0.1f), new Vector3(0.9f, 0.9f, 1.6f));   // poros kincir
        for (int k = 0; k < 4; k++)
        {
            float a = k * Mathf.PI / 2f;
            Vector3 dir = P(Mathf.Cos(a), Mathf.Sin(a), 0f);
            Vector3 perp = P(-Mathf.Sin(a), Mathf.Cos(a), 0f);
            b.Beam(Sl.Wood, dir * 0.8f, dir * 13.5f, 0.35f, 0.3f, Vector3.forward);
            // layar kain
            Vector3 q0 = dir * 2.5f + perp * 0.3f, q1 = dir * 13f + perp * 0.3f, q2 = dir * 13f + perp * 3.2f, q3 = dir * 2.5f + perp * 3.2f;
            b.QuadBoth(Sl.PlasterB, q0, q1, q2, q3);
            for (int i = 0; i < 6; i++)
                b.Beam(Sl.Wood, dir * (2.5f + i * 2f) + perp * 0.2f, dir * (2.5f + i * 2f) + perp * 3.3f, 0.12f, 0.12f, Vector3.forward);
        }
    }

    public static void Farmhouse(GeoBag b)
    {
        float w = 11f, d = 7.5f, h = 3.1f, y0 = 0.7f;
        b.BoxB(Sl.Stone, P(0, 0, 0), new Vector3(w + 0.4f, y0, d + 0.4f));
        b.BoxB(Sl.Plaster, P(0, y0, 0), new Vector3(w, h, d));
        b.BoxB(Sl.Plaster, P(-w / 2f + 2.5f, y0, d / 2f + 2.5f - 0.2f), new Vector3(5f, h, 5f));
        b.Push(P(0, y0, d / 2f), 0f); BuildKit.TimberFront(b, w, 0f, h, 0f, false); b.Pop();
        b.Gable(Sl.Thatch, Sl.Plaster, Sl.Wood, P(0, y0 + h, 0f), w, d, 4.2f, 0.9f, true);
        b.Gable(Sl.Thatch, Sl.Plaster, Sl.Wood, P(-w / 2f + 2.5f, y0 + h, d / 2f + 2.3f), 5f, 5f, 3.2f, 0.8f, false);
        BuildKit.DoorFront(b, P(2f, y0, d / 2f), 1.3f, 2.2f, false);
        BuildKit.Window(b, P(-3.4f, y0 + 1.6f, d / 2f + 0.02f), 1.3f, 1.4f, true, true);
        BuildKit.Window(b, P(5.0f, y0 + 1.6f, d / 2f + 0.02f), 1.1f, 1.3f, true, false);
        b.Push(P(-w / 2f + 2.5f, y0, d / 2f + 4.8f), 0f);
        BuildKit.Window(b, P(0, 1.6f, 0.02f), 1.3f, 1.4f, true, true);
        b.Pop();
        BuildKit.Chimney(b, P(w / 2f - 1.6f, y0 + h - 1f, -1f), 1.1f, 5.2f);
        b.Stairs(Sl.Stone, P(2f, 0, d / 2f + 1.0f), 2f, 1, 0.5f, 0.6f, 0f);
        BuildKit.Barrel(b, P(w / 2f - 0.8f, 0f, d / 2f + 0.9f));
        BuildKit.Crate(b, P(w / 2f - 1.8f, 0f, d / 2f + 0.9f), 0.8f, 20f);
    }

    public static void BarnOld(GeoBag b)
    {
        float w = 15f, d = 10.5f, h = 5.2f, y0 = 0.5f;
        b.BoxB(Sl.Stone, P(0, 0, 0), new Vector3(w + 0.4f, y0, d + 0.4f));
        b.BoxB(Sl.Wood, P(0, y0, 0), new Vector3(w, h, d));
        b.Gable(Sl.RoofB, Sl.Wood, Sl.Wood, P(0, y0 + h, 0), w, d, 6.4f, 0.9f, false);
        float zf = d / 2f;
        // pintu besar dengan palang X
        b.Box(Sl.WoodLight, P(0, y0 + 2.2f, zf + 0.06f), new Vector3(6.4f, 4.4f, 0.14f));
        b.Box(Sl.Wood, P(0, y0 + 2.2f, zf + 0.14f), new Vector3(0.18f, 4.4f, 0.08f));
        b.Beam(Sl.Wood, P(-3.1f, y0 + 0.1f, zf + 0.16f), P(-0.1f, y0 + 4.3f, zf + 0.16f), 0.3f, 0.1f, Vector3.forward);
        b.Beam(Sl.Wood, P(-0.1f, y0 + 0.1f, zf + 0.16f), P(-3.1f, y0 + 4.3f, zf + 0.16f), 0.3f, 0.1f, Vector3.forward);
        b.Beam(Sl.Wood, P(3.1f, y0 + 0.1f, zf + 0.16f), P(0.1f, y0 + 4.3f, zf + 0.16f), 0.3f, 0.1f, Vector3.forward);
        b.Beam(Sl.Wood, P(0.1f, y0 + 0.1f, zf + 0.16f), P(3.1f, y0 + 4.3f, zf + 0.16f), 0.3f, 0.1f, Vector3.forward);
        // pintu loteng jerami + katrol
        b.Box(Sl.WoodLight, P(0, y0 + h + 1.6f, zf + 0.05f), new Vector3(1.8f, 2.0f, 0.1f));
        b.Beam(Sl.Wood, P(0, y0 + h + 3.4f, zf), P(0, y0 + h + 3.4f, zf + 1.4f), 0.16f, 0.16f);
        // sandaran tiang
        foreach (float x in new[] { -w / 2f, w / 2f })
            for (int i = 0; i < 3; i++)
                b.BoxB(Sl.Wood, P(x, 0f, -d / 2f + 1f + i * (d - 2f) / 2f), new Vector3(0.4f, h + 0.5f, 0.4f));
        // silo
        b.Cyl(Sl.Stone, P(w / 2f + 3.2f, 0, 1.5f), 2.6f, 11f, 12, 2.6f, false);
        for (int i = 1; i < 4; i++) b.Cyl(Sl.Metal, P(w / 2f + 3.2f, i * 2.7f, 1.5f), 2.65f, 0.15f, 12, 2.65f, true);
        b.Dome(Sl.RoofB, P(w / 2f + 3.2f, 11f, 1.5f), 2.7f, 4, 12, 0.8f);
        b.Box(Sl.Wood, P(w / 2f + 3.2f, 1.2f, 1.5f + 2.62f), new Vector3(1.0f, 2.0f, 0.1f));
        // tumpukan jerami
        PropKit.Haystack(b, P(-w / 2f - 3f, 0, 2f));
        PropKit.Haystack(b, P(-w / 2f - 3.5f, 0, -2.4f));
    }

    public static void GazeboOld(GeoBag b)
    {
        b.Cyl(Sl.Marble, P(0, 0, 0), 4.0f, 0.5f, 12, 3.8f, true);
        for (int i = 0; i < 8; i++)
        {
            float a = i / 8f * Mathf.PI * 2f;
            BuildKit.Column(b, P(Mathf.Sin(a) * 3.2f, 0.5f, Mathf.Cos(a) * 3.2f), 0.22f, 3.6f, Sl.Marble);
        }
        b.Cyl(Sl.Marble, P(0, 4.1f, 0), 3.9f, 0.5f, 12, 3.7f, true);
        b.Spire(Sl.RoofB, P(0, 4.6f, 0), 3.9f, 3.6f, 12, 0.1f);
        BuildKit.Finial(b, P(0, 8.2f, 0), 1.6f, Sl.Gold);
        b.Cyl(Sl.Water, P(0, 0.52f, 0), 3.0f, 0.04f, 12, 3.0f, true);
        b.Cyl(Sl.Marble, P(0, 0.5f, 0), 0.5f, 0.9f, 8, 0.4f, true);
    }
}
