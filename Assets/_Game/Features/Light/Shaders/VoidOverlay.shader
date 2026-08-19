Shader "Hidden/LightGame/VoidOverlay"
{
    // Full-screen post pass for the "pure dark" playtest variant. Anywhere the
    // scene renders near-black (i.e. outside every Light2D's reach, since the
    // 2D Renderer's Multiply blend style already drives unlit pixels to 0)
    // gets replaced with a flat void color, THEN an animated "living darkness"
    // layer (drifting high-contrast blobs + a slow full-screen breathing pulse)
    // is blended on top of that - motion is a separate, deliberately obvious
    // signal instead of being baked subtly into the base fill color. Runs after
    // post-processing so tonemapping/color grading can't crush the fill back
    // down to black.
    Properties
    {
        _VoidColor("Void Color", Color) = (0.07, 0.06, 0.14, 1)
        _VoidColorDeep("Void Color Deep", Color) = (0.02, 0.015, 0.045, 1)
        _NoiseContrast("Overlay Opacity", Range(0, 1)) = 0.8
        _EdgeWidth("Edge Width (luminance)", Range(0.001, 0.5)) = 0.08
        _VoidStrength("Void Strength", Range(0, 1)) = 1
        _NoiseScale("Noise Scale", Float) = 4
        _ScrollSpeed1("Scroll Speed 1", Vector) = (0.25, 0.18, 0, 0)
        _ScrollSpeed2("Scroll Speed 2", Vector) = (-0.15, 0.3, 0, 0)
        _BreatheAmount("Breathe Amount", Range(0, 1)) = 0.35
        _BreatheSpeed("Breathe Speed (rad/s)", Float) = 1.2
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "VoidOverlay"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _VoidColor;
                half4 _VoidColorDeep;
                half _NoiseContrast;
                half _EdgeWidth;
                half _VoidStrength;
                half _NoiseScale;
                half4 _ScrollSpeed1;
                half4 _ScrollSpeed2;
                half _BreatheAmount;
                half _BreatheSpeed;
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

                float lightAmount = dot(sceneColor.rgb, half3(0.299, 0.587, 0.114));
                float darkness = 1.0 - smoothstep(0.0, max(_EdgeWidth, 0.0001), lightAmount);

                // 1) Flat base fill - this alone would be static.
                half3 voidBase = _VoidColor.rgb;

                // 2) Animated overlay drawn on top: two drifting noise layers,
                // contrast-punched into distinct moving blobs (smoothstep, not a
                // smooth gradient) so the drift is unmistakable frame to frame.
                float2 noiseUv = uv * _NoiseScale;
                float n1 = ValueNoise(noiseUv + _Time.y * _ScrollSpeed1.xy);
                float n2 = ValueNoise(noiseUv * 1.7 - _Time.y * _ScrollSpeed2.xy + n1 * 0.15);
                float blobs = smoothstep(0.3, 0.7, saturate(n1 * 0.5 + n2 * 0.5));

                half3 overlaid = lerp(voidBase, _VoidColorDeep.rgb, blobs * _NoiseContrast);

                // 3) Slow full-screen brightness "breathing" - a second, redundant
                // motion signal that reads even if the spatial drift above is too
                // subtle at a glance.
                float breathe = sin(_Time.y * _BreatheSpeed) * 0.5 + 0.5;
                half3 voidFill = overlaid * (1.0 + _BreatheAmount * (breathe - 0.5));

                half3 result = lerp(sceneColor.rgb, voidFill, darkness * _VoidStrength);

                return half4(result, sceneColor.a);
            }
            ENDHLSL
        }
    }
}
