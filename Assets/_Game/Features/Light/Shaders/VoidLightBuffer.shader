Shader "Hidden/LightGame/VoidLightBuffer"
{
    // "Pure dark" playtest variant, take 2: instead of inferring darkness from
    // the final rendered pixel color (which can't tell a dark-toned lit object
    // from an actually-unlit one, and needs "ghost" duplicates of every object
    // to work at all - see LIGHT_BUFFER_HANDOFF.md for why that was abandoned),
    // this reads URP 2D's own live light accumulation buffer (_ShapeLightTexture0)
    // directly. That buffer is only valid during the same frame's 2D light+sprite
    // pass, so this can't be a post-process full-screen pass - it has to be a
    // normal scene-space quad drawn in the Overlay queue (see VoidBufferQuadFitter.cs
    // for how it's kept covering the camera view). Real alpha blending composites
    // the void fill over whatever's already drawn beneath, so lit objects (including
    // the player, with no ghost setup needed) show through correctly by construction.
    //
    // Void-only now - the fog dust layer lives in FogOverlay.shader on its own
    // quad (FogOverlayQuad). URP2D draws exactly one pass per object per frame,
    // so a second Pass block here for fog never actually executed - splitting
    // into a separate object is what lets fog be stencil-gated (see
    // FogOverlay.shader) independently of void darkness.
    Properties
    {
        _VoidColor("Void Color", Color) = (0.09, 0.08, 0.18, 1)
        _VoidColorDeep("Void Color Deep", Color) = (0.005, 0.016, 0.007, 1)
        _NoiseContrast("Overlay Opacity", Range(0, 1)) = 0.8
        _EdgeWidth("Edge Width (luminance)", Range(0.001, 0.5)) = 0.25
        _VoidStrength("Void Strength", Range(0, 1)) = 1
        _NoiseScale("Noise Scale", Float) = 4
        _ScrollSpeed1("Scroll Speed 1", Vector) = (0.25, 0.18, 0, 0)
        _ScrollSpeed2("Scroll Speed 2", Vector) = (-0.15, 0.3, 0, 0)
        _BreatheAmount("Breathe Amount", Range(0, 1)) = 0.35
        _BreatheSpeed("Breathe Speed (rad/s)", Float) = 1.2
        _EdgeNoiseScale("Edge Noise Scale", Float) = 3
        _EdgeNoiseStrength("Edge Noise Strength", Range(0, 1)) = 0.2
        _EdgeNoiseSpeed("Edge Noise Speed", Vector) = (0.05, 0.03, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Overlay" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Void"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 lightingUV : TEXCOORD0;
                float2 worldUV : TEXCOORD1;
            };

            TEXTURE2D(_ShapeLightTexture0);
            SAMPLER(sampler_ShapeLightTexture0);

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
                half _EdgeNoiseScale;
                half _EdgeNoiseStrength;
                half4 _EdgeNoiseSpeed;
            CBUFFER_END

            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.lightingUV = ComputeScreenPos(o.positionCS / o.positionCS.w).xy;
                o.worldUV = TransformObjectToWorld(v.positionOS.xyz).xy;
                return o;
            }

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
                half4 shapeLight = SAMPLE_TEXTURE2D(_ShapeLightTexture0, sampler_ShapeLightTexture0, input.lightingUV);
                float lightAmount = dot(shapeLight.rgb, half3(0.299, 0.587, 0.114));

                // Perturb the width of the threshold band (not lightAmount itself) so
                // the lit/dark boundary wobbles into an organic shape instead of
                // tracing the light's perfect radial falloff. Scaling the band width
                // keeps pixels with truly zero light exactly at darkness=1 (smoothstep
                // of 0 against any band width is still 0) instead of letting noise
                // fabricate patches of "light" out of nothing far from any real source.
                float2 edgeNoiseUv = input.worldUV * _EdgeNoiseScale * 0.1 + _Time.y * _EdgeNoiseSpeed.xy;
                float edgeNoise = ValueNoise(edgeNoiseUv) - 0.5;
                float noisyEdgeWidth = max(_EdgeWidth * (1.0 + edgeNoise * _EdgeNoiseStrength * 2.0), 0.0001);
                float darkness = 1.0 - smoothstep(0.0, noisyEdgeWidth, lightAmount);

                // 1) Flat base fill - this alone would be static.
                half3 voidBase = _VoidColor.rgb;

                // 2) Animated overlay: two drifting noise layers in world space (not
                // screen space) so the pattern doesn't swim as the camera moves,
                // contrast-punched into distinct moving blobs.
                float2 noiseUv = input.worldUV * _NoiseScale * 0.1;
                float n1 = ValueNoise(noiseUv + _Time.y * _ScrollSpeed1.xy);
                float n2 = ValueNoise(noiseUv * 1.7 - _Time.y * _ScrollSpeed2.xy + n1 * 0.15);
                float blobs = smoothstep(0.3, 0.7, saturate(n1 * 0.5 + n2 * 0.5));

                half3 overlaid = lerp(voidBase, _VoidColorDeep.rgb, blobs * _NoiseContrast);

                // 3) Slow full-screen brightness "breathing".
                float breathe = sin(_Time.y * _BreatheSpeed) * 0.5 + 0.5;
                half3 voidFill = overlaid * (1.0 + _BreatheAmount * (breathe - 0.5));

                float voidAlpha = saturate(darkness * _VoidStrength);

                return half4(voidFill, voidAlpha);
            }
            ENDHLSL
        }
    }
}
