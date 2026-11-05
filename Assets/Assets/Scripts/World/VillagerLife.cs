using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// Kehidupan warga (otomatis, tanpa rebuild scene): warga pengembara tidur di malam hari, berteduh saat badai/blizzard,
    /// berjalan cepat saat hujan, dan sesekali berkomentar sesuai waktu dan cuaca (menyiram, mencangkul, bercakap-cakap).
    /// </summary>
    public class VillagerLife : MonoBehaviour
    {
        class V { public WanderNPC w; public NpcActor a; public Renderer[] rs; public float baseSpeed, bed, wake; public bool hidden; }
        readonly List<V> list = new List<V>();
        float scanT, tickT, talkT;
        static bool made;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (made) return; made = true;
            var go = new GameObject("VillagerLife");
            DontDestroyOnLoad(go);
            go.AddComponent<VillagerLife>();
        }

        void OnEnable() { SceneManager.sceneLoaded += (s, m) => { list.Clear(); scanT = 1.5f; }; }

        void Scan()
        {
            list.Clear();
            foreach (var w in FindObjectsByType<WanderNPC>(FindObjectsSortMode.None))
            {
                if (w.waypoints == null || w.waypoints.Length < 2) continue;      // penjaga tetap berjaga
                if (w.transform.parent != null && w.transform.parent.name == "EventCrowdNPCs") continue;      // tamu event tidak ikut tidur
                var v = new V { w = w, a = w.GetComponent<NpcActor>(), rs = w.GetComponentsInChildren<Renderer>(true), baseSpeed = w.speed };
                float h = (Mathf.Abs(w.gameObject.name.GetHashCode()) % 100) / 100f;
                v.bed = 21f + h * 2.5f; v.wake = 5f + h * 1.5f;
                list.Add(v);
            }
        }

        static bool Night(V v, float t) { return t >= v.bed || t < v.wake; }

        void Update()
        {
            if (GameState.I == null || !SeasonRuntime.Outdoors) return;
            scanT -= Time.deltaTime;
            if (scanT <= 0f) { scanT = 12f; Scan(); }
            tickT -= Time.deltaTime;
            if (tickT > 0f) return;
            tickT = 1f;
            float t = GameState.I.timeOfDay;
            var wx = SeasonRuntime.Weather;
            bool storm = wx == Wx.Badai || wx == Wx.Blizzard;
            foreach (var v in list)
            {
                if (v.w == null) continue;
                bool hide = storm || Night(v, t);
                if (hide != v.hidden)
                {
                    v.hidden = hide;
                    v.w.enabled = !hide;
                    foreach (var r in v.rs) if (r != null) r.enabled = !hide;
                    var col = v.w.GetComponent<Collider>(); if (col != null) col.enabled = !hide;
                    foreach (var it in v.w.GetComponentsInChildren<Interactable>(true)) if (it != null) it.enabled = !hide;   // v35z102n13: warga yang tidur tidak boleh bisa diajak bicara (prompt hantu)
                }
                v.w.speed = v.baseSpeed * (wx == Wx.Hujan ? 1.6f : wx == Wx.Salju ? 0.85f : 1f);
            }
            talkT -= 1f;
            if (talkT <= 0f)
            {
                talkT = Random.Range(10f, 22f);
                var pl = PlayerCombat.I;
                if (pl == null || list.Count == 0) return;
                var v = list[Random.Range(0, list.Count)];
                if (v.w == null || v.hidden || v.a == null) return;
                if (Vector3.Distance(v.w.transform.position, pl.transform.position) > 22f) return;
                v.a.Say(Line(t, wx));
            }
        }

        static string Line(float t, Wx wx)
        {
            var pool = new List<string>();
            if (wx == Wx.Hujan) { pool.Add("Hujan lagi... cucian belum kering."); pool.Add("Cepat, cepat, jangan sampai basah!"); }
            else if (wx == Wx.Salju) { pool.Add("Dingin sekali! Aku sedang mengumpulkan kayu bakar."); pool.Add("Salju pertama selalu cantik, ya."); }
            else if (t < 9f) { pool.Add("Pagi! Aku mau menyiram kebun dulu."); pool.Add("Roti hangat dari pandai kue, enak sekali."); }
            else if (t < 16f) { pool.Add("Tanahnya subur, aku sedang mencangkul."); pool.Add("Kemarin ada Hunter membawa ogre tusk sebesar ini!"); pool.Add("Sudah dengar kabar Gubernur dan Guild?"); }
            else { pool.Add("Sebentar lagi gelap, waktunya pulang."); pool.Add("Malam ini aku buat sup. Mau mampir?"); pool.Add("Katanya Hollow Circle berkeliaran di luar kota..."); }
            return pool[Random.Range(0, pool.Count)];
        }
    }
}
