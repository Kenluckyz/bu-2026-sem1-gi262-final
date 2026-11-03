// Game Mellow VFX (Unity, Built-in + URP) · generated from the Lab's shader, do not edit
// Export ID: gRR42b5Kl8uc_ISDDRRV4zjbTFhNIhnJwM-V-IZvA6oY6Zm7Gfoa_AiWtGKfMKrvkJMJggKPFt_VkEKp
Shader "NightForge/0u6b1m5z5z/m0" {
  SubShader {
    Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
    Pass {
      Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
      ZWrite Off
      ZTest LEqual
      Cull Off
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
float uPattern;
float uReveal;
float uErode;
float uFade;
float uTime;
float uGlow;
float uRimW;
float uWisp;
float3 uC[5];

// three.js attributes and varyings live in statics so helper functions can read them
static float3 position; static float3 normal; static float2 uv; static float3 color; static float3 instanceColor;
static float4 gl_Position; static float gl_PointSize; static float4 gl_FragColor; static float4 gl_FragCoord; static bool gl_FrontFacing; static float2 gl_PointCoord;
static float2 vUv;

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

float hash(vec2 p) {
return fract(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
}

float vnoise(vec2 p) {
vec2 i = floor(p), f = fract(p);
f = f * f * (3.0 - 2.0 * f);
return mix(mix(hash(i), hash(i + float2(1, 0)), f.x), mix(hash(i + float2(0, 1)), hash(i + float2(1, 1)), f.x), f.y);
}

float fbm2(vec2 p) {
float s = 0.0, a = 0.5;
for(int i = 0; i < 4; i ++) {
s += a * vnoise(p);
p *= 2.03;
a *= 0.5;
}
return s;
}


struct nf_appdata {
  float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; float4 color : COLOR;
  UNITY_VERTEX_INPUT_INSTANCE_ID
};
struct nf_v2f {
  float4 pos : SV_POSITION;
  float2 vUv : TEXCOORD0;

};

nf_v2f nf_vert(nf_appdata v) {
  nf_v2f o = (nf_v2f)0;
  UNITY_SETUP_INSTANCE_ID(v);
  position = v.vertex.xyz; normal = v.normal; uv = v.uv; color = v.color.rgb;
  instanceColor = float3(1, 1, 1);
  gl_Position = float4(0, 0, 0, 1);
  {
vUv = uv;
gl_Position = mul(mul(projectionMatrix, modelViewMatrix), float4(position, 1.0));
if(uPinN > 0) {
mat4 nfVP = mul(projectionMatrix, viewMatrix);
vec4 nfW = mul(nf_inverse(nfVP), gl_Position);
gl_Position = mul(nfVP, float4(nfPuppet(nfW.xyz / nfW.w), 1.0));
}

  }
  o.pos = gl_Position;

  o.vUv = vUv;
  return o;
}

float4 nf_frag(nf_v2f i, float nf_facing : VFACE) : SV_Target {
  gl_FrontFacing = nf_facing > 0;
  gl_FragCoord = i.pos;
  gl_FragColor = float4(0, 0, 0, 0);

  vUv = i.vUv;
  {
float u = vUv.x, v = vUv.y;
if(u > uReveal) discard;
float n = fbm2(float2(u * 7.0, v * 4.0) + uTime * 0.7);
if(u < uErode * 1.2 - 0.1 + (n - 0.5) * 0.35) discard;
float streak = fbm2(float2(u * 2.5 - uTime * 2.2, v * 12.0));
float inner = v + (streak - 0.5) * uWisp;
if(inner < 0.2) discard;
float t = clamp((inner - 0.2) / 0.8, 0.0, 1.0);
vec3 col;
float hot = 0.0;
if(uPattern < 0.5) {
float k = t * 0.85 + (streak - 0.5) * 0.5 + 0.1;
if(k < 0.3) col = uC[0];
else if(k < 0.55) col = uC[1];
else if(k < 0.8) {
col = uC[2];
hot = 0.6;
}
else {
col = uC[3];
hot = 1.0;
}
}
else if(uPattern < 1.5) {
col = mix(uC[0], uC[1], smoothstep(0.0, 0.9, t));
col = mix(col, uC[2], smoothstep(0.55, 0.8, streak) * 0.6);
vec2 g = float2(u * 28.0, v * 4.0);
vec2 id = floor(g);
vec2 f = fract(g) - 0.5;
float r = hash(id);
if(r > 0.45) {
vec2 o = (float2(hash(id + 3.1), hash(id + 7.7)) - 0.5) * 0.5;
vec2 q = f - o;
q.y *= 0.7;
float s = mix(0.08, 0.24, hash(id + 1.3)) * (0.75 + 0.25 * sin(uTime * 14.0 + r * 40.0));
float arms = max(0.0, 1.0 - abs(q.x * q.y) / (s * s * 0.05)) * max(0.0, 1.0 - length(q) / s);
float core = smoothstep(s * 0.25, 0.0, length(q));
col = mix(col, uC[3] * uGlow, clamp(arms * 1.6 + core, 0.0, 1.0));
}
col = mix(col, uC[3], step(0.986, hash(floor(float2(u * 170.0, v * 18.0)))) * 0.8);
}
else if(uPattern < 2.5) {
float k = t * 0.8 + (streak - 0.5) * 0.45 + 0.1;
if(k < 0.35) col = uC[0];
else if(k < 0.65) col = uC[1];
else {
col = uC[2];
hot = 0.7;
}
}
else {
col = mix(uC[0], uC[1], smoothstep(0.0, 0.6, t));
col = mix(col, uC[2], smoothstep(0.5, 1.0, t));
vec2 g = float2(u * 34.0, v * 5.0);
vec2 id = floor(g);
vec2 f = fract(g) - 0.5;
f.y *= 0.8;
float r = hash(id);
if(r > 0.45) {
float s = mix(0.12, 0.38, hash(id + 2.0));
float box = max(abs(f.x), abs(f.y));
if(r > 0.8) {
if(box < s) {
col = mix(col, uC[3], 0.85);
hot = 0.5;
}
}
else if(abs(box - s) < 0.045) col = mix(col, uC[3], 0.7);
}
}
if(v > 1.0 - uRimW) {
col = uC[4];
hot = 1.0;
}
else if(v > 1.0 - uRimW * 2.2) {
col = mix(col, uC[3], 0.6);
hot = max(hot, 0.6);
}
col *= mix(1.0, uGlow, hot);
gl_FragColor = float4(col, uFade);

  }
  return gl_FragColor;
}
      ENDCG
    }
  }
}
