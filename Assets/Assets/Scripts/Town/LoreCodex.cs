using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    public class LoreEntry
    {
        public string id, title, cat; public int arc; public string[] pages;
        public LoreEntry(string id, string title, string cat, int arc, string[] pages) { this.id = id; this.title = title; this.cat = cat; this.arc = arc; this.pages = pages; }
    }

    /// <summary>Lore Ashwatch (Tujuh Dewa, Lucifer, Dark Mana, Sang Perintis). Terbuka lewat Guild Master (per Arc) dan prasasti di dunia.</summary>
    public static partial class LoreDB
    {
        static LoreEntry[] all;
        public static LoreEntry[] All
        {
            get
            {
                if (all != null) return all;
                var l = new List<LoreEntry>(Build());
                foreach (var x in WorldStory.Root.lore)      // lore tambahan dari Resources/WorldStory.json
                {
                    if (x == null || string.IsNullOrEmpty(x.id) || x.pages == null || x.pages.Length == 0 || l.Exists(e => e.id == x.id)) continue;
                    l.Add(new LoreEntry(x.id, x.title, string.IsNullOrEmpty(x.cat) ? "Cerita" : x.cat, Mathf.Max(1, x.arc), x.pages));
                }
                all = l.ToArray(); return all;
            }
        }
        public static LoreEntry Get(string id) { foreach (var e in All) if (e.id == id) return e; return null; }
        public static bool Has(string id) { var g = GameState.I; return g != null && g.flags.Contains("lore_" + id); }
        public static int UnlockedCount { get { int n = 0; foreach (var e in All) if (Has(e.id)) n++; return n; } }

        /// <summary>Buka entri; true bila baru.</summary>
        public static bool Unlock(string id, bool toast = true)
        {
            var g = GameState.I; var e = Get(id);
            if (g == null || e == null || Has(id)) return false;
            g.flags.Add("lore_" + id);
            if (toast) { GameState.Say("Lore baru: " + e.title); Audio.Sfx("quest_accept"); }
            return true;
        }

        /// <summary>Arc cerita pemain saat ini (1-4) menurut rank.</summary>
        public static int PlayerArc
        {
            get { var g = GameState.I; int i = g == null ? 0 : GameState.RankIndex(g.rank); return i <= 3 ? 1 : i <= 5 ? 2 : i <= 7 ? 3 : 4; }
        }
    }

    public static class LoreCodex
    {
        /// <summary>Baca satu entri halaman demi halaman. back = aksi tombol kembali (null = tutup panel).</summary>
        public static void Read(string id, int page, System.Action back)
        {
            var e = LoreDB.Get(id); if (e == null) return;
            StoryForeshadow.Reveal(id);      // v35z102n77: tanda foreshadowing mendapat halaman "kini kau tahu" setelah bab pembayarnya selesai
            page = Mathf.Clamp(page, 0, e.pages.Length - 1);
            TownUI.I.Open(e.title + "  (" + (page + 1) + "/" + e.pages.Length + ")", () => e.pages[page], () =>
            {
                var l = new List<UIEntry>();
                if (page + 1 < e.pages.Length) l.Add(new UIEntry("Lanjut >", () => Read(id, page + 1, back)));
                if (page > 0) l.Add(new UIEntry("< Halaman sebelumnya", () => Read(id, page - 1, back)));
                l.Add(new UIEntry(back != null ? "< Kembali" : "Tutup", back != null ? back : (System.Action)(() => TownUI.I.Close())));
                return l;
            });
        }

        /// <summary>Arsip lore yang sudah ditemukan (Perpustakaan). </summary>
        public static void OpenArchive(System.Action back)
        {
            TownUI.I.Open("Arsip Lore Ashwatch", () => "Catatan yang sudah kamu temukan: " + LoreDB.UnlockedCount + "/" + LoreDB.All.Length + ".\nSebagian hanya diceritakan Guild Master, sebagian terukir di reruntuhan.", () =>
            {
                var l = new List<UIEntry>();
                string cat = null;
                foreach (var e in LoreDB.All)
                {
                    if (!LoreDB.Has(e.id)) continue;
                    var en = e;
                    string pre = en.cat != cat ? "[" + en.cat + "] " : "   ";
                    cat = en.cat;
                    l.Add(new UIEntry(pre + en.title, () => Read(en.id, 0, () => OpenArchive(back))));
                }
                if (l.Count == 0) l.Add(new UIEntry("(belum ada catatan)", () => { }, false));
                l.Add(new UIEntry("Putar ulang Prolog (sinematik)", () => { TownUI.I.Close(); Prologue.Replay(); }, !Cutscene.Active && !PlayerController.InFight()));      // v35z102n77
                l.Add(new UIEntry("< Kembali", back != null ? back : (System.Action)(() => TownUI.I.Close())));
                return l;
            });
        }

        /// <summary>Menu Guild Master: kisah yang boleh diceritakan sesuai Arc pemain.</summary>
        public static void OpenMasterLore(System.Action back)
        {
            int arc = LoreDB.PlayerArc;
            TownUI.I.Open("Kisah Guild", () => "\"Ada hal yang hanya diwariskan dari Guild Master ke Guild Master. Aku akan menceritakannya sesuai seberapa jauh kau melangkah, Hunter.\"", () =>
            {
                var l = new List<UIEntry>();
                foreach (var e in LoreDB.All)
                {
                    var en = e;
                    if (en.arc > arc || en.cat.StartsWith("Kronik")) continue;
                    bool isNew = !LoreDB.Has(en.id);
                    l.Add(new UIEntry((isNew ? "[BARU] " : "") + en.title, () => { LoreDB.Unlock(en.id, false); Read(en.id, 0, () => OpenMasterLore(back)); }));
                }
                int locked = 0; foreach (var e in LoreDB.All) if (e.arc > arc && !e.cat.StartsWith("Kronik")) locked++;
                if (locked > 0) l.Add(new UIEntry("??? (" + locked + " kisah belum waktunya)", () => { }, false));
                l.Add(new UIEntry("< Kembali", back));
                return l;
            });
        }
    }

    /// <summary>Prasasti kuno di dunia: dibaca sekali membuka entri lore (dan bisa dibaca ulang).</summary>
    public class LoreStone : Interactable
    {
        public string loreId = "tujuh_dewa";
        public override void Interact()
        {
            if (LoreDB.Get(loreId) == null) return;
            bool fresh = LoreDB.Unlock(loreId, true);
            LoreCodex.Read(loreId, 0, null);
        }
    }
}
