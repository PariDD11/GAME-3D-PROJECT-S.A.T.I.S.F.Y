using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Event khusus: saat pemain masuk radius pemicu, sekelompok bandit muncul dari semua arah dan mengepung.
    /// Dipasang oleh HuntSceneBuilder di jalan hutan dan di desa terbengkalai. Selesai bila semua bandit tumbang (hadiah gold).
    /// </summary>
    public class BanditAmbush : MonoBehaviour
    {
        public float triggerRadius = 22f;
        [Range(0f, 1f)] public float chance = 1f;
        public int swordsmen = 6, archers = 3;
        public bool leader;
        public float spawnMin = 14f, spawnMax = 24f;
        public float waterLevel = 16f;
        public int reward = 120;
        public string title = "Penyergapan bandit!";

        bool done, running;
        readonly List<Monster> spawned = new List<Monster>();

        void Update()
        {
            if (running) { CheckVictory(); return; }
            if (done) return;
            var pc = PlayerCombat.I;
            if (pc == null || pc.IsDead) return;
            Vector3 d = pc.transform.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > triggerRadius * triggerRadius) return;
            done = true;
            if (Random.value <= chance) StartCoroutine(Run(pc));
        }

        IEnumerator Run(PlayerCombat pc)
        {
            running = true;
            if (TownUI.I != null) TownUI.I.Toast("Ranting patah... ada yang mengawasimu dari balik pepohonan.");
            yield return new WaitForSeconds(2.4f);
            if (pc == null) { running = false; yield break; }

            int total = swordsmen + archers + (leader ? 1 : 0);
            float baseAng = Random.value * 360f;
            for (int i = 0; i < total; i++)
            {
                string id = (leader && i == 0) ? "bandit_leader" : (i < swordsmen + (leader ? 1 : 0) ? "bandit" : "bandit_archer");
                bool archer = id == "bandit_archer";
                Vector3 p = pc.transform.position;
                bool ok = false;
                for (int tries = 0; tries < 8 && !ok; tries++)
                {
                    float ang = (baseAng + i * 360f / total + Random.Range(-14f, 14f) + tries * 23f) * Mathf.Deg2Rad;
                    float dist = archer ? Random.Range(spawnMax * 0.8f, spawnMax + 4f) : Random.Range(spawnMin, spawnMax);
                    p = pc.transform.position + new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang)) * dist;
                    p.y = RT.GroundY(p);
                    ok = p.y > waterLevel + 0.3f;
                }
                if (!ok) continue;
                var go = new GameObject("Bandit_" + i);
                go.transform.position = p;
                var m = go.AddComponent<Monster>();
                m.defId = id;
                m.noLeash = true;
                m.Alert();
                spawned.Add(m);
                RT.Flash(p + Vector3.up * 0.1f, Quaternion.identity, new Vector3(1.2f, 0.12f, 1.2f), new Color(0.35f, 0.3f, 0.22f), 0.5f);   // debu muncul
            }
            if (TownUI.I != null) TownUI.I.Toast(title + " " + spawned.Count + " bandit mengepungmu!");
        }

        void CheckVictory()
        {
            if (spawned.Count == 0) return;
            foreach (var m in spawned) if (m != null && !m.IsDead) return;
            enabled = false;
            var gs = GameState.I;
            if (gs != null) gs.gold += reward;
            if (TownUI.I != null) TownUI.I.Toast("Semua bandit tumbang! +" + reward + " gold dari jarahan. (ambil drop bandit di tanah)");
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, triggerRadius);
        }
    }
}
