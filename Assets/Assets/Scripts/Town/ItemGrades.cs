using UnityEngine;

namespace HunterGuild
{
    public enum ItemGrade { Common, Uncommon, Rare, Epic, Legendary }

    /// <summary>
    /// Grade (kelangkaan) item. Dihitung otomatis dari data item, jadi item baru langsung ikut:
    ///  - Senjata boss (bw_*) = Legendary; senjata evolusi Lv30 (ev2_*) = Epic; evolusi Lv15 (ev1_*) = Rare.
    ///  - Gear area baru memakai ItemDef.tier: Perbatasan (2) Rare, Vulkanik (3) Epic, Padang Tulang (4) Epic, Lembah Iblis (5) Legendary.
    ///  - Gear dasar (tanpa tier): total ATK+DEF <= 10 Common, <= 30 Uncommon, selebihnya Rare.
    ///  - Attachment Sihir: Tier 1 Uncommon, Tier 2 Rare, Tier 3 Epic; attachment lain mengikuti harga jual.
    ///  - Lainnya (bahan, makanan, konsumsi): dari harga jual (>=10 Uncommon, >=20 Rare, >=40 Epic, >=100 Legendary).
    /// Override per item lewat ItemGrades.Set("id", grade).
    /// </summary>
    public static class ItemGrades
    {
        static readonly System.Collections.Generic.Dictionary<string, ItemGrade> over = new System.Collections.Generic.Dictionary<string, ItemGrade>();
        public static void Set(string id, ItemGrade g) { over[id] = g; }

        public static ItemGrade Of(ItemDef d)
        {
            if (d == null) return ItemGrade.Common;
            ItemGrade o;
            if (over.TryGetValue(d.id, out o)) return o;
            if (d.type == ItemType.Gear)
            {
                if (d.id.StartsWith("bw_")) return ItemGrade.Legendary;
                if (d.id.StartsWith("ev2_")) return ItemGrade.Epic;
                if (d.id.StartsWith("ev1_")) return ItemGrade.Rare;
                if (d.tier >= 5) return ItemGrade.Legendary;
                if (d.tier >= 3) return ItemGrade.Epic;
                if (d.tier == 2) return ItemGrade.Rare;
                int p = d.atk + d.def;
                return p <= 10 ? ItemGrade.Common : p <= 30 ? ItemGrade.Uncommon : ItemGrade.Rare;
            }
            if (d.type == ItemType.Attachment && d.tier > 0) return d.tier >= 3 ? ItemGrade.Epic : d.tier == 2 ? ItemGrade.Rare : ItemGrade.Uncommon;
            int s = d.sellPrice;
            return s >= 100 ? ItemGrade.Legendary : s >= 40 ? ItemGrade.Epic : s >= 20 ? ItemGrade.Rare : s >= 10 ? ItemGrade.Uncommon : ItemGrade.Common;
        }

        public static Color Tint(ItemGrade g)
        {
            switch (g)
            {
                case ItemGrade.Uncommon: return new Color(0.45f, 0.90f, 0.50f);
                case ItemGrade.Rare: return new Color(0.42f, 0.72f, 1.00f);
                case ItemGrade.Epic: return new Color(0.76f, 0.52f, 0.98f);
                case ItemGrade.Legendary: return new Color(1.00f, 0.72f, 0.25f);
                default: return new Color(0.84f, 0.84f, 0.88f);
            }
        }

        public static Color Tint(ItemDef d) { return Tint(Of(d)); }
        public static string Name(ItemDef d) { return Of(d).ToString(); }
        public static string Hex(ItemDef d) { return ColorUtility.ToHtmlStringRGB(Tint(Of(d))); }
    }
}
