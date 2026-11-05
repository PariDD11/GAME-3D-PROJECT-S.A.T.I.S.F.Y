using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Mesin Menara Eldrath: satu arena dipakai ulang untuk 100 lantai. Tiap lantai: spawn musuh sesuai tier, setelah semua kalah
    /// portal naik terbuka. Lantai kelipatan 10 = boss; mengalahkannya menyimpan checkpoint dan membuka peti hadiah.
    /// </summary>
    public class TowerManager : MonoBehaviour
    {
        public static TowerManager I { get; private set; }

        public Transform arenaCenter, spawnPoint;
        public float arenaRadius = 30f;
        public Renderer floorRenderer;

        // n89: Lantai Seratus (Docs/LORE_BIBLE.md 8.9). Dibuat oleh HuntSceneBuilder.TowerCanon.cs: aula pualam tanpa dinding dan langit-langit,
        // langit bintang, Lengkung Putih (Gerbang Terakhir), pecahan bilah, retakan hitam. Mati di semua lantai kecuali lantai terakhir.
        public GameObject hall100;
        public GameObject[] hideInHall;      // dinding bundar + langit-langit yang disembunyikan di lantai terakhir

        public int Floor { get; private set; }
        public bool Cleared { get; private set; }

        readonly List<Monster> mobs = new List<Monster>();
        TowerPortal nextPortal, exitPortal;
        GameObject chest;
        int faints, kills;
        bool leaving;
        Light fillLight;
        string banner = "";
        float bannerUntil;

        void Awake() { I = this; }

        void OnDestroy()
        {
            if (I == this) I = null;
            TowerChallenge.Active = "";
            PlayerCombat.OnFainted -= OnPlayerFainted;
        }

        void Start()
        {
            var gs = GameState.I;
            if (gs != null) gs.towerRuns++;
            PlayerCombat.OnFainted += OnPlayerFainted;

            FixHugeCapsules();
            Vector3 c = arenaCenter != null ? arenaCenter.position : Vector3.zero;
            Vector3 sp = spawnPoint != null ? spawnPoint.position : c + new Vector3(0, 0, -arenaRadius + 4f);
            exitPortal = TowerPortal.Create(transform, sp + new Vector3(-6f, 0f, 2f), TowerPortal.Kind.Exit, "Keluar dari Menara", new Color(0.4f, 0.7f, 1f));
            nextPortal = TowerPortal.Create(transform, c + new Vector3(0f, 0f, arenaRadius - 5f), TowerPortal.Kind.Next, "Naik ke lantai berikutnya", new Color(1f, 0.8f, 0.3f));
            nextPortal.gameObject.SetActive(false);

            var fl = new GameObject("FloorFill").AddComponent<Light>();
            fl.transform.SetParent(transform, false);
            fl.transform.position = c + Vector3.up * 14f;
            fl.type = LightType.Point; fl.range = arenaRadius * 2.4f; fl.intensity = 1.4f;
            fillLight = fl;

            int start = Mathf.Clamp(TowerRun.startFloor, 1, TowerDB.Floors);
            BeginFloor(start, true);
        }

        // ----------------------------------------------------- lantai
        void BeginFloor(int f, bool first)
        {
            Floor = f;
            Cleared = false;
            leaving = false;
            ClearMobs();
            if (chest != null) Destroy(chest);
            nextPortal.gameObject.SetActive(false);

            var gs = GameState.I;
            if (gs != null && f > gs.dungeonBest && !TowerChallenge.On) gs.dungeonBest = f;
            floorStart = Time.time; floorLimit = TowerChallenge.TimeLimit(f);

            Color tc = TowerDB.TierColor(f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.fogColor = tc;
            RenderSettings.ambientLight = Color.Lerp(tc, Color.white, 0.18f);
            if (fillLight != null) fillLight.color = Color.Lerp(Color.white, tc, 0.4f);
            if (floorRenderer != null) floorRenderer.material.color = Color.Lerp(new Color(0.28f, 0.28f, 0.3f), tc, 0.35f);
            ApplyHall(f);

            Vector3 c = arenaCenter != null ? arenaCenter.position : Vector3.zero;
            var pc = PlayerCombat.I;
            if (pc != null)
            {
                if (!first) MovePlayer(spawnPoint != null ? spawnPoint.position : c + new Vector3(0, 0, -arenaRadius + 4f));
                if (!first || f > 1) { pc.Heal(pc.MaxHp * (TowerDB.IsBoss(f - 1) || first ? 1f : 0.25f)); }
            }
            AllyManager.ReviveAndGather((spawnPoint != null ? spawnPoint.position : c) + Vector3.forward * 2f);

            if (TowerDB.IsBoss(f)) SpawnBossFloor(f, c); else SpawnFloor(f, c);

            banner = "LANTAI " + f + (TowerDB.IsBoss(f) ? "  -  BOSS" : "") + "\n" + TowerDB.TierName(f);
            bannerUntil = Time.time + 4f;
            if (TownUI.I != null)
                TownUI.I.Toast((TowerDB.IsBoss(f) ? "Lantai boss! " : "Lantai " + f + ". ") + "Level disarankan: " + TowerDB.RecommendedLevel(f) + ".");
        }

        /// <summary>Lantai terakhir = Lantai Seratus: aula pualam terbuka di bawah langit bintang (Kronik Bab 34). Lantai lain: kembali ke ruang bundar bertembok.</summary>
        void ApplyHall(int f)
        {
            bool hall = f >= TowerDB.Floors;
            if (hall100 != null) hall100.SetActive(hall);
            if (hideInHall != null) foreach (var g in hideInHall) if (g != null) g.SetActive(!hall);
            if (!hall) return;
            RenderSettings.fogColor = new Color(0.015f, 0.02f, 0.05f);
            RenderSettings.fogDensity = 0.0016f;
            RenderSettings.ambientLight = new Color(0.42f, 0.44f, 0.55f);
            if (fillLight != null) fillLight.color = new Color(0.85f, 0.9f, 1f);
            if (floorRenderer != null) floorRenderer.material.color = new Color(0.94f, 0.95f, 0.98f);
        }

        Vector3 RandomSpot(Vector3 c, float minR, float maxR)
        {
            for (int i = 0; i < 20; i++)
            {
                float a = Random.value * Mathf.PI * 2f, r = Random.Range(minR, maxR);
                Vector3 p = c + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                if (spawnPoint != null && (p - spawnPoint.position).sqrMagnitude < 14f * 14f) continue;
                return p;
            }
            return c + new Vector3(0f, 0f, arenaRadius * 0.5f);
        }

        Monster Spawn(string id, Vector3 pos, float hpMult, float atkMult, bool boss)
        {
            var go = new GameObject("Tower_" + id);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var m = go.AddComponent<Monster>();
            m.defId = id;
            m.hpMult = hpMult; m.atkMult = atkMult; m.isBoss = boss;
            m.noLeash = true;
            m.onDeath = OnMonsterDied;
            mobs.Add(m);
            return m;
        }

        void SpawnFloor(int f, Vector3 c)
        {
            var pool = TowerDB.Pool(f);
            int n = TowerDB.MobCount(f);
            for (int i = 0; i < n; i++)
                Spawn(pool[Random.Range(0, pool.Length)], RandomSpot(c, 6f, arenaRadius - 5f), TowerDB.HpMult(f), TowerDB.AtkMult(f), false);
            int e = TowerDB.EliteCount(f);
            var ep = TowerDB.ElitePool(f);
            for (int i = 0; i < e; i++)
            {
                var m = Spawn(ep[Random.Range(0, ep.Length)], c + new Vector3(Random.Range(-6f, 6f), 0f, arenaRadius * 0.35f), TowerDB.EliteHpMult(f), TowerDB.AtkMult(f) * 0.9f, false);
                m.displayName = "Elit " + MonsterDB.Get(m.defId).name;
            }
        }

        void SpawnBossFloor(int f, Vector3 c)
        {
            var boss = Spawn(TowerDB.BossId(f), c + new Vector3(0f, 0f, arenaRadius * 0.45f), 1f, 1f, true);
            var pool = TowerDB.Pool(f);
            int n = TowerDB.EscortCount(f);
            for (int i = 0; i < n; i++)
                Spawn(pool[Random.Range(0, pool.Length)], RandomSpot(c, 8f, arenaRadius - 6f), TowerDB.HpMult(f) * 0.8f, TowerDB.AtkMult(f) * 0.8f, false);
            var def = MonsterDB.Get(TowerDB.BossId(f));
            if (TownUI.I != null && def != null) TownUI.I.Toast("BOSS: " + def.name);
            boss.Alert();
        }

        void ClearMobs()
        {
            foreach (var m in mobs) if (m != null) Destroy(m.gameObject);
            mobs.Clear();
        }

        void MovePlayer(Vector3 pos)
        {
            var pc = PlayerCombat.I;
            if (pc == null) return;
            var cc = pc.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            pc.transform.position = pos + Vector3.up * 1.1f;
            pc.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            if (cc != null) cc.enabled = true;
        }

        // ----------------------------------------------------- progres
        void OnMonsterDied(Monster m)
        {
            kills++;
            HuntManager.Report("kill", "tower_any");
            var gs = GameState.I;
            if (gs != null) gs.AddStat("tower_kills");
        }

        float floorStart, floorLimit;

        /// <summary>Silinder primitif bawaan Unity memakai CapsuleCollider: pada lantai raksasa gepeng ia membesar menjadi BOLA (pemain di dalamnya jatuh menembus). Ganti dengan MeshCollider.</summary>
        void FixHugeCapsules()
        {
            foreach (var cap in FindObjectsByType<CapsuleCollider>(FindObjectsSortMode.None))
            {
                var t = cap.transform;
                float r = cap.radius * Mathf.Max(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.z));
                if (r < 8f) continue;
                var mf = cap.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                Destroy(cap);
                var mc = cap.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh;
            }
        }

        void Update()
        {
            var ppl = PlayerCombat.I;
            if (ppl != null && !leaving && ppl.transform.position.y < -6f)                     // pengaman: jatuh dari lantai -> kembali ke titik awal
                MovePlayer(spawnPoint != null ? spawnPoint.position : Vector3.zero);
            if (floorLimit > 0f && !Cleared && !leaving && Time.time - floorStart > floorLimit)
            {
                if (TownUI.I != null) TownUI.I.Toast("Waktu habis! Tantangan gagal.");
                StartCoroutine(EvacuateSoon());
                leaving = true;
                return;
            }
            if (Cleared || leaving || mobs.Count == 0) return;
            foreach (var m in mobs) if (m != null && !m.IsDead) return;
            OnFloorCleared();
        }

        void OnFloorCleared()
        {
            Audio.Sfx("tower_floor_clear");
            Cleared = true;
            var gs = GameState.I;
            int gold = Difficulty.Gold(TowerDB.ClearGold(Floor)), exp = TowerDB.ClearExp(Floor);
            string msg = "Lantai " + Floor + " bersih! +" + gold + "g, +" + Mathf.RoundToInt(exp * Difficulty.ExpMul) + " EXP.";
            if (gs != null)
            {
                gs.gold += gold;
                gs.AddExp(exp);
                gs.AddStat("tower_floors");
                if (Floor > gs.dungeonBest && !TowerChallenge.On) gs.dungeonBest = Floor;
                if (TowerChallenge.On) TowerChallenge.RecordClear(Floor);
            }
            Vector3 c = arenaCenter != null ? arenaCenter.position : Vector3.zero;
            if (TowerDB.IsBoss(Floor))
            {
                if (gs != null && Floor > gs.dungeonBossCleared && !TowerChallenge.On) gs.dungeonBossCleared = Floor;
                if (!TowerChallenge.On) msg += " Checkpoint tersimpan: mulai lagi dari lantai " + Mathf.Min(Floor + 1, TowerDB.Floors) + ".";
                var cg = new GameObject("BossChest");
                cg.transform.SetParent(transform, false);
                cg.transform.position = c + new Vector3(4f, 0f, arenaRadius * 0.45f);
                RT.Prim(PrimitiveType.Cube, cg.transform, new Vector3(0f, 0.45f, 0f), new Vector3(1.5f, 0.9f, 1f), new Color(0.45f, 0.28f, 0.1f));
                RT.Prim(PrimitiveType.Cube, cg.transform, new Vector3(0f, 0.95f, 0f), new Vector3(1.55f, 0.18f, 1.05f), new Color(0.9f, 0.7f, 0.15f));
                var ch = cg.AddComponent<TowerChest>();
                ch.floor = Floor; ch.prompt = "Buka peti hadiah boss"; ch.radius = 3.2f;
                chest = cg;
            }
            if (Floor >= TowerDB.Floors)
            {
                msg += " Menara ditaklukkan!";
                nextPortal.prompt = "Selesai: kembali ke Eldrath";
            }
            else nextPortal.prompt = "Naik ke lantai " + (Floor + 1);
            nextPortal.gameObject.SetActive(true);
            if (TownUI.I != null) TownUI.I.Toast(msg);
        }

        public void GoNext()
        {
            if (leaving) return;
            if (Floor >= TowerDB.Floors) { Leave(); return; }
            leaving = true;
            ScreenFader.I.Transition(() => BeginFloor(Floor + 1, false), 0.4f, 0.6f);
        }

        // ----------------------------------------------------- keluar & pingsan
        void OnPlayerFainted()
        {
            faints++;
            if (TowerChallenge.Is("oneshot")) { if (TownUI.I != null) TownUI.I.Toast("Satu Nyawa: tantangan gagal."); faints = 3; }
            if (TownUI.I != null && faints < 3) TownUI.I.Toast("Pingsan " + faints + "/3 di Menara. Tiga kali pingsan = diusir keluar.");
            if (faints >= 3) StartCoroutine(EvacuateSoon());
        }

        System.Collections.IEnumerator EvacuateSoon()
        {
            yield return new WaitForSeconds(2.5f);
            var gs = GameState.I;
            if (gs != null && gs.activeQuest != null && !gs.activeQuest.completed && HuntManager.I != null)
            {
                HuntManager.I.FailQuest();      // quest Menara gagal; kembali lewat DelayedReturn
                yield break;
            }
            if (TownUI.I != null) TownUI.I.Toast("Kamu terpaksa mundur dari Menara.");
            Leave();
        }

        /// <summary>Keluar ke Kota Eldrath. Tanpa quest, party dilepas dengan cooldown seperti quest biasa.</summary>
        public void Leave()
        {
            if (leaving && Floor <= 0) return;
            leaving = true;
            var gs = GameState.I;
            var gd = GuildData.I;
            if (gs != null && gd != null && gs.activeQuest == null && gd.party.Count > 0)
                gd.ReleaseParty(Mathf.Clamp(TowerDB.Tier(Floor) + 1, 1, 8), Cleared || Floor > 1);
            if (HuntManager.I != null) HuntManager.I.ReturnToTown();
        }

        // ----------------------------------------------------- HUD
        GUIStyle big, small;

        void OnGUI()
        {
            if (big == null)
            {
                big = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                big.normal.textColor = new Color(1f, 0.9f, 0.55f);
                small = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.UpperCenter };
                small.normal.textColor = Color.white;
            }
            int alive = 0;
            foreach (var m in mobs) if (m != null && !m.IsDead) alive++;
            string line = "Menara Eldrath  |  Lantai " + Floor + "/" + TowerDB.Floors + "  |  " + TowerDB.TierName(Floor) + "  |  " +
                          (Cleared ? "BERSIH - naik lewat portal emas" : "Musuh tersisa: " + alive);
            if (TowerChallenge.On) line += "  |  Tantangan: " + System.Array.Find(TowerChallenge.Rules, r => r.id == TowerChallenge.Active).name;
            if (floorLimit > 0f && !Cleared) line += "  |  Sisa waktu " + Mathf.Max(0, Mathf.CeilToInt(floorLimit - (Time.time - floorStart))) + " dtk";
            GUI.Label(new Rect(0, 8, Screen.width, 34), line, small);
            if (Time.time < bannerUntil)
                GUI.Label(new Rect(0, Screen.height * 0.22f, Screen.width, 140), banner, big);
        }
    }
}
