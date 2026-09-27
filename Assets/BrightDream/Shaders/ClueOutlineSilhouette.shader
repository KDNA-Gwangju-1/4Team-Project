Shader "BrightDream/ClueOutlineSilhouette"
{
    // 벤치 위 편지처럼 얇게 눕거나 구겨진 종이용 테두리.
    // 노멀 방향으로 미는 뒤집힌 껍질(ClueOutlineHull)은 종이 노멀이 위/아래만 향해 가장자리에 테두리가 안 생긴다.
    // 대신 물체 중심(_Center)에서 수평 바깥쪽으로 _OutlineWidth(m)만큼 밀어낸 복제본을 놓인 바닥 높이(_Center.w)로
    // 납작하게 눌러 종이 바로 밑에 깐다. 안쪽은 위에 놓인 종이에 가려지고, 가장자리 밖으로 삐져나온 부분만 테두리로 보인다.
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0.36, 0.17, 0.07, 1)
        _OutlineWidth ("Outline Width (m)", Range(0, 0.08)) = 0.018
        _Center ("World Center (xz) / Floor Y (w)", Vector) = (0, 0, 0, 0)
        _PulseSpeed ("Pulse Speed", Float) = 1.2
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.12
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }

        Pass
        {
            Cull Off
            ZWrite On
            ZTest LEqual
            // 바닥(벤치 좌석)과 몇 mm 차이라 깜빡이지 않게 살짝 앞으로 당긴다.
            Offset -1, -2

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _OutlineColor;
            float _OutlineWidth;
            float4 _Center;
            float _PulseSpeed;
            float _PulseAmount;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float2 away = worldPos.xz - _Center.xz;
                float len = length(away);
                // 중심 바로 위의 점은 방향이 없으니 밀지 않는다 (어차피 원본에 가려진다).
                float2 dir = len > 1e-4 ? away / len : float2(0, 0);
                worldPos.xz += dir * (_OutlineWidth * pulse);
                worldPos.y = _Center.w;
                o.pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
}
