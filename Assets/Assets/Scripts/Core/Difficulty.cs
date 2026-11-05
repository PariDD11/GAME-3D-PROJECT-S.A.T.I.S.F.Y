using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n14: tingkat kesulitan dan mati permanen, disimpan per slot save (PlayerPrefs hg_diff_N / hg_perma_N).
    /// Kesulitan mengalikan HP dan serangan SEMUA monster (termasuk boss dan Menara) di Monster.Start / Monster.Atk.
    /// Mati permanen: bila aktif, pingsan = karakter gugur, save slot dihapus, lalu pemain memilih Game Baru. Sekali aktif tidak bisa dimatikan.
    /// </summary>
    public static class Difficulty
    {
        public static readonly string[] Names = { "Santai", "Normal", "Sulit", "Mimpi Buruk" };
        static readonly float[] Hp = { 0.75f, 1f, 1.4f, 1.9f };
        static readonly float[] Atk = { 0.65f, 1f, 1.3f, 1.7f };

        static string K(string p) { return p + SaveSystem.Slot; }

        public static int Level { get { try { return Mathf.Clamp(PlayerPrefs.GetInt(K("hg_diff_"), 1), 0, 3); } catch { return 1; } } }
        public static void Cycle() { try { PlayerPrefs.SetInt(K("hg_diff_"), (Level + 1) % Names.Length); PlayerPrefs.Save(); } catch { } }
        /// <summary>Putaran Baru (NG+): tiap putaran menambah HP monster +40% dan serangan +30% (menumpuk dengan tingkat kesulitan).</summary>
        public static int NgLevel { get { var gs = GameState.I; return gs == null ? 0 : gs.Stat("ngplus"); } }
        public static bool NgAvailable { get { var gs = GameState.I; return gs != null && gs.dungeonBest >= 100; } }
        public static void StartNgPlus()
        {
            var gs = GameState.I; if (gs == null || !NgAvailable) return;
            gs.AddStat("ngplus");
            gs.dungeonBest = 0; gs.dungeonBossCleared = 0;       // Menara diulang dengan monster lebih kuat; level, gear, dan harta tetap
            Challenges.OnNg(gs.Stat("ngplus"));
            if (TownUI.I != null) TownUI.I.Toast("Putaran Baru " + gs.Stat("ngplus") + "! Monster lebih kuat, Menara dari lantai 1.");
        }

        public static float HpMul { get { return Hp[Level] * (1f + 0.4f * NgLevel) * Challenges.EliteHp; } }
        public static float AtkMul { get { return Atk[Level] * (1f + 0.3f * NgLevel) * Challenges.EliteAtk; } }
        // v35z102n15: hadiah. Kesulitan tinggi dan Putaran Baru memberi EXP dan gold quest/Menara lebih banyak (Santai sedikit lebih sedikit).
        static readonly float[] ExpL = { 0.9f, 1f, 1.2f, 1.5f };
        static readonly float[] GoldL = { 0.9f, 1f, 1.2f, 1.5f };
        public static float ExpMul { get { return ExpL[Level] * (1f + 0.25f * NgLevel) * Challenges.RewardMul; } }
        public static float GoldMul { get { return GoldL[Level] * (1f + 0.2f * NgLevel) * Challenges.RewardMul; } }
        /// <summary>Gold hadiah (quest, Menara, peti) setelah bonus kesulitan/NG+. Bukan untuk hasil jual barang atau pengembalian deposit.</summary>
        public static int Gold(int g) { return g <= 0 ? g : Mathf.Max(1, Mathf.RoundToInt(g * GoldMul * Balance.GoldRate)); }
        public static string Name { get { return Names[Level]; } }
        public static string Describe { get { return "HP monster x" + HpMul.ToString("0.##") + ", serangan monster x" + AtkMul.ToString("0.##") + ". Hadiah EXP x" + ExpMul.ToString("0.##") + ", gold quest/Menara x" + GoldMul.ToString("0.##") + ". Monster berlaku pada yang muncul berikutnya."; } }

        public static bool Permadeath { get { try { return PlayerPrefs.GetInt(K("hg_perma_"), 0) == 1; } catch { return false; } } }
        public static void EnablePermadeath() { try { PlayerPrefs.SetInt(K("hg_perma_"), 1); PlayerPrefs.Save(); } catch { } }

        /// <summary>Dipanggil PlayerCombat.Faint saat mati permanen aktif.</summary>
        public static void OnPermaDeath()
        {
            int slot = SaveSystem.Slot;
            SaveSystem.Halt();                         // hentikan autosave supaya tidak menulis ulang slot
            SaveSystem.DeleteSave();
            try { PlayerPrefs.DeleteKey("hg_perma_" + slot); PlayerPrefs.DeleteKey("hg_diff_" + slot); PlayerPrefs.Save(); } catch { }
            Time.timeScale = 1f;
            var ui = TownUI.I;
            if (ui == null) return;
            ui.Open("KARAKTERMU GUGUR",
                () => "Mode mati permanen aktif. Perjalanan di slot " + slot + " berakhir di sini, dan simpanannya sudah dihapus.\nBangkitlah sebagai Hunter baru.",
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Mulai Game Baru (slot " + slot + ")", () => MainMenu.NewGameAfterDeath()));
                    l.Add(new UIEntry("Keluar dari game", () =>
                    {
#if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
#else
                        Application.Quit();
#endif
                    }));
                    return l;
                });
        }
    }
}
