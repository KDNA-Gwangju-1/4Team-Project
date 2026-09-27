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
                // A single stepped front moves down the image. Each column has
                // one boundary, so the effect cannot leave random pixel islands.
                float column = floor(input.texcoord.x * 32);
                float offset = sin(column * .65) * .012;
                float threshold = 1 - input.texcoord.y + offset;
                float front = lerp(-.03, 1.03, saturate(_Progress));
                float visible = smoothstep(front, front + .008, threshold);
                // Preserve the source colours; no gold rim or added debris.
                c.a *= visible;
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
