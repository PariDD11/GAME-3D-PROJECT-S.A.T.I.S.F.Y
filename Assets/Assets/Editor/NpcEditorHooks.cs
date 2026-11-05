using UnityEditor;
using UnityEngine;

/// <summary>Menghubungkan NpcKit (kode runtime) dengan TownMats/AssetDatabase saat membangun scene di Editor.</summary>
[InitializeOnLoad]
public static class NpcEditorHooks
{
    static NpcEditorHooks()
    {
        NpcKit.EditorMat = (n, c, sm, me) => TownMats.Mk("N_" + n, c, null, 1f, sm, me);
        NpcKit.EditorBaseSlots = () => TownMats.Slots();
        NpcKit.EditorEnsureDirs = () =>
        {
            if (!AssetDatabase.IsValidFolder("Assets/Meshes")) AssetDatabase.CreateFolder("Assets", "Meshes");
            if (!AssetDatabase.IsValidFolder(NpcKit.MeshDir)) AssetDatabase.CreateFolder("Assets/Meshes", "Npc");
        };
    }
}
