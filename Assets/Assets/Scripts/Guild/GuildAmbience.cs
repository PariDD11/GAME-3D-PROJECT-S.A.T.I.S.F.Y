using UnityEngine;
using UnityEngine.Rendering;

namespace HunterGuild
{
    /// <summary>
    /// Pencahayaan interior mengikuti jam in-game (cahaya jendela siang/malam), dan mengganti objek
    /// resepsionis buka/tutup (tutup pukul 00.00-05.00, GDD 4.4).
    /// </summary>
    public class GuildAmbience : MonoBehaviour
    {
        public Light[] windowLights;
        public GameObject[] deskOpenObjects;     // aktif saat resepsionis buka (NPC resepsionis)
        public GameObject[] deskClosedObjects;   // aktif saat tutup (papan TUTUP)
        public Color dayAmbient = new Color(0.50f, 0.45f, 0.40f);
        public Color nightAmbient = new Color(0.16f, 0.15f, 0.22f);

        int deskState = -1;

        void Start()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            Apply();
        }

        void Update() { Apply(); }

        void Apply()
        {
            var gs = GameState.I;
            float t = gs != null ? gs.timeOfDay : 12f;
            float h = Mathf.Clamp01(Mathf.Sin((t - 6f) / 12f * Mathf.PI));   // 0 malam, 1 tengah hari
            RenderSettings.ambientLight = Color.Lerp(nightAmbient, dayAmbient, h);

            Color moon = new Color(0.55f, 0.65f, 1f);
            Color sun = new Color(1f, 0.92f, 0.75f);
            if (windowLights != null)
                foreach (var l in windowLights)
                {
                    if (l == null) continue;
                    l.intensity = Mathf.Lerp(0.25f, 1.6f, h);
                    l.color = Color.Lerp(moon, sun, h);
                }

            int st = GuildServices.DeskOpen ? 1 : 0;
            if (st == deskState) return;
            deskState = st;
            SetAll(deskOpenObjects, st == 1);
            SetAll(deskClosedObjects, st == 0);
        }

        static void SetAll(GameObject[] arr, bool on)
        {
            if (arr == null) return;
            foreach (var g in arr) if (g != null) g.SetActive(on);
        }
    }
}
