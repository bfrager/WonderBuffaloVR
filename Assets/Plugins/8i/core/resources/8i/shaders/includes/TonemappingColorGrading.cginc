#include "UnityCG.cginc"

sampler2D _MainTex;
half4 _MainTex_TexelSize;

sampler2D _InternalLutTex;
half3 _InternalLutParams;


half3 apply_lut(sampler2D tex, half3 uvw, half3 scaleOffset)
{
	// Strip format where `height = sqrt(width)`
	uvw.z *= scaleOffset.z;
	half shift = floor(uvw.z);
	uvw.xy = uvw.xy * scaleOffset.z * scaleOffset.xy + scaleOffset.xy * 0.5;
	uvw.x += shift * scaleOffset.y;
	uvw.xyz = lerp(tex2D(tex, uvw.xy).rgb, tex2D(tex, uvw.xy + half2(scaleOffset.y, 0)).rgb, uvw.z - shift);
	return uvw;
}

half4 frag_tcg(v2f_img i) : SV_Target
{
	half4 color = tex2D(_MainTex, i.uv);

	// Always convert to linear space when doing color grading
	color.rgb = GammaToLinearSpace(color.rgb);
	
#if ENABLE_COLOR_GRADING
	// LUT color grading
	color.rgb = apply_lut(_InternalLutTex, saturate(color.rgb), _InternalLutParams);
#endif

#if ENABLE_DITHERING
	// Interleaved Gradient Noise from http://www.iryoku.com/next-generation-post-processing-in-call-of-duty-advanced-warfare (slide 122)
	half3 magic = float3(0.06711056, 0.00583715, 52.9829189);
	half gradient = frac(magic.z * frac(dot(i.uv / _MainTex_TexelSize, magic.xy))) / 255.0;
	color.rgb -= gradient.xxx;
#endif

	// Always convert back to Gamma after doing color grading.
	// TODO: This needs to change once HvrActor Standard Rendering moves to using Graphics.DrawMesh instead of Graphics.DrawMeshNow
	color.rgb = LinearToGammaSpace(color.rgb);

	return color;
}
