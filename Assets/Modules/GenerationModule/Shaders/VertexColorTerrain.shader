Shader "Custom/URPVertexColorLit"
{
    Properties
    {
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 normalWS     : TEXCOORD0;
                float4 color        : TEXCOORD1;
            };

            float _Smoothness;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Базовый расчет освещения, чтобы видеть углы
                float3 lightDir = _MainLightPosition.xyz;
                float intensity = saturate(dot(input.normalWS, lightDir)) * 0.8 + 0.2;
                
                return float4(input.color.rgb * intensity, 1.0);
            }
            ENDHLSL
        }
    }
}