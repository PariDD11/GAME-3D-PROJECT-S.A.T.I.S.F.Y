using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>NPC tersembunyi (10 di kota). Temui semuanya untuk bonus. Ditandai "?" melayang saat pemain dekat.</summary>
    public class HiddenNpc : Interactable
    {
        public string id = "h1", title = "NPC Tersembunyi";
        [TextArea] public string lines = "";
        public int rewardGold = 50;
        public string rewardItem = ""; public int rewardCount = 1;
        public const int Total = 10;
        public Transform marker;

        static Transform player;

        string Flag { get { return "hidden_" + id; } }
        public static int Found()
        {
            int n = 0; if (GameState.I == null) return 0;
            foreach (var f in GameState.I.flags) if (f.StartsWith("hidden_") && f != "hidden_bonus") n++;
            return n;
        }

        void Awake() { prompt = "Bicara"; radius = 2.8f; }

        void Update()
        {
            if (marker == null) return;
            if (player == null) { var pc = FindFirstObjectByType<PlayerController>(); if (pc != null) player = pc.transform; if (player == null) return; }
            bool done = GameState.I != null && GameState.I.flags.Contains(Flag);
            bool near = (player.position - transform.position).sqrMagnitude < 18f * 18f;
            marker.gameObject.SetActive(near && !done);
            if (marker.gameObject.activeSelf)
            {
                marker.localPosition = new Vector3(0f, 2.55f + Mathf.Sin(Time.time * 3f) * 0.08f, 0f);
                if (Camera.main != null) marker.rotation = Quaternion.LookRotation(marker.position - Camera.main.transform.position);
            }
        }

        public override void Interact()
        {
            var gs = GameState.I; if (gs == null) return;
            var an = GetComponent<NpcAnimator>(); if (an != null) an.Wave(2f);
            string[] ls = lines.Split('|');
            string text = ls[Random.Range(0, ls.Length)];
            if (!gs.flags.Contains(Flag))
            {
                gs.flags.Add(Flag);
                gs.gold += rewardGold;
                string r = "+" + rewardGold + "G";
                if (!string.IsNullOrEmpty(rewardItem)) { gs.AddItem(rewardItem, rewardCount); r += "  +" + rewardItem + " x" + rewardCount; }
                int n = Found();
                text += "\n\n[Hadiah] " + r + "\n[NPC Tersembunyi " + n + "/" + Total + "]";
                if (n >= Total && !gs.flags.Contains("hidden_bonus"))
                {
                    gs.flags.Add("hidden_bonus"); gs.gold += 1000; gs.skillPoints += 2; gs.AddItem("potion", 5);
                    text += "\n\n★ SEMUA NPC TERSEMBUNYI DITEMUKAN! ★\n+1000G, +2 Skill Point, +5 Potion";
                }
                if (TownUI.I != null) TownUI.I.Toast("Kamu menemukan " + title + "!  (" + n + "/" + Total + ")");
            }
            else text += "\n\n(Sudah kamu temui.)";
            TownUI.I.Open(title, () => text, () => new List<UIEntry>());
        }
    }
}
