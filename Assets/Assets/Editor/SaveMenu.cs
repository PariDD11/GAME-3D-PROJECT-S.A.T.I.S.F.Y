using UnityEditor;
using UnityEngine;

namespace HunterGuild
{
    public static class SaveMenu
    {
        [MenuItem("Hunter Guild/Save/Hapus Save (mulai baru)")]
        static void Delete() { SaveSystem.DeleteSave(); Debug.Log("Save dihapus: " + SaveSystem.Path_); }

        [MenuItem("Hunter Guild/Save/Tampilkan Folder Save")]
        static void Reveal() { EditorUtility.RevealInFinder(Application.persistentDataPath); }
    }
}
