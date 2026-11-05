// Cel-shading bergaya anime untuk karakter dan monster: 2 tingkat bayangan, pita highlight, rim terang.
// Arah/warna cahaya diambil dari matahari lewat variabel global (diisi AnimeToon.cs), sehingga ikut siang/malam.
Shader "HunterGuild/Toon"
{
    Properties
    {
        [MainTexture] _BaseMap ("Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Color", Color) = (1,1,1,1)
        _ShadeColor ("Shade tint", Color) = (0.46,0.48,0.68,1)
        _Threshold ("Light threshold", Range(-1,1)) = 0.05
        _Softness ("Edge softness", Range(0.001,0.3)) = 0.035
        _Spec ("Highlight band", Range(0,1)) = 0.28
        _Rim ("Rim strength", Range(0,2)) = 0.7
        _RimColor ("Rim color", Color) = (0.65,0.8,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "Toon"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _BaseMap; float4 _BaseMap_ST;
            float4 _BaseColor, _ShadeColor, _RimColor;
            float _Threshold, _Softness, _Spec, _Rim;
            float4 _HGLightDir, _HGLightCol, _HGAmbient;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 nrm : TEXCOORD1; float3 view : TEXCOORD2; };
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.nrm = UnityObjectToWorldNormal(v.normal);
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.view = normalize(_WorldSpaceCameraPos - wp);
                return o;
            }
            float4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.nrm);
                float3 L = normalize(-_HGLightDir.xyz + float3(0, 0.0001, 0));
                float ndl = dot(n, L);
                float lit = smoothstep(_Threshold - _Softness, _Threshold + _Softness, ndl);
                float hi = smoothstep(0.62 - _Softness, 0.62 + _Softness, ndl);
                float4 tex = tex2D(_BaseMap, i.uv) * _BaseColor;
                // kain hitam pekat -> biru malam sangat gelap: lipatan & bentuk tetap terbaca (tidak menjadi siluet datar)
                float lumA = dot(tex.rgb, float3(0.3, 0.59, 0.11));
                tex.rgb += (1.0 - saturate(lumA * 5.0)) * float3(0.040, 0.046, 0.072);
                float3 amb = _HGAmbient.rgb * 0.55 + 0.45;
                float3 shade = tex.rgb * _ShadeColor.rgb * amb;
                float3 lite = tex.rgb * _HGLightCol.rgb;
                float3 col = lerp(shade, lite, lit);
                col += tex.rgb * _HGLightCol.rgb * hi * _Spec;
                float rim = pow(1.0 - saturate(dot(n, i.view)), 3.0) * smoothstep(-0.25, 0.35, ndl);
                col += _RimColor.rgb * step(0.45, rim) * _Rim * 0.45;
                col = max(col, float3(0.020, 0.022, 0.036));                  // tidak ada hitam murni (tinta biru malam)
                return float4(col, tex.a);
            }
            ENDCG
        }
        UsePass "Universal Render Pipeline/Lit/SHADOWCASTER"
        UsePass "Universal Render Pipeline/Lit/DEPTHONLY"
    }
    Fallback Off
}
