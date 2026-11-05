using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bangunan sipil Kota Ashwatch: Guild Hall, Penginapan, Kedai, Toko, Pandai Besi, Barak, Istal.
/// Konvensi: muka = +Z, pusat denah di titik asal, alas tanah y = 0. Ukuran selaras dengan T2.Size (Town2PlanData.cs).
/// </summary>
public static partial class T2Kit
{
    // ------------------------------------------------------------------ Guild Hall (42 x 30, tinggi ~24)
    public static float Guild(GeoBag b)
    {
        const float P = 1.0f;                       // tinggi podium
        // podium bertingkat
        b.Box(Sl.DarkStone, new Vector3(0f, (P - 0.7f) * 0.5f, -0.5f), new Vector3(44.6f, P + 0.7f, 31f));
        b.Box(Sl.Stone, new Vector3(0f, P - 0.06f, -0.5f), new Vector3(44.0f, 0.14f, 30.4f));
        // aula utama (batu) dengan penopang & jendela tinggi
        float hx = 12f, z0 = -15f, z1 = 11f, wallTop = P + 10.5f;
        StoneBlock(b, -hx, hx, z0, z1, P, wallTop, Sl.Stone);
        for (int sd = -1; sd <= 1; sd += 2)
        {
            for (int k = 0; k < 5; k++)
            {
                float z = z0 + 1.6f + k * 5.5f;
                b.Push(new Vector3(sd * hx, 0f, z), sd > 0 ? 90f : 270f);
                Buttress(b, 0f, P + 0.6f, 8.6f, 0.9f);
                b.Pop();
            }
            for (int k = 0; k < 4; k++)
            {
                float z = z0 + 4.35f + k * 5.5f;
                b.Push(new Vector3(sd * hx, 0f, z), sd > 0 ? 90f : 270f);
                TallWindow(b, 0f, P + 2.5f, 1.5f, 5.2f, 1);
                b.Pop();
            }
        }
        // dinding belakang: jendela mawar + tiga jendela tinggi
        b.Push(new Vector3(0f, 0f, z0), 180f);
        RoseWindow(b, 0f, P + 12.8f, 2.0f, 8);
        for (int k = -1; k <= 1; k++) TallWindow(b, k * 5.6f, P + 2.5f, 1.5f, 5.2f, 1);
        b.Pop();
        // gable depan: atap + dinding segitiga batu
        float rise = GableZ(b, 0f, wallTop, (z0 + z1) * 0.5f, z1 - z0, hx * 2f, 0.95f, 0.75f, 0.55f, 11, Sl.RoofB, Sl.DarkStone, true);
        // muka depan: portal, jendela mawar, spanduk
        b.Push(new Vector3(0f, 0f, z1), 0f);
        PointedPortal(b, 0f, 5.8f, 6.0f, 4, true);
        TallWindow(b, -8.2f, P + 2.5f, 1.3f, 4.6f, 1);
        TallWindow(b, 8.2f, P + 2.5f, 1.3f, 4.6f, 1);
        RoseWindow(b, 0f, wallTop + 6.2f, 2.3f, 8);
        b.Box(Sl.DarkStone, new Vector3(0f, wallTop + 3.3f, 0.12f), new Vector3(10.6f, 0.34f, 0.3f));
        for (int sd = -1; sd <= 1; sd += 2)
        {
            Banner(b, new Vector3(sd * 4.6f, wallTop - 1.0f, 0.28f), 1.9f, 6.2f, Sl.Cloth, Sl.ClothB);
            Banner(b, new Vector3(sd * 10.2f, wallTop - 0.2f, 0.28f), 1.5f, 5.4f, Sl.ClothB, Sl.Gold);
        }
        // lambang matahari emas di puncak gable
        b.Cyl(Sl.Gold, new Vector3(0f, wallTop + rise - 0.3f, 0.9f), 0.12f, 1.7f, 6, 0.06f, true);
        b.Ball(Sl.Gold, new Vector3(0f, wallTop + rise + 1.6f, 0.9f), 0.34f, 4, 8);
        b.Pop();
        // sayap kiri & kanan (rumah dua lantai bergaya kota, pintu samping)
        for (int sd = -1; sd <= 1; sd += 2)
        {
            var w = new TSpec { w = 9f, d = 24f, floors = 2, gh = 4.4f, fh = 3.5f, roof = 1, pitch = 0.9f, gnd = 0, upper = 0, jetty = false, sideL = sd < 0, sideR = sd > 0, doorX = sd * -1.2f, chimney = sd < 0 ? 0 : 1, seed = 41 + sd * 3, shutters = true, planters = true };
            b.Push(new Vector3(sd * 16.5f, P - BaseY, 0f), 0f);
            TownHouse(b, w);
            b.Pop();
        }
        // menara bulat di sudut depan
        for (int sd = -1; sd <= 1; sd += 2) RoundTower(b, sd * 23.0f, 7.4f, 3.9f, P, P + 15.4f, 7.4f, Sl.Stone, Sl.RoofB, 5 + sd, 5);
        // tangga depan & obor
        StairsFront(b, 0f, 11.5f, z1 - 0.3f, P, 4, 0.62f);
        for (int sd = -1; sd <= 1; sd += 2)
        {
            Brazier(b, new Vector3(sd * 6.0f, P, z1 + 1.5f), 2.1f);
            b.Box(Sl.DarkStone, new Vector3(sd * 6.0f, P + 0.3f, z1 + 1.5f), new Vector3(0.9f, 0.6f, 0.9f));
        }
        return wallTop + rise + 2.2f;
    }

    // ------------------------------------------------------------------ Penginapan (24 x 18, ~14)
    public static float Inn(GeoBag b)
    {
        var a = new TSpec { w = 14f, d = 18f, floors = 3, gh = 3.1f, fh = 2.55f, roof = 1, pitch = 0.82f, jetty = true, doorX = 5.0f, balcony = true, sign = true, chimney = 0, seed = 17, sideL = true, sideR = false, hoist = true, shutters = true };
        b.Push(new Vector3(-5f, 0f, 0f), 0f); TownHouse(b, a); b.Pop();
        var c = new TSpec { w = 10f, d = 14f, floors = 2, gh = 3.2f, fh = 2.6f, roof = 0, pitch = 0.7f, gnd = 3, doorX = -1.2f, chimney = 1, seed = 23, sideL = false, sideR = true, dormers = 1, shutters = true, planters = true };
        b.Push(new Vector3(7f, 0f, -2f), 0f); TownHouse(b, c); b.Pop();
        // beranda kayu beratap di depan sayap kanan
        b.Push(new Vector3(7f, 0f, 5.1f), 0f);
        for (int k = 0; k < 4; k++) b.Cyl(Sl.Wood, new Vector3(-4f + k * 2.67f, 0f, 1.6f), 0.14f, 3.2f, 6, 0.12f, true);
        V2Kit.Pent(b, Sl.Roof, Sl.Wood, -4.4f, 4.4f, 0.0f, 2.0f, 3.9f, 3.0f, 29);
        b.Pop();
        // bangku & tong di depan
        b.Box(Sl.Wood, new Vector3(2.4f, 0.55f, 9.7f), new Vector3(1.8f, 0.12f, 0.5f));
        b.Box(Sl.Wood, new Vector3(2.4f, 0.28f, 9.7f), new Vector3(1.6f, 0.4f, 0.1f));
        V2Kit.Barrel(b, new Vector3(-2.4f, 0f, 9.9f));
        return 16.4f;
    }

    // ------------------------------------------------------------------ Kedai minum (20 x 15, ~12)
    public static float Tavern(GeoBag b)
    {
        var s = new TSpec { w = 20f, d = 15f, floors = 2, gh = 3.5f, fh = 3.0f, roof = 0, pitch = 0.66f, jetty = true, doorX = 0f, sign = true, chimney = 1, seed = 9, dormers = 2, shutters = true, planters = true, balcony = true };
        float h = TownHouse(b, s);
        // beranda depan beratap
        float zf = 7.5f + 0.0f;
        for (int k = 0; k < 6; k++) b.Cyl(Sl.Wood, new Vector3(-7.5f + k * 3f, 0f, zf + 2.2f), 0.15f, 3.0f, 6, 0.13f, true);
        V2Kit.Pent(b, Sl.RoofB, Sl.Wood, -8.2f, 8.2f, zf - 0.2f, zf + 2.6f, 3.6f, 2.9f, 31);
        // tong besar & meja luar
        V2Kit.Barrel(b, new Vector3(-9.3f, 0f, zf + 1.6f));
        V2Kit.Barrel(b, new Vector3(-8.6f, 0f, zf + 2.4f));
        for (int k = 0; k < 2; k++)
        {
            float tx = 4.5f + k * 3.2f;
            b.Cyl(Sl.Wood, new Vector3(tx, 0.62f, zf + 1.6f), 0.55f, 0.1f, 8, 0.55f, true);
            b.Cyl(Sl.Wood, new Vector3(tx, 0f, zf + 1.6f), 0.1f, 0.62f, 6, 0.08f, true);
        }
        return h;
    }

    // ------------------------------------------------------------------ Toko umum (17 x 14, ~11)
    public static float Shop(GeoBag b)
    {
        var s = new TSpec { w = 17f, d = 14f, floors = 2, gh = 3.4f, fh = 2.8f, roof = 0, pitch = 0.62f, gnd = 1, awning = true, sign = true, jetty = true, doorX = 0f, chimney = 0, seed = 5, dormers = 2, shutters = true, planters = true };
        float h = TownHouse(b, s);
        // dagangan di depan: peti, karung, keranjang
        V2Kit.Crate(b, new Vector3(-6.4f, 0f, 8.4f), 0.9f, 10f);
        V2Kit.Crate(b, new Vector3(-6.4f, 0.9f, 8.45f), 0.7f, -6f);
        V2Kit.Crate(b, new Vector3(-5.3f, 0f, 8.7f), 0.8f, 3f);
        V2Kit.Sack(b, new Vector3(6.1f, 0f, 8.5f), 0.9f);
        V2Kit.Sack(b, new Vector3(6.9f, 0f, 8.3f), 0.8f);
        V2Kit.Barrel(b, new Vector3(5.5f, 0f, 8.9f));
        return h;
    }

    // ------------------------------------------------------------------ Pandai besi (16 x 13, ~10)
    public static float Smithy(GeoBag b)
    {
        var m = new TSpec { w = 9f, d = 12f, floors = 2, gh = 3.9f, fh = 2.5f, roof = 0, pitch = 0.62f, gnd = 0, doorX = -1.5f, chimney = -1, seed = 61, sideL = true, sideR = false, sign = true, shutters = true };
        b.Push(new Vector3(-3.5f, 0f, 0.5f), 0f); TownHouse(b, m); b.Pop();
        // cerobong tinggi di belakang tungku
        b.Box(Sl.Stone, new Vector3(2.4f, 5.4f, -3.2f), new Vector3(1.7f, 9.4f, 1.7f));
        b.Box(Sl.DarkStone, new Vector3(2.4f, 10.0f, -3.2f), new Vector3(2.1f, 0.22f, 2.1f));
        b.Box(Sl.DarkStone, new Vector3(2.4f, 10.25f, -3.2f), new Vector3(1.8f, 0.2f, 1.8f));
        // bengkel terbuka (tiang + atap miring ke belakang)
        float x0 = 1.2f, x1 = 8.0f, zb = -5.0f, zf = 5.8f;
        foreach (float px in new[] { x0 + 0.3f, x1 - 0.3f })
            foreach (float pz in new[] { zf - 0.3f, zb + 3.0f }) b.Cyl(Sl.Wood, new Vector3(px, 0f, pz), 0.16f, pz > 0 ? 3.2f : 3.8f, 6, 0.14f, true);
        V2Kit.Pent(b, Sl.RoofB, Sl.Wood, x0 - 0.3f, x1 + 0.3f, zb, zf + 0.4f, 4.2f, 3.2f, 71);
        // lantai batu + tungku dengan bara
        b.Box(Sl.Cobble, new Vector3((x0 + x1) * 0.5f, 0.05f, (zb + zf) * 0.5f), new Vector3(x1 - x0, 0.1f, zf - zb));
        b.Box(Sl.Stone, new Vector3(2.4f, 0.55f, -3.2f), new Vector3(2.4f, 1.1f, 1.7f));
        b.Box(Sl.Glow, new Vector3(2.4f, 1.12f, -3.0f), new Vector3(1.6f, 0.08f, 1.0f));
        b.Box(Sl.DarkStone, new Vector3(2.4f, 1.9f, -3.9f), new Vector3(2.0f, 1.6f, 0.4f));
        // paron di tunggul, tong air, rak alat, batu asah
        b.Cyl(Sl.Wood, new Vector3(5.2f, 0f, 0.6f), 0.4f, 0.75f, 8, 0.38f, true);
        b.Box(Sl.Metal, new Vector3(5.2f, 0.92f, 0.6f), new Vector3(0.9f, 0.2f, 0.3f));
        b.Box(Sl.Metal, new Vector3(5.75f, 0.9f, 0.6f), new Vector3(0.3f, 0.12f, 0.16f));
        V2Kit.Barrel(b, new Vector3(7.3f, 0f, -1.2f));
        b.Box(Sl.Wood, new Vector3(7.5f, 1.2f, 2.2f), new Vector3(0.12f, 1.6f, 1.6f));
        for (int k = 0; k < 4; k++) b.Beam(Sl.Metal, new Vector3(7.4f, 1.8f - 0.25f * (k % 2), 1.7f + k * 0.34f), new Vector3(7.2f, 1.1f, 1.7f + k * 0.34f), 0.06f, 0.05f);
        b.Cyl(Sl.DarkStone, new Vector3(3.8f, 0.45f, 3.6f), 0.4f, 0.14f, 10, 0.4f, true);
        b.Box(Sl.Wood, new Vector3(3.8f, 0.22f, 3.6f), new Vector3(0.7f, 0.44f, 0.12f));
        return 10.4f;
    }

    // ------------------------------------------------------------------ Barak penjaga (32 x 20, ~13)
    public static float Barracks(GeoBag b)
    {
        const float P = 0.5f;
        float hx = 14.6f, z0 = -9.5f, z1 = 9.5f, wt = P + 8.6f;
        b.Box(Sl.DarkStone, new Vector3(0f, (P - 0.7f) * 0.5f, 0f), new Vector3(hx * 2f + 2.4f, P + 0.7f, 20.4f));
        StoneBlock(b, -hx, hx, z0, z1, P, wt, Sl.Stone);
        // parapet berbenteng + atap datar tersembunyi
        b.Box(Sl.DarkStone, new Vector3(0f, wt + 0.15f, 0f), new Vector3(hx * 2f - 0.2f, 0.3f, 18.8f));
        b.Crenellate(Sl.Stone, new Vector3(-hx, wt, z1 - 0.1f), new Vector3(hx, wt, z1 - 0.1f), 1.2f, 0.6f, 1.3f, 1.0f);
        b.Crenellate(Sl.Stone, new Vector3(-hx, wt, z0 + 0.1f), new Vector3(hx, wt, z0 + 0.1f), 1.2f, 0.6f, 1.3f, 1.0f);
        b.Crenellate(Sl.Stone, new Vector3(-hx + 0.1f, wt, z0), new Vector3(-hx + 0.1f, wt, z1), 1.2f, 0.6f, 1.3f, 1.0f);
        b.Crenellate(Sl.Stone, new Vector3(hx - 0.1f, wt, z0), new Vector3(hx - 0.1f, wt, z1), 1.2f, 0.6f, 1.3f, 1.0f);
        // dua gubuk atap pelana di tengah (ruang jaga & dapur)
        b.Push(new Vector3(0f, wt + 0.3f, -2f), 0f);
        TileRoof(b, Sl.RoofB, Sl.DarkStone, Sl.Wood, Sl.Stone, new Vector3(0f, 0f, 0f), 18f, 9f, 3.2f, 0.3f, 0.3f, 7, 0.5f, true);
        b.Pop();
        // gerbang tengah: bukaan runcing, terali, obor, spanduk
        b.Push(new Vector3(0f, 0f, z1), 0f);
        b.Box(Sl.Stone, new Vector3(0f, P + 4.7f, 0.5f), new Vector3(8.4f, 9.4f, 1.1f));
        PointedPortal(b, 0f, 3.6f, 3.4f, 3, false);
        for (int k = -3; k <= 3; k++) b.Box(Sl.Metal, new Vector3(k * 0.42f, P + 2.2f, 0.28f), new Vector3(0.07f, 4.4f, 0.07f));
        for (int k = 0; k < 4; k++) b.Box(Sl.Metal, new Vector3(0f, P + 0.8f + k * 1.0f, 0.28f), new Vector3(3.2f, 0.06f, 0.07f));
        Banner(b, new Vector3(-3.4f, wt - 0.8f, 1.1f), 1.5f, 5.0f, Sl.CoatA, Sl.Gold);
        Banner(b, new Vector3(3.4f, wt - 0.8f, 1.1f), 1.5f, 5.0f, Sl.CoatA, Sl.Gold);
        b.Pop();
        // jendela panah & jendela atas
        for (int k = -4; k <= 4; k++)
        {
            if (Mathf.Abs(k) < 2) continue;
            b.Push(new Vector3(k * 3.0f, 0f, z1), 0f);
            b.Box(Sl.Dark, new Vector3(0f, P + 5.2f, 0.03f), new Vector3(0.26f, 1.3f, 0.08f));
            b.Box(Sl.DarkStone, new Vector3(0f, P + 5.95f, 0.05f), new Vector3(0.5f, 0.16f, 0.14f));
            Win(b, 0f, P + 2.4f, 0.9f, 1.4f, 3 + k, 2 | 1);
            b.Pop();
        }
        // menara bulat sudut
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f }) RoundTower(b, sx * (hx + 0.2f), sz * (z1 + 0.2f), 3.3f, P, wt + 4.4f, 6.8f, Sl.Stone, Sl.RoofB, 13 + (int)(sx * 3 + sz), 4);
        return wt + 4.4f + 6.8f + 1.5f;
    }

    // ------------------------------------------------------------------ Istal kota (30 x 18, ~10)
    public static float Stables(GeoBag b)
    {
        float hx = 14.4f, z0 = -8.6f, z1 = 8.6f, P = 0.45f;
        b.Box(Sl.Stone, new Vector3(0f, (P - 0.7f) * 0.5f, 0f), new Vector3(hx * 2f + 0.3f, P + 0.7f, 17.5f));
        // dinding bawah batu rendah + papan di atas
        float wb = P + 1.3f, wt = P + 4.6f;
        b.Box(Sl.Stone, new Vector3(0f, (P + wb) * 0.5f, 0f), new Vector3(hx * 2f, wb - P, 17.2f));
        b.Box(Sl.WoodLight, new Vector3(0f, (wb + wt) * 0.5f, 0f), new Vector3(hx * 2f - 0.1f, wt - wb, 17.1f));
        for (int k = 0; k <= 30; k++)
        {
            float x = -hx + k * 2f * hx / 30f;
            b.Box(Sl.Wood, new Vector3(x, (wb + wt) * 0.5f, z1 + 0.03f), new Vector3(0.08f, wt - wb, 0.05f));
            b.Box(Sl.Wood, new Vector3(x, (wb + wt) * 0.5f, z0 - 0.03f), new Vector3(0.08f, wt - wb, 0.05f));
        }
        b.Box(Sl.Wood, new Vector3(0f, wt - 0.1f, z1 + 0.05f), new Vector3(hx * 2f, 0.2f, 0.12f));
        // pintu istal setengah: 5 bilik di depan
        for (int k = 0; k < 5; k++)
        {
            float x = -10.4f + k * 5.2f;
            b.Push(new Vector3(x, 0f, z1), 0f);
            b.Box(Sl.Dark, new Vector3(0f, wb + 0.9f, 0.01f), new Vector3(1.9f, 2.6f, 0.06f));
            b.Box(Sl.Wood, new Vector3(0f, wb + 0.2f, 0.12f), new Vector3(2.0f, 1.2f, 0.12f));
            b.Box(Sl.Wood, new Vector3(-1.2f, wb + 1.2f, 0.1f), new Vector3(0.16f, 3.0f, 0.2f));
            b.Box(Sl.Wood, new Vector3(1.2f, wb + 1.2f, 0.1f), new Vector3(0.16f, 3.0f, 0.2f));
            b.Box(Sl.Wood, new Vector3(0f, wb + 2.65f, 0.1f), new Vector3(2.6f, 0.2f, 0.2f));
            b.Box(Sl.Cloth, new Vector3(0f, wb + 2.2f, 0.03f), new Vector3(0.7f, 0.3f, 0.04f));
            b.Pop();
        }
        // pintu lumbung jerami di tengah atas (kerek)
        TileRoof(b, Sl.Roof, Sl.RoofB, Sl.Wood, Sl.WoodLight, new Vector3(0f, wt, 0f), hx * 2f, 17.1f, 3.9f, 0.65f, 0.45f, 17, 0.5f, true);
        // dormer lumbung + balok kerek
        b.Push(new Vector3(0f, 0f, z1 + 0.1f), 0f);
        b.Box(Sl.Dark, new Vector3(0f, wt + 1.1f, 0.2f), new Vector3(1.8f, 1.7f, 0.1f));
        b.Box(Sl.Wood, new Vector3(0f, wt + 2.1f, 0.24f), new Vector3(2.2f, 0.2f, 0.24f));
        b.Beam(Sl.Wood, new Vector3(0f, wt + 2.0f, 0.2f), new Vector3(0f, wt + 2.0f, 1.8f), 0.18f, 0.18f);
        b.Pop();
        // palang tambat kuda & palung
        for (int sd = -1; sd <= 1; sd += 2)
        {
            b.Cyl(Sl.Wood, new Vector3(sd * 6f, 0f, z1 + 2.6f), 0.12f, 1.3f, 6, 0.1f, true);
        }
        b.Beam(Sl.Wood, new Vector3(-6f, 1.2f, z1 + 2.6f), new Vector3(6f, 1.2f, z1 + 2.6f), 0.14f, 0.14f);
        b.Push(new Vector3(0f, 0f, z1 + 3.6f), 0f); V2Kit.Trough(b); b.Pop();
        return wt + 3.9f + 0.8f;
    }
}
