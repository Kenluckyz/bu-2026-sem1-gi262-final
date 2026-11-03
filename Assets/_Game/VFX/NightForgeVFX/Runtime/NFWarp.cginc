// Game Mellow VFX: the Lab's screen warp (shockwave ripples + heat haze), set by NFEffect.
// Export ID: gRR42b5Kl8uc_ISDDRRV4zjbTFhNIhnJwM-V-IZvA6oY6Zm7Gfoa_AiWtGKfMKrvkJMJggKPFt_VkEKp
float4 _NfWarpA; // age of ripple 0, age of ripple 1 (-1 = off), heat, heat radius
float4 _NfWarpB; // centre (viewport 0..1), time, on

float2 nf_wave(float2 uv, float age, float asp)
{
    if (age < 0.0 || age > 0.7) return float2(0, 0);
    float2 d = uv - _NfWarpB.xy; d.x *= asp;
    float r = length(d), R = 0.04 + age * 0.9, w = 0.035 + age * 0.05;
    return normalize(d + 1e-5) * exp(-pow((r - R) / w, 2.0)) * (1.0 - age / 0.7);
}

// vUv: viewport position; returns (uv offset, chromatic offset) in viewport units
float4 nf_warp(float2 vUv)
{
    float asp = _ScreenParams.x / _ScreenParams.y;
    float2 d = vUv - _NfWarpB.xy; d.x *= asp;
    float m = 1.0 - smoothstep(_NfWarpA.w * 0.4, _NfWarpA.w, length(d));
    float t = _NfWarpB.z;
    float2 heat = float2(sin(vUv.y * 90.0 + t * 7.0), cos(vUv.x * 80.0 - t * 6.0)) * 0.0025 * _NfWarpA.z * m;
    float2 dir = nf_wave(vUv, _NfWarpA.x, asp) + nf_wave(vUv, _NfWarpA.y, asp); dir.x /= asp;
    return float4(-dir * 0.035 + heat, dir * 0.008);
}

float4 nf_warp_sample(sampler2D tex, float2 uv, float4 w, float2 s)
{
    float2 o = uv + w.xy * s, ca = w.zw * s;
    float4 c = tex2D(tex, o);
    return float4(tex2D(tex, o + ca).r, c.g, tex2D(tex, o - ca).b, c.a);
}
