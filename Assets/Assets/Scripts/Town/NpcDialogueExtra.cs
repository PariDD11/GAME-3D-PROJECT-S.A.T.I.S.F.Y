using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Tambahan kalimat NPC agar tiap obrolan terasa segar: peran (lebih banyak), musim, cuaca, festival, rank pemain,
    /// kabar kota (Hollow Circle, Gubernur, Eldrath), dan tidak mengulang kalimat yang baru saja dipakai.
    /// Dipanggil dari NpcDialogue.Greeting. Tambah kalimat baru cukup dengan menambah isi array di sini.
    /// </summary>
    public static class NpcDialogueExtra
    {
        public static readonly string[][] Role = {
            /*0 Warga*/ new[] { "Tadi pagi aku lihat serigala mondar-mandir di dekat gerbang. Aneh sekali.", "Kalau sempat, mampir ke pasar. Ada yang jual apel manis.", "Tetanggaku baru saja pulang dari Eldrath. Katanya di sana jauh lebih besar dari kota ini.", "Anak-anak tadi bermain di dekat air mancur sampai basah kuyup. Haha." },
            /*1 Pedagang*/ new[] { "Harga bahan sedang naik turun, tergantung musim. Beli sekarang atau menyesal nanti!", "Aku pernah menjual pedang ke seorang Hunter yang kini jadi legenda. Sungguh!", "Barang langka datang sebulan sekali. Jangan sampai terlewat.", "Kalau punya bahan monster berlebih, tawarkan ke Guild. Harganya bagus." },
            /*2 Petani*/ new[] { "Tanah musim ini subur sekali. Doaku terkabul.", "Kalau hujan datang, ladangku untung. Kalau badai, rugi besar.", "Aku menanam herbal di pinggir ladang. Kadang monster kecil ikut memakannya.", "Sapi-sapiku gelisah belakangan ini. Mungkin ada sesuatu di hutan." },
            /*3 Bangsawan*/ new[] { "Pesta musim ini pasti meriah. Sayang, undangannya terbatas.", "Seorang Hunter berprestasi bisa mendapat gelar. Pikirkan itu.", "Kekayaan tanpa kehormatan hanyalah angka di buku." },
            /*4 Penjaga*/ new[] { "Malam ini giliranku berjaga. Kopi adalah sahabatku.", "Ada orang berjubah mencurigakan terlihat di luar kota. Waspadalah.", "Gerbang kota selalu kujaga. Tak ada yang lolos tanpa sepengetahuanku.", "Hunter peringkat tinggi biasanya tenang. Yang berisik biasanya masih baru." },
            /*5 Ksatria*/ new[] { "Latihan pagi tadi melelahkan, tapi itulah jalan seorang ksatria.", "Kehormatan lebih berat dari baju zirah mana pun.", "Jika kau butuh bantuan melawan sesuatu yang besar, panggil kami." },
            /*6 Pendeta*/ new[] { "Cahaya selalu ada, bahkan di malam yang paling kelam.", "Jika lelah, beristirahatlah. Tubuh juga perlu dirawat.", "Aku mendoakan para Hunter yang pergi dan tak kembali." },
            /*7 Penyihir*/ new[] { "Mana mengalir lebih deras saat badai. Menarik, bukan?", "Setiap elemen punya wataknya sendiri. Api tak sabar, tanah keras kepala.", "Aku sedang meneliti inti golem. Sayang, bahannya mahal." },
            /*8 Hunter*/ new[] { "Tips: jangan serang buta. Perhatikan pola gerak monsternya dulu.", "Aku pernah hampir mati melawan boss. Sejak itu selalu bawa ramuan cadangan.", "Quest gathering memang kecil, tapi lumayan untuk bekal sehari-hari.", "Kalau stamina habis, mundur! Jangan sok kuat." },
            /*9 Pandai*/ new[] { "Gear yang dirawat bisa bertahan sepuluh kali lebih lama.", "Bahan dari monster kuat membuat tempaan paling tajam.", "Enchant memang mahal, tapi hasilnya sepadan." },
            /*10 Pelayan*/ new[] { "Kamar paling nyaman di lantai atas. Tenang dan jendelanya menghadap taman.", "Sarapan disajikan sampai pukul sembilan, ya.", "Tamu semalam meninggalkan satu kantung koin kecil. Baik sekali." },
            /*11 Bard*/ new[] { "Aku baru menggubah lagu tentang pertarungan di hutan. Mau dengar? Nanti ya.", "Setiap kota punya lagunya sendiri. Kota ini... riang tapi waspada.", "Kalau namamu masuk lagu, itu artinya kau terkenal." },
            /*12 Koki*/ new[] { "Sup hangat cocok untuk hari berhujan.", "Bahan segar adalah kunci. Herbal, daging, dan sedikit kesabaran.", "Hunter yang makan teratur lebih kuat. Itu kenyataan!" },
            /*13 Pelajar*/ new[] { "Tahukah kamu, tiap monster punya kelemahan yang berbeda?", "Aku mencatat perilaku slime. Ternyata mereka menyukai tempat lembap.", "Buku di perpustakaan menyimpan banyak rahasia dunia ini." },
            /*14 Pengembara*/ new[] { "Di jalan, aku bertemu pedagang yang kehilangan keledainya. Lucu sekali.", "Setiap kota menyimpan kisah. Tinggal kita mau mendengar atau tidak.", "Aku bermimpi melihat menara raksasa di balik awan." },
            /*15 Petugas*/ new[] { "Pendaftaran Hunter baru terus meningkat belakangan ini.", "Jangan lupa laporkan quest yang sudah selesai. Hadiahnya menunggu.", "Peringkat naik bukan karena beruntung, tapi karena konsisten." } };

        static readonly string[][] SeasonLines = {
            /*Semi*/ new[] { "Bunga-bunga bermekaran. Musim semi memang paling kusuka.", "Udara hangat begini cocok untuk piknik. Sayang banyak monster berkeliaran.", "Musim semi, saatnya menanam. Herbal tumbuh subur." },
            /*Panas*/ new[] { "Panas sekali hari ini. Air minum jangan sampai habis.", "Musim panas membuat malam lebih pendek. Sayang sekali.", "Anak-anak bermain di sungai tiap sore. Ramai sekali." },
            /*Gugur*/ new[] { "Daun-daun berguguran. Pemandangan paling indah menurutku.", "Panen hampir tiba. Semua orang sibuk di ladang.", "Musim gugur membuat angin dingin. Siapkan jubah." },
            /*Dingin*/ new[] { "Dingin sekali... aku butuh sup hangat.", "Salju menutupi jalan. Hati-hati terpeleset.", "Di musim dingin, monster lebih buas karena kekurangan makanan." } };

        static readonly string[] Rain = { "Hujan lagi... jalanan jadi becek.", "Kalau hujan begini, lebih enak berdiam di penginapan.", "Dengar suara hujan di atap? Menenangkan." };
        static readonly string[] Storm = { "Badai! Jangan berkeliaran di luar kalau tak perlu.", "Petir tadi hampir menyambar menara jam!" };
        static readonly string[] Snow = { "Salju turun lebat. Pakai pakaian tebal ya.", "Aku suka salju, tapi tidak suka kakiku membeku." };
        static readonly string[] Sunny = { "Cerah sekali! Hari yang bagus untuk berburu.", "Matahari bersinar. Semangat, Hunter!" };
        static readonly string[] Festival = { "Hari ini festival! Sudah lihat alun-alun?", "Festival hari ini meriah sekali. Jangan lewatkan!", "Ada makanan gratis di alun-alun. Cepat sebelum habis!" };

        static readonly string[] RankLow = { "Hunter baru, ya? Semangat, semua orang pernah mulai dari bawah.", "Rank rendah bukan masalah. Yang penting terus belajar." };
        static readonly string[] RankHigh = { "Rank setinggi itu... kau pasti sudah melihat banyak hal.", "Seorang Hunter hebat. Boleh aku minta tanda tangan?", "Nama Hunter peringkat atas selalu jadi buah bibir." };

        public static readonly string[] News = {
            "Kudengar ada kelompok berjubah yang disebut Hollow Circle. Orang-orang takut menyebut nama mereka.",
            "Gubernur dan Guild tampaknya tidak akur belakangan ini. Entah siapa yang benar.",
            "Eldrath, kota kedua kita, katanya megah dan penuh pasar. Aku ingin ke sana suatu hari.",
            "Ada yang bilang naga terlihat di pegunungan jauh. Semoga hanya rumor.",
            "Penjaga gerbang menambah patroli malam. Pasti ada sesuatu.",
            "Aku dengar menara besar muncul di balik kabut. Misterius sekali." };

        static readonly System.Collections.Generic.Queue<string> recent = new System.Collections.Generic.Queue<string>();

        public static void Fill(List<string> pool, int race, int role)
        {
            if (role >= 0 && role < Role.Length) { pool.AddRange(Role[role]); pool.AddRange(Role[role]); }
            pool.AddRange(News);
            { var sl = StoryAwareness.Lines(); pool.AddRange(sl); pool.AddRange(sl); }       // kabar sesuai perkembangan cerita pemain
            pool.AddRange(StoryForeshadow.Rumors());                                          // v35z102n77: desas-desus yang menyiratkan kejadian kelak
            var gs = GameState.I;
            if (gs == null) return;
            int si = GameCalendar.SeasonIndex(GameCalendar.Day);
            if (si >= 0 && si < SeasonLines.Length) pool.AddRange(SeasonLines[si]);
            if (GameCalendar.IsFestival(GameCalendar.Day)) { pool.AddRange(Festival); pool.AddRange(Festival); }
            if (SeasonRuntime.Outdoors)
            {
                switch (SeasonRuntime.Weather)
                {
                    case Wx.Hujan: pool.AddRange(Rain); break;
                    case Wx.Badai: pool.AddRange(Storm); break;
                    case Wx.Salju: case Wx.Blizzard: pool.AddRange(Snow); break;
                    case Wx.Cerah: pool.AddRange(Sunny); break;
                }
            }
            int ri = GameState.RankIndex(gs.rank);
            if (ri <= 1) pool.AddRange(RankLow); else if (ri >= GameState.Ranks.Length - 3) pool.AddRange(RankHigh);
        }

        /// <summary>Pilih acak, menghindari ~30 kalimat terakhir yang sudah dipakai (global) agar tidak terasa berulang.</summary>
        public static string Pick(List<string> pool)
        {
            if (pool.Count == 0) return "...";
            string s = pool[Random.Range(0, pool.Count)];
            for (int i = 0; i < 24 && recent.Contains(s) && pool.Count > 1; i++) s = pool[Random.Range(0, pool.Count)];
            recent.Enqueue(s); while (recent.Count > 30) recent.Dequeue();
            return s;
        }
    }
}
