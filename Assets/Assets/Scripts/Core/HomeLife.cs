using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>v35z102n32: peternakan di rumah. Kandang Ayam / Domba / Sapi (dibeli di Toko Furnitur) menghasilkan telur / wol / susu tiap hari game.
    /// Hasil menumpuk sampai 5 hari. Pakan: 1 Gandum per hari per kandang dari tas (kurang pakan = hasil berkurang sampai separuh).
    /// Data di GameState.stats ("hl_{rumah}_{jenis}" = hari terakhir diambil), jadi ikut save.</summary>
    public static class HomeLife
    {
        class Pen { public string furn, name, product, label; public int perDay; public int everyDays; }
        static readonly Pen[] Pens =
        {
            new Pen { furn = "coop", name = "Kandang Ayam", product = "egg", label = "Telur", perDay = 3, everyDays = 1 },
            new Pen { furn = "sheep", name = "Kandang Domba", product = "wool", label = "Wol", perDay = 2, everyDays = 2 },
            new Pen { furn = "cow", name = "Kandang Sapi", product = "milk", label = "Susu", perDay = 2, everyDays = 1 },
        };
        const int MaxDays = 5;

        static string Key(PropertyMenus.House h, Pen p) { return "hl_" + h.id + "_" + p.furn; }

        static int Pending(PropertyMenus.House h, Pen p, out int days)
        {
            var gs = GameState.I; string k = Key(h, p);
            int today = Mathf.Max(1, gs.day);
            if (!gs.stats.ContainsKey(k)) gs.stats[k] = today;           // mulai menghitung sejak kandang pertama kali dilihat
            days = Mathf.Clamp(today - gs.Stat(k), 0, MaxDays);
            return (days / Mathf.Max(1, p.everyDays)) * p.perDay;
        }

        public static void AddEntries(List<UIEntry> l, PropertyMenus.House h, System.Action refresh)
        {
            foreach (var pp in Pens)
            {
                var p = pp;
                if (!PropertyMenus.HasFurn(h, p.furn)) continue;
                int days; int n = Pending(h, p, out days);
                l.Add(new UIEntry(p.name + ": " + (n > 0 ? n + " " + p.label + " siap" : "belum ada hasil") + " (pakan: Gandum x" + days + ")", () => Collect(h, p, refresh), n > 0));
            }
        }

        static void Collect(PropertyMenus.House h, Pen p, System.Action refresh)
        {
            var gs = GameState.I; int days; int n = Pending(h, p, out days);
            if (n <= 0) return;
            int feed = Mathf.Min(days, gs.Count("wheat"));
            float fed = days <= 0 ? 1f : (float)feed / days;
            int get = Mathf.Max(1, Mathf.RoundToInt(n * (0.5f + 0.5f * fed)));
            if (!gs.HasSpace(p.product, get)) { if (TownUI.I != null) TownUI.I.Toast("Tas penuh."); return; }
            if (feed > 0) gs.RemoveItem("wheat", feed);
            gs.AddItem(p.product, get);
            gs.stats[Key(h, p)] = Mathf.Max(1, gs.day);
            gs.AddStat("harvested", get);
            Audio.Sfx("gather");
            if (TownUI.I != null) TownUI.I.Toast(p.label + " x" + get + (fed < 1f ? "  (pakan kurang: hasil berkurang)" : "  (diberi " + feed + " Gandum)"));
            if (refresh != null) refresh();
        }
    }
}
