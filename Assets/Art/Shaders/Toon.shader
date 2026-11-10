// LuckyStep/Toon -- "Printed Board" two-band cel shader for URP (Forward / Forward+, WebGL2).
//
// Look: two bands (lit / cool tinted shade), hard anti-aliased band edge, shade is never black
// (shade = base * _ShadeColor), cast shadows fold into the same band, ~3px ink outline via
// inverted hull. Gold is the only bloom: set an HDR _EmissionColor (> 1) on gold materials only.
//
// PROPERTIES
//   _BaseMap / _BaseColor        Albedo texture and tint.
//   _ShadeColor                  Multiplied onto base colour in the shade band (default #B8B0D8).
//   _ShadeThreshold              Half-lambert (N.L*0.5+0.5) cut-off between shade and lit (0..1).
//   _ShadeSoftness               Band half-width; widened automatically to one pixel (fwidth) for AA.
//   _EmissionColor (HDR)         Added after shading. Black = off.
//   _OutlineColor                Ink colour (#1C1A24).
//   _OutlineWidth                Outline thickness in pixels at 1080p screen height; constant
//                                with camera distance (perspective and orthographic). 0 disables.
//   _OutlineSmoothNormals        0 = extrude along mesh normal. 1 = extrude along the smoothed
//                                normal stored in vertex colour (see contract below).
//
// VERTEX-COLOUR CONTRACT (own Blender models, _OutlineSmoothNormals = 1)
//   COLOR.rgb = smoothed normal in OBJECT space, encoded n * 0.5 + 0.5 (unit n, same axes as the
//   imported mesh as Unity sees it, i.e. after FBX axis conversion). Alpha unused. Smoothed =
//   normals averaged over all vertices sharing a position, so hard edges do not split the hull.
//   Kenney / other meshes: leave _OutlineSmoothNormals = 0 (vertex colour ignored).
//
// Target: URP (Forward / Forward+), WebGL2 / GLES3 (#pragma target 3.0). Budget: 1 texture sample,
// well under 128 ALU in the lit fragment. Authored for Lucky Step by unity-shader-specialist.
// Keywords: _MAIN_LIGHT_SHADOWS / _MAIN_LIGHT_SHADOWS_CASCADE (main light shadows),
// _SHADOWS_SOFT (soft shadow filter), _CASTING_PUNCTUAL_LIGHT_SHADOW (shadow pass only).
// Total ForwardLit variants: 3 x 2 = 6.
//
// SRP Batcher: every material property lives in one CBUFFER_START(UnityPerMaterial).
// Additional (per-object / Forward+ clustered) lights and fog are intentionally ignored.

Shader "LuckyStep/Toon"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _ShadeColor("Shade Color", Color) = (0.7216, 0.6902, 0.8471, 1)
        _ShadeThreshold("Shade Threshold", Range(0, 1)) = 0.5
        _ShadeSoftness("Shade Softness", Range(0, 0.2)) = 0.01
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
        _OutlineColor("Outline Color", Color) = (0.1098, 0.1020, 0.1412, 1)
        _OutlineWidth("Outline Width (px @1080p)", Range(0, 8)) = 3
        [Toggle] _OutlineSmoothNormals("Outline Uses Smoothed Normals (vertex colour)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        // One CBUFFER shared by all passes -> SRP Batcher compatible.
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float4 _ShadeColor;
            float4 _EmissionColor;
            float4 _OutlineColor;
            float _ShadeThreshold;
            float _ShadeSoftness;
            float _OutlineWidth;
            float _OutlineSmoothNormals;
        CBUFFER_END
        ENDHLSL

        // ---------------------------------------------------------------- Main lit pass
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = p.positionCS;
                OUT.positionWS = p.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                float3 baseCol = baseTex.rgb * _BaseColor.rgb;

                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                // Half-lambert, then a crisp band; width is at least one pixel of change (fwidth) for AA.
                float h = dot(N, mainLight.direction) * 0.5 + 0.5;
                float soft = max(_ShadeSoftness, fwidth(h));
                float band = smoothstep(_ShadeThreshold - soft, _ShadeThreshold + soft, h);

                // Cast shadow joins the same band so shadowed areas become the tinted shade colour.
                float sh = mainLight.shadowAttenuation;
                band *= smoothstep(0.4, 0.6, sh);

                float3 shadeCol = baseCol * _ShadeColor.rgb;
                float3 col = lerp(shadeCol, baseCol, band) * mainLight.color;

                // Light ambient lift from spherical harmonics (never lets shade fall to black).
                col += baseCol * SampleSH(N) * 0.15;

                col += _EmissionColor.rgb;
                return float4(col, 1);
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------- Inverted-hull outline
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // Smoothed normal from vertex colour (n*0.5+0.5, object space) or mesh normal.
                float3 nOS = (_OutlineSmoothNormals > 0.5) ? (IN.color.rgb * 2.0 - 1.0) : IN.normalOS;
                float3 nWS = TransformObjectToWorldNormal(nOS);
                float3 nVS = mul((float3x3)UNITY_MATRIX_V, nWS);

                float4 posCS = TransformObjectToHClip(IN.positionOS.xyz);

                // View-space xy of a direction is already isotropic in pixels (P00 = P11 / aspect).
                float2 dir = nVS.xy;
                dir = dir / max(length(dir), 1e-5);

                // Pixels -> NDC (screen height spans 2 NDC units over 1080 px), x corrected by aspect.
                float aspect = _ScreenParams.x / _ScreenParams.y;
                // _ProjectionParams.x is -1 when the projection is Y-flipped (render-target / D3D), which would
                // otherwise push top/bottom hull edges into the object and leave the ink only on the sides.
                float2 ndcOffset = dir * (_OutlineWidth * 2.0 / 1080.0) * float2(1.0 / aspect, _ProjectionParams.x);

                // Multiply by w so the offset survives the perspective divide (constant pixel width).
                posCS.xy += ndcOffset * posCS.w;

                // Width 0 -> collapse to a degenerate (culled) vertex.
                OUT.positionCS = (_OutlineWidth > 0.0001) ? posCS : float4(0, 0, 0, 0);
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                return float4(_OutlineColor.rgb, 1);
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------- ShadowCaster
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            Cull Back
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDir = normalize(_LightPosition - positionWS);
            #else
                float3 lightDir = _LightDirection;
            #endif
                OUT.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDir));
                OUT.positionCS = ApplyShadowClamping(OUT.positionCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }

        // ---------------------------------------------------------------- DepthOnly
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            Cull Back
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half frag(Varyings IN) : SV_Target { return IN.positionCS.z; }
            ENDHLSL
        }

        // ---------------------------------------------------------------- DepthNormals
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return half4(NormalizeNormalPerPixel(IN.normalWS), 0.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
