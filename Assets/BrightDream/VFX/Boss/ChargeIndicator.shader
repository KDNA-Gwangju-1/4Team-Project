// 보스가 돌진하기 전에 바닥에 까는 피격 경로 표시.
//
// 돌진 판정(보스 몸 반경 원이 출발점에서 도착점까지 쓸고 가는 영역)과 같은
// 양끝이 둥근 막대 모양을 그린다. _Fill 이 0 에서 1 로 차오르면서 보스 쪽에서
// 돌진 방향으로 채워지고, 차오르는 앞머리가 밝게 번진다. 색/투명도는 SlamIndicator 와 맞춘다.
Shader "BrightDream/ChargeIndicator"
{
    Properties
    {
        _Color     ("Color", Color) = (1.0, 0.25, 0.18, 1)
        _EdgeColor ("Edge Color", Color) = (1.0, 0.75, 0.45, 1)
        _Fill      ("Fill", Range(0,1)) = 0
        _Alpha     ("Alpha", Range(0,1)) = 1
        _Length    ("Total Length (m)", Float) = 10
        _Width     ("Width (m)", Float) = 6
        _RingWidth ("Ring Width (m)", Float) = 0.3
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
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
            float  _Length;
            float  _Width;
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
                // uv.x = 가로(0~1), uv.y = 돌진 방향(0 = 보스 뒤쪽 끝, 1 = 도착점 앞쪽 끝) -> 미터 단위로 바꾼다.
                float r = _Width * 0.5;
                float2 p = float2((i.uv.x - 0.5) * _Width, i.uv.y * _Length);

                // 양끝이 둥근 막대(스타디움)까지의 거리. 안쪽이 음수.
                float d = length(float2(p.x, p.y - clamp(p.y, r, _Length - r))) - r;
                float aa = fwidth(d) * 1.5;
                float inside = 1.0 - smoothstep(-aa, 0.0, d);
                if (inside <= 0.0) discard;

                // 바깥 테두리 - 어디까지 맞는지 처음부터 보여 준다.
                float ring = smoothstep(-_RingWidth - aa, -_RingWidth, d);

                // 보스 쪽에서 돌진 방향으로 차오르는 안쪽 면
                float front = _Fill * _Length;
                float aaY = fwidth(p.y) * 1.5;
                float inner = 1.0 - smoothstep(front - aaY, front, p.y);

                // 차오르는 앞머리 - 여기가 제일 밝다.
                float edge = exp(-pow((p.y - front) / max(_RingWidth, 1e-4), 2.0));
                if (_Fill <= 0.001) edge = 0.0;

                fixed3 col = _Color.rgb * (inner * 0.55 + ring * 0.9)
                           + _EdgeColor.rgb * edge * 1.2;

                float a = saturate(inner * 0.34 + ring * 0.85 + edge * 0.9) * inside * _Alpha;
                return fixed4(col, a);
            }
            ENDCG
        }
    }

    Fallback Off
}
