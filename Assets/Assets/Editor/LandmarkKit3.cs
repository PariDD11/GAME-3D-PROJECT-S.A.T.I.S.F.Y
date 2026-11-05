using UnityEngine;

/// <summary>Landmark baru gaya isekai-fantasi (versi 2). Ukuran jejak sama dengan versi lama.</summary>
public static partial class LandmarkKit
{
    // ---- bahan bantu dinding batu berpilaster
    static void StoneWallFront(GeoBag b, float w, float y0, float h, float zFace, int nPil, int slot)
    {
        for (int i = 0; i <= nPil; i++)
        {
            float x = -w / 2f + i * w / nPil;
            b.BoxB(Sl.DarkStone, P(x, y0, zFace + 0.18f), new Vector3(0.9f, h, 0.7f));
            b.Box(Sl.Stone, P(x, y0 + h + 0.12f, zFace + 0.18f), new Vector3(1.15f, 0.26f, 0.95f));
        }
        // cornice
        b.Box(Sl.DarkStone, P(0f, y0 + h - 0.15f, zFace + 0.12f), new Vector3(w + 0.4f, 0.3f, 0.5f));
        // lis batu horizontal
        b.Box(Sl.DarkStone, P(0f, y0 + 0.35f, zFace + 0.05f), new Vector3(w + 0.2f, 0.18f, 0.2f));
    }

    public static void Guild(GeoBag b)
    {
        float w = GuildW, d = GuildD, g = 5.8f, u = 4.8f, jet = 0.9f, y0 = 1.3f, y1 = y0 + g, yT = y1 + u, rise = 10f;
        // ---- badan: pondasi, lantai batu, lantai atas kayu-plester menjorok
        FantasyKit.Foundation(b, 0f, 0f, w, d, y0, 11);
        FantasyKit.Wall(b, Sl.Stone, P(0, y0, 0), new Vector3(w, g, d), 1);
        FantasyKit.Wall(b, Sl.Plaster, P(0, y1, jet / 2f), new Vector3(w, u, d + jet), 0);
        FantasyKit.CornerPosts(b, 0f, jet / 2f, w, d + jet, y1, u, 3);
        b.Box(Sl.DarkStone, P(0, y1 - 0.15f, d / 2f + 0.1f), new Vector3(w + 0.3f, 0.5f, 0.3f));
        for (int i = 0; i < 14; i++)
        {
            float x = -w / 2f + 1f + i * (w - 2f) / 13f;
            b.Beam(Sl.Wood, P(x, y1 - 0.8f, d / 2f), P(x, y1, d / 2f + jet), 0.25f, 0.25f);
            b.Ball(Sl.Wood, P(x, y1 - 0.75f, d / 2f + 0.03f), 0.18f, 3, 5);
        }
        b.Push(P(0, y1, d / 2f + jet), 0f); FantasyKit.Frame(b, w, 0f, u, 5, 1); b.Pop();
        b.Push(P(0, y1, -d / 2f), 180f); FantasyKit.Frame(b, w, 0f, u, 6, 1); b.Pop();
        b.Push(P(w / 2f, y1, jet / 2f), 90f); FantasyKit.Frame(b, d + jet, 0f, u, 7, 1); b.Pop();
        b.Push(P(-w / 2f, y1, jet / 2f), -90f); FantasyKit.Frame(b, d + jet, 0f, u, 8, 1); b.Pop();
        StoneWallFront(b, w, y0, g, d / 2f, 8, Sl.Stone);

        // ---- jendela lantai bawah (lengkung tinggi) & atas
        foreach (float x in new[] { -17.5f, -12.5f, -7.5f, 7.5f, 12.5f, 17.5f })
            BuildKit.ArchWindow(b, P(x - (x > 0 ? 0.0f : 0f) + (x < 0 ? 0f : 0f), y0 + 0.9f, d / 2f + 0.02f), 1.7f, 3.9f, Sl.Marble);
        for (int i = 0; i < 4; i++)
        {
            float z = -7.5f + i * 5f;
            b.Push(P(w / 2f, y0 + 0.9f, z), 90f); BuildKit.ArchWindow(b, Vector3.zero, 1.8f, 3.6f, Sl.Marble); b.Pop();
            b.Push(P(-w / 2f, y0 + 0.9f, z), -90f); BuildKit.ArchWindow(b, Vector3.zero, 1.8f, 3.6f, Sl.Marble); b.Pop();
        }
        for (int i = 0; i < 6; i++)
        {
            float x = -17f + i * 6.8f;
            BuildKit.Window(b, P(x, y1 + u * 0.55f, d / 2f + jet + 0.02f), 1.3f, 1.9f, true, true);
            b.Push(P(x, y1 + u * 0.55f, -d / 2f), 180f); BuildKit.Window(b, Vector3.zero, 1.3f, 1.8f, true, false); b.Pop();
        }
        // ---- portal besar + portico
        BuildKit.DoorFront(b, P(0, y0, d / 2f), 3.8f, 4.9f, true);
        Portico(b, 0f, d / 2f, 13.5f, GuildPorch - 1.4f, y0, 6.4f, 4, true);

        // ---- atap utama + sayap melintang di depan (kiri & kanan) + lukarne
        b.Gable(Sl.Roof, Sl.Plaster, Sl.Wood, P(0, yT, jet / 2f), w, d + jet, rise, 1.1f, true);
        foreach (float sx in new[] { -1f, 1f })
        {
            float cx = sx * 12.5f, cw = 8.6f, cd = 3.2f;
            b.BoxB(Sl.Plaster, P(cx, y1, d / 2f + jet + cd / 2f - 0.05f), new Vector3(cw, u, cd + 0.1f));
            b.Push(P(cx, y1, d / 2f + jet + cd), 0f); FantasyKit.Frame(b, cw, 0f, u, 9, 1); b.Pop();
            b.Gable(Sl.Roof, Sl.Plaster, Sl.Wood, P(cx, yT, d / 2f + jet + cd / 2f - 0.05f), cw, cd + 0.1f, 6.2f, 1.0f, false);
            BuildKit.Window(b, P(cx, y1 + 2.4f, d / 2f + jet + cd + 0.02f), 1.5f, 2.0f, true, true);
            AnimeKit.RoundWindow(b, P(cx, yT + 2.4f, d / 2f + jet + cd + 0.03f), 0.7f);
            AnimeKit.PennantTop(b, P(cx, yT + 6.2f, d / 2f + jet + cd / 2f), sx < 0f ? Sl.Cloth : Sl.ClothB);
        }
        Dormers(b, yT, jet / 2f, d + jet, rise, Sl.RoofB, -4f, 4f, 0f);
        foreach (float cx in new[] { -16f, 16f })
            FantasyKit.RoundChimney(b, P(cx, yT - 1f, -1.5f), 0.9f, rise + 1.2f);

        // ---- menara bulat di sudut depan (jendela lengkung)
        foreach (float sx in new[] { -1f, 1f })
            BuildKit.RoundTower(b, P(sx * (w / 2f - 1.6f), 0f, d / 2f - 1.6f), 3.5f, 16.5f, Sl.Stone, Sl.DarkStone, Sl.RoofB, 11f, false, true, Sl.Cloth);
        // ---- menara jam di belakang tengah
        float tz = -d / 2f + 3.5f;
        FantasyKit.Wall(b, Sl.Stone, P(0, 0, tz), new Vector3(8.5f, 24f, 8.5f), 1);
        b.BoxB(Sl.DarkStone, P(0, 22f, tz), new Vector3(9.4f, 0.8f, 9.4f));
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                b.Cone(Sl.RoofB, P(sx * 4.2f, 22.8f, tz + sz * 4.2f), 0.9f, 3.4f, 8);
        b.Hip(Sl.RoofB, Sl.Wood, P(0, 24f, tz), 9.6f, 9.6f, 9.5f, 0.8f);
        BuildKit.Finial(b, P(0, 33.5f, tz), 3f, Sl.Gold);
        foreach (float zs in new[] { 1f, -1f })
        {
            b.Push(P(0, 18f, tz + zs * 4.3f), zs > 0f ? 0f : 180f);
            b.Box(Sl.Marble, P(0, 0, 0), new Vector3(4.6f, 4.6f, 0.25f));
            b.Box(Sl.Gold, P(0, 0, 0.14f), new Vector3(3.8f, 3.8f, 0.08f));
            b.Box(Sl.Marble, P(0, 0, 0.2f), new Vector3(3.4f, 3.4f, 0.08f));
            b.Box(Sl.Metal, P(0, 0.7f, 0.28f), new Vector3(0.14f, 1.5f, 0.06f));
            b.Box(Sl.Metal, P(0.6f, 0f, 0.28f), new Vector3(1.2f, 0.14f, 0.06f));
            b.Pop();
        }
        // ---- spanduk, papan gantung, hiasan
        foreach (float x in new[] { -20.8f, -10f, 10f, 20.8f })
            BuildKit.Banner(b, P(x, y0 + 5.6f, d / 2f + 0.5f), 1.7f, 4.6f, (x < 0f) ? Sl.Cloth : Sl.ClothB, true);
        b.Beam(Sl.Metal, P(w / 2f - 0.4f, y1 + 3.6f, d / 2f + jet), P(w / 2f - 0.4f, y1 + 3.6f, d / 2f + jet + 2.6f), 0.14f, 0.14f);
        b.Box(Sl.Wood, P(w / 2f - 0.4f, y1 + 2.3f, d / 2f + jet + 2.1f), new Vector3(0.18f, 2.2f, 2.2f));
        b.Box(Sl.Gold, P(w / 2f - 0.28f, y1 + 2.3f, d / 2f + jet + 2.1f), new Vector3(0.08f, 1.5f, 1.5f));
        AnimeKit.Bunting(b, P(-w / 2f + 0.6f, y1 + 0.8f, d / 2f + jet + 0.5f), P(w / 2f - 0.6f, y1 + 0.8f, d / 2f + jet + 0.5f), 1.3f, 16, 3);
        foreach (float fx in new[] { -15.5f, -11f, 11f, 15.5f })
            AnimeKit.FlowerBush(b, P(fx, 0f, d / 2f + 1.7f), 1.0f, fx < 0f ? Sl.Blossom : Sl.Gold);
        foreach (float fx in new[] { -5.4f, 5.4f })
            AnimeKit.HangLantern(b, P(fx, y0 + 4.4f, d / 2f + 0.1f), 1.0f);
        BuildKit.Barrel(b, P(-w / 2f + 2f, 0f, d / 2f + 1.2f));
        BuildKit.Barrel(b, P(-w / 2f + 3.1f, 0f, d / 2f + 1.5f));
        BuildKit.Crate(b, P(w / 2f - 2.2f, 0f, d / 2f + 1.3f), 1.1f, 15f);
        AnimeKit.Ivy(b, P(-w / 2f + 7.5f, y0, d / 2f + 0.5f), 5f, 1.6f, 5);
    }

    // ====================================================================== HALL umum (Kedai, Penginapan, Toko, Akademi, Barak)
    /// <summary>
    /// Bangunan besar bertingkat: pondasi, lantai bawah (batu atau plester), lantai atas menjorok berrangka kayu, atap melengkung,
    /// sayap melintang berbalkon, jendela lengkung, dormer, cerobong bulat, bunting. Mengembalikan y puncak atap utama.
    /// </summary>
    static float Hall(GeoBag b, float w, float d, int floors, float fh, float y0, bool stoneGround, int wings, float rise, int roofSlot, int roofSlot2, int seed, float doorW)
    {
        float jet = 0.7f;
        FantasyKit.Foundation(b, 0f, 0f, w, d, y0, seed);
        float y = y0;
        for (int i = 0; i < floors; i++)
        {
            float dd = d + (i > 0 ? jet : 0f), zc = i > 0 ? jet / 2f : 0f;
            int wall = (i == 0 && stoneGround) ? Sl.Stone : ((i == 0) ? Sl.PlasterB : Sl.Plaster);
            FantasyKit.Wall(b, wall, P(0f, y, zc), new Vector3(w, fh, dd), 0);
            FantasyKit.CornerPosts(b, 0f, zc, w, dd, y, fh, seed + i);
            if (i > 0)
            {
                int nb = Mathf.RoundToInt(w / 1.8f);
                for (int k = 0; k <= nb; k++)
                {
                    float x = -w / 2f + 0.6f + k * (w - 1.2f) / nb;
                    b.Beam(Sl.Wood, P(x, y - 0.7f, d / 2f), P(x, y, d / 2f + jet), 0.22f, 0.22f);
                    b.Ball(Sl.Wood, P(x, y - 0.65f, d / 2f + 0.03f), 0.16f, 3, 5);
                }
            }
            if (!(i == 0 && stoneGround))
            {
                b.Push(P(0f, y, zc + dd / 2f), 0f); FantasyKit.Frame(b, w, 0f, fh, seed + i, 1); b.Pop();
            }
            else StoneWallFront(b, w, y, fh, dd / 2f, Mathf.Max(3, Mathf.RoundToInt(w / 4.5f)), Sl.Stone);
            b.Push(P(0f, y, zc - dd / 2f), 180f); FantasyKit.Frame(b, w, 0f, fh, seed + 9 + i, 1); b.Pop();
            b.Push(P(w / 2f, y, zc), 90f); FantasyKit.Frame(b, dd, 0f, fh, seed + 3 + i, 1); b.Pop();
            b.Push(P(-w / 2f, y, zc), -90f); FantasyKit.Frame(b, dd, 0f, fh, seed + 5 + i, 1); b.Pop();
            y += fh;
        }
        float yT = y, dT = d + (floors > 1 ? jet : 0f), zT = floors > 1 ? jet / 2f : 0f;
        float fz = d / 2f, fzU = zT + dT / 2f;
        // pintu & jendela
        BuildKit.DoorFront(b, P(0f, y0, fz), doorW, doorW * 1.25f + 0.4f, true);
        int nw = Mathf.Max(2, Mathf.RoundToInt(w / 5.2f));
        for (int i = 0; i < floors; i++)
        {
            float yy = y0 + i * fh;
            float zf = i == 0 ? fz : fzU;
            for (int k = 0; k < nw * 2; k++)
            {
                float x = -w / 2f + (k + 0.5f) * w / (nw * 2f);
                if (i == 0 && Mathf.Abs(x) < doorW * 0.5f + 1.7f) continue;
                if (i == 0 && wings > 0 && Mathf.Abs(Mathf.Abs(x) - w * 0.3f) < 2.6f) continue;
                if (i > 0 && wings > 0 && Mathf.Abs(Mathf.Abs(x) - w * 0.3f) < 2.6f) continue;
                BuildKit.Window(b, P(x, yy + fh * 0.58f, zf + 0.02f), 1.3f, 1.9f, true, i == 0 || (k % 2 == 0));
            }
        }
        foreach (float sx in new[] { -1f, 1f })
            for (int i = 0; i < floors; i++)
                for (int k = 0; k < 2; k++)
                {
                    b.Push(P(sx * w / 2f, y0 + i * fh, (k - 0.5f) * d * 0.45f + (i > 0 ? jet / 2f : 0f)), sx * 90f);
                    BuildKit.Window(b, P(0f, fh * 0.58f, 0.02f), 1.2f, 1.8f, true, false);
                    b.Pop();
                }
        // detail kehidupan: talang, tambalan bata, tangga, pot gantung, jemuran, jalan batu
        foreach (float sx in new[] { -1f, 1f })
            FantasyKit.DrainPipe(b, P(sx * (w / 2f + 0.15f), yT - 0.2f, fzU + 0.12f), 0.35f, sx > 0);
        b.Push(P(-w / 2f, y0 + 0.9f, -d * 0.2f), -90f);
        FantasyKit.BrickPatch(b, P(0f, 0.6f, 0f), 1.8f, 1.3f, seed);
        b.Pop();
        b.Push(P(w / 2f, 0f, -d * 0.32f), 90f);
        FantasyKit.Ladder(b, P(0f, 0f, 0.05f), Mathf.Min(yT, 5.2f), 0.6f);
        b.Pop();
        FantasyKit.HangPot(b, P(-doorW * 0.5f - 1.3f, y0 + 2.8f, fz + 0.04f), Sl.Blossom);
        FantasyKit.HangPot(b, P(doorW * 0.5f + 1.3f, y0 + 2.8f, fz + 0.04f), Sl.Gold);
        FantasyKit.HerbBundle(b, P(doorW * 0.5f + 0.8f, y0 + 3.0f, fz + 0.2f), seed);
        if (floors > 1) FantasyKit.Hoist(b, P(w * 0.18f, yT - 0.9f, fzU + 0.04f));
        FantasyKit.StepStones(b, P(0f, 0.04f, fz + 4.5f), 4, seed);
        // atap utama
        b.Gable(roofSlot, Sl.Plaster, Sl.Wood, P(0f, yT, zT), w, dT, rise, 1.0f, true);
        if (wings > 0)
        {
            float cw = Mathf.Min(8.2f, w * 0.28f), cd = 2.8f;
            for (int k = 0; k < (wings > 1 ? 2 : 1); k++)
            {
                float cx = (wings > 1) ? (k == 0 ? -1f : 1f) * w * 0.3f : -w * 0.3f;
                float hh = (floors > 1 ? fh : 0f) + fh * 0.0f;
                b.BoxB(Sl.Plaster, P(cx, yT - fh, fzU + cd / 2f - 0.05f), new Vector3(cw, fh, cd + 0.1f));
                b.Push(P(cx, yT - fh, fzU + cd), 0f); FantasyKit.Frame(b, cw, 0f, fh, seed + k, 1); b.Pop();
                b.Gable(roofSlot2, Sl.Plaster, Sl.Wood, P(cx, yT, fzU + cd / 2f - 0.05f), cw, cd + 0.1f, rise * 0.72f, 0.95f, false);
                BuildKit.Window(b, P(cx, yT - fh * 0.42f, fzU + cd + 0.02f), 1.5f, 2.1f, true, true);
                AnimeKit.RoundWindow(b, P(cx, yT + rise * 0.3f, fzU + cd + 0.03f), 0.6f);
                AnimeKit.PennantTop(b, P(cx, yT + rise * 0.72f, fzU + cd / 2f), k == 0 ? Sl.Cloth : Sl.ClothB);
            }
        }
        int nd = Mathf.Max(2, Mathf.RoundToInt(w / 7.5f));
        for (int k = 0; k < nd; k++)
        {
            float x = (k - (nd - 1) / 2f) * w / nd;
            if (wings > 0 && Mathf.Abs(Mathf.Abs(x) - w * 0.3f) < 3.2f) continue;
            Dormers(b, yT, zT, dT, rise, roofSlot2, x);
        }
        foreach (float cx in new[] { -w * 0.38f, w * 0.38f })
            FantasyKit.RoundChimney(b, P(cx, yT - 1f, -dT * 0.18f), 0.85f, rise * 0.82f + 1.8f);
        // hiasan
        AnimeKit.Bunting(b, P(-w / 2f + 0.5f, y0 + fh + 0.55f, fzU + 0.5f), P(w / 2f - 0.5f, y0 + fh + 0.55f, fzU + 0.5f), 1.0f, Mathf.RoundToInt(w / 2f), seed);
        foreach (float fx in new[] { -w / 2f + 1.4f, w / 2f - 1.4f, -doorW - 2.2f, doorW + 2.2f })
            AnimeKit.FlowerBush(b, P(fx, 0f, fz + 1.5f), 0.85f, fx < 0f ? Sl.Blossom : Sl.Gold);
        AnimeKit.HangLantern(b, P(-doorW / 2f - 1.3f, y0 + doorW * 1.25f + 0.4f, fz + 0.1f), 0.9f);
        AnimeKit.HangLantern(b, P(doorW / 2f + 1.3f, y0 + doorW * 1.25f + 0.4f, fz + 0.1f), 0.9f);
        AnimeKit.Ivy(b, P(w / 2f - 0.5f, y0, fzU + 0.03f), fh * floors * 0.85f, 1.6f, seed);
        return yT + rise;
    }

    public static void Tavern(GeoBag b)
    {
        float w = TavernW, d = TavernD;
        float top = Hall(b, w, d, 2, 4.0f, 0.9f, false, 2, 7.5f, Sl.Roof, Sl.RoofB, 21, 2.4f);
        float y0 = 0.9f, fz = d / 2f;
        BuildKit.Awning(b, P(0f, y0 + 3.9f, fz + 0.1f), w - 8f, 2.2f, 0.9f, Sl.Cloth, Sl.Plaster);
        for (int i = 0; i < 3; i++)
        {
            float x = -8f + i * 8f;
            b.Cyl(Sl.WoodLight, P(x, 0f, fz + 3.6f), 0.9f, 0.9f, 10, 0.9f, true);
            foreach (float a in new[] { 0.8f, 2.4f, 4.0f, 5.5f })
                b.Cyl(Sl.Wood, P(x + Mathf.Sin(a) * 1.5f, 0f, fz + 3.6f + Mathf.Cos(a) * 1.5f), 0.28f, 0.6f, 7, 0.28f, true);
            b.Ball(Sl.Gold, P(x, 1.1f, fz + 3.6f), 0.14f, 3, 5);
        }
        BuildKit.Barrel(b, P(-w / 2f + 1.2f, 0f, fz + 1.2f));
        BuildKit.Barrel(b, P(-w / 2f + 2.4f, 0f, fz + 1.5f));
        BuildKit.HangingSign(b, P(w / 2f - 0.3f, y0 + 4.0f + 3.0f, fz + 0.75f), 2.2f, Sl.Gold);
        for (int i = 0; i < 5; i++) BuildKit.Lantern(b, P(-8f + i * 4f, y0 + 3.0f, fz + 2.0f));
    }

    public static void Inn(GeoBag b)
    {
        float w = InnW, d = InnD;
        float y0 = 1f, fh = 3.7f;
        float top = Hall(b, w, d, 3, fh, y0, true, 2, 8.5f, Sl.RoofB, Sl.Roof, 33, 2.6f);
        float fz = d / 2f, jet = 0.7f;
        FantasyKit.Balcony(b, P(0f, y0 + 2 * fh + 0.15f, fz + jet), w * 0.4f, 1.6f);
        BuildKit.HangingSign(b, P(-w / 2f + 0.3f, y0 + fh + 3.0f, fz + jet + 0.05f), 2.4f, Sl.Gold);
        // istal samping kecil beratap lengkung
        b.BoxB(Sl.Wood, P(w / 2f + 3.2f, 0f, -1f), new Vector3(6f, 3.2f, 11f));
        b.Gable(Sl.Thatch, Sl.Wood, Sl.Wood, P(w / 2f + 3.2f, 3.2f, -1f), 6f, 11f, 2.8f, 0.7f, false);
        BuildKit.Crate(b, P(w / 2f - 2f, 0f, fz + 1.5f), 1.0f, 8f);
        BuildKit.Barrel(b, P(-w / 2f + 1.5f, 0f, fz + 1.4f));
        FantasyKit.Woodpile(b, P(w / 2f + 3.2f, 0f, 5.8f), 90f);
    }

    public static void Shop(GeoBag b)
    {
        float w = ShopW, d = ShopD;
        float y0 = 0.9f, fh = 3.7f;
        float top = Hall(b, w, d, 2, fh, y0, false, 1, 7f, Sl.RoofB, Sl.Roof, 45, 2.2f);
        float fz = d / 2f;
        // etalase besar di kanan pintu + kanopi bergaris
        BuildKit.Awning(b, P(0f, y0 + 3.5f, fz + 0.1f), w - 3f, 2.0f, 0.8f, Sl.Cloth, Sl.Plaster);
        foreach (float x in new[] { -9f, -5.5f, 5.5f, 9f })
            b.Box(Sl.WoodLight, P(x, y0 + 0.7f, fz + 0.7f), new Vector3(1.8f, 1.2f, 0.9f));
        for (int i = 0; i < 4; i++)
        {
            BuildKit.Crate(b, P(-w / 2f + 0.8f + i * 1.15f, 0f, fz + 2.3f), 0.85f, i * 14f);
            b.Ball(i % 2 == 0 ? Sl.Cloth : Sl.Gold, P(-w / 2f + 0.8f + i * 1.15f, 1.1f, fz + 2.3f), 0.2f, 3, 6);
        }
        BuildKit.Barrel(b, P(w / 2f - 1.2f, 0f, fz + 1.8f));
        BuildKit.HangingSign(b, P(w / 2f - 0.3f, y0 + fh + 3.0f, fz + 1.4f), 2.0f, Sl.Gold);
    }

    public static void Academy(GeoBag b)
    {
        float w = AcadW, d = AcadD;
        float y0 = 1f, fh = 4.4f;
        Hall(b, w, d, 2, fh, y0, true, 2, 6.5f, Sl.RoofB, Sl.RoofB, 57, 2.8f);
        Portico(b, 0f, d / 2f, 8.5f, 4.2f, y0, 4.6f, 4, false);
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
        FantasyKit.FlareRoof(b, Sl.RoofB, P(0, 27.8f, tz), 2.6f, 5.5f, 12, Sl.Gold);
        foreach (float sx in new[] { -1f, 1f })
            BuildKit.RoundTower(b, P(sx * (w / 2f), 0f, d / 2f - 1f), 2.6f, 11f, Sl.Stone, Sl.DarkStone, Sl.RoofB, 8f, false, true, Sl.ClothB);
        b.Box(Sl.Gold, P(0, y0 + 6.2f, d / 2f + 0.15f), new Vector3(2.4f, 1.6f, 0.2f));
        b.Box(Sl.Cloth, P(0, y0 + 6.2f, d / 2f + 0.28f), new Vector3(1.0f, 1.3f, 0.1f));
    }

    // ====================================================================== PANDAI BESI
    public static void Smithy(GeoBag b)
    {
        float w = SmithW, d = SmithD, h = 5.4f, y0 = 0.9f, rise = 6.4f;
        FantasyKit.Foundation(b, 0f, 0f, w, d, y0, 71);
        FantasyKit.Wall(b, Sl.Stone, P(0, y0, 0), new Vector3(w, h, d), 1);
        FantasyKit.CornerPosts(b, 0f, 0f, w, d, y0, h, 3);
        StoneWallFront(b, w, y0, h, d / 2f, 6, Sl.Stone);
        // lantai loteng kayu menjorok di bagian atas dinding depan (gable berrangka)
        b.Gable(Sl.RoofB, Sl.Plaster, Sl.Wood, P(0, y0 + h, 0f), w, d, rise, 1.2f, true);
        // pintu bengkel & jendela
        BuildKit.DoorFront(b, P(-5f, y0, d / 2f), 2.4f, 3.4f, true);
        BuildKit.Window(b, P(-10f, y0 + 2.9f, d / 2f + 0.02f), 1.3f, 1.7f, true, true);
        foreach (float sx in new[] { -1f, 1f })
            for (int k = 0; k < 2; k++)
            {
                b.Push(P(sx * w / 2f, y0 + 2.9f, (k - 0.5f) * 8f), sx * 90f);
                BuildKit.Window(b, Vector3.zero, 1.2f, 1.6f, true, false);
                b.Pop();
            }
        // dormer berlampu
        b.Push(P(-6f, 0f, d * 0.2f), 0f); BuildKit.Dormer(b, P(0f, y0 + h + rise * 0.45f, 0f), 1.8f, 2.0f, Sl.Roof, Sl.Plaster); b.Pop();
        // cerobong tungku besar dari batu bersusun dengan celah menyala
        float cx = 8f, cz = -3.8f;
        b.BoxB(Sl.Stone, P(cx, y0 + h - 2.2f, cz), new Vector3(3.0f, rise + 5.6f, 3.0f));
        b.BoxB(Sl.DarkStone, P(cx, y0 + h + rise + 3.0f, cz), new Vector3(3.6f, 0.5f, 3.6f));
        b.BoxB(Sl.DarkStone, P(cx, y0 + h + rise + 3.5f, cz), new Vector3(2.2f, 0.9f, 2.2f));
        b.Cone(Sl.Roof, P(cx, y0 + h + rise + 4.4f, cz), 1.4f, 1.3f, 4);
        b.Box(Sl.Glow, P(cx, y0 + h + rise + 3.9f, cz + 1.12f), new Vector3(1.0f, 0.3f, 0.06f));
        FantasyKit.Moss(b, P(cx + 1.3f, y0 + h - 0.4f, cz + 0.5f), 0.4f, 3);
        // bengkel terbuka (lean-to) di depan kanan: tiang + atap melengkung
        float lx = 6.8f, lz = d / 2f + 3.6f;
        foreach (float px in new[] { -1f, 1f })
            foreach (float pz in new[] { -1f, 1f })
            {
                Vector3 pp = P(lx + px * 5.6f, 0f, lz + pz * 3.1f);
                b.Cyl(Sl.Wood, pp, 0.22f, 4.4f, 6, 0.2f, true);
                b.Beam(Sl.Wood, pp + P(0f, 3.5f, 0f), pp + P(-px * 0.9f, 4.4f, 0f), 0.14f, 0.14f);
            }
        b.Gable(Sl.RoofB, Sl.Wood, Sl.Wood, P(lx, 4.4f, lz), 11.6f, 6.6f, 2.0f, 0.8f, true);
        b.Box(Sl.Wood, P(lx, 4.35f, lz + 3.1f), new Vector3(11.6f, 0.28f, 0.3f));
        // tungku (forge) dengan bara menyala + cerobong kecil
        b.BoxB(Sl.DarkStone, P(lx + 2.8f, 0f, lz - 1.6f), new Vector3(3.0f, 1.3f, 2.2f));
        b.Box(Sl.Glow, P(lx + 2.8f, 1.34f, lz - 1.4f), new Vector3(2.2f, 0.12f, 1.5f));
        b.Box(Sl.Gold, P(lx + 2.8f, 1.5f, lz - 1.4f), new Vector3(1.2f, 0.22f, 0.8f));
        b.BoxB(Sl.DarkStone, P(lx + 2.8f, 1.3f, lz - 2.4f), new Vector3(3.0f, 3.2f, 0.8f));
        // landasan di tunggul, palung air, pengisap angin, batu asah
        b.Cyl(Sl.Wood, P(lx - 1.2f, 0f, lz), 0.62f, 0.8f, 8, 0.62f, true);
        b.Box(Sl.Metal, P(lx - 1.2f, 1.05f, lz), new Vector3(0.7f, 0.3f, 1.5f));
        b.Box(Sl.Metal, P(lx - 1.2f, 0.92f, lz), new Vector3(0.45f, 0.3f, 0.7f));
        b.Cone(Sl.Metal, P(lx - 1.2f, 1.2f, lz + 0.75f), 0.18f, 0.5f, 6);
        b.Box(Sl.Wood, P(lx - 3.6f, 0.55f, lz - 1.2f), new Vector3(2.2f, 1.1f, 0.9f));
        b.Box(Sl.Water, P(lx - 3.6f, 1.07f, lz - 1.2f), new Vector3(1.9f, 0.04f, 0.6f));
        BuildKit.Barrel(b, P(lx - 4.8f, 0f, lz - 1.8f));
        b.Cyl(Sl.Stone, P(lx - 3.4f, 0.7f, lz + 1.6f), 0.55f, 0.18f, 12, 0.55f, true);
        b.Box(Sl.Wood, P(lx - 3.4f, 0.35f, lz + 1.6f), new Vector3(1.0f, 0.7f, 0.2f));
        // arang & peti besi
        b.Ball(Sl.DarkStone, P(lx + 5.0f, 0.35f, lz + 1.4f), 0.7f, 4, 7, 0.7f);
        BuildKit.Crate(b, P(lx + 4.6f, 0f, lz - 0.2f), 0.9f, 22f);
        // alat tergantung pada balok depan
        for (int i = 0; i < 6; i++)
            b.Beam(Sl.Metal, P(lx - 4.8f + i * 1.9f, 4.25f, lz + 3.05f), P(lx - 4.8f + i * 1.9f, 3.1f - (i % 2) * 0.4f, lz + 3.05f), 0.08f, 0.08f);
        // rak senjata + papan palu bergaya
        b.Box(Sl.Wood, P(-8f, 0.1f, d / 2f + 0.9f), new Vector3(2.6f, 0.2f, 0.5f));
        for (int i = 0; i < 4; i++)
            b.Beam(Sl.Metal, P(-9f + i * 0.5f, 0.2f, d / 2f + 0.9f), P(-9f + i * 0.5f, 3.0f, d / 2f + 0.5f), 0.14f, 0.06f);
        b.Box(Sl.Metal, P(-5f, y0 + 4.9f, d / 2f + 0.55f), new Vector3(0.2f, 1.6f, 0.14f));
        b.Box(Sl.Metal, P(-5f, y0 + 5.6f, d / 2f + 0.55f), new Vector3(1.3f, 0.6f, 0.16f));
        AnimeKit.HangLantern(b, P(-2.4f, y0 + 3.6f, d / 2f + 0.1f), 0.8f);
        AnimeKit.FlowerBush(b, P(-11.3f, 0f, d / 2f + 1.2f), 0.8f, Sl.Gold);
        AnimeKit.PennantTop(b, P(-6f, y0 + h + rise, 0f), Sl.Cloth);
        AnimeKit.Ivy(b, P(-w / 2f + 0.8f, y0, d / 2f + 0.03f), 4.2f, 1.4f, 6);
    }

    // ====================================================================== BARAK
    public static void Barracks(GeoBag b)
    {
        float w = BarrW, d = BarrD, h = 6.4f, y0 = 0.9f;
        FantasyKit.Foundation(b, 0f, 0f, w, d, y0, 77);
        b.BoxB(Sl.Stone, P(0, y0, 0), new Vector3(w, h, d));
        StoneWallFront(b, w, y0, h, d / 2f, 6, Sl.Stone);
        // lantai atas kayu menjorok (hoarding) + atap pelana lengkung
        float jet = 0.9f, hu = 3.6f, yu = y0 + h;
        b.BoxB(Sl.Plaster, P(0, yu, jet / 2f), new Vector3(w, hu, d + jet));
        FantasyKit.CornerPosts(b, 0f, jet / 2f, w, d + jet, yu, hu, 4);
        for (int i = 0; i < 11; i++)
        {
            float x = -w / 2f + 1f + i * (w - 2f) / 10f;
            b.Beam(Sl.Wood, P(x, yu - 0.8f, d / 2f), P(x, yu, d / 2f + jet), 0.25f, 0.25f);
        }
        b.Push(P(0, yu, d / 2f + jet), 0f); FantasyKit.Frame(b, w, 0f, hu, 3, 1); b.Pop();
        b.Push(P(0, yu, -d / 2f), 180f); FantasyKit.Frame(b, w, 0f, hu, 5, 1); b.Pop();
        b.Push(P(w / 2f, yu, jet / 2f), 90f); FantasyKit.Frame(b, d + jet, 0f, hu, 7, 1); b.Pop();
        b.Push(P(-w / 2f, yu, jet / 2f), -90f); FantasyKit.Frame(b, d + jet, 0f, hu, 9, 1); b.Pop();
        float yT = yu + hu, rise = 6.2f;
        b.Gable(Sl.Roof, Sl.Plaster, Sl.Wood, P(0, yT, jet / 2f), w, d + jet, rise, 1.0f, true);
        foreach (float x in new[] { -8.2f, -3.8f, 3.8f, 8.2f })
        {
            BuildKit.Window(b, P(x, yu + hu * 0.55f, d / 2f + jet + 0.02f), 1.3f, 1.9f, true, false);
            BuildKit.Slit(b, P(x, y0 + 3.3f, d / 2f + 0.02f), 2.4f);
        }
        // menara bulat di empat sudut
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                BuildKit.RoundTower(b, P(sx * (w / 2f), 0f, sz * (d / 2f)), 2.9f, 15f, Sl.Stone, Sl.DarkStone, Sl.Roof, 9f, false, true, Sl.Cloth);
        // gerbang masuk berbingkai batu dengan lambang
        b.BoxB(Sl.DarkStone, P(0, 0, d / 2f + 0.3f), new Vector3(7.6f, 6.4f, 0.8f));
        BuildKit.DoorFront(b, P(0, y0, d / 2f + 0.7f), 3.4f, 4.2f, true);
        b.Box(Sl.Gold, P(0, y0 + 5.9f, d / 2f + 0.8f), new Vector3(2.0f, 2.0f, 0.14f));
        b.Box(Sl.Cloth, P(0, y0 + 5.9f, d / 2f + 0.88f), new Vector3(1.5f, 1.5f, 0.1f));
        BuildKit.Banner(b, P(-8f, y0 + 5.6f, d / 2f + 0.5f), 1.6f, 4.0f, Sl.Cloth, true);
        BuildKit.Banner(b, P(8f, y0 + 5.6f, d / 2f + 0.5f), 1.6f, 4.0f, Sl.Cloth, true);
        // halaman latihan
        for (int i = 0; i < 3; i++)
        {
            float x = -8f + i * 4.5f, z = d / 2f + 4.4f;
            b.Cyl(Sl.Wood, P(x, 0f, z), 0.12f, 2.2f, 6, 0.12f, true);
            b.Beam(Sl.Wood, P(x - 0.8f, 1.7f, z), P(x + 0.8f, 1.7f, z), 0.12f, 0.12f);
            b.Ball(Sl.Thatch, P(x, 2.35f, z), 0.3f, 4, 6);
            b.Box(Sl.Cloth, P(x, 1.4f, z), new Vector3(0.6f, 0.9f, 0.25f));
        }
        b.Box(Sl.Wood, P(9f, 1.0f, d / 2f + 2.6f), new Vector3(3.2f, 0.18f, 0.5f));
        for (int i = 0; i < 5; i++)
            b.Beam(Sl.Metal, P(7.8f + i * 0.6f, 0.2f, d / 2f + 2.6f), P(7.8f + i * 0.6f, 2.6f, d / 2f + 2.4f), 0.12f, 0.06f);
        FantasyKit.RoundChimney(b, P(-w / 2f + 3.5f, yT - 1f, -3f), 0.8f, rise * 0.8f + 1.4f);
        AnimeKit.PennantTop(b, P(0f, yT + rise, 0f), Sl.Cloth);
        AnimeKit.HangLantern(b, P(-2.8f, y0 + 4.4f, d / 2f + 0.8f), 0.8f);
        AnimeKit.HangLantern(b, P(2.8f, y0 + 4.4f, d / 2f + 0.8f), 0.8f);
    }

    // ====================================================================== ISTAL
    public static void Stables(GeoBag b)
    {
        float w = StableW, d = StableD, h = 5.6f, y0 = 0.9f, rise = 6.2f;
        FantasyKit.Foundation(b, 0f, -1f, w, d - 2f, y0, 83);
        b.BoxB(Sl.Wood, P(0, y0, -1f), new Vector3(w, h, d - 2f));
        FantasyKit.CornerPosts(b, 0f, -1f, w, d - 2f, y0, h, 5);
        b.Push(P(0, y0, d / 2f - 2f), 0f); FantasyKit.Frame(b, w, 0f, h, 4, 1); b.Pop();
        b.Gable(Sl.RoofB, Sl.Wood, Sl.Wood, P(0, y0 + h, -1f), w, d - 2f, rise, 1.1f, true);
        // papan dinding pintu lumbung depan + pintu loteng berbingkai
        BuildKit.DoorFront(b, P(-6f, y0, d / 2f - 2f), 3.0f, 3.6f, true);
        BuildKit.DoorFront(b, P(6f, y0, d / 2f - 2f), 3.0f, 3.6f, true);
        b.Push(P(0f, 0f, d * 0.12f), 0f); BuildKit.Dormer(b, P(0f, y0 + h + rise * 0.3f, 0f), 2.2f, 2.6f, Sl.Roof, Sl.Wood); b.Pop();
        // kandang terbuka di depan: tiang + sekat + atap miring melengkung
        for (int i = 0; i < 5; i++)
        {
            float x = -w / 2f + 2f + i * (w - 4f) / 4f;
            b.Cyl(Sl.Wood, P(x, 0f, d / 2f - 0.2f), 0.2f, 3.8f, 6, 0.18f, true);
            b.Box(Sl.Wood, P(x, 0.9f, d / 2f - 2.8f), new Vector3(0.18f, 1.8f, 4.6f));
        }
        b.Box(Sl.Wood, P(0, 3.7f, d / 2f - 0.2f), new Vector3(w - 2f, 0.4f, 0.5f));
        b.Gable(Sl.RoofB, Sl.Wood, Sl.Wood, P(0, 3.8f, d / 2f - 1.2f), w - 1.6f, 2.4f, 1.3f, 0.7f, true);
        for (int i = 0; i < 4; i++) PropKit.Haystack(b, P(-9f + i * 5.6f, 0f, d / 2f - 4.0f));
        foreach (float x in new[] { -4f, 6f })
            Horse(b, P(x, 0f, d / 2f + 3.6f), x < 0f ? 30f : -20f);
        b.Box(Sl.Wood, P(-w / 2f + 2f, 0.45f, d / 2f + 1.2f), new Vector3(3.4f, 0.8f, 0.9f));
        b.Box(Sl.Water, P(-w / 2f + 2f, 0.88f, d / 2f + 1.2f), new Vector3(3.0f, 0.04f, 0.6f));
        // atap lampu kecil (cupola) di punggung atap dengan penunjuk angin
        b.BoxB(Sl.Wood, P(0f, y0 + h + rise - 0.2f, -1f), new Vector3(2.2f, 1.6f, 2.2f));
        b.Hip(Sl.Roof, Sl.Wood, P(0f, y0 + h + rise + 1.4f, -1f), 2.4f, 2.4f, 1.6f, 0.4f);
        b.Beam(Sl.Metal, P(0f, y0 + h + rise + 3f, -1f), P(0f, y0 + h + rise + 4.2f, -1f), 0.06f, 0.06f);
        b.Ball(Sl.Gold, P(0f, y0 + h + rise + 4.3f, -1f), 0.14f, 3, 5);
        BuildKit.HangingSign(b, P(w / 2f - 0.4f, y0 + 4.3f, d / 2f - 0.2f), 1.8f, Sl.Metal);
        AnimeKit.Bunting(b, P(-w / 2f + 0.6f, y0 + h - 0.3f, d / 2f + 0.3f), P(w / 2f - 0.6f, y0 + h - 0.3f, d / 2f + 0.3f), 0.9f, 12, 8);
        AnimeKit.FlowerBush(b, P(w / 2f - 1.2f, 0f, d / 2f + 1.2f), 0.8f, Sl.Gold);
    }

    // ====================================================================== KINCIR ANGIN
    public static void WindmillBody(GeoBag b)
    {
        // dasar batu bulat, badan mengerucut berplester dengan pita kayu, galeri keliling, atap terompet
        FantasyKit.Foundation(b, 0f, 0f, 9.4f, 9.4f, 0.01f, 5);   // alas bayangan tipis (nol tinggi) agar bagan batu tidak null
        b.Cyl(Sl.DarkStone, P(0, 0, 0), 5.5f, 0.4f, 16, 5.4f, true);
        b.Cyl(Sl.Stone, P(0, 0.4f, 0), 5.3f, 3.6f, 16, 5.0f, true);
        for (int i = 0; i < 12; i++)
        {
            float a = i / 12f * Mathf.PI * 2f;
            b.Ball(Sl.Stone, P(Mathf.Sin(a) * 5.35f, 1.2f + 0.5f * Mathf.Sin(i * 1.7f), Mathf.Cos(a) * 5.35f), 0.5f, 3, 6, 0.9f);
        }
        b.Cyl(Sl.Plaster, P(0, 4.0f, 0), 5.0f, 13.0f, 16, 3.5f, false);
        for (int i = 0; i < 4; i++)
        {
            float t = (i + 0.5f) / 4.5f;
            float r = Mathf.Lerp(5.0f, 3.5f, t) + 0.12f;
            b.Cyl(Sl.Wood, P(0, 4.0f + 13.0f * t, 0), r, 0.3f, 16, r, true);
        }
        for (int k = 0; k < 8; k++)
        {
            float a = k / 8f * Mathf.PI * 2f + 0.2f;
            b.Beam(Sl.Wood, P(Mathf.Sin(a) * 5.05f, 4.0f, Mathf.Cos(a) * 5.05f), P(Mathf.Sin(a) * 3.6f, 17.0f, Mathf.Cos(a) * 3.6f), 0.2f, 0.2f);
        }
        // galeri keliling
        float gy = 9.2f, gr = 4.35f;
        b.Cyl(Sl.WoodLight, P(0, gy - 0.2f, 0), gr + 1.2f, 0.25f, 16, gr + 1.2f, true);
        for (int i = 0; i < 16; i++)
        {
            float a = i / 16f * Mathf.PI * 2f;
            b.Cyl(Sl.Wood, P(Mathf.Sin(a) * (gr + 1.1f), gy, Mathf.Cos(a) * (gr + 1.1f)), 0.06f, 1.0f, 5, 0.05f, true);
        }
        b.Cyl(Sl.Wood, P(0, gy + 0.95f, 0), gr + 1.2f, 0.1f, 16, gr + 1.2f, true);
        // topi atap
        b.Cyl(Sl.Wood, P(0, 17f, 0), 3.6f, 0.7f, 16, 3.9f, true);
        FantasyKit.FlareRoof(b, Sl.RoofB, P(0, 17.7f, 0), 4.2f, 6.6f, 16, Sl.Wood);
        b.Box(Sl.Wood, P(0, 15.6f, 3.6f), new Vector3(1.0f, 1.0f, 1.5f));       // poros
        // pintu dan jendela lengkung
        BuildKit.DoorFront(b, P(0, 0.4f, 5.15f), 1.8f, 2.8f, true);
        foreach (float a in new[] { 1.6f, 3.2f, 4.7f, 0.5f })
        {
            b.Push(P(Mathf.Sin(a) * 4.15f, 7.0f, Mathf.Cos(a) * 4.15f), a * Mathf.Rad2Deg);
            BuildKit.Window(b, Vector3.zero, 0.9f, 1.4f, true, a > 4f);
            b.Pop();
        }
        foreach (float a in new[] { 2.4f, 5.6f })
        {
            b.Push(P(Mathf.Sin(a) * 3.65f, 13.0f, Mathf.Cos(a) * 3.65f), a * Mathf.Rad2Deg);
            BuildKit.Window(b, Vector3.zero, 0.8f, 1.2f, true, false);
            b.Pop();
        }
        // karung gandum & gerobak
        for (int i = 0; i < 4; i++) b.Ball(Sl.Thatch, P(-2.6f + i * 0.9f, 0.5f, 6.3f + (i % 2) * 0.5f), 0.5f, 4, 7, 0.8f);
        AnimeKit.FlowerBush(b, P(3.4f, 0f, 5.6f), 0.7f, Sl.Blossom);
        AnimeKit.FlowerBush(b, P(-3.6f, 0f, 5.4f), 0.7f, Sl.Gold);
        AnimeKit.Ivy(b, P(-3.8f, 0.5f, 3.4f), 7f, 1.6f, 8);
        AnimeKit.Bunting(b, P(-4.8f, 9.6f, 3.3f), P(4.8f, 9.6f, 3.3f), 0.9f, 8, 4);
    }

    // ====================================================================== LUMBUNG
    public static void Barn(GeoBag b)
    {
        float w = 15f, d = 10.5f, h = 5.4f, y0 = 0.9f;
        FantasyKit.Foundation(b, 0f, 0f, w, d, y0, 91);
        b.BoxB(Sl.Cloth, P(0, y0, 0), new Vector3(w, h, d));
        FantasyKit.CornerPosts(b, 0f, 0f, w, d, y0, h, 9);
        b.Push(P(0, y0, d / 2f), 0f); FantasyKit.Frame(b, w, 0f, h, 11, 1); b.Pop();
        b.Push(P(0, y0, -d / 2f), 180f); FantasyKit.Frame(b, w, 0f, h, 12, 1); b.Pop();
        b.Push(P(w / 2f, y0, 0f), 90f); FantasyKit.Frame(b, d, 0f, h, 13, 1); b.Pop();
        b.Push(P(-w / 2f, y0, 0f), -90f); FantasyKit.Frame(b, d, 0f, h, 14, 1); b.Pop();
        b.Gable(Sl.RoofB, Sl.Cloth, Sl.Wood, P(0, y0 + h, 0), w, d, 7.0f, 1.1f, false);
        float zf = d / 2f;
        // pintu besar dengan palang X dan lengkung atas
        b.Box(Sl.WoodLight, P(0, y0 + 2.3f, zf + 0.06f), new Vector3(6.4f, 4.6f, 0.16f));
        b.Box(Sl.Wood, P(0, y0 + 2.3f, zf + 0.15f), new Vector3(0.18f, 4.6f, 0.08f));
        foreach (float sx in new[] { -1f, 1f })
        {
            b.Beam(Sl.Wood, P(sx * 3.1f, y0 + 0.1f, zf + 0.17f), P(sx * 0.1f, y0 + 4.5f, zf + 0.17f), 0.3f, 0.1f, Vector3.forward);
            b.Beam(Sl.Wood, P(sx * 0.1f, y0 + 0.1f, zf + 0.17f), P(sx * 3.1f, y0 + 4.5f, zf + 0.17f), 0.3f, 0.1f, Vector3.forward);
        }
        b.Box(Sl.Wood, P(0, y0 + 4.7f, zf + 0.1f), new Vector3(6.9f, 0.3f, 0.3f));
        // pintu loteng + katrol + jendela bulat di gable
        b.Box(Sl.WoodLight, P(0, y0 + h + 1.7f, zf + 0.05f), new Vector3(1.8f, 2.1f, 0.1f));
        b.Beam(Sl.Wood, P(0, y0 + h + 3.6f, zf), P(0, y0 + h + 3.6f, zf + 1.5f), 0.16f, 0.16f);
        AnimeKit.RoundWindow(b, P(0, y0 + h + 5.0f, zf + 0.05f), 0.5f);
        BuildKit.Window(b, P(-5.3f, y0 + 2.8f, zf + 0.02f), 1.2f, 1.6f, true, true);
        BuildKit.Window(b, P(5.3f, y0 + 2.8f, zf + 0.02f), 1.2f, 1.6f, true, true);
        // silo batu berkubah terompet
        b.Cyl(Sl.DarkStone, P(w / 2f + 3.2f, 0, 1.5f), 2.9f, 0.5f, 14, 2.8f, true);
        b.Cyl(Sl.Stone, P(w / 2f + 3.2f, 0.5f, 1.5f), 2.65f, 10.5f, 14, 2.55f, false);
        for (int i = 1; i < 4; i++) b.Cyl(Sl.Metal, P(w / 2f + 3.2f, i * 2.7f, 1.5f), 2.7f, 0.15f, 14, 2.7f, true);
        FantasyKit.FlareRoof(b, Sl.RoofB, P(w / 2f + 3.2f, 11f, 1.5f), 3.1f, 4.4f, 14, Sl.Wood);
        BuildKit.DoorFront(b, P(w / 2f + 3.2f, 0.5f, 1.5f + 2.55f), 1.2f, 2.0f, true);
        PropKit.Haystack(b, P(-w / 2f - 3f, 0, 2f));
        PropKit.Haystack(b, P(-w / 2f - 3.5f, 0, -2.4f));
        FantasyKit.Picket(b, -w / 2f, w / 2f, zf + 3.4f, 0.9f);
        BuildKit.Barrel(b, P(w / 2f - 0.8f, 0f, zf + 1.0f));
        BuildKit.Crate(b, P(w / 2f - 2.0f, 0f, zf + 1.0f), 0.9f, 10f);
        AnimeKit.FlowerBush(b, P(-w / 2f + 1.0f, 0f, zf + 1.2f), 0.8f, Sl.Gold);
        AnimeKit.PennantTop(b, P(0f, y0 + h + 7.0f, 0f), Sl.Cloth);
    }

    // ====================================================================== GAZEBO
    public static void Gazebo(GeoBag b)
    {
        b.Cyl(Sl.DarkStone, P(0, 0, 0), 4.3f, 0.25f, 12, 4.2f, true);
        b.Cyl(Sl.Marble, P(0, 0.25f, 0), 4.0f, 0.35f, 12, 3.8f, true);
        for (int i = 0; i < 8; i++)
        {
            float a = i / 8f * Mathf.PI * 2f;
            BuildKit.Column(b, P(Mathf.Sin(a) * 3.2f, 0.6f, Mathf.Cos(a) * 3.2f), 0.22f, 3.6f, Sl.Marble);
            if (i != 0) { b.Box(Sl.Marble, P(Mathf.Sin(a) * 3.2f, 1.1f, Mathf.Cos(a) * 3.2f), new Vector3(1.9f, 0.14f, 0.22f), a * Mathf.Rad2Deg + 90f); }
            // lampu gantung
            AnimeKit.Ivy(b, P(Mathf.Sin(a) * 3.35f, 0.7f, Mathf.Cos(a) * 3.35f), 3.2f, 0.4f, i);
        }
        b.Cyl(Sl.Marble, P(0, 4.2f, 0), 3.9f, 0.5f, 12, 3.7f, true);
        FantasyKit.FlareRoof(b, Sl.RoofB, P(0, 4.7f, 0), 4.1f, 4.4f, 12, Sl.Gold);
        b.Cyl(Sl.Water, P(0, 0.62f, 0), 3.0f, 0.04f, 12, 3.0f, true);
        b.Cyl(Sl.Marble, P(0, 0.6f, 0), 0.5f, 0.9f, 8, 0.4f, true);
        b.Ball(Sl.Glow, P(0, 3.4f, 0), 0.3f, 4, 8, 1.2f);
        b.Beam(Sl.Metal, P(0, 4.2f, 0), P(0, 3.7f, 0), 0.04f, 0.04f);
        AnimeKit.Bunting(b, P(-3.1f, 3.9f, 1.2f), P(3.1f, 3.9f, 1.2f), 0.6f, 6, 2);
    }
}
