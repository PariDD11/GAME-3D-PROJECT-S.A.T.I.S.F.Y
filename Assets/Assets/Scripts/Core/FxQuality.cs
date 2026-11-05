using UnityEngine;

namespace HunterGuild
{
    /// <summary>Kualitas efek pertempuran/sihir. 0 = hemat, 1 = normal, 2 = tinggi (default). Disimpan di PlayerPrefs "hg_fxq".</summary>
    public static class FxQuality
    {
        public static int Level
        {
            get { return Mathf.Clamp(PlayerPrefs.GetInt("hg_fxq", 2), 0, 2); }
            set { PlayerPrefs.SetInt("hg_fxq", Mathf.Clamp(value, 0, 2)); }
        }
        /// <summary>Pengali jumlah partikel.</summary>
        public static float Count { get { int l = Level; return l == 0 ? 0.6f : l == 1 ? 1f : 1.7f; } }
        /// <summary>Pengali intensitas cahaya HDR efek.</summary>
        public static float Glow { get { int l = Level; return l == 0 ? 1.1f : l == 1 ? 1.5f : 1.9f; } }
        public static bool Bloom { get { return Level >= 1 && PlayerPrefs.GetInt("hg_bloom", 1) == 1; } }
        public static int N(int n) { return Mathf.Max(1, Mathf.RoundToInt(n * Count)); }
    }
}
