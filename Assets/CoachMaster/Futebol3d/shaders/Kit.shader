Shader "CoachMaster/Futebol3d/Kit"
{
    Properties
    {
        _BaseMap("Skin", 2D) = "white" {}
        _Shirt("Shirt", Color) = (0.1, 0.3, 0.8, 1)
        _Shorts("Shorts", Color) = (1, 1, 1, 1)
        _Secondary("Secondary", Color) = (1, 1, 1, 1)
        _Pattern("Pattern", Float) = 0
        _Hair("Hair", Color) = (0.15, 0.08, 0.04, 1)
        _SkinTint("Skin Tint", Color) = (1, 1, 1, 1)
        _Gloves("Gloves", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _Shirt;
                float4 _Shorts;
                float4 _Secondary;
                float _Pattern;
                float4 _Hair;
                float4 _SkinTint;
                float _Gloves;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 extra : TEXCOORD1;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 extra : TEXCOORD1;
                float4 color : COLOR;
                float3 normalWS : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.extra = input.extra;
                output.color = input.color;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            float PatternMask(float height, float across)
            {
                float id = _Pattern;
                if (id < 0.5)
                    return 0;
                if (id < 1.5)
                    return step(0.5, frac(across * 7));
                if (id < 2.5)
                    return step(0.5, frac(height * 8));
                if (id < 3.5)
                    return step(0.5, frac((across + height) * 4));
                if (id < 4.5)
                    return step(0.5, across);
                if (id < 5.5)
                    return step(0.62, height) * step(height, 0.7);
                return step(0.78, frac(across * 16));
            }

            half4 frag(Varyings input) : SV_Target
            {
                float4 skin = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                float stripe = PatternMask(input.color.b, input.color.a);
                float3 shirt = lerp(_Shirt.rgb, _Secondary.rgb, stripe);
                float3 cloth = lerp(shirt, _Shorts.rgb, saturate(input.color.g));
                float amount = saturate(input.color.r + input.color.g);
                float3 rgb = lerp(skin.rgb * _SkinTint.rgb, cloth, amount);
                if (_Gloves > 0.5 && input.extra.x > 0.5)
                    rgb = float3(0.96, 0.96, 0.97);
                float light = saturate(dot(normalize(input.normalWS), normalize(float3(0.25, 0.85, 0.35))));
                rgb *= lerp(0.62, 1, light);
                return float4(rgb, 1);
            }
            ENDHLSL
        }
    }
}
