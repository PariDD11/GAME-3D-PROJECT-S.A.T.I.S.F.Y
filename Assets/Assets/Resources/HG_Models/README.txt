MODEL ASLI (opsional) UNTUK PEMAIN
Taruh prefab bernama "Player" di folder ini (Assets/Resources/HG_Models/Player.prefab) untuk mengganti model prosedural.
- Rig harus Humanoid (VRM/VRoid, Mixamo, Synty, Quaternius, dll).
- Opsional: Animator Controller bernama "PlayerController" dengan parameter
  float Speed, bool Drawn, bool Bow, bool Dead, trigger Slash0 Slash1 Slash2 Shoot Roll RollBack Hurt Faint Drink.
- Senjata (pedang/perisai/busur dari item) otomatis menempel ke tulang tangan/pinggang/dada.
- Kustomisasi rambut/baju/armor model asli dilakukan lewat pergantian prefab/material; layar G tetap mengatur nama & data.
Hapus prefab ini untuk kembali ke karakter prosedural.

MODEL ASLI (opsional) UNTUK MONSTER
Pakai menu Hunter Guild > Ganti Monster (data: Assets/Resources/MonsterSwapTable.asset), bukan menaruh prefab di folder ini.
- Seret FBX/prefab ke aturan: per id monster (mis. alpha_wolf), per bentuk dasar (mis. wolf = semua serigala), atau semua boss / bukan boss.
- Model otomatis dicocokkan ke tinggi model bawaan monster itu (atur "Skala tinggi", "Naik", "Putar" bila perlu). Collider, rigidbody, dan skrip bawaan prefab dimatikan.
- Opsional: Animator Controller dengan parameter float Speed, bool Telegraph, trigger Attack, trigger Hurt, bool Dead (atau trigger Die). Parameter yang tidak ada dilewati.
  Atau isi klip (Idle/Jalan/Serang/Terkena/Mati) lalu klik "Buat Controller dari klip ini". Tanpa Animator, model digerakkan secara prosedural.
Hapus / non-aktifkan aturan untuk kembali ke model bawaan (dari kode).
