Shader "Hidden/URPLab/VolumetricSpotCone"
{
    Properties
    {
        _ConeColor("Cone Color", Color) = (1, 0.95, 0.75, 1)
        _ConeIntensity("Cone Intensity", Float) = 1
        _ConeOpacity("Cone Opacity", Float) = 0.35

        _ConeLength("Cone Length", Float) = 10
        _ConeAngle("Cone Angle", Float) = 45

        _EdgeSoftness("Edge Softness", Float) = 1.5
        _LengthFade("Length Fade", Float) = 1.25
        _DepthFadeDistance("Depth Fade Distance", Float) = 1.0

        _NoiseToggle("Noise Toggle", Float) = 1
        _NoiseScale("Noise Scale", Float) = 3
        _NoiseSpeed("Noise Speed", Float) = 0.15
        _NoiseStrength("Noise Strength", Float) = 0.25
    }

        SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "URP Volumetric Spot Cone"

            ZWrite Off
            ZTest LEqual
            Cull Off
            Blend SrcAlpha One

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                float4 screenPos : TEXCOORD4;
            };

            float4 _ConeColor;
            float _ConeIntensity;
            float _ConeOpacity;

            float _ConeLength;
            float _ConeAngle;

            float _EdgeSoftness;
            float _LengthFade;
            float _DepthFadeDistance;

            float _NoiseToggle;
            float _NoiseScale;
            float _NoiseSpeed;
            float _NoiseStrength;

            Varyings Vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.positionOS = input.positionOS.xyz;
                output.normalWS = normalize(normalInputs.normalWS);
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionHCS);

                return output;
            }

            float Hash(float3 p)
            {
                return frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * 43758.5453);
            }

            float Noise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);

                f = f * f * (3.0 - 2.0 * f);

                float n000 = Hash(i + float3(0, 0, 0));
                float n100 = Hash(i + float3(1, 0, 0));
                float n010 = Hash(i + float3(0, 1, 0));
                float n110 = Hash(i + float3(1, 1, 0));

                float n001 = Hash(i + float3(0, 0, 1));
                float n101 = Hash(i + float3(1, 0, 1));
                float n011 = Hash(i + float3(0, 1, 1));
                float n111 = Hash(i + float3(1, 1, 1));

                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);

                float nxy0 = lerp(nx00, nx10, f.y);
                float nxy1 = lerp(nx01, nx11, f.y);

                return lerp(nxy0, nxy1, f.z);
            }

            float FractalNoise(float3 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;

                value += Noise(p * frequency) * amplitude;
                frequency *= 2.0;
                amplitude *= 0.5;

                value += Noise(p * frequency) * amplitude;
                frequency *= 2.0;
                amplitude *= 0.5;

                value += Noise(p * frequency) * amplitude;

                return saturate(value);
            }

            float CalculateDepthFade(float4 screenPos)
            {
                float2 screenUV = screenPos.xy / max(screenPos.w, 0.00001);

                float rawSceneDepth = SampleSceneDepth(screenUV);
                float sceneEyeDepth = LinearEyeDepth(rawSceneDepth, _ZBufferParams);
                float fragmentEyeDepth = screenPos.w;

                float depthDifference = sceneEyeDepth - fragmentEyeDepth;

                return saturate(depthDifference / max(_DepthFadeDistance, 0.0001));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float length01 = saturate(input.positionOS.z / max(_ConeLength, 0.0001));

                float apexFade = smoothstep(0.02, 0.18, length01);
                float rangeFade = pow(saturate(1.0 - length01), max(_LengthFade, 0.0001));

                float3 viewDirectionWS = normalize(_WorldSpaceCameraPos.xyz - input.positionWS);

                float viewDot = abs(dot(normalize(input.normalWS), viewDirectionWS));

                float surfaceFade = 1.0 - viewDot;
                surfaceFade = pow(saturate(surfaceFade), max(_EdgeSoftness, 0.0001));

                surfaceFade = lerp(0.45, 1.0, surfaceFade);

                float3 coneForwardWS = normalize(mul((float3x3)UNITY_MATRIX_M, float3(0.0, 0.0, 1.0)));
                float forwardScatter = saturate(dot(coneForwardWS, -viewDirectionWS));
                forwardScatter = pow(forwardScatter, 1.5);

                float noiseMask = 1.0;

                if (_NoiseToggle > 0.5)
                {
                    float time = _Time.y * _NoiseSpeed;

                    float3 noisePosition = input.positionWS * _NoiseScale;
                    noisePosition += float3(time, time * 0.37, time * 0.71);

                    float noiseValue = FractalNoise(noisePosition);
                    noiseMask = lerp(1.0, lerp(0.65, 1.15, noiseValue), _NoiseStrength);
                }

                float depthFade = CalculateDepthFade(input.screenPos);

                float alpha = _ConeOpacity;
                alpha *= apexFade;
                alpha *= rangeFade;
                alpha *= surfaceFade;
                alpha *= lerp(0.85, 1.25, forwardScatter);
                alpha *= noiseMask;
                alpha *= depthFade;

                alpha = saturate(alpha);

                float3 finalColor = _ConeColor.rgb * _ConeIntensity;
                finalColor *= lerp(0.9, 1.25, forwardScatter);

                return half4(finalColor, alpha);
            }

            ENDHLSL
        }
    }
}