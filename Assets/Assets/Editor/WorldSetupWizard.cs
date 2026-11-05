using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using HunterGuild;

namespace HunterGuild.EditorTools
{
    /// <summary>Menu: Hunter Guild > Setup Langit &amp; Terrain. Atur 4 fase hari + skybox sendiri, dan buat terrain dari tekstur sendiri.</summary>
    public class WorldSetupWizard : EditorWindow
    {
        const string Dir = "Assets/Resources/HG_World";
        const string SkyPath = Dir + "/SkyConfig.asset";
        const string TerDir = "Assets/HG_World";
        const string SetPath = TerDir + "/WorldSettings.asset";

        int tab;
        Vector2 scroll;
        HGSkyConfig sky;
        HGWorldSettings set;
        float previewHour = 12f;
        string msg = "";

        [MenuItem("Hunter Guild/Setup Langit & Terrain")]
        static void Open()
        {
            var w = GetWindow<WorldSetupWizard>("Langit & Terrain");
            w.minSize = new Vector2(460, 560);
            w.Show();
        }

        void OnEnable() { LoadAssets(); }

        void LoadAssets()
        {
            sky = AssetDatabase.LoadAssetAtPath<HGSkyConfig>(SkyPath);
            set = AssetDatabase.LoadAssetAtPath<HGWorldSettings>(SetPath);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        void OnGUI()
        {
            tab = GUILayout.Toolbar(tab, new[] { "Langit & Waktu", "Terrain" });
            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (tab == 0) SkyGUI(); else TerrainGUI();
            EditorGUILayout.EndScrollView();
            if (!string.IsNullOrEmpty(msg)) EditorGUILayout.HelpBox(msg, MessageType.Info);
        }

        // ===================== LANGIT =====================
        void SkyGUI()
        {
            EditorGUILayout.HelpBox("Isi skybox Material (fantasy skybox kamu) untuk tiap fase. Game otomatis pindah fase mengikuti jam in-game dan transisinya halus.", MessageType.None);
            if (sky == null)
            {
                if (GUILayout.Button("Buat SkyConfig baru", GUILayout.Height(30)))
                {
                    EnsureFolder(Dir);
                    sky = CreateInstance<HGSkyConfig>();
                    AssetDatabase.CreateAsset(sky, SkyPath);
                    AssetDatabase.SaveAssets();
                }
                return;
            }

            EditorGUI.BeginChangeCheck();
            for (int i = 0; i < sky.phases.Length; i++)
            {
                var p = sky.phases[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(p.name, EditorStyles.boldLabel);
                p.skybox = (Material)EditorGUILayout.ObjectField("Skybox Material", p.skybox, typeof(Material), false);
                p.startHour = EditorGUILayout.Slider("Mulai jam", p.startHour, 0f, 24f);
                p.sunColor = EditorGUILayout.ColorField("Warna matahari/bulan", p.sunColor);
                p.sunIntensity = EditorGUILayout.Slider("Intensitas cahaya", p.sunIntensity, 0f, 2.5f);
                p.ambientColor = EditorGUILayout.ColorField("Warna ambient", p.ambientColor);
                p.ambientIntensity = EditorGUILayout.Slider("Intensitas ambient", p.ambientIntensity, 0f, 2f);
                p.fogColor = EditorGUILayout.ColorField("Warna kabut", p.fogColor);
                p.fogDensity = EditorGUILayout.Slider("Kepadatan kabut", p.fogDensity, 0f, 0.02f);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.LabelField("Umum", EditorStyles.boldLabel);
            sky.blendHours = EditorGUILayout.Slider("Lama transisi (jam)", sky.blendHours, 0.1f, 4f);
            sky.skyDip = EditorGUILayout.Slider("Redup saat ganti skybox", sky.skyDip, 0f, 1f);
            sky.sunAzimuth = EditorGUILayout.Slider("Arah matahari", sky.sunAzimuth, 0f, 360f);
            sky.sunriseHour = EditorGUILayout.Slider("Matahari terbit", sky.sunriseHour, 3f, 9f);
            sky.sunsetHour = EditorGUILayout.Slider("Matahari terbenam", sky.sunsetHour, 15f, 21f);
            if (EditorGUI.EndChangeCheck()) { EditorUtility.SetDirty(sky); }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Pratinjau di scene (tanpa Play)", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            string[] nm = { "Pagi", "Siang", "Sore", "Malam" };
            float[] hr = { 7f, 12f, 17.5f, 22f };
            for (int i = 0; i < nm.Length; i++)
                if (GUILayout.Button(nm[i], GUILayout.Height(26))) { previewHour = hr[i]; Preview(); }
            EditorGUILayout.EndHorizontal();
            EditorGUI.BeginChangeCheck();
            previewHour = EditorGUILayout.Slider("Jam", previewHour, 0f, 24f);
            if (EditorGUI.EndChangeCheck()) Preview();

            EditorGUILayout.Space();
            if (GUILayout.Button("Simpan & Pasang ke Scene aktif", GUILayout.Height(32))) ApplyToScene();
            EditorGUILayout.HelpBox("Tombol di atas membuat/menemukan Directional Light dan komponen TownDayNight di scene yang terbuka. Scene bawaan (Town/Hunt) otomatis memakai SkyConfig ini saat Play karena TownDayNight sudah ada di sana.", MessageType.None);
        }

        static Light FindSun()
        {
            if (RenderSettings.sun != null) return RenderSettings.sun;
            foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) return l;
            return null;
        }

        void Preview()
        {
            if (sky == null) return;
            var sun = FindSun();
            sky.Apply(previewHour, sun, true);
            SceneView.RepaintAll();
        }

        void ApplyToScene()
        {
            if (sky == null) return;
            AssetDatabase.SaveAssets();
            var sun = FindSun();
            if (sun == null)
            {
                var go = new GameObject("Sun");
                sun = go.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.shadows = LightShadows.Soft;
                Undo.RegisterCreatedObjectUndo(go, "Buat Sun");
            }
            RenderSettings.sun = sun;
            var dn = UnityEngine.Object.FindFirstObjectByType<TownDayNight>();
            if (dn == null) dn = sun.gameObject.AddComponent<TownDayNight>();
            dn.sun = sun;
            EditorUtility.SetDirty(dn);
            Preview();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            msg = "Langit dipasang. Siklus berjalan saat Play mengikuti GameState.timeOfDay (butuh GameState di scene, ada di scene Town/Hunt).";
        }

        // ===================== TERRAIN =====================
        void TerrainGUI()
        {
            if (set == null)
            {
                EditorGUILayout.HelpBox("Pengaturan terrain belum ada.", MessageType.None);
                if (GUILayout.Button("Buat pengaturan terrain", GUILayout.Height(30)))
                {
                    EnsureFolder(TerDir);
                    set = CreateInstance<HGWorldSettings>();
                    AssetDatabase.CreateAsset(set, SetPath);
                    AssetDatabase.SaveAssets();
                }
                return;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Tekstur permukaan (opsional, kosong = warna polos)", EditorStyles.boldLabel);
            foreach (var l in set.layers)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(l.name, EditorStyles.boldLabel);
                l.diffuse = (Texture2D)EditorGUILayout.ObjectField("Diffuse", l.diffuse, typeof(Texture2D), false);
                l.normal = (Texture2D)EditorGUILayout.ObjectField("Normal (opsional)", l.normal, typeof(Texture2D), false);
                l.tileSize = EditorGUILayout.Slider("Ukuran ubin (m)", l.tileSize, 1f, 40f);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.LabelField("Bentuk", EditorStyles.boldLabel);
            set.size = EditorGUILayout.Vector3Field("Ukuran (X, tinggi, Z)", set.size);
            set.seed = EditorGUILayout.IntField("Seed", set.seed);
            set.noiseScale = EditorGUILayout.Slider("Skala noise", set.noiseScale, 0.002f, 0.05f);
            set.hillAmount = EditorGUILayout.Slider("Tinggi bukit", set.hillAmount, 0f, 1f);
            set.flatRadius = EditorGUILayout.Slider("Radius datar tengah", set.flatRadius, 0f, 150f);
            set.flatBlend = EditorGUILayout.Slider("Lebar peralihan", set.flatBlend, 1f, 100f);

            EditorGUILayout.LabelField("Pengecatan otomatis", EditorStyles.boldLabel);
            set.rockSlope = EditorGUILayout.Slider("Batu mulai di kemiringan", set.rockSlope, 10f, 70f);
            set.sandHeight = EditorGUILayout.Slider("Pasir di bawah tinggi", set.sandHeight, 0f, 0.3f);
            set.dirtNoise = EditorGUILayout.Slider("Bercak tanah", set.dirtNoise, 0f, 1f);

            EditorGUILayout.LabelField("Pohon (prefab milikmu)", EditorStyles.boldLabel);
            var so = new SerializedObject(set);
            EditorGUILayout.PropertyField(so.FindProperty("trees"), true);
            so.ApplyModifiedProperties();
            set.treeCount = EditorGUILayout.IntSlider("Jumlah pohon", set.treeCount, 0, 3000);
            set.treeMinScale = EditorGUILayout.Slider("Skala min", set.treeMinScale, 0.3f, 2f);
            set.treeMaxScale = EditorGUILayout.Slider("Skala max", set.treeMaxScale, 0.3f, 3f);

            EditorGUILayout.LabelField("Rumput detail (opsional)", EditorStyles.boldLabel);
            set.grassTexture = (Texture2D)EditorGUILayout.ObjectField("Tekstur rumput", set.grassTexture, typeof(Texture2D), false);
            set.grassDensity = EditorGUILayout.Slider("Kerapatan", set.grassDensity, 0.1f, 4f);
            if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(set);

            EditorGUILayout.Space();
            if (GUILayout.Button("Buat Terrain di Scene aktif", GUILayout.Height(34))) BuildTerrain();
            EditorGUILayout.HelpBox("Membuat Terrain baru (data disimpan di Assets/HG_World) dan menggantikan terrain bernama 'HG_Terrain' bila sudah ada. Terrain bawaan builder tidak disentuh.", MessageType.None);
        }

        static TerrainLayer MakeLayer(HGWorldSettings.Layer l, Color fallback, string file)
        {
            var tl = new TerrainLayer();
            tl.diffuseTexture = l.diffuse != null ? l.diffuse : SolidTex(fallback);
            tl.normalMapTexture = l.normal;
            tl.tileSize = new Vector2(l.tileSize, l.tileSize);
            AssetDatabase.CreateAsset(tl, TerDir + "/" + file + ".terrainlayer");
            return tl;
        }

        static Texture2D SolidTex(Color c)
        {
            var t = new Texture2D(4, 4);
            var px = new Color[16];
            for (int i = 0; i < 16; i++) px[i] = c;
            t.SetPixels(px); t.Apply();
            string p = TerDir + "/solid_" + ColorUtility.ToHtmlStringRGB(c) + ".asset";
            var ex = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            if (ex != null) return ex;
            AssetDatabase.CreateAsset(t, p);
            return t;
        }

        void BuildTerrain()
        {
            EnsureFolder(TerDir);
            var old = GameObject.Find("HG_Terrain");
            if (old != null) Undo.DestroyObjectImmediate(old);

            int res = set.heightRes;
            var td = new TerrainData();
            td.heightmapResolution = res;
            td.size = set.size;
            td.alphamapResolution = set.alphaRes;
            td.baseMapResolution = 512;

            // ketinggian
            var h = new float[res, res];
            float ox = set.seed * 31.7f, oz = set.seed * 17.3f;
            float cx = 0.5f, cz = 0.5f;
            for (int z = 0; z < res; z++)
                for (int x = 0; x < res; x++)
                {
                    float wx = x / (float)(res - 1) * set.size.x, wz = z / (float)(res - 1) * set.size.z;
                    float n = Mathf.PerlinNoise(ox + wx * set.noiseScale, oz + wz * set.noiseScale) * 0.65f
                            + Mathf.PerlinNoise(ox + wx * set.noiseScale * 2.3f, oz + wz * set.noiseScale * 2.3f) * 0.25f
                            + Mathf.PerlinNoise(ox + wx * set.noiseScale * 5f, oz + wz * set.noiseScale * 5f) * 0.10f;
                    float dx = wx - cx * set.size.x, dz = wz - cz * set.size.z;
                    float d = Mathf.Sqrt(dx * dx + dz * dz);
                    float flat = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - set.flatRadius) / Mathf.Max(0.01f, set.flatBlend)));
                    // pinggir peta naik sedikit sebagai tembok alami
                    float edge = Mathf.Clamp01((Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) - set.size.x * 0.42f) / (set.size.x * 0.08f));
                    h[z, x] = 0.08f + n * set.hillAmount * 0.5f * flat + edge * 0.25f;
                }
            td.SetHeights(0, 0, h);

            // layer
            Color[] fb = { new Color(.30f, .52f, .24f), new Color(.45f, .36f, .26f), new Color(.45f, .45f, .47f), new Color(.80f, .72f, .52f) };
            string[] fn = { "grass", "dirt", "rock", "sand" };
            var layers = new TerrainLayer[4];
            for (int i = 0; i < 4; i++) layers[i] = MakeLayer(set.layers[i], fb[i], "HG_" + fn[i]);
            td.terrainLayers = layers;

            // cat otomatis
            int ar = td.alphamapResolution;
            var am = new float[ar, ar, 4];
            for (int z = 0; z < ar; z++)
                for (int x = 0; x < ar; x++)
                {
                    float u = x / (float)(ar - 1), v = z / (float)(ar - 1);
                    float height = td.GetInterpolatedHeight(u, v) / set.size.y;
                    float slope = td.GetSteepness(u, v);
                    float wx = u * set.size.x, wz = v * set.size.z;
                    float nz = Mathf.PerlinNoise(ox + 100f + wx * 0.05f, oz + 100f + wz * 0.05f);
                    float rock = Mathf.SmoothStep(0f, 1f, (slope - set.rockSlope) / 8f);
                    float sand = height < set.sandHeight ? 1f : 0f;
                    float dirt = Mathf.Clamp01((nz - (1f - set.dirtNoise)) * 4f);
                    float dx = wx - set.size.x * .5f, dz = wz - set.size.z * .5f;
                    if (Mathf.Sqrt(dx * dx + dz * dz) < set.flatRadius) dirt = Mathf.Max(dirt, 0.15f);
                    float g = 1f; float r = rock, s = sand * (1f - r), di = dirt * (1f - r) * (1f - s);
                    g = Mathf.Max(0f, 1f - r - s - di);
                    float tot = g + di + r + s;
                    am[z, x, 0] = g / tot; am[z, x, 1] = di / tot; am[z, x, 2] = r / tot; am[z, x, 3] = s / tot;
                }
            td.SetAlphamaps(0, 0, am);

            // pohon
            var protos = new System.Collections.Generic.List<TreePrototype>();
            foreach (var t in set.trees) if (t != null) protos.Add(new TreePrototype { prefab = t });
            if (protos.Count > 0) td.treePrototypes = protos.ToArray();

            // rumput detail
            if (set.grassTexture != null)
            {
                td.SetDetailResolution(512, 16);
                td.detailPrototypes = new[] { new DetailPrototype {
                    prototypeTexture = set.grassTexture, renderMode = DetailRenderMode.GrassBillboard,
                    minWidth = set.grassMinSize, maxWidth = set.grassMaxSize, minHeight = set.grassMinSize, maxHeight = set.grassMaxSize,
                    healthyColor = Color.white, dryColor = new Color(.85f, .8f, .55f) } };
                var det = new int[512, 512];
                var rnd = new System.Random(set.seed);
                for (int z = 0; z < 512; z++)
                    for (int x = 0; x < 512; x++)
                    {
                        float u = x / 511f, v = z / 511f;
                        int ax = Mathf.Clamp(Mathf.RoundToInt(u * (ar - 1)), 0, ar - 1), az = Mathf.Clamp(Mathf.RoundToInt(v * (ar - 1)), 0, ar - 1);
                        if (am[az, ax, 0] > 0.6f && rnd.NextDouble() < 0.25 * set.grassDensity) det[z, x] = 1;
                    }
                td.SetDetailLayer(0, 0, 0, det);
            }

            AssetDatabase.CreateAsset(td, TerDir + "/HG_TerrainData.asset");
            var go = Terrain.CreateTerrainGameObject(td);
            go.name = "HG_Terrain";
            go.transform.position = new Vector3(-set.size.x * .5f, 0f, -set.size.z * .5f);
            Undo.RegisterCreatedObjectUndo(go, "Buat Terrain");
            var terr = go.GetComponent<Terrain>();

            // letakkan pohon, hindari tanah datar tengah, lereng curam, dan pasir
            int placed = 0;
            if (protos.Count > 0 && set.treeCount > 0)
            {
                var rnd = new System.Random(set.seed + 5);
                for (int tries = 0; tries < set.treeCount * 8 && placed < set.treeCount; tries++)
                {
                    float u = (float)rnd.NextDouble(), v = (float)rnd.NextDouble();
                    float wx = (u - .5f) * set.size.x, wz = (v - .5f) * set.size.z;
                    if (Mathf.Sqrt(wx * wx + wz * wz) < set.flatRadius + 8f) continue;
                    if (td.GetSteepness(u, v) > set.rockSlope) continue;
                    if (td.GetInterpolatedHeight(u, v) / set.size.y < set.sandHeight) continue;
                    var ti = new TreeInstance
                    {
                        prototypeIndex = rnd.Next(protos.Count),
                        position = new Vector3(u, 0f, v),
                        widthScale = Mathf.Lerp(set.treeMinScale, set.treeMaxScale, (float)rnd.NextDouble()),
                        color = Color.white, lightmapColor = Color.white,
                    };
                    ti.heightScale = ti.widthScale;
                    terr.AddTreeInstance(ti);
                    placed++;
                }
                terr.Flush();
            }

            AssetDatabase.SaveAssets();
            Selection.activeGameObject = go;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            msg = "Terrain jadi (" + placed + " pohon). Pusatnya (0,0,0) datar untuk kota; geser objek lain bila perlu.";
        }
    }
}
