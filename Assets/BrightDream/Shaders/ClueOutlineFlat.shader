Shader "BrightDream/ClueOutlineFlat"
{
    // 얇은 평면형 텍스처 decal(나무 새김 글자)용 - 글자 모양이 메시가 아니라 텍스처의 알파 채널에만
    // 있으므로, 메시를 키우는 방식으로는 글자 윤곽을 따라갈 수 없다. 대신 같은 텍스처의 알파를
    // 주변 UV로 여러 번 샘플링해서 "글자 안쪽은 비우고, 글자 바로 바깥 가장자리만" 칠하는
    // 알파 기반 가장자리 검출(스프라이트 아웃라인) 방식을 사용한다.
    Properties
    {
        _MainTex ("Source Texture (Alpha)", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (0.98, 0.92, 0.72, 1)
        _OutlineWidthTexels ("Outline Width (texels)", Range(0.5, 10)) = 3
        _AlphaCutoff ("Alpha Cutoff", Range(0, 1)) = 0.35
        _PushBack ("Push Back Along -Normal", Range(-0.02, 0.02)) = 0.0005
        _PulseSpeed ("Pulse Speed", Float) = 1.2
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.12
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-1" }

        Pass
        {
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _OutlineColor;
            float _OutlineWidthTexels;
            float _AlphaCutoff;
            float _PushBack;
            float _PulseSpeed;
            float _PulseAmount;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float3 worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz - worldNormal * _PushBack;
                o.pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float centerA = tex2D(_MainTex, i.uv).a;
                // 글자 안쪽(원본 decal이 이미 칠하는 영역)은 비워서 원본과 겹치지 않게 한다.
                if (centerA > _AlphaCutoff) discard;

                float maxA = 0;
                float2 texel = _MainTex_TexelSize.xy * _OutlineWidthTexels;
                [unroll]
                for (int x = -2; x <= 2; x++)
                {
                    [unroll]
                    for (int y = -2; y <= 2; y++)
                    {
                        if (x == 0 && y == 0) continue;
                        float2 offset = float2(x, y) * 0.5 * texel;
                        maxA = max(maxA, tex2D(_MainTex, i.uv + offset).a);
                    }
                }
                // 근처(가장자리 폭 안)에 글자가 없으면 완전히 비운다 - 얇은 테두리만 남긴다.
                if (maxA < _AlphaCutoff) discard;

                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                fixed4 col = _OutlineColor;
                col.a *= saturate(pulse);
                return col;
            }
            ENDCG
        }
    }
}
