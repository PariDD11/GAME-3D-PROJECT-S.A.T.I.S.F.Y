# AudioGen — pembuat suara Hunter Guild (Python)

Folder ini ada di luar `Assets/`, jadi Unity mengabaikannya. Isinya skrip yang membuat suara bawaan game (efek dan musik) dari nol, tanpa sampel pihak lain.

- `sfx_gen.py` — 46 efek suara (`sfx_*.wav`, mono 44,1 kHz).
- `music_gen.py` — 19 trek musik loop mulus (`music_*.ogg`, stereo). Perlu `ffmpeg` (libvorbis) di PATH.
- `synth.py`, `inst.py` — mesin sintesis dan instrumen.

Cara pakai: `pip install numpy scipy`, lalu
`python3 sfx_gen.py hasil_sfx` dan `python3 music_gen.py hasil_musik` (boleh `python3 music_gen.py hasil_musik town boss` untuk trek tertentu).
Salin hasilnya ke `Assets/Resources/Audio/` (nama berkas sudah sesuai katalog game). Mengubah `bpm`, `scale`, atau lapisan di fungsi `t_<nama>()` pada `music_gen.py` mengubah lagunya; hasil render selalu sama untuk pengaturan yang sama (seed tetap).

Ini suara sintetis: lebih baik dari suara sementara bawaan game, tetapi bukan rekaman asli. Berkas bernama sama yang kamu taruh di `Assets/Resources/Audio/` (rekaman asli, .wav/.ogg/.mp3) menggantikannya.
