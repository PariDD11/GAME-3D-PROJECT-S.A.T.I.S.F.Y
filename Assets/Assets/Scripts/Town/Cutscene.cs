using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// Sistem cutscene sinematik: kamera berpindah antar shot (lerp atau orbit), letterbox, teks subtitle, bisa dilewati (E / klik / Esc).
    /// Pemain kebal dan tidak bisa bergerak selama cutscene. Konten bawaan: intro game baru di Desa Pemula dan intro boss pertama kali bertemu.
    /// </summary>
    public class Cutscene : MonoBehaviour
    {
        public class Shot
        {
            public Vector3 p0, p1, l0, l1;                  // posisi dan titik pandang awal/akhir (mode lerp)
            public bool orbit; public Vector3 center; public float r0, r1, h0, h1, a0, a1, lookH = 1.2f;   // mode orbit
            public float dur = 3f, fov; public string caption = "", speaker = "";
        }

        public static bool Active;
        static Cutscene inst;
        List<Shot> shots; int idx; float t, barK, capA; Action done; float skipHold; float startedAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot() { if (inst != null) return; var go = new GameObject("Cutscene"); DontDestroyOnLoad(go); inst = go.AddComponent<Cutscene>(); SceneManager.sceneLoaded += (s, m) => { Active = false; if (inst != null) inst.shots = null; }; }

        public static Shot Lerp(Vector3 p0, Vector3 p1, Vector3 look0, Vector3 look1, float dur, string caption = "") { return new Shot { p0 = p0, p1 = p1, l0 = look0, l1 = look1, dur = dur, caption = caption }; }
        public static Shot Orbit(Vector3 center, float r0, float r1, float h0, float h1, float a0, float a1, float dur, string caption = "", float lookH = 1.2f)
        { return new Shot { orbit = true, center = center, r0 = r0, r1 = r1, h0 = h0, h1 = h1, a0 = a0, a1 = a1, dur = dur, caption = caption, lookH = lookH }; }

        public static void Play(List<Shot> list, Action onDone = null)
        {
            if (inst == null) Boot();
            if (list == null || list.Count == 0 || Active) { if (onDone != null) onDone(); return; }
            inst.shots = list; inst.idx = 0; inst.t = 0f; inst.done = onDone; inst.skipHold = 0f; inst.startedAt = Time.unscaledTime; inst.capA = 0f;
            Active = true;
        }

        void Finish()
        {
            Active = false; shots = null; var d = done; done = null;
            if (d != null) d();
        }

        void Update()
        {
            barK = Mathf.MoveTowards(barK, Active ? 1f : 0f, Time.unscaledDeltaTime * 2.5f);
            if (!Active || shots == null) return;
            if (Time.unscaledTime - startedAt > 0.6f && (TownInput.InteractDown() || TownInput.ConfirmDown() || TownInput.CancelDown() || TownInput.AttackDown())) { Finish(); return; }
            t += Time.unscaledDeltaTime;
            var s = shots[idx];
            capA = Mathf.Clamp01(Mathf.Min(t / 0.4f, (s.dur - t) / 0.4f));
            if (t >= s.dur) { idx++; t = 0f; if (idx >= shots.Count) Finish(); }
        }

        /// <summary>Dipanggil ThirdPersonCamera tiap frame saat Active.</summary>
        public static void Apply(Transform cam)
        {
            if (inst == null || inst.shots == null || inst.idx >= inst.shots.Count) return;
            var s = inst.shots[inst.idx]; float u = Mathf.Clamp01(inst.t / Mathf.Max(0.01f, s.dur)); float e = u * u * (3f - 2f * u);
            Vector3 pos, look;
            if (s.orbit)
            {
                float a = Mathf.Lerp(s.a0, s.a1, e) * Mathf.Deg2Rad, r = Mathf.Lerp(s.r0, s.r1, e), h = Mathf.Lerp(s.h0, s.h1, e);
                pos = s.center + new Vector3(Mathf.Sin(a) * r, h, Mathf.Cos(a) * r); look = s.center + Vector3.up * s.lookH;
            }
            else { pos = Vector3.Lerp(s.p0, s.p1, e); look = Vector3.Lerp(s.l0, s.l1, e); }
            Vector3 dir = look - pos; if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
            cam.SetPositionAndRotation(pos, Quaternion.LookRotation(dir));
            var c = cam.GetComponent<Camera>(); if (c != null && s.fov > 1f) c.fieldOfView = s.fov;
        }

        static GUIStyle capStyle, hintStyle;
        void OnGUI()
        {
            if (barK <= 0.01f) return;
            float h = Screen.height * 0.12f * barK;
            var o = GUI.color; GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, h), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, Screen.height - h, Screen.width, h), Texture2D.whiteTexture);
            if (Active && shots != null && idx < shots.Count)
            {
                if (capStyle == null) { capStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true }; hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.UpperRight }; }
                string cap = Loc.T(shots[idx].caption);
                if (!string.IsNullOrEmpty(cap))
                {
                    var r = new Rect(Screen.width * 0.12f, Screen.height - h - 90, Screen.width * 0.76f, 80);
                    GUI.color = new Color(0, 0, 0, 0.85f * capA); GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), cap, capStyle);
                    GUI.color = new Color(1f, 0.97f, 0.85f, capA); GUI.Label(r, cap, capStyle);
                }
                GUI.color = new Color(1, 1, 1, 0.6f); GUI.Label(new Rect(Screen.width - 330, h + 6, 320, 24), Loc.T("[E / Esc] Lewati"), hintStyle);
            }
            GUI.color = o;
        }

        // ================= konten bawaan =================
        float introCheck;
        void LateUpdate()
        {
            if (Active || Time.unscaledTime < introCheck) return;
            introCheck = Time.unscaledTime + 1f;
            var gs = GameState.I; var pc = PlayerCombat.I;
            if (gs == null || pc == null || pc.IsDead) return;
            if (SceneManager.GetActiveScene().name != StarterConfig.VillageScene) return;
            if (gs.Stat("cs_intro") != 0 || gs.level > 1 || gs.day > 1 || !CharacterData.Exists) return;
            if (TownUI.I != null && TownUI.I.PanelOpen) return;
            gs.stats["cs_intro"] = 1;
            Vector3 p = pc.transform.position;
            var l = new List<Shot>
            {
                Lerp(p + new Vector3(30, 22, -30), p + new Vector3(14, 9, -16), p + new Vector3(-4, 2, 6), p + Vector3.up * 1.4f, 4.2f, "Desa Pemula, di tepi hutan Ashwatch."),
                Orbit(p, 7f, 4.5f, 2.2f, 1.5f, 200f, 330f, 4.5f, "Namamu akan dikenal sebagai Hunter, jika kau bertahan."),
                Lerp(p + new Vector3(-2, 1.7f, 3.5f), p + new Vector3(0, 2.5f, 7f), p + new Vector3(0, 1.5f, 40f), p + new Vector3(0, 6f, 60f), 4f, "Langkah pertamamu dimulai di sini."),
            };
            // v35z102n77: prolog sinematik (latar sebelum bab 1) diputar sekali per save, lalu intro desa seperti biasa
            if (!Prologue.Seen) Prologue.Play(() => Play(l)); else Play(l);
        }

        /// <summary>Intro singkat boss saat pertama kali bertemu (sekali per jenis boss per save).</summary>
        public static void BossIntro(Monster m)
        {
            var gs = GameState.I; if (m == null || gs == null || Active) return;
            string key = "cs_boss_" + m.DefId; if (gs.Stat(key) != 0) return;
            gs.stats[key] = 1;
            Vector3 c = m.transform.position;
            float h = m.Def != null ? Mathf.Max(2f, m.Def.height) : 3f;
            var l = new List<Shot>
            {
                Orbit(c, 16f, 10f, h * 0.9f + 1.5f, h * 0.7f, 140f, 200f, 2.6f, m.Name),
                Orbit(c, 10f, 6.5f, h * 0.6f, h * 0.75f, 200f, 215f, 1.4f, "", h * 0.6f),
            };
            Play(l);
        }
    }
}
