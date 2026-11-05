using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n17: menu JEDA bergaya emas-gelap (mengikuti referensi): judul dengan ornamen, baris status (Hari/jam, Rank/Lv, Gold),
    /// catatan autosave, daftar tombol (penanda berlian, panah ">" untuk submenu, sorotan emas di pilihan aktif), tombol X, dan "Esc Tutup".
    /// Dibangun lewat kode (uGUI), tanpa aset. Navigasi: mouse, W/S atau panah, E/Enter, Esc, D-pad/tombol gamepad lewat TownInput.
    /// Panel TownUI kosong dibuka di belakangnya supaya kursor, jeda, dan Esc tetap bekerja (sama seperti SettingsPanel).
    /// </summary>
    public class PausePanel : MonoBehaviour
    {
        public class Item
        {
            public string label; public Action action; public bool enabled = true;
            public bool arrow;          // submenu (tampilkan ">")
            public bool leaves;         // true = aksi membuka panel lain / menutup jeda, jadi panel ini ikut hilang
            public Item(string label, Action action, bool enabled = true, bool arrow = false, bool leaves = false)
            { this.label = label; this.action = action; this.enabled = enabled; this.arrow = arrow; this.leaves = leaves; }
        }

        static readonly Color Gold = new Color(0.88f, 0.70f, 0.30f);
        static readonly Color GoldDim = new Color(0.55f, 0.43f, 0.2f);
        static readonly Color Cream = new Color(0.94f, 0.90f, 0.80f);
        static readonly Color Dim = new Color(0.5f, 0.5f, 0.52f);
        static readonly Color Back = new Color(0.035f, 0.05f, 0.085f, 0.98f);
        static readonly Color Field = new Color(0.07f, 0.09f, 0.135f, 1f);
        static readonly Color Hot = new Color(0.30f, 0.21f, 0.06f, 1f);

        static PausePanel inst;
        public static bool IsOpen { get { return inst != null; } }

        Func<List<Item>> items; Func<string[]> stats; Func<string> note; Action onClose;
        RectTransform root; readonly List<Image> rowImgs = new List<Image>(); readonly List<Text> rowTxt = new List<Text>();
        List<Item> cur; int sel;

        /// <param name="stats">tiga pasang [label, nilai] sebagai string[6]: hari, jam, rank, level, gold...: lihat MainMenu.</param>
        public static void Open(Func<string[]> stats, Func<string> note, Func<List<Item>> items)
        {
            if (inst != null) Destroy(inst.gameObject);
            var go = new GameObject("PausePanel");
            inst = go.AddComponent<PausePanel>();
            inst.stats = stats; inst.note = note; inst.items = items;
            inst.Build();
            var ui = TownUI.I;
            if (ui != null) ui.Open("JEDA", () => "", () => new List<UIEntry> { new UIEntry(" ", () => { }, false) });
        }

        public static void Dismiss() { if (inst != null) { var g = inst.gameObject; inst = null; Destroy(g); } }

        void Update()
        {
            if (TownUI.I != null && !TownUI.I.PanelOpen) { Dismiss(); return; }
            if (cur == null) return;
            int n = TownInput.NavDown();
            if (n != 0) { Move(n); }
            if (TownInput.ConfirmDown()) Activate(sel);
        }

        void Move(int d)
        {
            for (int k = 0; k < cur.Count; k++)
            {
                sel = (sel + d + cur.Count) % cur.Count;
                if (cur[sel].enabled) break;
            }
            Paint();
        }

        void Activate(int i)
        {
            if (cur == null || i < 0 || i >= cur.Count || !cur[i].enabled) return;
            var it = cur[i];
            Audio.Sfx("ui_open");
            if (it.leaves) { var a = it.action; Dismiss(); if (a != null) a(); return; }
            if (it.action != null) it.action();
            if (inst == this) Build();        // aksi konfirmasi/simpan: gambar ulang
        }

        // ---------------------------------------------------------------- UI util
        static Font font;
        static Font F()
        {
            if (font != null) return font;
            try { font = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Cambria", "Times New Roman", "Liberation Serif", "DejaVu Serif" }, 24); } catch { }
            if (font == null)
            {
#if UNITY_2022_2_OR_NEWER
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
            }
            return font;
        }
        static RectTransform Rt(string n, Transform p) { var g = new GameObject(n, typeof(RectTransform)); g.transform.SetParent(p, false); return g.GetComponent<RectTransform>(); }
        static Image Img(Transform p, string n, Color c, bool ray = false) { var r = Rt(n, p); var i = r.gameObject.AddComponent<Image>(); i.color = c; i.raycastTarget = ray; return i; }
        static Text Txt(Transform p, string s, int size, Color c, TextAnchor a, FontStyle st = FontStyle.Normal)
        {
            var r = Rt("T", p); var t = r.gameObject.AddComponent<Text>();
            t.font = F(); t.text = s; t.fontSize = size; t.color = c; t.alignment = a; t.fontStyle = st;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false;
            return t;
        }
        static void Border(Image im, Color c, float d) { var o = im.gameObject.AddComponent<Outline>(); o.effectColor = c; o.effectDistance = new Vector2(d, d); }
        static void Full(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        // posisi bertumpu pada tengah atas induk: x tengah, y turun dari atas
        static void Place(RectTransform r, float cx, float topY, float w, float h)
        {
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f); r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = new Vector2(cx, -topY); r.sizeDelta = new Vector2(w, h);
        }
        static void Gem(Transform p, float x, float y, float s, Color c)
        {
            var g = Img(p, "Gem", c); var r = g.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f); r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(s, s); r.localRotation = Quaternion.Euler(0, 0, 45f);
        }
        static void CornerMarks(RectTransform parent, float len, float th)
        {
            for (int i = 0; i < 4; i++)
            {
                bool right = (i & 1) == 1, top = (i & 2) == 2; float px = right ? 1f : 0f, py = top ? 1f : 0f;
                for (int k = 0; k < 2; k++)
                {
                    var im = Img(parent, "Corner", Gold); var r = im.rectTransform;
                    r.anchorMin = r.anchorMax = new Vector2(px, py); r.pivot = new Vector2(px, py);
                    r.sizeDelta = k == 0 ? new Vector2(len, th) : new Vector2(th, len); r.anchoredPosition = Vector2.zero;
                }
            }
        }

        // ---------------------------------------------------------------- build
        const float W = 780f, RowGap = 9f;
        float RowH = 62f;
        public static void Reselect() { if (inst != null) inst.sel = 0; }

        void Build()
        {
            if (root != null) Destroy(root.gameObject);
            if (GetComponent<Canvas>() == null)
            {
                var cv = gameObject.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 600;
                var sc = gameObject.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
                gameObject.AddComponent<GraphicRaycaster>();
            }
            root = Rt("Root", transform); Full(root);
            var dim = Img(root, "Dim", new Color(0.01f, 0.015f, 0.03f, 0.55f), true); Full(dim.rectTransform);

            cur = items();
            if (sel >= cur.Count || sel < 0 || !cur[sel].enabled) { sel = 0; while (sel < cur.Count - 1 && !cur[sel].enabled) sel++; }
            // baris menyusut bila daftar panjang agar jendela selalu muat di layar (referensi 1080)
            float fixedH = 112f + 84f + 56f + 14f + 22f + 70f;
            RowH = Mathf.Clamp((1030f - fixedH - (cur.Count - 1) * RowGap) / Mathf.Max(1, cur.Count), 40f, 62f);
            float listH = cur.Count * RowH + (cur.Count - 1) * RowGap;
            float H = 112f + 84f + 56f + 14f + listH + 22f + 70f;

            var win = Img(root, "Window", Back, true); var wr = win.rectTransform;
            wr.anchorMin = wr.anchorMax = wr.pivot = new Vector2(0.5f, 0.5f); wr.sizeDelta = new Vector2(W, H);
            Border(win, GoldDim, 2f); CornerMarks(wr, 38f, 4f);

            // judul + ornamen
            var title = Txt(win.transform, "JEDA", 62, new Color(0.96f, 0.78f, 0.36f), TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(title.rectTransform, 0, 22f, 300f, 76f);
            foreach (int sgn in new[] { -1, 1 })
            {
                var ln = Img(win.transform, "Ln", GoldDim); Place(ln.rectTransform, sgn * 200f, 60f, 230f, 2f);
                Gem(win.transform, sgn * 106f, 61f, 14f, Gold);
            }
            // tombol X
            var x = Img(win.transform, "Close", Field, true); Border(x, Gold, 2f);
            var xr = x.rectTransform; xr.anchorMin = xr.anchorMax = new Vector2(1f, 1f); xr.anchoredPosition = new Vector2(-46f, -46f); xr.sizeDelta = new Vector2(44, 44); xr.localRotation = Quaternion.Euler(0, 0, 45f);
            var xb = x.gameObject.AddComponent<Button>(); xb.targetGraphic = x; xb.onClick.AddListener(() => { Audio.Sfx("ui_open"); if (TownUI.I != null) TownUI.I.Close(); });
            var xt = Txt(x.transform, "X", 26, Cream, TextAnchor.MiddleCenter, FontStyle.Bold); Full(xt.rectTransform); xt.rectTransform.localRotation = Quaternion.Euler(0, 0, -45f);

            // baris status
            var st = stats();
            var bar = Img(win.transform, "Stats", new Color(0.045f, 0.06f, 0.1f, 1f)); Border(bar, GoldDim * 0.7f, 1f);
            Place(bar.rectTransform, 0, 112f, W - 70f, 76f);
            float cw = (W - 70f) / 3f;
            for (int i = 0; i < 3; i++)
            {
                float cx = -cw + i * cw;
                Gem(bar.transform, cx - cw / 2f + 30f, 38f, 16f, Gold);
                var a = Txt(bar.transform, st[i * 2], 28, Cream, TextAnchor.MiddleLeft); Place(a.rectTransform, cx + 25f, 8f, cw - 60f, 32f);
                var b = Txt(bar.transform, st[i * 2 + 1], 26, new Color(0.8f, 0.76f, 0.66f), TextAnchor.MiddleLeft); Place(b.rectTransform, cx + 25f, 38f, cw - 60f, 30f);
                if (i > 0) { var dv = Img(bar.transform, "Dv", GoldDim * 0.8f); Place(dv.rectTransform, cx - cw / 2f, 12f, 1.5f, 52f); }
            }

            // catatan autosave
            Gem(win.transform, -W / 2f + 54f, 112f + 84f + 28f, 12f, GoldDim);
            var nt = Txt(win.transform, note(), 25, new Color(0.85f, 0.8f, 0.68f), TextAnchor.MiddleLeft); Place(nt.rectTransform, 24f, 112f + 84f + 10f, W - 130f, 36f);
            var nl = Img(win.transform, "NLn", GoldDim * 0.6f); Place(nl.rectTransform, 0, 112f + 84f + 52f, W - 70f, 1.5f);

            // daftar tombol
            rowImgs.Clear(); rowTxt.Clear();
            float y0 = 112f + 84f + 56f + 14f;
            for (int i = 0; i < cur.Count; i++)
            {
                int k = i; var it = cur[i];
                var row = Img(win.transform, "Row" + i, Field, true); Border(row, GoldDim * 0.8f, 1.5f);
                Place(row.rectTransform, 0, y0 + i * (RowH + RowGap), W - 140f, RowH);
                var rb = row.gameObject.AddComponent<Button>(); rb.targetGraphic = row; rb.transition = Selectable.Transition.None;
                rb.onClick.AddListener(() => Activate(k));
                var trig = row.gameObject.AddComponent<EventTrigger>();
                var en = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter }; en.callback.AddListener(_ => { if (cur != null && k < cur.Count && cur[k].enabled) { sel = k; Paint(); } });
                trig.triggers.Add(en);
                Gem(row.transform, -(W - 140f) / 2f + 44f, RowH / 2f, 16f, it.enabled ? Gold : GoldDim * 0.6f);
                var t = Txt(row.transform, it.label, 30, it.enabled ? Cream : Dim, TextAnchor.MiddleCenter); Full(t.rectTransform);
                if (it.arrow)
                {
                    var ar = Txt(row.transform, ">", 34, it.enabled ? Gold : Dim, TextAnchor.MiddleRight, FontStyle.Bold);
                    var r = ar.rectTransform; r.anchorMin = new Vector2(1, 0); r.anchorMax = new Vector2(1, 1); r.pivot = new Vector2(1, 0.5f); r.sizeDelta = new Vector2(60, 0); r.anchoredPosition = new Vector2(-26, 0);
                }
                rowImgs.Add(row); rowTxt.Add(t);
            }

            // kaki: Esc Tutup
            float fy = H - 54f;
            foreach (int sgn in new[] { -1, 1 })
            {
                var ln = Img(win.transform, "FLn", GoldDim); Place(ln.rectTransform, sgn * 235f, fy + 20f, 190f, 2f);
                Gem(win.transform, sgn * 128f, fy + 21f, 12f, GoldDim);
            }
            var kb = Img(win.transform, "Key", Field); Border(kb, Cream, 1.5f); Place(kb.rectTransform, -52f, fy + 2f, 76f, 38f);
            var kt = Txt(kb.transform, "Esc", 24, Cream, TextAnchor.MiddleCenter); Full(kt.rectTransform);
            var ct = Txt(win.transform, "Tutup", 26, Cream, TextAnchor.MiddleLeft); Place(ct.rectTransform, 36f, fy + 5f, 120f, 34f);

            Paint();
        }

        void Paint()
        {
            for (int i = 0; i < rowImgs.Count; i++)
            {
                bool on = i == sel && cur[i].enabled;
                rowImgs[i].color = on ? Hot : Field;
                var o = rowImgs[i].GetComponent<Outline>(); if (o != null) { o.effectColor = on ? new Color(1f, 0.82f, 0.38f) : GoldDim * 0.8f; o.effectDistance = on ? new Vector2(2.5f, 2.5f) : new Vector2(1.5f, 1.5f); }
                rowTxt[i].color = !cur[i].enabled ? Dim : on ? new Color(1f, 0.93f, 0.7f) : Cream;
            }
        }
    }
}
