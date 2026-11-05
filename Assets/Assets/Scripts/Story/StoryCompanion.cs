using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Dagan Roe: partner cerita. Bergabung otomatis saat quest nd_k10 diterima, tidak memotong reward (0%), tidak bisa digaji lewat poster,
    /// ikut bertarung lewat sistem partner yang ada (AllyManager), dan hilang permanen setelah gugur di Takhta Zargoth (flag dagan_gone).
    /// </summary>
    public static class StoryCompanion
    {
        public const string Key = "u:dagan";
        static float next;

        public static bool Joined { get { var g = GameState.I; return g != null && g.flags.Contains("dagan_join"); } }
        public static bool Gone { get { var g = GameState.I; return g != null && g.flags.Contains("dagan_gone"); } }

        /// <summary>Dipanggil WorldQuests.Accept.</summary>
        public static void OnAccept(string questId)
        {
            var g = GameState.I; if (g == null) return;
            if (questId == "nd_k10" && !Joined && !Gone)
            {
                g.flags.Add("dagan_join");
                GameState.Say("Dagan Roe bergabung sebagai partner cerita (tanpa potongan reward).");
            }
            if (questId == "nd_k17" && Joined && !Gone)
                GameState.Say("Peringatan: ini perjalanan terakhir Dagan di party. Selesaikan urusan lain lebih dulu.");
        }

        /// <summary>Dipanggil StoryWorld.Update (tiap 0.5 dtk): menjaga Dagan tetap di party, atau melepasnya bila sudah gugur.</summary>
        public static void Tick()
        {
            if (Time.unscaledTime < next) return; next = Time.unscaledTime + 1f;
            var gd = GuildData.I; var gs = GameState.I;
            if (gd == null || gs == null) return;
            if (Gone)
            {
                gd.party.RemoveAll(x => x.key == Key);
                if (gd.favorite != null && gd.favorite.key == Key) gd.favorite = null;
                return;
            }
            if (!Joined || Cutscene.Active) return;
            if (gd.party.Exists(x => x.key == Key)) return;
            int ri = Mathf.Max(0, GameState.RankIndex(gs.rank));
            var def = new UniqueDef
            {
                key = Key, name = "Dagan Roe", age = 23, role = "DPS Ranged",
                lines = new[]
                {
                    "Dagan. Penyihir api. Aku menyalakan api hanya sedikit, jangan khawatir.",
                    "Kalau tanganku habis, jangan beri aku belas kasihan. Dan jangan biarkan aku mencoba membuktikan sesuatu.",
                    "Kau lupa jalan pulang? Bagus. Aku juga tidak."
                }
            };
            var p = PartnerFactory.MakeUnique(def, ri);
            p.hiredCost = 0;
            gd.party.Add(p);
            gd.postersVersion++;
        }
    }
}
