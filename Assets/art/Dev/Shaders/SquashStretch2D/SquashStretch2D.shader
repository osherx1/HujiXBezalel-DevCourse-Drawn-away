Shader "Custom/SquashStretch2D"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SquashAmountX ("Squash Amount X", Float) = 1
        _SquashAmountY ("Squash Amount Y", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
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
            float _SquashAmountX;
            float _SquashAmountY;

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

                // Scale around center (pivot at center assumed)
                float2 center = float2(0.0, 0.0);
                float2 offset = v.vertex.xy - center;
                offset.x *= _SquashAmountX;
                offset.y *= _SquashAmountY;
                v.vertex.xy = center + offset;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
