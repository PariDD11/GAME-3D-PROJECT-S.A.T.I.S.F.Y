using UnityEngine;
using UnityEngine.UI;

namespace HunterGuild
{
    /// <summary>
    /// Pratinjau 3D rumah yang dijual: kamera kecil mengorbit bangunan dan hasilnya tampil di sisi kiri layar selama menu penawaran terbuka.
    /// Hilang sendiri saat menu ditutup.
    /// </summary>
    public class PropertyPreview : MonoBehaviour
    {
        static PropertyPreview cur;
        Camera cam; RenderTexture rt; GameObject canvasGo;
        Vector3 center; float dist, height, ang;
        Text label;

        public static void Show(string houseId, string caption)
        {
            Hide();
            HomeDoor door = null;
            foreach (var d in Object.FindObjectsOfType<HomeDoor>()) if (d.houseId == houseId) { door = d; break; }
            if (door == null) return;
            var go = new GameObject("PropertyPreview");
            cur = go.AddComponent<PropertyPreview>();
            cur.Init(door, caption);
        }

        public static void Hide() { if (cur != null) Destroy(cur.gameObject); cur = null; }

        void Init(HomeDoor d, string caption)
        {
            center = d.center != Vector3.zero ? d.center : d.transform.position - d.transform.forward * 5f + Vector3.up * 2f;
            dist = d.viewDist > 0f ? d.viewDist : 16f;
            height = Mathf.Max(3f, dist * 0.32f);
            ang = d.transform.eulerAngles.y + 35f;
            rt = new RenderTexture(1040, 680, 24);
            var cgo = new GameObject("PreviewCam");
            cgo.transform.SetParent(transform, false);
            cam = cgo.AddComponent<Camera>();
            cam.targetTexture = rt;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.72f, 0.9f);
            cam.fieldOfView = 45f; cam.nearClipPlane = 0.3f; cam.farClipPlane = 300f;
            cam.depth = -5;
            if (cam.GetComponent<AudioListener>() != null) Destroy(cam.GetComponent<AudioListener>());

            canvasGo = new GameObject("PreviewCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 40;
            var sc = canvasGo.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
            var frame = new GameObject("Frame").AddComponent<Image>();
            frame.transform.SetParent(canvasGo.transform, false);
            frame.color = new Color(0.85f, 0.7f, 0.35f, 0.95f);
            var fr = frame.rectTransform;
            fr.anchorMin = fr.anchorMax = new Vector2(0f, 0.5f); fr.pivot = new Vector2(0f, 0.5f);
            fr.anchoredPosition = new Vector2(40f, 20f); fr.sizeDelta = new Vector2(536f, 372f);
            var raw = new GameObject("View").AddComponent<RawImage>();
            raw.transform.SetParent(frame.transform, false);
            raw.texture = rt; raw.raycastTarget = false;
            var rr = raw.rectTransform; rr.anchorMin = Vector2.zero; rr.anchorMax = Vector2.one; rr.offsetMin = new Vector2(8f, 8f); rr.offsetMax = new Vector2(-8f, -8f);
            var t = new GameObject("Caption").AddComponent<Text>();
            t.transform.SetParent(frame.transform, false);
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 26; t.alignment = TextAnchor.LowerCenter; t.color = Color.white; t.text = "PRATINJAU  -  " + caption; t.raycastTarget = false;
            var tr = t.rectTransform; tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(12f, 14f); tr.offsetMax = new Vector2(-12f, -10f);
            var sh = t.gameObject.AddComponent<Shadow>(); sh.effectColor = Color.black; sh.effectDistance = new Vector2(2f, -2f);
            label = t;
        }

        void LateUpdate()
        {
            if (TownUI.I == null || !TownUI.I.PanelOpen) { Hide(); return; }
            ang += 22f * Time.unscaledDeltaTime;
            float a = ang * Mathf.Deg2Rad;
            cam.transform.position = center + new Vector3(Mathf.Sin(a) * dist, height, Mathf.Cos(a) * dist);
            cam.transform.LookAt(center);
        }

        void OnDestroy() { if (rt != null) { rt.Release(); Destroy(rt); } }
    }
}
