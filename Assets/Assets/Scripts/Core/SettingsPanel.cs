using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n8: panel Pengaturan bergaya emas-gelap (sidebar tab + bagian Umum / Grafis / Audio / Kontrol / Gameplay / Bahasa / Akun,
    /// tombol "Kembali ke Default" dan "Terapkan"). Dibangun penuh lewat kode (uGUI), tanpa aset. Semua perubahan langsung berlaku;
    /// "Terapkan" menutup panel. Dropdown = tombol yang berganti pilihan tiap diklik (bukan daftar popup).
    /// Dibuka oleh MainMenu.ShowSettings. Sebuah panel TownUI kosong dibuka di belakangnya agar kursor, jeda, dan tombol Esc tetap bekerja.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        static SettingsPanel inst;

        // ---------- palet
        static readonly Color Gold = new Color(0.88f, 0.70f, 0.30f);
        static readonly Color GoldDim = new Color(0.55f, 0.43f, 0.2f);
        static readonly Color Cream = new Color(0.94f, 0.90f, 0.80f);
        static readonly Color Dim = new Color(0.5f, 0.5f, 0.52f);
        static readonly Color Back = new Color(0.035f, 0.05f, 0.085f, 0.97f);
        static readonly Color Panel = new Color(0.05f, 0.068f, 0.105f, 0.98f);
        static readonly Color Field = new Color(0.085f, 0.105f, 0.15f, 1f);

        Action back, openControls;
        RectTransform content, viewport;
        ScrollRect scroll;
        Text status;
        readonly List<Action> refreshers = new List<Action>();
        readonly List<RectTransform> blocks = new List<RectTransform>();
        readonly List<Image> tabImgs = new List<Image>();
        readonly List<Text> tabTxts = new List<Text>();
        int activeTab; bool confirmQuit, confirmPerma;

        public static bool IsOpen { get { return inst != null; } }

        public static void Open(Action back, Action openControls)
        {
            if (inst != null) Destroy(inst.gameObject);
            var go = new GameObject("SettingsPanel");
            inst = go.AddComponent<SettingsPanel>();
            inst.back = back; inst.openControls = openControls;
            inst.Build();
            var ui = TownUI.I;
            if (ui != null)
                ui.Open("PENGATURAN", () => "", () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("< Kembali", () => { if (inst != null) inst.Finish(true); }));
                    return l;
                });
        }

        void Update()
        {
            // panel TownUI di belakang ditutup (Esc / tombol B): tutup juga panel ini
            if (TownUI.I != null && !TownUI.I.PanelOpen) { Finish(false); return; }
        }

        void OnDestroy() { if (inst == this) inst = null; }

        void Finish(bool goBack)
        {
            var b = back;
            if (inst == this) inst = null;
            Destroy(gameObject);
            if (goBack && b != null) b();
        }

        // =============================================================== util UI
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

        static RectTransform Rt(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        static Image Img(Transform parent, string name, Color c)
        {
            var rt = Rt(name, parent);
            var im = rt.gameObject.AddComponent<Image>();
            im.color = c; im.raycastTarget = true;
            return im;
        }

        static void Anchor(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0); rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        static void Fill(RectTransform rt, float l, float b, float r, float t)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        }

        static Text Txt(Transform parent, string s, int size, Color c, TextAnchor a, FontStyle st = FontStyle.Normal)
        {
            var rt = Rt("T", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = F(); t.text = s; t.fontSize = size; t.color = c; t.alignment = a; t.fontStyle = st;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        static void Border(Image im, Color c, float d = 1.5f)
        {
            var o = im.gameObject.AddComponent<Outline>();
            o.effectColor = c; o.effectDistance = new Vector2(d, d);
        }

        /// <summary>Siku-siku emas di empat sudut (hiasan bingkai).</summary>
        static void Corners(RectTransform parent, float len = 22f, float th = 3f)
        {
            for (int i = 0; i < 4; i++)
            {
                bool right = (i & 1) == 1, top = (i & 2) == 2;
                float px = right ? 1f : 0f, py = top ? 1f : 0f;
                for (int k = 0; k < 2; k++)
                {
                    var im = Img(parent, "Corner", Gold); im.raycastTarget = false;
                    var rt = im.rectTransform;
                    rt.anchorMin = rt.anchorMax = new Vector2(px, py);
                    rt.pivot = new Vector2(px, py);
                    rt.sizeDelta = k == 0 ? new Vector2(len, th) : new Vector2(th, len);
                    rt.anchoredPosition = Vector2.zero;
                }
            }
        }

        static LayoutElement Le(RectTransform rt, float minW = -1f, float prefW = -1f, float flexW = -1f, float prefH = -1f)
        {
            var le = rt.gameObject.GetComponent<LayoutElement>(); if (le == null) le = rt.gameObject.AddComponent<LayoutElement>();
            if (minW >= 0f) le.minWidth = minW; if (prefW >= 0f) le.preferredWidth = prefW; if (flexW >= 0f) le.flexibleWidth = flexW; if (prefH >= 0f) { le.preferredHeight = prefH; le.minHeight = prefH; }
            return le;
        }

        static VerticalLayoutGroup Vlg(RectTransform rt, int l, int r, int t, int b, float spacing)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(l, r, t, b); v.spacing = spacing;
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return v;
        }

        static HorizontalLayoutGroup Hlg(RectTransform rt, int l, int r, int t, int b, float spacing)
        {
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(l, r, t, b); h.spacing = spacing;
            h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = true;
            h.childAlignment = TextAnchor.MiddleLeft;
            return h;
        }

        void Refresh() { for (int i = 0; i < refreshers.Count; i++) refreshers[i](); }
        void Say(string s) { if (status != null) status.text = s; }

        // =============================================================== build
        void Build()
        {
            var cv = gameObject.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 600;
            var sc = gameObject.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            var dim = Img(transform, "Dim", new Color(0.01f, 0.015f, 0.03f, 0.82f)); Anchor(dim.rectTransform, 0, 0, 1, 1);

            var win = Img(transform, "Window", Back); Anchor(win.rectTransform, 0.03f, 0.035f, 0.97f, 0.965f);
            Border(win, GoldDim, 2f); Corners(win.rectTransform, 34f, 4f);

            // header
            var head = Rt("Header", win.transform); Anchor(head, 0, 0.915f, 1, 1f);
            var title = Txt(head, "Pengaturan", 46, Cream, TextAnchor.MiddleLeft, FontStyle.Bold);
            Anchor(title.rectTransform, 0.06f, 0, 0.7f, 1);
            var dia = Img(head, "Gem", Gold); dia.raycastTarget = false; dia.rectTransform.anchorMin = dia.rectTransform.anchorMax = new Vector2(0.035f, 0.5f);
            dia.rectTransform.sizeDelta = new Vector2(26, 26); dia.rectTransform.localRotation = Quaternion.Euler(0, 0, 45f);
            var line = Img(head, "Line", GoldDim); line.raycastTarget = false; Anchor(line.rectTransform, 0.01f, 0, 0.99f, 0); line.rectTransform.sizeDelta = new Vector2(0, 2);
            var x = Img(head, "Close", Field); Border(x, Gold, 2f);
            x.rectTransform.anchorMin = x.rectTransform.anchorMax = new Vector2(0.972f, 0.5f); x.rectTransform.sizeDelta = new Vector2(64, 64); x.rectTransform.localRotation = Quaternion.Euler(0, 0, 45f);
            var xb = x.gameObject.AddComponent<Button>(); xb.targetGraphic = x; xb.onClick.AddListener(() => Finish(true));
            var xt = Txt(x.transform, "X", 34, Cream, TextAnchor.MiddleCenter, FontStyle.Bold); Anchor(xt.rectTransform, 0, 0, 1, 1); xt.rectTransform.localRotation = Quaternion.Euler(0, 0, -45f);

            // sidebar
            var side = Img(win.transform, "Sidebar", Panel); Anchor(side.rectTransform, 0.012f, 0.1f, 0.205f, 0.895f); Border(side, GoldDim, 1.5f); Corners(side.rectTransform, 14f, 2f);
            Vlg(side.rectTransform, 16, 16, 18, 18, 12);
            string[] tabs = { "Umum", "Grafis", "Audio", "Kontrol", "Gameplay", "Bahasa", "Simpanan" };
            int[] jump = { 0, 1, 2, 2, 3, 4, 5 };
            for (int i = 0; i < tabs.Length; i++)
            {
                int k = i;
                var tb = Img(side.transform, "Tab" + tabs[i], Field); Le(tb.rectTransform, prefH: 70f); Border(tb, GoldDim * 0.7f, 1f);
                var bt = tb.gameObject.AddComponent<Button>(); bt.targetGraphic = tb;
                bt.onClick.AddListener(() => { SelectTab(k); Jump(jump[k]); });
                var d = Img(tb.transform, "Dot", GoldDim); d.raycastTarget = false; d.rectTransform.anchorMin = d.rectTransform.anchorMax = new Vector2(0.1f, 0.5f);
                d.rectTransform.sizeDelta = new Vector2(12, 12); d.rectTransform.localRotation = Quaternion.Euler(0, 0, 45f);
                var tt = Txt(tb.transform, tabs[i], 30, Cream, TextAnchor.MiddleLeft); Anchor(tt.rectTransform, 0.2f, 0, 1, 1);
                tabImgs.Add(tb); tabTxts.Add(tt);
            }

            // isi (scroll)
            var area = Img(win.transform, "Content", new Color(0, 0, 0, 0)); area.raycastTarget = true; Anchor(area.rectTransform, 0.215f, 0.1f, 0.988f, 0.895f);
            viewport = Rt("Viewport", area.transform); Anchor(viewport, 0, 0, 1, 1);
            viewport.gameObject.AddComponent<RectMask2D>();
            content = Rt("List", viewport); content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1f); content.offsetMin = new Vector2(0, 0); content.offsetMax = new Vector2(0, 0);
            Vlg(content, 6, 14, 4, 20, 18);
            var fit = content.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll = area.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40f;

            BuildSections();

            // footer
            var foot = Rt("Footer", win.transform); Anchor(foot, 0, 0, 1, 0.095f);
            var def = Img(foot, "Default", Field); Border(def, GoldDim, 1.5f); Anchor(def.rectTransform, 0.012f, 0.14f, 0.235f, 0.86f);
            var db = def.gameObject.AddComponent<Button>(); db.targetGraphic = def; db.onClick.AddListener(ResetDefaults);
            var dt = Txt(def.transform, "Kembali ke Default", 28, Cream, TextAnchor.MiddleCenter); Anchor(dt.rectTransform, 0, 0, 1, 1);
            status = Txt(foot, "", 26, GoldDim, TextAnchor.MiddleCenter, FontStyle.Italic); Anchor(status.rectTransform, 0.25f, 0, 0.72f, 1);
            var ap = Img(foot, "Apply", new Color(0.86f, 0.66f, 0.22f)); Border(ap, new Color(1f, 0.88f, 0.5f), 2f); Anchor(ap.rectTransform, 0.76f, 0.1f, 0.988f, 0.9f);
            var ab = ap.gameObject.AddComponent<Button>(); ab.targetGraphic = ap; ab.onClick.AddListener(() => { Audio.Sfx("ui_open"); Finish(true); });
            var at = Txt(ap.transform, "Terapkan", 34, new Color(0.12f, 0.07f, 0.01f), TextAnchor.MiddleCenter, FontStyle.Bold); Anchor(at.rectTransform, 0, 0, 1, 1);

            SelectTab(0);
            Refresh();
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 1f;
        }

        void SelectTab(int i)
        {
            activeTab = i;
            for (int k = 0; k < tabImgs.Count; k++)
            {
                bool on = k == i;
                tabImgs[k].color = on ? new Color(0.30f, 0.22f, 0.07f, 1f) : Field;
                tabTxts[k].color = on ? new Color(1f, 0.9f, 0.6f) : Cream;
                var o = tabImgs[k].GetComponent<Outline>(); if (o != null) o.effectColor = on ? Gold : GoldDim * 0.7f;
            }
        }

        void Jump(int block)
        {
            if (block < 0 || block >= blocks.Count) return;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            var b = blocks[block];
            float topLocal = b.localPosition.y + (1f - b.pivot.y) * b.rect.height;
            float y = -topLocal;
            float max = Mathf.Max(1f, content.rect.height - viewport.rect.height);
            scroll.verticalNormalizedPosition = 1f - Mathf.Clamp01(y / max);
        }

        // =============================================================== bagian
        RectTransform Section(string title, float height, Transform parent, bool register)
        {
            var sec = Img(parent, "Sec_" + title, Panel); sec.raycastTarget = false; Border(sec, GoldDim, 1.5f); Corners(sec.rectTransform, 16f, 2.5f);
            Le(sec.rectTransform, prefH: height, flexW: 1f);
            if (register) blocks.Add(sec.rectTransform);
            var tt = Txt(sec.transform, title, 34, Cream, TextAnchor.MiddleLeft, FontStyle.Bold);
            tt.rectTransform.anchorMin = new Vector2(0, 1); tt.rectTransform.anchorMax = new Vector2(1, 1); tt.rectTransform.pivot = new Vector2(0, 1);
            tt.rectTransform.anchoredPosition = new Vector2(60, -8); tt.rectTransform.sizeDelta = new Vector2(-70, 52);
            var gem = Img(sec.transform, "Gem", Gold); gem.raycastTarget = false; gem.rectTransform.anchorMin = gem.rectTransform.anchorMax = new Vector2(0, 1);
            gem.rectTransform.anchoredPosition = new Vector2(34, -34); gem.rectTransform.sizeDelta = new Vector2(14, 14); gem.rectTransform.localRotation = Quaternion.Euler(0, 0, 45f);
            var ln = Img(sec.transform, "Ln", GoldDim * 0.8f); ln.raycastTarget = false;
            ln.rectTransform.anchorMin = new Vector2(0, 1); ln.rectTransform.anchorMax = new Vector2(1, 1); ln.rectTransform.pivot = new Vector2(0.5f, 1);
            ln.rectTransform.anchoredPosition = new Vector2(0, -58); ln.rectTransform.sizeDelta = new Vector2(-48, 1.5f);
            var body = Rt("Body", sec.transform); Fill(body, 26, 12, 26, 66);
            return body;
        }

        RectTransform Columns(RectTransform body, int n)
        {
            var h = Hlg(body, 0, 0, 0, 0, 44); h.childForceExpandWidth = true;
            return body;
        }

        RectTransform Column(RectTransform cols)
        {
            var c = Rt("Col", cols); Le(c, flexW: 1f);
            Vlg(c, 0, 0, 0, 0, 0);
            return c;
        }

        const float RowH = 46f;

        RectTransform Row(Transform parent, string label, bool enabled = true, string tag = null)
        {
            var row = Rt("Row", parent); Le(row, prefH: RowH, flexW: 1f);
            Hlg(row, 4, 4, 2, 2, 12);
            var ln = Img(row, "Ln", new Color(0.4f, 0.32f, 0.16f, 0.35f)); ln.raycastTarget = false;
            ln.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            ln.rectTransform.anchorMin = new Vector2(0, 0); ln.rectTransform.anchorMax = new Vector2(1, 0); ln.rectTransform.pivot = new Vector2(0.5f, 0); ln.rectTransform.sizeDelta = new Vector2(0, 1);
            var t = Txt(row, label + (tag != null ? "  (" + tag + ")" : ""), 27, enabled ? Cream : Dim, TextAnchor.MiddleLeft);
            Le(t.rectTransform, minW: 140f, flexW: 1f);
            return row;
        }

        void Cycle(Transform parent, string label, Func<string> get, Action step, bool enabled = true, string tag = null, float width = 290f)
        {
            var row = Row(parent, label, enabled, tag);
            var b = Img(row, "Cycle", Field); Border(b, enabled ? GoldDim : GoldDim * 0.4f, 1.5f); Le(b.rectTransform, prefW: width);
            var tx = Txt(b.transform, "", 26, enabled ? Cream : Dim, TextAnchor.MiddleLeft); Fill(tx.rectTransform, 16, 0, 44, 0);
            var ch = Txt(b.transform, "v", 24, enabled ? Gold : Dim, TextAnchor.MiddleCenter, FontStyle.Bold); ch.rectTransform.anchorMin = new Vector2(1, 0); ch.rectTransform.anchorMax = new Vector2(1, 1); ch.rectTransform.pivot = new Vector2(1, 0.5f); ch.rectTransform.sizeDelta = new Vector2(40, 0); ch.rectTransform.anchoredPosition = Vector2.zero;
            if (enabled)
            {
                var bt = b.gameObject.AddComponent<Button>(); bt.targetGraphic = b;
                bt.onClick.AddListener(() => { step(); Audio.Sfx("ui_open"); Refresh(); });
            }
            refreshers.Add(() => { if (tx != null) tx.text = get(); });
        }

        void Toggle(Transform parent, string label, Func<bool> get, Action<bool> set, bool enabled = true, string tag = null)
        {
            var row = Row(parent, label, enabled, tag);
            var holder = Rt("Tg", row); Le(holder, prefW: 74f);
            var tr = Img(holder, "Track", Dim); Anchor(tr.rectTransform, 0, 0.5f, 1, 0.5f); tr.rectTransform.sizeDelta = new Vector2(0, 30);
            var kn = Img(tr.transform, "Knob", Cream); kn.raycastTarget = false; kn.rectTransform.anchorMin = kn.rectTransform.anchorMax = new Vector2(0, 0.5f); kn.rectTransform.pivot = new Vector2(0, 0.5f); kn.rectTransform.sizeDelta = new Vector2(24, 24);
            if (enabled)
            {
                var bt = tr.gameObject.AddComponent<Button>(); bt.targetGraphic = tr;
                bt.onClick.AddListener(() => { set(!get()); Audio.Sfx("ui_open"); Refresh(); });
            }
            refreshers.Add(() =>
            {
                bool on = get();
                tr.color = !enabled ? new Color(0.25f, 0.25f, 0.27f) : on ? new Color(0.92f, 0.72f, 0.28f) : new Color(0.45f, 0.45f, 0.48f);
                kn.rectTransform.anchoredPosition = new Vector2(on ? 47f : 3f, 0f);
            });
        }

        void Slide(Transform parent, string label, float min, float max, Func<float> get, Action<float> set, string suffix = "")
        {
            var row = Row(parent, label);
            // lintasan + isi + pegangan belah ketupat
            var sl = Rt("Slider", row); Le(sl, minW: 120f, flexW: 1.35f);
            var bg = Img(sl, "Bg", new Color(0.2f, 0.2f, 0.22f)); Anchor(bg.rectTransform, 0, 0.5f, 1, 0.5f); bg.rectTransform.sizeDelta = new Vector2(-16, 6); bg.raycastTarget = false;
            var fa = Rt("FillArea", sl); Anchor(fa, 0, 0, 1, 1); fa.offsetMin = new Vector2(8, 0); fa.offsetMax = new Vector2(-8, 0);
            var fill = Img(fa, "Fill", Gold); fill.raycastTarget = false; fill.rectTransform.anchorMin = new Vector2(0, 0.5f); fill.rectTransform.anchorMax = new Vector2(0, 0.5f); fill.rectTransform.sizeDelta = new Vector2(0, 6);
            var ha = Rt("HandleArea", sl); Anchor(ha, 0, 0, 1, 1); ha.offsetMin = new Vector2(10, 0); ha.offsetMax = new Vector2(-10, 0);
            var hd = Img(ha, "Handle", new Color(1f, 0.86f, 0.5f)); Border(hd, new Color(0.35f, 0.25f, 0.05f), 1.5f);
            hd.rectTransform.sizeDelta = new Vector2(22, 22); hd.rectTransform.localRotation = Quaternion.Euler(0, 0, 45f);
            var s = sl.gameObject.AddComponent<Slider>();
            s.fillRect = fill.rectTransform; s.handleRect = hd.rectTransform; s.targetGraphic = hd; s.direction = Slider.Direction.LeftToRight;
            s.minValue = min; s.maxValue = max; s.wholeNumbers = true;
            var vt = Txt(row, "", 27, Cream, TextAnchor.MiddleRight); Le(vt.rectTransform, prefW: 74f);
            bool busy = false;
            s.onValueChanged.AddListener(v => { if (busy) return; set(v); Refresh(); });
            refreshers.Add(() =>
            {
                float v = get(); busy = true; s.SetValueWithoutNotify(Mathf.Clamp(v, min, max)); busy = false;
                vt.text = Mathf.RoundToInt(v) + suffix;
            });
        }

        void Action_(Transform parent, string label, Func<string> btnText, Action act, bool enabled = true, string tag = null)
        {
            var row = Row(parent, label, enabled, tag);
            var b = Img(row, "Btn", Field); Border(b, enabled ? Gold : GoldDim * 0.4f, 1.5f); Le(b.rectTransform, prefW: 290f);
            var tx = Txt(b.transform, "", 26, enabled ? Cream : Dim, TextAnchor.MiddleCenter); Anchor(tx.rectTransform, 0, 0, 1, 1);
            if (enabled)
            {
                var bt = b.gameObject.AddComponent<Button>(); bt.targetGraphic = b;
                bt.onClick.AddListener(() => { act(); Refresh(); });
            }
            refreshers.Add(() => { if (tx != null) tx.text = btnText(); });
        }

        void Info(Transform parent, string label, Func<string> val)
        {
            var row = Row(parent, label);
            var t = Txt(row, "", 27, Gold, TextAnchor.MiddleRight); Le(t.rectTransform, prefW: 290f);
            refreshers.Add(() => { if (t != null) t.text = val(); });
        }

        // =============================================================== isi tiap bagian
        void BuildSections()
        {
            // ---------------- Umum
            var umum = Section("Umum", 66 + 6 * RowH + 18, content, true);
            Vlg(umum, 0, 0, 0, 0, 0);
            Cycle(umum, "Mode Tampilan", () => GameSettings.Fullscreen ? "Layar Penuh" : "Berjendela", () => GameSettings.ToggleFullscreen());
            Cycle(umum, "Resolusi", () => GameSettings.ResLabel, () => GameSettings.CycleResolution(1));
            Cycle(umum, "Batas Frame Rate", () => GameSettings.FpsLabel, () => { GameSettings.CycleFps(); SettingsExtra.ApplyVSync(); });
            Toggle(umum, "V-Sync", () => SettingsExtra.VSync, v => SettingsExtra.VSync = v);
            Slide(umum, "Kecerahan", 0, 100, () => SettingsExtra.Brightness, v => SettingsExtra.Brightness = Mathf.RoundToInt(v));
            Slide(umum, "Field of View (FOV)", 50, 100, () => SettingsExtra.Fov, v => SettingsExtra.Fov = Mathf.RoundToInt(v));

            // ---------------- Grafis
            var grafis = Section("Grafis", 66 + 5 * RowH + 18, content, true);
            var cols = Columns(grafis, 2);
            var c1 = Column(cols); var c2 = Column(cols);
            Cycle(c1, "Kualitas Grafis", () => new[] { "Standar", "Sedang", "Tinggi" }[GameSettings.Graphics], () => { GameSettings.SetGraphics((GameSettings.Graphics + 1) % 3); SettingsExtra.ApplyQuality(); SettingsExtra.ApplyAa(); }, width: 210f);
            Cycle(c1, "Kualitas Bayangan", () => SettingsExtra.ShadowNames[SettingsExtra.ShadowQ], () => SettingsExtra.ShadowQ = (SettingsExtra.ShadowQ + 1) % 5, width: 210f);
            Cycle(c1, "Kualitas Efek", () => SettingsExtra.LevelNames[FxQuality.Level], () => FxQuality.Level = (FxQuality.Level + 1) % 3, width: 210f);
            Cycle(c1, "Kualitas Tekstur", () => SettingsExtra.LevelNames[SettingsExtra.TextureQ], () => SettingsExtra.TextureQ = (SettingsExtra.TextureQ + 1) % 3, width: 210f);
            Cycle(c1, "Skala Render", () => GameSettings.ScaleLabel, () => GameSettings.CycleScale(), width: 210f);
            Cycle(c2, "Anti-Aliasing", () => SettingsExtra.AaNames[SettingsExtra.Aa], () => SettingsExtra.Aa = (SettingsExtra.Aa + 1) % 5, width: 210f);
            Toggle(c2, "Motion Blur", () => SettingsExtra.MotionBlurOn, v => SettingsExtra.MotionBlurOn = v);
            Toggle(c2, "Depth of Field", () => SettingsExtra.DofOn, v => SettingsExtra.DofOn = v);
            Toggle(c2, "Bloom", () => SettingsExtra.BloomOn, v => SettingsExtra.BloomOn = v);
            Toggle(c2, "Detail Dunia (bunga, rumput 3D, kupu-kupu)", () => WorldDetail.Level > 0, v => WorldDetail.Level = v ? 2 : 0);
            Toggle(c2, "Langit & Warna Anime Fantasy", () => FantasyGrade.On, v => FantasyGrade.On = v);

            // ---------------- Audio | Kontrol (berdampingan)
            var pair = Rt("Pair", content); Le(pair, prefH: 66 + 5 * RowH + 18, flexW: 1f);
            Hlg(pair, 0, 0, 0, 0, 18).childForceExpandWidth = true;
            blocks.Add(pair);
            var audio = Section("Audio", 66 + 5 * RowH + 18, pair, false);
            Vlg(audio, 0, 0, 0, 0, 0);
            Slide(audio, "Volume Utama", 0, 100, () => SettingsExtra.MasterVol * 100f, v => SettingsExtra.MasterVol = v / 100f);
            Slide(audio, "Volume Musik", 0, 100, () => Vol(0) * 100f, v => SetVol(0, v / 100f));
            Slide(audio, "Volume Efek", 0, 100, () => Vol(1) * 100f, v => SetVol(1, v / 100f));
            Slide(audio, "Volume Suara NPC", 0, 100, () => Vol(2) * 100f, v => SetVol(2, v / 100f));
            Action_(audio, "Tes suara", () => "Putar efek", () => Audio.Sfx("hit_heavy"));
            var kontrol = Section("Kontrol", 66 + 5 * RowH + 18, pair, false);
            Vlg(kontrol, 0, 0, 0, 0, 0);
            Slide(kontrol, "Sensitivitas Mouse", 30, 300, () => Bindings.MouseSens * 100f, v => { PlayerPrefs.SetFloat("hg_msens", Mathf.Round(v / 10f) / 10f); PlayerPrefs.Save(); }, "%");
            Slide(kontrol, "Sensitivitas Gamepad", 30, 300, () => Bindings.PadSens * 100f, v => { PlayerPrefs.SetFloat("hg_psens", Mathf.Round(v / 10f) / 10f); PlayerPrefs.Save(); }, "%");
            Toggle(kontrol, "Inversi Kamera (Vertikal)", () => Bindings.InvertY, v => { if (v != Bindings.InvertY) Bindings.ToggleInvertY(); });
            Toggle(kontrol, "Getar (Controller)", () => SettingsExtra.Rumble, v => { SettingsExtra.Rumble = v; if (v) SettingsExtra.Pulse(0.4f, 0.6f, 0.25f); });
            Action_(kontrol, "Pemetaan tombol", () => "Ganti tombol >", () => { var oc = openControls; Finishing(); if (oc != null) oc(); });

            // ---------------- Gameplay
            var gp = Section("Gameplay", 66 + 9 * RowH + 18, content, true);
            var gc = Columns(gp, 2);
            var g1 = Column(gc); var g2 = Column(gc);
            Cycle(g1, "Skala UI", () => Mathf.RoundToInt(UiSettings.Scale * 100f) + "%", () => { if (UiSettings.Scale >= 1.29f) { for (int i = 0; i < 5; i++) UiSettings.StepScale(-1); } else UiSettings.StepScale(1); }, width: 200f);
            Cycle(g1, "Skala Teks", () => Mathf.RoundToInt(UiSettings.TextScale * 100f) + "%", () => { if (UiSettings.TextScale >= 1.19f) { for (int i = 0; i < 6; i++) UiSettings.StepText(-1); } else UiSettings.StepText(1); }, width: 200f);
            Cycle(g1, "Tema Warna UI", () => UiSettings.ThemeNames[UiSettings.Theme], () => UiSettings.CycleTheme(), width: 200f);
            Toggle(g1, "HUD Redup Saat Tenang", () => UiSettings.HudFade, v => { if (v != UiSettings.HudFade) UiSettings.ToggleFade(); });
            Cycle(g1, "Palet Penanda (aksesibilitas)", () => Access.PaletteNames[Access.Palette], () => Access.CyclePalette(), width: 230f);
            Toggle(g1, "Mode Tahan Otomatis (memancing)", () => Access.ToggleHold, v => Access.ToggleHold = v);
            Toggle(g1, "Animasi Panel", () => UiSettings.Anim, v => { if (v != UiSettings.Anim) UiSettings.ToggleAnim(); });
            Toggle(g2, "Ambil Loot Jarak Jauh (auto-loot luas)", () => Access.WideLoot, v => Access.WideLoot = v);
            Toggle(g2, "Timing Mudah (bar lambat & lebar)", () => Access.EasyTiming, v => Access.EasyTiming = v);
            Toggle(g2, "Tampilkan FPS", () => GameSettings.ShowFps, v => { if (v != GameSettings.ShowFps) GameSettings.ToggleShowFps(); });
            Cycle(g2, "Gaya Visual", () => AnimeLook.Bright ? "Cerah ala Genshin" : "Dark Fantasy", () => AnimeLook.ToggleBright(), width: 230f);
            Toggle(g2, "Gaya Anime (outline + efek layar)", () => AnimeLook.On, v => { if (v != AnimeLook.On) AnimeLook.Toggle(); });
            Cycle(g2, "Tingkat Kesulitan", () => Difficulty.Name, () => Difficulty.Cycle(), width: 230f);
            Action_(g2, "Mati Permanen", () => Difficulty.Permadeath ? "AKTIF (terkunci)" : confirmPerma ? "YAKIN? Klik lagi" : "Mati", () => { if (Difficulty.Permadeath) return; if (!confirmPerma) { confirmPerma = true; Say("Mati permanen: pingsan = karakter gugur dan save slot ini DIHAPUS. Tidak bisa dimatikan lagi."); return; } Difficulty.EnablePermadeath(); confirmPerma = false; Say("Mati permanen aktif untuk slot " + SaveSystem.Slot + "."); });
            Toggle(g2, "Tampilan Megah & Mewah", () => RoyalFx.On, v => { if (v != RoyalFx.On) LuxuryLook.Toggle(); });

            // ---------------- Bahasa
            var bh = Section("Bahasa", 66 + RowH + 40, content, true);
            Vlg(bh, 0, 0, 0, 0, 0);
            Cycle(bh, "Bahasa Permainan", () => Loc.Names[Loc.Lang], () => Loc.Set((Loc.Lang + 1) % 3));
            var note = Txt(bh, "Panel ini tetap berbahasa Indonesia; yang berganti adalah teks game (sebagian dialog/lore tidak diterjemahkan).", 22, Dim, TextAnchor.MiddleLeft);
            Le(note.rectTransform, prefH: 40f); note.horizontalOverflow = HorizontalWrapMode.Wrap;

            // ---------------- Akun
            var ak = Section("Simpanan (Offline)", 66 + 4 * RowH + 18, content, true);
            Vlg(ak, 0, 0, 0, 0, 0);
            Info(ak, "Slot save aktif", () => "Slot " + SaveSystem.Slot);
            Action_(ak, "Simpan progres", () => SafeScene() ? "Simpan sekarang" : CanSave() ? "Simpan checkpoint" : "Tidak tersedia", () => { Say(SaveSystem.Save() ? "Progres disimpan." : "Gagal menyimpan."); }, CanSave(), CanSave() ? null : "tidak tersedia di sini");
            Action_(ak, "Folder save (cadangan manual)", () => "Buka folder", () =>
            {
                try { Application.OpenURL("file://" + Application.persistentDataPath); Say("Salin file hunterguild_save*.json untuk cadangan."); } catch { Say("Folder: " + Application.persistentDataPath); }
            });
            Action_(ak, "Keluar dari game", () => confirmQuit ? "YAKIN? Klik lagi" : "Keluar", () => { if (!confirmQuit) { confirmQuit = true; return; } QuitGame(); });
        }

        static bool CanSave() { try { return SaveSystem.CanCheckpoint(SceneManager.GetActiveScene().name); } catch { return false; } }
        static bool SafeScene() { try { return SaveSystem.IsSafeScene(SceneManager.GetActiveScene().name); } catch { return false; } }

        void Finishing() { var b = back; back = null; Finish(false); back = b; }

        static float Vol(int ch) { var a = AudioManager.I; return a == null ? 0f : ch == 0 ? a.musicVol : ch == 1 ? a.sfxVol : a.voiceVol; }

        static void SetVol(int ch, float v)
        {
            var a = AudioManager.I; if (a == null) return;
            float m = a.musicVol, s = a.sfxVol, n = a.voiceVol;
            v = Mathf.Clamp01(Mathf.Round(v * 20f) / 20f);
            if (ch == 0) m = v; else if (ch == 1) s = v; else n = v;
            a.SetVolumes(m, s, n);
        }

        static void QuitGame()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void ResetDefaults()
        {
            try
            {
                SettingsExtra.ResetAll();
                UiSettings.Reset();
                PlayerPrefs.SetFloat("hg_msens", 1f); PlayerPrefs.SetFloat("hg_psens", 1f); PlayerPrefs.SetInt("hg_invy", 0);
                FxQuality.Level = 2;
                var a = AudioManager.I; if (a != null) a.SetVolumes(0.5f, 0.8f, 0.7f);
                PlayerPrefs.Save();
            }
            catch { }
            SettingsExtra.ApplyAa();
            Say("Pengaturan dikembalikan ke bawaan (mode layar, resolusi, dan Kualitas Grafis tidak diubah).");
            Refresh();
        }
    }
}
