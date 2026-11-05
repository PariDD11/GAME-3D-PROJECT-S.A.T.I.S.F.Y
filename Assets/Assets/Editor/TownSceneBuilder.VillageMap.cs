using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using HunterGuild;

/// <summary>Denah Desa Pemula: gambar peta (tekstur PNG) dibuat dari posisi bangunan sebenarnya, dipasang di papan besar.</summary>
public static partial class TownSceneBuilder
{
    class VMapItem { public string name; public Vector2 pos; public int kind; }   // kind: 0 sipil 1 toko 2 dijual 3 religi 4 ladang/latihan 5 gerbang 6 pusat
    static readonly List<VMapItem> vMap = new List<VMapItem>();

    static void VMapMark(string name, Vector2 pos, int kind) { vMap.Add(new VMapItem { name = name, pos = pos, kind = kind }); }
    static void VMapMark(string name, Vector3 pos, int kind) { vMap.Add(new VMapItem { name = name, pos = new Vector2(pos.x, pos.z), kind = kind }); }

    // ---------------- gambar ----------------
    const int MS = 1024;
    static Color32[] mpx;
    static float mScale, mHalf;

    static Vector2 M2P(Vector2 w) { return new Vector2((w.x + mHalf) * mScale, (w.y + mHalf) * mScale); }

    static void MPut(int x, int y, Color c)
    {
        if (x < 0 || y < 0 || x >= MS || y >= MS) return;
        mpx[y * MS + x] = c;
    }

    static void MDisc(Vector2 p, float r, Color c)
    {
        int x0 = Mathf.FloorToInt(p.x - r), x1 = Mathf.CeilToInt(p.x + r), y0 = Mathf.FloorToInt(p.y - r), y1 = Mathf.CeilToInt(p.y + r);
        for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
            if ((x - p.x) * (x - p.x) + (y - p.y) * (y - p.y) <= r * r) MPut(x, y, c);
    }

    static void MRing(Vector2 p, float r, float w, Color c)
    {
        int x0 = Mathf.FloorToInt(p.x - r - w), x1 = Mathf.CeilToInt(p.x + r + w), y0 = Mathf.FloorToInt(p.y - r - w), y1 = Mathf.CeilToInt(p.y + r + w);
        for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
        {
            float d = Mathf.Sqrt((x - p.x) * (x - p.x) + (y - p.y) * (y - p.y));
            if (Mathf.Abs(d - r) <= w * 0.5f) MPut(x, y, c);
        }
    }

    static void MLine(Vector2 a, Vector2 b, float w, Color c)
    {
        float len = Vector2.Distance(a, b);
        int n = Mathf.Max(1, Mathf.CeilToInt(len / 1.2f));
        for (int i = 0; i <= n; i++) MDisc(Vector2.Lerp(a, b, i / (float)n), w * 0.5f, c);
    }

    static void MRect(Vector2 p, float hw, float hh, Color c)
    {
        for (int y = Mathf.FloorToInt(p.y - hh); y <= Mathf.CeilToInt(p.y + hh); y++)
            for (int x = Mathf.FloorToInt(p.x - hw); x <= Mathf.CeilToInt(p.x + hw); x++) MPut(x, y, c);
    }

    // font piksel 3x5: 0-9 dan N
    static readonly string[] MGlyph =
    {
        "111101101101111", "010110010010111", "111001111100111", "111001111001111", "101101111001001",
        "111100111001111", "111100111101111", "111001001001001", "111101111101111", "111101111001111", "101111111111101"
    };

    static void MGlyphDraw(int g, Vector2 p, int px, Color c)
    {
        string s = MGlyph[g];
        for (int r = 0; r < 5; r++) for (int cc = 0; cc < 3; cc++)
            if (s[r * 3 + cc] == '1') MRect(new Vector2(p.x + cc * px, p.y - r * px), px * 0.5f, px * 0.5f, c);
    }

    static void MNumber(int n, Vector2 center, int px, Color c)
    {
        string t = n.ToString();
        float w = t.Length * 3 * px + (t.Length - 1) * px;
        float x = center.x - w * 0.5f + px * 0.5f;
        for (int i = 0; i < t.Length; i++) { MGlyphDraw(t[i] - '0', new Vector2(x, center.y + 2 * px), px, c); x += 4 * px; }
    }

    static Color MKindColor(int k)
    {
        switch (k)
        {
            case 0: return new Color(0.28f, 0.45f, 0.78f);
            case 1: return new Color(0.88f, 0.52f, 0.18f);
            case 2: return new Color(0.96f, 0.82f, 0.22f);
            case 3: return new Color(0.92f, 0.92f, 0.96f);
            case 4: return new Color(0.4f, 0.7f, 0.3f);
            case 5: return new Color(0.7f, 0.25f, 0.2f);
            default: return new Color(0.45f, 0.82f, 0.95f);
        }
    }

    static Texture2D VRenderMap(out List<string> legend)
    {
        mpx = new Color32[MS * MS];
        mHalf = VR + 22f;
        mScale = MS / (mHalf * 2f);
        var rng = new System.Random(5);
        Color paper = new Color(0.86f, 0.77f, 0.58f), ink = new Color(0.25f, 0.16f, 0.09f);
        for (int y = 0; y < MS; y++) for (int x = 0; x < MS; x++)
        {
            float n = ((x * 7 + y * 13) % 17) / 17f * 0.04f + (float)rng.NextDouble() * 0.03f;
            float edge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(MS - 1 - x, MS - 1 - y)) / 60f;
            float dark = Mathf.Clamp01(1f - edge) * 0.35f;
            mpx[y * MS + x] = Color.Lerp(new Color(paper.r - n, paper.g - n, paper.b - n), new Color(0.45f, 0.32f, 0.18f), dark);
        }
        // tanah desa di dalam pagar
        MDisc(M2P(Vector2.zero), VR * mScale, new Color(0.62f, 0.74f, 0.45f));
        // jalan keluar gerbang
        MLine(M2P(new Vector2(0f, VR)), M2P(new Vector2(0f, mHalf)), 5.4f * mScale, new Color(0.93f, 0.86f, 0.68f));
        MLine(M2P(new Vector2(VR, 0f)), M2P(new Vector2(mHalf, 0f)), 5.4f * mScale, new Color(0.93f, 0.86f, 0.68f));
        // jalan di dalam desa
        for (int i = 0; i < vStreets.Count; i++)
        {
            float w = (i < 2 ? 5.4f : 3.8f) * mScale;
            for (int k = 0; k + 1 < vStreets[i].Count; k++) MLine(M2P(vStreets[i][k]), M2P(vStreets[i][k + 1]), w + 4f, ink * 0.8f);
        }
        for (int i = 0; i < vStreets.Count; i++)
        {
            float w = (i < 2 ? 5.4f : 3.8f) * mScale;
            for (int k = 0; k + 1 < vStreets[i].Count; k++) MLine(M2P(vStreets[i][k]), M2P(vStreets[i][k + 1]), w, new Color(0.93f, 0.86f, 0.68f));
        }
        // alun-alun
        MDisc(M2P(Vector2.zero), 14f * mScale, new Color(0.8f, 0.78f, 0.72f));
        MRing(M2P(Vector2.zero), 14f * mScale, 4f, ink);
        // pagar desa
        MRing(M2P(Vector2.zero), VR * mScale, 9f, new Color(0.38f, 0.24f, 0.12f));
        // bingkai peta
        MRing(new Vector2(MS / 2f, MS / 2f), MS * 0.5f + 40f, 6f, ink);
        // penanda
        legend = new List<string>();
        int num = 0;
        foreach (var it in vMap)
        {
            num++;
            Vector2 p = M2P(it.pos);
            Color c = MKindColor(it.kind);
            float r = it.kind == 6 ? 26f : 19f;
            if (it.kind == 2) MRect(p, r, r, ink); else MDisc(p, r + 3f, ink);
            if (it.kind == 2) MRect(p, r - 3f, r - 3f, c); else MDisc(p, r, c);
            MNumber(num, p, num > 9 ? 4 : 5, ink);
            legend.Add(num + ". " + it.name);
        }
        // mata angin: anak panah utara + huruf N
        Vector2 na = new Vector2(MS - 90f, MS - 120f);
        MLine(na + new Vector2(0f, -40f), na + new Vector2(0f, 40f), 7f, ink);
        for (int i = 0; i < 18; i++) MLine(na + new Vector2(0f, 46f), na + new Vector2(-14f + i * 1.6f, 22f), 3f, ink);
        MGlyphDraw(10, na + new Vector2(-12f, 96f), 8, ink);
        var tex = new Texture2D(MS, MS, TextureFormat.RGBA32, false);
        tex.SetPixels32(mpx);
        tex.Apply();
        return tex;
    }

    static void VBuildMapBoard()
    {
        List<string> legend;
        var tex = VRenderMap(out legend);
        if (!AssetDatabase.IsValidFolder("Assets/Textures")) AssetDatabase.CreateFolder("Assets", "Textures");
        string tp = AssetDatabase.GenerateUniqueAssetPath("Assets/Textures/DenahDesaPemula.png");
        File.WriteAllBytes(tp, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(tp, ImportAssetOptions.ForceSynchronousImport);
        var ti = AssetImporter.GetAtPath(tp) as TextureImporter;
        if (ti != null) { ti.wrapMode = TextureWrapMode.Clamp; ti.mipmapEnabled = true; ti.maxTextureSize = 1024; ti.SaveAndReimport(); }
        var tx = AssetDatabase.LoadAssetAtPath<Texture2D>(tp);
        Shader sh = GraphicsSettings.currentRenderPipeline != null ? Shader.Find("Universal Render Pipeline/Unlit") : Shader.Find("Unlit/Texture");
        if (sh == null) sh = Shader.Find("Unlit/Texture");
        var mat = new Material(sh);
        mat.mainTexture = tx;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tx);
        mat.name = "DenahDesaPemula";
        HeightBake.NewAsset(mat, MatDir + "/DenahDesaPemula.mat");

        Vector3 pos = VPol(352f, VR - 12f);
        var h = new GameObject("PapanDenahDesa").transform;
        h.SetParent(tProps, false);
        h.position = pos;
        h.rotation = Quaternion.Euler(0f, 180f, 0f);            // menghadap selatan (ke arah alun-alun)
        var b = new GeoBag();
        b.Cyl(Sl.Wood, new Vector3(-4.5f, 0f, 0f), 0.2f, 5.6f, 6, 0.18f, true, false);
        b.Cyl(Sl.Wood, new Vector3(4.5f, 0f, 0f), 0.2f, 5.6f, 6, 0.18f, true, false);
        b.Box(Sl.Wood, new Vector3(0f, 3.3f, -0.08f), new Vector3(9.6f, 5.4f, 0.14f));
        b.Box(Sl.WoodLight, new Vector3(0f, 3.3f, 0.02f), new Vector3(9.2f, 5.0f, 0.1f));
        b.Box(Sl.Roof, new Vector3(0f, 6.2f, 0f), new Vector3(10.4f, 0.18f, 1.4f));
        b.Emit(h, "Mesh", baseSlots);
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "PetaDenah";
        Object.DestroyImmediate(quad.GetComponent<MeshCollider>());
        quad.transform.SetParent(h, false);
        quad.transform.localPosition = new Vector3(-2.35f, 3.3f, 0.1f);
        quad.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        quad.transform.localScale = new Vector3(4.7f, 4.7f, 1f);
        quad.GetComponent<Renderer>().sharedMaterial = mat;
        BuildKit.Text(h, "DENAH DESA PEMULA", new Vector3(2.35f, 5.35f, 0.14f), 180f, 0.07f, new Color(1f, 0.9f, 0.55f));
        string leg = string.Join("\n", legend.ToArray());
        BuildKit.Text(h, leg, new Vector3(2.4f, 3.3f, 0.14f), 180f, 0.03f, new Color(1f, 0.96f, 0.82f));
        BoxCol(h, "Col", new Vector3(0f, 3f, 0f), new Vector3(9.6f, 5.6f, 0.5f), Vector3.zero);
        AddLight(h, new Vector3(0f, 4f, -2f), 10f, 1.0f);
        Reserve(pos.x, pos.z, 6f);
        var ip = VPoint<InfoPoint>(h, "InteractPoint_Denah", h.TransformPoint(new Vector3(0f, 0f, -2.6f)), "Lihat Denah Desa Pemula", 4f);
        ip.title = "Denah Desa Pemula";
        ip.message = "DENAH DESA PEMULA\n\n" + string.Join("\n", legend.ToArray()) +
            "\n\nKuning = rumah dijual. Biru = bangunan desa. Oranye = toko. Putih = gereja & makam. Hijau = ladang & latihan. Merah = gerbang.\nUtara ada di atas peta.";
    }
}
