Shader "BrightDream/Garden Ground"
{
    Properties
    {
        _Color ("Warm earth", Color) = (0.64,0.51,0.34,1)
        _GrainStrength ("Soil variation", Range(0,1)) = 0.22
        _PebbleStrength ("Sparse pebbles", Range(0,1)) = 0.35
        _ArenaCenter ("Arena center XZ", Vector) = (30.3987,37.6758,0,0)
        _ArenaRadius ("Arena radius", Float) = 8.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        fixed4 _Color;
        float _GrainStrength, _PebbleStrength, _ArenaRadius;
        float4 _ArenaCenter;
        struct Input { float3 worldPos; };
        float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
        float noise(float2 p)
        {
            float2 i=floor(p), f=frac(p); f=f*f*(3-2*f);
            return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
        }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // World projection avoids stretched UVs on the flattened cylinder.
            float2 p=IN.worldPos.xz;
            float broad=noise(p*0.75)*0.6+noise(p*2.6)*0.4;
            float grain=noise(p*24)-0.5;
            float grainVisibility=1-saturate(max(fwidth(p.x),fwidth(p.y))*24);
            float ripple=sin(p.x*3.7+noise(p*0.6)*5+p.y*1.4)*0.025;
            float3 soil=_Color.rgb*(0.84+_GrainStrength*broad+grain*0.12*grainVisibility+ripple);
            float2 cell=floor(p*2.2), local=frac(p*2.2)-0.5;
            float h=hash(cell);
            float2 offset=float2(hash(cell+17),hash(cell+31))*0.35-0.175;
            float radius=0.045+h*0.075;
            float d=length((local-offset)*float2(1,1.35));
            float aa=max(fwidth(d),0.015);
            float pebble=1-smoothstep(radius-aa,radius+aa,d);
            float edge=smoothstep(0.5,0.9,length((p-_ArenaCenter.xy)/max(_ArenaRadius,0.1)));
            pebble*=step(0.68,h)*lerp(0.12,1,edge)*_PebbleStrength;
            o.Albedo=lerp(soil,float3(0.71,0.64,0.49)*(0.8+0.2*h),pebble);
            o.Metallic=0; o.Smoothness=0.08; o.Occlusion=1; o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
