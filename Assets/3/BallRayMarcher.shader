// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Hidden/BallRayMarcher"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct particle{
				float3 position;
				float3 direction;
				uint morton;
			};

			uniform StructuredBuffer<particle> computeBuffer;


			 //A simple input struct for our pixel shader step containing a position.
            struct ps_input {
                float4 pos : SV_POSITION;
            };
 
            //Our vertex function simply fetches a point from the buffer corresponding to the vertex index
            //which we transform with the view-projection matrix before passing to the pixel program.
            ps_input vert (uint id : SV_VertexID)
            {
                ps_input o;
                float3 worldPos = computeBuffer[id].position;
                o.pos = mul (UNITY_MATRIX_VP, float4(worldPos,1.0f));
                return o;
            }
 
            //Pixel function returns a solid color for each point.
            float4 frag (ps_input i) : COLOR
            {
                return float4(1,0.5f,0.0f,1);
            }
 
            ENDCG
		}
	}
}
