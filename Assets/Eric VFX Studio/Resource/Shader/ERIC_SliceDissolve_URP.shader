Shader "ERIC VFX/URP/Dissolve/Slice Dissolve"
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

        _SliceDensity ("Slice Density", Range(1,40)) = 10
        _SliceOffset ("Slice Position Offset", Range(-2,2)) = 0
        _SliceGap ("Slice Gap", Range(0,0.30)) = 0.08
        _SliceOffsetStrength ("Slice Timing Offset", Range(0,0.45)) = 0.22

        [Enum(Horizontal X,0,Vertical Y,1,Depth Z,2)] _AxisMode ("Slice Axis", Float) = 1
        [Toggle] _InvertDirection ("Invert Axis Direction", Float) = 0

        [HDR] _EdgeColor ("Slice Edge Color", Color) = (0,4,1,1)
        _EdgeWidth ("Slice Edge Width", Range(0.001,0.12)) = 0.025
        _EdgeIntensity ("Slice Edge Intensity", Range(0,10)) = 4

        _NoiseScale ("Micro Noise Scale", Range(0.1,30)) = 10
        _NoiseStrength ("Micro Noise Strength", Range(0,0.05)) = 0.012
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="AlphaTest"
            "RenderPipeline"="UniversalPipeline"
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
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

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

                float _SliceDensity;
                float _SliceOffset;
                float _SliceGap;
                float _SliceOffsetStrength;

                float _AxisMode;
                float _InvertDirection;

                float4 _EdgeColor;
                float _EdgeWidth;
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
                float3 positionWS : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
                float4 tangentWS : TEXCOORD4;
                float fogFactor : TEXCOORD5;
            };

            float MicroNoise(float3 p)
            {
                float a = sin(dot(p, float3(1.73, 2.41, 3.17)));
                float b = sin(dot(p, float3(3.11, 1.27, 2.03)) + 1.7);
                float c = sin(dot(p, float3(2.19, 3.07, 1.41)) + 3.1);
                return (a + b + c) / 6.0;
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
                OUT.positionOS = IN.positionOS.xyz;
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
                float4 tex =
                    SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);

                float4 baseCol = tex * _BaseColor;

                if (_AlphaClip > 0.5)
                    clip(baseCol.a - _Cutoff);

                float edge = 0.0;

                if (_DissolveAmount >= 0.9999)
                {
                    clip(-1.0);
                }
                else if (_DissolveAmount > 0.0001)
                {
                    float axisValue = IN.positionOS.y;

                    if (_AxisMode < 0.5)
                        axisValue = IN.positionOS.x;
                    else if (_AxisMode > 1.5)
                        axisValue = IN.positionOS.z;

                    if (_InvertDirection > 0.5)
                        axisValue = -axisValue;

                    float sliceDensity = max(1.0, _SliceDensity);
                    float scaledAxis =
                        (axisValue + _SliceOffset) * sliceDensity;

                    float sliceIndex = floor(scaledAxis);
                    float sliceLocal = frac(scaledAxis);

                    float distToEdge =
                        min(sliceLocal, 1.0 - sliceLocal) * 2.0;

                    float sliceRand =
                        frac(
                            sin((sliceIndex + 1.0) * 12.9898) *
                            43758.5453
                        );

                    float signedRand =
                        sliceRand * 2.0 - 1.0;

                    float endpointSafeOffset =
                        signedRand *
                        _SliceOffsetStrength *
                        _DissolveAmount *
                        (1.0 - _DissolveAmount) *
                        4.0;

                    float localProgress =
                        saturate(
                            _DissolveAmount +
                            endpointSafeOffset
                        );

                    float gapProgress =
                        _SliceGap *
                        localProgress;

                    float noise =
                        MicroNoise(
                            IN.positionOS *
                            _NoiseScale
                        ) *
                        _NoiseStrength *
                        localProgress *
                        (1.0 - localProgress);

                    float cutThreshold =
                        saturate(
                            localProgress +
                            gapProgress +
                            noise
                        );

                    float signedDistance =
                        distToEdge -
                        cutThreshold;

                    clip(signedDistance);

                    edge =
                        1.0 -
                        smoothstep(
                            0.0,
                            _EdgeWidth,
                            signedDistance
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

                float3 normalWS =
                    normalize(IN.normalWS);

                float3 tangentWS =
                    normalize(IN.tangentWS.xyz);

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

                surfaceData.albedo =
                    baseCol.rgb;

                surfaceData.metallic =
                    metallic;

                surfaceData.specular =
                    float3(0,0,0);

                surfaceData.smoothness =
                    smoothness;

                surfaceData.normalTS =
                    normalTS;

                surfaceData.emission =
                    emission;

                surfaceData.occlusion =
                    occlusion;

                surfaceData.alpha =
                    baseCol.a;

                surfaceData.clearCoatMask =
                    0;

                surfaceData.clearCoatSmoothness =
                    0;

                InputData inputData =
                    (InputData)0;

                inputData.positionWS =
                    IN.positionWS;

                inputData.normalWS =
                    normalWS;

                inputData.viewDirectionWS =
                    SafeNormalize(
                        GetWorldSpaceViewDir(
                            IN.positionWS
                        )
                    );

                inputData.shadowCoord =
                    TransformWorldToShadowCoord(
                        IN.positionWS
                    );

                inputData.fogCoord =
                    IN.fogFactor;

                inputData.vertexLighting =
                    float3(0,0,0);

                inputData.bakedGI =
                    SampleSH(normalWS);

                inputData.normalizedScreenSpaceUV =
                    GetNormalizedScreenSpaceUV(
                        IN.positionHCS
                    );

                inputData.shadowMask =
                    float4(1,1,1,1);

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

                color.a =
                    baseCol.a;

                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
