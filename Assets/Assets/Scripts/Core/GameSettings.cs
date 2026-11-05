using UnityEngine;
using UnityEngine.Rendering;

namespace HunterGuild
{
    /// <summary>
    /// Pengaturan grafik dan FPS (disimpan di PlayerPrefs, diterapkan otomatis saat game mulai).
    /// Grafik: Normal = tampilan project apa adanya (nilai awal), Medium dan HD menaikkan bayangan, anti-aliasing, filter tekstur, dan jarak detail.
    /// FPS: batas frame (30/60/90/120/Bebas) dan penghitung FPS di layar.
    /// </summary>
    public class GameSettings : MonoBehaviour
    {
        public const int Normal = 0, Medium = 1, HD = 2;
        public static readonly string[] GraphicsNames = { "Normal", "Medium", "HD" };
        public static readonly int[] FpsOptions = { 0, 30, 60, 90, 120 };       // 0 = tanpa batas

        public static int Graphics { get; private set; }
        public static int FpsCap { get; private set; }
        public static bool ShowFps { get; private set; }

        // ---- resolusi / layar penuh / skala render ----
        public static bool Fullscreen { get; private set; }
        public static int ResW { get; private set; }
        public static int ResH { get; private set; }
        public static float RenderScale { get; private set; }
        public static readonly float[] ScaleOptions = { 0.6f, 0.75f, 0.9f, 1f, 1.25f };
        static System.Collections.Generic.List<Vector2Int> resList;

        static System.Collections.Generic.List<Vector2Int> Resolutions()
        {
            if (resList != null) return resList;
            resList = new System.Collections.Generic.List<Vector2Int>();
            foreach (var r in Screen.resolutions)
            {
                var v = new Vector2Int(r.width, r.height);
                if (r.width >= 800 && !resList.Contains(v)) resList.Add(v);
            }
            if (resList.Count == 0) resList.Add(new Vector2Int(1280, 720));
            return resList;
        }
        public static string ResLabel { get { return ResW + " x " + ResH; } }
        public static string ScaleLabel { get { return Mathf.RoundToInt(RenderScale * 100f) + "%"; } }
        public static void CycleResolution(int dir)
        {
            var l = Resolutions(); int i = l.FindIndex(v => v.x == ResW && v.y == ResH); if (i < 0) i = l.Count - 1;
            i = (i + dir + l.Count) % l.Count; ResW = l[i].x; ResH = l[i].y;
            PlayerPrefs.SetInt("hg_resw", ResW); PlayerPrefs.SetInt("hg_resh", ResH); PlayerPrefs.Save(); ApplyScreen();
        }
        public static void ToggleFullscreen() { Fullscreen = !Fullscreen; PlayerPrefs.SetInt("hg_full", Fullscreen ? 1 : 0); PlayerPrefs.Save(); ApplyScreen(); }
        public static void CycleScale()
        {
            int i = 0; for (int k = 0; k < ScaleOptions.Length; k++) if (Mathf.Approximately(ScaleOptions[k], RenderScale)) i = k;
            RenderScale = ScaleOptions[(i + 1) % ScaleOptions.Length];
            PlayerPrefs.SetFloat("hg_rscale", RenderScale); PlayerPrefs.Save(); Apply();
        }
        static void ApplyScreen()
        {
#if !UNITY_EDITOR
            Screen.SetResolution(ResW, ResH, Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
#endif
        }

        static GameSettings inst;
        static bool baselineTaken;
        // nilai awal project (= "Normal")
        static ShadowResolution bShadowRes; static float bShadowDist; static int bCascades, bMsaa, bPixelLights, bVSync;
        static AnisotropicFiltering bAniso; static float bLod; static ShadowQuality bShadows;
        static object urpAsset; static float bScale; static int bUrpMsaa; static float bUrpShadowDist; static bool bUrpHdr;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (inst != null) return;
            var go = new GameObject("GameSettings");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<GameSettings>();
            TakeBaseline();
            Graphics = Mathf.Clamp(PlayerPrefs.GetInt("hg_gfx", Normal), 0, 2);
            FpsCap = PlayerPrefs.GetInt("hg_fps", 0);
            ShowFps = PlayerPrefs.GetInt("hg_showfps", 0) == 1;
            Fullscreen = PlayerPrefs.GetInt("hg_full", Screen.fullScreen ? 1 : 0) == 1;
            ResW = PlayerPrefs.GetInt("hg_resw", Screen.width); ResH = PlayerPrefs.GetInt("hg_resh", Screen.height);
            RenderScale = PlayerPrefs.GetFloat("hg_rscale", 1f);
            if (PlayerPrefs.HasKey("hg_resw")) ApplyScreen();
            Apply();
        }

        static void TakeBaseline()
        {
            if (baselineTaken) return;
            baselineTaken = true;
            bShadowRes = QualitySettings.shadowResolution; bShadowDist = QualitySettings.shadowDistance; bCascades = QualitySettings.shadowCascades;
            bMsaa = QualitySettings.antiAliasing; bPixelLights = QualitySettings.pixelLightCount; bVSync = QualitySettings.vSyncCount;
            bAniso = QualitySettings.anisotropicFiltering; bLod = QualitySettings.lodBias; bShadows = QualitySettings.shadows;
            urpAsset = GraphicsSettings.currentRenderPipeline;
            if (urpAsset != null)
            {
                bScale = GetF("renderScale", 1f); bUrpMsaa = (int)GetF("msaaSampleCount", 1f); bUrpShadowDist = GetF("shadowDistance", 50f);
                bUrpHdr = GetF("supportsHDR", 1f) > 0.5f;
            }
        }

        // ---------- API ----------
        public static void SetGraphics(int level) { Graphics = Mathf.Clamp(level, 0, 2); PlayerPrefs.SetInt("hg_gfx", Graphics); PlayerPrefs.Save(); Apply(); }
        public static void CycleFps()
        {
            int i = System.Array.IndexOf(FpsOptions, FpsCap);
            FpsCap = FpsOptions[(i + 1) % FpsOptions.Length];
            PlayerPrefs.SetInt("hg_fps", FpsCap); PlayerPrefs.Save(); Apply();
        }
        public static void ToggleShowFps() { ShowFps = !ShowFps; PlayerPrefs.SetInt("hg_showfps", ShowFps ? 1 : 0); PlayerPrefs.Save(); }
        public static string FpsLabel { get { return FpsCap == 0 ? "Tanpa batas" : FpsCap + " FPS"; } }

        // ---------- terapkan ----------
        public static void Apply()
        {
            TakeBaseline();
            if (Graphics == Normal)
            {
                QualitySettings.shadows = bShadows; QualitySettings.shadowResolution = bShadowRes; QualitySettings.shadowDistance = bShadowDist;
                QualitySettings.shadowCascades = bCascades; QualitySettings.antiAliasing = bMsaa; QualitySettings.pixelLightCount = bPixelLights;
                QualitySettings.anisotropicFiltering = bAniso; QualitySettings.lodBias = bLod;
                if (urpAsset != null) { SetF("renderScale", bScale); SetF("msaaSampleCount", bUrpMsaa); SetF("shadowDistance", bUrpShadowDist); SetF("supportsHDR", bUrpHdr ? 1f : 0f); }
            }
            else
            {
                bool hd = Graphics == HD;
                if (QualitySettings.shadows == ShadowQuality.Disable) QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.shadowResolution = hd ? ShadowResolution.VeryHigh : ShadowResolution.High;
                float dist = hd ? Mathf.Max(bShadowDist, 160f) : Mathf.Max(bShadowDist, 90f);
                QualitySettings.shadowDistance = dist;
                QualitySettings.shadowCascades = hd ? 4 : 2;
                QualitySettings.antiAliasing = hd ? 4 : 2;
                QualitySettings.pixelLightCount = Mathf.Max(bPixelLights, hd ? 8 : 4);
                QualitySettings.anisotropicFiltering = hd ? AnisotropicFiltering.ForceEnable : AnisotropicFiltering.Enable;
                QualitySettings.lodBias = Mathf.Max(bLod, hd ? 2f : 1.5f);
                if (urpAsset != null)
                {
                    SetF("renderScale", hd ? 1.1f : 1f); SetF("msaaSampleCount", hd ? 4 : 2); SetF("shadowDistance", dist); SetF("supportsHDR", 1f);
                    SetF("shadowCascadeCount", hd ? 4 : 2);
                }
            }
            // skala render pilihan pemain (hanya bila diubah dari 100%)
            if (urpAsset != null && !Mathf.Approximately(RenderScale, 1f)) SetF("renderScale", RenderScale * (Graphics == HD ? 1.1f : 1f));
            // FPS
            if (FpsCap == 0) { QualitySettings.vSyncCount = bVSync; Application.targetFrameRate = -1; }
            else { QualitySettings.vSyncCount = 0; Application.targetFrameRate = FpsCap; }
        }

        // ---------- URP lewat reflection (project bisa Built-in atau URP) ----------
        static float GetF(string prop, float def)
        {
            try
            {
                var p = urpAsset.GetType().GetProperty(prop);
                if (p == null) return def;
                object v = p.GetValue(urpAsset, null);
                if (v is bool) return (bool)v ? 1f : 0f;
                return System.Convert.ToSingle(v);
            }
            catch { return def; }
        }

        static void SetF(string prop, float val)
        {
            try
            {
                var p = urpAsset.GetType().GetProperty(prop);
                if (p == null || !p.CanWrite) return;
                if (p.PropertyType == typeof(bool)) p.SetValue(urpAsset, val > 0.5f, null);
                else if (p.PropertyType == typeof(int)) p.SetValue(urpAsset, Mathf.RoundToInt(val), null);
                else p.SetValue(urpAsset, val, null);
            }
            catch { }
        }

        // ---------- penghitung FPS ----------
        float acc, frames, shown;

        void Update()
        {
            acc += Time.unscaledDeltaTime; frames++;
            if (acc >= 0.5f) { shown = frames / acc; acc = 0f; frames = 0f; }
        }

        void OnGUI()
        {
            if (!ShowFps) return;
            var style = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperLeft };
            string t = "FPS " + Mathf.RoundToInt(shown) + "   (" + (shown > 0f ? (1000f / shown).ToString("0.0") : "-") + " ms)   " + GraphicsNames[Graphics];
            var r = new Rect(10, 6, 340, 26);
            GUI.color = new Color(0, 0, 0, 0.75f); GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), t, style);
            GUI.color = shown >= 55f ? new Color(0.5f, 1f, 0.5f) : shown >= 30f ? new Color(1f, 0.9f, 0.4f) : new Color(1f, 0.4f, 0.4f);
            GUI.Label(r, t, style);
            GUI.color = Color.white;
        }

#if UNITY_EDITOR
        // di Editor, asset URP dan QualitySettings ikut tersimpan: kembalikan ke nilai awal saat berhenti
        void OnApplicationQuit() { Graphics = Normal; RenderScale = 1f; var f = FpsCap; FpsCap = 0; Apply(); FpsCap = f; }
#endif
    }
}
