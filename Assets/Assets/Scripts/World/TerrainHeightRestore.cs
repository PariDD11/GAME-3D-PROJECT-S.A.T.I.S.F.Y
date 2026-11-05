using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Pengaman tinggi terrain: tinggi asli disimpan di file .heights.bytes saat build. Bila TerrainData yang termuat ternyata datar
    /// (tinggi tidak tersimpan di asset), tinggi dipulihkan dari file ini, termasuk collider. Tanpanya semua prop melayang dan
    /// pemain jatuh ke dataran datar di bawah lava.
    /// </summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public class TerrainHeightRestore : MonoBehaviour
    {
        public TextAsset heights;
        public int res = 513;
        public Vector3 size = new Vector3(1000f, 80f, 1000f);
        public static string LastInfo = "";

        void Awake() { Apply(); }
        void OnEnable() { Apply(); }

        public void Apply()
        {
            var t = GetComponent<Terrain>();
            if (t == null || heights == null || t.terrainData == null) return;
            var td = t.terrainData;
            byte[] b = heights.bytes;
            int n = res;
            if (b == null || b.Length < n * n * 2) return;
            bool bad = td.heightmapResolution != n || Mathf.Abs(td.size.y - size.y) > 0.5f;
            if (!bad)
            {
                int[] probe = { n / 2, n / 4, (3 * n) / 4, n / 8, (7 * n) / 8 };
                foreach (int z in probe)
                    foreach (int x in probe)
                    {
                        float want = Read(b, n, x, z) * size.y;
                        if (Mathf.Abs(td.GetHeight(x, z) - want) > 0.6f) { bad = true; break; }
                    }
            }
            if (!bad) { LastInfo = "tinggi terrain OK"; return; }
            if (td.heightmapResolution != n) td.heightmapResolution = n;
            td.size = size;
            var hs = new float[n, n];
            for (int z = 0; z < n; z++)
                for (int x = 0; x < n; x++) hs[z, x] = Read(b, n, x, z);
            td.SetHeights(0, 0, hs);
            foreach (var tc in GetComponents<Collider>())
                if (tc.GetType().Name == "TerrainCollider") { tc.enabled = false; tc.enabled = true; }
            LastInfo = "tinggi terrain DIPULIHKAN dari file";
            Debug.LogWarning("[HunterGuild] Tinggi terrain di asset ternyata datar; dipulihkan dari file (" + name + ").");
        }

        static float Read(byte[] b, int n, int x, int z)
        {
            int i = 2 * (z * n + x);
            return (b[i] | (b[i + 1] << 8)) / 65535f;
        }
    }
}
