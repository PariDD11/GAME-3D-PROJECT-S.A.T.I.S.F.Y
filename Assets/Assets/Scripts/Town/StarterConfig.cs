using UnityEngine;

namespace HunterGuild
{
    /// <summary>Alur awal game: Hunter baru mulai di Desa Pemula (Level 0), berlatih di Hutan Pemula sampai Level 5, lalu ke Ashwatch.</summary>
    public static class StarterConfig
    {
        public const string VillageScene = "StartVillage";
        public const string TutorialScene = "HuntArea_Tutorial";
        public const string CityScene = "Town";
        public const int CityMinLevel = 5;          // level minimum untuk melewati gerbang ke Ashwatch
        public const string VillageSpawn = "SpawnStart";

        public const int JobLevel = 5;              // level memilih job; sebelum itu pemain bertarung dengan tangan kosong
        public static bool Unarmed { get { return GameState.I != null && (GameState.I.level < JobLevel || GameState.I.NoJob || (GameState.I.requireJobChoice && !GameState.I.jobChosen)); } }

        static bool Can(string scene) { return Application.CanStreamedLevelBeLoaded(scene); }

        public static void StartNewGame()
        {
            if (Can(VillageScene)) SceneRouter.GoToSpawn(VillageScene, VillageSpawn);
            else if (TownUI.I != null) TownUI.I.Toast("Scene 'StartVillage' belum dibuat (menu Hunter Guild > Build Desa Pemula). Memulai di Ashwatch.");
        }

        public static void ContinueGame(string savedScene)
        {
            if (savedScene == VillageScene && Can(VillageScene)) SceneRouter.GoToSpawn(VillageScene, VillageSpawn);
        }

        /// <summary>Lanjutkan: kembali ke scene dan posisi terakhir pemain berdiri (bila tersimpan).</summary>
        public static void ContinueGame(SaveData sd)
        {
            if (sd == null) return;
            string sc = sd.scene;
            // v35z102n36: Hutan Pemula menyatu dengan Desa Pemula. Save lama di scene hutan dimuat di gerbang utara desa.
            if (sc == TutorialScene && !Can(sc) && Can(VillageScene)) { SceneRouter.GoToSpawn(VillageScene, "SpawnFromForest"); return; }
            if (string.IsNullOrEmpty(sc) || !Can(sc)) return;
            if (sd.hasPos) SceneRouter.GoFade(sc, new Vector3(sd.px, sd.py + 0.25f, sd.pz), sd.pyaw);
            else ContinueGame(sc);
        }
    }
}
