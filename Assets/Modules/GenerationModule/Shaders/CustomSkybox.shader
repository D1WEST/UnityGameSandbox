Shader "Custom/SimpleSkybox"
{
    Properties {
        _Tex ("Cubemap", Cube) = "white" {}
        _Tint ("Tint Color", Color) = (1,1,1,1)
        _Exposure ("Exposure", Range(0, 8)) = 1.0
    }
    SubShader {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            samplerCUBE _Tex;
            half4 _Tint;
            half _Exposure;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 viewDir : TEXCOORD0; };

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.viewDir = v.vertex.xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                half4 tex = texCUBE(_Tex, i.viewDir);
                return tex * _Tint * _Exposure;
            }
            ENDCG
        }
    }
}