Shader "Custom/2DCloud"
{
    Properties
    {
        _MainTex    ("Texture", 2D) = "white" {}
        _Color      ("Color", Color) = (1,1,1,1)

        _Amplitude  ("Wave Amplitude", Float) = 0.1
        _Frequency  ("Wave Frequency", Float) = 8.0
        _Speed      ("Wave Speed", Float) = 2.0
        _Curvature  ("Static Curvature", Float) = 0.2
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            float _Amplitude;
            float _Frequency;
            float _Speed;
            float _Curvature;

            struct appdata
            {
                float4 vertex   : POSITION;
                float2 uv       : TEXCOORD0;
                fixed4 color    : COLOR;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 uv       : TEXCOORD0;
                fixed4 color    : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;

                // t = position along rope (0..1)
                float t = v.uv.x;

                // waving animation
                float wave = sin(t * _Frequency + _Time.y * _Speed) * _Amplitude;

                // static sag (like hanging cable)
                float sag = -_Curvature * (t - 0.5) * (t - 0.5);

                v.vertex.y += wave + sag;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                clip(c.a - 0.001); // sprite-style alpha cut
                return c;
            }
            ENDCG
        }
    }
}
