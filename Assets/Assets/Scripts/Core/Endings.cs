using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n25: akhir cerita bercabang + layar "Perjalananmu". Setelah quest bab terakhir (ndy_10) selesai, pemain memilih
    /// salah satu dari tiga akhir. Pilihan disimpan sebagai flag end_* (tidak ikut terhapus oleh NG+). Ringkasan perjalanan bisa dibuka
    /// kapan saja dari Pencapaian > Perjalananmu.
    /// </summary>
    public class Endings : MonoBehaviour
    {
        class End { public string id, title, choice; public string[] pages; }

        static readonly End[] All =
        {
            new End { id = "pintu", title = "Menjadi Pintu", choice = "Aku akan menjadi Pintu. Menahan dari dalam, supaya yang lain bisa hidup di luar.",
                pages = new[] {
                    "Kamu melangkah ke ambang yang belum pernah ditutup siapa pun. Di belakangmu, kota menyalakan lilin satu per satu.",
                    "Yang di bawah tidak dikalahkan. Ia ditahan, dan kamu adalah tahanannya yang paling keras kepala.",
                    "Setahun kemudian Ashwatch bangun lebih lambat daripada kota mana pun, dan lebih keras kepala. Ia mulai lagi dengan roti." } },
            new End { id = "tutup", title = "Menutup Pintu", choice = "Aku akan menutupnya selamanya, berapa pun harganya, termasuk namaku sendiri.",
                pages = new[] {
                    "Kamu menutup ambang itu dengan satu-satunya yang kamu punya: nama yang selama ini kamu bawa.",
                    "Retakan menutup dengan bunyi yang tidak bisa dijelaskan. Tidak ada yang ingat siapa yang menutupnya, hanya bahwa seseorang melakukannya.",
                    "Di papan hitam di sudut jalan, kapur seseorang menulis satu nama tanpa tahu mengapa. Dan tidak ada yang menghapusnya." } },
            new End { id = "buka", title = "Membiarkan Terbuka", choice = "Aku tidak akan menutupnya. Biarkan nama-nama itu diucapkan, dan biar dunia belajar hidup bersamanya.",
                pages = new[] {
                    "Kamu membiarkan ambang itu terbuka, dan untuk pertama kalinya sejak seribu tahun, yang di bawah mendengar nama-nama diucapkan tanpa takut.",
                    "Ashwatch bergetar, bukan karena bencana, melainkan karena kota itu akhirnya bernapas. Papan-papan hitam dicopot satu per satu.",
                    "Tidak ada yang tahu apa yang akan datang. Tetapi untuk pertama kalinya, tidak ada yang berpura-pura tidak mendengar." } },
        };

        public static bool Chosen { get { var gs = GameState.I; if (gs == null) return false; foreach (var e in All) if (gs.flags.Contains("end_" + e.id)) return true; return false; } }
        public static int Seen { get { var gs = GameState.I; int n = 0; if (gs == null) return 0; foreach (var e in All) if (gs.flags.Contains("end_" + e.id)) n++; return n; } }
        public const int Total = 3;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<Endings>() != null) return;
            var go = new GameObject("Endings"); DontDestroyOnLoad(go); go.AddComponent<Endings>();
        }

        float t = 3f;
        void Update()
        {
            t -= Time.unscaledDeltaTime; if (t > 0f) return; t = 1.5f;
            var gs = GameState.I; var ui = TownUI.I;
            if (gs == null || ui == null || ui.PanelOpen || Time.timeScale == 0f) return;
            if (gs.flags.Contains("wq_done_ndy_10") && !gs.flags.Contains("end_prompted") && !PlayerController.InFight()) { gs.flags.Add("end_prompted"); Begin(); }
        }

        /// <summary>Layar pilihan akhir. Boleh dipanggil lagi dari menu untuk melihat akhir lain (NG+).</summary>
        public static void Begin()
        {
            var ui = TownUI.I; if (ui == null) return;
            ui.Open("PINTU TERAKHIR",
                () => "Semua jalan sudah bertemu. Yang di bawah menunggu jawabanmu.\n\nApa yang kamu pilih?",
                () =>
                {
                    var l = new List<UIEntry>();
                    foreach (var e0 in All) { var e = e0; l.Add(new UIEntry(e.title + ": " + e.choice, () => Choose(e))); }
                    l.Add(new UIEntry("Nanti saja", () => { var gs = GameState.I; if (gs != null) gs.flags.Remove("end_prompted"); ui.Close(); }));
                    return l;
                });
        }

        static void Choose(End e)
        {
            var gs = GameState.I; if (gs == null) return;
            gs.flags.Add("end_" + e.id); gs.flags.Add("end_prompted");
            Audio.Sfx("quest_reward");
            Page(e, 0);
        }

        static void Page(End e, int i)
        {
            var ui = TownUI.I;
            ui.Open(e.title.ToUpper(),
                () => e.pages[i],
                () => new List<UIEntry> { new UIEntry(i + 1 < e.pages.Length ? "Lanjut >" : "Lihat Perjalananmu >", () => { if (i + 1 < e.pages.Length) Page(e, i + 1); else Summary(null); }) });
        }

        // ---------------------------------------------------------------- Perjalananmu
        public static void Summary(System.Action back)
        {
            var ui = TownUI.I; if (ui == null) return;
            ui.Open("PERJALANANMU",
                () =>
                {
                    var gs = GameState.I; var cd = CharacterData.Current;
                    var sb = new System.Text.StringBuilder();
                    sb.Append(string.IsNullOrEmpty(cd.name) ? "Hunter" : cd.name).Append(", ").Append(gs.NoJob ? "Tanpa Job" : JobInfo.Display(gs)).Append("\n");
                    sb.Append("Level ").Append(gs.level).Append("   Rank ").Append(gs.rank).Append("   Hari ").Append(gs.day).Append("\n\n");
                    sb.Append("Monster dikalahkan: ").Append(gs.Stat("kills")).Append("   Boss: ").Append(gs.Stat("boss_kills")).Append("\n");
                    sb.Append("Quest Guild selesai: ").Append(gs.questsDone).Append("   Kasus diselesaikan: ").Append(gs.Stat("cases")).Append("\n");
                    sb.Append("Menara: lantai tertinggi ").Append(gs.dungeonBest).Append("/100\n");
                    int worldDone = 0; foreach (var q in WorldQuests.All) if (q != null && WorldQuests.Done(q)) worldDone++;
                    sb.Append("Bab cerita selesai: ").Append(worldDone).Append("/").Append(10).Append("   Lore: ").Append(LoreDB.UnlockedCount).Append("\n");
                    int partners = 0; var gd = GuildData.I; if (gd != null) partners = gd.hired.Count;
                    sb.Append("Partner yang pernah menemani: ").Append(partners).Append("\n");
                    sb.Append("Pencapaian: ").Append(Achievements.UnlockedCount(gs)).Append("/").Append(Achievements.Total).Append("\n");
                    sb.Append("Kesulitan: ").Append(Difficulty.Name).Append(Difficulty.Permadeath ? " (Mati Permanen)" : "").Append("   Putaran Baru: ").Append(gs.Stat("ngplus")).Append("\n\n");
                    sb.Append("Akhir cerita yang ditemukan: ").Append(Seen).Append("/").Append(Total);
                    foreach (var e in All) if (gs.flags.Contains("end_" + e.id)) sb.Append("\n  - ").Append(e.title);
                    return sb.ToString();
                },
                () =>
                {
                    var l = new List<UIEntry>();
                    if (Seen > 0 && Seen < Total) l.Add(new UIEntry("Pilih akhir lain (ulang pilihan di Pintu Terakhir) >", () => Begin()));
                    if (Difficulty.NgAvailable) l.Add(new UIEntry("Mulai Putaran Baru (NG+ " + (Difficulty.NgLevel + 1) + ")", () => { Difficulty.StartNgPlus(); ui.Close(); }));
                    l.Add(new UIEntry(back != null ? "< Kembali" : "Lanjutkan bermain", () => { if (back != null) back(); else ui.Close(); }));
                    return l;
                });
        }
    }
}
