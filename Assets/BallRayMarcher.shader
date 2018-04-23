// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Hidden/BallRayMarcher"
{
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader
	{

		Tags
	{
		"Queue" = "Geometry" "IgnoreProjector" = "True"
		"RenderType" = "Opaque"
		"DisableBatching" = "True"
		"LightMode" = "ForwardBase"
	}


		LOD 100
		//Blend SrcAlpha OneMinusSrcAlpha

		Cull off
		ZWrite off
		ZTest LEqual

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag

			#include "UnityCG.cginc"
			#pragma multi_compile_fog
			#pragma multi_compile_fwdadd_fullshadows

						//#pragma multi_compile_fwdbase
			#include "AutoLight.cginc"
			#include "UnityCG.cginc"

	struct particle {
		float3 position;
		float3 velocity;
		float mass;
		int isFixed;
		int iD;
		int springs[16];
	};

	int count = 64;

	struct clothVertex {
		float3 vertex;
		int index;
	};
	sampler2D _MainTex;

			uniform StructuredBuffer<particle> vertexBuffer;


			 //A simple input struct for our pixel shader step containing a position.
            struct ps_input {
                float4 pos : SV_POSITION;
				float4 neighbor : TEXCOORD1;
            };
 

			struct vs_out {
				float4 pos : SV_POSITION;
				float4 nieghbourPos : TEXCOORD0;
				float4 nieghbourPos2 : TEXCOORD1;
				float4 nieghbourPos3 : TEXCOORD2;

			};

			float3 getNeighbourPos(int id, int offset, int count) {
				int rows = sqrt(count);

				if (id + offset >= count) {
					return vertexBuffer[(id)].position;
				}

				if (id % rows == rows-1) {
					return vertexBuffer[(id)].position;
				}

				return vertexBuffer[(id + offset)].position;
			}

			vs_out vert(uint id : SV_VertexID)
			{
				vs_out o;

				int rowCount = sqrt(count);

				o.pos = (float4(vertexBuffer[id].position, 1));

				o.nieghbourPos = (float4(getNeighbourPos(id, rowCount, count), 1));
				o.nieghbourPos2 = (float4(getNeighbourPos(id, 1, count), 1));
				o.nieghbourPos3 = (float4(getNeighbourPos(id, rowCount+1, count), 1));

				return o;
			}

			struct gs_out {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 normal: TEXCOORD1;

			};

			float _Size;

			float3 CalculateNormal(float3 x, float3 y, float3 z) {
				float3 left = x - y;
				float3 forward = x -z;
				return normalize(cross(left, forward));
			}

			[maxvertexcount(6)]
			void geom(point vs_out input[1], inout TriangleStream<gs_out> outStream)
			{
				float dx = 0.1;
				float dy = 0.1 * _ScreenParams.x / _ScreenParams.y;
				gs_out output;
				float3 normal = CalculateNormal(input[0].pos, input[0].nieghbourPos, input[0].nieghbourPos2);

				output.pos = UnityObjectToClipPos(input[0].pos); output.uv = float2(0, 0); output.normal = normal; outStream.Append(output);
				output.pos = UnityObjectToClipPos(input[0].nieghbourPos); output.uv = float2(1, 0); output.normal = normal; outStream.Append(output);
				output.pos = UnityObjectToClipPos(input[0].nieghbourPos2); output.uv = float2(0, 0); output.normal = normal; outStream.Append(output);

				normal = -CalculateNormal(input[0].nieghbourPos, input[0].nieghbourPos2, input[0].nieghbourPos3);
				output.pos = UnityObjectToClipPos(input[0].nieghbourPos2); output.uv = float2(0, 0); output.normal = normal; outStream.Append(output);
				output.pos = UnityObjectToClipPos(input[0].nieghbourPos); output.uv = float2(1, 0); output.normal = normal; outStream.Append(output);
				output.pos = UnityObjectToClipPos(input[0].nieghbourPos3); output.uv = float2(1, 1); output.normal = normal; outStream.Append(output);
				//output.pos = input[0].pos + float4(dx, -dy, 0, 0); output.uv = float2(1, 1); outStream.Append(output);
				outStream.RestartStrip();
			}

 
            //Pixel function returns a solid color for each point.
            float4 frag (gs_out i) : COLOR
            {
				fixed4 col = tex2D(_MainTex, i.uv);

				float3 lightDir = _WorldSpaceLightPos0;
                return float4(1,0.5f,0.0f,1) * dot(i.normal, lightDir) + col;
            }
 
            ENDCG
		}
	}
}
