using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n37 "Dunia lebih detail": rumput 3D, petak bunga warna-warni, lavender, semak berbunga, jamur, kerikil, dan kupu-kupu
    /// yang ditebar otomatis di sekitar pemain pada SEMUA scene luar ruangan (tanpa build ulang scene). Memakai GPU instancing
    /// (HunterGuild/Foliage) sehingga ringan; area di luar jarak pandang dihapus & dibuat lagi saat pemain bergerak.
    /// Tempat tumbuh ditentukan dari lapisan terrain (rumput/lumut), bukan jalan, batu, atau air. Pengaturan: PlayerPrefs "hg_detail" (0 mati, 1 hemat, 2 penuh).
    /// </summary>
    [DefaultExecutionOrder(400)]
    public class WorldDetail : MonoBehaviour
    {
        public static int Level
        {
            get { return Mathf.Clamp(PlayerPrefs.GetInt("hg_detail", 2), 0, 2); }
            set { PlayerPrefs.SetInt("hg_detail", Mathf.Clamp(value, 0, 2)); }
        }

        static WorldDetail I;
        enum Biome { None, Meadow, Swamp, Desert }
        Biome biome = Biome.None;

        const int TGrass = 0, TFlower = 1, TLavender = 2, TBush = 3, TBloomBush = 4, TShroom = 5, TPebble = 6, TDry = 7, TCount = 8;
        const float Cs = 16f;                       // sisi sel (m)

        Material mat; Mesh[] meshes;
        Transform player; Camera cam;
        class Cell { public int cx, cz; public List<Matrix4x4>[] m = new List<Matrix4x4>[TCount]; public List<Vector4>[] c = new List<Vector4>[TCount]; }
        readonly Dictionary<long, Cell> cells = new Dictionary<long, Cell>();
        class Batch { public int type, n; public Matrix4x4[] m = new Matrix4x4[1023]; public Vector4[] c = new Vector4[1023]; public MaterialPropertyBlock mpb = new MaterialPropertyBlock(); }
        readonly List<Batch> batches = new List<Batch>();
        bool dirty; int lastCx = int.MinValue, lastCz = int.MinValue; float radius = 52f, lightT; int seed;
        Light sun;

        // kupu-kupu
        class Fly { public Transform t; public MaterialPropertyBlock mpb; public float ph, spd, rad, alt, flap; public Vector3 home; public Renderer r; }
        readonly List<Fly> flies = new List<Fly>();
        Mesh flyMesh; Transform flyRoot;

        static readonly Color[] FlowerCols =
        {
            new Color(1f, 1f, 0.96f), new Color(1f, 0.88f, 0.28f), new Color(1f, 0.56f, 0.76f), new Color(0.92f, 0.32f, 0.72f),
            new Color(0.72f, 0.58f, 1f), new Color(0.48f, 0.72f, 1f), new Color(1f, 0.6f, 0.22f), new Color(1f, 0.34f, 0.34f)
        };
        static readonly Color[] ShroomCols = { new Color(0.95f, 0.22f, 0.2f), new Color(1f, 0.62f, 0.25f), new Color(0.9f, 0.82f, 0.66f), new Color(0.72f, 0.45f, 0.9f) };
        static readonly Color[] SwampShroom = { new Color(0.3f, 0.95f, 0.95f), new Color(0.72f, 0.45f, 1f), new Color(0.62f, 1f, 0.3f) };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("WorldDetail"); DontDestroyOnLoad(go);
            I = go.AddComponent<WorldDetail>();
            SceneManager.sceneLoaded += (s, m) => { if (I != null) I.OnScene(); };
            I.OnScene();
        }

        void OnScene()
        {
            cells.Clear(); batches.Clear(); dirty = true; lastCx = int.MinValue; player = null; sun = null;
            grassyLayers.Clear(); blockLayers.Clear();
            ClearFlies();
            string n = SceneManager.GetActiveScene().name;
            seed = n.GetHashCode();
            if (InteriorInfo.IsInterior(n) || n == GuildConfig.InteriorScene || n.Contains("Tower") || n.Contains("Arena") || n.Contains("Volcano") || n.Contains("Bone") || n.Contains("Demon") || n == "DungeonCity") biome = Biome.None;
            else if (n.Contains("Swamp")) biome = Biome.Swamp;
            else if (n.Contains("Desert")) biome = Biome.Desert;
            else biome = Biome.Meadow;
            if (biome != Biome.None && mat == null) Setup();
        }

        void Setup()
        {
            var sh = Resources.Load<Shader>("HGFoliage");
            if (sh == null) { biome = Biome.None; return; }
            mat = new Material(sh) { name = "HG_Foliage", enableInstancing = true };
            meshes = new Mesh[TCount];
            meshes[TGrass] = Mesh_Grass(false);
            meshes[TDry] = Mesh_Grass(true);
            meshes[TFlower] = Mesh_Flower();
            meshes[TLavender] = Mesh_Lavender();
            meshes[TBush] = Mesh_Bush(false);
            meshes[TBloomBush] = Mesh_Bush(true);
            meshes[TShroom] = Mesh_Mushroom();
            meshes[TPebble] = Mesh_Pebble();
            flyMesh = Mesh_Butterfly();
        }

        // ------------------------------------------------------------------ mesh
        class MB
        {
            public List<Vector3> v = new List<Vector3>(); public List<Vector3> n = new List<Vector3>(); public List<Color> c = new List<Color>();
            public List<Vector2> uv = new List<Vector2>(); public List<int> t = new List<int>();
            public int V(Vector3 p, Vector3 nn, Color col, float h) { v.Add(p); n.Add(nn.normalized); c.Add(col); uv.Add(new Vector2(h, 0f)); return v.Count - 1; }
            public void Tri(int a, int b, int d) { t.Add(a); t.Add(b); t.Add(d); }
            public void Quad(int a, int b, int d, int e) { Tri(a, b, d); Tri(a, d, e); }
            public Mesh Build(string name)
            {
                var m = new Mesh { name = name };
                m.SetVertices(v); m.SetNormals(n); m.SetColors(c); m.SetUVs(0, uv); m.SetTriangles(t, 0);
                m.RecalculateBounds();
                return m;
            }
            // bola / kubah: lat,lon; asimetri (sx,sy,sz); gradien warna bawah->atas; a = bobot pewarnaan ulang
            public void Ball(Vector3 center, Vector3 radii, int lat, int lon, Color bottom, Color top, float a, float h0, float h1, float lumpy, int seedL, float latMax = Mathf.PI)
            {
                int start = v.Count;
                var rnd = new System.Random(seedL);
                for (int i = 0; i <= lat; i++)
                {
                    float th = latMax * i / lat;                    // 0 kutub atas
                    float y = Mathf.Cos(th), r = Mathf.Sin(th);
                    for (int j = 0; j <= lon; j++)
                    {
                        float ph = Mathf.PI * 2f * j / lon;
                        float lump = 1f + ((float)rnd.NextDouble() - 0.5f) * lumpy;
                        if (j == lon) lump = 1f;                    // jahitan
                        Vector3 d = new Vector3(Mathf.Sin(ph) * r, y, Mathf.Cos(ph) * r);
                        Vector3 p = center + new Vector3(d.x * radii.x, d.y * radii.y, d.z * radii.z) * lump;
                        float k = Mathf.Clamp01((d.y + 1f) * 0.5f);
                        Color col = Color.Lerp(bottom, top, k); col.a = a;
                        v.Add(p); n.Add(d); c.Add(col); uv.Add(new Vector2(Mathf.Lerp(h0, h1, k), 0f));
                    }
                }
                for (int i = 0; i < lat; i++)
                    for (int j = 0; j < lon; j++)
                    {
                        int a0 = start + i * (lon + 1) + j, b0 = a0 + lon + 1;
                        Tri(a0, b0, a0 + 1); Tri(a0 + 1, b0, b0 + 1);
                    }
            }
        }

        static Mesh Mesh_Grass(bool dry)
        {
            // rumpun rumput halus: banyak bilah tipis melengkung (4 ruas, meruncing), akar gelap -> ujung terang, normal condong ke atas
            // supaya tercahayai sama dengan tanah (tidak tampak sebagai "stiker" 3D yang kontras).
            var b = new MB(); var rnd = new System.Random(dry ? 91 : 17);
            Color baseC = dry ? new Color(0.30f, 0.24f, 0.12f) : new Color(0.075f, 0.17f, 0.065f);
            Color midC = dry ? new Color(0.55f, 0.44f, 0.21f) : new Color(0.16f, 0.30f, 0.10f);
            Color tipC = dry ? new Color(0.74f, 0.64f, 0.38f) : new Color(0.30f, 0.43f, 0.15f);
            float al = 0.4f;
            int blades = 30;
            for (int i = 0; i < blades; i++)
            {
                float ang = (float)rnd.NextDouble() * Mathf.PI * 2f, rr = Mathf.Sqrt((float)rnd.NextDouble()) * 0.17f;
                Vector3 bp = new Vector3(Mathf.Sin(ang) * rr, 0f, Mathf.Cos(ang) * rr);
                float face = ang + ((float)rnd.NextDouble() - 0.5f) * 1.6f;                        // condong menjauhi pusat rumpun
                Vector3 dir = new Vector3(Mathf.Sin(face), 0f, Mathf.Cos(face));
                Vector3 side = new Vector3(dir.z, 0f, -dir.x);
                float h = 0.2f + 0.3f * Mathf.Pow((float)rnd.NextDouble(), 1.3f);
                float lean = 0.05f + 0.2f * (float)rnd.NextDouble();
                float w = 0.011f + 0.007f * (float)rnd.NextDouble();
                Vector3 nrm = (Vector3.up * 0.92f + dir * 0.18f).normalized;
                int prevL = -1, prevR = -1;
                const int seg = 4;
                for (int k = 0; k <= seg; k++)
                {
                    float t = k / (float)seg;
                    Vector3 c = bp + dir * (lean * t * t * h * 2.2f) + Vector3.up * (h * t * (1f - 0.12f * t));
                    float ww = k == seg ? 0f : w * (1f - 0.75f * t * t);
                    Color col = t < 0.5f ? Color.Lerp(baseC, midC, t * 2f) : Color.Lerp(midC, tipC, (t - 0.5f) * 2f);
                    col.a = al;
                    if (k == seg)
                    {
                        int tp = b.V(c, nrm, col, t);
                        b.Tri(prevL, prevR, tp);
                    }
                    else
                    {
                        int l = b.V(c - side * ww, nrm, col, t), r = b.V(c + side * ww, nrm, col, t);
                        if (prevL >= 0) b.Quad(prevL, prevR, r, l);
                        prevL = l; prevR = r;
                    }
                }
            }
            return b.Build(dry ? "HG_DryGrass" : "HG_Grass");
        }

        static Mesh Mesh_Flower()
        {
            var b = new MB();
            Color stem = new Color(0.22f, 0.55f, 0.18f, 0f), stemTop = new Color(0.34f, 0.7f, 0.24f, 0f);
            float H = 0.34f;
            // batang: dua bilah silang
            for (int k = 0; k < 2; k++)
            {
                float a = k * Mathf.PI * 0.5f; Vector3 s = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.012f;
                int a0 = b.V(-s, Vector3.up, stem, 0f), a1 = b.V(s, Vector3.up, stem, 0f), a2 = b.V(s + Vector3.up * H, Vector3.up, stemTop, 0.9f), a3 = b.V(-s + Vector3.up * H, Vector3.up, stemTop, 0.9f);
                b.Quad(a0, a1, a2, a3);
            }
            // daun
            for (int k = 0; k < 2; k++)
            {
                float a = k * Mathf.PI + 0.6f; Vector3 d = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                Vector3 side = new Vector3(d.z, 0, -d.x) * 0.03f;
                int l0 = b.V(Vector3.up * 0.04f, Vector3.up, stem, 0.1f);
                int l1 = b.V(Vector3.up * 0.1f + d * 0.11f - side, Vector3.up, stemTop, 0.3f);
                int l2 = b.V(Vector3.up * 0.1f + d * 0.11f + side, Vector3.up, stemTop, 0.3f);
                int l3 = b.V(Vector3.up * 0.1f + d * 0.2f, Vector3.up, stemTop, 0.4f);
                b.Tri(l0, l1, l2); b.Tri(l1, l3, l2);
            }
            // kelopak: 6 layang-layang mengelilingi pusat, sedikit cekung
            Vector3 top = Vector3.up * H;
            Color pBase = new Color(1f, 1f, 1f, 1f), pTip = new Color(1f, 1f, 1f, 1f);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI * 2f / 6f; Vector3 d = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                Vector3 side = new Vector3(d.z, 0, -d.x);
                Vector3 inner = top + d * 0.012f + Vector3.up * 0.0f;
                Vector3 mid = top + d * 0.05f + Vector3.up * 0.018f;
                Vector3 outer = top + d * 0.095f + Vector3.up * 0.03f;
                Vector3 nrm = (Vector3.up + d * 0.25f);
                int p0 = b.V(inner, nrm, new Color(0.96f, 0.96f, 0.96f, 1f), 1f);
                int p1 = b.V(mid - side * 0.036f, nrm, pBase, 1f);
                int p2 = b.V(mid + side * 0.036f, nrm, pBase, 1f);
                int p3 = b.V(outer, nrm, pTip, 1f);
                b.Quad(p0, p1, p3, p2);
            }
            // pusat kuning
            b.Ball(top + Vector3.up * 0.012f, new Vector3(0.026f, 0.016f, 0.026f), 3, 6, new Color(1f, 0.82f, 0.2f, 0f), new Color(1f, 0.92f, 0.4f, 0f), 0f, 1f, 1f, 0f, 3);
            return b.Build("HG_Flower");
        }

        static Mesh Mesh_Lavender()
        {
            var b = new MB();
            Color stem = new Color(0.25f, 0.5f, 0.2f, 0f);
            float H = 0.55f;
            for (int k = 0; k < 2; k++)
            {
                float a = k * Mathf.PI * 0.5f; Vector3 s = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.01f;
                int a0 = b.V(-s, Vector3.up, stem, 0f), a1 = b.V(s, Vector3.up, stem, 0f), a2 = b.V(s + Vector3.up * H, Vector3.up, stem, 0.9f), a3 = b.V(-s + Vector3.up * H, Vector3.up, stem, 0.9f);
                b.Quad(a0, a1, a2, a3);
            }
            for (int i = 0; i < 7; i++)
            {
                float t = i / 6f; float y = H * 0.62f + t * H * 0.42f;
                float r = Mathf.Lerp(0.045f, 0.018f, t);
                Color lo = new Color(0.6f, 0.45f, 0.9f, 1f), hi = new Color(0.95f, 0.85f, 1f, 1f);
                b.Ball(new Vector3(0f, y, 0f), new Vector3(r, r * 1.15f, r), 3, 6, lo, hi, 1f, 0.6f + t * 0.4f, 0.8f + t * 0.2f, 0.25f, 11 + i);
            }
            return b.Build("HG_Lavender");
        }

        static Mesh Mesh_Bush(bool bloom)
        {
            var b = new MB(); var rnd = new System.Random(bloom ? 202 : 101);
            Color lo = new Color(0.06f, 0.17f, 0.07f), hi = new Color(0.24f, 0.44f, 0.14f);
            int blobs = 7;
            for (int i = 0; i < blobs; i++)
            {
                float ang = i * Mathf.PI * 2f / blobs + (float)rnd.NextDouble();
                float rr = i == 0 ? 0f : 0.3f + (float)rnd.NextDouble() * 0.2f;
                float rad = i == 0 ? 0.46f : 0.26f + (float)rnd.NextDouble() * 0.14f;
                Vector3 c = new Vector3(Mathf.Sin(ang) * rr, rad * 0.62f, Mathf.Cos(ang) * rr);
                b.Ball(c, new Vector3(rad, rad * 0.82f, rad), 5, 9, lo, hi, 0.55f, 0.1f, 1f, 0.3f, 30 + i);
            }
            if (bloom)
            {
                for (int i = 0; i < 26; i++)
                {
                    float u = (float)rnd.NextDouble() * Mathf.PI * 2f, vv = Mathf.Acos(1f - (float)rnd.NextDouble() * 0.95f);
                    Vector3 d = new Vector3(Mathf.Sin(vv) * Mathf.Sin(u), Mathf.Cos(vv), Mathf.Sin(vv) * Mathf.Cos(u));
                    Vector3 p = new Vector3(d.x * 0.62f, 0.4f + d.y * 0.5f, d.z * 0.62f);
                    float r0 = 0.05f + (float)rnd.NextDouble() * 0.035f;
                    b.Ball(p, new Vector3(r0, r0 * 0.7f, r0), 3, 6, new Color(1f, 1f, 1f, 1f), new Color(1f, 1f, 1f, 1f), 1f, 0.95f, 1f, 0f, 7 + i);
                }
            }
            return b.Build(bloom ? "HG_BloomBush" : "HG_Bush");
        }

        static Mesh Mesh_Mushroom()
        {
            var b = new MB();
            // batang
            Color stemC = new Color(0.95f, 0.9f, 0.8f, 0f);
            int sides = 6; float sh = 0.13f, sr = 0.022f;
            int s0 = b.v.Count;
            for (int ring = 0; ring < 2; ring++)
                for (int j = 0; j <= sides; j++)
                {
                    float ph = Mathf.PI * 2f * j / sides; Vector3 d = new Vector3(Mathf.Sin(ph), 0, Mathf.Cos(ph));
                    b.V(d * (ring == 0 ? sr * 1.25f : sr) + Vector3.up * (ring == 0 ? 0f : sh), d, stemC, ring == 0 ? 0f : 0.7f);
                }
            for (int j = 0; j < sides; j++) { int a = s0 + j, c = a + sides + 1; b.Tri(a, c, a + 1); b.Tri(a + 1, c, c + 1); }
            // tudung (tinted) + bintik putih
            b.Ball(new Vector3(0f, sh - 0.01f, 0f), new Vector3(0.075f, 0.058f, 0.075f), 4, 9, new Color(0.85f, 0.85f, 0.85f, 1f), new Color(1f, 1f, 1f, 1f), 1f, 0.8f, 1f, 0.05f, 5, Mathf.PI * 0.55f);
            var rnd = new System.Random(8);
            for (int i = 0; i < 5; i++)
            {
                float u = (float)rnd.NextDouble() * Mathf.PI * 2f, vv = 0.2f + (float)rnd.NextDouble() * 0.7f;
                Vector3 d = new Vector3(Mathf.Sin(vv) * Mathf.Sin(u), Mathf.Cos(vv), Mathf.Sin(vv) * Mathf.Cos(u));
                b.Ball(new Vector3(d.x * 0.074f, sh - 0.01f + d.y * 0.057f, d.z * 0.074f), new Vector3(0.012f, 0.008f, 0.012f), 2, 5, new Color(1f, 0.97f, 0.9f, 0f), new Color(1f, 0.97f, 0.9f, 0f), 0f, 1f, 1f, 0f, 2 + i);
            }
            return b.Build("HG_Mushroom");
        }

        static Mesh Mesh_Pebble()
        {
            var b = new MB();
            b.Ball(new Vector3(0f, 0.04f, 0f), new Vector3(0.16f, 0.09f, 0.12f), 4, 8, new Color(0.42f, 0.42f, 0.46f), new Color(0.72f, 0.72f, 0.74f), 0.35f, 0f, 0.2f, 0.25f, 77);
            b.Ball(new Vector3(0.2f, 0.03f, 0.06f), new Vector3(0.09f, 0.055f, 0.08f), 3, 7, new Color(0.45f, 0.44f, 0.46f), new Color(0.74f, 0.73f, 0.74f), 0.35f, 0f, 0.2f, 0.25f, 78);
            return b.Build("HG_Pebble");
        }

        static Mesh Mesh_Butterfly()
        {
            var b = new MB();
            Color body = new Color(0.12f, 0.1f, 0.12f, 0f), wing = new Color(1f, 1f, 1f, 1f), wingTip = new Color(1f, 1f, 1f, 1f);
            Color dark = new Color(0.55f, 0.55f, 0.6f, 1f);
            for (int s = -1; s <= 1; s += 2)
            {
                // sayap atas besar + bawah kecil, pangkal di sumbu tengah
                int w0 = b.V(new Vector3(0, 0, 0.02f), Vector3.up, wing, 0f);
                int w1 = b.V(new Vector3(s * 0.11f, 0.01f, 0.1f), Vector3.up, wingTip, 0f);
                int w2 = b.V(new Vector3(s * 0.14f, 0.01f, 0.01f), Vector3.up, dark, 0f);
                int w3 = b.V(new Vector3(s * 0.05f, 0.0f, -0.075f), Vector3.up, wing, 0f);
                b.Tri(w0, w1, w2); b.Tri(w0, w2, w3);
            }
            b.Quad(b.V(new Vector3(-0.012f, 0, 0.1f), Vector3.up, body, 0f), b.V(new Vector3(0.012f, 0, 0.1f), Vector3.up, body, 0f),
                   b.V(new Vector3(0.012f, 0, -0.09f), Vector3.up, body, 0f), b.V(new Vector3(-0.012f, 0, -0.09f), Vector3.up, body, 0f));
            return b.Build("HG_Butterfly");
        }

        // ------------------------------------------------------------------ tanah
        static readonly Dictionary<TerrainData, float[]> grassyLayers = new Dictionary<TerrainData, float[]>();
        static readonly Dictionary<TerrainData, float[]> blockLayers = new Dictionary<TerrainData, float[]>();

        void Classify(TerrainData td)
        {
            if (grassyLayers.ContainsKey(td)) return;
            int nl = td.alphamapLayers;
            var g = new float[nl]; var bl = new float[nl];
            bool any = false;
            for (int i = 0; i < nl; i++)
            {
                string nm = (td.terrainLayers != null && i < td.terrainLayers.Length && td.terrainLayers[i] != null ? td.terrainLayers[i].name : "").ToLowerInvariant();
                if (nm.Contains("grass") || nm.Contains("rumput")) { g[i] = 1f; any = true; }
                else if (nm.Contains("moss") || nm.Contains("lumut")) { g[i] = 0.7f; any = true; }
                else if (nm.Contains("litter") || nm.Contains("daun") || nm.Contains("leaf")) { g[i] = 0.35f; any = true; }
                if (nm.Contains("sand") || nm.Contains("pasir")) { if (biome == Biome.Desert) g[i] = 1f; else bl[i] = 1f; if (biome == Biome.Desert) any = true; }
                if (nm.Contains("dirt") || nm.Contains("tanah") || nm.Contains("rock") || nm.Contains("batu") || nm.Contains("cobble") || nm.Contains("mud") || nm.Contains("lumpur") || nm.Contains("road") || nm.Contains("jalan")) bl[i] = 1f;
            }
            if (!any && nl > 0) g[0] = 1f;
            grassyLayers[td] = g; blockLayers[td] = bl;
        }

        /// <summary>Bobot "boleh ditumbuhi" 0..1 pada titik hit (0 = jalan/batu/air/bangunan).</summary>
        float Ground(RaycastHit hit)
        {
            if (hit.normal.y < 0.72f) return 0f;
            var col = hit.collider;
            bool isTc = col != null && col.GetType().Name == "TerrainCollider";
            if (isTc)
            {
                var terr = col.GetComponent<Terrain>();
                if (terr == null || terr.terrainData == null) return 0f;
                var td = terr.terrainData;
                if (hit.point.y < -0.9f) return 0f;                                  // dasar sungai/kolam
                int res = td.alphamapResolution;
                Vector3 lp = hit.point - terr.transform.position;
                int ix = Mathf.Clamp(Mathf.RoundToInt(lp.x / td.size.x * (res - 1)), 0, res - 1);
                int iz = Mathf.Clamp(Mathf.RoundToInt(lp.z / td.size.z * (res - 1)), 0, res - 1);
                Classify(td);
                var a = td.GetAlphamaps(ix, iz, 1, 1);
                var g = grassyLayers[td]; var bk = blockLayers[td];
                float gw = 0f, bw = 0f;
                for (int i = 0; i < g.Length && i < a.GetLength(2); i++) { gw += a[0, 0, i] * g[i]; bw += a[0, 0, i] * bk[i]; }
                return Mathf.Clamp01(gw - bw * 0.9f);
            }
            var r = col.GetComponent<Renderer>();
            if (r != null && r.sharedMaterial != null && r.sharedMaterial.name.ToLowerInvariant().Contains("grass")) return 1f;
            return 0f;
        }

        // ------------------------------------------------------------------ generate
        static long Key(int cx, int cz) { return (long)cx * 200003L + cz; }

        bool Place(Cell cell, System.Random rng, int type, float x, float z, float sMin, float sMax, Color col, float minGround, float py, float tilt = 0.0f)
        {
            RaycastHit hit;
            if (!Physics.Raycast(new Vector3(x, py + 70f, z), Vector3.down, out hit, 260f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return false;
            float w = Ground(hit);
            if (w < minGround) return false;
            float s = Mathf.Lerp(sMin, sMax, (float)rng.NextDouble());
            Quaternion rot = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            if (tilt > 0f) rot = Quaternion.AngleAxis(((float)rng.NextDouble() - 0.5f) * tilt, new Vector3((float)rng.NextDouble() - 0.5f, 0f, (float)rng.NextDouble() - 0.5f)) * rot;
            else rot = Quaternion.FromToRotation(Vector3.up, Vector3.Slerp(Vector3.up, hit.normal, 0.5f)) * rot;
            cell.m[type].Add(Matrix4x4.TRS(hit.point - Vector3.up * 0.015f, rot, new Vector3(s, s * (0.9f + 0.25f * (float)rng.NextDouble()), s)));
            cell.c[type].Add(new Vector4(col.r, col.g, col.b, 1f));
            return true;
        }

        Cell Generate(int cx, int cz, float py)
        {
            var cell = new Cell { cx = cx, cz = cz };
            for (int i = 0; i < TCount; i++) { cell.m[i] = new List<Matrix4x4>(); cell.c[i] = new List<Vector4>(); }
            var rng = new System.Random(cx * 73856093 ^ cz * 19349663 ^ seed);
            float x0 = cx * Cs, z0 = cz * Cs;
            float dens = Level == 1 ? 0.55f : 1f;
            System.Func<float> R = () => (float)rng.NextDouble();

            if (biome == Biome.Meadow)
            {
                // rumput berumpun alami: kelompok berjari-jari acak + sebaran tunggal; warna per-kelompok sedikit berbeda
                int clumps = Mathf.RoundToInt(15 * dens);
                for (int c = 0; c < clumps; c++)
                {
                    float cxw = x0 + R() * Cs, czw = z0 + R() * Cs, cr = 0.5f + R() * 1.7f;
                    Color ct = Color.Lerp(new Color(1.06f, 1.0f, 0.8f), new Color(0.86f, 0.98f, 0.9f), R());
                    float csz = 0.75f + R() * 0.65f;
                    int k = 4 + rng.Next(6);
                    for (int j = 0; j < k; j++)
                    {
                        float a = R() * 6.283f, d = Mathf.Sqrt(R()) * cr;
                        Place(cell, rng, TGrass, cxw + Mathf.Sin(a) * d, czw + Mathf.Cos(a) * d, csz * 0.8f, csz * 1.25f, ct * (0.94f + 0.12f * R()), 0.45f, py);
                    }
                }
                int ng = Mathf.RoundToInt(34 * dens);
                for (int i = 0; i < ng; i++)
                {
                    float t = R();
                    Color tint = Color.Lerp(new Color(1.04f, 1.0f, 0.82f), new Color(0.88f, 1.0f, 0.92f), t);
                    Place(cell, rng, TGrass, x0 + R() * Cs, z0 + R() * Cs, 0.7f, 1.3f, tint, 0.45f, py);
                }
                // petak bunga: 2-3 petak, tiap petak 5-9 bunga satu warna (lebih mirip padang bunga anime)
                int patches = R() < 0.55f ? 2 : 1; if (R() < 0.25f) patches++;
                for (int p = 0; p < patches; p++)
                {
                    float px = x0 + R() * Cs, pz = z0 + R() * Cs;
                    Color pc = FlowerCols[rng.Next(FlowerCols.Length)];
                    Color pc2 = R() < 0.35f ? FlowerCols[rng.Next(FlowerCols.Length)] : pc;
                    int nf = Mathf.RoundToInt((5 + rng.Next(5)) * dens);
                    for (int i = 0; i < nf; i++)
                    {
                        float a = R() * 6.283f, d = Mathf.Sqrt(R()) * 1.9f;
                        Place(cell, rng, TFlower, px + Mathf.Sin(a) * d, pz + Mathf.Cos(a) * d, 0.8f, 1.35f, R() < 0.8f ? pc : pc2, 0.7f, py, 18f);
                    }
                }
                if (R() < 0.3f)
                {
                    float px = x0 + R() * Cs, pz = z0 + R() * Cs;
                    for (int i = 0; i < 6; i++)
                    {
                        float a = R() * 6.283f, d = Mathf.Sqrt(R()) * 1.2f;
                        Place(cell, rng, TLavender, px + Mathf.Sin(a) * d, pz + Mathf.Cos(a) * d, 0.9f, 1.3f, Color.white, 0.7f, py, 12f);
                    }
                }
                int nb = R() < 0.6f ? 2 : 1;
                for (int i = 0; i < nb; i++)
                {
                    bool bloom = R() < 0.35f;
                    Color c = bloom ? Color.Lerp(FlowerCols[2], FlowerCols[0], R()) : new Color(0.9f + R() * 0.2f, 1f, 0.9f + R() * 0.15f);
                    Place(cell, rng, bloom ? TBloomBush : TBush, x0 + R() * Cs, z0 + R() * Cs, 0.9f, 1.8f, c, 0.6f, py);
                }
                int nm = R() < 0.5f ? 3 : 1;
                for (int i = 0; i < nm; i++)
                {
                    float px = x0 + R() * Cs, pz = z0 + R() * Cs; Color mc = ShroomCols[rng.Next(ShroomCols.Length)];
                    int k = 1 + rng.Next(3);
                    for (int j = 0; j < k; j++) Place(cell, rng, TShroom, px + (R() - 0.5f) * 0.5f, pz + (R() - 0.5f) * 0.5f, 0.8f, 1.6f, mc, 0.5f, py, 10f);
                }
                int np = Mathf.RoundToInt(5 * dens);
                for (int i = 0; i < np; i++) Place(cell, rng, TPebble, x0 + R() * Cs, z0 + R() * Cs, 0.7f, 1.7f, new Color(1f, 1f, 1f), 0.3f, py);
            }
            else if (biome == Biome.Swamp)
            {
                int ng = Mathf.RoundToInt(70 * dens);
                for (int i = 0; i < ng; i++) Place(cell, rng, TGrass, x0 + R() * Cs, z0 + R() * Cs, 1.1f, 2.1f, new Color(0.62f, 0.86f, 0.74f), 0.4f, py);
                int nm = Mathf.RoundToInt(9 * dens);
                for (int i = 0; i < nm; i++) Place(cell, rng, TShroom, x0 + R() * Cs, z0 + R() * Cs, 1f, 2.4f, SwampShroom[rng.Next(SwampShroom.Length)], 0.35f, py, 14f);
                int nb = R() < 0.5f ? 1 : 0;
                for (int i = 0; i < nb; i++) Place(cell, rng, TBush, x0 + R() * Cs, z0 + R() * Cs, 0.9f, 1.5f, new Color(0.55f, 0.75f, 0.62f), 0.4f, py);
            }
            else if (biome == Biome.Desert)
            {
                int ng = Mathf.RoundToInt(46 * dens);
                for (int i = 0; i < ng; i++) Place(cell, rng, TDry, x0 + R() * Cs, z0 + R() * Cs, 0.8f, 1.6f, new Color(1f, 0.96f, 0.85f), 0.4f, py);
                int np = Mathf.RoundToInt(10 * dens);
                for (int i = 0; i < np; i++) Place(cell, rng, TPebble, x0 + R() * Cs, z0 + R() * Cs, 0.8f, 2.2f, new Color(1.2f, 1.05f, 0.85f), 0.3f, py);
                if (R() < 0.35f) Place(cell, rng, TBush, x0 + R() * Cs, z0 + R() * Cs, 0.8f, 1.4f, new Color(1.5f, 1.25f, 0.7f), 0.4f, py);
            }
            return cell;
        }

        // ------------------------------------------------------------------ loop
        void Update()
        {
            if (biome == Biome.None || mat == null || Level == 0) { if (batches.Count > 0 || cells.Count > 0) { cells.Clear(); batches.Clear(); dirty = true; } SetFlies(false); return; }
            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p == null) return;
                player = p.transform;
            }
            if (cam == null || !cam.isActiveAndEnabled) cam = Camera.main;
            if (cam == null) return;

            radius = Level == 1 ? 34f : 52f;
            lightT -= Time.unscaledDeltaTime;
            if (lightT <= 0f) { lightT = 0.4f; PushLight(); }

            Vector3 pp = player.position;
            int cx = Mathf.FloorToInt(pp.x / Cs), cz = Mathf.FloorToInt(pp.z / Cs);
            if (cx != lastCx || cz != lastCz || dirty)
            {
                lastCx = cx; lastCz = cz;
                int rc = Mathf.CeilToInt(radius / Cs);
                var drop = new List<long>();
                foreach (var kv in cells)
                    if (Mathf.Abs(kv.Value.cx - cx) > rc + 1 || Mathf.Abs(kv.Value.cz - cz) > rc + 1) drop.Add(kv.Key);
                foreach (var k in drop) cells.Remove(k);
                if (drop.Count > 0) dirty = true;
            }
            // buat sel yang belum ada, paling dekat dulu (maks 2 per frame)
            int budget = 2; int rc2 = Mathf.CeilToInt(radius / Cs);
            for (int ring = 0; ring <= rc2 && budget > 0; ring++)
                for (int dx = -ring; dx <= ring && budget > 0; dx++)
                    for (int dz = -ring; dz <= ring && budget > 0; dz++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != ring) continue;
                        float cxw = (cx + dx + 0.5f) * Cs - pp.x, czw = (cz + dz + 0.5f) * Cs - pp.z;
                        if (cxw * cxw + czw * czw > (radius + Cs) * (radius + Cs)) continue;
                        long key = Key(cx + dx, cz + dz);
                        if (cells.ContainsKey(key)) continue;
                        cells[key] = Generate(cx + dx, cz + dz, pp.y);
                        dirty = true; budget--;
                    }
            if (dirty) { Rebuild(pp); dirty = false; }
            Shader.SetGlobalFloat("_HGFolFade", radius);
            for (int i = 0; i < batches.Count; i++)
            {
                var b = batches[i];
                Graphics.DrawMeshInstanced(meshes[b.type], 0, mat, b.m, b.n, b.mpb, ShadowCastingMode.Off, false, 0, cam);
            }
            UpdateFlies(pp);
        }

        void Rebuild(Vector3 pp)
        {
            batches.Clear();
            for (int type = 0; type < TCount; type++)
            {
                Batch cur = null;
                foreach (var kv in cells)
                {
                    var cell = kv.Value;
                    float ccx = (cell.cx + 0.5f) * Cs - pp.x, ccz = (cell.cz + 0.5f) * Cs - pp.z;
                    if (ccx * ccx + ccz * ccz > (radius + Cs * 0.8f) * (radius + Cs * 0.8f)) continue;
                    var ml = cell.m[type]; var cl = cell.c[type];
                    for (int i = 0; i < ml.Count; i++)
                    {
                        if (cur == null || cur.n >= 1023) { Seal(cur); cur = new Batch { type = type }; batches.Add(cur); }
                        cur.m[cur.n] = ml[i]; cur.c[cur.n] = cl[i]; cur.n++;
                    }
                }
                Seal(cur);
            }
        }

        static void Seal(Batch b) { if (b != null) b.mpb.SetVectorArray("_Color", b.c); }

        void PushLight()
        {
            if (sun == null)
            {
                var dn = FindFirstObjectByType<TownDayNight>();
                sun = dn != null ? dn.sun : RenderSettings.sun;
                if (sun == null) foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) { sun = l; break; }
            }
            Vector3 f = sun != null ? sun.transform.forward : new Vector3(0.3f, -0.8f, 0.4f);
            Color sc = sun != null ? sun.color * Mathf.Clamp(sun.intensity, 0.35f, 1.3f) : Color.white;
            Color amb = RenderSettings.ambientMode == AmbientMode.Flat ? RenderSettings.ambientLight
                      : (RenderSettings.ambientSkyColor + RenderSettings.ambientEquatorColor) * 0.5f;
            amb *= Mathf.Max(0.5f, RenderSettings.ambientIntensity);
            Shader.SetGlobalVector("_HGFolDir", new Vector4(f.x, f.y, f.z, 0f));
            Shader.SetGlobalColor("_HGFolCol", sc);
            Shader.SetGlobalColor("_HGFolAmb", amb);
        }

        // ------------------------------------------------------------------ kupu-kupu
        void ClearFlies() { foreach (var f in flies) if (f.t != null) Destroy(f.t.gameObject); flies.Clear(); if (flyRoot != null) Destroy(flyRoot.gameObject); flyRoot = null; }

        void SetFlies(bool on) { if (flyRoot != null && flyRoot.gameObject.activeSelf != on) flyRoot.gameObject.SetActive(on); }

        void UpdateFlies(Vector3 pp)
        {
            bool day = sun != null && -sun.transform.forward.y > 0.18f;
            bool calm = true;
            try { calm = SeasonRuntime.Weather == Wx.Cerah || SeasonRuntime.Weather == Wx.Berawan; } catch { }
            bool want = biome == Biome.Meadow && day && calm && Level >= 1;
            if (!want) { SetFlies(false); return; }
            if (flyRoot == null)
            {
                flyRoot = new GameObject("Kupu2").transform; flyRoot.SetParent(transform, false);
                int n = Level == 1 ? 5 : 9;
                var rnd = new System.Random(5);
                for (int i = 0; i < n; i++)
                {
                    var go = new GameObject("Kupu"); go.transform.SetParent(flyRoot, false);
                    go.AddComponent<MeshFilter>().sharedMesh = flyMesh;
                    var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
                    var f = new Fly { t = go.transform, r = mr, mpb = new MaterialPropertyBlock(), ph = (float)rnd.NextDouble() * 6.28f, spd = 0.5f + (float)rnd.NextDouble() * 0.5f, rad = 2.5f + (float)rnd.NextDouble() * 4f, alt = 0.9f + (float)rnd.NextDouble() * 1.4f, flap = 14f + (float)rnd.NextDouble() * 6f };
                    Color c = FlowerCols[rnd.Next(FlowerCols.Length)];
                    mr.GetPropertyBlock(f.mpb); f.mpb.SetColor("_Color", c); f.mpb.SetFloat("_Sway", 0f); mr.SetPropertyBlock(f.mpb);
                    f.home = pp + new Vector3((float)(rnd.NextDouble() - 0.5) * 24f, 0f, (float)(rnd.NextDouble() - 0.5) * 24f);
                    flies.Add(f);
                }
            }
            SetFlies(true);
            float t = Time.time;
            foreach (var f in flies)
            {
                Vector3 d = f.home - pp; d.y = 0f;
                if (d.magnitude > 26f) f.home = pp + new Vector3(Random.Range(-14f, 14f), 0f, Random.Range(-14f, 14f)) + player.forward * 8f;
                f.home += new Vector3(Mathf.Sin(t * 0.2f + f.ph), 0f, Mathf.Cos(t * 0.17f + f.ph * 1.3f)) * Time.deltaTime * 0.9f;
                Vector3 pos = f.home + new Vector3(Mathf.Sin(t * f.spd + f.ph) * f.rad, 0f, Mathf.Cos(t * f.spd * 0.83f + f.ph) * f.rad);
                float gy = pp.y;
                RaycastHit hit;
                if (Physics.Raycast(new Vector3(pos.x, pp.y + 6f, pos.z), Vector3.down, out hit, 30f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) gy = hit.point.y;
                pos.y = gy + f.alt + Mathf.Sin(t * 1.7f + f.ph * 2f) * 0.35f;
                Vector3 dir = pos - f.t.position; dir.y = 0f;
                if (dir.sqrMagnitude > 0.0004f) f.t.rotation = Quaternion.Slerp(f.t.rotation, Quaternion.LookRotation(dir.normalized, Vector3.up) * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.3f + f.ph) * 12f), 6f * Time.deltaTime);
                f.t.position = pos;
                float fl = Mathf.Abs(Mathf.Cos(t * f.flap + f.ph));
                f.t.localScale = new Vector3(0.35f + 0.65f * fl, 1f, 1f) * 1.5f;
            }
        }
    }
}
