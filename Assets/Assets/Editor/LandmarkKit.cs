using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bangunan penting kota (Guild, Katedral, Akademi/Perpustakaan, Kedai, Penginapan, Toko, Pandai Besi, Barak, Istal, Menara Penyihir).
/// Setiap fungsi mengisi GeoBag dengan titik pusat tapak di origin dan sisi depan (pintu) menghadap +Z.
/// </summary>
public static partial class LandmarkKit
{
    static Vector3 P(float x, float y, float z) { return new Vector3(x, y, z); }

    public const float GuildW = 40f, GuildD = 24f, GuildPorch = 6.4f;
    public const float CathW = 30f, CathD = 54f;
    public const float AcadW = 28f, AcadD = 18f;
    public const float TavernW = 26f, TavernD = 17f;
    public const float InnW = 32f, InnD = 18f;
    public const float ShopW = 28f, ShopD = 17f;
    public const float SmithW = 26f, SmithD = 19f;
    public const float BarrW = 26f, BarrD = 17f;
    public const float StableW = 26f, StableD = 17f;
    public const float MageR = 7f;

    // ====================================================================== umum
    /// <summary>Portico bertiang marmer dengan pedimen. z0 = muka dinding, depth = kedalaman, base y = lantai teras.</summary>
    static void Portico(GeoBag b, float x, float z0, float width, float depth, float y0, float colH, int nCols, bool emblem)
    {
        b.BoxB(Sl.Stone, P(x, 0f, z0 + depth / 2f), new Vector3(width + 0.6f, y0, depth + 0.6f));
        for (int i = 0; i < nCols; i++)
        {
            float cx = x - width / 2f + 0.9f + i * (width - 1.8f) / (nCols - 1);
            BuildKit.Column(b, P(cx, y0, z0 + depth - 0.9f), 0.42f, colH, Sl.Marble);
        }
        float ey = y0 + colH + 0.3f;
        b.BoxB(Sl.Marble, P(x, ey, z0 + depth / 2f), new Vector3(width + 0.4f, 0.8f, depth));
        b.Gable(Sl.Roof, Sl.Marble, Sl.Wood, P(x, ey + 0.8f, z0 + depth / 2f), width + 0.4f, depth, width * 0.17f, 0.3f, false);
        if (emblem)
        {
            float zf = z0 + depth + 0.04f;
            b.Box(Sl.Gold, P(x, ey + 1.7f, zf), new Vector3(1.6f, 1.9f, 0.12f));
            b.Box(Sl.Cloth, P(x, ey + 1.7f, zf + 0.07f), new Vector3(1.2f, 1.5f, 0.08f));
            b.Beam(Sl.Gold, P(x - 1.4f, ey + 0.9f, zf + 0.05f), P(x + 1.4f, ey + 2.6f, zf + 0.05f), 0.16f, 0.1f, Vector3.forward);
            b.Beam(Sl.Gold, P(x + 1.4f, ey + 0.9f, zf + 0.05f), P(x - 1.4f, ey + 2.6f, zf + 0.05f), 0.16f, 0.1f, Vector3.forward);
        }
    }

    static void Dormers(GeoBag b, float yT, float zc, float span, float rise, int slot, params float[] xs)
    {
        foreach (float x in xs)
        {
            float s = span * 0.22f;
            float yb = yT + rise * (1f - s / (span / 2f)) - 0.9f;
            b.Push(P(x, 0f, zc + s), 0f);
            BuildKit.Dormer(b, P(0f, yb, 0f), 1.9f, 1.6f + 0.9f, slot, Sl.Plaster);
            b.Pop();
        }
    }

    // ====================================================================== GUILD
    public static void GuildOld(GeoBag b)
    {
        float w = GuildW, d = GuildD, g = 5.6f, u = 4.6f, jet = 0.9f, y0 = 1.2f, y1 = y0 + g, yT = y1 + u, rise = 9.5f;
        b.BoxB(Sl.DarkStone, P(0, 0, 0), new Vector3(w + 0.8f, y0, d + 0.8f));
        b.BoxB(Sl.Stone, P(0, y0, 0), new Vector3(w, g, d));
        b.BoxB(Sl.Plaster, P(0, y1, jet / 2f), new Vector3(w, u, d + jet));
        b.Box(Sl.DarkStone, P(0, y1 - 0.15f, d / 2f + 0.1f), new Vector3(w + 0.3f, 0.5f, 0.3f));
        // balok penopang jetty
        for (int i = 0; i < 14; i++)
        {
            float x = -w / 2f + 1f + i * (w - 2f) / 13f;
            b.Beam(Sl.Wood, P(x, y1 - 0.7f, d / 2f), P(x, y1, d / 2f + jet), 0.25f, 0.25f);
        }
        // rangka kayu lantai atas
        b.Push(P(0, y1, d / 2f + jet), 0f); BuildKit.TimberFront(b, w, 0f, u, 0f, true); b.Pop();
        b.Push(P(0, y1, -d / 2f), 180f); BuildKit.TimberFront(b, w, 0f, u, 0f, true); b.Pop();
        b.Push(P(w / 2f, y1, jet / 2f), 90f); BuildKit.TimberFront(b, d + jet, 0f, u, 0f, true); b.Pop();
        b.Push(P(-w / 2f, y1, jet / 2f), -90f); BuildKit.TimberFront(b, d + jet, 0f, u, 0f, true); b.Pop();

        // jendela lengkung lantai bawah
        foreach (float x in new[] { -17.5f, -12.5f, -7.5f, 7.5f, 12.5f, 17.5f })
            BuildKit.ArchWindow(b, P(x, y0 + 0.9f, d / 2f + 0.02f), 2.0f, 3.8f, Sl.Marble);
        for (int i = 0; i < 4; i++)
        {
            float z = -7.5f + i * 5f;
            b.Push(P(w / 2f, y0 + 0.9f, z), 90f); BuildKit.ArchWindow(b, Vector3.zero, 1.8f, 3.6f, Sl.Marble); b.Pop();
            b.Push(P(-w / 2f, y0 + 0.9f, z), -90f); BuildKit.ArchWindow(b, Vector3.zero, 1.8f, 3.6f, Sl.Marble); b.Pop();
        }
        for (int i = 0; i < 6; i++)
        {
            float x = -17f + i * 6.8f;
            BuildKit.Window(b, P(x, y1 + u * 0.55f, d / 2f + jet + 0.02f), 1.3f, 1.8f, true, true);
            b.Push(P(x, y1 + u * 0.55f, -d / 2f), 180f); BuildKit.Window(b, Vector3.zero, 1.3f, 1.8f, true, false); b.Pop();
        }
        // pintu besar + portico
        BuildKit.DoorFront(b, P(0, y0, d / 2f), 3.6f, 4.6f, true);
        Portico(b, 0f, d / 2f, 13.5f, GuildPorch - 1.4f, y0, 6.2f, 4, true);

        // atap + lukarne + cerobong
        b.Gable(Sl.Roof, Sl.Plaster, Sl.Wood, P(0, yT, jet / 2f), w, d + jet, rise, 1.0f, true);
        Dormers(b, yT, jet / 2f, d + jet, rise, Sl.Roof, -12f, 0f, 12f);
        foreach (float cx in new[] { -16f, 16f })
            BuildKit.Chimney(b, P(cx, yT - 1f, -1.5f), 1.4f, rise + 2.2f);

        // menara sudut depan
        foreach (float sx in new[] { -1f, 1f })
            BuildKit.RoundTower(b, P(sx * (w / 2f - 1.6f), 0f, d / 2f - 1.6f), 3.4f, 16f, Sl.Stone, Sl.DarkStone, Sl.RoofB, 10f, false, true, Sl.Cloth);
        // menara jam di belakang
        float tz = -d / 2f + 3.5f;
        b.BoxB(Sl.Stone, P(0, 0, tz), new Vector3(8.5f, 24f, 8.5f));
        b.BoxB(Sl.DarkStone, P(0, 22f, tz), new Vector3(9.4f, 0.6f, 9.4f));
        b.Hip(Sl.RoofB, Sl.Wood, P(0, 24f, tz), 9.6f, 9.6f, 9f, 0.7f);
        BuildKit.Finial(b, P(0, 33f, tz), 2.6f, Sl.Gold);
        foreach (float zs in new[] { 1f, -1f })
        {
            b.Push(P(0, 18f, tz + zs * 4.3f), zs > 0f ? 0f : 180f);
            b.Box(Sl.Marble, P(0, 0, 0), new Vector3(4.4f, 4.4f, 0.25f));
            b.Box(Sl.Gold, P(0, 0, 0.14f), new Vector3(3.6f, 3.6f, 0.08f));
            b.Box(Sl.Marble, P(0, 0, 0.2f), new Vector3(3.2f, 3.2f, 0.08f));
            b.Box(Sl.Metal, P(0, 0.7f, 0.28f), new Vector3(0.14f, 1.5f, 0.06f));
            b.Box(Sl.Metal, P(0.6f, 0f, 0.28f), new Vector3(1.2f, 0.14f, 0.06f));
            b.Pop();
        }
        // spanduk guild
        foreach (float x in new[] { -20.8f, -10f, 10f, 20.8f })
            BuildKit.Banner(b, P(x, y0 + 5.5f, d / 2f + 0.12f), 1.7f, 4.6f, (x < 0f) ? Sl.Cloth : Sl.ClothB, true);
        // papan gantung besar dengan emblem
        b.Beam(Sl.Metal, P(w / 2f - 0.4f, y1 + 3.6f, d / 2f + jet), P(w / 2f - 0.4f, y1 + 3.6f, d / 2f + jet + 2.6f), 0.14f, 0.14f);
        b.Box(Sl.Wood, P(w / 2f - 0.4f, y1 + 2.3f, d / 2f + jet + 2.1f), new Vector3(0.18f, 2.2f, 2.2f));
        b.Box(Sl.Gold, P(w / 2f - 0.28f, y1 + 2.3f, d / 2f + jet + 2.1f), new Vector3(0.08f, 1.5f, 1.5f));
        // tong & peti di depan
        BuildKit.Barrel(b, P(-w / 2f + 2f, 0f, d / 2f + 1.2f));
        BuildKit.Barrel(b, P(-w / 2f + 3.1f, 0f, d / 2f + 1.5f));
        BuildKit.Crate(b, P(w / 2f - 2.2f, 0f, d / 2f + 1.3f), 1.1f, 15f);
            // hiasan anime fantasi
        AnimeKit.Bunting(b, P(-w / 2f + 0.5f, y1 + 0.6f, d / 2f + jet + 0.5f), P(w / 2f - 0.5f, y1 + 0.6f, d / 2f + jet + 0.5f), 1.2f, 16, 3);
        AnimeKit.Bunting(b, P(-w / 2f + 0.5f, y1 - 1.0f, d / 2f + jet + 0.5f), P(w / 2f - 0.5f, y1 - 1.0f, d / 2f + jet + 0.5f), 0.9f, 14, 1);
        AnimeKit.PennantTop(b, P(-9f, yT + rise, 0f), Sl.Cloth);
        AnimeKit.PennantTop(b, P(9f, yT + rise, 0f), Sl.ClothB);
        foreach (float fx in new[] { -15f, -11f, 11f, 15f })
            AnimeKit.FlowerBush(b, P(fx, 0f, d / 2f + 1.6f), 1.0f, fx < 0f ? Sl.Blossom : Sl.Gold);
        foreach (float fx in new[] { -5.2f, 5.2f })
            AnimeKit.HangLantern(b, P(fx, y0 + 4.2f, d / 2f + 0.1f), 0.9f);
        AnimeKit.RoundWindow(b, P(0f, yT + rise * 0.55f, d / 2f + jet + 0.06f), 0.9f);
    }

    // ====================================================================== KATEDRAL
    public static void Cathedral(GeoBag b)
    {
        float w = CathW, d = CathD;
        float nave = 14f, wall = 15f, hw = nave / 2f;
        // lantai dasar dan dinding nave
        b.BoxB(Sl.DarkStone, P(0, 0, 0), new Vector3(w + 1f, 1.0f, d + 1f));
        b.BoxB(Sl.Marble, P(0, 1f, 0f), new Vector3(nave, wall, d - 6f));
        // lorong samping lebih rendah
        foreach (float sx in new[] { -1f, 1f })
        {
            float ax = sx * (hw + 3.3f);
            b.BoxB(Sl.Marble, P(ax, 1f, -1f), new Vector3(6.6f, 8.5f, d - 12f));
            // atap lorong miring ke luar
            Vector3 e0 = P(sx * (hw + 6.6f + 0.4f), 9.5f - 1.0f, -1f), e1 = P(sx * (hw - 0.1f), 9.5f + 2.2f - 1.0f, -1f);
            float zh = (d - 12f) / 2f + 0.6f;
            b.QuadBoth(Sl.RoofB, P(e0.x, e0.y, -1f - zh), P(e0.x, e0.y, -1f + zh), P(e1.x, e1.y, -1f + zh), P(e1.x, e1.y, -1f - zh));
        }
        // atap utama
        b.Gable(Sl.RoofB, Sl.Marble, Sl.Wood, P(0, 1f + wall, -0.5f), nave, d - 5f, 7.5f, 0.8f, false);
        // jendela lancet sepanjang nave
        for (int i = 0; i < 6; i++)
        {
            float z = -d / 2f + 8f + i * 6.4f;
            foreach (float sx in new[] { -1f, 1f })
            {
                b.Push(P(sx * (hw + 6.6f), 2.4f, z), sx > 0f ? 90f : -90f);
                BuildKit.ArchWindow(b, Vector3.zero, 1.6f, 4.8f, Sl.Marble);
                b.Pop();
                b.Push(P(sx * hw, 11f, z), sx > 0f ? 90f : -90f);
                BuildKit.ArchWindow(b, Vector3.zero, 1.5f, 3.4f, Sl.Marble);
                b.Pop();
                // penopang terbang
                b.Beam(Sl.Marble, P(sx * (hw + 7.1f), 6.5f, z + 3.2f), P(sx * (hw + 0.2f), 12.5f, z + 3.2f), 0.7f, 0.7f, Vector3.forward);
                b.BoxB(Sl.Marble, P(sx * (hw + 7.2f), 0f, z + 3.2f), new Vector3(1.0f, 8.5f, 1.2f));
                Vector3 pf = P(sx * (hw + 7.2f), 8.5f, z + 3.2f);
                b.Cone(Sl.RoofB, pf, 0.7f, 2.2f, 6);
            }
        }
        // apsis di belakang (setengah silinder + kerucut)
        b.Cyl(Sl.Marble, P(0, 1f, -d / 2f + 3f), hw, wall - 4f, 14, hw, false);
        b.Cyl(Sl.RoofB, P(0, wall - 3f, -d / 2f + 3f), hw + 0.8f, 0.6f, 14, hw + 0.4f, false);
        b.Spire(Sl.RoofB, P(0, wall - 2.4f, -d / 2f + 3f), hw + 0.4f, 11f, 14, 0f);
        BuildKit.Finial(b, P(0, wall + 8.4f, -d / 2f + 3f), 2.2f, Sl.Gold);

        // fasad depan: dua menara + portal + jendela mawar
        float fz = d / 2f - 3f;
        foreach (float sx in new[] { -1f, 1f })
        {
            float tx = sx * (hw + 1.5f);
            FantasyKit.Wall(b, Sl.Marble, P(tx, 0f, fz), new Vector3(8f, 34f, 8f), 1);
            b.BoxB(Sl.DarkStone, P(tx, 0f, fz), new Vector3(8.8f, 1.5f, 8.8f));
            foreach (float yy in new[] { 11f, 22f })
                b.BoxB(Sl.DarkStone, P(tx, yy, fz), new Vector3(8.5f, 0.6f, 8.5f));
            // lonceng
            FantasyKit.Wall(b, Sl.Stone, P(tx, 25f, fz), new Vector3(7.4f, 9f, 7.4f), 1);
            foreach (float zs in new[] { 1f, -1f })
            {
                b.Push(P(tx, 26f, fz + zs * 3.75f), zs > 0f ? 0f : 180f);
                BuildKit.ArchWindow(b, Vector3.zero, 2.2f, 5.6f, Sl.Gold);
                b.Pop();
            }
            foreach (float xs in new[] { 1f, -1f })
            {
                b.Push(P(tx + xs * 3.75f, 26f, fz), xs > 0f ? 90f : -90f);
                BuildKit.ArchWindow(b, Vector3.zero, 2.2f, 5.6f, Sl.Gold);
                b.Pop();
            }
            // lancet bawah menara
            b.Push(P(tx, 4.5f, fz + 4.02f), 0f);
            BuildKit.ArchWindow(b, Vector3.zero, 1.6f, 5.5f, Sl.Marble);
            b.Pop();
            b.Push(P(tx, 15f, fz + 4.02f), 0f);
            BuildKit.ArchWindow(b, Vector3.zero, 1.6f, 4.5f, Sl.Marble);
            b.Pop();
            b.Hip(Sl.RoofB, Sl.Wood, P(tx, 34f, fz), 8.8f, 8.8f, 3f, 0.4f);
            b.Spire(Sl.RoofB, P(tx, 35.5f, fz), 4.2f, 22f, 8, 0f);
            // pinakel kecil di sudut
            foreach (float cx in new[] { -1f, 1f })
                foreach (float cz in new[] { -1f, 1f })
                {
                    b.BoxB(Sl.Marble, P(tx + cx * 3.9f, 34f, fz + cz * 3.9f), new Vector3(0.9f, 1.4f, 0.9f));
                    b.Cone(Sl.RoofB, P(tx + cx * 3.9f, 35.4f, fz + cz * 3.9f), 0.75f, 3.2f, 4);
                }
            BuildKit.Finial(b, P(tx, 57.2f, fz), 3.6f, Sl.Gold);
        }
        // portal sentral bertingkat
        b.BoxB(Sl.Marble, P(0, 0, fz + 0.5f), new Vector3(nave - 0.4f + 3f, 20f, 7f));
        b.BoxB(Sl.Marble, P(0, 20f, fz + 0.5f), new Vector3(nave - 0.4f + 3f, 0.9f, 7.4f));
        b.Gable(Sl.RoofB, Sl.Marble, Sl.Wood, P(0, 20.9f, fz + 0.5f), nave + 2.6f, 7f, 5.5f, 0.5f, false);
        // pintu lengkung besar
        float dz = fz + 4f + 0.02f;
        b.Box(Sl.Wood, P(0, 3.3f, dz), new Vector3(4.6f, 6.4f, 0.3f));
        BuildKit.ArchWindow(b, P(0, 6.5f, dz), 4.6f, 0.2f, Sl.Gold);
        b.Box(Sl.Metal, P(0, 2.2f, dz + 0.2f), new Vector3(4.6f, 0.18f, 0.06f));
        b.Box(Sl.Metal, P(0, 4.6f, dz + 0.2f), new Vector3(4.6f, 0.18f, 0.06f));
        b.Box(Sl.Metal, P(0, 3.3f, dz + 0.2f), new Vector3(0.1f, 6.4f, 0.06f));
        // lengkung portal berlapis
        b.Box(Sl.Marble, P(-3.0f, 3.5f, dz), new Vector3(0.6f, 7f, 0.5f));
        b.Box(Sl.Marble, P(3.0f, 3.5f, dz), new Vector3(0.6f, 7f, 0.5f));
        // jendela mawar: kipas segitiga kaca + jari-jari emas
        Vector3 rc = P(0, 14f, dz + 0.02f);
        int rn = 16;
        for (int k = 0; k < rn; k++)
        {
            float a0 = k * Mathf.PI * 2f / rn, a1 = (k + 1) * Mathf.PI * 2f / rn;
            Vector3 p0 = rc + P(Mathf.Cos(a0) * 3.6f, Mathf.Sin(a0) * 3.6f, 0f), p1 = rc + P(Mathf.Cos(a1) * 3.6f, Mathf.Sin(a1) * 3.6f, 0f);
            b.Tri(Sl.Glass, rc, p0, p1);
            b.Beam(Sl.Gold, rc, p0, 0.12f, 0.12f, Vector3.forward);
            Vector3 q0 = rc + P(Mathf.Cos(a0) * 4.1f, Mathf.Sin(a0) * 4.1f, 0f), q1 = rc + P(Mathf.Cos(a1) * 4.1f, Mathf.Sin(a1) * 4.1f, 0f);
            b.Beam(Sl.Marble, (p0 + q0) / 2f, (p1 + q1) / 2f, 0.6f, 0.4f, Vector3.forward);
        }
        b.Ball(Sl.Gold, rc + P(0, 0, 0.1f), 0.5f, 4, 8);
        // transept dan menara persilangan (flèche)
        b.BoxB(Sl.Marble, P(0, 1f, 0f), new Vector3(w - 2f, 12f, 12f));
        b.Gable(Sl.RoofB, Sl.Marble, Sl.Wood, P(0, 13f, 0f), w - 2f, 12f, 6f, 0.7f, true);
        b.BoxB(Sl.Marble, P(0, 14f, 0f), new Vector3(5f, 7f, 5f));
        b.Spire(Sl.RoofB, P(0, 21f, 0f), 3.4f, 17f, 8, 0f);
        BuildKit.Finial(b, P(0, 38f, 0f), 3f, Sl.Gold);
        b.Box(Sl.Gold, P(0, 41.7f, 0f), new Vector3(0.35f, 2.2f, 0.35f));
        b.Box(Sl.Gold, P(0, 42.1f, 0f), new Vector3(1.4f, 0.35f, 0.35f));
        foreach (float sx in new[] { -1f, 1f })
        {
            b.Push(P(sx * (w / 2f - 1f), 1f, 0f), sx > 0f ? 90f : -90f);
            BuildKit.ArchWindow(b, Vector3.zero, 2.4f, 6.4f, Sl.Gold);
            b.Pop();
        }
        // bangku kecil dan lilin di depan
        b.BoxB(Sl.Stone, P(0, 0, d / 2f + 2f), new Vector3(8f, 0.4f, 3f));
            // hiasan: tangga masuk, semak mawar, tanaman rambat di menara, lentera tiang
        b.Stairs(Sl.Marble, P(0f, 0f, d / 2f + 3.2f), 7.2f, 3, 0.3f, 0.8f, 0f);
        foreach (float sx in new[] { -1f, 1f })
        {
            AnimeKit.Ivy(b, P(sx * (hw + 5.2f), 1.0f, fz + 4.03f), 14f, 2.4f, 5 + (int)sx);
            AnimeKit.FlowerBush(b, P(sx * 6.5f, 0f, d / 2f + 2.2f), 1.2f, sx < 0f ? Sl.Blossom : Sl.Gold);
            AnimeKit.FlowerBush(b, P(sx * 9.5f, 0f, d / 2f + 1.6f), 1.0f, Sl.Blossom);
            AnimeKit.HangLantern(b, P(sx * 3.4f, 6.2f, fz + 4.1f), 0.8f);
        }
    }

    // ====================================================================== AKADEMI / PERPUSTAKAAN
    public static void AcademyOld(GeoBag b)
    {
        float w = AcadW, d = AcadD, g = 4.4f, u = 4.0f, y0 = 1f, y1 = y0 + g, yT = y1 + u;
        b.BoxB(Sl.DarkStone, P(0, 0, 0), new Vector3(w + 0.8f, y0, d + 0.8f));
        b.BoxB(Sl.Stone, P(0, y0, 0), new Vector3(w, g, d));
        b.BoxB(Sl.PlasterB, P(0, y1, 0), new Vector3(w - 0.4f, u, d - 0.4f));
        b.Box(Sl.Marble, P(0, y1, d / 2f), new Vector3(w + 0.4f, 0.5f, 0.3f));
        // dua sayap atap pelana + menara bulat berkubah di tengah belakang
        b.Hip(Sl.RoofB, Sl.Wood, P(0, yT, 0), w, d, 5.5f, 0.9f);
        foreach (float x in new[] { -9.5f, -3.8f, 3.8f, 9.5f })
        {
            BuildKit.ArchWindow(b, P(x, y0 + 0.8f, d / 2f + 0.02f), 1.6f, 3.2f, Sl.Marble);
            BuildKit.ArchWindow(b, P(x, y1 + 0.5f, d / 2f - 0.18f), 1.3f, 2.8f, Sl.Gold);
        }
        for (int i = 0; i < 3; i++)
        {
            float z = -5f + i * 5f;
            b.Push(P(w / 2f - 0.2f, y0 + 0.8f, z), 90f); BuildKit.ArchWindow(b, Vector3.zero, 1.5f, 3.2f, Sl.Marble); b.Pop();
            b.Push(P(-w / 2f + 0.2f, y0 + 0.8f, z), -90f); BuildKit.ArchWindow(b, Vector3.zero, 1.5f, 3.2f, Sl.Marble); b.Pop();
        }
        BuildKit.DoorFront(b, P(0, y0, d / 2f), 2.8f, 3.6f, true);
        Portico(b, 0f, d / 2f, 8.5f, 4.2f, y0, 4.6f, 4, false);
        // menara dan kubah
        float tz = -d / 2f + 1.5f;
        b.Cyl(Sl.Stone, P(0, 0, tz), 6.4f, 2f, 20, 6.0f, true);
        b.Cyl(Sl.PlasterB, P(0, 2f, tz), 6.0f, 20f, 20, 5.6f, false);
        for (int i = 0; i < 3; i++) b.Cyl(Sl.Marble, P(0, 7f + i * 6.5f, tz), 5.9f, 0.5f, 20, 5.9f, true);
        foreach (float a in new[] { 0.6f, 2.2f, 3.8f, 5.4f })
        {
            b.Push(P(Mathf.Sin(a) * 5.62f, 13f, tz + Mathf.Cos(a) * 5.62f), a * Mathf.Rad2Deg);
            BuildKit.ArchWindow(b, Vector3.zero, 1.5f, 3.8f, Sl.Gold);
            b.Pop();
        }
        b.Cyl(Sl.Marble, P(0, 22f, tz), 6.0f, 0.9f, 20, 6.8f, true);
        b.Dome(Sl.RoofB, P(0, 22.6f, tz), 6.8f, 6, 20, 0.95f);
        BuildKit.Finial(b, P(0, 28.9f, tz), 3f, Sl.Gold);
        // menara kecil di sudut depan
        foreach (float sx in new[] { -1f, 1f })
            BuildKit.RoundTower(b, P(sx * (w / 2f), 0f, d / 2f - 1f), 2.6f, 11f, Sl.Stone, Sl.DarkStone, Sl.RoofB, 8f, false, true, Sl.ClothB);
        // buku emas raksasa di atas pintu
        b.Box(Sl.Gold, P(0, y0 + 6.2f, d / 2f + 0.15f), new Vector3(2.4f, 1.6f, 0.2f));
        b.Box(Sl.Cloth, P(0, y0 + 6.2f, d / 2f + 0.28f), new Vector3(1.0f, 1.3f, 0.1f));
        BuildKit.Chimney(b, P(w / 2f - 3f, yT - 0.5f, -3f), 1.2f, 5f);
            AnimeKit.Bunting(b, P(-w / 2f + 0.5f, y1 + 0.5f, d / 2f + 0.5f), P(w / 2f - 0.5f, y1 + 0.5f, d / 2f + 0.5f), 0.9f, 12, 11);
        foreach (float fx in new[] { -10f, 10f })
            AnimeKit.FlowerBush(b, P(fx, 0f, d / 2f + 1.5f), 0.9f, Sl.Blossom);
    }

    // ====================================================================== KEDAI MAKAN
    public static void TavernOld(GeoBag b)
    {
        float w = TavernW, d = TavernD, g = 4.2f, u = 3.8f, y0 = 0.9f, y1 = y0 + g, yT = y1 + u, jet = 0.7f, rise = 7.5f;
        b.BoxB(Sl.Stone, P(0, 0, 0), new Vector3(w + 0.5f, y0, d + 0.5f));
        b.BoxB(Sl.Plaster, P(0, y0, 0), new Vector3(w, g, d));
        b.BoxB(Sl.Plaster, P(0, y1, jet / 2f), new Vector3(w, u, d + jet));
        b.Push(P(0, y0, d / 2f), 0f); BuildKit.TimberFront(b, w, 0f, g, 0f, true); b.Pop();
        b.Push(P(0, y1, d / 2f + jet), 0f); BuildKit.TimberFront(b, w, 0f, u, 0f, true); b.Pop();
        b.Push(P(w / 2f, y0, 0f), 90f); BuildKit.TimberFront(b, d, 0f, g + u, 0f, true); b.Pop();
        b.Push(P(-w / 2f, y0, 0f), -90f); BuildKit.TimberFront(b, d, 0f, g + u, 0f, true); b.Pop();
        for (int i = 0; i < 9; i++)
        {
            float x = -w / 2f + 1f + i * (w - 2f) / 8f;
            b.Beam(Sl.Wood, P(x, y1 - 0.6f, d / 2f), P(x, y1, d / 2f + jet), 0.22f, 0.22f);
        }
        BuildKit.DoorFront(b, P(0, y0, d / 2f), 2.2f, 3.1f, true);
        foreach (float x in new[] { -8.5f, -4.2f, 4.2f, 8.5f })
            BuildKit.Window(b, P(x, y0 + g * 0.55f, d / 2f + 0.02f), 1.8f, 1.9f, true, true);
        foreach (float x in new[] { -9f, -4.5f, 0f, 4.5f, 9f })
            BuildKit.Window(b, P(x, y1 + u * 0.55f, d / 2f + jet + 0.02f), 1.3f, 1.6f, true, false);
        b.Gable(Sl.Roof, Sl.Plaster, Sl.Wood, P(0, yT, jet / 2f), w, d + jet, rise, 0.9f, true);
        Dormers(b, yT, jet / 2f, d + jet, rise, Sl.Roof, -7f, 7f);
        BuildKit.Chimney(b, P(-w / 2f + 3f, yT - 1f, -2f), 1.5f, rise + 2f);
        BuildKit.Chimney(b, P(w / 2f - 3f, yT - 1f, -2f), 1.5f, rise + 2f);
        // teras luar dengan kanopi bergaris & meja
        BuildKit.Awning(b, P(0, y0 + g - 0.2f, d / 2f + 0.1f), w - 3f, 2.2f, 0.9f, Sl.Cloth, Sl.Plaster);
        for (int i = 0; i < 3; i++)
        {
            float x = -8f + i * 8f;
            b.Cyl(Sl.WoodLight, P(x, 0.0f, d / 2f + 3.2f), 0.9f, 0.9f, 10, 0.9f, true);
            foreach (float a in new[] { 0.8f, 2.4f, 4.0f, 5.5f })
                b.Cyl(Sl.Wood, P(x + Mathf.Sin(a) * 1.5f, 0f, d / 2f + 3.2f + Mathf.Cos(a) * 1.5f), 0.28f, 0.6f, 7, 0.28f, true);
        }
        BuildKit.Barrel(b, P(-w / 2f + 1.2f, 0f, d / 2f + 1.2f));
        BuildKit.Barrel(b, P(-w / 2f + 2.4f, 0f, d / 2f + 1.5f));
        // papan gantung (cangkir)
        BuildKit.HangingSign(b, P(w / 2f - 0.3f, y1 + 3.0f, d / 2f + jet + 0.05f), 2.2f, Sl.Gold);
        // lentera tali
        for (int i = 0; i < 5; i++)
            BuildKit.Lantern(b, P(-8f + i * 4f, y0 + g - 0.9f, d / 2f + 2.0f));
            AnimeKit.Bunting(b, P(-w / 2f + 0.5f, y1 + 0.4f, d / 2f + jet + 0.4f), P(w / 2f - 0.5f, y1 + 0.4f, d / 2f + jet + 0.4f), 0.9f, 12, 5);
        AnimeKit.PennantTop(b, P(0f, yT + rise, 0f), Sl.ClothB);
        foreach (float fx in new[] { -11f, 11f })
            AnimeKit.FlowerBush(b, P(fx, 0f, d / 2f + 1.5f), 0.9f, Sl.Blossom);
        AnimeKit.Ivy(b, P(-w / 2f + 1.0f, y0, d / 2f + 0.02f), 6f, 1.6f, 4);
    }

    // ====================================================================== PENGINAPAN
    public static void InnOld(GeoBag b)
    {
        float w = InnW, d = InnD, fh = 3.6f, y0 = 1f, jet = 0.7f, rise = 8.5f;
        b.BoxB(Sl.Stone, P(0, 0, 0), new Vector3(w + 0.5f, y0, d + 0.5f));
        b.BoxB(Sl.Stone, P(0, y0, 0), new Vector3(w, fh, d));
        b.BoxB(Sl.Plaster, P(0, y0 + fh, jet / 2f), new Vector3(w, fh, d + jet));
        b.BoxB(Sl.Plaster, P(0, y0 + 2 * fh, jet / 2f), new Vector3(w, fh, d + jet));
        float yT = y0 + 3 * fh;
        for (int f = 1; f <= 2; f++)
        {
            b.Push(P(0, y0 + f * fh, d / 2f + jet), 0f); BuildKit.TimberFront(b, w, 0f, fh, 0f, f == 1); b.Pop();
            b.Push(P(w / 2f, y0 + f * fh, jet / 2f), 90f); BuildKit.TimberFront(b, d + jet, 0f, fh, 0f, true); b.Pop();
            b.Push(P(-w / 2f, y0 + f * fh, jet / 2f), -90f); BuildKit.TimberFront(b, d + jet, 0f, fh, 0f, true); b.Pop();
        }
        for (int i = 0; i < 12; i++)
        {
            float x = -w / 2f + 1f + i * (w - 2f) / 11f;
            b.Beam(Sl.Wood, P(x, y0 + fh - 0.6f, d / 2f), P(x, y0 + fh, d / 2f + jet), 0.22f, 0.22f);
        }
        // lengkung pintu besar
        BuildKit.DoorFront(b, P(0, y0, d / 2f), 2.6f, 3.2f, true);
        foreach (float x in new[] { -12f, -7f, 7f, 12f })
            BuildKit.Window(b, P(x, y0 + fh * 0.55f, d / 2f + 0.02f), 1.6f, 1.8f, true, true);
        // balkon lantai 2 dengan pagar
        b.Box(Sl.Wood, P(0, y0 + 2 * fh - 0.1f, d / 2f + jet + 0.9f), new Vector3(w - 4f, 0.25f, 1.9f));
        BuildKit.Balustrade(b, P(-w / 2f + 2f, y0 + 2 * fh, d / 2f + jet + 1.8f), w - 4f, 1.0f, Sl.Wood);
        for (int i = 0; i < 6; i++)
            b.Beam(Sl.Wood, P(-w / 2f + 3f + i * 5.2f, y0 + 2 * fh - 0.35f, d / 2f + jet), P(-w / 2f + 3f + i * 5.2f, y0 + 2 * fh - 0.1f, d / 2f + jet + 1.8f), 0.18f, 0.18f);
        foreach (float x in new[] { -12f, -6f, 6f, 12f })
            BuildKit.Window(b, P(x, y0 + 2 * fh + fh * 0.55f, d / 2f + jet + 0.02f), 1.5f, 1.9f, true, false);
        BuildKit.DoorFront(b, P(0, y0 + 2 * fh, d / 2f + jet), 1.4f, 2.3f, false);
        b.Gable(Sl.RoofB, Sl.Plaster, Sl.Wood, P(0, yT, jet / 2f), w, d + jet, rise, 1.0f, true);
        Dormers(b, yT, jet / 2f, d + jet, rise, Sl.RoofB, -10f, -3.4f, 3.4f, 10f);
        BuildKit.Chimney(b, P(-10f, yT - 1f, -2f), 1.5f, rise + 2.4f);
        BuildKit.Chimney(b, P(10f, yT - 1f, -2f), 1.5f, rise + 2.4f);
        BuildKit.HangingSign(b, P(-w / 2f + 0.3f, y0 + fh + 2.8f, d / 2f + jet + 0.05f), 2.4f, Sl.Gold);
        // istal samping kecil
        b.BoxB(Sl.Wood, P(w / 2f + 3.2f, 0f, -1f), new Vector3(6f, 3.2f, 11f));
        b.Gable(Sl.Thatch, Sl.Wood, Sl.Wood, P(w / 2f + 3.2f, 3.2f, -1f), 6f, 11f, 2.8f, 0.6f, false);
        // peti kereta di depan
        BuildKit.Crate(b, P(w / 2f - 2f, 0f, d / 2f + 1.5f), 1.0f, 8f);
        BuildKit.Barrel(b, P(-w / 2f + 1.5f, 0f, d / 2f + 1.4f));
        for (int i = 0; i < 3; i++) BuildKit.Lantern(b, P(-6f + i * 6f, y0 + 3.2f, d / 2f + 0.5f));
            AnimeKit.Bunting(b, P(-w / 2f + 0.5f, y0 + fh + 0.6f, d / 2f + jet + 0.4f), P(w / 2f - 0.5f, y0 + fh + 0.6f, d / 2f + jet + 0.4f), 1.0f, 14, 7);
        AnimeKit.PennantTop(b, P(0f, y0 + fh * 2f + rise, 0f), Sl.Cloth);
        foreach (float fx in new[] { -12f, 12f })
            AnimeKit.FlowerBush(b, P(fx, 0f, d / 2f + 1.5f), 0.9f, Sl.Blossom);
    }

    // ====================================================================== TOKO UMUM
    public static void ShopOld(GeoBag b)
    {
        float w = ShopW, d = ShopD, fh = 3.6f, y0 = 0.9f, jet = 0.6f, rise = 7f;
        b.BoxB(Sl.Stone, P(0, 0, 0), new Vector3(w + 0.4f, y0, d + 0.4f));
        b.BoxB(Sl.Plaster, P(0, y0, 0), new Vector3(w, fh, d));
        b.BoxB(Sl.Plaster, P(0, y0 + fh, jet / 2f), new Vector3(w, fh, d + jet));
        float yT = y0 + 2 * fh;
        b.Push(P(0, y0, d / 2f), 0f); BuildKit.TimberFront(b, w, 0f, fh, 0f, false); b.Pop();
        b.Push(P(0, y0 + fh, d / 2f + jet), 0f); BuildKit.TimberFront(b, w, 0f, fh, 0f, true); b.Pop();
        b.Push(P(w / 2f, y0, 0f), 90f); BuildKit.TimberFront(b, d, 0f, 2 * fh, 0f, true); b.Pop();
        b.Push(P(-w / 2f, y0, 0f), -90f); BuildKit.TimberFront(b, d, 0f, 2 * fh, 0f, true); b.Pop();
        // tiga etalase + pintu di tengah
        foreach (float x in new[] { -9f, -5f, 5f, 9f })
        {
            b.Box(Sl.Wood, P(x, y0 + 1.5f, d / 2f + 0.06f), new Vector3(3.2f, 2.3f, 0.14f));
            b.Quad(Sl.Glass, P(x - 1.4f, y0 + 0.5f, d / 2f + 0.14f), P(x + 1.4f, y0 + 0.5f, d / 2f + 0.14f), P(x + 1.4f, y0 + 2.5f, d / 2f + 0.14f), P(x - 1.4f, y0 + 2.5f, d / 2f + 0.14f));
            b.Box(Sl.Wood, P(x, y0 + 1.5f, d / 2f + 0.18f), new Vector3(0.08f, 2.0f, 0.05f));
        }
        BuildKit.DoorFront(b, P(0, y0, d / 2f), 2.2f, 3.0f, true);
        BuildKit.Awning(b, P(-7f, y0 + fh - 0.3f, d / 2f + 0.1f), 9f, 1.8f, 0.7f, Sl.Cloth, Sl.Plaster);
        BuildKit.Awning(b, P(7f, y0 + fh - 0.3f, d / 2f + 0.1f), 9f, 1.8f, 0.7f, Sl.ClothB, Sl.Plaster);
        foreach (float x in new[] { -10f, -5f, 5f, 10f })
            BuildKit.Window(b, P(x, y0 + fh + fh * 0.55f, d / 2f + jet + 0.02f), 1.5f, 1.8f, true, true);
        BuildKit.Window(b, P(0, y0 + fh + fh * 0.55f, d / 2f + jet + 0.02f), 1.8f, 2.0f, true, false);
        b.Gable(Sl.RoofB, Sl.Plaster, Sl.Wood, P(0, yT, jet / 2f), w, d + jet, rise, 0.9f, true);
        Dormers(b, yT, jet / 2f, d + jet, rise, Sl.RoofB, -8f, 0f, 8f);
        BuildKit.Chimney(b, P(-9f, yT - 1f, -2f), 1.4f, rise + 2f);
        BuildKit.HangingSign(b, P(w / 2f - 0.2f, y0 + fh + 3.0f, d / 2f + jet + 0.05f), 2.0f, Sl.Gold);
        BuildKit.HangingSign(b, P(-w / 2f + 0.2f, y0 + fh + 3.0f, d / 2f + jet + 0.05f), 2.0f, Sl.Cloth);
        // barang jualan di depan
        for (int i = 0; i < 3; i++) BuildKit.Barrel(b, P(-w / 2f + 1.4f + i * 1.1f, 0f, d / 2f + 1.6f));
        for (int i = 0; i < 3; i++) BuildKit.Crate(b, P(w / 2f - 2.8f + i * 1.1f, 0f, d / 2f + 1.6f), 0.9f, i * 14f);
            AnimeKit.Bunting(b, P(-w / 2f + 0.5f, y0 + fh + 0.6f, d / 2f + jet + 0.4f), P(w / 2f - 0.5f, y0 + fh + 0.6f, d / 2f + jet + 0.4f), 1.0f, 13, 9);
        AnimeKit.PennantTop(b, P(0f, y0 + fh * 2f + rise, 0f), Sl.ClothB);
        foreach (float fx in new[] { -11f, 11f })
            AnimeKit.FlowerBush(b, P(fx, 0f, d / 2f + 1.5f), 0.85f, Sl.Gold);
    }

    // ====================================================================== PANDAI BESI
    public static void SmithyOld(GeoBag b)
    {
        float w = SmithW, d = SmithD, h = 6.4f, y0 = 0.8f, rise = 5.5f;
        b.BoxB(Sl.DarkStone, P(0, 0, 0), new Vector3(w + 0.5f, y0, d + 0.5f));
        b.BoxB(Sl.Stone, P(0, y0, 0), new Vector3(w, h, d));
        b.Push(P(0, y0, d / 2f), 0f); BuildKit.TimberFront(b, w, h - 1.0f, h, 0f, false); b.Pop();
        b.Gable(Sl.RoofB, Sl.Stone, Sl.Wood, P(0, y0 + h, 0f), w, d, rise, 0.8f, true);
        BuildKit.DoorFront(b, P(-5f, y0, d / 2f), 2.2f, 3.0f, false);
        BuildKit.Window(b, P(-9.5f, y0 + 3.2f, d / 2f + 0.02f), 1.3f, 1.4f, true, false);
        // cerobong besar dan bara
        BuildKit.Chimney(b, P(8f, y0 + h - 2f, -3.5f), 2.6f, rise + 7f);
        b.Box(Sl.Glow, P(8f, y0 + h + 0.2f, -3.5f + 1.35f), new Vector3(1.2f, 0.9f, 0.08f));
        // bengkel terbuka (lean-to) di depan kanan
        float lx = 6.5f, lz = d / 2f + 3.4f;
        foreach (float px in new[] { -1f, 1f })
            foreach (float pz in new[] { -1f, 1f })
                b.BoxB(Sl.Wood, P(lx + px * 5.4f, 0f, lz + pz * 3.0f), new Vector3(0.35f, 4.0f, 0.35f));
        b.Quad(Sl.Roof, P(lx - 6.2f, 3.6f, lz + 3.8f), P(lx + 6.2f, 3.6f, lz + 3.8f), P(lx + 6.2f, 5.2f, lz - 3.4f), P(lx - 6.2f, 5.2f, lz - 3.4f));
        b.Quad(Sl.Wood, P(lx - 6.2f, 3.5f, lz - 3.4f), P(lx + 6.2f, 3.5f, lz - 3.4f), P(lx + 6.2f, 3.5f, lz + 3.8f), P(lx - 6.2f, 3.5f, lz + 3.8f));
        b.Quad(Sl.Wood, P(lx - 6.2f, 5.1f, lz - 3.4f), P(lx - 6.2f, 3.5f, lz - 3.4f), P(lx - 6.2f, 3.5f, lz + 3.8f), P(lx - 6.2f, 5.1f, lz + 3.8f));
        // tungku dengan bara menyala
        b.BoxB(Sl.DarkStone, P(lx + 2.8f, 0f, lz - 1.5f), new Vector3(2.6f, 1.2f, 2.0f));
        b.Box(Sl.Glow, P(lx + 2.8f, 1.22f, lz - 1.5f), new Vector3(2.0f, 0.1f, 1.4f));
        b.BoxB(Sl.DarkStone, P(lx + 2.8f, 1.2f, lz - 2.2f), new Vector3(2.6f, 3.2f, 0.7f));
        // landasan, tong air, batu asah, rak senjata
        b.BoxB(Sl.Metal, P(lx - 1.2f, 0f, lz), new Vector3(0.7f, 0.9f, 1.2f));
        b.Box(Sl.Metal, P(lx - 1.2f, 1.0f, lz), new Vector3(0.9f, 0.25f, 1.6f));
        BuildKit.Barrel(b, P(lx - 3.2f, 0f, lz - 1.6f));
        b.Cyl(Sl.Stone, P(lx - 3.6f, 0.7f, lz + 1.4f), 0.55f, 0.18f, 12, 0.55f, true);
        b.Box(Sl.Wood, P(lx - 3.6f, 0.35f, lz + 1.4f), new Vector3(1.0f, 0.7f, 0.2f));
        for (int i = 0; i < 4; i++)
            b.Beam(Sl.Metal, P(-9f + i * 0.5f, 0.2f, d / 2f + 0.9f), P(-9f + i * 0.5f, 3.0f, d / 2f + 0.5f), 0.14f, 0.06f);
        b.Box(Sl.Wood, P(-8f, 0.1f, d / 2f + 0.9f), new Vector3(2.6f, 0.2f, 0.5f));
        // papan palu
        b.Box(Sl.Metal, P(-5f, y0 + 4.4f, d / 2f + 0.3f), new Vector3(0.18f, 1.5f, 0.12f));
        b.Box(Sl.Metal, P(-5f, y0 + 5.1f, d / 2f + 0.3f), new Vector3(1.1f, 0.5f, 0.14f));
            AnimeKit.PennantTop(b, P(0f, y0 + h + rise, 0f), Sl.Cloth);
        AnimeKit.HangLantern(b, P(-2.4f, y0 + 3.4f, d / 2f + 0.1f), 0.8f);
        AnimeKit.FlowerBush(b, P(-11f, 0f, d / 2f + 1.2f), 0.8f, Sl.Gold);
    }
}
