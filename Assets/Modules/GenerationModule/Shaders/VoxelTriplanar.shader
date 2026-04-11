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
float noise_0 = PerlinNoise(worldPos * 0.0010);
float clamp_1 = clamp(noise_0, 0.0000f, 1.0000f);

    float nSel_2 = saturate(clamp_1 * 0.5 + 0.5);

    {
        float mixNoise_4 = SimplexNoise(worldPos * 0.0500);
    float4 texWeights_3 = float4(0,0,0,0);
    float w_5 = saturate(smoothstep(50.00-5.00, 50.00+5.00, worldPos.y + mixNoise_4*0.50)) * saturate(1.0 - smoothstep(100.00-5.00, 100.00+5.00, worldPos.y + mixNoise_4*0.50));
    texWeights_3.r += w_5;
    float w_6 = saturate(smoothstep(-100.00-5.00, -100.00+5.00, worldPos.y + mixNoise_4*0.50)) * saturate(1.0 - smoothstep(50.00-5.00, 50.00+5.00, worldPos.y + mixNoise_4*0.50));
    texWeights_3.a += w_6;
    texWeights_3 /= max(0.0001, texWeights_3.r + texWeights_3.g + texWeights_3.b + texWeights_3.a);

        float bw_7 = pow(saturate(1.0 - abs(nSel_2 - 0.200) * 4.0), 2.0);
        float3 c = (GetTriplanar(_Tex0, worldPos, normal, _Scale0) * texWeights_3.r +
                    GetTriplanar(_Tex1, worldPos, normal, _Scale1) * texWeights_3.g +
                    GetTriplanar(_Tex2, worldPos, normal, _Scale2) * texWeights_3.b +
                    GetTriplanar(_Tex3, worldPos, normal, _Scale3) * texWeights_3.a);
        finalColor += c * bw_7;
        totalW += bw_7;
    }
    {
        float mixNoise_9 = SimplexNoise(worldPos * 0.0500);
    float4 texWeights_8 = float4(0,0,0,0);
    float w_10 = saturate(smoothstep(50.00-5.00, 50.00+5.00, worldPos.y + mixNoise_9*0.50)) * saturate(1.0 - smoothstep(100.00-5.00, 100.00+5.00, worldPos.y + mixNoise_9*0.50));
    texWeights_8.g += w_10;
    float w_11 = saturate(smoothstep(-100.00-5.00, -100.00+5.00, worldPos.y + mixNoise_9*0.50)) * saturate(1.0 - smoothstep(50.00-5.00, 50.00+5.00, worldPos.y + mixNoise_9*0.50));
    texWeights_8.a += w_11;
    texWeights_8 /= max(0.0001, texWeights_8.r + texWeights_8.g + texWeights_8.b + texWeights_8.a);

        float bw_12 = pow(saturate(1.0 - abs(nSel_2 - 0.400) * 4.0), 2.0);
        float3 c = (GetTriplanar(_Tex0, worldPos, normal, _Scale0) * texWeights_8.r +
                    GetTriplanar(_Tex1, worldPos, normal, _Scale1) * texWeights_8.g +
                    GetTriplanar(_Tex2, worldPos, normal, _Scale2) * texWeights_8.b +
                    GetTriplanar(_Tex3, worldPos, normal, _Scale3) * texWeights_8.a);
        finalColor += c * bw_12;
        totalW += bw_12;
    }
    {
        float mixNoise_14 = SimplexNoise(worldPos * 0.0500);
    float4 texWeights_13 = float4(0,0,0,0);
    float w_15 = saturate(smoothstep(50.00-5.00, 50.00+5.00, worldPos.y + mixNoise_14*0.50)) * saturate(1.0 - smoothstep(100.00-5.00, 100.00+5.00, worldPos.y + mixNoise_14*0.50));
    texWeights_13.b += w_15;
    float w_16 = saturate(smoothstep(-100.00-5.00, -100.00+5.00, worldPos.y + mixNoise_14*0.50)) * saturate(1.0 - smoothstep(50.00-5.00, 50.00+5.00, worldPos.y + mixNoise_14*0.50));
    texWeights_13.b += w_16;
    texWeights_13 /= max(0.0001, texWeights_13.r + texWeights_13.g + texWeights_13.b + texWeights_13.a);

        float bw_17 = pow(saturate(1.0 - abs(nSel_2 - 0.600) * 4.0), 2.0);
        float3 c = (GetTriplanar(_Tex0, worldPos, normal, _Scale0) * texWeights_13.r +
                    GetTriplanar(_Tex1, worldPos, normal, _Scale1) * texWeights_13.g +
                    GetTriplanar(_Tex2, worldPos, normal, _Scale2) * texWeights_13.b +
                    GetTriplanar(_Tex3, worldPos, normal, _Scale3) * texWeights_13.a);
        finalColor += c * bw_17;
        totalW += bw_17;
    }
    {
        float mixNoise_19 = SimplexNoise(worldPos * 0.0500);
    float4 texWeights_18 = float4(0,0,0,0);
    float w_20 = saturate(smoothstep(50.00-5.00, 50.00+5.00, worldPos.y + mixNoise_19*0.50)) * saturate(1.0 - smoothstep(100.00-5.00, 100.00+5.00, worldPos.y + mixNoise_19*0.50));
    texWeights_18.a += w_20;
    float w_21 = saturate(smoothstep(-100.00-5.00, -100.00+5.00, worldPos.y + mixNoise_19*0.50)) * saturate(1.0 - smoothstep(50.00-5.00, 50.00+5.00, worldPos.y + mixNoise_19*0.50));
    texWeights_18.g += w_21;
    texWeights_18 /= max(0.0001, texWeights_18.r + texWeights_18.g + texWeights_18.b + texWeights_18.a);

        float bw_22 = pow(saturate(1.0 - abs(nSel_2 - 0.800) * 4.0), 2.0);
        float3 c = (GetTriplanar(_Tex0, worldPos, normal, _Scale0) * texWeights_18.r +
                    GetTriplanar(_Tex1, worldPos, normal, _Scale1) * texWeights_18.g +
                    GetTriplanar(_Tex2, worldPos, normal, _Scale2) * texWeights_18.b +
                    GetTriplanar(_Tex3, worldPos, normal, _Scale3) * texWeights_18.a);
        finalColor += c * bw_22;
        totalW += bw_22;
    }
    {
        float mixNoise_24 = SimplexNoise(worldPos * 0.0500);
    float4 texWeights_23 = float4(0,0,0,0);
    float w_25 = saturate(smoothstep(50.00-5.00, 50.00+5.00, worldPos.y + mixNoise_24*0.50)) * saturate(1.0 - smoothstep(100.00-5.00, 100.00+5.00, worldPos.y + mixNoise_24*0.50));
    texWeights_23.a += w_25;
    float w_26 = saturate(smoothstep(-100.00-5.00, -100.00+5.00, worldPos.y + mixNoise_24*0.50)) * saturate(1.0 - smoothstep(50.00-5.00, 50.00+5.00, worldPos.y + mixNoise_24*0.50));
    texWeights_23.r += w_26;
    texWeights_23 /= max(0.0001, texWeights_23.r + texWeights_23.g + texWeights_23.b + texWeights_23.a);

        float bw_27 = pow(saturate(1.0 - abs(nSel_2 - 1.000) * 4.0), 2.0);
        float3 c = (GetTriplanar(_Tex0, worldPos, normal, _Scale0) * texWeights_23.r +
                    GetTriplanar(_Tex1, worldPos, normal, _Scale1) * texWeights_23.g +
                    GetTriplanar(_Tex2, worldPos, normal, _Scale2) * texWeights_23.b +
                    GetTriplanar(_Tex3, worldPos, normal, _Scale3) * texWeights_23.a);
        finalColor += c * bw_27;
        totalW += bw_27;
    }
    finalColor /= max(0.0001, totalW);
    // [GENERATED_BIOME_LOGIC_END]

                float3 lightDir = _MainLightPosition.xyz;
                float light = saturate(dot(normal, lightDir)) * 0.8 + 0.2;
                return half4(finalColor * light, 1.0);
            }
            ENDHLSL
        }
    }
}