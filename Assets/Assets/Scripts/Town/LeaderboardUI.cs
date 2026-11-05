using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HunterGuild
{
    /// <summary>
    /// Leaderboard Guild bergaya emas-gelap: tab atas (Player/Level/Power/Guild/Hunting/Collection/Arena), sidebar kategori,
    /// tabel peringkat 10 baris per halaman, "Peringkat Saya", dan panel detail (profil, statistik, equipment, achievement).
    /// Pemain memakai data asli; Hunter NPC dihasilkan dari daftar Hunter Guild (deterministik per hari).
    /// </summary>
    public class LeaderboardUI : MonoBehaviour
    {
        public static LeaderboardUI I { get; private set; }
        public bool IsOpen { get { return root != null && root.gameObject.activeSelf; } }

        public static void Open(string board)
        {
            if (TownUI.I == null) return;
            if (I == null) new GameObject("LeaderboardUI").AddComponent<LeaderboardUI>();
            I.Show(board);
        }

        enum Mode { Overall, Level, Power, Job, Guild, Hunting, Collection, Achv, Arena, Tower }
        static readonly string[] SideNames = { "Overall", "Level", "Power", "Job Class", "Guild", "Hunting", "Collection", "Achievement", "Menara" };
        static readonly Mode[] SideModes = { Mode.Overall, Mode.Level, Mode.Power, Mode.Job, Mode.Guild, Mode.Hunting, Mode.Collection, Mode.Achv, Mode.Tower };
        static readonly string[] SideGlyph = { "#", "Lv", "P", "J", "G", "H", "C", "A", "T" };
        static readonly string[] TabNames = { "Player", "Level", "Power", "Guild", "Hunting", "Collection", "Arena" };
        static readonly Mode[] TabModes = { Mode.Overall, Mode.Level, Mode.Power, Mode.Guild, Mode.Hunting, Mode.Collection, Mode.Arena };
        static readonly string[] TabGlyph = { "P", "^", "X", "G", "H", "C", "A" };
        static readonly string[] Guilds = { "Astra", "Eclipse", "Stellaris", "Nemesis", "Valhalla", "Sanctum" };
        static readonly string[] Jobs = { "Swordsman", "Archer", "FireMage", "EarthMage", "WaterMage", "WindMage", "Priest" };
        static readonly string[] Extra = { "Raven", "Luneria", "Zyron", "Ardyn", "Mirelle", "Thorne", "Celest", "Nerith", "Darian", "Kaito", "Selene", "Orin", "Vexa", "Bryn", "Lyra", "Garrick", "Isolde", "Fenrik", "Talia", "Magnus",
            "Odette", "Rurik", "Sable", "Corwin", "Yara", "Dorian", "Ione", "Halvar", "Nyla", "Peregrine", "Ember", "Quill", "Rowan", "Tamsin", "Ulric", "Vesper", "Wynn", "Xander", "Ysolde", "Zephyr",
            "Alaric", "Briar", "Cassian", "Dahlia", "Edric", "Faye", "Gideon", "Hazel", "Ivor", "Juno" };

        class Row
        {
            public string name, job, cls, guild, rank;
            public int ri, level, power, hunting, collection, arena, achv, tower, exp, score;
            public bool me, rival;
        }

        class GRow { public string name, leader; public int members, power; public bool mine; }

        RectTransform root, tableHost, rightHost, mineHost;
        int themeVer = -1;
        Mode mode = Mode.Overall;
        int page;
        Row sel;
        GRow gsel;
        readonly List<Row> roster = new List<Row>();
        List<Row> sorted = new List<Row>();
        List<GRow> gsorted = new List<GRow>();
        Row mine;
        int rosterDay = -1;
        int openedFrame;

        void Awake() { I = this; }
        void OnDestroy() { if (I == this) I = null; }

        void Update()
        {
            if (IsOpen && TownUI.I != null && !TownUI.I.PanelOpen) Hide();
        }

        public void Show(string board)
        {
            var tui = TownUI.I;
            if (tui == null) return;
            tui.Close();
            mode = board == "tower" ? Mode.Tower : (board == "forest" || board == "desert" || board == "swamp") ? Mode.Hunting : Mode.Overall;
            page = 0; sel = null; gsel = null;
            BuildRoster();
            if (UiStyle.Stale(ref themeVer) && root != null) { Destroy(root.gameObject); root = null; }
            if (root == null) Build();
            root.gameObject.SetActive(true);
            tui.SetExternalOpen(true);
            openedFrame = Time.frameCount;
            Rebuild();
        }

        public void Hide()
        {
            if (root != null) root.gameObject.SetActive(false);
            if (TownUI.I != null) TownUI.I.SetExternalOpen(false);
        }

        // ------------------------------------------------------------ data
        static int H(string s) { unchecked { int h = 17; foreach (char c in s) h = h * 31 + c; return Mathf.Abs(h); } }

        string ClassFor(string job, int level, int seed)
        {
            if (level >= JobInfo.Evo2Level)
            {
                var e1 = JobInfo.EvosOf(job);
                if (e1.Count > 0)
                {
                    var a = e1[seed % e1.Count];
                    var e2 = JobInfo.Evos2Of(a.id);
                    if (e2.Count > 0) return e2[(seed / 3) % e2.Count].name;
                    return a.name;
                }
            }
            if (level >= JobInfo.EvoLevel) { var e = JobInfo.EvosOf(job); if (e.Count > 0) return e[seed % e.Count].name; }
            return JobInfo.Name(job);
        }

        void BuildRoster()
        {
            var gs = GameState.I;
            if (gs == null) return;
            roster.Clear();
            var gd = GuildData.I;
            var baseList = gd.BuildRanking();
            int day = Mathf.Max(1, gs.day);
            int n = 0;
            foreach (var e in baseList)
            {
                if (e.isPlayer) continue;
                roster.Add(MakeNpc(e.name, GameState.RankIndex(e.rank), e.quests, day, e.isRival, n++));
            }
            // v35z102n31: pemburu rekaan (daftar Extra) dihapus; hanya hunter bernama dari data Guild dan dirimu sendiri.
            mine = MakePlayer(gs);
        }

        Row MakeNpc(string name, int ri, int quests, int day, bool rival, int idx)
        {
            int h = H(name);
            var r = new Row { name = name, ri = ri, rival = rival, rank = GameState.Ranks[Mathf.Clamp(ri, 0, GameState.Ranks.Length - 1)] };
            r.level = Mathf.Clamp(4 + ri * 9 + h % 7 + day / 14, 1, 99);
            r.job = Jobs[h % Jobs.Length];
            r.cls = ClassFor(r.job, r.level, h / 5);
            r.guild = rival ? "Wanderer" : Guilds[(h / 11) % Guilds.Length];
            r.power = r.level * 120 + ri * 1700 + h % 900 + quests * 4;
            r.exp = r.level * r.level * 38 + h % 500;
            r.hunting = quests * 3 + ri * 40 + h % 60;
            r.collection = 6 + r.level / 2 + h % 12;
            r.arena = ri * 4 + h % 14;
            r.achv = Mathf.Clamp(ri / 2 + h % 2, 0, 5);
            r.tower = Mathf.Clamp(ri * 10 + h % 9, 0, 100);
            return r;
        }

        static bool[] Achieve(GameState gs)
        {
            // v35z102n22: tersambung ke sistem Pencapaian (30 buah); tiap cincin = 6 pencapaian terbuka
            int n = Achievements.UnlockedCount(gs), step = Mathf.Max(1, Achievements.Total / 5);
            return new[] { n >= step, n >= step * 2, n >= step * 3, n >= step * 4, n >= Achievements.Total };
        }

        Row MakePlayer(GameState gs)
        {
            var cd = CharacterData.Current;
            var r = new Row { me = true, name = string.IsNullOrEmpty(cd.name) ? "Kamu" : cd.name, ri = GameState.RankIndex(gs.rank), rank = gs.rank };
            r.level = gs.level; r.job = gs.job; r.cls = gs.NoJob ? "Tanpa Job" : JobInfo.Display(gs);
            r.guild = "Ashwatch";
            float hp = PlayerCombat.I != null ? PlayerCombat.I.MaxHp : 100f + 3f * Mathf.Max(0, gs.level - 1);
            r.power = Mathf.RoundToInt(gs.TotalAtk * 18f + gs.TotalDef * 14f + hp * 3f + gs.level * 120f);
            r.exp = gs.level * gs.level * 38 + gs.exp;
            r.hunting = gs.Stat("kills");
            r.collection = gs.inventory.Count + LoreDB.UnlockedCount;
            r.arena = gs.Stat("arena_wins");
            int a = 0; foreach (var b in Achieve(gs)) if (b) a++;
            r.achv = a;
            r.tower = gs.dungeonBest;
            return r;
        }

        static int Metric(Row r, Mode m)
        {
            switch (m)
            {
                case Mode.Level: return r.exp;
                case Mode.Power: case Mode.Job: return r.power;
                case Mode.Hunting: return r.hunting;
                case Mode.Collection: return r.collection;
                case Mode.Achv: return r.achv * 1000 + r.level;
                case Mode.Arena: return r.arena;
                case Mode.Tower: return r.tower;
                default: return r.power + r.level * 200 + r.hunting * 3 + r.collection * 15 + r.arena * 40 + r.achv * 80;
            }
        }

        static string MetricName(Mode m)
        {
            switch (m)
            {
                case Mode.Level: return "EXP";
                case Mode.Hunting: return "Buruan";
                case Mode.Collection: return "Koleksi";
                case Mode.Achv: return "Prestasi";
                case Mode.Arena: return "Menang";
                case Mode.Tower: return "Lantai";
                case Mode.Overall: return "Skor";
                default: return "Power";
            }
        }

        static string MetricText(Row r, Mode m)
        {
            if (m == Mode.Achv) return r.achv + " / 5";
            return GoldUI.Num(Metric(r, m));
        }

        void Resort()
        {
            var all = new List<Row>(roster); all.Add(mine);
            gsorted.Clear();
            if (mode == Mode.Guild)
            {
                var map = new Dictionary<string, GRow>();
                foreach (var r in all)
                {
                    GRow g;
                    if (!map.TryGetValue(r.guild, out g)) { g = new GRow { name = r.guild, mine = r.guild == mine.guild }; map[r.guild] = g; }
                    g.members++; g.power += r.power;
                    if (g.leader == null || r.power > 0 && r.power >= Leader(all, g.name)) g.leader = r.name;
                }
                foreach (var kv in map) gsorted.Add(kv.Value);
                gsorted.Sort((a, b) => b.power.CompareTo(a.power));
                sorted = new List<Row>();
                if (gsel == null && gsorted.Count > 0) gsel = gsorted[0];
                return;
            }
            if (mode == Mode.Job) all = all.FindAll(r => r.job == mine.job);
            Mode m = mode;
            all.Sort((a, b) => { int c = Metric(b, m).CompareTo(Metric(a, m)); return c != 0 ? c : a.me ? -1 : b.me ? 1 : string.Compare(a.name, b.name, StringComparison.Ordinal); });
            sorted = all;
            if (sel == null || !sorted.Contains(sel)) sel = sorted.Count > 0 ? sorted[0] : mine;
        }

        static int Leader(List<Row> all, string guild) { int best = 0; foreach (var r in all) if (r.guild == guild && r.power > best) best = r.power; return best; }

        // ------------------------------------------------------------ build
        void Build()
        {
            root = GoldUI.Window(TownUI.I.Root, "LeaderboardWindow", "LEADERBOARD", "T", Hide);
            tableHost = UIKit.Rect("TableHost", root); Fill(tableHost);
            rightHost = UIKit.Rect("RightHost", root); Fill(rightHost);
            mineHost = UIKit.Rect("MineHost", root); Fill(mineHost);
            root.gameObject.SetActive(false);
        }

        static void Fill(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }

        void Rebuild()
        {
            Resort();
            // bersihkan semuanya kecuali judul/tombol X (anak pertama root): host dibangun ulang penuh
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var c = root.GetChild(i);
                if (c.name == "TableHost" || c.name == "RightHost" || c.name == "MineHost" || c.name == "Dyn") { c.SetParent(null); Destroy(c.gameObject); }
            }
            var dyn = UIKit.Rect("Dyn", root); Fill(dyn);
            BuildTabs(dyn);
            BuildSidebar(dyn);
            BuildTable(dyn);
            BuildMine(dyn);
            BuildRight(dyn);
        }

        void SetMode(Mode m) { mode = m; page = 0; sel = null; gsel = null; Rebuild(); }

        void BuildTabs(Transform p)
        {
            var strip = GoldUI.Box(p, 14f, 88f, 1508f, 78f);
            for (int i = 0; i < TabNames.Length; i++)
            {
                var tm = TabModes[i];
                GoldUI.Tab(p, TabNames[i], TabGlyph[i], 22f + i * 214f, 94f, 206f, 66f, tm == mode, () => SetMode(tm));
            }
        }

        void BuildSidebar(Transform p)
        {
            GoldUI.Box(p, 14f, 184f, 216f, 664f);
            for (int i = 0; i < SideNames.Length; i++)
            {
                var sm = SideModes[i];
                bool on = sm == mode;
                var im = UiStyle.Plate("Side", p); im.raycastTarget = true;
                im.color = on ? new Color(1f, 0.82f, 0.4f) : new Color(0.7f, 0.7f, 0.76f);
                UIKit.At(im.rectTransform, 24f, 194f + i * 72f, 196f, 66f);
                var b = im.gameObject.AddComponent<Button>(); b.targetGraphic = im;
                GoldUI.Ring(im.transform, SideGlyph[i], 10f, 12f, 42f, on ? new Color(0.2f, 0.12f, 0.02f) : UiStyle.Gold);
                GoldUI.Label(im.transform, SideNames[i], 24, on ? new Color(1f, 0.97f, 0.88f) : UiStyle.Cream, 62f, 0f, 130f, 66f, TextAnchor.MiddleLeft);
                b.onClick.AddListener(() => { Audio.Sfx("click"); SetMode(sm); });
            }
        }

        string[] Headers()
        {
            if (mode == Mode.Guild) return new[] { "Rank", "Guild", "Anggota", "Pemimpin", "Power" };
            return new[] { "Rank", "Player", "Lv", "Class", "Guild", MetricName(mode) };
        }

        int PageCount { get { int n = mode == Mode.Guild ? gsorted.Count : sorted.Count; return Mathf.Max(1, (n + 9) / 10); } }

        void BuildTable(Transform p)
        {
            GoldUI.Box(p, 238f, 184f, 824f, 702f);
            // header kolom
            float[] cx = { 296f, 430f, 556f, 668f, 830f, 985f };
            string[] hd = Headers();
            if (mode == Mode.Guild) { cx = new[] { 296f, 470f, 650f, 790f, 970f }; }
            for (int i = 0; i < hd.Length; i++)
                GoldUI.Label(p, hd[i], 24, UiStyle.Cream, cx[i] - 60f, 198f, 120f, 34f, TextAnchor.MiddleCenter);
            GoldUI.Divider(p, 252f, 232f, 796f);

            int total = mode == Mode.Guild ? gsorted.Count : sorted.Count;
            page = Mathf.Clamp(page, 0, PageCount - 1);
            for (int i = 0; i < 10; i++)
            {
                int idx = page * 10 + i;
                if (idx >= total) break;
                float y = 242f + i * 60.5f;
                if (mode == Mode.Guild) GuildRow(p, gsorted[idx], idx, y, cx);
                else PlayerRow(p, sorted[idx], idx, y, cx, false);
            }
            // pager
            GoldUI.Gold(p, "<", 520f, 848f, 44f, 34f, page > 0, () => { page--; Rebuild(); }, false);
            GoldUI.Label(p, (page + 1) + " / " + PageCount, 24, UiStyle.Cream, 570f, 848f, 160f, 34f, TextAnchor.MiddleCenter);
            GoldUI.Gold(p, ">", 736f, 848f, 44f, 34f, page < PageCount - 1, () => { page++; Rebuild(); }, false);
        }

        static Color BadgeCol(int rank)
        {
            return rank == 0 ? new Color(1f, 0.82f, 0.3f) : rank == 1 ? new Color(0.6f, 0.75f, 1f) : rank == 2 ? new Color(0.95f, 0.5f, 0.25f) : UiStyle.Cream;
        }

        void RankBadge(Transform p, int idx, float x, float y)
        {
            if (idx < 3)
            {
                GoldUI.Ring(p, (idx + 1).ToString(), x - 22f, y + 4f, 44f, BadgeCol(idx));
            }
            else GoldUI.Label(p, (idx + 1).ToString(), 28, UiStyle.Cream, x - 40f, y, 80f, 52f, TextAnchor.MiddleCenter);
        }

        void PlayerRow(Transform p, Row r, int idx, float y, float[] cx, bool compact)
        {
            bool on = r == sel;
            var bg = UiStyle.Plate("Row", p); bg.raycastTarget = true;
            bg.color = on ? new Color(0.95f, 0.72f, 0.25f, 0.55f) : (idx % 2 == 0 ? new Color(0.09f, 0.1f, 0.14f, 0.9f) : new Color(0.06f, 0.07f, 0.1f, 0.6f));
            if (r.me && !on) bg.color = new Color(0.2f, 0.17f, 0.08f, 0.9f);
            UIKit.At(bg.rectTransform, 252f, y, 796f, 56f);
            var b = bg.gameObject.AddComponent<Button>(); b.targetGraphic = bg;
            var row = r;
            b.onClick.AddListener(() => { Audio.Sfx("click"); sel = row; Rebuild(); });
            RowContent(p, r, idx, y, cx);
        }

        void RowContent(Transform p, Row r, int idx, float y, float[] cx)
        {
            RankBadge(p, idx, cx[0], y);
            var pf = UiStyle.Frame("Pt", p); UIKit.At(pf.rectTransform, cx[1] - 100f, y + 2f, 60f, 52f);
            DialogueUI.BuildPortrait(pf.transform, 60f, 52f, r.name, null);
            GoldUI.Label(p, r.name, 24, r.me ? UiStyle.Gold : UiStyle.Cream, cx[1] - 32f, y, 150f, 56f, TextAnchor.MiddleLeft);
            GoldUI.Label(p, r.level.ToString(), 24, UiStyle.Cream, cx[2] - 40f, y, 80f, 56f, TextAnchor.MiddleCenter);
            GoldUI.Ring(p, r.cls.Substring(0, 1), cx[3] - 62f, y + 10f, 36f, UiStyle.Muted);
            GoldUI.Label(p, r.cls, 21, UiStyle.Cream, cx[3] - 22f, y, 140f, 56f, TextAnchor.MiddleLeft);
            var gc = Color.HSVToRGB((H(r.guild) % 100) / 100f, 0.5f, 0.95f);
            GoldUI.Ring(p, r.guild.Substring(0, 1), cx[4] - 62f, y + 10f, 36f, gc);
            GoldUI.Label(p, r.guild, 21, UiStyle.Cream, cx[4] - 22f, y, 120f, 56f, TextAnchor.MiddleLeft);
            GoldUI.Label(p, MetricText(r, mode), 24, UiStyle.Cream, cx[5] - 70f, y, 140f, 56f, TextAnchor.MiddleCenter);
        }

        void GuildRow(Transform p, GRow g, int idx, float y, float[] cx)
        {
            bool on = g == gsel;
            var bg = UiStyle.Plate("Row", p); bg.raycastTarget = true;
            bg.color = on ? new Color(0.95f, 0.72f, 0.25f, 0.55f) : (idx % 2 == 0 ? new Color(0.09f, 0.1f, 0.14f, 0.9f) : new Color(0.06f, 0.07f, 0.1f, 0.6f));
            UIKit.At(bg.rectTransform, 252f, y, 796f, 56f);
            var b = bg.gameObject.AddComponent<Button>(); b.targetGraphic = bg;
            var gg = g;
            b.onClick.AddListener(() => { Audio.Sfx("click"); gsel = gg; Rebuild(); });
            RankBadge(p, idx, cx[0], y);
            var gc = Color.HSVToRGB((H(g.name) % 100) / 100f, 0.5f, 0.95f);
            GoldUI.Ring(p, g.name.Substring(0, 1), cx[1] - 100f, y + 8f, 40f, gc);
            GoldUI.Label(p, g.name + (g.mine ? "  (kamu)" : ""), 24, g.mine ? UiStyle.Gold : UiStyle.Cream, cx[1] - 50f, y, 190f, 56f, TextAnchor.MiddleLeft);
            GoldUI.Label(p, g.members.ToString(), 24, UiStyle.Cream, cx[2] - 40f, y, 80f, 56f, TextAnchor.MiddleCenter);
            GoldUI.Label(p, g.leader ?? "-", 22, UiStyle.Cream, cx[3] - 70f, y, 150f, 56f, TextAnchor.MiddleCenter);
            GoldUI.Label(p, GoldUI.Num(g.power), 24, UiStyle.Cream, cx[4] - 70f, y, 140f, 56f, TextAnchor.MiddleCenter);
        }

        void BuildMine(Transform p)
        {
            GoldUI.Box(p, 238f, 894f, 824f, 120f);
            GoldUI.Label(p, "Peringkat Saya", 22, UiStyle.Gold, 262f, 900f, 300f, 30f, TextAnchor.MiddleLeft);
            GoldUI.Divider(p, 252f, 928f, 796f);
            if (mode == Mode.Guild)
            {
                int gi = gsorted.FindIndex(g => g.mine);
                GoldUI.Label(p, gi >= 0 ? "Guild " + mine.guild + " di peringkat " + (gi + 1) : "Belum bergabung dengan guild", 26, UiStyle.Cream, 270f, 940f, 760f, 60f, TextAnchor.MiddleLeft);
                return;
            }
            int rk = sorted.IndexOf(mine);
            float[] cx = { 296f, 430f, 556f, 668f, 830f, 985f };
            if (rk < 0)
            {
                GoldUI.Label(p, "Kamu belum masuk kategori ini (Job Class berbeda).", 24, UiStyle.Muted, 270f, 940f, 760f, 60f, TextAnchor.MiddleLeft);
                return;
            }
            var mm = mine;
            var bg = UIKit.Img("MineBtn", p, new Color(0, 0, 0, 0.01f));
            UIKit.At(bg.rectTransform, 252f, 936f, 796f, 70f);
            var b = bg.gameObject.AddComponent<Button>(); b.targetGraphic = bg;
            b.onClick.AddListener(() => { Audio.Sfx("click"); sel = mm; Rebuild(); });
            RowContent(p, mine, rk, 942f, cx);
        }

        // ------------------------------------------------------------ panel kanan
        void BuildRight(Transform p)
        {
            if (mode == Mode.Guild) { GuildDetail(p); return; }
            var r = sel ?? mine;
            // profil
            GoldUI.Box(p, 1070f, 184f, 456f, 296f);
            int rk = sorted.IndexOf(r);
            GoldUI.Ring(p, (rk + 1).ToString(), 1088f, 206f, 84f, rk < 3 ? BadgeCol(rk) : UiStyle.Gold);
            var pf = UiStyle.Frame("Big", p); UIKit.At(pf.rectTransform, 1170f, 196f, 190f, 274f);
            DialogueUI.BuildPortrait(pf.transform, 190f, 274f, r.name, null);
            GoldUI.Label(p, r.name, 36, UiStyle.Gold, 1372f, 200f, 150f, 44f, TextAnchor.MiddleLeft, true);
            GoldUI.Label(p, "Lv. " + r.level, 24, UiStyle.Cream, 1372f, 250f, 150f, 30f, TextAnchor.MiddleLeft);
            GoldUI.Label(p, r.cls, 22, UiStyle.Cream, 1372f, 284f, 150f, 30f, TextAnchor.MiddleLeft);
            GoldUI.Label(p, "Guild", 18, UiStyle.Muted, 1372f, 326f, 150f, 22f, TextAnchor.MiddleLeft);
            GoldUI.Label(p, r.guild, 24, UiStyle.Cream, 1372f, 346f, 150f, 30f, TextAnchor.MiddleLeft);
            GoldUI.Label(p, "Power", 18, UiStyle.Gold, 1372f, 392f, 150f, 22f, TextAnchor.MiddleLeft);
            GoldUI.Label(p, GoldUI.Num(r.power), 32, UiStyle.Gold, 1372f, 414f, 150f, 40f, TextAnchor.MiddleLeft, true);

            // statistik
            GoldUI.Box(p, 1070f, 488f, 456f, 196f);
            GoldUI.Label(p, "Detail Statistik", 22, UiStyle.Gold, 1090f, 494f, 300f, 30f, TextAnchor.MiddleLeft);
            GoldUI.Divider(p, 1084f, 522f, 428f);
            string[] sn, sv; Stats(r, out sn, out sv);
            for (int i = 0; i < 8; i++)
            {
                int col = i / 4, rw = i % 4;
                float x = 1090f + col * 210f, y = 536f + rw * 36f;
                GoldUI.Label(p, sn[i], 19, UiStyle.Cream, x, y, 130f, 32f, TextAnchor.MiddleLeft);
                GoldUI.Label(p, sv[i], 19, UiStyle.Cream, x + 100f, y, 94f, 32f, TextAnchor.MiddleRight);
            }

            // equipment
            GoldUI.Box(p, 1070f, 692f, 456f, 156f);
            GoldUI.Label(p, "Equipment Utama", 22, UiStyle.Gold, 1090f, 698f, 300f, 30f, TextAnchor.MiddleLeft);
            string[] slots = { "Weapon", "Head", "Body", "Hands", "Legs" };
            for (int i = 0; i < 5; i++) GearSlot(p, r, slots[i], 1090f + i * 84f, 732f);

            // achievement
            GoldUI.Box(p, 1070f, 856f, 456f, 158f);
            GoldUI.Label(p, r.me ? "Achievement  " + Achievements.UnlockedCount(GameState.I) + "/" + Achievements.Total : "Achievement", 22, UiStyle.Gold, 1090f, 862f, 300f, 30f, TextAnchor.MiddleLeft);
            bool[] got = new bool[5];
            if (r.me) got = Achieve(GameState.I); else for (int i = 0; i < 5; i++) got[i] = i < r.achv;
            Color[] mc = { new Color(0.95f, 0.75f, 0.3f), new Color(0.35f, 0.6f, 0.95f), new Color(0.6f, 0.4f, 0.9f), new Color(0.9f, 0.3f, 0.25f), new Color(0.4f, 0.75f, 0.35f) };
            for (int i = 0; i < 5; i++)
            {
                Color c = got[i] ? mc[i] : new Color(0.3f, 0.3f, 0.34f);
                GoldUI.Ring(p, got[i] ? "*" : "?", 1096f + i * 84f, 904f, 70f, c);
            }
        }

        void Stats(Row r, out string[] n, out string[] v)
        {
            n = new[] { "HP", "ATK", "DEF", "CRIT Rate", "Stamina", "Move SPD", "Elemental", "Dmg Reduc." };
            var gs = GameState.I;
            if (r.me)
            {
                float hp = PlayerCombat.I != null ? PlayerCombat.I.MaxHp : 100f + 3f * Mathf.Max(0, gs.level - 1);
                v = new[] { GoldUI.Num(Mathf.RoundToInt(hp)), GoldUI.Num(gs.TotalAtk), GoldUI.Num(gs.TotalDef), Mathf.RoundToInt(gs.Pas("crit")) + "%", "100",
                            (100 + Mathf.RoundToInt(gs.Pas("spd") * 100f)) + "%", "0%", Mathf.RoundToInt(gs.Pas("dmgred") * 100f) + "%" };
                return;
            }
            int h = H(r.name);
            v = new[] { GoldUI.Num(r.level * 95 + h % 400), GoldUI.Num(r.power / 8), GoldUI.Num(r.power / 11), (5 + r.ri * 3) + "%", (100 + r.ri * 15).ToString(),
                        (100 + r.ri) + "%", (r.ri * 2) + "%", Mathf.RoundToInt(r.ri * 1.5f) + "%" };
        }

        void GearSlot(Transform p, Row r, string slot, float x, float y)
        {
            string id = null; int enc = 0;
            var gs = GameState.I;
            if (r.me) { string v; if (gs.equipment.TryGetValue(slot, out v)) id = v; if (id != null) enc = GearTiers.EnchantLevel(gs, id); }
            else
            {
                int a = Mathf.Clamp(r.ri / 2, 0, 3);
                if (r.ri >= 1)
                    id = slot == "Weapon" ? "gw_" + a + "_" + r.job : "ga_" + a + "_" + slot;
                else id = slot == "Head" ? "leather_cap" : slot == "Body" ? "leather_armor" : slot == "Legs" ? "leather_boots" : slot == "Hands" ? "leather_gloves" : null;
                enc = r.ri * 2 + H(r.name + slot) % 3;
            }
            var fr = UiStyle.Plate("Slot", p); fr.color = new Color(0.14f, 0.13f, 0.2f, 1f);
            UIKit.At(fr.rectTransform, x, y, 76f, 100f);
            var def = id != null ? ItemDB.Get(id) : null;
            if (def != null)
            {
                var ic = UIKit.Img("Ic", fr.transform, Color.white); ic.sprite = ItemIcons.For(def); ic.preserveAspect = true; ic.raycastTarget = false;
                UIKit.At(ic.rectTransform, 8f, 8f, 60f, 60f);
                GoldUI.Label(fr.transform, "+" + enc, 20, UiStyle.Cream, 0f, 70f, 76f, 26f, TextAnchor.MiddleCenter);
            }
            else GoldUI.Label(fr.transform, "-", 30, UiStyle.Muted, 0f, 0f, 76f, 100f, TextAnchor.MiddleCenter);
        }

        void GuildDetail(Transform p)
        {
            var g = gsel ?? (gsorted.Count > 0 ? gsorted[0] : null);
            GoldUI.Box(p, 1070f, 184f, 456f, 830f);
            if (g == null) return;
            int rk = gsorted.IndexOf(g);
            var gc = Color.HSVToRGB((H(g.name) % 100) / 100f, 0.5f, 0.95f);
            GoldUI.Ring(p, (rk + 1).ToString(), 1090f, 206f, 84f, rk < 3 ? BadgeCol(rk) : UiStyle.Gold);
            GoldUI.Ring(p, g.name.Substring(0, 1), 1210f, 206f, 150f, gc);
            GoldUI.Label(p, g.name, 40, UiStyle.Gold, 1090f, 380f, 420f, 54f, TextAnchor.MiddleCenter, true);
            GoldUI.Divider(p, 1090f, 440f, 416f);
            GoldUI.Label(p, "Anggota", 22, UiStyle.Muted, 1110f, 460f, 200f, 30f, TextAnchor.MiddleLeft);
            GoldUI.Label(p, g.members.ToString(), 28, UiStyle.Cream, 1300f, 456f, 200f, 36f, TextAnchor.MiddleRight);
            GoldUI.Label(p, "Pemimpin", 22, UiStyle.Muted, 1110f, 506f, 200f, 30f, TextAnchor.MiddleLeft);
            GoldUI.Label(p, g.leader ?? "-", 28, UiStyle.Cream, 1300f, 502f, 200f, 36f, TextAnchor.MiddleRight);
            GoldUI.Label(p, "Total Power", 22, UiStyle.Gold, 1110f, 552f, 200f, 30f, TextAnchor.MiddleLeft);
            GoldUI.Label(p, GoldUI.Num(g.power), 32, UiStyle.Gold, 1260f, 546f, 240f, 40f, TextAnchor.MiddleRight, true);
            GoldUI.Label(p, g.mine ? "Guild kamu. Kumpulkan Power untuk naik peringkat." : "Guild Hunter dari kota lain yang bersaing di papan peringkat Ashwatch.", 22, UiStyle.Cream, 1100f, 620f, 400f, 120f, TextAnchor.UpperLeft);
        }
    }
}
