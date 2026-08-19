Shader "Hidden/LightGame/FogHalo"
{
    // Full-screen post-process pass, injected after CopyDepth (see the FogHalo
    // renderer feature entry in "New 2D Renderer Data.asset", gated to the Base
    // camera by FogHaloRendererFeature.cs). It carves a soft, drifting HOLE back
    // out of FogOverlay.shader's dust near anything that wrote real depth
    // (Sprite-Lit-Default-ZWrite.mat / PlayerColor.mat, positioned by the
    // SortingGroup-root + Player Z scheme).
    //
    // How the hole reads as a real see-through clearing rather than a flat tint:
    // FogOverlay already composited its dust into the scene color before this
    // pass runs, so we RECOMPUTE that exact drifting pattern here (shared
    // FogDustPattern in NoiseUtils.hlsl, same worldUV + _Time) and SUBTRACT it
    // back out inside the hole. Because the recomputed pattern is time-driven,
    // the hole animates with the same moving noise as the fog itself instead of
    // sitting there as a static colored border.
    //
    // The one term we can't recover is FogOverlay's light-buffer visibility gate
    // (_ShapeLightTexture0 is only valid during the sprite pass, long before
    // CopyDepth - same constraint as VoidLightBuffer.shader). We approximate it
    // from post-composite scene luminance: fog is only visible where it caught
    // light, and lit pixels are brighter, so brighter pixels get more fog
    // removed. This keeps dark/unlit areas (which had almost no fog painted)
    // from being over-subtracted into black.
    //
    // The fog-matching params below MUST mirror FogOverlay.mat or the subtracted
    // pattern won't match the painted one - a sync script keeps them equal.
    Properties
    {
        [Header(Hole shape)]
        _HaloDehazeStrength("Hole Strength", Range(0, 1)) = 0.9
        _HaloKernelRadius("Hole Radius (screen UV)", Range(0.001, 0.2)) = 0.08
        _HaloDepthThreshold("Occluder Min Gap (world units)", Float) = 22
        _HaloNoiseScale("Edge Noise Scale", Float) = 3
        _HaloNoiseStrength("Edge Noise Strength", Range(0, 1)) = 0.5
        _HaloNoiseSpeed("Edge Noise Speed", Vector) = (0.08, 0.05, 0, 0)

        [Header(Match FogOverlay kept in sync by script)]
        _FogColor("Fog Color", Color) = (0.92, 0.96, 1, 1)
        _FogOpacity("Fog Opacity", Range(0, 1)) = 0.5
        _FogStrength("Fog Strength", Range(0, 1)) = 1
        _FogNoiseScale("Fog Noise Scale", Float) = 1.8
        _FogScrollSpeed1("Fog Scroll Speed 1", Vector) = (0.4, 0.22, 0, 0)
        _FogScrollSpeed2("Fog Scroll Speed 2", Vector) = (-0.3, 0.35, 0, 0)
        _FogNoiseContrast("Fog Noise Contrast", Range(0, 1)) = 0.65
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "FogHalo"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "NoiseUtils.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _HaloDehazeStrength;
                half _HaloKernelRadius;
                half _HaloDepthThreshold;
                half _HaloNoiseScale;
                half _HaloNoiseStrength;
                half4 _HaloNoiseSpeed;
                half4 _FogColor;
                half _FogOpacity;
                half _FogStrength;
                half _FogNoiseScale;
                half4 _FogScrollSpeed1;
                half4 _FogScrollSpeed2;
                half _FogNoiseContrast;
            CBUFFER_END

            #define HALO_TAP_COUNT 8
            #define HALO_RING_COUNT 3

            // LinearEyeDepth(raw, _ZBufferParams) assumes a perspective camera's
            // hyperbolic depth distribution. This project's Main Camera is
            // orthographic, where depth is already linear in raw form - running
            // ortho raw-depth through the perspective reciprocal formula sends the
            // result toward +/-infinity/NaN (confirmed via Frame Debugger: this
            // pass's output went blank in the main scene, whose wider Z range hit
            // the blow-up the test scene's tiny Z range happened to dodge).
            // unity_OrthoParams.w is 1 for orthographic, 0 for perspective.
            float SampleLinearEyeDepth(float2 uv)
            {
                float rawDepth = SampleSceneDepth(uv);
                if (unity_OrthoParams.w > 0.5)
                {
                    #if UNITY_REVERSED_Z
                        rawDepth = 1.0 - rawDepth;
                    #endif
                    return lerp(_ProjectionParams.y, _ProjectionParams.z, rawDepth);
                }
                return LinearEyeDepth(rawDepth, _ZBufferParams);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                half4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float centerRawDepth = SampleSceneDepth(uv);
                float centerEyeDepth = SampleLinearEyeDepth(uv);

                // Soft occluder proximity: sample concentric rings outward and take
                // the strongest hit, where inner rings weigh more. A tap counts as
                // an occluder only if it's closer than the center by more than
                // _HaloDepthThreshold, so intra-group Z spread (a few units) is
                // ignored and only real front/back group separation opens a hole.
                // Multi-ring accumulation gives a smooth radial falloff instead of
                // the hard dilated-rectangle a single ring produced.
                float clear = 0.0;
                UNITY_UNROLL
                for (int r = 1; r <= HALO_RING_COUNT; r++)
                {
                    float ringRadius = _HaloKernelRadius * (r / (float) HALO_RING_COUNT);
                    float ringWeight = 1.0 - (r - 1) / (float) HALO_RING_COUNT;
                    UNITY_UNROLL
                    for (int i = 0; i < HALO_TAP_COUNT; i++)
                    {
                        float angle = (i / (float) HALO_TAP_COUNT) * (2.0 * PI);
                        float2 offset = float2(cos(angle), sin(angle)) * ringRadius;
                        float eyeDepth = SampleLinearEyeDepth(uv + offset);
                        if (centerEyeDepth - eyeDepth > _HaloDepthThreshold)
                            clear = max(clear, ringWeight);
                    }
                }

                // Ragged, drifting hole edge - perturb the mask with animated noise
                // so the boundary shimmers rather than sitting as a clean circle.
                float2 edgeNoiseUv = uv * _HaloNoiseScale * 10.0 + _Time.y * _HaloNoiseSpeed.xy;
                float edgeNoise = ValueNoise(edgeNoiseUv) - 0.5;
                clear = saturate(clear + edgeNoise * _HaloNoiseStrength) * _HaloDehazeStrength;

                // Recompute the exact drifting fog pattern FogOverlay painted here.
                // For an orthographic camera world XY is independent of depth, so
                // the reconstructed world position's XY matches the fog quad's
                // worldUV regardless of what depth this pixel sampled.
                float3 worldPos = ComputeWorldSpacePosition(uv, centerRawDepth, UNITY_MATRIX_I_VP);
                float fogPattern = FogDustPattern(worldPos.xy, _Time.y, _FogNoiseScale, _FogScrollSpeed1.xy, _FogScrollSpeed2.xy, _FogNoiseContrast);

                // Approximate FogOverlay's light-gated visibility from scene
                // luminance (see header) so we only pull fog back out where it was
                // actually painted, and never over-subtract dark areas into black.
                float sceneLum = dot(sceneColor.rgb, half3(0.299, 0.587, 0.114));
                float visApprox = smoothstep(0.02, 0.3, sceneLum);
                float fogAlpha = saturate(fogPattern * _FogOpacity * _FogStrength * visApprox);

                // Subtract the recomputed fog contribution inside the hole. Clamp at
                // 0 so it can only ever reveal what's behind the dust, never invert
                // into a colored border the way the old divide-based blend did.
                half3 result = sceneColor.rgb - _FogColor.rgb * fogAlpha * clear;
                return half4(max(result, 0.0), sceneColor.a);
            }
            ENDHLSL
        }
    }
}
