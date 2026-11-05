using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// Jalannya world quest dan Kodeks Lore. Progres disimpan di GameState.flags / stats (ikut save):
    /// wq_acc_&lt;id&gt;, wq_done_&lt;id&gt;, stat wq_step_&lt;id&gt; (tahap ke-n), wq_prog_&lt;id&gt; (hitungan di tahap itu), lore_&lt;id&gt;.
    /// </summary>
    public static class WorldQuests
    {
        static GameState GS { get { return GameState.I; } }

        public static bool Accepted(WorldQuest q) { return GS.flags.Contains("wq_acc_" + q.id); }
        public static bool Done(WorldQuest q) { return GS.flags.Contains("wq_done_" + q.id); }
        public static bool Active(WorldQuest q) { return Accepted(q) && !Done(q); }
        public static int StepIndex(WorldQuest q) { return GS.Stat("wq_step_" + q.id); }
        public static int Prog(WorldQuest q) { return GS.Stat("wq_prog_" + q.id); }
        public static StoryStep Step(WorldQuest q) { int i = StepIndex(q); return i >= 0 && i < q.steps.Length ? q.steps[i] : null; }

        public static bool Available(WorldQuest q)
        {
            if (GS == null || Accepted(q) || Done(q) || GS.level < q.minLevel) return false;
            if (!string.IsNullOrEmpty(q.prereq)) { var p = WorldStory.Quest(q.prereq); if (p != null && !Done(p)) return false; }
            return true;
        }

        public static IEnumerable<WorldQuest> All { get { return WorldStory.Root.quests; } }

        public static void Accept(WorldQuest q)
        {
            if (!Available(q)) return;
            GS.flags.Add("wq_acc_" + q.id); GS.stats["wq_step_" + q.id] = 0; GS.stats["wq_prog_" + q.id] = 0;
            Audio.Sfx("quest_accept");
            UnlockMany(q.startLore);
            StoryCompanion.OnAccept(q.id);
            if (TownUI.I != null) TownUI.I.Toast("World Quest diterima: " + q.title);
            CheckInstant(q);
        }

        // ---------------------------------------------------------------- lore
        static void UnlockMany(string ids)
        {
            if (string.IsNullOrEmpty(ids)) return;
            foreach (var part in ids.Split(',')) { var id = part.Trim(); if (id.Length > 0) UnlockLore(id); }
        }

        public static bool Has(string loreId) { return LoreDB.Has(loreId); }

        public static void UnlockLore(string loreId)
        {
            LoreDB.Unlock(loreId);     // toast + flag lore_<id> (Arsip Lore yang sama dengan lore Ashwatch)
        }

        public static int LoreCount { get { return LoreDB.UnlockedCount; } }

        // ---------------------------------------------------------------- event
        /// <summary>kind: kill | gather | lore | talk. Dipanggil dari StarterQuests.OnEvent (monster/herb), LoreObject dan StoryGiver.</summary>
        public static void OnEvent(string kind, string id)
        {
            if (GS == null) return;
            StoryCutscenes.OnEvent(kind, id);
            foreach (var q in WorldStory.Root.quests)
            {
                if (q == null || !Active(q)) continue;
                var s = Step(q);
                if (s == null || s.type != kind || s.target != id) continue;
                int p = Prog(q) + 1; GS.stats["wq_prog_" + q.id] = p;
                if (p >= Mathf.Max(1, s.count)) Advance(q);
                else if (TownUI.I != null) TownUI.I.Toast(q.title + ": " + p + "/" + s.count);
            }
        }

        static void CheckInstant(WorldQuest q)
        {
            var s = Step(q);
            if (s == null) return;
            if (s.type == "lore" && Has(s.target)) Advance(q);     // lore sudah dibaca sebelum quest diterima
        }

        public static void Advance(WorldQuest q)
        {
            int i = StepIndex(q) + 1;
            GS.stats["wq_step_" + q.id] = i; GS.stats["wq_prog_" + q.id] = 0;
            if (i >= q.steps.Length) { Complete(q); return; }
            Audio.Sfx("quest_done");
            if (TownUI.I != null) TownUI.I.Toast(q.title + " - tujuan baru: " + q.steps[i].text);
            CheckInstant(q);
        }

        static void Complete(WorldQuest q)
        {
            GS.flags.Add("wq_done_" + q.id);
            GS.gold += Difficulty.Gold(q.rewardGold); if (q.rewardExp > 0) GS.AddExp(q.rewardExp);
            if (!string.IsNullOrEmpty(q.rewardItems))
                foreach (var part in q.rewardItems.Split(','))
                {
                    var kv = part.Trim().Split(':'); int n = 1;
                    if (kv.Length > 1) int.TryParse(kv[1], out n);
                    if (kv[0].Length > 0) GS.AddItem(kv[0], Mathf.Max(1, n));
                }
            UnlockMany(q.unlockLore);
            Audio.Sfx("quest_reward");
            StoryCutscenes.OnQuestDone(q.id);
            if (TownUI.I != null) TownUI.I.Toast("World Quest selesai: " + q.title + "!  +" + q.rewardGold + "G" + (q.rewardExp > 0 ? "  +" + q.rewardExp + " EXP" : ""));
        }

        /// <summary>Titik tujuan quest aktif pertama (untuk kompas / minimap) di scene sekarang.</summary>
        public static bool Waypoint(out Vector3 pos, out string label)
        {
            pos = Vector3.zero; label = "";
            if (GS == null) return false;
            string sc = SceneManager.GetActiveScene().name;
            foreach (var q in WorldStory.Root.quests)
            {
                if (q == null || !Active(q)) continue;
                var s = Step(q); if (s == null) continue;
                if (s.hasPos && (string.IsNullOrEmpty(s.scene) || s.scene == sc)) { pos = new Vector3(s.x, s.y, s.z); label = q.title; return true; }
                if (s.type == "talk")
                {
                    var g = WorldStory.FindGiver(s.target);
                    if (g != null && (string.IsNullOrEmpty(g.scene) || g.scene == sc)) { pos = new Vector3(g.x, g.y, g.z); label = q.title; return true; }
                }
            }
            return false;
        }

        public static string StepText(WorldQuest q)
        {
            var s = Step(q); if (s == null) return "";
            string t = s.text ?? "";
            if (s.count > 1) t += "  " + Prog(q) + "/" + s.count;
            return t;
        }

        /// <summary>Baris untuk Quest Tracking di HUD.</summary>
        public static List<string[]> TrackerRows()
        {
            var l = new List<string[]>();
            if (GS == null) return l;
            foreach (var q in WorldStory.Root.quests) if (q != null && Active(q)) l.Add(new[] { "World Quest: " + q.title, StepText(q) });
            return l;
        }

        /// <summary>Dicek berkala oleh StoryWorld: tahap "reach" selesai bila pemain dekat titiknya.</summary>
        public static void CheckReach(Vector3 playerPos)
        {
            if (GS == null) return;
            string sc = SceneManager.GetActiveScene().name;
            foreach (var q in WorldStory.Root.quests)
            {
                if (q == null || !Active(q)) continue;
                var s = Step(q);
                if (s == null || s.type != "reach" || !(string.IsNullOrEmpty(s.scene) || s.scene == sc)) continue;
                var d = playerPos - new Vector3(s.x, s.y, s.z); d.y = 0f;
                if (d.sqrMagnitude < 7f * 7f) Advance(q);
            }
        }
    }
}
