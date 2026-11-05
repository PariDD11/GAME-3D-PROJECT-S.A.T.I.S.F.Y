using UnityEngine;

namespace HunterGuild
{
    /// <summary>Kedip halus untuk obor, perapian, dan brazier.</summary>
    [RequireComponent(typeof(Light))]
    public class FlickerLight : MonoBehaviour
    {
        public float amount = 0.18f;
        public float speed = 6f;

        Light lamp;
        float baseIntensity, seed;

        void Awake()
        {
            lamp = GetComponent<Light>();
            baseIntensity = lamp.intensity;
            seed = Random.value * 100f;
        }

        void Update()
        {
            float n = Mathf.PerlinNoise(Time.time * speed, seed) - 0.5f;
            lamp.intensity = baseIntensity * (1f + n * 2f * amount);
        }
    }
}
