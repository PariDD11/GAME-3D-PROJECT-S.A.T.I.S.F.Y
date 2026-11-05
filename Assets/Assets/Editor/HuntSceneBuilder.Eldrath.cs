using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Area hunting sekitar Kota Eldrath (masing-masing 1000 x 1000 m):
///   0 Hutan Perbatasan (jalan selatan, dua ujung: Eldrath dan Ashwatch), 1 Gunung Vulkanik (gerbang utara),
///   2 Padang Tulang (gerbang timur), 3 Lembah Iblis (gerbang barat).
/// Satu generator bertema: terrain bergelombang, tekstur prosedural, jalan setapak, base camp, landmark, monster, dan boss.
/// Koordinat = koordinat DESAIN (dicerminkan ke dunia seperti scene hunt lainnya).
/// </summary>
public static partial class HuntSceneBuilder
{
    const string EldrathTexDir = "Assets/Textures/Eldrath";
    static int ea;                    // area yang sedang dibangun
    static Material[] eSlots;         // slot material bertema
    static Material mLava;

    static readonly string[] EKeys = { "Frontier", "Volcano", "Bone", "Demon" };
    static readonly string[] ETitles = { "Hutan Perbatasan", "Gunung Vulkanik", "Padang Tulang", "Lembah Iblis" };
    public static string EldrathScenePath(int a) { return "Assets/Scenes/HuntArea_" + EKeys[a] + ".unity"; }

    static readonly Vector2 ECamp = new Vector2(0, -330);

    // ------------------------------------------------------------------ zona per area
    static ZoneDef[] EZones(int a)
    {
        switch (a)
        {
            case 0: return new[]
            {
                new ZoneDef { name = "Base Camp Perbatasan", x = 0, z = -330, r = 55, density = 0f },
                new ZoneDef { name = "Jalan Selatan Eldrath", x = 0, z = -190, r = 110, density = 0.5f },
                new ZoneDef { name = "Hutan Serigala", x = -230, z = -120, r = 120, density = 0.9f },
                new ZoneDef { name = "Sarang Ogre", x = 250, z = -90, r = 100, density = 0.5f },
                new ZoneDef { name = "Kamp Bandit Jalan Raya", x = -220, z = 140, r = 90, density = 0.4f },
                new ZoneDef { name = "Menara Pengawas Runtuh", x = 130, z = 230, r = 80, density = 0.3f },
                new ZoneDef { name = "Jalan Utara Ashwatch", x = 0, z = 380, r = 90, density = 0.4f },
            };
            case 1: return new[]
            {
                new ZoneDef { name = "Base Camp Kaki Gunung", x = 0, z = -330, r = 55, density = 0f },
                new ZoneDef { name = "Lereng Abu", x = 0, z = -190, r = 110, density = 0.3f },
                new ZoneDef { name = "Sungai Lava", x = -190, z = -20, r = 100, density = 0.2f },
                new ZoneDef { name = "Ngarai Belerang", x = 210, z = -10, r = 100, density = 0.2f },
                new ZoneDef { name = "Altar Pemuja Api", x = -200, z = 170, r = 80, density = 0.2f },
                new ZoneDef { name = "Bibir Kawah", x = 0, z = 190, r = 110, density = 0.1f },
                new ZoneDef { name = "Sarang Ignarok", x = 0, z = 310, r = 80, density = 0.05f },
            };
            case 2: return new[]
            {
                new ZoneDef { name = "Base Camp Medan Perang", x = 0, z = -330, r = 55, density = 0f },
                new ZoneDef { name = "Padang Tulang", x = 0, z = -170, r = 120, density = 0.3f },
                new ZoneDef { name = "Pemakaman Perang", x = -210, z = -40, r = 100, density = 0.3f },
                new ZoneDef { name = "Parit Tengkorak", x = 210, z = 0, r = 100, density = 0.25f },
                new ZoneDef { name = "Benteng Runtuh", x = -190, z = 170, r = 90, density = 0.2f },
                new ZoneDef { name = "Padang Hantu", x = 190, z = 190, r = 90, density = 0.2f },
                new ZoneDef { name = "Makam Raja Malrek", x = 0, z = 300, r = 85, density = 0.1f },
            };
            default: return new[]
            {
                new ZoneDef { name = "Base Camp Jurang", x = 0, z = -330, r = 55, density = 0f },
                new ZoneDef { name = "Gerbang Lembah", x = 0, z = -190, r = 110, density = 0.3f },
                new ZoneDef { name = "Sungai Api", x = 170, z = -30, r = 100, density = 0.2f },
                new ZoneDef { name = "Altar Ritual", x = -170, z = 80, r = 90, density = 0.2f },
                new ZoneDef { name = "Jurang Iblis", x = 190, z = 190, r = 90, density = 0.15f },
                new ZoneDef { name = "Dataran Retakan", x = -190, z = 230, r = 90, density = 0.15f },
                new ZoneDef { name = "Takhta Zargoth", x = 0, z = 320, r = 80, density = 0.05f },
            };
        }
    }

    static List<Vector2[]> ETrails(int a)
    {
        var l = new List<Vector2[]>();
        if (a == 0)
        {
            l.Add(new[] { V(0, -470), V(0, -380), V(0, -300), V(-25, -180), V(10, -60), V(40, 70), V(30, 190), V(0, 300), V(0, 470) });     // jalan raya Eldrath - Perbatasan
            l.Add(new[] { V(-25, -180), V(-120, -150), V(-230, -120) });
            l.Add(new[] { V(10, -60), V(120, -80), V(250, -90) });
            l.Add(new[] { V(40, 70), V(-80, 120), V(-220, 140) });
            l.Add(new[] { V(30, 190), V(90, 215), V(130, 230) });
        }
        else
        {
            l.Add(new[] { V(0, -470), V(0, -380), V(0, -330), V(10, -220), V(-10, -100), V(0, 20), V(10, 120), V(0, 220), V(0, 310) });      // jalur utama ke boss
            l.Add(new[] { V(-10, -100), V(-100, -60), V(-190, -20) });
            l.Add(new[] { V(-10, -100), V(100, -60), V(210, -10) });
            l.Add(new[] { V(10, 120), V(-100, 150), V(-200, 170) });
            l.Add(new[] { V(10, 120), V(100, 150), V(190, 190) });
        }
        return l;
    }

    // ------------------------------------------------------------------ tinggi
    static Vector2[] LavaRiver(int a)
    {
        if (a == 1) return new[] { V(-330, -60), V(-250, -10), V(-170, -20), V(-110, 40), V(-70, 110), V(-30, 160) };
        if (a == 3) return new[] { V(240, -200), V(190, -120), V(150, -50), V(190, 20), V(120, 90), V(70, 130), V(100, 190) };
        return new[] { V(9999, 9999), V(9999, 9998) };
    }

    static float EHeight(float x, float z)
    {
        float wx = x + (Noise(x, z, 0.004f, 3f) - 0.5f) * 120f;
        float wz = z + (Noise(x, z, 0.004f, 8f) - 0.5f) * 120f;
        float h = 0.30f + (Noise(wx, wz, 0.003f, 11f) - 0.5f) * 0.10f + (Noise(x, z, 0.012f, 5f) - 0.5f) * 0.035f + (Noise(x, z, 0.05f, 73f) - 0.5f) * 0.006f;
        float trail = DistToTrails(x, z);
        float onRoad = Smooth(1f - trail / 14f);
        // perbukitan besar (di luar area base camp dan jalan) supaya dataran tidak datar: kira-kira +-15 m
        {
            float midAll = Smooth((Dist(x, z, ECamp.x, ECamp.y) - 60f) / 100f);
            float hill = (Noise(x, z, 0.0045f, 90f) - 0.5f) * (ea == 0 ? 0.30f : ea == 1 ? 0.34f : ea == 2 ? 0.26f : 0.40f);
            hill += (Noise(x, z, 0.012f, 41f) - 0.5f) * 0.10f;
            hill += (Noise(x, z, 0.03f, 17f) - 0.5f) * 0.035f;
            h += hill * midAll * (1f - onRoad * 0.85f);
            h = Mathf.Max(h, 0.215f);          // lembah tidak tenggelam di bawah air/lava
        }
        if (ea == 0)
        {
            h += (Noise(x, z, 0.006f, 21f) - 0.4f) * 0.07f;
            float lake = Smooth(1f - Dist(x, z, 170f, 40f) / 75f);
            h = Mathf.Lerp(h, 0.17f, lake * 0.95f);
        }
        else if (ea == 1)
        {
            float cone = Smooth(1f - Dist(x, z, 0f, 215f) / 210f);
            h += cone * 0.42f + Mathf.Abs(Noise(x, z, 0.02f, 7f) - 0.5f) * 0.1f * (0.3f + cone);
            float crater = Smooth(1f - Dist(x, z, 0f, 215f) / 55f);
            h = Mathf.Lerp(h, 0.16f, crater);
            h = Mathf.Lerp(h, 0.30f, Smooth(1f - Dist(x, z, 0f, 310f) / 50f) * 0.9f);        // pelataran Ignarok
        }
        else if (ea == 2)
        {
            h += Mathf.Sin(x * 0.03f + Noise(x, z, 0.01f, 4f) * 4f) * 0.012f + (Noise(x, z, 0.02f, 2f) - 0.5f) * 0.03f;
            float pit = Smooth(1f - Dist(x, z, -210f, -40f) / 55f);
            h -= pit * 0.045f;
            float trench = Smooth(1f - DistSeg(x, z, 100f, -80f, 330f, 70f) / 16f);
            h -= trench * 0.05f;
            h += Smooth(1f - Dist(x, z, 0f, 300f) / 70f) * 0.04f;
        }
        else
        {
            float wall = Smooth((Mathf.Abs(x) - 250f) / 140f);
            h += wall * 0.30f + Mathf.Abs(Noise(x, z, 0.018f, 9f) - 0.5f) * 0.14f * wall;
            h += (Noise(x, z, 0.01f, 33f) - 0.5f) * 0.05f;
        }
        // relief tambahan: punggungan, lekukan, dan retakan agar dataran tidak datar
        if (ea >= 1)
        {
            float mid = Smooth((Dist(x, z, 0f, -330f) - 70f) / 90f);                       // jangan di sekitar base camp
            float ridge = Mathf.Abs(Noise(x, z, 0.011f, 77f) - 0.5f) * 2f;                 // 0 = punggungan
            h += (Noise(x, z, 0.007f, 55f) - 0.5f) * (ea == 3 ? 0.16f : ea == 2 ? 0.07f : 0.09f) * mid;
            h += (1f - ridge) * (1f - ridge) * (ea == 3 ? 0.07f : 0.04f) * mid * (1f - onRoad);
            h += (Noise(x, z, 0.035f, 14f) - 0.5f) * 0.03f * mid;
        }
        // sungai lava
        var lr = LavaRiver(ea);
        if (ea == 1 || ea == 3)
        {
            float rv = Smooth(1f - DistToPoly(x, z, lr) / 12f);
            h = Mathf.Lerp(h, 0.165f, rv * (1f - onRoad));
        }
        if (ea == 1)
        {
            // sungai lava kedua dan kolam-kolam lava (jalan tetap berupa jembatan tanah)
            float rv2 = Smooth(1f - DistToPoly(x, z, VolcanoRiver2) / 10f);
            h = Mathf.Lerp(h, 0.165f, rv2 * (1f - onRoad));
            for (int pi = 0; pi < VolcanoPools.Length; pi++)
            {
                var pl = VolcanoPools[pi];
                h = Mathf.Lerp(h, 0.16f, Smooth(1f - Dist(x, z, pl.x, pl.y) / pl.z) * (1f - onRoad));
            }
        }
        h = Mathf.Lerp(h, Mathf.Max(h * 0.4f + 0.18f, 0.285f), onRoad * 0.55f);       // jalan lebih rata dan di atas air/lava
        float edge = Smooth((Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) - 440f) / 50f);
        // koridor gerbang: tebing tepi dibuka di ujung yang tersambung ke kota (A: semua area, B: hanya Hutan Perbatasan)
        float gateK = Smooth(1f - Mathf.Max(0f, Mathf.Abs(x) - 18f) / 16f) * Smooth((Mathf.Abs(z) - 395f) / 25f);
        bool gateEnd = z < 0f || (z > 0f && ea == 0);
        if (gateEnd) { edge *= 1f - gateK; h = Mathf.Lerp(h, 0.3f, gateK); }
        h += edge * 0.7f;
        h = Mathf.Lerp(h, 0.30f, Smooth(1f - Dist(x, z, ECamp.x, ECamp.y) / 55f));
        if (ea == 0)
        {
            h = Plateau(h, x, z, V(-220, 140), 30f, 55f, 0.30f);
            h = Plateau(h, x, z, V(250, -90), 28f, 52f, 0.30f);
            h = Plateau(h, x, z, V(130, 230), 25f, 48f, 0.30f);
        }
        else if (ea == 1) h = Plateau(h, x, z, V(-200, 170), 32f, 58f, 0.30f);
        else if (ea == 2)
        {
            h = Plateau(h, x, z, V(-190, 170), 38f, 64f, 0.30f);
            h = Plateau(h, x, z, V(0, 300), 40f, 70f, 0.31f);
            h = Plateau(h, x, z, V(190, 190), 26f, 50f, 0.30f);
        }
        else
        {
            h = Plateau(h, x, z, V(-170, 80), 34f, 60f, 0.30f);
            h = Plateau(h, x, z, V(0, 320), 42f, 70f, 0.31f);
        }
        float endA = Smooth((-440f - z) / 40f), endB = ea == 0 ? Smooth((z - 440f) / 40f) : 0f;
        h = Mathf.Lerp(h, 0.30f, Mathf.Max(endA, endB));
        return Mathf.Clamp01(h);
    }

    // ------------------------------------------------------------------ tekstur dan layer
    /// <summary>Noise yang tileable: u,v dalam 0..P (lokal), off = pergeseran acak. Nilai di u=0 sama dengan di u=P (tanpa garis sambungan).</summary>
    static float TileNoise(float u, float v, float P, float off)
    {
        float a = Mathf.PerlinNoise(u + off, v + off), b = Mathf.PerlinNoise(u - P + off, v + off), c = Mathf.PerlinNoise(u + off, v - P + off), d = Mathf.PerlinNoise(u - P + off, v - P + off);
        float fx = u / P, fy = v / P;
        return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
    }

    static Texture2D ETex(string name, Color c, float var, int seed)
    {
        EnsureFolder(EldrathTexDir);
        string path = EldrathTexDir + "/" + name + ".png";
        const int N = 128;
        var t = new Texture2D(N, N, TextureFormat.RGB24, false);
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float n1 = TileNoise(x / (float)N * 6f, y / (float)N * 6f, 6f, 20f + seed);
                float n2 = TileNoise(x / (float)N * 18f, y / (float)N * 18f, 18f, 60f + seed * 2f);
                float k = 1f + (n1 - 0.5f) * var + (n2 - 0.5f) * var * 0.7f;
                t.SetPixel(x, y, new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k)));
            }
        t.Apply();
        File.WriteAllBytes(path, t.EncodeToPNG());
        Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(path);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static TerrainLayer[] EMakeLayers()
    {
        Color[][] pal =
        {
            new[] { new Color(0.26f, 0.46f, 0.18f), new Color(0.42f, 0.38f, 0.2f), new Color(0.5f, 0.5f, 0.5f), new Color(0.46f, 0.36f, 0.24f) },
            new[] { new Color(0.15f, 0.13f, 0.13f), new Color(0.38f, 0.15f, 0.08f), new Color(0.22f, 0.2f, 0.2f), new Color(0.3f, 0.23f, 0.19f) },
            new[] { new Color(0.55f, 0.53f, 0.46f), new Color(0.3f, 0.27f, 0.24f), new Color(0.5f, 0.5f, 0.52f), new Color(0.48f, 0.43f, 0.36f) },
            new[] { new Color(0.24f, 0.08f, 0.1f), new Color(0.14f, 0.08f, 0.17f), new Color(0.27f, 0.21f, 0.23f), new Color(0.38f, 0.15f, 0.11f) },
        };
        var layers = new TerrainLayer[4];
        for (int i = 0; i < 4; i++)
        {
            var layer = new TerrainLayer();
            layer.diffuseTexture = ETex("E" + ea + "_L" + i, pal[ea][i], i == 2 ? 0.55f : 0.4f, 11 + i * 5 + ea * 17);
            layer.tileSize = new Vector2(i == 2 ? 11f : 8f, i == 2 ? 11f : 8f);
            layer.smoothness = 0.04f;
            AssetDatabase.CreateAsset(layer, TerrDir + "/Layer_E" + ea + "_" + i + ".terrainlayer");
            layers[i] = layer;
        }
        return layers;
    }

    static void EPaint()
    {
        int res = td.alphamapResolution;
        var lr = LavaRiver(ea);
        var map = new float[res, res, 4];
        for (int zi = 0; zi < res; zi++)
            for (int xi = 0; xi < res; xi++)
            {
                float nx = xi / (res - 1f), nz = zi / (res - 1f);
                float wx = M * (Origin + nx * TSize), wz = M * (Origin + nz * TSize);
                float steep = td.GetSteepness(nx, nz);
                float rock = Smooth((steep - 22f) / 12f);
                float path = Smooth(1f - DistToTrails(wx, wz) / 3.6f);
                foreach (var c in clearings) path = Mathf.Max(path, Smooth(1f - Dist(wx, wz, c.x, c.y) / Mathf.Min(c.z, 16f)) * 0.55f);
                float alt = Smooth((Noise(wx, wz, 0.012f, 41f) - 0.48f) / 0.2f) * 0.85f;
                if (ea == 1) alt = Mathf.Max(alt, Smooth(1f - Dist(wx, wz, 0f, 215f) / 160f) * 0.8f);
                if (ea == 1 || ea == 3) alt = Mathf.Max(alt, Smooth(1f - DistToPoly(wx, wz, lr) / 40f) * 0.9f);
                if (ea == 1)
                {
                    alt = Mathf.Max(alt, Smooth(1f - DistToPoly(wx, wz, VolcanoRiver2) / 36f) * 0.9f);
                    for (int pi = 0; pi < VolcanoPools.Length; pi++) alt = Mathf.Max(alt, Smooth(1f - Dist(wx, wz, VolcanoPools[pi].x, VolcanoPools[pi].y) / (VolcanoPools[pi].z * 1.5f)) * 0.9f);
                }
                if (ea == 3) alt = Mathf.Max(alt, Smooth((Mathf.Abs(wx) - 240f) / 100f) * 0.8f);
                if (ea == 2) alt = Mathf.Max(alt, Smooth(1f - Dist(wx, wz, -210f, -40f) / 50f) * 0.9f);
                float rem = 1f;
                float wPath = Mathf.Min(rem, path); rem -= wPath;
                float wRock = Mathf.Min(rem, rock); rem -= wRock;
                float wAlt = rem * alt; rem -= wAlt;
                map[zi, xi, 0] = rem; map[zi, xi, 1] = wAlt; map[zi, xi, 2] = wRock; map[zi, xi, 3] = wPath;
            }
        alphaMap = map;
        td.SetAlphamaps(0, 0, map);
    }

    static void EDetail()
    {
        if (ea != 0) { td.detailPrototypes = new DetailPrototype[0]; return; }
        td.detailPrototypes = new[]
        {
            new DetailPrototype
            {
                usePrototypeMesh = false, prototypeTexture = ForestKit.TexGrassTuft(), renderMode = DetailRenderMode.GrassBillboard,
                minWidth = 0.9f, maxWidth = 1.7f, minHeight = 0.7f, maxHeight = 1.4f, noiseSpread = 0.35f,
                healthyColor = new Color(0.65f, 0.92f, 0.5f), dryColor = new Color(0.72f, 0.78f, 0.4f)
            }
        };
        int dr = 640;
        td.SetDetailResolution(dr, 16);
        var g = new int[dr, dr];
        int ar = td.alphamapResolution;
        var rnd = new System.Random(4141);
        for (int z = 0; z < dr; z++)
            for (int x = 0; x < dr; x++)
            {
                int ax = Mathf.Clamp(Mathf.RoundToInt(x / (dr - 1f) * (ar - 1)), 0, ar - 1);
                int az = Mathf.Clamp(Mathf.RoundToInt(z / (dr - 1f) * (ar - 1)), 0, ar - 1);
                if (rnd.NextDouble() < alphaMap[az, ax, 0] * 0.55) g[z, x] = 1;
            }
        td.SetDetailLayer(0, 0, 0, g);
    }

    static void EBuildTerrain()
    {
        td = new TerrainData();
        td.heightmapResolution = HRes;
        td.size = new Vector3(TSize, TH, TSize);
        HeightBake.NewAsset(td, TerrDir + "/Eldrath" + EKeys[ea] + "Terrain.asset");
        var hs = new float[HRes, HRes];
        float step = TSize / (HRes - 1);
        for (int zi = 0; zi < HRes; zi++)
            for (int xi = 0; xi < HRes; xi++)
                hs[zi, xi] = EHeight(M * (Origin + xi * step), M * (Origin + zi * step));
        td.SetHeights(0, 0, hs);
        {
            // cek medan: berapa persen area yang "datar" (selisih tinggi < 0.6 m dalam jendela 16 m) dan rentang tinggi
            int flat = 0, tot = 0; float mn = 9f, mx = -9f;
            for (int zi = 8; zi < HRes - 8; zi += 8)
                for (int xi = 8; xi < HRes - 8; xi += 8)
                {
                    float lo = 9f, hi = -9f;
                    for (int dz = -8; dz <= 8; dz += 4) for (int dx = -8; dx <= 8; dx += 4) { float v = hs[zi + dz, xi + dx]; lo = Mathf.Min(lo, v); hi = Mathf.Max(hi, v); }
                    mn = Mathf.Min(mn, hs[zi, xi]); mx = Mathf.Max(mx, hs[zi, xi]);
                    tot++; if ((hi - lo) * TH < 0.6f) flat++;
                }
            Debug.Log("[Medan] " + ETitles[ea] + ": datar " + Mathf.RoundToInt(100f * flat / Mathf.Max(1, tot)) + "% | tinggi " + (mn * TH).ToString("0") + ".." + (mx * TH).ToString("0") + " m");
        }
        td.terrainLayers = EMakeLayers();
        td.alphamapResolution = 512;
        EPaint();
        EDetail();
        var go = Terrain.CreateTerrainGameObject(td);
        go.name = EKeys[ea] + "Terrain";
        go.transform.SetParent(tRoot, false);
        go.transform.position = new Vector3(Origin, 0, Origin);
        terrain = go.GetComponent<Terrain>();
        HeightBake.Attach(go, td, hs, "Eldrath" + EKeys[ea] + "Terrain");
        terrain.detailObjectDistance = 70f;
        terrain.detailObjectDensity = 0.7f;
        terrain.heightmapPixelError = 6f;
        terrain.drawInstanced = true;
        EditorUtility.SetDirty(td);
    }

    // ------------------------------------------------------------------ material bertema
    static void EMakeSlots()
    {
        eSlots = (Material[])ForestKit.Slots.Clone();
        Color rock, stone, dark, dirt;
        if (ea == 1) { rock = new Color(0.13f, 0.11f, 0.11f); stone = new Color(0.2f, 0.16f, 0.15f); dark = new Color(0.09f, 0.08f, 0.08f); dirt = new Color(0.2f, 0.14f, 0.12f); }
        else if (ea == 2) { rock = new Color(0.72f, 0.7f, 0.63f); stone = new Color(0.62f, 0.6f, 0.55f); dark = new Color(0.36f, 0.34f, 0.32f); dirt = new Color(0.5f, 0.46f, 0.38f); }
        else if (ea == 3) { rock = new Color(0.22f, 0.1f, 0.12f); stone = new Color(0.28f, 0.14f, 0.16f); dark = new Color(0.1f, 0.05f, 0.08f); dirt = new Color(0.3f, 0.12f, 0.12f); }
        else return;
        eSlots[Sl.Stone] = Mat("E" + ea + "Stone", stone);
        eSlots[Sl.DarkStone] = Mat("E" + ea + "Dark", dark);
        eSlots[ForestKit.Rock] = Mat("E" + ea + "Rock", rock);
        eSlots[Sl.Dirt] = Mat("E" + ea + "Dirt", dirt);
        eSlots[Sl.Marble] = Mat("E" + ea + "Marble", Color.Lerp(stone, Color.white, 0.2f));
        if (ea == 1 || ea == 3)
        {
            var lava = TownMats.Emissive(Mat("E" + ea + "LavaGlow", new Color(1f, 0.35f, 0.05f)), new Color(2.2f, 0.7f, 0.1f));
            eSlots[ForestKit.Fire] = lava; eSlots[Sl.Glow] = lava;
        }
        if (ea == 3) eSlots[ForestKit.Crystal] = TownMats.Emissive(Mat("E3Crystal", new Color(0.6f, 0.1f, 0.8f)), new Color(1.4f, 0.2f, 2.0f));
    }

    static GameObject EKit(string key, System.Action<GeoBag> build, Transform parent, Vector3 pos, float yaw, float scale, bool collider)
    {
        var proto = PropKit.Get("E_" + key, build);
        var go = PropKit.Place(proto, parent, key, pos, yaw, scale, eSlots);
        if (collider) go.AddComponent<MeshCollider>().sharedMesh = proto.mesh;
        return go;
    }

    static void ELight(Transform parent, Vector3 worldPos, Color col, float range, float intensity, bool flicker = true)
    {
        var lg = new GameObject("Light").AddComponent<Light>();
        lg.transform.SetParent(parent, false);
        lg.transform.position = worldPos;
        lg.type = LightType.Point; lg.range = range; lg.intensity = intensity; lg.color = col;
        if (flicker) lg.gameObject.AddComponent<FlickerLight>();
    }

    // ------------------------------------------------------------------ build utama
    public static void BuildEldrathArea(int a)
    {
        ea = a;
        string path = EldrathScenePath(a);
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
        {
            if (!HGDlg.Show("Hunter Guild", EKeys[a] + " sudah ada: " + path + "\nMembangun ulang akan MENIMPA scene itu.\n\nLanjutkan?", "Timpa", "Batal")) return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BuildEldrathAreaCore(a, path);
        HGDlg.Show("Hunter Guild", ETitles[a] + " dibuat: " + path + "\nBangun ulang Kota Eldrath (Build Dungeon City) dan Town supaya gerbangnya aktif.", "OK");
    }

    /// <summary>Tanpa dialog: dipakai BUILD SEMUA SCENE.</summary>
    public static void BuildEldrathAreaCore(int a, string path)
    {
        ea = a;
        foreach (var f in new[] { "Assets/Scenes", "Assets/Materials", MatDir, "Assets/Meshes", MeshDir, "Assets/Prefabs", TerrDir, "Assets/Textures", TexDir, EldrathTexDir })
            EnsureFolder(f);
        EditorUtility.DisplayProgressBar("Hunter Guild", "Menyiapkan " + ETitles[a] + "...", 0.05f);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Random.InitState(7000 + a * 131);
        Zones = EZones(a);
        townSpawnName = a == 0 ? "SpawnFromTown" : "SpawnFromTown";
        clearings.Clear();
        plan.Clear();
        LoadMaterials();
        NpcKit.Reset();
        ForestKit.Reset();
        ForestKit.Load();
        EMakeSlots();
        trails = ETrails(a);

        tRoot = new GameObject("HuntArea" + EKeys[a]).transform;
        tCamp = Group("BaseCamp");
        tNodes = Group("GatherNodes");
        tRocks = Group("Rocks");
        tProps = Group("Props");

        clearings.Add(new Vector3(ECamp.x, ECamp.y, 42f));
        EditorUtility.DisplayProgressBar("Hunter Guild", "Membuat terrain...", 0.2f);
        EBuildTerrain();

        EditorUtility.DisplayProgressBar("Hunter Guild", "Menata landmark dan monster...", 0.6f);
        EBuildWater();
        EBuildCamp();
        EBuildLandmarks();
        EScatterProps();
        EAreaExtras();
        EBuildMonsters();
        EBuildEntrances();
        if (ea == 2) BoneCanon();            // kanon Padang Hantu (n87): tanpa UnityEngine.Random, jadi sebaran di atas tidak bergeser
        if (ea == 3) DemonCanon();           // kanon Lembah Iblis (n88): sama, tanpa UnityEngine.Random
        BuildPlayerAndManagers();

        var hm = Object.FindFirstObjectByType<HuntManager>();
        if (hm != null)
        {
            hm.defaultZone = ETitles[a];
            hm.returnScene = CityGate.DungeonCityScene;
            hm.townSpawn = a == 0 ? "SpawnFromTown" : "SpawnFrom" + EKeys[a];
        }
        EAtmosphere();
        MarkStatic(tRocks);

        EditorUtility.DisplayProgressBar("Hunter Guild", "Menyimpan scene...", 0.95f);
        AssetDatabase.SaveAssets();
        HeightBake.BackupScene(path); EditorSceneManager.SaveScene(scene, path);
        AddToBuildSettings("Assets/Scenes/Town.unity", true);
        AddToBuildSettings("Assets/Scenes/DungeonCity.unity", true);
        AddToBuildSettings(path, false);
        EditorUtility.ClearProgressBar();
    }

    static void EBuildWater()
    {
        Prim(PrimitiveType.Cube, "Liquid", tRoot, new Vector3(0, WaterY - 0.1f, 0), new Vector3(1000, 0.2f, 1000), mWater, false);
        if (ea == 1 || ea == 3)
        {
            mLava = TownMats.Emissive(Mat("E" + ea + "Lava", new Color(1f, 0.32f, 0.04f)), new Color(2.4f, 0.75f, 0.12f));
            tRoot.Find("Liquid").GetComponent<Renderer>().sharedMaterial = mLava;
            var lv = tRoot.Find("Liquid").gameObject;
            lv.name = "Lava";
        }
        else if (ea == 2) tRoot.Find("Liquid").gameObject.SetActive(false);
        foreach (float s in new[] { -1f, 1f })
        {
            Prim(PrimitiveType.Cube, "BoundX", tRoot, new Vector3(497f * s, 60, 0), new Vector3(6, 200, 1000), mStone, false).GetComponent<Renderer>().enabled = false;
            Prim(PrimitiveType.Cube, "BoundZ", tRoot, new Vector3(0, 60, 497f * s), new Vector3(1000, 200, 6), mStone, false).GetComponent<Renderer>().enabled = false;
        }
        foreach (var c in tRoot.GetComponentsInChildren<Transform>())
            if (c.name.StartsWith("Bound") && c.GetComponent<Collider>() == null) c.gameObject.AddComponent<BoxCollider>();
    }

    static void EBuildCamp()
    {
        Vector3 c = G(ECamp.x, ECamp.y);
        for (int i = 0; i < 4; i++)
        {
            float ang = (i * 90f + 45f) * Mathf.Deg2Rad;
            int cs = i % 2 == 0 ? Sl.Cloth : Sl.ClothB;
            Vector3 p = Gw(c.x + Mathf.Sin(ang) * 12f, c.z + Mathf.Cos(ang) * 12f);
            var go = EKit("ETent_" + cs, b => ForestKit.Tent(b, 1, cs), tCamp, p, i * 90f + 45f, 1.3f, false);
            var bc = go.AddComponent<BoxCollider>();
            bc.center = new Vector3(0, 0.85f, 0); bc.size = new Vector3(2.0f, 1.7f, 3.0f);
        }
        var fire = new GameObject("Campfire").transform;
        fire.SetParent(tCamp, false);
        fire.position = Gw(c.x, c.z);
        EKit("ECampfire", b => ForestKit.Campfire(b, 3), fire, fire.position, 0f, 1.25f, false);
        ELight(fire, fire.position + Vector3.up * 1.5f, new Color(1f, 0.6f, 0.25f), 22f, 1.8f);
        EKit("ECrate", b => ForestKit.Crate(b, 2), tCamp, Gw(c.x + 5f, c.z + 3f), 20f, 1.4f, true);
        EKit("EBarrel", b => ForestKit.Barrel(b), tCamp, Gw(c.x + 4.2f, c.z + 5.4f), 0f, 1.3f, true);
        if (ea == 0)
        {
            // penjaga perbatasan: mengantar gratis ke Ashwatch (menggantikan gerbang yang dihapus)
            NpcKit.Reset();
            var gspec = NpcKit.RandomSpec(NpcKit.OPenjaga, new System.Random(1234));
            gspec.elite = true;
            Vector3 gp2 = Gw(c.x - 6f, c.z + 8f);
            var gnpc = NpcKit.Spawn(tCamp, "NPC_PenjagaPerbatasan", gp2, 200f, gspec, true);
            gnpc.transform.position = gp2;
            var gact = gnpc.GetComponent<NpcActor>();
            if (gact != null) gact.displayName = "Penjaga Perbatasan";
            BuildKit.Text(gnpc.transform, "Penjaga Perbatasan\n<Antar ke Ashwatch>", new Vector3(0f, (gact != null ? gact.height : 1.8f) + 0.3f, 0f), 0f, 0.045f, Color.white);
            var oldChat = gnpc.GetComponent<NpcChat>();
            if (oldChat != null) Object.DestroyImmediate(oldChat);          // dialog umum NPC tidak boleh merebut interaksi antar
            var gip = new GameObject("InteractPoint").transform;
            gip.SetParent(gnpc.transform, false);
            gip.localPosition = Vector3.zero;
            var esc = gip.gameObject.AddComponent<TownEscort>();
            esc.prompt = "Bicara: Penjaga Perbatasan";
            esc.radius = 3.6f;
        }
        var sign = new GameObject("TrailSign").transform;
        sign.SetParent(tCamp, false);
        sign.position = Gw(c.x, c.z + 22f);
        Prim(PrimitiveType.Cube, "Post", sign, new Vector3(0, 1.5f, 0), new Vector3(0.3f, 3f, 0.3f), mWood);
        Prim(PrimitiveType.Cube, "Board", sign, new Vector3(0, 2.7f, 0), new Vector3(2.6f, 0.9f, 0.15f), mDark, false);
        var ip = new GameObject("BaseCamp_Interact");
        ip.transform.SetParent(tCamp, false);
        ip.transform.position = Gw(c.x, c.z + 5.5f);
        var bcm = ip.AddComponent<BaseCamp>();
        bcm.prompt = "Base Camp";
        bcm.radius = 5f;
        Prim(PrimitiveType.Cylinder, "Marker", ip.transform, new Vector3(0, 0.05f, 0), new Vector3(2.6f, 0.03f, 2.6f), mGold, false);
        // pedagang kemah (toko umum) dan pandai besi lapangan: crafting, enchant, dan imbue juga bisa dilakukan di base camp
        foreach (var f in new[] { 0, 1 })
        {
            Vector3 sp = Gw(c.x + (f == 0 ? -9f : -14f), c.z + (f == 0 ? 9f : 3f));
            var stall = new GameObject(f == 0 ? "PedagangKemah" : "PandaiLapangan").transform;
            stall.SetParent(tCamp, false);
            stall.position = sp;
            Prim(PrimitiveType.Cube, "Meja", stall, new Vector3(0, 0.55f, 0), new Vector3(2.8f, 1.1f, 1.2f), mWood);
            Prim(PrimitiveType.Cube, "Atap", stall, new Vector3(0, 2.6f, 0), new Vector3(3.4f, 0.12f, 2.2f), f == 0 ? mGold : mDark, false);
            foreach (float sx in new[] { -1.5f, 1.5f }) Prim(PrimitiveType.Cube, "Tiang", stall, new Vector3(sx, 1.3f, 0.8f), new Vector3(0.12f, 2.6f, 0.12f), mWood, false);
            if (f == 1) Prim(PrimitiveType.Sphere, "Bara", stall, new Vector3(0.9f, 1.25f, 0f), new Vector3(0.45f, 0.3f, 0.45f), mGlow, false);
            var ipo = new GameObject("InteractPoint").transform;
            ipo.SetParent(stall, false);
            ipo.localPosition = new Vector3(0f, 0f, 2.2f);
            if (f == 0) { var sh = ipo.gameObject.AddComponent<GeneralShop>(); sh.prompt = "Pedagang Kemah"; sh.radius = 3.4f; }
            else { var bs = ipo.gameObject.AddComponent<Blacksmith>(); bs.prompt = "Pandai Besi Lapangan"; bs.radius = 3.4f; }
        }
        if (ea == 0 || ea == 2) MakeSpring(Gw(c.x + 21f, c.z + 9f), "Mata Air");
        else
        {
            // air dibawa dari kemah (tidak ada air alami): mata air pegunungan kecil
            MakeSpring(Gw(c.x + 21f, c.z + 9f), "Mata Air Dingin");
        }
        var rsp = new GameObject("RespawnPoint").transform;
        rsp.SetParent(tRoot, false);
        rsp.position = G(ECamp.x, ECamp.y - 10f);
    }

    // ------------------------------------------------------------------ landmark bertema
    static void Place3(string key, System.Action<GeoBag> build, Vector2 d, float yaw, float scale, bool col, Transform parent = null)
    {
        EKit(key, build, parent != null ? parent : tProps, G(d.x, d.y), yaw, scale, col);
    }


    static void ERuinRing(Vector2 c, int n, float r, int seed, float brokenness)
    {
        for (int i = 0; i < n; i++)
        {
            Vector2 p = Pt(c, i * 360f / n, r);
            int sd = seed + i;
            EKit("ECol_" + sd, b => ForestKit.Column(b, Vector3.zero, 6f, 0.7f, sd, brokenness * ((i % 3) / 2f)), tProps, G(p.x, p.y), i * 40f, 1f, true);
        }
    }

    static void EFirepit(Vector2 c, Color col, float range, float intensity)
    {
        var g = new GameObject("Firepit").transform;
        g.SetParent(tProps, false);
        Vector3 p = G(c.x, c.y);
        g.position = p;
        EKit("EPit", b => ForestKit.Campfire(b, 5), g, p, 0f, 1.1f, false);
        ELight(g, p + Vector3.up * 1.8f, col, range, intensity);
    }

    static void ECampSite(Vector2 c, int tents, float r, string label)
    {
        clearings.Add(new Vector3(c.x, c.y, r + 12f));
        for (int i = 0; i < tents; i++)
        {
            Vector2 p = Pt(c, i * 360f / tents + 20f, r);
            int cs = i % 2 == 0 ? Sl.Cloth : Sl.ClothB;
            var go = EKit("ETentS_" + cs, b => ForestKit.Tent(b, 2, cs), tProps, G(p.x, p.y), i * 360f / tents + 200f, 1.2f, false);
            var bc = go.AddComponent<BoxCollider>();
            bc.center = new Vector3(0, 0.85f, 0); bc.size = new Vector3(2.0f, 1.7f, 3.0f);
        }
        EFirepit(c, new Color(1f, 0.6f, 0.25f), 20f, 1.5f);
        for (int i = 0; i < 8; i++)
        {
            if (i == 3 || i == 4) continue;
            Vector2 p = Pt(c, i * 45f, r + 10f);
            EKit("EFence", b => ForestKit.Fence(b, 3, 4f, 0.2f), tProps, G(p.x, p.y), i * 45f + 90f, 1.2f, false);
        }
    }

    static void EBuildLandmarks()
    {
        switch (ea)
        {
            case 0:
                ECampSite(V(-220, 140), 5, 14f, "Bandit");
                ECampSite(V(250, -90), 4, 14f, "Ogre");
                for (int i = 0; i < 5; i++) { Vector2 p = Pt(V(250, -90), i * 72f + 10f, 24f); Place3("EBones" + i, b => ForestKit.BeastSkeleton(b, 3), p, i * 70f, 1.3f, false); }
                clearings.Add(new Vector3(130, 230, 28f));
                Place3("EWatch", b => ForestKit.RuinTower(b, 4), V(130, 230), 20f, 1.1f, true);
                ELight(tProps, G(130, 230) + Vector3.up * 3f, new Color(1f, 0.7f, 0.4f), 18f, 1.1f);
                for (int i = 0; i < 4; i++) { Vector2 p = Pt(V(130, 230), i * 90f + 45f, 15f); Place3("EWatchRock" + i, b => ForestKit.Boulder(b, i, 1f, true), p, i * 50f, 1.8f, true); }
                // pos jaga di sepanjang jalan raya
                foreach (var pz in new[] { -420f, -250f, 0f, 150f, 350f })
                {
                    Vector2 p = V(14f, pz);
                    var lp = new GameObject("TiangObor").transform; lp.SetParent(tProps, false); lp.position = G(p.x, p.y);
                    Prim(PrimitiveType.Cube, "Tiang", lp, new Vector3(0, 1.6f, 0), new Vector3(0.2f, 3.2f, 0.2f), mWood, false);
                    Prim(PrimitiveType.Sphere, "Api", lp, new Vector3(0, 3.3f, 0), new Vector3(0.45f, 0.5f, 0.45f), mGlow, false);
                    ELight(lp, lp.position + Vector3.up * 3.4f, new Color(1f, 0.65f, 0.3f), 14f, 1.2f);
                }
                break;
            case 1:
            {
                // kawah dan sarang Ignarok
                clearings.Add(new Vector3(0, 310, 40f));
                for (int i = 0; i < 6; i++) { Vector2 p = Pt(V(0, 310), i * 60f, 30f); Place3("EObel", b => ForestKit.Obelisk(b, 7, 0.3f, 12f), p, i * 60f, 1f, true); ELight(tProps, G(p.x, p.y) + Vector3.up * 8f, new Color(1f, 0.4f, 0.1f), 24f, 1.5f); }
                // altar pemuja api
                clearings.Add(new Vector3(-200, 170, 36f));
                ERuinRing(V(-200, 170), 8, 34f, 11, 0.7f);
                EFirepit(V(-200, 170), new Color(1f, 0.35f, 0.1f), 26f, 2.2f);
                // cahaya lava di sepanjang sungai dan kawah
                var lr = LavaRiver(1);
                for (int i = 0; i < lr.Length; i++) ELight(tProps, G(lr[i].x, lr[i].y) + Vector3.up * 3f, new Color(1f, 0.4f, 0.08f), 40f, 2f, false);
                ELight(tProps, G(0, 215) + Vector3.up * 8f, new Color(1f, 0.4f, 0.08f), 90f, 3f, false);
                clearings.Add(new Vector3(210, -10, 25f));
                for (int i = 0; i < 7; i++) { Vector2 p = Pt(V(210, -10), i * 51f, 18f); Place3("EHoodooV" + i, b => ForestKit.Hoodoo(b, i + 3, 7f), p, i * 33f, 1.3f, true); }
                VolcanoExtras();
                break;
            }
            case 2:
            {
                clearings.Add(new Vector3(0, 300, 52f));
                ERuinRing(V(0, 300), 10, 30f, 31, 0.6f);
                Place3("EKingGate", b => ForestKit.Archway(b, 5, 0.2f), V(0, 262), 0f, 1.4f, true);
                Place3("EKingObel", b => ForestKit.Obelisk(b, 9, 0.1f, 14f), V(0, 318), 0f, 1.1f, true);
                for (int i = 0; i < 4; i++) ELight(tProps, G(Pt(V(0, 300), i * 90f + 45f, 26f).x, Pt(V(0, 300), i * 90f + 45f, 26f).y) + Vector3.up * 3f, new Color(0.45f, 0.75f, 1f), 22f, 1.6f);
                clearings.Add(new Vector3(-190, 170, 44f));
                Place3("EFort", b => ForestKit.RuinTower(b, 6), V(-190, 170), 40f, 1.3f, true);
                ERuinRing(V(-190, 170), 6, 26f, 71, 0.9f);
                for (int i = 0; i < 40; i++) { Vector2 p = V(Random.Range(-260f, -100f), Random.Range(-120f, 40f)); if (NearClearing(p.x, p.y) || GroundY(p.x, p.y) < WaterY + 1f) continue; int sd = i; Place3("EGrave" + (i % 6), b => ForestKit.Gravestone(b, sd % 6, sd % 2 == 0), p, Random.Range(0f, 360f), 1.3f, false); }
                clearings.Add(new Vector3(190, 190, 28f));
                for (int i = 0; i < 6; i++) { Vector2 p = Pt(V(190, 190), i * 60f, 16f); Place3("EBoneP" + i, b => ForestKit.BeastSkeleton(b, i), p, i * 61f, 1.5f, false); }
                break;
            }
            default:
            {
                clearings.Add(new Vector3(0, 320, 55f));
                ERuinRing(V(0, 320), 8, 32f, 91, 0.3f);
                Place3("EThroneGate", b => ForestKit.Archway(b, 8, 0f), V(0, 280), 0f, 1.6f, true);
                for (int i = 0; i < 6; i++) { Vector2 p = Pt(V(0, 320), i * 60f + 30f, 24f); Place3("EDObel" + i, b => ForestKit.Obelisk(b, 12, 0f, 13f), p, i * 60f, 1f, true); ELight(tProps, G(p.x, p.y) + Vector3.up * 8f, new Color(0.75f, 0.15f, 0.9f), 26f, 1.7f); }
                clearings.Add(new Vector3(-170, 80, 42f));
                ERuinRing(V(-170, 80), 7, 24f, 41, 0.4f);
                EFirepit(V(-170, 80), new Color(0.75f, 0.2f, 0.95f), 28f, 2.4f);
                var lr = LavaRiver(3);
                for (int i = 0; i < lr.Length; i++) ELight(tProps, G(lr[i].x, lr[i].y) + Vector3.up * 3f, new Color(1f, 0.3f, 0.08f), 40f, 2f, false);
                clearings.Add(new Vector3(190, 190, 26f));
                for (int i = 0; i < 6; i++) { Vector2 p = Pt(V(190, 190), i * 60f, 18f); Place3("EDHood" + i, b => ForestKit.Hoodoo(b, i + 2, 8f), p, i * 41f, 1.4f, true); }
                break;
            }
        }
    }

    // ------------------------------------------------------------------ pohon, batu, node
    static void EScatterProps()
    {
        int trees = ea == 0 ? 2600 : ea == 1 ? 400 : ea == 2 ? 480 : 440;
        int made = 0;
        for (int i = 0; i < trees * 8 && made < trees; i++)
        {
            float x = Random.Range(-440f, 440f), z = Random.Range(-440f, 440f);
            var zd = ZoneAt(x, z);
            float dens = zd != null ? zd.density : (ea == 0 ? 0.45f : 0.25f);
            if (Random.value > dens + (ea == 0 ? 0.40f : 0.16f)) continue;
            if (GroundY(x, z) < WaterY + 0.8f || NearClearing(x, z) || DistToTrails(x, z) < 6f || Slope(x, z) > 24f) continue;
            if (ea == 1 && Dist(x, z, 0, 215) < 120f) continue;
            int v = Random.Range(0, 4);
            GameObject go;
            if (ea == 0 && v < 2) go = EKit("EOak" + v, b => ForestKit.Oak(b, v + 20), tProps, G(x, z), Random.Range(0f, 360f), Random.Range(0.9f, 1.4f), false);
            else if (ea == 0 && v == 2) go = EKit("ESpruce", b => ForestKit.Spruce(b, 25), tProps, G(x, z), Random.Range(0f, 360f), Random.Range(0.9f, 1.4f), false);
            else if (ea == 0) go = EKit("EBirch", b => ForestKit.Birch(b, 26), tProps, G(x, z), Random.Range(0f, 360f), Random.Range(0.9f, 1.3f), false);
            else go = EKit("EDead" + v, b => ForestKit.DeadTree(b, v + 30), tProps, G(x, z), Random.Range(0f, 360f), Random.Range(0.9f, 1.5f), false);
            var cc = go.AddComponent<CapsuleCollider>(); cc.radius = 0.5f; cc.height = 5f; cc.center = new Vector3(0, 2.5f, 0);
            made++;
        }
        made = 0;
        int rocks = ea == 0 ? 320 : 760;
        for (int i = 0; i < 4000 && made < rocks; i++)
        {
            float x = Random.Range(-440f, 440f), z = Random.Range(-440f, 440f);
            if (GroundY(x, z) < WaterY + 0.3f || NearClearing(x, z) || DistToTrails(x, z) < 3f) continue;
            int v = Random.Range(0, 4);
            var go = EKit("EBoulder_" + v, b => ForestKit.Boulder(b, v, 1f, ea == 0), tRocks, G(x, z), Random.Range(0f, 360f), Random.Range(0.7f, 2.6f), false);
            go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
            made++;
        }
        if (ea == 2)
        {
            made = 0;
            for (int i = 0; i < 3000 && made < 24; i++)
            {
                float x = Random.Range(-440f, 440f), z = Random.Range(-440f, 440f);
                if (GroundY(x, z) < WaterY + 0.5f || NearClearing(x, z) || DistToTrails(x, z) < 8f || Slope(x, z) > 14f) continue;
                EKit("EBones", b => ForestKit.BeastSkeleton(b, 3), tRocks, G(x, z), Random.Range(0f, 360f), Random.Range(1f, 1.6f), false);
                made++;
            }
        }
        // spire / formasi khas area di seluruh dataran
        if (ea >= 1)
        {
            int spires = ea == 1 ? 140 : ea == 2 ? 110 : 160;
            made = 0;
            for (int i = 0; i < 6000 && made < spires; i++)
            {
                float x = Random.Range(-440f, 440f), z = Random.Range(-440f, 440f);
                if (GroundY(x, z) < WaterY + 0.8f || NearClearing(x, z) || DistToTrails(x, z) < 7f || Slope(x, z) > 30f) continue;
                int v = Random.Range(0, 5);
                GameObject go;
                if (ea == 2) go = EKit("ESpireB" + v, b => ForestKit.Obelisk(b, 40 + v, 0.6f, Random.Range(5f, 9f)), tRocks, G(x, z), Random.Range(0f, 360f), Random.Range(0.8f, 1.5f), false);
                else go = EKit("ESpire" + ea + "_" + v, b => ForestKit.Hoodoo(b, 50 + v + ea * 7, Random.Range(6f, 14f)), tRocks, G(x, z), Random.Range(0f, 360f), Random.Range(0.9f, 1.8f), false);
                go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
                made++;
            }
            // kerangka, tulang, dan puing kecil
            made = 0;
            for (int i = 0; i < 4000 && made < (ea == 2 ? 110 : 60); i++)
            {
                float x = Random.Range(-440f, 440f), z = Random.Range(-440f, 440f);
                if (GroundY(x, z) < WaterY + 0.5f || NearClearing(x, z) || DistToTrails(x, z) < 5f || Slope(x, z) > 18f) continue;
                EKit("EBonesX", b => ForestKit.BeastSkeleton(b, 7), tRocks, G(x, z), Random.Range(0f, 360f), Random.Range(0.9f, 1.8f), false);
                made++;
            }
            // pelita kecil menyebar (api/kristal) supaya dataran tidak gelap-kosong
            for (int i = 0; i < 64; i++)
            {
                float x = Random.Range(-420f, 420f), z = Random.Range(-420f, 420f);
                if (GroundY(x, z) < WaterY + 0.5f || NearClearing(x, z)) continue;
                Color lc = ea == 1 ? new Color(1f, 0.45f, 0.12f) : ea == 2 ? new Color(0.45f, 0.75f, 1f) : new Color(0.8f, 0.2f, 0.9f);
                ELight(tProps, G(x, z) + Vector3.up * 2.5f, lc, 22f, 1.6f);
            }
        }
        // pemandangan dekat pintu masuk: rapat di sekitar jalan awal (z -470..-230) supaya kesan pertama tidak kosong
        {
            int near = 0;
            for (int i = 0; i < 6000 && near < 150; i++)
            {
                float x = Random.Range(-170f, 170f), z = Random.Range(-465f, -235f);
                if (GroundY(x, z) < WaterY + 0.5f || NearClearing(x, z) || DistToTrails(x, z) < 7f || Mathf.Abs(x) < 20f && z < -440f) continue;
                GameObject go;
                int v = Random.Range(0, 5);
                if (ea == 0) go = EKit(v < 3 ? "EOak" + (v % 2) : "ESpruce", b => { if (v < 3) ForestKit.Oak(b, (v % 2) + 20); else ForestKit.Spruce(b, 25); }, tProps, G(x, z), Random.Range(0f, 360f), Random.Range(0.9f, 1.4f), false);
                else if (ea == 2) go = EKit("ESpireB" + v, b => ForestKit.Obelisk(b, 40 + v, 0.6f, Random.Range(5f, 9f)), tRocks, G(x, z), Random.Range(0f, 360f), Random.Range(0.8f, 1.5f), false);
                else go = EKit("ESpire" + ea + "_" + v, b => ForestKit.Hoodoo(b, 50 + v + ea * 7, Random.Range(6f, 14f)), tRocks, G(x, z), Random.Range(0f, 360f), Random.Range(0.9f, 1.8f), false);
                var mc = go.GetComponent<MeshFilter>();
                if (mc != null) go.AddComponent<MeshCollider>().sharedMesh = mc.sharedMesh;
                near++;
            }
        }
        // bahan gathering
        Scatter("herb", ea == 0 ? 40 : 18, new[] { 1, 2, 3 });
        Scatter("wild_berry", ea == 0 ? 24 : 6, new[] { 1, 2 });
        if (ea == 0)
        {
            Vector3? lake = Shore(V(170, 40), V(-1, 0), 120f);
            if (lake.HasValue) MakeSpring(lake.Value, "Air Danau", false);
        }
    }

    // ------------------------------------------------------------------ monster
    static void EBuildMonsters()
    {
        var root = Group("Monsters");
        switch (ea)
        {
            case 0:
                SpawnGroup(root, "road_wolf", -230, -120, 100, 10);
                SpawnGroup(root, "road_wolf", 0, -200, 90, 6);
                SpawnGroup(root, "road_wolf", 0, 330, 90, 6);
                SpawnRing(root, "highway_bandit", V(-220, 140), 22f, 8, 10f);
                SpawnRing(root, "highway_archer", V(-220, 140), 16f, 4, 50f);
                SpawnGroup(root, "highway_bandit", 40, 70, 60, 4);
                SpawnRing(root, "ogre", V(250, -90), 26f, 4, 30f);
                SpawnMonsterAt(root, "behemoth", G(250, -90));
                SpawnMonsterAt(root, "ogre_chief", G(130, 230));
                SpawnRing(root, "highway_bandit", V(130, 230), 18f, 4, 0f);
                SpawnGroup(root, "ogre", 90, 120, 50, 2);
                break;
            case 1:
                SpawnGroup(root, "lava_lizard", -190, -20, 90, 8);
                SpawnGroup(root, "ember_imp", 0, -190, 100, 8);
                SpawnGroup(root, "ember_imp", 210, -10, 80, 6);
                SpawnRing(root, "fire_cultist", V(-200, 170), 31f, 6, 15f);
                SpawnRing(root, "ash_wraith", V(0, 190), 60f, 6, 0f);
                SpawnGroup(root, "ash_wraith", 210, -10, 70, 3);
                SpawnMonsterAt(root, "magma_golem", G(210, -10));
                SpawnMonsterAt(root, "magma_golem", G(-190, 40));
                SpawnMonsterAt(root, "magma_golem", G(70, 120));
                SpawnMonsterAt(root, "ignarok", G(0, 310));
                break;
            case 2:
                SpawnGroup(root, "bone_soldier", 0, -170, 110, 10);
                SpawnGroup(root, "bone_soldier", -210, -40, 90, 8);
                SpawnGroup(root, "bone_archer", 210, 0, 90, 6);
                SpawnRing(root, "banshee", V(190, 190), 22f, 5, 0f);
                SpawnRing(root, "bone_soldier", V(-190, 170), 24f, 6, 20f);
                SpawnMonsterAt(root, "grave_knight", G(-190, 170));
                SpawnMonsterAt(root, "grave_knight", G(150, 60));
                SpawnMonsterAt(root, "grave_knight", G(-30, 20));
                SpawnRing(root, "bone_archer", V(0, 300), 34f, 6, 30f);
                SpawnRing(root, "bone_soldier", V(0, 300), 22f, 6, 0f);
                SpawnMonsterAt(root, "bone_king", G(0, 300));
                SpawnMonsterAt(root, "cerberus", G(0, 250));
                break;
            default:
                SpawnGroup(root, "imp", 0, -190, 100, 10);
                SpawnGroup(root, "hellhound", 170, -30, 90, 7);
                SpawnGroup(root, "hellhound", -190, 230, 90, 5);
                SpawnRing(root, "dark_cultist", V(-170, 80), 22f, 7, 15f);
                SpawnGroup(root, "imp", 190, 190, 80, 6);
                SpawnMonsterAt(root, "demon_brute", G(190, 190));
                SpawnMonsterAt(root, "demon_brute", G(-170, 80));
                SpawnMonsterAt(root, "demon_brute", G(60, 80));
                SpawnRing(root, "dark_cultist", V(0, 320), 36f, 6, 30f);
                SpawnMonsterAt(root, "demon_lord", G(0, 320));
                break;
        }
    }

    // ------------------------------------------------------------------ pintu masuk dan keluar
    static void EMark(string name, Vector3 pos, float yaw)
    {
        var sp = new GameObject(name).transform;
        sp.SetParent(tRoot, false);
        sp.position = pos;
        sp.rotation = Quaternion.Euler(0, yaw, 0);
    }

    static void EGateZone(string name, Vector3 pos, string returnScene, string spawn, bool safe)
    {
        var tr = new GameObject(name).transform;
        tr.SetParent(tRoot, false);
        tr.position = pos;
        var at = tr.gameObject.AddComponent<AreaTransition>();
        at.mode = AreaTransition.Mode.ToTown;
        at.returnScene = returnScene;
        at.townSpawn = spawn;
        at.safeTravel = safe;
        at.size = new Vector3(40f, 14f, 4f);
    }

    static void EBuildEntrances()
    {
        // ujung A (design z = -470): kembali ke Kota Eldrath. Pemain muncul menghadap ke dalam area (arah -Z dunia).
        spawnPos = G(0f, -430f);
        EMark("SpawnFromTown", spawnPos, 180f);
        EMark("SpawnFromEldrath", spawnPos, 180f);
        string spn = ea == 0 ? "SpawnFromTown" : "SpawnFrom" + EKeys[ea];
        EGateZone("ToEldrath", G(0f, -465f), CityGate.DungeonCityScene, spn, ea == 0);
        Barrier(G(0f, -480f), new Vector3(60f, 14f, 2f));
        if (ea == 0)
        {
            // ujung B (design z = +470): jalan ke Ashwatch
            EMark("SpawnFromBorder", G(0f, 430f), 0f);
            EGateZone("ToAshwatch", G(0f, 465f), "Town", "SpawnFromDungeon", true);        // jalan raya ke Ashwatch (aman, tanpa quest)
            Barrier(G(0f, 480f), new Vector3(60f, 14f, 2f));
        }
        // gerbang batu di mulut koridor (kelihatan dari dalam area)
        EEldrathGateway(true, 2, "KOTA ELDRATH");
        EEntrancePlaza();
        // papan penunjuk
        string label = ea == 0 ? "KOTA ELDRATH" : "KOTA ELDRATH";
        var sg = new GameObject("PapanArah").transform;
        sg.SetParent(tRoot, false);
        sg.position = G(10f, -440f);
        Prim(PrimitiveType.Cube, "Tiang", sg, new Vector3(0, 1.5f, 0), new Vector3(0.25f, 3f, 0.25f), mWood);
        Prim(PrimitiveType.Cube, "Papan", sg, new Vector3(0, 2.7f, 0), new Vector3(5.2f, 0.9f, 0.15f), mDark, false);
        BuildKit.Text(sg, label, new Vector3(0, 2.7f, -0.2f), 0f, 0.085f, new Color(1f, 0.85f, 0.45f));
    }

    /// <summary>Gerbang bergaya Kota Eldrath: GateKit asli, tembok + menara, jalan batu, lampu kristal, sipres.</summary>
    static void EEldrathGateway(bool endA, int tier, string sign)
    {
        TownMats.Load();
        DungeonKit.Load();
        GateKit.Reset();
        PropKit.Reset();
        Material[] sl = tier == 2 ? DungeonKit.Slots(0) : GateKit.SlotsFor(tier);
        float sgn = endA ? -1f : 1f;
        float inward = M * (endA ? 1f : -1f);
        float yaw = inward > 0f ? 0f : 180f;
        Vector3 gp = G(0f, sgn * 452f);
        var root = new GameObject(endA ? "GerbangEldrathA" : "GerbangPerbatasanB").transform;
        root.SetParent(tRoot, false);
        var res = GateKit.Build(root, "Gerbang", tier, sign, "", gp, yaw, false, true, false);
        var sp = GateKit.SpecFor(tier);
        float gy = gp.y;
        // tembok + menara ke kiri-kanan gerbang: sama persis gaya tembok Kota Eldrath (WallKit)
        var bag = new GeoBag();
        float h = 26f, th = 8f;
        float x0 = sp.tX + sp.towerR + 0.5f, x1 = 150f;
        foreach (float sx in new[] { -1f, 1f })
        {
            float len = x1 - x0, cx = sx * (x0 + x1) * 0.5f;
            var M3 = new Vector3(cx, 0f, 0f);
            bag.Box(Sl.Stone, M3 + new Vector3(0f, h / 2f, 0f), new Vector3(len + 0.5f, h, th), 0f);
            bag.Box(Sl.DarkStone, M3 + new Vector3(0f, 1.0f, 0f), new Vector3(len + 0.5f, 2.0f, th + 1.2f), 0f);
            bag.Box(Sl.DarkStone, M3 + new Vector3(0f, h - 3.2f, 0f), new Vector3(len + 0.5f, 0.5f, th + 0.3f), 0f);
            bag.Box(Sl.DarkStone, M3 + new Vector3(0f, h, 0f), new Vector3(len + 0.5f, 0.6f, th + 1.0f), 0f);
            bag.Box(Sl.Gold, M3 + new Vector3(0f, h + 0.34f, 0f), new Vector3(len + 0.5f, 0.14f, th + 1.04f), 0f);
            bag.BoxB(Sl.Stone, M3 + new Vector3(0f, h + 0.3f, -(th / 2f - 0.1f)), new Vector3(len + 0.5f, 0.9f, 0.55f), 0f);
            int nm = Mathf.Max(1, Mathf.FloorToInt(len / 3.4f));
            for (int j = 0; j < nm; j++)
            {
                float t = (j + 0.5f) / nm - 0.5f;
                bag.BoxB(Sl.Stone, M3 + new Vector3(t * len, h + 0.3f, th / 2f + 0.15f), new Vector3(1.9f, 1.6f, 0.9f), 0f);
            }
            int ti = 0;
            for (float x = x0 + 28f; x < x1; x += 30f, ti++)
                BuildKit.RoundTower(bag, new Vector3(sx * x, 0f, 0.6f), 4.6f, h + 5.5f, Sl.Stone, Sl.DarkStone, ti % 2 == 0 ? Sl.Roof : Sl.RoofB, 12f, false, true, Sl.Cloth);
            // menara ujung tembok
            BuildKit.RoundTower(bag, new Vector3(sx * x1, 0f, 0.6f), 4.6f, h + 5.5f, Sl.Stone, Sl.DarkStone, Sl.Roof, 12f, false, true, Sl.Cloth);
        }
        var wallGo = bag.Emit(res.root.transform, "TembokKiriKanan", sl);
        wallGo.transform.localRotation = Quaternion.identity;
        wallGo.transform.localPosition = Vector3.zero;
        // mesh dibuat dalam bingkai lokal gerbang (+Z keluar kota); beri collider
        var wmf = wallGo.GetComponent<MeshFilter>();
        if (wmf != null) wallGo.AddComponent<MeshCollider>().sharedMesh = wmf.sharedMesh;
        // jalan batu dari gerbang ke dalam area
        var cob = sl[Sl.Cobble];
        for (float d = 2f; d <= 100f; d += 6f)
        {
            Vector3 q = G(0f, sgn * (452f - d));
            var slab = Prim(PrimitiveType.Cube, "JalanBatu", root, q + Vector3.up * 0.05f, new Vector3(14f, 0.3f, 6.4f), null, false);
            slab.GetComponent<Renderer>().sharedMaterial = cob;
        }
        // lampu kristal + sipres
        var lamp = tier == 2 ? PropKit.Get("D_CrystalLamp", DungeonKit.CrystalLamp) : PropKit.LampProto();
        var cyp = PropKit.TreeProto(3);
        for (float d = 14f; d <= 98f; d += 14f)
            foreach (float sx in new[] { -1f, 1f })
            {
                Vector3 q = G(sx * 8f, sgn * (452f - d));
                var go = PropKit.Place(lamp, root, "Lampu", q, 0f, 1f, sl);
                if (tier == 2 && ((int)d / 14) % 2 == 0) ELight(go.transform, q + Vector3.up * 4.4f, new Color(0.55f, 0.85f, 1f), 16f, 1.8f);
            }
        for (float d = 10f; d <= 100f; d += 8f)
            foreach (float sx in new[] { -1f, 1f })
                PropKit.Place(cyp, root, "Sipres", G(sx * 14.5f, sgn * (452f - d)), (d * 37f) % 360f, 1f + ((d * 7f) % 5f) * 0.06f, sl);
    }

    /// <summary>Gerbang batu: dua pilar + ambang + papan nama yang menghadap ke dalam area. endA: ujung A (design z negatif).</summary>
    static void EGateArch(Vector3 pos, bool endA, string label)
    {
        var g = new GameObject(endA ? "GerbangA" : "GerbangB").transform;
        g.SetParent(tRoot, false);
        g.position = pos;
        float inward = M * (endA ? 1f : -1f);                 // arah dunia menuju bagian dalam area
        foreach (float sx in new[] { -9f, 9f })
        {
            Prim(PrimitiveType.Cube, "Pilar", g, new Vector3(sx, 5f, 0f), new Vector3(3f, 10f, 3f), mStone);
            Prim(PrimitiveType.Cube, "PilarTopi", g, new Vector3(sx, 10.4f, 0f), new Vector3(4f, 0.8f, 4f), mDark, false);
            ELight(g, g.position + new Vector3(sx, 6.5f, inward * 2.2f), new Color(1f, 0.7f, 0.35f), 16f, 1.5f);
        }
        Prim(PrimitiveType.Cube, "Ambang", g, new Vector3(0f, 11.4f, 0f), new Vector3(21f, 2.4f, 3.4f), mStone);
        Prim(PrimitiveType.Cube, "PapanGerbang", g, new Vector3(0f, 11.4f, inward * 1.8f), new Vector3(15f, 1.8f, 0.3f), mDark, false);
        BuildKit.Text(g, label, new Vector3(0f, 11.4f, inward * 2.02f), inward > 0f ? 180f : 0f, 0.2f, new Color(1f, 0.85f, 0.45f));
    }

    /// <summary>Latar di luar gerbang: tanah, jalan, dan tembok kota di kejauhan, supaya gerbang terlihat tersambung ke kota (bukan langit kosong).</summary>
    static void EBackdropCity(bool endA)
    {
        float sg = endA ? -1f : 1f;                           // arah keluar (koordinat design)
        var root = new GameObject(endA ? "LatarKotaA" : "LatarKotaB").transform;
        root.SetParent(tRoot, false);
        Color gc = ea == 0 ? new Color(0.27f, 0.38f, 0.2f) : ea == 1 ? new Color(0.2f, 0.1f, 0.08f) : ea == 2 ? new Color(0.5f, 0.5f, 0.52f) : new Color(0.22f, 0.09f, 0.11f);
        var ground = Mat("E" + ea + "Far", gc);
        var road = Mat("EFarRoad", new Color(0.42f, 0.33f, 0.22f));
        var wall = Mat("EFarWall", new Color(0.55f, 0.53f, 0.5f));
        var roof = Mat("EFarRoof", new Color(0.35f, 0.16f, 0.14f));
        float gy = G(0f, sg * 470f).y - 0.1f;
        System.Func<float, float> Z = d => M * sg * d;      // jarak keluar -> z dunia
        Prim(PrimitiveType.Cube, "TanahLuar", root, new Vector3(0f, gy - 0.5f, Z(500f + 700f)), new Vector3(3000f, 1f, 1400f), ground, false);
        Prim(PrimitiveType.Cube, "JalanKeKota", root, new Vector3(0f, gy + 0.05f, Z(685f)), new Vector3(16f, 0.2f, 430f), road, false);
        float wz = Z(900f);
        foreach (float sx in new[] { -1f, 1f })
            Prim(PrimitiveType.Cube, "TembokKota", root, new Vector3(sx * 458f, gy + 20f, wz), new Vector3(884f, 40f, 20f), wall, false);
        foreach (float tx in new[] { -450f, -300f, -150f, -30f, 30f, 150f, 300f, 450f })
        {
            bool gate = Mathf.Abs(tx) < 40f;
            float r = gate ? 24f : 18f, h = gate ? 56f : 46f;
            Prim(PrimitiveType.Cylinder, "MenaraKota", root, new Vector3(tx, gy + h / 2f, wz), new Vector3(r, h / 2f, r), wall, false);
            Prim(PrimitiveType.Cylinder, "AtapMenara", root, new Vector3(tx, gy + h + 5f, wz), new Vector3(r * 0.8f, 5f, r * 0.8f), roof, false);
        }
        Prim(PrimitiveType.Cube, "BentengKota", root, new Vector3(0f, gy + 55f, Z(985f)), new Vector3(140f, 110f, 70f), wall, false);
        Prim(PrimitiveType.Cube, "AtapBenteng", root, new Vector3(0f, gy + 118f, Z(985f)), new Vector3(100f, 16f, 50f), roof, false);
    }

    /// <summary>Penyambut di jalur awal (z -440..-345): obor di sepanjang jalan dan formasi mengapit, tanpa bergantung filter acak lain.</summary>
    static void EEntrancePlaza()
    {
        Color lc = ea == 2 ? new Color(0.45f, 0.75f, 1f) : ea == 0 ? new Color(1f, 0.8f, 0.5f) : ea == 1 ? new Color(1f, 0.5f, 0.15f) : new Color(0.8f, 0.25f, 0.9f);
        int made = 0;
        for (int i = 0; i < 400 && made < 44; i++)
        {
            float x = (made % 2 == 0 ? -1f : 1f) * Random.Range(26f, 90f), z = Random.Range(-440f, -345f);
            if (DistToTrails(x, z) < 6f || Slope(x, z) > 30f) continue;
            int v = Random.Range(0, 5);
            GameObject go;
            if (ea == 0) go = EKit(v < 3 ? "EOak" + (v % 2) : "ESpruce", b => { if (v < 3) ForestKit.Oak(b, (v % 2) + 20); else ForestKit.Spruce(b, 25); }, tProps, G(x, z), Random.Range(0f, 360f), Random.Range(0.9f, 1.4f), false);
            else if (ea == 2) go = EKit("ESpireB" + v, b => ForestKit.Obelisk(b, 40 + v, 0.6f, Random.Range(5f, 9f)), tRocks, G(x, z), Random.Range(0f, 360f), Random.Range(0.8f, 1.5f), false);
            else go = EKit("ESpire" + ea + "_" + v, b => ForestKit.Hoodoo(b, 50 + v + ea * 7, Random.Range(6f, 14f)), tRocks, G(x, z), Random.Range(0f, 360f), Random.Range(0.9f, 1.8f), false);
            var mf = go.GetComponent<MeshFilter>();
            if (mf != null) go.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            made++;
        }
    }

    static void Barrier(Vector3 pos, Vector3 size)
    {
        var b = new GameObject("Barrier");
        b.transform.SetParent(tRoot, false);
        b.transform.position = pos;
        var bc = b.AddComponent<BoxCollider>();
        bc.size = size;
    }

    static void EAtmosphere()
    {
        var sun = GameObject.Find("Directional Light");
        Light sl = sun != null ? sun.GetComponent<Light>() : null;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        switch (ea)
        {
            case 0: RenderSettings.fogDensity = 0.0017f; RenderSettings.fogColor = new Color(0.64f, 0.72f, 0.72f); break;
            case 1:
                RenderSettings.fogDensity = 0.0011f; RenderSettings.fogColor = new Color(0.30f, 0.12f, 0.08f);
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.34f, 0.2f, 0.16f);
                if (sl != null) { sl.color = new Color(1f, 0.6f, 0.4f); sl.intensity = 0.8f; }
                break;
            case 2:
                RenderSettings.fogDensity = 0.0018f; RenderSettings.fogColor = new Color(0.55f, 0.6f, 0.68f);
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.42f, 0.45f, 0.52f);
                if (sl != null) { sl.color = new Color(0.75f, 0.82f, 1f); sl.intensity = 0.7f; }
                break;
            default:
                // n88: langit jingga tanpa matahari yang jelas (kanon 8.8). Sebelumnya fog (0.22, 0.07, 0.12), ambient (0.3, 0.14, 0.2), matahari (0.9, 0.4, 0.55)
                RenderSettings.fogDensity = 0.0018f; RenderSettings.fogColor = new Color(0.34f, 0.14f, 0.07f);
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.34f, 0.18f, 0.12f);
                if (sl != null) { sl.color = new Color(1f, 0.6f, 0.32f); sl.intensity = 0.6f; }
                break;
        }
        if (ea == 1 || ea == 3)
        {
            // suasana tetap suram (tanpa siklus siang-malam)
            var dn = GameObject.Find("DayNight");
            if (dn != null) Object.DestroyImmediate(dn);
        }
    }

    // ------------------------------------------------------------------ menu
    [MenuItem("Hunter Guild/Build Hunt Area (Hutan Perbatasan)")] public static void BuildFrontier() { BuildEldrathArea(0); }
    [MenuItem("Hunter Guild/Build Hunt Area (Gunung Vulkanik)")] public static void BuildVolcano() { BuildEldrathArea(1); }
    [MenuItem("Hunter Guild/Build Hunt Area (Padang Tulang)")] public static void BuildBoneField() { BuildEldrathArea(2); }
    [MenuItem("Hunter Guild/Build Hunt Area (Lembah Iblis)")] public static void BuildDemonValley() { BuildEldrathArea(3); }
}
