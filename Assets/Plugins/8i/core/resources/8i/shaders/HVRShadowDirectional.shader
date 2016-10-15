Shader "Hidden/8i/HVRShadowDirectional"
{
	SubShader
	{
		Tags { "RenderType"="Opaque" }

			Pass
			{
				Blend DstColor Zero
				ZTest LEqual
				Offset -1, 0

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile __ SHADOWS_SOFT 
				#pragma multi_compile __ DEBUG_CASCADE_ON

				#include "UnityCG.cginc"

				struct appdata
				{
					float4 vertex : POSITION;
					float3 normal : NORMAL;
				};

				struct v2f
				{
					float4 vertex : SV_POSITION;
					float4 wpos : TEXCOORD0;
					float3 normal : TEXCOORD1;
				};

				uniform sampler2D _texLSDepth;
				uniform float4x4 _matLSViewProject;
				uniform float4x4 _matCSViewProject;
				uniform float4x4 _matModel;
				uniform float4 _HVRLightShadowData;
				uniform float4 _HVRMapTexelSize;

				uniform float4x4 _matShadowSplitSphere;
				uniform float4 _vecShadowSplitSqRadii;
				uniform float4x4 _matWorld2Shadow0;
				uniform float4x4 _matWorld2Shadow1;
				uniform float4x4 _matWorld2Shadow2;
				uniform float4x4 _matWorld2Shadow3;

				v2f vert(appdata v)
				{
					v2f o;
					float4 WPOS = mul(_matModel, v.vertex);
					o.vertex = mul(UNITY_MATRIX_VP, WPOS);
					o.normal = mul(_matModel, float4(v.normal, 0)).xyz; // world space normal

					o.wpos = WPOS;

					return o;
				}

				inline fixed4 getCascadeWeights_splitSpheres(float3 wpos)
				{
					float3 fromCenter0 = wpos.xyz - _matShadowSplitSphere[0].xyz;
					float3 fromCenter1 = wpos.xyz - _matShadowSplitSphere[1].xyz;
					float3 fromCenter2 = wpos.xyz - _matShadowSplitSphere[2].xyz;
					float3 fromCenter3 = wpos.xyz - _matShadowSplitSphere[3].xyz;
					// distance from pixel to center, squared
					float4 distances2 = float4(dot(fromCenter0, fromCenter0), dot(fromCenter1, fromCenter1), dot(fromCenter2, fromCenter2), dot(fromCenter3, fromCenter3));
					fixed4 weights = float4(distances2 < _vecShadowSplitSqRadii);
					weights.yzw = saturate(weights.yzw - weights.xyz);
					return weights;
				}

				inline float4 getShadowCoord(float4 wpos, fixed4 cascadeWeights)
				{
					float3 sc0 = mul(_matWorld2Shadow0, wpos).xyz;
					float3 sc1 = mul(_matWorld2Shadow1, wpos).xyz;
					float3 sc2 = mul(_matWorld2Shadow2, wpos).xyz;
					float3 sc3 = mul(_matWorld2Shadow3, wpos).xyz;
					return float4(sc0 * cascadeWeights[0] + sc1 * cascadeWeights[1] + sc2 * cascadeWeights[2] + sc3 * cascadeWeights[3], 1);
				}

				float4 frag(v2f i) : SV_Target
				{
					float4 WPOS = i.wpos;

					float3 LSNORM = mul(_matLSViewProject, float4(i.normal, 0)).xyz;

					fixed4 weights = getCascadeWeights_splitSpheres(WPOS);
					float4 coord = getShadowCoord(WPOS, weights);

					// convert from [-1, 1] to [0, 1]
					coord.xy = (coord.xy + 1.0) * 0.5;
					coord.xy = saturate(coord.xy);

#if defined(SHADER_API_GLCORE) || defined(SHADER_API_OPENGL) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
					coord.z = (coord.z + 1.0) * 0.5;
#endif
					
					float atten = 1.0;
#if defined(SHADOWS_SOFT)

					float4 LSDep;
					float4 weight;
					float bias = 0.005;
					float2 MapSize = _HVRMapTexelSize.xy;
					float2 TexelSize = _HVRMapTexelSize.zw;

					float2 pixcoord = coord * MapSize.xy + float2(0.5, 0.5);
					float2 pixcoord_i = floor(pixcoord);
					pixcoord_i = pixcoord_i * TexelSize;

					LSDep.x = tex2D(_texLSDepth, pixcoord_i + float2(0, 0));
					LSDep.y = tex2D(_texLSDepth, pixcoord_i + float2(0, TexelSize.y));
					LSDep.z = tex2D(_texLSDepth, pixcoord_i + float2(TexelSize.x, 0));
					LSDep.w = tex2D(_texLSDepth, pixcoord_i + float2(TexelSize.x, TexelSize.y));

					float2 st = frac(pixcoord);

					weight.x = (1 - st.x) *(1 - st.y);
					weight.y = (1 - st.x) * st.y;
					weight.z = st.x * (1 - st.y);
					weight.w = st.x * st.y;

					float4 shadows = (LSDep + bias < coord.zzzz) ? _HVRLightShadowData.r : 1.0f;
					atten = dot(shadows, weight);

#else
					float LSDep = tex2D(_texLSDepth, coord.xy);
					float bias = 0.005;
					// compare LSDep(pixel depth, from light space shadowmap) and coord.z(pixel depth, from current HVR Actor)
					if (LSDep + bias < coord.z)
					{
						atten = _HVRLightShadowData.r;
					}
#endif

					// This is important: do not show "back projected" shadow on surface back facing the lights
					float backfacing = dot(LSNORM, float3(0, 0, 1));
					if (backfacing > 0)
					{
						atten = 1.0f;
					}

#if defined(DEBUG_CASCADE_ON)
					float r = 1 - coord.x;
					float g = 1 - coord.y;
					float b = 0.4;
					float4 col = float4(r * atten, g * atten, b * atten, 1);
#else
					float4 col = float4(atten, atten, atten, 1);
#endif
					return col;
				}
				ENDCG
			}
		}
}
