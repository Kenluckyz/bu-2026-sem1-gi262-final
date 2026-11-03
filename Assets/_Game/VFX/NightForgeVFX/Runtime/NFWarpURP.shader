// Game Mellow VFX: screen warp for URP. Add a "Full Screen Pass Renderer Feature" to your
// URP renderer, set Pass Material to a material with this shader, Injection Point
// "Before Rendering Post Processing", Requirements "Color". It does nothing while no
// effect warps the screen.
// Export ID: gRR42b5Kl8uc_ISDDRRV4zjbTFhNIhnJwM-V-IZvA6oY6Zm7Gfoa_AiWtGKfMKrvkJMJggKPFt_VkEKp
Shader "Game Mellow/Warp (URP Full Screen Pass)" {
  SubShader {
    Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
    Pass {
      Name "NFWarp"
      ZTest Always ZWrite Off Cull Off Blend Off
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma target 3.5
      #include "UnityCG.cginc"
      #include "NFWarp.cginc"
      sampler2D _BlitTexture;
      struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
      v2f vert(uint id : SV_VertexID)
      {
        v2f o;
        float2 uv = float2((id << 1) & 2, id & 2); // full-screen triangle
        o.pos = float4(uv * 2.0 - 1.0, UNITY_NEAR_CLIP_VALUE, 1.0);
        #if UNITY_UV_STARTS_AT_TOP
        uv.y = 1.0 - uv.y;
        #endif
        o.uv = uv;
        return o;
      }
      float4 frag(v2f i) : SV_Target
      {
        if (_NfWarpB.w < 0.5) return tex2D(_BlitTexture, i.uv);
        return nf_warp_sample(_BlitTexture, i.uv, nf_warp(i.uv), float2(1.0, 1.0));
      }
      ENDCG
    }
  }
}
