using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Material Text3D tidak otomatis memakai atlas font (hasilnya kotak putih). Komponen ini menyalin tekstur atlas font
    /// ke renderer lewat MaterialPropertyBlock dan memperbaruinya saat atlas dibangun ulang.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshRenderer))]
    public class TextMatBinder : MonoBehaviour
    {
        MeshRenderer r; TextMesh tm; MaterialPropertyBlock pb; Texture last;

        void OnEnable() { Bind(); }

        public void Bind()
        {
            if (r == null) r = GetComponent<MeshRenderer>();
            if (tm == null) tm = GetComponent<TextMesh>();
            if (pb == null) pb = new MaterialPropertyBlock();
            Refresh(true);
        }

        void Refresh(bool force)
        {
            if (r == null || tm == null || tm.font == null) return;
            var tex = tm.font.material != null ? tm.font.material.mainTexture : null;
            if (tex == null || (!force && tex == last)) return;
            last = tex;
            r.GetPropertyBlock(pb);
            pb.SetTexture("_MainTex", tex);
            r.SetPropertyBlock(pb);
        }

        void LateUpdate() { Refresh(false); }
    }
}
