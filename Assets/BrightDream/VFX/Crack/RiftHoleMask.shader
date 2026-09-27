Shader "Dream1/RiftHoleMask"
{
    // 구멍 자리에 스텐실 값만 쓴다. 화면에는 아무것도 그리지 않는다.
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-100" }
        Pass
        {
            Name "HoleStencilWrite"
            ColorMask 0
            ZWrite Off
            Cull Off
            Stencil { Ref 7  Comp Always  Pass Replace }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); return o; }
            fixed4 frag (v2f i) : SV_Target { return 0; }
            ENDCG
        }
    }
}
