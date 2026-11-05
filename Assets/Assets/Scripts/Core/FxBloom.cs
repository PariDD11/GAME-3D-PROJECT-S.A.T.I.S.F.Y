using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HunterGuild
{
    /// <summary>
    /// Bloom (pendar cahaya) otomatis untuk efek HDR (URP): membuat Volume global saat runtime dan menyalakan post-processing pada kamera utama.
    /// Ambang 1.0, jadi hanya efek bercahaya yang berpendar; dunia biasa tidak berubah. Matikan: PlayerPrefs hg_bloom = 0.
    /// Juga memanaskan tekstur efek di awal supaya tidak tersendat saat pertempuran pertama.
    /// </summary>
    public class FxBloom : MonoBehaviour
    {
        static FxBloom I;
        Volume vol; VolumeProfile prof; float tick = 1f; bool warmed; int warmFrame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("FxBloom");
            DontDestroyOnLoad(go);
            I = go.AddComponent<FxBloom>();
        }

        void Update()
        {
            if (!warmed)
            {
                warmFrame++;
                if (warmFrame == 90) { MagicFx.Prewarm(); warmed = true; }      // beberapa detik setelah masuk: bangun tekstur HD sekali saja
            }
            tick -= Time.unscaledDeltaTime;
            if (tick > 0f) return;
            tick = 1.5f;
            Apply();
        }

        void Apply()
        {
            var rp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (rp == null) return;
            bool on = FxQuality.Bloom;
            if (on) { try { if (!rp.supportsHDR) rp.supportsHDR = true; } catch { } }
            if (vol == null)
            {
                var g = new GameObject("FxBloomVolume"); g.transform.SetParent(transform, false);
                vol = g.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 50f;
                prof = ScriptableObject.CreateInstance<VolumeProfile>();
                var b = prof.Add<Bloom>(true);
                b.threshold.value = 1.1f; b.threshold.overrideState = true;
                b.intensity.value = 0.22f; b.intensity.overrideState = true;
                b.scatter.value = 0.62f; b.scatter.overrideState = true;
                b.clamp.value = 40f; b.clamp.overrideState = true;
                vol.sharedProfile = prof;
            }
            vol.weight = on ? 1f : 0f;
            var cam = Camera.main;
            if (cam != null && on)
            {
                var cd = cam.GetComponent<UniversalAdditionalCameraData>();
                if (cd == null) cd = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                if (!cd.renderPostProcessing) cd.renderPostProcessing = true;
            }
        }
    }
}
