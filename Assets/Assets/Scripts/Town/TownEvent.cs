using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Satu kejadian acak di kota. Objek di 'show' diaktifkan selama 'duration', 'movers' berjalan di rutenya.</summary>
    public class TownEvent : MonoBehaviour
    {
        public string title = "Kejadian";
        [TextArea] public string announce = "";
        public float duration = 60f;
        public GameObject[] show;
        public PathMover[] movers;
        public Vector2 hours = new Vector2(6f, 22f);     // hanya terjadi di jam ini
        public GameObject[] showAtNight;                  // misal lentera menyala
        public bool Running { get; private set; }
        float endAt;

        public bool CanRun()
        {
            float h = GameState.I != null ? GameState.I.timeOfDay : 12f;
            return !Running && h >= hours.x && h <= hours.y;
        }
        public void Begin()
        {
            Running = true; endAt = Time.time + duration;
            foreach (var g in show) if (g != null) g.SetActive(true);
            foreach (var m in movers) if (m != null) m.Begin();
            if (TownUI.I != null && announce != "") TownUI.I.Toast(announce);
        }
        void Update()
        {
            if (Running && Time.time > endAt)
            {
                Running = false;
                foreach (var g in show) if (g != null) g.SetActive(false);
                foreach (var m in movers) if (m != null) m.gameObject.SetActive(false);
            }
        }
    }
}
