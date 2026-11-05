using UnityEngine;

public static partial class FantasyKit
{
    /// <summary>
    /// Rumah warga isekai-fantasi. kind: 0 rumah kota, 1 toko, 2 sempit-tinggi, 3 pondok, 4 rumah bermenara, 5 rumah panjang.
    /// Mengembalikan tinggi total. Ukuran jejak (w,d) sama dengan HouseKit lama supaya penempatan tidak berubah.
    /// </summary>
    public static float House(GeoBag b, int kind, float w, float d, int floors, int seed)
    {
        int rnd = Mathf.Abs(seed);
        bool shop = kind == 1, cottage = kind == 3, narrow = kind == 2, tower = kind == 4, longh = kind == 5;
        float plinth = cottage ? 0.5f : 0.65f;
        float fh = cottage ? 2.9f : 3.15f;
        float jet = cottage ? 0f : 0.5f;

        // ---------- badan utama
        Foundation(b, 0f, 0f, w, d, plinth, rnd);
        float y = plinth;
        for (int i = 0; i < floors; i++)
        {
            float dd = d + (i > 0 ? jet : 0f);
            float zc = i > 0 ? jet / 2f : 0f;
            int wall = (i == 0 && !cottage) ? Sl.PlasterB : Sl.Plaster;
            Wall(b, wall, P(0f, y, zc), new Vector3(w, fh, dd), 0);
            CornerPosts(b, 0f, zc, w, dd, y, fh, rnd + i);
            if (i > 0)
            {
                int nb = Mathf.Max(3, Mathf.RoundToInt(w / 1.3f));
                for (int k = 0; k < nb; k++)
                {
                    float x = -w / 2f + 0.5f + k * (w - 1f) / (nb - 1);
                    b.Beam(Sl.Wood, P(x, y - 0.6f, d / 2f), P(x, y, d / 2f + jet), 0.2f, 0.2f);
                    b.Ball(Sl.Wood, P(x, y - 0.55f, d / 2f + 0.02f), 0.14f, 3, 5);
                }
            }
            // rangka kayu: muka depan + dua sisi
            b.Push(P(0f, y, zc + dd / 2f), 0f); Frame(b, w, 0f, fh, rnd + i, i == 0 && !shop ? 0 : 1); b.Pop();
            b.Push(P(w / 2f, y, zc), 90f); Frame(b, dd, 0f, fh, rnd + 3 + i, 1); b.Pop();
            b.Push(P(-w / 2f, y, zc), -90f); Frame(b, dd, 0f, fh, rnd + 5 + i, 1); b.Pop();
            b.Push(P(0f, y, zc - dd / 2f), 180f); Frame(b, w, 0f, fh, rnd + 7 + i, 1); b.Pop();
            y += fh;
        }
        float yTop = y;
        float dTop = d + (floors > 1 ? jet : 0f);
        float zTop = floors > 1 ? jet / 2f : 0f;
        float fz = d / 2f;                         // muka dinding lantai bawah
        float fzU = zTop + dTop / 2f;              // muka dinding lantai atas

        // ---------- pintu & jendela lantai bawah
        float doorX = 0f;                                   // pintu selalu di tengah: fasad simetris kiri-kanan
        float doorOff = longh ? w * 0.2f : 0f;              // rumah panjang: dua pintu simetris
        if (longh)
        {
            ArchDoor(b, P(-doorOff, plinth, fz), 1.3f, 2.4f, rnd);
            ArchDoor(b, P(doorOff, plinth, fz), 1.3f, 2.4f, rnd);
        }
        else ArchDoor(b, P(doorX, plinth, fz), cottage ? 1.1f : 1.3f, cottage ? 2.1f : 2.4f, rnd);
        if (shop)
        {
            // dua etalase lengkung kembar kiri-kanan pintu + kanopi bersisik
            float ww = w * 0.26f, gx = w / 2f - ww / 2f - 0.5f;
            foreach (float sg in new[] { -1f, 1f })
            {
                ArchWin(b, P(sg * gx, plinth + 0.35f, fz + 0.02f), ww, 2.3f, false, false);
                b.Box(Sl.Wood, P(sg * gx, plinth + 0.5f, fz + 0.35f), new Vector3(ww + 0.8f, 0.14f, 0.7f));
                for (int i = 0; i < 3; i++) b.Ball(i % 2 == 0 ? Sl.Cloth : Sl.Gold, P(sg * gx + (i - 1) * 0.5f, plinth + 0.78f, fz + 0.4f), 0.2f, 3, 6);
                BuildKit.Crate(b, P(sg * (w / 2f - 0.6f), 0f, fz + 1.3f), 0.8f, 12f);
                BuildKit.Barrel(b, P(sg * (w / 2f + 0.1f), 0f, fz + 0.9f));
            }
            BuildKit.Awning(b, P(0f, plinth + 2.95f, fz + 0.05f), w - 0.6f, 1.5f, 0.55f, Sl.Cloth, Sl.ClothB);
            BuildKit.HangingSign(b, P(0f, plinth + fh + 0.35f, fzU + 0.05f), 1.5f, rnd % 2 == 0 ? Sl.Gold : Sl.Cloth);
        }
        else if (longh)
        {
            ArchWin(b, P(0f, plinth + 0.85f, fz + 0.02f), 1.0f, 1.7f, true, true);
            float lx = w / 2f - 1.35f;
            foreach (float sg in new[] { -1f, 1f }) ArchWin(b, P(sg * lx, plinth + 0.85f, fz + 0.02f), 1.0f, 1.7f, false, true);
        }
        else
        {
            float wx = cottage ? w / 2f - 0.95f : w / 2f - 1.25f;
            if (wx >= (cottage ? 1.35f : 1.8f))
                foreach (float sg in new[] { -1f, 1f })
                {
                    if (cottage) BuildKit.Window(b, P(sg * wx, plinth + 1.2f, fz + 0.02f), 0.95f, 1.1f, true, true);
                    else ArchWin(b, P(sg * wx, plinth + 0.85f, fz + 0.02f), 1.0f, 1.7f, true, true);
                }
        }
        // lantai atas (semua simetris terhadap sumbu tengah)
        for (int i = 1; i < floors; i++)
        {
            float yy = plinth + i * fh;
            int n = w > 7.5f ? 3 : 2;
            for (int k = 0; k < n; k++)
            {
                float x = (k - (n - 1) / 2f) * (w - 2.4f) / Mathf.Max(1, n - 1);
                ArchWin(b, P(x, yy + 0.8f, fzU + 0.02f), 1.0f, 1.65f, true, i == 1 && !shop);
            }
            if (n == 2 && !shop && !narrow && Pick(rnd, 2, 3) == 0)
                Oriel(b, P(0f, yy + 0.75f, fzU), 1.8f, 1.7f, Sl.RoofB);                 // jendela menjorok di tengah antara dua jendela
            if (i == 1 && n == 3 && !shop && Pick(rnd, 3, 3) == 0)
                Balcony(b, P(0f, yy + 0.15f, fzU), 2.8f, 1.1f);
        }
        // sisi
        foreach (float sx in new[] { -1f, 1f })
            for (int i = 0; i < floors; i++)
            {
                b.Push(P(sx * w / 2f, plinth + i * fh, 0f), sx * 90f);
                BuildKit.Window(b, P(0f, fh * 0.55f, 0.02f), 1.0f, 1.3f, true, i == 0);
                b.Pop();
            }

        // ---------- atap
        bool alongX = w > d * 1.15f && !narrow;
        float span = alongX ? dTop : w;
        float rise = Mathf.Clamp(span * (cottage ? 0.78f : 0.7f), 2.8f, 7.2f);
        int roofSlot = cottage ? Sl.Thatch : Sl.Roof;
        float ov = cottage ? 1.0f : 0.85f;
        b.Gable(roofSlot, Sl.Plaster, Sl.Wood, P(0f, yTop, zTop), w, dTop, rise, ov, alongX);
        float total = yTop + rise;

        if (alongX)
        {
            // sayap melintang di tengah depan (cross-gable) untuk rumah panjang dan sebagian rumah kota
            bool cross = longh || (!shop && !cottage && Pick(rnd, 4, 2) == 0 && w > 8f);
            if (cross)
            {
                float cw = Mathf.Min(w * 0.4f, 4.6f);
                float cd = 1.4f;
                float cx = 0f;
                float riseC = rise * 0.78f;
                b.BoxB(Sl.Plaster, P(cx, yTop - fh, zTop + dTop / 2f + cd / 2f - 0.05f), new Vector3(cw, fh, cd + 0.1f));
                b.Push(P(cx, yTop - fh, zTop + dTop / 2f + cd), 0f); Frame(b, cw, 0f, fh, rnd, 1); b.Pop();
                b.Gable(roofSlot, Sl.Plaster, Sl.Wood, P(cx, yTop, zTop + dTop / 2f + cd / 2f - 0.05f), cw, cd + 0.1f, riseC, ov * 0.9f, false);
                ArchWin(b, P(cx, yTop - fh + 0.8f, zTop + dTop / 2f + cd + 0.02f), 1.0f, 1.65f, true, true);
                RoundWindowFront(b, P(cx, yTop + riseC * 0.4f, zTop + dTop / 2f + cd + 0.03f), 0.42f);
            }
            int nd = w > 8.5f ? 2 : 1;
            for (int k = 0; k < nd; k++)
            {
                float x = nd == 1 ? 0f : (k == 0 ? -w * 0.28f : w * 0.28f);
                if (cross && Mathf.Abs(x) < 3.2f) continue;
                float z0 = zTop + dTop * 0.2f;
                float yb = yTop + rise * (1f - (z0 - zTop) / (dTop / 2f)) - 0.9f;
                b.Push(P(x, 0f, z0), 0f);
                BuildKit.Dormer(b, P(0f, yb, 0f), 1.7f, 2.2f, Sl.RoofB, Sl.Plaster);
                b.Pop();
            }
        }
        else
        {
            float zf = zTop + dTop / 2f + 0.03f, hw = w / 2f;
            // rangka & hiasan pada segitiga gable depan
            b.Beam(Sl.Wood, P(-hw, yTop, zf), P(0f, yTop + rise, zf), 0.24f, 0.2f, Vector3.forward);
            b.Beam(Sl.Wood, P(hw, yTop, zf), P(0f, yTop + rise, zf), 0.24f, 0.2f, Vector3.forward);
            b.Box(Sl.Wood, P(0f, yTop + rise * 0.38f, zf + 0.04f), new Vector3(w * 0.52f, 0.16f, 0.16f));
            b.Box(Sl.Wood, P(0f, yTop + rise * 0.5f, zf + 0.04f), new Vector3(0.16f, rise * 0.95f, 0.16f));
            RoundWindowFront(b, P(0f, yTop + rise * 0.22f, zf + 0.02f), 0.5f);
            AnimeKit.PennantTop(b, P(0f, yTop + rise + 0.1f, zTop), rnd % 2 == 0 ? Sl.Cloth : Sl.ClothB);
            total += 1.8f;
        }

        // ---------- cerobong bulat di tengah bubungan (simetris)
        float chx = 0f;
        float chz = alongX ? zTop : zTop - dTop * 0.2f;
        RoundChimney(b, P(chx, yTop - 0.4f, chz), 0.55f, rise + 1.5f);

        // ---------- varian kind
        if (tower)
        {
            float tr = 1.6f;
            foreach (float sg in new[] { -1f, 1f })
            {
                float tt = Tower(b, sg * (w / 2f + 0.4f), d / 2f - 0.3f, tr, floors + 1, fh, Sl.Plaster, Sl.RoofB, 5.2f, rnd, true);
                total = Mathf.Max(total, tt);
                b.Push(P(sg * (w / 2f + 0.4f), tt - 1.0f, d / 2f - 0.3f), 0f);
                BuildKit.Flag(b, P(0f, 0f, 0f), 2.6f, 1.8f, 1.1f, Sl.Cloth);
                b.Pop();
            }
            total += 3f;
        }
        if (cottage)
        {
            // dua sayap dapur beratap rendah kiri-kanan (simetris) + kebun
            float lw = 2.6f, ld = d * 0.6f;
            foreach (float sg in new[] { -1f, 1f })
            {
                float lc = sg * (w / 2f + lw / 2f - 0.1f);
                b.BoxB(Sl.Stone, P(lc, 0f, -d * 0.1f), new Vector3(lw, 0.6f, ld));
                b.BoxB(Sl.Plaster, P(lc, 0.6f, -d * 0.1f), new Vector3(lw, 2.2f, ld));
                b.Gable(Sl.RoofB, Sl.Plaster, Sl.Wood, P(lc, 2.8f, -d * 0.1f), lw, ld, 1.5f, 0.55f, false);
                BuildKit.Window(b, P(lc, 1.7f, -d * 0.1f + ld / 2f + 0.02f), 0.8f, 0.9f, true, true);
                Woodpile(b, P(sg * (w / 2f + lw / 2f), 0f, -d * 0.1f + ld / 2f + 0.8f), 0f);
            }
            Picket(b, -w / 2f - lw - 0.8f, w / 2f + lw + 0.8f, fz + 2.8f, 0.8f);
        }
        if (longh)
        {
            foreach (float sg in new[] { -1f, 1f })
            {
                BuildKit.Barrel(b, P(sg * (w / 2f - 0.8f), 0f, fz + 1.2f));
                BuildKit.Crate(b, P(sg * (w / 2f - 2.0f), 0f, fz + 1.1f), 0.9f, 10f);
            }
        }
        if (!cottage && !shop && !longh)
        {
            Porch(b, doorX, fz, 2.8f, 1.5f, 0.2f, plinth + 3.0f, Sl.RoofB, rnd);
        }

        // ---------- hiasan umum
        HouseDetail(b, w, d, plinth, fh, floors, doorX, fz, fzU, yTop, rnd, shop, cottage);
        int bushSlot = rnd % 2 == 0 ? Sl.Blossom : Sl.Gold;
        AnimeKit.FlowerBush(b, P(-w / 2f + 0.5f, 0f, fz + 0.95f), 0.6f, bushSlot);
        AnimeKit.FlowerBush(b, P(w / 2f - 0.5f, 0f, fz + 0.95f), 0.6f, bushSlot);
        if (rnd % 3 != 1) foreach (float sg in new[] { -1f, 1f }) AnimeKit.Ivy(b, P(sg * (w / 2f - 0.4f), plinth, fzU + 0.03f), fh * floors * 0.85f, 1.5f, rnd);
        if (!cottage && !shop)
            AnimeKit.Bunting(b, P(-w / 2f + 0.3f, yTop - 0.45f, fzU + 0.5f), P(w / 2f - 0.3f, yTop - 0.45f, fzU + 0.5f), 0.45f, Mathf.Max(4, Mathf.RoundToInt(w / 1.2f)), rnd);
        // lumut di atap: pasangan simetris kiri-kanan
        for (int i = 0; i < 2; i++)
        {
            float sfrac = 0.16f + 0.26f * i;
            foreach (float sg in new[] { -1f, 1f })
            {
                float xx, zz, yy2;
                if (alongX) { xx = sg * w * (0.3f - 0.1f * i); zz = zTop + dTop / 2f * (1f - sfrac); yy2 = yTop + rise * sfrac + 0.12f; }
                else { xx = sg * w * 0.5f * (0.25f + sfrac); zz = zTop + dTop * (-0.15f + 0.3f * i); yy2 = yTop + rise * (1f - Mathf.Abs(xx) / (w / 2f)) + 0.12f; }
                Moss(b, P(xx, yy2, zz), 0.28f, rnd + i);
            }
        }
        return total;
    }

    static void RoundWindowFront(GeoBag b, Vector3 c, float r)
    {
        AnimeKit.RoundWindow(b, c, r);
    }
}
