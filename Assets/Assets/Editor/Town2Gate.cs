using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Perakit gerbang Ashwatch baru (sisi Unity): memasang model Town2Gate.Model.cs + collider + daun pintu berpivot + terali + AnimatedGate.
/// Antarmuka sama dengan GateKit.Build (hasil bertipe GateKit.Result) sehingga CityGate / LevelGate / scene hutan tinggal memakainya.
/// </summary>
public static partial class T2Gate
{
    const string MeshDir = "Assets/Meshes/Town3";
    static readonly Mesh[] modelMesh = new Mesh[3];
    static readonly int[][] modelSlots = new int[3][];
    static readonly Mesh[] leafMeshL = new Mesh[3], leafMeshR = new Mesh[3];
    static readonly int[][] leafSlotsL = new int[3][], leafSlotsR = new int[3][];
    static readonly Mesh[] portMesh = new Mesh[3];
    static readonly int[][] portSlots = new int[3][];

    public static void Reset()
    {
        for (int i = 0; i < 3; i++) { modelMesh[i] = null; leafMeshL[i] = null; leafMeshR[i] = null; portMesh[i] = null; }
    }

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

    static void BoxCol(Transform parent, Vector3 center, Vector3 size)
    {
        var go = new GameObject("Col");
        go.transform.SetParent(parent, false);
        var bc = go.AddComponent<BoxCollider>();
        bc.center = center; bc.size = size;
    }

    static void CapsuleCol(Transform parent, Vector3 center, float r, float h)
    {
        var go = new GameObject("ColTower");
        go.transform.SetParent(parent, false);
        var cc = go.AddComponent<CapsuleCollider>();
        cc.center = center; cc.radius = r; cc.height = h;
    }

    static void AddLight(Transform parent, Vector3 localPos)
    {
        var go = new GameObject("GateLight");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point; l.range = 22f; l.intensity = 1.5f; l.color = new Color(1f, 0.78f, 0.5f);
        go.AddComponent<NightLight>();
    }

    /// <summary>Bahan per tingkat (n81): batu kelabu makin gelap ke pusat, atap genteng merah di tembok tengah, atap emas matte di Tembok Dalam, rongga mata patung hitam.</summary>
    public static Material[] SlotsFor(int tier)
    {
        var a = GateKit.SlotsFor(tier);
        a[Sl.Dark] = TownMats.Hollow;
        switch (tier)
        {
            case 0:
                a[Sl.Stone] = TownMats.Stone; a[Sl.DarkStone] = TownMats.DarkStone;
                break;
            case 1:
                a[Sl.Stone] = TownMats.StoneMid; a[Sl.DarkStone] = TownMats.DarkStone;
                a[Sl.Roof] = TownMats.Roofs[0]; a[Sl.RoofB] = TownMats.Roofs[0];
                break;
            default:
                a[Sl.Stone] = TownMats.DarkStone; a[Sl.DarkStone] = TownMats.StoneDeep;
                a[Sl.Roof] = TownMats.Roofs[5]; a[Sl.RoofB] = TownMats.RoofGold;
                break;
        }
        return a;
    }

    /// <param name="animated">true = pasang AnimatedGate (buka-tutup otomatis); false = pose terbuka statis (replika di scene hutan).</param>
    public static GateKit.Result Build(Transform parent, string name, int tier, string signOut, string signIn, Vector3 pos, float yaw, bool animated, bool withLights, bool request = true)
    {
        EnsureDir();
        var sp = SpecFor(tier);
        Material[] mats = SlotsFor(tier);
        float W = sp.W, hw = W * 0.5f, D = sp.D, hd = D * 0.5f, Hr = sp.Hr, Hd = sp.Hd, Hb = sp.Hb, tX = sp.tX, tR = sp.towerR;
        float pierOut = tX + tR * 0.55f;

        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos;
        root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        var res = new GateKit.Result { root = root };

        if (modelMesh[tier] == null)
        {
            var b = new GeoBag();
            Model(b, tier);
            modelMesh[tier] = b.ToMesh("T2Gate_T" + tier, MeshDir + "/T2Gate_T" + tier + ".asset", out modelSlots[tier]);
            var lb = new GeoBag(); Leaf(lb, tier, 1f);
            leafMeshL[tier] = lb.ToMesh("T2GateLeafL_T" + tier, MeshDir + "/T2GateLeafL_T" + tier + ".asset", out leafSlotsL[tier]);
            var rb = new GeoBag(); Leaf(rb, tier, -1f);
            leafMeshR[tier] = rb.ToMesh("T2GateLeafR_T" + tier, MeshDir + "/T2GateLeafR_T" + tier + ".asset", out leafSlotsR[tier]);
            var pb = new GeoBag(); Portcullis(pb, tier);
            portMesh[tier] = pb.ToMesh("T2GatePort_T" + tier, MeshDir + "/T2GatePort_T" + tier + ".asset", out portSlots[tier]);
        }
        Obj(root.transform, "Model", modelMesh[tier], modelSlots[tier], mats);

        var cols = new GameObject("Colliders").transform;
        cols.SetParent(root.transform, false);
        BoxCol(cols, new Vector3(-(hw + pierOut) * 0.5f, Hb * 0.5f, 0f), new Vector3(pierOut - hw, Hb, D));
        BoxCol(cols, new Vector3((hw + pierOut) * 0.5f, Hb * 0.5f, 0f), new Vector3(pierOut - hw, Hb, D));
        BoxCol(cols, new Vector3(0f, (Hd + Hb) * 0.5f, 0f), new Vector3(W, Hb - Hd, D));
        CapsuleCol(cols, new Vector3(-tX, sp.towerH * 0.5f, 0f), tR + 0.5f, sp.towerH);
        CapsuleCol(cols, new Vector3(tX, sp.towerH * 0.5f, 0f), tR + 0.5f, sp.towerH);

        float doorZ = hd - 1.0f;
        var pl = new GameObject("DoorPivotL").transform; pl.SetParent(root.transform, false); pl.localPosition = new Vector3(-hw, 0f, doorZ);
        var pr = new GameObject("DoorPivotR").transform; pr.SetParent(root.transform, false); pr.localPosition = new Vector3(hw, 0f, doorZ);
        var leafL = Obj(pl, "DoorLeafL", leafMeshL[tier], leafSlotsL[tier], mats);
        var leafR = Obj(pr, "DoorLeafR", leafMeshR[tier], leafSlotsR[tier], mats);
        float lw = hw - 0.1f;
        BoxCol(leafL.transform, new Vector3(lw * 0.5f + 0.05f, Hr * 0.5f, 0f), new Vector3(lw, Hr, 0.5f));
        BoxCol(leafR.transform, new Vector3(-(lw * 0.5f + 0.05f), Hr * 0.5f, 0f), new Vector3(lw, Hr, 0.5f));
        var portGo = Obj(root.transform, "Portcullis", portMesh[tier], portSlots[tier], mats);
        portGo.transform.localPosition = new Vector3(0f, 0f, hd - 3.2f);
        BoxCol(portGo.transform, new Vector3(0f, Hr * 0.5f + 0.2f, 0f), new Vector3(W - 0.4f, Hr, 0.4f));

        // papan nama (teks)
        float signY = Hd + 2.5f;
        var tr = root.transform;
        Color tc = tier == 2 ? new Color(0.95f, 0.8f, 0.3f) : Color.white;
        BuildKit.Text(tr, signOut, new Vector3(0f, signY, hd + 0.56f), 180f, 0.17f, tc);
        BuildKit.Text(tr, signIn, new Vector3(0f, signY, -hd - 0.56f), 0f, 0.17f, tc);
        if (withLights)
        {
            AddLight(tr, new Vector3(0f, Hd + 1.5f, hd + 2.0f));
            AddLight(tr, new Vector3(0f, Hd + 1.5f, -hd - 2.0f));
        }

        if (request)
        {
            // pose TERBUKA dipanggang ke scene: daun pintu terbuka lebar, terali terangkat, collider daun dimatikan
            pl.localRotation = Quaternion.Euler(0f, -100f, 0f);
            pr.localRotation = Quaternion.Euler(0f, 100f, 0f);
            portGo.transform.localPosition += new Vector3(0f, Hd + 0.6f, 0f);
            foreach (var t in new[] { pl, pr, portGo.transform })
                foreach (var c in t.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        }
        res.pivotL = pl; res.pivotR = pr; res.portcullis = portGo.transform;

        if (animated)
        {
            var g = root.AddComponent<AnimatedGate>();
            g.gateId = name;
            g.displayName = signOut;
            g.leftPivot = pl; g.rightPivot = pr; g.portcullis = portGo.transform;
            g.leftSign = -1f; g.rightSign = 1f;
            g.openAngle = 100f;
            g.liftHeight = Hd + 0.6f;
            g.triggerRadius = (tier == 0 ? 42f : 28f) + sp.D;
            res.gate = g;
            g.sealedGate = !request;
            g.alwaysOpen = request;
            if (GateKit.AskPermission && request)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    var ip = new GameObject(side < 0 ? "MintaIzin_Dalam" : "MintaIzin_Luar").transform;
                    ip.SetParent(root.transform, false);
                    ip.localPosition = new Vector3(0f, 0f, side * (hd + 4.5f));
                    var rq = ip.gameObject.AddComponent<GateRequest>();
                    rq.gate = g; rq.prompt = "Minta izin pada penjaga"; rq.radius = 5.5f;
                }
            }
        }
        else
        {
            pl.localRotation = Quaternion.Euler(0f, -100f, 0f);
            pr.localRotation = Quaternion.Euler(0f, 100f, 0f);
            portGo.transform.localPosition += new Vector3(0f, Hd + 0.6f, 0f);
        }
        return res;
    }
}
