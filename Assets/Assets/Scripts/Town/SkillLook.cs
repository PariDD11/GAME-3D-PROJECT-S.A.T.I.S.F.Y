using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// "Wujud" visual tiap skill ditentukan dari NAMA-nya (kata demi kata, bukan potongan huruf), lalu elemen sebagai cadangan:
    ///  - Theme : tema warna & bahan (fire, ice, storm, water, wind, earth, nature, holy, dark, poison, blood, steel, arrow)
    ///  - Sig   : bentuk efek (lihat SigFx): pilar, hujan, vorteks, kilat, nyala api, kristal es, bilah angin, asap, awan racun, akar, dinding, dst.
    ///  - Marks : bekas di tanah (GroundMark)
    /// Contoh: "Frost Arrow" selalu es-biru-putih, "Venom Trap" selalu hijau beracun, "Thunder Call" selalu kilat kuning-ungu,
    /// walaupun elemen datanya "water"/"arrow".
    /// </summary>
    public static class SkillLook
    {
        public struct Pal { public Color m, h, k; }

        // ------------------------------------------------------------------ pemecah nama
        static string[] Words(string name)
        {
            var l = new List<string>(); var sb = new System.Text.StringBuilder();
            string n = (name ?? "").ToLowerInvariant();
            for (int i = 0; i < n.Length; i++)
            {
                char c = n[i];
                if (c >= 'a' && c <= 'z') sb.Append(c);
                else if (c == '\'') continue;          // "Heaven's" -> "heavens"
                else { if (sb.Length > 0) { l.Add(sb.ToString()); sb.Length = 0; } }
            }
            if (sb.Length > 0) l.Add(sb.ToString());
            return l.ToArray();
        }

        /// <summary>Salah satu kata diawali kunci (kunci dipisah '|').</summary>
        static bool Any(string[] w, string keys)
        {
            foreach (var k in keys.Split('|')) foreach (var x in w) if (x.StartsWith(k)) return true;
            return false;
        }
        /// <summary>Salah satu kata persis sama kunci.</summary>
        static bool Ex(string[] w, string keys)
        {
            foreach (var k in keys.Split('|')) foreach (var x in w) if (x == k) return true;
            return false;
        }

        static string ElemOf(SkillDef s)
        {
            string e = !string.IsNullOrEmpty(s.elem) ? s.elem : JobInfo.Elem(s.job);
            if (e == "" && s.job == "Archer") e = "arrow";
            return e;
        }

        // ------------------------------------------------------------------ tema
        public static string Theme(SkillDef s)
        {
            string[] w = Words(s.name);
            string e = ElemOf(s);
            if (Any(w, "poison|venom|toxic|plague")) return "poison";
            if (Any(w, "shadow|umbral|night|smoke|curse|dusk") || Ex(w, "void|dark|death|doom|backstab")) return "dark";
            if (Any(w, "ice|icy|frost|glacial|blizzard|rime|freez|frozen|snow|cryo|permafrost|winter|hail|chill") || Ex(w, "zero")) return "ice";
            if (Any(w, "thunder|lightning|voltaic|volt|spark|shock|static") || Ex(w, "bolt") || e == "storm") return "storm";
            if (Any(w, "fire|flam|blaz|burn|inferno|ember|cinder|magma|lava|meteor|solar|sun|scorch|phoenix|sear|armageddon|hell|crimson|wyrmfire|pyre|ignit")) return "fire";
            if (Any(w, "water|tidal|tide|tsunami|geyser|bubble|brine|abyssal|leviathan|kraken|maelstrom|whirlpool|torrent|flood|aqua|mist|crashing|moonlit")) return "water";
            if (Any(w, "gale|wind|tornado|cyclone|typhoon|zephyr|squall|gust|twister|updraft|whirlwind|slipstream|breeze")) return "wind";
            if (Any(w, "root|nature|briar|vine|bloom|gaia")) return "nature";
            if (Any(w, "stone|rock|boulder|quake|fissure|terra|golem|titan|granite|bedrock|colossal|colossus|quicksand|sand|earthen|mountain|contin|sinking|gravel|obsidian") || Ex(w, "earth")) return "earth";
            if (e == "phys" && Any(w, "blood|gore|rend|rage|savage|berserk|warborn|slaughter")) return "blood";
            switch (e)
            {
                case "fire": return "fire";
                case "water": return Any(w, "sky|feather") ? "wind" : "water";
                case "wind": return "wind";
                case "earth": return "earth";
                case "holy": return "holy";
                case "arrow": return "arrow";
                case "phys": return "steel";
            }
            return "steel";
        }

        public static Pal Palette(string theme)
        {
            switch (theme)
            {
                case "fire": return P(1f, 0.5f, 0.12f, 1f, 0.95f, 0.7f, 0.8f, 0.1f, 0.05f);
                case "ice": return P(0.55f, 0.88f, 1f, 0.95f, 1f, 1f, 0.25f, 0.55f, 0.95f);
                case "storm": return P(0.95f, 0.88f, 0.3f, 1f, 1f, 0.9f, 0.55f, 0.35f, 1f);
                case "water": return P(0.25f, 0.65f, 1f, 0.85f, 0.97f, 1f, 0.1f, 0.3f, 0.8f);
                case "wind": return P(0.55f, 1f, 0.8f, 1f, 1f, 1f, 0.2f, 0.7f, 0.6f);
                case "earth": return P(0.85f, 0.6f, 0.25f, 1f, 0.9f, 0.6f, 0.4f, 0.28f, 0.14f);
                case "nature": return P(0.45f, 0.85f, 0.3f, 0.9f, 1f, 0.7f, 0.2f, 0.45f, 0.15f);
                case "holy": return P(1f, 0.9f, 0.45f, 1f, 1f, 0.95f, 1f, 0.7f, 0.2f);
                case "dark": return P(0.55f, 0.3f, 0.9f, 0.9f, 0.8f, 1f, 0.12f, 0.05f, 0.25f);
                case "poison": return P(0.5f, 0.95f, 0.2f, 0.9f, 1f, 0.6f, 0.35f, 0.15f, 0.5f);
                case "blood": return P(0.95f, 0.2f, 0.2f, 1f, 0.7f, 0.6f, 0.4f, 0.03f, 0.05f);
                case "arrow": return P(1f, 0.85f, 0.5f, 1f, 1f, 0.9f, 0.5f, 0.75f, 1f);
            }
            return P(0.75f, 0.85f, 1f, 1f, 1f, 1f, 0.35f, 0.45f, 0.7f);   // steel
        }

        static Pal P(float a, float b, float c, float d, float e, float f, float g, float h, float i)
        {
            return new Pal { m = new Color(a, b, c), h = new Color(d, e, f), k = new Color(g, h, i) };
        }

        public static Pal Of(SkillDef s) { return Palette(Theme(s)); }
        public static Color Main(SkillDef s) { return Of(s).m; }

        // ------------------------------------------------------------------ bentuk efek
        public static string Sig(SkillDef s)
        {
            string[] w = Words(s.name);
            string th = Theme(s), k = s.kind ?? "";

            // --- kata yang menentukan bentuk (urutan = prioritas)
            if (Any(w, "web|snare|net") && th != "nature") return "web";
            if (Ex(w, "trap") && th != "poison") return "jaws";
            if (th == "poison") return k == "bolt" || k == "multi" ? "poison" : "poison";
            if (Any(w, "root") || (Any(w, "bind") && th == "nature")) return "roots";
            if (Any(w, "prison|cage")) return th == "ice" ? "cage" : "chains";
            if (Any(w, "chain") && th != "storm") return "chains";
            if (Any(w, "bind|shackle") && !Any(w, "root")) return "chains";
            if (Any(w, "blizzard")) return "blizzard";
            if (Any(w, "bubble")) return "bubble";
            if (Any(w, "wing")) return "wings";
            if (Any(w, "rain|barrage|shower")) return k == "heal" ? "lotus" : "rain";
            if (Any(w, "tornado|cyclone|twister|whirlwind|typhoon|whirlpool|maelstrom|vortex|quicksand|sinking")) return "vortex";
            if (Any(w, "geyser|jet|fountain|spout|updraft")) return "geyser";
            if (Any(w, "wall")) return "wall";
            if (Any(w, "meteor|comet|skyfall|lance|fireball|throw|toss") || Ex(w, "fall")) return "comet";
            if (th == "storm")
            {
                if (Any(w, "dash|rush|step")) return "bolts";
                return "bolts";
            }
            if (Any(w, "smite|judg|purge|hammer") && th == "holy") return "smite";
            if (Any(w, "ray|beam") && th == "holy") return "ray";
            if (Any(w, "zero")) return "frost";
            if (th == "dark")
            {
                if (Any(w, "dart|shot|backstab|cut|stab")) return "crosscut";
                return "smoke";
            }
            if (Any(w, "heal|mend|mercy|blessing|grace|prayer|renew|restor|miracle|cleans|resurrect") && !Any(w, "flame")) return "lotus";
            if (Any(w, "flame") && k == "heal") return "lotus";
            if (Any(w, "veil|barrier|shield|ward|guard|bastion|stance|skin|armor|sanctuar|bulwark|aegis|faith|sacred")) return "halo";
            if (Any(w, "nova") || (Any(w, "burst|blast|explo|detonat") && k != "bolt")) return th == "ice" ? "frost" : "starburst";
            if (Any(w, "spike|shard|icicle|crystal|needle|thorn|spear") || (th == "ice" && k != "dash"))
                return th == "earth" ? "pillars" : "shards";
            if (Any(w, "pillar|spire|column|fissure|obelisk|sentinel|sentry")) return "pillars";
            if (Any(w, "wave|tidal|tide|surge|crashing|flood|crescent") && !Any(w, "slash|cut")) return "waves";
            if (Any(w, "cutter|slash|cut|blade|cleave|sever|slice|flurry|strike|edge"))
                return th == "wind" ? "blades" : (th == "fire" ? "flames" : "crosscut");
            if (Any(w, "fist|smash|slam|stomp|quake|roar|bash|challenge|taunt|provoke|hammer|kick") ) return "pulse";
            if (Any(w, "fang|claw|rend|leap|gore|savage|maul")) return "claws";
            if (Any(w, "dash|rush|step|charge|retreat|dance|waltz|flash|skate|slip")) return th == "fire" ? "flames" : "spiral";
            if (Any(w, "arrow|shot|volley|dart|quill|feather|barrage")) return k == "bolt" ? "impact" : "feathers";
            if (Any(w, "orbit|satellite|empower|rune")) return "orbit";
            if (Any(w, "storm|tempest|inferno|eruption")) return th == "fire" ? "flames" : "rain";
            if (Any(w, "summon|golem")) return "pillars";

            // --- cadangan: bentuk menurut jenis + tema
            switch (k)
            {
                case "ground":
                    switch (th) { case "fire": return "flames"; case "ice": return "frost"; case "storm": return "bolts"; case "water": return "geyser"; case "wind": return "vortex"; case "earth": return "pillars"; case "nature": return "roots"; case "holy": return "smite"; case "arrow": return "rain"; }
                    return "pillars";
                case "nova":
                    switch (th) { case "fire": return "starburst"; case "ice": return "frost"; case "storm": return "bolts"; case "water": return "waves"; case "wind": return "vortex"; case "earth": return "pulse"; case "holy": return "starburst"; }
                    return "pulse";
                case "line":
                    switch (th) { case "fire": return "comet"; case "ice": return "shards"; case "storm": return "bolts"; case "water": return "geyser"; case "wind": return "blades"; case "earth": return "pillars"; case "holy": return "ray"; case "arrow": return "feathers"; }
                    return "crosscut";
                case "cone":
                    switch (th) { case "fire": return "flames"; case "ice": return "shards"; case "storm": return "bolts"; case "water": return "waves"; case "wind": return "blades"; case "earth": return "pillars"; case "holy": return "ray"; case "arrow": return "feathers"; }
                    return "crosscut";
                case "dash": return th == "fire" ? "flames" : "spiral";
                case "bolt": return "impact";
                case "multi": return th == "storm" ? "bolts" : "feathers";
                case "buff": return "halo";
                case "heal": return "lotus";
                case "summon": return "pillars";
                case "trap": return "jaws";
            }
            return "starburst";
        }

        // ------------------------------------------------------------------ bekas di tanah
        public static void Marks(SkillDef s, out string main, out string extra)
        {
            string[] w = Words(s.name);
            string th = Theme(s), sg = s.sig ?? "";
            switch (th)
            {
                case "fire": main = "scorch"; extra = "pock"; break;
                case "ice": main = "frost"; extra = "crack"; break;
                case "storm": main = "bolt"; extra = "scorch"; break;
                case "water": main = "puddle"; extra = "frost"; break;
                case "wind": main = "wind"; extra = "claw"; break;
                case "earth": main = "crater"; extra = "crack"; break;
                case "nature": main = "slither"; extra = "crack"; break;
                case "holy": main = "glyph"; extra = null; break;
                case "dark": main = "rune"; extra = "scorch"; break;
                case "poison": main = "puddle"; extra = "pock"; break;
                case "blood": main = "claw"; extra = "pock"; break;
                case "arrow": main = "pock"; extra = "scorch"; break;
                default: main = "claw"; extra = "crack"; break;
            }
            if (Any(w, "meteor|comet|magma|lava|volcan")) { main = "crater"; extra = "scorch"; }
            else if (Any(w, "quake|smash|slam|stomp|fist|fissure|titan")) { main = "crater"; extra = "crack"; }
            else if (sg == "claws") { main = "claw"; extra = "pock"; }
            else if (s.kind == "dash") { main = th == "fire" ? "scorch" : "skid"; extra = "paw"; }
            else if (sg == "blades" || sg == "crosscut") { if (th != "ice") { extra = main; main = "claw"; } }
            else if (sg == "vortex" && th == "wind") { main = "wind"; extra = "claw"; }
        }
    }
}
