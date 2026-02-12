Shader "Custom/SpriteOutline_URP"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness("Outline Thickness", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Name "Outline"
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _OutlineColor;
            float _OutlineThickness;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 texelSize = _OutlineThickness / _ScreenParams.xy;
                float alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).a;

                if (alpha == 0)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        for (int y = -1; y <= 1; y++)
                        {
                            float2 offset = float2(x, y) * texelSize;
                            if (SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + offset).a > 0.1)
                                return _OutlineColor;
                        }
                    }
                    return 0;
                }

                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
            }
            ENDHLSL
        }
    }
}
