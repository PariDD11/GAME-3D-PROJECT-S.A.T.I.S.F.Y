using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n8: pengaturan tambahan untuk panel Pengaturan baru. Semua disimpan di PlayerPrefs dan diterapkan sendiri
    /// (kecerahan, V-Sync, FOV, kualitas bayangan/tekstur, anti-aliasing, motion blur, depth of field, volume utama).
    /// Nilai -1 = "Otomatis" (tidak menyentuh bawaan project / pilihan Kualitas Grafis).
    /// </summary>
    public class SettingsExtra : MonoBehaviour
    {
        static SettingsExtra I;

        public static readonly string[] LevelNames = { "Rendah", "Sedang", "Tinggi" };
        public static readonly string[] ShadowNames = { "Otomatis", "Mati", "Rendah", "Sedang", "Tinggi" };
        public static readonly string[] AaNames = { "Otomatis", "Mati", "FXAA", "SMAA", "MSAA 4x" };

        static int Gi(string k, int d) { try { return PlayerPrefs.GetInt(k, d); } catch { return d; } }
        static void Si(string k, int v) { try { PlayerPrefs.SetInt(k, v); PlayerPrefs.Save(); } catch { } }

        // ---- nilai
        public static int Brightness { get { return Mathf.Clamp(Gi("hg_bright", 50), 0, 100); } set { Si("hg_bright", Mathf.Clamp(value, 0, 100)); Touch(); } }
        public static bool VSync { get { return Gi("hg_vsync", 0) == 1; } set { Si("hg_vsync", value ? 1 : 0); ApplyVSync(); } }
        public static int Fov { get { int v = Gi("hg_fov", -1); if (v > 0) return Mathf.Clamp(v, 50, 100); float o = ThirdPersonCamera.OrigFov; return Mathf.Clamp(Mathf.RoundToInt(o > 1f ? o : 60f), 50, 100); } set { Si("hg_fov", Mathf.Clamp(value, 50, 100)); } }
        /// <summary>Dipakai ThirdPersonCamera: -1 bila pemain belum mengubah FOV.</summary>
        public static float FovPref { get { int v = Gi("hg_fov", -1); return v > 0 ? v : -1f; } }
        public static int ShadowQ { get { return Mathf.Clamp(Gi("hg_shadowq", 0), 0, 4); } set { Si("hg_shadowq", value); ApplyQuality(); } }
        public static int TextureQ { get { return Mathf.Clamp(Gi("hg_texq", 2), 0, 2); } set { Si("hg_texq", value); ApplyQuality(); } }   // 0 rendah .. 2 tinggi (penuh)
        public static int Aa { get { return Mathf.Clamp(Gi("hg_aa", 0), 0, 4); } set { Si("hg_aa", value); ApplyAa(); } }
        public static bool MotionBlurOn { get { return Gi("hg_mblur", 0) == 1; } set { Si("hg_mblur", value ? 1 : 0); Touch(); } }
        public static bool DofOn { get { return Gi("hg_dof", 0) == 1; } set { Si("hg_dof", value ? 1 : 0); Touch(); } }
        public static bool BloomOn { get { return Gi("hg_bloom", 1) == 1; } set { Si("hg_bloom", value ? 1 : 0); } }
        public static float MasterVol { get { try { return Mathf.Clamp01(PlayerPrefs.GetFloat("hg_master", 1f)); } catch { return 1f; } } set { try { PlayerPrefs.SetFloat("hg_master", Mathf.Clamp01(value)); PlayerPrefs.Save(); } catch { } AudioListener.volume = Mathf.Clamp01(value); } }
        public static bool Rumble { get { return Gi("hg_rumble", 1) == 1; } set { Si("hg_rumble", value ? 1 : 0); } }
        // v35z102n23: getar gamepad (Input System baru). Dengan Input Manager lama tidak ada API getar, jadi tidak melakukan apa-apa.
        static float rumbleUntil;
        public static void Pulse(float low, float high, float secs)
        {
            if (!Rumble) return;
#if ENABLE_INPUT_SYSTEM
            try { var g = Gamepad.current; if (g == null) return; g.SetMotorSpeeds(Mathf.Clamp01(low), Mathf.Clamp01(high)); rumbleUntil = Time.unscaledTime + secs; }
            catch { }
#endif
        }
        static void RumbleTick()
        {
#if ENABLE_INPUT_SYSTEM
            if (rumbleUntil > 0f && Time.unscaledTime >= rumbleUntil) { rumbleUntil = 0f; try { var g = Gamepad.current; if (g != null) g.SetMotorSpeeds(0f, 0f); } catch { } }
#endif
        }

        public static void ResetAll()
        {
            foreach (var k in new[] { "hg_bright", "hg_vsync", "hg_fov", "hg_shadowq", "hg_texq", "hg_aa", "hg_mblur", "hg_dof", "hg_rumble" }) { try { PlayerPrefs.DeleteKey(k); } catch { } }
            try { PlayerPrefs.SetFloat("hg_master", 1f); PlayerPrefs.SetInt("hg_bloom", 1); PlayerPrefs.Save(); } catch { }
            AudioListener.volume = 1f;
            ApplyVSync(); ApplyQuality(); ApplyAa(); Touch();
        }

        // ---- boot
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("SettingsExtra"); DontDestroyOnLoad(go);
            I = go.AddComponent<SettingsExtra>();
        }

        Volume vol; VolumeProfile prof; ColorAdjustments ca; MotionBlur mb; DepthOfField dof; float poll = 0.5f; bool first = true;

        void Update()
        {
            RumbleTick();
            poll -= Time.unscaledDeltaTime;
            if (poll > 0f) return;
            poll = 1.5f;
            if (first)
            {
                first = false;
                AudioListener.volume = MasterVol;
                ApplyVSync(); ApplyQuality();
            }
            ApplyAa();
            Touch();
        }

        /// <summary>Terapkan ulang volume pasca-proses (kecerahan, motion blur, DoF).</summary>
        static void Touch() { if (I != null) I.Sync(); }

        void Sync()
        {
            try
            {
                if (vol == null)
                {
                    var g = new GameObject("SettingsVolume"); g.transform.SetParent(transform, false);
                    vol = g.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 90f; vol.weight = 1f;
                    prof = ScriptableObject.CreateInstance<VolumeProfile>();
                    ca = prof.Add<ColorAdjustments>(true); mb = prof.Add<MotionBlur>(true); dof = prof.Add<DepthOfField>(true);
                    ca.postExposure.overrideState = true;
                    mb.intensity.overrideState = true; mb.intensity.value = 0.45f; mb.quality.overrideState = true; mb.quality.value = MotionBlurQuality.Low;
                    dof.mode.overrideState = true; dof.mode.value = DepthOfFieldMode.Gaussian;
                    dof.gaussianStart.overrideState = true; dof.gaussianStart.value = 28f;
                    dof.gaussianEnd.overrideState = true; dof.gaussianEnd.value = 90f;
                    dof.gaussianMaxRadius.overrideState = true; dof.gaussianMaxRadius.value = 1.2f;
                    vol.sharedProfile = prof;
                }
                ca.postExposure.value = (Brightness - 50) / 50f * 1.0f;
                mb.active = MotionBlurOn; dof.active = DofOn;
            }
            catch { }
        }

        // ---- penerapan
        public static void ApplyVSync()
        {
            try
            {
                if (VSync) { QualitySettings.vSyncCount = 1; Application.targetFrameRate = -1; }
                else if (GameSettings.FpsCap > 0) { QualitySettings.vSyncCount = 0; Application.targetFrameRate = GameSettings.FpsCap; }
                else { QualitySettings.vSyncCount = 0; }
            }
            catch { }
        }

        public static void ApplyQuality()
        {
            try
            {
                int s = ShadowQ;
                if (s > 0)
                {
                    if (s == 1) { QualitySettings.shadows = UnityEngine.ShadowQuality.Disable; SetUrp("shadowDistance", 0.1f); }
                    else
                    {
                        QualitySettings.shadows = UnityEngine.ShadowQuality.All;
                        QualitySettings.shadowResolution = s == 2 ? UnityEngine.ShadowResolution.Low : s == 3 ? UnityEngine.ShadowResolution.High : UnityEngine.ShadowResolution.VeryHigh;
                        float d = s == 2 ? 40f : s == 3 ? 80f : 140f;
                        QualitySettings.shadowDistance = d; SetUrp("shadowDistance", d);
                        QualitySettings.shadowCascades = s == 2 ? 1 : s == 3 ? 2 : 4;
                    }
                }
                int t = TextureQ;                                    // 2 = penuh
#if UNITY_2022_2_OR_NEWER
                QualitySettings.globalTextureMipmapLimit = 2 - t;
#else
                QualitySettings.masterTextureLimit = 2 - t;
#endif
            }
            catch { }
        }

        public static void ApplyAa()
        {
            try
            {
                int a = Aa; if (a == 0) return;
                var cam = Camera.main; if (cam == null) return;
                var d = cam.GetComponent<UniversalAdditionalCameraData>();
                if (d != null)
                {
                    d.antialiasing = a == 2 ? AntialiasingMode.FastApproximateAntialiasing : a == 3 ? AntialiasingMode.SubpixelMorphologicalAntiAliasing : AntialiasingMode.None;
                }
                SetUrp("msaaSampleCount", a == 4 ? 4f : 1f);
            }
            catch { }
        }

        static void SetUrp(string prop, float val)
        {
            try
            {
                var rp = GraphicsSettings.currentRenderPipeline; if (rp == null) return;
                var p = rp.GetType().GetProperty(prop); if (p == null || !p.CanWrite) return;
                if (p.PropertyType == typeof(int)) p.SetValue(rp, Mathf.RoundToInt(val), null); else p.SetValue(rp, val, null);
            }
            catch { }
        }
    }
}
