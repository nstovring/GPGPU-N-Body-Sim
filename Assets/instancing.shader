 Shader "Instanced/InstancedSurfaceShader" {
    Properties {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
		_MortonScale("MortonScale", float) = 1
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model
        #pragma surface surf Standard addshadow fullforwardshadows
        #pragma multi_compile_instancing
        #pragma instancing_options procedural:setup

		uniform float _MortonScale;
        sampler2D _MainTex;

        struct Input {
            float2 uv_MainTex;
        };
		struct particle{
			float3 pos;
			float3 dir;
			float3 color;
			float radius;
			uint morton;
			int collision;
		};
    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
	
        StructuredBuffer<particle> positionBuffer;
    #endif

        void rotate2D(inout float2 v, float r)
        {
            float s, c;
            sincos(r, s, c);
            v = float2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
		//particle myParticle;
				float3 direction;
				float3 position;
				float3 color;
				float radius;
				uint morton;
				int collision;
        void setup()
        {
        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			radius = positionBuffer[unity_InstanceID].radius;
            float4 data = float4(positionBuffer[unity_InstanceID].pos * 20, radius * 20);
			direction = positionBuffer[unity_InstanceID].dir;
			position = positionBuffer[unity_InstanceID].pos;
			morton =  positionBuffer[unity_InstanceID].morton;
			collision = positionBuffer[unity_InstanceID].collision;
			color = positionBuffer[unity_InstanceID].color;
            //float rotation = data.w * data.w * _Time.y * 0.5f;
            //rotate2D(data.xz, rotation);

            unity_ObjectToWorld._11_21_31_41 = float4(data.w, 0, 0, 0);
            unity_ObjectToWorld._12_22_32_42 = float4(0, data.w, 0, 0);
            unity_ObjectToWorld._13_23_33_43 = float4(0, 0, data.w, 0);
            unity_ObjectToWorld._14_24_34_44 = float4(data.xyz, 1);
            unity_WorldToObject = unity_ObjectToWorld;
            unity_WorldToObject._14_24_34 *= -1;
            unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;
        #endif
        }

        half _Glossiness;
        half _Metallic;

        void surf (Input IN, inout SurfaceOutputStandard o) {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
            o.Albedo = position;// float3(1,1,1) * color;// lerp(float3(1,1,1), float3(1,0,0),collision);//float3(1,1,1) * (morton/(_MortonScale * 10000000));
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}