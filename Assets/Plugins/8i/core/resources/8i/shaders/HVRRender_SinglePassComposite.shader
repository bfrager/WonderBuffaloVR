Shader "Hidden/8i/HVRRender_SinglePassComposite"
{
	Properties
	{
		_MainColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
		[HideInInspector]
		_HvrColorTex("Texture", 2D) = "white" {}
		[HideInInspector]
		_HvrDepthTex("Texture", 2D) = "white" {}
	}

	CGINCLUDE
	#include "UnityCG.cginc"

	struct Pixel
	{
		float4 position : SV_POSITION;
		half2 texcoord : TEXCOORD0;
		float4 screenPos : TEXCOORD1;
	};

	struct Output
	{
		half4 color : COLOR;
		float depth : DEPTH;
	};

	fixed4 _MainColor;
	sampler2D _HvrColorTex;
	sampler2D_float _HvrDepthTex;

	float BinaryDither4x4(float value, float2 sceneUVs)
	{
		float4x4 mtx = float4x4(
			float4(1, 9, 3, 11) / 17.0,
			float4(13, 5, 15, 7) / 17.0,
			float4(4, 12, 2, 10) / 17.0,
			float4(16, 8, 14, 6) / 17.0
			);
		float2 px = floor(_ScreenParams.xy * sceneUVs);
		int xSmp = fmod(px.x, 4);
		int ySmp = fmod(px.y, 4);
		float4 xVec = 1 - saturate(abs(float4(0, 1, 2, 3) - xSmp));
		float4 yVec = 1 - saturate(abs(float4(0, 1, 2, 3) - ySmp));
		float4 pxMult = float4(dot(mtx[0], yVec), dot(mtx[1], yVec), dot(mtx[2], yVec), dot(mtx[3], yVec));
		return round(value + dot(pxMult, xVec));
	}

	Pixel vert(appdata_img v)
	{
		Pixel o;
		o.position = mul(UNITY_MATRIX_MVP, v.vertex);
		o.texcoord = v.texcoord;
		o.screenPos = ComputeScreenPos(o.position);
		return o;
	}

	Output frag(Pixel pixel)
	{
		Output output;
		half4 color = tex2D(_HvrColorTex, pixel.screenPos);
		color.a = 1; // Set this to 1 for now, as we don't want this information to be used

		color = color * _MainColor;
#if TRANSPARENCY_DITHER
		color.a = BinaryDither4x4(_MainColor.a - 0.5, pixel.screenPos);
#endif

#if !UNITY_COLORSPACE_GAMMA
		color.rgb = GammaToLinearSpace(color.rgb);
#endif

		// Set the final colors here
		output.color = color;
		output.depth = tex2D(_HvrDepthTex, pixel.screenPos);

		return output;
	}
	ENDCG

		SubShader
	{
		Pass
		{
			ZTest Less
			ZWrite On
			Cull Off

			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile __ UNITY_COLORSPACE_GAMMA
			#pragma multi_compile __ TRANSPARENCY_DITHER
			ENDCG
		}
	}
	Fallback off
}
