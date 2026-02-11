Shader "Custom/InkFluid"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        
        [Header(Colors)]
        _InkColor ("Ink Color", Color) = (0.05, 0.05, 0.1, 1)
        _HighlightColor ("Highlight Color", Color) = (0.2, 0.2, 0.3, 1)
        
        [Header(Settings)]
        _NoiseScale ("Noise Scale", Float) = 2.0
        _Speed ("Flow Speed", Float) = 0.5
        _Distortion ("Turbulence Strength", Range(0, 1)) = 0.2
        _Contrast ("Contrast", Float) = 3.0
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
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            float4 _MainTex_ST;
            
            float4 _InkColor;
            float4 _HighlightColor;
            float _NoiseScale;
            float _Speed;
            float _Distortion;
            float _Contrast;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv; // אנחנו נעשה את כל חישובי התנועה ב-Fragment ליותר דיוק
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // דגימת הצבע המקורי (הצורה של הספרייט)
                fixed4 baseCol = tex2D(_MainTex, i.uv);
                
                // אם האלפא של הספרייט המקורי הוא 0, אין טעם לחשב (חוסך ביצועים)
                if (baseCol.a < 0.01) discard;

                float time = _Time.y * _Speed;

                // --- שלב 1: דגימת רעש ראשונה (זרימה בסיסית) ---
                // אנחנו מזיזים את ה-UV לאט לכיוון אחד
                float2 flowOffset = float2(time * 0.1, time * 0.2);
                float2 noiseUV1 = (i.uv * _NoiseScale) + flowOffset;
                float noise1 = tex2D(_NoiseTex, noiseUV1).r;

                // --- שלב 2: דגימת רעש שנייה (העיוות) ---
                // אנחנו משתמשים בתוצאה של הרעש הראשון (noise1) כדי להזיז את ה-UV של השני
                // זה מה שיוצר את אפקט ה"מערבולת"
                float2 distortOffset = (noise1 - 0.5) * _Distortion; 
                float2 noiseUV2 = (i.uv * _NoiseScale) - float2(time * 0.15, -time * 0.05) + distortOffset;
                
                float finalNoise = tex2D(_NoiseTex, noiseUV2).r;

                // --- חידוד הקונטרסט ---
                // שילוב של שתי שכבות הרעש לתוצאה עשירה יותר
                float combinedNoise = (noise1 + finalNoise) * 0.5;
                combinedNoise = saturate((combinedNoise - 0.5) * _Contrast + 0.5);

                // --- צביעה ---
                float3 finalColor = lerp(_InkColor.rgb, _HighlightColor.rgb, combinedNoise);

                return fixed4(finalColor, baseCol.a);
            }
            ENDCG
        }
    }
}