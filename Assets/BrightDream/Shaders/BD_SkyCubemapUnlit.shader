Shader "BrightDream/SkyCubemapUnlit"
{
    // 상자형 배경(벽 4면 + 천장)에 사용하는 Unlit 셰이더.
    // 월드 좌표를 중심점 기준 방향 벡터로 변환해 큐브맵을 샘플링하기 때문에
    // 각 면마다 UV를 따로 맞추지 않아도 반복/이음매 없이 자연스럽게 이어진다.
    Properties
    {
        _Cube ("Cubemap (Equirect source)", Cube) = "" {}
        _Center ("World Center", Vector) = (14.58, 6.25, 39.51, 0)
        _Tint ("Tint", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Back
        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            samplerCUBE _Cube;
            float4 _Center;
            fixed4 _Tint;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 worldPos : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 dir = normalize(i.worldPos - _Center.xyz);
                fixed4 col = texCUBE(_Cube, dir);
                return col * _Tint;
            }
            ENDCG
        }
    }
    Fallback "Unlit/Texture"
}
