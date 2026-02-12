Shader "Custom/MagicalLineSparks_Advanced"
{
    Properties
    {
        _SparkColor ("Base Spark Color", Color) = (1, 1, 1, 1)
        _SparkIntensity ("Spark Intensity", Range(0, 10)) = 3
        _SparkDensity ("Spark Density", Range(1, 100)) = 30
        _SparkSize ("Spark Size", Range(0.001, 0.05)) = 0.01
        _LifetimeRange ("Lifetime Range", Vector) = (0.2, 0.8, 0, 0)
        _LineColor ("Line Base Color", Color) = (0.1, 0.1, 0.2, 1)
        _ColorShift ("Hue Shift Along Line", Range(0,1)) = 0
        _TimeScale ("Time", Float) = 1
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _NoiseScale ("Noise Scale", Float) = 5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        ZWrite Off
        Blend One One
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _SparkColor;
            float _SparkIntensity;
            float _SparkDensity;
            float _SparkSize;
            float4 _LineColor;
            float _ColorShift;
            float _TimeScale;
            float4 _LifetimeRange;
            sampler2D _NoiseTex;
            float _NoiseScale;

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

            float3 hueShift(float3 color, float shift)
            {
                float angle = shift * 6.2831;
                float s = sin(angle), c = cos(angle);
                float3x3 mat = float3x3(
                    0.299 + 0.701 * c + 0.168 * s, 0.587 - 0.587 * c + 0.330 * s, 0.114 - 0.114 * c - 0.497 * s,
                    0.299 - 0.299 * c - 0.328 * s, 0.587 + 0.413 * c + 0.035 * s, 0.114 - 0.114 * c + 0.292 * s,
                    0.299 - 0.300 * c + 1.250 * s, 0.587 - 0.588 * c - 1.050 * s, 0.114 + 0.886 * c - 0.203 * s
                );
                return mul(mat, color);
            }

            float sparkShape(float2 uv, float2 pos, float size)
            {
                float d = length(uv - pos);
                return smoothstep(size, 0.0, d);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float t = _Time.y * _TimeScale;

                float spark = 0;

                float baseCount = _SparkDensity;
                float2 grid = floor(uv * baseCount);
                float2 f = frac(uv * baseCount);

                float2 sparkUV = (grid + 0.5) / baseCount;

                // Sample noise texture for variation
                float2 noiseUV = sparkUV * _NoiseScale;
                float noise = tex2D(_NoiseTex, noiseUV).r;

                // Randomized time offset per spark
                float lifeT = frac(t + noise);

                float fade = smoothstep(_LifetimeRange.x, _LifetimeRange.x + 0.1, lifeT) *
                             smoothstep(_LifetimeRange.y, _LifetimeRange.y - 0.1, lifeT);

                // Animated position jitter
                sparkUV += (f - 0.5) * 0.02 + 0.005 * float2(sin(t + noise * 10.0), cos(t + noise * 8.0));

                // Vary spark size
                float size = _SparkSize * (0.5 + noise);

                float shape = sparkShape(uv, sparkUV, size);
                spark += shape * fade;

                // Time-varying color tint
                float hueOffset = 0.1 * sin(t * 1.5 + noise * 6.28);
                float3 dynamicColor = hueShift(_SparkColor.rgb, hueOffset);

                float3 sparkCol = dynamicColor * spark * _SparkIntensity;
                float3 lineCol = hueShift(_LineColor.rgb, uv.x * _ColorShift);

                return float4(sparkCol + lineCol, 1);
            }
            ENDCG
        }
    }
}

/*
Shader "Custom/MagicalLineSparks_Static"
{
    Properties
    {
        _SparkColor ("Spark Color", Color) = (1, 1, 1, 1)
        _SparkIntensity ("Spark Intensity", Range(0, 10)) = 3
        _SparkDensity ("Spark Density", Range(1, 100)) = 30
        _SparkSize ("Spark Size", Range(0.001, 0.05)) = 0.01
        _LineColor ("Line Base Color", Color) = (0.1, 0.1, 0.2, 1)
        _ColorShift ("Hue Shift Along Line", Range(0,1)) = 0
        _TimeScale ("Time", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        ZWrite Off
        Blend One One
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _SparkColor;
            float _SparkIntensity;
            float _SparkDensity;
            float _SparkSize;
            float4 _LineColor;
            float _ColorShift;
            float _TimeScale;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0; // assume UV.x is line progress 0-1
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            // Hash function for pseudo-random numbers
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            // Simple glow shape (circular fade)
            float sparkShape(float2 uv, float2 pos, float size)
            {
                float d = length(uv - pos);
                return smoothstep(size, 0.0, d); // soft edge
            }

            // Optional hue shift for line color
            float3 hueShift(float3 color, float shift)
            {
                float angle = shift * 6.2831; // 0-2PI
                float s = sin(angle), c = cos(angle);
                float3x3 mat = float3x3(
                    0.299 + 0.701 * c + 0.168 * s, 0.587 - 0.587 * c + 0.330 * s, 0.114 - 0.114 * c - 0.497 * s,
                    0.299 - 0.299 * c - 0.328 * s, 0.587 + 0.413 * c + 0.035 * s, 0.114 - 0.114 * c + 0.292 * s,
                    0.299 - 0.300 * c + 1.250 * s, 0.587 - 0.588 * c - 1.050 * s, 0.114 + 0.886 * c - 0.203 * s
                );
                return mul(mat, color);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float t = _Time.y * _TimeScale;

                float sparkAccum = 0;

                // Compute spark count (static per pixel, no loop)
                float baseCount = _SparkDensity;

                // For each pixel, generate N sparks procedurally using UV hash
                // Instead of loop, scatter sparks via noise in UV space
                float2 id = floor(uv * baseCount);
                float2 rand = frac(uv * baseCount);

                float2 sparkUV = (id + 0.5) / baseCount;
                float n = hash(id + floor(t));

                // Random life cycle
                float life = frac(t + n);
                float fade = smoothstep(0.0, 0.2, life) * smoothstep(1.0, 0.8, life);

                // Spark jitter and slight time movement
                sparkUV += (rand - 0.5) * 0.02 + 0.005 * float2(sin(t + n * 6.0), cos(t + n * 4.0));

                // Spark size variation
                float size = _SparkSize * (0.5 + hash(id + 1.23) * 1.5);

                float shape = sparkShape(uv, sparkUV, size);
                sparkAccum += shape * fade;

                float3 sparkCol = _SparkColor.rgb * sparkAccum * _SparkIntensity;

                // Optional hue shift across the line
                float3 lineCol = hueShift(_LineColor.rgb, uv.x * _ColorShift);

                return float4(sparkCol + lineCol, 1);
            }
            ENDCG
        }
    }
}
*/
