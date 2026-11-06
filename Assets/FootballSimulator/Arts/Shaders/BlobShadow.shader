Shader "Football/BlobShadow"
{
    Properties
    {
        _BaseMap ("Shadow", 2D) = "white" {}
        _Power ("Power", Float) = 1
        _PlayerShadowPower ("Player Shadow Power", Float) = 1
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _Power;
                float _PlayerShadowPower;
            CBUFFER_END

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

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half2 centered = input.uv * 2.0 - 1.0;
                half radial = saturate(1.0 - length(centered));
                radial = radial * radial;
                half coverage = tex.a > 0.02 ? tex.a : tex.r;
                half mask = coverage * radial;
                half strength = saturate(_Power) * saturate(_PlayerShadowPower);
                return half4(0.0, 0.0, 0.0, mask * strength * 0.85);
            }
            ENDHLSL
        }
    }
}
