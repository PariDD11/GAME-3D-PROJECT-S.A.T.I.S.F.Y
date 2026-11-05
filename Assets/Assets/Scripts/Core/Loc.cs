using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Pilihan bahasa: Indonesia (asli), English, Basa Jawa. Penerjemah berbasis daftar frasa (glosarium): teks UI Indonesia diganti frasa per frasa
    /// (hanya kata utuh, frasa terpanjang dulu). Dialog NPC panjang dan lore belum diterjemahkan dan tetap Indonesia.
    /// Menambah terjemahan: tambah satu baris "indonesia|english|jawa" di tabel Table.
    /// </summary>
    public static class Loc
    {
        public const int Id = 0, En = 1, Jv = 2;
        public static readonly string[] Names = { "Indonesia", "English", "Basa Jawa" };
        static int lang = -1;
        public static int Lang
        {
            get { if (lang < 0) lang = Mathf.Clamp(PlayerPrefs.GetInt("hg_lang", 0), 0, 2); return lang; }
        }
        public static void Cycle() { lang = (Lang + 1) % 3; PlayerPrefs.SetInt("hg_lang", lang); PlayerPrefs.Save(); cache.Clear(); }
        public static void Set(int l) { lang = Mathf.Clamp(l, 0, 2); PlayerPrefs.SetInt("hg_lang", lang); PlayerPrefs.Save(); cache.Clear(); }

        class Entry { public string id, en, jv; public bool endOnly; }
        static List<Entry> list;
        static readonly Dictionary<string, string> cache = new Dictionary<string, string>();

        static void Init()
        {
            list = new List<Entry>();
            foreach (var line in Table.Split('\n'))
            {
                var p = line.Split('|'); if (p.Length < 3 || p[0].Length == 0) continue;
                string id = p[0].Trim(); bool eo = id.EndsWith("$"); if (eo) id = id.Substring(0, id.Length - 1);
                list.Add(new Entry { id = id, en = p[1].Trim(), jv = p[2].Trim(), endOnly = eo });
            }
            list.Sort((a, b) => b.id.Length.CompareTo(a.id.Length));
        }

        static bool IsLetter(char c) { return char.IsLetterOrDigit(c); }

        /// <summary>Terjemahkan teks tampilan sesuai bahasa terpilih.</summary>
        public static string T(string s)
        {
            if (string.IsNullOrEmpty(s) || Lang == Id) return s;
            string hit; int key = Lang;
            string ck = key + s;
            if (cache.TryGetValue(ck, out hit)) return hit;
            if (list == null) Init();
            var sb = new StringBuilder(s);
            foreach (var e in list)
            {
                string to = key == En ? e.en : e.jv; if (to.Length == 0 || to == e.id) continue;
                int from = 0; string cur = sb.ToString();
                if (cur.IndexOf(e.id, System.StringComparison.Ordinal) < 0) continue;
                if (e.endOnly)
                {
                    if (cur.EndsWith(e.id, System.StringComparison.Ordinal) && (cur.Length == e.id.Length || !IsLetter(cur[cur.Length - e.id.Length - 1]))) { sb.Length = 0; sb.Append(cur, 0, cur.Length - e.id.Length).Append(to); }
                    continue;
                }
                var res = new StringBuilder(); 
                while (true)
                {
                    int i = cur.IndexOf(e.id, from, System.StringComparison.Ordinal);
                    if (i < 0) { res.Append(cur, from, cur.Length - from); break; }
                    bool lb = i == 0 || !IsLetter(cur[i - 1]); int j = i + e.id.Length; bool rb = j >= cur.Length || !IsLetter(cur[j]);
                    res.Append(cur, from, i - from);
                    if (lb && rb) res.Append(to); else res.Append(cur, i, e.id.Length);
                    from = j;
                }
                sb.Length = 0; sb.Append(res);
            }
            string r = sb.ToString();
            if (cache.Count > 6000) cache.Clear();
            cache[ck] = r; return r;
        }

        // indonesia|english|jawa
        const string Table = @"
Game Baru (hapus simpanan lama) >|New Game (erase old save) >|Game Anyar (busak simpenan lawas) >
YAKIN? Ketuk lagi untuk menghapus simpanan lama|SURE? Tap again to erase the old save|YAKIN? Pencet maneh kanggo mbusak simpenan lawas
Belum ada progres tersimpan.|No saved progress yet.|Durung ana progres sing disimpen.
Simpanan terakhir|Last save|Simpenan pungkasan
Game Baru|New Game|Game Anyar
Lanjutkan|Continue|Terusake
Pengaturan|Settings|Pangaturan
PENGATURAN|SETTINGS|PANGATURAN
Keluar dari game|Quit game|Metu saka game
Keluar|Quit|Metu
JEDA|PAUSED|ENTENI
Panduan|Guide|Pandhuan
PANDUAN|GUIDE|PANDHUAN
Simpan sekarang|Save now|Simpen saiki
Progres disimpan.|Progress saved.|Progres wis disimpen.
Gagal menyimpan.|Save failed.|Gagal nyimpen.
Progres dimuat|Progress loaded|Progres wis dimuat
Kembali|Back|Bali
Tutup|Close|Tutup
Lanjut|Next|Terus
Batal|Cancel|Batal
Sampai jumpa.|See you later.|Nganti ketemu maneh.
Terima kasih|Thank you|Matur nuwun
Ngobrol saja.|Just chatting.|Ngobrol wae.
Ada quest?|Any quests?|Ana tugas?
Ada surat?|Any letters?|Ana layang?
Lihat barang dagangan.|Browse wares.|Deleng barang dagangan.
Grafik|Graphics|Grafik
Batas FPS|FPS limit|Watesan FPS
Tampil FPS|Show FPS|Tampilake FPS
Tampilkan FPS|Show FPS|Tampilake FPS
Tanpa batas|Unlimited|Tanpa wates
Resolusi|Resolution|Resolusi
Layar penuh|Fullscreen|Layar kebak
Skala render|Render scale|Skala render
Gaya visual|Visual style|Gaya visual
Cerah ala Genshin|Bright (Genshin style)|Cerah kaya Genshin
Gaya anime|Anime style|Gaya anime
Musik|Music|Musik
Efek suara|Sound effects|Efek swara
Suara NPC|NPC voice|Swara NPC
Efek|Effects|Efek
Bahasa|Language|Basa
ketuk untuk ganti|tap to change|pencet kanggo ngganti
Kontrol (ganti tombol)|Controls (rebind keys)|Kontrol (ganti tombol)
KONTROL GAMEPAD|GAMEPAD CONTROLS|KONTROL GAMEPAD
KONTROL|CONTROLS|KONTROL
Gamepad: ganti tombol|Gamepad: rebind buttons|Gamepad: ganti tombol
Sensitivitas mouse|Mouse sensitivity|Sensitivitas mouse
Sensitivitas gamepad|Gamepad sensitivity|Sensitivitas gamepad
Balik sumbu Y kamera gamepad|Invert gamepad camera Y axis|Walik sumbu Y kamera gamepad
Kembalikan tombol keyboard bawaan|Reset keyboard bindings|Balekake tombol keyboard asli
Kembalikan tombol gamepad bawaan|Reset gamepad bindings|Balekake tombol gamepad asli
Tampilan & Kenyamanan|Display & Comfort|Tampilan & Kepenak
TAMPILAN & KENYAMANAN|DISPLAY & COMFORT|TAMPILAN & KEPENAK
Skala UI|UI scale|Skala UI
Ukuran teks HUD|HUD text size|Ukuran teks HUD
Tema warna|Color theme|Tema warna
Animasi panel|Panel animation|Animasi panel
Kembalikan ke bawaan|Reset to default|Balekake menyang asli
Ya$|Yes|Iya
Tidak$|No|Ora
Maju|Forward|Maju
Mundur|Backward|Mundur
Kiri|Left|Kiwa
Kanan|Right|Tengen
Lompat|Jump|Mlumpat
Menghindar|Dodge|Ngindhari
Menyelam|Dive|Nyilem
Lari|Sprint|Mlayu
Interaksi|Interact|Interaksi
Serang|Attack|Nyerang
Inventori|Inventory|Inventori
Menu skill|Skill menu|Menu skill
Profil|Profile|Profil
Jurnal|Journal|Jurnal
Minum|Drink|Ngombe
Peta|Map|Peta
Jeda|Pause|Ngaso
Tas penuh.|Bag full.|Tas kebak.
Tas penuh|Bag full|Tas kebak
Tas|Bag|Tas
Gold tidak cukup|Not enough gold|Gold ora cukup
Gold|Gold|Gold
Stamina kurang|Not enough stamina|Stamina kurang
Stamina|Stamina|Stamina
Hari|Day|Dina
Musim Semi|Spring|Mangsa Semi
Musim Panas|Summer|Mangsa Panas
Musim Gugur|Autumn|Mangsa Gugur
Musim Dingin|Winter|Mangsa Atis
Pagi|Morning|Esuk
Siang|Noon|Awan
Sore|Evening|Sore
Malam|Night|Wengi
Memancing|Fishing|Mancing
Kail dilempar|Line cast|Kail dibuwang
Ikan menggigit, tarik sekarang!|A fish bites, reel it in now!|Iwak nyaplok, tarik saiki!
Terlalu cepat, ikannya kabur.|Too early, the fish got away.|Kecepetan, iwake mlayu.
Ikannya lolos. Coba lagi.|The fish escaped. Try again.|Iwake uwal. Coba maneh.
Talinya putus, ikan lepas.|The line snapped, the fish got away.|Taline pedhot, iwake uwal.
Menarik|Reeling in|Narik
Dapat|Caught|Oleh
Ikan Kecil|Small Fish|Iwak Cilik
Ikan Mas|Carp|Iwak Mas
Ikan Trout|Trout|Iwak Trout
Ikan Emas|Golden Fish|Iwak Emas
Ikan Oasis|Oasis Fish|Iwak Oasis
Ikan Magma|Magma Fish|Iwak Magma
Lele Rawa|Swamp Catfish|Lele Rawa
Cacing Tanah|Earthworm|Cacing Lemah
Umpan Emas|Golden Lure|Umpan Emas
Umpan|Bait|Umpan
Tanpa umpan|No bait|Tanpa umpan
Dapur: masak sendiri|Kitchen: cook yourself|Pawon: masak dhewe
Dapur Kedai|Tavern Kitchen|Pawon Warung
Api Unggun|Campfire|Geni Unggun
Masak di api unggun|Cook at the campfire|Masak ing geni unggun
Memasak|Cooking|Masak
Gosong!|Burnt!|Gosong!
Sempurna!|Perfect!|Sampurna!
Jadi:|Made:|Dadi:
Masakan Gosong|Burnt Food|Panganan Gosong
Ikan Bakar|Grilled Fish|Iwak Bakar
Sup Ikan|Fish Soup|Sup Iwak
Steak Ikan Trout|Trout Steak|Steak Iwak Trout
Sup Jamur|Mushroom Soup|Sup Jamur
Ayam Panggang|Roast Chicken|Pitik Panggang
Daging Sapi Panggang|Roast Beef|Daging Sapi Panggang
Domba Panggang|Roast Mutton|Wedhus Panggang
Pai Berry|Berry Pie|Pai Berry
Pesta Emas|Golden Feast|Pesta Emas
Roti Gandum|Wheat Bread|Roti Gandum
Rebusan Sayur|Vegetable Stew|Rebusan Sayur
Sup Labu|Pumpkin Soup|Sup Waluh
Bubur Daun Bawang Salju|Frost Leek Porridge|Bubur Godhong Bawang Salju
Bubur Daun Bawang|Leek Porridge|Bubur Godhong Bawang
Makan di tempat|Eat here|Mangan ing kene
Minum di tempat|Drink here|Ngombe ing kene
Menu Makanan|Food Menu|Menu Panganan
Menu Minuman|Drink Menu|Menu Ombenan
Kedai Makan|Tavern|Warung Mangan
Efek makanan habis.|The food effect wore off.|Efek panganan wis entek.
Buff makan|Meal buff|Buff mangan
Menambang|Mining|Nambang
Bijih Besi|Iron Ore|Bijih Wesi
Rubi|Ruby|Rubi
Safir|Sapphire|Safir
Zamrud|Emerald|Jamrud
Topas|Topaz|Topas
Lebur bijih|Smelt ore|Lebur bijih
Menanam|Planting|Nandur
Tanam|Plant|Nandur
Panen|Harvest|Panen
Siram|Water|Nyirami
Lahan kosong: tanam benih|Empty plot: plant a seed|Lahan kosong: nandur winih
Lahan|Plot|Lahan
Benih|Seed|Winih
Gandum|Wheat|Gandum
Wortel|Carrot|Wortel
Tomat|Tomato|Tomat
Labu|Pumpkin|Waluh
Daun Bawang Salju|Frost Leek|Godhong Bawang Salju
tumbuh|growing|thukul
Socket Permata|Gem Sockets|Socket Permata
Socket permata|Gem sockets|Socket permata
Pasang|Socket in|Pasang
lepas|remove|copot
kosong|empty|kosong
Kasus Investigasi|Investigation Cases|Kasus Panyelidikan
Terima kasus ini|Accept this case|Trima kasus iki
Batalkan kasus|Abandon case|Batalake kasus
Tuduh pelaku|Accuse the culprit|Tuduh pelaku
Siapa pelakunya?|Who is the culprit?|Sapa pelakune?
Periksa petunjuk|Examine clue|Priksa pitunjuk
Petunjuk|Clue|Pitunjuk
Kesaksian|Testimony|Paseksen
Saksi|Witness|Seksi
Tersangka|Suspect|Wong sing dicurigai
Catatan petunjuk|Clue notes|Cathetan pitunjuk
Benar!|Correct!|Bener!
Salah tuduh!|Wrong accusation!|Salah nuduh!
Selesai|Done|Rampung
Baru|New|Anyar
Tanya soal kasus|Ask about the case|Takon bab kasus
Serahkan surat dari|Hand over the letter from|Serahake layang saka
Ada surat yang perlu dikirim?|Do you have a letter to send?|Ana layang sing kudu dikirim?
Baca surat yang kubawa|Read the letters I carry|Waca layang sing dak gawa
Surat yang kubawa|Letters I carry|Layang sing dak gawa
Surat|Letter|Layang
Antar surat ke|Deliver a letter to|Ater layang menyang
Papan Pengumuman|Notice Board|Papan Pengumuman
Blacksmith|Blacksmith|Pandhe Wesi
Toko Umum|General Store|Toko Umum
Ramuan Penyembuh|Healing Potion|Ramuan Penyembuh
Ramuan Mana|Mana Potion|Ramuan Mana
Beli bahan dasar|Buy basic materials|Tuku bahan dhasar
Beli|Buy|Tuku
Jual|Sell|Dodol
Repair semua gear|Repair all gear|Ndandani kabeh gear
Craft gear|Craft gear|Nggawe gear
Penginapan|Inn|Penginapan
Sewa kamar.|Rent a room.|Nyewa kamar.
Istirahat di tenda|Rest in the tent|Ngaso ing tarub
Isi botol minum (gratis)|Refill canteen (free)|Isi botol ngombe (gratis)
Botol Minum|Canteen|Botol Ngombe
Pulang ke kota|Return to town|Mulih menyang kutha
Kota|Town|Kutha
Desa Pemula|Starter Village|Desa Wiwitan
Hutan Lerai|Lerai Forest|Alas Lerai
Gurun Zahar|Zahar Desert|Ara-ara Zahar
Rawa Terkutuk|Cursed Swamp|Rawa Kasepatan
Menara|Tower|Menara
Rank|Rank|Pangkat
Quest selesai|Quest complete|Tugas rampung
QUEST SELESAI!|QUEST COMPLETE!|TUGAS RAMPUNG!
Progres quest|Quest progress|Progres tugas
Quest|Quest|Tugas
Misi|Mission|Misi
Level|Level|Level
Kamu beristirahat.|You rest.|Kowe ngaso.
HP dan stamina pulih.|HP and stamina restored.|HP lan stamina wis pulih.
Naik kuda|Mounted|Numpak jaran
Turun dari kuda.|Dismounted.|Mudhun saka jaran.
Memanjat!|Climbing!|Manjat!
Stamina habis, kamu terlepas!|Out of stamina, you fall!|Stamina entek, kowe tiba!
Kehabisan nafas! Naiklah ke permukaan|Out of breath! Swim to the surface|Entek napase! Munggaha menyang permukaan
Selamat datang, Hunter!|Welcome, Hunter!|Sugeng rawuh, Hunter!
Selamat datang!|Welcome!|Sugeng rawuh!
Silakan|Please|Mangga
Tidak bisa memanggil kuda saat bertarung.|You cannot call your horse during combat.|Ora bisa nundang jaran nalika perang.
Tas penuh, hasil masakan hilang.|Bag full, the dish was lost.|Tas kebak, masakane ilang.
Tas penuh, ikan dilepas.|Bag full, the fish was released.|Tas kebak, iwake dibuwang.
Stamina kurang untuk menambang (butuh 15).|Not enough stamina to mine (needs 15).|Stamina kurang kanggo nambang (butuh 15).
Mulai menambang: 3 pukulan, tekan tepat di zona.|Mining starts: 3 strikes, press inside the zone.|Wiwit nambang: 3 pukulan, pencet pas ing zona.
Disiram. Tumbuh lebih cepat.|Watered. It grows faster.|Wis disirami. Thukul luwih cepet.
Salah tuduh! Denda 30g. Periksa lagi petunjuknya.|Wrong accusation! 30g fine. Re-check the clues.|Salah nuduh! Dendha 30g. Priksa maneh pitunjuke.
Kasus diterima|Case accepted|Kasus ditampa
Menanam|Planting|Nandur
terpasang.|socketed.|wis dipasang.
Tekan di zona hijau (emas = sempurna)|Press in the green zone (gold = perfect)|Pencet ing zona ijo (emas = sampurna)
Tahan Spasi / klik: naikkan zona. Jaga ikan (oranye) di zona hijau.|Hold Space / click to raise the zone. Keep the fish (orange) in the green zone.|Tahan Spasi / klik: munggahake zona. Jaga iwak (oranye) ing zona ijo.
Pilih resep.|Pick a recipe.|Pilih resep.
Bahan dari memancing, berburu, dan gathering.|Ingredients come from fishing, hunting and gathering.|Bahan saka mancing, mburu, lan ngumpulake.
Mini-game: tekan di zona hijau.|Mini-game: press in the green zone.|Mini-game: pencet ing zona ijo.
Emas = 2 porsi, meleset = gosong.|Gold = 2 portions, miss = burnt.|Emas = 2 porsi, luput = gosong.
Mewawancarai semua saksi = bonus hadiah 25%.|Interview all witnesses = 25% reward bonus.|Takon kabeh seksi = bonus hadiah 25%.
Slot save|Save slot|Slot simpenan
Pilih slot save|Choose save slot|Pilih slot simpenan
Slot kosong|Empty slot|Slot kosong
(slot ini dipakai)|(current slot)|(slot iki dipakake)
";
    }
}
