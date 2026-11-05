using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// 5 poster rekrutmen 3D di dinding belakang resepsionis. Teksnya disinkronkan dengan GuildData.posters,
    /// jadi isi poster di dinding sama dengan kartu di jendela rekrutmen.
    /// </summary>
    public class PosterWall : MonoBehaviour
    {
        public TextMesh[] names, roles, stats, costs;
        public Renderer[] portraits;

        int shownVersion = -1;

        void Update()
        {
            var gd = GuildData.I;
            if (GameState.I == null) return;
            gd.EnsurePosters();
            if (gd.postersVersion != shownVersion)
            {
                shownVersion = gd.postersVersion;
                for (int i = 0; i < names.Length; i++) Apply(i, gd.posters[i], gd);
            }
        }

        void Apply(int i, Partner p, GuildData gd)
        {
            if (p == null)
            {
                names[i].text = "???";
                roles[i].text = "Slot Reputasi";
                stats[i].text = "Butuh " + GuildConfig.TierNames[3];
                costs[i].text = "";
                portraits[i].material.color = new Color(0.12f, 0.12f, 0.14f);
                return;
            }
            names[i].text = p.name;
            roles[i].text = "Rank " + p.rank + " - " + p.role;
            stats[i].text = "HP " + p.hp + " ATK " + p.atk + " DEF " + p.def + " SPD " + p.spd;
            costs[i].text = "Biaya " + gd.CostFor(p) + "% reward" + (gd.InParty(p) ? "  [DIREKRUT]" : "");
            portraits[i].material.color = p.PortraitColor;
        }
    }
}
