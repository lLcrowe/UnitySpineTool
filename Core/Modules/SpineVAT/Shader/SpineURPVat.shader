Shader "SpineVAT/URPVat"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _VatPositionTex ("VAT Position Texture", 2D) = "black" {}
        _Color ("Tint Color", Color) = (1, 1, 1, 1)

        [Header(VAT Settings)]
        _TotalFrames ("Total Frames", Float) = 1
        _VertexCount ("Vertex Count", Float) = 1
        _FrameOffset ("Frame Offset (Clip Start Row)", Float) = 0
        _FrameCount ("Frame Count (Clip Frames)", Float) = 1

        [Header(Animation)]
        _AnimTime ("Normalized Anim Time (0~1)", Float) = 0

        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv        : TEXCOORD0;
                float2 uv2       : TEXCOORD1; // Baker가 기록한 버텍스 인덱스 (x = (vertexID+0.5)/vertexCount)
                float4 color     : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color       : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_VatPositionTex);
            SAMPLER(sampler_VatPositionTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                float _TotalFrames;
                float _VertexCount;
                float _FrameOffset;
                float _FrameCount;
                float _AnimTime;
            CBUFFER_END

            // GPU Instancing 지원: 인스턴스별로 다른 _AnimTime, _FrameOffset, _FrameCount
            #ifdef UNITY_INSTANCING_ENABLED
                UNITY_INSTANCING_BUFFER_START(Props)
                    UNITY_DEFINE_INSTANCED_PROP(float, _AnimTime)
                    UNITY_DEFINE_INSTANCED_PROP(float, _FrameOffset)
                    UNITY_DEFINE_INSTANCED_PROP(float, _FrameCount)
                UNITY_INSTANCING_BUFFER_END(Props)
            #endif

            Varyings vert(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                // 인스턴스별 파라미터 읽기
                #ifdef UNITY_INSTANCING_ENABLED
                    float animTime    = UNITY_ACCESS_INSTANCED_PROP(Props, _AnimTime);
                    float frameOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _FrameOffset);
                    float frameCount  = UNITY_ACCESS_INSTANCED_PROP(Props, _FrameCount);
                #else
                    float animTime    = _AnimTime;
                    float frameOffset = _FrameOffset;
                    float frameCount  = _FrameCount;
                #endif

                // 현재 프레임 계산 (보간을 위해 소수점 유지)
                float maxFrame = max(frameCount - 1.0, 1.0);
                float frameF = saturate(animTime) * maxFrame;
                float frame0 = floor(frameF);
                float frame1 = min(frame0 + 1.0, maxFrame);
                float lerpFactor = frameF - frame0;

                // 텍스처 UV 계산
                // X = 버텍스 인덱스 (uv2.x에 이미 정규화되어 있음)
                // Y = (frameOffset + frame) / totalFrames
                float texU = input.uv2.x;
                float invTotalFrames = 1.0 / max(_TotalFrames, 1.0);
                float texV0 = (frameOffset + frame0 + 0.5) * invTotalFrames;
                float texV1 = (frameOffset + frame1 + 0.5) * invTotalFrames;

                // 텍스처에서 위치 Fetch
                float4 pos0 = SAMPLE_TEXTURE2D_LOD(_VatPositionTex, sampler_VatPositionTex, float2(texU, texV0), 0);
                float4 pos1 = SAMPLE_TEXTURE2D_LOD(_VatPositionTex, sampler_VatPositionTex, float2(texU, texV1), 0);

                // 프레임 사이 보간
                float3 localPos = lerp(pos0.rgb, pos1.rgb, lerpFactor);

                output.positionCS = TransformObjectToHClip(localPos);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 finalColor = texColor * input.color;

                clip(finalColor.a - 0.01);

                return finalColor;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
