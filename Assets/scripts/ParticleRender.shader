Shader "Custom/ParticleRender"
{
    Properties
    {
        _PointSize ("Point Size", Float) = 10.0
        _Color ("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // Uniforms
            uniform float4x4 uViewProjection;
            uniform float _PointSize;
            uniform float4 _Color;

            // Buffer declaration
            StructuredBuffer<float3> particles;

            // Vertex input structure
            struct appdata
            {
                uint vertexID : SV_VertexID;
            };

            // Vertex to Fragment structure
            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float pointSize : PSIZE;
            };

            // Vertex shader
            v2f vert (appdata v)
            {
                v2f o;

                // Get particle position from buffer
                float3 particlePosition = particles[v.vertexID];

                // Transform position to clip space
                o.pos = mul(uViewProjection, float4(particlePosition, 1.0));
                
                // Set point size
                o.pointSize = _PointSize;

                // Pass color to fragment shader
                o.color = _Color;

                return o;
            }

            // Fragment shader
            float4 frag (v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
