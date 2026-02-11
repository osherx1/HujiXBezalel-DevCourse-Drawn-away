Shader "Custom/MysticLine_HandDrawn_Ultimate"
{
    Properties
    {
        [Header(Base Settings)]
        _MainTex ("Line Texture", 2D) = "white" {}
        [HDR] _LineColor ("Base Color", Color) = (1,1,1,1)
        _LineOpacity ("Base Opacity", Range(0, 1)) = 1.0
        _LineWidth ("Thickness", Range(0.0, 0.5)) = 0.2

        [Header(Hatching Effect From Trail2)]
        [HDR] _HatchColor ("Hatch Color", Color) = (1, 1, 1, 1)
        _HatchDensity ("Hatch Density", Float) = 20
        _HatchSpeed ("Hatch Speed", Float) = 2.0
        _HatchOpacity ("Hatch Opacity", Range(0, 1)) = 0.7

        [Header(Hand Drawn Distortion)]
        _DistortTex ("Distortion Noise", 2D) = "bump" {}
        _DistortStrength ("Distort Strength", Range(0, 0.1)) = 0.02
        _GrainStrength ("Grain/Stipple Strength", Range(0, 1)) = 0.5

        [Header(Outline and Glow)]
        [HDR] _OutlineColor ("Outline Color", Color) = (0.3, 0.6, 1.0, 1)
        _OutlineOpacity ("Outline Opacity", Range(0, 1)) = 0.8
        _OutlineWidth ("Outline Width", Range(0.0, 0.5)) = 0.1
        _GlowStrength ("Glow Multiplier", Range(0, 10)) = 2

        [Header(Animation)]
        _TimeScale ("Global Time Scale", Float) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha 
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float2 screenUV : TEXCOORD1;
            };

            sampler2D _MainTex, _DistortTex;
            float4 _MainTex_ST;
            float4 _LineColor, _OutlineColor, _HatchColor;
            float _LineOpacity, _OutlineOpacity, _HatchOpacity;
            float _LineWidth, _OutlineWidth, _GlowStrength, _HatchDensity, _HatchSpeed;
            float _DistortStrength, _GrainStrength, _TimeScale;

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            float rand(float2 st) {
                return frac(sin(dot(st, float2(12.9898, 78.233))) * 43758.5453);
            }

            // פונקציית ה-Trail מהקובץ שצירפת (מותאמת למילוי הקו)
            float getHatching(float2 uv, float speed) {
                float stxn = uv.x * _HatchDensity;
                float movement = _Time.y * speed;
                float randomOffset = rand(floor(stxn) * 0.5);
                float size = sin(uv.y * 10.0 + movement + randomOffset) * 0.5 + 0.5;
                return step(0.5, frac(stxn)) * size;
            }

            float4 frag (v2f i) : SV_Target {
                float time = _Time.y * _TimeScale;
                
                // 1. הוספת עיוות "ידני" (Distortion) לפי טקסטורת רעש
                float2 distort = tex2D(_DistortTex, i.uv + time * 0.1).rg * 2.0 - 1.0;
                float2 distortedUV = i.uv + distort * _DistortStrength;

                // 2. חישוב SDF לקו (מרחק מהמרכז)
                float dist = abs(distortedUV.y - 0.5);

                // 3. יצירת מסיכות (Masks)
                float lineMask = smoothstep(_LineWidth + 0.02, _LineWidth, dist);
                float outlineMask = smoothstep(_LineWidth + _OutlineWidth + 0.02, _LineWidth + _OutlineWidth, dist);
                outlineMask = saturate(outlineMask - lineMask);

                // 4. אפקט הפסים (Hatching) מהשיידר Trail2 [cite: 12, 13, 14]
                float hatch = getHatching(distortedUV, _HatchSpeed);
                float3 hatchRGB = _HatchColor.rgb * hatch * _HatchOpacity * lineMask;

                // 5. גרעיניות (Grain) בסגנון תמונת ה-Cave Background
                float grain = rand(i.uv + frac(time));
                float grainMult = lerp(1.0, grain, _GrainStrength);

                // 6. שילוב סופי
                float4 tex = tex2D(_MainTex, distortedUV + float2(time * 0.2, 0));
                
                float3 baseRGB = (_LineColor.rgb * tex.rgb * lineMask * _LineOpacity);
                float3 outlineRGB = (_OutlineColor.rgb * outlineMask * _OutlineOpacity);
                
                float3 finalRGB = (baseRGB + outlineRGB + hatchRGB) * _GlowStrength * grainMult;
                float finalAlpha = saturate(lineMask + outlineMask + (hatch * _HatchOpacity)) * i.color.a;

                return float4(finalRGB, finalAlpha);
            }
            ENDCG
        }
    }
}