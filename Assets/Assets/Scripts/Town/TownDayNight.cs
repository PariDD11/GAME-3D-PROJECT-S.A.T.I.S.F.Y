using UnityEngine;

namespace HunterGuild
{
    /// <summary>Siklus pagi/siang/sore/malam mengikuti jam in-game (GDD 5.6). Memakai HGSkyConfig bila ada (wizard), kalau tidak siklus sederhana.</summary>
    public class TownDayNight : MonoBehaviour
    {
        public Light sun;
        HGSkyConfig cfg; bool cfgChecked; float lastT = -1f;

        void Update()
        {
            var gs = GameState.I;
            if (gs == null || sun == null) return;
            float t = gs.timeOfDay;

            if (!cfgChecked) { cfg = HGSkyConfig.Load(); cfgChecked = true; }
            if (cfg != null && cfg.IsValid)
            {
                lastT = t; cfg.Apply(t, sun);   // tiap frame: lapisan musim/cuaca (SeasonRuntime) dipasang di atasnya
                return;
            }

            // v35z102n37: siklus lebih ramah mata. Matahari naik cepat jadi terang di pagi hari, dataran siang panjang, senja hangat; malam biru lembut (bukan gelap gulita).
            float sv = Mathf.Sin((t - 6f) / 12f * Mathf.PI);                       // -1..1 (positif = siang)
            float elev = 62f * sv;                                                // sudut matahari: puncak 62 derajat (bayangan masih terbaca)
            sun.transform.rotation = Quaternion.Euler(elev, 30f, 0f);
            float day = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(sv / 0.28f));      // 0 malam .. 1 terang penuh (sudah penuh di sudut ~17 derajat)
            sun.intensity = Mathf.Lerp(0.06f, 1.2f, day);
            float warm = 1f - Mathf.Clamp01(sv / 0.55f);                          // fajar/senja = hangat
            sun.color = Color.Lerp(new Color(1f, 0.97f, 0.9f), new Color(1f, 0.74f, 0.5f), warm * day);
            if (day < 0.01f) sun.color = new Color(0.55f, 0.65f, 1f);
            RenderSettings.ambientIntensity = Mathf.Lerp(0.5f, 1.25f, day);
        }
    }
}
