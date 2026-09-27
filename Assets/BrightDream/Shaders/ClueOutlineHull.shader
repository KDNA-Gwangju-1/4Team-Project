Shader "BrightDream/ClueOutlineHull"
{
    // 닫힌 3D 메시(리본/사진첩/편지)용 - 뒤집힌 껍질(inverted hull) 방식.
    // 버텍스를 노멀 방향으로 살짝 밀어내고 앞면은 컬링해서, 실루엣 가장자리에만
    // 얇은 테두리가 보이게 한다. 깊이 테스트를 그대로 쓰므로 다른 오브젝트/벽에 가리면 안 보인다.
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0.98, 0.92, 0.72, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.05)) = 0.008
        _PulseSpeed ("Pulse Speed", Float) = 1.2
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.12
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }

        Pass
        {
            Cull Front
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _PulseSpeed;
            float _PulseAmount;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                float3 worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz + worldNormal * (_OutlineWidth * pulse);
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
