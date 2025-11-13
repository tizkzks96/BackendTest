Shader "il/Media/RGB"
{
	Properties
	{
		_MainTex("Texture", 2D) = "black" {}
		_ChromaTex("Chroma", 2D) = "gray" {}
		[Toggle(USE_YPCBCR)] _UseYpCbCr("Use YpCbCr", Float) = 0
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

			#pragma multi_compile __ USE_YPCBCR

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			sampler2D _MainTex;
#if USE_YPCBCR
			uniform sampler2D _ChromaTex;
			uniform float4x4 _YpCbCrTransform;
#endif


			half3 GammaToLinear_Accurate(half3 col)
			{
				if (col.r <= 0.04045)
					col.r = col.r / 12.92;
				else
					col.r = pow((col.r + 0.055) / 1.055, 2.4);

				if (col.g <= 0.04045)
					col.g = col.g / 12.92;
				else
					col.g = pow((col.g + 0.055) / 1.055, 2.4);

				if (col.b <= 0.04045)
					col.b = col.b / 12.92;
				else
					col.b = pow((col.b + 0.055) / 1.055, 2.4);

				// NOTE: We tried to optimise the above, but actually the compiler does a better job..
				/*HALF3 a = col / 12.92;
				HALF3 b = pow((col + 0.055) / 1.055, 2.4);
				HALF3 c = step(col,0.04045);
				col = LERP(b, a, c);*/

				return col;
			}

			float3 ConvertYpCbCrToRGB(float3 YpCbCr, float4x4 YpCbCrTransform)
			{
#if defined(SHADERLAB_GLSL)
				return clamp(float3x3(YpCbCrTransform) * (YpCbCr + YpCbCrTransform[3].xyz), 0.0, 1.0);
#else
				return saturate(mul((float3x3)YpCbCrTransform, YpCbCr + YpCbCrTransform[3].xyz));
#endif
			}

			// http://entropymine.com/imageworsener/srgbformula/
			half3 GammaToLinear(half3 col)
			{
#if defined(AVPRO_CHEAP_GAMMA_CONVERSION)
				return GammaToLinear_ApproxFit(col);
#else
				return GammaToLinear_Accurate(col);
#endif
			}

			half4 SampleYpCbCr(sampler2D luma, sampler2D chroma, float2 uv, float4x4 YpCbCrTransform)
			{
#if defined(SHADERLAB_GLSL)		// GLSL doesn't support tex2D, and Adreno GPU doesn't support passing sampler as a parameter, so just return if this is called
				return half4(1.0, 1.0, 0.0, 1.0);
#else
#if defined(SHADER_API_METAL) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
				float3 YpCbCr = float3(tex2D(luma, uv).r, tex2D(chroma, uv).rg);
#else
				float3 YpCbCr = float3(tex2D(luma, uv).r, tex2D(chroma, uv).ra);
#endif
				half4 rgba = half4(ConvertYpCbCrToRGB(YpCbCr, YpCbCrTransform), 1.0);
#if defined(APPLY_GAMMA)
				rgba.rgb = GammaToLinear(rgba.rgb);
#endif
				return rgba;
#endif
			}
			float4 _MainTex_ST;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 outColor;
#if USE_YPCBCR
				outColor = SampleYpCbCr(_MainTex, _ChromaTex, i.uv.xy, _YpCbCrTransform);
#else
				outColor = tex2D(_MainTex, fixed2(i.uv.x, i.uv.y));
#endif

				if (i.uv.x < 0 || i.uv.x > 1 || i.uv.y < 0 || i.uv.y > 1)
					outColor = fixed4(0, 0, 0, 0);

				return outColor;
			}

			ENDCG
		}
	}
}