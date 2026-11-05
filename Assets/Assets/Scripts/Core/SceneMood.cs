using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n72 "Mood Per Scene": tiap wilayah punya warna sendiri (rawa kehijauan lembap, gurun hangat, vulkanik jingga-merah, padang tulang pucat
    /// dingin, lembah iblis ungu gelap, Eldrath biru-teal malam, menara dingin). Berupa satu Volume global ringan (prioritas 80, di atas LuxuryLook 70,
    /// di bawah SettingsExtra 90 sehingga slider kecerahan/kontras pemain tetap berlaku). Scene normal (desa, Ashwatch, hutan, interior) tidak diubah.
    /// Nilai sengaja tipis (±10%) dan masuk perlahan (±1,5 detik) supaya tidak terasa seperti filter. Matikan: PlayerPrefs "hg_mood" = 0.
    /// </summary>
    public class SceneMood : MonoBehaviour
    {
        public static bool On { get { return PlayerPrefs.GetInt("hg_mood", 1) == 1; } }

        class Mood
        {
            public Color filter; public float sat, con, exp, vig; public Vector4 shadows, highlights;
            public Mood(Color f, float s, float c, float e, float v, Vector4 sh, Vector4 hi) { filter = f; sat = s; con = c; exp = e; vig = v; shadows = sh; highlights = hi; }
        }

        static readonly Vector4 NeutralHi = new Vector4(1f, 1f, 1f, 0f);

        static Mood Pick(string n)
        {
            if (n.Contains("Swamp")) return new Mood(new Color(0.90f, 1.00f, 0.92f), -8f, 6f, -0.06f, 0.24f, new Vector4(0.86f, 1.00f, 0.92f, -0.04f), NeutralHi);
            if (n.Contains("Desert")) return new Mood(new Color(1.00f, 0.95f, 0.85f), 2f, 6f, 0.04f, 0.18f, new Vector4(1.00f, 0.95f, 0.90f, -0.02f), new Vector4(1.06f, 1.00f, 0.88f, 0.03f));
            if (n.Contains("Volcano")) return new Mood(new Color(1.00f, 0.87f, 0.80f), 4f, 10f, -0.06f, 0.27f, new Vector4(1.00f, 0.88f, 0.90f, -0.04f), new Vector4(1.08f, 0.98f, 0.88f, 0.04f));
            if (n.Contains("Bone")) return new Mood(new Color(0.91f, 0.95f, 1.00f), -20f, 8f, -0.08f, 0.28f, new Vector4(0.90f, 0.95f, 1.05f, -0.05f), NeutralHi);
            if (n.Contains("Demon")) return new Mood(new Color(0.96f, 0.87f, 1.00f), -8f, 12f, -0.10f, 0.30f, new Vector4(1.00f, 0.88f, 1.08f, -0.06f), new Vector4(1.04f, 0.96f, 1.0f, 0.02f));
            if (n.Contains("Tower")) return new Mood(new Color(0.94f, 0.95f, 1.00f), -4f, 6f, -0.05f, 0.22f, new Vector4(0.92f, 0.95f, 1.06f, -0.04f), NeutralHi);
            if (n.Contains("Arena")) return new Mood(new Color(1.00f, 0.97f, 0.90f), 2f, 6f, 0f, 0.20f, new Vector4(1f, 1f, 1f, 0f), new Vector4(1.04f, 1.00f, 0.92f, 0.02f));
            if (n == "DungeonCity") return new Mood(new Color(0.91f, 0.95f, 1.00f), 2f, 8f, -0.04f, 0.22f, new Vector4(0.88f, 0.96f, 1.08f, -0.05f), new Vector4(1.04f, 1.00f, 0.94f, 0.02f));
            if (n.Contains("Frontier")) return new Mood(new Color(0.96f, 0.98f, 1.00f), 2f, 4f, -0.02f, 0.18f, new Vector4(0.94f, 0.97f, 1.04f, -0.03f), NeutralHi);
            return null;
        }

        static SceneMood I;
        Volume vol; VolumeProfile prof; ColorAdjustments ca; Vignette vg; ShadowsMidtonesHighlights smh;
        float targetW; bool built;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("SceneMood");
            DontDestroyOnLoad(go);
            I = go.AddComponent<SceneMood>();
        }

        void OnEnable() { SceneManager.sceneLoaded += Loaded; }
        void OnDisable() { SceneManager.sceneLoaded -= Loaded; }
        void Start() { Apply(SceneManager.GetActiveScene()); }
        void Loaded(Scene s, LoadSceneMode m) { if (m == LoadSceneMode.Single) Apply(s); }

        void Apply(Scene s)
        {
            Mood m = On ? Pick(s.name) : null;
            if (m == null)
            {
                targetW = 0f;
                return;
            }
            Build();
            if (vol == null) { targetW = 0f; return; }
            try
            {
                ca.postExposure.value = m.exp; ca.contrast.value = m.con; ca.saturation.value = m.sat; ca.colorFilter.value = m.filter;
                vg.intensity.value = m.vig; vg.smoothness.value = 0.8f;
                smh.shadows.value = m.shadows; smh.midtones.value = new Vector4(1f, 1f, 1f, 0f); smh.highlights.value = m.highlights;
                vol.weight = 0f;                   // masuk perlahan
                targetW = 0.7f;
            }
            catch { targetW = 0f; }
        }

        void Build()
        {
            if (built) return;
            built = true;
            try
            {
                var g = new GameObject("MoodVolume"); g.transform.SetParent(transform, false);
                vol = g.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 80f; vol.weight = 0f;
                prof = ScriptableObject.CreateInstance<VolumeProfile>();
                ca = prof.Add<ColorAdjustments>(true); vg = prof.Add<Vignette>(true); smh = prof.Add<ShadowsMidtonesHighlights>(true);
                foreach (var p in new VolumeParameter[] { ca.postExposure, ca.contrast, ca.saturation, ca.colorFilter, vg.intensity, vg.smoothness, smh.shadows, smh.midtones, smh.highlights }) p.overrideState = true;
                vol.sharedProfile = prof;
            }
            catch { vol = null; }
        }

        void Update()
        {
            if (vol == null) return;
            float w = Mathf.MoveTowards(vol.weight, On ? targetW : 0f, Time.unscaledDeltaTime * 0.5f);
            if (!Mathf.Approximately(w, vol.weight)) vol.weight = w;
        }
    }
}
