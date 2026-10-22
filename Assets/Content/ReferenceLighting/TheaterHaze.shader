Shader "Mentality/Theater Reference Haze"
{
    Properties
    {
        _HazeColor ("Haze color", Color) = (0.46,0.48,0.62,0.3)
        _HazeShape ("Top halo / shaft", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            Cull Off ZWrite Off ZTest LEqual
            Blend SrcAlpha One
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _HazeColor;
                float _HazeShape;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.positionOS.xy + 0.5;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2.0 - 1.0;
                float falloff = saturate(1.0 - length(p));
                float halo = falloff * falloff;
                float width = lerp(0.85, 0.22, input.uv.y);
                float shaftFalloff = saturate(1.0 - abs(p.x) / max(width, 0.01));
                float shaft = shaftFalloff * shaftFalloff
                    * smoothstep(0, 0.28, input.uv.y) * (1 - smoothstep(0.82, 1, input.uv.y));
                return half4(_HazeColor.rgb, _HazeColor.a * lerp(halo, shaft, saturate(_HazeShape)));
            }
            ENDHLSL
        }
    }
}
