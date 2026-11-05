using UnityEngine;
using UnityEngine.UI;

namespace HunterGuild
{
    /// <summary>Efek layar skill ultimate: vignette berwarna elemen yang berdenyut, kilatan putih, dan slow-motion singkat.</summary>
    public class UltimateFx : MonoBehaviour
    {
        static UltimateFx inst;
        static Sprite vig;
        Image img, flash;
        Color col;
        float t0 = -99f, dur = 1.5f;
        bool slowSet; float slowScale;

        const float SlowDur = 0.28f, SlowScale = 0.35f;

        public static void Play(Color c, string name)
        {
            if (inst == null)
            {
                var go = new GameObject("UltimateFx");
                DontDestroyOnLoad(go);
                inst = go.AddComponent<UltimateFx>();
                inst.Build();
            }
            inst.Begin(c);
        }

        static Sprite Vig()
        {
            if (vig != null) return vig;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(u * u + v * v) / 1.2f;
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.45f) / 0.55f));
                    px[y * n + x] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels(px); tex.Apply(false, false);
            vig = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            return vig;
        }

        void Build()
        {
            var cv = gameObject.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 400;
            var fl = new GameObject("Flash", typeof(RectTransform));
            fl.transform.SetParent(transform, false);
            flash = fl.AddComponent<Image>(); flash.raycastTarget = false; flash.color = Color.clear;
            Stretch(flash.rectTransform);
            var vg = new GameObject("Vignette", typeof(RectTransform));
            vg.transform.SetParent(transform, false);
            img = vg.AddComponent<Image>(); img.sprite = Vig(); img.raycastTarget = false; img.color = Color.clear;
            Stretch(img.rectTransform);
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        void Begin(Color c)
        {
            col = c; t0 = Time.unscaledTime;
            if (!slowSet && Mathf.Abs(Time.timeScale - 1f) < 0.01f)
            {
                Time.timeScale = SlowScale; slowScale = SlowScale; slowSet = true;
            }
        }

        void RestoreSlow()
        {
            if (!slowSet) return;
            if (Mathf.Abs(Time.timeScale - slowScale) < 0.01f) Time.timeScale = 1f;
            slowSet = false;
        }

        void Update()
        {
            float t = Time.unscaledTime - t0;
            if (slowSet && t > SlowDur) RestoreSlow();
            if (t < 0f || t > dur) { if (img.color.a > 0f) { img.color = Color.clear; flash.color = Color.clear; } return; }
            float inK = Mathf.Clamp01(t / 0.12f);
            float outK = t < 0.7f ? 1f : Mathf.Clamp01(1f - (t - 0.7f) / (dur - 0.7f));
            float pulse = 0.85f + 0.15f * Mathf.Sin(t * 14f);
            float a = inK * outK * pulse;
            Color vc = Color.Lerp(col, new Color(col.r * 0.6f, col.g * 0.6f, col.b * 0.6f), 0.4f);
            vc.a = Mathf.Clamp01(a * 0.85f);
            img.color = vc;
            float fa = Mathf.Clamp01(1f - t / 0.18f) * 0.45f;
            flash.color = new Color(1f, 1f, 1f, fa);
        }

        void OnDisable() { RestoreSlow(); }
    }
}
