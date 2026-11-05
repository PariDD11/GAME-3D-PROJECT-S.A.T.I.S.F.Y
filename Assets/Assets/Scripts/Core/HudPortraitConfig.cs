using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Isi potret hex di HUD kiri-atas, per job dan evolusi (Level 15 dan Level 30). Diedit lewat plugin:
    /// menu Hunter Guild &gt; Potret HUD (Assets/Editor/HudPortraitWindow.cs). Asset: Assets/Resources/HudPortraitConfig.asset.
    /// Prioritas saat main: evolusi ke-2 &gt; evolusi ke-1 &gt; job. Bila entri tidak punya sprite, HUD memakai ikon bawaan.
    /// </summary>
    public class HudPortraitConfig : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public string key;                     // id job (Swordsman) atau id evolusi (Knight, PaladinLord, ...)
            public Sprite male, female;            // wajah/ilustrasi untuk avatar cowok / cewek (female kosong = pakai male)
            public Vector2 offset;                 // geser dalam hex (px kanvas)
            public float scale = 1f;               // zoom
        }

        public List<Entry> entries = new List<Entry>();

        public Entry Find(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            foreach (var e in entries) if (e != null && e.key == key) return e;
            return null;
        }

        public Entry GetOrAdd(string key)
        {
            var e = Find(key); if (e != null) return e;
            e = new Entry { key = key }; entries.Add(e); return e;
        }

        static HudPortraitConfig cached; static bool tried;
        public static HudPortraitConfig Load()
        {
            if (!tried || cached == null) { cached = Resources.Load<HudPortraitConfig>("HudPortraitConfig"); tried = true; }
            return cached;
        }

        /// <summary>Cari potret untuk state pemain saat ini. false = tidak ada (pakai ikon bawaan).</summary>
        public static bool Resolve(GameState gs, bool female, out Sprite sp, out Vector2 off, out float scale)
        {
            sp = null; off = Vector2.zero; scale = 1f;
            var cfg = Load(); if (cfg == null || gs == null) return false;
            string[] keys = { gs.evo2, gs.evo, gs.job };
            foreach (var k in keys)
            {
                var e = cfg.Find(k); if (e == null) continue;
                var s = female && e.female != null ? e.female : e.male;
                if (s == null) s = e.female;
                if (s == null) continue;
                sp = s; off = e.offset; scale = e.scale <= 0.01f ? 1f : e.scale; return true;
            }
            return false;
        }
    }
}
