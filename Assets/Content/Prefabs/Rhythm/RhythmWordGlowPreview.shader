Shader "Mentality/Rhythm/WordTextGlowPreview"
{
    Properties
    {
        _MainTex ("Font Atlas", 2D) = "white" {}
        _Color ("Text Tint", Color) = (1, 1, 1, 1)
        _GlowColor ("Music Glow", Color) = (0.08, 1, 0.32, 1)
        _GlowStrength ("Glow Strength", Range(0, 2)) = 0.7
        _GlowWidth ("Glow Width", Range(0.5, 4)) = 2
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        Pass
        {
            Name "WordTextGlowPreview"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _GlowColor;
                half _GlowStrength;
                half _GlowWidth;
            CBUFFER_END
            float4 _MainTex_TexelSize;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half center = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                float2 d = _MainTex_TexelSize.xy * _GlowWidth;
                half around = 0;
                around = max(around, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(d.x, 0)).a);
                around = max(around, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - float2(d.x, 0)).a);
                around = max(around, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(0, d.y)).a);
                around = max(around, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - float2(0, d.y)).a);
                around = max(around, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + d).a);
                around = max(around, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - d).a);
                half halo = saturate(around - center) * _GlowStrength;
                half baseAlpha = center * input.color.a;
                half alpha = max(baseAlpha, halo * 0.72h * input.color.a);
                half3 rgb = input.color.rgb * center + _GlowColor.rgb * halo;
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
