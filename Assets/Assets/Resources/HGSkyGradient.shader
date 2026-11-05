// Langit gradasi bergaya anime: biru jernih di atas, cakrawala pucat hangat, tanpa piringan matahari (matahari digambar SkyLife).
// Warna diatur dari FantasyGrade.cs sepanjang hari.
Shader "HunterGuild/SkyGradient"
{
    Properties
    {
        _Top ("Atas", Color) = (0.28,0.56,0.98,1)
        _Mid ("Tengah", Color) = (0.55,0.78,1,1)
        _Hor ("Cakrawala", Color) = (0.82,0.92,1,1)
        _Bot ("Bawah", Color) = (0.6,0.72,0.86,1)
        _Glow ("Cahaya matahari", Color) = (1,0.8,0.55,0)
        _SunDirX ("Arah matahari", Vector) = (0,0,1,0)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Top, _Mid, _Hor, _Bot, _Glow;
            float4 _SunDirX;
            struct a2v { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };
            v2f vert (a2v v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.dir = v.vertex.xyz; return o; }
            fixed4 frag (v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = d.y;
                float3 c;
                if (h >= 0.0)
                {
                    float a = saturate(h);
                    c = lerp(_Hor.rgb, _Mid.rgb, smoothstep(0.0, 0.22, a));
                    c = lerp(c, _Top.rgb, smoothstep(0.18, 0.85, a));
                }
                else c = lerp(_Hor.rgb, _Bot.rgb, smoothstep(0.0, 0.35, -h));
                // halo hangat di sekitar arah matahari (lembut, bukan piringan)
                float sd = saturate(dot(d, normalize(_SunDirX.xyz)));
                c += _Glow.rgb * (pow(sd, 6.0) * 0.55 + pow(sd, 40.0) * 0.5) * _Glow.a;
                return fixed4(c, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
