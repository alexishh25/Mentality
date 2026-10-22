Shader "Mentality/Theater Watching Eye"
{
    Properties
    {
        [PerRendererData] _MainTex ("Eye sheet", 2D) = "white" {}
        [PerRendererData] _EyeUvRect ("Sprite rectangle", Vector) = (0,0,1,1)
        _Tint ("Tint", Color) = (0.8,0.74,0.7,1)
        _ReferenceLook ("Cool reference lighting", Range(0,1)) = 0
        _AtmosphereFog ("Receive atmosphere fog", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="False" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _EyeUvRect;
                half4 _Tint;
                half _ReferenceLook;
                half _AtmosphereFog;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; half fog : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv; output.color = input.color;
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 source = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float2 cell = (input.uv - _EyeUvRect.xy) / _EyeUvRect.zw;
                float radius = length((cell - 0.5) / float2(0.415, 0.37));
                half brightness = max(source.r, max(source.g, source.b));
                // Key only the outside band. The black pupil and dark eyelids stay opaque.
                half edge = lerp(1.0h, smoothstep(0.015h, 0.065h, brightness), smoothstep(0.70, 0.86, radius));
                half alpha = edge * (1.0h - smoothstep(0.98, 1.025, radius));
                half3 color = source.rgb * input.color.rgb * _Tint.rgb;
                // Paint a cool overhead response on the sclera; retain the iris and pupil.
                half sclera = saturate((min(source.r, min(source.g, source.b)) - 0.10h) * 4.0h);
                half upper = smoothstep(0.30, 0.9, cell.y);
                half3 cool = lerp(half3(0.32h,0.35h,0.52h), half3(0.64h,0.78h,1.15h), upper);
                color = lerp(color, color * cool, _ReferenceLook * sclera);
                half lowerRim = smoothstep(0.72,0.96,radius) * (1 - smoothstep(0.18,0.55,cell.y));
                color += _ReferenceLook * lowerRim * half3(0.10h,0.012h,0.023h);
                color = lerp(color, MixFog(color, input.fog), _AtmosphereFog);
                return half4(color, alpha * input.color.a * _Tint.a);
            }
            ENDHLSL
        }
    }
}
