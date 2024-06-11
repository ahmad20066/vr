Shader "Custom/ParticleShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct Particle
            {
                float3 position;
                float3 velocity;
                float density;
                float pressure;
            };

            StructuredBuffer<Particle> particles;

            struct appdata
            {
                uint vertexID : SV_VertexID;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;

            float4 _MainTex_ST;
            float particleSize = 0.1; // Adjust the size of the particles here

            v2f vert (appdata v)
            {
                Particle p = particles[v.vertexID / 6]; // Each particle has 6 vertices (2 triangles)

                // Calculate vertex position
                float3 offsets[6] = {
                    float3(-1, -1, 0), float3(1, -1, 0), float3(1, 1, 0), // First triangle
                    float3(-1, -1, 0), float3(1, 1, 0), float3(-1, 1, 0)  // Second triangle
                };

                float3 offset = offsets[v.vertexID % 6] * particleSize;
                float3 worldPos = p.position + offset;

                v2f o;
                o.pos = UnityObjectToClipPos(float4(worldPos, 1.0));
                o.color = float3(1.0, 0.0, 0.0); // Red color for particles

                // Calculate UV coordinates
                float2 uv[6] = {
                    float2(0, 0), float2(1, 0), float2(1, 1),
                    float2(0, 0), float2(1, 1), float2(0, 1)
                };
                o.uv = uv[v.vertexID % 6];

                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 texColor = tex2D(_MainTex, i.uv);
                return half4(i.color, 1.0) * texColor;
            }
            ENDCG
        }
    }
}
