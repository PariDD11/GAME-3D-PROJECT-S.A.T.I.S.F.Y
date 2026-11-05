using UnityEngine;

namespace HunterGuild
{
    /// <summary>Tombol debug khusus Guild. F11 = naik 1 rank (+level), F12 = naik 1 tier reputasi dan +500 gold.</summary>
    public class GuildDebugKeys : MonoBehaviour
    {
        void Update()
        {
            var gs = GameState.I;
            if (gs == null) return;

            if (TownInput.DebugDown(11))
            {
                int i = GameState.RankIndex(gs.rank);
                if (i < 8)
                {
                    gs.rank = GameState.Ranks[i + 1];
                    gs.level = Mathf.Max(gs.level, GuildConfig.ExamMinLevel[i + 1]);
                    gs.questsDone = Mathf.Max(gs.questsDone, GuildConfig.ExamMinQuests[i + 1]);
                    GuildData.I.postersAt = -999f;   // paksa rotasi poster
                    GuildServices.Say("[DEBUG] Rank sekarang " + gs.rank + ".");
                }
            }

            if (TownInput.DebugDown(12))
            {
                var gd = GuildData.I;
                int next = Mathf.Min(4, gd.TierIndex + 1);
                gd.reputation = Mathf.Max(gd.reputation, GuildConfig.TierMinPoints[next]);
                gs.gold += 500;
                gd.postersAt = -999f;
                GuildServices.Say("[DEBUG] Reputasi: " + gd.TierName + ", +500 gold.");
            }
        }
    }
}
