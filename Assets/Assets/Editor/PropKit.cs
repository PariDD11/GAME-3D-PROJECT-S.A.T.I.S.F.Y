using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Properti kecil: pohon (arketipe), lampu jalan, air mancur, patung, lapak pasar, sumur, pagar, jerami, hewan ternak.</summary>
public static class PropKit
{
    const string MeshDir = "Assets/Meshes/Town3";

    public class Proto
    {
        public Mesh mesh;
        public int[] used;
    }

    static readonly Dictionary<string, Proto> cache = new Dictionary<string, Proto>();

    public static void Reset() { cache.Clear(); treeVar = 0; }

    /// <summary>Ambil (atau buat sekali) mesh arketipe bernama; build mengisi GeoBag di sekitar origin.</summary>
    public static Proto Get(string key, System.Action<GeoBag> build)
    {
        Proto p;
        if (cache.TryGetValue(key, out p) && p.mesh != null) return p;
        if (!AssetDatabase.IsValidFolder("Assets/Meshes")) AssetDatabase.CreateFolder("Assets", "Meshes");
        if (!AssetDatabase.IsValidFolder(MeshDir)) AssetDatabase.CreateFolder("Assets/Meshes", "Town3");
        var b = new GeoBag();
        build(b);
        p = new Proto();
        p.mesh = b.ToMesh(key, MeshDir + "/" + key + ".asset", out p.used);
        cache[key] = p;
        return p;
    }

    /// <summary>Pasang instance arketipe ke scene dengan material dari slotMats.</summary>
    public static GameObject Place(Proto p, Transform parent, string name, Vector3 pos, float yaw, float scale, Material[] slotMats)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        go.transform.localScale = new Vector3(scale, scale, scale);
        go.AddComponent<MeshFilter>().sharedMesh = p.mesh;
        var mats = new Material[p.used.Length];
        for (int i = 0; i < mats.Length; i++) mats[i] = slotMats[p.used[i]];
        go.AddComponent<MeshRenderer>().sharedMaterials = mats;
        return go;
    }

    // ------------------------------------------------------------------ pohon
    // kind: 0 berdaun lebar, 1 cemara, 2 sakura (merah muda), 3 sipres tinggi (taman bangsawan), 4 semak, 5 pohon apel
    static int treeVar;
    public static Proto TreeProto(int kind)
    {
        int variant = (treeVar++) % 4;                                   // 4 bentuk berbeda per jenis pohon (tidak ada pohon kembar)
        return Get("Tree" + kind + "v5_" + variant, b => TreeKit.Build(b, kind, variant));
    }

    // ------------------------------------------------------------------ lampu jalan
    public static Proto LampProto()
    {
        return Get("LampPost2", StreetArt.Lamp);
    }

    // ------------------------------------------------------------------ patung ksatria
    public static void KnightStatue(GeoBag b, Vector3 baseC, float yaw)
    {
        PropArt.KnightStatue(b, baseC, yaw);
    }

    // ------------------------------------------------------------------ air mancur bertingkat dengan patung dewi
    public static void Fountain(GeoBag b, Vector3 c, float scale)
    {
        PropArt.Fountain(b, c, scale);
    }

    // ------------------------------------------------------------------ sumur
    public static void Well(GeoBag b, Vector3 c)
    {
        PropArt.Well(b, c);
    }

    // ------------------------------------------------------------------ lapak pasar
    public static void Stall(GeoBag b, Vector3 c, float yaw, int cloth, int clothB)
    {
        PropArt.Stall(b, c, yaw, cloth, clothB);
    }

    // ------------------------------------------------------------------ pagar kayu mengikuti garis
    public static void Fence(GeoBag b, IList<Vector3> pts, float h)
    {
        PropArt.Fence(b, pts, h);
    }

    // ------------------------------------------------------------------ jerami & ternak
    public static void Haystack(GeoBag b, Vector3 c)
    { StreetArt.Haystack(b, c); }

    public static void Sheep(GeoBag b)
    { StreetArt.Sheep(b); }

    public static void Cow(GeoBag b)
    { StreetArt.Cow(b); }

    public static void Scarecrow(GeoBag b)
    { StreetArt.Scarecrow(b); }

    // ------------------------------------------------------------------ bangku taman & parasol
    public static void Bench(GeoBag b)
    { StreetArt.Bench(b); }

    // ------------------------------------------------------------------ tiang bendera tegak (alun-alun)
    public static void FlagPole(GeoBag b, int cloth)
    {
        b.Cyl(Sl.DarkStone, Vector3.zero, 0.5f, 0.6f, 8, 0.4f, true);
        BuildKit.Flag(b, new Vector3(0f, 0.6f, 0f), 9f, 3.2f, 1.9f, cloth);
    }
}
