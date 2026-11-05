using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HunterGuild
{
    /// <summary>
    /// Deteksi air saat runtime (semua scene, tanpa rebuild). Permukaan air diambil dari SEGITIGA sub-mesh yang bermaterial "water"
    /// (kecuali Arcane) yang menghadap atas/datar, BUKAN dari bounds seluruh renderer: mesh gabungan (mis. kompleks latihan yang memuat
    /// satu tong air, air mancur dengan pancuran) tidak lagi dianggap satu kolam raksasa setinggi atapnya, yang dulu membuat pemain
    /// dan monster "berenang di udara".
    /// Dipakai pemain (berenang, nafas) dan monster/hewan (berenang, tenggelam).
    /// </summary>
    public static class WaterZone
    {
        class W
        {
            public Bounds b; public float y;
            public Vector3[] tv;                           // 3 titik dunia per segitiga air
            public Dictionary<long, List<int>> grid = new Dictionary<long, List<int>>();
            public List<int> giant = new List<int>();     // segitiga sangat besar (bidang air datar raksasa): diuji pada setiap query
            public bool boundsOnly;                        // mesh tak terbaca: hanya kolam kecil murni-air yang dipakai
        }
        const float Cell = 8f;
        static long Key(int x, int z) { return ((long)x << 32) ^ (uint)z; }

        static bool IsOrnamental(Transform t)
        {
            for (int i = 0; i < 4 && t != null; i++, t = t.parent)
            {
                string n = t.name.ToLowerInvariant();
                if (n.IndexOf("mancur") >= 0 || n.IndexOf("fountain") >= 0 || n.IndexOf("fontana") >= 0) return true;
            }
            return false;
        }

        static bool IsWaterMat(Material m)
        {
            if (m == null) return false;
            string n = m.name.ToLowerInvariant();
            return n.IndexOf("water") >= 0 && n.IndexOf("arcane") < 0;
        }

        static W BuildFor(MeshRenderer r)
        {
            var ms = r.sharedMaterials;
            var mf = r.GetComponent<MeshFilter>(); var m = mf != null ? mf.sharedMesh : null;
            if (m == null) return null;
            var w = new W();
            if (!m.isReadable)
            {
                // tak bisa dibaca: hanya kolam kecil yang seluruh materialnya air (aman memakai bounds)
                bool pure = ms.Length > 0;
                for (int i = 0; i < ms.Length; i++) if (!IsWaterMat(ms[i])) { pure = false; break; }
                Bounds bb = r.bounds;
                if (!pure || bb.size.x > 14f || bb.size.z > 14f) return null;
                w.b = bb; w.y = bb.max.y; w.boundsOnly = true;
                return w;
            }
            var vs = m.vertices; var mt = r.localToWorldMatrix;
            var wv = new Vector3[vs.Length];
            for (int i = 0; i < vs.Length; i++) wv[i] = mt.MultiplyPoint3x4(vs[i]);
            var tris = new List<Vector3>();
            var buf = new List<int>();
            int subs = Mathf.Min(m.subMeshCount, ms.Length);
            for (int s = 0; s < subs; s++)
            {
                if (!IsWaterMat(ms[s])) continue;
                buf.Clear(); m.GetTriangles(buf, s);
                for (int t = 0; t + 2 < buf.Count; t += 3)
                {
                    Vector3 a = wv[buf[t]], b = wv[buf[t + 1]], c = wv[buf[t + 2]];
                    Vector3 n = Vector3.Cross(b - a, c - a);
                    float len = n.magnitude;
                    if (len < 0.6f || Mathf.Abs(n.y) / len < 0.85f) continue;       // len = 2 x luas: segitiga < 0,3 m2 (balok pancuran, tong kecil) bukan kolam       // hanya bidang datar/landai (pancuran, dinding, bola diabaikan)
                    tris.Add(a); tris.Add(b); tris.Add(c);
                }
            }
            if (tris.Count == 0) return null;
            w.tv = tris.ToArray();
            bool init = false; float top = float.NegativeInfinity;
            for (int i = 0; i < w.tv.Length; i++)
            {
                if (!init) { w.b = new Bounds(w.tv[i], Vector3.zero); init = true; } else w.b.Encapsulate(w.tv[i]);
                top = Mathf.Max(top, w.tv[i].y);
            }
            w.y = top;
            int n3 = w.tv.Length / 3;
            for (int k = 0; k < n3; k++)
            {
                Vector3 a = w.tv[3 * k], b = w.tv[3 * k + 1], c = w.tv[3 * k + 2];
                int x0 = Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x)) / Cell), x1 = Mathf.FloorToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x)) / Cell);
                int z0 = Mathf.FloorToInt(Mathf.Min(a.z, Mathf.Min(b.z, c.z)) / Cell), z1 = Mathf.FloorToInt(Mathf.Max(a.z, Mathf.Max(b.z, c.z)) / Cell);
                if ((long)(x1 - x0 + 1) * (z1 - z0 + 1) > 4000) { w.giant.Add(k); continue; }
                for (int x = x0; x <= x1; x++)
                    for (int z = z0; z <= z1; z++)
                    {
                        List<int> l; long key = Key(x, z);
                        if (!w.grid.TryGetValue(key, out l)) { l = new List<int>(); w.grid[key] = l; }
                        l.Add(k);
                    }
            }
            return w;
        }

        /// <summary>Tinggi permukaan air (segitiga datar terendah yang menutupi titik xz) atau false bila titik itu bukan air.</summary>
        static bool SurfaceAt(W w, float px, float pz, out float y)
        {
            y = w.y;
            if (w.boundsOnly) return true;
            bool hit = false; float best = float.PositiveInfinity;
            List<int> l;
            if (w.grid.TryGetValue(Key(Mathf.FloorToInt(px / Cell), Mathf.FloorToInt(pz / Cell)), out l))
                for (int i = 0; i < l.Count; i++) Test(w, l[i], px, pz, ref hit, ref best);
            for (int i = 0; i < w.giant.Count; i++) Test(w, w.giant[i], px, pz, ref hit, ref best);
            if (hit) y = best;
            return hit;
        }

        static void Test(W w, int k, float px, float pz, ref bool hit, ref float best)
        {
            Vector3 a = w.tv[3 * k], b = w.tv[3 * k + 1], c = w.tv[3 * k + 2];
            float d = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
            if (Mathf.Abs(d) < 1e-6f) return;
            float u = ((b.z - c.z) * (px - c.x) + (c.x - b.x) * (pz - c.z)) / d;
            float v = ((c.z - a.z) * (px - c.x) + (a.x - c.x) * (pz - c.z)) / d;
            float q = 1f - u - v;
            if (u < -0.001f || v < -0.001f || q < -0.001f) return;
            float y = u * a.y + v * b.y + q * c.y;
            if (y < best) best = y;
            hit = true;
        }

        static readonly List<W> list = new List<W>();
        static int sceneHandle = -1; static float scanAt, scan2At; static bool scan2Done;

        static void Scan()
        {
            list.Clear();
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (r == null || !r.enabled) continue;
                var ms = r.sharedMaterials; bool w = false;
                for (int i = 0; i < ms.Length; i++) if (IsWaterMat(ms[i])) { w = true; break; }
                if (!w) continue;
                if (IsOrnamental(r.transform)) continue;                                  // air mancur/kolam hias: dangkal, tidak bisa direnangi
                try { var w2 = BuildFor(r); if (w2 != null) list.Add(w2); } catch { }
            }
        }

        static void Ensure()
        {
            var sc = SceneManager.GetActiveScene();
            if (sc.handle != sceneHandle) { sceneHandle = sc.handle; list.Clear(); scanAt = Time.time + 1f; scan2At = Time.time + 4f; scan2Done = false; }
            if (scanAt > 0f && Time.time >= scanAt) { scanAt = 0f; Scan(); }
            else if (!scan2Done && scan2At > 0f && Time.time >= scan2At) { scan2Done = true; Scan(); }   // pemindaian ulang setelah RealWater selesai mengganti material
        }

        static readonly RaycastHit[] gbuf = new RaycastHit[16];
        /// <summary>Tinggi tanah di bawah p, TANPA menghitung collider permukaan air itu sendiri (jika dihitung, dasar sungai dianggap setinggi air = pemain tidak pernah berenang).</summary>
        static float GroundBelow(Vector3 p)
        {
            float ty = RT.GroundY(p);
            int n = Physics.RaycastNonAlloc(new Vector3(p.x, Mathf.Max(p.y, ty) + 2.5f, p.z), Vector3.down, gbuf, 80f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.NegativeInfinity;
            for (int i = 0; i < n; i++)
            {
                var h = gbuf[i]; var c = h.collider;
                if (c == null || h.normal.y < 0.6f) continue;
                if (c.GetComponentInParent<Monster>() != null || c.GetComponentInParent<PlayerController>() != null) continue;
                var rd = c.GetComponent<Renderer>();
                if (rd != null) { bool wat = true; var ms = rd.sharedMaterials; if (ms.Length == 0) wat = false; for (int k = 0; k < ms.Length; k++) if (!IsWaterMat(ms[k])) { wat = false; break; } if (wat) continue; }   // hanya bila SELURUH material = air
                if (h.point.y > best && h.point.y <= Mathf.Max(p.y, ty) + 2.6f) best = h.point.y;
            }
            return float.IsNegativeInfinity(best) ? ty : best;
        }

        /// <summary>True bila titik p berada di air; surfaceY = tinggi permukaan air. p.y = tinggi referensi (kaki).</summary>
        public static bool Query(Vector3 p, out float surfaceY)
        {
            surfaceY = float.NegativeInfinity;
            Ensure();
            bool hit = false;
            for (int i = 0; i < list.Count; i++)
            {
                var w = list[i];
                if (p.x < w.b.min.x - 0.2f || p.x > w.b.max.x + 0.2f || p.z < w.b.min.z - 0.2f || p.z > w.b.max.z + 0.2f) continue;
                float sy;
                if (!SurfaceAt(w, p.x, p.z, out sy)) continue;
                if (w.boundsOnly) { if (p.y > sy + 0.1f || p.y < sy - 6f) continue; }
                float gnd = GroundBelow(p);
                if (gnd >= sy - 0.05f || sy - gnd > 14f) continue;                     // tanah di atas air / terlalu dalam = bukan air
                if (sy > surfaceY) surfaceY = sy;
                hit = true;
            }
            return hit;
        }
    }

    /// <summary>Bar nafas + lapisan biru bawah air (tampil hanya saat berenang/menyelam atau nafas belum penuh).</summary>
    public class BreathHud : MonoBehaviour
    {
        Canvas cv; RectTransform root, fill; Image fillImg, tint; Text label;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<BreathHud>() != null) return;
            var go = new GameObject("BreathHud"); DontDestroyOnLoad(go); go.AddComponent<BreathHud>();
        }

        void Start()
        {
            cv = gameObject.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 120;
            var sc = gameObject.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
            // lapisan biru bawah air di canvas terpisah DI BELAKANG HUD (dulu menutupi HUD sehingga tampak pudar)
            var tg = new GameObject("UnderwaterCanvas"); tg.transform.SetParent(transform, false);
            var tc = tg.AddComponent<Canvas>(); tc.renderMode = RenderMode.ScreenSpaceOverlay; tc.sortingOrder = -50;
            tint = Img("UnderwaterTint", tg.transform, new Color(0.08f, 0.32f, 0.48f, 0f)); Stretch(tint.rectTransform);
            var bg = Img("BreathBar", transform, new Color(0.02f, 0.06f, 0.1f, 0.8f));
            root = bg.rectTransform; root.anchorMin = root.anchorMax = new Vector2(0.5f, 0f); root.pivot = new Vector2(0.5f, 0f);
            root.sizeDelta = new Vector2(380f, 20f); root.anchoredPosition = new Vector2(0f, 235f);
            var f = Img("Fill", root, new Color(0.45f, 0.85f, 1f, 0.95f)); fillImg = f; fill = f.rectTransform;
            fill.anchorMin = new Vector2(0f, 0f); fill.anchorMax = new Vector2(1f, 1f); fill.offsetMin = new Vector2(3f, 3f); fill.offsetMax = new Vector2(-3f, -3f);
            var t = new GameObject("Label", typeof(RectTransform)); t.transform.SetParent(root, false);
            label = t.AddComponent<Text>(); label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize = 14; label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white; label.text = "NAFAS"; label.raycastTarget = false;
            Stretch(label.rectTransform);
            root.gameObject.SetActive(false);
        }

        static Image Img(string n, Transform p, Color c)
        {
            var g = new GameObject(n, typeof(RectTransform)); g.transform.SetParent(p, false);
            var im = g.AddComponent<Image>(); im.color = c; im.raycastTarget = false; return im;
        }
        static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }

        void OnGUI()
        {
            // cadangan IMGUI: bar nafas selalu tampil saat berenang/nafas belum penuh, apa pun urutan canvas HUD
            float b = PlayerController.Breath01;
            // bar hanya tampil saat berenang, atau sebentar setelah berenang selagi nafas pulih (nilai nafas yang "nyangkut" di darat tidak menampilkan bar)
            if (!(PlayerController.Swimming || (b < 0.995f && Time.unscaledTime - lastSwimT < 10f))) return;
            float w = 340f, h = 18f, x = (Screen.width - w) / 2f, y = Screen.height - 250f;
            var old = GUI.color;
            GUI.color = new Color(0f, 0.04f, 0.08f, 0.85f); GUI.DrawTexture(new Rect(x - 3, y - 3, w + 6, h + 6), Texture2D.whiteTexture);
            GUI.color = b < 0.25f ? new Color(1f, 0.3f, 0.25f) : new Color(0.45f, 0.85f, 1f); GUI.DrawTexture(new Rect(x, y, w * Mathf.Clamp01(b), h), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y - 1, w, h + 2), b <= 0f ? "TENGGELAM!" : "NAFAS", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 13, fontStyle = FontStyle.Bold });
            GUI.color = old;
        }

        static float lastSwimT = -99f;

        void Update()
        {
            if (PlayerController.Swimming) lastSwimT = Time.unscaledTime;
            if (root == null) return;
            float b = PlayerController.Breath01;
            bool show = false;
            if (root.gameObject.activeSelf) root.gameObject.SetActive(false);
            float ta = PlayerController.Submerged ? 0.38f : 0f;
            tint.color = new Color(0.08f, 0.32f, 0.48f, Mathf.MoveTowards(tint.color.a, ta, Time.unscaledDeltaTime * 2.5f));
            if (!show) return;
            fill.anchorMax = new Vector2(Mathf.Clamp01(b), 1f);
            fill.offsetMax = new Vector2(b >= 0.999f ? -3f : 0f, -3f);
            bool low = b < 0.25f;
            fillImg.color = low ? Color.Lerp(new Color(1f, 0.25f, 0.2f), new Color(1f, 0.7f, 0.6f), Mathf.PingPong(Time.unscaledTime * 4f, 1f)) : new Color(0.45f, 0.85f, 1f, 0.95f);
            label.text = b <= 0f ? "TENGGELAM!" : "NAFAS";
        }
    }
}
