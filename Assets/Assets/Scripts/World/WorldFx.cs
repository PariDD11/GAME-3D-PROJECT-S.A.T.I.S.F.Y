using UnityEngine;
using UnityEngine.Rendering;

namespace HunterGuild
{
    /// <summary>Utilitas efek partikel: tekstur prosedural (daun, titik cahaya, asap) dan material sprite.</summary>
    public static class FxUtil
    {
        static Light sunCache;

        public static Shader SpriteShader()
        {
            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) sh = Shader.Find("Unlit/Transparent");
            return sh;
        }

        public static Material MakeMaterial(Texture2D tex)
        {
            var m = new Material(SpriteShader());
            if (tex != null) m.mainTexture = tex;
            m.color = Color.white;
            m.renderQueue = 3000;
            return m;
        }

        /// <summary>0 = gelap (malam), 1 = terang (siang), dari cahaya matahari dan ambient.</summary>
        public static float DayBrightness()
        {
            if (sunCache == null)
            {
                sunCache = RenderSettings.sun;
                if (sunCache == null)
                    foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                        if (l.type == LightType.Directional) { sunCache = l; break; }
            }
            float sun = sunCache != null ? sunCache.intensity : 1f;
            float amb = RenderSettings.ambientLight.grayscale;
            return Mathf.Clamp01(amb * 1.2f + sun * 0.55f);
        }

        public static Texture2D LeafTexture()
        {
            const int n = 64;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, true);
            var px = new Color[n * n];
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    float x = (i + 0.5f) / n * 2f - 1f, y = (j + 0.5f) / n * 2f - 1f;
                    float w = 0.66f * Mathf.Pow(Mathf.Clamp01(1f - y * y), 0.72f) * (y > 0f ? 0.92f : 1f);
                    float a = Mathf.Clamp01((w - Mathf.Abs(x)) / 0.07f);
                    float shade = 0.9f - Mathf.Abs(x) * 0.25f;
                    if (Mathf.Abs(x) < 0.045f) shade = 0.62f;                                            // tulang daun
                    else if (Mathf.Abs(Mathf.Sin((y * 6f + Mathf.Abs(x) * 5f) * 1.2f)) > 0.92f) shade *= 0.8f;       // tulang samping
                    if (y < -0.86f && Mathf.Abs(x) < 0.05f) { a = 1f; shade = 0.5f; }                  // tangkai
                    px[j * n + i] = new Color(shade, shade, shade, a);
                }
            t.SetPixels(px);
            t.wrapMode = TextureWrapMode.Clamp;
            t.Apply();
            return t;
        }

        public static Texture2D SoftDot(float hardness)
        {
            const int n = 32;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, true);
            var px = new Color[n * n];
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    float x = (i + 0.5f) / n * 2f - 1f, y = (j + 0.5f) / n * 2f - 1f;
                    float r = Mathf.Sqrt(x * x + y * y);
                    float a = Mathf.Pow(Mathf.Clamp01(1f - r), hardness);
                    px[j * n + i] = new Color(1f, 1f, 1f, a);
                }
            t.SetPixels(px);
            t.wrapMode = TextureWrapMode.Clamp;
            t.Apply();
            return t;
        }

        public static ParticleSystem NewSystem(string name, Transform parent)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }
    }





}
