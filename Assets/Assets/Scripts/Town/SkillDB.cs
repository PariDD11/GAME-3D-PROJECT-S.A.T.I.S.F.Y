using System.Collections.Generic;

namespace HunterGuild
{
    public class SkillDef
    {
        public string id, name, job, branch, desc;
        public int tier, cost, index;      // index = urutan dalam baris tier
        public bool active;                // Tier 1 = pasif, Tier 2-3 = aktif
        public float cooldown, mult;
        // --- skill berbasis data (job baru & evolusi); skill lama memakai kode khusus di SkillExec
        public string evo = "";            // id evolusi pemilik skill ("" = skill job biasa)
        public string kind = "";           // bolt | cone | nova | ground | line | dash | buff | heal | summon | multi | trap
        public string elem = "";           // fire / earth / water / wind / holy / phys / arrow
        public float radius, range = 0f;   // ukuran area (m) / jangkauan (m)
        public string fx = "";             // efek tambahan "slow:0.5:3;burn:0.1;knock:4;stagger:1;heal:0.2;dur:6;count:5"
        public string beast = "";         // ultimate: jenis siluet hewan mitologi (wolf/lion/horse/bird/dragon/serpent/turtle/kraken/bull/stag/phoenix/leviathan)
        public string sig = "";           // pola efek visual khas skill ini (lihat SigFx)
        public string pasStr = "";         // passive "dmg:0.1;def:5"
        public Dictionary<string, float> pas;
        public float Fx(string key, float def = 0f)
        {
            if (string.IsNullOrEmpty(fx)) return def;
            foreach (var part in fx.Split(';'))
            {
                var kv = part.Split(':');
                if (kv[0] == key && kv.Length > 1) { float v; if (float.TryParse(kv[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v)) return v; }
            }
            return def;
        }
        public float Fx2(string key, float def = 0f)    // nilai kedua (mis. slow:0.5:3 -> 3)
        {
            if (string.IsNullOrEmpty(fx)) return def;
            foreach (var part in fx.Split(';'))
            {
                var kv = part.Split(':');
                if (kv[0] == key && kv.Length > 2) { float v; if (float.TryParse(kv[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v)) return v; }
            }
            return def;
        }
        public string FxS(string key)    // nilai teks (mis. pet:golem)
        {
            if (string.IsNullOrEmpty(fx)) return "";
            foreach (var part in fx.Split(';')) { var kv = part.Split(':'); if (kv[0] == key && kv.Length > 1) return kv[1]; }
            return "";
        }
        public bool HasFx(string key) { if (string.IsNullOrEmpty(fx)) return false; foreach (var part in fx.Split(';')) if (part.Split(':')[0] == key) return true; return false; }
    }

    /// <summary>
    /// Data skill sesuai GDD 4.4: 3 cabang per job, tiap cabang 6 node
    /// (3 Tier 1 = 1 SP pasif, 2 Tier 2 = 3 SP aktif cooldown 8-15 dtk, 1 Tier 3 = 5 SP ultimate cooldown maks 45 dtk).
    /// Efek skill dijalankan oleh SkillExec; file ini berisi data dan status belajar.
    /// </summary>
    public static partial class SkillDB
    {
        public static readonly List<SkillDef> All = new List<SkillDef>();
        static readonly Dictionary<string, SkillDef> byId = new Dictionary<string, SkillDef>();

        public static readonly string[] SwordBranches = { "Offense (Berserker)", "Defense (Guardian)", "Mobility (Duelist)" };
        public static readonly string[] ArcherBranches = { "Precision (Sniper)", "Utility (Trapper)", "Mobility (Skirmisher)" };

        public static readonly string[] FireBranches = { "Inferno (Pembakar)", "Ember Ward (Penjaga Api)", "Flame Step (Penari Api)" };
        public static readonly string[] EarthBranches = { "Stonecraft (Pemahat Batu)", "Bedrock (Penjaga Bumi)", "Terra Flow (Penjelajah Tanah)" };

        public static readonly string[] WaterBranches = { "Tide (Arus Pasang)", "Frost (Pembeku)", "Mist (Kabut Penyembuh)" };
        public static readonly string[] WindBranches = { "Gale (Badai)", "Zephyr (Angin Gesit)", "Razor Wind (Bilah Angin)" };
        public static readonly string[] PriestBranches = { "Radiance (Cahaya)", "Mercy (Penyembuhan)", "Aegis (Perlindungan)" };

        public static string EvoBranch(string evoId) { return "EVO:" + evoId; }

        public static string[] Branches(string job)
        {
            switch (job)
            {
                case "WaterMage": return WaterBranches;
                case "WindMage": return WindBranches;
                case "Priest": return PriestBranches;
                case "Archer": return ArcherBranches;
                case "FireMage": return FireBranches;
                case "EarthMage": return EarthBranches;
                default: return SwordBranches;
            }
        }

        public static SkillDef Get(string id)
        {
            SkillDef s;
            return id != null && byId.TryGetValue(id, out s) ? s : null;
        }

        public static List<SkillDef> Row(string job, string branch, int tier)
        {
            var l = new List<SkillDef>();
            foreach (var s in All) if (s.job == job && s.branch == branch && s.tier == tier) l.Add(s);
            l.Sort((a, b) => a.index.CompareTo(b.index));
            return l;
        }

        static void P(string job, string branch, int idx, string id, string name, string desc)
        {
            Reg(new SkillDef { id = id, name = name, job = job, branch = branch, tier = 1, cost = 1, index = idx, active = false, desc = desc });
        }

        static void A(string job, string branch, int tier, int idx, string id, string name, float cd, string desc)
        {
            Reg(new SkillDef { id = id, name = name, job = job, branch = branch, tier = tier, cost = tier == 2 ? 3 : 5, index = idx, active = true, cooldown = cd, mult = tier == 2 ? 1.5f : 2.0f, desc = desc });
        }

        /// <summary>Cooldown ultimate (skill tier 3) paling lama, untuk SEMUA job dan evolusi.</summary>
        public const float UltimateMaxCooldown = 45f;

        static void Reg(SkillDef s)
        {
            if (s.tier >= 3 && s.active && s.cooldown > UltimateMaxCooldown) s.cooldown = UltimateMaxCooldown;
            s.pas = new Dictionary<string, float>();
            if (!string.IsNullOrEmpty(s.pasStr))
                foreach (var part in s.pasStr.Split(';'))
                {
                    var kv = part.Split(':'); float v;
                    if (kv.Length == 2 && float.TryParse(kv[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v)) s.pas[kv[0]] = v;
                }
            All.Add(s); byId[s.id] = s;
        }

        static SkillDB()
        {
            const string sw = "Swordsman", ar = "Archer";
            string b;

            b = SwordBranches[0];
            P(sw, b, 0, "sw_off_1", "Heavy Swing", "Serangan berat +10% damage.");
            P(sw, b, 1, "sw_off_2", "Stamina Surge", "Regen stamina +10% setelah serangan berat.");
            P(sw, b, 2, "sw_off_3", "Wound Edge", "Peluang kecil membuat luka (bleed) pada musuh.");
            A(sw, b, 2, 0, "sw_off_4", "Cleave", 10f, "Tebasan lebar ke depan.");
            A(sw, b, 2, 1, "sw_off_5", "Rage Strike", 14f, "Serangan penuh amarah, biaya stamina besar.");
            A(sw, b, 3, 0, "sw_off_6", "Executioner's Arc", 60f, "Ultimate: tebasan melengkung ke titik lemah.");

            b = SwordBranches[1];
            P(sw, b, 0, "sw_def_1", "Iron Guard", "Guard mengurangi damage tambahan 10%.");
            P(sw, b, 1, "sw_def_2", "Brace Stance", "Tidak terhuyung saat menahan serangan ringan.");
            P(sw, b, 2, "sw_def_3", "Second Wind", "Pulihkan sedikit stamina setelah guard sukses.");
            A(sw, b, 2, 0, "sw_def_4", "Perfect Parry", 8f, "Parry tepat waktu membatalkan serangan dan menghuyungkan monster.");
            A(sw, b, 2, 1, "sw_def_5", "Shield Bash", 12f, "Hantam dengan perisai, stun singkat.");
            A(sw, b, 3, 0, "sw_def_6", "Bulwark", 75f, "Ultimate: damage masuk turun drastis dan menarik aggro monster.");

            b = SwordBranches[2];
            P(sw, b, 0, "sw_mob_1", "Quick Step", "Biaya stamina dodge roll -10%.");
            P(sw, b, 1, "sw_mob_2", "Lunge", "Serangan maju lebih cepat dan jauh.");
            P(sw, b, 2, "sw_mob_3", "Light Foot", "Kecepatan gerak +5%.");
            A(sw, b, 2, 0, "sw_mob_4", "Blink Strike", 9f, "Dash singkat sambil menusuk.");
            A(sw, b, 2, 1, "sw_mob_5", "Riposte", 13f, "Serangan balik setelah dodge sukses.");
            A(sw, b, 3, 0, "sw_mob_6", "Phantom Flurry", 55f, "Ultimate: rentetan tebasan super cepat.");

            b = ArcherBranches[0];
            P(ar, b, 0, "ar_pre_1", "Steady Aim", "Bidikan lebih stabil dan zoom lebih jauh.");
            P(ar, b, 1, "ar_pre_2", "Weakpoint Sight", "Titik lemah monster bersinar saat membidik.");
            P(ar, b, 2, "ar_pre_3", "Quick Nock", "Waktu memasang panah -10%.");
            A(ar, b, 2, 0, "ar_pre_4", "Piercing Shot", 10f, "Panah menembus pertahanan monster.");
            A(ar, b, 2, 1, "ar_pre_5", "Deadeye", 14f, "Tembakan presisi ke titik lemah dengan peluang critical tinggi.");
            A(ar, b, 3, 0, "ar_pre_6", "Heartseeker", 70f, "Ultimate: satu panah penentu.");

            b = ArcherBranches[1];
            P(ar, b, 0, "ar_utl_1", "Coating Craft", "Panah khusus lebih hemat bahan.");
            P(ar, b, 1, "ar_utl_2", "Herb Eye", "Titik herb terlihat di kompas.");
            P(ar, b, 2, "ar_utl_3", "Pitfall Prep", "Perangkap lebih cepat dipasang.");
            A(ar, b, 2, 0, "ar_utl_4", "Frost Arrow", 12f, "Panah beku, memperlambat monster.");
            A(ar, b, 2, 1, "ar_utl_5", "Net Trap", 15f, "Jaring menahan monster sesaat.");
            A(ar, b, 3, 0, "ar_utl_6", "Snare Field", 80f, "Ultimate: area perangkap luas.");

            const string fm = "FireMage", em = "EarthMage";

            // ------------------------------ Penyihir Api
            b = FireBranches[0];
            P(fm, b, 0, "fm_inf_1", "Kindling", "Semua sihir api +10% damage.");
            P(fm, b, 1, "fm_inf_2", "Hot Embers", "Bola api dasar membakar musuh (damage berkelanjutan).");
            P(fm, b, 2, "fm_inf_3", "Cinder Focus", "Pemulihan mana +25%.");
            A(fm, b, 2, 0, "fm_inf_4", "Fireball", 11f, "Bola api besar meledak di sasaran, membakar semua di sekitarnya.");
            A(fm, b, 2, 1, "fm_inf_5", "Flame Wave", 13f, "Gelombang api menyapu kerucut di depanmu.");
            A(fm, b, 3, 0, "fm_inf_6", "Meteor Fall", 70f, "Ultimate: hujan meteor di area bidikan.");

            b = FireBranches[1];
            P(fm, b, 0, "fm_wrd_1", "Warm Robes", "Jubah hangat: DEF +5 dan mana maksimum +20.");
            P(fm, b, 1, "fm_wrd_2", "Ash Skin", "Kulit abu: semua damage masuk -7%.");
            P(fm, b, 2, "fm_wrd_3", "Smolder Guard", "Musuh dalam 4,5 m dari penyerang ikut terbakar saat kamu terkena serangan.");
            A(fm, b, 2, 0, "fm_wrd_4", "Flame Barrier", 15f, "Tabir api: damage masuk -50% selama 5 dtk dan membakar musuh yang dekat.");
            A(fm, b, 2, 1, "fm_wrd_5", "Searing Ring", 12f, "Cincin api di sekelilingmu membakar siapa pun yang masuk.");
            A(fm, b, 3, 0, "fm_wrd_6", "Phoenix Rebirth", 85f, "Ultimate: pulihkan 60% HP dan meledakkan api ke sekitar.");

            b = FireBranches[2];
            P(fm, b, 0, "fm_stp_1", "Light Ash", "Dodge roll lebih jauh dan cepat (+30%).");
            P(fm, b, 1, "fm_stp_2", "Quick Cast", "Jeda bola api dasar lebih singkat dan biaya mana semua sihir -10%.");
            P(fm, b, 2, "fm_stp_3", "Spark Trail", "Meninggalkan percikan yang membakar monster di dekatmu saat bergerak.");
            A(fm, b, 2, 0, "fm_stp_4", "Fire Dash", 8f, "Meluncur dalam kobaran api, membakar yang dilewati.");
            A(fm, b, 2, 1, "fm_stp_5", "Flame Pillar", 12f, "Tiang api menyembur dari tanah di titik bidikan.");
            A(fm, b, 3, 0, "fm_stp_6", "Blazing Comet", 60f, "Ultimate: melompat dan menghantam tanah seperti komet.");

            // ------------------------------ Penyihir Tanah
            b = EarthBranches[0];
            P(em, b, 0, "em_stn_1", "Hard Stone", "Semua sihir tanah +10% damage.");
            P(em, b, 1, "em_stn_2", "Gravel Shot", "Lemparan batu dasar memperlambat musuh sesaat.");
            P(em, b, 2, "em_stn_3", "Mineral Focus", "Pemulihan mana +25%.");
            A(em, b, 2, 0, "em_stn_4", "Rock Spike", 10f, "Deretan duri batu menonjol lurus ke depan.");
            A(em, b, 2, 1, "em_stn_5", "Boulder Toss", 13f, "Melontarkan batu raksasa yang menghancur di sasaran.");
            A(em, b, 3, 0, "em_stn_6", "Earthquake", 70f, "Ultimate: gempa bergelombang menghantam semua musuh di sekitar.");

            b = EarthBranches[1];
            P(em, b, 0, "em_bed_1", "Dense Skin", "Kulit sekeras batu: DEF +6 dan mana maksimum +20.");
            P(em, b, 1, "em_bed_2", "Rooted Stance", "Damage masuk -15% saat berdiri diam.");
            P(em, b, 2, "em_bed_3", "Granite Will", "Pulihkan 6 mana setiap kali terkena serangan.");
            A(em, b, 2, 0, "em_bed_4", "Stone Skin", 14f, "Kulit batu: damage masuk -60% selama 6 dtk.");
            A(em, b, 2, 1, "em_bed_5", "Stone Wall", 12f, "Dinding batu muncul di depan, menghantam dan menahan musuh.");
            A(em, b, 3, 0, "em_bed_6", "Titan Bulwark", 80f, "Ultimate: damage masuk -75% selama 10 dtk dan menarik perhatian musuh.");

            b = EarthBranches[2];
            P(em, b, 0, "em_flw_1", "Sure Footing", "Kecepatan gerak +6%.");
            P(em, b, 1, "em_flw_2", "Tremor Sense", "Memberi peringatan getaran saat monster masuk radius 28 m.");
            P(em, b, 2, "em_flw_3", "Seed of Earth", "HP pulih 1% per detik bila tidak terkena serangan selama 4 detik.");
            A(em, b, 2, 0, "em_flw_4", "Quicksand", 12f, "Pasir hisap memperlambat dan melukai musuh di area.");
            A(em, b, 2, 1, "em_flw_5", "Root Bind", 14f, "Akar membelit semua musuh di area, menahan mereka.");
            A(em, b, 3, 0, "em_flw_6", "Terra Surge", 60f, "Ultimate: gelombang tanah raksasa menerjang lurus ke depan.");

            b = ArcherBranches[2];
            P(ar, b, 0, "ar_mob_1", "Light Roll", "Dodge roll lebih jauh.");
            P(ar, b, 1, "ar_mob_2", "Sprint Draw", "Bisa menarik busur sambil berlari.");
            P(ar, b, 2, "ar_mob_3", "Evasive Nock", "Dodge setelah menembak lebih cepat.");
            A(ar, b, 2, 0, "ar_mob_4", "Retreat Shot", 9f, "Menembak sambil melompat mundur.");
            A(ar, b, 2, 1, "ar_mob_5", "Aerial Volley", 13f, "Melompat dan menghujani monster dengan panah.");
            A(ar, b, 3, 0, "ar_mob_6", "Storm Barrage", 60f, "Ultimate: badai panah.");

            BuildNewJobs();
            BuildEvolutions();
            BuildEvolutions2();
            BuildEvoExtras();
            ApplyIdentity();
        }
    }
}
