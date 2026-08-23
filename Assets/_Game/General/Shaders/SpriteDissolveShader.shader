Shader "Custom/URP/SpriteDissolve"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Dissolve Settings)]
        _NoiseTexture ("Noise Texture", 2D) = "white" {}
        _NoiseScale ("Noise Scale", Float) = 1

        [Header(Edge Settings)]
        _EdgeWidth ("Edge Width", Range(0, 0.5)) = 0.1
        _EdgeColor ("Edge Color", Color) = (1, 0.5, 0, 1)
        _EdgeGlowIntensity ("Edge Glow Intensity", Range(0, 10)) = 2.0
        _InnerEdgeOffset ("Inner Edge Offset", Range(0, 0.2)) = 0.05

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
            Name "SpriteDissolve"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float fogFactor : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_NoiseTexture);
            SAMPLER(sampler_NoiseTexture);

            // Multi-region dissolve support
            #define MAX_REGIONS 32

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _NoiseScale;
                float _EdgeWidth;
                float4 _EdgeColor;
                float _EdgeGlowIntensity;
                float _InnerEdgeOffset;

                // Region data arrays (set from C# script)
                float4 _RegionData[MAX_REGIONS];      // x,y,width,height for each UV region
                float _RegionDissolve[MAX_REGIONS];   // dissolve amount per region (0=visible, 1=dissolved)
                int _RegionCount;                      // number of active regions
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;
                OUT.normalWS = normalInputs.normalWS;
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

                // Use unlit sprite color (sprites don't need lighting)
                half4 litColor = spriteColor;

                // Generate noise - try texture first, fallback to procedural
                half noise = SAMPLE_TEXTURE2D(_NoiseTexture, sampler_NoiseTexture, IN.uv * _NoiseScale).r;

                // If noise texture is white (not set), use procedural noise
                if (noise > 0.99)
                {
                    // Procedural noise generation
                    float2 noiseUV = IN.uv * _NoiseScale;
                    noise = frac(sin(dot(noiseUV, float2(12.9898, 78.233))) * 43758.5453);
                    noise = noise * 0.5 + 0.5; // Remap to 0-1 range
                }

                // Calculate smooth dissolve value using distance-based interpolation
                // Instead of hard region boundaries, we blend based on distance to region centers
                half finalDissolve = 0.0;
                half totalWeight = 0.0;

                for (int i = 0; i < _RegionCount; i++)
                {
                    float4 region = _RegionData[i];
                    float2 regionMin = region.xy;
                    float2 regionMax = region.xy + region.zw;
                    float2 regionCenter = (regionMin + regionMax) * 0.5;
                    float2 regionSize = region.zw;

                    // Calculate normalized distance from pixel to region center
                    float2 distToCenter = abs(IN.uv - regionCenter) / (regionSize * 0.5);
                    float normalizedDist = max(distToCenter.x, distToCenter.y);

                    // Create a smooth falloff weight
                    // Inside region (dist < 1): full weight
                    // Outside region: weight falls off smoothly
                    const half falloffRange = 2.0; // How far the influence extends beyond the region
                    half weight = 1.0 - saturate((normalizedDist - 1.0) / falloffRange);

                    // Use smooth falloff curve for more natural blending
                    weight = smoothstep(0.0, 1.0, weight);

                    // Accumulate weighted dissolve values
                    finalDissolve += _RegionDissolve[i] * weight;
                    totalWeight += weight;
                }

                // Normalize the dissolve value
                if (totalWeight > 0.0)
                {
                    finalDissolve /= totalWeight;
                }

                // Calculate dissolve effect with bright unlit edges
                // finalDissolve: 0 = fully visible, 1 = fully dissolved
                if (noise < finalDissolve)
                {
                    clip(-1); // Discard this pixel
                }
                
                // Only show edge glow when we're close to the dissolve threshold
                // This ensures glow only appears at actual dissolve boundaries
                half distanceToDissolve = noise - finalDissolve;
                half outerEdge = _EdgeWidth;
                half innerEdge = _InnerEdgeOffset;
                
                // Only apply edge glow if we're within edge width of the dissolve boundary
                // AND the dissolve value is significant enough to create a boundary
                if (finalDissolve > 0.01 && distanceToDissolve > 0 && distanceToDissolve < outerEdge)
                {
                    // We're in the edge region
                    half edgeFactor = distanceToDissolve / _EdgeWidth;
                    edgeFactor = smoothstep(0.0, 1.0, edgeFactor);
                    
                    // Calculate bright inner glow (unlit/emissive)
                    half glowMask = 0.0;
                    if (distanceToDissolve < innerEdge)
                    {
                        // Inner bright edge - this will be unlit
                        glowMask = 1.0 - (distanceToDissolve / _InnerEdgeOffset);
                        glowMask = smoothstep(0.0, 1.0, glowMask);
                    }
                    
                    // Create the bright unlit edge color
                    half4 unlitEdgeColor = _EdgeColor * _EdgeGlowIntensity;
                    unlitEdgeColor.a = _EdgeColor.a;
                    
                    // Blend between lit sprite and unlit bright edge
                    half4 finalColor = lerp(litColor, unlitEdgeColor, glowMask);
                    
                    // Fade to lit sprite at outer edge
                    finalColor = lerp(unlitEdgeColor * 0.5, finalColor, edgeFactor);
                    finalColor.a = litColor.a;
                    
                    return finalColor;
                }

                // Normal sprite rendering with lighting
                return litColor;
            }
            ENDHLSL
        }
    }
}
