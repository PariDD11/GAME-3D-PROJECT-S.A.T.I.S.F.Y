using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HunterGuild
{
    /// <summary>Utilitas runtime: material, efek singkat.</summary>
    public static class RT
    {
        static Shader shader;

        public static Shader Sh
        {
            get
            {
                if (shader == null)
                {
                    shader = Shader.Find(GraphicsSettings.currentRenderPipeline != null ? "Universal Render Pipeline/Lit" : "Standard");
                    if (shader == null) shader = Shader.Find("Standard");
                }
                return shader;
            }
        }

        public static Material NewMat(Color c)
        {
            var m = new Material(Sh);
            m.color = c;
            return m;
        }

        public static GameObject Prim(PrimitiveType t, Transform parent, Vector3 localPos, Vector3 localScale, Color c)
        {
            var go = GameObject.CreatePrimitive(t);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = NewMat(c);
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            return go;
        }

        public static void Flash(Vector3 pos, Quaternion rot, Vector3 scale, Color c, float life)
        {
            var go = Prim(PrimitiveType.Cube, null, pos, scale, c);
            go.transform.rotation = rot;
            Object.Destroy(go, life);
        }

        public static void Line(Vector3 a, Vector3 b, float life)
        {
            Vector3 d = b - a;
            if (d.sqrMagnitude < 0.01f) return;
            var go = Prim(PrimitiveType.Cube, null, (a + b) / 2f, new Vector3(0.06f, 0.06f, d.magnitude), new Color(1f, 0.9f, 0.5f));
            go.transform.rotation = Quaternion.LookRotation(d.normalized);
            Object.Destroy(go, life);
        }

        /// <summary>Terrain yang benar-benar mencakup titik p (bukan sekadar activeTerrain, yang bisa terrain lain di scene).</summary>
        public static Terrain TerrainAt(Vector3 p)
        {
            Terrain best = null;
            var all = Terrain.activeTerrains;
            if (all != null)
                for (int i = 0; i < all.Length; i++)
                {
                    var t = all[i]; if (t == null || t.terrainData == null) continue;
                    Vector3 o = t.transform.position, sz = t.terrainData.size;
                    if (p.x >= o.x && p.x <= o.x + sz.x && p.z >= o.z && p.z <= o.z + sz.z) { if (best == null || o.y > best.transform.position.y) best = t; }
                }
            return best != null ? best : Terrain.activeTerrain;
        }

        public static float GroundY(Vector3 p)
        {
            var t = TerrainAt(p);
            return t != null ? t.SampleHeight(p) + t.transform.position.y : p.y;
        }

        static readonly RaycastHit[] rayBuf = new RaycastHit[12];

        /// <summary>Tinggi tanah untuk makhluk darat: raycast ke bawah pada collider padat (lantai, jembatan, tangga, terrain collider);
        /// bila tidak kena apa pun, memakai tinggi terrain. self = transform yang diabaikan.</summary>
        public static float WalkY(Vector3 p, Transform self)
        {
            float ty = GroundY(p);
            int n = Physics.RaycastNonAlloc(new Vector3(p.x, Mathf.Max(p.y, ty) + 2.5f, p.z), Vector3.down, rayBuf, 80f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.NegativeInfinity;
            for (int i = 0; i < n; i++)
            {
                var h = rayBuf[i]; var c = h.collider;
                if (c == null || h.normal.y < 0.6f) continue;
                if (self != null && c.transform.IsChildOf(self)) continue;
                if (c.GetComponentInParent<Monster>() != null || c.GetComponentInParent<PlayerController>() != null) continue;
                if (h.point.y > best && h.point.y <= Mathf.Max(p.y, ty) + 2.6f) best = h.point.y;
            }
            return float.IsNegativeInfinity(best) ? ty : best;
        }
    }

    public class MonsterDef
    {
        public string id, name, shape;
        public bool large;
        public float hp, atk, speed, aggro, telegraph = 0.8f, recover = 1.2f, reach, radius, height, size = 1f;
        public Color color;
        public string[] loot;
        public string rare = "";
        public float rareChance;
        public int carves = 1;
        public bool ranged;
        public bool passive;      // hewan ternak/buruan: tidak menyerang, kabur saat diserang, tidak masuk statistik monster/Bestiary
    }

    /// <summary>Data monster placeholder. Angka sementara; atur sesuai target TTK di GDD 3.2.</summary>
    public static class MonsterDB
    {
        static readonly Dictionary<string, MonsterDef> db = new Dictionary<string, MonsterDef>();

        public static MonsterDef Get(string id)
        {
            MonsterDef d;
            return id != null && db.TryGetValue(id, out d) ? d : null;
        }

        static void Add(MonsterDef d) { db[d.id] = d; }

        /// <summary>Semua monster (tanpa boneka latihan), urut HP naik. Dipakai Bestiary.</summary>
        public static List<MonsterDef> All()
        {
            var l = new List<MonsterDef>();
            foreach (var d in db.Values) if (d.id != "dummy" && !d.passive) l.Add(d);
            l.Sort((a, b) => a.hp.CompareTo(b.hp));
            return l;
        }

        /// <summary>Semua definisi tanpa kecuali (termasuk hewan pasif dan boneka latihan), urut id. Dipakai editor Ganti Monster.</summary>
        public static List<MonsterDef> Every()
        {
            var l = new List<MonsterDef>(db.Values);
            l.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            return l;
        }

        static MonsterDB()
        {
            Add(new MonsterDef { id = "dummy", name = "Boneka Latihan", shape = "dummy", hp = 1500, atk = 0, speed = 0, aggro = 0, telegraph = 1f, recover = 1f,
                reach = 1f, radius = 0.7f, height = 2.0f, color = new Color(0.72f, 0.58f, 0.34f), loot = new[] { "herb" }, carves = 0 });
            Add(new MonsterDef { id = "slime", name = "Slime", shape = "slime", hp = 60, atk = 6, speed = 2.2f, aggro = 12, telegraph = 0.6f, recover = 1.0f,
                reach = 1.8f, radius = 0.8f, height = 1.0f, color = new Color(0.3f, 0.8f, 0.4f), loot = new[] { "slime_jelly", "herb" }, carves = 1 });
            Add(new MonsterDef { id = "goblin", name = "Goblin", shape = "goblin", hp = 120, atk = 12, speed = 3.4f, aggro = 18, telegraph = 0.7f, recover = 1.1f,
                reach = 2.2f, radius = 0.6f, height = 1.7f, color = new Color(0.4f, 0.6f, 0.25f), loot = new[] { "goblin_ear", "leather" }, rare = "iron_ingot", rareChance = 0.1f, carves = 1 });
            Add(new MonsterDef { id = "alpha_wolf", name = "Alpha Wolf Muda", shape = "wolf", large = true, hp = 1800, atk = 22, speed = 5.2f, aggro = 40, telegraph = 0.9f, recover = 1.4f,
                reach = 4.2f, radius = 1.6f, height = 2.2f, size = 1.5f, color = new Color(0.45f, 0.45f, 0.5f), loot = new[] { "wolf_pelt", "monster_bone" }, rare = "wolf_fang", rareChance = 0.25f, carves = 3 });
            Add(new MonsterDef { id = "giant_boar", name = "Giant Boar", shape = "boar", large = true, hp = 2200, atk = 28, speed = 4.6f, aggro = 35, telegraph = 1.0f, recover = 1.6f,
                reach = 4.6f, radius = 1.8f, height = 2.4f, size = 1.7f, color = new Color(0.4f, 0.28f, 0.18f), loot = new[] { "boar_tusk", "monster_bone", "leather" }, rare = "iron_ingot", rareChance = 0.15f, carves = 3 });
            Add(new MonsterDef { id = "dire_wolf", name = "Dire Wolf Dewasa", shape = "wolf", large = true, hp = 3200, atk = 38, speed = 5.6f, aggro = 45, telegraph = 0.9f, recover = 1.4f,
                reach = 4.8f, radius = 1.9f, height = 2.6f, size = 1.9f, color = new Color(0.22f, 0.22f, 0.28f), loot = new[] { "wolf_pelt", "monster_bone" }, rare = "wolf_fang", rareChance = 0.35f, carves = 3 });
            // ----- Hewan ternak / buruan (pasif): daging dan bahan untuk dijual atau diantar ke NPC -----
            Add(new MonsterDef { id = "chicken", name = "Ayam", shape = "chicken", passive = true, hp = 18, atk = 0, speed = 3.0f, aggro = 0, reach = 1f, radius = 0.35f, height = 0.8f, size = 1f,
                color = new Color(0.96f, 0.94f, 0.88f), loot = new[] { "raw_chicken", "feather" }, carves = 2 });
            Add(new MonsterDef { id = "sheep", name = "Domba", shape = "sheep", passive = true, hp = 45, atk = 0, speed = 2.4f, aggro = 0, reach = 1f, radius = 0.6f, height = 1.2f, size = 1f,
                color = new Color(0.94f, 0.92f, 0.86f), loot = new[] { "raw_mutton", "wool" }, carves = 2 });
            Add(new MonsterDef { id = "goat", name = "Kambing", shape = "goat", passive = true, hp = 45, atk = 0, speed = 3.2f, aggro = 0, reach = 1f, radius = 0.55f, height = 1.2f, size = 1f,
                color = new Color(0.62f, 0.5f, 0.38f), loot = new[] { "raw_goat", "leather" }, carves = 2 });
            Add(new MonsterDef { id = "cow", name = "Sapi", shape = "cow", passive = true, hp = 100, atk = 0, speed = 2.2f, aggro = 0, reach = 1f, radius = 0.8f, height = 1.5f, size = 1f,
                color = new Color(0.95f, 0.94f, 0.92f), loot = new[] { "raw_beef", "cow_hide" }, carves = 3 });
            // ----- Gurun Zahar -----
            Add(new MonsterDef { id = "sand_lizard", name = "Kadal Pasir", shape = "lizard", hp = 110, atk = 10, speed = 4.2f, aggro = 18, telegraph = 0.6f, recover = 1.0f,
                reach = 2.1f, radius = 0.6f, height = 0.9f, color = new Color(0.72f, 0.58f, 0.34f), loot = new[] { "lizard_hide", "monster_bone" }, rare = "potion", rareChance = 0.08f, carves = 1 });
            Add(new MonsterDef { id = "mummy", name = "Mumi Penjaga", shape = "mummy", hp = 260, atk = 15, speed = 2.9f, aggro = 20, telegraph = 0.85f, recover = 1.3f,
                reach = 2.4f, radius = 0.6f, height = 1.9f, color = new Color(0.82f, 0.76f, 0.6f), loot = new[] { "mummy_wrap", "monster_bone" }, rare = "iron_ingot", rareChance = 0.12f, carves = 1 });
            Add(new MonsterDef { id = "giant_scorpion", name = "Skorpion Raksasa", shape = "scorpion", large = true, hp = 2400, atk = 30, speed = 4.4f, aggro = 36, telegraph = 1.0f, recover = 1.5f,
                reach = 4.6f, radius = 1.8f, height = 2.0f, size = 1.7f, color = new Color(0.55f, 0.32f, 0.16f), loot = new[] { "scorpion_stinger", "monster_bone" }, rare = "scorpion_stinger", rareChance = 0.3f, carves = 3 });
            Add(new MonsterDef { id = "sand_golem", name = "Golem Pasir", shape = "golem", large = true, hp = 4200, atk = 42, speed = 3.2f, aggro = 38, telegraph = 1.2f, recover = 1.8f,
                reach = 5.0f, radius = 2.0f, height = 4.2f, size = 1.9f, color = new Color(0.74f, 0.55f, 0.32f), loot = new[] { "golem_core", "monster_bone" }, rare = "golem_core", rareChance = 0.3f, carves = 3 });
            // bandit: dipakai event penyergapan (BanditAmbush)
            Add(new MonsterDef { id = "bandit", name = "Bandit", shape = "bandit", hp = 95, atk = 8, speed = 3.7f, aggro = 28, telegraph = 0.75f, recover = 1.0f,
                reach = 2.3f, radius = 0.5f, height = 1.8f, color = new Color(0.34f, 0.25f, 0.17f), loot = new[] { "leather", "herb", "potion" }, rare = "iron_ingot", rareChance = 0.12f, carves = 1 });
            Add(new MonsterDef { id = "bandit_archer", name = "Bandit Pemanah", shape = "archer", hp = 70, atk = 7, speed = 3.0f, aggro = 34, telegraph = 1.05f, recover = 1.7f,
                reach = 17f, radius = 0.5f, height = 1.8f, color = new Color(0.22f, 0.32f, 0.2f), loot = new[] { "leather", "herb" }, rare = "potion", rareChance = 0.2f, carves = 1, ranged = true });
            Add(new MonsterDef { id = "bandit_leader", name = "Kapten Bandit", shape = "leader", hp = 700, atk = 17, speed = 4.1f, aggro = 36, telegraph = 0.85f, recover = 1.2f,
                reach = 2.9f, radius = 0.65f, height = 2.1f, size = 1.18f, color = new Color(0.38f, 0.1f, 0.1f), loot = new[] { "leather", "iron_ingot", "orc_tusk" }, rare = "iron_ingot", rareChance = 0.5f, carves = 3 });
            // ----- Rawa Terkutuk (undead & penyihir) -----
            Add(new MonsterDef { id = "drowned", name = "Nelayan Tenggelam", shape = "zombie", hp = 150, atk = 11, speed = 2.6f, aggro = 20, telegraph = 0.85f, recover = 1.3f,
                reach = 2.3f, radius = 0.6f, height = 1.8f, color = new Color(0.42f, 0.56f, 0.5f), loot = new[] { "grave_dust", "monster_bone" }, rare = "potion", rareChance = 0.08f, carves = 1 });
            Add(new MonsterDef { id = "skeleton", name = "Kerangka Rawa", shape = "skeleton", hp = 130, atk = 13, speed = 3.5f, aggro = 24, telegraph = 0.7f, recover = 1.0f,
                reach = 2.4f, radius = 0.5f, height = 1.85f, color = new Color(0.82f, 0.82f, 0.74f), loot = new[] { "monster_bone", "grave_dust" }, rare = "iron_ingot", rareChance = 0.1f, carves = 1 });
            Add(new MonsterDef { id = "wraith", name = "Hantu Rawa", shape = "wraith", hp = 210, atk = 17, speed = 3.7f, aggro = 28, telegraph = 0.9f, recover = 1.3f,
                reach = 2.5f, radius = 0.6f, height = 2.0f, color = new Color(0.35f, 0.65f, 0.6f), loot = new[] { "ectoplasm", "grave_dust" }, rare = "potion", rareChance = 0.12f, carves = 1 });
            Add(new MonsterDef { id = "witch", name = "Penyihir Rawa", shape = "witch", hp = 190, atk = 15, speed = 3.0f, aggro = 32, telegraph = 1.1f, recover = 1.8f,
                reach = 16f, radius = 0.5f, height = 1.9f, color = new Color(0.36f, 0.2f, 0.5f), loot = new[] { "spider_silk", "hex_cloth", "herb" }, rare = "potion", rareChance = 0.2f, carves = 1, ranged = true });
            Add(new MonsterDef { id = "swamp_troll", name = "Troll Rawa", shape = "troll", large = true, hp = 3800, atk = 40, speed = 3.5f, aggro = 36, telegraph = 1.1f, recover = 1.7f,
                reach = 4.8f, radius = 1.9f, height = 3.6f, size = 1.8f, color = new Color(0.34f, 0.46f, 0.3f), loot = new[] { "troll_hide", "monster_bone" }, rare = "troll_hide", rareChance = 0.3f, carves = 3 });
            Add(new MonsterDef { id = "necromancer", name = "Maelis sang Nekromancer", shape = "necro", large = true, hp = 6800, atk = 54, speed = 3.7f, aggro = 42, telegraph = 1.2f, recover = 1.6f,
                reach = 5.4f, radius = 1.6f, height = 3.4f, size = 1.7f, color = new Color(0.2f, 0.15f, 0.28f), loot = new[] { "necro_core", "ectoplasm", "monster_bone" }, rare = "necro_core", rareChance = 0.4f, carves = 3 });
            AddTowerDefs();
            AddEldrathAreaDefs();
            AddExamDefs();
            HiddenBosses.RegisterMonsters(Add);
        }

        static void M(string id, string name, string shape, float hp, float atk, float speed, float aggro, float reach, float rad, float h, float size, Color col, bool ranged, string l1, string l2, string rare, float rc)
        {
            Add(new MonsterDef { id = id, name = name, shape = shape, hp = hp, atk = atk, speed = speed, aggro = aggro, telegraph = ranged ? 1.1f : 0.8f, recover = ranged ? 1.7f : 1.2f,
                reach = reach, radius = rad, height = h, size = size, color = col, loot = new[] { l1, l2 }, rare = rare, rareChance = rc, carves = 1, ranged = ranged });
        }

        static void L(string id, string name, string shape, float hp, float atk, float speed, float size, Color col, float h, float rad, float reach, string l1, string l2, string rare)
        {
            Add(new MonsterDef { id = id, name = name, shape = shape, large = true, hp = hp, atk = atk, speed = speed, aggro = 40, telegraph = 1.1f, recover = 1.7f,
                reach = reach, radius = rad, height = h, size = size, color = col, loot = new[] { l1, l2, "monster_bone" }, rare = rare, rareChance = 0.3f, carves = 3 });
        }

        /// <summary>Area hunting Eldrath: Hutan Perbatasan (selatan), Gunung Vulkanik (utara), Padang Tulang (timur), Lembah Iblis (barat).</summary>
        static void AddEldrathAreaDefs()
        {
            // Hutan Perbatasan
            M("road_wolf", "Serigala Jalan", "wolf", 210, 17, 5.0f, 24, 2.3f, 0.7f, 1.0f, 1f, new Color(0.45f, 0.42f, 0.38f), false, "road_pelt", "wolf_fang", "potion", 0.08f);
            M("highway_bandit", "Bandit Jalan Raya", "bandit", 230, 17, 3.8f, 28, 2.4f, 0.5f, 1.8f, 1f, new Color(0.3f, 0.22f, 0.16f), false, "leather", "iron_ingot", "potion", 0.15f);
            M("highway_archer", "Pemanah Jalan Raya", "archer", 170, 15, 3.0f, 34, 17f, 0.5f, 1.8f, 1f, new Color(0.22f, 0.3f, 0.2f), true, "leather", "herb", "potion", 0.2f);
            L("ogre", "Ogre Hutan", "troll", 3200, 38, 3.6f, 1.6f, new Color(0.5f, 0.42f, 0.3f), 3.4f, 1.7f, 4.6f, "ogre_tusk", "troll_hide", "ogre_tusk");
            L("ogre_chief", "Grumak, Penguasa Hutan Perbatasan", "troll", 7600, 55, 3.9f, 2.0f, new Color(0.38f, 0.3f, 0.2f), 4.0f, 2.1f, 5.4f, "ogre_tusk", "iron_ingot", "ogre_tusk");
            // Gunung Vulkanik
            M("ember_imp", "Imp Bara", "goblin", 300, 26, 4.0f, 26, 2.3f, 0.6f, 1.6f, 1.05f, new Color(0.85f, 0.3f, 0.12f), false, "ember_scale", "brimstone", "potion", 0.1f);
            M("lava_lizard", "Kadal Lava", "lizard", 360, 28, 4.5f, 22, 2.3f, 0.7f, 1.0f, 1.25f, new Color(0.9f, 0.4f, 0.1f), false, "ember_scale", "brimstone", "magma_core", 0.05f);
            M("ash_wraith", "Hantu Abu", "wraith", 420, 32, 3.7f, 30, 2.6f, 0.6f, 2.0f, 1f, new Color(0.35f, 0.3f, 0.3f), false, "hellfire_ash", "brimstone", "potion", 0.12f);
            M("fire_cultist", "Dukun Api", "witch", 340, 30, 3.0f, 34, 16f, 0.5f, 1.9f, 1f, new Color(0.8f, 0.2f, 0.1f), true, "hellfire_ash", "hex_cloth", "potion", 0.2f);
            L("magma_golem", "Golem Magma", "golem", 8200, 66, 3.2f, 2.0f, new Color(0.9f, 0.35f, 0.08f), 4.3f, 2.1f, 5.2f, "magma_core", "brimstone", "magma_core");
            L("ignarok", "Ignarok, Naga Magma", "lizard", 15500, 84, 4.6f, 3.4f, new Color(0.85f, 0.2f, 0.05f), 3.0f, 2.8f, 6.2f, "magma_core", "ember_scale", "magma_core");
            // Padang Tulang
            M("bone_soldier", "Prajurit Tulang", "skeleton", 360, 28, 3.6f, 26, 2.4f, 0.5f, 1.9f, 1f, new Color(0.88f, 0.86f, 0.76f), false, "ancient_bone", "grave_dust", "iron_ingot", 0.12f);
            M("bone_archer", "Pemanah Tulang", "archer", 280, 26, 3.1f, 36, 17f, 0.5f, 1.8f, 1f, new Color(0.8f, 0.8f, 0.72f), true, "ancient_bone", "grave_dust", "potion", 0.2f);
            M("banshee", "Banshee Medan Perang", "wraith", 450, 34, 3.8f, 32, 2.6f, 0.6f, 2.0f, 1.05f, new Color(0.6f, 0.7f, 0.9f), false, "ectoplasm", "grave_dust", "potion", 0.14f);
            L("grave_knight", "Ksatria Makam", "leader", 5200, 58, 4.0f, 1.5f, new Color(0.35f, 0.38f, 0.45f), 2.4f, 0.9f, 3.6f, "ancient_bone", "iron_ingot", "ancient_bone");
            L("behemoth", "Behemoth, Penghancur Hutan", "bull", 11000, 62, 4.2f, 2.0f, new Color(0.34f, 0.26f, 0.2f), 1.9f, 2.6f, 6.2f, "behemoth_horn", "iron_ingot", "ogre_tusk");
            L("cerberus", "Cerberus, Penjaga Gerbang Makam", "cerberus", 13500, 70, 4.6f, 2.0f, new Color(0.14f, 0.1f, 0.14f), 1.8f, 2.4f, 6.0f, "cerberus_fang", "ancient_bone", "cerberus_fang");
            L("bone_king", "Malrek, Raja Tulang", "necro", 16500, 82, 3.9f, 2.0f, new Color(0.82f, 0.8f, 0.68f), 3.6f, 1.7f, 5.6f, "king_crown_shard", "ancient_bone", "king_crown_shard");
            // Lembah Iblis
            M("imp", "Imp Jurang", "goblin", 420, 36, 4.2f, 28, 2.3f, 0.6f, 1.6f, 1.05f, new Color(0.5f, 0.15f, 0.55f), false, "demon_horn", "hellfire_ash", "potion", 0.1f);
            M("pollen_boar", "Babi Serbuk Sari", "boar", 380, 24, 4.6f, 22, 2.2f, 0.9f, 1.2f, 1.0f, new Color(0.9f, 0.6f, 0.7f), false, "leather", "wolf_fang", "ogre_tusk", 0.05f);
            M("heat_jackal", "Rubah Padang Panas", "wolf", 520, 34, 6.2f, 30, 2.3f, 0.8f, 1.0f, 1.0f, new Color(0.85f, 0.6f, 0.25f), false, "wolf_fang", "leather", "ogre_tusk", 0.06f);
            M("harvest_brute", "Brute Labu Panen", "troll", 1400, 40, 3.4f, 28, 2.8f, 1.2f, 2.0f, 1.9f, new Color(0.85f, 0.45f, 0.12f), false, "ogre_tusk", "leather", "iron_ingot", 0.08f);
            M("frost_wolf", "Serigala Salju", "wolf", 640, 38, 6.0f, 34, 2.4f, 0.8f, 1.1f, 1.1f, new Color(0.82f, 0.92f, 1f), false, "wolf_fang", "leather", "ogre_tusk", 0.08f);
            M("hellhound", "Anjing Neraka", "wolf", 520, 42, 5.8f, 32, 2.5f, 0.8f, 1.1f, 1.1f, new Color(0.2f, 0.08f, 0.1f), false, "hellfire_ash", "wolf_fang", "demon_horn", 0.08f);
            M("dark_cultist", "Hollow Circle: Pemuja Lucifer", "witch", 400, 38, 3.1f, 36, 17f, 0.5f, 1.9f, 1f, new Color(0.3f, 0.08f, 0.35f), true, "hex_cloth", "demon_horn", "potion", 0.2f);
            L("demon_brute", "Iblis Brutal", "troll", 9800, 76, 3.8f, 2.2f, new Color(0.55f, 0.1f, 0.12f), 4.0f, 2.1f, 5.4f, "demon_horn", "hellfire_ash", "demon_horn");
            L("demon_lord", "Zargoth, Lord Iblis", "necro", 22000, 96, 4.1f, 2.3f, new Color(0.45f, 0.05f, 0.18f), 4.0f, 1.9f, 6.0f, "demon_heart", "demon_horn", "demon_heart");
        }

        /// <summary>Lawan Arena Ujian Guild: Arena Warden (D->C, C->B), Gladiator Elite (B->A, A->S), tiga hunter S-rank (S->SS).</summary>
        static void AddExamDefs()
        {
            Boss("arena_warden", "Arena Warden", "leader", 3600, 46, 4.2f, 1.5f, new Color(0.55f, 0.58f, 0.66f), 2.4f, 0.9f, 3.4f, false, "iron_ingot", "monster_bone");
            Boss("gladiator_elite", "Gladiator Elite", "leader", 9000, 74, 4.6f, 1.6f, new Color(0.72f, 0.5f, 0.22f), 2.5f, 0.95f, 3.6f, false, "iron_ingot", "ogre_tusk");
            Boss("srank_blade", "Kael sang Pedang Fajar", "leader", 11000, 84, 4.8f, 1.6f, new Color(0.85f, 0.3f, 0.25f), 2.4f, 0.9f, 3.6f, false, "iron_ingot", "ogre_tusk");
            Boss("srank_shadow", "Mirela si Panah Bayangan", "archer", 7500, 70, 4.0f, 1.45f, new Color(0.25f, 0.3f, 0.45f), 2.0f, 0.6f, 20f, true, "leather", "ancient_bone");
            Boss("srank_ash", "Orvane Penyihir Abu", "witch", 8000, 76, 3.6f, 1.5f, new Color(0.5f, 0.2f, 0.15f), 2.0f, 0.6f, 20f, true, "hex_cloth", "hellfire_ash");
        }

        /// <summary>Boss Menara Eldrath (lantai 10, 20, ... 100). Nama dan warna unik; bentuk memakai model monster yang sudah ada.</summary>
        static void AddTowerDefs()
        {
            // id, nama, shape, hp, atk, speed, size, warna, tinggi, radius, reach, ranged
            Boss("tbs_10", "Grakk, Raja Goblin", "goblin", 2600, 24, 4.2f, 2.8f, new Color(0.35f, 0.5f, 0.15f), 1.7f, 0.6f, 2.6f, false, "goblin_ear", "iron_ingot");
            Boss("tbs_20", "Fenrath, Serigala Neraka", "wolf", 5200, 38, 6.0f, 2.2f, new Color(0.5f, 0.12f, 0.1f), 2.2f, 1.6f, 4.8f, false, "wolf_fang", "wolf_pelt");
            Boss("tbs_30", "Kapten Voss si Pengkhianat", "leader", 8800, 52, 4.6f, 1.9f, new Color(0.15f, 0.12f, 0.22f), 2.1f, 0.65f, 3.2f, false, "iron_ingot", "potion");
            Boss("tbs_40", "Anubra, Raja Mumi", "mummy", 13500, 66, 3.6f, 2.5f, new Color(0.9f, 0.8f, 0.45f), 1.9f, 0.6f, 3.4f, false, "mummy_wrap", "golem_core");
            Boss("tbs_50", "Gorgath, Troll Batu", "troll", 20000, 84, 3.8f, 2.4f, new Color(0.4f, 0.4f, 0.45f), 3.6f, 1.9f, 5.2f, false, "troll_hide", "golem_core");
            Boss("tbs_60", "Skorra, Ratu Kalajengking", "scorpion", 29000, 104, 5.0f, 2.8f, new Color(0.15f, 0.3f, 0.35f), 2.0f, 1.8f, 5.4f, false, "scorpion_stinger", "golem_core");
            Boss("tbs_70", "Morgath, Golem Obsidian", "golem", 40000, 126, 3.4f, 2.7f, new Color(0.12f, 0.1f, 0.14f), 4.2f, 2.0f, 5.8f, false, "golem_core", "necro_core");
            Boss("tbs_80", "Lady Nyxara, Penyihir Abyss", "witch", 52000, 150, 4.2f, 2.5f, new Color(0.45f, 0.1f, 0.55f), 1.9f, 0.5f, 18f, true, "hex_cloth", "necro_core");
            Boss("tbs_90", "Malvek, Raja Tengkorak", "skeleton", 66000, 176, 4.8f, 3.0f, new Color(0.9f, 0.9f, 0.82f), 1.85f, 0.5f, 4.6f, false, "monster_bone", "necro_core");
            Boss("tbs_100", "Eldrath, Penjaga Menara", "necro", 90000, 220, 4.4f, 2.6f, new Color(0.9f, 0.75f, 0.2f), 3.4f, 1.6f, 7.0f, false, "necro_core", "golem_core");
        }

        static void Boss(string id, string name, string shape, float hp, float atk, float speed, float size, Color col, float height, float radius, float reach, bool ranged, string loot1, string loot2)
        {
            Add(new MonsterDef { id = id, name = name, shape = shape, large = true, hp = hp, atk = atk, speed = speed, aggro = 60, telegraph = 1.2f, recover = 1.4f,
                reach = reach, radius = radius * size / 1.7f, height = height, size = size, color = col, loot = new[] { loot1, loot2, "monster_bone" }, rare = loot2, rareChance = 0.5f, carves = 3, ranged = ranged });
        }
    }

    /// <summary>Monster placeholder: patroli, mengejar, telegraph, menyerang, enrage di bawah 50% HP, leash ke sarang.</summary>
    public class Monster : MonoBehaviour
    {
        public static readonly List<Monster> All = new List<Monster>();

        public string defId = "slime";
        public float hp;
        public bool noLeash;                       // dipakai bandit penyergap: tidak kembali ke titik awal saat mengejar
        public float hpMult = 1f, atkMult = 1f;    // skala per lantai Menara (atau elite)
        public float damageTakenMult = 1f;         // < 1 = perisai (dipakai BossBrain)
        public float speedMult = 1f;               // pengali kecepatan gerak (dipakai BossBrain)
        public bool isBoss;                        // boss Menara: aura, toast khusus
        public string displayName = "";            // menimpa nama di MonsterDef (mis. "Elite ...")
        public System.Action<Monster> onDeath;
        float maxHp = 1f;

        // ---- v35z102n33: co-op LAN. Di tamu, monster adalah "boneka" yang mengikuti snapshot host (tanpa AI); damage dikirim ke host. ----
        public bool netPuppet; public int netId;
        Vector3 netPos; float netYaw, netHpInit, netMaxInit; int netState;
        public int NetState { get { return (int)state; } }
        public void NetApply(Vector3 pos, float yaw, float nhp, float nmax, int st)
        {
            netPos = pos; netYaw = yaw; netState = st; netHpInit = nhp; netMaxInit = nmax;
            if (def != null) { maxHp = Mathf.Max(1f, nmax); hp = Mathf.Max(1f, Mathf.Min(nhp, nmax)); }
        }
        /// <summary>Host menerima damage mentah dari tamu (pengali kritis/elemen sudah dihitung di sisi tamu).</summary>
        public void NetDamage(float dmg)
        {
            if (IsDead || def == null || netPuppet) return;
            dmg *= damageTakenMult;
            hp -= dmg; hitFlash = 0.12f;
            if (swap != null) swap.Hurt();
            if (state == State.Idle || state == State.Return) state = State.Chase;
            CombatFx.DamageNumber(HitPoint + Vector3.up * (def.height * 0.5f), dmg, dmg >= 28f, Color.white);
            Audio.Sfx("hit", transform.position);
            if (hp <= 0f) Die();
        }
        /// <summary>Tamu: host melaporkan monster ini mati -> jalankan kematian lokal (kredit kill, EXP, drop untuk tamu).</summary>
        public void NetDie() { if (IsDead || def == null) return; hp = 0f; Die(); }
        void NetPuppetTick()
        {
            float k = 1f - Mathf.Exp(-12f * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, netPos, k);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, netYaw, 0f), k);
            if (netState == 2) Tint(new Color(1f, 0.15f, 0.1f), 0.55f + 0.3f * Mathf.Sin(Time.time * 30f));
            else if (tinted && hitFlash <= 0f) Tint(Color.white, 0f);
            AnimateLegs();
        }
        public float MaxHp { get { return maxHp; } }
        public float Atk { get { return def != null ? def.atk * atkMult * Difficulty.AtkMul * Balance.MonAtk * WorldReact.AtkMul(def.id) : 0f; } }
        Transform[] legs;
        Vector3 lastPos;
        float walkT;
        public bool IsDead { get; private set; }
        public string Name { get { return !string.IsNullOrEmpty(displayName) ? displayName : (def != null ? def.name : defId); } }
        public string DefId { get { return defId; } }

        /// <summary>Membuat monster langsung mengejar pemain (dipakai event penyergapan).</summary>
        public void Alert() { if (!IsDead && (state == State.Idle || state == State.Return)) state = State.Chase; }
        public float Radius { get { return def != null ? def.radius : 1f; } }
        public Vector3 HitPoint { get { return transform.position + Vector3.up * (def != null ? def.height * 0.5f : 1f); } }
        public string Condition
        {
            get
            {
                if (def == null) return "";
                if (hp < maxHp * 0.5f) return "Marah";
                if (hp < maxHp * 0.8f) return "Terluka";
                return "Sehat";
            }
        }

        enum State { Idle, Chase, Telegraph, Recover, Return }
        /// <summary>true bila monster sedang memburu/menyerang (dipakai musik pertempuran).</summary>
        public bool InCombat { get { return !IsDead && (state == State.Chase || state == State.Telegraph || state == State.Recover); } }

        MonsterDef def;
        public MonsterDef Def { get { return def; } }
        State state = State.Idle;
        Vector3 den, wanderTarget;
        float stateUntil, nextWander, hitFlash;
        Transform visual;
        MonsterFeedback fb;
        Renderer[] rends;
        Color[] baseColors;
        MonsterRig rig;                                 // v35z102n78: model baru dari kode (MonsterKit)
        MonsterSwapModel swap;                          // v35z102n78: model buatan pengguna (Hunter Guild > Ganti Monster); menggantikan rig
        /// <summary>true bila monster memakai model MonsterKit / model pengganti (bahan dibagi lewat palet atau milik prefab, jangan digandakan).</summary>
        public bool HasRig { get { return rig != null || swap != null; } }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        void OnDestroy() { if (rig != null) { rig.Dispose(); rig = null; } }

        void Start()
        {
            def = MonsterDB.Get(defId);
            if (def == null) { enabled = false; return; }
            hp = def.hp * hpMult * Difficulty.HpMul * Balance.MonHp;
            maxHp = hp;
            if (netPuppet && netMaxInit > 0f) { maxHp = netMaxInit; hp = Mathf.Max(1f, netHpInit); }
            if (BossBrain.Has(defId)) isBoss = true;
            den = transform.position;
            wanderTarget = den;
            BuildVisual();
            { fb = gameObject.AddComponent<MonsterFeedback>(); fb.Init(this, def); }
            gameObject.AddComponent<MonsterRoyal>().Init(this, def.color, isBoss, def.size, def.height, def.radius);   // v35z102n6
            if (isBoss)
            {
                var aura = new GameObject("BossAura").AddComponent<Light>();
                aura.transform.SetParent(transform, false);
                aura.transform.localPosition = Vector3.up * (def.height * 0.6f);
                aura.type = LightType.Point; aura.range = 14f + def.size * 3f; aura.intensity = 2.2f;
                aura.color = Color.Lerp(def.color, new Color(1f, 0.2f, 0.15f), 0.6f);
            }
            var cap = gameObject.AddComponent<CapsuleCollider>();
            cap.radius = def.radius;
            cap.height = Mathf.Max(def.height, def.radius * 2f);
            cap.center = new Vector3(0, def.height / 2f, 0);
            Snap();
            if (!netPuppet) BossBrain.AttachTo(this);
        }

        // ---- berenang & tenggelam ----
        public bool Swimming { get; private set; }
        float breath = -1f, drownTick, sink;
        bool drowning;

        /// <summary>Makhluk tak bernafas / akuatik tidak tenggelam (boneka, slime, undead, golem, boss).</summary>
        bool WaterImmune()
        {
            if (def == null || isBoss) return true;
            switch (def.id) { case "dummy": case "slime": case "drowned": case "wraith": case "skeleton": case "mummy": case "sand_golem": case "necromancer": return true; }
            return false;
        }

        float BreathMax() { return 10f * (def.large ? 1.6f : 1f) * (def.passive ? 2f : 1f); }

        void Snap()
        {
            Vector3 p = transform.position;
            float gy = RT.WalkY(p, transform);
            p.y = gy;
            float surf;
            bool swim = false;
            float h = Mathf.Max(0.8f, def != null ? def.height : 1f);
            if (def != null && WaterZone.Query(new Vector3(p.x, gy + 0.1f, p.z), out surf))
            {
                float depth = surf - gy;
                if (depth > h * 0.55f && depth < 9f)
                {
                    swim = true;
                    float dt = Time.deltaTime;
                    if (breath < 0f) breath = BreathMax();
                    if (!WaterImmune())
                    {
                        breath = Mathf.Max(0f, breath - dt);
                        if (breath <= 0f)
                        {   // nafas habis: tenggelam pelan ke dasar dan terus kehilangan HP
                            if (!drowning) { drowning = true; CombatFx.Burst(p + Vector3.up * h * 0.5f, new Color(0.85f, 0.95f, 1f), 10, 0.9f, 1f, 0.5f, 0.08f); }
                            sink = Mathf.Min(depth, sink + dt * 0.7f);
                            drownTick -= dt;
                            if (drownTick <= 0f) { drownTick = 1f; TakeDamage(maxHp * 0.12f, false); }
                        }
                    }
                    float floatY = surf - h * 0.5f;
                    p.y = Mathf.Max(gy, floatY - sink);
                    if (Time.frameCount % 40 == 0) CombatFx.ShockRing(new Vector3(p.x, surf + 0.02f, p.z), Mathf.Max(0.8f, def.radius * 2.2f), new Color(0.85f, 0.95f, 1f, 0.5f), 0.5f);
                }
            }
            if (!swim && breath >= 0f)
            {
                float mx = def != null ? BreathMax() : 10f;
                breath = Mathf.Min(mx, breath + Time.deltaTime * 4f);
                sink = 0f; drowning = false;
            }
            Swimming = swim;
            transform.position = p;
        }

        void BuildVisual()
        {
            visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);
            visual.localScale = Vector3.one * def.size;

            // v35z102n78: 1) model buatan pengguna (Ganti Monster), 2) model baru dari kode (MonsterKit), 3) model primitif lama di bawah sebagai cadangan.
            try { swap = MonsterSwapRuntime.TryApply(this, def, visual); }
            catch (System.Exception swapEx) { Debug.LogWarning("[MonsterSwap] " + def.id + ": " + swapEx.Message); swap = null; ClearVisual(); }
            if (swap != null) return;
            try { rig = MonsterKit.Build(def, visual); }
            catch (System.Exception kitEx) { Debug.LogWarning("[MonsterKit] " + def.id + ": " + kitEx.Message); rig = null; ClearVisual(); }
            if (rig != null) return;

            Color c = def.color;
            Color dark = c * 0.6f; dark.a = 1f;
            Color white = new Color(0.95f, 0.95f, 0.9f);
            Color red = new Color(0.8f, 0.15f, 0.1f);

            switch (def.shape)
            {
                case "bandit":
                case "archer":
                case "leader":
                    BuildBandit(c);
                    break;
                case "lizard":
                    {
                        Color belly = c * 1.2f; belly.a = 1f;
                        var lb = RT.Prim(PrimitiveType.Capsule, visual, new Vector3(0, 0.5f, 0), new Vector3(0.55f, 0.8f, 0.55f), c);
                        lb.transform.localRotation = Quaternion.Euler(90, 0, 0);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 0.55f, 0.85f), new Vector3(0.5f, 0.38f, 0.65f), belly);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(-0.17f, 0.7f, 1.0f), new Vector3(0.12f, 0.12f, 0.12f), red);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0.17f, 0.7f, 1.0f), new Vector3(0.12f, 0.12f, 0.12f), red);
                        var lt = RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(0, 0.35f, -1.2f), new Vector3(0.2f, 0.75f, 0.2f), dark);
                        lt.transform.localRotation = Quaternion.Euler(80, 0, 0);
                        foreach (float lx in new[] { -0.42f, 0.42f })
                            foreach (float lz in new[] { -0.4f, 0.5f })
                                RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(lx, 0.18f, lz), new Vector3(0.14f, 0.2f, 0.14f), dark);
                        for (int s = 0; s < 4; s++) RT.Prim(PrimitiveType.Cube, visual, new Vector3(0, 0.82f, -0.3f + s * 0.3f), new Vector3(0.06f, 0.16f, 0.12f), dark);
                    }
                    break;
                case "mummy":
                    {
                        Color wrap = c; wrap.a = 1f;
                        legs = new Transform[2];
                        for (int i = 0; i < 2; i++)
                        {
                            var hip = new GameObject("Leg" + i).transform;
                            hip.SetParent(visual, false);
                            hip.localPosition = new Vector3(i == 0 ? -0.2f : 0.2f, 0.95f, 0f);
                            RT.Prim(PrimitiveType.Cylinder, hip, new Vector3(0, -0.46f, 0), new Vector3(0.24f, 0.46f, 0.24f), wrap);
                            legs[i] = hip;
                        }
                        RT.Prim(PrimitiveType.Capsule, visual, new Vector3(0, 1.38f, 0), new Vector3(0.66f, 0.46f, 0.44f), wrap);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 1.88f, 0), new Vector3(0.36f, 0.4f, 0.36f), wrap);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(-0.09f, 1.92f, 0.15f), new Vector3(0.07f, 0.07f, 0.07f), new Color(0.3f, 1f, 0.9f));
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0.09f, 1.92f, 0.15f), new Vector3(0.07f, 0.07f, 0.07f), new Color(0.3f, 1f, 0.9f));
                        foreach (float sx in new[] { -1f, 1f })
                        {
                            var arm = RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(sx * 0.46f, 1.4f, 0.25f), new Vector3(0.16f, 0.4f, 0.16f), wrap);
                            arm.transform.localRotation = Quaternion.Euler(70f, 0, sx * 8f);
                        }
                        RT.Prim(PrimitiveType.Cube, visual, new Vector3(0.25f, 1.1f, -0.2f), new Vector3(0.05f, 0.9f, 0.04f), wrap * 0.8f);    // perban menjuntai
                    }
                    break;
                case "dummy":
                    {
                        Color straw = new Color(0.82f, 0.7f, 0.38f), wood = new Color(0.4f, 0.26f, 0.14f), cloth = new Color(0.75f, 0.2f, 0.18f);
                        RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(0, 0.9f, 0), new Vector3(0.18f, 0.9f, 0.18f), wood);
                        RT.Prim(PrimitiveType.Cube, visual, new Vector3(0, 1.45f, 0), new Vector3(1.3f, 0.12f, 0.12f), wood);
                        RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(0, 1.25f, 0), new Vector3(0.55f, 0.4f, 0.4f), straw);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 1.85f, 0), new Vector3(0.42f, 0.42f, 0.42f), straw);
                        RT.Prim(PrimitiveType.Cube, visual, new Vector3(0, 1.3f, 0.21f), new Vector3(0.5f, 0.1f, 0.05f), cloth);
                        RT.Prim(PrimitiveType.Cube, visual, new Vector3(0, 0.08f, 0), new Vector3(1.1f, 0.16f, 0.16f), wood);
                        RT.Prim(PrimitiveType.Cube, visual, new Vector3(0, 0.08f, 0), new Vector3(0.16f, 0.16f, 1.1f), wood);
                    }
                    break;
                case "scorpion":
                    {
                        Color pale = c * 1.25f; pale.a = 1f;
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 0.8f, 0), new Vector3(1.5f, 0.9f, 2.0f), c);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 0.85f, 1.1f), new Vector3(0.9f, 0.6f, 0.8f), pale);
                        for (int s = 0; s < 5; s++)
                        {
                            float t = s / 4f;
                            RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 1.0f + t * t * 1.9f, -1.0f - t * 0.7f + t * t * 0.9f), new Vector3(0.5f - t * 0.15f, 0.5f - t * 0.15f, 0.5f - t * 0.15f), c);
                        }
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 2.8f, -0.55f), new Vector3(0.28f, 0.5f, 0.28f), red);
                        foreach (float sx in new[] { -1f, 1f })
                        {
                            var claw = RT.Prim(PrimitiveType.Sphere, visual, new Vector3(sx * 0.95f, 0.75f, 1.65f), new Vector3(0.65f, 0.35f, 0.9f), pale);
                            claw.transform.localRotation = Quaternion.Euler(0, sx * -20f, 0);
                            RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(sx * 0.65f, 0.7f, 1.2f), new Vector3(0.18f, 0.35f, 0.18f), dark).transform.localRotation = Quaternion.Euler(0, 0, sx * 70f);
                            for (int lg = 0; lg < 3; lg++)
                                RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(sx * 1.0f, 0.4f, -0.5f + lg * 0.5f), new Vector3(0.12f, 0.45f, 0.12f), dark).transform.localRotation = Quaternion.Euler(0, 0, sx * 40f);
                        }
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(-0.2f, 1.12f, 1.35f), new Vector3(0.1f, 0.1f, 0.1f), red);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0.2f, 1.12f, 1.35f), new Vector3(0.1f, 0.1f, 0.1f), red);
                    }
                    break;
                case "golem":
                    {
                        Color moss = new Color(0.5f, 0.6f, 0.3f);
                        RT.Prim(PrimitiveType.Cube, visual, new Vector3(0, 1.3f, 0), new Vector3(1.7f, 1.4f, 1.1f), c);
                        RT.Prim(PrimitiveType.Cube, visual, new Vector3(0, 2.3f, 0), new Vector3(2.2f, 0.9f, 1.2f), dark);
                        RT.Prim(PrimitiveType.Cube, visual, new Vector3(0, 3.1f, 0.1f), new Vector3(0.9f, 0.8f, 0.8f), c);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(-0.2f, 3.2f, 0.52f), new Vector3(0.14f, 0.14f, 0.1f), new Color(0.3f, 1f, 0.95f));
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0.2f, 3.2f, 0.52f), new Vector3(0.14f, 0.14f, 0.1f), new Color(0.3f, 1f, 0.95f));
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 1.7f, 0.6f), new Vector3(0.3f, 0.3f, 0.12f), new Color(0.3f, 1f, 0.95f));
                        foreach (float sx in new[] { -1f, 1f })
                        {
                            RT.Prim(PrimitiveType.Cube, visual, new Vector3(sx * 1.45f, 1.7f, 0.1f), new Vector3(0.7f, 1.8f, 0.7f), c);
                            RT.Prim(PrimitiveType.Cube, visual, new Vector3(sx * 1.5f, 0.6f, 0.2f), new Vector3(0.95f, 0.9f, 0.95f), dark);
                            RT.Prim(PrimitiveType.Cube, visual, new Vector3(sx * 0.55f, 0.45f, 0f), new Vector3(0.75f, 0.9f, 0.85f), c);
                        }
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0.3f, 3.0f, -0.2f), new Vector3(0.5f, 0.2f, 0.4f), moss);
                    }
                    break;
                case "zombie":
                    {
                        Color rag = c * 0.7f; rag.a = 1f;
                        Color weed = new Color(0.2f, 0.42f, 0.2f);
                        legs = new Transform[2];
                        for (int i = 0; i < 2; i++)
                        {
                            var hip = new GameObject("Leg" + i).transform;
                            hip.SetParent(visual, false);
                            hip.localPosition = new Vector3(i == 0 ? -0.2f : 0.2f, 0.95f, 0f);
                            RT.Prim(PrimitiveType.Cylinder, hip, new Vector3(0, -0.46f, 0), new Vector3(0.24f, 0.46f, 0.24f), rag);
                            legs[i] = hip;
                        }
                        var tor = RT.Prim(PrimitiveType.Capsule, visual, new Vector3(0, 1.35f, 0.1f), new Vector3(0.66f, 0.46f, 0.46f), c);
                        tor.transform.localRotation = Quaternion.Euler(18f, 0, 0);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 1.78f, 0.3f), new Vector3(0.34f, 0.38f, 0.34f), c * 1.1f);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(-0.09f, 1.82f, 0.46f), new Vector3(0.07f, 0.07f, 0.07f), new Color(0.95f, 0.9f, 0.4f));
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0.09f, 1.82f, 0.46f), new Vector3(0.07f, 0.07f, 0.07f), new Color(0.95f, 0.9f, 0.4f));
                        foreach (float sx in new[] { -1f, 1f })
                        {
                            var arm = RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(sx * 0.45f, 1.35f, 0.5f), new Vector3(0.16f, 0.45f, 0.16f), c);
                            arm.transform.localRotation = Quaternion.Euler(80f, 0, sx * 6f);
                        }
                        RT.Prim(PrimitiveType.Cube, visual, new Vector3(-0.2f, 1.1f, 0.25f), new Vector3(0.06f, 0.8f, 0.04f), weed);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0.25f, 1.75f, 0.1f), new Vector3(0.3f, 0.12f, 0.3f), weed);
                    }
                    break;
                case "skeleton":
                    {
                        Color bone = c; bone.a = 1f;
                        legs = new Transform[2];
                        for (int i = 0; i < 2; i++)
                        {
                            var hip = new GameObject("Leg" + i).transform;
                            hip.SetParent(visual, false);
                            hip.localPosition = new Vector3(i == 0 ? -0.16f : 0.16f, 0.95f, 0f);
                            RT.Prim(PrimitiveType.Cylinder, hip, new Vector3(0, -0.46f, 0), new Vector3(0.1f, 0.46f, 0.1f), bone);
                            legs[i] = hip;
                        }
                        RT.Prim(PrimitiveType.Cube, visual, new Vector3(0, 1.0f, 0), new Vector3(0.4f, 0.12f, 0.22f), bone);
                        RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(0, 1.3f, -0.02f), new Vector3(0.07f, 0.4f, 0.07f), bone);
                        for (int r = 0; r < 4; r++) RT.Prim(PrimitiveType.Cube, visual, new Vector3(0, 1.2f + r * 0.13f, 0.03f), new Vector3(0.46f - r * 0.03f, 0.05f, 0.26f), bone);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 1.82f, 0), new Vector3(0.27f, 0.3f, 0.28f), bone);
                        RT.Prim(PrimitiveType.Cube, visual, new Vector3(0, 1.68f, 0.1f), new Vector3(0.16f, 0.09f, 0.12f), bone * 0.85f);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(-0.07f, 1.85f, 0.12f), new Vector3(0.07f, 0.07f, 0.05f), new Color(0.3f, 1f, 0.6f));
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0.07f, 1.85f, 0.12f), new Vector3(0.07f, 0.07f, 0.05f), new Color(0.3f, 1f, 0.6f));
                        foreach (float sx in new[] { -1f, 1f })
                            RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(sx * 0.32f, 1.3f, 0.12f), new Vector3(0.08f, 0.4f, 0.08f), bone).transform.localRotation = Quaternion.Euler(50f, 0, sx * 8f);
                        RT.Prim(PrimitiveType.Cube, visual, new Vector3(0.34f, 1.15f, 0.55f), new Vector3(0.07f, 0.12f, 0.95f), new Color(0.45f, 0.38f, 0.32f));
                    }
                    break;
                case "wraith":
                    {
                        Color ghost = c; ghost.a = 1f;
                        Color eye = new Color(0.9f, 1f, 0.8f);
                        for (int r = 0; r < 5; r++)
                            RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 0.55f + r * 0.35f, 0), new Vector3(0.95f - r * 0.14f, 0.55f, 0.95f - r * 0.14f), ghost * (0.6f + r * 0.1f));
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 2.15f, 0.05f), new Vector3(0.42f, 0.5f, 0.42f), dark);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(-0.09f, 2.18f, 0.2f), new Vector3(0.09f, 0.12f, 0.06f), eye);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0.09f, 2.18f, 0.2f), new Vector3(0.09f, 0.12f, 0.06f), eye);
                        foreach (float sx in new[] { -1f, 1f })
                            RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(sx * 0.55f, 1.4f, 0.3f), new Vector3(0.12f, 0.6f, 0.12f), ghost).transform.localRotation = Quaternion.Euler(60f, 0, sx * 20f);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 0.15f, -0.1f), new Vector3(0.6f, 0.25f, 0.6f), ghost * 0.5f);
                    }
                    break;
                case "witch":
                    {
                        Color robe = c; robe.a = 1f;
                        Color skin = new Color(0.62f, 0.7f, 0.5f);
                        Color glow = new Color(0.4f, 1f, 0.55f);
                        RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(0, 0.55f, 0), new Vector3(0.95f, 0.55f, 0.95f), robe);
                        RT.Prim(PrimitiveType.Capsule, visual, new Vector3(0, 1.4f, 0), new Vector3(0.6f, 0.4f, 0.42f), robe * 0.85f);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 1.85f, 0), new Vector3(0.3f, 0.34f, 0.3f), skin);
                        RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(0, 2.0f, 0), new Vector3(0.62f, 0.04f, 0.62f), dark);
                        RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(0, 2.2f, 0), new Vector3(0.34f, 0.2f, 0.34f), dark);
                        RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(0.05f, 2.5f, 0), new Vector3(0.18f, 0.12f, 0.18f), dark);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(-0.07f, 1.88f, 0.14f), new Vector3(0.06f, 0.06f, 0.05f), glow);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0.07f, 1.88f, 0.14f), new Vector3(0.06f, 0.06f, 0.05f), glow);
                        RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(0.5f, 1.2f, 0.2f), new Vector3(0.06f, 1.2f, 0.06f), new Color(0.3f, 0.2f, 0.12f));
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0.5f, 2.5f, 0.2f), new Vector3(0.22f, 0.22f, 0.22f), glow);
                        RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(-0.42f, 1.45f, 0.2f), new Vector3(0.12f, 0.35f, 0.12f), robe).transform.localRotation = Quaternion.Euler(60f, 0, -10f);
                    }
                    break;
                case "troll":
                    {
                        Color moss = new Color(0.25f, 0.5f, 0.25f);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 1.8f, 0), new Vector3(2.0f, 2.2f, 1.6f), c);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 2.9f, 0.55f), new Vector3(0.8f, 0.75f, 0.8f), c * 1.1f);
                        RT.Prim(PrimitiveType.Cube, visual, new Vector3(0, 2.7f, 0.95f), new Vector3(0.6f, 0.25f, 0.3f), dark);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(-0.22f, 3.05f, 0.9f), new Vector3(0.12f, 0.12f, 0.1f), new Color(1f, 0.85f, 0.2f));
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0.22f, 3.05f, 0.9f), new Vector3(0.12f, 0.12f, 0.1f), new Color(1f, 0.85f, 0.2f));
                        RT.Prim(PrimitiveType.Cube, visual, new Vector3(-0.15f, 2.55f, 1.1f), new Vector3(0.1f, 0.22f, 0.08f), white);
                        RT.Prim(PrimitiveType.Cube, visual, new Vector3(0.15f, 2.55f, 1.1f), new Vector3(0.1f, 0.22f, 0.08f), white);
                        foreach (float sx in new[] { -1f, 1f })
                        {
                            RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(sx * 1.25f, 1.4f, 0.4f), new Vector3(0.55f, 1.3f, 0.55f), c).transform.localRotation = Quaternion.Euler(20f, 0, sx * 8f);
                            RT.Prim(PrimitiveType.Sphere, visual, new Vector3(sx * 1.35f, 0.35f, 0.8f), new Vector3(0.75f, 0.6f, 0.75f), dark);
                            RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(sx * 0.6f, 0.55f, 0f), new Vector3(0.7f, 0.55f, 0.7f), dark);
                        }
                        RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(1.45f, 0.9f, 1.0f), new Vector3(0.28f, 1.3f, 0.28f), new Color(0.3f, 0.22f, 0.14f)).transform.localRotation = Quaternion.Euler(70f, 0, 0);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(-0.4f, 2.6f, -0.3f), new Vector3(0.9f, 0.3f, 0.8f), moss);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0.5f, 1.4f, 0.7f), new Vector3(0.5f, 0.2f, 0.4f), moss);
                    }
                    break;
                case "necro":
                    {
                        Color robe = c; robe.a = 1f;
                        Color glow = new Color(0.4f, 1f, 0.6f);
                        Color bone = new Color(0.88f, 0.88f, 0.8f);
                        for (int r = 0; r < 4; r++)
                            RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(0, 0.6f + r * 0.55f, 0), new Vector3(1.5f - r * 0.22f, 0.6f, 1.5f - r * 0.22f), robe * (0.8f + r * 0.1f));
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 2.7f, 0), new Vector3(0.5f, 0.55f, 0.5f), bone);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 2.75f, -0.05f), new Vector3(0.7f, 0.75f, 0.7f), dark);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(-0.11f, 2.74f, 0.2f), new Vector3(0.1f, 0.1f, 0.07f), glow);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0.11f, 2.74f, 0.2f), new Vector3(0.1f, 0.1f, 0.07f), glow);
                        foreach (float sx in new[] { -1f, 1f })
                        {
                            RT.Prim(PrimitiveType.Sphere, visual, new Vector3(sx * 0.75f, 2.2f, 0), new Vector3(0.5f, 0.35f, 0.5f), dark);
                            RT.Prim(PrimitiveType.Cube, visual, new Vector3(sx * 0.85f, 2.55f, 0), new Vector3(0.12f, 0.5f, 0.12f), bone);
                            RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(sx * 0.7f, 1.6f, 0.4f), new Vector3(0.18f, 0.5f, 0.18f), robe).transform.localRotation = Quaternion.Euler(60f, 0, 0);
                        }
                        RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(0.75f, 1.8f, 0.8f), new Vector3(0.09f, 1.9f, 0.09f), new Color(0.25f, 0.18f, 0.12f));
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0.75f, 3.8f, 0.8f), new Vector3(0.42f, 0.55f, 0.42f), glow);
                        for (int k = 0; k < 3; k++)
                        {
                            float a = k * 2.1f;
                            RT.Prim(PrimitiveType.Sphere, visual, new Vector3(Mathf.Sin(a) * 1.5f, 1.8f + k * 0.4f, Mathf.Cos(a) * 1.5f), new Vector3(0.28f, 0.3f, 0.28f), bone);
                        }
                    }
                    break;
                case "slime":
                    RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 0.5f, 0), new Vector3(1.4f, 1f, 1.4f), c);
                    RT.Prim(PrimitiveType.Sphere, visual, new Vector3(-0.25f, 0.65f, 0.55f), new Vector3(0.22f, 0.22f, 0.22f), white);
                    RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0.25f, 0.65f, 0.55f), new Vector3(0.22f, 0.22f, 0.22f), white);
                    break;
                case "goblin":
                    RT.Prim(PrimitiveType.Capsule, visual, new Vector3(0, 0.75f, 0), new Vector3(0.7f, 0.75f, 0.7f), c);
                    RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 1.65f, 0), new Vector3(0.6f, 0.6f, 0.6f), c);
                    RT.Prim(PrimitiveType.Cube, visual, new Vector3(-0.42f, 1.7f, 0), new Vector3(0.3f, 0.45f, 0.08f), dark);
                    RT.Prim(PrimitiveType.Cube, visual, new Vector3(0.42f, 1.7f, 0), new Vector3(0.3f, 0.45f, 0.08f), dark);
                    RT.Prim(PrimitiveType.Cube, visual, new Vector3(0.55f, 0.9f, 0.3f), new Vector3(0.15f, 0.9f, 0.15f), new Color(0.35f, 0.22f, 0.12f));
                    break;
                case "bull":
                    RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 1.5f, 0), new Vector3(2.2f, 2.0f, 3.2f), c);
                    RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 2.0f, 1.2f), new Vector3(1.6f, 1.5f, 1.4f), dark);
                    RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 1.7f, 2.0f), new Vector3(1.2f, 1.1f, 1.3f), c);
                    RT.Prim(PrimitiveType.Cube, visual, new Vector3(0, 1.4f, 2.7f), new Vector3(0.7f, 0.5f, 0.5f), dark);
                    foreach (float hx in new[] { -1f, 1f })
                    {
                        var hn = RT.Prim(PrimitiveType.Cube, visual, new Vector3(hx * 0.8f, 2.5f, 1.9f), new Vector3(0.22f, 0.22f, 1.4f), white);
                        hn.transform.localRotation = Quaternion.Euler(-25, hx * 35f, hx * 20f);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(hx * 0.4f, 2.25f, 1.9f), new Vector3(0.2f, 0.2f, 0.2f), red);
                        foreach (float lz in new[] { -1.0f, 1.0f })
                            RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(hx * 0.8f, 0.6f, lz), new Vector3(0.5f, 0.6f, 0.5f), dark);
                    }
                    break;
                case "cerberus":
                    var cb = RT.Prim(PrimitiveType.Capsule, visual, new Vector3(0, 1.1f, 0), new Vector3(1.1f, 1.3f, 1.1f), c);
                    cb.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    foreach (float hx in new[] { -0.75f, 0f, 0.75f })
                    {
                        float hy = hx == 0f ? 1.9f : 1.65f;
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(hx, hy, 1.5f), new Vector3(0.8f, 0.8f, 0.9f), c);
                        RT.Prim(PrimitiveType.Cube, visual, new Vector3(hx, hy - 0.15f, 2.1f), new Vector3(0.38f, 0.32f, 0.7f), dark);
                        RT.Prim(PrimitiveType.Cube, visual, new Vector3(hx - 0.2f, hy + 0.5f, 1.4f), new Vector3(0.16f, 0.4f, 0.12f), dark);
                        RT.Prim(PrimitiveType.Cube, visual, new Vector3(hx + 0.2f, hy + 0.5f, 1.4f), new Vector3(0.16f, 0.4f, 0.12f), dark);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(hx - 0.18f, hy + 0.1f, 1.95f), new Vector3(0.12f, 0.12f, 0.12f), red);
                        RT.Prim(PrimitiveType.Sphere, visual, new Vector3(hx + 0.18f, hy + 0.1f, 1.95f), new Vector3(0.12f, 0.12f, 0.12f), red);
                    }
                    foreach (float lx in new[] { -0.5f, 0.5f })
                        foreach (float lz in new[] { -0.9f, 0.9f })
                            RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(lx, 0.5f, lz), new Vector3(0.36f, 0.5f, 0.36f), dark);
                    break;
                case "wolf":
                    var body = RT.Prim(PrimitiveType.Capsule, visual, new Vector3(0, 1.0f, 0), new Vector3(0.9f, 1.1f, 0.9f), c);
                    body.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 1.35f, 1.35f), new Vector3(0.8f, 0.8f, 0.9f), c);
                    RT.Prim(PrimitiveType.Cube, visual, new Vector3(0, 1.2f, 1.9f), new Vector3(0.4f, 0.35f, 0.7f), dark);
                    RT.Prim(PrimitiveType.Cube, visual, new Vector3(-0.28f, 1.85f, 1.2f), new Vector3(0.18f, 0.4f, 0.12f), dark);
                    RT.Prim(PrimitiveType.Cube, visual, new Vector3(0.28f, 1.85f, 1.2f), new Vector3(0.18f, 0.4f, 0.12f), dark);
                    RT.Prim(PrimitiveType.Sphere, visual, new Vector3(-0.2f, 1.5f, 1.75f), new Vector3(0.12f, 0.12f, 0.12f), red);
                    RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0.2f, 1.5f, 1.75f), new Vector3(0.12f, 0.12f, 0.12f), red);
                    foreach (float lx in new[] { -0.4f, 0.4f })
                        foreach (float lz in new[] { -0.7f, 0.8f })
                            RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(lx, 0.45f, lz), new Vector3(0.28f, 0.45f, 0.28f), dark);
                    var tail = RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(0, 1.3f, -1.5f), new Vector3(0.22f, 0.7f, 0.22f), dark);
                    tail.transform.localRotation = Quaternion.Euler(-55, 0, 0);
                    break;
                case "chicken":
                    RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 0.45f, 0), new Vector3(0.5f, 0.45f, 0.7f), c);
                    RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 0.78f, 0.3f), new Vector3(0.24f, 0.26f, 0.24f), c);
                    RT.Prim(PrimitiveType.Cube, visual, new Vector3(0, 0.76f, 0.46f), new Vector3(0.08f, 0.07f, 0.14f), new Color(0.95f, 0.65f, 0.15f));
                    RT.Prim(PrimitiveType.Cube, visual, new Vector3(0, 0.95f, 0.3f), new Vector3(0.05f, 0.1f, 0.14f), red);
                    RT.Prim(PrimitiveType.Cube, visual, new Vector3(0, 0.62f, -0.4f), new Vector3(0.08f, 0.3f, 0.25f), c * 0.9f);
                    foreach (float lx in new[] { -0.1f, 0.1f }) RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(lx, 0.14f, 0f), new Vector3(0.04f, 0.14f, 0.04f), new Color(0.95f, 0.65f, 0.15f));
                    break;
                case "sheep":
                    RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 0.85f, 0), new Vector3(0.95f, 0.85f, 1.15f), c);
                    RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 1.0f, 0.7f), new Vector3(0.36f, 0.4f, 0.42f), new Color(0.22f, 0.2f, 0.2f));
                    foreach (float ex in new[] { -0.22f, 0.22f }) RT.Prim(PrimitiveType.Cube, visual, new Vector3(ex, 1.1f, 0.62f), new Vector3(0.18f, 0.06f, 0.1f), new Color(0.22f, 0.2f, 0.2f));
                    foreach (float lx in new[] { -0.28f, 0.28f }) foreach (float lz in new[] { -0.4f, 0.4f })
                        RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(lx, 0.25f, lz), new Vector3(0.1f, 0.25f, 0.1f), new Color(0.22f, 0.2f, 0.2f));
                    break;
                case "goat":
                    RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 0.8f, 0), new Vector3(0.6f, 0.6f, 1.05f), c);
                    RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 1.15f, 0.62f), new Vector3(0.3f, 0.34f, 0.4f), c * 1.1f);
                    RT.Prim(PrimitiveType.Cube, visual, new Vector3(0, 0.95f, 0.85f), new Vector3(0.1f, 0.1f, 0.12f), white * 0.8f);
                    foreach (float hx in new[] { -0.1f, 0.1f }) { var hn = RT.Prim(PrimitiveType.Cube, visual, new Vector3(hx, 1.4f, 0.55f), new Vector3(0.06f, 0.3f, 0.06f), new Color(0.85f, 0.8f, 0.7f)); hn.transform.localRotation = Quaternion.Euler(-20, 0, hx > 0 ? -15 : 15); }
                    foreach (float lx in new[] { -0.2f, 0.2f }) foreach (float lz in new[] { -0.35f, 0.35f })
                        RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(lx, 0.25f, lz), new Vector3(0.08f, 0.25f, 0.08f), dark);
                    break;
                case "cow":
                    RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 1.0f, 0), new Vector3(1.0f, 0.95f, 1.7f), c);
                    RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0.3f, 1.15f, 0.2f), new Vector3(0.5f, 0.5f, 0.6f), new Color(0.2f, 0.17f, 0.15f));
                    RT.Prim(PrimitiveType.Sphere, visual, new Vector3(-0.25f, 1.0f, -0.4f), new Vector3(0.45f, 0.45f, 0.5f), new Color(0.2f, 0.17f, 0.15f));
                    RT.Prim(PrimitiveType.Cube, visual, new Vector3(0, 1.15f, 1.05f), new Vector3(0.48f, 0.5f, 0.6f), c);
                    RT.Prim(PrimitiveType.Cube, visual, new Vector3(0, 0.98f, 1.38f), new Vector3(0.36f, 0.24f, 0.2f), new Color(0.92f, 0.7f, 0.68f));
                    foreach (float hx in new[] { -0.28f, 0.28f }) RT.Prim(PrimitiveType.Cube, visual, new Vector3(hx, 1.45f, 1.0f), new Vector3(0.08f, 0.2f, 0.08f), new Color(0.9f, 0.85f, 0.75f));
                    foreach (float lx in new[] { -0.35f, 0.35f }) foreach (float lz in new[] { -0.6f, 0.6f })
                        RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(lx, 0.35f, lz), new Vector3(0.18f, 0.35f, 0.18f), new Color(0.3f, 0.26f, 0.22f));
                    break;
                case "boar":
                    RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 1.1f, 0), new Vector3(1.9f, 1.7f, 2.8f), c);
                    RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 1.0f, 1.5f), new Vector3(1.2f, 1.1f, 1.2f), dark);
                    RT.Prim(PrimitiveType.Cube, visual, new Vector3(0, 0.85f, 2.15f), new Vector3(0.6f, 0.5f, 0.5f), dark);
                    var tuskL = RT.Prim(PrimitiveType.Cube, visual, new Vector3(-0.4f, 0.8f, 2.05f), new Vector3(0.1f, 0.1f, 0.7f), white);
                    tuskL.transform.localRotation = Quaternion.Euler(-30, 20, 0);
                    var tuskR = RT.Prim(PrimitiveType.Cube, visual, new Vector3(0.4f, 0.8f, 2.05f), new Vector3(0.1f, 0.1f, 0.7f), white);
                    tuskR.transform.localRotation = Quaternion.Euler(-30, -20, 0);
                    RT.Prim(PrimitiveType.Sphere, visual, new Vector3(-0.3f, 1.3f, 1.9f), new Vector3(0.14f, 0.14f, 0.14f), red);
                    RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0.3f, 1.3f, 1.9f), new Vector3(0.14f, 0.14f, 0.14f), red);
                    foreach (float lx in new[] { -0.6f, 0.6f })
                        foreach (float lz in new[] { -0.9f, 0.9f })
                            RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(lx, 0.4f, lz), new Vector3(0.35f, 0.4f, 0.35f), dark);
                    break;
            }

            rends = visual.GetComponentsInChildren<Renderer>();
            baseColors = new Color[rends.Length];
            for (int i = 0; i < rends.Length; i++) baseColors[i] = rends[i].material.color;
        }


        void BuildBandit(Color cloth)
        {
            bool leader = def.shape == "leader", archer = def.shape == "archer";
            Color leather = new Color(0.25f, 0.16f, 0.1f), skin = new Color(0.78f, 0.6f, 0.48f), metal = new Color(0.58f, 0.6f, 0.64f);
            Color dark = new Color(0.1f, 0.09f, 0.09f), pants = cloth * 0.7f, hood = cloth * 0.8f;
            pants.a = 1f; hood.a = 1f; cloth.a = 1f;
            legs = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                var hip = new GameObject("Leg" + i).transform;
                hip.SetParent(visual, false);
                hip.localPosition = new Vector3(i == 0 ? -0.17f : 0.17f, 0.92f, 0f);
                RT.Prim(PrimitiveType.Cylinder, hip, new Vector3(0, -0.44f, 0), new Vector3(0.2f, 0.44f, 0.2f), pants);
                RT.Prim(PrimitiveType.Cube, hip, new Vector3(0, -0.86f, 0.05f), new Vector3(0.22f, 0.16f, 0.34f), leather);
                legs[i] = hip;
            }
            RT.Prim(PrimitiveType.Capsule, visual, new Vector3(0, 1.28f, 0), new Vector3(0.58f, 0.42f, 0.4f), cloth);
            RT.Prim(PrimitiveType.Cube, visual, new Vector3(0, 0.98f, 0), new Vector3(0.6f, 0.1f, 0.42f), leather);
            RT.Prim(PrimitiveType.Cube, visual, new Vector3(0, 1.3f, 0.2f), new Vector3(0.4f, 0.5f, 0.06f), leather);        // rompi kulit
            RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 1.8f, 0), new Vector3(0.3f, 0.32f, 0.3f), skin);
            RT.Prim(PrimitiveType.Sphere, visual, new Vector3(0, 1.84f, -0.03f), new Vector3(0.38f, 0.36f, 0.38f), hood);   // tudung
            RT.Prim(PrimitiveType.Cube, visual, new Vector3(0, 1.74f, 0.14f), new Vector3(0.27f, 0.13f, 0.06f), dark);               // masker
            foreach (float sx in new[] { -1f, 1f })
            {
                var arm = RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(sx * 0.4f, 1.28f, 0.05f), new Vector3(0.14f, 0.36f, 0.14f), cloth);
                arm.transform.localRotation = Quaternion.Euler(archer ? 40f : 12f, 0, sx * 10f);
            }
            if (archer)
            {
                // busur + tabung panah
                RT.Prim(PrimitiveType.Cube, visual, new Vector3(-0.5f, 1.3f, 0.35f), new Vector3(0.05f, 1.0f, 0.05f), leather);
                RT.Prim(PrimitiveType.Cube, visual, new Vector3(-0.5f, 1.85f, 0.32f), new Vector3(0.05f, 0.3f, 0.05f), leather).transform.localRotation = Quaternion.Euler(20, 0, 0);
                RT.Prim(PrimitiveType.Cube, visual, new Vector3(-0.5f, 0.75f, 0.32f), new Vector3(0.05f, 0.3f, 0.05f), leather).transform.localRotation = Quaternion.Euler(-20, 0, 0);
                RT.Prim(PrimitiveType.Cube, visual, new Vector3(-0.5f, 1.3f, 0.33f), new Vector3(0.01f, 1.1f, 0.01f), new Color(0.9f, 0.9f, 0.85f));
                RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(0.18f, 1.4f, -0.28f), new Vector3(0.14f, 0.3f, 0.14f), leather).transform.localRotation = Quaternion.Euler(0, 0, 20);
            }
            else
            {
                // pedang
                var blade = RT.Prim(PrimitiveType.Cube, visual, new Vector3(0.5f, 1.2f, 0.45f), new Vector3(0.06f, 0.05f, leader ? 1.15f : 0.9f), metal);
                blade.transform.localRotation = Quaternion.Euler(-25, 0, 0);
                RT.Prim(PrimitiveType.Cube, visual, new Vector3(0.5f, 1.2f, 0.0f), new Vector3(0.22f, 0.05f, 0.05f), leather);
                if (!leader) RT.Prim(PrimitiveType.Cylinder, visual, new Vector3(-0.5f, 1.2f, 0.25f), new Vector3(0.34f, 0.04f, 0.34f), leather).transform.localRotation = Quaternion.Euler(90, 0, 0);   // perisai kayu
            }
            if (leader)
            {
                foreach (float sx in new[] { -1f, 1f })
                {
                    RT.Prim(PrimitiveType.Sphere, visual, new Vector3(sx * 0.4f, 1.58f, 0), new Vector3(0.3f, 0.2f, 0.3f), metal);              // pelindung bahu
                    RT.Prim(PrimitiveType.Cube, visual, new Vector3(sx * 0.17f, 2.06f, 0), new Vector3(0.05f, 0.3f, 0.05f), new Color(0.9f, 0.88f, 0.8f)).transform.localRotation = Quaternion.Euler(0, 0, -sx * 25);   // tanduk helm
                }
                RT.Prim(PrimitiveType.Cube, visual, new Vector3(0, 1.1f, -0.26f), new Vector3(0.7f, 1.0f, 0.05f), new Color(0.45f, 0.08f, 0.08f));   // jubah
            }
        }

        bool tinted;

        void Tint(Color t, float amount)
        {
            if (rig != null) { tinted = amount > 0.001f; rig.Tint(t, amount); return; }        // palet bersama (MonsterKit)
            if (swap != null) { tinted = amount > 0.001f; swap.Flash(t, amount); return; }     // model pengguna: MaterialPropertyBlock
            if (rends == null) return;
            tinted = amount > 0.001f;
            for (int i = 0; i < rends.Length; i++) rends[i].material.color = Color.Lerp(baseColors[i], t, amount);
        }

        /// <summary>Buang semua anak visual (dipakai saat pembangunan model gagal di tengah jalan, sebelum model cadangan dibuat).</summary>
        void ClearVisual()
        {
            if (visual == null) return;
            for (int i = visual.childCount - 1; i >= 0; i--) DestroyImmediate(visual.GetChild(i).gameObject);
        }

        public Vector3 HeadPoint { get { return transform.position + Vector3.up * (def != null ? def.height * 0.88f : 1.6f); } }

        /// <summary>True bila pemain (Archer/penyihir) sedang membidik dan garis bidik kamera melewati kepala monster ini.</summary>
        bool IsHeadshot(GameState gs)
        {
            if (def == null || def.height < 0.9f) return false;
            if (!(gs.job == "Archer" || gs.IsMage)) return false;
            if (!ThirdPersonCamera.Aiming && !ThirdPersonCamera.FirstPerson) return false;
            var cam = Camera.main; if (cam == null) return false;
            Vector3 o = cam.transform.position, d = cam.transform.forward, c = HeadPoint;
            float t = Vector3.Dot(c - o, d);
            if (t < 1f || t > 90f) return false;
            float rad = Mathf.Max(0.3f, def.radius * 0.65f) + t * 0.008f;       // toleransi sedikit untuk jarak jauh
            return (o + d * t - c).sqrMagnitude <= rad * rad;
        }

        public void TakeDamage(float dmg, bool byPlayer = true)
        {
            if (IsDead || def == null) return;
            dmg *= damageTakenMult;
            if (byPlayer) dmg *= Cards.DealtMul(def.id);
            if (byPlayer) dmg *= Combat2.Hit(this, dmg) * Challenges.DealtMul;
            if (byPlayer) { float sn = Stealth.SneakAttackMul(this); if (sn > 1f) { dmg *= sn; CombatFx.PopText(HeadPoint + Vector3.up * 0.8f, Loc.T("SERANGAN DIAM-DIAM!"), new Color(0.7f, 0.85f, 1f)); } }
            var gsa = GameState.I;
            bool isCrit = false, isHead = false;
            if (byPlayer && gsa != null)
            {
                // CRITICAL: dasar 5% + efek gear/passive (%), x1.75
                float crit = 5f + gsa.AttachEffect("crit") + gsa.Pas("crit");
                if (Random.value * 100f < crit) { isCrit = true; dmg *= 1.75f; CombatFx.ShockRing(HitPoint, 1.6f, CombatFx.SwordGold, 0.25f); }
                // HEADSHOT: pemanah/penyihir yang membidik tepat ke kepala (garis bidik kamera menembus bola kepala)
                if (IsHeadshot(gsa)) { isHead = true; dmg *= gsa.job == "Archer" ? 1.8f : 1.5f; CombatFx.ShockRing(HeadPoint, 1.1f, new Color(1f, 0.35f, 0.25f, 0.9f), 0.22f); }
                float ls = gsa.AttachEffect("lifesteal") + gsa.Pas("lifesteal") * 100f;
                if (ls > 0f && PlayerCombat.I != null && !PlayerCombat.I.IsDead) { PlayerCombat.I.Heal(dmg * ls / 100f); if (SkillAction.BloodbathActive) SkillAction.BloodPulse(PlayerCombat.I, HitPoint); }
            }
            if (DevTools.OneHit) dmg = Mathf.Max(dmg, hp + 1f);
            hp -= dmg;
            if (netPuppet) { CoopNet.SendDamage(netId, dmg); if (hp < 1f) hp = 1f; }        // tamu: damage diterapkan host, kematian datang dari host
            if (def.id == "dummy")
            {
                if (hp < maxHp * 0.3f) hp = maxHp;          // boneka latihan tidak pernah hancur
                StarterQuests.OnEvent("dummy", "");
                if (byPlayer) TrainingStation.OnDummyHit(this, dmg);
            }
            hitFlash = 0.12f;
            if (swap != null && hp > 0f) swap.Hurt();
            Audio.Sfx(dmg >= 28f ? "hit_heavy" : "hit", transform.position);
            if (hp > 0f && Random.value < 0.35f) Audio.Sfx("monster_hurt", transform.position);
            if (def.passive) fleeUntil = Time.time + 5f;
            else if (state == State.Idle || state == State.Return) state = State.Chase;
            bool heavy = dmg >= 28f || isCrit || isHead;
            var gsx = GameState.I;
            bool arch = gsx != null && gsx.job == "Archer";
            CombatFx.HitSpark(HitPoint, arch ? CombatFx.BowCol : (heavy ? CombatFx.SwordGold : CombatFx.SwordCol), heavy ? 1.5f : 1f, heavy ? 22 : 12);
            Color dc = isHead ? new Color(1f, 0.35f, 0.25f) : isCrit ? new Color(1f, 0.6f, 0.1f) : heavy ? new Color(1f, 0.85f, 0.3f) : Color.white;
            CombatFx.DamageNumber((isHead ? HeadPoint : HitPoint + Vector3.up * (def.height * 0.5f)), dmg, heavy, dc);
            if (isHead) { CombatFx.PopText(HeadPoint + Vector3.up * 0.9f, "HEADSHOT!", new Color(1f, 0.35f, 0.25f)); Audio.Sfx("hit_heavy", transform.position); }
            else if (isCrit) CombatFx.PopText(HitPoint + Vector3.up * (def.height * 0.5f + 0.7f), "CRIT!", new Color(1f, 0.7f, 0.15f));
            AnimeLook.Hit(heavy ? 1f : 0.3f);
            AnimeLook.Impact(HitPoint, arch ? CombatFx.BowCol : (heavy ? CombatFx.SwordGold : CombatFx.SwordCol), heavy);
            if (hp <= 0f) Die();
        }

        float slowUntil, slowMult = 1f;
        /// <summary>Menghuyungkan monster sejenak (tidak bergerak/menyerang). Dipakai Parry, Shield Bash, Net Trap.</summary>
        public void Stagger(float seconds)
        {
            if (IsDead || def == null) return;
            state = State.Recover;
            stateUntil = Mathf.Max(stateUntil, Time.time + seconds);
            Tint(Color.white, 0f);
            CombatFx.Burst(HitPoint + Vector3.up * 0.6f, new Color(1f, 0.95f, 0.5f), 8, 0.4f, 1.6f, 0.6f, 0.12f);
        }

        /// <summary>Memperlambat gerak monster (mult 0.4 = 40% kecepatan) selama seconds.</summary>
        public void Slow(float mult, float seconds) { slowMult = mult; slowUntil = Time.time + seconds; }

        public void Knock(Vector3 dir, float dist)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            if (def != null && def.passive && PassiveBlocked(dir.normalized, dist)) return;
            transform.position += dir.normalized * dist;
        }

        void Die()
        {
            IsDead = true;
            if (!netPuppet) CoopNet.OnMonsterDied(this);
            Audio.Sfx(isBoss ? "boss_roar" : "monster_die", transform.position);
            if (isBoss) Audio.Sfx("victory");
            hp = 0f;
            CombatFx.Burst(HitPoint, def.color, 18, 0.6f, 3f, 0.8f, 0.25f);
            CombatFx.Dust(transform.position, 10);
            RoyalFx.MonsterDeath(HitPoint, def.color, isBoss, def.size);   // v35z102n6
            Tint(new Color(0.2f, 0.2f, 0.2f), 0.5f);
            bool animDeath = swap != null && swap.Die();        // model pengguna dengan animasi mati: Animator yang merobohkannya
            if (visual != null && !animDeath)
            {
                visual.localRotation = Quaternion.Euler(0, 0, 80f);
                visual.localPosition = new Vector3(0, def.radius * 0.6f, 0);
            }
            var cap = GetComponent<CapsuleCollider>();
            if (cap != null) cap.enabled = false;
            if (def.passive) { var carve = gameObject.AddComponent<CarveSpot>(); carve.Setup(def); }      // hewan buruan: tetap di-carve (daging)
            else if (def.id != "dummy") WorldDrops.MonsterDeath(def, transform.position, isBoss);        // monster: bahan jatuh sendiri (penempaan), terambil saat didekati
            if (def.id != "dummy") HuntManager.Report("kill", def.id);
            if (def.passive)
            {
                if (GameState.I != null) GameState.I.AddStat("animals_hunted");
                if (onDeath != null) onDeath(this);
                if (TownUI.I != null) TownUI.I.Toast(Name + " tumbang. Tekan E untuk ambil daging.");
                return;
            }
            if (def.id != "dummy" && GameState.I != null)
            {
                // drop attachment: boss paling sering, monster besar jarang, monster biasa sangat jarang
                bool big = isBoss || def.carves >= 3;
                float chance = isBoss ? 0.5f : (big ? 0.12f : 0.04f);
                if (Random.value < chance) GameState.I.GiveRandomAttachment(isBoss ? 2 : (big ? 1 : 0));
            }
            var gsk = GameState.I;
            if (gsk != null)
            {
                gsk.AddStat("kills");
                PartnerPersona.OnKill(def, isBoss);
                WeaponLevel.OnKill(def, isBoss);
                Cards.OnKill(def, isBoss);
                WorldReact.OnKill();
                if (gsk.IsMage && PlayerCombat.I != null) { float mk = gsk.AttachEffect("manakill"); if (mk > 0f) PlayerCombat.I.RestoreMana(mk); }
                if (def.id != "dummy")
                {
                    gsk.AddStat("bk_" + def.id);
                    gsk.AddStat("bl_" + def.id + "_" + GameState.RegionKeyFor(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name));
                }
                if (gsk.level < 5)
                {
                    // bonus pemula: kill memberi EXP supaya Lv 0 -> 5 bisa dicapai di Hutan Pemula
                    int xp = Mathf.Max(1, Mathf.RoundToInt(def.hp / 20f));
                    gsk.AddExp(xp);
                    if (TownUI.I != null) TownUI.I.Toast("+" + xp + " EXP");
                }
                if (gsk.level >= 5 && gsk.level < GameState.LevelCap) { int kx = Balance.KillExp(def, isBoss); if (kx > 0) gsk.AddExp(kx); }
                gsk.AddStat("kills_" + GameState.RegionKeyFor(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name));
                if (def.large) gsk.AddStat("big_kills");
                if (isBoss) { gsk.AddStat("boss_kills"); GearTiers.BossDrop(def.id); HiddenBosses.OnKill(def.id); Challenges.OnBossKill(this); }
            }
            if (onDeath != null) onDeath(this);
            if (TownUI.I != null) TownUI.I.Toast(Name + " dikalahkan!" + (isBoss ? "" : " Ambil drop-nya."));
        }

        void Update()
        {
            if (IsDead || def == null) return;

            if (hitFlash > 0f)
            {
                hitFlash -= Time.deltaTime;
                Tint(Color.white, 0.6f);
            }
            else if (tinted && state != State.Telegraph) Tint(Color.white, 0f);

            if (netPuppet) { NetPuppetTick(); return; }
            if (def.passive) { PassiveUpdate(); return; }
            bool enraged = hp < maxHp * 0.5f;
            float speed = def.speed * speedMult * (enraged ? 1.2f : 1f) * (Time.time < slowUntil ? slowMult : 1f) * (Swimming && !WaterImmune() ? 0.5f : 1f);

            // target: pemain atau partner (tank menarik perhatian lebih kuat)
            var pc = PlayerCombat.I;
            bool playerOk = pc != null && !pc.IsDead;
            Vector3 tgtPos = Vector3.zero;
            float dist = float.MaxValue;
            AllyUnit ally = null;
            if (playerOk)
            {
                tgtPos = pc.transform.position;
                Vector3 dp = tgtPos - transform.position; dp.y = 0f;
                dist = dp.magnitude;
            }
            float ad;
            var al = AllyUnit.Nearest(transform.position, def.aggro * 1.5f, out ad);
            if (al != null && (!playerOk || ad * al.TauntWeight < dist))
            {
                ally = al; tgtPos = al.transform.position; dist = ad;
            }
            RemotePlayer rp = null;
            if (CoopNet.IsHost)
            {
                float rd; var cand = RemotePlayer.Nearest(transform.position, def.aggro * 1.5f, out rd);
                if (cand != null && rd < dist) { rp = cand; ally = null; tgtPos = cand.transform.position; dist = rd; }
            }
            bool hasTarget = playerOk || ally != null || rp != null;
            float denDist = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(den.x, 0, den.z));

            switch (state)
            {
                case State.Idle:
                    if (hasTarget && dist < def.aggro * (ally == null ? Stealth.DetectMul(this) : 1f)) { state = State.Chase; if (fb != null) fb.Alert(); break; }
                    if (Time.time > nextWander)
                    {
                        Vector2 r = Random.insideUnitCircle * 12f;
                        wanderTarget = den + new Vector3(r.x, 0, r.y);
                        nextWander = Time.time + Random.Range(4f, 8f);
                    }
                    MoveToward(wanderTarget, speed * 0.35f);
                    break;

                case State.Chase:
                    if (!hasTarget || dist > def.aggro * 2.4f || (!noLeash && denDist > 90f)) { state = State.Return; break; }
                    if (dist <= def.reach)
                    {
                        state = State.Telegraph;
                        Audio.Sfx("monster_growl", transform.position);
                        stateUntil = Time.time + def.telegraph;
                        if (fb != null) fb.BeginTelegraph(def.telegraph, def.reach);
                        break;
                    }
                    MoveToward(tgtPos, speed);
                    break;

                case State.Telegraph:
                    if (hasTarget) Face(tgtPos);
                    Tint(new Color(1f, 0.15f, 0.1f), 0.55f + 0.3f * Mathf.Sin(Time.time * 30f));
                    if (Time.time >= stateUntil)
                    {
                        if (rig != null) rig.Lunge();
                        if (fb != null) fb.Strike(tgtPos, hasTarget && dist <= def.reach + 1.2f);
                        if (hasTarget && dist <= def.reach + 1.2f)
                        {
                            float dmg = def.atk * atkMult * (enraged ? 1.15f : 1f);
                            Audio.Sfx("monster_attack", transform.position);
                            if (def.ranged) RT.Line(transform.position + Vector3.up * 1.4f, tgtPos + Vector3.up * 0.9f, 0.18f);
                            if (rp != null) rp.Hit(dmg * Cards.TakenMul(def.id), transform.position);
                            else if (ally != null) ally.TakeDamage(dmg);
                            else if (playerOk) { bool inv = pc.Invulnerable; pc.TakeDamage(dmg * Cards.TakenMul(def.id), transform.position); if (!inv) Combat2.OnMonsterHit(def, isBoss); }
                        }
                        Tint(Color.white, 0f);
                        state = State.Recover;
                        stateUntil = Time.time + def.recover;
                    }
                    break;

                case State.Recover:
                    if (Time.time >= stateUntil) state = State.Chase;
                    break;

                case State.Return:
                    MoveToward(den, speed);
                    hp = Mathf.Min(maxHp, hp + maxHp * 0.05f * Time.deltaTime);
                    if (denDist < 3f) state = State.Idle;
                    else if (hasTarget && dist < def.aggro * 0.6f && denDist < 40f) state = State.Chase;
                    break;
            }
            Snap();
            AnimateLegs();
        }

        float fleeUntil;
        void PassiveUpdate()
        {
            Vector3 pp = PlayerCombat.I != null ? PlayerCombat.I.transform.position : den + Vector3.right * 999f;
            Vector3 away = transform.position - pp; away.y = 0f;
            float dist = away.magnitude;
            bool fleeing = Time.time < fleeUntil || dist < 2.2f;
            float before = (transform.position - lastPos).magnitude;
            if (fleeing)
            {
                Vector3 fd = away.sqrMagnitude > 0.01f ? away.normalized : transform.forward;
                // kabur ke arah bebas: coba arah menjauh, lalu geser sudut bila terhalang dinding/pagar/pohon
                float[] turns = { 0f, 40f, -40f, 80f, -80f, 130f, -130f };
                foreach (float ang in turns)
                {
                    Vector3 cand = Quaternion.Euler(0f, ang, 0f) * fd;
                    if (!PassiveBlocked(cand, 1.2f)) { PassiveMove(cand, def.speed * (Time.time < fleeUntil ? 2.0f : 1.2f)); break; }
                }
            }
            else
            {
                if (Time.time > nextWander)
                {
                    Vector2 r = Random.insideUnitCircle * 9f;
                    wanderTarget = den + new Vector3(r.x, 0, r.y);
                    nextWander = Time.time + Random.Range(3f, 9f);
                }
                if (Random.value < 0.002f) nextWander = Time.time + Random.Range(2f, 5f);   // berhenti merumput sejenak
                Vector3 wd = wanderTarget - transform.position; wd.y = 0f;
                if (wd.magnitude > 0.4f)
                {
                    Vector3 wdir = wd.normalized;
                    if (PassiveBlocked(wdir, 1.0f)) nextWander = 0f;                 // terhalang: pilih tujuan lain
                    else PassiveMove(wdir, def.speed * 0.3f);
                }
            }
            if (visual != null)
            {
                float spd = (transform.position - lastPos).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
                if (rig != null || swap != null)
                {
                    // model baru: gerak sendiri (langkah, napas, kepala merunduk merumput); visual tetap di tempat
                    if (rig != null) rig.Tick(spd, Time.deltaTime, false);
                    if (swap != null) swap.Tick(spd, false, Time.deltaTime);
                }
                else visual.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 0.05f * Mathf.Clamp01(spd / 1.5f), 0f);
            }
            lastPos = transform.position;
            Snap();
        }

        // hewan pasif tidak boleh menembus dinding/pagar/pohon: cek dengan sphere cast sebelum melangkah
        bool PassiveBlocked(Vector3 dir, float dist)
        {
            Vector3 o = transform.position + Vector3.up * 0.7f;
            float r = Mathf.Max(0.25f, def.radius * 0.8f);
            var hits = Physics.SphereCastAll(o, r, dir, dist + r, ~0, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
            {
                var c = h.collider;
                if (c == null || c.transform.IsChildOf(transform)) continue;
                if (c.GetComponent<Terrain>() != null || c.GetType().Name == "TerrainCollider") continue;
                if (h.point.y < transform.position.y + 0.15f && h.normal.y > 0.6f) continue;      // lantai/tanah datar
                return true;
            }
            return false;
        }

        void PassiveMove(Vector3 dir, float speed)
        {
            Face(transform.position + dir);
            transform.position += dir * speed * Time.deltaTime;
        }

        void AnimateLegs()
        {
            if (rig != null || swap != null)
            {
                float vs = (transform.position - lastPos).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
                lastPos = transform.position;
                bool wnd = netPuppet ? netState == 2 : state == State.Telegraph;
                if (rig != null) rig.Tick(vs, Time.deltaTime, wnd);
                if (swap != null) swap.Tick(vs, wnd, Time.deltaTime);
                return;
            }
            if (legs == null) { lastPos = transform.position; return; }
            float spd = (transform.position - lastPos).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            lastPos = transform.position;
            walkT += spd * Time.deltaTime * 1.6f;
            float amp = Mathf.Clamp01(spd / 2.2f) * 38f;
            legs[0].localRotation = Quaternion.Euler(Mathf.Sin(walkT * 3f) * amp, 0f, 0f);
            legs[1].localRotation = Quaternion.Euler(-Mathf.Sin(walkT * 3f) * amp, 0f, 0f);
        }

        void Face(Vector3 target)
        {
            Vector3 d = target - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d.normalized), 8f * Time.deltaTime);
        }

        void MoveToward(Vector3 target, float speed)
        {
            Vector3 d = target - transform.position;
            d.y = 0f;
            if (d.magnitude < 0.3f) return;
            Face(target);
            transform.position += d.normalized * speed * Time.deltaTime;
        }
    }

    /// <summary>Bangkai monster: tekan E untuk carve (jumlah carve mengikuti ukuran monster).</summary>
    public class CarveSpot : Interactable
    {
        MonsterDef def;
        int remaining;
        float despawnAt;

        public void Setup(MonsterDef d)
        {
            def = d;
            remaining = d.carves;
            prompt = "Carve " + d.name + " (" + remaining + ")";
            radius = 3.8f + d.radius;
            despawnAt = Time.time + 180f;
        }

        void Update()
        {
            if (Time.time > despawnAt) Destroy(gameObject);
        }

        public override void Interact()
        {
            var gs = GameState.I;
            if (def == null || remaining <= 0) return;

            string id = (def.passive && remaining == def.carves) ? def.loot[0]      // carve pertama hewan buruan selalu daging
                : (def.rare != "" && Random.value < def.rareChance) ? def.rare : def.loot[Random.Range(0, def.loot.Length)];
            if (!gs.HasSpace(id, 1)) { TownUI.I.Toast("Tas penuh! Buang sesuatu dulu (Tab)."); return; }
            gs.AddItem(id, 1);
            gs.AddStat("bd_" + def.id + "_" + id);
            remaining--;
            TownUI.I.Toast("Carve: " + ItemDB.Get(id).name);
            prompt = "Carve " + def.name + " (" + remaining + ")";
            if (remaining <= 0)
            {
                enabled = false;
                Destroy(gameObject, 4f);
            }
        }
    }
}
