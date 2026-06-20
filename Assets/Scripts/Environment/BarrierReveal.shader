Shader "Custom/BarrierReveal"
{
    Properties
    {
        [MainTexture] _BaseMap ("Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Tint", Color) = (1,1,1,1)
        _PlayerPos ("Reveal Center (World)", Vector) = (0,0,0,0)
        _RevealRadius ("Reveal Radius", Float) = 1.5
        _RevealSoftness ("Reveal Softness", Float) = 0.6
        _GlobalAlpha ("Global Alpha", Range(0,1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "BarrierRevealForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _PlayerPos;
                float _RevealRadius;
                float _RevealSoftness;
                float _GlobalAlpha;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs positions = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = positions.positionCS;
                OUT.positionWS = positions.positionWS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                // Circular reveal around the player position projected onto the barrier plane.
                float dist = distance(IN.positionWS, _PlayerPos.xyz);
                float softness = max(_RevealSoftness, 1e-4);
                float reveal = 1.0 - smoothstep(_RevealRadius, _RevealRadius + softness, dist);

                col.a *= reveal * _GlobalAlpha;
                return col;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
