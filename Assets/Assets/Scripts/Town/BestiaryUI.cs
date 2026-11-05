using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HunterGuild
{
    /// <summary>
    /// Bestiary / Ensiklopedia monster (tombol B). Mencatat monster yang sudah dikalahkan, drop yang pernah didapat lewat carve,
    /// dan wilayah tempat ia dikalahkan. Hadiah kecil di tonggak penemuan; hadiah besar kalau lengkap.
    /// Data memakai stats GameState (tersimpan): bk_&lt;id&gt; kill, bl_&lt;id&gt;_&lt;wilayah&gt; kill per wilayah, bd_&lt;id&gt;_&lt;item&gt; drop.
    /// </summary>
    public class BestiaryUI : MonoBehaviour
    {
        public static BestiaryUI I { get; private set; }
        public bool IsOpen { get { return root != null && root.activeSelf; } }

        static readonly Color Gold = new Color(0.93f, 0.76f, 0.35f), GoldDim = new Color(0.55f, 0.43f, 0.2f), Panel = new Color(0.07f, 0.07f, 0.1f, 0.97f), PanelIn = new Color(0.11f, 0.11f, 0.15f, 1f);
        const int PerPage = 13;
        const string Regions = "forest,desert,swamp,tower,frontier,volcano,bone,demon";

        // tonggak: jumlah monster yang sudah ditemukan -> hadiah
        struct Reward { public string key; public float fraction; public int gold; public string item; public int itemN; public int sp; public string label; }
        static readonly Reward[] Rewards =
        {
            new Reward { key = "best_r1", fraction = 0.25f, gold = 200, item = "potion", itemN = 3, label = "25% ditemukan" },
            new Reward { key = "best_r2", fraction = 0.50f, gold = 600, item = "mana_potion", itemN = 3, label = "50% ditemukan" },
            new Reward { key = "best_r3", fraction = 0.75f, gold = 1500, item = "potion", itemN = 8, label = "75% ditemukan" },
            new Reward { key = "best_r4", fraction = 1.00f, gold = 5000, sp = 3, label = "LENGKAP (100%)" },
        };

        GameObject root;
        RectTransform listRoot, detailRoot, footRoot;
        int page, openedFrame = -1;
        string sel = "";
        List<MonsterDef> all;

        void Awake() { I = this; }
        void OnDestroy() { if (I == this) I = null; }

        void Update()
        {
            if (!IsOpen) return;
            if (Time.frameCount != openedFrame && (TownInput.BestiaryDown() || TownInput.CancelDown())) Hide();
        }

        public void Show()
        {
            if (root == null) Build();
            all = MonsterDB.All();
            root.SetActive(true);
            openedFrame = Time.frameCount;
            TownUI.I.SetExternalOpen(true);
            Refresh();
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
            if (TownUI.I != null) TownUI.I.SetExternalOpen(false);
        }

        // ------------------------------------------------------------------ data
        static int Kills(string id) { return GameState.I.Stat("bk_" + id); }
        static bool Seen(string id) { return Kills(id) > 0; }
        static bool DropKnown(string mid, string item) { return GameState.I.Stat("bd_" + mid + "_" + item) > 0; }

        int Discovered() { int n = 0; foreach (var d in all) if (Seen(d.id)) n++; return n; }

        static string ItemName(string id)
        {
            var it = ItemDB.Get(id);
            return it != null ? it.name : id;
        }

        static string RegionsOf(string id)
        {
            var parts = new List<string>();
            foreach (var r in Regions.Split(','))
            {
                int n = GameState.I.Stat("bl_" + id + "_" + r);
                if (n > 0) parts.Add(GuildQuest.RegionName(r) + " x" + n);
            }
            return parts.Count == 0 ? "-" : string.Join("\n", parts.ToArray());
        }

        // ------------------------------------------------------------------ gambar
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
            var border = Box(TownUI.I.Root, 0, 0, 1500, 880, Gold);
            var outer = border.rectTransform;
            var c = new Vector2(0.5f, 0.5f);
            UIKit.Place(outer, c, c, c, Vector2.zero, new Vector2(1500, 880));
            root = outer.gameObject;
            var bg = Box(outer, 2, 2, 1496, 876, Panel);
            Label(bg.transform, "BESTIARY", 0, 12, 1496, 44, 34, TextAnchor.MiddleCenter, Gold);
            Box(bg.transform, 40, 62, 1416, 2, GoldDim);
            var close = UIKit.Btn(bg.transform, "X", Hide, true, new Color(0.4f, 0.2f, 0.2f), 24);
            UIKit.At((RectTransform)close.transform, 1430, 12, 50, 44);
            listRoot = UIKit.Rect("List", bg.transform); UIKit.At(listRoot, 20, 74, 560, 700);
            detailRoot = UIKit.Rect("Detail", bg.transform); UIKit.At(detailRoot, 600, 74, 880, 700);
            footRoot = UIKit.Rect("Foot", bg.transform); UIKit.At(footRoot, 20, 784, 1456, 86);
        }

        void Refresh()
        {
            var gs = GameState.I;
            UIKit.Clear(listRoot); UIKit.Clear(detailRoot); UIKit.Clear(footRoot);
            int pages = Mathf.Max(1, Mathf.CeilToInt(all.Count / (float)PerPage));
            page = Mathf.Clamp(page, 0, pages - 1);
            if (sel == "" && all.Count > 0) sel = all[0].id;

            // daftar
            for (int i = 0; i < PerPage; i++)
            {
                int idx = page * PerPage + i;
                if (idx >= all.Count) break;
                var d = all[idx];
                bool seen = Seen(d.id);
                string id = d.id;
                int k = Kills(d.id);
                string label = (idx + 1).ToString("00") + "  " + (seen ? d.name : "???") + (seen ? "   (" + k + ")" + (k >= 25 ? "  *" : "") : "");
                var b = UIKit.Btn(listRoot, label, () => { sel = id; Refresh(); }, true, id == sel ? new Color(0.35f, 0.28f, 0.1f) : (seen ? new Color(0.16f, 0.17f, 0.22f) : new Color(0.1f, 0.1f, 0.12f)), 22);
                UIKit.At((RectTransform)b.transform, 0, i * 48, 560, 44);
            }
            var prev = UIKit.Btn(listRoot, "<", () => { page--; Refresh(); }, page > 0, new Color(0.2f, 0.23f, 0.3f), 24);
            UIKit.At((RectTransform)prev.transform, 0, 652, 90, 44);
            var next = UIKit.Btn(listRoot, ">", () => { page++; Refresh(); }, page < pages - 1, new Color(0.2f, 0.23f, 0.3f), 24);
            UIKit.At((RectTransform)next.transform, 470, 652, 90, 44);
            Label(listRoot, "Hal. " + (page + 1) + "/" + pages, 100, 652, 360, 44, 22, TextAnchor.MiddleCenter, Gold);

            DrawDetail();

            // kaki: progres dan hadiah
            int disc = Discovered();
            Label(footRoot, "Ditemukan " + disc + " / " + all.Count + "   (" + Mathf.RoundToInt(100f * disc / Mathf.Max(1, all.Count)) + "%)", 0, 0, 520, 40, 26, TextAnchor.MiddleLeft, Gold);
            int claimable = 0;
            Reward next2 = default(Reward); bool hasNext = false;
            foreach (var r in Rewards)
            {
                bool done = gs.flags.Contains(r.key);
                bool ok = disc >= Mathf.CeilToInt(all.Count * r.fraction);
                if (!done && ok) claimable++;
                if (!done && !ok && !hasNext) { next2 = r; hasNext = true; }
            }
            string info = claimable > 0 ? "Hadiah siap diambil!" : hasNext ? "Hadiah berikutnya: " + next2.label + " (" + Mathf.CeilToInt(all.Count * next2.fraction) + " monster)" : "Semua hadiah sudah diambil.";
            Label(footRoot, info, 0, 44, 900, 38, 22, TextAnchor.MiddleLeft, new Color(0.85f, 0.85f, 0.9f));
            var claim = UIKit.Btn(footRoot, "AMBIL HADIAH", ClaimRewards, claimable > 0, claimable > 0 ? new Color(0.5f, 0.38f, 0.1f) : new Color(0.2f, 0.2f, 0.24f), 24);
            UIKit.At((RectTransform)claim.transform, 1130, 20, 320, 56);
        }

        void DrawDetail()
        {
            var d = MonsterDB.Get(sel);
            if (d == null) return;
            var gs = GameState.I;
            Box(detailRoot, 0, 0, 880, 700, PanelIn);
            bool seen = Seen(d.id);
            if (!seen)
            {
                Label(detailRoot, "???", 0, 20, 880, 60, 44, TextAnchor.MiddleCenter, GoldDim);
                Label(detailRoot, "Kalahkan monster ini untuk mencatatnya di Bestiary.", 0, 300, 880, 40, 24, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.75f));
                return;
            }
            Label(detailRoot, d.name, 20, 14, 840, 56, 40, TextAnchor.MiddleLeft, Gold);
            string tag = (d.large ? "Monster besar / Boss" : "Monster") + "   |   Dikalahkan: " + Kills(d.id) + "x" + (Kills(d.id) >= 25 ? "   |   * Ahli" : "");
            Label(detailRoot, tag, 20, 70, 840, 34, 22, TextAnchor.MiddleLeft, new Color(0.8f, 0.8f, 0.85f));
            Box(detailRoot, 20, 108, 840, 2, GoldDim);

            // statistik: bertahap sesuai jumlah kill
            int k = Kills(d.id);
            string st = "HP " + Mathf.RoundToInt(d.hp) + (k >= 3 ? "     ATK " + Mathf.RoundToInt(d.atk) : "     ATK ???") + (k >= 5 ? "     Kecepatan " + d.speed.ToString("0.0") : "     Kecepatan ???") + (k >= 5 && d.ranged ? "     Serangan jarak jauh" : "");
            Label(detailRoot, st, 20, 120, 840, 36, 24, TextAnchor.MiddleLeft, Color.white);
            if (k < 5) Label(detailRoot, "(kalahkan lebih banyak untuk membuka seluruh data)", 20, 156, 840, 28, 18, TextAnchor.MiddleLeft, GoldDim);

            Label(detailRoot, "LOKASI", 20, 200, 400, 34, 24, TextAnchor.MiddleLeft, Gold);
            Label(detailRoot, RegionsOf(d.id), 20, 238, 400, 190, 20, TextAnchor.UpperLeft, Color.white);

            Label(detailRoot, "DROP", 450, 200, 410, 34, 24, TextAnchor.MiddleLeft, Gold);
            var seenItems = new List<string>();
            var lines = new List<string>();
            if (d.loot != null)
                foreach (var it in d.loot)
                {
                    if (seenItems.Contains(it)) continue;
                    seenItems.Add(it);
                    lines.Add(DropKnown(d.id, it) ? "- " + ItemName(it) + "  (x" + gs.Stat("bd_" + d.id + "_" + it) + ")" : "- ???");
                }
            if (!string.IsNullOrEmpty(d.rare) && !seenItems.Contains(d.rare))
                lines.Add(DropKnown(d.id, d.rare) ? "- " + ItemName(d.rare) + "  [LANGKA] (x" + gs.Stat("bd_" + d.id + "_" + d.rare) + ")" : "- ???  [LANGKA]");
            Label(detailRoot, lines.Count == 0 ? "-" : string.Join("\n", lines.ToArray()), 450, 238, 410, 300, 20, TextAnchor.UpperLeft, Color.white);
            Label(detailRoot, "ELEMEN & STATUS", 20, 440, 840, 34, 24, TextAnchor.MiddleLeft, Gold);
            Label(detailRoot, Combat2.BestiaryText(d, k, d.large), 20, 478, 420, 150, 18, TextAnchor.UpperLeft, Color.white);
            Label(detailRoot, "Drop tercatat saat kamu mengambilnya dari tanah.", 20, 640, 840, 30, 18, TextAnchor.MiddleLeft, GoldDim);
        }

        void ClaimRewards()
        {
            var gs = GameState.I;
            int disc = Discovered();
            string msg = "";
            foreach (var r in Rewards)
            {
                if (gs.flags.Contains(r.key)) continue;
                if (disc < Mathf.CeilToInt(all.Count * r.fraction)) continue;
                if (!string.IsNullOrEmpty(r.item) && !gs.HasSpace(r.item, r.itemN)) { msg = "Tas penuh; kosongkan dulu untuk mengambil hadiah."; break; }
                gs.flags.Add(r.key);
                gs.gold += r.gold;
                if (!string.IsNullOrEmpty(r.item)) gs.AddItem(r.item, r.itemN);
                gs.skillPoints += r.sp;
                msg = "Hadiah Bestiary (" + r.label + "): " + r.gold + "g" + (!string.IsNullOrEmpty(r.item) ? ", " + r.itemN + "x " + ItemName(r.item) : "") + (r.sp > 0 ? ", +" + r.sp + " SP" : "");
            }
            if (msg != "") GameState.Say(msg);
            Refresh();
        }
    }
}
