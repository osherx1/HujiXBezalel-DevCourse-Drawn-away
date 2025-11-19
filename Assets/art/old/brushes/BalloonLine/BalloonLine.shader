Shader "Custom/BalloonLinePro_SinglePass"
{
    Properties
    {
        [Header(Balloon Settings)]
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _Smooth ("Edge Smoothness", Range(0.001, 0.5)) = 0.05
        
        [Header(Shine Settings)]
        _ShineColor ("Shine Color", Color) = (1,1,1,1)
        _ShinePower ("Shine Power", Range(0, 5)) = 1.2
        _ShinePos ("Shine Position Y", Range(0,1)) = 0.25
        _ShineWidth ("Shine Width", Range(0.01, 1)) = 0.45

        [Header(Outline Settings)]
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness ("Outline Thickness", Range(0, 1)) = 0.15
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
            "IgnoreProjector"="True" 
        }
        
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off 

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Properties
            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            float4 _Color;
            float _Smooth;

            float4 _ShineColor;
            float _ShinePower;
            float _ShinePos;
            float _ShineWidth;

            float4 _OutlineColor;
            float _OutlineThickness;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. Calculate Distance from center (0.0 = Center, 1.0 = Edge)
                // LineRenderer UV.y goes from 0 to 1. Center is 0.5.
                float distFromCenter = abs(i.uv.y - 0.5) * 2.0;

                // 2. Calculate Outline Logic
                // Define where the outline starts (e.g., at 0.8 distance)
                float outlineStart = 1.0 - _OutlineThickness;
                
                // Create a soft transition factor between Body and Outline to avoid aliasing
                float outlineFactor = smoothstep(outlineStart - _Smooth, outlineStart, distFromCenter);

                // 3. Calculate Main Texture & Body Color
                float4 texCol = tex2D(_MainTex, i.uv) * _Color;

                // 4. Calculate Shine (Only applies to the Body)
                float shineDist = abs(i.uv.y - _ShinePos);
                float shine = pow(max(0, 1.0 - shineDist / _ShineWidth), 3) * _ShinePower;
                
                // Add shine color, but mask it out where the outline begins
                float4 bodyWithShine = texCol + (_ShineColor * shine);
                
                // 5. Blend Body and Outline
                // If outlineFactor is 0 -> Body, if 1 -> Outline
                float4 finalColor = lerp(bodyWithShine, _OutlineColor, outlineFactor);

                // 6. Calculate Alpha (Fade out edges of the line)
                // Smoothstep from 1.0 down to (1.0 - smooth)
                float alpha = smoothstep(1.0, 1.0 - _Smooth, distFromCenter);

                finalColor.a *= alpha;

                return finalColor;
            }
            ENDCG
        }
    }
}

/*
Shader "Custom/BalloonLinePro"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _Smooth ("Smooth Edge", Range(0,1)) = 0.7
        _Shine ("Shine Power", Range(0,3)) = 1.2
        _ShinePos ("Shine Position", Range(0,1)) = 0.25
        _ShineWidth ("Shine Width", Range(0,1)) = 0.45
    }

    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Color;
            float _Smooth;
            float _Shine;
            float _ShinePos;
            float _ShineWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // התאמה ל-UV של LineRenderer: 0-1 לאורך עובי הקו
                float v = abs(i.uv.y - 0.5) * 2.0;

                // smooth בקצוות בלבד
                float edge = smoothstep(1.0, _Smooth, v);

                // טקסטורה
                float4 tex = tex2D(_MainTex, i.uv);

                // SHINE במיקום לבחירה לאורך העבודה של הקו
                float shineDist = abs(i.uv.y - _ShinePos);
                float shine = pow(max(0, 1.0 - shineDist / _ShineWidth), 3) * _Shine;

                float4 col = tex * _Color;
                col.rgb += shine;

                col.a *= edge;
                return col;
            }
            ENDCG
        }
    }
}

*/


/*
Shader "Custom/BalloonLine"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _Thickness ("Thickness", Float) = 1.0
        _Smooth ("Smooth Edge", Range(0,1)) = 0.7
        _Shine ("Shine Power", Range(0,3)) = 1.2
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Color;
            float _Thickness;
            float _Smooth;
            float _Shine;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float sdCircle(float2 p)        // Signed distance from center
            {
                return length(p) - 0.5;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 centeredUV = (i.uv - 0.5) * _Thickness;

                // Distance from circular balloon shape
                float d = sdCircle(centeredUV);

                float edge = smoothstep(_Smooth, 0.0, -d);

                float shine = pow(max(0, 1 - length(centeredUV * 1.4)), 4) * _Shine;

                float4 col = tex2D(_MainTex, i.uv) * _Color;
                col.rgb += shine;

                col.a *= edge;
                return col;
            }
            ENDCG
        }
    }
}
*/