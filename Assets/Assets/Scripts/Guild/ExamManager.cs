using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Arena Ujian Guild (scene ArenaExam): membaca ujian yang sedang berjalan, memunculkan lawan sesuai jenis ujian,
    /// meluluskan pemain bila semua lawan tumbang, dan menggagalkan bila pemain pingsan.</summary>
    public class ExamManager : MonoBehaviour
    {
        public static ExamManager I { get; private set; }
        public Transform arenaCenter, spawnPoint;
        public float arenaRadius = 24f;

        int target = -1;
        string kind = "";
        readonly List<Monster> foes = new List<Monster>();
        float startAt;
        bool started, finished;
        string banner = "";
        float bannerUntil;
        GUIStyle big, small;

        void Awake() { I = this; }
        void OnDestroy() { if (I == this) I = null; PlayerCombat.OnFainted -= OnFainted; }

        void Start()
        {
            var gs = GameState.I;
            if (gs != null)
                for (int t = 3; t <= 8; t++)
                    if (gs.flags.Contains("exam_running_" + t)) target = t;
            if (target < 0)
            {
                if (TownUI.I != null) TownUI.I.Toast("Tidak ada ujian yang berjalan. Kembali ke kota.");
                StartCoroutine(LeaveSoon(1.5f));
                finished = true;
                return;
            }
            kind = GuildServices.ExamKind(target);
            PlayerCombat.OnFainted += OnFainted;
            startAt = Time.time + 4f;
            banner = "UJIAN RANK " + GameState.Ranks[target] + "\n" + GuildServices.ExamOpponent(target);
            bannerUntil = Time.time + 4f;
            if (gs != null && PlayerCombat.I != null) PlayerCombat.I.Heal(PlayerCombat.I.MaxHp);
        }

        // ------------------------------------------------------------ lawan
        Monster Spawn(string id, Vector3 pos, float hp, float atk, string name)
        {
            var go = new GameObject("Exam_" + id);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var m = go.AddComponent<Monster>();
            m.defId = id; m.hpMult = hp; m.atkMult = atk; m.isBoss = true; m.noLeash = true;
            if (!string.IsNullOrEmpty(name)) m.displayName = name;
            foes.Add(m);
            return m;
        }

        void Begin()
        {
            started = true;
            Vector3 c = arenaCenter != null ? arenaCenter.position : Vector3.zero;
            float t = target - 3;                       // 1 (C) .. 5 (SS)
            if (kind == "ringan")
            {
                float k = target == 5 ? 1.5f : 1f;
                Spawn("arena_warden", c + new Vector3(0f, 0f, arenaRadius * 0.5f), k, 1f + (k - 1f) * 0.5f, null).Alert();
            }
            else if (kind == "menengah")
            {
                float k = target == 7 ? 1.7f : 1f;
                Spawn("gladiator_elite", c + new Vector3(0f, 0f, arenaRadius * 0.5f), k, 1f + (k - 1f) * 0.45f, null).Alert();
            }
            else
            {
                string[] ids = { "srank_blade", "srank_shadow", "srank_ash" };
                for (int i = 0; i < ids.Length; i++)
                {
                    float a = (i - 1) * 0.75f;
                    Spawn(ids[i], c + new Vector3(Mathf.Sin(a) * arenaRadius * 0.55f, 0f, Mathf.Cos(a) * arenaRadius * 0.55f), 1f, 1f, null).Alert();
                }
            }
            if (TownUI.I != null) TownUI.I.Toast("Ujian dimulai! Kalahkan " + (foes.Count > 1 ? "semua lawan." : "lawanmu."));
            Audio.Sfx("boss_roar");
        }

        void Update()
        {
            if (finished) return;
            if (!started) { if (Time.time >= startAt) Begin(); return; }
            foreach (var m in foes) if (m != null && !m.IsDead) return;
            Win();
        }

        // ------------------------------------------------------------ hasil
        void Win()
        {
            finished = true;
            var gs = GameState.I;
            string msg = "";
            if (gs != null)
            {
                msg = GuildServices.PassExam(gs, target);
                gs.gold += 150 * (target - 2);
                gs.AddExp(120 * (target - 2));
                gs.flags.Remove("exam_running_" + target);
            }
            banner = "UJIAN LULUS!\n" + msg;
            bannerUntil = Time.time + 6f;
            Audio.Sfx("victory");
            if (TownUI.I != null) TownUI.I.Toast(msg + " Hadiah +" + (150 * (target - 2)) + "g.");
            StartCoroutine(LeaveSoon(6f));
        }

        void OnFainted()
        {
            if (finished || !started) return;
            finished = true;
            var gs = GameState.I;
            if (gs != null) gs.flags.Remove("exam_running_" + target);
            banner = "UJIAN GAGAL\nKamu boleh mencoba lagi setelah pulih.";
            bannerUntil = Time.time + 5f;
            Audio.Sfx("defeat");
            if (TownUI.I != null) TownUI.I.Toast("Kamu kalah. Persetujuan ujian tetap berlaku; temui Pengawas lagi untuk mengulang.");
            foreach (var m in foes) if (m != null && !m.IsDead) Destroy(m.gameObject);
            StartCoroutine(LeaveSoon(4.5f));
        }

        System.Collections.IEnumerator LeaveSoon(float t)
        {
            yield return new WaitForSeconds(t);
            if (HuntManager.I != null) HuntManager.I.ReturnToTown();
            else SceneRouter.GoToSpawn(GuildConfig.TownScene, "SpawnFromDungeon");
        }

        void OnGUI()
        {
            if (big == null)
            {
                big = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                big.normal.textColor = new Color(1f, 0.9f, 0.55f);
                small = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.UpperCenter };
                small.normal.textColor = Color.white;
            }
            if (target < 0) return;
            int alive = 0;
            foreach (var m in foes) if (m != null && !m.IsDead) alive++;
            string line = "Arena Ujian Guild  |  Rank " + GameState.Ranks[target] + "  |  " + (!started ? "Bersiap " + Mathf.CeilToInt(Mathf.Max(0f, startAt - Time.time)) : finished ? "Selesai" : "Lawan tersisa: " + alive);
            GUI.Label(new Rect(0, 8, Screen.width, 34), line, small);
            if (Time.time < bannerUntil) GUI.Label(new Rect(0, Screen.height * 0.22f, Screen.width, 160), banner, big);
        }
    }
}
