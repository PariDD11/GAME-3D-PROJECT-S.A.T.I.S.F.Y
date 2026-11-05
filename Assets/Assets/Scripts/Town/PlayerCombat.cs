using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Combat dasar pemain (placeholder sesuai GDD 3): HP, stamina, serangan, dodge dengan i-frame, guard (Swordsman),
    /// tembak (Archer), pingsan 3x per quest.
    /// Kontrol: klik kiri / J serang, Ctrl dodge, Spasi lompat, klik kanan tahan guard (Swordsman).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerCombat : MonoBehaviour
    {
        public static PlayerCombat I { get; private set; }

        public float hp, stamina, mana;
        float lastManaUse = -99f, bashToast;
        public float lastHitTime = -99f;
        public Vector3 respawnPos;

        public static System.Action OnFainted;      // dipanggil saat pemain pingsan (Menara memakai ini)
        public static System.Action OnRevived;      // dipanggil setelah respawn

        public bool IsDead { get; private set; }
        public bool Guarding { get; private set; }
        public float MaxHp { get { return (100f + 3f * (GameState.I.level - 1) + StatUpgrade.HpBonus(GameState.I)) * (1f + GameState.I.Pas("hp")); } }
        public float MaxStamina { get { return 100f + 0.5f * GameState.I.level + StatUpgrade.StaBonus(GameState.I); } }
        public float MaxMana { get { return GameState.I.MaxMana; } }
        public bool Invulnerable { get { return Time.time < invulUntil; } }

        CharacterController cc;
        Transform cam;
        float dodgeInvulUntil;
        float lastAction, invulUntil, attackReady, comboReset, dashUntil, dodgeReady;
        int combo;
        Vector3 dashDir;
        float dashSpeed = 14f, buffUntil, buffMult = 1f, parryUntil, riposteUntil, counterUntil, bulwarkUntil;

        void Awake() { I = this; cc = GetComponent<CharacterController>(); }
        void OnDestroy() { if (I == this) I = null; }

        void Start()
        {
            if (Camera.main != null) cam = Camera.main.transform;
            hp = MaxHp;
            stamina = MaxStamina;
            mana = MaxMana;
            var rp = GameObject.Find("RespawnPoint");
            respawnPos = rp != null ? rp.transform.position + Vector3.up * 1.1f : transform.position;
        }

        float sparkAt, senseAt, senseToast;
        readonly HashSet<int> sensed = new HashSet<int>();

        /// <summary>Passive penyihir yang berjalan terus-menerus: Seed of Earth, Spark Trail, Tremor Sense.</summary>
        void MagePassives(GameState gs)
        {
            float rg = gs.Pas("regen");
            if (rg > 0f && !IsDead && hp < MaxHp) hp = Mathf.Min(MaxHp, hp + MaxHp * rg * Time.deltaTime);
            if (!gs.IsMage || IsDead) return;
            if (gs.Knows("em_flw_3") && Time.time - lastHitTime > 4f && hp < MaxHp) hp = Mathf.Min(MaxHp, hp + MaxHp * 0.01f * Time.deltaTime);
            if (gs.Knows("fm_stp_3") && Time.time >= sparkAt && cc.velocity.magnitude > 2f)
            {
                sparkAt = Time.time + 0.3f;
                Vector3 feet = transform.position - Vector3.up * 0.8f;
                CombatFx.Burst(feet, CombatFx.FireCol, 4, 0.4f, 0.8f, 0.5f, 0.12f);
                float d = SkillExec.MageBase(gs) * 0.08f;
                foreach (var m in new List<Monster>(Monster.All))
                    if (m != null && !m.IsDead && Vector3.Distance(m.transform.position, transform.position) < 2.3f + m.Radius) SkillExec.Burn(m, d, 2f);
            }
            if (gs.Knows("em_flw_2") && Time.time >= senseAt)
            {
                senseAt = Time.time + 1f;
                int fresh = 0;
                foreach (var m in new List<Monster>(Monster.All))
                {
                    if (m == null || m.IsDead) continue;
                    float dist = Vector3.Distance(m.transform.position, transform.position);
                    int id = m.GetInstanceID();
                    if (dist < 28f) { if (sensed.Add(id)) fresh++; }
                    else if (dist > 36f) sensed.Remove(id);
                }
                if (fresh > 0 && Time.time > senseToast)
                {
                    senseToast = Time.time + 6f;
                    CombatFx.ShockRing(transform.position - Vector3.up * 0.9f, 5f, CombatFx.EarthCol, 0.5f);
                    if (TownUI.I != null) TownUI.I.Toast("Getaran tanah: " + fresh + " monster dalam 28 m.");
                }
            }
        }

        void Update()
        {
            var gs = GameState.I;
            if (gs == null) return;
            MagePassives(gs);

            if (!IsDead)
            {
                if (Time.time > lastAction + 0.8f && !Guarding) stamina = Mathf.Min(MaxStamina, stamina + 20f * (1f + gs.AttachEffect("stamina") / 100f) * Time.deltaTime);
                if (gs.IsMage)
                {   // mana: pulih pelan saat bertarung, cepat saat tenang
                    float rate = (Time.time < lastManaUse + 2.5f ? 2.5f : 7f) * gs.ManaRegenMult;
                    mana = Mathf.Min(MaxMana, mana + rate * Time.deltaTime);
                }
                if (Time.time < dashUntil && cc.enabled) cc.Move(dashDir * dashSpeed * Time.deltaTime);
            }

            bool menuOpen = (TownUI.I != null && TownUI.I.PanelOpen) || MiniGame.Active || Cutscene.Active;
            if (IsDead || menuOpen) { Guarding = false; charging = false; return; }

            if (!StarterConfig.Unarmed && TownInput.GuardHeld() && PlayerAvatar.I != null) PlayerAvatar.I.Draw();   // klik kanan: langsung mode tempur
            Guarding = gs.job == "Swordsman" && !StarterConfig.Unarmed && TownInput.GuardHeld() && stamina > 1f;
            if (PlayerAvatar.I != null) PlayerAvatar.I.SetGuard(Guarding);
            if (TownInput.DodgeDown() && stamina >= 20f && Time.time >= dodgeReady && !PlayerController.Swimming) Dodge();
            if ((gs.job == "Archer" || gs.IsMage) && !StarterConfig.Unarmed && ThirdPersonCamera.Aiming) FaceCamera();
            if (gs.job == "Archer" && !StarterConfig.Unarmed) HandleBow(gs);
            else if (gs.IsMage && !StarterConfig.Unarmed)
            {
                charging = false;
                if (TownInput.AttackDown() && Time.time >= attackReady)
                {
                    // penyihir pedang: klik kiri = tebasan pedang bersihir; tahan klik kanan (bidik) + klik kiri = tembakan sihir
                    if (gs.IsSpellblade && !ThirdPersonCamera.Aiming) SpellSlash(gs);
                    else CastBolt(gs);
                }
                if (gs.IsSpellblade && !sbHint && Time.time > 3f) { sbHint = true; if (TownUI.I != null) TownUI.I.Toast("Pedang sihir: klik kiri = tebas, tahan klik kanan + klik kiri = sihir."); }
            }
            else { charging = false; if (TownInput.AttackDown() && Time.time >= attackReady) Attack(gs); }
        }

        // ---- dipakai SkillExec ----
        public Transform Cam { get { return cam; } }
        public void FaceAim() { FaceCamera(); }
        public void SpendMana(float v) { mana = Mathf.Max(0f, mana - v); lastManaUse = Time.time; lastAction = Time.time; }
        public void RestoreMana(float v) { mana = Mathf.Min(MaxMana, mana + v); }
        public void SpendStamina(float v) { stamina = Mathf.Max(0f, stamina - v); lastAction = Time.time; }
        /// <summary>Damage berkelanjutan (racun/bakar): tanpa animasi sakit, tidak pernah membunuh.</summary>
        public void DotDamage(float dmg, Color col)
        {
            if (IsDead || DevTools.God) return;
            dmg = Mathf.Min(dmg, hp - 1f); if (dmg < 0.5f) return;
            hp -= dmg; lastHitTime = Time.time; Challenges.OnPlayerDamaged();
            CombatFx.DamageNumber(transform.position + Vector3.up * 2.1f, dmg, false, col);
        }

        public void Invuln(float s) { invulUntil = Mathf.Max(invulUntil, Time.time + s); }
        public void DashTo(Vector3 dir, float dur, float speed) { dashDir = dir.normalized; dashSpeed = speed; dashUntil = Time.time + dur; }
        public void Buff(float mult, float dur) { buffMult = mult; buffUntil = Time.time + dur; }
        public void ParryWindow(float dur) { parryUntil = Time.time + dur; }
        public void BulwarkFor(float dur) { bulwarkUntil = Time.time + dur; }
        public bool Bulwarked { get { return Time.time < bulwarkUntil; } }
        public bool RiposteActive { get { return Time.time < riposteUntil; } }
        public void CounterWindow(float dur) { counterUntil = Time.time + dur; }
        public void RiposteWindow(float dur) { riposteUntil = Time.time + dur; }
        public void LockAttack(float s) { attackReady = Mathf.Max(attackReady, Time.time + s); }

        void FaceCamera()
        {
            if (cam == null) return;
            Vector3 f = cam.forward;
            f.y = 0f;
            if (f.sqrMagnitude > 0.01f)
            {
                var nr = Quaternion.LookRotation(f.normalized);
                float dy = Mathf.DeltaAngle(transform.eulerAngles.y, nr.eulerAngles.y);
                transform.rotation = nr;                                  // arah serangan tetap langsung (akurat), tampilannya diputar halus
                if (PlayerAvatar.I != null && Mathf.Abs(dy) > 1f) PlayerAvatar.I.KickYaw(-dy);
            }
        }

        void Attack(GameState gs)
        {
            FaceCamera();
            if (StarterConfig.Unarmed) { Punch(gs); return; }
            if (gs.job == "Archer") { Shoot(gs, 0f); return; }
            if (Time.time < counterUntil)     // jendela counter setelah Perfect Parry: serangan langsung jadi Counter
            {
                counterUntil = 0f; lastAction = Time.time; attackReady = Time.time + 0.55f;
                SkillExec.Counter(this);
                return;
            }
            if (stamina < 8f) return;

            stamina -= 8f;
            lastAction = Time.time;
            attackReady = Time.time + 0.45f;
            if (Time.time > comboReset) combo = 0;
            if (PlayerAvatar.I != null) PlayerAvatar.I.Slash(combo);
            if (SkillAction.FortressActive) SkillAction.FortressSlash(this);
            if (SkillAction.BloodbathActive) SkillAction.BloodSlash(this);
            if (SkillAction.AscendedActive) SkillAction.AscendedSlash(this);
            Audio.Sfx(combo == 0 ? "swing" : combo == 1 ? "swing2" : "heavy_swing", transform.position);
            float dmg = (10f + gs.TotalAtk) * (1f + 0.1f * combo) * (combo == 2 ? 1.4f : 1f);
            combo = (combo + 1) % 3;
            comboReset = Time.time + 1.1f;

            {
                // efek tebasan Swordsman: combo 1 miring kanan, combo 2 miring kiri, combo 3 tebasan penutup dari atas (emas + cincin kejut)
                int cIdx = (combo + 2) % 3;
                bool fin = cIdx == 2;
                Vector3 c0 = transform.position + Vector3.up * 1.0f;
                CombatFx.SlashArc(c0, transform.rotation, fin ? 3.3f : 2.7f, cIdx == 0 ? 22f : cIdx == 1 ? -22f : 90f, fin ? CombatFx.SwordGold : CombatFx.SwordCol, fin ? 0.28f : 0.19f, fin ? 1.1f : 0.95f);
                if (fin)
                {
                    CombatFx.ShockRing(transform.position + transform.forward * 1.8f - Vector3.up * 0.9f, 2.6f, CombatFx.SwordGold, 0.32f);
                    CombatFx.Dust(transform.position + transform.forward * 2.2f - Vector3.up * 0.9f, 8);
                    CombatFx.Flash(c0 + transform.forward * 2f, CombatFx.SwordGold, 7f, 1.6f, 0.16f);
                }
            }
            foreach (var m in new List<Monster>(Monster.All))
            {
                if (m.IsDead) continue;
                Vector3 d = m.HitPoint - transform.position;
                d.y = 0f;
                float dist = d.magnitude;
                if (dist > 3.4f + m.Radius) continue;
                if (dist > m.Radius + 0.5f && Vector3.Angle(transform.forward, d) > 70f) continue;
                m.TakeDamage(dmg);
            }
        }

        /// <summary>Level 0-4: tanpa senjata. Tinju kombo 3 pukulan (pukulan ketiga lebih kuat, dengan cincin kejut kecil).</summary>
        void Punch(GameState gs)
        {
            if (stamina < 5f) return;
            stamina -= 5f;
            lastAction = Time.time;
            attackReady = Time.time + 0.38f;
            if (Time.time > comboReset) combo = 0;
            int ci = combo;
            if (PlayerAvatar.I != null) PlayerAvatar.I.Slash(ci);
            Audio.Sfx("swing", transform.position);
            float dmg = (7f + gs.level * 1.5f) * (1f + 0.12f * ci) * (ci == 2 ? 1.5f : 1f);
            combo = (combo + 1) % 3;
            comboReset = Time.time + 1.0f;
            bool hit = false;
            foreach (var m in new List<Monster>(Monster.All))
            {
                if (m.IsDead) continue;
                Vector3 d = m.HitPoint - transform.position;
                d.y = 0f;
                float dist = d.magnitude;
                if (dist > 2.3f + m.Radius) continue;
                if (dist > m.Radius + 0.4f && Vector3.Angle(transform.forward, d) > 65f) continue;
                m.TakeDamage(dmg);
                CombatFx.HitSpark(m.HitPoint, Color.white, ci == 2 ? 1.2f : 0.7f, ci == 2 ? 12 : 6);
                hit = true;
            }
            if (ci == 2 && hit) CombatFx.ShockRing(transform.position + transform.forward * 1.4f - Vector3.up * 0.9f, 1.8f, Color.white, 0.2f);
        }

        // ---- Archer: tahan klik kiri / J untuk membidik; lepas untuk menembak (makin lama makin sakit) ----
        bool charging; float chargeStart;
        public const float MaxCharge = 1.4f;
        public float Charge01 { get { return charging ? Mathf.Clamp01((Time.time - chargeStart) / MaxCharge) : 0f; } }
        public bool Charging { get { return charging; } }
        float chargeFxAt;

        void HandleBow(GameState gs)
        {
            if (!charging)
            {
                if (TownInput.AttackDown() && Time.time >= attackReady && stamina >= 6f && cam != null)
                {
                    charging = true; chargeStart = Time.time; chargeFxAt = 0f;
                    Audio.Sfx("swing", transform.position);
                }
                return;
            }
            FaceCamera();
            float c = Charge01;
            lastAction = Time.time;
            if (TownInput.DodgeDown() && stamina >= 20f && Time.time >= dodgeReady && !PlayerController.Swimming) { charging = false; Dodge(); return; }
            if (Time.time >= chargeFxAt)
            {
                chargeFxAt = Time.time + 0.12f;
                Vector3 bow = transform.position + Vector3.up * 1.3f + transform.forward * 0.8f;
                CombatFx.Burst(bow + Random.insideUnitSphere * (1.2f * (1f - c) + 0.2f), c >= 1f ? CombatFx.SwordGold : CombatFx.BowCol, 1, 0.2f, 0.2f, 0.25f, 0.07f + 0.06f * c);
                if (c >= 1f && Time.time - chargeStart < MaxCharge + 0.2f) { CombatFx.ShockRing(transform.position - Vector3.up * 0.9f, 2.2f, CombatFx.SwordGold, 0.25f); Audio.Sfx("block", transform.position); }
            }
            if (!TownInput.AttackHeld())
            {
                charging = false;
                Shoot(gs, c);
            }
        }

        void OnGUI()
        {
            if (!charging) return;
            float c = Charge01;
            float w = 220f, h = 14f, x = (Screen.width - w) / 2f, y = Screen.height * 0.62f;
            GUI.color = new Color(0f, 0f, 0f, 0.7f); GUI.DrawTexture(new Rect(x - 3, y - 3, w + 6, h + 6), Texture2D.whiteTexture);
            GUI.color = c >= 1f ? new Color(1f, 0.85f, 0.25f) : Color.Lerp(new Color(0.4f, 0.8f, 1f), new Color(1f, 0.6f, 0.2f), c);
            GUI.DrawTexture(new Rect(x, y, w * c, h), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var st = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 15 };
            GUI.Label(new Rect(x, y + 16, w, 22), c >= 1f ? "PENUH! lepas untuk menembak" : "Membidik... " + Mathf.RoundToInt(c * 100f) + "%", st);
        }

        void Shoot(GameState gs, float charge)
        {
            if (stamina < 6f || cam == null) return;
            stamina -= 6f + 8f * charge;
            lastAction = Time.time;
            attackReady = Time.time + 0.6f;
            if (PlayerAvatar.I != null) PlayerAvatar.I.Shoot();
            Audio.Sfx("bow_shot", transform.position);

            Vector3 origin = transform.position + Vector3.up * 1.4f;
            Vector3 end = cam.position + cam.forward * 80f;
            var hits = Physics.RaycastAll(cam.position, cam.forward, 80f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                if (h.collider.GetComponentInParent<PlayerCombat>() != null) continue;
                end = h.point;
                var m = h.collider.GetComponentInParent<Monster>();
                if (m != null && !m.IsDead) { Audio.Sfx("arrow_hit", m.transform.position); float ad = (8f + gs.TotalAtk * 0.9f) * (1f + 1.8f * charge); m.TakeDamage(ad); if (charge >= 0.99f) { CombatFx.ShockRing(m.HitPoint - Vector3.up * 0.5f, 2.4f, CombatFx.SwordGold, 0.3f); CombatFx.HitSpark(m.HitPoint, CombatFx.SwordGold, 1.6f, 20); } }
                break;
            }
            Vector3 mz = origin + transform.forward * 0.7f;
            CombatFx.Muzzle(mz, CombatFx.BowCol);
            Vector3 endC = end;
            CombatFx.Arrow(mz, endC, charge >= 0.99f ? CombatFx.SwordGold : CombatFx.BowCol, 1f + 1.2f * charge, 110f + 60f * charge, () => CombatFx.HitSpark(endC, charge >= 0.99f ? CombatFx.SwordGold : CombatFx.BowCol, 0.6f + charge, 6 + (int)(10 * charge)));
        }

        // ---- Penyihir: klik kiri / J melempar bola sihir (api = ledakan kecil + bakar, tanah = batu + perlambat) ----
        bool sbHint;

        /// <summary>Penyihir pedang (Inferno Knight, Flame Paladin, Blade Wind, Wind Sword Saint): tebasan pedang yang diselimuti elemen, kombo 3 tebasan; tebasan ketiga melepas gelombang sihir.</summary>
        void SpellSlash(GameState gs)
        {
            FaceCamera();
            if (stamina < 6f) return;
            stamina -= 6f;
            lastAction = Time.time;
            attackReady = Time.time + 0.42f;
            if (Time.time > comboReset) combo = 0;
            if (PlayerAvatar.I != null) PlayerAvatar.I.Slash(combo);
            Audio.Sfx(combo == 0 ? "swing" : combo == 1 ? "swing2" : "heavy_swing", transform.position);
            string el = JobInfo.Elem(gs.job);
            Color col = MagicFx.ElemCol(el);
            Color hot = Color.Lerp(col, Color.white, 0.7f);
            int cIdx = combo; bool fin = cIdx == 2;
            float dmg = (SkillExec.MageBase(gs) * 0.75f + 6f) * (1f + 0.1f * cIdx) * (fin ? 1.5f : 1f) * 1.15f;
            combo = (combo + 1) % 3;
            comboReset = Time.time + 1.1f;

            Vector3 c0 = transform.position + Vector3.up * 1.0f;
            float roll = cIdx == 0 ? 22f : cIdx == 1 ? -22f : 90f;
            CombatFx.SlashArc(c0, transform.rotation, fin ? 3.6f : 2.9f, roll, col, fin ? 0.3f : 0.21f, fin ? 1.2f : 1f);
            CombatFx.Burst(c0 + transform.forward * 1.6f, col, fin ? 26 : 14, 0.9f, 2.2f, 0.7f, 0.14f);
            if (fin)
            {
                Vector3 front = transform.position + transform.forward * 2.0f - Vector3.up * 0.9f;
                CombatFx.ShockRing(front, 3.2f, col, 0.35f);
                CombatFx.Flash(c0 + transform.forward * 2f, col, 8f, 2f, 0.2f);
                MagicFx.BoltE(c0 + transform.forward * 0.8f, c0 + transform.forward * 9f, el, 0.5f, 48f, null);
            }
            foreach (var m in new List<Monster>(Monster.All))
            {
                if (m == null || m.IsDead) continue;
                Vector3 d = m.HitPoint - transform.position; d.y = 0f;
                float dist = d.magnitude;
                bool melee = dist <= 3.6f + m.Radius && (dist <= m.Radius + 0.5f || Vector3.Angle(transform.forward, d) <= 70f);
                bool wave = fin && !melee && dist <= 9f + m.Radius && Vector3.Angle(transform.forward, d) <= 18f;
                if (!melee && !wave) continue;
                float hit = melee ? dmg : dmg * 0.6f;
                if (el == "wind" && UnityEngine.Random.value < 0.15f) hit *= 1.5f;
                m.TakeDamage(hit);
                CombatFx.HitSpark(m.HitPoint, col, fin ? 1.3f : 0.9f, fin ? 18 : 12);
                MagicFx.ElemImpact(m.HitPoint, el, fin ? 1.6f : 1f);
                if (el == "fire") SkillExec.Burn(m, dmg * 0.12f, 3f);
            }
        }

        void CastBolt(GameState gs)
        {
            if (cam == null) return;
            float boltCost = 6f * gs.ManaCostMult;
            if (mana < boltCost) { StaffBash(gs); return; }
            FaceCamera();
            string el = JobInfo.Elem(gs.job);
            bool fire = el == "fire";
            bool newEl = el == "water" || el == "wind" || el == "holy";
            SpendMana(boltCost);
            attackReady = Time.time + (fire && gs.Knows("fm_stp_2") ? 0.42f : 0.55f);
            if (PlayerAvatar.I != null) PlayerAvatar.I.Cast();
            Audio.Sfx(fire ? "swing" : "heavy_swing", transform.position);

            float dmg = SkillExec.MageBase(gs) * 0.9f * gs.DmgMult;   // MageBase sudah memuat INT
            Vector3 end = cam.position + cam.forward * 70f;
            Monster target = null;
            var hits = Physics.RaycastAll(cam.position, cam.forward, 70f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                if (h.collider.GetComponentInParent<PlayerCombat>() != null) continue;
                end = h.point;
                var m = h.collider.GetComponentInParent<Monster>();
                if (m != null && !m.IsDead) target = m;
                break;
            }
            Vector3 mz = MagicFx.Tip();
            Color col = newEl ? MagicFx.ElemCol(el) : (fire ? CombatFx.FireCol : CombatFx.EarthCol);
            MagicFx.CastCharge(col, 0.18f, 0.35f); CombatFx.Muzzle(mz, col);
            Vector3 e2 = end;
            bool burn = fire && gs.Knows("fm_inf_2"), slow = !fire && gs.Knows("em_stn_2");
            if (newEl)
            {
                bool slowE = el == "water" && gs.Knows("wm_tid_3");
                Monster tg = target;
                MagicFx.BoltE(mz, e2, el, 0.42f, el == "wind" ? 70f : 52f, () =>
                {
                    MagicFx.ElemImpact(e2, el, 1.6f);
                    if (tg != null && !tg.IsDead)
                    {
                        tg.TakeDamage(dmg);
                        if (slowE || el == "water") tg.Slow(slowE ? 0.5f : 0.8f, 2f);
                        if (el == "holy") Heal(MaxHp * 0.01f);
                    }
                });
                return;
            }
            MagicFx.Orb(mz, e2, col, fire ? 0.42f : 0.55f, fire ? 55f : 44f, () =>
            {
                CombatFx.HitSpark(e2, col, 1f, 12);
                if (fire)
                {
                    CombatFx.ShockRing(e2, 2.2f, col, 0.25f);
                    foreach (var m in new List<Monster>(Monster.All))
                    {
                        if (m == null || m.IsDead) continue;
                        if (Vector3.Distance(m.transform.position, e2) > 1.8f + m.Radius) continue;
                        m.TakeDamage(m == target ? dmg : dmg * 0.6f);
                        if (burn) SkillExec.Burn(m, dmg * 0.1f, 3f);
                    }
                }
                else if (target != null && !target.IsDead)
                {
                    target.TakeDamage(dmg);
                    if (slow) target.Slow(0.55f, 2f);
                    CombatFx.Dust(e2, 4);
                }
            });
        }

        /// <summary>Mana habis: pukulan tongkat jarak dekat, tanpa biaya dan lemah.</summary>
        void StaffBash(GameState gs)
        {
            FaceCamera();
            lastAction = Time.time;
            attackReady = Time.time + 0.6f;
            if (Time.time > comboReset) combo = 0;
            if (PlayerAvatar.I != null) PlayerAvatar.I.Slash(combo);
            Audio.Sfx("swing", transform.position);
            combo = (combo + 1) % 3;
            comboReset = Time.time + 1.1f;
            if (Time.time > bashToast) { bashToast = Time.time + 4f; if (TownUI.I != null) TownUI.I.Toast("Mana habis! Memukul dengan tongkat (tunggu mana pulih)."); }
            float dmg = SkillExec.MageBase(gs) * 0.4f;
            foreach (var m in new List<Monster>(Monster.All))
            {
                if (m == null || m.IsDead) continue;
                Vector3 d = m.HitPoint - transform.position;
                d.y = 0f;
                float dist = d.magnitude;
                if (dist > 2.8f + m.Radius) continue;
                if (dist > m.Radius + 0.4f && Vector3.Angle(transform.forward, d) > 70f) continue;
                m.TakeDamage(dmg);
                CombatFx.HitSpark(m.HitPoint, new Color(0.8f, 0.8f, 0.9f), 0.8f, 6);
            }
        }

        void Dodge()
        {
            Audio.Sfx("dodge", transform.position);
            CombatFx.Dust(transform.position - Vector3.up * 0.9f, 12);
            CombatFx.ShockRing(transform.position - Vector3.up * 0.9f, 1.4f, new Color(1f, 1f, 1f, 0.6f), 0.25f);
            dashSpeed = (GameState.I != null && (GameState.I.Knows("fm_stp_1") || GameState.I.Knows("ar_mob_1"))) ? 18f : 14f;   // Light Ash / Light Roll
            stamina -= 20f;
            lastAction = Time.time;
            invulUntil = Time.time + 0.25f;
            dodgeInvulUntil = invulUntil;
            dashUntil = Time.time + 0.3f;
            dodgeReady = Time.time + 0.7f;

            Vector2 inp = TownInput.Move();
            if (inp.sqrMagnitude > 0.01f && cam != null)
            {
                Vector3 f = cam.forward;
                f.y = 0f;
                f.Normalize();
                Vector3 r = Vector3.Cross(Vector3.up, f);
                dashDir = (f * inp.y + r * inp.x).normalized;
                float dy2 = Mathf.DeltaAngle(transform.eulerAngles.y, Quaternion.LookRotation(dashDir).eulerAngles.y);
                transform.rotation = Quaternion.LookRotation(dashDir);
                if (PlayerAvatar.I != null && Mathf.Abs(dy2) > 1f) PlayerAvatar.I.KickYaw(-dy2);
            }
            else dashDir = -transform.forward;
            if (PlayerAvatar.I != null) PlayerAvatar.I.Roll(Vector3.Dot(dashDir, transform.forward) < -0.5f);
        }

        public void TakeDamage(float dmg, Vector3 from)
        {
            if (!IsDead && Time.time < dodgeInvulUntil && Invulnerable) { dodgeInvulUntil = 0f; Combat2.PerfectDodge(); }      // Hindar Sempurna: hindar tepat saat serangan datang
            if (IsDead || Invulnerable || DevTools.God || Cutscene.Active) return;
            if (Time.time < parryUntil)
            {
                // Perfect Parry: serangan dibatalkan, monster di sekitar terhuyung
                parryUntil = 0f;
                Audio.Sfx("block", transform.position);
                SkillAction.ParryHit(this, from);
                LockAttack(0.5f);
                CounterWindow(0.8f);
                foreach (var m in new List<Monster>(Monster.All)) if (!m.IsDead && Vector3.Distance(m.transform.position, transform.position) < 7f) m.Stagger(2.2f);
                if (TownUI.I != null) TownUI.I.Toast("PARRY!");
                return;
            }
            if (Time.time < riposteUntil)
            {
                riposteUntil = 0f;
                SkillAction.RiposteHit(this, from);
                return;
            }
            if (Time.time < buffUntil) dmg *= buffMult;
            dmg *= Challenges.TakenMul;
            SettingsExtra.Pulse(Mathf.Clamp01(dmg / 80f) * 0.6f + 0.15f, Mathf.Clamp01(dmg / 60f) * 0.8f + 0.2f, 0.18f);
            if (SkillAction.GuardActive) SkillAction.GuardFlash(this, from);
            var gp = GameState.I;
            if (gp != null) dmg *= 1f - Mathf.Min(0.75f, gp.Pas("dmgred"));
            if (gp != null && gp.IsMage)
            {
                if (gp.job == "FireMage" && gp.Knows("fm_wrd_2")) dmg *= 0.93f;                                   // Ash Skin
                if (gp.job == "EarthMage" && gp.Knows("em_bed_2") && cc.velocity.magnitude < 0.5f) dmg *= 0.85f;   // Rooted Stance
                if (gp.job == "EarthMage" && gp.Knows("em_bed_3")) RestoreMana(6f);   // Granite Will
                if (gp.job == "FireMage" && gp.Knows("fm_wrd_3"))                                                  // Smolder Guard
                    foreach (var mm in new List<Monster>(Monster.All))
                        if (mm != null && !mm.IsDead && Vector3.Distance(mm.transform.position, from) < 4.5f) SkillExec.Burn(mm, SkillExec.MageBase(gp) * 0.1f, 3f);
            }
            bool blocked = false;
            if (Guarding)
            {
                Vector3 d = from - transform.position;
                d.y = 0f;
                if (Vector3.Angle(transform.forward, d) < 100f)
                {
                    stamina = Mathf.Max(0f, stamina - dmg * 0.5f);
                    dmg *= 0.4f;
                    blocked = true;
                    lastAction = Time.time;
                }
            }
            hp -= dmg;
            lastHitTime = Time.time;
            Challenges.OnPlayerDamaged();
            bool bul = Time.time < bulwarkUntil;
            if (bul) { SkillAction.BulwarkHit(this, from); CombatFx.DamageNumber(transform.position + Vector3.up * 2.1f, dmg, false, CombatFx.SwordGold); Audio.Sfx("block", transform.position); if (hp <= 0f) Faint(); return; }
            if (blocked)
            {
                CombatFx.HitSpark(transform.position + transform.forward * 0.9f + Vector3.up * 1.1f, CombatFx.SwordCol, 1.3f, 14);
                CombatFx.ShockRing(transform.position - Vector3.up * 0.9f, 2.2f, CombatFx.SwordCol, 0.22f);
            }
            else CombatFx.HitSpark(transform.position + Vector3.up * 1.2f, CombatFx.HurtCol, 1.1f, 12);
            CombatFx.DamageNumber(transform.position + Vector3.up * 2.1f, dmg, false, blocked ? CombatFx.SwordCol : CombatFx.HurtCol);
            if (!blocked) AnimeLook.Hurt(dmg / Mathf.Max(1f, MaxHp));
            if (hp > 0f && PlayerAvatar.I != null) PlayerAvatar.I.Hurt();
            Audio.Sfx(blocked ? "block" : "hurt", transform.position);
            if (hp <= 0f) Faint();
        }

        /// <summary>Damage jatuh: menembus armor/guard/parry. allowKill=false: tidak pernah membunuh (menyisakan 1 HP).</summary>
        public void FallDamage(float dmg, bool allowKill)
        {
            if (IsDead || DevTools.God) return;
            if (!allowKill) dmg = Mathf.Min(dmg, hp - 1f);
            if (dmg < 1f) return;
            hp -= dmg;
            lastHitTime = Time.time;
            CombatFx.HitSpark(transform.position + Vector3.up * 0.3f, CombatFx.HurtCol, 1.2f, 12);
            CombatFx.DamageNumber(transform.position + Vector3.up * 2.1f, dmg, true, CombatFx.HurtCol);
            AnimeLook.Hurt(dmg / Mathf.Max(1f, MaxHp));
            if (hp > 0f && PlayerAvatar.I != null) PlayerAvatar.I.Hurt();
            Audio.Sfx("hurt", transform.position);
            if (hp <= 0f) Faint();
        }

        void Faint()
        {
            hp = 0f;
            IsDead = true;
            if (PlayerAvatar.I != null) PlayerAvatar.I.Faint();
            Audio.Sfx("faint");
            if (Difficulty.Permadeath) { Difficulty.OnPermaDeath(); return; }   // v35z102n14
            var gs = GameState.I;
            var q = gs.activeQuest;
            int n = 1;
            if (q != null) { q.faints++; n = q.faints; }
            if (TownUI.I != null) TownUI.I.Toast("Kamu pingsan! (" + n + "/3)" + (q != null ? " Reward dipotong 15%." : ""));
            if (OnFainted != null) OnFainted();
            if (TowerManager.I != null) { StartCoroutine(Respawn()); return; }           // Menara punya aturan sendiri (evakuasi setelah 3x)
            if (q != null && n >= 3 && HuntManager.I != null) { HuntManager.I.FailQuest(); return; }
            StartCoroutine(Respawn());
        }

        IEnumerator Respawn()
        {
            yield return new WaitForSeconds(2.5f);
            cc.enabled = false;
            transform.position = respawnPos;
            cc.enabled = true;
            hp = MaxHp;
            stamina = MaxStamina;
            mana = MaxMana;
            IsDead = false;
            if (PlayerAvatar.I != null) PlayerAvatar.I.Revive();
            if (OnRevived != null) OnRevived();
        }

        /// <summary>Dipakai DevTools: hidupkan lagi tanpa menunggu respawn.</summary>
        public void Revive() { if (!IsDead) return; StopAllCoroutines(); IsDead = false; hp = MaxHp; stamina = MaxStamina; mana = MaxMana; }

        public void Heal(float amount) { hp = Mathf.Min(MaxHp, hp + amount); }

        public void FullRestore() { hp = MaxHp; stamina = MaxStamina; mana = MaxMana; }

        public void UseManaPotion()
        {
            var gs = GameState.I;
            if (IsDead) return;
            if (TowerChallenge.BlockPotion()) return;
            if (!gs.IsMage) { TownUI.I.Toast("Ramuan Mana hanya berguna bagi Penyihir."); return; }
            if (mana >= MaxMana - 1f) { TownUI.I.Toast("Mana sudah penuh."); return; }
            if (!gs.RemoveItem("mana_potion", 1)) { TownUI.I.Toast("Tidak punya Ramuan Mana."); return; }
            RestoreMana(Mathf.Max(40f, MaxMana * 0.5f));
            Audio.Sfx("potion");
            if (PlayerAvatar.I != null) PlayerAvatar.I.Drink();
            CombatFx.Burst(transform.position + Vector3.up * 1.2f, new Color(0.4f, 0.65f, 1f), 18, 0.8f, 2f, 0.8f, 0.14f);
            TownUI.I.Toast("Ramuan Mana dipakai.");
        }

        public void UsePotion()
        {
            var gs = GameState.I;
            if (IsDead) return;
            if (TowerChallenge.BlockPotion()) return;
            if (hp >= MaxHp - 1f) { TownUI.I.Toast("HP sudah penuh."); return; }
            if (!gs.RemoveItem("potion", 1)) { TownUI.I.Toast("Tidak punya Ramuan."); return; }
            Heal(60f);
            Audio.Sfx("potion");
            if (PlayerAvatar.I != null) PlayerAvatar.I.Drink();
            TownUI.I.Toast("Ramuan dipakai (+60 HP).");
        }
    }
}
