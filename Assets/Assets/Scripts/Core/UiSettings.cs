using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Pengaturan kenyamanan UI (disimpan di PlayerPrefs): skala UI, skala teks, tema warna, HUD redup saat tenang, animasi panel.
    /// Skala UI/teks/HUD redup/animasi berlaku langsung; tema warna berlaku di scene berikutnya (warna dibaca saat UI dibangun).
    /// </summary>
    public static class UiSettings
    {
        public static readonly string[] ThemeNames = { "Emas Klasik", "Hangat Lembut", "Kontras Tinggi" };
        const string K = "HG_ui_";

        static int Geti(string k, int d) { try { return PlayerPrefs.GetInt(K + k, d); } catch { return d; } }
        static void Seti(string k, int v) { try { PlayerPrefs.SetInt(K + k, v); PlayerPrefs.Save(); } catch { } }

        /// <summary>Skala keseluruhan UI, langkah 10%: 80% .. 130%.</summary>
        public static float Scale { get { return Mathf.Clamp(Geti("scale", 100), 80, 130) / 100f; } }
        /// <summary>Skala ukuran teks, langkah 5%: 90% .. 120%.</summary>
        public static float TextScale { get { return Mathf.Clamp(Geti("text", 100), 90, 120) / 100f; } }
        public static int Theme { get { return Mathf.Clamp(Geti("theme", 0), 0, ThemeNames.Length - 1); } }
        public static bool HudFade { get { return Geti("fade", 1) == 1; } }
        public static bool Anim { get { return Geti("anim", 1) == 1; } }

        public static void StepScale(int dir) { Seti("scale", Mathf.Clamp(Mathf.RoundToInt(Scale * 100f) + dir * 10, 80, 130)); Apply(); }
        public static void StepText(int dir) { Seti("text", Mathf.Clamp(Mathf.RoundToInt(TextScale * 100f) + dir * 5, 90, 120)); Apply(); }
        public static void CycleTheme() { Seti("theme", (Theme + 1) % ThemeNames.Length); }
        public static void ToggleFade() { Seti("fade", HudFade ? 0 : 1); Apply(); }
        public static void ToggleAnim() { Seti("anim", Anim ? 0 : 1); }
        public static void Reset() { Seti("scale", 100); Seti("text", 100); Seti("fade", 1); Seti("anim", 1); Apply(); }

        public static void Apply() { if (TownUI.I != null) TownUI.I.ApplyUiSettings(); }
    }

    /// <summary>Animasi muncul halus (fade + membesar sedikit) untuk panel/dialog. Memakai waktu tak terskala agar jalan saat jeda.</summary>
    public class UiPop : MonoBehaviour
    {
        CanvasGroup cg; float t = 1f; const float Dur = 0.16f;

        public static void Play(GameObject go)
        {
            if (go == null || !UiSettings.Anim) return;
            var p = go.GetComponent<UiPop>();
            if (p == null) p = go.AddComponent<UiPop>();
            p.Begin();
        }

        void Begin()
        {
            if (cg == null) { cg = GetComponent<CanvasGroup>(); if (cg == null) cg = gameObject.AddComponent<CanvasGroup>(); }
            t = 0f; cg.alpha = 0f; transform.localScale = Vector3.one * 0.965f;
        }

        void Update()
        {
            if (t >= 1f) return;
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / Dur);
            float e = 1f - (1f - t) * (1f - t);
            if (cg != null) cg.alpha = e;
            transform.localScale = Vector3.one * Mathf.Lerp(0.965f, 1f, e);
            if (t >= 1f) { if (cg != null) cg.alpha = 1f; transform.localScale = Vector3.one; }
        }

        void OnDisable() { if (t < 1f) { t = 1f; if (cg != null) cg.alpha = 1f; transform.localScale = Vector3.one; } }
    }
}
