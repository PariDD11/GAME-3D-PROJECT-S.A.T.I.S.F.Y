using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// Air realistis di semua scene (otomatis, tanpa rebuild): material berwarna "Water" / "SwampWater" / "T_Water" diganti shader air
    /// (Resources/HGWater.shader) dengan gelombang bergerak, Fresnel, pantulan langit, dan kilau matahari. Warna menyesuaikan jenis air
    /// (rawa lebih gelap dan keruh). Langit/matahari/cuaca diperbarui terus. Air sihir (Arcane) tidak diubah.
    /// Matikan lewat RealWater.Enabled = false (disimpan di PlayerPrefs "hg_realwater").
    /// </summary>
    public class RealWater : MonoBehaviour
    {
        public static bool Enabled
        {
            get { return PlayerPrefs.GetInt("hg_realwater", 1) == 1; }
            set { PlayerPrefs.SetInt("hg_realwater", value ? 1 : 0); }
        }

        static RealWater I;
        static Shader shader;
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        Light sun; float tick;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("RealWater");
            DontDestroyOnLoad(go);
            I = go.AddComponent<RealWater>();
            SceneManager.sceneLoaded += (s, m) => { if (I != null) I.Convert(s); };
            I.Convert(SceneManager.GetActiveScene());
        }

        static bool IsWater(Material m)
        {
            if (m == null) return false;
            string n = m.name.ToLowerInvariant();
            if (n.StartsWith("hg_water")) return false;
            if (n.IndexOf("water") < 0) return false;
            if (n.IndexOf("arcane") >= 0) return false;
            return true;
        }

        static bool Ornamental(Transform t)
        {
            for (int i = 0; i < 4 && t != null; i++, t = t.parent)
            { string n = t.name.ToLowerInvariant(); if (n.IndexOf("mancur") >= 0 || n.IndexOf("fountain") >= 0 || n.IndexOf("fontana") >= 0) return true; }
            return false;
        }

        static Material For(Material src, bool fountain)
        {
            string n = src.name.Replace(" (Instance)", "") + (fountain ? "_Fountain" : "");
            Material m;
            if (cache.TryGetValue(n, out m) && m != null) return m;
            if (shader == null) shader = Shader.Find("HunterGuild/Water");
            if (shader == null) return null;
            m = new Material(shader); m.name = "HG_Water_" + n;
            string l = n.ToLowerInvariant();
            if (fountain)
            {
                m.SetColor("_Shallow", new Color(0.24f, 0.7f, 0.82f)); m.SetColor("_Deep", new Color(0.06f, 0.3f, 0.48f));
                m.SetFloat("_Alpha", 0.92f); m.SetFloat("_Bump", 0.3f); m.SetFloat("_Reflect", 0.2f); m.SetFloat("_SkyTint", 1f);   // pantulan langit selalu biru (tidak oranye/krem)
            }
            else if (l.Contains("swamp"))
            {
                m.SetColor("_Shallow", new Color(0.17f, 0.24f, 0.13f)); m.SetColor("_Deep", new Color(0.03f, 0.07f, 0.05f));
                m.SetFloat("_Alpha", 0.93f); m.SetFloat("_Bump", 0.22f); m.SetFloat("_Speed", 0.45f); m.SetFloat("_Gloss", 160f);
            }
            else
            {
                // warna dasar mengikuti material lama sedikit (biru kehijauan untuk danau/sungai)
                Color c = src.HasProperty("_Color") ? src.color : new Color(0.15f, 0.42f, 0.6f);
                m.SetFloat("_SkyTint", 0.8f);
                m.SetColor("_Shallow", Color.Lerp(new Color(0.14f, 0.46f, 0.5f), c, 0.25f));
                m.SetColor("_Deep", Color.Lerp(new Color(0.02f, 0.13f, 0.2f), c * 0.25f, 0.3f));
            }
            cache[n] = m;
            return m;
        }

        void Convert(Scene s)
        {
            if (!Enabled || !s.IsValid() || !s.isLoaded) return;
            int n = 0;
            foreach (var root in s.GetRootGameObjects())
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                    var mats = r.sharedMaterials; bool ch = false;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (!IsWater(mats[i])) continue;
                        var nm = For(mats[i], Ornamental(r.transform)); if (nm == null) continue;
                        mats[i] = nm; ch = true;
                    }
                    if (ch) { r.sharedMaterials = mats; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; n++; }
                }
            if (n > 0) { Push(true); Debug.Log("[RealWater] " + n + " permukaan air diganti di scene " + s.name); }
        }

        void Update()
        {
            tick -= Time.deltaTime;
            if (tick > 0f) return;
            tick = 0.25f;
            Push(false);
        }

        static readonly Vector3[] dirs = { Vector3.up, new Vector3(1f, 0.15f, 0f), new Vector3(-1f, 0.15f, 0f), new Vector3(0f, 0.15f, 1f), new Vector3(0f, 0.15f, -1f) };
        void Push(bool force)
        {
            if (cache.Count == 0 && !force) return;
            if (sun == null)
            {
                sun = RenderSettings.sun;
                if (sun == null)
                    foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                        if (l.type == LightType.Directional && (sun == null || l.intensity > sun.intensity)) sun = l;
            }
            Vector3 sd = new Vector3(0.3f, 0.8f, 0.4f).normalized; Color sc = Color.white; float day = 1f;
            if (sun != null)
            {
                sd = -sun.transform.forward;
                sc = sun.color * Mathf.Clamp(sun.intensity, 0.2f, 2f);
                day = Mathf.Clamp01(sd.y * 2.2f) * Mathf.Clamp01(sun.intensity * 1.4f);
                if (sd.y < 0.02f) day = 0f;
            }
            // warna langit dari ambient probe (berlaku untuk skybox maupun gradient)
            var res = new Color[dirs.Length];
            UnityEngine.Rendering.SphericalHarmonicsL2 sh = RenderSettings.ambientProbe;
            sh.Evaluate(dirs, res);
            Color top = res[0], hor = (res[1] + res[2] + res[3] + res[4]) * 0.25f;
            float k = RenderSettings.ambientIntensity;
            top = Brighten(top * k, 1.35f); hor = Brighten(hor * k, 1.5f);
            // netralkan langit ke biru: cakrawala kekuningan/krem membuat air tampak seperti pasir di sudut pandang rendah
            float lt = Mathf.Clamp(top.grayscale * 1.25f, 0.12f, 1.1f), lh = Mathf.Clamp(hor.grayscale * 1.2f, 0.12f, 1.1f);
            top = Color.Lerp(top, new Color(0.38f, 0.62f, 1f) * lt, 0.5f); hor = Color.Lerp(hor, new Color(0.55f, 0.75f, 1f) * lh, 0.6f);
            float dayLight = Mathf.Max(day, 0.1f + 0.35f * Mathf.Clamp01(top.grayscale));
            Shader.SetGlobalVector("_HGSunDir", new Vector4(sd.x, sd.y, sd.z, 0f));
            Shader.SetGlobalColor("_HGSunCol", sc);
            Shader.SetGlobalColor("_HGSkyTop", new Color(top.r, top.g, top.b, 1f));
            Shader.SetGlobalColor("_HGSkyHor", new Color(hor.r, hor.g, hor.b, Mathf.Clamp01(dayLight)));
            float storm = 0f;
            switch (SeasonRuntime.Weather) { case Wx.Hujan: storm = 0.6f; break; case Wx.Badai: storm = 1f; break; case Wx.Blizzard: storm = 0.8f; break; }
            Shader.SetGlobalFloat("_HGStorm", storm);
        }

        static Color Brighten(Color c, float m) { return new Color(Mathf.Clamp(c.r * m, 0.02f, 1.6f), Mathf.Clamp(c.g * m, 0.03f, 1.6f), Mathf.Clamp(c.b * m, 0.05f, 1.8f), 1f); }
    }
}
