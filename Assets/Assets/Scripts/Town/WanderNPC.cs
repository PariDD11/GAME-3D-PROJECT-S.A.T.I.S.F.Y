using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// NPC placeholder: berjalan bolak-balik di antara waypoint, berhenti sebentar, label nama selalu menghadap kamera.
    /// Tanpa waypoint = berdiri diam (dipakai untuk penjaga gerbang).
    /// </summary>
    public class WanderNPC : MonoBehaviour
    {
        public Transform[] waypoints;
        public float speed = 1.3f;
        public Vector2 pauseRange = new Vector2(1f, 4f);
        public Transform label;

        int idx, dir = 1;
        float pause, nextPush;
        Transform cam;

        void OnEnable() { if (waypoints != null && waypoints.Length > 1) AnimatedGate.Walkers.Add(transform); }   // penjaga (tanpa rute) tidak ikut memicu gerbang
        void OnDisable() { AnimatedGate.Walkers.Remove(transform); }

        void Start()
        {
            if (Camera.main != null) cam = Camera.main.transform;
            if (waypoints != null && waypoints.Length > 1) idx = Random.Range(0, waypoints.Length);
            SnapToTerrain();
            NpcMove.Depenetrate(transform);
        }

        /// <summary>Medan berbukit (Desa Pemula): NPC mengikuti tinggi tanah. Di scene tanpa Terrain (kota) tidak berbuat apa-apa.</summary>
        void SnapToTerrain()
        {
            var t = FarmAnimals.TerrainAt(transform.position);
            if (t == null || t.terrainData == null) return;
            Vector3 p = transform.position;
            float h = t.SampleHeight(p) + t.transform.position.y;
            if (Mathf.Abs(h - p.y) > 6f) return;               // jauh dari tanah (mis. di atas menara): jangan dipaksa turun
            p.y = h;
            transform.position = p;
        }

        void Update()
        {
            if (waypoints == null || waypoints.Length < 2) return;
            if (pause > 0f) { pause -= Time.deltaTime; return; }

            Vector3 target = waypoints[idx].position;
            target.y = transform.position.y;
            Vector3 d = target - transform.position;

            if (d.magnitude < 0.25f)
            {
                idx += dir;
                if (idx >= waypoints.Length) { dir = -1; idx = waypoints.Length - 2; }
                else if (idx < 0) { dir = 1; idx = 1; }
                pause = Random.Range(pauseRange.x, pauseRange.y);
                return;
            }

            float step = Mathf.Min(speed * Time.deltaTime, d.magnitude);
            if (NpcMove.Blocked(transform, d, step + 0.25f))
            {
                // tembok/benda padat di depan: jangan menembus, balik ke titik rute berikutnya
                idx += dir;
                if (idx >= waypoints.Length) { dir = -1; idx = Mathf.Max(0, waypoints.Length - 2); }
                else if (idx < 0) { dir = 1; idx = Mathf.Min(1, waypoints.Length - 1); }
                pause = Random.Range(0.6f, 1.6f);
                return;
            }
            transform.position += d.normalized * step;
            SnapToTerrain();
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(d.x, 0f, d.z)), 6f * Time.deltaTime);
            nextPush -= Time.deltaTime;
            if (nextPush <= 0f) { nextPush = 0.3f; NpcMove.Depenetrate(transform); }
        }

        void LateUpdate()
        {
            if (label != null && cam != null)
                label.rotation = Quaternion.LookRotation(label.position - cam.position);
        }
    }
}
