using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Mesin obrolan NPC yang tidak berbasis template tunggal: tiap NPC punya kepribadian & latar belakang tetap (dari namanya),
    /// ingatan tentang pemain (berapa kali bertemu, kalimat yang sudah dikatakan), sapaan yang berubah menurut keakraban/waktu,
    /// dan pilihan topik (kabar, dirinya, pekerjaan, rumor). Kalimat tidak diulang sebelum kolamnya habis.
    /// </summary>
    public static class NpcTalk
    {
        class Mem { public int visits, today, day = -1; public HashSet<string> used = new HashSet<string>(); public int facet; }
        static readonly Dictionary<string, Mem> mems = new Dictionary<string, Mem>();

        // ===== kepribadian: 0 ramah, 1 ceria, 2 ketus, 3 pemalu, 4 bijak =====
        static readonly string[][] Interj = {
            new[] { "Oh, ", "Nah, ", "Baiklah. ", "Begini, " },
            new[] { "Wah! ", "Hehe, ", "Asyik, ", "Eh, eh! " },
            new[] { "Hmph. ", "Ck. ", "Hah... ", "Apa lagi? " },
            new[] { "A-ah... ", "Um... ", "Ehm, ", "Maaf, ... " },
            new[] { "Hmm... ", "Begitulah. ", "Duduklah sebentar. ", "Ah, " } };
        static readonly string[][] Bye = {
            new[] { "Hati-hati di jalan, ya.", "Mampirlah lagi kalau sempat.", "Semoga harimu lancar." },
            new[] { "Dadah! Sampai ketemu lagi!", "Jangan lupa mampir, ya~", "Semangat terus, Hunter!" },
            new[] { "Sudah? Baguslah.", "Pergi sana, jangan menghalangi jalan.", "Hmph. Sampai nanti." },
            new[] { "Sampai jumpa... terima kasih sudah mau mengobrol.", "A-aku kembali bekerja, ya...", "Hati-hati..." },
            new[] { "Semoga jalanmu terang.", "Pergilah. Dunia menunggumu.", "Kita akan bertemu lagi, kurasa." } };

        // ===== latar belakang: dipilih tetap per NPC lewat seed =====
        static readonly string[][] Origin = {
            new[] { "Aku lahir di dusun kecil dekat padang gandum. Dulu kupikir dunia hanya seluas pagar desa.", "Orang tuaku pindah ke sini sebelum aku bisa berjalan. Jadi bagiku, kota inilah kampung halaman.", "Aku datang dari kota pelabuhan di timur. Bau garam masih suka terbawa mimpi.", "Dulu aku bocah jalanan. Sekarang setidaknya aku punya atap." },
            new[] { "Aku tumbuh di bawah gunung, di aula yang gemanya bisa bikin telinga berdenging seminggu.", "Keluargaku tiga generasi menambang. Aku yang pertama memilih naik ke permukaan.", "Di tanah kelahiranku, anak dwarf diberi palu kecil sebelum diberi nama." },
            new[] { "Aku besar di hutan tua. Pohon-pohon di sana kenal namaku, percayalah.", "Dulu rumahku di balik kabut perbatasan. Sudah lama aku tak pulang.", "Aku meninggalkan kaumku setelah berdebat tentang apakah dunia luar layak dilihat." },
            new[] { "Aku lahir di kemah pengembara. Langit adalah atapku, dan itu cukup.", "Dulu kaumku berpindah-pindah, tak diizinkan menetap. Di sini baru kurasa punya tempat.", "Ibuku mengajarkanku mencium cuaca sebelum hujan turun. Aku tak pernah salah." } };
        static readonly string[] Family = {
            "Di rumah ada ibuku yang sudah sepuh. Dia selalu bilang aku terlalu kurus.",
            "Aku punya adik yang keras kepala. Katanya mau jadi Hunter. Aku belum rela.",
            "Istriku... dia yang menjaga semua ini tetap berjalan. Aku hanya ikut-ikutan.",
            "Aku tinggal sendiri. Sepi, tapi tenang. Kucing tetangga sering menginap.",
            "Ayahku dulu seorang prajurit. Dia tak banyak bicara, tapi cara dia diam itu banyak artinya.",
            "Aku punya dua anak. Yang satu rajin, yang satu... ya, kuharap dia mengikuti kakaknya.",
            "Keluargaku sudah lama berpencar. Kadang ada surat, kadang tidak." };
        static readonly string[] Hobby = {
            "Kalau sedang senggang, aku suka duduk di tepi air mancur dan memperhatikan orang lewat.",
            "Aku mengumpulkan kerikil bentuk aneh. Kau mau lihat? ...Ah, lain kali saja.",
            "Setiap malam aku menulis catatan kecil tentang apa yang terjadi hari itu.",
            "Aku suka memasak, walau hasilnya kadang tak bisa dimakan. Kucingku pun menolak.",
            "Hobiku memancing. Hasil tangkapannya? Lebih sering cerita daripada ikan.",
            "Aku belajar memainkan seruling. Tetangga sudah mengeluh dua kali.",
            "Aku merawat tanaman kecil di jendela. Katanya, kalau diajak bicara, tumbuhnya lebih cepat." };
        static readonly string[] Worry = {
            "Jujur saja, belakangan aku sulit tidur. Banyak yang kupikirkan soal masa depan.",
            "Harga-harga naik terus. Entah sampai kapan aku sanggup bertahan begini.",
            "Aku khawatir pada orang-orang yang kusayang. Dunia di luar tembok tidak seramah dulu.",
            "Ada yang kusesali. Kata-kata yang tak sempat kuucapkan... ah, sudahlah.",
            "Kadang aku bertanya apakah pekerjaanku ini punya arti. Lalu seseorang tersenyum padaku, dan aku lupa bertanya." };
        static readonly string[] Dream = {
            "Impianku sederhana: suatu hari punya kedai kecil sendiri, dengan jendela menghadap matahari terbenam.",
            "Aku ingin melihat laut sekali saja sebelum tua.",
            "Kalau ada keberanian, aku ingin menjadi Hunter juga. Tapi kakiku masih gemetar memikirkannya.",
            "Aku ingin anak-anak di kota ini tumbuh tanpa takut pada malam.",
            "Seandainya bisa, aku ingin menyaksikan festival di Eldrath. Orang bilang lampunya seperti bintang yang turun." };

        // ===== balasan khusus topik yang tak punya jawaban baru =====
        static readonly string[] Exhausted = {
            "Hmm, rasanya semua yang bisa kuceritakan sudah kuceritakan. Kau pendengar yang baik.",
            "Kau menanyakan itu lagi? Hehe. Aku sudah kehabisan bahan, sungguh.",
            "Soal itu... sudah kita bahas, bukan? Tanyakan hal lain saja.",
            "Ceritaku mulai habis. Gantian dong, kau punya cerita apa?" };
        static readonly string[] Pushy = {
            "Kau banyak bertanya hari ini, ya. Aku suka, jarang ada yang mau mendengarkan.",
            "Sudah berapa kali kita bicara hari ini? Aku sampai lupa menghitung.",
            "Santai saja, Hunter. Aku tidak ke mana-mana." };

        // ===== pembuka berdasarkan keakraban =====
        static string Opening(NpcActor a, Mem m)
        {
            var gs = GameState.I; string rank = gs != null ? gs.rank.ToString() : "";
            float h = gs != null ? gs.timeOfDay : 12f;
            var list = new List<string>();
            if (m.visits == 0)
            {
                list.Add("Kita belum pernah bicara, ya? Namaku " + a.displayName + ".");
                list.Add("Orang baru? Aku " + a.displayName + ". Senang berkenalan.");
                list.Add("Hm? Kau Hunter, bukan? Aku " + a.displayName + ".");
            }
            else if (m.visits < 4)
            {
                list.Add("Oh, kau lagi. Senang melihatmu.");
                list.Add("Hei, kita bertemu lagi. Ada kabar baru?");
                list.Add("Wah, " + a.displayName.Split(' ')[0] + " tak menyangka kau mampir lagi.");
            }
            else
            {
                list.Add("Sahabat lama! Duduklah, tak usah sungkan.");
                list.Add("Nah, ini dia. Aku baru saja membayangkan kau akan lewat.");
                list.Add("Kau sudah seperti bagian dari tempat ini sekarang.");
            }
            if (m.day == GameCalendar.Day && m.today >= 1) list.Add("Kembali lagi hari ini? Ada yang tertinggal?");
            if (h >= 19f || h < 5f) list.Add("Larut begini masih berkeliaran? Baiklah, ayo bicara sebentar.");
            else if (h < 10f) list.Add("Pagi, Hunter. Aku baru mulai hari ini.");
            if (gs != null && gs.activeQuest != null) list.Add("Kudengar kau sedang mengurus misi. Jangan paksakan dirimu.");
            return Choose(m, list);
        }

        static string Choose(Mem m, List<string> pool)
        {
            var fresh = new List<string>();
            foreach (var s in pool) if (!m.used.Contains(s)) fresh.Add(s);
            if (fresh.Count == 0) { foreach (var s in pool) m.used.Remove(s); fresh = pool; }
            string pick = fresh[Random.Range(0, fresh.Count)];
            m.used.Add(pick); return pick;
        }

        static Mem Get(NpcActor a)
        {
            string k = a.displayName + "|" + a.race + "|" + a.role;
            Mem m; if (!mems.TryGetValue(k, out m)) { m = new Mem(); mems[k] = m; }
            return m;
        }
        static int Seed(NpcActor a)
        {
            int h = 17; foreach (char c in a.displayName) h = h * 31 + c;
            return Mathf.Abs(h * 7 + a.race * 13 + a.role);
        }
        static T At<T>(T[] arr, int seed) { return arr[seed % arr.Length]; }

        // ===== API =====
        public static string Greet(NpcActor a)
        {
            var m = Get(a); int s = Seed(a);
            if (m.day != GameCalendar.Day) { m.day = GameCalendar.Day; m.today = 0; }
            string open = Opening(a, m);
            string body = Spice(a, Choose(m, NpcDialogue.Pool(a.race, a.role)), s);
            m.visits++; m.today++;
            return open + "\n\n" + body;
        }

        public static string Topic(NpcActor a, int topic)
        {
            var m = Get(a); int s = Seed(a);
            string extra = m.today >= 4 && Random.value < 0.5f ? Choose(m, new List<string>(Pushy)) + "\n\n" : "";
            m.today++;
            switch (topic)
            {
                case 0: return extra + Spice(a, Choose(m, NpcDialogue.Pool(a.race, a.role)), s);
                case 1: return extra + About(a, m, s);
                case 2:
                    {
                        var p = new List<string>(NpcDialogue.RoleLines(a.role)); p.AddRange(NpcDialogueExtra.Role[Mathf.Clamp(a.role, 0, NpcDialogueExtra.Role.Length - 1)]);
                        return extra + Spice(a, Choose(m, p), s);
                    }
                default:
                    {
                        var p = new List<string>(NpcDialogueExtra.News); p.AddRange(NpcDialogue.RaceLines(a.race));
                        p.AddRange(StoryForeshadow.Rumors()); p.AddRange(StoryAwareness.Lines());
                        return extra + Spice(a, Choose(m, p), s);
                    }
            }
        }

        // cerita pribadi diungkap bertahap; hal yang lebih dalam hanya diceritakan setelah cukup akrab
        static string About(NpcActor a, Mem m, int s)
        {
            int r = Mathf.Clamp(a.race, 0, 3);
            string[] order = new string[5];
            order[0] = At(Origin[r], s); order[1] = At(Family, s / 3); order[2] = At(Hobby, s / 5);
            order[3] = At(Worry, s / 7); order[4] = At(Dream, s / 11);
            if (m.facet >= 5) return Choose(m, new List<string>(Exhausted));
            if (m.facet == 3 && m.visits < 3) return "Ada hal yang lebih pribadi... tapi kita belum cukup akrab. Mungkin lain kali.";
            if (m.facet == 4 && m.visits < 5) return "Soal mimpiku? Hehe, itu kusimpan untuk orang yang sudah sering mampir.";
            return order[m.facet++];
        }

        // sentuhan watak: kadang diawali seruan khas, kadang tidak (agar tidak pola)
        static string Spice(NpcActor a, string line, int s)
        {
            int t = s % 5;
            if (Random.value < 0.45f) line = Interj[t][Random.Range(0, Interj[t].Length)] + line;
            return line;
        }

        /// <summary>Satu kalimat latar NPC untuk Buku Nama (asal-usulnya).</summary>
        public static string Fact(NpcActor a)
        {
            int r = Mathf.Clamp(a.race, 0, 3); return At(Origin[r], Seed(a));
        }

        public static string Farewell(NpcActor a)
        {
            var b = Bye[Seed(a) % 5]; return b[Random.Range(0, b.Length)];
        }
    }
}
