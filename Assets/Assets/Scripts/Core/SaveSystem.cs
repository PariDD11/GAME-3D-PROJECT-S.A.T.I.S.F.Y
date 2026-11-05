using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    [Serializable] public class SaveKV { public string k; public string s; public float f; public int i; }

    /// <summary>Isi file save (JSON). JsonUtility tidak mendukung Dictionary/null, jadi semuanya dipecah jadi list + flag.</summary>
    [Serializable]
    public class SaveData
    {
        public int version = 2;
        [NonSerialized] public int loadedVersion;      // versi asli file sebelum migrasi
        public int diff = 1, perma, playSec;            // v2: kesulitan, mati permanen, lama main (detik)
        public string charJson;                         // v2: kustomisasi karakter (dulu hanya di PlayerPrefs)
        public string savedAt, scene;
        public bool hasPos;
        public float px, py, pz, pyaw;
        // --- GameState
        public string job, rank, mealBuff, spawnPoint, evo, evo2;
        public int intel, wis, statPoints;
        public int level, exp, skillPoints, gold, canteen, expBuffQuests, day, questsDone, questProgress, dungeonBest, dungeonBossCleared, towerRuns;
        public bool jobChosen;
        public float hunger, thirst, hoursAwake, timeOfDay;
        public bool hasQuest; public QuestInfo quest;
        public List<SaveKV> inventory = new List<SaveKV>(), equipment = new List<SaveKV>(), durability = new List<SaveKV>(),
                            maxDurability = new List<SaveKV>(), attach = new List<SaveKV>(), storedDur = new List<SaveKV>(), storedMax = new List<SaveKV>(), stats = new List<SaveKV>();
        public List<string> flags = new List<string>(), skills = new List<string>();
        public string[] skillSlots = new string[7], quickbar = new string[6];
        // --- GuildData
        public int reputation, depositHeld, postersVersion, refreshCounter, noticeDay;
        public bool rankUpPending, exclusiveLocked, hasFavorite;
        public string lastGreetedRank;
        public float postersAt;
        public List<DropRec> drops = new List<DropRec>();      // item di tanah (drop monster & buangan pemain), tetap ada antar scene
        public List<SaveKV> rankCooldown = new List<SaveKV>(), hired = new List<SaveKV>(), partnerCooldown = new List<SaveKV>();
        public List<Partner> party = new List<Partner>(), posters = new List<Partner>();
        public List<bool> posterHas = new List<bool>();
        public Partner favorite;
        public List<Notice> notices = new List<Notice>();
        public List<GuildQuest> extraQuests = new List<GuildQuest>();
    }

    /// <summary>
    /// Save/load progres ke file JSON (persistentDataPath/hunterguild_save.json).
    /// Autosave hanya di tempat aman (kota, interior Guild, Kota Eldrath): saat masuk scene aman, tiap 60 detik, setelah tidur, dan saat keluar game.
    /// Save otomatis dimuat sekali saat game dibuka. Hapus save: menu Hunter Guild > Hapus Save, atau SaveSystem.DeleteSave().
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        const string FileName = "hunterguild_save.json";
        static SaveSystem inst;
        static bool loadedThisRun;
        float timer;

        // posisi terakhir pemain di luar ruangan (kota/desa/Eldrath). Dipakai Lanjutkan agar pemain muncul di tempat terakhir berdiri.
        static float resaveAt;
        static string snapScene; static Vector3 snapPos; static float snapYaw; static bool snapHas;

        static bool IsOutdoor(string scene)
        {
            return scene == GuildConfig.TownScene || scene == StarterConfig.VillageScene || scene == CityGate.DungeonCityScene;
        }

        static void Capture()
        {
            string sc = SceneManager.GetActiveScene().name;
            if (!IsOutdoor(sc)) return;
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return;
            snapScene = sc; snapPos = p.transform.position; snapYaw = p.transform.eulerAngles.y; snapHas = true;
        }

        // ---- slot save (1-3). Slot 1 memakai nama file lama supaya save lama tetap terbaca. ----
        public const int SlotCount = 3;
        public static int Slot { get { return Mathf.Clamp(PlayerPrefs.GetInt("hg_slot", 1), 1, SlotCount); } }
        public static string PathOf(int slot) { return Path.Combine(Application.persistentDataPath, slot <= 1 ? FileName : "hunterguild_save_" + slot + ".json"); }
        public static bool ExistsSlot(int slot) { return File.Exists(PathOf(slot)); }
        public static SaveData PeekSlot(int slot) { return ReadBest(PathOf(slot)); }
        /// <summary>Ganti slot aktif (hanya dari menu utama, sebelum Lanjutkan/Game Baru).</summary>
        public static void SetSlot(int slot) { PlayerPrefs.SetInt("hg_slot", Mathf.Clamp(slot, 1, SlotCount)); PlayerPrefs.Save(); CharacterData.Reload(); }

        public static string Path_ { get { return PathOf(Slot); } }
        public static bool Exists { get { return File.Exists(Path_); } }
        public static bool Ready { get { return loadedThisRun; } }
        /// <summary>Dipanggil menu utama setelah pemain memilih; mengaktifkan autosave.</summary>
        public static void MarkReady() { loadedThisRun = true; }
        /// <summary>Hentikan autosave (dipakai mati permanen).</summary>
        public static void Halt() { loadedThisRun = false; }
        public static SaveData Peek() { return ReadBest(Path_); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (inst != null) return;
            var go = new GameObject("SaveSystem");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<SaveSystem>();
            SceneManager.sceneLoaded += (s, m) => { if (IsOutdoor(s.name)) { snapHas = false; resaveAt = Time.unscaledTime + 1.5f; } if (loadedThisRun && IsSafeScene(s.name)) Save(); };
        }

        public static bool IsSafeScene(string scene)
        {
            return HuntManager.I == null && (scene == GuildConfig.TownScene || scene == StarterConfig.VillageScene || scene == GuildConfig.InteriorScene || scene == CityGate.DungeonCityScene || InteriorInfo.IsInterior(scene));
        }

        /// <summary>v35z102n20: checkpoint di area hunt/Menara. Menyimpan progres (item, EXP, quest, statistik) tetapi posisi tetap titik aman terakhir
        /// di kota, jadi Lanjutkan memunculkan pemain di kota dengan hasil hunt tersimpan. Tidak di Arena Ujian, tidak saat bertarung/pingsan.</summary>
        public static bool CanCheckpoint(string scene)
        {
            if (IsSafeScene(scene)) return true;
            var hm = HuntManager.I;
            return hm != null && hm.defaultZone != "Arena Ujian" && snapHas && !string.IsNullOrEmpty(snapScene);
        }
        public static bool InField(string scene) { return !IsSafeScene(scene) && CanCheckpoint(scene); }

        float fieldTimer, playAcc;
        static bool pending; static float pendingAt;
        /// <summary>Minta simpan sebentar lagi (setelah event penting: quest selesai, evolusi, hadiah). Dijalankan saat tenang dan di tempat yang boleh menyimpan.</summary>
        public static void RequestSave(float delay = 2f) { pending = true; pendingAt = Time.unscaledTime + delay; }
        void Update()
        {
            var gs = GameState.I;
            if (gs == null) return;
            if (!loadedThisRun) return;      // menu utama (MainMenu) yang memutuskan Lanjutkan / Game Baru; autosave baru aktif setelahnya
            Capture();
            playAcc += Time.unscaledDeltaTime; if (playAcc >= 1f) { int w = (int)playAcc; playAcc -= w; gs.AddStat("playsec", w); }
            if (pending && Time.unscaledTime >= pendingAt && CanCheckpoint(SceneManager.GetActiveScene().name) && !PlayerController.InFight() && !(PlayerCombat.I != null && PlayerCombat.I.IsDead))
            { pending = false; Save(); }
            if (resaveAt > 0f && Time.unscaledTime >= resaveAt) { resaveAt = 0f; if (snapHas && IsSafeScene(SceneManager.GetActiveScene().name)) Save(); }
            timer += Time.unscaledDeltaTime;
            if (timer >= 60f) { timer = 0f; if (IsSafeScene(SceneManager.GetActiveScene().name)) Save(); }
            // checkpoint otomatis di area hunt tiap 120 dtk, hanya saat tenang (tidak bertarung, tidak pingsan, tidak ada panel terbuka)
            if (InField(SceneManager.GetActiveScene().name))
            {
                fieldTimer += Time.unscaledDeltaTime;
                if (fieldTimer >= 120f && !PlayerController.InFight() && !(PlayerCombat.I != null && PlayerCombat.I.IsDead) && !(TownUI.I != null && TownUI.I.PanelOpen))
                { fieldTimer = 0f; if (Save() && TownUI.I != null) TownUI.I.Toast("Checkpoint tersimpan."); }
            }
            else fieldTimer = 0f;
        }

        void OnApplicationQuit() { if (loadedThisRun && CanCheckpoint(SceneManager.GetActiveScene().name) && !(PlayerCombat.I != null && PlayerCombat.I.IsDead)) Save(); }
        void OnApplicationPause(bool paused) { if (paused && loadedThisRun && CanCheckpoint(SceneManager.GetActiveScene().name) && !(PlayerCombat.I != null && PlayerCombat.I.IsDead)) Save(); }

        // ---------------- helpers ----------------
        static List<SaveKV> ToList(Dictionary<string, int> d) { var l = new List<SaveKV>(); foreach (var p in d) l.Add(new SaveKV { k = p.Key, i = p.Value }); return l; }
        static List<SaveKV> ToList(Dictionary<string, float> d) { var l = new List<SaveKV>(); foreach (var p in d) l.Add(new SaveKV { k = p.Key, f = p.Value }); return l; }
        static List<SaveKV> ToList(Dictionary<string, string> d) { var l = new List<SaveKV>(); foreach (var p in d) l.Add(new SaveKV { k = p.Key, s = p.Value }); return l; }
        static void Fill(Dictionary<string, int> d, List<SaveKV> l) { d.Clear(); if (l != null) foreach (var e in l) d[e.k] = e.i; }
        static void Fill(Dictionary<string, float> d, List<SaveKV> l) { d.Clear(); if (l != null) foreach (var e in l) d[e.k] = e.f; }
        static void Fill(Dictionary<string, string> d, List<SaveKV> l) { d.Clear(); if (l != null) foreach (var e in l) d[e.k] = e.s; }

        // ---------------- save ----------------
        public static bool Save()
        {
            var gs = GameState.I;
            if (gs == null) return false;
            try
            {
                var gd = GuildData.I;
                var d = new SaveData();
                if (gs.level < 0 || string.IsNullOrEmpty(gs.rank)) return false;       // keadaan belum siap: jangan menimpa simpanan yang baik
                d.version = CurrentVersion;
                d.diff = Difficulty.Level; d.perma = Difficulty.Permadeath ? 1 : 0; d.playSec = gs.Stat("playsec");
                try { d.charJson = JsonUtility.ToJson(CharacterData.Current); } catch (Exception) { d.charJson = ""; }
                d.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                string active = SceneManager.GetActiveScene().name;
                Capture();
                d.scene = active;
                if (IsOutdoor(active)) { if (snapHas && snapScene == active) { d.hasPos = true; d.px = snapPos.x; d.py = snapPos.y; d.pz = snapPos.z; d.pyaw = snapYaw; } }
                else if (snapHas && !string.IsNullOrEmpty(snapScene)) { d.scene = snapScene; d.hasPos = true; d.px = snapPos.x; d.py = snapPos.y; d.pz = snapPos.z; d.pyaw = snapYaw; }
                d.evo = gs.evo; d.evo2 = gs.evo2; d.job = gs.job; d.rank = gs.rank; d.mealBuff = gs.mealBuff; d.spawnPoint = gs.spawnPoint;
                d.intel = gs.intel; d.wis = gs.wis; d.statPoints = gs.statPoints;
                d.level = gs.level; d.exp = gs.exp; d.skillPoints = gs.skillPoints; d.gold = gs.gold; d.canteen = gs.canteen;
                d.expBuffQuests = gs.expBuffQuests; d.day = gs.day; d.questsDone = gs.questsDone; d.questProgress = gs.questProgress;
                d.dungeonBest = gs.dungeonBest; d.dungeonBossCleared = gs.dungeonBossCleared; d.towerRuns = gs.towerRuns;
                d.jobChosen = gs.jobChosen; d.hunger = gs.hunger; d.thirst = gs.thirst; d.hoursAwake = gs.hoursAwake; d.timeOfDay = gs.timeOfDay;
                d.hasQuest = gs.activeQuest != null; d.quest = gs.activeQuest;
                d.inventory = ToList(gs.inventory); d.equipment = ToList(gs.equipment); d.durability = ToList(gs.durability); d.attach = ToList(gs.attachments);
                d.maxDurability = ToList(gs.maxDurability); d.storedDur = ToList(gs.storedDur); d.storedMax = ToList(gs.storedMax); d.stats = ToList(gs.stats);
                foreach (var f in gs.flags) if (f != GameState.FlagKompas && f != GameState.FlagMinimap) d.flags.Add(f);
                d.skills.AddRange(gs.learnedSkills);
                Array.Copy(gs.skillSlots, d.skillSlots, 7); Array.Copy(gs.quickbar, d.quickbar, 6);

                d.reputation = gd.reputation; d.depositHeld = gd.depositHeld; d.postersVersion = gd.postersVersion;
                d.refreshCounter = gd.refreshCounter; d.noticeDay = gd.noticeDay; d.rankUpPending = gd.rankUpPending;
                d.exclusiveLocked = gd.exclusiveLocked; d.lastGreetedRank = gd.lastGreetedRank; d.postersAt = gd.postersAt;
                d.rankCooldown = ToList(gd.rankCooldown); d.hired = ToList(gd.hired); d.partnerCooldown = ToList(gd.partnerCooldown);
                d.party.AddRange(gd.party);
                d.hasFavorite = gd.favorite != null; d.favorite = gd.favorite ?? new Partner();
                foreach (var p in gd.posters) { d.posterHas.Add(p != null); d.posters.Add(p ?? new Partner()); }
                d.notices.AddRange(gd.notices); d.extraQuests.AddRange(gd.extraQuests);
                d.drops = WorldDrops.Snapshot();

                string json = JsonUtility.ToJson(d, true);
                string tmp = Path_ + ".tmp";
                File.WriteAllText(tmp, json);
                if (TryRead(tmp, true) == null) { Debug.LogWarning("[Save] hasil tulis tidak valid; simpanan lama dipertahankan."); File.Delete(tmp); return false; }
                if (File.Exists(Path_) && TryRead(Path_, true) != null)           // hanya cadangkan simpanan yang sehat (jangan menimpa .bak yang baik dengan file rusak)
                {
                    CopyF(Path_, Path_ + ".bak");                                  // cadangan terbaru (tiap simpan)
                    RotateBackups();                                               // cadangan bergilir (tiap ~10 menit)
                }
                Del(Path_ + ".crc");
                File.Copy(tmp, Path_, true);
                File.WriteAllText(Path_ + ".crc", Crc(json));
                File.Delete(tmp);
                return true;
            }
            catch (Exception e) { Debug.LogWarning("[Save] gagal: " + e.Message); return false; }
        }

        // ---------------- load ----------------
        public const int CurrentVersion = 2;

        /// <summary>Muat slot aktif. Urutan: simpanan utama, .bak, gen1..3 (yang lolos checksum dulu, baru yang tidak). Bila yang dipakai bukan simpanan utama, simpanan utama diperbaiki darinya.</summary>
        public static bool Load()
        {
            var gs = GameState.I;
            if (gs == null || !Exists && !HasAnyBackup()) return false;
            var cands = Candidates(Path_);
            for (int pass = 0; pass < 2; pass++)
                foreach (var c in cands)
                {
                    var d = TryRead(c, pass == 0);
                    if (d == null) continue;
                    if (!Apply(d)) continue;
                    if (c != Path_)
                    {
                        try { if (File.Exists(Path_)) File.Copy(Path_, Path_ + ".corrupt", true); CopyF(c, Path_); } catch (Exception) { }
                        if (TownUI.I != null) TownUI.I.Toast("Save utama rusak; dimuat dari cadangan (" + d.savedAt + ").");
                    }
                    else if (pass == 1 && TownUI.I != null) TownUI.I.Toast("Peringatan: checksum save tidak cocok, tapi datanya terbaca.");
                    else if (d.loadedVersion > CurrentVersion && TownUI.I != null) TownUI.I.Toast("Save dibuat versi game yang lebih baru; sebagian data mungkin diabaikan.");
                    return true;
                }
            Debug.LogWarning("[Save] semua berkas save/cadangan rusak, mulai baru.");
            return false;
        }

        static bool HasAnyBackup() { foreach (var c in Candidates(Path_)) if (File.Exists(c)) return true; return false; }

        static List<string> Candidates(string path)
        {
            var l = new List<string> { path, path + ".bak" };
            for (int g = 1; g <= Generations; g++) l.Add(Gen(path, g));
            return l;
        }

        static bool Apply(SaveData d)
        {
            var gs = GameState.I;
            try
            {
                if (d.loadedVersion < CurrentVersion && File.Exists(Path_))        // simpan salinan sebelum migrasi format, sekali per versi
                {
                    string pm = Path_ + ".v" + d.loadedVersion + ".premigrate";
                    if (!File.Exists(pm)) File.Copy(Path_, pm, false);
                }
                var gd = GuildData.I;
                gs.evo = d.evo ?? ""; gs.evo2 = d.evo2 ?? ""; gs.job = d.job; gs.rank = d.rank; gs.mealBuff = d.mealBuff ?? ""; gs.spawnPoint = d.spawnPoint ?? "Inn";
                gs.intel = d.intel; gs.wis = d.wis; gs.statPoints = d.statPoints;
                gs.level = d.level; gs.exp = d.exp; gs.skillPoints = d.skillPoints; gs.gold = d.gold; gs.canteen = d.canteen;
                gs.expBuffQuests = d.expBuffQuests; gs.day = d.day; gs.questsDone = d.questsDone; gs.questProgress = d.questProgress;
                gs.dungeonBest = d.dungeonBest; gs.dungeonBossCleared = d.dungeonBossCleared; gs.towerRuns = d.towerRuns;
                gs.jobChosen = d.jobChosen; gs.hunger = d.hunger; gs.thirst = d.thirst; gs.hoursAwake = d.hoursAwake; gs.timeOfDay = d.timeOfDay;
                gs.activeQuest = d.hasQuest ? d.quest : null;
                Fill(gs.inventory, d.inventory); Fill(gs.equipment, d.equipment); Fill(gs.durability, d.durability); Fill(gs.attachments, d.attach);
                Fill(gs.maxDurability, d.maxDurability); Fill(gs.storedDur, d.storedDur); Fill(gs.storedMax, d.storedMax); Fill(gs.stats, d.stats);
                gs.flags.Clear(); foreach (var f in d.flags) gs.flags.Add(f);
                gs.learnedSkills.Clear(); foreach (var sk in d.skills) gs.learnedSkills.Add(sk);
                for (int i = 0; i < 7; i++) gs.skillSlots[i] = d.skillSlots != null && i < d.skillSlots.Length && !string.IsNullOrEmpty(d.skillSlots[i]) ? d.skillSlots[i] : null;
                for (int i = 0; i < 6; i++) gs.quickbar[i] = d.quickbar != null && i < d.quickbar.Length && !string.IsNullOrEmpty(d.quickbar[i]) ? d.quickbar[i] : null;

                gd.reputation = d.reputation; gd.depositHeld = d.depositHeld; gd.postersVersion = d.postersVersion + 1;
                gd.refreshCounter = d.refreshCounter; gd.noticeDay = d.noticeDay; gd.rankUpPending = d.rankUpPending;
                gd.exclusiveLocked = d.exclusiveLocked; gd.lastGreetedRank = d.lastGreetedRank ?? ""; gd.postersAt = d.postersAt;
                Fill(gd.rankCooldown, d.rankCooldown); Fill(gd.hired, d.hired); Fill(gd.partnerCooldown, d.partnerCooldown);
                gd.party.Clear(); gd.party.AddRange(d.party);
                gd.favorite = d.hasFavorite ? d.favorite : null;
                for (int i = 0; i < gd.posters.Length; i++)
                    gd.posters[i] = i < d.posters.Count && i < d.posterHas.Count && d.posterHas[i] ? d.posters[i] : null;
                gd.notices.Clear(); gd.notices.AddRange(d.notices);
                gd.extraQuests.Clear(); gd.extraQuests.AddRange(d.extraQuests);
                WorldDrops.Restore(d.drops);
                if (d.loadedVersion >= 2)
                {
                    try
                    {
                        PlayerPrefs.SetInt("hg_diff_" + Slot, Mathf.Clamp(d.diff, 0, 3));
                        if (d.perma == 1) PlayerPrefs.SetInt("hg_perma_" + Slot, 1);
                        if (!string.IsNullOrEmpty(d.charJson)) { var cd = JsonUtility.FromJson<CharacterData>(d.charJson); if (cd != null) { cd.Save(); } }
                        PlayerPrefs.Save();
                    }
                    catch (Exception) { }
                }
                loadedThisRun = true;
                if (TownUI.I != null) TownUI.I.Toast("Progres dimuat (" + d.savedAt + ").");
                return true;
            }
            catch (Exception e) { Debug.LogWarning("[Save] gagal memuat: " + e.Message); return false; }
        }

        // ---------------- checksum & salin ----------------
        static string Crc(string text)
        {
            unchecked { uint h = 2166136261; foreach (char ch in text) { h ^= ch; h *= 16777619; } return h.ToString("x8") + text.Length; }
        }
        static bool CrcOk(string path, string text)
        {
            try { string c = path + ".crc"; return !File.Exists(c) || File.ReadAllText(c).Trim() == Crc(text); }   // tanpa .crc = save lama (n31 ke bawah), dianggap sah
            catch (Exception) { return true; }
        }
        static void CopyF(string src, string dst)
        {
            File.Copy(src, dst, true);
            if (File.Exists(src + ".crc")) File.Copy(src + ".crc", dst + ".crc", true); else if (File.Exists(dst + ".crc")) File.Delete(dst + ".crc");
        }

        /// <summary>Migrasi format: dipanggil setiap save dibaca. JsonUtility mengisi field baru dengan nilai awal, jadi di sini hanya perbaikan nilai.</summary>
        static void Migrate(SaveData d)
        {
            d.loadedVersion = d.version;
            if (d.version < 2) { d.diff = 1; d.perma = 0; d.charJson = ""; }
            if (d.version < CurrentVersion) d.version = CurrentVersion;
            // pembersihan nilai (save yang rusak sebagian / hasil edit)
            if (d.level < 0) d.level = 0; if (d.exp < 0) d.exp = 0; if (d.gold < 0) d.gold = 0; if (d.day < 1) d.day = 1;
            if (d.skillPoints < 0) d.skillPoints = 0; if (d.statPoints < 0) d.statPoints = 0;
            if (float.IsNaN(d.hunger) || float.IsInfinity(d.hunger)) d.hunger = 0f;
            if (float.IsNaN(d.thirst) || float.IsInfinity(d.thirst)) d.thirst = 0f;
            if (float.IsNaN(d.timeOfDay) || float.IsInfinity(d.timeOfDay)) d.timeOfDay = 8f;
            if (float.IsNaN(d.hoursAwake) || float.IsInfinity(d.hoursAwake)) d.hoursAwake = 0f;
            if (d.hasPos && (float.IsNaN(d.px + d.py + d.pz) || float.IsInfinity(d.px + d.py + d.pz))) d.hasPos = false;
            if (d.inventory != null) d.inventory.RemoveAll(e => e == null || string.IsNullOrEmpty(e.k) || e.i < 0);
            if (d.flags != null) d.flags.RemoveAll(string.IsNullOrEmpty);
        }

        // ---------------- cadangan bergilir (v35z102n14) ----------------
        public const int Generations = 3;
        static float lastRotate = -9999f;
        static string Gen(string path, int g) { return path + ".gen" + g; }

        /// <summary>Geser .gen1 -> .gen2 -> .gen3 lalu salin simpanan yang sedang ada ke .gen1, paling cepat tiap 10 menit.</summary>
        static void RotateBackups()
        {
            try
            {
                if (Time.realtimeSinceStartup - lastRotate < 600f && File.Exists(Gen(Path_, 1))) return;
                lastRotate = Time.realtimeSinceStartup;
                if (TryRead(Path_, true) == null) return;                                // jangan menggilir simpanan rusak
                for (int g = Generations; g >= 2; g--) if (File.Exists(Gen(Path_, g - 1))) CopyF(Gen(Path_, g - 1), Gen(Path_, g));
                CopyF(Path_, Gen(Path_, 1));
            }
            catch (Exception) { }
        }

        /// <summary>Baca simpanan terbaik: utama, .bak, gen1..3 (yang lolos checksum dulu).</summary>
        static SaveData ReadBest(string path)
        {
            for (int pass = 0; pass < 2; pass++)
                foreach (var c in Candidates(path)) { var d = TryRead(c, pass == 0); if (d != null) return d; }
            return null;
        }

        /// <summary>Daftar cadangan slot aktif yang bisa dibaca: (nama, data), dan path-nya.</summary>
        public static List<KeyValuePair<string, SaveData>> ListBackups(out List<string> paths)
        {
            var res = new List<KeyValuePair<string, SaveData>>(); paths = new List<string>();
            string p = Path_;
            var cand = new List<string>(); var names = new List<string>();
            cand.Add(p + ".bak"); names.Add("Cadangan terbaru");
            for (int g = 1; g <= Generations; g++) { cand.Add(Gen(p, g)); names.Add(g == 1 ? "Cadangan bergilir 1 (terbaru)" : g == Generations ? "Cadangan bergilir " + g + " (tertua)" : "Cadangan bergilir " + g); }
            cand.Add(p + ".trash"); names.Add("Simpanan yang ditimpa Game Baru");
            cand.Add(p + ".corrupt"); names.Add("Simpanan utama yang tadinya rusak");
            for (int v = 1; v < CurrentVersion; v++) { cand.Add(p + ".v" + v + ".premigrate"); names.Add("Sebelum pembaruan format save v" + v); }
            for (int i = 0; i < cand.Count; i++) { var d = TryRead(cand[i], false); if (d != null) { res.Add(new KeyValuePair<string, SaveData>(names[i], d)); paths.Add(cand[i]); } }
            return res;
        }

        /// <summary>Jadikan file cadangan sebagai simpanan utama slot aktif.</summary>
        public static bool RestoreBackup(string backupPath)
        {
            try
            {
                if (TryRead(backupPath, false) == null) return false;
                if (File.Exists(Path_) && TryRead(Path_, false) != null) CopyF(Path_, Path_ + ".trash");       // yang tertimpa tetap bisa dipulihkan
                CopyF(backupPath, Path_); return true;
            }
            catch (Exception) { return false; }
        }

        static SaveData TryRead(string path, bool checkCrc)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string text = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(text)) return null;
                if (checkCrc && !CrcOk(path, text)) return null;
                var d = JsonUtility.FromJson<SaveData>(text);
                if (d == null || d.version < 1 || string.IsNullOrEmpty(d.savedAt) || d.inventory == null || d.stats == null) return null;
                Migrate(d);
                return d;
            }
            catch (Exception) { return null; }
        }

        // ---------------- kelola slot ----------------
        static bool PermaOf(int slot) { try { return PlayerPrefs.GetInt("hg_perma_" + slot, 0) == 1; } catch (Exception) { return false; } }
        static string PrefDiff(int slot) { return "hg_diff_" + slot; }

        /// <summary>Hapus semua berkas slot. keepTrash: simpanan utama disalin ke .trash dulu (bisa dipulihkan lewat menu Cadangan). Mati permanen selalu hapus total.</summary>
        public static void DeleteSlot(int slot, bool keepTrash)
        {
            string p = PathOf(slot);
            try
            {
                if (keepTrash && !PermaOf(slot) && File.Exists(p) && TryRead(p, false) != null) CopyF(p, p + ".trash");
                else { Del(p + ".trash"); Del(p + ".trash.crc"); }
                foreach (var c in Candidates(p)) { Del(c); Del(c + ".crc"); }
                Del(p + ".corrupt"); Del(p + ".corrupt.crc");
                for (int v = 1; v < CurrentVersion; v++) Del(p + ".v" + v + ".premigrate");
            }
            catch (Exception) { }
        }
        static void Del(string f) { try { if (File.Exists(f)) File.Delete(f); } catch (Exception) { } }

        /// <summary>Salin slot a ke slot b (menimpa b; b lama masuk .trash). Ditolak untuk slot mati permanen.</summary>
        public static bool CopySlot(int a, int b)
        {
            try
            {
                if (a == b || PermaOf(a) || PermaOf(b)) return false;
                var d = ReadBest(PathOf(a)); if (d == null) return false;
                string pa = PathOf(a), pb = PathOf(b), src = null;
                foreach (var c in Candidates(pa)) if (TryRead(c, true) != null) { src = c; break; }
                if (src == null) foreach (var c in Candidates(pa)) if (TryRead(c, false) != null) { src = c; break; }
                if (src == null) return false;
                DeleteSlot(b, true);
                CopyF(src, pb);
                if (!string.IsNullOrEmpty(d.charJson)) { PlayerPrefs.SetString(b <= 1 ? "HG_Character_v1" : "HG_Character_v1_s" + b, d.charJson); }
                PlayerPrefs.SetInt(PrefDiff(b), Mathf.Clamp(d.diff, 0, 3)); PlayerPrefs.Save();
                CharacterData.Reload();
                return true;
            }
            catch (Exception) { return false; }
        }

        public static void DeleteSave() { DeleteSave(false); }
        public static void DeleteSave(bool keepTrash) { WorldDrops.Clear(); DeleteSlot(Slot, keepTrash); }
    }
}
