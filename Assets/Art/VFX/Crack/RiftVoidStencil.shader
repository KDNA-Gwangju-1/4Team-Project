Shader "Dream1/RiftVoidStencil"
{
    Properties
    {
        _NearColor ("Near Color", Color) = (0.16,0.10,0.30,1)
        _FarColor  ("Far Color",  Color) = (0.01,0.00,0.03,1)
        _Swirl     ("Swirl Speed", Range(0,3)) = 0.35
        _SwirlTint ("Swirl Tint", Color) = (0.55,0.30,0.95,1)
    }
    SubShader
    {
        // 스텐실이 7 인 곳 = 구멍 안쪽에서만 그린다. 옆에서 보면 사라진다.
        Tags { "RenderType"="Opaque" "Queue"="Geometry-1" }
        Pass
        {
            Name "VoidThroughHole"
            ZWrite On
            Cull Off                        // 감김 방향에 의존하지 않는다
            Stencil { Ref 7  Comp Equal  Pass Keep }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _NearColor, _FarColor, _SwirlTint;
            float  _Swirl;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float depth : TEXCOORD1; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                o.depth = saturate(v.vertex.z * 0.8 + 0.25);   // 뒤로 갈수록 1
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 c = i.uv - 0.5;
                float  r = length(c) + 1e-5;
                float  a = atan2(c.y, c.x);

                // 안으로 감기는 소용돌이 줄무늬
                float sw = sin(a*3.0 + 1.0/max(r,0.04) * 0.55 - _Time.y * _Swirl * 2.0);
                sw = sw * 0.5 + 0.5;

                fixed3 col = lerp(_NearColor.rgb, _FarColor.rgb, i.depth);
                col += _SwirlTint.rgb * sw * (1.0 - i.depth) * 0.35;
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
