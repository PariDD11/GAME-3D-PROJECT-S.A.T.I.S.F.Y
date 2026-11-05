using UnityEngine;
using HunterGuild;

/// <summary>
/// Struktur desa bergaya anime-isekai abad pertengahan: gerbang kayu beratap, menara jaga, gereja, pagar glondongan runcing.
/// Semua fungsi hanya menambahkan mesh ke GeoBag (tanpa collider), sehingga logika scene tidak berubah.
/// </summary>
public static class VillageKit
{
    static Vector3 P(float x, float y, float z) { return new Vector3(x, y, z); }

    // ------------------------------------------------------------------ gerbang
    /// <summary>Gerbang kayu beratap. c = tengah jalur, az = arah keluar (derajat), halfOpen = setengah lebar jalur.</summary>
    public static void TimberGate(GeoBag b, Vector3 c, float az, float halfOpen, float gyL = 0f, float gyR = 0f)
    {
        b.Push(c, az);                       // lokal: x = melintang, z = keluar
        float px = halfOpen + 0.9f;
        for (int s = -1; s <= 1; s += 2)
        {
            float x = s * px;
            // alas batu bertingkat
            // alas batu diperpanjang sampai tanah/dasar sungai (tidak melayang di lereng atau tepi air)
            float yb = Mathf.Min(0f, s < 0 ? gyL : gyR) - 0.6f;
            b.Box(Sl.Stone, P(x, (0.9f + yb) / 2f, 0f), P(2.3f, 0.9f - yb, 2.3f).Size());
            if (yb < -1.2f) b.Box(Sl.DarkStone, P(x, 0.1f, 0f), P(2.6f, 0.22f, 2.6f).Size());
            b.Box(Sl.DarkStone, P(x, 1.0f, 0f), P(1.9f, 0.25f, 1.9f).Size());
            // tiang kayu bundar + cincin besi
            b.Cyl(Sl.Wood, P(x, 1.1f, 0f), 0.52f, 5.6f, 10, 0.46f, true, false);
            for (int k = 0; k < 3; k++) b.Cyl(Sl.Metal, P(x, 1.9f + k * 1.7f, 0f), 0.56f, 0.14f, 10, 0.56f, true);
            // penyangga lengkung (dua balok miring)
            Vector3 top = P(x - s * 0.3f, 5.2f, 0f);
            Vector3 mid = P(s * (halfOpen - 0.5f), 6.1f, 0f);
            b.Beam(Sl.WoodLight, top - P(0f, 1.3f, 0f), P(s * (halfOpen - 1.6f), 5.8f, 0f), 0.3f, 0.3f);
            b.Beam(Sl.Wood, top - P(0f, 2.3f, 0f), P(s * (halfOpen - 2.8f), 5.9f, 0f), 0.22f, 0.22f);
            // lentera gantung dan semak bunga
            AnimeKit.HangLantern(b, P(x - s * 0.2f, 4.7f, 0.55f), 0.3f);
            if ((s < 0 ? gyL : gyR) > -0.6f) AnimeKit.FlowerBush(b, P(x + s * 1.6f, 0f, 0.4f), 0.75f, s > 0 ? Sl.Blossom : Sl.Gold);
        }
        // palang utama ganda
        float L = px + 1.2f;
        b.Beam(Sl.Wood, P(-L, 6.35f, 0f), P(L, 6.35f, 0f), 0.62f, 0.55f);
        b.Beam(Sl.WoodLight, P(-L, 5.55f, 0f), P(L, 5.55f, 0f), 0.34f, 0.32f);
        // papan nama gantung
        b.Box(Sl.WoodLight, P(0f, 5.0f, 0f), P(halfOpen * 1.5f, 1.5f, 0.14f).Size());
        b.Box(Sl.Gold, P(0f, 5.78f, 0f), P(halfOpen * 1.5f + 0.2f, 0.08f, 0.2f).Size());
        b.Box(Sl.Gold, P(0f, 4.22f, 0f), P(halfOpen * 1.5f + 0.2f, 0.08f, 0.2f).Size());
        // atap pelana (punggung sepanjang melintang)
        b.Gable(Sl.Roof, Sl.PlasterB, Sl.Wood, P(0f, 6.65f, 0f), (L + 1.1f) * 2f, 2.8f, 1.7f, 0.55f, true);
        // pucuk dan pita
        b.Ball(Sl.Gold, P(-L - 0.9f, 8.0f, 0f), 0.26f, 4, 8);
        b.Ball(Sl.Gold, P(L + 0.9f, 8.0f, 0f), 0.26f, 4, 8);
        AnimeKit.PennantTop(b, P(0f, 8.3f, 0f), Sl.Cloth);
        // spanduk guild di dua sisi
        foreach (float sz in new[] { 0.55f, -0.55f })
            foreach (float sx in new[] { -1f, 1f })
            {
                b.Push(P(sx * (halfOpen * 0.55f), 0f, sz), sz < 0 ? 180f : 0f);
                BuildKit.Banner(b, P(0f, 5.3f, 0f), 0.9f, 2.2f, Sl.Cloth, true);
                b.Pop();
            }
        b.Pop();
    }

    // ------------------------------------------------------------------ menara jaga
    public static void Watchtower(GeoBag b, Vector3 c)
    {
        // kaki batu + tiang miring
        b.Cyl(Sl.Stone, c, 3.4f, 0.6f, 12, 3.2f, true);
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                Vector3 p = c + P(sx * 1.75f, 0f, sz * 1.75f);
                b.Cyl(Sl.Stone, p, 0.62f, 0.9f, 8, 0.52f, true);
                b.Beam(Sl.Wood, p + P(0f, 0.8f, 0f), c + P(sx * 1.55f, 7.0f, sz * 1.55f), 0.46f, 0.46f);
            }
        // palang silang
        for (int lv = 0; lv < 2; lv++)
        {
            float y = 2.3f + lv * 2.6f, hh = 1.7f - lv * 0.12f;
            b.Beam(Sl.Wood, c + P(-hh, y, -hh), c + P(hh, y, -hh), 0.22f, 0.22f);
            b.Beam(Sl.Wood, c + P(-hh, y, hh), c + P(hh, y, hh), 0.22f, 0.22f);
            b.Beam(Sl.Wood, c + P(-hh, y, -hh), c + P(-hh, y, hh), 0.22f, 0.22f);
            b.Beam(Sl.Wood, c + P(hh, y, -hh), c + P(hh, y, hh), 0.22f, 0.22f);
        }
        b.Beam(Sl.WoodLight, c + P(-1.7f, 0.9f, -1.7f), c + P(1.55f, 4.9f, -1.6f), 0.15f, 0.15f);
        b.Beam(Sl.WoodLight, c + P(1.7f, 0.9f, -1.7f), c + P(-1.55f, 4.9f, -1.6f), 0.15f, 0.15f);
        // dek pengintai
        b.Box(Sl.WoodLight, c + P(0f, 7.0f, 0f), P(5.2f, 0.3f, 5.2f).Size());
        b.Box(Sl.Wood, c + P(0f, 6.78f, 0f), P(4.4f, 0.18f, 4.4f).Size());
        // pagar dek: tiang dan palang
        for (int i = -2; i <= 2; i++)
        {
            float x = i * 1.1f;
            foreach (float z in new[] { -2.4f, 2.4f })
            {
                b.Cyl(Sl.Wood, c + P(x, 7.15f, z), 0.07f, 0.95f, 5, 0.06f, true);
                b.Cyl(Sl.Wood, c + P(z, 7.15f, x), 0.07f, 0.95f, 5, 0.06f, true);
            }
        }
        b.Beam(Sl.Wood, c + P(-2.4f, 8.0f, -2.4f), c + P(2.4f, 8.0f, -2.4f), 0.14f, 0.14f);
        b.Beam(Sl.Wood, c + P(-2.4f, 8.0f, 2.4f), c + P(2.4f, 8.0f, 2.4f), 0.14f, 0.14f);
        b.Beam(Sl.Wood, c + P(-2.4f, 8.0f, -2.4f), c + P(-2.4f, 8.0f, 2.4f), 0.14f, 0.14f);
        b.Beam(Sl.Wood, c + P(2.4f, 8.0f, -2.4f), c + P(2.4f, 8.0f, 2.4f), 0.14f, 0.14f);
        // tiang atap + papan setengah dinding
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                b.Cyl(Sl.Wood, c + P(sx * 2.0f, 7.15f, sz * 2.0f), 0.13f, 2.9f, 6, 0.12f, true);
        b.Box(Sl.PlasterB, c + P(0f, 7.75f, -2.0f), P(3.9f, 1.0f, 0.16f).Size());
        b.Box(Sl.PlasterB, c + P(-2.0f, 7.75f, 0f), P(0.16f, 1.0f, 3.9f).Size());
        b.Box(Sl.PlasterB, c + P(2.0f, 7.75f, 0f), P(0.16f, 1.0f, 3.9f).Size());
        b.Box(Sl.Wood, c + P(0f, 9.95f, 0f), P(4.4f, 0.2f, 4.4f).Size());
        // atap jungkit
        b.Hip(Sl.Roof, Sl.Wood, c + P(0f, 10.0f, 0f), 5.4f, 5.4f, 2.1f, 0.6f);
        b.Ball(Sl.Gold, c + P(0f, 12.2f, 0f), 0.24f, 4, 8);
        AnimeKit.PennantTop(b, c + P(0f, 12.3f, 0f), Sl.Cloth);
        // lentera, genta, tangga
        AnimeKit.HangLantern(b, c + P(0f, 9.7f, 2.0f), 0.4f);
        AnimeKit.HangLantern(b, c + P(2.0f, 9.7f, 0f), 0.3f);
        b.Stairs(Sl.WoodLight, c + P(0f, 0.6f, 2.2f), 1.2f, 6, 0.2f, 0.3f, 0f);
        AnimeKit.FlowerBush(b, c + P(2.8f, 0f, 2.6f), 0.7f, Sl.Blossom);
        AnimeKit.FlowerBush(b, c + P(-2.8f, 0f, 2.6f), 0.7f, Sl.Gold);
    }

    // ------------------------------------------------------------------ pagar glondongan
    /// <summary>Satu tiang pagar runcing. p = dasar, h = tinggi.</summary>
    public static void Log(GeoBag b, Vector3 p, float h, int i)
    {
        b.Cyl(Sl.Wood, p, 0.34f, h, 7, 0.3f, false, false);
        b.Spire(Sl.WoodLight, p + P(0f, h, 0f), 0.31f, 0.85f, 7, 0.1f);
        if (i % 4 == 0) b.Cyl(Sl.Cloth, p + P(0f, h * 0.62f, 0f), 0.345f, 0.12f, 7, 0.345f, false);   // ikat tali berwarna
    }

    /// <summary>Alas batu, tiang lentera dan bendera di antara dua titik (sisi dalam).</summary>
    public static void PalisadeSegment(GeoBag b, Vector3 s0, Vector3 s1, int idx)
    {
        b.Beam(Sl.Stone, s0 + P(0f, 0.3f, 0f), s1 + P(0f, 0.3f, 0f), 1.0f, 0.6f);
        b.Beam(Sl.DarkStone, s0 + P(0f, 0.62f, 0f), s1 + P(0f, 0.62f, 0f), 0.7f, 0.1f);
        Vector3 m = (s0 + s1) / 2f;
        // tiang lentera
        b.Cyl(Sl.Wood, m, 0.2f, 4.0f, 6, 0.16f, true);
        b.Ball(Sl.Gold, m + P(0f, 4.05f, 0f), 0.13f, 3, 6);
        b.Ball(Sl.Glow, m + P(0f, 3.5f, 0f), 0.24f, 4, 8, 1.2f);
        if (idx % 2 == 0) AnimeKit.FlowerBush(b, m + (s0 - s1).normalized * 1.2f, 0.45f, Sl.Blossom);
    }

    // ------------------------------------------------------------------ gereja
    /// <summary>Gereja desa dalam koordinat lokal: depan +Z, menara di z=11.2. Cakupan tapak tidak berubah (10 x 20 + menara 6.2).</summary>
    public static void Church(GeoBag b)
    {
        // alas batu bertingkat
        b.Box(Sl.DarkStone, P(0f, 0.2f, 0f), P(11.2f, 0.4f, 21.2f).Size());
        b.Box(Sl.Stone, P(0f, 0.6f, 0f), P(10.6f, 0.5f, 20.6f).Size());
        // lambung
        b.Box(Sl.Plaster, P(0f, 3.85f, 0f), P(10f, 6.0f, 20f).Size());
        // balok-balok setengah kayu
        for (int s = -1; s <= 1; s += 2)
        {
            float x = s * 5.03f;
            b.Beam(Sl.Wood, P(x, 0.9f, -9.9f), P(x, 6.8f, -9.9f), 0.3f, 0.3f);
            b.Beam(Sl.Wood, P(x, 6.85f, -9.9f), P(x, 6.85f, 9.9f), 0.28f, 0.28f);
            b.Beam(Sl.Wood, P(x, 0.95f, -9.9f), P(x, 0.95f, 9.9f), 0.26f, 0.26f);
            // penopang batu dan jendela kaca patri lengkung
            for (int i = 0; i < 5; i++)
            {
                float z = -8f + i * 4f;
                b.Box(Sl.Stone, P(x + s * 0.28f, 3.3f, z), P(0.9f, 5.4f, 0.9f).Size());
                b.Box(Sl.DarkStone, P(x + s * 0.28f, 6.2f, z), P(1.1f, 0.35f, 1.1f).Size());
                b.Spire(Sl.Roof, P(x + s * 0.28f, 6.4f, z), 0.55f, 0.9f, 4, 0.2f);
                if (i < 4)
                {
                    b.Push(P(x, 0f, z + 2f), s > 0 ? 90f : -90f);
                    FantasyKit.ArchWin(b, P(0f, 1.6f, 0f), 1.5f, 3.4f, false, false);
                    b.Pop();
                }
            }
        }
        // atap utama
        b.Gable(Sl.Roof, Sl.Plaster, Sl.Wood, P(0f, 6.85f, 0f), 10f, 20f, 4.6f, 0.9f, false);
        // abside
        b.Cyl(Sl.Stone, P(0f, 0.6f, -10f), 3.9f, 0.5f, 14, 3.9f, true);
        b.Cyl(Sl.Plaster, P(0f, 1.0f, -10f), 3.8f, 5.8f, 14, 3.8f, true);
        FantasyKit.FlareRoof(b, Sl.Roof, P(0f, 6.7f, -10f), 4.5f, 4.0f, 14, Sl.Gold);
        for (int i = -1; i <= 1; i++)
        {
            float a = i * 0.7f;
            b.Push(P(Mathf.Sin(a) * 3.82f, 0f, -10f - Mathf.Cos(a) * 3.82f), 180f - a * Mathf.Rad2Deg);
            FantasyKit.ArchWin(b, P(0f, 1.8f, 0f), 1.0f, 2.8f, false, false);
            b.Pop();
        }
        // menara lonceng
        b.Box(Sl.DarkStone, P(0f, 0.6f, 11.2f), P(7.2f, 1.2f, 7.2f).Size());
        b.Box(Sl.Stone, P(0f, 8.8f, 11.2f), P(6.2f, 15.6f, 6.2f).Size());
        for (int k = 0; k < 3; k++) b.Box(Sl.DarkStone, P(0f, 4.0f + k * 4.4f, 11.2f), P(6.6f, 0.38f, 6.6f).Size());
        // sudut batu (quoin) menara
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { 8.1f, 14.3f })
                for (int q = 0; q < 7; q++)
                    b.Box(Sl.PlasterB, P(sx * 3.1f, 1.8f + q * 2.0f, sz), P(0.55f, 0.9f, 0.55f).Size());
        // ruang lonceng terbuka
        b.Box(Sl.DarkStone, P(0f, 16.9f, 11.2f), P(7.0f, 0.5f, 7.0f).Size());
        foreach (float sx in new[] { -2.7f, 2.7f })
            foreach (float sz in new[] { 8.5f, 13.9f })
                b.Cyl(Sl.Stone, P(sx, 17.1f, sz), 0.42f, 3.4f, 8, 0.38f, true);
        foreach (float yaw in new[] { 0f, 90f, 180f, 270f })
        {
            b.Push(P(0f, 17.1f, 11.2f), yaw);
            b.Box(Sl.Stone, P(0f, 3.2f, 2.7f), P(5.0f, 0.5f, 0.55f).Size());
            for (int i = 0; i < 8; i++)
            {
                float a = Mathf.PI * i / 7f;
                b.Ball(Sl.Stone, P(Mathf.Cos(a) * 2.25f, 2.9f + Mathf.Sin(a) * 0.25f, 2.7f), 0.0f + 0.18f, 3, 5);
            }
            b.Pop();
        }
        b.Ball(Sl.Gold, P(0f, 18.4f, 11.2f), 0.85f, 4, 10, 1.1f);                // lonceng
        b.Beam(Sl.Metal, P(-2.7f, 19.0f, 11.2f), P(2.7f, 19.0f, 11.2f), 0.18f, 0.18f);
        // atap menara: spire terompet delapan sisi dengan kisi
        b.Cyl(Sl.RoofB, P(0f, 20.4f, 11.2f), 4.6f, 0.6f, 8, 4.0f, true);
        b.Spire(Sl.Roof, P(0f, 21.0f, 11.2f), 3.9f, 9.0f, 8, 0.45f);
        foreach (float yaw in new[] { 0f, 90f, 180f, 270f })
        {
            b.Push(P(0f, 21.0f, 11.2f), yaw);
            b.Gable(Sl.Roof, Sl.Plaster, Sl.Wood, P(0f, 1.3f, 3.0f), 1.9f, 1.6f, 1.3f, 0.2f, false);
            b.Pop();
        }
        // salib emas dan bola
        b.Ball(Sl.Gold, P(0f, 30.2f, 11.2f), 0.4f, 4, 8);
        b.Box(Sl.Gold, P(0f, 31.4f, 11.2f), P(0.26f, 2.4f, 0.26f).Size());
        b.Box(Sl.Gold, P(0f, 31.9f, 11.2f), P(1.4f, 0.26f, 0.26f).Size());
        // pintu lengkung batu + mawar kaca
        FantasyKit.ArchDoor(b, P(0f, 0.9f, 14.3f), 2.8f, 4.2f, 3);
        AnimeKit.RoundWindow(b, P(0f, 10.6f, 14.35f), 1.15f);
        FantasyKit.ArchWin(b, P(-1.9f, 11.9f, 14.3f), 0.8f, 1.9f, false, false);
        FantasyKit.ArchWin(b, P(1.9f, 11.9f, 14.3f), 0.8f, 1.9f, false, false);
        // papan nama
        b.Box(Sl.WoodLight, P(0f, 6.3f, 14.38f), P(4.8f, 1.5f, 0.16f).Size());
        b.Box(Sl.Gold, P(0f, 7.1f, 14.4f), P(5.0f, 0.1f, 0.14f).Size());
        b.Box(Sl.Gold, P(0f, 5.5f, 14.4f), P(5.0f, 0.1f, 0.14f).Size());
        // tangga depan
        b.Stairs(Sl.Stone, P(0f, 0.6f, 14.3f), 5.2f, 3, 0.2f, 0.7f, 0f);
        // sayap kecil (kapel) di sisi barat dengan atap sendiri
        b.Box(Sl.Plaster, P(-6.6f, 2.2f, 4.5f), P(3.2f, 3.8f, 5.0f).Size());
        b.Gable(Sl.RoofB, Sl.Plaster, Sl.Wood, P(-6.6f, 4.1f, 4.5f), 3.2f, 5.0f, 1.8f, 0.45f, false);
        b.Push(P(-8.2f, 0f, 4.5f), -90f);
        FantasyKit.ArchWin(b, P(0f, 1.0f, 0f), 1.0f, 2.2f, true, true);
        b.Pop();
        // tanaman: rambat, semak bunga, lentera
        AnimeKit.Ivy(b, P(-4.9f, 0.9f, 12f), 5.5f, 2.2f, 3);
        AnimeKit.Ivy(b, P(4.9f, 0.9f, -4f), 5.0f, 2.0f, 8);
        foreach (float sx in new[] { -4.6f, 4.6f })
            foreach (float sz in new[] { 15.6f, 6f, -2f })
                AnimeKit.FlowerBush(b, P(sx, 0.4f, sz), 0.85f, sz > 10f ? Sl.Blossom : Sl.Gold);
        AnimeKit.HangLantern(b, P(-2.2f, 4.0f, 14.3f), 0.5f);
        AnimeKit.HangLantern(b, P(2.2f, 4.0f, 14.3f), 0.5f);
        AnimeKit.Bunting(b, P(-3f, 8.0f, 14.5f), P(3f, 8.0f, 14.5f), 0.5f, 0, 5);
    }
}

static class VecSizeExt
{
    /// <summary>Penanda keterbacaan: P(...) dipakai sebagai ukuran.</summary>
    public static Vector3 Size(this Vector3 v) { return v; }
}
