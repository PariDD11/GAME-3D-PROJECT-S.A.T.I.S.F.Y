using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Alat medan (Terrain asli) untuk Desa Pemula dan Hutan Pemula: bukit bergelombang, pegunungan pembatas,
/// sungai dengan jembatan kayu, kolam. Tinggi didefinisikan satu fungsi (vH) sehingga pohon, monster, herb,
/// dan jembatan bisa ditaruh tepat di atas tanah.
/// </summary>
public static partial class TownSceneBuilder
{
    const string VTerrDir = "Assets/Terrain";
    const float VTerrTH = 110f;            // tinggi maksimum terrain
    const float VTerrBase = 10f;           // terrain digeser turun sebesar ini: tinggi dunia 0 = tanah datar
    const float VWaterLevel = -1.6f;
    const int VHRes = 513;

    static System.Func<float, float, float> vH;
    static Terrain vTerrain;

    static float vGrassDens = 0.4f;
    static float VGy(float x, float z) { return vH == null ? 0f : vH(x, z); }
    static Vector3 VG(float x, float z) { return new Vector3(x, VGy(x, z), z); }
    static float VSm(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }
    static float VN(float x, float z, float s, float o) { return Mathf.PerlinNoise(x * s + o + 1000f, z * s + o * 1.7f + 1000f); }

    static float VDistSeg(float px, float pz, float ax, float az, float bx, float bz)
    {
        float vx = bx - ax, vz = bz - az;
        float l2 = vx * vx + vz * vz;
        float t = l2 < 0.0001f ? 0f : Mathf.Clamp01(((px - ax) * vx + (pz - az) * vz) / l2);
        float dx = px - (ax + vx * t), dz = pz - (az + vz * t);
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    static float VDistPoly(float x, float z, IList<Vector2> p)
    {
        float best = 99999f;
        for (int i = 0; i + 1 < p.Count; i++)
        {
            float d = VDistSeg(x, z, p[i].x, p[i].y, p[i + 1].x, p[i + 1].y);
            if (d < best) best = d;
        }
        return best;
    }

    static float VSlopeDeg(float x, float z)
    {
        const float e = 1.5f;
        float gx = (VGy(x + e, z) - VGy(x - e, z)) / (2f * e), gz = (VGy(x, z + e) - VGy(x, z - e)) / (2f * e);
        return Mathf.Atan(Mathf.Sqrt(gx * gx + gz * gz)) * Mathf.Rad2Deg;
    }

    /// <summary>Titik-titik halus (Catmull-Rom sederhana) dari polyline kasar, untuk sungai yang berkelok mulus.</summary>
    static List<Vector2> VSmoothPoly(Vector2[] p, int sub)
    {
        var r = new List<Vector2>();
        for (int i = 0; i + 1 < p.Length; i++)
        {
            Vector2 p0 = p[Mathf.Max(0, i - 1)], p1 = p[i], p2 = p[i + 1], p3 = p[Mathf.Min(p.Length - 1, i + 2)];
            for (int k = 0; k < sub; k++)
            {
                float t = k / (float)sub, t2 = t * t, t3 = t2 * t;
                r.Add(0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
            }
        }
        r.Add(p[p.Length - 1]);
        return r;
    }

    // =====================================================================
    // Terrain
    // =====================================================================
    /// <param name="paint">(x, z, tinggi) -> bobot 7 lapisan: 0 rumput, 1 tanah, 2 batu, 3 lumpur, 4 serasah, 5 lumut, 6 batu bulat</param>
    /// <param name="hres">resolusi heightmap (2^n + 1); Kota Ashwatch memakai 1025 agar landai kota & tembok presisi</param>
    /// <param name="ares">resolusi peta cat tanah</param>
    static void VBuildTerrain(string name, float half, System.Func<float, float, float, float[]> paint, int hres = VHRes, int ares = 512)
    {
        EnsureFolder(VTerrDir);
        var td = new TerrainData();
        td.heightmapResolution = hres;
        td.size = new Vector3(half * 2f, VTerrTH, half * 2f);
        HeightBake.NewAsset(td, VTerrDir + "/" + name + ".asset");

        var hs = new float[hres, hres];
        float step = half * 2f / (hres - 1);
        for (int zi = 0; zi < hres; zi++)
            for (int xi = 0; xi < hres; xi++)
                hs[zi, xi] = Mathf.Clamp01((vH(-half + xi * step, -half + zi * step) + VTerrBase) / VTerrTH);
        td.SetHeights(0, 0, hs);

        // lapisan tekstur
        string[] ln = { "Grass", "Dirt", "Rock", "Mud", "Litter", "Moss", "Cobble" };
        Texture2D[] tex = { ForestKit.tGrass, ForestKit.tDirt, ForestKit.tRock, ForestKit.tMud, ForestKit.tLitter, ForestKit.tMoss, ForestKit.tCobble };
        float[] tile = { 7f, 6f, 9f, 6f, 5f, 6f, 5f };
        var layers = new TerrainLayer[ln.Length];
        for (int i = 0; i < layers.Length; i++)
        {
            var l = new TerrainLayer();
            l.diffuseTexture = tex[i];
            l.tileSize = new Vector2(tile[i], tile[i]);
            l.smoothness = i == 2 ? 0.25f : 0.05f;
            AssetDatabase.CreateAsset(l, VTerrDir + "/" + name + "_Layer_" + ln[i] + ".terrainlayer");
            layers[i] = l;
        }
        td.terrainLayers = layers;
        int res = ares;
        td.alphamapResolution = res;
        var map = new float[res, res, 7];
        for (int zi = 0; zi < res; zi++)
            for (int xi = 0; xi < res; xi++)
            {
                float wx = -half + xi / (res - 1f) * half * 2f, wz = -half + zi / (res - 1f) * half * 2f;
                var w = paint(wx, wz, VGy(wx, wz));
                float sum = 0f;
                for (int k = 0; k < 7; k++) sum += w[k];
                if (sum < 0.0001f) { w[0] = 1f; sum = 1f; }
                for (int k = 0; k < 7; k++) map[zi, xi, k] = w[k] / sum;
            }
        td.SetAlphamaps(0, 0, map);

        // rumput detail
        var tuft = ForestKit.TexGrassTuft();
        td.detailPrototypes = new[]
        {
            new DetailPrototype
            {
                usePrototypeMesh = false, prototypeTexture = tuft, renderMode = DetailRenderMode.GrassBillboard,
                minWidth = 0.9f, maxWidth = 1.7f, minHeight = 0.7f, maxHeight = 1.3f, noiseSpread = 0.35f,
                healthyColor = new Color(0.78f, 0.95f, 0.65f), dryColor = new Color(0.85f, 0.78f, 0.45f)
            }
        };
        int dr = 512;
        td.SetDetailResolution(dr, 16);
        var det = new int[dr, dr];
        var rnd = new System.Random(4242);
        for (int z = 0; z < dr; z++)
            for (int x = 0; x < dr; x++)
            {
                int ax = Mathf.Clamp(Mathf.RoundToInt(x / (dr - 1f) * (res - 1)), 0, res - 1);
                int az = Mathf.Clamp(Mathf.RoundToInt(z / (dr - 1f) * (res - 1)), 0, res - 1);
                float g = map[az, ax, 0] + map[az, ax, 4] * 0.25f + map[az, ax, 5] * 0.4f;
                if (g < 0.5f) continue;
                if (rnd.NextDouble() < vGrassDens * g) det[z, x] = 1 + (rnd.NextDouble() < 0.4 ? 1 : 0);
            }
        td.SetDetailLayer(0, 0, 0, det);

        var go = Terrain.CreateTerrainGameObject(td);
        if (go != null)
        {
            go.name = name;
            go.transform.SetParent(tEnv, false);
            go.transform.position = new Vector3(-half, -VTerrBase, -half);
            vTerrain = go.GetComponent<Terrain>();
            HeightBake.Attach(go, td, hs, name);
            vTerrain.detailObjectDistance = 80f;
            vTerrain.detailObjectDensity = 0.8f;
            vTerrain.heightmapPixelError = 6f;
            vTerrain.drawInstanced = true;
        }
        EditorUtility.SetDirty(td);


        // permukaan air: hanya petak yang benar-benar di cekungan (sungai/kolam), BUKAN satu bidang penuh peta
        var wb = new GeoBag();
        float cs = 3f;
        int nc = Mathf.CeilToInt(half * 2f / cs);
        int wet = 0;
        for (int iz = 0; iz < nc; iz++)
            for (int ix = 0; ix < nc; ix++)
            {
                float x0 = -half + ix * cs, z0 = -half + iz * cs, x1 = x0 + cs, z1 = z0 + cs;
                float lo = Mathf.Min(Mathf.Min(VGy(x0, z0), VGy(x1, z0)), Mathf.Min(VGy(x0, z1), VGy(x1, z1)));
                if (lo >= VWaterLevel - 0.05f) continue;
                wb.Patch(Sl.Water, V(x0, z0), V(x0, z1), V(x1, z1), V(x1, z0), VWaterLevel, V(1f, 0f));
                wet++;
            }
        if (wet > 0) wb.Emit(tEnv, "Air", baseSlots, MeshDir + "/" + name + "_Water.asset");
    }

    // =====================================================================
    // Jembatan di mana jalur memotong air
    // =====================================================================
    /// <summary>Telusuri jalur (fungsi jarak -> titik dunia); tiap bentang air dilewati dengan jembatan kayu.</summary>
    static int VBridgesAlong(string label, System.Func<float, Vector3> pathAt, float d0, float d1, float width)
    {
        int made = 0;
        float d = d0;
        while (d < d1)
        {
            Vector3 p = pathAt(d);
            if (VGy(p.x, p.z) < VWaterLevel - 0.05f)
            {
                float s = d;
                while (d < d1 && VGy(pathAt(d).x, pathAt(d).z) < VWaterLevel - 0.05f) d += 0.75f;
                float e = d;
                float a = s - 4.5f, b = e + 4.5f;
                Vector3 A = pathAt(a), B = pathAt(b);
                A.y = VGy(A.x, A.z) + 0.3f;
                B.y = VGy(B.x, B.z) + 0.3f;
                float floorY = Mathf.Min(VGy(pathAt((s + e) / 2f).x, pathAt((s + e) / 2f).z), VWaterLevel) - 0.6f;
                BridgeKit.Place(tProps, "Jembatan_" + label + "_" + (++made), A, B, width, floorY, baseSlots,
                    MeshDir + "/Bridge_" + label + "_" + made + ".asset", (x, z) => VGy(x, z));
                d = b + 1f;
            }
            else d += 0.75f;
        }
        return made;
    }

    // =====================================================================
    // Tanaman medan (pohon sesuai ketinggian)
    // =====================================================================
    static PropKit.Proto[] vTreeProtos;

    static void VLoadTreeProtos()
    {
        vTreeProtos = new[]
        {
            PropKit.Get("Tut_Oak_A", b => ForestKit.Oak(b, 3)),
            PropKit.Get("Tut_Spruce_A", b => ForestKit.Spruce(b, 5)),
            PropKit.Get("Tut_Birch_A", b => ForestKit.Birch(b, 11)),
            PropKit.Get("Tut_Bush", b => ForestKit.Bush(b, 7)),
            PropKit.Get("Tut_Oak_B", b => ForestKit.Oak(b, 21, ForestKit.LeafGreen, ForestKit.LeafYellow, 1.1f)),
            PropKit.Get("Tut_Spruce_B", b => ForestKit.Spruce(b, 25, 1.1f)),
            PropKit.Get("Tut_Fern", b => ForestKit.Fern(b, 15)),
            PropKit.Get("Tut_Maple", b => ForestKit.Oak(b, 33, ForestKit.LeafRed, ForestKit.LeafOrange, 1.05f)),
        };
    }

    /// <summary>Tanam satu pohon di atas tanah. alt = ketinggian: di atas 16 m dominan cemara.</summary>
    static GameObject VPlaceTree(Transform parent, float x, float z, float scaleMul)
    {
        float y = VGy(x, z);
        int k;
        float r = Random.value;
        if (y > 18f) k = r < 0.5f ? 1 : (r < 0.9f ? 5 : 3);
        else if (y > 9f) k = r < 0.4f ? 1 : (r < 0.6f ? 5 : (r < 0.8f ? 0 : 2));
        else k = r < 0.26f ? 0 : (r < 0.42f ? 4 : (r < 0.56f ? 2 : (r < 0.7f ? 7 : (r < 0.8f ? 1 : (r < 0.9f ? 3 : 6)))));
        float s = Random.Range(0.9f, 1.5f) * scaleMul;
        var go = PropKit.Place(vTreeProtos[k], parent, "Pohon", new Vector3(x, y - 0.1f, z), Random.Range(0f, 360f), s, ForestKit.Slots);
        if (k != 3 && k != 6)
        {
            var cc = go.AddComponent<CapsuleCollider>();
            cc.center = new Vector3(0f, 1.5f, 0f); cc.radius = 0.5f; cc.height = 3f;
        }
        return go;
    }

    /// <summary>Batu besar di lereng pegunungan.</summary>
    static void VScatterBoulders(Transform parent, int count, float half, System.Func<float, float, bool> allow)
    {
        for (int i = 0; i < 4000 && count > 0; i++)
        {
            float x = Random.Range(-half, half), z = Random.Range(-half, half);
            float y = VGy(x, z);
            if (y < 1.2f || !allow(x, z)) continue;
            int v = Random.Range(0, 4);
            float s = Random.Range(1.0f, 3.6f) * (y > 14f ? 1.6f : 1f);
            var proto = PropKit.Get("Tut_Boulder" + v, b => ForestKit.Boulder(b, 50 + v * 7, 1f, v % 2 == 0));
            var go = PropKit.Place(proto, parent, "Batu", new Vector3(x, y - 0.2f * s, z), Random.Range(0f, 360f), s, ForestKit.Slots);
            if (s > 1.4f)
            {
                var bc = go.AddComponent<SphereCollider>();
                bc.radius = 0.9f; bc.center = new Vector3(0f, 0.7f, 0f);
            }
            count--;
        }
    }
}
