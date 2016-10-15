Shader "Hidden/8i/HVRShadowRenderer"
{
	Properties
	{
		_oDEP("Offscreen Depth", 2D) = "" {}
	}
	SubShader
	{
		Name "HVRShadowRenderer"

		Pass
		{
			Name "HVR Shadow Encoding in 2D map"

			Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			
			#include "UnityCG.cginc"

			uniform sampler2D _oDEP;

			struct ia_out
			{
				float4 vertex : POSITION;
			};

			struct vs_out
			{
				float4 vertex	: SV_POSITION;
				float4 spos		: TEXCOORD0;
			};

			struct fs_out
			{
				float4 color : SV_Target;
				float depth : SV_Depth;
			};

			uniform float4x4 _Projection;
			uniform float4x4 _ProjectionInverse;
			uniform float _ErrorBias; // Introducing this variable is because what I believe a bug in generating the orthogonal projection(directional light only), compensate with a constant bias as a workaround

			float4 DepthToVPOSWithBias(float depth, float2 uv)
			{
				// Returns World Position of a pixel from clip-space depth map..

				// H is the viewport position at this pixel in the range -1 to 1.
				// However, since we use the depth from this function to reconstruct a clip space position, we don't fuss the depth here
				// Otherwise it will affect the UnityApplyLinearShadowBias()'s output
				//depth = depth * 2 - 1;
#if UNITY_UV_STARTS_AT_TOP
				uv.y = 1.0 - uv.y;
#endif
				float4 H = float4((uv.x) * 2 - 1, (uv.y) * 2 - 1, depth, 1.0);
				float4 D = mul(_ProjectionInverse, H);
				D /= D.w;

				return D;
			}

			vs_out vert(ia_out v)
			{
				vs_out o;
				// just copy the vertex output since it's [-1,1] and in homo space
				o.vertex = v.vertex;
				o.spos = ComputeScreenPos(v.vertex);
				return o;
			}

			fs_out frag(vs_out v)
			{
#if UNITY_UV_STARTS_AT_TOP
				v.spos.y = 1.0 - v.spos.y;
#endif
				float dep = tex2D(_oDEP, v.spos.xy);

				/* 
				 * Without normal, the normal bias doesn't help too much 
				float4 WPOS = DepthToWPOS(dep, v.spos.xy);
				float3 WLIGHT = normalize(UnityWorldSpaceLightDir(WPOS));
				// To take a look at diffuse n dot L lighting, uncomment the following four lines
				//Calculate Normalse
				float3 WNORMAL = normalize(cross(ddy(WPOS.xyz), ddx(WPOS.xyz)));
				//Move the normals into the correct range
				WNORMAL = WNORMAL * 0.5 + 0.5;

				float shadowCos = dot(WNORMAL, WLIGHT);
				float shadowSine = sqrt(1 - shadowCos * shadowCos);
				float normalBias = unity_LightShadowBias.z * shadowSine;

				WPOS -= float4(WNORMAL, 0) * normalBias;
				float4 CPOS = mul(_ViewProject, WPOS);
				*/
				
				// Can we simplify the calculation here?
				float4 VPOS = DepthToVPOSWithBias(dep, v.spos.xy);
				float4 CPOS = mul(_Projection, VPOS);

				CPOS = UnityApplyLinearShadowBias(CPOS);
				
				CPOS /= CPOS.w;
				dep = CPOS.z - _ErrorBias;

				fs_out output;
				output.depth = dep;
				output.color = float4(dep, dep, dep, 1);
				return output;
			}

			ENDCG
		}

		Pass
		{
			Name "Geometry Shadow Encoding in 2D map"
			ZTest LEqual
			ZWrite On
			// for circumstance that need to output both color and depth as target, input vertex is in world space
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			uniform float4x4 _matModelViewProject;

			struct ia_out
			{
				float4 vertex : POSITION;
			};

			struct vs_out
			{
				float4 vertex	: SV_POSITION;
				float4 spos		: TEXCOORD0;
				float4 pos		: TEXCOORD1;
			};

			struct fs_out
			{
				float4 color : SV_Target;
				float depth : SV_Depth;
			};

			vs_out vert(ia_out v)
			{
				vs_out o;
				o.vertex = mul(_matModelViewProject, v.vertex);
				o.spos = ComputeScreenPos(v.vertex);
				o.pos = o.vertex; // To get rid of this: Shader model ps_4_0_level_9_1 doesn't allow reading from position semantics.
				return o;
			}

			fs_out frag(vs_out v)
			{
#if UNITY_UV_STARTS_AT_TOP
				v.spos.y = 1.0 - v.spos.y;
#endif
				float dep = v.pos.z / v.pos.w;
				dep += unity_LightShadowBias.x / v.pos.w; // add constant bias

				fs_out output;
				output.depth = dep;
				output.color = float4(dep, dep, dep, 1);

				return output;
			}

			ENDCG
		}

		Pass
		{
			Name "HVR Shadow Encoding in Cubemap"
			Cull Off
			//ZTest LEqual
			//ZWrite On
			Blend SrcAlpha OneMinusSrcAlpha
			
			CGPROGRAM
			#pragma vertex vertShadowCaster_HVR
			#pragma fragment fragShadowCaster_HVR

			#include "UnityCG.cginc"
			#include "UnityStandardShadow.cginc"

			uniform float4x4 _ViewProjectInverse;
			uniform sampler2D _oDEP; // camera space 
			uniform float _yBias;
			
			struct VertexOutputShadowCaster
			{
				float4 spos : TEXCOORD0;
			};

			struct fs_out
			{
				float4 color : SV_Target;
				float depth : SV_Depth;
			};

			float4 DepthToWPOS(float depth, float2 uv)
			{
				// Returns World Position of a pixel from clip-space depth map..
				//float depth = tex2D(_oDEP, uv);
				// H is the viewport position at this pixel in the range -1 to 1.
				depth = depth * 2 - 1;
#if UNITY_UV_STARTS_AT_TOP
				uv.y = 1.0 - uv.y;
#endif
				float4 H = float4((uv.x) * 2 - 1, (uv.y) * 2 - 1, depth, 1.0);
				float4 D = mul(_ViewProjectInverse, H);
				D /= D.w;
				
				return D;
			}

			void vertShadowCaster_HVR(VertexInput v,
				out VertexOutputShadowCaster o,
				out float4 opos : SV_POSITION)
			{
				opos = v.vertex;
				opos.w = 1;
				opos.z = 0;
				o.spos = ComputeScreenPos(v.vertex);
			}

			fs_out fragShadowCaster_HVR(VertexOutputShadowCaster i)
			{
				fs_out o;
				float alpha = 1;
#if !UNITY_UV_STARTS_AT_TOP
				i.spos.y = 1.0 - i.spos.y;
#endif
				float dep = tex2D(_oDEP, i.spos.xy);
				// Workaround: no depth provided by Unity's cubemap shadowmap, so we need to eliminate depth==1 pixels, using alpha blending or clip()
				if (dep > 0.999999)
				{
					alpha = 0;
				}
				//clip(dep * -1 + 0.999999); 
				
				// need to get linear depth/range from dep
				half4 WPOS = DepthToWPOS(dep, i.spos.xy);
				WPOS.y += _yBias;
				float3 vec = WPOS.xyz - _LightPositionRange.xyz;

				float4 linear_depth = UnityEncodeCubeShadowDepth((length(vec) + unity_LightShadowBias.x) * _LightPositionRange.w);
				o.color = linear_depth;
				o.color.a = alpha;
				o.depth = linear_depth.r;
				return o;
			}

			
			ENDCG

		}

		Pass
		{
			Name "Geometry Shadow Encoding in Cubemap"
			ZTest LEqual
			ZWrite On
			Cull Front // looks like culling front faces gives more reasonable results for point lights(e.g. lights behind the wall, under the ground)

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag


			#include "UnityCG.cginc"
			#include "UnityStandardShadow.cginc"

			uniform float4x4 _matModel;
			uniform float4x4 _matModelViewProj;

			struct ia_out
			{
				float4 vertex : POSITION;
			};

			struct vs_out
			{
				float4 opos : SV_POSITION;
				float3 vec  : TEXCOORD0;
			};

			struct fs_out
			{
				float4 color : SV_Target;
				float  depth : SV_Depth;
			};

			vs_out vert(ia_out v)
			{
				vs_out o;
				// vertex pos - light pos
				o.vec = mul(_matModel, v.vertex).xyz - _LightPositionRange.xyz;
				// output the homo space pos
				o.opos = mul(_matModelViewProj, v.vertex);
				return o;
			} 


			fs_out frag(vs_out i)
			{
				// param0: point light to object vec
				// param1: point light range
				fs_out o;
				float depth = UnityEncodeCubeShadowDepth((length(i.vec) + unity_LightShadowBias.x) * _LightPositionRange.w);
				o.color = float4(depth, depth, depth, 1);
				o.depth = depth;
				return o;
			}


			
			ENDCG

		}
	}
}
