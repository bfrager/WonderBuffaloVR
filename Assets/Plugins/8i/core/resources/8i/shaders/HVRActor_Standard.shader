Shader "8i/HVRActor_Standard"
{
	Properties
	{
		_Color("Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_Emissive("Emissive", Color) = (0.0, 0.0, 0.0, 0.0)
		[HideInInspector] _HvrColorTex("Texture", 2D) = "white" {}
		[HideInInspector] _HvrDepthTex("Texture", 2D) = "white" {}
		[HideInInspector] _Mode("__mode", Float) = 0.0
		[HideInInspector] _SrcBlend("__src", Float) = 1.0
		[HideInInspector] _DstBlend("__dst", Float) = 0.0
		[HideInInspector] _ZWrite("__zw", Float) = 1.0
	}

		CGINCLUDE
#include "UnityCG.cginc"

	struct Pixel
	{
		float4 position : SV_POSITION;
		half2 texcoord : TEXCOORD0;
		float4 screenPos : TEXCOORD1;
		float4 wpos : TEXCOORD2;
		UNITY_FOG_COORDS(3)
	};

	struct Output
	{
		half4 color : COLOR;
		float depth : DEPTH;
	};

	fixed4 _Color;
	fixed4 _Emissive;
	sampler2D _HvrColorTex;
	sampler2D_float _HvrDepthTex;

	sampler2D _ScreenSpaceShadowTex;

	float4x4 _ViewProjectInverse;

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

	float4 DepthToWPOS(float depth, float2 uv)
	{
		// Returns World Position of a pixel from clip-space depth map..
		// H is the viewport position at this pixel in the range -1 to 1.

		// WPOS.rgb in world space, WPOS.z in clip space

		depth = depth * 2 - 1;

#if UNITY_UV_STARTS_AT_TOP
		uv.y = 1.0 - uv.y;
#endif

		float4 H = float4((uv.x) * 2 - 1, (uv.y) * 2 - 1, depth, 1.0);
		float4 D = mul(_ViewProjectInverse, H);
		D /= D.w;

		return D;
	}

	Pixel vert(appdata_img v)
	{
		Pixel o;
		o.position = v.vertex;
		o.texcoord = v.texcoord;
		o.screenPos = ComputeScreenPos(v.vertex);
		return o;
	}

	Output frag(Pixel pixel)
	{
		Output output;

		half4 color = tex2D(_HvrColorTex, pixel.screenPos.xy);
		color.a = 1; // Set this to 1 for now, as we don't want this information to be used

		float depth = tex2D(_HvrDepthTex, pixel.screenPos.xy);
		half4 wpos = DepthToWPOS(depth, pixel.screenPos);

		color = color * _Color;
		color.rgb += _Emissive.rgb;

#if defined(RECEIVE_SHADOWS)
		float shadow = tex2D(_ScreenSpaceShadowTex, pixel.screenPos);
		color.rgb *= shadow;
#endif

#if TRANSPARENCY_DITHER
		color.a = BinaryDither4x4(_Color.a - 0.5, pixel.screenPos);
#endif

		UNITY_TRANSFER_FOG(pixel, mul(UNITY_MATRIX_MVP, wpos));
		UNITY_APPLY_FOG(pixel.fogCoord, color);

#if !UNITY_COLORSPACE_GAMMA
		color.rgb = GammaToLinearSpace(color.rgb);
#endif

		// Set the final colors here
		output.color = color;
		output.depth = depth;

		return output;
	}

	ENDCG

	SubShader
	{
		Tags{ "RenderType" = "Opaque" }

		Pass
		{
			ZTest Less
			Cull Off

			Blend[_SrcBlend][_DstBlend]
			ZWrite[_ZWrite]

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile __ UNITY_COLORSPACE_GAMMA
			#pragma multi_compile __ TRANSPARENCY_DITHER
			#pragma multi_compile __ RECEIVE_SHADOWS
			#pragma multi_compile_fog
			ENDCG
		}
	}
	Fallback off

	CustomEditor "ShaderGUI_HvrActor_Standard"
}
