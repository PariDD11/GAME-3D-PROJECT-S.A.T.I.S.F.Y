using System.Collections.Generic;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Tekstur prosedural (dibuat sekali lalu disimpan sebagai PNG di Assets/Textures/Town) dan material kota.
/// Semua tekstur ubin mulus (tileable) dan dibuat netral/terang supaya bisa diwarnai lewat warna material.
/// </summary>
public static class TownMats
{
    const string MatDir = "Assets/Materials/Town3";
    const string TexDir = "Assets/Textures/Town";

    public static Material[] Plaster, Roofs, Cloths, FieldMats;
    public static Material StoneMid, StoneDeep, RoofGold, Hollow, Stone, StoneWarm, DarkStone, Marble, Wood, WoodLight, Thatch, Glass, Glow, Iron, Gold, Cobble, Pave, Dirt, Grass,
                           Leaf, Blossom, Water, Hedge, Skin, Flame,
                           Bark, LeafMid, LeafWarm, BlossomDeep, BlossomPale;      // kulit kayu bertekstur + nada daun/bunga tambahan untuk TreeGen
    public static Material PlasterWhite, RoofBlue, RoofRed, RoofSlate;

    // ------------------------------------------------------------------ noise tileable
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

    /// <summary>Value noise mulus; px/py = jumlah sel kisi per ubin.</summary>
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
        for (int i = 0; i < oct; i++)
        {
            sum += VN(u, v, per, per, seed + i * 17) * amp;
            norm += amp;
            amp *= 0.5f;
            per *= 2;
        }
        return sum / norm;
    }

    static void Voronoi(float u, float v, int cells, int seed, out float f1, out float f2, out int id)
    {
        float x = u * cells, y = v * cells;
        int cx = Mathf.FloorToInt(x), cy = Mathf.FloorToInt(y);
        f1 = 9f; f2 = 9f; id = 0;
        for (int j = -1; j <= 1; j++)
            for (int i = -1; i <= 1; i++)
            {
                int gx = cx + i, gy = cy + j;
                int wx = Wrap(gx, cells), wy = Wrap(gy, cells);
                float px = gx + 0.15f + 0.7f * Hash(wx, wy, seed);
                float py = gy + 0.15f + 0.7f * Hash(wx, wy, seed + 91);
                float dx = px - x, dy = py - y;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d < f1) { f2 = f1; f1 = d; id = wx * 131 + wy; }
                else if (d < f2) f2 = d;
            }
    }

    // ------------------------------------------------------------------ generator tekstur
    static Texture2D Make(string name, int size, Func<float, float, Color> fn)
    {
        string path = TexDir + "/" + name + ".png";
        var have = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (have != null) return have;          // tekstur sudah pernah dibuat; hapus file PNG-nya untuk membuat ulang
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
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.filterMode = FilterMode.Trilinear;
            ti.anisoLevel = 4;
            ti.mipmapEnabled = true;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Color G(float v) { return new Color(v, v, v, 1f); }

        static Texture2D TexPlaster()
    {
        return Make("t_plaster3", 512, (u, v) =>
        {
            float n = FBM(u, v, 6, 5, 3) - 0.5f;
            float streak = VN(u, v, 7, 40, 9) - 0.5f;
            float spk = (Hash((int)(u * 512), (int)(v * 512), 5) - 0.5f) * 0.06f;
            float patch = Mathf.SmoothStep(0.47f, 0.53f, FBM(u, v, 3, 3, 8));
            float stain = Mathf.SmoothStep(0.55f, 0.85f, VN(u, v, 9, 3, 33)) * Mathf.SmoothStep(0.0f, 1f, 1f - Mathf.Abs(v - 0.5f) * 2f) * 0.05f;
            float f1, f2; int id;
            Voronoi(u, v, 5, 17, out f1, out f2, out id);
            float crack = (f2 - f1) < 0.012f && Hash(id, 3, 4) > 0.55f ? 0.12f : 0f;
            float trowel = Mathf.Sin((u * 3f + FBM(u, v, 4, 3, 41) * 1.2f) * 6.2832f * 2f) * 0.012f;
            float c = 0.95f + n * 0.1f + streak * 0.05f + spk - patch * 0.05f - stain - crack + trowel;
            return new Color(c, c * 0.99f, c * 0.96f, 1f);
        });
    }

        static Texture2D TexBricks(string name, int cols, int rows, float tone, float toneVar, float mortarTone, int seed)
    {
        return Make(name + "v3", 512, (u, v) =>
        {
            float rv = v * rows;
            int row = Mathf.FloorToInt(rv);
            float u2 = u * cols + (row % 2 == 0 ? 0f : 0.5f);
            int col = Mathf.FloorToInt(u2);
            float fu = u2 - col, fv = rv - row;
            float bw = 512f / cols, bh = 512f / rows;
            float dl = fu * bw, dr = (1f - fu) * bw, db = fv * bh, dt = (1f - fv) * bh;
            float d = Mathf.Min(Mathf.Min(dl, dr), Mathf.Min(db, dt));
            float h = Hash(Wrap(col, cols), row, seed);
            float h2 = Hash(Wrap(col, cols), row, seed + 7);
            float grain = FBM(u, v, 14, 4, seed + 5) - 0.5f;
            float t = tone + (h - 0.5f) * toneVar + grain * 0.16f;
            float warm = (h2 - 0.5f) * 0.08f;
            if (d < 1.9f) { t = mortarTone + grain * 0.06f; return new Color(t, t * 0.98f, t * 0.93f, 1f); }
            float bevel = Mathf.SmoothStep(0f, 1f, (d - 1.9f) / 4.5f);
            t *= 0.82f + 0.18f * bevel;
            if (dt < 5f || dl < 5f) t *= 1.05f; else if (db < 5f || dr < 5f) t *= 0.95f;     // sorot atas-kiri, bayangan bawah-kanan
            float chip = Hash((int)(u * 512), (int)(v * 512), seed + 13) > 0.985f ? 0.85f : 1f;
            float moss = Mathf.SmoothStep(0.62f, 0.8f, FBM(u, v, 5, 3, seed + 21)) * (1f - Mathf.SmoothStep(0f, 1f, fv)) * 0.22f;
            t *= chip;
            return new Color(t * (1f + warm) - moss * 0.1f, t - moss * 0.02f, t * (0.95f - warm) - moss * 0.12f, 1f);
        });
    }

        static Texture2D TexRoof()
    {
        return Make("t_roof3", 512, (u, v) =>
        {
            const int rows = 8, cols = 4;
            float rv = v * rows;
            int row = Mathf.FloorToInt(rv);
            float fv = rv - row;
            float u2 = u * cols + (row % 2 == 0 ? 0f : 0.5f);
            int col = Mathf.FloorToInt(u2);
            float fu = u2 - col;
            float h = Hash(Wrap(col, cols), row, 21);
            float edge = 0.06f * Mathf.Sin(Mathf.PI * fu);                                     // tepi bawah genteng melengkung
            float fvr = fv + (0.5f - Mathf.Abs(fu - 0.5f)) * 0.0f;
            float shade = 0.7f + 0.3f * Mathf.SmoothStep(0f, 0.5f, fv);
            float curve = 1f - 0.2f * Mathf.Pow(Mathf.Abs(fu - 0.5f) * 2f, 2f);
            float t = shade * curve * (0.86f + h * 0.14f);
            if (fv < 0.07f + edge) t *= 1.12f;                                                // sorot lekuk atas
            if (fv > 0.9f) t *= 0.7f - (fv - 0.9f) * 0.3f;                                    // bayangan tepi bawah
            if (fu < 0.045f || fu > 0.955f) t *= 0.62f;
            float moss = Mathf.SmoothStep(0.64f, 0.78f, FBM(u, v, 6, 3, 77)) * 0.18f;
            t *= 0.97f + (VN(u, v, 32, 32, 4) - 0.5f) * 0.1f;
            float chip = Hash((int)(u * 512), (int)(v * 512), 91) > 0.992f ? 0.8f : 1f;
            t = Mathf.Clamp01(t * chip);
            return new Color(t - moss * 0.2f, t + moss * 0.05f, t - moss * 0.25f, 1f);
        });
    }

        static Texture2D TexWood()
    {
        return Make("t_wood3", 512, (u, v) =>
        {
            const int planks = 8;
            float pu = u * planks;
            int col = Mathf.FloorToInt(pu);
            float fu = pu - col;
            float h = Hash(col, 3, 31);
            float grain = VN(u, v, planks * 8, 3, 41 + col) - 0.5f;
            float warp = (FBM(u, v, 4, 3, 42) - 0.5f) * 0.6f;
            float rings = Mathf.Sin((fu + grain * 0.4f + warp * 0.2f) * 52f) * 0.05f;
            float fine = (VN(u, v, planks * 30, 2, 43 + col) - 0.5f) * 0.1f;
            float t = 0.8f + (h - 0.5f) * 0.2f + grain * 0.22f + rings + fine;
            // simpul kayu
            float kx = Hash(col, 9, 55) , ky = Hash(col, 11, 56);
            float dk = Mathf.Sqrt(Mathf.Pow((fu - 0.25f - kx * 0.5f) * 1.2f, 2f) + Mathf.Pow((v * 2f - Mathf.Floor(v * 2f) - ky) * 0.6f, 2f));
            if (Hash(col, (int)(v * 2f), 57) > 0.55f && dk < 0.12f) t *= 0.62f + dk * 3f;
            if (fu < 0.04f || fu > 0.96f) t *= 0.42f;
            else if (fu < 0.08f || fu > 0.92f) t *= 0.88f;
            // paku
            float vy = Mathf.Repeat(v * 4f, 1f);
            if ((fu > 0.12f && fu < 0.16f || fu > 0.84f && fu < 0.88f) && vy > 0.06f && vy < 0.1f) t *= 0.55f;
            return new Color(t, t * 0.93f, t * 0.82f, 1f);
        });
    }

        static Texture2D TexCobble()
    {
        return Make("t_cobble3", 512, (u, v) =>
        {
            float f1, f2; int id;
            Voronoi(u, v, 9, 7, out f1, out f2, out id);
            float edge = f2 - f1;
            float h = Hash(id, id / 7, 3);
            float warm = Hash(id, 5, 6);
            float dome = 1f - f1 * 0.9f;
            float t = 0.66f + h * 0.2f + dome * 0.14f + (FBM(u, v, 22, 3, 8) - 0.5f) * 0.12f;
            float moss = edge < 0.12f ? Mathf.SmoothStep(0.55f, 0.75f, FBM(u, v, 10, 3, 19)) * 0.25f : 0f;
            if (edge < 0.07f) { float m = 0.34f + edge * 1.4f; return new Color(m - moss * 0.1f, m + moss * 0.05f, m - moss * 0.12f, 1f); }
            float hi = edge < 0.16f ? 0.9f + (edge - 0.07f) * 1.1f : 1f;
            t *= hi;
            return new Color(t * (0.96f + warm * 0.08f), t * 0.98f, t * (0.96f - warm * 0.06f), 1f);
        });
    }

    static Texture2D TexPave()
    {
        return Make("t_pave", 256, (u, v) =>
        {
            const int n = 6;
            float x = u * n, y = v * n;
            int cx = Mathf.FloorToInt(x), cy = Mathf.FloorToInt(y);
            float fx = x - cx, fy = y - cy;
            float d = Mathf.Min(Mathf.Min(fx, 1f - fx), Mathf.Min(fy, 1f - fy)) * 256f / n;
            float t = 0.78f + (Hash(Wrap(cx, n), Wrap(cy, n), 12) - 0.5f) * 0.12f + (FBM(u, v, 10, 2, 2) - 0.5f) * 0.08f;
            if (d < 1.4f) t = 0.5f;
            return new Color(t, t * 0.99f, t * 0.95f, 1f);
        });
    }

        static Texture2D TexGrass()
    {
        return Make("t_grass3", 512, (u, v) =>
        {
            float big = FBM(u, v, 4, 4, 11);
            float fine = VN(u, v, 128, 128, 13);
            float blade = VN(u, v, 110, 36, 14);
            float blade2 = VN(u, v, 70, 24, 24);
            float patch = Mathf.SmoothStep(0.46f, 0.54f, FBM(u, v, 6, 3, 15));
            float r = 0.34f + big * 0.14f + patch * 0.14f + (fine - 0.5f) * 0.06f + (blade - 0.5f) * 0.05f;
            float g = 0.66f + big * 0.16f + patch * 0.08f + (fine - 0.5f) * 0.08f + (blade - 0.5f) * 0.08f + (blade2 - 0.5f) * 0.05f;
            float b = 0.24f + big * 0.08f + (fine - 0.5f) * 0.03f;
            float hh = Hash((int)(u * 256), (int)(v * 256), 28);
            if (hh > 0.994f) { r = 0.95f; g = 0.92f; b = 0.55f; }                    // bunga kecil kuning
            else if (hh < 0.004f) { r = 0.95f; g = 0.95f; b = 0.95f; }               // bunga putih
            else if (hh > 0.97f && hh < 0.98f) { g *= 1.12f; }                       // kilau bilah
            return new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), 1f);
        });
    }

    static Texture2D TexDirt()
    {
        return Make("t_dirt2", 256, (u, v) =>
        {
            float n = FBM(u, v, 8, 4, 51);
            float peb = Hash((int)(u * 128), (int)(v * 128), 52) > 0.965f ? 0.12f : 0f;
            float t = 0.66f + n * 0.22f + peb;
            return new Color(t * 0.98f, t * 0.82f, t * 0.6f, 1f);
        });
    }

        static Texture2D TexThatch()
    {
        return Make("t_thatch3", 512, (u, v) =>
        {
            float s = VN(u, v, 96, 6, 61);
            float s2 = VN(u, v, 48, 12, 62);
            float s3 = VN(u, v, 192, 3, 63);
            float t = 0.62f + (s - 0.5f) * 0.4f + (s2 - 0.5f) * 0.22f + (s3 - 0.5f) * 0.16f;
            float band = Mathf.Repeat(v * 6f, 1f);
            if (band > 0.88f) t *= 0.62f + (1f - band) * 2.5f;
            else if (band < 0.1f) t *= 1.08f;
            float moss = Mathf.SmoothStep(0.65f, 0.8f, FBM(u, v, 5, 3, 64)) * 0.14f;
            return new Color(t - moss * 0.12f, t * 0.88f + moss * 0.04f, t * 0.52f - moss * 0.05f, 1f);
        });
    }

        static Texture2D TexLeaf()
    {
        return Make("t_leaf3", 512, (u, v) =>
        {
            float b = FBM(u, v, 10, 4, 71);
            float s = VN(u, v, 64, 64, 72);
            float f1, f2; int id;
            Voronoi(u, v, 14, 73, out f1, out f2, out id);
            float vein = (f2 - f1) < 0.05f ? -0.1f : 0f;
            float t = 0.45f + b * 0.55f + (s - 0.5f) * 0.2f + vein + (Hash(id, 1, 74) - 0.5f) * 0.14f;
            t = Mathf.Clamp01(t);
            return new Color(t * 0.9f, t, t * 0.8f, 1f);
        });
    }

    /// <summary>Kulit kayu: alur vertikal, retakan gelap, bercak lumut tipis (v = sepanjang batang).</summary>
    static Texture2D TexBark()
    {
        return Make("t_bark3", 256, (u, v) =>
        {
            float ridge = VN(u, v, 10, 2, 91) * 0.6f + VN(u, v, 22, 4, 92) * 0.4f;
            float crack = Mathf.Abs(VN(u, v, 7, 2, 93) - 0.5f) * 2f;
            float fine = VN(u, v, 48, 12, 94);
            float t = 0.6f + (ridge - 0.5f) * 0.5f + (fine - 0.5f) * 0.14f;
            t *= Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(crack * 5f));
            float moss = Mathf.Clamp01((FBM(u, v, 3, 3, 95) - 0.62f) * 4f) * 0.35f;
            return new Color(t * (1f - moss * 0.5f), t * (0.88f + moss * 0.1f), t * (0.76f - moss * 0.2f), 1f);
        });
    }

    // kind: 0 gandum, 1 sayur berbaris, 2 kubis, 3 bajak/tanah
    static Texture2D TexField(string name, int kind)
    {
        return Make(name, 256, (u, v) =>
        {
            const int rows = 12;
            float r = Mathf.Repeat(v * rows, 1f);
            float along = u;
            float soil = 0.38f + (FBM(u, v, 8, 3, 80 + kind) - 0.5f) * 0.14f;
            Color soilC = new Color(soil * 1.05f, soil * 0.78f, soil * 0.55f, 1f);
            float bump = Mathf.Pow(1f - Mathf.Abs(r * 2f - 1f), 0.8f);
            Color plant;
            switch (kind)
            {
                case 0:
                {
                    float tip = VN(along, v, 96, 24, 85);
                    float t = 0.65f + tip * 0.35f;
                    plant = new Color(0.86f * t, 0.7f * t, 0.26f * t, 1f);
                    return Color.Lerp(soilC, plant, Mathf.Clamp01(bump * 1.5f - 0.1f));
                }
                case 1:
                {
                    float t = 0.6f + VN(along, v, 48, 12, 86) * 0.4f;
                    plant = new Color(0.25f * t, 0.6f * t, 0.2f * t, 1f);
                    return Color.Lerp(soilC, plant, Mathf.Clamp01(bump * 1.6f - 0.15f));
                }
                case 2:
                {
                    float cu = Mathf.Repeat(u * 12f, 1f);
                    float d = Mathf.Sqrt((cu - 0.5f) * (cu - 0.5f) + (r - 0.5f) * (r - 0.5f));
                    float t = 0.7f + VN(u, v, 24, 24, 87) * 0.3f;
                    plant = new Color(0.55f * t, 0.8f * t, 0.42f * t, 1f);
                    return Color.Lerp(soilC, plant, Mathf.Clamp01((0.36f - d) * 7f));
                }
                default:
                {
                    float furrow = 0.5f + 0.5f * Mathf.Cos(r * Mathf.PI * 2f);
                    float t = 0.75f + furrow * 0.25f;
                    return new Color(soilC.r * t, soilC.g * t, soilC.b * t, 1f);
                }
            }
        });
    }

    // ------------------------------------------------------------------ material
    public static Material Mk(string name, Color c, Texture2D tex, float tileMeters, float smooth = 0.12f, float metal = 0f)
    {
        string path = MatDir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            Shader sh = GraphicsSettings.currentRenderPipeline != null ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Standard");
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, path);
        }
        m.color = c;
        if (tex != null)
        {
            m.mainTexture = tex;
            float s = 1f / Mathf.Max(0.01f, tileMeters);
            m.mainTextureScale = new Vector2(s, s);
        }
        ApplyNormal(m, tex);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);          // dua sisi: bangunan solid dari sudut mana pun
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    // ------------------------------------------------------------------ normal map otomatis (relief batu/kayu/genteng, bukan datar ala Roblox)
    static readonly Dictionary<string, Texture2D> normCache = new Dictionary<string, Texture2D>();

    static void ApplyNormal(Material m, Texture2D tex)
    {
        try
        {
            if (tex == null || !m.HasProperty("_BumpMap")) return;
            string src = AssetDatabase.GetAssetPath(tex);
            if (string.IsNullOrEmpty(src) || !src.EndsWith(".png")) return;
            Texture2D n;
            if (!normCache.TryGetValue(src, out n) || n == null)
            {
                string np = src.Substring(0, src.Length - 4) + "_N.png";
                n = AssetDatabase.LoadAssetAtPath<Texture2D>(np);
                if (n == null)
                {
                    var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!t.LoadImage(System.IO.File.ReadAllBytes(src))) return;
                    int w = t.width, h = t.height;
                    var px = t.GetPixels32();
                    var g = new float[w * h];
                    for (int i = 0; i < g.Length; i++) g[i] = (px[i].r * 0.3f + px[i].g * 0.59f + px[i].b * 0.11f) / 255f;
                    var outp = new Color32[w * h];
                    float k = 3.2f;
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                        {
                            int xl = (x + w - 1) % w, xr = (x + 1) % w, yd = (y + h - 1) % h, yu = (y + 1) % h;
                            float dx = (g[y * w + xr] - g[y * w + xl]) * k;
                            float dy = (g[yu * w + x] - g[yd * w + x]) * k;
                            var v = new Vector3(-dx, -dy, 1f).normalized;
                            outp[y * w + x] = new Color32((byte)((v.x * 0.5f + 0.5f) * 255f), (byte)((v.y * 0.5f + 0.5f) * 255f), (byte)((v.z * 0.5f + 0.5f) * 255f), 255);
                        }
                    var nt = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    nt.SetPixels32(outp);
                    System.IO.File.WriteAllBytes(np, nt.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(nt); UnityEngine.Object.DestroyImmediate(t);
                    AssetDatabase.ImportAsset(np, ImportAssetOptions.ForceSynchronousImport);
                    var imp = AssetImporter.GetAtPath(np) as TextureImporter;
                    if (imp != null)
                    {
                        imp.textureType = TextureImporterType.NormalMap;
                        imp.wrapMode = TextureWrapMode.Repeat;
                        imp.SaveAndReimport();
                    }
                    n = AssetDatabase.LoadAssetAtPath<Texture2D>(np);
                }
                normCache[src] = n;
            }
            if (n == null) return;
            m.SetTexture("_BumpMap", n);
            m.SetTextureScale("_BumpMap", m.mainTextureScale);
            if (m.HasProperty("_BumpScale")) m.SetFloat("_BumpScale", 0.9f);
            m.EnableKeyword("_NORMALMAP");
        }
        catch (Exception e) { Debug.LogWarning("[TownMats] normal map dilewati: " + e.Message); }
    }

    public static Material Emissive(Material m, Color e)
    {
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", e);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Color C(float r, float g, float b) { return new Color(r, g, b, 1f); }

    public static void Load()
    {
        foreach (var d in new[] { "Assets/Materials", MatDir, "Assets/Textures", TexDir })
            EnsureFolder(d);

        var tPlaster = TexPlaster();
        var tStone = TexBricks("t_stone", 6, 10, 0.64f, 0.22f, 0.32f, 101);
        var tMarble = TexBricks("t_marble", 4, 6, 0.9f, 0.07f, 0.66f, 131);
        var tRoof = TexRoof();
        var tWood = TexWood();
        var tCobble = TexCobble();
        var tPave = TexPave();
        var tGrass = TexGrass();
        var tDirt = TexDirt();
        var tThatch = TexThatch();
        var tLeaf = TexLeaf();
        var tBark = TexBark();
        var tWheat = TexField("t_field_wheat", 0);
        var tVeg = TexField("t_field_veg", 1);
        var tCab = TexField("t_field_cabbage", 2);
        var tPlow = TexField("t_field_plow", 3);

        Plaster = new[]
        {
            Mk("T2_PlasterCream", C(0.9f, 0.84f, 0.7f), tPlaster, 3f, 0.04f),
            Mk("T2_PlasterWhite", C(0.92f, 0.9f, 0.84f), tPlaster, 3f, 0.04f),
            Mk("T2_PlasterPeach", C(0.84f, 0.68f, 0.52f), tPlaster, 3f, 0.04f),
            Mk("T2_PlasterRose", C(0.78f, 0.6f, 0.54f), tPlaster, 3f, 0.04f),
            Mk("T2_PlasterButter", C(0.84f, 0.74f, 0.5f), tPlaster, 3f, 0.04f),
            Mk("T2_PlasterMint", C(0.66f, 0.72f, 0.62f), tPlaster, 3f, 0.04f),
            Mk("T2_PlasterSky", C(0.62f, 0.68f, 0.74f), tPlaster, 3f, 0.04f),
        };
        PlasterWhite = Plaster[1];
        Roofs = new[]
        {
            Mk("T2_RoofCoral", C(0.62f, 0.3f, 0.2f), tRoof, 2f, 0.15f),
            Mk("T2_RoofSun", C(0.72f, 0.5f, 0.28f), tRoof, 2f, 0.15f),
            Mk("T2_RoofCobalt", C(0.3f, 0.38f, 0.52f), tRoof, 2f, 0.18f),
            Mk("T2_RoofIndigo", C(0.34f, 0.35f, 0.4f), tRoof, 2f, 0.15f),
            Mk("T2_RoofTeal", C(0.26f, 0.4f, 0.36f), tRoof, 2f, 0.15f),
            Mk("T2_RoofPlum", C(0.5f, 0.28f, 0.28f), tRoof, 2f, 0.15f),
        };
        RoofRed = Roofs[0]; RoofBlue = Roofs[2]; RoofSlate = Roofs[3];
        Stone = Mk("T2_Stone", C(0.8f, 0.78f, 0.74f), tStone, 3f, 0.08f);
        StoneWarm = Mk("T2_StoneWarm", C(0.84f, 0.78f, 0.68f), tStone, 3f, 0.08f);
        DarkStone = Mk("T2_DarkStone", C(0.48f, 0.48f, 0.53f), tStone, 3f, 0.1f);
        // n81 tiga tembok konsentris: batu kelabu makin gelap ke pusat (luar = Stone, tengah = StoneMid, dalam = DarkStone/StoneDeep), atap emas matte menara Tembok Dalam, rongga mata patung penjaga
        StoneMid = Mk("T2_StoneMid", C(0.66f, 0.65f, 0.63f), tStone, 3f, 0.09f);
        StoneDeep = Mk("T2_StoneDeep", C(0.36f, 0.36f, 0.4f), tStone, 3f, 0.1f);
        RoofGold = Mk("T2_RoofGold", C(0.86f, 0.66f, 0.24f), tRoof, 2f, 0.3f, 0.25f);
        Hollow = Mk("T2_Hollow", C(0.05f, 0.045f, 0.05f), null, 1f, 0.05f);
        Marble = Mk("T_Marble", C(0.88f, 0.88f, 0.88f), tMarble, 4f, 0.35f);
        Wood = Mk("T2_Wood", C(0.44f, 0.3f, 0.2f), tWood, 2f, 0.1f);
        WoodLight = Mk("T2_WoodLight", C(0.68f, 0.52f, 0.36f), tWood, 2f, 0.1f);
        Thatch = Mk("T2_Thatch", C(0.8f, 0.7f, 0.44f), tThatch, 2f, 0.02f);
        Glass = Emissive(Mk("T2_Glass", C(0.4f, 0.55f, 0.65f), null, 1f, 0.85f), C(0.28f, 0.22f, 0.08f));
        Glow = Emissive(Mk("T_Glow", C(1f, 0.78f, 0.42f), null, 1f, 0.3f), C(1.5f, 0.95f, 0.4f));
        Flame = Emissive(Mk("T_Flame", C(1f, 0.55f, 0.15f), null, 1f, 0.3f), C(2.2f, 0.9f, 0.2f));
        Iron = Mk("T_Iron", C(0.2f, 0.2f, 0.23f), null, 1f, 0.45f, 0.7f);
        Gold = Mk("T_Gold", C(0.95f, 0.75f, 0.25f), null, 1f, 0.7f, 0.85f);
        Cobble = Mk("T2_Cobble", C(0.82f, 0.78f, 0.72f), tCobble, 4f, 0.08f);
        Pave = Mk("T2_Pave", C(0.82f, 0.78f, 0.7f), tPave, 3f, 0.1f);
        Dirt = Mk("T2_Dirt", C(0.78f, 0.72f, 0.64f), tDirt, 4f, 0.02f);
        Grass = Mk("T_Grass", C(0.74f, 0.86f, 0.66f), tGrass, 6f, 0.02f);
        Leaf = Mk("T2_Leaf", C(0.42f, 0.66f, 0.26f), tLeaf, 3f, 0.05f);
        Blossom = Mk("T2_Blossom", C(0.88f, 0.66f, 0.74f), tLeaf, 3f, 0.05f);
        Hedge = Mk("T2_Hedge", C(0.27f, 0.54f, 0.25f), tLeaf, 2f, 0.04f);
        Bark = Mk("T2_Bark", C(0.66f, 0.52f, 0.4f), tBark, 2.2f, 0.04f);
        LeafMid = Mk("T2_LeafMid", C(0.34f, 0.60f, 0.24f), tLeaf, 2.5f, 0.05f);
        LeafWarm = Mk("T2_LeafWarm", C(0.62f, 0.72f, 0.27f), tLeaf, 3f, 0.05f);
        BlossomDeep = Mk("T2_BlossomDeep", C(0.78f, 0.46f, 0.58f), tLeaf, 3f, 0.05f);
        BlossomPale = Mk("T2_BlossomPale", C(0.96f, 0.80f, 0.86f), tLeaf, 3f, 0.05f);
        Water = Mk("T2_Water", C(0.2f, 0.46f, 0.58f), null, 1f, 0.9f);
        Skin = Mk("T_Skin", C(0.95f, 0.78f, 0.64f), null, 1f, 0.2f);
        Cloths = new[]
        {
            Mk("T_ClothRed", C(0.56f, 0.15f, 0.15f), null, 1f, 0.2f),
            Mk("T_ClothBlue", C(0.2f, 0.28f, 0.5f), null, 1f, 0.2f),
            Mk("T_ClothYellow", C(0.78f, 0.62f, 0.2f), null, 1f, 0.2f),
            Mk("T_ClothGreen", C(0.22f, 0.4f, 0.24f), null, 1f, 0.2f),
            Mk("T_ClothWhite", C(0.96f, 0.95f, 0.9f), null, 1f, 0.2f),
            Mk("T_ClothPurple", C(0.4f, 0.26f, 0.5f), null, 1f, 0.2f),
        };
        FieldMats = new[]
        {
            Mk("T_FieldWheat", C(1f, 1f, 1f), tWheat, 8f, 0.02f),
            Mk("T_FieldVeg", C(1f, 1f, 1f), tVeg, 8f, 0.02f),
            Mk("T_FieldCabbage", C(1f, 1f, 1f), tCab, 8f, 0.02f),
            Mk("T_FieldPlow", C(1f, 1f, 1f), tPlow, 8f, 0.02f),
        };
        AssetDatabase.SaveAssets();
    }

    /// <summary>Daftar material untuk tiap slot geometri. Slot yang tidak diberikan memakai nilai bawaan.</summary>
    public static Material[] Slots(Material plaster = null, Material roof = null, Material plasterB = null, Material roofB = null, Material cloth = null, Material clothB = null)
    {
        var a = new Material[Sl.Count];
        a[Sl.Plaster] = plaster != null ? plaster : Plaster[0];
        a[Sl.PlasterB] = plasterB != null ? plasterB : Plaster[1];
        a[Sl.Stone] = Stone;
        a[Sl.Wood] = Wood;
        a[Sl.WoodLight] = WoodLight;
        a[Sl.Roof] = roof != null ? roof : Roofs[0];
        a[Sl.RoofB] = roofB != null ? roofB : Roofs[2];
        a[Sl.Glass] = Glass;
        a[Sl.Metal] = Iron;
        a[Sl.Gold] = Gold;
        a[Sl.Cloth] = cloth != null ? cloth : Cloths[0];
        a[Sl.ClothB] = clothB != null ? clothB : Cloths[1];
        a[Sl.Thatch] = Thatch;
        a[Sl.Marble] = Marble;
        a[Sl.DarkStone] = DarkStone;
        a[Sl.Glow] = Glow;
        a[Sl.Cobble] = Cobble;
        a[Sl.Dirt] = Dirt;
        a[Sl.Grass] = Grass;
        a[Sl.Field] = FieldMats[0];
        a[Sl.FieldB] = FieldMats[1];
        a[Sl.Leaf] = Leaf;
        a[Sl.Water] = Water;
        a[Sl.Blossom] = Blossom;
        a[Sl.Hedge] = Hedge;
        a[Sl.Pave] = Pave;
        // slot NPC yang tidak dipakai bangunan dipinjam TreeGen: batang, daun tengah/hangat, sakura tua/pucat
        a[Sl.Leather] = Bark;
        a[Sl.Fur] = LeafMid;
        a[Sl.Hair] = LeafWarm;
        a[Sl.Lip] = BlossomDeep;
        a[Sl.EyeW] = BlossomPale;
        return a;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int i = path.LastIndexOf('/');
        EnsureFolder(path.Substring(0, i));
        AssetDatabase.CreateFolder(path.Substring(0, i), path.Substring(i + 1));
    }
}
