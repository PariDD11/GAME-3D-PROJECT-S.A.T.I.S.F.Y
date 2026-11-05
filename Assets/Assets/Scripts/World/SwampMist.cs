using UnityEngine;
using UnityEngine.Rendering;

namespace HunterGuild
{
    /// <summary>Kabut rendah dan cahaya hantu (wisp) yang mengikuti kamera, untuk Rawa Terkutuk.</summary>
    public class SwampMist : MonoBehaviour
    {
        public Material material;
        public float area = 60f;
        ParticleSystem haze, wisps;
        Material mat;
        Transform cam;

        void Start()
        {
            mat = material != null ? new Material(material) : FxUtil.MakeMaterial(FxUtil.SoftDot(1.4f));
            if (mat.mainTexture == null) mat.mainTexture = FxUtil.SoftDot(1.4f);
            haze = Make("MistHaze", false);
            wisps = Make("SpiritWisps", true);
            var c = Camera.main; cam = c != null ? c.transform : null;
            Follow();
            haze.Simulate(10f, true, true); wisps.Simulate(8f, true, true);
            haze.Play(); wisps.Play();
        }

        ParticleSystem Make(string name, bool wisp)
        {
            var ps = FxUtil.NewSystem(name, transform);
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            var main = ps.main;
            main.loop = true; main.playOnAwake = false; main.prewarm = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = wisp ? 90 : 90;
            main.startLifetime = wisp ? new ParticleSystem.MinMaxCurve(5f, 9f) : new ParticleSystem.MinMaxCurve(9f, 14f);
            main.startSpeed = 0f;
            main.startSize = wisp ? new ParticleSystem.MinMaxCurve(0.18f, 0.4f) : new ParticleSystem.MinMaxCurve(14f, 24f);
            main.startColor = wisp ? new Color(0.55f, 1f, 0.75f, 0.9f) : new Color(0.6f, 0.72f, 0.66f, 0.10f);
            var em = ps.emission; em.enabled = true; em.rateOverTime = wisp ? 11f : 8f;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(area, wisp ? 3.5f : 2.2f, area);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
            vel.y = new ParticleSystem.MinMaxCurve(wisp ? 0.05f : -0.05f, wisp ? 0.35f : 0.1f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
            var noise = ps.noise; noise.enabled = true; noise.strength = new ParticleSystem.MinMaxCurve(wisp ? 1.4f : 0.6f); noise.frequency = 0.25f; noise.scrollSpeed = 0.3f;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(fade);
            return ps;
        }

        void Follow()
        {
            if (cam == null) return;
            transform.position = cam.position + new Vector3(0f, -1.2f, 0f);
        }

        void LateUpdate()
        {
            if (cam == null) { var c = Camera.main; cam = c != null ? c.transform : null; return; }
            Follow();
        }
    }
}
