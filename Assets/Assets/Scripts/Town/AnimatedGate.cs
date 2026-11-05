using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Gerbang kota beranimasi (ada 12 di kota: 4 gerbang tembok luar, 4 tembok dalam, 4 tembok tengah).
    /// Urutan buka: terali (portcullis) naik, lalu dua daun pintu berayun ke luar. Urutan tutup kebalikannya.
    /// Gerbang kota kini TERBUKA terus (alwaysOpen); gerbang tersegel tetap tertutup. Mode lama (izin penjaga) aktif bila alwaysOpen = false.
    /// </summary>
    public class AnimatedGate : MonoBehaviour
    {
        public static readonly List<AnimatedGate> All = new List<AnimatedGate>();
        /// <summary>Daftar yang membuat gerbang membuka selain pemain (NPC terdaftar di WanderNPC).</summary>
        public static readonly List<Transform> Walkers = new List<Transform>();
        /// <summary>Gerbang Luar Selatan: jalan menuju Hutan Lerai.</summary>
        public static AnimatedGate Main { get; private set; }

        public string gateId = "gate";
        public string displayName = "Gerbang";
        public bool isMain;
        public bool sealedGate;             // hanya gerbang tersegel yang boleh tertutup
        public bool alwaysOpen = true;      // gerbang kota terbuka terus; false hanya untuk gerbang tersegel

        public Transform leftPivot, rightPivot, portcullis;
        public float leftSign = -1f, rightSign = 1f;     // arah putar daun pintu supaya membuka ke luar
        public float openAngle = 100f;
        public float doorTime = 1.8f;
        public float liftTime = 1.3f;
        public float liftHeight = 8f;
        public float triggerRadius = 17f;
        public float closeDelay = 4.0f;
        public float stayRadius = 8f;       // selama pemain masih sedekat ini, gerbang yang sedang terbuka tidak menutup

        float door, lift;          // 0 = tertutup, 1 = terbuka penuh
        float holdUntil, lastNear;
        float nextScan;
        bool wantOpen, wasOpen;
        Vector3 portBase;
        Transform player;
        Collider[] blockers;
        bool blockersOn = true;

        public bool IsOpen { get { return door > 0.98f; } }
        public bool IsMoving { get { return (door > 0.001f && door < 0.999f) || (lift > 0.001f && lift < 0.999f); } }

        void OnEnable()
        {
            All.Add(this);
            if (isMain) Main = this;
        }

        void OnDisable()
        {
            All.Remove(this);
            if (Main == this) Main = null;
        }

        void Start()
        {
            if (!sealedGate) alwaysOpen = true;   // paksa terbuka, termasuk scene lama
            // referensi kosong di scene lama: cari berdasarkan nama
            if (leftPivot == null) leftPivot = FindByName("DoorPivotL");
            if (rightPivot == null) rightPivot = FindByName("DoorPivotR");
            if (portcullis == null) portcullis = FindByName("Portcullis");
            if (portcullis != null) portBase = portcullis.localPosition;
            bool baked = leftPivot != null && Quaternion.Angle(leftPivot.localRotation, Quaternion.identity) > 30f;   // pose terbuka sudah dipanggang saat build
            lastNear = -999f;
            // collider daun pintu & terali dilepas begitu gerbang mulai terbuka, jadi pemain yang terus berjalan tidak tersangkut
            var list = new List<Collider>();
            foreach (var t in new[] { leftPivot, rightPivot, portcullis })
                if (t != null) list.AddRange(t.GetComponentsInChildren<Collider>(true));
            blockers = list.ToArray();
            if (alwaysOpen)
            {
                // terbuka permanen: daun pintu terbuka lebar dan terali terangkat (tetap terlihat), collider dimatikan
                foreach (var c in blockers) if (c != null) c.enabled = false;
                blockersOn = false;
                door = 1f; lift = 1f;
                if (!baked) Apply();
                enabled = false;
                return;
            }
            Apply();
        }

        Transform FindByName(string n)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true)) if (t != transform && t.name == n) return t;
            return null;
        }

        /// <summary>Tahan gerbang tetap terbuka beberapa detik (dipakai tombol "Buka gerbang" di Gerbang Kota).</summary>
        public void HoldOpen(float seconds) { holdUntil = Time.time + seconds; }

        public void Open() { HoldOpen(25f); }

        static float Ease(float t) { return t * t * (3f - 2f * t); }

        void Update()
        {
            float dt = Time.deltaTime;
            if (Time.time >= nextScan)
            {
                nextScan = Time.time + 0.15f;
                Scan();
            }
            wantOpen = alwaysOpen || Time.time < holdUntil || (door > 0.05f && Time.time - lastNear < closeDelay);
            if (wantOpen && !wasOpen) Audio.Sfx("gate", transform.position);
            wasOpen = wantOpen;

            if (wantOpen)
            {
                lift = Mathf.MoveTowards(lift, 1f, dt / liftTime);
                if (lift > 0.35f) door = Mathf.MoveTowards(door, 1f, dt / doorTime);
            }
            else
            {
                door = Mathf.MoveTowards(door, 0f, dt / doorTime);
                if (door < 0.35f) lift = Mathf.MoveTowards(lift, 0f, dt / liftTime);
            }
            Apply();
            bool on = lift < 0.12f && door < 0.12f;
            if (on != blockersOn)
            {
                blockersOn = on;
                if (blockers != null) foreach (var c in blockers) if (c != null) c.enabled = on;
            }
        }

        void Scan()
        {
            if (door < 0.05f && Time.time >= holdUntil) return;           // tertutup: tidak ada yang perlu dipantau
            if (player != null && !player.gameObject.activeInHierarchy) player = null;
            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
            }
            if (player != null && Flat(player.position - transform.position) < stayRadius * stayRadius) lastNear = Time.time;
        }

        static float Flat(Vector3 d) { return d.x * d.x + d.z * d.z; }

        void Apply()
        {
            float a = openAngle * Ease(door);
            if (leftPivot != null) leftPivot.localRotation = Quaternion.Euler(0f, leftSign * a, 0f);
            if (rightPivot != null) rightPivot.localRotation = Quaternion.Euler(0f, rightSign * a, 0f);
            if (portcullis != null) portcullis.localPosition = portBase + new Vector3(0f, liftHeight * Ease(lift), 0f);
        }
    }
}
