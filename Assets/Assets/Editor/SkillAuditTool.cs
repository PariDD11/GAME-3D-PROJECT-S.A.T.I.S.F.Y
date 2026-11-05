using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HunterGuild;

/// <summary>Hunter Guild > Audit Skill: cari nama ganda, skill beranggka/efek identik, pola visual ganda, dan ultimate tanpa siluet hewan. Hasil di Console.</summary>
public static class SkillAuditTool
{
    [MenuItem("Hunter Guild/Audit Skill (duplikat)")]
    static void Run()
    {
        int bad = 0;
        var names = new Dictionary<string, string>();
        var sigs = new Dictionary<string, string>();
        var vis = new Dictionary<string, string>();
        foreach (var s in SkillDB.All)
        {
            string n = (s.name ?? "").ToLowerInvariant();
            string other;
            if (names.TryGetValue(n, out other)) { Debug.LogWarning("[Audit] Nama ganda: '" + s.name + "'  (" + other + " dan " + s.id + ")"); bad++; } else names[n] = s.id;
            if (!s.active || string.IsNullOrEmpty(s.kind) || s.tier < 2) continue;
            string key = s.job + "|" + s.kind + "|" + s.elem + "|" + s.mult.ToString("0.00") + "|" + s.radius + "|" + s.range + "|" + s.fx + "|" + s.cooldown.ToString("0.0");
            if (sigs.TryGetValue(key, out other)) { Debug.LogWarning("[Audit] Angka/efek identik: " + other + " dan " + s.id); bad++; } else sigs[key] = s.id;
            if (!string.IsNullOrEmpty(s.sig))
            {
                string vk = s.job + "|" + s.kind + "|" + s.sig;
                if (vis.TryGetValue(vk, out other)) { Debug.LogWarning("[Audit] Pola visual sama (" + s.sig + ") di job+jenis yang sama: " + other + " dan " + s.id); bad++; } else vis[vk] = s.id;
            }
            if (s.tier >= 3 && string.IsNullOrEmpty(s.beast)) { Debug.LogWarning("[Audit] Ultimate tanpa siluet hewan: " + s.id + " " + s.name); bad++; }
        }
        Debug.Log("[Audit Skill] " + SkillDB.All.Count + " skill diperiksa. Temuan: " + bad);
    }
}
