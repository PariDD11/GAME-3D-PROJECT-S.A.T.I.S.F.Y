using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Kristal Mana: titik sihir di dunia (Akademi, Kota Eldrath, kota Guild). Tekan E untuk menyerap energinya:
    /// mana/stamina pulih. Penyihir menyerap penuh; job lain hanya sebagian. Mengisi ulang setelah beberapa detik.
    /// Visual dibuat saat game berjalan (unlit, jalan di Built-in dan URP).
    /// </summary>
    public class ManaCrystal : Interactable
    {
        public float cooldown = 45f;
        float readyAt;
        Transform gem;
        Renderer[] rends;
        Light glow;
        float baseY;

        void Awake()
        {
            prompt = "Serap energi Kristal Mana";
            radius = 3.2f;
            var sh = Shader.Find("Sprites/Default");
            var mat = sh != null ? new Material(sh) : null;
            if (mat != null) mat.color = new Color(0.45f, 0.85f, 1f, 0.95f);
            gem = new GameObject("Gem").transform;
            gem.SetParent(transform, false);
            baseY = 1.5f;
            gem.localPosition = new Vector3(0f, baseY, 0f);
            for (int i = 0; i < 2; i++)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var col = c.GetComponent<Collider>(); if (col != null) Destroy(col);
                c.transform.SetParent(gem, false);
                c.transform.localRotation = Quaternion.Euler(i == 0 ? 35f : 55f, 45f, i == 0 ? 35f : 0f);
                c.transform.localScale = Vector3.one * (i == 0 ? 0.7f : 0.45f);
                if (mat != null) c.GetComponent<Renderer>().sharedMaterial = mat;
            }
            // dudukan batu
            var b = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var bc = b.GetComponent<Collider>(); if (bc != null) Destroy(bc);
            b.transform.SetParent(transform, false);
            b.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            b.transform.localScale = new Vector3(0.9f, 0.3f, 0.9f);
            var bs = Shader.Find("Standard");
            if (bs != null) { var bm = new Material(bs); bm.color = new Color(0.35f, 0.35f, 0.4f); b.GetComponent<Renderer>().sharedMaterial = bm; }
            rends = gem.GetComponentsInChildren<Renderer>();
            var lg = new GameObject("Glow"); lg.transform.SetParent(gem, false);
            glow = lg.AddComponent<Light>(); glow.type = LightType.Point; glow.color = new Color(0.5f, 0.85f, 1f); glow.range = 9f; glow.intensity = 1.6f;
        }

        void Update()
        {
            bool ready = Time.time >= readyAt;
            gem.Rotate(0f, 50f * Time.deltaTime, 0f, Space.World);
            gem.localPosition = new Vector3(0f, baseY + Mathf.Sin(Time.time * 2f) * 0.12f, 0f);
            glow.intensity = ready ? 1.6f + Mathf.Sin(Time.time * 3f) * 0.3f : 0.2f;
            foreach (var r in rends) if (r != null && r.sharedMaterial != null) { var c = r.sharedMaterial.color; c.a = ready ? 0.95f : 0.25f; r.sharedMaterial.color = c; }
            prompt = ready ? "Serap energi Kristal Mana" : "Kristal Mana (mengisi " + Mathf.CeilToInt(readyAt - Time.time) + " dtk)";
        }

        public override void Interact()
        {
            if (Time.time < readyAt) { if (TownUI.I != null) TownUI.I.Toast("Kristal masih kosong."); return; }
            var gs = GameState.I;
            var pc = PlayerCombat.I;
            readyAt = Time.time + cooldown;
            Audio.Sfx("potion", transform.position);
            CombatFx.ShockRing(transform.position + Vector3.up * 0.2f, 4f, new Color(0.5f, 0.85f, 1f), 0.5f);
            CombatFx.Burst(transform.position + Vector3.up * 1.5f, new Color(0.5f, 0.85f, 1f), 24, 1f, 3f, 1f, 0.16f);
            if (pc == null) { if (TownUI.I != null) TownUI.I.Toast("Energi kristal mengalir hangat ke tubuhmu."); return; }
            if (gs != null && gs.IsMage)
            {
                pc.stamina = pc.MaxStamina;
                pc.mana = pc.MaxMana;
                pc.Heal(pc.MaxHp * 0.25f);
                if (TownUI.I != null) TownUI.I.Toast("Resonansi sihir! Mana pulih penuh.");
            }
            else
            {
                pc.stamina = Mathf.Min(pc.MaxStamina, pc.stamina + pc.MaxStamina * 0.4f);
                if (TownUI.I != null) TownUI.I.Toast("Energi kristal memulihkan sebagian staminamu.");
            }
        }
    }

    /// <summary>Guru Sihir di Akademi: penjelasan dua aliran sihir dan tongkat latihan gratis bagi penyihir yang belum bersenjata.</summary>
    public class MageMentor : Interactable
    {
        public override void Interact() { OpenMain(); }

        void OpenMain()
        {
            var gs = GameState.I;
            TownUI.I.Open("Guru Sihir Akademi",
                () => "Magister Aelwyn: \"Api membakar, tanah menahan. Keduanya meminjam kekuatan dunia; jangan lupa meminjamnya dengan hormat.\"" +
                      "\nJob-mu: " + (gs.jobChosen ? JobInfo.Name(gs.job) : "belum dipilih (Level 5)"),
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Aliran Api >", () => Info("Penyihir Api",
                        "Tongkat api melempar bola api yang meledak dan membakar musuh. Skill: Fireball, Flame Wave, Meteor Fall; Flame Barrier, Searing Ring, Phoenix Rebirth; Fire Dash, Flame Pillar, Blazing Comet.\n\n" +
                        "Gaya bermain: damage besar ke kerumunan, damage berkelanjutan (bakar). Rapuh bila didekati; gunakan Flame Barrier. Bahan tongkat: Kain Kutukan dan Ektoplasma dari penyihir rawa.")));
                    l.Add(new UIEntry("Aliran Tanah >", () => Info("Penyihir Tanah",
                        "Tongkat batu melempar bongkahan yang memperlambat. Skill: Rock Spike, Boulder Toss, Earthquake; Stone Skin, Stone Wall, Titan Bulwark; Quicksand, Root Bind, Terra Surge.\n\n" +
                        "Gaya bermain: kontrol dan ketahanan; menahan serta memperlambat musuh sambil tetap hidup. Bahan tongkat: Inti Golem Pasir dari gurun.")));
                    l.Add(new UIEntry("Aliran Air >", () => Info("Water Mage", "Sihir air dan es: memperlambat, membekukan, dan kontrol area. Cabang: Tide, Frost, Mist. Evolusi Lv.15: Cryomancer, Tidecaller, Storm Mage.")));
                    l.Add(new UIEntry("Aliran Angin >", () => Info("Wind Mage", "Sihir angin: cepat, menarik dan menerbangkan musuh. Cabang: Gale, Zephyr, Razor Wind. Evolusi Lv.15: Tempest Lord, Blade Wind, Sky Dancer.")));
                    l.Add(new UIEntry("Aliran Cahaya >", () => Info("Priest", "Cahaya suci: menyembuhkan, melindungi, dan menghukum. Cabang: Radiance, Mercy, Aegis. Evolusi Lv.15: Paladin, Saint, Cleric.")));
                    bool mage = gs.jobChosen && gs.IsMage;
                    string starter = gs.job == "FireMage" ? "ember_rod" : gs.job == "WaterMage" ? "tide_rod" : gs.job == "WindMage" ? "breeze_rod" : gs.job == "Priest" ? "prayer_rod" : "oak_staff";
                    bool got = gs.flags.Contains("mage_starter");
                    bool hasWeapon = gs.equipment.ContainsKey("Weapon");
                    l.Add(new UIEntry("Ambil tongkat latihan (gratis, sekali)" + (got ? "  [sudah diambil]" : mage ? "" : "  [khusus penyihir]"), () =>
                    {
                        gs.flags.Add("mage_starter");
                        gs.SetEquipment("Weapon", starter, 100f, 100f);
                        TownUI.I.Toast("Magister Aelwyn memberimu " + ItemDB.Get(starter).name + ".");
                        OpenMain();
                    }, mage && !got && !hasWeapon));
                    l.Add(new UIEntry("Tutup", () => TownUI.I.Close()));
                    return l;
                });
        }

        void Info(string title, string body)
        {
            TownUI.I.Open(title, () => body, () => { var l = new List<UIEntry>(); l.Add(new UIEntry("< Kembali", OpenMain)); return l; });
        }
    }
}
