Shader "BrightDream/TreehouseCraft"
{
 Properties { _Color ("Paper tint", Color) = (1,1,1,1) _VertexTint ("Use craft vertex colors", Range(0,1)) = 0 _Glossiness ("Smoothness", Range(0,1)) = 0.2 }
 SubShader
 {
  Tags { "RenderType"="Opaque" }
  Cull Off
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows vertex:vert
  #pragma target 3.0
  struct Input { float4 craftColor; float3 worldPos; };
  fixed4 _Color; half _VertexTint; half _Glossiness;
  void vert(inout appdata_full v, out Input o) { UNITY_INITIALIZE_OUTPUT(Input,o); o.craftColor=v.color; }
  void surf(Input IN, inout SurfaceOutputStandard o)
  {
   fixed3 tint=lerp(fixed3(1,1,1),IN.craftColor.rgb,_VertexTint);
   float fibers=0.985+0.015*sin(IN.worldPos.x*287+sin(IN.worldPos.z*43))*sin(IN.worldPos.y*191);
   o.Albedo=_Color.rgb*tint*fibers; o.Smoothness=_Glossiness; o.Metallic=0; o.Alpha=1;
  }
  ENDCG
 }
 Fallback "Diffuse"
}

