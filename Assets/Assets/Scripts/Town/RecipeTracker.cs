using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>v35z102n31: resep yang ditandai (tab RESEP di Jurnal) tampil di pelacak HUD dengan jumlah bahan yang dimiliki. Disimpan sebagai flag "rtrack:&lt;itemId&gt;".</summary>
    public static class RecipeTracker
    {
        const string P = "rtrack:";
        public static string Pinned
        {
            get { var gs = GameState.I; if (gs == null) return ""; foreach (var f in gs.flags) if (f.StartsWith(P)) return f.Substring(P.Length); return ""; }
        }
        public static void Set(string id)
        {
            var gs = GameState.I; if (gs == null) return;
            gs.flags.RemoveWhere(f => f.StartsWith(P));
            if (!string.IsNullOrEmpty(id)) gs.flags.Add(P + id);
        }

        /// <summary>Baris pelacak: [judul, tujuan]. Kosong bila tidak ada resep yang ditandai.</summary>
        public static List<string[]> Rows()
        {
            var res = new List<string[]>(); var gs = GameState.I; string id = Pinned; if (gs == null || id == "") return res;
            foreach (var r in Blacksmith.AllRecipes())
            {
                if (r.result != id) continue;
                var d = ItemDB.Get(id); var sb = new System.Text.StringBuilder(); bool all = true;
                foreach (var m in r.mats)
                {
                    int have = gs.Count(m.Key); if (have < m.Value) all = false;
                    var md = ItemDB.Get(m.Key);
                    sb.Append(md != null ? md.name : m.Key).Append(' ').Append(Mathf.Min(have, m.Value)).Append('/').Append(m.Value).Append("  ");
                }
                res.Add(new[] { "Resep: " + (d != null ? d.name : id), all ? "Bahan lengkap - temui Hadrik" : sb.ToString().Trim() });
            }
            return res;
        }
    }
}
