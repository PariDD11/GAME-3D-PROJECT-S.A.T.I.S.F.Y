using System.Collections.Generic;

namespace HunterGuild
{
    public static partial class SkillDB
    {
        // kind, elem, nama, cd, mult, radius, range, fx, deskripsi
        class Tpl
        {
            public string name, kind, elem, fx, desc; public float cd, mult, radius, range;
            public Tpl(string name, float cd, string kind, string elem, float mult, float radius, float range, string fx, string desc)
            { this.name = name; this.cd = cd; this.kind = kind; this.elem = elem; this.mult = mult; this.radius = radius; this.range = range; this.fx = fx; this.desc = desc; }
        }

        // 7 job x 3 varian (urutan evolusi Lv15 job itu) x 2 skill aktif tambahan.
        static readonly Dictionary<string, Tpl[][]> ExtraTpl = new Dictionary<string, Tpl[][]>
        {
            { "Swordsman", new[] {
                new[] { new Tpl("Guard Bash", 9f, "cone", "phys", 1.7f, 45f, 4f, "stagger:1.5;knock:2", "Hantam perisai menghuyungkan musuh di depan."), new Tpl("Bastion Stance", 22f, "buff", "phys", 0f, 0f, 0f, "b_dmgred:0.5;dur:8", "Sikap bertahan: damage masuk -50% selama 8 dtk.") },
                new[] { new Tpl("Rending Leap", 10f, "dash", "phys", 2.1f, 0f, 9f, "stagger:0.5", "Melompat menerjang dan merobek musuh di jalur."), new Tpl("Blood Rush", 24f, "buff", "phys", 0f, 0f, 0f, "b_dmg:0.35;dur:8", "Amarah membara: damage +35% selama 8 dtk.") },
                new[] { new Tpl("Crescent Slash", 8f, "line", "phys", 2f, 1.4f, 16f, "knock:1", "Tebasan bulan sabit menembus semua musuh di jalurnya."), new Tpl("Flash Step", 12f, "dash", "phys", 1.4f, 0f, 11f, "crit:25", "Melesat cepat dengan peluang crit tinggi.") } } },
            { "Archer", new[] {
                new[] { new Tpl("Snare Shot", 9f, "bolt", "arrow", 1.6f, 2f, 40f, "slow:0.6:3", "Panah jaring memperlambat target."), new Tpl("Venom Trap", 13f, "ground", "arrow", 1.3f, 5f, 20f, "slow:0.4:4;burn:0.06", "Jebakan beracun di titik bidikan.") },
                new[] { new Tpl("Twin Volley", 8f, "multi", "arrow", 1.2f, 0f, 40f, "count:3;spread:12", "Tiga panah sekaligus ke depan."), new Tpl("Rain Barrage", 14f, "ground", "arrow", 1.2f, 6f, 30f, "count:6;tick:0.4", "Hujan panah di area bidikan.") },
                new[] { new Tpl("Shadow Dart", 9f, "bolt", "arrow", 2.4f, 1.5f, 45f, "crit:30", "Panah bayangan dengan crit besar."), new Tpl("Smoke Retreat", 12f, "dash", "arrow", 0.8f, 0f, 10f, "stagger:0.5;slow:0.5:2", "Mundur dalam asap, memperlambat pengejar.") } } },
            { "FireMage", new[] {
                new[] { new Tpl("Fire Lance", 8f, "line", "fire", 2f, 1.2f, 18f, "burn:0.1;dur:4", "Tombak api menembus barisan musuh."), new Tpl("Magma Burst", 12f, "ground", "fire", 2f, 5f, 24f, "knock:2", "Ledakan magma di titik bidikan.") },
                new[] { new Tpl("Ember Mend", 14f, "heal", "fire", 0f, 0f, 0f, "heal:0.25", "Api lembut memulihkan 25% HP."), new Tpl("Flame Veil", 20f, "buff", "fire", 0f, 0f, 0f, "b_dmgred:0.4;dur:7", "Tirai api: damage masuk -40% selama 7 dtk.") },
                new[] { new Tpl("Blazing Cleave", 8f, "cone", "fire", 2f, 55f, 5f, "burn:0.1;dur:4", "Tebasan berapi di depan."), new Tpl("Flame Charge", 11f, "dash", "fire", 1.9f, 0f, 9f, "burn:0.08;dur:3", "Menerjang dengan selimut api.") } } },
            { "EarthMage", new[] {
                new[] { new Tpl("Stone Spike", 8f, "line", "earth", 1.9f, 1.4f, 16f, "knock:2", "Duri batu menembus tanah di depan."), new Tpl("Quicksand", 13f, "ground", "earth", 1.2f, 6f, 22f, "slow:0.6:4", "Pasir hisap memperlambat musuh.") },
                new[] { new Tpl("Rock Throw", 8f, "bolt", "earth", 2f, 2f, 30f, "knock:1", "Melempar batu besar."), new Tpl("Earthen Guard", 20f, "buff", "earth", 0f, 0f, 0f, "b_dmgred:0.45;dur:8", "Kulit batu: damage masuk -45% selama 8 dtk.") },
                new[] { new Tpl("Quake Stomp", 11f, "nova", "earth", 1.8f, 9f, 0f, "stagger:1.2;knock:2", "Injakan yang mengguncang sekitar."), new Tpl("Granite Skin", 22f, "buff", "earth", 0f, 0f, 0f, "b_dmgred:0.55;dur:7", "Tubuh granit: damage masuk -55% selama 7 dtk.") } } },
            { "WaterMage", new[] {
                new[] { new Tpl("Ice Shard", 8f, "bolt", "water", 2f, 1.8f, 35f, "slow:0.5:3", "Pecahan es memperlambat target."), new Tpl("Frost Nova", 13f, "nova", "water", 1.5f, 9f, 0f, "slow:0.7:3;stagger:0.6", "Ledakan beku di sekelilingmu.") },
                new[] { new Tpl("Tidal Surge", 9f, "line", "water", 2f, 2f, 22f, "knock:4", "Gelombang dorong menyapu lurus."), new Tpl("Whirlpool", 14f, "ground", "water", 1.4f, 6f, 22f, "slow:0.5:3;count:5;tick:0.5", "Pusaran air menarik dan menyakiti musuh.") },
                new[] { new Tpl("Chain Spark", 8f, "multi", "water", 1.4f, 0f, 35f, "count:4;spread:30;stagger:0.3", "Percikan petir ke beberapa arah."), new Tpl("Thunder Call", 12f, "ground", "water", 2.2f, 5f, 26f, "stagger:1", "Memanggil sambaran petir di titik bidikan.") } } },
            { "WindMage", new[] {
                new[] { new Tpl("Gust Cutter", 8f, "line", "wind", 1.9f, 1.4f, 20f, "knock:3", "Sayatan angin menembus musuh."), new Tpl("Cyclone", 14f, "ground", "wind", 1.4f, 6f, 20f, "count:5;tick:0.5;knock:1", "Pusaran angin menyapu area.") },
                new[] { new Tpl("Wind Slash", 8f, "cone", "wind", 2f, 55f, 6f, "crit:15", "Tebasan angin tajam di depan."), new Tpl("Gale Dash", 11f, "dash", "wind", 1.8f, 0f, 10f, "stagger:0.5", "Menerjang secepat badai.") },
                new[] { new Tpl("Sky Step", 9f, "dash", "wind", 1.5f, 0f, 12f, "crit:20", "Loncatan udara dengan peluang crit."), new Tpl("Feather Barrage", 10f, "multi", "wind", 1.2f, 0f, 35f, "count:5;spread:25", "Bulu-bulu angin ditembakkan serentak.") } } },
            { "Priest", new[] {
                new[] { new Tpl("Holy Hammer", 9f, "cone", "holy", 1.9f, 50f, 4.5f, "stagger:1.5", "Palu suci menghuyungkan musuh."), new Tpl("Sacred Ward", 20f, "buff", "holy", 0f, 0f, 0f, "b_dmgred:0.5;dur:8", "Perisai suci: damage masuk -50% selama 8 dtk.") },
                new[] { new Tpl("Blessing", 13f, "heal", "holy", 0f, 0f, 0f, "heal:0.3", "Berkat memulihkan 30% HP."), new Tpl("Radiant Burst", 12f, "nova", "holy", 1.6f, 10f, 0f, "stagger:0.6", "Ledakan cahaya di sekelilingmu.") },
                new[] { new Tpl("Mending Light", 10f, "heal", "holy", 0f, 0f, 0f, "heal:0.2;dur:6", "Cahaya penyembuh yang mengalir."), new Tpl("Purge", 12f, "ground", "holy", 1.7f, 5f, 22f, "stagger:0.8", "Pembersihan suci di titik bidikan.") } } },
        };

        /// <summary>Tiap evolusi Lv15 dan Lv30 mendapat 2 skill aktif tambahan (baris Tier 2 jadi 4 node), sesuai gaya evolusinya.</summary>
        static void BuildEvoExtras()
        {
            var lv15 = new Dictionary<string, int>();
            foreach (var job in JobInfo.All) { var l = JobInfo.EvosOf(job); for (int i = 0; i < l.Count; i++) lv15[l[i].id] = i; }
            foreach (var e in JobInfo.Evos)
            {
                Tpl[][] t; if (!ExtraTpl.TryGetValue(e.job, out t)) continue;
                bool lv30 = e.parent != "";
                int v; if (!lv15.TryGetValue(lv30 ? e.parent : e.id, out v)) v = 0;
                v = System.Math.Min(v, t.Length - 1);
                curEvo = e.id; string br = EvoBranch(e.id);
                for (int k = 0; k < 2; k++)
                {
                    var p = t[v][k];
                    float mult = lv30 && p.mult > 0f ? p.mult * 1.3f : p.mult;
                    string name = (lv30 ? "Greater " : "") + p.name;
                    string desc = p.desc + (lv30 ? " (versi " + e.name + ", lebih kuat)" : "");
                    Aa(e.job, br, 2, 2 + k, "x" + (lv30 ? "30_" : "15_") + e.id + "_" + (k + 1), name, p.cd, p.kind, p.elem, mult, p.radius, p.range, p.fx, desc);
                }
            }
            curEvo = "";
        }
    }
}
