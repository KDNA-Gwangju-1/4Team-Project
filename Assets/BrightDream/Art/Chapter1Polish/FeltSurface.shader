Shader "BrightDream/MetreFelt"
{
    Properties
    {
        _Color ("Material tint", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = .15
        _Grain ("Woven surface", Range(0,1)) = .15
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        struct Input { float3 worldPos; };
        fixed4 _Color;
        half _Glossiness, _Grain;
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 p=IN.worldPos;
            // Fixed physical frequency: enlarging a prop cannot enlarge the fibres.
            float grain=sin(p.x*241+sin(p.z*67))*sin(p.y*193+p.z*211);
            float mottling=sin(p.x*7.3+p.z*5.7)*sin(p.y*13.1-p.z*9.7);
            // Fade the weave below pixel size to avoid distant shimmer.
            float footprint=max(length(ddx(p)),length(ddy(p)));
            float visible=1-smoothstep(.004,.025,footprint);
            o.Albedo=_Color.rgb*(1+grain*_Grain*visible+mottling*.025);
            o.Metallic=0;
            o.Smoothness=_Glossiness;
            o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Standard"
}
