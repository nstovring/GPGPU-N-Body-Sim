// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/BufferShader" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Pass
		{
		ZTest Always Cull Off ZWrite Off
		Fog{Mode off}

		CGPROGRAM
		#include "UnityCG.cginc"
		#pragma target 5.0
		#pragma vertex vert
		#pragma fragment frag

		struct particle{
		float3 position;
		float3 direction;
		uint morton;
		};

		uniform StructuredBuffer<particle> computeBuffer;

		struct v2f{
			float4 pos: SV_POSITION;
			float3 normal : TEXCOORD;
			float4 direction : TEXCOORD1;
			float4 position : TEXCOORD2;
		};

		

		v2f vert(uint id : SV_VERTEXID){
			float4 pos = float4(computeBuffer[id].position,1);
			float4 dir = float4(computeBuffer[id].direction,1);
			float4 position = float4(computeBuffer[id].position,1);
			v2f OUT;
			OUT.direction = dir;
			OUT.position = position;
			OUT.pos = UnityObjectToClipPos(pos);
			return OUT;
		}

		float4 frag(v2f IN) : COLOR{
			//float NdotL = dot(IN.direction,_WorldSpaceLightPos0);
			return pow(IN.position,2) ;// +  float4(1,1,1,1) / pow(distance(IN.pos, float3(0.5,0.5,0.5)),0.01);
		}

		ENDCG
		}
	}
	FallBack "Diffuse"
}
