Shader "Custom/Trail2_Masked_Colored"
{
    Properties
    {
        [Header(Mask Settings)]
        _MainTex ("Mask Texture (Alpha/Grayscale)", 2D) = "white" {}
        [Toggle] _UseMask ("Enable Mask", Float) = 1

        [Header(Color Palette)]
        [HDR] _Color1 ("Layer 1 Color", Color) = (1, 0, 0, 1) // צבע לשכבה 1
        [HDR] _Color2 ("Layer 2 Color", Color) = (0, 0, 1, 1) // צבע לשכבה 2
        [HDR] _IntersectionColor ("Intersection Color", Color) = (0, 1, 0, 1) // צבע בחיתוך

        [Header(Animation Settings)]
        _Rotation ("Rotation Angle", Range(0, 360)) = 45
        _Speed ("Animation Speed", Float) = 1.3
        
        [Header(Trail Settings)]
        _Density1 ("Layer 1 Density", Float) = 45
        _Density2 ("Layer 2 Density", Float) = 20
        _Thickness ("Trail Thickness", Range(0.1, 3.0)) = 1.42
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    // --- Variables ---
    sampler2D _MainTex;
    float4 _MainTex_ST; // נדרש ל-Tiling/Offset של הטקסטורה
    
    float4 _Color1;
    float4 _Color2;
    float4 _IntersectionColor;
    float _UseMask;

    float _Rotation;
    float _Speed;
    float _Density1;
    float _Density2;
    float _Thickness;

    #define PI 3.14159265359

    // --- Helper Logic ---
    float2 fix_aspect(float2 uv)
    {
        float2 res = _ScreenParams.xy;
        // מונע חלוקה ב-0 במקרה קיצון
        if (res.y == 0) return uv; 
        float aspect = res.x / res.y;
        uv.x *= aspect;
        return uv;
    }

    float rand(float2 st)
    {
        return frac(sin(dot(st, float2(12.9898, 78.233))) * 43758.5453);
    }

    float2 rotate(float2 st, float angle)
    {
        float rad = radians(angle);
        float2x2 mat = float2x2(cos(rad), -sin(rad),
                                sin(rad), cos(rad));
        st -= 0.5;
        st = mul(mat, st);
        st += 0.5;
        return st;
    }

    float trail(float2 st, float n)
    {
        float stxn = st.x * n;
        stxn *= sin(st.y);

        float movement = _Time.y * _Speed;
        float randomOffset = rand(floor(stxn) * 0.5);
        
        float size = sin(st.y + movement + randomOffset) * _Thickness;

        st = frac(stxn);
        st = step(size, st) * step(size, 1.0 - st);

        return st.x * st.y;
    }

    float4 frag(v2f_img i) : SV_Target
    {
        // 1. שמירת UV מקורי למסיכה (Mask)
        // אנחנו רוצים שהטקסטורה תישאר ישרה, לכן לא מפעילים עליה rotate או aspect fix
        // אלא אם כן המטרה היא למתוח אותה על כל המסך. כאן הנחתי מיפוי רגיל.
        float2 maskUV = i.uv;
        
        // 2. חישוב UV לאפקט השבילים
        float2 effectUV = fix_aspect(i.uv);
        effectUV = rotate(effectUV, _Rotation);

        // 3. יצירת השכבות
        float l1 = trail(effectUV, _Density1);
        float l2 = trail(effectUV, _Density2);
        
        // חישוב החיתוך (איפה ששני הקווים נפגשים)
        float intersection = step(2, l1 + l2);
        
        // כדי שהחיתוך לא ייצבע פעמיים (גם ב-l1 וגם ב-l2), ננקה אותו מהשכבות הבסיסיות
        // זה נתון לשיקול דעת אומנותי. כאן אני משאיר אותו "מעל" הכל.
        
        // 4. הרכבת הצבעים
        float3 finalColor = (l1 * _Color1.rgb) + (l2 * _Color2.rgb);
        
        // הוספת צבע החיתוך (אופציונלי: אפשר לעשות lerp במקום חיבור אם רוצים צבע אבסולוטי)
        // כאן השתמשתי ב-lerp: איפה שיש חיתוך, נציג את צבע החיתוך. אחרת, את סכום השכבות.
        finalColor = lerp(finalColor, _IntersectionColor.rgb, intersection);

        // 5. טיפול במסיכה (Masking)
        float maskValue = 1.0;
        if (_UseMask > 0.5)
        {
            // דוגמים את ערוץ ה-Red או Alpha של הטקסטורה (תלוי בטקסטורה שלך)
            // הנחתי שהטקסטורה היא שחור-לבן (Grayscale)
            float4 maskSample = tex2D(_MainTex, maskUV);
            maskValue = maskSample.r * maskSample.a; 
        }

        // החלת המסיכה על הצבע הסופי
        finalColor *= maskValue;

        return float4(finalColor, 1.0);
    }

    ENDCG

    SubShader
    {
        Pass
        {
            // Blend One One מוסיף את הצבע לרקע (Additive) - טוב לאפקטים זוהרים
            // אם זה חומר אטום על אובייקט, אפשר לשנות או למחוק את השורה הזו
            Blend SrcAlpha OneMinusSrcAlpha 
            
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            ENDCG
        }
    }
}