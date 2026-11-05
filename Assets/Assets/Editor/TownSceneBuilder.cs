using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using HunterGuild;

/// <summary>
/// Menu: Hunter Guild > Build Town Scene
/// KOTA BENTENG TIGA TEMBOK (gaya fantasi abad pertengahan ala anime isekai), diameter sekitar 660 m.
///
///   Tembok Luar   (r 330)  -> sektor pertanian: ladang gandum, sayur, kebun apel & anggur, peternakan, kincir angin
///   Tembok Dalam  (r 215)  -> seluruh kota & perekonomian: 4 alun-alun (Guild, Pasar, Akademi, Katedral), ratusan rumah
///   Tembok Tengah (r 100)  -> kediaman bangsawan: Kastil Penguasa di pusat + 8 rumah bangsawan
///
/// Tiap tembok punya 4 gerbang (utara, timur, selatan, barat) = 12 gerbang beranimasi (terali naik, daun pintu berayun).
/// Utara = +Z. Pemain datang dari Gerbang Luar Selatan (jalan ke Hutan Lerai). Gerbang di sumbu yang sama sehingga
/// terbentuk satu jalan lurus dari gerbang luar sampai pintu kastil.
///
/// Hierarki: Town > Environment | Walls | Gates | Roads | Buildings | Houses | Noble | Farmland | Props | NPCs | NPC_Waypoints
/// Tiap bangunan interaktif: NamaBangunan > Model + InteractPoint. Untuk mengganti dengan prefab sendiri: hapus "Model", taruh
/// prefab sebagai child (0,0,0) dengan pintu menghadap +Z lokal; jangan hapus InteractPoint.
/// </summary>
public static partial class TownSceneBuilder
{
    const string ScenePath = "Assets/Scenes/Town.unity";
    const string MatDir = "Assets/Materials/Town3";
    const string MeshDir = "Assets/Meshes/Town3";

    // ---- geometri tata kota
    internal const float PlazaRing = 158f, PlazaR = 34f;
    static readonly float[] PlazaAz = { 180f, 90f, 0f, 270f };                   // Selatan (Guild), Timur (Pasar), Utara (Akademi), Barat (Katedral)
    static readonly string[] PlazaName = { "Alun-alun Guild", "Alun-alun Pasar", "Alun-alun Akademi", "Alun-alun Katedral" };

    public const float GateSouthZ = -330f;       // garis tengah Gerbang Luar Selatan (dipakai juga oleh scene hutan)

    // ---- state
    static Transform tTown, tEnv, tWalls, tGates, tRoads, tBuildings, tHouses, tNoble, tFarm, tProps, tNpcs, tWp;
    static Material[] baseSlots;
    static Material mMarker;
    static int houseCount, npcCount;
    static readonly Dictionary<long, List<Vector3>> grid = new Dictionary<long, List<Vector3>>();

    [MenuItem("Hunter Guild/Build Town Scene")]
    public static void BuildTown()
    {
        T2BuildTown();           // v35z102n75: Kota Ashwatch "Bukit Abu" (denah, medan, tembok, bangunan, dan prop baru). Denah lama (3 tembok konsentris): menu Legacy di bawah.
    }

    [MenuItem("Hunter Guild/Legacy/Build Town Scene LAMA (3 tembok, n74)")]
    public static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            if (!HGDlg.Show("Hunter Guild",
                "Town.unity sudah ada.\nMembangun ulang akan MENIMPA scene itu, termasuk bangunan yang sudah kamu ganti.\n\nLanjutkan?",
                "Timpa", "Batal")) return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BuildingMover.CaptureBeforeRebuild(ScenePath); complexScene = null;          // simpan posisi bangunan yang kamu pindahkan manual

        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Materials");
        EnsureFolder(MatDir);
        EnsureFolder("Assets/Meshes");
        EnsureFolder(MeshDir);

        EditorUtility.DisplayProgressBar("Hunter Guild", "Membuat tekstur dan material...", 0.04f);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Random.InitState(20260101);
        grid.Clear();
        solidBoxes.Clear();
        streets.Clear();
        houseCount = 0;
        npcCount = 0;
        TownMats.Load();
        NpcKit.Reset();
        GateKit.Reset();
        PropKit.Reset();
        baseSlots = TownMats.Slots();
        mMarker = UMat("Marker", new Color(1f, 0.85f, 0.2f));

        tTown = new GameObject("Town").transform;
        tEnv = Group("Environment", tTown);
        tWalls = Group("Walls", tTown);
        tGates = Group("Gates", tTown);
        tRoads = Group("Roads", tTown);
        tBuildings = Group("Buildings", tTown);
        tHouses = Group("Houses", tTown);
        tNoble = Group("Noble", tTown);
        tFarm = Group("Farmland", tTown);
        tProps = Group("Props", tTown);
        tNpcs = Group("NPCs", tTown);
        tWp = Group("NPC_Waypoints", tTown);

        EditorUtility.DisplayProgressBar("Hunter Guild", "Tanah, tembok, dan 12 gerbang...", 0.1f);
        BuildGround();
        BuildWallsAndGates();

        EditorUtility.DisplayProgressBar("Hunter Guild", "Bangunan penting...", 0.25f);
        ReserveStructural();
        BuildPlazaBuildings();
        BuildNobleBuildings();

        EditorUtility.DisplayProgressBar("Hunter Guild", "Jalan dan alun-alun...", 0.4f);
        BuildRoads();
        BuildPlazaSurfaces();

        EditorUtility.DisplayProgressBar("Hunter Guild", "Rumah warga...", 0.5f);
        PlacePropertyComplex(GuildConfig.TownScene, new Vector2(0f, -42f));      // Komplek Properti dipesan lebih dulu
        BuildHouses();

        EditorUtility.DisplayProgressBar("Hunter Guild", "Kediaman bangsawan dan taman...", 0.65f);
        BuildNobleGardens();

        EditorUtility.DisplayProgressBar("Hunter Guild", "Sektor pertanian...", 0.75f);
        BuildFarmland();
        BuildOutside();
        BuildCorridor();
        BuildCorridorEast();
        BuildCorridorNorth();
        BuildCorridorWest();

        EditorUtility.DisplayProgressBar("Hunter Guild", "Properti, NPC, pemain...", 0.88f);
        BuildProps();
        BuildNpcs();
        BuildHiddenNpcs();
        BuildEvents();
        BuildPlayerAndManagers();

        MarkStatic(tEnv); MarkStatic(tWalls); MarkStatic(tGates); MarkStatic(tRoads); MarkStatic(tBuildings);
        MarkStatic(tHouses); MarkStatic(tNoble); MarkStatic(tFarm); MarkStatic(tProps);
        BuildingMover.Prepare("Town", new[] { tBuildings, tHouses, tNoble }, new[] { tProps, tBuildings, tHouses, tNoble });

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0012f;
        RenderSettings.fogColor = new Color(0.62f, 0.72f, 0.78f);

        EditorUtility.DisplayProgressBar("Hunter Guild", "Menyimpan scene...", 0.96f);
        AssetDatabase.SaveAssets();
        HeightBake.BackupScene(ScenePath); EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings(ScenePath);
        EditorUtility.ClearProgressBar();
        HGDlg.Show("Hunter Guild",
            "Ashwatch 3 Tembok dibuat: " + ScenePath + "\n12 gerbang, " + houseCount + " rumah, " + npcCount + " NPC (4 ras), 10 NPC tersembunyi, 8 kejadian acak.\nTekan Play untuk mencoba.", "OK");
    }

    // =====================================================================
    // Koordinat bantu
    // =====================================================================
    internal static Vector2 PlazaCenter(int i) { return WallKit.Out(PlazaAz[i]) * PlazaRing; }

    /// <summary>Posisi di bingkai plaza i: lateral = sepanjang tembok (searah jarum jam), radial = menjauhi pusat kota.</summary>
    internal static Vector2 PlazaPos(int i, float lateral, float radial)
    {
        float a = PlazaAz[i] * Mathf.Deg2Rad;
        Vector2 outv = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
        Vector2 cw = new Vector2(Mathf.Cos(a), -Mathf.Sin(a));
        return PlazaCenter(i) + cw * lateral + outv * radial;
    }

    /// <summary>Yaw agar +Z lokal menghadap titik target.</summary>
    static float YawToward(Vector2 from, Vector2 to)
    {
        Vector2 d = to - from;
        return Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
    }

    static Vector3 V3(Vector2 p, float y = 0f) { return new Vector3(p.x, y, p.y); }

    // =====================================================================
    // Ruang terpakai (grid lingkaran)
    // =====================================================================
    static long Key(int cx, int cz) { return (long)(cx + 2000) * 5000L + (cz + 2000); }

    static void Reserve(float x, float z, float r)
    {
        long k = Key(Mathf.FloorToInt(x / 16f), Mathf.FloorToInt(z / 16f));
        List<Vector3> l;
        if (!grid.TryGetValue(k, out l)) { l = new List<Vector3>(); grid[k] = l; }
        l.Add(new Vector3(x, z, r));
    }

    static bool IsFree(float x, float z, float r)
    {
        int cx = Mathf.FloorToInt(x / 16f), cz = Mathf.FloorToInt(z / 16f);
        int R = Mathf.CeilToInt((r + 40f) / 16f) + 1;
        for (int i = cx - R; i <= cx + R; i++)
            for (int j = cz - R; j <= cz + R; j++)
            {
                List<Vector3> l;
                if (!grid.TryGetValue(Key(i, j), out l)) continue;
                foreach (var c in l)
                {
                    float dx = c.x - x, dz = c.y - z, rr = c.z + r;
                    if (dx * dx + dz * dz < rr * rr) return false;
                }
            }
        return true;
    }

    static void BoxCircles(float cx, float cz, float w, float d, float yaw, float pad, List<Vector3> outList)
    {
        // jaring lingkaran rapat yang menutupi seluruh kotak (termasuk sudut), diperlebar sebesar pad
        float a = yaw * Mathf.Deg2Rad;
        Vector2 fwd = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
        Vector2 right = new Vector2(fwd.y, -fwd.x);
        float step = 2.4f, rc = step * 0.75f + pad;
        int nx = Mathf.Max(1, Mathf.CeilToInt(w / step)), nz = Mathf.Max(1, Mathf.CeilToInt(d / step));
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
            {
                float lx = ((i + 0.5f) / nx - 0.5f) * w, lz = ((j + 0.5f) / nz - 0.5f) * d;
                outList.Add(new Vector3(cx + right.x * lx + fwd.x * lz, cz + right.y * lx + fwd.y * lz, rc));
            }
    }

    static readonly List<Vector3> tmpCircles = new List<Vector3>();

    static bool BoxFree(float cx, float cz, float w, float d, float yaw, float pad)
    {
        tmpCircles.Clear();
        BoxCircles(cx, cz, w, d, yaw, pad, tmpCircles);
        foreach (var c in tmpCircles) if (!IsFree(c.x, c.y, c.z)) return false;
        return true;
    }

    // kotak padat (cx, cz, w, d, yaw) untuk memfilter rute NPC supaya tidak menembus bangunan
    static readonly List<float[]> solidBoxes = new List<float[]>();

    static bool InSolid(Vector2 p, float pad)
    {
        foreach (var b in solidBoxes)
        {
            float yr = b[4] * Mathf.Deg2Rad, c = Mathf.Cos(yr), s = Mathf.Sin(yr);
            float dx = p.x - b[0], dz = p.y - b[1];
            float lx = dx * c - dz * s, lz = dx * s + dz * c;
            if (Mathf.Abs(lx) <= b[2] / 2f + pad && Mathf.Abs(lz) <= b[3] / 2f + pad) return true;
        }
        return false;
    }

    static bool SegmentClear(Vector2 a, Vector2 b, float pad)
    {
        float len = Vector2.Distance(a, b);
        int n = Mathf.Max(1, Mathf.CeilToInt(len / 0.75f));
        for (int i = 0; i <= n; i++) if (InSolid(Vector2.Lerp(a, b, i / (float)n), pad)) return false;
        return true;
    }

    /// <summary>Potong rute: ambil rangkaian titik terpanjang yang tidak menembus kotak padat (bangunan).</summary>
    static List<Vector2> ClearRoute(List<Vector2> pts)
    {
        if (solidBoxes.Count == 0 || pts.Count < 2) return pts;
        List<Vector2> best = new List<Vector2>(), cur = new List<Vector2>();
        for (int i = 0; i < pts.Count; i++)
        {
            bool ok = !InSolid(pts[i], 0.6f);
            if (ok && cur.Count > 0 && !SegmentClear(cur[cur.Count - 1], pts[i], 0.5f)) { if (cur.Count > best.Count) best = cur; cur = new List<Vector2>(); }
            if (ok) cur.Add(pts[i]);
            else { if (cur.Count > best.Count) best = cur; cur = new List<Vector2>(); }
        }
        if (cur.Count > best.Count) best = cur;
        if (best.Count < 2) { var one = new List<Vector2>(); one.Add(pts[0]); one.Add(pts[0] + new Vector2(0.01f, 0f)); return one; }
        return best;
    }

    static void ReserveBox(float cx, float cz, float w, float d, float yaw, float pad)
    {
        solidBoxes.Add(new[] { cx, cz, w + pad * 2f, d + pad * 2f, yaw });
        tmpCircles.Clear();
        BoxCircles(cx, cz, w, d, yaw, pad, tmpCircles);
        foreach (var c in tmpCircles) Reserve(c.x, c.y, c.z);
    }


    // =====================================================================
    // Jejak sebenarnya (mesh) dari rumah dan landmark: dipakai agar tidak ada bangunan yang saling menembus
    // =====================================================================
    static readonly Dictionary<HouseKit.HouseDef, float[]> houseFp = new Dictionary<HouseKit.HouseDef, float[]>();

    /// <summary>Jejak mesh rumah dalam ruang lokal: [setengah lebar, z belakang, z depan]. Depan = +Z (menghadap jalan).</summary>
    static float[] Footprint(HouseKit.HouseDef def)
    {
        float[] f;
        if (houseFp.TryGetValue(def, out f)) return f;
        float hw = def.w / 2f, z0 = -def.d / 2f, z1 = def.d / 2f;
        if (def.proto != null && def.proto.mesh != null)
        {
            float mnx = 1e9f, mxx = -1e9f, mnz = 1e9f, mxz = -1e9f;
            foreach (var v in def.proto.mesh.vertices)
            {
                if (v.x < mnx) mnx = v.x; if (v.x > mxx) mxx = v.x;
                if (v.z < mnz) mnz = v.z; if (v.z > mxz) mxz = v.z;
            }
            if (mxx > mnx) { hw = Mathf.Max(Mathf.Abs(mnx), Mathf.Abs(mxx)); z0 = mnz; z1 = mxz; }
        }
        f = new[] { hw + 0.15f, z0 - 0.15f, z1 + 0.15f };
        houseFp[def] = f;
        return f;
    }

    /// <summary>Pusat kotak jejak di dunia untuk rumah yang titik asalnya di c.</summary>
    static Vector2 FootCenter(Vector2 c, float yaw, float[] f)
    {
        float a = yaw * Mathf.Deg2Rad;
        return c + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * ((f[1] + f[2]) / 2f);
    }

    /// <summary>Hitung batas mesh sebuah landmark (ruang lokal akar) lalu cadangkan seluruhnya, termasuk beranda dan tangga.</summary>
    static void ReserveMeshFootprint(Transform model, Vector2 pos, float yaw, float pad)
    {
        float mnx = 1e9f, mxx = -1e9f, mnz = 1e9f, mxz = -1e9f;
        bool any = false;
        foreach (var mf in model.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            foreach (var v in mf.sharedMesh.vertices)
            {
                if (v.x < mnx) mnx = v.x; if (v.x > mxx) mxx = v.x;
                if (v.z < mnz) mnz = v.z; if (v.z > mxz) mxz = v.z;
                any = true;
            }
        }
        if (!any) return;
        float a = yaw * Mathf.Deg2Rad;
        Vector2 fwd = new Vector2(Mathf.Sin(a), Mathf.Cos(a)), right = new Vector2(fwd.y, -fwd.x);
        float cx = (mnx + mxx) / 2f, cz = (mnz + mxz) / 2f;
        Vector2 wc = pos + right * cx + fwd * cz;
        ReserveBox(wc.x, wc.y, (mxx - mnx) + pad * 2f, (mxz - mnz) + pad * 2f, yaw, 0f);
    }

    // =====================================================================
    // Util scene
    // =====================================================================
    static Transform Group(string name, Transform parent)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }

    static void MarkStatic(Transform group)
    {
        foreach (var t in group.GetComponentsInChildren<Transform>(true))
        {
            if (t.GetComponent<MeshRenderer>() == null) continue;
            if (t.GetComponentInParent<SpinObject>() != null) continue;
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
        }
    }

    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material m, bool collider = true)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static void AddLight(Transform parent, Vector3 localPos, float range, float intensity)
    {
        var go = new GameObject("PointLight");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = range;
        l.intensity = intensity;
        l.color = new Color(1f, 0.8f, 0.5f);
        go.AddComponent<NightLight>();
    }

    static Material UMat(string name, Color c)
    {
        string path = MatDir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) { m.color = c; return m; }
        Shader sh = GraphicsSettings.currentRenderPipeline != null ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
        if (sh == null) sh = Shader.Find("Standard");
        m = new Material(sh);
        m.color = c;
        m.name = System.IO.Path.GetFileNameWithoutExtension(path); AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int i = path.LastIndexOf('/');
        EnsureFolder(path.Substring(0, i));
        AssetDatabase.CreateFolder(path.Substring(0, i), path.Substring(i + 1));
    }

    static void AddToBuildSettings(string path)
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in list) if (s.path == path) return;
        list.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    /// <summary>Pasang satu bangunan interaktif: root &gt; Model (mesh) + InteractPoint. Kembalikan komponen interaksinya.</summary>
    static T Landmark<T>(string name, Transform parent, Vector2 pos, float yaw, System.Action<GeoBag> build, Material[] slots,
        float w, float d, float h, float forecourt, string prompt, float baseY = 0f) where T : Interactable
    {
        var root = new GameObject(name).transform;
        root.SetParent(parent, false);
        root.position = new Vector3(pos.x, baseY, pos.y);
        root.rotation = Quaternion.Euler(0f, yaw, 0f);
        var model = new GameObject("Model").transform;        // hapus child ini untuk mengganti dengan prefab sendiri
        model.SetParent(root, false);
        var bag = new GeoBag();
        build(bag);
        bag.Emit(model, "Mesh", slots, MeshDir + "/LM_" + name + ".asset");
        var col = new GameObject("Collider");
        col.transform.SetParent(model, false);
        var bc = col.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, h / 2f, 0f);
        bc.size = new Vector3(w, h, d);

        var ip = new GameObject("InteractPoint").transform;
        ip.SetParent(root, false);
        ip.localPosition = new Vector3(0f, 0f, d / 2f + forecourt);
        var comp = ip.gameObject.AddComponent<T>();
        comp.prompt = prompt;
        comp.radius = 3.6f;
        Prim(PrimitiveType.Cylinder, "Marker", ip, new Vector3(0f, 0.16f, 0f), new Vector3(2.4f, 0.03f, 2.4f), mMarker, false);
        AddLight(root, new Vector3(0f, 4.5f, d / 2f + 1.5f), 14f, 1.4f);
        ReserveBox(pos.x, pos.y, w + 2f, d + 2f, yaw, 0f);
        ReserveMeshFootprint(model, pos, yaw, 0.5f);
        Vector2 fwd = new Vector2(Mathf.Sin(yaw * Mathf.Deg2Rad), Mathf.Cos(yaw * Mathf.Deg2Rad));
        Vector2 fc = pos + fwd * (d / 2f + forecourt);
        Reserve(fc.x, fc.y, 4f);
        return comp;
    }

    /// <summary>Ganti komponen fungsi bangunan di titik interaksi (Inn, Blacksmith, ...) dengan pintu ke scene interiornya.
    /// Fungsi aslinya kini ada di dalam interior (Hunter Guild > Build Interiors).</summary>
    static BuildingDoor Door(Interactable comp, string interiorId)
    {
        var go = comp.gameObject;
        string prompt = comp.prompt; float r = comp.radius;
        Object.DestroyImmediate(comp);
        var d = go.AddComponent<BuildingDoor>();
        d.interiorId = interiorId; d.prompt = "Masuk: " + prompt; d.radius = r;
        return d;
    }

    static GameObject Sign(Transform root, string text, Vector3 localPos, float charSize)
    {
        var model = root.Find("Model");
        return BuildKit.Text(model != null ? model : root, text, localPos, 180f, charSize, Color.white);
    }
}
