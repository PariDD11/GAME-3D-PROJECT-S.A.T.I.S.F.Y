using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Pementasan makhluk mitologi: pemanggilan (lingkaran sihir + pilar cahaya + sigil di tanah), kemunculan membesar, AUMAN, lalu menyerang
    /// dengan gaya khas tiap makhluk (menerkam, menyambar, menghentak, melompat, melilit, jatuh dari langit, atau berdiri merapal), meninggalkan
    /// bekas di tanah (jejak kaki, cakar, kawah, retakan, es, hangus, sigil), lalu lenyap menjadi percikan cahaya.
    /// </summary>
    public static partial class SkillSilhouette
    {
        static Dictionary<string, MySpec> mySpecs;

        // id|bentuk|profil|skala|gerak|serangan|warna|fitur
        static readonly string[] MyRows =
        {
            "sw_off_6|q|wolf|1.6|run|bite|ice|chain,bushy", "sw_def_6|t|xuanwu|1.7|drop|quake||", "sw_mob_6|q|horse|1.6|run|sweep||legs8,mane,flame",
            "ar_pre_6|b|garuda|1.7|fly|talon|gold|crown,sun", "ar_utl_6|o|ouro|1.6|cast|bind||", "ar_mob_6|b|roc|2.0|fly|gust||",
            "fm_inf_6|q|lion|1.6|leap|volley|red|human,scorp,batwing", "fm_stp_6|q|wolf|1.5|run|breath|red|flame,bushy",
            "em_stn_6|q|bull|2.1|stomp|quake||tusk,armor,spikes", "em_bed_6|t|akupara|1.9|drop|quake||", "em_flw_6|q|lion|1.7|stomp|stomp||legs6,shell,spikes",
            "wm_tid_6|k|kraken|1.7|coil|tide||", "wm_fst_6|q|wolf|1.6|run|howl|ice|sun,bushy", "wm_mst_6|q|horse|1.6|leap|radiance|white|onehorn,scales,mane",
            "wd_gal_6|b|anzu|1.7|fly|gust||", "wd_zep_6|q|horse|1.6|leap|gust|white|wing,mane", "wd_rzr_6|q|griffin|1.7|fly|slash||",
            "pr_rad_6|q|lamassu|1.7|cast|radiance|gold|human,beard,crown,legs5", "pr_mer_6|b|heron|1.7|cast|radiance|gold|crest,sun", "pr_aeg_6|q|sphinx|1.7|cast|gaze|gold|human,nemes,wing",
            "kn_6|h|minotaur|1.7|stomp|gore||", "bz_6|q|wolf|1.6|run|bite|red|blood,chain", "bm_6|h|tengu|1.5|fly|slash||",
            "ms_6|b|thunder|1.9|fly|bolt|blue|bolt", "sh_6|b|raven|1.5|fly|swarm|dark|", "py_6|d|typhon|1.8|fly|breath|red|",
            "ik_6|q|wolf|1.6|run|breath|red|heads3,flame", "ge_6|s|midgard|1.6|coil|quake||", "gm_6|q|bull|1.8|stomp|gaze||lowhead,spikes",
            "tg_6|q|baku|1.7|stomp|quake||", "cr_6|d|nidhogg|1.7|fly|breath|ice|", "sm_6|q|wolf|1.5|run|bolt|blue|bolt",
            "tl_6|m|maw|1.6|cast|tide||", "bw_6|q|hippogriff|1.7|fly|talon||", "sd_6|q|qilin|1.6|leap|radiance|red|antler,flame,scales",
            "pl_6|q|lion|1.7|leap|bite|gold|mane,armor", "sa_6|q|horse|1.6|leap|pierce|white|spiral,mane", "cl_6|q|stag|1.7|leap|radiance|white|antler,halo",
            "e30_0_6|q|ram|1.7|run|gore|gold|ramhorn,wool,golden", "e30_1_6|y|hydra|1.6|coil|bind||", "e30_3_6|b|simurgh|1.8|fly|pierce|gold|plumes,crest",
            "e30_4_6|b|flock|1.6|fly|swarm||", "e30_5_6|h|anubis|1.6|cast|gaze|gold|", "e30_6_6|b|crow3|1.6|fly|breath|red|fire",
            "e30_8_6|q|chimera|1.7|stomp|breath|red|mane,goathead,snaketail,wing,flame", "e30_9_6|t|zaratan|2.1|drop|quake||",
            "e30_10_6|d|bahamut|2.0|cast|breath|white|", "e30_11_6|q|stag|1.8|leap|radiance|green|tree", "e30_12_6|d|fafnir|1.7|fly|breath|ice|",
            "e30_14_6|q|elephant|1.9|stomp|bolt|white|tusk4", "e30_15_6|w|wyvern|1.6|fly|gust||", "e30_16_6|q|weasel|1.5|run|slash||sickle",
            "e30_17_6|q|horse|1.7|leap|pierce|white|spiral,wing,mane", "e30_18_6|r|tetra|1.6|cast|radiance|gold|", "e30_19_6|b|ziz|2.3|fly|gust|gold|sun",
            "e30_20_6|q|stag|1.6|leap|radiance|gold|goldantler,halo",
        };

        static void MyInit()
        {
            if (mySpecs != null) return;
            mySpecs = new Dictionary<string, MySpec>();
            foreach (var r in MyRows)
            {
                var p = r.Split('|');
                if (p.Length < 8) continue;
                var sp = new MySpec { id = p[0], bas = p[1], prof = p[2], mot = p[4], atk = p[5], tone = p[6], f = p[7].Length == 0 ? new string[0] : p[7].Split(',') };
                float.TryParse(p[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out sp.scale);
                if (sp.scale < 0.5f) sp.scale = 1.5f;
                mySpecs[sp.id] = sp;
            }
        }

        internal static bool MyHas(string id) { MyInit(); return mySpecs.ContainsKey(id); }

        static Color MyTone(string t)
        {
            switch (t)
            {
                case "ice": return new Color(0.6f, 0.92f, 1f);
                case "gold": return new Color(1f, 0.82f, 0.3f);
                case "red": return new Color(1f, 0.32f, 0.2f);
                case "blue": return new Color(0.45f, 0.65f, 1f);
                case "white": return new Color(0.95f, 0.97f, 1f);
                case "dark": return new Color(0.65f, 0.35f, 0.95f);
                case "green": return new Color(0.5f, 1f, 0.55f);
            }
            return Color.clear;
        }

        /// <summary>Dipanggil dari BeastStrike. true bila skill ini punya makhluk khusus.</summary>
        static bool MyStrike(SkillDef s, PlayerCombat pc, Col col, float delay)
        {
            MyInit();
            MySpec sp;
            if (!mySpecs.TryGetValue(s.id, out sp)) return false;
            Color tn = MyTone(sp.tone);
            if (tn.a > 0f)
            {
                Color m = Color.Lerp(col.m, tn, 0.6f);
                col = new Col { m = m, h = Color.Lerp(m, Color.white, 0.6f), k = Color.Lerp(m, Color.black, 0.5f) };
            }
            CombatFx.Host.I.StartCoroutine(MythR(sp, pc, col, delay));
            return true;
        }

        // ------------------------------------------------------------------ bantu
        static void MyPose(View v, MySpec sp, Vector3 pos, Vector3 dir, float k, float ph, float roar)
        {
            v.Drive(sp, pos, dir, sp.scale * 1.3f * k, ph, roar);
        }

        static Vector3 MyHeadW(Vector3 pos, Vector3 dir, float sc) { Vector3 a = View.LastAx.sqrMagnitude > 0.01f ? View.LastAx : dir; return pos + a * (myHead.x * sc * 1.3f) + Vector3.up * (myHead.y * sc * 1.3f); }

        static string MyFoot(MySpec sp)
        {
            switch (sp.bas)
            {
                case "q":
                    switch (sp.prof)
                    {
                        case "wolf": case "weasel": case "lion": case "baku": case "chimera": case "sphinx": return "paw";
                        case "griffin": case "hippogriff": return "claw";
                        case "elephant": return "crater";
                        default: return "hoof";
                    }
                case "h": return sp.prof == "minotaur" ? "crater" : "claw";
                case "b": case "d": case "w": return "claw";
                case "s": case "y": case "o": return "slither";
                case "t": return "crater";
                case "k": case "m": return "puddle";
                default: return "glyph";
            }
        }

        static void MyZigW(Vector3 a, Vector3 b, int seg, float amp, Color col, float w, float life)
        {
            Vector3 prev = a;
            for (int i = 1; i <= seg; i++)
            {
                Vector3 p = Vector3.Lerp(a, b, i / (float)seg) + (i < seg ? UnityEngine.Random.insideUnitSphere * amp : Vector3.zero);
                CombatFx.Beam(prev, p, col, w, life); prev = p;
            }
        }

        // ------------------------------------------------------------------ tumbukan khas makhluk + bekas tanah
        static void MythHit(MySpec sp, Vector3 head, Vector3 tp, Vector3 dir, Col c)
        {
            Vector3 up = Vector3.up, right = Vector3.Cross(up, dir), hp = tp + up * 1.2f;
            float s = sp.scale;
            CombatFx.HitSpark(hp, c.h, 1.4f, 12); CombatFx.ShockRing(tp, 4f * s / 1.5f, c.m, 0.3f);
            ThirdPersonCamera.Shake(0.4f, 0.22f);
            string tone = sp.tone;
            // sihir dikurangi: makhluknya yang jadi sorotan. Serangan non-fisik cukup satu sinar tipis dari kepala/mulut ke sasaran.
            switch (sp.atk) { case "breath": case "bolt": case "gaze": case "radiance": case "tide": case "howl": case "pierce": case "swarm": case "bind": case "gust": case "volley": case "quake": case "sweep":
                    CombatFx.Beam(head, hp, c.h, 0.5f, 0.3f); CombatFx.Flash(hp, c.h, 6f, 2f, 0.18f); MyMarks(sp, tp, dir, c); return; }
            switch (sp.atk)
            {
                case "bite":
                    CombatFx.Beam(tp + up * 5.5f + right * 1.0f - dir * 0.5f, tp + up * 0.6f + right * 0.15f, c.h, 0.6f, 0.28f);
                    CombatFx.Beam(tp + up * 5.5f - right * 1.0f - dir * 0.5f, tp + up * 0.6f - right * 0.15f, c.h, 0.6f, 0.28f);
                    CombatFx.Beam(tp - up * 0.1f + right * 1.1f - dir * 1.8f, tp + up * 0.9f + right * 0.15f, c.m, 0.45f, 0.28f);
                    CombatFx.Beam(tp - up * 0.1f - right * 1.1f - dir * 1.8f, tp + up * 0.9f - right * 0.15f, c.m, 0.45f, 0.28f);
                    for (int i = -1; i <= 1; i++) CombatFx.Beam(tp + right * i * 0.8f + up * 3.4f - dir * 1.4f, tp + right * i * 1.0f + up * 0.3f + dir * 1.6f, c.m, 0.35f, 0.3f);
                    CombatFx.Dust(tp, 10); break;
                case "breath":
                    for (int i = -4; i <= 4; i++) CombatFx.Beam(head, tp + right * i * 0.9f + dir * Mathf.Abs(i) * 0.6f + up * 0.6f, i % 2 == 0 ? c.h : c.m, 0.4f, 0.32f);
                    for (int i = 1; i <= 3; i++) CombatFx.Burst(Vector3.Lerp(head, tp, i / 3.5f), c.m, 12, 1.2f, 2.5f, 0.7f, 0.26f);
                    CombatFx.Flash(head, c.h, 12f, 3.5f, 0.25f); CombatFx.Flash(tp + up, c.m, 14f, 3f, 0.3f); break;
                case "talon":
                    for (int i = -2; i <= 2; i++) CombatFx.Beam(tp + right * i * 0.7f + up * 5.5f - dir * 1.5f, tp + right * i * 0.85f + up * 0.2f + dir * 1.8f, i == 0 ? c.h : c.m, 0.4f, 0.3f);
                    CombatFx.ShockRing(tp, 8f, c.h, 0.4f); CombatFx.Flash(tp + up * 2f, c.h, 10f, 3f, 0.25f); break;
                case "stomp":
                    for (int i = 0; i < 3; i++) CombatFx.ShockRing(tp, 6f + i * 3.5f, i % 2 == 0 ? c.m : c.h, 0.4f + i * 0.08f);
                    MagicFx.Crack(tp, 7.5f, 5f); MagicFx.Debri(tp, 5f, 14, 1.5f); ThirdPersonCamera.Shake(0.7f, 0.4f); break;
                case "gore":
                    CombatFx.Beam(tp - dir * 3.5f + up * 0.2f, tp + up * 2.2f - dir * 0.5f, c.m, 0.7f, 0.3f); CombatFx.Beam(tp + up * 2.2f - dir * 0.5f, tp + up * 6f + dir * 2.5f, c.h, 0.7f, 0.3f);
                    CombatFx.HalfWave(tp - dir * 1.5f, dir, 8f, c.m, 0.45f); MagicFx.Debri(tp, 4.5f, 12, 1.4f); ThirdPersonCamera.Shake(0.55f, 0.3f); break;
                case "bolt":
                    for (int i = 0; i < 3; i++) MyZigW(tp + up * 24f + right * (i - 1) * 3.5f, tp + right * (i - 1) * 1.2f, 7, 1.6f, i == 1 ? c.h : c.m, 0.45f, 0.3f);
                    CombatFx.Flash(tp + up * 3f, c.h, 18f, 4.5f, 0.25f); ThirdPersonCamera.Shake(0.5f, 0.3f); break;
                case "sweep":
                    CombatFx.HalfWave(tp - dir * 1f, dir, 10f, c.m, 0.45f); CombatFx.HalfWave(tp - dir * 1f + up * 0.6f, Quaternion.AngleAxis(25f, up) * dir, 9f, c.h, 0.4f);
                    for (int i = -3; i <= 3; i++) CombatFx.Beam(tp + right * i * 1.4f + up * 2.5f - dir * 3f, tp + right * i * 1.6f + up * 0.4f + dir * 3f, c.m, 0.3f, 0.3f); break;
                case "gaze":
                    CombatFx.Beam(head, hp, c.h, 0.75f, 0.45f); CombatFx.Beam(head, hp, c.m, 1.3f, 0.35f);
                    MagicFx.MagicCircle(tp, 5.5f, c.m, 1.0f); MagicFx.Column(tp, 1.3f, 10f, c.h, 0.8f); break;
                case "radiance":
                    MagicFx.Column(tp, 2.2f, 22f, c.h, 1.1f); CombatFx.Flash(tp + up * 3f, c.h, 22f, 5f, 0.4f);
                    for (int i = 0; i < 8; i++) { float a = i * Mathf.PI * 2f / 8f; Vector3 o = tp + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 4.5f; CombatFx.Beam(o + up * 16f, o, i % 2 == 0 ? c.m : c.h, 0.3f, 0.5f); }
                    CombatFx.ShockRing(tp, 9f, c.h, 0.55f); break;
                case "tide":
                    MagicFx.Column(tp, 2.6f, 13f, c.m, 0.9f); for (int i = 0; i < 3; i++) CombatFx.ShockRing(tp, 5f + i * 3f, i % 2 == 0 ? c.m : c.h, 0.4f + i * 0.1f);
                    CombatFx.Burst(tp + up * 2f, c.h, 40, 3f, 7f, 1.1f, 0.26f); MagicFx.Debri(tp, 4f, 8, 1f); break;
                case "quake":
                    for (int i = 0; i < 9; i++) { float a = i * Mathf.PI * 2f / 9f; MagicFx.Spike(tp + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (3f + (i % 2) * 2.2f), 4f + (i % 3), 0.8f, c.m, i * 0.025f); }
                    MagicFx.Crack(tp, 8f, 6f); for (int i = 0; i < 3; i++) CombatFx.ShockRing(tp, 6f + i * 4f, c.m, 0.45f); ThirdPersonCamera.Shake(0.8f, 0.5f); break;
                case "howl":
                    for (int i = 0; i < 4; i++) CombatFx.ShockRing(tp, 5f + i * 3.2f, i % 2 == 0 ? c.h : c.m, 0.35f + i * 0.1f);
                    CombatFx.Burst(tp + up * 2f, c.h, 36, 4.5f, 3f, 1.2f, 0.28f); CombatFx.Beam(head, hp, c.m, 0.6f, 0.3f); break;
                case "pierce":
                    CombatFx.Beam(head, tp + dir * 14f + up * 1.2f, c.m, 1.3f, 0.35f); CombatFx.Beam(head, tp + dir * 14f + up * 1.2f, c.h, 0.5f, 0.4f);
                    CombatFx.HitSpark(tp + up * 1.4f, c.h, 2.4f, 28); CombatFx.Flash(tp + up * 1.5f, c.h, 14f, 4f, 0.25f); break;
                case "swarm":
                    for (int i = 0; i < 16; i++)
                    {
                        Vector2 q = UnityEngine.Random.insideUnitCircle * 5f; Vector3 p = tp + new Vector3(q.x, 0f, q.y);
                        CombatFx.Beam(p + up * UnityEngine.Random.Range(9f, 16f) - dir * 4f, p, i % 3 == 0 ? c.h : c.m, 0.2f, 0.25f); CombatFx.HitSpark(p + up * 0.3f, c.h, 0.6f, 5);
                    }
                    break;
                case "slash":
                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 d = Quaternion.AngleAxis(i * 45f - 20f, up) * dir, tilt = Quaternion.AngleAxis((i % 2 == 0 ? 1 : -1) * 28f, dir) * d;
                        CombatFx.Beam(tp + up * 1.5f - tilt * 5.5f, tp + up * 1.5f + tilt * 5.5f, i % 2 == 0 ? c.h : c.m, 0.5f, 0.3f);
                    }
                    CombatFx.HitSpark(tp + up * 1.5f, c.h, 2.4f, 26); break;
                case "bind":
                    for (int i = 0; i < 7; i++) { float a = i * Mathf.PI * 2f / 7f; Vector3 o = tp + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 4.5f; CombatFx.Beam(o + up * 0.3f, tp + up * 1.6f, i % 2 == 0 ? c.m : c.h, 0.28f, 0.7f); }
                    CombatFx.Flash(tp + up, c.h, 12f, 3f, 0.35f); MagicFx.Column(tp, 1.4f, 7f, c.m, 0.6f); break;
                case "gust":
                    MagicFx.Tornado(tp, 3.2f, 9f, c.m, 1.3f); CombatFx.ShockRing(tp, 9f, c.h, 0.5f); for (int i = 0; i < 6; i++) CombatFx.Beam(tp + up * (1f + i) + right * 4f, tp + up * (1f + i) - right * 4f, c.h, 0.2f, 0.3f); break;
                case "volley":
                    for (int i = 0; i < 8; i++)
                    {
                        Vector2 q = UnityEngine.Random.insideUnitCircle * 5.5f; Vector3 p = tp + new Vector3(q.x, 0f, q.y);
                        CombatFx.Beam(p + up * 26f - dir * 12f, p, i % 2 == 0 ? c.h : c.m, 0.6f, 0.3f); CombatFx.ShockRing(p, 3f, c.m, 0.3f); CombatFx.Burst(p + up * 0.5f, c.h, 8, 0.8f, 3f, 0.6f, 0.2f);
                    }
                    break;
            }
            MyMarks(sp, tp, dir, c);
        }

        static void MyMarks(MySpec sp, Vector3 tp, Vector3 dir, Col c)
        {
            float s = sp.scale, R = 3.2f * s;
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            Color col = c.m;
            switch (sp.atk)
            {
                case "bite": GroundMark.Put("claw", tp, R, col, 16f, yaw); GroundMark.Put("pock", tp + dir * 1.5f, R * 0.8f, col, 14f); break;
                case "breath":
                    {
                        string k = sp.tone == "ice" ? "frost" : (sp.tone == "white" || sp.tone == "gold") ? "glyph" : sp.tone == "blue" ? "bolt" : "scorch";
                        GroundMark.Put(k, tp, R * 1.15f, col, 18f); GroundMark.Trail(k == "glyph" ? "scorch" : k, tp - dir * 7f, tp, 2.6f, R * 0.4f, col, 14f, false); break;
                    }
                case "talon": GroundMark.Put("claw", tp, R, col, 16f, yaw); GroundMark.Put("wind", tp, R * 1.2f, col, 12f); break;
                case "stomp": GroundMark.Put("crater", tp, R * 1.2f, col, 20f); GroundMark.Put("crack", tp, R * 1.9f, col, 20f); break;
                case "gore": GroundMark.Put("crater", tp, R * 0.9f, col, 18f); GroundMark.Put("skid", tp - dir * 2.5f, R * 1.3f, col, 14f, yaw + 90f); break;
                case "bolt": GroundMark.Put("bolt", tp, R * 1.3f, col, 18f); GroundMark.Put("scorch", tp, R * 0.8f, col, 18f); break;
                case "sweep": GroundMark.Put("skid", tp, R * 1.6f, col, 14f, yaw + 90f); GroundMark.Put("wind", tp, R, col, 12f); break;
                case "gaze": GroundMark.Put("rune", tp, R * 1.3f, col, 18f, 0f); break;
                case "radiance": GroundMark.Put("glyph", tp, R * 1.5f, col, 20f, 0f); break;
                case "tide": GroundMark.Put("puddle", tp, R * 1.5f, col, 16f); GroundMark.Scatter("puddle", tp, R * 1.6f, 3, 0.8f, 1.6f, col, 14f); break;
                case "quake": GroundMark.Put("crater", tp, R * 1.1f, col, 22f); GroundMark.Put("crack", tp, R * 2.3f, col, 22f); break;
                case "howl": GroundMark.Put("frost", tp, R * 1.6f, col, 18f); GroundMark.Scatter("frost", tp, R * 1.5f, 3, 0.8f, 1.6f, col, 16f); break;
                case "pierce": GroundMark.Put("pock", tp, R, col, 16f); GroundMark.Put("skid", tp + dir * 6f, R * 2.4f, col, 12f, yaw + 90f); break;
                case "swarm": GroundMark.Scatter("pock", tp, R * 1.4f, 4, 1.0f, 1.8f, col, 16f); break;
                case "slash": GroundMark.Put("claw", tp, R * 1.2f, col, 16f, yaw + 40f); GroundMark.Put("claw", tp, R, col, 16f, yaw - 35f); GroundMark.Put("crack", tp, R * 1.3f, col, 14f); break;
                case "bind": GroundMark.Put("slither", tp, R * 1.1f, col, 18f, yaw); GroundMark.Put("rune", tp, R * 1.3f, col, 16f, 0f); break;
                case "gust": GroundMark.Put("wind", tp, R * 1.7f, col, 14f); GroundMark.Put("claw", tp, R * 0.9f, col, 12f, yaw); break;
                case "volley": GroundMark.Scatter("crater", tp, R * 1.4f, 4, 1.2f, 2.0f, col, 20f); GroundMark.Put("scorch", tp, R * 1.4f, col, 20f); break;
            }
        }

        // ------------------------------------------------------------------ pementasan
        static IEnumerator MythR(MySpec sp, PlayerCombat pc, Col c, float delay)
        {
            if (delay > 0f) yield return W(delay);
            if (pc == null) yield break;
            Vector3 up = Vector3.up;
            Vector3 P = pc.transform.position - up * 0.9f, F = Flat(pc.transform.forward), R = Vector3.Cross(up, F);
            var tg = Targets(P, F);
            Vector3 cen = Vector3.zero; foreach (var t0 in tg) cen += t0; cen /= tg.Count;
            Vector3 d0 = cen - P; d0.y = 0f; d0 = d0.sqrMagnitude < 1f ? F : d0.normalized;
            float sc = sp.scale, ph = 0f, roar = 0f;
            var v = new View(c);
            string mot = sp.mot, foot = MyFoot(sp);
            float footR = (foot == "crater" ? 1.5f : foot == "puddle" ? 1.3f : 0.85f) * sc;
            float alt = sp.bas == "b" ? 9f : mot == "fly" ? 7f : (mot == "cast" && (sp.bas == "d" || sp.bas == "r")) ? 3f : 0f;
            // ---------- pemanggilan
            Vector3 stage = mot == "cast" ? P + d0 * Mathf.Min(13f, (cen - P).magnitude * 0.55f) + R * 7f : P + R * 7f - F * 4f;
            Vector3 dir = Flat(cen - stage);
            Vector3 pos = stage + up * alt;
            CombatFx.ShockRing(stage, 6f, c.m, 0.4f); CombatFx.Dust(stage, 8);
            ThirdPersonCamera.Shake(0.5f, 0.35f);
            for (int i = 1; i <= 8; i++)
            {
                float u = i / 8f, kk = Mathf.SmoothStep(0.15f, 1f, u);
                MyPose(v, sp, pos - up * (1f - u) * 4f, dir, kk, ph, 0f); ph += 0.5f;
                
                yield return W(0.05f);
            }
            Audio.Sfx("skill_cast", pos);
            for (int i = 0; i < 12; i++)
            {
                roar = Mathf.Sin(i / 11f * Mathf.PI);
                MyPose(v, sp, pos, dir, 1f + roar * 0.05f, ph, roar); ph += 0.35f;
                Vector3 hw = MyHeadW(pos, dir, sc);
                if (i == 3) { ThirdPersonCamera.Shake(0.6f, 0.4f); }
                
                yield return W(0.05f);
            }
            roar = 0f;
            // ---------- serangan
            if (mot == "run" || mot == "stomp")
            {
                bool st = mot == "stomp";
                float stepLen = st ? 1.15f : 1.9f;
                foreach (var tp in tg)
                {
                    for (int it = 0; it < 120; it++)
                    {
                        Vector3 d = tp - pos; d.y = 0f;
                        if (d.magnitude < 3.8f) break;
                        dir = d.normalized; pos += dir * stepLen; pos.y = Mathf.Lerp(pos.y, tp.y, 0.25f);
                        MyPose(v, sp, pos, dir, 1f, ph, 0.1f); ph += st ? 0.55f : 1.05f;
                        if (it % 3 == 0)
                        {
                            Vector3 side = Vector3.Cross(up, dir) * (it % 6 == 0 ? 0.9f : -0.9f) * sc;
                            GroundMark.Put(foot, pos + side, footR, c.m, 9f, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg);
                            if (st) { CombatFx.ShockRing(pos, 4.5f * sc / 1.5f, c.m, 0.3f); ThirdPersonCamera.Shake(0.28f, 0.15f); MagicFx.Crack(pos, 3f * sc / 1.5f, 3f); }
                        }
                        if (it % 2 == 0) { CombatFx.Dust(pos, st ? 5 : 2); CombatFx.Burst(pos + up * 2f * sc, c.m, 2, 0.8f, 1f, 0.5f, 0.14f); }
                        yield return W(0.05f);
                    }
                    MyPose(v, sp, pos, dir, 1f, ph, 1f);
                    MythHit(sp, MyHeadW(pos, dir, sc), tp, dir, c); yield return W(0.14f);
                }
                for (int i = 0; i < 6; i++) { pos += dir * 2f; MyPose(v, sp, pos, dir, 1f, ph, 0f); ph += 1.0f; yield return W(0.05f); }
            }
            else if (mot == "leap")
            {
                foreach (var tp in tg)
                {
                    Vector3 from = pos, to = tp - Flat(tp - pos) * 2.8f; to.y = tp.y;
                    Vector3 hd = to - from; hd.y = 0f; if (hd.sqrMagnitude > 0.5f) dir = hd.normalized;
                    for (int i = 1; i <= 10; i++)
                    {
                        float u = i / 10f;
                        pos = Vector3.Lerp(from, to, u) + up * Mathf.Sin(u * Mathf.PI) * (6f + sc * 2f);
                        MyPose(v, sp, pos, dir, 1f, ph, 0f); ph += 0.3f;
                        
                        yield return W(0.04f);
                    }
                    GroundMark.Put(foot, pos + dir * 0.5f * sc, footR * 1.4f, c.m, 12f, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg);
                    MyPose(v, sp, pos, dir, 1f, ph, 0.8f);
                    MythHit(sp, MyHeadW(pos, dir, sc), tp, dir, c); yield return W(0.16f);
                }
            }
            else if (mot == "fly")
            {
                foreach (var tp in tg)
                {
                    Vector3 toT = tp - pos; toT.y = 0f; Vector3 dd = toT.sqrMagnitude < 1f ? dir : toT.normalized;
                    Vector3 apex = tp + up * (9f + sc * 2.5f) - dd * 10f;
                    for (int it = 0; it < 70 && (apex - pos).magnitude > 2.6f; it++)
                    {
                        Vector3 mv = apex - pos; pos += mv.normalized * Mathf.Min(2.2f, mv.magnitude); Vector3 hd = mv; hd.y = 0f; if (hd.sqrMagnitude > 0.1f) dir = hd.normalized;
                        MyPose(v, sp, pos, dir, 1f, ph, 0f); ph += 0.9f;
                        if (it % 2 == 0) CombatFx.Burst(pos - dir * 3f * sc, c.m, 3, 0.8f, 1f, 0.6f, 0.16f);
                        yield return W(0.05f);
                    }
                    Vector3 stp = pos, en = tp + up * ((sp.bas == "d" || sp.bas == "w" ? 4.2f : 2f) + sc);
                    for (int i = 1; i <= 6; i++)
                    {
                        float u = i / 6f; pos = Vector3.Lerp(stp, en, u * u); Vector3 hd = en - stp; hd.y = 0f; if (hd.sqrMagnitude > 0.1f) dir = hd.normalized;
                        MyPose(v, sp, pos, dir, 1f, ph, u > 0.6f ? 1f : 0f); ph += 0.6f;
                        CombatFx.Beam(pos + up * 1.5f * sc, pos + up * 1.5f * sc - dir * 7f, c.h, 0.3f, 0.2f);
                        yield return W(0.04f);
                    }
                    MythHit(sp, MyHeadW(pos, dir, sc), tp, dir, c);
                    for (int i = 0; i < 4; i++) { pos += dir * 2.2f + up * 2.2f; MyPose(v, sp, pos, dir, 1f, ph, 0f); ph += 0.9f; yield return W(0.04f); }
                }
                for (int i = 0; i < 6; i++) { pos += dir * 3f + up * 1.6f; MyPose(v, sp, pos, dir, 1f, ph, 0f); ph += 0.9f; yield return W(0.05f); }
            }
            else if (mot == "coil")
            {
                Vector3 anchor = cen - d0 * 5.5f; dir = d0;
                GroundMark.Put("slither", anchor, 3.2f * sc, c.m, 16f, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg);
                for (float t = 0f; t < 0.5f; t += 0.05f) { float u = t / 0.5f; MyPose(v, sp, anchor - up * (1f - u) * 7f, dir, Mathf.Lerp(0.3f, 1f, u), ph, 0f); ph += 0.7f; yield return W(0.05f); }
                MagicFx.Column(anchor, 2.5f, 15f, c.m, 0.8f); CombatFx.ShockRing(anchor, 7f, c.h, 0.5f); MagicFx.Crack(anchor, 6f, 5f); ThirdPersonCamera.Shake(0.6f, 0.35f);
                foreach (var tp in tg)
                {
                    Vector3 d = tp - anchor; d.y = 0f; dir = d.sqrMagnitude < 1f ? dir : d.normalized;
                    Vector3 goal = tp - dir * 4.2f;
                    for (int i = 1; i <= 5; i++) { pos = Vector3.Lerp(anchor, goal, i / 5f); MyPose(v, sp, pos, dir, 1f, ph, i == 5 ? 1f : 0f); ph += 0.7f; GroundMark.Put("slither", pos, 1.8f * sc, c.m, 10f, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg); yield return W(0.04f); }
                    MythHit(sp, MyHeadW(pos, dir, sc), tp, dir, c);
                    for (int i = 4; i >= 0; i--) { pos = Vector3.Lerp(anchor, goal, i / 5f); MyPose(v, sp, pos, dir, 1f, ph, 0f); ph += 0.7f; yield return W(0.04f); }
                }
                pos = anchor;
            }
            else if (mot == "drop")
            {
                Vector3 tp0 = tg[0]; dir = Flat(tp0 - P);
                for (int i = 1; i <= 8; i++) { float u = i / 8f; pos = tp0 + up * Mathf.Lerp(32f, 0f, u * u); MyPose(v, sp, pos, dir, 1f, ph, 0f); ph += 0.2f; yield return W(0.04f); }
                MythHit(sp, MyHeadW(pos, dir, sc), tp0, dir, c); ThirdPersonCamera.Shake(1.0f, 0.55f);
                for (int i = 0; i < 3; i++) { CombatFx.ShockRing(tp0, 8f + i * 5f, i % 2 == 0 ? c.m : c.h, 0.5f); yield return W(0.09f); }
                for (int i = 1; i < tg.Count; i++) { MythHit(sp, MyHeadW(pos, dir, sc), tg[i], (tg[i] - tp0).sqrMagnitude < 1f ? dir : (tg[i] - tp0).normalized, c); yield return W(0.1f); }
                pos = tp0; MyPose(v, sp, pos, dir, 1f, ph, 0.6f); yield return W(0.45f);
            }
            else   // cast: berdiri / melayang merapal
            {
                for (int i = 0; i < 6; i++) { MyPose(v, sp, pos, dir, 1f + i * 0.01f, ph, 0.3f); ph += 0.4f; yield return W(0.05f); }
                foreach (var tp in tg)
                {
                    Vector3 d = tp - pos; d.y = 0f; if (d.sqrMagnitude > 1f) dir = d.normalized;
                    MyPose(v, sp, pos, dir, 1.04f, ph, 1f); ph += 0.4f;
                    MythHit(sp, MyHeadW(pos, dir, sc * 1.04f), tp, dir, c);
                    for (int i = 0; i < 4; i++) { MyPose(v, sp, pos, dir, 1f, ph, 0.2f); ph += 0.4f; yield return W(0.06f); }
                }
                for (int i = 0; i < 6; i++) { MyPose(v, sp, pos, dir, 1f, ph, 0f); ph += 0.4f; yield return W(0.06f); }
            }
            // ---------- lenyap menjadi percikan cahaya
            CombatFx.Flash(MyHeadW(pos, dir, sc), c.h, 7f, 1.5f, 0.2f);
            for (int i = 0; i < 9; i++)
            {
                float u = i / 8f;
                MyPose(v, sp, pos + up * u * 4f, dir, 1f - u * 0.9f, ph, 0f); ph += 0.5f;
                for (int b = 0; b < 2; b++) CombatFx.Burst(pos + dir * UnityEngine.Random.Range(-5f, 5f) * sc + up * UnityEngine.Random.Range(0.5f, 7f) * sc * (1f - u * 0.5f), c.m, 6, 1.2f, 3f, 0.9f, 0.22f);
                yield return W(0.05f);
            }
            v.Kill();
        }
    }
}
