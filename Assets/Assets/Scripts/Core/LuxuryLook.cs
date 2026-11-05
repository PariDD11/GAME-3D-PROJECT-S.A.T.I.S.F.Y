using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n6 "Megah &amp; Mewah": lapisan tampilan global yang berjalan sendiri (tanpa perlu dipasang di scene).
    /// - Volume pasca-proses tambahan (bobot 0.65) di atas AnimeLook: bloom lebih lebar, kontras &amp; saturasi emas-hangat, vignette lembut.
    /// - Matahari: warna hangat, bayangan lunak, sudut lebih rendah (cahaya sore). Ambient trilight hangat-biru. Kabut tipis berlapis.
    /// - Aset URP (lewat try/catch): MSAA 4x, HDR, bayangan lebih jauh &amp; lunak, cascade 4.
    /// - Debu cahaya emas melayang di sekitar kamera.
    /// Matikan dengan LuxuryLook.Toggle() (PlayerPrefs "hg_lux"); berlaku juga untuk RoyalFx/MonsterRoyal/PlayerRoyal.
    /// </summary>
    public class LuxuryLook : MonoBehaviour
    {
        public static LuxuryLook I { get; private set; }
        public static bool On { get { return RoyalFx.On; } }
        public static void Toggle() { RoyalFx.Toggle(); if (I != null) I.Refresh(); }

        Volume vol; VolumeProfile prof; ParticleSystem dust; Light sun; float poll;
        bool applied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("LuxuryLook");
            DontDestroyOnLoad(go);
            I = go.AddComponent<LuxuryLook>();
        }

        void Start() { Refresh(); }

        void Refresh()
        {
            if (!On) { Restore(); return; }
            if (vol == null) BuildVolume();
            if (vol != null) vol.weight = 0.6f;
            ApplyPipeline();
            ApplyScene();
            if (probe == null) BuildProbe();
            if (dust == null) BuildDust();
            if (dust != null) dust.gameObject.SetActive(true);
        }

        void Restore()
        {
            if (vol != null) vol.weight = 0f;
            if (dust != null) dust.gameObject.SetActive(false);
            if (probe != null) { Destroy(probe.gameObject); probe = null; }
            applied = false;
        }

        void BuildVolume()
        {
            try
            {
                var g = new GameObject("LuxuryVolume"); g.transform.SetParent(transform, false);
                vol = g.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 70f; vol.weight = 0.65f;
                prof = ScriptableObject.CreateInstance<VolumeProfile>();
                var bl = prof.Add<Bloom>(true); var ca = prof.Add<ColorAdjustments>(true); var vg = prof.Add<Vignette>(true);
                bl.threshold.value = 1.1f; bl.intensity.value = 0.18f; bl.scatter.value = 0.6f; bl.tint.value = new Color(1f, 0.95f, 0.85f);
                ca.postExposure.value = -0.1f; ca.contrast.value = 4f; ca.saturation.value = -6f; ca.colorFilter.value = new Color(1f, 0.97f, 0.92f);
                vg.intensity.value = 0.14f; vg.smoothness.value = 0.8f;
                foreach (var p in new VolumeParameter[] { bl.threshold, bl.intensity, bl.scatter, bl.tint, ca.postExposure, ca.contrast, ca.saturation, ca.colorFilter, vg.intensity, vg.smoothness }) p.overrideState = true;
                vol.sharedProfile = prof;
            }
            catch { }
            ExtraGrade();
        }

        /// <summary>Tonemapping ACES, split-toning (bayangan biru-sejuk, highlight hangat), DOF halus, film grain tipis: bikin gambar sinematik, bukan datar.</summary>
        void ExtraGrade()
        {
            if (prof == null) return;
            try
            {
                var tm = prof.Add<Tonemapping>(true); tm.mode.value = TonemappingMode.ACES; tm.mode.overrideState = true;
            }
            catch { }
            try
            {
                var smh = prof.Add<ShadowsMidtonesHighlights>(true);
                smh.shadows.value = new Vector4(0.86f, 0.92f, 1.08f, -0.06f);
                smh.midtones.value = new Vector4(1f, 1f, 1f, 0f);
                smh.highlights.value = new Vector4(1.08f, 1.02f, 0.9f, 0.05f);
                smh.shadows.overrideState = true; smh.midtones.overrideState = true; smh.highlights.overrideState = true;
            }
            catch { }
            try
            {
                var lgg = prof.Add<LiftGammaGain>(true);
                lgg.lift.value = new Vector4(1f, 1f, 1.03f, -0.02f); lgg.lift.overrideState = true;
                lgg.gain.value = new Vector4(1.02f, 1f, 0.97f, 0.03f); lgg.gain.overrideState = true;
            }
            catch { }
            try
            {
                var dof = prof.Add<DepthOfField>(true);
                dof.mode.value = DepthOfFieldMode.Gaussian; dof.mode.overrideState = true;
                dof.gaussianStart.value = 90f; dof.gaussianEnd.value = 260f; dof.gaussianMaxRadius.value = 0.9f;
                dof.gaussianStart.overrideState = true; dof.gaussianEnd.overrideState = true; dof.gaussianMaxRadius.overrideState = true;
            }
            catch { }
            try
            {
                var cab = prof.Add<ChromaticAberration>(true);
                cab.intensity.value = 0f; cab.intensity.overrideState = true;
            }
            catch { }
            try
            {
                var mb = prof.Add<MotionBlur>(true);
                mb.mode.value = MotionBlurMode.CameraOnly; mb.intensity.value = 0f; mb.quality.value = MotionBlurQuality.Low;
                mb.mode.overrideState = true; mb.intensity.overrideState = true; mb.quality.overrideState = true;
            }
            catch { }
            try
            {
                var fg = prof.Add<FilmGrain>(true);
                fg.type.value = FilmGrainLookup.Thin1; fg.intensity.value = 0.06f; fg.response.value = 0.8f;
                fg.type.overrideState = true; fg.intensity.overrideState = true; fg.response.overrideState = true;
            }
            catch { }
        }

        /// <summary>Pengaturan aset URP lewat API publik; dibungkus try/catch karena properti berbeda antar versi URP.</summary>
        void ApplyPipeline()
        {
            if (applied) return; applied = true;
            try
            {
                var rp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                if (rp == null) return;
                try { rp.msaaSampleCount = 4; } catch { }
                try { rp.supportsHDR = true; } catch { }
                try { rp.shadowDistance = Mathf.Max(rp.shadowDistance, 70f); } catch { }
                try { rp.shadowCascadeCount = 4; } catch { }
                try { rp.supportsCameraDepthTexture = true; } catch { }
                try { rp.supportsCameraOpaqueTexture = true; } catch { }
            }
            catch { }
            try { QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable; } catch { }
        }

        void ApplyScene()
        {
            try
            {
                sun = RenderSettings.sun;
                if (sun == null) foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) { sun = l; break; }
                if (sun != null)
                {
                    sun.color = Color.Lerp(sun.color, new Color(1f, 0.93f, 0.8f), 0.6f);
                    sun.shadows = LightShadows.Soft; sun.shadowStrength = Mathf.Max(sun.shadowStrength, 0.85f);
                }
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.5f, 0.62f, 0.82f);
                RenderSettings.ambientEquatorColor = new Color(0.6f, 0.58f, 0.55f);
                RenderSettings.ambientGroundColor = new Color(0.34f, 0.32f, 0.3f);
                RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = Mathf.Min(RenderSettings.fogDensity > 0f ? RenderSettings.fogDensity : 0.006f, 0.008f);
                RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, new Color(0.7f, 0.74f, 0.86f), 0.5f);
            }
            catch { }
        }

        ReflectionProbe probe; float probeT;

        /// <summary>Probe pantulan realtime mengikuti kamera: permukaan licin (logam, kaca, air, emas) memantulkan langit &amp; sekitar, bukan warna datar ala plastik.</summary>
        void BuildProbe()
        {
            try
            {
                var go = new GameObject("LuxuryProbe"); go.transform.SetParent(transform, false);
                probe = go.AddComponent<ReflectionProbe>();
                probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
                probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
                probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.IndividualFaces;
                probe.resolution = 128; probe.hdr = true;
                probe.size = new Vector3(260f, 80f, 260f); probe.center = Vector3.zero;
                probe.intensity = 1f; probe.boxProjection = false;
                probe.nearClipPlane = 0.3f; probe.farClipPlane = 400f;
                probe.cullingMask = ~0;
                probeT = 0f;
            }
            catch { probe = null; }
        }

        void BuildDust()
        {
            var go = new GameObject("RoyalDust"); go.transform.SetParent(transform, false);
            dust = go.AddComponent<ParticleSystem>();
            dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = dust.main;
            main.loop = true; main.playOnAwake = false; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.075f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.6f), new Color(1f, 1f, 0.9f));
            main.maxParticles = 140;
            var em = dust.emission; em.rateOverTime = 14f * FxQuality.Count;
            var sp = dust.shape; sp.shapeType = ParticleSystemShapeType.Box; sp.scale = new Vector3(40f, 10f, 40f);
            var nz = dust.noise; nz.enabled = true; nz.strength = 0.4f; nz.frequency = 0.3f;
            var cl = dust.colorOverLifetime; cl.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.7f, 0.3f), new GradientAlphaKey(0f, 1f) });
            cl.color = g;
            dust.GetComponent<ParticleSystemRenderer>().sharedMaterial = CombatFx.Mat(CombatFx.Soft(), new Color(1f, 0.92f, 0.65f) * 0.8f);
            dust.Play();
        }

        void LateUpdate()
        {
            if (!On) { if (dust != null && dust.gameObject.activeSelf) Restore(); return; }
            var cam = Camera.main;
            if (dust != null && cam != null) dust.transform.position = cam.transform.position + Vector3.up * 2f;
            if (probe != null && cam != null)
            {
                probe.transform.position = cam.transform.position + Vector3.up * 3f;
                probeT -= Time.unscaledDeltaTime;
                if (probeT <= 0f) { probeT = 4f; try { probe.RenderProbe(); } catch { } }
            }
            poll -= Time.unscaledDeltaTime;
            if (poll > 0f) return;
            poll = 3f;                                       // scene baru / matahari baru → pasang ulang tanpa menumpuk
            if (vol != null && vol.weight < 0.01f) Refresh();
            else if (sun == null) ApplyScene();      // jangan menimpa siklus siang-malam tiap detik
        }
    }
}
