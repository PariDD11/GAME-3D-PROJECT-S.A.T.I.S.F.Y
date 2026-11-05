using System.Collections;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Lonceng Katedral Ashwatch (n89, Docs/LORE_BIBLE.md 8.9). Berdentang ENAM kali tiap fajar (06.00) dan senja (18.00) waktu permainan,
    /// jeda sekitar 3,6 detik antar dentang, lalu diam. Tidak ada dentang ketujuh: kota menahan napas tujuh tarikan (kanon "Katedral bernyanyi", Bab 32-33).
    /// Dipasang oleh TownSceneBuilder.T2CathedralCanon() di atas menara depan Katedral (hanya scene Kota).
    /// Bunyi: Resources/Audio/sfx_bell_toll (AudioManager.Clip). Sumber suara 3D milik sendiri supaya jangkauannya jauh (terdengar di seluruh kota, bukan 45 m).
    /// Berbunyi hanya bila pemain berada dalam 'hearRange' meter; saat tidur/loncat waktu melewati 06.00 atau 18.00 ia berdentang sekali lewat sesudahnya.
    /// </summary>
    public class CathedralBell : MonoBehaviour
    {
        public float hearRange = 260f;
        public float interval = 3.6f;
        public int tolls = 6;
        public float dawnHour = 6f, duskHour = 18f;

        AudioSource src;
        float lastT = -1f;
        Coroutine ring;

        void Awake()
        {
            src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = 30f;
            src.maxDistance = hearRange;
            src.dopplerLevel = 0f;
        }

        void Update()
        {
            var gs = GameState.I;
            if (gs == null) return;
            float t = gs.timeOfDay;
            if (lastT >= 0f && ring == null && (Crossed(lastT, t, dawnHour) || Crossed(lastT, t, duskHour))) Ring();
            lastT = t;
        }

        /// <summary>Apakah jam h terlewati dari waktu a ke b (24 jam melingkar). b lebih kecil dari a banyak (lebih dari 12 jam) berarti lewat tengah malam atau tidur;
        /// mundur sedikit (muat ulang, atur jam) tidak dihitung.</summary>
        public static bool Crossed(float a, float b, float h)
        {
            if (b >= a) return a < h && b >= h;
            if (a - b < 12f) return false;
            return a < h || b >= h;
        }

        /// <summary>Mulai satu rangkaian dentang (juga bisa dipanggil dari kode lain, mis. cerita).</summary>
        public void Ring()
        {
            if (ring == null) ring = StartCoroutine(RingRoutine());
        }

        IEnumerator RingRoutine()
        {
            var am = AudioManager.I;
            var pl = GameObject.FindGameObjectWithTag("Player");
            bool near = pl != null && (pl.transform.position - transform.position).sqrMagnitude <= hearRange * hearRange;
            AudioClip clip = (am != null && near) ? am.Clip("bell_toll") : null;
            if (clip != null)
            {
                for (int i = 0; i < tolls; i++)
                {
                    src.pitch = 1f;
                    src.PlayOneShot(clip, am.sfxVol);
                    yield return new WaitForSeconds(interval);
                }
            }
            ring = null;
        }
    }
}
