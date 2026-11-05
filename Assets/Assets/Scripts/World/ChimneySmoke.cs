using UnityEngine;
using UnityEngine.Rendering;

namespace HunterGuild
{
    /// <summary>Asap tipis dari cerobong rumah desa.</summary>
    public class ChimneySmoke : MonoBehaviour
    {
        public Material material;

        void Start()
        {
            var ps = FxUtil.NewSystem("Smoke", transform);
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = material != null ? new Material(material) : FxUtil.MakeMaterial(FxUtil.SoftDot(1.1f));
            if (r.sharedMaterial.mainTexture == null) r.sharedMaterial.mainTexture = FxUtil.SoftDot(1.1f);

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 40;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 6.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            main.startColor = new Color(0.7f, 0.7f, 0.72f, 0.45f);

            var em = ps.emission;
            em.rateOverTime = 3.5f;

            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = 7f;
            sh.radius = 0.12f;
            sh.rotation = new Vector3(-90f, 0f, 0f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);   // x/y/z harus sama mode (TwoConstants), kalau tidak Unity spam error
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 2.6f)));

            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.7f, 0.15f), new GradientAlphaKey(0f, 1f) });
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(fade);
            ps.Play();
        }
    }
}
