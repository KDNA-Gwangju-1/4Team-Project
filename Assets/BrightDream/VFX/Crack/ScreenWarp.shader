// 균열로 빨려 들어갈 때 화면 전체를 일렁이게 하는 이미지 이펙트.
//
// Built-in RP 용이다. 카메라의 OnRenderImage 에서 Graphics.Blit 으로 통과시킨다.
// _Strength 0 이면 원본 그대로, 1 이면 최대로 일그러진다.
Shader "Hidden/BrightDream/ScreenWarp"
{
    Properties
    {
        _MainTex   ("Texture", 2D) = "white" {}
        _Strength  ("Strength", Range(0,1)) = 0
        _Tint      ("Tint", Color) = (0.55, 0.30, 0.95, 1)
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float  _Strength;
            fixed4 _Tint;

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                float2 c  = uv - 0.5;
                float  r  = length(c);
                float  a  = atan2(c.y, c.x);
                float  t  = _Time.y;
                float  s  = saturate(_Strength);

                float2 dir = r > 1e-4 ? c / r : float2(0.0, 0.0);

                // 화면 가운데로 빨려 드는 당김. 바깥일수록 세게 끌린다.
                float pull = s * 0.20 * smoothstep(0.0, 1.0, r);
                // 물 위에 퍼지는 동심원
                float ripple = sin(r * 24.0 - t * 5.0) * 0.014 * s;
                // 각 방향으로 느리게 흔들리는 일렁임
                float wob = sin(a * 3.0 + t * 2.1) * 0.010 * s
                          + sin(a * 5.0 - t * 1.4) * 0.006 * s;

                float2 duv = uv - dir * (pull + ripple + wob);

                // 색수차. 안쪽으로 갈수록 채널이 벌어진다.
                float ca = s * 0.007 * (0.3 + r);
                fixed3 col;
                col.r = tex2D(_MainTex, duv + dir * ca).r;
                col.g = tex2D(_MainTex, duv).g;
                col.b = tex2D(_MainTex, duv - dir * ca).b;

                // 잔물결처럼 반짝이는 잡음
                float grain = hash21(uv * 640.0 + t * 37.0);
                col += (grain - 0.5) * 0.05 * s;

                // 보랏빛으로 물들고 가장자리가 어두워진다
                col = lerp(col, col * _Tint.rgb * 1.3, s * 0.55);
                float vig = 1.0 - smoothstep(0.22, 0.88, r) * s * 0.85;
                col *= vig;

                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }

    Fallback Off
}
