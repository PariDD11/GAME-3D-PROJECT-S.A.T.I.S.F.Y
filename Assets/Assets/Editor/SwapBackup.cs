using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Pengaturan Ganti Aset 3D / Ganti NPC disimpan permanen: selain di Assets/Resources/*.asset, otomatis disalin ke
/// HG_UserData/*.json (di luar folder Assets, berisi path prefab). Kalau .asset hilang (mis. folder Assets ditimpa zip baru),
/// pengaturan dipulihkan otomatis saat Unity dibuka. Tidak ada yang perlu diatur ulang tiap Play.
/// </summary>
[InitializeOnLoad]
public static class SwapBackup
{
    const string AssetTablePath = "Assets/Resources/AssetSwapTable.asset";
    const string NpcTablePath = "Assets/Resources/NpcSwapTable.asset";
    const string MonsterTablePath = "Assets/Resources/MonsterSwapTable.asset";

    static SwapBackup() { EditorApplication.delayCall += RestoreAll; }

    static string Dir { get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "HG_UserData"); } }
    static string P(UnityEngine.Object o) { return o == null ? "" : AssetDatabase.GetAssetPath(o); }
    static T L<T>(string path) where T : UnityEngine.Object { return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path); }
    static List<string> Ps<T>(List<T> l) where T : UnityEngine.Object { var r = new List<string>(); foreach (var o in l) r.Add(P(o)); return r; }
    static List<T> Ls<T>(List<string> l) where T : UnityEngine.Object { var r = new List<T>(); if (l != null) foreach (var p in l) { var o = L<T>(p); if (o != null) r.Add(o); } return r; }

    // ---------------------------------------------------------------- DTO
    [Serializable] class AssetFile { public List<AE> e = new List<AE>(); }
    [Serializable] class AE { public string scene, category, key, prefab; public bool enabled, fitHeight, keep; public float scale, yOffset, rotY; public int count; }

    [Serializable] class NpcFile { public List<NR> r = new List<NR>(); public List<NS> s = new List<NS>(); }
    [Serializable] class NR
    {
        public string label, npcName, scene, portraitTex, portrait, controller, idle, walk, wave, talk, work;
        public bool enabled, workWhenIdle, pFlip; public float mMul, aSpd, sScale; public float pScale, pX, pY; public int pSide; public int gender, race, role, importance; public float heightScale, yOffset;
        public List<string> prefabs, portraits, portraitTexes;
    }
    [Serializable] class NS { public string scene, npcName; public int race, role; public bool female, important; }

    [Serializable] class MonsterFile { public List<MR> r = new List<MR>(); }
    [Serializable] class MR
    {
        public string label, monsterId, shape, scene, controller, idle, walk, attack, hurt, die;
        public bool enabled, life; public int tier; public float heightScale, yOffset, yaw, aSpd, sScale;
        public List<string> prefabs;
    }

    // ---------------------------------------------------------------- simpan
    public static void Backup(AssetSwapTable t)
    {
        if (t == null) return;
        var f = new AssetFile();
        foreach (var e in t.entries)
            f.e.Add(new AE { scene = e.scene, category = e.category, key = e.key, prefab = P(e.prefab), enabled = e.enabled, fitHeight = e.fitHeight, keep = e.keepPrefabColliders, scale = e.scale, yOffset = e.yOffset, rotY = e.rotY, count = e.count });
        Write("AssetSwapTable.json", JsonUtility.ToJson(f, true));
    }

    public static void Backup(NpcSwapTable t)
    {
        if (t == null) return;
        var f = new NpcFile();
        foreach (var r in t.rules)
            f.r.Add(new NR
            {
                label = r.label, npcName = r.npcName, scene = r.scene, enabled = r.enabled, workWhenIdle = r.workWhenIdle, gender = r.gender, race = r.race, role = r.role,
                importance = r.importance, heightScale = r.heightScale, yOffset = r.yOffset, mMul = r.moveSpeedMul, aSpd = r.animSpeed, sScale = r.speedParamScale, pScale = r.portraitScale, pX = r.portraitX, pY = r.portraitY, pFlip = r.portraitFlip, pSide = r.portraitSide, portraitTex = P(r.portraitTex), portrait = P(r.portrait), controller = P(r.controller),
                idle = P(r.clipIdle), walk = P(r.clipWalk), wave = P(r.clipWave), talk = P(r.clipTalk), work = P(r.clipWork),
                prefabs = Ps(r.prefabs), portraits = Ps(r.portraits), portraitTexes = Ps(r.portraitTexes)
            });
        foreach (var s in t.seen) f.s.Add(new NS { scene = s.scene, npcName = s.npcName, race = s.race, role = s.role, female = s.female, important = s.important });
        Write("NpcSwapTable.json", JsonUtility.ToJson(f, true));
    }

    public static void Backup(MonsterSwapTable t)
    {
        if (t == null) return;
        var f = new MonsterFile();
        foreach (var r in t.rules)
            f.r.Add(new MR
            {
                label = r.label, monsterId = r.monsterId, shape = r.shape, scene = r.scene, enabled = r.enabled, life = r.lifeMotion, tier = r.tier,
                heightScale = r.heightScale, yOffset = r.yOffset, yaw = r.yaw, aSpd = r.animSpeed, sScale = r.speedParamScale, controller = P(r.controller),
                idle = P(r.clipIdle), walk = P(r.clipWalk), attack = P(r.clipAttack), hurt = P(r.clipHurt), die = P(r.clipDie), prefabs = Ps(r.prefabs)
            });
        Write("MonsterSwapTable.json", JsonUtility.ToJson(f, true));
    }

    static void Write(string name, string json)
    {
        try { Directory.CreateDirectory(Dir); File.WriteAllText(Path.Combine(Dir, name), json); }
        catch (Exception ex) { Debug.LogWarning("[SwapBackup] gagal menyimpan cadangan: " + ex.Message); }
    }

    // ---------------------------------------------------------------- pulihkan
    static void RestoreAll()
    {
        try { RestoreAssetTable(); RestoreNpcTable(); RestoreMonsterTable(); }
        catch (Exception ex) { Debug.LogWarning("[SwapBackup] gagal memulihkan: " + ex.Message); }
    }

    static void RestoreMonsterTable()
    {
        string json = Read("MonsterSwapTable.json"); if (json == null) return;
        var t = AssetDatabase.LoadAssetAtPath<MonsterSwapTable>(MonsterTablePath);
        if (t != null && t.rules.Count > 0) return;
        var f = JsonUtility.FromJson<MonsterFile>(json); if (f == null || f.r.Count == 0) return;
        bool created = t == null;
        if (created) { Directory.CreateDirectory("Assets/Resources"); t = ScriptableObject.CreateInstance<MonsterSwapTable>(); }
        t.rules.Clear();
        foreach (var r in f.r)
            t.rules.Add(new MonsterSwapTable.Rule
            {
                label = r.label, monsterId = r.monsterId, shape = r.shape, scene = r.scene, enabled = r.enabled, lifeMotion = r.life, tier = r.tier,
                heightScale = r.heightScale <= 0f ? 1f : r.heightScale, yOffset = r.yOffset, yaw = r.yaw, animSpeed = r.aSpd <= 0f ? 1f : r.aSpd, speedParamScale = r.sScale <= 0f ? 1f : r.sScale,
                controller = L<RuntimeAnimatorController>(r.controller),
                clipIdle = L<AnimationClip>(r.idle), clipWalk = L<AnimationClip>(r.walk), clipAttack = L<AnimationClip>(r.attack), clipHurt = L<AnimationClip>(r.hurt), clipDie = L<AnimationClip>(r.die),
                prefabs = Ls<GameObject>(r.prefabs)
            });
        if (created) AssetDatabase.CreateAsset(t, MonsterTablePath); else EditorUtility.SetDirty(t);
        AssetDatabase.SaveAssets();
        Debug.Log("[SwapBackup] Pengaturan Ganti Monster dipulihkan dari HG_UserData (" + t.rules.Count + " aturan).");
    }

    static string Read(string name) { string p = Path.Combine(Dir, name); return File.Exists(p) ? File.ReadAllText(p) : null; }

    static void RestoreAssetTable()
    {
        string json = Read("AssetSwapTable.json"); if (json == null) return;
        var t = AssetDatabase.LoadAssetAtPath<AssetSwapTable>(AssetTablePath);
        if (t != null && t.entries.Count > 0) return;
        var f = JsonUtility.FromJson<AssetFile>(json); if (f == null || f.e.Count == 0) return;
        bool created = t == null;
        if (created) { Directory.CreateDirectory("Assets/Resources"); t = ScriptableObject.CreateInstance<AssetSwapTable>(); }
        t.entries.Clear();
        foreach (var e in f.e)
            t.entries.Add(new AssetSwapTable.Entry { scene = e.scene, category = e.category, key = e.key, prefab = L<GameObject>(e.prefab), enabled = e.enabled, fitHeight = e.fitHeight, keepPrefabColliders = e.keep, scale = e.scale, yOffset = e.yOffset, rotY = e.rotY, count = e.count });
        if (created) AssetDatabase.CreateAsset(t, AssetTablePath); else EditorUtility.SetDirty(t);
        AssetDatabase.SaveAssets();
        Debug.Log("[SwapBackup] Pengaturan Ganti Aset 3D dipulihkan dari HG_UserData (" + t.entries.Count + " bagian).");
    }

    static void RestoreNpcTable()
    {
        string json = Read("NpcSwapTable.json"); if (json == null) return;
        var t = AssetDatabase.LoadAssetAtPath<NpcSwapTable>(NpcTablePath);
        if (t != null && t.rules.Count > 0) return;
        var f = JsonUtility.FromJson<NpcFile>(json); if (f == null || (f.r.Count == 0 && f.s.Count == 0)) return;
        bool created = t == null;
        if (created) { Directory.CreateDirectory("Assets/Resources"); t = ScriptableObject.CreateInstance<NpcSwapTable>(); }
        t.rules.Clear(); t.seen.Clear();
        foreach (var r in f.r)
            t.rules.Add(new NpcSwapTable.Rule
            {
                label = r.label, npcName = r.npcName, scene = r.scene, enabled = r.enabled, workWhenIdle = r.workWhenIdle, gender = r.gender, race = r.race, role = r.role,
                importance = r.importance, heightScale = r.heightScale, yOffset = r.yOffset, moveSpeedMul = r.mMul <= 0f ? 1f : r.mMul, animSpeed = r.aSpd <= 0f ? 1f : r.aSpd, speedParamScale = r.sScale <= 0f ? 1f : r.sScale, portraitScale = r.pScale <= 0f ? 1f : r.pScale, portraitX = r.pX, portraitY = r.pY, portraitFlip = r.pFlip, portraitSide = r.pSide, portraitTex = L<Texture2D>(r.portraitTex), portrait = L<Sprite>(r.portrait), controller = L<RuntimeAnimatorController>(r.controller),
                clipIdle = L<AnimationClip>(r.idle), clipWalk = L<AnimationClip>(r.walk), clipWave = L<AnimationClip>(r.wave), clipTalk = L<AnimationClip>(r.talk), clipWork = L<AnimationClip>(r.work),
                prefabs = Ls<GameObject>(r.prefabs), portraits = Ls<Sprite>(r.portraits), portraitTexes = Ls<Texture2D>(r.portraitTexes)
            });
        foreach (var s in f.s) t.seen.Add(new NpcSwapTable.Seen { scene = s.scene, npcName = s.npcName, race = s.race, role = s.role, female = s.female, important = s.important });
        if (created) AssetDatabase.CreateAsset(t, NpcTablePath); else EditorUtility.SetDirty(t);
        AssetDatabase.SaveAssets();
        Debug.Log("[SwapBackup] Pengaturan Ganti NPC dipulihkan dari HG_UserData (" + t.rules.Count + " aturan).");
    }
}
