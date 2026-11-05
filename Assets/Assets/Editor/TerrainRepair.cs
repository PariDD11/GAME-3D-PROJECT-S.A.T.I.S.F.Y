using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Memperbaiki tanah yang tampil sebagai kotak-kotak abu-abu (checker bawaan Unity = layer terrain kehilangan tekstur).
/// Menu: Hunter Guild > Perbaiki Tekstur Terrain (scene aktif). Hanya menyentuh terrain yang layernya kosong/tanpa tekstur;
/// tinggi dan cat tanah (alphamap) tidak diubah.
/// </summary>
public static class TerrainRepair
{
    const string Dir = "Assets/Terrain";

    [MenuItem("Hunter Guild/Perbaiki Tekstur Terrain (scene aktif)")]
    public static void Run()
    {
        var terrains = Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
        if (terrains.Length == 0) { EditorUtility.DisplayDialog("Hunter Guild", "Tidak ada Terrain di scene aktif.", "OK"); return; }
        if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets", "Terrain");

        int fixedN = 0, okN = 0, skipN = 0;
        var log = new List<string>();
        foreach (var t in terrains)
        {
            var td = t.terrainData;
            if (td == null) { skipN++; log.Add(t.name + ": tidak ada TerrainData"); continue; }
            var cur = td.terrainLayers;
            bool bad = cur == null || cur.Length == 0;
            if (!bad) foreach (var l in cur) if (l == null || l.diffuseTexture == null) { bad = true; break; }
            if (!bad) { okN++; continue; }

            int need = td.alphamapLayers;
            string[] names; Texture2D[] tex; float[] tile;
            if (!Pick(t.name, need, out names, out tex, out tile))
            {
                skipN++; log.Add(t.name + ": jumlah layer " + need + " tidak dikenal, dilewati");
                continue;
            }
            var layers = new TerrainLayer[names.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                var layer = new TerrainLayer { diffuseTexture = tex[i], tileSize = new Vector2(tile[i], tile[i]), smoothness = 0.05f };
                string path = Dir + "/Fix_" + SafeName(t.name) + "_" + names[i] + ".terrainlayer";
                AssetDatabase.CreateAsset(layer, path);
                layers[i] = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            }
            td.terrainLayers = layers;
            EditorUtility.SetDirty(td);
            fixedN++; log.Add(t.name + ": " + layers.Length + " layer dipulihkan");
        }

        AssetDatabase.SaveAssets();
        if (fixedN > 0)
        {
            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
        }
        string msg = "Diperbaiki: " + fixedN + "   Sudah baik: " + okN + "   Dilewati: " + skipN + (log.Count > 0 ? "\n\n" + string.Join("\n", log) : "");
        Debug.Log("[HunterGuild] Perbaiki Tekstur Terrain\n" + msg);
        EditorUtility.DisplayDialog("Hunter Guild", msg, "OK");
    }

    static string SafeName(string s) { foreach (char c in System.IO.Path.GetInvalidFileNameChars()) s = s.Replace(c, '_'); return s; }

    static bool Pick(string terrainName, int count, out string[] names, out Texture2D[] tex, out float[] tile)
    {
        names = null; tex = null; tile = null;
        if (count != 7) return false;
        if (terrainName.Contains("Desert"))
        {
            ForestKit.LoadDesert();
            names = new[] { "DSand", "DDune", "DRock", "DCrack", "DGravel", "DDry", "DOasis" };
            tex = new[] { ForestKit.dSandT, ForestKit.dDuneT, ForestKit.dRockT, ForestKit.dCrackT, ForestKit.dGravelT, ForestKit.dDryGrassT, ForestKit.dOasisT };
            tile = new[] { 8f, 9f, 10f, 7f, 5f, 6f, 6f };
        }
        else if (terrainName.Contains("Swamp"))
        {
            ForestKit.LoadSwamp();
            names = new[] { "SGrass", "SMoss", "SPeat", "SMud", "SRock", "SPath", "SAsh" };
            tex = new[] { ForestKit.sGrassT, ForestKit.sMossT, ForestKit.sPeatT, ForestKit.sMudT, ForestKit.sRockT, ForestKit.sPathT, ForestKit.sAshT };
            tile = new[] { 7f, 6f, 8f, 6f, 10f, 5f, 7f };
        }
        else
        {
            ForestKit.Load();
            names = new[] { "Grass", "Dirt", "Rock", "Mud", "Litter", "Moss", "Cobble" };
            tex = new[] { ForestKit.tGrass, ForestKit.tDirt, ForestKit.tRock, ForestKit.tMud, ForestKit.tLitter, ForestKit.tMoss, ForestKit.tCobble };
            tile = new[] { 7f, 6f, 9f, 6f, 5f, 6f, 5f };
        }
        foreach (var x in tex) if (x == null) return false;
        return true;
    }
}
