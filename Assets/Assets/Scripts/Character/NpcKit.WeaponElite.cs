using System.Collections.Generic;
using UnityEngine;

/// <summary>Model senjata unik: set area Eldrath (gw_), senjata boss (bw_), dan senjata kelas evolusi (ev1_/ev2_).
/// Bentuk mengikuti job: pedang, busur, tongkat api (mahkota api), palu batu (tanah), trisula (air), cincin angin, tongkat suci (halo + salib).</summary>
public static partial class NpcKit
{
    class EliteInfo { public string job; public int theme; public bool grand; public int variant; }

    static readonly Dictionary<string, string> bossTheme = new Dictionary<string, string>
    { { "ogre_chief", "0" }, { "behemoth", "0" }, { "cerberus", "2" }, { "ignarok", "1" }, { "bone_king", "2" }, { "demon_lord", "3" } };

    static bool EliteParse(string id, out EliteInfo e)
    {
        e = null;
        if (string.IsNullOrEmpty(id)) return false;
        if (id.StartsWith("gw_"))
        {
            var p = id.Split('_');           // gw_a_Job
            if (p.Length < 3) return false;
            int a; if (!int.TryParse(p[1], out a)) return false;
            e = new EliteInfo { job = p[2], theme = a, grand = false, variant = a };
            return true;
        }
        if (id.StartsWith("bw_"))
        {
            int us = id.LastIndexOf('_');
            string job = id.Substring(us + 1), boss = id.Substring(3, us - 3), t;
            if (!bossTheme.TryGetValue(boss, out t)) t = "0";
            e = new EliteInfo { job = job, theme = int.Parse(t), grand = true, variant = int.Parse(t) + 4 };
            return true;
        }
        if (id.StartsWith("ev1_") || id.StartsWith("ev2_"))
        {
            var evo = HunterGuild.JobInfo.Evo(id.Substring(4));
            if (evo == null) return false;
            int h = 0; foreach (char c in evo.id) h = h * 31 + c;
            e = new EliteInfo { job = evo.job, theme = 4, grand = id[2] == '2', variant = Mathf.Abs(h) % 5 + 8 };
            return true;
        }
        return false;
    }

    static string EliteKind(string job) { return job == "Swordsman" ? "sword" : job == "Archer" ? "bow" : "staff"; }

    static void ThemeSlots(EliteInfo e, string job, out int blade, out int guard, out int gem, out int wrap)
    {
        switch (e.theme)
        {
            case 0: blade = Sl.Metal; guard = Sl.Wood; gem = Sl.CoatB; wrap = Sl.Leather; break;      // Perbatasan: besi & kayu hijau
            case 1: blade = Sl.Dark; guard = Sl.Hair; gem = Sl.Glow; wrap = Sl.CoatA; break;          // Bara: gelap & lava
            case 2: blade = Sl.Eye; guard = Sl.Eye; gem = Sl.CoatC; wrap = Sl.Dark; break;            // Tulang kuno: tulang & biru pucat
            case 3: blade = Sl.Dark; guard = Sl.Metal; gem = Sl.CoatA; wrap = Sl.Dark; break;         // Iblis: hitam & merah
            default:                                                                                   // kelas evolusi: warna elemen job
                blade = Sl.Metal; guard = Sl.Gold; wrap = Sl.CoatA;
                switch (job)
                {
                    case "FireMage": gem = Sl.CoatA; break;
                    case "EarthMage": gem = Sl.CoatB; break;
                    case "WaterMage": gem = Sl.Water; break;
                    case "WindMage": gem = Sl.Glass; break;
                    case "Priest": gem = Sl.Glow; break;
                    default: gem = Sl.CoatC; break;
                }
                break;
        }
    }

    static WeaponModel EliteWeapon(string id, EliteInfo e)
    {
        var b = new GeoBag();
        var w = new WeaponModel { id = id, kind = EliteKind(e.job) };
        int blade, guard, gem, wrap;
        ThemeSlots(e, e.job, out blade, out guard, out gem, out wrap);
        float g = e.grand ? 1.18f : 1f;
        int spikes = 2 + (e.variant % 4);
        switch (e.job)
        {
            case "Swordsman":
                {
                    float len = (0.86f + 0.05f * Mathf.Min(e.theme, 3)) * g;
                    Sword(b, len, 0.09f * g, blade, guard, wrap, true);
                    if (e.theme == 1 || e.theme == 3)         // gerigi di punggung bilah
                        for (int i = 0; i < spikes + 1; i++) Pyramid(b, blade, new Vector3(0.045f, 0.2f + i * len / (spikes + 2), -0.006f), new Vector3(0.045f, 0.2f + i * len / (spikes + 2), 0.006f), new Vector3(0.045f + 0.045f, 0.2f + i * len / (spikes + 2) - 0.02f, 0f), new Vector3(0.045f, 0.24f + i * len / (spikes + 2), 0f));
                    b.Ball(gem, new Vector3(0f, 0.075f, 0.02f), 0.026f, 3, 7);
                    w.length = len + 0.25f;
                    break;
                }
            case "Archer":
                {
                    float half = (0.68f + 0.04f * Mathf.Min(e.theme, 3)) * g;
                    Bow(b, blade == Sl.Dark ? Sl.Dark : guard == Sl.Gold ? Sl.WoodLight : blade, wrap, half);
                    b.Ball(gem, new Vector3(0f, 0.0f, 0.03f), 0.03f, 3, 7);
                    for (int s = -1; s <= 1; s += 2) b.Beam(gem, new Vector3(0f, s * half, -0.2f), new Vector3(0f, s * (half + 0.09f * g), -0.2f), 0.022f, 0.004f);   // ujung limb runcing
                    if (e.grand) { b.Beam(Sl.Gold, new Vector3(0f, half * 0.55f, -0.07f), new Vector3(0f, half * 0.55f, 0.02f), 0.02f, 0.02f); b.Beam(Sl.Gold, new Vector3(0f, -half * 0.55f, -0.07f), new Vector3(0f, -half * 0.55f, 0.02f), 0.02f, 0.02f); }
                    w.length = half * 2f;
                    break;
                }
            default:
                {
                    float len = (1.25f + 0.08f * Mathf.Min(e.theme, 3)) * g;
                    int shaft = e.theme == 2 ? Sl.Eye : e.theme == 1 || e.theme == 3 ? Sl.Dark : Sl.WoodLight;
                    b.Cyl(shaft, new Vector3(0f, -0.25f, 0f), 0.021f, len + 0.25f, 8, 0.021f, true, true);
                    for (int i = 0; i < 3; i++) b.Cyl(wrap, new Vector3(0f, -0.06f + i * 0.045f, 0f), 0.027f, 0.025f, 8, 0.027f, false, false);
                    b.Ball(Sl.Gold, new Vector3(0f, -0.26f, 0f), 0.022f, 3, 6);
                    float top = len - 0.05f;
                    int deco = e.grand ? Sl.Gold : guard;
                    switch (e.job)
                    {
                        case "FireMage":        // mahkota api: cincin duri bara mengelilingi permata
                            b.Ball(gem, new Vector3(0f, top + 0.12f, 0f), 0.08f * g, 4, 8);
                            for (int i = 0; i < 5 + (e.grand ? 2 : 0); i++)
                            {
                                float a = i * 2f * Mathf.PI / (5 + (e.grand ? 2 : 0));
                                b.Cone(gem, new Vector3(Mathf.Sin(a) * 0.09f, top + 0.05f, Mathf.Cos(a) * 0.09f), 0.02f, 0.15f * g, 5);
                            }
                            b.Cyl(deco, new Vector3(0f, top - 0.01f, 0f), 0.055f, 0.03f, 10, 0.04f, true, true);
                            break;
                        case "EarthMage":       // palu/gada batu: kepala balok bergerigi
                            b.Box(Sl.Stone, new Vector3(0f, top + 0.08f, 0f), new Vector3(0.2f * g, 0.14f * g, 0.14f * g));
                            b.Box(deco, new Vector3(0f, top + 0.08f, 0f), new Vector3(0.05f, 0.16f * g, 0.16f * g));
                            b.Box(gem, new Vector3(0f, top + 0.08f, 0.075f * g), new Vector3(0.07f, 0.07f, 0.02f));
                            for (int i = 0; i < spikes; i++) b.Cone(Sl.Stone, new Vector3(-0.07f * g + i * 0.14f * g / Mathf.Max(1, spikes - 1), top + 0.15f * g, 0f), 0.03f, 0.1f, 5);
                            b.Cone(gem, new Vector3(0f, top + 0.15f * g, 0f), 0.04f, 0.16f * g, 6);
                            break;
                        case "WaterMage":       // trisula: bulan sabit + orb
                            for (int i = -3; i <= 3; i++)
                            {
                                float t = i / 3f;
                                b.Ball(deco, new Vector3(t * 0.12f * g, top + 0.03f + (1f - t * t) * -0.05f + t * t * 0.1f, 0f), 0.022f, 3, 6);
                            }
                            b.Ball(gem, new Vector3(0f, top + 0.1f, 0f), 0.065f * g, 4, 8);
                            b.Cone(deco, new Vector3(0f, top + 0.16f * g, 0f), 0.02f, 0.14f * g, 5);
                            for (int s = -1; s <= 1; s += 2) b.Cone(deco, new Vector3(s * 0.12f * g, top + 0.13f, 0f), 0.016f, 0.12f * g, 5);
                            break;
                        case "WindMage":        // cincin angin: lingkaran + bulu
                            for (int i = 0; i < 12; i++)
                            {
                                float a = i * Mathf.PI * 2f / 12f;
                                b.Ball(deco, new Vector3(Mathf.Sin(a) * 0.12f * g, top + 0.14f + Mathf.Cos(a) * 0.12f * g, 0f), 0.014f, 3, 5);
                            }
                            b.Ball(gem, new Vector3(0f, top + 0.14f, 0f), 0.05f * g, 4, 8);
                            for (int i = 0; i < 3 + (e.grand ? 1 : 0); i++)
                            {
                                float a = i * Mathf.PI * 2f / (3 + (e.grand ? 1 : 0));
                                b.Beam(gem, new Vector3(0f, top + 0.14f, 0f), new Vector3(Mathf.Sin(a) * 0.19f * g, top + 0.14f + Mathf.Cos(a) * 0.19f * g, 0f), 0.016f, 0.004f);
                            }
                            break;
                        default:                // Priest: halo + salib
                            for (int i = 0; i < 14; i++)
                            {
                                float a = i * Mathf.PI * 2f / 14f;
                                b.Ball(Sl.Gold, new Vector3(Mathf.Sin(a) * 0.1f * g, top + 0.16f + Mathf.Cos(a) * 0.1f * g, 0f), 0.012f, 3, 5);
                            }
                            b.Box(gem, new Vector3(0f, top + 0.16f, 0f), new Vector3(0.025f, 0.15f * g, 0.025f));
                            b.Box(gem, new Vector3(0f, top + 0.19f, 0f), new Vector3(0.1f * g, 0.025f, 0.025f));
                            b.Cyl(deco, new Vector3(0f, top - 0.02f, 0f), 0.05f, 0.04f, 10, 0.03f, true, true);
                            break;
                    }
                    w.length = len + 0.1f;
                    break;
                }
        }
        int[] used;
        w.mesh = b.ToMesh("W_" + id, null, out used);
        var all = WeaponSlots(id);
        w.mats = new Material[used.Length];
        for (int i = 0; i < used.Length; i++) w.mats[i] = all[used[i]];
        weaponCache[id] = w;
        return w;
    }
}
