using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Koreografi skill: tiap skill punya timeline fase (pose tubuh per fase + efek pada waktu tertentu + saat damage terjadi).
    /// Skill yang punya ActionDef tidak lagi memakai pose Cast() umum, sehingga tiap skill/ultimate punya gerak dan efek sendiri.
    /// Definisi per job ada di SkillActions.*.cs (satu fungsi per skill).
    /// </summary>
    public class ActionDef
    {
        public string id;
        public float dur = 1f;           // panjang total animasi (detik)
        public float lockUntil = 0.8f;   // serangan dasar/skill lain terkunci sampai detik ini
        public float hitAt = 0.5f;       // detik terjadinya damage (memanggil Run skill)
        public readonly List<float> times = new List<float>();
        public readonly List<PlayerAnimator.Pose> poses = new List<PlayerAnimator.Pose>();
        public readonly List<KeyValuePair<float, Action<PlayerCombat>>> events = new List<KeyValuePair<float, Action<PlayerCombat>>>();

        /// <summary>Pose pada detik t (waktu absolut dalam durasi).</summary>
        public ActionDef Key(float t, PlayerAnimator.Pose p) { times.Add(t / dur); poses.Add(p); return this; }
        public ActionDef At(float t, Action<PlayerCombat> fn) { events.Add(new KeyValuePair<float, Action<PlayerCombat>>(t, fn)); return this; }
    }

    public static partial class SkillAction
    {
        static readonly Dictionary<string, ActionDef> defs = new Dictionary<string, ActionDef>();
        static bool built;

        static void Ensure() { if (built) return; built = true; RegisterAll(); }
        static void Add(ActionDef d) { defs[d.id] = d; }
        public static bool Has(string id) { Ensure(); return defs.ContainsKey(id); }
        /// <summary>Detik sampai damage terjadi (0 bila skill tanpa koreografi): untuk menjadwalkan efek yang harus muncul saat skill "meledak".</summary>
        public static float HitDelay(string id) { Ensure(); ActionDef d; return defs.TryGetValue(id, out d) ? d.hitAt : 0f; }

        /// <summary>Jalankan aksi skill: pose, efek terjadwal, dan damage (hit) pada waktunya.</summary>
        public static void Play(SkillDef s, PlayerCombat pc, Action hit)
        {
            Ensure();
            ActionDef d;
            if (!defs.TryGetValue(s.id, out d)) { if (hit != null) hit(); return; }
            pc.LockAttack(d.lockUntil);
            if (PlayerAvatar.I != null && d.times.Count > 1) PlayerAvatar.I.PlayCustom(d.dur, d.times.ToArray(), d.poses.ToArray());
            CombatFx.Host.I.StartCoroutine(Run(d, pc, hit));
        }

        static IEnumerator Run(ActionDef d, PlayerCombat pc, Action hit)
        {
            var evs = new List<KeyValuePair<float, Action<PlayerCombat>>>(d.events);
            evs.Add(new KeyValuePair<float, Action<PlayerCombat>>(d.hitAt, p => { if (hit != null) hit(); }));
            evs.Sort((a, b) => a.Key.CompareTo(b.Key));
            float t = 0f;
            foreach (var e in evs)
            {
                while (t < e.Key) { if (pc == null) yield break; yield return null; t += Time.deltaTime; }
                if (pc == null) yield break;
                if (e.Value != null) e.Value(pc);
            }
        }

        // ---------------------------------------------------------------- pembantu waktu & aura
        /// <summary>Hit-stop singkat (detik waktu nyata): permainan nyaris berhenti supaya hantaman terasa berat.</summary>
        public static void HitStop(float real, float scale = 0.04f)
        {
            if (Time.timeScale < 0.5f) return;      // sudah dalam slow-mo / jeda
            CombatFx.Host.I.StartCoroutine(HitStopRoutine(Mathf.Min(real, 0.075f) * 0.8f, Mathf.Max(scale, 0.12f)));   // lebih singkat & tidak membeku total: terasa berat tanpa tersendat
        }
        static IEnumerator HitStopRoutine(float real, float scale)
        {
            float prev = Time.timeScale;
            Time.timeScale = scale;
            yield return new WaitForSecondsRealtime(real);
            // pulih bertahap (bukan loncat) supaya gerak tidak "tersentak" saat jeda berakhir
            if (Mathf.Abs(Time.timeScale - scale) > 0.001f) yield break;       // ada slow-mo lain yang mengambil alih
            float el = 0f, last = scale;
            while (el < 0.07f)
            {
                el += Time.unscaledDeltaTime;
                if (Mathf.Abs(Time.timeScale - last) > 0.01f) yield break;     // diubah pihak lain: jangan ditimpa
                last = Mathf.Lerp(scale, prev, Mathf.SmoothStep(0f, 1f, el / 0.07f));
                Time.timeScale = last;
                yield return null;
            }
            if (Mathf.Abs(Time.timeScale - last) < 0.01f) Time.timeScale = prev;
        }

        /// <summary>Aura berpartikel naik dari tubuh dan pendar di ujung pedang selama secs detik.</summary>
        public static void Aura(PlayerCombat pc, Color col, float secs, float strength = 1f)
        {
            CombatFx.Host.I.StartCoroutine(AuraRoutine(pc, col, secs, strength));
        }
        static IEnumerator AuraRoutine(PlayerCombat pc, Color col, float secs, float strength)
        {
            float t = 0f;
            while (t < secs && pc != null)
            {
                CombatFx.Burst(Feet(pc) + Vector3.up * 0.3f, col, Mathf.RoundToInt(5 * strength), 0.5f, 2.6f, 0.9f, 0.12f);
                CombatFx.Flash(Tip(pc), col, 3.5f * strength, 0.7f * strength, 0.12f);
                yield return new WaitForSeconds(0.06f);
                t += 0.06f;
            }
        }

        /// <summary>Partikel cahaya berputar menuju ujung pedang (pengumpulan energi).</summary>
        public static void Converge(PlayerCombat pc, Color col, float secs, float radius = 1.4f)
        {
            CombatFx.Host.I.StartCoroutine(ConvergeRoutine(pc, col, secs, radius));
        }
        static IEnumerator ConvergeRoutine(PlayerCombat pc, Color col, float secs, float radius)
        {
            float t = 0f, ang = 0f;
            while (t < secs && pc != null)
            {
                Vector3 tip = Tip(pc);
                for (int i = 0; i < 3; i++)
                {
                    ang += 2.1f;
                    Vector3 off = new Vector3(Mathf.Cos(ang), UnityEngine.Random.Range(-0.6f, 0.9f), Mathf.Sin(ang)).normalized * radius;
                    var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    UnityEngine.Object.Destroy(q.GetComponent<Collider>());
                    q.name = "FxConverge";
                    q.transform.position = tip + off;
                    q.GetComponent<MeshRenderer>().sharedMaterial = CombatFx.Mat(CombatFx.Soft(), col);
                    var fx = q.AddComponent<CombatFx.FxAnim>();
                    fx.life = 0.22f; fx.billboard = true; fx.color = col;
                    fx.s0 = Vector3.one * 0.32f; fx.s1 = Vector3.one * 0.05f; fx.vel = -off / 0.22f;
                }
                yield return new WaitForSeconds(0.04f);
                t += 0.04f;
            }
        }

        // ---------------------------------------------------------------- pembantu efek
        public static Vector3 Feet(PlayerCombat pc) { return pc.transform.position - Vector3.up * 0.9f; }
        public static Vector3 Chest(PlayerCombat pc) { return pc.transform.position + Vector3.up * 1.0f; }
        public static Vector3 Tip(PlayerCombat pc) { return PlayerAvatar.I != null ? PlayerAvatar.I.WeaponTip : Chest(pc); }

        /// <summary>Monster hidup di depan pemain (jarak dan setengah sudut).</summary>
        public static List<Monster> InFront(PlayerCombat pc, float range, float halfAngle)
        {
            var l = new List<Monster>();
            Vector3 fwd = pc.transform.forward;
            foreach (var m in new List<Monster>(Monster.All))
            {
                if (m == null || m.IsDead) continue;
                Vector3 d = m.HitPoint - pc.transform.position; d.y = 0f;
                float dist = d.magnitude;
                if (dist > range + m.Radius) continue;
                if (dist > m.Radius + 0.5f && Vector3.Angle(fwd, d) > halfAngle) continue;
                l.Add(m);
            }
            return l;
        }

        /// <summary>Titik tanah di depan pemain (raycast), untuk retakan dan debu.</summary>
        public static bool GroundFront(PlayerCombat pc, float dist, out Vector3 p)
        {
            Vector3 o = pc.transform.position + pc.transform.forward * dist + Vector3.up * 2f;
            RaycastHit h;
            if (Physics.Raycast(o, Vector3.down, out h, 6f, ~0, QueryTriggerInteraction.Ignore) && h.normal.y > 0.7f) { p = h.point; return true; }
            p = Feet(pc) + pc.transform.forward * dist; return false;
        }
    }
}
