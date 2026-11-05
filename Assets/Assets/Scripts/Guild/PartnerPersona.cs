using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n24: partner jadi karakter. Tiap partner punya watak (persona) yang menentukan ucapannya saat bertarung
    /// (mulai tempur, terluka, tumbang, bangkit, khawatir pada pemain, boss tumbang) dan, untuk partner unik, sebuah misi pribadi
    /// yang membuka bonus kecil permanen (+HP / +ATK / +kekuatan penyembuhan). Partner biasa mendapat bonus "Sahabat Seperjalanan" di Akrab Lv 5.
    /// Progres disimpan di GameState.stats / flags (pq_p_*, pq_acc_*, pq_done_*) dan ikut save slot.
    /// </summary>
    public static class PartnerPersona
    {
        public class Persona
        {
            public string id, name, desc;
            public string[] engage, hurt, down, revive, worry, boss;
        }

        static readonly Dictionary<string, Persona> all = new Dictionary<string, Persona>();
        static string[] S(string s) { return s.Split('|'); }
        static void P(string id, string name, string desc, string engage, string hurt, string down, string revive, string worry, string boss)
        {
            all[id] = new Persona { id = id, name = name, desc = desc, engage = S(engage), hurt = S(hurt), down = S(down), revive = S(revive), worry = S(worry), boss = S(boss) };
        }

        static PartnerPersona()
        {
            P("berani", "Pemberani", "Maju paling depan, tidak suka mundur.",
              "Serahkan di depan padaku!|Mereka datang. Bagus.|Jangan lari, kita hadapi!",
              "Ini belum apa-apa!|Masih bisa berdiri...|Lukanya dangkal, lanjut!",
              "Maaf... aku harus rebah sebentar...|Jangan... biarkan mereka lewat...",
              "Aku kembali! Jangan hitung aku mati.|Sekali lagi, ayo!",
              "Awas, nyawamu menipis! Mundur!|Aku di depanmu, jangan nekat!",
              "Kita menang! Itu baru pertarungan!|Boss pun jatuh juga akhirnya.");
            P("ceria", "Ceria", "Selalu bersemangat, bahkan di tengah bahaya.",
              "Wah, ramai sekali! Seru!|Siap-siap, ya! Kita pasti bisa!|Hehe, giliranku!",
              "Aduh! Sakitnya... tapi aku masih kuat!|Jangan khawatir, ini cuma gores!",
              "Aku istirahat sebentar, ya...|Maaf, jangan marah...",
              "Aku sudah baikan! Terima kasih sudah menunggu!|Sudah bangun lagi, hehe!",
              "Hei, kamu terluka parah! Minum ramuannya!|Jangan sampai jatuh, aku sedih nanti!",
              "Kita berhasil! Hebat sekali kamu!|Yey! Itu tadi luar biasa!");
            P("tenang", "Tenang", "Berkepala dingin dan jarang bicara.",
              "Hitung napas. Mulai.|Posisi terbaca. Bergerak.|Mereka datang, tetap tenang.",
              "Luka ini bisa ditahan.|Cukup. Aku masih bisa melanjutkan.",
              "Aku jatuh... lanjutkan tanpaku sebentar.|Tidak apa-apa. Selamatkan dirimu.",
              "Aku kembali. Terima kasih.|Napas teratur lagi. Ayo.",
              "Hatimu panik. Atur napas, lalu mundur.|Lukamu parah. Perhatikan sekitarmu.",
              "Selesai. Tidak perlu berlebihan.|Satu masalah berkurang.");
            P("teliti", "Teliti", "Memperhatikan detail dan merencanakan langkah.",
              "Pola gerak mereka sudah kubaca.|Tujuh langkah, lalu serang sisi kirinya.|Formasi kita aman. Mulai.",
              "Kerusakan 40 persen. Masih dalam batas.|Aku salah menghitung jarak...",
              "Perhitunganku meleset. Maaf...|Catat: jangan ulangi kesalahan ini...",
              "Kondisi pulih. Kembali ke formasi.|Aku sudah mencatat kelemahannya.",
              "Nyawamu di bawah batas aman. Mundur sekarang.|Ramuan dulu, serang belakangan.",
              "Sesuai rencana. Catatannya kuberi tanda.|Boss itu punya satu kelemahan, dan kita menemukannya.");
            P("sinis", "Sinis", "Bicara pedas, tetapi tetap bisa diandalkan.",
              "Lagi-lagi begini. Baiklah.|Mereka tidak belajar, ya.|Jangan pandangi aku, cepat serang.",
              "Bagus. Sekarang aku kesal.|Ini bagian dari bayaran, kurasa.",
              "Bagus. Tumbang lagi. Jangan komentar...|Aku tidak mati. Hanya... beristirahat.",
              "Jangan senyum begitu. Ayo.|Aku kembali. Terpaksa.",
              "Kamu bodoh kalau terus maju begitu. Mundur!|Aku tidak mau mengubur klien. Minum ramuanmu.",
              "Boss lagi, boss lagi. Gaji kita naik, kan?|Selesai. Jangan minta aku merayakannya.");
            P("disiplin", "Disiplin", "Bekerja sesuai kontrak, mantan prajurit.",
              "Perintah diterima. Bergerak.|Formasi, tahan garis!|Kontrak berlaku. Maju.",
              "Luka ringan. Tidak mengganggu tugas.|Aku tidak akan mundur tanpa perintah.",
              "Aku jatuh... laporkan tugas selesai...|Lanjutkan misi tanpaku.",
              "Siap bertugas kembali.|Pulih. Melapor siap.",
              "Pemimpin terluka. Mundur dan pulihkan diri. Itu perintah.|Hentikan serangan, prioritaskan keselamatan.",
              "Target dinetralkan. Tidak ada korban di pihak kita.|Misi tuntas.");
            P("angkuh", "Angkuh", "Suka menang dan tidak mau kalah dari siapa pun.",
              "Biar kutunjukkan caranya.|Jangan halangi garis tembakku.|Hitung saja berapa yang jatuh olehku.",
              "Beraninya mereka...|Itu kebetulan. Tidak akan terulang.",
              "Mustahil... aku jatuh?|Jangan lihat. Aku butuh waktu.",
              "Aku tidak kalah. Hanya jeda.|Sekarang giliranku membalas.",
              "Kamu payah kalau terluka. Berhenti sebentar.|Jangan mati sebelum kita selesai bersaing.",
              "Kuakui, itu tadi layak dihitung. Satu poin untukmu.|Boss pun tidak selamat dari kita.");
            P("bijak", "Bijak", "Tua, sabar, dan penuh pengalaman.",
              "Pelan-pelan, nak. Biar yang tua di depan.|Aku sudah lihat yang lebih buruk dari ini.",
              "Tulang tua tidak sekeras dulu...|Luka ini mengingatkanku pada masa muda.",
              "Aku sudah tua... kau lanjutkan, nak...|Hargai hidup selagi ia hangat...",
              "Aku bangkit lagi. Mati belum waktunya.|Tulang tua keras kepala juga.",
              "Nak, kau terlalu berani. Mundur dulu.|Jangan wariskan lukamu kepada hari esok.",
              "Satu lagi malam yang bisa tidur nyenyak.|Anak muda, kamu hebat. Ingat itu.");
            P("dingin", "Dingin", "Tidak banyak bicara dan penuh rahasia.",
              "Tidak perlu percaya padaku. Cukup lihat.|Selesaikan cepat.",
              "Aku pernah lebih parah.|Hmph.",
              "Jangan lihat.|Aku... baik-baik saja.",
              "Terima kasih. Itu saja.|Aku masih punya utang.",
              "Mundur. Aku tidak bisa menyeretmu pulang.|Kamu terluka. Jangan jadi beban.",
              "Dungeon atau monster, semuanya sama.|Selesai. Bayar tepat waktu.");
        }

        // ----- watak tiap partner
        static readonly Dictionary<string, string> UniquePersona = new Dictionary<string, string>
        {
            { "u:kael", "disiplin" }, { "u:mira", "ceria" }, { "u:draven", "sinis" }, { "u:elysia", "angkuh" },
            { "u:torvin", "bijak" }, { "u:nyx", "dingin" }, { "u:seraphine", "tenang" }, { "u:dagan", "sinis" },
        };
        static readonly string[] GenericPool = { "berani", "ceria", "tenang", "teliti", "sinis" };

        public static Persona Of(Partner p)
        {
            if (p == null) return all["tenang"];
            string id;
            if (!UniquePersona.TryGetValue(p.key ?? "", out id))
            {
                int h = 17; foreach (char c in p.key ?? "") h = h * 31 + c;
                id = GenericPool[Mathf.Abs(h) % GenericPool.Length];
            }
            return all[id];
        }

        /// <summary>Ucapan acak untuk sebuah peristiwa: engage, hurt, down, revive, worry, boss.</summary>
        public static string Line(Partner p, string ev)
        {
            var ps = Of(p); string[] a;
            switch (ev) { case "engage": a = ps.engage; break; case "hurt": a = ps.hurt; break; case "down": a = ps.down; break; case "revive": a = ps.revive; break; case "worry": a = ps.worry; break; default: a = ps.boss; break; }
            return a[Random.Range(0, a.Length)];
        }

        // ----- misi pribadi (partner unik)
        public class PQ
        {
            public string key, title, intro, done; public int need; public string[] monsters; public bool boss, large;
            public string perk;      // hp | atk | heal
            public float perkAmt;
        }

        public static readonly List<PQ> Quests = new List<PQ>
        {
            new PQ { key = "u:kael", title = "Perintah yang Tak Terlupakan", need = 40, perk = "atk", perkAmt = 0.12f,
                intro = "Kael mau menuntaskan satu perintah lama: kalahkan 40 monster bersamanya.",
                done = "Kael: \"Perintah itu sudah selesai. Aku tidak lagi merasa terikat. Terima kasih, Hunter.\"" },
            new PQ { key = "u:mira", title = "Resep Terakhir Ibu", need = 12, monsters = new[] { "alpha_wolf", "dire_wolf", "giant_boar" }, perk = "heal", perkAmt = 0.25f,
                intro = "Mira butuh bahan daging buruan untuk resep ibunya: kalahkan 12 serigala atau babi hutan bersamanya.",
                done = "Mira: \"Resepnya akhirnya lengkap! Aku sekarang bisa memasak dan meramu lebih mujarab untukmu.\"" },
            new PQ { key = "u:draven", title = "Yang Bukan Monster", need = 20, monsters = new[] { "skeleton", "wraith", "mummy", "drowned" }, perk = "hp", perkAmt = 0.2f,
                intro = "Draven ingin membuktikan sesuatu yang pernah ia lihat: kalahkan 20 undead bersamanya.",
                done = "Draven: \"Aku tidak salah lihat. Dan sekarang ada yang percaya padaku. Itu cukup.\"" },
            new PQ { key = "u:elysia", title = "Adu Bidik", need = 2, boss = true, perk = "atk", perkAmt = 0.12f,
                intro = "Elysia menantang: kalahkan 2 boss bersamanya, dan lihat siapa yang lebih sering menembak.",
                done = "Elysia: \"Seri. Untuk pertama kalinya aku tidak kecewa dengan seri.\"" },
            new PQ { key = "u:torvin", title = "Retakan di Bukit Timur", need = 15, large = true, perk = "hp", perkAmt = 0.2f,
                intro = "Torvin ingin mengingatkan bukit timur: kalahkan 15 monster besar bersamanya.",
                done = "Torvin: \"Enam puluh satu musim dingin, dan aku masih punya alasan baru untuk bangun pagi.\"" },
            new PQ { key = "u:nyx", title = "Sisi yang Benar", need = 25, monsters = new[] { "bandit", "bandit_archer", "bandit_leader" }, perk = "atk", perkAmt = 0.12f,
                intro = "Nyx ingin menebus masa lalunya: kalahkan 25 bandit bersamanya.",
                done = "Nyx: \"Aku berdiri di sisi yang benar kali ini. Jangan beri tahu siapa pun.\"" },
            new PQ { key = "u:seraphine", title = "Pilihan Terakhir", need = 3, boss = true, perk = "heal", perkAmt = 0.25f,
                intro = "Seraphine ingin melihat apakah kau sanggup memilih: kalahkan 3 boss bersamanya.",
                done = "Seraphine: \"Kau memilih dengan hati-hati. Aku akan menjaga langkahmu sampai akhir.\"" },
        };

        public static PQ QuestOf(string key) { foreach (var q in Quests) if (q.key == key) return q; return null; }
        static GameState GS { get { return GameState.I; } }
        public static bool Accepted(string key) { return GS != null && GS.flags.Contains("pq_acc_" + key); }
        public static bool Done(string key) { return GS != null && GS.flags.Contains("pq_done_" + key); }
        public static int Progress(string key) { return GS == null ? 0 : GS.Stat("pq_p_" + key); }
        public static int Fam(string key) { return GuildData.I == null ? 0 : GuildData.I.FamiliarityLevel(key); }

        /// <summary>Teks status untuk kartu rekrutmen; kosong bila partner tidak punya misi.</summary>
        public static string QuestLine(Partner p)
        {
            var q = QuestOf(p.key); if (q == null) return Done(p.key) ? "Sahabat Seperjalanan: +6% HP/ATK" : "Akrab Lv 5 membuka bonus Sahabat";
            if (Done(q.key)) return "Misi selesai: " + q.title;
            if (Accepted(q.key)) return "Misi: " + q.title + " " + Mathf.Min(Progress(q.key), q.need) + "/" + q.need;
            return "Misi pribadi terbuka di Akrab Lv 2";
        }

        /// <summary>Dipanggil saat sebuah partner masuk party / tiap kill: membuka misi bila syarat akrab terpenuhi.</summary>
        public static void Refresh()
        {
            if (GS == null || GuildData.I == null) return;
            foreach (var p in GuildData.I.party)
            {
                var q = QuestOf(p.key);
                if (q != null && !Accepted(q.key) && Fam(q.key) >= 2) { GS.flags.Add("pq_acc_" + q.key); if (TownUI.I != null) TownUI.I.Toast(p.name + " - Misi pribadi: " + q.title + ". " + q.intro); }
                if (q == null && Fam(p.key) >= 5 && !Done(p.key)) { GS.flags.Add("pq_done_" + p.key); if (TownUI.I != null) TownUI.I.Toast(p.name + " kini Sahabat Seperjalanan: bonus HP dan ATK kecil."); }
            }
        }

        /// <summary>Dipanggil dari Monster.Die.</summary>
        public static void OnKill(MonsterDef def, bool isBoss)
        {
            if (GS == null || GuildData.I == null || def == null) return;
            Refresh();
            foreach (var p in GuildData.I.party)
            {
                var q = QuestOf(p.key);
                if (q == null || !Accepted(q.key) || Done(q.key)) continue;
                bool ok = q.boss ? isBoss : q.large ? def.large : q.monsters == null ? true : System.Array.IndexOf(q.monsters, def.id) >= 0;
                if (!ok) continue;
                int n = Progress(q.key) + 1; GS.stats["pq_p_" + q.key] = n;
                if (n >= q.need)
                {
                    GS.flags.Add("pq_done_" + q.key);
                    if (TownUI.I != null) TownUI.I.Toast(q.done);
                    Audio.Sfx("quest_done");
                }
            }
            if (isBoss) foreach (var a in new List<AllyUnit>(AllyUnit.All)) if (a != null && !a.Down) { a.Bark("boss"); break; }
        }

        // ----- bonus
        static float Perk(string key, string kind)
        {
            if (!Done(key)) return 0f;
            var q = QuestOf(key);
            if (q == null) return kind == "hp" || kind == "atk" ? 0.06f : 0f;       // Sahabat Seperjalanan
            return q.perk == kind ? q.perkAmt : 0f;
        }
        public static float HpMul(string key) { return 1f + Perk(key, "hp"); }
        public static float AtkMul(string key) { return 1f + Perk(key, "atk"); }
        public static float HealMul(string key) { return 1f + Perk(key, "heal"); }
    }
}
