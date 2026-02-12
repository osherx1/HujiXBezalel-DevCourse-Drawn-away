Shader "Custom/SimpleMagicGlow2D"
{
    Properties
    {
        [MainTexture] _MainTex ("Particle Texture", 2D) = "white" {}
        [HDR] _GlowColor ("Glow Color", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity", Range(1, 10)) = 2.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR; // צבע מה-Particle System
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _GlowColor;
                float _Intensity;
            CBUFFER_END

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                half4 finalColor = tex * input.color * _GlowColor * _Intensity;
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}