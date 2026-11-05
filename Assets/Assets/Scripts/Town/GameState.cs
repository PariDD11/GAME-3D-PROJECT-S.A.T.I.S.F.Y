using System;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    [Serializable]
    public class QuestInfo
    {
        public string id, title, description;
        public int reward, exp;
        public bool completed;
        public string type, targetId;      // type: hunt / exterm / gather
        public int targetCount, faints;
    }

    /// <summary>
    /// Data pemain yang dipakai lintas scene: gold, survival, waktu, inventory, quest.
    /// Angka mengikuti GDD v2 (hari = 48 menit real, hunger habis 36 jam in-game, thirst 24 jam).
    /// </summary>
    public class GameState : MonoBehaviour
    {
        public static GameState I { get; private set; }

        public const float RealSecondsPerGameHour = 120f;   // 1 jam in-game = 2 menit real
        public const float HungerEmptyHours = 36f;
        public const float ThirstEmptyHours = 24f;
        public const float FatigueAfterHours = 48f;
        public const int CanteenMax = 5;
        public const int LevelCap = 50;
        public const int PouchCapacity = 24;   // GDD 10.1
        public static readonly string[] Ranks = { "G", "F", "E", "D", "C", "B", "A", "S", "SS" };

        [Header("Karakter")]
        public bool IsMage { get { return JobInfo.IsMage(job); } }
        public bool IsSpellblade { get { return IsMage && JobInfo.IsSpellblade(evo, evo2); } }
        public string evo = "";                // evolusi job (dipilih di Level 15): Knight, Pyromancer, ...
        public string evo2 = "";               // evolusi ke-2 (Level 30)
        public bool CanEvolve2 { get { return !string.IsNullOrEmpty(evo) && level >= JobInfo.Evo2Level && string.IsNullOrEmpty(evo2); } }
        public bool CanEvolve { get { return jobChosen && level >= JobInfo.EvoLevel && string.IsNullOrEmpty(evo); } }
        public bool Knows(string skillId) { return learnedSkills.Contains(skillId); }
        public string job = "Swordsman";   // Swordsman / Archer / FireMage / EarthMage / WaterMage / WindMage / Priest (dipilih permanen di level 5)
        public string rank = "G";
        public int level = 0;      // Hunter baru mulai dari Level 0 di Desa Pemula; job dipilih saat Level 5
        public int exp;
        public int skillPoints;
        public int intel, wis, statPoints;      // atribut sihir: INT (mana maks + damage), WIS (pulih mana + hemat mana)
        public int gold = 100;
        public bool jobChosen;
        /// <summary>Belum punya job: Level di bawah 5 dan belum memilih job (dipaksa, tidak bergantung Inspector).</summary>
        public bool NoJob { get { return !jobChosen && level < 5; } }
        public bool requireJobChoice = true;   // matikan di Inspector untuk testing tanpa pilih job

        [Header("Survival")]
        [Range(0, 100)] public float hunger = 100f;
        [Range(0, 100)] public float thirst = 100f;
        public int canteen = CanteenMax;
        public float hoursAwake;
        public string mealBuff = "";
        public int expBuffQuests;

        [Header("Waktu")]
        [Range(0, 24)] public float timeOfDay = 8f;
        public int day = 1;

        [Header("Progres")]
        public int questsDone;
        public int questProgress;
        // ---- Dungeon Eldrath (100 lantai, boss tiap 10 lantai)
        public int dungeonBest;            // lantai tertinggi yang pernah dicapai
        public int dungeonBossCleared;     // boss terakhir yang dikalahkan (0, 10, 20, ... 100)
        public int towerRuns;              // berapa kali masuk Menara
        public readonly Dictionary<string, int> stats = new Dictionary<string, int>();   // statistik untuk papan peringkat (kills_forest, boss_kills, ...)
        [NonSerialized] public QuestInfo activeQuest;     // NonSerialized: kalau tidak, Unity membuat objek kosong sehingga 'quest aktif' selalu ada
        public string spawnPoint = "Inn";

        public readonly Dictionary<string, int> inventory = new Dictionary<string, int>();
        public readonly Dictionary<string, string> equipment = new Dictionary<string, string>(); // slot -> itemId
        public readonly Dictionary<string, string> attachments = new Dictionary<string, string>(); // "slot#indeks" -> itemId attachment
        public static readonly int[] AttachUnlockLevel = { 1, 10, 20, 35 };
        public readonly Dictionary<string, float> durability = new Dictionary<string, float>();   // slot -> durability saat ini
        public readonly Dictionary<string, float> maxDurability = new Dictionary<string, float>(); // slot -> batas repair (craft 70, toko 100)
        public readonly HashSet<string> flags = new HashSet<string>();
        public readonly HashSet<string> learnedSkills = new HashSet<string>();
        public readonly string[] skillSlots = new string[7];   // aktif: Z X C [V = slot tengah / ultimate] R T Q
        public readonly string[] quickbar = new string[6];     // item: tombol 1-6
        public readonly Dictionary<string, float> storedDur = new Dictionary<string, float>();
        public readonly Dictionary<string, float> storedMax = new Dictionary<string, float>();

        public void AddStat(string key, int n = 1) { int v; stats.TryGetValue(key, out v); stats[key] = v + n; }
        public int Stat(string key) { int v; return stats.TryGetValue(key, out v) ? v : 0; }

        /// <summary>Kunci wilayah dari nama scene hunt: forest / desert / swamp / tower.</summary>
        public static string RegionKeyFor(string scene)
        {
            if (scene == "HuntArea_Desert") return "desert";
            if (scene == "HuntArea_Swamp") return "swamp";
            if (scene == "HuntArea_Volcano") return "volcano";
            if (scene == "HuntArea_Bone") return "bone";
            if (scene == "HuntArea_Demon") return "demon";
            if (scene == "HuntArea_Frontier") return "frontier";
            if (scene == "DungeonTower") return "tower";
            return "forest";
        }

        public bool IsFatigued { get { return hoursAwake >= FatigueAfterHours; } }

        public string Clock
        {
            get
            {
                int h = Mathf.FloorToInt(timeOfDay) % 24;
                int m = Mathf.FloorToInt((timeOfDay - Mathf.Floor(timeOfDay)) * 60f);
                return h.ToString("00") + ":" + m.ToString("00");
            }
        }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            if (activeQuest != null && string.IsNullOrEmpty(activeQuest.id)) activeQuest = null;   // objek kosong hasil serialisasi Unity
            DontDestroyOnLoad(gameObject);
            if (inventory.Count == 0) { AddItem("bread", 1); quickbar[0] = "bread"; }
        }

        void OnDestroy() { if (I == this) I = null; }

        void Update()
        {
            // Waktu berhenti saat menu terbuka agar belanja tidak menguras survival.
            if (TownUI.I != null && TownUI.I.PanelOpen) return;
            Advance(Time.deltaTime / RealSecondsPerGameHour, 1f, true);
        }

        public void Advance(float hours, float drainMultiplier, bool countAwake)
        {
            timeOfDay += hours;
            while (timeOfDay >= 24f) { timeOfDay -= 24f; day++; }
            if (countAwake) hoursAwake += hours;
            hunger = Mathf.Max(0f, hunger - 100f / HungerEmptyHours * hours * drainMultiplier * GameCalendar.HungerMul);
            thirst = Mathf.Max(0f, thirst - 100f / ThirstEmptyHours * hours * drainMultiplier * GameCalendar.ThirstMul);
        }

        public void SleepUntilMorning()
        {
            const float wake = 6f;
            float hours = timeOfDay < wake ? wake - timeOfDay : 24f - timeOfDay + wake;
            Advance(hours, 0.5f, false);
            timeOfDay = wake;
            hoursAwake = 0f;
        }

        // ---------- Perlengkapan jelajah (hilang tiap pulang ke kota) ----------
        public const string FlagKompas = "exp_kompas", FlagMinimap = "exp_minimap";
        public bool HasKompas { get { return true; } }          // kompas gratis untuk semua pemain
        public bool HasMinimap { get { return true; } }         // minimap/peta gratis di semua scene

        /// <summary>Dipanggil saat kembali ke kota: kompas & minimap jelajah hangus, harus beli lagi di Gerbang Luar.</summary>
        public void EndExpedition()
        {
            bool had = HasKompas;
            flags.Remove(FlagKompas);
            flags.Remove(FlagMinimap);
            flags.Remove("map_G_basic"); flags.Remove("map_G_standard"); flags.Remove("map_G_premium");   // peta permanen versi lama
            if (had) Say("Kompas/Peta jelajahmu sudah tidak berlaku. Beli lagi di Gerbang Luar sebelum berangkat.");
        }

        // ---------- Inventory & gold ----------
        public int Count(string id) { int n; return inventory.TryGetValue(id, out n) ? n : 0; }

        public void AddItem(string id, int n)
        {
            inventory[id] = Count(id) + n;
        }

        public bool RemoveItem(string id, int n)
        {
            if (Count(id) < n) return false;
            inventory[id] = Count(id) - n;
            if (inventory[id] <= 0) inventory.Remove(id);
            return true;
        }

        public bool TrySpend(int amount)
        {
            if (amount > 0 && (SeasonEvents.BuyMul != 1f || GovernorChain.BuyMul != 1f)) amount = Mathf.Max(1, Mathf.RoundToInt(amount * SeasonEvents.BuyMul * GovernorChain.BuyMul));   // diskon event musim
            if (gold < amount) { Say("Gold tidak cukup (butuh " + amount + ")."); return false; }
            gold -= amount;
            if (amount > 0) Audio.Sfx("coin");
            return true;
        }

        // ---------- Survival ----------
        public void Consume(string id)
        {
            var def = ItemDB.Get(id);
            if (def == null || !RemoveItem(id, 1)) return;
            ApplyFood(def);
        }

        // Dipakai kedai (makan langsung tanpa lewat inventory).
        public void ApplyFood(ItemDef def)
        {
            hunger = Mathf.Min(100f, hunger + def.hunger);
            thirst = Mathf.Min(100f, thirst + def.thirst);
            if (!string.IsNullOrEmpty(def.buff)) { mealBuff = def.buff; MealBuff.Begin(def.buff); }
            FishCook.OnEat(def.id);
            Say(def.name + ": " + def.Describe());
        }

        public void DrinkCanteen()
        {
            if (canteen <= 0) { Say("Botol Minum kosong. Isi gratis di kedai atau sumber air."); return; }
            if (thirst >= 98f) { Say("Kamu belum haus."); return; }
            canteen--;
            thirst = Mathf.Min(100f, thirst + 20f);
            Say("Minum dari botol minum (+20 Thirst).");
        }

        public void RefillCanteen() { canteen = CanteenMax; Say("Botol Minum terisi penuh."); }

        // ---------- Level & rank ----------
        public static int ExpToNext(int lvl) { return Mathf.Max(1, Mathf.RoundToInt(3f * Mathf.Pow(Mathf.Max(1, lvl), 1.5f) * Balance.ExpCurve)); } // GDD 4.3

        public void AddExp(int amount)
        {
            if (amount > 0 && Difficulty.ExpMul != 1f) amount = Mathf.Max(1, Mathf.RoundToInt(amount * Difficulty.ExpMul));
            if (amount > 0 && SeasonEvents.ExpMul != 1f) amount = Mathf.Max(1, Mathf.RoundToInt(amount * SeasonEvents.ExpMul));
            exp += amount;
            while (level < LevelCap && exp >= ExpToNext(level))
            {
                exp -= ExpToNext(level);
                level++;
                Audio.Sfx("levelup");
                skillPoints++;
                statPoints += 2;
                Say("Level naik ke " + level + "! (+1 SP, +2 poin atribut)");
                if (level == JobInfo.Evo2Level && CanEvolve2) Say("Evolusi Lv.30 tersedia! Buka Skill Tree (K) tab KELAS LV.30.");
                if (level == JobInfo.EvoLevel && CanEvolve) Say("Evolusi Job tersedia! Buka Skill Tree (K) lalu pilih Evolusi.");
            }
        }

        public static int RankIndex(string r) { return Array.IndexOf(Ranks, r); }

        public void CheckRankUp()
        {
            int[] need = { 3, 11, 23 }; // poin quest: Kumpulkan/Basmi 1, Buru 3. G->F: 3 poin (3 herbal / 1 buru), F->E: +8, E->D: +12
            for (int i = 0; i < need.Length; i++)
            {
                if (rank == Ranks[i] && questsDone >= need[i])
                {
                    rank = Ranks[i + 1];
                    Say("Rank naik! Sekarang kamu Hunter Rank " + rank + ".");
                }
            }
        }

        // ---------- Pouch ----------
        public int SlotsUsed()
        {
            int used = 0;
            foreach (var kv in inventory)
            {
                var def = ItemDB.Get(kv.Key);
                if (def != null && def.type == ItemType.QuestItem) continue;
                int stack = def != null ? def.stack : 10;
                used += (kv.Value + stack - 1) / stack;
            }
            return used;
        }

        public bool HasSpace(string id, int n)
        {
            var def = ItemDB.Get(id);
            if (def != null && def.type == ItemType.QuestItem) return true;
            int stack = def != null ? def.stack : 10;
            int cur = Count(id);
            int extra = (cur + n + stack - 1) / stack - (cur + stack - 1) / stack;
            return SlotsUsed() + extra <= PouchCapacity;
        }

        // ---------- Perlengkapan ----------
        public int TotalAtk
        {
            get { int t = 0; foreach (var kv in equipment) { var d = ItemDB.Get(kv.Value); if (d != null) t += d.atk; } return t + GearSets.Atk(this) + StatUpgrade.AtkBonus(this) + WeaponLevel.AtkBonus(this) + AttachSum(true) + GearTiers.EnchantSum(this, true) + Sockets.Atk(this); }
        }

        public int TotalDef
        {
            get { int t = 0; foreach (var kv in equipment) { var d = ItemDB.Get(kv.Value); if (d != null) t += d.def; } return t + GearSets.Def(this) + StatUpgrade.DefBonus(this) + AttachSum(false) + GearTiers.EnchantSum(this, false) + Sockets.Def(this) + (Knows("fm_wrd_1") ? 5 : 0) + (Knows("em_bed_1") ? 6 : 0) + Mathf.RoundToInt(Pas("def")); }
        }

        // ---------- Bonus pasif skill & buff sementara ----------
        // key: dmg (pecahan), def (flat), hp (pecahan), mana (flat), manaregen, manacost, crit (persen), lifesteal (pecahan), spd, dmgred, cdr, regen (pecahan HP/dtk)
        readonly Dictionary<string, KeyValuePair<float, float>> tempBuffs = new Dictionary<string, KeyValuePair<float, float>>();

        public void AddTempBuff(string key, float val, float dur) { tempBuffs[key] = new KeyValuePair<float, float>(val, Time.time + dur); }

        public float Pas(string key)
        {
            float t = 0f;
            foreach (var id in learnedSkills)
            {
                var s = SkillDB.Get(id);
                float v;
                if (s != null && s.pas != null && s.pas.TryGetValue(key, out v) && (s.evo == "" || s.evo == evo || s.evo == evo2)) t += v;
            }
            if (key == "crit") t += StatUpgrade.CritBonus(this);
            KeyValuePair<float, float> b;
            if (tempBuffs.TryGetValue(key, out b) && Time.time < b.Value) t += b.Key;
            return t;
        }

        public float DmgMult { get { return (1f + Pas("dmg")) * Balance.PlayerDmg; } }

        // ---------- Mana ----------
        public int EquipMana { get { int t = 0; foreach (var kv in equipment) { var d = ItemDB.Get(kv.Value); if (d != null) t += d.mana; } return t; } }
        public float MaxMana { get { return 60f + 4f * level + 6f * intel + EquipMana + AttachEffect("maxmana") + ((Knows("fm_wrd_1") || Knows("em_bed_1")) ? 20f : 0f) + Pas("mana"); } }
        public float ManaRegenMult { get { return 1f + 0.04f * wis + AttachEffect("manaregen") / 100f + ((Knows("fm_inf_3") || Knows("em_stn_3")) ? 0.25f : 0f) + Pas("manaregen"); } }
        public float ManaCostMult { get { return Mathf.Max(0.55f, 1f - 0.01f * wis - (job == "FireMage" && Knows("fm_stp_2") ? 0.1f : 0f) - Pas("manacost") - AttachEffect("manacost") / 100f); } }
        public float SpellPower { get { return 1f + 0.01f * intel + AttachEffect("spellpower") / 100f; } }
        public bool SpendStatPoint(string which)
        {
            if (statPoints <= 0) return false;
            if (which == "int") { intel++; AddStat("pt_int"); } else if (which == "wis") { wis++; AddStat("pt_wis"); } else return false;
            statPoints--;
            return true;
        }

        // ---------- Attachment ----------
        public static string AttKey(string slot, int idx) { return slot + "#" + idx; }

        int AttachSum(bool atk)
        {
            int t = 0;
            foreach (var kv in attachments)
            {
                int h = kv.Key.IndexOf('#');
                if (h < 0 || !equipment.ContainsKey(kv.Key.Substring(0, h))) continue;   // gear tidak terpasang: bonus tidak berlaku
                var d = ItemDB.Get(kv.Value);
                if (d != null && d.job == "Mage" && !IsMage) continue;
                if (d != null) t += atk ? d.atk : d.def;
            }
            return t;
        }

        /// <summary>Jumlah efek khusus (persen) dari semua attachment aktif (gear-nya terpasang).</summary>
        public float AttachEffect(string eff)
        {
            float t = GearTiers.GearEffect(this, eff) + Sockets.Effect(this, eff) + GearSets.Effect(this, eff);
            foreach (var kv in attachments)
            {
                int h = kv.Key.IndexOf('#');
                if (h < 0 || !equipment.ContainsKey(kv.Key.Substring(0, h))) continue;
                var d = ItemDB.Get(kv.Value);
                if (d != null && d.job == "Mage" && !IsMage) continue;       // attachment khusus penyihir
                if (d != null && d.effect == eff) t += d.effVal;
            }
            return t;
        }

        static readonly string[][] AttDropPool =
        {
            new[] { "att_whet", "att_plate", "att_charm" },
            new[] { "att_fang", "att_guard", "att_keen", "att_swift", "att_coin", "att_mana", "att_focus" },
            new[] { "att_rune_atk", "att_rune_def", "att_charm2", "att_vamp", "att_scroll", "att_mana", "att_focus" },
        };

        /// <summary>Undi attachment. tier 0 = umum, 1 = menengah, 2 = langka. Mengembalikan id atau null bila tas penuh.</summary>
        public string GiveRandomAttachment(int tier)
        {
            var pool = AttDropPool[Mathf.Clamp(tier, 0, 2)];
            string id = pool[UnityEngine.Random.Range(0, pool.Length)];
            if (IsMage && UnityEngine.Random.value < 0.4f)
            {
                // penyihir: 40% hadiah berupa attachment Sihir; tier drop 0 -> T1, 1 -> T1/T2, 2 -> T2/T3
                int mt = Mathf.Clamp(tier + 1 - (UnityEngine.Random.value < 0.5f ? 1 : 0), 1, 3);
                string[] fx = { "att_m_power", "att_m_core", "att_m_flow", "att_m_thrift", "att_m_hourglass", "att_m_siphon" };
                id = fx[UnityEngine.Random.Range(0, fx.Length)] + "_" + mt;
            }
            if (!HasSpace(id, 1)) { Say("Tas penuh: " + ItemDB.Get(id).name + " tidak terbawa."); return null; }
            AddItem(id, 1);
            Say("Dapat attachment: " + ItemDB.Get(id).name + "!");
            return id;
        }

        /// <summary>Sinkronkan item quest dengan keadaan quest: kartu guild selalu ada, surat saat quest aktif, bukti saat quest selesai.</summary>
        public void SyncQuestItems()
        {
            inventory["guild_card"] = 1;
            Set1("quest_letter", activeQuest != null && !activeQuest.completed);
            Set1("quest_proof", activeQuest != null && activeQuest.completed);
        }

        void Set1(string id, bool on) { if (on) inventory[id] = 1; else inventory.Remove(id); }

        public string AttachmentAt(string slot, int idx) { string id; return attachments.TryGetValue(AttKey(slot, idx), out id) ? id : null; }
        public bool AttachUnlocked(int idx) { return idx >= 0 && idx < AttachUnlockLevel.Length && level >= AttachUnlockLevel[idx]; }

        public bool TryAttach(string slot, int idx, string id)
        {
            var d = ItemDB.Get(id);
            if (d == null || d.type != ItemType.Attachment || Count(id) <= 0) return false;
            if (!equipment.ContainsKey(slot)) { Say("Pasang gear dulu."); return false; }
            if (!AttachUnlocked(idx)) { Say("Slot terbuka pada Lv " + AttachUnlockLevel[idx] + "."); return false; }
            if (AttachmentAt(slot, idx) != null) { Say("Slot sudah terisi."); return false; }
            RemoveItem(id, 1);
            attachments[AttKey(slot, idx)] = id;
            Say(d.name + " dipasang.");
            return true;
        }

        public bool Detach(string slot, int idx)
        {
            string id = AttachmentAt(slot, idx);
            if (id == null) return false;
            if (!HasSpace(id, 1)) { Say("Tas penuh."); return false; }
            attachments.Remove(AttKey(slot, idx));
            AddItem(id, 1);
            Say(ItemDB.Get(id).name + " dilepas.");
            return true;
        }

        /// <summary>Pasang gear ke slot. Gear lama (kalau ada) kembali ke tas dengan durability-nya tersimpan.</summary>
        public void SetEquipment(string slot, string id, float dur, float max)
        {
            string old;
            if (equipment.TryGetValue(slot, out old) && !string.IsNullOrEmpty(old))
            {
                float od, om;
                durability.TryGetValue(slot, out od);
                if (!maxDurability.TryGetValue(slot, out om)) om = 100f;
                storedDur[old] = od;
                storedMax[old] = om;
                AddItem(old, 1);
            }
            equipment[slot] = id;
            durability[slot] = dur;
            maxDurability[slot] = max;
        }

        public bool EquipFromInventory(string id)
        {
            var def = ItemDB.Get(id);
            if (def == null || def.type != ItemType.Gear || Count(id) <= 0) return false;
            if (def.job != "Any" && def.job != job) { Say("Gear ini khusus " + JobInfo.Name(def.job) + "."); return false; }
            if (!GearTiers.EvoOk(this, def)) { Say("Senjata ini khusus kelas " + JobInfo.EvoName(def.evoReq) + "."); return false; }
            float d = 100f, m = 100f;
            if (storedDur.ContainsKey(id)) { d = storedDur[id]; m = storedMax[id]; }
            RemoveItem(id, 1);
            SetEquipment(def.slot, id, d, m);
            Say(def.name + " dipasang.");
            return true;
        }

        public bool Unequip(string slot)
        {
            string id;
            if (!equipment.TryGetValue(slot, out id)) return false;
            if (!HasSpace(id, 1)) { Say("Tas penuh."); return false; }
            float d, m;
            durability.TryGetValue(slot, out d);
            if (!maxDurability.TryGetValue(slot, out m)) m = 100f;
            storedDur[id] = d;
            storedMax[id] = m;
            equipment.Remove(slot);
            durability.Remove(slot);
            maxDurability.Remove(slot);
            AddItem(id, 1);
            Say(ItemDB.Get(id).name + " dilepas.");
            return true;
        }

        // ---------- Quickbar item ----------
        public void UseQuickbar(int i)
        {
            string id = quickbar[i];
            if (string.IsNullOrEmpty(id)) return;
            var def = ItemDB.Get(id);
            if (def == null) return;
            if (Count(id) <= 0) { Say(def.name + " habis."); return; }
            if (def.type == ItemType.Food || def.type == ItemType.Drink) Consume(id);
            else if (id == "potion" && PlayerCombat.I != null) PlayerCombat.I.UsePotion();
            else if (id == "mana_potion" && PlayerCombat.I != null) PlayerCombat.I.UseManaPotion();
            else Say(def.name + " dipakai saat hunt.");
        }

        // ---------- Skill ----------
        int CountLearned(string sJob, string branch, int tier)
        {
            int n = 0;
            foreach (var s in SkillDB.All)
                if (s.job == sJob && s.branch == branch && s.tier == tier && learnedSkills.Contains(s.id)) n++;
            return n;
        }

        public bool CanLearn(SkillDef s, out string reason)
        {
            reason = "";
            if (s.job != job) { reason = "Bukan skill untuk job " + job + "."; return false; }
            if (learnedSkills.Contains(s.id)) { reason = "Sudah dipelajari."; return false; }
            if (!string.IsNullOrEmpty(s.evo) && s.evo != evo && s.evo != evo2) { var ed = JobInfo.Evo(s.evo); int need = ed != null ? ed.level : JobInfo.EvoLevel; reason = (string.IsNullOrEmpty(evo) || (need == JobInfo.Evo2Level && string.IsNullOrEmpty(evo2))) ? "Butuh evolusi " + JobInfo.EvoName(s.evo) + " (Level " + need + ")." : "Khusus evolusi " + JobInfo.EvoName(s.evo) + "."; return false; }
            if (s.tier == 2 && level < 10) { reason = "Butuh Level 10."; return false; }
            if (s.tier == 3 && level < 20) { reason = "Butuh Level 20."; return false; }
            if (s.tier == 2 && CountLearned(s.job, s.branch, 1) < 2) { reason = "Butuh minimal 2 skill Tier 1 di cabang ini."; return false; }
            if (s.tier == 3 && CountLearned(s.job, s.branch, 2) < 2) { reason = "Butuh kedua skill Tier 2 di cabang ini."; return false; }
            if (skillPoints < s.cost) { reason = "SP kurang (butuh " + s.cost + ", punya " + skillPoints + ")."; return false; }
            return true;
        }

        public bool LearnSkill(string id)
        {
            var s = SkillDB.Get(id);
            string reason;
            if (s == null) return false;
            if (!CanLearn(s, out reason)) { Say(reason); return false; }
            skillPoints -= s.cost;
            learnedSkills.Add(id);
            Say("Skill dipelajari: " + s.name);
            return true;
        }

        public bool Evolve(string id)
        {
            var e = JobInfo.Evo(id);
            if (e == null || e.job != job || !CanEvolve) return false;
            evo = id;
            skillPoints += 3;
            Audio.Sfx("levelup");
            Say("EVOLUSI! Kamu kini seorang " + e.name + " (+3 SP).");
            return true;
        }

        public bool Evolve2(string id)
        {
            var e = JobInfo.Evo(id);
            if (e == null || e.job != job || e.level != JobInfo.Evo2Level || e.parent != evo || !CanEvolve2) return false;
            evo2 = id;
            skillPoints += 5;
            Audio.Sfx("levelup");
            Say("EVOLUSI LV.30! Kamu kini seorang " + e.name + " (+5 SP).");
            return true;
        }

        public void AssignSkill(int slot, string id)
        {
            var s = SkillDB.Get(id);
            if (s == null || !s.active || !learnedSkills.Contains(id)) return;
            for (int i = 0; i < skillSlots.Length; i++) if (skillSlots[i] == id) skillSlots[i] = null;
            skillSlots[slot] = id;
        }

        public void TryUseSkill(int slot)
        {
            SkillExec.Use(slot);   // damage + efek visual per skill (SkillExec.cs)
        }

        public int RespecCost { get { return 20 * level; } }   // GDD 4.4

        public bool RespecSkills()
        {
            if (learnedSkills.Count == 0) { Say("Belum ada skill yang dipelajari."); return false; }
            if (!TrySpend(RespecCost)) return false;
            int refund = 0;
            foreach (var id in learnedSkills) { var s = SkillDB.Get(id); if (s != null) refund += s.cost; }
            skillPoints += refund;
            learnedSkills.Clear();
            for (int i = 0; i < skillSlots.Length; i++) skillSlots[i] = null;
            Say("Skill di-reset. +" + refund + " SP dikembalikan.");
            return true;
        }

        // ---------- Debug ----------
        public void GiveTestMaterials()
        {
            AddItem("wolf_pelt", 3); AddItem("boar_tusk", 2); AddItem("monster_bone", 5);
            AddItem("orc_tusk", 1); AddItem("herb", 5); gold += 200;
            foreach (var g in new[] { "hunter_blade", "iron_sword", "knight_sword", "bone_bow", "hunters_bow", "long_bow", "leather_cap", "iron_cap", "knight_helm", "leather_armor", "wolfhide_armor", "iron_armor", "knight_armor", "leather_boots", "iron_boots" })
                if (Count(g) <= 0) AddItem(g, 1);
            Say("[DEBUG] +bahan monster, 200 gold & semua gear (Tab untuk memasang)");
        }

        public static void Say(string msg) { if (TownUI.I != null) TownUI.I.Toast(msg); }
    }
}
