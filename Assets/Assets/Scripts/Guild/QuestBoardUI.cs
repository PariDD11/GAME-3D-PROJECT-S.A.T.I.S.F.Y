using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HunterGuild
{
    /// <summary>
    /// Jendela Quest Guild bergaya emas-gelap: tab kategori (Semua/Utama/Sampingan/Harian/Guild/Selesai), daftar bergulir berkelompok di kiri,
    /// detail di tengah (tujuan, hadiah, deskripsi, tombol), serta lokasi dan peta di kanan.
    /// </summary>
    public class QuestBoardUI : MonoBehaviour
    {
        public static QuestBoardUI I { get; private set; }

        public static QuestBoardUI Get()
        {
            if (I == null) new GameObject("QuestBoardUI").AddComponent<QuestBoardUI>();
            return I;
        }

        public bool IsOpen { get { return root != null && root.gameObject.activeSelf; } }

        static readonly string[] TabNames = { "Semua", "Utama", "Sampingan", "Harian", "Guild", "Selesai" };
        static readonly string[] TabGlyph = { "#", "!", "Q", "D", "G", "OK" };
        static readonly string[] CatKeys = { "utama", "sampingan", "harian", "guild", "selesai" };
        static readonly string[] CatTitle = { "Quest Utama", "Quest Sampingan", "Quest Harian", "Quest Guild", "Quest Selesai" };
        static readonly string[] CatGlyph = { "!", "Q", "D", "G", "OK" };
        static readonly string[] RegionKeys = { "", "forest", "desert", "swamp", "tower", "frontier", "volcano", "bone", "demon" };
        static readonly string[] RegionLabels = { "Semua", "Hutan", "Gurun", "Rawa", "Menara", "Perbatasan", "Vulkanik", "Tulang", "Iblis" };

        RectTransform root, dyn, listContent;
        int themeVer = -1;
        ScrollRect scroll;
        List<GuildQuest> shown = new List<GuildQuest>();
        string selId;
        int tab;
        int regionIdx;
        Texture2D mapTex;

        void Awake() { I = this; }

        void OnDestroy()
        {
            if (I == this) I = null;
            if (mapTex != null) Destroy(mapTex);
        }

        void Update()
        {
            // Esc menutup lewat TownUI.Close(); jendela ini ikut tertutup saat TownUI tidak lagi "terbuka".
            if (IsOpen && (TownUI.I == null || !TownUI.I.PanelOpen)) Hide();
        }

        public void Show()
        {
            if (TownUI.I == null) return;
            if (UiStyle.Stale(ref themeVer) && root != null) { Destroy(root.gameObject); root = null; }
            if (root == null) { root = GoldUI.Window(TownUI.I.Root, "QuestWindow", "QUEST", "Q", Hide); }
            root.gameObject.SetActive(true);
            TownUI.I.SetExternalOpen(true);
            Rebuild(true);
        }

        public void Hide()
        {
            if (root != null) root.gameObject.SetActive(false);
            if (TownUI.I != null) TownUI.I.SetExternalOpen(false);
        }

        // ---------------- pembantu data ----------------
        static string Cat(GuildQuest q)
        {
            var gs = GameState.I;
            if (q.fromNotice) return "harian";
            if (!string.IsNullOrEmpty(q.examTarget) || q.towerFloor > 0) return "guild";
            string t, tg; int c;
            if (GuildQuestDB.Objective(q.id, out t, out tg, out c) && t == "hunt") return "utama";
            return "sampingan";
        }

        static bool Done(GuildQuest q) { return GameState.I.Stat("qd_" + q.id) > 0; }

        static string RegionShort(GuildQuest q)
        {
            string n = GuildQuest.RegionName(q.Region);
            int i = n.IndexOf(" (");
            return i > 0 ? n.Substring(0, i) : n;
        }

        static string NiceName(string id)
        {
            var m = MonsterDB.Get(id); if (m != null) return m.name;
            var d = ItemDB.Get(id); if (d != null) return d.name;
            return id;
        }

        static string Glyph(string cat)
        {
            int i = System.Array.IndexOf(CatKeys, cat);
            return i >= 0 ? CatGlyph[i] : "Q";
        }

        // ---------------- isi ----------------
        GuildQuest Current()
        {
            foreach (var q in shown) if (q.id == selId) return q;
            return null;
        }

        void Rebuild(bool resetScroll)
        {
            var gs = GameState.I;
            GuildData.I.EnsureNotices();
            float pos = scroll != null ? scroll.verticalNormalizedPosition : 1f;

            shown = GuildQuestDB.Visible();
            string rk = RegionKeys[regionIdx];
            if (rk != "") shown = shown.FindAll(q => q.Region == rk);
            string want = tab == 0 ? "" : CatKeys[tab - 1];
            shown = shown.FindAll(q =>
            {
                bool done = Done(q);
                if (want == "selesai") return done;
                if (want == "") return true;
                return Cat(q) == want;
            });

            if (Current() == null)
            {
                selId = null;
                int pr = GameState.RankIndex(gs.rank);
                foreach (var q in shown) if (q.RankIdx <= pr) { selId = q.id; break; }
                if (selId == null && shown.Count > 0) selId = shown[0].id;
            }

            if (dyn != null) { dyn.SetParent(null); Destroy(dyn.gameObject); }
            dyn = UIKit.Rect("Dyn", root);
            dyn.anchorMin = Vector2.zero; dyn.anchorMax = Vector2.one; dyn.offsetMin = Vector2.zero; dyn.offsetMax = Vector2.zero;

            for (int i = 0; i < TabNames.Length; i++)
            {
                int ti = i;
                GoldUI.Tab(dyn, TabNames[i], TabGlyph[i], 36f + i * 249f, 88f, 240f, 66f, i == tab, () => { tab = ti; Rebuild(true); });
            }
            BuildList(resetScroll, pos);
            BuildDetail();
        }

        void BuildList(bool resetScroll, float pos)
        {
            var box = GoldUI.Box(dyn, 18f, 168f, 514f, 830f);
            GoldUI.Gold(dyn, "Wilayah: " + RegionLabels[regionIdx] + "  >", 30f, 176f, 490f, 40f, true, () => { regionIdx = (regionIdx + 1) % RegionKeys.Length; Rebuild(true); }, false);

            var vp = UIKit.Img("Viewport", dyn, new Color(1f, 1f, 1f, 0.01f));
            UIKit.At(vp.rectTransform, 28f, 224f, 494f, 766f);
            vp.gameObject.AddComponent<RectMask2D>();
            scroll = vp.gameObject.AddComponent<ScrollRect>();
            listContent = UIKit.Rect("Content", vp.transform);
            listContent.anchorMin = new Vector2(0f, 1f); listContent.anchorMax = new Vector2(1f, 1f); listContent.pivot = new Vector2(0.5f, 1f);
            listContent.anchoredPosition = Vector2.zero;

            float y = 0f;
            var gs = GameState.I;
            if (shown.Count == 0)
            {
                GoldUI.Label(listContent, tab == 5 ? "Belum ada quest yang selesai." : "Tidak ada quest di kategori ini.", 22, UiStyle.Muted, 10f, 14f, 470f, 40f, TextAnchor.MiddleLeft);
                y = 60f;
            }
            else if (tab == 0)
            {
                for (int c = 0; c < 4; c++)
                {
                    var group = shown.FindAll(q => Cat(q) == CatKeys[c]);
                    if (group.Count == 0) continue;
                    y = Section(CatTitle[c], CatGlyph[c], y);
                    foreach (var q in group) y = Row(q, y);
                }
            }
            else
            {
                y = Section(CatTitle[tab - 1], CatGlyph[tab - 1], y);
                foreach (var q in shown) y = Row(q, y);
            }
            listContent.sizeDelta = new Vector2(0f, y + 8f);
            scroll.viewport = vp.rectTransform; scroll.content = listContent;
            scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40f;
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = resetScroll ? 1f : pos;
        }

        float Section(string title, string glyph, float y)
        {
            GoldUI.Ring(listContent, glyph, 6f, y + 4f, 34f, UiStyle.Gold);
            GoldUI.Label(listContent, title, 24, UiStyle.Gold, 50f, y, 400f, 42f, TextAnchor.MiddleLeft);
            return y + 48f;
        }

        float Row(GuildQuest q, float y)
        {
            var gs = GameState.I;
            bool locked = q.RankIdx > GameState.RankIndex(gs.rank);
            bool active = gs.activeQuest != null && gs.activeQuest.id == q.id;
            bool on = q.id == selId;
            var bg = UiStyle.Plate("Row", listContent); bg.raycastTarget = true;
            bg.color = on ? UiStyle.Cur.TabOn : active ? new Color(0.45f, 0.85f, 0.5f) : locked ? new Color(0.45f, 0.45f, 0.5f) : new Color(0.75f, 0.75f, 0.8f);
            UIKit.At(bg.rectTransform, 4f, y, 478f, 78f);
            var b = bg.gameObject.AddComponent<Button>(); b.targetGraphic = bg;
            var quest = q;
            b.onClick.AddListener(() => { Audio.Sfx("click"); selId = quest.id; Rebuild(false); });
            string cat = Cat(q);
            Color ic = cat == "harian" ? new Color(0.75f, 0.4f, 0.95f) : UiStyle.Gold;
            GoldUI.Ring(bg.transform, locked ? "x" : Glyph(cat), 12f, 14f, 50f, ic);
            GoldUI.Label(bg.transform, q.title, 23, UiStyle.Cream, 74f, 6f, 300f, 36f, TextAnchor.MiddleLeft);
            GoldUI.Label(bg.transform, "Rank " + q.rank + (active ? "   [AKTIF]" : locked ? "   [Terkunci]" : Done(q) ? "   [Selesai]" : ""), 18, UiStyle.Muted, 74f, 42f, 300f, 28f, TextAnchor.MiddleLeft);
            GoldUI.Label(bg.transform, RegionShort(q), 17, UiStyle.Muted, 300f, 42f, 150f, 28f, TextAnchor.MiddleRight);
            return y + 84f;
        }

        void BuildDetail()
        {
            var q = Current();
            // kotak tengah dan kanan
            GoldUI.Box(dyn, 540f, 168f, 570f, 830f);
            GoldUI.Box(dyn, 1122f, 168f, 404f, 450f);
            GoldUI.Box(dyn, 1122f, 628f, 404f, 370f);
            GoldUI.Label(dyn, "Lokasi Quest", 22, UiStyle.Gold, 1142f, 176f, 300f, 30f, TextAnchor.MiddleLeft);
            GoldUI.Label(dyn, "Peta", 22, UiStyle.Gold, 1142f, 636f, 300f, 30f, TextAnchor.MiddleLeft);
            if (q == null)
            {
                GoldUI.Label(dyn, "Pilih quest di daftar kiri.", 26, UiStyle.Muted, 570f, 200f, 500f, 60f, TextAnchor.MiddleLeft);
                return;
            }
            var gs = GameState.I;
            bool active = gs.activeQuest != null && gs.activeQuest.id == q.id;
            string cat = Cat(q);

            // header
            GoldUI.Ring(dyn, Glyph(cat), 562f, 186f, 82f, cat == "harian" ? new Color(0.75f, 0.4f, 0.95f) : UiStyle.Gold);
            GoldUI.Label(dyn, q.title, 36, UiStyle.Gold, 660f, 184f, 340f, 56f, TextAnchor.MiddleLeft, true);
            GoldUI.Label(dyn, CatTitle[System.Array.IndexOf(CatKeys, cat)], 22, UiStyle.GoldDim, 660f, 242f, 340f, 30f, TextAnchor.MiddleLeft);
            var lv = GoldUI.Panel(dyn, 1004f, 206f, 88f, 46f, new Color(0.1f, 0.1f, 0.14f, 1f));
            GoldUI.Label(lv.transform, "Rank " + q.rank, 20, UiStyle.Cream, 0f, 0f, 88f, 46f, TextAnchor.MiddleCenter);
            GoldUI.Divider(dyn, 556f, 284f, 538f);

            // deskripsi
            GoldUI.Label(dyn, q.desc, 22, UiStyle.Cream, 562f, 296f, 524f, 104f, TextAnchor.UpperLeft);
            GoldUI.Divider(dyn, 556f, 404f, 538f);

            // tujuan
            GoldUI.Label(dyn, "Tujuan Quest", 24, UiStyle.Gold, 562f, 412f, 400f, 32f, TextAnchor.MiddleLeft);
            string ot, otg; int oc;
            bool hasObj = GuildQuestDB.Objective(q.id, out ot, out otg, out oc);
            string line1 = "Pergi ke " + q.area;
            string line2 = hasObj ? (ot == "gather" ? "Kumpulkan " : ot == "hunt" ? "Kalahkan " : "Basmi ") + NiceName(otg) + (oc > 1 ? " x" + oc : "") : "Selesaikan misi di lokasi";
            if (active && hasObj && gs.activeQuest.targetCount > 0) line2 += "   (" + gs.questProgress + "/" + gs.activeQuest.targetCount + ")";
            string line3 = "Lapor ke resepsionis Guild";
            bool done2 = active && gs.activeQuest.completed;
            Objective(562f, 450f, line1, active, false);
            Objective(562f, 490f, line2, done2, !active);
            Objective(562f, 530f, line3, false, !done2);

            // hadiah
            GoldUI.Divider(dyn, 556f, 574f, 538f);
            GoldUI.Label(dyn, "Hadiah Quest", 24, UiStyle.Gold, 562f, 582f, 400f, 32f, TextAnchor.MiddleLeft);
            var chips = new List<string[]>();
            chips.Add(new[] { "G", GoldUI.Num(q.reward) + " g" });
            chips.Add(new[] { "XP", GoldUI.Num(q.exp) + " EXP" });
            if (q.deposit > 0) chips.Add(new[] { "$", q.deposit + " g" });
            chips.Add(new[] { "T", q.time });
            for (int i = 0; i < chips.Count && i < 4; i++)
            {
                var chip = GoldUI.Panel(dyn, 562f + i * 98f, 620f, 90f, 100f, new Color(0.1f, 0.1f, 0.15f, 1f));
                GoldUI.Ring(chip.transform, chips[i][0], 17f, 8f, 56f, UiStyle.Gold);
                GoldUI.Label(chip.transform, chips[i][1], 16, UiStyle.Cream, 0f, 68f, 90f, 28f, TextAnchor.MiddleCenter);
            }

            // deskripsi tambahan
            GoldUI.Divider(dyn, 556f, 728f, 538f);
            GoldUI.Label(dyn, "Deskripsi Tambahan", 24, UiStyle.Gold, 562f, 736f, 400f, 32f, TextAnchor.MiddleLeft);
            string why = active ? "" : GuildServices.CanAccept(q);
            string info = "Wilayah: " + GuildQuest.RegionName(q.Region) + "\nMode: " + (q.PartyRequired ? "WAJIB PARTY (min " + q.minPartners + " partner)" : "Solo") +
                          (q.timeLimitMin > 0 ? "   |   Batas waktu " + q.timeLimitMin + " menit" : "") +
                          (q.expiresAt >= 0f ? "\nBerlaku: " + GuildServices.FormatHours(Mathf.Max(0.1f, q.expiresAt - GuildData.Now)) + " lagi" : "") +
                          "\nJika gagal: " + q.PenaltyText() + ".";
            GoldUI.Label(dyn, info, 19, UiStyle.Cream, 562f, 770f, 524f, 100f, TextAnchor.UpperLeft);
            string st = active ? "Quest ini sedang aktif." + (gs.activeQuest.completed ? " Selesai: klaim di resepsionis." : "")
                        : why == "" ? "Siap diterima. Berangkat lewat Gerbang Kota." : why;
            GoldUI.Label(dyn, st, 19, active ? new Color(0.5f, 1f, 0.6f) : why == "" ? new Color(0.6f, 0.9f, 1f) : new Color(1f, 0.8f, 0.4f), 562f, 862f, 524f, 36f, TextAnchor.MiddleLeft);

            // tombol
            var quest = q;
            if (active)
            {
                GoldUI.Gold(dyn, "Quest Aktif", 562f, 922f, 292f, 56f, false, null, true);
                GoldUI.Gold(dyn, gs.activeQuest.completed ? "Selesai" : "Batalkan", 866f, 922f, 220f, 56f, !gs.activeQuest.completed, () => { GuildServices.CancelActive(); Rebuild(false); }, false);
            }
            else
            {
                GoldUI.Gold(dyn, why == "" ? "Terima Quest" : "Tidak bisa diterima", 562f, 922f, 292f, 56f, why == "", () => { if (GuildServices.Accept(quest)) Rebuild(false); }, true);
                GoldUI.Gold(dyn, "Batalkan", 866f, 922f, 220f, 56f, false, null, false);
            }

            // lokasi (banner prosedural) + peta
            Color a, b;
            MapPreview.Palette(q.theme, out a, out b);
            var ban = UIKit.Img("Banner", dyn, a); ban.raycastTarget = false;
            UIKit.At(ban.rectTransform, 1136f, 212f, 376f, 350f);
            var ban2 = UIKit.Img("Banner2", dyn, new Color(b.r, b.g, b.b, 0.55f)); ban2.raycastTarget = false;
            UIKit.At(ban2.rectTransform, 1136f, 212f, 376f, 170f);
            GoldUI.Ring(dyn, Glyph(cat), 1236f, 250f, 176f, new Color(1f, 0.93f, 0.7f));
            GoldUI.Label(dyn, q.area, 28, Color.white, 1146f, 440f, 356f, 50f, TextAnchor.MiddleCenter, true);
            GoldUI.Label(dyn, GuildQuest.RegionName(q.Region), 18, UiStyle.Cream, 1146f, 490f, 356f, 60f, TextAnchor.UpperCenter);
            GoldUI.Ring(dyn, "o", 1142f, 568f, 40f, UiStyle.Gold);
            GoldUI.Label(dyn, q.area, 22, UiStyle.Cream, 1192f, 566f, 320f, 44f, TextAnchor.MiddleLeft);

            if (mapTex != null) Destroy(mapTex);
            mapTex = MapPreview.Make(q);
            var frame = UIKit.Img("MapFrame", dyn, new Color(0.02f, 0.02f, 0.03f, 1f)); frame.raycastTarget = false;
            UIKit.At(frame.rectTransform, 1136f, 672f, 376f, 312f);
            var mapRt = UIKit.Rect("Map", frame.transform);
            mapRt.anchorMin = Vector2.zero; mapRt.anchorMax = Vector2.one; mapRt.offsetMin = new Vector2(5f, 5f); mapRt.offsetMax = new Vector2(-5f, -5f);
            var ri = mapRt.gameObject.AddComponent<RawImage>(); ri.texture = mapTex; ri.raycastTarget = false;
            GoldUI.Label(frame.transform, "N ^", 20, Color.white, 300f, 10f, 66f, 28f, TextAnchor.UpperRight);
        }

        void Objective(float x, float y, string text, bool done, bool locked)
        {
            var c = GoldUI.Ring(dyn, done ? "v" : locked ? "x" : "", x + 2f, y + 3f, 30f, locked ? UiStyle.Muted : UiStyle.Gold);
            GoldUI.Label(dyn, text, 21, locked ? UiStyle.Muted : UiStyle.Cream, x + 44f, y, 480f, 36f, TextAnchor.MiddleLeft);
        }
    }

    /// <summary>Map preview kecil dibuat prosedural dari tema area (Perlin noise) + rute dari kota ke lokasi.</summary>
    public static class MapPreview
    {
        public static void Palette(string theme, out Color a, out Color b)
        {
            switch (theme)
            {
                case "plains": a = new Color(0.40f, 0.55f, 0.28f); b = new Color(0.58f, 0.68f, 0.36f); break;
                case "deepforest": a = new Color(0.08f, 0.22f, 0.12f); b = new Color(0.16f, 0.32f, 0.16f); break;
                case "desert": a = new Color(0.72f, 0.56f, 0.32f); b = new Color(0.88f, 0.74f, 0.46f); break;
                case "swamp": a = new Color(0.10f, 0.20f, 0.17f); b = new Color(0.26f, 0.38f, 0.30f); break;
                case "volcano": a = new Color(0.25f, 0.08f, 0.04f); b = new Color(0.8f, 0.32f, 0.08f); break;
                case "bone": a = new Color(0.35f, 0.38f, 0.45f); b = new Color(0.75f, 0.76f, 0.7f); break;
                case "demon": a = new Color(0.18f, 0.04f, 0.1f); b = new Color(0.55f, 0.12f, 0.35f); break;
                case "cave": a = new Color(0.25f, 0.22f, 0.20f); b = new Color(0.42f, 0.36f, 0.30f); break;
                case "ruins": a = new Color(0.45f, 0.42f, 0.35f); b = new Color(0.62f, 0.57f, 0.46f); break;
                case "dungeon": a = new Color(0.20f, 0.12f, 0.25f); b = new Color(0.36f, 0.20f, 0.40f); break;
                case "tower": a = new Color(0.20f, 0.22f, 0.30f); b = new Color(0.40f, 0.42f, 0.52f); break;
                case "secret": a = new Color(0.10f, 0.05f, 0.05f); b = new Color(0.36f, 0.10f, 0.10f); break;
                default: a = new Color(0.18f, 0.38f, 0.20f); b = new Color(0.30f, 0.50f, 0.25f); break;   // forest
            }
        }

        static int Hash(string s)
        {
            unchecked
            {
                int h = 23;
                foreach (char c in s) h = h * 31 + c;
                return h;
            }
        }

        static void Dot(Color[] px, int w, int h, int cx, int cy, int r, Color c)
        {
            for (int y = cy - r; y <= cy + r; y++)
                for (int x = cx - r; x <= cx + r; x++)
                {
                    if (x < 0 || y < 0 || x >= w || y >= h) continue;
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r) px[y * w + x] = c;
                }
        }

        public static Texture2D Make(GuildQuest q)
        {
            const int W = 184, H = 124;
            Color a, b;
            Palette(q.theme, out a, out b);
            int hs = Hash(q.id);
            float off = (hs & 1023) * 0.37f;

            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float n = Mathf.PerlinNoise(x * 0.06f + off, y * 0.06f + off * 0.5f);
                    float n2 = Mathf.PerlinNoise(x * 0.20f + off, y * 0.20f);
                    Color col = Color.Lerp(a, b, n * 0.7f + n2 * 0.3f);
                    int edge = Mathf.Min(Mathf.Min(x, W - 1 - x), Mathf.Min(y, H - 1 - y));
                    if (edge < 3) col *= 0.6f;
                    col.a = 1f;
                    px[y * W + x] = col;
                }

            // Kota di kiri-bawah, lokasi quest ditentukan dari hash id.
            int tx = 18, ty = 18;
            int qx = 70 + Mathf.Abs(hs / 7) % 90;
            int qy = 40 + Mathf.Abs(hs / 13) % 70;
            for (int i = 0; i <= 40; i++)
            {
                float t = i / 40f;
                float cx = Mathf.Lerp(tx, qx, t) + Mathf.Sin(t * Mathf.PI) * 14f;
                float cy = Mathf.Lerp(ty, qy, t);
                Dot(px, W, H, Mathf.RoundToInt(cx), Mathf.RoundToInt(cy), 1, new Color(0.9f, 0.82f, 0.55f, 1f));
            }
            Dot(px, W, H, tx, ty, 5, Color.white);
            Dot(px, W, H, qx, qy, 6, new Color(0.85f, 0.15f, 0.15f, 1f));
            Dot(px, W, H, qx, qy, 3, new Color(1f, 0.85f, 0.3f, 1f));

            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }
    }
}
