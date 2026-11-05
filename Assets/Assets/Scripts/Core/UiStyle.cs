using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HunterGuild
{
    /// <summary>
    /// Tema UI berbingkai ornamen. Setiap JOB punya tema sendiri (warna + motif sudut bingkai + pembatas + lambang),
    /// tanpa job = tema emas klasik. Semua sprite digambar prosedural saat runtime, tanpa aset gambar.
    /// UiStyle.Cur = tema aktif (otomatis mengikuti job pemain), UiStyle.Classic = tema emas klasik (dipakai dialog).
    /// UiStyle.Version naik tiap tema berganti: jendela yang dibangun sekali harus dibangun ulang bila Version berubah.
    /// </summary>
    public static class UiStyle
    {
        public static Sprite OverrideFrame, OverridePlate, OverrideBar;      // opsional: sprite 9-slice buatan sendiri

        static UiTheme classic;
        static readonly Dictionary<string, UiTheme> themes = new Dictionary<string, UiTheme>();
        static UiTheme cur;
        static string curKey = "?";
        static int curSetting = -1, checkedFrame = -1;
        public static int Version { get; private set; }

        public static UiTheme Classic { get { return classic != null ? classic : classic = UiTheme.Make("", UiSettings.Theme); } }

        /// <summary>Kunci tema untuk pemain sekarang ("" = klasik bila belum punya job).</summary>
        public static string KeyFor(GameState gs)
        {
            if (gs == null) return "";
            if (gs.NoJob || (gs.requireJobChoice && !gs.jobChosen) || string.IsNullOrEmpty(gs.job)) return "";
            return gs.job;
        }

        public static UiTheme Cur
        {
            get
            {
                if (cur != null && checkedFrame == Time.frameCount) return cur;     // cukup cek sekali per frame
                checkedFrame = Time.frameCount;
                string k = KeyFor(GameState.I);
                int set = UiSettings.Theme;
                if (cur == null || k != curKey || set != curSetting)
                {
                    string id = k + "#" + set;
                    UiTheme t;
                    if (!themes.TryGetValue(id, out t)) { t = UiTheme.Make(k, set); themes[id] = t; }
                    cur = t; curKey = k; curSetting = set; Version++;
                }
                return cur;
            }
        }

        /// <summary>true bila tema berganti sejak `built` dicatat (lalu `built` diperbarui). Pakai: if (UiStyle.Stale(ref ver) && root != null) { Destroy(root); root = null; }</summary>
        public static bool Stale(ref int built)
        {
            var t = Cur;
            if (built == Version) return false;
            built = Version;
            return true;
        }

        /// <summary>
        /// Ganti sprite & warna tema lama di seluruh hierarki `root` ke tema aktif (untuk UI yang dibangun sebelum job berganti, mis. HUD dan panel menu).
        /// Dialog (anak "DialogueBox") sengaja dilewati: dialog tetap bergaya klasik.
        /// </summary>
        public static void Reskin(Transform root)
        {
            if (root == null) return;
            var to = Cur;
            var olds = new List<UiTheme>();
            foreach (var t in themes.Values) if (t != to) olds.Add(t);
            if (olds.Count == 0) return;
            foreach (var g in root.GetComponentsInChildren<Graphic>(true))
            {
                bool skip = false;
                var p = g.transform;
                for (int i = 0; i < 14 && p != null; i++, p = p.parent) if (p.name == "DialogueBox") { skip = true; break; }
                if (skip) continue;
                var im = g as Image;
                for (int k = 0; k < olds.Count; k++)
                {
                    var o = olds[k];
                    if (im != null && im.sprite != null)
                    {
                        var m = o.MapSprite(im.sprite, to);
                        if (m != null) im.sprite = m;
                    }
                    Color nc;
                    if (o.MapColor(g.color, to, out nc)) { g.color = nc; break; }
                }
            }
        }

        public static Color Gold { get { return Cur.Gold; } }
        public static Color GoldDim { get { return Cur.GoldDim; } }
        public static Color Cream { get { return Cur.Cream; } }
        public static Color Muted { get { return Cur.Muted; } }
        public static Color Ink { get { return Cur.Ink; } }

        public static Sprite FrameSprite { get { return OverrideFrame != null ? OverrideFrame : Cur.FrameSprite; } }
        public static Sprite PlateSprite { get { return OverridePlate != null ? OverridePlate : Cur.PlateSprite; } }
        public static Sprite BarSprite { get { return OverrideBar != null ? OverrideBar : Cur.BarSprite; } }
        public static Sprite SheenSprite { get { return Cur.SheenSprite; } }
        public static Sprite RingSprite { get { return Cur.RingSprite; } }
        public static Sprite DividerSprite { get { return Cur.DividerSprite; } }
        public static Sprite EmblemSprite { get { return Cur.EmblemSprite; } }

        public static Image Frame(string name, Transform parent) { return Cur.Frame(name, parent); }
        public static Image Plate(string name, Transform parent) { return Cur.Plate(name, parent); }
        public static Image Divider(Transform parent, float x, float y, float w) { return Cur.Divider(parent, x, y, w); }
        public static RectTransform Bar(Transform parent, float x, float y, float w, float h, Color fillColor, out Text label, int fontSize) { return Cur.Bar(parent, x, y, w, h, fillColor, out label, fontSize); }
        public static Text Label(Transform parent, string name, int size, TextAnchor anchor, Color color, float x, float y, float w, float h) { return Cur.Label(parent, name, size, anchor, color, x, y, w, h); }
        public static void StyleButton(Image im, bool enabled) { Cur.StyleButton(im, enabled); }
    }

    /// <summary>Satu tema UI: palet + motif + sprite (dibuat malas, di-cache per tema).</summary>
    public sealed class UiTheme
    {
        public string Key, Title, Subtitle;
        public int Motif;                                   // 0 klasik, 1 pedang, 2 daun, 3 api, 4 batu, 5 ombak, 6 angin, 7 suci
        public Color Gold, GoldDim, Cream, Muted, Ink;
        public Color PanelLo, PanelHi;                      // gradasi isi panel (bawah -> atas)
        public Color Slot, SlotSel, SlotEmpty, Row, Desc, TabOff, TabOn, Chip;

        Sprite frame, plate, bar, sheen, ring, divider, emblem;

        public Sprite FrameSprite { get { return frame != null ? frame : frame = MakeBox(96, 26, Motif == 1 ? 15 : 12, true); } }
        public Sprite PlateSprite { get { return plate != null ? plate : plate = MakeBox(48, 14, 6, false); } }
        public Sprite BarSprite { get { return bar != null ? bar : bar = MakeBar(); } }
        public Sprite SheenSprite { get { return sheen != null ? sheen : sheen = MakeSheen(); } }
        public Sprite RingSprite { get { return ring != null ? ring : ring = MakeRing(); } }
        public Sprite DividerSprite { get { return divider != null ? divider : divider = MakeDivider(); } }
        public Sprite EmblemSprite { get { return emblem != null ? emblem : emblem = MakeEmblem(); } }

        public Sprite MapSprite(Sprite s, UiTheme to)
        {
            if (s == null) return null;
            if (s == frame) return to.FrameSprite;
            if (s == plate) return to.PlateSprite;
            if (s == bar) return to.BarSprite;
            if (s == ring) return to.RingSprite;
            if (s == divider) return to.DividerSprite;
            if (s == emblem) return to.EmblemSprite;
            return null;
        }

        static bool Near(Color a, Color b) { return Mathf.Abs(a.r - b.r) < 0.004f && Mathf.Abs(a.g - b.g) < 0.004f && Mathf.Abs(a.b - b.b) < 0.004f; }

        /// <summary>Bila warna c sama dengan salah satu warna palet tema ini, kembalikan padanannya di tema `to` (alpha dipertahankan).</summary>
        public bool MapColor(Color c, UiTheme to, out Color r)
        {
            r = c;
            if (Near(c, Gold)) { r = to.Gold; }
            else if (Near(c, GoldDim)) { r = to.GoldDim; }
            else if (Near(c, Cream)) { r = to.Cream; }
            else if (Near(c, Muted)) { r = to.Muted; }
            else return false;
            r.a = c.a;
            return true;
        }

        static Color Rgb(float r, float g, float b, float a) { return new Color(r, g, b, a); }

        // ================= Definisi tema per job =================
        public static UiTheme Make(string job, int setting)
        {
            var t = new UiTheme();
            t.Key = job;
            switch (job)
            {
                case "Swordsman":
                    t.Motif = 1; t.Title = "GUDANG PERANG"; t.Subtitle = "Bilah, zirah, dan bekal ksatria";
                    t.Gold = Rgb(0.92f, 0.38f, 0.34f, 1f); t.GoldDim = Rgb(0.46f, 0.14f, 0.16f, 1f);
                    t.Cream = Rgb(0.97f, 0.91f, 0.89f, 1f); t.Muted = Rgb(0.78f, 0.68f, 0.68f, 1f);
                    t.PanelLo = Rgb(0.03f, 0.02f, 0.03f, 1f); t.PanelHi = Rgb(0.12f, 0.05f, 0.06f, 1f);
                    break;
                case "Archer":
                    t.Motif = 2; t.Title = "KANTONG PEMBURU"; t.Subtitle = "Anak panah, jebakan, dan hasil buruan";
                    t.Gold = Rgb(0.58f, 0.84f, 0.42f, 1f); t.GoldDim = Rgb(0.22f, 0.40f, 0.17f, 1f);
                    t.Cream = Rgb(0.93f, 0.97f, 0.85f, 1f); t.Muted = Rgb(0.70f, 0.78f, 0.62f, 1f);
                    t.PanelLo = Rgb(0.02f, 0.04f, 0.025f, 1f); t.PanelHi = Rgb(0.06f, 0.12f, 0.06f, 1f);
                    break;
                case "FireMage":
                    t.Motif = 3; t.Title = "GRIMOIRE BARA"; t.Subtitle = "Ramuan, bahan, dan tongkat berapi";
                    t.Gold = Rgb(1.00f, 0.58f, 0.20f, 1f); t.GoldDim = Rgb(0.55f, 0.22f, 0.07f, 1f);
                    t.Cream = Rgb(1.00f, 0.93f, 0.80f, 1f); t.Muted = Rgb(0.82f, 0.68f, 0.55f, 1f);
                    t.PanelLo = Rgb(0.04f, 0.015f, 0.01f, 1f); t.PanelHi = Rgb(0.15f, 0.05f, 0.02f, 1f);
                    break;
                case "EarthMage":
                    t.Motif = 4; t.Title = "PETI BATU"; t.Subtitle = "Mineral, akar, dan perlengkapan bumi";
                    t.Gold = Rgb(0.82f, 0.64f, 0.34f, 1f); t.GoldDim = Rgb(0.41f, 0.30f, 0.16f, 1f);
                    t.Cream = Rgb(0.96f, 0.90f, 0.78f, 1f); t.Muted = Rgb(0.76f, 0.68f, 0.54f, 1f);
                    t.PanelLo = Rgb(0.035f, 0.028f, 0.02f, 1f); t.PanelHi = Rgb(0.11f, 0.085f, 0.055f, 1f);
                    break;
                case "WaterMage":
                    t.Motif = 5; t.Title = "KANTONG SAMUDRA"; t.Subtitle = "Embun, kristal es, dan tongkat arus";
                    t.Gold = Rgb(0.40f, 0.78f, 0.98f, 1f); t.GoldDim = Rgb(0.13f, 0.33f, 0.52f, 1f);
                    t.Cream = Rgb(0.88f, 0.96f, 1.00f, 1f); t.Muted = Rgb(0.62f, 0.75f, 0.85f, 1f);
                    t.PanelLo = Rgb(0.01f, 0.025f, 0.05f, 1f); t.PanelHi = Rgb(0.04f, 0.10f, 0.17f, 1f);
                    break;
                case "WindMage":
                    t.Motif = 6; t.Title = "TAS ANGIN"; t.Subtitle = "Ringan, lincah, dan penuh bulu";
                    t.Gold = Rgb(0.56f, 0.93f, 0.80f, 1f); t.GoldDim = Rgb(0.20f, 0.46f, 0.40f, 1f);
                    t.Cream = Rgb(0.92f, 1.00f, 0.97f, 1f); t.Muted = Rgb(0.65f, 0.80f, 0.76f, 1f);
                    t.PanelLo = Rgb(0.015f, 0.04f, 0.04f, 1f); t.PanelHi = Rgb(0.06f, 0.14f, 0.13f, 1f);
                    break;
                case "Priest":
                    t.Motif = 7; t.Title = "PERBENDAHARAAN SUCI"; t.Subtitle = "Berkat, ramuan, dan perlengkapan suci";
                    t.Gold = Rgb(1.00f, 0.92f, 0.64f, 1f); t.GoldDim = Rgb(0.62f, 0.55f, 0.34f, 1f);
                    t.Cream = Rgb(1.00f, 0.99f, 0.94f, 1f); t.Muted = Rgb(0.82f, 0.79f, 0.70f, 1f);
                    t.PanelLo = Rgb(0.05f, 0.045f, 0.07f, 1f); t.PanelHi = Rgb(0.16f, 0.14f, 0.21f, 1f);
                    break;
                default:
                    t.Motif = 0; t.Title = "TAS"; t.Subtitle = "Perbekalan seorang pemburu";
                    t.Gold = Rgb(0.90f, 0.73f, 0.35f, 1f); t.GoldDim = Rgb(0.55f, 0.42f, 0.18f, 1f);
                    t.Cream = Rgb(0.97f, 0.91f, 0.76f, 1f); t.Muted = Rgb(0.75f, 0.70f, 0.60f, 1f);
                    t.PanelLo = Rgb(0.02f, 0.02f, 0.04f, 1f); t.PanelHi = Rgb(0.07f, 0.07f, 0.12f, 1f);
                    break;
            }
            float alpha = 0.92f;
            // pengaturan tema warna: 1 Hangat Lembut (lebih redup), 2 Kontras Tinggi
            if (setting == 1)
            {
                Color warm = Rgb(0.5f, 0.45f, 0.38f, 1f);
                t.Gold = Color.Lerp(t.Gold, warm, 0.16f) * 0.93f; t.GoldDim = Color.Lerp(t.GoldDim, warm, 0.1f) * 0.9f;
                t.Cream = Color.Lerp(t.Cream, warm, 0.1f); t.Muted = Color.Lerp(t.Muted, warm, 0.1f);
                alpha = 0.90f;
            }
            else if (setting == 2)
            {
                t.Gold = Color.Lerp(t.Gold, Color.white, 0.12f); t.Cream = Color.Lerp(t.Cream, Color.white, 0.5f);
                t.Muted = Color.Lerp(t.Muted, Color.white, 0.3f); t.GoldDim = Color.Lerp(t.GoldDim, t.Gold, 0.25f);
                t.PanelLo *= 0.6f; t.PanelHi *= 0.7f; alpha = 0.97f;
            }
            t.Gold.a = 1f; t.GoldDim.a = 1f; t.Cream.a = 1f; t.Muted.a = 1f;
            t.PanelLo.a = 1f; t.PanelHi.a = 1f;
            t.Ink = new Color(t.PanelLo.r, t.PanelLo.g, t.PanelLo.b, alpha);
            // warna turunan untuk isi jendela
            t.Slot = Color.Lerp(t.PanelHi, t.GoldDim, 0.38f);
            t.SlotSel = Color.Lerp(t.Gold, Color.white, 0.12f);
            t.SlotEmpty = Color.Lerp(t.PanelLo, t.PanelHi, 0.55f);
            t.Row = Color.Lerp(t.PanelHi, t.GoldDim, 0.22f);
            t.Desc = Color.Lerp(t.PanelLo, t.PanelHi, 0.7f);
            t.TabOff = Color.Lerp(t.PanelHi, t.GoldDim, 0.45f);
            t.TabOn = Color.Lerp(t.Gold, Color.white, 0.1f);
            t.Chip = Color.Lerp(t.PanelHi, t.GoldDim, 0.6f);
            return t;
        }

        // ================= Pembuat elemen =================
        public Image Frame(string name, Transform parent) { return Sliced(name, parent, UiStyle.OverrideFrame != null ? UiStyle.OverrideFrame : FrameSprite, Color.white); }
        public Image Plate(string name, Transform parent) { return Sliced(name, parent, UiStyle.OverridePlate != null ? UiStyle.OverridePlate : PlateSprite, Color.white); }

        static Image Sliced(string name, Transform parent, Sprite s, Color c)
        {
            var im = UIKit.Img(name, parent, c);
            im.sprite = s;
            im.type = Image.Type.Sliced;
            im.raycastTarget = false;
            return im;
        }

        public Image Divider(Transform parent, float x, float y, float w)
        {
            var im = UIKit.Img("Divider", parent, Color.white);
            im.sprite = DividerSprite;
            im.raycastTarget = false;
            UIKit.At(im.rectTransform, x, y, w, 12f);
            return im;
        }

        /// <summary>Bar berbingkai. Isi bar diatur lewat fill.anchorMax = (0..1, 1).</summary>
        public RectTransform Bar(Transform parent, float x, float y, float w, float h, Color fillColor, out Text label, int fontSize)
        {
            var bg = Sliced("Bar", parent, UiStyle.OverrideBar != null ? UiStyle.OverrideBar : BarSprite, Color.white);
            UIKit.At(bg.rectTransform, x, y, w, h);
            var inner = UIKit.Rect("Inner", bg.transform);
            UIKit.Place(inner, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            inner.offsetMin = new Vector2(3, 3); inner.offsetMax = new Vector2(-3, -3);
            var fill = UIKit.Img("Fill", inner, fillColor);
            fill.sprite = SheenSprite; fill.raycastTarget = false;
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = Vector2.zero; fill.rectTransform.offsetMax = Vector2.zero;
            label = UIKit.Txt("Label", bg.transform, fontSize, TextAnchor.MiddleCenter, Cream);
            UIKit.Place(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var sh = label.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.9f); sh.effectDistance = new Vector2(1, -1);
            return fill.rectTransform;
        }

        public Text Label(Transform parent, string name, int size, TextAnchor anchor, Color color, float x, float y, float w, float h)
        {
            var t = UIKit.Txt(name, parent, size, anchor, color);
            UIKit.At(t.rectTransform, x, y, w, h);
            var sh = t.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.85f); sh.effectDistance = new Vector2(1.2f, -1.2f);
            return t;
        }

        public void StyleButton(Image im, bool enabled)
        {
            im.sprite = UiStyle.OverridePlate != null ? UiStyle.OverridePlate : PlateSprite; im.type = Image.Type.Sliced;
            var b = im.GetComponent<Button>();
            if (b == null) return;
            var cb = b.colors;
            cb.normalColor = enabled ? new Color(0.88f, 0.86f, 0.84f) : new Color(0.38f, 0.38f, 0.38f);
            cb.highlightedColor = Color.Lerp(Gold, Color.white, 0.55f);
            cb.pressedColor = Color.Lerp(Gold, GoldDim, 0.4f);
            cb.disabledColor = new Color(0.38f, 0.38f, 0.38f);
            b.colors = cb;
        }

        // ================= Gambar sprite =================
        static Color32 C(Color c, float a) { return new Color32((byte)(Mathf.Clamp01(c.r) * 255), (byte)(Mathf.Clamp01(c.g) * 255), (byte)(Mathf.Clamp01(c.b) * 255), (byte)(Mathf.Clamp01(a) * 255)); }

        static Sprite ToSprite(Texture2D t, int border)
        {
            t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear; t.hideFlags = HideFlags.HideAndDontSave;
            t.Apply(false, false);
            return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        // Ornamen pojok per motif. u,v = jarak ke dua tepi terdekat (pojok = 0,0); d = jarak ke tepi.
        void Corner(float u, float v, float d, ref Color col, ref float a)
        {
            float s = (u + v) * 0.5f, df = Mathf.Abs(u - v);
            float p = (u + v) * 0.7071f, q = (u - v) * 0.7071f;
            switch (Motif)
            {
                case 0:
                    if (Mathf.Abs(u - 12f) + Mathf.Abs(v - 12f) < 4.5f) { col = Gold; a = 1f; }
                    float rr0 = Mathf.Sqrt((u - 26f) * (u - 26f) + (v - 26f) * (v - 26f));
                    if (u < 26f && v < 26f && Mathf.Abs(rr0 - 15f) < 0.9f && d >= 5f) { col = Color.Lerp(GoldDim, Gold, 0.5f); a = 0.95f; }
                    break;
                case 1:   // bilah menyilang di sudut + hulu
                    if (df < 1.4f && s > 7f && s < 25f && d >= 4f) { col = Color.Lerp(GoldDim, Gold, s / 25f); a = 1f; }
                    if (Mathf.Abs((u + v) - 24f) < 1.2f && df < 7f && d >= 4f) { col = Gold; a = 1f; }
                    if (Mathf.Abs(u - 10f) + Mathf.Abs(v - 10f) < 3.4f) { col = Gold; a = 1f; }
                    break;
                case 2:   // daun
                    if ((p - 19f) * (p - 19f) / 81f + q * q / 11f < 1f && d >= 4f) { col = Color.Lerp(GoldDim, Gold, 0.4f + (p - 10f) / 20f); a = 0.97f; }
                    if (Mathf.Abs(q) < 0.55f && Mathf.Abs(p - 19f) < 9f && d >= 4f) { col = PanelLo; a = 1f; }
                    if (Mathf.Abs(u - 9f) + Mathf.Abs(v - 9f) < 2.6f) { col = Gold; a = 1f; }
                    break;
                case 3:   // lidah api
                    {
                        float wig = Mathf.Sin(p * 0.55f) * 1.6f, w = 5.2f * (1f - (p - 8f) / 22f);
                        if (p > 8f && p < 30f && Mathf.Abs(q - wig) < w && d >= 4f)
                        {
                            col = Mathf.Abs(q - wig) < w * 0.45f ? Color.Lerp(Gold, Cream, 0.55f) : Color.Lerp(GoldDim, Gold, 0.6f); a = 1f;
                        }
                        if (Mathf.Abs(u - 8f) + Mathf.Abs(v - 8f) < 3f) { col = Gold; a = 1f; }
                    }
                    break;
                case 4:   // balok batu bertingkat
                    if (u >= 6f && u < 13f && v >= 6f && v < 13f) { col = Gold; a = 1f; }
                    if (u >= 15f && u < 20f && v >= 6f && v < 10f) { col = GoldDim; a = 1f; }
                    if (v >= 15f && v < 20f && u >= 6f && u < 10f) { col = GoldDim; a = 1f; }
                    if (u >= 8f && u < 11f && v >= 8f && v < 11f) { col = PanelLo; a = 1f; }
                    break;
                case 5:   // riak air + mutiara
                    {
                        float rc = Mathf.Sqrt((u - 14f) * (u - 14f) + (v - 14f) * (v - 14f));
                        if (rc < 3.4f) { col = Color.Lerp(Gold, Cream, 0.5f); a = 1f; }
                        if (Mathf.Abs(rc - 7.5f) < 0.8f && d >= 4f) { col = Gold; a = 0.95f; }
                        if (Mathf.Abs(rc - 12f) < 0.8f && d >= 4f && (u > 12f || v > 12f)) { col = GoldDim; a = 0.95f; }
                    }
                    break;
                case 6:   // pusaran angin
                    {
                        float dx = u - 14f, dy = v - 14f, rw = Mathf.Sqrt(dx * dx + dy * dy);
                        float th = (Mathf.Atan2(dy, dx) + Mathf.PI) / (2f * Mathf.PI);
                        for (int k = 0; k < 2; k++)
                        {
                            float tr = 2.5f + th * 5f + k * 5f;
                            if (rw < 13.5f && Mathf.Abs(rw - tr) < 0.95f && d >= 3.5f) { col = Color.Lerp(GoldDim, Gold, 0.5f + 0.5f * th); a = 1f; }
                        }
                    }
                    break;
                case 7:   // bintang suci + lingkaran halo
                    {
                        float dx = u - 13f, dy = v - 13f;
                        if (Mathf.Sqrt(Mathf.Abs(dx)) + Mathf.Sqrt(Mathf.Abs(dy)) < 3.3f) { col = Gold; a = 1f; }
                        if (Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - 10.5f) < 0.75f && d >= 4f) { col = GoldDim; a = 0.9f; }
                    }
                    break;
            }
        }

        // Kotak 9-slice dengan sudut terpotong, garis ganda, dan ornamen sudut sesuai job.
        Sprite MakeBox(int size, int border, int cut, bool ornate)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = Mathf.Min(x, size - 1 - x), v = Mathf.Min(y, size - 1 - y);
                    float d = Mathf.Min(u, v);
                    if (u + v < cut * 2f) d = Mathf.Min(d, (u + v - cut) / 1.4142f);   // sudut terpotong
                    float t = (float)y / size;                                          // 0 bawah, 1 atas
                    Color col = Color.clear; float a = 0f;
                    float outer = ornate ? 3f : 1.5f, outerW = ornate ? 2.2f : 1.6f;
                    if (d < 0f) { a = 0f; }
                    else if (d >= outer && d < outer + outerW) { col = Color.Lerp(GoldDim, Gold, 0.35f + 0.65f * t); a = 1f; }
                    else if (ornate && d >= 8f && d < 9f) { col = GoldDim; a = 0.85f; }
                    else if (d >= outer + outerW)
                    {
                        col = Color.Lerp(PanelLo, PanelHi, t); a = Ink.a;
                        float gl = Mathf.Clamp01(1f - (d - outer - outerW) / 7f);               // cahaya aksen tipis di sisi dalam bingkai
                        col = Color.Lerp(col, Gold, gl * 0.16f);
                    }
                    else if (d >= 0f) { col = Gold; a = 0.15f; }

                    if (ornate) Corner(u, v, d, ref col, ref a);
                    else if (Mathf.Abs(u - cut * 0.55f) + Mathf.Abs(v - cut * 0.55f) < 2.6f) { col = Gold; a = 1f; }
                    px[y * size + x] = C(col, a);
                }
            tex.SetPixels32(px);
            return ToSprite(tex, border);
        }

        Sprite MakeBar()
        {
            const int W = 48, H = 20;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = Mathf.Min(x, W - 1 - x), v = Mathf.Min(y, H - 1 - y);
                    float d = Mathf.Min(u, v);
                    Color col; float a;
                    if (d < 1.2f) { col = Color.Lerp(GoldDim, Gold, (float)y / H); a = 1f; }
                    else if (d < 2.2f) { col = Color.Lerp(PanelLo, GoldDim, 0.2f); a = 1f; }
                    else { col = PanelLo * 0.8f; a = 0.92f; }
                    if (u + v < 3f) a = 0f;
                    px[y * W + x] = C(col, a);
                }
            tex.SetPixels32(px);
            tex.wrapMode = TextureWrapMode.Clamp; tex.filterMode = FilterMode.Bilinear; tex.hideFlags = HideFlags.HideAndDontSave; tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(8, 6, 8, 6));
        }

        static Sprite MakeSheen()
        {
            var tex = new Texture2D(4, 16, TextureFormat.RGBA32, false);
            for (int y = 0; y < 16; y++)
            {
                float t = y / 15f;
                float k = t > 0.5f ? Mathf.Lerp(0.85f, 1.25f, (t - 0.5f) * 2f) : Mathf.Lerp(0.55f, 0.85f, t * 2f);
                for (int x = 0; x < 4; x++) tex.SetPixel(x, y, new Color(Mathf.Min(1f, k), Mathf.Min(1f, k), Mathf.Min(1f, k), 1f));
            }
            tex.wrapMode = TextureWrapMode.Clamp; tex.filterMode = FilterMode.Bilinear; tex.hideFlags = HideFlags.HideAndDontSave; tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 16), new Vector2(0.5f, 0.5f), 100f);
        }

        Sprite MakeRing()
        {
            const int S = 96;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            var px = new Color32[S * S];
            float c = (S - 1) * 0.5f;
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float r = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    Color col = Color.clear; float a = 0f;
                    if (r < 45f) { col = Color.Lerp(PanelLo, PanelHi, 0.4f); a = 0.95f; }
                    if (r >= 40f && r < 41.5f) { col = GoldDim; a = 1f; }
                    if (r >= 43f && r < 46f) { col = Color.Lerp(GoldDim, Gold, (float)y / S); a = 1f; }
                    if (r >= 46f && r < 47.5f) a = Mathf.Max(a, (47.5f - r) / 1.5f);
                    float ax = Mathf.Abs(x - c), ay = Mathf.Abs(y - c);
                    if ((ax < 2.5f && Mathf.Abs(ay - 47f) < 2.5f) || (ay < 2.5f && Mathf.Abs(ax - 47f) < 2.5f)) { col = Gold; a = 1f; }
                    px[y * S + x] = C(col, a);
                }
            tex.SetPixels32(px);
            tex.wrapMode = TextureWrapMode.Clamp; tex.filterMode = FilterMode.Bilinear; tex.hideFlags = HideFlags.HideAndDontSave; tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
        }

        // bentuk hiasan tengah pembatas (dx dari tengah, dy dari garis)
        bool CenterMark(float dx, float dy)
        {
            switch (Motif)
            {
                case 1: return Mathf.Abs(dx) * 0.45f + Mathf.Abs(dy) < 5f;                    // bilah pipih
                case 2: return dx * dx / 40f + dy * dy / 14f < 1f;                              // daun
                case 3: return dy < 5f && Mathf.Abs(dx) < Mathf.Max(0f, (5f - dy) * 0.75f);    // api
                case 4: return Mathf.Abs(dx) < 4f && Mathf.Abs(dy) < 4f;                        // batu
                case 5: return dx * dx + dy * dy < 22f;                                         // mutiara
                case 6: return (dx - 3f) * (dx - 3f) + dy * dy < 7f || (dx + 3f) * (dx + 3f) + dy * dy < 7f;
                case 7: return Mathf.Sqrt(Mathf.Abs(dx) * 0.8f) + Mathf.Sqrt(Mathf.Abs(dy)) < 2.9f;
                default: return Mathf.Abs(dx) + Mathf.Abs(dy) < 5f;
            }
        }

        Sprite MakeDivider()
        {
            const int W = 128, H = 12;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float cx = (W - 1) * 0.5f;
                    float fx = Mathf.Abs(x - cx) / cx;      // 0 tengah, 1 ujung
                    Color col = Color.clear; float a = 0f;
                    if (Mathf.Abs(y - 5.5f) < 0.9f) { col = Gold; a = Mathf.Clamp01(1.25f - fx * 1.25f); }
                    if (Mathf.Abs(y - 5.5f) < 0.5f && fx > 0.12f) a *= 0.9f;
                    if (CenterMark(x - cx, y - 5.5f)) { col = Gold; a = 1f; }
                    if (Mathf.Abs(Mathf.Abs(x - cx) - 14f) + Mathf.Abs(y - 5.5f) < 2.6f) { col = GoldDim; a = 1f; }   // berlian kecil kiri-kanan
                    px[y * W + x] = C(col, a);
                }
            tex.SetPixels32(px);
            tex.wrapMode = TextureWrapMode.Clamp; tex.filterMode = FilterMode.Bilinear; tex.hideFlags = HideFlags.HideAndDontSave; tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
        }

        // ================= Lambang job =================
        // keluaran: 0 di luar, 1 warna utama, 2 warna sorot. x,y dalam [-1,1], y ke atas.
        int Shape(float x, float y)
        {
            switch (Motif)
            {
                case 1:   // pedang diagonal
                    {
                        float c = 0.8192f, s = 0.5736f;            // putar 35 derajat
                        float rx = x * c - y * s, ry = x * s + y * c;
                        if (ry > -0.2f && ry < 0.95f && Mathf.Abs(rx) < 0.12f * Mathf.Min(1f, (0.95f - ry) / 0.3f)) return Mathf.Abs(rx) < 0.025f ? 2 : 1;
                        if (Mathf.Abs(ry + 0.25f) < 0.07f && Mathf.Abs(rx) < 0.44f) return 1;
                        if (Mathf.Abs(rx) < 0.07f && ry > -0.72f && ry <= -0.3f) return 1;
                        if (rx * rx + (ry + 0.8f) * (ry + 0.8f) < 0.015f) return 1;
                        return 0;
                    }
                case 2:   // busur + anak panah
                    {
                        float bx = x + 0.45f, rr = Mathf.Sqrt(bx * bx + y * y);
                        if (Mathf.Abs(rr - 0.92f) < 0.065f && bx > 0.3f) return 1;
                        if (Mathf.Abs(x - 0.02f) < 0.02f && Mathf.Abs(y) < 0.79f) return 2;
                        if (Mathf.Abs(y) < 0.035f && x > -0.62f && x < 0.8f) return 1;
                        if (x >= 0.62f && x < 0.95f && Mathf.Abs(y) < (0.95f - x) * 0.7f) return 1;
                        if (x < -0.45f && x > -0.68f && Mathf.Abs(y) < (x + 0.68f) * 0.7f + 0.04f && Mathf.Abs(y) > 0.04f) return 2;
                        return 0;
                    }
                case 3:   // api
                    {
                        float t = (y + 0.88f) / 1.78f;
                        if (t < 0f || t > 1f) return 0;
                        float sway = 0.14f * Mathf.Sin(t * 5.2f) * t;
                        float w = 0.62f * Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.55f));
                        float dxs = Mathf.Abs(x - sway);
                        if (dxs < w)
                        {
                            float t2 = (y + 0.7f) / 1.0f, w2 = 0.3f * Mathf.Sin(Mathf.PI * Mathf.Pow(Mathf.Clamp01(t2), 0.6f));
                            if (t2 > 0f && t2 < 1f && dxs < w2) return 2;
                            return 1;
                        }
                        return 0;
                    }
                case 4:   // kristal batu
                    {
                        float ax = Mathf.Abs(x + 0.02f);
                        if (ax / 0.46f + Mathf.Abs(y - 0.05f) / 0.92f < 1f) return (x > 0.02f && y > -0.45f) ? 2 : 1;
                        float sx = Mathf.Abs(x + 0.62f);
                        if (sx / 0.2f + Mathf.Abs(y + 0.45f) / 0.38f < 1f) return 1;
                        float tx = Mathf.Abs(x - 0.64f);
                        if (tx / 0.18f + Mathf.Abs(y + 0.5f) / 0.33f < 1f) return 1;
                        return 0;
                    }
                case 5:   // tetes air
                    {
                        float cy = y + 0.25f;
                        if (x * x + cy * cy < 0.3f) return (x < -0.1f && x > -0.32f && cy > -0.1f && cy < 0.3f && (x + 0.2f) * (x + 0.2f) + (cy - 0.1f) * (cy - 0.1f) < 0.02f) ? 2 : 1;
                        if (y > -0.25f && y < 0.92f && Mathf.Abs(x) < 0.55f * (0.92f - y) / 1.17f) return 1;
                        return 0;
                    }
                case 6:   // pusaran
                    {
                        float rr = Mathf.Sqrt(x * x + y * y), ang = Mathf.Atan2(y, x);
                        for (int k = 0; k < 3; k++)
                        {
                            float th = ang + Mathf.PI * 2f * k + Mathf.PI;
                            float tr = 0.1f + 0.075f * th;
                            if (tr > 0.05f && tr < 0.92f && Mathf.Abs(rr - tr) < 0.06f) return k == 0 ? 2 : 1;
                        }
                        return 0;
                    }
                case 7:   // salib + halo
                    {
                        float rr = Mathf.Sqrt(x * x + y * y);
                        if (Mathf.Abs(rr - 0.88f) < 0.06f) return 1;
                        if (Mathf.Abs(x) < 0.1f && Mathf.Abs(y) < 0.62f) return 2;
                        if (Mathf.Abs(y - 0.14f) < 0.1f && Mathf.Abs(x) < 0.4f) return 2;
                        return 0;
                    }
                default:  // kantong
                    {
                        float cy = y + 0.2f;
                        if (x * x / 0.64f + cy * cy / 0.5f < 1f) return (x < -0.3f && cy > 0.05f && cy < 0.45f) ? 2 : 1;
                        if (Mathf.Abs(x) < 0.3f && y > 0.4f && y < 0.66f) return 1;
                        if (Mathf.Abs(x) < 0.46f && Mathf.Abs(y - 0.68f) < 0.08f) return 2;
                        return 0;
                    }
            }
        }

        Sprite MakeEmblem()
        {
            const int S = 96;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float n1 = 0f, n2 = 0f;
                    for (int sy = 0; sy < 3; sy++)
                        for (int sx = 0; sx < 3; sx++)
                        {
                            float fx = ((x + (sx + 0.5f) / 3f) / S) * 2f - 1f, fy = ((y + (sy + 0.5f) / 3f) / S) * 2f - 1f;
                            int s = Shape(fx, fy);
                            if (s == 1) n1 += 1f; else if (s == 2) n2 += 1f;
                        }
                    float cov = (n1 + n2) / 9f;
                    if (cov <= 0f) { px[y * S + x] = new Color32(0, 0, 0, 0); continue; }
                    float tt = (float)y / S;
                    Color main = Color.Lerp(GoldDim, Gold, 0.35f + 0.65f * tt);
                    Color hi = Color.Lerp(Gold, Color.white, 0.75f);
                    Color col = Color.Lerp(main, hi, n2 / Mathf.Max(0.0001f, n1 + n2));
                    px[y * S + x] = C(col, cov);
                }
            tex.SetPixels32(px);
            tex.wrapMode = TextureWrapMode.Clamp; tex.filterMode = FilterMode.Bilinear; tex.hideFlags = HideFlags.HideAndDontSave; tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
