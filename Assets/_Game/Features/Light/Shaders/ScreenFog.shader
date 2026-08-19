Shader "Hidden/LightGame/ScreenFog"
{
    // Full-screen dust/mist overlay in the style of Ori and the Blind Forest:
    // a soft white noise pattern that only really reads where there's light to
    // catch it (gated by the rendered pixel's own brightness), staying almost
    // invisible in unlit areas. Drawn after VoidOverlay so it sits on top of
    // both normal and voided pixels without disturbing VoidOverlay's own
    // luminance-based darkness read.
    Properties
    {
        _FogColor("Fog Color", Color) = (0.92, 0.96, 1, 1)
        _FogOpacity("Fog Opacity", Range(0, 1)) = 0.35
        _FogStrength("Fog Strength", Range(0, 1)) = 1
        _NoiseScale("Noise Scale", Float) = 1.8
        _ScrollSpeed1("Scroll Speed 1", Vector) = (0.02, 0.03, 0, 0)
        _ScrollSpeed2("Scroll Speed 2", Vector) = (-0.015, 0.025, 0, 0)
        _NoiseContrast("Noise Contrast", Range(0, 1)) = 0.65
        _HeightFalloff("Height Falloff", Range(0, 2)) = 0.15
        _DarkVisibility("Fog Visibility In Darkness", Range(0, 1)) = 0.03
        _LightGateSharpness("Light Gate Sharpness", Float) = 3
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ScreenFog"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend Off

            // Only draws over pixels stencil-tagged by objects on the FogReceiver layer
            // (written via the FogReceiverStencil Render Objects feature) - everything else
            // (gameplay tiles/obstacles, and anywhere untagged) stays fog-free.
            Stencil
            {
                Ref 1
                Comp Equal
                Pass Keep
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _FogColor;
                half _FogOpacity;
                half _FogStrength;
                half _NoiseScale;
                half4 _ScrollSpeed1;
                half4 _ScrollSpeed2;
                half _NoiseContrast;
                half _HeightFalloff;
                half _DarkVisibility;
                half _LightGateSharpness;
            CBUFFER_END

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

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                half4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float2 noiseUv = uv * _NoiseScale;
                float n1 = ValueNoise(noiseUv + _Time.y * _ScrollSpeed1.xy);
                float n2 = ValueNoise(noiseUv * 1.6 - _Time.y * _ScrollSpeed2.xy + n1 * 0.2);
                float pattern = saturate(n1 * 0.5 + n2 * 0.5);

                // At contrast 0 the fog is a uniform sheet; at contrast 1 it's fully patchy.
                float density = lerp(1.0, pattern, _NoiseContrast);

                // Denser toward the bottom of the screen (uv.y == 0) so it reads as ground mist.
                density = saturate(density + _HeightFalloff * (1.0 - uv.y) * 0.5);

                // Mist only really reads where there's light to catch it - fully dark
                // areas get just a faint hint (_DarkVisibility) instead of a flat wash,
                // which also keeps near-black corners from blowing out toward grey.
                float lightAmount = dot(sceneColor.rgb, half3(0.299, 0.587, 0.114));
                float litGate = saturate(lightAmount * _LightGateSharpness);
                float visibility = lerp(_DarkVisibility, 1.0, litGate);

                float fogAlpha = saturate(density * _FogOpacity * _FogStrength * visibility);

                half3 result = lerp(sceneColor.rgb, _FogColor.rgb, fogAlpha);
                return half4(result, sceneColor.a);
            }
            ENDHLSL
        }
    }
}
