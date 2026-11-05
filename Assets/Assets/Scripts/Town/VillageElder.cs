using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Brann, veteran petualang Desa Lerai: pembimbing pemain dari Level 0 sampai Level 5. Memberi 5 misi latihan berurutan (StarterQuests),
    /// memberi nasihat sesuai level, lalu di Level 5 mewawancarai pemain (4 pertanyaan) sebelum merekomendasikan job.
    /// (Nama kelas dipertahankan "VillageElder" supaya scene Desa yang sudah dibangun tetap menempel; yang berubah hanya karakternya.)
    /// </summary>
    public class VillageElder : Interactable, IQuestGiver
    {
        public const string MentorName = "Veteran Brann";
        public const string MentorTitle = "Petualang Veteran";
        public string elderName = MentorName;

        /// <summary>Ada pembimbing di scene ini (dipakai PlayerController agar menu job tidak dipaksa saat Brann bisa ditemui).</summary>
        public static bool Exists;

        protected override void OnEnable() { base.OnEnable(); Exists = true; }
        protected override void OnDisable() { base.OnDisable(); Exists = false; }

        void Start()
        {
            // scene lama masih menamai NPC ini "Tetua Haldor": ganti saat runtime agar tidak perlu membangun ulang scene
            elderName = MentorName; speakerName = MentorName; speakerRole = MentorTitle; prompt = "Bicara: " + MentorName;
            var actor = GetComponent<NpcActor>(); if (actor != null) actor.displayName = MentorName;
            var tm = GetComponentInChildren<TextMesh>(); if (tm != null) tm.text = MentorName + "\n<" + MentorTitle + ">";
        }

        string Title { get { return MentorName + " - " + MentorTitle; } }

        public override void Interact()
        {
            var gs = GameState.I;
            if (!gs.flags.Contains("stq_met"))
            {
                gs.flags.Add("stq_met"); Audio.Voice("guildmaster", "Hm? Wajah baru.");
                Intro();
                return;
            }
            OpenMain();
        }

        // ------------------------------------------------------------------ perkenalan pertama
        void Intro()
        {
            TownUI.I.Open(Title,
                () => "\"Hm? Wajah baru. Kau pasti calon Hunter yang baru tiba di Desa Lerai.\n\n" +
                      "Namaku Brann. Dulu aku Hunter rank A; sekarang cuma pria tua dengan lutut rewel yang mengawasi desa ini. " +
                      "Semua orang mulai dari nol, dan aku yang akan membimbingmu dari Level 1 sampai Level 5: pukul, bertahan hidup, berburu. " +
                      "Setelah itu barulah kau pantas memilih jalanmu, job-mu. Dan aku akan membantumu memilih, kalau kau mau.\"",
                () =>
                {
                    var l = new List<UIEntry>();
                    DialogueUI.Card = null;
                    l.Add(new UIEntry("Aku siap dibimbing, Brann.", OpenMain));
                    l.Add(new UIEntry("Kenapa kau berhenti jadi Hunter?", Backstory));
                    return l;
                });
        }

        void Backstory()
        {
            TownUI.I.Open(Title,
                () => "\"Lutut kiriku hancur di Menara Eldrath, lantai 40, dihantam golem batu. Aku masih hidup karena rekan satu tim menahan golem itu. " +
                      "Dia tidak pulang. Sejak itu aku bersumpah: anak baru yang lewat desa ini tidak boleh berangkat tanpa bekal dan tanpa tahu kemampuan sendiri.\"",
                () => { var l = new List<UIEntry>(); l.Add(new UIEntry("Aku mengerti. Ajari aku.", OpenMain)); return l; });
        }

        // ------------------------------------------------------------------ sapaan menurut tahap
        /// <summary>v35z102n11: 1 = latihan baru tersedia / saatnya memilih job, 2 = latihan siap dilaporkan, 0 = sedang berjalan atau semua selesai.</summary>
        public int QuestMarkState()
        {
            try
            {
                int cur = StarterQuests.Current;
                if (cur >= StarterQuests.All.Length) return NeedsJob ? 1 : 0;
                var st = StarterQuests.All[cur];
                if (StarterQuests.Ready(st)) return 2;
                return StarterQuests.Accepted(st) ? 0 : 1;
            }
            catch { return 0; }
        }

        bool NeedsJob { get { var gs = GameState.I; return gs.requireJobChoice && !gs.jobChosen && gs.level >= StarterConfig.JobLevel; } }

        string Greeting()
        {
            var gs = GameState.I;
            int cur = StarterQuests.Current;
            if (NeedsJob && cur >= StarterQuests.All.Length)
                return "\"Level 5. Aku sudah menunggu hari ini. Sekarang saatnya memilih jalan hidupmu; ini bukan keputusan kecil, job tidak bisa diganti. " +
                       "Mau kutanya-tanya dulu supaya pilihanmu tepat?\"";
            if (cur >= StarterQuests.All.Length)
                return "\"Kau sudah bukan anak baru. Ikuti jalan timur menuju Ashwatch dan daftarkan dirimu di Guild. Desa ini selalu menantimu pulang.\"";
            var s = StarterQuests.All[cur];
            if (StarterQuests.Ready(s)) return "\"Wajahmu cerah. Pasti ada kabar baik. Laporkan misimu.\"";
            if (StarterQuests.Accepted(s)) return "\"Latihanmu belum selesai: " + s.title + ". Jangan terburu-buru; tubuh belajar lewat pengulangan. Butuh nasihat?\"";
            if (cur == 0) return "\"Mulai dari yang paling dasar. Kau belum pegang senjata; tinjumu dulu yang harus bicara. Tanya aku kalau bingung.\"";
            return "\"Kau kembali. Siap untuk latihan berikutnya?\"";
        }

        void OpenMain()
        {
            var gs = GameState.I;
            TownUI.I.Open(Title,
                () => Greeting(),
                () =>
                {
                    var l = new List<UIEntry>();
                    DialogueUI.Card = null;
                    l.Add(new UIEntry("Latihan / misi dari Brann", AskQuest));
                    if (NeedsJob) l.Add(new UIEntry("Aku sudah Level 5, bantu aku memilih job >", () => CounselIntro(true)));
                    else if (!gs.jobChosen) l.Add(new UIEntry("Job apa yang cocok untukku? (konsultasi)", () => CounselIntro(false)));
                    l.Add(new UIEntry("Minta nasihat >", Advice));
                    l.Add(new UIEntry("Tanya: dua jalan keluar desa >", Roads));
                    l.Add(new UIEntry("Tanya: soal job dan level >", JobInfoPage));
                    l.Add(new UIEntry("Aku ingin tanya soal properti & rumah", AskProperty));
                    l.Add(new UIEntry("Pulihkan tenaga (gratis)", () =>
                    {
                        if (PlayerCombat.I != null) PlayerCombat.I.FullRestore();
                        gs.hunger = Mathf.Max(gs.hunger, 60f); gs.thirst = Mathf.Max(gs.thirst, 60f);
                        TownUI.I.Toast("Sup hangat buatan Brann memulihkan tenagamu."); Audio.Sfx("potion");
                    }));
                    l.Add(new UIEntry("Tutup", () => TownUI.I.Close()));
                    return l;
                });
        }

        // ------------------------------------------------------------------ misi bimbingan
        static readonly string[] AcceptLines =
        {
            "Pertama, tinju. Senjata tak berguna kalau tubuhmu lemah. Hajar boneka di tengah desa; ingat: tiga serangan beruntun itu kombo, dan Ctrl untuk berguling (Spasi untuk melompat).",
            "Bagus, dasarnya sudah ada. Sekarang rasakan monster sungguhan: Slime di hutan utara. Kecil, tapi jangan diremehkan; bawa roti dan air.",
            "Hunter bukan hanya petarung. Petik Herb di hutan; ramuan menyelamatkan nyawa lebih sering daripada pedang.",
            "Goblin itu licik dan bergerombol. Jangan serang buta: tunggu serangannya, guling, baru balas. Ingat itu.",
            "Satu langkah lagi. Capai Level 5 dengan berburu, lalu kembali padaku. Di situ kita bicara soal jalan hidupmu."
        };

        static readonly string[] ClaimLines =
        {
            "Tinju yang bagus. Tubuhmu sudah mulai mengerti.",
            "Slime-nya habis? Bagus. Itu baru permulaan.",
            "Herb-nya segar. Kau punya mata yang jeli.",
            "Goblin sudah ketakutan padamu sekarang. Hati-hati, jangan sombong.",
            "Level 5. Kau lulus. Sekarang, soal jalanmu..."
        };

        void AskQuest()
        {
            TownUI.I.Open(Title,
                () =>
                {
                    int cur = StarterQuests.Current;
                    if (cur >= StarterQuests.All.Length) return "\"Tidak ada lagi yang perlu kuajarkan soal dasar. Semua latihan sudah kau selesaikan.\"";
                    var s = StarterQuests.All[cur];
                    if (StarterQuests.Ready(s)) return "\"" + ClaimLines[Mathf.Clamp(cur, 0, ClaimLines.Length - 1)] + " Laporkan, biar kuberi hadiahnya.\"";
                    if (StarterQuests.Accepted(s)) return "\"Misimu masih berjalan. Selesaikan dulu sebelum mengambil yang lain.\"";
                    return "\"Ada. Latihan berikutnya sudah kusiapkan. Lihat sendiri.\"";
                },
                () =>
                {
                    var l = new List<UIEntry>();
                    DialogueUI.Card = null;
                    int cur = StarterQuests.Current;
                    if (cur < StarterQuests.All.Length)
                    {
                        var s = StarterQuests.All[cur];
                        if (StarterQuests.Ready(s)) l.Add(new UIEntry("Lapor: " + s.title + " (selesai)", () => { StarterQuests.Claim(s); AfterClaim(cur); }));
                        else if (StarterQuests.Accepted(s)) l.Add(new UIEntry("Misi berjalan: " + s.title + " (" + StarterQuests.Progress(s) + "/" + s.count + ")", null, false));
                        else l.Add(new UIEntry("Latihan " + (cur + 1) + "/" + StarterQuests.All.Length + ": " + s.title, () => QuestDetail(s)));
                    }
                    l.Add(new UIEntry("< Kembali", OpenMain));
                    return l;
                });
        }

        void AfterClaim(int stage)
        {
            bool last = stage >= StarterQuests.All.Length - 1;
            TownUI.I.Open(Title,
                () => "\"" + ClaimLines[Mathf.Clamp(stage, 0, ClaimLines.Length - 1)] + "\"" + (last ? "\n\n\"Hari ini kau bukan anak baru lagi. Mau aku bantu memilih job sekarang?\"" : ""),
                () =>
                {
                    var l = new List<UIEntry>();
                    DialogueUI.Card = null;
                    if (last && NeedsJob) l.Add(new UIEntry("Ya, tanya-tanya aku dulu >", () => CounselIntro(true)));
                    else if (!last) l.Add(new UIEntry("Latihan berikutnya >", AskQuest));
                    l.Add(new UIEntry("Kembali", OpenMain));
                    return l;
                });
        }

        void QuestDetail(StarterQuests.Stage s)
        {
            int idx = System.Array.IndexOf(StarterQuests.All, s);
            TownUI.I.Open(Title,
                () => "\"" + AcceptLines[Mathf.Clamp(idx, 0, AcceptLines.Length - 1)] + "\"",
                () =>
                {
                    var l = new List<UIEntry>();
                    DialogueUI.Card = new DialogueCard { title = "Latihan dari Brann: " + s.title, desc = s.desc, objective = "Target: " + s.count + " (0/" + s.count + ")", reward = StarterQuests.RewardText(s), acceptPrefix = "Terima latihan" };
                    l.Add(new UIEntry("Terima latihan: " + s.title, () => { StarterQuests.Accept(s); DialogueUI.Card = null; OpenMain(); }));
                    l.Add(new UIEntry("< Kembali", AskQuest));
                    return l;
                });
        }

        // ------------------------------------------------------------------ nasihat menurut level
        void Advice()
        {
            TownUI.I.Open(Title,
                () =>
                {
                    int lv = GameState.I.level;
                    if (lv <= 1) return "\"Level awal: jangan pakai senjata dulu. Pelajari ritme: serang, mundur, guling. Kalau HP tinggal setengah, mundur dan makan. Hunter yang hidup lebih berharga daripada yang berani.\"";
                    if (lv <= 3) return "\"Slime dan Goblin akan mengajarimu tiga hal: jarak, waktu, dan sabar. Jangan lupa Herb dan roti di tas (Tab). Lapar dan haus menggerogotimu pelan-pelan.\"";
                    if (lv == 4) return "\"Satu level lagi. Pikirkan sekarang: kau suka bertarung di depan, atau menyerang dari jauh? Pikirkan juga elemen. Jawabannya menentukan job-mu.\"";
                    if (!GameState.I.jobChosen) return "\"Kau sudah Level 5 tapi belum memilih job. Tanyakan padaku soal job; aku akan menanyaimu dulu supaya pilihanmu pas.\"";
                    return "\"Setelah ini: naikkan level, pelajari skill (K), dan ambil quest di Guild Ashwatch. Di Level 15 kau berevolusi, di Level 30 sekali lagi.\"";
                },
                () => { var l = new List<UIEntry>(); l.Add(new UIEntry("< Kembali", OpenMain)); return l; });
        }

        // ------------------------------------------------------------------ konsultasi job (wawancara)
        static readonly string[] Jobs = { "Swordsman", "Archer", "FireMage", "EarthMage", "WaterMage", "WindMage", "Priest" };

        sealed class Opt { public string text; public int[] score; public Opt(string t, params int[] s) { text = t; score = s; } }
        sealed class Qn { public string q; public Opt[] opts; public Qn(string q, params Opt[] o) { this.q = q; opts = o; } }

        //                                                   Sword Arch Fire Earth Water Wind Priest
        static readonly Qn[] Questions =
        {
            new Qn("Seekor monster menerjang ke arahmu. Reaksimu yang paling alami?",
                new Opt("Kuhadapi langsung, jarak dekat.",                                 3, 0, 0, 1, 0, 0, 1),
                new Opt("Mundur dan tembak dari jauh.",                                     0, 3, 1, 0, 1, 1, 0),
                new Opt("Hancurkan dengan ledakan sihir.",                                  0, 0, 3, 1, 1, 1, 0),
                new Opt("Lindungi diri dan rekan, bertahan dulu.",                          1, 0, 0, 2, 0, 0, 3)),
            new Qn("Gaya bertarung seperti apa yang membuatmu merasa kuat?",
                new Opt("Cepat dan lincah, sulit disentuh.",                                1, 1, 0, 0, 0, 3, 0),
                new Opt("Kokoh dan kuat, tahan pukulan.",                                   2, 0, 0, 3, 0, 0, 1),
                new Opt("Cerdas, mengontrol medan dan lawan.",                              0, 1, 0, 1, 3, 1, 1),
                new Opt("Satu ledakan damage besar.",                                       1, 1, 3, 0, 0, 1, 0)),
            new Qn("Mana yang paling menarik bagimu?",
                new Opt("Api yang membakar habis.",                                         0, 0, 3, 0, 0, 0, 0),
                new Opt("Kekuatan tanah dan batu.",                                         0, 0, 0, 3, 0, 0, 0),
                new Opt("Air dan es yang menenangkan sekaligus mematikan.",                 0, 0, 0, 0, 3, 0, 0),
                new Opt("Angin yang tak tertangkap.",                                       0, 0, 0, 0, 0, 3, 0),
                new Opt("Cahaya suci untuk menyembuhkan.",                                  0, 0, 0, 0, 0, 0, 3),
                new Opt("Bukan sihir. Aku lebih suka senjata.",                             3, 3, 0, 0, 0, 0, 0)),
            new Qn("Kau lebih nyaman dengan risiko seperti apa?",
                new Opt("Rapuh tapi mematikan. Menang atau tumbang.",                       0, 1, 2, 0, 0, 2, 0),
                new Opt("Seimbang, tidak terlalu berani, tidak terlalu takut.",             2, 1, 0, 0, 2, 0, 1),
                new Opt("Aman dan stabil. Aku mau bertahan lama.",                          1, 0, 0, 2, 0, 0, 3))
        };

        static readonly int[] tally = new int[7];

        void CounselIntro(bool canChoose)
        {
            TownUI.I.Open(Title + " - Konsultasi Job",
                () => canChoose
                    ? "\"Bagus. Dengar, aku tidak akan memilihkan, hanya membantumu mengenal dirimu sendiri. Ada empat pertanyaan. Jawab jujur, bukan jawaban yang kau kira bagus.\""
                    : "\"Belum Level 5, tapi tidak ada salahnya mulai berpikir. Aku akan bertanya empat hal, lalu kuberi gambaran job yang cocok. Ingat jawabannya sampai Level 5.\"",
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Mulai pertanyaan", () => { for (int i = 0; i < tally.Length; i++) tally[i] = 0; Ask(0, canChoose); }));
                    l.Add(new UIEntry("Langsung lihat daftar job", () => OpenJobList(canChoose)));
                    l.Add(new UIEntry("< Kembali", OpenMain));
                    return l;
                });
        }

        void OpenJobList(bool canChoose)
        {
            if (canChoose) JobMenu.Open();
            else TownUI.I.Toast("Lihat info semua job di menu 'Tanya: soal job dan level'. Memilih baru bisa di Level 5.");
        }

        void Ask(int qi, bool canChoose)
        {
            var q = Questions[qi];
            TownUI.I.Open(Title + " - Pertanyaan " + (qi + 1) + "/" + Questions.Length,
                () => "\"" + q.q + "\"",
                () =>
                {
                    var l = new List<UIEntry>();
                    DialogueUI.Card = null;
                    foreach (var o in q.opts)
                    {
                        var oo = o;
                        l.Add(new UIEntry(oo.text, () =>
                        {
                            for (int i = 0; i < tally.Length && i < oo.score.Length; i++) tally[i] += oo.score[i];
                            if (qi + 1 < Questions.Length) Ask(qi + 1, canChoose); else Result(canChoose);
                        }));
                    }
                    l.Add(new UIEntry("< Batal", OpenMain));
                    return l;
                });
        }

        void Result(bool canChoose)
        {
            int a = 0, b = -1;
            for (int i = 1; i < tally.Length; i++) if (tally[i] > tally[a]) a = i;
            for (int i = 0; i < tally.Length; i++) { if (i == a) continue; if (b < 0 || tally[i] > tally[b]) b = i; }
            string ja = Jobs[a], jb = Jobs[b];
            TownUI.I.Open(Title + " - Hasil",
                () => "\"Hm. Setelah mendengarmu, menurutku kau paling cocok jadi <color=#FFD27A>" + JobInfo.Name(ja) + "</color>.\n\n" + Reason(ja) + "\n\n" +
                      "Pilihan kedua yang masuk akal: <color=#9FD3FF>" + JobInfo.Name(jb) + "</color>. " + Reason(jb) + "\n\n" +
                      (canChoose ? "Keputusan akhir ada di tanganmu. Job tidak bisa diganti, jadi lihat detailnya dulu.\"" : "Ingat ini sampai Level 5. Masih ada waktu untuk berubah pikiran.\""),
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Lihat detail " + JobInfo.Name(ja), () => JobMenu.OpenPreview(ja, () => Result(canChoose))));
                    l.Add(new UIEntry("Lihat detail " + JobInfo.Name(jb), () => JobMenu.OpenPreview(jb, () => Result(canChoose))));
                    l.Add(new UIEntry("Jawab ulang pertanyaan", () => CounselIntro(canChoose)));
                    if (canChoose) l.Add(new UIEntry("Lihat semua job", () => JobMenu.Open()));
                    l.Add(new UIEntry("< Kembali", OpenMain));
                    return l;
                });
        }

        static string Reason(string job)
        {
            switch (job)
            {
                case "Swordsman": return "Kau suka garis depan dan kontrol langsung. Pedang dan perisai memberimu ketahanan dan damage per pukulan tertinggi.";
                case "Archer": return "Kau menghargai jarak dan ketepatan. Pemanah menang dengan posisi, bukan kekuatan kasar.";
                case "FireMage": return "Kau ingin ledakan. Api membakar kerumunan dan menghabisi lawan, dengan harga tubuh yang rapuh.";
                case "EarthMage": return "Kau ingin kokoh dan mengontrol medan. Batu menahan, memperlambat, dan membuatmu sulit tumbang.";
                case "WaterMage": return "Kau berpikir sebelum bertindak. Air dan es mengontrol lawan sekaligus memulihkan diri.";
                case "WindMage": return "Kau suka kecepatan. Angin memberi serangan beruntun dan mobilitas, tetapi menuntut ketelitian.";
                default: return "Kau menopang orang lain. Priest menyembuhkan dan melindungi, dan cahaya suci menghukum yang jahat.";
            }
        }

        // ------------------------------------------------------------------ info lain
        void AskProperty()
        {
            TownUI.I.Open(Title,
                () => "\"Rumah? Desa ini punya beberapa tempat yang bisa dibeli atau disewa. Mau kujelaskan, atau langsung lihat daftarnya?\"",
                () =>
                {
                    var l = new List<UIEntry>();
                    DialogueUI.Card = null;
                    l.Add(new UIEntry("Tunjukkan daftar properti", () => PropertyMenus.Open(AskProperty)));
                    l.Add(new UIEntry("< Kembali", OpenMain));
                    return l;
                });
        }

        void Roads()
        {
            TownUI.I.Open("Dua Jalan Keluar Desa",
                () => "Jalan UTARA menuju Hutan Pemula: tempat berlatih Hunter muda. Slime, Goblin, dan banyak Herb. Naikkan levelmu sampai 5 di sana.\n\n" +
                      "Jalan TIMUR menuju Ashwatch dan Guild Hunter. Penjaga hanya mengizinkan Hunter Level " + StarterConfig.CityMinLevel + " ke atas yang sudah memilih job. Kota itu keras untuk pemula.",
                () => { var l = new List<UIEntry>(); l.Add(new UIEntry("< Kembali", OpenMain)); return l; });
        }

        void JobInfoPage()
        {
            TownUI.I.Open("Job dan Level",
                () =>
                {
                    string t = "Kamu mulai dari Level 0 dengan TANGAN KOSONG (tinju) sampai Level 4. Di Level 5 kamu memilih job PERMANEN, dan Brann akan membantumu:\n\n";
                    foreach (var j in Jobs)
                    { string inf = HunterGuild.JobInfo.Info(j); int dot = inf.IndexOf(". "); if (dot > 0) inf = inf.Substring(0, dot + 1); t += HunterGuild.JobInfo.Name(j) + ": " + inf + "\n"; }
                    return t + "\nTiap level memberi 1 Skill Point (tekan K). Skill aktif dipasang di slot Z X C V R T Q (V = slot tengah besar, cocok untuk ultimate).";
                },
                () => { var l = new List<UIEntry>(); l.Add(new UIEntry("< Kembali", OpenMain)); return l; });
        }
    }
}
