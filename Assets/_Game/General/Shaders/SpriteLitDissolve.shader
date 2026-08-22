// URP 2D Sprite-Lit shader with a world-space multi-sphere dissolve cutout and a glowing,
// time-drifting edge (matching the drift used by the Void/light shaders).
// Reproduces Amazing Assets/Advanced Dissolve's geometric Sphere cutout (up to 4 spheres) on a
// 2D-lit base, so the sprite receives Light2D and 2D shadows while dissolving where lights touch it.
//
// Driven by SimpleBlockToSpriteSync:
//   _CutoutSpheres[k] = (centerX, centerY, 0, radius)  // world space; unused slots => radius 0, far center
//   _DissolveInvert   0 = dissolve OUTSIDE spheres, 1 = dissolve INSIDE (default)
Shader "LightGame/2D/SpriteLitDissolve"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}

        [Header(Dissolve)]
        [MaterialToggle] _DissolveInvert("Dissolve Inside Spheres", Float) = 1
        _EdgeWidth("Edge Width (world units)", Range(0.0001, 2)) = 0.4
        [HDR] _EdgeColor("Edge Color (outer)", Color) = (1, 0.749, 0.282, 1)
        [HDR] _EdgeColor2("Edge Color (burn)", Color) = (1, 0, 0, 1)

        [Header(Edge Drift (matches light shaders))]
        _EdgeNoiseScale("Edge Noise Scale", Float) = 3
        _EdgeNoiseStrength("Edge Noise Strength (world units)", Range(0, 1)) = 0.3
        _EdgeNoiseSpeed("Edge Noise Speed", Vector) = (0.05, 0.03, 0, 0)

        [MaterialToggle] _ZWrite("ZWrite", Float) = 0

        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        // Shared dissolve math (identical CBUFFER + helpers in every pass for SRP batcher).
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float4 _CutoutSpheres[4];
            float _DissolveInvert;
            float _EdgeWidth;
            half4 _EdgeColor;
            half4 _EdgeColor2;
            float _EdgeNoiseScale;
            float _EdgeNoiseStrength;
            float4 _EdgeNoiseSpeed;
        CBUFFER_END

        float DisHash21(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 45.32);
            return frac(p.x * p.y);
        }

        float DisValueNoise(float2 p)
        {
            float2 i = floor(p);
            float2 f = frac(p);
            float a = DisHash21(i);
            float b = DisHash21(i + float2(1, 0));
            float c = DisHash21(i + float2(0, 1));
            float d = DisHash21(i + float2(1, 1));
            float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0); // quintic (smoother than cubic)
            return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
        }

        // Returns soft alpha [0..1] (0 = dissolved), edge glow [0..1], and the signed cut.
        float SphereCut(float2 wxy, out float edge, out float alpha)
        {
            float cut = -1e9;
            [unroll]
            for (int k = 0; k < 4; k++)
                cut = max(cut, _CutoutSpheres[k].w - length(wxy - _CutoutSpheres[k].xy));

            // Time-drifting boundary distortion (same smooth value-noise as the Void/light shaders).
            float2 en = wxy * _EdgeNoiseScale + _Time.y * _EdgeNoiseSpeed.xy;
            cut += (DisValueNoise(en) - 0.5) * _EdgeNoiseStrength;

            if (_DissolveInvert < 0.5) cut = -cut;

            float ew = max(_EdgeWidth, 1e-4);
            alpha = 1.0 - smoothstep(-ew, 0.0, cut);     // soft feathered rim over the edge width
            edge = saturate(1.0 + cut / ew) * step(cut, 0.0);
            return cut;
        }
        ENDHLSL

        // ---------------------------------------------------------------------
        // 2D lit pass — receives Light2D + 2D shadows.
        // ---------------------------------------------------------------------
        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex CombinedShapeLightVertex
            #pragma fragment CombinedShapeLightFragment

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                float3 normal       : NORMAL;
                UNITY_SKINNED_VERTEX_INPUTS
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS  : SV_POSITION;
                half4   color       : COLOR;
                float2  uv          : TEXCOORD0;
                half2   lightingUV  : TEXCOORD1;
                float2  positionWS  : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            #if USE_SHAPE_LIGHT_TYPE_0
            SHAPE_LIGHT(0)
            #endif
            #if USE_SHAPE_LIGHT_TYPE_1
            SHAPE_LIGHT(1)
            #endif
            #if USE_SHAPE_LIGHT_TYPE_2
            SHAPE_LIGHT(2)
            #endif
            #if USE_SHAPE_LIGHT_TYPE_3
            SHAPE_LIGHT(3)
            #endif

            Varyings CombinedShapeLightVertex(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_SKINNED_VERTEX_COMPUTE(v);

                SetUpSpriteInstanceProperties();
                v.positionOS = UnityFlipSprite(v.positionOS, unity_SpriteProps.xy);
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.positionWS = TransformObjectToWorld(v.positionOS).xy;
                o.uv = v.uv;
                o.lightingUV = half2(ComputeScreenPos(o.positionCS / o.positionCS.w).xy);
                o.color = v.color * _Color * unity_SpriteColor;
                return o;
            }

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

            half4 CombinedShapeLightFragment(Varyings i) : SV_Target
            {
                float edge, alpha;
                SphereCut(i.positionWS, edge, alpha);
                float outA = saturate(max(alpha, edge * _EdgeColor.a)); // keep the burning rim visible
                clip(outA - 0.01);

                const half4 main = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                const half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, i.uv);
                const half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.uv));

                SurfaceData2D surfaceData;
                InputData2D inputData;
                InitializeSurfaceData(main.rgb, main.a, mask, normalTS, surfaceData);
                InitializeInputData(i.uv, i.lightingUV, inputData);

                half4 lit = CombinedShapeLightShared(surfaceData, inputData);

                half3 edgeCol = lerp(_EdgeColor.rgb, _EdgeColor2.rgb, edge);
                lit.rgb = lerp(lit.rgb, edgeCol, saturate(edge * _EdgeColor.a));
                lit.a *= outA;
                return lit;
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------------
        // Normals pass — clip dissolved pixels so they don't write normals.
        // ---------------------------------------------------------------------
        Pass
        {
            Tags { "LightMode" = "NormalsRendering"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex NormalsRenderingVertex
            #pragma fragment NormalsRenderingFragment

            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                float3 normal       : NORMAL;
                float4 tangent      : TANGENT;
                UNITY_SKINNED_VERTEX_INPUTS
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS      : SV_POSITION;
                half4   color           : COLOR;
                float2  uv              : TEXCOORD0;
                half3   normalWS        : TEXCOORD1;
                half3   tangentWS       : TEXCOORD2;
                half3   bitangentWS     : TEXCOORD3;
                float2  positionWS      : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            Varyings NormalsRenderingVertex(Attributes attributes)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(attributes);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_SKINNED_VERTEX_COMPUTE(attributes);

                SetUpSpriteInstanceProperties();
                attributes.positionOS = UnityFlipSprite(attributes.positionOS, unity_SpriteProps.xy);
                o.positionCS = TransformObjectToHClip(attributes.positionOS);
                o.positionWS = TransformObjectToWorld(attributes.positionOS).xy;
                o.uv = attributes.uv;
                o.color = attributes.color * _Color * unity_SpriteColor;
                o.normalWS = TransformObjectToWorldDir(attributes.normal);
                o.tangentWS = TransformObjectToWorldDir(attributes.tangent.xyz);
                o.bitangentWS = cross(o.normalWS, o.tangentWS) * attributes.tangent.w;
                return o;
            }

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/NormalsRenderingShared.hlsl"

            half4 NormalsRenderingFragment(Varyings i) : SV_Target
            {
                float edge, alpha;
                SphereCut(i.positionWS, edge, alpha);
                clip(alpha - 0.01);
                const half4 mainTex = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                const half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.uv));
                return NormalsRenderingShared(mainTex, normalTS, i.tangentWS.xyz, i.bitangentWS.xyz, i.normalWS.xyz);
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------------
        // Unlit forward fallback (non-2D cameras).
        // ---------------------------------------------------------------------
        Pass
        {
            Tags { "LightMode" = "UniversalForward" "Queue"="Transparent" "RenderType"="Transparent"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_SKINNED_VERTEX_INPUTS
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS      : SV_POSITION;
                float4  color           : COLOR;
                float2  uv              : TEXCOORD0;
                float2  positionWS      : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings UnlitVertex(Attributes attributes)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(attributes);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_SKINNED_VERTEX_COMPUTE(attributes);

                SetUpSpriteInstanceProperties();
                attributes.positionOS = UnityFlipSprite(attributes.positionOS, unity_SpriteProps.xy);
                o.positionCS = TransformObjectToHClip(attributes.positionOS);
                o.positionWS = TransformObjectToWorld(attributes.positionOS).xy;
                o.uv = attributes.uv;
                o.color = attributes.color * _Color * unity_SpriteColor;
                return o;
            }

            float4 UnlitFragment(Varyings i) : SV_Target
            {
                float edge, alpha;
                SphereCut(i.positionWS, edge, alpha);
                float outA = saturate(max(alpha, edge * _EdgeColor.a));
                clip(outA - 0.01);
                float4 mainTex = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half3 edgeCol = lerp(_EdgeColor.rgb, _EdgeColor2.rgb, edge);
                mainTex.rgb = lerp(mainTex.rgb, edgeCol, saturate(edge * _EdgeColor.a));
                mainTex.a *= outA;
                return mainTex;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
