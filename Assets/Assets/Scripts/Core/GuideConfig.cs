using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Karakter pemandu tutorial: Lynx (memandu avatar cowok) dan Kael (memandu avatar cewek).
    /// Diedit lewat menu Hunter Guild &gt; Karakter Pemandu... ; asset: Assets/Resources/GuideConfig.asset.
    /// </summary>
    public class GuideConfig : ScriptableObject
    {
        [System.Serializable]
        public class Guide
        {
            public string id;              // "Lynx" atau "Kael"
            public string displayName;     // nama yang tampil di dialog (kosong = id)
            public Sprite portrait;        // ilustrasi badan/setengah badan, latar transparan
            public Vector2 offset;         // geser (px kanvas)
            public float scale = 1f;       // zoom
        }

        public List<Guide> guides = new List<Guide>();

        public Guide Find(string id) { foreach (var g in guides) if (g != null && g.id == id) return g; return null; }
        public Guide GetOrAdd(string id) { var g = Find(id); if (g == null) { g = new Guide { id = id }; guides.Add(g); } return g; }

        static GuideConfig cached; static bool tried;
        public static GuideConfig Load()
        {
            if (!tried || cached == null) { cached = Resources.Load<GuideConfig>("GuideConfig"); tried = true; }
            return cached;
        }
    }
}
