Shader "Custom/VoxelTriplanar"
{
    Properties
    {
        // [GENERATED_PROPERTIES_START]
        _Tex0("0", 2D) = "white" {}
        _Scale0("Scale 0", Float) = 0.100
        _Tex1("1", 2D) = "white" {}
        _Scale1("Scale 1", Float) = 0.100
        _Tex2("2", 2D) = "white" {}
        _Scale2("Scale 2", Float) = 0.100
        _Tex3("3", 2D) = "white" {}
        _Scale3("Scale 3", Float) = 0.100
        _Tex4("4", 2D) = "white" {}
        _Scale4("Scale 4", Float) = 0.100

    // [GENERATED_PROPERTIES_END]
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

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; float4 weights : COLOR; };

            // [GENERATED_SAMPLERS_START]
            sampler2D _Tex0; float _Scale0;
            sampler2D _Tex1; float _Scale1;
            sampler2D _Tex2; float _Scale2;
            sampler2D _Tex3; float _Scale3;
            sampler2D _Tex4; float _Scale4;

    // [GENERATED_SAMPLERS_END]

            Varyings vert(Attributes input) {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.weights = input.color;
                return output;
            }

            float3 GetTriplanar(sampler2D tex, float3 p, float3 n, float scale) {
                float3 blending = abs(n);
                blending /= (blending.x + blending.y + blending.z);
                float3 x = tex2D(tex, p.zy * scale).rgb;
                float3 y = tex2D(tex, p.xz * scale).rgb;
                float3 z = tex2D(tex, p.xy * scale).rgb;
                return x * blending.x + y * blending.y + z * blending.z;
            }

            half4 frag(Varyings input) : SV_Target {
                float3 normal = normalize(input.normalWS);
                float3 pos = input.positionWS;

                // ВАЖНО: На этом этапе мы берем первые 4 текстуры из конфига как "основные"
                // Если нужно больше - потребуется использование UV для передачи индексов (это следующий шаг)
                float3 c0 = GetTriplanar(_Tex0, pos, normal, _Scale0);
                float3 c1 = GetTriplanar(_Tex1, pos, normal, _Scale1);
                float3 c2 = GetTriplanar(_Tex2, pos, normal, _Scale2);
                float3 c3 = GetTriplanar(_Tex3, pos, normal, _Scale3);

                float3 finalCol = c0 * input.weights.r + c1 * input.weights.g + c2 * input.weights.b + c3 * input.weights.a;

                float3 lightDir = _MainLightPosition.xyz;
                float light = saturate(dot(normal, lightDir)) * 0.8 + 0.2;

                return half4(finalCol * light, 1.0);
            }
            ENDHLSL
        }
    }
}