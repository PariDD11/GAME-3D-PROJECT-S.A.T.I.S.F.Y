using UnityEngine;
using UnityEngine.Rendering;

namespace HunterGuild
{
    /// <summary>Daun berguguran yang mengikuti kamera: jatuh pelan, berputar, tertiup angin, hilang saat menyentuh tanah.</summary>
    public class FallingLeaves : MonoBehaviour
    {
        public Material material;                // opsional; kosong = dibuat otomatis
        public float area = 38f, height = 11f, rate = 26f;
        public int maxParticles = 520;
        public Vector2 wind = new Vector2(0.9f, 0.25f);
        public Color[] palette =
        {
            new Color(0.88f, 0.52f, 0.14f), new Color(0.8f, 0.3f, 0.1f), new Color(0.93f, 0.75f, 0.2f),
            new Color(0.55f, 0.6f, 0.2f), new Color(0.38f, 0.5f, 0.16f), new Color(0.62f, 0.38f, 0.16f)
        };

        ParticleSystem ps;
        Material mat;
        Transform cam;
        float nextLight;

        void Start()
        {
            ps = FxUtil.NewSystem("LeafParticles", transform);
            var r = ps.GetComponent<ParticleSystemRenderer>();
            mat = material != null ? new Material(material) : FxUtil.MakeMaterial(FxUtil.LeafTexture());
            if (material != null && mat.mainTexture == null) mat.mainTexture = FxUtil.LeafTexture();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.prewarm = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;
            main.gravityModifier = 0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(10f, 15f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.34f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            var grad = new Gradient();
            var keys = new GradientColorKey[palette.Length];
            for (int i = 0; i < palette.Length; i++) keys[i] = new GradientColorKey(palette[i], i / (float)Mathf.Max(1, palette.Length - 1));
            grad.SetKeys(keys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            var sc = new ParticleSystem.MinMaxGradient(grad);
            sc.mode = ParticleSystemGradientMode.RandomColor;
            main.startColor = sc;

            var em = ps.emission;
            em.enabled = true;
            em.rateOverTime = rate;

            var sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(area, 0.5f, area);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(wind.x * 0.3f, wind.x * 1.3f);
            vel.y = new ParticleSystem.MinMaxCurve(-1.5f, -0.7f);
            vel.z = new ParticleSystem.MinMaxCurve(wind.y - 0.4f, wind.y + 0.4f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.separateAxes = false;
            noise.strength = new ParticleSystem.MinMaxCurve(1.2f);
            noise.frequency = 0.35f;
            noise.scrollSpeed = 0.4f;
            noise.damping = true;
            noise.quality = ParticleSystemNoiseQuality.Medium;

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.separateAxes = false;
            rot.z = new ParticleSystem.MinMaxCurve(-2.4f, 2.4f);

            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f), new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) });
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(fade);

            var coll = ps.collision;
            coll.enabled = true;
            coll.type = ParticleSystemCollisionType.World;
            coll.mode = ParticleSystemCollisionMode.Collision3D;
            coll.quality = ParticleSystemCollisionQuality.Low;
            coll.lifetimeLoss = new ParticleSystem.MinMaxCurve(1f);
            coll.radiusScale = 0.2f;
            coll.maxCollisionShapes = 128;

            FindCam();
            Follow();
            ps.Play();
            // isi awal: simulasikan sebentar supaya langsung ada daun di udara
            ps.Simulate(8f, true, true);
            ps.Play();
        }

        void FindCam() { var c = Camera.main; cam = c != null ? c.transform : null; }

        void Follow()
        {
            if (cam == null) return;
            transform.position = cam.position + new Vector3(0f, height, 0f);
        }

        void LateUpdate()
        {
            if (cam == null) { FindCam(); return; }
            Follow();
            if (Time.time > nextLight && mat != null)
            {
                nextLight = Time.time + 0.5f;
                float b = FxUtil.DayBrightness();
                mat.color = Color.Lerp(new Color(0.2f, 0.22f, 0.32f, 1f), Color.white, b);
            }
        }
    }
}
