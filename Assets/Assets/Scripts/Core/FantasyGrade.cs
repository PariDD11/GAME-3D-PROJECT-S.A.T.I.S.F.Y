using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n37 "Anime Fantasy": langit gradasi cerah (biru jernih -> cakrawala pucat hangat, fajar/senja merah muda-jingga, malam biru tua),
    /// kabut mengikuti warna cakrawala (bukan abu-abu), dan lantai cahaya sekitar supaya sisi bayangan tetap berwarna (lilac/biru), tidak lumpur gelap.
    /// Berjalan otomatis di semua scene luar ruangan bernuansa normal (Desa, Ashwatch, hutan). Scene bertema khusus (rawa, gurun, vulkanik, iblis, tulang, menara)
    /// tidak diganti langitnya, hanya diberi lantai cahaya ringan.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public class FantasyGrade : MonoBehaviour
    {
        public static bool On { get { return PlayerPrefs.GetInt("hg_fantasy", 1) == 1; } set { PlayerPrefs.SetInt("hg_fantasy", value ? 1 : 0); } }

        static FantasyGrade I;
        Material sky; Light sun; bool skyScene; bool special; Material prevSky;
        float sunY = 0.8f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("FantasyGrade"); DontDestroyOnLoad(go);
            I = go.AddComponent<FantasyGrade>();
            SceneManager.sceneLoaded += (s, m) => { if (I != null) I.OnScene(); };
            I.OnScene();
        }

        void OnScene()
        {
            sun = null;
            string n = SceneManager.GetActiveScene().name;
            if (n.Contains("Frontier") || n.Contains("HuntArea_G")) { var c0 = Camera.main; if (c0 != null) c0.clearFlags = CameraClearFlags.Skybox; }
            special = n.Contains("Swamp") || n.Contains("Desert") || n.Contains("Volcano") || n.Contains("Bone") || n.Contains("Demon") || n.Contains("Tower") || n.Contains("Arena") || n == "DungeonCity";
            bool interior = InteriorInfo.IsInterior(n) || n == GuildConfig.InteriorScene;
            skyScene = !special && !interior;
            if (skyScene) EnsureSky();
        }

        void EnsureSky()
        {
            if (sky != null) { if (RenderSettings.skybox != sky) { prevSky = RenderSettings.skybox; RenderSettings.skybox = sky; } return; }
            var sh = Resources.Load<Shader>("HGSkyGradient");
            if (sh == null) return;
            sky = new Material(sh) { name = "HG_SkyGradient" };
            prevSky = RenderSettings.skybox;
            RenderSettings.skybox = sky;
        }

        Light FindSun()
        {
            var dn = FindFirstObjectByType<TownDayNight>();
            if (dn != null && dn.sun != null) return dn.sun;
            if (RenderSettings.sun != null) return RenderSettings.sun;
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) return l;
            return null;
        }

        static Color Mix(Color a, Color b, float t) { return Color.Lerp(a, b, Mathf.Clamp01(t)); }

        void LateUpdate()
        {
            if (!On) { if (sky != null && RenderSettings.skybox == sky) RenderSettings.skybox = prevSky; return; }
            if (sun == null) { sun = FindSun(); if (sun == null) return; }
            Vector3 sd = -sun.transform.forward; sunY = sd.y;
            float day = Mathf.Clamp01((sd.y + 0.02f) / 0.30f);            // 0 malam .. 1 siang
            float dusk = Mathf.Clamp01(1f - Mathf.Abs(sd.y) / 0.22f);     // 1 tepat di cakrawala (fajar/senja)

            Color topD = new Color(0.27f, 0.56f, 0.98f), midD = new Color(0.52f, 0.77f, 1f), horD = new Color(0.84f, 0.94f, 1f);
            Color topN = new Color(0.05f, 0.08f, 0.22f), midN = new Color(0.09f, 0.13f, 0.32f), horN = new Color(0.16f, 0.20f, 0.42f);
            Color topW = new Color(0.40f, 0.50f, 0.88f), midW = new Color(0.78f, 0.62f, 0.82f), horW = new Color(1f, 0.72f, 0.52f);   // fajar/senja
            Color top = Mix(topN, topD, day), mid = Mix(midN, midD, day), hor = Mix(horN, horD, day);
            top = Mix(top, topW, dusk * 0.7f); mid = Mix(mid, midW, dusk * 0.8f); hor = Mix(hor, horW, dusk * 0.9f);

            if (skyScene && sky != null)
            {
                if (RenderSettings.skybox != sky) RenderSettings.skybox = sky;
                sky.SetColor("_Top", top); sky.SetColor("_Mid", mid); sky.SetColor("_Hor", hor);
                sky.SetColor("_Bot", Mix(hor, new Color(0.45f, 0.55f, 0.45f), 0.35f));
                sky.SetVector("_SunDirX", new Vector4(sd.x, sd.y, sd.z, 0f));
                Color sc = Color.Lerp(new Color(1f, 0.85f, 0.6f), new Color(1f, 0.7f, 0.45f), dusk);
                sc.a = Mathf.Clamp01(day * 0.9f + dusk * 0.4f);
                sky.SetColor("_Glow", sc);
                // kabut = warna cakrawala, supaya gunung/hutan jauh menyatu dengan langit (perspektif udara anime)
                if (RenderSettings.fog)
                {
                    RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, Color.Lerp(hor, Color.white, 0.12f), 0.55f);
                    if (RenderSettings.fogDensity > 0.0022f) RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, 0.0022f, 0.5f);
                }
            }

            // lantai cahaya sekitar: sisi bayangan tetap berwarna (lilac-biru siang, ungu-biru malam), tidak jatuh ke hitam
            if (RenderSettings.ambientMode == UnityEngine.Rendering.AmbientMode.Flat)
            {
                Color floor = Color.Lerp(new Color(0.13f, 0.16f, 0.30f), new Color(0.50f, 0.56f, 0.72f), day);
                Color a = RenderSettings.ambientLight;
                RenderSettings.ambientLight = new Color(Mathf.Max(a.r, floor.r), Mathf.Max(a.g, floor.g), Mathf.Max(a.b, floor.b), 1f);
            }
        }
    }
}
