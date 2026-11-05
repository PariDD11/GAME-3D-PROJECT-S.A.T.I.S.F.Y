using UnityEngine;
using UnityEngine.UI;

namespace HunterGuild
{
    /// <summary>
    /// Poster Rekrutmen (GDD 5.2 dan 14): 5 kartu horizontal dengan indikator cooldown,
    /// Slot Favorit, dan ringkasan party. Slot ke-5 adalah slot reputasi eksklusif.
    /// </summary>
    public class PartyRecruitUI : MonoBehaviour
    {
        public static PartyRecruitUI I { get; private set; }

        public static PartyRecruitUI Get()
        {
            if (I == null) new GameObject("PartyRecruitUI").AddComponent<PartyRecruitUI>();
            return I;
        }

        public bool IsOpen { get { return root != null && root.activeSelf; } }

        static readonly Color Panel = new Color(0.13f, 0.14f, 0.18f, 1f);
        static readonly Color Gold = new Color(1f, 0.85f, 0.4f);
        static readonly Color Good = new Color(0.5f, 1f, 0.6f);
        static readonly Color Bad = new Color(1f, 0.55f, 0.45f);

        GameObject root;
        int themeVer = -1;
        RectTransform cardRoot, bottomRoot;
        Text subtitle;

        void Awake() { I = this; }
        void OnDestroy() { if (I == this) I = null; }

        void Update()
        {
            if (IsOpen && (TownUI.I == null || !TownUI.I.PanelOpen)) Hide();
        }

        public void Show()
        {
            if (TownUI.I == null) return;
            GuildData.I.EnsurePosters();
            if (UiStyle.Stale(ref themeVer) && root != null) { Destroy(root); root = null; }
            if (root == null) Build();
            root.SetActive(true);
            TownUI.I.SetExternalOpen(true);
            Rebuild();
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
            if (TownUI.I != null) TownUI.I.SetExternalOpen(false);
        }

        static Color Dim(Color c, float f) { return new Color(c.r * f, c.g * f, c.b * f, 1f); }

        void Build()
        {
            var bg = UiStyle.Frame("PartyWindow", TownUI.I.Root); bg.raycastTarget = true;
            var c = new Vector2(0.5f, 0.5f);
            UIKit.Place(bg.rectTransform, c, c, c, Vector2.zero, new Vector2(1580, 880));
            root = bg.gameObject;

            var title = UIKit.Txt("Title", root.transform, 36, TextAnchor.MiddleLeft, Gold);
            UIKit.At(title.rectTransform, 24, 10, 1000, 48);
            title.text = "Poster Rekrutmen - Resepsionis Guild";

            subtitle = UIKit.Txt("Subtitle", root.transform, 21, TextAnchor.UpperLeft, new Color(0.85f, 0.85f, 0.85f));
            UIKit.At(subtitle.rectTransform, 24, 58, 1500, 50);

            var close = UIKit.Btn(root.transform, "Tutup (Esc)", Hide, true, new Color(0.35f, 0.2f, 0.2f), 20);
            UIKit.At((RectTransform)close.transform, 1426, 14, 130, 44);

            cardRoot = UIKit.Rect("Cards", root.transform);
            UIKit.At(cardRoot, 20, 112, 1550, 540);
            bottomRoot = UIKit.Rect("Bottom", root.transform);
            UIKit.At(bottomRoot, 20, 676, 1550, 190);
            root.SetActive(false);
        }

        void Rebuild()
        {
            var gs = GameState.I;
            var gd = GuildData.I;
            UIKit.Clear(cardRoot);
            UIKit.Clear(bottomRoot);

            string gate = "";
            if (!gs.jobChosen && gs.level < 5)
                gate = "\nRekrutmen dibuka setelah Level 5 / memilih job. Poster hanya bisa dilihat.";
            else gate = "\nPartner yang direkrut langsung muncul di sisimu, mengikutimu, dan ikut bertarung.";
            subtitle.text = "Poster berganti dalam " + GuildServices.FormatHours(Mathf.Max(0.1f, gd.HoursToPosterRefresh)) +
                            " atau setelah kamu klaim quest. Biaya = persen reward yang diambil partner." + gate;

            for (int i = 0; i < GuildConfig.PosterCount; i++) BuildCard(i, gd.posters[i]);
            BuildBottom();
        }


        void BuildCard(int i, Partner p)
        {
            var gd = GuildData.I;
            var card = UIKit.Img("Card" + i, cardRoot, Panel);
            UIKit.At(card.rectTransform, i * 310f, 0f, 300f, 530f);
            Transform c = card.transform;

            if (p == null)
            {
                var sil = UIKit.Img("Portrait", c, new Color(0.08f, 0.08f, 0.1f, 1f));
                UIKit.At(sil.rectTransform, 0, 0, 300, 130);
                var q = UIKit.Txt("Q", sil.transform, 80, TextAnchor.MiddleCenter, new Color(0.3f, 0.3f, 0.35f));
                UIKit.Place(q.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                q.text = "?";
                var t1 = UIKit.Txt("Name", c, 26, TextAnchor.UpperLeft, Gold);
                UIKit.At(t1.rectTransform, 12, 140, 276, 70);
                t1.text = "Slot Reputasi Eksklusif";
                var t2 = UIKit.Txt("Info", c, 21, TextAnchor.UpperLeft, new Color(0.7f, 0.7f, 0.7f));
                UIKit.At(t2.rectTransform, 12, 220, 276, 200);
                t2.text = "Partner terbaik diprioritaskan untuk hunter berreputasi tinggi.\n\nTerbuka di reputasi: " + GuildConfig.TierNames[3] +
                          "\nReputasimu: " + gd.TierName;
                return;
            }

            int fam = gd.FamiliarityLevel(p.key);
            var portrait = UIKit.Img("Portrait", c, Dim(p.PortraitColor, 0.75f));
            UIKit.At(portrait.rectTransform, 0, 0, 300, 130);
            var letter = UIKit.Txt("Initial", portrait.transform, 84, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.9f));
            UIKit.Place(letter.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            letter.text = p.name.Substring(0, 1);
            var rk = UIKit.Txt("Rank", portrait.transform, 26, TextAnchor.UpperLeft, Color.white);
            UIKit.At(rk.rectTransform, 10, 6, 150, 32);
            rk.text = "RANK " + p.rank;
            if (p.unique)
            {
                var un = UIKit.Txt("Unik", portrait.transform, 22, TextAnchor.UpperRight, Gold);
                UIKit.At(un.rectTransform, 190, 8, 100, 30);
                un.text = "UNIK";
            }

            var nm = UIKit.Txt("Name", c, 26, TextAnchor.UpperLeft, Color.white);
            UIKit.At(nm.rectTransform, 12, 138, 276, 34);
            nm.text = p.name;
            var ag = UIKit.Txt("Age", c, 20, TextAnchor.UpperLeft, new Color(0.8f, 0.8f, 0.8f));
            UIKit.At(ag.rectTransform, 12, 172, 276, 28);
            ag.text = "Umur " + p.age + "  |  Rank " + p.rank;
            var rl = UIKit.Txt("Role", c, 22, TextAnchor.UpperLeft, p.PortraitColor + new Color(0.25f, 0.25f, 0.25f, 0f));
            UIKit.At(rl.rectTransform, 12, 198, 276, 28);
            rl.text = p.role + "  |  " + PartnerPersona.Of(p).name;
            var st = UIKit.Txt("Stats", c, 22, TextAnchor.UpperLeft, Color.white);
            UIKit.At(st.rectTransform, 12, 232, 276, 60);
            st.text = "HP " + p.hp + "    ATK " + p.atk + "\nDEF " + p.def + "    SPD " + p.spd;
            var cost = UIKit.Txt("Cost", c, 22, TextAnchor.UpperLeft, Gold);
            UIKit.At(cost.rectTransform, 12, 298, 276, 28);
            int eff = gd.CostFor(p);
            cost.text = "Biaya: " + eff + "% reward" + (eff != p.costPct ? " (Distrusted +10)" : "");
            var fm = UIKit.Txt("Fam", c, 18, TextAnchor.UpperLeft, new Color(0.7f, 0.85f, 1f));
            UIKit.At(fm.rectTransform, 12, 326, 276, 26);
            fm.text = fam > 0 ? "Akrab Lv " + fam + " (+" + gd.FamiliarityBonusPct(p.key) + "% damage/heal)" : "Belum pernah ikut quest bersamamu";
            var qt = UIKit.Txt("Quote", c, 18, TextAnchor.UpperLeft, new Color(0.7f, 0.7f, 0.72f));
            UIKit.At(qt.rectTransform, 12, 354, 276, 84);
            string ql = PartnerPersona.QuestLine(p);
            qt.fontSize = ql.Length > 0 ? 16 : 18;
            qt.text = "\"" + p.Line(fam) + "\"" + (ql.Length > 0 ? "\n" + ql : "");

            bool inParty = gd.InParty(p);
            string why = gd.RecruitBlockReason(p);
            var status = UIKit.Txt("Status", c, 18, TextAnchor.UpperLeft, inParty || why == "" ? Good : Bad);
            UIKit.At(status.rectTransform, 12, 442, 276, 44);
            status.text = inParty ? "Di party-mu" : why == "" ? "Tersedia" : why;

            var partner = p;
            Button btn;
            if (inParty) btn = UIKit.Btn(c, "Lepas dari party", () => { gd.Dismiss(partner); Rebuild(); }, true, new Color(0.45f, 0.22f, 0.2f), 22);
            else btn = UIKit.Btn(c, "Rekrut", () => { gd.Recruit(partner); Rebuild(); }, why == "", new Color(0.2f, 0.45f, 0.3f), 22);
            UIKit.At((RectTransform)btn.transform, 10, 484, 280, 40);
        }

        void BuildBottom()
        {
            var gs = GameState.I;
            var gd = GuildData.I;

            // Slot Favorit
            var fb = UIKit.Img("Fav", bottomRoot, Panel);
            UIKit.At(fb.rectTransform, 0, 0, 760, 190);
            var ft = UIKit.Txt("FavTitle", fb.transform, 24, TextAnchor.UpperLeft, Gold);
            UIKit.At(ft.rectTransform, 14, 8, 400, 30);
            ft.text = "Slot Favorit";
            var fav = gd.favorite;
            var fi = UIKit.Txt("FavInfo", fb.transform, 21, TextAnchor.UpperLeft, Color.white);
            UIKit.At(fi.rectTransform, 14, 42, 520, 140);
            if (fav == null)
            {
                fi.color = new Color(0.7f, 0.7f, 0.7f);
                fi.text = "Kosong. Ikut " + GuildConfig.FavoriteMinHires + " quest bersama partner yang sama untuk menjadikannya favorit. Favorit selalu tersedia tanpa menunggu poster.";
            }
            else
            {
                int fam = gd.FamiliarityLevel(fav.key);
                string why = gd.RecruitBlockReason(fav);
                bool inParty = gd.InParty(fav);
                fi.text = fav.name + "  (Rank " + fav.rank + ", " + fav.role + ")\nHP " + fav.hp + "  ATK " + fav.atk + "  DEF " + fav.def + "  SPD " + fav.spd +
                          "\nAkrab Lv " + fam + " (+" + gd.FamiliarityBonusPct(fav.key) + "% damage/heal)  |  Biaya " + gd.CostFor(fav) + "%\n" +
                          (inParty ? "Di party-mu" : why == "" ? "Tersedia" : why);
                var f = fav;
                Button b;
                if (inParty) b = UIKit.Btn(fb.transform, "Lepas", () => { gd.Dismiss(f); Rebuild(); }, true, new Color(0.45f, 0.22f, 0.2f), 22);
                else b = UIKit.Btn(fb.transform, "Rekrut", () => { gd.Recruit(f); Rebuild(); }, why == "", new Color(0.2f, 0.45f, 0.3f), 22);
                UIKit.At((RectTransform)b.transform, 560, 70, 180, 50);
            }

            // Party
            var pb = UIKit.Img("Party", bottomRoot, Panel);
            UIKit.At(pb.rectTransform, 780, 0, 770, 190);
            var pt = UIKit.Txt("PartyTitle", pb.transform, 24, TextAnchor.UpperLeft, Gold);
            UIKit.At(pt.rectTransform, 14, 8, 500, 30);
            pt.text = "Party-mu (" + (gd.party.Count + 1) + "/" + (GuildConfig.MaxPartners + 1) + ")";
            var pi = UIKit.Txt("PartyInfo", pb.transform, 21, TextAnchor.UpperLeft, Color.white);
            UIKit.At(pi.rectTransform, 14, 42, 742, 140);
            string s = "Kamu (" + JobInfo.Name(gs.job) + ", Rank " + gs.rank + ")";
            int total = 0;
            foreach (var m in gd.party) { s += "\n" + m.name + "  (" + m.role + ", Rank " + m.rank + ")  - " + m.hiredCost + "%"; total += m.hiredCost; }
            if (gd.party.Count > 0) s += "\nTotal bagi hasil partner: " + total + "% reward";
            s += "\n" + GuildServices.PartyRequirementText();
            pi.text = s;
        }
    }
}
