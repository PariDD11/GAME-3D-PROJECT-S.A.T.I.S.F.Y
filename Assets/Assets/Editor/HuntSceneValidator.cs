using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using HunterGuild;

/// <summary>
/// Pembersih dan pemeriksa scene hunt.
/// - SceneSanitizer: otomatis membuang komponen "script hilang" (The referenced script (Unknown) ... is missing!) setiap scene di Assets/Scenes disimpan.
/// - Menu "Hunter Guild/Validasi dan Perbaiki Semua Area Hunt": membuka tiap HuntArea_*.unity, membersihkan, memeriksa isi scene
///   (terrain, pemain, manager, monster, base camp, pintu area), menurunkan/menaikkan pemain, monster dan bahan yang terkubur di bawah tanah atau melayang,
///   lalu menyimpan dan menampilkan laporan.
/// </summary>
public class SceneSanitizer : AssetModificationProcessor
{
    static string[] OnWillSaveAssets(string[] paths)
    {
        foreach (var p in paths)
        {
            if (!p.EndsWith(".unity") || !p.StartsWith("Assets/Scenes")) continue;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var sc = SceneManager.GetSceneAt(i);
                if (sc.path == p && sc.isLoaded) HuntSceneValidator.StripMissing(sc);
            }
        }
        return paths;
    }
}

public static class HuntSceneValidator
{
    public static int StripMissing(Scene sc)
    {
        int n = 0;
        foreach (var root in sc.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                n += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        return n;
    }

    [MenuItem("Hunter Guild/Validasi dan Perbaiki Semua Area Hunt")]
    static void ValidateMenu() { ValidateAll(false); }

    public static void ValidateAll(bool silent)
    {
        if (!silent && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var guids = AssetDatabase.FindAssets("HuntArea_ t:Scene", new[] { "Assets/Scenes" });
        var sb = new StringBuilder();
        int problems = 0, fixedN = 0, scenes = 0;
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            if (!System.IO.Path.GetFileName(path).StartsWith("HuntArea_")) continue;
            var sc = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            scenes++;
            var log = new List<string>();
            bool dirty = false;

            int miss = StripMissing(sc);
            if (miss > 0) { log.Add("dibersihkan " + miss + " komponen script-hilang"); dirty = true; fixedN += miss; }

            var terrain = Terrain.activeTerrain;
            var player = Object.FindFirstObjectByType<PlayerController>();
            var monsters = Object.FindObjectsByType<Monster>(FindObjectsSortMode.None);
            var trans = Object.FindObjectsByType<AreaTransition>(FindObjectsSortMode.None);
            var camps = Object.FindObjectsByType<BaseCamp>(FindObjectsSortMode.None);
            var nodes = Object.FindObjectsByType<GatherNode>(FindObjectsSortMode.None);

            if (Object.FindFirstObjectByType<HuntManager>() == null) { log.Add("MASALAH: HuntManager tidak ada"); problems++; }
            if (Object.FindFirstObjectByType<GameState>() == null) { log.Add("MASALAH: GameState tidak ada"); problems++; }
            if (Object.FindFirstObjectByType<TownUI>() == null) { log.Add("MASALAH: TownUI tidak ada"); problems++; }
            if (player == null) { log.Add("MASALAH: Player tidak ada"); problems++; }
            else if (!player.CompareTag("Player")) { player.gameObject.tag = "Player"; log.Add("tag Player dipasang"); dirty = true; fixedN++; }
            if (camps.Length == 0) { log.Add("peringatan: tidak ada BaseCamp"); }
            if (monsters.Length == 0) { log.Add("MASALAH: tidak ada monster"); problems++; }
            if (trans.Length == 0) { log.Add("peringatan: tidak ada pintu keluar (AreaTransition)"); }

            if (terrain == null) log.Add("catatan: tidak ada Terrain aktif (scene ini memakai ground lain, snap dilewati)");
            else
            {
                fixedN += Snap(player != null ? new[] { player.transform } : new Transform[0], terrain, log, "pemain", ref dirty, 1.0f);
                var mt = new List<Transform>(); foreach (var m in monsters) if (m != null) mt.Add(m.transform);
                fixedN += Snap(mt.ToArray(), terrain, log, "monster", ref dirty, 0f);
                var nt = new List<Transform>(); foreach (var n in nodes) if (n != null) nt.Add(n.transform);
                fixedN += Snap(nt.ToArray(), terrain, log, "bahan gather", ref dirty, 0f);
            }

            // diagnosa isi scene: jumlah objek tiap grup, tinggi terrain, posisi pemain
            int rends = 0; foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)) if (r.enabled) rends++;
            log.Add("INFO: renderer aktif " + rends + ", lampu " + Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Length);
            if (terrain != null)
            {
                float mn = 1e9f, mx = -1e9f;
                for (float x = -480f; x <= 480f; x += 40f) for (float z = -480f; z <= 480f; z += 40f) { float h = terrain.SampleHeight(new Vector3(x, 0, z)) + terrain.transform.position.y; if (h < mn) mn = h; if (h > mx) mx = h; }
                log.Add("INFO: tinggi terrain " + mn.ToString("0.0") + " .. " + mx.ToString("0.0") + " m, posisi terrain " + terrain.transform.position + ", ukuran " + terrain.terrainData.size);
            }
            if (player != null) log.Add("INFO: Player di " + player.transform.position);
            foreach (var root in sc.GetRootGameObjects())
                foreach (Transform ch in root.transform)
                    if (ch.childCount > 0) log.Add("INFO: " + root.name + "/" + ch.name + " berisi " + ch.childCount + " anak");
            if (dirty) EditorSceneManager.SaveScene(sc);
            string line = sc.name + ": " + monsters.Length + " monster, " + nodes.Length + " bahan, " + trans.Length + " pintu | " + string.Join(" | ", log.ToArray());
            sb.AppendLine(line);
            Debug.Log("[Validasi] " + line);
        }
        string report = sb.ToString();
        Debug.Log("[Validasi Hunt]\n" + report);
        if (!silent) EditorUtility.DisplayDialog("Validasi Area Hunt", scenes + " scene diperiksa.\nMasalah: " + problems + "   Perbaikan otomatis: " + fixedN + "\n\nRincian ada di Console.", "OK");
    }

    // Turunkan/naikkan objek yang terkubur lebih dari 1.5 m di bawah terrain atau melayang lebih dari 6 m di atasnya.
    static int Snap(Transform[] ts, Terrain terrain, List<string> log, string what, ref bool dirty, float lift)
    {
        int c = 0;
        foreach (var t in ts)
        {
            Vector3 p = t.position;
            float gy = terrain.SampleHeight(p) + terrain.transform.position.y;
            if (p.y < gy - 1.5f || p.y > gy + 6f)
            {
                var cc = t.GetComponent<CharacterController>();
                bool had = cc != null && cc.enabled; if (had) cc.enabled = false;
                t.position = new Vector3(p.x, gy + lift, p.z);
                if (had) cc.enabled = true;
                c++;
            }
        }
        if (c > 0) { log.Add(what + ": " + c + " dipindah ke permukaan tanah"); dirty = true; }
        return c;
    }
}
