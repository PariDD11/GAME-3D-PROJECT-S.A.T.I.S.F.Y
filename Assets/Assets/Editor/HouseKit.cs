using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Arketipe rumah warga bergaya kota abad pertengahan fantasi (setengah kayu, lantai menjorok, atap curam, lukarne, cerobong, kotak bunga).
/// Tiap arketipe = satu mesh; warna dinding/atap divariasikan lewat material saat dipasang. Sisi depan (pintu) = +Z lokal.
/// </summary>
public static class HouseKit
{
    public class HouseDef
    {
        public string key;
        public int kind;           // 0 townhouse, 1 toko, 2 sempit, 3 pondok jerami, 4 menara sudut, 5 rumah panjang
        public float w, d, height;
        public PropKit.Proto proto;
    }

    public static readonly List<HouseDef> Defs = new List<HouseDef>();

    public static void BuildAll()
    {
        Defs.Clear();
        Add(0, 6.5f, 7.5f, 2); Add(0, 7.5f, 8f, 2); Add(0, 8.5f, 7.5f, 3); Add(0, 6.2f, 8f, 3); Add(0, 9f, 8f, 2);
        Add(1, 7f, 8f, 2); Add(1, 8f, 8f, 3); Add(1, 9f, 9f, 2);
        Add(2, 5.2f, 7f, 3); Add(2, 5f, 7.5f, 4); Add(2, 5.6f, 7f, 3);
        Add(3, 5.5f, 6.5f, 1); Add(3, 6.5f, 7f, 1); Add(3, 5f, 5.5f, 1);
        Add(4, 7f, 8f, 2); Add(4, 8f, 8f, 3);
        Add(5, 12f, 7.5f, 2); Add(5, 11f, 7f, 3);
    }

    static void Add(int kind, float w, float d, int floors)
    {
        string key = "House_" + kind + "_" + Mathf.RoundToInt(w * 10f) + "_" + Mathf.RoundToInt(d * 10f) + "_" + floors;
        var def = new HouseDef { key = key, kind = kind, w = w, d = d };
        float h = 0f;
        def.proto = PropKit.Get(key, b => { h = Build(b, kind, w, d, floors, key.GetHashCode()); });
        def.height = h > 0f ? h : floors * 3.2f + 5f;
        Defs.Add(def);
    }

    // ------------------------------------------------------------------ pembangun
    const float FH = 3.2f;

    static float Build(GeoBag b, int kind, float w, float d, int floors, int hash)
    {
        return FantasyKit.House(b, kind, w, d, floors, hash);
    }

    static float BuildOld(GeoBag b, int kind, float w, float d, int floors, int hash)
    {
        bool shop = kind == 1, cottage = kind == 3;
        float jet = cottage ? 0f : 0.55f;
        float plinth = 0.6f;
        int rnd = Mathf.Abs(hash);

        // ---- badan: alas batu, lantai bertumpuk dengan lantai atas menjorok ke depan
        b.BoxB(Sl.Stone, Vector3.zero, new Vector3(w + 0.3f, plinth, d + 0.3f));
        float y = plinth;
        float fh = cottage ? 2.9f : FH;
        for (int i = 0; i < floors; i++)
        {
            float dd = d + (i > 0 ? jet : 0f);
            float zc = i > 0 ? jet / 2f : 0f;
            b.BoxB(Sl.Plaster, new Vector3(0f, y, zc), new Vector3(w, fh, dd));
            if (i > 0)
            {
                // penopang di bawah lantai menjorok
                int nb = Mathf.Max(3, Mathf.RoundToInt(w / 1.4f));
                for (int k = 0; k < nb; k++)
                {
                    float x = -w / 2f + 0.4f + k * (w - 0.8f) / (nb - 1);
                    b.Beam(Sl.Wood, new Vector3(x, y - 0.55f, d / 2f), new Vector3(x, y, d / 2f + jet), 0.2f, 0.2f);
                }
                b.Box(Sl.Wood, new Vector3(0f, y - 0.1f, d / 2f + jet), new Vector3(w + 0.1f, 0.22f, 0.22f));
            }
            if (!cottage)
            {
                b.Push(new Vector3(0f, y, d / 2f + (i > 0 ? jet : 0f)), 0f);
                BuildKit.TimberFront(b, w, 0f, fh, 0f, i > 0 && (rnd + i) % 2 == 0);
                b.Pop();
                b.Push(new Vector3(w / 2f, y, zc), 90f);
                BuildKit.TimberFront(b, dd, 0f, fh, 0f, true);
                BuildKit.Window(b, new Vector3(0f, fh * 0.55f, 0.02f), 1.1f, 1.5f, true, i == 0);
                b.Pop();
                b.Push(new Vector3(-w / 2f, y, zc), -90f);
                BuildKit.TimberFront(b, dd, 0f, fh, 0f, true);
                BuildKit.Window(b, new Vector3(0f, fh * 0.55f, 0.02f), 1.1f, 1.5f, true, false);
                b.Pop();
            }
            else
            {
                b.Push(new Vector3(w / 2f, y, zc), 90f);
                BuildKit.Window(b, new Vector3(0f, fh * 0.55f, 0.02f), 0.9f, 1.2f, true, false);
                b.Pop();
                b.Push(new Vector3(-w / 2f, y, zc), -90f);
                BuildKit.Window(b, new Vector3(0f, fh * 0.55f, 0.02f), 0.9f, 1.2f, true, false);
                b.Pop();
            }
            y += fh;
        }
        float yTop = y;
        float dTop = d + (floors > 1 ? jet : 0f);
        float zTop = floors > 1 ? jet / 2f : 0f;

        // ---- jendela dan pintu muka
        float fz = d / 2f;
        b.Stairs(Sl.Stone, new Vector3(0f, 0f, fz + 0.2f + 3 * 0.4f - 0.2f), 2.1f, 3, 0.2f, 0.4f, 0f);
        if (shop)
        {
            // etalase besar kiri + pintu kanan
            float gx = -w * 0.2f;
            b.Box(Sl.Wood, new Vector3(gx, plinth + 1.45f, fz + 0.06f), new Vector3(w * 0.5f, 1.9f, 0.14f));
            b.Quad(Sl.Glass, new Vector3(gx - w * 0.22f, plinth + 0.7f, fz + 0.14f), new Vector3(gx + w * 0.22f, plinth + 0.7f, fz + 0.14f),
                             new Vector3(gx + w * 0.22f, plinth + 2.1f, fz + 0.14f), new Vector3(gx - w * 0.22f, plinth + 2.1f, fz + 0.14f));
            b.Box(Sl.Wood, new Vector3(gx, plinth + 1.4f, fz + 0.18f), new Vector3(0.08f, 1.5f, 0.05f));
            b.Box(Sl.Stone, new Vector3(gx, plinth + 0.5f, fz + 0.2f), new Vector3(w * 0.5f + 0.2f, 0.22f, 0.4f));
            BuildKit.DoorFront(b, new Vector3(w * 0.28f, plinth, fz), 1.3f, 2.3f, true);
            BuildKit.Awning(b, new Vector3(0f, plinth + 3.0f, fz + 0.05f), w - 0.5f, 1.6f, 0.65f, Sl.Cloth, Sl.ClothB);
            BuildKit.HangingSign(b, new Vector3(w / 2f - 0.1f, plinth + fh + 1.4f, fz + jet + 0.05f), 1.6f, rnd % 2 == 0 ? Sl.Gold : Sl.Cloth);
        }
        else
        {
            BuildKit.DoorFront(b, new Vector3(0f, plinth, fz), cottage ? 1.1f : 1.3f, cottage ? 2.0f : 2.3f, !cottage);
            float wx = w * 0.3f;
            BuildKit.Window(b, new Vector3(-wx - 0.3f, plinth + fh * 0.55f, fz + 0.02f), 1.2f, 1.5f, true, true);
            BuildKit.Window(b, new Vector3(wx + 0.3f, plinth + fh * 0.55f, fz + 0.02f), 1.2f, 1.5f, true, true);
        }
        for (int i = 1; i < floors; i++)
        {
            float yy = plinth + i * fh + fh * 0.55f;
            int n = w > 7.5f ? 3 : 2;
            for (int k = 0; k < n; k++)
            {
                float x = (k - (n - 1) / 2f) * (w - 2.2f) / Mathf.Max(1, n - 1);
                if (n == 1) x = 0f;
                BuildKit.Window(b, new Vector3(x, yy, fz + jet + 0.02f), 1.15f, 1.5f, true, i == 1 && !shop);
            }
        }

        // ---- atap
        bool alongX = w > d * 1.15f;
        float span = alongX ? dTop : w;
        float rise = Mathf.Clamp(span * (cottage ? 0.62f : 0.6f), 2.4f, 6.4f);
        int roofSlot = cottage ? Sl.Thatch : Sl.Roof;
        float ov = cottage ? 0.85f : 0.7f;
        b.Gable(roofSlot, Sl.Plaster, Sl.Wood, new Vector3(0f, yTop, zTop), w, dTop, rise, ov, alongX);
        float total = yTop + rise;

        if (alongX)
        {
            // lukarne di lereng depan
            int nd = w > 8.5f ? 2 : 1;
            for (int k = 0; k < nd; k++)
            {
                float x = nd == 1 ? 0f : (k == 0 ? -w * 0.24f : w * 0.24f);
                float z0 = zTop + dTop * 0.22f;
                float yb = yTop + rise * (1f - (z0 - zTop) / (dTop / 2f)) - 0.8f;
                if (floors >= 2 || !cottage)
                {
                    b.Push(new Vector3(x, 0f, z0), 0f);
                    BuildKit.Dormer(b, new Vector3(0f, yb, 0f), 1.7f, 1.5f + 0.8f, roofSlot == Sl.Thatch ? Sl.Thatch : Sl.RoofB, Sl.Plaster);
                    b.Pop();
                }
            }
        }
        else
        {
            // gable muka: rangka kayu segitiga + jendela + hiasan puncak
            float zf = zTop + dTop / 2f + 0.02f;
            float hw = w / 2f;
            b.Beam(Sl.Wood, new Vector3(-hw, yTop, zf), new Vector3(0f, yTop + rise, zf), 0.22f, 0.2f, Vector3.forward);
            b.Beam(Sl.Wood, new Vector3(hw, yTop, zf), new Vector3(0f, yTop + rise, zf), 0.22f, 0.2f, Vector3.forward);
            b.Box(Sl.Wood, new Vector3(0f, yTop + 0.1f, zf + 0.05f), new Vector3(w, 0.22f, 0.2f));
            b.Box(Sl.Wood, new Vector3(0f, yTop + rise * 0.42f, zf + 0.05f), new Vector3(w * 0.5f, 0.16f, 0.18f));
            b.Box(Sl.Wood, new Vector3(0f, yTop + rise * 0.5f, zf + 0.05f), new Vector3(0.18f, rise * 0.9f, 0.18f));
            BuildKit.Window(b, new Vector3(0f, yTop + rise * 0.22f, zf + 0.02f), 0.9f, 1.1f, true, false);
            BuildKit.Finial(b, new Vector3(0f, yTop + rise - 0.1f, zf), 1.6f, kind == 2 ? Sl.Gold : Sl.Metal);
        }

        // ---- cerobong
        float cx = (rnd % 2 == 0 ? -1f : 1f) * (w / 2f - 1.0f);
        BuildKit.Chimney(b, new Vector3(cx, yTop - 1.0f, alongX ? -0.2f : -dTop * 0.15f), 0.9f, rise + 1.9f);

        // ---- hiasan anime fantasi
        if (!cottage)
        {
            float ez = fz + (floors > 1 ? jet : 0f) + 0.45f;
            AnimeKit.Bunting(b, new Vector3(-w / 2f + 0.3f, yTop - 0.35f, ez), new Vector3(w / 2f - 0.3f, yTop - 0.35f, ez), 0.45f, Mathf.Max(4, Mathf.RoundToInt(w / 1.2f)), rnd);
            if (!shop) AnimeKit.HangLantern(b, new Vector3(-1.2f, plinth + 2.8f, fz + 0.05f), 0.7f);
        }
        AnimeKit.FlowerBush(b, new Vector3(-w / 2f + 0.5f, 0f, fz + 0.9f), 0.65f, rnd % 2 == 0 ? Sl.Blossom : Sl.Gold);
        AnimeKit.FlowerBush(b, new Vector3(w / 2f - 0.5f, 0f, fz + 0.9f), 0.55f, rnd % 3 == 0 ? Sl.Cloth : Sl.Blossom);
        if (rnd % 3 != 1) AnimeKit.Ivy(b, new Vector3(w / 2f - 0.4f, plinth, fz + (floors > 1 ? jet : 0f) + 0.02f), fh * floors * 0.8f, 1.4f, rnd);
        if (!alongX && !cottage) AnimeKit.RoundWindow(b, new Vector3(0f, yTop + rise * 0.62f, zTop + dTop / 2f + 0.03f), 0.38f);
        AnimeKit.PennantTop(b, new Vector3(0f, yTop + rise + 0.1f, zTop), rnd % 2 == 0 ? Sl.Cloth : Sl.ClothB);
        total += 1.8f;

        // ---- varian khusus
        if (kind == 4)
        {
            // menara sudut bulat dengan atap kerucut kecil
            float tr = 1.7f;
            Vector3 tp = new Vector3(-w / 2f - 0.1f, 0f, d / 2f - 0.2f);
            float th = yTop + 1.2f;
            b.Cyl(Sl.Stone, tp, tr + 0.25f, 1.0f, 12, tr + 0.05f, true);
            b.Cyl(Sl.Plaster, tp + new Vector3(0f, 1.0f, 0f), tr, th - 1.0f, 12, tr, false);
            b.Cyl(Sl.Wood, tp + new Vector3(0f, th * 0.5f, 0f), tr + 0.08f, 0.2f, 12, tr + 0.08f, true);
            b.Cyl(Sl.RoofB, tp + new Vector3(0f, th, 0f), tr + 0.7f, 0.4f, 12, tr + 0.3f, false);
            b.Spire(Sl.RoofB, tp + new Vector3(0f, th + 0.3f, 0f), tr + 0.3f, 5.2f, 12, 0f);
            BuildKit.Finial(b, tp + new Vector3(0f, th + 5.3f, 0f), 1.6f, Sl.Gold);
            for (int k = 0; k < floors; k++)
            {
                float a = 0.55f;
                Vector3 wp = tp + new Vector3(Mathf.Sin(a) * tr, plinth + k * fh + fh * 0.55f, Mathf.Cos(a) * tr);
                b.Push(wp, a * Mathf.Rad2Deg);
                BuildKit.Window(b, Vector3.zero, 0.7f, 1.2f, false, false);
                b.Pop();
            }
            total = Mathf.Max(total, th + 5.5f);
        }
        if (kind == 2 || kind == 0)
        {
            // lentera dekat pintu
            b.Beam(Sl.Metal, new Vector3(1.2f, plinth + 2.7f, fz + 0.05f), new Vector3(1.2f, plinth + 2.7f, fz + 0.6f), 0.07f, 0.07f);
            BuildKit.Lantern(b, new Vector3(1.2f, plinth + 2.45f, fz + 0.6f));
        }
        if (kind == 5)
        {
            // pintu kedua + tong
            BuildKit.DoorFront(b, new Vector3(w * 0.3f, plinth, fz), 1.3f, 2.3f, true);
            BuildKit.Barrel(b, new Vector3(-w / 2f + 0.8f, 0f, fz + 1.0f));
            BuildKit.Crate(b, new Vector3(-w / 2f + 2.0f, 0f, fz + 0.9f), 0.9f, 10f);
        }
        return total;
    }
}
