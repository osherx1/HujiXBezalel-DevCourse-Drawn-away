Shader "Custom/Cloud_DomainWarp_LineRenderer"
{
    Properties
    {
        [Header(Base Settings)]
        _MainTex ("Cloud Sprite (Alpha)", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (1,1,1,1)
        
        [Header(Noise Settings)]
        _NoiseScale ("Noise Scale", Float) = 3.0
        _NoiseSpeed ("Animation Speed", Float) = 0.5
        _Distortion ("Distortion Strength", Range(0, 2)) = 0.5
        
        [Header(Cloud Density)]
        _CloudDensity ("Density / Cutoff", Range(0, 1)) = 0.5
        _Softness ("Edge Softness", Range(0.01, 1)) = 0.2
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
        }
        
        // הגדרות בלנדינג שמתאימות לעשן/עננים (שקוף)
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
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR; // קריטי ל-LineRenderer
                float2 texcoord : TEXCOORD0;
                float2 noiseUV : TEXCOORD1; // UV נפרד לרעש
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _TintColor;
            
            float _NoiseScale;
            float _NoiseSpeed;
            float _Distortion;
            float _CloudDensity;
            float _Softness;

            // --- Noise Functions ---
            float random(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }

            float noise(float2 st)
            {
                float2 i = floor(st);
                float2 f = frac(st);
                float a = random(i);
                float b = random(i + float2(1.0, 0.0));
                float c = random(i + float2(0.0, 1.0));
                float d = random(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a)* u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            float fbm(float2 st)
            {
                float v = 0.0;
                float a = 0.5;
                float2x2 rot = float2x2(cos(0.5), sin(0.5), -sin(0.5), cos(0.5));
                // הורדתי ל-4 איטרציות לטובת ביצועים על LineRenderer שיש לו הרבה חלקיקים/סגמנטים
                for (int i = 0; i < 4; ++i) {
                    v += a * noise(st);
                    st = mul(rot, st) * 2.0 + 100.0;
                    a *= 0.5;
                }
                return v;
            }

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // חישוב UV רגיל לספרייט
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                
                // חישוב UV לרעש - מוסיף לו את הזמן כבר כאן כדי לחסוך חישובים לכל פיקסל
                // אנחנו משתמשים ב-WorldPos או ב-UV תלוי באפקט הרצוי. כאן נשתמש ב-UV.
                o.noiseUV = v.texcoord * _NoiseScale; 
                
                o.color = v.color; // העברת הצבע מה-LineRenderer
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Time.y * _NoiseSpeed;
                float2 st = i.noiseUV;

                // --- Domain Warping Logic ---
                // שלב 1: יצירת תנועת בסיס
                float2 q = float2(0,0);
                q.x = fbm(st + 0.1 * t);
                q.y = fbm(st + float2(5.2, 1.3) + 0.12 * t);

                // שלב 2: שימוש בתנועה לעיוות ה-UV המקורי של הטקסטורה
                // זה גורם לספרייט הענן להיראות כאילו הוא מתערבל
                float2 distortedUV = i.texcoord + (q - 0.5) * _Distortion;
                
                // דגימת הטקסטורה עם ה-UV המעוות
                fixed4 col = tex2D(_MainTex, distortedUV);
                
                // --- Cloud Shape & Noise Interaction ---
                
                // יצירת רעש נוסף לטובת חיתוך (Alpha Erosion)
                float f = fbm(st + q + t * 0.2);
                
                // שילוב: האלפא של הטקסטורה * האלפא של הוורטקס (מה-LineRenderer) * הרעש
                float noiseMask = smoothstep(_CloudDensity - _Softness, _CloudDensity + _Softness, f);
                
                // חישוב הצבע הסופי
                // אנחנו לוקחים את הצבע מה-Texture, מוסיפים Tint, ומכפילים ב-Vertex Color
                fixed4 finalColor = col * _TintColor * i.color;
                
                // חיתוך האלפא לפי הרעש - זה יוצר את המראה ה"ענני" שמשתנה
                finalColor.a *= noiseMask;

                return finalColor;
            }
            ENDCG
        }
    }
}

/*Shader "ShaderSketches/DomainWarping_Editable"
{
    Properties
    {
        [Header(Main Configuration)]
        _MainTex ("Mask Texture", 2D) = "white" {}
        [Toggle] _UseMask ("Enable Mask", Float) = 1
        
        _Scale ("Noise Scale", Float) = 3.0
        _Speed ("Animation Speed", Float) = 0.5
        
        [Header(Warp Settings)]
        _WarpStrength1 ("Warp 1 Strength", Range(0, 5)) = 1.0
        _WarpStrength2 ("Warp 2 Strength", Range(0, 5)) = 1.0

        [Header(Color Palette)]
        [HDR] _ColorA ("Primary Color (Teal)", Color) = (0.1, 0.62, 0.67, 1)
        [HDR] _ColorB ("Secondary Color (Sand)", Color) = (0.67, 0.67, 0.5, 1)
        [HDR] _ColorC ("Depth Color (Dark Blue)", Color) = (0, 0, 0.16, 1)
        [HDR] _ColorD ("Highlight Color (Green)", Color) = (0.07, 1, 0.07, 1)
    }
    
    CGINCLUDE
    #include "UnityCG.cginc"
    // #include "Common.cginc" // הוסר לצורך עצמאות השיידר

    // --- Exposed Variables ---
    sampler2D _MainTex;
    float4 _MainTex_ST;
    float _UseMask;

    float _Scale;
    float _Speed;
    float _WarpStrength1;
    float _WarpStrength2;

    float4 _ColorA;
    float4 _ColorB;
    float4 _ColorC;
    float4 _ColorD;

    // --- Helper Functions ---

    // פונקציה לתיקון יחס מסך (במקום screen_aspect)
    float2 fix_aspect(float2 uv)
    {
        float2 res = _ScreenParams.xy;
        if (res.y == 0) return uv;
        float aspect = res.x / res.y;
        uv.x *= aspect;
        return uv;
    }

    float random(float2 st)
    {
        return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
    }

    float noise(float2 st)
    {
        float2 i = floor(st);
        float2 f = frac(st);

        // Four corners in 2D of a tile
        float a = random(i);
        float b = random(i + float2(1.0, 0.0));
        float c = random(i + float2(0.0, 1.0));
        float d = random(i + float2(1.0, 1.0));

        float2 u = f * f * (3.0 - 2.0 * f);

        return lerp(a, b, u.x) +
                   (c - a)* u.y * (1.0 - u.x) +
                   (d - b) * u.x * u.y;
    }

    float fbm(float2 st)
    {
        float v = 0.0;
        float a = 0.5;
        float2 shift = 100.0;
        // Rotate to reduce axial bias
        float2x2 rotate = float2x2(cos(0.5), sin(0.5), -sin(0.5), cos(0.50));

        // Loop of octaves
        for (int i = 0; i < 7; ++i)
        {
            v += a * noise(st);
            st = mul(rotate, st) * 2.0 + shift;
            a *= 0.5;
        }
        return v;
    }
    
    // Domain Warping Logic
    float4 frag(v2f_img i) : SV_Target
    {
        // 1. Mask UV Calculation
        float2 maskUV = i.uv * _MainTex_ST.xy + _MainTex_ST.zw;

        // 2. Aspect Ratio Fix & Scaling
        float2 st = fix_aspect(i.uv);
        st *= _Scale; // זום אין/אאוט

        float t = _Time.y * _Speed;

        // --- The Warping Magic ---
        
        // Step 1: First layer of noise
        float2 q = 0;
        q.x = fbm(st + 0.00 * t);
        q.y = fbm(st + 1);

        // Step 2: Second layer, distorted by 'q'
        float2 r = 0;
        // הכפלה ב-_WarpStrength מאפשרת לשלוט בכמות העיוות
        r.x = fbm(st + _WarpStrength1 * q + float2(1.7, 9.2) + 0.15 * t);
        r.y = fbm(st + _WarpStrength1 * q + float2(8.3, 2.8) + 0.126 * t);

        // Step 3: Final noise, distorted by 'r'
        float f = fbm(st + _WarpStrength2 * r);
        
        // --- Coloring ---
        float3 color = 0.0;
        
        // Base mix between Color A and B based on the final noise 'f'
        color = lerp(_ColorA.rgb,
                     _ColorB.rgb,
                     saturate(f * f * 4.0));

        // Darken areas based on the length of 'q' (intermediate noise)
        color = lerp(color,
                     _ColorC.rgb,
                     saturate(length(q)));

        // Highlight areas based on 'r.x'
        color = lerp(color,
                     _ColorD.rgb,
                     saturate(length(r.x)));

        // Final contrast adjustment
        float3 finalRGB = (f * f * f + 0.6 * f * f + 0.5 * f) * color;

        // --- Masking ---
        float alpha = 1.0;
        if (_UseMask > 0.5)
        {
            float4 maskSample = tex2D(_MainTex, maskUV);
            alpha = maskSample.r * maskSample.a;
        }

        return float4(finalRGB, alpha);
    }
    
    ENDCG

    SubShader
    {
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            ENDCG
        }
    }
}*/