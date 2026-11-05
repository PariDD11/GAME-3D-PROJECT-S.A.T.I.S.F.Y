#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Hanya untuk project yang memakai Input Manager lama: menambah axis stik kanan, trigger dan D-pad (mapping Xbox di Windows).</summary>
    public static class GamepadAxesSetup
    {
        [MenuItem("Hunter Guild/Setup Gamepad (Input Manager lama)")]
        public static void Run()
        {
            var obj = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/InputManager.asset");
            if (obj == null) { Debug.LogError("InputManager.asset tidak ditemukan."); return; }
            var so = new SerializedObject(obj);
            var axes = so.FindProperty("m_Axes");
            string[] names = { "HG_RX", "HG_RY", "HG_LT", "HG_RT", "HG_DX", "HG_DY" };
            int[] ax = { 3, 4, 8, 9, 5, 6 };   // nomor axis (0-based) = axis ke-4,5,9,10,6,7
            int added = 0;
            for (int n = 0; n < names.Length; n++)
            {
                bool exists = false;
                for (int i = 0; i < axes.arraySize; i++) if (axes.GetArrayElementAtIndex(i).FindPropertyRelative("m_Name").stringValue == names[n]) exists = true;
                if (exists) continue;
                axes.InsertArrayElementAtIndex(axes.arraySize);
                var e = axes.GetArrayElementAtIndex(axes.arraySize - 1);
                e.FindPropertyRelative("m_Name").stringValue = names[n];
                e.FindPropertyRelative("descriptiveName").stringValue = "";
                e.FindPropertyRelative("descriptiveNegativeName").stringValue = "";
                e.FindPropertyRelative("negativeButton").stringValue = "";
                e.FindPropertyRelative("positiveButton").stringValue = "";
                e.FindPropertyRelative("altNegativeButton").stringValue = "";
                e.FindPropertyRelative("altPositiveButton").stringValue = "";
                e.FindPropertyRelative("gravity").floatValue = 0f;
                e.FindPropertyRelative("dead").floatValue = 0.19f;
                e.FindPropertyRelative("sensitivity").floatValue = 1f;
                e.FindPropertyRelative("snap").boolValue = false;
                e.FindPropertyRelative("invert").boolValue = false;
                e.FindPropertyRelative("type").intValue = 2;          // Joystick Axis
                e.FindPropertyRelative("axis").intValue = ax[n];
                e.FindPropertyRelative("joyNum").intValue = 0;
                added++;
            }
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.Log("Gamepad axis ditambahkan: " + added + " (sisanya sudah ada).");
        }
    }
}
#endif
