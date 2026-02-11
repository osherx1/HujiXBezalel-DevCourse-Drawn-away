Shader "Custom/MysticPreDrawLine_FullControl_Opacity"
{
    Properties
    {
        [Header(Base Settings)]
        _MainTex ("Line Texture (Scrollable)", 2D) = "white" {}
        [HDR] _LineColor ("Line Base Color", Color) = (1,1,1,1)
        _LineOpacity ("Line Core Opacity", Range(0, 1)) = 1.0
        _LineWidth ("Line Thickness", Range(0.0, 0.5)) = 0.2

        [Header(Outline and Glow)]
        [HDR] _OutlineColor ("Outline Color", Color) = (0.3, 0.6, 1.0, 1)
        _OutlineOpacity ("Outline Opacity", Range(0, 1)) = 0.8
        _OutlineWidth ("Outline Width", Range(0.0, 0.5)) = 0.1
        _SmoothWidth ("Edge Smoothness", Range(0.01, 0.2)) = 0.05
        _GlowStrength ("Glow Multiplier", Range(0, 10)) = 2

        [Header(Magic Dust)]
        [HDR] _DustColor ("Dust Color", Color) = (1.0, 0.9, 0.8, 1)
        _DustOpacity ("Dust Opacity", Range(0, 1)) = 0.5
        _DustDensity ("Dust Density", Float) = 20
        _DustSize ("Dust Size", Range(0.001, 0.1)) = 0.02
        _DustSpeed ("Dust Movement Speed", Float) = 0.5

        [Header(Animation)]
        _TimeScale ("Global Time Scale", Float) = 1
        _ScrollSpeed ("Texture Scroll Speed", Float) = 0.2
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
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
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _LineColor, _OutlineColor, _DustColor;
            float _LineOpacity, _OutlineOpacity, _DustOpacity;
            float _LineWidth, _OutlineWidth, _SmoothWidth, _GlowStrength;
            float _DustDensity, _DustSize, _DustSpeed, _TimeScale, _ScrollSpeed;

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color; 
                return o;
            }

            float hash(float2 p) {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            float4 frag (v2f i) : SV_Target {
                float2 uv = i.uv;
                float time = _Time.y * _TimeScale;
                
                float dist = abs(uv.y - 0.5);

                float lineMask = smoothstep(_LineWidth + _SmoothWidth, _LineWidth, dist);
                float outlineMask = smoothstep(_LineWidth + _OutlineWidth + _SmoothWidth, _LineWidth + _OutlineWidth, dist);
                outlineMask = saturate(outlineMask - lineMask);

                float2 dustUV = uv * float2(_DustDensity, 1.0);
                float2 gridID = floor(dustUV);
                float2 cellUV = frac(dustUV) - 0.5;
                float h = hash(gridID);
                float life = frac(time * _DustSpeed + h);
                float fade = smoothstep(0, 0.5, life) * smoothstep(1, 0.5, life);
                float2 offset = float2(0, sin(time + h * 6.28) * 0.2);
                float dustShape = smoothstep(_DustSize, 0.0, length(cellUV - offset));
                
                float4 tex = tex2D(_MainTex, uv + float2(time * _ScrollSpeed, 0));
                
             
                float3 coreRGB = _LineColor.rgb * tex.rgb * lineMask * _LineOpacity;
                float3 outlineRGB = _OutlineColor.rgb * outlineMask * _OutlineOpacity;
                float3 dustRGB = _DustColor.rgb * dustShape * fade * _DustOpacity;

                float3 finalRGB = (coreRGB + outlineRGB) * _GlowStrength + dustRGB;

                float finalAlpha = saturate((lineMask * _LineOpacity) + 
                                            (outlineMask * _OutlineOpacity) + 
                                            (dustShape * fade * _DustOpacity));
                
                finalAlpha *= i.color.a;

                return float4(finalRGB, finalAlpha);
            }
            ENDCG
        }
    }
}
/*Shader "Custom/MysticPreDrawLine_FullControl_Smooth"
{
    Properties
    {
        [Header(Base Settings)]
        _MainTex ("Line Texture (Scrollable)", 2D) = "white" {}
        [HDR] _LineColor ("Line Base Color", Color) = (1,1,1,1)
        _LineWidth ("Line Thickness", Range(0.0, 0.5)) = 0.2

        [Header(Outline and Glow)]
        [HDR] _OutlineColor ("Outline Color", Color) = (0.3, 0.6, 1.0, 1)
        _OutlineWidth ("Outline Width", Range(0.0, 0.5)) = 0.1
        _SmoothWidth ("Edge Smoothness", Range(0.01, 0.2)) = 0.05
        _GlowStrength ("Glow Multiplier", Range(0, 10)) = 2

        [Header(Magic Dust)]
        [HDR] _DustColor ("Dust Color", Color) = (1.0, 0.9, 0.8, 1)
        _DustDensity ("Dust Density", Float) = 20
        _DustSize ("Dust Size", Range(0.001, 0.1)) = 0.02
        _DustSpeed ("Dust Movement Speed", Float) = 0.5

        [Header(Animation)]
        _TimeScale ("Global Time Scale", Float) = 1
        _NoiseTex ("Noise (R)", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        // Use Alpha Blending for better control, or Blend One One for pure additive glow
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
                float4 color : COLOR; // Support LineRenderer Vertex Colors
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            float4 _MainTex_ST;
            
            float4 _LineColor, _OutlineColor, _DustColor;
            float _LineWidth, _OutlineWidth, _SmoothWidth, _GlowStrength;
            float _DustDensity, _DustSize, _DustSpeed, _TimeScale;

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            // Pseudo-random function for dust
            float hash(float2 p) {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            float4 frag (v2f i) : SV_Target {
                float2 uv = i.uv;
                float time = _Time.y * _TimeScale;
                
                // 1D SDF: Distance from the horizontal center of the Line Renderer
                // Since LineRenderer UV.y goes 0 to 1, 0.5 is the center.
                float dist = abs(uv.y - 0.5);

                // --- Masks ---
                // Core Line
                float lineMask = smoothstep(_LineWidth + _SmoothWidth, _LineWidth, dist);
                // Outline (wrapped around the core)
                float outlineMask = smoothstep(_LineWidth + _OutlineWidth + _SmoothWidth, _LineWidth + _OutlineWidth, dist);
                outlineMask = saturate(outlineMask - lineMask);

                // --- Magic Dust logic ---
                float2 dustUV = uv * float2(_DustDensity, 1.0);
                float2 gridID = floor(dustUV);
                float2 cellUV = frac(dustUV) - 0.5;
                
                float h = hash(gridID);
                float life = frac(time * _DustSpeed + h);
                float fade = smoothstep(0, 0.5, life) * smoothstep(1, 0.5, life);
                
                // Animate dust position within its "zone"
                float2 offset = float2(0, sin(time + h * 6.28) * 0.2);
                float dustShape = smoothstep(_DustSize, 0.0, length(cellUV - offset));
                float dustAlpha = dustShape * fade * step(0.1, dist); // Only show dust near/outside line

                // --- Final Composition ---
                float4 tex = tex2D(_MainTex, uv + float2(time * 0.1, 0));
                
                float3 finalRGB = (_LineColor.rgb * tex.rgb * lineMask);
                finalRGB += (_OutlineColor.rgb * outlineMask);
                finalRGB *= _GlowStrength;
                
                // Add dust on top
                finalRGB += (_DustColor.rgb * dustAlpha * 2.0);

                float finalAlpha = saturate(lineMask + outlineMask + dustAlpha) * i.color.a;

                return float4(finalRGB, finalAlpha);
            }
            ENDCG
        }
    }
}*/


/*
Shader "Custom/MysticPreDrawLine"
{
    Properties
    {
        _LineColor ("Line Glow Color", Color) = (0.4, 0.6, 1.0, 1)
        _DustColor ("Dust Color", Color) = (1.0, 0.9, 0.8, 1)
        _LineWidth ("Line Width", Range(0.001, 0.1)) = 0.02
        _GlowStrength ("Glow Strength", Range(0, 5)) = 2
        _DustDensity ("Dust Density", Range(1, 100)) = 40
        _DustSize ("Dust Size", Range(0.001, 0.03)) = 0.008
        _TimeScale ("Time Scale", Float) = 1
        _NoiseTex ("Noise", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend One One
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _LineColor;
            float4 _DustColor;
            float _LineWidth;
            float _GlowStrength;
            float _DustDensity;
            float _DustSize;
            float _TimeScale;
            sampler2D _NoiseTex;

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

            float sdfLine(float2 uv, float y, float halfWidth)
            {
                float2 p = uv;
                p.x = clamp(p.x, 0.0, 1.0);
                p.y -= y;
                return length(p - uv) - halfWidth;
            }

            float4 hueShift(float4 col, float shift)
            {
                float angle = shift * 6.2831;
                float s = sin(angle), c = cos(angle);
                float3x3 mat = float3x3(
                    0.299 + 0.701 * c + 0.168 * s, 0.587 - 0.587 * c + 0.330 * s, 0.114 - 0.114 * c - 0.497 * s,
                    0.299 - 0.299 * c - 0.328 * s, 0.587 + 0.413 * c + 0.035 * s, 0.114 - 0.114 * c + 0.292 * s,
                    0.299 - 0.300 * c + 1.250 * s, 0.587 - 0.588 * c - 1.050 * s, 0.114 + 0.886 * c - 0.203 * s
                );
                return float4(mul(mat, col.rgb), col.a);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float time = _Time.y * _TimeScale;

                // ==== 1. Glow Line ====
                float dist = sdfLine(uv, 0.5, _LineWidth * 0.5);
                float glow = smoothstep(_LineWidth, 0.0, abs(dist)); // Base glow
                float pulse = 0.8 + 0.2 * sin(time * 3.0 + uv.x * 10.0); // breathing effect
                float4 lineCol = _LineColor * glow * pulse * _GlowStrength;

                // ==== 7. Magic Dust ====
                float2 grid = floor(uv * _DustDensity);
                float2 rand = frac(uv * _DustDensity);

                float2 dustUV = (grid + 0.5) / _DustDensity;

                float noise = tex2D(_NoiseTex, dustUV * 5.0).r;
                float dustTime = frac(time + noise);

                float fade = smoothstep(0.0, 0.2, dustTime) * smoothstep(1.0, 0.8, dustTime);

                // Gentle vertical float
                dustUV.y += 0.02 * sin(time * 2.0 + noise * 10.0);

                float dust = smoothstep(_DustSize, 0.0, distance(uv, dustUV)) * fade;
                float4 dustCol = _DustColor * dust;

                return lineCol + dustCol;
            }
            ENDCG
        }
    }
}
*/
