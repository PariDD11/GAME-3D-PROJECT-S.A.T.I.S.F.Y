using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// Menu utama (muncul sekali saat game dibuka: Lanjutkan / Game Baru / Pengaturan / Keluar)
    /// dan menu jeda (Esc saat bermain: Lanjutkan / Simpan / Pengaturan / Keluar). Memakai panel TownUI, jadi tidak perlu setup scene.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        static MainMenu inst;
        /// <summary>True setelah pemain melewati menu judul (Lanjutkan / Game Baru) atau game sudah berjalan.</summary>
        public static bool TitleDone { get { return inst == null || inst.titleDone; } }
        bool titleDone, titleOpened, wasOpen, paused, confirmQuit, ngConfirm;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (inst != null) return;
            var go = new GameObject("MainMenu");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<MainMenu>();
            SceneManager.sceneLoaded += (s, m) => { Time.timeScale = 1f; inst.paused = false; };
        }

        static bool Busy()
        {
            var cc = CharacterCreatorUI.I;
            if (cc != null && cc.IsOpen) return true;
            if (QuestBoardUI.I != null && QuestBoardUI.I.IsOpen) return true;
            if (Cutscene.Active) return true;
            if (PhotoMode.Active || Time.frameCount <= PhotoMode.EndFrame + 2) return true;   // keluar dari mode foto dengan Esc tidak boleh langsung membuka jeda
            return false;
        }

        void Update()
        {
            var ui = TownUI.I;
            var gs = GameState.I;
            if (ui == null || gs == null) { wasOpen = false; return; }

            // game sudah berjalan (mis. mulai langsung dari scene / pakai cheat): jangan terjebak di menu utama
            if (!titleDone && !ui.PanelOpen && PlayerCombat.I != null && gs.level > 1) titleDone = true;

            // ---- menu utama saat peluncuran
            if (!titleDone)
            {
                if (!titleOpened || !ui.PanelOpen) { titleOpened = true; ShowTitle(); }
                wasOpen = true;
                return;
            }

            // ---- jeda
            if (paused && !ui.PanelOpen) { paused = false; confirmQuit = false; Time.timeScale = 1f; }
            bool open = ui.PanelOpen;
            if (!open && !wasOpen && !Busy() && (TownInput.CancelDown() || TownInput.PauseDown()) && (PlayerCombat.I == null || !PlayerCombat.I.IsDead)) ShowPause();
            wasOpen = ui.PanelOpen;
        }

        /// <summary>Dipanggil tombol jeda di HUD (di samping jam).</summary>
        public static void OpenPause()
        {
            if (inst == null) return;
            var ui = TownUI.I;
            if (!inst.titleDone && PlayerCombat.I != null) inst.titleDone = true;
            if (!inst.titleDone) return;
            if (ui == null || GameState.I == null || ui.PanelOpen || Busy()) return;
            if (PlayerCombat.I != null && PlayerCombat.I.IsDead) return;
            inst.ShowPause();
        }

        // ================= Menu utama =================
        void ShowTitle()
        {
            var ui = TownUI.I;
            ui.Open("HUNTER GUILD",
                () =>
                {
                    var d = SaveSystem.Exists ? SaveSystem.Peek() : null;
                    return "Slot save " + SaveSystem.Slot + "\n" + (d == null ? "Belum ada progres tersimpan." :
                        "Simpanan terakhir: " + d.savedAt + "\nRank " + d.rank + "  Lv " + d.level + "  Hari " + d.day + "  Gold " + d.gold + "  Menara lantai " + d.dungeonBest);
                },
                () =>
                {
                    var l = new List<UIEntry>();
                    bool has = SaveSystem.Exists && SaveSystem.Peek() != null;
                    l.Add(new UIEntry("Lanjutkan", () =>
                    {
                        var sd = SaveSystem.Peek();
                        if (SaveSystem.Load()) { titleDone = true; ui.Close(); StarterConfig.ContinueGame(sd); }
                        else ui.Toast("Gagal memuat simpanan.");
                    }, has));
                    l.Add(new UIEntry(has && !confirmQuit ? "Game Baru (simpanan lama dipindah ke cadangan) >" : confirmQuit ? "YAKIN? Ketuk lagi untuk memulai game baru di slot ini" : "Game Baru", () =>
                    {
                        if (has && !confirmQuit) { confirmQuit = true; return; }
                        confirmQuit = false;
                        SaveSystem.DeleteSave(true);               // simpanan lama masuk "tong sampah": bisa dipulihkan lewat Pulihkan dari cadangan
                        CharacterData.ResetAll();
                        SaveSystem.MarkReady();
                        titleDone = true;
                        ui.Close();
                        ui.Toast("Selamat datang, Hunter!");
                        StarterConfig.StartNewGame();
                    }));
                    l.Add(new UIEntry("Pilih slot save (sekarang: " + SaveSystem.Slot + ") >", () => ShowSlots()));
                    l.Add(new UIEntry("Pulihkan dari cadangan >", () => ShowBackups()));
                    l.Add(new UIEntry("Pengaturan >", () => ShowSettings(ShowTitle)));
                    l.Add(new UIEntry("Keluar", () => Quit()));
                    return l;
                });
        }

        // ================= Cadangan save =================
        void ShowBackups()
        {
            var ui = TownUI.I;
            ui.Open("Pulihkan dari cadangan",
                () => "Slot " + SaveSystem.Slot + ". Cadangan dibuat otomatis tiap menyimpan (terbaru) dan tiap ~10 menit (bergilir 3 generasi). Simpanan yang dihapus Game Baru juga ada di sini. Memulihkan MENIMPA simpanan utama slot ini (yang tertimpa disimpan juga).",
                () =>
                {
                    var l = new List<UIEntry>();
                    List<string> paths; var bk = SaveSystem.ListBackups(out paths);
                    if (bk.Count == 0) l.Add(new UIEntry("(Belum ada cadangan untuk slot ini)", null, false));
                    for (int i = 0; i < bk.Count; i++)
                    {
                        int k = i; var d = bk[k].Value;
                        l.Add(new UIEntry(bk[k].Key + ":  Rank " + d.rank + "  Lv " + d.level + "  Hari " + d.day + "  (" + d.savedAt + ")", () =>
                        {
                            ui.Toast(SaveSystem.RestoreBackup(paths[k]) ? "Simpanan dipulihkan. Tekan Lanjutkan." : "Gagal memulihkan.");
                            ShowTitle();
                        }));
                    }
                    l.Add(new UIEntry("< Kembali", () => ShowTitle()));
                    return l;
                });
        }

        // ================= Slot save =================
        void ShowSlots()
        {
            var ui = TownUI.I;
            ui.Open("Pilih slot save",
                () => "Tiap slot punya progres dan karakter sendiri. Game Baru hanya menghapus slot yang sedang dipilih.",
                () =>
                {
                    var l = new List<UIEntry>();
                    for (int i = 1; i <= SaveSystem.SlotCount; i++)
                    {
                        int s = i; var d = SaveSystem.ExistsSlot(s) ? SaveSystem.PeekSlot(s) : null;
                        string info = d == null ? (SaveSystem.ExistsSlot(s) ? "Berkas rusak (cek Pulihkan dari cadangan)" : "Slot kosong") : "Rank " + d.rank + "  Lv " + d.level + "  Hari " + d.day + "  Gold " + d.gold + "  " + PlayTxt(d.playSec) + "   (" + d.savedAt + ")";
                        l.Add(new UIEntry((SaveSystem.Slot == s ? "> " : "") + "Slot " + s + ":  " + info + (SaveSystem.Slot == s ? "  (slot ini dipakai)" : ""), () => { confirmQuit = false; SaveSystem.SetSlot(s); ShowTitle(); }));
                    }
                    l.Add(new UIEntry("Salin / hapus slot >", () => ShowSlotTools()));
                    l.Add(new UIEntry("< Kembali", () => ShowTitle()));
                    return l;
                });
        }

        static string PlayTxt(int sec) { return sec <= 0 ? "" : (sec / 3600) + "j " + (sec / 60 % 60) + "m"; }
        string slotPend = "";

        void ShowSlotTools()
        {
            var ui = TownUI.I;
            ui.Open("Salin / hapus slot",
                () => "Salin menimpa slot tujuan (isi lama masuk cadangan 'Simpanan yang ditimpa'). Hapus juga memindahkan simpanan ke cadangan, kecuali slot mati permanen. Aksi butuh dua ketukan.",
                () =>
                {
                    var l = new List<UIEntry>();
                    for (int i = 1; i <= SaveSystem.SlotCount; i++)
                    {
                        int a = i; if (!SaveSystem.ExistsSlot(a)) continue;
                        for (int j = 1; j <= SaveSystem.SlotCount; j++)
                        {
                            int b = j; if (a == b) continue;
                            string key = "c" + a + ">" + b;
                            l.Add(new UIEntry((slotPend == key ? "YAKIN? " : "") + "Salin slot " + a + " ke slot " + b + (SaveSystem.ExistsSlot(b) ? " (menimpa)" : ""), () =>
                            {
                                if (slotPend != key) { slotPend = key; ShowSlotTools(); return; }
                                slotPend = ""; ui.Toast(SaveSystem.CopySlot(a, b) ? "Slot disalin." : "Gagal menyalin (slot mati permanen tidak bisa disalin)."); ShowSlotTools();
                            }));
                        }
                        string dk = "d" + a;
                        l.Add(new UIEntry((slotPend == dk ? "YAKIN? " : "") + "Hapus slot " + a, () =>
                        {
                            if (slotPend != dk) { slotPend = dk; ShowSlotTools(); return; }
                            slotPend = ""; SaveSystem.DeleteSlot(a, true); if (SaveSystem.Slot == a) CharacterData.Reload(); ui.Toast("Slot " + a + " dihapus."); ShowSlotTools();
                        }));
                    }
                    if (l.Count == 0) l.Add(new UIEntry("(Belum ada slot terisi)", null, false));
                    l.Add(new UIEntry("< Kembali", () => { slotPend = ""; ShowSlots(); }));
                    return l;
                });
        }

        // ================= Menu jeda =================
        int pausePage;      // 0 utama, 1 Karakter, 2 Misi, 3 Dunia, 4 Lainnya
        void ShowPause()
        {
            var ui = TownUI.I;
            if (!paused || !PausePanel.IsOpen && !TownUI.I.PanelOpen) pausePage = 0;     // buka baru dari game = halaman utama
            paused = true;
            confirmQuit = false; ngConfirm = false;
            Time.timeScale = CoopNet.Connected ? 1f : 0f;      // co-op: dunia tetap berjalan saat menu terbuka
            // v35z102n17: tampilan baru (PausePanel). Versi lama (daftar TownUI) tetap ada di ShowPauseClassic sebagai cadangan.
            PausePanel.Open(
                () =>
                {
                    var gs = GameState.I;
                    return new[] { "Hari " + gs.day, gs.Clock, "Rank " + gs.rank, "Lv. " + gs.level, "Gold", gs.gold.ToString() };
                },
                () => (Titles.Current != "" ? "Gelar: " + Titles.Current + "   " : "") + (SaveSystem.IsSafeScene(SceneManager.GetActiveScene().name) ? "Autosave aktif di sini." : SaveSystem.CanCheckpoint(SceneManager.GetActiveScene().name) ? "Checkpoint otomatis tiap 2 menit saat tenang. Dimuat ulang = muncul di kota." : "Progres di sini tidak tersimpan; selesaikan dulu atau keluar dari area."),
                () =>
                {
                    bool safe = SaveSystem.IsSafeScene(SceneManager.GetActiveScene().name);
                    bool canSave = SaveSystem.CanCheckpoint(SceneManager.GetActiveScene().name);
                    var l = new List<PausePanel.Item>();
                    if (pausePage != 0)
                    {
                        l.Add(new PausePanel.Item("< Kembali", () => { pausePage = 0; PausePanel.Reselect(); }));
                        if (pausePage == 1)       // Karakter & Build
                        {
                            l.Add(new PausePanel.Item("Build: Set, Kartu, Loadout", () => BuildMenu.Open(ShowPause), true, true, true));
                            l.Add(new PausePanel.Item("Upgrade Status (" + GameState.I.skillPoints + " SP)", () => StatUpgrade.Open(ShowPause), true, true, true));
                            l.Add(new PausePanel.Item("Koleksi & Gelar (" + Titles.UnlockedCount + "/" + Titles.All.Count + ")", () => Titles.OpenCollection(ShowPause), true, true, true));
                            l.Add(new PausePanel.Item("Pencapaian (" + Achievements.UnlockedCount(GameState.I) + "/" + Achievements.Total + ")", () => Achievements.Open(ShowPause), true, true, true));
                        }
                        else if (pausePage == 2)  // Misi & Tantangan
                        {
                            l.Add(new PausePanel.Item("Misi Harian & Mingguan" + (DailyQuests.Claimable() > 0 ? " (" + DailyQuests.Claimable() + " siap)" : ""), () => DailyQuests.Open(ShowPause, false), true, true, true));
                            l.Add(new PausePanel.Item("Tantangan Dunia (hadiah x" + Challenges.RewardMul.ToString("0.00") + ")", () => Challenges.Open(ShowPause), true, true, true));
                            if (Difficulty.NgAvailable) l.Add(new PausePanel.Item(ngConfirm ? "YAKIN? Menara diulang & monster lebih kuat. Ketuk lagi" : "Mulai Putaran Baru (NG+ " + (Difficulty.NgLevel + 1) + ")", () => { if (!ngConfirm) { ngConfirm = true; return; } ngConfirm = false; Difficulty.StartNgPlus(); }));
                        }
                        else if (pausePage == 3)  // Dunia
                        {
                            l.Add(new PausePanel.Item("Peta Dunia", () => WorldMap.Open(ShowPause), true, true, true));
                            l.Add(new PausePanel.Item("Perjalanan cepat", () => FastTravel.Open(ShowPause), FastTravel.Blocked() == null, true, true));
                            l.Add(new PausePanel.Item("Mode foto", () => { Time.timeScale = 1f; paused = false; PhotoMode.Begin(); }, true, false, true));
                        }
                        else                      // Lainnya
                        {
                            l.Add(new PausePanel.Item("Panduan", () => ShowGuide(-1), true, true, true));
                            l.Add(new PausePanel.Item("Keseimbangan (Dev)", () => Balance.Open(ShowPause), true, true, true));
                        }
                        return l;
                    }
                    l.Add(new PausePanel.Item("Lanjutkan", () => ui.Close(), true, false, true));
                    l.Add(new PausePanel.Item(safe ? "Simpan sekarang" : canSave ? "Simpan checkpoint" : "Simpan (tidak tersedia di sini)", () =>
                    {
                        ui.Toast(SaveSystem.Save() ? (safe ? "Progres disimpan." : "Checkpoint tersimpan. Saat dimuat kamu muncul di kota.") : "Gagal menyimpan.");
                    }, canSave));
                    l.Add(new PausePanel.Item("Karakter & Build", () => { pausePage = 1; PausePanel.Reselect(); }, true, true));
                    l.Add(new PausePanel.Item("Misi & Tantangan" + (DailyQuests.Claimable() > 0 ? " (" + DailyQuests.Claimable() + " siap)" : ""), () => { pausePage = 2; PausePanel.Reselect(); }, true, true));
                    l.Add(new PausePanel.Item("Dunia & Perjalanan", () => { pausePage = 3; PausePanel.Reselect(); }, true, true));
                    l.Add(new PausePanel.Item(CoopNet.Connected ? "Main Bersama LAN (" + (CoopNet.IsHost ? "HOST" : "tamu") + ")" : "Main Bersama (Co-op LAN)", () => CoopUI.Open(ShowPause), true, true, true));
                    l.Add(new PausePanel.Item("Pengaturan", () => ShowSettings(ShowPause), true, true, true));
                    l.Add(new PausePanel.Item("Panduan & Lainnya", () => { pausePage = 4; PausePanel.Reselect(); }, true, true));
                    l.Add(new PausePanel.Item(confirmQuit ? (safe ? "YAKIN? Ketuk lagi untuk keluar (tersimpan)" : canSave ? "YAKIN? Keluar menyimpan checkpoint. Ketuk lagi" : "YAKIN? Progres hunt ini HILANG. Ketuk lagi") : "Keluar dari game", () =>
                    {
                        if (!confirmQuit) { confirmQuit = true; return; }
                        Quit();
                    }, true, true));
                    return l;
                });
        }

        void ShowPauseClassic()
        {
            var ui = TownUI.I;
            paused = true;
            confirmQuit = false;
            Time.timeScale = CoopNet.Connected ? 1f : 0f;
            ui.Open("JEDA",
                () =>
                {
                    var gs = GameState.I;
                    bool safe = SaveSystem.IsSafeScene(SceneManager.GetActiveScene().name);
                    return "Hari " + gs.day + "  " + gs.Clock + "   Rank " + gs.rank + "  Lv " + gs.level + "   Gold " + gs.gold +
                           "\n" + (safe ? "Autosave aktif di sini." : "Di luar kota progres TIDAK tersimpan otomatis; kembali ke kota untuk menyimpan.");
                },
                () =>
                {
                    bool safe = SaveSystem.IsSafeScene(SceneManager.GetActiveScene().name);
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Lanjutkan", () => ui.Close()));
                    l.Add(new UIEntry(safe ? "Simpan sekarang" : "Simpan (hanya di kota)", () =>
                    {
                        ui.Toast(SaveSystem.Save() ? "Progres disimpan." : "Gagal menyimpan.");
                    }, safe));
                    l.Add(new UIEntry("Pengaturan >", () => ShowSettings(ShowPause)));
                    if (Difficulty.NgAvailable) l.Add(new UIEntry(ngConfirm ? "YAKIN? Menara diulang & monster lebih kuat. Ketuk lagi" : "Mulai Putaran Baru (NG+ " + (Difficulty.NgLevel + 1) + ") >", () => { if (!ngConfirm) { ngConfirm = true; return; } ngConfirm = false; Difficulty.StartNgPlus(); }));
                    l.Add(new UIEntry("Perjalanan cepat >", () => FastTravel.Open(ShowPause), FastTravel.Blocked() == null));
                    l.Add(new UIEntry("Pencapaian (" + Achievements.UnlockedCount(GameState.I) + "/" + Achievements.Total + ") >", () => Achievements.Open(ShowPause)));
                    l.Add(new UIEntry("Mode foto", () => { Time.timeScale = 1f; paused = false; PhotoMode.Begin(); }));
                    l.Add(new UIEntry("Panduan >", () => ShowGuide(-1)));
                    l.Add(new UIEntry(confirmQuit ? (safe ? "YAKIN? Ketuk lagi untuk keluar (tersimpan)" : "YAKIN? Progres hunt ini HILANG. Ketuk lagi untuk keluar") : "Keluar dari game >", () =>
                    {
                        if (!confirmQuit) { confirmQuit = true; return; }
                        Quit();
                    }));
                    return l;
                });
        }

        // ================= Panduan =================
        void ShowGuide(int topic)
        {
            var ui = TownUI.I;
            ui.Open("PANDUAN",
                () => topic < 0 ? "Pilih topik untuk dibaca ulang." : Tutorial.Guide[topic][0] + "\n\n" + Tutorial.Guide[topic][1],
                () =>
                {
                    var l = new List<UIEntry>();
                    for (int i = 0; i < Tutorial.Guide.Length; i++)
                    {
                        int k = i;
                        l.Add(new UIEntry((k == topic ? "> " : "") + Tutorial.Guide[k][0], () => ShowGuide(k)));
                    }
                    l.Add(new UIEntry("< Kembali", () => ShowPause()));
                    return l;
                });
        }

        // ================= Pengaturan =================
        void ShowSettings(System.Action back)
        {
            SettingsPanel.Open(back, () => ShowControls(() => ShowSettings(back)));   // v35z102n8: panel baru
        }

        /// <summary>Daftar pengaturan lama (tidak dipakai lagi; dipertahankan sebagai cadangan).</summary>
        void ShowSettingsClassic(System.Action back)
        {
            var ui = TownUI.I;
            ui.Open("PENGATURAN",
                () => "Grafik: " + GameSettings.GraphicsNames[GameSettings.Graphics] + "     Batas FPS: " + GameSettings.FpsLabel + "     Tampil FPS: " + (GameSettings.ShowFps ? "Ya" : "Tidak") +
                      "\nMusik " + Mathf.RoundToInt(Vol(0) * 100f) + "%     Efek " + Mathf.RoundToInt(Vol(1) * 100f) + "%     Suara NPC " + Mathf.RoundToInt(Vol(2) * 100f) + "%",
                () =>
                {
                    var l = new List<UIEntry>();
                    string[] desc = { "Normal  (tampilan standar project)", "Medium  (bayangan lebih tajam, anti-aliasing 2x)", "HD  (bayangan terbaik, anti-aliasing 4x, detail jauh)" };
                    for (int i = 0; i < 3; i++)
                    {
                        int g = i;
                        l.Add(new UIEntry((GameSettings.Graphics == g ? "[x] " : "[  ] ") + "Grafik " + desc[g], () => GameSettings.SetGraphics(g), GameSettings.Graphics != g));
                    }
                    l.Add(new UIEntry("Batas FPS: " + GameSettings.FpsLabel + "  (ketuk untuk ganti)", () => GameSettings.CycleFps()));
                    l.Add(new UIEntry("Bahasa: " + Loc.Names[Loc.Lang] + "  (ketuk untuk ganti)", () => Loc.Cycle()));
                    l.Add(new UIEntry("Resolusi: " + GameSettings.ResLabel + "  (ketuk untuk ganti)", () => GameSettings.CycleResolution(1)));
                    l.Add(new UIEntry("Layar penuh: " + (GameSettings.Fullscreen ? "Ya" : "Tidak"), () => GameSettings.ToggleFullscreen()));
                    l.Add(new UIEntry("Skala render: " + GameSettings.ScaleLabel + "  (turunkan bila berat)", () => GameSettings.CycleScale()));
                    l.Add(new UIEntry("Gaya visual: " + (AnimeLook.Bright ? "Cerah ala Genshin" : "Dark fantasy") + "  (ketuk untuk ganti)", () => AnimeLook.ToggleBright()));
                    l.Add(new UIEntry("Gaya anime (grading + outline + efek layar): " + (AnimeLook.On ? "Ya" : "Tidak"), () => AnimeLook.Toggle()));
                    l.Add(new UIEntry("Tampilan megah & mewah (cahaya, aura, efek kena): " + (RoyalFx.On ? "Ya" : "Tidak"), () => LuxuryLook.Toggle()));
                    l.Add(new UIEntry("Tampilkan FPS: " + (GameSettings.ShowFps ? "Ya" : "Tidak"), () => GameSettings.ToggleShowFps()));
                    l.Add(new UIEntry("Musik  -", () => SetVol(0, -0.1f)));
                    l.Add(new UIEntry("Musik  +", () => SetVol(0, 0.1f)));
                    l.Add(new UIEntry("Efek suara  -", () => SetVol(1, -0.1f)));
                    l.Add(new UIEntry("Efek suara  +", () => { SetVol(1, 0.1f); Audio.Sfx("hit_heavy"); }));
                    l.Add(new UIEntry("Suara NPC  -", () => SetVol(2, -0.1f)));
                    l.Add(new UIEntry("Suara NPC  +", () => { SetVol(2, 0.1f); Audio.Voice("receptionist", "Selamat datang!"); }));
                    l.Add(new UIEntry("Kontrol (ganti tombol) >", () => ShowControls(() => ShowSettings(back))));
                    l.Add(new UIEntry("Tampilan & Kenyamanan >", () => ShowUiSettings(() => ShowSettings(back))));
                    l.Add(new UIEntry("< Kembali", () => { if (back != null) back(); }));
                    return l;
                });
        }

        // ================= Kontrol =================
        void ShowControls(System.Action back)
        {
            var ui = TownUI.I;
            ui.Open("KONTROL",
                () => Bindings.Target.HasValue ? "Tekan tombol keyboard baru untuk: " + Bindings.Labels[(int)Bindings.Target.Value] + "   (Esc = batal)"
                      : "Ketuk satu baris lalu tekan tombol baru. Tombol bentrok otomatis ditukar." + (Bindings.LastMsg != null ? "\n" + Bindings.LastMsg : "") +
                        "\nKamera: mouse " + Bindings.MouseSens.ToString("0.0") + "x   gamepad " + Bindings.PadSens.ToString("0.0") + "x   balik Y: " + (Bindings.InvertY ? "Ya" : "Tidak") +
                        "\nGamepad " + (TownInput.HasPad ? "terdeteksi" : "tidak terdeteksi") + PadInput.KindLabel + "\nSneak bawaan = R3 (tekan stik kanan); ganti kamera tidak punya tombol bawaan, atur di sini.",
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Gamepad: ganti tombol >", () => ShowPadControls(() => ShowControls(back))));
                    l.Add(new UIEntry("Sensitivitas mouse  -", () => Bindings.StepSens(false, -1)));
                    l.Add(new UIEntry("Sensitivitas mouse  +", () => Bindings.StepSens(false, 1)));
                    l.Add(new UIEntry("Sensitivitas gamepad  -", () => Bindings.StepSens(true, -1)));
                    l.Add(new UIEntry("Sensitivitas gamepad  +", () => Bindings.StepSens(true, 1)));
                    l.Add(new UIEntry("Balik sumbu Y kamera gamepad: " + (Bindings.InvertY ? "Ya" : "Tidak"), () => Bindings.ToggleInvertY()));
                    for (int i = 0; i < Bindings.Labels.Length; i++)
                    {
                        Act a = (Act)i;
                        string mark = Bindings.Target.HasValue && Bindings.Target.Value == a ? "..." : Bindings.Name(a);
                        l.Add(new UIEntry(Bindings.Labels[i] + ":  " + mark, () => Bindings.BeginCapture(a)));
                    }
                    l.Add(new UIEntry("Kembalikan tombol keyboard bawaan", () => Bindings.ResetAll()));
                    l.Add(new UIEntry("< Kembali", () => { if (back != null) back(); }));
                    return l;
                });
        }

        void ShowPadControls(System.Action back)
        {
            var ui = TownUI.I;
            ui.Open("KONTROL GAMEPAD",
                () => PadInput.Target.HasValue ? "Tekan tombol gamepad baru untuk: " + PadInput.ActLabels[(int)PadInput.Target.Value] + "   (Esc keyboard = batal)"
                      : "Ketuk baris lalu tekan tombol gamepad. Skill: pengubah A + A/B/X/Y = skill 1-4, pengubah B + A/B/X = skill 5-7. Menu: A pilih, B kembali, D-pad naik/turun." +
                        (PadInput.LastMsg != null ? "\n" + PadInput.LastMsg : "") + "\nGamepad " + (TownInput.HasPad ? "terdeteksi" : "tidak terdeteksi") + PadInput.KindLabel + "\nSneak bawaan = R3 (tekan stik kanan); ganti kamera tidak punya tombol bawaan, atur di sini.",
                () =>
                {
                    var l = new List<UIEntry>();
                    for (int i = 0; i < PadInput.ActLabels.Length; i++)
                    {
                        PadAct a = (PadAct)i;
                        string mark = PadInput.Target.HasValue && PadInput.Target.Value == a ? "..." : PadInput.Name(a);
                        l.Add(new UIEntry(PadInput.ActLabels[i] + ":  " + mark, () => PadInput.BeginCapture(a)));
                    }
                    l.Add(new UIEntry("Kembalikan tombol gamepad bawaan", () => PadInput.ResetAll()));
                    l.Add(new UIEntry("< Kembali", () => { if (back != null) back(); }));
                    return l;
                });
        }

        // ================= Tampilan & kenyamanan =================
        void ShowUiSettings(System.Action back)
        {
            var ui = TownUI.I;
            ui.Open("TAMPILAN & KENYAMANAN",
                () => "Skala UI " + Mathf.RoundToInt(UiSettings.Scale * 100f) + "%     Ukuran teks " + Mathf.RoundToInt(UiSettings.TextScale * 100f) + "%     Tema: " + UiSettings.ThemeNames[UiSettings.Theme] +
                      "\nHUD redup saat tenang: " + (UiSettings.HudFade ? "Ya" : "Tidak") + "     Animasi panel: " + (UiSettings.Anim ? "Ya" : "Tidak") +
                      "\nTema warna berlaku setelah pindah scene atau membuka ulang game.",
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Skala UI  -", () => UiSettings.StepScale(-1), UiSettings.Scale > 0.8f));
                    l.Add(new UIEntry("Skala UI  +", () => UiSettings.StepScale(1), UiSettings.Scale < 1.3f));
                    l.Add(new UIEntry("Ukuran teks HUD  -", () => UiSettings.StepText(-1), UiSettings.TextScale > 0.9f));
                    l.Add(new UIEntry("Ukuran teks HUD  +", () => UiSettings.StepText(1), UiSettings.TextScale < 1.2f));
                    l.Add(new UIEntry("Tema warna: " + UiSettings.ThemeNames[UiSettings.Theme] + "  (ketuk untuk ganti)", () => UiSettings.CycleTheme()));
                    l.Add(new UIEntry("HUD redup saat tenang: " + (UiSettings.HudFade ? "Ya" : "Tidak"), () => UiSettings.ToggleFade()));
                    l.Add(new UIEntry("Animasi panel: " + (UiSettings.Anim ? "Ya" : "Tidak"), () => UiSettings.ToggleAnim()));
                    l.Add(new UIEntry("Kembalikan ke bawaan", () => UiSettings.Reset()));
                    l.Add(new UIEntry("< Kembali", () => { if (back != null) back(); }));
                    return l;
                });
        }

        static float Vol(int ch) { var a = AudioManager.I; return a == null ? 0f : ch == 0 ? a.musicVol : ch == 1 ? a.sfxVol : a.voiceVol; }

        static void SetVol(int ch, float delta)
        {
            var a = AudioManager.I;
            if (a == null) return;
            float m = a.musicVol, s = a.sfxVol, v = a.voiceVol;
            float n = Mathf.Clamp01(Mathf.Round((Vol(ch) + delta) * 10f) / 10f);
            if (ch == 0) m = n; else if (ch == 1) s = n; else v = n;
            a.SetVolumes(m, s, v);
        }

        /// <summary>Dipanggil setelah karakter gugur (mati permanen): mulai Game Baru di slot yang sama.</summary>
        public static void NewGameAfterDeath()
        {
            SaveSystem.DeleteSave();
            CharacterData.ResetAll();
            SaveSystem.MarkReady();
            if (inst != null) inst.titleDone = true;
            if (TownUI.I != null) { TownUI.I.Close(); TownUI.I.Toast("Selamat datang, Hunter baru."); }
            StarterConfig.StartNewGame();
        }

        static void Quit()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
