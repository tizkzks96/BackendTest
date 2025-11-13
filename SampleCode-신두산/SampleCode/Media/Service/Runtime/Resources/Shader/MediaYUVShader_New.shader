//Shader "il/Media/YUV_New"
//{
//	Properties
//	{
//		[Enum(Default, 0, Waiting, 1, Normal, 2)] _DisplayMode("Display", Int) = 0
//		_BaseTex("Default Texture", 2D) = "white" {}
//		_WaitTex("Waiting Texture", 2D) = "white" {}
//		_MainTex("YUV", 2D) = "black" {}
//		_Angle("Angle", Float) = 0
//	}
//	SubShader
//	{
//		Tags { "RenderType" = "Opaque" }
//		LOD 100
//
//		Pass
//		{
//			CGPROGRAM
//			#pragma vertex vert
//			#pragma fragment frag
//
//			#include "UnityCG.cginc"
//
//			struct appdata
//			{
//				float4 vertex : POSITION;
//				float2 defaultUV : TEXCOORD0;
//				float2 waitingUV : TEXCOORD1;
//				float2 yuvUV : TEXCOORD2;
//			};
//
//			struct v2f
//			{
//				float4 vertex : SV_POSITION;
//				float2 defaultUV : TEXCOORD0;
//				float2 waitingUV : TEXCOORD1;
//				float2 yuvUV : TEXCOORD2;
//
//				float2 yUV : TEXCOORD3;
//				float2 uUV : TEXCOORD4;
//				float2 vUV : TEXCOORD5;
//			};
//
//			sampler2D _BaseTex;
//			sampler2D _WaitTex;
//			sampler2D _MainTex;
//
//			float4 _BaseTex_ST;
//			float4 _WaitTex_ST;
//			float4 _MainTex_ST;
//
//			float _DisplayMode;
//			float _Angle;
//
//			// UV 시계방향 회전.
//			float2 rotateUvs(float2 texCoords, float theta)
//			{
//				float2 center = float2(0.5f, 0.5f);
//
//				float2 sc;
//				sincos((-theta / 180.0f * 3.141592f), sc.x, sc.y);
//
//				float2 uv = texCoords - center;
//
//				float2 rotatedUv;
//				rotatedUv.x = dot(uv, float2(sc.y, -sc.x));
//				rotatedUv.y = dot(uv, sc.xy);
//
//				rotatedUv += center;
//				return rotatedUv;
//			}
//
//			v2f vert(appdata v)
//			{
//				v2f o;
//				o.vertex = UnityObjectToClipPos(v.vertex);
//				o.defaultUV = TRANSFORM_TEX(v.defaultUV, _BaseTex);
//				o.waitingUV = TRANSFORM_TEX(v.waitingUV, _WaitTex);
//
//				float2 origin = TRANSFORM_TEX(v.yuvUV, _MainTex);
//				o.yuvUV = rotateUvs(fixed2(origin.x, 1 - origin.y), _Angle);
//
//				float offsetY = 0.6666f;
//				o.yUV = float2(o.yuvUV.x, o.yuvUV.y * offsetY);
//				o.uUV = float2(o.yUV.x * 0.5f, (o.yUV.y * 0.25f) + offsetY);
//				o.vUV = float2(o.yUV.x * 0.5f, o.uUV.y + (0.25f / 1.5f));
//				return o;
//			}
//
//			float4 ConvertYUV(v2f i)
//			{
//				fixed4 yColor = tex2D(_MainTex, i.yUV);
//				fixed4 uColor = tex2D(_MainTex, i.uUV);
//				fixed4 vColor = tex2D(_MainTex, i.vUV);
//
//				float r = yColor.a + 1.4022 * vColor.a - 0.7011;
//				float g = yColor.a - 0.3456 * uColor.a - 0.7145 * vColor.a + 0.53005;
//				float b = yColor.a + 1.771 * uColor.a - 0.8855;
//
//				fixed4 outColor = fixed4(r, g, b, 1);
//				if (i.yuvUV.x < 0 || i.yuvUV.x > 1 || i.yuvUV.y < 0 || i.yuvUV.y > 1)
//					outColor = fixed4(0, 0, 0, 0);
//
//				return outColor;
//			}
//
//			fixed4 frag(v2f i) : SV_Target
//			{
//				switch (_DisplayMode)
//				{
//					default:
//					case 0: return tex2D(_BaseTex, i.defaultUV); // default texture.
//					case 1: return tex2D(_WaitTex, i.waitingUV); // waiting texture.
//					case 2: return ConvertYUV(i);
//				}
//			}
//			ENDCG
//		}
//	}
//}