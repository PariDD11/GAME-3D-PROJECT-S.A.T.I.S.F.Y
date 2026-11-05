using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace HunterGuild
{
    /// <summary>
    /// Tampilan "dark fantasy anime" (isekai gelap & dramatis): color grading gelap berkontras, bayangan kebiruan + highlight hangat, vignette,
    /// butir film tipis, bloom lebih kuat, ditambah efek dinamis: denyut chromatic saat memukul, kilat merah saat terluka, hit-stop pukulan berat,
    /// "impact frame" hitam-putih + garis kecepatan + banner nama untuk ultimate.
    /// Aktif/nonaktif lewat Pengaturan (PlayerPrefs hg_anime, default aktif). Hanya tampilan.
    /// </summary>
    public class AnimeLook : MonoBehaviour
    {
        public static bool On { get { return PlayerPrefs.GetInt("hg_anime", 1) == 1; } }
        public static void Toggle() { PlayerPrefs.SetInt("hg_anime", On ? 0 : 1); PlayerPrefs.Save(); }

        /// <summary>Gaya terang ala Genshin (default). Mati = gaya dark fantasy lama.</summary>
        public static bool Bright { get { return PlayerPrefs.GetInt("hg_look", 1) == 1; } }
        public static void ToggleBright() { PlayerPrefs.SetInt("hg_look", Bright ? 0 : 1); PlayerPrefs.Save(); }

        static AnimeLook I;
        Volume vol; VolumeProfile prof;
        ColorAdjustments ca; Vignette vg; ChromaticAberration ch; FilmGrain fg; ShadowsMidtonesHighlights smh; Tonemapping tm; Bloom bl;
        float hit, hurt, impact, tick;
        // overlay
        Canvas cv; Image lines, flash, hurtImg; RectTransform linesRt, band; Text bandTx, bandSub; Image bandBg, bandTop, bandBot; Outline bandOut;
        float lineT0 = -9f, lineDur = 0.9f, bandT0 = -9f; Color lineCol = Color.white;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("AnimeLook"); DontDestroyOnLoad(go); I = go.AddComponent<AnimeLook>();
        }

        // ------------------------------------------------------------------ API
        /// <summary>Denyut tipis saat pukulan mengenai (k 0..1). Pukulan berat (k &gt; 0.6) memberi hit-stop singkat.</summary>
        public static void Hit(float k)
        {
            if (I == null || !On) return;
            I.hit = Mathf.Max(I.hit, Mathf.Clamp01(k));
            if (k > 0.6f && Time.unscaledTime - I.lastStop > 0.3f && FxQuality.Level >= 1) { I.lastStop = Time.unscaledTime; I.StartCoroutine(I.HitStop(0.045f)); }
        }

        /// <summary>Semburan garis radial tipis + kilat di titik pukul (khas anime). heavy = pukulan berat / crit.</summary>
        public static void Impact(Vector3 pos, Color col, bool heavy)
        {
            if (I == null || !On || FxQuality.Level == 0) return;
            int n = FxQuality.N(heavy ? 12 : 6);
            Color hot = Color.Lerp(col, Color.white, 0.6f);
            for (int i = 0; i < n; i++)
            {
                Vector3 d = Random.onUnitSphere; if (d.y < -0.3f) d.y = -d.y;
                float len = Random.Range(heavy ? 1.8f : 0.9f, heavy ? 3.8f : 1.9f);
                CombatFx.Beam(pos + d * 0.25f, pos + d * len, i % 3 == 0 ? hot : col, heavy ? 0.09f : 0.05f, heavy ? 0.16f : 0.11f);
            }
            if (heavy) { CombatFx.ShockRing(pos, 2.8f, hot, 0.22f); CombatFx.Flash(pos, hot, 8f, 2.5f, 0.12f); ThirdPersonCamera.Shake(0.12f, 0.09f); }
        }

        /// <summary>Kilat merah di tepi layar saat pemain terluka (frac = damage / HP maks).</summary>
        public static void Hurt(float frac) { if (I != null && On) I.hurt = Mathf.Max(I.hurt, Mathf.Clamp(0.35f + frac * 2f, 0.35f, 1f)); }

        /// <summary>Ultimate: impact frame, garis kecepatan berwarna elemen, banner nama skill.</summary>
        public static void Ultimate(Color c, string name)
        {
            if (I == null || !On) return;
            I.impact = 1f; I.hit = 1f;
            I.lineCol = c; I.lineT0 = Time.unscaledTime;
            I.bandT0 = Time.unscaledTime;
            if (I.bandTx != null) { I.bandTx.text = (name ?? "").ToUpperInvariant(); I.bandOut.effectColor = new Color(c.r * 0.6f, c.g * 0.6f, c.b * 0.6f, 0.95f); I.bandSub.color = Color.Lerp(c, Color.white, 0.35f); I.bandTop.color = c; I.bandBot.color = c; }
        }

        float lastStop = -9f;
        IEnumerator HitStop(float secs)
        {
            if (!Mathf.Approximately(Time.timeScale, 1f)) yield break;
            const float scale = 0.04f;
            Time.timeScale = scale;
            float t = 0f;
            while (t < secs) { t += Time.unscaledDeltaTime; yield return null; }
            if (Mathf.Approximately(Time.timeScale, scale)) Time.timeScale = 1f;
        }

        // ------------------------------------------------------------------ bangun
        void Start() { BuildOverlay(); }

        void BuildVolume()
        {
            var g = new GameObject("AnimeLookVolume"); g.transform.SetParent(transform, false);
            vol = g.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 60f;
            prof = ScriptableObject.CreateInstance<VolumeProfile>();
            ca = prof.Add<ColorAdjustments>(true); vg = prof.Add<Vignette>(true); ch = prof.Add<ChromaticAberration>(true);
            fg = prof.Add<FilmGrain>(true); smh = prof.Add<ShadowsMidtonesHighlights>(true); tm = prof.Add<Tonemapping>(true); bl = prof.Add<Bloom>(true);
            tm.mode.value = TonemappingMode.ACES; tm.mode.overrideState = true;
            smh.shadows.value = new Vector4(0.84f, 0.92f, 1.14f, 0f); smh.shadows.overrideState = true;
            smh.midtones.value = new Vector4(1f, 1f, 1f, 0f); smh.midtones.overrideState = true;
            smh.highlights.value = new Vector4(1.08f, 1.0f, 0.9f, 0f); smh.highlights.overrideState = true;
            vg.color.value = new Color(0.02f, 0.01f, 0.05f); vg.color.overrideState = true;
            vg.smoothness.value = 0.55f; vg.smoothness.overrideState = true;
            vg.rounded.value = false; vg.rounded.overrideState = true;
            fg.type.value = FilmGrainLookup.Thin1; fg.type.overrideState = true;
            fg.response.value = 0.8f; fg.response.overrideState = true;
            bl.threshold.value = 0.9f; bl.threshold.overrideState = true;
            bl.scatter.value = 0.68f; bl.scatter.overrideState = true;
            bl.tint.value = new Color(0.85f, 0.9f, 1f); bl.tint.overrideState = true;
            bl.clamp.value = 40f; bl.clamp.overrideState = true;
            ca.colorFilter.value = new Color(0.94f, 0.96f, 1f); ca.colorFilter.overrideState = true;
            foreach (var p in new VolumeParameter[] { ca.postExposure, ca.contrast, ca.saturation, vg.intensity, ch.intensity, fg.intensity, bl.intensity }) p.overrideState = true;
            vol.sharedProfile = prof;
        }

        void BuildOverlay()
        {
            var go = new GameObject("AnimeOverlay"); go.transform.SetParent(transform, false);
            cv = go.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 395;
            var sc = go.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
            hurtImg = Full("Hurt", go.transform, Vig(), Color.clear);
            var lg = Full("SpeedLines", go.transform, Lines(), Color.clear); lines = lg; linesRt = lg.rectTransform; linesRt.anchorMin = linesRt.anchorMax = new Vector2(0.5f, 0.5f); linesRt.sizeDelta = new Vector2(2600, 2600);
            flash = Full("Flash", go.transform, null, Color.clear);
            // banner
            var bn = new GameObject("Banner", typeof(RectTransform)); bn.transform.SetParent(go.transform, false);
            band = bn.GetComponent<RectTransform>(); band.anchorMin = band.anchorMax = new Vector2(0.5f, 0.5f); band.sizeDelta = new Vector2(2400, 150); band.localRotation = Quaternion.Euler(0, 0, -4f);
            bandBg = bn.AddComponent<Image>(); bandBg.color = new Color(0.02f, 0.02f, 0.05f, 0.86f); bandBg.raycastTarget = false;
            bandTop = Bar("Top", band, 1f); bandBot = Bar("Bot", band, 0f);
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bandTx = Txt("Name", band, f, 78, FontStyle.BoldAndItalic, new Vector2(0, 14)); bandTx.color = Color.white;
            bandOut = bandTx.gameObject.AddComponent<Outline>(); bandOut.effectDistance = new Vector2(3, -3);
            bandSub = Txt("Sub", band, f, 24, FontStyle.Italic, new Vector2(0, -52)); bandSub.text = "—  ULTIMATE  —";
            band.gameObject.SetActive(false);
        }

        static Image Full(string n, Transform p, Sprite sp, Color c)
        {
            var g = new GameObject(n, typeof(RectTransform)); g.transform.SetParent(p, false);
            var im = g.AddComponent<Image>(); im.sprite = sp; im.color = c; im.raycastTarget = false;
            var rt = im.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            return im;
        }
        static Image Bar(string n, RectTransform p, float top)
        {
            var g = new GameObject(n, typeof(RectTransform)); g.transform.SetParent(p, false);
            var im = g.AddComponent<Image>(); im.raycastTarget = false; var rt = im.rectTransform;
            rt.anchorMin = new Vector2(0, top); rt.anchorMax = new Vector2(1, top); rt.pivot = new Vector2(0.5f, top); rt.sizeDelta = new Vector2(0, 5); rt.anchoredPosition = Vector2.zero;
            return im;
        }
        static Text Txt(string n, RectTransform p, Font f, int size, FontStyle st, Vector2 pos)
        {
            var g = new GameObject(n, typeof(RectTransform)); g.transform.SetParent(p, false);
            var t = g.AddComponent<Text>(); t.font = f; t.fontSize = size; t.fontStyle = st; t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            var rt = t.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(1600, 100); rt.anchoredPosition = pos;
            return t;
        }

        static Sprite vig, lns;
        static Sprite Vig()
        {
            if (vig != null) return vig;
            const int n = 128; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f, d = Mathf.Sqrt(u * u + v * v) / 1.25f;
                px[y * n + x] = new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.4f) / 0.6f)));
            }
            tex.SetPixels(px); tex.Apply(false, false);
            vig = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f); return vig;
        }
        // garis kecepatan radial: kosong di tengah, garis tipis memanjang ke tepi dengan panjang/tebal acak
        static Sprite Lines()
        {
            if (lns != null) return lns;
            const int n = 384; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[n * n]; const int bins = 150;
            var inner = new float[bins]; var width = new float[bins]; var amp = new float[bins];
            var rnd = new System.Random(7);
            for (int i = 0; i < bins; i++) { inner[i] = 0.28f + (float)rnd.NextDouble() * 0.38f; width[i] = 0.35f + (float)rnd.NextDouble() * 0.6f; amp[i] = (float)rnd.NextDouble() < 0.4f ? 0f : 0.5f + (float)rnd.NextDouble() * 0.5f; }
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f, r = Mathf.Sqrt(u * u + v * v);
                float ang = (Mathf.Atan2(v, u) / (Mathf.PI * 2f) + 0.5f) * bins; int b = Mathf.Clamp((int)ang, 0, bins - 1); float f = ang - b;
                float core = 1f - Mathf.Abs(f - 0.5f) * 2f / width[b]; core = Mathf.Clamp01(core);
                float len = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((r - inner[b]) / 0.25f)) * (1f - Mathf.Clamp01((r - 0.96f) / 0.04f));
                px[y * n + x] = new Color(1f, 1f, 1f, core * core * len * amp[b]);
            }
            tex.SetPixels(px); tex.Apply(false, false);
            lns = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f); return lns;
        }

        // ------------------------------------------------------------------ per frame
        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            bool on = On;
            tick -= dt;
            if (tick <= 0f)
            {
                tick = 1.5f;
                var rp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                if (rp != null)
                {
                    if (vol == null) BuildVolume();
                    if (on) { try { if (!rp.supportsHDR) rp.supportsHDR = true; } catch { } }
                    var cam = Camera.main;
                    if (cam != null && on) { var cd = cam.GetComponent<UniversalAdditionalCameraData>(); if (cd == null) cd = cam.gameObject.AddComponent<UniversalAdditionalCameraData>(); if (!cd.renderPostProcessing) cd.renderPostProcessing = true; }
                }
            }
            hit = Mathf.MoveTowards(hit, 0f, dt * 5f); hurt = Mathf.MoveTowards(hurt, 0f, dt * 1.6f); impact = Mathf.MoveTowards(impact, 0f, dt * 9f);
            if (vol != null)
            {
                vol.weight = on && FxQuality.Level >= 1 ? 1f : 0f;
                float q = FxQuality.Level >= 2 ? 1f : 0.6f;
                bool br = Bright;
                if (br)
                {
                    // cerah, jenuh, lembut: bayangan terangkat kebiruan-ungu, highlight hangat, tanpa grain/vignette gelap
                    ca.postExposure.value = Mathf.Lerp(-0.08f, 0.55f, impact);
                    ca.contrast.value = Mathf.Lerp(10f, 85f, impact);
                    ca.saturation.value = Mathf.Lerp(2f, -100f, impact);
                    ca.colorFilter.value = new Color(1f, 0.985f, 0.96f);
                    smh.shadows.value = new Vector4(0.92f, 0.95f, 1.12f, 0.06f);
                    smh.highlights.value = new Vector4(1.06f, 1.02f, 0.94f, 0f);
                    tm.mode.value = TonemappingMode.Neutral;
                    vg.intensity.value = Mathf.Clamp01(0.14f + hurt * 0.4f);
                    vg.color.value = Color.Lerp(new Color(0.12f, 0.1f, 0.2f), new Color(0.45f, 0.02f, 0.03f), hurt);
                    fg.intensity.value = 0f;
                    bl.threshold.value = 1.05f; bl.scatter.value = 0.6f; bl.tint.value = new Color(1f, 0.95f, 0.88f);
                    bl.intensity.value = (0.2f + hit * 0.6f) * q;
                }
                else
                {
                    ca.postExposure.value = Mathf.Lerp(-0.2f, 0.55f, impact);
                    ca.contrast.value = Mathf.Lerp(24f, 85f, impact);
                    ca.saturation.value = Mathf.Lerp(0f, -100f, impact);
                    ca.colorFilter.value = new Color(0.94f, 0.96f, 1f);
                    smh.shadows.value = new Vector4(0.84f, 0.92f, 1.14f, 0f);
                    smh.highlights.value = new Vector4(1.08f, 1.0f, 0.9f, 0f);
                    tm.mode.value = TonemappingMode.ACES;
                    vg.intensity.value = Mathf.Clamp01(0.36f + hurt * 0.25f);
                    vg.color.value = Color.Lerp(new Color(0.02f, 0.01f, 0.05f), new Color(0.45f, 0.02f, 0.03f), hurt);
                    fg.intensity.value = 0.14f * q;
                    bl.threshold.value = 1.0f; bl.scatter.value = 0.6f; bl.tint.value = new Color(0.85f, 0.9f, 1f);
                    bl.intensity.value = (0.4f + hit * 0.6f) * q;
                }
                ch.intensity.value = Mathf.Clamp01(((br ? 0f : 0.02f) + hit * 0.55f + hurt * 0.3f) * q);
            }
            if (cv == null) return;
            // overlay
            hurtImg.color = new Color(0.8f, 0.04f, 0.06f, hurt * 0.5f);
            flash.color = new Color(1f, 1f, 1f, impact * 0.55f);
            float lt = Time.unscaledTime - lineT0;
            if (lt >= 0f && lt < lineDur)
            {
                float a = Mathf.Clamp01(lt / 0.06f) * Mathf.Clamp01(1f - (lt - 0.45f) / (lineDur - 0.45f));
                lines.color = new Color(lineCol.r, lineCol.g, lineCol.b, a * 0.75f); linesRt.localRotation = Quaternion.Euler(0, 0, lt * 14f);
                linesRt.localScale = Vector3.one * (1f + lt * 0.12f);
            }
            else if (lines.color.a > 0f) lines.color = Color.clear;
            float bt = Time.unscaledTime - bandT0;
            if (bt >= 0f && bt < 1.15f)
            {
                if (!band.gameObject.activeSelf) band.gameObject.SetActive(true);
                float x;
                if (bt < 0.16f) x = Mathf.Lerp(1500f, 0f, 1f - Mathf.Pow(1f - bt / 0.16f, 3f));
                else if (bt < 0.9f) x = Mathf.Lerp(0f, -90f, (bt - 0.16f) / 0.74f);
                else x = Mathf.Lerp(-90f, -1600f, Mathf.Pow((bt - 0.9f) / 0.25f, 2f));
                band.anchoredPosition = new Vector2(x, 210f);
            }
            else if (band.gameObject.activeSelf) band.gameObject.SetActive(false);
        }

        void OnDisable() { if (Mathf.Approximately(Time.timeScale, 0.04f)) Time.timeScale = 1f; }
    }
}
