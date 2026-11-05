// Unlit berwarna-vertex tanpa kabut, opak, untuk langit dan pemandangan jauh (gunung api, pegunungan, awan badai).
Shader "HunterGuild/FarUnlit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Flash ("Flash", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Cull Off
        ZWrite On
        ZTest LEqual
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata_t { float4 vertex : POSITION; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; };
            fixed4 _Color; fixed4 _Flash;
            v2f vert (appdata_t v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.color = v.color * _Color; return o; }
            fixed4 frag (v2f i) : SV_Target { return fixed4(i.color.rgb + _Flash.rgb, 1); }
            ENDCG
        }
    }
}
