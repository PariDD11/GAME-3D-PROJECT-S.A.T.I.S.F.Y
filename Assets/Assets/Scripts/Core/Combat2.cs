using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>v35z102n32: elemen monster (lemah/tahan), gauge Break, status efek (racun, bakar, beku, stun) untuk monster DAN pemain,
    /// perubahan elemen boss tiap fase, Hindar Sempurna (hindar tepat sebelum kena), pelapis senjata elemen, dan panel info boss.
    /// Semua angka tebakan awal; lihat konstanta di bagian atas.</summary>
    public enum Elem { None, Api, Air, Tanah, Angin, Cahaya, Gelap }

    public static class Combat2
    {
        public static readonly string[] Names = { "-", "Api", "Air", "Tanah", "Angin", "Cahaya", "Gelap" };
        public static readonly Color[] Cols =
        {
            Color.white, new Color(1f, 0.5f, 0.15f), new Color(0.35f, 0.65f, 1f), new Color(0.7f, 0.55f, 0.3f),
            new Color(0.6f, 1f, 0.8f), new Color(1f, 0.95f, 0.55f), new Color(0.65f, 0.4f, 0.9f)
        };

        // ---- angka ----
        const float WeakMul = 1.5f, ResistMul = 0.6f, BreakMul = 1.4f;
        const float CoatSeconds = 240f;
        const float PerfectDodgeBuff = 1.3f;

        /// <summary>Elemen yang mengalahkan e: Api lemah ke Air, Air lemah ke Angin (petir), Angin lemah ke Tanah, Tanah lemah ke Api, Gelap lemah ke Cahaya, Cahaya lemah ke Gelap.</summary>
        public static Elem WeakTo(Elem e)
        {
            switch (e)
            {
                case Elem.Api: return Elem.Air;
                case Elem.Air: return Elem.Angin;
                case Elem.Angin: return Elem.Tanah;
                case Elem.Tanah: return Elem.Api;
                case Elem.Gelap: return Elem.Cahaya;
                case Elem.Cahaya: return Elem.Gelap;
            }
            return Elem.None;
        }
        static Elem Next(Elem e)
        {
            switch (e)
            {
                case Elem.Api: return Elem.Tanah;
                case Elem.Tanah: return Elem.Angin;
                case Elem.Angin: return Elem.Air;
                case Elem.Air: return Elem.Api;
                case Elem.Gelap: return Elem.Cahaya;
                case Elem.Cahaya: return Elem.Gelap;
            }
            return Elem.Api;
        }

        // ---- elemen monster ----
        static readonly Dictionary<string, Elem> byId = new Dictionary<string, Elem>
        {
            { "ember_imp", Elem.Api }, { "lava_lizard", Elem.Api }, { "fire_cultist", Elem.Api }, { "magma_golem", Elem.Api }, { "ignarok", Elem.Api },
            { "imp", Elem.Api }, { "hellhound", Elem.Api }, { "heat_jackal", Elem.Api }, { "cerberus", Elem.Api }, { "demon_brute", Elem.Api }, { "srank_ash", Elem.Api },
            { "ash_wraith", Elem.Gelap }, { "banshee", Elem.Gelap }, { "bone_soldier", Elem.Gelap }, { "bone_archer", Elem.Gelap }, { "grave_knight", Elem.Gelap },
            { "bone_king", Elem.Gelap }, { "dark_cultist", Elem.Gelap }, { "demon_lord", Elem.Gelap }, { "tbs_40", Elem.Gelap }, { "tbs_80", Elem.Gelap },
            { "tbs_90", Elem.Gelap }, { "tbs_100", Elem.Gelap }, { "srank_shadow", Elem.Gelap },
            { "ogre", Elem.Tanah }, { "ogre_chief", Elem.Tanah }, { "behemoth", Elem.Tanah }, { "harvest_brute", Elem.Tanah }, { "pollen_boar", Elem.Tanah },
            { "tbs_10", Elem.Tanah }, { "tbs_50", Elem.Tanah }, { "tbs_60", Elem.Tanah }, { "tbs_70", Elem.Tanah },
            { "road_wolf", Elem.Angin }, { "tbs_20", Elem.Angin }, { "srank_blade", Elem.Angin },
            { "frost_wolf", Elem.Air },
            { "hb_lerai", Elem.Angin }, { "hb_frontier", Elem.Tanah }, { "hb_desert", Elem.Tanah }, { "hb_swamp", Elem.Air }, { "hb_volcano", Elem.Api },
            { "hb_bone", Elem.Gelap }, { "hb_demon", Elem.Gelap },
        };
        static readonly Dictionary<string, Elem> cache = new Dictionary<string, Elem>();

        public static Elem OfDef(MonsterDef d)
        {
            if (d == null) return Elem.None;
            Elem e;
            if (byId.TryGetValue(d.id, out e)) return e;
            if (cache.TryGetValue(d.id, out e)) return e;
            string n = ((d.name ?? "") + " " + (d.shape ?? "")).ToLowerInvariant();
            e = Elem.None;
            if (Has(n, "api", "lava", "bara", "magma", "neraka", "lizard")) e = Elem.Api;
            else if (Has(n, "slime", "air", "rawa", "frost", "salju", "katak", "ikan", "buaya")) e = Elem.Air;
            else if (Has(n, "tulang", "hantu", "mumi", "skeleton", "wraith", "necro", "witch", "dukun", "banshee")) e = Elem.Gelap;
            else if (Has(n, "serigala", "wolf", "burung", "rubah", "elang", "harpy", "bat", "kelelawar")) e = Elem.Angin;
            else if (Has(n, "golem", "ogre", "troll", "batu", "babi", "boar", "bull", "goblin", "scorpion", "kalajengking")) e = Elem.Tanah;
            cache[d.id] = e;
            return e;
        }
        static bool Has(string s, params string[] k) { foreach (var x in k) if (s.Contains(x)) return true; return false; }

        /// <summary>Teks Bestiary: elemen, kelemahan, ketahanan, status yang bisa ditimbulkan. Dibuka bertahap menurut jumlah kill.</summary>
        public static string BestiaryText(MonsterDef d, int kills, bool boss)
        {
            if (d == null) return "";
            Elem e = OfDef(d);
            if (kills < 2) return "Elemen: ??? (2 kill)";
            var sb = new System.Text.StringBuilder();
            sb.Append("Elemen: ").Append(e == Elem.None ? "Netral (tanpa kelemahan / ketahanan)" : Names[(int)e]);
            if (e != Elem.None)
            {
                if (kills >= 4) sb.Append("\nLemah: ").Append(Names[(int)WeakTo(e)]).Append(" x1.5 + Break  |  Tahan: ").Append(Names[(int)e]).Append(" x0.6");
                else sb.Append("\nLemah / Tahan: ??? (4 kill)");
            }
            if (kills >= 6)
            {
                string st = StatusInflicts(d);
                sb.Append("\nStatus: ").Append(st == "" ? "-" : st);
                if (boss) { string ph = PhaseText(d.id); if (ph != "") sb.Append("\nFase: ").Append(ph); }
            }
            else sb.Append("\nStatus: ??? (6 kill)");
            return sb.ToString();
        }

        public static string StatusInflicts(MonsterDef d)
        {
            Elem e = OfDef(d);
            string n = ((d.name ?? "") + " " + (d.shape ?? "")).ToLowerInvariant();
            var l = new List<string>();
            if (Has(n, "scorpion", "kalajengking", "spider", "laba", "ular", "snake", "cultist", "dukun") || e == Elem.Gelap) l.Add("Racun");
            if (e == Elem.Api) l.Add("Terbakar");
            if (e == Elem.Air) l.Add("Beku");
            if (e == Elem.Tanah && d.large) l.Add("Stun");
            return string.Join(", ", l.ToArray());
        }

        public static string PhaseText(string id)
        {
            Elem[] arr; if (!bossPhases.TryGetValue(id, out arr)) return "";
            var l = new List<string>(); foreach (var x in arr) l.Add(Names[(int)x]);
            return string.Join(" > ", l.ToArray());
        }

        // ---- elemen serangan pemain ----
        static Elem coatElem; static float coatUntil; static bool coatPoison;
        public static Elem PlayerElem()
        {
            var gs = GameState.I; if (gs == null) return Elem.None;
            switch (gs.job)
            {
                case "FireMage": return Elem.Api;
                case "WaterMage": return Elem.Air;
                case "EarthMage": return Elem.Tanah;
                case "WindMage": return Elem.Angin;
                case "Priest": return Elem.Cahaya;
            }
            return Time.time < coatUntil && !coatPoison ? coatElem : Elem.None;
        }
        public static bool PoisonCoat { get { return coatPoison && Time.time < coatUntil; } }
        public static string CoatText
        {
            get
            {
                if (Time.time >= coatUntil) return "";
                float t = coatUntil - Time.time;
                return "Pelapis: " + (coatPoison ? "Racun" : Names[(int)coatElem]) + " " + (int)(t / 60f) + ":" + ((int)t % 60).ToString("00");
            }
        }

        // ---- pelapis & penawar (item) ----
        public class Gear { public string id, name; public Elem e; public bool poison, antidote; public int price; }
        public static readonly Gear[] Items =
        {
            new Gear { id = "cx_fire", name = "Pelapis Api", e = Elem.Api, price = 90 },
            new Gear { id = "cx_water", name = "Pelapis Air", e = Elem.Air, price = 90 },
            new Gear { id = "cx_earth", name = "Pelapis Tanah", e = Elem.Tanah, price = 90 },
            new Gear { id = "cx_wind", name = "Pelapis Angin", e = Elem.Angin, price = 90 },
            new Gear { id = "cx_light", name = "Pelapis Cahaya", e = Elem.Cahaya, price = 110 },
            new Gear { id = "cx_dark", name = "Pelapis Gelap", e = Elem.Gelap, price = 110 },
            new Gear { id = "cx_poison", name = "Pelapis Racun", poison = true, price = 80 },
            new Gear { id = "cx_antidote", name = "Penawar", antidote = true, price = 50 },
        };
        public static bool IsItem(string id) { return id != null && id.StartsWith("cx_"); }

        public static void Register()
        {
            foreach (var g in Items)
                ItemDB.AddRaw(new ItemDef
                {
                    id = g.id, name = g.name, type = ItemType.Consumable, buyPrice = g.price, sellPrice = g.price / 3, stack = 10,
                    note = g.antidote ? "Menyembuhkan racun, bakar, beku, dan stun; kebal status 20 detik."
                         : g.poison ? "Senjata meracuni musuh selama 4 menit (semua job). Racun menggerogoti HP."
                         : "Senjata bernuansa " + Names[(int)g.e] + " selama 4 menit (job non-penyihir). Kena lemah = damage x1.5 + gauge Break."
                });
        }

        public static bool UseItem(string id)
        {
            var gs = GameState.I; if (gs == null) return false;
            Gear g = null; foreach (var x in Items) if (x.id == id) g = x;
            if (g == null || gs.Count(id) < 1) return false;
            if (g.antidote) { gs.RemoveItem(id, 1); Cleanse(20f); Toast("Penawar dipakai: status efek hilang."); return true; }
            if (!g.poison && gs.IsMage) { Toast("Penyihir sudah memakai elemen jobnya; pelapis elemen untuk Swordsman/Archer."); return false; }
            gs.RemoveItem(id, 1);
            coatPoison = g.poison; coatElem = g.e; coatUntil = Time.time + CoatSeconds;
            Toast(g.name + " terpasang (4 menit).");
            return true;
        }

        static void Toast(string s) { if (TownUI.I != null) TownUI.I.Toast(s); }

        public static void OpenShop()
        {
            var gs = GameState.I;
            TownUI.I.Open("Toko Alkimia",
                () => "Gold: " + gs.gold + "\nPelapis elemen dipakai dari tas (job non-penyihir). Musuh punya elemen: serang elemen yang dilemahkannya untuk damage x1.5 dan gauge Break.\nApi lemah Air, Air lemah Angin, Angin lemah Tanah, Tanah lemah Api, Gelap <-> Cahaya.",
                () =>
                {
                    var l = new List<UIEntry>();
                    foreach (var gg in Items)
                    {
                        var g2 = gg;
                        l.Add(new UIEntry(g2.name + "  -  " + g2.price + "g   (punya " + gs.Count(g2.id) + ")", () => { if (gs.TrySpend(g2.price)) { gs.AddItem(g2.id, 1); Audio.Sfx("coin"); } OpenShop(); }));
                    }
                    l.Add(new UIEntry("< Kembali", () => TownUI.I.Close()));
                    return l;
                });
        }

        // ---- status efek pemain ----
        static float pPoisonUntil, pBurnUntil, pFreezeUntil, pStunUntil, pImmuneUntil, pDps, pStunImmune;
        public static void Cleanse(float immune)
        {
            pPoisonUntil = pBurnUntil = pFreezeUntil = pStunUntil = 0f; pImmuneUntil = Time.time + immune;
        }
        public static float PlayerMoveMul()
        {
            if (Time.time < pStunUntil) return 0f;
            return Time.time < pFreezeUntil ? 0.45f : 1f;
        }
        public static string PlayerStatusText()
        {
            string s = "";
            if (Time.time < pPoisonUntil) s += "RACUN " + Mathf.CeilToInt(pPoisonUntil - Time.time) + "d  ";
            if (Time.time < pBurnUntil) s += "TERBAKAR " + Mathf.CeilToInt(pBurnUntil - Time.time) + "d  ";
            if (Time.time < pFreezeUntil) s += "BEKU " + Mathf.CeilToInt(pFreezeUntil - Time.time) + "d  ";
            if (Time.time < pStunUntil) s += "STUN  ";
            return s;
        }

        /// <summary>Dipanggil Monster.Update setelah serangan mengenai pemain.</summary>
        public static void OnMonsterHit(MonsterDef d, bool boss)
        {
            var pc = PlayerCombat.I; if (pc == null || d == null || pc.IsDead || Time.time < pImmuneUntil) return;
            if (pc.Guarding) return;                                   // tangkisan menahan status juga
            Elem e = OfDef(d);
            string n = ((d.name ?? "") + " " + (d.shape ?? "")).ToLowerInvariant();
            float k = boss ? 1.5f : 1f;
            float atk = Mathf.Max(4f, d.atk);
            if (Has(n, "scorpion", "kalajengking", "spider", "laba", "ular", "snake", "cultist", "dukun") && Random.value < 0.35f * k) PlayerPoison(6f, atk * 0.12f);
            else if (e == Elem.Api && Random.value < 0.25f * k) PlayerBurn(4f, atk * 0.15f);
            else if (e == Elem.Air && Random.value < (Has(n, "frost", "salju") ? 0.3f : 0.12f) * k) PlayerFreeze(boss ? 1.5f : 2.5f);
            else if (e == Elem.Tanah && (d.large || boss) && Time.time > pStunImmune && Random.value < 0.14f * k) PlayerStun(boss ? 0.8f : 1.1f);
            else if (e == Elem.Gelap && Random.value < 0.18f * k) PlayerPoison(5f, atk * 0.1f);
        }
        static void PlayerPoison(float dur, float dps) { Audio.Sfx("status_hit"); pPoisonUntil = Time.time + dur; pDps = Mathf.Max(pDps, dps); Toast("Kamu keracunan!"); }
        static void PlayerBurn(float dur, float dps) { Audio.Sfx("status_hit"); pBurnUntil = Time.time + dur; pDps = Mathf.Max(pDps, dps); Toast("Kamu terbakar!"); }
        static void PlayerFreeze(float dur) { Audio.Sfx("status_hit"); pFreezeUntil = Time.time + dur; Toast("Kamu membeku! Gerak melambat."); var pc = PlayerCombat.I; if (pc != null) pc.LockAttack(dur * 0.5f); }
        static void PlayerStun(float dur) { Audio.Sfx("status_hit"); pStunUntil = Time.time + dur; pStunImmune = Time.time + dur + 6f; Toast("STUN!"); var pc = PlayerCombat.I; if (pc != null) pc.LockAttack(dur); }

        static float pTick;
        internal static void TickPlayer()
        {
            var pc = PlayerCombat.I; if (pc == null || pc.IsDead) return;
            bool dot = Time.time < pPoisonUntil || Time.time < pBurnUntil;
            if (!dot) { pDps = 0f; return; }
            if (Time.time < pTick) return;
            pTick = Time.time + 0.5f;
            bool burn = Time.time < pBurnUntil;
            pc.DotDamage(pDps * 0.5f, burn ? Cols[(int)Elem.Api] : new Color(0.5f, 0.9f, 0.3f));
            CombatFx.Burst(pc.transform.position + Vector3.up * 1.2f, burn ? Cols[(int)Elem.Api] : new Color(0.5f, 0.9f, 0.3f), 4, 0.4f, 1f, 0.5f, 0.1f);
        }

        // ---- Hindar Sempurna ----
        static float dodgeBuffUntil;
        public static void PerfectDodge()
        {
            var pc = PlayerCombat.I; if (pc == null) return;
            dodgeBuffUntil = Time.time + 4f;
            pc.stamina = Mathf.Min(pc.MaxStamina, pc.stamina + 25f);
            CombatFx.PopText(pc.transform.position + Vector3.up * 2.4f, "HINDAR SEMPURNA!", new Color(0.6f, 1f, 1f));
            CombatFx.ShockRing(pc.transform.position - Vector3.up * 0.9f, 3f, new Color(0.6f, 1f, 1f, 0.8f), 0.3f);
            Audio.Sfx("perfect_dodge", pc.transform.position);
            if (Time.timeScale > 0.9f && CombatFx.Host.I != null) CombatFx.Host.I.StartCoroutine(SlowMo());
        }
        static IEnumerator SlowMo()
        {
            Time.timeScale = 0.35f;
            float t = 0f;
            while (t < 0.45f) { t += Time.unscaledDeltaTime; if (Time.timeScale == 0f) yield break; yield return null; }
            if (Time.timeScale > 0f && Time.timeScale < 0.9f) Time.timeScale = 1f;
        }

        // ---- serangan pemain mengenai monster: kembalikan pengali damage ----
        public static float Hit(Monster m, float dmg)
        {
            if (m == null || m.IsDead) return 1f;
            var mc = MonCombat.Of(m);
            if (mc.engagedAt < 0f) mc.engagedAt = Time.time;
            Elem a = PlayerElem();
            float mul = 1f;
            bool weak = false;
            if (a != Elem.None && mc.elem != Elem.None)
            {
                if (WeakTo(mc.elem) == a) { mul = WeakMul; weak = true; }
                else if (mc.elem == a) mul = ResistMul;
            }
            if (Time.time < dodgeBuffUntil) mul *= PerfectDodgeBuff;
            if (Time.time < mc.crackUntil) mul *= 1.2f;
            if (Time.time < mc.markUntil && PlayerCombat.I != null) PlayerCombat.I.Heal(Mathf.Max(1f, dmg * mul * 0.04f));
            if (Time.time < mc.breakUntil) mul *= BreakMul;
            else if (Time.time >= mc.breakImmune)
            {
                mc.gauge += weak ? (m.isBoss ? 11f : 25f) : (m.isBoss ? 2f : 5f);
                if (mc.gauge >= 100f)
                {
                    mc.gauge = 0f;
                    float dur = m.isBoss ? 3f : 4f;
                    mc.breakUntil = Time.time + dur; mc.breakImmune = Time.time + dur + (m.isBoss ? 15f : 8f);
                    m.Stagger(dur);
                    CombatFx.PopText(m.HeadPoint + Vector3.up * 1.4f, "BREAK!", new Color(1f, 0.9f, 0.2f));
                    CombatFx.ShockRing(m.HitPoint, 3.5f, new Color(1f, 0.9f, 0.3f, 0.9f), 0.35f);
                    Audio.Sfx("break", m.transform.position);
                }
            }
            if (Time.time >= mc.popAt)
            {
                if (weak) { mc.popAt = Time.time + 0.9f; CombatFx.PopText(m.HeadPoint + Vector3.up * 1.0f, "LEMAH! (" + Names[(int)mc.elem] + ")", Cols[(int)a]); }
                else if (mul < 1f && a != Elem.None) { mc.popAt = Time.time + 0.9f; CombatFx.PopText(m.HeadPoint + Vector3.up * 1.0f, "TAHAN", new Color(0.7f, 0.7f, 0.7f)); }
            }
            Proc(m, mc, a, dmg);
            return mul;
        }

        // ---- konteks skill: skill penyihir yang baru dilepas pasti memasang status elemennya pada tiap target (sekali per target per cast) ----
        static int castSerial; static float castUntil, castPotency = 1f;
        public static void OnSkillCast(SkillDef s)
        {
            var gs = GameState.I; if (gs == null || s == null || !gs.IsMage) return;
            castSerial++; castUntil = Time.time + 1.6f;
            castPotency = s.tier >= 3 ? 1.6f : s.tier == 2 ? 1.15f : 1f;
        }

        /// <summary>Efek status KHAS tiap elemen, dengan reaksi antar elemen:
        /// Api = Terbakar bertumpuk (maks 3), bila target Basah jadi UAP (ledakan area, basah hilang).
        /// Air = Basah (lambat), 3 tumpuk = Beku; memadamkan bakar.
        /// Tanah = Retak (target menerima damage +20%), 3 tumpuk = Stun.
        /// Angin = Gores (pendarahan + lambat); bila target Basah jadi KONDUKSI (stun + sambaran ke musuh dekat).
        /// Cahaya = Tanda Suci (8 dtk: tiap damage ke target ini memulihkan HP-mu 4%).</summary>
        static void ApplyElement(Monster m, MonCombat mc, Elem a, float atk, float pot)
        {
            Vector3 top = m.HeadPoint + Vector3.up * 1.2f;
            if (Time.time > mc.wetUntil) mc.wetStacks = 0;
            if (Time.time > mc.crackUntil) mc.crackStacks = 0;
            switch (a)
            {
                case Elem.Api:
                    if (Time.time < mc.wetUntil)
                    {
                        mc.wetUntil = 0f; mc.wetStacks = 0;
                        CombatFx.PopText(top, "UAP!", new Color(0.85f, 0.9f, 1f)); CombatFx.ShockRing(m.HitPoint, 4f, new Color(0.85f, 0.9f, 1f, 0.8f), 0.4f);
                        foreach (var o in new List<Monster>(Monster.All)) if (o != null && !o.IsDead && Vector3.Distance(o.transform.position, m.transform.position) < 4f) { o.TakeDamage(atk * 0.8f * pot, false); o.Slow(0.7f, 2.5f); }
                    }
                    else
                    {
                        mc.burnStacks = Mathf.Min(3, mc.burnStacks + 1);
                        mc.Burn(4f * pot, atk * 0.1f * mc.burnStacks);
                        CombatFx.PopText(top, "TERBAKAR x" + mc.burnStacks, Cols[(int)Elem.Api]);
                    }
                    break;
                case Elem.Air:
                    mc.wetStacks++; mc.wetUntil = Time.time + 8f * pot; m.Slow(0.8f, 3f);
                    mc.burnStacks = 0; mc.ClearBurn();
                    if (mc.wetStacks >= 3) { mc.wetStacks = 0; mc.wetUntil = 0f; if (Time.time > mc.ccReady) mc.Freeze(m.isBoss ? 0.9f : 1.8f * pot); }
                    else CombatFx.PopText(top, "BASAH x" + mc.wetStacks, Cols[(int)Elem.Air]);
                    break;
                case Elem.Tanah:
                    mc.crackStacks++; mc.crackUntil = Time.time + 7f * pot;
                    if (mc.crackStacks >= 3) { mc.crackStacks = 0; if (Time.time > mc.ccReady) mc.Stun(m.isBoss ? 0.7f : 1.1f * pot); }
                    else CombatFx.PopText(top, "RETAK x" + mc.crackStacks + " (+20% dmg)", Cols[(int)Elem.Tanah]);
                    break;
                case Elem.Angin:
                    if (Time.time < mc.wetUntil)
                    {
                        mc.wetUntil = 0f; mc.wetStacks = 0;
                        CombatFx.PopText(top, "KONDUKSI!", new Color(0.7f, 0.85f, 1f));
                        if (Time.time > mc.ccReady) mc.Stun(m.isBoss ? 0.7f : 1.2f);
                        int chain = 0;
                        foreach (var o in new List<Monster>(Monster.All))
                            if (o != null && o != m && !o.IsDead && chain < 2 && Vector3.Distance(o.transform.position, m.transform.position) < 6f)
                            { chain++; CombatFx.Beam(m.HitPoint, o.HitPoint, new Color(0.7f, 0.85f, 1f), 0.15f, 0.2f); o.TakeDamage(atk * 0.5f * pot, false); }
                    }
                    else
                    {
                        mc.Bleed(5f * pot, atk * 0.08f); m.Slow(0.7f, 3f);
                        CombatFx.PopText(top, "GORES", Cols[(int)Elem.Angin]);
                    }
                    break;
                case Elem.Cahaya:
                    mc.markUntil = Time.time + 8f * pot;
                    CombatFx.PopText(top, "TANDA SUCI", Cols[(int)Elem.Cahaya]);
                    break;
            }
        }

        static void Proc(Monster m, MonCombat mc, Elem a, float dmg)
        {
            var gs = GameState.I; if (gs == null) return;
            float atk = Mathf.Max(5f, gs.TotalAtk);
            if (PoisonCoat) mc.Poison(6f, atk * 0.1f + 2f);
            if (a == Elem.None) return;
            bool inCast = gs.IsMage && Time.time < castUntil;
            if (inCast && mc.lastCast != castSerial) { mc.lastCast = castSerial; ApplyElement(m, mc, a, atk, castPotency); return; }       // skill: status pasti terpasang
            float chance = a == Elem.Air ? 0.25f : a == Elem.Angin ? 0.20f : a == Elem.Tanah ? 0.12f : 0.15f;
            if (gs.IsMage) chance *= 0.6f;               // serangan dasar penyihir lebih jarang; kekuatan status ada di skill
            if (Random.value < chance) ApplyElement(m, mc, a, atk, 1f);
        }

        // ---- boss: ganti elemen tiap fase ----
        static readonly Dictionary<string, Elem[]> bossPhases = new Dictionary<string, Elem[]>
        {
            { "ogre_chief", new[] { Elem.Tanah, Elem.Tanah, Elem.Api } },
            { "ignarok", new[] { Elem.Api, Elem.Tanah, Elem.Api } },
            { "bone_king", new[] { Elem.Gelap, Elem.Gelap, Elem.Api } },
            { "demon_lord", new[] { Elem.Gelap, Elem.Api, Elem.Gelap, Elem.Api } },
            { "behemoth", new[] { Elem.Tanah, Elem.Angin, Elem.Tanah } },
            { "cerberus", new[] { Elem.Api, Elem.Gelap, Elem.Api } },
        };

        public static void OnBossPhase(Monster m, int ph)
        {
            if (m == null) return;
            var mc = MonCombat.Of(m);
            Elem[] arr; Elem ne;
            if (bossPhases.TryGetValue(m.DefId, out arr)) ne = arr[Mathf.Clamp(ph, 0, arr.Length - 1)];
            else { ne = mc.baseElem; for (int i = 0; i < ph; i++) ne = Next(ne); }
            bool changed = ne != mc.elem;
            mc.elem = ne; mc.gauge = 0f; mc.breakUntil = 0f;
            m.hp = Mathf.Min(m.MaxHp, m.hp + m.MaxHp * 0.03f);
            CombatFx.ShockRing(m.transform.position + Vector3.up * 0.2f, 9f, Cols[(int)ne], 0.5f);
            if (changed) Toast(m.Name + " berubah: elemen " + Names[(int)ne] + (WeakTo(ne) != Elem.None ? " (lemah " + Names[(int)WeakTo(ne)] + ")" : ""));
            else Toast("Fase " + (ph + 1) + "!");
            var pc = PlayerCombat.I;
            if (pc != null && !pc.IsDead && ph > 0 && Vector3.Distance(pc.transform.position, m.transform.position) < 8f) pc.TakeDamage(m.Atk * 0.4f, m.transform.position);   // gelombang kejut saat fase berganti
        }
    }

    /// <summary>Data tempur per monster: elemen, gauge Break, status efek.</summary>
    public class MonCombat : MonoBehaviour
    {
        public Elem elem, baseElem;
        public float gauge, breakUntil, breakImmune, popAt, ccReady, engagedAt = -1f;
        public float wetUntil, crackUntil, markUntil; public int wetStacks, crackStacks, burnStacks, lastCast;
        float bleedUntil, bleedDps;
        float poisonUntil, poisonDps, burnUntil, burnDps, tick;
        Monster m;

        public static MonCombat Of(Monster m)
        {
            var c = m.GetComponent<MonCombat>();
            if (c == null)
            {
                c = m.gameObject.AddComponent<MonCombat>();
                c.m = m; c.baseElem = c.elem = Combat2.OfDef(MonsterDB.Get(m.DefId));
            }
            return c;
        }

        public void Bleed(float dur, float dps) { bleedUntil = Time.time + dur; bleedDps = Mathf.Max(bleedDps, dps); }
        public void ClearBurn() { burnUntil = 0f; burnDps = 0f; }
        public void Poison(float dur, float dps) { poisonUntil = Time.time + dur; poisonDps = Mathf.Max(poisonDps, dps); }
        public void Burn(float dur, float dps) { burnUntil = Time.time + dur; burnDps = Mathf.Max(burnDps, dps); }
        public void Freeze(float dur) { ccReady = Time.time + dur + (m.isBoss ? 12f : 5f); m.Slow(0.3f, dur + 1f); m.Stagger(Mathf.Min(dur, 1.4f)); CombatFx.PopText(m.HeadPoint + Vector3.up * 1.2f, "BEKU", Combat2.Cols[(int)Elem.Air]); }
        public void Stun(float dur) { ccReady = Time.time + dur + (m.isBoss ? 12f : 5f); m.Stagger(dur); CombatFx.PopText(m.HeadPoint + Vector3.up * 1.2f, "STUN", new Color(1f, 0.95f, 0.4f)); }

        void Update()
        {
            if (m == null || m.IsDead) return;
            if (Time.time < tick) return;
            tick = Time.time + 0.5f;
            if (Time.time >= burnUntil) { burnDps = 0f; burnStacks = 0; }
            if (Time.time >= poisonUntil) poisonDps = 0f;
            if (Time.time >= bleedUntil) bleedDps = 0f;
            if (Time.time < poisonUntil) { CombatFx.Burst(m.HitPoint, new Color(0.5f, 0.9f, 0.3f), 4, 0.4f, 1f, 0.5f, 0.1f); m.TakeDamage(poisonDps * 0.5f, false); }
            if (m != null && !m.IsDead && Time.time < bleedUntil) { CombatFx.Burst(m.HitPoint, new Color(0.9f, 0.2f, 0.25f), 3, 0.4f, 1f, 0.4f, 0.08f); m.TakeDamage(bleedDps * 0.5f, false); }
            if (m != null && !m.IsDead && Time.time < burnUntil) { CombatFx.Burst(m.HitPoint, Combat2.Cols[(int)Elem.Api], 4, 0.4f, 1f, 0.5f, 0.1f); m.TakeDamage(burnDps * 0.5f, false); }
        }
    }

    /// <summary>Pemicu status pemain + panel info boss/status (OnGUI).</summary>
    public class Combat2Host : MonoBehaviour
    {
        static Combat2Host inst;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { if (inst != null) return; var go = new GameObject("Combat2Host"); DontDestroyOnLoad(go); inst = go.AddComponent<Combat2Host>(); }

        GUIStyle st; Texture2D tex;
        void Update() { Combat2.TickPlayer(); }

        void OnGUI()
        {
            if (GameState.I == null || !SaveSystem.Ready) return;
            if (st == null)
            {
                st = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                tex = new Texture2D(1, 1); tex.SetPixel(0, 0, Color.white); tex.Apply();
            }
            float sc = Screen.height / 1080f;
            st.fontSize = Mathf.RoundToInt(20 * sc);
            string ps = Combat2.PlayerStatusText();
            string cs = Combat2.CoatText;
            if (ps != "" || cs != "")
            {
                st.normal.textColor = ps != "" ? new Color(1f, 0.55f, 0.4f) : new Color(0.8f, 0.95f, 1f);
                GUI.Label(new Rect(Screen.width * 0.5f - 300 * sc, Screen.height - 150 * sc, 600 * sc, 30 * sc), (ps + cs).Trim(), st);
            }
            // panel boss terdekat yang sedang bertarung
            Monster best = null; float bd = 45f;
            var pc = PlayerCombat.I; if (pc == null) return;
            foreach (var m in Monster.All)
            {
                if (m == null || m.IsDead || !m.isBoss || !m.InCombat && Vector3.Distance(m.transform.position, pc.transform.position) > 25f) continue;
                float d = Vector3.Distance(m.transform.position, pc.transform.position);
                if (d < bd) { bd = d; best = m; }
            }
            if (best == null) return;
            var mc = MonCombat.Of(best);
            float w = 460 * sc, x = Screen.width * 0.5f - w * 0.5f, y = 14 * sc;
            Elem wk = Combat2.WeakTo(mc.elem);
            GUI.color = new Color(0f, 0f, 0f, 0.55f); GUI.DrawTexture(new Rect(x, y, w, 62 * sc), tex);
            GUI.color = Color.white;
            st.normal.textColor = Color.white; st.fontSize = Mathf.RoundToInt(18 * sc);
            var bb = best.GetComponent<BossBrain>();
            string head = best.Name + (bb != null ? "   Fase " + (bb.Phase + 1) + "/" + bb.PhaseCount : "");
            GUI.Label(new Rect(x, y, w, 24 * sc), head, st);
            st.normal.textColor = Combat2.Cols[(int)mc.elem]; st.fontSize = Mathf.RoundToInt(16 * sc);
            GUI.Label(new Rect(x, y + 22 * sc, w, 22 * sc), "Elemen: " + Combat2.Names[(int)mc.elem] + (wk != Elem.None ? "    Lemah: " + Combat2.Names[(int)wk] : ""), st);
            float frac = Time.time < mc.breakUntil ? 1f : Mathf.Clamp01(mc.gauge / 100f);
            GUI.color = new Color(0.15f, 0.15f, 0.15f, 0.9f); GUI.DrawTexture(new Rect(x + 20 * sc, y + 48 * sc, w - 40 * sc, 8 * sc), tex);
            GUI.color = Time.time < mc.breakUntil ? new Color(1f, 0.9f, 0.2f) : new Color(1f, 0.6f, 0.2f); GUI.DrawTexture(new Rect(x + 20 * sc, y + 48 * sc, (w - 40 * sc) * frac, 8 * sc), tex);
            GUI.color = Color.white;
        }
    }
}
