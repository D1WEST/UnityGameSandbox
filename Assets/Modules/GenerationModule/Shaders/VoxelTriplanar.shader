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
            #include "NoiseCommon.hlsl"
            #include "SimplexNoise3D.hlsl"
            #include "PerlinNoise3D.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; };

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
                return output;
            }

            float3 GetTriplanar(sampler2D tex, float3 p, float3 n, float scale) {
                float3 blending = abs(n);
                blending /= (blending.x + blending.y + blending.z);
                return tex2D(tex, p.zy * scale).rgb * blending.x + 
                       tex2D(tex, p.xz * scale).rgb * blending.y + 
                       tex2D(tex, p.xy * scale).rgb * blending.z;
            }

            half4 frag(Varyings input) : SV_Target {
                float3 worldPos = input.positionWS;
                float3 normal = normalize(input.normalWS);
                
                float totalW = 0.0001f;
                float3 finalColor = float3(0,0,0);

                // [GENERATED_BIOME_LOGIC_START]
float noise_0 = PerlinNoise(worldPos * 0.0100);
float clamp_1 = clamp(noise_0, 0.0010f, 1.0000f);

    float nSel_2 = saturate(clamp_1 * 0.5 + 0.5);

    {
        float mixNoise_4 = SimplexNoise(worldPos * 0.0500);
    float4 texWeights_3 = float4(0,0,0,0);
    float w_5 = saturate(smoothstep(-100.00-5.00, -100.00+5.00, worldPos.y + mixNoise_4*0.50)) * saturate(1.0 - smoothstep(100.00-5.00, 100.00+5.00, worldPos.y + mixNoise_4*0.50));
    texWeights_3.r += w_5;
    texWeights_3 /= max(0.0001, texWeights_3.r + texWeights_3.g + texWeights_3.b + texWeights_3.a);

        float bw_6 = pow(saturate(1.0 - abs(nSel_2 - 0.200) * 4.0), 2.0);
        float3 c = (GetTriplanar(_Tex0, worldPos, normal, _Scale0) * texWeights_3.r +
                    GetTriplanar(_Tex1, worldPos, normal, _Scale1) * texWeights_3.g +
                    GetTriplanar(_Tex2, worldPos, normal, _Scale2) * texWeights_3.b +
                    GetTriplanar(_Tex3, worldPos, normal, _Scale3) * texWeights_3.a);
        finalColor += c * bw_6;
        totalW += bw_6;
    }
    {
        float mixNoise_8 = SimplexNoise(worldPos * 0.0500);
    float4 texWeights_7 = float4(0,0,0,0);
    float w_9 = saturate(smoothstep(-100.00-5.00, -100.00+5.00, worldPos.y + mixNoise_8*0.50)) * saturate(1.0 - smoothstep(100.00-5.00, 100.00+5.00, worldPos.y + mixNoise_8*0.50));
    texWeights_7.g += w_9;
    texWeights_7 /= max(0.0001, texWeights_7.r + texWeights_7.g + texWeights_7.b + texWeights_7.a);

        float bw_10 = pow(saturate(1.0 - abs(nSel_2 - 0.400) * 4.0), 2.0);
        float3 c = (GetTriplanar(_Tex0, worldPos, normal, _Scale0) * texWeights_7.r +
                    GetTriplanar(_Tex1, worldPos, normal, _Scale1) * texWeights_7.g +
                    GetTriplanar(_Tex2, worldPos, normal, _Scale2) * texWeights_7.b +
                    GetTriplanar(_Tex3, worldPos, normal, _Scale3) * texWeights_7.a);
        finalColor += c * bw_10;
        totalW += bw_10;
    }
    {
        float mixNoise_12 = SimplexNoise(worldPos * 0.0500);
    float4 texWeights_11 = float4(0,0,0,0);
    float w_13 = saturate(smoothstep(-100.00-5.00, -100.00+5.00, worldPos.y + mixNoise_12*0.50)) * saturate(1.0 - smoothstep(100.00-5.00, 100.00+5.00, worldPos.y + mixNoise_12*0.50));
    texWeights_11.b += w_13;
    texWeights_11 /= max(0.0001, texWeights_11.r + texWeights_11.g + texWeights_11.b + texWeights_11.a);

        float bw_14 = pow(saturate(1.0 - abs(nSel_2 - 0.600) * 4.0), 2.0);
        float3 c = (GetTriplanar(_Tex0, worldPos, normal, _Scale0) * texWeights_11.r +
                    GetTriplanar(_Tex1, worldPos, normal, _Scale1) * texWeights_11.g +
                    GetTriplanar(_Tex2, worldPos, normal, _Scale2) * texWeights_11.b +
                    GetTriplanar(_Tex3, worldPos, normal, _Scale3) * texWeights_11.a);
        finalColor += c * bw_14;
        totalW += bw_14;
    }
    {
        float mixNoise_16 = SimplexNoise(worldPos * 0.0500);
    float4 texWeights_15 = float4(0,0,0,0);
    float w_17 = saturate(smoothstep(-100.00-5.00, -100.00+5.00, worldPos.y + mixNoise_16*0.50)) * saturate(1.0 - smoothstep(100.00-5.00, 100.00+5.00, worldPos.y + mixNoise_16*0.50));
    texWeights_15.a += w_17;
    texWeights_15 /= max(0.0001, texWeights_15.r + texWeights_15.g + texWeights_15.b + texWeights_15.a);

        float bw_18 = pow(saturate(1.0 - abs(nSel_2 - 0.800) * 4.0), 2.0);
        float3 c = (GetTriplanar(_Tex0, worldPos, normal, _Scale0) * texWeights_15.r +
                    GetTriplanar(_Tex1, worldPos, normal, _Scale1) * texWeights_15.g +
                    GetTriplanar(_Tex2, worldPos, normal, _Scale2) * texWeights_15.b +
                    GetTriplanar(_Tex3, worldPos, normal, _Scale3) * texWeights_15.a);
        finalColor += c * bw_18;
        totalW += bw_18;
    }
    {
        float mixNoise_20 = SimplexNoise(worldPos * 0.0500);
    float4 texWeights_19 = float4(0,0,0,0);
    float w_21 = saturate(smoothstep(-100.00-5.00, -100.00+5.00, worldPos.y + mixNoise_20*0.50)) * saturate(1.0 - smoothstep(100.00-5.00, 100.00+5.00, worldPos.y + mixNoise_20*0.50));
    texWeights_19.a += w_21;
    texWeights_19 /= max(0.0001, texWeights_19.r + texWeights_19.g + texWeights_19.b + texWeights_19.a);

        float bw_22 = pow(saturate(1.0 - abs(nSel_2 - 1.000) * 4.0), 2.0);
        float3 c = (GetTriplanar(_Tex0, worldPos, normal, _Scale0) * texWeights_19.r +
                    GetTriplanar(_Tex1, worldPos, normal, _Scale1) * texWeights_19.g +
                    GetTriplanar(_Tex2, worldPos, normal, _Scale2) * texWeights_19.b +
                    GetTriplanar(_Tex3, worldPos, normal, _Scale3) * texWeights_19.a);
        finalColor += c * bw_22;
        totalW += bw_22;
    }
    finalColor /= totalW;
    // [GENERATED_BIOME_LOGIC_END]

                float3 lightDir = _MainLightPosition.xyz;
                float light = saturate(dot(normal, lightDir)) * 0.8 + 0.2;
                return half4(finalColor * light, 1.0);
            }
            ENDHLSL
        }
    }
}