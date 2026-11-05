using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HunterGuild
{
    /// <summary>
    /// Minimap pojok kiri-atas (tombol M = perbesar/perkecil). Dibuat otomatis di tiap scene yang punya TownUI.
    /// - Scene kota: minimap PERMANEN, selalu tampil.
    /// - Scene hunt: hanya tampil kalau pemain punya Peta Jelajah (dibeli di Gerbang Luar, hangus saat pulang ke kota).
    /// - Interior Guild dan scene lain: tidak tampil.
    /// Menggambar peta lewat kamera ortografis dari atas ke RenderTexture (tanpa aset tambahan).
    /// </summary>
    public class Minimap : MonoBehaviour
    {
        public string townSceneName = "Town";
        public float townSize = 45f, huntSize = 55f;       // setengah lebar area yang tampak (meter)
        public float refreshInterval = 0.12f;
        public int textureSize = 256;

        Camera cam;
        RenderTexture rt;
        RectTransform frame, clip, wpDot;
        readonly System.Collections.Generic.List<RectTransform> wqDots = new System.Collections.Generic.List<RectTransform>();
        RawImage view;
        RectTransform arrow, targetDot, campDot;

        static Sprite triSprite;
        /// <summary>Segitiga emas menunjuk ke atas, tepi gelap, halus (dibuat lewat kode).</summary>
        static Sprite TriangleSprite()
        {
            if (triSprite != null) return triSprite;
            const int N = 64;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[N * N];
            Vector2 A = new Vector2(N * 0.5f, N * 0.95f), B = new Vector2(N * 0.08f, N * 0.06f), C = new Vector2(N * 0.92f, N * 0.06f);
            Vector2 notch = new Vector2(N * 0.5f, N * 0.26f);          // takik di dasar supaya terbaca sebagai panah
            Color gold = new Color(1f, 0.85f, 0.25f, 1f), edge = new Color(0.25f, 0.14f, 0.02f, 1f);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = TriDist(p, A, notch, B, C);                // negatif = di dalam
                    float aIn = Mathf.Clamp01(0.5f - d), aOut = Mathf.Clamp01(0.5f - (d - 4.5f));
                    Color c = Color.Lerp(edge, gold, Mathf.Clamp01(0.5f - (d + 4f)));
                    c.a = aOut;
                    px[y * N + x] = aOut <= 0f ? Color.clear : c;
                }
            tex.SetPixels(px); tex.Apply();
            triSprite = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
            return triSprite;
        }

        // jarak bertanda ke poligon cembung/cekung kecil (A, notch, B, C): dibuat dari dua segitiga A-B-notch dan A-notch-C
        static float TriDist(Vector2 p, Vector2 a, Vector2 n, Vector2 b, Vector2 c)
        {
            return Mathf.Min(SignedTri(p, a, b, n), SignedTri(p, a, n, c));
        }
        static float SignedTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float s = Mathf.Sign((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x));
            float d1 = s * EdgeD(p, a, b), d2 = s * EdgeD(p, b, c), d3 = s * EdgeD(p, c, a);
            // d* > 0 di dalam (sisi kiri); jarak ke tepi terdekat; negatif bila di dalam
            return -Mathf.Min(d1, Mathf.Min(d2, d3));
        }
        static float EdgeD(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 e = b - a; return ((e.x * (p.y - a.y) - e.y * (p.x - a.x)) / e.magnitude);
        }
        readonly System.Collections.Generic.List<RectTransform> bossDots = new System.Collections.Generic.List<RectTransform>();
        readonly System.Collections.Generic.List<RectTransform> questDots = new System.Collections.Generic.List<RectTransform>();
        RectTransform gateDot;
        CityGate[] gates;
        string gatesScene;
        Text caption;
        Transform player;
        BaseCamp camp;
        float nextRender;
        bool big;
        bool built;
        static Minimap inst;
        /// <summary>Dibaca HUD: minimap sedang tampil / dalam ukuran besar.</summary>
        public static bool Showing, Big;
        /// <summary>Data untuk kompas HUD: arah pandang (derajat), setengah lebar peta (px kanvas), posisi pemain, dan penanda dunia.</summary>
        public static float Yaw, HalfPx;
        public static Vector3 PlayerPos;
        public static bool WpOn; public static Vector3 WpPos; public static string WpLabel = "";
        public struct MapMark { public Vector3 pos; public Color col; public string label; }
        public static readonly System.Collections.Generic.List<MapMark> Markers = new System.Collections.Generic.List<MapMark>();
        Vector3 gatePos; bool gateOn;
        public static void ToggleSize() { if (inst != null && inst.built) inst.SetSize(!inst.big); }
        public static void SetBig(bool b) { if (inst != null && inst.built) inst.SetSize(b); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
            Spawn();
        }

        static void OnLoaded(Scene s, LoadSceneMode m) { Spawn(); }

        static void Spawn()
        {
#if UNITY_2023_1_OR_NEWER
            if (Object.FindFirstObjectByType<Minimap>() != null) return;
#else
            if (Object.FindObjectOfType<Minimap>() != null) return;
#endif
            new GameObject("Minimap").AddComponent<Minimap>();
        }

        enum Mode { Off, Town, Hunt, Interior }

        /// <summary>Minimap + kompas tampil di SEMUA scene (tanpa beli); area berburu memakai jangkauan lebih luas, interior lebih sempit.</summary>
        Mode Current()
        {
            if (HuntManager.I != null) return Mode.Hunt;
            string sn = SceneManager.GetActiveScene().name;
            if (sn == GuildConfig.InteriorScene || InteriorInfo.IsInterior(sn)) return Mode.Interior;
            return Mode.Town;
        }

        void Build()
        {
            inst = this;
            var root = TownUI.I.Root;
            var fimg = UIKit.Img("MinimapFrame", root, new Color(0.04f, 0.05f, 0.07f, 0.92f)); fimg.raycastTarget = false;
            frame = fimg.rectTransform;
            GameHud.StyleMapFrame(fimg);
            // peta berbentuk lingkaran: semua isi peta dipotong dengan mask lingkaran
            var clipImg = UIKit.Img("Clip", frame, Color.white); clipImg.raycastTarget = false; clipImg.sprite = HudArt.DiscFill();
            var clipRt = clipImg.rectTransform; UIKit.Place(clipRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-20, -20));
            var mk = clipImg.gameObject.AddComponent<UnityEngine.UI.Mask>(); mk.showMaskGraphic = false;
            clipRt.SetAsFirstSibling(); clip = clipRt;
            var vgo = UIKit.Rect("MinimapView", clip);
            view = vgo.gameObject.AddComponent<RawImage>();
            view.raycastTarget = false;
            UIKit.Place(vgo, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // v35z102n19: penanda pemain = segitiga emas (ujung menunjuk arah hadap), dengan garis tepi gelap
            var a = UIKit.Img("Arrow", clip, Color.white);
            a.raycastTarget = false; a.sprite = TriangleSprite();
            UIKit.Place(a.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26, 30));
            arrow = a.rectTransform;

            var t = UIKit.Img("Target", clip, Access.Remap(new Color(1f, 0.2f, 0.2f)));
            t.raycastTarget = false;
            UIKit.Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(12, 12));
            targetDot = t.rectTransform;
            var c = UIKit.Img("Camp", clip, new Color(0.4f, 0.8f, 1f));
            c.raycastTarget = false;
            UIKit.Place(c.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 10));
            campDot = c.rectTransform;
            var wd = UIKit.Img("Waypoint", clip, new Color(0.45f, 0.85f, 1f)); wd.raycastTarget = false;
            UIKit.Place(wd.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16, 16));
            wd.rectTransform.localRotation = Quaternion.Euler(0, 0, 45f); wpDot = wd.rectTransform;

            var gd = UIKit.Img("QuestGate", clip, new Color(1f, 0.85f, 0.2f));
            gd.raycastTarget = false;
            UIKit.Place(gd.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14, 14));
            gd.rectTransform.localRotation = Quaternion.Euler(0, 0, 45f);
            gateDot = gd.rectTransform;

            caption = UIKit.Txt("MapCaption", frame, 16, TextAnchor.LowerCenter, Color.white);
            UIKit.Place(caption.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 0f), new Vector2(0, 2), new Vector2(0, 24));

            rt = new RenderTexture(textureSize, textureSize, 16);
            rt.name = "MinimapRT";
            view.texture = rt;

            var cgo = new GameObject("MinimapCamera");
            cgo.transform.SetParent(transform, false);
            cam = cgo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.1f, 0.08f);
            cam.cullingMask = ~0;
            cam.targetTexture = rt;
            cam.allowHDR = false;
            cam.allowMSAA = false;
            cam.enabled = false;           // dirender manual lewat Render() tiap beberapa frame
            cam.depth = -50;
            SetSize(false);
            built = true;
        }

        void SetSize(bool large)
        {
            big = large;
            float px = (large ? 250f : 200f) * 1.148f;
            UIKit.Place(frame, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8f * 1.148f, -58f * 1.148f), new Vector2(px, px));
            Big = large;
        }

        void OnDestroy()
        {
            if (rt != null) { rt.Release(); Destroy(rt); }
        }

        void LateUpdate()
        {
            var mode = TownUI.I != null && TownUI.I.Root != null ? Current() : Mode.Off;
            if (mode == Mode.Off || GameState.I == null)
            {
                Showing = false;
                if (built) frame.gameObject.SetActive(false);
                return;
            }
            if (!built) Build();
            frame.gameObject.SetActive(true);
            Showing = true;

            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p == null) return;
                player = p.transform;
            }
            if (TownInput.MapDown() && (TownUI.I == null || !TownUI.I.PanelOpen)) SetSize(!big);

            float size = (mode == Mode.Town ? townSize : (mode == Mode.Hunt ? huntSize : 24f)) * (big ? 1.7f : 1f);
            cam.orthographicSize = size;
            var mc = Camera.main;
            float yaw = mc != null ? mc.transform.eulerAngles.y : player.eulerAngles.y;
            cam.transform.position = player.position + Vector3.up * 150f;
            cam.transform.rotation = Quaternion.Euler(90f, yaw, 0f);
            cam.farClipPlane = 320f;
            if (Time.unscaledTime >= nextRender)
            {
                nextRender = Time.unscaledTime + refreshInterval;
                cam.Render();
            }

            arrow.localRotation = Quaternion.Euler(0f, 0f, -Mathf.DeltaAngle(yaw, player.eulerAngles.y));
            float half = frame.sizeDelta.x * 0.5f - 18f;

            caption.text = "";

            // target buruan (setelah jejak cukup) dan base camp
            Vector3 tp; string label;
            bool hasT = HuntManager.I != null && HuntManager.I.CompassActive(out tp, out label);
            if (hasT) { HuntManager.I.CompassActive(out tp, out label); Mark(targetDot, tp, yaw, size, half); }
            targetDot.gameObject.SetActive(hasT);

            if (camp == null && mode == Mode.Hunt)
            {
#if UNITY_2023_1_OR_NEWER
                camp = FindFirstObjectByType<BaseCamp>();
#else
                camp = FindObjectOfType<BaseCamp>();
#endif
            }
            bool hasC = mode == Mode.Hunt && camp != null;
            if (hasC) Mark(campDot, camp.transform.position, yaw, size, half);
            campDot.gameObject.SetActive(hasC);

            UpdateQuestMarks(mode, yaw, size, half);
            wpDot.gameObject.SetActive(WpOn); if (WpOn) Mark(wpDot, WpPos, yaw, size, half);
            { Vector3 wp2; string wl2; bool wqOn = WorldQuests.Waypoint(out wp2, out wl2);
              if (wqOn && wqDots.Count == 0) NewDot(wqDots, "WqMark", new Color(0.75f, 0.55f, 1f), 14f, true);
              if (wqDots.Count > 0) { wqDots[0].gameObject.SetActive(wqOn); if (wqOn) Mark(wqDots[0], wp2, yaw, size, half); } }

            // ekspor untuk kompas HUD
            Yaw = yaw; HalfPx = half + 18f; PlayerPos = player.position;
            Markers.Clear();
            if (hasT) { HuntManager.I.CompassActive(out tp, out label); Markers.Add(new MapMark { pos = tp, col = new Color(1f, 0.85f, 0.25f), label = label }); }
            if (hasC) Markers.Add(new MapMark { pos = camp.transform.position, col = new Color(0.45f, 0.85f, 1f), label = "Camp" });
            if (WpOn) Markers.Add(new MapMark { pos = WpPos, col = new Color(0.45f, 0.85f, 1f), label = WpLabel });
            { Vector3 wqp; string wql; if (WorldQuests.Waypoint(out wqp, out wql)) Markers.Add(new MapMark { pos = wqp, col = new Color(0.75f, 0.55f, 1f), label = wql }); }
            foreach (var ip in Investigation.MapPoints()) Markers.Add(new MapMark { pos = ip.Key, col = new Color(1f, 0.92f, 0.3f), label = ip.Value });
            if (gateOn) Markers.Add(new MapMark { pos = gatePos, col = new Color(1f, 0.85f, 0.2f), label = "Gerbang Quest" });
            if (mode == Mode.Hunt)
                foreach (var m in Monster.All)
                    if (m != null && m.isBoss && !m.IsDead) Markers.Add(new MapMark { pos = m.transform.position, col = new Color(0.95f, 0.2f, 0.6f), label = "Boss" });
        }

        RectTransform NewDot(System.Collections.Generic.List<RectTransform> pool, string nm, Color col, float sz, bool diamond)
        {
            var d = UIKit.Img(nm, clip, Access.Remap(col));
            d.raycastTarget = false;
            UIKit.Place(d.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(sz, sz));
            if (diamond) d.rectTransform.localRotation = Quaternion.Euler(0, 0, 45f);
            pool.Add(d.rectTransform);
            return d.rectTransform;
        }

        /// <summary>Penanda boss (merah besar), monster target quest aktif (kuning), dan gerbang menuju area quest (berlian emas, di kota).</summary>
        void UpdateQuestMarks(Mode mode, float yaw, float size, float half)
        {
            var gs = GameState.I;
            int nb = 0, nq = 0;
            if (mode == Mode.Hunt)
            {
                string qt = "";
                if (gs.activeQuest != null && !gs.activeQuest.completed && (gs.activeQuest.type == "hunt" || gs.activeQuest.type == "exterm")) qt = gs.activeQuest.targetId;
                foreach (var m in Monster.All)
                {
                    if (m == null || m.IsDead) continue;
                    bool isQ = qt != "" && m.DefId == qt && !m.isBoss;
                    if (!m.isBoss && !isQ) continue;
                    if (m.isBoss)
                    {
                        if (nb >= bossDots.Count) NewDot(bossDots, "BossMark", new Color(0.85f, 0.1f, 0.55f), 17f, true);
                        var d = bossDots[nb++]; d.gameObject.SetActive(true); Mark(d, m.transform.position, yaw, size, half);
                    }
                    else if (nq < 24)
                    {
                        if (nq >= questDots.Count) NewDot(questDots, "QuestMark", new Color(1f, 0.9f, 0.15f), 9f, false);
                        var d = questDots[nq++]; d.gameObject.SetActive(true); Mark(d, m.transform.position, yaw, size, half);
                    }
                }
            }
            for (int i = nb; i < bossDots.Count; i++) bossDots[i].gameObject.SetActive(false);
            for (int i = nq; i < questDots.Count; i++) questDots[i].gameObject.SetActive(false);

            // gerbang kota menuju area quest aktif
            bool showGate = false;
            if (mode == Mode.Town && gs.activeQuest != null && !gs.activeQuest.completed)
            {
                var q = GuildQuestDB.Find(gs.activeQuest.id);
                string hs = q != null ? q.huntScene : "";
                if (!string.IsNullOrEmpty(hs))
                {
                    string sn = SceneManager.GetActiveScene().name;
                    if (gates == null || gatesScene != sn)
                    {
                        gatesScene = sn;
#if UNITY_2023_1_OR_NEWER
                        gates = FindObjectsByType<CityGate>(FindObjectsSortMode.None);
#else
                        gates = FindObjectsOfType<CityGate>();
#endif
                    }
                    CityGate best = null; float bd = 1e9f;
                    foreach (var g in gates)
                    {
                        if (g == null || g.huntSceneName != hs) continue;
                        float d = (g.transform.position - player.position).sqrMagnitude;
                        if (d < bd) { bd = d; best = g; }
                    }
                    if (best != null) { showGate = true; gatePos = best.transform.position; Mark(gateDot, best.transform.position, yaw, size, half); }
                }
            }
            gateOn = showGate;
            gateDot.gameObject.SetActive(showGate);
        }

        void Mark(RectTransform dot, Vector3 world, float yaw, float size, float half)
        {
            Vector3 to = Quaternion.Euler(0f, -yaw, 0f) * (world - player.position);
            Vector2 p = new Vector2(to.x, to.z) / size * half;
            float m = p.magnitude;
            if (m > half) p *= half / m;           // tetap terlihat di tepi lingkaran peta
            dot.anchoredPosition = p;
        }
    }
}
