Shader "LightGame/SpriteBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BlurPixels ("Blur Radius (source px)", Float) = 0
        _EdgeBlurBoost ("Extra LOD At Fully-Transparent Edge", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
            "RenderPipeline"="UniversalPipeline"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            float4 _Color;
            float  _BlurPixels;
            float  _EdgeBlurBoost;

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color      = v.color * _Color;
                o.uv         = v.uv;
                return o;
            }

            // Texture is stored premultiplied (rgb baked with alpha at import), so
            // we can filter/blur it directly without a bilinear-halo.
            half4 frag(Varyings i) : SV_Target
            {
                float radius = max(0.0, _BlurPixels);
                float base_lod = max(0.0, log2(max(1.0, radius)) - 1.0);

                // Extra blur near the edge: peek at alpha at the base LOD and
                // boost the sampling LOD in proportion to (1 - alpha). Interior
                // pixels (a = 1) keep the base blur; near-transparent edge pixels
                // pull from higher (blurrier) mips, feathering the shape softer.
                half peek_a = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, i.uv, base_lod).a;
                float lod   = base_lod + saturate(1.0 - peek_a) * _EdgeBlurBoost;
                float2 t    = _MainTex_TexelSize.xy * exp2(lod);

                half4 c  = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, i.uv,                    lod) * 0.40h;
                c       += SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, i.uv + float2( t.x, 0),  lod) * 0.15h;
                c       += SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, i.uv + float2(-t.x, 0),  lod) * 0.15h;
                c       += SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, i.uv + float2(0,  t.y),  lod) * 0.15h;
                c       += SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, i.uv + float2(0, -t.y),  lod) * 0.15h;

                // Tint: multiply premultiplied output by tint (rgb * tint.rgb, a * tint.a),
                // and by tint.a to keep the result premultiplied.
                half4 tint = i.color;
                c.rgb *= tint.rgb * tint.a;
                c.a   *= tint.a;
                return c;
            }
            ENDHLSL
        }
    }
}
