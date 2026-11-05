using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Penanda kota Ashwatch: Katedral Abu (28 x 64, ~52), Akademi/Perpustakaan (40 x 28), Menara Sihir (r 9, ~44), Kastil Penguasa (76 x 64, ~50), Rumah Bangsawan (26 x 22).
/// Konvensi: muka = +Z, pusat denah di titik asal, alas tanah y = 0.
/// </summary>
public static partial class T2Kit
{
    // ------------------------------------------------------------------ menara persegi bermahkota (badan + lis + pinakel + bukaan lonceng)
    static void SquareTower(GeoBag b, float cx, float cz, float w, float y0, float y1, float yBell, int seed, int sWall)
    {
        StoneBlock(b, cx - w * 0.5f, cx + w * 0.5f, cz - w * 0.5f, cz + w * 0.5f, y0, y1, sWall);
        for (int sd = 0; sd < 4; sd++)
        {
            b.Push(new Vector3(cx, 0f, cz), sd * 90f);
            float zf = w * 0.5f;
            b.Push(new Vector3(0f, 0f, zf), 0f);
            TallWindow(b, 0f, yBell, 1.0f, 3.4f, 0);
            if (y0 < 6f) TallWindow(b, 0f, y0 + 3.5f, 0.8f, 2.4f, 0);
            b.Pop();
            b.Pop();
        }
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
            {
                b.Box(Sl.Stone, new Vector3(cx + sx * (w * 0.5f), y1 + 0.9f, cz + sz * (w * 0.5f)), new Vector3(0.9f, 1.8f, 0.9f));
                b.Spire(Sl.RoofB, new Vector3(cx + sx * (w * 0.5f), y1 + 1.8f, cz + sz * (w * 0.5f)), 0.7f, 2.2f, 4, 0.1f);
            }
    }

    // ------------------------------------------------------------------ KATEDRAL (28 x 64, ~52)
    public static float Cathedral(GeoBag b)
    {
        const float P = 0.6f;
        float nx = 6.8f, aisleX = 11.8f, zA = -25.5f, zN1 = 22f, zF = 30f;
        // podium
        b.Box(Sl.DarkStone, new Vector3(0f, (P - 0.7f) * 0.5f, 0f), new Vector3(29.4f, P + 0.7f, 66f));
        // tubuh nave
        float nt = P + 17f;
        StoneBlock(b, -nx, nx, zA + 6f, zN1, P, nt, Sl.Stone);
        // lorong samping lebih rendah
        float at = P + 8.8f;
        StoneBlock(b, nx, aisleX, zA + 7f, zN1 - 1f, P, at, Sl.Stone);
        StoneBlock(b, -aisleX, -nx, zA + 7f, zN1 - 1f, P, at, Sl.Stone);
        // apsis setengah lingkaran
        b.Cyl(Sl.Stone, new Vector3(0f, P, zA + 6f), nx, 17f, 12, nx, false);
        b.Cyl(Sl.DarkStone, new Vector3(0f, nt, zA + 6f), nx + 0.4f, 0.3f, 12, nx + 0.4f, true);
        b.Spire(Sl.RoofB, new Vector3(0f, nt + 0.3f, zA + 6f), nx + 0.5f, 7.5f, 12, 0.15f);
        for (int k = -2; k <= 2; k++)
        {
            float a = k * 0.5f;
            b.Push(new Vector3(Mathf.Sin(a) * (nx + 0.02f), 0f, zA + 6f - Mathf.Cos(a) * (nx + 0.02f)), 180f + a * Mathf.Rad2Deg);
            TallWindow(b, 0f, P + 6.5f, 1.3f, 6.5f, 1);
            b.Pop();
        }
        // atap nave (pelana)
        float rise = GableZ(b, 0f, nt, (zA + 6f + zN1) * 0.5f, zN1 - (zA + 6f), nx * 2f, 1.15f, 0.6f, 0.3f, 13, Sl.RoofB, Sl.DarkStone, true);
        // atap lorong (lean-to) + penopang melayang
        for (int sd = -1; sd <= 1; sd += 2)
        {
            float xm = sd * (nx + aisleX) * 0.5f;
            b.Push(new Vector3(xm, 0f, (zA + 7f + zN1 - 1f) * 0.5f), sd > 0 ? 90f : 270f);
            V2Kit.Pent(b, Sl.RoofB, Sl.Wood, -(zN1 - 1f - zA - 7f) * 0.5f - 0.3f, (zN1 - 1f - zA - 7f) * 0.5f + 0.3f, -(aisleX - nx) * 0.5f - 0.1f, (aisleX - nx) * 0.5f + 0.6f, at + 1.6f, at - 0.4f, 17);
            b.Pop();
            for (int k = 0; k < 6; k++)
            {
                float z = zA + 9f + k * 6.4f;
                // tiang penopang luar
                b.Box(Sl.Stone, new Vector3(sd * (aisleX + 0.5f), P + 5.2f, z), new Vector3(1.1f, 10.4f, 1.1f));
                b.Spire(Sl.Stone, new Vector3(sd * (aisleX + 0.5f), P + 10.4f, z), 0.55f, 2.4f, 4, 0.1f);
                // busur penopang
                b.Beam(Sl.Stone, new Vector3(sd * (aisleX + 0.3f), P + 9.6f, z), new Vector3(sd * (nx + 0.2f), P + 14.4f, z), 0.7f, 0.6f, new Vector3(0f, 0f, 1f));
                b.Beam(Sl.DarkStone, new Vector3(sd * (aisleX - 0.2f), P + 8.6f, z), new Vector3(sd * (nx + 0.2f), P + 12.8f, z), 0.4f, 0.4f, new Vector3(0f, 0f, 1f));
                // jendela tinggi di nave & lorong
                b.Push(new Vector3(sd * nx, 0f, z + 3.2f), sd > 0 ? 90f : 270f);
                TallWindow(b, 0f, P + 10.8f, 1.5f, 4.6f, 1);
                b.Pop();
                b.Push(new Vector3(sd * aisleX, 0f, z + 3.2f), sd > 0 ? 90f : 270f);
                TallWindow(b, 0f, P + 3.2f, 1.2f, 3.4f, 0);
                b.Pop();
            }
        }
        // menara persilangan + puncak
        float cz = -3f;
        SquareTower(b, 0f, cz, 9.6f, nt - 4f, P + 34f, P + 28.4f, 7, Sl.Stone);
        b.Cyl(Sl.DarkStone, new Vector3(0f, P + 34.1f, cz), 5.2f, 0.4f, 8, 5.2f, true);
        b.Spire(Sl.RoofB, new Vector3(0f, P + 34.5f, cz), 4.6f, 13.2f, 8, 0.2f);
        // n81 kanon: jarum putih ramping menjulang dari puncak menara persilangan (titik tertinggi kota)
        b.Cyl(Sl.Stone, new Vector3(0f, P + 47.5f, cz), 1.5f, 1.3f, 8, 1.3f, true);
        b.Spire(Sl.RoofB, new Vector3(0f, P + 48.7f, cz), 1.35f, 27f, 8, 0.08f);
        b.Cyl(Sl.Gold, new Vector3(0f, P + 75.5f, cz), 0.1f, 2.4f, 6, 0.06f, true);
        b.Box(Sl.Gold, new Vector3(0f, P + 77.1f, cz), new Vector3(1.1f, 0.14f, 0.14f));
        b.Box(Sl.Gold, new Vector3(0f, P + 76.7f, cz), new Vector3(0.14f, 1.0f, 0.14f));
        // blok muka + dua menara
        float ft = P + 24f;
        StoneBlock(b, -14f, 14f, zN1 - 0.5f, zF, P, ft, Sl.Stone);
        for (int sd = -1; sd <= 1; sd += 2)
        {
            SquareTower(b, sd * 10.6f, (zN1 + zF) * 0.5f + 0.4f, 6.8f, ft - 1f, P + 31f, P + 26.4f, 9 + sd, Sl.Stone);
            b.Cyl(Sl.DarkStone, new Vector3(sd * 10.6f, P + 31.1f, (zN1 + zF) * 0.5f + 0.4f), 3.8f, 0.3f, 8, 3.8f, true);
            b.Spire(Sl.RoofB, new Vector3(sd * 10.6f, P + 31.4f, (zN1 + zF) * 0.5f + 0.4f), 3.6f, 10.5f, 8, 0.25f);
            b.Ball(Sl.Gold, new Vector3(sd * 10.6f, P + 42.2f, (zN1 + zF) * 0.5f + 0.4f), 0.2f, 3, 6);
        }
        // pelana depan tengah (setinggi ~30)
        b.Push(new Vector3(0f, 0f, zF), 0f);
        GableTri(b, Sl.Stone, 0f, ft, 7.1f, 6.2f, 0f, true);
        b.Beam(Sl.DarkStone, new Vector3(-7.3f, ft - 0.1f, 0.12f), new Vector3(0f, ft + 6.4f, 0.12f), 0.5f, 0.4f, new Vector3(0f, 0f, 1f));
        b.Beam(Sl.DarkStone, new Vector3(7.3f, ft - 0.1f, 0.12f), new Vector3(0f, ft + 6.4f, 0.12f), 0.5f, 0.4f, new Vector3(0f, 0f, 1f));
        b.Cyl(Sl.Gold, new Vector3(0f, ft + 6.4f, 0.12f), 0.08f, 2.0f, 6, 0.05f, true);
        b.Box(Sl.Gold, new Vector3(0f, ft + 7.6f, 0.12f), new Vector3(0.9f, 0.12f, 0.12f));
        PointedPortal(b, 0f, 5.0f, 7.0f, 6, true);
        TallWindow(b, -5.9f, P + 3.4f, 1.1f, 7.0f, 1);
        TallWindow(b, 5.9f, P + 3.4f, 1.1f, 7.0f, 1);
        RoseWindow(b, 0f, P + 19.5f, 3.2f, 12);
        // galeri relung patung di atas portal
        for (int k = -3; k <= 3; k++)
        {
            b.Box(Sl.DarkStone, new Vector3(k * 1.5f, P + 15.1f, 0.1f), new Vector3(0.95f, 2.2f, 0.18f));
            b.Box(Sl.Marble, new Vector3(k * 1.5f, P + 14.8f, 0.2f), new Vector3(0.34f, 1.3f, 0.3f));
            b.Ball(Sl.Marble, new Vector3(k * 1.5f, P + 15.65f, 0.2f), 0.2f, 3, 6);
        }
        b.Box(Sl.DarkStone, new Vector3(0f, P + 16.4f, 0.12f), new Vector3(11.4f, 0.4f, 0.4f));
        b.Pop();
        // tangga depan
        StairsFront(b, 0f, 14f, zF + 0.1f, P, 3, 0.7f);
        // n89 kanon: retak setipis rambut di muka (Town2Kit.CathedralCanon.cs)
        CathedralCracks(b, P, zF);
        // atap blok muka (sisi): datar berpagar
        return P + 77.9f;
    }

    // ------------------------------------------------------------------ AKADEMI / PERPUSTAKAAN (40 x 28, ~22)
    public static float Library(GeoBag b)
    {
        const float P = 1.4f;
        float hx = 20f, z0 = -14f, z1 = 10f, wt = P + 12.6f;
        b.Box(Sl.DarkStone, new Vector3(0f, (P - 0.7f) * 0.5f, -1f), new Vector3(hx * 2f + 2f, P + 0.7f, 29f));
        b.Box(Sl.Marble, new Vector3(0f, P - 0.08f, -1f), new Vector3(hx * 2f + 1.2f, 0.2f, 28.2f));
        StoneBlock(b, -hx, hx, z0, z1, P, wt, Sl.Marble);
        // jendela lengkung dua tingkat + pilaster
        for (int sd = 0; sd < 4; sd++)
        {
            bool longSide = sd % 2 == 0;
            float len = longSide ? hx * 2f : (z1 - z0);
            int nb = longSide ? 8 : 5;
            b.Push(new Vector3(0f, 0f, 0f), 0f);
            Vector3 c0 = sd == 0 ? new Vector3(0f, 0f, z1) : (sd == 2 ? new Vector3(0f, 0f, z0) : (sd == 1 ? new Vector3(hx, 0f, (z0 + z1) * 0.5f) : new Vector3(-hx, 0f, (z0 + z1) * 0.5f)));
            float yaw = sd == 0 ? 0f : (sd == 2 ? 180f : (sd == 1 ? 90f : 270f));
            b.Pop();
            b.Push(c0, yaw);
            for (int k = 0; k < nb; k++)
            {
                float x = -len * 0.5f + (k + 0.5f) * len / nb;
                if (sd == 0 && Mathf.Abs(x) < 8.5f) continue;
                for (int fl = 0; fl < 2; fl++)
                {
                    float y = P + 1.5f + fl * 5.6f, h = 3.4f, w = 1.7f;
                    b.Box(Sl.Stone, new Vector3(x, y + h * 0.5f, 0.03f), new Vector3(w + 0.5f, h + 0.1f, 0.12f));
                    V2Kit.ArchRing(b, Sl.Stone, x, y + h, w * 0.5f, w * 0.5f + 0.28f, 0f, 180f, 8, 0f, 0.16f);
                    b.Box(Sl.Glass, new Vector3(x, y + h * 0.5f, 0.08f), new Vector3(w, h, 0.04f));
                    V2Kit.DiscV(b, Sl.Glass, x, y + h, w * 0.5f, 0.1f, 8, 0f, 180f);
                    b.Box(Sl.Stone, new Vector3(x, y + h * 0.55f, 0.11f), new Vector3(0.06f, h * 1.1f, 0.04f));
                    b.Box(Sl.Stone, new Vector3(x, y + h * 0.5f, 0.11f), new Vector3(w, 0.06f, 0.04f));
                }
                b.Box(Sl.Marble, new Vector3(x + len / nb * 0.5f, P + 6.0f, 0.1f), new Vector3(0.55f, 11.6f, 0.2f));
            }
            b.Pop();
        }
        // pelana kecil di sisi + atap limas hijau tembaga
        HipRoof(b, Sl.RoofB, Sl.DarkStone, Sl.Wood, new Vector3(0f, wt, (z0 + z1) * 0.5f), hx * 2f + 0.4f, (z1 - z0) + 0.4f, 4.8f, 0.9f, 31, 0.55f);
        // kubah pusat
        float dy = wt + 3.3f;
        b.Cyl(Sl.Marble, new Vector3(0f, wt + 1.6f, (z0 + z1) * 0.5f - 1f), 6.6f, 3.0f, 16, 6.6f, false);
        for (int k = 0; k < 8; k++)
        {
            float a = k * Mathf.PI * 2f / 8f;
            b.Push(new Vector3(Mathf.Sin(a) * 6.62f, wt + 2.0f, (z0 + z1) * 0.5f - 1f + Mathf.Cos(a) * 6.62f), a * Mathf.Rad2Deg);
            b.Box(Sl.Glass, new Vector3(0f, 0.8f, 0.02f), new Vector3(0.9f, 1.8f, 0.06f));
            b.Box(Sl.Stone, new Vector3(0f, 1.8f, 0.04f), new Vector3(1.2f, 0.16f, 0.12f));
            b.Pop();
        }
        b.Cyl(Sl.DarkStone, new Vector3(0f, dy + 0.2f, (z0 + z1) * 0.5f - 1f), 6.9f, 0.4f, 16, 6.9f, true);
        b.Dome(Sl.RoofB, new Vector3(0f, dy + 0.4f, (z0 + z1) * 0.5f - 1f), 6.6f, 6, 16, 0.85f);
        b.Cyl(Sl.Marble, new Vector3(0f, dy + 0.4f + 5.4f, (z0 + z1) * 0.5f - 1f), 1.0f, 1.8f, 8, 0.95f, true);
        b.Spire(Sl.RoofB, new Vector3(0f, dy + 0.4f + 7.2f, (z0 + z1) * 0.5f - 1f), 1.15f, 2.4f, 8, 0.1f);
        b.Ball(Sl.Gold, new Vector3(0f, dy + 0.4f + 9.8f, (z0 + z1) * 0.5f - 1f), 0.3f, 4, 8);
        // portico enam tiang + pediment + tangga
        float pz = z1 + 3.4f;
        b.Box(Sl.Marble, new Vector3(0f, P + 0.0f, (z1 + pz) * 0.5f + 0.2f), new Vector3(19.6f, 0.2f, pz - z1 + 0.8f));
        for (int k = 0; k < 6; k++)
        {
            float x = -8.3f + k * 3.32f;
            b.Cyl(Sl.Marble, new Vector3(x, P, pz), 0.72f, 0.5f, 10, 0.72f, true);
            b.Cyl(Sl.Marble, new Vector3(x, P + 0.5f, pz), 0.58f, 10.2f, 10, 0.5f, false);
            b.Cyl(Sl.Gold, new Vector3(x, P + 10.7f, pz), 0.62f, 0.26f, 10, 0.78f, true);
            b.Box(Sl.Marble, new Vector3(x, P + 11.1f, pz), new Vector3(1.4f, 0.28f, 1.4f));
        }
        b.Box(Sl.Marble, new Vector3(0f, P + 11.75f, (z1 + pz) * 0.5f + 0.1f), new Vector3(19.2f, 1.1f, pz - z1 + 1.6f));
        b.Box(Sl.DarkStone, new Vector3(0f, P + 12.5f, (z1 + pz) * 0.5f + 0.1f), new Vector3(19.8f, 0.3f, pz - z1 + 2.0f));
        b.Push(new Vector3(0f, P + 12.65f, pz + 0.9f), 0f);
        b.Tri(Sl.Marble, new Vector3(-9.6f, 0f, 0f), new Vector3(9.6f, 0f, 0f), new Vector3(0f, 3.6f, 0f));
        V2Kit.DiscV(b, Sl.Gold, 0f, 1.0f, 1.0f, 0.06f, 12);
        V2Kit.ArchRing(b, Sl.Gold, 0f, 1.0f, 1.0f, 1.25f, 0f, 360f, 12, 0.02f, 0.1f);
        b.Pop();
        b.Beam(Sl.DarkStone, new Vector3(-9.9f, P + 12.8f, pz + 0.9f), new Vector3(0f, P + 16.4f, pz + 0.9f), 0.5f, 0.3f, new Vector3(0f, 0f, 1f));
        b.Beam(Sl.DarkStone, new Vector3(9.9f, P + 12.8f, pz + 0.9f), new Vector3(0f, P + 16.4f, pz + 0.9f), 0.5f, 0.3f, new Vector3(0f, 0f, 1f));
        // pintu utama
        b.Push(new Vector3(0f, 0f, z1), 0f);
        b.Box(Sl.DarkStone, new Vector3(0f, P + 3.1f, 0.06f), new Vector3(4.2f, 6.2f, 0.12f));
        V2Kit.ArchRing(b, Sl.Gold, 0f, P + 5.2f, 1.7f, 2.0f, 0f, 180f, 10, 0f, 0.2f);
        b.Box(Sl.Wood, new Vector3(0f, P + 2.5f, 0.1f), new Vector3(3.2f, 5.0f, 0.08f));
        V2Kit.DiscV(b, Sl.Wood, 0f, P + 5.0f, 1.6f, 0.1f, 10, 0f, 180f);
        for (int k = -3; k <= 3; k++) b.Box(Sl.WoodLight, new Vector3(k * 0.44f, P + 2.5f, 0.15f), new Vector3(0.06f, 5.0f, 0.03f));
        b.Box(Sl.Gold, new Vector3(0f, P + 2.5f, 0.18f), new Vector3(0.1f, 5.0f, 0.04f));
        b.Pop();
        StairsFront(b, 0f, 12f, pz + 0.7f, P, 6, 0.55f);
        for (int sd = -1; sd <= 1; sd += 2) Brazier(b, new Vector3(sd * 7.6f, 0f, pz + 3.8f), 2.3f);
        return P + 16.4f + 4.4f;
    }

    // ------------------------------------------------------------------ MENARA SIHIR (r 9, ~44)
    public static float MageTower(GeoBag b)
    {
        const float P = 0.6f;
        // aula bundar di dasar
        b.Cyl(Sl.DarkStone, new Vector3(0f, -0.7f, 0f), 8.9f, 1.3f + P, 16, 8.7f, true);
        b.Cyl(Sl.Stone, new Vector3(0f, P, 0f), 8.0f, 8.4f, 16, 7.8f, false);
        b.Cyl(Sl.DarkStone, new Vector3(0f, P + 8.4f, 0f), 8.2f, 0.4f, 16, 8.4f, true);
        b.Spire(Sl.RoofB, new Vector3(0f, P + 8.8f, 0f), 8.6f, 3.6f, 16, 0.1f);
        for (int k = 0; k < 8; k++)
        {
            float a = k * Mathf.PI * 2f / 8f + 0.2f;
            b.Push(new Vector3(Mathf.Sin(a) * 8.0f, 0f, Mathf.Cos(a) * 8.0f), a * Mathf.Rad2Deg);
            TallWindow(b, 0f, P + 2.6f, 1.2f, 3.6f, 0);
            b.Pop();
        }
        // pintu utama (muka +Z)
        b.Push(new Vector3(0f, 0f, 7.9f), 0f);
        b.Box(Sl.DarkStone, new Vector3(0f, P + 2.4f, 0.05f), new Vector3(5.2f, 4.8f, 0.3f));
        PointedPortal(b, 0f, 2.8f, 3.3f, 4, true);
        b.Pop();
        // badan menara
        b.Cyl(Sl.Stone, new Vector3(0f, P + 8.4f, 0f), 4.5f, 21.5f, 12, 3.9f, false);
        for (int k = 0; k < 4; k++)
        {
            float y = P + 12f + k * 5.4f;
            float a = k * 1.4f;
            b.Push(new Vector3(Mathf.Sin(a) * 4.3f, y, Mathf.Cos(a) * 4.3f), a * Mathf.Rad2Deg);
            TallWindow(b, 0f, 0f, 1.0f, 2.4f, 0);
            b.Pop();
        }
        // balkon cincin + pita rune bercahaya
        float[] by = { P + 15.2f, P + 21.8f, P + 27.4f };
        float[] br = { 5.6f, 5.1f, 4.6f };
        for (int k = 0; k < 3; k++)
        {
            b.Cyl(Sl.DarkStone, new Vector3(0f, by[k] - 0.3f, 0f), br[k], 0.36f, 12, br[k], true);
            for (int q = 0; q < 12; q++)
            {
                float a = q * Mathf.PI * 2f / 12f;
                b.Box(Sl.Metal, new Vector3(Mathf.Sin(a) * (br[k] - 0.12f), by[k] + 0.45f, Mathf.Cos(a) * (br[k] - 0.12f)), new Vector3(0.07f, 0.9f, 0.07f));
            }
            b.Cyl(Sl.Glow, new Vector3(0f, by[k] - 3.0f, 0f), 4.28f - k * 0.17f, 0.3f, 12, 4.28f - k * 0.17f, false);
        }
        // pendopo pengamatan: delapan tiang terbuka, lantai & atap
        float oy = P + 29.9f;
        b.Cyl(Sl.DarkStone, new Vector3(0f, oy - 0.2f, 0f), 4.9f, 0.5f, 12, 4.9f, true);
        for (int k = 0; k < 8; k++)
        {
            float a = k * Mathf.PI * 2f / 8f;
            b.Cyl(Sl.Marble, new Vector3(Mathf.Sin(a) * 4.0f, oy + 0.3f, Mathf.Cos(a) * 4.0f), 0.28f, 4.2f, 8, 0.24f, true);
        }
        b.Cyl(Sl.DarkStone, new Vector3(0f, oy + 4.5f, 0f), 4.7f, 0.4f, 12, 4.7f, true);
        b.Spire(Sl.RoofB, new Vector3(0f, oy + 4.9f, 0f), 4.9f, 9.2f, 12, 0.34f);
        b.Cyl(Sl.Gold, new Vector3(0f, oy + 13.9f, 0f), 0.09f, 1.9f, 6, 0.05f, true);
        b.Ball(Sl.Gold, new Vector3(0f, oy + 15.9f, 0f), 0.24f, 3, 6);
        // kristal melayang & cincin
        b.Ball(Sl.Glow, new Vector3(0f, oy + 2.4f, 0f), 1.3f, 5, 10, 1.45f);
        V2Kit.ArchRing(b, Sl.Gold, 0f, oy + 2.4f, 2.2f, 2.34f, 0f, 360f, 16, -0.03f, 0.03f);
        b.Push(new Vector3(0f, oy + 2.4f, 0f), 90f);
        V2Kit.ArchRing(b, Sl.Gold, 0f, 0f, 2.6f, 2.72f, 0f, 360f, 16, -0.03f, 0.03f);
        b.Pop();
        for (int k = 0; k < 5; k++)
        {
            float a = k * Mathf.PI * 2f / 5f;
            b.Ball(Sl.Glow, new Vector3(Mathf.Sin(a) * 3.1f, oy + 1.4f + 0.7f * (k % 2), Mathf.Cos(a) * 3.1f), 0.22f, 3, 6, 1.6f);
        }
        return oy + 16.2f;
    }

    // ------------------------------------------------------------------ KASTIL PENGUASA (76 x 64, ~50)
    public static float Castle(GeoBag b)
    {
        const float P = 0.9f;
        // lantai plato
        b.Box(Sl.DarkStone, new Vector3(0f, (P - 0.7f) * 0.5f, 0f), new Vector3(77f, P + 0.7f, 65f));
        // keep utama
        float kt = P + 24f;
        StoneBlock(b, -14f, 14f, -20f, 8f, P, kt, Sl.Stone);
        b.Crenellate(Sl.Stone, new Vector3(-14f, kt, 8f), new Vector3(14f, kt, 8f), 1.2f, 0.7f, 1.4f, 1.0f);
        HipRoof(b, Sl.RoofB, Sl.DarkStone, Sl.Wood, new Vector3(0f, kt + 0.4f, -6f), 25.6f, 24.6f, 9.5f, 0.9f, 3, 0.6f);
        // sayap kiri-kanan
        float wt = P + 15f;
        for (int sd = -1; sd <= 1; sd += 2)
        {
            StoneBlock(b, sd > 0 ? 14f : -30f, sd > 0 ? 30f : -14f, -13f, 12f, P, wt, Sl.Stone);
            HipRoof(b, Sl.RoofB, Sl.DarkStone, Sl.Wood, new Vector3(sd * 22f, wt + 0.2f, -0.5f), 16.8f, 25.8f, 6.2f, 0.8f, 5 + sd, 0.55f);
            for (int fl = 0; fl < 2; fl++)
                for (int k = 0; k < 3; k++)
                {
                    b.Push(new Vector3(sd * 22f + (k - 1) * 4.6f, 0f, 12f), 0f);
                    TallWindow(b, 0f, P + 1.8f + fl * 6.4f, 1.3f, fl == 0 ? 3.9f : 3.4f, 1);
                    b.Pop();
                }
        }
        // keep: jendela besar dan balkon
        for (int k = 0; k < 4; k++)
        {
            b.Push(new Vector3(-9.2f + k * 6.1f, 0f, 8f), 0f);
            if (k == 1 || k == 2) { b.Pop(); continue; }
            TallWindow(b, 0f, P + 9f, 1.5f, 6.5f, 1);
            TallWindow(b, 0f, P + 1.8f, 1.3f, 4.2f, 1);
            b.Pop();
        }
        // menara gerbang depan (tengah)
        float gz = 8f, gw = 12.2f;
        StoneBlock(b, -gw * 0.5f, gw * 0.5f, gz, gz + 8f, P, P + 22f, Sl.Stone);
        b.Push(new Vector3(0f, 0f, gz + 8f), 0f);
        PointedPortal(b, 0f, 6.0f, 7.5f, 6, true);
        // balkon lengkung di atas portal
        b.Box(Sl.DarkStone, new Vector3(0f, P + 14.2f, 0.7f), new Vector3(9.4f, 0.5f, 1.8f));
        for (int k = -4; k <= 4; k++) b.Box(Sl.Marble, new Vector3(k * 1.0f, P + 15.1f, 1.4f), new Vector3(0.14f, 1.2f, 0.14f));
        b.Box(Sl.Marble, new Vector3(0f, P + 15.8f, 1.4f), new Vector3(9.4f, 0.16f, 0.24f));
        for (int k = -1; k <= 1; k++) TallWindow(b, k * 3.0f, P + 16.6f, 1.2f, 3.0f, 0);
        Banner(b, new Vector3(-4.8f, P + 20.6f, 0.2f), 1.8f, 7.0f, Sl.Cloth, Sl.Gold);
        Banner(b, new Vector3(4.8f, P + 20.6f, 0.2f), 1.8f, 7.0f, Sl.Cloth, Sl.Gold);
        b.Pop();
        // menara tengah beratap lancip
        b.Cyl(Sl.Stone, new Vector3(0f, P + 22f, gz + 2f), 5.6f, 12.5f, 8, 5.2f, false);
        for (int sd = 0; sd < 8; sd += 2)
        {
            float a = sd * Mathf.PI * 2f / 8f + Mathf.PI / 8f;
            b.Push(new Vector3(Mathf.Sin(a) * 5.4f, P + 28f, gz + 2f + Mathf.Cos(a) * 5.4f), a * Mathf.Rad2Deg);
            TallWindow(b, 0f, 0f, 1.1f, 3.4f, 1);
            b.Pop();
        }
        b.Cyl(Sl.DarkStone, new Vector3(0f, P + 34.5f, gz + 2f), 5.9f, 0.5f, 8, 5.9f, true);
        b.Spire(Sl.RoofB, new Vector3(0f, P + 35f, gz + 2f), 6.0f, 13.4f, 8, 0.3f);
        b.Cyl(Sl.Gold, new Vector3(0f, P + 48.2f, gz + 2f), 0.1f, 2.4f, 6, 0.05f, true);
        b.Ball(Sl.Gold, new Vector3(0f, P + 50.7f, gz + 2f), 0.3f, 3, 6);
        // menara sudut besar
        foreach (float sx in new[] { -1f, 1f })
        {
            RoundTower(b, sx * 34.2f, 17f, 5.0f, P, P + 26f, 11.5f, Sl.Stone, Sl.RoofB, 7 + (int)sx, 6);
            RoundTower(b, sx * 34.2f, -15f, 4.4f, P, P + 22f, 9.5f, Sl.Stone, Sl.RoofB, 9 + (int)sx, 5);
            RoundTower(b, sx * 15f, -22f, 4.6f, P, P + 30f, 10.5f, Sl.Stone, Sl.RoofB, 11 + (int)sx, 6);
        }
        // tembok penghubung berbenteng
        for (int sd = -1; sd <= 1; sd += 2)
        {
            b.Box(Sl.Stone, new Vector3(sd * 31.5f, P + 5.2f, 2f), new Vector3(2.2f, 10.4f, 32f));
            b.Crenellate(Sl.Stone, new Vector3(sd * 31.5f, P + 10.4f, -14f), new Vector3(sd * 31.5f, P + 10.4f, 18f), 1.1f, 0.7f, 1.3f, 1.0f);
        }
        // tangga lebar & obor
        StairsFront(b, 0f, 26f, gz + 8.3f, P, 5, 0.7f);
        for (int sd = -1; sd <= 1; sd += 2)
        {
            Brazier(b, new Vector3(sd * 8f, P, gz + 12.5f), 2.4f);
            Brazier(b, new Vector3(sd * 12.5f, P, gz + 12.5f), 2.4f);
        }
        return P + 50.9f;
    }

    // ------------------------------------------------------------------ RUMAH BANGSAWAN (26 x 22, ~15): v = variasi
    public static float Manor(GeoBag b, int v)
    {
        bool mir = v % 2 == 1;
        float sg = mir ? -1f : 1f;
        var main = new TSpec { w = 14f, d = 16f, floors = 3, gh = 3.5f, fh = 2.9f, roof = 2, pitch = v % 4 < 2 ? 0.68f : 0.6f, gnd = 0, upper = v % 3 == 0 ? 1 : 0, jetty = false, doorX = sg * -1.8f, balcony = v % 2 == 0, turret = false, sign = false, seed = 71 + v * 13, sideL = true, sideR = true, chimney = mir ? 0 : 1, planters = true };
        b.Push(new Vector3(sg * -3.6f, 0f, -1f), 0f); TownHouse(b, main); b.Pop();
        var wing = new TSpec { w = 9f, d = 12f, floors = 2, gh = 3.2f, fh = 2.8f, roof = 1, pitch = 0.88f, gnd = 0, upper = 0, jetty = true, doorX = 0f, seed = 83 + v * 7, sideL = mir, sideR = !mir, chimney = mir ? 1 : 0, shutters = true };
        b.Push(new Vector3(sg * 8.8f, 0f, -3f), 0f); TownHouse(b, wing); b.Pop();
        // menara tangga bulat di sudut dalam
        RoundTower(b, sg * 3.9f, 4.4f, 2.1f, BaseY, BaseY + 13.5f, 5.2f, Sl.Stone, Sl.RoofB, 61 + v, 3);
        // beranda depan bertiang
        float zf = 8f - 1f;
        for (int k = 0; k < 4; k++)
        {
            float x = sg * -3.6f + (k - 1.5f) * 1.5f + sg * -1.8f * 0.0f;
            b.Cyl(Sl.Marble, new Vector3(x, BaseY, zf + 1.7f), 0.24f, 3.4f, 10, 0.2f, true);
        }
        b.Push(new Vector3(sg * -3.6f, 0f, zf + 1.7f), 0f);
        b.Box(Sl.Marble, new Vector3(0f, BaseY + 3.55f, 0f), new Vector3(5.6f, 0.3f, 2.8f));
        b.Tri(Sl.Marble, new Vector3(-2.8f, BaseY + 3.7f, 1.4f), new Vector3(2.8f, BaseY + 3.7f, 1.4f), new Vector3(0f, BaseY + 5.0f, 1.4f));
        b.Pop();
        // pagar besi & pilar gerbang halaman
        return 15.4f;
    }

    // ------------------------------------------------------------------ KINCIR ANGIN (10 x 10, ~24)
    public static float Windmill(GeoBag b) { return Windmill(b, true); }

    /// <param name="blades">false = hanya badan kincir; bilah dibuat terpisah (WindmillBlades) agar bisa berputar di builder</param>
    public static float Windmill(GeoBag b, bool blades)
    {
        b.Cyl(Sl.DarkStone, new Vector3(0f, -0.6f, 0f), 5.2f, 1.3f, 12, 4.9f, true);
        b.Cyl(Sl.Stone, new Vector3(0f, 0.6f, 0f), 4.6f, 12.4f, 12, 3.2f, false);
        for (int k = 0; k < 3; k++)
        {
            float a = k * 2.1f + 0.4f;
            b.Push(new Vector3(Mathf.Sin(a) * 4.2f, 5.0f + k * 3.6f, Mathf.Cos(a) * 4.2f), a * Mathf.Rad2Deg);
            Win(b, 0f, 0f, 0.8f, 1.1f, 9 + k, 2 | 1);
            b.Pop();
        }
        b.Push(new Vector3(0f, 0f, 4.3f), 0f);
        b.Box(Sl.DarkStone, new Vector3(0f, 1.5f, 0.05f), new Vector3(2.4f, 3.0f, 0.14f));
        b.Box(Sl.Wood, new Vector3(0f, 1.3f, 0.12f), new Vector3(1.7f, 2.6f, 0.08f));
        b.Pop();
        b.Cyl(Sl.DarkStone, new Vector3(0f, 13.0f, 0f), 3.6f, 0.4f, 12, 3.9f, true);
        // topi kayu berbentuk perahu terbalik
        b.Cyl(Sl.Wood, new Vector3(0f, 13.4f, 0f), 3.4f, 2.6f, 12, 3.0f, false);
        b.Spire(Sl.RoofB, new Vector3(0f, 16.0f, 0f), 3.7f, 4.4f, 12, 0.2f);
        b.Ball(Sl.Gold, new Vector3(0f, 20.55f, 0f), 0.2f, 3, 6);
        // poros + empat bilah layar kisi (menghadap +Z)
        float hy = 15.0f, hz = 3.5f;
        b.Cyl(Sl.Metal, new Vector3(0f, hy, 2.4f), 0.28f, 2.0f, 8, 0.28f, true);
        if (blades) WindmillBlades(b, new Vector3(0f, hy, hz + 0.8f));
        return 21.1f;
    }

    /// <summary>Empat bilah layar kisi di sekitar poros (hub di 'hub', bidang XY, menghadap +Z).</summary>
    public static void WindmillBlades(GeoBag b, Vector3 hub)
    {
        b.Push(hub, 0f);
        for (int k = 0; k < 4; k++)
        {
            float a = (k * 90f + 18f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f), nrm = new Vector3(-Mathf.Sin(a), Mathf.Cos(a), 0f);
            b.Beam(Sl.Wood, dir * 0.6f, dir * 10.8f, 0.34f, 0.26f, new Vector3(0f, 0f, 1f));
            // kisi layar
            for (int r = 0; r < 7; r++)
            {
                Vector3 c = dir * (3.0f + r * 1.06f) + nrm * 1.5f;
                b.Beam(Sl.WoodLight, c - nrm * 1.1f, c + nrm * 1.1f, 0.12f, 0.1f, new Vector3(0f, 0f, 1f));
            }
            b.Beam(Sl.Wood, dir * 2.4f + nrm * 2.55f, dir * 10.6f + nrm * 2.55f, 0.12f, 0.1f, new Vector3(0f, 0f, 1f));
            for (int r = 0; r < 4; r++)
            {
                Vector3 c = dir * (3.6f + r * 2.2f) + nrm * 1.5f;
                b.QuadBoth(Sl.Cloth, c - dir * 1.05f - nrm * 1.0f, c + dir * 1.05f - nrm * 1.0f, c + dir * 1.05f + nrm * 1.0f, c - dir * 1.05f + nrm * 1.0f);
            }
        }
        b.Ball(Sl.Metal, new Vector3(0f, 0f, 0f), 0.5f, 4, 8);
        b.Pop();
    }
}
