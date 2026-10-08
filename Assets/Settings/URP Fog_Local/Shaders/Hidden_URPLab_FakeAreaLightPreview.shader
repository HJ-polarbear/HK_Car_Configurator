Shader "Hidden/URPLab/FakeAreaLightPreview"
{
    Properties
    {
        _AreaColor("Area Color", Color) = (1, 0.85, 0.55, 1)
        _Intensity("Intensity", Float) = 2
        _Opacity("Opacity", Float) = 0.35
        _EdgeSoftness("Edge Softness", Float) = 2.5
        _CenterBoost("Center Boost", Float) = 1.5

        _NoiseToggle("Noise Toggle", Float) = 1
        _NoiseScale("Noise Scale", Float) = 4
        _NoiseSpeed("Noise Speed", Float) = 0.1
        _NoiseStrength("Noise Strength", Float) = 0.15
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
            Name "URP Fake Area Light Preview"

            ZWrite Off
            ZTest LEqual
            Cull Off
            Blend SrcAlpha One

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            float4 _AreaColor;
            float _Intensity;
            float _Opacity;
            float _EdgeSoftness;
            float _CenterBoost;

            float _NoiseToggle;
            float _NoiseScale;
            float _NoiseSpeed;
            float _NoiseStrength;

            Varyings Vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);

                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.uv = input.uv;

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

            half4 Frag(Varyings input) : SV_Target
            {
                float2 centeredUV = input.uv * 2.0 - 1.0;

                float rectangularDistance = max(abs(centeredUV.x), abs(centeredUV.y));
                float edgeFade = 1.0 - smoothstep(0.35, 1.0, rectangularDistance);
                edgeFade = pow(saturate(edgeFade), max(_EdgeSoftness, 0.0001));

                float centerDistance = length(centeredUV);
                float centerMask = 1.0 - saturate(centerDistance);
                centerMask = pow(centerMask, 1.25);

                float noiseMask = 1.0;

                if (_NoiseToggle > 0.5)
                {
                    float time = _Time.y * _NoiseSpeed;

                    float3 noisePosition = input.positionWS * _NoiseScale;
                    noisePosition += float3(time, time * 0.43, time * 0.71);

                    float noiseValue = FractalNoise(noisePosition);
                    noiseMask = lerp(1.0, lerp(0.8, 1.2, noiseValue), _NoiseStrength);
                }

                float alpha = _Opacity;
                alpha *= edgeFade;
                alpha *= lerp(1.0, _CenterBoost, centerMask);
                alpha *= noiseMask;
                alpha = saturate(alpha);

                float3 finalColor = _AreaColor.rgb * _Intensity;
                finalColor *= lerp(0.85, 1.25, centerMask);

                return half4(finalColor, alpha);
            }

            ENDHLSL
        }
    }
}