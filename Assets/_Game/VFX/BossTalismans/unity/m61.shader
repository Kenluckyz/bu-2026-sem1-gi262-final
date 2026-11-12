// Game Mellow VFX (Unity, Built-in + URP) · generated from the Lab's shader, do not edit
// Export ID: KWplS7ypCpXtoySbfMTK9BwZ44gBaJfBX2Wx26Uf6PxIs_m2bc7o8yGkyZBB1F-Welm_d2rCkyNSnt9J
Shader "NightForge/6c1j1o6z41/m61" {
  SubShader {
    Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
    Pass {
      Blend SrcAlpha One
      ZWrite Off
      ZTest LEqual
      Cull Back
      CGPROGRAM
      #pragma vertex nf_vert
      #pragma fragment nf_frag
      #pragma target 3.5
      
      #include "UnityCG.cginc"


#define vec2 float2
#define vec3 float3
#define vec4 float4
#define ivec2 int2
#define ivec3 int3
#define ivec4 int4
#define mat2 float2x2
#define mat3 float3x3
#define mat4 float4x4
#define mix lerp
#define fract frac
#define dFdx ddx
#define dFdy ddy
#define inversesqrt rsqrt
#define texture2D tex2D
#define texture tex2D
#define mod(x, y) ((x) - (y) * floor((x) / (y)))
#define modelViewMatrix UNITY_MATRIX_MV
#define projectionMatrix UNITY_MATRIX_P
#define modelMatrix unity_ObjectToWorld
#define viewMatrix UNITY_MATRIX_V
#define normalMatrix ((float3x3)UNITY_MATRIX_IT_MV)
#define cameraPosition _WorldSpaceCameraPos
#define instanceMatrix NF_IDENTITY
#define isOrthographic false
static const float4x4 NF_IDENTITY = float4x4(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1);
float2x2 nf_inverse(float2x2 m) { return float2x2(m._22, -m._12, -m._21, m._11) / determinant(m); }
float3x3 nf_inverse(float3x3 m) {
  float3 a = m[0], b = m[1], c = m[2];
  float3 r0 = cross(b, c), r1 = cross(c, a), r2 = cross(a, b);
  return transpose(float3x3(r0, r1, r2)) / dot(a, r0);
}
float4x4 nf_inverse(float4x4 m) {
  float n11 = m[0][0], n12 = m[1][0], n13 = m[2][0], n14 = m[3][0];
  float n21 = m[0][1], n22 = m[1][1], n23 = m[2][1], n24 = m[3][1];
  float n31 = m[0][2], n32 = m[1][2], n33 = m[2][2], n34 = m[3][2];
  float n41 = m[0][3], n42 = m[1][3], n43 = m[2][3], n44 = m[3][3];
  float t11 = n23 * n34 * n42 - n24 * n33 * n42 + n24 * n32 * n43 - n22 * n34 * n43 - n23 * n32 * n44 + n22 * n33 * n44;
  float t12 = n14 * n33 * n42 - n13 * n34 * n42 - n14 * n32 * n43 + n12 * n34 * n43 + n13 * n32 * n44 - n12 * n33 * n44;
  float t13 = n13 * n24 * n42 - n14 * n23 * n42 + n14 * n22 * n43 - n12 * n24 * n43 - n13 * n22 * n44 + n12 * n23 * n44;
  float t14 = n14 * n23 * n32 - n13 * n24 * n32 - n14 * n22 * n33 + n12 * n24 * n33 + n13 * n22 * n34 - n12 * n23 * n34;
  float det = n11 * t11 + n21 * t12 + n31 * t13 + n41 * t14;
  float id = 1.0 / det;
  float4x4 ret;
  ret[0][0] = t11 * id;
  ret[0][1] = (n24 * n33 * n41 - n23 * n34 * n41 - n24 * n31 * n43 + n21 * n34 * n43 + n23 * n31 * n44 - n21 * n33 * n44) * id;
  ret[0][2] = (n22 * n34 * n41 - n24 * n32 * n41 + n24 * n31 * n42 - n21 * n34 * n42 - n22 * n31 * n44 + n21 * n32 * n44) * id;
  ret[0][3] = (n23 * n32 * n41 - n22 * n33 * n41 - n23 * n31 * n42 + n21 * n33 * n42 + n22 * n31 * n43 - n21 * n32 * n43) * id;
  ret[1][0] = t12 * id;
  ret[1][1] = (n13 * n34 * n41 - n14 * n33 * n41 + n14 * n31 * n43 - n11 * n34 * n43 - n13 * n31 * n44 + n11 * n33 * n44) * id;
  ret[1][2] = (n14 * n32 * n41 - n12 * n34 * n41 - n14 * n31 * n42 + n11 * n34 * n42 + n12 * n31 * n44 - n11 * n32 * n44) * id;
  ret[1][3] = (n12 * n33 * n41 - n13 * n32 * n41 + n13 * n31 * n42 - n11 * n33 * n42 - n12 * n31 * n43 + n11 * n32 * n43) * id;
  ret[2][0] = t13 * id;
  ret[2][1] = (n14 * n23 * n41 - n13 * n24 * n41 - n14 * n21 * n43 + n11 * n24 * n43 + n13 * n21 * n44 - n11 * n23 * n44) * id;
  ret[2][2] = (n12 * n24 * n41 - n14 * n22 * n41 + n14 * n21 * n42 - n11 * n24 * n42 - n12 * n21 * n44 + n11 * n22 * n44) * id;
  ret[2][3] = (n13 * n22 * n41 - n12 * n23 * n41 - n13 * n21 * n42 + n11 * n23 * n42 + n12 * n21 * n43 - n11 * n22 * n43) * id;
  ret[3][0] = t14 * id;
  ret[3][1] = (n13 * n24 * n31 - n14 * n23 * n31 + n14 * n21 * n33 - n11 * n24 * n33 - n13 * n21 * n34 + n11 * n23 * n34) * id;
  ret[3][2] = (n14 * n22 * n31 - n12 * n24 * n31 - n14 * n21 * n32 + n11 * n24 * n32 + n12 * n21 * n34 - n11 * n22 * n34) * id;
  ret[3][3] = (n12 * n23 * n31 - n13 * n22 * n31 + n13 * n21 * n32 - n11 * n23 * n32 - n12 * n21 * n33 + n11 * n22 * n33) * id;
  return transpose(ret);
}

#define nf_viewportHeight _ScreenParams.y
float3 uPinP[8];
float3 uPinD[8];
int uPinN;
float uPinR;
float3 nf_color;
float nf_opacity;
sampler2D nf_map;
float nf_rotation;
float2 nf_center;

// three.js attributes and varyings live in statics so helper functions can read them
static float3 position; static float3 normal; static float2 uv; static float3 color; static float3 instanceColor;
static float4 gl_Position; static float gl_PointSize; static float4 gl_FragColor; static float4 gl_FragCoord; static bool gl_FrontFacing; static float2 gl_PointCoord;
static float2 vUv;
static float3 vCol;

vec3 nfPuppet(vec3 w) {
vec3 sum = ((float3)(0.0));
float ws = 0.0;
for(int i = 0; i < 8; i ++) {
if(i >= uPinN) break;
vec3 d = w - uPinP[i];
float g = exp(-dot(d, d) / (uPinR * uPinR) * 3.0);
sum += uPinD[i] * g;
ws += g;
}
return w + sum / max(1.0, ws);
}


struct nf_appdata {
  float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; float4 color : COLOR;
  UNITY_VERTEX_INPUT_INSTANCE_ID
};
struct nf_v2f {
  float4 pos : SV_POSITION;
  float2 vUv : TEXCOORD0;
  float3 vCol : TEXCOORD1;

};

nf_v2f nf_vert(nf_appdata v) {
  nf_v2f o = (nf_v2f)0;
  UNITY_SETUP_INSTANCE_ID(v);
  position = v.vertex.xyz; normal = v.normal; uv = v.uv; color = v.color.rgb;
  instanceColor = float3(1, 1, 1);
  gl_Position = float4(0, 0, 0, 1);
  {
vUv = uv;
vCol = ((float3)(1.0));
vec4 mvPosition = mul(modelViewMatrix, float4(0.0, 0.0, 0.0, 1.0));
vec2 scale = float2(length(modelMatrix[0].xyz), length(modelMatrix[1].xyz));
vec2 al = (position.xy - (nf_center - ((float2)(0.5)))) * scale;
float s = sin(nf_rotation), c = cos(nf_rotation);
mvPosition.xy += float2(c * al.x - s * al.y, s * al.x + c * al.y);
gl_Position = mul(projectionMatrix, mvPosition);
if(uPinN > 0) {
mat4 nfVP = mul(projectionMatrix, viewMatrix);
vec4 nfW = mul(nf_inverse(nfVP), gl_Position);
gl_Position = mul(nfVP, float4(nfPuppet(nfW.xyz / nfW.w), 1.0));
}

  }
  o.pos = gl_Position;

  o.vUv = vUv;
  o.vCol = vCol;
  return o;
}

float4 nf_frag(nf_v2f i, float nf_facing : VFACE) : SV_Target {
  gl_FrontFacing = nf_facing > 0;
  gl_FragCoord = i.pos;
  gl_FragColor = float4(0, 0, 0, 0);

  vUv = i.vUv;
  vCol = i.vCol;
  {
vec4 c = float4(nf_color, nf_opacity);
c *= texture2D(nf_map, vUv);
gl_FragColor = c;

  }
  return gl_FragColor;
}
      ENDCG
    }
  }
}
