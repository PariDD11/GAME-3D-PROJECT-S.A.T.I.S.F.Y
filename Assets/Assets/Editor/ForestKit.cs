using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Kit alam &amp; desa bergaya fantasy-isekai untuk Hutan Lerai: pohon (ek, cemara, birch, maple musim gugur, pohon mati), batu, rumah desa,
/// reruntuhan, properti. Semua mesh prosedural (GeoBag) dengan tekstur prosedural; material memakai slot Sl.* yang dipetakan di ForestKit.Slots.
/// </summary>
public static partial class ForestKit
{
    // ---- peran slot (memakai indeks Sl.* yang tidak terpakai oleh kota) ----
    public const int Bark = Sl.Leather, BarkBirch = Sl.Marble, BarkDead = Sl.Dark,
                     LeafGreen = Sl.Leaf, LeafPine = Sl.Hedge, LeafYellow = Sl.Hair, LeafOrange = Sl.Blossom, LeafRed = Sl.FieldB,
                     Moss = Sl.Grass, Rock = Sl.Pave, OldThatch = Sl.Field, Crystal = Sl.Eye, Ivy = Sl.Fur, Ash = Sl.Lip,
                     Weathered = Sl.WoodLight, Grime = Sl.PlasterB, OldRoof = Sl.RoofB, Fire = Sl.Water,
                     LeafLight = Sl.CoatA;     // daun terang (puncak mahkota yang kena matahari), dipakai TreeGen

    const string TexDir = "Assets/Textures/Forest";
    const string MatDir = "Assets/Materials/Forest";

    public static Material[] Slots;
    public static Texture2D tGrass, tDirt, tRock, tMud, tLitter, tMoss, tCobble;
    static bool loaded;

    public static void Reset() { loaded = false; PropKit.Reset(); }

    // ------------------------------------------------------------------ noise
    static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + seed * 1442695041;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0xffffff) / (float)0x1000000;
        }
    }

    static int Wrap(int v, int p) { v %= p; return v < 0 ? v + p : v; }

    static float VN(float u, float v, int px, int py, int seed)
    {
        float x = u * px, y = v * py;
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float fx = x - x0, fy = y - y0;
        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);
        float a = Hash(Wrap(x0, px), Wrap(y0, py), seed), b = Hash(Wrap(x0 + 1, px), Wrap(y0, py), seed);
        float c = Hash(Wrap(x0, px), Wrap(y0 + 1, py), seed), d = Hash(Wrap(x0 + 1, px), Wrap(y0 + 1, py), seed);
        return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
    }

    static float FBM(float u, float v, int p, int oct, int seed)
    {
        float sum = 0f, amp = 0.5f, norm = 0f;
        int per = p;
        for (int i = 0; i < oct; i++) { sum += VN(u, v, per, per, seed + i * 17) * amp; norm += amp; amp *= 0.5f; per *= 2; }
        return sum / norm;
    }

    static void Voro(float u, float v, int cells, int seed, out float f1, out float f2, out int id)
    {
        float x = u * cells, y = v * cells;
        int cx = Mathf.FloorToInt(x), cy = Mathf.FloorToInt(y);
        f1 = 9f; f2 = 9f; id = 0;
        for (int j = -1; j <= 1; j++)
            for (int i = -1; i <= 1; i++)
            {
                int gx = cx + i, gy = cy + j, wx = Wrap(gx, cells), wy = Wrap(gy, cells);
                float px = gx + 0.15f + 0.7f * Hash(wx, wy, seed), py = gy + 0.15f + 0.7f * Hash(wx, wy, seed + 91);
                float dx = px - x, dy = py - y, d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d < f1) { f2 = f1; f1 = d; id = wx * 131 + wy; }
                else if (d < f2) f2 = d;
            }
    }

    static Texture2D Make(string name, int size, Func<float, float, Color> fn, bool alpha = false)
    {
        string path = TexDir + "/" + name + ".png";
        var have = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (have != null) return have;
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                px[y * size + x] = fn((x + 0.5f) / size, (y + 0.5f) / size);
        t.SetPixels(px);
        t.Apply();
        File.WriteAllBytes(path, t.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti != null)
        {
            ti.wrapMode = alpha ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            ti.filterMode = FilterMode.Trilinear;
            ti.anisoLevel = 4;
            ti.mipmapEnabled = true;
            ti.alphaIsTransparency = alpha;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Color Cl(float r, float g, float b) { return new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), 1f); }

    // ------------------------------------------------------------------ tekstur objek
    static Texture2D TexBark()
    {
        return Make("f_bark", 256, (u, v) =>
        {
            float furrow = VN(u + VN(u, v, 4, 6, 3) * 0.04f, v, 14, 3, 21);
            float ridge = Mathf.Abs(furrow - 0.5f) * 2f;
            float t = 0.30f + ridge * 0.34f + (FBM(u, v, 8, 3, 22) - 0.5f) * 0.18f;
            if (VN(u, v, 14, 3, 21) < 0.13f) t *= 0.45f;                       // alur dalam
            return Cl(t * 1.0f, t * 0.82f, t * 0.66f);
        });
    }

    static Texture2D TexBirch()
    {
        return Make("f_birch", 256, (u, v) =>
        {
            float t = 0.86f + (FBM(u, v, 6, 3, 31) - 0.5f) * 0.14f;
            float mark = VN(u, v, 5, 36, 32);
            if (mark > 0.80f) t = Mathf.Lerp(t, 0.1f, Mathf.Clamp01((mark - 0.80f) * 9f));
            else if (mark < 0.1f) t *= 0.72f;
            return Cl(t, t * 0.98f, t * 0.94f);
        });
    }

    static Texture2D TexClump()
    {
        return Make("f_leafclump", 256, (u, v) =>
        {
            float f1, f2; int id;
            Voro(u, v, 14, 41, out f1, out f2, out id);
            float cell = 0.55f + Hash(id, id / 3, 41) * 0.45f;
            float edge = Mathf.SmoothStep(0f, 0.22f, f2 - f1);
            float dapple = FBM(u, v, 5, 3, 43);
            float t = (0.35f + 0.65f * edge) * cell * (0.7f + dapple * 0.5f);
            return Cl(t, t, t * 0.9f);
        });
    }

    static Texture2D TexMossObj()
    {
        return Make("f_moss", 256, (u, v) =>
        {
            float n = FBM(u, v, 10, 4, 51), f = VN(u, v, 96, 96, 52);
            float t = 0.45f + n * 0.4f + (f - 0.5f) * 0.12f;
            return Cl(t * 0.62f, t * 0.9f, t * 0.36f);
        });
    }

    static Texture2D TexRockObj()
    {
        return Make("f_rock", 256, (u, v) =>
        {
            float f1, f2; int id;
            Voro(u, v, 6, 61, out f1, out f2, out id);
            float strata = Mathf.Sin((v + FBM(u, v, 4, 3, 62) * 0.3f) * Mathf.PI * 14f) * 0.04f;
            float t = 0.58f + (Hash(id, 2, 61) - 0.5f) * 0.14f + strata + (FBM(u, v, 12, 4, 63) - 0.5f) * 0.18f;
            if (f2 - f1 < 0.035f) t *= 0.5f;
            return Cl(t, t * 0.99f, t * 0.96f);
        });
    }

    static Texture2D TexAged(string name, int seed, float baseT, bool streaks)
    {
        return Make(name, 256, (u, v) =>
        {
            float n = FBM(u, v, 6, 4, seed) - 0.5f;
            float t = baseT + n * 0.28f;
            if (streaks) t -= Mathf.Clamp01(VN(u, v, 8, 2, seed + 5) - 0.45f) * 0.35f;
            return Cl(t, t * 0.98f, t * 0.92f);
        });
    }

    static Texture2D TexWeathered()
    {
        return Make("f_weathered", 256, (u, v) =>
        {
            const int planks = 7;
            float pu = u * planks; int col = Mathf.FloorToInt(pu); float fu = pu - col;
            float h = Hash(col, 3, 71);
            float grain = VN(u, v, planks * 6, 3, 72 + col) - 0.5f;
            float t = 0.5f + (h - 0.5f) * 0.18f + grain * 0.22f + (FBM(u, v, 5, 3, 73) - 0.5f) * 0.12f;
            if (fu < 0.04f || fu > 0.96f) t *= 0.4f;
            return Cl(t * 0.88f, t * 0.86f, t * 0.8f);
        });
    }

    // ------------------------------------------------------------------ tekstur terrain
    static Texture2D TexTGrass()
    {
        // gaya lukisan anime: bercak warna lembut besar (hijau limau -> hijau zamrud), tepi bercak tegas, helai halus bercahaya
        return Make("f_tgrass2", 512, (u, v) =>
        {
            float big = FBM(u, v, 4, 4, 111);
            float patch = Mathf.SmoothStep(0.46f, 0.54f, FBM(u, v, 6, 3, 116));
            float fine = VN(u, v, 192, 192, 112), blade = VN(u, v, 140, 60, 113);
            float flowerDot = Hash((int)(u * 160), (int)(v * 160), 117) > 0.992f ? 1f : 0f;
            Color deep = Cl(0.20f, 0.56f, 0.24f), lime = Cl(0.52f, 0.82f, 0.28f), emer = Cl(0.26f, 0.68f, 0.38f);
            Color c = Color.Lerp(Color.Lerp(deep, emer, big), lime, patch * 0.65f);
            float k = 0.94f + (fine - 0.5f) * 0.10f + (blade - 0.5f) * 0.08f;
            c = new Color(c.r * k, c.g * k, c.b * k, 1f);
            if (flowerDot > 0f) c = Color.Lerp(c, Cl(1f, 0.95f, 0.7f), 0.8f);
            return c;
        });
    }

    static Texture2D TexTDirt()
    {
        return Make("f_tdirt2", 512, (u, v) =>
        {
            float n = FBM(u, v, 7, 4, 121);
            float peb = Hash((int)(u * 200), (int)(v * 200), 122) > 0.96f ? 0.12f : 0f;
            float band = Mathf.SmoothStep(0.45f, 0.55f, FBM(u, v, 5, 3, 124));
            float t = 0.62f + n * 0.22f + peb + band * 0.06f;
            return Cl(t * 1.0f, t * 0.80f, t * 0.56f);                    // tanah hangat keemasan
        });
    }

    static Texture2D TexTRock()
    {
        return Make("f_trock2", 512, (u, v) =>
        {
            float f1, f2; int id;
            Voro(u, v, 6, 131, out f1, out f2, out id);
            float strata = Mathf.Sin((v + FBM(u, v, 4, 3, 132) * 0.35f) * Mathf.PI * 14f) * 0.04f;
            float h = Hash(id, 5, 131);
            float t = 0.66f + (h - 0.5f) * 0.14f + strata + (FBM(u, v, 12, 4, 133) - 0.5f) * 0.14f;
            if (f2 - f1 < 0.035f) t *= 0.62f;
            Color warm = Cl(t * 1.02f, t * 0.95f, t * 0.88f), cool = Cl(t * 0.88f, t * 0.94f, t * 1.04f);
            return Color.Lerp(warm, cool, h);                              // batu berselang hangat/dingin ala lukisan latar anime
        });
    }

    static Texture2D TexTMud()
    {
        return Make("f_tmud2", 512, (u, v) =>
        {
            float n = FBM(u, v, 6, 5, 141);
            float puddle = Mathf.Clamp01((FBM(u, v, 3, 3, 142) - 0.6f) * 4f);
            float t = 0.34f + n * 0.18f - puddle * 0.07f;
            return Cl(t * 1.05f, t * 0.88f, t * 0.62f);
        });
    }

    static Texture2D TexTLitter()
    {
        return Make("f_tlitter2", 512, (u, v) =>
        {
            float f1, f2; int id;
            Voro(u, v, 46, 151, out f1, out f2, out id);
            float h = Hash(id, id / 5, 151);
            float under = 0.40f + FBM(u, v, 6, 3, 152) * 0.14f;
            Color baseC = Cl(under * 1.1f, under * 0.85f, under * 0.5f);
            Color leaf;
            if (h < 0.28f) leaf = Cl(0.92f, 0.48f, 0.16f);            // jingga
            else if (h < 0.5f) leaf = Cl(0.98f, 0.78f, 0.22f);        // kuning
            else if (h < 0.7f) leaf = Cl(0.80f, 0.30f, 0.20f);        // merah bata
            else if (h < 0.88f) leaf = Cl(0.58f, 0.40f, 0.20f);       // cokelat
            else leaf = Cl(0.40f, 0.62f, 0.22f);                      // hijau tua
            float k = (0.75f + 0.25f * Hash(id, 3, 153)) * (0.8f + FBM(u, v, 20, 2, 154) * 0.3f);
            float cover = Mathf.SmoothStep(0.34f, 0.12f, f1);
            return Color.Lerp(baseC, new Color(leaf.r * k, leaf.g * k, leaf.b * k, 1f), cover);
        });
    }

    static Texture2D TexTMoss()
    {
        return Make("f_tmoss2", 512, (u, v) =>
        {
            float n = FBM(u, v, 8, 4, 161), f = VN(u, v, 160, 160, 162);
            float t = 0.55f + n * 0.3f + (f - 0.5f) * 0.10f;
            return Cl(t * 0.52f, t * 1.0f, t * 0.36f);
        });
    }

    static Texture2D TexTCobble()
    {
        return Make("f_tcobble2", 512, (u, v) =>
        {
            float f1, f2; int id;
            Voro(u, v, 10, 171, out f1, out f2, out id);
            float t = 0.68f + (Hash(id, 7, 171) - 0.5f) * 0.22f - f1 * 0.2f + (FBM(u, v, 18, 3, 172) - 0.5f) * 0.12f;
            if (f2 - f1 < 0.075f) t = 0.34f + (f2 - f1) * 1.5f;
            float moss = Mathf.Clamp01((FBM(u, v, 4, 3, 173) - 0.55f) * 3f);
            return Color.Lerp(Cl(t, t * 0.98f, t * 0.94f), Cl(t * 0.5f, t * 0.8f, t * 0.35f), moss * 0.7f);
        });
    }

    // rumput detail (RGBA, tidak diulang)
    public static Texture2D TexGrassTuft()
    {
        return Make("f_grasstuft", 128, (u, v) =>
        {
            float cov = 0f, shade = 0f;
            for (int i = 0; i < 14; i++)
            {
                float x0 = 0.12f + 0.76f * Hash(i, 1, 181);
                float lean = (Hash(i, 2, 181) - 0.5f) * 0.55f;
                float hgt = 0.55f + 0.42f * Hash(i, 3, 181);
                float w = 0.028f + 0.02f * Hash(i, 4, 181);
                float yy = 1f - v;                                    // 0 = akar (bawah gambar)
                if (yy > hgt) continue;
                float tt = yy / hgt;
                float cx = x0 + lean * tt * tt;
                float half = w * (1f - tt * 0.92f);
                float d = Mathf.Abs(u - cx);
                if (d < half) { cov = 1f; shade = Mathf.Max(shade, 0.55f + 0.45f * tt); }
            }
            return new Color(0.34f * shade + 0.1f, 0.58f * shade + 0.1f, 0.2f * shade, cov);
        }, true);
    }

    // ------------------------------------------------------------------ material
    static Material M(string name, Color c, Texture2D tex, float tile, float smooth = 0.08f, float metal = 0f)
    {
        var m = TownMats.Mk("F_" + name, c, tex, tile, smooth, metal);
        return m;
    }

    public static void Load()
    {
        if (loaded && Slots != null) return;
        TownMats.Load();
        foreach (var d in new[] { "Assets/Materials", MatDir, "Assets/Textures", TexDir })
            if (!AssetDatabase.IsValidFolder(d))
            {
                int i = d.LastIndexOf('/');
                AssetDatabase.CreateFolder(d.Substring(0, i), d.Substring(i + 1));
            }

        var bark = TexBark(); var birch = TexBirch(); var clump = TexClump(); var moss = TexMossObj(); var rock = TexRockObj();
        var weather = TexWeathered();
        var grime = TexAged("f_grime", 201, 0.72f, true);
        var oldRoof = TexAged("f_oldroof", 211, 0.5f, true);
        var oldThatch = TexAged("f_oldthatch", 221, 0.46f, false);
        tGrass = TexTGrass(); tDirt = TexTDirt(); tRock = TexTRock(); tMud = TexTMud(); tLitter = TexTLitter(); tMoss = TexTMoss(); tCobble = TexTCobble();

        Slots = new Material[Sl.Count];
        for (int i = 0; i < Slots.Length; i++) Slots[i] = TownMats.Stone;       // cadangan aman
        Slots[Sl.Plaster] = TownMats.PlasterWhite != null ? TownMats.PlasterWhite : TownMats.Plaster[0];
        Slots[Sl.Stone] = TownMats.Stone;
        Slots[Sl.DarkStone] = TownMats.DarkStone;
        Slots[Sl.Wood] = TownMats.Wood;
        Slots[Sl.Roof] = TownMats.Roofs[5];
        Slots[Sl.Glass] = TownMats.Glass;
        Slots[Sl.Metal] = TownMats.Iron;
        Slots[Sl.Gold] = TownMats.Gold;
        Slots[Sl.Cloth] = TownMats.Cloths != null && TownMats.Cloths.Length > 0 ? TownMats.Cloths[0] : TownMats.Stone;
        Slots[Sl.ClothB] = TownMats.Cloths != null && TownMats.Cloths.Length > 2 ? TownMats.Cloths[2] : TownMats.Stone;
        Slots[Sl.Thatch] = TownMats.Thatch;
        Slots[Sl.Glow] = TownMats.Glow;
        Slots[Sl.Cobble] = TownMats.Cobble;
        Slots[Sl.Dirt] = TownMats.Dirt;

        Slots[Bark] = M("Bark", Cl(0.78f, 0.7f, 0.62f), bark, 2.2f, 0.04f);
        Slots[BarkBirch] = M("BarkBirch", Cl(0.95f, 0.94f, 0.9f), birch, 2.4f, 0.1f);
        Slots[BarkDead] = M("BarkDead", Cl(0.62f, 0.58f, 0.56f), bark, 2.2f, 0.03f);
        Slots[LeafGreen] = M("LeafGreen", Cl(0.30f, 0.50f, 0.20f), clump, 1.8f, 0.1f);
        Slots[LeafPine] = M("LeafPine", Cl(0.13f, 0.30f, 0.17f), clump, 1.6f, 0.08f);
        Slots[LeafYellow] = M("LeafYellow", Cl(0.58f, 0.62f, 0.2f), clump, 1.7f, 0.1f);
        Slots[LeafLight] = M("LeafLight", Cl(0.42f, 0.64f, 0.22f), clump, 1.7f, 0.1f);
        Slots[LeafOrange] = M("LeafOrange", Cl(0.86f, 0.42f, 0.12f), clump, 1.7f, 0.1f);
        Slots[LeafRed] = M("LeafRed", Cl(0.66f, 0.18f, 0.10f), clump, 1.7f, 0.1f);
        Slots[Moss] = M("Moss", Cl(0.7f, 0.9f, 0.55f), moss, 1.5f, 0.02f);
        Slots[Rock] = M("Rock", Cl(0.78f, 0.77f, 0.75f), rock, 3f, 0.06f);
        Slots[Ivy] = M("Ivy", Cl(0.24f, 0.46f, 0.18f), clump, 1.2f, 0.12f);
        Slots[Weathered] = M("Weathered", Cl(0.9f, 0.88f, 0.84f), weather, 2f, 0.03f);
        Slots[Grime] = M("Grime", Cl(0.82f, 0.78f, 0.68f), grime, 3f, 0.03f);
        Slots[OldRoof] = M("OldRoof", Cl(0.62f, 0.6f, 0.5f), oldRoof, 2f, 0.05f);
        Slots[OldThatch] = M("OldThatch", Cl(0.72f, 0.64f, 0.46f), oldThatch, 2f, 0.02f);
        Slots[Ash] = M("Ash", Cl(0.13f, 0.12f, 0.11f), bark, 2f, 0.02f);
        Slots[Crystal] = TownMats.Emissive(M("Crystal", Cl(0.35f, 0.85f, 0.9f), null, 1f, 0.85f), Cl(0.25f, 1.1f, 1.5f));
        Slots[Fire] = TownMats.Flame;
        loaded = true;
    }
}
