Shader "ERIC VFX/URP/Dissolve/Hologram Dissolve"
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

        _HologramStart ("Hologram Start", Range(0,0.8)) = 0.10
        _HologramFull ("Hologram Full", Range(0.1,0.95)) = 0.45

        _ScanLineCount ("Scan Line Count", Range(4,120)) = 42
        _ScanLineWidth ("Scan Line Width", Range(0.02,0.9)) = 0.18
        _ScanLinePhase ("Scan Line Phase", Range(-6.283,6.283)) = 0

        _BlockCountX ("Block Count X", Range(2,40)) = 10
        _BlockCountY ("Block Count Y", Range(2,40)) = 18
        _BlockDropout ("Max Block Dropout", Range(0,1)) = 0.45

        _Transparency ("Final Hologram Transparency", Range(0.05,1)) = 0.55
        _FlickerStrength ("Flicker Strength", Range(0,1)) = 0.25

        [HDR] _HoloColor ("Hologram Color", Color) = (0.1,2.5,6,1)
        [HDR] _EdgeColor ("Edge Color", Color) = (0.3,4,8,1)

        _EdgeWidth ("Edge Width", Range(0.001,0.15)) = 0.025
        _EdgeIntensity ("Edge Intensity", Range(0,12)) = 4

        _NoiseScale ("Dissolve Noise Scale", Range(0.5,30)) = 6
        _NoiseStrength ("Dissolve Noise Strength", Range(0,0.25)) = 0.05
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
                float _HologramStart;
                float _HologramFull;

                float _ScanLineCount;
                float _ScanLineWidth;
                float _ScanLinePhase;

                float _BlockCountX;
                float _BlockCountY;
                float _BlockDropout;

                float _Transparency;
                float _FlickerStrength;

                float4 _HoloColor;
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
                float2 uv : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
                float4 tangentWS : TEXCOORD4;
                float fogFactor : TEXCOORD5;
            };

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
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
                OUT.normalWS = normalInputs.normalWS;

                float tangentSign =
                    IN.tangentOS.w * GetOddNegativeScale();

                OUT.tangentWS =
                    float4(normalInputs.tangentWS, tangentSign);

                OUT.fogFactor =
                    ComputeFogFactor(posInputs.positionCS.z);

                return OUT;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float ValueNoise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = Hash31(i + float3(0,0,0));
                float n100 = Hash31(i + float3(1,0,0));
                float n010 = Hash31(i + float3(0,1,0));
                float n110 = Hash31(i + float3(1,1,0));
                float n001 = Hash31(i + float3(0,0,1));
                float n101 = Hash31(i + float3(1,0,1));
                float n011 = Hash31(i + float3(0,1,1));
                float n111 = Hash31(i + float3(1,1,1));

                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);

                float nxy0 = lerp(nx00, nx10, f.y);
                float nxy1 = lerp(nx01, nx11, f.y);

                return lerp(nxy0, nxy1, f.z);
            }

            float FBM(float3 p)
            {
                float n = ValueNoise3D(p);
                n += ValueNoise3D(p * 2.03 + 5.7) * 0.5;
                n += ValueNoise3D(p * 4.07 + 11.3) * 0.25;
                return n / 1.75;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float4 tex =
                    SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);

                float4 baseCol =
                    tex * _BaseColor;

                if (_AlphaClip > 0.5)
                    clip(baseCol.a - _Cutoff);

                float progress =
                    saturate(_DissolveAmount);

                if (progress >= 0.9999)
                    clip(-1.0);

                float holoProgress =
                    smoothstep(
                        _HologramStart,
                        max(_HologramStart + 0.001, _HologramFull),
                        progress
                    );

                float scanMask = 1.0;
                float blockMask = 1.0;
                float flicker = 1.0;
                float edge = 0.0;

                if (progress > 0.0001)
                {
                    float noise =
                        FBM(IN.positionWS * _NoiseScale);

                    float dissolveField =
                        saturate(
                            noise +
                            (noise - 0.5) * _NoiseStrength
                        );

                    float clipProgress =
                        smoothstep(
                            0.18,
                            1.0,
                            progress
                        );

                    float threshold =
                        lerp(
                            -0.08,
                            1.02,
                            clipProgress
                        );

                    float signedDistance =
                        dissolveField - threshold;

                    clip(signedDistance);

                    float scanPhase =
                        IN.positionWS.y *
                        _ScanLineCount +
                        _ScanLinePhase;

                    float scanWave =
                        frac(scanPhase);

                    scanMask =
                        step(
                            scanWave,
                            _ScanLineWidth
                        );

                    float2 blockUV =
                        IN.uv *
                        float2(
                            _BlockCountX,
                            _BlockCountY
                        );

                    float2 blockID =
                        floor(blockUV);

                    float blockRand =
                        Hash21(
                            blockID +
                            floor(progress * 24.0)
                        );

                    float dropoutAmount =
                        _BlockDropout *
                        holoProgress;

                    blockMask =
                        step(
                            dropoutAmount,
                            blockRand
                        );

                    float flickerRand =
                        Hash21(
                            float2(
                                floor(IN.positionWS.y * 18.0),
                                floor(progress * 60.0)
                            )
                        );

                    flicker =
                        lerp(
                            1.0,
                            0.45 + 0.55 * flickerRand,
                            _FlickerStrength *
                            holoProgress
                        );

                    edge =
                        1.0 -
                        smoothstep(
                            0.0,
                            _EdgeWidth,
                            signedDistance
                        );

                    float targetAlpha =
                        baseCol.a *
                        _Transparency *
                        blockMask *
                        lerp(
                            0.35,
                            1.0,
                            scanMask
                        );

                    float alpha =
                        lerp(
                            baseCol.a,
                            targetAlpha,
                            holoProgress
                        );

                    float2 screenUV =
                        IN.screenPos.xy /
                        max(IN.screenPos.w, 0.0001);

                    float2 pixelCoord =
                        floor(
                            screenUV *
                            _ScreenParams.xy
                        );

                    float dither =
                        Hash21(pixelCoord);

                    clip(alpha - dither);
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

                float3 albedo =
                    lerp(
                        baseCol.rgb,
                        _HoloColor.rgb * 0.22,
                        0.65 * holoProgress
                    );

                albedo *=
                    lerp(
                        1.0,
                        lerp(
                            0.35,
                            1.0,
                            scanMask
                        ),
                        holoProgress
                    );

                albedo *=
                    blockMask *
                    flicker;

                float3 emission =
                    SAMPLE_TEXTURE2D(
                        _EmissionMap,
                        sampler_EmissionMap,
                        IN.uv
                    ).rgb *
                    _EmissionColor.rgb;

                emission +=
                    _HoloColor.rgb *
                    0.45 *
                    holoProgress *
                    blockMask *
                    flicker;

                emission +=
                    _EdgeColor.rgb *
                    edge *
                    _EdgeIntensity *
                    holoProgress;

                SurfaceData surfaceData =
                    (SurfaceData)0;

                surfaceData.albedo =
                    albedo;

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
                    1.0;

                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
