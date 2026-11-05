using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HunterGuild
{
    /// <summary>
    /// Jurnal (tombol L): Quest Aktif (progres + arah), Riwayat quest selesai, Boss (sudah/belum dikalahkan + petunjuk lokasi), Pencapaian (dengan hadiah).
    /// Data dari stats GameState: qd_&lt;quest&gt; selesai, bk_&lt;monster&gt; kill, dll. Hadiah pencapaian diklaim lewat flag ach_&lt;key&gt;.
    /// </summary>
    public class JournalUI : MonoBehaviour
    {
        public static JournalUI I { get; private set; }
        public bool IsOpen { get { return root != null && root.activeSelf; } }

        // warna mengikuti tema job (UiStyle.Cur)
        static Color Gold { get { return UiStyle.Cur.Gold; } }
        static Color GoldDim { get { return UiStyle.Cur.GoldDim; } }
        static Color Panel { get { var t = UiStyle.Cur; return new Color(t.PanelLo.r * 1.4f + 0.02f, t.PanelLo.g * 1.4f + 0.02f, t.PanelLo.b * 1.4f + 0.025f, 0.97f); } }
        static Color PanelIn { get { var t = UiStyle.Cur; return Color.Lerp(t.PanelLo, t.PanelHi, 0.5f); } }
        int themeVer = -1;
        static readonly string[] Tabs = { "RINGKASAN", "QUEST AKTIF", "RIWAYAT", "BOSS", "PENCAPAIAN", "WORLD QUEST", "KODEKS LORE", "RESEP" };

        class Ach { public string key, name, desc; public int target, gold; public System.Func<GameState, int> cur; }
        static readonly List<Ach> Achs = new List<Ach>();

        // petunjuk lokasi boss (untuk yang belum dikalahkan)
        static readonly Dictionary<string, string> BossHint = new Dictionary<string, string>
        {
            { "behemoth", "Sarang Behemoth, Hutan Perbatasan (jalan raya Ashwatch-Eldrath)." },
            { "cerberus", "Gerbang Makam Malrek, Padang Tulang." },
            { "ogre_chief", "Sarang Ogre, Hutan Perbatasan (jalan raya Perbatasan-Eldrath)." },
            { "ignarok", "Sarang Ignarok, puncak Gunung Vulkanik (Gerbang Utara Eldrath, quest Guild)." },
            { "bone_king", "Makam Raja Malrek, Padang Tulang (Gerbang Timur Eldrath, quest Guild)." },
            { "demon_lord", "Takhta Zargoth, Lembah Iblis (Gerbang Barat Eldrath, quest Guild)." },
            { "necromancer", "Menara Maelis, Rawa Terkutuk (Gerbang Barat Ashwatch)." },
        };

        GameObject root;
        RectTransform body;
        int tab, openedFrame = -1, page;

        static JournalUI() { BuildAchs(); }

        static void A(string key, string name, string desc, int target, int gold, System.Func<GameState, int> cur)
        {
            Achs.Add(new Ach { key = key, name = name, desc = desc, target = target, gold = gold, cur = cur });
        }

        static void BuildAchs()
        {
            A("kill10", "Pemburu Pemula", "Kalahkan 10 monster.", 10, 50, g => g.Stat("kills"));
            A("kill100", "Pemburu Berpengalaman", "Kalahkan 100 monster.", 100, 300, g => g.Stat("kills"));
            A("kill500", "Pembantai Monster", "Kalahkan 500 monster.", 500, 1500, g => g.Stat("kills"));
            A("kill2000", "Legenda Perburuan", "Kalahkan 2000 monster.", 2000, 6000, g => g.Stat("kills"));
            A("big10", "Pemburu Raksasa", "Kalahkan 10 monster besar.", 10, 500, g => g.Stat("big_kills"));
            A("boss1", "Penakluk Pertama", "Kalahkan 1 boss.", 1, 200, g => g.Stat("boss_kills"));
            A("boss5", "Pembunuh Boss", "Kalahkan 5 boss.", 5, 1200, g => g.Stat("boss_kills"));
            A("quest1", "Pekerjaan Pertama", "Selesaikan 1 quest.", 1, 50, g => g.Stat("quests_n"));
            A("quest25", "Hunter Andal", "Selesaikan 25 quest.", 25, 800, g => g.Stat("quests_n"));
            A("quest100", "Tulang Punggung Guild", "Selesaikan 100 quest.", 100, 4000, g => g.Stat("quests_n"));
            A("lv10", "Naik Daun", "Capai Level 10.", 10, 200, g => g.level);
            A("lv30", "Hunter Sejati", "Capai Level 30.", 30, 2000, g => g.level);
            A("rankD", "Rank D", "Capai rank D.", GameState.RankIndex("D"), 400, g => GameState.RankIndex(g.rank));
            A("rankB", "Rank B", "Capai rank B.", GameState.RankIndex("B"), 1500, g => GameState.RankIndex(g.rank));
            A("rankS", "Rank S", "Capai rank S.", GameState.RankIndex("S"), 5000, g => GameState.RankIndex(g.rank));
            A("forest", "Penguasa Hutan", "Kalahkan 100 monster di hutan.", 100, 300, g => g.Stat("kills_forest"));
            A("desert", "Anak Gurun", "Kalahkan 80 monster di Gurun Zahar.", 80, 500, g => g.Stat("kills_desert"));
            A("swamp", "Penyintas Rawa", "Kalahkan 80 monster di Rawa Terkutuk.", 80, 500, g => g.Stat("kills_swamp"));
            A("frontier", "Penjaga Jalan Raya", "Kalahkan 80 monster di Hutan Perbatasan.", 80, 500, g => g.Stat("kills_frontier"));
            A("volcano", "Penakluk Api", "Kalahkan 80 monster di Gunung Vulkanik.", 80, 900, g => g.Stat("kills_volcano"));
            A("bone", "Penghuni Makam", "Kalahkan 80 monster di Padang Tulang.", 80, 900, g => g.Stat("kills_bone"));
            A("demon", "Pemburu Iblis", "Kalahkan 80 monster di Lembah Iblis.", 80, 1200, g => g.Stat("kills_demon"));
            A("tower10", "Pendaki Menara", "Capai lantai 10 Menara Eldrath.", 10, 300, g => g.dungeonBest);
            A("tower50", "Setengah Jalan", "Capai lantai 50 Menara Eldrath.", 50, 2500, g => g.dungeonBest);
            A("tower100", "Puncak Menara", "Capai lantai 100 Menara Eldrath.", 100, 10000, g => g.dungeonBest);
            A("evo15", "Evolusi", "Capai evolusi Lv.15.", 1, 500, g => string.IsNullOrEmpty(g.evo) ? 0 : 1);
            A("evo30", "Kelas Tertinggi", "Capai evolusi Lv.30.", 1, 2000, g => string.IsNullOrEmpty(g.evo2) ? 0 : 1);
            A("rich", "Kaya Raya", "Miliki 10.000 gold.", 10000, 500, g => g.gold);
            A("best50", "Kolektor Bestiary", "Temukan 50% monster di Bestiary.", 50, 800, g =>
            {
                var all = MonsterDB.All(); int n = 0;
                foreach (var d in all) if (g.Stat("bk_" + d.id) > 0) n++;
                return all.Count == 0 ? 0 : Mathf.RoundToInt(100f * n / all.Count);
            });
        }

        void Awake() { I = this; }
        void OnDestroy() { if (I == this) I = null; }

        void Update()
        {
            if (!IsOpen) return;
            if (Time.frameCount != openedFrame && (TownInput.JournalDown() || TownInput.CancelDown())) Hide();
        }

        public void Show()
        {
            if (UiStyle.Stale(ref themeVer) && root != null) { Destroy(root); root = null; }
            if (root == null) Build();
            root.SetActive(true);
            openedFrame = Time.frameCount;
            TownUI.I.SetExternalOpen(true);
            Refresh();
        }

        public void ShowTab(int t) { Show(); tab = t; page = 0; Refresh(); }

        string selRecipe = "";

        // ------------------------------------------------------------------ ringkasan: quest aktif + tujuan di peta + petunjuk
        void DrawSummary()
        {
            var gs = GameState.I;
            var quests = new System.Text.StringBuilder(); var dest = new System.Text.StringBuilder(); var clues = new System.Text.StringBuilder();
            var q = gs.activeQuest;
            if (q != null)
            {
                var gq = GuildQuestDB.Find(q.id);
                quests.Append("[Guild] ").Append(q.title).Append(q.completed ? "  (selesai - lapor)" : "  " + gs.questProgress + "/" + q.targetCount).Append("\n");
                dest.Append(q.completed ? "Guild: lapor di resepsionis.\n" : "Guild: " + (gq != null ? gq.area + ", " + GuildQuest.RegionName(gq.Region) : "-") + "\n");
            }
            int wn = 0;
            foreach (var w in WorldQuests.All)
            {
                if (w == null || !WorldQuests.Active(w)) continue;
                wn++;
                quests.Append("[Cerita] B").Append(w.chapter).Append(" ").Append(w.title).Append("\n");
                dest.Append("Cerita: ").Append(WorldQuests.StepText(w)).Append("\n");
            }
            foreach (var c in Investigation.Cases)
            {
                if (Investigation.State(c) != 1) continue;
                quests.Append("[Kasus] ").Append(c.title).Append("  ").Append(Investigation.FoundCount(c)).Append("/").Append(c.clues.Length).Append(" petunjuk\n");
                dest.Append("Kasus: ").Append(Investigation.FoundCount(c) >= c.clues.Length ? "tuduh pelaku di Papan Pengumuman Guild" : "cari kilau emas di " + c.place).Append("\n");
                for (int i = 0; i < c.clues.Length; i++) if (Investigation.Found(c, i)) clues.Append("- ").Append(c.clues[i]).Append("\n");
                for (int k = 0; k < c.witness.Length; k++) if (Investigation.Asked(c, k)) clues.Append("- Saksi: ").Append(c.witness[k]).Append("\n");
            }
            foreach (var h in HiddenBosses.Defs)
            {
                if (!HiddenBosses.HintSeen(h) || HiddenBosses.Defeated(h)) continue;
                dest.Append("Altar rahasia: ").Append(h.region).Append("\n");
                clues.Append("- Prasasti ").Append(h.region).Append(": altar boss rahasia sudah diketahui.\n");
            }
            var lore = new List<LoreEntry>(); foreach (var e in LoreDB.All) if (LoreDB.Has(e.id)) lore.Add(e);
            for (int i = Mathf.Max(0, lore.Count - 3); i < lore.Count; i++) clues.Append("- Kodeks: ").Append(lore[i].title).Append("\n");
            if (quests.Length == 0) quests.Append("Tidak ada quest berjalan.\nCoba: ").Append(Suggest(gs).Split('\n')[0]).Append("\n");
            if (dest.Length == 0) dest.Append("Belum ada tujuan.\n");
            if (clues.Length == 0) clues.Append("Belum ada petunjuk. Periksa prasasti, saksi, dan benda berkilau.\n");
            Label(body, "RINGKASAN JURNAL", 20, 10, 500, 36, 26, TextAnchor.MiddleLeft, Gold);
            Label(body, WorldReact.Conditions().Replace("\n", "   |   "), 400, 10, 920, 40, 14, TextAnchor.MiddleRight, new Color(0.7f, 0.8f, 0.9f));
            string[] heads = { "QUEST BERJALAN", "TUJUAN DI PETA", "PETUNJUK DITEMUKAN" };
            string[] txt = { quests.ToString(), dest.ToString(), clues.ToString() };
            for (int i = 0; i < 3; i++)
            {
                float x = 20 + i * 442;
                Box(body, x, 54, 428, 640, new Color(0f, 0f, 0f, 0.18f));
                Label(body, heads[i], x + 12, 60, 404, 32, 22, TextAnchor.MiddleLeft, Gold);
                Label(body, txt[i], x + 12, 98, 404, 590, 18, TextAnchor.UpperLeft, Color.white);
            }
        }

        void DrawRecipes()
        {
            var gs = GameState.I;
            var all = Blacksmith.AllRecipes();
            int can = 0; foreach (var r in all) if (CanCraft(gs, r)) can++;
            Label(body, "BUKU RESEP PANDAI BESI   (bisa dibuat sekarang: " + can + ")", 20, 10, 1290, 36, 26, TextAnchor.MiddleLeft, Gold);
            if (all.Count == 0) return;
            Blacksmith.RecipeRow cur = null; foreach (var r in all) if (r.result == selRecipe) cur = r;
            if (cur == null) { cur = all[0]; selRecipe = cur.result; }
            const int per = 12; int pages = Mathf.Max(1, Mathf.CeilToInt(all.Count / (float)per)); page = Mathf.Clamp(page, 0, pages - 1);
            for (int i = 0; i < per; i++)
            {
                int idx = page * per + i; if (idx >= all.Count) break;
                var r = all[idx]; string id = r.result; var d = ItemDB.Get(id);
                var b = UIKit.Btn(body, (CanCraft(gs, r) ? "[Siap] " : "") + (d != null ? d.name : id), () => { selRecipe = id; Refresh(); }, true,
                    id == selRecipe ? new Color(0.45f, 0.35f, 0.12f) : new Color(0.16f, 0.18f, 0.24f), 17);
                UIKit.At((RectTransform)b.transform, 20, 54 + i * 48, 480, 44);
            }
            Pager(pages);
            Box(body, 520, 54, 2, 590, GoldDim);
            var cd = ItemDB.Get(cur.result);
            Label(body, cd != null ? cd.name : cur.result, 540, 54, 770, 44, 30, TextAnchor.MiddleLeft, Gold);
            Label(body, cur.area + "   |   " + cur.slot + "   |   Job: " + (cur.job == "Any" ? "Semua" : cur.job), 540, 98, 770, 26, 17, TextAnchor.MiddleLeft, new Color(0.7f, 0.7f, 0.78f));
            string t = "BIAYA: " + cur.gold + " gold" + (gs.gold >= cur.gold ? "  (cukup)" : "  (kurang " + (cur.gold - gs.gold) + ")") + "\n\nBAHAN:\n";
            foreach (var m in cur.mats)
            {
                var md = ItemDB.Get(m.Key); int have = gs.Count(m.Key);
                t += (have >= m.Value ? "[x] " : "[ ] ") + (md != null ? md.name : m.Key) + "  " + have + "/" + m.Value + "\n      asal: " + Blacksmith.SourceOf(m.Key) + "\n";
            }
            t += "\nBawa bahan ke Hadrik (Pandai Besi), lalu perkuat dengan Enchant, Imbue, dan Socket.";
            Label(body, t, 540, 134, 770, 440, 20, TextAnchor.UpperLeft, Color.white);
            bool pinned = RecipeTracker.Pinned == cur.result;
            var pb = UIKit.Btn(body, pinned ? "Berhenti lacak bahan" : "Lacak bahan di HUD", () => { RecipeTracker.Set(pinned ? "" : cur.result); Refresh(); }, true, pinned ? new Color(0.45f, 0.2f, 0.2f) : new Color(0.3f, 0.22f, 0.06f), 20);
            UIKit.At((RectTransform)pb.transform, 540, 600, 330, 44);
        }

        static bool CanCraft(GameState gs, Blacksmith.RecipeRow r)
        {
            if (gs.gold < r.gold) return false;
            foreach (var m in r.mats) if (gs.Count(m.Key) < m.Value) return false;
            return true;
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
            if (TownUI.I != null) TownUI.I.SetExternalOpen(false);
        }

        static Image Box(Transform parent, float x, float y, float w, float h, Color col)
        {
            var im = UIKit.Img("Box", parent, col);
            im.raycastTarget = false;
            UIKit.At(im.rectTransform, x, y, w, h);
            return im;
        }

        static Text Label(Transform parent, string s, float x, float y, float w, float h, int size, TextAnchor a, Color col)
        {
            var t = UIKit.Txt("T", parent, size, a, col);
            UIKit.At(t.rectTransform, x, y, w, h);
            t.text = s;
            return t;
        }

        void Build()
        {
            var border = Box(TownUI.I.Root, 0, 0, 1400, 860, Gold);
            var outer = border.rectTransform;
            var c = new Vector2(0.5f, 0.5f);
            UIKit.Place(outer, c, c, c, Vector2.zero, new Vector2(1400, 860));
            root = outer.gameObject;
            var bg = Box(outer, 2, 2, 1396, 856, Panel);
            Label(bg.transform, "JURNAL", 0, 10, 1396, 44, 34, TextAnchor.MiddleCenter, Gold);
            var close = UIKit.Btn(bg.transform, "X", Hide, true, new Color(0.4f, 0.2f, 0.2f), 24);
            UIKit.At((RectTransform)close.transform, 1330, 10, 50, 44);
            for (int i = 0; i < Tabs.Length; i++)
            {
                int ti = i;
                var b = UIKit.Btn(bg.transform, Tabs[i], () => { tab = ti; page = 0; Refresh(); }, true, new Color(0.18f, 0.2f, 0.27f), 17);
                UIKit.At((RectTransform)b.transform, 30 + i * 168, 64, 162, 46);
                tabBtns[i] = b.GetComponent<Image>();
            }
            body = UIKit.Rect("Body", bg.transform);
            UIKit.At(body, 30, 122, 1336, 716);
        }

        readonly Image[] tabBtns = new Image[8];

        void Refresh()
        {
            for (int i = 0; i < tabBtns.Length; i++) if (tabBtns[i] != null) tabBtns[i].color = i == tab ? new Color(0.45f, 0.35f, 0.12f) : new Color(0.18f, 0.2f, 0.27f);
            UIKit.Clear(body);
            Box(body, 0, 0, 1336, 716, PanelIn);
            switch (tab)
            {
                case 0: DrawSummary(); break;
                case 1: DrawActive(); break;
                case 2: DrawHistory(); break;
                case 3: DrawBosses(); break;
                case 4: DrawAch(); break;
                case 5: DrawWorld(); break;
                case 7: DrawRecipes(); break;
                default: DrawLore(); break;
            }
        }

        // ------------------------------------------------------------------ world quest + kodeks lore
        string selWq = "", selLore = "";

        void DrawWorld()
        {
            var gs = GameState.I;
            var list = new List<WorldQuest>();
            foreach (var q in WorldQuests.All) if (q != null && (WorldQuests.Accepted(q) || WorldQuests.Done(q) || WorldQuests.Available(q))) list.Add(q);
            list.Sort((a, b) => { int ra = WorldQuests.Active(a) ? 0 : WorldQuests.Done(a) ? 2 : 1, rb = WorldQuests.Active(b) ? 0 : WorldQuests.Done(b) ? 2 : 1; return ra != rb ? ra.CompareTo(rb) : a.chapter.CompareTo(b.chapter); });
            Label(body, "WORLD QUEST", 20, 10, 500, 36, 26, TextAnchor.MiddleLeft, Gold);
            if (list.Count == 0) { Label(body, "Belum ada world quest. Jelajahi dunia dan bicaralah dengan para penduduk.", 20, 60, 1290, 40, 22, TextAnchor.UpperLeft, Color.white); return; }
            WorldQuest cur = null; foreach (var q in list) if (q.id == selWq) cur = q;
            if (cur == null) { cur = list[0]; selWq = cur.id; }
            const int per = 12; int pages = Mathf.Max(1, Mathf.CeilToInt(list.Count / (float)per)); page = Mathf.Clamp(page, 0, pages - 1);
            for (int i = 0; i < per; i++)
            {
                int idx = page * per + i; if (idx >= list.Count) break;
                var q = list[idx]; string id = q.id;
                string tag = WorldQuests.Done(q) ? "[Selesai] " : WorldQuests.Active(q) ? "[Berjalan] " : "[Baru] ";
                var b = UIKit.Btn(body, "B" + q.chapter + "  " + tag + q.title, () => { selWq = id; Refresh(); }, true, id == selWq ? new Color(0.45f, 0.35f, 0.12f) : new Color(0.16f, 0.18f, 0.24f), 17);
                UIKit.At((RectTransform)b.transform, 20, 54 + i * 48, 480, 44);
            }
            Pager(pages);
            Box(body, 520, 54, 2, 590, GoldDim);
            Label(body, cur.title, 540, 54, 770, 40, 28, TextAnchor.MiddleLeft, Gold);
            Label(body, "Bab " + cur.chapter + (cur.minLevel > 1 ? "   |   Level " + cur.minLevel + "+" : "") + (string.IsNullOrEmpty(cur.giver) ? "" : "   |   Pemberi: " + WorldStory.GiverName(cur.giver)), 540, 94, 770, 26, 16, TextAnchor.MiddleLeft, new Color(0.7f, 0.7f, 0.78f));
            Label(body, cur.summary ?? "", 540, 128, 770, 150, 19, TextAnchor.UpperLeft, new Color(0.88f, 0.88f, 0.92f));
            Label(body, "TUJUAN", 540, 292, 770, 28, 20, TextAnchor.MiddleLeft, Gold);
            int si = WorldQuests.Accepted(cur) ? WorldQuests.StepIndex(cur) : -1; bool done = WorldQuests.Done(cur);
            string t = "";
            for (int i = 0; i < cur.steps.Length; i++)
            {
                bool past = done || i < si, now = !done && i == si;
                if (!past && !now && !(si < 0)) { t += "[ ]  ???\n"; continue; }
                if (si < 0 && i > 0) { t += "[ ]  ???\n"; continue; }
                t += (past ? "[x]  " : "[ ]  ") + (cur.steps[i].text ?? "") + (now ? "   " + WorldQuests.StepText(cur).Substring((cur.steps[i].text ?? "").Length) : "") + "\n";
            }
            Label(body, t, 540, 322, 770, 220, 18, TextAnchor.UpperLeft, Color.white);
            Label(body, "HADIAH: " + cur.rewardGold + " gold" + (cur.rewardExp > 0 ? ", " + cur.rewardExp + " EXP" : "") + (string.IsNullOrEmpty(cur.rewardItems) ? "" : ", " + cur.rewardItems.Replace(":", " x")), 540, 560, 770, 36, 18, TextAnchor.MiddleLeft, Gold);
            if (!string.IsNullOrEmpty(cur.unlockLore)) { var l = WorldStory.Lore(cur.unlockLore); if (l != null) Label(body, "Membuka Kodeks: " + (WorldQuests.Has(l.id) ? l.title : "???"), 540, 598, 770, 30, 16, TextAnchor.MiddleLeft, new Color(0.6f, 0.85f, 1f)); }
        }

        void DrawLore()
        {
            var all = new List<LoreEntry>(LoreDB.All);
            Label(body, "KODEKS LORE   " + LoreDB.UnlockedCount + "/" + all.Count, 20, 10, 700, 36, 26, TextAnchor.MiddleLeft, Gold);
            if (all.Count == 0) { Label(body, "Belum ada lore.", 20, 60, 1290, 40, 22, TextAnchor.UpperLeft, Color.white); return; }
            LoreEntry cur = null; foreach (var l in all) if (l.id == selLore) cur = l;
            if (cur == null) { cur = all[0]; selLore = cur.id; }
            const int per = 12; int pages = Mathf.Max(1, Mathf.CeilToInt(all.Count / (float)per)); page = Mathf.Clamp(page, 0, pages - 1);
            for (int i = 0; i < per; i++)
            {
                int idx = page * per + i; if (idx >= all.Count) break;
                var l = all[idx]; string id = l.id; bool has = LoreDB.Has(id);
                var b = UIKit.Btn(body, "A" + l.arc + "  " + (has ? l.title : "??? (terkunci)"), () => { selLore = id; Refresh(); }, true, id == selLore ? new Color(0.45f, 0.35f, 0.12f) : new Color(0.16f, 0.18f, 0.24f), 17);
                UIKit.At((RectTransform)b.transform, 20, 54 + i * 48, 480, 44);
            }
            Pager(pages);
            Box(body, 520, 54, 2, 590, GoldDim);
            bool open = LoreDB.Has(cur.id);
            Label(body, open ? cur.title : "???", 540, 54, 770, 44, 30, TextAnchor.MiddleLeft, Gold);
            Label(body, "Arc " + cur.arc + (string.IsNullOrEmpty(cur.cat) ? "" : "   |   " + cur.cat), 540, 98, 770, 26, 16, TextAnchor.MiddleLeft, new Color(0.7f, 0.7f, 0.78f));
            Label(body, open ? string.Join("\n\n", cur.pages) : "Belum ditemukan. Selesaikan world quest, tanya Guild Master, atau periksa prasasti dan benda tua di dunia untuk membukanya.", 540, 134, 770, 490, 19, TextAnchor.UpperLeft, open ? new Color(0.9f, 0.9f, 0.94f) : new Color(0.6f, 0.6f, 0.65f));
        }

        // ------------------------------------------------------------------ quest aktif
        void DrawActive()
        {
            var gs = GameState.I;
            var q = gs.activeQuest;
            if (q == null)
            {
                Label(body, "Tidak ada quest aktif.", 30, 30, 1270, 50, 34, TextAnchor.UpperLeft, Gold);
                Label(body, "Arah untukmu:\n" + Suggest(gs), 30, 100, 1270, 500, 24, TextAnchor.UpperLeft, Color.white);
                return;
            }
            var gq = GuildQuestDB.Find(q.id);
            Label(body, q.title, 30, 24, 1270, 52, 38, TextAnchor.UpperLeft, Gold);
            string where = gq != null ? gq.area + "  —  " + GuildQuest.RegionName(gq.Region) : "-";
            Label(body, "Rank " + (gq != null ? gq.rank : "?") + "   |   Lokasi: " + where, 30, 80, 1270, 34, 22, TextAnchor.UpperLeft, new Color(0.8f, 0.8f, 0.85f));
            Label(body, q.description, 30, 130, 1270, 120, 24, TextAnchor.UpperLeft, Color.white);
            string obj;
            if (q.completed) obj = "Selesai! Kembali ke Guild untuk melapor dan ambil hadiah.";
            else obj = (q.type == "gather" ? "Kumpulkan " : q.type == "hunt" ? "Buru " : "Basmi ") + q.targetId + ": " + gs.questProgress + " / " + q.targetCount;
            Label(body, "TUJUAN", 30, 270, 400, 36, 26, TextAnchor.MiddleLeft, Gold);
            Label(body, obj, 30, 308, 1270, 40, 26, TextAnchor.MiddleLeft, q.completed ? new Color(0.5f, 1f, 0.5f) : Color.white);
            Label(body, "HADIAH", 30, 380, 400, 36, 26, TextAnchor.MiddleLeft, Gold);
            Label(body, q.reward + " gold,  " + q.exp + " EXP", 30, 418, 1270, 40, 24, TextAnchor.MiddleLeft, Color.white);
            Label(body, "PETUNJUK", 30, 490, 400, 36, 26, TextAnchor.MiddleLeft, Gold);
            string hint = q.completed ? "Lapor di meja resepsionis Guild." : gq != null && !string.IsNullOrEmpty(gq.huntScene) ? "Berangkat lewat gerbang menuju " + GuildQuest.RegionName(gq.Region) + ". Pastikan bekal, ramuan, dan botol minum terisi." : "Selesaikan sesuai deskripsi.";
            Label(body, hint, 30, 528, 1270, 100, 22, TextAnchor.UpperLeft, new Color(0.85f, 0.85f, 0.9f));
        }

        static string Suggest(GameState gs)
        {
            int ri = GameState.RankIndex(gs.rank);
            var l = new List<string>();
            if (gs.level < 5) l.Add("- Capai Level 5 di Hutan Pemula lalu pilih job.");
            else if (!gs.jobChosen) l.Add("- Pilih job di Guild.");
            if (ri <= 1) l.Add("- Ambil quest rank G/F di Guild; Hutan Lerai (gerbang selatan Ashwatch) cocok untuk pemula.");
            else if (ri <= 3) l.Add("- Coba Gurun Zahar (timur) atau Rawa Terkutuk (barat); atau jalan raya ke Eldrath lewat Hutan Perbatasan.");
            else if (ri <= 5) l.Add("- Ambil quest Guild Eldrath: Gunung Vulkanik, Padang Tulang, atau Menara Eldrath.");
            else l.Add("- Tantang Lembah Iblis (Zargoth) dan lantai atas Menara Eldrath.");
            if (gs.level >= 15 && string.IsNullOrEmpty(gs.evo)) l.Add("- Kamu bisa evolusi job Lv.15 di Skill Tree (K).");
            if (gs.level >= 30 && string.IsNullOrEmpty(gs.evo2)) l.Add("- Kamu bisa naik kelas Lv.30 di Skill Tree (K).");
            if (gs.skillPoints > 0) l.Add("- Ada " + gs.skillPoints + " Skill Point belum dipakai (K).");
            l.Add("- Cek Bestiary (B) untuk monster yang belum ditemukan.");
            return string.Join("\n", l.ToArray());
        }

        // ------------------------------------------------------------------ riwayat
        void DrawHistory()
        {
            var gs = GameState.I;
            var done = new List<GuildQuest>();
            foreach (var q in GuildQuestDB.Visible()) if (gs.Stat("qd_" + q.id) > 0) done.Add(q);
            Label(body, "Quest selesai: " + gs.Stat("quests_n") + "   (poin Guild: " + gs.questsDone + ")", 30, 14, 1270, 40, 26, TextAnchor.MiddleLeft, Gold);
            if (done.Count == 0) { Label(body, "Belum ada quest yang diselesaikan.", 30, 70, 1270, 40, 24, TextAnchor.MiddleLeft, Color.white); return; }
            const int per = 14;
            int pages = Mathf.Max(1, Mathf.CeilToInt(done.Count / (float)per));
            page = Mathf.Clamp(page, 0, pages - 1);
            for (int i = 0; i < per; i++)
            {
                int idx = page * per + i;
                if (idx >= done.Count) break;
                var q = done[idx];
                int n = gs.Stat("qd_" + q.id);
                Label(body, "[" + q.rank + "]  " + q.title + (n > 1 ? "   x" + n : ""), 30, 62 + i * 40, 900, 36, 22, TextAnchor.MiddleLeft, Color.white);
                Label(body, GuildQuest.RegionName(q.Region), 930, 62 + i * 40, 390, 36, 18, TextAnchor.MiddleRight, GoldDim);
            }
            Pager(pages);
        }

        void Pager(int pages)
        {
            if (pages <= 1) return;
            var p = UIKit.Btn(body, "<", () => { page--; Refresh(); }, page > 0, new Color(0.2f, 0.23f, 0.3f), 24);
            UIKit.At((RectTransform)p.transform, 30, 664, 90, 40);
            var n = UIKit.Btn(body, ">", () => { page++; Refresh(); }, page < pages - 1, new Color(0.2f, 0.23f, 0.3f), 24);
            UIKit.At((RectTransform)n.transform, 1216, 664, 90, 40);
            Label(body, "Hal. " + (page + 1) + "/" + pages, 130, 664, 1076, 40, 22, TextAnchor.MiddleCenter, Gold);
        }

        // ------------------------------------------------------------------ boss
        void DrawBosses()
        {
            var gs = GameState.I;
            var bosses = new List<MonsterDef>();
            foreach (var d in MonsterDB.All()) if (d.large || BossHint.ContainsKey(d.id)) bosses.Add(d);
            int defeated = 0;
            foreach (var d in bosses) if (gs.Stat("bk_" + d.id) > 0) defeated++;
            Label(body, "Boss & monster raksasa dikalahkan: " + defeated + " / " + bosses.Count, 30, 14, 1270, 40, 26, TextAnchor.MiddleLeft, Gold);
            const int per = 14;
            int pages = Mathf.Max(1, Mathf.CeilToInt(bosses.Count / (float)per));
            page = Mathf.Clamp(page, 0, pages - 1);
            for (int i = 0; i < per; i++)
            {
                int idx = page * per + i;
                if (idx >= bosses.Count) break;
                var d = bosses[idx];
                int k = gs.Stat("bk_" + d.id);
                string hint;
                if (!BossHint.TryGetValue(d.id, out hint)) hint = "";
                string line = k > 0 ? "[v]  " + d.name + "   (" + k + "x)" : "[ ]  " + (hint != "" ? d.name : "???");
                Label(body, line, 30, 62 + i * 40, 620, 36, 22, TextAnchor.MiddleLeft, k > 0 ? new Color(0.55f, 1f, 0.55f) : Color.white);
                Label(body, k > 0 ? "" : hint, 660, 62 + i * 40, 660, 36, 17, TextAnchor.MiddleLeft, GoldDim);
            }
            Pager(pages);
        }

        // ------------------------------------------------------------------ pencapaian
        void DrawAch()
        {
            var gs = GameState.I;
            int got = 0;
            foreach (var a in Achs) if (gs.flags.Contains("ach_" + a.key)) got++;
            Label(body, "Pencapaian diambil: " + got + " / " + Achs.Count, 30, 14, 800, 40, 26, TextAnchor.MiddleLeft, Gold);
            const int per = 7;
            int pages = Mathf.Max(1, Mathf.CeilToInt(Achs.Count / (float)per));
            page = Mathf.Clamp(page, 0, pages - 1);
            for (int i = 0; i < per; i++)
            {
                int idx = page * per + i;
                if (idx >= Achs.Count) break;
                var a = Achs[idx];
                int cur = Mathf.Min(a.cur(gs), a.target);
                bool taken = gs.flags.Contains("ach_" + a.key);
                bool ready = !taken && cur >= a.target;
                float y = 62 + i * 84;
                Box(body, 20, y, 1296, 78, new Color(0.14f, 0.15f, 0.2f));
                Label(body, a.name, 36, y + 4, 700, 36, 26, TextAnchor.MiddleLeft, taken ? new Color(0.55f, 1f, 0.55f) : Gold);
                Label(body, a.desc + "   (" + cur + " / " + a.target + ")", 36, y + 40, 800, 32, 19, TextAnchor.MiddleLeft, new Color(0.8f, 0.8f, 0.85f));
                Box(body, 860, y + 28, 220, 18, new Color(0.05f, 0.05f, 0.07f));
                Box(body, 860, y + 28, 220f * cur / Mathf.Max(1, a.target), 18, taken ? new Color(0.3f, 0.7f, 0.3f) : new Color(0.8f, 0.62f, 0.2f));
                string key = a.key; int gold = a.gold;
                var b = UIKit.Btn(body, taken ? "DIAMBIL" : ready ? "AMBIL " + gold + "g" : gold + "g", () => Claim(key, gold, a.name), ready, ready ? new Color(0.5f, 0.38f, 0.1f) : new Color(0.2f, 0.2f, 0.24f), 20);
                UIKit.At((RectTransform)b.transform, 1110, y + 14, 190, 50);
            }
            Pager(pages);
        }

        void Claim(string key, int gold, string name)
        {
            var gs = GameState.I;
            if (gs.flags.Contains("ach_" + key)) return;
            gs.flags.Add("ach_" + key);
            gs.gold += gold;
            Audio.Sfx("quest_reward");
            GameState.Say("Pencapaian: " + name + "  +" + gold + "g");
            Refresh();
        }

        /// <summary>Jumlah pencapaian yang siap diambil (untuk penanda di HUD, jika diperlukan).</summary>
        public static int ReadyCount()
        {
            var gs = GameState.I;
            if (gs == null) return 0;
            int n = 0;
            foreach (var a in Achs) if (!gs.flags.Contains("ach_" + a.key) && a.cur(gs) >= a.target) n++;
            return n;
        }
    }
}
