using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

/// <summary>
/// Terrain di scene butuh modul bawaan "Terrain Physics" (TerrainCollider). Bila project mematikannya, collider terrain terhapus
/// dan pemain jatuh menembus tanah. Skrip ini mengaktifkannya otomatis saat Editor dibuka (sekali), atau lewat menu Hunter Guild.
/// </summary>
[InitializeOnLoad]
public static class EnableTerrainPhysics
{
    const string Pkg = "com.unity.modules.terrainphysics";

    static EnableTerrainPhysics()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (!IsEnabled() && !SessionState.GetBool("HG_TP_Asked", false))
            {
                SessionState.SetBool("HG_TP_Asked", true);
                Enable();
            }
        };
    }

    public static bool IsEnabled()
    {
        try
        {
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Packages/manifest.json");
            return File.Exists(path) && File.ReadAllText(path).Contains(Pkg);
        }
        catch { return true; }
    }

    [MenuItem("Hunter Guild/Aktifkan Terrain Physics")]
    public static void Enable()
    {
        if (IsEnabled()) { Debug.Log("[HunterGuild] Terrain Physics sudah aktif."); return; }
        Debug.Log("[HunterGuild] Mengaktifkan modul Terrain Physics (collider terrain)...");
        Client.Add(Pkg);
    }
}
