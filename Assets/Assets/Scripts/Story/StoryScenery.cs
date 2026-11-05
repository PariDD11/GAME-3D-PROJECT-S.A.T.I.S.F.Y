using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Isi cerita untuk scene yang sudah ada, dibangun saat runtime (tanpa rebuild scene):
    ///  HuntArea_Bone  : tulang di rumput, Batu Dua Nama + lentera Teno, pintu tangga 800.
    ///  HuntArea_Demon : pondok Varra + dapur + dua imp, 100 anak tangga obsidian, Altar Ritual dengan ratusan peziarah (stealth), Dagan melepas api saat Zargoth &lt; 50% HP.
    /// Koordinat desain builder (x,z) dipetakan ke dunia (-x,-z) karena konstanta M = -1 di HuntSceneBuilder.
    /// </summary>
    public static class StoryScenery
    {
        static GameObject root; static string scene = "";
        static Vector3 altarPos; static bool hasAltar;
        static float alert, alarmCd, tick; static Vector3 lastPos; static float speed;

        static Vector3 W(float dx, float dz) { var p = new Vector3(-dx, 0f, -dz); p.y = RT.GroundY(p); return p; }

        static Material M(Color c) { return CombatFx.Solid(null, c); }
        static GameObject P(PrimitiveType t, Transform par, Vector3 lp, Vector3 sc, Color c, bool collider = false)
        {
            var g = GameObject.CreatePrimitive(t);
            if (!collider) { var col = g.GetComponent<Collider>(); if (col != null) Object.Destroy(col); }
            g.transform.SetParent(par, false); g.transform.localPosition = lp; g.transform.localScale = sc;
            g.GetComponent<Renderer>().sharedMaterial = M(c);
            return g;
        }
        static void Text(Transform par, string s, Vector3 lp, Color c, float size = 0.09f)
        {
            var go = new GameObject("Teks"); go.transform.SetParent(par, false); go.transform.localPosition = lp;
            var tm = go.AddComponent<TextMesh>(); tm.text = s; tm.characterSize = size; tm.fontSize = 48; tm.anchor = TextAnchor.MiddleCenter; tm.color = c;
            go.AddComponent<Billboard>();
        }
        static Light LightAt(Transform par, Vector3 lp, Color c, float range, float inten)
        {
            var go = new GameObject("Cahaya"); go.transform.SetParent(par, false); go.transform.localPosition = lp;
            var l = go.AddComponent<Light>(); l.type = LightType.Point; l.color = c; l.range = range; l.intensity = inten; return l;
        }

        // ------------------------------------------------------------------ bangun
        public static void Build(string sc)
        {
            if (root != null) Object.Destroy(root);
            root = null; scene = sc; hasAltar = false; alert = 0f; alarmCd = 0f;
            if (sc != "HuntArea_Bone" && sc != "HuntArea_Demon") return;
            Random.InitState(5150 + sc.Length);
            root = new GameObject("StoryScenery");
            if (sc == "HuntArea_Bone") BuildBone(); else BuildDemon();
        }

        static void BuildBone()
        {
            var bone = new Color(0.86f, 0.84f, 0.76f);
            for (int i = 0; i < 46; i++)
            {
                var p = W(Random.Range(-130f, 130f), Random.Range(-260f, -60f));
                var g = new GameObject("Tulang"); g.transform.SetParent(root.transform, false); g.transform.position = p;
                int k = i % 3;
                if (k == 0) { for (int r = 0; r < 4; r++) { var rib = P(PrimitiveType.Capsule, g.transform, new Vector3(r * 0.32f - 0.5f, 0.35f, 0f), new Vector3(0.08f, 0.55f, 0.08f), bone); rib.transform.localRotation = Quaternion.Euler(0f, 0f, 60f - r * 5f); } }
                else if (k == 1) { P(PrimitiveType.Sphere, g.transform, new Vector3(0, 0.2f, 0), new Vector3(0.38f, 0.32f, 0.4f), bone); P(PrimitiveType.Sphere, g.transform, new Vector3(0, 0.1f, 0.2f), new Vector3(0.25f, 0.18f, 0.2f), bone); }
                else { var b = P(PrimitiveType.Capsule, g.transform, new Vector3(0, 0.12f, 0), new Vector3(0.1f, 0.7f, 0.1f), bone); b.transform.localRotation = Quaternion.Euler(80f, Random.Range(0f, 360f), 0f); }
                g.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
            // Batu Dua Nama: menggantikan placeholder LoreObject di Padang Hantu
            var stone = GameObject.Find("LoreObject_nd_o_ketukan");
            Transform host = stone != null ? stone.transform : null;
            if (host == null) { var h = new GameObject("BatuDuaNama"); h.transform.SetParent(root.transform, false); h.transform.position = W(190f, 190f); host = h.transform; }
            else foreach (var r in stone.GetComponentsInChildren<MeshRenderer>()) r.enabled = false;
            var slab = P(PrimitiveType.Cube, host, new Vector3(0f, 0.75f, 0f), new Vector3(1.7f, 1.5f, 0.45f), new Color(0.55f, 0.56f, 0.6f), true);
            P(PrimitiveType.Cube, host, new Vector3(0f, 1.55f, 0f), new Vector3(1.4f, 0.18f, 0.4f), new Color(0.48f, 0.49f, 0.53f));
            Text(host, "Lysa Ardent", new Vector3(-0.38f, 1.0f, -0.27f), new Color(1f, 0.82f, 0.3f), 0.05f);
            Text(host, "Teno Kest", new Vector3(0.38f, 0.55f, -0.27f), new Color(1f, 0.82f, 0.3f), 0.04f);
            // pintu tangga 800 di dekat batu
            var door = new GameObject("PintuTangga"); door.transform.SetParent(root.transform, false); door.transform.position = W(190f - 7f, 190f);
            P(PrimitiveType.Cube, door.transform, new Vector3(0f, 0.04f, 0f), new Vector3(2.4f, 0.08f, 3f), new Color(0.04f, 0.04f, 0.06f));
            for (int i = 0; i < 5; i++) P(PrimitiveType.Cube, door.transform, new Vector3(0f, -0.02f - i * 0.02f, 0.6f - i * 0.5f), new Vector3(2.0f - i * 0.08f, 0.05f, 0.5f), new Color(0.1f, 0.1f, 0.12f));
            Text(door.transform, "800 ANAK TANGGA", new Vector3(0f, 1.4f, 0f), new Color(0.8f, 0.85f, 1f), 0.07f);
            LightAt(door.transform, new Vector3(0f, 1.2f, 0f), new Color(0.6f, 0.75f, 1f), 8f, 0.9f);
        }

        static void BuildDemon()
        {
            // ---- pondok Varra
            Vector3 hut = W(-18f, -190f);
            var h = new GameObject("PondokVarra"); h.transform.SetParent(root.transform, false); h.transform.position = hut;
            P(PrimitiveType.Cube, h.transform, new Vector3(0f, 1.5f, 0f), new Vector3(6f, 3f, 5f), new Color(0.12f, 0.1f, 0.11f), true);
            var r1 = P(PrimitiveType.Cube, h.transform, new Vector3(-1.8f, 3.5f, 0f), new Vector3(3.8f, 0.25f, 5.8f), new Color(0.36f, 0.27f, 0.15f)); r1.transform.localRotation = Quaternion.Euler(0f, 0f, 32f);
            var r2 = P(PrimitiveType.Cube, h.transform, new Vector3(1.8f, 3.5f, 0f), new Vector3(3.8f, 0.25f, 5.8f), new Color(0.36f, 0.27f, 0.15f)); r2.transform.localRotation = Quaternion.Euler(0f, 0f, -32f);
            P(PrimitiveType.Cylinder, h.transform, new Vector3(2f, 4.2f, -1.2f), new Vector3(0.7f, 1.4f, 0.7f), new Color(0.15f, 0.12f, 0.12f));
            P(PrimitiveType.Cube, h.transform, new Vector3(0f, 1.0f, -2.55f), new Vector3(1.3f, 2.0f, 0.12f), new Color(0.2f, 0.12f, 0.06f));
            LightAt(h.transform, new Vector3(0f, 2.2f, -3.5f), new Color(1f, 0.7f, 0.35f), 16f, 1.8f);
            Text(h.transform, "Dapur Varra\nSup bawang", new Vector3(0f, 5.6f, -2f), new Color(1f, 0.93f, 0.6f), 0.1f);
            // kuali + meja di depan pondok (titik interaksi)
            var kit = new GameObject("DapurVarra"); kit.transform.SetParent(root.transform, false); kit.transform.position = W(-10f, -190f);
            P(PrimitiveType.Cylinder, kit.transform, new Vector3(0f, 0.55f, 0f), new Vector3(1.3f, 0.5f, 1.3f), new Color(0.08f, 0.08f, 0.09f), true);
            P(PrimitiveType.Cylinder, kit.transform, new Vector3(0f, 1.0f, 0f), new Vector3(1.15f, 0.03f, 1.15f), new Color(0.55f, 0.36f, 0.12f));
            for (int i = 0; i < 3; i++) P(PrimitiveType.Cube, kit.transform, new Vector3(-0.5f + i * 0.5f, 0.12f, 0.9f), new Vector3(0.12f, 0.24f, 0.7f), new Color(0.3f, 0.2f, 0.1f));
            LightAt(kit.transform, new Vector3(0f, 1.4f, 0f), new Color(1f, 0.6f, 0.25f), 9f, 1.4f);
            kit.AddComponent<VarraKitchen>();
            // dua imp
            MakeImp(W(-8f, -184f), new Color(0.75f, 0.18f, 0.15f), "Imp Merah");
            MakeImp(W(-8f, -196f), new Color(0.45f, 0.45f, 0.5f), "Imp Kelabu");

            // ---- 100 anak tangga obsidian menuju Takhta
            var obs = new Color(0.05f, 0.03f, 0.07f);
            var stairs = new GameObject("TanggaObsidian"); stairs.transform.SetParent(root.transform, false);
            for (int i = 0; i < 100; i++)
            {
                var p = W(0f, 212f + i * 0.6f);
                var s = P(PrimitiveType.Cube, stairs.transform, Vector3.zero, new Vector3(7f, 0.12f + (i % 2) * 0.03f, 0.55f), obs);
                s.transform.position = p + Vector3.up * (0.06f + i * 0.004f);
                if (i % 10 == 9)
                {
                    LightAt(s.transform, new Vector3(3.2f, 1.2f, 0f), new Color(0.75f, 0.2f, 0.95f), 10f, 1.6f);
                    if ((i + 1) % 25 == 0) Text(s.transform, "ANAK TANGGA " + (i + 1) + " / 100", new Vector3(0f, 2.2f, 0f), new Color(0.85f, 0.6f, 1f), 0.09f);
                }
            }

            // ---- Altar Ritual: Telinga + peziarah
            altarPos = W(-170f, 80f); hasAltar = true;
            var al = new GameObject("TelingaAltar"); al.transform.SetParent(root.transform, false); al.transform.position = altarPos;
            P(PrimitiveType.Cylinder, al.transform, new Vector3(0f, 1.2f, 0f), new Vector3(2.6f, 1.2f, 2.6f), new Color(0.12f, 0.1f, 0.14f), true);
            P(PrimitiveType.Sphere, al.transform, new Vector3(0f, 3.0f, 0f), Vector3.one * 0.9f, new Color(0.7f, 0.25f, 0.95f));
            LightAt(al.transform, new Vector3(0f, 3.2f, 0f), new Color(0.7f, 0.2f, 0.95f), 18f, 2.0f);
            Text(al.transform, "TELINGA", new Vector3(0f, 4.4f, 0f), new Color(0.85f, 0.6f, 1f), 0.1f);
            var pil = new GameObject("Peziarah"); pil.transform.SetParent(root.transform, false);
            int pilN = 0; int[] counts = { 14, 22, 30, 38 }; float[] rads = { 7f, 11f, 15f, 19f };
            for (int ring = 0; ring < 4; ring++)
                for (int i = 0; i < counts[ring]; i++)
                {
                    float a = (i + Random.value * 0.4f) * Mathf.PI * 2f / counts[ring];
                    var pp = altarPos + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * (rads[ring] + Random.Range(-0.8f, 0.8f));
                    pp.y = RT.GroundY(pp);
                    var yaw = Mathf.Atan2(altarPos.x - pp.x, altarPos.z - pp.z) * Mathf.Rad2Deg;
                    var f = NpcKit.Spawn(pil.transform, "Peziarah", pp, yaw, PilgrimSpec(pilN++ % 8), false, false);
                    f.AddComponent<PilgrimSway>();
                }
            Text(al.transform, "Jangan berlari. Telinga mendengar langkah yang tergesa.", new Vector3(0f, 5.4f, 0f), new Color(1f, 0.8f, 0.6f), 0.06f);
        }

        // delapan variasi spec dipakai ulang supaya mesh NpcKit ter-cache (ratusan peziarah tetap ringan)
        static NpcKit.Spec[] pilSpecs;
        static NpcKit.Spec PilgrimSpec(int i)
        {
            if (pilSpecs == null)
            {
                pilSpecs = new NpcKit.Spec[8];
                for (int k = 0; k < 8; k++)
                {
                    var sp = NpcKit.RandomSpec(NpcKit.OPengembara, new System.Random(900 + k), k % 4 == 3 ? NpcKit.Elf : NpcKit.Human, k % 2);
                    sp.hat = 5; sp.prop = 0; sp.cA = k % 3 == 0 ? 8 : 10; sp.cB = 10; sp.cC = k % 2 == 0 ? 7 : 8;   // tudung gelap, jubah kelabu/ungu tua
                    sp.name = "Peziarah";
                    pilSpecs[k] = sp;
                }
            }
            return pilSpecs[i % 8];
        }

        static void MakeImp(Vector3 p, Color c, string nm)
        {
            var sp = NpcKit.RandomSpec(NpcKit.OPelayan, new System.Random(nm.Length * 13), NpcKit.Dwarf, 0);
            sp.hat = 0; sp.prop = 0; sp.cA = 4; sp.cB = 1; sp.cC = 10; sp.hairCol = c.r > 0.6f ? 5 : 6; sp.skin = 1; sp.name = nm;
            var g = NpcKit.Spawn(root.transform, nm, p, 0f, sp, false, false);
            g.transform.localScale = Vector3.one * 0.75f;
            var rig = g.GetComponent<CharacterRig>();
            Transform hd = rig != null && rig.head != null ? rig.head : g.transform;
            var horn = new Color(0.12f, 0.05f, 0.05f);
            for (int i = -1; i <= 1; i += 2)
            {
                var h = P(PrimitiveType.Cylinder, hd, new Vector3(i * 0.09f, 0.2f, 0.02f), new Vector3(0.035f, 0.07f, 0.035f), horn);
                h.transform.localRotation = Quaternion.Euler(0f, 0f, -i * 25f);
                var tip = P(PrimitiveType.Sphere, hd, new Vector3(i * 0.135f, 0.275f, 0.02f), Vector3.one * 0.04f, horn);
            }
            // ekor beruncing + celemek karung
            P(PrimitiveType.Cylinder, g.transform, new Vector3(0f, 0.5f, -0.22f), new Vector3(0.04f, 0.22f, 0.04f), c).transform.localRotation = Quaternion.Euler(-50f, 0f, 0f);
            P(PrimitiveType.Sphere, g.transform, new Vector3(0f, 0.28f, -0.5f), Vector3.one * 0.1f, c);
            P(PrimitiveType.Cube, g.transform, new Vector3(0f, 0.75f, 0.2f), new Vector3(0.38f, 0.6f, 0.03f), new Color(0.35f, 0.25f, 0.15f));
            Text(g.transform, nm, new Vector3(0f, 1.75f, 0f), new Color(1f, 0.8f, 0.7f), 0.06f);
            var col = g.AddComponent<CapsuleCollider>(); col.center = new Vector3(0f, 0.7f, 0f); col.radius = 0.4f; col.height = 1.4f; col.isTrigger = true;
            g.AddComponent<ImpChat>();
        }

        // ------------------------------------------------------------------ tiap frame
        public static void Tick()
        {
            if (root == null) return;
            var pc = PlayerCombat.I; if (pc == null) return;
            float dt = Time.unscaledDeltaTime;
            Vector3 pp = pc.transform.position;
            speed = Mathf.Lerp(speed, (new Vector3(pp.x - lastPos.x, 0f, pp.z - lastPos.z)).magnitude / Mathf.Max(0.001f, dt), 0.2f); lastPos = pp;
            if (alarmCd > 0f) alarmCd -= dt;
            if (hasAltar && !Cutscene.Active)
            {
                float d = new Vector2(pp.x - altarPos.x, pp.z - altarPos.z).magnitude;
                if (d < 26f && speed > 3.4f) alert += dt * (d < 14f ? 1.6f : 1f); else alert = Mathf.Max(0f, alert - dt * 0.7f);
                if (d < 26f && d > 8f && alert > 0.5f && ((int)(alert * 4f) % 4 == 0)) { }
                if (alert > 2.5f && alarmCd <= 0f) { Alarm(pp, "Telinga mendengar langkahmu yang tergesa!"); alert = 0f; alarmCd = 80f; }
            }
            if (scene == "HuntArea_Demon") WatchZargoth();
        }

        public static void Alarm(Vector3 around, string msg)
        {
            GameState.Say(msg);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f + Random.value; var p = around + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * 9f; p.y = RT.GroundY(p);
                var go = new GameObject("PemujaPanggilan"); go.transform.position = p;
                var m = go.AddComponent<Monster>(); m.defId = "dark_cultist"; m.noLeash = true; m.Alert();
            }
        }

        // ------------------------------------------------------------------ Dagan melepas api
        static bool fireBusy;
        static void WatchZargoth()
        {
            var g = GameState.I; if (g == null || fireBusy || !StoryCompanion.Joined || StoryCompanion.Gone || g.flags.Contains("cs_dagan_fire") || Cutscene.Active) return;
            foreach (var m in Monster.All)
            {
                if (m == null || m.IsDead || m.DefId != "demon_lord") continue;
                if (m.MaxHp > 1f && m.hp / m.MaxHp < 0.5f) { g.flags.Add("cs_dagan_fire"); fireBusy = true; DaganFire(m); }
                return;
            }
        }

        static void DaganFire(Monster boss)
        {
            Vector3 c = boss.transform.position;
            var ghost = StoryCutscenes.Ghost.Make(c + new Vector3(-9f, 0f, -6f), new Color(1f, 0.5f, 0.12f), 1f);
            ghost.fadeStart = 99f;
            var l = new List<Cutscene.Shot>
            {
                Cutscene.Orbit(c, 14f, 11f, 4f, 3.4f, 210f, 250f, 3.2f, "Dagan naik ke sisi Ari. Api yang selama ini ia tahan, ia lepaskan sekarang.", 2f),
                Cutscene.Orbit(c, 9f, 7f, 3.2f, 3f, 250f, 280f, 2.8f, "\"Selesai... hampir.\"", 2f),
            };
            Cutscene.Play(l, () =>
            {
                if (ghost != null) ghost.Kill();
                if (boss != null && !boss.IsDead)
                {
                    boss.TakeDamage(boss.MaxHp * 0.18f, false);
                    RunSlow(boss);
                }
                GameState.Say("Dagan melepas apinya! Zargoth terguncang.");
                fireBusy = false;
            });
        }

        static void RunSlow(Monster m)
        {
            var host = root != null ? root.AddComponent<SlowHost>() : null;
            if (host != null) host.Run(m, 0.15f, 5f);
        }

        class SlowHost : MonoBehaviour
        {
            public void Run(Monster m, float mul, float dur) { StartCoroutine(Co(m, mul, dur)); }
            IEnumerator Co(Monster m, float mul, float dur)
            {
                if (m != null) m.speedMult = mul;
                float t = 0f; while (t < dur) { t += Time.deltaTime; yield return null; }
                if (m != null) m.speedMult = 1f;
            }
        }

        // ------------------------------------------------------------------ komponen kecil
        class PilgrimSway : MonoBehaviour
        {
            float ph; Vector3 b;
            void Start() { ph = Random.value * 10f; b = transform.localScale; }
            void Update() { transform.localScale = new Vector3(b.x, b.y * (0.96f + 0.04f * Mathf.Sin(Time.time * 0.8f + ph)), b.z); }
        }

        class ImpChat : Interactable
        {
            static readonly string[] Lines = { "Jangan pegang sendok Nyonya Varra. Aku pernah. Sekali.", "Supnya enak. Tapi jangan bilang enak, nanti dia curiga.", "Dia galak, tapi dia yang menjahit celemekku.", "Aku bukan jahat. Aku cuma bertanduk.", "Seribu tahun, dan dia belum pernah lupa menaruh garam." };
            Vector3 b; float ph;
            protected override void OnEnable() { base.OnEnable(); prompt = "Ajak bicara"; radius = 3f; b = transform.position; ph = Random.value * 6f; }
            void Update() { transform.position = b + new Vector3(Mathf.Sin(Time.time * 0.6f + ph) * 0.8f, Mathf.Abs(Mathf.Sin(Time.time * 2f + ph)) * 0.12f, Mathf.Cos(Time.time * 0.5f + ph) * 0.8f); }
            public override void Interact() { GameState.Say(name + ": \"" + Lines[Random.Range(0, Lines.Length)] + "\""); }
        }
    }

    /// <summary>Dapur Varra: sup bawang memulihkan HP penuh dan memberi buff serangan. Sekali per hari permainan.</summary>
    public class VarraKitchen : Interactable
    {
        protected override void OnEnable() { base.OnEnable(); prompt = "Makan sup bawang"; radius = 3.8f; }

        public override void Interact()
        {
            var ui = TownUI.I; var g = GameState.I; var pc = PlayerCombat.I; if (ui == null || g == null || pc == null) return;
            string key = "varra_sup_day"; int today = GameCalendar.Day;
            bool eaten = g.Stat(key) == today;
            ui.Open("Dapur Varra", () => eaten ? "Varra: \"Sudah kuberi satu mangkuk hari ini. Besok, nak. Aku tidak mau ada yang kembung di depan Zargoth.\"" : "Varra mengaduk kuali tanpa menoleh.\n\"Duduk. Makan pelan-pelan. Seribu tahun kuajarkan hal ini ke orang yang tidak mau mendengar.\"", () =>
            {
                var l = new List<UIEntry>();
                if (!eaten) l.Add(new UIEntry("Makan sup bawang  (HP pulih penuh, ATK +15% selama 10 menit)", () =>
                {
                    g.stats[key] = today;
                    pc.Heal(pc.MaxHp); pc.Buff(1.15f, 600f);
                    Audio.Sfx("quest_reward");
                    GameState.Say("Sup bawang. Hampir seperti buatan Basra. HP pulih, ATK +15%.");
                    ui.Close();
                }));
                else l.Add(new UIEntry("(sudah makan hari ini)", () => { }, false));
                l.Add(new UIEntry("Tutup", () => ui.Close()));
                return l;
            });
        }
    }
}
