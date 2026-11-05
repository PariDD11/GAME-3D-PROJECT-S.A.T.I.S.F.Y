using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Rekomendasi skill yang dibawa (7 slot: Z X C V R T Q) untuk job + evolusi pemain saat ini, ideal untuk raid / bos.
    /// Dihitung dari data skill (jenis, area, cooldown, efek kontrol), jadi otomatis mengikuti semua job dan evolusi ke-1/ke-2.
    /// </summary>
    public static class SkillLoadout
    {
        public class Pick { public int slot; public string key, role, reason; public SkillDef skill; public bool learned; }

        static readonly string[] Keys = { "Z", "X", "C", "V", "R", "T", "Q" };

        static string Txt(SkillDef s) { return ((s.name ?? "") + " " + (s.desc ?? "")).ToLowerInvariant(); }
        static bool Any(SkillDef s, params string[] w) { string t = Txt(s); foreach (var x in w) if (t.Contains(x)) return true; return false; }

        static bool IsSupport(SkillDef s) { return s.kind == "heal" || s.kind == "buff" || Any(s, "heal", "pulih", "sembuh", "shield", "perisai", "lindung", "barrier", "guard", "regen"); }
        static bool IsControl(SkillDef s) { return s.kind == "trap" || s.HasFx("slow") || s.HasFx("stagger") || s.HasFx("knock") || s.HasFx("stun") || s.HasFx("freeze") || s.HasFx("root") || Any(s, "slow", "stun", "bekuk", "freeze", "pelamb", "tahan", "kunci", "jebak"); }
        static bool IsMobility(SkillDef s) { return s.kind == "dash" || Any(s, "dash", "lompat", "teleport", "meluncur", "melesat", "step", "blink"); }
        static bool IsAoe(SkillDef s) { return s.kind == "nova" || s.kind == "ground" || s.kind == "cone" || s.kind == "multi" || s.radius >= 3f || Any(s, "area", "sekitar", "semua musuh", "ledak", "gelombang"); }
        static float Dmg(SkillDef s) { return s.mult > 0f ? s.mult : 1f; }

        public static List<SkillDef> Pool(GameState gs)
        {
            var l = new List<SkillDef>();
            foreach (var s in SkillDB.All)
                if (s.job == gs.job && s.active && (string.IsNullOrEmpty(s.evo) || s.evo == gs.evo || s.evo == gs.evo2)) l.Add(s);
            return l;
        }

        // Pilih skill terbaik untuk satu peran; yang sudah dipelajari didahulukan, lalu yang belum.
        static SkillDef Best(List<SkillDef> pool, HashSet<string> used, GameState gs, System.Func<SkillDef, bool> fit, System.Func<SkillDef, float> score)
        {
            SkillDef best = null; float bs = float.MinValue;
            foreach (var pass in new[] { true, false })
            {
                foreach (var s in pool)
                {
                    if (used.Contains(s.id) || s.tier < 2 || s.tier >= 3 && s.active && IsUlt(s)) continue;
                    if (gs.Knows(s.id) != pass || !fit(s)) continue;
                    float v = score(s) + (!string.IsNullOrEmpty(s.evo) ? 0.3f : 0f);
                    if (v > bs) { bs = v; best = s; }
                }
                if (best != null) return best;
            }
            return null;
        }
        static bool IsUlt(SkillDef s) { return s.tier >= 3; }

        public static List<Pick> Recommend(GameState gs)
        {
            var pool = Pool(gs); var used = new HashSet<string>(); var res = new List<Pick>();

            // V = ultimate: prioritas evolusi ke-2 > evolusi ke-1 > job; yang sudah dipelajari dulu
            SkillDef ult = null; float us = float.MinValue;
            foreach (var pass in new[] { true, false })
            {
                foreach (var s in pool)
                {
                    if (s.tier < 3 || gs.Knows(s.id) != pass) continue;
                    float v = (s.evo == gs.evo2 && !string.IsNullOrEmpty(gs.evo2) ? 20f : !string.IsNullOrEmpty(s.evo) ? 10f : 0f) + Dmg(s);
                    if (v > us) { us = v; ult = s; }
                }
                if (ult != null) break;
            }

            var z = Best(pool, used, gs, s => s.tier == 2 && !IsSupport(s), s => 10f - s.cooldown + Dmg(s) * 0.5f);   // pembuka cepat
            if (z != null) used.Add(z.id);
            var x = Best(pool, used, gs, s => !IsSupport(s) && !IsAoe(s), s => Dmg(s) * 2f + (s.cooldown > 6 ? 1f : 0f));  // burst target tunggal
            if (x != null) used.Add(x.id);
            var c = Best(pool, used, gs, s => !IsSupport(s) && IsAoe(s), s => Dmg(s) * 1.5f + s.radius * 0.4f);            // area
            if (c != null) used.Add(c.id);
            var r = Best(pool, used, gs, IsSupport, s => (s.kind == "heal" ? 3f : 1f) + Dmg(s) * 0.1f);                    // pemulihan / pertahanan
            if (r != null) used.Add(r.id);
            var t = Best(pool, used, gs, IsControl, s => s.Fx("slow") + s.Fx("stagger") + s.Fx("knock") * 0.2f + (s.kind == "trap" ? 1f : 0f));  // kontrol
            if (t != null) used.Add(t.id);
            var q = Best(pool, used, gs, IsMobility, s => 10f - s.cooldown);                                              // gerak / menghindar
            if (q == null) q = Best(pool, used, gs, s => true, s => Dmg(s));
            if (q != null) used.Add(q.id);

            Add(res, 0, z, "Pembuka cepat", "Cooldown pendek: dipakai terus di sela serangan biasa untuk menjaga damage tetap berjalan.", gs);
            Add(res, 1, x, "Burst target tunggal", "Damage besar ke satu target. Simpan untuk bos saat ia terbuka / selesai menyerang.", gs);
            Add(res, 2, c, "Serangan area", "Membersihkan anak buah bos dan kerumunan monster sekaligus.", gs);
            Add(res, 3, ult, "ULTIMATE", "Damage terbesar. Pakai saat bos lengah atau di fase kritis, bukan di awal.", gs);
            Add(res, 4, r, "Bertahan / pulih", "Penyelamat saat HP menipis atau menahan serangan besar bos.", gs);
            Add(res, 5, t, "Kontrol", "Memperlambat / menahan musuh supaya kamu punya waktu mundur atau menyerang.", gs);
            Add(res, 6, q, "Gerak / menghindar", "Keluar dari area serangan bos dengan cepat; kunci untuk selamat di raid.", gs);
            return res;
        }

        static void Add(List<Pick> l, int slot, SkillDef s, string role, string reason, GameState gs)
        {
            l.Add(new Pick { slot = slot, key = Keys[slot], role = role, reason = reason, skill = s, learned = s != null && gs.Knows(s.id) });
        }

        /// <summary>Pasang semua rekomendasi yang sudah dipelajari. Mengembalikan jumlah slot terpasang.</summary>
        public static int Apply(GameState gs)
        {
            int n = 0;
            foreach (var p in Recommend(gs))
                if (p.skill != null && p.learned) { gs.AssignSkill(p.slot, p.skill.id); n++; }
            return n;
        }

        public static string[] RaidTips(GameState gs)
        {
            return new[]
            {
                "Bawa Botol Minum penuh dan isi quickbar 1-6 dengan ramuan HP / stamina sebelum masuk raid.",
                "Pelajari pola bos dulu: serang saat bos selesai menyerang, lalu mundur dengan skill gerak (Q).",
                "Ultimate (V) dipakai di fase kritis atau saat bos lemah, jangan dihabiskan di awal.",
                "Skill kontrol (T) dan pertahanan (R) menyelamatkan nyawa; jangan ditinggal demi damage saja.",
            };
        }
    }
}
