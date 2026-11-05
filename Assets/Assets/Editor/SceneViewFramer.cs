using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Saat scene game dibuka di editor, kamera Scene view langsung diarahkan ke Player.
/// Tanpa ini, kamera Scene view default berada di titik (0,0,0) yang berada DI BAWAH terrain (tanah area hunt ~24 m),
/// sehingga yang terlihat hanya permukaan air/lava dari bawah dan grid editor — tampak "rusak" padahal bukan bug game.
/// </summary>
[InitializeOnLoad]
public static class SceneViewFramer
{
    static SceneViewFramer() { EditorSceneManager.sceneOpened += (sc, mode) => EditorApplication.delayCall += Frame; }

    [MenuItem("Hunter Guild/Arahkan Scene View ke Player")]
    public static void Frame()
    {
        var pl = Object.FindFirstObjectByType<PlayerController>();
        var sv = SceneView.lastActiveSceneView;
        if (pl == null || sv == null) return;
        sv.LookAt(pl.transform.position + Vector3.up * 2f, Quaternion.Euler(22f, pl.transform.eulerAngles.y, 0f), 28f);
    }
}
