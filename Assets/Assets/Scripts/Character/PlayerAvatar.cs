using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Mengganti kapsul pemain dengan karakter 3D hasil kustomisasi (CharacterData) + armor/senjata dari GameState.equipment.
    /// Tubuh dibangun ulang otomatis saat kustomisasi atau equipment berubah. Animasi kombat ada di PlayerAnimator.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerAvatar : MonoBehaviour
    {
        public static PlayerAvatar I { get; private set; }
        public const float TargetHeight = 1.72f;

        public CharacterRig Rig { get; private set; }
        public PlayerAnimator Anim { get; private set; }
        public bool Drawn { get { return Time.time < combatUntil; } }   // diam = senjata disarungkan; klik kanan / menyerang = mode tempur (kembali santai 8 dtk setelah aksi terakhir)

        Transform model, pivot;
        GameObject rigGO, swordObj, bowObj, shieldObj, arrowObj;
        string sig = "";
        float poll, combatUntil;
        bool lastDrawn, guarding;
        CharacterController cc;
        Renderer capsule;
        float centerY, modelScale = 1f;
        bool hasSword, hasBow, hasShield, isStaff;
        ExternalModelDriver ext;

        void Awake() { I = this; cc = GetComponent<CharacterController>(); capsule = GetComponent<Renderer>(); }
        void OnDestroy() { if (I == this) I = null; }

        void Start()
        {
            if (GetComponent<PlayerRoyal>() == null) gameObject.AddComponent<PlayerRoyal>();   // v35z102n6
            if (capsule != null)
            {
                if (capsule.sharedMaterial != null) NpcKit.RuntimeShader = capsule.sharedMaterial.shader;
                capsule.enabled = false;
            }
            var fm = transform.Find("FacingMarker");
            if (fm != null) Destroy(fm.gameObject);
            Rebuild();
        }

        string Eq(string slot)
        {
            string v;
            return GameState.I != null && GameState.I.equipment.TryGetValue(slot, out v) ? v : "";
        }

        string Signature()
        {
            var c = CharacterData.Current;
            return JsonUtility.ToJson(c) + "|" + Eq("Head") + "|" + Eq("Body") + "|" + Eq("Legs") + "|" + Eq("Weapon") + "|" + Eq("Hands") + Eq("Ring") + Eq("Neck") + "|" + (GameState.I != null ? GameState.I.job : "") + "|" + (StarterConfig.Unarmed ? "U" : "") + (GameState.I != null && GameState.I.IsSpellblade ? "|SB" : "");
        }

        void Update()
        {
            poll -= Time.deltaTime;
            if (poll <= 0f)
            {
                poll = 0.3f;
                if (Signature() != sig) Rebuild();
            }
            if (Drawn != lastDrawn) { lastDrawn = Drawn; PlaceWeapons(); }
        }

        public void Rebuild()
        {
            sig = Signature();
            var data = CharacterData.Current;
            var spec = data.ToSpec();
            NpcKit.ApplyEquipment(spec, Eq("Head"), Eq("Body"), Eq("Legs"));

            if (model == null)
            {
                model = new GameObject("PlayerModel").transform;
                model.SetParent(transform, false);
                pivot = new GameObject("Pivot").transform;
                pivot.SetParent(model, false);
            }
            if (rigGO != null) Destroy(rigGO);
            ext = null;
            var extPrefab = ExternalModelDriver.LoadPrefab(CharacterData.Current.female, ArmorTier(Eq("Body")));
            if (extPrefab != null) { RebuildExternal(extPrefab); return; }
            var go = NpcKit.Spawn(pivot, "Rig", Vector3.zero, 0f, spec, false, false);
            rigGO = go;
            Rig = go.GetComponent<CharacterRig>();
            modelScale = 1f;
            centerY = Rig.height * 0.5f;
            go.transform.localPosition = new Vector3(0f, -centerY, 0f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            model.localScale = Vector3.one * modelScale;
            float feet = cc != null ? cc.center.y - cc.height * 0.5f : -1f;
            model.localPosition = new Vector3(0f, feet, 0f);
            model.localRotation = Quaternion.identity;
            pivot.localPosition = new Vector3(0f, centerY, 0f);
            pivot.localRotation = Quaternion.identity;

            Anim = go.AddComponent<PlayerAnimator>();
            Anim.Init(this, Rig, pivot, centerY);
            BuildWeapons();
            BuildAccessories();
            lastDrawn = Drawn;
            PlaceWeapons();
            ApplyViewMode();
        }

        // ---------------- aksesori: sarung tangan, cincin, kalung ----------------
        GameObject accRoot;

        static Color AccColor(string id)
        {
            if (id != null && id.StartsWith("ga_") && id.Length > 3)      // sarung tangan set area Eldrath
            {
                switch (id[3]) { case '0': return new Color(0.4f, 0.28f, 0.16f); case '1': return new Color(0.95f, 0.4f, 0.12f); case '2': return new Color(0.88f, 0.86f, 0.74f); default: return new Color(0.5f, 0.08f, 0.14f); }
            }
            switch (id)
            {
                case "leather_gloves": return new Color(0.42f, 0.28f, 0.16f);
                case "iron_gauntlet": return new Color(0.58f, 0.6f, 0.66f);
                case "ring_power": return new Color(0.85f, 0.25f, 0.2f);
                case "ring_guard": return new Color(0.25f, 0.5f, 0.9f);
                case "ring_hero": return new Color(0.95f, 0.8f, 0.3f);
                case "amulet_vigor": return new Color(0.3f, 0.75f, 0.4f);
                case "amulet_fang": return new Color(0.92f, 0.9f, 0.8f);
            }
            return new Color(0.8f, 0.7f, 0.3f);
        }

        // bentuk dasar berukuran meter dunia, dipasang ke tulang dengan kompensasi skala
        GameObject Acc(Transform parent, PrimitiveType shape, Vector3 worldPos, Vector3 worldSize, Color col)
        {
            if (parent == null) return null;
            var g = GameObject.CreatePrimitive(shape);
            var c = g.GetComponent<Collider>(); if (c != null) Destroy(c);
            g.transform.SetParent(parent, false);
            Vector3 ls = parent.lossyScale;
            ls = new Vector3(Mathf.Max(0.0001f, ls.x), Mathf.Max(0.0001f, ls.y), Mathf.Max(0.0001f, ls.z));
            g.transform.localPosition = new Vector3(worldPos.x / ls.x, worldPos.y / ls.y, worldPos.z / ls.z);
            g.transform.localScale = new Vector3(worldSize.x / ls.x, worldSize.y / ls.y, worldSize.z / ls.z);
            var r = g.GetComponent<Renderer>();
            var sh = NpcKit.RuntimeShader != null ? NpcKit.RuntimeShader : Shader.Find("Standard");
            if (sh != null) { var m = new Material(sh); m.color = col; r.sharedMaterial = m; }
            return g;
        }

        void BuildAccessories()
        {
            if (accessories != null) foreach (var o in accessories) if (o != null) Destroy(o);
            accessories = null;
            string gl = Eq("Hands"), rg = Eq("Ring"), nk = Eq("Neck");
            if (gl == "" && rg == "" && nk == "") return;
            Transform hR = ext != null ? ext.handR : (Rig != null ? Rig.handR : null);
            Transform hL = ext != null ? ext.handL : (Rig != null ? Rig.handL : null);
            Transform nb = ext != null ? ext.chest : (Rig != null ? Rig.head : null);
            accRoot = new GameObject("Accessories");
            accRoot.transform.SetParent(transform, false);
            var made = new System.Collections.Generic.List<GameObject>();
            if (gl != "")
            {
                Color c = AccColor(gl);
                foreach (var h in new[] { hR, hL })
                {
                    made.Add(Acc(h, PrimitiveType.Sphere, new Vector3(0f, -0.02f, 0f), new Vector3(0.115f, 0.14f, 0.115f), c));
                    made.Add(Acc(h, PrimitiveType.Cylinder, new Vector3(0f, 0.07f, 0f), new Vector3(0.1f, 0.05f, 0.1f), c * 0.8f));
                    if (gl.StartsWith("ga_"))      // duri / bara di punggung tangan
                    {
                        made.Add(Acc(h, PrimitiveType.Cube, new Vector3(0f, 0.02f, 0.05f), new Vector3(0.05f, 0.05f, 0.05f), gl[3] == '1' ? new Color(1f, 0.7f, 0.2f) : gl[3] == '3' ? new Color(1f, 0.2f, 0.2f) : Color.Lerp(c, Color.white, 0.4f)));
                        made.Add(Acc(h, PrimitiveType.Cylinder, new Vector3(0f, 0.12f, 0f), new Vector3(0.12f, 0.03f, 0.12f), gl[3] == '0' ? new Color(0.85f, 0.8f, 0.7f) : Color.Lerp(c, Color.black, 0.4f)));
                    }
                }
            }
            if (rg != "")
            {
                Color c = AccColor(rg);
                made.Add(Acc(hL, PrimitiveType.Cylinder, new Vector3(0f, 0.035f, 0f), new Vector3(0.082f, 0.012f, 0.082f), new Color(0.95f, 0.8f, 0.3f)));
                made.Add(Acc(hL, PrimitiveType.Sphere, new Vector3(0.035f, 0.035f, 0.035f), new Vector3(0.035f, 0.035f, 0.035f), c));
            }
            if (nk != "" && nb != null)
            {
                Color c = AccColor(nk);
                bool onHead = ext == null;
                float y = onHead ? 0.02f : 0.0f, fz = onHead ? 0.105f : 0.12f;
                for (int i = 0; i < 12; i++)
                {
                    float a = i / 12f * Mathf.PI * 2f;
                    made.Add(Acc(nb, PrimitiveType.Sphere, new Vector3(Mathf.Sin(a) * 0.105f, y, Mathf.Cos(a) * 0.105f), Vector3.one * 0.022f, new Color(0.75f, 0.6f, 0.25f)));
                }
                made.Add(Acc(nb, PrimitiveType.Sphere, new Vector3(0f, y - 0.035f, fz), new Vector3(0.05f, 0.06f, 0.03f), c));
            }
            foreach (var g in made) if (g != null) g.transform.SetParent(g.transform.parent, true);
            Destroy(accRoot);   // wadah kosong; aksesori melekat langsung pada tulang
            accRoot = null;
            accessories = made;
        }

        System.Collections.Generic.List<GameObject> accessories;

        // ---------------- first person ----------------
        /// <summary>Posisi mata (dunia) untuk kamera first person.</summary>
        public Vector3 EyePosition()
        {
            if (Rig != null && Rig.head != null) return Rig.head.TransformPoint(new Vector3(0f, 0.23f, 0.17f));
            if (ext != null && ext.anim != null)
            {
                var h = ext.anim.GetBoneTransform(HumanBodyBones.Head);
                if (h != null) return h.position + transform.forward * 0.08f + Vector3.up * 0.08f;
            }
            return transform.position + Vector3.up * 1.6f;
        }

        /// <summary>Sembunyikan kepala (dan rambut/topi) di first person; tetap membuat bayangan.</summary>
        public void ApplyViewMode()
        {
            bool fp = ThirdPersonCamera.FirstPerson;
            var rs = new System.Collections.Generic.List<Renderer>();
            if (Rig != null && Rig.head != null) rs.AddRange(Rig.head.GetComponentsInChildren<Renderer>(true));
            else if (ext != null && ext.anim != null)
            {
                var h = ext.anim.GetBoneTransform(HumanBodyBones.Head);
                if (h != null) rs.AddRange(h.GetComponentsInChildren<Renderer>(true));
            }
            if (ext != null) ext.hideHead = fp;
            foreach (var r in rs) r.shadowCastingMode = fp ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : UnityEngine.Rendering.ShadowCastingMode.On;
        }

        // ---------------- model asli (opsional) ----------------
        static int ArmorTier(string bodyId)
        {
            bodyId = GearTiers.VisualBase(bodyId);
            switch (bodyId) { case "leather_armor": return 1; case "wolfhide_armor": return 2; case "iron_armor": return 3; case "knight_armor": return 4; }
            return 0;
        }

        void RebuildExternal(GameObject prefab)
        {
            Rig = null; Anim = null; ext = null;
            float feet = cc != null ? cc.center.y - cc.height * 0.5f : -1f;
            model.localScale = Vector3.one; model.localRotation = Quaternion.identity;
            model.localPosition = new Vector3(0f, feet, 0f);
            pivot.localPosition = Vector3.zero; pivot.localRotation = Quaternion.identity;
            ext = ExternalModelDriver.Create(prefab, pivot, this);
            rigGO = ext.gameObject;
            var cfg = HGModelConfig.Load();
            ext.Fit(cfg != null ? cfg.playerHeight : TargetHeight);
            swordObj = bowObj = shieldObj = arrowObj = null; hasSword = hasBow = hasShield = isStaff = false;
            string w = Eq("Weapon"); string kind = StarterConfig.Unarmed ? "" : NpcKit.WeaponKind(w);
            bool sb = GameState.I != null && GameState.I.IsSpellblade && kind == "staff";
            if (sb) { isStaff = false; swordObj = MakeExtProp(NpcKit.Weapon("knight_sword"), "Sword"); hasSword = true; AttachBladeGlow(swordObj); }
            else if (kind == "sword" || kind == "staff") { isStaff = kind == "staff"; swordObj = MakeExtProp(NpcKit.Weapon(w), "Sword"); hasSword = true; }
            if (kind == "bow") { bowObj = MakeExtProp(NpcKit.Weapon(w), "Bow"); hasBow = true; }
            if (GameState.I != null && GameState.I.job == "Swordsman" && !StarterConfig.Unarmed) { shieldObj = MakeExtProp(NpcKit.Weapon("@shield"), "Shield"); hasShield = true; }
            BuildAccessories();
            lastDrawn = Drawn;
            PlaceWeapons();
            ApplyViewMode();
        }

        GameObject MakeExtProp(NpcKit.WeaponModel m, string name)
        {
            var cfg = HGModelConfig.Load();
            GameObject custom = cfg == null ? null : (name == "Sword" ? cfg.swordProp : (name == "Bow" ? cfg.bowProp : cfg.shieldProp));
            if (custom != null)
            {
                var inst = Instantiate(custom, ext.transform, false); inst.name = name;
                foreach (var c in inst.GetComponentsInChildren<Collider>()) Destroy(c);
                return inst;
            }
            var g = new GameObject(name);
            g.AddComponent<MeshFilter>().sharedMesh = m.mesh;
            g.AddComponent<MeshRenderer>().sharedMaterials = m.mats;
            g.transform.SetParent(ext.transform, false);
            return g;
        }

        void Bind(GameObject g, Transform bone, Vector3 pos, Vector3 euler)
        {
            if (g == null) return;
            if (bone == null) bone = ext.transform;
            var t = g.transform;
            t.SetParent(bone, false);
            t.localPosition = pos; t.localRotation = Quaternion.Euler(euler);
            float ws = bone.lossyScale.x;
            var cfg = HGModelConfig.Load();
            bool custom = cfg != null && ((g.name == "Sword" && cfg.swordProp != null) || (g.name == "Bow" && cfg.bowProp != null) || (g.name == "Shield" && cfg.shieldProp != null));
            t.localScale = Vector3.one * ((custom ? ext.fitScale : 1.1f) / Mathf.Max(0.0001f, ws));
        }

        // Offset ini perkiraan umum untuk rig humanoid; sesuaikan bila perlu.
        void PlaceExternal()
        {
            bool d = Drawn;
            var cf = HGModelConfig.Load();
            Vector3 sP = cf != null ? cf.swordPos : new Vector3(0f, 0.02f, 0f) / 1.1f, sE = cf != null ? cf.swordEuler : new Vector3(0f, 0f, -90f);
            Vector3 bP = cf != null ? cf.bowPos : Vector3.zero, bE = cf != null ? cf.bowEuler : new Vector3(0f, 0f, 90f);
            Vector3 hP = cf != null ? cf.shieldPos : new Vector3(0f, 0.04f, 0f), hE = cf != null ? cf.shieldEuler : new Vector3(0f, 90f, 90f);
            if (swordObj != null) { if (d) Bind(swordObj, ext.handR, sP, sE); else if (isStaff) Bind(swordObj, ext.chest, new Vector3(0.1f, -0.3f, -0.2f) / 1.1f, new Vector3(-8f, 0f, 10f)); else Bind(swordObj, ext.hip, new Vector3(-0.2f, 0f, 0f) / 1.1f, new Vector3(0f, 0f, 120f)); }
            if (bowObj != null) { if (d) Bind(bowObj, ext.handL, bP, bE); else Bind(bowObj, ext.chest, new Vector3(0f, 0f, -0.15f) / 1.1f, new Vector3(0f, 0f, 20f)); }
            if (shieldObj != null) { if (d) Bind(shieldObj, ext.handL, hP, hE); else Bind(shieldObj, ext.chest, new Vector3(0f, 0f, -0.2f) / 1.1f, new Vector3(-90f, 0f, 0f)); }
        }

        // pedang sihir (evolusi penyihir bersenjata pedang): bilah berpendar sesuai elemen
        void AttachBladeGlow(GameObject blade)
        {
            if (blade == null || GameState.I == null) return;
            string el = JobInfo.Elem(GameState.I.job);
            Color c = MagicFx.ElemCol(el);
            var g = blade.AddComponent<BladeGlow>(); g.color = c;
        }

        // ---------------- senjata ----------------
        void BuildWeapons()
        {
            hasSword = hasBow = hasShield = isStaff = false;
            string w = Eq("Weapon");
            string kind = StarterConfig.Unarmed ? "" : NpcKit.WeaponKind(w);
            bool sb = GameState.I != null && GameState.I.IsSpellblade && kind == "staff";
            if (sb) { isStaff = false; swordObj = MakeProp(NpcKit.Weapon("knight_sword"), "Sword"); hasSword = true; AttachBladeGlow(swordObj); }
            else if (kind == "sword" || kind == "staff") { isStaff = kind == "staff"; swordObj = MakeProp(NpcKit.Weapon(w), "Sword"); hasSword = true; }
            if (kind == "bow") { bowObj = MakeProp(NpcKit.Weapon(w), "Bow"); hasBow = true; arrowObj = MakeProp(NpcKit.Weapon("@arrow"), "Arrow"); arrowObj.SetActive(false); }
            if (GameState.I != null && GameState.I.job == "Swordsman" && !StarterConfig.Unarmed) { shieldObj = MakeProp(NpcKit.Weapon("@shield"), "Shield"); hasShield = true; }
        }

        GameObject MakeProp(NpcKit.WeaponModel m, string name)
        {
            var g = new GameObject(name);
            g.transform.SetParent(Rig.transform, false);
            g.AddComponent<MeshFilter>().sharedMesh = m.mesh;
            g.AddComponent<MeshRenderer>().sharedMaterials = m.mats;
            return g;
        }

        // orientasi senjata di tangan (diuji lewat render pose; blade ke atas saat siku ditekuk 90 derajat)
        public static readonly Quaternion SwordGrip = Quaternion.Euler(90f, 0f, 0f) * Quaternion.Euler(0f, 90f, 0f);
        public static readonly Quaternion BowGrip = Quaternion.Euler(90f, 0f, 0f);

        void PlaceWeapons()
        {
            if (ext != null) { PlaceExternal(); return; }
            if (Rig == null) return;
            bool d = Drawn;
            if (hasSword && swordObj != null)
            {
                var t = swordObj.transform;
                if (d) { t.SetParent(Rig.handR, false); t.localPosition = Vector3.zero; t.localRotation = SwordGrip; }
                else if (isStaff) { t.SetParent(Rig.body, false); t.localPosition = new Vector3(0.1f, -0.3f, -0.22f); t.localRotation = Quaternion.Euler(-8f, 0f, 10f); }
                else { t.SetParent(Rig.body, false); t.localPosition = new Vector3(-0.21f, -0.02f, 0.02f); t.localRotation = Quaternion.Euler(200f, 0f, 12f); }
            }
            if (hasBow && bowObj != null)
            {
                var t = bowObj.transform;
                if (d) { t.SetParent(Rig.handL, false); t.localPosition = Vector3.zero; t.localRotation = BowGrip; }
                else { t.SetParent(Rig.body, false); t.localPosition = new Vector3(0f, 0.34f, -0.2f); t.localRotation = Quaternion.Euler(0f, 0f, 20f); }
            }
            if (hasShield && shieldObj != null)
            {
                var t = shieldObj.transform;
                if (d) { t.SetParent(Rig.foreL, false); t.localPosition = new Vector3(-0.055f, -0.13f, 0.02f); t.localRotation = Quaternion.Euler(0f, 0f, 90f); }
                else { t.SetParent(Rig.body, false); t.localPosition = new Vector3(0f, 0.3f, -0.23f); t.localRotation = Quaternion.Euler(-90f, 0f, 0f); }
            }
        }

        /// <summary>Posisi ujung tongkat (dunia) untuk titik keluar sihir; cadangan: depan dada.</summary>
        public Vector3 StaffTip
        {
            get
            {
                if (swordObj != null && isStaff && swordObj.activeInHierarchy)
                {
                    var w = NpcKit.Weapon(Eq("Weapon"));
                    return swordObj.transform.TransformPoint(new Vector3(0f, w != null ? w.length : 1.2f, 0f));
                }
                return transform.position + Vector3.up * 1.5f + transform.forward * 0.8f;
            }
        }

        public bool HasBow { get { return hasBow; } }
        public bool HasSword { get { return hasSword; } }
        public bool HasShield { get { return hasShield; } }
        public GameObject Arrow { get { return arrowObj; } }
        public GameObject BowObject { get { return bowObj; } }

        // ---------------- API untuk PlayerCombat ----------------
        public void Draw() { Combat(); }
        void Combat() { combatUntil = Time.time + 8f; }
        public void Slash(int combo) { Combat(); if (ext != null) ext.Trigger("Slash" + Mathf.Clamp(combo, 0, 2)); if (Anim != null) Anim.Play(PlayerAnimator.Clip.Slash0 + Mathf.Clamp(combo, 0, 2)); }
        public void Shoot() { Combat(); if (ext != null) ext.Trigger("Shoot"); if (Anim != null) Anim.Play(PlayerAnimator.Clip.Shoot); }
        public void Roll(bool back) { Combat(); if (ext != null) ext.Trigger(back ? "RollBack" : "Roll"); if (Anim != null) Anim.Play(back ? PlayerAnimator.Clip.RollBack : PlayerAnimator.Clip.Roll); }
        public void Hurt() { if (ext != null) ext.Trigger("Hurt"); if (Anim != null) Anim.Play(PlayerAnimator.Clip.Hurt); }
        public void Faint() { if (ext != null) { ext.Trigger("Faint"); ext.SetDead(true); } if (Anim != null) Anim.Play(PlayerAnimator.Clip.Faint); }
        public void Revive() { if (ext != null) ext.SetDead(false); if (Anim != null) Anim.Stop(); }
        public void Drink() { if (ext != null) ext.Trigger("Drink"); if (Anim != null) Anim.Play(PlayerAnimator.Clip.Drink); }
        /// <summary>Aksi skill kustom (timeline pose). Model eksternal memakai trigger tebasan sebagai cadangan.</summary>
        public void KickYaw(float deg) { if (Anim != null) Anim.KickYaw(deg); }
        public void PlayCustom(float dur, float[] times, PlayerAnimator.Pose[] poses)
        {
            Combat();
            if (ext != null) { ext.Trigger("Slash2"); return; }
            if (Anim != null) Anim.PlayCustom(dur, times, poses);
        }

        /// <summary>Ujung senjata (dunia) untuk efek; cadangan: depan badan.</summary>
        public Vector3 WeaponTip
        {
            get
            {
                if (swordObj != null && swordObj.activeInHierarchy)
                {
                    var w = NpcKit.Weapon(Eq("Weapon"));
                    return swordObj.transform.TransformPoint(new Vector3(0f, w != null ? w.length * 0.9f : 1.0f, 0f));
                }
                return transform.position + Vector3.up * 1.2f + transform.forward * 0.6f;
            }
        }

        public void Cast()
        {
            Combat();
            if (ext != null) ext.Trigger("Slash2");
            bool mage = GameState.I != null && GameState.I.IsMage;
            if (Anim != null) Anim.Play(mage ? PlayerAnimator.Clip.Spell : PlayerAnimator.Clip.Slash2);
        }
        public void SetGuard(bool g) { guarding = g; if (Anim != null) Anim.Guarding = g; if (g) Combat(); }
    }
}
