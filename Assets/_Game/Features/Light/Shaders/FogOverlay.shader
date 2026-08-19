Shader "Hidden/LightGame/FogOverlay"
{
    // Drifting dust layer, drawn as its own quad (FogOverlayQuad) because
    // URP2D only draws one pass per object per frame - a second Pass block
    // in VoidLightBuffer.shader never actually executed.
    //
    // Occlusion by gameplay/foreground content is handled entirely by draw
    // order, not by any mask in this shader: FogOverlayQuad sits on the
    // "Default" sorting layer with an order between the Background (0) and
    // Gameplay (100) SortingGroups (see scene setup), so anything drawn
    // after it - gameplay objects, the player, foreground, overlay - simply
    // paints over the fog wherever it's actually in front, the same
    // painter's-algorithm way every other layer in this scene already
    // occludes the ones behind it. No stencil, no per-object marking, no
    // per-frame scanning.
    //
    // _EdgeWidth/_EdgeNoise* duplicate VoidLightBuffer's darkness curve so
    // fog visibility and void opacity stay exact complements - keep these in
    // sync with VoidLightBuffer.mat if either is retuned.
    Properties
    {
        _EdgeWidth("Edge Width (luminance)", Range(0.001, 0.5)) = 0.25
        _EdgeNoiseScale("Edge Noise Scale", Float) = 3
        _EdgeNoiseStrength("Edge Noise Strength", Range(0, 1)) = 0.2
        _EdgeNoiseSpeed("Edge Noise Speed", Vector) = (0.05, 0.03, 0, 0)

        _FogColor("Fog Color", Color) = (0.92, 0.96, 1, 1)
        _FogOpacity("Fog Opacity", Range(0, 1)) = 0.5
        _FogStrength("Fog Strength", Range(0, 1)) = 1
        _FogNoiseScale("Fog Noise Scale", Float) = 1.8
        _FogScrollSpeed1("Fog Scroll Speed 1", Vector) = (0.4, 0.22, 0, 0)
        _FogScrollSpeed2("Fog Scroll Speed 2", Vector) = (-0.3, 0.35, 0, 0)
        _FogNoiseContrast("Fog Noise Contrast", Range(0, 1)) = 0.65
        _FogDarkVisibility("Fog Visibility In Darkness", Range(0, 1)) = 0.02
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Fog"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "NoiseUtils.hlsl"

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
                half _EdgeWidth;
                half _EdgeNoiseScale;
                half _EdgeNoiseStrength;
                half4 _EdgeNoiseSpeed;
                half4 _FogColor;
                half _FogOpacity;
                half _FogStrength;
                half _FogNoiseScale;
                half4 _FogScrollSpeed1;
                half4 _FogScrollSpeed2;
                half _FogNoiseContrast;
                half _FogDarkVisibility;
            CBUFFER_END

            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.lightingUV = ComputeScreenPos(o.positionCS / o.positionCS.w).xy;
                o.worldUV = TransformObjectToWorld(v.positionOS.xyz).xy;
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 shapeLight = SAMPLE_TEXTURE2D(_ShapeLightTexture0, sampler_ShapeLightTexture0, input.lightingUV);
                float lightAmount = dot(shapeLight.rgb, half3(0.299, 0.587, 0.114));

                float2 edgeNoiseUv = input.worldUV * _EdgeNoiseScale * 0.1 + _Time.y * _EdgeNoiseSpeed.xy;
                float edgeNoise = ValueNoise(edgeNoiseUv) - 0.5;
                float noisyEdgeWidth = max(_EdgeWidth * (1.0 + edgeNoise * _EdgeNoiseStrength * 2.0), 0.0001);
                float darkness = 1.0 - smoothstep(0.0, noisyEdgeWidth, lightAmount);

                // A drifting noise layer (Ori-and-the-Blind-Forest style dust) that
                // only reads where the real light buffer says there's light to
                // catch it - near-invisible in the dark by construction. FogHalo
                // recomputes this exact pattern (shared FogDustPattern) to carve
                // clean holes back out of it near occluders.
                float fogPattern = FogDustPattern(input.worldUV, _Time.y, _FogNoiseScale, _FogScrollSpeed1.xy, _FogScrollSpeed2.xy, _FogNoiseContrast);

                float fogVisibility = lerp(_FogDarkVisibility, 1.0, 1.0 - darkness);
                float fogAlpha = saturate(fogPattern * _FogOpacity * _FogStrength * fogVisibility);

                return half4(_FogColor.rgb, fogAlpha);
            }
            ENDHLSL
        }
    }
}
