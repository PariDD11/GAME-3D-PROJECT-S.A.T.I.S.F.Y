using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Hutan Lerai - desa kecil (Alder), desa terbengkalai (Marrow, sarang bandit), titik penyergapan bandit, efek dunia (daun berguguran, kunang-kunang).
/// Semua koordinat di sini adalah koordinat DESAIN (dicerminkan ke dunia lewat G()).
/// </summary>
public static partial class HuntSceneBuilder
{
    class Plc { public string kind; public float x, z, yaw, ruin, a, b; public int seed, roof; }

    static readonly List<Plc> plan = new List<Plc>();
    static Transform tAlder, tMarrow, tVillagers;
    static Material mFxLeaf, mFxGlow, mFxSmoke;
    static System.Random vRng;

    static Vector2 Pt(Vector2 c, float angDeg, float r)
    {
        float a = angDeg * Mathf.Deg2Rad;
        return c + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * r;
    }

    static float FaceYaw(Vector2 from, Vector2 to) { return Mathf.Atan2(to.x - from.x, to.y - from.y) * Mathf.Rad2Deg; }

    /// <summary>Titik di depan bangunan (lokal x,z) dalam koordinat desain.</summary>
    static Vector2 Local(Vector2 p, float yawDeg, float lx, float lz)
    {
        float t = yawDeg * Mathf.Deg2Rad;
        return p + new Vector2(lx * Mathf.Cos(t) + lz * Mathf.Sin(t), -lx * Mathf.Sin(t) + lz * Mathf.Cos(t));
    }

    static Plc Add(string kind, float x, float z, float yaw, int seed = 0, float ruin = 0f, float a = 0f, float b = 0f, int roof = 0, float clear = 0f)
    {
        var p = new Plc { kind = kind, x = x, z = z, yaw = yaw, seed = seed, ruin = ruin, a = a, b = b, roof = roof };
        plan.Add(p);
        if (clear > 0f) clearings.Add(new Vector3(x, z, clear));
        return p;
    }

    // =====================================================================
    // Rencana tata letak (dipanggil sebelum terrain, supaya jalan setapak ikut dicat)
    // =====================================================================
    static void PlanVillages()
    {
        plan.Clear();
        PlanAlder();
        PlanMarrow();
        SanitizePlan();
    }

    // ---------------------------------------------------------------
    // Pembersihan rencana: tidak ada dekorasi yang menimpa bangunan, dan tanah di bawah bangunan diratakan
    // supaya tidak ada atap/dinding yang "melayang" di lereng.
    // ---------------------------------------------------------------
    static float PlanRadius(Plc p)
    {
        switch (p.kind)
        {
            case "house": case "rhouse": case "shouse": return 5.5f;
            case "dhouse": return Mathf.Max(p.a, p.b) * 0.5f + 2.4f;
            case "inn": case "sinn": return 8f;
            case "dinn": return 7.5f;
            case "barn": return 8.5f;
            case "windmill": return 6f;
            case "tower": return 5f;
            case "shrine": case "dshrine": return 4.5f;
            case "domeshrine": return 5.5f;
            case "stall": case "dstall": return 2.8f;
            case "tent": return 3.2f;
            case "chapel": return 9f;
            case "crypt": return 6.5f;
            case "spire": return 8f;
            case "witchhut": return 6.5f;
            case "temple": return 22f;
            case "ziggurat": return 19f;
            case "colossus": return 6.5f;
            case "archway": return 4.5f;
            case "obelisk": return 2.2f;
            case "well": return 2.2f;
            case "crops": return Mathf.Max(p.a, p.b) * 0.5f + 1.2f;
            case "palm": return 1.6f;
            case "deadtree": return 1.4f;
            case "boulder": return 1f + p.a * 0.8f;
            case "cart": return 2.8f;
            case "hay": return 1.8f;
            case "bones": return 1.4f;
        }
        return 0f;
    }

    static bool PlanSolid(string k)
    {
        switch (k)
        {
            case "house": case "rhouse": case "shouse": case "dhouse": case "inn": case "sinn": case "dinn": case "barn":
            case "windmill": case "tower": case "shrine": case "dshrine": case "domeshrine": case "stall": case "dstall":
            case "tent": case "chapel": case "crypt": case "spire": case "witchhut": case "temple": case "ziggurat":
            case "colossus": case "archway": case "well": case "crops": return true;
        }
        return false;
    }

    static bool PlanSprawl(string k)
    {
        return k == "palm" || k == "deadtree" || k == "boulder" || k == "cart" || k == "hay" || k == "bones" || k == "obelisk";
    }

    static void SanitizePlan()
    {
        var drop = new List<Plc>();
        int dropped = 0, clashes = 0;
        for (int i = 0; i < plan.Count; i++)
        {
            var a = plan[i]; float ra = PlanRadius(a);
            if (ra <= 0f || drop.Contains(a)) continue;
            for (int j = 0; j < plan.Count; j++)
            {
                if (i == j) continue;
                var b = plan[j]; float rb = PlanRadius(b);
                if (rb <= 0f || drop.Contains(b)) continue;
                float d = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
                if (d >= ra + rb - 0.3f) continue;
                // dekorasi yang menimpa bangunan: hapus dekorasinya
                if (PlanSprawl(a.kind) && PlanSolid(b.kind)) { drop.Add(a); dropped++; break; }
                // dua bangunan kecil bertabrakan: hapus yang belakangan (landmark besar tidak pernah dihapus)
                if (PlanSolid(a.kind) && PlanSolid(b.kind) && j < i && ra < 9f && rb < 9f && a.kind != "well" && a.kind != "crops")
                {
                    drop.Add(a); dropped++; break;
                }
                if (PlanSolid(a.kind) && PlanSolid(b.kind) && j < i) { clashes++; Debug.LogWarning("Landmark besar bertumpuk: " + a.kind + " & " + b.kind + " pada " + a.x + "," + a.z); }
            }
        }
        foreach (var d in drop) plan.Remove(d);
        Debug.Log("SanitizePlan: " + dropped + " item dihapus karena menimpa bangunan, " + clashes + " tumpukan landmark besar.");
    }

    /// <summary>Ratakan tanah di bawah setiap bangunan (inti = radius bangunan, lalu menyatu mulus ke medan).</summary>
    static void FlattenPlan(float[,] hs, float step)
    {
        int n = hs.GetLength(0);
        float waterN = WaterY / TH;
        var core = new bool[n, n];
        var order = new List<Plc>();
        foreach (var p in plan) if (PlanSolid(p.kind) && p.kind != "crops" && PlanRadius(p) >= 2f) order.Add(p);
        order.Sort((a, b) => PlanRadius(b).CompareTo(PlanRadius(a)));
        foreach (var p in order)
        {
            float R = PlanRadius(p) + 0.8f, B = R + 7f;
            int cx = Mathf.RoundToInt((M * p.x - Origin) / step), cz = Mathf.RoundToInt((M * p.z - Origin) / step);
            if (cx < 0 || cz < 0 || cx >= n || cz >= n) continue;
            float target = hs[cz, cx];
            if (target < waterN + 0.012f) continue;     // di air / tepi air: biarkan (rumah panggung dll.)
            int span = Mathf.CeilToInt(B / step) + 1;
            for (int zi = Mathf.Max(0, cz - span); zi <= Mathf.Min(n - 1, cz + span); zi++)
                for (int xi = Mathf.Max(0, cx - span); xi <= Mathf.Min(n - 1, cx + span); xi++)
                {
                    if (core[zi, xi]) continue;
                    float dx = (xi - cx) * step, dz = (zi - cz) * step;
                    float d = Mathf.Sqrt(dx * dx + dz * dz);
                    if (d > B) continue;
                    if (hs[zi, xi] < waterN - 0.002f) continue;   // jangan menimbun air
                    float k = d <= R ? 1f : 1f - Smooth((d - R) / (B - R));
                    hs[zi, xi] = Mathf.Lerp(hs[zi, xi], target, k);
                    if (d <= R) core[zi, xi] = true;
                }
        }
    }

    static void PlanAlder()
    {
        Vector2 c = AlderC;
        clearings.Add(new Vector3(c.x, c.y, 54f));
        Add("well", c.x, c.y, 0f, 1);

        float[] ang = { 20, 62, 104, 146, 250, 292, 334 };
        float[] rad = { 24, 27, 24, 26, 25, 27, 24 };
        for (int i = 0; i < ang.Length; i++)
        {
            Vector2 p = Pt(c, ang[i], rad[i]);
            float w = 5.2f + (i % 3) * 0.7f, d = 4.4f + (i % 2) * 0.6f;
            float yaw = FaceYaw(p, c);
            Add("house", p.x, p.y, yaw, 10 + i, 0f, w, d, i % 2);
            Vector2 toC = (c - p).normalized;
            trails.Add(new[] { p + toC * (d / 2f + 2.4f), c - toC * 10f });
            if (i % 2 == 0)
            {
                Vector2 ch = Local(p, yaw, w / 2f - 1.0f, 0f);
                Add("smoke", ch.x, ch.y, 0f, 0, 0f, 0.5f + 2.5f + Mathf.Min(2.4f, d * 0.42f) + 1.0f);
            }
        }

        Vector2 innP = Pt(c, 200, 23);
        float innYaw = FaceYaw(innP, c);
        Add("inn", innP.x, innP.y, innYaw, 40, 0f, 9.2f, 6.8f);
        trails.Add(new[] { Local(innP, innYaw, 0f, 7f), c + (innP - c).normalized * 10f });
        Vector2 ich = Local(innP, innYaw, 9.2f / 2f - 1.0f, 0f);
        Add("smoke", ich.x, ich.y, 0f, 0, 0f, 0.5f + 2.7f + 2.4f + 2.6f + 1.0f);

        Vector2 barnP = Pt(c, 255, 47);
        Add("barn", barnP.x, barnP.y, FaceYaw(barnP, c), 50);
        Vector2 millP = Pt(c, 70, 50);
        Add("windmill", millP.x, millP.y, 90f, 60);
        Vector2 shrP = Pt(c, 352, 40);
        Add("shrine", shrP.x, shrP.y, FaceYaw(shrP, c), 70);
        trails.Add(new[] { Local(shrP, FaceYaw(shrP, c), 0f, 7f), c + (shrP - c).normalized * 10f });

        foreach (float a in new float[] { 80, 160, 300 })
        {
            Vector2 sp = Pt(c, a, 13f);
            Add("stall", sp.x, sp.y, FaceYaw(sp, c) + 180f, (int)a);
        }
        for (int i = 0; i < 6; i++)
        {
            Vector2 lp = Pt(c, 30f + i * 60f, 16f);
            Add("lamp", lp.x, lp.y, 0f, i, 0f, i % 2 == 0 ? 1f : 0f);
        }
        Vector2 bp = Pt(c, 330, 14f);
        Add("board", bp.x, bp.y, FaceYaw(bp, c), 1);

        foreach (float[] h in new[] { new[] { 236f, 52f }, new[] { 242f, 57f }, new[] { 264f, 57f }, new[] { 270f, 52f } })
        {
            Vector2 hp = Pt(c, h[0], h[1]);
            Add("hay", hp.x, hp.y, h[0] * 3f, (int)h[0]);
        }
        Vector2 f1 = Pt(c, 228, 66), f2 = Pt(c, 278, 68);
        Add("crops", f1.x, f1.y, FaceYaw(f1, c), 1, 0f, 8f, 6f, 0);
        Add("crops", f2.x, f2.y, FaceYaw(f2, c), 2, 0f, 8f, 6f, 1);
        Add("scarecrow", f1.x, f1.y, FaceYaw(f1, c));
        foreach (float sgn in new[] { -1f, 1f })
        {
            Vector2 fp = Local(f1, FaceYaw(f1, c), sgn * 4.6f, 0f);
            Add("fence", fp.x, fp.y, FaceYaw(f1, c) + 90f, 3, 0f, 6.5f);
        }
        Vector2 cartP = Pt(c, 265, 38);
        Add("cart", cartP.x, cartP.y, 75f, 4);
        for (int k = 0; k < 3; k++)
        {
            Vector2 bp2 = Local(innP, innYaw, -6f + k * 1.1f, 5.2f);
            Add(k == 1 ? "crate" : "barrel", bp2.x, bp2.y, k * 40f, k);
        }
    }

    static void PlanMarrow()
    {
        Vector2 m = MarrowC;
        clearings.Add(new Vector3(m.x, m.y, 50f));
        Add("well", m.x, m.y, 0f, 2, 0.9f);

        Vector2 chap = Pt(m, 15, 16);
        float chapYaw = FaceYaw(chap, m);
        Add("shrine", chap.x, chap.y, chapYaw, 71, 0.85f);
        Add("crystal", chap.x, chap.y, 0f, 5, 0f, 1f);

        float[] ang = { 70, 110, 160, 205, 250, 300, 345 };
        float[] rad = { 26, 24, 29, 25, 27, 25, 28 };
        for (int i = 0; i < ang.Length; i++)
        {
            Vector2 p = Pt(m, ang[i], rad[i]);
            float w = 5.2f + (i % 3) * 0.7f, d = 4.4f + (i % 2) * 0.6f;
            Add("house", p.x, p.y, FaceYaw(p, m), 110 + i, Mathf.Min(1f, 0.55f + (i % 4) * 0.12f), w, d, i % 2);
        }
        Vector2 barn = Pt(m, 235, 38);
        Add("barn", barn.x, barn.y, FaceYaw(barn, m), 120, 0.8f);
        Vector2 tw = Pt(m, 120, 40);
        Add("tower", tw.x, tw.y, 0f, 7);
        Vector2 cart = Pt(m, 40, 10);
        Add("cart", cart.x, cart.y, 30f, 9, 0.8f);

        // kuburan di barat laut desa
        Vector2 gy = Pt(m, 285, 24);
        for (int i = 0; i < 12; i++)
        {
            float gx = gy.x + (i % 4 - 1.5f) * 2.4f + (Mathf.Sin(i * 7.3f)) * 0.4f;
            float gz = gy.y + (i / 4 - 1f) * 2.7f + (Mathf.Cos(i * 5.1f)) * 0.4f;
            Add("grave", gx, gz, 180f + Mathf.Sin(i * 3.1f) * 18f, 130 + i, 0f, i % 3 == 0 ? 1f : 0f);
        }
        Add("fence", gy.x, gy.y + 5.2f, 0f, 5, 0.9f, 8f);
        Add("fence", gy.x - 6.5f, gy.y, 90f, 6, 0.9f, 6f);
        Add("deadtree", gy.x + 6f, gy.y - 4f, 40f, 3);
        Add("deadtree", gy.x - 7f, gy.y + 3f, 120f, 4);
        Add("mush", gy.x + 2f, gy.y + 6.5f, 0f, 1);

        // sarang bandit: api unggun, tenda, bendera, barikade, peti
        Vector2 fire = m + new Vector2(8f, -4f);
        Add("fire", fire.x, fire.y, 0f, 6);
        for (int k = 0; k < 4; k++)
        {
            Vector2 tp = Pt(fire, 30f + k * 90f, 7f);
            Add("tent", tp.x, tp.y, FaceYaw(tp, fire), 140 + k, 0f, 1f);
        }
        Vector2 b1 = m + new Vector2(-3f, 9f), b2 = m + new Vector2(15f, 7f);
        Add("banner", b1.x, b1.y, 0f, 1);
        Add("banner", b2.x, b2.y, 0f, 2);
        for (int k = 0; k < 5; k++)
        {
            Vector2 cp = fire + new Vector2(-2.2f + k * 1.2f, 6.4f + (k % 2) * 0.8f);
            Add(k % 2 == 0 ? "crate" : "barrel", cp.x, cp.y, k * 30f, 50 + k);
        }
        Vector2 dirIn = new Vector2(35f, 30f).normalized;
        Vector2 entry = m - dirIn * 31f;
        float bYaw = Mathf.Atan2(dirIn.x, dirIn.y) * Mathf.Rad2Deg + 90f;
        Vector2 perp = new Vector2(dirIn.y, -dirIn.x);
        Add("barricade", entry.x + perp.x * 5.2f, entry.y + perp.y * 5.2f, bYaw, 1, 0f, 6f);
        Add("barricade", entry.x - perp.x * 5.2f, entry.y - perp.y * 5.2f, bYaw, 2, 0f, 6f);
        Add("barricade", m.x + 36f, m.y - 5f, 90f, 3, 0f, 6f);
        Add("banner", entry.x + perp.x * 8f, entry.y + perp.y * 8f, 0f, 3);

        // dekorasi alam gelap
        Add("crystal", m.x + 22f, m.y + 24f, 0f, 3, 0f, 1f);
        Add("crystal", m.x - 30f, m.y - 16f, 0f, 4, 0f, 0f);
        for (int k = 0; k < 6; k++)
        {
            Vector2 mp = Pt(m, k * 60f + 20f, 34f + (k % 3) * 4f);
            Add(k % 2 == 0 ? "mush" : "log", mp.x, mp.y, k * 50f, 10 + k);
        }
        for (int k = 0; k < 8; k++)
        {
            Vector2 bp = Pt(m, k * 45f + 10f, 40f + (k % 3) * 4f);
            Add("boulder", bp.x, bp.y, k * 70f, 20 + k, 0f, 1.2f + (k % 3) * 0.5f);
        }
        Add("ruinboard", chap.x + 3f, chap.y - 7f, FaceYaw(chap, m), 1);
    }

    // =====================================================================
    // Instansiasi
    // =====================================================================
    static GameObject PlaceKit(Plc p, string key, System.Action<GeoBag> build, Transform parent, bool collider, float scale = 1f)
    {
        var proto = PropKit.Get(key, build);
        var go = PropKit.Place(proto, parent, p.kind, G(p.x, p.z), p.yaw + (M < 0f ? 180f : 0f), scale, ForestKit.Slots);
        if (collider) go.AddComponent<MeshCollider>().sharedMesh = proto.mesh;
        return go;
    }

    static void EnsureFxMats()
    {
        if (mFxLeaf != null) return;
        mFxLeaf = FxMat("FxLeaf", FxUtil.LeafTexture());
        mFxGlow = FxMat("FxGlow", FxUtil.SoftDot(1.6f));
        mFxSmoke = FxMat("FxSmoke", FxUtil.SoftDot(1.1f));
    }

    static Material FxMat(string name, Texture2D tex)
    {
        string tp = TexDir + "/" + name + ".png";
        File.WriteAllBytes(tp, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(tp, ImportAssetOptions.ForceUpdate);
        var ti = AssetImporter.GetAtPath(tp) as TextureImporter;
        if (ti != null) { ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.mipmapEnabled = true; ti.SaveAndReimport(); }
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(tp);
        var m = new Material(FxUtil.SpriteShader());
        m.mainTexture = t;
        AssetDatabase.CreateAsset(m, MatDir + "/" + name + ".mat");
        return m;
    }

    static void BuildVillages()
    {
        EnsureFxMats();
        tAlder = Group("Desa_Alder");
        tMarrow = Group("Desa_Marrow_Terbengkalai");
        foreach (var p in plan)
        {
            bool alder = Dist(p.x, p.z, AlderC.x, AlderC.y) < Dist(p.x, p.z, MarrowC.x, MarrowC.y);
            Transform parent = alder ? tAlder : tMarrow;
            int sd = p.seed;
            float ruin = p.ruin;
            string rk = "_" + (int)(ruin * 100f);
            switch (p.kind)
            {
                case "house":
                {
                    float w = p.a, d = p.b; int roof = p.roof;
                    PlaceKit(p, "HuntHouse_" + sd + "_" + (int)(w * 10f) + "_" + (int)(d * 10f) + rk + "_" + roof, b => ForestKit.House(b, sd, w, d, 2.5f, ruin, roof, true), parent, true);
                    break;
                }
                case "inn": PlaceKit(p, "HuntInn_" + sd, b => ForestKit.Inn(b, sd), parent, true); break;
                case "barn": PlaceKit(p, "HuntBarn_" + sd + rk, b => ForestKit.Barn(b, sd, ruin), parent, true); break;
                case "shrine": PlaceKit(p, "HuntShrine_" + sd + rk, b => ForestKit.Shrine(b, sd, ruin), parent, true); break;
                case "tower": PlaceKit(p, "HuntTower_" + sd, b => ForestKit.RuinTower(b, sd), parent, true); break;
                case "well": PlaceKit(p, "HuntWell" + rk, b => ForestKit.Well(b, sd, ruin), parent, true); break;
                case "stall": PlaceKit(p, "HuntStall_" + (sd % 2), b => ForestKit.Stall(b, sd % 2), parent, true); break;
                case "fence": { float len = p.a; PlaceKit(p, "HuntFence_" + sd + rk + "_" + (int)len, b => ForestKit.Fence(b, sd, len, ruin), parent, false); break; }
                case "hay": PlaceKit(p, "HuntHayBale", b => ForestKit.HayBale(b, 1), parent, true); break;
                case "barrel": PlaceKit(p, "HuntBarrel", b => ForestKit.Barrel(b), parent, true); break;
                case "crate": PlaceKit(p, "HuntCrate_" + (sd % 3), b => ForestKit.Crate(b, sd % 3), parent, true); break;
                case "cart": PlaceKit(p, "HuntCart" + rk, b => ForestKit.Cart(b, sd, ruin), parent, true); break;
                case "scarecrow": PlaceKit(p, "HuntScarecrow", b => ForestKit.Scarecrow(b), parent, false); break;
                case "crops": { float w = p.a, d = p.b; int kind = p.roof; PlaceKit(p, "HuntCrops_" + kind, b => ForestKit.CropRows(b, sd, w, d, kind), parent, false); break; }
                case "windmill":
                {
                    var body = PlaceKit(p, "HuntWindmill", b => ForestKit.WindmillBody(b), parent, true);
                    var blades = PropKit.Place(PropKit.Get("HuntWindmillBlades", b => ForestKit.WindmillBlades(b)), body.transform, "Blades", body.transform.position, 0f, 1f, ForestKit.Slots);
                    blades.transform.localPosition = new Vector3(0f, 6.3f, 2.0f);
                    blades.transform.localRotation = Quaternion.identity;
                    var sp = blades.AddComponent<SpinObject>();
                    sp.axis = Vector3.forward; sp.degreesPerSecond = 22f;
                    break;
                }
                case "lamp":
                {
                    var lamp = PlaceKit(p, "HuntLamp", b => ForestKit.Lamp(b), parent, false);
                    if (p.a > 0.5f)
                    {
                        var l = new GameObject("LampLight").AddComponent<Light>();
                        l.transform.SetParent(lamp.transform, false);
                        l.transform.localPosition = new Vector3(0.45f, 2.3f, 0f);
                        l.type = LightType.Point; l.range = 15f; l.intensity = 1.1f; l.color = new Color(1f, 0.75f, 0.45f);
                    }
                    break;
                }
                case "board":
                {
                    var bd = PlaceKit(p, "HuntBoard", b => ForestKit.NoticeBoard(b), parent, false);
                    var ip = bd.AddComponent<InfoPoint>();
                    ip.prompt = "Baca papan pengumuman"; ip.radius = 3f; ip.title = "Papan Desa Alder";
                    ip.message = "PERINGATAN UNTUK PARA HUNTER\n\n- Bandit menyergap pelancong di Jalan Tua menuju Desa Marrow (tenggara). Jumlah mereka banyak; jangan pergi sendirian tanpa persiapan.\n- Desa Marrow ditinggalkan sejak wabah dan kini dikuasai bandit. Jangan berkemah di sana.\n- Penginapan Alder menerima tamu kapan saja. Sumur desa gratis.\n\n- Tetua Faelar";
                    break;
                }
                case "ruinboard":
                {
                    var bd = PlaceKit(p, "HuntBoardRuin", b => ForestKit.NoticeBoard(b), parent, false);
                    var ip = bd.AddComponent<InfoPoint>();
                    ip.prompt = "Periksa papan lapuk"; ip.radius = 3f; ip.title = "Papan Lapuk";
                    ip.message = "Kertas-kertas di papan sudah membusuk. Satu lembar masih terbaca:\n\n\"... wabah semakin parah. Siapa pun yang membaca ini, pergilah ke Alder. Jangan bawa apa pun dari Marrow ...\"\n\nDi bawahnya, bendera merah baru ditancapkan dengan paku. Seseorang tinggal di sini sekarang.";
                    break;
                }
                case "tent": { int cs = p.a > 0.5f ? Sl.ClothB : Sl.Cloth; PlaceKit(p, "HuntTent_" + cs, b => ForestKit.Tent(b, sd, cs), parent, true, 1.25f); break; }
                case "fire":
                {
                    var f = PlaceKit(p, "HuntBanditFire", b => ForestKit.Campfire(b, sd), parent, false, 1.3f);
                    var l = new GameObject("FireLight").AddComponent<Light>();
                    l.transform.SetParent(f.transform, false);
                    l.transform.localPosition = new Vector3(0, 1.4f, 0);
                    l.type = LightType.Point; l.range = 20f; l.intensity = 1.7f; l.color = new Color(1f, 0.55f, 0.22f);
                    var sm = new GameObject("FireSmoke").AddComponent<ChimneySmoke>();
                    sm.material = mFxSmoke;
                    sm.transform.SetParent(f.transform, false);
                    sm.transform.localPosition = new Vector3(0, 1.4f, 0);
                    break;
                }
                case "barricade": { float len = p.a; PlaceKit(p, "HuntBarricade_" + sd + "_" + (int)len, b => ForestKit.Barricade(b, sd, len), parent, true); break; }
                case "banner": PlaceKit(p, "HuntBanner_" + sd, b => ForestKit.BannerPole(b, sd), parent, false); break;
                case "grave": { bool cross = p.a > 0.5f; PlaceKit(p, "HuntGrave_" + sd + (cross ? "x" : "r"), b => ForestKit.Gravestone(b, sd, cross), parent, false); break; }
                case "crystal":
                {
                    var cr = PlaceKit(p, "HuntCrystalV_" + sd, b => ForestKit.CrystalCluster(b, sd), parent, false, p.a > 0.5f ? 1.8f : 1.2f);
                    if (p.a > 0.5f)
                    {
                        var l = new GameObject("CrystalLight").AddComponent<Light>();
                        l.transform.SetParent(cr.transform, false);
                        l.transform.localPosition = new Vector3(0, 2.0f, 0);
                        l.type = LightType.Point; l.range = 18f; l.intensity = 1.5f; l.color = new Color(0.4f, 0.9f, 1f);
                    }
                    break;
                }
                case "mush": PlaceKit(p, "HuntMushV_" + (sd % 3), b => ForestKit.GlowMushrooms(b, sd % 3), parent, false, 1.5f); break;
                case "log": PlaceKit(p, "HuntLogV_" + (sd % 3), b => ForestKit.FallenLog(b, sd % 3), parent, false); break;
                case "boulder": PlaceKit(p, "HuntBoulderV_" + (sd % 4), b => ForestKit.Boulder(b, sd % 4, 1f, true), parent, p.a > 1.3f, p.a); break;
                case "deadtree": PlaceKit(p, "HuntDeadV_" + (sd % 3), b => ForestKit.DeadTree(b, sd, 1.1f), parent, true); break;
                case "smoke":
                {
                    var sm = new GameObject("ChimneySmoke").AddComponent<ChimneySmoke>();
                    sm.material = mFxSmoke;
                    sm.transform.SetParent(parent, false);
                    sm.transform.position = G(p.x, p.z) + Vector3.up * p.a;
                    break;
                }
            }
        }
        BuildVillageNpcs();
        BuildVillageInteractions();
    }

    // =====================================================================
    // NPC desa & titik interaksi
    // =====================================================================
    // ---------------------------------------------------------------
    // Penempatan NPC yang aman: tidak di dalam pohon, batu, bangunan, atau air.
    // ---------------------------------------------------------------
    static List<Vector2> npcTreeCache; static TerrainData npcTreeTd;
    static List<Vector2> NpcTrees()
    {
        if (npcTreeCache != null && npcTreeTd == td) return npcTreeCache;
        npcTreeTd = td; npcTreeCache = new List<Vector2>();
        foreach (var t in td.treeInstances)
            npcTreeCache.Add(new Vector2((t.position.x * TSize + Origin) / M, (t.position.z * TSize + Origin) / M));
        return npcTreeCache;
    }

    static bool NpcSpotOk(Vector2 p)
    {
        if (GroundY(p.x, p.y) < WaterY + 0.25f) return false;
        if (Slope(p.x, p.y) > 32f) return false;
        foreach (var t in NpcTrees())
        {
            float dx = t.x - p.x, dz = t.y - p.y;
            if (dx * dx + dz * dz < 2.6f * 2.6f) return false;
        }
        Vector3 w = G(p.x, p.y);
        var hits = Physics.OverlapCapsule(w + Vector3.up * 0.5f, w + Vector3.up * 1.6f, 0.5f);
        foreach (var h in hits)
        {
            if (h.GetType().Name == "TerrainCollider" || h.isTrigger) continue;
            if (h.GetComponentInParent<NpcActor>() != null) continue;
            return false;
        }
        return true;
    }

    /// <summary>Cari titik bebas terdekat (spiral) dari p.</summary>
    static int npcWarn;
    static Vector2 NpcSafe(Vector2 p)
    {
        Physics.SyncTransforms();
        if (NpcSpotOk(p)) return p;
        for (float r = 1f; r <= 14f; r += 1f)
        {
            int n = Mathf.Max(8, Mathf.RoundToInt(r * 6f));
            for (int i = 0; i < n; i++)
            {
                float a = (i + (r % 2f) * 0.5f) / n * Mathf.PI * 2f;
                var q = p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (NpcSpotOk(q)) return q;
            }
        }
        if (npcWarn++ < 3) Debug.LogWarning("NPC: tidak menemukan titik bebas dekat " + p + " (titik rute di dekat pagar/bangunan; peringatan berikutnya disembunyikan)");
        return p;
    }

    static Transform[] VRoute(string name, params Vector2[] pts)
    {
        // pecah rute jadi titik tiap <= 4 m lalu geser tiap titik ke tempat bebas, supaya NPC tidak menembus pohon di tengah rute
        var dense = new List<Vector2>();
        for (int i = 0; i < pts.Length; i++)
        {
            if (i == 0) { dense.Add(NpcSafe(pts[0])); continue; }
            float len = Vector2.Distance(pts[i - 1], pts[i]);
            int n = Mathf.Max(1, Mathf.CeilToInt(len / 4f));
            for (int k = 1; k <= n; k++) dense.Add(NpcSafe(Vector2.Lerp(pts[i - 1], pts[i], k / (float)n)));
        }
        var g = Group("WP_" + name);
        var arr = new Transform[dense.Count];
        for (int i = 0; i < dense.Count; i++)
        {
            var t = new GameObject("P" + i).transform;
            t.SetParent(g, false);
            t.position = G(dense[i].x, dense[i].y);
            arr[i] = t;
        }
        return arr;
    }

    static GameObject VNpc(string name, string role, int outfit, int race, bool female, Vector2 pos, float yaw, string lines, Transform[] route, bool guard)
    {
        var spec = NpcKit.RandomSpec(outfit, vRng, race, female ? 1 : 0);
        spec.name = name;
        if (guard) spec.elite = true;
        pos = NpcSafe(pos);
        Vector3 wp = G(pos.x, pos.y);
        var go = NpcKit.Spawn(tVillagers, "NPC_" + name, wp, yaw + (M < 0f ? 180f : 0f), spec, true);
        go.transform.position = wp;
        var actor = go.GetComponent<NpcActor>();
        actor.displayName = name;
        if (!string.IsNullOrEmpty(lines)) actor.customLines = lines.Split('|');
        var lab = BuildKit.Text(go.transform, name + "\n<" + role + ">", new Vector3(0f, actor.height + 0.3f, 0f), 0f, 0.045f, Color.white);
        var npc = go.AddComponent<WanderNPC>();
        npc.waypoints = route;
        npc.label = lab.transform;
        npc.speed = 1.1f + (float)vRng.NextDouble() * 0.4f;
        if (guard) actor.greetRadius = 4f; else go.AddComponent<NpcChat>();
        return go;
    }

    static void BuildVillageNpcs()
    {
        tVillagers = Group("Villagers");
        vRng = new System.Random(515);
        Vector2 c = AlderC;
        Vector2 innP = Pt(c, 200, 23);
        float innYaw = FaceYaw(innP, c);
        Vector2 innDoor = Local(innP, innYaw, ForestKit.DoorX(40, 9.2f) + 1.4f, 9.2f * 0f + 6.8f / 2f + 1.6f);

        VNpc("Bu Marta", "Pemilik Penginapan", NpcKit.OPelayan, NpcKit.Human, true, innDoor, innYaw + 180f,
            "Selamat datang di Alder! Kamar hangat, sup panas, dan tak ada bandit di dalam sini.|Kalau kamu mau ke Marrow, kuatkan hati. Anak buahku pernah pulang dengan baju robek-robek.", null, false);

        Vector2 stall = Pt(c, 80, 13f);
        VNpc("Pak Tomas", "Pedagang Keliling", NpcKit.OPedagang, NpcKit.Human, false, Pt(c, 80, 11.2f), FaceYaw(Pt(c, 80, 11.2f), c) + 180f,
            "Barang bagus, harga bersahabat! Perlengkapan Hunter, ramuan, semua ada.|Jalan tua ke Marrow sepi sejak bandit datang. Daganganku jadi sulit sampai ke kota.", null, false);

        Vector2 shrP = Pt(c, 352, 40);
        VNpc("Tetua Faelar", "Tetua Desa", NpcKit.OPendeta, NpcKit.Elf, false, Local(shrP, FaceYaw(shrP, c), 0f, 6.5f), FaceYaw(shrP, c) + 180f,
            "Dulu Marrow adalah desa kembar kami. Wabah membuat semua orang pergi... atau mati.|Bandit memakai kuil tua di sana sebagai sarang. Aku melihat cahaya biru dari sana di malam hari.|Kalau kamu menumpas mereka, Alder akan berutang budi.", null, false);

        Vector2 gate = c + (new Vector2(0f, 0f) - c).normalized * 40f;
        VNpc("Gorrim", "Penjaga Desa", NpcKit.OPenjaga, NpcKit.Dwarf, false, gate, FaceYaw(gate, c) + 180f,
            "Halt! ...Oh, Hunter. Silakan lewat.|Aku tak berani meninggalkan pos. Kalau bandit turun ke Alder, aku satu-satunya yang berdiri di antara mereka dan lumbung kami.", null, true);

        Vector2 millP = Pt(c, 70, 50);
        VNpc("Pak Hobb", "Penggiling", NpcKit.OPetani, NpcKit.Human, false, Local(millP, 90f, 3.2f, 3.4f), 90f,
            "Kincir ini sudah berputar sejak kakekku. Anginnya bagus hari ini.|Daun-daun maple jatuh tiap musim gugur. Cantik, tapi merepotkan untuk disapu!", null, false);

        var plaza = VRoute("AlderPlaza", Pt(c, 0, 17), Pt(c, 60, 17), Pt(c, 120, 17), Pt(c, 180, 17), Pt(c, 240, 17), Pt(c, 300, 17));
        VNpc("Rina", "Petani", NpcKit.OPetani, NpcKit.Human, true, Pt(c, 0, 17), 0f, "Panen tahun ini lumayan, kalau bandit tidak merampas gerobak.|Hati-hati di hutan. Jangan menyimpang dari jalan.", plaza, false);
        var plaza2 = VRoute("AlderPlaza2", Pt(c, 150, 15), Pt(c, 210, 15), Pt(c, 270, 15), Pt(c, 330, 15));
        VNpc("Kiba", "Anak Desa", NpcKit.OWarga, NpcKit.Beast, false, Pt(c, 150, 15), 0f, "Aku pernah lihat kunang-kunang besar di dekat Marrow! Tapi Ibu melarangku ke sana.|Mau main? Eh, kamu Hunter ya? Keren!", plaza2, false);
        var field = VRoute("AlderField", Pt(c, 232, 56), Pt(c, 250, 62), Pt(c, 272, 60));
        VNpc("Nenek Suri", "Petani", NpcKit.OWarga, NpcKit.Human, true, Pt(c, 232, 56), 0f, "Burung-burung selalu mencuri bijiku. Makanya ada orang-orangan sawah itu.|Dulu Marrow ramai. Anak-anak berlarian di sana...", field, false);
    }

    static void BuildVillageInteractions()
    {
        Vector2 c = AlderC;
        Vector2 innP = Pt(c, 200, 23);
        float innYaw = FaceYaw(innP, c);
        Vector2 door = Local(innP, innYaw, ForestKit.DoorX(40, 9.2f), 6.8f / 2f + 2.2f);
        var io = new GameObject("InnDoor");
        io.transform.SetParent(tAlder, false);
        io.transform.position = G(door.x, door.y);
        var inn = io.AddComponent<Inn>();
        inn.prompt = "Penginapan Alder"; inn.radius = 3.6f;

        Vector2 sp = Pt(c, 80, 11.2f);
        var so = new GameObject("StallShop");
        so.transform.SetParent(tAlder, false);
        so.transform.position = G(sp.x, sp.y);
        var shop = so.AddComponent<GeneralShop>();
        shop.prompt = "Lapak Pak Tomas"; shop.radius = 3.4f;

        var wo = new GameObject("VillageWell");
        wo.transform.SetParent(tAlder, false);
        wo.transform.position = G(c.x, c.y + 1.6f);
        var ws = wo.AddComponent<WaterSource>();
        ws.prompt = "Sumur Desa"; ws.radius = 3.4f;

        Vector2 chap = Pt(MarrowC, 15, 16);
        var co = new GameObject("RuinAltar");
        co.transform.SetParent(tMarrow, false);
        co.transform.position = G(chap.x, chap.y);
        var ip = co.AddComponent<InfoPoint>();
        ip.prompt = "Periksa altar retak"; ip.radius = 5f; ip.title = "Kuil Marrow";
        ip.message = "Atap kuil runtuh, ivy merambat di dinding. Sebuah kristal biru berdenyut lemah di tengah ruangan, seolah masih mengingat doa-doa lama.\n\nDi lantai tampak bekas api unggun dan botol-botol kosong: para bandit pernah bermalam di sini.";
    }

    // =====================================================================
    // Penyergapan bandit
    // =====================================================================
    static void BuildBanditSites()
    {
        var g = Group("BanditEvents");
        MakeAmbush(g, "Ambush_JalanTua", new Vector2(90f, 190f), 20f, 0.7f, 6, 3, false, 120, "Penyergapan di jalan tua!");
        MakeAmbush(g, "Ambush_SarangMarrow", MarrowC + new Vector2(-4f, -2f), 30f, 1f, 8, 4, true, 300, "Sarang bandit!");
    }

    static void MakeAmbush(Transform parent, string name, Vector2 p, float radius, float chance, int swords, int archers, bool leader, int reward, string title)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = G(p.x, p.y);
        var a = go.AddComponent<BanditAmbush>();
        a.triggerRadius = radius; a.chance = chance;
        a.swordsmen = swords; a.archers = archers; a.leader = leader;
        a.reward = reward; a.title = title; a.waterLevel = WaterY;
    }

    // =====================================================================
    // Efek dunia
    // =====================================================================
    static void BuildWorldFx()
    {
        EnsureFxMats();
        var fx = Group("WorldFx");
        var leaves = new GameObject("FallingLeaves").AddComponent<FallingLeaves>();
        leaves.transform.SetParent(fx, false);
        leaves.material = mFxLeaf;
        var ff = new GameObject("Fireflies").AddComponent<FireflyMotes>();
        ff.transform.SetParent(fx, false);
        ff.material = mFxGlow;
    }
}
