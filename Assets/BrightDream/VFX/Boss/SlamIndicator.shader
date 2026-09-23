// 보스가 내려찍기 전에 바닥에 까는 피격 반경 표시.
//
// 바닥에 눕힌 Quad 한 장에 입힌다. _Fill 이 0 에서 1 로 차오르면서
// 안쪽이 채워지고, 차오르는 가장자리가 밝게 번진다.
Shader "BrightDream/SlamIndicator"
{
    Properties
    {
        _Color     ("Color", Color) = (1.0, 0.25, 0.18, 1)
        _EdgeColor ("Edge Color", Color) = (1.0, 0.75, 0.45, 1)
        _Fill      ("Fill", Range(0,1)) = 0
        _Alpha     ("Alpha", Range(0,1)) = 1
        _RingWidth ("Ring Width", Range(0.005, 0.3)) = 0.05
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        // 바닥 메시와 같은 높이에서 z-fighting 이 나지 않게 살짝 앞으로 당긴다.
        Offset -1, -1

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            fixed4 _Color;
            fixed4 _EdgeColor;
            float  _Fill;
            float  _Alpha;
            float  _RingWidth;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // uv 중심 기준 0(가운데)~1(바깥 테두리)
                float r = length(i.uv - 0.5) * 2.0;
                if (r > 1.0) discard;

                float aa = fwidth(r) * 1.5;

                // 바깥 테두리 - 반경이 어디까지인지 처음부터 보여 준다.
                float ring = smoothstep(1.0 - _RingWidth - aa, 1.0 - _RingWidth, r);

                // 차오르는 안쪽 면
                float inner = 1.0 - smoothstep(_Fill - aa, _Fill, r);

                // 차오르는 경계선 - 여기가 제일 밝다.
                float edge = exp(-pow((r - _Fill) / max(_RingWidth, 1e-4), 2.0));
                if (_Fill <= 0.001) edge = 0.0;

                fixed3 col = _Color.rgb * (inner * 0.55 + ring * 0.9)
                           + _EdgeColor.rgb * edge * 1.2;

                float a = saturate(inner * 0.34 + ring * 0.85 + edge * 0.9) * _Alpha;
                return fixed4(col, a);
            }
            ENDCG
        }
    }

    Fallback Off
}
