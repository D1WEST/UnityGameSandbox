Shader "Custom/VoxelTriplanar"
{
    Properties
    {
        [Header(Textures)]
        _Tex0("Slot 0 (R)", 2D) = "white" {}
        _Tex1("Slot 1 (G)", 2D) = "white" {}
        _Tex2("Slot 2 (B)", 2D) = "white" {}
        _Tex3("Slot 3 (A)", 2D) = "white" {}
        _Scale("Texture Scale", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 color        : COLOR; // Это наши веса из графа!
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float4 weights      : COLOR;
            };

            sampler2D _Tex0, _Tex1, _Tex2, _Tex3;
            float _Scale;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.weights = input.color;
                return output;
            }

            float3 GetTriplanar(sampler2D tex, float3 p, float3 n)
            {
                float3 blending = abs(n);
                blending /= (blending.x + blending.y + blending.z);
                
                float3 x = tex2D(tex, p.zy * _Scale).rgb;
                float3 y = tex2D(tex, p.xz * _Scale).rgb;
                float3 z = tex2D(tex, p.xy * _Scale).rgb;
                
                return x * blending.x + y * blending.y + z * blending.z;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 pos = input.positionWS;

                // Получаем цвета из всех 4-х слотов
                float3 col0 = GetTriplanar(_Tex0, pos, normal);
                float3 col1 = GetTriplanar(_Tex1, pos, normal);
                float3 col2 = GetTriplanar(_Tex2, pos, normal);
                float3 col3 = GetTriplanar(_Tex3, pos, normal);

                // Смешиваем их на основе весов из вершин (Vertex Color)
                float3 finalCol = col0 * input.weights.r + 
                                  col1 * input.weights.g + 
                                  col2 * input.weights.b + 
                                  col3 * input.weights.a;

                // Базовое освещение
                float3 lightDir = _MainLightPosition.xyz;
                float light = saturate(dot(normal, lightDir)) * 0.8 + 0.2;

                return half4(finalCol * light, 1.0);
            }
            ENDHLSL
        }
    }
}