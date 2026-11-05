using UnityEngine;

public static partial class FantasyKit
{
    /// <summary>Pipa talang vertikal dengan corong, braket, dan tong penampung air hujan.</summary>
    public static void DrainPipe(GeoBag b, Vector3 top, float bottomY, bool barrel)
    {
        float h = top.y - bottomY;
        if (h < 1f) return;
        b.Cone(Sl.Metal, top + P(0f, -0.35f, 0f), 0.2f, 0.4f, 8);
        b.Cyl(Sl.Metal, P(top.x, bottomY, top.z), 0.065f, h - 0.3f, 6, 0.065f, true);
        int n = Mathf.Max(2, Mathf.RoundToInt(h / 1.4f));
        for (int i = 0; i < n; i++)
            b.Cyl(Sl.DarkStone, P(top.x, bottomY + 0.5f + i * (h - 1f) / n, top.z), 0.1f, 0.08f, 6, 0.1f, true);
        b.Beam(Sl.Metal, P(top.x, bottomY + 0.35f, top.z), P(top.x, bottomY + 0.12f, top.z + 0.4f), 0.07f, 0.07f);
        if (barrel) BuildKit.Barrel(b, P(top.x, 0f, top.z + 0.75f));
    }

    /// <summary>Tambalan plester yang mengelupas memperlihatkan susunan bata. Menghadap +Z, c = tengah.</summary>
    public static void BrickPatch(GeoBag b, Vector3 c, float w, float h, int rnd)
    {
        b.Box(Sl.DarkStone, c + P(0f, 0f, 0.03f), P(w, h, 0.05f));
        int rows = Mathf.Max(2, Mathf.RoundToInt(h / 0.2f));
        for (int r = 0; r < rows; r++)
        {
            float y = -h / 2f + (r + 0.5f) * h / rows;
            float off = (r % 2) * 0.17f;
            int cols = Mathf.Max(2, Mathf.RoundToInt(w / 0.34f));
            for (int k = 0; k < cols; k++)
            {
                float x = -w / 2f + 0.17f + off + k * 0.34f;
                if (x > w / 2f - 0.1f) continue;
                if (((rnd >> ((r * 5 + k) % 20)) & 7) == 0) continue;               // bata hilang
                b.Box(((r + k + rnd) % 4 == 0) ? Sl.Stone : Sl.Cobble, c + P(x, y, 0.07f), P(0.3f, h / rows - 0.03f, 0.07f));
            }
        }
        // tepi plester yang retak: potongan kecil
        b.Box(Sl.PlasterB, c + P(-w / 2f - 0.05f, h * 0.2f, 0.03f), P(0.12f, h * 0.5f, 0.06f), 8f);
        b.Box(Sl.PlasterB, c + P(w / 2f + 0.05f, -h * 0.2f, 0.03f), P(0.12f, h * 0.4f, 0.06f), -6f);
    }

    /// <summary>Tali jemuran dengan kain berkibar. a dan c = dua ujung tali.</summary>
    public static void Laundry(GeoBag b, Vector3 a, Vector3 c, int rnd)
    {
        b.Beam(Sl.WoodLight, a, c, 0.03f, 0.03f);
        int n = 3;
        for (int i = 0; i < n; i++)
        {
            float t = (i + 0.8f) / (n + 0.6f);
            Vector3 p = Vector3.Lerp(a, c, t) + P(0f, -0.04f, 0f);
            float cw = 0.42f + 0.16f * R01(rnd, i);
            Vector3 dir = (c - a).normalized;
            int sl = (rnd + i) % 3 == 0 ? Sl.Cloth : ((rnd + i) % 3 == 1 ? Sl.ClothB : Sl.Plaster);
            b.QuadBoth(sl, p - dir * cw / 2f, p + dir * cw / 2f, p + dir * cw / 2f + P(0f, -0.7f, 0.06f), p - dir * cw / 2f + P(0f, -0.7f, 0.1f));
        }
    }

    /// <summary>Tangga kayu bersandar di dinding menghadap +Z. base = pangkal bawah (di tanah), top lebih dekat ke dinding.</summary>
    public static void Ladder(GeoBag b, Vector3 baseP, float h, float lean)
    {
        for (int s = -1; s <= 1; s += 2)
            b.Beam(Sl.Wood, baseP + P(s * 0.28f, 0f, lean), baseP + P(s * 0.28f, h, 0f), 0.08f, 0.06f);
        int n = Mathf.RoundToInt(h / 0.38f);
        for (int i = 1; i < n; i++)
        {
            float t = i / (float)n;
            b.Beam(Sl.WoodLight, baseP + P(-0.28f, h * t, lean * (1f - t)), baseP + P(0.28f, h * t, lean * (1f - t)), 0.05f, 0.05f);
        }
    }

    /// <summary>Jalan setapak batu pijakan dari pintu ke luar. start = titik di depan pintu, arah +Z.</summary>
    public static void StepStones(GeoBag b, Vector3 start, int n, int rnd)
    {
        for (int i = 0; i < n; i++)
        {
            float x = start.x;                                    // lurus di tengah (simetris)
            float z = start.z + i * 0.95f;
            float r = 0.38f + 0.14f * R01(rnd, i * 3 + 1);
            b.Cyl(Sl.Stone, P(x, start.y, z), r, 0.1f, 7, r * 0.92f, true);
        }
    }

    /// <summary>Pot bunga gantung dari braket besi di dinding. wallPt di muka dinding.</summary>
    public static void HangPot(GeoBag b, Vector3 wallPt, int flowerSlot)
    {
        b.Beam(Sl.Metal, wallPt, wallPt + P(0f, 0f, 0.55f), 0.05f, 0.05f);
        b.Beam(Sl.Metal, wallPt + P(0f, -0.4f, 0f), wallPt + P(0f, 0f, 0.4f), 0.04f, 0.04f);
        Vector3 e = wallPt + P(0f, -0.1f, 0.55f);
        b.Beam(Sl.Metal, e, e + P(0f, -0.3f, 0f), 0.02f, 0.02f);
        b.Cyl(Sl.WoodLight, e + P(0f, -0.58f, 0f), 0.2f, 0.28f, 8, 0.14f, true);
        b.Ball(Sl.Hedge, e + P(0f, -0.28f, 0f), 0.22f, 3, 6, 0.7f);
        b.Ball(flowerSlot, e + P(0.07f, -0.2f, 0.08f), 0.09f, 3, 5);
        b.Ball(flowerSlot, e + P(-0.1f, -0.22f, -0.05f), 0.08f, 3, 5);
    }

    /// <summary>Balok katrol (hoist) di loteng dengan tali dan peti. top = pangkal di dinding.</summary>
    public static void Hoist(GeoBag b, Vector3 wallPt)
    {
        b.Beam(Sl.Wood, wallPt, wallPt + P(0f, 0f, 1.2f), 0.16f, 0.16f);
        b.Beam(Sl.Wood, wallPt + P(0f, -0.7f, 0f), wallPt + P(0f, -0.1f, 1.0f), 0.1f, 0.1f);
        b.Ball(Sl.Metal, wallPt + P(0f, -0.14f, 1.15f), 0.1f, 3, 6);
        b.Beam(Sl.WoodLight, wallPt + P(0f, -0.2f, 1.15f), wallPt + P(0f, -1.3f, 1.15f), 0.02f, 0.02f);
        BuildKit.Crate(b, wallPt + P(0f, -1.85f, 1.15f), 0.55f, 20f);
    }

    /// <summary>Bundel rempah/gandum bergantung di dinding atau teras.</summary>
    public static void HerbBundle(GeoBag b, Vector3 p, int rnd)
    {
        b.Beam(Sl.WoodLight, p, p + P(0f, -0.2f, 0f), 0.02f, 0.02f);
        b.Cone(Sl.Thatch, p + P(0f, -0.62f, 0f), 0.04f, 0.42f, 5);
        b.Cyl(Sl.Thatch, p + P(0f, -0.62f, 0f), 0.12f, 0.4f, 5, 0.02f, true);
        b.Ball((rnd & 1) == 0 ? Sl.Cloth : Sl.Gold, p + P(0f, -0.22f, 0f), 0.06f, 3, 5);
    }

    /// <summary>Hiasan umum untuk satu rumah. Semua dalam koordinat rumah (depan +Z).</summary>
    public static void HouseDetail(GeoBag b, float w, float d, float plinth, float fh, int floors, float doorX, float fz, float fzU, float yTop, int rnd, bool shop, bool cottage)
    {
        // semua hiasan dibuat simetris (kembar kiri-kanan) supaya fasad tidak terlihat mencong
        foreach (float sx in new[] { -1f, 1f })
        {
            DrainPipe(b, P(sx * (w / 2f + 0.15f), yTop - 0.15f, fzU + 0.12f), 0.35f, true);
            b.Push(P(sx * (w / 2f), plinth + 0.9f, d * 0.2f), sx * 90f);
            BrickPatch(b, P(0f, 0.5f, 0f), 1.5f, 1.1f, rnd);
            b.Pop();
            if (floors > 1)
            {
                b.Push(P(sx * (w / 2f), 0f, 0f), sx * 90f);
                Laundry(b, P(-d * 0.35f, plinth + fh + 1.9f, 0.12f), P(d * 0.35f, plinth + fh + 1.4f, 0.12f), rnd);
                b.Pop();
            }
            if (!shop)
            {
                HangPot(b, P(doorX + sx * 1.15f, plinth + 2.6f, fz + 0.04f), rnd % 2 == 0 ? Sl.Blossom : Sl.Cloth);
                HerbBundle(b, P(doorX + sx * 1.3f, plinth + 2.7f, fz + 0.2f), rnd);
            }
        }
        if (floors > 1 && !cottage && w <= 7.5f && !shop) Hoist(b, P(0f, yTop - 0.8f, fzU + 0.04f));
        StepStones(b, P(doorX, 0.04f, fz + 0.9f + 0.38f * Mathf.Ceil(plinth / 0.17f)), 4, rnd);
    }
}
