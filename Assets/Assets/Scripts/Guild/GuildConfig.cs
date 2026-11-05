using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Semua konstanta Guild di satu tempat. Angka bertanda "asumsi" tidak tertulis di GDD dan boleh di-tuning.
    /// Nomor bagian merujuk ke GDD_Hunter_Game.md.
    /// </summary>
    public static class GuildConfig
    {
        // ---------- Scene ----------
        public const string TownScene = "Town";
        public const string InteriorScene = "GuildInterior";
        public const string ArenaScene = "ArenaExam";   // dibangun lewat menu Hunter Guild > Build Arena Ujian

        // Titik muncul saat pindah scene.
        public static readonly Vector3 InteriorEntryPos = new Vector3(0f, 1.1f, -10.5f);  // di dalam pintu Guild, menghadap utara
        public static readonly Vector3 TownReturnPos = new Vector3(32f, 1.1f, -239.4f);   // di depan pintu Guild di kota (denah Kota Benteng, n76: pintu di sisi selatan, 3 m di depan GuildHall), menghadap selatan ke alun-alun gerbang. v35z102n77: sebelumnya masih nilai n75 (36,-191) yang kini jatuh di parit di belakang Guild

        // ---------- Jam & party ----------
        public const float DeskOpenHour = 5f;           // resepsionis tutup 00.00-05.00 (GDD 4.4)
        public const string PartyUnlockRank = "B";      // party wajib mulai quest rank B (GDD 5.1)
        public const int MaxPartners = 2;               // slot party 2-3 termasuk pemain => maks 2 partner (GDD 5.2)
        public const float PosterRefreshHours = 6f;     // rotasi poster (asumsi; GDD: "tiap beberapa in-game hours")
        public const int PosterCount = 5;               // 5 poster sekaligus (GDD 5.2)
        public const int FavoriteMinHires = 3;          // partner yang sudah 3x ikut quest masuk Slot Favorit (asumsi)

        // ---------- Reputasi (GDD 11): pemain hanya melihat nama tier ----------
        public static readonly string[] TierNames = { "Distrusted", "Novice Trust", "Reliable", "Trusted Hunter", "Guild Pillar" };
        public static readonly int[] TierMinPoints = { int.MinValue, 0, 30, 80, 150 };      // asumsi
        public static readonly float[] TierSellMod = { -0.20f, 0f, 0.05f, 0.10f, 0.15f };   // GDD 5.4 (+5-15%) dan 11 (-20%)
        public const int StartReputation = 10;          // mulai di Novice Trust

        // ---------- Ujian kenaikan rank (GDD 3.4). Indeks = rank tujuan, G=0 .. SS=8. G sampai D otomatis tanpa ujian. ----------
        public static readonly int[] ExamMinLevel = { 0, 0, 0, 0, 18, 26, 34, 42, 50 };    // asumsi, selaras "C-B ~ lv 20-35, S-SS ~ lv 50+"
        public static readonly int[] ExamMinQuests = { 0, 0, 0, 0, 60, 90, 130, 175, 230 }; // dalam POIN quest (Buru 3, lainnya 1); asumsi
        public static readonly int[] ExamMinTier = { 0, 0, 0, 0, 1, 2, 2, 3, 4 };          // asumsi, indeks tier reputasi

        // ---------- Debug ----------
        public const bool DebugAllowExamSkip = false;   // true = tombol debug "anggap lulus" muncul bila scene ArenaExam belum dibangun
    }
}
