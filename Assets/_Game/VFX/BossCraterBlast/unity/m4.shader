// Game Mellow VFX (Unity, Built-in + URP) · generated from the Lab's shader, do not edit
// Export ID: KWplS7ypCpXtoySbfMTK9BwZ44gBaJfBX2Wx26Uf6PxIs_m2bc7o8yGkyZBB1F-Welm_d2rCkyNSnt9J
Shader "NightForge/4z6w6s254y/m4" {
  SubShader {
    Tags { "Queue"="Geometry" "RenderType"="Opaque" "IgnoreProjector"="True" }
    Pass {
      Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
      ZWrite On
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
float3 uVein;
float3 uHot;
float3 uRim;
float uT;
float3 nf_color;
float4 nf_emissive;
float2 nf_pbr;
float nf_opacity;
float3 nf_hemiSky;
float3 nf_hemiGround;
float3 nf_dirDir;
float3 nf_dirCol;
float4 nf_plPos[4];
float4 nf_plCol[4];

// three.js attributes and varyings live in statics so helper functions can read them
static float3 position; static float3 normal; static float2 uv; static float3 color; static float3 instanceColor;
static float4 gl_Position; static float gl_PointSize; static float4 gl_FragColor; static float4 gl_FragCoord; static bool gl_FrontFacing; static float2 gl_PointCoord;
static float3 vRP;
static float3 nf_vN;
static float3 nf_vW;
static float3 nf_vCol;

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

float h2(vec2 p) {
return fract(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

float n2(vec2 p) {
vec2 i = floor(p), f = fract(p);
f = f * f * (3.0 - 2.0 * f);
return mix(mix(h2(i), h2(i + float2(1, 0)), f.x), mix(h2(i + float2(0, 1)), h2(i + 1.0), f.x), f.y);
}

float fbm(vec2 p) {
return 0.55 * n2(p) + 0.3 * n2(p * 2.1 + 3.1) + 0.15 * n2(p * 4.3 + 7.7);
}

float h3(vec3 p) {
p = fract(p * 0.3183099 + 0.1);
p *= 17.0;
return fract(p.x * p.y * p.z * (p.x + p.y + p.z));
}

float n3(vec3 x) {
vec3 i = floor(x), f = fract(x);
f = f * f * (3.0 - 2.0 * f);
return mix(mix(mix(h3(i), h3(i + float3(1, 0, 0)), f.x), mix(h3(i + float3(0, 1, 0)), h3(i + float3(1, 1, 0)), f.x), f.y), mix(mix(h3(i + float3(0, 0, 1)), h3(i + float3(1, 0, 1)), f.x), mix(h3(i + float3(0, 1, 1)), h3(i + 1.0), f.x), f.y), f.z);
}


struct nf_appdata {
  float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; float4 color : COLOR;
  UNITY_VERTEX_INPUT_INSTANCE_ID
};
struct nf_v2f {
  float4 pos : SV_POSITION;
  float3 vRP : TEXCOORD0;
  float3 nf_vN : TEXCOORD1;
  float3 nf_vW : TEXCOORD2;
  float3 nf_vCol : TEXCOORD3;

};

nf_v2f nf_vert(nf_appdata v) {
  nf_v2f o = (nf_v2f)0;
  UNITY_SETUP_INSTANCE_ID(v);
  position = v.vertex.xyz; normal = v.normal; uv = v.uv; color = v.color.rgb;
  instanceColor = float3(1, 1, 1);
  gl_Position = float4(0, 0, 0, 1);
  {
vRP = position;
vec4 w = mul(modelMatrix, float4(position, 1.0));
nf_vN = mul(((float3x3)(modelMatrix)), normal);
nf_vW = w.xyz;
nf_vCol = ((float3)(1.0));
gl_Position = mul(projectionMatrix, (mul(viewMatrix, w)));
if(uPinN > 0) {
mat4 nfVP = mul(projectionMatrix, viewMatrix);
vec4 nfW = mul(nf_inverse(nfVP), gl_Position);
gl_Position = mul(nfVP, float4(nfPuppet(nfW.xyz / nfW.w), 1.0));
}

  }
  o.pos = gl_Position;

  o.vRP = vRP;
  o.nf_vN = nf_vN;
  o.nf_vW = nf_vW;
  o.nf_vCol = nf_vCol;
  return o;
}

float4 nf_frag(nf_v2f i, float nf_facing : VFACE) : SV_Target {
  gl_FrontFacing = nf_facing > 0;
  gl_FragCoord = i.pos;
  gl_FragColor = float4(0, 0, 0, 0);

  vRP = i.vRP;
  nf_vN = i.nf_vN;
  nf_vW = i.nf_vW;
  nf_vCol = i.nf_vCol;
  {
vec3 geometryViewDir = normalize(cameraPosition - nf_vW);
vec3 normal = normalize(cross(dFdx(nf_vW), dFdy(nf_vW)));
if(dot(normal, geometryViewDir) < 0.0) normal = -normal;
vec3 irr = mix(nf_hemiGround, nf_hemiSky, 0.5 * normal.y + 0.5) + nf_dirCol * max(dot(normal, nf_dirDir), 0.0);
for(int k = 0; k < 4; k ++) {
vec3 L = nf_plPos[k].xyz - nf_vW;
float d = length(L), cut = nf_plPos[k].w;
float fall = 1.0 / max(pow(d, nf_plCol[k].w), 0.01);
if(cut > 0.0) {
float r = clamp(1.0 - pow(d / cut, 4.0), 0.0, 1.0);
fall *= r * r;
}
irr += nf_plCol[k].rgb * fall * max(dot(normal, L / max(d, 1e-4)), 0.0);
}
vec3 totalEmissiveRadiance = nf_emissive.rgb * nf_emissive.a;
float rv = abs(n3(vRP * 2.6 + 3.0) - 0.5);
float vein = 1.0 - smoothstep(0.0, 0.05, rv);
vein *= 0.75 + 0.25 * sin(uT * 4.0 + vRP.y * 6.0);
totalEmissiveRadiance += mix(uVein, uHot, vein * vein) * vein + uRim * pow(1.0 - clamp(dot(normal, geometryViewDir), 0.0, 1.0), 2.2);
vec3 base = nf_color * nf_vCol * (1.0 - nf_pbr.y);
gl_FragColor = float4(base * irr * 0.3183099 + totalEmissiveRadiance, nf_opacity);

  }
  return gl_FragColor;
}
      ENDCG
    }
  }
}
