using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Pengaturan impor otomatis untuk klip di Assets/Resources/Audio (hanya saat file BARU masuk; setelah itu bebas kamu ubah sendiri di Inspector):
/// musik dan backsound dimuat terkompresi di memori (hemat RAM, tetap stereo), efek dan suara NPC didekompres saat dimuat dan dijadikan mono (cocok untuk suara 3D).
/// Tidak ada menu yang perlu dipencet. Juga memberi peringatan bila ada dua berkas bernama sama dengan ekstensi berbeda (mis. music_town.wav dan music_town.ogg).
/// </summary>
public class AudioImportSettings : AssetPostprocessor
{
    const string Root = "/Resources/Audio/";

    void OnPreprocessAudio()
    {
        if (!assetPath.Replace('\\', '/').Contains(Root)) return;
        var imp = assetImporter as AudioImporter;
        if (imp == null || !imp.importSettingsMissing) return;
        string n = Path.GetFileNameWithoutExtension(assetPath);
        bool longClip = n.StartsWith("music_") || n.StartsWith("amb_");
        var s = imp.defaultSampleSettings;
        s.loadType = longClip ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
        s.compressionFormat = AudioCompressionFormat.Vorbis;
        s.quality = longClip ? 0.6f : 0.8f;
        imp.defaultSampleSettings = s;
        imp.forceToMono = !longClip;
        imp.loadInBackground = longClip;
    }

    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        foreach (var p in imported)
        {
            string q = p.Replace('\\', '/');
            if (!q.Contains(Root)) continue;
            string ext = Path.GetExtension(q).ToLowerInvariant();
            if (ext != ".wav" && ext != ".ogg" && ext != ".mp3") continue;
            string stem = q.Substring(0, q.Length - ext.Length);
            foreach (var other in new[] { ".wav", ".ogg", ".mp3" })
            {
                if (other == ext) continue;
                if (File.Exists(stem + other))
                    Debug.LogWarning("Audio ganda: " + stem + ext + " dan " + stem + other + ". Hapus salah satu supaya game memakai yang kamu mau.");
            }
        }
    }
}
