using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Mekanik boss unik (menempel otomatis ke Monster): fase berdasarkan HP, serangan area bertelegraf (lingkaran/garis merah di tanah),
    /// pemanggilan pasukan, perisai yang dipecahkan dengan membunuh pasukan, dan serangan khas tiap boss.
    ///   Grumak (ogre_chief): hantaman tanah, terjangan, panggil bandit/ogre.
    ///   Ignarok (ignarok): kolam lava, letusan, hujan meteor.
    ///   Malrek (bone_king): tombak tulang, pasukan tulang + perisai, jeritan kematian.
    ///   Zargoth (demon_lord): api neraka, teleportasi, rantai penarik, gerbang neraka (perisai), mengamuk.
    /// Semua angka placeholder.
    /// </summary>
    public class BossBrain : MonoBehaviour
    {
        static readonly HashSet<string> Ids = new HashSet<string> { "ogre_chief", "ignarok", "bone_king", "demon_lord", "behemoth", "cerberus" };
        public static bool Has(string id) { return id != null && (Ids.Contains(id) || IsGeneric(id)); }

        // v35z102n14: boss Menara (tbs_10..100), Arena/Gladiator, dan S-Rank kini juga punya fase + serangan bertelegraf (set gerakan per boss).
        static bool IsGeneric(string id) { return id != null && (id.StartsWith("tbs_") || id == "arena_warden" || id == "gladiator_elite" || id.StartsWith("srank_") || id.StartsWith("hb_")); }

        static readonly Dictionary<string, string[]> Moves = new Dictionary<string, string[]>
        {
            { "tbs_10", new[] { "slam", "charge", "slam", "charge" } },
            { "tbs_20", new[] { "charge", "blink", "charge", "slam" } },
            { "tbs_30", new[] { "spear", "slam", "charge", "spear" } },
            { "tbs_40", new[] { "pool", "chains", "slam", "pool" } },
            { "tbs_50", new[] { "slam", "charge", "meteor", "slam" } },
            { "tbs_60", new[] { "pool", "charge", "blink", "pool" } },
            { "tbs_70", new[] { "slam", "pool", "meteor", "slam" } },
            { "tbs_80", new[] { "pool", "blink", "chains", "meteor" } },
            { "tbs_90", new[] { "spear", "slam", "chains", "spear" } },
            { "tbs_100", new[] { "slam", "pool", "blink", "meteor" } },
            { "arena_warden", new[] { "charge", "slam", "spear", "charge" } },
            { "gladiator_elite", new[] { "charge", "slam", "spear", "slam" } },
            { "srank_blade", new[] { "blink", "spear", "charge", "blink" } },
            { "srank_shadow", new[] { "blink", "pool", "chains", "spear" } },
            { "srank_ash", new[] { "pool", "slam", "meteor", "pool" } },
            { "hb_lerai", new[] { "blink", "charge", "blink", "slam" } },
            { "hb_frontier", new[] { "slam", "pool", "slam", "charge" } },
            { "hb_desert", new[] { "spear", "charge", "pool", "spear" } },
            { "hb_swamp", new[] { "pool", "chains", "pool", "blink" } },
            { "hb_volcano", new[] { "meteor", "slam", "pool", "meteor" } },
            { "hb_bone", new[] { "chains", "spear", "blink", "chains" } },
            { "hb_demon", new[] { "blink", "meteor", "chains", "pool" } },
        };
        static readonly Dictionary<string, string> Escort = new Dictionary<string, string>
        {
            { "tbs_10", "goblin" }, { "tbs_20", "alpha_wolf" }, { "tbs_30", "highway_bandit" }, { "tbs_40", "bone_soldier" }, { "tbs_50", "ogre" },
            { "tbs_70", "ember_imp" }, { "tbs_80", "imp" }, { "tbs_90", "bone_soldier" }, { "tbs_100", "demon_brute" },
        };

        public static void AttachTo(Monster mo)
        {
            if (mo != null && Has(mo.DefId) && mo.GetComponent<BossBrain>() == null) mo.gameObject.AddComponent<BossBrain>();
        }

        public int Phase { get { return phase; } }
        public int PhaseCount { get { return id == "demon_lord" ? 4 : 3; } }
        Monster m;
        PlayerCombat pc;
        string id;
        int phase = -1;
        float tA, tB, tC, tD;
        bool shield;
        readonly List<Monster> minions = new List<Monster>();

        float[] gt = new float[4];
        void Start() { m = GetComponent<Monster>(); id = m != null ? m.DefId : ""; for (int i = 0; i < 4; i++) gt[i] = Time.time + 6f + 3f * i; tA = Time.time + 6f; tB = Time.time + 10f; tC = Time.time + 14f; tD = Time.time + 20f; }

        static float Flat(Vector3 a, Vector3 b) { a.y = 0; b.y = 0; return Vector3.Distance(a, b); }

        int PhaseFor(float r)
        {
            if (IsGeneric(id)) return r > 0.66f ? 0 : r > 0.33f ? 1 : 2;
            switch (id)
            {
                case "ogre_chief": return r > 0.66f ? 0 : r > 0.33f ? 1 : 2;
                case "behemoth": case "cerberus": return r > 0.66f ? 0 : r > 0.33f ? 1 : 2;
                case "ignarok": return r > 0.6f ? 0 : r > 0.3f ? 1 : 2;
                case "bone_king": return r > 0.6f ? 0 : r > 0.3f ? 1 : 2;
                default: return r > 0.7f ? 0 : r > 0.4f ? 1 : r > 0.15f ? 2 : 3;
            }
        }

        void Update()
        {
            if (m == null || m.IsDead)
            {
                foreach (var x in minions) if (x != null && !x.IsDead) x.TakeDamage(99999f, false);       // pasukan lenyap bersama bosnya
                minions.Clear();
                enabled = false;
                return;
            }
            pc = PlayerCombat.I;
            if (pc == null || pc.IsDead) return;
            float d = Flat(pc.transform.position, transform.position);
            if (d > 60f && !m.InCombat) return;
            int ph = PhaseFor(m.hp / m.MaxHp);
            if (ph != phase) { phase = ph; EnterPhase(ph); }
            minions.RemoveAll(x => x == null || x.IsDead);
            if (shield && minions.Count == 0)
            {
                shield = false; m.damageTakenMult = 1f;
                Say("Perisai " + m.Name + " pecah! Serang sekarang!");
                m.Stagger(3.5f);
                CombatFx.ShockRing(transform.position + Vector3.up * 0.1f, 9f, new Color(1f, 0.9f, 0.4f), 0.5f);
            }
            if (!m.InCombat) return;
            Tick();
        }

        static void Say(string s) { if (TownUI.I != null) TownUI.I.Toast(s); }

        // ------------------------------------------------------------------ fase
        void EnterPhase(int ph)
        {
            if (ph == 0) return;
            Audio.Sfx("boss_roar", transform.position);
            CombatFx.ShockRing(transform.position + Vector3.up * 0.1f, 12f, new Color(1f, 0.3f, 0.2f), 0.6f);
            Combat2.OnBossPhase(m, ph);
            if (IsGeneric(id)) { GenericPhase(ph); return; }
            switch (id)
            {
                case "ogre_chief":
                    if (ph == 1) { Say("Grumak meraung memanggil anak buahnya!"); Summon("highway_bandit", 3, 9f); }
                    else { Say("Grumak mengamuk!"); m.atkMult *= 1.25f; m.speedMult *= 1.2f; Summon("ogre", 2, 9f); }
                    break;
                case "behemoth":
                    if (ph == 1) { Say("Behemoth menghentak bumi, kawanan ogre berdatangan!"); Summon("ogre", 2, 10f); }
                    else { Say("Behemoth mengamuk menyeruduk membabi buta!"); m.atkMult *= 1.3f; m.speedMult *= 1.2f; }
                    break;
                case "cerberus":
                    if (ph == 1) { Say("Tiga kepala Cerberus melolong, anjing neraka menyerbu!"); Summon("hellhound", 3, 10f); }
                    else { Say("Mata Cerberus menyala, api neraka meledak!"); m.atkMult *= 1.3f; m.speedMult *= 1.2f; }
                    break;
                case "ignarok":
                    if (ph == 1) { Say("Gunung bergemuruh! Ignarok memanggil imp bara!"); Summon("ember_imp", 4, 10f); m.atkMult *= 1.15f; }
                    else { Say("Ignarok membakar langit: hujan meteor!"); m.atkMult *= 1.2f; m.speedMult *= 1.2f; }
                    break;
                case "bone_king":
                    if (ph == 1)
                    {
                        Say("Malrek memanggil pasukannya! Hancurkan mereka untuk memecah perisai.");
                        Summon("bone_soldier", 4, 10f); Summon("bone_archer", 2, 14f);
                        shield = true; m.damageTakenMult = 0.15f;
                    }
                    else { Say("Malrek murka: jeritan kematian mengguncang makam!"); m.atkMult *= 1.3f; }
                    break;
                default:
                    if (ph == 1) { Say("Zargoth tertawa. Imp berdatangan!"); Summon("imp", 3, 10f); }
                    else if (ph == 2)
                    {
                        Say("Gerbang Neraka terbuka! Kalahkan brute untuk memecah perisai Zargoth.");
                        Summon("demon_brute", 2, 11f);
                        shield = true; m.damageTakenMult = 0.15f;
                    }
                    else { Say("Zargoth mengamuk dengan seluruh kekuatannya!"); m.atkMult *= 1.4f; m.speedMult *= 1.3f; }
                    break;
            }
        }

        void Summon(string mid, int n, float r)
        {
            if (MonsterDB.Get(mid) == null) return;
            for (int i = 0; i < n; i++)
            {
                float a = (i * 360f / n + Random.Range(0f, 40f)) * Mathf.Deg2Rad;
                Vector3 p = transform.position + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * r;
                p.y = RT.GroundY(p);
                var go = new GameObject("Minion_" + mid);
                go.transform.position = p;
                var mo = go.AddComponent<Monster>();
                mo.defId = mid;
                mo.noLeash = true;
                minions.Add(mo);
                CombatFx.Burst(p + Vector3.up, new Color(1f, 0.5f, 0.3f), 10, 0.6f, 2f);
            }
            StartCoroutine(AlertLater());
        }

        IEnumerator AlertLater()
        {
            yield return new WaitForSeconds(0.5f);
            foreach (var x in minions) if (x != null && !x.IsDead) x.Alert();
        }

        // ------------------------------------------------------------------ serangan per boss
        void Tick()
        {
            float now = Time.time;
            float atk = m.Atk;
            Vector3 pp = pc.transform.position;
            if (IsGeneric(id)) { GenericTick(now, atk, pp); return; }
            switch (id)
            {
                case "ogre_chief":
                    if (now >= tA) { tA = now + (phase == 0 ? 10f : phase == 1 ? 8f : 6f); Slam(transform.position, 8f, 1.3f, atk * 1.3f, CombatFx.EarthCol); }
                    if (phase >= 1 && now >= tB) { tB = now + 13f; Charge(atk * 1.2f); }
                    break;
                case "behemoth":
                    if (now >= tA) { tA = now + (phase == 0 ? 9f : 7f); Slam(transform.position, 9f, 1.2f, atk * 1.3f, CombatFx.EarthCol); }
                    if (now >= tB) { tB = now + (phase >= 2 ? 8f : 12f); Charge(atk * 1.4f); }
                    break;
                case "cerberus":
                    if (now >= tA) { tA = now + 8f; for (int i = 0; i < 3; i++) Pool(pp + Random.insideUnitSphere * 5f, 4f, 1.4f, 6f, atk * 0.25f, CombatFx.MagmaCol); }
                    if (phase >= 1 && now >= tB) { tB = now + 12f; Charge(atk * 1.3f); }
                    if (phase >= 2 && now >= tC) { tC = now + 12f; Slam(transform.position, 13f, 1.8f, atk * 1.5f, new Color(0.9f, 0.3f, 0.2f)); }
                    break;
                case "ignarok":
                    if (now >= tA) { tA = now + 9f; for (int i = 0; i < 3; i++) Pool(pp + Random.insideUnitSphere * 5f, 4f, 1.5f, 6f, atk * 0.25f, CombatFx.MagmaCol); }
                    if (phase >= 1 && now >= tB) { tB = now + 14f; Slam(transform.position, 13f, 2.2f, atk * 1.6f, CombatFx.MagmaCol); Say("Ignarok bersiap meletus! Menjauh!"); }
                    if (phase >= 2 && now >= tC) { tC = now + 12f; StartCoroutine(Meteors(pp, atk * 1.1f)); }
                    break;
                case "bone_king":
                    if (now >= tA) { tA = now + 8f; Spear(atk * 1.2f); }
                    if (!shield && now >= tB) { tB = now + 30f; if (minions.Count < 4) Summon("bone_soldier", 3, 10f); }
                    if (phase >= 2 && now >= tC) { tC = now + 12f; Slam(transform.position, 15f, 2f, atk * 1.4f, new Color(0.6f, 0.8f, 1f)); Say("Malrek menjerit!"); }
                    break;
                default:
                    if (now >= tA) { tA = now + 8f; for (int i = 0; i < 3; i++) Pool(pp + Random.insideUnitSphere * 6f, 4f, 1.4f, 6f, atk * 0.25f, new Color(0.8f, 0.2f, 0.9f)); }
                    if (now >= tB) { tB = now + 13f; Blink(atk * 1.2f); }
                    if (phase >= 1 && now >= tC) { tC = now + 15f; Chains(atk * 0.6f); }
                    if (phase >= 3 && now >= tD) { tD = now + 9f; Slam(transform.position, 14f, 1.6f, atk * 1.4f, new Color(0.9f, 0.2f, 0.4f)); }
                    break;
            }
        }

        // ------------------------------------------------------------------ boss generik (Menara / Arena / S-Rank)
        void GenericPhase(int ph)
        {
            string esc; Escort.TryGetValue(id, out esc);
            if (ph == 1)
            {
                Say(m.Name + " mulai serius! Pola serangannya bertambah.");
                if (esc != null) Summon(esc, 2, 9f);
            }
            else
            {
                Say(m.Name + " mengamuk!");
                m.atkMult *= 1.25f; m.speedMult *= 1.15f;
                if (esc != null) Summon(esc, 3, 10f);
            }
        }

        void GenericTick(float now, float atk, Vector3 pp)
        {
            string[] mv; if (!Moves.TryGetValue(id, out mv)) mv = new[] { "slam", "charge", "slam", "charge" };
            float f = id.StartsWith("tbs_") ? 1f : 0.85f;          // boss ujian sedikit lebih lunak daripada boss Menara
            for (int i = 0; i < 4; i++)
            {
                if (i == 2 && phase < 1) continue;                  // gerakan ke-3 mulai di fase 2
                if (i == 3 && phase < 2) continue;                  // gerakan ke-4 mulai di fase 3
                if (now < gt[i]) continue;
                gt[i] = now + (9f + 2f * i) / (1f + 0.25f * phase);
                switch (mv[i])
                {
                    case "slam": Slam(transform.position, 8f + 2f * phase, 1.3f, atk * 1.3f * f, CombatFx.EarthCol); break;
                    case "charge": Charge(atk * 1.3f * f); break;
                    case "spear": Spear(atk * 1.2f * f); break;
                    case "blink": Blink(atk * 1.2f * f); break;
                    case "chains": Chains(atk * 0.6f * f); break;
                    case "meteor": StartCoroutine(Meteors(pp, atk * 1.0f * f)); break;
                    case "pool": for (int k = 0; k < 3; k++) Pool(pp + Random.insideUnitSphere * 5f, 4f, 1.4f, 6f, atk * 0.25f * f, new Color(0.6f, 0.9f, 0.3f)); break;
                }
                break;                                              // satu gerakan per tick agar tidak menumpuk
            }
        }

        // ------------------------------------------------------------------ bangunan serangan
        GameObject Disk(Vector3 pos, float r, Color c)
        {
            pos.y = RT.GroundY(pos) + 0.1f;
            c.a = 1f;
            var go = RT.Prim(PrimitiveType.Cylinder, null, pos, new Vector3(r * 2f, 0.03f, r * 2f), Color.Lerp(c, Color.red, 0.5f));
            return go;
        }

        void HitDisk(Vector3 center, float r, float dmg)
        {
            if (pc != null && !pc.IsDead && Flat(pc.transform.position, center) <= r + 0.4f) pc.TakeDamage(dmg, center);
            CoopNet.HitRemotesInDisk(center, r, dmg);
        }

        void Slam(Vector3 center, float r, float tele, float dmg, Color col) { StartCoroutine(SlamCo(center, r, tele, dmg, col)); }

        IEnumerator SlamCo(Vector3 center, float r, float tele, float dmg, Color col)
        {
            var disk = Disk(center, r, col);
            Audio.Sfx("monster_growl", center);
            yield return new WaitForSeconds(tele);
            if (disk != null) Destroy(disk);
            CombatFx.ShockRing(center + Vector3.up * 0.1f, r, col, 0.4f);
            CombatFx.Dust(center, 16);
            CombatFx.Flash(center + Vector3.up, col, r * 1.4f, 3f, 0.25f);
            Audio.Sfx("hit_heavy", center);
            HitDisk(center, r, dmg);
        }

        void Pool(Vector3 center, float r, float tele, float life, float dps, Color col) { StartCoroutine(PoolCo(center, r, tele, life, dps, col)); }

        IEnumerator PoolCo(Vector3 center, float r, float tele, float life, float dmg, Color col)
        {
            var disk = Disk(center, r, col);
            yield return new WaitForSeconds(tele);
            if (disk != null) disk.GetComponent<Renderer>().material.color = col;
            float t = 0f;
            while (t < life)
            {
                CombatFx.Burst(center + Vector3.up * 0.3f, col, 3, r * 0.5f, 1.2f, 0.5f, 0.2f);
                HitDisk(center, r, dmg);
                yield return new WaitForSeconds(0.5f);
                t += 0.5f;
            }
            if (disk != null) Destroy(disk);
        }

        IEnumerator Meteors(Vector3 around, float dmg)
        {
            Say("Meteor jatuh!");
            for (int i = 0; i < 6; i++)
            {
                Vector2 o = Random.insideUnitCircle * 14f;
                Vector3 c = (i % 2 == 0 && pc != null ? pc.transform.position : around) + new Vector3(o.x, 0f, o.y);
                Slam(c, 4.5f, 1.4f, dmg, CombatFx.MagmaCol);
                yield return new WaitForSeconds(0.35f);
            }
        }

        void Charge(float dmg) { StartCoroutine(ChargeCo(dmg)); }

        IEnumerator ChargeCo(float dmg)
        {
            Vector3 target = pc.transform.position;
            var disk = Disk(target, 4f, CombatFx.EarthCol);
            Face(target);
            yield return new WaitForSeconds(1.2f);
            if (disk != null) Destroy(disk);
            if (m == null || m.IsDead) yield break;
            Vector3 from = transform.position, to = target;
            for (float t = 0f; t < 0.35f; t += Time.deltaTime)
            {
                transform.position = Vector3.Lerp(from, to, t / 0.35f);
                yield return null;
            }
            CombatFx.ShockRing(transform.position + Vector3.up * 0.1f, 5f, CombatFx.EarthCol, 0.35f);
            CombatFx.Dust(transform.position, 14);
            HitDisk(transform.position, 5f, dmg);
        }

        void Spear(float dmg) { StartCoroutine(SpearCo(dmg)); }

        IEnumerator SpearCo(float dmg)
        {
            Vector3 a = transform.position;
            Vector3 dir = pc.transform.position - a; dir.y = 0f; dir.Normalize();
            float len = 32f;
            Vector3 b = a + dir * len;
            var go = RT.Prim(PrimitiveType.Cube, null, a + dir * len * 0.5f + Vector3.up * 0.15f, new Vector3(3f, 0.04f, len), new Color(0.8f, 0.2f, 0.2f));
            go.transform.rotation = Quaternion.LookRotation(dir);
            yield return new WaitForSeconds(1.2f);
            Destroy(go);
            for (int i = 1; i <= 8; i++) CombatFx.Burst(a + dir * (len / 8f * i), new Color(0.9f, 0.92f, 0.8f), 6, 0.5f, 3f, 0.5f, 0.2f);
            Audio.Sfx("hit_heavy", a);
            if (pc != null && !pc.IsDead)
            {
                Vector3 p = pc.transform.position - a; p.y = 0f;
                float along = Vector3.Dot(p, dir);
                float side = Mathf.Abs(Vector3.Cross(dir, p).y);
                if (along >= 0f && along <= len && side <= 2.2f) pc.TakeDamage(dmg, a);
            }
        }

        void Blink(float dmg) { StartCoroutine(BlinkCo(dmg)); }

        IEnumerator BlinkCo(float dmg)
        {
            Vector3 behind = pc.transform.position - pc.transform.forward * 4f;
            var disk = Disk(behind, 6f, new Color(0.8f, 0.2f, 0.9f));
            yield return new WaitForSeconds(1.0f);
            if (disk != null) Destroy(disk);
            if (m == null || m.IsDead) yield break;
            CombatFx.Burst(transform.position + Vector3.up, new Color(0.8f, 0.2f, 0.9f), 20, 1f, 2f);
            behind.y = RT.GroundY(behind);
            transform.position = behind;
            CombatFx.ShockRing(behind + Vector3.up * 0.1f, 6f, new Color(0.8f, 0.2f, 0.9f), 0.35f);
            Audio.Sfx("hit_heavy", behind);
            HitDisk(behind, 6f, dmg);
        }

        void Chains(float dmg) { StartCoroutine(ChainsCo(dmg)); }

        IEnumerator ChainsCo(float dmg)
        {
            Say("Rantai Iblis! Hindari atau kamu akan ditarik.");
            var line = RT.Prim(PrimitiveType.Cube, null, transform.position, new Vector3(0.3f, 0.05f, 1f), new Color(0.7f, 0.1f, 0.8f));
            float t = 0f;
            while (t < 1.0f)
            {
                if (pc != null)
                {
                    Vector3 a = transform.position + Vector3.up * 0.2f, b = pc.transform.position + Vector3.up * 0.2f;
                    line.transform.position = (a + b) / 2f; line.transform.localScale = new Vector3(0.3f, 0.05f, Mathf.Max(0.1f, Vector3.Distance(a, b)));
                    if (b != a) line.transform.rotation = Quaternion.LookRotation((b - a).normalized);
                }
                t += Time.deltaTime; yield return null;
            }
            Destroy(line);
            if (pc == null || pc.IsDead || Flat(pc.transform.position, transform.position) > 32f) yield break;
            if (pc.Invulnerable) yield break;                 // dodge menghindari rantai
            var cc = pc.GetComponent<CharacterController>();
            Vector3 dir = transform.position - pc.transform.position; dir.y = 0f;
            float dist = Mathf.Max(0f, dir.magnitude - 3f);
            dir.Normalize();
            for (float k = 0f; k < 0.5f && cc != null; k += Time.deltaTime)
            {
                cc.Move(dir * (dist / 0.5f) * Time.deltaTime);
                yield return null;
            }
            pc.TakeDamage(dmg, transform.position);
        }

        void Face(Vector3 target)
        {
            Vector3 d = target - transform.position; d.y = 0f;
            if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
        }
    }
}
