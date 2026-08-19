#ifndef LIGHTGAME_NOISE_UTILS_INCLUDED
#define LIGHTGAME_NOISE_UTILS_INCLUDED

// Shared by FogOverlay.shader (base dust pattern + light-edge jitter) and
// FogHalo.shader (organic halo edge around depth occluders) so both use
// identical noise, not separately drifting copies.

float Hash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float ValueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float a = Hash21(i);
    float b = Hash21(i + float2(1, 0));
    float c = Hash21(i + float2(0, 1));
    float d = Hash21(i + float2(1, 1));
    float2 u = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

// The drifting two-layer dust pattern shared by FogOverlay.shader (which draws
// the fog) and FogHalo.shader (which clears it back out near occluders). Both
// MUST feed the same worldUV/time/params so the halo subtracts exactly the fog
// that was painted - otherwise the "hole" reveals a mismatched pattern and the
// animation looks doubled. `time` is _Time.y at the call site.
float FogDustPattern(float2 worldUV, float time, float noiseScale, float2 scrollSpeed1, float2 scrollSpeed2, float noiseContrast)
{
    float2 fogUv = worldUV * noiseScale * 0.1;
    float f1 = ValueNoise(fogUv + time * scrollSpeed1);
    float f2 = ValueNoise(fogUv * 1.6 - time * scrollSpeed2 + f1 * 0.2);
    return lerp(0.5, saturate(f1 * 0.5 + f2 * 0.5), noiseContrast);
}

#endif
