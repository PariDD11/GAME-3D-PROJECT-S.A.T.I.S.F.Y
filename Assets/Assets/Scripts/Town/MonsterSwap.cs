using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n78: jalur ganti model monster. Bila Assets/Resources/MonsterSwapTable.asset punya aturan yang cocok dengan monster
    /// (id / bentuk / boss / scene), prefab milik pengguna dipasang sebagai pengganti model bawaan (MonsterKit).
    /// Prefab dicocokkan ke tinggi model bawaan, collider dan skrip bawaannya dimatikan, lalu Animator (bila ada) diberi parameter:
    ///   Speed (float, m/dtk), Telegraph (bool, saat ancang-ancang), Attack (trigger, mulai ancang-ancang), Hurt (trigger), Dead (bool) / Die (trigger).
    /// Parameter yang tidak ada dilewati tanpa error. Model tanpa Animator digerakkan secara prosedural (naik-turun, miring, menerjang).
    /// </summary>
    public static class MonsterSwapRuntime
    {
        /// <summary>Semua nilai MonsterDef.shape yang punya model bawaan (dipakai editor untuk pilihan "bentuk").</summary>
        public static readonly string[] Shapes =
        {
            "wolf", "bandit", "leader", "archer", "goblin", "troll", "zombie", "skeleton", "mummy", "dummy", "witch", "necro",
            "wraith", "slime", "chicken", "scorpion", "golem", "boar", "bull", "cerberus", "sheep", "goat", "cow", "lizard"
        };

        static MonsterSwapTable table;
        static bool loaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { table = null; loaded = false; }

        public static MonsterSwapTable Table()
        {
            if (!loaded) { loaded = true; table = Resources.Load<MonsterSwapTable>("MonsterSwapTable"); }
            return table;
        }

        public static bool HasPrefab(MonsterSwapTable.Rule r)
        {
            if (r == null || r.prefabs == null) return false;
            for (int i = 0; i < r.prefabs.Count; i++) if (r.prefabs[i] != null) return true;
            return false;
        }

        static int Score(MonsterSwapTable.Rule r, MonsterDef d, bool boss, string scene)
        {
            if (r == null || !r.enabled || !HasPrefab(r)) return -1;
            int sc = 0;
            if (!string.IsNullOrEmpty(r.monsterId)) { if (!string.Equals(r.monsterId.Trim(), d.id, StringComparison.OrdinalIgnoreCase)) return -1; sc += 16; }
            if (!string.IsNullOrEmpty(r.shape)) { if (!string.Equals(r.shape.Trim(), d.shape, StringComparison.OrdinalIgnoreCase)) return -1; sc += 4; }
            if (r.tier != 0) { if ((r.tier == 1) != boss) return -1; sc += 2; }
            if (!string.IsNullOrEmpty(r.scene)) { if (r.scene != scene) return -1; sc += 1; }
            return sc;
        }

        /// <summary>Aturan terbaik untuk monster ini (null = pakai model bawaan). Skor sama: yang paling atas di daftar menang.</summary>
        public static MonsterSwapTable.Rule Match(MonsterSwapTable t, MonsterDef d, bool boss, string scene)
        {
            if (t == null || d == null) return null;
            MonsterSwapTable.Rule best = null; int bs = -1;
            for (int i = 0; i < t.rules.Count; i++)
            {
                int s = Score(t.rules[i], d, boss, scene);
                if (s > bs) { bs = s; best = t.rules[i]; }
            }
            return best;
        }

        /// <summary>Pasang model pengganti di bawah visual. Mengembalikan null bila tidak ada aturan yang cocok (pemanggil memakai model bawaan).</summary>
        public static MonsterSwapModel TryApply(Monster m, MonsterDef def, Transform visual)
        {
            var t = Table();
            if (t == null || t.rules.Count == 0 || m == null || def == null || visual == null) return null;
            var r = Match(t, def, m.isBoss, SceneManager.GetActiveScene().name);
            if (r == null) return null;

            var list = new List<GameObject>();
            for (int i = 0; i < r.prefabs.Count; i++) if (r.prefabs[i] != null) list.Add(r.prefabs[i]);
            if (list.Count == 0) return null;
            GameObject prefab = list[list.Count == 1 ? 0 : UnityEngine.Random.Range(0, list.Count)];

            var model = UnityEngine.Object.Instantiate(prefab, visual, false);
            model.name = "Model";
            model.SetActive(true);
            var mt = model.transform;
            mt.localPosition = Vector3.zero; mt.localRotation = Quaternion.Euler(0f, r.yaw, 0f); mt.localScale = Vector3.one;
            // fisika/AI bawaan prefab dimatikan: monster digerakkan oleh Monster.cs, tabrakan lewat kapsul di akar
            foreach (var c in model.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
            foreach (var rb in model.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(rb);
            foreach (var b in model.GetComponentsInChildren<Behaviour>(true)) if (b != null && !(b is Animator)) b.enabled = false;

            // ukuran: setinggi model bawaan monster ini (x skala tinggi), telapak kaki di titik akar
            float refH = MonsterKit.RefHeight(def);
            float h = Mathf.Max(0.3f, refH * Mathf.Max(0.05f, def.size) * Mathf.Max(0.1f, r.heightScale));
            ExternalModelDriver.FitTo(mt, h);
            if (Mathf.Abs(r.yOffset) > 0.0001f) mt.position += Vector3.up * r.yOffset;

            var an = model.GetComponentInChildren<Animator>(true);
            if (an != null)
            {
                if (r.controller != null) an.runtimeAnimatorController = r.controller;
                an.applyRootMotion = false;
                an.speed = r.animSpeed > 0f ? r.animSpeed : 1f;
                an.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            }
            return new MonsterSwapModel(mt, an, r, h);
        }
    }

    /// <summary>Penggerak satu model pengganti: parameter Animator, gerak prosedural cadangan, dan kilatan warna (kena pukul / ancang-ancang).</summary>
    public sealed class MonsterSwapModel
    {
        static readonly int IdBase = Shader.PropertyToID("_BaseColor"), IdColor = Shader.PropertyToID("_Color");

        sealed class TintRef { public Renderer r; public Color[] c0; }

        readonly Transform model;
        readonly Animator anim;
        readonly float speedScale, height;
        readonly bool life;                                         // gerak prosedural (model tanpa Animator / tanpa controller)
        readonly Vector3 basePos, baseScale;
        readonly Quaternion baseRot;
        readonly Dictionary<string, AnimatorControllerParameterType> pars = new Dictionary<string, AnimatorControllerParameterType>();
        readonly List<TintRef> tint = new List<TintRef>();
        MaterialPropertyBlock mpb;
        float speedSm, phase, time, lunge, wind, hurt, lastAmt;
        Color lastTo;
        bool wasWind, dead;

        public MonsterSwapModel(Transform model, Animator anim, MonsterSwapTable.Rule r, float height)
        {
            this.model = model; this.anim = anim; this.height = height;
            speedScale = r.speedParamScale > 0f ? r.speedParamScale : 1f;
            bool hasCtrl = anim != null && anim.runtimeAnimatorController != null;
            if (hasCtrl) { var ps = anim.parameters; for (int i = 0; i < ps.Length; i++) pars[ps[i].name] = ps[i].type; }
            life = r.lifeMotion && !hasCtrl;
            basePos = model.localPosition; baseRot = model.localRotation; baseScale = model.localScale;

            // renderer yang bisa diwarnai lewat MaterialPropertyBlock (tidak menggandakan bahan milik prefab)
            var rds = model.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rds.Length; i++)
            {
                var rd = rds[i];
                if (!(rd is MeshRenderer) && !(rd is SkinnedMeshRenderer)) continue;
                var ms = rd.sharedMaterials; var c0 = new Color[ms.Length]; bool any = false;
                for (int k = 0; k < ms.Length; k++)
                {
                    c0[k] = Color.white;
                    var mt = ms[k]; if (mt == null) continue;
                    if (mt.HasProperty(IdBase)) { c0[k] = mt.GetColor(IdBase); any = true; }
                    else if (mt.HasProperty(IdColor)) { c0[k] = mt.GetColor(IdColor); any = true; }
                }
                if (any) tint.Add(new TintRef { r = rd, c0 = c0 });
            }
        }

        bool Has(string n, AnimatorControllerParameterType t) { AnimatorControllerParameterType x; return pars.TryGetValue(n, out x) && x == t; }
        void SetF(string n, float v) { if (anim != null && Has(n, AnimatorControllerParameterType.Float)) anim.SetFloat(n, v); }
        void SetB(string n, bool v) { if (anim != null && Has(n, AnimatorControllerParameterType.Bool)) anim.SetBool(n, v); }
        void Trig(string n) { if (anim != null && Has(n, AnimatorControllerParameterType.Trigger)) anim.SetTrigger(n); }

        /// <summary>Dipanggil tiap frame oleh Monster. speed = laju gerak (m/dtk); winding = sedang ancang-ancang serangan (Telegraph).</summary>
        public void Tick(float speed, bool winding, float dt)
        {
            if (dead || model == null) return;
            dt = Mathf.Max(dt, 1e-4f);
            speedSm = Mathf.Lerp(speedSm, Mathf.Min(speed, 12f), 1f - Mathf.Exp(-10f * dt));
            if (winding && !wasWind) Trig("Attack");                    // mulai ancang-ancang: klip serang ikut berjalan, pukulan jatuh di ujungnya
            if (!winding && wasWind) lunge = 1f;                        // ancang-ancang selesai = saat pukulan mendarat
            wasWind = winding;
            SetF("Speed", speedSm * speedScale);
            SetB("Telegraph", winding);
            if (!life) return;

            time += dt; phase += speedSm * dt * 3.2f;
            wind = Mathf.MoveTowards(wind, winding ? 1f : 0f, dt * 4f);
            lunge = Mathf.MoveTowards(lunge, 0f, dt * 3f);
            hurt = Mathf.MoveTowards(hurt, 0f, dt * 5f);
            float mv = Mathf.Clamp01(speedSm / 3f);
            float bob = (Mathf.Abs(Mathf.Sin(phase)) * 0.045f * mv + Mathf.Sin(time * 1.8f) * 0.006f) * height;
            float roll = Mathf.Sin(phase) * 3.5f * mv;
            float pitch = -wind * 8f + lunge * 12f - hurt * 6f;         // + = condong ke depan; ancang-ancang menyandar ke belakang
            model.localPosition = basePos + new Vector3(0f, bob, lunge * 0.22f * height);
            model.localRotation = Quaternion.AngleAxis(pitch, Vector3.right) * Quaternion.AngleAxis(roll, Vector3.forward) * baseRot;
        }

        public void Hurt() { if (dead) return; hurt = 1f; Trig("Hurt"); }

        /// <summary>Mulai animasi mati bila controller punya Dead (bool) atau Die (trigger). true = Animator yang mengurus robohnya model.</summary>
        public bool Die()
        {
            if (dead) return false;
            dead = true;
            bool handled = false;
            if (Has("Dead", AnimatorControllerParameterType.Bool)) { anim.SetBool("Dead", true); handled = true; }
            if (Has("Die", AnimatorControllerParameterType.Trigger)) { anim.SetTrigger("Die"); handled = true; }
            if (Has("Die", AnimatorControllerParameterType.Bool)) { anim.SetBool("Die", true); handled = true; }
            if (!handled && model != null) { model.localPosition = basePos; model.localRotation = baseRot; model.localScale = baseScale; }
            return handled;
        }

        /// <summary>Warna model bergeser ke "to" sebesar amount (0 = asli). Tanpa renderer yang bisa diwarnai: denyut ukuran kecil sebagai gantinya.</summary>
        public void Flash(Color to, float amount)
        {
            if (amount < 0.001f) amount = 0f;
            if (Mathf.Abs(amount - lastAmt) < 0.01f && (amount == 0f || to == lastTo)) return;
            lastAmt = amount; lastTo = to;
            if (tint.Count == 0)
            {
                if (model != null && !dead) model.localScale = baseScale * (1f + 0.05f * amount);
                return;
            }
            if (mpb == null) mpb = new MaterialPropertyBlock();
            for (int i = 0; i < tint.Count; i++)
            {
                var e = tint[i]; if (e.r == null) continue;
                for (int k = 0; k < e.c0.Length; k++)
                {
                    if (amount <= 0f) { e.r.SetPropertyBlock(null, k); continue; }
                    Color col = Color.Lerp(e.c0[k], to, amount);
                    mpb.Clear(); mpb.SetColor(IdBase, col); mpb.SetColor(IdColor, col);
                    e.r.SetPropertyBlock(mpb, k);
                }
            }
        }
    }
}
