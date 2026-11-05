// Dedaunan/bunga/semak/jamur bergaya anime untuk WorldDetail.cs (GPU instancing). Warna dasar dari vertex color
// (alpha = bagian yang diwarnai ulang oleh _Color per-instans, mis. kelopak), pencahayaan lembut ala cel, goyang angin di ujung,
// tumbuh perlahan di tepi jarak pandang. Dipakai juga oleh MeshRenderer biasa (kupu-kupu).
Shader "HunterGuild/Foliage"
{
    Properties
    {
        _Color ("Warna", Color) = (1,1,1,1)
        _Sway ("Goyang angin", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            float _Sway;
            float hash21(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            float vnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p); f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash21(i), hash21(i + float2(1, 0)), f.x), lerp(hash21(i + float2(0, 1)), hash21(i + float2(1, 1)), f.x), f.y);
            }
            float4 _HGFolDir, _HGFolCol, _HGFolAmb;     // global dari WorldDetail: arah cahaya, warna cahaya, ambient
            float _HGFolFade;                            // jarak pandang maksimum; 0 = tanpa pemudaran
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; float3 nrm : TEXCOORD0; fixed4 col : COLOR; UNITY_FOG_COORDS(1) float h : TEXCOORD2; UNITY_VERTEX_INPUT_INSTANCE_ID };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                float3 org = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                if (_HGFolFade > 0.0)
                {
                    float d = distance(org, _WorldSpaceCameraPos);
                    v.vertex.xyz *= smoothstep(0.0, 1.0, saturate((_HGFolFade - d) / 26.0));
                }
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                float hf = v.uv.x;                                       // 0 akar .. 1 ujung
                float t = _Time.y;
                // gelombang angin besar bergulir melintasi padang + riak halus
                float sw = sin(t * 1.25 + org.x * 0.16 + org.z * 0.11) * 0.6 + sin(t * 2.3 + org.x * 0.7 - org.z * 0.55) * 0.22 + sin(t * 0.7 + org.z * 0.05) * 0.2;
                wp.x += sw * hf * hf * 0.11 * _Sway;
                wp.z += sw * hf * hf * 0.06 * _Sway;
                o.pos = UnityWorldToClipPos(float4(wp, 1.0));
                o.nrm = UnityObjectToWorldNormal(v.normal);
                fixed4 inst = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                fixed3 tint = lerp(fixed3(1, 1, 1), inst.rgb, v.color.a);
                // variasi warna berbasis dunia: bercak kuning-hijau <-> hijau kebiruan + riak halus, supaya padang tidak tampak berulang
                float n1 = vnoise(org.xz * 0.085), n2 = vnoise(org.xz * 0.7 + 17.0);
                float3 vari = lerp(float3(1.1, 1.02, 0.74), float3(0.8, 0.96, 0.9), n1) * (0.88 + 0.24 * n2);
                float isPlant = step(v.color.a, 0.99);
                tint *= lerp(fixed3(1, 1, 1), vari, 0.75 * isPlant);
                float ao = lerp(0.55, 1.0, saturate(hf * 1.7));          // akar lebih gelap (oklusi)
                o.col = fixed4(v.color.rgb * tint * lerp(1.0, ao, isPlant), 1);
                o.h = hf;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.nrm);
                float3 L = normalize(-_HGFolDir.xyz + float3(0, 0.0001, 0));
                float ndl = dot(n, L) * 0.5 + 0.5;                      // pembungkus lembut: sisi gelap tidak hitam
                float lit = smoothstep(0.05, 0.95, ndl);                // peralihan lembut (tidak kontras keras)
                float3 amb = _HGFolAmb.rgb * 0.75 + 0.25;
                float3 shade = i.col.rgb * amb * float3(0.78, 0.82, 1.0);
                float3 sun = i.col.rgb * (_HGFolCol.rgb * 0.85 + amb * 0.35);
                float3 c = lerp(shade, sun, lit);
                c += i.col.rgb * i.h * 0.025;                             // ujung sedikit bercahaya (tembus cahaya)
                fixed4 r = fixed4(c, 1);
                UNITY_APPLY_FOG(i.fogCoord, r);
                return r;
            }
            ENDCG
        }
    }
    Fallback Off
}
