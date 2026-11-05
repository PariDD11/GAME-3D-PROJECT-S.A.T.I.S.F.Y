// Efek pertempuran/sihir: aditif berintensitas HDR (menyala, berwarna cerah), mendukung warna vertex partikel.
// Mode alfa biasa tersedia lewat _Src/_Dst (SrcAlpha/OneMinusSrcAlpha) untuk asap/debu.
Shader "HunterGuild/Glow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Intensity ("HDR Intensity", Float) = 1.6
        [Enum(UnityEngine.Rendering.BlendMode)] _Src ("Src", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _Dst ("Dst", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent+5" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Blend [_Src] [_Dst]
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color; float _Intensity;
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.color = v.color * _Color; o.uv = TRANSFORM_TEX(v.uv, _MainTex); return o; }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 t = tex2D(_MainTex, i.uv);
                float4 c = float4(t.rgb * i.color.rgb, t.a * i.color.a);
                c.rgb *= _Intensity;
                return c;
            }
            ENDCG
        }
    }
}
