using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n14: perjalanan cepat antar kota/desa yang sudah pernah dikunjungi (flag "visited_scene"). Dibuka dari menu jeda,
    /// hanya di area aman (kota/desa/interior), tidak saat bertarung atau saat berburu. Gratis; hanya memindahkan pemain.
    /// </summary>
    public class FastTravel : MonoBehaviour
    {
        public class Dest { public string scene, name, spawn; public Vector3 pos; public float yaw; }

        public static readonly Dest[] All =
        {
            new Dest { scene = StarterConfig.VillageScene, name = "Desa Pemula", spawn = StarterConfig.VillageSpawn },
            new Dest { scene = GuildConfig.TownScene, name = "Ashwatch (depan Guild)", pos = GuildConfig.TownReturnPos, yaw = 180f },
            new Dest { scene = CityGate.DungeonCityScene, name = "Kota Menara", spawn = "SpawnFromTown" },
        };

        static string Flag(string scene) { return "visited_" + scene; }
        public static bool Visited(string scene) { var gs = GameState.I; return gs != null && gs.flags.Contains(Flag(scene)); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var go = new GameObject("FastTravel"); DontDestroyOnLoad(go); go.AddComponent<FastTravel>();
        }

        float t;
        void Update()
        {
            t -= Time.unscaledDeltaTime;
            if (t > 0f) return;
            t = 2f;
            var gs = GameState.I; if (gs == null) return;
            string sc = SceneManager.GetActiveScene().name;
            foreach (var d in All) if (d.scene == sc) gs.flags.Add(Flag(sc));
            if (IsHuntScene(sc)) gs.flags.Add(Flag(sc));
        }

        /// <summary>Area hunt biasa (bukan Menara, Arena Ujian, atau scene tutorial).</summary>
        public static bool IsHuntScene(string sc) { return sc != null && sc.StartsWith("HuntArea_") && !sc.Contains("Tutorial"); }

        /// <summary>Sedang di luar kota (area hunt / Menara) dan boleh pulang cepat. Arena Ujian tidak boleh ditinggalkan.</summary>
        public static bool CanReturnFromField()
        {
            var hm = HuntManager.I;
            return hm != null && hm.defaultZone != "Arena Ujian";
        }

        /// <summary>Alasan perjalanan cepat tidak bisa dipakai sekarang; null bila bisa.</summary>
        public static string Blocked()
        {
            string sc = SceneManager.GetActiveScene().name;
            if (!SaveSystem.IsSafeScene(sc) && !CanReturnFromField()) return "Perjalanan cepat tidak bisa dipakai di sini (Arena Ujian atau area khusus).";
            if (PlayerController.InFight()) return "Tidak bisa saat bertarung.";
            if (PlayerCombat.I != null && PlayerCombat.I.IsDead) return "Tidak bisa saat pingsan.";
            return null;
        }

        public static void Go(Dest d)
        {
            if (!Application.CanStreamedLevelBeLoaded(d.scene)) { if (TownUI.I != null) TownUI.I.Toast("Area ini belum ada di Build Settings."); return; }
            Time.timeScale = 1f;
            if (!string.IsNullOrEmpty(d.spawn)) SceneRouter.GoToSpawn(d.scene, d.spawn);
            else SceneRouter.GoFade(d.scene, d.pos, d.yaw);
        }

        public static void Open(System.Action back)
        {
            var ui = TownUI.I;
            ui.Open("PERJALANAN CEPAT",
                () => Blocked() ?? "Pilih tujuan. Hanya area yang pernah kamu kunjungi.",
                () =>
                {
                    var l = new List<UIEntry>();
                    string cur = SceneManager.GetActiveScene().name;
                    bool ok = Blocked() == null;
                    if (CanReturnFromField() && ok) { ok = true; }
                    if (CanReturnFromField())
                    {
                        var hm = HuntManager.I;
                        l.Add(new UIEntry("Pulang ke kota (quest tetap berjalan, progres hunt tidak tersimpan)", () => { Time.timeScale = 1f; hm.ReturnToTown(); }, ok));
                        if (cur == "HuntArea_Frontier")
                            l.Add(new UIEntry("Pulang ke Ashwatch (jalan raya Perbatasan)", () => { Time.timeScale = 1f; if (GameState.I != null) GameState.I.EndExpedition(); SceneRouter.GoToSpawn(GuildConfig.TownScene, "SpawnFromDungeon"); }, ok));
                    }
                    else
                    {
                        // area hunt yang pernah dikunjungi: hanya area milik quest aktif, lewat aturan gerbang yang sama (party, scene tersedia)
                        var gsq = GameState.I; string msg = null, hs = null;
                        bool canHunt = gsq != null && CityGate.CanLeave("", out msg, out hs) && IsHuntScene(hs);
                        hs = canHunt ? hs : null;
                        if (canHunt)
                        {
                            string hsc = hs; bool vis2 = Visited(hsc);
                            l.Add(new UIEntry("Area quest: " + CityGate.AreaName(hsc) + (vis2 ? "" : "  (belum dikunjungi)"),
                                () => { Time.timeScale = 1f; SceneRouter.GoToSpawn(hsc, "SpawnFromTown"); }, ok && vis2));
                        }
                        else l.Add(new UIEntry("Area quest (ambil quest hunt dulu)", () => { }, false));
                    }
                    foreach (var d0 in All)
                    {
                        var d = d0;
                        bool vis = Visited(d.scene);
                        string label = d.name + (d.scene == cur ? "  (di sini)" : vis ? "" : "  (belum dikunjungi)");
                        l.Add(new UIEntry(label, () => Go(d), ok && vis && d.scene != cur && SaveSystem.IsSafeScene(cur)));
                    }
                    // v35z102n23: checkpoint Menara (lantai tepat setelah boss yang sudah dikalahkan); aturan masuk sama dengan pintu Menara
                    var gsT = GameState.I;
                    if (gsT != null && Visited(CityGate.DungeonCityScene))
                        foreach (int f0 in TowerDB.StartFloors(gsT.dungeonBossCleared))
                        {
                            if (f0 <= 1) continue;
                            int fl = f0;
                            l.Add(new UIEntry("Menara Eldrath: Lantai " + fl + " (checkpoint)", () => { Time.timeScale = 1f; DungeonEntrance.Enter(fl); }, ok && SaveSystem.IsSafeScene(cur)));
                        }
                    l.Add(new UIEntry("< Kembali", () => { if (back != null) back(); }));
                    return l;
                });
        }
    }
}
