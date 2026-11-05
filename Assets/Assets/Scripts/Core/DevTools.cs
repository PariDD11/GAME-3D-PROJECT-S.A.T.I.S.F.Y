using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// "Karakter Developer": panel cheat untuk testing. Tekan F1 saat bermain.
    /// Hanya aktif di Editor / Development Build (atau jika PlayerPrefs HG_Dev = 1). Build rilis tidak punya cheat.
    /// Fitur: God mode, 1-hit kill, stamina tak habis, tanpa lapar/haus, terbang + tembus dinding (Space naik, Ctrl turun),
    /// kecepatan x1/x2/x4, bekukan waktu, paket Developer (Lv 50, rank SS, gold, semua skill, item), level/rank/gold/SP,
    /// teleport antar scene atau ke objek terdekat, spawn/bunuh monster, beri item, atur jam.
    /// </summary>
    public class DevTools : MonoBehaviour
    {
        static DevTools inst;
        public static bool Enabled { get { return true; } }   // cheat selalu aktif (proyek solo); kembalikan ke Editor/Debug saja sebelum rilis publik

        public static bool God, OneHit, InfStamina, NoNeeds, FreezeTime, NoCooldown;
        public static bool Fly { get; private set; }
        public static float Speed = 1f;

        string page = "main";
        int itemPage;
        float frozenTime; int frozenDay;

        static readonly string[] Scenes = { "Town", "GuildInterior", "DungeonCity", "DungeonTower", "HuntArea_G", "HuntArea_Desert", "HuntArea_Swamp", "HuntArea_Frontier", "HuntArea_Volcano", "HuntArea_Bone", "HuntArea_Demon",
            "Interior_Inn", "Interior_Smithy", "Interior_Shop", "Interior_Academy", "Interior_Tavern", "Interior_Cathedral" };
        static readonly string[] MonsterIds = { "slime", "goblin", "alpha_wolf", "giant_boar", "dire_wolf", "sand_lizard", "mummy", "giant_scorpion", "sand_golem",
            "bandit", "bandit_archer", "bandit_leader", "drowned", "skeleton", "wraith", "witch", "swamp_troll", "necromancer" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (inst != null || !Enabled) return;
            var go = new GameObject("DevTools");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<DevTools>();
            SceneManager.sceneLoaded += (s, m) => { if (Fly) SetFly(false); };
        }

        void Update()
        {
            var gs = GameState.I;
            if (gs == null) return;
            var pc = PlayerCombat.I;
            if (pc != null && InfStamina) { pc.stamina = pc.MaxStamina; pc.mana = pc.MaxMana; }
            if (NoNeeds) { gs.hunger = 100f; gs.thirst = 100f; gs.hoursAwake = 0f; }
            if (FreezeTime) { gs.timeOfDay = frozenTime; gs.day = frozenDay; }

            CheatKeys(gs);
            if (TownInput.DevMenuDown())
            {
                var ui = TownUI.I;
                if (ui == null) return;
                if (ui.PanelOpen) { if (OwnPanel) ui.Close(); }
                else OpenMenu("main");
            }
        }

        bool ownPanel;
        bool OwnPanel { get { return ownPanel && TownUI.I != null && TownUI.I.PanelOpen; } }

        // ------------------------------------------------------------------ menu
        void OpenMenu(string p)
        {
            page = p; itemPage = 0; ownPanel = true;
            TownUI.I.Open("DEVELOPER", Header, Entries);
        }

        string Header()
        {
            var gs = GameState.I;
            string s = "Lv " + gs.level + "  Rank " + gs.rank + "  Gold " + gs.gold + "  SP " + gs.skillPoints + "  Jam " + gs.Clock;
            if (page == "main") s += "\nGod " + On(God) + "  1-Hit " + On(OneHit) + "  Stamina~ " + On(InfStamina) + "  NoLapar " + On(NoNeeds) + "  Terbang " + On(Fly) + "  Waktu beku " + On(FreezeTime) + "  Speed x" + Speed;
            return s;
        }

        static string On(bool b) { return b ? "ON" : "off"; }
        static string T(string name, bool b) { return name + ": " + On(b); }

        List<UIEntry> Entries()
        {
            var l = new List<UIEntry>();
            var gs = GameState.I;
            var pc = PlayerCombat.I;
            switch (page)
            {
                case "main":
                    l.Add(new UIEntry("PAKET DEVELOPER (Lv50, Rank SS, gold, semua skill, item)", () => GiveDevKit()));
                    l.Add(new UIEntry(T("God mode", God), () => God = !God));
                    l.Add(new UIEntry(T("Sekali pukul mati", OneHit), () => OneHit = !OneHit));
                    l.Add(new UIEntry(T("Stamina & mana tak habis (F7)", InfStamina), () => InfStamina = !InfStamina));
                    l.Add(new UIEntry(T("Tanpa cooldown skill (F8)", NoCooldown), () => { NoCooldown = !NoCooldown; SkillExec.ResetCooldowns(); }));
                    l.Add(new UIEntry("MODE DEWA: god + 1-hit + stamina/mana + tanpa CD (F2)", () => ToggleGodMode()));
                    l.Add(new UIEntry("Ganti JOB / evolusi  >", () => page = "job"));
                    l.Add(new UIEntry(T("Tanpa lapar/haus", NoNeeds), () => NoNeeds = !NoNeeds));
                    l.Add(new UIEntry(T("Terbang + tembus dinding (Space naik, Ctrl turun)", Fly), () => SetFly(!Fly)));
                    l.Add(new UIEntry(T("Bekukan waktu", FreezeTime), () => { FreezeTime = !FreezeTime; frozenTime = gs.timeOfDay; frozenDay = gs.day; }));
                    l.Add(new UIEntry("Kecepatan jalan: x" + Speed + "  (klik untuk ganti)", () => Speed = Speed < 1.5f ? 2f : Speed < 3f ? 4f : 1f));
                    l.Add(new UIEntry("Karakter (level, rank, gold, HP)  >", () => page = "char"));
                    l.Add(new UIEntry("Teleport  >", () => page = "tp"));
                    l.Add(new UIEntry("Monster  >", () => page = "mob"));
                    l.Add(new UIEntry("Item  >", () => { page = "items"; itemPage = 0; }));
                    l.Add(new UIEntry("Waktu & Quest  >", () => page = "time"));
                    l.Add(new UIEntry("Tutup (F1)", () => TownUI.I.Close()));
                    break;

                case "job":
                    l.Add(new UIEntry("Sekarang: " + gs.job + (string.IsNullOrEmpty(gs.evo) ? "" : " > " + gs.evo) + (string.IsNullOrEmpty(gs.evo2) ? "" : " > " + gs.evo2) + "   (F4 = job berikut, F6 = evolusi berikut)", null, false));
                    foreach (var jb in JobInfo.All)
                    {
                        string j = jb;
                        l.Add(new UIEntry("Jadi " + JobInfo.Name(j) + " (Lv50, semua skill, 4 skill terkuat dipasang)", () => SwitchJob(j, "", "")));
                        foreach (var e1 in JobInfo.EvosOf(j))
                            foreach (var e2 in JobInfo.Evos2Of(e1.id))
                            {
                                string a = e1.id, b = e2.id;
                                l.Add(new UIEntry("      " + JobInfo.Name(j) + " > " + e1.name + " > " + e2.name, () => SwitchJob(j, a, b)));
                            }
                    }
                    l.Add(new UIEntry("<  Kembali", () => page = "main"));
                    break;

                case "char":
                    l.Add(new UIEntry("Isi penuh HP + stamina + hidupkan lagi", () => { if (pc != null) { pc.Revive(); pc.FullRestore(); } gs.hunger = 100; gs.thirst = 100; gs.hoursAwake = 0; }));
                    l.Add(new UIEntry("Level +1", () => SetLevel(gs.level + 1)));
                    l.Add(new UIEntry("Level +5", () => SetLevel(gs.level + 5)));
                    l.Add(new UIEntry("Level -1", () => SetLevel(gs.level - 1)));
                    l.Add(new UIEntry("Level langsung 50", () => SetLevel(GameState.LevelCap)));
                    l.Add(new UIEntry("Level langsung 1", () => SetLevel(1)));
                    l.Add(new UIEntry("Rank naik satu", () => SetRank(GameState.RankIndex(gs.rank) + 1)));
                    l.Add(new UIEntry("Rank turun satu", () => SetRank(GameState.RankIndex(gs.rank) - 1)));
                    l.Add(new UIEntry("Gold +1.000", () => gs.gold += 1000));
                    l.Add(new UIEntry("Gold +100.000", () => gs.gold += 100000));
                    l.Add(new UIEntry("Gold jadi 0", () => gs.gold = 0));
                    l.Add(new UIEntry("Skill point +10", () => gs.skillPoints += 10));
                    l.Add(new UIEntry("Pelajari semua skill job ini", () => LearnAll()));
                    l.Add(new UIEntry("Reputasi guild naik satu tier", () => RepUp()));
                    l.Add(new UIEntry("<  Kembali", () => page = "main"));
                    break;

                case "tp":
                    foreach (var sc in Scenes)
                    {
                        string s = sc;
                        bool ok = Application.CanStreamedLevelBeLoaded(s);
                        l.Add(new UIEntry("Scene: " + s + (ok ? "" : "  (belum di Build Settings)"), () => LoadScene(s), ok));
                    }
                    l.Add(new UIEntry("Ke objek interaksi terdekat berikutnya  >", () => page = "tpnear"));
                    l.Add(new UIEntry("Ke monster terdekat", () => TeleportToMonster()));
                    l.Add(new UIEntry("<  Kembali", () => page = "main"));
                    break;

                case "tpnear":
                    var near = NearestInteractables(12);
                    foreach (var it in near)
                    {
                        var target = it;
                        l.Add(new UIEntry(target.prompt + "   (" + Mathf.RoundToInt(Vector3.Distance(target.transform.position, PlayerPos())) + " m)", () => TeleportNear(target.transform)));
                    }
                    if (near.Count == 0) l.Add(new UIEntry("(tidak ada objek interaksi di scene ini)", null, false));
                    l.Add(new UIEntry("<  Kembali", () => page = "tp"));
                    break;

                case "mob":
                    foreach (var id in MonsterIds)
                    {
                        string m = id;
                        var d = MonsterDB.Get(m);
                        if (d == null) continue;
                        l.Add(new UIEntry("Spawn " + d.name + " (" + m + ")", () => SpawnMonster(m, false)));
                    }
                    l.Add(new UIEntry("Spawn Alpha Wolf sebagai BOSS (HP x3)", () => SpawnMonster("alpha_wolf", true)));
                    l.Add(new UIEntry("BUNUH SEMUA monster di scene", () => KillAll()));
                    l.Add(new UIEntry("<  Kembali", () => page = "main"));
                    break;

                case "items":
                    var all = new List<ItemDef>();
                    foreach (ItemType t in System.Enum.GetValues(typeof(ItemType))) all.AddRange(ItemDB.OfType(t));
                    const int per = 10;
                    int pages = Mathf.Max(1, (all.Count + per - 1) / per);
                    itemPage = Mathf.Clamp(itemPage, 0, pages - 1);
                    for (int i = itemPage * per; i < Mathf.Min(all.Count, (itemPage + 1) * per); i++)
                    {
                        var d = all[i];
                        int n = d.type == ItemType.Gear ? 1 : 10;
                        l.Add(new UIEntry("+" + n + "  " + d.name + "  [" + d.type + "]", () => { gs.AddItem(d.id, n); TownUI.I.Toast("+" + n + " " + d.name); }));
                    }
                    l.Add(new UIEntry("Halaman " + (itemPage + 1) + "/" + pages + "  >  berikutnya", () => itemPage = (itemPage + 1) % pages));
                    l.Add(new UIEntry("Beri 1 set semua bahan crafting (x20)", () => gs.GiveTestMaterials()));
                    l.Add(new UIEntry("<  Kembali", () => page = "main"));
                    break;

                case "time":
                    l.Add(new UIEntry("Jam 06:00 (pagi)", () => SetHour(6f)));
                    l.Add(new UIEntry("Jam 12:00 (siang)", () => SetHour(12f)));
                    l.Add(new UIEntry("Jam 18:00 (sore)", () => SetHour(18f)));
                    l.Add(new UIEntry("Jam 22:00 (malam)", () => SetHour(22f)));
                    l.Add(new UIEntry("Hari +1", () => { gs.day++; frozenDay = gs.day; }));
                    l.Add(new UIEntry("Selesaikan quest aktif (lapor ke guild)", () => { if (gs.activeQuest != null) { gs.activeQuest.completed = true; gs.questProgress = gs.activeQuest.targetCount; TownUI.I.Toast("Quest ditandai selesai."); } else TownUI.I.Toast("Tidak ada quest aktif."); }));
                    l.Add(new UIEntry("Progres quest +1", () => { if (gs.activeQuest != null) gs.questProgress = Mathf.Min(gs.activeQuest.targetCount, gs.questProgress + 1); }));
                    l.Add(new UIEntry("Quest selesai (counter) +5", () => gs.questsDone += 5));
                    l.Add(new UIEntry("Isi botol minum air", () => gs.RefillCanteen()));
                    l.Add(new UIEntry("<  Kembali", () => page = "main"));
                    break;
            }
            return l;
        }

        // ------------------------------------------------------------------ aksi
        static Vector3 PlayerPos()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            return p != null ? p.transform.position : Vector3.zero;
        }

        public static void SetFly(bool on)
        {
            Fly = on;
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return;
            var cc = p.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = !on;
        }

        void GiveDevKit()
        {
            var gs = GameState.I;
            gs.job = string.IsNullOrEmpty(gs.job) ? "Swordsman" : gs.job;
            gs.jobChosen = true;
            SetLevel(GameState.LevelCap);
            SetRank(GameState.Ranks.Length - 1);
            gs.gold = Mathf.Max(gs.gold, 99999);
            gs.skillPoints = Mathf.Max(gs.skillPoints, 60);
            LearnAll();
            gs.canteen = GameState.CanteenMax;
            foreach (var id in new[] { "bread", "grilled_meat", "water", "berry_juice" }) gs.AddItem(id, 10);
            gs.GiveTestMaterials();
            God = true; InfStamina = true; NoNeeds = true;
            TownUI.I.Toast("Paket Developer aktif: Lv50, Rank SS, god mode.");
        }

        static void SetLevel(int lv)
        {
            var gs = GameState.I;
            int old = gs.level;
            gs.level = Mathf.Clamp(lv, 1, GameState.LevelCap);
            gs.exp = 0;
            if (gs.level > old) gs.skillPoints += gs.level - old;
            if (PlayerCombat.I != null) PlayerCombat.I.FullRestore();
        }

        static void SetRank(int idx)
        {
            var gs = GameState.I;
            idx = Mathf.Clamp(idx, 0, GameState.Ranks.Length - 1);
            gs.rank = GameState.Ranks[idx];
            gs.level = Mathf.Max(gs.level, GuildConfig.ExamMinLevel[idx]);
            gs.questsDone = Mathf.Max(gs.questsDone, GuildConfig.ExamMinQuests[idx]);
            if (GuildData.I != null) GuildData.I.postersAt = -999f;
        }

        // ------------------------------------------------------------------ hotkey & ganti job
        static void ToggleGodMode()
        {
            bool on = !(God && InfStamina && NoCooldown && OneHit);
            God = InfStamina = NoCooldown = OneHit = on;
            SkillExec.ResetCooldowns();
            if (TownUI.I != null) TownUI.I.Toast("MODE DEWA " + (on ? "ON (god, 1-hit, stamina/mana tak habis, tanpa CD)" : "off"));
        }

        void CheatKeys(GameState gs)
        {
            if (TownInput.CheatDown(0)) ToggleGodMode();
            if (TownInput.CheatDown(1)) NextJob(gs);
            if (TownInput.CheatDown(2)) NextEvo(gs);
            if (TownInput.CheatDown(3)) { InfStamina = !InfStamina; Say("Stamina & mana tak habis: " + On(InfStamina)); }
            if (TownInput.CheatDown(4)) { NoCooldown = !NoCooldown; SkillExec.ResetCooldowns(); Say("Tanpa cooldown: " + On(NoCooldown)); }
            if (TownInput.CheatDown(5)) { var pc = PlayerCombat.I; if (pc != null) { pc.Revive(); pc.FullRestore(); } SkillExec.ResetCooldowns(); Say("HP/stamina/mana penuh, cooldown di-reset."); }
        }

        static void Say(string m) { if (TownUI.I != null) TownUI.I.Toast(m); }

        static void NextJob(GameState gs)
        {
            int i = System.Array.IndexOf(JobInfo.All, gs.job);
            SwitchJob(JobInfo.All[(i + 1) % JobInfo.All.Length], "", "");
        }

        /// <summary>Siklus jalur evolusi akhir job aktif: dasar -> jalur A -> B -> C -> dasar.</summary>
        static void NextEvo(GameState gs)
        {
            var paths = new List<string[]>();
            paths.Add(new[] { "", "" });
            foreach (var e1 in JobInfo.EvosOf(gs.job)) foreach (var e2 in JobInfo.Evos2Of(e1.id)) paths.Add(new[] { e1.id, e2.id });
            int cur = 0;
            for (int i = 0; i < paths.Count; i++) if (paths[i][0] == gs.evo && paths[i][1] == gs.evo2) cur = i;
            var n = paths[(cur + 1) % paths.Count];
            SwitchJob(gs.job, n[0], n[1]);
        }

        public static void SwitchJob(string job, string evo, string evo2)
        {
            var gs = GameState.I;
            if (gs == null) return;
            gs.job = job; gs.jobChosen = true; gs.evo = evo; gs.evo2 = evo2;
            if (gs.level < GameState.LevelCap) SetLevel(GameState.LevelCap);
            foreach (var s in SkillDB.All) if (s.job == job) gs.learnedSkills.Add(s.id);
            // pasang 4 skill aktif terkuat milik jalur ini: evolusi ke-2, evolusi ke-1, lalu skill dasar; ultimate (tier 3) diutamakan
            var pool = new List<SkillDef>();
            foreach (var s in SkillDB.All) if (s.job == job && s.active && (s.evo == "" || s.evo == evo || s.evo == evo2)) pool.Add(s);
            pool.Sort((a, b) => Rank(b, evo, evo2).CompareTo(Rank(a, evo, evo2)));
            for (int i = 0; i < gs.skillSlots.Length; i++) gs.skillSlots[i] = null;
            // ultimate terkuat di slot tengah (V), sisanya mengisi Z X C R T Q
            int[] order = { 3, 0, 1, 2, 4, 5, 6 };
            for (int i = 0; i < pool.Count && i < order.Length; i++) gs.AssignSkill(order[i], pool[i].id);
            SkillExec.ResetCooldowns();
            var pc = PlayerCombat.I;
            if (pc != null) pc.FullRestore();
            string tag = job + (evo != "" ? " > " + evo : "") + (evo2 != "" ? " > " + evo2 : "");
            Say("JOB: " + tag + "  (skill dipasang di slot Z X C V R T Q)");
        }

        static int Rank(SkillDef s, string evo, string evo2)
        {
            int r = s.tier * 10;
            if (s.evo != "" && s.evo == evo2) r += 100; else if (s.evo != "" && s.evo == evo) r += 50;
            return r;
        }

        static void LearnAll()
        {
            var gs = GameState.I;
            foreach (var s in SkillDB.All) if (s.job == gs.job) gs.learnedSkills.Add(s.id);
            int slot = 0;
            foreach (var s in SkillDB.All)
                if (s.job == gs.job && s.active && slot < 7) { gs.AssignSkill(slot, s.id); slot++; }
        }

        static void RepUp()
        {
            var gd = GuildData.I;
            if (gd == null) return;
            int next = Mathf.Min(4, gd.TierIndex + 1);
            gd.reputation = Mathf.Max(gd.reputation, GuildConfig.TierMinPoints[next]);
        }

        void SetHour(float h)
        {
            var gs = GameState.I;
            gs.timeOfDay = h; frozenTime = h;
        }

        void LoadScene(string scene)
        {
            if (TownUI.I != null) TownUI.I.Close();
            if (Fly) SetFly(false);
            if (scene == "Town" && GameState.I != null) GameState.I.EndExpedition();
            SceneManager.LoadScene(scene);   // pemain muncul di titik awal bawaan scene
        }

        static List<Interactable> NearestInteractables(int max)
        {
            var list = new List<Interactable>(Interactable.All);
            list.RemoveAll(i => i == null);
            Vector3 p = PlayerPos();
            list.Sort((a, b) => Vector3.Distance(a.transform.position, p).CompareTo(Vector3.Distance(b.transform.position, p)));
            // lewati yang sedang sangat dekat (sudah di situ)
            var res = new List<Interactable>();
            foreach (var i in list) { if (res.Count >= max) break; res.Add(i); }
            return res;
        }

        static void MoveTo(Vector3 pos)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return;
            var cc = p.GetComponent<CharacterController>();
            bool was = cc != null && cc.enabled;
            if (cc != null) cc.enabled = false;
            p.transform.position = pos;
            if (cc != null) cc.enabled = was;
        }

        void TeleportNear(Transform t)
        {
            Vector3 dir = (PlayerPos() - t.position); dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = -t.forward;
            dir.Normalize();
            MoveTo(t.position + dir * 2.5f + Vector3.up * 1.2f);
            if (TownUI.I != null) TownUI.I.Close();
        }

        void TeleportToMonster()
        {
            Monster best = null; float bd = float.MaxValue; Vector3 p = PlayerPos();
            foreach (var m in Monster.All) { if (m == null || m.IsDead) continue; float d = Vector3.Distance(m.transform.position, p); if (d < bd && d > 6f) { bd = d; best = m; } }
            if (best == null) { TownUI.I.Toast("Tidak ada monster."); return; }
            MoveTo(best.transform.position - best.transform.forward * 8f + Vector3.up * 1.5f);
            TownUI.I.Close();
        }

        void SpawnMonster(string id, bool boss)
        {
            var pl = GameObject.FindGameObjectWithTag("Player");
            if (pl == null) return;
            Vector3 pos = pl.transform.position + pl.transform.forward * 8f;
            pos.y = RT.GroundY(pos);
            var go = new GameObject("Dev_" + id);
            go.transform.position = pos;
            var m = go.AddComponent<Monster>();
            m.defId = id; m.noLeash = true; m.isBoss = boss;
            if (boss) { m.hpMult = 3f; m.displayName = "[DEV] " + id; }
            TownUI.I.Close();
            TownUI.I.Toast("Spawn " + id);
        }

        void KillAll()
        {
            int n = 0;
            foreach (var m in new List<Monster>(Monster.All)) if (m != null && !m.IsDead) { m.TakeDamage(999999f); n++; }
            TownUI.I.Toast(n + " monster dibunuh.");
        }
    }
}
