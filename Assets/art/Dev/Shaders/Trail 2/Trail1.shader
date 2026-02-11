Shader "ShaderSketches/Trail1_FullControl"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white"{}

        [Header(Colors)]
        _BaseColor ("Background Color", Color) = (0, 0.1, 0, 1) // ירוק כהה כברירת מחדל
        _Layer1Color ("Layer 1 Color", Color) = (1, 0.2, 0.2, 1) // אדמדם
        _Layer2Color ("Layer 2 Color", Color) = (0.2, 0.2, 1, 1) // כחלחל

        [Header(Animation Settings)]
        _Speed ("Animation Speed", Float) = 1.3
        _GlobalRot ("Global Rotation (Radians)", Float) = 0.785398 
        
        [Header(Grid Settings)]
        _Density1 ("Layer 1 Density", Float) = 45.0
        _Density2 ("Layer 2 Density", Float) = 20.0
    }

    CGINCLUDE
    #include "UnityCG.cginc"
    // וודא ש-Common.cginc נמצא בפרויקט שלך. אם לא, מחק את השורה והשתמש בתיקון שהוצע למטה ב-frag

    #define PI 3.14159265359
    
    // --- הגדרת המשתנים ---
    float _Speed;
    float _GlobalRot;
    float _Density1;
    float _Density2;
    
    // משתני הצבע החדשים (fixed4 יעיל יותר לצבעים)
    fixed4 _BaseColor;
    fixed4 _Layer1Color;
    fixed4 _Layer2Color;

    float rand(float2 uv)
    {
        return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
    }

    float2 rotate(float2 st, float angle)
    {
        float2x2 mat = float2x2(cos(angle), -sin(angle),
                                sin(angle), cos(angle));
        st -= 0.5;
        st = mul(mat, st);
        st += 0.5;
        return st;
    }

    float box(float2 st, float t)
    {
        st = rotate(st, t * 2.05 * PI / 4);
        float size = t * 1.42;
        st = step(size, st) * step(size, 1.0 - st);
        return st.x * st.y;
    }

    float lattice(float2 st, float n)
    {
        float size = sin(st.y + _Time.y * _Speed + rand(floor(st * n).x * 0.5));
        return box(frac(st * n), size);
    }

    float4 frag(v2f_img i) : SV_Target
    {
        // תיקון יחס מסך (במידה ואין לך את Common.cginc)
         i.uv.x *= _ScreenParams.x / _ScreenParams.y;
        //i.uv = screen_aspect(i.uv); 

        i.uv = rotate(i.uv, _GlobalRot);

        // חישוב שתי שכבות הסריג (ערכים בין 0 ל-1)
        float l1 = lattice(i.uv, _Density1);
        float l2 = lattice(i.uv, _Density2);
        
        // --- לוגיקת הצבע החדשה ---
        // מתחילים מצבע הרקע
        float3 finalColor = _BaseColor.rgb;
        
        // מוסיפים את שכבה 1 צבועה בצבע שלה
        finalColor += l1 * _Layer1Color.rgb;
        
        // מוסיפים את שכבה 2 צבועה בצבע שלה
        finalColor += l2 * _Layer2Color.rgb;
        
        // מחזירים את התוצאה עם אלפא 1 (אטום)
        return float4(finalColor, 1.0);
    }

    ENDCG

    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            ENDCG
        }
    }
}