Shader "Dream1/RiftVoidStencil"
{
    Properties
    {
        [Header(Base)]
        _NearColor   ("Near Color", Color) = (0.16,0.10,0.30,1)
        _FarColor    ("Far Color",  Color) = (0.01,0.00,0.03,1)
        _Swirl       ("Swirl Speed", Range(0,3)) = 0.35
        _Twist       ("Twist Amount", Range(0,4)) = 1.2

        [Header(Depth Layers)]
        _StarTint    ("Star Tint", Color) = (0.85,0.80,1.00,1)
        _StarAmount  ("Star Amount", Range(0,3)) = 1.0
        _StarDensity ("Star Density", Range(2,40)) = 14
        _Parallax    ("Parallax Spread", Range(0,3)) = 1.0

        [Header(Nebula)]
        _NebulaTint  ("Nebula Tint", Color) = (0.55,0.30,0.95,1)
        _NebulaAmount("Nebula Amount", Range(0,3)) = 1.0
        _NebulaScale ("Nebula Scale", Range(0.5,8)) = 2.5

        [Header(Particles)]
        _DustTint    ("Dust Tint", Color) = (0.90,0.75,1.00,1)
        _DustAmount  ("Dust Amount", Range(0,3)) = 1.0
        _DustSpeed   ("Dust Speed", Range(0,4)) = 1.3

        [Header(Rim)]
        _RimColor    ("Rim Glow Color", Color) = (0.70,0.45,1.00,1)
        _RimAmount   ("Rim Glow Amount", Range(0,4)) = 1.4
        _RimWidth    ("Rim Glow Width", Range(0.02,0.6)) = 0.22
    }

    SubShader
    {
        // 스텐실이 7 인 곳 = 구멍 안쪽에서만 그린다. 옆에서 보면 사라진다.
        Tags { "RenderType"="Opaque" "Queue"="Geometry-1" }
        Pass
        {
            Name "VoidThroughHole"
            ZWrite On
            Cull Off
            Stencil { Ref 7  Comp Equal  Pass Keep }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _NearColor, _FarColor, _StarTint, _NebulaTint, _DustTint, _RimColor;
            float  _Swirl, _Twist;
            float  _StarAmount, _StarDensity, _Parallax;
            float  _NebulaAmount, _NebulaScale;
            float  _DustAmount, _DustSpeed;
            float  _RimAmount, _RimWidth;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float depth : TEXCOORD1; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = v.uv;
                o.depth = saturate(v.vertex.z * 0.8 + 0.25);
                return o;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f*f*(3.0-2.0*f);
                float a = hash21(i);
                float b = hash21(i + float2(1,0));
                float c = hash21(i + float2(0,1));
                float d = hash21(i + float2(1,1));
                return lerp(lerp(a,b,f.x), lerp(c,d,f.x), f.y);
            }

            float fbm(float2 p)
            {
                float s = 0.0, amp = 0.5;
                for (int k = 0; k < 4; k++) { s += vnoise(p)*amp; p *= 2.03; amp *= 0.5; }
                return s;
            }

            // 격자 칸마다 점 하나. 로그-폴라 위에서 쓰면 별밭이 된다.
            float starfield(float2 p, float sharp)
            {
                float2 i = floor(p), f = frac(p);
                float h = hash21(i);
                if (h < 0.55) return 0.0;                       // 칸의 절반 정도만 별
                float2 c = float2(hash21(i + 3.7), hash21(i + 8.1));
                float d = length(f - c);
                float b = frac(h * 91.7);                       // 밝기 편차
                return pow(saturate(1.0 - d*sharp), 9.0) * (0.35 + b*0.65);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Time.y;
                float2 c = i.uv - 0.5;
                float  r = length(c) + 1e-5;
                float  a = atan2(c.y, c.x);

                float rn = saturate(r / 0.28);                  // 0=중심 1=구멍 가장자리
                float inv = 1.0 / max(r, 0.022);                // 안쪽으로 무한히 뻗는 축

                // 반지름에 따라 각을 비틀어 소용돌이를 만든다
                float aw = a + inv * _Twist * 0.16 - t * _Swirl * 0.5;
                float2 lp = float2(aw / 6.2831, inv);           // 로그-폴라

                // ── 깊이 레이어: 스크롤 속도를 달리해 시차 ──
                float stars = 0.0;
                stars += starfield(lp * float2(_StarDensity*1.0, 5.5) + float2(0.00, -t*0.20*_Parallax), 7.0) * 1.00;
                stars += starfield(lp * float2(_StarDensity*1.8, 9.0) + float2(0.31, -t*0.38*_Parallax), 8.5) * 0.70;
                stars += starfield(lp * float2(_StarDensity*3.1, 15.0) + float2(0.67, -t*0.64*_Parallax), 10.0) * 0.45;
                // 깜빡임
                float tw = 0.75 + 0.25*sin(t*3.1 + hash21(floor(lp*40.0))*31.4);
                stars *= _StarAmount * saturate(rn*1.8) * tw;

                // ── 성운: 방사 주파수를 올려 띠가 지지 않게 한다 ──
                float2 np = float2(aw/6.2831 * _NebulaScale*3.0, inv * _NebulaScale*2.2);
                float n1 = fbm(np + float2(0, -t*0.13));
                float n2 = fbm(np*2.1 + float2(0.5, -t*0.24));
                float neb = saturate(n1*0.70 + n2*0.50 - 0.38) * _NebulaAmount;
                neb *= saturate(rn*1.4);

                // ── 입자: 빨려 들어가는 티끌 ──
                float2 dp = float2(aw/6.2831 * 30.0, inv*5.0 - t*_DustSpeed*2.0);
                float dust = starfield(dp, 12.0) * _DustAmount * saturate(rn*2.2);
                // 안쪽으로 갈수록 길게 늘어진 잔상
                float2 dp2 = float2(aw/6.2831 * 30.0, inv*5.0 - t*_DustSpeed*2.0 + 0.12);
                dust += starfield(dp2, 12.0) * _DustAmount * 0.45 * saturate(rn*2.2);

                // ── 테두리 잔광: 각도마다 세기가 달라 균일한 띠가 되지 않는다 ──
                float rimBase = saturate((rn - (1.0-_RimWidth)) / max(_RimWidth,1e-4));
                float rimVar  = 0.55 + 0.45*vnoise(float2(a*2.2, t*0.6));
                float rim = pow(rimBase, 2.2) * _RimAmount * rimVar;

                // ── 합성 ──
                float3 col = lerp(_FarColor.rgb, _NearColor.rgb, saturate(rn*1.15));
                col = lerp(col, col*0.35, i.depth*0.7);
                col += _NebulaTint.rgb * neb * (0.30 + rn*0.80);
                col += _StarTint.rgb   * stars;
                col += _DustTint.rgb   * dust * 1.1;
                col += _RimColor.rgb   * rim;

                // 중심 특이점 — 빛이 빨려 사라지는 지점
                col *= saturate(0.05 + rn*3.0);

                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
