// From https://github.com/asus4/tf-lite-unity-sample/blob/master/Packages/com.github.asus4.tflite.common/Resources/Resize.shader
Shader "Hidden/TFLite/Resize"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }
        SubShader
    {
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            uniform float4x4 _VertTransform;
            uniform float4 _UVRect; // the same as _MainTex_ST;

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
                o.vertex = UnityObjectToClipPos(mul(_VertTransform, v.vertex));
                o.uv = v.uv *_UVRect.xy + _UVRect.zw;
                return o;
            }

            sampler2D _MainTex;

            fixed4 frag(v2f i) : SV_Target
            {
                if (i.uv.x < 0 || i.uv.x > 1 || i.uv.y < 0 || i.uv.y > 1)
                {
                    return fixed4(0, 0, 0, 0);
                }
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}