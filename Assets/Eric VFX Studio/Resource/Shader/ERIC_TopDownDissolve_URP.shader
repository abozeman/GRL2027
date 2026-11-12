Shader "ERIC VFX/URP/Dissolve/Top Down Dissolve"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1,1,1,1)

        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Range(0,2)) = 1

        _MetallicGlossMap ("Metallic Map", 2D) = "white" {}
        _Metallic ("Metallic", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5

        _OcclusionMap ("Occlusion Map", 2D) = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0,1)) = 1

        _EmissionMap ("Emission Map", 2D) = "white" {}
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,1)

        [Toggle] _AlphaClip ("Alpha Clipping", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5

        _DissolveAmount ("Dissolve Amount", Range(0,1)) = 0

        _WorldHeightMin ("World Bottom Y", Float) = -1
        _WorldHeightMax ("World Top Y", Float) = 1

        _EdgeWidth ("Edge Width", Range(0.001,0.2)) = 0.04
        [HDR] _EdgeColor ("Edge Color", Color) = (0,1,1,1)
        _EdgeIntensity ("Edge Intensity", Range(0,20)) = 4

        _NoiseScale ("Noise Scale", Range(1,80)) = 18
        _NoiseStrength ("Noise Strength", Range(0,0.3)) = 0.08
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="AlphaTest"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual
            Blend One Zero

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            TEXTURE2D(_MetallicGlossMap);
            SAMPLER(sampler_MetallicGlossMap);

            TEXTURE2D(_OcclusionMap);
            SAMPLER(sampler_OcclusionMap);

            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;

                float _BumpScale;
                float _Metallic;
                float _Smoothness;
                float _OcclusionStrength;
                float4 _EmissionColor;

                float _AlphaClip;
                float _Cutoff;

                float _DissolveAmount;
                float _WorldHeightMin;
                float _WorldHeightMax;

                float _EdgeWidth;
                float4 _EdgeColor;
                float _EdgeIntensity;

                float _NoiseScale;
                float _NoiseStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 uv)
            {
                float2 id = floor(uv);
                float2 f = frac(uv);

                float a = Hash21(id);
                float b = Hash21(id + float2(1, 0));
                float c = Hash21(id + float2(0, 1));
                float d = Hash21(id + float2(1, 1));

                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(
                    lerp(a, b, u.x),
                    lerp(c, d, u.x),
                    u.y
                );
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs posInputs =
                    GetVertexPositionInputs(IN.positionOS.xyz);

                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.normalWS = normalInputs.normalWS;

                float tangentSign =
                    IN.tangentOS.w * GetOddNegativeScale();

                OUT.tangentWS =
                    float4(normalInputs.tangentWS, tangentSign);

                OUT.fogFactor =
                    ComputeFogFactor(posInputs.positionCS.z);

                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float4 baseSample =
                    SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);

                float4 baseCol =
                    baseSample * _BaseColor;

                if (_AlphaClip > 0.5)
                    clip(baseCol.a - _Cutoff);

                float amount = saturate(_DissolveAmount);
                float edge = 0.0;

                if (amount >= 0.9999)
                {
                    clip(-1.0);
                }
                else if (amount > 0.0001)
                {
                    float worldRange =
                        max(0.0001, _WorldHeightMax - _WorldHeightMin);

                    float height01 =
                        saturate(
                            (IN.positionWS.y - _WorldHeightMin) /
                            worldRange
                        );

                    float dissolveValue =
                        1.0 - height01;

                    float noise =
                        ValueNoise(
                            IN.positionWS.xz * _NoiseScale
                        );

                    dissolveValue +=
                        (noise - 0.5) *
                        _NoiseStrength;

                    dissolveValue =
                        saturate(dissolveValue);

                    float signedDistance =
                        dissolveValue - amount;

                    clip(signedDistance - 0.0001);

                    edge =
                        1.0 -
                        smoothstep(
                            0.0,
                            _EdgeWidth,
                            abs(signedDistance)
                        );
                }

                float3 normalTS =
                    UnpackNormalScale(
                        SAMPLE_TEXTURE2D(
                            _BumpMap,
                            sampler_BumpMap,
                            IN.uv
                        ),
                        _BumpScale
                    );

                float3 normalWS = normalize(IN.normalWS);
                float3 tangentWS = normalize(IN.tangentWS.xyz);

                float3 bitangentWS =
                    IN.tangentWS.w *
                    cross(normalWS, tangentWS);

                float3x3 tangentToWorld =
                    float3x3(
                        tangentWS,
                        bitangentWS,
                        normalWS
                    );

                normalWS =
                    normalize(
                        TransformTangentToWorld(
                            normalTS,
                            tangentToWorld
                        )
                    );

                float4 metallicSample =
                    SAMPLE_TEXTURE2D(
                        _MetallicGlossMap,
                        sampler_MetallicGlossMap,
                        IN.uv
                    );

                float metallic =
                    saturate(
                        metallicSample.r *
                        _Metallic
                    );

                float smoothness =
                    saturate(
                        metallicSample.a *
                        _Smoothness
                    );

                float occlusionSample =
                    SAMPLE_TEXTURE2D(
                        _OcclusionMap,
                        sampler_OcclusionMap,
                        IN.uv
                    ).g;

                float occlusion =
                    lerp(
                        1.0,
                        occlusionSample,
                        _OcclusionStrength
                    );

                float3 emission =
                    SAMPLE_TEXTURE2D(
                        _EmissionMap,
                        sampler_EmissionMap,
                        IN.uv
                    ).rgb *
                    _EmissionColor.rgb;

                emission +=
                    _EdgeColor.rgb *
                    edge *
                    _EdgeIntensity;

                SurfaceData surfaceData =
                    (SurfaceData)0;

                surfaceData.albedo = baseCol.rgb;
                surfaceData.metallic = metallic;
                surfaceData.specular = float3(0,0,0);
                surfaceData.smoothness = smoothness;
                surfaceData.normalTS = normalTS;
                surfaceData.emission = emission;
                surfaceData.occlusion = occlusion;
                surfaceData.alpha = baseCol.a;
                surfaceData.clearCoatMask = 0;
                surfaceData.clearCoatSmoothness = 0;

                InputData inputData =
                    (InputData)0;

                inputData.positionWS = IN.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS =
                    SafeNormalize(
                        GetWorldSpaceViewDir(IN.positionWS)
                    );
                inputData.shadowCoord =
                    TransformWorldToShadowCoord(IN.positionWS);
                inputData.fogCoord = IN.fogFactor;
                inputData.vertexLighting = float3(0,0,0);
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV =
                    GetNormalizedScreenSpaceUV(IN.positionHCS);
                inputData.shadowMask = float4(1,1,1,1);

                float4 color =
                    UniversalFragmentPBR(
                        inputData,
                        surfaceData
                    );

                color.rgb =
                    MixFog(
                        color.rgb,
                        IN.fogFactor
                    );

                color.a = baseCol.a;

                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
