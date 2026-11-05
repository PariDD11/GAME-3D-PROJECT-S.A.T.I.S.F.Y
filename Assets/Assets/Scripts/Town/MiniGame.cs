using System;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Kerangka mini-game overlay (OnGUI): Timing (hentikan penanda di zona) dan Reel (tahan untuk menjaga ikan di zona). Dipakai memancing dan memasak.</summary>
    public class MiniGame : MonoBehaviour
    {
        public static bool Active;
        static MiniGame inst;
        enum Kind { None, Timing, Reel }
        Kind kind; string title; float t0;
        Action<int> doneTiming; Action<bool> doneReel;
        // timing
        float pos, dir = 1f, speed, zoneC, zoneW, perfW;
        // reel
        float fishY, fishV, fishTarget, zoneY, zoneV, prog, timeLeft, fishSpeed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot() { if (inst != null) return; var go = new GameObject("MiniGame"); DontDestroyOnLoad(go); inst = go.AddComponent<MiniGame>(); }

        /// <summary>Timing bar: hasil 0 = meleset, 1 = bagus, 2 = sempurna.</summary>
        public static void Timing(string title, float speed, float zoneWidth, Action<int> done)
        {
            if (inst == null) Boot();
            inst.kind = Kind.Timing; inst.title = title; inst.t0 = Time.unscaledTime; inst.doneTiming = done;
            inst.speed = speed; inst.zoneW = zoneWidth; inst.perfW = zoneWidth * 0.3f; inst.zoneC = UnityEngine.Random.Range(0.3f, 0.75f);
            inst.pos = 0f; inst.dir = 1f; Active = true;
        }

        /// <summary>Menarik ikan: tahan Spasi/klik untuk menaikkan zona, jaga ikan tetap di dalamnya. difficulty 0.3 - 1.</summary>
        public static void Reel(string title, float difficulty, Action<bool> done)
        {
            if (inst == null) Boot();
            var m = inst; m.kind = Kind.Reel; m.title = title; m.t0 = Time.unscaledTime; m.doneReel = done;
            latch = false; m.fishY = 0.5f; m.fishV = 0f; m.fishTarget = 0.5f; m.zoneY = 0.4f; m.zoneV = 0f; m.prog = 0.3f; m.timeLeft = 28f; m.fishSpeed = Mathf.Lerp(0.5f, 1.6f, Mathf.Clamp01(difficulty));
            Active = true;
        }

        static bool Press() { return TownInput.JumpDown() || TownInput.InteractDown() || TownInput.AttackDown() || TownInput.ConfirmDown(); }
        static bool latch;
        static bool Hold()
        {
            if (Access.ToggleHold) { if (Press()) latch = !latch; return latch; }
            return TownInput.JumpHeld() || TownInput.AttackHeld();
        }

        void Update()
        {
            if (kind == Kind.None) return;
            Active = true;
            bool grace = Time.unscaledTime - t0 < 0.35f;
            float dt = Time.unscaledDeltaTime;
            if (TownInput.CancelDown() && !grace)
            {
                var k = kind; kind = Kind.None; Active = false;
                if (k == Kind.Timing && doneTiming != null) doneTiming(0); else if (k == Kind.Reel && doneReel != null) doneReel(false);
                return;
            }
            if (kind == Kind.Timing)
            {
                pos += dir * speed * (Access.EasyTiming ? 0.6f : 1f) * dt;
                if (pos > 1f) { pos = 1f; dir = -1f; } else if (pos < 0f) { pos = 0f; dir = 1f; }
                if (!grace && Press())
                {
                    float d = Mathf.Abs(pos - zoneC);
                    float ez = Access.EasyTiming ? 1.4f : 1f;
                    int res = d <= perfW * 0.5f * ez ? 2 : d <= zoneW * 0.5f * ez ? 1 : 0;
                    kind = Kind.None; Active = false; var cb = doneTiming; doneTiming = null;
                    if (cb != null) cb(res);
                }
            }
            else if (kind == Kind.Reel)
            {
                timeLeft -= dt;
                if (Mathf.Abs(fishY - fishTarget) < 0.04f || UnityEngine.Random.value < 0.01f) fishTarget = UnityEngine.Random.value;
                fishV = Mathf.Lerp(fishV, Mathf.Sign(fishTarget - fishY) * 0.35f * fishSpeed, 3f * dt);
                fishY = Mathf.Clamp01(fishY + fishV * dt);
                zoneV += ((Hold() ? 1.6f : -1.4f)) * dt;
                zoneV = Mathf.Clamp(zoneV, -0.9f, 0.9f);
                zoneY += zoneV * dt;
                if (zoneY < 0.1f) { zoneY = 0.1f; zoneV = 0f; } else if (zoneY > 0.9f) { zoneY = 0.9f; zoneV = 0f; }
                bool inZone = Mathf.Abs(fishY - zoneY) < 0.11f;
                prog += (inZone ? 0.22f : -0.17f) * dt;
                if (prog >= 1f || prog <= 0f || timeLeft <= 0f)
                {
                    bool win = prog >= 1f;
                    kind = Kind.None; Active = false; var cb = doneReel; doneReel = null;
                    if (cb != null) cb(win);
                }
            }
        }

        static GUIStyle lbl;
        static void Box(Rect r, Color c) { var o = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = o; }

        void OnGUI()
        {
            if (kind == Kind.None) return;
            if (lbl == null) lbl = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            float cx = Screen.width * 0.5f;
            if (kind == Kind.Timing)
            {
                var bar = new Rect(cx - 200, Screen.height * 0.7f, 400, 28);
                Box(new Rect(bar.x - 4, bar.y - 4, bar.width + 8, bar.height + 8), new Color(0, 0, 0, 0.75f));
                Box(bar, new Color(0.25f, 0.25f, 0.28f));
                Box(new Rect(bar.x + (zoneC - zoneW * 0.5f) * bar.width, bar.y, zoneW * bar.width, bar.height), new Color(0.3f, 0.8f, 0.35f));
                Box(new Rect(bar.x + (zoneC - perfW * 0.5f) * bar.width, bar.y, perfW * bar.width, bar.height), new Color(1f, 0.85f, 0.3f));
                Box(new Rect(bar.x + pos * bar.width - 3, bar.y - 8, 6, bar.height + 16), Color.white);
                GUI.color = Color.white; GUI.Label(new Rect(cx - 250, bar.y - 48, 500, 34), title, lbl);
                GUI.Label(new Rect(cx - 250, bar.y + 34, 500, 30), "Tekan Spasi / E / klik di zona hijau (emas = sempurna)   Esc = batal", new GUIStyle(lbl) { fontSize = 14 });
            }
            else
            {
                var bar = new Rect(cx + 160, Screen.height * 0.25f, 34, Screen.height * 0.45f);
                Box(new Rect(bar.x - 4, bar.y - 4, bar.width + 8, bar.height + 8), new Color(0, 0, 0, 0.75f));
                Box(bar, new Color(0.18f, 0.3f, 0.45f));
                float zh = 0.22f * bar.height, zy = bar.yMax - zoneY * bar.height - zh * 0.5f;
                Box(new Rect(bar.x, zy, bar.width, zh), new Color(0.3f, 0.85f, 0.4f, 0.8f));
                Box(new Rect(bar.x + 6, bar.yMax - fishY * bar.height - 6, bar.width - 12, 12), new Color(1f, 0.6f, 0.2f));
                var pb = new Rect(bar.xMax + 14, bar.y, 12, bar.height);
                Box(pb, new Color(0.15f, 0.15f, 0.15f, 0.9f));
                Box(new Rect(pb.x, pb.yMax - pb.height * Mathf.Clamp01(prog), pb.width, pb.height * Mathf.Clamp01(prog)), new Color(1f, 0.85f, 0.3f));
                GUI.color = Color.white; GUI.Label(new Rect(cx - 250, bar.y - 40, 500, 34), title, lbl);
                GUI.Label(new Rect(cx - 250, bar.yMax + 10, 500, 30), (Access.ToggleHold ? "Ketuk Spasi / klik: ganti naik/turun (mode tahan otomatis). Jaga ikan (oranye) di zona hijau.   Esc = lepas" : "Tahan Spasi / klik: naikkan zona. Jaga ikan (oranye) di zona hijau.   Esc = lepas"), new GUIStyle(lbl) { fontSize = 14 });
            }
        }
    }
}
