using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// Tutorial kontekstual + Panduan. Petunjuk muncul sekali saat situasinya terjadi (disimpan sebagai flag "tut_*" di save),
    /// dan semua isinya bisa dibaca ulang lewat Esc > Panduan.
    /// </summary>
    public class Tutorial : MonoBehaviour
    {
        static Tutorial inst;
        float sceneTime, nextHint, poll;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (inst != null) return;
            var go = new GameObject("Tutorial");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<Tutorial>();
            SceneManager.sceneLoaded += (s, m) => { inst.sceneTime = Time.unscaledTime; };
            inst.sceneTime = Time.unscaledTime;
        }

        // ---------------- Panduan (dibaca ulang kapan saja) ----------------
        public static readonly string[][] Guide =
        {
            new[] { "Kontrol",
                "WASD gerak, Shift lari, Ctrl menghindar, Spasi lompat, klik kiri / J menyerang, klik kanan menahan.\n" +
                "E interaksi, Tab tas, K skill, M peta, F minum, G ubah karakter (di kota).\n" +
                "1-6 item cepat, Z X C V R T Q skill, Esc menu jeda.\n" +
                "Lompat ganda hanya untuk Penyihir Angin; job lain melompat sekali (Spasi di udara = meluncur bila stamina cukup).\n" +
                "Memanjat dinding hanya bisa di area hunt dan Menara, tidak di kota/desa." },
            new[] { "Alur Dasar",
                "1) Datangi Guild, ambil quest di Papan Quest (mulai Rank G).\n" +
                "2) Keluar lewat gerbang kota yang sesuai wilayah quest, beli perbekalan di Gerbang Luar.\n" +
                "3) Selesaikan buruan, kembali ke Base Camp lalu pulang ke kota.\n" +
                "4) Lapor ke resepsionis Guild: dapat gold, EXP, reputasi; naik rank lewat ujian." },
            new[] { "Empat Gerbang Kota",
                "Selatan: Hutan Lerai (wilayah pemula).\n" +
                "Timur: Gurun Zahar (panas, butuh air).\n" +
                "Barat: Rawa Terkutuk (kabut tebal, berbahaya).\n" +
                "Utara: jalan raya aman ke Kota Eldrath, tempat Menara Eldrath dan cabang Guild.\n" +
                "Gerbang hanya terbuka untuk wilayah quest yang kamu ambil." },
            new[] { "Berburu",
                "Quest 'hunt' butuh jejak dulu: temukan 3 jejak buruan, lalu kompas menunjuk arahnya.\n" +
                "Kompas / Peta Jelajah dijual penjaga Gerbang Luar tiap berangkat dan hangus saat pulang.\n" +
                "Bawa ramuan, makanan, dan air. Pingsan 3 kali = quest gagal (reputasi turun, deposit hangus)." },
            new[] { "Menara Eldrath",
                "100 lantai, boss tiap 10 lantai. Kalahkan semua musuh agar portal naik terbuka.\n" +
                "Setiap boss mengalahkan = checkpoint; kamu bisa mulai lagi dari lantai kelipatan 10 yang sudah dibuka.\n" +
                "Bicara dengan Guild di Eldrath untuk quest menara dan Papan Peringkat." },
            new[] { "Party & Rank",
                "Rekrutmen partner dibuka mulai Rank B di meja resepsionis Guild (maks 2 partner).\n" +
                "Partner ikut bertarung, mengambil sebagian reward, lalu cooldown. Makin sering disewa makin akrab (bonus kecil).\n" +
                "Quest rank B ke atas mewajibkan party. Rank naik lewat ujian di Guild dan reputasi." },
            new[] { "Bertahan Hidup",
                "Hunger habis ~36 jam game, thirst ~24 jam. Makan di restoran, isi botol minum di sumber air.\n" +
                "Tidur di penginapan memulihkan dan menyimpan progres. Terlalu lama terjaga (48 jam) membuat lelah.\n" +
                "Progres tersimpan otomatis di kota, Guild, dan Eldrath." },
            new[] { "Simpan & Checkpoint",
                "Di area hunt dan Menara, checkpoint otomatis tiap 2 menit saat tenang (tidak bertarung). Menu Jeda juga punya Simpan checkpoint.\n" +
                "Memuat checkpoint dari area hunt memunculkanmu di kota dengan item, EXP, dan progres quest yang tersimpan.\n" +
                "Arena Ujian tidak bisa disimpan. Pengaturan > Simpanan membuka folder file save untuk cadangan manual." },
            new[] { "Perjalanan Cepat",
                "Menu Jeda > Perjalanan cepat. Dari kota/desa: ke tempat yang pernah dikunjungi, ke area quest aktif, dan ke checkpoint Menara.\n" +
                "Dari area hunt/Menara: Pulang ke kota (quest tetap berjalan, progres hunt tidak ikut tersimpan). Tidak bisa saat bertarung." },
            new[] { "Kesulitan & Putaran Baru",
                "Pengaturan > Gameplay: Santai, Normal, Sulit, Mimpi Buruk, plus Mati Permanen (tidak bisa dimatikan lagi).\n" +
                "Kesulitan tinggi dan NG+ membuat monster lebih kuat, tetapi EXP dan gold quest/Menara juga lebih besar.\n" +
                "NG+ terbuka di Menara lantai 100; Menara diulang dengan monster lebih kuat." },
            new[] { "Cerita & Pencapaian",
                "Cerita utama 'Nama Yang Dilarang' berjalan lewat NPC bertanda ! di Desa, Ashwatch, dan Eldrath (mulai Level 5).\n" +
                "Menu Jeda > Pencapaian: pencapaian lokal dan Rekor Pribadi. Menu Jeda > Koleksi & Gelar: pelacak Bestiary/Lore/boss rahasia dan gelar kosmetik. Tanda ? di atas NPC = kamu membawa surat untuknya." },
            new[] { "Kasus, Boss Rahasia & Tantangan",
                "Papan Pengumuman Guild: kasus investigasi berantai (tuntaskan satu untuk membuka berikutnya). Cari kilau emas, tanyai saksi, lalu tuduh pelaku.\n" +
                "Tiap area hunt punya Prasasti Tua (petunjuk arah) dan Altar yang memanggil boss rahasia; kalahkan untuk gear unik.\n" +
                "Menara Eldrath > Tantangan Menara (terbuka setelah NG+): Tanpa Ramuan, Batas Waktu, Satu Nyawa. Hadiah: gelar kosmetik.\n" +
                "Jurnal (J) > RINGKASAN menampilkan quest, tujuan, dan petunjuk dalam satu layar; tab RESEP menampilkan resep Pandai Besi." },
            new[] { "Elemen, Status & Tantangan",
                "Tiap monster punya elemen (Api, Air, Tanah, Angin, Cahaya, Gelap). Serang elemen yang dilemahkannya = damage x1.5 dan mengisi gauge BREAK; gauge penuh membuat musuh terhuyung dan menerima damage x1.4. Penyihir memakai elemen jobnya; Swordsman/Archer memakai Pelapis Elemen dari Toko Alkimia (menu Blacksmith).\n" +
                "Api lemah Air, Air lemah Angin, Angin lemah Tanah, Tanah lemah Api, Gelap lemah Cahaya (dan sebaliknya). Boss BERUBAH elemen tiap fase; lihat panel di atas layar.\n" +
                "Status efek: Racun, Terbakar (damage berkelanjutan, tidak membunuh), Beku (lambat), Stun. Penawar menyembuhkan semuanya. Hindar (Dodge) tepat sebelum kena = HINDAR SEMPURNA: waktu melambat, stamina pulih, damage +30% 4 detik.\n" +
                "Misi Harian & Mingguan di Resepsionis Guild. Menu Jeda > Tantangan Dunia: aturan sulit dengan hadiah lebih besar. Boss tanpa goresan = bonus gold dan gelar. Kandang ternak dibeli di Toko Furnitur (telur/susu/wol tiap hari)." },
            new[] { "Main Bersama (Co-op LAN)",
                "Menu Jeda > Main Bersama. Satu pemain menekan BUKA DUNIA KE LAN (HOST); teman di Wi-Fi/LAN yang sama memilih dunianya di daftar atau mengetik alamat IP host.\n" +
                "Tiap pemain memakai karakter dan save sendiri. Pemain saling terlihat dan bisa chat (Enter). Di area hunt (HuntArea), HOST mengatur monster; tamu melawan monster yang sama dan mendapat EXP/drop sendiri.\n" +
                "Dunia tidak berhenti saat menu dibuka selama co-op aktif. Menara, Arena, dan kota dimainkan sendiri-sendiri." },
        };

        class Hint { public string flag, text; public System.Func<GameState, bool> when; }

        static string Scene { get { return SceneManager.GetActiveScene().name; } }
        static bool InHunt { get { return HuntManager.I != null && TowerManager.I == null; } }

        static readonly List<Hint> Hints = new List<Hint>
        {
            new Hint { flag = "tut_welcome", text = "Selamat datang! E = interaksi, Tab = tas, M = peta, Esc = jeda/panduan. Mulai dari Guild: ambil quest di Papan Quest.",
                       when = g => Scene == GuildConfig.TownScene && g.jobChosen },
            new Hint { flag = "tut_village", text = "Desa Pemula: bicara dengan Veteran Brann (E), pembimbingmu sampai Level 5; latihan di boneka (klik kiri / J), lalu ke Hutan Pemula lewat jalan utara. Capai Level 5 untuk membuka jalan ke Ashwatch.",
                       when = g => Scene == StarterConfig.VillageScene && g.level < 5 && !g.jobChosen },
            new Hint { flag = "tut_novicehunt", text = "Hutan Pemula: serang Slime dan Goblin, petik Herb (E). Ctrl = guling, Spasi = lompat, Shift = lari. Pulang lewat pondok di dekat gerbang desa.",
                       when = g => (Scene == StarterConfig.TutorialScene || (Scene == StarterConfig.VillageScene && HuntManager.I != null)) && g.level < 5 && !g.jobChosen },
            new Hint { flag = "tut_quest", text = "Quest diterima! Keluar lewat gerbang sesuai wilayah (S Hutan, T Gurun, B Rawa) dan beli kompas di Gerbang Luar.",
                       when = g => g.activeQuest != null && !g.activeQuest.completed && !InHunt && TowerManager.I == null },
            new Hint { flag = "tut_hunt", text = "Area hunt: cari 3 jejak buruan agar kompas menunjuk targetnya. Pulang lewat Base Camp. Awas hunger/thirst.",
                       when = g => InHunt },
            new Hint { flag = "tut_hunger", text = "Hunger/thirst rendah! Makan roti dari tas (tombol 1) atau minum (F) sebelum lemas.",
                       when = g => g.hunger < 30f || g.thirst < 30f },
            new Hint { flag = "tut_done", text = "Quest selesai! Pulang ke kota dan lapor ke resepsionis Guild untuk menerima hadiah.",
                       when = g => g.activeQuest != null && g.activeQuest.completed },
            new Hint { flag = "tut_eldrath", text = "Kota Eldrath: masuk Menara dari alun-alun. Cabang Guild di sini punya quest menara dan Papan Peringkat.",
                       when = g => Scene == CityGate.DungeonCityScene },
            new Hint { flag = "tut_tower", text = "Menara: habisi semua musuh agar portal naik terbuka. Boss tiap 10 lantai menyimpan checkpoint.",
                       when = g => TowerManager.I != null },
            new Hint { flag = "tut_party", text = "Kamu bisa merekrut partner di meja resepsionis Guild (Poster Rekrutmen); partner langsung ikut dan bertarung bersamamu.",
                       when = g => g.level >= 5 || g.jobChosen },
        };

        void Update()
        {
            var gs = GameState.I; var ui = TownUI.I;
            if (gs == null || ui == null || !SaveSystem.Ready) return;
            poll -= Time.unscaledDeltaTime;
            if (poll > 0f) return;
            poll = 1f;
            if (ui.PanelOpen || Time.timeScale == 0f) return;
            if (Time.unscaledTime - sceneTime < 4f || Time.unscaledTime < nextHint) return;     // beri waktu toast sambutan scene

            foreach (var h in Hints)
            {
                if (gs.flags.Contains(h.flag) || !h.when(gs)) continue;
                gs.flags.Add(h.flag);
                ui.Toast(h.text);
                nextHint = Time.unscaledTime + 12f;
                return;
            }
        }
    }
}
