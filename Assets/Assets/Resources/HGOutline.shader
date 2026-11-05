// Garis tepi anime (inverted hull): salinan mesh yang sisi depannya dibuang dan digeser sedikit mengikuti normal.
Shader "HunterGuild/ToonOutline"
{
    Properties
    {
        _OutlineColor ("Outline color", Color) = (0.02,0.02,0.05,1)
        _Width ("Width (m)", Float) = 0.012
        _Blend ("Arah dari pusat (0=normal, 1=dari pusat)", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }
        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _OutlineColor; float _Width, _Blend;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; };
            v2f vert (appdata v)
            {
                v2f o;
                float3 dirV = normalize(v.vertex.xyz + float3(0, 0.00001, 0));
                float3 d = normalize(lerp(v.normal, dirV, _Blend));
                float3 dW = normalize(mul((float3x3)unity_ObjectToWorld, d));
                float4 pw = mul(unity_ObjectToWorld, v.vertex);
                float dist = distance(_WorldSpaceCameraPos, pw.xyz);
                pw.xyz += dW * _Width * clamp(dist * 0.12, 0.6, 3.0);
                o.pos = mul(UNITY_MATRIX_VP, pw);
                return o;
            }
            float4 frag (v2f i) : SV_Target { return _OutlineColor; }
            ENDCG
        }
    }
    Fallback Off
}
