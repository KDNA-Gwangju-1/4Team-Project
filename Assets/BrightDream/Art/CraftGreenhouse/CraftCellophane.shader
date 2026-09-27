Shader "BrightDream/CraftCellophane"
{
 Properties { _Color ("Mint film tint / opacity", Color) = (0.42,0.81,0.76,0.25) _Glossiness ("Smoothness", Range(0,1)) = 0.28 }
 SubShader
 {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" }
  Cull Off
  ZWrite Off
  CGPROGRAM
  #pragma surface surf Standard alpha:fade
  #pragma target 3.0
  struct Input { float3 worldPos; };
  fixed4 _Color; half _Glossiness;
  void surf(Input IN, inout SurfaceOutputStandard o) { o.Albedo=_Color.rgb; o.Metallic=0; o.Smoothness=_Glossiness; o.Alpha=_Color.a; }
  ENDCG
 }
 Fallback "Transparent/Diffuse"
}
