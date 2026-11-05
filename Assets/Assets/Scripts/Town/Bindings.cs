using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using KeyT = UnityEngine.InputSystem.Key;
#else
using KeyT = UnityEngine.KeyCode;
#endif

namespace HunterGuild
{
    public enum Act { Forward, Back, Left, Right, Jump, Dodge, Sprint, Interact, Attack, Inventory, SkillMenu, Profile, Bestiary, Journal, Drink, Map, Skill1, Skill2, Skill3, Skill4, Skill5, Skill6, Skill7, Mount, Sneak }

    /// <summary>Ganti tombol sendiri (disimpan di PlayerPrefs). Tombol yang bentrok otomatis ditukar.</summary>
    public class Bindings : MonoBehaviour
    {
        public static readonly string[] Labels = { "Maju", "Mundur", "Kiri", "Kanan", "Lompat", "Menghindar / Menyelam", "Lari", "Interaksi", "Serang (tambahan klik kiri)", "Inventori", "Menu skill", "Profil", "Bestiary", "Jurnal", "Minum", "Peta",
            "Skill 1", "Skill 2", "Skill 3", "Skill 4", "Skill 5", "Skill 6", "Skill 7", "Panggil / turun kuda", "Mengendap (toggle)" };
#if ENABLE_INPUT_SYSTEM
        static readonly KeyT[] Def = { KeyT.W, KeyT.S, KeyT.A, KeyT.D, KeyT.Space, KeyT.LeftCtrl, KeyT.LeftShift, KeyT.E, KeyT.J, KeyT.Tab, KeyT.K, KeyT.H, KeyT.B, KeyT.L, KeyT.F, KeyT.M,
            KeyT.Z, KeyT.X, KeyT.C, KeyT.V, KeyT.R, KeyT.T, KeyT.Q, KeyT.N, KeyT.U };
#else
        static readonly KeyT[] Def = { KeyT.W, KeyT.S, KeyT.A, KeyT.D, KeyT.Space, KeyT.LeftControl, KeyT.LeftShift, KeyT.E, KeyT.J, KeyT.Tab, KeyT.K, KeyT.H, KeyT.B, KeyT.L, KeyT.F, KeyT.M,
            KeyT.Z, KeyT.X, KeyT.C, KeyT.V, KeyT.R, KeyT.T, KeyT.Q, KeyT.N, KeyT.U };
#endif
        static KeyT[] cur;
        static Act? capturing; static int doneFrame = -10;
        static Bindings inst;
        public static string LastMsg;

        // sensitivitas kamera (0.3x - 3x) dan balik sumbu Y
        public static float MouseSens { get { return PlayerPrefs.GetFloat("hg_msens", 1f); } }
        public static float PadSens { get { return PlayerPrefs.GetFloat("hg_psens", 1f); } }
        public static bool InvertY { get { return PlayerPrefs.GetInt("hg_invy", 0) == 1; } }
        public static void StepSens(bool pad, int dir)
        {
            string k = pad ? "hg_psens" : "hg_msens";
            float v = Mathf.Clamp(Mathf.Round((PlayerPrefs.GetFloat(k, 1f) + dir * 0.1f) * 10f) / 10f, 0.3f, 3f);
            PlayerPrefs.SetFloat(k, v); PlayerPrefs.Save();
        }
        public static void ToggleInvertY() { PlayerPrefs.SetInt("hg_invy", InvertY ? 0 : 1); PlayerPrefs.Save(); }

        public static bool Capturing { get { return capturing.HasValue || Time.frameCount <= doneFrame + 1; } }
        public static Act? Target { get { return capturing; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (inst != null) return;
            var go = new GameObject("Bindings"); DontDestroyOnLoad(go); inst = go.AddComponent<Bindings>();
            Load();
        }

        static void Load()
        {
            cur = (KeyT[])Def.Clone();
            for (int i = 0; i < cur.Length; i++)
            {
                string s = PlayerPrefs.GetString("hg_key_" + ((Act)i), "");
                if (s.Length == 0) continue;
                try { cur[i] = (KeyT)System.Enum.Parse(typeof(KeyT), s); } catch { }
            }
        }

        public static KeyT Get(Act a) { if (cur == null) Load(); return cur[(int)a]; }
        public static string Name(Act a) { return Get(a).ToString(); }

        public static void Set(Act a, KeyT k)
        {
            if (cur == null) Load();
            int ai = (int)a;
            for (int i = 0; i < cur.Length; i++)
                if (i != ai && cur[i].Equals(k)) { cur[i] = cur[ai]; PlayerPrefs.SetString("hg_key_" + ((Act)i), cur[i].ToString()); LastMsg = "Ditukar dengan: " + Labels[i]; }
            cur[ai] = k; PlayerPrefs.SetString("hg_key_" + a, k.ToString()); PlayerPrefs.Save();
        }

        public static void ResetAll()
        {
            for (int i = 0; i < Def.Length; i++) PlayerPrefs.DeleteKey("hg_key_" + ((Act)i));
            PlayerPrefs.Save(); Load();
        }

        public static void BeginCapture(Act a) { capturing = a; LastMsg = null; }

        void Update()
        {
            if (!capturing.HasValue) return;
            KeyT hit; bool ok = false;
#if ENABLE_INPUT_SYSTEM
            hit = KeyT.None;
            var kb = Keyboard.current;
            if (kb == null) return;
            foreach (var kc in kb.allKeys)
                if (kc.wasPressedThisFrame) { hit = kc.keyCode; ok = true; break; }
            if (ok && hit == KeyT.Escape) { capturing = null; doneFrame = Time.frameCount; return; }
#else
            hit = KeyT.None;
            foreach (KeyCode kc in System.Enum.GetValues(typeof(KeyCode)))
            {
                if ((int)kc >= (int)KeyCode.Mouse0 && (int)kc <= (int)KeyCode.Mouse6) continue;
                if ((int)kc >= (int)KeyCode.JoystickButton0) continue;
                if (Input.GetKeyDown(kc)) { hit = kc; ok = true; break; }
            }
            if (ok && hit == KeyCode.Escape) { capturing = null; doneFrame = Time.frameCount; return; }
#endif
            if (!ok) return;
            Set(capturing.Value, hit);
            capturing = null; doneFrame = Time.frameCount;
        }
    }
}
