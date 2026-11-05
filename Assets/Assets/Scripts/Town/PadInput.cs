using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HunterGuild
{
    public enum PadBtn { South, East, West, North, LB, RB, LT, RT, LS, RS, Start, Select, DUp, DDown, DLeft, DRight, None }
    public enum PadAct { Jump, Dodge, Interact, Inventory, Attack, Guard, Sprint, Drink, Journal, SkillMenu, Map, Pause, ViewToggle, ModA, ModB, Mount, Sneak }

    /// <summary>Lapisan gamepad satu-API untuk Input System baru dan Input Manager lama. Tombol bisa diganti (PlayerPrefs).</summary>
    public class PadInput : MonoBehaviour
    {
        public static readonly string[] ActLabels = { "Lompat", "Menghindar / Menyelam", "Interaksi", "Inventori", "Serang", "Tahan (guard)", "Lari", "Minum", "Jurnal", "Menu skill", "Peta", "Jeda", "Ganti kamera", "Pengubah skill A (skill 1-4)", "Pengubah skill B (skill 5-7)", "Panggil / turun kuda", "Mengendap (toggle)" };
        static readonly string[] BtnNames = { "A", "B", "X", "Y", "LB", "RB", "LT", "RT", "L3", "R3", "Start", "Select", "D-pad Atas", "D-pad Bawah", "D-pad Kiri", "D-pad Kanan", "(tidak dipakai)" };
        static readonly PadBtn[] Def = { PadBtn.South, PadBtn.East, PadBtn.West, PadBtn.North, PadBtn.RT, PadBtn.LT, PadBtn.LS, PadBtn.DDown, PadBtn.DLeft, PadBtn.DRight, PadBtn.Select, PadBtn.Start, PadBtn.None, PadBtn.RB, PadBtn.LB, PadBtn.DUp, PadBtn.RS };
        static PadBtn[] cur;
        static PadAct? capturing; static int doneFrame = -10;
        static PadInput inst;

        public static bool Capturing { get { return capturing.HasValue || Time.frameCount <= doneFrame + 1; } }
        public static PadAct? Target { get { return capturing; } }
        public static bool Active;          // pemain terakhir memakai gamepad
        public static string LastMsg;

        const int N = 16;
        static bool[] held = new bool[N], down = new bool[N], prev = new bool[N];
        static int polled = -1;
        public static Vector2 LeftStick, RightStick;
        static bool anyMove;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (inst != null) return;
            var go = new GameObject("PadInput"); DontDestroyOnLoad(go); inst = go.AddComponent<PadInput>();
            Load();
        }

        static void Load()
        {
            cur = (PadBtn[])Def.Clone();
            for (int i = 0; i < cur.Length; i++)
            {
                string s = PlayerPrefs.GetString("hg_pad_" + ((PadAct)i), "");
                if (s.Length == 0) continue;
                try { cur[i] = (PadBtn)System.Enum.Parse(typeof(PadBtn), s); } catch { }
            }
        }
        public static PadBtn Get(PadAct a) { if (cur == null) Load(); return cur[(int)a]; }
        public static string Name(PadAct a) { return BtnNames[(int)Get(a)]; }
        public static string BtnName(PadBtn b) { return BtnNames[(int)b]; }
        public static void BeginCapture(PadAct a) { capturing = a; LastMsg = null; }
        public static void ResetAll() { for (int i = 0; i < Def.Length; i++) PlayerPrefs.DeleteKey("hg_pad_" + ((PadAct)i)); PlayerPrefs.Save(); Load(); }
        static void Set(PadAct a, PadBtn b)
        {
            if (cur == null) Load();
            int ai = (int)a;
            for (int i = 0; i < cur.Length; i++)
                if (i != ai && cur[i] == b) { cur[i] = cur[ai]; PlayerPrefs.SetString("hg_pad_" + ((PadAct)i), cur[i].ToString()); LastMsg = "Ditukar dengan: " + ActLabels[i]; }
            cur[ai] = b; PlayerPrefs.SetString("hg_pad_" + a, b.ToString()); PlayerPrefs.Save();
        }

        // ---------- baca perangkat ----------
        public static bool Connected
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Gamepad.current != null;
#else
                var n = Input.GetJoystickNames(); for (int i = 0; i < n.Length; i++) if (!string.IsNullOrEmpty(n[i])) return true; return false;
#endif
            }
        }

#if !ENABLE_INPUT_SYSTEM
        static float Axis(string name) { try { return Input.GetAxisRaw(name); } catch { return 0f; } }
#endif

#if !ENABLE_INPUT_SYSTEM
        // Peta tombol mode lama per jenis pad. Xbox: nomor bawaan Unity. PlayStation (DS4/DualSense di Windows): Square=0 Cross=1 Circle=2 Triangle=3 L1=4 R1=5 L2=6 R2=7 Share=8 Options=9 L3=10 R3=11.
        // Switch Pro: urutan posisi (belum diuji di perangkat asli). Urutan array = PadBtn: South,East,West,North,LB,RB,LT,RT,LS,RS,Start,Select,DUp..DRight.
        static int[] layout; static string layoutFor = "";
        static readonly int[] LayXbox = { 0, 1, 2, 3, 4, 5, -1, -1, 8, 9, 7, 6, -1, -1, -1, -1 };
        static readonly int[] LayPS = { 1, 2, 0, 3, 4, 5, 6, 7, 10, 11, 9, 8, -1, -1, -1, -1 };
        static readonly int[] LaySwitch = { 0, 1, 2, 3, 4, 5, 6, 7, 10, 11, 9, 8, -1, -1, -1, -1 };
        public static string PadKind = "Xbox";
        static int[] Layout()
        {
            if (Time.frameCount % 120 == 0 || layout == null)
            {
                string all = ""; foreach (var n in Input.GetJoystickNames()) if (!string.IsNullOrEmpty(n)) { all = n.ToLowerInvariant(); break; }
                if (layout == null || all != layoutFor)
                {
                    layoutFor = all;
                    if (all.Contains("wireless controller") || all.Contains("playstation") || all.Contains("dualshock") || all.Contains("dualsense") || all.Contains("ps4") || all.Contains("ps5")) { layout = LayPS; PadKind = "PlayStation"; }
                    else if (all.Contains("nintendo") || all.Contains("pro controller") || all.Contains("switch")) { layout = LaySwitch; PadKind = "Switch"; }
                    else { layout = LayXbox; PadKind = "Xbox"; }
                }
            }
            return layout;
        }
#endif

        public static string KindLabel
        {
            get
            {
#if !ENABLE_INPUT_SYSTEM
                return Connected ? " (" + PadKind + ")" : "";
#else
                return "";
#endif
            }
        }

        static void Poll()
        {
            if (polled == Time.frameCount) return;
            polled = Time.frameCount;
            System.Array.Copy(held, prev, N);
#if ENABLE_INPUT_SYSTEM
            var g = Gamepad.current;
            if (g == null) { for (int i = 0; i < N; i++) { held[i] = false; down[i] = false; } LeftStick = RightStick = Vector2.zero; return; }
            var c = new UnityEngine.InputSystem.Controls.ButtonControl[] { g.buttonSouth, g.buttonEast, g.buttonWest, g.buttonNorth, g.leftShoulder, g.rightShoulder, g.leftTrigger, g.rightTrigger,
                g.leftStickButton, g.rightStickButton, g.startButton, g.selectButton, g.dpad.up, g.dpad.down, g.dpad.left, g.dpad.right };
            for (int i = 0; i < N; i++) { held[i] = c[i].isPressed; down[i] = c[i].wasPressedThisFrame; }
            LeftStick = g.leftStick.ReadValue(); RightStick = g.rightStick.ReadValue();
#else
            // Windows (Xbox): tombol 0-9, stik kanan axis 4/5, trigger axis 9/10, D-pad axis 6/7. Axis tambahan dibuat lewat menu Hunter Guild > Setup Gamepad (Input Manager lama).
            int[] btn = Layout();
            for (int i = 0; i < N; i++)
            {
                if (btn[i] >= 0) held[i] = Input.GetKey((KeyCode)((int)KeyCode.JoystickButton0 + btn[i]));
            }
            held[(int)PadBtn.LT] = Axis("HG_LT") > 0.5f; held[(int)PadBtn.RT] = Axis("HG_RT") > 0.5f;
            float dx = Axis("HG_DX"), dy = Axis("HG_DY");
            held[(int)PadBtn.DLeft] = dx < -0.5f; held[(int)PadBtn.DRight] = dx > 0.5f; held[(int)PadBtn.DUp] = dy > 0.5f; held[(int)PadBtn.DDown] = dy < -0.5f;
            for (int i = 0; i < N; i++) down[i] = held[i] && !prev[i];
            LeftStick = new Vector2(Axis("Horizontal"), Axis("Vertical")); RightStick = new Vector2(Axis("HG_RX"), -Axis("HG_RY"));
#endif
        }

        public static bool Down(PadBtn b) { if ((int)b >= N) return false; Poll(); return down[(int)b]; }
        public static bool Held(PadBtn b) { if ((int)b >= N) return false; Poll(); return held[(int)b]; }
        public static bool Down(PadAct a) { return Down(Get(a)); }
        public static bool Held(PadAct a) { return Held(Get(a)); }
        /// <summary>true bila pengubah skill (RB/LB bawaan) sedang ditahan.</summary>
        public static bool Mod() { return Held(PadAct.ModA) || Held(PadAct.ModB); }

        void Update()
        {
            Poll();
            if (capturing.HasValue)
            {
                for (int i = 0; i < N; i++)
                    if (down[i]) { Set(capturing.Value, (PadBtn)i); capturing = null; doneFrame = Time.frameCount; break; }
                if (capturing.HasValue && TownInputCancel()) { capturing = null; doneFrame = Time.frameCount; }
                return;
            }
            // perangkat terakhir yang dipakai
            bool padUse = LeftStick.sqrMagnitude > 0.25f || RightStick.sqrMagnitude > 0.25f;
            for (int i = 0; i < N && !padUse; i++) if (held[i]) padUse = true;
            if (padUse) Active = true;
            else
            {
                bool kb = false;
#if ENABLE_INPUT_SYSTEM
                var k = Keyboard.current; var m = Mouse.current;
                kb = (k != null && k.anyKey.wasPressedThisFrame) || (m != null && m.delta.ReadValue().sqrMagnitude > 4f);
#else
                kb = (Input.anyKeyDown && !Input.GetKeyDown(KeyCode.JoystickButton0) && !Input.GetKeyDown(KeyCode.JoystickButton1)) || Input.GetAxisRaw("Mouse X") != 0f;
#endif
                if (kb) Active = false;
            }
        }

        static bool TownInputCancel()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; return k != null && k.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        // ---------- petunjuk di layar ----------
        static GUIStyle st;
        void OnGUI()
        {
            if (!Active || !Connected || Capturing) return;
            if (st == null) st = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.LowerLeft };
            string t = "[" + Name(PadAct.Jump) + "] Lompat   [" + Name(PadAct.Dodge) + "] Hindar   [" + Name(PadAct.Interact) + "] Interaksi   [" + Name(PadAct.Inventory) + "] Inventori   [" + Name(PadAct.Attack) + "] Serang   [" + Name(PadAct.Guard) + "] Tahan   [" + Name(PadAct.ModA) + "+A/B/X/Y] Skill";
            var r = new Rect(12, Screen.height - 30, Screen.width - 24, 24);
            GUI.color = new Color(0, 0, 0, 0.7f); GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), t, st);
            GUI.color = new Color(1f, 1f, 1f, 0.8f); GUI.Label(r, t, st); GUI.color = Color.white;
        }

        /// <summary>Teks tombol interaksi sesuai perangkat aktif: "E" atau "X".</summary>
        public static string InteractHint() { return Active && Connected ? Name(PadAct.Interact) : Bindings.Name(Act.Interact); }
    }
}
