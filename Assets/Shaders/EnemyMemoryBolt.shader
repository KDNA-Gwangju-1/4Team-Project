Shader "ReDream/EnemyMemoryBolt"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment BoltFragment
            #include "UnitySprites.cginc"
            fixed4 BoltFragment(v2f i) : SV_Target
            {
                float2 p = (floor(i.texcoord * 32) + .5) / 32 * 2 - 1;
                float radius = length(p);
                float alpha = (1-smoothstep(.94,1.0,radius)) * i.color.a;
                // Dark separation rim, red danger ring and pale diamond core remain distinct
                // on both the purple background and the cream platform tops.
                float3 ink = float3(.15,.035,.10);
                ink = lerp(ink,float3(1.0,.24,.37),1-smoothstep(.76,.84,radius));
                ink = lerp(ink,float3(.60,.07,.22),1-smoothstep(.56,.62,radius));
                float diamond = 1-smoothstep(.38,.48,abs(p.x)+abs(p.y));
                ink = lerp(ink,float3(1.0,.95,.73),diamond);
                float glint = (1-smoothstep(.06,.11,abs(p.x+p.y+.6))) *
                    (1-smoothstep(.70,.76,radius)) * smoothstep(.48,.55,radius);
                ink = lerp(ink,float3(1.0,.78,.68),glint*.8);
                return fixed4(ink*alpha,alpha);
            }
            ENDCG
        }
    }
}
