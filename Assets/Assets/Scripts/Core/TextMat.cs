using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Material untuk TextMesh 3D dengan uji kedalaman. Material bawaan font menggambar di atas segalanya (ZTest Always),
    /// sehingga tulisan sisi belakang papan tembus ke depan dan menumpuk dengan tulisan sisi depan.
    /// </summary>
    public static class TextMat
    {
        static Material mat;

        public static Material Get()
        {
            if (mat != null) return mat;
            var sh = Resources.Load<Shader>("Text3D");
            if (sh == null) return null;
#if UNITY_EDITOR
            const string path = "Assets/Materials/Text3D.mat";
            if (UnityEditor.AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                var m = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null || m.shader != sh)
                {
                    m = new Material(sh);
                    m.name = "Text3D";
                    UnityEditor.AssetDatabase.CreateAsset(m, path);
                }
                mat = m;
                return mat;
            }
#endif
            mat = new Material(sh);
            mat.name = "Text3D";
            return mat;
        }

        /// <summary>Pasang material berdepth ke renderer TextMesh; jika shader tidak ada, pakai material bawaan font.</summary>
        public static void Apply(MeshRenderer r, Material fallback)
        {
            if (r == null) return;
            var m = Get();
            r.sharedMaterial = m != null ? m : fallback;
            if (m != null)
            {
                var b = r.GetComponent<TextMatBinder>();
                if (b == null) b = r.gameObject.AddComponent<TextMatBinder>();
                b.Bind();
            }
        }
    }
}
