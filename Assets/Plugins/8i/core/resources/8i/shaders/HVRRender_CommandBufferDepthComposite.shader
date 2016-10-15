Shader "Hidden/8i/HVRRender_CommandBufferDepthComposite"
{
	Properties
	{
		_MainColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
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

	sampler2D _oDEP;

	Pixel vert(appdata_img v)
	{
		Pixel o;
		o.position = v.vertex;
		o.texcoord = v.texcoord;
		o.screenPos = ComputeScreenPos(o.position);
		return o;
	}

	Output frag_gbuffer_cameradepth(Pixel pixel)
	{
		Output output;
		
		float dep = tex2D(_oDEP, pixel.screenPos.xy);
		output.color = half4(dep, dep, dep, 1);
		output.depth = dep;
		return output;
	}
	ENDCG

	SubShader
	{
		Pass
		{
			ZTest LEqual
			ZWrite On
			Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_gbuffer_cameradepth
			ENDCG
		}
	}
	Fallback off
}
