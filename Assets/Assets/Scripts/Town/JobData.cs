using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    public class EvoDef { public string id, job, name, desc, parent = ""; public int level = 15; }

    /// <summary>Data job: nama, elemen, warna, deskripsi kelas, dan 3 evolusi per job (dipilih sekali di Level 15).</summary>
    public static class JobInfo
    {
        public const int EvoLevel = 15, Evo2Level = 30;
        public static readonly string[] All = { "Swordsman", "Archer", "FireMage", "EarthMage", "WaterMage", "WindMage", "Priest" };

        public static readonly List<EvoDef> Evos = new List<EvoDef>
        {
            new EvoDef { id = "Knight", job = "Swordsman", name = "Knight", desc = "Penjaga barisan depan: DEF tinggi, taunt, perisai, dan hantaman yang menghuyungkan." },
            new EvoDef { id = "Berserker", job = "Swordsman", name = "Berserker", desc = "Brutal dan haus darah: damage besar, makin kuat saat HP rendah, curi nyawa dari musuh." },
            new EvoDef { id = "Blademaster", job = "Swordsman", name = "Blademaster", desc = "Ahli pedang cepat: kombo beruntun, tebasan menembus, gerak lincah dan crit tinggi." },

            new EvoDef { id = "Ranger", job = "Archer", name = "Ranger", desc = "Pemburu alam: jebakan, panah beracun, jaring, dan kontrol medan." },
            new EvoDef { id = "Multishot", job = "Archer", name = "Multishot Master", desc = "Hujan panah: menembak banyak panah sekaligus dan menyapu kerumunan." },
            new EvoDef { id = "ShadowHunter", job = "Archer", name = "Shadow Hunter", desc = "Pemburu bayangan: menyelinap, serangan kejutan mematikan, dan kabur lincah." },

            new EvoDef { id = "Pyromancer", job = "FireMage", name = "Pyromancer", desc = "Penguasa ledakan: ledakan api area besar, meteor, dan bakar massal." },
            new EvoDef { id = "PhoenixMage", job = "FireMage", name = "Phoenix Mage", desc = "Api kebangkitan: api yang menyembuhkan, melindungi, dan membangkitkan diri." },
            new EvoDef { id = "InfernoKnight", job = "FireMage", name = "Inferno Knight", desc = "Penyihir pedang api: menebas dengan pedang berselimut api DAN merapal sihir langsung. Klik kiri = tebasan pedang, tahan klik kanan + klik kiri = tembakan sihir." },

            new EvoDef { id = "Geomancer", job = "EarthMage", name = "Geomancer", desc = "Pengendali medan: dinding batu, pasir hisap, retakan tanah, dan akar penjerat." },
            new EvoDef { id = "GolemMaster", job = "EarthMage", name = "Golem Master", desc = "Pemanggil golem batu yang menyerang dan menahan musuh di sisimu." },
            new EvoDef { id = "TitanGuardian", job = "EarthMage", name = "Titan Guardian", desc = "Tubuh gunung: sangat tahan banting, taunt, dan damage dari tubuh batu." },

            new EvoDef { id = "Cryomancer", job = "WaterMage", name = "Cryomancer", desc = "Penguasa es: membekukan, memperlambat, dan menghancurkan dengan pecahan es." },
            new EvoDef { id = "Tidecaller", job = "WaterMage", name = "Tidecaller", desc = "Pemanggil ombak: gelombang besar yang mendorong dan menyapu musuh." },
            new EvoDef { id = "StormMage", job = "WaterMage", name = "Storm Mage", desc = "Badai petir: hujan dan sambaran petir beruntun di area luas." },

            new EvoDef { id = "TempestLord", job = "WindMage", name = "Tempest Lord", desc = "Raja badai: tornado dan pusaran yang menarik serta menghancurkan musuh." },
            new EvoDef { id = "BladeWind", job = "WindMage", name = "Blade Wind", desc = "Penyihir pedang angin: menebas dengan pedang DAN merapal sihir langsung. Sayatan angin tajam jarak jauh, crit tinggi, menembus musuh." },
            new EvoDef { id = "SkyDancer", job = "WindMage", name = "Sky Dancer", desc = "Penari langit: kecepatan, dash udara, dan serangan sulit dihindari." },

            new EvoDef { id = "Paladin", job = "Priest", name = "Paladin", desc = "Ksatria suci: tameng cahaya, palu suci, tahan banting sambil menyembuhkan diri." },
            new EvoDef { id = "Saint", job = "Priest", name = "Saint", desc = "Santo: berkat besar, area suci, pemulih mana, dan ultimate penyelamat." },
            new EvoDef { id = "Cleric", job = "Priest", name = "Cleric", desc = "Penyembuh utama: heal kuat, regenerasi, dan perlindungan tim." },

            new EvoDef { id = "PaladinLord", job = "Swordsman", parent = "Knight", level = 30, name = "Paladin Lord", desc = "Ksatria suci: perisai bercahaya, damage dan pertahanan seimbang." },
            new EvoDef { id = "Warlord", job = "Swordsman", parent = "Berserker", level = 30, name = "Warlord", desc = "Panglima perang: damage area besar, makin buas saat HP rendah." },
            new EvoDef { id = "DragonBlade", job = "Swordsman", parent = "Blademaster", level = 30, name = "Dragon Blade", desc = "Tebasan naga: gelombang pedang jarak jauh yang menyapu musuh." },
            new EvoDef { id = "WindSniper", job = "Archer", parent = "Ranger", level = 30, name = "Wind Sniper", desc = "Penembak jitu: tembakan jarak ekstrem dengan crit tinggi." },
            new EvoDef { id = "ArrowStorm", job = "Archer", parent = "Multishot", level = 30, name = "Arrow Storm", desc = "Badai panah: hujan panah yang menutupi area luas." },
            new EvoDef { id = "NightAssassin", job = "Archer", parent = "ShadowHunter", level = 30, name = "Night Assassin", desc = "Pembunuh malam: serangan kejutan dengan burst crit." },
            new EvoDef { id = "ArchmageFlame", job = "FireMage", parent = "Pyromancer", level = 30, name = "Archmage of Flame", desc = "Penyihir agung api: sihir api area luas dan meteor." },
            new EvoDef { id = "PhoenixLord", job = "FireMage", parent = "PhoenixMage", level = 30, name = "Phoenix Lord", desc = "Penguasa feniks: api yang menyembuhkan dan membangkitkan." },
            new EvoDef { id = "FlamePaladin", job = "FireMage", parent = "InfernoKnight", level = 30, name = "Flame Paladin", desc = "Ksatria pedang api: tebasan pedang berlapis api suci dan sihir api, dipakai bersamaan." },
            new EvoDef { id = "EarthSovereign", job = "EarthMage", parent = "Geomancer", level = 30, name = "Earth Sovereign", desc = "Penguasa bumi: kontrol medan dan gempa area." },
            new EvoDef { id = "ColossusLord", job = "EarthMage", parent = "GolemMaster", level = 30, name = "Colossus Lord", desc = "Penguasa kolosus: golem raksasa dan pasukan batu." },
            new EvoDef { id = "GaiaAvatar", job = "EarthMage", parent = "TitanGuardian", level = 30, name = "Gaia Avatar", desc = "Avatar Gaia: tubuh bumi yang pulih dan menghantam." },
            new EvoDef { id = "FrostSovereign", job = "WaterMage", parent = "Cryomancer", level = 30, name = "Frost Sovereign", desc = "Penguasa es: membekukan area luas." },
            new EvoDef { id = "OceanSovereign", job = "WaterMage", parent = "Tidecaller", level = 30, name = "Ocean Sovereign", desc = "Penguasa samudra: gelombang besar dan arus penarik." },
            new EvoDef { id = "ThunderSovereign", job = "WaterMage", parent = "StormMage", level = 30, name = "Thunder Sovereign", desc = "Penguasa petir: sambaran berantai berdaya hancur tinggi." },
            new EvoDef { id = "StormEmperor", job = "WindMage", parent = "TempestLord", level = 30, name = "Storm Emperor", desc = "Kaisar badai: tornado raksasa mengendalikan kerumunan." },
            new EvoDef { id = "WindSwordSaint", job = "WindMage", parent = "BladeWind", level = 30, name = "Wind Sword Saint", desc = "Santo pedang angin: pedang dan sihir angin dipakai bersamaan, bilah presisi dengan crit tinggi." },
            new EvoDef { id = "ZephyrEmpress", job = "WindMage", parent = "SkyDancer", level = 30, name = "Zephyr Empress", desc = "Permaisuri zephyr: mobilitas ekstrem dan serangan sulit dihindari." },
            new EvoDef { id = "DivineGuardian", job = "Priest", parent = "Paladin", level = 30, name = "Divine Guardian", desc = "Pelindung ilahi: perisai suci yang menahan segala serangan." },
            new EvoDef { id = "Archsaint", job = "Priest", parent = "Saint", level = 30, name = "Archsaint", desc = "Santo agung: cahaya penyembuh dan penghukum berkekuatan besar." },
            new EvoDef { id = "HighCleric", job = "Priest", parent = "Cleric", level = 30, name = "High Cleric", desc = "Pendeta tinggi: penyembuh utama dengan regenerasi dan perlindungan." },
        };

        public static string Name(string job)
        {
            switch (job)
            {
                case "FireMage": return "Penyihir Api";
                case "EarthMage": return "Penyihir Tanah";
                case "WaterMage": return "Penyihir Air";
                case "WindMage": return "Penyihir Angin";
                case "Priest": return "Priest";
                default: return job;
            }
        }

        /// <summary>Evolusi penyihir yang bersenjata pedang sihir: bisa menebas dengan pedang DAN merapal sihir langsung.</summary>
        public static readonly string[] SpellbladeEvos = { "InfernoKnight", "FlamePaladin", "BladeWind", "WindSwordSaint" };
        public static bool IsSpellblade(string evo, string evo2)
        {
            foreach (var e in SpellbladeEvos) if ((!string.IsNullOrEmpty(evo) && evo == e) || (!string.IsNullOrEmpty(evo2) && evo2 == e)) return true;
            return false;
        }

        public static bool IsMage(string job) { return job == "FireMage" || job == "EarthMage" || job == "WaterMage" || job == "WindMage" || job == "Priest"; }

        /// <summary>Elemen sihir: fire / earth / water / wind / holy (kosong untuk job fisik).</summary>
        public static string Elem(string job)
        {
            switch (job) { case "FireMage": return "fire"; case "EarthMage": return "earth"; case "WaterMage": return "water"; case "WindMage": return "wind"; case "Priest": return "holy"; }
            return "";
        }

        public static Color ElemColor(string elem)
        {
            switch (elem)
            {
                case "fire": return new Color(1f, 0.5f, 0.15f);
                case "earth": return new Color(0.72f, 0.55f, 0.32f);
                case "water": return new Color(0.3f, 0.65f, 1f);
                case "wind": return new Color(0.6f, 1f, 0.85f);
                case "holy": return new Color(1f, 0.92f, 0.5f);
                case "arrow": return new Color(0.7f, 0.9f, 0.5f);
                default: return new Color(0.85f, 0.88f, 0.95f);
            }
        }

        public static Color JobColor(string job)
        {
            string e = Elem(job);
            if (e != "") return ElemColor(e);
            return job == "Archer" ? ElemColor("arrow") : new Color(0.95f, 0.45f, 0.35f);
        }

        public static string Info(string job)
        {
            switch (job)
            {
                case "Swordsman": return "Petarung jarak dekat dengan pedang dan perisai. Kuat dalam duel, guard, parry, dan kombo berbasis stamina.";
                case "Archer": return "Penembak jarak jauh. Kontrol jarak, tembakan titik lemah, panah khusus, dan mobilitas tinggi.";
                case "FireMage": return "Penyihir api: bola api, ledakan, dan bakar musuh. Damage area besar, tubuh rapuh.";
                case "EarthMage": return "Penyihir tanah: batu, gempa, dan dinding. Memperlambat, menahan, dan sangat tahan serangan.";
                case "WaterMage": return "Penyihir air: ombak, es, dan kabut penyembuh. Mengendalikan musuh dan memulihkan diri.";
                case "WindMage": return "Penyihir angin: bilah, pusaran, dan kecepatan. Serangan cepat dan gerak lincah.";
                case "Priest": return "Pendeta cahaya: sinar suci, penyembuhan, dan perlindungan. Menghukum yang gelap dan menopang tim.";
            }
            return "";
        }

        public static List<EvoDef> EvosOf(string job) { var l = new List<EvoDef>(); foreach (var e in Evos) if (e.job == job && e.level == EvoLevel) l.Add(e); return l; }
        /// <summary>Kelas Lv.30 yang terbuka dari evolusi Lv.15 tertentu.</summary>
        public static List<EvoDef> Evos2Of(string evo1) { var l = new List<EvoDef>(); foreach (var e in Evos) if (e.level == Evo2Level && e.parent == evo1) l.Add(e); return l; }
        public static EvoDef Evo(string id) { foreach (var e in Evos) if (e.id == id) return e; return null; }
        public static string EvoName(string id) { var e = Evo(id); return e != null ? e.name : id; }

        /// <summary>Nama tampilan: nama evolusi bila sudah berevolusi, jika tidak nama job.</summary>
        public static string Display(GameState gs) { return !string.IsNullOrEmpty(gs.evo2) ? EvoName(gs.evo2) : (!string.IsNullOrEmpty(gs.evo) ? EvoName(gs.evo) : Name(gs.job)); }
    }
}
