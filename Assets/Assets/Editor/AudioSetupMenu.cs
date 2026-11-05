using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Menu Hunter Guild > Audio: menyiapkan folder Resources/Audio, mengekspor suara sementara sebagai WAV (bisa ditimpa file asli), dan memeriksa file yang sudah ada.
/// Daftar nama berasal dari AudioCatalog (Scripts/Core/Audio.cs).
/// </summary>
public static class AudioSetupMenu
{
    const string Dir = "Assets/Resources/Audio";

    [MenuItem("Hunter Guild/Audio/1. Siapkan Folder + Daftar Audio")]
    static void Prepare()
    {
        EnsureDir();
        var sb = new StringBuilder();
        sb.AppendLine("DAFTAR AUDIO HUNTER GUILD");
        sb.AppendLine("Taruh file (wav/ogg/mp3) di folder ini dengan nama PERSIS seperti di bawah (tanpa spasi). Game otomatis memakainya menggantikan suara sementara.");
        sb.AppendLine("Voice boleh punya varian: voice_nama_1, voice_nama_2, voice_nama_3 (dipilih acak). Musik sebaiknya bisa di-loop. Pastikan lisensi file mengizinkan dipakai di game.");
        sb.AppendLine();
        string last = "";
        foreach (var e in AudioCatalog.All)
        {
            if (e.kind != last) { sb.AppendLine(); sb.AppendLine("== " + e.kind.ToUpper() + " =="); last = e.kind; }
            sb.AppendLine(e.File.PadRight(26) + e.desc);
        }
        File.WriteAllText(Dir + "/_DAFTAR_AUDIO.txt", sb.ToString());
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Hunter Guild", "Folder " + Dir + " siap, daftar nama ada di _DAFTAR_AUDIO.txt.", "OK");
    }

    [MenuItem("Hunter Guild/Audio/2. Ekspor Suara Sementara ke WAV (yang belum ada)")]
    static void Export()
    {
        EnsureDir();
        int n = 0;
        foreach (var e in AudioCatalog.All)
        {
            string path = Dir + "/" + e.File + ".wav";
            if (Exists(e.File)) continue;
            WriteWav(path, AudioManager.SamplesFor(e.File), AudioManager.SampleRate);
            n++;
        }
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Hunter Guild", n + " file WAV sementara dibuat di " + Dir + ".\nTimpa satu per satu dengan file asli bernama sama (ganti ekstensi boleh .ogg/.mp3, hapus .wav-nya).", "OK");
    }

    [MenuItem("Hunter Guild/Audio/3. Cek Audio (asli vs sementara)")]
    static void Check()
    {
        int real = 0, synth = 0;
        var sb = new StringBuilder();
        foreach (var e in AudioCatalog.All)
        {
            bool has = Exists(e.File) || (e.kind == "voice" && (Exists(e.File + "_1")));
            if (has) real++; else { synth++; sb.AppendLine("  (sementara) " + e.File + "  -  " + e.desc); }
        }
        Debug.Log("Audio: " + real + " file di Resources/Audio, " + synth + " masih suara sintetis sementara.\n" + sb);
        EditorUtility.DisplayDialog("Hunter Guild", real + " file ada, " + synth + " masih sementara.\nRinciannya ada di Console.", "OK");
    }

    static bool Exists(string name) { return Resources.Load<AudioClip>("Audio/" + name) != null; }

    static void EnsureDir()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/Resources", "Audio");
    }

    static void WriteWav(string path, float[] data, int rate)
    {
        using (var fs = new FileStream(path, FileMode.Create))
        using (var w = new BinaryWriter(fs))
        {
            int bytes = data.Length * 2;
            w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes); w.Write(Encoding.ASCII.GetBytes("WAVE"));
            w.Write(Encoding.ASCII.GetBytes("fmt ")); w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write(Encoding.ASCII.GetBytes("data")); w.Write(bytes);
            foreach (var f in data) w.Write((short)(Mathf.Clamp(f, -1f, 1f) * 32767f));
        }
    }
}
