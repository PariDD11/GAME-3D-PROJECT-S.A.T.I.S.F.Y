using System;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Data cerita dunia: lore (Kodeks), world quest berantai, dan penempatan NPC / benda lore.
    /// Seluruhnya diisi lewat Assets/Resources/WorldStory.json (tanpa coding). Lihat WorldStory.json untuk contoh dan panduan.
    /// </summary>
    /// <summary>Lore tambahan dari JSON; digabung ke LoreDB (Arsip Lore) saat game berjalan. pages = halaman teks.</summary>
    [Serializable] public class StoryLore { public string id, title, cat = "Cerita"; public int arc = 1; public string[] pages = new string[0]; }

    [Serializable] public class StoryStep
    {
        public string type;          // talk | kill | gather | lore | reach
        public string target;        // talk: id NPC cerita | kill: id monster | gather: id item | lore: id lore | reach: (kosong)
        public int count = 1;        // kill / gather
        public string text;          // petunjuk tujuan (tampil di tracker dan jurnal)
        public bool hasPos;          // titik tujuan (penanda kompas); wajib untuk reach
        public float x, y, z;
        public string scene = "";    // kosong = di scene mana pun
    }

    [Serializable] public class StoryChoice { public string text, reply; }

    [Serializable] public class WorldQuest
    {
        public string[] offerPages = new string[0], finishPages = new string[0];     // v35z102n31: dialog multi-halaman
        public StoryChoice[] choices = new StoryChoice[0];                              // pertanyaan opsional sebelum menerima
        public string id, title, summary, giver, offer, finish, prereq, rewardItems, unlockLore, startLore;      // unlockLore / startLore: id lore dipisah koma
        public int chapter = 1, minLevel = 1, rewardGold, rewardExp;
        public StoryStep[] steps = new StoryStep[0];
    }

    [Serializable] public class StorySpawn
    {
        public string kind;          // giver | lore
        public string id;            // giver: id NPC cerita | lore: id lore yang dibuka
        public string name, role;    // nama tampil
        public string scene = "";    // kosong = scene mana pun
        public float x, y, z;
        public string anchor = "";   // v35z102n21: bila diisi, x/y/z menjadi OFFSET dari objek bernama itu di scene (mis. SpawnStart) atau "GuildFront" (depan Guild di Town)
    }

    [Serializable] public class StoryRoot
    {
        public StoryLore[] lore = new StoryLore[0];
        public WorldQuest[] quests = new WorldQuest[0];
        public StorySpawn[] spawns = new StorySpawn[0];
    }

    public static class WorldStory
    {
        static StoryRoot root;
        static readonly Dictionary<string, WorldQuest> quests = new Dictionary<string, WorldQuest>();

        public static StoryRoot Root { get { Load(); return root; } }

        public static void Reload() { root = null; Load(); }

        static void Load()
        {
            if (root != null) return;
            root = new StoryRoot(); quests.Clear();
            try
            {
                var ta = Resources.Load<TextAsset>("WorldStory");
                if (ta != null) { var r = JsonUtility.FromJson<StoryRoot>(ta.text); if (r != null) root = r; }
            }
            catch (Exception e) { Debug.LogWarning("[WorldStory] JSON tidak terbaca: " + e.Message); }
            if (root.lore == null) root.lore = new StoryLore[0];
            if (root.quests == null) root.quests = new WorldQuest[0];
            if (root.spawns == null) root.spawns = new StorySpawn[0];
            foreach (var q in root.quests) if (q != null && !string.IsNullOrEmpty(q.id)) { if (q.steps == null) q.steps = new StoryStep[0]; quests[q.id] = q; }
        }

        public static LoreEntry Lore(string id) { return id == null ? null : LoreDB.Get(id); }
        public static WorldQuest Quest(string id) { Load(); WorldQuest q; return id != null && quests.TryGetValue(id, out q) ? q : null; }
        public static StorySpawn FindGiver(string npcId) { foreach (var s in Root.spawns) if (s.kind == "giver" && s.id == npcId) return s; return null; }
        public static string GiverName(string npcId) { var s = FindGiver(npcId); return s != null && !string.IsNullOrEmpty(s.name) ? s.name : npcId; }
    }
}
