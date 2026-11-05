using UnityEditor;
using UnityEngine;
using HunterGuild;

/// <summary>Alat bantu menulis cerita dunia: buka WorldStory.json, salin posisi pemain ke clipboard, cek isi data.</summary>
public static class WorldStoryTools
{
    const string Path = "Assets/Resources/WorldStory.json";

    [MenuItem("Hunter Guild/Cerita/Buka WorldStory.json")]
    static void OpenJson()
    {
        var o = AssetDatabase.LoadAssetAtPath<TextAsset>(Path);
        if (o == null) { EditorUtility.DisplayDialog("Cerita", "File " + Path + " tidak ada.", "OK"); return; }
        AssetDatabase.OpenAsset(o);
    }

    [MenuItem("Hunter Guild/Cerita/Salin posisi pemain (saat Play)")]
    static void CopyPos()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (!Application.isPlaying || pc == null) { EditorUtility.DisplayDialog("Cerita", "Jalankan game (Play) dulu, lalu pilih menu ini di dekat lokasi yang diinginkan.", "OK"); return; }
        var p = pc.transform.position;
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string s = "\"scene\": \"" + scene + "\", \"x\": " + p.x.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ", \"y\": " + p.y.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ", \"z\": " + p.z.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        GUIUtility.systemCopyBuffer = s;
        Debug.Log("[Cerita] Disalin ke clipboard: " + s);
    }

    [MenuItem("Hunter Guild/Cerita/Periksa isi WorldStory.json")]
    static void Validate()
    {
        WorldStory.Reload();
        var r = WorldStory.Root; int bad = 0;
        foreach (var q in r.quests)
        {
            if (q == null) continue;
            if (!string.IsNullOrEmpty(q.unlockLore) && WorldStory.Lore(q.unlockLore) == null) { Debug.LogWarning("[Cerita] " + q.id + ": unlockLore '" + q.unlockLore + "' tidak ada."); bad++; }
            if (!string.IsNullOrEmpty(q.prereq) && WorldStory.Quest(q.prereq) == null) { Debug.LogWarning("[Cerita] " + q.id + ": prereq '" + q.prereq + "' tidak ada."); bad++; }
            if (!string.IsNullOrEmpty(q.giver) && WorldStory.FindGiver(q.giver) == null) { Debug.LogWarning("[Cerita] " + q.id + ": giver '" + q.giver + "' belum punya spawn."); bad++; }
            for (int i = 0; i < q.steps.Length; i++)
            {
                var s = q.steps[i]; if (s == null) continue;
                if (s.type == "lore" && WorldStory.Lore(s.target) == null) { Debug.LogWarning("[Cerita] " + q.id + " tahap " + (i + 1) + ": lore '" + s.target + "' tidak ada."); bad++; }
                if (s.type == "talk" && WorldStory.FindGiver(s.target) == null) { Debug.LogWarning("[Cerita] " + q.id + " tahap " + (i + 1) + ": NPC '" + s.target + "' belum punya spawn."); bad++; }
                if (s.type == "reach" && !s.hasPos) { Debug.LogWarning("[Cerita] " + q.id + " tahap " + (i + 1) + ": 'reach' butuh hasPos:true dan x,y,z."); bad++; }
                if (s.type != "talk" && s.type != "kill" && s.type != "gather" && s.type != "lore" && s.type != "reach") { Debug.LogWarning("[Cerita] " + q.id + " tahap " + (i + 1) + ": type '" + s.type + "' tidak dikenal."); bad++; }
            }
        }
        Debug.Log("[Cerita] " + r.lore.Length + " lore, " + r.quests.Length + " quest, " + r.spawns.Length + " penempatan. Masalah: " + bad);
    }
}
