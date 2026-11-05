using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Dialog yang otomatis "Ya" saat mode batch (Build SEMUA Scene), supaya tidak perlu klik per scene.</summary>
public static class HGDlg
{
    public static bool Batch;
    public static bool Show(string title, string msg, string ok, string cancel = null)
    {
        if (Batch) return true;
        return cancel == null ? EditorUtility.DisplayDialog(title, msg, ok) : EditorUtility.DisplayDialog(title, msg, ok, cancel);
    }
}

/// <summary>
/// Membangun SEMUA scene sekaligus dalam urutan yang benar (interior, guild, area berburu, menara, kota Eldrath, desa, hutan pemula, kota terakhir).
/// Menu: Hunter Guild/BUILD SEMUA SCENE. Juga menawarkan otomatis saat editor dibuka bila scene belum ada / generator diperbarui.
/// </summary>
[InitializeOnLoad]
public static class HGBuildAll
{
    // naikkan angka ini setiap generator berubah; editor akan menawarkan bangun-ulang sekali per versi
    const int Version = 76;
    const string Pref = "HG_BuildAll_Version";

    static readonly string[] Steps =
    {
        "Hunter Guild/Build Interiors (Penginapan, Pandai Besi, Toko, Akademi, Restoran, Katedral)",
        "Hunter Guild/Build Guild Interior Scene",
        "Hunter Guild/Build Guild Interior (Eldrath - Balai Agung)",
        "Hunter Guild/Build Hunt Area (Hutan Lerai)",
        "Hunter Guild/Build Hunt Area (Rawa Terkutuk)",
        "Hunter Guild/Build Hunt Area (Gurun Zahar)",
        "Hunter Guild/Build Hunt Area (Hutan Perbatasan)",
        "Hunter Guild/Build Hunt Area (Gunung Vulkanik)",
        "Hunter Guild/Build Hunt Area (Padang Tulang)",
        "Hunter Guild/Build Hunt Area (Lembah Iblis)",
        "Hunter Guild/Build Dungeon Tower (Menara Eldrath)",
        "Hunter Guild/Build Arena Ujian (ArenaExam)",
        "Hunter Guild/Build Dungeon City (Eldrath)",
        "Hunter Guild/Build Desa Pemula (Start Village)",
        "Hunter Guild/Build Hutan Pemula (Tutorial Hunt)",
        "Hunter Guild/Build Town Scene",          // terakhir: gerbang/pintu butuh scene lain sudah ada
    };

    static HGBuildAll()
    {
        EditorApplication.delayCall += AutoOffer;
    }

    static void AutoOffer()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (SessionState.GetBool("HG_BuildAll_Asked", false)) return;
        SessionState.SetBool("HG_BuildAll_Asked", true);
        bool missing = !File.Exists("Assets/Scenes/StartVillage.unity") || !File.Exists("Assets/Scenes/Town.unity");
        bool outdated = EditorPrefs.GetInt(Pref, 0) != Version;
        if (!missing && !outdated) return;
        string why = missing ? "Beberapa scene belum dibuat." : "Generator scene diperbarui; scene lama perlu dibangun ulang agar perbaikan terpasang.";
        if (EditorUtility.DisplayDialog("Hunter Guild", why + "\n\nBangun SEMUA scene sekarang? (otomatis, beberapa menit; scene lama ditimpa)", "Bangun semua", "Nanti"))
            EditorApplication.update += RunDeferred;
    }

    // dijalankan dari loop update biasa (bukan dari dialog/delayCall) supaya NewScene tidak gagal "MayUpdate" saat editor masih mengimpor
    static int waitFrames;
    static void RunDeferred()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (++waitFrames < 30) return;
        EditorApplication.update -= RunDeferred;
        waitFrames = 0;
        Run();
    }

    [MenuItem("Hunter Guild/BUILD SEMUA SCENE (otomatis)", priority = 0)]
    public static void Run()
    {
        if (!EnableTerrainPhysics.IsEnabled())
        {
            EnableTerrainPhysics.Enable();
            EditorUtility.DisplayDialog("Hunter Guild", "Modul Terrain Physics belum aktif (tanpa ini terrain tidak punya collider dan pemain jatuh).\n\nSedang diaktifkan otomatis. Tunggu Unity selesai memuat ulang, lalu jalankan BUILD SEMUA SCENE lagi.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        HGDlg.Batch = true;
        int ok = 0;
        try
        {
            for (int i = 0; i < Steps.Length; i++)
            {
                EditorUtility.DisplayProgressBar("Hunter Guild - Build semua scene", Steps[i].Substring(Steps[i].LastIndexOf('/') + 1), i / (float)Steps.Length);
                if (EditorApplication.ExecuteMenuItem(Steps[i])) ok++;
                else Debug.LogWarning("[HunterGuild] Menu tidak ditemukan: " + Steps[i]);
            }
        }
        finally
        {
            HGDlg.Batch = false;
            EditorUtility.ClearProgressBar();
        }
        try { GraphicsSetup.Apply(false); } catch { }
        EditorPrefs.SetInt(Pref, Version);
        try { HuntSceneValidator.ValidateAll(true); } catch (System.Exception e) { Debug.LogWarning("[HunterGuild] Validasi hunt gagal: " + e.Message); }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        if (File.Exists("Assets/Scenes/StartVillage.unity")) EditorSceneManager.OpenScene("Assets/Scenes/StartVillage.unity");
        EditorUtility.DisplayDialog("Hunter Guild", ok + " dari " + Steps.Length + " builder selesai.\nScene dibuka: StartVillage. Cek Console bila ada error.", "OK");
    }
}
