using System.Collections.Generic;

namespace HunterGuild
{
    /// <summary>
    /// Identitas unik tiap skill: nama, siluet hewan mitologi untuk ultimate, status/angka khas tiap evolusi, dan pola visual (sig).
    /// Dijalankan setelah semua skill terdaftar (akhir static constructor SkillDB). Tidak mengubah skill berkoreografi tangan (Swordsman e30_0..2).
    /// </summary>
    public static partial class SkillDB
    {
        // id | nama | siluet | fx tambahan (menimpa kunci yang sama) | pengali damage
        static readonly string[] UltRows =
        {
            "sw_off_6|Fenrir's Final Fang|wolf||1", "sw_def_6|Xuanwu Bastion|turtle||1", "sw_mob_6|Sleipnir Phantom Rush|horse||1",
            "ar_pre_6|Garuda Heartseeker|bird||1", "ar_utl_6|Ouroboros Snare Field|serpent||1", "ar_mob_6|Roc Storm Barrage|bird||1",
            "fm_inf_6|Manticore Meteor Fall|lion||1", "fm_wrd_6|Phoenix Rebirth|phoenix||1", "fm_stp_6|Hellhound Blazing Comet|wolf||1",
            "em_stn_6|Behemoth Earthquake|bull||1", "em_bed_6|Akupara Titan Bulwark|turtle||1", "em_flw_6|Tarasque Terra Surge|lion||1",
            "wm_tid_6|Kraken Tsunami|kraken||1", "wm_fst_6|Skoll Blizzard Howl|wolf||1", "wm_mst_6|Kirin Moonlit Spring|stag||1",
            "wd_gal_6|Anzu Tornado|bird||1", "wd_zep_6|Pegasus Storm Step|horse||1", "wd_rzr_6|Griffin Thousand Blades|bird||1",
            "pr_rad_6|Lamassu Judgment|lion||1", "pr_mer_6|Benu Resurrection Light|bird||1", "pr_aeg_6|Sphinx Divine Shield|lion||1",
            "kn_6|Minotaur Fortress|bull||1", "bz_6|Garm Bloodbath|wolf||1", "bm_6|Tengu Thousand Cuts|bird||1",
            "rg_6|Cernunnos Wild Hunt|wolf||1", "ms_6|Thunderbird Arrow Storm|bird||1", "sh_6|Huginn Nightfall|bird||1",
            "py_6|Typhon Armageddon|dragon||1", "ph_6|Fenghuang Ascension|phoenix||1", "ik_6|Cerberus Inferno Blade|wolf||1",
            "ge_6|Midgard Serpent Quake|serpent||1", "gm_6|Catoblepas Colossus|bull||1", "tg_6|Baku Mountain's Wrath|bull||1",
            "cr_6|Nidhogg Absolute Zero|dragon||1", "td_6|Leviathan's Wrath|leviathan||1", "sm_6|Raiju Tempest Rain|wolf||1",
            "tl_6|Charybdis Maelstrom|kraken||1", "bw_6|Hippogriff Heaven's Edge|horse||1", "sd_6|Qilin Heavenly Waltz|stag||1",
            "pl_6|Nemean Avenging Wrath|lion||1", "sa_6|Unicorn Heaven's Gate|horse||1", "cl_6|White Stag Miracle|stag||1",
            "e30_0_6|Golden Ram Ascended Fortress|bull|b_dmgred:0.7;b_dmg:0.3;invuln:1;heal:0.35|1",
            "e30_1_6|Hydra Ragnarok Slash|serpent|count:9;tick:0.55;stagger:0.6;knock:2|1.05",
            "e30_2_6|Dragon King Slash|dragon||1",
            "e30_3_6|Simurgh Heaven Piercer|bird||1",
            "e30_4_6|Stymphalian Cataclysm Volley|bird|count:10;tick:0.5;slow:0.4:3|1",
            "e30_5_6|Anubis Death Mark|wolf|crit:55;stagger:0.6;knock:2;dot:0.1:4|1.1",
            "e30_6_6|Yatagarasu Sun Fall|bird|count:7;tick:0.7;burn:0.22:4|1.1",
            "e30_7_6|Eternal Rebirth|phoenix|heal:0.15;count:8;tick:0.8;mana:0.2;invuln:1.5;b_dmg:0.2;dur:8|1",
            "e30_8_6|Chimera Solar Bulwark|lion|b_dmgred:0.6;b_dmg:0.4;b_lifesteal:0.15;dur:10;invuln:0.6|1",
            "e30_9_6|Zaratan Continental Quake|turtle|pull:10;stagger:1.5;count:6;tick:0.8;slow:0.3:3|1",
            "e30_10_6|Bahamut Colossus Awakening|bull|dur:40|1",
            "e30_11_6|Eikthyrnir Gaia Embrace|stag|b_dmgred:0.8;b_dmg:0.15;heal:0.45;dur:10|1",
            "e30_12_6|Fafnir Glacial Zero|dragon|pull:6;stagger:3;count:7;tick:0.7;slow:0.6:4|1",
            "e30_13_6|Leviathan Call|leviathan|slow:0.3:4;pull:4|1",
            "e30_14_6|Airavata Heaven's Wrath|bull|stagger:2;knock:3;crit:20;dot:0.08:3|1.05",
            "e30_15_6|Wyvern Imperial Typhoon|dragon|pull:12;stagger:0.6;count:9;tick:0.5;knock:2|1",
            "e30_16_6|Kamaitachi Hundred Wind Blades|wolf|crit:50;stagger:0.5;knock:1;slow:0.3:2|1.1",
            "e30_17_6|Alicorn Ten Thousand Gales|horse|knock:8;b_spd:0.5;dur:10|1",
            "e30_18_6|Tetramorph Divine Judgement|lion|invuln:1.5;b_dmgred:0.6;heal:0.2;b_cdr:0.2;dur:10|1",
            "e30_19_6|Ziz Celestial Descent|bird|heal:0.22;count:5;tick:1.2;mana:0.4;b_dmgred:0.3;dur:6|1",
            "e30_20_6|Ceryneian Hind Miracle|stag|heal:0.12;count:10;tick:0.6;mana:0.5;b_cdr:0.3;dur:8|1",
        };

        static string BeastWord(string b)
        {
            switch (b)
            {
                case "wolf": return "serigala"; case "lion": return "singa bersayap"; case "horse": return "kuda bersayap";
                case "bird": return "burung raksasa"; case "dragon": return "naga"; case "serpent": return "ular raksasa";
                case "turtle": return "kura-kura raksasa"; case "kraken": return "kraken"; case "bull": return "banteng raksasa";
                case "stag": return "rusa suci"; case "phoenix": return "feniks"; case "leviathan": return "leviathan";
            }
            return b;
        }

        // id evolusi | kata khas (awalan nama) | fx tambahan khas
        static readonly string[] FlavorRows =
        {
            "Knight|Bastion|lifesteal:0.05", "Berserker|Gore|burn:0.08:3", "Blademaster|Swiftsteel|crit:15",
            "Ranger|Briar|dot:0.06:4", "Multishot|Volley|crit:10;stagger:0.3", "ShadowHunter|Umbral|crit:20;slow:0.3:2",
            "Pyromancer|Cinder|burn:0.12:4", "PhoenixMage|Emberwing|heal:0.05", "InfernoKnight|Scorchblade|burn:0.1:3;crit:10",
            "Geomancer|Terran|slow:0.4:3", "GolemMaster|Golem|stagger:0.8", "TitanGuardian|Titanic|knock:3",
            "Cryomancer|Rime|slow:0.5:4", "Tidecaller|Brine|knock:3", "StormMage|Voltaic|stagger:0.6;crit:10",
            "TempestLord|Squall|pull:3", "BladeWind|Zephyrblade|crit:20", "SkyDancer|Skyward|slow:0.3:2;knock:1",
            "Paladin|Sanctum|heal:0.04", "Saint|Halo|mana:0.03;heal:0.03", "Cleric|Mercy|heal:0.06",
            "PaladinLord|Dawnforged|lifesteal:0.08", "Warlord|Warborn|stagger:1;lifesteal:0.06", "DragonBlade|Wyrmfire|burn:0.15:4",
            "WindSniper|Farflight|crit:25", "ArrowStorm|Stormquill|stagger:0.5;knock:1", "NightAssassin|Nightfang|crit:30;dot:0.08:3",
            "ArchmageFlame|Solar|burn:0.18:5", "PhoenixLord|Rebirth|heal:0.08;burn:0.1:3", "FlamePaladin|Sunblade|burn:0.14:3;heal:0.04",
            "EarthSovereign|Bedrock|slow:0.5:4;stagger:0.6", "ColossusLord|Colossal|knock:4;stagger:1", "GaiaAvatar|Gaian|heal:0.06;knock:2",
            "FrostSovereign|Glacial|slow:0.6:5;stagger:0.8", "OceanSovereign|Abyssal|knock:5;pull:3", "ThunderSovereign|Thunderous|stagger:1;crit:15;dot:0.07:3",
            "StormEmperor|Imperial|pull:5;knock:2", "WindSwordSaint|Sworn|crit:35", "ZephyrEmpress|Zephyr|slow:0.4:3;crit:15",
            "DivineGuardian|Aegis|heal:0.06;stagger:0.8", "Archsaint|Radiant|heal:0.05;mana:0.04;stagger:0.5", "HighCleric|Sanctified|heal:0.08",
        };

        // urutan evolusi Lv30 = nomor di id e30_<n>_...
        static readonly string[] Evo2Order =
        {
            "PaladinLord", "Warlord", "DragonBlade", "WindSniper", "ArrowStorm", "NightAssassin", "ArchmageFlame", "PhoenixLord", "FlamePaladin",
            "EarthSovereign", "ColossusLord", "GaiaAvatar", "FrostSovereign", "OceanSovereign", "ThunderSovereign", "StormEmperor", "WindSwordSaint",
            "ZephyrEmpress", "DivineGuardian", "Archsaint", "HighCleric",
        };

        static readonly Dictionary<string, string[]> SigByKind = new Dictionary<string, string[]>
        {
            { "ground", new[] { "rain", "pillars", "comet", "geyser", "vortex", "lotus", "chains", "shards", "starburst", "halo" } },
            { "nova", new[] { "lotus", "spiral", "pulse", "starburst", "shards", "halo", "orbit", "web" } },
            { "line", new[] { "waves", "crosscut", "claws", "geyser", "feathers", "web", "comet" } },
            { "cone", new[] { "claws", "crosscut", "feathers", "waves", "shards", "starburst" } },
            { "dash", new[] { "claws", "crosscut", "waves", "spiral", "feathers", "starburst", "web" } },
            { "bolt", new[] { "orbit", "feathers", "web", "comet", "starburst", "spiral", "shards" } },
            { "multi", new[] { "feathers", "starburst", "web", "orbit", "comet", "shards", "spiral" } },
            { "buff", new[] { "halo", "spiral", "orbit", "lotus", "pulse", "feathers", "chains" } },
            { "heal", new[] { "halo", "lotus", "spiral", "pulse", "orbit", "feathers" } },
        };

        // kata di nama skill -> pola visual yang sesuai (urutan = prioritas). Nama menentukan wujud efeknya, bukan sekadar putaran daftar.
        static readonly string[][] SigByName =
        {
            new[] { "rain", " rain|barrage|storm|shower" }, new[] { "comet", "comet|meteor|lance|throw| call|falling|descent" },
            new[] { "geyser", "geyser|eruption|fountain|spout" }, new[] { "vortex", "tornado|cyclone|typhoon|vortex|whirl|maelstrom|tempest|quicksand|twister" },
            new[] { "chains", "chain|bind|prison|shackle|root|lock" }, new[] { "web", "web|snare|trap|net " },
            new[] { "pulse", " bash|hammer" },
            new[] { "halo", "halo|sanctuary|bastion|aegis|ward|barrier|shield|guard|bulwark|veil|skin|fortress|stance" },
            new[] { "lotus", "heal|prayer|mercy|grace|bloom|lotus|renew|mend|bless|restor" },
            new[] { "pillars", "pillar|spire|judg|smite|column|obelisk|fissure|stone|sentinel|purge" },
            new[] { "shards", "shard|glacial|spike|crystal|icicle|needle|frost bolt" },
            new[] { "starburst", "nova|burst|explosion|blast|detonat|supernova" },
            new[] { "pulse", "pulse|roar|shout|cry|howl|challenge|bash|hammer|stomp|quake|smash" },
            new[] { "orbit", "orbit|satellite|orb |wings|dance|empower|rune" },
            new[] { "claws", "claw|fang|talon|rend|maul|savage|feral" },
            new[] { "crosscut", "slash|cut|blade|cleave|sever|slice|backstab|strike" },
            new[] { "feathers", "feather|arrow|volley|dart|piercing|skyward|quill|shot" },
            new[] { "waves", "wave|tide|surge|flood|crescent|shockwave" },
            new[] { "spiral", "spiral|drill|dash|rush|step|charge|leap|retreat|gale|smoke" },
        };

        static string SigFromName(string name)
        {
            string n = " " + (name ?? "").ToLowerInvariant() + " ";
            foreach (var row in SigByName)
                foreach (var kw in row[1].Split('|'))
                    if (kw.Length > 0 && n.Contains(kw)) return row[0];
            return null;
        }

        // skill dengan koreografi/eksekusi sendiri (SkillExec.Run/RunMage, SkillAction): tidak diberi sig generik
        static bool IsBespoke(SkillDef s)
        {
            string id = s.id;
            if (id.StartsWith("sw_") || id.StartsWith("ar_pre") || id.StartsWith("ar_utl") || id.StartsWith("ar_mob")) return true;
            if (id.StartsWith("fm_inf") || id.StartsWith("fm_wrd") || id.StartsWith("fm_stp") || id.StartsWith("em_stn") || id.StartsWith("em_bed") || id.StartsWith("em_flw")) return true;
            if (id.StartsWith("e30_")) { var q = id.Split('_'); int n; if (q.Length == 3 && int.TryParse(q[1], out n) && n < 3) return true; }
            return false;
        }

        static int Hash(string s) { int h = 17; foreach (char c in s) h = h * 31 + c; return h & 0x7fffffff; }

        static string MergeFx(string fx, string add)
        {
            if (string.IsNullOrEmpty(add)) return fx;
            var keys = new List<string>(); var vals = new Dictionary<string, string>();
            foreach (var src in new[] { fx, add })
            {
                if (string.IsNullOrEmpty(src)) continue;
                foreach (var part in src.Split(';'))
                {
                    if (part.Length == 0) continue;
                    int i = part.IndexOf(':'); string k = i < 0 ? part : part.Substring(0, i);
                    if (!vals.ContainsKey(k)) keys.Add(k);
                    vals[k] = part;
                }
            }
            var o = new List<string>(); foreach (var k in keys) o.Add(vals[k]);
            return string.Join(";", o.ToArray());
        }

        static string FxText(string add)
        {
            var l = new List<string>();
            foreach (var part in add.Split(';'))
            {
                string k = part.Split(':')[0];
                switch (k)
                {
                    case "burn": l.Add("membakar target"); break; case "slow": l.Add("memperlambat target"); break;
                    case "stagger": l.Add("menghuyungkan target"); break; case "knock": l.Add("mendorong target"); break;
                    case "pull": l.Add("menarik target"); break; case "lifesteal": l.Add("menyerap nyawa musuh"); break;
                    case "dot": l.Add("meracuni target"); break; case "crit": l.Add("peluang crit tinggi"); break;
                    case "heal": l.Add("memulihkan sebagian HP"); break; case "mana": l.Add("memulihkan mana"); break;
                }
            }
            return string.Join(", ", l.ToArray());
        }

        public static bool HasSig(SkillDef s) { return s != null && !string.IsNullOrEmpty(s.sig); }

        // nama ganda dengan skill lain -> dibuat unik
        static readonly string[] NameFix =
        {
            "e30_3_3|Deadeye Mastery", "e30_3_4|Zephyr Needle Volley", "e30_4_4|Tempest Arrow Rain", "e30_5_4|Umbral Backstab", "e30_6_4|Crimson Fire Tempest",
            "e30_7_2|Ashen Rebirth Flame", "e30_8_1|Sunforged Armor", "e30_9_4|Continent Fissure", "e30_9_2|Bedrock Will", "e30_12_1|Eternal Permafrost",
            "e30_13_5|Abyssal Crashing Tide", "e30_14_1|Thunder Charge", "e30_4_3|Arrowstorm Focus", "e30_0_1|Dawn Plate", "e30_19_4|Archsaint's Grace",
        };

        static void ApplyIdentity()
        {
            foreach (var row in NameFix) { var p = row.Split('|'); var s0 = Get(p[0]); if (s0 != null) s0.name = p[1]; }
            // ---- ultimate: nama hewan mitologi + siluet + angka khas
            foreach (var row in UltRows)
            {
                var p = row.Split('|'); var s = Get(p[0]); if (s == null) continue;
                s.name = p[1]; s.beast = p[2];
                if (p[3].Length > 0) s.fx = MergeFx(s.fx, p[3]);
                float m; if (float.TryParse(p[4], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out m) && s.mult > 0f) s.mult *= m;
                if (s.desc != null && !s.desc.Contains("Siluet")) s.desc += " Siluet " + BeastWord(p[2]) + " mitologi muncul dan menyerang musuh.";
            }

            // ---- skill evolusi: kata khas + status + angka unik per evolusi
            var flavor = new Dictionary<string, string[]>();
            foreach (var row in FlavorRows) { var p = row.Split('|'); flavor[p[0]] = p; }
            foreach (var s in All)
            {
                string evo = null;
                if (s.id.StartsWith("x15_") || s.id.StartsWith("x30_"))
                {
                    int u = s.id.LastIndexOf('_'); evo = s.id.Substring(4, u - 4);
                }
                else if (s.id.StartsWith("e30_") && s.tier == 2 && s.active)
                {
                    var q = s.id.Split('_'); int n; if (q.Length == 3 && int.TryParse(q[1], out n) && n >= 3 && n < Evo2Order.Length) evo = Evo2Order[n];   // e30_0..2 = koreografi tangan, dilewati
                }
                if (evo == null) continue;
                string[] f; if (!flavor.TryGetValue(evo, out f)) continue;
                int h = Hash(s.id);
                bool extra = s.id[0] == 'x';
                string baseName = s.name;
                if (extra)
                {
                    string bn = baseName.Replace("Greater ", "");
                    string fl = f[1].ToLowerInvariant();
                    // nama tidak boleh ganda ("Radiant Radiant Burst"): jika kata pertama sama, pakai "Grand"
                    bool dup = bn.ToLowerInvariant().StartsWith(fl) || bn.ToLowerInvariant().Contains(" " + fl + " ") || bn.ToLowerInvariant().Split(' ')[0] == fl.Split(' ')[0];
                    s.name = (dup ? "Grand" : f[1]) + " " + bn;
                }
                s.fx = MergeFx(s.fx, f[2]);
                if (s.mult > 0f) s.mult *= 1f + ((h % 7) - 3) * 0.04f;
                if (s.radius > 3f) s.radius *= 1f + ((h / 7 % 5) - 2) * 0.05f;
                s.cooldown *= 0.9f + (h / 35 % 5) * 0.05f;
                string baseDesc = s.desc ?? "";
                int cut = baseDesc.IndexOf(" (versi "); if (cut > 0) baseDesc = baseDesc.Substring(0, cut);
                s.desc = baseDesc + " Gaya " + JobInfo.EvoName(evo) + ": " + FxText(f[2]) + ".";
            }

            // ---- pola visual: SEMUA skill tier-2 aktif non-bespoke mendapat bentuk efek sesuai namanya (SkillLook)
            foreach (var s in All)
            {
                if (!s.active || s.tier != 2 || string.IsNullOrEmpty(s.kind)) continue;
                if (IsBespoke(s)) continue;
                s.sig = SkillLook.Sig(s);
            }

            // ---- jaring pengaman: tidak boleh ada dua skill dengan nama sama (yang kedua dst diberi angka romawi)
            var seen = new Dictionary<string, int>(); string[] rom = { "", " II", " III", " IV", " V", " VI" };
            foreach (var s in All)
            {
                if (string.IsNullOrEmpty(s.name)) continue;
                int c; seen.TryGetValue(s.name, out c); seen[s.name] = c + 1;
                if (c > 0) { string nn = s.name + rom[System.Math.Min(c, rom.Length - 1)]; s.name = nn; }
            }
        }
    }
}
