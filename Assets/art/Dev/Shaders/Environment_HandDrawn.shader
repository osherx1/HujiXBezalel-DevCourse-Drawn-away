Shader "Custom/InkFluid_Boiling"
{
    Properties
    {
        [Header(Base)]
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        
        [Header(Animation Settings)]
        _FPS ("Animation FPS", Range(1, 60)) = 12
        _Amplitude ("Edge Wobble Amount", Range(0, 0.1)) = 0.005
        _WobbleScale ("Wobble Frequency", Float) = 20.0
        
        [Header(Ink Settings)]
        _InkColor ("Ink Color", Color) = (0.05, 0.05, 0.1, 1)
        _HighlightColor ("Highlight Color", Color) = (0.2, 0.2, 0.3, 1)
        _NoiseScale ("Noise Scale", Float) = 2.0
        _Speed ("Flow Speed", Float) = 0.5
        _Distortion ("Turbulence Strength", Range(0, 1)) = 0.2
        _Contrast ("Contrast", Float) = 3.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True" 
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0 // Ensure decent precision for mobile
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                float steppedTime : TEXCOORD1; // Pass calculated time to frag
            };

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            float4 _MainTex_ST;
            
            // Fixed precision is sufficient for colors
            fixed4 _InkColor;
            fixed4 _HighlightColor;
            
            // Half precision for general math to save mobile performance
            half _NoiseScale;
            half _Speed;
            half _Distortion;
            half _Contrast;
            
            // Animation Props
            half _FPS;
            half _Amplitude;
            half _WobbleScale;

            v2f vert(appdata_t v)
            {
                v2f o;
                
                // --- 1. Stepped Time Logic ---
                // We calculate this once per vertex to sync the whole mesh
                // floor(Time * FPS) / FPS creates the stair-step effect
                float timeVal = _Time.y;
                o.steppedTime = floor(timeVal * _FPS) / _FPS;

                // --- 2. Vertex Displacement (Boiling Effect) ---
                // We use World Position so the noise feels like it exists in the environment
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                
                // Generate cheap procedural noise using Sin/Cos
                // We combine position + steppedTime to make it jump every frame
                float2 wobble;
                wobble.x = sin(worldPos.y * _WobbleScale + o.steppedTime * 10.0);
                wobble.y = cos(worldPos.x * _WobbleScale + o.steppedTime * 25.0);
                
                // Apply the wobble to the vertex position (Object Space)
                // We multiply by _Amplitude to control strength
                v.vertex.xy += wobble * _Amplitude;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color; // Pass vertex color (essential for SpriteRenderer tint)
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample the base sprite shape
                fixed4 baseCol = tex2D(_MainTex, i.uv);
                
                // --- Optimization: clip() ---
                // Discards pixel if alpha is effectively zero. 
                // Better than 'if (alpha < 0) discard' on modern hardware.
                clip(baseCol.a - 0.01);

                // Use the Stepped Time passed from vertex for the internal flow
                // ensuring the ink moves at the same framerate as the outline.
                float flowTime = i.steppedTime * _Speed;

                // --- Ink Fluid Logic (Synced to Stop-Motion) ---

                // Layer 1: Base Flow
                // We step the UVs rather than sliding them smoothly
                float2 flowOffset = float2(flowTime * 0.1, flowTime * 0.2);
                float2 noiseUV1 = (i.uv * _NoiseScale) + flowOffset;
                half noise1 = tex2D(_NoiseTex, noiseUV1).r;

                // Layer 2: Turbulence / Distortion
                // We distort the second layer using the result of the first
                float2 distortOffset = (noise1 - 0.5) * _Distortion; 
                float2 noiseUV2 = (i.uv * _NoiseScale) - float2(flowTime * 0.15, -flowTime * 0.05) + distortOffset;
                half finalNoise = tex2D(_NoiseTex, noiseUV2).r;

                // --- Contrast & Coloring ---
                half combinedNoise = (noise1 + finalNoise) * 0.5;
                
                // Sharpen the noise to make it look like ink pools
                combinedNoise = saturate((combinedNoise - 0.5) * _Contrast + 0.5);

                // Mix colors
                half3 finalRGB = lerp(_InkColor.rgb, _HighlightColor.rgb, combinedNoise);
                
                // Multiply by SpriteRenderer color (i.color) to allow tinting in editor
                return fixed4(finalRGB * i.color.rgb, baseCol.a * i.color.a);
            }
            ENDCG
        }
    }
}