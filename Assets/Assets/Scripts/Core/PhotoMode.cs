using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n14: mode foto. Waktu dibekukan, semua UI disembunyikan, kamera bebas terbang (gerak = tombol gerak, naik = lompat, turun = berguling,
    /// kamera = mouse/stik kanan, lari = percepat). Konfirmasi (E / Enter / A) atau klik kiri memotret ke folder Photos di persistentDataPath.
    /// Batal (Esc / B) keluar. Dibuka dari menu jeda.
    /// </summary>
    public class PhotoMode : MonoBehaviour
    {
        public static bool Active { get; private set; }
        public static int EndFrame = -10;
        static PhotoMode inst;

        Camera cam; Transform ct; float yaw, pitch, savedTime;
        readonly List<Behaviour> disabled = new List<Behaviour>();
        readonly List<Canvas> hiddenCanvases = new List<Canvas>();
        GameObject hint; Text hintText; bool shooting; float msgT;

        public static void Begin()
        {
            if (Active || Camera.main == null) return;
            var go = new GameObject("PhotoMode"); inst = go.AddComponent<PhotoMode>(); inst.Setup();
        }

        void Setup()
        {
            Active = true;
            cam = Camera.main; ct = cam.transform;
            Vector3 e = ct.eulerAngles; yaw = e.y; pitch = e.x > 180f ? e.x - 360f : e.x;
            savedTime = Time.timeScale <= 0f ? 1f : Time.timeScale;
            if (TownUI.I != null) TownUI.I.Close();
            foreach (var c in cam.GetComponents<MonoBehaviour>()) if (c != null && c.enabled && c.GetType().Name == "ThirdPersonCamera") { c.enabled = false; disabled.Add(c); }
            var pc = Object.FindFirstObjectByType<PlayerController>(); if (pc != null && pc.enabled) { pc.enabled = false; disabled.Add(pc); }
            if (PlayerCombat.I != null && PlayerCombat.I.enabled) { PlayerCombat.I.enabled = false; disabled.Add(PlayerCombat.I); }
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (c != null && c.enabled && c.isRootCanvas) { c.enabled = false; hiddenCanvases.Add(c); }
            BuildHint();
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        }

        void BuildHint()
        {
            hint = new GameObject("PhotoHint");
            var cv = hint.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 900;
            var sc = hint.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080);
            var go = new GameObject("T", typeof(RectTransform)); go.transform.SetParent(hint.transform, false);
            var rt = go.GetComponent<RectTransform>(); rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0f); rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0, 24); rt.sizeDelta = new Vector2(0, 80);
            hintText = go.AddComponent<Text>();
#if UNITY_2022_2_OR_NEWER
            hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            hintText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
            hintText.fontSize = 26; hintText.alignment = TextAnchor.LowerCenter; hintText.color = new Color(1f, 0.95f, 0.8f);
            hintText.text = Hint();
            var sh = go.AddComponent<Shadow>(); sh.effectColor = Color.black; sh.effectDistance = new Vector2(2, -2);
        }

        static string Hint() { return "MODE FOTO   Gerak: tombol gerak   Naik: Lompat   Turun: Guling   Lari: percepat   Foto: E / Klik   Keluar: Esc"; }

        void Update()
        {
            Time.timeScale = 0f;
            if (ct == null) { End(); return; }
            float dt = Time.unscaledDeltaTime;
            if (!shooting)
            {
                if (TownInput.CancelDown()) { End(); return; }
                Vector2 look = TownInput.Look();
                yaw += look.x * 1.2f; pitch = Mathf.Clamp(pitch - look.y * 1.2f, -85f, 85f);
                ct.rotation = Quaternion.Euler(pitch, yaw, 0f);
                Vector2 mv = TownInput.Move();
                float up = (TownInput.JumpHeld() ? 1f : 0f) - (TownInput.DiveHeld() ? 1f : 0f);
                float sp = (TownInput.Sprint() ? 18f : 6f) * dt;
                ct.position += (ct.forward * mv.y + ct.right * mv.x + Vector3.up * up) * sp;
                if (TownInput.ConfirmDown() || TownInput.AttackDown()) StartCoroutine(Shoot());
            }
            if (msgT > 0f) { msgT -= dt; if (msgT <= 0f && hintText != null) hintText.text = Hint(); }
        }

        IEnumerator Shoot()
        {
            shooting = true;
            if (hint != null) hint.SetActive(false);
            yield return new WaitForEndOfFrame();
            string msg;
            try
            {
                string dir = Path.Combine(Application.persistentDataPath, "Photos");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "HunterGuild_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
                ScreenCapture.CaptureScreenshot(path);
                msg = "Foto disimpan: " + path;
            }
            catch (System.Exception e) { msg = "Gagal menyimpan foto: " + e.Message; }
            yield return null; yield return null;
            if (hint != null) hint.SetActive(true);
            if (hintText != null) hintText.text = msg;
            msgT = 4f; shooting = false;
        }

        void End()
        {
            Active = false; EndFrame = Time.frameCount;
            foreach (var b in disabled) if (b != null) b.enabled = true;
            foreach (var c in hiddenCanvases) if (c != null) c.enabled = true;
            if (hint != null) Destroy(hint);
            Time.timeScale = savedTime;
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            if (inst == this) inst = null;
            Destroy(gameObject);
        }

        void OnDestroy() { if (Active && inst == this) { Active = false; } }
    }
}
