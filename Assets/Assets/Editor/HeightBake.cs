using System.IO;
using UnityEditor;
using UnityEngine;
using HunterGuild;

/// <summary>Menyimpan tinggi terrain ke file .heights.bytes dan memasang TerrainHeightRestore sebagai cadangan.</summary>
public static class HeightBake
{
    /// <summary>Buat asset BARU dengan nama unik (tidak menimpa file lama).</summary>
    public static string NewAsset(Object o, string path)
    {
        string u = AssetDatabase.GenerateUniqueAssetPath(path);
        AssetDatabase.CreateAsset(o, u);
        return u;
    }

    /// <summary>Salin scene lama ke Assets/_Lama sebelum disimpan ulang (cadangan, tidak menghapus apa pun).</summary>
    public static void BackupScene(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath) || !File.Exists(scenePath)) return;
        if (!AssetDatabase.IsValidFolder("Assets/_Lama")) AssetDatabase.CreateFolder("Assets", "_Lama");
        string dst = AssetDatabase.GenerateUniqueAssetPath("Assets/_Lama/" + Path.GetFileNameWithoutExtension(scenePath) + "_lama.unity");
        AssetDatabase.CopyAsset(scenePath, dst);
    }

    public static void Attach(GameObject terrainGO, TerrainData td, float[,] hs, string name)
    {
        if (terrainGO == null || td == null || hs == null) return;
        int n = hs.GetLength(0);
        var bytes = new byte[n * n * 2];
        for (int z = 0; z < n; z++)
            for (int x = 0; x < n; x++)
            {
                int v = Mathf.Clamp(Mathf.RoundToInt(hs[z, x] * 65535f), 0, 65535);
                bytes[2 * (z * n + x)] = (byte)(v & 255);
                bytes[2 * (z * n + x) + 1] = (byte)(v >> 8);
            }
        string dir = "Assets/Terrain";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets", "Terrain");
        string path = AssetDatabase.GenerateUniqueAssetPath(dir + "/" + name + ".heights.bytes");
        File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        var c = terrainGO.GetComponent<TerrainHeightRestore>();
        if (c == null) c = terrainGO.AddComponent<TerrainHeightRestore>();
        c.heights = ta; c.res = n; c.size = td.size;
        EditorUtility.SetDirty(c);
    }
}
