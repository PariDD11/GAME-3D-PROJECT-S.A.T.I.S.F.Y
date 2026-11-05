using UnityEngine;

namespace HunterGuild
{
    /// <summary>Aura elemen pada pedang sihir: percikan berwarna terus menerus di sekitar bilah dan cahaya lembut.</summary>
    public class BladeGlow : MonoBehaviour
    {
        public Color color = Color.white;
        ParticleSystem ps; Light lt;

        void Start()
        {
            var go = new GameObject("BladeGlowFx");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, RoyalFx.On ? 0.2f : 0.14f);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.6f));
            main.gravityModifier = -0.25f;
            var em = ps.emission; em.rateOverTime = RoyalFx.On ? 46f : 28f;
            var sp = ps.shape; sp.shapeType = ParticleSystemShapeType.Box; sp.scale = new Vector3(0.08f, 1.0f, 0.08f);
            var cl = ps.colorOverLifetime; cl.enabled = true;
            var gr = new Gradient();
            gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 0.5f), new GradientColorKey(color, 1f) },
                       new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            cl.color = gr;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = CombatFx.Mat(CombatFx.Soft(), Color.white);
            ps.Play();

            var lg = new GameObject("BladeLight"); lg.transform.SetParent(transform, false); lg.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            lt = lg.AddComponent<Light>(); lt.type = LightType.Point; lt.color = color; lt.range = RoyalFx.On ? 5.5f : 4f; lt.intensity = RoyalFx.On ? 1.1f : 0.8f; lt.shadows = LightShadows.None;
        }

        void Update()
        {
            if (lt != null) lt.intensity = (RoyalFx.On ? 1.0f : 0.7f) + Mathf.Sin(Time.time * 6f) * 0.2f;
            if (ps != null) { var e = ps.emission; e.enabled = gameObject.activeInHierarchy; }
        }
    }
}
