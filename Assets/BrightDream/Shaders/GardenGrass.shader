Shader "BrightDream/Continuous Garden Grass"
{
    Properties
    {
        _Color ("Meadow green", Color) = (0.43,0.64,0.25,1)
        _LightColor ("Soft young grass", Color) = (0.64,0.76,0.38,1)
        _Variation ("Broad variation", Range(0,1)) = 0.15
        _DetailStrength ("Grass detail", Range(0,1)) = 0.10
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        fixed4 _Color, _LightColor;
        half _Variation, _DetailStrength;
        struct Input { float3 worldPos; };
        float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
        float noise(float2 p)
        {
            float2 i=floor(p), f=frac(p); f=f*f*(3-2*f);
            return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),
                        lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
        }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // World coordinates are shared across every floor tile and grass patch.
            // Never use mesh UVs, per-object origins or per-material offsets here.
            float2 p=IN.worldPos.xz;
            float broad=noise(p*.17)+.35*noise(p*.43+13.2);
            float blend=saturate(.48+(broad-.675)*_Variation);
            float3 meadow=lerp(_Color.rgb,_LightColor.rgb,blend);
            float soft=noise(p*1.7+6.1)-.5;
            // Fine directional flecks fade with screen footprint to prevent shimmer.
            float2 q=float2(p.x*.91+p.y*.41,-p.x*.41+p.y*.91);
            float grain=noise(q*float2(18,5))-.5;
            float fade=1-smoothstep(.04,.24,max(length(ddx(p)),length(ddy(p))));
            meadow*=1+soft*.035+grain*_DetailStrength*fade;
            o.Albedo=meadow;
            o.Metallic=0;
            o.Smoothness=.06;
            o.Occlusion=1;
            o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
