using System.Collections.Generic;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Dua ribu empat ratus goresan 3+3 di dinding Kantor Pendaki (kanon: "tiga goresan pendek sejajar, jeda, tiga lagi. Tok, tok, tok.
/// Mereka tidak mengetuk. Mereka menghitung."). Tiap kelompok = enam garis tipis (tiga, jeda, tiga) pada permukaan batu, sedikit miring dan
/// berbeda ukuran seperti dibuat tangan yang berbeda. Jumlah akhir TEPAT EcGoresanTotal (dihitung kode, dibagi menurut luas tiap permukaan).
/// Semua kuad menghadap keluar dinding, tanpa collider. Deterministik (EcHash), tanpa UnityEngine.Random.
/// </summary>
public static partial class TownSceneBuilder
{
    public const int EcGoresanTotal = 2400;
    static int ecGoresanDrawn;

    struct EcRect
    {
        public float x0, y0, x1, y1;
        public EcRect(float a, float b, float c, float d) { x0 = a; y0 = b; x1 = c; y1 = d; }
    }

    /// <summary>Permukaan dinding: o = titik kiri pada y = 0, u = arah mendatar ke kanan, n = normal keluar. Koordinat permukaan (x, y) dalam meter.</summary>
    sealed class EcSurf
    {
        public Vector3 o, u, n;
        public float w, h;
        public List<EcRect> skip = new List<EcRect>();
        public List<Vector2> cells = new List<Vector2>();
        public int quota;
        public EcSurf(Vector3 left, Vector3 right, Vector3 normal, float height)
        {
            Vector3 d = right - left; d.y = 0f;
            w = d.magnitude; u = d / w; o = left; n = normal; h = height;
        }
    }

    static void EcCells(EcSurf s)
    {
        const float dx = 0.42f, dy = 0.25f;
        int row = 0;
        for (float y = 0.6f; y <= s.h - 0.3f; y += dy, row++)
            for (float x = 0.4f + ((row & 1) == 0 ? 0f : dx * 0.5f); x <= s.w - 0.4f; x += dx)
            {
                bool bad = false;
                for (int i = 0; i < s.skip.Count; i++)
                {
                    EcRect r = s.skip[i];
                    if (x + 0.27f > r.x0 && x - 0.27f < r.x1 && y + 0.16f > r.y0 && y - 0.16f < r.y1) { bad = true; break; }
                }
                if (!bad) s.cells.Add(new Vector2(x, y));
            }
    }

    static Vector3 EcPt(EcSurf s, float cx, float cy, float px, float py, float c, float sn)
    {
        float x = cx + px * c - py * sn, y = cy + px * sn + py * c;
        return s.o + s.u * x + Vector3.up * y + s.n * 0.016f;
    }

    /// <summary>Satu kelompok 3+3: enam goresan vertikal tipis (tiga, jeda, tiga), miring +-6 derajat, 85-115% ukuran.</summary>
    static void EcGroup(GeoBag b, EcSurf s, float cx, float cy, int seed)
    {
        float th = (EcHash(seed, 1) - 0.5f) * 12f * Mathf.Deg2Rad;
        float sc = 0.85f + 0.3f * EcHash(seed, 2);
        cx += (EcHash(seed, 3) - 0.5f) * 0.08f;
        cy += (EcHash(seed, 4) - 0.5f) * 0.05f;
        float c = Mathf.Cos(th), sn = Mathf.Sin(th);
        for (int k = 0; k < 6; k++)
        {
            float sx = (k < 3 ? -0.16f + 0.045f * k : 0.07f + 0.045f * (k - 3)) * sc;       // -0,16 -0,115 -0,07 | jeda | 0,07 0,115 0,16
            float hh = 0.065f * sc * (0.75f + 0.5f * EcHash(seed, 10 + k));
            const float hwid = 0.009f;
            EcQ(b, Sl.PlasterB,
                EcPt(s, cx, cy, sx - hwid, -hh, c, sn), EcPt(s, cx, cy, sx + hwid, -hh, c, sn),
                EcPt(s, cx, cy, sx + hwid, hh, c, sn), EcPt(s, cx, cy, sx - hwid, hh, c, sn), s.n);
        }
    }

    static void EcGoresan(Transform model)
    {
        float hw = KpW / 2f, hd = KpD / 2f, pw = KpPW / 2f;
        float hTop = KpH - 0.2f;
        var surfs = new List<EcSurf>();

        // 1-2. dinding lorong (menghadap ke tengah lorong), tinggi sampai langit-langit
        surfs.Add(new EcSurf(new Vector3(-pw, 0f, hd), new Vector3(-pw, 0f, -hd), Vector3.right, KpPH - 0.15f));
        surfs.Add(new EcSurf(new Vector3(pw, 0f, -hd), new Vector3(pw, 0f, hd), -Vector3.right, KpPH - 0.15f));

        // 3. muka utara (-Z), 4-5. ujung timur dan barat, 6. muka selatan (+Z)
        var north = new EcSurf(new Vector3(-hw, 0f, -hd), new Vector3(hw, 0f, -hd), -Vector3.forward, hTop);
        var east = new EcSurf(new Vector3(hw, 0f, hd), new Vector3(hw, 0f, -hd), Vector3.right, hTop);
        var west = new EcSurf(new Vector3(-hw, 0f, -hd), new Vector3(-hw, 0f, hd), -Vector3.right, hTop);
        var south = new EcSurf(new Vector3(-hw, 0f, hd), new Vector3(hw, 0f, hd), Vector3.forward, hTop);
        surfs.Add(north); surfs.Add(east); surfs.Add(west); surfs.Add(south);

        // pilaster sudut dan bingkai lorong tidak digores
        foreach (var s in new[] { north, south })
        {
            s.skip.Add(new EcRect(0f, 0f, 0.55f, 9f));
            s.skip.Add(new EcRect(s.w - 0.55f, 0f, s.w, 9f));
            s.skip.Add(new EcRect(hw - pw - 0.65f, 0f, hw + pw + 0.65f, 9f));
        }
        foreach (var s in new[] { east, west })
        {
            s.skip.Add(new EcRect(0f, 0f, 0.55f, 9f));
            s.skip.Add(new EcRect(s.w - 0.55f, 0f, s.w, 9f));
        }
        // muka selatan: jendela, pintu, papan nama, lentera
        foreach (float wx in new[] { -14.2f, -5.9f, 6f, 10f, 14f })
            south.skip.Add(new EcRect(wx + hw - 1.02f, 1.15f, wx + hw + 1.02f, 3.95f));
        foreach (float wx in new[] { -14.2f, -10f, -5.9f, 6f, 10f, 14f })
            south.skip.Add(new EcRect(wx + hw - 0.9f, 4.55f, wx + hw + 0.9f, 6.25f));
        south.skip.Add(new EcRect(-10f + hw - 2.85f, 0f, -10f + hw + 2.85f, 4.95f));

        // pembagian jumlah menurut kapasitas, sisa ke permukaan berikutnya yang masih lega
        int cap = 0;
        foreach (var s in surfs) { EcCells(s); cap += s.cells.Count; }
        int given = 0;
        foreach (var s in surfs) { s.quota = Mathf.Min(s.cells.Count, EcGoresanTotal * s.cells.Count / Mathf.Max(1, cap)); given += s.quota; }
        for (int pass = 0; given < EcGoresanTotal && pass < 64; pass++)
            foreach (var s in surfs)
                if (given < EcGoresanTotal && s.quota < s.cells.Count) { s.quota++; given++; }

        // gambar
        var slots = (Material[])dSl.Clone();
        slots[Sl.PlasterB] = UMat("EldGoresan", new Color(0.86f, 0.82f, 0.72f, 1f));
        var g = new GameObject("Goresan").transform;
        g.SetParent(model, false);
        var bag = new GeoBag();
        int inBag = 0, part = 0, seed = 0;
        ecGoresanDrawn = 0;
        for (int si = 0; si < surfs.Count; si++)
        {
            var s = surfs[si];
            var order = new List<int>();
            for (int i = 0; i < s.cells.Count; i++) order.Add(i);
            order.Sort(delegate (int a, int c2)
            {
                int r = EcHash(a, 700 + si).CompareTo(EcHash(c2, 700 + si));
                return r != 0 ? r : a.CompareTo(c2);                                                                       // pilihan acak yang tetap
            });
            for (int k = 0; k < s.quota; k++)
            {
                Vector2 cell = s.cells[order[k]];
                EcGroup(bag, s, cell.x, cell.y, ++seed);
                ecGoresanDrawn++;
                if (++inBag == 400) { bag.Emit(g, "Goresan_" + (++part), slots); bag = new GeoBag(); inBag = 0; }
            }
        }
        if (inBag > 0) bag.Emit(g, "Goresan_" + (++part), slots);
    }
}
