using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// Boss rahasia opsional, 1 per area hunt (7 area). Alur: temukan Prasasti Tua (petunjuk arah) -> temukan Altar -> panggil bosnya
    /// -> kalahkan dengan pola serangan khusus -> gear unik (kill pertama). Posisi prasasti/altar dipilih acak sekali lalu disimpan di save.
    /// </summary>
    public static class HiddenBosses
    {
        public class Def
        {
            public string key, scene, region, name, shape, hint, gearName, slot, vis, e1, e2;
            public float hp, atk, speed, size, height, radius, reach, v1, v2; public bool ranged; public int def, tier; public Color color; public string loot1, loot2;
            public string Id { get { return "hb_" + key; } }
            public string GearId { get { return "hbg_" + key; } }
        }

        public static readonly Def[] Defs =
        {
            new Def { key = "lerai", scene = "HuntArea_G", region = "Hutan Lerai", name = "Salju, Serigala Putih Lerai", shape = "wolf", hp = 4200, atk = 30, speed = 6.4f, size = 2.4f, height = 2.3f, radius = 1.7f, reach = 5f, color = new Color(0.92f, 0.94f, 1f), loot1 = "wolf_fang", loot2 = "wolf_pelt",
                hint = "Pemburu tua menulis: 'Serigala berbulu salju hanya menampakkan diri pada mereka yang sudah menumpahkan banyak darah hutan ini. Altarnya ada di arah {dir}, sekitar {dist} langkah dari batu ini.'",
                gearName = "Zirah Bulu Salju", slot = "Body", vis = "eld_body_0", def = 22, tier = 3, e1 = "stamina", v1 = 25f, e2 = "lifesteal", v2 = 2f },
            new Def { key = "frontier", scene = "HuntArea_Frontier", region = "Hutan Perbatasan", name = "Morrak, Troll Lumut Tua", shape = "troll", hp = 9000, atk = 52, speed = 3.8f, size = 2.5f, height = 3.6f, radius = 1.9f, reach = 5.2f, color = new Color(0.25f, 0.45f, 0.22f), loot1 = "troll_hide", loot2 = "ogre_tusk",
                hint = "Ukiran di batu: 'Jalan raya ini dibangun di atas kuburan seorang penjaga. Ia bangun bila altarnya disentuh. Cari ke arah {dir}, sekitar {dist} langkah.'",
                gearName = "Helm Lumut Penjaga", slot = "Head", vis = "eld_head_0", def = 16, tier = 3, e1 = "lifesteal", v1 = 3f, e2 = "stamina", v2 = 20f },
            new Def { key = "desert", scene = "HuntArea_Desert", region = "Gurun Zahar", name = "Sethra, Kalajengking Pasir Abadi", shape = "scorpion", hp = 11500, atk = 60, speed = 5.2f, size = 2.9f, height = 2.0f, radius = 1.8f, reach = 5.4f, color = new Color(0.85f, 0.7f, 0.3f), loot1 = "scorpion_stinger", loot2 = "golem_core",
                hint = "Tulisan di pecahan tembikar: 'Pasir menyimpan satu altar yang tak pernah tertimbun badai. Arah {dir}, sekitar {dist} langkah. Bawa ramuan, makhluknya berbisa.'",
                gearName = "Sepatu Pasir Abadi", slot = "Legs", vis = "eld_legs_1", def = 17, tier = 4, e1 = "stamina", v1 = 35f, e2 = "crit", v2 = 3f },
            new Def { key = "swamp", scene = "HuntArea_Swamp", region = "Rawa Terkutuk", name = "Nenek Mirah, Penyihir Rawa", shape = "witch", hp = 13500, atk = 66, speed = 4.2f, size = 2.2f, height = 1.9f, radius = 0.6f, reach = 18f, ranged = true, color = new Color(0.3f, 0.5f, 0.35f), loot1 = "hex_cloth", loot2 = "ectoplasm",
                hint = "Coretan di papan lapuk: 'Nenek Mirah tak pernah mati, hanya menunggu dipanggil. Altarnya di arah {dir}, sekitar {dist} langkah. Jangan berdiri di genangan.'",
                gearName = "Sarung Tangan Rawa", slot = "Hands", vis = "leather_gloves", def = 15, tier = 4, e1 = "crit", v1 = 6f, e2 = "cdr", v2 = 4f },
            new Def { key = "volcano", scene = "HuntArea_Volcano", region = "Gunung Vulkanik", name = "Pyrrhus, Golem Magma Purba", shape = "golem", hp = 19000, atk = 82, speed = 3.4f, size = 2.9f, height = 4.2f, radius = 2.0f, reach = 5.8f, color = new Color(0.9f, 0.35f, 0.1f), loot1 = "magma_core", loot2 = "ember_scale",
                hint = "Terukir di batu hitam: 'Inti gunung ini adalah makhluk, bukan lava. Altar yang membangunkannya ada di arah {dir}, sekitar {dist} langkah.'",
                gearName = "Zirah Inti Magma", slot = "Body", vis = "eld_body_1", def = 33, tier = 5, e1 = "lifesteal", v1 = 4f, e2 = "crit", v2 = 5f },
            new Def { key = "bone", scene = "HuntArea_Bone", region = "Padang Tulang", name = "Ksatria Tanpa Nama", shape = "skeleton", hp = 23000, atk = 94, speed = 5.0f, size = 2.7f, height = 1.9f, radius = 0.6f, reach = 4.8f, color = new Color(0.7f, 0.75f, 0.85f), loot1 = "ancient_bone", loot2 = "king_crown_shard",
                hint = "Di antara tulang ada sehelai pita: 'Ia bersumpah menjaga sebuah nama yang dilupakan. Datangi altarnya di arah {dir}, sekitar {dist} langkah, bila kau berani.'",
                gearName = "Helm Sumpah Tanpa Nama", slot = "Head", vis = "eld_head_2", def = 20, tier = 6, e1 = "crit", v1 = 7f, e2 = "lifesteal", v2 = 3f },
            new Def { key = "demon", scene = "HuntArea_Demon", region = "Lembah Iblis", name = "Gema, Penjaga Nama", shape = "necro", hp = 31000, atk = 112, speed = 4.6f, size = 2.8f, height = 3.4f, radius = 1.6f, reach = 7f, color = new Color(0.35f, 0.1f, 0.5f), loot1 = "demon_heart", loot2 = "necro_core",
                hint = "Bisikan di dinding batu: 'Nama yang dilarang masih bergema di lembah ini. Gema menunggu di altar, arah {dir}, sekitar {dist} langkah dari sini.'",
                gearName = "Jubah Gema Terlarang", slot = "Body", vis = "eld_body_3", def = 42, tier = 7, e1 = "crit", v1 = 9f, e2 = "lifesteal", v2 = 5f },
        };

        public static Def ById(string monsterId) { foreach (var d in Defs) if (d.Id == monsterId) return d; return null; }
        public static Def ForScene(string scene) { foreach (var d in Defs) if (d.scene == scene) return d; return null; }
        public static bool Defeated(Def d) { return GameState.I != null && GameState.I.Stat("hbk_" + d.key) > 0; }
        public static bool HintSeen(Def d) { return GameState.I != null && GameState.I.Stat("hbh_" + d.key) > 0; }
        public static int DefeatedCount() { int n = 0; foreach (var d in Defs) if (Defeated(d)) n++; return n; }

        /// <summary>Dipanggil dari ItemDB.</summary>
        public static void Register()
        {
            foreach (var d in Defs)
            {
                ItemDB.AddRaw(new ItemDef { id = d.GearId, name = d.gearName, type = ItemType.Gear, slot = d.slot, job = "Any", atk = 0, def = d.def, mana = 0, tier = d.tier, stack = 1,
                    effect = d.e1, effVal = d.v1, effect2 = d.e2, effVal2 = d.v2, sellPrice = d.def * 8 });
                GearTiers.SetVisual(d.GearId, d.vis);
            }
        }

        /// <summary>Dipanggil dari MonsterDB.</summary>
        public static void RegisterMonsters(System.Action<MonsterDef> add)
        {
            foreach (var d in Defs)
                add(new MonsterDef { id = d.Id, name = d.name, shape = d.shape, large = true, hp = d.hp, atk = d.atk, speed = d.speed, aggro = 60, telegraph = 1.1f, recover = 1.3f,
                    reach = d.reach, radius = d.radius * d.size / 1.7f, height = d.height, size = d.size, color = d.color, loot = new[] { d.loot1, d.loot2, "monster_bone" }, rare = d.loot2, rareChance = 0.5f, carves = 3, ranged = d.ranged });
        }

        /// <summary>Dipanggil saat monster mati (Monster.Die).</summary>
        public static void OnKill(string monsterId)
        {
            var d = ById(monsterId); var gs = GameState.I; if (d == null || gs == null) return;
            bool first = gs.Stat("hbk_" + d.key) == 0;
            gs.AddStat("hbk_" + d.key);
            if (!first) return;
            if (gs.HasSpace(d.GearId, 1)) { gs.AddItem(d.GearId, 1); if (TownUI.I != null) TownUI.I.Toast("BOSS RAHASIA TUMBANG! Hadiah unik: " + d.gearName); }
            else { gs.stats["hbk_" + d.key] = 0; if (TownUI.I != null) TownUI.I.Toast("Tas penuh! Hadiah unik tidak terbawa. Kosongkan tas lalu panggil lagi."); return; }
            Audio.Sfx("victory");
        }

        static readonly string[] Compass = { "utara", "timur laut", "timur", "tenggara", "selatan", "barat daya", "barat", "barat laut" };
        public static string Dir(Vector3 from, Vector3 to)
        {
            float a = Mathf.Atan2(to.x - from.x, to.z - from.z) * Mathf.Rad2Deg; if (a < 0) a += 360f;
            return Compass[Mathf.RoundToInt(a / 45f) % 8];
        }
    }

    public class HiddenBossSpawner : MonoBehaviour
    {
        float born; bool done;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { SceneManager.sceneLoaded -= OnLoaded; SceneManager.sceneLoaded += OnLoaded; Install(SceneManager.GetActiveScene()); }
        static void OnLoaded(Scene s, LoadSceneMode m) { Install(s); }
        static void Install(Scene s)
        {
            if (HiddenBosses.ForScene(s.name) == null || Object.FindFirstObjectByType<HiddenBossSpawner>() != null) return;
            new GameObject("HiddenBossSpawner").AddComponent<HiddenBossSpawner>();
        }
        void Start() { born = Time.time; }

        static bool Pick(Vector3 center, System.Random rng, float rMin, float rMax, out Vector3 g)
        {
            g = center;
            for (int a = 0; a < 400; a++)
            {
                double ang = rng.NextDouble() * 6.2832, rad = rMin + rng.NextDouble() * (rMax - rMin);
                Vector3 q = center + new Vector3((float)(System.Math.Cos(ang) * rad), 0f, (float)(System.Math.Sin(ang) * rad));
                if (!FarmAnimals.Ground(q, out g)) continue;
                float sy; if (WaterZone.Query(new Vector3(g.x, g.y, g.z), out sy) && sy > g.y - 0.3f) continue;
                return true;
            }
            return false;
        }

        static bool Stored(HiddenBosses.Def d, string w, out Vector3 g)
        {
            g = Vector3.zero; var gs = GameState.I; if (gs.Stat("hbp_" + d.key + w) == 0) return false;
            Vector3 q = new Vector3(gs.Stat("hbx_" + d.key + w) - 5000, 0f, gs.Stat("hbz_" + d.key + w) - 5000);
            return FarmAnimals.Ground(q, out g);
        }
        static void Save(HiddenBosses.Def d, string w, Vector3 g)
        {
            var gs = GameState.I; gs.stats["hbp_" + d.key + w] = 1;
            gs.stats["hbx_" + d.key + w] = Mathf.RoundToInt(g.x) + 5000; gs.stats["hbz_" + d.key + w] = Mathf.RoundToInt(g.z) + 5000;
        }

        void Update()
        {
            if (done || GameState.I == null) return;
            if (Time.time - born < 5f) return;
            var p = GameObject.FindGameObjectWithTag("Player"); if (p == null) return;
            done = true;
            var d = HiddenBosses.ForScene(SceneManager.GetActiveScene().name); if (d == null) return;
            var rng = new System.Random(d.key.GetHashCode() * 17 + 3);
            Vector3 altar, stone;
            if (!Stored(d, "a", out altar)) { if (!Pick(p.transform.position, rng, 70f, 110f, out altar)) return; Save(d, "a", altar); }
            if (!Stored(d, "s", out stone)) { if (!Pick(p.transform.position, rng, 25f, 50f, out stone)) return; Save(d, "s", stone); }
            HintStone.Make(d, stone, altar);
            BossAltar.Make(d, altar);
        }
    }

    public class HintStone : Interactable
    {
        HiddenBosses.Def d; Vector3 altar;
        public static void Make(HiddenBosses.Def d, Vector3 pos, Vector3 altar)
        {
            var go = new GameObject("HintStone_" + d.key); go.transform.position = pos;
            var s = go.AddComponent<HintStone>(); s.d = d; s.altar = altar; s.radius = 2.6f; s.prompt = "Baca prasasti tua";
            RT.Prim(PrimitiveType.Cube, go.transform, new Vector3(0, 0.8f, 0), new Vector3(0.9f, 1.6f, 0.35f), new Color(0.45f, 0.45f, 0.5f));
            RT.Prim(PrimitiveType.Sphere, go.transform, new Vector3(0, 1.9f, 0), Vector3.one * 0.22f, new Color(0.7f, 0.85f, 1f));
        }
        public override void Interact()
        {
            var gs = GameState.I; if (gs == null) return;
            gs.stats["hbh_" + d.key] = 1; Audio.Sfx("quest_progress", transform.position);
            float dist = Vector3.Distance(transform.position, altar); dist = Mathf.Round(dist / 10f) * 10f;
            string t = d.hint.Replace("{dir}", HiddenBosses.Dir(transform.position, altar)).Replace("{dist}", dist.ToString("0"));
            TownUI.I.Open("Prasasti Tua - " + d.region, () => t + "\n\n(Tercatat. Altar pemanggil sudah kamu ketahui.)", () => { var l = new List<UIEntry>(); l.Add(new UIEntry("Tutup", () => TownUI.I.Close())); return l; });
        }
    }

    public class BossAltar : Interactable
    {
        HiddenBosses.Def d; Monster live; float born;
        public static void Make(HiddenBosses.Def d, Vector3 pos)
        {
            var go = new GameObject("BossAltar_" + d.key); go.transform.position = pos;
            var a = go.AddComponent<BossAltar>(); a.d = d; a.radius = 3.2f; a.prompt = "Altar tua"; a.born = Time.time;
            RT.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0, 0.3f, 0), new Vector3(2.2f, 0.3f, 2.2f), new Color(0.3f, 0.3f, 0.34f));
            RT.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0, 0.9f, 0), new Vector3(0.9f, 0.6f, 0.9f), new Color(0.4f, 0.4f, 0.45f));
            RT.Prim(PrimitiveType.Sphere, go.transform, new Vector3(0, 1.8f, 0), Vector3.one * 0.5f, d.color);
        }
        public override void Interact()
        {
            var gs = GameState.I; var ui = TownUI.I; if (gs == null) return;
            if (live != null && !live.IsDead) { ui.Toast(d.name + " sedang bertarung. Kalahkan dulu!"); return; }
            if (!HiddenBosses.HintSeen(d))
            {
                ui.Open("Altar Tua", () => "Altar dingin dan tak bereaksi. Tulisan di sisinya sudah pudar.\n\nMungkin ada prasasti petunjuk di suatu tempat di " + d.region + ".", () => { var l = new List<UIEntry>(); l.Add(new UIEntry("Tutup", () => ui.Close())); return l; });
                return;
            }
            bool again = HiddenBosses.Defeated(d);
            ui.Open("Altar Tua - " + d.region,
                () => "Panggil " + d.name + "?\n" + (again ? "Sudah pernah dikalahkan (hadiah unik hanya pada kemenangan pertama)." : "Hadiah unik: " + d.gearName + ".") + "\nBoss rahasia sangat kuat. Siapkan ramuan dan partner.",
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Panggil boss", () => { ui.Close(); Summon(); }));
                    l.Add(new UIEntry("Jangan sekarang", () => ui.Close()));
                    return l;
                });
        }
        void Summon()
        {
            var go = new GameObject("HB_" + d.key); go.transform.position = transform.position + new Vector3(0, 0, 7f);
            Vector3 g; if (FarmAnimals.Ground(go.transform.position, out g)) go.transform.position = g;
            var m = go.AddComponent<Monster>(); m.defId = d.Id; m.isBoss = true; m.noLeash = true;
            live = m; m.Alert();
            Audio.Sfx("boss_roar", transform.position);
            if (TownUI.I != null) TownUI.I.Toast(d.name + " terbangun!");
        }
    }
}
