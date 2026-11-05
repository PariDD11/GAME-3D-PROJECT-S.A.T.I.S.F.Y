using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Reflection;

/// <summary>
/// Menyetel aset URP aktif (bayangan lunak, cascade, HDR, MSAA, depth/opaque texture) dan memasang SSAO (Screen Space Ambient Occlusion)
/// pada renderer. Semua lewat try/catch: bila properti berbeda antar versi URP, langkah itu dilewati tanpa merusak build.
/// </summary>
public static class GraphicsSetup
{
    [MenuItem("Hunter Guild/Grafis/Pasang look sinematik (URP + SSAO)", priority = 5)]
    public static void Run() { Apply(true); }

    public static void Apply(bool verbose)
    {
        int ok = 0;
        try
        {
            var rp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (rp == null) { if (verbose) Debug.LogWarning("[Grafis] URP asset aktif tidak ditemukan."); return; }
            Undo.RecordObject(rp, "Graphics setup");
            try { rp.msaaSampleCount = 4; ok++; } catch { }
            try { rp.supportsHDR = true; ok++; } catch { }
            try { rp.shadowDistance = 90f; ok++; } catch { }
            try { rp.shadowCascadeCount = 4; ok++; } catch { }
            try { rp.supportsCameraDepthTexture = true; rp.supportsCameraOpaqueTexture = true; ok++; } catch { }
            var so = new SerializedObject(rp);
            SetBool(so, "m_SoftShadowsSupported", true);
            SetInt(so, "m_SoftShadowQuality", 2);
            SetInt(so, "m_MainLightShadowmapResolution", 4096);
            SetInt(so, "m_AdditionalLightsShadowmapResolution", 2048);
            SetFloat(so, "m_CascadeBorder", 0.2f);
            SetFloat(so, "m_ShadowDepthBias", 1.0f);
            SetFloat(so, "m_ShadowNormalBias", 1.0f);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(rp);
            ok += AddSsao(rp);
            TuneSsao(rp);
            AssetDatabase.SaveAssets();
        }
        catch (System.Exception e) { Debug.LogWarning("[Grafis] dilewati: " + e.Message); }
        if (verbose) Debug.Log("[Grafis] Look sinematik terpasang (" + ok + " langkah).");
    }

    static void SetBool(SerializedObject so, string n, bool v) { var p = so.FindProperty(n); if (p != null) p.boolValue = v; }
    static void SetInt(SerializedObject so, string n, int v) { var p = so.FindProperty(n); if (p != null) p.intValue = v; }
    static void SetFloat(SerializedObject so, string n, float v) { var p = so.FindProperty(n); if (p != null) p.floatValue = v; }

    /// <summary>Atur SSAO lebih tegas (intensitas, radius, sampel) lewat SerializedObject pada feature yang ada.</summary>
    static void TuneSsao(UniversalRenderPipelineAsset rp)
    {
        try
        {
            var rpso = new SerializedObject(rp);
            var list = rpso.FindProperty("m_RendererDataList");
            if (list == null || list.arraySize == 0) return;
            var rd = list.GetArrayElementAtIndex(0).objectReferenceValue as ScriptableRendererData;
            if (rd == null) return;
            foreach (var f in rd.rendererFeatures)
            {
                if (f == null || f.GetType().Name != "ScreenSpaceAmbientOcclusion") continue;
                var so = new SerializedObject(f);
                var st = so.FindProperty("m_Settings");
                if (st == null) continue;
                var a = st.FindPropertyRelative("Intensity"); if (a != null) a.floatValue = 1.1f;
                var r = st.FindPropertyRelative("Radius"); if (r != null) r.floatValue = 0.45f;
                var d = st.FindPropertyRelative("DirectLightingStrength"); if (d != null) d.floatValue = 0.2f;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(f);
            }
        }
        catch { }
    }

    /// <summary>Tambahkan ScreenSpaceAmbientOcclusion (internal URP) ke daftar renderer feature lewat refleksi; hindari duplikat.</summary>
    static int AddSsao(UniversalRenderPipelineAsset rp)
    {
        try
        {
            var rpso = new SerializedObject(rp);
            var list = rpso.FindProperty("m_RendererDataList");
            if (list == null || list.arraySize == 0) return 0;
            var rd = list.GetArrayElementAtIndex(0).objectReferenceValue as ScriptableRendererData;
            if (rd == null) return 0;
            foreach (var f in rd.rendererFeatures) if (f != null && f.GetType().Name == "ScreenSpaceAmbientOcclusion") return 0;
            System.Type t = null;
            foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                t = a.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion");
                if (t != null) break;
            }
            if (t == null) return 0;
            var feat = ScriptableObject.CreateInstance(t) as ScriptableRendererFeature;
            if (feat == null) return 0;
            feat.name = "SSAO";
            AssetDatabase.AddObjectToAsset(feat, rd);
            var rso = new SerializedObject(rd);
            var feats = rso.FindProperty("m_RendererFeatures");
            var map = rso.FindProperty("m_RendererFeatureMap");
            if (feats == null || map == null) return 0;
            feats.arraySize++; feats.GetArrayElementAtIndex(feats.arraySize - 1).objectReferenceValue = feat;
            string guid; long fid;
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feat, out guid, out fid))
            { map.arraySize++; map.GetArrayElementAtIndex(map.arraySize - 1).longValue = fid; }
            rso.ApplyModifiedProperties();
            EditorUtility.SetDirty(rd);
            return 1;
        }
        catch (System.Exception e) { Debug.LogWarning("[Grafis] SSAO dilewati: " + e.Message); return 0; }
    }
}
