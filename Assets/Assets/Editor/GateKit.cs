using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Gerbang kota bergaya kastil fantasi anime: dua menara bulat beratap kerucut, jambang batu dengan lengkung,
/// tympanum berlambang, ruang jaga setengah kayu di atas gerbang, spanduk, lentera, daun pintu kayu berpita besi
/// yang berayun, dan terali besi (portcullis) yang naik-turun. Tier 0 = tembok luar, 1 = tembok dalam, 2 = tembok tengah.
/// Bingkai lokal: +Z = arah keluar dari pusat kota, origin di titik tengah gerbang pada garis tembok.
/// </summary>
public static class GateKit
{
    public class Spec
    {
        public int tier;
        public float W, towerR, towerH, Hdoor, Hb, D, chamberH, roofH, tX;
    }

    public static Spec SpecFor(int tier)
    {
        var s = new Spec { tier = tier };
        switch (tier)
        {
            case 0: s.W = 12f; s.towerR = 6.4f; s.towerH = 30f; s.Hdoor = 8.5f; s.Hb = 20f; s.D = 12f; s.chamberH = 6f; s.roofH = 17f; break;
            case 1: s.W = 11f; s.towerR = 6.0f; s.towerH = 34f; s.Hdoor = 8.0f; s.Hb = 23f; s.D = 12.5f; s.chamberH = 6.5f; s.roofH = 19f; break;
            default: s.W = 11f; s.towerR = 5.6f; s.towerH = 38f; s.Hdoor = 8.0f; s.Hb = 26f; s.D = 12.5f; s.chamberH = 7f; s.roofH = 21f; break;
        }
        s.tX = s.W / 2f + s.towerR + 2.0f;
        return s;
    }

    public static readonly string[] TierName = { "Luar", "Dalam", "Tengah" };

    /// <summary>Material slot untuk tiap tingkat gerbang: tembok luar = batu abu + atap bata, dalam = batu hangat + atap biru, tengah = marmer + emas.</summary>
    public static Material[] SlotsFor(int tier)
    {
        Material[] a;
        if (tier == 0)
        {
            a = TownMats.Slots(TownMats.Plaster[0], TownMats.Roofs[0], TownMats.Plaster[2], TownMats.Roofs[5], TownMats.Cloths[0], TownMats.Cloths[3]);
        }
        else if (tier == 1)
        {
            a = TownMats.Slots(TownMats.Plaster[1], TownMats.Roofs[2], TownMats.Plaster[0], TownMats.Roofs[4], TownMats.Cloths[1], TownMats.Cloths[2]);
            a[Sl.Stone] = TownMats.StoneWarm;
        }
        else
        {
            a = TownMats.Slots(TownMats.Plaster[1], TownMats.Roofs[2], TownMats.Plaster[1], TownMats.Roofs[2], TownMats.Cloths[4], TownMats.Cloths[1]);
            a[Sl.Stone] = TownMats.Marble;
            a[Sl.DarkStone] = TownMats.Stone;
        }
        return a;
    }

    // cache mesh per tier
    static readonly Mesh[] modelMesh = new Mesh[3];
    static readonly int[][] modelSlots = new int[3][];
    static readonly Mesh[] leafMesh = new Mesh[3];
    static readonly int[][] leafSlots = new int[3][];
    static readonly Mesh[] portMesh = new Mesh[3];
    static readonly int[][] portSlots = new int[3][];

    public static void Reset()
    {
        for (int i = 0; i < 3; i++) { modelMesh[i] = null; leafMesh[i] = null; portMesh[i] = null; }
    }

    public class Result
    {
        public GameObject root;
        public AnimatedGate gate;
        public Transform pivotL, pivotR, portcullis;
    }

    const string MeshDir = "Assets/Meshes/Town3";

    static void EnsureDir()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Meshes")) AssetDatabase.CreateFolder("Assets", "Meshes");
        if (!AssetDatabase.IsValidFolder(MeshDir)) AssetDatabase.CreateFolder("Assets/Meshes", "Town3");
    }

    static GameObject Obj(Transform parent, string name, Mesh mesh, int[] used, Material[] slotMats)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mats = new Material[used.Length];
        for (int i = 0; i < used.Length; i++) mats[i] = slotMats[used[i]];
        go.AddComponent<MeshRenderer>().sharedMaterials = mats;
        return go;
    }

    static void Collider(Transform parent, Vector3 center, Vector3 size)
    {
        var go = new GameObject("Col");
        go.transform.SetParent(parent, false);
        var bc = go.AddComponent<BoxCollider>();
        bc.center = center;
        bc.size = size;
    }

    static void CapsuleCol(Transform parent, Vector3 center, float r, float h)
    {
        var go = new GameObject("ColTower");
        go.transform.SetParent(parent, false);
        var cc = go.AddComponent<CapsuleCollider>();
        cc.center = center;
        cc.radius = r;
        cc.height = h;
    }

    // ------------------------------------------------------------------ bangun
    /// <param name="animated">true = pasang AnimatedGate (buka-tutup otomatis); false = pose terbuka statis (replika di scene hutan).</param>
    /// <summary>true = pasang titik 'Minta izin' penjaga (gerbang tertutup sampai diberi izin). Default false: gerbang terbuka.</summary>
    public static bool AskPermission = false;

    public static Result Build(Transform parent, string name, int tier, string signOut, string signIn, Vector3 pos, float yaw, bool animated, bool withLights, bool request = true)
    {
        EnsureDir();
        Spec sp = SpecFor(tier);
        Material[] mats = SlotsFor(tier);
        float W = sp.W, hw = W / 2f, D = sp.D, hd = D / 2f, Hd = sp.Hdoor, Hb = sp.Hb, tX = sp.tX;

        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos;
        root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        var res = new Result { root = root };

        if (modelMesh[tier] == null)
        {
            var b = new GeoBag();
            BuildModel(b, sp);
            modelMesh[tier] = b.ToMesh("Gate_T" + tier, MeshDir + "/Gate_T" + tier + ".asset", out modelSlots[tier]);
            var lb = new GeoBag();
            BuildLeaf(lb, sp);
            leafMesh[tier] = lb.ToMesh("GateLeaf_T" + tier, MeshDir + "/GateLeaf_T" + tier + ".asset", out leafSlots[tier]);
            var pb = new GeoBag();
            BuildPortcullis(pb, sp);
            portMesh[tier] = pb.ToMesh("GatePort_T" + tier, MeshDir + "/GatePort_T" + tier + ".asset", out portSlots[tier]);
        }

        Obj(root.transform, "Model", modelMesh[tier], modelSlots[tier], mats);

        // collider statis
        var cols = new GameObject("Colliders").transform;
        cols.SetParent(root.transform, false);
        float pierW = tX - hw;
        Collider(cols, new Vector3(-(hw + tX) / 2f, Hb / 2f, 0f), new Vector3(pierW + sp.towerR, Hb, D));
        Collider(cols, new Vector3((hw + tX) / 2f, Hb / 2f, 0f), new Vector3(pierW + sp.towerR, Hb, D));
        Collider(cols, new Vector3(0f, (Hd + Hb) / 2f, 0f), new Vector3(W, Hb - Hd, D));
        CapsuleCol(cols, new Vector3(-tX, sp.towerH / 2f, 0f), sp.towerR + 0.5f, sp.towerH);
        CapsuleCol(cols, new Vector3(tX, sp.towerH / 2f, 0f), sp.towerR + 0.5f, sp.towerH);

        // daun pintu
        float doorZ = hd - 1.0f;
        var pl = new GameObject("DoorPivotL").transform;
        pl.SetParent(root.transform, false);
        pl.localPosition = new Vector3(-hw, 0f, doorZ);
        var pr = new GameObject("DoorPivotR").transform;
        pr.SetParent(root.transform, false);
        pr.localPosition = new Vector3(hw, 0f, doorZ);
        bool openGate = false;      // daun pintu & terali selalu dibuat; AnimatedGate membukanya (terbuka lebar) saat game mulai
        GameObject portGo = null;
        if (!openGate)
        {
            var leafL = Obj(pl, "DoorLeafL", leafMesh[tier], leafSlots[tier], mats);
            var leafR = MirrorLeaf(pr, sp, tier, mats);       // daun kanan = mesh cermin (skala negatif akan membalik arah permukaan)
            float lw = hw - 0.1f;
            Collider(leafL.transform, new Vector3(lw / 2f + 0.05f, Hd / 2f, 0f), new Vector3(lw, Hd, 0.5f));
            Collider(leafR.transform, new Vector3(-(lw / 2f + 0.05f), Hd / 2f, 0f), new Vector3(lw, Hd, 0.5f));

            // terali
            portGo = Obj(root.transform, "Portcullis", portMesh[tier], portSlots[tier], mats);
            portGo.transform.localPosition = new Vector3(0f, 0f, hd - 3.2f);
            Collider(portGo.transform, new Vector3(0f, Hd / 2f + 0.2f, 0f), new Vector3(W - 0.4f, Hd, 0.4f));

        }

        // papan nama + teks
        float signY = (Hd + hw + Hb) / 2f;
        var tr = root.transform;
        BuildKit.Text(tr, signOut, new Vector3(0f, signY, hd + 0.36f), 180f, 0.17f, tier == 2 ? new Color(0.95f, 0.8f, 0.3f) : Color.white);
        BuildKit.Text(tr, signIn, new Vector3(0f, signY, -hd - 0.36f), 0f, 0.17f, tier == 2 ? new Color(0.95f, 0.8f, 0.3f) : Color.white);

        if (withLights)
        {
            AddLight(tr, new Vector3(0f, Hd + 1.5f, hd + 2.0f));
            AddLight(tr, new Vector3(0f, Hd + 1.5f, -hd - 2.0f));
        }

        if (request && !openGate)
        {
            // pose TERBUKA dipanggang ke scene: daun pintu terbuka lebar, terali terangkat (tetap terlihat), collider daun dimatikan
            pl.localRotation = Quaternion.Euler(0f, -100f, 0f);
            pr.localRotation = Quaternion.Euler(0f, 100f, 0f);
            if (portGo != null) portGo.transform.localPosition += new Vector3(0f, Hd + 0.6f, 0f);
            foreach (var t in new[] { pl, pr, portGo != null ? portGo.transform : null })
                if (t != null) foreach (var c in t.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        }
        res.pivotL = pl; res.pivotR = pr; res.portcullis = portGo != null ? portGo.transform : null;

        if (animated)
        {
            var g = root.AddComponent<AnimatedGate>();
            g.gateId = name;
            g.displayName = signOut;
            g.leftPivot = pl; g.rightPivot = pr; g.portcullis = portGo != null ? portGo.transform : null;
            g.leftSign = -1f; g.rightSign = 1f;
            g.openAngle = 100f;
            g.liftHeight = Hd + 0.6f;
            g.triggerRadius = (tier == 0 ? 42f : 28f) + sp.D;
            res.gate = g;
            g.sealedGate = !request;
            g.alwaysOpen = request;      // tersegel (request=false) tetap tertutup; gerbang lain terbuka
            if (AskPermission && request)
            {
                // dua titik "Minta izin": sisi dalam dan sisi luar, dekat penjaga
                for (int side = -1; side <= 1; side += 2)
                {
                    var ip = new GameObject(side < 0 ? "MintaIzin_Dalam" : "MintaIzin_Luar").transform;
                    ip.SetParent(root.transform, false);
                    ip.localPosition = new Vector3(0f, 0f, side * (hd + 4.5f));
                    var rq = ip.gameObject.AddComponent<GateRequest>();
                    rq.gate = g;
                    rq.prompt = "Minta izin pada penjaga";
                    rq.radius = 5.5f;
                }
            }
        }
        else
        {
            pl.localRotation = Quaternion.Euler(0f, -100f, 0f);
            pr.localRotation = Quaternion.Euler(0f, 100f, 0f);
            if (portGo != null) portGo.transform.localPosition += new Vector3(0f, Hd + 0.6f, 0f);
        }
        return res;
    }

    static GameObject MirrorLeaf(Transform pivot, Spec sp, int tier, Material[] mats)
    {
        // daun kanan: geometri dibangun mirror (memanjang ke -X)
        var b = new GeoBag();
        BuildLeafGeometry(b, sp, -1f);
        int[] used;
        var mesh = b.ToMesh("GateLeafR_T" + tier, MeshDir + "/GateLeafR_T" + tier + ".asset", out used);
        return Obj(pivot, "DoorLeafR", mesh, used, mats);
    }

    static void AddLight(Transform parent, Vector3 localPos)
    {
        var go = new GameObject("GateLight");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 22f;
        l.intensity = 1.5f;
        l.color = new Color(1f, 0.78f, 0.5f);
        go.AddComponent<NightLight>();
    }

    // ------------------------------------------------------------------ model statis
    static void BuildModel(GeoBag b, Spec sp)
    {
        int tier = sp.tier;
        float W = sp.W, hw = W / 2f, D = sp.D, hd = D / 2f, Hd = sp.Hdoor, Hb = sp.Hb, tX = sp.tX, tR = sp.towerR;
        int trim = tier == 0 ? Sl.DarkStone : Sl.Marble;

        // ---- jambang batu kiri/kanan + alas
        foreach (float sx in new[] { -1f, 1f })
        {
            float xc = sx * (hw + tX) / 2f, wd = tX - hw;
            FantasyKit.Wall(b, Sl.Stone, new Vector3(xc, 0f, 0f), new Vector3(wd, Hb, D), 1);
            b.BoxB(Sl.DarkStone, new Vector3(xc, 0f, 0f), new Vector3(wd + 0.5f, 1.0f, D + 0.6f));
        }
        // ---- lantai gerbang
        b.Box(Sl.Pave, new Vector3(0f, 0.06f, 0f), new Vector3(W, 0.12f, D + 2f));

        // ---- kepala gerbang dengan lubang lengkung setengah lingkaran
        int N = 18;
        var ax = new float[N + 1];
        var ay = new float[N + 1];
        for (int k = 0; k <= N; k++)
        {
            float th = Mathf.PI - Mathf.PI * k / N;
            ax[k] = hw * Mathf.Cos(th);
            ay[k] = Hd + hw * Mathf.Sin(th);
        }
        for (int k = 0; k < N; k++)
        {
            // muka depan (+Z)
            b.Quad(Sl.Stone, new Vector3(ax[k], ay[k], hd), new Vector3(ax[k + 1], ay[k + 1], hd), new Vector3(ax[k + 1], Hb, hd), new Vector3(ax[k], Hb, hd));
            // muka belakang (-Z)
            b.Quad(Sl.Stone, new Vector3(ax[k + 1], ay[k + 1], -hd), new Vector3(ax[k], ay[k], -hd), new Vector3(ax[k], Hb, -hd), new Vector3(ax[k + 1], Hb, -hd));
            // langit-langit lengkung (menghadap ke bawah)
            b.Quad(Sl.DarkStone, new Vector3(ax[k + 1], ay[k + 1], hd), new Vector3(ax[k], ay[k], hd), new Vector3(ax[k], ay[k], -hd), new Vector3(ax[k + 1], ay[k + 1], -hd));
        }
        b.Quad(Sl.Stone, new Vector3(-hw, Hb, hd), new Vector3(hw, Hb, hd), new Vector3(hw, Hb, -hd), new Vector3(-hw, Hb, -hd));

        // ---- cincin lengkung (voussoir) di muka depan dan belakang
        foreach (float zs in new[] { 1f, -1f })
        {
            float z = zs * (hd + 0.14f);
            float ro = hw + 1.0f;
            for (int k = 0; k < N; k++)
            {
                float th0 = Mathf.PI - Mathf.PI * k / N, th1 = Mathf.PI - Mathf.PI * (k + 1) / N;
                Vector3 pi0 = new Vector3(Mathf.Cos(th0) * (hw + 0.05f), Hd + Mathf.Sin(th0) * (hw + 0.05f), z);
                Vector3 po0 = new Vector3(Mathf.Cos(th0) * ro, Hd + Mathf.Sin(th0) * ro, z);
                Vector3 pi1 = new Vector3(Mathf.Cos(th1) * (hw + 0.05f), Hd + Mathf.Sin(th1) * (hw + 0.05f), z);
                Vector3 po1 = new Vector3(Mathf.Cos(th1) * ro, Hd + Mathf.Sin(th1) * ro, z);
                // dua balok bergantian agar tampak seperti batu-batu lengkung
                b.Beam(k % 2 == 0 ? trim : Sl.Stone, (pi0 + pi1) / 2f, (po0 + po1) / 2f, (pi1 - pi0).magnitude * 0.92f, 0.34f, Vector3.forward);
            }
            b.Box(Sl.Gold, new Vector3(0f, Hd + ro - 0.2f, z + zs * 0.06f), new Vector3(1.0f, 1.3f, 0.5f));
        }

        // ---- tympanum (pengisi setengah lingkaran di atas daun pintu), dua sisi
        float tz = hd - 1.4f;
        Vector3 tc = new Vector3(0f, Hd, tz);
        for (int k = 0; k < N; k++)
        {
            float th0 = Mathf.PI - Mathf.PI * k / N, th1 = Mathf.PI - Mathf.PI * (k + 1) / N;
            Vector3 p0 = tc + new Vector3(Mathf.Cos(th0) * hw, Mathf.Sin(th0) * hw, 0f);
            Vector3 p1 = tc + new Vector3(Mathf.Cos(th1) * hw, Mathf.Sin(th1) * hw, 0f);
            b.Tri(Sl.WoodLight, tc, p1, p0);
            b.Tri(Sl.WoodLight, tc, p0, p1);
        }
        b.Box(Sl.Metal, tc + new Vector3(0f, hw * 0.45f, 0.08f), new Vector3(0.15f, hw * 0.9f, 0.1f));
        b.Box(Sl.Gold, tc + new Vector3(0f, hw * 0.52f, 0.14f), new Vector3(2.2f, 2.6f, 0.12f));
        b.Box(Sl.Cloth, tc + new Vector3(0f, hw * 0.52f, 0.2f), new Vector3(1.6f, 2.0f, 0.1f));
        b.Box(Sl.Gold, tc + new Vector3(0f, hw * 0.52f, 0.27f), new Vector3(0.28f, 1.5f, 0.06f));
        b.Box(Sl.Gold, tc + new Vector3(0f, hw * 0.62f, 0.27f), new Vector3(1.1f, 0.26f, 0.06f));

        // ---- garis hias (string course) & puncak bergerigi
        foreach (float zs in new[] { 1f, -1f })
        {
            b.Box(trim, new Vector3(0f, Hb - 1.2f, zs * (hd + 0.1f)), new Vector3(tX * 2f - 2f, 0.55f, 0.28f));
            b.Box(tier == 2 ? Sl.Gold : Sl.DarkStone, new Vector3(0f, Hb - 1.55f, zs * (hd + 0.2f)), new Vector3(tX * 2f - 2f, 0.16f, 0.14f));
            b.Crenellate(Sl.Stone, new Vector3(-tX + tR * 0.6f, Hb, zs * (hd - 0.35f)), new Vector3(tX - tR * 0.6f, Hb, zs * (hd - 0.35f)), 1.5f, 0.8f, 1.9f, 1.3f);
        }

        // ---- menara bulat
        foreach (float sx in new[] { -1f, 1f })
            BuildKit.RoundTower(b, new Vector3(sx * tX, 0f, 0f), tR, sp.towerH, Sl.Stone, Sl.DarkStone, Sl.Roof, sp.roofH, false, true, Sl.Cloth);

        // ---- ruang jaga di atas gerbang
        float cw = 2f * (tX - tR) - 1.4f, chD = D - 2.6f, ch = sp.chamberH;
        FantasyKit.Wall(b, Sl.Plaster, new Vector3(0f, Hb, 0f), new Vector3(cw, ch, chD), 0);
        foreach (float zs in new[] { 1f, -1f })
        {
            b.Push(new Vector3(0f, Hb, zs * chD / 2f), zs > 0f ? 0f : 180f);
            BuildKit.TimberFront(b, cw, 0f, ch, 0f, true);
            BuildKit.Window(b, new Vector3(-cw * 0.28f, ch * 0.5f, 0.05f), 1.3f, 1.9f, true, false);
            BuildKit.Window(b, new Vector3(cw * 0.28f, ch * 0.5f, 0.05f), 1.3f, 1.9f, true, false);
            BuildKit.Window(b, new Vector3(0f, ch * 0.5f, 0.05f), 1.6f, 2.2f, true, false);
            b.Pop();
        }
        b.Gable(Sl.RoofB, Sl.Plaster, Sl.Wood, new Vector3(0f, Hb + ch, 0f), cw, chD, cw * 0.36f, 0.9f, false);
        BuildKit.Finial(b, new Vector3(0f, Hb + ch + cw * 0.36f, chD / 2f + 0.5f), 1.8f, Sl.Gold);
        BuildKit.Finial(b, new Vector3(0f, Hb + ch + cw * 0.36f, -chD / 2f - 0.5f), 1.8f, Sl.Gold);

        // ---- spanduk + lentera + lampu dinding di sisi lengkung (depan dan belakang)
        foreach (float zs in new[] { 1f, -1f })
        {
            b.Push(new Vector3(0f, 0f, zs * (hd + 0.1f)), zs > 0f ? 0f : 180f);
            float bx = hw + 1.35f;
            BuildKit.Banner(b, new Vector3(-bx, Hd + 4.2f, 0.05f), 1.5f, 6.4f, Sl.Cloth, true);
            BuildKit.Banner(b, new Vector3(bx, Hd + 4.2f, 0.05f), 1.5f, 6.4f, Sl.Cloth, true);
            b.Beam(Sl.Metal, new Vector3(-hw - 0.4f, 5.4f, 0.2f), new Vector3(-hw - 0.4f, 5.4f, 0.9f), 0.1f, 0.1f);
            b.Beam(Sl.Metal, new Vector3(hw + 0.4f, 5.4f, 0.2f), new Vector3(hw + 0.4f, 5.4f, 0.9f), 0.1f, 0.1f);
            BuildKit.Lantern(b, new Vector3(-hw - 0.4f, 5.0f, 0.95f));
            BuildKit.Lantern(b, new Vector3(hw + 0.4f, 5.0f, 0.95f));
            b.Pop();
        }

        // ---- papan nama
        foreach (float zs in new[] { 1f, -1f })
        {
            float signY = (Hd + hw + Hb) / 2f;
            b.Box(Sl.Wood, new Vector3(0f, signY, zs * (hd + 0.2f)), new Vector3(W * 1.5f, 2.0f, 0.3f));
            b.Box(Sl.Gold, new Vector3(0f, signY + 1.05f, zs * (hd + 0.22f)), new Vector3(W * 1.5f + 0.3f, 0.14f, 0.3f));
            b.Box(Sl.Gold, new Vector3(0f, signY - 1.05f, zs * (hd + 0.22f)), new Vector3(W * 1.5f + 0.3f, 0.14f, 0.3f));
        }

        // ---- tier tengah: patung ksatria di kedua sisi pintu masuk (gerbang bangsawan)
        if (tier == 2)
        {
            foreach (float sx in new[] { -1f, 1f })
                PropKit.KnightStatue(b, new Vector3(sx * (hw + 2.6f), 0f, hd + 3.4f), 180f);
        }
    }

    // ------------------------------------------------------------------ daun pintu & terali
    static void BuildLeaf(GeoBag b, Spec sp) { BuildLeafGeometry(b, sp, 1f); }

    /// <summary>dir = +1: daun memanjang ke +X dari engsel (daun kiri); -1: ke -X (daun kanan).</summary>
    static void BuildLeafGeometry(GeoBag b, Spec sp, float dir)
    {
        float hw = sp.W / 2f, Hd = sp.Hdoor;
        float lw = hw - 0.1f;
        float cx = dir * (lw / 2f + 0.05f);
        b.Box(Sl.Wood, new Vector3(cx, Hd / 2f, 0f), new Vector3(lw, Hd, 0.5f));
        foreach (float zs in new[] { 1f, -1f })
        {
            float z = zs * 0.28f;
            foreach (float yy in new[] { 1.1f, Hd * 0.5f, Hd - 1.1f })
                b.Box(Sl.Metal, new Vector3(cx, yy, z), new Vector3(lw, 0.34f, 0.07f));
            // rangka tegak
            b.Box(Sl.Metal, new Vector3(dir * 0.35f, Hd / 2f, z), new Vector3(0.3f, Hd, 0.07f));
            b.Box(Sl.Metal, new Vector3(dir * (lw - 0.1f), Hd / 2f, z), new Vector3(0.3f, Hd, 0.07f));
            // paku besi
            for (int i = 0; i < 5; i++)
            {
                float x = dir * (0.9f + i * (lw - 1.8f) / 4f);
                foreach (float yy in new[] { 1.1f, Hd * 0.5f, Hd - 1.1f })
                    b.Box(Sl.Gold, new Vector3(x, yy, z + zs * 0.05f), new Vector3(0.16f, 0.16f, 0.08f));
            }
            // cincin pegangan
            b.Cyl(Sl.Metal, new Vector3(dir * (lw - 0.9f), Hd * 0.45f, z + zs * 0.05f), 0.28f, 0.08f, 8, 0.28f, true);
        }
    }

    static void BuildPortcullis(GeoBag b, Spec sp)
    {
        float W = sp.W, Hd = sp.Hdoor;
        int n = Mathf.FloorToInt((W - 0.6f) / 0.8f);
        float x0 = -(n - 1) * 0.8f / 2f;
        for (int i = 0; i < n; i++)
        {
            float x = x0 + i * 0.8f;
            b.Box(Sl.Metal, new Vector3(x, Hd / 2f + 0.45f, 0f), new Vector3(0.2f, Hd - 0.5f, 0.2f));
            b.Cyl(Sl.Metal, new Vector3(x, 0f, 0f), 0f, 0.5f, 4, 0.14f, false);
        }
        foreach (float yy in new[] { 1.2f, 3.0f, 4.8f, 6.6f, Hd - 0.3f })
            b.Box(Sl.Metal, new Vector3(0f, yy, 0f), new Vector3((n - 1) * 0.8f + 0.4f, 0.22f, 0.26f));
        b.Box(Sl.Metal, new Vector3(0f, Hd + 0.15f, 0f), new Vector3(W - 0.5f, 0.3f, 0.34f));
    }
}
