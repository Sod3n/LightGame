Shader "Hidden/LightGame/VoidDepth"
{
    // Prepass shader for the void depth feature. Used as an OVERRIDE material by
    // VoidDepthPrepassFeature: every gameplay renderer in the feature's LayerMask is redrawn
    // with this pass, outputting its WORLD-space Z (remapped 0..1) so the void can keep depth
    // readable in the dark. World Z is global - a child sprite's world Z already folds in every
    // parent transform, so depth set on a root propagates to all children.
    //
    // Transparency note: all gameplay sprites use TIGHT meshes, so the rasterized geometry
    // already follows each PNG's alpha outline - depth is only written on the sprite's actual
    // shape, never the transparent rectangle. We deliberately do NOT alpha-clip here: an
    // override material never receives the per-sprite texture (_MainTex reads the engine
    // default, not the sprite), so a clip() would discard everything. If pixel-perfect edges
    // are ever needed, add a "VoidDepth" pass to the shared sprite shader and drop the override.
    Properties
    {
        _DepthNear ("World Z Near (maps to black)", Float) = -5
        _DepthFar  ("World Z Far (maps to white)", Float) = 5
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "VoidDepth"
            Tags { "LightMode" = "Universal2D" }
            ZWrite Off
            ZTest Always
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _DepthNear;
                float _DepthFar;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float  worldZ     : TEXCOORD0;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                float3 worldPos = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(worldPos);
                o.worldZ = worldPos.z;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float denom = max(_DepthFar - _DepthNear, 1e-4);
                float g = saturate((i.worldZ - _DepthNear) / denom);
                return half4(g, g, g, 1);
            }
            ENDHLSL
        }
    }
}
