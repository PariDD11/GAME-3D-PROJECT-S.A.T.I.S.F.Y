using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// Partner hasil rekrutan di Papan Rekrutmen: ikut ke area hunt dan Menara, mengikuti pemain, dan bertarung sesuai role.
    /// Tank menarik perhatian monster, DPS menyerang (Ranged dari jauh), Support menyembuhkan.
    /// Statistik diambil dari Partner (hp, atk, def, spd) + bonus familiarity.
    /// </summary>
    public class AllyUnit : MonoBehaviour
    {
        public static readonly List<AllyUnit> All = new List<AllyUnit>();

        public Partner partner;
        public int slot;
        public float hp, maxHp;
        public bool Down { get; private set; }

        float attackReady, downUntil, healReady, nextChat;
        float nextBark, bubbleUntil, worryAt, calmAt; bool engaged, lowSaid; Transform bubble; TextMesh bubbleTm;
        Transform body, label;
        Renderer[] rends;
        Color[] baseCols;
        float flash;

        public string Role { get { return partner != null ? partner.role : "DPS Melee"; } }
        public float TauntWeight { get { return Role == "Tank" ? 0.5f : Role == "Support" ? 1.25f : 1f; } }
        bool Ranged { get { return Role == "DPS Ranged"; } }
        bool IsSupport { get { return Role == "Support"; } }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        /// <summary>Partner hidup terdekat (untuk target monster). dist = jarak datar.</summary>
        public static AllyUnit Nearest(Vector3 from, float maxDist, out float dist)
        {
            AllyUnit best = null;
            dist = float.MaxValue;
            foreach (var a in All)
            {
                if (a.Down) continue;
                Vector3 d = a.transform.position - from; d.y = 0f;
                float m = d.magnitude;
                if (m < dist && m <= maxDist) { dist = m; best = a; }
            }
            return best;
        }

        public void Init(Partner p, int index)
        {
            partner = p;
            slot = index;
            float fam = 1f + (GuildData.I != null ? GuildData.I.FamiliarityBonusPct(p.key) / 100f : 0f);
            maxHp = Mathf.Max(40f, p.hp * fam * PartnerPersona.HpMul(p.key));
            hp = maxHp;
            name = "Ally_" + p.name;
            BuildVisual();
        }

        void BuildVisual()
        {
            Color c = PartnerFactory.RoleColor(partner.role);
            Color skin = new Color(0.82f, 0.66f, 0.54f);
            Color dark = c * 0.55f; dark.a = 1f;
            body = new GameObject("Body").transform;
            body.SetParent(transform, false);
            RT.Prim(PrimitiveType.Capsule, body, new Vector3(0f, 1.0f, 0f), new Vector3(0.7f, 0.8f, 0.5f), c);
            RT.Prim(PrimitiveType.Sphere, body, new Vector3(0f, 1.95f, 0f), new Vector3(0.36f, 0.38f, 0.36f), skin);
            RT.Prim(PrimitiveType.Cylinder, body, new Vector3(-0.17f, 0.35f, 0f), new Vector3(0.2f, 0.35f, 0.2f), dark);
            RT.Prim(PrimitiveType.Cylinder, body, new Vector3(0.17f, 0.35f, 0f), new Vector3(0.2f, 0.35f, 0.2f), dark);
            switch (partner.role)
            {
                case "Tank":
                    RT.Prim(PrimitiveType.Cylinder, body, new Vector3(-0.55f, 1.2f, 0.15f), new Vector3(0.7f, 0.05f, 0.7f), new Color(0.6f, 0.62f, 0.68f)).transform.localRotation = Quaternion.Euler(90, 0, 0);
                    RT.Prim(PrimitiveType.Sphere, body, new Vector3(0f, 2.05f, 0f), new Vector3(0.42f, 0.3f, 0.42f), new Color(0.6f, 0.62f, 0.68f));
                    break;
                case "DPS Melee":
                    RT.Prim(PrimitiveType.Cube, body, new Vector3(0.55f, 1.3f, 0.45f), new Vector3(0.07f, 0.06f, 1.2f), new Color(0.75f, 0.77f, 0.8f));
                    break;
                case "DPS Ranged":
                    RT.Prim(PrimitiveType.Cube, body, new Vector3(-0.5f, 1.3f, 0.3f), new Vector3(0.05f, 1.1f, 0.05f), new Color(0.35f, 0.22f, 0.12f));
                    break;
                default:
                    RT.Prim(PrimitiveType.Cube, body, new Vector3(0.5f, 1.2f, 0.2f), new Vector3(0.06f, 1.5f, 0.06f), new Color(0.85f, 0.75f, 0.35f));
                    RT.Prim(PrimitiveType.Sphere, body, new Vector3(0.5f, 2.0f, 0.2f), new Vector3(0.2f, 0.2f, 0.2f), new Color(1f, 0.95f, 0.6f));
                    break;
            }
            var lab = new GameObject("Label");
            lab.transform.SetParent(transform, false);
            lab.transform.localPosition = new Vector3(0f, 2.55f, 0f);
            var tm = lab.AddComponent<TextMesh>();
            tm.text = partner.name + "\n<" + partner.role + ">";
            tm.fontSize = 48; tm.characterSize = 0.045f;
            tm.anchor = TextAnchor.LowerCenter; tm.alignment = TextAlignment.Center;
            tm.color = new Color(0.7f, 1f, 0.75f);
            tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            HunterGuild.TextMat.Apply(lab.GetComponent<MeshRenderer>(), tm.font.material);
            label = lab.transform;
            rends = body.GetComponentsInChildren<Renderer>();
            baseCols = new Color[rends.Length];
            for (int i = 0; i < rends.Length; i++) baseCols[i] = rends[i].material.color;
        }

        static float GroundAt(Vector3 p)
        {
            RaycastHit hit;
            if (Physics.Raycast(p + Vector3.up * 40f, Vector3.down, out hit, 120f, ~0, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<Monster>() == null)
                return hit.point.y;
            return RT.GroundY(p);
        }

        void Snap()
        {
            Vector3 p = transform.position;
            p.y = Mathf.Lerp(p.y, GroundAt(p), 0.5f);
            transform.position = p;
        }

        void Update()
        {
            if (partner == null) return;
            var pc = PlayerCombat.I;
            if (pc == null) return;

            if (label != null && Camera.main != null)
                label.rotation = Quaternion.LookRotation(label.position - Camera.main.transform.position);
            if (bubble != null)
            {
                if (Time.time > bubbleUntil) bubble.gameObject.SetActive(false);
                else if (Camera.main != null) bubble.rotation = Quaternion.LookRotation(bubble.position - Camera.main.transform.position);
            }
            if (!Down)
            {
                if (hp / maxHp < 0.3f && !lowSaid) { lowSaid = true; Bark("hurt"); }
                else if (hp / maxHp > 0.6f) lowSaid = false;
                if (!pc.IsDead && pc.hp < pc.MaxHp * 0.3f && Time.time > worryAt) { worryAt = Time.time + 30f; Bark("worry"); }
            }
            if (flash > 0f)
            {
                flash -= Time.deltaTime;
                for (int i = 0; i < rends.Length; i++) rends[i].material.color = Color.Lerp(baseCols[i], Color.white, 0.6f);
            }
            else if (rends != null && rends.Length > 0 && rends[0].material.color != baseCols[0] && !Down)
                for (int i = 0; i < rends.Length; i++) rends[i].material.color = baseCols[i];

            if (Down)
            {
                if (Time.time >= downUntil) Revive(0.4f);
                return;
            }
            if (TownUI.I != null && TownUI.I.PanelOpen) return;

            float fam = 1f + (GuildData.I != null ? GuildData.I.FamiliarityBonusPct(partner.key) / 100f : 0f);
            float speed = 3.2f + partner.spd * 0.05f;
            Vector3 pp = pc.transform.position;
            float toPlayer = Flat(pp - transform.position).magnitude;

            // terlalu jauh dari pemain: menyusul
            if (toPlayer > 45f) { Teleport(pp + SlotOffset()); return; }

            // dukungan
            if (IsSupport && Time.time >= healReady)
            {
                float amt = (6f + partner.atk * 0.35f) * fam * PartnerPersona.HealMul(partner.key);
                if (!pc.IsDead && pc.hp < pc.MaxHp * 0.7f && toPlayer < 18f)
                {
                    pc.Heal(amt);
                    healReady = Time.time + 5f;
                    RT.Line(transform.position + Vector3.up * 1.6f, pp + Vector3.up * 1.2f, 0.25f);
                }
                else
                {
                    AllyUnit weak = null; float wr = 0.6f;
                    foreach (var a in All) if (a != this && !a.Down && a.hp / a.maxHp < wr) { wr = a.hp / a.maxHp; weak = a; }
                    if (weak != null) { weak.Heal(amt * 1.4f); healReady = Time.time + 5f; }
                }
            }

            // cari musuh dekat pemain
            Monster target = null;
            float bd = float.MaxValue, engage = IsSupport ? 12f : 26f;
            foreach (var m in Monster.All)
            {
                if (m.IsDead) continue;
                float dp = Flat(m.transform.position - pp).magnitude;
                if (dp > engage) continue;
                float d = Flat(m.transform.position - transform.position).magnitude;
                if (d < bd) { bd = d; target = m; }
            }

            if (target != null)
            {
                if (!engaged) { engaged = true; if (Random.value < 0.55f) Bark("engage"); }
                calmAt = Time.time + 5f;
            }
            else if (engaged && Time.time > calmAt) engaged = false;

            if (target != null)
            {
                float range = Ranged ? 13f : IsSupport ? 9f : 2.6f + target.Radius;
                Vector3 tp = target.transform.position;
                float d = Flat(tp - transform.position).magnitude;
                Face(tp);
                if (d > range) Move(tp, speed * (d > range + 6f ? 1.5f : 1f));
                else if ((Ranged || IsSupport) && d < range * 0.45f) Move(transform.position - Flat(tp - transform.position).normalized * 3f, speed);
                if (d <= range + 0.3f && Time.time >= attackReady && !IsSupport)
                {
                    attackReady = Time.time + Mathf.Lerp(1.5f, 0.8f, Mathf.InverseLerp(40f, 95f, partner.spd)) * (Role == "Tank" ? 1.25f : 1f);
                    float dmg = partner.atk * fam * PartnerPersona.AtkMul(partner.key) * (Role == "Tank" ? 0.7f : 1f);
                    target.TakeDamage(dmg, false);
                    if (Ranged) RT.Line(transform.position + Vector3.up * 1.5f, target.HitPoint, 0.12f);
                    else RT.Flash(Vector3.Lerp(transform.position, tp, 0.6f) + Vector3.up * 1.2f, transform.rotation, new Vector3(1.6f, 0.1f, 1.1f), Color.white, 0.07f);
                }
            }
            else
            {
                Vector3 want = pp + SlotOffset();
                float d = Flat(want - transform.position).magnitude;
                if (d > 1.2f) { Face(want); Move(want, speed * (d > 8f ? 1.8f : 1f)); }
                if (Time.time > nextChat && toPlayer < 6f && partner.lines != null && partner.lines.Length > 0 && TownUI.I != null)
                {
                    nextChat = Time.time + Random.Range(120f, 220f);
                    TownUI.I.Toast(partner.name + ": \"" + partner.Line(GuildData.I.FamiliarityLevel(partner.key)) + "\"");
                }
            }
            Snap();
        }

        Vector3 SlotOffset()
        {
            float side = slot == 0 ? -1f : 1f;
            return new Vector3(side * 2.4f, 0f, -2.2f);
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        void Face(Vector3 t)
        {
            Vector3 d = Flat(t - transform.position);
            if (d.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d.normalized), 10f * Time.deltaTime);
        }

        void Move(Vector3 t, float speed)
        {
            Vector3 d = Flat(t - transform.position);
            if (d.magnitude < 0.2f) return;
            float step = speed * Time.deltaTime;
            if (NpcMove.Blocked(transform, d, step + 0.2f, 0.4f)) return;     // tidak menembus tembok
            transform.position += d.normalized * step;
        }

        public void Teleport(Vector3 p)
        {
            transform.position = new Vector3(p.x, GroundAt(p), p.z);
        }

        public void Heal(float amt)
        {
            if (Down) return;
            hp = Mathf.Min(maxHp, hp + amt);
        }

        public void TakeDamage(float dmg)
        {
            if (Down) return;
            float def = partner != null ? partner.def : 0f;
            dmg *= 100f / (100f + def * 0.8f);
            hp -= dmg;
            flash = 0.1f;
            if (hp <= 0f) GoDown();
        }

        void GoDown()
        {
            hp = 0f;
            Down = true;
            downUntil = Time.time + 25f;
            if (body != null) { body.localRotation = Quaternion.Euler(0, 0, 80f); body.localPosition = new Vector3(0f, 0.4f, 0f); }
            if (TownUI.I != null) TownUI.I.Toast(partner.name + " tumbang! Akan bangkit lagi sebentar.");
            nextBark = 0f; Bark("down");
        }

        /// <summary>Ucapan singkat di atas kepala partner sesuai wataknya (jeda minimal 6 dtk).</summary>
        public void Bark(string ev)
        {
            if (partner == null || Time.time < nextBark) return;
            string line = PartnerPersona.Line(partner, ev);
            if (string.IsNullOrEmpty(line)) return;
            nextBark = Time.time + 6f;
            if (bubble == null)
            {
                var go = new GameObject("Bubble"); go.transform.SetParent(transform, false); go.transform.localPosition = new Vector3(0f, 3.35f, 0f);
                bubbleTm = go.AddComponent<TextMesh>();
                bubbleTm.fontSize = 44; bubbleTm.characterSize = 0.05f; bubbleTm.anchor = TextAnchor.LowerCenter; bubbleTm.alignment = TextAlignment.Center;
                bubbleTm.color = new Color(1f, 0.97f, 0.8f);
                bubbleTm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                HunterGuild.TextMat.Apply(go.GetComponent<MeshRenderer>(), bubbleTm.font.material);
                bubble = go.transform;
            }
            // pecah baris panjang agar tidak melebar
            var sb = new System.Text.StringBuilder(); int run = 0;
            foreach (var w in line.Split(' ')) { if (run + w.Length > 26) { sb.Append('\n'); run = 0; } sb.Append(w).Append(' '); run += w.Length + 1; }
            bubbleTm.text = sb.ToString().TrimEnd();
            bubble.gameObject.SetActive(true); bubbleUntil = Time.time + 4.2f;
        }

        public void Revive(float fraction)
        {
            Down = false;
            nextBark = 0f; Bark("revive");
            hp = Mathf.Max(hp, maxHp * fraction);
            if (body != null) { body.localRotation = Quaternion.identity; body.localPosition = Vector3.zero; }
            if (rends != null) for (int i = 0; i < rends.Length; i++) rends[i].material.color = baseCols[i];
        }
    }

}
