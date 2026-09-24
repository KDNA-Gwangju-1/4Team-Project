Shader "BrightDream/NightmareCloth"
{
    Properties
    {
        [HideInInspector] _MainTex ("UV source", 2D) = "white" {}
        _Color ("Dyed cloth", Color) = (.18,.10,.22,1)
        _Glossiness ("Worn satin sheen", Range(0,1)) = .23
        _Grain ("Fine fibres", Range(0,1)) = .16
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        struct Input { float2 uv_MainTex; };
        sampler2D _MainTex;
        fixed4 _Color;
        half _Glossiness, _Grain;
        float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
        float noise(float2 p)
        {
            float2 i=floor(p),f=frac(p); f=f*f*(3-2*f);
            return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
        }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv=IN.uv_MainTex;
            float wear=noise(uv*47)*.6+noise(uv*117)*.4;
            float footprint=max(length(ddx(uv)),length(ddy(uv)));
            float visible=1-smoothstep(.0004,.003,footprint);
            float fibres=sin(uv.x*4100+sin(uv.y*97))*sin(uv.y*5300);
            o.Albedo=_Color.rgb*(.82+wear*.35+fibres*_Grain*visible);
            o.Normal=normalize(float3(cos(uv.x*4100)*.10*visible,sin(uv.y*5300)*.08*visible,1));
            o.Smoothness=_Glossiness*(.75+wear*.4);
            o.Metallic=0; o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Standard"
}
