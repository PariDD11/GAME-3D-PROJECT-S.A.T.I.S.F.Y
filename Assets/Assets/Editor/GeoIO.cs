using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GeoIO
{
    static GeoIO() { GeoBag.MeshSaver = SaveMesh; }

    /// <summary>Simpan mesh sebagai asset; bila sudah ada, isi lama ditimpa tanpa mengganti GUID (referensi scene tetap valid).</summary>
    public static Mesh SaveMesh(Mesh mesh, string path)
    {
        mesh.name = System.IO.Path.GetFileNameWithoutExtension(path);
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            existing.Clear();
            EditorUtility.CopySerialized(mesh, existing);
            EditorUtility.SetDirty(existing);
            return existing;
        }
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }
}
