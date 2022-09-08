Shader "Unlit/TreeColliderSegmentation"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        //_SegmentationOutputMode("Segmentation Mode", Int) = 1
        _ObjectSegmentationColor("Object segmentation color",Color) = (1,1,1,1)
        _TagSegmentationColor("Tag segmentation color",Color) = (1,1,1,1)
        _LayerSegmentationColor("Layer segmentation color",Color) = (1,1,1,1)
        _CustomSegmentationColor("Custom segmentation color",Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 100

        Pass
        {
            Name "Segmentation"
            Tags { "LightMode" = "Segmentation" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            int _SegmentationOutputMode;
            float4 _ObjectSegmentationColor;
            float4 _TagSegmentationColor;
            float4 _LayerSegmentationColor;
            float4 _CustomSegmentationColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                half4 color;
                switch (_SegmentationOutputMode)
                {
                case 0:
                    color = _ObjectSegmentationColor; break;
                case 1:
                    color = _TagSegmentationColor; break;
                case 2:
                    color = _LayerSegmentationColor; break;
                case 3:
                    color = _CustomSegmentationColor; break;
                default:
                    color = float4(1, 0.5, 0.5, 1); break;// unsupported _OutputMode
                }
                return color;
            }
            ENDCG
        }
    }
}
