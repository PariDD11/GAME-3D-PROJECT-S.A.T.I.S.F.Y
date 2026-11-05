using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Memilih kejadian acak tiap 70–160 detik.</summary>
    public class TownEventManager : MonoBehaviour
    {
        public TownEvent[] events;
        public Vector2 interval = new Vector2(70f, 160f);
        float next;
        void Start() { next = Time.time + Random.Range(25f, 50f); }
        void Update()
        {
            if (Time.time < next || events == null) return;
            next = Time.time + Random.Range(interval.x, interval.y);
            var ok = new List<TownEvent>();
            foreach (var e in events) if (e != null && e.CanRun()) ok.Add(e);
            if (ok.Count > 0) ok[Random.Range(0, ok.Count)].Begin();
        }
    }
}
