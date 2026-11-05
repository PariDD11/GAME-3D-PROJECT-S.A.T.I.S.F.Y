using UnityEngine;

namespace HunterGuild
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float walkSpeed = 4.5f;
        public float sprintSpeed = 7.5f;
        public float gravity = -22f;
        public float turnSpeed = 14f;
        public float jumpSpeed = 8.4f;                // tinggi lompat ~1.6 m
        public float safeFall = 4f;                   // jatuh sampai 4 m tidak melukai

        CharacterController cc;
        Transform cam;
        float vy;
        Interactable current;
        bool creatorShown;
        float coyote, jumpBuf, peakY; int airJumps; bool airborne; Vector3 lastPos;
        // --- renang & nafas ---
        public static bool Swimming, Submerged; public static float Breath01 = 1f;
        float breath = -1f, drownT, bubbleT, surfNow;
        // --- memanjat, meluncur, animasi udara ---
        public static bool Climbing, Gliding; public static float LandT, VelY, AirT, ClimbSpd01;
        float climbMantle, driftT, glideT; Vector3 drift;

        void Start()
        {
            cc = GetComponent<CharacterController>();
            Climbing = false; Gliding = false; LandT = 0f; AirT = 0f; ClimbSpd01 = 0f;
            if (Camera.main != null) cam = Camera.main.transform;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            EnsureWindows();
        }

        /// <summary>Dipanggil tombol menu HUD: 0 karakter, 1 skill, 2 tas, 3 quest, 4 bestiary.</summary>
        public static void OpenWindow(int w)
        {
            var ui = TownUI.I; if (ui != null && ui.PanelOpen) return;
            if (w == 0) { ProfileUI.Open(); return; }
            EnsureWindows();
            if (w == 1) SkillUI.I.Show(); else if (w == 2) BackpackUI.I.Show(); else if (w == 3) JournalUI.I.Show(); else if (w == 4) BestiaryUI.I.Show();
        }

        static bool jobReminded;
        static float fightCheckAt; static bool fightCached;
        /// <summary>True bila sedang bertarung: baru terkena hit (&lt;8 dtk) atau ada monster yang mengejar/menyerang dalam 35 m.</summary>
        public static bool InFight()
        {
            var pc = PlayerCombat.I; if (pc == null) return false;
            if (Time.time - pc.lastHitTime < 8f) return true;
            if (Time.time < fightCheckAt) return fightCached;
            fightCheckAt = Time.time + 0.4f; fightCached = false;
            foreach (var m in Monster.All)
                if (m != null && m.InCombat && (m.transform.position - pc.transform.position).sqrMagnitude < 35f * 35f) { fightCached = true; break; }
            return fightCached;
        }

        static void EnsureWindows()
        {
            if (BackpackUI.I == null) new GameObject("BackpackUI").AddComponent<BackpackUI>();
            if (SkillUI.I == null) new GameObject("SkillUI").AddComponent<SkillUI>();
            if (JournalUI.I == null) new GameObject("JournalUI").AddComponent<JournalUI>();
            if (BestiaryUI.I == null) new GameObject("BestiaryUI").AddComponent<BestiaryUI>();
        }

        void Update()
        {
            var gs = GameState.I;
            var ui = TownUI.I;
            bool menuOpen = (ui != null && ui.PanelOpen) || MiniGame.Active || FishingSystem.Busy || Cutscene.Active;

            if (DevTools.Fly)
            {
                // mode developer: terbang + tembus dinding
                if (cc.enabled) cc.enabled = false;
                vy = 0f; airborne = false; Swimming = false; Submerged = false;
                if (!menuOpen)
                {
                    Vector3 fm = ReadMove(gs);
                    float up = (TownInput.DevUpHeld() ? 1f : 0f) - (TownInput.DevDownHeld() ? 1f : 0f);
                    transform.position += (fm + Vector3.up * up * 8f * DevTools.Speed) * Time.deltaTime;
                }
            }
            else
            {
                if (!cc.enabled) cc.enabled = true;
                Vector3 move = menuOpen ? Vector3.zero : ReadMove(gs);
                float dt = Time.deltaTime;
                if ((transform.position - lastPos).sqrMagnitude > 36f) airborne = false;          // teleport / respawn / pindah scene: bukan jatuh
                // ---- air: berenang bila kedalaman > 1.15 m (setinggi dada), menyelam dengan Ctrl, naik dengan Spasi ----
                float surf; bool inW = WaterZone.Query(new Vector3(transform.position.x, transform.position.y - 0.9f, transform.position.z), out surf);
                float depth = inW ? surf - (transform.position.y - 0.9f) : 0f;
                bool swim = inW && depth > (Swimming ? 0.85f : 1.15f);
                if (swim != Swimming) { Swimming = swim; if (swim) EnterWater(surf); }
                surfNow = surf;
                Submerged = swim && transform.position.y + 0.75f < surf - 0.05f;
                TickBreath(gs, swim, dt);
                if (swim)
                {
                    Climbing = false; Gliding = false; AirT = 0f;
                    airborne = false; coyote = 0f; jumpBuf = 0f; airJumps = gs != null && gs.job == "WindMage" ? 1 : 0;
                    float want;
                    if (!menuOpen && TownInput.DiveHeld()) want = -3.2f;
                    else if (!menuOpen && TownInput.JumpHeld() && Submerged) want = 3.4f;
                    else want = Mathf.Clamp(((surf - 0.3f) - transform.position.y) * 2.5f, -3f, 3f);          // mengapung: kepala di atas permukaan
                    vy = Mathf.Lerp(vy, want, 1f - Mathf.Exp(-8f * dt));
                    if (!menuOpen && TownInput.JumpDown() && !Submerged) vy = 7f;                               // loncat keluar dari air (naik ke tepian)
                    Vector3 sm = move * 0.58f; sm.y = vy;
                    cc.Move(sm * dt);
                    lastPos = transform.position;
                    BubblesFx(dt);
                }
                else if (TickClimb(gs, menuOpen, dt)) { lastPos = transform.position; }
                else
                {
                bool grounded = cc.isGrounded;
                AirT = grounded ? 0f : AirT + dt; if (LandT > 0f) LandT -= dt;
                if (grounded) Gliding = false;
                if (grounded) { coyote = 0.12f; airJumps = gs != null && gs.job == "WindMage" ? 1 : 0; }
                else coyote -= dt;
                bool dead = PlayerCombat.I != null && PlayerCombat.I.IsDead;
                if (!menuOpen && !dead && TownInput.JumpDown()) jumpBuf = 0.14f; else jumpBuf -= dt;
                if (grounded && vy < 0f) vy = -2f;
                bool didJump = false;
                if (jumpBuf > 0f)
                {
                    if (coyote > 0f) { vy = jumpSpeed; coyote = 0f; jumpBuf = 0f; JumpFx(false); didJump = true; Gliding = false; }
                    else if (airJumps > 0)
                    {   // Penyihir Angin: lompat kedua di udara (angin menopang)
                        airJumps--; vy = jumpSpeed * 0.95f; jumpBuf = 0f; peakY = transform.position.y; JumpFx(true); didJump = true;
                    }
                }
                // meluncur (glider): tekan Lompat saat jatuh dan lompat ganda sudah habis; tekan lagi untuk menutup
                if (!menuOpen && !dead && !didJump && TownInput.JumpDown() && !grounded && coyote <= 0f && airJumps <= 0 && AirT > 0.3f && !MountSystem.Mounted && GameState.I != null && GameState.I.job == "WindMage")
                {
                    if (Gliding) { Gliding = false; }
                    else if (vy < 0.5f && PlayerCombat.I != null && PlayerCombat.I.stamina > 8f) { Gliding = true; jumpBuf = 0f; Audio.Sfx("dodge", transform.position); CombatFx.ShockRing(transform.position, 2f, new Color(0.8f, 1f, 1f, 0.7f), 0.3f); }
                }
                if (Gliding)
                {
                    var pcg = PlayerCombat.I;
                    if (pcg != null) pcg.SpendStamina(4f * dt);
                    if (pcg == null || pcg.stamina <= 0.5f || dead) Gliding = false;
                }
                if (Gliding)
                {
                    vy = Mathf.Lerp(vy, -2.1f, 1f - Mathf.Exp(-6f * dt));
                    Vector3 gf = move.sqrMagnitude > 0.01f ? move.normalized : transform.forward; gf.y = 0f; gf.Normalize();
                    move = gf * 9f;
                    if (gf.sqrMagnitude > 0.1f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(gf), 6f * dt);
                    peakY = transform.position.y;
                }
                else vy += gravity * dt;
                if (driftT > 0f) { driftT -= dt; move += drift * Mathf.Clamp01(driftT / 0.4f); }
                VelY = vy;
                move.y = vy;
                cc.Move(move * dt);
                // fall damage: ukur selisih ketinggian dari titik tertinggi sampai mendarat
                bool g2 = cc.isGrounded;
                float y = transform.position.y;
                if (!g2) { if (!airborne) { airborne = true; peakY = y; } else if (y > peakY) peakY = y; }
                else if (airborne) { airborne = false; Land(peakY - y); }
                lastPos = transform.position;
                }
            }

            // Cari titik interaksi terdekat
            current = null;
            float best = float.MaxValue;
            foreach (var it in Interactable.All)
            {
                Vector3 d = it.transform.position - transform.position;
                d.y = 0f;
                float dist = d.magnitude;
                if (dist <= it.radius && dist < best) { best = dist; current = it; }
            }

            if (menuOpen || ui == null || gs == null) return;

            // Kustomisasi karakter: otomatis saat pertama kali main, atau tekan G (hanya di kota/guild)
            if (!CharacterData.Exists && !creatorShown) { creatorShown = true; CharacterCreatorUI.Open(true); return; }
            if (TownInput.CustomizeDown() && HuntManager.I == null) { CharacterCreatorUI.Open(false); return; }

            // Pilih job dulu (GDD 4.2). Menu terbuka lagi sampai pemain memilih.
            if (gs.requireJobChoice && !gs.jobChosen && gs.level >= 5)
            {
                string scn = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (scn == StarterConfig.VillageScene || scn == StarterConfig.TutorialScene)
                {
                    // Level 5 di area pemula: Brann yang membimbing pemilihan job (tidak dipaksa)
                    if (!jobReminded && !InFight()) { jobReminded = true; ui.Toast("Level 5! Temui Veteran Brann di desa untuk memilih job-mu."); }
                }
                else if (!InFight()) { JobMenu.Open(); return; }   // jangan pernah memotong pertarungan
            }

            if (current == null && FishingSystem.Available(transform)) { ui.SetPrompt("[" + PadInput.InteractHint() + "] Memancing"); if (TownInput.InteractDown()) { FishingSystem.Begin(transform); return; } }
            if (current != null) ui.SetPrompt("[" + PadInput.InteractHint() + "] " + current.prompt);

            if (TownInput.InteractDown() && current != null && Time.frameCount > DialogueUI.ClosedFrame + 1) { DialogueUI.SetContext(current); NpcVoice.Greet(current); gs.AddStat("interacts"); current.Interact(); return; }
            if (TownInput.ProfileDown()) { ProfileUI.Open(); return; }
            if (TownInput.InventoryDown()) { EnsureWindows(); BackpackUI.I.Show(); return; }
            if (TownInput.SkillMenuDown()) { EnsureWindows(); SkillUI.I.Show(); return; }
            if (TownInput.JournalDown()) { EnsureWindows(); JournalUI.I.Show(); return; }
            if (TownInput.BestiaryDown()) { EnsureWindows(); BestiaryUI.I.Show(); return; }
            if (TownInput.DrinkDown()) gs.DrinkCanteen();

            for (int i = 0; i < 6; i++) if (TownInput.NumberDown(i)) gs.UseQuickbar(i);
            for (int i = 0; i < 7; i++) if (TownInput.SkillKeyDown(i)) gs.TryUseSkill(i);

            // Tombol debug untuk testing tanpa scene hunting
            if (TownInput.DebugDown(9)) gs.GiveTestMaterials();
            if (TownInput.DebugDown(10) && gs.activeQuest != null)
            {
                gs.activeQuest.completed = true;
                ui.Toast("[DEBUG] Quest ditandai selesai. Lapor ke Guild.");
            }
        }

        // ---- memanjat: maju ke dinding tinggi sambil melompat/di udara; Spasi = lompat menjauh, Ctrl = lepas ----
        bool TickClimb(GameState gs, bool menuOpen, float dt)
        {
            var pc = PlayerCombat.I;
            bool dead = pc == null || pc.IsDead;
            if (menuOpen || dead || MountSystem.Mounted || HuntManager.I == null) { Climbing = false; ClimbSpd01 = 0f; return false; }   // v35z102n15: memanjat hanya di area hunt
            Vector2 inp = TownInput.Move();
            Vector3 org = transform.position + Vector3.up * 0.2f;
            Vector3 fwd = transform.forward; fwd.y = 0f; fwd.Normalize();
            RaycastHit hit;
            bool wall = Physics.Raycast(org, fwd, out hit, 0.8f, ~0, QueryTriggerInteraction.Ignore) && hit.normal.y < 0.35f && Climbable(hit.collider);
            if (climbMantle > 0f)
            {
                climbMantle -= dt;
                cc.Move((Vector3.up * 5.5f + fwd * 2.4f) * dt); vy = 0f; airborne = false;
                if (climbMantle <= 0f) Climbing = false;
                ClimbSpd01 = 1f;
                return true;
            }
            if (!Climbing)
            {
                if (!wall || inp.y < 0.3f || pc.stamina < 8f) return false;
                if (cc.isGrounded && !TownInput.JumpHeld()) return false;
                if (!Physics.Raycast(transform.position + Vector3.up * 1.6f, fwd, 1f, ~0, QueryTriggerInteraction.Ignore)) return false;   // bukan tembok: hanya undakan kecil
                Climbing = true; Gliding = false; vy = 0f; airborne = false; jumpBuf = 0f;
                if (TownUI.I != null) TownUI.I.Toast("Memanjat! Spasi = lompat menjauh, " + Bindings.Name(Act.Dodge) + " = lepas.");
            }
            if (!wall)
            {   // puncak: naik ke atas bila ada pijakan di depan
                if (inp.y > 0.1f && Physics.Raycast(transform.position + Vector3.up * 0.9f + fwd * 0.7f, Vector3.down, 2.2f, ~0, QueryTriggerInteraction.Ignore)) { climbMantle = 0.38f; return true; }
                Climbing = false; return false;
            }
            Vector3 n = hit.normal; n.y = 0f; n.Normalize();
            Vector3 face = -n;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face), 14f * dt);
            Vector3 right = Vector3.Cross(Vector3.up, face);
            bool fast = TownInput.Sprint();
            pc.SpendStamina(((inp.y > 0.1f ? 9f : 4f) * (fast ? 1.8f : 1f)) * dt);
            if (pc.stamina <= 0.5f) { Climbing = false; if (TownUI.I != null) TownUI.I.Toast("Stamina habis, kamu terlepas!"); return false; }
            float spd = 2.4f * (fast ? 1.6f : 1f);
            Vector3 v = Vector3.up * inp.y * spd + right * inp.x * spd - n * 1.0f;
            ClimbSpd01 = Mathf.Clamp01(inp.magnitude);
            cc.Move(v * dt); vy = 0f; airborne = false;
            if (TownInput.JumpDown()) { Climbing = false; vy = jumpSpeed * 0.85f; drift = n * 6f; driftT = 0.4f; JumpFx(false); return false; }
            if (TownInput.DodgeDown()) { Climbing = false; return false; }
            if (cc.isGrounded && inp.y <= 0.05f) Climbing = false;
            return true;
        }

        static bool Climbable(Collider c)
        {
            if (c == null || c.isTrigger) return false;
            if (c.GetComponentInParent<Monster>() != null) return false;
            if (c.GetComponentInParent<CharacterController>() != null) return false;
            return true;
        }

        float BreathMax(GameState gs) { return gs != null && gs.job == "WaterMage" ? 50f : 20f; }

        void TickBreath(GameState gs, bool swim, float dt)
        {
            float max = BreathMax(gs);
            if (breath < 0f || breath > max) breath = max;
            var pc = PlayerCombat.I;
            if (pc != null && pc.IsDead) breath = max;
            if (swim && Submerged) breath = Mathf.Max(0f, breath - dt);
            else breath = Mathf.Min(max, breath + dt * 9f);
            Breath01 = breath / max;
            if (breath <= 0f && pc != null && !pc.IsDead)
            {
                drownT -= dt;
                if (drownT <= 0f)
                {
                    drownT = 1f;
                    pc.FallDamage(pc.MaxHp * 0.08f, true);                            // tenggelam: 8% HP maks per detik
                    if (TownUI.I != null) TownUI.I.Toast("Kehabisan nafas! Naiklah ke permukaan (tahan Spasi).");
                }
            }
            else drownT = 0.6f;
            if (!swim) { Swimming = false; Submerged = false; }
        }

        void EnterWater(float surf)
        {
            Vector3 p = new Vector3(transform.position.x, surf + 0.03f, transform.position.z);
            CombatFx.ShockRing(p, 2.4f, new Color(0.8f, 0.95f, 1f, 0.8f), 0.4f);
            CombatFx.Burst(p + Vector3.up * 0.2f, new Color(0.85f, 0.95f, 1f), 12, 0.7f, 2.2f, 0.6f, 0.1f);
            Audio.Sfx("dodge", transform.position);
            vy *= 0.25f;
        }

        void BubblesFx(float dt)
        {
            if (!Submerged) { bubbleT = 0f; return; }
            bubbleT -= dt;
            if (bubbleT > 0f) return;
            bubbleT = 0.9f;
            CombatFx.Burst(transform.position + Vector3.up * 0.7f, new Color(0.85f, 0.95f, 1f), 3, 0.9f, 0.7f, 0.4f, 0.07f);
        }

        void JumpFx(bool air)
        {
            Vector3 foot = transform.position - Vector3.up * 0.9f;
            if (air)
            {
                CombatFx.ShockRing(foot, 2.2f, new Color(0.65f, 1f, 0.85f, 0.8f), 0.3f);
                CombatFx.Dust(foot, 8);
                Audio.Sfx("dodge", transform.position);
            }
            else CombatFx.Dust(foot, 5);
        }

        void Land(float drop)
        {
            if (drop > 0.6f) LandT = Mathf.Clamp(0.1f + drop * 0.03f, 0.12f, 0.4f);
            if (drop < 1.2f) return;
            Vector3 foot = transform.position - Vector3.up * 0.9f;
            CombatFx.Dust(foot, drop > safeFall ? 14 : 5);
            if (drop <= safeFall) return;
            float ws; if (WaterZone.Query(new Vector3(transform.position.x, transform.position.y - 0.9f, transform.position.z), out ws) && ws - (transform.position.y - 0.9f) > 0.9f)
            { CombatFx.ShockRing(new Vector3(transform.position.x, ws, transform.position.z), 3f, new Color(0.8f, 0.95f, 1f, 0.8f), 0.4f); return; }        // air meredam jatuh
            var pc = PlayerCombat.I; var gs = GameState.I;
            if (pc == null) return;
            float frac = Mathf.Clamp01((drop - safeFall) * 0.04f);                 // 10 m = 24% HP, 20 m = 64%, 29 m+ = 100%
            float dmg = pc.MaxHp * frac;
            bool wind = gs != null && gs.job == "WindMage";
            if (wind) dmg *= 0.35f;                                                // Penyihir Angin meredam jatuh
            CombatFx.ShockRing(foot, Mathf.Min(5f, 1.5f + drop * 0.15f), new Color(1f, 0.9f, 0.7f, 0.7f), 0.3f);
            ThirdPersonCamera.Shake(Mathf.Min(0.35f, 0.04f * drop), 0.25f);
            pc.FallDamage(dmg, drop >= 25f && !wind);
        }

        Vector3 ReadMove(GameState gs)
        {
            Vector2 input = TownInput.Move();
            if (input.sqrMagnitude < 0.01f) return Vector3.zero;

            Vector3 fwd = cam != null ? cam.forward : transform.forward;
            fwd.y = 0f;
            fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            Vector3 dir = (fwd * input.y + right * input.x).normalized;

            bool parched = gs != null && gs.thirst <= 0f;      // GDD 9: thirst 0 = tidak bisa sprint, -15% speed
            bool sprint = TownInput.Sprint() && !parched && !Stealth.Sneaking;
            float speed = sprint ? sprintSpeed : walkSpeed;
            if (parched) speed *= 0.85f;
            if (gs != null && gs.Knows("em_flw_1")) speed *= 1.06f;      // Sure Footing
            if (gs != null) speed *= 1f + gs.Pas("spd");
            if (MountSystem.Mounted) speed *= MountSystem.SpeedMul;
            if (Stealth.Sneaking) speed *= 0.5f;
            speed *= DevTools.Speed;
            speed *= Combat2.PlayerMoveMul();

            if (!ThirdPersonCamera.FirstPerson) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
            return dir * speed;
        }
    }
}
