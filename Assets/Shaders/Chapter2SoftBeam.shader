Shader "ReDream/Chapter2SoftBeam"
{
    Properties
    {
        [PerRendererData] _MainTex ("Beam", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FadeStart ("Distance fade start", Range(0,0.9)) = 0.2
        _Intensity ("Intensity", Range(0,1)) = 0.58
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment BeamFragment
            #pragma target 2.0
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            float _FadeStart, _Intensity;
            fixed4 BeamFragment(v2f input) : SV_Target
            {
                // The source cutout has transparent leading pixels; do not leave a gap at the lens.
                float2 beamUV=input.texcoord;
                beamUV.x=lerp(0.02,1.0,beamUV.x);
                fixed4 color=SampleSpriteTexture(beamUV)*input.color;
                float distanceFade=1.0-smoothstep(_FadeStart,1.0,input.texcoord.x);
                // A narrow aperture at the lens opens gradually, rather than a glowing rectangle.
                float coneWidth=lerp(0.035,1.0,input.texcoord.x);
                float edgeFade=1.0-smoothstep(0.60,1.0,abs(input.texcoord.y-0.5)*2.0/coneWidth);
                color.a*=distanceFade*edgeFade*_Intensity;
                color.rgb*=color.a;
                return color;
            }
            ENDCG
        }
    }
}
