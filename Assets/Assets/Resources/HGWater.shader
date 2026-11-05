// Air bergaya anime fantasy: gelombang lembut berlapis (riak halus memudar dengan jarak => tidak ada moire/kotak-kotak),
// warna dangkal-dalam cerah, pantulan langit lembut, garis riak putih ala anime, kilau matahari berpita.
// Berjalan di URP/Built-in (unlit + pencahayaan sendiri). Nilai langit/matahari dikirim global oleh RealWater.cs.
Shader "HunterGuild/Water"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _Shallow ("Warna dangkal", Color) = (0.16,0.46,0.5,1)
        _Deep ("Warna dalam", Color) = (0.02,0.13,0.2,1)
        _Alpha ("Kejernihan (opasitas dasar)", Range(0,1)) = 0.78
        _Scale ("Skala gelombang", Float) = 1
        _Speed ("Kecepatan", Float) = 1
        _Bump ("Kekuatan riak", Float) = 0.5
        _Gloss ("Ketajaman kilau", Float) = 380
        _Reflect ("Pantulan langit", Range(0,1)) = 0.85
        _SkyTint ("Netralkan warna langit ke biru", Range(0,1)) = 0
        _Jet ("Pancuran: permukaan tegak putih kebiruan & tembus pandang", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent-10" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            fixed4 _Color, _Shallow, _Deep;
            float _Alpha, _Scale, _Speed, _Bump, _Gloss, _Reflect, _SkyTint, _Jet;
            float4 _HGSunDir, _HGSunCol, _HGSkyTop, _HGSkyHor;   // global: arah matahari (xyz), warna matahari, langit atas, langit cakrawala (w = terang siang 0..1)
            float _HGStorm;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 wp : TEXCOORD0; float3 wn : TEXCOORD1; UNITY_FOG_COORDS(2) };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.wn = UnityObjectToWorldNormal(v.normal);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            // satu lapis gelombang; w = bobot (memudar dengan jarak supaya frekuensi tinggi tidak beralias menjadi pola kotak)
            void W(float2 p, float2 d, float f, float s, float a, float t, float w, inout float2 g)
            {
                float ph = dot(p, d) * f + t * s;
                g += d * (cos(ph) * a * f * w);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Time.y * _Speed;
                float3 V = _WorldSpaceCameraPos - i.wp;
                float dist = length(V);
                V = V / max(dist, 0.001);
                float wMid = saturate(1.0 - (dist - 25.0) / 90.0);     // riak sedang memudar 25..115 m
                float wHi  = saturate(1.0 - (dist - 8.0) / 42.0);      // riak halus memudar 8..50 m

                float2 p = i.wp.xz * _Scale;
                p += sin(p.yx * 0.31 + t * 0.21) * 0.9;           // lengkungkan supaya pola tidak terlihat berulang
                float2 g = float2(0, 0);
                W(p, float2( 0.98, 0.20), 0.62, 1.10, 0.50, t, 1.0, g);
                W(p, float2(-0.60, 0.80), 1.10, 1.45, 0.34, t, 1.0, g);
                W(p, float2( 0.30,-0.95), 1.90, 1.90, 0.24, t, lerp(0.4, 1.0, wMid), g);
                W(p, float2(-0.91,-0.41), 3.10, 2.40, 0.15, t, wMid, g);
                W(p, float2( 0.71, 0.71), 5.30, 3.10, 0.09, t, wHi, g);
                W(p, float2(-0.20, 0.98), 8.70, 3.90, 0.055, t, wHi * wHi, g);
                float bump = _Bump * (1.0 + _HGStorm * 1.6) * lerp(0.55, 1.0, wMid);

                float3 up = float3(0, 1, 0);
                float3 N = normalize(float3(-g.x * bump, 1.0, -g.y * bump));
                float3 wn = normalize(i.wn); if (dot(wn, V) < 0) wn = -wn;
                float steep = _Jet * (1.0 - smoothstep(0.30, 0.80, abs(wn.y)));   // 1 pada permukaan tegak (tirai/pancuran), 0 pada permukaan datar
                N = normalize(N + (wn - up));                    // benda tegak (pancuran) tetap mengikuti bentuk mesh

                float ndv = saturate(dot(N, V));
                float fres = saturate(0.03 + 0.97 * pow(1.0 - ndv, 4.5));
                float3 R = reflect(-V, N); R.y = abs(R.y);

                float day = max(_HGSkyHor.w, 0.55);               // air tetap cerah saat fajar/senja; malam diredam oleh warna langit
                float3 sky = lerp(_HGSkyHor.rgb, _HGSkyTop.rgb, pow(saturate(R.y), 0.55));
                float lum = dot(sky, float3(0.3, 0.55, 0.15));
                sky = lerp(sky, float3(0.45, 0.72, 1.0) * saturate(lum * 1.4 + 0.15), _SkyTint);
                float3 L = normalize(_HGSunDir.xyz);
                float sd = saturate(dot(R, L));
                // kilau berpita (cel): bintik terang tegas + halo lembut, tidak ada titik-titik tajam sub-piksel
                float spBand = smoothstep(0.90, 0.96, pow(sd, _Gloss * 0.045)) * 0.9 * wMid;
                float3 spec = _HGSunCol.rgb * (spBand + pow(sd, _Gloss * 0.12) * 0.30) * _HGSkyHor.w;

                float3 body = lerp(_Deep.rgb, _Shallow.rgb, pow(ndv, 1.4)) * _Color.rgb;
                body *= (0.55 + 0.45 * day) * (0.7 + 0.3 * _HGSkyHor.rgb);
                body = lerp(body, body * float3(0.85, 1.05, 1.1), 0.5);                    // sedikit lebih biru kehijauan
                body = lerp(body, float3(0.80, 0.94, 1.0) * (0.75 + 0.25 * day), steep * 0.8);   // pancuran: putih kebiruan
                // cahaya menembus puncak gelombang
                float sss = pow(saturate(dot(V, -L) * 0.5 + 0.5), 4.0) * saturate(g.x * g.x + g.y * g.y) * 0.16 * day;
                body += _Shallow.rgb * sss;

                // garis riak putih ala anime (dua pola silang, tepi tegas)
                float w1 = sin(dot(p, float2(0.92, 0.38)) * 1.6 + g.x * 2.2 + t * 0.7);
                float w2 = sin(dot(p, float2(-0.38, 0.92)) * 1.15 + g.y * 2.2 - t * 0.5);
                float streak = smoothstep(0.55, 0.62, w1 * w2) * wMid * 0.38;

                float3 col = lerp(body, sky, fres * _Reflect) + spec + streak * float3(0.9, 1.0, 1.0) * (0.5 + 0.5 * day);
                float a = saturate(lerp(_Alpha, 1.0, fres) + dot(spec, float3(0.33, 0.33, 0.33)) + streak * 0.35);
                a = lerp(a, a * 0.6, steep);
                fixed4 c = fixed4(col, a);
                UNITY_APPLY_FOG(i.fogCoord, c);
                return c;
            }
            ENDCG
        }
    }
}
