using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// Memunculkan kembali monster kecil (Slime, Goblin, dll.) yang sudah mati di area perburuan, supaya hutan tidak kosong.
    /// Dipasang otomatis di scene yang punya HuntManager; boss/monster besar dan boneka latihan tidak ikut.
    /// Monster baru hanya muncul bila pemain berada jauh dari titik asalnya.
    /// </summary>
    public class MonsterRespawner : MonoBehaviour
    {
        public float delay = 75f;
        public float minPlayerDistance = 38f;

        class Slot { public string id; public Vector3 pos; public Transform parent; public GameObject live; public float deadAt = -1f; }
        readonly List<Slot> slots = new List<Slot>();
        Transform player;
        float started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
            Install();
        }
        static void OnLoaded(Scene s, LoadSceneMode m) { Install(); }

        static void Install()
        {
            if (FindFirstObjectByType<MonsterRespawner>() != null) return;
            if (FindFirstObjectByType<HuntManager>() == null) return;
            if (FindFirstObjectByType<TowerManager>() != null) return;
            new GameObject("MonsterRespawner").AddComponent<MonsterRespawner>();
        }

        void Start() { started = Time.time; }

        void Collect()
        {
            foreach (var m in Monster.All)
            {
                if (m == null || m.IsDead) continue;
                var d = MonsterDB.Get(m.DefId);
                if (d == null || d.passive || d.large || m.DefId == "dummy" || m.DefId.StartsWith("bandit")) continue;
                slots.Add(new Slot { id = m.DefId, pos = m.transform.position, parent = m.transform.parent, live = m.gameObject });
            }
        }

        void Update()
        {
            if (slots.Count == 0)
            {
                if (Time.time - started > 1.5f && Monster.All.Count > 0 && !collected) { collected = true; Collect(); }
                return;
            }
            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
                if (player == null) return;
            }
            foreach (var s in slots)
            {
                if (s.live != null) continue;
                if (s.deadAt < 0f) s.deadAt = Time.time;
                if (Time.time - s.deadAt < delay * WorldReact.RespawnMul()) continue;
                Vector3 d = player.position - s.pos; d.y = 0f;
                if (d.magnitude < minPlayerDistance) continue;
                var go = new GameObject("Monster_" + s.id);
                go.transform.SetParent(s.parent, false);
                go.transform.position = s.pos;
                go.AddComponent<Monster>().defId = s.id;
                s.live = go; s.deadAt = -1f;
            }
        }
        bool collected;
    }
}
