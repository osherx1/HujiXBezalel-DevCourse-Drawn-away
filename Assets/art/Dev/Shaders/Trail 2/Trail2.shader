Shader "Custom/Trail2_Ultimate_WithBackground"
{
    Properties
    {
        [Header(Mask Configuration)]
        _MainTex ("Mask Texture (Alpha/Grayscale)", 2D) = "white" {}
        // Tiling & Offset נשלטים מכאן
        [Toggle] _UseMask ("Enable Mask", Float) = 1

        [Header(Colors)]
        _BackgroundColor ("Background Color", Color) = (0, 0, 0, 1) // הצבע השחור שביקשת לשנות
        [HDR] _Color1 ("Layer 1 Color", Color) = (1, 0, 0, 1)
        [HDR] _Color2 ("Layer 2 Color", Color) = (0, 0, 1, 1)
        [HDR] _IntersectionColor ("Intersection Color", Color) = (0, 1, 0, 1)

        [Header(Animation)]
        _Rotation ("Rotation Angle", Range(0, 360)) = 45
        _Speed ("Animation Speed", Float) = 1.3
        
        [Header(Trail Settings)]
        _Density1 ("Layer 1 Density", Float) = 45
        _Density2 ("Layer 2 Density", Float) = 20
        _Thickness ("Trail Thickness", Range(0.1, 3.0)) = 1.42
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float4 _MainTex_ST;
    
    float4 _BackgroundColor;
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

    float2 fix_aspect(float2 uv)
    {
        float2 res = _ScreenParams.xy;
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
        // 1. חישוב UV למסיכה
        float2 maskUV = i.uv * _MainTex_ST.xy + _MainTex_ST.zw;
        
        // 2. חישוב UV לאפקט
        float2 effectUV = fix_aspect(i.uv);
        effectUV = rotate(effectUV, _Rotation);

        // 3. יצירת הפסים
        float l1 = trail(effectUV, _Density1);
        float l2 = trail(effectUV, _Density2);
        float intersection = step(2, l1 + l2);
        
        // 4. חישוב צבע הפסים (Trails Logic)
        float3 trailsColor = (l1 * _Color1.rgb) + (l2 * _Color2.rgb);
        trailsColor = lerp(trailsColor, _IntersectionColor.rgb, intersection);

        // 5. הרכבת הצבע הסופי: רקע + פסים
        // אנחנו מוסיפים את הפסים על גבי הרקע
        float3 finalRGB = _BackgroundColor.rgb + trailsColor;

        // 6. טיפול במסיכה (Mask Logic)
        float maskAlpha = 1.0;
        if (_UseMask > 0.5)
        {
            float4 maskSample = tex2D(_MainTex, maskUV);
            // המסיכה מגדירה את השקיפות הכללית של האובייקט
            maskAlpha = maskSample.r * maskSample.a;
        }

        // מחזירים את הצבע המחושב, ואת האלפא לפי המסיכה
        return float4(finalRGB, maskAlpha);
    }

    ENDCG

    SubShader
    {
        Pass
        {
            // מאפשר שקיפות (אם המסיכה או הרקע שקופים)
            Blend SrcAlpha OneMinusSrcAlpha 
            
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            ENDCG
        }
    }
}