Shader "il/Media/YUV"
{
	Properties
	{
		_MainTex("YUV", 2D) = "white" {}
		_MainTex2("YUV", 2D) = "white" {}
		_MainTex3("YUV", 2D) = "white" {}
		_Angle("Angle", Float) = 0
	}
	SubShader
	{
		Tags { "Queue" = "Transparent" }
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
				float2 yuvUV : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 yuvUV : TEXCOORD0;
			};

			sampler2D _MainTex;
			sampler2D _MainTex2;
			sampler2D _MainTex3;

			float4 _MainTex_ST;
			float4 _MainTex2_ST;
			float4 _MainTex3_ST;

			float _Angle;

			// UV 시계방향 회전.
			float2 rotateUvs(float2 texCoords, float theta)
			{
				float2 center = float2(0.5f, 0.5f);

				float2 sc;
				sincos((-theta / 180.0f * 3.141592f), sc.x, sc.y);

				float2 uv = texCoords - center;

				float2 rotatedUv;
				rotatedUv.x = dot(uv, float2(sc.y, -sc.x));
				rotatedUv.y = dot(uv, sc.xy);

				rotatedUv += center;
				return rotatedUv;
			}

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.yuvUV = TRANSFORM_TEX(v.yuvUV, _MainTex);
				return o;
			}

			float4 ConvertYUV(v2f i)
			{
				fixed2 uv = rotateUvs(fixed2(i.yuvUV.x, 1 - i.yuvUV.y), _Angle);
				fixed4 yColor = tex2D(_MainTex, uv);
				fixed4 uColor = tex2D(_MainTex2, uv);
				fixed4 vColor = tex2D(_MainTex3, uv);

				float r = yColor.a + 1.4022 * vColor.a - 0.7011;
				float g = yColor.a - 0.3456 * uColor.a - 0.7145 * vColor.a + 0.53005;
				float b = yColor.a + 1.771 * uColor.a - 0.8855;

				fixed4 outColor = fixed4(r, g, b, 1);
				if (i.yuvUV.x < 0 || i.yuvUV.x > 1 || i.yuvUV.y < 0 || i.yuvUV.y > 1)
					outColor = fixed4(0, 0, 0, 0);

				return outColor;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				return ConvertYUV(i);
			}
			ENDCG
		}
	}
}