using System;
using System.Collections.Generic;

namespace HunterGuild
{
    /// <summary>Pemilihan job permanen di level 5 (GDD 4.2). Dibuka otomatis saat game mulai sampai pemain memilih.</summary>
    public static class JobMenu
    {
        static Action backTo;

        /// <summary>Pratinjau satu job (dipakai konsultasi Brann). back = layar yang dibuka tombol kembali.</summary>
        public static void OpenPreview(string job, Action back) { backTo = back; Preview(job, 0); }

        public static void Open()
        {
            backTo = null;
            TownUI.I.Open("Pilih Job (Level 5)",
                () => "Job bersifat PERMANEN dan menentukan gaya bertarung serta skill tree-mu.",
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Swordsman - Pedang & Perisai\nMelee, damage per hit tinggi, guard dan parry, combo berbasis stamina.", () => Confirm("Swordsman")));
                    l.Add(new UIEntry("Archer - Busur\nRanged, kontrol jarak, sniping titik lemah, panah khusus, mobilitas tinggi.", () => Confirm("Archer")));
                    l.Add(new UIEntry("Penyihir Api - Tongkat Api\nSihir jarak jauh: bola api, gelombang api, meteor. Membakar musuh (damage berkelanjutan) dan menghabisi kerumunan.", () => Confirm("FireMage")));
                    l.Add(new UIEntry("Penyihir Tanah - Tongkat Batu\nSihir jarak jauh: lonjakan batu, gempa, dinding batu. Memperlambat, menahan, dan sangat tahan serangan.", () => Confirm("EarthMage")));
                    foreach (var j in new[] { "WaterMage", "WindMage", "Priest" })
                    {
                        string jj = j;
                        l.Add(new UIEntry(JobInfo.Name(j) + "\n" + JobInfo.Info(j), () => Confirm(jj)));
                    }
                    return l;
                });
        }

        // ---------------- Pratinjau job (menggantikan menu "Yakin atau tidak") ----------------
        static string Weapon(string job)
        {
            switch (job)
            {
                case "Swordsman": return "Pedang & Perisai";
                case "Archer": return "Busur";
                case "FireMage": return "Tongkat Api";
                case "EarthMage": return "Tongkat Batu";
                case "WaterMage": return "Tongkat Air";
                case "WindMage": return "Tongkat Angin";
                default: return "Tongkat Suci";
            }
        }

        static string Style(string job)
        {
            switch (job)
            {
                case "Swordsman": return "Jarak dekat  |  Tahan banting: tinggi  |  Kesulitan: mudah";
                case "Archer": return "Jarak jauh  |  Mobilitas: tinggi  |  Kesulitan: sedang";
                case "FireMage": return "Jarak jauh  |  Damage area: sangat tinggi  |  Tubuh: rapuh  |  Kesulitan: sedang";
                case "EarthMage": return "Jarak jauh  |  Kontrol & tahan serangan: tinggi  |  Kesulitan: mudah";
                case "WaterMage": return "Jarak jauh  |  Kontrol & pemulihan diri  |  Kesulitan: sedang";
                case "WindMage": return "Jarak jauh  |  Kecepatan & serangan beruntun  |  Kesulitan: sulit";
                default: return "Dukungan  |  Penyembuh & pelindung tim  |  Kesulitan: sedang";
            }
        }

        static string Cut(string t, int n) { return t != null && t.Length > n ? t.Substring(0, n - 1).TrimEnd() + "…" : t; }

        static void Confirm(string job) { Preview(job, 0); }

        static void Choose(string job)
        {
            var gs = GameState.I;
            if (gs.jobChosen) return;
            gs.job = job;
            gs.jobChosen = true;
            TownUI.I.Close();
            TownUI.I.Toast("Brann: \"Pilihan yang tepat. Kau sekarang seorang " + JobInfo.Name(job) + ". Tekan K untuk skill, Tab untuk tas.\"");
        }

        /// <summary>page 0 = ringkasan, 1 = skill, 2 = bocoran kelas kedua.</summary>
        static void Preview(string job, int page)
        {
            string title = page == 0 ? JobInfo.Name(job) : page == 1 ? JobInfo.Name(job) + " - Skill" : JobInfo.Name(job) + " - Kelas Kedua";
            TownUI.I.Open(title,
                () =>
                {
                    if (page == 0)
                    {
                        string b = "";
                        var br = SkillDB.Branches(job);
                        for (int i = 0; i < br.Length; i++) b += (i > 0 ? "\n" : "") + "  \u2022 " + br[i];
                        var evs = JobInfo.EvosOf(job);
                        string ev = "";
                        for (int i = 0; i < evs.Count; i++) ev += (i > 0 ? ", " : "") + evs[i].name;
                        return "<color=#F0D58A>" + JobInfo.Info(job) + "</color>\n\n"
                             + "Senjata: " + Weapon(job) + "\n" + Style(job) + "\n\n"
                             + "Tiga cabang skill:\n" + b + "\n\n"
                             + "<color=#9FD3FF>Bocoran: di Level " + JobInfo.EvoLevel + " kamu bisa berevolusi ke kelas kedua (" + ev + "), lalu kelas ketiga di Level " + JobInfo.Evo2Level + ".</color>\n"
                             + "<color=#E58E8E>Job bersifat PERMANEN.</color>";
                    }
                    if (page == 1)
                    {
                        string t = "";
                        foreach (var br in SkillDB.Branches(job))
                        {
                            t += "<color=#F0D58A>" + br + "</color>\n";
                            var t2 = SkillDB.Row(job, br, 2);
                            var t3 = SkillDB.Row(job, br, 3);
                            string names = "";
                            foreach (var sk in t2) names += (names == "" ? "" : ", ") + sk.name;
                            if (names != "") t += "  Aktif: " + names + "\n";
                            foreach (var sk in t3) t += "  Ultimate: <color=#FFD27A>" + sk.name + "</color> - " + Cut(sk.desc, 70) + "\n";
                        }
                        return t + "\nSkill dipelajari dengan Skill Point (tekan K).";
                    }
                    string r = "Evolusi menanti di Level " + JobInfo.EvoLevel + " (pilih satu), dan bercabang lagi di Level " + JobInfo.Evo2Level + ":\n\n";
                    foreach (var e in JobInfo.EvosOf(job))
                    {
                        r += "<color=#F0D58A>" + e.name + "</color>\n  " + e.desc + "\n";
                        string n2 = "";
                        foreach (var e2 in JobInfo.Evos2Of(e.id)) n2 += (n2 == "" ? "" : ", ") + e2.name;
                        if (n2 != "") r += "  <color=#9FD3FF>Lv." + JobInfo.Evo2Level + " \u2192 " + n2 + "</color>\n";
                    }
                    return r;
                },
                () =>
                {
                    var l = new List<UIEntry>();
                    if (page != 1) l.Add(new UIEntry("Lihat skill >", () => Preview(job, 1)));
                    if (page != 2) l.Add(new UIEntry("Bocoran kelas kedua >", () => Preview(job, 2)));
                    if (page != 0) l.Add(new UIEntry("< Ringkasan", () => Preview(job, 0)));
                    var gsn = GameState.I;
                    bool can = gsn == null || gsn.level >= StarterConfig.JobLevel || !gsn.requireJobChoice;
                    if (can && gsn != null && !gsn.jobChosen) l.Add(new UIEntry("Pilih " + JobInfo.Name(job) + " (PERMANEN)", () => Choose(job)));
                    else if (!can) l.Add(new UIEntry("Memilih job baru bisa di Level " + StarterConfig.JobLevel, null, false));
                    if (backTo != null) l.Add(new UIEntry("< Kembali", () => backTo())); else l.Add(new UIEntry("< Kembali ke daftar job", Open));
                    return l;
                });
        }
    }
}
