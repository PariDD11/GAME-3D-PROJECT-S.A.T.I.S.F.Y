using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Basis semua titik interaksi di kota. Pemain menekan E saat berada dalam radius.</summary>
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        public string prompt = "Berinteraksi";
        public float radius = 3.2f;
        public string speakerName, speakerRole;      // opsional: tampilan dialog (jika bukan NPC biasa)

        protected virtual void OnEnable() { All.Add(this); }
        protected virtual void OnDisable() { All.Remove(this); }

        public abstract void Interact();
    }
}
