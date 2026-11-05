using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Daftar interior bangunan. Nama scene = "Interior_" + id.</summary>
    public static class InteriorInfo
    {
        public const string Prefix = "Interior_";
        public static string Scene(string id) { return Prefix + id; }
        public static bool IsInterior(string scene) { return scene != null && scene.StartsWith(Prefix); }

        public static readonly string[,] Table =
        {
            { "Inn", "Penginapan", "Kamar & Aula" },
            { "Smithy", "Pandai Besi", "Bengkel Tempa" },
            { "Shop", "Toko Umum", "Perbekalan" },
            { "Academy", "Akademi & Perpustakaan", "Aula Pengetahuan" },
            { "Tavern", "Restoran", "Kedai & Panggung" },
            { "Cathedral", "Katedral Cahaya Suci", "Aula Doa" },
            { "Home", "Rumahku", "Kediaman Hunter" },
            { "PropOffice", "Kantor Properti", "Balai Properti" },
            { "GuildEldrath", "Guild Hunter Eldrath", "Balai Agung" },
        };

        public static string Title(string scene, out string sub)
        {
            for (int i = 0; i < Table.GetLength(0); i++)
                if (Scene(Table[i, 0]) == scene) { sub = Table[i, 2]; return Table[i, 1]; }
            sub = ""; return scene;
        }
    }

    /// <summary>Menyimpan dari mana pemain masuk supaya pintu keluar membawanya kembali ke tempat yang sama (Town atau Kota Eldrath).</summary>
    public static class InteriorRoute
    {
        public static string returnScene = GuildConfig.TownScene;
        public static Vector3 returnPos;
        public static float returnYaw;
        public static bool valid;
        public static readonly Vector3 EntryPos = new Vector3(0f, 1.1f, 2.6f);       // titik muncul dalam interior (di dalam pintu selatan, menghadap +Z)

        public static void Enter(string interiorId, Vector3 doorPos, float doorYaw)
        {
            returnScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            returnPos = doorPos + Vector3.up * 0f; returnPos.y = Mathf.Max(doorPos.y, 0f) + 1.1f;
            returnYaw = doorYaw;
            valid = true;
            Audio.Sfx("gate");
            SceneRouter.GoFade(InteriorInfo.Scene(interiorId), EntryPos, 0f);
        }

        public static string homeId = "";

        /// <summary>Masuk ke Balai Agung Guild Eldrath (titik muncul = di dalam pintu Guild).</summary>
        public static void EnterGuildEldrath(Vector3 doorPos, float doorYaw)
        {
            returnScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            returnPos = doorPos; returnPos.y = Mathf.Max(doorPos.y, 0f) + 1.1f;
            returnYaw = doorYaw;
            valid = true;
            Audio.Sfx("gate");
            SceneRouter.GoFade(InteriorInfo.Scene("GuildEldrath"), GuildConfig.InteriorEntryPos, 0f);
        }

        /// <summary>Masuk ke interior rumah milik pemain (satu scene Interior_Home untuk semua rumah; furnitur disesuaikan di HomeInterior).</summary>
        public static bool preview;       // true = tur rumah yang belum dibeli

        public static void EnterHome(string houseId, Vector3 doorPos, float doorYaw, bool previewTour = false)
        {
            homeId = houseId; preview = previewTour;
            Enter("Home", doorPos, doorYaw);
        }

        public static void Exit()
        {
            Audio.Sfx("gate");
            if (valid) SceneRouter.GoFade(returnScene, returnPos, returnYaw);
            else SceneRouter.GoToSpawn(GuildConfig.TownScene, "SpawnFromForest");       // dibuka langsung dari editor/save: kembali ke gerbang selatan
        }
    }

    /// <summary>Pintu bangunan di kota: tekan E untuk masuk ke scene interiornya.</summary>
    public class BuildingDoor : Interactable
    {
        public string interiorId = "Inn";

        public override void Interact()
        {
            if (!Application.CanStreamedLevelBeLoaded(InteriorInfo.Scene(interiorId)))
            {
                if (TownUI.I != null) TownUI.I.Toast("Scene '" + InteriorInfo.Scene(interiorId) + "' belum dibuat (menu Hunter Guild > Build Interiors).");
                return;
            }
            InteriorRoute.Enter(interiorId, transform.position, transform.eulerAngles.y);
        }
    }


}
