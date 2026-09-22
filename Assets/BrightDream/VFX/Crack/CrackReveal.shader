Shader "Dream1/CrackReveal"
{
    Properties
    {
        _GlowTex      ("Glow (RGB)", 2D) = "black" {}
        _GrowthTex    ("Growth Mask (R)", 2D) = "black" {}
        _VoidTex      ("Void / Base (RGB)", 2D) = "black" {}
        _VoidOpacity  ("Void Opacity", Range(0,1)) = 0.9
        _Progress     ("Progress", Range(0,1)) = 0
        _EdgeSoft     ("Edge Softness", Range(0.01,0.4)) = 0.10
        _TipBoost     ("Tip Glow", Range(0,4)) = 1.6
        _Shimmer      ("Shimmer Strength", Range(0,0.06)) = 0.012
        _ShimmerSpeed ("Shimmer Speed", Range(0,4)) = 1.0
        _Intensity    ("Intensity", Range(0,4)) = 1.4
        _Tint         ("Tint", Color) = (1,1,1,1)
        _Desat        ("Desaturate before tint", Range(0,1)) = 0
        _EdgeFade     ("Border Fade", Range(0.01,1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }


        // ── Pass 1: 어두운 구멍. 밝은 배경을 가려 발광이 읽히게 한다 ──
        Pass
        {
            Name "CrackVoid"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Lighting Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _VoidTex;   float4 _VoidTex_ST;
            sampler2D _GrowthTex;
            float _Progress, _EdgeSoft, _EdgeFade, _VoidOpacity;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _VoidTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float  growth = tex2D(_GrowthTex, i.uv).r;
                fixed3 vcol   = tex2D(_VoidTex,   i.uv).rgb;

                float reveal = saturate((_Progress - growth) / _EdgeSoft + 0.5);

                // 구멍의 진하기 = 그 픽셀이 얼마나 어두운가
                float lum  = max(max(vcol.r, vcol.g), vcol.b);
                float dark = saturate(1.0 - lum * 4.2);
                dark = pow(dark, 1.5);                    // 표면 잔여만 깎고 구멍은 살린다

                float2 c = abs(i.uv - 0.5) * 2.0;
                float  vig = saturate((1.05 - length(c)) / _EdgeFade);

                float a = dark * reveal * vig * _VoidOpacity;
                return fixed4(vcol * 0.12, a);
            }
            ENDCG
        }

        // ── Pass 2: 보라 발광 (가산) ──
        Pass
        {
            Name "CrackAdditive"
            Blend One One
            ZWrite Off
            Cull Off
            Lighting Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _GlowTex;   float4 _GlowTex_ST;
            sampler2D _GrowthTex;
            float  _Progress, _EdgeSoft, _TipBoost;
            float  _Shimmer, _ShimmerSpeed, _Intensity, _EdgeFade, _Desat;
            fixed4 _Tint;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _GlowTex);
                return o;
            }

            // 값싼 2옥타브 노이즈 — 일렁임용
            float n2(float2 p)
            {
                return sin(p.x*7.13 + p.y*3.71) * 0.50
                     + sin(p.x*3.29 - p.y*6.47) * 0.35
                     + sin((p.x+p.y)*11.7)      * 0.15;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Time.y * _ShimmerSpeed;
                float2 uv = i.uv;

                // 일렁임 — 성장이 끝난 뒤에만 켠다
                float settle = saturate((_Progress - 0.75) / 0.25);
                float2 w = float2(n2(uv*3.1 + t*0.31), n2(uv*2.7 - t*0.24));
                uv += w * _Shimmer * settle;

                float  growth = tex2D(_GrowthTex, uv).r;
                fixed3 glow   = tex2D(_GlowTex,   uv).rgb;

                // 성장: 임계값 이하만 드러난다
                float reveal = saturate((_Progress - growth) / _EdgeSoft + 0.5);

                // 선단광: 번지는 경계에서만 밝게
                float d   = (growth - _Progress) / max(_EdgeSoft * 0.9, 1e-4);
                float tip = exp(-d*d) * (1.0 - _Progress) * _TipBoost;

                // 쿼드 경계 페이드 — 사각 테두리를 숨긴다
                float2 c = abs(i.uv - 0.5) * 2.0;
                float  r = length(c);
                float  vig = saturate((1.05 - r) / _EdgeFade);

                // 착색이 실제로 먹도록 먼저 탈색한다
                float  gl  = dot(glow, float3(0.299, 0.587, 0.114));
                fixed3 src = lerp(glow, fixed3(gl, gl, gl), _Desat);
                fixed3 col = src * (reveal + tip) * vig * _Intensity * _Tint.rgb;
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
