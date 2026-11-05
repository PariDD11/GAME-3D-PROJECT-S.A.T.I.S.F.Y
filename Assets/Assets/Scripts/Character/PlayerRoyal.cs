using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n6: dandanan pemain. Cakram cahaya lembut di kaki (warna elemen/job), serpihan emas yang mengorbit pelan,
    /// dan lampu pengisi hangat dari depan-atas supaya model tidak "datar". Saat senjata terhunus (Drawn) semuanya sedikit menguat.
    /// </summary>
    public class PlayerRoyal : MonoBehaviour
    {
        PlayerAvatar av; Light fill; ParticleSystem motes; Transform halo; Renderer haloR; Color tint = RoyalFx.Gold; float t;

        void Start()
        {
            av = GetComponent<PlayerAvatar>();
            if (!RoyalFx.On) { enabled = false; return; }
            try { if (GameState.I != null) tint = Color.Lerp(RoyalFx.Gold, MagicFx.ElemCol(JobInfo.Elem(GameState.I.job)), 0.55f); } catch { }

            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = "RoyalHalo";
            q.transform.SetParent(transform, false);
            q.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            q.transform.localScale = Vector3.one * 2.4f;
            haloR = q.GetComponent<MeshRenderer>();
            haloR.sharedMaterial = CombatFx.Mat(CombatFx.Soft(), tint * 0.35f);
            haloR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            halo = q.transform;

            var lg = new GameObject("RoyalFill"); lg.transform.SetParent(transform, false);
            lg.transform.localPosition = new Vector3(0.6f, 2.4f, 1.4f);
            fill = lg.AddComponent<Light>(); fill.type = LightType.Point; fill.color = new Color(1f, 0.92f, 0.78f); fill.range = 7f; fill.intensity = 0.55f; fill.shadows = LightShadows.None;

            var go = new GameObject("RoyalMotes"); go.transform.SetParent(transform, false); go.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            motes = go.AddComponent<ParticleSystem>();
            motes.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = motes.main;
            main.loop = true; main.playOnAwake = false; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.08f);
            main.startColor = new ParticleSystem.MinMaxGradient(tint, Color.Lerp(tint, Color.white, 0.7f));
            main.gravityModifier = -0.05f; main.maxParticles = 60;
            var em = motes.emission; em.rateOverTime = 5f * FxQuality.Count;
            var sp = motes.shape; sp.shapeType = ParticleSystemShapeType.Cone; sp.angle = 0f; sp.radius = 0.7f; sp.rotation = new Vector3(0f, 0f, 0f);
            var vo = motes.velocityOverLifetime; vo.enabled = true; vo.space = ParticleSystemSimulationSpace.World; vo.orbitalY = new ParticleSystem.MinMaxCurve(0.9f);
            var cl = motes.colorOverLifetime; cl.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(tint, 0.5f), new GradientColorKey(tint, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.85f, 0.25f), new GradientAlphaKey(0f, 1f) });
            cl.color = g;
            motes.GetComponent<ParticleSystemRenderer>().sharedMaterial = CombatFx.Mat(CombatFx.Soft(), Color.white);
            motes.Play();
        }

        void Update()
        {
            if (av == null) return;
            t += Time.deltaTime;
            bool drawn = av.Drawn;
            float k = drawn ? 1f : 0.55f;
            if (fill != null) fill.intensity = Mathf.Lerp(fill.intensity, 0.35f + 0.5f * k, Time.deltaTime * 3f);
            if (halo != null) halo.localScale = Vector3.one * (2.2f + 0.25f * Mathf.Sin(t * 2f) + (drawn ? 0.5f : 0f));
            if (motes != null) { var e = motes.emission; e.rateOverTime = (drawn ? 11f : 5f) * FxQuality.Count; }
        }
    }
}
