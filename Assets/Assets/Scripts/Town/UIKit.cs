using System;
using UnityEngine;
using UnityEngine.UI;

namespace HunterGuild
{
    /// <summary>Helper pembuat elemen UI lewat kode (dipakai BackpackUI dan SkillUI).</summary>
    public static class UIKit
    {
        static Font font;

        public static Font F
        {
            get
            {
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
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        public static Image Img(string name, Transform parent, Color color)
        {
            var rt = Rect(name, parent);
            var im = rt.gameObject.AddComponent<Image>();
            im.color = color;
            return im;
        }

        public static Text Txt(string name, Transform parent, int size, TextAnchor anchor, Color color)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = F;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static void Place(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min; rt.anchorMax = max; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        /// <summary>Posisi dari pojok kiri-atas parent (x ke kanan, y ke bawah).</summary>
        public static void At(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        public static Button Btn(Transform parent, string label, Action onClick, bool enabled, Color bg, int fontSize)
        {
            var im = Img("Btn", parent, enabled ? bg : new Color(0.14f, 0.14f, 0.16f, 1f));
            var b = im.gameObject.AddComponent<Button>();
            b.targetGraphic = im;
            b.interactable = enabled;
            UiStyle.StyleButton(im, enabled);
            im.color = enabled ? Color.Lerp(Color.white, Color.Lerp(bg, UiStyle.Gold, 0.45f), 0.38f) : Color.white;     // warna bg jadi aksen halus, dicampur warna tema job

            var t = Txt("Label", im.transform, fontSize, TextAnchor.MiddleCenter, enabled ? UiStyle.Cream : new Color(0.55f, 0.55f, 0.55f));
            Place(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            t.rectTransform.offsetMin = new Vector2(6, 2);
            t.rectTransform.offsetMax = new Vector2(-6, -2);
            t.text = label;
            b.onClick.AddListener(() => Audio.Sfx("click"));
            if (onClick != null) b.onClick.AddListener(() => onClick());
            return b;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var c = t.GetChild(i);
                c.SetParent(null);
                UnityEngine.Object.Destroy(c.gameObject);
            }
        }
    }
}
