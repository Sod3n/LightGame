Shader "Custom/URP/RimLighting"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Rim Lighting Settings)]
        _RimColor ("Rim Color", Color) = (0.5, 0.8, 1, 1)
        _RimPower ("Rim Power", Range(0.1, 8.0)) = 2.0
        _RimIntensity ("Rim Intensity", Range(0, 5)) = 2.0
        _RimWidth ("Rim Width (Pixels)", Range(1, 20)) = 5

        [Header(Darkness Settings)]
        _DarknessThreshold ("Darkness Threshold", Range(0, 1)) = 0.3
        _DarknessFalloff ("Darkness Falloff", Range(0.1, 2)) = 0.5

        [Header(Core Visibility)]
        _CoreBrightness ("Core Brightness in Dark", Range(0, 1)) = 0.1

        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
        }

        LOD 100
        Blend [_SrcBlend] [_DstBlend]
        ZWrite Off
        Cull Off

        Pass
        {
            Name "RimLighting"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 positionWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _RimColor;
                float _RimPower;
                float _RimIntensity;
                float _RimWidth;
                float _DarknessThreshold;
                float _DarknessFalloff;
                float _CoreBrightness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);

                OUT.positionHCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                OUT.fogFactor = ComputeFogFactor(OUT.positionHCS.z);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Sample sprite texture
                half4 spriteColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                spriteColor *= _Color * IN.color;

                // Discard fully transparent pixels
                if (spriteColor.a < 0.01)
                    discard;

                // Calculate lighting
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                // Calculate total illumination from all lights
                half3 totalLighting = mainLight.color * mainLight.distanceAttenuation * mainLight.shadowAttenuation;

                #ifdef _ADDITIONAL_LIGHTS
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex, IN.positionWS);
                    totalLighting += light.color * light.distanceAttenuation * light.shadowAttenuation;
                }
                #endif

                // Calculate darkness factor (0 = dark, 1 = bright)
                half lightIntensity = saturate(length(totalLighting));
                half darknessFactor = smoothstep(0, _DarknessThreshold, lightIntensity);
                darknessFactor = pow(darknessFactor, _DarknessFalloff);

                // Calculate rim lighting using neighbor search method
                // This method is scale-independent and works perfectly for any sprite shape!

                // Get the UV derivatives to understand texture pixel size
                float2 uvDDX = ddx(IN.uv);
                float2 uvDDY = ddy(IN.uv);
                float2 baseTexelSize = float2(length(uvDDX), length(uvDDY));

                // Calculate texel size for sampling
                float2 texelSize = (_RimWidth / 2.0) * baseTexelSize;

                // Current pixel alpha
                half currentAlpha = spriteColor.a;
                half minNeighborAlpha = currentAlpha;

                // Search surrounding pixels in a fixed 5x5 grid (unrollable)
                // This is scale-independent and works for any sprite shape
                [unroll]
                for (int x = -2; x <= 2; x++)
                {
                    [unroll]
                    for (int y = -2; y <= 2; y++)
                    {
                        if (x == 0 && y == 0) continue; // Skip center pixel

                        float2 offset = float2(x, y) * texelSize;
                        half neighborAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + offset).a;
                        minNeighborAlpha = min(minNeighborAlpha, neighborAlpha);
                    }
                }

                // Calculate rim factor based on edge detection
                // If we have high alpha but neighbors have low alpha, we're at the edge
                half rimFactor = 0.0;
                if (currentAlpha >= 0.5 && minNeighborAlpha < 0.5)
                {
                    rimFactor = 1.0 - (minNeighborAlpha / 0.5);
                    rimFactor = pow(rimFactor, _RimPower);
                }

                // Rim is stronger in darkness
                half rimStrength = rimFactor * _RimIntensity * (1.0 - darknessFactor);

                // Create rim color (preserve sprite color hue but add rim glow)
                half3 rimColorFinal = _RimColor.rgb * spriteColor.rgb;

                // In darkness: show mostly rim + slight core visibility
                // In light: show normal sprite color
                half3 darkColor = lerp(
                    spriteColor.rgb * _CoreBrightness,  // Dim core
                    rimColorFinal,                       // Bright rim
                    rimStrength
                );

                // Blend between dark appearance and lit appearance
                half3 finalColor = lerp(darkColor, spriteColor.rgb, darknessFactor);

                // Apply fog
                finalColor = MixFog(finalColor, IN.fogFactor);

                return half4(finalColor, spriteColor.a);
            }
            ENDHLSL
        }

        // Shadow caster pass for proper shadow rendering
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
            CBUFFER_END

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                // For 2D sprites, use a simple forward-facing normal
                float3 normalWS = float3(0, 0, -1);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(vertexInput.positionWS, normalWS, _LightDirection));

                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                clip(texColor.a - 0.01);
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Sprites/Default"
}
