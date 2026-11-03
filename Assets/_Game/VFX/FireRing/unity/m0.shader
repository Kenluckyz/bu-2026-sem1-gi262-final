// Game Mellow VFX (Unity, Built-in + URP) · generated from the Lab's shader, do not edit
// Export ID: gRR42b5Kl8uc_ISDDRRV4zjbTFhNIhnJwM-V-IZvA6oY6Zm7Gfoa_AiWtGKfMKrvkJMJggKPFt_VkEKp
Shader "NightForge/6q4f204q5e/m0" {
  SubShader {
    Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
    Pass {
      Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
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
float uStyle;
float uTime;
float uAlpha;
float uGlow;
float uWobble;
float uCenter;
float uSeed;
float3 uPal[5];

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

vec2 hash2(vec2 p) {
return fract(sin(float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)))) * 43758.5453);
}

vec3 voro(vec2 x) {
vec2 n = floor(x), f = fract(x);
float d1 = 8.0, d2 = 8.0;
vec2 id = ((float2)(0.0));
for(int j = -1; j <= 1; j ++) for(int i = -1; i <= 1; i ++) {
vec2 g = float2(float(i), float(j));
vec2 o = hash2(n + g);
o = 0.5 + 0.4 * sin(uTime * 0.8 + 6.2831 * o);
vec2 r = g + o - f;
float d = dot(r, r);
if(d < d1) {
d2 = d1;
d1 = d;
id = n + g;
}
else if(d < d2) d2 = d;
}
return float3(sqrt(d2) - sqrt(d1), hash(id), sqrt(d1));
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
vec2 p = (vUv - 0.5) * 2.2;
float r = length(p);
float ang = atan2(p.y, p.x);
float R = 0.9 + (vnoise(float2(ang * 2.5 + uSeed, uTime * 0.6)) - 0.5) * 0.16 * uWobble + sin(ang * 14.0) * 0.012 * uWobble;
if(r > R + 0.08) discard;
float edge = 1.0 - smoothstep(R, R + 0.07, r);
vec3 col = uPal[1];
float a = 1.0;
float hot = 0.0;
int st = int(uStyle + 0.5);
if(st == 0) {
float tw = ang + r * 5.0 - uTime * 2.2;
float k = fbm2(float2(cos(tw), sin(tw)) * 2.0 + ((float2)(r * 4.0 - uTime * 0.6))) + r * 0.45;
if(k < 0.55) col = uPal[0];
else if(k < 0.75) col = uPal[1];
else if(k < 0.9) {
col = uPal[2];
hot = 0.6;
}
else {
col = uPal[3];
hot = 1.0;
}
a = mix(0.75, 1.0, smoothstep(0.2, 0.6, r));
}
else if(st == 1) {
float n = fbm2(p * 3.0 + float2(uTime * 0.4, -uTime * 0.3));
float nf_line = abs(sin(n * 14.0 + uTime * 1.5));
col = mix(uPal[1], uPal[2], smoothstep(0.0, 0.9, r));
if(nf_line < 0.12) {
col = uPal[3];
hot = 0.8;
}
a = 0.85;
}
else if(st == 2) {
vec3 v = voro(p * 4.0);
col = mix(uPal[0], uPal[1], smoothstep(0.0, 0.6, r));
if(v.y > 0.45 && v.z < 0.38) {
col = uPal[2];
hot = 0.6;
}
if(v.x < 0.05) col = mix(col, uPal[3], 0.5);
a = 0.9;
}
else if(st == 3) {
float n = fbm2(p * 3.2 + float2(0.0, -uTime * 1.2)) + 0.25 * (1.0 - r);
if(n < 0.45) {
col = uPal[0];
a = 0.75;
}
else if(n < 0.58) col = uPal[1];
else if(n < 0.7) {
col = uPal[2];
hot = 0.7;
}
else {
col = uPal[3];
hot = 1.0;
}
}
else if(st == 4) {
vec3 v = voro(p * 5.0 + 0.3);
col = mix(uPal[0], uPal[1], fbm2(p * 4.0));
if(v.x < 0.06) {
col = uPal[3];
hot = 0.8;
}
float rays = smoothstep(0.6, 1.0, r) * (0.5 + 0.5 * sin(ang * 90.0 + vnoise(float2(ang * 20.0, 0.0)) * 6.0));
col = mix(col, uPal[3], rays * 0.6);
hot = max(hot, rays * 0.6);
a = 0.85;
}
else if(st == 5) {
col = uPal[0];
a = 0.7;
}
else {
float n = fbm2(p * 2.5 + uTime * 0.15);
col = mix(uPal[0], uPal[1], smoothstep(0.35, 0.7, n));
col = mix(col, uPal[2], smoothstep(0.6, 0.85, n));
float s = step(0.985, hash(floor(p * 60.0)));
col = mix(col, uPal[4], s);
hot = max(hot, s);
a = 0.95;
}
float rimLine = smoothstep(R - 0.03, R - 0.008, r) * edge, rimBand = 0.22 * smoothstep(R - 0.16, R - 0.03, r) * edge;
col = mix(col, uPal[3], rimBand);
col = mix(col, uPal[4], rimLine);
hot = max(hot, max(rimLine, rimBand));
float cg = uCenter * exp(-r * r * 14.0);
col += uPal[4] * cg;
hot = max(hot, cg);
col *= mix(1.0, uGlow, hot);
gl_FragColor = float4(col, a * edge * uAlpha);

  }
  return gl_FragColor;
}
      ENDCG
    }
  }
}
