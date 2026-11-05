using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Jalur model asli: bila ada prefab di Resources/HG_Models/Player (mis. hasil VRoid/VRM, Mixamo, asset pack humanoid),
    /// PlayerAvatar memakainya sebagai ganti model prosedural. Syarat prefab: punya Animator dengan Avatar Humanoid.
    /// Animator Controller (Resources/HG_Models/PlayerController, opsional) memakai parameter:
    ///   float Speed, bool Drawn, bool Bow, trigger Slash0 Slash1 Slash2 Shoot Roll RollBack Hurt Faint Drink, bool Dead.
    /// Parameter yang tidak ada akan dilewati tanpa error.
    /// </summary>
    public class ExternalModelDriver : MonoBehaviour
    {
        public const string PrefabPath = "HG_Models/Player";
        public const string ControllerPath = "HG_Models/PlayerController";

        public Animator anim;
        public Transform handR, handL, chest, hip;
        readonly HashSet<string> pars = new HashSet<string>();
        PlayerAvatar avatar;
        Vector3 last; float speedSm;
        public GameObject sword, bow, shield, arrow;

        public float fitScale = 1f;
        Transform headBone;
        public bool hideHead;

        /// <summary>Prefab pemain dari konfigurasi wizard (atau fallback Resources/HG_Models/Player).</summary>
        public static GameObject LoadPrefab(bool female = false, int armorTier = 0)
        {
            var cfg = HGModelConfig.Load();
            if (cfg != null && cfg.HasPlayer) return cfg.PickPlayer(female, armorTier);
            return Resources.Load<GameObject>(PrefabPath);
        }

        /// <summary>Skala model agar tinggi total = h dan telapak kaki di titik akar; pusat horizontal di akar.</summary>
        public static float FitTo(Transform model, float h)
        {
            var rs = model.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return 1f;
            Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            float k = h / Mathf.Max(0.05f, b.size.y);
            model.localScale = model.localScale * k;
            b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            Vector3 root = model.parent != null ? model.parent.position : Vector3.zero;
            model.position += new Vector3(root.x - b.center.x, root.y - b.min.y, root.z - b.center.z);
            return k;
        }

        public static ExternalModelDriver Create(GameObject prefab, Transform parent, PlayerAvatar av)
        {
            var go = Instantiate(prefab, parent, false);
            go.name = "ExternalRig";
            var d = go.AddComponent<ExternalModelDriver>();
            d.avatar = av;
            d.anim = go.GetComponentInChildren<Animator>();
            if (d.anim != null)
            {
                var cfg = HGModelConfig.Load();
                RuntimeAnimatorController ctrl = cfg != null && cfg.playerController != null ? cfg.playerController : Resources.Load<RuntimeAnimatorController>(ControllerPath);
                if (ctrl != null) d.anim.runtimeAnimatorController = ctrl;
                d.anim.applyRootMotion = false;
                foreach (var p in d.anim.parameters) d.pars.Add(p.name);
                d.handR = d.anim.GetBoneTransform(HumanBodyBones.RightHand);
                d.handL = d.anim.GetBoneTransform(HumanBodyBones.LeftHand);
                d.chest = d.anim.GetBoneTransform(HumanBodyBones.UpperChest) ?? d.anim.GetBoneTransform(HumanBodyBones.Chest);
                d.hip = d.anim.GetBoneTransform(HumanBodyBones.Hips);
                d.headBone = d.anim.GetBoneTransform(HumanBodyBones.Head);
            }
            foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            d.last = av.transform.position;
            return d;
        }

        public void Fit(float targetHeight) { fitScale = FitTo(transform, targetHeight); }

        void LateUpdate()
        {
            if (headBone != null) headBone.localScale = hideHead ? Vector3.one * 0.0001f : Vector3.one;      // first person: sembunyikan kepala
        }

        void Update()
        {
            if (anim == null) return;
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            Vector3 d = avatar.transform.position - last; d.y = 0f; last = avatar.transform.position;
            speedSm = Mathf.Lerp(speedSm, Mathf.Min(d.magnitude / dt, 10f), 10f * dt);
            Set("Speed", speedSm);
            Set("Drawn", avatar.Drawn);
            Set("Bow", avatar.HasBow);
        }

        void Set(string n, float v) { if (pars.Contains(n)) anim.SetFloat(n, v); }
        void Set(string n, bool v) { if (pars.Contains(n)) anim.SetBool(n, v); }
        public void Trigger(string n) { if (anim != null && pars.Contains(n)) anim.SetTrigger(n); }
        public void SetDead(bool dead) { Set("Dead", dead); }
    }
}
