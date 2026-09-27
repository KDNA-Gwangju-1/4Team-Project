Shader "ReDream/NightmareDissolve"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Progress ("Dissolve", Range(0,1)) = 0
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
            #pragma fragment DissolveFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            float _Progress;
            fixed4 DissolveFragment(v2f input) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(input.texcoord) * input.color;
                float2 cell = floor(input.texcoord * 128);
                float noise = frac(sin(dot(cell, float2(12.9898,78.233))) * 43758.5453);
                // Fixed pixel islands erode from the head toward the feet.
                // The child underneath never changes sprite, pivot or size.
                float threshold = (1 - input.texcoord.y) * .72 + noise * .28;
                float remaining = threshold - _Progress * 1.05;
                float visible = step(0, remaining);
                float edge = (1 - smoothstep(0, .035, remaining)) * step(.001, _Progress);
                c.rgb = lerp(c.rgb, fixed3(.83,.70,.47), edge * .7);
                c.a *= visible;
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
