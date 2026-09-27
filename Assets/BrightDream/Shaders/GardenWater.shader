Shader "BrightDream/Garden Water"
{
    Properties
    {
        _ShallowColor ("Shallow mint", Color) = (0.31,0.68,0.62,1)
        _DeepColor ("Deep turquoise", Color) = (0.065,0.32,0.36,1)
        _RippleColor ("Soft ripple light", Color) = (0.65,0.89,0.82,1)
        _WaveSpeed ("Wave speed", Range(0,1)) = 0.2
        _WaveStrength ("Wave strength", Range(0,1)) = 0.16
        _ShoreMask ("Distance from shoreline", 2D) = "white" {}
        _LakeCenter ("Lake center XZ", Vector) = (17.993473,20.313265,0,0)
        _LakeExtent ("Lake extent XZ", Vector) = (5.243719,7.11038,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            fixed4 _ShallowColor, _DeepColor, _RippleColor;
            float _WaveSpeed, _WaveStrength;
            sampler2D _ShoreMask;
            float4 _LakeCenter, _LakeExtent;
            struct appdata { float4 vertex:POSITION; };
            struct v2f { float4 pos:SV_POSITION; float3 world:TEXCOORD0; SHADOW_COORDS(1) UNITY_FOG_COORDS(2) };
            v2f vert(appdata v)
            {
                v2f o; o.pos=UnityObjectToClipPos(v.vertex);
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                TRANSFER_SHADOW(o); UNITY_TRANSFER_FOG(o,o.pos); return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                // World projection keeps ripple size independent of mesh UV scale.
                // never deform vertices, so shoreline, bridge and collision stay unchanged.
                float2 p=i.world.xz;
                float t=_Time.y*_WaveSpeed;
                float a=dot(p,float2(1.4,0.9))+t;
                float b=dot(p,float2(-0.8,1.7))-t*0.73;
                float2 slope=float2(1.4,0.9)*cos(a)+float2(-0.8,1.7)*cos(b)*0.55;
                float3 n=normalize(float3(-slope.x*_WaveStrength,1,-slope.y*_WaveStrength));
                float2 shoreUV=(p-_LakeCenter.xy)/(2*_LakeExtent.xy)+0.5;
                float distanceToShore=tex2D(_ShoreMask,shoreUV).r;
                float depth=smoothstep(0,1,distanceToShore);
                float wave=sin(a*1.8+sin(b*1.4)*1.1+cos(p.x*0.7-p.y)*0.8);
                float rippleLight=pow(1-abs(wave),14)*0.09*smoothstep(-0.2,0.7,sin(b*1.3+cos(a)));
                float shore=(1-smoothstep(0.015,0.10,distanceToShore))*0.1;
                float3 view=normalize(_WorldSpaceCameraPos-i.world);
                float fresnel=pow(1-saturate(dot(n,view)),4)*0.25;
                float3 light=normalize(_WorldSpaceLightPos0.xyz);
                float spec=pow(saturate(dot(n,normalize(light+view))),72)*0.3;
                float shadow=SHADOW_ATTENUATION(i);
                float3 color=lerp(_ShallowColor.rgb,_DeepColor.rgb,depth*0.8);
                color*=0.85+0.15*saturate(dot(n,light))*shadow;
                color+=_RippleColor.rgb*(rippleLight+fresnel+shore)+_LightColor0.rgb*spec*shadow;
                fixed4 result=fixed4(color,1);
                UNITY_APPLY_FOG(i.fogCoord,result); return result;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
