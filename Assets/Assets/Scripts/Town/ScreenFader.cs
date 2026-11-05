using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HunterGuild
{
    /// <summary>Layar hitam untuk transisi antar scene (fade out, muat scene, fade in). Hidup lintas scene.</summary>
    public class ScreenFader : MonoBehaviour
    {
        static ScreenFader inst;

        public static ScreenFader I
        {
            get
            {
                if (inst == null)
                {
                    var go = new GameObject("ScreenFader");
                    DontDestroyOnLoad(go);
                    inst = go.AddComponent<ScreenFader>();
                    inst.Build();
                }
                return inst;
            }
        }

        public bool Busy { get; private set; }
        Image img;

        void Build()
        {
            var cg = new GameObject("FadeCanvas");
            cg.transform.SetParent(transform, false);
            var canvas = cg.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var ig = new GameObject("Fade");
            ig.transform.SetParent(cg.transform, false);
            img = ig.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public void Transition(Action load, float outTime, float inTime)
        {
            if (Busy) return;
            StartCoroutine(Run(load, outTime, inTime));
        }

        IEnumerator Run(Action load, float outT, float inT)
        {
            Busy = true;
            yield return Fade(0f, 1f, outT);
            load();
            yield return null;
            yield return null;      // beri waktu scene baru menaruh pemain di titik spawn
            yield return Fade(1f, 0f, inT);
            Busy = false;
        }

        IEnumerator Fade(float a, float b, float t)
        {
            float e = 0f;
            while (e < t)
            {
                e += Time.unscaledDeltaTime;
                img.color = new Color(0, 0, 0, Mathf.Lerp(a, b, e / t));
                yield return null;
            }
            img.color = new Color(0, 0, 0, b);
        }
    }
}
