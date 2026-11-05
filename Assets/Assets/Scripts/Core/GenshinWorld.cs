using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HunterGuild
{
    /// <summary>
    /// Pass "cerah ala Genshin" untuk dunia (semua scene): material URP/Lit dibuat lebih jenuh & terang, spekular dikurangi (tampilan cat/cel),
    /// bayangan matahari dilembutkan, kabut diberi nuansa biru muda. Berjalan otomatis; hanya aktif bila AnimeLook.Bright.
    /// Material milik shader HunterGuild/*, partikel, dan UI tidak disentuh. Tiap material hanya diubah sekali (nilai asli disimpan untuk dikembalikan).
    /// </summary>
    public class GenshinWorld : MonoBehaviour
    {
        static GenshinWorld I;
        readonly HashSet<int> seenRend = new HashSet<int>();
        readonly Dictionary<Material, Color> orig = new Dictionary<Material, Color>();
        readonly Dictionary<Material, float> origSm = new Dictionary<Material, float>();
        bool applied; float tick = 2f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("GenshinWorld"); DontDestroyOnLoad(go); I = go.AddComponent<GenshinWorld>();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (sc, m) => { if (I != null) { I.seenRend.Clear(); I.pending = null; I.tick = 0.4f; } };
        }

        Renderer[] pending; int pi; bool foundNew;

        void Update()
        {
            if (pending != null)
            {
                // dikerjakan bertahap (renderer baru dibatasi ±160 per frame) supaya tidak ada patah-patah tiap beberapa detik
                int n = 0;
                while (pi < pending.Length && n < 160)
                {
                    var r = pending[pi++];
                    if (r == null || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                    if (!seenRend.Add(r.GetInstanceID())) continue;
                    foundNew = true; n++;
                    var ms = r.sharedMaterials;
                    for (int i = 0; i < ms.Length; i++) { Solid(ms[i]); if (applied) Tune(ms[i]); }
                }
                if (pi >= pending.Length) { pending = null; tick = foundNew ? 1.5f : 7f; }   // scene sudah stabil → cek jarang saja
                return;
            }
            tick -= Time.unscaledDeltaTime;
            if (tick > 0f) return;
            bool br = AnimeLook.Bright;
            if (br != applied) { if (!br) Restore(); applied = br; seenRend.Clear(); }
            if (br) Lights();
            pending = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            pi = 0; foundNew = false;
        }

        void Lights()
        {
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type != LightType.Directional) continue;
                l.shadowStrength = Mathf.Min(l.shadowStrength, 0.72f);
                if (l.shadows == LightShadows.Hard) l.shadows = LightShadows.Soft;
            }
            if (RenderSettings.fog) RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, new Color(0.62f, 0.76f, 0.92f), 0.08f);
        }

        /// <summary>Material bangunan dibuat dua sisi (Cull Off) agar atap/dinding tidak tembus pandang bila ada sisi mesh yang terbalik.</summary>
        static void Solid(Material m)
        {
            if (m == null || m.shader == null || !m.HasProperty("_Cull")) return;
            string n = m.shader.name;
            if (n != "Universal Render Pipeline/Lit" && n != "Universal Render Pipeline/Simple Lit") return;
            if (m.GetFloat("_Cull") != 0f) m.SetFloat("_Cull", 0f);
        }

        void Tune(Material m)
        {
            if (m == null || orig.ContainsKey(m) || m.shader == null) return;
            string n = m.shader.name;
            if (n != "Universal Render Pipeline/Lit" && n != "Standard" && n != "Universal Render Pipeline/Simple Lit") return;
            if (!m.HasProperty("_BaseColor") && !m.HasProperty("_Color")) return;
            string cp = m.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
            Color c = m.GetColor(cp);
            if (c.a < 0.98f) return;                      // transparan/kaca/efek: biarkan
            orig[m] = c;
            float h, sa, v; Color.RGBToHSV(c, out h, out sa, out v);
            sa = Mathf.Clamp01(sa * 1.18f + 0.03f);
            v = Mathf.Clamp01(v * 1.12f + 0.03f);
            Color o = Color.HSVToRGB(h, sa, v); o.a = c.a;
            m.SetColor(cp, o);
            string sp = m.HasProperty("_Smoothness") ? "_Smoothness" : (m.HasProperty("_Glossiness") ? "_Glossiness" : null);
            if (sp != null) { origSm[m] = m.GetFloat(sp); m.SetFloat(sp, Mathf.Min(m.GetFloat(sp), 0.18f)); }
        }

        void Restore()
        {
            foreach (var kv in orig)
            {
                var m = kv.Key; if (m == null) continue;
                m.SetColor(m.HasProperty("_BaseColor") ? "_BaseColor" : "_Color", kv.Value);
            }
            foreach (var kv in origSm)
            {
                var m = kv.Key; if (m == null) continue;
                m.SetFloat(m.HasProperty("_Smoothness") ? "_Smoothness" : "_Glossiness", kv.Value);
            }
            orig.Clear(); origSm.Clear();
        }
    }
}
