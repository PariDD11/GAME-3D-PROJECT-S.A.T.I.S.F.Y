using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HunterGuild
{
    /// <summary>
    /// Skill Tree (tombol K) bergaya panel gelap-emas: daftar kelas di kiri, tab cabang + baris tier berisi node bulat di tengah,
    /// panel detail skill (efek, preview, syarat) di kanan. Tab ke-4 = Evolusi Job (Level 15).
    /// </summary>
    public class SkillUI : MonoBehaviour
    {
        public static SkillUI I { get; private set; }
        public bool IsOpen { get { return root != null && root.activeSelf; } }

        static readonly string[] SlotKeys = { "Z", "X", "C", "V", "R", "T", "Q" };
        static readonly int[] TierLv = { 1, 10, 20 };
        // warna mengikuti tema job (UiStyle.Cur)
        static Color Gold { get { return UiStyle.Cur.Gold; } }
        static Color GoldDim { get { return UiStyle.Cur.GoldDim; } }
        static Color Panel { get { var t = UiStyle.Cur; return new Color(t.PanelLo.r * 1.4f + 0.02f, t.PanelLo.g * 1.4f + 0.02f, t.PanelLo.b * 1.4f + 0.025f, 0.97f); } }
        static Color PanelIn { get { var t = UiStyle.Cur; return Color.Lerp(t.PanelLo, t.PanelHi, 0.45f); } }
        int themeVer = -1;

        GameObject root;
        RectTransform leftRoot, centerRoot, rightRoot;
        string selSkill, evoPick;
        int tab, openedFrame = -1;
        bool confirmReset, showRec;
        readonly List<Image> glows = new List<Image>();
        Image pvDot, pvRing, pvGlow; string pvKind = "", pvElem = "";
        static Sprite circle;

        void Awake() { I = this; }
        void OnDestroy() { if (I == this) I = null; }

        void Update()
        {
            if (!IsOpen) return;
            if (Time.frameCount != openedFrame && TownInput.SkillMenuDown()) { Hide(); return; }
            float t = Time.unscaledTime;
            foreach (var g in glows) if (g != null) { var c = g.color; c.a = 0.28f + 0.2f * Mathf.Sin(t * 3f); g.color = c; }
            AnimatePreview(t);
        }

        public void Show()
        {
            var gs = GameState.I;
            if (!gs.jobChosen) { TownUI.I.Toast("Pilih job dulu."); JobMenu.Open(); return; }
            if (UiStyle.Stale(ref themeVer) && root != null) { Destroy(root); root = null; }
            if (root == null) Build();
            root.SetActive(true);
            openedFrame = Time.frameCount;
            TownUI.I.SetExternalOpen(true);
            if (gs.CanEvolve) tab = SkillDB.Branches(gs.job).Length; else if (gs.CanEvolve2) tab = SkillDB.Branches(gs.job).Length + 1;
            Refresh();
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
            if (TownUI.I != null) TownUI.I.SetExternalOpen(false);
        }

        // ------------------------------------------------------------------ util gambar
        static Sprite Circle()
        {
            if (circle != null) return circle;
            const int n = 96;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f));
                float a = Mathf.Clamp01((n / 2f - 1f - d) / 1.5f);
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            tex.Apply();
            circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            return circle;
        }

        static Image Disc(Transform parent, string name, float cx, float cy, float size, Color col)
        {
            var im = UIKit.Img(name, parent, col);
            im.sprite = Circle();
            im.raycastTarget = false;
            UIKit.Place(im.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(cx, -cy), new Vector2(size, size));
            return im;
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

        static void Line(Transform parent, Vector2 a, Vector2 b, Color col, float w)
        {
            var im = UIKit.Img("Line", parent, col);
            im.raycastTarget = false;
            Vector2 mid = (a + b) / 2f, d = b - a;
            UIKit.Place(im.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(mid.x, -mid.y), new Vector2(d.magnitude, w));
            im.rectTransform.localRotation = Quaternion.Euler(0, 0, -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }

        static void LockIcon(Transform parent, float cx, float cy, float s, Color col)
        {
            Box(parent, cx - 9 * s, cy - 2 * s, 18 * s, 14 * s, col);
            Box(parent, cx - 6 * s, cy - 11 * s, 3 * s, 10 * s, col);
            Box(parent, cx + 3 * s, cy - 11 * s, 3 * s, 10 * s, col);
            Box(parent, cx - 6 * s, cy - 12 * s, 12 * s, 3 * s, col);
        }

        static Image Window(Transform parent, float x, float y, float w, float h, Color fill, Color border)
        {
            var b = Box(parent, x, y, w, h, border);
            var f = Box(b.transform, 2, 2, w - 4, h - 4, fill);
            return f;
        }

        static Color ElemTint(SkillDef s, string job)
        {
            string e = !string.IsNullOrEmpty(s.elem) ? s.elem : JobInfo.Elem(job);
            if (e == "phys" || e == "arrow" || e == "") return JobInfo.JobColor(job);
            return JobInfo.ElemColor(e);
        }

        // ------------------------------------------------------------------ bangun jendela
        void Build()
        {
            var bg = Window(TownUI.I.Root, 0, 0, 1560, 900, Panel, Gold);
            var outer = bg.transform.parent.GetComponent<RectTransform>();
            var c = new Vector2(0.5f, 0.5f);
            UIKit.Place(outer, c, c, c, Vector2.zero, new Vector2(1560, 900));
            root = outer.gameObject;
            var t = Label(bg.transform, "SKILL TREE", 0, 14, 1556, 44, 34, TextAnchor.MiddleCenter, Gold);
            Box(bg.transform, 40, 64, 1476, 2, GoldDim);
            var close = UIKit.Btn(bg.transform, "X", Hide, true, new Color(0.4f, 0.2f, 0.2f), 24);
            UIKit.At((RectTransform)close.transform, 1490, 14, 50, 44);

            leftRoot = UIKit.Rect("Left", bg.transform); UIKit.At(leftRoot, 18, 78, 300, 800);
            centerRoot = UIKit.Rect("Center", bg.transform); UIKit.At(centerRoot, 332, 78, 790, 800);
            rightRoot = UIKit.Rect("Right", bg.transform); UIKit.At(rightRoot, 1136, 78, 410, 800);
            root.SetActive(false);
        }

        void Refresh()
        {
            var gs = GameState.I;
            glows.Clear(); pvDot = null; pvRing = null; pvGlow = null;
            UIKit.Clear(leftRoot); UIKit.Clear(centerRoot); UIKit.Clear(rightRoot);
            DrawLeft(gs); DrawCenter(gs); DrawRight(gs);
        }

        // ------------------------------------------------------------------ kiri: kelas
        void DrawLeft(GameState gs)
        {
            Window(leftRoot, 0, 0, 300, 800, PanelIn, GoldDim);
            Label(leftRoot, "KELAS", 16, 8, 270, 30, 22, TextAnchor.MiddleLeft, Gold);
            for (int i = 0; i < JobInfo.All.Length; i++)
            {
                string j = JobInfo.All[i];
                bool cur = j == gs.job;
                float y = 44 + i * 52;
                var row = Window(leftRoot, 12, y, 276, 46, cur ? new Color(0.22f, 0.17f, 0.07f, 1f) : new Color(0.08f, 0.08f, 0.1f, 1f), cur ? Gold : new Color(0.25f, 0.25f, 0.3f));
                Disc(row.transform, "Dot", 24, 21, 22, cur ? JobInfo.JobColor(j) : new Color(0.25f, 0.25f, 0.28f));
                Label(row.transform, JobInfo.Name(j), 46, 0, 190, 42, 19, TextAnchor.MiddleLeft, cur ? Color.white : new Color(0.5f, 0.5f, 0.55f));
                if (!cur) LockIcon(row.transform, 250, 20, 0.9f, new Color(0.5f, 0.5f, 0.55f));
            }
            Label(leftRoot, "INFO CLASS", 16, 418, 270, 28, 20, TextAnchor.MiddleLeft, Gold);
            string info = JobInfo.Info(gs.job);
            if (!string.IsNullOrEmpty(gs.evo)) info = "Evolusi: " + JobInfo.EvoName(gs.evo) + (string.IsNullOrEmpty(gs.evo2) ? "" : " > " + JobInfo.EvoName(gs.evo2)) + "\n" + info;
            Label(leftRoot, info, 16, 448, 268, 200, 16, TextAnchor.UpperLeft, new Color(0.85f, 0.85f, 0.88f));
            Box(leftRoot, 16, 664, 268, 2, GoldDim);
            Label(leftRoot, "SKILL POINT", 16, 674, 150, 28, 18, TextAnchor.MiddleLeft, Gold);
            Label(leftRoot, gs.skillPoints.ToString(), 150, 668, 130, 40, 32, TextAnchor.MiddleRight, Color.white);
            Label(leftRoot, "Level " + gs.level, 16, 700, 268, 22, 16, TextAnchor.MiddleLeft, new Color(0.7f, 0.7f, 0.75f));
            var rb = UIKit.Btn(leftRoot, confirmReset ? "YAKIN? " + gs.RespecCost + "g" : "RESET SKILL", () =>
            {
                if (!confirmReset) { confirmReset = true; Refresh(); return; }
                confirmReset = false; gs.RespecSkills(); Refresh();
            }, true, new Color(0.45f, 0.22f, 0.2f), 20);
            UIKit.At((RectTransform)rb.transform, 16, 760, 268, 36);
            var recb = UIKit.Btn(leftRoot, "REKOMENDASI RAID", () => { showRec = true; selSkill = null; evoPick = null; Refresh(); }, true, new Color(0.2f, 0.4f, 0.55f), 19);
            UIKit.At((RectTransform)recb.transform, 16, 720, 268, 36);
        }

        // ------------------------------------------------------------------ tengah: tab + node
        string[] TabNames(GameState gs)
        {
            var b = SkillDB.Branches(gs.job);
            var l = new List<string>();
            foreach (var x in b) { int p = x.IndexOf(" ("); l.Add(p > 0 ? x.Substring(0, p) : x); }
            l.Add("EVOLUSI LV.15"); l.Add("KELAS LV.30");
            return l.ToArray();
        }

        void DrawCenter(GameState gs)
        {
            Window(centerRoot, 0, 0, 790, 800, PanelIn, GoldDim);
            var names = TabNames(gs);
            float tw = 790f / names.Length;
            for (int i = 0; i < names.Length; i++)
            {
                int ti = i; bool on = i == tab;
                var tb = UIKit.Btn(centerRoot, names[i].ToUpper(), () => { tab = ti; selSkill = null; evoPick = null; showRec = false; Refresh(); }, true, on ? new Color(0.55f, 0.42f, 0.15f) : new Color(0.2f, 0.2f, 0.25f), 16);
                UIKit.At((RectTransform)tb.transform, 8 + i * tw, 8, tw - 8, 46);
            }

            string[] branches = SkillDB.Branches(gs.job);
            string branch = null;
            if (tab == branches.Length)
            {
                if (string.IsNullOrEmpty(gs.evo)) { DrawEvoChoice(gs, false); return; }
                branch = SkillDB.EvoBranch(gs.evo);
            }
            else if (tab == branches.Length + 1)
            {
                if (string.IsNullOrEmpty(gs.evo2)) { DrawEvoChoice(gs, true); return; }
                branch = SkillDB.EvoBranch(gs.evo2);
            }
            else branch = branches[tab];

            // posisi node
            var pos = new Dictionary<string, Vector2>();
            var rows = new List<List<SkillDef>>();
            for (int t = 1; t <= 3; t++)
            {
                var row = SkillDB.Row(gs.job, branch, t);
                rows.Add(row);
                for (int i = 0; i < row.Count; i++)
                    pos[row[i].id] = new Vector2(150 + 620f * (i + 0.5f) / row.Count, 150 + (t - 1) * 230f);
            }
            // garis penghubung
            for (int t = 0; t < 2; t++)
                foreach (var a in rows[t]) foreach (var b in rows[t + 1])
                {
                    bool lit = gs.Knows(a.id) && gs.Knows(b.id);
                    bool half = gs.Knows(a.id);
                    Line(centerRoot, pos[a.id] + new Vector2(0, 46), pos[b.id] - new Vector2(0, 46), lit ? Gold : (half ? GoldDim : new Color(0.28f, 0.28f, 0.33f)), lit ? 4 : 3);
                }
            // label tier
            for (int t = 0; t < 3; t++)
            {
                float y = 150 + t * 230f;
                var dm = Box(centerRoot, 36, y - 12, 24, 24, Gold);
                dm.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                Label(centerRoot, "TIER " + (t + 1), 8, y + 18, 80, 26, 18, TextAnchor.MiddleCenter, Gold);
                Label(centerRoot, "Lv. " + TierLv[t] + "+", 8, y + 42, 80, 22, 16, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.75f));
                Box(centerRoot, 100, y - 82, 1, 0, Color.clear);
            }
            // node
            for (int t = 0; t < 3; t++) foreach (var s in rows[t]) DrawNode(gs, s, pos[s.id]);
            if (rows[0].Count == 0) Label(centerRoot, "Belum ada skill di tab ini.", 100, 360, 600, 40, 22, TextAnchor.MiddleCenter, Color.gray);
        }

        void DrawNode(GameState gs, SkillDef s, Vector2 p)
        {
            bool learned = gs.Knows(s.id);
            string reason; bool can = gs.CanLearn(s, out reason);
            bool lvLock = gs.level < TierLv[Mathf.Clamp(s.tier, 1, 3) - 1];
            bool sel = s.id == selSkill;
            Color ring = learned ? Gold : (can ? new Color(0.4f, 0.65f, 1f) : new Color(0.32f, 0.32f, 0.38f));
            Color tint = ElemTint(s, gs.job);
            Color fill = learned ? Color.Lerp(new Color(0.08f, 0.08f, 0.1f), tint, 0.55f) : (can ? Color.Lerp(new Color(0.08f, 0.08f, 0.1f), tint, 0.3f) : new Color(0.1f, 0.1f, 0.12f));
            if (learned) { var g = Disc(centerRoot, "Glow", p.x, p.y, 128, new Color(Gold.r, Gold.g, Gold.b, 0.35f)); glows.Add(g); }
            if (sel) Disc(centerRoot, "Sel", p.x, p.y, 112, new Color(1, 1, 1, 0.35f));
            var outer = Disc(centerRoot, "Node", p.x, p.y, 96, ring);
            outer.raycastTarget = true;
            var btn = outer.gameObject.AddComponent<Button>();
            btn.targetGraphic = outer; btn.transition = Selectable.Transition.None;
            string sid = s.id;
            btn.onClick.AddListener(() => { Audio.Sfx("click"); selSkill = sid; showRec = false; Refresh(); });
            Disc(outer.transform.parent, "In", p.x, p.y, 84, fill);
            var icn = Disc(centerRoot, "Icon", p.x, p.y, 74, learned || can ? Color.white : new Color(0.45f, 0.45f, 0.5f, 1f));
            icn.sprite = SkillIcons.For(s);
            icn.raycastTarget = false;
            if (!learned && !can && (lvLock || reason.StartsWith("Butuh")))
            {
                Disc(centerRoot, "Dim", p.x, p.y, 84, new Color(0, 0, 0, 0.45f));
                LockIcon(centerRoot, p.x, p.y - 4, 1.2f, new Color(0.75f, 0.75f, 0.8f));
                if (lvLock) Label(centerRoot, "Lv. " + TierLv[Mathf.Clamp(s.tier, 1, 3) - 1], p.x - 40, p.y + 14, 80, 22, 16, TextAnchor.MiddleCenter, new Color(1f, 0.35f, 0.3f));
            }
            Label(centerRoot, s.name, p.x - 80, p.y + 52, 160, 22, 16, TextAnchor.MiddleCenter, learned ? Gold : new Color(0.8f, 0.8f, 0.85f));
            Label(centerRoot, (learned ? 1 : 0) + "/1", p.x - 30, p.y + 72, 60, 22, 16, TextAnchor.MiddleCenter, learned ? Gold : new Color(0.6f, 0.6f, 0.65f));
        }

        void DrawEvoChoice(GameState gs, bool lv30)
        {
            int lvNeed = lv30 ? JobInfo.Evo2Level : JobInfo.EvoLevel;
            bool can = lv30 ? gs.CanEvolve2 : gs.CanEvolve;
            if (lv30 && string.IsNullOrEmpty(gs.evo))
            {
                Label(centerRoot, "KELAS LEVEL 30", 0, 150, 790, 40, 30, TextAnchor.MiddleCenter, Gold);
                Label(centerRoot, "Pilih Evolusi Level 15 dulu. Setiap evolusi Lv.15 membuka satu kelas Lv.30:", 60, 210, 670, 80, 20, TextAnchor.UpperCenter, new Color(0.85f, 0.85f, 0.9f));
                var all = JobInfo.EvosOf(gs.job);
                for (int i = 0; i < all.Count; i++)
                {
                    var n = JobInfo.Evos2Of(all[i].id);
                    Label(centerRoot, all[i].name + "  >  " + (n.Count > 0 ? n[0].name : "-"), 60, 300 + i * 40, 670, 36, 20, TextAnchor.MiddleLeft, new Color(0.7f, 0.7f, 0.75f));
                }
                return;
            }
            var list = lv30 ? JobInfo.Evos2Of(gs.evo) : JobInfo.EvosOf(gs.job);
            if (!can)
            {
                Label(centerRoot, lv30 ? "KELAS LEVEL 30" : "EVOLUSI JOB", 0, 150, 790, 40, 30, TextAnchor.MiddleCenter, Gold);
                Label(centerRoot, "Terbuka di Level " + lvNeed + ".\nSetelah naik kelas kamu mendapat cabang skill khusus.\n\nLevelmu sekarang: " + gs.level, 60, 210, 670, 140, 22, TextAnchor.UpperCenter, new Color(0.85f, 0.85f, 0.9f));
                for (int i = 0; i < list.Count; i++)
                    Label(centerRoot, list[i].name + " - " + list[i].desc, 60, 380 + i * 70, 670, 64, 17, TextAnchor.UpperLeft, new Color(0.65f, 0.65f, 0.7f));
                return;
            }
            Label(centerRoot, lv30 ? "NAIK KELAS LEVEL 30" : "PILIH EVOLUSI JOB (PERMANEN)", 0, 74, 790, 40, 26, TextAnchor.MiddleCenter, Gold);
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i]; string eid = e.id; bool on = evoPick == eid;
                float y = 130 + i * 210;
                var card = Window(centerRoot, 30, y, 730, 190, on ? new Color(0.2f, 0.16f, 0.07f, 1f) : new Color(0.08f, 0.08f, 0.11f, 1f), on ? Gold : GoldDim);
                Disc(card.transform, "Ic", 80, 95, 90, Color.Lerp(new Color(0.1f, 0.1f, 0.12f), JobInfo.JobColor(gs.job), 0.6f));
                var ft = Label(card.transform, e.name.Substring(0, 1), 35, 60, 90, 70, 44, TextAnchor.MiddleCenter, Color.white); ft.fontStyle = FontStyle.Bold;
                Label(card.transform, e.name, 150, 12, 560, 38, 28, TextAnchor.MiddleLeft, Gold);
                Label(card.transform, e.desc, 150, 54, 560, 80, 18, TextAnchor.UpperLeft, new Color(0.85f, 0.85f, 0.9f));
                var cb = UIKit.Btn(card.transform, "Lihat skill", () => { evoPick = eid; selSkill = null; Refresh(); }, true, new Color(0.2f, 0.35f, 0.6f), 19);
                UIKit.At((RectTransform)cb.transform, 150, 134, 200, 42);
            }
        }

        // ------------------------------------------------------------------ kanan: detail
        void DrawRec(GameState gs)
        {
            Label(rightRoot, "REKOMENDASI RAID", 16, 8, 378, 30, 22, TextAnchor.MiddleLeft, Gold);
            Label(rightRoot, JobInfo.Name(gs.job) + (string.IsNullOrEmpty(gs.evo) ? "" : " > " + JobInfo.EvoName(gs.evo)) + (string.IsNullOrEmpty(gs.evo2) ? "" : " > " + JobInfo.EvoName(gs.evo2)), 16, 38, 378, 24, 15, TextAnchor.MiddleLeft, new Color(0.7f, 0.7f, 0.78f));
            var picks = SkillLoadout.Recommend(gs);
            float y = 70f;
            foreach (var p in picks)
            {
                var row = Window(rightRoot, 12, y, 386, 74, new Color(0.08f, 0.08f, 0.1f, 1f), p.slot == 3 ? Gold : new Color(0.25f, 0.25f, 0.3f));
                Label(row.transform, p.key, 6, 4, 34, 64, 26, TextAnchor.MiddleCenter, Gold);
                string nm = p.skill == null ? "(belum ada)" : p.skill.name + (p.learned ? "" : "  - belum dipelajari");
                Label(row.transform, nm, 44, 2, 336, 26, 17, TextAnchor.MiddleLeft, p.learned ? Color.white : new Color(0.55f, 0.75f, 1f));
                Label(row.transform, p.role + ": " + p.reason, 44, 26, 336, 46, 12, TextAnchor.UpperLeft, new Color(0.75f, 0.75f, 0.8f));
                y += 80f;
            }
            var ab = UIKit.Btn(rightRoot, "PASANG SEMUA", () => { int n = SkillLoadout.Apply(gs); TownUI.I.Toast(n + " skill rekomendasi dipasang ke slot."); Refresh(); }, true, new Color(0.5f, 0.38f, 0.1f), 21);
            UIKit.At((RectTransform)ab.transform, 12, 636, 386, 44);
            var tips = SkillLoadout.RaidTips(gs);
            Label(rightRoot, "- " + tips[0] + "\n- " + tips[1] + "\n- " + tips[2] + "\n- " + tips[3], 14, 686, 382, 108, 12, TextAnchor.UpperLeft, new Color(0.8f, 0.8f, 0.85f));
        }

        void DrawRight(GameState gs)
        {
            Window(rightRoot, 0, 0, 410, 800, PanelIn, GoldDim);
            if (showRec && selSkill == null) { DrawRec(gs); return; }
            if (selSkill == null && evoPick != null && (gs.CanEvolve || gs.CanEvolve2)) { DrawEvoDetail(gs); return; }
            var s = SkillDB.Get(selSkill);
            if (s == null)
            {
                Label(rightRoot, "Pilih sebuah skill untuk melihat detailnya.\n\nEmas = sudah dipelajari\nBiru = bisa dipelajari\nAbu-abu = terkunci\n\nTier 2 butuh Lv.10 dan 2 skill Tier 1 di cabang yang sama; Tier 3 butuh Lv.20 dan kedua skill Tier 2.\n\nSkill aktif dipasang ke slot Z X C V R T Q (V = slot tengah besar, cocok untuk ultimate).\n\nBingung mau bawa apa? Tekan REKOMENDASI RAID di kiri.", 20, 30, 370, 500, 18, TextAnchor.UpperLeft, new Color(0.8f, 0.8f, 0.85f));
                return;
            }
            bool learned = gs.Knows(s.id);
            string reason; bool can = gs.CanLearn(s, out reason);
            Color tint = ElemTint(s, gs.job);
            Disc(rightRoot, "G", 205, 74, 118, new Color(Gold.r, Gold.g, Gold.b, learned ? 0.4f : 0.15f));
            Disc(rightRoot, "R", 205, 74, 100, learned ? Gold : GoldDim);
            Disc(rightRoot, "I", 205, 74, 88, Color.Lerp(new Color(0.08f, 0.08f, 0.1f), tint, 0.55f));
            var ft = Label(rightRoot, s.name.Substring(0, 1).ToUpper(), 165, 46, 80, 56, 42, TextAnchor.MiddleCenter, Color.white); ft.fontStyle = FontStyle.Bold;
            Label(rightRoot, s.name, 10, 138, 390, 36, 28, TextAnchor.MiddleCenter, Gold);
            Label(rightRoot, "Lv. " + (learned ? 1 : 0) + " / 1", 10, 174, 390, 26, 20, TextAnchor.MiddleCenter, Color.white);
            var tag = Box(rightRoot, 145, 204, 120, 28, s.active ? new Color(0.2f, 0.4f, 0.7f) : new Color(0.3f, 0.5f, 0.3f));
            Label(rightRoot, s.active ? "Skill Aktif" : "Skill Pasif", 145, 204, 120, 28, 16, TextAnchor.MiddleCenter, Color.white);
            Label(rightRoot, s.desc, 20, 240, 370, 100, 16, TextAnchor.UpperLeft, new Color(0.85f, 0.85f, 0.9f));

            // detail efek
            Label(rightRoot, "DETAIL EFEK", 20, 338, 370, 26, 18, TextAnchor.MiddleLeft, Gold);
            Box(rightRoot, 20, 364, 370, 1, GoldDim);
            float y = 370;
            var rows = new List<string[]>();
            if (s.active)
            {
                string dmg = s.kind == "heal" || s.kind == "buff" ? "-" : Mathf.RoundToInt(s.mult * 100f) + "%";
                if (s.kind == "heal" && s.HasFx("heal")) dmg = "Heal " + Mathf.RoundToInt(s.Fx("heal") * 100f) + "% HP";
                rows.Add(new[] { "Damage", dmg });
                string area = s.radius > 0f && s.kind != "cone" ? s.radius.ToString("0.#") + " m" : (s.range > 0f ? s.range.ToString("0.#") + " m" : "-");
                if (s.kind == "cone") area = s.range.ToString("0.#") + " m / " + (s.radius * 2f).ToString("0") + "°";
                rows.Add(new[] { "Area", area });
                float cdr = Mathf.Min(0.5f, gs.Pas("cdr"));
                rows.Add(new[] { "Cooldown", cdr > 0.001f ? s.cooldown.ToString("0.#") + "s → " + (s.cooldown * (1f - cdr)).ToString("0.#") + "s" : s.cooldown.ToString("0.#") + "s" });
                if (gs.IsMage) { float b = s.tier >= 3 ? 50f : 25f; rows.Add(new[] { "Konsumsi Mana", Mathf.RoundToInt(b) + " → " + Mathf.RoundToInt(b * gs.ManaCostMult) }); }
                else rows.Add(new[] { "Konsumsi Stamina", (s.tier >= 3 ? 40 : 20).ToString() });
                if (!string.IsNullOrEmpty(s.fx)) rows.Add(new[] { "Efek", FxText(s) });
            }
            else
            {
                if (s.pas != null) foreach (var kv in s.pas) rows.Add(new[] { PasName(kv.Key), PasVal(kv.Key, kv.Value) });
                if (rows.Count == 0) rows.Add(new[] { "Efek", "Aktif otomatis" });
            }
            foreach (var r in rows)
            {
                Label(rightRoot, r[0], 24, y, 160, 28, 16, TextAnchor.MiddleLeft, new Color(0.7f, 0.7f, 0.75f));
                Label(rightRoot, r[1], 170, y, 220, 28, 16, TextAnchor.MiddleRight, Color.white);
                Box(rightRoot, 20, y + 29, 370, 1, new Color(0.2f, 0.2f, 0.25f));
                y += 31;
            }
            y = Mathf.Max(y, 500);

            // preview
            Label(rightRoot, "PREVIEW SKILL", 20, y + 4, 370, 24, 18, TextAnchor.MiddleLeft, Gold);
            var pv = Window(rightRoot, 20, y + 30, 370, 104, new Color(0.04f, 0.04f, 0.07f, 1f), GoldDim);
            pvKind = s.kind; pvElem = string.IsNullOrEmpty(s.elem) ? JobInfo.Elem(gs.job) : s.elem;
            if (!s.active) pvKind = "passive";
            Color pc = tint;
            pvGlow = Disc(pv.transform, "PG", 185 - 2, 50, 40, new Color(pc.r, pc.g, pc.b, 0.5f));
            pvRing = Disc(pv.transform, "PR", 185 - 2, 50, 20, new Color(pc.r, pc.g, pc.b, 0.6f));
            pvDot = Disc(pv.transform, "PD", 40, 50, 22, Color.Lerp(pc, Color.white, 0.5f));

            // syarat
            float ry = y + 144;
            Label(rightRoot, "SYARAT UPGRADE", 20, ry, 370, 24, 18, TextAnchor.MiddleLeft, Gold);
            if (learned)
            {
                Label(rightRoot, "Level Maksimum", 20, ry + 26, 370, 30, 20, TextAnchor.MiddleCenter, Gold);
                if (s.active)
                {
                    for (int i = 0; i < 7; i++)
                    {
                        int si = i; string sid = s.id;
                        bool here = gs.skillSlots[i] == s.id;
                        var ab = UIKit.Btn(rightRoot, "[" + SlotKeys[i] + "]", () => { gs.AssignSkill(si, sid); Refresh(); }, true, here ? new Color(0.5f, 0.4f, 0.1f) : new Color(0.25f, 0.28f, 0.45f), 19);
                        UIKit.At((RectTransform)ab.transform, 20 + i * 53f, ry + 62, 50, 40);
                    }
                }
            }
            else
            {
                Label(rightRoot, "Skill Point: " + s.cost + "  (punya " + gs.skillPoints + ")", 20, ry + 26, 370, 28, 17, TextAnchor.MiddleLeft, gs.skillPoints >= s.cost ? Color.white : new Color(1f, 0.4f, 0.35f));
                if (!can) Label(rightRoot, reason, 20, ry + 54, 370, 40, 15, TextAnchor.UpperLeft, new Color(1f, 0.55f, 0.45f));
                string sid = s.id;
                var lb = UIKit.Btn(rightRoot, "PELAJARI", () => { gs.LearnSkill(sid); Refresh(); }, can, new Color(0.5f, 0.38f, 0.1f), 22);
                UIKit.At((RectTransform)lb.transform, 20, ry + 96, 370, 48);
            }
        }

        void DrawEvoDetail(GameState gs)
        {
            var e = JobInfo.Evo(evoPick);
            if (e == null) return;
            Label(rightRoot, e.name, 10, 30, 390, 40, 30, TextAnchor.MiddleCenter, Gold);
            Label(rightRoot, e.desc, 20, 80, 370, 130, 18, TextAnchor.UpperLeft, new Color(0.85f, 0.85f, 0.9f));
            Label(rightRoot, "SKILL EVOLUSI", 20, 220, 370, 26, 18, TextAnchor.MiddleLeft, Gold);
            float y = 250;
            foreach (var s in SkillDB.All)
                if (s.evo == evoPick) { Label(rightRoot, "T" + s.tier + "  " + s.name + (s.active ? "  (aktif)" : ""), 24, y, 370, 24, 16, TextAnchor.MiddleLeft, new Color(0.85f, 0.85f, 0.9f)); y += 25; }
            string id = evoPick;
            Label(rightRoot, e.level == JobInfo.Evo2Level ? "Pilihan ini PERMANEN. Bonus +5 SP." : "Pilihan ini PERMANEN. Bonus +3 SP.", 20, 640, 370, 28, 16, TextAnchor.MiddleCenter, new Color(1f, 0.7f, 0.4f));
            var eb = UIKit.Btn(rightRoot, "EVOLUSI: " + e.name.ToUpper(), () => { if (e.level == JobInfo.Evo2Level ? gs.Evolve2(id) : gs.Evolve(id)) { evoPick = null; selSkill = null; Refresh(); } }, e.level == JobInfo.Evo2Level ? gs.CanEvolve2 : gs.CanEvolve, new Color(0.55f, 0.4f, 0.1f), 21);
            UIKit.At((RectTransform)eb.transform, 20, 690, 370, 56);
        }

        static string FxText(SkillDef s)
        {
            var l = new List<string>();
            if (s.HasFx("slow")) l.Add("Slow");
            if (s.HasFx("stagger")) l.Add("Stagger");
            if (s.HasFx("knock")) l.Add("Knockback");
            if (s.HasFx("pull")) l.Add("Tarik");
            if (s.HasFx("burn")) l.Add("Bakar");
            if (s.HasFx("dot")) l.Add("DoT");
            if (s.HasFx("chain")) l.Add("Chain");
            if (s.HasFx("heal")) l.Add("Heal");
            if (s.HasFx("mana")) l.Add("Mana");
            if (s.HasFx("invuln")) l.Add("Kebal");
            if (s.HasFx("count")) l.Add("x" + Mathf.RoundToInt(s.Fx("count")));
            if (s.HasFx("pet")) l.Add("Summon");
            foreach (var k in new[] { "dmg", "spd", "dmgred", "cdr", "crit" }) if (s.HasFx("b_" + k)) l.Add("Buff " + PasName(k));
            return l.Count == 0 ? "-" : string.Join(", ", l.ToArray());
        }

        static string PasName(string k)
        {
            switch (k)
            {
                case "dmg": return "Damage"; case "def": return "Pertahanan"; case "hp": return "HP maks"; case "mana": return "Mana maks";
                case "manaregen": return "Regen mana"; case "manacost": return "Biaya mana"; case "crit": return "Crit"; case "lifesteal": return "Lifesteal";
                case "spd": return "Kecepatan"; case "dmgred": return "Redam damage"; case "cdr": return "Cooldown"; case "regen": return "Regen HP";
                case "slowbolt": return "Bolt memperlambat";
            }
            return k;
        }

        static string PasVal(string k, float v)
        {
            if (k == "def" || k == "mana") return "+" + Mathf.RoundToInt(v);
            if (k == "crit") return "+" + v.ToString("0.#") + "%";
            if (k == "slowbolt") return "Ya";
            if (k == "cdr" || k == "manacost" || k == "dmgred") return "-" + Mathf.RoundToInt(v * 100f) + "%";
            return "+" + Mathf.RoundToInt(v * 100f) + "%";
        }

        // ------------------------------------------------------------------ animasi preview
        void AnimatePreview(float t)
        {
            if (pvDot == null) return;
            float u = (t % 1.6f) / 1.6f;
            float cx = 183f;
            bool travel = pvKind == "bolt" || pvKind == "multi" || pvKind == "line" || pvKind == "dash";
            if (travel)
            {
                float x = Mathf.Lerp(30f, 330f, Mathf.Clamp01(u * 1.3f));
                pvDot.rectTransform.anchoredPosition = new Vector2(x, -50f);
                pvDot.enabled = u < 0.8f;
                float burst = Mathf.Clamp01((u - 0.77f) / 0.23f);
                pvRing.rectTransform.anchoredPosition = new Vector2(330f, -50f);
                pvGlow.rectTransform.anchoredPosition = new Vector2(330f, -50f);
                pvRing.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(10f, 70f, burst);
                pvRing.color = new Color(pvRing.color.r, pvRing.color.g, pvRing.color.b, (1f - burst) * 0.7f * (u > 0.77f ? 1f : 0f));
                pvGlow.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(8f, 40f, burst);
            }
            else if (pvKind == "passive" || pvKind == "buff" || pvKind == "heal")
            {
                pvDot.enabled = false;
                float s = 1f + 0.18f * Mathf.Sin(t * 4f);
                pvGlow.rectTransform.anchoredPosition = new Vector2(cx, -50f);
                pvGlow.rectTransform.sizeDelta = Vector2.one * 56f * s;
                pvRing.rectTransform.anchoredPosition = new Vector2(cx, -50f);
                pvRing.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(30f, 90f, u);
                pvRing.color = new Color(pvRing.color.r, pvRing.color.g, pvRing.color.b, (1f - u) * 0.6f);
            }
            else
            {
                pvDot.enabled = true;
                pvDot.rectTransform.anchoredPosition = new Vector2(cx, -50f);
                pvDot.rectTransform.sizeDelta = Vector2.one * 22f;
                pvGlow.rectTransform.anchoredPosition = new Vector2(cx, -50f);
                pvGlow.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(20f, 100f, Mathf.Clamp01(u * 1.6f));
                pvGlow.color = new Color(pvGlow.color.r, pvGlow.color.g, pvGlow.color.b, Mathf.Clamp01(1f - u) * 0.5f);
                pvRing.rectTransform.anchoredPosition = new Vector2(cx, -50f);
                pvRing.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(10f, 96f, u);
                pvRing.color = new Color(pvRing.color.r, pvRing.color.g, pvRing.color.b, (1f - u) * 0.7f);
            }
        }
    }
}
