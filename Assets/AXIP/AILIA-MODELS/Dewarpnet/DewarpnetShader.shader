Shader "Ailia/DewarpnetShader"
{
    SubShader
    {
        Offset 0, -1
        Tags { "RenderType" = "Opaque" }
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
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _uvTex;
            float blendFlag;
            float mainVFlip;
            float4 _MainTex_ST;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv.y = mainVFlip + o.uv.y - 2 * mainVFlip * o.uv.y;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 uvTex = tex2D(_uvTex, i.uv);
                uvTex.y = 1 - uvTex.y;
                fixed4 col = tex2D(_MainTex, uvTex);
                fixed4 col_original = tex2D(_MainTex, float2(i.uv.x, 1 - i.uv.y));
                return col * blendFlag + (1 - blendFlag) * col_original;

            }
            ENDCG
        }
    }
}

