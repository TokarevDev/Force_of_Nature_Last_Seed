Shader "ForceOfNature/Sprites/Cocoon Reward"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        [MaterialToggle] _ZWrite ("ZWrite", Float) = 0

        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0

        _BaseTint ("Base Tint", Color) = (0.82,0.96,0.98,1)
        _WormAccentColor ("Worm Accent Color", Color) = (0.0,0.62,0.72,1)
        _GlowColor ("Glow Color", Color) = (0.58,1,1,1)
        _GlowStrength ("Glow Strength", Range(0, 2)) = 0.08
        _PulseSpeed ("Pulse Speed", Float) = 1.7
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.05
        _PearlStrength ("Pearl Strength", Range(0, 1)) = 0.24
        _AccentStrength ("Accent Strength", Range(0, 1)) = 0.58
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.78
        _LegendaryBlend ("Legendary Blend", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex CocoonVertex
            #pragma fragment CocoonFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _BaseTint;
                half4 _WormAccentColor;
                half4 _GlowColor;
                half _GlowStrength;
                half _PulseSpeed;
                half _PulseAmount;
                half _PearlStrength;
                half _AccentStrength;
                half _RimStrength;
                half _LegendaryBlend;
            CBUFFER_END

            Varyings CocoonVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half4 CocoonFragment(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half alpha = tex.a * input.color.a;

                clip(alpha - 0.001);

                half luminance = dot(tex.rgb, half3(0.299, 0.587, 0.114));
                half pearlMask = saturate((luminance - 0.28) * 1.55);
                half3 pearl = lerp(tex.rgb, _BaseTint.rgb, _PearlStrength * pearlMask);

                half shadowMask = saturate((0.72 - luminance) * 2.2);
                half highlightMask = saturate((luminance - 0.72) * 2.6);

                float2 centeredUv = input.uv - 0.5f;
                half radial = 1.0 - saturate(length(centeredUv * float2(1.05f, 1.35f)) * 2.05);
                radial *= radial;

                half edge = saturate(length(centeredUv * float2(1.0f, 1.22f)) * 1.75 - 0.32);
                half rimMask = saturate(edge * shadowMask);

                half pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                half glowMask = saturate(radial + highlightMask * 0.28);
                half glow = _GlowStrength * pulse * glowMask;

                half3 color = pearl;
                color *= 1.0 - shadowMask * 0.22;
                color = lerp(color, _WormAccentColor.rgb * 0.82, shadowMask * _AccentStrength);
                color = lerp(color, _WormAccentColor.rgb, rimMask * _RimStrength);
                color += _GlowColor.rgb * glow;

                half legendaryHotspot = saturate(radial * _LegendaryBlend * pulse);
                color = lerp(color, _GlowColor.rgb, legendaryHotspot * 0.32);
                color *= input.color.rgb;

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
