using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// Satwa liar penghias area hunt (tidak menyerang, tidak bisa dibunuh): kelinci, rusa, rubah, burung, kadal, katak, kelelawar, dll.
    /// Dibuat saat runtime berdasarkan nama scene (tanpa rebuild): hewan muncul di sekitar pemain dan lenyap jika terlalu jauh.
    /// Hewan darat berkeliaran dan lari saat pemain mendekat; burung/kelelawar terbang melingkar.
    /// </summary>
    public class Wildlife : MonoBehaviour
    {
        class Species
        {
            public string id; public float speed, flee, scale = 1f; public bool flyer, hop; public System.Action<Transform, Color> build; public Color col;
            public float minH = 0f;
        }

        class Animal
        {
            public Species sp; public Transform t; public Transform[] wings;
            public Vector3 home, target; public float pauseUntil, fleeing, ang, angSpeed, radius, alt, phase;
        }

        static readonly Dictionary<string, Species> Sp = new Dictionary<string, Species>();
        readonly List<Animal> animals = new List<Animal>();
        string[] ground, air;
        int groundMax, airMax;
        Transform player;
        float nextSpawn;

        static Color Dk(Color c, float k) { return new Color(c.r * k, c.g * k, c.b * k, 1f); }

        static GameObject P(PrimitiveType t, Transform p, Vector3 pos, Vector3 sc, Color c) { return RT.Prim(t, p, pos, sc, c); }

        static void Define()
        {
            if (Sp.Count > 0) return;
            Sp["rabbit"] = new Species { id = "rabbit", speed = 5.5f, flee = 9f, hop = true, col = new Color(0.62f, 0.52f, 0.4f), build = (t, c) =>
            {
                P(PrimitiveType.Sphere, t, new Vector3(0, 0.25f, 0), new Vector3(0.38f, 0.34f, 0.55f), c);
                P(PrimitiveType.Sphere, t, new Vector3(0, 0.38f, 0.28f), new Vector3(0.24f, 0.24f, 0.26f), c);
                foreach (float s in new[] { -1f, 1f }) { var e = P(PrimitiveType.Cylinder, t, new Vector3(s * 0.07f, 0.62f, 0.26f), new Vector3(0.06f, 0.16f, 0.05f), Dk(c, 0.9f)); e.transform.localRotation = Quaternion.Euler(-10f, 0, s * 8f); }
                P(PrimitiveType.Sphere, t, new Vector3(0, 0.27f, -0.3f), new Vector3(0.14f, 0.14f, 0.14f), Color.white);
            } };
            Sp["deer"] = new Species { id = "deer", speed = 8f, flee = 16f, col = new Color(0.55f, 0.38f, 0.22f), build = (t, c) =>
            {
                P(PrimitiveType.Capsule, t, new Vector3(0, 1.0f, 0), new Vector3(0.6f, 0.75f, 0.6f), c).transform.localRotation = Quaternion.Euler(90f, 0, 0);
                P(PrimitiveType.Cylinder, t, new Vector3(0, 1.45f, 0.65f), new Vector3(0.18f, 0.35f, 0.18f), c).transform.localRotation = Quaternion.Euler(30f, 0, 0);
                P(PrimitiveType.Sphere, t, new Vector3(0, 1.75f, 0.9f), new Vector3(0.28f, 0.26f, 0.38f), c);
                P(PrimitiveType.Sphere, t, new Vector3(0, 1.0f, -0.7f), new Vector3(0.12f, 0.12f, 0.12f), new Color(0.95f, 0.9f, 0.8f));
                foreach (float lx in new[] { -0.2f, 0.2f }) foreach (float lz in new[] { -0.55f, 0.55f })
                    P(PrimitiveType.Cylinder, t, new Vector3(lx, 0.45f, lz), new Vector3(0.1f, 0.45f, 0.1f), Dk(c, 0.7f));
                foreach (float s in new[] { -1f, 1f })
                {
                    var a = P(PrimitiveType.Cylinder, t, new Vector3(s * 0.12f, 2.05f, 0.82f), new Vector3(0.035f, 0.28f, 0.035f), new Color(0.82f, 0.74f, 0.6f)); a.transform.localRotation = Quaternion.Euler(-20f, 0, s * 25f);
                    var b = P(PrimitiveType.Cylinder, t, new Vector3(s * 0.22f, 2.18f, 0.78f), new Vector3(0.03f, 0.2f, 0.03f), new Color(0.82f, 0.74f, 0.6f)); b.transform.localRotation = Quaternion.Euler(-10f, 0, s * 55f);
                }
                P(PrimitiveType.Sphere, t, new Vector3(0.2f, 1.2f, 0.1f), new Vector3(0.08f, 0.08f, 0.08f), new Color(0.95f, 0.9f, 0.8f));
                P(PrimitiveType.Sphere, t, new Vector3(-0.15f, 1.3f, -0.2f), new Vector3(0.08f, 0.08f, 0.08f), new Color(0.95f, 0.9f, 0.8f));
            } };
            Sp["gazelle"] = new Species { id = "gazelle", speed = 10f, flee = 18f, scale = 0.85f, col = new Color(0.78f, 0.6f, 0.38f), build = (t, c) => { Sp["deer"].build(t, c); } };
            Sp["fox"] = new Species { id = "fox", speed = 7f, flee = 12f, col = new Color(0.85f, 0.42f, 0.14f), build = (t, c) =>
            {
                P(PrimitiveType.Capsule, t, new Vector3(0, 0.4f, 0), new Vector3(0.32f, 0.4f, 0.32f), c).transform.localRotation = Quaternion.Euler(90f, 0, 0);
                P(PrimitiveType.Sphere, t, new Vector3(0, 0.55f, 0.45f), new Vector3(0.24f, 0.22f, 0.3f), c);
                P(PrimitiveType.Sphere, t, new Vector3(0, 0.5f, 0.65f), new Vector3(0.09f, 0.08f, 0.1f), Color.black);
                foreach (float s in new[] { -1f, 1f }) P(PrimitiveType.Sphere, t, new Vector3(s * 0.08f, 0.72f, 0.4f), new Vector3(0.08f, 0.14f, 0.05f), Dk(c, 0.8f));
                var tail = P(PrimitiveType.Capsule, t, new Vector3(0, 0.45f, -0.6f), new Vector3(0.18f, 0.35f, 0.18f), c); tail.transform.localRotation = Quaternion.Euler(70f, 0, 0);
                P(PrimitiveType.Sphere, t, new Vector3(0, 0.42f, -0.92f), new Vector3(0.14f, 0.14f, 0.2f), Color.white);
                foreach (float lx in new[] { -0.1f, 0.1f }) foreach (float lz in new[] { -0.3f, 0.3f }) P(PrimitiveType.Cylinder, t, new Vector3(lx, 0.15f, lz), new Vector3(0.06f, 0.15f, 0.06f), Dk(c, 0.4f));
            } };
            Sp["lizard"] = new Species { id = "lizard", speed = 4.5f, flee = 7f, col = new Color(0.55f, 0.5f, 0.3f), build = (t, c) =>
            {
                P(PrimitiveType.Sphere, t, new Vector3(0, 0.08f, 0), new Vector3(0.14f, 0.07f, 0.4f), c);
                P(PrimitiveType.Sphere, t, new Vector3(0, 0.09f, 0.24f), new Vector3(0.1f, 0.06f, 0.14f), c);
                var tail = P(PrimitiveType.Cylinder, t, new Vector3(0, 0.05f, -0.45f), new Vector3(0.05f, 0.25f, 0.05f), Dk(c, 0.85f)); tail.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            } };
            Sp["frog"] = new Species { id = "frog", speed = 3.5f, flee = 5f, hop = true, col = new Color(0.25f, 0.5f, 0.2f), build = (t, c) =>
            {
                P(PrimitiveType.Sphere, t, new Vector3(0, 0.12f, 0), new Vector3(0.28f, 0.2f, 0.32f), c);
                foreach (float s in new[] { -1f, 1f }) P(PrimitiveType.Sphere, t, new Vector3(s * 0.08f, 0.24f, 0.1f), new Vector3(0.07f, 0.07f, 0.07f), new Color(0.9f, 0.9f, 0.4f));
            } };
            Sp["rat"] = new Species { id = "rat", speed = 4.5f, flee = 6f, col = new Color(0.32f, 0.3f, 0.28f), build = (t, c) =>
            {
                P(PrimitiveType.Sphere, t, new Vector3(0, 0.1f, 0), new Vector3(0.16f, 0.14f, 0.3f), c);
                P(PrimitiveType.Sphere, t, new Vector3(0, 0.11f, 0.18f), new Vector3(0.1f, 0.09f, 0.14f), c);
                var tail = P(PrimitiveType.Cylinder, t, new Vector3(0, 0.07f, -0.32f), new Vector3(0.02f, 0.18f, 0.02f), new Color(0.6f, 0.45f, 0.45f)); tail.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            } };
            Sp["bird"] = new Species { id = "bird", speed = 6f, flyer = true, scale = 1f, col = new Color(0.3f, 0.3f, 0.35f), build = BuildBird };
            Sp["vulture"] = new Species { id = "vulture", speed = 5f, flyer = true, scale = 2.2f, col = new Color(0.22f, 0.17f, 0.14f), build = BuildBird };
            Sp["bat"] = new Species { id = "bat", speed = 7f, flyer = true, scale = 1.1f, col = new Color(0.14f, 0.1f, 0.16f), build = BuildBird };
        }

        static void BuildBird(Transform t, Color c)
        {
            P(PrimitiveType.Sphere, t, Vector3.zero, new Vector3(0.22f, 0.2f, 0.4f), c);
            P(PrimitiveType.Sphere, t, new Vector3(0, 0.04f, 0.22f), new Vector3(0.14f, 0.14f, 0.16f), c);
            P(PrimitiveType.Sphere, t, new Vector3(0, 0, -0.26f), new Vector3(0.1f, 0.04f, 0.26f), Dk(c, 0.8f));
            foreach (float s in new[] { -1f, 1f })
            {
                var pivot = new GameObject(s < 0 ? "WingL" : "WingR").transform;
                pivot.SetParent(t, false);
                pivot.localPosition = new Vector3(s * 0.1f, 0.03f, 0f);
                P(PrimitiveType.Cube, pivot, new Vector3(s * 0.35f, 0, 0), new Vector3(0.7f, 0.025f, 0.28f), Dk(c, 0.9f));
            }
        }

        // ------------------------------------------------------------------ scene
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            string s = SceneManager.GetActiveScene().name;
            if (!s.StartsWith("HuntArea")) return;
            if (Object.FindFirstObjectByType<Wildlife>() != null) return;
            new GameObject("Wildlife").AddComponent<Wildlife>();
        }

        void Start()
        {
            Define();
            string s = SceneManager.GetActiveScene().name;
            if (s.Contains("Desert")) { ground = new[] { "gazelle", "gazelle", "lizard", "lizard", "lizard", "rat" }; air = new[] { "vulture", "vulture", "bird" }; groundMax = 22; airMax = 7; }
            else if (s.Contains("Swamp")) { ground = new[] { "frog", "frog", "frog", "rat", "rat" }; air = new[] { "bat", "bat", "bird" }; groundMax = 24; airMax = 8; }
            else if (s.Contains("Volcano")) { ground = new[] { "lizard", "lizard", "rat" }; air = new[] { "bat", "bird" }; groundMax = 14; airMax = 6; }
            else if (s.Contains("Bone")) { ground = new[] { "rat", "rat", "lizard" }; air = new[] { "bird", "bird", "bat" }; groundMax = 14; airMax = 8; }
            else if (s.Contains("Demon")) { ground = new[] { "rat", "lizard" }; air = new[] { "bat", "bat" }; groundMax = 10; airMax = 8; }
            else { ground = new[] { "rabbit", "rabbit", "rabbit", "deer", "deer", "fox", "frog" }; air = new[] { "bird", "bird", "bird" }; groundMax = 34; airMax = 9; }
            // burung gagak/burung merah: warna beda per area
        }

        Color Tone(Species sp)
        {
            string s = SceneManager.GetActiveScene().name;
            Color c = sp.col;
            if (sp.id == "bird") c = s.Contains("Tutorial") || s.Contains("HuntArea_G") || s.Contains("Frontier") ? Color.Lerp(c, new Color(0.65f, 0.4f, 0.25f), Random.value) : new Color(0.12f, 0.12f, 0.14f);
            if (sp.id == "bat" && s.Contains("Demon")) c = new Color(0.35f, 0.08f, 0.2f);
            if (sp.id == "lizard" && s.Contains("Volcano")) c = new Color(0.75f, 0.28f, 0.1f);
            return Color.Lerp(c, Dk(c, 0.75f), Random.value * 0.4f);
        }

        bool Valid(Vector3 p)
        {
            if (p.y < 16.8f) return false;
            var t = Terrain.activeTerrain;
            if (t == null) return true;
            Vector3 lp = p - t.transform.position;
            var sz = t.terrainData.size;
            if (lp.x < 8f || lp.z < 8f || lp.x > sz.x - 8f || lp.z > sz.z - 8f) return false;
            return t.terrainData.GetSteepness(lp.x / sz.x, lp.z / sz.z) < 28f;
        }

        void Spawn(string id, bool flyer)
        {
            Species sp;
            if (!Sp.TryGetValue(id, out sp)) return;
            for (int tries = 0; tries < 8; tries++)
            {
                Vector2 o = Random.insideUnitCircle.normalized * Random.Range(28f, 95f);
                Vector3 p = player.position + new Vector3(o.x, 0f, o.y);
                p.y = RT.GroundY(p);
                if (!Valid(p)) continue;
                var go = new GameObject("Wild_" + id);
                go.transform.position = p;
                var body = new GameObject("Body").transform;
                body.SetParent(go.transform, false);
                body.localScale = Vector3.one * sp.scale;
                sp.build(body, Tone(sp));
                var a = new Animal { sp = sp, t = go.transform, home = p, target = p, pauseUntil = Time.time + Random.Range(0f, 3f), phase = Random.value * 10f };
                if (flyer)
                {
                    a.radius = Random.Range(10f, 28f); a.alt = Random.Range(14f, 30f); a.ang = Random.value * 6.28f;
                    a.angSpeed = sp.speed / a.radius * (Random.value < 0.5f ? 1f : -1f);
                    var w = new List<Transform>();
                    foreach (var tr in body.GetComponentsInChildren<Transform>()) if (tr.name == "WingL" || tr.name == "WingR") w.Add(tr);
                    a.wings = w.ToArray();
                }
                animals.Add(a);
                return;
            }
        }

        void Update()
        {
            if (player == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) player = p.transform;
                return;
            }
            float dt = Time.deltaTime;
            Vector3 pp = player.position;
            int g = 0, f = 0;
            for (int i = animals.Count - 1; i >= 0; i--)
            {
                var a = animals[i];
                if (a.t == null) { animals.RemoveAt(i); continue; }
                if (Vector3.Distance(a.t.position, pp) > 130f) { Destroy(a.t.gameObject); animals.RemoveAt(i); continue; }
                if (a.sp.flyer) { f++; Fly(a, pp, dt); } else { g++; Walk(a, pp, dt); }
            }
            if (Time.time >= nextSpawn)
            {
                nextSpawn = Time.time + 0.4f;
                if (g < groundMax && ground != null) Spawn(ground[Random.Range(0, ground.Length)], false);
                else if (f < airMax && air != null) Spawn(air[Random.Range(0, air.Length)], true);
            }
        }

        void Walk(Animal a, Vector3 pp, float dt)
        {
            Vector3 pos = a.t.position;
            Vector3 toP = pos - pp; toP.y = 0f;
            float d = toP.magnitude;
            Vector3 move = Vector3.zero; float spd = 0f;
            if (d < a.sp.flee) a.fleeing = Time.time + 2.5f;
            if (Time.time < a.fleeing)
            {
                Vector3 dir = toP.sqrMagnitude > 0.01f ? toP.normalized : a.t.forward;
                move = dir; spd = a.sp.speed;
                a.target = pos + dir * 10f; a.home = pos;
            }
            else if (Time.time >= a.pauseUntil)
            {
                Vector3 dd = a.target - pos; dd.y = 0f;
                if (dd.magnitude < 0.8f)
                {
                    a.pauseUntil = Time.time + Random.Range(2f, 7f);
                    Vector2 r = Random.insideUnitCircle * 12f;
                    a.target = a.home + new Vector3(r.x, 0f, r.y);
                }
                else { move = dd.normalized; spd = a.sp.speed * 0.3f; }
            }
            if (spd > 0f)
            {
                Vector3 np = pos + move * spd * dt;
                np.y = RT.GroundY(np);
                if (Valid(np)) { pos = np; a.t.rotation = Quaternion.Slerp(a.t.rotation, Quaternion.LookRotation(move), 10f * dt); }
                else { a.target = a.home; a.fleeing = 0f; }
            }
            float hop = a.sp.hop && spd > 0f ? Mathf.Abs(Mathf.Sin(Time.time * (spd > a.sp.speed * 0.5f ? 14f : 8f) + a.phase)) * (a.sp.id == "frog" ? 0.18f : 0.28f) : 0f;
            a.t.position = new Vector3(pos.x, RT.GroundY(pos) + hop, pos.z);
        }

        void Fly(Animal a, Vector3 pp, float dt)
        {
            a.ang += a.angSpeed * dt;
            Vector3 c = a.home;
            // pusat orbit ikut pelan mengikuti pemain agar selalu ada burung di dekatnya
            a.home = Vector3.MoveTowards(a.home, new Vector3(pp.x, a.home.y, pp.z), 1.2f * dt);
            Vector3 np = new Vector3(c.x + Mathf.Cos(a.ang) * a.radius, RT.GroundY(c) + a.alt + Mathf.Sin(Time.time * 0.7f + a.phase) * 1.5f, c.z + Mathf.Sin(a.ang) * a.radius);
            Vector3 dir = np - a.t.position;
            if (dir.sqrMagnitude > 0.0001f) a.t.rotation = Quaternion.Slerp(a.t.rotation, Quaternion.LookRotation(dir), 6f * dt);
            a.t.position = np;
            if (a.wings != null)
            {
                float flap = Mathf.Sin(Time.time * (a.sp.id == "bat" ? 18f : a.sp.id == "vulture" ? 3f : 10f) + a.phase) * 35f;
                for (int i = 0; i < a.wings.Length; i++) a.wings[i].localRotation = Quaternion.Euler(0, 0, (a.wings[i].name == "WingL" ? -1f : 1f) * flap);
            }
        }
    }
}
