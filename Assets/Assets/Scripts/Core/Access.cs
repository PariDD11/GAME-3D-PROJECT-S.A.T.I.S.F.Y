using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n27: opsi aksesibilitas (disimpan di PlayerPrefs, berlaku global tanpa mengubah save).
    /// - Palet Penanda: Standar / Ramah Buta Warna (merah diganti oranye, magenta diganti putih) / Kontras Tinggi. Berlaku untuk penanda minimap baru (setelah pindah area).
    /// - Mode Tahan Otomatis: mini-game memancing (menarik ikan) memakai satu ketukan untuk naik/turun, tidak perlu menahan tombol.
    /// - Timing Mudah: bar timing lebih lambat dan zona hijau lebih lebar.
    /// Ukuran teks sudah ada di Pengaturan > Gameplay > Skala Teks.
    /// </summary>
    public static class Access
    {
        public static readonly string[] PaletteNames = { "Standar", "Ramah Buta Warna", "Kontras Tinggi" };

        public static int Palette
        {
            get { try { return Mathf.Clamp(PlayerPrefs.GetInt("hg_acc_pal", 0), 0, 2); } catch { return 0; } }
            set { try { PlayerPrefs.SetInt("hg_acc_pal", Mathf.Clamp(value, 0, 2)); PlayerPrefs.Save(); } catch { } }
        }
        public static void CyclePalette() { Palette = (Palette + 1) % 3; }

        public static bool ToggleHold
        {
            get { try { return PlayerPrefs.GetInt("hg_acc_hold", 0) == 1; } catch { return false; } }
            set { try { PlayerPrefs.SetInt("hg_acc_hold", value ? 1 : 0); PlayerPrefs.Save(); } catch { } }
        }
        public static bool EasyTiming
        {
            get { try { return PlayerPrefs.GetInt("hg_acc_easy", 0) == 1; } catch { return false; } }
            set { try { PlayerPrefs.SetInt("hg_acc_easy", value ? 1 : 0); PlayerPrefs.Save(); } catch { } }
        }

        public static bool WideLoot
        {
            get { try { return PlayerPrefs.GetInt("hg_acc_loot", 0) == 1; } catch { return false; } }
            set { try { PlayerPrefs.SetInt("hg_acc_loot", value ? 1 : 0); PlayerPrefs.Save(); } catch { } }
        }

        /// <summary>Ubah warna penanda sesuai palet. Warna lain dibiarkan.</summary>
        public static Color Remap(Color c)
        {
            int p = Palette; if (p == 0) return c;
            bool red = c.r > 0.75f && c.g < 0.4f && c.b < 0.45f;
            bool magenta = c.r > 0.7f && c.g < 0.3f && c.b >= 0.45f;
            bool green = c.g > 0.7f && c.r < 0.6f && c.b < 0.6f;
            if (p == 1)
            {
                if (red) return new Color(1f, 0.55f, 0.05f, c.a);
                if (magenta) return new Color(1f, 1f, 1f, c.a);
                if (green) return new Color(0.2f, 0.6f, 1f, c.a);
                return c;
            }
            if (red || magenta) return new Color(1f, 1f, 1f, c.a);
            if (green) return new Color(0.2f, 1f, 1f, c.a);
            return Color.Lerp(c, Color.yellow, 0.35f);
        }
    }
}
