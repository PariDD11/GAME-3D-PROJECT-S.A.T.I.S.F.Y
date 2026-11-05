using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HunterGuild
{
    /// <summary>
    /// Pengaturan langit &amp; waktu: 4 fase (pagi, siang, sore, malam), masing-masing dengan skybox + warna matahari/ambient/kabut.
    /// Dibuat oleh wizard (Hunter Guild > Setup Langit &amp; Terrain) di Assets/Resources/HG_World/SkyConfig.asset.
    /// </summary>
    public class HGSkyConfig : ScriptableObject
    {
        public const string ResPath = "HG_World/SkyConfig";

        [Serializable]
        public class Phase
        {
            public string name = "Siang";
            public float startHour = 9f;
            public Material skybox;
            public Color sunColor = Color.white;
            public float sunIntensity = 1.1f;
            public Color ambientColor = new Color(0.75f, 0.8f, 0.9f);
            public float ambientIntensity = 1f;
            public Color fogColor = new Color(0.62f, 0.72f, 0.78f);
            public float fogDensity = 0.0012f;
        }

        public Phase[] phases = Defaults();
        public float blendHours = 1.5f;          // lama transisi menuju fase berikutnya
        public float sunAzimuth = 30f;
        public float skyDip = 0.75f;             // seberapa gelap skybox saat bertukar (0 = langsung ganti)
        public float sunriseHour = 6f, sunsetHour = 18f;

        public static Phase[] Defaults()
        {
            return new[]
            {
                new Phase { name = "Pagi", startHour = 5.5f, sunColor = new Color(1f, 0.82f, 0.62f), sunIntensity = 0.8f, ambientColor = new Color(0.62f, 0.62f, 0.72f), ambientIntensity = 0.8f, fogColor = new Color(0.78f, 0.74f, 0.72f), fogDensity = 0.0015f },
                new Phase { name = "Siang", startHour = 9f, sunColor = new Color(1f, 0.96f, 0.88f), sunIntensity = 1.15f, ambientColor = new Color(0.75f, 0.8f, 0.9f), ambientIntensity = 1f, fogColor = new Color(0.62f, 0.72f, 0.78f), fogDensity = 0.0012f },
                new Phase { name = "Sore", startHour = 16.5f, sunColor = new Color(1f, 0.6f, 0.32f), sunIntensity = 0.95f, ambientColor = new Color(0.75f, 0.55f, 0.5f), ambientIntensity = 0.8f, fogColor = new Color(0.85f, 0.6f, 0.45f), fogDensity = 0.0016f },
                new Phase { name = "Malam", startHour = 19.5f, sunColor = new Color(0.45f, 0.55f, 0.9f), sunIntensity = 0.25f, ambientColor = new Color(0.12f, 0.15f, 0.28f), ambientIntensity = 0.45f, fogColor = new Color(0.06f, 0.08f, 0.16f), fogDensity = 0.002f },
            };
        }

        public static HGSkyConfig Load() { return Resources.Load<HGSkyConfig>(ResPath); }
        public bool IsValid { get { return phases != null && phases.Length >= 2; } }

        // salinan material per fase saat runtime agar asset aslinya tidak berubah
        Material[] runtimeSky;
        int shownSky = -2;

        int[] Order()
        {
            int n = phases.Length; var idx = new int[n];
            for (int i = 0; i < n; i++) idx[i] = i;
            Array.Sort(idx, (a, b) => phases[a].startHour.CompareTo(phases[b].startHour));
            return idx;
        }

        /// <summary>Menerapkan pencahayaan, ambient, kabut, dan skybox untuk jam t (0-24). preview = di Editor tanpa Play.</summary>
        public void Apply(float t, Light sun, bool preview = false)
        {
            if (!IsValid) return;
            t = ((t % 24f) + 24f) % 24f;
            var ord = Order(); int n = ord.Length;
            int cur = ord[n - 1];                                        // sebelum fase pertama = fase terakhir (malam)
            int curPos = n - 1;
            for (int i = 0; i < n; i++) if (t >= phases[ord[i]].startHour) { cur = ord[i]; curPos = i; }
            int next = ord[(curPos + 1) % n];
            float nextStart = phases[next].startHour;
            if (nextStart <= phases[cur].startHour) nextStart += 24f;
            float tt = t < phases[cur].startHour ? t + 24f : t;
            float bh = Mathf.Max(0.01f, blendHours);
            float s = Mathf.Clamp01((tt - (nextStart - bh)) / bh);       // 0 = sepenuhnya fase sekarang, 1 = fase berikut
            s = s * s * (3f - 2f * s);
            Phase a = phases[cur], b = phases[next];

            Color sunC = Color.Lerp(a.sunColor, b.sunColor, s);
            float sunI = Mathf.Lerp(a.sunIntensity, b.sunIntensity, s);
            Color amb = Color.Lerp(a.ambientColor, b.ambientColor, s);
            float ambI = Mathf.Lerp(a.ambientIntensity, b.ambientIntensity, s);

            if (sun != null)
            {
                float elev = SunElevation(t);
                sun.transform.rotation = Quaternion.Euler(Mathf.Max(elev, 4f), sunAzimuth, 0f);
                sun.color = sunC; sun.intensity = sunI;
            }
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = amb * ambI;
            RenderSettings.fog = true;
            RenderSettings.fogColor = Color.Lerp(a.fogColor, b.fogColor, s);
            RenderSettings.fogDensity = Mathf.Lerp(a.fogDensity, b.fogDensity, s);

            // skybox: tukar di tengah transisi; saat runtime redupkan sebentar agar tidak "lompat"
            int want = s < 0.5f ? cur : next;
            Material src = phases[want].skybox;
            if (src == null) src = phases[cur].skybox != null ? phases[cur].skybox : phases[next].skybox;
            if (src == null) return;
            Material use = src;
            if (!preview)
            {
                if (runtimeSky == null || runtimeSky.Length != phases.Length) runtimeSky = new Material[phases.Length];
                if (runtimeSky[want] == null || runtimeSky[want].shader != src.shader) runtimeSky[want] = new Material(src);
                use = runtimeSky[want];
                if (skyDip > 0f && use.HasProperty("_Exposure"))
                {
                    float baseExp = src.GetFloat("_Exposure");
                    float dip = 1f - skyDip * Mathf.Sin(s * Mathf.PI);
                    use.SetFloat("_Exposure", baseExp * Mathf.Clamp(dip, 0.05f, 1f));
                }
            }
            if (RenderSettings.skybox != use) RenderSettings.skybox = use;
            if (!preview && want != shownSky) { shownSky = want; DynamicGI.UpdateEnvironment(); }
        }

        public float SunElevation(float t)
        {
            if (t >= sunriseHour && t <= sunsetHour) return Mathf.Sin((t - sunriseHour) / (sunsetHour - sunriseHour) * Mathf.PI) * 68f;
            float nt = t > sunsetHour ? t - sunsetHour : t + 24f - sunsetHour;           // 0..(24 - day length)
            float nightLen = 24f - (sunsetHour - sunriseHour);
            return Mathf.Sin(Mathf.Clamp01(nt / nightLen) * Mathf.PI) * 50f;                // bulan melintas
        }
    }
}
