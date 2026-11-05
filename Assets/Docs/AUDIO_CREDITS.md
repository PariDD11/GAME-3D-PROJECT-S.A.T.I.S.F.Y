# Kredit dan lisensi audio

Berkas di `Assets/Resources/Audio/` (terpasang otomatis, nama sudah sesuai `AudioCatalog`).

| Kelompok | Jumlah | Asal | Lisensi |
|---|---|---|---|
| Efek suara `sfx_*` (tempur, monster, quest, dunia, sting, lonceng Katedral, dll.) | 46 | Dibuat sendiri dengan sintesis kode (`Tools/AudioGen/sfx_gen.py`) | Bebas dipakai; tidak memakai sampel pihak lain |
| Musik `music_*` (19 trek loop) | 19 | Komposisi algoritmik buatan sendiri (`Tools/AudioGen/music_gen.py`) | Bebas dipakai; tidak memakai sampel pihak lain |
| `sfx_click`, `sfx_ui_open`, `sfx_ui_close`, `sfx_error` | 4 | Kenney, **Interface Sounds** (`click_003`, `open_002`, `close_002`, `error_008`), via kenney.nl. Diubah: dijadikan mono, disaring (open/close), diatur levelnya | **CC0** (domain publik). Mencantumkan Kenney tidak wajib, tetapi baik |

Catatan:
- Suara buatan sendiri adalah sintetis: lebih baik dari suara sementara bawaan game, bukan rekaman asli. Timpa dengan rekaman asli bernama sama kapan saja; periksa lisensinya dulu (lihat lembar Sumber di `HunterGuild_Daftar_Suara_dan_Backsound.xlsx`).
- Suara NPC (`voice_*`) belum diganti: game tetap memakai "babble" sintetis per peran.
- Kalau sebelumnya kamu pernah memakai menu `Hunter Guild/Audio/2. Ekspor Suara Sementara ke WAV`, hapus WAV sementara bernama sama (mis. `music_town.wav`) agar tidak bentrok dengan `music_town.ogg`; Unity akan memberi peringatan "Audio ganda" di Console bila ada.
