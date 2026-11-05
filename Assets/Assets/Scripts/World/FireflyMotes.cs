using UnityEngine;
using UnityEngine.Rendering;

namespace HunterGuild
{
    /// <summary>Kunang-kunang / spora bercahaya di dekat tanah, muncul saat sore dan malam.</summary>
    public class FireflyMotes : MonoBehaviour
    {
        public Material material;
        public float area = 34f, maxRate = 22f;
        ParticleSystem ps;
        Transform cam;

        void Start()
        {
            ps = FxUtil.NewSystem("Fireflies", transform);
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = material != null ? new Material(material) : FxUtil.MakeMaterial(FxUtil.SoftDot(1.6f));
            if (r.sharedMaterial.mainTexture == null) r.sharedMaterial.mainTexture = FxUtil.SoftDot(1.6f);

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 300;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.09f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 1f, 0.45f, 1f), new Color(1f, 0.9f, 0.4f, 1f));

            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(area, 3f, area);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(0.9f);
            noise.frequency = 0.5f;
            noise.scrollSpeed = 0.5f;

            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0.35f, 0.5f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(fade);

            var em = ps.emission;
            em.rateOverTime = 0f;
            ps.Play();
        }

        float NightFactor()
        {
            var gs = GameState.I;
            if (gs == null) return 1f - FxUtil.DayBrightness();
            float t = gs.timeOfDay;
            if (t >= 17.5f) return Mathf.Clamp01((t - 17.5f) / 2f);
            if (t <= 6.5f) return Mathf.Clamp01((6.5f - t) / 1.5f);
            return 0f;
        }

        void LateUpdate()
        {
            if (cam == null) { var c = Camera.main; if (c != null) cam = c.transform; return; }
            Vector3 p = cam.position;
            p.y = RT.GroundY(p) + 1.8f;
            transform.position = p;
            var em = ps.emission;
            em.rateOverTime = maxRate * NightFactor();
        }
    }
}
