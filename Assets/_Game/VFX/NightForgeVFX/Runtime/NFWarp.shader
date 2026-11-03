// Game Mellow VFX: screen warp for the Built-in pipeline (NFEffect draws it after the effect).
// Export ID: gRR42b5Kl8uc_ISDDRRV4zjbTFhNIhnJwM-V-IZvA6oY6Zm7Gfoa_AiWtGKfMKrvkJMJggKPFt_VkEKp
Shader "Hidden/NightForge/Warp" {
  SubShader {
    Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" }
    GrabPass { "_NfGrab" }
    Pass {
      ZTest Always ZWrite Off Cull Off Blend Off
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #include "UnityCG.cginc"
      #include "NFWarp.cginc"
      sampler2D _NfGrab;
      struct v2f { float4 pos : SV_POSITION; float4 grab : TEXCOORD0; float4 scr : TEXCOORD1; };
      v2f vert(float4 vertex : POSITION)
      {
        v2f o;
        o.pos = float4(vertex.xy * 2.0, UNITY_NEAR_CLIP_VALUE, 1.0); // the quad covers the screen
        o.grab = ComputeGrabScreenPos(o.pos);
        o.scr = ComputeScreenPos(o.pos);
        return o;
      }
      float4 frag(v2f i) : SV_Target
      {
        float2 g = i.grab.xy / i.grab.w, v = i.scr.xy / i.scr.w;
        #if UNITY_UV_STARTS_AT_TOP
        float fs = -_ProjectionParams.x;
        #else
        float fs = _ProjectionParams.x;
        #endif
        return nf_warp_sample(_NfGrab, g, nf_warp(v), float2(1.0, fs));
      }
      ENDCG
    }
  }
}
