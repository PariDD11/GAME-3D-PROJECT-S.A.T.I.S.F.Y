using UnityEngine;

namespace HunterGuild
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        public Transform target;
        public float distance = 6f;
        public float height = 1.6f;
        public float sensitivity = 2.5f;
        public float minPitch = -10f;
        public float maxPitch = 60f;

        /// <summary>Mode first person (tombol F5 atau `): kamera di mata karakter, kepala disembunyikan.</summary>
        public static bool FirstPerson;
        /// <summary>Archer menahan klik kanan: zoom halus ke arah bidikan (tidak terlalu dekat).</summary>
        public static bool Aiming;
        const float AimFovDelta = 14f, AimDistMul = 0.62f;
        public static float OrigFov = -1f;
        float baseFov = -1f, aimT;
        float yaw, pitch = 20f, savedNear = 0.3f;
        bool lastFp;

        void Start()
        {
            if (target == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) target = p.transform;
            }
            if (target != null) yaw = target.eulerAngles.y;
        }

        // ---- kamera sinematik (ultimate sihir): mundur & naik, FOV melebar, lalu kembali halus
        static float cineStart = -99f, cineIn = 1f, cineHold = 1f, cineOut = 1f, cineDist, cineH, cineFov;

        /// <summary>Kamera menjauh distMul x jarak normal, naik extraHeight m, FOV +fovAdd; masuk inSecs, tahan holdSecs, kembali outSecs.</summary>
        public static void Cinematic(float distMul, float extraHeight, float fovAdd, float inSecs, float holdSecs, float outSecs)
        {
            cineStart = Time.time; cineDist = distMul; cineH = extraHeight; cineFov = fovAdd;
            cineIn = Mathf.Max(0.05f, inSecs); cineHold = holdSecs; cineOut = Mathf.Max(0.05f, outSecs);
        }

        static float CineK()
        {
            float t = Time.time - cineStart;
            if (t < 0f) return 0f;
            float e;
            if (t < cineIn) e = t / cineIn;
            else if (t < cineIn + cineHold) e = 1f;
            else if (t < cineIn + cineHold + cineOut) e = 1f - (t - cineIn - cineHold) / cineOut;
            else return 0f;
            return e * e * (3f - 2f * e);
        }

        static float shakeAmp, shakeEnd, shakeDur = 1f;

        /// <summary>Guncangan kamera singkat (ledakan, gempa). amp dalam meter, dur dalam detik.</summary>
        public static void Shake(float amp, float dur)
        {
            if (Time.time < shakeEnd && shakeAmp > amp) return;
            shakeAmp = amp; shakeDur = Mathf.Max(0.05f, dur); shakeEnd = Time.time + dur;
        }

        static Vector3 ShakeOffset()
        {
            if (Time.time >= shakeEnd) return Vector3.zero;
            float k = (shakeEnd - Time.time) / shakeDur;
            // getaran halus (noise Perlin, bukan acak tiap frame) dan sedikit lebih kecil: terasa berat tapi nyaman di mata
            float s = Time.unscaledTime * 24f;
            Vector3 n = new Vector3(Mathf.PerlinNoise(s, 0.37f) - 0.5f, Mathf.PerlinNoise(s, 7.71f) - 0.5f, Mathf.PerlinNoise(s, 13.13f) - 0.5f) * 2f;
            return n * (shakeAmp * 0.7f * k * k);
        }

        void LateUpdate()
        {
            if (target == null) return;
            if (Cutscene.Active) { Cutscene.Apply(transform); return; }

            bool menuOpen = TownUI.I != null && TownUI.I.PanelOpen;
            if (!menuOpen && TownInput.ViewToggleDown())
            {
                FirstPerson = !FirstPerson;
                pitch = FirstPerson ? 0f : 20f;
                if (TownUI.I != null) TownUI.I.Toast(FirstPerson ? "Mode first person (F5 untuk kembali)" : "Mode third person");
            }
            if (FirstPerson != lastFp)
            {
                lastFp = FirstPerson;
                var cam = GetComponent<Camera>();
                if (cam != null) { if (FirstPerson) { savedNear = cam.nearClipPlane; cam.nearClipPlane = 0.04f; } else cam.nearClipPlane = savedNear; }
                if (PlayerAvatar.I != null) PlayerAvatar.I.ApplyViewMode();
            }
            if (!menuOpen)
            {
                Vector2 l = TownInput.Look();
                yaw += l.x * sensitivity;
                pitch = Mathf.Clamp(pitch - l.y * sensitivity, FirstPerson ? -80f : minPitch, FirstPerson ? 80f : maxPitch);
            }

            var gsA = GameState.I;
            Aiming = !menuOpen && !FirstPerson && gsA != null && (gsA.job == "Archer" || gsA.IsMage) && !StarterConfig.Unarmed && TownInput.GuardHeld();
            aimT = Mathf.MoveTowards(aimT, Aiming ? 1f : 0f, Time.deltaTime * 5f);
            float ease = aimT * aimT * (3f - 2f * aimT);
            var camC = GetComponent<Camera>();
            if (camC != null)
            {
                if (baseFov < 0f) { baseFov = camC.fieldOfView; OrigFov = baseFov; }
                { float pf = SettingsExtra.FovPref; if (pf > 0f) baseFov = pf; }   // v35z102n8: slider FOV
                if (!FirstPerson) camC.fieldOfView = baseFov - AimFovDelta * ease + CineK() * cineFov;
            }

            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            if (FirstPerson)
            {
                // badan selalu menghadap arah pandang
                target.rotation = Quaternion.Euler(0f, yaw, 0f);
                Vector3 eye = PlayerAvatar.I != null ? PlayerAvatar.I.EyePosition() : target.position + Vector3.up * 1.6f;
                transform.SetPositionAndRotation(eye + ShakeOffset(), rot);
                return;
            }
            float ck = CineK();
            Vector3 focus = target.position + Vector3.up * (height + ck * cineH);
            Vector3 desired = focus - rot * Vector3.forward * Mathf.Lerp(distance, distance * AimDistMul, ease) * (1f + ck * cineDist)
                                 + rot * Vector3.right * (0.5f * ease);

            RaycastHit hit;
            if (Physics.Linecast(focus, desired, out hit, ~0, QueryTriggerInteraction.Ignore))
                desired = hit.point + hit.normal * 0.2f;

            transform.SetPositionAndRotation(desired + ShakeOffset(), rot);
        }

        void OnGUI()
        {
            if ((!FirstPerson && !Aiming) || (TownUI.I != null && TownUI.I.PanelOpen)) return;
            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
            var old = GUI.color; GUI.color = new Color(1f, 1f, 1f, 0.85f);
            GUI.DrawTexture(new Rect(cx - 1.5f, cy - 1.5f, 3f, 3f), Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
