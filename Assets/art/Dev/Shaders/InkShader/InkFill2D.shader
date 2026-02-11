Shader "Custom/InkFill2D"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _InkColor ("Ink Color", Color) = (0.05, 0.05, 0.1, 1)
        _HighlightColor ("Highlight Color", Color) = (0.2, 0.2, 0.3, 1)
        _NoiseScale ("Noise Scale", Float) = 10.0
        _Speed ("Animation Speed", Float) = 1.0
        _Contrast ("Pattern Contrast", Float) = 5.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 uvNoise : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            float4 _MainTex_ST;     // שים לב: זה נדרש עבור המקרו TRANSFORM_TEX
            float4 _NoiseTex_ST;

            float4 _InkColor;
            float4 _HighlightColor;
            float _NoiseScale;
            float _Speed;
            float _Contrast;

            // --- התיקון: מחקנו את השורה: float time = _Time.y; ---

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                // --- התיקון בחישוב התנועה ---
                // 1. משתמשים ב-_Time.y ישירות
                // 2. יוצרים float2 כדי שיתאים לחיבור עם ה-UV
                float timeOffset = _Time.y * _Speed;
                o.uvNoise = v.uv * _NoiseScale + float2(timeOffset, timeOffset); 
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 baseCol = tex2D(_MainTex, i.uv);
                
                // כאן הכל תקין
                float noise = tex2D(_NoiseTex, i.uvNoise).r;

                // Adjust contrast and remap noise
                noise = saturate((noise - 0.5) * _Contrast + 0.5);

                // Lerp between ink and highlight color based on noise
                float3 ink = lerp(_InkColor.rgb, _HighlightColor.rgb, noise);

                return fixed4(ink, baseCol.a); // Keep original alpha
            }
            ENDCG
        }
    }
}