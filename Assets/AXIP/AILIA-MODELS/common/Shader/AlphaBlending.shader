Shader "Ailia/AlphaBlending2Tex"
{
    SubShader
    {
        Offset 0, -1
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _BlendTex;
            float blendFlag;
            float mainVFlip;
            float blendVFlip;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv2 = o.uv;
                o.uv.y = mainVFlip + o.uv.y - 2 * mainVFlip * o.uv.y;
                o.uv2.y = blendVFlip + o.uv2.y - 2 * blendVFlip * o.uv2.y;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 col2 = tex2D(_BlendTex, i.uv2);
                col.rgb = lerp(col.rgb, col2.rgb, col2.a * blendFlag);
                return col;
            }
            ENDCG
        }
    }
}
