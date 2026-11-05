using System;
using UnityEngine;
using UnityEngine.UI;

namespace HunterGuild
{
    /// <summary>Pembantu UI bergaya emas-gelap (dipakai Leaderboard dan Quest). Koordinat dari pojok kiri-atas induk.</summary>
    public static class GoldUI
    {
        // warna isi mengikuti tema job (UiStyle.Cur)
        public static Color Dark { get { var t = UiStyle.Cur; return new Color(t.PanelLo.r * 1.1f, t.PanelLo.g * 1.1f, t.PanelLo.b * 1.1f, 0.96f); } }
        public static Color Row { get { var t = UiStyle.Cur; return new Color(t.Row.r * 0.55f, t.Row.g * 0.55f, t.Row.b * 0.55f, 0.95f); } }
        public static Color RowSel { get { var t = UiStyle.Cur; return new Color(t.GoldDim.r * 1.15f, t.GoldDim.g * 1.15f, t.GoldDim.b * 1.15f, 0.95f); } }

        /// <summary>Jendela berbingkai 1536x1024 (skala 0,98 agar muat di 1080p) lengkap dengan judul + tombol X. Mengembalikan root konten.</summary>
        public static RectTransform Window(Transform parent, string name, string title, string glyph, Action onClose)
        {
            var bg = UiStyle.Frame(name, parent); bg.raycastTarget = true;
            var c = new Vector2(0.5f, 0.5f);
            UIKit.Place(bg.rectTransform, c, c, c, Vector2.zero, new Vector2(1536f, 1024f));
            bg.rectTransform.localScale = Vector3.one * 0.98f;
            var back = UIKit.Img("Backing", bg.transform, Dark); back.raycastTarget = false;
            Stretch(back.rectTransform, 8f);
            back.transform.SetAsFirstSibling();
            Ring(bg.transform, glyph, 30f, 10f, 74f, UiStyle.Gold);
            Label(bg.transform, title, 42, UiStyle.Gold, 130f, 18f, 900f, 56f, TextAnchor.MiddleLeft, true);
            var x = Panel(bg.transform, 1456f, 12f, 66f, 62f, UiStyle.Cur.Row * 0.6f);
            var xb = x.gameObject.AddComponent<Button>(); xb.targetGraphic = x;
            x.raycastTarget = true;
            Label(x.transform, "X", 40, UiStyle.Gold, 0f, 0f, 66f, 62f, TextAnchor.MiddleCenter, true);
            xb.onClick.AddListener(() => { Audio.Sfx("click"); if (onClose != null) onClose(); });
            return bg.rectTransform;
        }

        public static void Stretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
        }

        public static Image Panel(Transform parent, float x, float y, float w, float h, Color col)
        {
            var im = UiStyle.Plate("Panel", parent); im.color = col;
            UIKit.At(im.rectTransform, x, y, w, h);
            return im;
        }

        /// <summary>Panel berbingkai emas ornamen dengan latar gelap.</summary>
        public static RectTransform Box(Transform parent, float x, float y, float w, float h)
        {
            var f = UiStyle.Frame("Box", parent);
            UIKit.At(f.rectTransform, x, y, w, h);
            var b = UIKit.Img("Backing", f.transform, Dark); b.raycastTarget = false;
            Stretch(b.rectTransform, 6f); b.transform.SetAsFirstSibling();
            return f.rectTransform;
        }

        public static Text Label(Transform parent, string text, int size, Color col, float x, float y, float w, float h, TextAnchor a, bool bold = false)
        {
            var t = UIKit.Txt("T", parent, size, a, col);
            t.text = text; t.supportRichText = true;
            if (bold) t.fontStyle = FontStyle.Bold;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            UIKit.At(t.rectTransform, x, y, w, h);
            return t;
        }

        public static Image Ring(Transform parent, string glyph, float x, float y, float size, Color col)
        {
            var r = UIKit.Img("Ring", parent, col); r.sprite = UiStyle.RingSprite; r.raycastTarget = false;
            UIKit.At(r.rectTransform, x, y, size, size);
            var g = UIKit.Txt("G", r.transform, Mathf.RoundToInt(size * 0.42f), TextAnchor.MiddleCenter, col);
            g.text = glyph; g.fontStyle = FontStyle.Bold;
            UIKit.Place(g.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            return r;
        }

        public static Image Disc(Transform parent, Color col, float x, float y, float size)
        {
            var im = UIKit.Img("Disc", parent, col); im.raycastTarget = false;
            UIKit.At(im.rectTransform, x, y, size, size);
            return im;
        }

        public static Button Tab(Transform parent, string label, string glyph, float x, float y, float w, float h, bool sel, Action onClick)
        {
            var im = UiStyle.Plate("Tab", parent); im.raycastTarget = true;
            im.color = sel ? UiStyle.Cur.TabOn : Color.Lerp(Color.white, UiStyle.Cur.TabOff, 0.55f);
            UIKit.At(im.rectTransform, x, y, w, h);
            var b = im.gameObject.AddComponent<Button>(); b.targetGraphic = im;
            var cb = b.colors; cb.highlightedColor = Color.Lerp(UiStyle.Cur.Gold, Color.white, 0.5f); cb.pressedColor = UiStyle.Cur.GoldDim * 1.3f; b.colors = cb;
            Ring(im.transform, glyph, 16f, (h - 36f) * 0.5f, 36f, sel ? UiStyle.Cur.PanelLo : UiStyle.Gold);
            Label(im.transform, label, 26, sel ? new Color(1f, 0.97f, 0.88f) : UiStyle.Cream, 62f, 0f, w - 70f, h, TextAnchor.MiddleLeft);
            b.onClick.AddListener(() => { Audio.Sfx("click"); if (onClick != null) onClick(); });
            return b;
        }

        public static Button Gold(Transform parent, string label, float x, float y, float w, float h, bool enabled, Action onClick, bool primary = true)
        {
            var im = UiStyle.Plate("Btn", parent); im.raycastTarget = true;
            im.color = !enabled ? new Color(0.3f, 0.3f, 0.32f) : primary ? UiStyle.Cur.TabOn : Color.Lerp(Color.white, UiStyle.Cur.TabOff, 0.5f);
            UIKit.At(im.rectTransform, x, y, w, h);
            var b = im.gameObject.AddComponent<Button>(); b.targetGraphic = im; b.interactable = enabled;
            Label(im.transform, label, 28, primary && enabled ? UiStyle.Cur.PanelLo : UiStyle.Cream, 0f, 0f, w, h, TextAnchor.MiddleCenter, primary);
            b.onClick.AddListener(() => { Audio.Sfx("click"); if (onClick != null) onClick(); });
            return b;
        }

        public static Image Divider(Transform parent, float x, float y, float w)
        {
            var d = UIKit.Img("Div", parent, Color.white); d.sprite = UiStyle.DividerSprite; d.raycastTarget = false;
            UIKit.At(d.rectTransform, x, y, w, 12f);
            return d;
        }

        public static string Num(int n) { return n.ToString("N0", System.Globalization.CultureInfo.InvariantCulture); }
    }
}
