using System;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Pengaturan model asli (dibuat oleh wizard: menu Hunter Guild > Setup Karakter &amp; NPC).
    /// Disimpan di Assets/Resources/HG_Models/Config.asset. Bila tidak ada / kosong, game memakai karakter prosedural.
    /// </summary>
    public class HGModelConfig : ScriptableObject
    {
        public const string ResPath = "HG_Models/Config";

        [Serializable] public class NpcEntry { public GameObject prefab; public int gender; public int race = -1; }       // gender: 0 bebas, 1 pria, 2 wanita; race: -1 bebas, 0 manusia, 1 dwarf, 2 elf, 3 beast
        [Serializable] public class ArmorVariant { public int tier = 1; public int gender; public GameObject prefab; }     // tier armor badan 1..4

        public GameObject playerMale, playerFemale;
        public RuntimeAnimatorController playerController, npcController;
        public float playerHeight = 1.75f, npcHeight = 1.70f;
        public List<NpcEntry> npcs = new List<NpcEntry>();
        public List<ArmorVariant> armorVariants = new List<ArmorVariant>();

        // properti senjata (opsional); kosong = pakai model senjata prosedural
        public GameObject swordProp, bowProp, shieldProp;
        public Vector3 swordPos = new Vector3(0f, 0.02f, 0f), swordEuler = new Vector3(0f, 0f, -90f);
        public Vector3 bowPos = Vector3.zero, bowEuler = new Vector3(0f, 0f, 90f);
        public Vector3 shieldPos = new Vector3(0f, 0.04f, 0f), shieldEuler = new Vector3(0f, 90f, 90f);

        public static HGModelConfig Load() { return Resources.Load<HGModelConfig>(ResPath); }
        public static bool UsesExternalPlayer { get { var c = Load(); return c != null && c.HasPlayer; } }

        public bool HasPlayer { get { return playerMale != null || playerFemale != null; } }
        public bool HasNpcs { get { foreach (var n in npcs) if (n != null && n.prefab != null) return true; return false; } }

        /// <summary>armorTier: 0 = tanpa armor badan. Mengembalikan varian armor bila ada, kalau tidak model dasar.</summary>
        public GameObject PickPlayer(bool female, int armorTier)
        {
            if (armorTier > 0)
                foreach (var v in armorVariants)
                    if (v != null && v.prefab != null && v.tier == armorTier && (v.gender == 0 || v.gender == (female ? 2 : 1))) return v.prefab;
            GameObject g = female ? playerFemale : playerMale;
            if (g == null) g = female ? playerMale : playerFemale;
            return g;
        }

        /// <summary>Pilih model NPC yang cocok (gender/ras), deterministik berdasarkan seed.</summary>
        public GameObject PickNpc(int race, bool female, int seed)
        {
            var best = new List<GameObject>(); var okGender = new List<GameObject>(); var any = new List<GameObject>();
            foreach (var n in npcs)
            {
                if (n == null || n.prefab == null) continue;
                bool g = n.gender == 0 || n.gender == (female ? 2 : 1);
                bool r = n.race < 0 || n.race == race;
                if (g && r) best.Add(n.prefab);
                if (g) okGender.Add(n.prefab);
                any.Add(n.prefab);
            }
            var pool = best.Count > 0 ? best : (okGender.Count > 0 ? okGender : any);
            if (pool.Count == 0) return null;
            return pool[Mathf.Abs(seed) % pool.Count];
        }
    }
}
