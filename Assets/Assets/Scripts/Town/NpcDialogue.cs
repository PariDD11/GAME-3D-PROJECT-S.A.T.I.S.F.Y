using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Kumpulan kalimat sapaan & obrolan NPC (dipilih acak menurut peran, ras, waktu, dan misi).</summary>
    public static class NpcDialogue
    {
        // ras: 0 Manusia, 1 Dwarf, 2 Elf, 3 Beastfolk
        static readonly string[] Generic = {
            "Selamat datang, Hunter!", "Hai! Cuaca bagus hari ini, ya.", "Oh, halo! Semoga harimu menyenangkan.", "Salam, pengelana!",
            "Hati-hati di jalan, ya.", "Wah, kamu orang dari Guild, kan? Keren sekali!", "Selamat datang di kota kami!", "Semoga beruntung dalam perburuanmu!",
            "Ada yang bisa kubantu?", "Hm? Oh, halo. Kota ini ramai sekali hari ini.", "Dengar-dengar monster di hutan makin liar akhir-akhir ini.",
            "Jangan lupa makan yang cukup sebelum berburu!" };
        static readonly string[] Morning = { "Selamat pagi! Udaranya segar sekali.", "Pagi, Hunter! Sudah sarapan?", "Matahari baru naik, semangat ya!" };
        static readonly string[] Night = { "Malam begini masih berkeliaran? Hati-hati ya.", "Lentera sudah menyala... kota ini cantik kalau malam.", "Selamat malam, Hunter. Jangan pulang terlalu larut.", "Hoaam... sebentar lagi aku pulang." };
        static readonly string[] Quest = { "Kudengar kamu sedang mengerjakan misi. Semoga lancar!", "Misi dari Guild, ya? Bawa potion cadangan!", "Pulanglah dengan selamat, ya." };
        static readonly string[][] Race = {
            new[] { "Kami para manusia mungkin tak sekuat dwarf, tapi kami pantang menyerah!", "Kota ini dibangun bersama banyak ras, kami bangga." },
            new[] { "Hrmph! Baja terbaik tentu hasil tempa dwarf!", "Jenggotku ini sudah tiga ratus tahun, Nak. Hormati yang tua!", "Bir dan palu, itu kebahagiaan sejati!", "Gunung adalah rumahku, tapi kota ini lumayan juga." },
            new[] { "Hutan berbisik tentang kedatanganmu, pengelana.", "Usiaku sudah dua abad, tapi kota ini masih mengejutkanku.", "Semoga angin hutan menyertai langkahmu.", "Manusia selalu terburu-buru... tapi aku suka semangat kalian." },
            new[] { "Hidungku mencium bau petualangan darimu!", "Telingaku mendengar langkahmu dari jauh, lho~", "Ekorku bergoyang kalau bertemu orang baik. Lihat?", "Kami beastfolk dulu dijauhi... sekarang kota ini menerima kami." } };
        static readonly string[][] Role = {
            /*0 Warga*/ new[] { "Pasar hari ini ramai, ya.", "Anakku ingin jadi Hunter sepertimu.", "Harga roti naik lagi... haah." },
            /*1 Pedagang*/ new[] { "Barang bagus, harga bersahabat! Mampir ya!", "Rempah dari seberang lautan, murah meriah!", "Untuk Hunter, kuberi diskon... di dalam mimpi. Hehe." },
            /*2 Petani*/ new[] { "Panen tahun ini melimpah, syukurlah.", "Awas injak ladangku, ya!", "Serigala sering mengintip ternakku di malam hari." },
            /*3 Bangsawan*/ new[] { "Hm, seorang rakyat jelata. Baiklah, kuizinkan kau menyapaku.", "Teh sore adalah satu-satunya kemewahan yang kupercaya.", "Jaga sikapmu di hadapan keluarga kerajaan." },
            /*4 Penjaga*/ new[] { "Berhenti. ...Oh, Hunter Guild. Silakan lewat.", "Kota ini aman selama aku berjaga.", "Gerbang ditutup saat malam larut. Ingat itu.", "Lapor kalau melihat sesuatu yang mencurigakan." },
            /*5 Ksatria*/ new[] { "Demi kehormatan kerajaan!", "Pedangku untuk melindungi yang lemah.", "Hunter, kau dan aku sama-sama pelindung kota." },
            /*6 Pendeta*/ new[] { "Semoga cahaya melindungimu.", "Berdoalah sebelum berburu, anakku.", "Jiwa yang tenang adalah pedang terbaik." },
            /*7 Penyihir*/ new[] { "Mana di sekitarmu... menarik sekali.", "Jangan sentuh ramuanku, itu bisa meledak. Mungkin.", "Sihir adalah ilmu, bukan keajaiban." },
            /*8 Hunter*/ new[] { "Sesama Hunter! Semangat berburunya!", "Monster hari ini lumayan alot, loh.", "Kalau butuh partner, cari aku di Guild." },
            /*9 Pandai*/ new[] { "Senjatamu perlu diasah? Datang ke bengkel!", "Klang! Klang! Besi bagus butuh kesabaran.", "Bajamu retak? Ah, itu tanda orang yang bertempur." },
            /*10 Pelayan*/ new[] { "Selamat datang! Silakan, kami punya kamar kosong!", "Hari ini sup spesial kami enak sekali.", "Mau pesan sesuatu, Tuan?" },
            /*11 Bard*/ new[] { "♪ La la laa~ Mau dengar balada Hunter pemberani?", "Aku menulis lagu tentang kota ini!", "Koin untuk sang penyair, boleh?" },
            /*12 Koki*/ new[] { "Hm, kau terlihat lapar. Makan dulu, Hunter!", "Rahasia masakanku? Cinta... dan banyak garam.", "Daging panggang segar hari ini!" },
            /*13 Pelajar*/ new[] { "Aku sedang mempelajari ekologi monster!", "Perpustakaan adalah tempat terbaik di dunia.", "Boleh tahu apa kau pernah melihat Wyvern asli?" },
            /*14 Pengembara*/ new[] { "Aku sudah menjelajah tujuh benua, kau tahu?", "Jalan panjang mengajarkan banyak hal.", "Kalau kau mendengar cerita aneh, ceritakan padaku." },
            /*15 Petugas*/ new[] { "Dokumen sudah lengkap? Bagus.", "Guild selalu butuh Hunter yang disiplin.", "Selamat datang di Guild Hunter." } };
        static readonly string[] Farewell = { "Sampai jumpa!", "Hati-hati!", "Sampai bertemu lagi!", "Jaga dirimu!" };

        public static string Greeting(int race, int role, bool allowQuestLines = true)
        {
            return NpcDialogueExtra.Pick(Pool(race, role, allowQuestLines));
        }

        public static string[] RoleLines(int role) { return role >= 0 && role < Role.Length ? Role[role] : new string[0]; }
        public static string[] RaceLines(int race) { return race >= 0 && race < Race.Length ? Race[race] : new string[0]; }

        /// <summary>Seluruh kandidat kalimat (peran, ras, waktu, cuaca, musim, misi) untuk NPC ini saat ini.</summary>
        public static List<string> Pool(int race, int role, bool allowQuestLines = true)
        {
            var pool = new List<string>();
            float h = GameState.I != null ? GameState.I.timeOfDay : 12f;
            bool night = h >= 19f || h < 5f;
            // peran diberi bobot tertinggi
            if (role >= 0 && role < Role.Length) { pool.AddRange(Role[role]); pool.AddRange(Role[role]); }
            if (race >= 0 && race < Race.Length) pool.AddRange(Race[race]);
            pool.AddRange(Generic);
            if (night) { pool.AddRange(Night); pool.AddRange(Night); }
            else if (h < 10f) pool.AddRange(Morning);
            if (allowQuestLines && GameState.I != null && GameState.I.activeQuest != null) pool.AddRange(Quest);
            NpcDialogueExtra.Fill(pool, race, role);
            return pool;
        }

        public static string Chat(int race, int role)
        {
            return Greeting(race, role) + "\n\n" + Greeting(race, role, false) + "\n\n\"" + Farewell[Random.Range(0, Farewell.Length)] + "\"";
        }
    }
}
