using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Cel-shading + garis tepi bergaya anime untuk pemain, NPC, dan monster. Material URP Lit/Standard diganti HunterGuild/Toon dan tiap mesh
    /// diberi salinan "inverted hull" (HunterGuild/ToonOutline). Model dengan shader sendiri (MToon, lilToon, dll) tidak disentuh.
    /// Aktif/nonaktif lewat Pengaturan (PlayerPrefs hg_anime). Hanya tampilan; tidak mengubah gameplay.
    /// </summary>
    public class AnimeToon : MonoBehaviour
    {
        static readonly int LightDir = Shader.PropertyToID("_HGLightDir"), LightCol = Shader.PropertyToID("_HGLightCol"), Ambient = Shader.PropertyToID("_HGAmbient");
        Shader toonSh, outSh; Material outMat, outMatBlend;
        readonly Dictionary<Material, Material> cache = new Dictionary<Material, Material>();
        Light sun; float scanT, sunT, ambT; Color amb = new Color(0.45f, 0.5f, 0.6f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<AnimeToon>() != null) return;
            var go = new GameObject("AnimeToon"); DontDestroyOnLoad(go); go.AddComponent<AnimeToon>();
        }

        void Start()
        {
            toonSh = Shader.Find("HunterGuild/Toon"); outSh = Shader.Find("HunterGuild/ToonOutline");
            if (toonSh != null && !toonSh.isSupported) toonSh = null;     // shader gagal dikompilasi -> jangan dipakai (hindari objek pink)
            if (outSh != null && !outSh.isSupported) outSh = null;
            Shader.SetGlobalVector(LightDir, new Vector4(-0.4f, -0.8f, -0.4f, 0f));
            Shader.SetGlobalColor(LightCol, Color.white); Shader.SetGlobalColor(Ambient, amb);
            if (outSh != null)
            {
                outMat = new Material(outSh) { name = "ToonOutline" };
                outMatBlend = new Material(outSh) { name = "ToonOutlineHull" }; outMatBlend.SetFloat("_Blend", 0.55f);
            }
        }

        void Update()
        {
            if (toonSh == null || outSh == null) return;
            UpdateLight();
            scanT -= Time.unscaledDeltaTime;
            if (scanT > 0f) return;
            scanT = 1f;
            bool on = AnimeLook.On;
            foreach (var m in new List<Monster>(Monster.All)) if (m != null) Sync(m.gameObject, on, 0.016f);
            if (PlayerAvatar.I != null) Sync(PlayerAvatar.I.gameObject, on, 0.010f);
            foreach (var n in FindObjectsByType<NpcActor>(FindObjectsSortMode.None)) Sync(n.gameObject, on, 0.010f);
            if (++pruneN >= 20) { pruneN = 0; Prune(); }
        }

        int pruneN;
        /// <summary>Buang bahan toon milik bahan asli yang sudah dihancurkan (mis. monster MonsterKit yang sudah hilang; tiap individu punya bahan palet sendiri).</summary>
        void Prune()
        {
            List<Material> dead = null;
            foreach (var kv in cache) if (kv.Key == null) { if (dead == null) dead = new List<Material>(); dead.Add(kv.Key); }
            if (dead == null) return;
            for (int i = 0; i < dead.Count; i++)
            {
                Material tm;
                if (cache.TryGetValue(dead[i], out tm) && tm != null) Destroy(tm);
                cache.Remove(dead[i]);
            }
        }

        void UpdateLight()
        {
            sunT -= Time.unscaledDeltaTime;
            if (sun == null && sunT <= 0f)
            {
                sunT = 3f; sun = RenderSettings.sun;
                if (sun == null) foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) { sun = l; break; }
            }
            ambT -= Time.unscaledDeltaTime;
            if (ambT <= 0f)
            {
                ambT = 2f;
                try { var cols = new Color[1]; RenderSettings.ambientProbe.Evaluate(new[] { Vector3.up }, cols); amb = cols[0]; amb.a = 1f; } catch { }
                Shader.SetGlobalColor(Ambient, amb);
            }
            if (sun != null)
            {
                Vector3 f = sun.transform.forward;
                Shader.SetGlobalVector(LightDir, new Vector4(f.x, f.y, f.z, 0f));
                Color c = sun.color * Mathf.Clamp(sun.intensity, 0.3f, 1.35f); c.a = 1f;
                Shader.SetGlobalColor(LightCol, c);
            }
        }

        static bool Swappable(Material m)
        {
            if (m == null || m.shader == null) return false;
            string n = m.shader.name;
            return n == "Universal Render Pipeline/Lit" || n == "Universal Render Pipeline/Simple Lit" || n == "Standard" || n == "Universal Render Pipeline/Baked Lit";
        }

        Material ToonOf(Material src)
        {
            Material t;
            if (cache.TryGetValue(src, out t) && t != null) return t;
            t = new Material(toonSh) { name = src.name + "_Toon" };
            Color c = src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor") : src.HasProperty("_Color") ? src.GetColor("_Color") : Color.white;
            Texture tex = src.HasProperty("_BaseMap") ? src.GetTexture("_BaseMap") : src.HasProperty("_MainTex") ? src.GetTexture("_MainTex") : null;
            t.SetColor("_BaseColor", c); if (tex != null) t.SetTexture("_BaseMap", tex);
            cache[src] = t; return t;
        }

        void Sync(GameObject root, bool on, float width)
        {
            var st = root.GetComponent<AnimeStyled>();
            if (on && st == null) Apply(root, width);
            else if (!on && st != null) st.Revert(false);
            else if (on && st != null && st.count != Count(root)) { st.Revert(true); Apply(root, width); }   // model diganti / ditambah bagian baru
        }

        /// <summary>Jumlah mesh yang bisa di-toon (shader asli URP Lit/Standard atau sudah HunterGuild/Toon), tidak termasuk garis tepi.</summary>
        static int Count(GameObject root)
        {
            int n = 0;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
                if (r.GetComponent<ToonOutline>() != null) continue;
                var m = r.sharedMaterial; if (m == null || m.shader == null) continue;
                if (Swappable(m) || m.shader.name == "HunterGuild/Toon") n++;
            }
            return n;
        }

        void Apply(GameObject root, float width)
        {
            var st = root.AddComponent<AnimeStyled>();
            st.count = Count(root);
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var mr = r as MeshRenderer; var sk = r as SkinnedMeshRenderer;
                if (mr == null && sk == null) continue;
                if (mr != null && (mr.GetComponent<TextMesh>() != null || mr.GetComponent<MeshFilter>() == null || mr.GetComponent<MeshFilter>().sharedMesh == null)) continue;
                if (r.GetComponent<ToonOutline>() != null) continue;
                var mats = r.sharedMaterials; bool ok = mats.Length > 0;
                foreach (var m in mats) if (!Swappable(m)) { ok = false; break; }
                if (!ok) continue;
                var swapped = new Material[mats.Length];
                for (int i = 0; i < mats.Length; i++) swapped[i] = ToonOf(mats[i]);
                st.original.Add(new AnimeStyled.Entry { r = r, mats = mats });
                r.sharedMaterials = swapped;
                // garis tepi: salinan mesh, sisi depan dibuang
                var go = new GameObject("ToonOutline"); go.transform.SetParent(r.transform, false);
                Renderer hull;
                if (sk != null)
                {
                    var h = go.AddComponent<SkinnedMeshRenderer>();
                    h.sharedMesh = sk.sharedMesh; h.bones = sk.bones; h.rootBone = sk.rootBone; h.localBounds = sk.localBounds; h.updateWhenOffscreen = sk.updateWhenOffscreen; hull = h;
                }
                else { go.AddComponent<MeshFilter>().sharedMesh = mr.GetComponent<MeshFilter>().sharedMesh; hull = go.AddComponent<MeshRenderer>(); }
                var arr = new Material[Mathf.Max(1, mats.Length)];
                for (int i = 0; i < arr.Length; i++) arr[i] = sk != null ? outMat : outMatBlend;
                hull.sharedMaterials = arr;
                hull.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; hull.receiveShadows = false;
                var sync = go.AddComponent<ToonOutline>(); sync.src = r; sync.width = width; sync.hull = hull;
                st.hulls.Add(go);
            }
        }
    }

    /// <summary>Penanda objek yang sudah ber-toon + data untuk mengembalikan material aslinya.</summary>
    public class AnimeStyled : MonoBehaviour
    {
        public struct Entry { public Renderer r; public Material[] mats; }
        public readonly List<Entry> original = new List<Entry>();
        public readonly List<GameObject> hulls = new List<GameObject>();
        public int count;
        public void Revert(bool immediate)
        {
            foreach (var e in original) if (e.r != null) e.r.sharedMaterials = e.mats;
            foreach (var h in hulls) if (h != null) { if (immediate) DestroyImmediate(h); else Destroy(h); }
            if (immediate) DestroyImmediate(this); else Destroy(this);
        }
    }

    /// <summary>Menyamakan garis tepi dengan mesh aslinya (disembunyikan bila mesh aslinya mati / dimatikan).</summary>
    public class ToonOutline : MonoBehaviour
    {
        public Renderer src, hull; public float width = 0.012f;
        MaterialPropertyBlock mpb;
        void LateUpdate()
        {
            if (src == null) { Destroy(gameObject); return; }
            hull.enabled = src.enabled && src.gameObject.activeInHierarchy && !src.forceRenderingOff;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            hull.GetPropertyBlock(mpb);
            mpb.SetFloat("_Width", width);
            hull.SetPropertyBlock(mpb);
        }
    }
}
