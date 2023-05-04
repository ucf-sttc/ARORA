/*
Clive: This is the shader that is used in place of the normal shaders when rendering to the segmentation renderTexture.
It has default segmentation colors set up and uses 0 as the segmentationOutputMode by default but these properties are meant to be overridden by either a material property block or by
properties on the original shader. For the URP implementation only the first subshader is called.
*/
Shader "Custom/SegmentationShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0

		_ObjectSegmentationColor("Object segmentation color",Color) = (1,1,1,1)
		_TagSegmentationColor("Tag segmentation color",Color) = (1,1,1,1)
		_LayerSegmentationColor("Layer segmentation color",Color) = (1,1,1,1)
    }

    SubShader
    {
		Lighting Off
        CGINCLUDE

        int _SegmentationOutputMode;
        fixed4 _ObjectSegmentationColor;
        fixed4 _TagSegmentationColor;
		fixed4 _LayerSegmentationColor;

        float4 Output()
        {
			/*
            enum ReplacelementModes {
                ObjectId 			= 0,
                TagCatergoryId		= 1,
                LayerCategoryId		= 2,

            };*/
			switch (_SegmentationOutputMode)
			{
				case 0:
					return _ObjectSegmentationColor;
				case 1:
					return _TagSegmentationColor;
				case 2:
					return _LayerSegmentationColor;
				default:
					return float4(1, 0.5, 0.5, 1);// unsupported _OutputMode
			}
        }
        ENDCG

        Tags{ "RenderType" = "Opaque" }
        Pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct v2f {
                float4 pos : SV_POSITION;
                float4 nz : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            v2f vert(appdata_base v) {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.nz.xyz = COMPUTE_VIEW_NORMAL;
                o.nz.w = COMPUTE_DEPTH_01;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target {
                return Output();
            }
            ENDCG
        }
    }

	SubShader{
		Tags { "RenderType" = "TransparentCutout" }
		Pass {
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#include "UnityCG.cginc"
		struct v2f {
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
			float4 nz : TEXCOORD1;
			UNITY_VERTEX_OUTPUT_STEREO
		};
		uniform float4 _MainTex_ST;

		v2f vert(appdata_base v) {
			v2f o;
			UNITY_SETUP_INSTANCE_ID(v);
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
			o.nz.xyz = COMPUTE_VIEW_NORMAL;
			o.nz.w = COMPUTE_DEPTH_01;
			return o;
		}
		uniform sampler2D _MainTex;
		uniform fixed _Cutoff;
		uniform fixed4 _Color;
		fixed4 frag(v2f i) : SV_Target {
			fixed4 texcol = tex2D(_MainTex, i.uv);
			clip(texcol.a* _Color.a - _Cutoff);
			return Output();
		}
		ENDCG
		}
	}

	SubShader{
		Tags { "RenderType" = "TreeBark" }
		Pass {
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#include "UnityCG.cginc"
		#include "Lighting.cginc"
		#include "UnityBuiltin3xTreeLibrary.cginc"
		struct v2f {
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
			float4 nz : TEXCOORD1;
			UNITY_VERTEX_OUTPUT_STEREO
		};
		v2f vert(appdata_full v) {
			v2f o;
			UNITY_SETUP_INSTANCE_ID(v);
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
			TreeVertBark(v);

			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv = v.texcoord.xy;
			o.nz.xyz = COMPUTE_VIEW_NORMAL;
			o.nz.w = COMPUTE_DEPTH_01;
			return o;
		}
		fixed4 frag(v2f i) : SV_Target {
			return Output();
		}
		ENDCG
			}
	}

	SubShader{
		Tags { "RenderType" = "TreeLeaf" }
		Pass {
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#include "UnityCG.cginc"
		#include "Lighting.cginc"
		#include "UnityBuiltin3xTreeLibrary.cginc"
		struct v2f {
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
			float4 nz : TEXCOORD1;
			UNITY_VERTEX_OUTPUT_STEREO
		};
		v2f vert(appdata_full v) {
			v2f o;
			UNITY_SETUP_INSTANCE_ID(v);
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
			TreeVertLeaf(v);

			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv = v.texcoord.xy;
			o.nz.xyz = COMPUTE_VIEW_NORMAL;
			o.nz.w = COMPUTE_DEPTH_01;
			return o;
		}
		uniform sampler2D _MainTex;
		uniform fixed _Cutoff;
		fixed4 frag(v2f i) : SV_Target {
			half alpha = tex2D(_MainTex, i.uv).a;

			clip(alpha - _Cutoff);
			return Output();
		}
		ENDCG
		}
	}

	SubShader{
		Tags { "RenderType" = "TreeOpaque" "DisableBatching" = "True" }
		Pass {
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#include "UnityCG.cginc"
		#include "TerrainEngine.cginc"
		
		struct v2f {
			float4 pos : SV_POSITION;
			float4 nz : TEXCOORD0;
			UNITY_VERTEX_OUTPUT_STEREO
		};
		struct appdata {
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			fixed4 color : COLOR;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};
		v2f vert(appdata v) {
			v2f o;
			UNITY_SETUP_INSTANCE_ID(v);
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
			//TerrainAnimateTree(v.vertex, v.color.w);
			o.pos = UnityObjectToClipPos(v.vertex);
			o.nz.xyz = COMPUTE_VIEW_NORMAL;
			o.nz.w = COMPUTE_DEPTH_01;
			return o;
		}
		fixed4 frag(v2f i) : SV_Target {
			return Output();
		} 
		ENDCG
		}
	}

	SubShader{
		Tags { "RenderType" = "TreeTransparentCutout" "DisableBatching" = "True" }
		Pass {
			Cull Back
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "TerrainEngine.cginc"

			struct v2f {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 nz : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};
			struct appdata {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				fixed4 color : COLOR;
				float4 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			v2f vert(appdata v) {
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				//TerrainAnimateTree(v.vertex, v.color.w);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.texcoord.xy;
				o.nz.xyz = COMPUTE_VIEW_NORMAL;
				o.nz.w = COMPUTE_DEPTH_01;
				return o;
			}
			uniform sampler2D _MainTex;
			uniform fixed _Cutoff;
			fixed4 frag(v2f i) : SV_Target {
				half alpha = tex2D(_MainTex, i.uv).a;

				clip(alpha - _Cutoff);
				return Output();
			}
			ENDCG
		}
		Pass {
		Cull Front
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#include "UnityCG.cginc"
		#include "TerrainEngine.cginc"

			struct v2f {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 nz : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};
			struct appdata {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				fixed4 color : COLOR;
				float4 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			v2f vert(appdata v) {
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				//TerrainAnimateTree(v.vertex, v.color.w);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.texcoord.xy;
				o.nz.xyz = -COMPUTE_VIEW_NORMAL;
				o.nz.w = COMPUTE_DEPTH_01;
				return o;
			}
			uniform sampler2D _MainTex;
			uniform fixed _Cutoff;
			fixed4 frag(v2f i) : SV_Target {
				fixed4 texcol = tex2D(_MainTex, i.uv);
				clip(texcol.a - _Cutoff);
				return Output();
			}
		ENDCG
		}

	}

	SubShader{
	Tags { "RenderType" = "TreeBillboard" }
	Pass {
	Cull Off
	CGPROGRAM
	#pragma vertex vert
	#pragma fragment frag
	#include "UnityCG.cginc"
	#include "TerrainEngine.cginc"
		struct v2f {
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
			float4 nz : TEXCOORD1;
			UNITY_VERTEX_OUTPUT_STEREO
		};
		v2f vert(appdata_tree_billboard v) {
			v2f o;
			UNITY_SETUP_INSTANCE_ID(v);
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
			TerrainBillboardTree(v.vertex, v.texcoord1.xy, v.texcoord.y);
			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv.x = v.texcoord.x;
			o.uv.y = v.texcoord.y > 0;
			o.nz.xyz = float3(0,0,1);
			o.nz.w = COMPUTE_DEPTH_01;
			return o;
		}
		uniform sampler2D _MainTex;
		fixed4 frag(v2f i) : SV_Target {
			fixed4 texcol = tex2D(_MainTex, i.uv);
			clip(texcol.a - 0.001);
			return Output();
		}
	ENDCG
		}
	}

	SubShader{
		Tags { "RenderType" = "GrassBillboard" }
		Pass {
			Cull Off
	CGPROGRAM
	#pragma vertex vert
	#pragma fragment frag
	#include "UnityCG.cginc"
	#include "TerrainEngine.cginc"

	struct v2f {
		float4 pos : SV_POSITION;
		fixed4 color : COLOR;
		float2 uv : TEXCOORD0;
		float4 nz : TEXCOORD1;
		UNITY_VERTEX_OUTPUT_STEREO
	};

	v2f vert(appdata_full v) {
		v2f o;
		UNITY_SETUP_INSTANCE_ID(v);
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
		WavingGrassBillboardVert(v);
		o.color = v.color;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = v.texcoord.xy;
		o.nz.xyz = COMPUTE_VIEW_NORMAL;
		o.nz.w = COMPUTE_DEPTH_01;
		return o;
	}
	uniform sampler2D _MainTex;
	uniform fixed _Cutoff;
	fixed4 frag(v2f i) : SV_Target {
		fixed4 texcol = tex2D(_MainTex, i.uv);
		fixed alpha = texcol.a * i.color.a;
		clip(alpha - _Cutoff);
		return Output();
	}
	ENDCG
		}
		}

	SubShader{
		Tags { "RenderType" = "Grass" }
		Pass {
		Cull Off
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#include "UnityCG.cginc"
		#include "TerrainEngine.cginc"
			struct v2f {
				float4 pos : SV_POSITION;
				fixed4 color : COLOR;
				float2 uv : TEXCOORD0;
				float4 nz : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert(appdata_full v) {
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				WavingGrassVert(v);
				o.color = v.color;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.texcoord;
				o.nz.xyz = COMPUTE_VIEW_NORMAL;
				o.nz.w = COMPUTE_DEPTH_01;
				return o;
			}
			uniform sampler2D _MainTex;
			uniform fixed _Cutoff;
			fixed4 frag(v2f i) : SV_Target {
				fixed4 texcol = tex2D(_MainTex, i.uv);
				fixed alpha = texcol.a * i.color.a;
				clip(alpha - _Cutoff);
				return Output();
			}
		ENDCG
		}
	}
	
	SubShader
	{
		Tags{ "RenderType" = "Background" }
		Pass{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			struct v2f {
				float4 pos : SV_POSITION;
				float4 nz : TEXCOORD0;
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert(appdata_base v) {
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.nz.xyz = COMPUTE_VIEW_NORMAL;
				o.nz.w = COMPUTE_DEPTH_01;
				return o;
			}
			fixed4 frag(v2f i) : SV_Target{
				return float4(0,0,0,1);
			}
			ENDCG
		}
	}
	Fallback "UniversalForward"
}
