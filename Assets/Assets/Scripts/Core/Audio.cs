using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// Daftar semua suara yang dipakai game (satu-satunya sumber nama). Letakkan klip asli di Assets/Resources/Audio/ dengan nama persis:
    ///   sfx_{nama}, music_{nama}, voice_{nama} (voice boleh punya varian voice_{nama}_1 .. _3). Format wav/ogg/mp3.
    /// Tanpa file, game memakai suara sintetis sementara (bunyi sederhana). Menu Hunter Guild > Audio membantu menyiapkan & memeriksa.
    /// </summary>
    public static class AudioCatalog
    {
        public class Entry { public string kind, name, desc; public Entry(string k, string n, string d) { kind = k; name = n; desc = d; } public string File { get { return kind + "_" + name; } } }

        public static readonly List<Entry> All = new List<Entry>
        {
            // ---- Combat
            new Entry("sfx","swing","Ayunan pedang kombo 1 (pendek, cepat)"), new Entry("sfx","swing2","Ayunan pedang kombo 2"), new Entry("sfx","heavy_swing","Ayunan kombo 3 / serangan berat"),
            new Entry("sfx","hit","Pukulan kena musuh (ringan)"), new Entry("sfx","hit_heavy","Pukulan kena musuh (berat/kritis)"),
            new Entry("sfx","bow_shot","Tembakan busur"), new Entry("sfx","arrow_hit","Anak panah kena musuh"),
            new Entry("sfx","block","Tangkisan dengan perisai/pedang"), new Entry("sfx","dodge","Guling / menghindar (whoosh)"),
            new Entry("sfx","hurt","Pemain terkena serangan"), new Entry("sfx","faint","Pemain pingsan"),
            new Entry("sfx","skill_cast","Memakai skill"), new Entry("sfx","heal","Pemulihan / berkat"), new Entry("sfx","potion","Minum ramuan"),
            new Entry("sfx","monster_growl","Monster mulai menyerang (geraman)"), new Entry("sfx","monster_attack","Serangan monster mengenai"),
            new Entry("sfx","monster_hurt","Monster kesakitan"), new Entry("sfx","monster_die","Monster mati"), new Entry("sfx","boss_roar","Boss meraung / mati"),
            new Entry("sfx","victory","Sting kemenangan setelah boss kalah"), new Entry("sfx","defeat","Sting kekalahan"),
            // ---- Quest & progres
            new Entry("sfx","quest_accept","Quest diterima di Guild"), new Entry("sfx","quest_progress","Progres quest bertambah"), new Entry("sfx","quest_done","Quest selesai di lapangan"),
            new Entry("sfx","quest_reward","Laporan quest & hadiah diterima"), new Entry("sfx","quest_fail","Quest gagal"), new Entry("sfx","rankup","Naik rank (fanfare)"),
            new Entry("sfx","levelup","Naik level"), new Entry("sfx","track_found","Jejak buruan ditemukan"),
            // ---- Dunia & UI
            new Entry("sfx","pickup","Ambil item"), new Entry("sfx","gather","Mengumpulkan bahan"), new Entry("sfx","coin","Gold dibelanjakan"),
            new Entry("sfx","click","Klik tombol UI"), new Entry("sfx","ui_open","Buka panel"), new Entry("sfx","ui_close","Tutup panel"), new Entry("sfx","error","Aksi ditolak"),
            new Entry("sfx","paper","Papan quest / pengumuman"), new Entry("sfx","gate","Gerbang terbuka"), new Entry("sfx","door","Pintu bangunan"),
            new Entry("sfx","tower_portal","Portal Menara aktif"), new Entry("sfx","tower_floor_clear","Lantai Menara selesai"), new Entry("sfx","chest_open","Peti Menara dibuka"),
            // ---- v35z102n32: mekanik tempur & hadiah baru
            new Entry("sfx","perfect_dodge","Hindar Sempurna (whoosh bening)"), new Entry("sfx","break","Musuh kena Break (retak berat)"),
            new Entry("sfx","status_hit","Terkena status efek (racun/bakar/beku/stun)"), new Entry("sfx","claim","Klaim hadiah misi harian/mingguan"),
            // ---- n89: lonceng Katedral Ashwatch (CathedralBell: enam dentang tiap fajar 06.00 dan senja 18.00)
            new Entry("sfx","bell_toll","Lonceng Katedral (satu dentang; dibunyikan enam kali)"),
            // ---- Musik
            new Entry("music","town","Kota (siang)"), new Entry("music","town_night","Kota (malam)"), new Entry("music","guild","Guild & interior bangunan"), new Entry("music","eldrath","Kota Eldrath"),
            new Entry("music","forest","Hutan Lerai"), new Entry("music","desert","Gurun Zahar"), new Entry("music","swamp","Rawa Terkutuk"), new Entry("music","tower","Menara Eldrath"),
            new Entry("music","volcano","Gunung Vulkanik"), new Entry("music","bone","Padang Tulang"), new Entry("music","demon","Lembah Iblis"),
            new Entry("music","combat","Pertempuran biasa (menggantikan musik area sementara)"), new Entry("music","boss","Pertarungan boss"),
            new Entry("music","boss_ogre_chief","Boss Grumak"), new Entry("music","boss_behemoth","Boss Behemoth"), new Entry("music","boss_cerberus","Boss Cerberus"), new Entry("music","boss_ignarok","Boss Ignarok"), new Entry("music","boss_bone_king","Boss Malrek"), new Entry("music","boss_demon_lord","Boss Zargoth"),
            // ---- Suara NPC penting (dipakai saat diajak bicara / urusan quest)
            new Entry("voice","receptionist","Resepsionis Guild (quest, laporan)"), new Entry("voice","guildmaster","Guild Master"), new Entry("voice","trainer","Pelatih Guild"),
            new Entry("voice","referee","Pengawas Ujian rank"), new Entry("voice","appraiser","Penilai bahan"), new Entry("voice","smith","Pandai Besi"), new Entry("voice","innkeeper","Pemilik Penginapan"),
            new Entry("voice","merchant","Pedagang Toko Umum"), new Entry("voice","librarian","Pustakawan"), new Entry("voice","cook","Koki Restoran"), new Entry("voice","priest","Pendeta Katedral"),
            new Entry("voice","guard","Penjaga & Ksatria gerbang"), new Entry("voice","bard","Bard"), new Entry("voice","citizen_m","Warga pria"), new Entry("voice","citizen_f","Warga wanita"),
        };
    }

    public class AudioManager : MonoBehaviour
    {
        const int Rate = 22050;
        static AudioManager inst;
        readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        AudioSource musicA, musicB, voiceSrc;
        readonly AudioSource[] pool = new AudioSource[10];
        int poolIdx;
        string curMusic = "", sceneKey = "town";
        bool aActive = true;
        float fade, nextDirector, combatUntil;
        Coroutine babble;

        public float musicVol = 0.5f, sfxVol = 0.8f, voiceVol = 0.7f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (inst != null) return;
            var go = new GameObject("AudioManager");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<AudioManager>();
            inst.Init();
            SceneManager.sceneLoaded += (s, m) => inst.OnScene(s.name);
            inst.OnScene(SceneManager.GetActiveScene().name);
        }

        public static AudioManager I { get { return inst; } }

        void Init()
        {
            musicVol = PlayerPrefs.GetFloat("HG_VolMusic", 0.5f);
            sfxVol = PlayerPrefs.GetFloat("HG_VolSfx", 0.8f);
            voiceVol = PlayerPrefs.GetFloat("HG_VolVoice", 0.7f);
            musicA = NewSrc(true); musicB = NewSrc(true); voiceSrc = NewSrc(false);
            for (int i = 0; i < pool.Length; i++) pool[i] = NewSrc(false);
        }

        AudioSource NewSrc(bool loop)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false; s.loop = loop; s.spatialBlend = 0f;
            return s;
        }

        void OnScene(string scene)
        {
            sceneKey = scene == GuildConfig.TownScene ? "town" : (scene == GuildConfig.InteriorScene || InteriorInfo.IsInterior(scene)) ? "guild" :
                       scene == CityGate.DungeonCityScene ? "eldrath" : scene == "HuntArea_Desert" ? "desert" :
                       scene == "HuntArea_Swamp" ? "swamp" : scene == "DungeonTower" ? "tower" :
                       scene == "ArenaExam" ? "boss" : scene == "HuntArea_Volcano" ? "volcano" : scene == "HuntArea_Bone" ? "bone" : scene == "HuntArea_Demon" ? "demon" : "forest";
            combatUntil = 0f;
            nextDirector = 0f;
            PlayMusic(sceneKey);
        }

        void Update()
        {
            var on = aActive ? musicA : musicB; var off = aActive ? musicB : musicA;
            fade = Mathf.MoveTowards(fade, 1f, Time.unscaledDeltaTime / 1.5f);
            on.volume = musicVol * fade;
            off.volume = Mathf.Max(0f, off.volume - Time.unscaledDeltaTime / 1.5f * musicVol);
            if (off.isPlaying && off.volume <= 0.001f) off.Stop();
            if (Time.unscaledTime >= nextDirector) { nextDirector = Time.unscaledTime + 0.5f; Direct(); }
        }

        /// <summary>Memilih musik: boss > pertempuran > malam > musik area.</summary>
        void Direct()
        {
            string key = sceneKey;
            var gs = GameState.I; var pl = GameObject.FindGameObjectWithTag("Player");
            if (HuntManager.I != null && pl != null)
            {
                bool boss = false, fight = false; string bossKey = "boss"; float bestD = 999f;
                foreach (var m in Monster.All)
                {
                    if (m == null || m.IsDead) continue;
                    float d = (m.transform.position - pl.transform.position).magnitude;
                    if (m.isBoss && d < 70f) { boss = true; if (d < bestD) { bestD = d; bossKey = BossMusicKey(m.DefId); } }
                    if (m.InCombat && d < 30f) fight = true;
                }
                if (fight) combatUntil = Time.unscaledTime + 6f;
                if (boss) key = bossKey;
                else if (Time.unscaledTime < combatUntil) key = "combat";
            }
            else if (sceneKey == "town" && gs != null && (gs.timeOfDay >= 19f || gs.timeOfDay < 5f)) key = "town_night";
            PlayMusic(key);
        }

        static string BossMusicKey(string defId)
        {
            switch (defId) { case "behemoth": case "cerberus":
                case "ogre_chief": case "ignarok": case "bone_king": case "demon_lord": return "boss_" + defId; }
            return "boss";
        }

        public void SetVolumes(float music, float sfx, float voice = -1f)
        {
            musicVol = Mathf.Clamp01(music); sfxVol = Mathf.Clamp01(sfx);
            if (voice >= 0f) voiceVol = Mathf.Clamp01(voice);
            PlayerPrefs.SetFloat("HG_VolMusic", musicVol); PlayerPrefs.SetFloat("HG_VolSfx", sfxVol); PlayerPrefs.SetFloat("HG_VolVoice", voiceVol); PlayerPrefs.Save();
        }

        public void PlayMusic(string key)
        {
            if (key == curMusic) return;
            curMusic = key;
            var clip = Get("music_" + key);
            aActive = !aActive;
            var src = aActive ? musicA : musicB;
            src.clip = clip; src.volume = 0f; fade = 0f;
            if (clip != null) src.Play();
        }

        /// <summary>Klip efek (sfx_{name}) dari Resources/Audio, atau bunyi sintetis bila berkasnya tidak ada. Untuk pemancar khusus yang punya AudioSource sendiri (mis. CathedralBell).</summary>
        public AudioClip Clip(string name) { return Get("sfx_" + name); }

        public void PlaySfx(string name, Vector3? pos)
        {
            var clip = Get("sfx_" + name);
            if (clip == null) return;
            var src = pool[poolIdx++ % pool.Length];
            src.transform.position = pos.HasValue ? pos.Value : Vector3.zero;
            src.spatialBlend = pos.HasValue ? 0.8f : 0f;
            src.minDistance = 4f; src.maxDistance = 45f;
            src.pitch = Random.Range(0.94f, 1.06f);
            src.PlayOneShot(clip, sfxVol);
        }

        // ---------------- Suara NPC ----------------
        public void PlayVoice(string key, string text)
        {
            if (babble != null) StopCoroutine(babble);
            voiceSrc.Stop();
            // klip asli (voice_key, voice_key_1..3)?
            var real = RealVoice(key);
            if (real != null) { voiceSrc.pitch = Random.Range(0.97f, 1.03f); voiceSrc.PlayOneShot(real, voiceVol); return; }
            babble = StartCoroutine(Babble(key, text ?? ""));
        }

        AudioClip RealVoice(string key)
        {
            var list = new List<AudioClip>();
            foreach (var suffix in new[] { "", "_1", "_2", "_3" })
            {
                var c = Resources.Load<AudioClip>("Audio/voice_" + key + suffix);
                if (c != null) list.Add(c);
            }
            return list.Count == 0 ? null : list[Random.Range(0, list.Count)];
        }

        IEnumerator Babble(string key, string text)
        {
            var v = NpcVoice.Def(key);
            int count = Mathf.Clamp(text.Length / 3, 4, 14);
            if (text.Length == 0) count = Random.Range(3, 6);
            bool ask = text.EndsWith("?"), shout = text.EndsWith("!");
            for (int i = 0; i < count; i++)
            {
                int step = NpcVoice.Steps[(i * 7 + (text.Length > i ? text[i] : i)) % NpcVoice.Steps.Length];
                if (i == count - 1) step = ask ? 5 : -3;
                float f = v.pitch * Mathf.Pow(2f, step / 12f);
                voiceSrc.pitch = 1f;
                voiceSrc.PlayOneShot(NpcVoice.Blip(v, f, shout ? 1.2f : 1f), voiceVol * (shout ? 1f : 0.85f));
                yield return new WaitForSecondsRealtime(v.tempo * Random.Range(0.9f, 1.15f));
            }
            babble = null;
        }

        // ---------------- Klip ----------------
        AudioClip Get(string full)
        {
            AudioClip c;
            if (cache.TryGetValue(full, out c)) return c;
            c = Resources.Load<AudioClip>("Audio/" + full);
            if (c == null) c = Synth(full);
            cache[full] = c;
            return c;
        }

        /// <summary>Klip sintetis untuk nama lengkap (sfx_xxx / music_xxx / voice_xxx).</summary>
        public static AudioClip Synth(string full)
        {
            if (full.StartsWith("music_")) return Make(full, MusicSamples(full.Substring(6)));
            if (full.StartsWith("voice_")) return Make(full, NpcVoice.Sample(full.Substring(6)));
            return Make(full, SfxSamples(full.Substring(4)));
        }

        public static float[] SamplesFor(string full)
        {
            if (full.StartsWith("music_")) return MusicSamples(full.Substring(6));
            if (full.StartsWith("voice_")) return NpcVoice.Sample(full.Substring(6));
            return SfxSamples(full.Substring(4));
        }

        public const int SampleRate = Rate;

        static AudioClip Make(string name, float[] d)
        {
            var c = AudioClip.Create(name, d.Length, 1, Rate, false);
            c.SetData(d, 0);
            return c;
        }

        public static float[] Buf(float sec) { return new float[Mathf.CeilToInt(sec * Rate)]; }

        public static void Tone(float[] b, float t0, float dur, float f0, float f1, float amp, int wave = 0)
        {
            int s = Mathf.Max(0, (int)(t0 * Rate)), n = (int)(dur * Rate);
            double ph = 0;
            for (int i = 0; i < n && s + i < b.Length; i++)
            {
                float u = (float)i / n;
                float f = Mathf.Lerp(f0, f1, u);
                ph += 2 * System.Math.PI * f / Rate;
                float v = wave == 0 ? (float)System.Math.Sin(ph) : wave == 1 ? (float)(System.Math.Sin(ph) > 0 ? 1 : -1) * 0.5f : (float)((ph / System.Math.PI) % 2 - 1);
                float env = Mathf.Min(1f, i / (0.004f * Rate)) * Mathf.Pow(1f - u, 2f);
                b[s + i] += v * env * amp;
            }
        }

        public static void Noise(float[] b, float t0, float dur, float amp, float lp0, float lp1, System.Random r)
        {
            int s = Mathf.Max(0, (int)(t0 * Rate)), n = (int)(dur * Rate);
            float y = 0;
            for (int i = 0; i < n && s + i < b.Length; i++)
            {
                float u = (float)i / n;
                float a = Mathf.Lerp(lp0, lp1, u);
                y += a * ((float)r.NextDouble() * 2f - 1f - y);
                b[s + i] += y * Mathf.Pow(1f - u, 1.5f) * amp;
            }
        }

        static void Arp(float[] b, float[] notes, float gap, float len, float amp, int wave = 0)
        {
            for (int i = 0; i < notes.Length; i++) Tone(b, i * gap, len, notes[i], notes[i], amp, wave);
        }

        static float[] SfxSamples(string n)
        {
            var r = new System.Random(n.GetHashCode());
            float[] b;
            switch (n)
            {
                case "swing": b = Buf(0.2f); Noise(b, 0, 0.2f, 0.7f, 0.05f, 0.5f, r); break;
                case "swing2": b = Buf(0.2f); Noise(b, 0, 0.2f, 0.7f, 0.08f, 0.6f, r); Tone(b, 0, 0.1f, 500, 700, 0.1f); break;
                case "heavy_swing": b = Buf(0.35f); Noise(b, 0, 0.32f, 0.8f, 0.03f, 0.4f, r); Tone(b, 0, 0.3f, 90, 50, 0.4f); break;
                case "bell_toll": b = Buf(3.4f); Tone(b, 0, 3.2f, 140, 140, 0.5f); Tone(b, 0, 2.6f, 167, 167, 0.3f); Tone(b, 0, 2.0f, 280, 280, 0.22f); Tone(b, 0, 0.05f, 900, 300, 0.3f); break;
                case "thunder": b = Buf(2.6f); Noise(b, 0, 0.5f, 0.9f, 0.5f, 0.2f, r); Noise(b, 0.2f, 2.2f, 0.8f, 0.12f, 0.03f, r); Tone(b, 0, 2.0f, 70, 26, 0.8f); Tone(b, 0.35f, 1.6f, 48, 22, 0.6f); break;
                case "hit": b = Buf(0.2f); Noise(b, 0, 0.12f, 0.8f, 0.5f, 0.15f, r); Tone(b, 0, 0.18f, 150, 70, 0.7f); break;
                case "hit_heavy": b = Buf(0.35f); Noise(b, 0, 0.2f, 0.9f, 0.6f, 0.1f, r); Tone(b, 0, 0.3f, 120, 40, 0.9f); Tone(b, 0.05f, 0.2f, 70, 35, 0.6f); break;
                case "bow_shot": b = Buf(0.3f); Tone(b, 0, 0.25f, 420, 180, 0.35f, 1); Noise(b, 0, 0.1f, 0.4f, 0.6f, 0.2f, r); break;
                case "arrow_hit": b = Buf(0.15f); Noise(b, 0, 0.06f, 0.7f, 0.5f, 0.2f, r); Tone(b, 0, 0.12f, 320, 140, 0.5f); break;
                case "block": b = Buf(0.3f); Tone(b, 0, 0.25f, 900, 880, 0.25f, 1); Tone(b, 0, 0.25f, 1350, 1320, 0.2f, 1); Noise(b, 0, 0.05f, 0.5f, 0.7f, 0.3f, r); break;
                case "dodge": b = Buf(0.3f); Noise(b, 0, 0.28f, 0.45f, 0.03f, 0.35f, r); break;
                case "hurt": b = Buf(0.3f); Tone(b, 0, 0.28f, 260, 110, 0.5f, 1); Noise(b, 0, 0.15f, 0.4f, 0.4f, 0.1f, r); break;
                case "faint": b = Buf(0.9f); Tone(b, 0, 0.9f, 300, 60, 0.6f); break;
                case "skill_cast": b = Buf(0.6f); Tone(b, 0, 0.5f, 400, 1200, 0.3f); Tone(b, 0.1f, 0.4f, 800, 1800, 0.15f); break;
                case "heal": b = Buf(0.8f); Arp(b, new[] { 660f, 880f, 1100f, 1320f }, 0.12f, 0.35f, 0.28f); break;
                case "potion": b = Buf(0.5f); for (int i = 0; i < 4; i++) Tone(b, i * 0.1f, 0.12f, 400 + i * 120, 600 + i * 120, 0.3f); break;
                case "monster_growl": b = Buf(0.7f); Tone(b, 0, 0.65f, 100, 60, 0.45f, 2); Noise(b, 0, 0.6f, 0.25f, 0.1f, 0.03f, r); break;
                case "monster_attack": b = Buf(0.25f); Noise(b, 0, 0.12f, 0.6f, 0.4f, 0.1f, r); Tone(b, 0, 0.2f, 200, 90, 0.5f, 1); break;
                case "monster_hurt": b = Buf(0.3f); Tone(b, 0, 0.25f, 330, 170, 0.4f, 2); Noise(b, 0, 0.08f, 0.3f, 0.4f, 0.1f, r); break;
                case "monster_die": b = Buf(0.5f); Noise(b, 0, 0.35f, 0.6f, 0.3f, 0.05f, r); Tone(b, 0, 0.45f, 220, 50, 0.5f, 2); break;
                case "boss_roar": b = Buf(1.4f); Tone(b, 0, 1.4f, 90, 55, 0.7f, 2); Noise(b, 0, 1.2f, 0.5f, 0.15f, 0.03f, r); break;
                case "victory": b = Buf(2.0f); Arp(b, new[] { 261.6f, 329.6f, 392f, 523.3f }, 0.2f, 0.9f, 0.3f, 2); Tone(b, 0.8f, 1.1f, 523.3f, 523.3f, 0.3f, 2); Tone(b, 0.8f, 1.1f, 659.3f, 659.3f, 0.25f, 2); break;
                case "defeat": b = Buf(1.4f); Arp(b, new[] { 392f, 330f, 262f, 196f }, 0.28f, 0.6f, 0.3f, 1); break;
                case "quest_accept": b = Buf(0.6f); Tone(b, 0, 0.25f, 440, 440, 0.35f); Tone(b, 0.2f, 0.35f, 660, 660, 0.4f); break;
                case "quest_progress": b = Buf(0.3f); Tone(b, 0, 0.1f, 880, 880, 0.3f); Tone(b, 0.09f, 0.14f, 1100, 1100, 0.3f); break;
                case "quest": case "quest_done": b = Buf(0.9f); Tone(b, 0, 0.25f, 523, 523, 0.4f); Tone(b, 0.18f, 0.25f, 659, 659, 0.4f); Tone(b, 0.36f, 0.25f, 784, 784, 0.4f); Tone(b, 0.54f, 0.35f, 1046, 1046, 0.45f); break;
                case "quest_reward": b = Buf(1.2f); Arp(b, new[] { 1568f, 2093f, 1568f, 2093f, 2637f }, 0.08f, 0.2f, 0.25f, 1); Tone(b, 0.45f, 0.7f, 523f, 523f, 0.3f); Tone(b, 0.45f, 0.7f, 784f, 784f, 0.25f); break;
                case "quest_fail": b = Buf(1.0f); Arp(b, new[] { 400f, 340f, 280f, 200f }, 0.22f, 0.4f, 0.3f); break;
                case "rankup": b = Buf(2.2f); Arp(b, new[] { 392f, 494f, 587f, 784f, 988f, 1175f }, 0.17f, 0.9f, 0.28f, 2); Tone(b, 1.0f, 1.2f, 784f, 784f, 0.3f, 2); Tone(b, 1.0f, 1.2f, 1175f, 1175f, 0.25f); break;
                case "levelup": b = Buf(1.3f); Arp(b, new[] { 523f, 659f, 784f, 1046f, 1318f }, 0.15f, 0.5f, 0.35f); break;
                case "track_found": b = Buf(0.4f); Tone(b, 0, 0.1f, 600, 600, 0.35f); Tone(b, 0.12f, 0.2f, 900, 900, 0.35f); break;
                case "pickup": b = Buf(0.25f); Tone(b, 0, 0.1f, 880, 880, 0.4f); Tone(b, 0.08f, 0.15f, 1320, 1320, 0.4f); break;
                case "gather": b = Buf(0.3f); Noise(b, 0, 0.12f, 0.5f, 0.3f, 0.1f, r); Tone(b, 0.08f, 0.15f, 500, 700, 0.3f); break;
                case "coin": b = Buf(0.25f); Tone(b, 0, 0.06f, 1568, 1568, 0.35f, 1); Tone(b, 0.06f, 0.18f, 2093, 2093, 0.35f, 1); break;
                case "click": b = Buf(0.06f); Tone(b, 0, 0.05f, 1200, 900, 0.4f); break;
                case "ui_open": b = Buf(0.15f); Tone(b, 0, 0.12f, 500, 800, 0.3f); break;
                case "ui_close": b = Buf(0.15f); Tone(b, 0, 0.12f, 800, 500, 0.3f); break;
                case "error": b = Buf(0.25f); Tone(b, 0, 0.22f, 160, 130, 0.45f, 1); break;
                case "paper": b = Buf(0.25f); Noise(b, 0, 0.22f, 0.5f, 0.35f, 0.15f, r); break;
                case "gate": b = Buf(1.2f); Noise(b, 0, 1.2f, 0.8f, 0.03f, 0.02f, r); Tone(b, 0, 1.2f, 55, 45, 0.5f, 2); break;
                case "door": b = Buf(0.5f); Tone(b, 0, 0.3f, 110, 60, 0.6f); Noise(b, 0, 0.2f, 0.4f, 0.2f, 0.05f, r); break;
                case "tower_portal": b = Buf(1.2f); Noise(b, 0, 1.1f, 0.3f, 0.02f, 0.4f, r); Tone(b, 0, 1.1f, 200, 900, 0.3f); break;
                case "tower_floor_clear": b = Buf(0.9f); Arp(b, new[] { 392f, 523f, 659f }, 0.2f, 0.5f, 0.35f); break;
                case "chest_open": b = Buf(1.1f); Tone(b, 0, 0.3f, 150, 300, 0.3f, 2); Arp(b, new[] { 988f, 1318f, 1568f, 1976f }, 0.1f, 0.35f, 0.25f); break;
                case "perfect_dodge": b = Buf(0.5f); Noise(b, 0, 0.3f, 0.35f, 0.02f, 0.3f, r); Tone(b, 0, 0.45f, 1400, 2100, 0.25f); Tone(b, 0.05f, 0.4f, 2100, 2800, 0.15f); break;
                case "break": b = Buf(0.7f); Noise(b, 0, 0.18f, 0.9f, 0.7f, 0.1f, r); Tone(b, 0, 0.6f, 200, 40, 0.8f, 2); Tone(b, 0.02f, 0.4f, 1200, 300, 0.3f, 1); break;
                case "status_hit": b = Buf(0.35f); Tone(b, 0, 0.3f, 380, 200, 0.35f, 1); Noise(b, 0, 0.2f, 0.4f, 0.3f, 0.1f, r); break;
                case "claim": b = Buf(0.9f); Arp(b, new[] { 784f, 988f, 1175f, 1568f }, 0.1f, 0.35f, 0.3f); Tone(b, 0.4f, 0.5f, 1568, 1568, 0.25f, 1); break;
                default: b = Buf(0.1f); break;
            }
            for (int i = 0; i < b.Length; i++) b[i] = Mathf.Clamp(b[i], -1f, 1f);
            return b;
        }

        // Pad ambient 16 detik yang bisa di-loop (frekuensi dibulatkan ke siklus utuh agar tidak ada klik).
        static float[] MusicSamples(string key)
        {
            const float Len = 16f;
            float root; float[] ratios; float trem; float noise; int wave;
            switch (key)
            {
                case "town": root = 196f; ratios = new[] { 1f, 1.25f, 1.5f, 2f }; trem = 0.25f; noise = 0f; wave = 0; break;
                case "town_night": root = 174.6f; ratios = new[] { 1f, 1.2f, 1.5f, 2f }; trem = 0.12f; noise = 0.01f; wave = 0; break;
                case "guild": root = 164.8f; ratios = new[] { 1f, 1.2f, 1.5f, 2f }; trem = 0.2f; noise = 0f; wave = 0; break;
                case "eldrath": root = 146.8f; ratios = new[] { 1f, 1.2f, 1.5f, 1.8f }; trem = 0.3f; noise = 0.02f; wave = 0; break;
                case "forest": root = 220f; ratios = new[] { 1f, 1.5f, 2f, 3f }; trem = 0.4f; noise = 0.03f; wave = 0; break;
                case "desert": root = 174.6f; ratios = new[] { 1f, 1.125f, 1.5f, 2f }; trem = 0.15f; noise = 0.025f; wave = 0; break;
                case "swamp": root = 110f; ratios = new[] { 1f, 1.19f, 1.41f, 2f }; trem = 0.5f; noise = 0.04f; wave = 0; break;
                case "volcano": root = 92.5f; ratios = new[] { 1f, 1.19f, 1.41f, 2f }; trem = 0.6f; noise = 0.05f; wave = 2; break;
                case "bone": root = 103.8f; ratios = new[] { 1f, 1.2f, 1.5f, 1.8f }; trem = 0.3f; noise = 0.045f; wave = 0; break;
                case "demon": root = 87.3f; ratios = new[] { 1f, 1.06f, 1.41f, 2f }; trem = 0.8f; noise = 0.04f; wave = 2; break;
                case "tower": root = 98f; ratios = new[] { 1f, 1.2f, 1.5f, 2.4f }; trem = 0.35f; noise = 0.02f; wave = 2; break;
                case "combat": root = 110f; ratios = new[] { 1f, 1.5f, 2f, 3f }; trem = 2f; noise = 0.03f; wave = 2; break;
                case "boss_ogre_chief": root = 73.4f; ratios = new[] { 1f, 1.5f, 2f, 2.5f }; trem = 1.6f; noise = 0.05f; wave = 2; break;      // genderang berat
                case "boss_behemoth": root = 61.7f; ratios = new[] { 1f, 1.5f, 1.89f, 2.25f }; trem = 1.9f; noise = 0.06f; wave = 2; break;   // derap banteng, berat dan cepat
                case "boss_cerberus": root = 69.3f; ratios = new[] { 1f, 1.12f, 1.41f, 1.68f }; trem = 2.6f; noise = 0.05f; wave = 2; break;   // tiga napas, menggeram
                case "boss_ignarok": root = 65.4f; ratios = new[] { 1f, 1.19f, 1.41f, 2.38f }; trem = 2.2f; noise = 0.07f; wave = 2; break;   // bergemuruh panas
                case "boss_bone_king": root = 77.8f; ratios = new[] { 1f, 1.2f, 1.5f, 1.8f }; trem = 0.9f; noise = 0.035f; wave = 0; break;    // dingin, lambat
                case "boss_demon_lord": root = 58.3f; ratios = new[] { 1f, 1.06f, 1.41f, 2.12f }; trem = 3.0f; noise = 0.06f; wave = 2; break; // gelap, berdenyut cepat
                case "boss": root = 82.4f; ratios = new[] { 1f, 1.06f, 1.5f, 2f }; trem = 1.0f; noise = 0.03f; wave = 2; break;
                default: root = 196f; ratios = new[] { 1f, 1.5f }; trem = 0.25f; noise = 0f; wave = 0; break;
            }
            var b = Buf(Len);
            var r = new System.Random(key.GetHashCode());
            float lp = 0;
            for (int k = 0; k < ratios.Length; k++)
            {
                float f = Mathf.Round(root * ratios[k] * Len) / Len;
                float tf = Mathf.Round(trem * (1 + k * 0.37f) * Len) / Len;
                for (int i = 0; i < b.Length; i++)
                {
                    float t = (float)i / Rate;
                    float s = Mathf.Sin(2 * Mathf.PI * f * t);
                    if (wave == 2) s = s * 0.6f + 0.4f * (2f * ((f * t) % 1f) - 1f);
                    b[i] += s * (0.55f + 0.45f * Mathf.Sin(2 * Mathf.PI * tf * t + k)) * 0.12f;
                }
            }
            if (noise > 0f)
                for (int i = 0; i < b.Length; i++) { lp += 0.02f * ((float)r.NextDouble() * 2f - 1f - lp); b[i] += lp * noise * 8f; }
            return b;
        }
    }

    /// <summary>Pintu masuk statis: Audio.Sfx("hit"), Audio.Music("boss"), Audio.Voice("smith", "teks").</summary>
    public static class Audio
    {
        public static void Sfx(string name) { if (AudioManager.I != null) AudioManager.I.PlaySfx(name, null); }
        public static void Sfx(string name, Vector3 pos) { if (AudioManager.I != null) AudioManager.I.PlaySfx(name, pos); }
        public static void Music(string key) { if (AudioManager.I != null) AudioManager.I.PlayMusic(key); }
        public static void Voice(string key, string text = "") { if (AudioManager.I != null) AudioManager.I.PlayVoice(key, text); }
        public static void SetVolumes(float music, float sfx, float voice = -1f) { if (AudioManager.I != null) AudioManager.I.SetVolumes(music, sfx, voice); }
    }
}
