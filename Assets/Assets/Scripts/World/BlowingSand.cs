using UnityEngine;
using UnityEngine.Rendering;

namespace HunterGuild
{
    /// <summary>Pasir tertiup angin di dekat tanah (gurun): guratan pasir panjang dan kabut debu tipis, mengikuti kamera. Embusan berdenyut pelan.</summary>
    public class BlowingSand : MonoBehaviour
    {
        public Material material;
        public float area = 46f, rate = 90f;
        public Vector2 wind = new Vector2(7f, 2.2f);
        public Color sand = new Color(0.9f, 0.76f, 0.5f, 0.55f);
        ParticleSystem streaks, haze;
        Material mat;
        Transform cam;

        void Start()
        {
            mat = material != null ? new Material(material) : FxUtil.MakeMaterial(FxUtil.SoftDot(1.4f));
            if (mat.mainTexture == null) mat.mainTexture = FxUtil.SoftDot(1.4f);
            streaks = Make("SandStreaks", true);
            haze = Make("DustHaze", false);
            var c = Camera.main; cam = c != null ? c.transform : null;
            Follow();
            streaks.Play(); haze.Play();
            streaks.Simulate(4f, true, true); haze.Simulate(6f, true, true);
            streaks.Play(); haze.Play();
        }

        ParticleSystem Make(string name, bool stretched)
        {
            var ps = FxUtil.NewSystem(name, transform);
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            if (stretched) { r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.08f; r.lengthScale = 2.2f; }
            else r.renderMode = ParticleSystemRenderMode.Billboard;
            var main = ps.main;
            main.loop = true; main.playOnAwake = false; main.prewarm = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = stretched ? 900 : 120;
            main.startLifetime = stretched ? new ParticleSystem.MinMaxCurve(2.5f, 4.5f) : new ParticleSystem.MinMaxCurve(8f, 12f);
            main.startSpeed = 0f;
            main.startSize = stretched ? new ParticleSystem.MinMaxCurve(0.05f, 0.14f) : new ParticleSystem.MinMaxCurve(9f, 16f);
            main.startColor = stretched ? sand : new Color(sand.r, sand.g, sand.b, 0.07f);
            var em = ps.emission; em.enabled = true; em.rateOverTime = stretched ? rate : 5f;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(area, stretched ? 2.2f : 3f, area);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            float k = stretched ? 1f : 0.35f;
            vel.x = new ParticleSystem.MinMaxCurve(wind.x * 0.7f * k, wind.x * 1.3f * k);
            vel.y = new ParticleSystem.MinMaxCurve(-0.1f, 0.3f);
            vel.z = new ParticleSystem.MinMaxCurve(wind.y * 0.6f * k, wind.y * 1.4f * k);
            if (stretched)
            {
                var noise = ps.noise; noise.enabled = true; noise.strength = new ParticleSystem.MinMaxCurve(1.6f); noise.frequency = 0.5f; noise.scrollSpeed = 0.6f;
            }
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(fade);
            return ps;
        }

        void Follow()
        {
            if (cam == null) return;
            transform.position = cam.position + new Vector3(-wind.x * 1.2f, -0.2f, -wind.y * 1.2f);     // sedikit di hulu angin
        }

        void LateUpdate()
        {
            if (cam == null) { var c = Camera.main; cam = c != null ? c.transform : null; return; }
            Follow();
            float gust = Mathf.PerlinNoise(Time.time * 0.07f, 3.3f);
            var em = streaks.emission; em.rateOverTime = rate * (0.25f + 1.5f * gust);
            if (mat != null) mat.color = Color.Lerp(new Color(0.25f, 0.25f, 0.3f, 1f), Color.white, FxUtil.DayBrightness());
        }
    }
}
